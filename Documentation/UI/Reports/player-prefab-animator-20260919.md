> Current status: [latest validation and remaining visual limitations](runtime-validation-20260919.md). The historical findings below describe their named revisions.

# Player prefab Animator delivery — 2026-09-19

The Player reference prefab now contains a serialized Animator, its native-bone controller and CombatAnimationRelay. The seven states are Idle, Basic, Hit, Buff, Weak, Strong and Death. The four attacking/support animation events authorize their armed callbacks once. Existing sprite and bone offsets are preserved; the battle wrapper disables the nested Animator to avoid duplicate playback/events.

Delivered in [PR98](https://github.com/kuzuni/20260916/pull/98), merge a55a718cdea502851d9d00038b740ed3c5072d36. Tested source: 3d7813c72bf04c562e5b51e871f97c2767615d07. The cloud entry point now calls ReferencePlayerAnimatorBuilder.ValidateCommitted(), so this validation loads the committed prefab/controller/clips instead of regenerating them.

Hosted Unity 6000.3.8f1 [run120](https://github.com/kuzuni/20260916/actions/runs/35363288646), test job105661168685, passed283 tests with0 failures, skips or inconclusive cases. The downloaded summary and XML agree. All12 ReferencePlayerAnimatorTests passed: one serialized reference/native-binding test, seven actual bone-motion/root-preservation cases and four exactly-once event cases.

Evidence: C:/Users/user/.codex/artifacts/moonlit-gameplay-restoration-20260918/run120/tests/Artifacts/TestResults/{summary.txt,playmode-results.xml}, artifact10555811350. Local main was fast-forwarded with the user's Recovery scene and metadata preserved. No local Unity editor was opened or run.

This report certifies the native Animator delivery and the named automated tests. It is not final visual acceptance of the separate six-skill revision. Run118 original images exposed short-screen food clipping and a formation-clearance regression that can place the enemy on the player's side; those follow-up corrections were subsequently verified in run121; the linked current report records that review and remaining art/HUD layering limits. Run118 also verified visible shop diamond absorption at both ratios, clean subsequent anvil/pass captures, and the restored single Equip button without description overlap.
