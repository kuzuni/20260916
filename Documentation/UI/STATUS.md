# Cloud dispatch status

Updated 2026-09-16 (Asia/Seoul).

## Shared baseline

- Repository: https://github.com/kuzuni/20260916, main.
- Main runtime UI baseline 76e42bf; 24-image reference pack/briefs 4187190.
- Dispatch baseline: 7b244bc (includes CI secret instructions).
- Images are byte-identical copies; original Korean filename requirements are in reference-manifest.json.
- No local Unity execution, UI automation or scene manipulation.

## Connected environment

Repository-backed environment: kuzuni/20260916.
Environment ID: 6aaa3ea9e7748191a9023a62689d6c2b.
Settings: https://chatgpt.com/codex/cloud/settings/environment/6aaa3ea9e7748191a9023a62689d6c2b
Universal container; agent internet retains default off.

## Submitted cloud tasks

All four submissions succeeded via codex cloud exec against this environment and main. Latest confirmed state: all four drafts are ready for coordinator review (2026-09-16 16:21 KST). These are actual repository-backed cloud jobs, not local worker processes. Task titles are service-generated; links identify each assignment.

| Assignment | Brief | Task |
|---|---|---|
| Shared canvases, modal stack, API, CI | Tasks/00-foundation.md | [Implement cloud CI for Moonlit UI](https://chatgpt.com/codex/tasks/task_e_6aaa3f284b848329a2f503179ed3c3c2) |
| Forge/equipment/offline/pass: 8 screens | Tasks/01-forge.md | [Implement Moonlit UI screens in cloud](https://chatgpt.com/codex/tasks/task_e_6aaa3f42b96c8329b8be80604ac2c898) |
| Skills/pets/heroes/dungeons: 7 screens | Tasks/02-progression.md | [Implement moonlit UI screens in cloud](https://chatgpt.com/codex/tasks/task_e_6aaa3f47b9e48329981d5b65dde1ce30) |
| Profile/settings/chat/ranking/PvP/shop: 9 screens | Tasks/03-social.md | [Implement moonlit UI screens in cloud](https://chatgpt.com/codex/tasks/task_e_6aaa3f4ca5b08329aaaaf1935e0165a3) |

Earlier ChatGPT Work conversation 6aaa3c5a-69dc-83ee-85cb-a46cbbf95790 received a stop-implementation/report-only instruction to avoid duplicate work. It is not the implementation source to merge.

## CI

The user confirmed existing GitHub Actions repository secrets UNITY_EMAIL, UNITY_LICENSE, UNITY_PASSWORD and explicitly authorized their use for CI. Only names were observed; values must never be retrieved, echoed, or stored in the repo. Reference them via workflow secrets expressions.

Secrets exist in GitHub Actions, not automatically in Codex Cloud's container. Unity account login and ULF license activation were verified successfully in an actual GitHub-hosted Unity 6000.3.8f1 run on 2026-09-16. Evidence: https://github.com/kuzuni/20260916/actions/runs/35067009693/job/104699606811#step:5:171 (masked successful login), #step:5:190 (entitlement activation), and #step:5:192 (ULF activation). This verifies the configured account credentials and license work together in CI. Secret values were not retrieved.

Workflow: .github/workflows/unity-license-check.yml, main commit 70f30a3. The baseline build completed successfully, including Unity compilation, Linux player build and artifact upload (job 104699606811). The 24 new UI screens are not included in that baseline; their compilation and screenshots still require validation. Do not mistake activation success or static checks for runtime UI validation.

An earlier draft branch codex/ui-foundation (9943238), produced by the stopped ChatGPT Work conversation, failed CI before activation because the hosted runner ran out of disk while extracting the Unity image. The coordinator added hosted-runner disk cleanup to the separate baseline verification workflow; the replacement run pulled the image and activated Unity successfully. Apply the same disk fix to the final UI validation workflow before integration. Do not merge both foundation implementations.

## Coordinator continuation

1. Inspect task status using codex cloud status TASK_ID. CLI list may include unrelated environments; filter exact task IDs.
2. Review foundation diff first. Then review each module's owned files against original images and ARCHITECTURE.md.
3. Apply reviewed changes on isolated integration branches, resolve conflicts and wire registrations/main entry points.
4. Run GitHub Actions and inspect actual logs/captures. Verify both portrait aspect ratios, simulated safe areas, nested dim input blocking, back/focus restoration, exact shop quantities and all 24 screens.
5. Iterate on findings, then merge approved validated implementation. No implementation has been merged yet.

## Draft review findings

All four repository-backed tasks have returned reviewable diffs. No UI implementation has been merged into main. Draft patches are preserved for coordinator review outside the active Unity Assets tree.

- Foundation: shared runtime routing/canvases and CI draft; final CI needs disk cleanup and a review of graphics-job activation. Registration wiring and all-screen capture coverage still require integration.
- Forge, progression and social: 24 route implementations across the three modules; static checks only. Each reports missing bespoke reference artwork or approximations from existing assets. They must not be presented as visually finished.
- Resolve duplicate Scripts/Screens.meta ownership when combining modules; preserve one consistent folder GUID.
- Review per-screen images, compile in cloud, and validate input, safe areas and captures before merging implementation.

## Active integration follow-up

The coordinator assembled all four drafts on remote branch codex/ui-24-integration, commit b7657cf. This is an unaccepted integration checkpoint, not a finished implementation. Worktree: C:/Users/user/.codex/worktrees/moonlit-ui-24-integration. Main project Assets were not modified and local Unity was not run.

Resolved duplicate Screens.meta ownership, normalized patch line endings, and added disk cleanup to both UI CI jobs. Static git diff --check passed.

Integration cloud task: https://chatgpt.com/codex/tasks/task_e_6aaa4b7d4a2c8329938a738ce3607eff . Submitted successfully against codex/ui-24-integration. It owns route registration/main entry wiring, compilation/test assembly fixes, modal/safe area behavior and cloud workflow/capture expansion. While active, do not duplicate its code edits. Inspect its result, apply the diff on the integration branch, create/update a PR and inspect actual CI. Bespoke image generation and visual review remain coordinator responsibilities. Do not merge main before acceptance.

## Heartbeat 2026-09-16 17:25 KST — integration PR and actual CI started

Source of truth now: remote codex/ui-24-integration commit 3549348499c6ba67394fda40a2b5793402ca8f2d; worktree C:/Users/user/.codex/worktrees/moonlit-ui-24-integration. Integration task task_e_6aaa4b7d4a2c8329938a738ce3607eff completed; its diff has been reviewed and applied. Do NOT reapply it.

Draft PR: https://github.com/kuzuni/20260916/pull/1 . Actual new UI CI running: https://github.com/kuzuni/20260916/actions/runs/35073553695 . Read run jobs/results before claiming tests passed. PR remains draft and unmerged.

Coordinator fixed page disabled-ancestor input, close/back behavior, deferred overlay visibility, pet/hero closure indexing, actual GameCI graphics activation and capture domain-reload persistence. Added real backdrop raycast and page return tests. Generated one production background with built-in image_gen and wired it in dungeon list/details: Assets/DarkFantasyUI/Resources/Moonlit/Dungeons/HammerThief-v1.png. Exact prompt and inspected reference: Documentation/UI/ART-GENERATION.md on integration branch. Other bespoke art is still missing. Full findings: integration branch Documentation/UI/INTEGRATION-CHECKPOINT.md.

New bounded cloud follow-up task https://chatgpt.com/codex/tasks/task_e_6aaa524857dc8329b7f0463cd99f179f started from 3549348. It owns real demo state fixes (sell/equip, automatic forge stop/config, one-time offline rewards, quick equip and pet/hero selection, mobile page return) and meaningful state/scroll tests in module code. Do not duplicate its in-progress module edits. Coordinator owns artwork and CI inspection. After completion, review/apply its diff, rerun CI as needed, inspect all captures at both aspects and safe areas, finish missing illustrations/reference fidelity before merge. No local Unity was run.
