# Progression module implementation report

## Scope and registrations

`ProgressionScreenModule.Register(UiScreenRegistry)` registers the seven assigned routes without changing foundation or bootstrap files:

| Route | Presentation | Reference |
|---|---|---|
| `skills-pets-heroes` | Page | 19 |
| `skill-details` | Modal | 14 |
| `summon-probability` | Modal | 15 |
| `summon-probability-details` | Modal | 16 |
| `summon-result` | Fullscreen | 17 |
| `dungeons` | Page | 20 |
| `dungeon-details` | Modal | 04 |

The module creates uGUI at runtime under `ScreenContext.Root`. It uses bounded panels and dimensions derived from `ScreenContext.Height`, leaving safe-area ownership to the host contract. Both lists use real `ScrollRect`/`RectMask2D` viewports. All buttons have local behavior: tabs redraw in place, slots open a payload-specific detail, upgrades change the demo level, quick-equip reports its local result, summon checks/deducts local currency, probability details stack above probability, dungeon rows pass the selected dungeon, and dungeon entry/sweep explicitly identify themselves as local demo actions.

## Integration hook

After the foundation task is integrated, the coordinator must call:

```csharp
ProgressionScreenModule.Register(registry);
```

from its registration/bootstrap location. No shared file or bootstrap was edited here, as required by the ownership contract.

## Reference and artwork notes

All seven original PNGs were inspected at original resolution before implementation. They are not imported or used as runtime backgrounds. Existing `MainScreenAssets.worldBackground` and its independently sliced equipment/interface icon catalog are reused. Frames, fills, selection surfaces, labels, progress bars and close controls are independent runtime objects.

The supplied reference has only the skill tab. The pet and hero collections are deliberately inferred from the same round-slot, bronze-frame, dark-stone visual vocabulary; the required third label is `영웅`, not `기술 트리`. Their content is deterministic local demo data.

The environment exposed no built-in `image_gen` tool, so new bespoke skill and dungeon bitmap illustrations could not be generated. Runtime rune emblems and existing local icon sprites are used instead. This is an explicit visual-fidelity limitation, not represented as finished bespoke art; no screenshot was pasted into the product.

## Verification performed

- Confirmed the branch starts at `7b244bc` and only progression-owned paths were added.
- Checked route registrations, presentation types, `ScrollRect` construction, Korean tab labels, five-result loop, four-dungeon loop, backdrop-dismiss settings and LF/meta GUID hygiene with repository scripts/commands.
- Unity 6000.3.8f1 is not installed in this cloud container and no usable cloud Unity/license dispatch endpoint is configured in this branch. Consequently no Unity compile, Play Mode test, interaction capture, 9:16/9:19 capture or safe-area capture was claimed.

## Required cloud follow-up

After foundation integration, run the repository's cloud Unity 6000.3.8f1 workflow and capture all seven routes at 1080x1920 and 1080x2280, including top/bottom/side safe insets. Exercise parent tab and scroll restoration through both probability layers, top-only back behavior, full-screen summon return, insufficient summon currency, each dungeon payload, backdrop blocking and navigation click-through. Evidence should be stored under the coordinator's artifacts path and linked from the final integration report.
