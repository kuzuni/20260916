# Popup action button v2

Generated using the built-in image_gen tool, precise-object-edit of BlueAction-v1.png. The original v1 remains in the project.

## Exact prompt

Use case: precise-object-edit. Asset type: reusable Unity dark fantasy UI button backplate, transparent PNG. Input image is the edit target BlueAction-v1, not a screen to reproduce. Preserve the sapphire blue cracked stone face, silver bevel with thin muted antique-gold outline, straight-on flat UI view and wide horizontal clipped-corner silhouette. Remove ALL jewels and pointed flourishes from the center of the top edge, center of the bottom edge, and middle of the left and right edges. The full middle 70 percent of the top and bottom edges must be straight, uniform horizontal silver/gold rails, without any ornament or varying thickness, suitable for horizontal nine-slice stretch. Use only small restrained metal corner accents inside the four outer corner regions. Keep a large empty blue center for live Korean labels; no text, symbols, icons, branding or watermark. Tight crop around the backplate with only a small transparent margin; preserve genuinely transparent background outside the button, no shadow backdrop. This is one standalone button asset, not a mockup.

## Output and runtime binding

- Assets/DarkFantasyUI/Resources/Moonlit/Popup/BlueAction-v2.png: 2172 x 724, ARGB; corner alpha is 0. Source copied without image processing. Unique .meta, max texture size 4096, no NPOT rescaling or compression.
- PopupSkin.ActionArt creates a sprite from Rect(48,120,2076,488), preserving four pixels around measured visible bounds. This excludes export padding without changing the PNG. Border (240,150,240,150) keeps the clipped corners fixed. The middle edge has no motif that would be stretched across a wide button.
- Text, button hit target, icon and selected/unselected state remain separate runtime controls. Shared panel and close backplates are unchanged.
- Original profile reference 06 and v1/v2 art visually inspected. This is a closer reusable blue/silver backplate, not a claim of pixel-identical reproduction. Cloud screen captures must still confirm proportions and label clearance at both aspect ratios.
