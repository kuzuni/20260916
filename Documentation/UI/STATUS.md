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

All four submissions succeeded via codex cloud exec against this environment and main. Initial confirmed state: pending. These are actual repository-backed cloud jobs, not local worker processes. Task titles are service-generated; links identify each assignment.

| Assignment | Brief | Task |
|---|---|---|
| Shared canvases, modal stack, API, CI | Tasks/00-foundation.md | [Implement cloud CI for Moonlit UI](https://chatgpt.com/codex/tasks/task_e_6aaa3f284b848329a2f503179ed3c3c2) |
| Forge/equipment/offline/pass: 8 screens | Tasks/01-forge.md | [Implement Moonlit UI screens in cloud](https://chatgpt.com/codex/tasks/task_e_6aaa3f42b96c8329b8be80604ac2c898) |
| Skills/pets/heroes/dungeons: 7 screens | Tasks/02-progression.md | [Implement moonlit UI screens in cloud](https://chatgpt.com/codex/tasks/task_e_6aaa3f47b9e48329981d5b65dde1ce30) |
| Profile/settings/chat/ranking/PvP/shop: 9 screens | Tasks/03-social.md | [Implement moonlit UI screens in cloud](https://chatgpt.com/codex/tasks/task_e_6aaa3f4ca5b08329aaaaf1935e0165a3) |

Earlier ChatGPT Work conversation 6aaa3c5a-69dc-83ee-85cb-a46cbbf95790 received a stop-implementation/report-only instruction to avoid duplicate work. It is not the implementation source to merge.

## CI

The user confirmed existing GitHub Actions repository secrets UNITY_EMAIL, UNITY_LICENSE, UNITY_PASSWORD and explicitly authorized their use for CI. Only names were observed; values must never be retrieved, echoed, or stored in the repo. Reference them via workflow secrets expressions.

Secrets exist in GitHub Actions, not automatically in Codex Cloud's container. License activation, cloud Unity compilation and screenshots are not yet verified. Do not mistake submission or static checks for runtime validation.

## Coordinator continuation

1. Inspect task status using codex cloud status TASK_ID. CLI list may include unrelated environments; filter exact task IDs.
2. Review foundation diff first. Then review each module's owned files against original images and ARCHITECTURE.md.
3. Apply reviewed changes on isolated integration branches, resolve conflicts and wire registrations/main entry points.
4. Run GitHub Actions and inspect actual logs/captures. Verify both portrait aspect ratios, simulated safe areas, nested dim input blocking, back/focus restoration, exact shop quantities and all 24 screens.
5. Iterate on findings, then merge approved validated implementation. No implementation has been merged yet.
