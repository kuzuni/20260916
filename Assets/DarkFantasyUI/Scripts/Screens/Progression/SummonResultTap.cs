using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Moonlit.UI
{
    // Clicks bubble through non-interactive result cards; ScrollRect retains drag ownership.
    public sealed class SummonResultTap : MonoBehaviour, IPointerClickHandler
    {
        SummonRevealAnimation reveal;
        Action close;
        SummonResultTap forward;
        int lastTapFrame=-1;
        public void Initialize(SummonRevealAnimation animation,Action dismiss){reveal=animation;close=dismiss;}
        public void ForwardTo(SummonResultTap target){forward=target;}
        public void OnPointerClick(PointerEventData eventData)
        {
            if(forward){forward.OnPointerClick(eventData);return;}
            if(eventData==null || eventData.button!=PointerEventData.InputButton.Left || eventData.dragging)return;
            float threshold=EventSystem.current?EventSystem.current.pixelDragThreshold:10;
            if((eventData.position-eventData.pressPosition).sqrMagnitude>threshold*threshold)return;
            if(lastTapFrame==Time.frameCount)return;
            lastTapFrame=Time.frameCount;
            if(reveal && reveal.IsRevealing)reveal.Complete();
            else close?.Invoke();
        }
        void OnDestroy(){close=null;forward=null;}
    }
}
