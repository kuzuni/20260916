# Moonlit 24 UI — coordinator status

Updated 2026-09-16. This file replaces older chronological notes; Git history preserves those notes.

## Current source of truth

- Repository: https://github.com/kuzuni/20260916 (private).
- Draft PR: https://github.com/kuzuni/20260916/pull/1 . UI implementation is NOT merged into main.
- Remote branch: codex/ui-24-integration.
- Latest pushed commit: 50d3fe073ddcae49b89bbac705b18bb1d0d9c099.
- Coordinator checkout: C:/Users/user/.codex/worktrees/moonlit-ui-24-integration (clean at checkpoint).
- Main project Assets were not modified by the coordinator. No local Unity editor was run or controlled.
- Read the integration branch's Documentation/UI/INTEGRATION-CHECKPOINT.md, ART-GENERATION.md, PROGRESSION-STATE-REVIEW.md, SOCIAL-VISUAL-REVIEW.md and CI-35077607384.md for implementation/evidence details.

## Actual hosted verification

Unity version: 6000.3.8f1. GitHub Actions secrets UNITY_EMAIL, UNITY_LICENSE and UNITY_PASSWORD are user-authorized; use workflow secret expressions only. Never retrieve/read/print their values.

- Baseline login/license/Linux player build: SUCCESS, run 35067009693, job 104699606811. This baseline did not include the new 24 routes.
- First 24-route CI run 35073553695: FAILED before tests, Unity license TimeStamp validation failed. No test artifact, graphics skipped. Do not count as a compile/test pass.
- Replacement run https://github.com/kuzuni/20260916/actions/runs/35077607384 at commit 3d6dd2d7237c38fc97ca2cd74c377c517fabafeb: compile/PlayMode job 104733618485 SUCCESS. Downloaded artifact 10439357413 confirms 10 passed, 0 failed, 0 skipped, 0 inconclusive. Graphics capture job 104737565570 was still running at this checkpoint.
- Latest queued validation: https://github.com/kuzuni/20260916/actions/runs/35079253091 at commit 50d3fe0. It must validate newer art/progression/social changes; no pass claimed yet. Intermediate pending run 35078727676 is superseded by this newest queued update.
- Workflow now preserves active runs (cancel-in-progress false) and queues the latest pending PR update. Test entry uses the proven unity-builder activation path plus Editor Test Framework API; actual NUnit evidence required. Static git diff --check passes but is never a Unity substitute.
- Downloaded test evidence: C:/Users/user/AppData/Local/Temp/moonlit-cloud-review/ci-35077607384/unpacked. Summary and XML inspected; no account secret values retrieved.

## Implementation progress and gaps

All 24 routes are assembled and connected to main/nav entries with runtime-only construction. Coordinator reviewed cloud drafts and corrected page input/close/back, pending craft cost and duplicate/stale decisions, one-time offline wallet reward, auto settings state, quick-equip accumulation, actual skill equip/upgrade and summon state, system-back summon lockout, social navigation clearance, ranking/PvP/opponent raycast targets and modal-interior hit blocking. Newly added tests still need newest queued CI.

All four dungeon source paintings are now generated with built-in image_gen and stored under Assets/DarkFantasyUI/Resources/Moonlit/Dungeons: HammerThief-v1, GhostVillage-v1, Invasion-v1, ZombieRush-v1. Unique metadata, exact prompts and inspection notes are committed. List/detail crop proportionally; images, reusable frames, labels and buttons remain separate. No image generation request remains running.

Still incomplete: original-reference visual fidelity across 24 screens, bespoke skill/reward/shop/avatar/crest illustrations, control behavior/layout gaps revealed by real captures, all-screen safe-area and interaction acceptance. Placeholders/glyphs are not finished artwork. Runtime capture code requests all 24 routes at both aspect ratios, but capture existence alone is not visual acceptance. Do not merge before reviewing actual results and fixing failures.

## Cloud work already reviewed and applied

Environment: kuzuni/20260916, ID 6aaa3ea9e7748191a9023a62689d6c2b. Repository-backed Codex Cloud, not local workers. All following tasks are complete and their diffs applied. DO NOT APPLY THEM AGAIN. No delegated task is currently executing.

| Assignment | Task ID |
|---|---|
| Foundation | task_e_6aaa3f284b848329a2f503179ed3c3c2 |
| Forge | task_e_6aaa3f42b96c8329b8be80604ac2c898 |
| Progression | task_e_6aaa3f47b9e48329981d5b65dde1ce30 |
| Social | task_e_6aaa3f4ca5b08329aaaaf1935e0165a3 |
| Entry/CI integration | task_e_6aaa4b7d4a2c8329938a738ce3607eff |
| Initial local demo state | task_e_6aaa524857dc8329b7f0463cd99f179f |
| Skill/summon live state | task_e_6aaa5c620df8832988a95fa79042550e |
| Social sprites/layout | task_e_6aaa5cb3f8608329a8f710daae4691ff |

Task URLs: https://chatgpt.com/codex/tasks/TASK_ID . Patches are preserved under C:/Users/user/AppData/Local/Temp/moonlit-cloud-review. The last progression patch required a clean three-way merge to preserve independently added dungeon art; final branch contains both.

Earlier ChatGPT Work conversation 6aaa3c5a-69dc-83ee-85cb-a46cbbf95790 was stopped to avoid duplicate foundation work. Do not merge its codex/ui-foundation branch/9943238 alongside current implementation. One consistent Scripts/Screens.meta is already preserved.

## Next heartbeat

1. Inspect graphics job 104737565570 and newest run 35079253091. Fetch actual NUnit results/logs/capture artifacts and repair failures on integration branch.
2. Compare per-screen captured PNGs to all originals in Documentation/UI/References and filenames in reference-manifest.json. Verify 9:16, 9:19, safe areas, top-only modal/back input, interior backdrop blocking, parent tab/scroll, shop quantities and actual control states.
3. Generate missing illustrated assets with built-in image_gen, keep slots/icons separate, improve unfinished reference layouts. Assign bounded independent follow-ups in cloud if useful; avoid duplicate work.
4. Push reviewed fixes to integration PR, allow hosted validation to finish, record checkpoints. Merge only after the 24-page acceptance requirements actually pass. Pause automation only on actual completion.

Automation moonlit-24-ui is active every 15 minutes. Keep unchanged/non-actionable monitoring quiet; notify on meaningful progress, completion, failure or required user action. Use limits/access failures as explicit blockers, never fabricated success. No local Unity/MCP/self-hosted runner/Library/Moonlit.command use. Do not touch Assets/_Recovery.
