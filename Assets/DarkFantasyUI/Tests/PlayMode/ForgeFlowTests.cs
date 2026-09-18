using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Moonlit.UI.Tests
{
    public sealed class ForgeFlowTests
    {
        GameObject root;
        MainScreen main;
        MainScreenAssets assets;
        UiScreenHost host;
        CanvasGroup mainGroup,navGroup;
        [SetUp] public void Setup()
        {
            ForgeState.Current=new ForgeState();
            root=new GameObject("forge flow root",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            ((RectTransform)root.transform).sizeDelta=new Vector2(1080,1920);
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var popup=Child("popups");var pages=Child("pages");
            mainGroup=Child("main").gameObject.AddComponent<CanvasGroup>();
            navGroup=Child("navigation").gameObject.AddComponent<CanvasGroup>();
            main=root.AddComponent<MainScreen>();main.enabled=false;
            assets=ScriptableObject.CreateInstance<MainScreenAssets>();assets.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            main.font=assets.font;main.design=root.transform;main.toastRoot=root.transform;
            main.oreText=Label("ore");main.goldText=Label("gold");main.gemText=Label("diamonds");
            main.powerText=Label("power");main.stageText=Label("stage");main.autoText=Label("auto");
            main.equipment=new EquipmentSlot[0];main.ore=1000;main.gold=0;
            main.forgeButton=Child("anvil").gameObject.AddComponent<Button>();
            main.goldButton=Child("gold wallet").gameObject.AddComponent<Button>();
            Ui.Image("Crown coin",main.goldButton.transform,0,0,60,60,PopupSkin.CloseArt);
            host=root.AddComponent<UiScreenHost>();host.Initialize(assets,main,popup,pages,mainGroup,navGroup);
            host.SetPreviewMetrics(new Vector2Int(1080,1920),new Rect(36,84,1008,1752));
            main.screens=host.Registry;ForgeScreenModule.Register(host.Registry);
            new GameObject("forge events",typeof(EventSystem));
        }
        RectTransform Child(string name){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(root.transform,false);return r;}
        Text Label(string name){return Child(name).gameObject.AddComponent<Text>();}
        [TearDown] public void Teardown()
        {
            Object.DestroyImmediate(root);Object.DestroyImmediate(assets);
            foreach(var e in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))Object.DestroyImmediate(e.gameObject);
            ForgeState.Current=new ForgeState();
        }
        [UnityTest] public IEnumerator ManualForgeOrdersAnvilRevealComparisonAndEmptyEquip()
        {
            var runtime=ForgeRuntime.Ensure(main);runtime.BeginManual();
            Assert.AreEqual(999,main.ore);Assert.IsTrue(runtime.Busy);Assert.AreEqual(0,host.ModalDepth);
            runtime.BeginManual();Assert.AreEqual(999,main.ore,"A second tap during forging does not spend");
            Assert.IsNull(GameObject.Find("Forged equipment hand"));
            yield return new WaitForSecondsRealtime(1.08f);
            Assert.IsNotNull(GameObject.Find("Forged equipment hand"));Assert.AreEqual(0,host.ModalDepth);
            yield return new WaitForSecondsRealtime(.52f);
            Assert.IsFalse(runtime.Busy);Assert.IsNull(GameObject.Find("Forged equipment hand"));
            Assert.AreEqual(1,host.ModalDepth);Assert.IsFalse(mainGroup.blocksRaycasts);Assert.IsFalse(navGroup.blocksRaycasts);
            var popup=GameObject.Find("Popup Layer forge-comparison");
            Assert.IsFalse(popup.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("판매")));
            var item=ForgeState.Current.Pending;
            popup.GetComponentsInChildren<Button>().Single(b=>b.name=="장착").onClick.Invoke();yield return null;
            Assert.AreSame(item,ForgeState.Current.equipped[(int)item.part]);
            Assert.IsNull(ForgeState.Current.Pending);Assert.AreEqual(0,host.ModalDepth);
            Assert.IsTrue(mainGroup.blocksRaycasts);
        }
        [UnityTest] public IEnumerator CompareSwapsCardsTwiceAndSellsOnlyTheUnequippedItem()
        {
            var state=ForgeState.Current;
            var original=new EquipmentRoll{id=10,part=EquipmentPart.Weapon};
            var drop=new EquipmentRoll{id=11,part=EquipmentPart.Weapon,level=2};
            state.equipped[5]=original;state.pending.Add(drop);
            main.screens.Open("forge-comparison");yield return null;
            var popup=GameObject.Find("Popup Layer forge-comparison");
            Assert.AreEqual("New equipment",popup.GetComponentsInChildren<Text>().Single(t=>t.name=="New marker").transform.parent.name);
            popup.GetComponentsInChildren<Button>().Single(b=>b.name=="장착").onClick.Invoke();yield return null;
            Assert.AreEqual("Current equipment",popup.GetComponentsInChildren<Text>().Single(t=>t.name=="New marker").transform.parent.name);
            Assert.AreSame(drop,state.equipped[5]);Assert.AreSame(original,state.Pending);
            Assert.AreEqual(drop.Name,popup.GetComponentsInChildren<Text>().First(t=>t.name=="Name").text,"Equipped card moves to the top");
            popup.GetComponentsInChildren<Button>().Single(b=>b.name=="장착").onClick.Invoke();yield return null;
            Assert.AreSame(original,state.equipped[5]);Assert.AreSame(drop,state.Pending);
            Assert.AreEqual("New equipment",popup.GetComponentsInChildren<Text>().Single(t=>t.name=="New marker").transform.parent.name);
            var sale=popup.GetComponentsInChildren<Button>().Single(b=>b.name.StartsWith("판매"));
            sale.onClick.Invoke();int gold=main.gold;sale.onClick.Invoke();yield return null;
            Assert.AreEqual(EquipmentRules.SaleGold(drop),gold);Assert.AreEqual(gold,main.gold);
            Assert.AreSame(original,state.equipped[5]);Assert.IsNull(state.Pending);Assert.AreEqual(0,host.ModalDepth);
        }
        [UnityTest] public IEnumerator AutoBatchShowsEveryCardThenSaleEffectAndStopsWhenEmpty()
        {
            var state=ForgeState.Current;state.batchSize=22;state.keepTiers=new bool[10];main.ore=22;
            var runtime=ForgeRuntime.Ensure(main);runtime.StartAuto();yield return null;
            Assert.AreEqual(0,main.ore);Assert.AreEqual(22,state.pending.Count);
            int expected=state.pending.Sum(EquipmentRules.SaleGold);
            yield return new WaitForSecondsRealtime(1.08f);
            var hand=GameObject.Find("Forged equipment hand");
            Assert.IsNotNull(hand);Assert.AreEqual(22,hand.GetComponentsInChildren<Image>().Count(i=>i.name.StartsWith("Forged card ")));
            var cards=hand.GetComponentsInChildren<Image>().Where(i=>i.name.StartsWith("Forged card ")).Select(i=>i.rectTransform).ToArray();
            foreach(var card in cards) {
                Assert.That(card.rect.width,Is.EqualTo(card.rect.height).Within(.01f),"Reveal frames must be square, including a 22-item hand.");
                var thumbnail=card.Find("Equipment thumbnail").GetComponent<Image>();
                Assert.IsNotNull(thumbnail.sprite);Assert.IsTrue(thumbnail.preserveAspect);
                var level=(RectTransform)card.Find("Level");
                Assert.GreaterOrEqual(level.anchoredPosition.y-level.rect.height,-card.rect.height-1);
            }
            Assert.That(cards[1].anchoredPosition.x-cards[0].anchoredPosition.x,Is.EqualTo(cards[0].rect.width*.5f).Within(.01f));
            Assert.That(cards[0].anchoredPosition.y,Is.EqualTo(cards[10].anchoredPosition.y).Within(.01f));
            Assert.Less(cards[11].anchoredPosition.y,cards[0].anchoredPosition.y,"22 cards appear as two half-overlapped hands");
            Assert.AreEqual(0,main.gold);Assert.AreEqual(0,host.ModalDepth);
            yield return new WaitForSecondsRealtime(.52f);
            Assert.IsNotNull(GameObject.Find("Gold sale effect"));Assert.AreEqual(expected,main.gold);
            var burst=GameObject.Find("Gold coin burst");Assert.IsNotNull(burst);
            var coins=burst.GetComponentsInChildren<Image>();Assert.AreEqual(8,coins.Length);
            Assert.IsTrue(coins.All(i=>i.sprite==main.goldButton.transform.Find("Crown coin").GetComponent<Image>().sprite && !i.raycastTarget));
            yield return new WaitForSecondsRealtime(.65f);
            Assert.IsFalse(state.autoEnabled);Assert.IsFalse(runtime.Busy);Assert.IsNull(state.Pending);
            Assert.AreEqual(0,host.ModalDepth);Assert.IsNull(GameObject.Find("Gold sale effect"));
        }
        [UnityTest] public IEnumerator StopDuringAutoAnimationDefersComparisonUntilRevealCompletes()
        {
            var state=ForgeState.Current;state.batchSize=22;
            var runtime=ForgeRuntime.Ensure(main);runtime.StartAuto();yield return null;
            Assert.IsTrue(runtime.Busy);Assert.AreEqual(22,state.pending.Count);
            host.Registry.Open("auto-forge");yield return null;
            GameObject.Find("정지").GetComponent<Button>().onClick.Invoke();yield return null;
            Assert.IsFalse(state.autoEnabled);Assert.IsTrue(runtime.Busy);
            Assert.AreEqual(0,host.ModalDepth,"Stopping must not expose an in-flight batch to equip or sale actions");
            Assert.IsNull(GameObject.Find("Popup Layer forge-comparison"));
            yield return new WaitForSecondsRealtime(1.6f);
            Assert.IsFalse(runtime.Busy);Assert.AreEqual(1,host.ModalDepth);
            Assert.IsNotNull(GameObject.Find("Popup Layer forge-comparison"));
            Assert.AreEqual(22,state.pending.Count);Assert.AreEqual(0,main.gold);
            Assert.AreEqual(978,main.ore);
        }

        [UnityTest] public IEnumerator ExitDuringAnvilPersistsAutoSalesAndDoesNotSpendAgainOnRestore()
        {
            var state=ForgeState.Current;state.batchSize=22;state.keepTiers=new bool[10];state.keepTiers[9]=true;
            main.ore=22;main.gold=37;
            var runtime=ForgeRuntime.Ensure(main);runtime.StartAuto();yield return null;
            Assert.IsTrue(runtime.Busy);Assert.AreEqual(0,main.ore);
            Assert.AreEqual(22,state.automaticSaleIds.Count,"Sale decisions must exist before the first animation frame is saved");
            Assert.IsNull(GameObject.Find("Forged equipment hand"));
            int expected=state.pending.Sum(EquipmentRules.SaleGold);
            var save=new GameplaySave {gold=main.gold,hammers=main.ore,successfulForges=main.successfulForges,forge=JsonUtility.ToJson(state)};
            string snapshot=JsonUtility.ToJson(save);
            Object.DestroyImmediate(runtime);
            var restored=JsonUtility.FromJson<GameplaySave>(snapshot);
            ForgeState.Current=JsonUtility.FromJson<ForgeState>(restored.forge);
            ForgeState.Current.NormalizeAfterLoad();
            ForgeState.Current.SettleAutoBatch(ref restored.gold);
            Assert.AreEqual(37+expected,restored.gold);Assert.AreEqual(0,restored.hammers);
            Assert.AreEqual(22,restored.successfulForges);Assert.IsNull(ForgeState.Current.Pending);
            Assert.IsFalse(ForgeState.Current.autoEnabled);Assert.AreEqual(0,host.ModalDepth);
            Assert.AreEqual(0,ForgeState.Current.SettleAutoBatch(ref restored.gold));
            Assert.AreEqual(37+expected,restored.gold);
            yield return null;
        }

        [UnityTest] public IEnumerator AutoResumesAfterLastDecisionAndStopsAtZeroHammers()
        {
            var state=ForgeState.Current;state.batchSize=1;state.continueAfterMatch=false;main.ore=3;
            var runtime=ForgeRuntime.Ensure(main);runtime.StartAuto();
            for(int batch=0;batch<3;batch++){
                double until=Time.realtimeSinceStartupAsDouble+5;
                while(host.ModalDepth==0 && Time.realtimeSinceStartupAsDouble<until)yield return null;
                Assert.AreEqual(1,host.ModalDepth);Assert.AreEqual(2-batch,main.ore);
                Assert.AreEqual(batch<2,state.autoEnabled,"Comparison pauses automatic forging without switching it off");
                var layer=GameObject.Find("Popup Layer forge-comparison");
                var sale=layer.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name=="판매");
                if(sale)sale.onClick.Invoke();else layer.GetComponentsInChildren<Button>().Single(b=>b.name=="장착").onClick.Invoke();
                yield return null;
            }
            Assert.IsFalse(state.autoEnabled);Assert.IsNull(state.Pending);Assert.AreEqual(0,main.ore);
            yield return new WaitForSecondsRealtime(.1f);Assert.IsFalse(runtime.Busy);
        }
        [UnityTest] public IEnumerator EmptyComparisonHasOnlyNewCardAndNoEquippedHeader()
        {
            ForgeState.Current.pending.Add(new EquipmentRoll{id=1,part=EquipmentPart.Hat});
            host.Registry.Open("forge-comparison");yield return null;
            var layer=GameObject.Find("Popup Layer forge-comparison");
            Assert.AreEqual(1,layer.GetComponentsInChildren<Text>().Count(t=>t.name=="Name"));
            Assert.IsFalse(layer.GetComponentsInChildren<Text>().Any(t=>t.text=="장착됨" || t.text=="장착 중"));
            Assert.IsFalse(layer.GetComponentsInChildren<Button>().Any(b=>b.name=="판매"));
            Assert.IsNotNull(layer.GetComponentsInChildren<Image>().Single(i=>i.name=="Equipment frame").sprite);
        }
        [UnityTest] public IEnumerator HammerAnimatorSamplesThreeDistinctStrikesBeforeReveal()
        {
            var motion=ForgeHammerMotion.Play(main.forgeButton.transform);
            Assert.IsNotNull(motion.GetComponent<Animator>());
            var hammer=motion.transform.Find("Hammer");
            float[] strikes={.16f,.49f,.82f};
            foreach(float strike in strikes){
                motion.Sample(strike);float down=hammer.localEulerAngles.z;
                motion.Sample(strike-.12f);float raised=hammer.localEulerAngles.z;
                Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(down,raised)),20,"Authored clip must move the hammer at each strike");
            }
            Object.Destroy(motion.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator GoldSegmentsSpanSameFullWidthAtThreeAndSixSegments()
        {
            foreach(int level in new[]{1,4}){
                ForgeState.Current.level=level;
                host.Registry.Open("forge-probability");yield return null;
                var segments=GameObject.Find("Popup Layer forge-probability").GetComponentsInChildren<Image>().Where(i=>i.name.StartsWith("Gold segment ")).OrderBy(i=>i.name).ToArray();
                Assert.AreEqual(ForgeState.Current.Segments,segments.Length);
                float left=segments[0].rectTransform.anchoredPosition.x;
                float right=segments.Last().rectTransform.anchoredPosition.x+segments.Last().rectTransform.rect.width;
                Assert.That(right-left,Is.EqualTo(668).Within(.01));
                host.CloseTop();yield return null;
            }
        }

        [UnityTest] public IEnumerator AutoComparisonWaitsForCoveringModalThenOpensOnce()
        {
            foreach(int initialHammers in new[]{2,1}){
                var state=ForgeState.Current=new ForgeState {batchSize=1,continueAfterMatch=false};
                main.ore=initialHammers;
                var runtime=ForgeRuntime.Ensure(main);runtime.StartAuto();yield return null;
                Assert.IsTrue(runtime.Busy);
                host.Registry.Open("equipment-details");yield return null;
                double until=Time.realtimeSinceStartupAsDouble+5;
                while(runtime.Busy && Time.realtimeSinceStartupAsDouble<until)yield return null;
                Assert.IsFalse(runtime.Busy);Assert.AreEqual(1,host.ModalDepth);
                Assert.IsNotNull(GameObject.Find("Popup Layer equipment-details"));
                Assert.IsNull(GameObject.Find("Popup Layer forge-comparison"));
                host.CloseTop();yield return null;yield return null;
                Assert.AreEqual(1,host.ModalDepth);
                Assert.IsNotNull(GameObject.Find("Popup Layer forge-comparison"));
                Assert.AreEqual(initialHammers-1,main.ore);
                Assert.AreEqual(initialHammers>1,state.autoEnabled,"Zero hammers stop forging but must not suppress the completed result");
                // Explicit dismissal keeps the reviewed queue without reopening it on every frame.
                host.CloseTop();yield return null;yield return null;
                Assert.AreEqual(0,host.ModalDepth);Assert.AreEqual(initialHammers-1,main.ore);
                runtime.StopAuto();yield return null;Assert.IsFalse(state.autoEnabled);
            }
        }

        [UnityTest] public IEnumerator AscensionClaimCancelsInFlightForgeAndClearsPendingWithoutSale()
        {
            var state=ForgeState.Current;state.level=35;
            state.equipped[0]=new EquipmentRoll {id=77,tier=9,part=EquipmentPart.Armor};state.nextId=77;
            var runtime=ForgeRuntime.Ensure(main);runtime.BeginManual();
            Assert.IsTrue(runtime.Busy);Assert.AreEqual(999,main.ore);
            state.filledSegments=state.Segments;state.upgradeEndsUtcTicks=System.DateTime.UtcNow.AddSeconds(-1).Ticks;
            main.screens.Open("forge-probability");yield return null;
            var layer=GameObject.Find("Popup Layer forge-probability");
            var finish=layer.GetComponentsInChildren<Button>().Single(button=>button.name=="업그레이드");
            Assert.AreEqual("승천하기",finish.GetComponentInChildren<Text>().text);
            finish.onClick.Invoke();yield return null;
            Assert.AreEqual(1,state.ascension);Assert.AreEqual(1,state.level);
            Assert.IsFalse(runtime.Busy);Assert.IsFalse(state.autoEnabled);
            Assert.IsEmpty(state.pending);Assert.IsEmpty(state.automaticSaleIds);
            Assert.IsTrue(state.equipped.All(item=>item==null));
            Assert.IsNull(GameObject.Find("Forging hammer"));Assert.IsNull(GameObject.Find("Forged equipment hand"));
            Assert.IsTrue(main.forgeButton.interactable);
            yield return new WaitForSecondsRealtime(1.7f);
            Assert.IsNull(GameObject.Find("Popup Layer forge-comparison"));
            Assert.AreEqual(0,main.gold);Assert.AreEqual(999,main.ore,"Ascension neither refunds nor spends more hammers.");
            Assert.IsEmpty(state.pending);Assert.AreEqual(1,state.ascension);
            Assert.AreEqual(1,host.ModalDepth);
        }

        [UnityTest] public IEnumerator EquipmentStarsUseSavedAscensionAndCatalogUsesSquareFrames()
        {
            foreach(int ascension in new[]{0,1,3}) {
                ForgeState.Current.ascension=ascension;
                host.Registry.Open("forge-probability-details");yield return null;
                var layer=GameObject.Find("Popup Layer forge-probability-details");
                var cells=layer.GetComponentsInChildren<Button>().Where(button=>button.name.StartsWith("Equipment ")).ToArray();
                Assert.AreEqual(180,cells.Length);
                foreach(var cell in cells) {
                    var rect=(RectTransform)cell.transform;
                    Assert.That(rect.rect.width,Is.EqualTo(rect.rect.height).Within(.01f));
                    Assert.AreEqual(ascension>0,cell.GetComponentsInChildren<Text>().Any(text=>text.name=="Star"));
                }
                cells[0].onClick.Invoke();yield return null;
                var info=GameObject.Find("Popup Layer forge-item-details");
                var stars=info.GetComponentsInChildren<Text>().Where(text=>text.name=="Star").ToArray();
                Assert.AreEqual(ascension>0?1:0,stars.Length);
                if(ascension>0)Assert.AreEqual(EquipmentRules.AscensionStars(ascension),stars[0].text);
                host.CloseTop();host.CloseTop();yield return null;
                // The equipment card must use its own saved ascension, not the current forge's value.
                ForgeState.Current.ascension=ascension+1;
                host.Registry.Open("equipment-details",new EquipmentRoll {id=1,ascension=ascension,part=EquipmentPart.Armor});
                yield return null;
                var details=GameObject.Find("Popup Layer equipment-details");
                Assert.AreEqual(ascension>0?1:0,details.GetComponentsInChildren<Text>().Count(text=>text.name=="Star"));
                host.CloseTop();yield return null;
            }
        }

        [UnityTest] public IEnumerator ProbabilityRestoresIconWalletHeaderAndHidesZeroAscensionStars()
        {
            foreach(int ascension in new[]{0,2}) {
                ForgeState.Current.ascension=ascension;main.gold=1234;main.gems=56;
                host.Registry.Open("forge-probability");yield return null;
                var layer=GameObject.Find("Popup Layer forge-probability");
                var texts=layer.GetComponentsInChildren<Text>();
                Assert.AreEqual("제련 확률",texts.Single(text=>text.name=="Subtitle").text);
                Assert.AreEqual(MainScreen.Compact(1234),texts.Single(text=>text.name=="Wallet value 0").text);
                Assert.AreEqual("56",texts.Single(text=>text.name=="Wallet value 1").text);
                Assert.IsTrue(layer.GetComponentsInChildren<Image>().Any(image=>image.name=="Wallet icon 0"));
                Assert.IsTrue(layer.GetComponentsInChildren<Image>().Any(image=>image.name=="Wallet icon 1"));
                Assert.IsFalse(texts.Any(text=>text.name=="Skip rate" || text.name=="Wallet"));
                Assert.AreEqual(ascension>0?10:0,texts.Count(text=>text.name=="Rarity star"));
                host.CloseTop();yield return null;
            }
        }

        [Test] public void MainEquipmentSlotShowsOnlyItsItemsAscensionStars()
        {
            var go=new GameObject("Ascension slot",typeof(RectTransform),typeof(Button));
            go.transform.SetParent(root.transform,false);go.SetActive(false);
            var slot=go.AddComponent<EquipmentSlot>();
            slot.frame=Ui.Image("Frame",go.transform,0,0,148,148,null);
            slot.icon=Ui.Image("Icon",go.transform,0,0,148,148,null);
            slot.levelLabel=Ui.Text("Level",go.transform,0,100,148,40,"",24,assets.font);
            slot.starLabel=Ui.Text("Star",go.transform,0,138,148,28,"",24,assets.font);
            slot.lockedBadge=Ui.Rect("Locked",go.transform,0,0,20,20).gameObject;
            slot.notificationBadge=Ui.Rect("Notification",go.transform,0,0,20,20).gameObject;
            var definition=ScriptableObject.CreateInstance<ItemDefinition>();
            try {
                ForgeState.Current.ascension=3;
                foreach(int ascension in new[]{0,1,3}) {
                    slot.roll=new EquipmentRoll {id=1,ascension=ascension};
                    slot.Bind(definition,1);
                    Assert.AreEqual(ascension>0,slot.starLabel.gameObject.activeSelf);
                    Assert.AreEqual(ascension>0?EquipmentRules.AscensionStars(ascension):"",slot.starLabel.text);
                }
                slot.Bind(null);
                Assert.IsFalse(slot.starLabel.gameObject.activeSelf);Assert.AreEqual("",slot.starLabel.text);
            } finally {Object.DestroyImmediate(definition);}
        }

        [UnityTest] public IEnumerator MaxAscensionShowsDisabledMaxLevelButton()
        {
            ForgeState.Current.ascension=3;ForgeState.Current.level=35;main.gold=1000000;
            host.Registry.Open("forge-probability");yield return null;
            var layer=GameObject.Find("Popup Layer forge-probability");
            var primary=layer.GetComponentsInChildren<Button>().Single(button=>button.name=="업그레이드");
            Assert.AreEqual("만렙",primary.GetComponentInChildren<Text>().text);
            Assert.IsFalse(primary.interactable);
            primary.onClick.Invoke();yield return null;
            Assert.AreEqual(1000000,main.gold);Assert.AreEqual(3,ForgeState.Current.ascension);
            Assert.AreEqual(35,ForgeState.Current.level);
        }

    }
}
