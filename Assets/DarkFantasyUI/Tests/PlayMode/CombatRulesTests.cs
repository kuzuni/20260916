using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonlit.UI.Tests
{
    public sealed class CombatRulesTests
    {
        [Test]
        public void InitiativeUsesSpeedAndExactHalfTieBoundary()
        {
            Assert.IsTrue(CombatRules.PlayerFirst(2, 1, .99));
            Assert.IsFalse(CombatRules.PlayerFirst(1, 2, 0));
            Assert.IsTrue(CombatRules.PlayerFirst(10, 10, .499999));
            Assert.IsFalse(CombatRules.PlayerFirst(10, 10, .5));
        }
        [Test]
        public void RoundFifteenAllowsKillingBlowButAliveEnemyLoses()
        {
            Assert.IsFalse(CombatRules.RoundLimitLost(14, true));
            Assert.IsTrue(CombatRules.RoundLimitLost(15, true));
            Assert.IsFalse(CombatRules.RoundLimitLost(15, false));
            Assert.AreEqual(1, CombatRules.StageAfter(1, false));
            Assert.AreEqual(9, CombatRules.StageAfter(10, false));
            Assert.AreEqual(11, CombatRules.StageAfter(10, true));
            Assert.AreEqual(3, CombatRules.WavesPerStage);
        }
        [Test]
        public void DoubleChanceCannotRecurseAndSkillsBracketBasicsOnCadence()
        {
            var skills = new List<CombatSkill> {
                new CombatSkill { variant=0, cooldown=3 },
                new CombatSkill { variant=1, cooldown=2 },
                new CombatSkill { variant=2, cooldown=5 }
            };
            var actor = new CombatActorState(new CombatStats { doubleChance=100 }, skills);
            for (int turn=1; turn<=30; turn++)
            {
                var actions = CombatRules.PlanTurn(actor, () => 0);
                Assert.AreEqual(2, actions.Count(a => a.IsBasic), "turn "+turn);
                Assert.AreEqual(turn%3==0 ? 1 : 0, actions.Count(a => a.skill?.variant==0));
                Assert.AreEqual(turn%2==0 ? 1 : 0, actions.Count(a => a.skill?.variant==1));
                Assert.AreEqual(turn%5==0 ? 1 : 0, actions.Count(a => a.skill?.variant==2));
                if (turn%3==0) Assert.AreEqual(0, actions[0].skill.variant);
                var firstBasic = actions.FindIndex(a => a.IsBasic);
                Assert.IsTrue(actions[firstBasic+1].IsBasic);
                Assert.IsTrue(actions.Skip(firstBasic+2).All(a => a.skill.variant>0));
            }
        }
        [Test]
        public void LifestealUsesActualDamageAndRegenerationUsesMaximumHealth()
        {
            var attacker = new CombatActorState(new CombatStats { health=100, lifeSteal=50, regeneration=3 });
            attacker.Damage(90);
            var target = new CombatActorState(new CombatStats { health=12 });
            var hit = CombatRules.Strike(attacker, target, 1000, false, () => .99);
            Assert.AreEqual(12, hit.damage);
            Assert.AreEqual(6, hit.healing);
            Assert.AreEqual(16, attacker.Health);
            attacker.Regenerate();
            Assert.AreEqual(19, attacker.Health);
            Assert.IsFalse(target.Alive);
        }
        [Test]
        public void DodgeHasNoDamageOrLifestealAndFixedSkillDoesNotUseBasicAttack()
        {
            var attacker = new CombatActorState(new CombatStats { attack=99999, skillDamage=15, criticalChance=100 });
            var target = new CombatActorState(new CombatStats { health=1000, dodge=100 });
            Assert.IsTrue(CombatRules.Strike(attacker,target,20,true,()=>0).evaded);
            Assert.AreEqual(1000, target.Health);
            target.stats.dodge=0;
            var hit = CombatRules.Strike(attacker,target,20,true,()=>.99);
            Assert.AreEqual(23, hit.damage, 1e-9);
            Assert.IsFalse(hit.critical);
        }
        [Test]
        public void AnimationGateConsumesOnceAndCancelRejectsLateEvents()
        {
            int hits=0;
            var gate=new CombatEventGate();
            gate.Arm(()=>hits++);
            Assert.IsTrue(gate.Consume());
            Assert.IsFalse(gate.Consume());
            gate.Arm(()=>hits++); gate.Cancel();
            Assert.IsFalse(gate.Consume());
            Assert.AreEqual(1,hits);
        }
        [UnityTest]
        public IEnumerator GeneratedAnimatorActuallyDeliversOnlyItsArmedImpact()
        {
            var assets=Resources.Load<BattleAssetCatalog>("Moonlit/Combat/BattleAssets");
            Assert.IsNotNull(assets,"Run CombatAssetBuilder.Build in cloud before PlayMode tests.");
            Assert.IsNotNull(assets.controller);
            var actor=new GameObject("Combat event integration actor");
            new GameObject("Motion").transform.SetParent(actor.transform,false);
            var animator=actor.AddComponent<Animator>();
            animator.runtimeAnimatorController=assets.controller;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var relay=actor.AddComponent<CombatAnimationRelay>();
            int hits=0;
            relay.Arm(0,()=>hits++);
            animator.Play("Basic",0,0);
            yield return new WaitForSeconds(.85f);
            Assert.AreEqual(1,hits,"Damage must originate from the clip event.");
            Assert.IsFalse(relay.Pending);
            relay.OnCombatImpact(0);
            Assert.AreEqual(1,hits,"Duplicate events must not duplicate damage.");
            relay.Arm(2,()=>hits++);
            relay.OnCombatImpact(0);
            Assert.IsTrue(relay.Pending,"A previous clip kind must not consume the next action.");
            animator.Play("Weak",0,0);
            yield return new WaitForSeconds(.85f);
            Assert.AreEqual(2,hits);
            UnityEngine.Object.Destroy(actor);
        }
        [UnityTest]
        public IEnumerator ActualBattleKillingBlowSkipsDoubleAttackAndDeadEnemyTurn()
        {
            var catalog=Resources.Load<BattleAssetCatalog>("Moonlit/Combat/BattleAssets");
            Assert.IsNotNull(catalog,"Cloud asset preparation is required.");
            var previousForge=ForgeState.Current;
            var previousCollections=CollectionProgression.Data;
            var root=new GameObject("Isolated battle integration",typeof(RectTransform));
            var mainAssets=ScriptableObject.CreateInstance<MainScreenAssets>();
            try
            {
                ForgeState.Current=new ForgeState();
                CollectionProgression.Data=CollectionProgression.Create();
                ForgeState.Current.equipped[(int)EquipmentPart.Weapon]=new EquipmentRoll {
                    id=1, tier=0, level=100, part=EquipmentPart.Weapon,
                    affixes=new[] { new EquipmentAffix {kind=EquipmentAffixKind.DoubleChance,percent=100} }
                };
                ForgeState.Current.equipped[(int)EquipmentPart.Armor]=new EquipmentRoll {
                    id=2, tier=0, level=100, part=EquipmentPart.Armor
                };
                var main=root.AddComponent<MainScreen>(); main.enabled=false;
                main.design=root.transform;
                ((RectTransform)root.transform).sizeDelta=new Vector2(1080,1920);
                mainAssets.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); main.font=mainAssets.font;
                var runtime=root.AddComponent<BattleRuntime>(); runtime.Initialize(main,mainAssets);
                runtime.StopAllCoroutines();
                var method=typeof(BattleRuntime).GetMethod("FightStage",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                int callbacks=0; bool victory=false;
                Action<bool> result=won=>{callbacks++;victory=won;};
                var encounter=(IEnumerator)method.Invoke(runtime,new object[]{1,1,result});
                yield return runtime.StartCoroutine(encounter);
                Assert.AreEqual(1,callbacks);
                Assert.IsTrue(victory);
                Assert.AreEqual(1,runtime.Round);
                Assert.AreEqual(0,runtime.EnemyState.Health);
                Assert.AreEqual(1,runtime.PlayerResolvedBasicAttacks,"100% double chance cannot add a hit after a lethal basic.");
                Assert.AreEqual(0,runtime.EnemyResolvedBasicAttacks,"A dead target cannot take its scheduled turn.");
                Assert.AreEqual(0,runtime.EnemyState.Turns);
            }
            finally
            {
                ForgeState.Current=previousForge;
                CollectionProgression.Data=previousCollections;
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(mainAssets);
            }
            yield return null;
        }

        [Test]
        public void AssetsRetainOriginalPrefabSevenPartsAndOnlyThreePrimitiveVfx()
        {
            var assets=Resources.Load<BattleAssetCatalog>("Moonlit/Combat/BattleAssets");
            Assert.IsNotNull(assets);
            Assert.IsNotNull(assets.playerPrefab);
            Assert.AreEqual(30,assets.appearances.Length);
            foreach(var set in assets.appearances)
            {
                Assert.IsNotNull(set.weapon); Assert.IsNotNull(set.head); Assert.IsNotNull(set.body);
                Assert.IsNotNull(set.arm1); Assert.IsNotNull(set.arm2); Assert.IsNotNull(set.leg1); Assert.IsNotNull(set.leg2);
            }
            Assert.IsNotNull(assets.buffSprite); Assert.IsNotNull(assets.weakSprite); Assert.IsNotNull(assets.strongSprite);
        }
    }
}
