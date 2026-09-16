using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    // Full-bleed scenery, safe-area controls and an elastic battle viewport.
    public sealed class PortraitSafeArea : MonoBehaviour
    {
        public const float DesignWidth=1080, BottomHeight=985, MinimumHeight=1600;
        // Includes the 180-unit navigation panel and its 18-unit bottom margin.
        public const float NavigationTopFromBottom=198;
        public RectTransform canvasRect, safeFrame, design, bottomPanel, battleViewport, battleArt;
        public RectTransform[] additionalSafeFrames, additionalDesigns, additionalBottomPanels;
        public Rect SafePixels { get; private set; }
        public Vector2Int ScreenPixels { get; private set; }
        public float LogicalHeight => design ? design.rect.height : 0;
        bool applying;
#if UNITY_EDITOR
        Vector2Int? previewScreen;
        Rect previewSafe;
        public void SetPreviewMetrics(Vector2Int size,Rect safe) { previewScreen=size; previewSafe=safe; Apply(); }
        public void ClearPreviewMetrics() { previewScreen=null; Apply(); }
#endif
        void OnEnable() { Canvas.willRenderCanvases+=Apply; }
        void OnDisable() { Canvas.willRenderCanvases-=Apply; }
        void LateUpdate() { Apply(); }
        public void Apply()
        {
            if(applying || !canvasRect || !safeFrame || !design || !bottomPanel) return;
            var pixels=new Vector2Int(Screen.width,Screen.height);
            Rect safe=Screen.safeArea;
#if UNITY_EDITOR
            if(previewScreen.HasValue) { pixels=previewScreen.Value; safe=previewSafe; }
#endif
            if(pixels.x<=0 || pixels.y<=0 || safe.width<=0 || safe.height<=0) return;
            applying=true;
            safe=Rect.MinMaxRect(Mathf.Clamp(safe.xMin,0,pixels.x),Mathf.Clamp(safe.yMin,0,pixels.y),Mathf.Clamp(safe.xMax,0,pixels.x),Mathf.Clamp(safe.yMax,0,pixels.y));
            ScreenPixels=pixels; SafePixels=safe;
            safeFrame.anchorMin=new Vector2(safe.xMin/pixels.x,safe.yMin/pixels.y);
            safeFrame.anchorMax=new Vector2(safe.xMax/pixels.x,safe.yMax/pixels.y);
            safeFrame.offsetMin=safeFrame.offsetMax=Vector2.zero;
            Vector2 available=safeFrame.rect.size;
            if(available.x<=0 || available.y<=0) { applying=false; return; }
            float scale=Mathf.Min(available.x/DesignWidth,available.y/MinimumHeight);
            float height=available.y/scale;
            design.anchorMin=design.anchorMax=new Vector2(.5f,1); design.pivot=new Vector2(.5f,1);
            design.sizeDelta=new Vector2(DesignWidth,height); design.anchoredPosition=Vector2.zero; design.localScale=Vector3.one*scale;
            bottomPanel.anchorMin=bottomPanel.anchorMax=Vector2.zero; bottomPanel.pivot=Vector2.zero;
            bottomPanel.anchoredPosition=Vector2.zero; bottomPanel.sizeDelta=new Vector2(DesignWidth,BottomHeight);
            int count=additionalSafeFrames == null ? 0 : additionalSafeFrames.Length;
            for(int i=0;i<count;i++) {
                var frame=additionalSafeFrames[i]; var content=additionalDesigns[i]; var bottom=additionalBottomPanels[i];
                frame.anchorMin=safeFrame.anchorMin; frame.anchorMax=safeFrame.anchorMax; frame.offsetMin=frame.offsetMax=Vector2.zero;
                content.anchorMin=content.anchorMax=new Vector2(.5f,1); content.pivot=new Vector2(.5f,1);
                content.sizeDelta=new Vector2(DesignWidth,height); content.anchoredPosition=Vector2.zero; content.localScale=Vector3.one*scale;
                if(bottom) { bottom.anchorMin=bottom.anchorMax=Vector2.zero; bottom.pivot=Vector2.zero; bottom.anchoredPosition=Vector2.zero;
                    bottom.sizeDelta=new Vector2(DesignWidth,BottomHeight); }
            }
            if(battleViewport && battleArt) {
                float topInset=canvasRect.rect.height*(1-safe.yMax/pixels.y);
                float battleHeight=topInset+(height-BottomHeight+65)*scale;
                battleViewport.sizeDelta=new Vector2(canvasRect.rect.width,battleHeight);
                var sprite=battleArt.GetComponent<Image>().sprite;
                float aspect=sprite.rect.width/sprite.rect.height;
                float width=Mathf.Max(canvasRect.rect.width,battleHeight*aspect);
                battleArt.sizeDelta=new Vector2(width,width/aspect);
                battleArt.anchorMin=battleArt.anchorMax=battleArt.pivot=new Vector2(.5f,.5f); battleArt.anchoredPosition=Vector2.zero;
            }
            applying=false;
        }
    }
}
