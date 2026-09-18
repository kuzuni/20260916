# World combat / thirty skills

Based on main f953cdc. Cloud-only branch codex/combat-world-skills-20260918.

- Both actual Player-prefab actor roots are scaled by exactly two. The previous projected pixels/world-unit are retained; the viewport expands upward only when required to contain the enlarged actors and head HUD. No compensating zoom-out.
- Each character owns a following World Space Canvas on combat layer 30; its health bar/text follows the real head renderer. Damage floats are created at the victim's head by the same gated AnimationEvent that resolves damage. The old fixed health/status/action/wave labels are removed.
- Root HUD consumes Wave, WaveCount, Round for its cyan nodes and separate round line.
- CombatSkill now retains its collection grade as tier. Thirty era/variant sprite VFX use Resources/Moonlit/Combat/Skills/Tier00..09/{Buff,Weak,Strong}; fixed values and cooldown logic remain unchanged. Each era has its illustrated subject and matching movement. Real alpha-textured sprite particles replace default white squares; primitive thrown stones scatter stone sprites.
- Weak/strong effects launch before the animation; .65-second Animator events remain the sole source of damage. Buffs heal at the existing .3-second event.
- Successful dungeons stop at AwaitingExternalClaim with their scene/result intact. The UI calls CompleteExternalClaim once after claiming. Arena and losses return normally. Callback state is established before invocation, including synchronous claim safety.
- PreviewSkill(tier,variant) previews all thirty without a blocking toast; PreviewPrimitiveSkill remains a wrapper.

Integration: root owns Main.PreviewSkill forwarding, stage/round HUD, claim/key charging workflow, all thirty image assets, and shared capture harness. World HP labels are accessible via BattleRuntime.PlayerHud.HealthText / EnemyHud.HealthText, not through Main.GetComponentsInChildren because the world is intentionally isolated.

Tests: existing real-bootstrap tests now inspect world HP; dungeon audit checks the result wait and idempotent claim; compact test checks the explicit 2x scale and unchanged projection density. Added actual-world head following, AnimationEvent victim-number ownership, thirty distinct illustrations, and alpha-textured impact tests. No local Unity was run. Cloud compile, PlayMode tests and both-aspect capture acceptance are still required on the integrated branch.
