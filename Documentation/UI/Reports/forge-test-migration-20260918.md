# Forge and reward integration test migration

Branch based on collection migration 1d78d9850d771ac85c94ff010b66f3d2b3faadbf. Reward and main APIs read from integration 525976ab. The root coordinator should take these test files into the integrated branch; this branch intentionally does not duplicate all root gameplay dependencies.

Preserved collection and dungeon tests from the collection agent. Replaced obsolete free/premium pass assertions with 100 milestones (stage 5 through 500), 200 independent hammer/ticket cards, three-ticket reward cycle, stage gate enforcement, duplicate claim rejection, parent scroll preservation on claim, and claimed-state persistence on reopening.

Replaced old level-123 ItemDefinition comparison assertions with six-slot EquipmentRoll bindings and separate existing thumbnail images, both portrait/safe-area sizes, nonmutating previews, pending comparison reopening without a new hammer charge, explicit equip swap followed by old-item gold sale.

Offline reward tests now register RewardsScreenModule after ForgeScreenModule. Deterministic future-anchored state clocks exercise 1 gold/second and 1 hammer/minute, visible accrual while a modal stays open, integer-minute remainder preservation, no duplicate claims, claimed-state reopening, and subsequent valid accrual/claim. Tests do not wait real minutes or depend on a wall-clock second boundary.

Auto configuration verifies all ten grades, all nine affix filters, disabled controls retaining values when the filter is off, persisted choices after reopen, quantity bounds 1 through 99, and stopping without an unintended batch. Existing ForgeFlowTests separately covers the animated batches. Production ForgeGameplayScreens adds stable affix checkbox names and toggles their interactability alongside filter enablement.

UiScreenHostTests.LocalRewardAndForgeDecisions now directly verifies RewardState accrual/claim and ForgeState stale callbacks rather than legacy hardcoded ore sales.

Static source checks: all edited C# brace counts balanced; old obsolete forge/pass/offline references removed from migrated methods. Cloud Unity compilation and PlayMode execution have not been run by this worker; no runtime pass claimed.
