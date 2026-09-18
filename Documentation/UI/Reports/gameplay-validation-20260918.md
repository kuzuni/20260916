# Gameplay validation — 2026-09-18

Final gameplay source: `df703d4a6eff0447bae1da68a36a44a350b4f98f`.
Hosted Unity version: **6000.3.8f1**. No local Unity editor was launched or controlled.

## Runtime tests

The substantive gameplay revision `47fce2c` passed **106 PlayMode cases, 0 failed, 0 skipped, 0 inconclusive** in [run 35291153905](https://github.com/kuzuni/20260916/actions/runs/35291153905). Its evidence is artifact `10526960859`, `Artifacts/TestResults/summary.txt` and `playmode-results.xml`.

The final source passed **108 PlayMode cases, 0 failed, 0 skipped, 0 inconclusive** in [run 35293032778](https://github.com/kuzuni/20260916/actions/runs/35293032778), artifact `10526609814`. The two added production-path tests passed using the actual shipped asset catalog: inactive full-factory activation/reactivation, and `MainScreenBootstrap.Awake`/idempotent `Build`. Both require visible health text and actual animation-event damage with no unexpected logs.

Tests exercise actual runtime UI actions, animation events and Player sprite swaps as well as game rules: equipment rarity/per-grade levels/affixes; equip-swap-sell and empty slots; 22-item overlapping forge hands; interrupted automatic-sale save/restore without duplicated payment; per-batch keep goals; forge upgrades/Korean midnight skips; collection fragments/permanent unlocks and independent summon XP above level 100; dungeon entry, refill, sweep and callback/refund boundaries; 15-round timeout, fixed skill damage at projectile arrival, stage/dungeon return; production bootstrap activation/reactivation and actual first-turn damage; continuous rewards, pass, arena, shop quantities and modal navigation.

## Rendered evidence

The final source passed the graphics job in [run 35293032778](https://github.com/kuzuni/20260916/actions/runs/35293032778), artifact `10527805077`: **85 runtime PNGs**. `Verification.txt` passes all 30 modal routes at 9:16 and 9:19, six viewport/safe-area cases, live production-bootstrap combat with animation-event attacks, single-line large power values, and the timed forge sequence. It includes all three primitive skill flight/impact phases at both aspect ratios.

Local evidence: `C:/Users/user/.codex/artifacts/moonlit-gameplay-20260918/` contains the 85 original captures, `Verification.txt`, `test-summary.txt`, and `playmode-results.xml`. The battle captures intentionally use seeded high-tier equipment to test large values; fresh player defaults remain stage 1, forge level 1 and 1,000 hammers.

The hosted Editor log also contains an unrelated UnityEditor.Search.SearchDatabase startup-indexing exception; it has no game-code frame. The graphics harness completes successfully afterward. No inactive-root coroutine error remains.

The capture harness seeds equipment for comparison/details and real ticket summons for 5- and 10-card results. Primitive effects are captured in flight and at impact at fixed simulation frames, at both aspect ratios. These are hosted runtime renders, not screenshots assembled as UI.

Manual full-resolution review covered populated equipment comparisons/details, automatic-forge settings including the lower scroll region, collection/result/probability dialogs, profile, shop, offline reward, pass, arena, combat and skill effects. Resolved issues included profile stat overlap, empty-slot white boxes, collection label outline, probability footer spacing, summon-new label wrapping and the 9:19 combat render density.

The three primitive effects are review prototypes. On 9:16, the combat status overlay can cover part of a character head or effect trail; 9:19 has more separation. The offline reward label wraps onto two lines but remains above the health label. These visual polish limitations do not prevent input or combat progression.

Typography REVIEW lines are layout diagnostics, not an assertion that every overflowing Text component visibly clips; images were inspected separately. Safe areas/device cutouts were simulated. Physical mobile hardware was not tested.

## Integration and scope

The coordinator integrated reviewed progress into main and fast-forwarded the original local checkout during development (PRs 64–69), preserving a clean user checkout. `Assets/_Recovery` and the original Art sources were not changed.

See [game rules](../../Gameplay-20260918.md) and [Korean quickstart](../../Gameplay-Quickstart-ko.md). Gear statistics provide the shared six-slot baseline for skills, pets and mounts. All six currencies and progress are persisted locally. The three primitive effects use separate 2D sprites, ParticleSystems and TrailRenderers; seven Animator motions deliver damage through AnimationEvents.

Deferred by user: pet/mount illustrations, additional tier-specific skill effects, emblem/wings/spirit functionality. Arena opponents remain dummy data; shop buttons grant local demo items without payment integration. Balance choices left unspecified by the user are documented in the shared rules/module reports.

Earlier failed validation exposed legacy demo assertions, a profile/arena null reference, JsonUtility phantom equipment, catalog button identity, frame-timing assumptions in the capture harness, and an inactive-root normal-combat startup failure missed by isolated fixtures. They were corrected and rerun; no earlier failed run is presented as a pass.
