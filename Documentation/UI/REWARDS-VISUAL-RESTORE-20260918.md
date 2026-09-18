# Pass and offline visual restoration

Read original References/05-progress-pass.png and 10-offline-rewards.png at full resolution. Restored the authored d64fa77 ForgeScreenModule layout helpers rather than inventing another layout.

Progress pass restores the 940-unit cracked-stone panel, independent sword banner, blue free/gold premium columns, original gold purchase button, central cyan timeline/diamond nodes, paired illustrated cards, locked premium chest artwork, and claimed checkmarks. The real free progression now spans 100 rows at stages 5..500, awarding the existing 100 hammers + 10 rotating skill/pet/mount tickets exactly once. Premium artwork remains the original locked demo presentation; purchase only explains that it is unconnected, with no new premium entitlement or reward policy.

Offline restores the 740x900 ornate panel, original reusable square equipment frames, two crest dividers, green 1/sec and 1/min rates, separate currency icons and numeric totals, and blue collect button. The existing live accrued-seconds state still updates while open. Currency totals fit long integer values. Claims emit RewardVisuals.Absorb effects from the actual reward area with the amount actually credited.

New RewardsVisualRestoreTests cover 100 paired timeline rows, claim idempotence and checks, reaching stage500 by actual scrolling, and live offline totals while retaining the original illustrated slots/dividers. Shared ProgressionStateTests and root dungeon route are untouched. Cloud Unity validation/captures pending integration. Local Unity was not run.

Dependency: coordinator RewardVisuals.Ticket(category) and Absorb(MainScreen, RewardVisuals.Kind, int, Vector3?) with Kind.Gold/Hammer/SkillTicket/PetTicket/MountTicket.
