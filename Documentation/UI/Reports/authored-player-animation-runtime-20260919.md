# Authored Player animation runtime — 2026-09-19

The battle previously disabled the Animator inside Player.prefab and assigned the generated PlayerCombat controller to an outer object. Editing the native prefab clips therefore did not change live combat. [PR104](https://github.com/kuzuni/20260916/pull/104) removes that replacement: both combatants now use the Animator, assigned controller and event relay on Motion/PlayerRig. Motion only places the actor.

The user's prefab and seven edited native clips from a903491f2551d6f5ff6962e73858993e502fd508 are preserved, including position, rotation, scale, clip lengths and event times. Basic/Weak/Strong currently authorize impact at 0.5 seconds. Actions wait for the authored state to finish instead of a fixed 0.8-second cutoff; combos start from the actual authorized event. Builders validate assigned animation assets without regenerating native or legacy clips.

## Hosted validation

- Unity 6000.3.8f1 [run127](https://github.com/kuzuni/20260916/actions/runs/35381684036), source40b4357821e1251609f31d3acfa1e20f9760604c, tests job105719080383: **291 passed, 0 failed, 0 skipped, 0 inconclusive**. Downloaded summary and XML agree (artifact10563202045).
- Tests compare actual player/enemy native bone poses against the assigned prefab across seven states, preserve builder input bytes, and verify a 1.6-second override clip with its later 1.1-second impact. They also cover late event authorization without pre-event damage.
- [Run126](https://github.com/kuzuni/20260916/actions/runs/35380508521), sourcec352825ab13c387d018af947267291fc97b42977, graphics job105715304827: **success**, downloaded Verification.txt **PASS**. Run127 has identical runtime/editor/art sources; its only source difference is four test-fixture initialization lines in ReferencePlayerAnimatorTests.cs.
- Run126 tests were 290 passed / 1 failed: the new long-clip fixture omitted combat-state initialization after bypassing normal encounter creation. [PR105](https://github.com/kuzuni/20260916/pull/105) initializes that fixture without weakening assertions. Run127 proves the corrected test.
- Run127's duplicate graphics job was still running when this report was written. Graphics evidence here is explicitly run126, not an inferred run127 pass.

Evidence roots:
`C:/Users/user/.codex/artifacts/moonlit-gameplay-restoration-20260918/run127/tests/Artifacts/TestResults/{summary.txt,playmode-results.xml}`
and `C:/Users/user/.codex/artifacts/moonlit-gameplay-restoration-20260918/run126/`.

## Actual image review

Downloaded run126 reports and normal aspect bundles contain 139 PNGs per ratio plus 3 general images. **30 unique originals** were opened: 8 basic/strong foot-shadow, hit-flash and skill-HUD views; 18 mounted idle/basic/notch views; and 4 wave before/after views. Both 1080x1920 and 1080x2280 were inspected. The newly authored poses visibly play; reviewed mounted poses show no broken native binding, disconnected body or viewport clipping. Wave fixtures preserve player HP140/200 and position.

This review does not claim every capture or the separate skill-phase bundles were inspected. Wave-after fixtures show an enemy-head/round-label overlap at9:16 and a lingering white20 over part of the enemy HP at both ratios. The fixture manually steps entrance and renders immediately; these captures alone do not establish normal-play duration of that overlap. Existing pet/mount occlusion and the notched reward-dot overlap also remain. User-authored poses were not altered to conceal those limits.

The successful graphics log contains a UnityEditor.Search.SearchDatabase startup ArgumentOutOfRangeException, followed by runtime PASS; no gameplay stack or compile error was found. Safe areas are Editor simulations, not physical-device testing.

## Delivery and preservation

Runtime and test fixes are merged through PR104/105; local main was fast-forwarded to dd465295e8d5876b13928a9c5653e3ad6331439b. SHA256 comparisons confirmed all21 user prefab/native controller/clip/meta/Recovery files unchanged across delivery. The diff of those paths against the user's a903491 commit is empty. No local Unity editor was run or controlled, and the paused automation remains paused.

This report and the associated guidance update are documentation-only. Enemy visuals still reuse Player; companion rigs remain archived and current pets/mounts use whole PNGs. This certifies the authored Animator connection and named checks, not removal of all previously documented visual limitations.
