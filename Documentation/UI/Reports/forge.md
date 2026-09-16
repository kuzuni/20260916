# Forge / equipment screen module report

## Scope delivered

`ForgeScreenModule.Register(UiScreenRegistry)` registers eight runtime uGUI modal routes:

| Route | Reference | Runtime behavior |
|---|---:|---|
| `forge-probability` | 01 | Ten colored probability bands, live level columns, timer bar, insufficient-ruby skip feedback, and nested information button. |
| `forge-probability-details` | 02 | Real vertical `ScrollRect`, five categorized groups, reusable item cells, and item-detail navigation using `ItemDefinition` payloads. |
| `forge-item-details` | 03 | Payload-aware item icon/name/description plus the full possible-stat range list. |
| `progress-pass` | 05 | Real vertical `ScrollRect`, two-column free/premium reward track, per-stage one-time local claims, and an explicitly disconnected premium purchase control. |
| `equipment-details` | 08 | `EquipmentSlot` payload support with separate icon, level, rarity frame, and live item detail text; deterministic demo fallback. |
| `forge-comparison` | 09 | Separate equipped/new item cards and mutually exclusive, one-time local sell/equip decision. Backdrop dismissal is disabled. |
| `offline-rewards` | 10 | Live reward text and a session-stable, one-time claim control. Backdrop dismissal is disabled. |
| `auto-forge` | 12 | Interactive rarity and stat `Toggle` controls, bounded hammer quantity controls, continue option, and validated local start feedback. |

The implementation is runtime construction only. It uses the shared `ScreenContext.Root` logical safe-area surface, bounds each centered dialog to the current safe logical height, and leaves full-viewport dim/input interception to the shared modal host per the frozen contract. Long probability and pass content scrolls rather than reducing text to an unreadable size.

## Visual implementation

All eight original PNGs were opened at original resolution before implementation. The module reproduces their dark inset stone panels, nested bronze/gold rules, central diamond ornaments, cobalt actions, crimson decision/close treatment, rarity-colored probability bands, separate item images, dense grids, and two-column reward track. Text and controls remain live uGUI objects; no reference PNG is included under `Assets` or used as a runtime background.

Existing `MainScreenAssets.equipmentIcons` and `ItemDefinition.icon` sprites are reused as separate item artwork. No new bitmap was generated because the shared catalog already provides production equipment imagery usable by these screens. Reference-only art not present in the shared catalog remains approximated with runtime vector-like uGUI layers and glyphs: the many unique catalogue items, pass treasure chests, hammer/coin reward paintings, and bespoke large filigree corners. These are explicit visual-fidelity gaps rather than baked screenshots or claimed finished illustrations.

## Coordinator integration hooks

After the shared routing foundation lands, the coordinator should call:

```csharp
ForgeScreenModule.Register(registry);
```

Expected main-screen route hookups are:

- forge level button → `Open("forge-probability")`
- anvil button → `Open("forge-comparison")`
- automatic forge button → `Open("auto-forge")`
- each equipment slot → `Open("equipment-details", slot)`
- left offline timer → `Open("offline-rewards")`
- former fairy / pass button → `Open("progress-pass")`

No shared/bootstrap file was changed in this feature branch.

## Checks actually run

- Inspected references 01, 02, 03, 05, 08, 09, 10, and 12 at original resolution with the container image viewer.
- Confirmed repository base is commit `7b244bc` and project editor version is `6000.3.8f1`.
- Ran `git diff --check` successfully.
- Checked added GUIDs for format and uniqueness against tracked `.meta` files.
- Checked source structure for all eight exact route registrations and required `ScrollRect` / `Toggle` controls.

## Unrun acceptance checks and blockers

Unity runtime compile, modal-stack input tests, and 1080×1920 / 1080×2280 safe-area captures were **not run**. This isolated feature baseline intentionally predates the parallel foundation implementation of `UiScreenRegistry`, `ScreenContext`, `ScreenPresentation`, and the cloud CI workflow, so the module cannot compile or be routed until that shared work is integrated. No local Unity editor, Unity MCP, user machine, or local runner was used. The static checks above are not represented as a Unity pass.

After foundation integration, cloud CI must verify top-only back behavior, no click-through, parent scroll restoration after item details, one-time comparison/reward decisions, safe-area resizing, and captures for every route at both required aspect ratios.
