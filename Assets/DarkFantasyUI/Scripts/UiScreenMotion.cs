using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    /// <summary>Animate visibility only, preserving Safe Area geometry, hit areas and modal input ownership.</summary>
    public sealed class UiScreenMotion : MonoBehaviour
    {
        readonly List<CanvasGroup> targets=new List<CanvasGroup>();
        Sequence sequence;
        public bool Sequential { get; private set; }
        public bool IsPlaying => sequence!=null && sequence.IsActive() && !sequence.IsComplete();
        public IReadOnlyList<CanvasGroup> Targets => targets;
        public static UiScreenMotion Play(RectTransform root,bool sequential)
        {
            var motion=root.GetComponent<UiScreenMotion>()??root.gameObject.AddComponent<UiScreenMotion>();
            motion.Begin(root,sequential);return motion;
        }
        void Begin(RectTransform root,bool sequential)
        {
            Complete();targets.Clear();Sequential=sequential;
            if(sequential) {
                var elements=new List<RectTransform>();
                Collect(root,elements,false);
                elements.Sort((a,b)=>{
                    var pa=root.InverseTransformPoint(a.TransformPoint(a.rect.center));
                    var pb=root.InverseTransformPoint(b.TransformPoint(b.rect.center));
                    int row=Mathf.RoundToInt(-pa.y/48).CompareTo(Mathf.RoundToInt(-pb.y/48));
                    return row!=0?row:pa.x.CompareTo(pb.x);
                });
                foreach(var element in elements)targets.Add(Group(element));
            } else targets.Add(Group(root));
            sequence=DOTween.Sequence().SetUpdate(true).SetTarget(this);
            float gap=sequential?Mathf.Min(.075f,.5f/Mathf.Max(1,targets.Count-1)):0;
            for(int i=0;i<targets.Count;i++) {
                var group=targets[i];group.alpha=0;
                sequence.Insert(i*gap,DOTween.To(()=>group?group.alpha:1,value=>{if(group)group.alpha=value;},1f,.22f).SetEase(Ease.OutCubic));
            }
            if(targets.Count==0)Complete();
        }
        static CanvasGroup Group(RectTransform rect)
            => rect.GetComponent<CanvasGroup>()??rect.gameObject.AddComponent<CanvasGroup>();
        static void Collect(RectTransform node,List<RectTransform> result,bool include)
        {
            if(!node || !node.gameObject.activeInHierarchy)return;
            var scroll=node.GetComponent<ScrollRect>();
            if(scroll && scroll.content) {
                foreach(Transform child in scroll.content)Collect(child as RectTransform,result,true);
                return;
            }
            if(include && (node.GetComponent<Graphic>() || node.GetComponent<Selectable>())) {
                result.Add(node);return;
            }
            foreach(Transform child in node)Collect(child as RectTransform,result,true);
        }
        public void Complete()
        {
            if(sequence!=null){sequence.Kill();sequence=null;}
            foreach(var target in targets)if(target)target.alpha=1;
        }
        void OnDisable(){Complete();}
        void OnDestroy(){Complete();targets.Clear();}
    }
}
