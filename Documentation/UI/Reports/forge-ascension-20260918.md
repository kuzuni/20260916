# Forge ascension revision — 2026-09-18

## Implemented
- Forge level 35 now follows the same gold segments → timed upgrade → explicit completion flow. Its completion button is 승천하기.
- Claiming ascension increments the saved ascension and resets forge level to 1, all six equipped items, pending items, frozen automatic-sale IDs, comparison identity and per-grade draw counts. Auto forging stops and transient hammer/reveal/sale objects are cancelled. Wallets, collection progress, daily skip allowance and monotonically increasing equipment IDs remain intact.
- Equipment stores its own ascension. Primitive level 1 after ascension is exactly twice the prior ascension's celestial level 100 for HP, attack and speed. Original linear HP anchors remain 80 → 880 → 1760. Collection callers use the unchanged default ascension-zero baseline.
- Extreme stats saturate at double.MaxValue / 1024 per piece to retain finite arithmetic headroom; ordinary ascensions follow the requested exact progression.
- New marker follows the new roll's identity when the cards swap, including after save/reload. Resolving the comparison advances the marker to the next pending roll.
- Ascension zero has no star. All equipment cards, main slots, forge rarity rows and catalogue entries show numeric ★ n for positive ascension.
- Catalogue frames are square. Probability headers restore the reference subtitle, two independent currency icons/counts, and level columns. The explanatory skip-rate footer is removed.

## Reference review
Full-resolution original References/01-forge-probability.png, 02-forge-probability-details.png and 09-forge-comparison.png were inspected before editing. Existing frame, icon and tier-band art is reused.

## Verification
Added rule tests for upgrade prerequisites/reset, successive ascension stat boundaries, old item identity after JSON restore and new-roll identity across toggles/save. Added UI tests for marker movement, cancelling an in-flight forge on ascension, square catalogues, saved item stars, main-slot stars, currency headers and zero-ascension star hiding. Existing ordered forge/equip/sale/auto tests remain enabled.

No local Unity was run. Cloud Unity 6000.3.8f1 compilation, test execution and both-aspect screenshots are pending coordinator integration.
