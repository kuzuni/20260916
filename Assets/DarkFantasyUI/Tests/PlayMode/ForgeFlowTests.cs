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
            popup.GetComponentsInChildren<Button>().Single(b=>b.name=="장착").onClick.Invoke();yield return null;
            Assert.AreSame(drop,state.equipped[5]);Assert.AreSame(original,state.Pending);
            Assert.AreEqual(drop.Name,popup.GetComponentsInChildren<Text>().First(t=>t.name=="Name").text,"Equipped card moves to the top");
            popup.GetComponentsInChildren<Button>().Single(b=>b.name=="장착").onClick.Invoke();yield return null;
            Assert.AreSame(original,state.equipped[5]);Assert.AreSame(drop,state.Pending);
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

    }
}
