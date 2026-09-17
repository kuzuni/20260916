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
            Assert.AreEqual(0,main.gold);Assert.AreEqual(0,host.ModalDepth);
            yield return new WaitForSecondsRealtime(.52f);
            Assert.IsNotNull(GameObject.Find("Gold sale effect"));Assert.AreEqual(expected,main.gold);
            yield return new WaitForSecondsRealtime(.65f);
            Assert.IsFalse(state.autoEnabled);Assert.IsFalse(runtime.Busy);Assert.IsNull(state.Pending);
            Assert.AreEqual(0,host.ModalDepth);Assert.IsNull(GameObject.Find("Gold sale effect"));
        }
    }
}
