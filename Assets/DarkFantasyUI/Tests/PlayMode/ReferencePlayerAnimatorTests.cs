using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace Moonlit.UI.Tests
{
    internal static class AuthoredAnimationTestSupport
    {
        public static Animator ActorAnimator(Transform actor)
        {
            var rig = actor.Find("Motion/PlayerRig");
            Assert.IsNotNull(rig);
            var animator = rig.GetComponent<Animator>();
            Assert.IsNotNull(animator, "Combat must play the Animator supplied on Player.prefab.");
            Assert.IsNull(actor.GetComponent<Animator>(), "A generated wrapper must not replace the authored Animator.");
            return animator;
        }
        public static AnimationClip Clip(Animator animator, string state)
        {
#if UNITY_EDITOR
            var assigned = animator.runtimeAnimatorController;
            var overrides = assigned as AnimatorOverrideController;
            var controller = (overrides ? overrides.runtimeAnimatorController : assigned) as AnimatorController;
            Assert.IsNotNull(controller);
            var clip = controller.layers[0].stateMachine.states.Single(child => child.state.name == state).state.motion as AnimationClip;
            if (overrides && clip && overrides[clip]) clip = overrides[clip];
#else
            var clip = animator.runtimeAnimatorController.animationClips.Single(candidate => candidate.name == state);
#endif
            Assert.IsNotNull(clip);
            return clip;
        }
        public static float ImpactTime(Animator animator, string state, int kind)
            => Clip(animator, state).events.First(evt => evt.functionName == "OnCombatImpact" && evt.intParameter == kind).time;
        public static void SeekEffects(BattleRuntime battle, float time)
        {
            var effects = (PrimitiveSkillEffects)typeof(BattleRuntime).GetField("effects", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(battle);
            // Manual Animator.Update does not tick Unity's presentation coroutine. Advance both fixture clocks together.
            typeof(PrimitiveSkillEffects).GetProperty("PlaybackElapsed").SetValue(effects, time);
        }
    }

    public sealed class ReferencePlayerAnimatorTests
    {
        const string Path = "Assets/Art/ChihuahuaEquipmentThemes/Reference/Player.prefab";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject instance;
        [TearDown] public void TearDown() { if (instance) Object.DestroyImmediate(instance); }

        GameObject Prefab()
        {
#if UNITY_EDITOR
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            Assert.IsNotNull(prefab);
            return prefab;
#else
            Assert.Fail("Reference asset validation requires hosted Editor PlayMode.");
            return null;
#endif
        }

        [Test]
        public void ReferenceVariantSerializesOwnAnimatorAndSevenNativeClipsWithoutWrapperPaths()
        {
            var prefab = Prefab();
            var animator = prefab.GetComponent<Animator>();
            Assert.IsNotNull(animator);
            Assert.IsNotNull(prefab.GetComponent<CombatAnimationRelay>());
            Assert.IsNotNull(animator.runtimeAnimatorController);
#if UNITY_EDITOR
            foreach (string state in new[] { "Idle", "Basic", "Hit", "Buff", "Weak", "Strong", "Death" })
            {
                var clip = AuthoredAnimationTestSupport.Clip(animator, state);
                var bindings = AnimationUtility.GetCurveBindings(clip).Concat(AnimationUtility.GetObjectReferenceCurveBindings(clip)).ToArray();
                Assert.IsNotEmpty(bindings);
                foreach (var binding in bindings)
                {
                    var target = string.IsNullOrEmpty(binding.path) ? prefab.transform : prefab.transform.Find(binding.path);
                    Assert.IsNotNull(target, state + " / " + binding.path);
                    // Authored position, scale, every rotation axis and sprite curves are valid.
                }
                int kind = state == "Basic" ? 0 : state == "Buff" ? 1 : state == "Weak" ? 2 : state == "Strong" ? 3 : -1;
                if (kind >= 0)
                    Assert.That(AuthoredAnimationTestSupport.ImpactTime(animator, state, kind), Is.InRange(0, clip.length));
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
            var bones = instance.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("bone_", StringComparison.Ordinal)).ToArray();
            animator.Rebind(); animator.Play(state, 0, 0); animator.Update(0);
            var rotations = bones.Select(t => t.localRotation).ToArray();
            var positions = bones.Select(t => t.localPosition).ToArray();
            var scales = bones.Select(t => t.localScale).ToArray();
            animator.Update(AuthoredAnimationTestSupport.Clip(animator, state).length * .4f);
            Assert.IsTrue(bones.Where((t, i) => Quaternion.Angle(rotations[i], t.localRotation) > .1f ||
                Vector3.Distance(positions[i], t.localPosition) > .001f || Vector3.Distance(scales[i], t.localScale) > .001f).Any(),
                state + " must play the user's actual native bone curves.");
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
            var clip = AuthoredAnimationTestSupport.Clip(animator, state);
            float eventTime = AuthoredAnimationTestSupport.ImpactTime(animator, state, kind);
            animator.Rebind(); animator.Play(state, 0, 0); animator.Update(0);
            int hits = 0; relay.Arm(kind, () => hits++);
            animator.Update(Mathf.Max(0, eventTime - .01f));
            Assert.AreEqual(0, hits, "The authored contact time must not be replaced by the old generated .3/.65 timing.");
            animator.Update(.02f);
            Assert.AreEqual(1, hits); Assert.IsFalse(relay.Pending);
            animator.Update(clip.length + .1f);
            relay.OnCombatImpact(kind);
            Assert.AreEqual(1, hits, "Duplicate native events and state completion consume only one armed callback.");
        }

        [Test]
        public void CloudBuildersPreserveEveryAuthoredAnimationAndDoNotRegenerateWrapperClips()
        {
#if UNITY_EDITOR
            var prefab = Prefab();
            var animator = prefab.GetComponent<Animator>();
            string root = "Assets/DarkFantasyUI/Resources/Moonlit/Combat";
            var paths = AssetDatabase.GetDependencies(Path, true)
                .Where(path => path == Path || path.EndsWith(".anim", StringComparison.Ordinal) || path.EndsWith(".controller", StringComparison.Ordinal) || path.EndsWith(".overrideController", StringComparison.Ordinal))
                .Concat(Directory.GetFiles(root, "*.anim", SearchOption.TopDirectoryOnly))
                .Concat(Directory.GetFiles(root, "*.controller", SearchOption.TopDirectoryOnly)).Distinct().ToArray();
            var before = paths.ToDictionary(path => path, File.ReadAllBytes);
            var assigned = animator.runtimeAnimatorController;
            foreach (string builder in new[] { "ReferencePlayerAnimatorBuilder", "CombatAssetBuilder" })
            {
                var type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("Moonlit.Editor." + builder)).FirstOrDefault(t => t != null);
                Assert.IsNotNull(type);
                type.GetMethod("Build", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
            }
            foreach (var pair in before) CollectionAssert.AreEqual(pair.Value, File.ReadAllBytes(pair.Key), "Builder rewrote user/archived animation: " + pair.Key);
            Assert.AreSame(assigned, prefab.GetComponent<Animator>().runtimeAnimatorController);
            Assert.AreSame(assigned, Resources.Load<BattleAssetCatalog>("Moonlit/Combat/BattleAssets").controller);
#endif
        }

        [Test]
        public void ActualBattleBindsPrefabAnimatorAndReproducesItsAuthoredNativeBonePose()
        {
            var previousForge = ForgeState.Current; var previousCollections = CollectionProgression.Data;
            var root = new GameObject("Native authored battle fixture", typeof(RectTransform));
            var assets = ScriptableObject.CreateInstance<MainScreenAssets>();
            try
            {
                ForgeState.Current = new ForgeState(); CollectionProgression.Data = CollectionProgression.Create();
                ((RectTransform)root.transform).sizeDelta = new Vector2(1080, 2280);
                var main = root.AddComponent<MainScreen>(); main.enabled = false; main.design = root.transform;
                assets.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); main.font = assets.font;
                var battle = root.AddComponent<BattleRuntime>(); battle.Initialize(main, assets); battle.StopAllCoroutines();
                instance = Object.Instantiate(Prefab());
                var standalone = instance.GetComponent<Animator>();
                foreach (var hud in new[] { battle.PlayerHud, battle.EnemyHud })
                {
                    var animator = AuthoredAnimationTestSupport.ActorAnimator(hud.Actor);
                    Assert.AreSame(standalone.runtimeAnimatorController, animator.runtimeAnimatorController);
                    string prefix = hud == battle.PlayerHud ? "player" : "enemy";
                    Assert.AreSame(animator, typeof(BattleRuntime).GetField(prefix + "Animator", Private).GetValue(battle));
                    Assert.AreSame(animator.GetComponent<CombatAnimationRelay>(), typeof(BattleRuntime).GetField(prefix + "Relay", Private).GetValue(battle));
                    foreach (string state in new[] { "Idle", "Basic", "Hit", "Buff", "Weak", "Strong", "Death" })
                    {
                        foreach (var target in new[] { standalone, animator })
                        {
                            target.Rebind(); target.Play(state, 0, 0); target.Update(0);
                            target.Update(AuthoredAnimationTestSupport.Clip(target, state).length * .4f);
                        }
                        foreach (var bone in instance.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("bone_", StringComparison.Ordinal)))
                        {
#if UNITY_EDITOR
                            var actual = animator.transform.Find(AnimationUtility.CalculateTransformPath(bone, instance.transform));
                            Assert.IsNotNull(actual);
                            Assert.Less(Vector3.Distance(bone.localPosition, actual.localPosition), .0001f, state + " " + bone.name + " position");
                            Assert.Less(Quaternion.Angle(bone.localRotation, actual.localRotation), .05f, state + " " + bone.name + " rotation");
                            Assert.Less(Vector3.Distance(bone.localScale, actual.localScale), .0001f, state + " " + bone.name + " scale");
#endif
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(root); Object.DestroyImmediate(assets);
                ForgeState.Current = previousForge; CollectionProgression.Data = previousCollections;
            }
        }

        [Test]
        public void ActualBattleWaitsForLongAuthoredClipAndItsLaterEventInsteadOfOldPointEightSeconds()
        {
#if UNITY_EDITOR
            var previousForge = ForgeState.Current; var previousCollections = CollectionProgression.Data;
            var root = new GameObject("Long native action fixture", typeof(RectTransform));
            var assets = ScriptableObject.CreateInstance<MainScreenAssets>();
            AnimationClip longer = null; AnimatorOverrideController overridden = null;
            try
            {
                ForgeState.Current = new ForgeState(); CollectionProgression.Data = CollectionProgression.Create();
                ((RectTransform)root.transform).sizeDelta = new Vector2(1080, 2280);
                var main = root.AddComponent<MainScreen>(); main.enabled = false; main.design = root.transform;
                assets.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); main.font = assets.font;
                var battle = root.AddComponent<BattleRuntime>(); battle.Initialize(main, assets); battle.StopAllCoroutines();
                var animator = AuthoredAnimationTestSupport.ActorAnimator(battle.PlayerHud.Actor);
                var basic = AuthoredAnimationTestSupport.Clip(animator, "Basic");
                longer = Object.Instantiate(basic); longer.name = "Long authored test copy";
                var binding = AnimationUtility.GetCurveBindings(longer).First(b => b.type == typeof(Transform));
                var curve = AnimationUtility.GetEditorCurve(longer, binding);
                curve.AddKey(1.6f, curve.Evaluate(curve.keys.Last().time));
                AnimationUtility.SetEditorCurve(longer, binding, curve);
                var settings = AnimationUtility.GetAnimationClipSettings(longer); settings.stopTime = 1.6f;
                AnimationUtility.SetAnimationClipSettings(longer, settings);
                AnimationUtility.SetAnimationEvents(longer, new[] { new AnimationEvent { time = 1.1f, functionName = "OnCombatImpact", intParameter = 0 } });
                overridden = new AnimatorOverrideController(animator.runtimeAnimatorController); overridden[basic] = longer;
                animator.runtimeAnimatorController = overridden; animator.Rebind();
                int contacts = 0;
                var action = (IEnumerator)typeof(BattleRuntime).GetMethod("AnimatedAction", Private).Invoke(battle,
                    new object[] { true, 0, "Basic", new Action(() => contacts++), null });
                Assert.IsTrue(action.MoveNext());
                var wait = action.Current as IEnumerator; Assert.IsNotNull(wait);
                Assert.IsTrue(wait.MoveNext());
                animator.Update(0); animator.Update(.85f);
                Assert.AreEqual(0, contacts); Assert.IsTrue(wait.MoveNext(), "The old .8 second boundary must not end this authored clip.");
                animator.Update(.26f);
                Assert.AreEqual(1, contacts); Assert.IsTrue(wait.MoveNext(), "Contact does not cut off the remainder of the authored pose.");
                animator.Update(.6f);
                Assert.IsFalse(wait.MoveNext());
                Assert.IsFalse(action.MoveNext());
                Assert.AreEqual(1, contacts);
                Assert.AreSame(overridden, animator.runtimeAnimatorController);
            }
            finally
            {
                Object.DestroyImmediate(root); Object.DestroyImmediate(assets);
                if (overridden) Object.DestroyImmediate(overridden); if (longer) Object.DestroyImmediate(longer);
                ForgeState.Current = previousForge; CollectionProgression.Data = previousCollections;
            }
#endif
        }
    }
}
