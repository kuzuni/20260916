using System.Linq;
using System.Collections;
using UnityEngine.TestTools;
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


        [UnityTest]
        public IEnumerator NewRewardDoesNotConsumeTheRenderFramesPreviousTime()
        {
            root=new GameObject("Reward clock test",typeof(RectTransform),typeof(Canvas));
            var design=(RectTransform)root.transform;design.sizeDelta=new Vector2(1080,1920);
            var main=root.AddComponent<MainScreen>();main.enabled=false;
            main.toastRoot=design;main.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            main.forgeButton=Ui.ArtButton("Hammer target",design,480,1400,120,100);
            // Simulate a long camera render before creating the effect in the same frame.
            // Its preceding work must not be charged against the new particle's lifetime.
            var watch=System.Diagnostics.Stopwatch.StartNew();
            while(watch.Elapsed.TotalSeconds<.9) { }
            RewardVisuals.Absorb(main,RewardVisuals.Kind.Hammer,100,design.TransformPoint(new Vector3(0,-600,0)));
            var life=design.GetComponentInChildren<RewardVisualLifetime>();
            var first=life.transform.Find("Reward particle 0");
            yield return null;
            Assert.IsNotNull(first,"The first particle must survive its first frame after an expensive render.");
            Assert.IsNotNull(life);
            Assert.AreEqual(13,DOTween.TweensByTarget(life,false).Count,"No particle sequence may auto-complete from time spent before it existed.");
            foreach(var tween in DOTween.TweensByTarget(life,false))tween.Goto(.16f,false);
            var particle=first.GetComponent<Image>();
            Assert.IsNotNull(particle.sprite);Assert.Greater(particle.color.a,0);
            Assert.AreEqual("+100",life.GetComponentsInChildren<Text>().Single(label=>label.name=="Reward amount").text);
            DOTween.Play(life);life.SampleAge(1.3f);
            yield return null;
            Assert.IsTrue(life==null,"The independent real-time clock must still clean up a completed reward.");
        }

        [TestCase(RewardVisuals.Kind.HammerKey,5)]
        [TestCase(RewardVisuals.Kind.GhostKey,2)]
        [TestCase(RewardVisuals.Kind.InvasionKey,4)]
        [TestCase(RewardVisuals.Kind.ZombieKey,3)]
        public void DungeonKeyParticlesUseTheirOwnArtAndArriveAtDungeonNavigation(RewardVisuals.Kind kind,int iconIndex)
        {
            root=new GameObject("key reward visual test",typeof(RectTransform));
            var design=(RectTransform)root.transform;design.sizeDelta=new Vector2(1080,1920);
            var main=root.AddComponent<MainScreen>();main.enabled=false;
            main.toastRoot=design;main.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            main.navigation=new Button[3];
            for(int i=0;i<3;i++)main.navigation[i]=Ui.ArtButton("Navigation "+i,design,100+i*300,1760,100,90);
            RewardVisuals.Absorb(main,kind,2,design.TransformPoint(new Vector3(350,-500,0)));
            var life=design.GetComponentInChildren<RewardVisualLifetime>();
            var particle=life.transform.Find("Reward particle 0").GetComponent<Image>();
            Assert.AreSame(PopupSkin.RewardIcon(iconIndex),particle.sprite);
            Assert.AreEqual(new Vector2(144,144),particle.rectTransform.sizeDelta);
            var target=(RectTransform)main.navigation[1].transform;
            DOTween.TweensByTarget(life,false)[0].Goto(.809f,false);
            Assert.Less(Vector3.Distance(target.TransformPoint(target.rect.center),
                particle.rectTransform.TransformPoint(particle.rectTransform.rect.center)),8f);
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
            Assert.AreEqual(new Vector2(144,144),particle.sizeDelta,"All reward particles are three times the former 48px size.");
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
