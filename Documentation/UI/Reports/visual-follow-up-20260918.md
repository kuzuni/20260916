> Current status: [latest validation and remaining visual limitations](runtime-validation-20260919.md). The historical findings below describe their named revisions.

# Visual review follow-up — 2026-09-18

This is an in-progress record, not final acceptance.

## Proven evidence

- Hosted Unity6000.3.8f1 run109 (35335954005), source975ac03731ef97b178762ea28123f755d48f57c9: 210passed,0failed,0skipped,0inconclusive. Native221vertex/1152index companion sprites, UV persistence, real bone deformation and saddle attachment passed.
- Its generated companion runtime delivery was committed in PR94: six prefabs,48sprite assets,30clips,six controllers and catalog. Source PNGs and the user's Player prefab were not overwritten.
- Run112 (35337189659), source601b81db0b3358cebe3920b1d96a198225abd271: downloaded summary confirms209passed,0failed,0skipped,0inconclusive. This covers instantaneous popup presentation and dedicated basic-attack dust/sword arc, before the new rhythmic multi-hit revision.

## Actual visual defects discovered

Run109 original captures under `C:/Users/user/.codex/artifacts/moonlit-gameplay-restoration-20260918/run109/captures-9x19` were opened, not inferred from test assertions.

- `Runtime-companion-rig-1-0-idle-9x19.png`: primitive child's arm/leg connection gaps and exposed cut ends.
- `Runtime-companion-rig-1-1-idle-9x19.png`: sabertooth neck gap and visible tail seam. Review also identified attachment seams in the other four samples. Mesh deformation tests do not certify assembled illustration alignment.
- `Runtime-shop-diamond-absorption-9x19.png`: wallet100 and claimed offer render, but reward particles and +100 are absent. The existence-of-Image assertion is insufficient.
- `Runtime-dungeon-1-absorption-9x19.png` does show ticket particles and +4; the rendering failure needs shop-specific diagnosis.

Companion pivot/attachment corrections are stopped at the user's explicit request; current rigs remain as-is. Shop reward visibility is being corrected and requires new hosted captures. The user's latest skill direction also requires new evidence for every skill's choreography and real3/5hit sequences. No final visual pass is claimed.

## Remaining acceptance

Validate committed companion delivery without rebuilding over it, all revised combat tests, actual captures at9:16/9:19, preserved original routes and simulated safe areas. Preserve the user's local Player.prefab edit and Recovery scene. Higher-era companion art remains deferred; enemy still uses the Player prefab, arena opponents remain dummy data, demo accessory slots/shop retain the requested local behavior. Physical devices have not been used.

Latest user direction: stop companion rigging and leave the current committed rigs as-is. Known visual attachment gaps remain deferred; no rig/pivot/bone/part edits or regeneration are authorized. This is not a visual acceptance of the rigs. Skill choreography and general UI work continue.
