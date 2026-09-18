using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;
namespace Moonlit.UI
{
    // The Animator samples an authored three-strike clip against actual elapsed time.
    public sealed class ForgeHammerMotion : MonoBehaviour
    {
        PlayableGraph graph;AnimationClipPlayable motion;
        public static ForgeHammerMotion Play(Transform anvil)
        {
            var root=Ui.Rect("Forging hammer",anvil,125,-100,164,164);
            var image=Ui.Image("Hammer",root,0,0,164,164,RewardsScreenModule.HammerArt);
            image.preserveAspect=true;image.rectTransform.pivot=new Vector2(.5f,.12f);
            image.rectTransform.anchoredPosition+=new Vector2(82,-144);
            var result=root.gameObject.AddComponent<ForgeHammerMotion>();
            var animator=root.gameObject.AddComponent<Animator>();animator.updateMode=AnimatorUpdateMode.UnscaledTime;
            var clip=Resources.Load<AnimationClip>("Moonlit/Forge/ForgeHammer");
            if(!clip){Debug.LogError("ForgeHammer animation is missing.");return result;}
            result.graph=PlayableGraph.Create("Forge hammer three strikes");
            result.graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            result.motion=AnimationClipPlayable.Create(result.graph,clip);
            result.motion.SetApplyFootIK(false);result.motion.SetApplyPlayableIK(false);
            var output=AnimationPlayableOutput.Create(result.graph,"Hammer",animator);output.SetSourcePlayable(result.motion);
            result.graph.Play();result.Sample(0);return result;
        }
        public void Sample(double seconds)
        {
            if(!graph.IsValid())return;
            motion.SetTime(System.Math.Min(1,seconds));graph.Evaluate(0);
        }
        void OnDestroy(){if(graph.IsValid())graph.Destroy();}
    }
}
