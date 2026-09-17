# Chihuahua assets available to cloud tasks

The authoritative files are committed to this repository under `Assets/Art`.
They do not require access to the original Windows filesystem or Codex image cache.

- `Assets/Art/ChihuahuaEquipmentThemes/01_Primitive` through `10_Holy` contain
  Thief, Warrior and Assassin folders: 30 character equipment sets.
- Each set includes `rigging_original.png`, `rigging_layers.psb`, and Unity `.meta` files.
- `Assets/Art/ChihuahuaEquipmentThemes/Reference/character_base.png` is the anatomy reference.
- `Reference/character_base.psb.meta` contains the user's 11-bone skeleton and per-part bone bindings.
- `Reference/Player.prefab` is the user's reference prefab. Preserve it.
- `Assets/Art/ChihuahuaGameUI` contains the related game UI artwork.

For a cloud task on an older checkout, fetch `origin/main` and integrate the latest
main revision while preserving that task's own edits. If sparse checkout is enabled,
include `Assets/Art` and `Assets/Art.meta`. A local Windows path is not needed.

PSB image layers and Unity rig data are separate: the `.psb` stores the seven raster
parts, while the accompanying `.psb.meta` stores bones, mesh geometry, weights and
sprite IDs. Always carry them together. Do not regenerate or replace these `.meta`
files with blank importer templates; that would erase the user's rig setup.

Cloud rig generation uses Unity 6000.3.8f1, 2D Animation 13.0.4 and PSD Importer 12.0.1.
The workflow `.github/workflows/chihuahua-rigging.yml` copies only the PSBs and their
metadata into a disposable cloud project, copies the reference skeleton, and calls
the same outline, triangulation and weighting implementations used by Auto Geometry.
It never opens or controls the user's local Unity editor.

Settings: Outline Detail 10, Alpha Tolerance 10, Subdivide 0, Generate Weights enabled.
See the [Unity Auto Geometry documentation](https://docs.unity3d.com/kr/Packages/com.unity.2d.animation@13.0/manual/SkinEdToolsShortcuts.html).
Each sprite uses its own texture outline. Reference global bone transforms are copied
unchanged; per-sprite root offsets compensate for differing sprite crop positions.
Reference meshes are not copied onto differently shaped artwork.

Execution and validation status is recorded separately in `CHIHUAHUA-RIGGING-VERIFICATION.md`.
