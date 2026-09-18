using DG.Tweening;
using UnityEngine;

namespace Moonlit.UI
{
    /// <summary>Owns the result screen's unscaled, ordered DOTween reveal and cancels it with its UI lifetime.</summary>
    public sealed class SummonRevealAnimation : MonoBehaviour
    {
        public const float Stagger = .14f;
        public const float Duration = .32f;
        Sequence sequence;
        CanvasGroup[] cards;
        Vector2[] positions;
        public bool IsRevealing => sequence != null && sequence.IsActive() && !sequence.IsComplete();

        public void Play(CanvasGroup[] resultCards)
        {
            Finish();
            cards = resultCards;
            positions = new Vector2[cards.Length];
            sequence = DOTween.Sequence().SetUpdate(true);
            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                var card = cards[i];
                var rect = (RectTransform)card.transform;
                positions[i] = rect.anchoredPosition;
                card.alpha = 0;
                card.interactable = card.blocksRaycasts = false;
                rect.localScale = Vector3.one * .68f;
                rect.anchoredPosition = positions[i] + Vector2.down * 44;
                float progress = 0;
                sequence.Insert(i * Stagger, DOTween.To(() => progress, value => {
                    progress = value;
                    if (!card) return;
                    card.alpha = Mathf.Clamp01(value);
                    rect.localScale = Vector3.one * Mathf.LerpUnclamped(.68f, 1, value);
                    rect.anchoredPosition = positions[index] + Vector2.down * (44 * (1 - value));
                }, 1, Duration).SetEase(Ease.OutBack).OnComplete(() => {
                    if (card) card.interactable = card.blocksRaycasts = true;
                }));
            }
        }
        void Finish()
        {
            if (sequence != null) { sequence.Kill(); sequence = null; }
            if (cards == null) return;
            for (int i = 0; i < cards.Length; i++)
            {
                if (!cards[i]) continue;
                cards[i].alpha = 1;
                cards[i].interactable = cards[i].blocksRaycasts = true;
                var rect = (RectTransform)cards[i].transform;
                rect.localScale = Vector3.one;
                if (positions != null) rect.anchoredPosition = positions[i];
            }
        }
        void OnDisable() { Finish(); }
        void OnDestroy() { Finish(); cards = null; positions = null; }
    }
}
