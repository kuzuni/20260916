# Cloud task 01 — forge/equipment module (8 references)

Implement ForgeScreenModule.Register per ARCHITECTURE.md. Own only Assets/DarkFantasyUI/Scripts/Screens/Forge/**, Art/Screens/Forge/** with metas, and Documentation/UI/Reports/forge.md. Do not edit shared files.

Routes/reference IDs: 01 forge-probability; 02 forge-probability-details; 03 forge-item-details; 08 equipment-details; 09 forge-comparison; 10 offline-rewards; 12 auto-forge; 05 progress-pass. All are Modal.

Inspect all original PNGs; build their dense illustrated layouts, probability rarity rows, categorized item grids, stats, compare panel, configurable checkboxes/toggles, scrollable two-column pass rewards and level/timer bars. UI text stays live; item art and frames separate. Nested links use context.Open. Selection payloads must match ARCHITECTURE.md.

Main anvil must lead to compare-and-equip/sell; do not simply auto-upgrade when the integration hook opens this route. Implement local demo state coherently (no double claim, explicit sell/equip, insufficient currency, stable filters). Extra premium purchases remain unconnected clearly; no payment flow. Pass navigation is the former fairy button.

You may reuse public existing Ui/MainScreenAssets/EquipmentSlot. Keep feature-specific helpers inside owned folder. Use generated artwork for missing visual assets if available, preserve prompts, otherwise explicitly list the art gaps. Never use full reference PNGs in production.

Deliver module code and a report with registrations, expected main entry hooks, actual checks, screenshots if possible and blockers. Foundation types are being implemented in parallel; don't add conflicting stub types to production. Do not merge main.
