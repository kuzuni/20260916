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
        [Test] public void SummonsStopAtHundredAndRequireExactConfirmedQuantityBeforeCharging()
        {
            var category=CollectionProgression.Data.categories[2];category.summonLevel=99;category.experience=70;
            int tickets=2,diamonds=2800;var random=new Random(12);
            Assert.AreEqual(30,category.RemainingSummons);Assert.AreEqual(30,CollectionProgression.SummonQuantity(2,500));
            Assert.IsFalse(CollectionProgression.TrySummon(2,500,ref tickets,ref diamonds,random,out var rejected));
            Assert.IsEmpty(rejected);Assert.AreEqual(2,tickets);Assert.AreEqual(2800,diamonds);
            Assert.AreEqual(70,category.experience);Assert.IsTrue(category.entries.All(e=>!e.unlocked));
            Assert.IsTrue(CollectionProgression.TrySummon(2,30,ref tickets,ref diamonds,random,out var result));
            Assert.AreEqual(30,result.Length);Assert.AreEqual(30,category.entries.Sum(e=>e.fragments));
            Assert.AreEqual(0,tickets);Assert.AreEqual(0,diamonds);Assert.AreEqual(100,category.summonLevel);
            Assert.AreEqual(0,category.experience);Assert.IsFalse(category.CanSummon);Assert.IsTrue(category.CanAscend);
            tickets=500;diamonds=10000;
            Assert.IsFalse(CollectionProgression.TrySummon(2,1,ref tickets,ref diamonds,random,out _));
            Assert.AreEqual(500,tickets);Assert.AreEqual(10000,diamonds);
            category.AddExperience();Assert.AreEqual(0,category.experience);
            Assert.AreEqual(1,CollectionProgression.Data.categories[0].summonLevel);
        }
        [Test] public void FiveHundredSummonsGrantFiveHundredCopiesWithoutChargingExtra()
        {
            int tickets=30,diamonds=47000;
            Assert.IsTrue(CollectionProgression.TrySummon(0,500,ref tickets,ref diamonds,new Random(7),out var result));
            Assert.AreEqual(500,result.Length);Assert.AreEqual(500,CollectionProgression.Data.categories[0].entries.Sum(e=>e.fragments));
            Assert.AreEqual(0,tickets);Assert.AreEqual(0,diamonds);
        }
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void AscensionResetsOnlyItsCategoryAndEachPrimitiveBenefitDoublesPriorCelestialHundred(int category)
        {
            var other=CollectionProgression.Data.categories[(category+1)%3];other.entries[0].unlocked=true;other.experience=7;
            Assert.IsFalse(CollectionProgression.Ascend(category));
            for(int star=1;star<=3;star++){
                var before=CollectionProgression.Data.categories[category];before.summonLevel=100;
                var old=before.entries.Skip(27).ToArray();
                foreach(var entry in old){entry.unlocked=true;entry.level=100;entry.fragments=20;}
                CollectionProgression.Equip(old[0],0);
                double[] hp=old.Select(e=>e.OwnedHealth).ToArray(),attack=old.Select(e=>e.OwnedAttack).ToArray();
                double heal=old[0].FixedHeal,buff=old[0].FixedAttackBoost,weak=old[1].FixedDamage,strong=old[2].FixedDamage;
                double equippedHp=old[0].EquippedHealth,equippedAttack=old[0].EquippedAttack;
                Assert.IsTrue(CollectionProgression.Ascend(category));
                var after=CollectionProgression.Data.categories[category];
                Assert.AreEqual(star,after.ascension);Assert.AreEqual(1,after.summonLevel);Assert.AreEqual(0,after.experience);
                Assert.IsTrue(after.entries.All(e=>e.ascension==star&&e.level==1&&!e.unlocked&&e.fragments==0));
                Assert.IsTrue(after.equipped.All(id=>id==-1));Assert.IsFalse(CollectionProgression.Equip(old[0],0));
                Assert.AreSame(other,CollectionProgression.Data.categories[(category+1)%3]);Assert.AreEqual(7,other.experience);
                for(int i=0;i<3;i++){
                    Assert.That(after.entries[i].OwnedHealth,Is.EqualTo(hp[i]*2).Within(Math.Abs(hp[i])*1e-10));
                    Assert.That(after.entries[i].OwnedAttack,Is.EqualTo(attack[i]*2).Within(Math.Abs(attack[i])*1e-10));
                }
                Assert.That(after.entries[0].EquippedHealth,Is.EqualTo(equippedHp*2).Within(Math.Max(1,Math.Abs(equippedHp))*1e-10));
                Assert.That(after.entries[0].EquippedAttack,Is.EqualTo(equippedAttack*2).Within(Math.Max(1,Math.Abs(equippedAttack))*1e-10));
                Assert.That(after.entries[0].FixedHeal,Is.EqualTo(heal*2).Within(Math.Max(1,Math.Abs(heal))*1e-10));
                Assert.That(after.entries[0].FixedAttackBoost,Is.EqualTo(buff*2).Within(Math.Max(1,Math.Abs(buff))*1e-10));
                Assert.That(after.entries[1].FixedDamage,Is.EqualTo(weak*2).Within(Math.Max(1,Math.Abs(weak))*1e-10));
                Assert.That(after.entries[2].FixedDamage,Is.EqualTo(strong*2).Within(Math.Max(1,Math.Abs(strong))*1e-10));
            }
            CollectionProgression.Data.categories[category].summonLevel=100;
            Assert.IsFalse(CollectionProgression.Data.categories[category].CanAscend);Assert.IsFalse(CollectionProgression.Ascend(category));
        }
        [Test] public void AscensionSaveRestoresStarsAndNormalizesFormerUnboundedLevels()
        {
            var category=CollectionProgression.Data.categories[1];category.summonLevel=100;
            Assert.IsTrue(CollectionProgression.Ascend(1));category=CollectionProgression.Data.categories[1];
            category.entries[2].unlocked=true;category.entries[2].level=7;category.entries[2].fragments=3;
            CollectionProgression.Equip(category.entries[2],0);category.summonLevel=101;category.experience=12;
            string json=UnityEngine.JsonUtility.ToJson(CollectionProgression.Data);CollectionProgression.Reset();
            UnityEngine.JsonUtility.FromJsonOverwrite(json,CollectionProgression.Data);CollectionProgression.NormalizeAfterLoad();
            category=CollectionProgression.Data.categories[1];
            Assert.AreEqual(1,category.ascension);Assert.AreEqual(1,category.entries[2].ascension);
            Assert.AreEqual(7,category.entries[2].level);Assert.AreEqual(3,category.entries[2].fragments);
            Assert.AreEqual(2,category.equipped[0]);Assert.AreEqual(100,category.summonLevel);Assert.AreEqual(0,category.experience);
        }
        [Test] public void UpgradeAndQuickEquipAlertsReportAnActualAvailableImprovement()
        {
            Assert.IsFalse(CollectionProgression.HasUpgrade(1));Assert.IsFalse(CollectionProgression.HasBetterEquip(1));
            var pet=CollectionProgression.Data.categories[1].entries[0];pet.unlocked=true;
            Assert.IsTrue(CollectionProgression.HasBetterEquip(1));CollectionProgression.QuickEquip(1);
            Assert.IsFalse(CollectionProgression.HasBetterEquip(1));
            pet.fragments=2;Assert.IsTrue(CollectionProgression.HasUpgrade(1));Assert.IsTrue(pet.Upgrade());
            Assert.IsFalse(CollectionProgression.HasUpgrade(1));Assert.IsFalse(CollectionProgression.HasBetterEquip(1));
            CollectionProgression.Data.categories[1].entries[29].unlocked=true;
            Assert.IsTrue(CollectionProgression.HasBetterEquip(1));
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
            Assert.AreEqual(full.attack/2,skills[1].FixedDamage);Assert.AreEqual(full.attack*1.2,skills[2].FixedDamage);
            skills[2].level=100;Assert.AreEqual(EquipmentRules.FullSetStats(0,100).attack*1.2,skills[2].FixedDamage);
        }
        [Test] public void LinearGearTierBoundaryPropagatesToEveryCollectionBenefit()
        {
            double oldHealth=EquipmentRules.baseHealth,oldAttack=EquipmentRules.baseAttack;
            try {
                EquipmentRules.baseHealth=80;EquipmentRules.baseAttack=10;
                var skills=CollectionProgression.Data.categories[0].entries;
                var pet=CollectionProgression.Data.categories[1].entries[0];
                var mount=CollectionProgression.Data.categories[2].entries[0];
                for(int tier=0;tier<=1;tier++){
                    int level=tier==0?100:1;
                    double hp=tier==0?2640:5280,attack=tier==0?330:660;
                    foreach(var entry in skills.Take(3)){entry.grade=tier;entry.level=level;}
                    pet.grade=mount.grade=tier;pet.level=mount.level=level;
                    Assert.That(skills[0].OwnedHealth*3,Is.EqualTo(hp/4).Within(.000001));
                    Assert.That(skills[0].OwnedAttack*3,Is.EqualTo(attack/4).Within(.000001));
                    Assert.That(pet.EquippedHealth*3,Is.EqualTo(hp/2).Within(.000001));
                    Assert.That(pet.EquippedAttack*3,Is.EqualTo(attack/2).Within(.000001));
                    Assert.That(mount.EquippedHealth,Is.EqualTo(hp/2).Within(.000001));
                    Assert.That(mount.EquippedAttack,Is.EqualTo(attack/2).Within(.000001));
                    Assert.That(skills[0].FixedHeal,Is.EqualTo(hp/4).Within(.000001));
                    Assert.That(skills[0].FixedAttackBoost,Is.EqualTo(attack/5).Within(.000001));
                    Assert.That(skills[1].FixedDamage,Is.EqualTo(attack/2).Within(.000001));
                    Assert.That(skills[2].FixedDamage,Is.EqualTo(attack*1.2).Within(.000001));
                }
                double heal=skills[0].FixedHeal,damage=skills[2].FixedDamage,owned=pet.OwnedHealth;
                EquipmentRules.baseHealth*=2;EquipmentRules.baseAttack*=3;
                Assert.That(skills[0].FixedHeal,Is.EqualTo(heal*2).Within(.000001));
                Assert.That(skills[2].FixedDamage,Is.EqualTo(damage*3).Within(.000001));
                Assert.That(pet.OwnedHealth,Is.EqualTo(owned*2).Within(.000001));
            } finally {EquipmentRules.baseHealth=oldHealth;EquipmentRules.baseAttack=oldAttack;}
        }
        [Test] public void ThirtySkillsHaveDistinctThemedIdentityAndStableArtPaths()
        {
            var skills=CollectionProgression.Data.categories[0].entries;
            Assert.AreEqual(30,skills.Select(e=>e.Name).Distinct().Count());
            Assert.AreEqual(30,skills.Select(e=>SkillCatalog.IconKey(e.grade,e.variant)).Distinct().Count());
            foreach(var entry in skills){
                Assert.IsNotEmpty(SkillCatalog.Description(entry.grade,entry.variant));
                Assert.AreEqual(entry.variant==0?3:entry.variant==1?2:5,entry.Cooldown);
            }
            Assert.AreEqual("사냥꾼의 만찬",skills[0].Name);
            Assert.AreEqual("전차 돌격",skills[10].Name);
            Assert.AreEqual("신의 분노",skills[29].Name);
            Assert.AreEqual("Moonlit/Combat/Skills/Tier09/Strong",SkillCatalog.IconKey(9,2));
        }
        [Test] public void JsonRestoreRetainsUnlockedZeroFragmentEntryAndIndependentSummonProgress()
        {
            var entry=CollectionProgression.Data.categories[1].entries[0];
            entry.unlocked=true;entry.fragments=2;Assert.IsTrue(entry.Upgrade());
            Assert.IsTrue(CollectionProgression.Equip(entry,2));
            CollectionProgression.Data.categories[2].summonLevel=12;
            CollectionProgression.Data.categories[2].experience=7;
            double owned=CollectionProgression.OwnedHealth,equipped=CollectionProgression.EquippedHealth;
            string json=UnityEngine.JsonUtility.ToJson(CollectionProgression.Data);
            CollectionProgression.Reset();
            UnityEngine.JsonUtility.FromJsonOverwrite(json,CollectionProgression.Data);
            var restored=CollectionProgression.Data.categories[1].entries[0];
            Assert.IsTrue(restored.unlocked);Assert.AreEqual(0,restored.fragments);Assert.AreEqual(2,restored.level);
            Assert.AreEqual(0,CollectionProgression.Data.categories[1].equipped[2]);
            Assert.AreEqual(12,CollectionProgression.Data.categories[2].summonLevel);
            Assert.AreEqual(7,CollectionProgression.Data.categories[2].experience);
            Assert.AreEqual(1,CollectionProgression.Data.categories[0].summonLevel);
            Assert.AreEqual(owned,CollectionProgression.OwnedHealth);
            Assert.AreEqual(equipped,CollectionProgression.EquippedHealth);
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
        [Test] public void DungeonEntryDefersSpendUntilClaimAndCompletionCannotPayTwice()
        {
            Assert.IsTrue(DungeonProgression.BeginEntry(0,1));Assert.AreEqual(2,DungeonProgression.Data.keys[0]);
            Assert.IsFalse(DungeonProgression.BeginEntry(1,1));
            Assert.IsFalse(DungeonProgression.CompleteEntry(false,out _,out _));
            Assert.AreEqual(2,DungeonProgression.Data.keys[0]);
            Assert.AreEqual(0,DungeonProgression.Data.highestCleared[0]);
            Assert.IsTrue(DungeonProgression.BeginEntry(0,1));
            Assert.IsTrue(DungeonProgression.CompleteEntry(true,out int index,out int amount));
            Assert.AreEqual(0,index);Assert.AreEqual(100,amount);
            Assert.AreEqual(2,DungeonProgression.Data.keys[0]);Assert.AreEqual(1,DungeonProgression.NextDifficulty(0));
            Assert.IsFalse(DungeonProgression.CompleteEntry(true,out _,out _));
            Assert.IsTrue(DungeonProgression.ClaimEntry(out index,out amount));
            Assert.AreEqual(0,index);Assert.AreEqual(100,amount);
            Assert.AreEqual(1,DungeonProgression.Data.keys[0]);Assert.AreEqual(2,DungeonProgression.NextDifficulty(0));
            Assert.IsFalse(DungeonProgression.ClaimEntry(out _,out _));
        }
        [Test] public void RejectedBattleReleasesReservationWithoutChangingKeys()
        {
            Assert.IsTrue(DungeonProgression.BeginEntry(2,1));Assert.AreEqual(2,DungeonProgression.Data.keys[2]);
            DungeonProgression.CancelEntry();DungeonProgression.CancelEntry();
            Assert.AreEqual(2,DungeonProgression.Data.keys[2]);
            Assert.IsFalse(DungeonProgression.CompleteEntry(true,out _,out _));
            Assert.IsFalse(DungeonProgression.ClaimEntry(out _,out _));
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
