> Superseded runtime connection: [authored Player animation verification](authored-player-animation-runtime-20260919.md). PR104 removes the wrapper Animator; the reports below retain their historical source/run details.

# Runtime validation — 2026-09-19

The Player reference prefab contains its Animator, native controller, seven clips and exactly-once CombatAnimationRelay. Hosted validation loads the committed assets without rebuilding them. The first two skill eras use the six requested sequences, collection details use one Equip button, and selecting equipment preserves its rarity frame. The latest runtime snapshot is main357ec7a76a860f1ddf6add700730d00f20497a30, tree88fa4f290301ea08f39fc4184aaa0f739db57e48.

## Tested source and evidence

Hosted Unity6000.3.8f1 [run121](https://github.com/kuzuni/20260916/actions/runs/35366944853) tested a11a51218c571fcb6ab24ee3f4ac8c69a09bd63b, tree1e43844146d8c7b27a2042ef7fac09a8263ada4b (identical to main387781985e2dae71067d62e60fa286ff57f69924 before the isolated anvil correction). Both jobs succeeded: tests105671595825 and graphics105671595526.

Downloaded summary and XML agree: **285 passed,0 failed,0 skipped,0 inconclusive**. These include12 committed Player Animator cases, actual mounted geometry for72 combinations, food-pulse head/profile limits, combo totals, collection costs/500-result scrolling/ascension, and modal input/parent restoration. No local Unity editor was opened or run.

Evidence root: C:/Users/user/.codex/artifacts/moonlit-gameplay-restoration-20260918/run121/. Test evidence is under tests/Artifacts/TestResults/{summary.txt,playmode-results.xml}; graphics Verification.txt is under captures-reports. Seven original bundles contain **533 PNGs**:139 regular views per ratio,66 early-era phases per ratio,60 later-era phases per ratio, and3 general images. Both ratios are1080x1920 and1080x2280; cutouts were simulated.

## Actual image review

Review covered **110 unique originals**, not every generated image:60 first-two-era skill phases,20 companion/formation views,16 shop/forge/pass/power views and14 main/detail views.

- Chicken/apple pulse three times, disappear and heal with green aura; bones form an eight-item ring and contact the head sequentially; five curved arrows, one arcing rock with rock fragments, and the sword/slash render. Contact budgets were120 across bone8/arrows5 and288 for rock1/sword1 in the capture fixture.
- Main profile no longer clips the food's upper edge. Player/enemy remain on opposite sides with separate HP bars; mount saddle, feet, tail and pass clearance are visible. Three equipped skill-slot clearance and Strong poses are automated geometry coverage; mounted capture fixtures themselves have no active skill slots.
- Shop captures visibly show gem particles, +100 and wallet100 at both ratios. Later independent forge/pass views have no leaked shop particles. Power feedback shows green UP220/red DOWN160. The pass opens at first claimable225 and fills to237 between235/240.
- Collection detail descriptions no longer overlap the owned-effect heading and have only one Equip button. Equipment detail frames retain rarity color. Native Player Animator details and asset paths are in [its delivery report](player-prefab-animator-20260919.md).

## Anvil correction and latest validation

Run121's procedural burst was invisible despite the particle-count assertion. Run122 failed compilation in an optional mesh diagnostic before tests ran; that diagnostic was removed. Run123 passed285 tests but its actual ON/OFF render check exposed a missing CanvasRenderer. [PR101](https://github.com/kuzuni/20260916/pull/101) requires that renderer; [PR103](https://github.com/kuzuni/20260916/pull/103) selects the exact mesh callback in the regression test.

Hosted [run125](https://github.com/kuzuni/20260916/actions/runs/35372967347), source61ec406a7357501eddfa48ffdb956cdea4d53860, has the same tree as the latest runtime snapshot above. **285 tests passed,0 failed/skipped/inconclusive**, verified from summary and XML (artifact10559611977). Tests105691025706 and graphics105691026908 both succeeded. Verification.txt is PASS. Only the isolated anvil runtime component, its test and capture verification differ from the run121 baseline.

All six new anvil originals were opened: three strikes at each ratio visibly show golden flash/streaks by the hammer head. Same-frame burst ON/OFF comparisons measured1210/1114/1209 changed pixels at9:16 and1069/966/1062 at9:19, each above400. The previously invisible procedural burst is resolved.

New evidence root: C:/Users/user/.codex/artifacts/moonlit-gameplay-restoration-20260918/run125/. Report artifact10558923753 is in captures-reports; six selected anvil originals from artifacts10559348387/10559502964 are in captures-9x16 and captures-9x19. Only those six new graphics originals were extracted/reviewed for the changed feature; the broader110-image manual review remains the run121 baseline above. This does not claim every run125 image was manually reviewed.

## Remaining visual limits

On9:16, some sword/rock/bone/arrow flight art passes behind stage/wave/round UI. The lower food edge partly passes behind world HP at both ratios. Trailing pets are intentionally behind the player/mount, but the front cub's head can be hidden by the mount, the rear boar face by the wolf, and the reward button/red dot can cover parts of pets in notched views. These are known layering limits, not a claim of unobstructed art or pixel-perfect acceptance.

The graphics log contains one UnityEditor.Search.SearchDatabase startup ArgumentOutOfRangeException followed by runtime PASS; no gameplay stack or compile error was found in the successful run121 and run125 graphics logs. DOTween increased capacity during500-card fixtures. No physical devices were tested.

Enemy characters reuse Player; arena opponents and shop purchases remain local demonstrations; accessory slots and higher-era companion artwork remain deferred. Previous separated-part companion rigs remain archived with known seams and are not used by live whole-PNG companions. User Recovery scene/metadata were preserved, and paused automation remains paused.
