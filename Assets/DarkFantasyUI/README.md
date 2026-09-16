# Moonlit — dark fantasy main screen

Unity 6000.3 / uGUI. Open `Scenes/MoonlitMain.unity` and press Play.

## Runtime generation

The saved scene contains exactly one `Moonlit Runtime Bootstrap` object, with a `MainScreenBootstrap` component and a reference to `Data/MainScreenAssets.asset`. No Canvas, camera, UI buttons or equipment slots are pre-placed in the scene. `Awake` creates the camera, Canvas, HUD, inventory, forge, chat, navigation and EventSystem. Stopping Play mode removes that runtime hierarchy.

- `MainScreenBootstrap.Build()` is idempotent: calling it again returns the existing screen.
- `RuntimeMainScreenFactory.cs` and `RuntimeMainScreenLayout.cs` contain the runtime construction code; they have no UnityEditor dependencies.
- `MainScreenAssets` holds serialized references to the generated artwork, fonts, item data and the reusable slot prefab. Artwork is bundled locally; no image-generation service is called at runtime.
- `Moonlit > Build Main Screen` is a development setup command that refreshes the asset catalog/prefab and saves the bootstrap-only scene. Normal launches need only Play.

## Play startup and runtime performance

Enter Play Mode skips domain reload and retains scene reload, so the bootstrap still rebuilds a fresh runtime hierarchy. `MoonlitRuntimeSettings` resets session-only progression, forge and profile state through `SubsystemRegistration`, including atlas arrays that can retain destroyed sprites between runs. It requests 60 FPS with VSync disabled and enables background execution before the first scene loads. This is a frame-rate target, not a guarantee on slower hardware; mobile operating systems may still suspend background applications.

The optional Unity MCP server no longer starts automatically (`ProjectSettings/McpUnitySettings.json`). Read-only local logs showed repeated port 8090 bind errors on startup, which can trigger the Console's Error Pause. The toggle itself and the user's actual Play transition have not been controlled or verified. Restarting the editor once reloads cached MCP configuration if an already-running server keeps retrying. Cloud Unity validation is required; local editor execution is prohibited for the agent.

## Responsive portrait layout and Safe Area

Both 9:16 (1080 × 1920) and 9:19 (1080 × 2280) use a 1080-unit logical width. The HUD anchors to the safe top, and the equipment/forge/chat/navigation block anchors to the safe bottom. Additional height expands the battle viewport; buttons retain their proportions. The scenery fills the screen and is cropped proportionally instead of stretched.

`PortraitSafeArea` reads `Screen.safeArea` and the current screen dimensions before rendering and on each update, so OS-reported camera cutouts, notches, home-indicator insets and size changes are respected. Interactive controls and centered dialogs live inside that safe rectangle; decorative scenery may extend behind system areas. There is a compact-layout fallback for unusually short viewports. Preview overrides exist only in Editor builds.

## Shared page and popup routing

`UiScreenRegistry` is the integration boundary for feature modules. Runtime construction creates
separately sorted main and navigation canvases plus an on-demand sibling popup stack. Opening a
page replaces the current page and closes its dialogs; opening a modal pushes a new independently
blocked layer. Back/close pops only the top layer, restores its opener, and preserves the parent
object (including its tab, scroll and selection state). Every open layer follows Safe Area changes.
See `Documentation/UI/FOUNDATION-INTEGRATION.md` for the coordinator hook and frozen route keys.

## Reusable equipment slots

`Prefabs/EquipmentSlot.prefab` contains separate children for:

- **Frame:** the orange slot in `Art/InterfaceFrames-v2.png`, or `Art/CompanionFrame-v2.png` for companions; both are empty reusable 9-slice backplates.
- **Item icon:** a transparent sprite from `Art/EquipmentIcons-v2.png` (9 individually trimmed sub-sprites).
- **Level / rarity star:** live UI Text elements.
- **Lock / notification / selection:** independent visibility layers.

`Data/Item_00.asset` through `Item_08.asset` are ScriptableObject item definitions containing an icon, name, rarity, starting level and description. The prefab itself has no item assigned, and all nine runtime slots are instances of that one prefab. The companion slot stretches the same frame to double width while preserving icon aspect ratio.

```csharp
slot.Bind(itemDefinition, itemLevel: 108, locked: false, notify: true);
slot.SetLocked(true);
slot.SetSelected(true);
slot.SetNotification(false);
slot.Bind(null); // clears previous icon, level, badges and selection
slot.Clicked += selectedSlot => Debug.Log(selectedSlot.item.displayName);
```

Assign a different sprite to an item's `icon` field to reuse the slot with any future artwork. Rarity selects or tints the frame independently; the item icon is never tinted. PortraitSafeArea adapts the logical height to the safe viewport while preserving slot aspect ratios.

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

Main combat scenery uses the dedicated `Resources/Moonlit/Main/ForestBattle-v1.png`: a moonlit conifer forest and horizontal dirt trail, with the existing three-character party composition. Other pages/cards retain `MainScreenAssets.worldBackground`. The battle image follows the same elastic crop and does not change HUD/forge/navigation layout. Source reference and exact built-in imagegen edit prompt: `Documentation/UI/ForestBattle-v1-prompt.md`.

Production images were generated using the built-in image generation tool. Original prompts are in `Art/GENERATION.md`; reference-fidelity revision prompts are in `Art/GENERATION-v2.md` and `Art/CompanionFrame-v2-prompt.md`. New files use a v2 suffix, preserving the first-pass artwork. All project references use local assets. No reference screenshot is baked into the UI. The environment painting includes the hero and companions; animated embers are a separate UI layer.

Noto Sans KR and Noto Sans CJK KR Bold are bundled under the SIL Open Font License, included in `Fonts/OFL.txt`.

## Verification

`Moonlit > Capture Main Screen` or the `verify` command in `Library/Moonlit.command` enters Play mode, verifies the runtime-built UI, writes six screenshots to `Artifacts/Runtime-*.png`, writes `Artifacts/Verification.txt`, and returns to Edit mode. The saved scene remains bootstrap-only.

The cases cover 9:16 and 9:19 without cutouts, both sizes with simulated top/bottom insets, additional side insets, and returning to 9:16. Each case checks all 25 button hit targets, button/text bounds, safe-centered dialogs, fixed control proportions and idempotent initialization. Runtime interactions also check slot clearing/rebinding, forge resource costs, locks, automatic forging and navigation. These are Editor simulations, not physical-device tests.

## Reference fidelity revision

The main reward entries use dedicated transparent sprites: ProgressPassIcon-v1 (blue sword/pass pennant) and OfflineRewardIcon-v1 (clock/reward chest), following the user's explicit subject change. Their timer text and invisible hit surfaces remain separate. Generation prompts: Documentation/UI/MainRewardIcons-v1-prompts.md.

Event buttons are frameless. Navigation uses five large icons on one shared stone panel without individual boxes or labels. Currency icons use a round crown coin and tall diamond ruby; the shop uses a striped awning. Chat includes a speech balloon and live text with a 99 badge. Forge management and auto use silver-beveled cobalt blue backplates; auto shows a circular-arrow icon that rotates while active. The anvil has its own clickable silhouette and press feedback. All decorative layers ignore raycasts. Verify also checks actual pointer hit targets for the anvil, forge management, auto, events, chat and navigation.
