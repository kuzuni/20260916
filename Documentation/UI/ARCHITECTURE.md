# Canvas, routing and modal stack design

## Runtime hierarchy

```text
Runtime UI root (one scaler and shared camera)
  MainCanvas                   sortingOrder 0
    Full-bleed scenery
    SafeArea / main HUD and content page host
  NavigationCanvas             sortingOrder 100
    SafeArea / bottom navigation
  PopupRoot
    PopupLayer 0 Canvas        sortingOrder 200
      Full-viewport Dim
      SafeArea / dialog
    PopupLayer 1 Canvas        sortingOrder 210
      Full-viewport Dim
      SafeArea / detail dialog
    PopupLayer 2 Canvas        sortingOrder 220
      Full-viewport Dim
      SafeArea / deeper dialog
    ... allocate/reuse on demand
  ToastCanvas                  sortingOrder 1000, no raycasts
```

This supports the user's main/nav/popup/detail/deeper arrangement without hard-coding a maximum of three nested details. Layers should be siblings under PopupRoot, managed as a stack, not a hierarchy where closing a child destroys its parent. Nested canvases can use overrideSorting; share render camera/scaling. Do not place CanvasScaler on every nested panel.

## Routing and input invariants

- Registry Open of an already active base page is idempotent. Bottom navigation is a separate user toggle: selecting its active page closes it and restores the original icon; inactive entries open/replace their page. The selected entry displays reusable crimson close artwork and a live × label. Page/back/context close all restore navigation from host state.
- The unsupplied quest demo uses a base page so its navigation entry follows the same close toggle. A nested modal still blocks all navigation until only that modal is closed.
- Modal Open pushes a new layer; CloseTop/Escape/Android back pops exactly one layer.
- Each modal owns its dim behind its panel. Default alpha is tunable (roughly .4); avoid .85 at each level. Verify multiple layers visually, cap effective background darkness if necessary.
- Disable input on every covered panel/page/nav via CanvasGroup and raycaster control. Only the top modal is interactive. Ensure independently sorted canvases cannot leak clicks through a disabled ancestor.
- Full-screen dim intercepts pointer/touch even outside SafeArea. Clicking inside a modal must not trigger backdrop close.
- Backdrop dismissal is an explicit per-route option; item comparison and destructive decisions must not silently sell/equip/claim on dismissal.
- Parent view instance survives child details; tab/scroll/selection state survives pop. Restore selection/focus to opener.
- Close during transition, double open, rapid back, and repeated tap must be safe. Disable hidden raycasters and remove listeners on destruction.
- Tooltip/notification art never captures raycasts.
- Screen.safeArea changes apply to every active layer; no fixed assumption of zero insets. Existing scene must remain bootstrap-only. No editor-generated UI persisted in scenes.

## Common module API (frozen for parallel implementation)

Foundation task creates these types in namespace Moonlit.UI. Feature tasks MUST NOT redefine them. This is a design contract; these types are not yet in baseline code.

```csharp
public enum ScreenPresentation { Page, Modal, Fullscreen }
public sealed class UiScreenRegistry {
    public void Register(string key, ScreenPresentation presentation,
        System.Action<ScreenContext> build, bool dismissOnBackdrop = true);
}
public sealed class ScreenContext {
    public UnityEngine.RectTransform Root { get; } // empty safe content root, logical width 1080
    public float Width { get; }                    // 1080
    public float Height { get; }                   // actual safe logical height
    public MainScreenAssets Assets { get; }
    public MainScreen Main { get; }
    public object Payload { get; }
    public void Open(string key, object payload = null);
    public void Close();
    public void Toast(string message);
}
```

Root is top-left anchored logical space compatible with existing Ui.Rect/Image/Text/ArtButton. Registry/host owns dim, top-level sorting, close/back lifecycle and safe region. Each module owns the actual artwork/panel and close button, created under Root. Use proportional sizing or a centered bounded panel and real ScrollRect for overflowing contents. Preserve original Korean labels from references.

Modules register exactly:
- ForgeScreenModule.Register(UiScreenRegistry registry)
- ProgressionScreenModule.Register(UiScreenRegistry registry)
- SocialScreenModule.Register(UiScreenRegistry registry)

All three classes public static, namespace Moonlit.UI. Keep local helper names inside their module namespace or prefix to prevent collisions. Existing public Ui and MainScreenAssets are available. Modules may use feature-specific MonoBehaviours for lifecycle/state. No dependency on another feature module class: navigation uses registered string keys.

Cross-module player-details payload: Dictionary<string, object> with keys name (string), power (string), rank (int); optional avatarIndex (int). Equipment payload: EquipmentSlot when actual equipment; forge-item-details may receive ItemDefinition. Omitted payload renders a documented deterministic demo.

Foundation must not directly reference missing feature module classes before merge. Integrator wires registrations after each module is present, or foundation provides explicit optional registration discovery with a documented bootstrap hook (avoid fragile type-name magic). Keep feature modules self-contained until integrated.

## Ownership / integration

Foundation owns existing Scripts files, shared contracts, routing, canvas split, bootstrap integration, CI and tests.
Feature tasks only add their owned folders + corresponding .meta, feature-specific generated art and a task report.
Do not change existing scene, asset catalog, shared helpers, dependencies, .github workflows, project version, or other module files in feature tasks.
Foundation is reviewed/integrated first, then modules, then a final coordinator-only entry-point hookup and visual validation.
