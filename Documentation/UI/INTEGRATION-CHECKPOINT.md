# Integration checkpoint

Four cloud drafts have been assembled on codex/ui-24-integration from main 95a50bc. They are NOT accepted or wired yet. Do not merge main until real cloud compilation, interaction and visual checks pass.

Baseline Unity license + Linux player build succeeded: https://github.com/kuzuni/20260916/actions/runs/35067009693 . Job 104699606811 completed successfully, including artifact upload. This baseline does not contain the 24 new screens.

The coordinator preserved a single new Scripts/Screens.meta and added hosted runner disk cleanup to both unity-cloud jobs. Do not integrate the earlier duplicate codex/ui-foundation branch.

Next cloud integration work:
- Review the four module drafts; wire module registrations and all main/nav entry points using SCREENS.md.
- Replace old direct upgrade/auto-toggle paths with requested comparison/settings flows; check demo state across opens and closes.
- Fix compilation/asmdef/test integration, stale verification assumptions, popup input blocking, pages under usable nav, parent state/focus, safe area resizing.
- Make cloud workflows actually activate using the proven GameCI configuration, run meaningful tests and capture all 24 routes in 9:16 and 9:19 plus notch/side inset cases. Existing capture covers only main and is insufficient.
- Explicitly list missing bespoke artwork. Coordinator will supply generated images separately. Do not claim placeholders are finished art.
- Return reviewable diff and actual checks. Keep checkpoint updated. No local Unity.
