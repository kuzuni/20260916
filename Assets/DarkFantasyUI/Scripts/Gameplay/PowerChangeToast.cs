using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    // A separate non-interactive overlay: ordinary Toast messages keep their own text and lifetime.
    public sealed class PowerChangeToast : MonoBehaviour
    {
        MainScreen main;
        bool hasBaseline;
        double previousPower;
        RectTransform visual;
        CanvasGroup group;
        Text direction, total;
        Sequence feedbackSequence;
        double started;
        public int PresentationCount { get; private set; }
        public double LastPower => previousPower;
        public bool Visible => visual && visual.gameObject.activeSelf;
        public bool Increased { get; private set; }

        public static PowerChangeToast Ensure(MainScreen main)
        {
            var feedback = main.GetComponent<PowerChangeToast>();
            if (!feedback) feedback = main.gameObject.AddComponent<PowerChangeToast>();
            feedback.main = main;
            return feedback;
        }

        public void Observe(double power)
        {
            if (double.IsNaN(power) || double.IsInfinity(power)) return;
            if (!hasBaseline)
            {
                hasBaseline = true;
                previousPower = power;
                return;
            }
            if (power == previousPower) return;
            Increased = power > previousPower;
            previousPower = power;
            if (!main || (!main.toastRoot && !main.design)) return;
            Build();
            feedbackSequence?.Kill();
            visual.gameObject.SetActive(true);
            visual.SetAsLastSibling();
            visual.anchoredPosition = Vector2.zero;
            visual.localScale = Vector3.one * .78f;
            group.alpha = 0;
            Color tint = Increased ? new Color(.3f, 1f, .4f) : new Color(1f, .24f, .2f);
            direction.text = Increased ? "↑ UP" : "↓ DOWN";
            direction.color = tint;
            total.text = "전투력 " + MainScreen.Compact(power);
            total.color = Ui.Ivory;
            PresentationCount++;
            started = Time.realtimeSinceStartupAsDouble;
            feedbackSequence = DOTween.Sequence().SetUpdate(UpdateType.Manual, true).SetTarget(this);
            feedbackSequence.Append(DOTween.To(() => group.alpha, value => group.alpha = value, 1f, .12f));
            feedbackSequence.Join(visual.DOScale(1.08f, .18f).SetEase(Ease.OutBack));
            feedbackSequence.Append(visual.DOScale(1, .12f).SetEase(Ease.OutQuad));
            feedbackSequence.AppendInterval(.85f);
            feedbackSequence.Append(DOTween.To(() => group.alpha, value => group.alpha = value, 0f, .32f));
            feedbackSequence.Join(DOTween.To(() => visual.anchoredPosition, value => visual.anchoredPosition = value, new Vector2(0, 40), .32f).SetEase(Ease.OutQuad));
            feedbackSequence.OnComplete(() => { if (visual) visual.gameObject.SetActive(false); });
        }

        void Update()
        {
            if (feedbackSequence != null && feedbackSequence.IsActive() && feedbackSequence.IsPlaying())
                feedbackSequence.Goto((float)(Time.realtimeSinceStartupAsDouble - started), true);
        }

        void Build()
        {
            if (visual) return;
            visual = Ui.Rect("Power change overlay", main.toastRoot ? main.toastRoot : main.design, 0, 0, 800, 156);
            visual.anchorMin = visual.anchorMax = new Vector2(.5f, .68f);
            visual.pivot = Vector2.one * .5f;
            group = visual.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            direction = Ui.Text("Power direction", visual, 0, 0, 800, 62, "", 38, main.font);
            total = Ui.Text("Power total", visual, 0, 64, 800, 84, "", 48, main.font);
            direction.horizontalOverflow = total.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        void OnDisable()
        {
            feedbackSequence?.Kill();
            if (visual) visual.gameObject.SetActive(false);
        }
        void OnDestroy()
        {
            feedbackSequence?.Kill();
            if (visual) Destroy(visual.gameObject);
        }
    }
}
