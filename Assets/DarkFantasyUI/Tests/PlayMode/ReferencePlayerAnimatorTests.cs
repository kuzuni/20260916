using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace Moonlit.UI.Tests
{
    public sealed class ReferencePlayerAnimatorTests
    {
        const string Path = "Assets/Art/ChihuahuaEquipmentThemes/Reference/Player.prefab";
        GameObject instance;
        [TearDown] public void TearDown() { if (instance) Object.DestroyImmediate(instance); }

        GameObject Prefab()
        {
#if UNITY_EDITOR
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            Assert.IsNotNull(prefab);
            return prefab;
#else
            Assert.Ignore("Reference asset validation runs in hosted Editor PlayMode.");
            return null;
#endif
        }

        [Test]
        public void ReferenceVariantSerializesOwnAnimatorAndSevenNativeClipsWithoutWrapperPaths()
        {
            var prefab = Prefab();
            var animator = prefab.GetComponent<Animator>();
            Assert.IsNotNull(animator, "Animator must be on the actual reference Player prefab.");
            Assert.IsNotNull(prefab.GetComponent<CombatAnimationRelay>());
            Assert.IsTrue(animator.enabled);
            Assert.IsFalse(animator.applyRootMotion);
            Assert.IsNotNull(animator.runtimeAnimatorController);
#if UNITY_EDITOR
            Assert.AreEqual("Assets/DarkFantasyUI/Resources/Moonlit/Combat/PlayerReference/PlayerReference.controller",
                AssetDatabase.GetAssetPath(animator.runtimeAnimatorController));
            Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(prefab));
            var machine = ((AnimatorController)animator.runtimeAnimatorController).layers[0].stateMachine;
            CollectionAssert.AreEquivalent(new[] { "Idle", "Basic", "Hit", "Buff", "Weak", "Strong", "Death" },
                machine.states.Select(s => s.state.name).ToArray());
            Assert.AreEqual("Idle", machine.defaultState.name);
            foreach (var state in machine.states)
            {
                var clip = state.state.motion as AnimationClip;
                Assert.IsNotNull(clip);
                var bindings = AnimationUtility.GetCurveBindings(clip);
                Assert.IsNotEmpty(bindings);
                foreach (var binding in bindings)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(binding.path), "Keep the prefab's placement and offsets unanimated.");
                    Assert.IsFalse(binding.path.StartsWith("Motion/", StringComparison.Ordinal));
                    Assert.IsNotNull(prefab.transform.Find(binding.path), binding.path);
                    Assert.AreEqual(typeof(Transform), binding.type);
                    Assert.AreEqual("localEulerAnglesRaw.z", binding.propertyName);
                }
                int kind = clip.name == "Basic" ? 0 : clip.name == "Buff" ? 1 : clip.name == "Weak" ? 2 : clip.name == "Strong" ? 3 : -1;
                var events = AnimationUtility.GetAnimationEvents(clip);
                Assert.AreEqual(kind < 0 ? 0 : 1, events.Length);
                if (kind >= 0)
                {
                    Assert.AreEqual("OnCombatImpact", events[0].functionName);
                    Assert.AreEqual(kind, events[0].intParameter);
                    Assert.Greater(events[0].time, 0);
                    Assert.Less(events[0].time, clip.length);
                }
            }
#endif
        }

        [TestCase("Idle")]
        [TestCase("Basic")]
        [TestCase("Hit")]
        [TestCase("Buff")]
        [TestCase("Weak")]
        [TestCase("Strong")]
        [TestCase("Death")]
        public void StandaloneAnimatorActuallyMovesNativeBonesAndKeepsRootPlacement(string state)
        {
            instance = Object.Instantiate(Prefab());
            var rootPosition = instance.transform.localPosition;
            var rootRotation = instance.transform.localRotation;
            var rootScale = instance.transform.localScale;
            var animator = instance.GetComponent<Animator>();
            var bones = instance.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("bone_", StringComparison.Ordinal)).ToArray();
            animator.Rebind();
            animator.Play(state, 0, 0); animator.Update(0);
            var start = bones.Select(t => t.localRotation).ToArray();
            animator.Update(state == "Idle" ? .64f : .288f);
            Assert.IsTrue(bones.Where((t, i) => Quaternion.Angle(start[i], t.localRotation) > .5f).Any(),
                state + " must animate native bones without a BattleRuntime wrapper.");
            Assert.AreEqual(rootPosition, instance.transform.localPosition);
            Assert.Less(Quaternion.Angle(rootRotation, instance.transform.localRotation), .001f);
            Assert.AreEqual(rootScale, instance.transform.localScale);
        }

        [TestCase("Basic", 0)]
        [TestCase("Buff", 1)]
        [TestCase("Weak", 2)]
        [TestCase("Strong", 3)]
        public void StandaloneImpactEventConsumesArmedCallbackExactlyOnce(string state, int kind)
        {
            instance = Object.Instantiate(Prefab());
            var animator = instance.GetComponent<Animator>();
            var relay = instance.GetComponent<CombatAnimationRelay>();
            animator.Rebind();
            animator.Play(state, 0, 0); animator.Update(0);
            int hits = 0;
            relay.Arm(kind, () => hits++);
            animator.Update(.7f);
            Assert.AreEqual(1, hits);
            Assert.IsFalse(relay.Pending);
            animator.Update(.7f);
            Assert.AreEqual(1, hits, "Returning to Idle must not repeat impact.");
        }
    }
}
