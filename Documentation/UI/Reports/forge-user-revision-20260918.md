# Forge user correction — 2026-09-18

The user's explicit equipment anchors supersede compound growth: primitive health level 1 = 80, level 100 = 880, medieval level 1 = 1760. Attack uses the same scale (10 → 110 → 220). Levels interpolate linearly from 1× to 11× across the 99 intervals between level 1 and 100; this preserves the explicit endpoints despite the approximate “10% per level” wording. Each rarity starts at 22× the previous rarity's level 1. Collections and skills consume the same shared EquipmentRules baseline. Speed remains +1 per level with the next rarity starting at twice the previous level 100.

Source changes are cloud-only; local Unity is prohibited. Regression assertions cover exact anchors, constant increments and all ten rarity boundaries. Cloud tests and updated captures are pending.
