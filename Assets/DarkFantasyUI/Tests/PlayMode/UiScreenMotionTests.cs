using System.Collections;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Moonlit.UI.Tests
{
    public sealed class UiScreenMotionTests
    {
        [UnityTest]
        public IEnumerator PageAndChatRevealElementsSequentiallyWithoutMovingRootOrHitAreas()
        {
            var root=new GameObject("Motion fixture",typeof(RectTransform));
            try {
                var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(1080,1920);
                var panel=Ui.Image("Decorative page frame",rect,0,0,1080,1920,null);
                var first=Ui.Button("First",panel.transform,50,100,400,90,"",null);
                var last=Ui.Button("Last",panel.transform,50,700,400,90,"",null);
                var before=((RectTransform)first.transform).anchoredPosition;
                var motion=UiScreenMotion.Play(rect,true);
                Assert.IsNull(root.GetComponent<CanvasGroup>(),"The page itself must not fade.");
                Assert.IsNull(panel.GetComponent<CanvasGroup>(),"Decorative frame must not fade all contents as one group.");
                Assert.AreEqual(2,motion.Targets.Count);Assert.AreEqual(Vector3.one,rect.localScale);
                DOTween.Goto(motion,.06f,false);
                Assert.Greater(motion.Targets[0].alpha,motion.Targets[1].alpha);
                Assert.AreEqual(before,((RectTransform)first.transform).anchoredPosition);
                Assert.IsTrue(first.interactable);
                motion.Complete();
                foreach(var group in motion.Targets)Assert.AreEqual(1,group.alpha);
                Assert.IsFalse(motion.IsPlaying);
                yield return null;LogAssert.NoUnexpectedReceived();
            } finally {Object.DestroyImmediate(root);}
        }
        [Test]
        public void ModalFadesAndPopsAroundItsCenterAndClosingDuringTweenRestoresLayout()
        {
            var root=new GameObject("Modal motion",typeof(RectTransform));
            try {
                var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(1080,1920);
                rect.pivot=new Vector2(0,1);rect.localScale=Vector3.one*.9f;
                rect.anchoredPosition=new Vector2(42,-31);
                var restingScale=rect.localScale;var restingPosition=rect.localPosition;
                var center=rect.TransformPoint(rect.rect.center);
                Ui.Image("Modal panel",rect,50,400,980,1000,null);
                var motion=UiScreenMotion.Play(rect,false);
                var group=root.GetComponent<CanvasGroup>();
                Assert.AreEqual(0,group.alpha);Assert.AreEqual(1,motion.Targets.Count);
                Assert.AreEqual(restingScale.x*.86f,rect.localScale.x,.001f);
                Assert.Less(Vector3.Distance(center,rect.TransformPoint(rect.rect.center)),.01f);
                DOTween.Goto(motion,.11f,false);Assert.That(group.alpha,Is.InRange(.01f,.99f));
                DOTween.Goto(motion,.19f,false);Assert.Greater(rect.localScale.x,restingScale.x);
                Assert.Less(Vector3.Distance(center,rect.TransformPoint(rect.rect.center)),.01f);
                var tween=DOTween.TweensByTarget(motion,false)[0];
                root.SetActive(false);Assert.IsFalse(tween.IsActive());Assert.AreEqual(1,group.alpha);
                Assert.AreEqual(restingScale,rect.localScale);Assert.AreEqual(restingPosition,rect.localPosition);
                root.SetActive(true);motion=UiScreenMotion.Play(rect,false);
                DOTween.Goto(motion,.4f,false);
                Assert.AreEqual(restingScale,rect.localScale);Assert.AreEqual(restingPosition,rect.localPosition);
            } finally {Object.DestroyImmediate(root);}
        }
    }
}
