# Independent combat runtime review

Review base: main4cb817c9ee35f40d5fede5bd19d29924fcf0069a.

Confirmed one layout defect: compact Safe Area height1600 leaves a120-unit battle view. The previous camera upper edge was2.3 while the normalized actor is2.55 tall, clipping the head. The last-action label also extended to136, overlapping the bottom panel. Camera center is now clamped to at least1.35 world units (allowing the normal0.35 skill lift), and the secondary last-action line hides below150 view units. Normal9:16/9:19 layouts keep the line.

Reviewed: speed/tie initiative, buff-before-basic and attack-skills-after-double planning, no extra attack after lethal hit, maximum15 complete rounds, stage floor1, same normal stage after dungeon result, single-use animation gate, original armor/head/weapon sprite swapping. SpriteSkin.autoRebind setter behavior was checked against the installed2DAnimation package source; enabling it after sprite assignment triggers cache/rebinding.

Added four meaningful PlayMode tests in CombatRuntimeAuditTests:
- Actual Player prefab armor replaces torso+fourlimbs, hat replaces head, weapon replaces weapon; unequip restores originals.
- Actual Animator-driven encounter with durable armor and weak attack completes both actors' round15 before timeout loss.
- Dungeon rejects a concurrent entry, invokes completiononce, preserves normalstage and restarts normal coroutine.
- Actual actor renderer bounds fit the compact camera and the overflowing secondary line hides.

Tests require generated CombatAssetBuilder catalog and Unity6000.3.8f1 cloud. They were authored/reviewed but not run locally; cloud validation/captures remain coordinator work. No artwork was regenerated during this audit.
