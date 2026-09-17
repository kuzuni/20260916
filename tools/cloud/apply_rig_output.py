"""Copy audited cloud metadata only if local inputs still match the saved snapshot."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("output", type=Path)
    ap.add_argument("--manifest", type=Path, required=True)
    ap.add_argument("--audit", type=Path, required=True)
    ap.add_argument("--destination", type=Path, required=True)
    ap.add_argument("--backup", type=Path, required=True)
    args = ap.parse_args()
    before = json.loads(args.manifest.read_text())
    audit = json.loads(args.audit.read_text(encoding="utf-8"))
    assert len(audit["files"]) == 30 and audit["meshes"] == 210
    for rel, values in before.items():
        for suffix,key in [("", "psb_sha256"), (".meta", "meta_sha256")]:
            assert sha(args.destination / (rel+suffix)) == values[key], "Changed since snapshot: " + rel+suffix
    for entry in audit["files"]:
        rel = entry["path"] + ".meta"
        assert sha(args.output / rel) == entry["sha256"]
        assert "Reference" not in Path(rel).parts
    for entry in audit["files"]:
        rel = entry["path"] + ".meta"
        backup = args.backup / rel
        assert not backup.exists(), "Refusing to overwrite backup: " + str(backup)
        backup.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(args.destination / rel, backup)
    for entry in audit["files"]:
        rel = entry["path"] + ".meta"
        shutil.copyfile(args.output / rel, args.destination / rel)
        assert sha(args.destination / rel) == entry["sha256"]
    print("Applied 30 audited metadata files; reference and PSB image contents preserved.")


if __name__ == "__main__":
    main()
