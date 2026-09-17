"""Separate the seven existing character_base.png parts into a PSB container.

No redraw, resizing, or repositioning. Only the exterior white sheet is masked.
Requires Pillow, numpy, scipy, psd-tools==1.19.0.
"""

import json
import re
import shutil
import uuid
import zipfile

import numpy as np
from PIL import Image
from scipy import ndimage as ndi
from psd_tools import PSDImage
from psd_tools.api.layers import PixelLayer
from psd_tools.constants import Compression, Tag

from repair_chihuahua_psb import ASSETS, ROOT, NAMES, composite, dump, independent_read, sha


def main():
    folder = ASSETS / "Reference"
    source = folder / "character_base.png"
    output = folder / "character_base.psb"
    if output.exists():
        previous = json.loads((folder / "layers.json").read_text(encoding="utf-8"))
        assert sha(output) == previous["psb_sha256"], "Refusing to replace externally modified PSB"
    work = ROOT / "Artifacts/ReferencePSB-20260918"
    work.mkdir(parents=True, exist_ok=True)
    original_hash = sha(source)
    original_meta_hash = sha(source.with_suffix(".png.meta"))
    with zipfile.ZipFile(work / "source-backup.zip", "w", zipfile.ZIP_DEFLATED) as z:
        for path in (source, source.with_suffix(".png.meta")):
            z.write(path, path.relative_to(ROOT).as_posix())
    image = Image.open(source).convert("RGBA")
    data = np.asarray(image)
    rgb = data[:, :, :3]
    labels, _ = ndi.label(rgb.min(axis=2) < 200)
    counts = np.bincount(labels.ravel())
    ids = sorted(np.flatnonzero(counts[1:] > 10000) + 1)
    assert len(ids) == 7
    # Spatial ordering of the separated objects in the unmodified source PNG.
    assignments = dict(zip(ids, ["무기", "머리", "몸통", "팔1", "팔2", "다리1", "다리2"]))
    parts = {}
    covered = np.zeros(data.shape[:2], dtype=bool)
    for component, name in assignments.items():
        inside = ndi.binary_fill_holes((labels == component) & (rgb.min(axis=2) < 80))
        nearby = ndi.binary_dilation(inside, iterations=2)
        fringe = nearby & ~inside & (rgb.min(axis=2) < 250)
        support = inside | fringe
        assert not np.any(covered & support)
        covered |= support
        rgba = np.zeros_like(data)
        rgba[inside, :3] = rgb[inside]
        rgba[inside, 3] = 255
        # Remove the white matte only along the outer antialiased black outline.
        # Enclosed whites (eyes, gloves, soles) are protected by hole filling.
        alpha = (255 - rgb[fringe].min(axis=1)).astype(np.float64)
        rgba[fringe, 3] = alpha.astype(np.uint8)
        unmatted = (rgb[fringe].astype(np.float64) - (255 - alpha[:, None])) * 255 / alpha[:, None]
        rgba[fringe, :3] = np.clip(np.rint(unmatted), 0, 255).astype(np.uint8)
        y, x = np.where(support)
        bbox = [int(x.min()), int(y.min()), int(x.max() + 1), int(y.max() + 1)]
        cropped = Image.fromarray(rgba).crop(bbox)
        assert np.array_equal(rgba[inside], data[inside])
        parts[name] = dict(image=cropped, bbox=bbox, id=1001 + NAMES.index(name),
                           opaque_pixel_count=int(inside.sum()), edge_pixel_count=int(fringe.sum()))
        cropped.save(work / f"{parts[name]['id']}.png")

    psb = PSDImage.new("RGBA", image.size, color=(0, 0, 0, 0))
    psb._record.header.version = 2
    for name in reversed(NAMES):
        part = parts[name]
        layer = PixelLayer.frompil(part["image"], psb, name=name,
            left=part["bbox"][0], top=part["bbox"][1], compression=Compression.RLE)
        layer._record.name = ["Weapon", "Head", "Body", "Arm1", "Arm2", "Leg1", "Leg2"][NAMES.index(name)]
        layer._record.tagged_blocks.set_data(Tag.UNICODE_LAYER_NAME, name)
        layer._record.tagged_blocks.set_data(Tag.LAYER_ID, part["id"])
    psb._update_record()
    psb._record.layer_and_mask_information.layer_info.layer_count = -7
    merged = composite(psb, image.size)
    white = Image.alpha_composite(Image.new("RGBA", image.size, "white"), merged)
    planes = [channel.tobytes() for channel in white.convert("RGB").split()]
    planes.append(merged.getchannel("A").tobytes())
    psb._record.image_data.compression = Compression.RLE
    psb._record.image_data.set_data(planes, psb._record.header)
    staged = work / "character_base.psb"
    with staged.open("wb") as f:
        psb._record.write(f)

    reopened = PSDImage.open(staged)
    assert len(reopened) == 7 and [x.name for x in reversed(reopened)] == NAMES
    assert reopened.size == image.size
    records, merged_bytes = independent_read(staged)
    assert merged_bytes == b"".join(planes)
    for record, layer in zip(records, reopened):
        part = parts[layer.name]
        assert list(layer.bbox) == part["bbox"] and record["id"] == part["id"]
        assert layer.topil().tobytes() == part["image"].tobytes()
        for key, channel in zip([0, 1, 2, -1], part["image"].split()):
            assert record["pixels"][key] == channel.tobytes()
    error = np.abs(np.asarray(white).astype(np.int16) - data.astype(np.int16))
    assert error[covered].max() <= 1
    assert merged.getpixel((0, 0))[3] == 0
    merged.save(work / "transparent-preview.png")
    preview = Image.alpha_composite(Image.new("RGBA", image.size, "#777777"), merged)
    preview.save(work / "preview-gray.png")
    manifest = dict(source="character_base.png", canvas=list(image.size),
        source_sha256=original_hash, psb_sha256=sha(staged), layers_top_to_bottom=NAMES,
        background_layer_included=False, compression="RLE", parts=[
            {k: v for k, v in parts[name].items() if k != "image"} | {"name": name}
            for name in NAMES])
    dump(work / "layers.json", manifest)
    report = dict(source_unchanged=True, source_sha256=original_hash, layer_count=7,
        canvas=list(image.size), layer_ids=[1001, 1002, 1003, 1004, 1005, 1006, 1007],
        independent_psb_and_rle_decode="PASS", extracted_pixels_roundtrip="PASS",
        opaque_part_pixel_difference=0, white_composite_difference_on_parts=int(error[covered].max()),
        background_method="Exterior sheet removed; enclosed whites preserved; 2px outer edge white matte removed",
        unity_import_validation="NOT RUN", photoshop_application_validation="NOT RUN")
    dump(work / "verification.json", report)
    assert sha(source) == original_hash and sha(source.with_suffix(".png.meta")) == original_meta_hash

    # Reuse the clean importer template from before Unity populated sprite caches.
    with zipfile.ZipFile(ROOT / "Artifacts/PSBRepair-20260918/before.zip") as z:
        template = z.read("Assets/Art/ChihuahuaEquipmentThemes/01_Primitive/Assassin/rigging_layers.psb.meta").decode("utf-8")
    template = re.sub(r"(?m)^guid: [0-9a-f]+", "guid: " + uuid.uuid4().hex, template)
    template = template.replace("T513 ChihuahuaEquipmentThemes", "ChihuahuaEquipmentThemes Reference parts")
    if not output.with_suffix(".psb.meta").exists():
        output.with_suffix(".psb.meta").write_text(template, encoding="utf-8", newline="\n")
    shutil.copyfile(staged, output)
    shutil.copyfile(work / "layers.json", folder / "layers.json")
    if not (folder / "layers.json.meta").exists():
        (folder / "layers.json.meta").write_text(
            "fileFormatVersion: 2\nguid: " + uuid.uuid4().hex +
            "\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n",
            encoding="utf-8", newline="\n")
    assert sha(output) == manifest["psb_sha256"]
    print(json.dumps(report, ensure_ascii=False, indent=2))
    print(output)


if __name__ == "__main__":
    main()
