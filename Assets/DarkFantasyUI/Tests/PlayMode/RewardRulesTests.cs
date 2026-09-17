using System;
using NUnit.Framework;
using UnityEngine;
namespace Moonlit.UI.Tests
{
    public class RewardRulesTests
    {
        [SetUp] public void Reset(){MoonlitRuntimeSettings.ResetSession();}
        [Test] public void AccrualRetainsSecondsAcrossClaimsAndClockRollback()
        {
            var s=new RewardState();s.Advance(1000);s.Advance(1059);
            Assert.AreEqual(59,s.GoldAvailable);Assert.AreEqual(0,s.HammersAvailable);
            s.goldClaimed=59;s.Advance(1060);
            Assert.AreEqual(1,s.GoldAvailable);Assert.AreEqual(1,s.HammersAvailable);
            s.Advance(1000);Assert.AreEqual(1060,s.lastTickUtc);
            s.Advance(1061);Assert.AreEqual(2,s.GoldAvailable);
        }
        [Test] public void ArenaBoundariesAndPodiumAlwaysBeatChallengerFive()
        {
            Assert.AreEqual("아이언 1",RewardRules.TierName(0));
            Assert.AreEqual("아이언 1",RewardRules.TierName(100));
            Assert.AreEqual("아이언 2",RewardRules.TierName(101));
            Assert.AreEqual("챌린저 5",RewardRules.TierName(4401));
            int bestTier=RewardRules.RewardFactor(4500,6);
            for(int rank=1;rank<=5;rank++)Assert.Greater(RewardRules.RewardFactor(0,rank),bestTier);
            Assert.Greater(RewardRules.RewardFactor(0,1),RewardRules.RewardFactor(0,2));
        }
        [Test] public void SaveRoundTripPreservesClocksPendingEquipmentUnlockAndClaims()
        {
            var forge=ForgeState.Current;
            forge.pending.Add(forge.DrawTier(0,new System.Random(2)));
            forge.filledSegments=forge.Segments;forge.StartUpgrade(DateTime.UtcNow);
            var entry=CollectionProgression.Data.categories[0].entries[0];entry.unlocked=true;entry.level=2;entry.fragments=0;
            DungeonProgression.Data.keys[0]=5;DungeonProgression.Data.highestCleared[0]=10;
            var rewards=RewardState.Current;rewards.Advance(1000);rewards.Advance(1121);rewards.passClaimed[99]=true;
            var save=new GameplaySave {forge=JsonUtility.ToJson(forge),collections=JsonUtility.ToJson(CollectionProgression.Data),dungeons=JsonUtility.ToJson(DungeonProgression.Data),rewards=rewards};
            string json=JsonUtility.ToJson(save);
            MoonlitRuntimeSettings.ResetSession();
            var restored=JsonUtility.FromJson<GameplaySave>(json);
            JsonUtility.FromJsonOverwrite(restored.forge,ForgeState.Current);
            JsonUtility.FromJsonOverwrite(restored.collections,CollectionProgression.Data);
            JsonUtility.FromJsonOverwrite(restored.dungeons,DungeonProgression.Data);
            Assert.AreEqual(1,ForgeState.Current.pending.Count);
            Assert.AreEqual(1,ForgeState.Current.draws[0]);
            Assert.Greater(ForgeState.Current.upgradeEndsUtcTicks,0);
            Assert.IsTrue(CollectionProgression.Data.categories[0].entries[0].unlocked);
            Assert.AreEqual(0,CollectionProgression.Data.categories[0].entries[0].fragments);
            Assert.AreEqual(5,DungeonProgression.Data.keys[0]);
            Assert.AreEqual(10,DungeonProgression.Data.highestCleared[0]);
            Assert.IsTrue(restored.rewards.passClaimed[99]);
            Assert.AreEqual(121,restored.rewards.GoldAvailable);
            Assert.AreEqual(2,restored.rewards.HammersAvailable);
            Assert.IsNull(ForgeState.Current.equipped[0],"Empty slots must not become phantom equipment after restore.");
        }
    }
}
