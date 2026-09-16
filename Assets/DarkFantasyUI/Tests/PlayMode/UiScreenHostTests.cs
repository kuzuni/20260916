using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Moonlit.UI.Tests
{
    public sealed class UiScreenHostTests
    {
        GameObject root;
        UiScreenHost host;
        CanvasGroup main, navigation;
        MainScreenAssets assets;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("test root", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var popup = Child("popups", root.transform); var pages = Child("pages", root.transform);
            main = Child("main", root.transform).gameObject.AddComponent<CanvasGroup>();
            navigation = Child("navigation", root.transform).gameObject.AddComponent<CanvasGroup>();
            var screen = root.AddComponent<MainScreen>();
            screen.enabled = false;
            assets = ScriptableObject.CreateInstance<MainScreenAssets>();
            assets.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            screen.font = assets.font;
            host = root.AddComponent<UiScreenHost>(); host.Initialize(assets, screen, popup, pages, main, navigation);
            host.SetPreviewMetrics(new Vector2Int(1080,1920), new Rect(0,0,1080,1920));
            new GameObject("events", typeof(EventSystem));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(assets);
            foreach (var events in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                Object.DestroyImmediate(events.gameObject);
        }

        [UnityTest]
        public IEnumerator ModalStack_PopsOnlyTop_RestoresParentAndFocus()
        {
            int parentValue = 0;
            host.Registry.Register("parent", ScreenPresentation.Modal, context => {
                var state = new GameObject("preserved state"); state.transform.SetParent(context.Root, false); parentValue = 17;
                var opener = new GameObject("child opener", typeof(RectTransform), typeof(Image), typeof(Button)); opener.transform.SetParent(context.Root, false);
                EventSystem.current.SetSelectedGameObject(opener);
                opener.GetComponent<Button>().onClick.AddListener(() => context.Open("child"));
            });
            host.Registry.Register("child", ScreenPresentation.Modal, context => new GameObject("child content").transform.SetParent(context.Root, false));
            host.Registry.Open("parent"); yield return null;
            var opener = GameObject.Find("child opener"); opener.GetComponent<Button>().onClick.Invoke(); yield return null;
            Assert.AreEqual(2, host.ModalDepth); Assert.IsFalse(main.blocksRaycasts); Assert.IsFalse(navigation.blocksRaycasts);
            host.CloseTop(); yield return null;
            Assert.AreEqual(1, host.ModalDepth); Assert.AreEqual(17, parentValue); Assert.IsNotNull(GameObject.Find("preserved state"));
            Assert.AreSame(opener, EventSystem.current.currentSelectedGameObject);
        }

        [UnityTest]
        public IEnumerator DuplicateTap_IsIdempotent_AndDestructionIsClean()
        {
            int builds = 0;
            host.Registry.Register("one", ScreenPresentation.Modal, context => { builds++; new GameObject("content").transform.SetParent(context.Root, false); });
            host.Registry.Open("one"); host.Registry.Open("one");
            Assert.AreEqual(1, host.ModalDepth); Assert.AreEqual(1, builds);
            host.CloseTop(); host.CloseTop(); yield return null;
            Assert.AreEqual(0, host.ModalDepth); Assert.IsTrue(main.blocksRaycasts); Assert.IsTrue(navigation.blocksRaycasts);
            Assert.IsNull(GameObject.Find("content"));
        }

        [UnityTest]
        public IEnumerator PagesReplace_CloseModals_AndBackdropBlocksClicks()
        {
            int clicks = 0;
            var navButton = new GameObject("nav button", typeof(RectTransform), typeof(Image), typeof(Button)); navButton.transform.SetParent(navigation.transform, false);
            navButton.GetComponent<Button>().onClick.AddListener(() => clicks++);
            host.Registry.Register("page-a", ScreenPresentation.Page, context => new GameObject("page state").transform.SetParent(context.Root, false));
            host.Registry.Register("page-b", ScreenPresentation.Page, context => { });
            host.Registry.Register("modal", ScreenPresentation.Modal, context => { });
            host.Registry.Open("page-a"); host.Registry.Open("modal"); yield return null;
            Assert.IsFalse(navButton.GetComponent<Button>().IsInteractable());
            host.Registry.Open("page-b"); yield return null;
            Assert.AreEqual(0, host.ModalDepth); Assert.AreEqual("page-b", host.ActivePageKey); Assert.IsNull(GameObject.Find("page state")); Assert.AreEqual(0, clicks);
        }

        [UnityTest]
        public IEnumerator PageCanReceiveInputAndCloseBackToMain()
        {
            ScreenContext pageContext = null;
            Button button = null;
            host.Registry.Register("page", ScreenPresentation.Page, context => {
                pageContext = context;
                var rect = Child("page action", context.Root);
                rect.gameObject.AddComponent<Image>().raycastTarget = true;
                button = rect.gameObject.AddComponent<Button>();
            });
            host.Registry.Open("page"); yield return null;
            Assert.IsTrue(button.IsInteractable());
            Assert.IsTrue(navigation.interactable);
            Assert.IsFalse(main.interactable);
            pageContext.Close(); yield return null;
            Assert.IsNull(host.ActivePageKey);
            Assert.IsTrue(main.interactable);
            host.Registry.Open("page"); host.CloseTop(); yield return null;
            Assert.IsNull(host.ActivePageKey);
        }

        [UnityTest]
        public IEnumerator ModalDimInterceptsPointerOutsideSafeArea()
        {
            navigation.gameObject.AddComponent<Canvas>().overrideSorting = true;
            navigation.GetComponent<Canvas>().sortingOrder = 100;
            navigation.gameObject.AddComponent<GraphicRaycaster>();
            var navRect = Child("full nav button", navigation.transform);
            navRect.gameObject.AddComponent<Image>().raycastTarget = true;
            var navButton = navRect.gameObject.AddComponent<Button>();
            int clicks = 0;
            navButton.onClick.AddListener(() => clicks++);
            host.Registry.Register("modal", ScreenPresentation.Modal, context => { });
            host.SetPreviewMetrics(new Vector2Int(1080,1920), new Rect(0,120,1080,1700));
            host.Registry.Open("modal"); yield return null;
            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(EventSystem.current) { position = new Vector2(2,2) };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer,hits);
            Assert.IsNotEmpty(hits);
            Assert.AreEqual("Dim",hits[0].gameObject.name);
            ExecuteEvents.Execute(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
            Assert.AreEqual(0,clicks);
            Assert.AreEqual(0,host.ModalDepth);
        }

        [UnityTest]
        public IEnumerator SafeAreaChange_RefitsEveryOpenLayerWithoutRebuilding()
        {
            RectTransform parent = null, child = null;
            host.Registry.Register("parent", ScreenPresentation.Modal, context => parent = context.Root);
            host.Registry.Register("child", ScreenPresentation.Modal, context => child = context.Root);
            host.Registry.Open("parent"); host.Registry.Open("child");
            host.SetPreviewMetrics(new Vector2Int(1080, 2280), new Rect(36, 84, 1008, 2076));
            yield return null;
            Assert.AreEqual(2, host.ModalDepth);
            Assert.That(parent.rect.height, Is.EqualTo(1080f * 2076f / 1008f).Within(.1f));
            Assert.That(child.rect.height, Is.EqualTo(parent.rect.height).Within(.1f));
            Assert.That(child.localScale.x, Is.EqualTo(1008f / 1080f).Within(.001f));
            host.ClearPreviewMetrics();
        }

        [Test]
        public void LocalRewardAndForgeDecisions_MutateExactlyOnce()
        {
            PrepareMainLabels();
            var startGold = host.GetComponent<MainScreen>().gold;
            var startOre = host.GetComponent<MainScreen>().ore;
            Assert.IsTrue(host.GetComponent<MainScreen>().ClaimOfflineRewards(174, 2));
            Assert.IsFalse(host.GetComponent<MainScreen>().ClaimOfflineRewards(174, 2));
            Assert.AreEqual(startGold + 174, host.GetComponent<MainScreen>().gold);
            Assert.AreEqual(startOre + 2, host.GetComponent<MainScreen>().ore);
            var screen = host.GetComponent<MainScreen>();
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            try
            {
                Assert.IsFalse(screen.BeginCraft(item, -100));
                Assert.IsTrue(screen.BeginCraft(item, 100));
                int firstId = screen.PendingCraftId;
                Assert.IsTrue(screen.BeginCraft(item, 100));
                Assert.AreEqual(startOre - 98, screen.ore, "Reopening must not charge twice");
                Assert.IsTrue(screen.ResolveCraftedEquipment(firstId, false, 120));
                Assert.IsFalse(screen.ResolveCraftedEquipment(firstId, false, 120));
                Assert.AreEqual(startOre + 22, screen.ore);
                Assert.IsTrue(screen.BeginCraft(item, 100));
                Assert.IsFalse(screen.ResolveCraftedEquipment(firstId, false, 120), "A stale callback must not sell a new item");
                Assert.AreEqual(startOre - 78, screen.ore);
            }
            finally { Object.DestroyImmediate(item); }
        }

        [UnityTest]
        public IEnumerator Shop_HasExactlyTheFiveRequiredOffers_AndCanReturnToMain()
        {
            PrepareMainLabels();
            SocialScreenModule.Register(host.Registry);
            host.Registry.Open("shop"); yield return null;
            var expected = new[] { 60, 220, 800, 1500, 3300 };
            foreach (var amount in expected) Assert.IsNotNull(GameObject.Find("Gem offer " + amount));
            var scroll = Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Single();
            Assert.AreEqual(expected.Length, scroll.content.Cast<Transform>().Count(t => t.name.StartsWith("Gem offer ")));
            Assert.IsTrue(scroll.vertical);
            GameObject.Find("‹ 메인").GetComponent<Button>().onClick.Invoke(); yield return null;
            Assert.IsNull(host.ActivePageKey);
            Assert.IsTrue(main.interactable);
        }

        [UnityTest]
        public IEnumerator CollectionQuickEquip_DoesNotAccumulateSlots_AndChildKeepsScroll()
        {
            PrepareMainLabels();
            ProgressionScreenModule.Register(host.Registry);
            host.Registry.Open("skills-pets-heroes"); yield return null;
            GameObject.Find("Tab 스킬").GetComponent<Button>().onClick.Invoke(); yield return null;
            var equipped = GameObject.Find("Equipped skills").transform;
            var quick = GameObject.Find("Quick equip").GetComponent<Button>();
            quick.onClick.Invoke(); quick.onClick.Invoke(); yield return null;
            Assert.AreEqual(3, equipped.childCount);
            var scroll = Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Single();
            scroll.content.sizeDelta = new Vector2(0, 3000);
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = .37f;
            host.Registry.Open("skill-details"); yield return null;
            host.CloseTop(); yield return null;
            Assert.AreSame(scroll, Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Single());
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(.37f).Within(.01f));
            Assert.AreEqual(3, equipped.childCount);
        }

        [Test]
        public void AutoForgeConfiguration_CanBeStoppedAndPreservesChoices()
        {
            PrepareMainLabels();
            var screen = host.GetComponent<MainScreen>();
            screen.ConfigureAutoForge(7, 5, false, new[] { true, false, true, false });
            Assert.IsTrue(screen.autoForge);
            Assert.AreEqual(7, screen.autoForgeBatchSize);
            Assert.AreEqual(5, screen.autoForgeFilterMask);
            Assert.IsFalse(screen.autoForgeContinue);
            CollectionAssert.AreEqual(new[] { true, false, true, false }, screen.autoForgeKeep);
            screen.StopAutoForge();
            Assert.IsFalse(screen.autoForge);
        }

        void PrepareMainLabels()
        {
            var screen = host.GetComponent<MainScreen>();
            screen.oreText = Label("ore"); screen.goldText = Label("gold"); screen.gemText = Label("gems");
            screen.powerText = Label("power"); screen.stageText = Label("stage"); screen.autoText = Label("auto");
            screen.toastRoot = root.transform; screen.equipment = new EquipmentSlot[0];
        }

        Text Label(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(root.transform, false); return go.GetComponent<Text>();
        }

        static RectTransform Child(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); Ui.Stretch(rect); return rect;
        }
    }
}
