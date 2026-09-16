# Moonlit 24 UI — coordinator status

Updated 2026-09-16, 10:07 UTC. Implementation is unfinished; automation remains active.

## Latest skill artwork checkpoint — 2026-09-16 10:31 UTC

- **Verified hosted regression pass:** run 35082860621, revision 353d8ec14957892e93e63254ebd7e440aadabf6d, job 104750728936 SUCCESS. Downloaded artifact 10441188409; NUnit XML and summary agree **17 passed, 0 failed, 0 skipped, 0 inconclusive**. The prior empty-raycast failure is resolved. Local evidence: C:/Users/user/AppData/Local/Temp/moonlit-cloud-review/artifact-10441188409/unpacked/Artifacts/TestResults. Graphics job 104754290100 also succeeded; its capture artifact still needs downloading/review. This revision predates profile and skill art additions.
- Run 35083318972 (profile-art revision d45e4ef) has started; compile/PlayMode job 104757861148 was running. Inspect actual results next.
- **PR #4 merged and original local project synced:** https://github.com/kuzuni/20260916/pull/4 , merge d9029988e5b81b1a4f0888f8a1e6e68c2b78203c, reviewed source d7caf441af96db23958b83f5dcb77be3af27654a. Four preexisting dungeon metadata changes were preserved. No local Unity was run/controlled.
- Original reference 19 and older runtime skill capture were compared at full resolution. The shared SkillSlot's font glyphs/flat square art have been replaced with 12 generated illustrated symbols and a separate reusable empty gold ring. Collection, equipped, detail, probability detail and summon result share these sprites. Meteor/bomb/sealed-power now have distinct art indices.
- Built-in image_gen generated SkillIcons-v1.png (1448x1086, 4x3 cells) and SkillRing-v1.png (1254x1254). Source images inspected; actual ARGB alpha=0 at outer corners and ring center. No screenshot UI, labels or frames baked into icon sprites. Separate transparent button hit surface and non-raycastable artwork; level/star/ownership/progress remain live UI.
- Assets: Assets/DarkFantasyUI/Resources/Moonlit/Skills/. Exact prompts and remaining gaps: Documentation/UI/SkillIllustrations-v1-prompt.md. Source/test edits were made on isolated GitHub branch codex/skill-illustrations; generated assets staged by coordinator on codex/skill-art-review in the existing integration worktree.
- Added a meaningful hosted regression test for resource import, atlas orientation/distinct cells and reused sprites across collection/detail. git diff --check passed. **Newest CI run 35085271201 is in progress; no pass claimed for this new code/art.**
- Still open: latest visual review, reference's fuller skill roster (current demo has 12), ornate rails/buttons, currency art, companion/hero art and wider 24-screen fidelity/acceptance. Do not count source art as final rendered acceptance.
- No new delegated cloud task created; previous applied task IDs below remain unchanged. Next heartbeat should inspect newest CI 35085271201 plus 35083318972, retrieve actual XML/captures, repair failures and continue reference-based visual work.

## Previous integration checkpoint

PR #3 merged as 19bed74423867127202275de10e6dedde279dbf5. The original local project was fast-forwarded to this commit, preserving all four preexisting dungeon metadata modifications. Final reviewed source head was d45e4efa81af76482229d1ebd4a6e4cfe56f2266 (4eee17c code/art plus status and whitespace-only metadata normalization). git diff --check passed. Latest hosted run **35083318972** is pending behind active run 35082860621; it supersedes the intermediate pending runs below. No new runtime test pass is claimed.

## Delivery and current work

- Repository: https://github.com/kuzuni/20260916 .
- User explicitly wants intermediate progress merged into main and fast-forwarded into the original local C:/Users/user/Documents/GitHub/20260916 project. Do not wait for all 24 screens before integrating reviewed progress. Merging does not establish final acceptance.
- PR #1 implementation was merged as 9e1fb8c. PR #2 fixed System.Action/UnityAction compile errors in the summon return buttons; merged as 793143bab04888b0cbeb971c837a7f66b4d16042 and synced locally.
- This heartbeat: PR #3 https://github.com/kuzuni/20260916/pull/3 . Code/art revision 4eee17c3ca7f5f5d2c3965658daf587abbb795cb, based on current main. Source/test edits were committed on an isolated GitHub branch. Coordinator staged generated artwork in C:/Users/user/.codex/worktrees/moonlit-ui-24-integration, branch codex/profile-review, tracking origin/codex/modal-pointer-viewport.
- PR #3 matches the social test CanvasScaler to runtime width-based scaling, retains frame-edge pointer testing, and asserts the test point is on-screen and the frame hierarchy intercepts it. The former unscaled test canvas could place the left frame edge outside a small hosted Game View.
- Profile now uses the existing hood sprite instead of a crown coin and has a separate generated ruins painting below rank controls. Built-in image_gen source inspected; exact prompt and limitations in ProfileRuins-v1-prompt.md. Frame/text/buttons remain runtime objects; painting ignores raycasts and preserves aspect.
- Before sync, the original project had user/Unity modifications to the four dungeon PNG .meta files. Preserve them. No local Unity, MCP, command-file or self-hosted runner execution is allowed.
- Optional 20260916-preview is an older detached copy; the original project is the delivery target.

## Actual hosted verification

Unity 6000.3.8f1, GitHub-hosted Ubuntu. Secrets are referenced only by workflow environment; never retrieve account secrets.

- Baseline run 35067009693: login/license/Linux player build succeeded before the 24-route implementation.
- Run 35073553695: license timestamp failure before tests; no test pass.
- Run 35077607384, source 3d6dd2d7237c38fc97ca2cd74c377c517fabafeb: compile/PlayMode job 104733618485 SUCCESS. Artifact 10439357413 XML confirms 10 passed, 0 failed/skipped/inconclusive.
- Its graphics job 104737565570 also SUCCESS. Downloaded artifact 10439746706 and inspected Verification.txt: main layout and runtime interaction checks passed. Counted 54 PNGs: six main cases plus all 24 route keys in both aspect ratios, no missing files. Route capture checks establish construction, not exhaustive bounds/click acceptance.
- Runtime-profile-9x16.png was visually compared with full-resolution original 06-profile.png. Findings: incorrect coin avatar (fixed in PR #3), missing lower ruins painting (added), crude/incorrect frame ornamentation and unfinished styling (still open). Other captures require systematic visual comparison; 54 files existing is not 54 visually accepted screens.
- Run 35079253091, source 50d3fe0: compilation failed with the two summon callback CS1503 errors. Already fixed by PR #2.
- Run 35081094322, source 67524acd5a4ea6b8e1d03311076621b489658db5: compiled, tests actually ran. Artifact 10440159906 NUnit XML + summary: **16 passed, 1 failed, 0 skipped, 0 inconclusive**. Failed test: SocialScreenLayoutTests.PointerInsideModalFrame_DoesNotDismissThroughBackdrop, empty raycast list at line 112. Graphics skipped. PR #3 corrects the test viewport scaling; a fresh pass is not yet confirmed.
- Run 35082860621 at 353d8ec (test scaler fix): in progress at checkpoint.
- Run 35083086652 at 4eee17c (test fix plus profile art): pending behind active PR #3 run. Inspect these next, and use newest source results for acceptance.

Downloaded evidence:
- C:/Users/user/AppData/Local/Temp/moonlit-cloud-review/artifact-10439746706/unpacked (54 captures, Verification.txt).
- C:/Users/user/AppData/Local/Temp/moonlit-cloud-review/artifact-10440159906/unpacked/Artifacts/TestResults (17-test XML and summary).
- C:/Users/user/AppData/Local/Temp/moonlit-cloud-review/ci-35077607384/unpacked (prior 10-test evidence).

## Implementation and gaps

All 24 routes are connected with runtime-only construction, separate reusable slots/icons, page navigation and sibling modal stack. Existing fixes cover pending craft costs, stale/duplicate decisions, offline rewards, auto configuration, skill upgrade/equip, summon state, parent scroll preservation, navigation clearance, row raycasts and modal-interior interception.

Generated paintings: HammerThief-v1, GhostVillage-v1, Invasion-v1, ZombieRush-v1 and now ProfileRuins-v1. Source artwork is reviewed; latest rendered capture review remains outstanding.

Still incomplete: reference fidelity throughout 24 screens, skill/reward/shop/avatar/crest illustrations, decorative frames and actual control/layout gaps. Placeholder glyphs are not final. Required acceptance remains cloud compile, meaningful modal stack input tests, latest per-screen 9:16/9:19 safe-area captures, top-only input/back, no interior/backdrop click-through, parent tab/scroll restoration and exact shop quantities. Assets/_Recovery is untouched.

## Cloud tasks already applied — do not repeat

Environment ID 6aaa3ea9e7748191a9023a62689d6c2b. No delegated cloud task currently running.

| Work | Applied task ID |
|---|---|
| Foundation | task_e_6aaa3f284b848329a2f503179ed3c3c2 |
| Forge | task_e_6aaa3f42b96c8329b8be80604ac2c898 |
| Progression | task_e_6aaa3f47b9e48329981d5b65dde1ce30 |
| Social | task_e_6aaa3f4ca5b08329aaaaf1935e0165a3 |
| Entry/CI integration | task_e_6aaa4b7d4a2c8329938a738ce3607eff |
| Demo state | task_e_6aaa524857dc8329b7f0463cd99f179f |
| Skill/summon state | task_e_6aaa5c620df8832988a95fa79042550e |
| Social sprites/layout | task_e_6aaa5cb3f8608329a8f710daae4691ff |

Earlier ChatGPT Work foundation 6aaa3c5a-69dc-83ee-85cb-a46cbbf95790 was stopped. Do not merge its duplicate codex/ui-foundation branch.

## Next steps

1. Inspect runs 35082860621 and 35083086652, download actual NUnit and latest captures, repair remaining failures without weakening assertions.
2. Review all 24 latest captures against original reference PNGs at full resolution; fix visible layout/art deficits and strengthen per-route bounds/input evidence where current construction checks are insufficient.
3. Continue missing bitmap asset generation, preserve exact prompts, and integrate reviewed progress into main and the original project while preserving local modifications.
4. Record tested source commits and evidence. Keep automation active until all 24 screens meet actual acceptance; report meaningful progress, failure or required user action only.
