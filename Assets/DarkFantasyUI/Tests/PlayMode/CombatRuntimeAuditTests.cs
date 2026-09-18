using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonlit.UI.Tests
{
    public sealed class CombatRuntimeAuditTests
    {
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        sealed class Fixture : IDisposable
        {
            readonly ForgeState previousForge = ForgeState.Current;
            readonly CollectionSave previousCollections;
            readonly float previousTimeScale = Time.timeScale;
            public readonly GameObject root;
            public readonly MainScreen main;
            public readonly BattleRuntime battle;
            readonly MainScreenAssets assets;

            public Fixture(float height = 1920)
            {
                previousCollections = CollectionProgression.Data;
                ForgeState.Current = new ForgeState();
                CollectionProgression.Data = CollectionProgression.Create();
                Time.timeScale = 10;
                root = new GameObject("Battle audit fixture", typeof(RectTransform));
                ((RectTransform)root.transform).sizeDelta = new Vector2(1080, height);
                main = root.AddComponent<MainScreen>(); main.enabled = false; main.design = root.transform;
                assets = ScriptableObject.CreateInstance<MainScreenAssets>();
                assets.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); main.font = assets.font;
                battle = root.AddComponent<BattleRuntime>(); battle.Initialize(main, assets);
                battle.StopAllCoroutines();
            }
            public void Dispose()
            {
                battle.StopAllCoroutines();
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(assets);
                ForgeState.Current = previousForge;
                CollectionProgression.Data = previousCollections;
                Time.timeScale = previousTimeScale;
            }
        }

        [Test]
        public void ActualPrefabSwapsArmorFivePartsHatAndWeaponThenRestores()
        {
            var catalog = Resources.Load<BattleAssetCatalog>("Moonlit/Combat/BattleAssets");
            Assert.IsNotNull(catalog, "Cloud CombatAssetBuilder must run first.");
            var rig = UnityEngine.Object.Instantiate(catalog.playerPrefab);
            try
            {
                var renderers = rig.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(r => r.sprite).ToDictionary(r => r.sprite.name, r => r);
                var original = renderers.ToDictionary(p => p.Key, p => p.Value.sprite);
                var appearance = rig.AddComponent<CombatAppearance>(); appearance.Initialize(catalog);
                var items = new EquipmentRoll[6];
                items[(int)EquipmentPart.Armor] = new EquipmentRoll { id = 101, part = EquipmentPart.Armor, tier = 9, variant = 0, level = 1 };
                items[(int)EquipmentPart.Hat] = new EquipmentRoll { id = 102, part = EquipmentPart.Hat, tier = 8, variant = 1, level = 1 };
                items[(int)EquipmentPart.Weapon] = new EquipmentRoll { id = 103, part = EquipmentPart.Weapon, tier = 7, variant = 2, level = 1 };
                appearance.Refresh(items);
                var armor = catalog.Find(9, 0);
                Assert.AreSame(armor.body, renderers["몸통"].sprite);
                Assert.AreSame(armor.arm1, renderers["팔1"].sprite); Assert.AreSame(armor.arm2, renderers["팔2"].sprite);
                Assert.AreSame(armor.leg1, renderers["다리1"].sprite); Assert.AreSame(armor.leg2, renderers["다리2"].sprite);
                Assert.AreSame(catalog.Find(8, 1).head, renderers["머리"].sprite);
                Assert.AreSame(catalog.Find(7, 2).weapon, renderers["무기"].sprite);
                appearance.Refresh(new EquipmentRoll[6]);
                foreach (var part in original) Assert.AreSame(part.Value, renderers[part.Key].sprite, part.Key);
            }
            finally { UnityEngine.Object.DestroyImmediate(rig); }
        }

        [UnityTest]
        public IEnumerator LethalAnimatorImpactThenWaveCompletionDoesNotReplayDeath()
        {
            using(var fixture = new Fixture())
            {
                ForgeState.Current.equipped[(int)EquipmentPart.Weapon] = new EquipmentRoll {
                    id=801,part=EquipmentPart.Weapon,tier=0,level=100
                };
                ForgeState.Current.equipped[(int)EquipmentPart.Necklace] = new EquipmentRoll {
                    id=802,part=EquipmentPart.Necklace,tier=0,level=100
                };
                bool completed=false, victory=false;
                var stage=(IEnumerator)typeof(BattleRuntime).GetMethod("FightStage",PrivateInstance).Invoke(
                    fixture.battle,new object[]{1,1,new Action<bool>(won=>{completed=true;victory=won;})});
                // Step actual production iterators explicitly so a slow render frame cannot skip the restart.
                Assert.IsTrue(stage.MoveNext()); // entrance
                Assert.IsTrue(stage.MoveNext()); // faster player's turn
                var turn=(IEnumerator)stage.Current; Assert.IsTrue(turn.MoveNext());
                var strike=(IEnumerator)turn.Current; Assert.IsTrue(strike.MoveNext());
                var action=(IEnumerator)strike.Current; Assert.IsTrue(action.MoveNext());
                var playerAnimator=(Animator)typeof(BattleRuntime).GetField("playerAnimator",PrivateInstance).GetValue(fixture.battle);
                var enemyAnimator=(Animator)typeof(BattleRuntime).GetField("enemyAnimator",PrivateInstance).GetValue(fixture.battle);
                playerAnimator.Update(0); playerAnimator.Update(.31f);
                Assert.IsFalse(fixture.battle.EnemyState.Alive,"A real Animator impact must be lethal.");
                enemyAnimator.Update(0); enemyAnimator.Update(.4f);
                var halfway=enemyAnimator.GetCurrentAnimatorStateInfo(0);
                Assert.IsTrue(halfway.IsName("Death")); Assert.Greater(halfway.normalizedTime,.4f);
                Assert.IsFalse(enemyAnimator.runtimeAnimatorController.animationClips.Single(clip=>clip.name=="Death").isLooping);
                Assert.IsFalse(action.MoveNext()); Assert.IsFalse(strike.MoveNext()); Assert.IsFalse(turn.MoveNext());
                Assert.IsTrue(stage.MoveNext()); // wave outcome's death hold
                enemyAnimator.Update(0);
                Assert.GreaterOrEqual(enemyAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime,halfway.normalizedTime-.001f,
                    "Wave completion must not rewind the death already started at the lethal hit.");
                enemyAnimator.Update(.5f);
                Assert.IsTrue(enemyAnimator.GetCurrentAnimatorStateInfo(0).IsName("Death"));
                Assert.GreaterOrEqual(enemyAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime,1);
                Assert.IsFalse(stage.MoveNext());
                Assert.IsTrue(completed);Assert.IsTrue(victory);
                Assert.AreEqual(1,fixture.battle.PlayerResolvedBasicAttacks);
                Assert.AreEqual(0,fixture.battle.EnemyResolvedBasicAttacks);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ActualBattleTimesOutAfterBothActorsCompleteRoundFifteen()
        {
            using (var fixture = new Fixture())
            {
                // Health/speed equipment can survive the enemy but adds no attack: deterministic timeout.
                ForgeState.Current.equipped[(int)EquipmentPart.Armor] = new EquipmentRoll {
                    id = 201, part = EquipmentPart.Armor, tier = 9, level = 100
                };
                int callbacks = 0; bool victory = true;
                var method = typeof(BattleRuntime).GetMethod("FightStage", PrivateInstance);
                var encounter = (IEnumerator)method.Invoke(fixture.battle, new object[] {
                    100, 1, new Action<bool>(won => { callbacks++; victory = won; })
                });
                yield return fixture.battle.StartCoroutine(encounter);
                Assert.AreEqual(1, callbacks); Assert.IsFalse(victory);
                Assert.AreEqual(15, fixture.battle.Round);
                Assert.IsTrue(fixture.battle.PlayerState.Alive); Assert.IsTrue(fixture.battle.EnemyState.Alive);
                Assert.AreEqual(15, fixture.battle.PlayerResolvedBasicAttacks);
                Assert.AreEqual(15, fixture.battle.EnemyResolvedBasicAttacks);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator DungeonWaitsForClaimExactlyOnceBeforeRestartingNormalLoop()
        {
            using (var fixture = new Fixture())
            {
                fixture.main.stage = 7;
                ForgeState.Current.equipped[(int)EquipmentPart.Weapon] = new EquipmentRoll {
                    id = 301, part = EquipmentPart.Weapon, tier = 1, level = 100
                };
                ForgeState.Current.equipped[(int)EquipmentPart.Armor] = new EquipmentRoll {
                    id = 302, part = EquipmentPart.Armor, tier = 1, level = 100
                };
                int callbacks = 0; bool victory = false;
                Assert.IsTrue(fixture.battle.StartDungeon(0, 1, 1, won => { callbacks++; victory = won; }));
                Assert.IsFalse(fixture.battle.StartDungeon(0, 1, 1, won => Assert.Fail("A rejected duplicate must not callback.")));
                var routineField = typeof(BattleRuntime).GetField("battle", PrivateInstance);
                var externalRoutine = routineField.GetValue(fixture.battle);
                float deadline = Time.realtimeSinceStartup + 15;
                while (callbacks == 0 && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.AreEqual(1, callbacks); Assert.IsTrue(victory);
                Assert.IsTrue(fixture.battle.IsExternalBattle);
                Assert.IsTrue(fixture.battle.AwaitingExternalClaim);
                Assert.AreEqual(7, fixture.main.stage, "Dungeon outcome must not mutate normal stage.");
                var finishedState = fixture.battle.EnemyState;
                yield return new WaitForSeconds(2);
                Assert.AreSame(finishedState, fixture.battle.EnemyState, "A won dungeon must remain visible until reward claim.");
                Assert.IsTrue(fixture.battle.CompleteExternalClaim());
                Assert.IsFalse(fixture.battle.CompleteExternalClaim(), "A duplicate claim cannot restart twice.");
                yield return new WaitForSeconds(1.6f);
                Assert.AreNotSame(externalRoutine, routineField.GetValue(fixture.battle), "Normal loop must restart.");
                Assert.AreEqual(1, callbacks);
                Assert.IsFalse(fixture.battle.IsExternalBattle);
                Assert.AreEqual(7, fixture.main.stage, "The first normal encounter should have resumed, not completed.");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator DoubledStrongMotionKeepsHeadHealthInsideViewAtEveryPortraitHeight()
        {
            foreach(float height in new[]{1600f,1920f,2280f})
            using(var fixture=new Fixture(height))
            {
                var animator=(Animator)typeof(BattleRuntime).GetField("playerAnimator",PrivateInstance).GetValue(fixture.battle);
                animator.Play("Strong",0,0);animator.Update(0);animator.Update(.36f);animator.speed=0;
                yield return null;
                typeof(BattleRuntime).GetMethod("LateUpdate",PrivateInstance).Invoke(fixture.battle,null);
                typeof(CombatWorldHud).GetMethod("LateUpdate",PrivateInstance).Invoke(fixture.battle.PlayerHud,null);
                var camera=(Camera)typeof(BattleRuntime).GetField("renderCamera",PrivateInstance).GetValue(fixture.battle);
                var corners=new Vector3[4];
                ((RectTransform)fixture.battle.PlayerHud.transform).GetWorldCorners(corners);
                foreach(var corner in corners)
                {
                    Vector3 projected=camera.WorldToViewportPoint(corner);
                    Assert.That(projected.x,Is.InRange(-.001f,1.001f),"World health horizontal bounds at "+height);
                    Assert.That(projected.y,Is.InRange(-.001f,1.001f),"Strong motion must not clip the doubled actor's head HUD at "+height);
                }
            }
        }

        [UnityTest]
        public IEnumerator SkillStripKeepsFeetClearAndGroundShadowsFollowOnlyHorizontalMotion()
        {
            foreach(float height in new[]{1600f,1920f,2280f})
            using(var fixture=new Fixture(height))
            {
                yield return null;
                typeof(BattleRuntime).GetMethod("LateUpdate",PrivateInstance).Invoke(fixture.battle,null);
                var camera=(Camera)typeof(BattleRuntime).GetField("renderCamera",PrivateInstance).GetValue(fixture.battle);
                var view=fixture.root.transform.Find("Live turn battle") as RectTransform;
                var actor=fixture.battle.PlayerHud.Actor;
                float bottom= -view.anchoredPosition.y+view.rect.height;
                float footY=bottom-camera.WorldToViewportPoint(actor.position).y*view.rect.height;
                Assert.LessOrEqual(footY,height-PortraitSafeArea.BottomHeight-BattleRuntime.SkillHudReservedHeight,
                    "Feet must stay above the expanded skill strip at "+height);
                Assert.AreEqual(2,actor.localScale.y,.001f,"Do not reduce the doubled actor.");
                var ground=actor.parent.Find(actor.name+" ground shadow");
                Assert.IsNotNull(ground);
                Assert.That(ground.GetComponent<MeshFilter>().sharedMesh.bounds.size.x,Is.GreaterThan(2));
                var motion=actor.Find("Motion");
                actor.GetComponent<Animator>().enabled=false;
                var feet=actor.GetComponentsInChildren<SpriteRenderer>().Where(part=>part.sprite &&
                    (part.sprite.name=="다리1" || part.sprite.name=="다리2")).ToArray();
                Assert.AreEqual(2,feet.Length,"Measure actual prefab foot renderers, not the weapon-centered actor pivot.");
                // Let the native skinning pass settle before recording the real foot plane.
                // Creation-time bounds are not the assembled/deformed PSB bounds.
                yield return null;yield return null;
                float floor=ground.position.y;
                float projectedFoot=feet.Min(part=>part.bounds.min.y)-(motion.position.y-actor.position.y)+.03f;
                Assert.AreEqual(projectedFoot,floor,.02f,"The natural LateUpdate must follow this frame's SpriteSkin bounds.");
                float previousMotionY=motion.position.y;
                motion.position+=new Vector3(.8f,.5f,0);
                yield return null;yield return null;
                Assert.AreEqual(previousMotionY+.5f,motion.position.y,.002f,"The test must retain a real half-unit jump.");
                Assert.AreEqual(feet.Average(part=>part.bounds.center.x),ground.position.x,.01f);
                Assert.AreEqual(floor,ground.position.y,.02f,"A jumping actor must leave its shadow on the ground.");
                // An asymmetric rig offset/foot pose must move the shadow even while the Motion pivot stays fixed.
                float before=ground.position.x;
                actor.Find("Motion/PlayerRig").position+=Vector3.right*.6f;
                yield return null;yield return null;
                Assert.AreEqual(feet.Average(part=>part.bounds.center.x),ground.position.x,.01f);
                Assert.Greater(ground.position.x,before+.1f);
            }
        }


        [UnityTest]
        public IEnumerator GroundShadowsTrackBothMirroredFeetThroughAttackAndEquipmentChanges()
        {
            using (var fixture = new Fixture())
            {
                ForgeState.Current.equipped[(int)EquipmentPart.Armor] = new EquipmentRoll {
                    id=991, part=EquipmentPart.Armor, tier=4, variant=1, level=1
                };
                yield return null;
                bool pivotMismatch = false;
                foreach (var actor in new[] { fixture.battle.PlayerHud.Actor, fixture.battle.EnemyHud.Actor })
                {
                    var feet = actor.GetComponentsInChildren<SpriteRenderer>().Where(part => part.sprite &&
                        (part.sprite.name == "다리1" || part.sprite.name == "다리2")).ToArray();
                    Assert.AreEqual(2, feet.Length);
                    var animator = actor.GetComponent<Animator>();
                    var motion = actor.Find("Motion");
                    var shadow = actor.parent.Find(actor.name + " ground shadow");
                    foreach (string pose in new[] { "Idle", "Basic", "Strong", "Hit" })
                    {
                        animator.speed=1; animator.Play(pose,0,0); animator.Update(0); animator.Update(.28f); animator.speed=0;
                        yield return null;
                        typeof(CombatGroundShadow).GetMethod("LateUpdate",PrivateInstance).Invoke(shadow.GetComponent<CombatGroundShadow>(),null);
                        float feetX=feet.Average(foot=>foot.bounds.center.x);
                        float ground=feet.Min(foot=>foot.bounds.min.y)-(motion.position.y-actor.position.y)+.03f;
                        pivotMismatch |= Mathf.Abs(feetX-motion.position.x)>.03f || Mathf.Abs(ground-actor.position.y-.03f)>.03f;
                        Assert.AreEqual(feetX,shadow.position.x,.02f,actor.name+" "+pose+" horizontal foot anchor");
                        Assert.AreEqual(ground,shadow.position.y,.02f,actor.name+" "+pose+" ground projection");
                    }
                }
                Assert.IsTrue(pivotMismatch,"Actual rig feet must expose the old pivot-based shadow error.");
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        public void SkillCooldownCountdownResetsOnlyWhenFoodPulsesFinish(int tier)
        {
            using(var fixture=new Fixture())
            {
                var skill=new CombatSkill {tier=tier,variant=0,cooldown=3,heal=20};
                var state=new CombatActorState(new CombatStats(),new System.Collections.Generic.List<CombatSkill>{skill});
                typeof(BattleRuntime).GetProperty("PlayerState").SetValue(fixture.battle,state);
                Assert.AreEqual(3,fixture.battle.PlayerSkillTurnsUntilReady(0));
                state.BeginTurn();Assert.AreEqual(2,fixture.battle.PlayerSkillTurnsUntilReady(0));
                state.BeginTurn();Assert.AreEqual(1,fixture.battle.PlayerSkillTurnsUntilReady(0));
                state.Damage(40);
                typeof(BattleRuntime).GetProperty("EnemyState").SetValue(fixture.battle,new CombatActorState(new CombatStats()));
                var turn=(IEnumerator)typeof(BattleRuntime).GetMethod("ActorTurn",PrivateInstance)
                    .Invoke(fixture.battle,new object[]{true});
                Assert.IsTrue(turn.MoveNext());
                Assert.AreEqual(0,fixture.battle.PlayerSkillTurnsUntilReady(0),"Buff is due while its animation is pending.");
                var buff=(IEnumerator)turn.Current; Assert.IsTrue(buff.MoveNext());
                var action=(IEnumerator)buff.Current; Assert.IsTrue(action.MoveNext());
                var animator=(Animator)typeof(BattleRuntime).GetField("playerAnimator",PrivateInstance).GetValue(fixture.battle);
                animator.Update(0);animator.Update(.31f);
                Assert.AreEqual(40,state.Health,"The early Animator event only arms healing after three food pulses.");
                var healingSequence=(CombatComboSequence)typeof(BattleRuntime).GetField("activeCombo",PrivateInstance).GetValue(fixture.battle);
                healingSequence.Advance(SkillChoreography.BuffHealTime(tier)-.3f-.01f);
                Assert.AreEqual(40,state.Health); Assert.AreEqual(0,fixture.battle.PlayerSkillActivationCount(0));
                healingSequence.Advance(.011f);
                Assert.AreEqual(60,state.Health,"The completed food pulses heal once when the green aura appears.");
                Assert.IsTrue(healingSequence.Pending,"Hold the actor until its body aura finishes.");
                healingSequence.Advance(SkillChoreography.BuffDuration(tier));
                Assert.IsFalse(healingSequence.Pending);
                var amount=fixture.battle.PlayerHud.transform.parent.Find("Player damage number").GetComponentInChildren<UnityEngine.UI.Text>();
                Assert.AreEqual("+20",amount.text);
                Assert.AreEqual(new Color(.4f,1,.55f),amount.color);
                Assert.AreEqual(3,fixture.battle.PlayerTurnCount);
                Assert.AreEqual(1,fixture.battle.PlayerSkillActivationCount(0));
                var relay=(CombatAnimationRelay)typeof(BattleRuntime).GetField("playerRelay",PrivateInstance).GetValue(fixture.battle);
                relay.OnCombatImpact(1);
                Assert.AreEqual(1,fixture.battle.PlayerSkillActivationCount(0),"Duplicate event cannot duplicate HUD activation.");
                Assert.AreEqual(3,fixture.battle.PlayerSkillTurnsUntilReady(0));
                Assert.AreEqual(-1,fixture.battle.PlayerSkillTurnsUntilReady(1));
            }
        }


        [TestCase(0)]
        [TestCase(1)]
        public void FoodBuffCannotHealOrBoostADeadOrReplacedActor(int tier)
        {
            using (var fixture = new Fixture())
            {
                var actor = new CombatActorState(new CombatStats { health = 100 });
                actor.Damage(40);
                typeof(BattleRuntime).GetProperty("PlayerState").SetValue(fixture.battle, actor);
                var buff = (IEnumerator)typeof(BattleRuntime).GetMethod("BuffAction", PrivateInstance)
                    .Invoke(fixture.battle, new object[] { true, new CombatSkill { tier = tier, variant = 0, heal = 25, attackBoost = 30 } });
                buff.MoveNext(); ((IEnumerator)buff.Current).MoveNext();
                var animator = (Animator)typeof(BattleRuntime).GetField("playerAnimator", PrivateInstance).GetValue(fixture.battle);
                animator.Update(0); animator.Update(.31f);
                var sequence = (CombatComboSequence)typeof(BattleRuntime).GetField("activeCombo", PrivateInstance).GetValue(fixture.battle);
                Assert.AreEqual(0, sequence.ResolvedHits);
                actor.Damage(100);
                sequence.Advance(10);
                Assert.IsTrue(sequence.Cancelled); Assert.AreEqual(0, sequence.ResolvedHits);
                Assert.AreEqual(0, actor.Health); Assert.AreEqual(0, actor.AttackBoost);
                var replacement = new CombatActorState(new CombatStats { health = 100 });
                typeof(BattleRuntime).GetProperty("PlayerState").SetValue(fixture.battle, replacement);
                sequence.Advance(10);
                Assert.AreEqual(100, replacement.Health); Assert.AreEqual(0, replacement.AttackBoost);
            }
        }

        [Test]
        public void WaveTransitionsPreserveHealthPositionBuffAndSkillPhaseUntilStageReset()
        {
            using (var fixture = new Fixture())
            {
                var fight = (IEnumerator)typeof(BattleRuntime).GetMethod("FightStage", PrivateInstance).Invoke(
                    fixture.battle, new object[] { 1, 3, new Action<bool>(won => Assert.IsTrue(won)) });
                Assert.IsTrue(fight.MoveNext()); // Initial entrance.
                var player = fixture.battle.PlayerState;
                var skill = new CombatSkill { tier = 0, variant = 1, cooldown = 2, damage = 15 };
                player.skills.Add(skill); player.BeginTurn(); player.BeginTurn();
                player.Damage(13); player.SetAttackBoost(7);
                typeof(BattleRuntime).GetMethod("RecordPlayerSkill", PrivateInstance).Invoke(fixture.battle, new object[] { skill });
                var home = new Vector3(-2.7f, .15f, 0);
                fixture.battle.PlayerHud.Actor.localPosition = home;
                fixture.battle.PlayerHud.SetVisible(true);
                for (int wave = 1; wave <= 3; wave++)
                {
                    Assert.IsTrue(fight.MoveNext()); // Round one, deliberately hold the actor iterator.
                    Assert.AreEqual(1, fixture.battle.Round);
                    fixture.battle.EnemyState.Damage(double.MaxValue);
                    Assert.IsTrue(fight.MoveNext()); // Death hold.
                    if (wave == 3) { Assert.IsFalse(fight.MoveNext()); break; }
                    Assert.IsTrue(fight.MoveNext()); // Only the next enemy enters.
                    Assert.AreEqual(wave + 1, fixture.battle.Wave);
                    Assert.AreSame(player, fixture.battle.PlayerState);
                    Assert.AreEqual(player.stats.health - 13, player.Health);
                    Assert.AreEqual(7, player.AttackBoost);
                    Assert.AreEqual(2, fixture.battle.PlayerTurnCount);
                    Assert.AreEqual(1, fixture.battle.PlayerSkillActivationCount(0));
                    Assert.AreEqual(2, fixture.battle.PlayerSkillTurnsUntilReady(0), "Used skill must not appear ready again between waves.");
                    var entrance = (IEnumerator)fight.Current;
                    Assert.IsTrue(entrance.MoveNext());
                    Assert.AreEqual(home, fixture.battle.PlayerHud.Actor.localPosition);
                    Assert.IsTrue(fixture.battle.PlayerHud.WorldCanvas.enabled, "The player must stay visible during the next enemy entrance.");
                }
                var nextStage = (IEnumerator)typeof(BattleRuntime).GetMethod("FightStage", PrivateInstance).Invoke(
                    fixture.battle, new object[] { 2, 3, new Action<bool>(_ => {}) });
                Assert.IsTrue(nextStage.MoveNext());
                Assert.AreNotSame(player, fixture.battle.PlayerState);
                Assert.AreEqual(fixture.battle.PlayerState.stats.health, fixture.battle.PlayerState.Health);
                Assert.AreEqual(0, fixture.battle.PlayerTurnCount);
                Assert.AreEqual(0, fixture.battle.PlayerSkillActivationCount(0));
                Assert.AreEqual(0, fixture.battle.PlayerState.AttackBoost);
            }
        }

        [TestCase(true, 0, false)]
        [TestCase(false, 0, false)]
        [TestCase(true, 1, false)]
        [TestCase(true, 2, false)]
        [TestCase(true, 1, true)]
        [TestCase(true, 0, true)]
        public void AnimatorImpactAloneFlashesVictimAndSeparatesBasicDustFromStoneSkills(bool isPlayer, int variant, bool evade)
        {
            using (var fixture = new Fixture())
            {
                var attacker = new CombatActorState(new CombatStats { health = 100 });
                var victim = new CombatActorState(new CombatStats { health = 100, dodge = evade ? 100 : 0 });
                typeof(BattleRuntime).GetProperty("PlayerState").SetValue(fixture.battle, isPlayer ? attacker : victim);
                typeof(BattleRuntime).GetProperty("EnemyState").SetValue(fixture.battle, isPlayer ? victim : attacker);
                var victimActor = isPlayer ? fixture.battle.EnemyHud.Actor : fixture.battle.PlayerHud.Actor;
                var flash = victimActor.GetComponent<CombatHitFlash>();
                var animator = (Animator)typeof(BattleRuntime).GetField(isPlayer ? "playerAnimator" : "enemyAnimator", PrivateInstance).GetValue(fixture.battle);
                var relay = (CombatAnimationRelay)typeof(BattleRuntime).GetField(isPlayer ? "playerRelay" : "enemyRelay", PrivateInstance).GetValue(fixture.battle);
                var effects = (PrimitiveSkillEffects)typeof(BattleRuntime).GetField("effects", PrivateInstance).GetValue(fixture.battle);
                var strike = (IEnumerator)typeof(BattleRuntime).GetMethod("Strike", PrivateInstance)
                    .Invoke(fixture.battle, new object[] { isPlayer, 10d, variant > 0, variant });
                Assert.IsTrue(strike.MoveNext());
                var action = (IEnumerator)strike.Current; Assert.IsTrue(action.MoveNext());
                Assert.IsFalse(flash.IsFlashing);
                Assert.AreEqual(0, effects.GetComponentsInChildren<ParticleSystem>().Count(x => x.name == "Basic hit dust puff"));
                Assert.IsFalse(effects.GetComponentsInChildren<SpriteRenderer>().Any(x => x.name == "Basic sword slash"));
                float impactTime = variant == 0 ? .3f : PrimitiveSkillEffects.AttackFlightDuration;
                animator.Update(0); animator.Update(impactTime - .01f);
                Assert.AreEqual(100, victim.Health); Assert.IsFalse(flash.IsFlashing);
                animator.Update(.02f);
                if (variant > 0)
                {
                    Assert.AreEqual(100, victim.Health, "The early event must not hit during ring formation or the lob.");
                    var sequence = (CombatComboSequence)typeof(BattleRuntime).GetField("activeCombo", PrivateInstance).GetValue(fixture.battle);
                    sequence.Advance(SkillChoreography.HitTimes(0, variant)[0] - SkillChoreography.AttackLead + .000001f);
                }
                double afterFirstHit = evade ? 100 : 100 - (variant == 0 ? 10 : 10d / SkillChoreography.HitTimes(0, variant).Length);
                Assert.AreEqual(afterFirstHit, victim.Health, .000001);
                Assert.AreEqual(!evade, flash.IsFlashing);
                var fragments = effects.GetComponentsInChildren<ParticleSystem>();
                Assert.AreEqual(evade ? 0 : 1, fragments.Count(x => x.name == "Basic hit dust puff"));
                var arcs = effects.GetComponentsInChildren<SpriteRenderer>().Where(x => x.name == "Basic sword slash").ToArray();
                Assert.AreEqual(variant == 0 ? 1 : 0, arcs.Length);
                if (!evade)
                {
                    foreach (var sprite in victimActor.GetComponentsInChildren<SpriteRenderer>())
                        Assert.AreEqual("Moonlit/Combat/HitWhite", sprite.sharedMaterial.shader.name);
                    var dust = fragments.Single(x => x.name == "Basic hit dust puff");
                    var dustArt = Resources.Load<Sprite>(PrimitiveSkillEffects.HitDustKey);
                    Assert.IsNotNull(dustArt, "Dedicated transparent dust art must be bundled.");
                    Assert.AreSame(dustArt, dust.textureSheetAnimation.GetSprite(0));
                    Assert.AreSame(dustArt.texture, dust.GetComponent<ParticleSystemRenderer>().sharedMaterial.mainTexture);
                    Assert.IsTrue(dust.colorOverLifetime.enabled);
                    Assert.Greater(dust.sizeOverLifetime.size.curve.Evaluate(1), dust.sizeOverLifetime.size.curve.Evaluate(0),
                        "Dust expands and fades instead of falling as stone fragments.");
                    if (variant == 0)
                    {
                        Assert.AreSame(Resources.Load<Sprite>(PrimitiveSkillEffects.BasicSlashKey), arcs[0].sprite);
                        Assert.AreEqual(isPlayer ? 1 : -1, Mathf.Sign(arcs[0].transform.localScale.x), "Mirror the actual slash for the enemy.");
                        Assert.Greater(arcs[0].bounds.size.y, 2.4f, "The basic arc must remain readable beside doubled actors.");
                        Assert.IsFalse(fragments.Any(x => x.name.Contains("illustrated impact fragments")));
                    }

                }
                relay.OnCombatImpact(variant == 0 ? 0 : variant + 1);
                Assert.AreEqual(afterFirstHit, victim.Health, .000001);
                Assert.AreEqual(fragments.Length, effects.GetComponentsInChildren<ParticleSystem>().Length, "Duplicate events must not spawn another impact.");
                Assert.AreEqual(arcs.Length, effects.GetComponentsInChildren<SpriteRenderer>().Count(x => x.name == "Basic sword slash"));
            }
        }


        static System.Collections.Generic.IEnumerable<object[]> AllAttackChoreographies()
        {
            for (int tier = 0; tier < 10; tier++)
                for (int variant = 1; variant <= 2; variant++)
                    yield return new object[] { tier, variant };
        }
        [TestCaseSource(nameof(AllAttackChoreographies))]
        public void RealAnimatorArmsAuthoredHitCountAndWaitsForEveryActualContact(int tier, int variant)
        {
            using (var fixture = new Fixture())
            {
                var skill = new CombatSkill { tier = tier, variant = variant, cooldown = variant == 1 ? 2 : 5 };
                var attacker = new CombatActorState(new CombatStats { health = 1000, lifeSteal = 30, skillDamage = 15 },
                    new System.Collections.Generic.List<CombatSkill> { skill });
                attacker.Damage(500);
                var target = new CombatActorState(new CombatStats { health = 1000 });
                typeof(BattleRuntime).GetProperty("PlayerState").SetValue(fixture.battle, attacker);
                typeof(BattleRuntime).GetProperty("EnemyState").SetValue(fixture.battle, target);
                var animator = (Animator)typeof(BattleRuntime).GetField("playerAnimator", PrivateInstance).GetValue(fixture.battle);
                var relay = (CombatAnimationRelay)typeof(BattleRuntime).GetField("playerRelay", PrivateInstance).GetValue(fixture.battle);
                double total = variant == 1 ? 101 : 97.2;
                int count = tier < 2 ? (variant == 2 ? 1 : tier == 0 ? 8 : 5) : variant == 1 ? 3 : 5;
                Assert.AreEqual(count, SkillChoreography.HitTimes(tier, variant).Length, "The supplied early-era hit counts must not silently inherit later-era defaults.");
                var strike = (IEnumerator)typeof(BattleRuntime).GetMethod("StrikeTier", PrivateInstance)
                    .Invoke(fixture.battle, new object[] { true, total, true, variant, tier });
                Assert.IsTrue(strike.MoveNext());
                var animation = (IEnumerator)strike.Current; Assert.IsTrue(animation.MoveNext());
                Assert.IsFalse(fixture.battle.IsSkillComboRunning);
                animator.Update(0); animator.Update(PrimitiveSkillEffects.AttackFlightDuration - .01f);
                Assert.AreEqual(1000, target.Health, "No damage before the real Animator event.");
                animator.Update(.02f);
                var combo = (CombatComboSequence)typeof(BattleRuntime).GetField("activeCombo", PrivateInstance).GetValue(fixture.battle);
                Assert.AreEqual(count, combo.HitCount);
                var times = SkillChoreography.HitTimes(tier, variant);
                int atEvent = times[0] <= SkillChoreography.AttackLead ? 1 : 0;
                Assert.AreEqual(atEvent, combo.ResolvedHits);
                Assert.AreEqual(1000 - total / count * 1.15 * atEvent, target.Health, .000001);
                Assert.AreEqual(1, fixture.battle.PlayerSkillActivationCount(0));
                Assert.IsTrue(animation.MoveNext(), "The action must wait beyond its clip while contacts remain.");
                float previous = SkillChoreography.AttackLead;
                for (int hit = atEvent; hit < count; hit++)
                {
                    double health = target.Health;
                    relay.OnCombatImpact(variant + 1);
                    Assert.AreEqual(health, target.Health, "A duplicate clip event cannot create another combo or hit.");
                    float gap = times[hit] - previous;
                    combo.Advance(gap * .5f);
                    Assert.AreEqual(hit, combo.ResolvedHits, "No damage during formation, flight, or the interval between contacts.");
                    combo.Advance(gap * .5f + .000001f);
                    Assert.AreEqual(hit + 1, combo.ResolvedHits);
                    previous = times[hit];
                }
                Assert.IsFalse(combo.Pending); Assert.IsFalse(combo.Cancelled);
                Assert.AreEqual(1000 - total * 1.15, target.Health, .000001, "All portions conserve the supplied fixed total.");
                Assert.AreEqual(500 + total * 1.15 * .3, attacker.Health, .000001, "Life steal uses actual per-hit damage.");
                Assert.AreEqual(1, fixture.battle.PlayerSkillActivationCount(0));
                Assert.IsFalse(animation.MoveNext(), "The next action may proceed only after the final impact.");
                Assert.IsFalse(strike.MoveNext());
                relay.OnCombatImpact(variant + 1); combo.Advance(10);
                Assert.AreEqual(count, combo.ResolvedHits);
                Assert.AreEqual(1000 - total * 1.15, target.Health, .000001);
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        public void DeathCancelsRemainingRealSkillHitsWithoutHittingTheNextWave(int variant)
        {
            using (var fixture = new Fixture())
            {
                var attacker = new CombatActorState(new CombatStats { health = 100, lifeSteal = 100 });
                attacker.Damage(50);
                var victim = new CombatActorState(new CombatStats { health = 1 });
                typeof(BattleRuntime).GetProperty("PlayerState").SetValue(fixture.battle, attacker);
                typeof(BattleRuntime).GetProperty("EnemyState").SetValue(fixture.battle, victim);
                var strike = (IEnumerator)typeof(BattleRuntime).GetMethod("Strike", PrivateInstance)
                    .Invoke(fixture.battle, new object[] { true, 100d, true, variant });
                strike.MoveNext(); var animation = (IEnumerator)strike.Current; animation.MoveNext();
                var animator = (Animator)typeof(BattleRuntime).GetField("playerAnimator", PrivateInstance).GetValue(fixture.battle);
                animator.Update(0); animator.Update(PrimitiveSkillEffects.AttackFlightDuration + .01f);
                var combo = (CombatComboSequence)typeof(BattleRuntime).GetField("activeCombo", PrivateInstance).GetValue(fixture.battle);
                combo.Advance(SkillChoreography.HitTimes(0, variant)[0] - SkillChoreography.AttackLead + .000001f);
                Assert.AreEqual(1, combo.ResolvedHits);
                Assert.AreEqual(variant == 1, combo.Cancelled, "Only an unfinished multihit sequence is cancelled; a lethal single hit has completed."); Assert.IsFalse(combo.Pending);
                Assert.AreEqual(0, victim.Health); Assert.AreEqual(51, attacker.Health, "Overkill cannot grant excess healing.");
                var nextEnemy = new CombatActorState(new CombatStats { health = 200 });
                typeof(BattleRuntime).GetProperty("EnemyState").SetValue(fixture.battle, nextEnemy);
                combo.Advance(10);
                Assert.AreEqual(200, nextEnemy.Health);
                Assert.AreEqual(1, combo.ResolvedHits);
            }
        }

        [Test]
        public void ComboCancellationAndDodgePreserveDamageAndNeverResumeStaleActions()
        {
            var attacker = new CombatActorState(new CombatStats { health = 100, lifeSteal = 100 });
            attacker.Damage(50);
            var target = new CombatActorState(new CombatStats { health = 100, dodge = 100 });
            bool valid = true; int callbacks = 0;
            var combo = new CombatComboSequence(77, new[] { 0f, .13f, .47f }, () => valid,
                (index, portion) => { callbacks++; CombatRules.Strike(attacker, target, portion, true, () => .2); });
            combo.Advance(0); combo.Advance(1);
            Assert.AreEqual(3, callbacks); Assert.AreEqual(100, target.Health); Assert.AreEqual(50, attacker.Health);
            combo = new CombatComboSequence(77, new[] { 0f, .13f, .47f }, () => valid,
                (index, portion) => callbacks++);
            combo.Advance(0); valid = false; combo.Advance(1); valid = true; combo.Advance(10);
            Assert.IsTrue(combo.Cancelled); Assert.AreEqual(4, callbacks);
        }

        [Test]
        public void DisablingBattleCancelsAnArmedLiveCombo()
        {
            using (var fixture = new Fixture())
            {
                var attacker = new CombatActorState(new CombatStats { health = 100 });
                var target = new CombatActorState(new CombatStats { health = 100 });
                typeof(BattleRuntime).GetProperty("PlayerState").SetValue(fixture.battle, attacker);
                typeof(BattleRuntime).GetProperty("EnemyState").SetValue(fixture.battle, target);
                var strike = (IEnumerator)typeof(BattleRuntime).GetMethod("Strike", PrivateInstance)
                    .Invoke(fixture.battle, new object[] { true, 30d, true, 1 });
                strike.MoveNext(); ((IEnumerator)strike.Current).MoveNext();
                var animator = (Animator)typeof(BattleRuntime).GetField("playerAnimator", PrivateInstance).GetValue(fixture.battle);
                animator.Update(0); animator.Update(PrimitiveSkillEffects.AttackFlightDuration + .01f);
                var combo = (CombatComboSequence)typeof(BattleRuntime).GetField("activeCombo", PrivateInstance).GetValue(fixture.battle);
                Assert.IsTrue(combo.Pending); Assert.AreEqual(100, target.Health, "An armed bone ring has not launched any hits yet.");
                fixture.battle.enabled = false; combo.Advance(10);
                Assert.IsTrue(combo.Cancelled); Assert.AreEqual(100, target.Health);
            }
        }

        [UnityTest]
        public IEnumerator HitFlashRestoresOriginalMaterialsAfterHoldAndOnDisable()
        {
            using (var fixture = new Fixture())
            {
                Time.timeScale = 1;
                var actor = fixture.battle.EnemyHud.Actor;
                var sprites = actor.GetComponentsInChildren<SpriteRenderer>();
                var originals = sprites.Select(sprite => sprite.sharedMaterial).ToArray();
                var flash = actor.GetComponent<CombatHitFlash>();
                flash.Play(); Assert.IsTrue(flash.IsFlashing);
                yield return null;
                yield return new WaitForSeconds(CombatHitFlash.Duration + .05f);
                Assert.IsFalse(flash.IsFlashing);
                for (int i = 0; i < sprites.Length; i++) Assert.AreSame(originals[i], sprites[i].sharedMaterial);
                flash.Play(); flash.Play(); // Refresh during another hit must retain the actual original material.
                flash.enabled = false;
                Assert.IsFalse(flash.IsFlashing);
                for (int i = 0; i < sprites.Length; i++) Assert.AreSame(originals[i], sprites[i].sharedMaterial);
            }
        }

        static Rect WorldRect(RectTransform rect)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
        }

        [UnityTest]
        public IEnumerator CompactHeadBarsAndLargeAmountsAvoidStageRoundAndWaveNodes()
        {
            bool reproducedOldOverlap=false;
            foreach(float height in new[]{1600f,1660f,1720f,1920f,2280f})
            using(var fixture=new Fixture(height))
            {
                fixture.main.stageText=Ui.Text("Stage title",fixture.root.transform,290,146,500,58,"스테이지 2",45,fixture.main.font);
                fixture.main.roundText=Ui.Text("Battle round",fixture.root.transform,290,258,500,48,"라운드 15/15",26,fixture.main.font);
                fixture.main.waveNodes=new UnityEngine.UI.Image[3];
                for(int n=0;n<3;n++) fixture.main.waveNodes[n]=Ui.Image("Wave "+n,fixture.root.transform,402+n*127,219,22,22,null,Color.cyan);
                var animator=(Animator)typeof(BattleRuntime).GetField("playerAnimator",PrivateInstance).GetValue(fixture.battle);
                animator.Play("Basic",0,0);animator.Update(0);animator.Update(.36f);animator.speed=0;
                yield return null;
                typeof(BattleRuntime).GetMethod("LateUpdate",PrivateInstance).Invoke(fixture.battle,null);
                var areas=(Rect[])typeof(BattleRuntime).GetField("protectedHudAreas",PrivateInstance).GetValue(fixture.battle);
                foreach(var hud in new[]{fixture.battle.PlayerHud,fixture.battle.EnemyHud})
                {
                    hud.SetProtectedAreas(null);
                    typeof(CombatWorldHud).GetMethod("LateUpdate",PrivateInstance).Invoke(hud,null);
                    reproducedOldOverlap |= areas.Any(area=>area.Overlaps(WorldRect((RectTransform)hud.transform)));
                    hud.SetProtectedAreas(areas);
                    typeof(CombatWorldHud).GetMethod("LateUpdate",PrivateInstance).Invoke(hud,null);
                    foreach(var area in areas)
                        Assert.IsFalse(area.Overlaps(WorldRect((RectTransform)hud.transform)),"Head HP must avoid stage text at "+height);
                    Assert.AreEqual(48,((RectTransform)hud.transform.Find("Health fill")).rect.height);
                    // Step each phase of the real number coroutine so slow cloud frames cannot hide overlap.
                    var amount=(IEnumerator)typeof(CombatWorldHud).GetMethod("FloatNumber",PrivateInstance)
                        .Invoke(hud,new object[]{"+16k",Color.green});
                    Assert.IsTrue(amount.MoveNext());
                    var number=hud.transform.parent.Find(hud==fixture.battle.PlayerHud?"Player damage number":"Enemy damage number");
                    var label=number.GetComponentInChildren<UnityEngine.UI.Text>();
                    for(int frame=0;frame<4;frame++)
                    {
                        float halfWidth=(label.preferredWidth+24)*number.localScale.x/2;
                        var rendered=new Rect(number.position.x-halfWidth,number.position.y-.8f,halfWidth*2,1.6f);
                        foreach(var area in areas)
                            Assert.IsFalse(area.Overlaps(rendered),"Large amount must avoid wave nodes at "+height);
                        yield return null;
                        if(!amount.MoveNext())break;
                    }
                    Assert.AreEqual(117,label.fontSize,"Readability fix must not shrink the requested amount size.");
                }
            }
            Assert.IsTrue(reproducedOldOverlap,"This fixture must reproduce the actual compact HUD regression before avoidance.");
        }

        [UnityTest]
        public IEnumerator AlreadyFloatingDamageRechecksProtectionWhenSafeAreaShrinks()
        {
            using(var fixture=new Fixture(2280))
            {
                Time.timeScale=1;
                fixture.main.stageText=Ui.Text("Stage title",fixture.root.transform,290,146,500,58,"스테이지 2",45,fixture.main.font);
                fixture.main.roundText=Ui.Text("Battle round",fixture.root.transform,290,258,500,48,"라운드 1/15",26,fixture.main.font);
                fixture.main.waveNodes=new UnityEngine.UI.Image[3];
                for(int n=0;n<3;n++)fixture.main.waveNodes[n]=Ui.Image("Wave "+n,fixture.root.transform,402+n*127,219,22,22,null,Color.cyan);
                var animator=(Animator)typeof(BattleRuntime).GetField("playerAnimator",PrivateInstance).GetValue(fixture.battle);
                animator.Play("Basic",0,0);animator.Update(0);animator.Update(.36f);animator.speed=0;
                yield return null;
                typeof(BattleRuntime).GetMethod("LateUpdate",PrivateInstance).Invoke(fixture.battle,null);
                typeof(CombatWorldHud).GetMethod("LateUpdate",PrivateInstance).Invoke(fixture.battle.PlayerHud,null);
                var amount=(IEnumerator)typeof(CombatWorldHud).GetMethod("FloatNumber",PrivateInstance)
                    .Invoke(fixture.battle.PlayerHud,new object[]{"5",Color.white});
                Assert.IsTrue(amount.MoveNext());
                var number=fixture.battle.PlayerHud.transform.parent.Find("Player damage number");
                var label=number.GetComponentInChildren<UnityEngine.UI.Text>();
                Vector3 before=number.position;
                // Same responsive design-height transition used by the 9:19 -> notched 9:16 capture.
                ((RectTransform)fixture.root.transform).sizeDelta=new Vector2(1080,1720);
                typeof(BattleRuntime).GetMethod("LateUpdate",PrivateInstance).Invoke(fixture.battle,null);
                var areas=(Rect[])typeof(BattleRuntime).GetField("protectedHudAreas",PrivateInstance).GetValue(fixture.battle);
                float halfWidth=Mathf.Min(760,label.preferredWidth+24)*.008f*1.2f/2;
                var oldPath=new Rect(before.x-halfWidth,before.y-.8f,halfWidth*2,2.5f);
                Assert.IsTrue(areas.Any(area=>area.Overlaps(oldPath)),"The already-spawned number must cross the new stage protection.");
                Assert.Less(number.position.x,before.x,"Resolve before rendering, without waiting for another coroutine frame.");
                Assert.AreEqual(before.y,number.position.y,.001f,"Keep the upward animation continuous.");
                var newPath=new Rect(number.position.x-halfWidth,number.position.y-.8f,halfWidth*2,2.5f);
                foreach(var area in areas)Assert.IsFalse(area.Overlaps(newPath));
                Assert.IsTrue(amount.MoveNext());
                Assert.LessOrEqual(number.position.x,before.x);
                Assert.AreEqual(117,label.fontSize);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CompactSafeAreaKeepsEntireActorAndHudInsideBattleView()
        {
            using (var fixture = new Fixture(1600))
            {
                yield return null;
                typeof(BattleRuntime).GetMethod("LateUpdate", PrivateInstance).Invoke(fixture.battle, null);
                var camera = (Camera)typeof(BattleRuntime).GetField("renderCamera", PrivateInstance).GetValue(fixture.battle);
                var actor = (GameObject)typeof(BattleRuntime).GetField("player", PrivateInstance).GetValue(fixture.battle);
                Assert.IsNotNull(camera);
                foreach (var renderer in actor.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    Assert.LessOrEqual(camera.WorldToViewportPoint(renderer.bounds.max).y, 1.001f, renderer.name + " top clipped");
                    Assert.GreaterOrEqual(camera.WorldToViewportPoint(renderer.bounds.min).y, -.001f, renderer.name + " bottom clipped");
                }
                var view = fixture.root.transform.Find("Live turn battle") as RectTransform;
                Assert.Greater(view.rect.height, 120);
                Assert.IsNull(view.Find("Last combat action"));
                Assert.IsNull(view.Find("Battle round"));
                Assert.AreEqual(2, Mathf.Abs(actor.transform.localScale.x), .001f);
                Assert.AreEqual(2, actor.transform.localScale.y, .001f);
                Assert.AreEqual(30, actor.layer);
                Assert.AreEqual(RenderMode.WorldSpace, fixture.battle.PlayerHud.WorldCanvas.renderMode);
                Assert.AreSame(camera, fixture.battle.PlayerHud.WorldCanvas.worldCamera);
                Assert.AreEqual(120 / 4f, view.rect.height / (camera.orthographicSize * 2), .01f,
                    "Expanding the viewport must preserve the old compact pixels/world-unit so doubling is not cancelled.");
            }
            yield return null;
        }
    }
}
