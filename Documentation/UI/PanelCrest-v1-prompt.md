# Popup metal crest v1

Generated using the built-in image_gen tool. Original popup references 01/06/07 and hosted frame captures were inspected before this change. Source copied unchanged to Resources/Moonlit/Popup/PanelCrest-v1.png: 1983x793 ARGB, outer corner alpha=0.

## Exact prompt

Use case: stylized-concept. Asset type: standalone transparent ornamental top-center crest for a dark fantasy game popup frame. A narrow upright hollow diamond spearhead of beveled antique bronze and muted gold, dark iron inset, flanked symmetrically by slender curved gothic metal flourishes that extend sideways along an imaginary horizontal panel rail. Central diamond tall and pointed, strong readable silhouette; side flourishes restrained, like the top center of an ornate medieval stone UI window. Hand-painted dark gothic RPG interface art with crisp dark outlines and fine warm metallic highlights. Straight-on flat UI view, no perspective. About 2.5:1 overall silhouette width-to-height. Actual transparent empty background outside the metal and through the hollow diamond; keep ample transparent margin. One crest only. No full panel, no frame rectangle, no text, no lettering, no icons, no colored gem, no crown, no skull, no star, no background scene, no shadows cast onto a backdrop. It will be a separate decorative Image above a scalable panel, not stretched with the panel.

## Runtime binding

PopupSkin.CrestArt loads the full imported texture. Large shared panels attach an independent 180x72 Image at the top center, y=-34, with preserved aspect ratio and raycasts disabled. This replaces the faint procedural line ornament; it is not included in the panel's nine-slice region and cannot stretch when the frame changes size. The original PanelOrnament helper remains available for other decorations.

The existing shared-popup regression checks imported crest art, aspect preservation and input transparency. Static diff checks passed. Cloud rendered title/crest clearance at both aspect ratios is still pending; this checkpoint is not final visual acceptance.
