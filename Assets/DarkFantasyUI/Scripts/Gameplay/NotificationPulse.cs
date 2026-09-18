using DG.Tweening;
using UnityEngine;
namespace Moonlit.UI
{
    public sealed class NotificationPulse : MonoBehaviour
    {
        Tween pulse;
        void Awake()
        {
            var rect=transform as RectTransform;
            if(rect){var delta=new Vector2(.5f,.5f)-rect.pivot;rect.anchoredPosition+=Vector2.Scale(delta,rect.rect.size);rect.pivot=new Vector2(.5f,.5f);}
        }
        void OnEnable()
        {
            transform.localScale=Vector3.one;
            pulse=DOTween.To(()=>transform.localScale,v=>transform.localScale=v,Vector3.one*1.2f,.55f)
                .SetEase(Ease.InOutSine).SetLoops(-1,LoopType.Yoyo).SetUpdate(true).SetTarget(this);
        }
        void OnDisable(){if(pulse!=null)pulse.Kill();pulse=null;transform.localScale=Vector3.one;}
        void OnDestroy(){if(pulse!=null)pulse.Kill();}
        public static void Ensure(GameObject dot){if(dot && !dot.GetComponent<NotificationPulse>())dot.AddComponent<NotificationPulse>();}
    }
}
