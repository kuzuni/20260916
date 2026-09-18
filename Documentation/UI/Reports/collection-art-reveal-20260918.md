# Collection presentation and 30 skill catalog

Base: f953cdc940b41f1de11505ae3da4f14a01c2537b. Isolated cloud branch: codex/collection-art-reveal-20260918.

## Changes
- Reviewed original references 14, 15, 16, 17 and 19 at full resolution and pre-gameplay layout history d64fa77. Restored summon dais background, outlined probability groups with separate tinted headers, passive effect panel and equipped ribbon overlays.
- Collection scroll masks extend to 20 logical pixels above the equipped panel at both aspect ratios. All 30 skills show their assigned illustration even while locked; ownership lock is a separate overlay.
- New shared SkillCatalog supplies 30 distinct tier-themed Korean names/descriptions and stable resource keys; combat and collection use the same skill Sprite.
- Cost row shows only ticket icon/count, diamond icon/count, or both separated by +. Pet ticket uses egg artwork and mount ticket uses hoof artwork.
- Summon probability dialog replaces explanatory text with the actual selected category's current level and XP gauge, independent of the previewed probability level.
- DOTween is already installed in Assets/Plugins/Demigiant/DOTween. Result cards and status labels reveal together every 0.14s with a 0.32s scale/fade/move tween. Hidden cards cannot be clicked. Disable/destroy cancels owned tweens and never grants or rolls rewards again.
- All 30 skill detail previews forward MainScreen.PreviewSkill(tier,variant).
- Dungeon methods/state and pass logic unchanged.

## Integration dependencies
Coordinator supplies Sprite assets at Resources/Moonlit/Combat/Skills/Tier00..Tier09/{Buff,Weak,Strong}.png and Resources/Moonlit/Popup/{PetTicketEgg-v2,MountTicketHoof-v2}.png, plus MainScreen.PreviewSkill forwarding.
Forge branch supplies shared linear equipment scaling: primitive level100 HP880 per health piece; medieval level1 HP1760. Collection values still query EquipmentRules.FullSetStats directly.

## Validation
Added regression coverage for shared equipment rebalance propagation, 30 distinct catalog identities, all 30 actual resource mappings including unowned items, extended safe-area mask bounds, single/mixed cost display, category XP while browsing, deterministic DOTween reveal ordering, early-close cancellation and permanent granted copies.
Existing collection lifecycle/ticket/upgrade assertions retained; result-card lookup now follows wrapper hierarchy. No local Unity execution or authoring performed. Cloud compile, PlayMode run and captures must run after coordinator merges required assets/forwarder/shared balance; this branch alone deliberately does not fabricate missing artwork.
