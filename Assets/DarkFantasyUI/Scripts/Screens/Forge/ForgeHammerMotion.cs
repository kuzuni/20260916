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
        static readonly float[] HitTimes={.16f,.49f,.82f};
        ForgeImpactSparks sparks;
        RectTransform hammer;
        int nextStrike;
        public int ImpactCount => nextStrike;
        public event System.Action<int> Struck;
        public static ForgeHammerMotion Play(Transform anvil)
        {
            var root=Ui.Rect("Forging hammer",anvil,125,-100,164,164);
            var image=Ui.Image("Hammer",root,0,0,164,164,RewardsScreenModule.HammerArt);
            image.preserveAspect=true;image.rectTransform.pivot=new Vector2(.5f,.12f);
            image.rectTransform.anchoredPosition+=new Vector2(82,-144);
            var result=root.gameObject.AddComponent<ForgeHammerMotion>();
            result.hammer=image.rectTransform;
            var sparkRoot=Ui.Rect("Forge impact sparks",root,0,0,360,240);
            sparkRoot.pivot=Vector2.one*.5f;
            result.sparks=sparkRoot.gameObject.AddComponent<ForgeImpactSparks>();
            result.sparks.raycastTarget=false;
            result.sparks.Sample(0,1);
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
            seconds=System.Math.Max(0,System.Math.Min(1,seconds));
            motion.SetTime(seconds);graph.Evaluate(0);
            // PlayableGraph is sampled manually, so use the same clip timestamps instead of
            // AnimationEvents (which can be skipped by zero-delta Evaluate / slow frames).
            while(nextStrike<HitTimes.Length && seconds>=HitTimes[nextStrike]) {
                // Find the head at the impact pose, not at a later recovery-frame pose.
                motion.SetTime(HitTimes[nextStrike]);graph.Evaluate(0);
                Vector3 head=hammer.TransformPoint(new Vector3(hammer.rect.xMin+hammer.rect.width*.3f,hammer.rect.yMin+hammer.rect.height*.78f,0));
                sparks.rectTransform.position=head;
                nextStrike++;
                Struck?.Invoke(nextStrike);
            }
            motion.SetTime(seconds);graph.Evaluate(0);
            int visibleStrike=-1;
            for(int i=0;i<HitTimes.Length;i++)if(seconds>=HitTimes[i])visibleStrike=i;
            sparks.Sample(visibleStrike,visibleStrike<0?1:seconds-HitTimes[visibleStrike]);
        }
        void OnDestroy(){if(graph.IsValid())graph.Destroy();}
    }
}
