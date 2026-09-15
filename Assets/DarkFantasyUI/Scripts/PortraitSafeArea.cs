using UnityEngine;

namespace Moonlit.UI
{
    [ExecuteAlways]
    public sealed class PortraitSafeArea : MonoBehaviour
    {
        public RectTransform design;
        public RectTransform canvasRect;
        void LateUpdate()
        {
            if (!design || !canvasRect || Screen.width == 0 || Screen.height == 0) return;
            Rect safe = Application.isPlaying ? Screen.safeArea : new Rect(0,0,Screen.width,Screen.height);
            Vector2 size = canvasRect.rect.size;
            float factor = Mathf.Min(size.x * safe.width / Screen.width / 1080f, size.y * safe.height / Screen.height / 1920f);
            design.localScale = Vector3.one * factor;
            design.anchoredPosition = new Vector2((safe.center.x / Screen.width-.5f)*size.x,(safe.center.y / Screen.height-.5f)*size.y);
        }
    }
}
