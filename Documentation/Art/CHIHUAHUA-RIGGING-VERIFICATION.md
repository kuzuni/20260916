# Chihuahua rigging verification — 2026-09-18

Scope: 30 equipment PSBs (01_Primitive–10_Holy, three classes each). The user's
Reference/character_base.psb provides the 11-bone skeleton. Each equipment PSB has
seven independent meshes: weapon, head, torso, two arms and two legs.

Cloud execution: [GitHub Actions run 35283090035](https://github.com/kuzuni/20260916/actions/runs/35283090035).
Input revision: `49225a2`. Status: **PASS**. All 30 files were generated, saved and
synchronously reimported successfully in cloud Unity. The independent metadata
audit also passed: 210 meshes, 7,358 vertices and 6,938 triangles. The copied
global bone values have zero numeric difference from the user's reference.

Evidence in the repository:

- `Artifacts/ChihuahuaRig-20260918/verification.json`: Unity's per-file/per-layer report.
- `Artifacts/ChihuahuaRig-20260918/independent-audit.json`: metadata hashes and independent checks.
- `Artifacts/ChihuahuaRig-20260918/all-meshes.jpg`: all 30 sets with mesh and bone overlays.
- `Artifacts/ChihuahuaRig-20260918/10_Holy-Assassin-mesh.jpg`: full-size example.

The overlays were visually reviewed. Existing asset GUIDs and all 210 sprite IDs
were preserved, including the Holy IDs that the local importer created during
the cloud job. Reference metadata and all PSB image bytes remained unchanged.
Original metadata is backed up locally under `Artifacts/ChihuahuaRig-20260918/`.

The disposable Linux project uses Unity 6000.3.8f1, 2D Animation 13.0.4 and PSD
Importer 12.0.1. No local Unity editor was opened, run or controlled.

The generator copies bone names, GUIDs, hierarchy, rotations, lengths, colors and
document-space positions. Sprite-local root coordinates account for different
image crop positions. It retains the existing asset GUIDs and sprite IDs.

Geometry uses Unity's OutlineGenerator and Triangulator with Outline Detail 10,
Alpha Tolerance 10 and Subdivide 0. Two-bone limbs use Unity's bounded biharmonic
weight generator; single-bone parts have rigid weight 1. Reference mesh geometry
is not reused for different equipment silhouettes.

Checks include seven matching parts, 11 skeleton bones, nonempty indexed meshes,
valid vertex and bone indices, finite nonnegative weights summing to one, and
bone/geometry persistence after synchronous Unity reimport. A separate Python
audit reads the emitted YAML metadata, compares reference bones and existing IDs,
checks all 210 meshes, and renders mesh/bone overlay images.

Earlier attempts stopped on verification before any metadata was applied locally.
Asset garbage collection during metadata reserialization could unload an importer
whose edits were not yet dirty/saved. The generator now saves first, pins the
importer during the operation and shares/releases its temporary readable texture.

This validates asset import and rig data generation, not runtime animation motion,
equipment swapping or a full project player build. Photoshop application testing
was not performed.
