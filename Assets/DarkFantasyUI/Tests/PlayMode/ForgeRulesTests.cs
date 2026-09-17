using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace Moonlit.UI.Tests
{
    public sealed class ForgeRulesTests
    {
        [Test] public void SixSlotBaselineAndEveryTierBoundaryUseSameRules()
        {
            var initial=EquipmentRules.FullSetStats(0,1);
            Assert.AreEqual(240,initial.health);Assert.AreEqual(30,initial.attack);Assert.AreEqual(3,initial.speed);
            Assert.AreEqual(3,Enum.GetValues(typeof(EquipmentPart)).Cast<EquipmentPart>().Count(EquipmentRules.IsHealthPart));
            for(int tier=1;tier<10;tier++) {
                var previous=EquipmentRules.FullSetStats(tier-1,100);var next=EquipmentRules.FullSetStats(tier,1);
                Assert.That(next.health/previous.health,Is.EqualTo(2).Within(1e-10));
                Assert.That(next.attack/previous.attack,Is.EqualTo(2).Within(1e-10));
                Assert.AreEqual(previous.speed*2,next.speed);
            }
            Assert.That(EquipmentRules.BaseStats(0,2,EquipmentPart.Armor).health,Is.EqualTo(88).Within(.0001));
        }
        [Test] public void EveryProbabilityLevelSumsToOneAndImpossibleGradesStayZero()
        {
            for(int level=1;level<=35;level++) {
                var p=EquipmentRules.TierProbabilities(level);
                Assert.That(p.Sum(),Is.EqualTo(1).Within(1e-10));Assert.IsTrue(p.All(x=>x>=0));
            }
            Assert.AreEqual(1,EquipmentRules.TierProbabilities(1)[0]);
            Assert.AreEqual(0,EquipmentRules.TierProbabilities(1)[9]);
            Assert.AreEqual(0,EquipmentRules.TierProbabilities(35)[0]);
            Assert.AreEqual(.04,EquipmentRules.TierProbabilities(35)[9]);
        }
        [Test] public void AffixesRespectTierThresholdsRangesAndUniqueness()
        {
            var random=new System.Random(8347);
            for(int tier=0;tier<10;tier++)for(int n=0;n<200;n++){
                var a=EquipmentRules.RollAffixes(tier,random);
                Assert.AreEqual(tier<2?0:tier<4?1:2,a.Length);
                Assert.AreEqual(a.Length,a.Select(x=>x.kind).Distinct().Count());
                foreach(var x in a)Assert.That(x.percent,Is.InRange(1,EquipmentRules.AffixMaximums[(int)x.kind]));
            }
        }
        [Test] public void DrawCountersArePerGradeAndCapAtOneHundred()
        {
            var state=new ForgeState();var random=new System.Random(9);
            for(int i=1;i<=120;i++)Assert.AreEqual(Math.Min(100,i),state.DrawTier(0,random).level);
            Assert.AreEqual(1,state.DrawTier(1,random).level);
        }
        [Test] public void EquipTwiceRestoresOriginalAndSaleResolvesOnlyUnequipped()
        {
            var state=new ForgeState();var original=new EquipmentRoll{id=1,part=EquipmentPart.Weapon};
            var replacement=new EquipmentRoll{id=2,part=EquipmentPart.Weapon,level=2};
            state.equipped[5]=original;state.pending.Add(replacement);
            Assert.IsTrue(state.ToggleEquip(2));Assert.AreSame(replacement,state.equipped[5]);
            Assert.AreSame(original,state.Pending);
            Assert.IsTrue(state.ToggleEquip(1));Assert.AreSame(original,state.equipped[5]);
            Assert.IsTrue(state.SellPending(2,out int gold));Assert.Greater(gold,0);
            Assert.IsNull(state.Pending);Assert.IsFalse(state.SellPending(2,out _));
        }
        [Test] public void EmptySlotAllowsEquipOnly()
        {
            var state=new ForgeState();var item=new EquipmentRoll{id=1,part=EquipmentPart.Hat};state.pending.Add(item);
            Assert.IsFalse(state.SellPending(1,out _));Assert.IsTrue(state.ToggleEquip(1));
            Assert.AreSame(item,state.equipped[2]);Assert.IsNull(state.Pending);
        }
        [Test] public void UpgradeRequiresSegmentsStartElapsedAndExplicitClaim()
        {
            var state=new ForgeState();int gold=10000;
            Assert.IsFalse(state.StartUpgrade(DateTime.UtcNow));
            for(int i=0;i<state.Segments;i++)Assert.IsTrue(state.FillSegment(ref gold));
            var now=DateTime.UtcNow;Assert.IsTrue(state.StartUpgrade(now));
            Assert.IsFalse(state.StartUpgrade(now));Assert.IsFalse(state.ClaimUpgrade(now.AddSeconds(9)));
            Assert.AreEqual(1,state.level);Assert.IsTrue(state.ClaimUpgrade(now.AddSeconds(10)));
            Assert.AreEqual(2,state.level);Assert.AreEqual(0,state.filledSegments);
            Assert.IsFalse(state.ClaimUpgrade(now.AddSeconds(100)));
        }
        [Test] public void SkipChargesRemainingTimeAndDailyRefillHasFourUses()
        {
            var now=new DateTime(2026,9,18,0,0,0,DateTimeKind.Utc);
            var state=new ForgeState {upgradeEndsUtcTicks=now.AddSeconds(61).Ticks};int diamonds=11;
            Assert.AreEqual(11,state.DiamondSkipCost(now));Assert.IsTrue(state.DiamondSkip(now,ref diamonds));Assert.AreEqual(0,diamonds);
            for(int i=0;i<4;i++){state.upgradeEndsUtcTicks=now.AddHours(1).Ticks;Assert.IsTrue(state.FreeSkip(now));Assert.AreEqual(1800,state.RemainingSeconds(now));}
            Assert.IsFalse(state.FreeSkip(now));Assert.AreEqual(4,state.freeSkipsUsed);
            state.upgradeEndsUtcTicks=now.AddDays(1).AddHours(1).Ticks;Assert.IsTrue(state.FreeSkip(now.AddDays(1)));
            Assert.AreEqual(1,state.freeSkipsUsed);
        }
        [Test] public void ContinueQueueWaitsUntilTwentyFiveAndFiltersAreApplied()
        {
            var state=new ForgeState{continueAfterMatch=true};
            for(int i=0;i<24;i++)state.pending.Add(new EquipmentRoll{id=i+1});
            Assert.IsFalse(state.ShouldCompare);state.pending.Add(new EquipmentRoll{id=25});Assert.IsTrue(state.ShouldCompare);
            state.filterEnabled=true;state.affixMask=1<<(int)EquipmentAffixKind.DoubleChance;
            Assert.IsFalse(state.Matches(new EquipmentRoll()));
            var match=new EquipmentRoll{affixes=new[]{new EquipmentAffix{kind=EquipmentAffixKind.DoubleChance,percent=1}}};
            Assert.IsTrue(state.Matches(match));state.keepTiers[0]=false;Assert.IsFalse(state.Matches(match));
        }
        [Test] public void SaveRoundTripRetainsPendingAndUpgradeTimer()
        {
            var state=new ForgeState{level=9,upgradeEndsUtcTicks=DateTime.UtcNow.AddHours(3).Ticks};
            state.pending.Add(state.DrawTier(2,new System.Random(54)));state.draws[8]=100;
            var restored=JsonUtility.FromJson<ForgeState>(JsonUtility.ToJson(state));
            Assert.AreEqual(state.upgradeEndsUtcTicks,restored.upgradeEndsUtcTicks);
            Assert.AreEqual(state.Pending.id,restored.Pending.id);Assert.AreEqual(100,restored.draws[8]);
            Assert.AreEqual(1,restored.Pending.affixes.Length);
        }
    }
}
