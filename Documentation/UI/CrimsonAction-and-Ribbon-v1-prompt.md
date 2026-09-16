# Crimson action and equipment ribbon — generated artwork

Built-in imagegen, 2026-09-16. Original PNG outputs copied unchanged; no image editing script. Text and hit targets remain independent runtime uGUI. Reference 09 inspected at full resolution.

## CrimsonAction-v1.png

Edit input: Assets/DarkFantasyUI/Resources/Moonlit/Popup/BlueAction-v2.png.
Output: exec-da24dbed-de46-45fe-9166-273ca85975a0.png, 2172x724, alpha 0 at corner. Alpha>180 bounds x51..2119, y112..599. Runtime crop (48,116,2076,504) in bottom-origin coordinates; fixed slice borders (240,150,240,150). Crop/borders/PPU scale to loaded texture dimensions. Existing original blue remains unchanged.

Exact prompt:

Use case: precise-object-edit. Edit target: the supplied BlueAction-v2 PNG, a reusable Unity dark-fantasy button backplate. Produce its crimson sale/decline counterpart. Preserve the exact wide horizontal chamfered rectangle silhouette, straight metallic rails, antique gold outer rim, silver inner bevel, small triangular gold corner accents, cracked stone texture and transparent background. Change ONLY the cobalt-blue stone face and blue edge glow to deep blood-crimson/burgundy stone with restrained red highlights. Keep the central label area dark enough for white Korean live text. No words, no letters, no symbols, no extra ornaments, no center jewels, no perspective, no drop shadow outside the silhouette. Keep the original button scale, placement, aspect ratio and export gutters as closely as possible. True alpha transparency outside the button. Output a single isolated production bitmap, not a mockup.

## EquippedRibbon-v1.png

Output: exec-6b213fb6-2583-4d65-93bd-048cec63bfaf.png, 1942x809, alpha 0 at corner. Alpha>180 bounds x100..1840, y228..566. Runtime crop (96,235,1752,350), bottom origin. Aspect-preserving 260x52 independent Image; live equipped label separately drawn. No stretching of end notch.

Exact prompt:

Use case: stylized-concept. Asset type: a single reusable 2D dark-fantasy game popup title ribbon, without text, for a Unity uGUI equipment panel. Straight-on orthographic view. A long horizontal charcoal-black cracked slate nameplate with a fine antique gold/bronze raised metal edge. Left edge vertical and square; right edge is cut into a shallow inward V swallowtail notch, like the equipment 'equipped' header ribbon on a gothic mobile RPG. Top and bottom edges perfectly straight and parallel. Thin bevel and subtly worn golden highlights, small restrained corner details at far left. The interior is uniformly dark charcoal so white Korean labels can be drawn independently. Width about 4.6 times height; the ribbon occupies 92 percent of canvas width, centered vertically, generous transparent margin outside. True transparent alpha background. No text, letters, icons, logos, gem, crown, center ornament, scroll curls, extra banners, perspective, surrounding panel or scene. Premium hand-painted game UI metal and stone texture; one isolated blank ribbon only.

## Validation

Generated PNGs visually inspected. Static diff/import coordinate checks only at commit time; hosted Unity tests and 9:16/9:19 captures are required for actual runtime/visual acceptance. Equipment lifecycle test also asserts imported crimson/blue faces are separate and ribbon does not intercept input.
