# Run117 visual follow-up — 2026-09-19

Source e77a04a6855be242decd150e8b501bb564d933a3 (PR97), merged as d7f5b1f046fc43a7a82a76ddbfb504c58434d596, tree 3db8a583ff3eec53f0bcbc18af1223dfdcbcfcda.

Hosted Unity6000.3.8f1 [run117](https://github.com/kuzuni/20260916/actions/runs/35358594699) completed compilation, 268 PlayMode tests (zero failed/skipped/inconclusive), and graphics verification. Downloaded originals comprise 527 PNGs: 136 regular images per aspect, 66 early-era skill phases per aspect, 60 later-era phases per aspect, and three general images. Evidence root: C:/Users/user/.codex/artifacts/moonlit-gameplay-restoration-20260918/run117/.

Actual image review found issues despite the automated graphics PASS; this is not final visual acceptance:

- Long chicken skill description overlaps the owned-effect heading in both detail captures. The single Equip button is correctly restored; pet/mount-specific detail captures are being added.
- Early-skill captures can show an old effects frame (food remains in a healing sample) and the 9:19 actors can be frozen at entry edges. Capture sampling must wait for the requested effects frame and render the battle texture after the real entrance settles.
- The right-side pass icon overlaps the enemy in a short notched mounted view; actor clearance requires review.
- The shop absorption now actually renders: wallet100, +100 and gem particles are visible in both originals, and ON/OFF comparison measures46,370 changed pixels at9:16 and40,574 at9:19. The held effect must complete before independent pass/anvil captures to avoid carrying previous particles into later fixtures.

Whole companion PNG closeups (all six at both ratios) show complete illustrations without old rig seams. Pets behind a mounted character are partially occluded by the mount; no rig repair was attempted. Skill totals, event authorization, delayed healing, independent equipment selection, modal input and saved progression passed the tests, but captures of the revised sequences require another review.

The graphics log includes an editor-internal UnityEditor.Search.SearchDatabase startup ArgumentOutOfRangeException, followed by successful runtime verification. DOTween expanded its capacity during500-result fixtures; no gameplay exception was reported in this run. Physical devices and the user's uncommitted local prefab edits were not tested. Enemy uses the Player prefab; arena opponents and purchases are local demos; higher-era companion art and accessory functionality remain deferred.
