# Forge probability tier artwork v1

Built-in image_gen produced two independent atlases after inspecting original reference 01 at full resolution. PNGs are copied unchanged; no screenshot is used in runtime UI.

## Exact icon prompt

Use case: stylized-concept. Asset type: transparent sprite atlas of ten independent dark fantasy RPG progression-tier icons, hand-painted with crisp black outlines and metallic highlights, legible at 54 pixels. Exact grid: five equal square columns by two equal rows, each icon centered with generous transparent padding, no overlap. Reading order TOP ROW: primitive stone axe on wooden handle diagonally; medieval silver straight sword with gold crossguard diagonally; simple wooden longbow diagonally; compact black and brass fantasy flintlock pistol diagonally; red and silver futuristic space ray pistol diagonally. BOTTOM ROW: purple orb-tipped arcane wand diagonally; cyan red-accented multiverse plasma blaster diagonally; glowing royal blue atom symbol with three orbit ellipses; silver underworld trident upright; symmetrical golden divine winged sun emblem. Closely match ornate Korean gothic fantasy mobile-game equipment probability table icon language, no realistic photographic weapons. All are distinct recognizable silhouettes with no circular medallion frames. Actual transparent background, no text, no letters, no numbers, no labels, no star symbols, no UI backplates, no grid lines, no watermark. Atlas overall ratio 5:2.

## Exact background prompt

Use case: stylized-concept. Asset type: reusable background atlas for a gothic RPG rarity probability table. EXACT atlas layout: two equal columns and five equal rows, ten independent long horizontal bars. Each bar has a 6:1 width to height silhouette inside its own cell with transparent gutters. Thin bevelled colored metal border, small clipped corners, straight rails, dark atmospheric painted castle or cosmic scene inside. Keep at least 80 percent of each interior subdued and dark enough for white live text overlay. No text, no numbers, no weapons, no icons, no stars, no UI labels. Reading order LEFT TO RIGHT, TOP TO BOTTOM: 1 charcoal gray ruined stone castle; 2 sapphire blue moonlit fortress; 3 emerald green misty forest keep; 4 muted gold ochre castle; 5 crimson red burning citadel; 6 violet arcane nebula; 7 cyan otherworldly castle; 8 royal blue swirling cosmic portal; 9 dark russet underworld fortress; 10 amber orange divine city. Consistent outer bar geometry and thin edge weight. Crisp hand-painted dark fantasy mobile game UI, no glossy modern gradients. Real transparent background outside each bar, no checkerboard, no watermark. Overall atlas ratio roughly 12:5. All ten bars completely separated, no attached symbols or lettering; they will be sliced individually and receive independent icon/text layers at runtime.

## Files and runtime regions

- Resources/Moonlit/Forge/TierIcons-v1.png: 1983 x 793 ARGB; outer corner alpha 0. Ten separate illustrated tier symbols. Silhouettes are staggered, so runtime uses authored source regions (row split y379, lower final split x1540) instead of blindly dividing equal cells; this preserves trident and wing tips.
- Resources/Moonlit/Forge/TierBands-v1.png: 1942 x 809 ARGB; outer corner alpha 0. Ten colored scenery bars in two columns. Measured crops start x24/983 and y32/178/327/475/624, each 936x134. Runtime 9-slice borders are (40,12,40,12), while icons, Korean tier names, gold stars and probability numbers are independent controls.
- All sprite regions, borders and pixel density scale to actual loaded texture resolution. No dependency on original PNG resolution at runtime.
- Original main crown coin and ruby sprites are reused for the probability wallet; live main wallet text is used when present.

## Limits

New artwork and original reference were visually inspected; static diff checks pass. Cloud import/navigation tests and actual 9:16/9:19 captures remain required. Other forge screens still contain placeholder glyphs/backgrounds and repeated equipment illustrations. This checkpoint does not claim all forge or all 24 screens complete. Source image alpha gutters are preserved; generated scene composition is inspired by the reference rather than identical.
