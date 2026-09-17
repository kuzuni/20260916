using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Moonlit.UI.Tests
{
    public sealed class RuntimeSettingsTests
    {
        const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
        static FieldInfo Field(Type type, string name) => type.GetField(name, StaticPrivate);

        [Test]
        public void Startup_OverridesVSyncAndFrameCap_AndRunsWithoutFocus()
        {
            int previousVSync=QualitySettings.vSyncCount, previousCap=Application.targetFrameRate;
            bool previousBackground=Application.runInBackground;
            try
            {
                QualitySettings.vSyncCount=1;
                Application.targetFrameRate=15;
                Application.runInBackground=false;
                MoonlitRuntimeSettings.Apply();
                Assert.AreEqual(0,QualitySettings.vSyncCount,"VSync must not override the requested frame cap");
                Assert.AreEqual(60,Application.targetFrameRate);
                Assert.IsTrue(Application.runInBackground);
            }
            finally
            {
                QualitySettings.vSyncCount=previousVSync;
                Application.targetFrameRate=previousCap;
                Application.runInBackground=previousBackground;
            }
        }

        [Test]
        public void FreshSessions_ResetSpentCurrencySkillsClaimsAndProfile_WithoutReloadingStatics()
        {
            var progression=typeof(ProgressionScreenModule);
            var social=typeof(SocialScreenModule);
            try
            {
                for(int session=0;session<2;session++)
                {
                    var collections=CollectionProgression.Data;
                    var forge=ForgeState.Current;
                    var rewards=RewardState.Current;
                    var dungeons=DungeonProgression.Data;
                    Field(progression,"selectedCollectionTab").SetValue(null,2);
                    foreach(var category in collections.categories)
                    {
                        category.summonLevel=53; category.experience=87;
                        category.entries[0].level=100;
                        category.entries[0].fragments=99;
                        category.entries[0].unlocked=true;
                        category.equipped[0]=0;
                    }
                    forge.level=35; forge.autoEnabled=true; forge.draws[0]=100;
                    forge.filledSegments=5; forge.freeSkipsUsed=4;
                    forge.upgradeEndsUtcTicks=DateTime.UtcNow.AddHours(1).Ticks;
                    forge.keepTiers[0]=false; forge.affixMask=1;
                    forge.pending.Add(new EquipmentRoll { id=22,level=90 });
                    forge.equipped[0]=new EquipmentRoll { id=11,level=80 };
                    rewards.passClaimed[0]=true; rewards.passClaimed[99]=true;
                    rewards.accruedSeconds=300; rewards.goldClaimed=100; rewards.hammersClaimed=2;
                    rewards.arenaPoints=3400; rewards.arenaChallenges=40; rewards.arenaWins=30;
                    dungeons.keys[0]=0; dungeons.highestCleared[0]=12; dungeons.refillDay="2099-12-31";
                    Field(social,"profileName").SetValue(null,"changed profile");
                    Field(social,"profileFemale").SetValue(null,true);
                    MoonlitRuntimeSettings.ResetSession();
                    Assert.AreNotSame(collections,CollectionProgression.Data);
                    Assert.AreEqual(0,Field(progression,"selectedCollectionTab").GetValue(null));
                    foreach(var category in CollectionProgression.Data.categories)
                    {
                        Assert.AreEqual(1,category.summonLevel); Assert.AreEqual(0,category.experience);
                        CollectionAssert.AreEqual(new[]{-1,-1,-1},category.equipped);
                        foreach(var entry in category.entries)
                        {
                            Assert.AreEqual(1,entry.level); Assert.AreEqual(0,entry.fragments);
                            Assert.IsFalse(entry.unlocked,"A fresh model must not retain previous play-session ownership.");
                        }
                    }
                    Assert.AreEqual(0,CollectionProgression.EquippedSkills.Count);
                    Assert.AreNotSame(forge,ForgeState.Current);
                    Assert.AreEqual(1,ForgeState.Current.level);
                    Assert.IsFalse(ForgeState.Current.autoEnabled);
                    Assert.IsEmpty(ForgeState.Current.pending);
                    Assert.AreEqual(0,ForgeState.Current.filledSegments);
                    Assert.AreEqual(0,ForgeState.Current.upgradeEndsUtcTicks);
                    Assert.AreEqual(0,ForgeState.Current.freeSkipsUsed);
                    Assert.AreEqual(511,ForgeState.Current.affixMask);
                    foreach(var equipped in ForgeState.Current.equipped) Assert.IsNull(equipped);
                    foreach(int count in ForgeState.Current.draws) Assert.AreEqual(0,count);
                    foreach(bool keep in ForgeState.Current.keepTiers) Assert.IsTrue(keep);
                    Assert.AreNotSame(rewards,RewardState.Current);
                    Assert.AreEqual(0,RewardState.Current.GoldAvailable);
                    Assert.AreEqual(0,RewardState.Current.HammersAvailable);
                    Assert.AreEqual(0,RewardState.Current.arenaPoints);
                    Assert.AreEqual(0,RewardState.Current.arenaChallenges);
                    Assert.AreEqual(0,RewardState.Current.arenaWins);
                    foreach(bool claimed in RewardState.Current.passClaimed) Assert.IsFalse(claimed);
                    Assert.AreNotSame(dungeons,DungeonProgression.Data);
                    CollectionAssert.AreEqual(new[]{2,2,2,2},DungeonProgression.Data.keys);
                    CollectionAssert.AreEqual(new[]{0,0,0,0},DungeonProgression.Data.highestCleared);
                    Assert.AreEqual("",DungeonProgression.Data.refillDay);
                    Assert.AreEqual("moonzzanf",Field(social,"profileName").GetValue(null));
                    Assert.AreEqual(false,Field(social,"profileFemale").GetValue(null));
                }
            }
            finally { MoonlitRuntimeSettings.ResetSession(); }
        }

        [Test]
        public void FreshSession_RebuildsDestroyedAvatarSprites_FromRetainedStaticCache()
        {
            var portrait=typeof(SocialScreenModule).GetMethod("AvatarPortrait",StaticPrivate);
            var first=(Sprite)portrait.Invoke(null,new object[]{0});
            Assert.IsNotNull(first);
            UnityEngine.Object.DestroyImmediate(first);
            MoonlitRuntimeSettings.ResetSession();
            var next=(Sprite)portrait.Invoke(null,new object[]{0});
            Assert.IsTrue(next,"A retained array must not return a sprite destroyed when the preceding Play session ended");
            Assert.AreNotSame(first,next);
            Assert.IsNotNull(next.texture);
        }
    }
}
