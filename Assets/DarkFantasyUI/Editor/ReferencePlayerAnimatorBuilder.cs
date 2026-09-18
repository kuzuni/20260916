using System;
using System.IO;
using System.Linq;
using Moonlit.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Moonlit.Editor
{
    // Explicit cloud build only. This edits the variant, never the source PSB hierarchy.
    public static class ReferencePlayerAnimatorBuilder
    {
        public const string PrefabPath = "Assets/Art/ChihuahuaEquipmentThemes/Reference/Player.prefab";
        public const string Root = "Assets/DarkFantasyUI/Resources/Moonlit/Combat/PlayerReference";
        public const string ControllerPath = Root + "/PlayerReference.controller";
        static readonly string[] Names = { "Idle", "Basic", "Hit", "Buff", "Weak", "Strong", "Death" };

        public static void Build()
        {
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var bones = contents.GetComponentsInChildren<Transform>(true)
                    .Where(t => t.name.StartsWith("bone_", StringComparison.Ordinal)).ToArray();
                if (!bones.Any(t => t.name == "bone_7") || !bones.Any(t => t.name == "bone_2"))
                    throw new InvalidOperationException("Reference Player must retain its native arm and body bones.");
                var rootBone = bones.FirstOrDefault(t => !t.parent || !t.parent.name.StartsWith("bone_", StringComparison.Ordinal));
                if (!rootBone) throw new InvalidOperationException("Reference Player root bone is missing.");

                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
                if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
                var machine = controller.layers[0].stateMachine;
                foreach (var child in machine.states) machine.RemoveState(child.state);
                AnimatorState idle = null;
                foreach (string name in Names)
                {
                    string path = Root + "/" + name + ".anim";
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
                    clip.ClearCurves(); clip.name = name; clip.frameRate = 30;
                    float duration = name == "Idle" ? 1.6f : .72f;
                    foreach (var bone in bones)
                    {
                        float swing = Swing(name, bone.name);
                        float end = 0;
                        if (name == "Death" && bone == rootBone) { swing = -55; end = -85; }
                        string nativePath = AnimationUtility.CalculateTransformPath(bone, contents.transform);
                        float angle = bone.localEulerAngles.z;
                        AnimationUtility.SetEditorCurve(clip,
                            EditorCurveBinding.FloatCurve(nativePath, typeof(Transform), "localEulerAnglesRaw.z"),
                            new AnimationCurve(new Keyframe(0, angle),
                                new Keyframe(duration * .4f, angle + swing), new Keyframe(duration, angle + end)));
                    }
                    var settings = AnimationUtility.GetAnimationClipSettings(clip);
                    settings.loopTime = name == "Idle";
                    AnimationUtility.SetAnimationClipSettings(clip, settings);
                    int kind = name == "Basic" ? 0 : name == "Buff" ? 1 : name == "Weak" ? 2 : name == "Strong" ? 3 : -1;
                    AnimationUtility.SetAnimationEvents(clip, kind < 0 ? Array.Empty<AnimationEvent>() : new[] {
                        new AnimationEvent { functionName = "OnCombatImpact", intParameter = kind,
                            time = kind >= 2 ? PrimitiveSkillEffects.AttackFlightDuration : .3f }
                    });
                    var state = machine.AddState(name); state.motion = clip; state.writeDefaultValues = true;
                    if (name == "Idle") idle = state;
                    EditorUtility.SetDirty(clip);
                }
                machine.defaultState = idle;
                foreach (var child in machine.states)
                    if (child.state.name != "Idle" && child.state.name != "Death")
                    {
                        var transition = child.state.AddTransition(idle);
                        transition.hasExitTime = true; transition.exitTime = 1;
                        transition.hasFixedDuration = true; transition.duration = .08f;
                    }
                var animator = contents.GetComponent<Animator>();
                if (!animator) animator = contents.AddComponent<Animator>();
                animator.enabled = true;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (!contents.GetComponent<CombatAnimationRelay>()) contents.AddComponent<CombatAnimationRelay>();
                EditorUtility.SetDirty(controller);
                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Moonlit] Reference Player now owns its Animator, seven native-path clips and impact relay.");
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        static float Swing(string state, string bone)
        {
            if (bone == "bone_7") return state == "Basic" ? -75 : state == "Weak" ? -45 : state == "Strong" ? -120 : state == "Buff" ? 50 : 0;
            if (bone == "bone_5") return state == "Basic" ? 20 : state == "Strong" ? 45 : state == "Buff" ? -30 : 0;
            if (bone == "bone_2") return state == "Idle" ? 2 : state == "Hit" ? 8 : state == "Buff" ? -8 : 0;
            if (bone == "bone_4" || bone == "bone_3") return state == "Basic" ? 7 : 0;
            return 0;
        }
    }
}
