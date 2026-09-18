using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    /// <summary>Animates only a new wave; repeated HUD refreshes preserve the running transition.</summary>
    public sealed class StageWaveProgress : MonoBehaviour
    {
        Image[] nodes=new Image[0], lines=new Image[0];
        RectTransform[] pulses=new RectTransform[0];
        float[] widths=new float[0];
        Vector3[] scales=new Vector3[0];
        Sequence transition;
        int currentStage, currentWave;
        bool hasProgress;

        public void Initialize(Image[] nodeImages,Image[] lineImages,RectTransform[] pulseRoots)
        {
            StopTransition();
            nodes=nodeImages??new Image[0];
            lines=lineImages??new Image[0];
            pulses=pulseRoots??new RectTransform[0];
            widths=new float[lines.Length];
            for(int i=0;i<lines.Length;i++)if(lines[i]) {
                var rect=lines[i].rectTransform;
                widths[i]=rect.rect.width;
                // Preserve the painted left edge if a caller supplied a centered pivot.
                float shift=widths[i]*rect.pivot.x;
                rect.pivot=new Vector2(0,rect.pivot.y);
                rect.anchoredPosition-=new Vector2(shift,0);
                lines[i].color=Ui.Cyan;
                SetWidth(i,0);
            }
            scales=new Vector3[pulses.Length];
            for(int i=0;i<pulses.Length;i++)scales[i]=pulses[i]?pulses[i].localScale:Vector3.one;
            hasProgress=false;
            SetProgress(1,1);
        }

        public void SetProgress(int stage,int wave)
        {
            stage=Mathf.Max(1,stage);
            wave=Mathf.Clamp(wave,1,Mathf.Max(1,nodes.Length));
            if(hasProgress && stage==currentStage && wave==currentWave)return;
            bool animate=hasProgress && stage==currentStage && wave>currentWave && isActiveAndEnabled;
            StopTransition();
            currentStage=stage;currentWave=wave;hasProgress=true;
            SnapToProgress();
            if(!animate)return;
            int lineIndex=wave-2;
            if(lineIndex<0 || lineIndex>=lines.Length || !lines[lineIndex])return;
            SetWidth(lineIndex,0);
            int nodeIndex=wave-1;
            if(nodes[nodeIndex])nodes[nodeIndex].color=new Color(0,.24f,.34f);
            var line=lines[lineIndex].rectTransform;
            float targetWidth=widths[lineIndex];
            transition=DOTween.Sequence().SetUpdate(true).SetTarget(this);
            transition.Append(DOTween.To(()=>line.rect.width,
                value=>{
                    line.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,value);
                    if(nodes[nodeIndex])nodes[nodeIndex].color=value>=targetWidth-.01f?Ui.Cyan:new Color(0,.24f,.34f);
                },
                targetWidth,.4f).SetEase(Ease.OutQuad));
            if(nodeIndex<pulses.Length && pulses[nodeIndex]) {
                var pulse=pulses[nodeIndex];var original=scales[nodeIndex];
                transition.Append(DOTween.To(()=>pulse.localScale,value=>pulse.localScale=value,
                    original*1.22f,.12f).SetEase(Ease.OutQuad));
                transition.Append(DOTween.To(()=>pulse.localScale,value=>pulse.localScale=value,
                    original,.18f).SetEase(Ease.InOutQuad));
            }
        }

        void SetWidth(int index,float width)
        {
            if(lines[index])lines[index].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width);
        }
        void SnapToProgress()
        {
            for(int i=0;i<nodes.Length;i++)if(nodes[i])
                nodes[i].color=i+1<=currentWave?Ui.Cyan:new Color(0,.24f,.34f);
            for(int i=0;i<lines.Length;i++)SetWidth(i,i<currentWave-1?widths[i]:0);
            for(int i=0;i<pulses.Length;i++)if(pulses[i])pulses[i].localScale=scales[i];
        }
        void StopTransition()
        {
            if(transition!=null && transition.IsActive())transition.Kill();
            transition=null;
        }
        void OnDisable()
        {
            StopTransition();
            if(hasProgress)SnapToProgress();
        }
        void OnDestroy(){StopTransition();}
    }
}
