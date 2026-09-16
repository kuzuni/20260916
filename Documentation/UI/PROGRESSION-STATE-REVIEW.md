# Progression live-state review

## Scope reviewed

This bounded repair covers only `ProgressionScreenModule` and its dedicated PlayMode regression
tests. Original references 14 (skill details), 17 (summon result), and 19
(skills/pets/heroes) were inspected at full resolution before editing. The existing runtime-built
uGUI, page/modal routing, safe-area host, mobile navigation, quick-equip ordering, and pet/hero
selection behavior remain in place.

## State repairs

- Skill details now receives the preserved collection view as part of its local payload. Upgrade
  changes the actual skill level and refreshes visible parent slot/equipped-row labels. Equip changes
  the three-entry equipped-skill model and redraws only the equipped row. Neither operation rebuilds
  the collection scroll or changes its selected tab.
- Summons use a deterministic local sequence. Each paid five-card decision creates a fresh result
  set, increments collection shards, unlocks newly obtained skills, refreshes the preserved parent
  currency/collection, and rerenders the current result cards. This is local demo state only; it
  makes no backend, purchase, or random-service claim.
- The originating summon control is blocked by the host's covered-page CanvasGroup and guarded
  against same-frame repeats. This also restores input after system Back. Result decisions reject duplicate invocations in the same frame, while later deliberate
  summons remain available. Destroyed result/card controls have their listeners removed so stale
  references cannot resolve another decision.

Coordinator review: merged with the new dungeon paintings using a three-way merge, preserving all four art routes and proportional cropping. Corrected a stuck summon button after system Back, refreshed parent shards immediately so any close path sees new state, refreshed the detail slot level, guarded unowned equip/upgrade, filtered quick-equip to owned entries, and prevented direct result-preview routing from granting free shards. Added a system-Back/next-paid-summon regression. All new runtime tests remain pending hosted CI.

## Regression coverage and status

`ProgressionStateTests.cs` drives the real runtime-created Buttons and ScrollRect. It checks that a
child upgrade/equip is visible in the preserved parent after close without losing tab/scroll state,
and that initial plus repeated summons deduct exactly two costs, change result cards, unlock the
collection item, refresh currency, and ignore a same-frame duplicate click.

Static checks are recorded in the change summary. Per project policy, Unity was not launched in this
container. Unity 6000.3.8f1 compilation and PlayMode execution remain **unrun** and must be performed
by coordinator-owned hosted CI; no runtime pass is claimed here.

## Artwork limitations

No artwork was generated or changed in this task. The independent runtime frames/glyph layers remain
functional drafts, but the bespoke illustrated skill icons and summon reveal paintings visible in
references 14, 17, and 19 are still missing. Those coordinator-owned assets are required before the
screens can be described as visually finished; no generic glyph is being reported as final art.
