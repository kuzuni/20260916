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
            MoonlitRuntimeSettings.ResetSession();
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
        public IEnumerator ForgeCatalog_SixSlotSelectionPreservesIdentityAndParentScroll()
        {
            ForgeState.Current=new ForgeState();
            ForgeScreenModule.Register(host.Registry);
            var model=new EquipmentRoll{tier=0,variant=0,part=EquipmentPart.Armor};
            var thumbnail=EquipmentArt.Icon(model);
            Assert.IsNotNull(thumbnail,"Use the supplied primitive thief armor thumbnail");
            foreach(int height in new[]{1920,2280}) {
                host.SetPreviewMetrics(new Vector2Int(1080,height),new Rect(36,84,1008,height-168));
                host.Registry.Open("forge-probability-details"); yield return null;
                var parent=GameObject.Find("Popup Layer forge-probability-details");
                var scroll=parent.GetComponentInChildren<ScrollRect>();
                var cells=scroll.content.GetComponentsInChildren<Button>();
                Assert.AreEqual(180,cells.Length,"10 tiers × 3 art variants × 6 equipment parts");
                foreach(var cell in cells) Assert.IsNotNull(cell.transform.Find("Thumbnail").GetComponent<Image>().sprite, cell.name+" must have its own equipment artwork");
                var selected=cells.Single(b=>b.name=="Equipment 0 0 Armor");
                Assert.AreSame(thumbnail,selected.transform.Find("Thumbnail").GetComponent<Image>().sprite);
                scroll.verticalNormalizedPosition=.65f; Canvas.ForceUpdateCanvases();
                selected.onClick.Invoke(); yield return null;
                var child=GameObject.Find("Popup Layer forge-item-details");
                Assert.AreSame(thumbnail,child.GetComponentsInChildren<Image>().Single(i=>i.name=="Equipment icon").sprite);
                Assert.AreEqual(model.Name,child.GetComponentsInChildren<Text>().Single(t=>t.name=="Name").text);
                StringAssert.Contains("체력 80",child.GetComponentsInChildren<Text>().Single(t=>t.name=="Stats").text);
                StringAssert.Contains("스피드 1",child.GetComponentsInChildren<Text>().Single(t=>t.name=="Stats").text);
                Assert.AreEqual(2,host.ModalDepth);
                host.CloseTop(); yield return null;
                Assert.AreSame(parent,GameObject.Find("Popup Layer forge-probability-details"));
                Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(.65f).Within(.02f));
                host.CloseTop(); yield return null;
                host.Registry.Open("forge-item-details"); yield return null;
                child=GameObject.Find("Popup Layer forge-item-details");
                Assert.AreSame(thumbnail,child.GetComponentsInChildren<Image>().Single(i=>i.name=="Equipment icon").sprite);
                host.CloseTop(); yield return null;
            }
        }

        [Test]
        public void Navigation_FourEqualCellsKeepOriginalIconsAndDecoration()
        {
            assets.panels=new Sprite[5];
            assets.interfaceIcons=new Sprite[14];
            var screen=root.GetComponent<MainScreen>();
            var factory=new RuntimeMainScreenFactory(assets);
            typeof(RuntimeMainScreenFactory).GetMethod("BuildNavigation",
                System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .Invoke(factory,new object[]{screen,navigation.transform});
            Assert.AreEqual(4,screen.navigation.Length);
            CollectionAssert.AreEqual(new[]{"Arena","Dungeon","Companions","Shop"},
                screen.navigation.Select(button=>button.name.Split(' ')[0]).ToArray());
            var panel=screen.navigation[0].transform.parent;
            Assert.IsNotNull(panel.Find("Navigation stone texture"));
            Assert.IsNotNull(panel.Find("Upper bronze ornament"));
            Assert.AreEqual(3,panel.Cast<Transform>().Count(child=>child.name=="Bronze divider"));
            for(int i=0;i<screen.navigation.Length;i++) {
                var rect=(RectTransform)screen.navigation[i].transform;
                Assert.That(rect.rect.width,Is.EqualTo(250).Within(.01f));
                Assert.That(rect.anchoredPosition.x,Is.EqualTo(10+i*270).Within(.01f));
                var icon=(RectTransform)rect.Find("Menu icon");
                Assert.That(icon.anchoredPosition.x+icon.rect.width*.5f,
                    Is.EqualTo(rect.rect.width*.5f).Within(.01f),"Icon must stay centered in its enlarged cell");
                Assert.IsNotNull(rect.Find("Close icon"));
                Assert.IsFalse(rect.Find("Close icon").gameObject.activeSelf);
            }
        }

        [UnityTest]
        public IEnumerator Navigation_TogglesMatchingPage_AndRestoresIconOnEveryClosePath()
        {
            var screen=root.GetComponent<MainScreen>();
            screen.screens=host.Registry;
            screen.navigation=new Button[4];
            string[] routes={"pvp","dungeons","skills-pets-heroes","shop"};
            ScreenContext lastContext=null;
            foreach (string route in routes)
                host.Registry.Register(route,ScreenPresentation.Page,context=>lastContext=context);
            host.Registry.Register("child",ScreenPresentation.Modal,context=>{});
            for (int i=0;i<4;i++)
            {
                int index=i;
                var button=Ui.ArtButton("Navigation "+i,navigation.transform,i*200,0,190,146);
                Ui.Image("Menu icon",button.transform,30,0,117,118,null);
                var close=Ui.Image("Close icon",button.transform,30,0,117,118,PopupSkin.CloseArt);
                close.gameObject.SetActive(false);
                Ui.Image("Notification",button.transform,140,0,20,20,null);
                button.onClick.AddListener(()=>screen.Navigate(index));
                screen.navigation[i]=button;
            }
            for(int i=0;i<4;i++)
            {
                var button=screen.navigation[i];
                button.onClick.Invoke(); yield return null;
                Assert.AreEqual(routes[i],host.ActivePageKey);
                Assert.IsTrue(button.transform.Find("Close icon").gameObject.activeSelf);
                Assert.IsFalse(button.transform.Find("Menu icon").gameObject.activeSelf);
                Assert.IsFalse(button.transform.Find("Notification").gameObject.activeSelf);
                Assert.IsTrue(button.IsInteractable());
                button.onClick.Invoke(); yield return null;
                Assert.IsNull(host.ActivePageKey);
                Assert.IsFalse(button.transform.Find("Close icon").gameObject.activeSelf);
                Assert.IsTrue(button.transform.Find("Menu icon").gameObject.activeSelf);
            }
            screen.Navigate(4); yield return null;
            Assert.IsNull(host.ActivePageKey,"The removed fifth navigation index must not open a page");
            screen.Navigate(0); screen.Navigate(1); yield return null;
            Assert.AreEqual("dungeons",host.ActivePageKey);
            Assert.IsTrue(screen.navigation[0].transform.Find("Menu icon").gameObject.activeSelf);
            Assert.IsTrue(screen.navigation[1].transform.Find("Close icon").gameObject.activeSelf);
            host.Registry.Open("child"); yield return null;
            Assert.IsFalse(screen.navigation[1].IsInteractable());
            screen.Navigate(1);
            Assert.AreEqual(1,host.ModalDepth);
            Assert.AreEqual("dungeons",host.ActivePageKey);
            host.CloseTop(); yield return null;
            Assert.IsTrue(screen.navigation[1].IsInteractable());
            lastContext.Close(); yield return null;
            Assert.IsNull(host.ActivePageKey);
            Assert.IsTrue(screen.navigation[1].transform.Find("Menu icon").gameObject.activeSelf);
            screen.Navigate(3); host.CloseTop(); yield return null;
            Assert.IsNull(host.ActivePageKey);
            Assert.IsTrue(screen.navigation[3].transform.Find("Menu icon").gameObject.activeSelf);
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
        public IEnumerator PageScenery_CoversViewportAcrossInsetsAndResize_AndClosesWithPage()
        {
            assets.worldBackground=Resources.Load<Sprite>("Moonlit/Social/ProfileRuins-v1");
            Assert.IsNotNull(assets.worldBackground);
            RectTransform backdrop=null, safeRoot=null;
            host.Registry.Register("scenery",ScreenPresentation.Page,context=>{
                safeRoot=context.Root;
                backdrop=PopupSkin.FullViewportBackdrop(context);
            });
            var pages=(RectTransform)root.transform.Find("pages");
            pages.anchorMin=pages.anchorMax=new Vector2(.5f,.5f);
            foreach(int height in new[]{1920,2280,1920})
            {
                pages.sizeDelta=new Vector2(1080,height);
                host.SetPreviewMetrics(new Vector2Int(1080,height),new Rect(36,84,1008,height-168));
                host.Registry.Open("scenery");
                yield return null;
                Canvas.ForceUpdateCanvases();
                Assert.AreSame(safeRoot.parent,backdrop.parent);
                Assert.IsNull(backdrop.parent.GetComponentInParent<RectMask2D>(),"Decoration must not inherit the SafeArea clip");
                var clip=backdrop.GetComponent<RectMask2D>();
                Assert.IsNotNull(clip,"Page scenery must stop above navigation even when canvas render ordering differs");
                float navTop=backdrop.InverseTransformPoint(safeRoot.TransformPoint(new Vector3(0,safeRoot.rect.yMin+PortraitSafeArea.NavigationTopFromBottom,0))).y;
                Assert.That(backdrop.rect.yMin+clip.padding.y,Is.EqualTo(navTop).Within(.1f));
                Assert.AreEqual(0,clip.padding.x); Assert.AreEqual(0,clip.padding.z); Assert.AreEqual(0,clip.padding.w);
                Assert.AreEqual(0,backdrop.GetComponentsInChildren<Graphic>().Count(g=>g.raycastTarget));
                Assert.IsNull(backdrop.GetComponent<Graphic>(),"Opaque backing must be a mask child so it cannot cover navigation");
                var painting=backdrop.Find("Page scenery").GetComponent<RectTransform>();
                Assert.GreaterOrEqual(painting.rect.width,backdrop.rect.width-.1f);
                Assert.GreaterOrEqual(painting.rect.height,backdrop.rect.height-.1f);
                Assert.That(painting.rect.width/painting.rect.height,
                    Is.EqualTo(assets.worldBackground.rect.width/assets.worldBackground.rect.height).Within(.001f));
                Assert.That(backdrop.rect.width,Is.EqualTo(1080).Within(.1f));
                Assert.That(backdrop.rect.height,Is.EqualTo(height).Within(.1f));
                Assert.That(safeRoot.localScale.x,Is.EqualTo(1008f/1080f).Within(.001f));
                Assert.IsNotNull(safeRoot.GetComponent<RectMask2D>(),"Controls still use the safe navigation clip");
            }
            host.ClosePage();
            yield return null;
            Assert.IsTrue(backdrop==null,"Closing a page must remove its scenery too");
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
            var screen=host.GetComponent<MainScreen>();
            var oldRewards=RewardState.Current;var oldForge=ForgeState.Current;
            try {
                long start=System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()+10000;
                var rewards=RewardState.Current=new RewardState{lastTickUtc=start};
                int goldBefore=screen.gold,oreBefore=screen.ore;
                rewards.Advance(start+174);rewards.Advance(start+174);rewards.Advance(start+120);
                Assert.AreEqual(174,rewards.GoldAvailable);Assert.AreEqual(2,rewards.HammersAvailable);
                Assert.IsTrue(rewards.Claim(screen));Assert.IsFalse(rewards.Claim(screen));
                Assert.AreEqual(goldBefore+174,screen.gold);Assert.AreEqual(oreBefore+2,screen.ore);
                rewards.Advance(start+180);
                Assert.AreEqual(6,rewards.GoldAvailable);Assert.AreEqual(1,rewards.HammersAvailable);
                Assert.IsFalse(rewards.Claim(screen),"A minute must accumulate after the previous claim.");
                rewards.Advance(start+234);
                Assert.IsTrue(rewards.Claim(screen));Assert.IsFalse(rewards.Claim(screen));
                Assert.AreEqual(goldBefore+234,screen.gold);Assert.AreEqual(oreBefore+3,screen.ore);
                var state=ForgeState.Current=new ForgeState();
                var original=new EquipmentRoll{id=41,part=EquipmentPart.Weapon};
                var first=new EquipmentRoll{id=42,part=EquipmentPart.Weapon,level=2};
                var second=new EquipmentRoll{id=43,part=EquipmentPart.Weapon,level=3};
                state.equipped[5]=original;state.pending.Add(first);state.pending.Add(second);
                Assert.IsFalse(state.ToggleEquip(999));Assert.IsFalse(state.SellPending(999,out _));
                Assert.IsTrue(state.SellPending(first.id,out int sale));
                Assert.AreEqual(EquipmentRules.SaleGold(first),sale);Assert.AreSame(second,state.Pending);
                Assert.IsFalse(state.SellPending(first.id,out _),"A stale sale callback cannot dispose of the next pending item");
                Assert.IsFalse(state.ToggleEquip(first.id),"A stale equip callback cannot equip the next pending item");
                Assert.AreSame(original,state.equipped[5]);Assert.AreSame(second,state.Pending);
                Assert.IsTrue(state.ToggleEquip(second.id));Assert.AreSame(second,state.equipped[5]);
                Assert.IsTrue(state.SellPending(original.id,out _));Assert.IsNull(state.Pending);
                Assert.IsFalse(state.SellPending(original.id,out _));
            }
            finally {RewardState.Current=oldRewards;ForgeState.Current=oldForge;}
        }

        [UnityTest]
        public IEnumerator Shop_HasExactlyTheFiveRequiredOffers_AndCanReturnToMain()
        {
#if UNITY_EDITOR
            var authored=UnityEditor.AssetDatabase.LoadAssetAtPath<MainScreenAssets>("Assets/DarkFantasyUI/Data/MainScreenAssets.asset");
            Assert.IsNotNull(authored,"The shop fixture must use the same authored currency artwork as the runtime factory.");
            Assert.IsNotNull(authored.interfaceIcons);Assert.Greater(authored.interfaceIcons.Length,1);
            assets.interfaceIcons=authored.interfaceIcons;
            Assert.IsNotNull(assets.interfaceIcons[1],"The authored diamond sprite must be imported.");
#else
            Assert.Ignore("The authored runtime catalog fixture requires the cloud Editor.");
            yield break;
#endif
            PrepareMainLabels();
            SocialScreenModule.Register(host.Registry);
            host.Registry.Open("shop"); yield return null;
            Assert.AreSame(assets.interfaceIcons[1],GameObject.Find("Daily diamond icon").GetComponent<Image>().sprite);
            var expected = new[] { 600, 2200, 8000, 15000, 33000 };
            foreach (var amount in expected) Assert.IsNotNull(GameObject.Find("Gem offer " + amount));
            var scroll = Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Single();
            Assert.AreEqual(expected.Length, scroll.content.Cast<Transform>().Count(t => t.name.StartsWith("Gem offer ")));
            Assert.IsTrue(scroll.vertical);
            host.CloseTop(); yield return null; // System/context back closes the page; the removed top <메인> button must not return.
            Assert.IsNull(host.ActivePageKey);
            Assert.IsTrue(main.interactable);
        }

        [UnityTest]
        public IEnumerator CollectionQuickEquip_DoesNotAccumulateSlots_AndChildKeepsScroll()
        {
            PrepareMainLabels();
            foreach(var entry in CollectionProgression.Data.categories[0].entries.Take(3))entry.unlocked=true;
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
