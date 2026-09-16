# Social visual and layout review

Coordinator review: applied task task_e_6aaa5cb3f8608329a8f710daae4691ff, preserving the exact offer counts and navigation reservation. Found and corrected non-raycastable ranking/PvP/opponent row backplates, and modal frame backgrounds that let interior taps reach Dim. Added actual pointer raycast regression checks for ranking and interior modal taps. The same interior hit-blocking fix was applied to forge/progression modal frames. Existing sprite indices were checked against the main runtime layout. New tests remain pending; the earlier 3d6dd2d revision passed 10 hosted PlayMode tests (CI-35077607384.md).

Reviewed the full-resolution originals for references 06, 07, 11, 13, 18, 21, 22, 23 and 24 against the runtime-built social module.

## Implemented fidelity pass

- Reused `MainScreenAssets.panels` for bronze stone surfaces and silver/cobalt action plates rather than flat-color rectangles.
- Reused the separate crown coin, ruby, crossed-swords and star sprites from `interfaceIcons`; shop currency and gem offers no longer represent those assets with font glyphs.
- Shop remains a vertical `ScrollRect` with exactly 60, 220, 800, 1500 and 3300 ruby offers. The last two remain explicitly `가격 미설정`; every purchase action is an unconnected local preview.
- Shop and PvP calculate their lower content boundary from a 210-unit navigation reservation. PvP's sticky player row and challenge action remain above that reservation at 9:16, 9:19 and reduced Safe Area heights.
- Modal frames are centered inside the host-provided Safe Area; close controls remain within their frame. Ranking/player children continue to use host stacking, so the live parent list and its scroll state survive child close.
- All changed presentation remains runtime uGUI: separate Images and Text, Buttons, InputFields, Toggles and ScrollRects. No reference image is imported into `Assets` or displayed as finished UI.

## Artwork still missing

Existing shared assets do **not** contain the following reference-specific paintings. They are intentionally not replaced with generic artwork presented as final:

- unique player/avatar portraits and the wide player-details companion battle painting;
- chat portrait set, clan-rank crests, embedded battle-result card and video-camera icon;
- PvP gold-league shield/laurel crest, challenge-ticket icon and ranked medal set;
- shop resource/pet/dungeon bundle paintings, red ribbon header and distinct ruby pile/bag paintings;
- reward resource icons beyond the shared crown coin/ruby set, plus the reward-modal red cloth title banner;
- profile avatar/edit-pencil artwork and settings row pictograms.

The coordinator can generate and catalog these as separate transparent icons, reusable frames, and background paintings without changing the screen contracts.

## Validation status

Added Play Mode coverage for both portrait ratios, exact shop quantities/unconfigured prices, navigation-reserved page bounds, Safe Area PvP controls and ranking scroll restoration. This task did not run Unity locally, per the execution restriction. Hosted Unity 6000.3.8f1 compile, Play Mode execution, and captures at both aspect ratios remain coordinator-owned and are required before calling this a runtime pass.
