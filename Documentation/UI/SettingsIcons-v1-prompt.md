# Settings silver icons v1

Generated with the built-in image_gen tool for reference 07. Inspected the full-resolution reference and generated atlas. Original 1983 x 793 ARGB PNG copied without processing; outer corner alpha is zero.

## Exact prompt

Use case: stylized-concept. Asset type: transparent dark fantasy game settings icon atlas, ten separate silver metal icons. Exact layout: 5 equal columns by 2 equal rows, each icon centered in its own square grid cell with generous transparent padding and no overlap. Reading order top row: vibration waveform with five vertical bars; paired musical eighth notes; speaker with two sound waves; speech bubble with three dots; crescent moon. Bottom row: group of three people busts; wireframe globe; single person bust; prohibition circle with diagonal slash; heraldic shield outline. Match ornate gothic mobile RPG UI: hand-painted softly bevelled aged silver/iron, subtle dark outer contour, white metallic edge highlights, readable chunky silhouettes. Restrained detail, consistent size and material across all ten. Actual transparent background between and behind icons, no checkerboard, no grid lines, no labels, no text, no numbers, no buttons, no frames, no gold coins or colored gems. Atlas width-to-height 5:2. These will be sliced into ten independent icon sprites, not shown as one sheet.

## Runtime integration and limits

Assets/DarkFantasyUI/Resources/Moonlit/Social/SettingsIcons-v1.png contains five columns and two rows. SocialScreenModule slices ten sprites using the actual imported texture dimensions so platform rescaling is supported. Each icon is a separate non-raycast Image beside live Korean labels. Switches and four transparent list-row Button hit targets are independent. The four lower links retain local-demo feedback and do not perform account or messaging actions.

Settings now uses ten consistent rows and thin rules instead of four oversized blue action backplates. Selected profile/settings tabs keep the shared blue backplate. Frame dimensions and current switch implementation are unchanged. Hosted screen captures at both aspect ratios remain required before final visual acceptance.
