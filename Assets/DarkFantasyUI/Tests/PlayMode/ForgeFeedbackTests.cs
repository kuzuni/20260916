using System.Collections;
using System.Linq;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Moonlit.UI.Tests
{
    public sealed class ForgeFeedbackTests
    {
        GameObject root;
        MainScreen main;
        [SetUp] public void SetUp()
        {
            ForgeState.Current = new ForgeState();
            CollectionProgression.Reset();
            root = new GameObject("Feedback test canvas", typeof(RectTransform), typeof(Canvas));
            ((RectTransform)root.transform).sizeDelta = new Vector2(1080, 1920);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            main = root.AddComponent<MainScreen>(); main.enabled = false;
            main.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            main.design = main.toastRoot = root.transform;
            main.powerText = Ui.Text("HUD power", root.transform, 0, 0, 300, 60, "", 30, main.font);
        }
        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(root);
            ForgeState.Current = new ForgeState();
            CollectionProgression.Reset();
        }

        [Test] public void FirstRefreshIsSilentAndEquipmentChangesShowNewTotalOnce()
        {
            ForgeState.Current.equipped[5] = new EquipmentRoll { id = 1, part = EquipmentPart.Weapon, level = 20 };
            main.Refresh();
            var feedback = main.GetComponent<PowerChangeToast>();
            Assert.AreEqual(0, feedback.PresentationCount, "Loading already equipped gear must be silent.");
            Assert.IsFalse(feedback.Visible);
            ForgeState.Current.equipped[5].level = 30;
            main.Refresh();
            Assert.IsTrue(feedback.Visible); Assert.IsTrue(feedback.Increased);
            var total = root.GetComponentsInChildren<Text>().Single(t => t.name == "Power total");
            Assert.AreEqual("전투력 " + main.powerText.text, total.text, "Display the new total, not the delta.");
            var direction = root.GetComponentsInChildren<Text>().Single(t => t.name == "Power direction");
            Assert.AreEqual("↑ UP", direction.text); Assert.Greater(direction.color.g, direction.color.r);
            Assert.Greater(direction.rectTransform.anchoredPosition.y, total.rectTransform.anchoredPosition.y);
            for(int i = 0; i < 20; i++) main.Refresh();
            Assert.AreEqual(1, feedback.PresentationCount);
            ForgeState.Current.equipped[5] = null;
            main.Refresh();
            Assert.AreEqual(2, feedback.PresentationCount); Assert.IsFalse(feedback.Increased);
            Assert.AreEqual("↓ DOWN", direction.text); Assert.Greater(direction.color.r, direction.color.g);
            Assert.AreEqual("전투력 " + main.powerText.text, total.text);
            Assert.IsFalse(total.transform.parent.GetComponent<CanvasGroup>().blocksRaycasts);
            Assert.IsTrue(total.transform.parent.GetComponentsInChildren<Graphic>().All(g => !g.raycastTarget));
        }

        [Test] public void CollectionUpgradeUsesSameFeedbackAndOrdinaryToastRemainsIndependent()
        {
            main.Refresh();
            var entry = CollectionProgression.Data.categories[0].entries[0];
            entry.unlocked = true; entry.fragments = 5;
            main.Refresh();
            var feedback = main.GetComponent<PowerChangeToast>();
            Assert.AreEqual(1, feedback.PresentationCount);
            double before = feedback.LastPower;
            Assert.IsTrue(entry.Upgrade());
            main.Refresh();
            Assert.Greater(feedback.LastPower, before);
            Assert.AreEqual(2, feedback.PresentationCount);
            main.Toast("업그레이드 완료");
            Assert.AreEqual("업그레이드 완료", root.GetComponentsInChildren<Text>().Single(t => t.name == "Message").text);
            Assert.IsTrue(feedback.Visible);
            DOTween.Complete(feedback, true);
            Assert.IsFalse(feedback.Visible);
            Assert.IsTrue(root.transform.Find("Toast").gameObject.activeSelf);
        }

        [UnityTest] public IEnumerator PowerFeedbackFadesAndDisableCancelsTween()
        {
            main.Refresh();
            ForgeState.Current.equipped[5] = new EquipmentRoll { id = 2, part = EquipmentPart.Weapon, level = 10 };
            main.Refresh();
            var feedback = main.GetComponent<PowerChangeToast>();
            Assert.IsTrue(feedback.Visible);
            yield return new WaitForSecondsRealtime(1.7f);
            Assert.IsFalse(feedback.Visible);
            ForgeState.Current.equipped[5].level++;
            main.Refresh(); Assert.IsTrue(feedback.Visible);
            root.SetActive(false);
            Assert.IsFalse(feedback.Visible); Assert.IsFalse(DOTween.IsTweening(feedback));
        }

        [Test] public void AnimatorImpactSamplingEmitsThreeBurstsOnceIncludingSkippedFrames()
        {
            var anvil = Ui.Rect("Anvil", root.transform, 350, 1400, 350, 230);
            var motion = ForgeHammerMotion.Play(anvil);
            var particles = motion.GetComponentInChildren<ForgeImpactSparks>();
            int callbacks = 0; motion.Struck += _ => callbacks++;
            motion.Sample(.15);
            Assert.AreEqual(0, motion.ImpactCount); Assert.AreEqual(0, particles.VisibleSparkCount);
            motion.Sample(.16f);
            Assert.AreEqual(1, motion.ImpactCount); Assert.AreEqual(18, particles.VisibleSparkCount);
            motion.Sample(.16f); Assert.AreEqual(1, callbacks, "Repeated clip evaluation must not duplicate a burst.");
            motion.Sample(.4);
            Assert.AreEqual(0, particles.VisibleSparkCount);
            motion.Sample(.83);
            Assert.AreEqual(3, motion.ImpactCount); Assert.AreEqual(3, callbacks, "A slow frame must still cross both remaining impact timestamps.");
            Assert.AreEqual(18, particles.VisibleSparkCount);
            Assert.IsFalse(particles.raycastTarget);
            Assert.IsTrue(particles.gameObject.activeInHierarchy);
            motion.Sample(.49f); motion.Sample(1);
            Assert.AreEqual(3, callbacks, "Rewinding for a preview cannot re-emit spent impacts.");
        }
    }
}
