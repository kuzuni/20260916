using System.Linq;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Moonlit.UI.Tests
{
    public sealed class SkillHudFeedbackTests
    {
        GameObject root;
        SkillHudFeedback feedback;
        Image icon,shade,flash;
        Text label;
        CollectionEntry skill;
        object stage;

        [SetUp]
        public void SetUp()
        {
            root=new GameObject("Skill feedback fixture",typeof(RectTransform));
            icon=Graphic<Image>("Icon",new Vector2(92,92));
            shade=Graphic<Image>("Cooldown",new Vector2(100,100));
            shade.type=Image.Type.Filled;shade.fillMethod=Image.FillMethod.Radial360;
            flash=Graphic<Image>("Flash",new Vector2(116,116));
            label=Graphic<Text>("Turns",new Vector2(116,38));
            feedback=root.AddComponent<SkillHudFeedback>();
            skill=new CollectionEntry{category=0,variant=2,unlocked=true};
            stage=new object();
        }
        T Graphic<T>(string name,Vector2 size) where T:Graphic
        {
            var child=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(T));
            child.transform.SetParent(root.transform,false);
            var rect=(RectTransform)child.transform;
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
            rect.sizeDelta=size;rect.anchoredPosition=new Vector2(12,-10);
            return child.GetComponent<T>();
        }
        void Initialize(){feedback.Initialize(icon,shade,flash,label);feedback.Apply(skill,stage,5,0);}
        void Seek(float time)
        {
            var tweens=DOTween.TweensByTarget(feedback,false);
            if(tweens==null)return;
            foreach(var tween in tweens.ToArray()){tween.Pause();tween.GotoWithCallbacks(time);}
        }
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void InitializationPreservesIconBoundsAndDoesNotReplayHistoricalActivation()
        {
            var before=new Vector3[4];var after=new Vector3[4];
            icon.rectTransform.GetWorldCorners(before);
            feedback.Initialize(icon,shade,flash,label);
            icon.rectTransform.GetWorldCorners(after);
            for(int i=0;i<4;i++)Assert.Less(Vector3.Distance(before[i],after[i]),.001f);
            feedback.Apply(skill,stage,3,7);
            Assert.AreEqual("3턴",label.text);
            Assert.AreEqual(.6f,shade.fillAmount,.0001f);
            Assert.AreEqual(0,flash.color.a);
            Assert.IsFalse(DOTween.IsTweening(feedback));
        }

        [Test]
        public void TurnChangesAnimateRadialAndLabelOnceWithoutRestartingEveryRefresh()
        {
            Initialize();
            feedback.Apply(skill,stage,4,0);
            Seek(.08f);
            Assert.Greater(shade.fillAmount,.8f);
            Assert.Less(shade.fillAmount,1f);
            Assert.Greater(label.transform.localScale.x,1f);
            Assert.AreEqual("4턴",label.text);
            var active=DOTween.TweensByTarget(feedback,false).ToArray();
            feedback.Apply(skill,stage,4,0);
            CollectionAssert.AreEquivalent(active,DOTween.TweensByTarget(feedback,false));
            Seek(.3f);
            Assert.AreEqual(.8f,shade.fillAmount,.0001f);
            Assert.AreEqual(Vector3.one,label.transform.localScale);
            Assert.AreEqual(0,flash.color.a);
        }

        [Test]
        public void ActualImpactFlashesEvenWhenReadyTurnWasNotRenderedAndDoesNotRepeat()
        {
            Initialize();
            // An Animator event can occur between two HUD refreshes: remaining stays five.
            feedback.Apply(skill,stage,5,1);
            Assert.Greater(flash.color.a,.9f);
            Seek(.1f);
            Assert.Greater(icon.transform.localScale.x,1.1f);
            Assert.Greater(flash.color.a,0);
            float alpha=flash.color.a;
            feedback.Apply(skill,stage,5,1);
            Assert.AreEqual(alpha,flash.color.a,.0001f);
            Seek(.6f);
            Assert.AreEqual(Vector3.one,icon.transform.localScale);
            Assert.AreEqual(0,flash.color.a,.0001f);
            feedback.Apply(skill,stage,5,2);
            Assert.Greater(flash.color.a,.9f,"A subsequent impact in the same stage must animate.");
        }

        [Test]
        public void StageResetAndDisableCleanTweensWithoutChangingTurnStateOrReplayingImpact()
        {
            Initialize();feedback.Apply(skill,stage,5,1);Seek(.1f);
            feedback.Apply(skill,new object(),5,0);
            Assert.IsFalse(DOTween.IsTweening(feedback));
            Assert.AreEqual(0,flash.color.a);
            Assert.AreEqual(Vector3.one,icon.transform.localScale);
            stage=new object();feedback.Apply(skill,stage,5,0);
            feedback.Apply(skill,stage,4,1);Seek(.08f);
            root.SetActive(false);
            Assert.IsFalse(DOTween.IsTweening(feedback));
            Assert.AreEqual(.8f,shade.fillAmount,.0001f);
            Assert.AreEqual(Vector3.one,icon.transform.localScale);
            Assert.AreEqual(Vector3.one,label.transform.localScale);
            Assert.AreEqual(0,flash.color.a);
            root.SetActive(true);feedback.Apply(skill,stage,4,1);
            Assert.AreEqual("4턴",label.text);
            Assert.IsFalse(DOTween.IsTweening(feedback),"Reopening the HUD must not replay an old impact.");
        }
    }
}
