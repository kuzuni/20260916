using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Moonlit.UI.Tests
{
    public sealed class ProgressionStateTests
    {
        GameObject root;
        UiScreenHost host;
        MainScreenAssets assets;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("progression test root", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
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
            screen.oreText = Label("ore"); screen.goldText = Label("gold"); screen.gemText = Label("gems");
            screen.powerText = Label("power"); screen.stageText = Label("stage"); screen.autoText = Label("auto");
            screen.equipment = new EquipmentSlot[0];
            host = root.AddComponent<UiScreenHost>();
            host.Initialize(assets, screen, popup, pages, main, navigation);
            host.SetPreviewMetrics(new Vector2Int(1080, 1920), new Rect(0, 0, 1080, 1920));
            ProgressionScreenModule.Register(host.Registry);
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
        public IEnumerator SkillUpgradeAndEquip_RefreshParentWithoutLosingTabOrScroll()
        {
            host.Registry.Open("skills-pets-heroes");
            yield return null;
            GameObject.Find("Tab 스킬").GetComponent<Button>().onClick.Invoke(); yield return null;
            var skillTab = GameObject.Find("Tab 스킬").GetComponent<Button>();
            var petTab = GameObject.Find("Tab 펫").GetComponent<Button>();
            var heroTab = GameObject.Find("Tab 영웅").GetComponent<Button>();
            Assert.IsNotNull(PopupSkin.ActionArt);
            Assert.IsNotNull(PopupSkin.PanelArt);
            petTab.onClick.Invoke(); yield return null;
            Assert.AreSame(PopupSkin.ActionArt, ((Image)petTab.targetGraphic).sprite);
            Assert.AreSame(PopupSkin.PanelArt, ((Image)skillTab.targetGraphic).sprite);
            skillTab.onClick.Invoke(); yield return null;
            Assert.AreSame(PopupSkin.ActionArt, ((Image)skillTab.targetGraphic).sprite);
            Assert.AreSame(PopupSkin.PanelArt, ((Image)petTab.targetGraphic).sprite);
            var scroll = Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Single();
            scroll.content.sizeDelta = new Vector2(0, 3000);
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = .41f;
            var parentSlot = GameObject.Find("Skill 핏빛 파편").GetComponent<Button>();
            var levelBefore = ChildText(parentSlot.transform, "Level").text;

            parentSlot.onClick.Invoke();
            yield return null;
            GameObject.Find("Upgrade").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("Equip").GetComponent<Button>().onClick.Invoke();
            host.CloseTop();
            yield return null;

            Assert.AreSame(scroll, Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Single());
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(.41f).Within(.01f));
            Assert.AreNotEqual(levelBefore, ChildText(parentSlot.transform, "Level").text);
            var equipped = GameObject.Find("Equipped skills").transform;
            Assert.IsTrue(equipped.Cast<Transform>().Any(t => t.name == "Skill 핏빛 파편"));
            Assert.AreSame(PopupSkin.ActionArt, ((Image)skillTab.targetGraphic).sprite,
                "Skill tab selection must survive the child modal");
            Assert.AreSame(PopupSkin.PanelArt, ((Image)petTab.targetGraphic).sprite);
            Assert.AreSame(PopupSkin.PanelArt, ((Image)heroTab.targetGraphic).sprite);
        }

        [UnityTest]
        public IEnumerator RepeatedSummon_MutatesCardsCurrencyAndCollection_WithSameFrameGuard()
        {
            host.Registry.Open("skills-pets-heroes");
            yield return null;
            var currency = GameObject.Find("Currency").GetComponent<Text>();
            var start = CurrencyValue(currency.text);
            GameObject.Find("Summon five").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("Summon five").GetComponent<Button>().onClick.Invoke();
            yield return null;
            var firstNames = ResultNames();
            Assert.AreEqual(5, firstNames.Length);

            var again = GameObject.Find("Again").GetComponent<Button>();
            again.onClick.Invoke();
            again.onClick.Invoke(); // deliberately rapid: only one decision may resolve this frame
            yield return null;
            Assert.IsFalse(firstNames.SequenceEqual(ResultNames()));
            GameObject.Find("Return").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.AreEqual(start - 320, CurrencyValue(currency.text));
            Assert.IsTrue(GameObject.Find("Summon five").GetComponent<Button>().interactable);
            var unlocked = GameObject.Find("Skill 봉인된 권능").GetComponentsInChildren<Text>(true).Single(t => t.name == "Ownership");
            Assert.AreEqual(string.Empty, unlocked.text);
        }

        [UnityTest]
        public IEnumerator SystemBackFromSummon_AllowsNextPaidSummon()
        {
            host.Registry.Open("skills-pets-heroes"); yield return null;
            var currency = GameObject.Find("Currency").GetComponent<Text>();
            var start = CurrencyValue(currency.text);
            var summon = GameObject.Find("Summon five").GetComponent<Button>();
            summon.onClick.Invoke(); yield return null;
            host.CloseTop(); yield return null;
            Assert.IsTrue(summon.IsInteractable());
            summon.onClick.Invoke(); yield return null;
            Assert.AreEqual(start - 320, CurrencyValue(currency.text));
            Assert.AreEqual(1, host.ModalDepth);
        }

        [UnityTest]
        public IEnumerator SkillArtwork_LoadsDistinctAtlasCells_AndReusesFrameInDetails()
        {
            host.Registry.Open("skills-pets-heroes"); yield return null;
            GameObject.Find("Tab 스킬").GetComponent<Button>().onClick.Invoke(); yield return null;
            var first = GameObject.Find("Skill 핏빛 파편").GetComponent<Button>();
            var second = GameObject.Find("Skill 쌍날 투척").GetComponent<Button>();
            var icon = first.transform.Find("Icon").GetComponent<Image>();
            var otherIcon = second.transform.Find("Icon").GetComponent<Image>();
            var frame = first.transform.Find("Slot frame").GetComponent<Image>();
            Assert.IsNotNull(icon.sprite, "The generated atlas must be imported and loadable in runtime.");
            Assert.IsNotNull(frame.sprite, "The reusable ring must import as a sprite.");
            Assert.AreSame(icon.sprite.texture, otherIcon.sprite.texture);
            Assert.AreNotEqual(icon.sprite.rect, otherIcon.sprite.rect);
            Assert.AreNotSame(icon.sprite.texture, frame.sprite.texture);
            Assert.IsFalse(icon.raycastTarget);
            Assert.IsFalse(frame.raycastTarget);
            var cell = icon.sprite.rect;
            Assert.That(cell.width, Is.EqualTo(cell.height));
            Assert.That(cell.y, Is.EqualTo(icon.sprite.texture.height * 2f / 3f));
            first.onClick.Invoke(); yield return null;
            var detail = GameObject.Find("Popup Layer skill-details").GetComponentsInChildren<Button>()
                .Single(b => b.name == first.name);
            Assert.AreSame(icon.sprite, detail.transform.Find("Icon").GetComponent<Image>().sprite);
            Assert.AreSame(frame.sprite, detail.transform.Find("Slot frame").GetComponent<Image>().sprite);
            host.CloseTop(); yield return null;
            Assert.IsTrue(first.IsInteractable());
        }

        [UnityTest]
        public IEnumerator EquipmentPopups_ReuseSlots_KeepMainBinding_AndResumePendingCraft()
        {
            ForgeScreenModule.Register(host.Registry);
            var screen = root.GetComponent<MainScreen>();
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.displayName = "검증 장비"; item.startingLevel = 108;
            item.icon = PopupSkin.CloseArt;
            // Populate an inactive serialized-prefab equivalent before Awake reads its fields.
            var templateObject = new GameObject("slot template", typeof(RectTransform));
            templateObject.transform.SetParent(root.transform, false);
            templateObject.SetActive(false);
            var template = templateObject.AddComponent<EquipmentSlot>();
            template.frame = Ui.Image("Frame", template.transform, 0, 0, 148, 148, PopupSkin.PanelArt);
            template.equipmentFrame = PopupSkin.PanelArt;
            template.icon = Ui.Image("Icon", template.transform, 0, 0, 128, 128, null);
            template.levelLabel = Ui.Text("Level", template.transform, 0, 110, 148, 38, "", 28, assets.font);
            template.starLabel = Ui.Text("Star", template.transform, 0, 138, 148, 28, "★", 24, assets.font);
            template.lockedBadge = Child("lock", template.transform).gameObject;
            template.notificationBadge = Child("notification", template.transform).gameObject;
            template.selection = Child("selection", template.transform).gameObject;
            template.Button.targetGraphic = template.frame;
            template.Bind(item, 123);
            templateObject.SetActive(true);
            assets.equipmentSlotPrefab = template;
            assets.items = new[] { item };
            screen.equipment = new[] { template };
            int oreBefore = screen.ore;
            try
            {
                foreach (int height in new[] { 1920, 2280 })
                {
                    host.SetPreviewMetrics(new Vector2Int(1080, height),
                        new Rect(36, 84, 1008, height - 204));
                    host.Registry.Open("equipment-details", template);
                    yield return null;
                    var dialog = GameObject.Find("Equipment details Dialog");
                    var detail = dialog.GetComponentInChildren<EquipmentSlot>();
                    Assert.AreNotSame(template, detail);
                    Assert.AreSame(item.icon, detail.icon.sprite);
                    Assert.AreSame(template.frame.sprite, detail.frame.sprite);
                    Assert.AreNotSame(detail.frame, detail.icon);
                    Assert.AreEqual("Lv.123", detail.levelLabel.text);
                    Assert.IsFalse(detail.Button.enabled);
                    Assert.IsTrue(detail.GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget));
                    dialog.GetComponentsInChildren<Button>().Single(b => b.name == "Close").onClick.Invoke();
                    yield return null;
                    Assert.AreEqual(0, host.ModalDepth);
                }

                host.Registry.Open("forge-comparison"); yield return null;
                int craftId = screen.PendingCraftId;
                Assert.AreEqual(oreBefore - 100, screen.ore);
                var current = GameObject.Find("Current equipment").GetComponentInChildren<EquipmentSlot>();
                var next = GameObject.Find("New equipment").GetComponentInChildren<EquipmentSlot>();
                Assert.AreEqual(123, current.level);
                Assert.AreEqual(124, next.level);
                Assert.AreSame(item, current.item);
                Assert.AreSame(item, next.item);
                Assert.AreEqual(123, template.level, "The preview must not mutate the equipped slot.");
                GameObject.Find("Equipment comparison Dialog").GetComponentsInChildren<Button>()
                    .Single(b => b.name == "Close").onClick.Invoke();
                yield return null;
                host.Registry.Open("forge-comparison"); yield return null;
                Assert.AreEqual(craftId, screen.PendingCraftId);
                Assert.AreEqual(oreBefore - 100, screen.ore, "Resuming a pending result must not charge twice.");
                GameObject.Find("장착").GetComponent<Button>().onClick.Invoke();
                yield return null;
                Assert.AreEqual(124, template.level);
                Assert.IsNull(screen.PendingCraftItem);
                Assert.AreEqual(0, host.ModalDepth);
            }
            finally { Object.DestroyImmediate(item); }
        }

        static string[] ResultNames()
        {
            return GameObject.Find("Summon result cards").transform.Cast<Transform>()
                .Where(t => t.name.StartsWith("Skill ")).Select(t => t.name).ToArray();
        }

        static int CurrencyValue(string value) { return int.Parse(value.Replace("◆", "").Replace(",", "").Trim()); }
        static Text ChildText(Transform parent, string name) { return parent.GetComponentsInChildren<Text>(true).Single(t => t.name == name); }

        Text Label(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(root.transform, false);
            return go.GetComponent<Text>();
        }

        static RectTransform Child(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); Ui.Stretch(rect); return rect;
        }
    }
}
