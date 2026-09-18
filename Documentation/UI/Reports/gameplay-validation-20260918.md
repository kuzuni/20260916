# Gameplay validation — 2026-09-18

Implementation baseline: `8bbb4bf77f6682f948d37ebc177408ef8edaefc8`.
Hosted Unity 6000.3.8f1 [run 35288626760](https://github.com/kuzuni/20260916/actions/runs/35288626760) compiled and executed PlayMode tests: **94 passed, 0 failed, 0 skipped, 0 inconclusive**.

Coverage includes per-grade gear levels/rarity windows, affix uniqueness, forge phases and skip costs, animated manual/automatic batches, swap/sell semantics, stop-during-forge, save/restore normalization, permanent collection unlocks and fragments, independent summon XP, dungeon keys/refunds/midnight refresh, combat event gating, actual Player sprite swaps, 15-round timeout, dungeon return, rewards/pass, shop quantities and modal-stack navigation.

Generated combat assets are checked in. The six celestial armor/weapon crops and all 18 imported celestial thumbnail settings were recovered from cloud artifact `10524938405`; original Art sources were not changed.

Graphics capture job is pending at the time of this progress integration. A test pass alone is not visual acceptance. No local Unity editor execution or control occurred.

Earlier runs:
- 35286902632: compiled; 51 passed / 23 failed. Obsolete demo assertions and arena null reference were addressed.
- 35287941458: compiled; 80 passed / 2 failed. Empty serialized gear and catalog button identity were fixed.

Deferred by user: pet/mount illustrations, non-primitive skill effects, emblem/wings/spirit functionality. Arena opponents and shop grants are local demos.
