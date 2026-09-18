using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Moonlit.UI
{
    public sealed class ForgeRuntime : MonoBehaviour
    {
        public MainScreen main;
        public bool Busy { get; private set; }
        public event Action HandRevealed;
        bool awaitingComparison;
        readonly System.Random random=new System.Random();
        readonly Dictionary<int,ItemDefinition> definitions=new Dictionary<int,ItemDefinition>();
        public static ForgeRuntime Ensure(MainScreen main)
        {
            var runtime=main.GetComponent<ForgeRuntime>();
            if(!runtime) { runtime=main.gameObject.AddComponent<ForgeRuntime>();runtime.main=main; }
            return runtime;
        }
        void Update()
        {
            if(!main)return;
            main.autoForge=ForgeState.Current.autoEnabled;
            main.forgeLevel=ForgeState.Current.level;
            if(!Busy && ForgeState.Current.autoEnabled && main.screens!=null && main.screens.ModalDepth==0) {
                if(awaitingComparison && ForgeState.Current.Pending!=null)return;
                awaitingComparison=false;
                if(ForgeState.Current.ShouldCompare) PresentComparison();
                else StartCoroutine(Cycle(true));
            }
        }
        public void BeginManual()
        {
            if(Busy)return;
            var state=ForgeState.Current;
            state.autoEnabled=false;
            if(state.Pending!=null) { main.screens.Open("forge-comparison");return; }
            StartCoroutine(Cycle(false));
        }
        void PresentComparison()
        {
            awaitingComparison=true;
            if(main.screens.ModalDepth==0)main.screens.Open("forge-comparison");
        }
        public void StartAuto()
        {
            awaitingComparison=false;
            ForgeState.Current.autoEnabled=true;main.autoForge=true;main.Refresh();
        }
        public void StopAuto()
        {
            ForgeState.Current.autoEnabled=false;main.autoForge=false;main.Refresh();
            awaitingComparison=false;
        }
        IEnumerator Cycle(bool automatic)
        {
            var state=ForgeState.Current;
            int count=automatic ? Math.Max(1,Math.Min(99,state.batchSize)) : 1;
            count=Math.Min(count,main.ore);
            if(count<=0) {
                state.autoEnabled=false;main.autoForge=false;main.Toast("망치가 부족합니다.");
                if(state.Pending!=null)main.screens.Open("forge-comparison");yield break;
            }
            Busy=true; main.forgeButton.interactable=false;
            main.ore-=count;
            var items=new List<EquipmentRoll>();
            for(int i=0;i<count;i++) { var item=state.Draw(random);items.Add(item);state.pending.Add(item); }
            if(automatic)state.TrackAutoBatch(items);
            main.successfulForges+=count;main.Refresh();
            var hammer=ForgeHammerMotion.Play(main.forgeButton.transform);
            double anvilStarted=Time.realtimeSinceStartupAsDouble;
            while(Time.realtimeSinceStartupAsDouble-anvilStarted<1) {
                hammer.Sample(Time.realtimeSinceStartupAsDouble-anvilStarted);
                yield return null;
            }
            hammer.Sample(1);Destroy(hammer.gameObject);
            var host=(RectTransform)main.design;
            int columns=count<=12?count:count<=24?Mathf.CeilToInt(count/2f):22;
            int rows=Mathf.CeilToInt(count/(float)columns);
            float size=Mathf.Min(118,940/(1+(columns-1)*.5f)),cardHeight=size*1.4f,pitch=cardHeight*.75f;
            float handHeight=(rows-1)*pitch+cardHeight;
            var anvilRect=(RectTransform)main.forgeButton.transform;
            var corners=new Vector3[4];anvilRect.GetWorldCorners(corners);
            float anvilTop=host.rect.yMax-host.InverseTransformPoint(corners[1]).y;
            float revealY=Mathf.Max(24,anvilTop-handHeight-18);
            var cards=Ui.Rect("Forged equipment hand",host,40,revealY,1000,handHeight+60);
            for(int i=0;i<count;i++) {
                var item=items[i];int row=i/columns,col=i%columns,n=Math.Min(columns,count-row*columns);
                float step=size*.5f,left=(1000-(n-1)*step-size)*.5f;
                var frame=main.equipment!=null && main.equipment.Length>0 && main.equipment[0] ? main.equipment[0].equipmentFrame : PopupSkin.PanelArt;
                var card=Ui.Image("Forged card "+item.id,cards,left+col*step,row*pitch,size,cardHeight,frame);
                EquipmentPictograms.TintFrame(card,EquipmentRules.TierColor(item.tier));
                card.type=Image.Type.Sliced;card.pixelsPerUnitMultiplier=8;
                Ui.Image("Equipment thumbnail",card.transform,8,8,size-16,size-16,EquipmentArt.Icon(item)).preserveAspect=true;
                Ui.Text("Level",card.transform,2,size,size-4,cardHeight-size,"Lv."+item.level,Mathf.RoundToInt(size*.19f),main.font);
            }

            HandRevealed?.Invoke();
            yield return new WaitForSecondsRealtime(.5f);
            Destroy(cards.gameObject);
            int sold=0;
            if(automatic) {
                sold=state.SettleAutoBatch(ref main.gold);
                main.Refresh();
                if(sold>0) {
                    RewardVisuals.Absorb(main,RewardVisuals.Kind.Gold,sold,main.forgeButton.transform.position);

                    var effect=Ui.Text("Gold sale effect",host,220,revealY,640,80,"골드 +"+sold,44,main.font,Ui.Gold);
                    var effectGroup=effect.gameObject.AddComponent<CanvasGroup>();effectGroup.blocksRaycasts=false;
                    var coinRoot=Ui.Rect("Gold coin burst",host,220,host.rect.height*.42f,640,130);
                    var coinGroup=coinRoot.gameObject.AddComponent<CanvasGroup>();coinGroup.blocksRaycasts=false;
                    var source=main.goldButton?main.goldButton.transform.Find("Crown coin"):null;
                    var coinSprite=source?source.GetComponent<Image>().sprite:null;
                    var coins=new RectTransform[coinSprite?8:0];
                    for(int i=0;i<coins.Length;i++) {
                        var coin=Ui.Image("Sale coin "+i,coinRoot,290,30,46,46,coinSprite);
                        coin.preserveAspect=true;coins[i]=coin.rectTransform;
                    }
                    float t=0;
                    while(t<.55f) {
                        t+=Time.unscaledDeltaTime;float progress=Mathf.Clamp01(t/.55f);
                        effect.rectTransform.anchoredPosition+=Vector2.up*Time.unscaledDeltaTime*140;effectGroup.alpha=1-progress;
                        coinGroup.alpha=1-progress;
                        for(int i=0;i<coins.Length;i++) {
                            float spread=(i-(coins.Length-1)*.5f)*44;
                            coins[i].anchoredPosition=new Vector2(290+spread*progress,-30+Mathf.Sin(progress*Mathf.PI)*100+progress*90);
                            coins[i].localRotation=Quaternion.Euler(0,0,(i%2==0?1:-1)*progress*220);
                        }
                        yield return null;
                    }
                    Destroy(coinRoot.gameObject);Destroy(effect.gameObject);
                }
            }
            Busy=false;main.forgeButton.interactable=true;
            if(main.ore==0){state.autoEnabled=false;main.autoForge=false;}
            main.Refresh();
            if(!automatic || state.ShouldCompare || !state.autoEnabled || main.ore==0) {
                if(state.Pending!=null)PresentComparison();
            }
        }
        public ItemDefinition Definition(EquipmentRoll item)
        {
            if(item==null)return null;
            if(definitions.TryGetValue(item.id,out var cached) && cached)return cached;
            var d=ScriptableObject.CreateInstance<ItemDefinition>();d.name=item.Name;d.displayName=item.Name;
            d.icon=EquipmentArt.Icon(item);d.rarity=ItemRarity.Common;d.startingLevel=item.level;d.baseHealth=item.Stats.health;
            d.description=EquipmentRules.PartNames[(int)item.part];definitions[item.id]=d;return d;
        }
        public void SyncSlots()
        {
            if(main.equipment==null)return;
            for(int i=0;i<Math.Min(6,main.equipment.Length);i++) {
                var slot=main.equipment[i];if(!slot)continue;
                var item=ForgeState.Current.equipped[i];
                slot.roll=item;slot.Bind(Definition(item),item!=null?item.level:0);
                if(item!=null)slot.frame.color=EquipmentRules.TierColor(item.tier);
            }
        }
        void OnDestroy() {foreach(var item in definitions.Values)if(item)Destroy(item);}
    }
}
