# Main reward button icons

User explicitly selected functional icons on 2026-09-17: progression pass = sword/pass pennant; offline rewards = clock/reward chest. This overrides the original fairy/brazier subjects while preserving frameless buttons, placement and separate live timers.

Generated using the built-in imagegen tool, one call per icon. Original alpha PNG bytes copied unchanged into Assets/DarkFantasyUI/Resources/Moonlit/Main. Each uses a unique single-sprite TextureImporter .meta. Source artwork was visually inspected and transparent corner alpha verified; hosted Unity rendering remains pending.

## ProgressPassIcon-v1.png

Source: exec-c7521067-e45b-4be4-b1b8-a2a5f6bcb810.png

Use case: stylized-concept. Asset: standalone transparent game HUD icon for a dark fantasy mobile RPG progression pass. One compact cobalt-blue hanging PASS PENNANT made of blue leather with pointed lower end and subtle bronze trim, with a single upright silver sword superimposed centrally, gold hilt at top and blade pointing down, small cyan gem on crossguard. The sword and pennant form one clear recognizable silhouette. Hand-painted 2D game icon, bold clean dark outlines, chunky readable shapes at 96 pixels, restrained bevel highlights, matching an ornate gothic blue/bronze game interface. Entire icon centered, fills 85 percent of square canvas, generous clear alpha margins, no surrounding square/circular button frame. Genuine transparent background. No letters, no text, no numbers, no timer, no label, no UI panel, no fairy, no environment, no cast shadow outside object. Not photorealistic.

## OfflineRewardIcon-v1.png

Source: exec-945d2352-c6cc-457b-adb2-4eef5ad9cc81.png

Use case: stylized-concept. Asset: standalone transparent game HUD icon for OFFLINE REWARDS in a dark fantasy mobile RPG. One compact slightly open dark oak treasure chest reinforced with warm bronze metal, with a few bright crown-style gold coins and one silver ingot visibly collected inside. A large antique round clock is attached in front at lower right, about 45 percent of chest width, ivory dial, two bold dark clock hands, bronze rim, NO numbers or letters. The clock must read immediately as a clock at 96 pixels. Chest plus clock form one compact readable silhouette. Hand-painted 2D game icon with bold dark outlines and chunky readable shapes, warm amber highlights and subtle cyan accent, matching an ornate gothic blue/bronze interface. Entire subject centered fills 85 percent of square canvas, transparent alpha margins, genuine transparent background. No enclosing square/circular button frame, no words, no timer labels, no UI panel, no brazier or fire, no environment, no cast shadow outside object. Not photorealistic.

## Runtime integration

RuntimeMainScreenLayout loads dedicated sprites from Resources. The legacy MainScreen eventButton/fairyButton fields remain for compatibility; their routes are still offline-rewards/progress-pass. Artwork preserves aspect, ignores raycasts and receives press feedback separately from the invisible button hit surface. Time labels remain live text. Hosted verification checks imported sprites, separate hit targets and both routes in addition to existing six main aspect/safe-area captures.
