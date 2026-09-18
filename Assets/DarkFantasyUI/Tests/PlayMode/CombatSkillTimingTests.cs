using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonlit.UI.Tests
{
    public sealed class CombatSkillTimingTests
    {
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [TestCase(1, "Weak", "Primitive stone crescent")]
        [TestCase(2, "Strong", "Primitive falling boulder")]
        public void AttackLaunchesBeforeDamageAndAnimatorHitsOnlyAtArrival(int variant, string state, string effectName)
        {
            var catalog = Resources.Load<BattleAssetCatalog>("Moonlit/Combat/BattleAssets");
            Assert.IsNotNull(catalog, "Cloud asset preparation is required.");
            var root = new GameObject("Skill impact timing fixture", typeof(RectTransform));
            var uiAssets = ScriptableObject.CreateInstance<MainScreenAssets>();
            GameObject stageRoot = null;
            try
            {
                var main = root.AddComponent<MainScreen>(); main.enabled = false;
                main.design = root.transform;
                ((RectTransform)root.transform).sizeDelta = new Vector2(1080, 1920);
                uiAssets.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); main.font = uiAssets.font;
                var runtime = root.AddComponent<BattleRuntime>(); runtime.Initialize(main, uiAssets);
                runtime.StopAllCoroutines();
                stageRoot = (GameObject)typeof(BattleRuntime).GetField("stageRoot", PrivateInstance).GetValue(runtime);
                var player = new CombatActorState(new CombatStats { health = 100, attack = 99999 });
                var target = new CombatActorState(new CombatStats { health = 100 });
                typeof(BattleRuntime).GetProperty("PlayerState").SetValue(runtime, player);
                typeof(BattleRuntime).GetProperty("EnemyState").SetValue(runtime, target);
                var animator = (Animator)typeof(BattleRuntime).GetField("playerAnimator", PrivateInstance).GetValue(runtime);
                var relay = (CombatAnimationRelay)typeof(BattleRuntime).GetField("playerRelay", PrivateInstance).GetValue(runtime);
                var effects = (PrimitiveSkillEffects)typeof(BattleRuntime).GetField("effects", PrivateInstance).GetValue(runtime);
                // Step the real imported Animator explicitly, avoiding wall-clock assumptions in slow cloud frames.
                var strike = (IEnumerator)typeof(BattleRuntime).GetMethod("Strike", PrivateInstance)
                    .Invoke(runtime, new object[] { true, 20d, true, variant });
                Assert.IsTrue(strike.MoveNext());
                Assert.IsNotNull(effects.transform.Find(effectName), "The projectile must launch at motion start, before the damage event.");
                Assert.AreEqual(100, target.Health);
                var motion = (IEnumerator)strike.Current;
                Assert.IsTrue(motion.MoveNext());
                Assert.IsTrue(relay.Pending);
                animator.Update(0);
                Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName(state));
                float eventTime=AuthoredAnimationTestSupport.ImpactTime(animator,state,variant+1);
                animator.Update(eventTime - .01f);
                Assert.AreEqual(100, target.Health, "No damage while the attack projectile is still in flight.");
                Assert.IsTrue(relay.Pending);
                AuthoredAnimationTestSupport.SeekEffects(runtime,eventTime);
                animator.Update(.02f);
                Assert.AreEqual(100, target.Health, "The authored event only authorizes the skill while the ring or lob is still preparing.");
                var combo = (CombatComboSequence)typeof(BattleRuntime).GetField("activeCombo", PrivateInstance).GetValue(runtime);
                float delay = SkillChoreography.HitTimes(0, variant)[0] - eventTime;
                combo.Advance(delay - .01f);
                Assert.AreEqual(100, target.Health);
                combo.Advance(.011f);
                double afterFirstHit = 100 - 20d / SkillChoreography.HitTimes(0, variant).Length;
                Assert.AreEqual(afterFirstHit, target.Health, .000001, "Only the authored contact resolves its first portion.");
                Assert.IsFalse(relay.Pending);
                relay.OnCombatImpact(variant + 1);
                Assert.AreEqual(afterFirstHit, target.Health, .000001, "Duplicate impact callbacks cannot repeat damage.");
                Assert.AreEqual(0, runtime.PlayerResolvedBasicAttacks);
                var authoredWait=(IEnumerator)motion.Current;
                Assert.IsTrue(authoredWait.MoveNext());
                animator.Update(AuthoredAnimationTestSupport.Clip(animator,state).length);
                Assert.IsFalse(authoredWait.MoveNext());
                if (combo.Pending) Assert.IsTrue(motion.MoveNext(), "The action waits for its remaining combo impacts.");
                combo.Advance(10);
                Assert.AreEqual(80, target.Health, .000001, "The complete combo retains its supplied fixed total.");
                Assert.IsFalse(motion.MoveNext());
                Assert.IsFalse(strike.MoveNext());

                Assert.AreSame(catalog.playerPrefab.GetComponent<Animator>().runtimeAnimatorController, animator.runtimeAnimatorController,
                    "The actual supplied prefab controller, with its authored timing, drives combat.");
                Assert.AreSame(animator.runtimeAnimatorController,catalog.controller);

            }
            finally
            {
                if (stageRoot) UnityEngine.Object.DestroyImmediate(stageRoot);
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(uiAssets);
            }
        }
        [UnityTest]
        public IEnumerator AbsoluteCaptureSampleWaitsForRealEffectPoseAndFoodVanishesBeforeAura()
        {
            var root=new GameObject("Absolute skill sample regression");
            var effects=root.AddComponent<PrimitiveSkillEffects>();
            effects.Initialize(Resources.Load<BattleAssetCatalog>("Moonlit/Combat/BattleAssets"));
            var source=new Vector3(-2,0,0);var target=new Vector3(2,0,0);
            try {
                for(int tier=0;tier<2;tier++)for(int variant=0;variant<3;variant++) {
                    effects.enabled=false;effects.enabled=true;yield return null;
                    effects.Play(tier,variant,source,target,null,null,false);
                    float sample=variant==0?1.24f:variant==1?.55f:.75f;
                    effects.EarlyPlaybackTimeOverride=sample;
                    Assert.AreNotEqual(sample,effects.PlaybackElapsed,
                        "Setting a requested capture time must not be mistaken for the coroutine having sampled it.");
                    int frames=0;
                    while(Mathf.Abs(effects.PlaybackElapsed-sample)>.001f&&frames++<10)yield return null;
                    Assert.AreEqual(sample,effects.PlaybackElapsed,.001f);
                    var sprites=effects.GetComponentsInChildren<SpriteRenderer>();
                    if(variant==0) {
                        Assert.IsFalse(sprites.Single(r=>r.name=="Overhead food").enabled);
                        Assert.IsTrue(sprites.Any(r=>r.name=="Healing body glow"&&r.enabled&&r.color.a>.02f),
                            "A heal snapshot must contain the green body aura, not time-zero food.");
                    } else {
                        string name=variant==1?(tier==0?"Orbit bone 0":"Curved arrow 0"):tier==0?"Single rock":"Single sword";
                        var sprite=sprites.Single(r=>r.name==name);
                        var pose=variant==1?(tier==0?SixSkillChoreography.Bone(source,target,0,sample):SixSkillChoreography.Arrow(source,target,0,sample)):
                            tier==0?SixSkillChoreography.Rock(source,target,sample):SixSkillChoreography.Sword(source,target,sample);
                        Assert.Less(Vector3.Distance(sprite.transform.position,pose.position),.005f);
                        Assert.Less(Mathf.Abs(Mathf.DeltaAngle(sprite.transform.eulerAngles.z,pose.rotation)),.1f);
                        if(tier==0&&variant==1) {
                            var opposite=sprites.Single(r=>r.name=="Orbit bone 4");
                            Assert.Greater(Vector3.Distance(sprite.transform.position,opposite.transform.position),2.4f,
                                "Eight bones must form a ring, never remain collapsed at the launch point.");
                        }
                    }
                }
            } finally {UnityEngine.Object.DestroyImmediate(root);}
        }

    }
}
