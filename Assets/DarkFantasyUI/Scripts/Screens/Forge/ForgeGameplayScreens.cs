using System;
using UnityEngine;
using UnityEngine.UI;
namespace Moonlit.UI
{
    public static partial class ForgeScreenModule
    {
        static void Watch(RectTransform root, Action refresh)
        {
            var live=root.gameObject.AddComponent<ForgeLiveView>();live.refresh=refresh;refresh();
        }
        static string StatText(EquipmentRoll item)
        {
            if(item==null)return "장착된 장비 없음";
            var s=item.Stats;
            string result=s.health>0 ? "체력 "+EquipmentRules.Number(s.health)+"  ·  스피드 "+EquipmentRules.Number(s.speed) : "공격력 "+EquipmentRules.Number(s.attack);
            foreach(var a in item.affixes) result+="\n"+EquipmentRules.AffixNames[(int)a.kind]+" +"+a.percent+"%";
            return result;
        }
        static RectTransform RollCard(ScreenContext c,Transform parent,float y,EquipmentRoll item,string label)
        {
            var panel=Ui.Image(label,parent,12,y,728,244,PopupSkin.PanelArt,item!=null?EquipmentRules.TierColor(item.tier):Color.gray);
            panel.type=Image.Type.Sliced;panel.pixelsPerUnitMultiplier=9;
            var icon=EquipmentArt.Icon(item);
            if(icon)Ui.Image("Equipment icon",panel.transform,14,34,155,155,icon).preserveAspect=true;
            else Ui.Text("Artwork status",panel.transform,15,64,150,100,item==null?"빈 슬롯":"썸네일\n준비 중",23,Font(c));
            Ui.Text("Label",panel.transform,182,8,525,35,label,22,Font(c),Ui.Gold,TextAnchor.MiddleLeft);
            Ui.Text("Name",panel.transform,182,45,525,42,item!=null?item.Name:"장착된 장비 없음",26,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            Ui.Text("Level",panel.transform,16,195,152,40,item!=null?"Lv."+item.level:"",27,Font(c));
            Ui.Text("Stats",panel.transform,182,96,526,141,StatText(item),25,Font(c),Ui.Ivory,TextAnchor.UpperLeft);
            return panel.rectTransform;
        }
        static void BuildProbabilityLive(ScreenContext c)
        {
            ForgeRuntime.Ensure(c.Main);
            Frame(c,"확률 정보",820,1450,out var body);
            Scroll(c,body,0,0,body.rect.width,body.rect.height-20,1260,out var b);
            Action(c,b,650,0,58,58,"i",()=>c.Open("forge-probability-details"),Slate);
            var levels=Ui.Text("Levels",b,16,0,620,54,"",28,Font(c));
            var wallet=Ui.Text("Wallet",b,18,60,680,48,"",27,Font(c),Ui.Gold);
            var current=new Text[10];var next=new Text[10];
            for(int i=0;i<10;i++) {
                var row=Ui.Image("Rarity "+EquipmentRules.TierNames[i],b,8,124+i*68,704,60,TierBand(i));row.type=Image.Type.Sliced;row.pixelsPerUnitMultiplier=1.4f;
                Ui.Image("Tier icon",row.transform,7,2,56,56,TierIcon(i)).preserveAspect=true;
                Ui.Text("Name",row.transform,73,2,300,54,EquipmentRules.TierNames[i],27,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
                current[i]=Ui.Text("Current",row.transform,420,2,112,54,"",26,Font(c));
                next[i]=Ui.Text("Next",row.transform,558,2,126,54,"",26,Font(c),Ui.Cyan);
            }
            var status=Ui.Text("Upgrade status",b,12,817,704,58,"",27,Font(c));
            var segments=new Image[6];
            for(int i=0;i<6;i++)segments[i]=Ui.Image("Gold segment "+i,b,26+i*112,886,100,40,PopupSkin.ActionArt);
            Button primary=null,diamond=null,free=null;
            primary=Action(c,b,40,954,642,90,"업그레이드",()=>{
                var s=ForgeState.Current;var now=DateTime.UtcNow;bool changed=false;
                switch(s.UpgradePhase(now)) {
                    case 0: changed=s.FillSegment(ref c.Main.gold);break;
                    case 1: changed=s.StartUpgrade(now);break;
                    case 3: changed=s.ClaimUpgrade(now);break;
                }
                if(!changed)c.Toast("골드가 부족하거나 아직 완료되지 않았습니다.");
                c.Main.forgeLevel=s.level;c.Main.Refresh();
            });
            diamond=Action(c,b,26,1070,326,94,"건너뛰기",()=>{
                if(!ForgeState.Current.DiamondSkip(DateTime.UtcNow,ref c.Main.gems))c.Toast("다이아가 부족합니다.");
                c.Main.Refresh();
            });
            free=Action(c,b,368,1070,326,94,"30분 스킵",()=>{
                if(!ForgeState.Current.FreeSkip(DateTime.UtcNow))c.Toast("오늘의 무료 스킵을 모두 사용했습니다.");
                c.Main.Refresh();
            });
            Ui.Text("Skip rate",b,22,1190,686,54,"다이아 10개 / 1분 · 무료 30분 하루 4회",22,Font(c));
            Watch(b,()=>{
                var s=ForgeState.Current;var now=DateTime.UtcNow;s.ResetDaily(now);int phase=s.UpgradePhase(now);
                levels.text="대장간 레벨 "+s.level+(s.level<35?"  ▶  레벨 "+(s.level+1):" · 최고 레벨");
                wallet.text="골드 "+c.Main.gold.ToString("N0")+"    다이아 "+c.Main.gems;
                var rates=EquipmentRules.TierProbabilities(s.level);var future=EquipmentRules.TierProbabilities(s.level+1);
                for(int i=0;i<10;i++){current[i].text=(rates[i]*100).ToString("0.##")+"%";next[i].text=(future[i]*100).ToString("0.##")+"%";}
                for(int i=0;i<6;i++){segments[i].gameObject.SetActive(i<s.Segments && phase!=4);segments[i].color=i<s.filledSegments?Ui.Cyan:new Color(.18f,.22f,.28f);}
                status.text=phase==0?"골드 업그레이드 "+s.filledSegments+" / "+s.Segments:phase==1?"게이지 완료 · 시간 업그레이드를 시작하세요":phase==2?"업그레이드 중 "+TimeSpan.FromSeconds(s.RemainingSeconds(now)).ToString(@"hh\:mm\:ss"):phase==3?"시간 업그레이드 완료":"대장간 최고 레벨 35";
                primary.GetComponentInChildren<Text>().text=phase==0?"골드 업그레이드 · "+s.SegmentCost:phase==1?"시간 업그레이드 시작":phase==2?"업그레이드 중":phase==3?"업그레이드 완료":"최고 레벨";
                primary.interactable=phase==0 || phase==1 || phase==3;
                diamond.gameObject.SetActive(phase==2);free.gameObject.SetActive(phase==2);
                diamond.GetComponentInChildren<Text>().text="건너뛰기\n다이아 "+s.DiamondSkipCost(now);
                free.GetComponentInChildren<Text>().text="30분 무료 스킵\n"+(4-s.freeSkipsUsed)+" / 4";free.interactable=s.freeSkipsUsed<4;
            });
        }
        static void BuildProbabilityDetailsLive(ScreenContext c)
        {
            Frame(c,"모든 장비의 목록",820,1380,out var b);
            Scroll(c,b,0,0,b.rect.width,b.rect.height-12,10*860,out var content);
            var rates=EquipmentRules.TierProbabilities(ForgeState.Current.level);
            for(int tier=0;tier<10;tier++) {
                int top=tier*860;
                var head=Ui.Image("Tier "+tier,content,6,top,724,66,TierBand(tier));head.type=Image.Type.Sliced;head.pixelsPerUnitMultiplier=1.4f;
                Ui.Image("Tier emblem",head.transform,10,3,60,60,TierIcon(tier)).preserveAspect=true;
                Ui.Text("Tier name",head.transform,86,0,450,66,EquipmentRules.TierNames[tier],29,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
                Ui.Text("Tier chance",head.transform,560,0,152,66,(rates[tier]*100).ToString("0.##")+"%",27,Font(c));
                for(int n=0;n<18;n++) {
                    var item=new EquipmentRoll {tier=tier,level=Math.Max(1,ForgeState.Current.draws[tier]),variant=n/6,part=(EquipmentPart)(n%6)};
                    float x=15+n%4*180,y=top+82+n/4*151;
                    var slot=Action(c,content,x,y,155,112,"",()=>c.Open("forge-item-details",item),EquipmentRules.TierColor(tier));
                    slot.name="Equipment "+item.tier+" "+item.variant+" "+item.part;
                    var icon=EquipmentArt.Icon(item);
                    if(icon)Ui.Image("Thumbnail",slot.transform,14,7,126,92,icon).preserveAspect=true;
                    else Ui.Text("Pending art",slot.transform,6,20,143,74,"썸네일\n준비 중",21,Font(c));
                    Ui.Text("Part",content,x,y+111,155,37,EquipmentRules.VariantNames[item.variant]+" "+EquipmentRules.PartNames[(int)item.part],20,Font(c));
                }
            }
        }
        static void BuildItemDetailsLive(ScreenContext c)
        {
            var item=c.Payload as EquipmentRoll ?? new EquipmentRoll();
            Frame(c,"장비 정보",820,1120,out var b);
            RollCard(c,b,0,item,"장비 도감");
            Ui.Text("Affix count",b,28,270,682,62,"추가 옵션 "+EquipmentRules.AffixCount(item.tier)+"개 · 중복 없음",28,Font(c),Ui.Gold);
            string ranges="";
            for(int i=0;i<9;i++)ranges+=EquipmentRules.AffixNames[i]+"  +1 ~ "+EquipmentRules.AffixMaximums[i]+"%\n";
            Ui.Text("Affix ranges",b,42,350,674,520,ranges,27,Font(c),Ui.Ivory,TextAnchor.UpperLeft);
        }
        static void BuildEquipmentDetailsLive(ScreenContext c)
        {
            var slot=c.Payload as EquipmentSlot;
            var item=c.Payload as EquipmentRoll ?? (slot!=null ? slot.roll : null);
            Frame(c,"장비 세부정보",820,590,out var b);
            RollCard(c,b,12,item,"장착됨");
            Action(c,b,170,284,410,82,"장비 목록",()=>c.Open("forge-probability-details"));
        }
        static void BuildComparisonLive(ScreenContext c)
        {
            ForgeRuntime.Ensure(c.Main);
            Frame(c,"장비 비교",820,1040,out var b);
            var content=Ui.Rect("Comparison cards",b,0,0,752,810);
            Action redraw=null;
            redraw=()=>{
                for(int i=content.childCount-1;i>=0;i--) {var child=content.GetChild(i).gameObject;child.SetActive(false);UnityEngine.Object.Destroy(child);}
                var s=ForgeState.Current;var item=s.Pending;
                if(item==null) {Ui.Text("No pending",content,20,50,710,120,"보관 중인 장비가 없습니다.",30,Font(c));return;}
                var equipped=s.equipped[(int)item.part];
                RollCard(c,content,10,equipped,"장착 중");
                RollCard(c,content,276,item,"비교할 장비");
                Ui.Text("Queue",content,20,536,710,58,"보관 "+s.pending.Count+"개 · "+EquipmentRules.PartNames[(int)item.part]+" 비교",27,Font(c),Ui.Gold);
                int id=item.id;
                if(equipped!=null)Action(c,content,22,660,332,100,"판매 · 골드 "+EquipmentRules.SaleGold(item),()=>{
                    if(!s.SellPending(id,out int gold))return;
                    c.Main.gold=(int)Math.Min(int.MaxValue,(long)c.Main.gold+gold);c.Main.Refresh();
                    c.Toast("골드 +"+gold);
                    if(s.Pending==null)c.Close();else redraw();
                },Red,26);
                Action(c,content,equipped!=null?386:190,660,332,100,"장착",()=>{
                    if(!s.ToggleEquip(id))return;
                    ForgeRuntime.Ensure(c.Main).SyncSlots();c.Main.Refresh();
                    if(equipped==null){c.Close();if(s.Pending!=null)c.Open("forge-comparison");}
                    else redraw();
                });
                Ui.Text("Decision help",content,24,598,704,54,equipped==null?"빈 슬롯 · 장착하면 완료됩니다":"장착을 다시 누르면 원래 장비로 돌아갑니다.",23,Font(c));
            };
            redraw();
        }
        static void BuildAutoForgeLive(ScreenContext c)
        {
            Frame(c,"자동 제련",820,1420,out var body);
            Scroll(c,body,0,0,body.rect.width,body.rect.height-24,1730,out var b);
            var s=ForgeState.Current;
            Ui.Text("Keep title",b,8,0,710,54,"유지할 등급",30,Font(c),Ui.Gold,TextAnchor.MiddleLeft);
            for(int i=0;i<10;i++) {
                int index=i;var row=Ui.Image("Keep grade "+i,b,8,62+i*66,706,60,TierBand(i));row.type=Image.Type.Sliced;row.pixelsPerUnitMultiplier=1.4f;
                var toggle=Check(c,row.transform,9,6,s.keepTiers[i]);toggle.onValueChanged.AddListener(value=>s.keepTiers[index]=value);
                Ui.Image("Tier icon",row.transform,72,2,55,55,TierIcon(i)).preserveAspect=true;
                Ui.Text("Tier name",row.transform,144,0,500,60,EquipmentRules.TierNames[i],27,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            }
            Ui.Text("Filter title",b,12,742,430,55,"추가 옵션 필터 (하나 이상 일치)",25,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            var filterChoices=new System.Collections.Generic.List<Toggle>();
            PopupSkin.Switch(b,580,746,"Enable affix filter",s.filterEnabled,value=>{s.filterEnabled=value;foreach(var choice in filterChoices)choice.interactable=value;});
            for(int i=0;i<9;i++) {
                int index=i;float x=i%2*356,y=824+i/2*74;
                var check=Check(c,b,x+10,y,(s.affixMask&(1<<i))!=0);
                check.name="Affix filter "+((EquipmentAffixKind)i);check.interactable=s.filterEnabled;filterChoices.Add(check);
                check.onValueChanged.AddListener(value=>{if(value)s.affixMask|=1<<index;else s.affixMask&=~(1<<index);});
                Ui.Text("Affix",b,x+70,y-2,272,52,EquipmentRules.AffixNames[i],23,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            }
            Ui.Text("Batch label",b,12,1220,424,65,"한 번에 사용할 망치 수",26,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            var amount=Ui.Text("Batch size",b,498,1220,112,65,s.batchSize.ToString(),34,Font(c));
            Action(c,b,436,1220,60,65,"−",()=>{s.batchSize=Math.Max(1,s.batchSize-1);amount.text=s.batchSize.ToString();});
            Action(c,b,612,1220,60,65,"+",()=>{s.batchSize=Math.Min(99,s.batchSize+1);amount.text=s.batchSize.ToString();});
            var continuing=Check(c,b,650,1325,s.continueAfterMatch);
            continuing.onValueChanged.AddListener(value=>s.continueAfterMatch=value);
            Ui.Text("Continue label",b,10,1310,624,88,"목표 장비를 찾아도 제련 계속하기\n25개 이상 보관 시 배치 완료 후 비교",24,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            Ui.Text("Filter help",b,12,1410,690,120,"체크한 등급과 옵션이 일치하는 장비만 보관합니다.\n그 외 장비는 골드로 자동 판매합니다.\n원시 / 중세 장비는 추가 옵션이 없습니다.",23,Font(c),Ui.Ivory,TextAnchor.UpperLeft);
            Action(c,b,174,1570,370,100,s.autoEnabled?"정지":"시작",()=>{
                if(s.autoEnabled){var runtime=ForgeRuntime.Ensure(c.Main);runtime.StopAuto();c.Close();if(!runtime.Busy && s.Pending!=null)c.Open("forge-comparison");return;}
                if(!Array.Exists(s.keepTiers,value=>value)){c.Toast("유지할 등급을 선택하세요.");return;}
                if(s.filterEnabled && s.affixMask==0){c.Toast("추가 옵션 필터를 선택하세요.");return;}
                c.Close();ForgeRuntime.Ensure(c.Main).StartAuto();
            });
        }
    }
    public sealed class ForgeLiveView : MonoBehaviour
    {
        public Action refresh; float next;
        void Update(){if(Time.unscaledTime>=next){next=Time.unscaledTime+.15f;refresh?.Invoke();}}
    }
}
