using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Moonlit.UI.Tests
{
    public sealed class SocialScreenLayoutTests
    {
        GameObject root;
        UiScreenHost host;
        MainScreenAssets assets;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("social layout test", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            // Match the runtime canvas: the hosted test Game View need not be 1080 pixels wide.
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0;
            var popup = Child("popups", root.transform);
            var pages = Child("pages", root.transform);
            var main = Child("main", root.transform).gameObject.AddComponent<CanvasGroup>();
            var navigation = Child("navigation", root.transform).gameObject.AddComponent<CanvasGroup>();
            var screen = root.AddComponent<MainScreen>();
            screen.enabled = false;
            assets = ScriptableObject.CreateInstance<MainScreenAssets>();
            assets.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            screen.font = assets.font;
            screen.toastRoot = root.transform;
            host = root.AddComponent<UiScreenHost>();
            host.Initialize(assets, screen, popup, pages, main, navigation);
            host.SetPreviewMetrics(new Vector2Int(1080, 1920), new Rect(0, 0, 1080, 1920));
            SocialScreenModule.Register(host.Registry);
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
        public IEnumerator Shop_ReservesNavigation_AndHasOnlyRequiredOffersAtBothAspectRatios()
        {
            foreach (var height in new[] { 1920, 2280 })
            {
                host.SetPreviewMetrics(new Vector2Int(1080, height), new Rect(0, 0, 1080, height));
                host.Registry.Open("shop");
                yield return null;
                var scroll = GameObject.Find("Shop page").GetComponentInChildren<ScrollRect>();
                Assert.LessOrEqual(scroll.GetComponent<RectTransform>().rect.height, scroll.transform.parent.GetComponent<RectTransform>().rect.height - 330f);
                CollectionAssert.AreEquivalent(new[] { "Gem offer 60", "Gem offer 220", "Gem offer 800", "Gem offer 1500", "Gem offer 3300" },
                    scroll.content.Cast<Transform>().Where(t => t.name.StartsWith("Gem offer ")).Select(t => t.name));
                var illustrations = scroll.content.GetComponentsInChildren<Image>(true)
                    .Where(i => i.name == "Ruby artwork").ToArray();
                Assert.AreEqual(5, illustrations.Length);
                Assert.IsTrue(illustrations.All(i => i.sprite != null && i.preserveAspect && !i.raycastTarget),
                    "All five offers need imported, separate non-interactive illustrations.");
                Assert.AreEqual(5, illustrations.Select(i => i.sprite.rect).Distinct().Count());
                Assert.AreEqual(3, scroll.content.GetComponentsInChildren<Image>(true)
                    .Count(i => i.name == "Deal illustration" && i.sprite != null));
                Assert.AreEqual("가격 미설정", GameObject.Find("Gem offer 1500").GetComponentInChildren<Button>().name);
                Assert.AreEqual("가격 미설정", GameObject.Find("Gem offer 3300").GetComponentInChildren<Button>().name);
                host.CloseTop();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Pvp_ActionAndStickyRowStayAboveReservedNavigation()
        {
            host.SetPreviewMetrics(new Vector2Int(1080, 1920), new Rect(0, 80, 1080, 1740));
            host.Registry.Open("pvp");
            yield return null;
            var action = GameObject.Find("도전").GetComponent<RectTransform>();
            var sticky = GameObject.Find("My sticky rank").GetComponent<RectTransform>();
            float safeHeight = action.transform.parent.GetComponent<RectTransform>().rect.height;
            Assert.LessOrEqual(-action.anchoredPosition.y + action.rect.height, safeHeight - 210f);
            Assert.LessOrEqual(-sticky.anchoredPosition.y + sticky.rect.height, safeHeight - 210f);
        }

        [UnityTest]
        public IEnumerator RankingChildClose_PreservesParentScrollInstanceAndPosition()
        {
            host.Registry.Open("power-ranking");
            yield return null;
            var scroll = Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Single();
            scroll.verticalNormalizedPosition = .42f;
            Canvas.ForceUpdateCanvases();
            var row = GameObject.Find("Rank 5").GetComponent<RectTransform>();
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, row.TransformPoint(row.rect.center)) };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits);
            Assert.AreSame(row.GetComponent<Button>(), hits[0].gameObject.GetComponentInParent<Button>());
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return null;
            var childPortrait = GameObject.Find("Popup Layer player-details").GetComponentsInChildren<Image>()
                .Single(i => i.name == "Avatar artwork");
            Assert.AreSame(row.GetComponentsInChildren<Image>().Single(i => i.name == "Avatar artwork").sprite, childPortrait.sprite);
            host.CloseTop();
            yield return null;
            Assert.AreSame(scroll, Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Single());
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(.42f).Within(.01f));
        }

        [UnityTest]
        public IEnumerator PointerInsideModalFrame_DoesNotDismissThroughBackdrop()
        {
            host.Registry.Open("profile"); yield return null;
            Canvas.ForceUpdateCanvases();
            var frame = GameObject.Find("프로필 frame").GetComponent<RectTransform>();
            var point = new Vector2(frame.rect.xMin + 18, frame.rect.center.y);
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, frame.TransformPoint(point)) };
            Assert.IsTrue(new Rect(0, 0, Screen.width, Screen.height).Contains(pointer.position),
                $"Profile frame test point {pointer.position} must lie inside the actual {Screen.width}x{Screen.height} test viewport.");
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits);
            Assert.IsTrue(hits[0].gameObject.transform.IsChildOf(frame), "The modal frame or one of its children must intercept the pointer.");
            Assert.AreNotEqual("Dim", hits[0].gameObject.name);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.AreEqual(1, host.ModalDepth);
        }

        [UnityTest]
        public IEnumerator AvatarChange_UsesPortraitAtlas_AndSurvivesProfileReopen()
        {
            host.Registry.Open("profile"); yield return null;
            var portrait = GameObject.Find("Profile content").GetComponentsInChildren<Image>()
                .Single(i => i.name == "Avatar artwork");
            Assert.IsNotNull(portrait.sprite);
            Assert.AreEqual("AvatarPortraits-v1", portrait.sprite.texture.name);
            var original = portrait.sprite;
            GameObject.Find("아바타 변경").GetComponent<Button>().onClick.Invoke();
            var changed = portrait.sprite;
            Assert.AreNotEqual(original.rect, changed.rect);
            Assert.AreSame(original.texture, changed.texture);
            host.CloseTop(); yield return null;
            host.Registry.Open("profile"); yield return null;
            Assert.AreSame(changed, GameObject.Find("Profile content").GetComponentsInChildren<Image>()
                .Single(i => i.name == "Avatar artwork").sprite);
            // Restore demo selection so this test does not change subsequent test data.
            for (int i = 0; i < 8; i++) GameObject.Find("아바타 변경").GetComponent<Button>().onClick.Invoke();
        }

        [UnityTest]
        public IEnumerator Pvp_StickyIdentityAndDetailsMatchStandings_AndRewardsOpen()
        {
            // Use a shorter safe viewport so the standings actually overflow and scroll.
            host.SetPreviewMetrics(new Vector2Int(1080, 1920), new Rect(0, 200, 1080, 1500));
            host.Registry.Open("pvp"); yield return null;
            Assert.IsNotNull(GameObject.Find("Gold league crest").GetComponent<Image>().sprite);
            Assert.IsNotNull(GameObject.Find("Season gift").GetComponent<Image>().sprite);
            int[] expectedStars = { 15, 14, 13, 11, 4, 2, 0 };
            for (int i = 0; i < expectedStars.Length; i++)
                Assert.AreEqual(expectedStars[i].ToString(),
                    GameObject.Find("PvP rank " + (8 + i)).transform.Find("Stars").GetComponent<Text>().text);
            var mine = GameObject.Find("PvP rank 11").transform;
            var sticky = GameObject.Find("My sticky rank").transform;
            foreach (var label in new[] { "Rank", "Player", "Power", "Stars", "Server" })
                Assert.AreEqual(mine.Find(label).GetComponent<Text>().text, sticky.Find(label).GetComponent<Text>().text);
            var portrait = sticky.GetComponentsInChildren<Image>().Single(i => i.name == "Avatar artwork").sprite;
            Assert.AreSame(mine.GetComponentsInChildren<Image>().Single(i => i.name == "Avatar artwork").sprite, portrait);
            var scroll = GameObject.Find("PvP page").GetComponentInChildren<ScrollRect>();
            scroll.verticalNormalizedPosition = .37f; Canvas.ForceUpdateCanvases();
            sticky.GetComponent<Button>().onClick.Invoke(); yield return null;
            var details = GameObject.Find("Popup Layer player-details");
            Assert.IsNotNull(details);
            Assert.AreSame(portrait, details.GetComponentsInChildren<Image>().Single(i => i.name == "Avatar artwork").sprite);
            StringAssert.Contains("moonzzanf", details.GetComponentsInChildren<Text>().Single(t => t.name == "Player").text);
            host.CloseTop(); yield return null;
            Assert.AreSame(scroll, GameObject.Find("PvP page").GetComponentInChildren<ScrollRect>());
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(.37f).Within(.01f));
            GameObject.Find("Season rewards").GetComponent<Button>().onClick.Invoke(); yield return null;
            Assert.IsNotNull(GameObject.Find("Popup Layer pvp-rewards"));
            Assert.IsFalse(sticky.GetComponent<Button>().IsInteractable());
        }

        [UnityTest]
        public IEnumerator Profile_SaveAndGenderSurviveReopen_AndControlsDoNotOverlap()
        {
            host.Registry.Open("profile"); yield return null;
            var input = GameObject.Find("Profile name").GetComponent<InputField>();
            string originalName = input.text;
            string originalGender = GameObject.Find("Profile gender").GetComponent<InputField>().text;
            var inputRect = input.GetComponent<RectTransform>();
            var save = GameObject.Find("저장").GetComponent<Button>();
            Assert.Less(inputRect.anchoredPosition.x + inputRect.rect.width,
                save.GetComponent<RectTransform>().anchoredPosition.x, "Save must not cover the editable name.");
            input.text = "Moonlit QA"; save.onClick.Invoke();
            GameObject.Find("변경").GetComponent<Button>().onClick.Invoke();
            string changedGender = GameObject.Find("Profile gender").GetComponent<InputField>().text;
            Assert.AreNotEqual(originalGender, changedGender);
            host.CloseTop(); yield return null;
            host.Registry.Open("profile"); yield return null;
            input = GameObject.Find("Profile name").GetComponent<InputField>();
            Assert.AreEqual("Moonlit QA", input.text);
            Assert.AreEqual(changedGender, GameObject.Find("Profile gender").GetComponent<InputField>().text);
            input.text = " "; GameObject.Find("저장").GetComponent<Button>().onClick.Invoke();
            host.CloseTop(); yield return null;
            host.Registry.Open("profile"); yield return null;
            input = GameObject.Find("Profile name").GetComponent<InputField>();
            Assert.AreEqual("Moonlit QA", input.text, "Blank save must not replace the previous name.");
            // Restore session state used by the other screen tests.
            input.text = originalName; GameObject.Find("저장").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("변경").GetComponent<Button>().onClick.Invoke();
        }

        [UnityTest]
        public IEnumerator Settings_ReopenRetainsSwitch_OffThumbRemainsVisible_AndTabsUpdateTitle()
        {
            host.Registry.Open("settings"); yield return null;
            var toggle = GameObject.Find("음악 toggle").GetComponent<Toggle>();
            bool original = toggle.isOn;
            toggle.isOn = !original;
            host.CloseTop(); yield return null;
            host.Registry.Open("settings"); yield return null;
            toggle = GameObject.Find("음악 toggle").GetComponent<Toggle>();
            Assert.AreEqual(!original, toggle.isOn);
            toggle.isOn = false; yield return null;
            var thumb = toggle.transform.Find("Thumb").GetComponent<Image>();
            Assert.IsTrue(thumb.enabled);
            Assert.Greater(thumb.canvasRenderer.GetAlpha(), .99f, "Off must show the switch thumb, not fade it out.");
            Assert.AreEqual(6f, thumb.rectTransform.anchoredPosition.x);
            toggle.isOn = true;
            Assert.AreEqual(80f, thumb.rectTransform.anchoredPosition.x);
            toggle.isOn = original;
            GameObject.Find("프로필").GetComponent<Button>().onClick.Invoke();
            var frame = GameObject.Find("설정 frame").transform;
            Assert.AreEqual("프로필", frame.Find("Title").GetComponent<Text>().text);
            Assert.IsNotNull(GameObject.Find("Profile name"));
            GameObject.Find("설정").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual("설정", frame.Find("Title").GetComponent<Text>().text);
        }

        [UnityTest]
        public IEnumerator SharedPopupSkin_LoadsSeparateArtwork_AndCloseWorksAcrossModules()
        {
            ForgeScreenModule.Register(host.Registry);
            ProgressionScreenModule.Register(host.Registry);
            Assert.IsNotNull(PopupSkin.PanelArt);
            Assert.IsNotNull(PopupSkin.ActionArt);
            Assert.IsNotNull(PopupSkin.CloseArt);
            Assert.AreNotSame(PopupSkin.PanelArt.texture, PopupSkin.ActionArt.texture);
            Assert.AreNotSame(PopupSkin.ActionArt.texture, PopupSkin.CloseArt.texture);
            Assert.Greater(PopupSkin.PanelArt.border.x, 0);
            Assert.Greater(PopupSkin.ActionArt.border.x, 0);
            foreach (string route in new[] { "profile", "forge-probability", "skill-details" })
            {
                host.Registry.Open(route); yield return null;
                var layer = GameObject.Find("Popup Layer " + route);
                var frames = layer.GetComponentsInChildren<Image>().Where(i => i.sprite == PopupSkin.PanelArt).ToArray();
                Assert.IsTrue(frames.Any(i => i.type == Image.Type.Sliced && i.raycastTarget),
                    route + " must retain a sliced frame that intercepts interior touches.");
                if (route == "skill-details")
                {
                    var upgrade = layer.GetComponentsInChildren<Button>().Single(b => b.name == "Upgrade");
                    var equip = layer.GetComponentsInChildren<Button>().Single(b => b.name == "Equip");
                    Assert.AreSame(PopupSkin.PanelArt, ((Image)upgrade.targetGraphic).sprite,
                        "Neutral secondary actions must not be misclassified as saturated blue.");
                    Assert.AreSame(PopupSkin.ActionArt, ((Image)equip.targetGraphic).sprite);
                }
                var close = layer.GetComponentsInChildren<Button>().Single(b => b.name == "Close");
                Assert.AreSame(PopupSkin.CloseArt, ((Image)close.targetGraphic).sprite);
                Assert.IsFalse(close.targetGraphic.raycastTarget, "Artwork must not replace the independent hit surface.");
                Assert.AreEqual("×", close.GetComponentInChildren<Text>().text);
                close.onClick.Invoke(); yield return null;
                Assert.AreEqual(0, host.ModalDepth);
            }
        }


        [UnityTest]
        public IEnumerator Settings_UsesSeparateSilverIcons_AndListLinksRemainClickable()
        {
            host.Registry.Open("settings"); yield return null;
            var layer = GameObject.Find("Popup Layer settings");
            var icons = layer.GetComponentsInChildren<Image>()
                .Where(i => i.name.StartsWith("Settings icon ")).ToArray();
            Assert.AreEqual(10, icons.Length);
            Assert.IsTrue(icons.All(i => i.sprite != null && !i.raycastTarget));
            Assert.AreEqual(10, icons.Select(i => i.sprite.rect).Distinct().Count());
            Assert.AreEqual(1, icons.Select(i => i.sprite.texture).Distinct().Count());
            for (int i = 0; i < 4; i++)
            {
                var link = layer.GetComponentsInChildren<Button>().Single(b => b.name == "Settings link " + i);
                Assert.IsTrue(link.IsInteractable());
                Assert.IsTrue(link.GetComponent<Image>().raycastTarget);
                link.onClick.Invoke(); yield return null;
                Assert.AreEqual(1, host.ModalDepth, "A local settings link must not dismiss its parent.");
            }
            layer.GetComponentsInChildren<Button>().Single(b => b.name == "Close").onClick.Invoke();
            yield return null;
            Assert.AreEqual(0, host.ModalDepth);
        }


        [UnityTest]
        public IEnumerator ForgeProbability_LoadsSeparateTierArt_AndPreservesRatesAfterDetails()
        {
            ForgeScreenModule.Register(host.Registry);
            host.Registry.Open("forge-probability"); yield return null;
            var layer = GameObject.Find("Popup Layer forge-probability");
            var bands = layer.GetComponentsInChildren<Image>().Where(i => i.name.StartsWith("Rarity ")).ToArray();
            var icons = layer.GetComponentsInChildren<Image>().Where(i => i.name == "Tier icon").ToArray();
            Assert.AreEqual(10, bands.Length);
            Assert.AreEqual(10, icons.Length);
            Assert.IsTrue(bands.All(i => i.sprite != null && i.type == Image.Type.Sliced && !i.raycastTarget));
            Assert.IsTrue(icons.All(i => i.sprite != null && i.preserveAspect && !i.raycastTarget));
            Assert.AreEqual(10, bands.Select(i => i.sprite.rect).Distinct().Count());
            Assert.AreEqual(10, icons.Select(i => i.sprite.rect).Distinct().Count());
            Assert.AreNotSame(bands[0].sprite.texture, icons[0].sprite.texture);
            var quantum = bands.Single(i => i.name == "Rarity 양자");
            Assert.AreEqual("58%", quantum.transform.Find("Current").GetComponent<Text>().text);
            Assert.AreEqual("64%", quantum.transform.Find("Next").GetComponent<Text>().text);
            layer.GetComponentsInChildren<Button>().Single(b => b.name == "i").onClick.Invoke();
            yield return null;
            Assert.AreEqual(2, host.ModalDepth);
            host.CloseTop(); yield return null;
            Assert.AreEqual(1, host.ModalDepth);
            Assert.AreSame(layer, GameObject.Find("Popup Layer forge-probability"));
            Assert.AreEqual("64%", quantum.transform.Find("Next").GetComponent<Text>().text);
        }

        static RectTransform Child(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Ui.Stretch(rect);
            return rect;
        }
    }
}
