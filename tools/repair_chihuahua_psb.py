"""Lossless PSB container repair; no artwork generation or pixel retouching.

Requires psd-tools==1.19.0, Pillow, numpy. Run from the repository root.
All outputs are staged and verified before --apply replaces the asset files.
The original PNG, Unity .meta, layer IDs, bounds, and RGBA bytes are preserved.
"""

import argparse
import hashlib
import io
import json
from pathlib import Path
import shutil
import struct
import zipfile

import numpy as np
from PIL import Image, ImageDraw
from psd_tools import PSDImage
from psd_tools.constants import Compression, Tag

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets/Art/ChihuahuaEquipmentThemes"
NAMES = ["무기", "머리", "몸통", "팔1", "팔2", "다리1", "다리2"]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def dump(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")


def composite(layers, size):
    result = Image.new("RGBA", size)
    for layer in layers:
        result.alpha_composite(layer.topil(), (layer.left, layer.top))
    return result


def unpackbits(data, width):
    """Independent PackBits decoder, not psd-tools' codec."""
    result = bytearray()
    pos = 0
    while pos < len(data):
        n = data[pos]
        pos += 1
        if n < 128:
            count = n + 1
            assert pos + count <= len(data)
            result.extend(data[pos:pos + count])
            pos += count
        elif n > 128:
            assert pos < len(data)
            result.extend(data[pos:pos + 1] * (257 - n))
            pos += 1
    assert len(result) == width, (len(result), width)
    return bytes(result)


def independent_read(path):
    """Check PSB section bounds, names, IDs, 32-bit RLE tables, and every pixel."""
    f = io.BytesIO(path.read_bytes())

    def read(n):
        data = f.read(n)
        assert len(data) == n
        return data

    def number(fmt):
        return struct.unpack(">" + fmt, read(struct.calcsize(">" + fmt)))[0]

    def skip_block(fmt):
        read(number(fmt))

    def rle(width, rows):
        lengths = [number("I") for _ in range(rows)]
        return b"".join(unpackbits(read(n), width) for n in lengths)

    assert read(4) == b"8BPS" and number("H") == 2
    assert read(6) == b"\0" * 6
    assert number("H") == 4
    height, width = number("I"), number("I")
    assert number("H") == 8 and number("H") == 3
    skip_block("I")
    skip_block("I")
    mask_size = number("Q")
    mask_end = f.tell() + mask_size
    info_size = number("Q")
    info_end = f.tell() + info_size
    assert number("h") == -7  # merged fourth channel is transparency
    records = []
    for _ in range(7):
        top, left, bottom, right = [number("i") for _ in range(4)]
        channels = [(number("h"), number("Q")) for _ in range(number("H"))]
        assert [ch[0] for ch in channels] == [-1, 0, 1, 2]
        assert read(8) == b"8BIMnorm"
        assert number("B") == 255 and number("B") == 0
        assert number("B") & 2 == 0
        assert number("B") == 0
        extra_size = number("I")
        extra_end = f.tell() + extra_size
        skip_block("I")
        skip_block("I")
        n = number("B")
        read(n)
        read((-(n + 1)) % 4)
        name, layer_id = None, None
        while f.tell() < extra_end:
            assert read(4) == b"8BIM"
            key, size = read(4), number("I")
            payload = read(size)
            if key == b"luni":
                count = struct.unpack(">I", payload[:4])[0]
                name = payload[4:4 + count * 2].decode("utf-16-be").rstrip("\0")
            elif key == b"lyid":
                layer_id = struct.unpack(">I", payload)[0]
            read(size % 2)
        assert f.tell() == extra_end
        records.append(dict(name=name, id=layer_id, bbox=(left, top, right, bottom), channels=channels))
    for rec in records:
        left, top, right, bottom = rec["bbox"]
        pixels = {}
        for ch_id, length in rec.pop("channels"):
            start = f.tell()
            assert number("H") == 1
            pixels[ch_id] = rle(right - left, bottom - top)
            assert f.tell() == start + length
        rec["pixels"] = pixels
    assert 0 <= info_end - f.tell() < 4
    assert not any(read(info_end - f.tell()))
    assert number("I") == 0  # no global layer mask
    assert not any(read(mask_end - f.tell()))
    assert number("H") == 1
    merged = rle(width, height * 4)
    assert not f.read(1), "Unexpected trailing bytes"
    return records, merged


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--output", default="Artifacts/PSBRepair-20260918")
    args = parser.parse_args()
    out = (ROOT / args.output).resolve()
    out.mkdir(parents=True, exist_ok=True)
    files = sorted(ASSETS.glob("*/*/rigging_layers.psb"))
    assert len(files) == 27
    backup = out / "before.zip"
    if not backup.exists():
        with zipfile.ZipFile(backup, "x", zipfile.ZIP_DEFLATED) as z:
            z.write(ASSETS / "README.md", "Assets/Art/ChihuahuaEquipmentThemes/README.md")
            for path in files:
                for p in [path, path.with_suffix(".psb.meta"), path.with_name("layers.json")]:
                    z.write(p, p.relative_to(ROOT).as_posix())
    report = dict(format="PSB v2, RGB 8-bit, RLE layers and composite", files=[],
                  unity_runtime_validation="NOT RUN; local Unity execution prohibited; no cloud run dispatched",
                  photoshop_application_validation="NOT RUN", backup=str(backup))
    previews = []
    for path in files:
        relative = path.parent.relative_to(ASSETS)
        dest = out / "staged" / relative
        dest.mkdir(parents=True, exist_ok=True)
        original, meta = path.with_name("rigging_original.png"), path.with_suffix(".psb.meta")
        source_hash, meta_hash = sha(original), sha(meta)
        manifest = json.loads(path.with_name("layers.json").read_text(encoding="utf-8"))
        assert manifest["source_sha256"] == source_hash
        psb = PSDImage.open(path)
        source = Image.open(original).convert("RGBA")
        assert psb.size == source.size == tuple(manifest["canvas"])
        # Existing separated masks are verified against the original before reuse.
        full = composite(psb, psb.size)
        source_diff = int(np.abs(np.asarray(full).astype(np.int16) - np.asarray(source).astype(np.int16)).max())
        assert source_diff <= 1
        selected = [layer for layer in psb if layer.name in NAMES]
        assert [l.name for l in reversed(selected)] == NAMES
        manifest_parts = {part["name"]: part for part in manifest["parts"]}
        before = {}
        for layer in selected:
            part = manifest_parts[layer.name]
            assert tuple(part["bbox"]) == layer.bbox
            assert part["id"] == layer._record.tagged_blocks.get_data(Tag.LAYER_ID)
            assert layer.visible and layer.opacity == 255
            pixels = layer.topil()
            assert pixels.mode == "RGBA" and pixels.getchannel("A").getextrema()[1] == 255
            before[layer.name] = pixels.tobytes()
            for channel in layer._channels:
                raw = channel.get_data(layer.width, layer.height, 8, 2)
                channel.compression = Compression.RLE
                channel.set_data(raw, layer.width, layer.height, 8, 2)
        info = psb._record.layer_and_mask_information.layer_info
        info.layer_records[:] = [l._record for l in selected]
        info.channel_image_data[:] = [l._channels for l in selected]
        info.layer_count = -7
        merged = composite(selected, psb.size)
        # Photoshop's merged preview uses white-matted RGB plus a separate alpha.
        # Individual editable layer channels remain straight, unchanged RGBA.
        white_preview = Image.alpha_composite(Image.new("RGBA", psb.size, "white"), merged)
        merged_data = [channel.tobytes() for channel in white_preview.convert("RGB").split()]
        merged_data.append(merged.getchannel("A").tobytes())
        psb._record.image_data.compression = Compression.RLE
        psb._record.image_data.set_data(merged_data, psb._record.header)
        target = dest / path.name
        # Write the record directly so no optional compositor changes pixel values.
        with target.open("wb") as f:
            psb._record.write(f)
        reopened = PSDImage.open(target)
        assert len(reopened) == 7
        assert [l.name for l in reversed(reopened)] == NAMES
        for layer in reopened:
            assert layer.topil().tobytes() == before[layer.name]
        preview = reopened.topil()
        assert preview.getchannel("A").tobytes() == merged.getchannel("A").tobytes()
        reopened_white = Image.alpha_composite(Image.new("RGBA", psb.size, "white"), preview)
        assert np.abs(np.asarray(reopened_white).astype(np.int16) - np.asarray(white_preview).astype(np.int16)).max() <= 1
        decoded, independent_merged = independent_read(target)
        assert independent_merged == b"".join(merged_data)
        for rec, layer in zip(decoded, reopened):
            assert rec["name"] == layer.name and rec["bbox"] == layer.bbox
            assert rec["id"] == manifest_parts[layer.name]["id"]
            for key, channel in zip([0, 1, 2, -1], layer.topil().split()):
                assert rec["pixels"][key] == channel.tobytes()
        assert sha(original) == source_hash and sha(meta) == meta_hash
        manifest["layers_top_to_bottom"] = NAMES
        manifest["psb_sha256"] = sha(target)
        manifest.pop("background_visible", None)
        manifest.pop("source_reconstruction_max_channel_difference", None)
        manifest["background_layer_included"] = False
        manifest["repair"] = dict(compression="RLE", visible_layer_count=7,
            recovered_layer_rgba_max_channel_difference=0,
            pre_repair_source_reconstruction_max_channel_difference=source_diff,
            independently_decoded=True, unity_import_verified=False)
        dump(dest / "layers.json", manifest)
        report["files"].append(dict(path=path.relative_to(ROOT).as_posix(),
            old_sha256=sha(path), new_sha256=sha(target), original_sha256=source_hash,
            meta_sha256=meta_hash, layers=NAMES, layer_ids=[p["id"] for p in manifest["parts"]],
            canvas=list(psb.size), rgba_pixel_difference=0,
            source_reconstruction_difference=source_diff, independent_rle_decode="PASS"))
        preview = merged.copy()
        preview.thumbnail((280, 330))
        previews.append((relative.as_posix(), preview))
        print(f"Verified {relative}: 7 layers, unchanged RGBA, independent RLE decode PASS", flush=True)
    for page in range(3):
        sheet = Image.new("RGB", (900, 1110), "#777777")
        draw = ImageDraw.Draw(sheet)
        for i, (title, preview) in enumerate(previews[page * 9:(page + 1) * 9]):
            x, y = (i % 3) * 300, (i // 3) * 370
            draw.text((x + 8, y + 8), title, fill="white")
            sheet.paste(preview, (x + 10, y + 32), preview)
        sheet.save(out / f"preview-{page + 1}.png")
    if args.apply:
        # Recheck for concurrent asset edits before replacing any source.
        for row in report["files"]:
            path = ROOT / row["path"]
            assert sha(path) == row["old_sha256"]
            assert sha(path.with_name("rigging_original.png")) == row["original_sha256"]
            assert sha(path.with_suffix(".psb.meta")) == row["meta_sha256"]
        for path in files:
            staged = out / "staged" / path.parent.relative_to(ASSETS)
            shutil.copyfile(staged / path.name, path)
            shutil.copyfile(staged / "layers.json", path.with_name("layers.json"))
        report["applied"] = True
    else:
        report["applied"] = False
    dump(out / "verification.json", report)


if __name__ == "__main__":
    main()
