using System;
using System.Linq;
using NUnit.Framework;
namespace Moonlit.UI.Tests
{
    public sealed class CollectionRulesTests
    {
        [SetUp] public void Setup(){CollectionProgression.Reset();DungeonProgression.Reset();}
        [Test] public void ThreeCategoriesHaveThreeEntriesPerEquipmentGradeAndStartLocked()
        {
            foreach(var category in CollectionProgression.Data.categories){
                Assert.AreEqual(EquipmentRules.TierNames.Length*3,category.entries.Length);
                Assert.IsTrue(category.entries.All(e=>e.level==1&&!e.unlocked&&e.fragments==0));
                Assert.AreEqual(1,category.summonLevel);Assert.AreEqual(10,category.ExperienceRequired);
            }
        }
        [Test] public void FragmentsAreSpentAndUnlockPersistsAtZero()
        {
            var entry=CollectionProgression.Data.categories[0].entries[0];
            entry.unlocked=true;entry.fragments=2;
            Assert.IsTrue(entry.Upgrade());Assert.AreEqual(2,entry.level);Assert.AreEqual(0,entry.fragments);
            Assert.IsTrue(entry.unlocked);Assert.AreEqual(3,entry.Required);Assert.IsFalse(entry.Upgrade());
            entry.level=30;Assert.AreEqual(20,entry.Required);
            entry.level=100;entry.fragments=100;Assert.IsFalse(entry.Upgrade());Assert.AreEqual(100,entry.fragments);
        }
        [Test] public void SummonPaysOneTicketThenHundredDiamondsWithoutPartialFailedSpend()
        {
            int tickets=2,diamonds=299;
            Assert.IsFalse(CollectionProgression.PaySummon(5,ref tickets,ref diamonds));
            Assert.AreEqual(2,tickets);Assert.AreEqual(299,diamonds);
            diamonds=300;Assert.IsTrue(CollectionProgression.PaySummon(5,ref tickets,ref diamonds));
            Assert.AreEqual(0,tickets);Assert.AreEqual(0,diamonds);
        }
        [Test] public void SummonGrantsEveryCopyIncludingFirstAndIndependentExperience()
        {
            var result=CollectionProgression.Summon(1,10,new Random(4));
            Assert.AreEqual(10,result.Length);Assert.AreEqual(10,CollectionProgression.Data.categories[1].entries.Sum(e=>e.fragments));
            Assert.AreEqual(2,CollectionProgression.Data.categories[1].summonLevel);
            Assert.AreEqual(0,CollectionProgression.Data.categories[1].experience);
            Assert.AreEqual(13,CollectionProgression.Data.categories[1].ExperienceRequired);
            Assert.AreEqual(1,CollectionProgression.Data.categories[0].summonLevel);
            Assert.IsTrue(CollectionProgression.Data.categories[0].entries.All(e=>!e.unlocked));
        }
        [Test] public void ExperienceRequirementCapsAtHundred()
        {
            var category=CollectionProgression.Data.categories[2];category.summonLevel=60;
            Assert.AreEqual(100,category.ExperienceRequired);category.experience=99;category.AddExperience();
            Assert.AreEqual(61,category.summonLevel);Assert.AreEqual(0,category.experience);
        }
        [Test] public void StatRatiosUseSharedEquipmentBalanceAndScaleAtLevelHundred()
        {
            for(int category=0;category<3;category++)
                foreach(var entry in CollectionProgression.Data.categories[category].entries.Take(3))entry.unlocked=true;
            var full=EquipmentRules.FullSetStats(0,1);
            Assert.That(CollectionProgression.OwnedHealth,Is.EqualTo(full.health*.75).Within(.001));
            Assert.That(CollectionProgression.OwnedAttack,Is.EqualTo(full.attack*.75).Within(.001));
            CollectionProgression.QuickEquip(1);CollectionProgression.QuickEquip(2);
            Assert.That(CollectionProgression.EquippedHealth,Is.EqualTo(full.health).Within(.001));
            Assert.That(CollectionProgression.EquippedAttack,Is.EqualTo(full.attack).Within(.001));
            var skills=CollectionProgression.Data.categories[0].entries;
            Assert.AreEqual(full.health/4,skills[0].FixedHeal);Assert.AreEqual(full.attack/5,skills[0].FixedAttackBoost);
            Assert.AreEqual(full.attack/2,skills[1].FixedDamage);Assert.AreEqual(full.attack*1.5,skills[2].FixedDamage);
            skills[2].level=100;Assert.AreEqual(EquipmentRules.FullSetStats(0,100).attack*1.5,skills[2].FixedDamage);
        }
        [Test] public void EquipHasCategoryCapAndNoDuplicates()
        {
            var pets=CollectionProgression.Data.categories[1].entries;foreach(var entry in pets.Take(3))entry.unlocked=true;
            Assert.IsTrue(CollectionProgression.Equip(pets[0],0));Assert.IsTrue(CollectionProgression.Equip(pets[0],1));
            Assert.AreEqual(1,CollectionProgression.Equipped(1).Count);Assert.IsFalse(CollectionProgression.Equip(pets[0],3));
            var mount=CollectionProgression.Data.categories[2].entries[0];mount.unlocked=true;
            Assert.IsFalse(CollectionProgression.Equip(mount,1));Assert.IsTrue(CollectionProgression.Equip(mount,0));
        }
        [Test] public void ProbabilitiesUseAllEquipmentGradesAndNormalizeAtEverySummonLevel()
        {
            Assert.AreEqual(1,CollectionProgression.Probabilities(1)[0]);
            for(int level=1;level<=100;level++){
                var probabilities=CollectionProgression.Probabilities(level);
                Assert.AreEqual(EquipmentRules.TierNames.Length,probabilities.Length);
                Assert.That(probabilities.Sum(),Is.EqualTo(1).Within(.00001));
            }
            Assert.That(CollectionProgression.Probabilities(100).Last(),Is.EqualTo(.04).Within(.00001));
        }
        [Test] public void DungeonKeysRefillToMinimumAndNeverAccumulateOrDecrease()
        {
            DungeonProgression.Data.keys=new[]{5,0,2,1};
            DungeonProgression.RefreshDay(new DateTime(2026,9,18,15,0,0,DateTimeKind.Utc));
            CollectionAssert.AreEqual(new[]{5,2,2,2},DungeonProgression.Data.keys);
            DungeonProgression.Data.keys[1]=0;
            DungeonProgression.RefreshDay(new DateTime(2026,9,18,16,0,0,DateTimeKind.Utc));
            Assert.AreEqual(0,DungeonProgression.Data.keys[1]);
            DungeonProgression.RefreshDay(new DateTime(2026,9,19,15,0,0,DateTimeKind.Utc));
            Assert.AreEqual(2,DungeonProgression.Data.keys[1]);
        }
        [Test] public void DungeonEntrySpendsEvenOnLossAndCompletionCannotPayTwice()
        {
            Assert.IsTrue(DungeonProgression.BeginEntry(0,1));Assert.AreEqual(1,DungeonProgression.Data.keys[0]);
            Assert.IsFalse(DungeonProgression.BeginEntry(1,1));
            Assert.IsFalse(DungeonProgression.CompleteEntry(false,out _,out _));
            Assert.AreEqual(0,DungeonProgression.Data.highestCleared[0]);
            Assert.IsTrue(DungeonProgression.BeginEntry(0,1));
            Assert.IsTrue(DungeonProgression.CompleteEntry(true,out int index,out int amount));
            Assert.AreEqual(0,index);Assert.AreEqual(100,amount);
            Assert.IsFalse(DungeonProgression.CompleteEntry(true,out _,out _));
            Assert.AreEqual(2,DungeonProgression.NextDifficulty(0));
        }
        [Test] public void RejectedBattleRefundsReservedKeyExactlyOnce()
        {
            Assert.IsTrue(DungeonProgression.BeginEntry(2,1));
            DungeonProgression.CancelEntry();DungeonProgression.CancelEntry();
            Assert.AreEqual(2,DungeonProgression.Data.keys[2]);
            Assert.IsFalse(DungeonProgression.CompleteEntry(true,out _,out _));
        }
        [Test] public void SweepUsesPreviousToHighestClearedAndSpendsMatchingKey()
        {
            DungeonProgression.Data.highestCleared[0]=10;
            Assert.AreEqual(9,DungeonProgression.SweepDifficulty(0));
            Assert.IsTrue(DungeonProgression.TrySweep(0,out int reward));Assert.AreEqual(124,reward);
            Assert.AreEqual(1,DungeonProgression.Data.keys[0]);Assert.AreEqual(2,DungeonProgression.Data.keys[1]);
            Assert.IsFalse(DungeonProgression.TrySweep(1,out _));
            Assert.AreEqual(50,DungeonProgression.NormalStage(10));Assert.AreEqual(12,DungeonProgression.Reward(3,10));
            CollectionAssert.AreEqual(new[]{1,2,3,3},Enumerable.Range(0,4).Select(DungeonProgression.Waves));
        }
    }
}
