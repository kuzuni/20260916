# Cloud task 00 — shared runtime UI and CI foundation

Repository: https://github.com/kuzuni/20260916
Read AGENTS.md and Documentation/UI/{SCREENS,ARCHITECTURE}.md.

Implement the common module API exactly as specified; split runtime main/nav canvases and build a dynamic popup stack. Main navigation pages and nested modals must have separate lifecycle/input semantics. Maintain runtime bootstrap-only scene and existing main reference appearance. Existing MainScreen.Modal currently destroys a previous modal; replace that behavior for the new routing architecture without breaking the current main UI.

Own shared Scripts, tests, CI, and documentation. Do not implement the 24 module screens; parallel workers own those. Avoid compile-time dependencies on unmerged module classes. Provide an explicit integration hook and a route-key map for the integrator.

Build a cloud CI workflow for Unity 6000.3.8f1, using GitHub Actions/GameCI when feasible. Determine actual license method/support; do not assume Personal credentials are already available. Keep build/test job separate from graphics-capable screenshot job. Do not use -nographics for render captures. Upload logs/results/captures. Missing secret or unavailable runner must be visible failure/pending configuration, not green success. Do not run local Unity or use a self-hosted user's PC.

The user confirmed GitHub Actions repository secrets UNITY_EMAIL, UNITY_LICENSE and UNITY_PASSWORD are already registered and authorized their use for this CI. Reference these by secrets expressions in the workflow; never retrieve, print, copy into code, or request their values. License validity is not yet proven; verify via actual CI activation. These are GitHub Actions secrets, not credentials installed in the Codex task container.

Add meaningful tests for multi-level popup push/pop, click-through to nav/main, focus restore, preserving parent state, changing safe area while modal open, duplicate taps, and clean destruction. Cloud compilation must verify feature contract itself. Include a minimal temporary test screen only in test code.

Return code diff/branch or PR, actual checks/results, CI prerequisites, and exact integration instructions. Do not merge main.
