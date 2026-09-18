using System.Linq;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI.Tests
{
    public sealed class RewardVisualTests
    {
        GameObject root;

        [TearDown]
        public void TearDown()
        {
            if(root)Object.DestroyImmediate(root);
        }

        [TestCase(1920f,0f,1f)]
        [TestCase(1920f,.5f,1f)]
        [TestCase(2280f,.5f,.85f)]
        public void RewardParticles_StartAtClaimAndArriveAtHudAcrossSafeLayouts(float height,float pivotX,float scale)
        {
            root=new GameObject("Reward visual test",typeof(RectTransform));
            var design=(RectTransform)root.transform;
            design.sizeDelta=new Vector2(1080,height);
            design.pivot=new Vector2(pivotX,1);
            design.localScale=Vector3.one*scale;
            var main=root.AddComponent<MainScreen>();main.enabled=false;
            main.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            main.toastRoot=design;
            var target=Ui.Rect("Hammer HUD",design,480,height-550,120,100);
            main.forgeButton=target.gameObject.AddComponent<Button>();
            var reward=Ui.Rect("Claimed reward",design,430,height*.45f,220,220);
            Vector3 origin=reward.TransformPoint(reward.rect.center);
            Vector3 destination=target.TransformPoint(target.rect.center);

            RewardVisuals.Absorb(main,RewardVisuals.Kind.Hammer,100,origin);

            var life=design.GetComponentInChildren<RewardVisualLifetime>();
            Assert.IsNotNull(life,"A claimed hammer reward must produce a visible effect.");
            var particle=(RectTransform)life.transform.Find("Reward particle 0");
            Assert.IsNotNull(particle);
            Assert.Less(Vector3.Distance(origin,particle.TransformPoint(particle.rect.center)),.01f,
                "The effect must begin on the claimed reward even when the safe-area design has a centered pivot.");
            var amount=life.GetComponentsInChildren<Text>().Single(t=>t.name=="Reward amount");
            Assert.AreEqual("+100",amount.text);
            var labelCenter=amount.rectTransform.TransformPoint(amount.rectTransform.rect.center);
            Assert.That(Mathf.Abs(labelCenter.x-origin.x),Is.LessThan(.01f));
            Assert.Greater(labelCenter.y,origin.y,"The amount floats immediately above the reward.");

            var tweens=DOTween.TweensByTarget(life,false);
            Assert.IsNotNull(tweens);
            tweens[0].Goto(.809f,false);
            Assert.Less(Vector3.Distance(destination,particle.TransformPoint(particle.rect.center)),8f,
                "The first particle must finish at the hammer HUD, not a pivot-shifted position.");
        }
    }
}
