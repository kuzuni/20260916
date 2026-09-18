# Gameplay capture correction review — 2026-09-18

The original 209 PNGs from hosted Unity 6000.3.8f1 run [35315371447](https://github.com/kuzuni/20260916/actions/runs/35315371447) were downloaded without image changes through evidence packaging run 35317119028. Original evidence is under C:/Users/user/.codex/artifacts/moonlit-gameplay-restoration-20260918/run89/{captures-9x16,captures-9x19,captures-reports}. Its PlayMode summary records 133 passed, 0 failed, 0 skipped. The subsequent source run 35316533151 also completed both hosted jobs successfully.

Actual full-resolution inspection covered both portrait ratios: main and world HUD, comparison/details/catalog/forge controls, collection/probability/XP/results, pass/offline/chat/dungeons/shop, and skill samples spanning all ten eras. Standalone egg and horseshoe currency silhouettes, restored pass/offline structures, empty-slot pictograms, grade frames, full-width segmented forge progress, probability labels and hidden disabled affix choices were confirmed.

Visible defects corrected in this revision:
- Reward absorption used the centered toast design's coordinates for a top-left-anchored effect layer, shifting particles and amount text left by half the screen. Both endpoints now use the effect layer's coordinates; dungeon rewards originate at the reward frame's center.
- World HP bars overlapped fixed buttons during entrance and each other at melee contact. Bars appear at home arrival with small outward offsets; large HP values use the existing compact number format.
- Skill previews begun during entrance could leave the food buff behind the actor. Effects now follow the actual motion transforms.
- A comparison card's star and new-item label overlapped. Only the new card height and label position were adjusted.
- Unrequested local-implementation labels were removed from chat/shop.

Regression tests exercise effect world-space endpoints across design pivots/scales, entrance/contact HUD placement and moving buff attachment. The capture harness is being extended to finish summon reveals and cover pet/mount tabs, all currency payment modes and enabled/disabled affix filters. These new changes still require a fresh hosted run and visual inspection; this report is not final acceptance.

Future validation uploads separate original 9:16, 9:19 and general/report artifacts directly. This avoids the connector's 512 MiB artifact limit without resizing, re-encoding or omitting images. The old packaging workflow remains available manually for historical oversized bundles.

No local Unity editor was run. Enemy Player-prefab reuse, dummy PvP, deferred pet/mount character art and the three demo accessory slots remain as requested.
