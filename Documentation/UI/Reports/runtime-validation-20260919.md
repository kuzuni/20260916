# Runtime validation — 2026-09-19

The Player reference prefab contains its Animator, native controller, seven clips and exactly-once CombatAnimationRelay. Hosted validation loads the committed assets without rebuilding them. The first two skill eras use the six requested sequences, collection details use one Equip button, and selecting equipment preserves its rarity frame. All are present in local main387781985e2dae71067d62e60fa286ff57f69924.

## Tested source and evidence

Hosted Unity6000.3.8f1 [run121](https://github.com/kuzuni/20260916/actions/runs/35366944853) tested a11a51218c571fcb6ab24ee3f4ac8c69a09bd63b, tree1e43844146d8c7b27a2042ef7fac09a8263ada4b (identical to the named local main tree). Both jobs succeeded: tests105671595825 and graphics105671595526.

Downloaded summary and XML agree: **285 passed,0 failed,0 skipped,0 inconclusive**. These include12 committed Player Animator cases, actual mounted geometry for72 combinations, food-pulse head/profile limits, combo totals, collection costs/500-result scrolling/ascension, and modal input/parent restoration. No local Unity editor was opened or run.

Evidence root: C:/Users/user/.codex/artifacts/moonlit-gameplay-restoration-20260918/run121/. Test evidence is under tests/Artifacts/TestResults/{summary.txt,playmode-results.xml}; graphics Verification.txt is under captures-reports. Seven original bundles contain **533 PNGs**:139 regular views per ratio,66 early-era phases per ratio,60 later-era phases per ratio, and3 general images. Both ratios are1080x1920 and1080x2280; cutouts were simulated.

## Actual image review

Review covered **110 unique originals**, not every generated image:60 first-two-era skill phases,20 companion/formation views,16 shop/forge/pass/power views and14 main/detail views.

- Chicken/apple pulse three times, disappear and heal with green aura; bones form an eight-item ring and contact the head sequentially; five curved arrows, one arcing rock with rock fragments, and the sword/slash render. Contact budgets were120 across bone8/arrows5 and288 for rock1/sword1 in the capture fixture.
- Main profile no longer clips the food's upper edge. Player/enemy remain on opposite sides with separate HP bars; mount saddle, feet, tail and pass clearance are visible. Three equipped skill-slot clearance and Strong poses are automated geometry coverage; mounted capture fixtures themselves have no active skill slots.
- Shop captures visibly show gem particles, +100 and wallet100 at both ratios. Later independent forge/pass views have no leaked shop particles. Power feedback shows green UP220/red DOWN160. The pass opens at first claimable225 and fills to237 between235/240.
- Collection detail descriptions no longer overlap the owned-effect heading and have only one Equip button. Equipment detail frames retain rarity color. Native Player Animator details and asset paths are in [its delivery report](player-prefab-animator-20260919.md).

## Remaining visual limits and follow-up

The procedural anvil burst was **not visibly established** in run121 despite the object-count assertion. [PR101](https://github.com/kuzuni/20260916/pull/101) adds same-frame burst ON/OFF pixel comparison and a registration frame. Its first run122 failed compilation in an optional mesh diagnostic before tests ran; the diagnostic was removed and replacement run123 is pending. This report does not claim final anvil acceptance.

On9:16, some sword/rock/bone/arrow flight art passes behind stage/wave/round UI. The lower food edge partly passes behind world HP at both ratios. Trailing pets are intentionally behind the player/mount, but the front cub's head can be hidden by the mount, the rear boar face by the wolf, and the reward button/red dot can cover parts of pets in notched views. These are known layering limits, not a claim of unobstructed art or pixel-perfect acceptance.

The graphics log contains one UnityEditor.Search.SearchDatabase startup ArgumentOutOfRangeException followed by runtime PASS; no gameplay stack or compile error was found in run121. DOTween increased capacity during500-card fixtures. No physical devices were tested.

Enemy characters reuse Player; arena opponents and shop purchases remain local demonstrations; accessory slots and higher-era companion artwork remain deferred. Previous separated-part companion rigs remain archived with known seams and are not used by live whole-PNG companions. User Recovery scene/metadata were preserved, and paused automation remains paused.
