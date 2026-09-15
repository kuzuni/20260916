using UnityEngine;
using UnityEngine.EventSystems;

namespace Moonlit.UI
{
    public sealed class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public RectTransform artwork;
        Vector3 originalScale;
        Vector2 originalPosition;
        bool pressed;
        bool captured;
        void Awake() { Capture(); }
        void Capture() { if(artwork && !captured) { originalScale=artwork.localScale; originalPosition=artwork.anchoredPosition; captured=true; } }
        public void OnPointerDown(PointerEventData data) {
            if(!artwork || data.button!=PointerEventData.InputButton.Left) return;
            Capture();
            pressed=true; artwork.localScale=originalScale*.96f;
            artwork.anchoredPosition=originalPosition+new Vector2(artwork.rect.width*.02f,-artwork.rect.height*.02f);
        }
        public void OnPointerUp(PointerEventData data) { Restore(); }
        public void OnPointerExit(PointerEventData data) { Restore(); }
        void OnDisable() { Restore(); }
        void Restore() { if(!pressed || !artwork) return; artwork.localScale=originalScale; artwork.anchoredPosition=originalPosition; pressed=false; }
    }
}
