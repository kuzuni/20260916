using System;
using System.Linq;
using Moonlit.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Moonlit.Editor
{
    // Historical entry point retained for CI compatibility. Animation authoring belongs to the user.
    // Neither entry point creates, clears, rewrites, saves or reassigns animation assets.
    public static class ReferencePlayerAnimatorBuilder
    {
        public const string PrefabPath = "Assets/Art/ChihuahuaEquipmentThemes/Reference/Player.prefab";
        public const string Root = "Assets/DarkFantasyUI/Resources/Moonlit/Combat/PlayerReference";
        public const string ControllerPath = Root + "/PlayerReference.controller";
        static readonly string[] Names = { "Idle", "Basic", "Hit", "Buff", "Weak", "Strong", "Death" };

        public static void Build() => ValidateCommitted();

        public static void ValidateCommitted()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!prefab) throw new InvalidOperationException("The supplied reference Player prefab is missing.");
            var animator = prefab.GetComponent<Animator>();
            if (!animator || !animator.runtimeAnimatorController)
                throw new InvalidOperationException("Assign the authored Animator controller on the reference Player prefab.");
            var assigned = animator.runtimeAnimatorController;
            var overrides = assigned as AnimatorOverrideController;
            var controller = (overrides ? overrides.runtimeAnimatorController : assigned) as AnimatorController;
            if (!controller || controller.layers.Length == 0)
                throw new InvalidOperationException("The assigned Player controller must contain a base animation layer.");
            var machine = controller.layers[0].stateMachine;
            // Extra states/layers and author-selected clip paths, curves and transition settings are retained.
            foreach (string name in Names)
            {
                var state = machine.states.Select(child => child.state).FirstOrDefault(candidate => candidate.name == name);
                if (!state) throw new InvalidOperationException("Player controller is missing the runtime state " + name);
                var clip = state.motion as AnimationClip;
                if (overrides && clip) clip = overrides[clip] ? overrides[clip] : clip;
                if (!clip || clip.length <= 0)
                    throw new InvalidOperationException("Player state needs a nonempty authored clip: " + name);
                var bindings = AnimationUtility.GetCurveBindings(clip)
                    .Concat(AnimationUtility.GetObjectReferenceCurveBindings(clip)).ToArray();
                if (bindings.Length == 0)
                    throw new InvalidOperationException("Player clip contains no authored animation: " + name);
                foreach (var binding in bindings)
                {
                    var target = string.IsNullOrEmpty(binding.path) ? prefab.transform : prefab.transform.Find(binding.path);
                    if (!target)
                        throw new InvalidOperationException("Player clip has an unresolved native binding: " + name + " / " + binding.path);
                }
                int kind = name == "Basic" ? 0 : name == "Buff" ? 1 : name == "Weak" ? 2 : name == "Strong" ? 3 : -1;
                if (kind < 0) continue;
                var impacts = AnimationUtility.GetAnimationEvents(clip)
                    .Where(evt => evt.functionName == nameof(CombatAnimationRelay.OnCombatImpact) && evt.intParameter == kind).ToArray();
                if (impacts.Length == 0 || impacts.Any(evt => evt.time < 0 || evt.time > clip.length))
                    throw new InvalidOperationException("Player clip requires an in-range OnCombatImpact event of kind " + kind + ": " + name);
            }
            Debug.Log("[Moonlit] Validated the prefab-assigned Player Animator without changing authored assets or settings.");
        }
    }
}
