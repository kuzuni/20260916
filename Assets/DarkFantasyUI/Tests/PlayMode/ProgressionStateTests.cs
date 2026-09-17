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
            MoonlitRuntimeSettings.ResetSession();
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
            screen.screens=host.Registry;
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
        public IEnumerator SkillCatalog_CountsOwnershipAndKeepsEquippedBadgesInSync()
        {

            host.Registry.Open("skills-pets-heroes"); yield return null;
            var scroll=GameObject.Find("Tab content").GetComponentInChildren<ScrollRect>();
            var slots=scroll.content.GetComponentsInChildren<Button>();
            Assert.AreEqual(30,slots.Length);
            Assert.AreEqual("스킬 0/30",GameObject.Find("Collection title").GetComponent<Text>().text);
            Assert.IsTrue(slots.All(b=>ChildText(b.transform,"Ownership").text=="미보유"));
            var entries=CollectionProgression.Data.categories[0].entries;
            for(int i=0;i<3;i++){entries[i].unlocked=true;entries[i].fragments=1;}
            GameObject.Find("Quick equip").GetComponent<Button>().onClick.Invoke();yield return null;
            Assert.AreSame(scroll,GameObject.Find("Tab content").GetComponentInChildren<ScrollRect>());
            Assert.AreEqual(3,CollectionProgression.EquippedSkills.Count);
            Assert.AreEqual("스킬 3/30",GameObject.Find("Collection title").GetComponent<Text>().text);
            for(int i=0;i<slots.Length;i++)Assert.AreEqual(i<3?"장착됨":"",ChildText(slots[i].transform,"Equipped badge").text);
            scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();yield return null;
            var last=slots.Last().GetComponent<RectTransform>();
            Assert.IsTrue(scroll.viewport.rect.Contains(scroll.viewport.InverseTransformPoint(last.TransformPoint(last.rect.center))));
            host.Registry.Open("summon-probability-details");yield return null;
            var catalog=GameObject.Find("Popup Layer summon-probability-details").GetComponentInChildren<ScrollRect>();
            var probabilitySlots=catalog.content.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Skill ")).ToArray();
            Assert.AreEqual(30,probabilitySlots.Length);
            Assert.AreEqual(30,catalog.content.GetComponentsInChildren<Text>().Count(t=>t.name=="Chance"));
            foreach(var slot in probabilitySlots){
                Assert.IsNull(slot.transform.Find("Progress"));Assert.IsNull(slot.transform.Find("Level"));
            }
            float saved=catalog.verticalNormalizedPosition;
            probabilitySlots[0].onClick.Invoke();yield return null;
            Assert.AreEqual(2,host.ModalDepth);
            host.CloseTop();yield return null;
            Assert.AreSame(catalog,GameObject.Find("Popup Layer summon-probability-details").GetComponentInChildren<ScrollRect>());
            Assert.That(catalog.verticalNormalizedPosition,Is.EqualTo(saved).Within(.01f));
        }

        [UnityTest]
        public IEnumerator Collection_AllTabsLoadCurrencyRibbonAndProgressArtwork()
        {

            host.Registry.Open("skills-pets-heroes");yield return null;
            foreach(string tab in new[]{"스킬","펫","탈것"}){
                GameObject.Find("Tab "+tab).GetComponent<Button>().onClick.Invoke();yield return null;
                Assert.AreEqual(tab+" 0/30",GameObject.Find("Collection title").GetComponent<Text>().text);
                foreach(string name in new[]{"Summon currency icon","Summon cost icon","Equipped ribbon"})
                    Assert.IsNotNull(GameObject.Find(name).GetComponent<Image>().sprite);
                var content=GameObject.Find("Tab content");
                Assert.AreEqual(30,content.GetComponentsInChildren<Button>().Length);
                Assert.IsTrue(content.GetComponentsInChildren<Image>().Where(i=>i.name=="Progress frame").All(i=>i.sprite!=null));
                if(tab!="스킬")Assert.AreEqual(30,content.GetComponentsInChildren<Text>().Count(t=>t.name=="Art pending"));
            }
        }

        [UnityTest]
        public IEnumerator IllustratedDungeonAndPass_KeepArtSeparateAndPreserveParentScroll()
        {
            host.Registry.Open("dungeons"); yield return null;
            var scroll=GameObject.Find("Page — dungeons").GetComponentInChildren<ScrollRect>();
            var rows=scroll.content.Cast<Transform>().ToArray();
            Assert.AreEqual(4,rows.Length);
            foreach(var row in rows)
            {
                var rim=row.Find("Card rim").GetComponent<Image>();
                Assert.IsFalse(rim.fillCenter);
                float coveredHeight=(rim.sprite.border.y+rim.sprite.border.w)/(rim.pixelsPerUnit*rim.pixelsPerUnitMultiplier);
                Assert.Less(coveredHeight,((RectTransform)row).rect.height*.15f,"Dungeon art should fill the banner opening");
                Assert.IsNotNull(row.Find("Dungeon key icon").GetComponent<Image>().sprite);
            }
            float position=scroll.verticalNormalizedPosition;
            rows[3].Find("Open").GetComponent<Button>().onClick.Invoke(); yield return null;
            var stage=GameObject.Find("Difficulty").GetComponent<Text>();
            Assert.AreEqual("난이도 1",stage.text);
            GameObject.Find("Previous difficulty").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual("난이도 1",stage.text);
            host.CloseTop(); yield return null;
            Assert.AreSame(scroll,GameObject.Find("Page — dungeons").GetComponentInChildren<ScrollRect>());
            Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(position).Within(.01f));
            host.CloseTop(); yield return null;
            ForgeScreenModule.Register(host.Registry);
            host.Registry.Open("progress-pass"); yield return null;
            Assert.IsNotNull(GameObject.Find("Pass sword header").GetComponent<Image>().sprite);
            var pass=GameObject.Find("Popup Layer progress-pass");
            var premiumTab=GameObject.Find("Premium tab").GetComponent<Image>();
            Assert.IsNotNull(premiumTab.sprite);
            Assert.AreEqual("GoldAction-v1",premiumTab.sprite.name);
            Assert.AreSame(premiumTab.sprite,GameObject.Find("₩13,900").GetComponent<Button>().targetGraphic.GetComponent<Image>().sprite);
            var cards=pass.GetComponentsInChildren<RectTransform>(true)
                .Where(t=>t.name=="Free reward" || t.name=="Premium reward").ToArray();
            Assert.AreEqual(12,cards.Length);
            foreach(var card in cards)
            {
                var rim=card.Find("Card rim").GetComponent<Image>();
                float coveredHeight=(rim.sprite.border.y+rim.sprite.border.w)/(rim.pixelsPerUnit*rim.pixelsPerUnitMultiplier);
                Assert.IsFalse(rim.fillCenter);
                Assert.Greater(card.rect.height-coveredHeight,card.rect.height*.8f,
                    "Compact reward cards must not hide their painting behind thick border slices.");
                Assert.AreEqual("ProfileRuins-v1",card.Find("Card painting crop/Card painting").GetComponent<Image>().sprite.name);
            }
            var fourthPremium=cards.Where(t=>t.name=="Premium reward").ElementAt(3);
            CollectionAssert.AreEqual(new[]{"40k"},fourthPremium.GetComponentsInChildren<Text>().Where(t=>t.name=="Reward amount").Select(t=>t.text));
            var locks=pass.GetComponentsInChildren<Image>(true).Where(i=>i.name=="Premium lock").ToArray();
            Assert.AreEqual(6,locks.Length);
            Assert.IsTrue(locks.All(i=>i.sprite!=null && !i.raycastTarget));
            var chests=pass.GetComponentsInChildren<Image>(true).Where(i=>i.name=="Premium chest").ToArray();
            Assert.AreEqual(6,chests.Length);
            Assert.AreEqual(4,chests.Take(4).Select(i=>i.sprite.rect).Distinct().Count());
            Assert.IsTrue(chests.All(i=>i.sprite!=null && !i.raycastTarget));
            var claim=pass.GetComponentsInChildren<Button>().First(b=>b.name=="받기");
            var claimed=claim.transform.parent.Find("Claimed reward check").GetComponent<Image>();
            Assert.IsFalse(claimed.gameObject.activeSelf);
            Assert.IsNotNull(claimed.sprite);
            Assert.IsFalse(claimed.raycastTarget);
            claim.onClick.Invoke(); yield return null;
            Assert.IsFalse(claim.interactable);
            Assert.IsFalse(claim.gameObject.activeSelf);
            Assert.IsTrue(claimed.gameObject.activeSelf);
            claim.onClick.Invoke();
            Assert.AreEqual(1,pass.GetComponentsInChildren<Image>().Count(i=>i.name=="Claimed reward check"));
            host.CloseTop(); yield return null;
            host.Registry.Open("progress-pass"); yield return null;
            pass=GameObject.Find("Popup Layer progress-pass");
            Assert.AreEqual(1,pass.GetComponentsInChildren<Image>().Count(i=>i.name=="Claimed reward check"),
                "Claimed artwork must survive closing and reopening the pass without duplicate claims");
        }

        [UnityTest]
        public IEnumerator Collection_HeaderIsCentered_AndFooterTracksSafeBottom()
        {
            foreach (int height in new[] { 1920,2280 })
            {
                host.SetPreviewMetrics(new Vector2Int(1080,height),new Rect(36,84,1008,height-168));
                host.Registry.Open("skills-pets-heroes"); yield return null;
                var title=GameObject.Find("Collection title").GetComponent<RectTransform>();
                float safeHeight=title.parent.GetComponent<RectTransform>().rect.height;
                Assert.That(title.anchoredPosition.x+title.rect.width/2,Is.EqualTo(540).Within(.1f));
                var wallet=GameObject.Find("Summon wallet").GetComponent<RectTransform>();
                Assert.Less(wallet.anchoredPosition.x+wallet.rect.width,title.anchoredPosition.x);
                Assert.Less(-wallet.anchoredPosition.y,120);
                Assert.IsNotNull(GameObject.Find("Summon currency icon").GetComponent<Image>().sprite);
                var equipped=GameObject.Find("Equipped panel").GetComponent<RectTransform>();
                Assert.That(-equipped.anchoredPosition.y,Is.EqualTo(safeHeight-840).Within(.1f));
                var label=GameObject.Find("Equipped label").GetComponent<RectTransform>();
                var slots=GameObject.Find("Equipped skills").GetComponent<RectTransform>();
                Assert.AreSame(equipped,label.parent);
                var ribbon=GameObject.Find("Equipped ribbon").GetComponent<Image>();
                Assert.IsNotNull(ribbon.sprite);
                Assert.AreSame(PopupSkin.ParchmentRibbonArt,ribbon.sprite);
                Assert.AreNotSame(PopupSkin.RibbonArt.texture,ribbon.sprite.texture);
                Assert.IsFalse(ribbon.raycastTarget);
                Assert.Less(label.anchoredPosition.x+label.rect.width,slots.anchoredPosition.x);
                var back=GameObject.Find("Return to main").GetComponent<RectTransform>();
                Assert.Greater(-back.anchoredPosition.y,-equipped.anchoredPosition.y+equipped.rect.height);
                var tabs=GameObject.Find("Tab 스킬").GetComponent<RectTransform>();
                Assert.LessOrEqual(-tabs.anchoredPosition.y+tabs.rect.height,safeHeight-210);
                GameObject.Find("Tab 펫").GetComponent<Button>().onClick.Invoke(); yield return null;
                Assert.AreEqual("펫 0/30",title.GetComponent<Text>().text);
                Assert.IsNotNull(GameObject.Find("Tab content").GetComponentInChildren<ScrollRect>());
                GameObject.Find("Tab 스킬").GetComponent<Button>().onClick.Invoke();
                back.GetComponent<Button>().onClick.Invoke(); yield return null;
                Assert.IsNull(host.ActivePageKey);
            }
        }

        [UnityTest]
        public IEnumerator UpgradeAll_RespectsOwnershipAndLevelCap_PreservesScrollAndEquippedStars()
        {

            var entries=CollectionProgression.Data.categories[0].entries;
            entries[0].unlocked=true;entries[0].fragments=2;
            entries[1].unlocked=true;entries[1].fragments=1;
            entries[2].unlocked=true;entries[2].level=100;entries[2].fragments=20;
            host.Registry.Open("skills-pets-heroes");yield return null;
            var scroll=GameObject.Find("Tab content").GetComponentInChildren<ScrollRect>();
            Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=.42f;
            GameObject.Find("Upgrade all").GetComponent<Button>().onClick.Invoke();yield return null;
            Assert.AreSame(scroll,GameObject.Find("Tab content").GetComponentInChildren<ScrollRect>());
            Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(.42f).Within(.01f));
            Assert.AreEqual(2,entries[0].level);Assert.AreEqual(0,entries[0].fragments);Assert.IsTrue(entries[0].unlocked);
            Assert.AreEqual(1,entries[1].level);Assert.AreEqual(1,entries[1].fragments);
            Assert.AreEqual(100,entries[2].level);Assert.AreEqual(20,entries[2].fragments);
            var capped=scroll.content.GetComponentsInChildren<Button>()[2];
            capped.onClick.Invoke();yield return null;
            Assert.IsFalse(GameObject.Find("Upgrade").GetComponent<Button>().interactable);
            GameObject.Find("Upgrade").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(100,entries[2].level);
            host.CloseTop();yield return null;Assert.AreEqual("Lv.100",ChildText(capped.transform,"Level").text);
        }

        [UnityTest]
        public IEnumerator SkillUpgradeAndEquip_RefreshParentWithoutLosingTabOrScroll()
        {

            var entry=CollectionProgression.Data.categories[0].entries[0];entry.unlocked=true;entry.fragments=2;
            host.Registry.Open("skills-pets-heroes");yield return null;
            var skillTab=GameObject.Find("Tab 스킬").GetComponent<Button>();
            var petTab=GameObject.Find("Tab 펫").GetComponent<Button>();
            var mountTab=GameObject.Find("Tab 탈것").GetComponent<Button>();
            petTab.onClick.Invoke();yield return null;
            Assert.AreSame(PopupSkin.ActionArt,((Image)petTab.targetGraphic).sprite);
            Assert.AreSame(PopupSkin.PanelArt,((Image)skillTab.targetGraphic).sprite);
            skillTab.onClick.Invoke();yield return null;
            var scroll=GameObject.Find("Tab content").GetComponentInChildren<ScrollRect>();
            Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=.41f;
            var slot=scroll.content.GetComponentsInChildren<Button>()[0];slot.onClick.Invoke();yield return null;
            GameObject.Find("Upgrade").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("Equip slot 2").GetComponent<Button>().onClick.Invoke();
            host.CloseTop();yield return null;
            Assert.AreSame(scroll,GameObject.Find("Tab content").GetComponentInChildren<ScrollRect>());
            Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(.41f).Within(.01f));
            Assert.AreEqual("Lv.2",ChildText(slot.transform,"Level").text);
            Assert.AreEqual(0,entry.fragments);Assert.IsTrue(CollectionProgression.IsEquipped(entry));
            Assert.AreEqual(0,CollectionProgression.Data.categories[0].equipped[1]);
            Assert.AreSame(PopupSkin.ActionArt,((Image)skillTab.targetGraphic).sprite);
            Assert.AreSame(PopupSkin.PanelArt,((Image)mountTab.targetGraphic).sprite);
        }

        [UnityTest]
        public IEnumerator RepeatedSummon_MutatesCardsCurrencyAndCollection_WithSameFrameGuard()
        {

            var main=root.GetComponent<MainScreen>();main.skillTickets=7;main.gems=300;
            host.Registry.Open("skills-pets-heroes");yield return null;
            var summon=GameObject.Find("Summon five").GetComponent<Button>();
            summon.onClick.Invoke();summon.onClick.Invoke();yield return null;
            Assert.AreEqual(5,ResultNames().Length);Assert.AreEqual(2,main.skillTickets);Assert.AreEqual(300,main.gems);
            Assert.AreEqual(5,CollectionProgression.Data.categories[0].entries.Sum(e=>e.fragments));
            var cards=GameObject.Find("Summon result cards").GetComponentsInChildren<Button>();
            foreach(var card in cards){Assert.IsNull(card.transform.Find("Level"));Assert.IsNull(card.transform.Find("Progress"));}
            Assert.AreEqual(1,host.ModalDepth);
            GameObject.Find("Continue").GetComponent<Button>().onClick.Invoke();yield return null;
            summon.onClick.Invoke();summon.onClick.Invoke();yield return null;
            Assert.AreEqual(0,main.skillTickets);Assert.AreEqual(0,main.gems);
            Assert.AreEqual(10,CollectionProgression.Data.categories[0].entries.Sum(e=>e.fragments));
            Assert.AreEqual(2,CollectionProgression.Data.categories[0].summonLevel);
            host.CloseTop();yield return null;
            summon.onClick.Invoke();yield return null;
            Assert.AreEqual(0,host.ModalDepth,"Unaffordable summons must not open a result or spend anything.");
            Assert.AreEqual(10,CollectionProgression.Data.categories[0].entries.Sum(e=>e.fragments));
        }

        [UnityTest]
        public IEnumerator SystemBackFromSummon_AllowsNextPaidSummon()
        {

            var main=root.GetComponent<MainScreen>();main.skillTickets=10;
            host.Registry.Open("skills-pets-heroes");yield return null;
            var summon=GameObject.Find("Summon five").GetComponent<Button>();
            summon.onClick.Invoke();yield return null;host.CloseTop();yield return null;
            Assert.IsTrue(summon.IsInteractable());summon.onClick.Invoke();yield return null;
            Assert.AreEqual(0,main.skillTickets);Assert.AreEqual(1,host.ModalDepth);
            Assert.AreEqual(10,CollectionProgression.Data.categories[0].entries.Sum(e=>e.fragments));
        }

        [UnityTest]
        public IEnumerator SkillArtwork_LoadsDistinctAtlasCells_AndReusesFrameInDetails()
        {

            host.Registry.Open("skills-pets-heroes");yield return null;
            var slots=GameObject.Find("Tab content").GetComponentInChildren<ScrollRect>().content.GetComponentsInChildren<Button>();
            var icon=slots[0].transform.Find("Icon").GetComponent<Image>();
            var frame=slots[0].transform.Find("Slot frame").GetComponent<Image>();
            Assert.IsNotNull(icon.sprite);Assert.IsNotNull(frame.sprite);
            Assert.AreNotSame(icon.sprite.texture,frame.sprite.texture);
            Assert.IsFalse(icon.raycastTarget);Assert.IsFalse(frame.raycastTarget);
            Assert.AreEqual(3,slots.Count(s=>s.transform.Find("Icon")!=null),"Only three primitive skill illustrations are approved.");
            slots[0].onClick.Invoke();yield return null;
            var detail=GameObject.Find("Popup Layer skill-details").GetComponentsInChildren<Button>().Single(b=>b.name==slots[0].name);
            Assert.AreSame(icon.sprite,detail.transform.Find("Icon").GetComponent<Image>().sprite);
            Assert.AreSame(frame.sprite,detail.transform.Find("Slot frame").GetComponent<Image>().sprite);
            Assert.IsNotNull(GameObject.Find("Preview skill"));
            host.CloseTop();yield return null;Assert.IsTrue(slots[0].IsInteractable());
        }

        [UnityTest]
        public IEnumerator EquipmentPopups_ReuseSlots_KeepMainBinding_AndResumePendingCraft()
        {
            ForgeScreenModule.Register(host.Registry);
            var screen = root.GetComponent<MainScreen>();
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.displayName = "검증 장비"; item.startingLevel = 123;
            item.baseHealth = 1000000000;
            item.firstBonusName = "블록 확률"; item.firstBonusPercent = 1.62f;
            item.secondBonusName = "공격 속도"; item.secondBonusPercent = 30.1f;
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
                    var header = dialog.transform.Find("Equipped header").GetComponent<Image>();
                    Assert.IsNotNull(PopupSkin.RibbonArt);
                    Assert.AreSame(PopupSkin.RibbonArt, header.sprite);
                    Assert.IsTrue(header.preserveAspect);
                    Assert.IsFalse(header.raycastTarget);
                    Assert.AreEqual("1b 체력\n+1.62% 블록 확률\n+30.1% 공격 속도",
                        dialog.transform.Find("Item details").GetComponent<Text>().text);
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
                var sell = GameObject.Find("판매").GetComponent<Button>();
                var equip = GameObject.Find("장착").GetComponent<Button>();
                Assert.IsNotNull(PopupSkin.CrimsonActionArt);
                Assert.AreSame(PopupSkin.CrimsonActionArt, ((Image)sell.targetGraphic).sprite);
                Assert.AreSame(PopupSkin.ActionArt, ((Image)equip.targetGraphic).sprite);
                Assert.AreNotSame(((Image)sell.targetGraphic).sprite.texture, ((Image)equip.targetGraphic).sprite.texture);
                Assert.IsFalse(sell.targetGraphic.raycastTarget);
                int craftId = screen.PendingCraftId;
                Assert.AreEqual(oreBefore - 100, screen.ore);
                var current = GameObject.Find("Current equipment").GetComponentInChildren<EquipmentSlot>();
                var next = GameObject.Find("New equipment").GetComponentInChildren<EquipmentSlot>();
                Assert.AreEqual(123, current.level);
                Assert.AreEqual(124, next.level);
                StringAssert.Contains("1b 체력 <color=#FF4933>▼</color>",
                    current.transform.parent.Find("Stats").GetComponent<Text>().text);
                StringAssert.Contains("1.02b 체력 <color=#33FF55>▲</color>",
                    next.transform.parent.Find("Stats").GetComponent<Text>().text);
                Assert.AreEqual(1000000000d, item.baseHealth);
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
                host.Registry.Open("equipment-details", template); yield return null;
                StringAssert.StartsWith("1.02b 체력",
                    GameObject.Find("Item details").GetComponent<Text>().text);
                host.CloseTop(); yield return null;
            }
            finally { Object.DestroyImmediate(item); }
        }

        [UnityTest]
        public IEnumerator OfflineRewards_IllustrationsAndDisplayedTotals_MatchSingleClaim()
        {
            ForgeScreenModule.Register(host.Registry);
            assets.interfaceIcons = new[] { PopupSkin.CloseArt };
            var screen = root.GetComponent<MainScreen>();
            int goldBefore = screen.gold, oreBefore = screen.ore;
            host.SetPreviewMetrics(new Vector2Int(1080, 1920), new Rect(0, 60, 1080, 1740));
            host.Registry.Open("offline-rewards"); yield return null;
            var layer = GameObject.Find("Popup Layer offline-rewards");
            var illustrations = layer.GetComponentsInChildren<Image>()
                .Where(image => image.name == "Reward illustration").ToArray();
            Assert.AreEqual(2, illustrations.Length);
            Assert.IsTrue(illustrations.All(image => image.sprite && image.preserveAspect && !image.raycastTarget));
            Assert.AreEqual("RewardHammer-v1", illustrations[1].sprite.name);
            Assert.AreNotSame(illustrations[1].sprite.texture,
                illustrations[1].transform.parent.GetComponent<Image>().sprite.texture);
            int goldShown = int.Parse(GameObject.Find("Gold total").GetComponent<Text>().text);
            int oreShown = int.Parse(GameObject.Find("Forge total").GetComponent<Text>().text);
            var claim = GameObject.Find("수집").GetComponent<Button>();
            claim.onClick.Invoke(); claim.onClick.Invoke();
            yield return null;
            Assert.AreEqual(goldBefore + goldShown, screen.gold);
            Assert.AreEqual(oreBefore + oreShown, screen.ore);
            Assert.IsFalse(claim.interactable);
            host.CloseTop(); yield return null;
            host.SetPreviewMetrics(new Vector2Int(1080, 2280), new Rect(36, 84, 1008, 2076));
            host.Registry.Open("offline-rewards"); yield return null;
            var claimed = GameObject.Find("수집 완료").GetComponent<Button>();
            Assert.IsFalse(claimed.interactable);
            Assert.AreEqual(goldBefore + goldShown, screen.gold);
            Assert.AreEqual(oreBefore + oreShown, screen.ore);
            host.CloseTop(); yield return null;
            Assert.AreEqual(0, host.ModalDepth);
        }

        [UnityTest]
        public IEnumerator AutoForge_FilterSwitchPreservesChoices_AndQuantityConfiguresStart()
        {
            ForgeScreenModule.Register(host.Registry);
            var screen = root.GetComponent<MainScreen>();
            host.Registry.Open("auto-forge"); yield return null;
            var layer = GameObject.Find("Popup Layer auto-forge");
            var rows = layer.GetComponentsInChildren<Image>().Where(image => image.name.StartsWith("Keep ")).ToArray();
            Assert.AreEqual(4, rows.Length);
            Assert.IsTrue(rows.All(row => row.sprite != null && row.type == Image.Type.Sliced));
            foreach (var row in rows)
            {
                Assert.IsNotNull(row.transform.Find("Tier icon").GetComponent<Image>().sprite);
                Assert.IsNotNull(row.transform.Find("Checkbox").GetComponent<Image>().sprite);
                row.GetComponentInChildren<Toggle>().isOn = false;
            }
            rows[0].GetComponentInChildren<Toggle>().isOn = true;
            var master = GameObject.Find("Stat filter toggle").GetComponent<Toggle>();
            master.isOn = true;
            var filters = layer.GetComponentsInChildren<Image>().Where(image => image.name.StartsWith("Filter "))
                .Select(row => row.GetComponentInChildren<Toggle>()).ToArray();
            Assert.AreEqual(6, filters.Length);
            foreach (var filter in filters) filter.isOn = false;
            filters[2].isOn = true;
            master.isOn = false;
            Assert.IsTrue(filters[2].isOn);
            Assert.IsTrue(filters.All(filter => !filter.interactable));
            host.CloseTop(); yield return null;
            host.SetPreviewMetrics(new Vector2Int(1080, 2280), new Rect(36,84,1008,2076));
            host.Registry.Open("auto-forge"); yield return null;
            master = GameObject.Find("Stat filter toggle").GetComponent<Toggle>();
            Assert.IsFalse(master.isOn);
            Assert.IsFalse(GameObject.Find("Filter 블록 확률").GetComponentInChildren<Toggle>().interactable);
            Assert.IsTrue(GameObject.Find("Filter 블록 확률").GetComponentInChildren<Toggle>().isOn);
            var up = GameObject.Find("▲").GetComponent<Button>();
            var down = GameObject.Find("▼").GetComponent<Button>();
            for (int n = 0; n < 105; n++) up.onClick.Invoke();
            Assert.AreEqual("99", GameObject.Find("Hammer count").GetComponent<Text>().text);
            for (int n = 0; n < 105; n++) down.onClick.Invoke();
            Assert.AreEqual("1", GameObject.Find("Hammer count").GetComponent<Text>().text);
            up.onClick.Invoke(); up.onClick.Invoke();
            GameObject.Find("시작").GetComponent<Button>().onClick.Invoke(); yield return null;
            Assert.IsTrue(screen.autoForge);
            Assert.AreEqual(3, screen.autoForgeBatchSize);
            Assert.AreEqual(63, screen.autoForgeFilterMask, "Filter off accepts any stat without clearing saved choices.");
            Assert.IsTrue(screen.autoForgeKeep[0]);
            Assert.AreEqual(0, host.ModalDepth);
            host.Registry.Open("auto-forge"); yield return null;
            GameObject.Find("Stat filter toggle").GetComponent<Toggle>().isOn = true;
            Assert.IsTrue(GameObject.Find("Filter 블록 확률").GetComponentInChildren<Toggle>().interactable);
            GameObject.Find("정지").GetComponent<Button>().onClick.Invoke(); yield return null;
            Assert.IsFalse(screen.autoForge);
        }

        static string[] ResultNames()
        {
            return GameObject.Find("Summon result cards").transform.Cast<Transform>()
                .Where(t => t.name.StartsWith("Skill ")).Select(t => t.name).ToArray();
        }

        static int CurrencyValue(string value)
        {
            return value.EndsWith("k")
                ? Mathf.RoundToInt(float.Parse(value.Substring(0,value.Length-1),System.Globalization.CultureInfo.InvariantCulture)*1000)
                : int.Parse(value);
        }
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
