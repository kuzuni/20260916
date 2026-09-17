"""Prepare a minimal Unity project exclusively on the GitHub-hosted Linux runner."""
import json
import os
from pathlib import Path
import shutil

assert os.environ.get("GITHUB_ACTIONS") == "true" and os.name == "posix"
root = Path("RiggingProject")
assert not root.exists()
(root / "ProjectSettings").mkdir(parents=True)
(root / "Packages").mkdir()
(root / "Assets/Editor").mkdir(parents=True)
shutil.copyfile("ProjectSettings/ProjectVersion.txt", root / "ProjectSettings/ProjectVersion.txt")
(root / "Packages/manifest.json").write_text(json.dumps({"dependencies": {
    "com.unity.2d.animation": "13.0.4", "com.unity.2d.psdimporter": "12.0.1",
    "com.unity.2d.sprite": "1.0.0", "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0", "com.unity.modules.physics2d": "1.0.0"
}}, indent=2) + "\n")
shutil.copyfile("tools/cloud/ChihuahuaRigBatch.cs", root / "Assets/Editor/ChihuahuaRigBatch.cs")
paths = list(Path("Assets/Art/ChihuahuaEquipmentThemes").rglob("*.psb"))
assert len(paths) == 31
for path in paths:
    dest = root / path
    dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(path, dest)
    shutil.copyfile(str(path) + ".meta", str(dest) + ".meta")
print("Prepared 31 PSBs (one reference, thirty targets) for hosted Unity.")
