# Progress pass reward artwork

## Generation and sources

Original References/05-progress-pass.png was inspected at full resolution. Built-in imagegen generated both assets on 2026-09-17. PNG bytes were copied unchanged into Assets/DarkFantasyUI/Resources/Moonlit/Forge with unique import GUIDs.

- PassChests-v1.png: exec-69b56ca0-0e93-4ea4-bf86-4fe10b3454c5.png, 1536x1024 RGBA atlas, four 768x512 cells. Corner and center gutter alpha are 0. Visually inspected: red, cyan, amber and crown chests, no text or lock badges. First four rows match this order; the two extra demo stages reuse the crown chest because no original art is supplied for them.
- ClaimedCheck-v1.png: exec-bf52c7a1-4702-4b51-bb00-dd9767faa304.png, 1254x1254 RGBA, corner alpha 0. Visually inspected: single green beveled check, no frame/text.

## Exact chest prompt

Use case: stylized-concept. Create a reusable 2x2 sprite atlas of FOUR separate reward treasure chest icons for a Korean dark fantasy mobile RPG, using the four premium reward chests down the RIGHT column of the provided screenshot as visual reference only. Top left: closed blue-black iron chest with gold straps, red ruby clasp and a few red gems scattered below. Top right: closed dark blue chest, bronze corner armor, bright cyan-blue diamond clasp. Bottom left: dark iron chest with ornate curling bronze trim and a large amber/red diamond clasp. Bottom right: stout black and gold chest with a prominent golden crown emblem on its front, a few coins below. Three-quarter front view, matching hand-painted inked fantasy illustration with crisp bold edges, restrained warm highlights. Each icon large and centered in its own equal cell with generous consistent transparent gutters. Exactly four icons, no cell borders, no frames, no text, no padlock badges (locks are rendered separately), no UI, no scenery, no extra separate reward symbols. Transparent RGBA background everywhere outside each chest silhouette, no checkerboard pixels. Atlas landscape 1536x1024, equal 768x512 cells. Keep every chest within its cell, no touching or overlap. Preserve the reference's distinct red, cyan, amber, and gold progression; these are reward illustration assets, not a screenshot recreation.

## Exact check prompt

Use case: stylized-concept. Generate ONE isolated bright emerald green CHECK MARK game UI icon matching the reward-claimed checkmarks in the LEFT reward cards of the provided fantasy RPG screenshot. Thick bold checkmark, long upward right stroke, short lower-left stroke, slight bevel with pale mint highlight on upper edges, deep forest green shadow and thin near-black outline. Front-facing, angled like a normal check mark, crisp hand-painted illustrated mobile-game style. No square or circle frame, no background shape, no text or other icons, no glow extending far outside. Center the single checkmark tightly with small padding on a square canvas. Real transparent RGBA background around the silhouette, no checkerboard pixels. This will be a separate small overlay on claimed reward cards.

## Runtime and validation

Pass card frames, reward amounts, locks and claimed indicators remain separate controls/art. Runtime slices atlas cells using imported texture dimensions and preserves aspect ratio; session reset invalidates cached sprite references. Claiming hides the action button and reveals a non-interactive check; opening again restores that state. This retains the local-only single-claim behavior, not backend rewards or premium purchases.

Existing pass regression extended for four distinct chest regions, duplicate-claim handling and restored check state on reopen. Hosted capture loop additionally saves claimed pass screenshots at both aspect ratios. Static diff check passed; new-source Unity and visual verification pending. No local Unity execution.
