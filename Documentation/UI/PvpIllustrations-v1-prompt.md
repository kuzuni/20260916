# PvP illustrations v1

Generated 2026-09-16 with built-in image_gen. Original reference 23-pvp.png was visually inspected at full resolution. Each illustration is a separate transparent PNG; labels, buttons, timer, portrait frames and standings remain independent runtime uGUI. No screenshot was used as finished UI.

## GoldLeagueCrest-v1.png

Saved: Assets/DarkFantasyUI/Resources/Moonlit/Social/GoldLeagueCrest-v1.png

Exact prompt:

Use case: stylized-concept. Create a single production game UI sprite: a front-facing gold league shield crest for a Korean dark fantasy mobile RPG. A stout pointed heraldic shield with a thick layered bevelled gold and dark bronze rim, angular top corners, amber-orange inset face, and one dark iron sword silhouette diagonally rising from lower left to upper right. Painted 2D fantasy inventory icon style, crisp black contour, bright warm metal highlights, subtle scratches, rich shading, readable when reduced to 150 pixels. Center the whole shield within a square canvas, occupy about 82 percent of the image height, leave equal clean padding. Isolated on a truly transparent alpha background. No text, letters, numbers, banner, crown, wings, sparkles, floor, shadow plane, surrounding UI, or additional objects. This is only a crest illustration; all interface labels and buttons will be separate runtime UI.

## SeasonGift-v1.png

Saved: Assets/DarkFantasyUI/Resources/Moonlit/Social/SeasonGift-v1.png

Exact prompt:

Use case: stylized-concept. Create one single production game UI icon for a dark fantasy mobile RPG: a compact square crimson red gift box, luminous warm golden ribbon wrapped vertically and horizontally, a neat golden two-loop bow on the lid. Slight three-quarter front view with a visible lid, painted 2D game inventory illustration, crisp dark outline, rich saturated red, subtle paper texture, bright gold highlights and simple readable silhouette at 50 pixels. Center the whole gift within a square canvas with generous transparent padding, occupying about 78 percent width and height. Truly transparent alpha background. No lettering, symbols, numbers, frame, scenery, floor, ground shadow, particles or extra objects. Illustration only, separate from all UI buttons and labels.

## Binding and limits

Both originals are 1254 x 1254 ARGB PNGs with transparent outer corners verified by read-only pixel inspection. Imported as single sprites, no mipmaps, original dimensions, alpha preserved. Runtime loads them from Resources/Moonlit/Social. Artwork ignores raycasts; the season reward button owns the hit target and opens pvp-rewards.

The generated shield and gift replace unsupported font emoji, not exact pixel copies of the reference. Row decoration and many other 24-screen visual details remain unfinished. The nine portrait illustrations currently differ from the original PvP character lineup. New runtime import, interactions and captures await hosted Unity validation. No local Unity execution.

