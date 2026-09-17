# Dungeon title plaque

## Source and generation

Original reference: References/20-dungeons.png, inspected at full resolution. Only its upper title plaque was used as shape/style reference. Generated with built-in imagegen on 2026-09-17; output exec-319e2cec-39a7-4d8a-ab3a-c2fe16f95bff.png copied unchanged to Assets/DarkFantasyUI/Resources/Moonlit/Dungeons/DungeonTitle-v1.png with a unique single-sprite importer.

Exact prompt:

Use case: stylized-concept. Asset type: reusable transparent game UI title frame. Use the supplied screenshot only as a STYLE AND SHAPE REFERENCE for the small DUNGEON TITLE PLAQUE at the very top center. Generate ONE isolated empty title plaque, front facing: wide dark charcoal/navy textured stone or worn black leather center, aged bronze/gold double beveled angular outline, pointed symmetrical side corners, a small central skull crest on the upper rim surrounded by sharp bronze rays/thorns and a short central spike, small downward diamond flourish on the lower rim. Match that top plaque's illustrated dark fantasy inked mobile game style, ornate but readable at small size. Broad EMPTY central horizontal face for a separate live Korean heading. Approximately 2.2:1 aspect ratio including the top crest. Only this single empty plaque, centered, tight padding, all decoration fully within the image. Transparent RGBA background around silhouette. No text whatsoever, no letters, no rune writing, no scene, no character, no other UI, no button rows, no multiple designs, no checkerboard pixels or solid background. Keep skull compact above the blank text area; bronze linework, black center, no glowing blue gem.

## Inspection and integration

1860 x 846 PNG, 32-bit ARGB; corner alpha 0, center alpha 253. Visually inspected: a single empty dark plaque with bronze rim, compact skull/spike crest and pointed lower flourish. No text or surrounding UI baked into the image. Artwork remains unchanged and retains generated alpha.

BuildDungeons renders the preserved-aspect plaque at 360 x 164 logical units, centered within SafeArea. Korean heading is a separate live Text over the empty face; both are non-interactive. Existing reset notice, row cards, scroll, navigation and child dialogs keep their positions and behavior. New-source hosted Unity and screenshot verification remain pending; no local editor execution.
