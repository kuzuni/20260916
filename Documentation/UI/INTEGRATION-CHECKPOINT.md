# Integration checkpoint

## Latest verified result and social integration

CI 35077607384 compile/PlayMode job 104733618485 succeeded at revision 3d6dd2d. Downloaded NUnit artifact 10439357413 confirms 10 passed, zero failures/skips/inconclusive. Evidence report: CI-35077607384.md. Graphics job still running at this checkpoint; all-screen visual acceptance remains pending.

Social task task_e_6aaa5cb3f8608329a8f710daae4691ff is now reviewed/applied (do not reapply). Coordinator added actual row pointer targets and interior modal hit blocking with pointer tests, in addition to the cloud sprite reuse/navigation clearance changes. Both recent cloud tasks are complete and applied; no delegated task is currently executing. New source updates queue behind the running CI. Next: inspect graphics outcome and newest queued CI, correct failures, generate missing illustrated skill/reward/shop/avatar assets and compare all 24 rendered pages to original references before main merge.

## Latest checkpoint — 2026-09-16 18:20 KST

Reviewed/applied progression cloud task task_e_6aaa5c620df8832988a95fa79042550e, including coordinator fixes (see PROGRESSION-STATE-REVIEW.md). Do not apply again. Generated and wired all four dungeon paintings with independent sprite metadata and proportional cropped display; exact prompts in ART-GENERATION.md and per-image prompt files. Local commits b14ae06 (art) and 543ff5a (progression) preserve the state/CI baseline 3d6dd2d.

CI run 35077607384 tests commit 3d6dd2d and was still running at this checkpoint; no compile/test pass claimed. Subsequent CI should validate the newer commits. Concurrency now preserves a running validation and queues the latest PR update, avoiding repeated cancellation during integration. Social task task_e_6aaa5cb3f8608329a8f710daae4691ff remains in progress; inspect/apply its returned diff without overwriting progression or new artwork. Next required work: inspect actual NUnit/log evidence and graphics captures, fix failures, review social diff, continue missing skill/reward/shop/PvP art and all 24 reference layouts before merge. Main implementation remains unmerged.

Four cloud drafts are assembled and their 24 routes are wired on codex/ui-24-integration. They are NOT accepted yet. See the latest dated section below for the current source and CI state. Do not merge main until real cloud compilation, interaction and visual checks pass.

Baseline Unity license + Linux player build succeeded: https://github.com/kuzuni/20260916/actions/runs/35067009693 . Job 104699606811 completed successfully, including artifact upload. This baseline does not contain the 24 new screens.

The coordinator preserved a single new Scripts/Screens.meta and added hosted runner disk cleanup to both unity-cloud jobs. Do not integrate the earlier duplicate codex/ui-foundation branch.

Original integration checklist (historical; current progress below):
- Review the four module drafts; wire module registrations and all main/nav entry points using SCREENS.md.
- Replace old direct upgrade/auto-toggle paths with requested comparison/settings flows; check demo state across opens and closes.
- Fix compilation/asmdef/test integration, stale verification assumptions, popup input blocking, pages under usable nav, parent state/focus, safe area resizing.
- Make cloud workflows actually activate using the proven GameCI configuration, run meaningful tests and capture all 24 routes in 9:16 and 9:19 plus notch/side inset cases. Existing capture covers only main and is insufficient.
- Explicitly list missing bespoke artwork. Coordinator will supply generated images separately. Do not claim placeholders are finished art.
- Return reviewable diff and actual checks. Keep checkpoint updated. No local Unity.

## Integration implementation (current branch)

The three feature modules are now registered explicitly by the runtime factory. Main-screen entry
points use the required route keys: profile, offline rewards, progress pass, forge probability,
comparison, auto-forge settings, chat, equipment details, and the four supplied bottom-navigation
pages. The small main information button is the shared `player-details` entry with the local player
payload; ranking and PvP continue to use that same route with their own payloads.

The old immediate anvil upgrade and auto-toggle listeners are no longer attached. Comparison now
requires a sell/equip decision, applies its local demo result once, and closes without trapping a
resolved modal. Auto-forge rarity/stat choices, hammer quantity, and continue choice persist across
opens; Start is the only control that enables the existing local automatic-forge loop.

Cloud verification now requests every one of the 24 routes at both 1080x1920 and 1080x2280 with
notch/side safe insets, in addition to the six main-screen resize cases. The graphics container is
given the same three masked GameCI activation environment variables as the proven baseline rather
than relying on a manually installed license file. PlayMode coverage still checks modal top-only
pop/focus restoration, duplicate opens, page replacement/input blocking, and live safe-area refits.

Checks performed in this integration container: all 24 reference files and module reports were
located; representative full-resolution references were visually re-audited; route registrations,
main listener mappings, metadata presence, brace balance, and whitespace were checked statically.
`git diff --check` passes. Unity is intentionally not run locally, so Unity compilation, PlayMode
results, and the 48 route captures remain **unverified until the GitHub Actions run completes**.

Remaining artwork limitations are unchanged and must not be called finished: unique catalogue item
paintings, pass treasure/reward paintings, skill and dungeon illustrations, character portraits,
league crests, gem/product piles, banners, and several bespoke filigree/frame treatments are not in
the shared asset catalog. Current drafts reuse independent existing sprites and runtime ornamentation;
coordinator-provided generated assets are still required for final reference fidelity.

## Coordinator review 2026-09-16 17:30 KST

Applied task_e_6aaa4b7d4a2c8329938a738ce3607eff patch (7 files) and reviewed it. The cloud patch did not fix all requested defects. Coordinator corrections:
- Move PageHost outside the disabled MainCanvas CanvasGroup so page controls remain usable.
- Close page contexts and back from a page to main; deactivate closing layers before deferred destruction to prevent stale overlays in same-frame actions/captures.
- Fix pet/hero loop callback capture (names[i] was out of range after the loop).
- Replace raw docker capture invocation with actual game-ci/unity-builder activation, custom capture method, manualExit and enableGpu. Raw editor env vars alone did not invoke GameCI activation.
- Persist capture lifecycle via SessionState and InitializeOnLoad across Unity domain reload. Reject stale report from prior runs.
- Add page-close/input and actual modal backdrop raycast tests.
- Generate and wire the first independent dungeon painting, HammerThief-v1. Provenance: ART-GENERATION.md. Other bespoke art remains pending.

Static whitespace validation passed. Unity test/capture execution pending PR CI, never claimed as passed. Remaining important review items: screenshot bounds/content clipping and active safe-area relayout, meaningful sell/equip state (equip currently only reports a result), stopping/configuring automatic forge, parent scroll/tab preservation, remaining artwork, and original reference fidelity for every route. Do not merge until resolved.

## Heartbeat 2026-09-16 18:00 KST — state review and CI activation repair

Applied and reviewed cloud task task_e_6aaa524857dc8329b7f0463cd99f179f (state-fixes.patch). Do not apply it again. Coordinator corrected duplicate quick-equip slots, overlapping collection/shop return controls, forged-item repeated decisions and stale callbacks, same-category unlocked equipment binding, and charge-once pending craft state. Offline rewards now mutate the wallet once; auto configuration and stopping affect local state, with deterministic demo tier/filter matching. Full economy and skill/pet artwork fidelity remain incomplete.

Actual CI run 35073553695 failed before tests: Unity Licensing client Code 400, TimeStamp validation failed. No test result artifacts; graphics job skipped. This is NOT a Unity compile/test pass and does not prove credentials invalid. The earlier baseline builder job successfully authenticated the same three secret references. Replaced the latest CLI-backed unity-test-runner invocation with the existing unity-builder activation path and an Editor-only CloudTests entry using Test Framework 1.6 API. It writes NUnit XML and exits nonzero on failed/skipped/zero tests or timeout. API signatures checked against the installed 1.6 package source; Unity was not launched locally. Cloud verification must still establish whether this resolves activation.

Added meaningful regression checks for one-time offline rewards, pending craft cost/reopening, duplicate/stale sell callbacks, exact five shop offer rows plus actual return action, repeated quick-equip child count and parent ScrollRect restoration. Static git diff --check passes; new runtime tests are UNRUN until hosted CI completes.

Inspected original reference 19 and 21 at full resolution. Current skill glyphs, shop deal/reward art and general frames still differ substantially; these are drafts, not accepted artwork. PR #1 stays draft and main is not merged. Next: inspect replacement CI logs/results, repair actual failures, finish bespoke art and per-screen capture review for all 24 routes.
