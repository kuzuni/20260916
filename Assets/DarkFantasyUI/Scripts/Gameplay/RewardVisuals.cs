using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
namespace Moonlit.UI
{
    public static class RewardVisuals
    {
        public enum Kind { Gold,Diamond,SkillTicket,PetTicket,MountTicket,Hammer }
        public static Sprite Ticket(int category) => Resources.Load<Sprite>(category==1?"Moonlit/Popup/PetTicketEgg-v2":category==2?"Moonlit/Popup/MountTicketHoof-v2":"Moonlit/Skills/SummonTicket-v1");
        public static Sprite Icon(MainScreen main,Kind kind)
        {
            if(kind==Kind.Hammer)return RewardsScreenModule.HammerArt;
            if((int)kind>=(int)Kind.SkillTicket && (int)kind<=(int)Kind.MountTicket)return Ticket((int)kind-2);
            var target=kind==Kind.Gold?main.goldButton:main.gemButton;
            if(!target)return null;
            var image=target.transform.Find(kind==Kind.Gold?"Crown coin":"Diamond ruby");
            return image?image.GetComponent<Image>().sprite:null;
        }
        public static void Absorb(MainScreen main,Kind kind,int amount,Vector3? origin=null)
        {
            if(!main || !main.toastRoot || amount<=0)return;
            var root=(RectTransform)main.toastRoot;
            var layer=Ui.Rect("Reward absorption",root,0,0,root.rect.width,root.rect.height);
            var canvas=layer.gameObject.AddComponent<CanvasGroup>();canvas.blocksRaycasts=false;canvas.interactable=false;
            var life=layer.gameObject.AddComponent<RewardVisualLifetime>();
            // Particle anchors use the layer's top-left origin, while the safe-area root is centered.
            var source=origin.HasValue?(Vector2)layer.InverseTransformPoint(origin.Value):new Vector2(root.rect.width*.5f,-root.rect.height*.45f);
            Transform target=null;
            if(kind==Kind.Gold && main.goldButton)target=main.goldButton.transform;
            else if(kind==Kind.Diamond && main.gemButton)target=main.gemButton.transform;
            else if(kind==Kind.Hammer && main.forgeButton)target=main.forgeButton.transform;
            else if(main.navigation!=null && main.navigation.Length>2 && main.navigation[2])target=main.navigation[2].transform;
            if(!target){Object.Destroy(layer.gameObject);return;}
            var targetRect=(RectTransform)target;
            Vector2 destination=layer.InverseTransformPoint(targetRect.TransformPoint(targetRect.rect.center));
            var icon=Icon(main,kind);if(!icon){Object.Destroy(layer.gameObject);return;}
            int count=Mathf.Clamp(amount,5,12);
            for(int i=0;i<count;i++)
            {
                var bit=Ui.Image("Reward particle "+i,layer,0,0,48,48,icon);bit.preserveAspect=true;
                var rect=bit.rectTransform;rect.pivot=Vector2.one*.5f;rect.anchoredPosition=source;
                Vector2 spread=source+new Vector2(Mathf.Cos(i*2.4f)*85,Mathf.Sin(i*2.4f)*60);
                var seq=DOTween.Sequence().SetUpdate(true).SetTarget(life);
                seq.Append(DOTween.To(()=>rect.anchoredPosition,x=>rect.anchoredPosition=x,spread,.18f).SetEase(Ease.OutQuad));
                seq.AppendInterval(.08f+i*.025f);
                seq.Append(DOTween.To(()=>rect.anchoredPosition,x=>rect.anchoredPosition=x,destination,.55f).SetEase(Ease.InQuad));
                seq.Join(DOTween.To(()=>rect.localScale.x,x=>rect.localScale=Vector3.one*x,.4f,.55f));
                seq.OnComplete(()=>{if(bit)Object.Destroy(bit.gameObject);});
            }
            var text=Ui.Text("Reward amount",layer,source.x-180,-source.y-75,360,60,"+"+amount.ToString("N0"),34,main.font,Ui.Gold);
            var label=text.rectTransform;
            var finish=DOTween.Sequence().SetUpdate(true).SetTarget(life);
            finish.Append(DOTween.To(()=>label.anchoredPosition,x=>label.anchoredPosition=x,label.anchoredPosition+Vector2.up*70,.75f));
            finish.AppendInterval(.45f);finish.OnComplete(()=>{if(layer)Object.Destroy(layer.gameObject);});
        }
    }
    public sealed class RewardVisualLifetime:MonoBehaviour
    {
        void OnDestroy(){DOTween.Kill(this);}
    }
}
