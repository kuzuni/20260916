using DG.Tweening;
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Moonlit.UI.Tests
{
    public class RewardAvailabilityTests
    {
        [SetUp] public void Reset(){MoonlitRuntimeSettings.ResetSession();}
        [Test] public void ArenaHasFiveAttemptsAndRefillsAtKoreanMidnightAfterSave()
        {
            var day=new DateTime(2026,9,18,14,59,0,DateTimeKind.Utc);
            var s=new RewardState();
            for(int i=5;i>0;i--){Assert.AreEqual(i,s.ArenaRemaining(day));Assert.IsTrue(s.TryUseArenaAttempt(day));}
            Assert.AreEqual(0,s.ArenaRemaining(day));Assert.IsFalse(s.TryUseArenaAttempt(day));
            s=JsonUtility.FromJson<RewardState>(JsonUtility.ToJson(s));
            Assert.AreEqual(0,s.ArenaRemaining(day.AddDays(-1)),"Clock rollback cannot refill attempts.");
            Assert.AreEqual(5,s.ArenaRemaining(day.AddMinutes(1)));
            Assert.IsTrue(s.TryUseArenaAttempt(day.AddMinutes(1)));
            Assert.AreEqual(4,s.ArenaRemaining(day.AddMinutes(2)));
        }
        [Test] public void OfflineRequiresFullMinuteAfterEachClaimIncludingSaveAndLegacyState()
        {
            var root=new GameObject("Claim availability");root.SetActive(false);
            try {
                var main=root.AddComponent<MainScreen>();
                long start=DateTimeOffset.UtcNow.ToUnixTimeSeconds()+10000;
                var s=new RewardState{lastTickUtc=start};
                s.Advance(start+59);Assert.IsFalse(s.CanClaim);Assert.IsFalse(s.Claim(main));
                s.Advance(start+60);Assert.IsTrue(s.CanClaim);Assert.IsTrue(s.Claim(main));
                Assert.AreEqual(60,main.gold);Assert.AreEqual(1001,main.ore);Assert.AreEqual(0,s.CollectionSeconds);
                s=JsonUtility.FromJson<RewardState>(JsonUtility.ToJson(s));
                s.Advance(start+119);Assert.IsFalse(s.CanClaim);s.Advance(start+120);Assert.IsTrue(s.CanClaim);
                Assert.IsTrue(s.Claim(main));Assert.AreEqual(120,main.gold);Assert.AreEqual(1002,main.ore);
                var legacy=new RewardState{accruedSeconds=174,goldClaimed=174,hammersClaimed=2,lastTickUtc=start};
                legacy.Advance(start+6);Assert.IsFalse(legacy.CanClaim);
                legacy.Advance(start+60);Assert.IsTrue(legacy.CanClaim);
            } finally {UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test] public void NotificationPulseRepeatsAndStopsWhenHidden()
        {
            var root=new GameObject("Pulse fixture",typeof(RectTransform));
            try {
                var dot=RewardNotificationDots.Create(root.transform,0,0,27,null);
                dot.SetActive(true);var pulse=dot.GetComponent<NotificationPulse>();
                var tween=DG.Tweening.DOTween.TweensByTarget(pulse,false)[0];
                tween.Goto(.55f,false);Assert.Greater(dot.transform.localScale.x,1);
                dot.SetActive(false);Assert.IsFalse(tween.IsActive());Assert.AreEqual(Vector3.one,dot.transform.localScale);
                dot.SetActive(true);Assert.AreEqual(1,DG.Tweening.DOTween.TweensByTarget(pulse,false).Count);
            } finally {UnityEngine.Object.DestroyImmediate(root);}
        }
        [UnityTest] public IEnumerator RedDotsTrackConsumableAndClaimAvailability()
        {
#if UNITY_EDITOR
            bool persistence=MainScreen.PersistenceEnabled;
            var root=new GameObject("Reward dots acceptance");root.SetActive(false);
            try {
                MainScreen.PersistenceEnabled=false;
                var assets=AssetDatabase.LoadAssetAtPath<MainScreenAssets>("Assets/DarkFantasyUI/Data/MainScreenAssets.asset");
                var main=new RuntimeMainScreenFactory(assets).Create(root.transform);main.enabled=false;
                main.GetComponent<BattleRuntime>().enabled=false;root.SetActive(true);yield return null;
                var dots=main.GetComponent<RewardNotificationDots>();
                var state=RewardState.Current;var now=DateTime.UtcNow;
                DungeonProgression.RefreshDay(now);
                for(int i=0;i<4;i++)DungeonProgression.Data.keys[i]=0;
                for(int i=0;i<5;i++)state.TryUseArenaAttempt(now);
                state.dailyDiamondClaimDay=now.ToUniversalTime().AddHours(9).ToString("yyyy-MM-dd");
                main.skillTickets=main.petTickets=main.mountTickets=0;
                state.accruedSeconds=59;state.lastTickUtc=DateTimeOffset.UtcNow.ToUnixTimeSeconds()+1000;
                main.highestClearedStage=4;dots.RefreshNow();
                foreach(var button in main.navigation)Assert.IsFalse(button.transform.Find("Notification").gameObject.activeSelf);
                Assert.IsFalse(main.eventButton.transform.Find("Notification").gameObject.activeSelf);
                Assert.IsFalse(main.fairyButton.transform.Find("Notification").gameObject.activeSelf);
                state.accruedSeconds=60;main.highestClearedStage=5;DungeonProgression.Data.keys[2]=1;main.mountTickets=1;
                state.arenaDay="";state.dailyDiamondClaimDay="";dots.RefreshNow();
                foreach(var button in main.navigation)Assert.IsTrue(button.transform.Find("Notification").gameObject.activeSelf);
                Assert.IsTrue(main.eventButton.transform.Find("Notification").gameObject.activeSelf);
                Assert.IsTrue(main.fairyButton.transform.Find("Notification").gameObject.activeSelf);
                main.screens.Open("progress-pass");
                var claim=root.GetComponentsInChildren<UnityEngine.UI.Button>(true);
                var first=Array.Find(claim,b=>b.name=="Claim milestone 0");
                var next=Array.Find(claim,b=>b.name=="Claim milestone 1");
                Assert.IsTrue(first.transform.Find("Notification").gameObject.activeSelf);
                Assert.IsFalse(next.transform.Find("Notification").gameObject.activeSelf);
                first.onClick.Invoke();dots.RefreshNow();
                Assert.IsFalse(main.fairyButton.transform.Find("Notification").gameObject.activeSelf);
                Assert.AreEqual(10,main.skillTickets);
                var forge=ForgeState.Current;var forgeDot=main.forgeLevelButton.transform.Find("Notification").gameObject;
                main.gold=forge.SegmentCost-1;dots.RefreshNow();Assert.IsFalse(forgeDot.activeSelf);
                main.gold=forge.SegmentCost;dots.RefreshNow();Assert.IsTrue(forgeDot.activeSelf);
                forge.filledSegments=forge.Segments;main.gold=0;dots.RefreshNow();Assert.IsTrue(forgeDot.activeSelf);
                forge.StartUpgrade(now);dots.RefreshNow();Assert.IsFalse(forgeDot.activeSelf);
                forge.upgradeEndsUtcTicks=now.AddSeconds(-1).Ticks;dots.RefreshNow();Assert.IsTrue(forgeDot.activeSelf);
                main.screens.CloseTop();main.screens.Open("wallet");
                foreach(var kind in new[]{"Diamond","Gold","SkillTicket","PetTicket","MountTicket","Hammer"}) {
                    var images=root.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                    var icon=Array.Find(images,image=>image.name=="Wallet icon "+kind);
                    Assert.IsNotNull(icon);Assert.IsNotNull(icon.sprite);
                }
                yield return null;
                LogAssert.NoUnexpectedReceived();
            } finally {UnityEngine.Object.DestroyImmediate(root);MoonlitRuntimeSettings.ResetSession();MainScreen.PersistenceEnabled=persistence;}
#else
            Assert.Ignore("Cloud Editor fixture uses runtime assets.");yield return null;
#endif
        }
    }
}
