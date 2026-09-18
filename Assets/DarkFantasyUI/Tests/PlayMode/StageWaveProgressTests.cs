using DG.Tweening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI.Tests
{
    public sealed class StageWaveProgressTests
    {
        GameObject root;
        Image[] nodes,lines;
        RectTransform[] pulses;
        StageWaveProgress progress;
        [SetUp]
        public void SetUp()
        {
            root=new GameObject("Wave progress test",typeof(RectTransform));
            nodes=new Image[3];pulses=new RectTransform[3];lines=new Image[2];
            for(int i=0;i<3;i++) {
                nodes[i]=Ui.Image("Node "+i,root.transform,i*100,0,24,24,null);
                pulses[i]=nodes[i].rectTransform;
            }
            for(int i=0;i<2;i++)lines[i]=Ui.Image("Line "+i,root.transform,i*100+24,8,76,8,null);
            progress=root.AddComponent<StageWaveProgress>();
            progress.Initialize(nodes,lines,pulses);
        }
        [TearDown]
        public void TearDown(){if(root)Object.DestroyImmediate(root);}

        [Test]
        public void WaveAdvance_FillsConnectingLineThenPulsesNode_WithoutRestartOnRefresh()
        {
            Assert.AreEqual(0,lines[0].rectTransform.rect.width);
            Assert.AreEqual(0,lines[1].rectTransform.rect.width);
            Assert.AreEqual(Ui.Cyan,nodes[0].color);
            progress.SetProgress(1,2);
            var tween=DOTween.TweensByTarget(progress,false)[0];
            tween.Goto(.2f,false);
            float midpoint=lines[0].rectTransform.rect.width;
            Assert.That(midpoint,Is.GreaterThan(0).And.LessThan(76));
            Assert.AreEqual(Vector3.one,pulses[1].localScale,"Node pulse follows the line fill.");
            Assert.AreEqual(new Color(0,.24f,.34f),nodes[1].color,
                "The next node stays dark until the connecting line reaches it.");
            for(int i=0;i<60;i++)progress.SetProgress(1,2);
            Assert.AreSame(tween,DOTween.TweensByTarget(progress,false)[0]);
            Assert.That(lines[0].rectTransform.rect.width,Is.EqualTo(midpoint).Within(.001f));
            tween.Goto(.5f,false);
            Assert.That(lines[0].rectTransform.rect.width,Is.EqualTo(76).Within(.01f));
            Assert.AreEqual(Ui.Cyan,nodes[1].color,"The filled connection lights the node before its pulse.");
            Assert.Greater(pulses[1].localScale.x,1);
            Assert.AreEqual(0,lines[1].rectTransform.rect.width);
            tween.Goto(.699f,false);
            Assert.That(pulses[1].localScale.x,Is.EqualTo(1).Within(.001f));
        }

        [Test]
        public void StageChangesAndWaveRollback_CancelOldAnimationAndResetImmediately()
        {
            progress.SetProgress(1,2);
            DOTween.TweensByTarget(progress,false)[0].Goto(.5f,false);
            progress.SetProgress(2,1);
            Assert.IsFalse(DOTween.IsTweening(progress));
            foreach(var line in lines)Assert.AreEqual(0,line.rectTransform.rect.width);
            foreach(var pulse in pulses)Assert.AreEqual(Vector3.one,pulse.localScale);
            progress.SetProgress(2,3);
            DOTween.TweensByTarget(progress,false)[0].Goto(.2f,false);
            progress.SetProgress(2,1);
            Assert.IsFalse(DOTween.IsTweening(progress));
            foreach(var line in lines)Assert.AreEqual(0,line.rectTransform.rect.width);
            progress.SetProgress(1,3);
            foreach(var line in lines)Assert.AreEqual(76,line.rectTransform.rect.width);
            Assert.IsFalse(DOTween.IsTweening(progress),"A different stage restores saved wave progress without replaying.");
        }

        [Test]
        public void DisableAndDestroy_CancelTweenAndRestoreStableNodeScale()
        {
            progress.SetProgress(1,2);
            DOTween.TweensByTarget(progress,false)[0].Goto(.5f,false);
            progress.enabled=false;
            Assert.IsFalse(DOTween.IsTweening(progress));
            Assert.AreEqual(76,lines[0].rectTransform.rect.width);
            Assert.AreEqual(Vector3.one,pulses[1].localScale);
            progress.enabled=true;
            progress.SetProgress(1,3);
            Assert.IsTrue(DOTween.IsTweening(progress));
            Object.DestroyImmediate(root);root=null;
            Assert.IsFalse(DOTween.IsTweening(progress));
        }
    }
}
