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
            MoonlitRuntimeSettings.ResetSession();
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
            // Minimal wallet sprite injection; hosted captures use the real serialized catalog.
            assets.interfaceIcons = new[] { PopupSkin.CloseArt, PopupSkin.CloseArt };
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
            MoonlitRuntimeSettings.ResetSession();
        }

        [UnityTest]
        public IEnumerator Chat_TabsPreserveDraftsScrollAndLocalMessages()
        {
            foreach(int height in new[]{1920,2280}) {
                host.SetPreviewMetrics(new Vector2Int(1080,height),new Rect(36,84,1008,height-168));
                host.Registry.Open("chat"); yield return null;
                var chat=GameObject.Find("Chat");
                var world=chat.transform.Find("Chat channel 0");
                var clan=chat.transform.Find("Chat channel 1");
                var scroll=world.GetComponentInChildren<ScrollRect>();
                var input=chat.GetComponentInChildren<InputField>();
                var worldTab=chat.GetComponentsInChildren<Button>().Single(b=>b.name=="월드");
                var clanTab=chat.GetComponentsInChildren<Button>().Single(b=>b.name=="클랜");
                Assert.AreSame(PopupSkin.ActionArt,((Image)worldTab.targetGraphic).sprite);
                Assert.AreSame(PopupSkin.PanelArt,((Image)clanTab.targetGraphic).sprite);
                Assert.AreEqual("ProfileRuins-v1",GameObject.Find("Full viewport backdrop").transform.Find("Page scenery").GetComponent<Image>().sprite.name);
                scroll.content.sizeDelta=new Vector2(0,3000); Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition=.37f;
                input.text="월드 초안";
                clanTab.onClick.Invoke(); yield return null;
                Assert.IsFalse(world.gameObject.activeSelf);
                Assert.IsTrue(clan.gameObject.activeSelf);
                Assert.AreEqual("",input.text);
                input.text="클랜 초안";
                worldTab.onClick.Invoke(); yield return null;
                Assert.AreEqual("월드 초안",input.text);
                Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(.37f).Within(.02f));
                input.text="<b>로컬 메시지</b>";
                var send=chat.GetComponentsInChildren<Button>().Single(b=>b.name=="전송");
                send.onClick.Invoke(); yield return null;
                var messages=world.GetComponentsInChildren<Text>().Where(t=>t.name=="Message").ToArray();
                Assert.AreEqual(13,messages.Length);
                Assert.AreEqual("<b>로컬 메시지</b>",messages.Last().text);
                Assert.IsFalse(messages.Last().supportRichText);
                Assert.AreEqual("",input.text);
                send.onClick.Invoke(); yield return null;
                Assert.AreEqual(13,world.GetComponentsInChildren<Text>().Count(t=>t.name=="Message"));
                clanTab.onClick.Invoke(); yield return null;
                Assert.AreEqual("클랜 초안",input.text);
                Assert.AreEqual(12,clan.GetComponentsInChildren<Text>().Count(t=>t.name=="Message"));
                GameObject.Find("Chat back").GetComponent<Button>().onClick.Invoke(); yield return null;
                Assert.AreEqual(0,host.ModalDepth);
                Assert.IsNull(GameObject.Find("Chat"));
            }
        }

        [UnityTest]
        public IEnumerator Shop_ReservesNavigation_AndHasOnlyRequiredOffersAtBothAspectRatios()
        {
            foreach (var height in new[] { 1920, 2280 })
            {
                host.SetPreviewMetrics(new Vector2Int(1080, height),height==1920?new Rect(0,60,1080,1740):new Rect(36,84,1008,2076));
                host.Registry.Open("shop");
                yield return null;
                var shop=GameObject.Find("Shop page").transform;
                Assert.IsNull(shop.Find("‹ 메인"));
                var gold=shop.Find("Gold wallet").GetComponent<RectTransform>();
                var ruby=shop.Find("Ruby wallet").GetComponent<RectTransform>();
                var title=shop.Find("Shop title frame").GetComponent<RectTransform>();
                Assert.That(title.anchoredPosition.x+title.rect.width/2,Is.EqualTo(540).Within(.1f));
                Assert.Less(gold.anchoredPosition.x+gold.rect.width,title.anchoredPosition.x);
                Assert.Greater(ruby.anchoredPosition.x,title.anchoredPosition.x+title.rect.width);
                var scroll = shop.GetComponentInChildren<ScrollRect>();
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
                Assert.AreEqual(14,scroll.content.GetComponentsInChildren<RectTransform>(true).Count(t=>t.name.StartsWith("Reward cell ")));
                foreach(var art in scroll.content.GetComponentsInChildren<Image>(true).Where(i=>i.name=="Deal illustration" || i.name=="Ruby artwork"))
                {
                    var card=art.transform.parent.GetComponent<RectTransform>();
                    Assert.Greater(art.rectTransform.rect.height,card.rect.height*.6f,"Illustrations should fill the card, not sit as tiny thumbnails.");
                    Assert.IsFalse(card.Find("Card rim").GetComponent<Image>().fillCenter);
                    Assert.AreEqual("ProfileRuins-v1",card.Find("Card painting crop/Card painting").GetComponent<Image>().sprite.name,
                        "Store cards must not repeat the main party painting");
                    var rim=card.Find("Card rim").GetComponent<Image>();
                    float coveredHeight=(rim.sprite.border.y+rim.sprite.border.w)/(rim.pixelsPerUnit*rim.pixelsPerUnitMultiplier);
                    Assert.Less(coveredHeight,card.rect.height*.15f);
                }
                foreach(int amount in new[]{60,220,800}) {
                    var card=GameObject.Find("Gem offer "+amount).GetComponent<RectTransform>();
                    Assert.LessOrEqual(-card.anchoredPosition.y+card.rect.height,scroll.viewport.rect.height,
                        "The first three gem prices should fit at the initial scroll position even with cutouts");
                }
                Assert.AreEqual("가격 미설정", GameObject.Find("Gem offer 1500").GetComponentInChildren<Button>().name);
                Assert.AreEqual("가격 미설정", GameObject.Find("Gem offer 3300").GetComponentInChildren<Button>().name);
                host.CloseTop();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Pvp_AllOneHundredRanksScrollAndRestoreAfterDetails()
        {
            foreach (int height in new[] { 1920,2280 })
            {
                host.SetPreviewMetrics(new Vector2Int(1080,height),new Rect(36,84,1008,height-168));
                host.Registry.Open("pvp"); yield return null;
                var scroll=GameObject.Find("PvP page").GetComponentInChildren<ScrollRect>();
                var rows=scroll.content.Cast<Transform>().Where(t=>t.name.StartsWith("PvP rank ")).ToArray();
                Assert.AreEqual(100,rows.Length);
                CollectionAssert.AreEqual(Enumerable.Range(1,100).Select(i=>i.ToString()),
                    rows.Select(t=>t.Find("Rank").GetComponent<Text>().text));
                Canvas.ForceUpdateCanvases();
                Assert.Greater(scroll.content.rect.height,scroll.viewport.rect.height);
                scroll.verticalNormalizedPosition=0; Canvas.ForceUpdateCanvases(); yield return null;
                var last=rows[99].GetComponent<RectTransform>();
                var inViewport=scroll.viewport.InverseTransformPoint(last.TransformPoint(last.rect.center));
                Assert.IsTrue(scroll.viewport.rect.Contains(inViewport),"Rank 100 must be reachable at the bottom.");
                rows[99].GetComponent<Button>().onClick.Invoke(); yield return null;
                Assert.AreEqual(1,host.ModalDepth);
                host.CloseTop(); yield return null;
                Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(0).Within(.01f));
                var back=GameObject.Find("Return to main").GetComponent<RectTransform>();
                Assert.Greater(-back.anchoredPosition.y,1000);
                back.GetComponent<Button>().onClick.Invoke(); yield return null;
                Assert.IsNull(host.ActivePageKey);
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
            Assert.AreEqual("AvatarPortraits-v2", portrait.sprite.texture.name);
            var original = portrait.sprite;
            GameObject.Find("아바타 변경").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.AreEqual(2,host.ModalDepth);
            Assert.IsFalse(GameObject.Find("아바타 변경").GetComponent<Button>().IsInteractable());
            var choices=GameObject.Find("Avatar scroll").GetComponentsInChildren<Button>();
            Assert.AreEqual(20,choices.Length);
            var artworks=GameObject.Find("Avatar scroll").GetComponentsInChildren<Image>().Where(i=>i.name=="Avatar artwork").ToArray();
            Assert.AreEqual(20,artworks.Select(i=>i.sprite.rect).Distinct().Count());
            foreach(var choice in choices)
            {
                var rim=choice.transform.Find("Avatar rim").GetComponent<Image>();
                Assert.IsFalse(rim.fillCenter); Assert.IsFalse(rim.raycastTarget);
                float coveredWidth=(rim.sprite.border.x+rim.sprite.border.z)/(rim.pixelsPerUnit*rim.pixelsPerUnitMultiplier);
                Assert.Greater(rim.rectTransform.rect.width-coveredWidth,rim.rectTransform.rect.width*.8f,
                    "The reusable frame must expose at least 80% of the portrait width.");
            }
            Canvas.ForceUpdateCanvases();
            var avatarScroll=GameObject.Find("Avatar scroll").GetComponent<ScrollRect>();
            Assert.LessOrEqual(avatarScroll.content.rect.height,avatarScroll.viewport.rect.height,
                "All five rows should fit at the standard portrait viewport.");
            choices.Single(b=>b.name=="Avatar choice 19").onClick.Invoke();
            var changed = portrait.sprite;
            Assert.AreNotEqual(original.rect, changed.rect);
            Assert.AreSame(original.texture, changed.texture);
            host.CloseTop(); yield return null;
            Assert.AreEqual(1,host.ModalDepth);
            Assert.IsTrue(GameObject.Find("아바타 변경").GetComponent<Button>().IsInteractable());
            host.CloseTop(); yield return null;
            host.Registry.Open("profile"); yield return null;
            Assert.AreSame(changed, GameObject.Find("Profile content").GetComponentsInChildren<Image>()
                .Single(i => i.name == "Avatar artwork").sprite);
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
            var save = GameObject.Find("이름 변경").GetComponent<Button>();
            foreach(var editName in new[]{"이름 변경","변경","아바타 변경"})
            {
                var editButton=GameObject.Find(editName).GetComponent<Button>();
                var pencil=editButton.transform.Find("Edit pencil").GetComponent<Image>();
                Assert.IsNotNull(pencil.sprite);
                Assert.AreEqual("EditPencil-v1",pencil.sprite.name);
                Assert.IsFalse(pencil.raycastTarget);
                Assert.IsNull(editButton.transform.Find("Label"),"Compact edit buttons use separate artwork, without overflowing labels.");
            }
            Assert.Less(inputRect.anchoredPosition.x + inputRect.rect.width,
                save.GetComponent<RectTransform>().anchoredPosition.x, "Edit button must not cover the displayed name.");
            Assert.IsTrue(input.readOnly);
            var main=root.GetComponent<MainScreen>(); main.gems=500;
            save.onClick.Invoke(); yield return null;
            var edit=GameObject.Find("Nickname input").GetComponent<InputField>();
            var confirm=GameObject.Find("Nickname confirm").GetComponent<Button>();
            edit.text=" "; Assert.IsFalse(confirm.interactable);
            confirm.onClick.Invoke(); Assert.AreEqual(originalName,input.text); Assert.AreEqual(500,main.gems);
            edit.text="Cancelled"; GameObject.Find("Nickname cancel").GetComponent<Button>().onClick.Invoke(); yield return null;
            Assert.AreEqual(originalName,input.text); Assert.AreEqual(500,main.gems);
            save.onClick.Invoke(); yield return null;
            edit=GameObject.Find("Nickname input").GetComponent<InputField>();
            confirm=GameObject.Find("Nickname confirm").GetComponent<Button>();
            edit.text="Moonlit QA"; Assert.IsTrue(confirm.interactable);
            confirm.onClick.Invoke(); confirm.onClick.Invoke(); yield return null;
            Assert.AreEqual(300,main.gems,"Repeated stale callback must not charge twice");
            Assert.AreEqual(1,host.ModalDepth);
            GameObject.Find("변경").GetComponent<Button>().onClick.Invoke();
            yield return null;
            GameObject.Find("Gender 1").GetComponent<Button>().onClick.Invoke();
            string changedGender = GameObject.Find("Profile gender").GetComponent<InputField>().text;
            Assert.AreNotEqual(originalGender, changedGender);
            host.CloseTop(); yield return null;
            host.CloseTop(); yield return null;
            host.Registry.Open("profile"); yield return null;
            input = GameObject.Find("Profile name").GetComponent<InputField>();
            Assert.AreEqual("Moonlit QA", input.text);
            Assert.AreEqual(changedGender, GameObject.Find("Profile gender").GetComponent<InputField>().text);
            main.gems=21; GameObject.Find("이름 변경").GetComponent<Button>().onClick.Invoke(); yield return null;
            GameObject.Find("Nickname input").GetComponent<InputField>().text="Cannot afford";
            confirm=GameObject.Find("Nickname confirm").GetComponent<Button>();
            Assert.IsFalse(confirm.interactable); confirm.onClick.Invoke();
            Assert.AreEqual(21,main.gems); Assert.AreEqual("Moonlit QA",input.text);
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
            var track = toggle.transform.Find("Track").GetComponent<Image>();
            var offArt = track.sprite;
            Assert.IsNotNull(offArt);
            Assert.IsNotNull(thumb.sprite);
            Assert.AreNotEqual(offArt.rect, thumb.sprite.rect);
            Assert.IsFalse(track.raycastTarget);
            Assert.IsFalse(thumb.raycastTarget);
            Assert.IsTrue(toggle.GetComponent<Image>().raycastTarget);
            toggle.isOn = true;
            Assert.AreEqual(80f, thumb.rectTransform.anchoredPosition.x);
            Assert.IsNotNull(track.sprite);
            Assert.AreNotSame(offArt, track.sprite, "On must use the green track artwork.");
            Assert.AreSame(offArt.texture, track.sprite.texture);
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
            Assert.IsNotNull(PopupSkin.CrestArt);
            Assert.AreNotSame(PopupSkin.PanelArt.texture, PopupSkin.ActionArt.texture);
            Assert.AreNotSame(PopupSkin.ActionArt.texture, PopupSkin.CloseArt.texture);
            Assert.Greater(PopupSkin.PanelArt.border.x, 0);
            Assert.Greater(PopupSkin.ActionArt.border.x, 0);
            foreach (string route in new[] { "profile", "forge-probability", "skill-details" })
            {
                host.Registry.Open(route); yield return null;
                var layer = GameObject.Find("Popup Layer " + route);
                var crest = layer.GetComponentsInChildren<Image>().First(i => i.sprite == PopupSkin.CrestArt);
                Assert.IsTrue(crest.preserveAspect);
                Assert.IsFalse(crest.raycastTarget);
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
                Assert.AreEqual(i<3?2:1, host.ModalDepth, "Settings child dialogs retain their parent.");
                if(i<3)
                {
                    Assert.IsFalse(link.IsInteractable());
                    host.CloseTop(); yield return null;
                    Assert.AreSame(layer,GameObject.Find("Popup Layer settings"));
                    Assert.IsTrue(link.IsInteractable());
                }
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

        [UnityTest]
        public IEnumerator SettingsChildren_KeepExclusiveLanguageChoiceAndLocalBlockedState()
        {
            host.Registry.Open("settings"); yield return null;
            var parent=GameObject.Find("Popup Layer settings");
            GameObject.Find("Settings link 0").GetComponent<Button>().onClick.Invoke(); yield return null;
            var languages=GameObject.Find("Language scroll").GetComponentsInChildren<Toggle>();
            Assert.AreEqual(11,languages.Length);
            Assert.IsNotNull(PopupSkin.CheckboxArt,"Shared checkbox artwork must import successfully");
            foreach(var language in languages)
                Assert.AreSame(PopupSkin.CheckboxArt,((Image)language.targetGraphic).sprite);
            Assert.AreEqual(1,languages.Count(t=>t.isOn));
            Assert.IsTrue(languages.Single(t=>t.name=="Language 3").isOn);
            languages.Single(t=>t.name=="Language 0").isOn=true;
            Assert.AreEqual(1,languages.Count(t=>t.isOn));
            Assert.AreEqual(Color.white,languages.Single(t=>t.name=="Language 3").targetGraphic.color,
                "Changing language must clear the previous green frame");
            Assert.AreNotEqual(Color.white,languages.Single(t=>t.name=="Language 0").targetGraphic.color);
            host.CloseTop(); yield return null;
            Assert.AreSame(parent,GameObject.Find("Popup Layer settings"));
            GameObject.Find("Settings link 0").GetComponent<Button>().onClick.Invoke(); yield return null;
            Assert.IsTrue(GameObject.Find("Language 0").GetComponent<Toggle>().isOn);
            host.CloseTop(); yield return null;
            GameObject.Find("Settings link 2").GetComponent<Button>().onClick.Invoke(); yield return null;
            Assert.AreEqual(3,GameObject.Find("Blocked players scroll").GetComponentsInChildren<Button>().Length);
            GameObject.Find("Blocked player 1").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("차단 해제").GetComponent<Button>().onClick.Invoke(); yield return null;
            Assert.AreEqual(2,GameObject.Find("Blocked players scroll").GetComponentsInChildren<Button>().Length);
            host.CloseTop(); yield return null;
            GameObject.Find("Settings link 2").GetComponent<Button>().onClick.Invoke(); yield return null;
            Assert.IsNull(GameObject.Find("Blocked player 1"));
            host.CloseTop(); yield return null;
            GameObject.Find("Settings link 1").GetComponent<Button>().onClick.Invoke(); yield return null;
            int gems=root.GetComponent<MainScreen>().gems;
            foreach(string action in new[]{"Account link","Account logout","Account delete"})
            {
                GameObject.Find(action).GetComponent<Button>().onClick.Invoke(); yield return null;
                Assert.AreEqual(2,host.ModalDepth);
                Assert.AreEqual(gems,root.GetComponent<MainScreen>().gems);
            }
            host.CloseTop(); yield return null;
            Assert.AreSame(parent,GameObject.Find("Popup Layer settings"));
            Assert.AreEqual("설정",parent.GetComponentsInChildren<Text>().Single(t=>t.name=="Title").text);
        }

        [UnityTest]
        public IEnumerator NewChildren_KeepInputInSafeAreaAndBackOnlyClosesTop_AtBothAspects()
        {
            foreach(int height in new[]{1920,2280})
            {
                host.SetPreviewMetrics(new Vector2Int(1080,height),new Rect(36,84,1008,height-204));
                foreach(string route in new[]{"profile-name","profile-gender","profile-avatar","settings-language","settings-blocked","settings-account"})
                {
                    bool profile=route.StartsWith("profile-");
                    host.Registry.Open(profile?"profile":"settings"); yield return null;
                    var parent=GameObject.Find("Popup Layer "+(profile?"profile":"settings"));
                    var parentClose=parent.GetComponentsInChildren<Button>().Single(b=>b.name=="Close");
                    host.Registry.Open(route); yield return null; Canvas.ForceUpdateCanvases();
                    Assert.AreEqual(2,host.ModalDepth);
                    Assert.IsFalse(parentClose.IsInteractable());
                    var layer=GameObject.Find("Popup Layer "+route);
                    var safe=(RectTransform)layer.transform.Find("SafeArea");
                    // Dim intentionally covers the whole display, including cutouts. Only dialog
                    // controls belong inside SafeArea; scroll content is clipped by its viewport.
                    var dim=layer.transform.Find("Dim").GetComponent<Button>();
                    Assert.IsTrue(dim.IsInteractable());
                    Assert.IsTrue(dim.GetComponent<Image>().raycastTarget);
                    foreach(var button in safe.GetComponentsInChildren<Button>().Where(b=>!b.GetComponentInParent<ScrollRect>()))
                    {
                        var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(safe,button.transform);
                        Assert.GreaterOrEqual(bounds.min.x,safe.rect.xMin-.5f,route+" left");
                        Assert.LessOrEqual(bounds.max.x,safe.rect.xMax+.5f,route+" right");
                        Assert.GreaterOrEqual(bounds.min.y,safe.rect.yMin-.5f,route+" bottom");
                        Assert.LessOrEqual(bounds.max.y,safe.rect.yMax+.5f,route+" top");
                    }
                    host.CloseTop(); yield return null;
                    Assert.AreEqual(1,host.ModalDepth); Assert.IsTrue(parentClose.IsInteractable());
                    Assert.AreSame(parent,GameObject.Find("Popup Layer "+(profile?"profile":"settings")));
                    host.CloseTop(); yield return null;
                }
            }
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
