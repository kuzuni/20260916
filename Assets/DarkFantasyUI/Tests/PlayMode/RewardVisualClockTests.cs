using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Moonlit.UI.Tests
{
    public sealed class RewardVisualClockTests
    {
        GameObject root;
        MainScreen CreateReward()
        {
            root=new GameObject("Reward post-creation stall",typeof(RectTransform),typeof(Canvas));
            var design=(RectTransform)root.transform;design.sizeDelta=new Vector2(1080,1920);
            var main=root.AddComponent<MainScreen>();main.enabled=false;
            main.toastRoot=design;main.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            main.forgeButton=Ui.ArtButton("Hammer target",design,480,1400,120,100);
            RewardVisuals.Absorb(main,RewardVisuals.Kind.Hammer,100,design.TransformPoint(new Vector3(0,-600,0)));
            return main;
        }
        [TearDown] public void TearDown(){if(root)Object.DestroyImmediate(root);}

        [UnityTest]
        public IEnumerator SlowFrameAfterCreationCannotExpireRewardBeforeItsFirstVisibleFrames()
        {
            CreateReward();
            var life=root.GetComponentInChildren<RewardVisualLifetime>();
            var source=life.transform.Find("Reward particle 0").GetComponent<RectTransform>();
            // This reproduces the shop capture path: effect exists, then a long layout/render frame,
            // before the next Update. The old absolute-age clock destroyed all sprites immediately.
            var delay=System.Diagnostics.Stopwatch.StartNew();
            while(delay.Elapsed.TotalSeconds<1.35) { }
            yield return null;yield return null;
            Assert.IsNotNull(life,"The reward must survive its first two displayed frames after the stall.");
            Assert.IsNotNull(source);
            Assert.AreEqual(12,life.GetComponentsInChildren<Image>().Length);
            Assert.AreEqual("+100",life.GetComponentsInChildren<Text>().Single().text);
            Assert.Greater(source.localScale.x,.8f,"Do not skip straight to the shrunken destination.");
            life.SampleAge(1.3f);
            yield return null;
            Assert.IsTrue(life==null,"Explicit completion must still clean the entire effect up.");
        }

        [UnityTest]
        public IEnumerator CaptureHoldBeforeFirstYieldKeepsVisibleRewardPoseAndResumeCompletes()
        {
            CreateReward();
            var life=root.GetComponentInChildren<RewardVisualLifetime>();
            life.HoldAt(.16f);
            var bit=life.transform.Find("Reward particle 0").GetComponent<RectTransform>();
            Vector2 position=bit.anchoredPosition;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsNotNull(life);Assert.AreEqual(position,bit.anchoredPosition);
            Assert.AreEqual(12,life.GetComponentsInChildren<Image>().Length);
            life.Resume();life.SampleAge(.55f);
            Assert.Greater(Vector2.Distance(position,bit.anchoredPosition),10);
            life.SampleAge(1.3f);yield return null;
            Assert.IsTrue(life==null);
        }
    }
}
