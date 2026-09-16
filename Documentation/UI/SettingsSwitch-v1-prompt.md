# Settings metallic switch v1

Generated with the built-in image_gen tool after full-resolution inspection of reference 07 and the previous hosted settings capture. Source is an unmodified 1942 x 809 ARGB PNG with alpha 0 at the outer corner.

## Exact prompt

Use case: stylized-concept. Asset type: transparent sprite sheet of THREE separate components for a dark gothic RPG settings toggle. Layout: top half has two identical-sized horizontal capsule tracks side by side, with plenty of transparent gutters. Left track is OFF: completely empty dark obsidian interior with a thin polished silver bevel rim and black outer outline. Right track is ON: same identical rim and geometry with luminous emerald green empty interior. BOTH tracks must be EMPTY, NO KNOBS inside them. Bottom half, centered: ONE separate perfectly circular cyan blue luminous glass thumb with its own thin silver metallic bezel and small white specular highlight. Hand-painted fantasy mobile game UI shading, readable at small size, straight-on front view, simple rounded ends, subdued metal, no gothic spikes. Track silhouette aspect ratio 2.4:1; thumb circle diameter about 75 percent of track height. Actual transparent background outside these three parts, no checkerboard, no black background, no labels, no text, no numbers, no symbols, no sample assembled controls. Keep every component fully inside the canvas with ample empty space around it so runtime can slice and move the thumb independently. This is one asset sheet, not a screen mockup.

## Runtime binding

Resources/Moonlit/Social/SettingsSwitch-v1.png contains separate empty off/on tracks and a circular cyan thumb. Top-left source crops are (96,122,830,286), (1015,122,830,286) and (834,462,276,276), measured from nontransparent bounds with padding. All crop coordinates, border widths and pixel density scale to the actual imported texture dimensions. Track end-cap borders (142,0,142,0) and pixel multiplier 5.26 keep rounded end proportions at 128x54. The separate 42x42 thumb moves between x6 and x80.

The real Toggle root remains the only raycast target. Track and thumb artwork are noninteractive children. Toggle.graphic stays null so off state does not fade the thumb away. Existing session persistence and tabs are retained. The state regression now checks imported off/on/knob art, distinct regions, green-track replacement and independent hit targets.

## Validation limits

Original reference, prior capture and source atlas were inspected; static diff checks passed. New cloud test and graphics results are pending. No local Unity was run. This does not yet claim final physical-device or reference-fidelity acceptance.
