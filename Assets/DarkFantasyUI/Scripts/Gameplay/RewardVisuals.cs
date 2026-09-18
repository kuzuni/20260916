using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
namespace Moonlit.UI
{
    public static class RewardVisuals
    {
        public enum Kind { Gold,Diamond,SkillTicket,PetTicket,MountTicket,Hammer,HammerKey,GhostKey,InvasionKey,ZombieKey }
        public const float ParticleSize=144f;
        public static Sprite Ticket(int category) => Resources.Load<Sprite>(category==1?"Moonlit/Popup/PetTicketEgg-v2":category==2?"Moonlit/Popup/MountTicketHoof-v2":"Moonlit/Skills/SummonTicket-v1");
        public static Sprite Icon(MainScreen main,Kind kind)
        {
            if(kind==Kind.Hammer)return RewardsScreenModule.HammerArt;
            if((int)kind>=(int)Kind.HammerKey)return PopupSkin.RewardIcon(new[]{5,2,4,3}[(int)kind-(int)Kind.HammerKey]);
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
            // Rewards are dynamically created after a page canvas is already batched. Give the
            // effect its own sorting boundary and camera, rather than relying on an empty toast
            // ancestor's batch being rebuilt above the opaque shop/page canvas.
            var parentCanvas=root.GetComponentInParent<Canvas>();
            if(parentCanvas)
            {
                var rewardCanvas=layer.gameObject.AddComponent<Canvas>();
                rewardCanvas.overrideSorting=true;
                rewardCanvas.sortingLayerID=parentCanvas.sortingLayerID;
                rewardCanvas.sortingOrder=Mathf.Max(1100,parentCanvas.sortingOrder+1);
                rewardCanvas.worldCamera=parentCanvas.rootCanvas.worldCamera;
                layer.gameObject.layer=parentCanvas.gameObject.layer;
            }
            var canvas=layer.gameObject.AddComponent<CanvasGroup>();
            canvas.alpha=1;canvas.ignoreParentGroups=true;canvas.blocksRaycasts=false;canvas.interactable=false;
            var life=layer.gameObject.AddComponent<RewardVisualLifetime>();
            // Particle anchors use the layer's top-left origin, while the safe-area root is centered.
            var source=origin.HasValue?(Vector2)layer.InverseTransformPoint(origin.Value):new Vector2(root.rect.width*.5f,-root.rect.height*.45f);
            Transform target=null;
            if(kind==Kind.Gold && main.goldButton)target=main.goldButton.transform;
            else if(kind==Kind.Diamond && main.gemButton)target=main.gemButton.transform;
            else if(kind==Kind.Hammer && main.forgeButton)target=main.forgeButton.transform;
            else if((int)kind>=(int)Kind.HammerKey && main.navigation!=null && main.navigation.Length>1 && main.navigation[1])target=main.navigation[1].transform;
            else if(main.navigation!=null && main.navigation.Length>2 && main.navigation[2])target=main.navigation[2].transform;
            if(!target){Object.Destroy(layer.gameObject);return;}
            var targetRect=(RectTransform)target;
            Vector2 destination=layer.InverseTransformPoint(targetRect.TransformPoint(targetRect.rect.center));
            var icon=Icon(main,kind);if(!icon){Object.Destroy(layer.gameObject);return;}
            int count=Mathf.Clamp(amount,5,12);
            for(int i=0;i<count;i++)
            {
                var bit=Ui.Image("Reward particle "+i,layer,0,0,ParticleSize,ParticleSize,icon);bit.preserveAspect=true;
                bit.gameObject.layer=layer.gameObject.layer;
                bit.maskable=false; // A reward must remain visible after it leaves the shop scroll viewport.
                var rect=bit.rectTransform;rect.pivot=Vector2.one*.5f;rect.anchoredPosition=source;
                Vector2 spread=source+new Vector2(Mathf.Cos(i*2.4f)*85,Mathf.Sin(i*2.4f)*60);
                var seq=life.Sequence();
                seq.Append(DOTween.To(()=>rect.anchoredPosition,x=>rect.anchoredPosition=x,spread,.18f).SetEase(Ease.OutQuad));
                seq.AppendInterval(.08f+i*.025f);
                seq.Append(DOTween.To(()=>rect.anchoredPosition,x=>rect.anchoredPosition=x,destination,.55f).SetEase(Ease.InQuad));
                seq.Join(DOTween.To(()=>rect.localScale.x,x=>rect.localScale=Vector3.one*x,.4f,.55f));
                seq.OnComplete(()=>{if(bit)Object.Destroy(bit.gameObject);});
            }
            var text=Ui.Text("Reward amount",layer,source.x-180,-source.y-75,360,60,"+"+amount.ToString("N0"),34,main.font,Ui.Gold);
            text.gameObject.layer=layer.gameObject.layer;text.maskable=false;
            var label=text.rectTransform;
            var finish=life.Sequence();
            finish.Append(DOTween.To(()=>label.anchoredPosition,x=>label.anchoredPosition=x,label.anchoredPosition+Vector2.up*70,.75f));
            finish.AppendInterval(.45f);finish.OnComplete(()=>{if(layer)Object.Destroy(layer.gameObject);});
        }
    }
    public sealed class RewardVisualLifetime:MonoBehaviour
    {
        readonly System.Collections.Generic.List<Sequence> animations=new System.Collections.Generic.List<Sequence>();
        double previousFrame;
        float displayedAge;
        bool held;
        // A reward must be displayed, not expire unseen during resource uploads or a slow render.
        // At low frame rates retain the short burst and flight instead of jumping directly to destruction.
        public const float MaximumFrameAdvance = .08f;
        void Awake(){previousFrame=Time.realtimeSinceStartupAsDouble;}
        public Sequence Sequence()
        {
            // A tween born late in a slow render frame must not consume that entire frame's delta.
            // Manual sampling counts only real time since this effect was actually created.
            var sequence=DOTween.Sequence().SetUpdate(UpdateType.Manual,true).SetTarget(this);
            animations.Add(sequence);return sequence;
        }
        void Update()
        {
            double now=Time.realtimeSinceStartupAsDouble;
            float step=Mathf.Clamp((float)(now-previousFrame),0,MaximumFrameAdvance);
            previousFrame=now;
            if(!held)SampleAge(displayedAge+step);
        }
        public void SampleAge(float seconds)
        {
            displayedAge=Mathf.Max(0,seconds);
            foreach(var animation in animations)
                if(animation.IsActive() && animation.IsPlaying())animation.Goto(displayedAge,true);
        }
        public void HoldAt(float seconds)
        {
            held=true;displayedAge=Mathf.Max(0,seconds);
            foreach(var animation in animations)
                if(animation.IsActive())animation.Goto(displayedAge,false);
        }
        public void Resume()
        {
            held=false;previousFrame=Time.realtimeSinceStartupAsDouble;
            foreach(var animation in animations)if(animation.IsActive())animation.Play();
        }
        void OnDestroy(){DOTween.Kill(this);animations.Clear();}
    }
}
