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
                if(ForgeState.Current.ShouldCompare) { ForgeState.Current.autoEnabled=false;main.screens.Open("forge-comparison"); }
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
        public void StartAuto()
        {
            ForgeState.Current.autoEnabled=true;main.autoForge=true;main.Refresh();
        }
        public void StopAuto()
        {
            ForgeState.Current.autoEnabled=false;main.autoForge=false;main.Refresh();
            if(!Busy && ForgeState.Current.Pending!=null && main.screens!=null && main.screens.ModalDepth==0)main.screens.Open("forge-comparison");
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
            main.successfulForges+=count;main.Refresh();
            var anvil=main.forgeButton.transform;
            var originalScale=anvil.localScale;
            float elapsed=0;
            while(elapsed<1) {
                elapsed+=Time.unscaledDeltaTime;
                anvil.localScale=originalScale*(1+.07f*Mathf.Sin(elapsed*28)*Mathf.Sin(elapsed*Mathf.PI));
                yield return null;
            }
            anvil.localScale=originalScale;
            var host=(RectTransform)main.design;
            var cards=Ui.Rect("Forged equipment hand",host,40,host.rect.height*.39f,1000,360);
            int rows=Mathf.CeilToInt(count/22f);
            for(int i=0;i<count;i++) {
                var item=items[i];int row=i/22,col=i%22,n=Math.Min(22,count-row*22);
                float size=118,step=Mathf.Min(60,(940-size)/Mathf.Max(1,n-1));
                var card=Ui.Image("Forged card "+item.id,cards,30+col*step,row*58,size,166,PopupSkin.PanelArt,EquipmentRules.TierColor(item.tier));
                card.type=Image.Type.Sliced;card.pixelsPerUnitMultiplier=8;
                Ui.Image("Equipment thumbnail",card.transform,8,12,102,102,EquipmentArt.Icon(item)).preserveAspect=true;
                Ui.Text("Level",card.transform,2,122,114,38,"Lv."+item.level,22,main.font);
            }
            Ui.Text("Batch count",cards,20,rows*58+170,940,44,count+"개 제작 · 보관 "+state.pending.Count+"개",28,main.font);
            yield return new WaitForSecondsRealtime(.5f);
            Destroy(cards.gameObject);
            int sold=0;
            if(automatic) {
                foreach(var item in items)if(!state.Matches(item)) { sold+=EquipmentRules.SaleGold(item);state.pending.Remove(item); }
                if(sold>0) {
                    main.gold=(int)Math.Min(int.MaxValue,(long)main.gold+sold);main.Refresh();
                    var effect=Ui.Text("Gold sale effect",host,220,host.rect.height*.42f,640,80,"골드 +"+sold,44,main.font,Ui.Gold);
                    var effectGroup=effect.gameObject.AddComponent<CanvasGroup>();effectGroup.blocksRaycasts=false;
                    float t=0;
                    while(t<.55f) {t+=Time.unscaledDeltaTime;effect.rectTransform.anchoredPosition+=Vector2.up*Time.unscaledDeltaTime*140;effectGroup.alpha=1-t/.55f;yield return null;}
                    Destroy(effect.gameObject);
                }
            }
            Busy=false;main.forgeButton.interactable=true;main.Refresh();
            if(!automatic || state.ShouldCompare || !state.autoEnabled || main.ore==0) {
                state.autoEnabled=false;main.autoForge=false;
                if(state.Pending!=null)main.screens.Open("forge-comparison");
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
