# Forge user correction — 2026-09-18

The user's explicit equipment anchors supersede compound growth: primitive health level 1 = 80, level 100 = 880, medieval level 1 = 1760. Attack uses the same scale (10 → 110 → 220). Levels interpolate linearly from 1× to 11× across the 99 intervals between level 1 and 100; this preserves the explicit endpoints despite the approximate “10% per level” wording. Each rarity starts at 22× the previous rarity's level 1. Collections and skills consume the same shared EquipmentRules baseline. Speed remains +1 per level with the next rarity starting at twice the previous level 100.

Source changes are cloud-only; local Unity is prohibited. Regression assertions cover exact anchors, constant increments and all ten rarity boundaries. Cloud tests and updated captures are pending.

## Runtime and presentation follow-up

Reference 09 was reviewed at original resolution. Equipment comparison again uses the lower ornate dialog, separate framed icons, neutral stone cards and the original red/blue action placement. With no equipped item in that part, only the new item appears and the header never claims it is equipped. Equipment details no longer links to the catalog. The catalog shows each item probability (grade probability / 18 variants/parts).

Original frame art is retained; a masked uGUI material neutralizes its source color before applying the probability grade color. Primitive is gray; medieval blue. EquipmentPictograms.Icon(index 0..8) supplies nine separate native silhouette sprites, and EquipmentSlot.SetEmptyIcon binds them without altering occupied item art. Root integrates these calls into the shared factory.

The gold gauge always spans the same width with the current 3–6 segments. Auto settings show only available grades; switching affix filtering off hides its subordinate controls and ignores them when matching. Comparisons pause an enabled automatic session; resolving its last card resumes it. Explicit stop or no hammers ends it. Closing comparison keeps the queue and permits the main auto button to stop the paused session. Shared auto-button wiring remains the coordinator's integration.

ForgeHammerMotion uses an Animator and authored one-second clip, sampled from real elapsed time, with three strikes. Equipment hands appear directly above the anvil with separate grade frames, half-overlapping within each row. Sale effects also call the shared RewardVisuals gold absorption API from the coordinator's change.

Focused tests cover automatic resume/empty-stop, missing equipped-card UI, exact three/six-segment width, and all three authored hammer poses. Existing auto-filter tests now assert hidden options and available-grade rows. Cloud compile, runtime tests and visual captures remain pending; no local Unity was run.
