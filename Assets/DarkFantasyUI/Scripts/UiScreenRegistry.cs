using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Moonlit.UI
{
    public enum ScreenPresentation { Page, Modal, Fullscreen }

    public sealed class UiScreenRegistry
    {
        internal sealed class Route
        {
            public ScreenPresentation presentation;
            public Action<ScreenContext> build;
            public bool dismissOnBackdrop;
        }

        readonly Dictionary<string, Route> routes = new Dictionary<string, Route>(StringComparer.Ordinal);
        readonly UiScreenHost host;

        internal UiScreenRegistry(UiScreenHost host) { this.host = host; }

        public void Register(string key, ScreenPresentation presentation,
            Action<ScreenContext> build, bool dismissOnBackdrop = true)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A route key is required.", nameof(key));
            if (build == null) throw new ArgumentNullException(nameof(build));
            routes[key] = new Route { presentation = presentation, build = build, dismissOnBackdrop = dismissOnBackdrop };
        }

        public bool IsRegistered(string key) { return key != null && routes.ContainsKey(key); }
        public void Open(string key, object payload = null) { host.Open(key, payload); }
        public void CloseTop() { host.CloseTop(); }
        public void ShowMainPage() { host.ClosePage(); }
        public int ModalDepth => host.ModalDepth;
        public string ActivePageKey => host.ActivePageKey;
        internal bool TryGet(string key, out Route route) { return routes.TryGetValue(key, out route); }
    }

    public sealed class ScreenContext
    {
        readonly UiScreenHost host;
        readonly int instanceId;
        public RectTransform Root { get; }
        public float Width => 1080f;
        public float Height => Root.rect.height;
        public MainScreenAssets Assets { get; }
        public MainScreen Main { get; }
        public object Payload { get; }

        internal ScreenContext(UiScreenHost host, int instanceId, RectTransform root,
            MainScreenAssets assets, MainScreen main, object payload)
        {
            this.host = host; this.instanceId = instanceId; Root = root;
            Assets = assets; Main = main; Payload = payload;
        }

        public void Open(string key, object payload = null) { host.Registry.Open(key, payload); }
        public void Close() { host.Close(instanceId); }
        public void Toast(string message) { Main.Toast(message); }
    }

    /// <summary>Owns base-page replacement and an unbounded sibling modal stack.</summary>
    public sealed class UiScreenHost : MonoBehaviour
    {
        sealed class Entry
        {
            public int id;
            public string key;
            public GameObject layer;
            public RectTransform safeRoot;
            public CanvasGroup group;
            public GameObject opener;
        }

        readonly List<Entry> stack = new List<Entry>();
        MainScreenAssets assets;
        MainScreen main;
        RectTransform popupRoot, pageHost;
        CanvasGroup mainInput, navigationInput;
        Entry page;
        int nextId;
        Rect lastSafe;
        Vector2Int lastScreen;
#if UNITY_EDITOR
        Vector2Int? previewScreen;
        Rect previewSafe;
        public void SetPreviewMetrics(Vector2Int size, Rect safe) { previewScreen = size; previewSafe = safe; ApplySafeArea(true); }
        public void ClearPreviewMetrics() { previewScreen = null; ApplySafeArea(true); }
#endif

        public UiScreenRegistry Registry { get; private set; }
        public int ModalDepth => stack.Count;
        public string ActivePageKey => page == null ? null : page.key;

        public void Initialize(MainScreenAssets screenAssets, MainScreen screen, RectTransform popup,
            RectTransform pages, CanvasGroup mainGroup, CanvasGroup navigationGroup)
        {
            assets = screenAssets; main = screen; popupRoot = popup; pageHost = pages;
            mainInput = mainGroup; navigationInput = navigationGroup;
            Registry = new UiScreenRegistry(this);
        }

        void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) CloseTop();
            ApplySafeArea();
        }

        internal void Open(string key, object payload)
        {
            if (!Registry.TryGet(key, out var route)) { Debug.LogWarning("Unknown Moonlit UI route: " + key, this); return; }
            if (route.presentation == ScreenPresentation.Page) { OpenPage(key, route, payload); return; }
            if (stack.Count > 0 && stack[stack.Count - 1].key == key) return;

            var entry = CreateEntry(key, 200 + stack.Count * 10, popupRoot, true);
            entry.opener = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (route.dismissOnBackdrop)
            {
                var backdrop = entry.layer.GetComponentInChildren<Button>();
                backdrop.onClick.AddListener(() => Close(entry.id));
            }
            stack.Add(entry);
            SetInputState();
            ApplySafeArea(true);
            route.build(new ScreenContext(this, entry.id, entry.safeRoot, assets, main, payload));
            if(entry.safeRoot)UiScreenMotion.Play(entry.safeRoot,key=="chat");
        }

        void OpenPage(string key, UiScreenRegistry.Route route, object payload)
        {
            CloseAllModals();
            if (page != null && page.key == key) return;
            if (page != null) { SetGroup(page.group, false); Destroy(page.layer); }
            page = CreateEntry(key, 10, pageHost, false);
            SetInputState();
            ApplySafeArea(true);
            route.build(new ScreenContext(this, page.id, page.safeRoot, assets, main, payload));
            if(page!=null && page.safeRoot)UiScreenMotion.Play(page.safeRoot,true);
        }

        Entry CreateEntry(string key, int order, RectTransform parent, bool modal)
        {
            var layer = new GameObject((modal ? "Popup Layer " : "Page — ") + key,
                typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup));
            var rect = layer.GetComponent<RectTransform>(); rect.SetParent(parent, false); Ui.Stretch(rect);
            var canvas = layer.GetComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = order;
            var group = layer.GetComponent<CanvasGroup>();
            if (modal)
            {
                var dim = Ui.Image("Dim", rect, 0, 0, 1, 1, null, new Color(0, .015f, .025f, .85f));
                Ui.Stretch(dim.rectTransform); dim.raycastTarget = true;
                dim.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            }
            var safe = Ui.Rect("SafeArea", rect, 0, 0, 1080, 1920);
            // Page controls must not paint over the persistent bottom navigation rail.
            // Full-viewport scenery is a separate non-interactive sibling of SafeArea.
            // Preserve the full logical layout height used by feature modules.
            if (!modal) safe.gameObject.AddComponent<RectMask2D>().padding = new Vector4(0, PortraitSafeArea.NavigationTopFromBottom, 0, 0);
            return new Entry { id = ++nextId, key = key, layer = layer, safeRoot = safe, group = group };
        }

        internal void Close(int id)
        {
            if (stack.Count == 0 && page != null && page.id == id) { ClosePage(); return; }
            if (stack.Count == 0 || stack[stack.Count - 1].id != id) return;
            CloseTop();
        }

        public void CloseTop()
        {
            if (stack.Count == 0) { if (page != null) ClosePage(); return; }
            var entry = stack[stack.Count - 1]; stack.RemoveAt(stack.Count - 1);
            SetGroup(entry.group, false); entry.layer.SetActive(false); Destroy(entry.layer); SetInputState();
            if (entry.opener && entry.opener.activeInHierarchy && EventSystem.current)
                EventSystem.current.SetSelectedGameObject(entry.opener);
        }

        public void CloseAllModals() { while (stack.Count > 0) CloseTop(); }

        public void ClosePage()
        {
            CloseAllModals();
            if (page != null) { SetGroup(page.group, false); page.layer.SetActive(false); Destroy(page.layer); page = null; }
            SetInputState();
        }

        void SetInputState()
        {
            bool uncovered = stack.Count == 0;
            SetGroup(mainInput, uncovered && page == null); SetGroup(navigationInput, uncovered);
            if (page != null) SetGroup(page.group, uncovered);
            for (int i = 0; i < stack.Count; i++) SetGroup(stack[i].group, i == stack.Count - 1);
            if (main) main.RefreshNavigation(ActivePageKey);
        }

        static void SetGroup(CanvasGroup group, bool enabled)
        {
            if (!group) return;
            group.interactable = enabled; group.blocksRaycasts = enabled;
        }

        public void ApplySafeArea(bool force = false)
        {
            var size = new Vector2Int(Screen.width, Screen.height); var safe = Screen.safeArea;
#if UNITY_EDITOR
            if (previewScreen.HasValue) { size = previewScreen.Value; safe = previewSafe; }
#endif
            if (!force && size == lastScreen && safe == lastSafe) return;
            if (size.x <= 0 || size.y <= 0 || safe.width <= 0 || safe.height <= 0) return;
            lastScreen = size; lastSafe = safe;
            if (page != null) Fit(page.safeRoot, size, safe);
            foreach (var entry in stack) Fit(entry.safeRoot, size, safe);
        }

        static void Fit(RectTransform root, Vector2Int size, Rect safe)
        {
            var motion=root.GetComponent<UiScreenMotion>();
            if(motion)motion.Complete();
            root.anchorMin = new Vector2(safe.xMin / size.x, safe.yMin / size.y);
            root.anchorMax = new Vector2(safe.xMax / size.x, safe.yMax / size.y);
            root.pivot = new Vector2(.5f, .5f); root.offsetMin = root.offsetMax = Vector2.zero;
            float logicalHeight = 1080f * safe.height / safe.width;
            root.anchorMin = root.anchorMax = new Vector2((safe.xMin + safe.xMax) * .5f / size.x, (safe.yMin + safe.yMax) * .5f / size.y);
            root.sizeDelta = new Vector2(1080, logicalHeight);
            root.localScale = Vector3.one * (safe.width / size.x);
            PopupSkin.UpdateBackdropClip(root);
        }

        void OnDestroy()
        {
            stack.Clear(); page = null;
        }
    }
}
