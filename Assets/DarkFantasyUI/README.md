# Moonlit — dark fantasy main screen

Unity 6000.3 / uGUI, portrait 1080 × 1920. Open `Scenes/MoonlitMain.unity` and press Play. The original SampleScene is preserved. `Moonlit > Build Main Screen` regenerates the new scene, data and reusable prefab; it should only be used when intentionally resetting this UI's layout.

## Reusable equipment slots

`Prefabs/EquipmentSlot.prefab` contains separate children for:

- **Frame:** the orange slot in `Art/InterfaceFrames-v2.png`, or `Art/CompanionFrame-v2.png` for companions; both are empty reusable 9-slice backplates.
- **Item icon:** a transparent sprite from `Art/EquipmentIcons-v2.png` (9 individually trimmed sub-sprites).
- **Level / rarity star:** live UI Text elements.
- **Lock / notification / selection:** independent visibility layers.

`Data/Item_00.asset` through `Item_08.asset` are ScriptableObject item definitions containing an icon, name, rarity, starting level and description. The prefab itself has no item assigned, and all nine scene slots are instances of that one prefab. The companion slot stretches the same frame to double width while preserving icon aspect ratio.

```csharp
slot.Bind(itemDefinition, itemLevel: 108, locked: false, notify: true);
slot.SetLocked(true);
slot.SetSelected(true);
slot.SetNotification(false);
slot.Bind(null); // clears previous icon, level, badges and selection
slot.Clicked += selectedSlot => Debug.Log(selectedSlot.item.displayName);
```

Assign a different sprite to an item's `icon` field to reuse the slot with any future artwork. Rarity selects or tints the frame independently; the item icon is never tinted. PortraitSafeArea fits the 1080 × 1920 design within device safe areas and adds letterboxing on wider displays.

## Local UI interactions

- Select a slot: inspect, lock/unlock, upgrade.
- Anvil button: standalone transparent foreground artwork; spend 100 stones and upgrade one unlocked equipment item.
- Forge level button: separate management dialog, with no resource spending.
- Auto: repeat forging every 1.4 seconds; pause while a dialog is open; stop if resources are insufficient.
- Stage: explicitly labeled local completion simulation.
- Event / fairy: one claim each per session.
- Navigation: equipment, dungeon, companion, quest reward, gold-to-stone exchange.
- Wallet/profile/chat: local information dialogs. No server, live chat, combat simulation, payments or persistent account state are connected.

## Artwork

Production images were generated using the built-in image generation tool. Original prompts are in `Art/GENERATION.md`; reference-fidelity revision prompts are in `Art/GENERATION-v2.md` and `Art/CompanionFrame-v2-prompt.md`. New files use a v2 suffix, preserving the first-pass artwork. All project references use local assets. No reference screenshot is baked into the UI. The environment painting includes the hero and companions; animated embers are a separate UI layer.

Noto Sans KR and Noto Sans CJK KR Bold are bundled under the SIL Open Font License, included in `Fonts/OFL.txt`.

## Verification

`Moonlit > Capture Main Screen` writes `Artifacts/MoonlitMain.png`. The editor's one-shot local command runner accepts `build`, `capture` or `verify` in `Library/Moonlit.command`. `verify` runs a Play mode smoke check and writes `Artifacts/Verification.txt`. It checks costs, locked-slot exclusion, auto toggle, dialogs, navigation, clearing/rebinding a reusable slot and insufficient-resource handling.

## Reference fidelity revision

Event buttons are frameless. Navigation uses five large icons on one shared stone panel without individual boxes or labels. Currency icons use a round crown coin and tall diamond ruby; the shop uses a striped awning. Chat includes a speech balloon and live text with a 99 badge. Forge management and auto use silver-beveled cobalt blue backplates; auto shows a circular-arrow icon that rotates while active. The anvil has its own clickable silhouette and press feedback. All decorative layers ignore raycasts. Verify also checks actual pointer hit targets for the anvil, forge management, auto, events, chat and navigation.

