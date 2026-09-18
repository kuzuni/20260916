using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    // Animates presentation only; battle owns turn counters and records actual skill impacts.
    public sealed class SkillHudFeedback : MonoBehaviour
    {
        Image icon,shade,flash;
        Text label;
        CollectionEntry entry;
        object battleState;
        int remaining,activations;
        bool initialized;
        float targetFill;
        Tween cooldownTween;
        Sequence turnTween,activationTween;

        public void Initialize(Image skillIcon,Image cooldownShade,Image activationFlash,Text turnLabel)
        {
            icon=skillIcon;shade=cooldownShade;flash=activationFlash;label=turnLabel;
            CenterPivot(icon.rectTransform);CenterPivot(label.rectTransform);
            SetFlash(0);
        }
        static void CenterPivot(RectTransform rect)
        {
            var pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition+=Vector2.Scale(pivot-rect.pivot,rect.rect.size);
            rect.pivot=pivot;
        }
        public void Apply(CollectionEntry current,object state,int turnsRemaining,int activationCount)
        {
            if(current==null || !icon || !shade || !label)return;
            turnsRemaining=Mathf.Clamp(turnsRemaining,0,current.Cooldown);
            targetFill=turnsRemaining/(float)current.Cooldown;
            if(!initialized || !ReferenceEquals(entry,current) || !ReferenceEquals(battleState,state)) {
                StopAnimations();entry=current;battleState=state;remaining=turnsRemaining;activations=activationCount;
                initialized=true;shade.fillAmount=targetFill;SetLabel(turnsRemaining);return;
            }
            if(turnsRemaining!=remaining) {
                remaining=turnsRemaining;SetLabel(remaining);
                if(cooldownTween!=null)cooldownTween.Kill();
                cooldownTween=DOTween.To(()=>shade.fillAmount,v=>shade.fillAmount=v,targetFill,.22f)
                    .SetEase(Ease.OutCubic).SetUpdate(true).SetTarget(this);
                if(turnTween!=null)turnTween.Kill();
                label.transform.localScale=Vector3.one;
                turnTween=DOTween.Sequence().SetUpdate(true).SetTarget(this);
                turnTween.Append(DOTween.To(()=>label.transform.localScale,v=>label.transform.localScale=v,Vector3.one*1.18f,.1f).SetEase(Ease.OutQuad));
                turnTween.Append(DOTween.To(()=>label.transform.localScale,v=>label.transform.localScale=v,Vector3.one,.16f).SetEase(Ease.OutQuad));
            }
            if(activationCount>activations)PlayActivation();
            activations=activationCount;
        }
        void SetLabel(int turns) {label.text=turns==0?"발동":turns+"턴";}
        void PlayActivation()
        {
            if(activationTween!=null)activationTween.Kill();
            icon.transform.localScale=Vector3.one;SetFlash(.95f);
            activationTween=DOTween.Sequence().SetUpdate(true).SetTarget(this);
            activationTween.Append(DOTween.To(()=>icon.transform.localScale,v=>icon.transform.localScale=v,Vector3.one*1.22f,.12f).SetEase(Ease.OutQuad));
            activationTween.Append(DOTween.To(()=>icon.transform.localScale,v=>icon.transform.localScale=v,Vector3.one,.3f).SetEase(Ease.OutBack));
            activationTween.Insert(0,DOTween.To(()=>flash.color.a,SetFlash,0f,.42f).SetEase(Ease.OutQuad));
        }
        void SetFlash(float alpha) {if(flash)flash.color=new Color(1,1,1,alpha);}
        void StopAnimations()
        {
            if(cooldownTween!=null)cooldownTween.Kill();
            if(turnTween!=null)turnTween.Kill();
            if(activationTween!=null)activationTween.Kill();
            cooldownTween=null;turnTween=null;activationTween=null;
            if(icon)icon.transform.localScale=Vector3.one;
            if(label)label.transform.localScale=Vector3.one;
            if(shade)shade.fillAmount=targetFill;
            SetFlash(0);
        }
        void OnDisable(){StopAnimations();initialized=false;}
        void OnDestroy(){StopAnimations();}
    }
}
