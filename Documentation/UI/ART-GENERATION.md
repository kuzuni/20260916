# Generated UI artwork

## Dungeon set completed as source artwork — 2026-09-16

All four dungeons now have independent generated paintings in Resources/Moonlit/Dungeons. Added GhostVillage-v1, Invasion-v1 and ZombieRush-v1 (each 2172 x 724) with unique sprite metadata. Original reference 20 was reviewed at full resolution, and reference 04 informed the detail crop. Both list and detail use a RectMask2D viewport with proportional cover scaling, keeping controls/frame/text separate. These source images were visually inspected; rendered Unity captures are still pending. Other UI categories still need bespoke artwork.

Exact prompts and per-image inspection notes: [Ghost Village](GhostVillage-v1-prompt.md), [Invasion](Invasion-v1-prompt.md), [Zombie Rush](ZombieRush-v1-prompt.md).

## HammerThief-v1 — 2026-09-16

Tool: built-in image_gen (no API/CLI fallback).
Asset: Assets/DarkFantasyUI/Resources/Moonlit/Dungeons/HammerThief-v1.png.
Reference inspected at full resolution: Documentation/UI/References/20-dungeons.png.
Generated output: 2167 x 726 pixels. Reviewed visually: central iron hammer, gothic blue castle backdrop, no UI/text/frame. This painting is loaded as an independent sprite in the dungeon list and detail; buttons, label and reusable framing are runtime UI. Other dungeon illustrations remain pending.

Exact prompt:

Create a production game art asset, one seamless wide horizontal illustration banner for a dark fantasy mobile RPG dungeon called Hammer Thief. Landscape 3:1 composition. High quality hand-painted 2D gothic fantasy, deep desaturated midnight blue and cyan rim light, bronze torch glows. A huge worn forged iron blacksmith hammer leaning diagonally in the center foreground, ancient moonlit medieval castle towers and abandoned workshops in the distance, ivy and damp stones along bottom. Composition should remain readable cropped to 4:1, focal hammer in the middle, upper left and right third quieter dark scenery so live UI labels and buttons can later overlay it. Detailed crisp illustrative contours with painterly texture consistent with a dark gothic idle RPG. No text, letters, numbers, UI, frame, border, slots, badges, buttons, watermark. This is a standalone background painting only; every UI frame and button will be a separate reusable Unity object.
