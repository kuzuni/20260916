# Forge scenario review and fixes

Reviewed integrated 4cb817c9ee35f40d5fede5bd19d29924fcf0069a independently against the user's forge scenarios.

Confirmed existing shared rules implement: 35-level cap, primitive-only at level 1, celestial 4% at level 35, exactly impossible low/high rarity zeros, separate draw counters per rarity capped at 100, zero/one/two affixes at the requested thresholds with unique kinds/ranges, empty-slot equip only, repeated equip swaps, and unequipped-only sale.

Found and fixed:
- Free upgrade skips used UTC midnight while dungeon keys used Korean midnight. ResetDaily now uses UTC + 9 hours consistently.
- Stopping auto forge from its settings dialog while a batch animation was running immediately opened comparison over transient pending results. This allowed premature equip actions before filtering completed. Stop now defers comparison until the runtime completes its ordered animation/reveal/settlement.
- Auto sale settlement is now centralized and idempotent (CompleteAutoBatch), with a guard against selling an equipped item.
- Twenty-two cards previously overlapped by about two-thirds. They now use two hands of 11 cards with exact half-width overlap. Larger batches scale to bounded multirow hands.
- Sale effects now animate eight copies of the existing main HUD crown-coin sprite alongside the floating gold text, with no raycast targets.
- NormalizeAfterLoad removes JsonUtility-created id-zero phantom equipment, invalid items and duplicate identities; rebuilds fixed arrays, normalizes ranges/affixes/counters, retains valid pending results and reconstructs nextId. It pauses automatic forging after loading. Coordinator must invoke this after deserializing ForgeState before SyncSlots.

Regression tests added: Korean-versus-UTC midnight, exact 22×3 batches retaining 10+11+5=26, grade plus zero/one/two-affix filtering, malformed/empty save normalization, and settings-stop during an active animation. Existing batch flow test now asserts half overlap and actual coin sprites. Original rarity/counter/affix/equip tests remain.

No MainScreen files changed. Tests are committed for cloud Unity execution; this worker did not execute local Unity or claim runtime success.
