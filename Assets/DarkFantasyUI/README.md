
> Latest companion direction: live pets/mounts use complete generated PNGs, all quadrupeds standing calmly and facing right, matching the Player's thick-outline cartoon style. No live companion rigging, part separation, Animator or bobbing. Previous rig assets remain archived unchanged; further rigging is stopped. Hosted run125 passed285 tests; actual whole-image placement was reviewed at both ratios. Remaining visual limitations are recorded in Documentation/UI/Reports/runtime-validation-20260919.md. See the current game rules and Flat-Primitive art provenance files.

# Moonlit — dark fantasy main screen

> 2026-09-18 gameplay update: [current game rules](../../Documentation/Gameplay-20260918.md) supersede screenshot/demo economy, equipment, collection, combat and reward values. The 30 reference routes and safe-area contracts remain in effect. The latest revision expands skills to all 30 era-themed entries with generated sprites; six primitive pet/mount whole-PNG sprites matching the Player style are included; higher-era companion art remains deferred. The separated-part rigs committed in PR94 are archived and are not used by live companions. Latest evidence and remaining visual corrections are recorded under Documentation/UI/Reports. Follow the [Korean gameplay quickstart](../../Documentation/Gameplay-Quickstart-ko.md) for current interactions.

Unity 6000.3 / uGUI. Open `Scenes/MoonlitMain.unity` and press Play.

## Latest gameplay corrections — 2026-09-18

- Equipment grows linearly between the user's exact endpoints: primitive health equipment has HP80 at level1 and HP880 at level100; medieval level1 has HP1760. Attack uses the same scale (10 → 110 → 220). The shared six-piece EquipmentRules baseline also drives collection ownership/equipment bonuses and fixed skill values.
- All ten eras now have three named skill entries (buff, weak attack, strong attack), generated sprites, descriptions and previews. Collection icons and combat effects share SkillCatalog resource keys: the first two eras use the latest six directed sequences and standalone Focused item sprites; later eras retain their existing Tier02..Tier09 art. Primitive companions use three pet and three mount whole PNGs; the old rig pipeline is archive-only.
- The pet summon currency icon is a standalone egg; the mount summon currency icon is a standalone hoof. The `PetTicketEgg-v2` and `MountTicketHoof-v2` resource filenames do not imply a ticket-shaped illustration. Summon buttons show only the currency icons and amounts actually spent.
- Dungeon entry reserves a challenge without consuming its key. A win creates a saved pending reward. Only the reward popup's claim button consumes one key, updates the highest cleared difficulty and pays the reward once; defeat/cancellation spends no key. Sweeps still spend one key and grant the previous-difficulty reward immediately.
- Forging uses three Animator-driven hammer strikes with synchronized sparks, then a square rarity frame and equipment icon for 0.5 seconds. Multiple results overlap as a hand. Automatic forging uses a persistent 1–99 batch-size dropdown.
- Waves retain the current player and combat state within a stage; only the next enemy enters. Result-screen taps finish a running summon reveal, then close on the next tap; scrolling does not count as a tap. “모두 업그레이드” consumes each owned entry's available fragments repeatedly up to its affordable level or level100.
- The shop has one six-offer diamond grid: first row free100 / 600 / 2200; second row8000 / 15000 / 33000. The free offer is claimable once per Korean day. All purchases remain local demonstrations.
- The pass opens at the first claimable milestone (otherwise the next progress milestone). Its central cyan rail fills to the highest cleared stage; intermediate diamond ornaments are removed. Reward/pass icons and equipped-skill HUD slots are enlarged1.5×, with matching enlarged reward/pass labels.
- Actual power changes show the new total in an independent DOTween popup, with a green UP or red DOWN arrow above it. Initial loading and unchanged refreshes do not create power popups.
- Current revision validation is recorded separately in cloud reports; earlier passing results do not certify later source or artwork changes.

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

Runtime typography enlarges original design sizes by 25%, capped at eight additional logical units per label (25 → 31, 32 → 40, 44 → 52). This applies to the main HUD, feature pages and dialogs, including independently instantiated equipment labels. Symbol-only close/back/star controls keep their designed size. Long chat messages wrap into taller bubbles instead of shrinking the type. All sizes remain in the shared 1080-unit layout.

Shop product art uses a centered RectTransform pivot with unchanged outer bounds. Unity uGUI uses this pivot for preserve-aspect placement, so narrow gem bags no longer stick to the left edge of their card.

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

## Original main-screen demo interactions (historical)

These retained notes describe the initial UI prototype. Gameplay requests and the latest corrections above supersede their costs, progression, battle and persistence behavior; use the current quickstart for testing.

- Select a slot: inspect, lock/unlock, upgrade.
- Anvil button: standalone transparent foreground artwork; spend 100 stones and upgrade one unlocked equipment item.
- Forge level button: separate management dialog, with no resource spending.
- Auto: repeat forging every 1.4 seconds; pause while a dialog is open; stop if resources are insufficient.
- Stage: explicitly labeled local completion simulation.
- Event / fairy: one claim each per session.
- Navigation: equipment, dungeon, companion, quest reward, gold-to-stone exchange.
- Wallet/profile/chat: local information dialogs. No server, live chat, combat simulation, payments or persistent account state are connected.

## Artwork

Main combat scenery uses the dedicated `Resources/Moonlit/Main/EmptyCryptBattle-v1.png`: an empty stone crypt with ribbed arches, wall statues, warm torches and a cracked flagstone floor. No character, pet, monster or health bar is painted into this background; the previous cyan particle overlay is also removed. Other pages/cards retain `MainScreenAssets.worldBackground`. The battle image follows the same elastic crop and does not change HUD/forge/navigation layout. Source reference and exact built-in imagegen prompt: `Documentation/UI/EmptyCryptBattle-v1-prompt.md`. The older forest asset remains available for rollback.

Production images were generated using the built-in image generation tool. Original prompts are in `Art/GENERATION.md`; reference-fidelity revision prompts are in `Art/GENERATION-v2.md` and `Art/CompanionFrame-v2-prompt.md`. Versioned files preserve earlier artwork. All project references use local assets. No reference screenshot is baked into the UI. The environment asset itself contains no actors; current gameplay places runtime player/enemy objects and effects over it.

Noto Sans KR and Noto Sans CJK KR Bold are bundled under the SIL Open Font License, included in `Fonts/OFL.txt`.

## Primitive companion art and rig pipeline

PR88 introduces six primitive samples: 원시 꼬마, 검치호 새끼, 새끼 익룡, 원시 랩터, 야생 멧돼지 and 돌바퀴 수레. Collection illustrations live under `Resources/Moonlit/Companions`; their separate transparent part sheets live under `Resources/Moonlit/CompanionParts`. Each sample has eight parts (48 parts across six samples).

`CompanionRigTemplates` defines ten anatomical templates: humanoid, quadruped, bird, serpentine, insect, aquatic, floating, biped mount, quadruped mount and vehicle. Six templates have primitive samples; ten templates do not mean ten finished companions. The cloud editor builder creates a bone hierarchy, per-part SpriteRenderer/SpriteSkin meshes and native Animator clips (Idle/Walk/Attack/Hit/Death), rather than moving one whole illustration as a fake rig. This archived integration is not used by live companions; current whole-PNG companions use a simple back anchor. Higher-era companion art remains deferred.

PR94 commits hosted run109's six prefabs,48native sprite assets,30animation clips,six controllers and catalog. Run109 passed210PlayMode tests including221-vertex/UV persistence, real deformation and saddle attachment. Actual assembled review found visible seams; the user stopped further rigging. These archived assets are retained without a claim of visual approval. Live whole-PNG companions and their placement are covered by the current validation report.

## Verification

Acceptance runs in cloud Unity6000.3.8f1, with compile/tests and captures at both portrait ratios and simulated safe insets. Agents must not run or control local Unity or use `Library/Moonlit.command`. The saved scene remains bootstrap-only. Current workflow results, artifact paths and pending/failed checks are recorded in the coordinator's verification report.

The original main-screen capture cases cover 9:16 and 9:19 without cutouts, both sizes with simulated top/bottom insets, additional side insets, and returning to 9:16. Each case checks all 25 button hit targets, button/text bounds, safe-centered dialogs, fixed control proportions and idempotent initialization. Runtime interactions also check slot clearing/rebinding, forge resource costs, locks, automatic forging and navigation. These are Editor simulations, not physical-device tests.

## Reference fidelity revision

The main reward entries use dedicated transparent sprites: ProgressPassIcon-v1 (blue sword/pass pennant) and OfflineRewardIcon-v1 (clock/reward chest), following the user's explicit subject change. Their timer text and invisible hit surfaces remain separate. Generation prompts: Documentation/UI/MainRewardIcons-v1-prompts.md.

Event buttons are frameless. Navigation uses four evenly spaced large icons (arena, dungeons, collections, shop) on one shared stone panel without individual boxes or labels. Currency icons use a round crown coin and tall diamond ruby; the shop uses a striped awning. Chat includes a speech balloon and live text with a 99 badge. Forge management and auto use silver-beveled cobalt blue backplates; auto shows a circular-arrow icon that rotates while active. The anvil has its own clickable silhouette and press feedback. All decorative layers ignore raycasts. Verify also checks actual pointer hit targets for the anvil, forge management, auto, events, chat and navigation.

Latest user direction: stop companion rigging and leave the current committed rigs as-is. Known visual attachment gaps remain deferred; no rig/pivot/bone/part edits or regeneration are authorized. This is not a visual acceptance of the rigs. Skill choreography and general UI work continue.
