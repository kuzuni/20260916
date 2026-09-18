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
            Assert.AreEqual(0,slots.Length);
            Assert.IsNotNull(GameObject.Find("Empty collection"));
            Assert.AreEqual("스킬 0/30",GameObject.Find("Collection title").GetComponent<Text>().text);
            var entries=CollectionProgression.Data.categories[0].entries;
            for(int i=0;i<7;i++){entries[i].unlocked=true;entries[i].fragments=1;}
            GameObject.Find("Quick equip").GetComponent<Button>().onClick.Invoke();yield return null;
            Assert.AreSame(scroll,GameObject.Find("Tab content").GetComponentInChildren<ScrollRect>());
            Assert.AreEqual(3,CollectionProgression.EquippedSkills.Count);
            Assert.AreEqual("스킬 7/30",GameObject.Find("Collection title").GetComponent<Text>().text);
            slots=scroll.content.GetComponentsInChildren<Button>();Assert.AreEqual(7,slots.Length);
            Assert.IsNull(GameObject.Find("Empty collection"));
            for(int i=0;i<slots.Length;i++)Assert.AreEqual(CollectionProgression.IsEquipped(entries[i])?"장착됨":"",ChildText(slots[i].transform,"Equipped badge").text);
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
        public IEnumerator Collection_AllTabsShowOnlyOwnedEntriesInThreeColumnsAndKeepSummoningWhenEmpty()
        {
            foreach(int height in new[]{1920,2280}){
                CollectionProgression.Reset();
                host.SetPreviewMetrics(new Vector2Int(1080,height),new Rect(36,84,1008,height-168));
                host.Registry.Open("skills-pets-heroes");yield return null;
                for(int category=0;category<3;category++){
                    var tab=GameObject.Find("Tab "+CollectionProgression.CategoryNames[category]).GetComponent<Button>();
                    tab.onClick.Invoke();yield return null;
                    var scroll=GameObject.Find("Tab content").GetComponentInChildren<ScrollRect>();
                    Assert.AreEqual(0,scroll.content.GetComponentsInChildren<Button>().Length);
                    StringAssert.Contains(CollectionProgression.CategoryNames[category],GameObject.Find("Empty collection").GetComponent<Text>().text);
                    Assert.IsTrue(GameObject.Find("Summon five").GetComponent<Button>().IsInteractable());
                    foreach(string name in new[]{"Summon currency icon","Summon cost icon","Equipped ribbon"})
                        Assert.IsNotNull(GameObject.Find(name).GetComponent<Image>().sprite);
                    var entries=CollectionProgression.Data.categories[category].entries;
                    int[] owned={0,2,4,6,8,10,12};
                    foreach(int index in owned){entries[index].unlocked=true;entries[index].fragments=0;}
                    tab.onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();
                    var cards=scroll.content.GetComponentsInChildren<Button>();
                    Assert.AreEqual(7,cards.Length,"Zero fragments still counts as permanently owned.");
                    Assert.IsNull(GameObject.Find("Empty collection"));
                    for(int i=0;i<cards.Length;i++){
                        Assert.AreEqual("Skill "+entries[owned[i]].Name,cards[i].name);
                        var rect=(RectTransform)cards[i].transform;
                        Assert.That(rect.rect.width,Is.EqualTo(260).Within(.1f));
                        Assert.That(rect.anchoredPosition.x,Is.EqualTo(56+(i%3)*306).Within(.1f));
                        Assert.That(-rect.anchoredPosition.y,Is.EqualTo(16+(i/3)*390).Within(.1f));
                        Assert.IsFalse(cards[i].transform.Find("Ownership lock").gameObject.activeSelf);
                        Assert.IsNotNull(cards[i].GetComponentsInChildren<Image>().Single(image=>image.name=="Progress frame").sprite);
                        if(category>0){
                            var pending=cards[i].transform.Find("Art pending").GetComponent<RectTransform>();
                            var level=cards[i].transform.Find("Level").GetComponent<RectTransform>();
                            Assert.Less(-pending.anchoredPosition.y+pending.rect.height,-level.anchoredPosition.y);
                        }
                    }
                    scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();yield return null;
                    var last=(RectTransform)cards.Last().transform;
                    Assert.IsTrue(scroll.viewport.rect.Contains(scroll.viewport.InverseTransformPoint(last.TransformPoint(last.rect.center))));
                }
                host.Registry.ShowMainPage();yield return null;
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
            RewardsScreenModule.Register(host.Registry);
            RewardState.Current=new RewardState();
            var screen=root.GetComponent<MainScreen>();
            screen.design=root.transform;
            screen.highestClearedStage=4;
            assets.worldBackground=Resources.Load<Sprite>("Moonlit/Social/ProfileRuins-v1");
            int oreBefore=screen.ore, skillBefore=screen.skillTickets, petBefore=screen.petTickets, mountBefore=screen.mountTickets;
            host.Registry.Open("progress-pass"); yield return null;
            var pass=GameObject.Find("Popup Layer progress-pass");
            var passScroll=pass.GetComponentInChildren<ScrollRect>();
            Assert.IsTrue(passScroll.vertical);Assert.IsFalse(passScroll.horizontal);
            var stages=pass.GetComponentsInChildren<Text>().Where(t=>t.name.StartsWith("Stage milestone ")).ToArray();
            Assert.AreEqual(100,stages.Length);
            Assert.AreEqual("스테이지 5",stages[0].text);Assert.AreEqual("스테이지 500",stages[99].text);
            var cards=pass.GetComponentsInChildren<RectTransform>().Where(t=>t.name.StartsWith("Free reward ")||t.name.StartsWith("Premium reward ")).ToArray();
            Assert.AreEqual(200,cards.Length,"Every milestone restores the original free and locked premium columns");
            foreach(var card in cards) {
                Assert.IsFalse(card.Find("Card rim").GetComponent<Image>().fillCenter);
                Assert.IsNotNull(card.Find("Card painting crop/Card painting").GetComponent<Image>().sprite);
            }
            Assert.AreEqual(100,pass.GetComponentsInChildren<Image>(true).Count(i=>i.name=="Claimed reward check"));
            Assert.AreEqual(100,pass.GetComponentsInChildren<Image>().Count(i=>i.name=="Timeline glow"));
            Assert.AreEqual(100,pass.GetComponentsInChildren<Image>().Count(i=>i.name=="Premium lock"));
            for(int i=0;i<3;i++) {
                var free=cards.Single(t=>t.name=="Free reward "+i);
                var amounts=free.GetComponentsInChildren<Text>().Where(t=>t.name=="Reward amount").Select(t=>t.text).ToArray();
                CollectionAssert.AreEqual(new[]{"10","100"},amounts);
                var icons=free.GetComponentsInChildren<Image>().Where(t=>t.name=="Reward icon").ToArray();
                var expected=RewardVisuals.Ticket(i);
                Assert.AreSame(expected.texture,icons[0].sprite.texture);
                Assert.AreEqual(expected.rect,icons[0].sprite.rect);
                Assert.AreSame(RewardsScreenModule.HammerArt,icons[1].sprite);
            }
            var first=GameObject.Find("Claim milestone 0").GetComponent<Button>();
            Assert.IsFalse(first.interactable);first.onClick.Invoke();
            Assert.AreEqual(oreBefore,screen.ore,"Calling a disabled claim cannot bypass the stage gate");
            screen.highestClearedStage=15;
            yield return new WaitForSecondsRealtime(.25f);
            Canvas.ForceUpdateCanvases();passScroll.verticalNormalizedPosition=.43f;
            for(int i=0;i<3;i++){var claim=GameObject.Find("Claim milestone "+i).GetComponent<Button>();Assert.IsTrue(claim.interactable);claim.onClick.Invoke();claim.onClick.Invoke();}
            yield return new WaitForSecondsRealtime(.25f);
            Assert.AreSame(passScroll,pass.GetComponentInChildren<ScrollRect>());
            Assert.That(passScroll.verticalNormalizedPosition,Is.EqualTo(.43f).Within(.01f));
            Assert.AreEqual(oreBefore+300,screen.ore);Assert.AreEqual(skillBefore+10,screen.skillTickets);
            Assert.AreEqual(petBefore+10,screen.petTickets);Assert.AreEqual(mountBefore+10,screen.mountTickets);
            Assert.IsFalse(first.interactable);Assert.IsFalse(first.gameObject.activeSelf);
            Assert.IsTrue(first.transform.parent.Find("Claimed reward check").gameObject.activeSelf);
            host.CloseTop();yield return null;
            host.Registry.Open("progress-pass");yield return null;
            pass=GameObject.Find("Popup Layer progress-pass");
            for(int i=0;i<3;i++) {
                var claim=pass.GetComponentsInChildren<Button>(true).Single(x=>x.name=="Claim milestone "+i);
                Assert.IsFalse(claim.interactable);Assert.IsFalse(claim.gameObject.activeSelf);
                Assert.IsTrue(claim.transform.parent.Find("Claimed reward check").gameObject.activeSelf);
                claim.onClick.Invoke();
            }
            Assert.AreEqual(oreBefore+300,screen.ore,"Reopening and old callbacks cannot claim twice");
            passScroll=pass.GetComponentInChildren<ScrollRect>();passScroll.verticalNormalizedPosition=0;
            Canvas.ForceUpdateCanvases();yield return null;
            var last=GameObject.Find("Claim milestone 99").GetComponent<Button>();
            var lastRect=(RectTransform)last.transform;
            Assert.IsTrue(passScroll.viewport.rect.Contains(passScroll.viewport.InverseTransformPoint(lastRect.TransformPoint(lastRect.rect.center))));
            screen.highestClearedStage=499;last.onClick.Invoke();Assert.IsFalse(RewardState.Current.passClaimed[99]);
            screen.highestClearedStage=500;last.onClick.Invoke();last.onClick.Invoke();
            Assert.IsTrue(RewardState.Current.passClaimed[99]);
            Assert.AreEqual(oreBefore+400,screen.ore);Assert.AreEqual(skillBefore+20,screen.skillTickets);
            host.CloseTop();yield return null;
        }

        [UnityTest]
        public IEnumerator DungeonDailyRefill_UpdatesOpenDialogKeysAndDisabledActions()
        {
            DungeonProgression.Data.refillDay=System.DateTime.UtcNow.AddHours(9).ToString("yyyy-MM-dd");
            DungeonProgression.Data.keys[0]=0;
            DungeonProgression.Data.highestCleared[0]=1;
            host.Registry.Open("dungeons");yield return null;
            var pageScroll=GameObject.Find("Page — dungeons").GetComponentInChildren<ScrollRect>();
            pageScroll.content.GetChild(0).Find("Open").GetComponent<Button>().onClick.Invoke();yield return null;
            var panel=GameObject.Find("Dungeon detail frame");
            var enter=panel.transform.Find("Enter").GetComponent<Button>();
            var sweep=panel.transform.Find("Previous").GetComponent<Button>();
            var keys=panel.transform.Find("Keys").GetComponent<Text>();
            Assert.AreEqual("0/2",keys.text);Assert.IsFalse(enter.interactable);Assert.IsFalse(sweep.interactable);
            // Move only the recorded day back: the next tick represents crossing the KST reset boundary.
            DungeonProgression.Data.refillDay=System.DateTime.UtcNow.AddHours(9).AddDays(-1).ToString("yyyy-MM-dd");
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.AreSame(panel,GameObject.Find("Dungeon detail frame"));
            Assert.AreEqual("2/2",keys.text);Assert.IsTrue(enter.interactable);Assert.IsTrue(sweep.interactable);
            Assert.AreSame(pageScroll,GameObject.Find("Page — dungeons").GetComponentInChildren<ScrollRect>());
            Assert.AreEqual(1,host.ModalDepth);
        }

        [UnityTest]
        public IEnumerator DungeonRejectedBattle_RefundsKeyAndReleasesEntryReservation()
        {
            host.Registry.Open("dungeon-details");yield return null;
            int keys=DungeonProgression.Data.keys[0];
            // This fixture intentionally has no BattleRuntime, so MainScreen rejects the battle request.
            GameObject.Find("Enter").GetComponent<Button>().onClick.Invoke();yield return null;
            Assert.AreEqual(keys,DungeonProgression.Data.keys[0]);
            Assert.AreEqual(0,DungeonProgression.Data.highestCleared[0]);
            Assert.IsFalse(DungeonProgression.CompleteEntry(true,out _,out _),"A rejected battle cannot later grant rewards.");
            Assert.IsTrue(DungeonProgression.BeginEntry(0,1),"Rejection must release the active reservation.");
            DungeonProgression.CancelEntry();
        }

        [UnityTest]
        public IEnumerator DungeonRewardClaim_PaysOnlyOnButtonAndCannotRepeat()
        {
            RewardsScreenModule.Register(host.Registry);
            var main=root.GetComponent<MainScreen>();
            for(int dungeon=0;dungeon<4;dungeon++){
                DungeonProgression.Reset();
                DungeonProgression.Data.refillDay=System.DateTime.UtcNow.AddHours(9).ToString("yyyy-MM-dd");
                DungeonProgression.Data.highestCleared[dungeon]=5;
                int ore=main.ore,skill=main.skillTickets,pet=main.petTickets,mount=main.mountTickets;
                Assert.IsTrue(DungeonProgression.BeginEntry(dungeon,6));
                Assert.IsTrue(DungeonProgression.CompleteEntry(true,out _,out _));
                host.Registry.Open("dungeon-reward");yield return null;
                Assert.AreEqual(2,DungeonProgression.Data.keys[dungeon]);
                Assert.AreEqual(5,DungeonProgression.Data.highestCleared[dungeon]);
                Assert.AreEqual(ore,main.ore);Assert.AreEqual(skill,main.skillTickets);
                Assert.AreEqual(pet,main.petTickets);Assert.AreEqual(mount,main.mountTickets);
                var claim=GameObject.Find("Claim dungeon reward").GetComponent<Button>();
                claim.onClick.Invoke();claim.onClick.Invoke();yield return null;
                int reward=DungeonProgression.Reward(dungeon,6);
                Assert.AreEqual(ore+(dungeon==0?reward:0),main.ore);
                Assert.AreEqual(skill+(dungeon==1?reward:0),main.skillTickets);
                Assert.AreEqual(pet+(dungeon==2?reward:0),main.petTickets);
                Assert.AreEqual(mount+(dungeon==3?reward:0),main.mountTickets);
                Assert.AreEqual(1,DungeonProgression.Data.keys[dungeon]);
                Assert.AreEqual(6,DungeonProgression.Data.highestCleared[dungeon]);
                Assert.IsFalse(DungeonProgression.Data.pendingClaim);
                Assert.IsFalse(DungeonProgression.ClaimEntry(out _,out _));
            }
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
                var content=GameObject.Find("Tab content").GetComponent<RectTransform>();
                Assert.That(-content.anchoredPosition.y+content.rect.height,
                    Is.EqualTo(-equipped.anchoredPosition.y-20).Within(.1f),
                    "The scroll mask should reach immediately above the equipped panel at both aspect ratios.");
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
            for(int i=3;i<9;i++)entries[i].unlocked=true;
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
            for(int i=1;i<9;i++)CollectionProgression.Data.categories[0].entries[i].unlocked=true;
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
            var visible=GameObject.Find("Tab content").GetComponentInChildren<ScrollRect>().content.GetComponentsInChildren<Button>();
            Assert.AreEqual(CollectionProgression.Data.categories[0].entries.Count(entry=>entry.unlocked),visible.Length);
            Assert.Greater(visible.Length,0);Assert.IsNull(GameObject.Find("Empty collection"));
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
        public IEnumerator SkillArtwork_LoadsAllThirtyThemedSprites_AndReusesFrameInDetails()
        {
            foreach(var entry in CollectionProgression.Data.categories[0].entries)entry.unlocked=true;
            host.Registry.Open("skills-pets-heroes");yield return null;
            var slots=GameObject.Find("Tab content").GetComponentInChildren<ScrollRect>().content.GetComponentsInChildren<Button>();
            var icon=slots[0].transform.Find("Icon").GetComponent<Image>();
            var frame=slots[0].transform.Find("Slot frame").GetComponent<Image>();
            Assert.IsNotNull(icon.sprite);Assert.IsNotNull(frame.sprite);
            Assert.AreNotSame(icon.sprite.texture,frame.sprite.texture);
            Assert.IsFalse(icon.raycastTarget);Assert.IsFalse(frame.raycastTarget);
            Assert.AreEqual(30,slots.Count(s=>s.transform.Find("Icon")!=null));
            var icons=slots.Select(s=>s.transform.Find("Icon").GetComponent<Image>()).ToArray();
            Assert.IsTrue(icons.All(i=>i.sprite!=null),"Every owned skill displays its actual artwork.");
            Assert.AreEqual(30,icons.Select(i=>i.sprite).Distinct().Count());
            Assert.IsTrue(slots.All(s=>!s.transform.Find("Ownership lock").gameObject.activeSelf));
            for(int i=0;i<30;i++)Assert.AreSame(Resources.Load<Sprite>(SkillCatalog.IconKey(i/3,i%3)),icons[i].sprite);
            slots[0].onClick.Invoke();yield return null;
            var detail=GameObject.Find("Popup Layer skill-details").GetComponentsInChildren<Button>().Single(b=>b.name==slots[0].name);
            Assert.AreSame(icon.sprite,detail.transform.Find("Icon").GetComponent<Image>().sprite);
            Assert.AreSame(frame.sprite,detail.transform.Find("Slot frame").GetComponent<Image>().sprite);
            Assert.IsNotNull(GameObject.Find("Preview skill"));
            host.CloseTop();yield return null;Assert.IsTrue(slots[0].IsInteractable());
        }

        [UnityTest]
        public IEnumerator SummonCostsShowOnlyCurrenciesActuallySpentAndUseCategoryTicketArt()
        {
            var main=root.GetComponent<MainScreen>();main.skillTickets=main.petTickets=main.mountTickets=0;main.gems=1000;
            host.Registry.Open("skills-pets-heroes");yield return null;
            for(int category=0;category<3;category++){
                var tab=GameObject.Find("Tab "+CollectionProgression.CategoryNames[category]).GetComponent<Button>();
                tab.onClick.Invoke();yield return null;
                var row=GameObject.Find("Summon cost row");
                Assert.AreEqual(1,row.GetComponentsInChildren<Image>().Length);
                Assert.AreEqual("500",row.GetComponentsInChildren<Text>().Single().text);
                Assert.IsNull(row.transform.Find("Cost plus"));
                if(category==0)main.skillTickets=2;else if(category==1)main.petTickets=2;else main.mountTickets=2;
                tab.onClick.Invoke();yield return null;row=GameObject.Find("Summon cost row");
                Assert.AreEqual(2,row.GetComponentsInChildren<Image>().Length);
                Assert.AreEqual("2",ChildText(row.transform,"Summon cost").text);
                Assert.AreEqual("300",ChildText(row.transform,"Summon diamond cost").text);
                Assert.AreEqual("+",ChildText(row.transform,"Cost plus").text);
                Assert.AreSame(GameObject.Find("Summon currency icon").GetComponent<Image>().sprite,
                    row.transform.Find("Summon cost icon").GetComponent<Image>().sprite);
                if(category==0)main.skillTickets=10;else if(category==1)main.petTickets=10;else main.mountTickets=10;
                tab.onClick.Invoke();yield return null;row=GameObject.Find("Summon cost row");
                Assert.AreEqual(1,row.GetComponentsInChildren<Image>().Length);
                Assert.AreEqual("5",row.GetComponentsInChildren<Text>().Single().text);
            }
        }

        [UnityTest]
        public IEnumerator ProbabilityShowsActualCategoryExperienceWhileBrowsingOtherLevels()
        {
            var category=CollectionProgression.Data.categories[1];category.summonLevel=12;category.experience=7;
            host.Registry.Open("skills-pets-heroes");yield return null;
            GameObject.Find("Tab 펫").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("Probability").GetComponent<Button>().onClick.Invoke();yield return null;
            var layer=GameObject.Find("Popup Layer summon-probability");
            Assert.AreEqual("펫 소환 Lv.12",ChildText(layer.transform,"Current summon level").text);
            Assert.AreEqual("7/43",ChildText(layer.transform,"Value").text);
            Assert.IsFalse(layer.GetComponentsInChildren<Text>().Any(t=>t.name=="Hint"));
            layer.GetComponentsInChildren<Button>().Single(b=>b.name=="Next level").onClick.Invoke();
            Assert.AreEqual("7/43",ChildText(layer.transform,"Value").text);
            Assert.IsTrue(ChildText(layer.transform,"Probability title").text.Contains("13"));
        }

        [UnityTest]
        public IEnumerator SummonRevealUsesOrderedTweenAndClosingEarlyCancelsWithoutLosingRewards()
        {
            var main=root.GetComponent<MainScreen>();main.skillTickets=5;
            host.Registry.Open("skills-pets-heroes");yield return null;
            GameObject.Find("Summon five").GetComponent<Button>().onClick.Invoke();
            var reveal=GameObject.Find("Summon result cards").GetComponent<SummonRevealAnimation>();
            var sequence=(DG.Tweening.Sequence)typeof(SummonRevealAnimation).GetField("sequence",
                System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(reveal);
            DG.Tweening.TweenExtensions.Pause(sequence);
            var cards=reveal.GetComponentsInChildren<CanvasGroup>();
            Assert.AreEqual(5,cards.Length);Assert.IsTrue(cards.All(c=>c.alpha==0&&!c.interactable));
            DG.Tweening.TweenExtensions.GotoWithCallbacks(sequence,.1f);
            Assert.Greater(cards[0].alpha,0);Assert.AreEqual(0,cards[1].alpha);Assert.AreEqual(0,cards[4].alpha);
            DG.Tweening.TweenExtensions.GotoWithCallbacks(sequence,2f);
            Assert.IsTrue(cards.All(c=>Mathf.Approximately(c.alpha,1)&&c.interactable));
            Assert.AreEqual(5,CollectionProgression.Data.categories[0].entries.Sum(e=>e.fragments));
            host.CloseTop();yield return null;
            main.skillTickets=5;
            GameObject.Find("Summon five").GetComponent<Button>().onClick.Invoke();
            reveal=GameObject.Find("Summon result cards").GetComponent<SummonRevealAnimation>();
            sequence=(DG.Tweening.Sequence)typeof(SummonRevealAnimation).GetField("sequence",
                System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(reveal);
            host.CloseTop();yield return null;
            Assert.IsFalse(DG.Tweening.TweenExtensions.IsActive(sequence));
            Assert.AreEqual(10,CollectionProgression.Data.categories[0].entries.Sum(e=>e.fragments));
            Assert.AreEqual(0,main.skillTickets);
        }

        [UnityTest]
        public IEnumerator EquipmentPopups_SeparateArtwork_KeepMainBinding_AndResumePendingCraft()
        {
            ForgeScreenModule.Register(host.Registry);
            var screen=root.GetComponent<MainScreen>();
            var state=ForgeState.Current=new ForgeState();
            var equipped=new EquipmentRoll{id=31,tier=0,level=12,part=EquipmentPart.Armor,variant=0};
            var dropped=new EquipmentRoll{id=32,tier=0,level=13,part=EquipmentPart.Armor,variant=1};
            state.equipped[0]=equipped;state.pending.Add(dropped);
            var templateObject=new GameObject("slot template",typeof(RectTransform));
            templateObject.transform.SetParent(root.transform,false);templateObject.SetActive(false);
            var template=templateObject.AddComponent<EquipmentSlot>();
            template.frame=Ui.Image("Frame",template.transform,0,0,148,148,PopupSkin.PanelArt);
            template.equipmentFrame=PopupSkin.PanelArt;
            template.icon=Ui.Image("Icon",template.transform,0,0,128,128,null);
            template.levelLabel=Ui.Text("Level",template.transform,0,110,148,38,"",28,assets.font);
            template.starLabel=Ui.Text("Star",template.transform,0,138,148,28,"★",24,assets.font);
            template.lockedBadge=Child("lock",template.transform).gameObject;
            template.notificationBadge=Child("notification",template.transform).gameObject;
            template.selection=Child("selection",template.transform).gameObject;
            template.Button.targetGraphic=template.frame;
            screen.equipment=new[]{template};
            var runtime=ForgeRuntime.Ensure(screen);runtime.SyncSlots();templateObject.SetActive(true);
            int oreBefore=screen.ore;
            foreach(int height in new[]{1920,2280}) {
                host.SetPreviewMetrics(new Vector2Int(1080,height),new Rect(36,84,1008,height-204));
                host.Registry.Open("equipment-details",template);yield return null;
                var layer=GameObject.Find("Popup Layer equipment-details");
                var icon=layer.GetComponentsInChildren<Image>().Single(i=>i.name=="Equipment icon");
                Assert.AreSame(EquipmentArt.Icon(equipped),icon.sprite);Assert.IsTrue(icon.preserveAspect);Assert.IsFalse(icon.raycastTarget);
                Assert.AreNotSame(template.icon,icon,"The detail owns its separate presentation");
                Assert.AreEqual(equipped.Name,ChildText(layer.transform,"Name").text);
                Assert.AreEqual("Lv.12",ChildText(layer.transform,"Level").text);
                StringAssert.Contains("체력 "+EquipmentRules.Number(equipped.Stats.health),ChildText(layer.transform,"Stats").text);
                Assert.AreSame(equipped,template.roll);Assert.AreEqual(12,template.level);
                host.CloseTop();yield return null;
            }
            host.Registry.Open("forge-comparison");yield return null;
            var comparison=GameObject.Find("Popup Layer forge-comparison");
            Assert.AreEqual(oreBefore,screen.ore,"Displaying an already generated pending drop costs no additional hammer");
            var cardNames=comparison.GetComponentsInChildren<Text>().Where(t=>t.name=="Name").Select(t=>t.text).ToArray();
            CollectionAssert.AreEqual(new[]{equipped.Name,dropped.Name},cardNames);
            Assert.AreSame(equipped,template.roll);Assert.AreEqual(12,template.level);
            host.CloseTop();yield return null;
            host.Registry.Open("forge-comparison");yield return null;
            Assert.AreSame(dropped,state.Pending);Assert.AreEqual(oreBefore,screen.ore);
            GameObject.Find("장착").GetComponent<Button>().onClick.Invoke();yield return null;
            Assert.AreSame(dropped,template.roll);Assert.AreEqual(13,template.level);
            Assert.AreSame(equipped,state.Pending,"The old item stays available until explicitly sold");
            Assert.AreEqual(1,host.ModalDepth);
            comparison=GameObject.Find("Popup Layer forge-comparison");
            int goldBefore=screen.gold;
            comparison.GetComponentsInChildren<Button>().Single(b=>b.name.StartsWith("판매")).onClick.Invoke();yield return null;
            Assert.AreEqual(goldBefore+EquipmentRules.SaleGold(equipped),screen.gold);
            Assert.IsNull(state.Pending);Assert.AreEqual(0,host.ModalDepth);
            host.Registry.Open("equipment-details",template);yield return null;
            var final=GameObject.Find("Popup Layer equipment-details");
            Assert.AreEqual(dropped.Name,ChildText(final.transform,"Name").text);
            Assert.AreEqual("Lv.13",ChildText(final.transform,"Level").text);
            host.CloseTop();yield return null;
        }

        [UnityTest]
        public IEnumerator OfflineRewards_AccrueWhileOpen_AndDisplayedTotalsMatchSingleClaim()
        {
            ForgeScreenModule.Register(host.Registry);RewardsScreenModule.Register(host.Registry);
            assets.interfaceIcons=new[]{PopupSkin.CloseArt};
            var screen=root.GetComponent<MainScreen>();screen.design=root.transform;
            long start=System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()+10000;
            var rewards=RewardState.Current=new RewardState{lastTickUtc=start};
            rewards.Advance(start+120);
            int goldBefore=screen.gold,oreBefore=screen.ore;
            host.SetPreviewMetrics(new Vector2Int(1080,1920),new Rect(0,60,1080,1740));
            host.Registry.Open("offline-rewards");yield return null;
            var layer=GameObject.Find("Popup Layer offline-rewards");
            var illustrations=layer.GetComponentsInChildren<Image>().Where(i=>i.name=="Reward illustration").ToArray();
            Assert.AreEqual(2,illustrations.Length);Assert.IsTrue(illustrations.All(i=>i.sprite && i.preserveAspect && !i.raycastTarget));
            Assert.IsNotNull(layer.GetComponentsInChildren<Image>().Single(i=>i.name=="Gold reward").sprite);
            Assert.IsNotNull(layer.GetComponentsInChildren<Image>().Single(i=>i.name=="Forge reward").sprite);
            Assert.AreEqual(2,layer.GetComponentsInChildren<Image>().Count(i=>i.name=="Reward divider"));
            Assert.AreEqual(2,layer.GetComponentsInChildren<Image>().Count(i=>i.name=="Divider ornament"));
            var rates=layer.GetComponentsInChildren<Text>().Where(t=>t.name=="Rate").ToArray();
            CollectionAssert.AreEqual(new[]{"1/초","1/분"},rates.Select(t=>t.text).ToArray());
            Assert.IsTrue(rates.All(t=>t.color.g>.9f));
            Assert.IsNotNull(GameObject.Find("Gold total icon").GetComponent<Image>().sprite);
            Assert.IsNotNull(GameObject.Find("Forge total icon").GetComponent<Image>().sprite);
            Assert.AreEqual("120",GameObject.Find("Gold total").GetComponent<Text>().text);
            Assert.AreEqual("2",GameObject.Find("Forge total").GetComponent<Text>().text);
            rewards.Advance(start+180);yield return new WaitForSecondsRealtime(.25f);
            Assert.AreSame(layer,GameObject.Find("Popup Layer offline-rewards"));
            Assert.AreEqual("180",GameObject.Find("Gold total").GetComponent<Text>().text);
            Assert.AreEqual("3",GameObject.Find("Forge total").GetComponent<Text>().text);
            var claim=GameObject.Find("Claim").GetComponent<Button>();claim.onClick.Invoke();claim.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.25f);
            Assert.AreEqual(goldBefore+180,screen.gold);Assert.AreEqual(oreBefore+3,screen.ore);Assert.IsFalse(claim.interactable);
            host.CloseTop();yield return null;
            host.SetPreviewMetrics(new Vector2Int(1080,2280),new Rect(36,84,1008,2076));
            host.Registry.Open("offline-rewards");yield return null;
            Assert.AreEqual("0",GameObject.Find("Gold total").GetComponent<Text>().text);
            Assert.AreEqual("0",GameObject.Find("Forge total").GetComponent<Text>().text);
            Assert.IsFalse(GameObject.Find("Claim").GetComponent<Button>().interactable);
            rewards.Advance(start+240);yield return new WaitForSecondsRealtime(.25f);
            Assert.IsTrue(GameObject.Find("Claim").GetComponent<Button>().interactable);
            GameObject.Find("Claim").GetComponent<Button>().onClick.Invoke();yield return null;
            Assert.AreEqual(goldBefore+240,screen.gold);Assert.AreEqual(oreBefore+4,screen.ore);
            host.CloseTop();yield return null;Assert.AreEqual(0,host.ModalDepth);
        }

        [UnityTest]
        public IEnumerator AutoForge_HidesDisabledChoices_AndOnlyShowsAvailableGradesAndQuantity()
        {
            ForgeScreenModule.Register(host.Registry);
            var screen=root.GetComponent<MainScreen>();
            var state=ForgeState.Current=new ForgeState();
            host.Registry.Open("auto-forge");yield return null;
            var layer=GameObject.Find("Popup Layer auto-forge");
            var rows=layer.GetComponentsInChildren<Image>().Where(i=>i.name.StartsWith("Keep grade ")).ToArray();
            Assert.AreEqual(1,rows.Length);Assert.AreEqual("Keep grade 0",rows[0].name);Assert.IsTrue(rows.All(i=>i.sprite && i.type==Image.Type.Sliced));
            Assert.IsFalse(layer.GetComponentsInChildren<Toggle>().Any(t=>t.name.StartsWith("Affix filter ")));
            foreach(var row in rows) {
                Assert.IsNotNull(row.transform.Find("Tier icon").GetComponent<Image>().sprite);
                Assert.IsNotNull(row.transform.Find("Checkbox").GetComponent<Image>().sprite);
                row.GetComponentInChildren<Toggle>().isOn=false;
            }
            rows[0].GetComponentInChildren<Toggle>().isOn=true;
            var master=GameObject.Find("Enable affix filter toggle").GetComponent<Toggle>();master.isOn=true;
            var filters=layer.GetComponentsInChildren<Toggle>().Where(t=>t.name.StartsWith("Affix filter ")).ToArray();
            Assert.AreEqual(9,filters.Length);
            foreach(var filter in filters)filter.isOn=false;
            var dodge=GameObject.Find("Affix filter Dodge").GetComponent<Toggle>();dodge.isOn=true;
            master.isOn=false;
            Assert.IsTrue(dodge.isOn);Assert.IsTrue(filters.All(t=>!t.gameObject.activeInHierarchy));
            Assert.IsFalse(state.filterEnabled);Assert.AreEqual(1<<(int)EquipmentAffixKind.Dodge,state.affixMask);
            host.CloseTop();yield return null;
            host.SetPreviewMetrics(new Vector2Int(1080,2280),new Rect(36,84,1008,2076));
            host.Registry.Open("auto-forge");yield return null;
            master=GameObject.Find("Enable affix filter toggle").GetComponent<Toggle>();
            Assert.IsFalse(master.isOn);
            dodge=GameObject.Find("Popup Layer auto-forge").GetComponentsInChildren<Toggle>(true).Single(t=>t.name=="Affix filter Dodge");Assert.IsTrue(dodge.isOn);Assert.IsFalse(dodge.gameObject.activeInHierarchy);
            var up=GameObject.Find("+").GetComponent<Button>();var down=GameObject.Find("−").GetComponent<Button>();
            for(int i=0;i<105;i++)up.onClick.Invoke();Assert.AreEqual("99",GameObject.Find("Batch size").GetComponent<Text>().text);
            for(int i=0;i<105;i++)down.onClick.Invoke();Assert.AreEqual("1",GameObject.Find("Batch size").GetComponent<Text>().text);
            up.onClick.Invoke();up.onClick.Invoke();
            GameObject.Find("시작").GetComponent<Button>().onClick.Invoke();
            // Open settings synchronously before the next Update starts a batch; animation is separately exercised by ForgeFlowTests.
            host.Registry.Open("auto-forge");yield return null;
            Assert.IsTrue(screen.autoForge);Assert.IsTrue(state.autoEnabled);Assert.AreEqual(3,state.batchSize);
            Assert.IsTrue(state.keepTiers[0]);Assert.IsTrue(state.keepTiers.Skip(1).All(value=>value),"Unavailable grades keep their preference but are not shown or required");
            master=GameObject.Find("Enable affix filter toggle").GetComponent<Toggle>();master.isOn=true;
            Assert.IsTrue(GameObject.Find("Affix filter Dodge").GetComponent<Toggle>().interactable);
            Assert.IsTrue(GameObject.Find("Affix filter Dodge").GetComponent<Toggle>().isOn);
            GameObject.Find("정지").GetComponent<Button>().onClick.Invoke();yield return null;
            Assert.IsFalse(screen.autoForge);Assert.IsFalse(state.autoEnabled);Assert.AreEqual(0,host.ModalDepth);
        }

        static string[] ResultNames()
        {
            return GameObject.Find("Summon result cards").GetComponentsInChildren<Button>()
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
