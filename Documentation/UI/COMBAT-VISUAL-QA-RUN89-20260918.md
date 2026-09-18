# Combat visual QA of run89

Reviewed 16 actual cloud captures at original resolution: live combat, initial 9:16 main, and selected flight/impact captures spanning all ten eras (with primitive/modern/celestial samples and both 9:16 and 9:19). Evidence directory: C:/Users/user/.codex/artifacts/moonlit-gameplay-restoration-20260918/run89/.

Observed correct illustration silhouettes including meat, thrown rock, boulder fragments, crossbow bolts, cannonball, tank, orbital laser, supernova, dimensional fragments, quantum particles, soul chain and divine gold effects. No default white-square particle or obvious VFX viewport clipping was observed in these selected frames. Actual 8/10 damage and critical15 numbers were visible above victims. The existing 133 passing tests and209PNG were reported by the coordinator; this review did not rerun Unity.

Concrete corrections:
- Initial entrance HP bars crossed the fixed left reward/right pass buttons. World canvases now start hidden and reveal after the .45-second entrance reaches the battle home positions. Approved buttons and actor2x scale remain unchanged.
- During melee contact the two HP bars visually joined. Each still follows its own head, with a small .35-world-unit outward horizontal offset to keep them separated.
- Long integer HP values forced tiny text. Health uses the existing MainScreen.Compact formatter (e.g.64.17m/64.17m) so the maximum readable size is retained.
- A primitive buff preview started during entrance could remain at the old spawn point. VFX now optionally follow the real source/target Motion transforms, retaining their initial offsets; travel duration and event-only damage remain unchanged.

Added runtime tests for entrance visibility/home positions, compact HP and separated bars at the actual Basic animation peak, plus buff following a moving preview source. Follow-up cloud compile/tests/captures required; no local Unity run.
