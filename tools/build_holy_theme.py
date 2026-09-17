"""Register ImageGen's Holy sheets to Reference anatomy, then package 7-layer PSBs.

Uses the generated alpha. Registration only resamples existing generated pixels;
no procedural artwork is painted. Raw generations and exact prompts are archived.
Requires psd-tools==1.19.0, Pillow, numpy, scipy.
"""
import json
from pathlib import Path
import re
import shutil
import uuid
import zipfile

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi
from psd_tools import PSDImage
from psd_tools.api.layers import PixelLayer
from psd_tools.constants import Compression, Tag

from repair_chihuahua_psb import ROOT, ASSETS, NAMES, sha, dump, composite, independent_read

WORK = ROOT / "Artifacts/HolyTheme-20260918"
DEST = ASSETS / "10_Holy"
SIZE = (1155, 1362)
FILES = {
    "Thief": "exec-c645e326-99f0-406f-b0a8-3e39ea889de7.png",
    "Warrior": "exec-719d1223-b5de-4e52-a894-08140d931456.png",
    "Assassin": "exec-6944ee44-434b-49ff-946e-94d8e339cecb.png",
}
KEYS = dict(zip(NAMES, ["Weapon", "Head", "Body", "Arm1", "Arm2", "Leg1", "Leg2"]))


def bbox(mask):
    y, x = np.where(mask)
    return [int(x.min()), int(y.min()), int(x.max() + 1), int(y.max() + 1)]


def shaft(data):
    rgb = data[:, :, :3].astype(float)
    mask = ((data[:, :, 3] > 128) & (rgb[:, :, 0] > 140) &
            (rgb[:, :, 1] > 90) & (rgb[:, :, 2] < 100) & (rgb[:, :, 0] > rgb[:, :, 1] * 1.03))
    labels, _ = ndi.label(mask)
    counts = np.bincount(labels.ravel())
    components = [bbox(labels == i) for i in np.flatnonzero(counts[1:] > 100) + 1]
    return min(components, key=lambda b: b[1])


def split_sheet(image):
    data = np.asarray(image)
    labels, _ = ndi.label(data[:, :, 3] > 16)
    counts = np.bincount(labels.ravel())
    ids = np.flatnonzero(counts[1:] > 10000) + 1
    assert len(ids) == 7, "Expected exactly seven detached parts"
    parts = {}
    for ident in ids:
        left, top, right, bottom = bbox(labels == ident)
        cx, cy = (left + right) / 2, (top + bottom) / 2
        if cy < 500:
            name = "무기" if cx < 470 else "머리"
        elif cy < 930:
            name = "몸통"
        else:
            name = "팔1" if cx < 250 else "팔2" if cx < 500 else "다리1" if cx < 800 else "다리2"
        assert name not in parts
        # Retain the generated low-alpha antialias fringe around each object.
        layer = np.zeros_like(data)
        x0, x1 = max(0, left - 3), min(data.shape[1], right + 3)
        y0, y1 = max(0, top - 3), min(data.shape[0], bottom + 3)
        layer[y0:y1, x0:x1] = data[y0:y1, x0:x1]
        parts[name] = layer
    assert set(parts) == set(NAMES)
    return parts


def register(data, reference, is_limb):
    source_bounds = bbox(data[:, :, 3] > 128)
    target_bounds = bbox(reference[:, :, 3] > 128)
    src, dst = source_bounds, target_bounds
    if is_limb:
        src, dst = shaft(data), shaft(reference)
        src_y = [source_bounds[1], src[1], src[3], source_bounds[3]]
        dst_y = [target_bounds[1], dst[1], dst[3], target_bounds[3]]
    else:
        src_y, dst_y = [src[1], src[3]], [dst[1], dst[3]]
    # Pixel centers: preserve the bounds of the reference connector/part.
    y = np.arange(SIZE[1]) + 0.5
    mapped_y = np.interp(y, dst_y, src_y)
    mapped_y[y < dst_y[0]] = src_y[0] + (y[y < dst_y[0]] - dst_y[0]) * (src_y[1] - src_y[0]) / (dst_y[1] - dst_y[0])
    mapped_y[y > dst_y[-1]] = src_y[-1] + (y[y > dst_y[-1]] - dst_y[-1]) * (src_y[-1] - src_y[-2]) / (dst_y[-1] - dst_y[-2])
    mapped_x = src[0] + (np.arange(SIZE[0]) + 0.5 - dst[0]) * (src[2] - src[0]) / (dst[2] - dst[0])
    yy, xx = np.meshgrid(mapped_y - 0.5, mapped_x - 0.5, indexing="ij")
    # Premultiplied interpolation keeps the supplied transparent edges clean.
    srcf = data.astype(np.float32) / 255
    srcf[:, :, :3] *= srcf[:, :, 3:4]
    channels = [ndi.map_coordinates(srcf[:, :, i], [yy, xx], order=1, mode="constant", cval=0) for i in range(4)]
    result = np.stack(channels, axis=2)
    result[:, :, :3] /= np.maximum(result[:, :, 3:4], 1e-8)
    result = np.clip(np.rint(result * 255), 0, 255).astype(np.uint8)
    result[result[:, :, 3] == 0, :3] = 0
    actual = shaft(result) if is_limb else bbox(result[:, :, 3] > 128)
    error = int(np.abs(np.array(actual) - dst).max())
    assert error <= 2, (src, dst, actual, error)
    info = dict(source_visible_bbox=source_bounds, reference_visible_bbox=target_bounds,
                source_registration_bbox=src, reference_registration_bbox=dst,
                final_registration_bbox=actual, max_registration_error_px=error,
                source_y_landmarks=src_y, reference_y_landmarks=dst_y)
    return result, info


def meta(path, template=None, folder=False):
    target = Path(str(path) + ".meta")
    assert not target.exists(), f"Refusing to replace metadata: {target}"
    if template:
        text = re.sub(r"(?m)^guid: [0-9a-f]+", "guid: " + uuid.uuid4().hex, template)
    else:
        text = "fileFormatVersion: 2\nguid: " + uuid.uuid4().hex + "\n"
        if folder:
            text += "folderAsset: yes\nDefaultImporter:\n"
        else:
            text += "TextScriptImporter:\n"
        text += "  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    target.write_text(text, encoding="utf-8", newline="\n")


def main():
    assert not DEST.exists(), "New theme directory already exists"
    WORK.mkdir(parents=True, exist_ok=True)
    provenance = json.loads((WORK / "generation-provenance.json").read_text(encoding="utf-8"))
    reference_file = ASSETS / "Reference/character_base.psb"
    reference_sha = sha(reference_file)
    refdoc = PSDImage.open(reference_file)
    references = {}
    for layer in refdoc:
        canvas = Image.new("RGBA", SIZE)
        canvas.paste(layer.topil(), (layer.left, layer.top))
        references[layer.name] = np.asarray(canvas)
    with zipfile.ZipFile(ROOT / "Artifacts/PSBRepair-20260918/before.zip") as z:
        psb_meta = z.read("Assets/Art/ChihuahuaEquipmentThemes/01_Primitive/Assassin/rigging_layers.psb.meta").decode("utf-8")
    png_meta = (ASSETS / "Reference/character_base.png.meta").read_text(encoding="utf-8")
    reports, catalog_entries, previews = [], [], []
    for design in provenance["variants"]:
        job = design["class_id"]
        src = Path("C:/Users/user/.codex/generated_images/01a0b107-f487-7cc0-9ad1-0397a79ad61c") / FILES[job]
        source_archive = WORK / (job + "-generated.png")
        shutil.copyfile(src, source_archive)
        sheet = Image.open(source_archive).convert("RGBA")
        assert sheet.getextrema()[3] == (0, 255)
        extracted = split_sheet(sheet)
        parts, registration = {}, {}
        for name in NAMES:
            parts[name], registration[name] = register(extracted[name], references[name], name.startswith(("팔", "다리")))
        stage = WORK / "staged" / job
        stage.mkdir(parents=True, exist_ok=True)
        psb = PSDImage.new("RGBA", SIZE, color=(0, 0, 0, 0))
        psb._record.header.version = 2
        manifest_parts = []
        for name in reversed(NAMES):
            pixels = Image.fromarray(parts[name])
            bounds = pixels.getbbox()
            crop = pixels.crop(bounds)
            layer = PixelLayer.frompil(crop, psb, name=KEYS[name], top=bounds[1], left=bounds[0], compression=Compression.RLE)
            layer.name = name
            ident = 1001 + NAMES.index(name)
            layer._record.tagged_blocks.set_data(Tag.LAYER_ID, ident)
            manifest_parts.append(dict(name=name, key=KEYS[name], id=ident, bbox=list(bounds)))
        psb._update_record()
        psb._record.layer_and_mask_information.layer_info.layer_count = -7
        merged = composite(psb, SIZE)
        # This PNG is the registered transparent source for the layered export.
        merged.save(stage / "rigging_original.png")
        white = Image.alpha_composite(Image.new("RGBA", SIZE, "white"), merged)
        planes = [p.tobytes() for p in white.convert("RGB").split()] + [merged.getchannel("A").tobytes()]
        psb._record.image_data.compression = Compression.RLE
        psb._record.image_data.set_data(planes, psb._record.header)
        psb_path = stage / "rigging_layers.psb"
        with psb_path.open("wb") as f:
            psb._record.write(f)
        reopened = PSDImage.open(psb_path)
        assert len(reopened) == 7 and [l.name for l in reversed(reopened)] == NAMES
        assert composite(reopened, SIZE).tobytes() == merged.tobytes()
        decoded, merged_bytes = independent_read(psb_path)
        assert merged_bytes == b"".join(planes)
        for record, layer in zip(decoded, reopened):
            assert record["id"] == 1001 + NAMES.index(layer.name)
            assert layer.topil().tobytes() == Image.fromarray(parts[layer.name]).crop(layer.bbox).tobytes()
            for key, channel in zip([0, 1, 2, -1], layer.topil().split()):
                assert record["pixels"][key] == channel.tobytes()
        body_bounds = bbox(parts["몸통"][:, :, 3] > 128)
        assert np.abs(np.array(body_bounds) - bbox(references["몸통"][:, :, 3] > 128)).max() <= 2
        _, comp_count = ndi.label(np.asarray(merged)[:, :, 3] > 128)
        manifest = dict(canvas=list(SIZE), layers_top_to_bottom=NAMES,
            source_sha256=sha(stage / "rigging_original.png"), psb_sha256=sha(psb_path),
            background_layer_included=False, compression="RLE", parts=list(reversed(manifest_parts)),
            reference="../../Reference/character_base.png", registration=registration,
            source_reconstruction_max_channel_difference=0)
        dump(stage / "layers.json", manifest)
        equipment = dict(id="10_Holy/" + job, title="신성한 " + design["job"], grade="신성한", job=design["job"],
            original="rigging_original.png", psb="rigging_layers.psb", scope="rigging_artwork", complete=True)
        dump(stage / "equipment.json", equipment)
        dump(stage / "design.json", dict(id=equipment["id"], grade_id="10_Holy", grade="신성한", class_id=job,
            job=design["job"], title=equipment["title"], prompt=design["prompt"],
            restrictions=["no sleeves", "no shoulder extensions", "no back ornaments", "no wings", "no cape"],
            provenance="Artifacts/HolyTheme-20260918/generation-provenance.json"))
        report = dict(class_id=job, source_generated_size=list(sheet.size), output_size=list(SIZE),
            source_generated_sha256=sha(source_archive), png_sha256=manifest["source_sha256"], psb_sha256=manifest["psb_sha256"],
            layer_count=7, independent_rle_decode="PASS", psb_layer_composite_equals_png=True,
            max_registration_error_px=max(r["max_registration_error_px"] for r in registration.values()),
            visual_review="No sleeves, shoulder extensions, back ornaments, wings, capes or additional props",
            unity_import_validation="NOT RUN", photoshop_application_validation="NOT RUN")
        reports.append(report)
        catalog_entries.append(equipment)
        white.save(WORK / (job + "-preview-white.png"))
        preview = Image.alpha_composite(Image.new("RGBA", SIZE, "#777777"), merged)
        preview.save(WORK / (job + "-preview-gray.png"))
        previews.append((design["job"], preview))
        print(json.dumps(report, ensure_ascii=False), flush=True)
    assert sha(reference_file) == reference_sha
    # All three staged files pass before they are installed into Assets.
    DEST.mkdir()
    meta(DEST, folder=True)
    for job in FILES:
        folder = DEST / job
        shutil.copytree(WORK / "staged" / job, folder)
        meta(folder, folder=True)
        for path in folder.iterdir():
            if path.suffix == ".psb":
                meta(path, psb_meta)
            elif path.suffix == ".png":
                meta(path, png_meta)
            elif path.suffix == ".json":
                meta(path)
    catalog_path = ASSETS / "catalog.json"
    shutil.copyfile(catalog_path, WORK / "catalog-before.json")
    catalog = json.loads(catalog_path.read_text(encoding="utf-8"))
    assert not any(e["id"].startswith("10_Holy/") for e in catalog["themes"])
    catalog["themes"].extend(catalog_entries)
    catalog["layer_order"] = NAMES
    dump(catalog_path, catalog)
    dump(WORK / "verification.json", dict(reference_sha256=reference_sha, variants=reports,
        registration_note="Head/torso/weapon bounds aligned; limb exposed-shaft bounds and distal ends registered to Reference. No bones/weights created.",
        generated_method="built-in image_gen", anatomy_limit="Silhouettes and connector landmarks registered within 2 pixels; generated artwork is not pixel-identical to the reference."))
    sheet = Image.new("RGB", (1155, 480), "#777777")
    for i, (_, preview) in enumerate(previews):
        preview.thumbnail((375, 442))
        sheet.paste(preview.convert("RGB"), (i * 385 + 5, 30))
    draw = ImageDraw.Draw(sheet)
    for i, job in enumerate(FILES):
        draw.text((i * 385 + 10, 8), "10 HOLY - " + job, fill="white")
    sheet.save(WORK / "preview-all.png")


if __name__ == "__main__":
    main()
