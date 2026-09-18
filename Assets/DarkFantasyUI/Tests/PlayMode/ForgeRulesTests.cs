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
            Assert.That(EquipmentRules.BaseStats(0,2,EquipmentPart.Armor).health,Is.EqualTo(80*(1+10.0/99)).Within(.0001));
            Assert.AreEqual(880,EquipmentRules.BaseStats(0,100,EquipmentPart.Armor).health);
            Assert.AreEqual(1760,EquipmentRules.BaseStats(1,1,EquipmentPart.Armor).health);
            Assert.AreEqual(110,EquipmentRules.BaseStats(0,100,EquipmentPart.Weapon).attack);
            double increment=EquipmentRules.BaseStats(0,2,EquipmentPart.Armor).health-80;
            for(int level=2;level<=100;level++)Assert.That(EquipmentRules.BaseStats(0,level,EquipmentPart.Armor).health-EquipmentRules.BaseStats(0,level-1,EquipmentPart.Armor).health,Is.EqualTo(increment).Within(.000001));
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
        [Test] public void ContinueQueueUsesConfiguredBatchSizeAndFiltersAreApplied()
        {
            var state=new ForgeState{continueAfterMatch=true,batchSize=22};
            for(int i=0;i<21;i++)state.pending.Add(new EquipmentRoll{id=i+1});
            Assert.IsFalse(state.ShouldCompare);state.pending.Add(new EquipmentRoll{id=22});Assert.IsTrue(state.ShouldCompare);
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
        [Test] public void FreeSkipsResetAtKoreanMidnightNotUtcMidnight()
        {
            var before=new DateTime(2026,9,18,14,59,59,DateTimeKind.Utc);
            var state=new ForgeState {freeSkipDay="2026-09-18",freeSkipsUsed=4};
            state.ResetDaily(before);Assert.AreEqual(4,state.freeSkipsUsed);
            state.ResetDaily(before.AddSeconds(1));Assert.AreEqual(0,state.freeSkipsUsed);
            Assert.AreEqual("2026-09-19",state.freeSkipDay);
            state.freeSkipsUsed=4;
            state.ResetDaily(new DateTime(2026,9,19,0,0,0,DateTimeKind.Utc));
            Assert.AreEqual(4,state.freeSkipsUsed,"UTC midnight is 09:00 in the same Korean day");
        }
        [Test] public void ThreeTwentyTwoItemBatchesRetainTenElevenFiveBeforeComparison()
        {
            var state=new ForgeState {continueAfterMatch=true,batchSize=22,keepTiers=new bool[10]};
            state.keepTiers[2]=true;
            var random=new System.Random(811);
            int[] keepCounts={10,11,5},totals={10,21,26};
            for(int batch=0;batch<3;batch++) {
                var items=new EquipmentRoll[22];int expectedGold=0;
                for(int i=0;i<items.Length;i++) {
                    items[i]=state.DrawTier(i<keepCounts[batch]?2:0,random);state.pending.Add(items[i]);
                    if(i>=keepCounts[batch])expectedGold+=EquipmentRules.SaleGold(items[i]);
                }
                Assert.AreEqual(expectedGold,state.CompleteAutoBatch(items));
                Assert.AreEqual(totals[batch],state.pending.Count);
                Assert.AreEqual(batch==2,state.ShouldCompare);
                Assert.AreEqual(0,state.CompleteAutoBatch(items),"Repeated settlement cannot pay twice");
            }
            Assert.IsTrue(state.pending.All(item=>item.tier==2));
        }
        [Test] public void FilterMatchesZeroOneAndTwoAffixesWithoutInventingOptions()
        {
            var state=new ForgeState {filterEnabled=true,affixMask=1<<(int)EquipmentAffixKind.Regeneration};
            var primitive=new EquipmentRoll{tier=0};
            var earlyModern=new EquipmentRoll{tier=2,affixes=new[]{new EquipmentAffix{kind=EquipmentAffixKind.Regeneration,percent=1}}};
            var space=new EquipmentRoll{tier=4,affixes=new[]{new EquipmentAffix{kind=EquipmentAffixKind.CriticalChance,percent=2},new EquipmentAffix{kind=EquipmentAffixKind.Regeneration,percent=3}}};
            Assert.IsFalse(state.Matches(primitive));Assert.IsTrue(state.Matches(earlyModern));Assert.IsTrue(state.Matches(space));
            state.keepTiers[4]=false;Assert.IsFalse(state.Matches(space));
            state.filterEnabled=false;Assert.IsTrue(state.Matches(primitive));Assert.IsFalse(state.Matches(space));
        }
        [Test] public void NormalizeSaveRemovesPhantomNullsAndRebuildsIdentityAndArrays()
        {
            var state=new ForgeState {equipped=new EquipmentRoll[8],draws=new[]{500},keepTiers=null,autoEnabled=true,nextId=0,batchSize=999};
            state.equipped[0]=new EquipmentRoll();
            state.equipped[6]=new EquipmentRoll{id=17,part=EquipmentPart.Weapon,level=21};
            state.pending.Add(new EquipmentRoll());
            state.pending.Add(new EquipmentRoll{id=17,part=EquipmentPart.Weapon});
            state.pending.Add(new EquipmentRoll{id=18,tier=4,part=EquipmentPart.Hat,level=200,variant=9,affixes=new[]{
                new EquipmentAffix{kind=EquipmentAffixKind.LifeSteal,percent=99},
                new EquipmentAffix{kind=EquipmentAffixKind.LifeSteal,percent=1},
                new EquipmentAffix{kind=EquipmentAffixKind.Regeneration,percent=3}}});
            var restored=JsonUtility.FromJson<ForgeState>(JsonUtility.ToJson(state));
            restored.NormalizeAfterLoad();
            Assert.AreEqual(6,restored.equipped.Length);Assert.IsNull(restored.equipped[0]);
            Assert.AreEqual(17,restored.equipped[5].id);Assert.AreEqual(1,restored.pending.Count);
            Assert.AreEqual(18,restored.Pending.id);Assert.AreEqual(100,restored.Pending.level);Assert.AreEqual(2,restored.Pending.variant);
            Assert.AreEqual(10,restored.draws.Length);Assert.AreEqual(100,restored.draws[0]);Assert.AreEqual(100,restored.draws[4]);
            Assert.AreEqual(2,restored.Pending.affixes.Length);Assert.AreEqual(5,restored.Pending.affixes[0].percent);
            Assert.AreEqual(10,restored.keepTiers.Length);Assert.AreEqual(99,restored.batchSize);Assert.IsFalse(restored.autoEnabled);
            Assert.AreEqual(19,restored.DrawTier(1,new System.Random(2)).id);
            var empty=JsonUtility.FromJson<ForgeState>(JsonUtility.ToJson(new ForgeState()));
            empty.NormalizeAfterLoad();Assert.IsTrue(empty.equipped.All(item=>item==null));Assert.AreEqual(0,empty.TotalStats.health);
        }

        [TestCase(1)] [TestCase(2)] [TestCase(22)] [TestCase(99)]
        public void ContinueTargetTracksBatchSizeAndImmediateModeStopsAtFirstMatch(int batchSize)
        {
            var state=new ForgeState {batchSize=batchSize,continueAfterMatch=true};
            Assert.IsFalse(state.ShouldCompare);
            for(int i=1;i<batchSize;i++)state.pending.Add(new EquipmentRoll{id=i});
            Assert.IsFalse(state.ShouldCompare);
            state.pending.Add(new EquipmentRoll{id=batchSize});Assert.IsTrue(state.ShouldCompare);
            state.pending.Clear();state.pending.Add(new EquipmentRoll{id=100});
            state.continueAfterMatch=false;Assert.IsTrue(state.ShouldCompare);
        }
        [Test] public void InterruptedBatchSaveSettlesFrozenSalesOnceWithoutTouchingEarlierItems()
        {
            var state=new ForgeState {batchSize=22,keepTiers=new bool[10]};
            state.keepTiers[2]=true;var random=new System.Random(234);
            var previous=state.DrawTier(0,random);state.pending.Add(previous);
            var equipped=state.DrawTier(0,random);state.equipped[(int)equipped.part]=equipped;
            var batch=new EquipmentRoll[22];int expectedSale=0;
            for(int i=0;i<batch.Length;i++) {
                batch[i]=state.DrawTier(i<10?2:0,random);state.pending.Add(batch[i]);
                if(i>=10)expectedSale+=EquipmentRules.SaleGold(batch[i]);
            }
            state.TrackAutoBatch(batch);
            Assert.AreEqual(12,state.automaticSaleIds.Count);
            state.keepTiers[0]=true; // A later UI setting does not change the committed batch decision.
            var envelope=new GameplaySave {gold=73,hammers=978,successfulForges=22,forge=JsonUtility.ToJson(state)};
            var restoredEnvelope=JsonUtility.FromJson<GameplaySave>(JsonUtility.ToJson(envelope));
            var restored=JsonUtility.FromJson<ForgeState>(restoredEnvelope.forge);restored.NormalizeAfterLoad();
            Assert.AreEqual(expectedSale,restored.SettleAutoBatch(ref restoredEnvelope.gold));
            Assert.AreEqual(73+expectedSale,restoredEnvelope.gold);
            Assert.AreEqual(11,restored.pending.Count);
            Assert.IsTrue(restored.pending.Any(item=>item.id==previous.id));
            Assert.AreEqual(equipped.id,restored.equipped[(int)equipped.part].id);
            Assert.AreEqual(978,restoredEnvelope.hammers);Assert.AreEqual(22,restoredEnvelope.successfulForges);
            Assert.AreEqual(state.draws,restored.draws);
            Assert.AreEqual(0,restored.SettleAutoBatch(ref restoredEnvelope.gold));
            restoredEnvelope.forge=JsonUtility.ToJson(restored);
            var secondEnvelope=JsonUtility.FromJson<GameplaySave>(JsonUtility.ToJson(restoredEnvelope));
            var second=JsonUtility.FromJson<ForgeState>(secondEnvelope.forge);second.NormalizeAfterLoad();
            Assert.AreEqual(0,second.SettleAutoBatch(ref secondEnvelope.gold));
            Assert.AreEqual(73+expectedSale,secondEnvelope.gold);Assert.AreEqual(11,second.pending.Count);
            Assert.AreEqual(0,second.automaticSaleIds.Count);
        }

    }
}
