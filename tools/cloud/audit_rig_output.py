"""Independently audit cloud-generated Unity metadata and render mesh diagnostics."""
import argparse
import hashlib
import json
import math
from pathlib import Path
import re
import struct

from PIL import Image, ImageDraw, ImageFont
import yaml


def read(path):
    # Unity writes packed int arrays as unquoted hex; YAML 1.1 mistakes digits for octal.
    source = path.read_text(encoding="utf-8")
    source = re.sub(r'(?m)^(\s*(?:bones|indices): )([0-9a-fA-F]+)[ \t]*$', r'\1"\2"', source)
    return yaml.safe_load(source)


def ints(value):
    if not value:
        return []
    raw = bytes.fromhex(str(value))
    return list(struct.unpack("<" + "i" * (len(raw) // 4), raw))


def world(bones, index):
    b = bones[index]
    x, y = b["position"]["x"], b["position"]["y"]
    q = b["rotation"]
    angle = 2 * math.atan2(q["z"], q["w"])
    if b["parentId"] >= 0:
        px, py, pa = world(bones, b["parentId"])
        return px + math.cos(pa)*x-math.sin(pa)*y, py + math.sin(pa)*x+math.cos(pa)*y, pa+angle
    return x, y, angle


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("output", type=Path)
    ap.add_argument("--root", type=Path, default=Path.cwd())
    ap.add_argument("--evidence", type=Path, default=Path("Artifacts/ChihuahuaRig-20260918"))
    args = ap.parse_args()
    report = json.loads((args.output / "verification.json").read_text(encoding="utf-8"))
    assert (args.output / "SUCCESS.txt").exists()
    assert report["processedFiles"] == 30 and report["unityVersion"] == "6000.3.8f1"
    reference = read(args.root / "Assets/Art/ChihuahuaEquipmentThemes/Reference/character_base.psb.meta")["ScriptedImporter"]
    reference_bones = reference["characterData"]["bones"]
    reference_sprites = {s["name"]: s for s in reference["layeredSpriteImportData"]}
    reference_parts = {p["spriteId"]:p for p in reference["characterData"]["parts"]}
    result = {"files": [], "maxBoneNumericError": 0.0, "vertices": 0, "triangles": 0, "meshes": 0}
    panels = []
    args.evidence.mkdir(parents=True, exist_ok=True)
    for f in report["files"]:
        rel = f["path"]
        output_meta = args.output / (rel + ".meta")
        before_meta = args.root / (rel + ".meta")
        after, before = read(output_meta), read(before_meta)
        assert after["guid"] == before["guid"] == f["assetGuid"]
        d = after["ScriptedImporter"]
        sprites = d["layeredSpriteImportData"]
        assert len(sprites) == 7
        assert {s["name"]:s["spriteID"] for s in sprites} == {s["name"]:s["spriteID"] for s in before["ScriptedImporter"]["layeredSpriteImportData"]}
        assert d["characterMode"] == 1 and d["mosaicLayers"] == 1
        assert len(d["characterData"]["bones"]) == 11
        for b,r in zip(d["characterData"]["bones"], reference_bones):
            for k in ("name", "guid", "parentId", "color"):
                assert b[k] == r[k], (rel,k,b[k],r[k])
            errors = [abs(b["length"] - r["length"])]
            errors += [abs(b[k][axis]-r[k][axis]) for k in ("position", "rotation") for axis in r[k]]
            error = max(errors)
            assert error < 0.001, (rel,b["name"],error)
            result["maxBoneNumericError"] = max(result["maxBoneNumericError"], error)
        parts = {p["spriteId"]:p for p in d["characterData"]["parts"]}
        im = Image.open(args.root / Path(rel).parent / "rigging_original.png").convert("RGBA")
        canvas = Image.new("RGBA", im.size, (35,40,50,255))
        canvas.alpha_composite(im)
        draw = ImageDraw.Draw(canvas)
        stats = []
        for s in sprites:
            r = reference_sprites[s["name"]]
            assert ints(parts[s["spriteID"]]["bones"]) == ints(reference_parts[r["spriteID"]]["bones"])
            bones, vertices, indices = s["spriteBone"], s["vertices"], ints(s["indices"])
            assert len(bones) == len(r["spriteBone"])
            assert len(vertices) > 4 and len(indices) >= 3 and len(indices) % 3 == 0
            assert all(0 <= i < len(vertices) for i in indices)
            ox, oy = s["spritePosition"]["x"], s["spritePosition"]["y"]
            points = [(v["position"]["x"]+ox, im.height-(v["position"]["y"]+oy)) for v in vertices]
            for i in range(0,len(indices),3):
                tri = [points[j] for j in indices[i:i+3]]
                draw.line(tri+[tri[0]], fill=(30,210,230,220), width=1)
            for v in vertices:
                w = v["boneWeight"]
                assert abs(sum(w["weight"+str(i)] for i in range(4))-1) < 0.001
                assert all(math.isfinite(w["weight"+str(i)]) and w["weight"+str(i)] >= 0 for i in range(4))
                assert all(w["weight"+str(i)] == 0 or 0 <= w["boneIndex"+str(i)] < len(bones) for i in range(4))
            for i,b in enumerate(bones):
                rb = r["spriteBone"][i]
                assert b["name"] == rb["name"] and b["guid"] == rb["guid"] and b["parentId"] == rb["parentId"]
                x,y,a = world(bones,i)
                rx,ry,ra = world(r["spriteBone"],i)
                assert abs(x+ox-rx-r["spritePosition"]["x"]) < 0.005
                assert abs(y+oy-ry-r["spritePosition"]["y"]) < 0.005
                start = (x+ox,im.height-y-oy)
                end = (x+ox+math.cos(a)*b["length"],im.height-y-oy-math.sin(a)*b["length"])
                draw.line([start,end],fill=(255,75,165,255),width=4)
                draw.ellipse((start[0]-5,start[1]-5,start[0]+5,start[1]+5),fill=(255,245,70,255))
            result["vertices"] += len(vertices)
            result["triangles"] += len(indices)//3
            result["meshes"] += 1
            stats.append({"name":s["name"],"vertices":len(vertices),"triangles":len(indices)//3,"bones":len(bones)})
        name = "-".join(Path(rel).parts[-3:-1])
        canvas.convert("RGB").save(args.evidence / (name+"-mesh.jpg"), quality=88)
        panel = Image.new("RGB",(385,480),(35,40,50))
        panel.paste(canvas.convert("RGB").resize((385,454)),(0,26))
        ImageDraw.Draw(panel).text((8,5),name,fill="white")
        panels.append(panel)
        result["files"].append({"path":rel,"sha256":hashlib.sha256(output_meta.read_bytes()).hexdigest(),"layers":stats})
    assert result["meshes"] == 210
    sheet = Image.new("RGB",(385*6,480*5))
    for i,p in enumerate(panels): sheet.paste(p,((i%6)*385,(i//6)*480))
    sheet.save(args.evidence / "all-meshes.jpg",quality=88)
    (args.evidence / "independent-audit.json").write_text(json.dumps(result,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print(json.dumps({k:v for k,v in result.items() if k != "files"},indent=2))


if __name__ == "__main__":
    main()
