# Progression capture visual review — 2026-09-18

Reviewed the actual hosted Unity captures in artifact 10524904383, both 9:16 and 9:19 for skills-pets-heroes, skill-details, summon-probability, summon-probability-details, dungeons, dungeon-details and summon-result (14 original-resolution PNGs). The baseline gameplay suite passed 94 tests; this report does not claim a new post-change Unity run.

Two visible corrections:
- The dark 장착됨 lettering on pale parchment had the default black outline, visibly filling/merging the glyphs. Restored the light subtle outline used by the earlier parchment label.
- Per-entry percentages in summon-probability-details crossed the lower gold rim. Added 20 logical units of card height and matching row pitch/content height, keeping text/icon dimensions and real scrolling unchanged.

The partially visible third collection row is the intended ScrollRect crop, not hidden unreachable content. Other reviewed dialog/button labels remain readable in both ratios; typography REVIEW logs alone did not justify resizing them.

Both summon-result captures show the explicit no-payload empty state. They verify that fallback layout only. Paid five-/ten-item result captures remain a separate visual evidence gap; functional summon tests do not substitute for those captures. Coordinator notified. No local Unity execution/control and no bitmap editing.
