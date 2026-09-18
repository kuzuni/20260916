# Projectile arrival and damage timing

Weak and Strong previously consumed their damage event at0.3seconds, then started a0.65second projectile. Health/death could change before the visible hit and the next action could start before arrival.

Attack VFX now launch immediately with the motion. Their only damage event is at the shared PrimitiveSkillEffects.AttackFlightDuration (0.65seconds), before the0.8second action finishes. Basic and Buff retain their0.3second events; Buff healing/effect remain launched together. VFX remain purely visual and never call combat damage. Arrival snaps the projectile to the target and hides its sprite while the trail fades and impact particles play.

Both CombatAssetBuilder and the checked-in Weak.anim/Strong.anim event data are updated, so existing installs receive the correction without rebuilding assets.

CombatSkillTimingTests has two real BattleRuntime/Animator cases. It starts the actual Strike iterator, asserts a projectile exists before any damage, explicitly samples the real imported Animator before/after arrival, checks fixed damage and duplicate rejection, and verifies Basic/Buff timing remains unchanged. Explicit Animator stepping avoids cloud wall-clock flakiness. Requires Unity6000.3.8f1 cloud; not run locally.

CaptureGameplay's fixed WaitForSecondsRealtime(.38) does not guarantee a flight phase under slow graphics frames. Capture with a fixed simulation time step plus a counted number of frames, or a normalized-time preview API; do not infer VFX quality solely from screenshot file existence. RuntimeVerification was not modified by this task.
