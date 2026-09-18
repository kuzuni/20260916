using System;
using NUnit.Framework;
using UnityEngine;

namespace Moonlit.UI.Tests
{
    public sealed class DungeonClaimTests
    {
        [SetUp] public void Setup()
        {
            DungeonProgression.Reset();
            // Keep tests deterministic: zero keys below must not trigger a first-load daily refill.
            DungeonProgression.Data.refillDay=DateTime.UtcNow.AddHours(9).ToString("yyyy-MM-dd");
        }
        [TearDown] public void Teardown() { DungeonProgression.Reset(); }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void EntryAndWinDoNotSpendUntilSingleClaim(int dungeon)
        {
            var data=DungeonProgression.Data;
            data.highestCleared[dungeon]=5;
            int[] keys=(int[])data.keys.Clone();
            Assert.IsTrue(DungeonProgression.BeginEntry(dungeon,6));
            CollectionAssert.AreEqual(keys,data.keys,"Entering only reserves this challenge.");
            Assert.IsFalse(DungeonProgression.BeginEntry((dungeon+1)%4,1));
            Assert.IsFalse(DungeonProgression.ClaimEntry(out _,out _),"An unfinished battle has nothing to claim.");
            Assert.IsTrue(DungeonProgression.CompleteEntry(true,out int previewIndex,out int previewAmount));
            Assert.AreEqual(dungeon,previewIndex);
            Assert.AreEqual(DungeonProgression.Reward(dungeon,6),previewAmount);
            CollectionAssert.AreEqual(keys,data.keys);
            Assert.AreEqual(5,data.highestCleared[dungeon]);
            Assert.IsTrue(data.pendingClaim);Assert.AreEqual(dungeon,data.pendingIndex);Assert.AreEqual(6,data.pendingDifficulty);
            Assert.IsFalse(DungeonProgression.CompleteEntry(true,out _,out _),"Repeated battle callbacks cannot create more rewards.");
            Assert.IsFalse(DungeonProgression.BeginEntry((dungeon+1)%4,1),"Pending rewards block a new reservation.");
            Assert.IsFalse(DungeonProgression.TrySweep(dungeon,out _),"Pending rewards block sweeps.");
            Assert.IsTrue(DungeonProgression.ClaimEntry(out int claimedIndex,out int amount));
            Assert.AreEqual(dungeon,claimedIndex);Assert.AreEqual(DungeonProgression.Reward(dungeon,6),amount);
            keys[dungeon]--;CollectionAssert.AreEqual(keys,data.keys);
            Assert.AreEqual(6,data.highestCleared[dungeon]);Assert.IsFalse(data.pendingClaim);
            Assert.IsFalse(DungeonProgression.ClaimEntry(out _,out _),"The reward can only be claimed once.");
            CollectionAssert.AreEqual(keys,data.keys);
            Assert.IsTrue(DungeonProgression.BeginEntry(dungeon,7),"Claiming releases the pending-reward gate.");
            DungeonProgression.CancelEntry();
        }

        [Test] public void DefeatAndCancellationPreserveKeysAndDoNotCreateRewards()
        {
            Assert.IsTrue(DungeonProgression.BeginEntry(0,1));
            Assert.IsFalse(DungeonProgression.CompleteEntry(false,out _,out _));
            Assert.AreEqual(2,DungeonProgression.Data.keys[0]);
            Assert.AreEqual(0,DungeonProgression.Data.highestCleared[0]);
            Assert.IsFalse(DungeonProgression.Data.pendingClaim);
            Assert.IsFalse(DungeonProgression.ClaimEntry(out _,out _));
            Assert.IsTrue(DungeonProgression.BeginEntry(0,1));
            DungeonProgression.CancelEntry();DungeonProgression.CancelEntry();
            Assert.AreEqual(2,DungeonProgression.Data.keys[0],"Cancelling a reservation cannot manufacture refund keys.");
            Assert.IsFalse(DungeonProgression.CompleteEntry(true,out _,out _));
            Assert.IsTrue(DungeonProgression.BeginEntry(1,1));
        }

        [Test] public void PendingRewardRoundTripSurvivesRestartAndStillClaimsExactlyOnce()
        {
            DungeonProgression.Data.keys[2]=5;
            DungeonProgression.Data.highestCleared[2]=8;
            Assert.IsTrue(DungeonProgression.BeginEntry(2,9));
            Assert.IsTrue(DungeonProgression.CompleteEntry(true,out _,out _));
            string saved=JsonUtility.ToJson(DungeonProgression.Data);
            DungeonProgression.Reset();
            JsonUtility.FromJsonOverwrite(saved,DungeonProgression.Data);
            Assert.IsTrue(DungeonProgression.Data.pendingClaim);
            Assert.AreEqual(2,DungeonProgression.Data.pendingIndex);
            Assert.AreEqual(9,DungeonProgression.Data.pendingDifficulty);
            Assert.AreEqual(5,DungeonProgression.Data.keys[2]);
            Assert.AreEqual(8,DungeonProgression.Data.highestCleared[2]);
            DungeonProgression.CancelEntry();
            Assert.IsTrue(DungeonProgression.Data.pendingClaim,"Cancelling an inactive reservation cannot discard a saved reward.");
            Assert.IsFalse(DungeonProgression.BeginEntry(0,1));
            Assert.IsFalse(DungeonProgression.TrySweep(2,out _));
            Assert.IsTrue(DungeonProgression.ClaimEntry(out int index,out int amount));
            Assert.AreEqual(2,index);Assert.AreEqual(11,amount);
            Assert.AreEqual(4,DungeonProgression.Data.keys[2]);
            Assert.AreEqual(9,DungeonProgression.Data.highestCleared[2]);
            string claimed=JsonUtility.ToJson(DungeonProgression.Data);
            DungeonProgression.Reset();JsonUtility.FromJsonOverwrite(claimed,DungeonProgression.Data);
            Assert.IsFalse(DungeonProgression.ClaimEntry(out _,out _));
            Assert.AreEqual(4,DungeonProgression.Data.keys[2]);
        }

        [Test] public void EntryRequiresAKeyAndSweepStillSpendsImmediately()
        {
            DungeonProgression.Data.keys[1]=0;
            Assert.IsFalse(DungeonProgression.BeginEntry(1,1));
            Assert.IsFalse(DungeonProgression.CompleteEntry(true,out _,out _));
            Assert.IsFalse(DungeonProgression.ClaimEntry(out _,out _));
            DungeonProgression.Data.highestCleared[0]=10;
            Assert.IsTrue(DungeonProgression.TrySweep(0,out int amount));
            Assert.AreEqual(124,amount);Assert.AreEqual(1,DungeonProgression.Data.keys[0]);
            Assert.IsFalse(DungeonProgression.Data.pendingClaim,"Sweeps pay immediately and do not create a second claim.");
            Assert.AreEqual(10,DungeonProgression.Data.highestCleared[0]);
        }

        [Test] public void ClaimingAnOlderDifficultyCannotReduceHighestCleared()
        {
            DungeonProgression.Data.highestCleared[3]=10;
            Assert.IsTrue(DungeonProgression.BeginEntry(3,4));
            Assert.IsTrue(DungeonProgression.CompleteEntry(true,out _,out _));
            Assert.AreEqual(10,DungeonProgression.Data.highestCleared[3]);
            Assert.IsTrue(DungeonProgression.ClaimEntry(out int index,out int amount));
            Assert.AreEqual(3,index);Assert.AreEqual(6,amount);
            Assert.AreEqual(10,DungeonProgression.Data.highestCleared[3]);
        }
    }
}
