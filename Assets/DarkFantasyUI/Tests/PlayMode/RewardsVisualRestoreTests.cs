using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Moonlit.UI.Tests
{
    public sealed class RewardsVisualRestoreTests
    {
        GameObject root;
        MainScreen main;
        MainScreenAssets assets;
        UiScreenHost host;
        RewardState previous;
        bool persistence;
        [SetUp] public void Setup()
        {
            previous=RewardState.Current;RewardState.Current=new RewardState();
            persistence=MainScreen.PersistenceEnabled;MainScreen.PersistenceEnabled=false;
            root=new GameObject("Reward visual fixture",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            ((RectTransform)root.transform).sizeDelta=new Vector2(1080,1920);
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            main=root.AddComponent<MainScreen>();main.enabled=false;main.design=root.transform;main.toastRoot=root.transform;
            assets=ScriptableObject.CreateInstance<MainScreenAssets>();assets.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            main.font=assets.font;main.equipment=new EquipmentSlot[0];
            main.oreText=Label("ore");main.goldText=Label("gold");main.gemText=Label("gems");
            main.stageText=Label("stage");main.powerText=Label("power");main.autoText=Label("auto");
            main.forgeButton=Child("anvil").gameObject.AddComponent<Button>();main.goldButton=Child("wallet").gameObject.AddComponent<Button>();
            host=root.AddComponent<UiScreenHost>();
            host.Initialize(assets,main,Child("popups"),Child("pages"),Child("main").gameObject.AddComponent<CanvasGroup>(),Child("navigation").gameObject.AddComponent<CanvasGroup>());
            host.SetPreviewMetrics(new Vector2Int(1080,1920),new Rect(0,0,1080,1920));
            main.screens=host.Registry;RewardsScreenModule.Register(host.Registry);
        }
        RectTransform Child(string name) { var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(root.transform,false);return r; }
        Text Label(string name) => Child(name).gameObject.AddComponent<Text>();
        [TearDown] public void Cleanup()
        {
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(assets);
            RewardState.Current=previous;MainScreen.PersistenceEnabled=persistence;
        }
        [UnityTest] public IEnumerator RestoredTwoColumnTimelineHasHundredClaimableMilestonesAndFinalRowReachable()
        {
            main.highestClearedStage=500;main.ore=0;main.skillTickets=0;
            host.Registry.Open("progress-pass");yield return null;
            var scroll=root.GetComponentInChildren<ScrollRect>();
            var images=scroll.content.GetComponentsInChildren<Image>(true);
            Assert.AreEqual(100,images.Count(x=>x.name=="Claimed reward check"));
            // The user's continuous progress rail replaced the hundred decorative segments.
            var rail=images.Single(x=>x.name=="Timeline track").rectTransform;
            var fill=images.Single(x=>x.name=="Timeline glow").rectTransform;
            var core=images.Single(x=>x.name=="Timeline").rectTransform;
            var labels=scroll.content.GetComponentsInChildren<Text>(true);
            var milestones=labels.Where(x=>x.name.StartsWith("Stage milestone ")).ToArray();
            var claims=scroll.content.GetComponentsInChildren<Button>(true)
                .Where(x=>x.name.StartsWith("Claim milestone ")).ToArray();
            Assert.AreEqual(100,milestones.Length);
            Assert.AreEqual(100,claims.Length);
            Assert.IsTrue(claims.All(x=>x.interactable),"Every milestone is claimable at stage500.");
            for(int i=0;i<100;i++)
            {
                Assert.AreEqual("스테이지 "+((i+1)*5),milestones.Single(x=>x.name=="Stage milestone "+i).text);
                var rewards=claims.Single(x=>x.name=="Claim milestone "+i).transform.parent
                    .GetComponentsInChildren<Text>(true).Where(x=>x.name=="Reward amount").Select(x=>x.text);
                CollectionAssert.AreEquivalent(new[]{"10","100"},rewards.ToArray());
            }
            Assert.AreEqual(0,images.Count(x=>x.name=="Timeline node" || x.name=="Node"),
                "Intermediate diamond ornaments were removed.");
            float markerSpan=Mathf.Abs(milestones.Single(x=>x.name=="Stage milestone 99").rectTransform.anchoredPosition.y
                -milestones.Single(x=>x.name=="Stage milestone 0").rectTransform.anchoredPosition.y);
            Assert.That(rail.rect.height,Is.EqualTo(markerSpan).Within(.01f));
            Assert.That(fill.rect.height,Is.EqualTo(rail.rect.height).Within(.01f),
                "Stage500 fills the complete continuous rail.");
            Assert.That(core.rect.height,Is.EqualTo(fill.rect.height).Within(.01f));
            Assert.AreEqual(100,images.Count(x=>x.name=="Premium lock"));
            Assert.IsNotNull(root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Free tab"));
            Assert.IsNotNull(root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Premium tab"));
            var claim=scroll.content.GetComponentsInChildren<Button>(true).Single(x=>x.name=="Claim milestone 0");
            claim.onClick.Invoke();claim.onClick.Invoke();
            Assert.AreEqual(100,main.ore);Assert.AreEqual(10,main.skillTickets);Assert.IsTrue(RewardState.Current.passClaimed[0]);
            Assert.IsFalse(claim.gameObject.activeSelf);
            Assert.IsTrue(claim.transform.parent.Find("Claimed reward check").gameObject.activeSelf);
            scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();yield return null;
            var last=scroll.content.GetComponentsInChildren<Text>().Single(t=>t.name=="Stage milestone 99").rectTransform;
            Assert.AreEqual("스테이지 500",last.GetComponent<Text>().text);
            Assert.IsTrue(scroll.viewport.rect.Contains(scroll.viewport.InverseTransformPoint(last.TransformPoint(last.rect.center))));
        }
        [UnityTest] public IEnumerator OfflineKeepsOriginalSlotsDividersAndLiveTotalsWhileOpen()
        {
            var state=RewardState.Current;
            state.accruedSeconds=120;state.lastTickUtc=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            host.Registry.Open("offline-rewards");yield return null;
            var images=root.GetComponentsInChildren<Image>(true);
            Assert.AreEqual(2,images.Count(x=>x.name=="Reward divider"));
            Assert.AreEqual(2,images.Count(x=>x.name=="Reward illustration"));
            Assert.IsNotNull(images.Single(x=>x.name=="Gold total icon"));
            Assert.IsNotNull(images.Single(x=>x.name=="Forge total icon"));
            var gold=root.GetComponentsInChildren<Text>().Single(t=>t.name=="Gold total");
            var ore=root.GetComponentsInChildren<Text>().Single(t=>t.name=="Forge total");
            long before=state.GoldAvailable;
            Assert.AreEqual(before.ToString("N0"),gold.text);Assert.AreEqual("2",ore.text);
            state.lastTickUtc-=60;
            root.GetComponentsInChildren<LiveUiRefresh>().Single().RefreshView();
            Assert.GreaterOrEqual(state.GoldAvailable,before+60);
            Assert.AreEqual(state.GoldAvailable.ToString("N0"),gold.text);Assert.AreEqual("3",ore.text);
            var rates=root.GetComponentsInChildren<Text>().Where(t=>t.name=="Rate").ToArray();
            Assert.AreEqual(2,rates.Length);Assert.IsTrue(rates.All(t=>t.color.g>.9f));
        }
    }
}
