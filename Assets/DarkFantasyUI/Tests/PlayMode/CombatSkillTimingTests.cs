using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

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
                animator.Update(PrimitiveSkillEffects.AttackFlightDuration - .01f);
                Assert.AreEqual(100, target.Health, "No damage while the attack projectile is still in flight.");
                Assert.IsTrue(relay.Pending);
                animator.Update(.02f);
                Assert.AreEqual(100, target.Health, "The old .65 event must not hit while the new ring or lob is still preparing.");
                var combo = (CombatComboSequence)typeof(BattleRuntime).GetField("activeCombo", PrivateInstance).GetValue(runtime);
                float delay = SkillChoreography.HitTimes(0, variant)[0] - SkillChoreography.AttackLead;
                combo.Advance(delay - .01f);
                Assert.AreEqual(100, target.Health);
                combo.Advance(.011f);
                double afterFirstHit = 100 - 20d / SkillChoreography.HitTimes(0, variant).Length;
                Assert.AreEqual(afterFirstHit, target.Health, .000001, "Only the authored contact resolves its first portion.");
                Assert.IsFalse(relay.Pending);
                relay.OnCombatImpact(variant + 1);
                Assert.AreEqual(afterFirstHit, target.Health, .000001, "Duplicate impact callbacks cannot repeat damage.");
                Assert.AreEqual(0, runtime.PlayerResolvedBasicAttacks);
                if (combo.Pending) Assert.IsTrue(motion.MoveNext(), "The action waits for its remaining combo impacts.");
                combo.Advance(10);
                Assert.AreEqual(80, target.Health, .000001, "The complete combo retains its supplied fixed total.");
                Assert.IsFalse(motion.MoveNext());
                Assert.IsFalse(strike.MoveNext());

                var clip = catalog.controller.animationClips.Single(c => c.name == state);
                var impact = clip.events.Single(e => e.functionName == "OnCombatImpact");
                Assert.AreEqual(PrimitiveSkillEffects.AttackFlightDuration, impact.time, .0001f,
                    "The checked-in/generated clip authorizes playback; new contacts may occur later.");
                foreach (var unchanged in new[] { "Basic", "Buff" })
                {
                    var other = catalog.controller.animationClips.Single(c => c.name == unchanged);
                    Assert.AreEqual(.3f, other.events.Single(e => e.functionName == "OnCombatImpact").time, .0001f);
                }
            }
            finally
            {
                if (stageRoot) UnityEngine.Object.DestroyImmediate(stageRoot);
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(uiAssets);
            }
        }
    }
}
