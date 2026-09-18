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
        static Sprite SlotFrame(ScreenContext c) => c.Assets!=null && c.Assets.equipmentSlotPrefab ? c.Assets.equipmentSlotPrefab.equipmentFrame : PopupSkin.PanelArt;
        static RectTransform RollCard(ScreenContext c,Transform parent,float y,EquipmentRoll item,string label)
        {
            var panel=Ui.Image(label,parent,32,y,756,248,PopupSkin.PanelArt);
            panel.type=Image.Type.Sliced;panel.pixelsPerUnitMultiplier=7;
            var slot=Ui.Image("Equipment frame",panel.transform,24,24,166,166,SlotFrame(c));
            slot.type=Image.Type.Sliced;slot.pixelsPerUnitMultiplier=7;
            EquipmentPictograms.TintFrame(slot,item!=null?EquipmentRules.TierColor(item.tier):Color.gray);
            var icon=EquipmentArt.Icon(item);
            if(icon)Ui.Image("Equipment icon",slot.transform,12,10,142,138,icon).preserveAspect=true;
            Ui.Text("Level",slot.transform,-6,136,178,40,item!=null?"Lv."+item.level:"",27,Font(c));
            if(item!=null && item.ascension>0) {
                var star=Ui.Text("Star",slot.transform,0,170,166,30,EquipmentRules.AscensionStars(item.ascension),25,Font(c),Ui.Gold);
                star.resizeTextForBestFit=true;star.resizeTextMinSize=14;star.resizeTextMaxSize=star.fontSize;
            }
            Ui.Text("Name",panel.transform,212,20,520,55,item!=null?item.Name:"빈 슬롯",28,Font(c),item!=null?EquipmentRules.TierColor(item.tier):Ui.Ivory,TextAnchor.MiddleLeft);
            Ui.Text("Stats",panel.transform,212,82,520,148,StatText(item),26,Font(c),Ui.Ivory,TextAnchor.UpperLeft);
            return panel.rectTransform;
        }
        static void BuildProbabilityLive(ScreenContext c)
        {
            ForgeRuntime.Ensure(c.Main);
            Frame(c,"확률 정보",820,1450,out var body);
            Scroll(c,body,0,0,body.rect.width,body.rect.height-20,1260,out var b);
            Ui.Text("Subtitle",b,0,0,b.rect.width,48,"제련 확률",30,Font(c),Ui.Gold);
            Action(c,b,650,0,58,58,"i",()=>c.Open("forge-probability-details"),Slate);
            ProbabilityWallet(c,b,126,0,"0");
            ProbabilityWallet(c,b,402,1,"0");
            var goldValue=b.Find("Wallet value 0").GetComponent<Text>();
            var diamondValue=b.Find("Wallet value 1").GetComponent<Text>();
            var levels=Ui.Text("Levels",b,264,116,446,48,"",27,Font(c),Ui.Ivory,TextAnchor.MiddleRight);
            var current=new Text[10];var next=new Text[10];var stars=new Text[10];
            for(int i=0;i<10;i++) {
                var row=Ui.Image("Rarity "+EquipmentRules.TierNames[i],b,8,174+i*68,704,60,TierBand(i));row.type=Image.Type.Sliced;row.pixelsPerUnitMultiplier=1.4f;
                Ui.Image("Next level shade",row.transform,546,5,148,50,null,new Color(0,0,0,.38f));
                Ui.Image("Tier icon",row.transform,7,2,56,56,TierIcon(i)).preserveAspect=true;
                Ui.Text("Name",row.transform,73,2,230,54,EquipmentRules.TierNames[i],27,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
                stars[i]=Ui.Text("Rarity star",row.transform,303,2,106,54,"",24,Font(c),Ui.Gold);
                stars[i].resizeTextForBestFit=true;stars[i].resizeTextMinSize=13;stars[i].resizeTextMaxSize=stars[i].fontSize;
                current[i]=Ui.Text("Current",row.transform,420,2,112,54,"",26,Font(c));
                next[i]=Ui.Text("Next",row.transform,558,2,126,54,"",26,Font(c),Ui.Cyan);
            }
            var status=Ui.Text("Upgrade status",b,12,867,704,58,"",27,Font(c));
            var segments=new Image[6];
            for(int i=0;i<6;i++)segments[i]=Ui.Image("Gold segment "+i,b,26+i*112,936,100,40,PopupSkin.ActionArt);
            Button primary=null,diamond=null,free=null;
            primary=Action(c,b,40,1004,642,90,"업그레이드",()=>{
                var s=ForgeState.Current;var now=DateTime.UtcNow;bool changed=false;
                bool ascending=s.level>=EquipmentRules.MaxForgeLevel && s.UpgradePhase(now)==3;
                switch(s.UpgradePhase(now)) {
                    case 0: changed=s.FillSegment(ref c.Main.gold);break;
                    case 1: changed=s.StartUpgrade(now);break;
                    case 3: changed=s.ClaimUpgrade(now);break;
                }
                if(!changed)c.Toast("골드가 부족하거나 아직 완료되지 않았습니다.");
                if(changed && ascending)ForgeRuntime.Ensure(c.Main).ResetAfterAscension();
                c.Main.forgeLevel=s.level;c.Main.Refresh();
            });
            diamond=Action(c,b,26,1120,326,94,"건너뛰기",()=>{
                if(!ForgeState.Current.DiamondSkip(DateTime.UtcNow,ref c.Main.gems))c.Toast("다이아가 부족합니다.");
                c.Main.Refresh();
            });
            free=Action(c,b,368,1120,326,94,"30분 스킵",()=>{
                if(!ForgeState.Current.FreeSkip(DateTime.UtcNow))c.Toast("오늘의 무료 스킵을 모두 사용했습니다.");
                c.Main.Refresh();
            });
            Watch(b,()=>{
                var s=ForgeState.Current;var now=DateTime.UtcNow;s.ResetDaily(now);int phase=s.UpgradePhase(now);
                bool ascending=s.level>=EquipmentRules.MaxForgeLevel;
                levels.text=phase==4?"레벨 35 · 만렙":"레벨 "+s.level+"  ▶  "+(ascending?"승천 "+(s.ascension+1):"레벨 "+(s.level+1));
                goldValue.text=MainScreen.Compact(c.Main.gold);diamondValue.text=MainScreen.Compact(c.Main.gems);
                var rates=EquipmentRules.TierProbabilities(s.level);var future=EquipmentRules.TierProbabilities(phase==4?s.level:ascending?1:s.level+1);
                for(int i=0;i<10;i++){
                    current[i].text=(rates[i]*100).ToString("0.##")+"%";next[i].text=(future[i]*100).ToString("0.##")+"%";
                    stars[i].text=EquipmentRules.AscensionStars(s.ascension);stars[i].gameObject.SetActive(s.ascension>0);
                }
                for(int i=0;i<6;i++){
                    segments[i].gameObject.SetActive(i<s.Segments && phase!=4);
                    float width=(668f-(s.Segments-1)*12f)/s.Segments;
                    segments[i].rectTransform.sizeDelta=new Vector2(width,40);
                    segments[i].rectTransform.anchoredPosition=new Vector2(26+i*(width+12),-936);
                    segments[i].color=i<s.filledSegments?Ui.Cyan:new Color(.18f,.22f,.28f);
                }
                status.text=phase==0?"골드 업그레이드 "+s.filledSegments+" / "+s.Segments:phase==1?"게이지 완료 · 시간 업그레이드를 시작하세요":phase==2?"업그레이드 중 "+TimeSpan.FromSeconds(s.RemainingSeconds(now)).ToString(@"hh\:mm\:ss"):phase==3?"시간 업그레이드 완료":"대장간 만렙";
                primary.GetComponentInChildren<Text>().text=phase==0?"골드 업그레이드 · "+s.SegmentCost:phase==1?"시간 업그레이드 시작":phase==2?"업그레이드 중":phase==4?"만렙":ascending?"승천하기":"업그레이드 완료";
                primary.interactable=phase==0 || phase==1 || phase==3;
                diamond.gameObject.SetActive(phase==2);free.gameObject.SetActive(phase==2);
                diamond.GetComponentInChildren<Text>().text="건너뛰기\n다이아 "+s.DiamondSkipCost(now);
                free.GetComponentInChildren<Text>().text="30분 무료 스킵\n"+(4-s.freeSkipsUsed)+" / 4";free.interactable=s.freeSkipsUsed<4;
            });
        }
        static void BuildProbabilityDetailsLive(ScreenContext c)
        {
            Frame(c,"모든 장비의 목록",820,1380,out var b);
            Scroll(c,b,0,0,b.rect.width,b.rect.height-12,10*1130,out var content);
            var rates=EquipmentRules.TierProbabilities(ForgeState.Current.level);
            for(int tier=0;tier<10;tier++) {
                int top=tier*1130;
                var head=Ui.Image("Tier "+tier,content,6,top,724,66,TierBand(tier));head.type=Image.Type.Sliced;head.pixelsPerUnitMultiplier=1.4f;
                Ui.Image("Tier emblem",head.transform,10,3,60,60,TierIcon(tier)).preserveAspect=true;
                Ui.Text("Tier name",head.transform,86,0,330,66,EquipmentRules.TierNames[tier],29,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
                if(ForgeState.Current.ascension>0) {
                    var star=Ui.Text("Tier star",head.transform,408,0,140,66,EquipmentRules.AscensionStars(ForgeState.Current.ascension),24,Font(c),Ui.Gold);
                    star.resizeTextForBestFit=true;star.resizeTextMinSize=12;star.resizeTextMaxSize=star.fontSize;
                }
                Ui.Text("Tier chance",head.transform,560,0,152,66,(rates[tier]*100).ToString("0.##")+"%",27,Font(c));
                for(int n=0;n<18;n++) {
                    var item=new EquipmentRoll {ascension=ForgeState.Current.ascension,tier=tier,level=Math.Max(1,ForgeState.Current.draws[tier]),variant=n/6,part=(EquipmentPart)(n%6)};
                    float x=15+n%4*180,y=top+82+n/4*206;
                    var slot=Action(c,content,x,y,155,155,"",()=>c.Open("forge-item-details",item),EquipmentRules.TierColor(tier));
                    slot.name="Equipment "+item.tier+" "+item.variant+" "+item.part;
                    var frame=(Image)slot.targetGraphic;frame.sprite=SlotFrame(c);EquipmentPictograms.TintFrame(frame,EquipmentRules.TierColor(tier));
                    var icon=EquipmentArt.Icon(item);
                    if(icon)Ui.Image("Thumbnail",slot.transform,14,9,126,130,icon).preserveAspect=true;
                    else Ui.Text("Pending art",slot.transform,6,30,143,90,"썸네일\n준비 중",21,Font(c));
                    if(item.ascension>0) {
                        var star=Ui.Text("Star",slot.transform,0,131,155,28,EquipmentRules.AscensionStars(item.ascension),21,Font(c),Ui.Gold);
                        star.resizeTextForBestFit=true;star.resizeTextMinSize=12;star.resizeTextMaxSize=star.fontSize;
                    }
                    Ui.Text("Drop chance",content,x,y+159,155,37,(rates[tier]*100/18).ToString("0.0000")+"%",20,Font(c));
                }
            }
        }
        static void BuildItemDetailsLive(ScreenContext c)
        {
            var item=c.Payload as EquipmentRoll ?? new EquipmentRoll{ascension=ForgeState.Current.ascension};
            Frame(c,"장비 정보",820,1120,out var b);
            var card=RollCard(c,b,0,item,"장비 도감");card.anchoredPosition=new Vector2(0,0);card.sizeDelta=new Vector2(740,248);
            Ui.Text("Affix count",b,28,270,682,62,"추가 옵션 "+EquipmentRules.AffixCount(item.tier)+"개 · 중복 없음",28,Font(c),Ui.Gold);
            string ranges="";
            for(int i=0;i<9;i++)ranges+=EquipmentRules.AffixNames[i]+"  +1 ~ "+EquipmentRules.AffixMaximums[i]+"%\n";
            Ui.Text("Affix ranges",b,42,350,674,520,ranges,27,Font(c),Ui.Ivory,TextAnchor.UpperLeft);
        }
        static void BuildEquipmentDetailsLive(ScreenContext c)
        {
            var slot=c.Payload as EquipmentSlot;
            var item=c.Payload as EquipmentRoll ?? (slot!=null ? slot.roll : null);
            var b=EquipmentDialog(c,"Equipment details Dialog",390,c.Height*.21f);
            b.Find("Tag").GetComponent<Text>().text=item!=null?"장착됨":"빈 슬롯";
            RollCard(c,b,94,item,"Equipment details card");
            PopupSkin.Close("Close",b,b.rect.width-68,12,56,Font(c),c.Close);
        }
        static void BuildComparisonLive(ScreenContext c)
        {
            ForgeRuntime.Ensure(c.Main);
            var b=EquipmentDialog(c,"Equipment comparison Dialog",810,170);
            var content=Ui.Rect("Comparison cards",b,0,0,820,810);
            PopupSkin.Close("Close",b,b.rect.width-68,12,56,Font(c),c.Close);
            Action redraw=null;
            redraw=()=>{
                for(int i=content.childCount-1;i>=0;i--) {var child=content.GetChild(i).gameObject;child.SetActive(false);UnityEngine.Object.Destroy(child);}
                var s=ForgeState.Current;var item=s.Pending;
                if(item==null) {b.Find("Tag").GetComponent<Text>().text="";Ui.Text("No pending",content,30,240,760,90,"보관 중인 장비가 없습니다.",28,Font(c));return;}
                var equipped=s.equipped[(int)item.part];
                b.Find("Tag").GetComponent<Text>().text=equipped!=null?"장착됨":"새로운 장비";
                RectTransform current=null;
                if(equipped!=null) {
                    current=RollCard(c,content,94,equipped,"Current equipment");
                    current.sizeDelta=new Vector2(current.sizeDelta.x,270);
                }
                var candidate=RollCard(c,content,equipped!=null?377:225,item,"New equipment");
                candidate.sizeDelta=new Vector2(candidate.sizeDelta.x,270);
                var newlyRolled=equipped!=null && equipped.id==s.ComparisonNewId?current:candidate;
                Ui.Text("New marker",newlyRolled,24,228,166,34,"새로운!",25,Font(c),new Color(1,.25f,.18f));
                int id=item.id;
                if(equipped!=null)Action(c,content,32,676,360,100,"판매",()=>{
                    if(!s.SellPending(id,out int gold))return;
                    c.Main.gold=(int)Math.Min(int.MaxValue,(long)c.Main.gold+gold);c.Main.Refresh();
                    RewardVisuals.Absorb(c.Main,RewardVisuals.Kind.Gold,gold,c.Main.forgeButton ? c.Main.forgeButton.transform.position : candidate.position);
                    if(s.Pending==null)c.Close();else redraw();
                },Red);
                Action(c,content,equipped!=null?428:230,676,360,100,"장착",()=>{
                    if(!s.ToggleEquip(id))return;
                    ForgeRuntime.Ensure(c.Main).SyncSlots();c.Main.Refresh();
                    if(s.Pending==null)c.Close();else redraw();
                });
            };
            redraw();
        }
        static void BuildAutoForgeLive(ScreenContext c)
        {
            Frame(c,"자동 제련",820,1180,out var body);
            var scroll=Scroll(c,body,0,0,body.rect.width,body.rect.height-24,1050,out var b);
            var s=ForgeState.Current;var rates=EquipmentRules.TierProbabilities(s.level);
            Ui.Text("Keep title",b,8,0,710,54,"유지할 등급",30,Font(c),Ui.Gold,TextAnchor.MiddleLeft);
            int visible=0;
            for(int i=0;i<10;i++) {
                if(rates[i]<=0)continue;
                int index=i;var row=Ui.Image("Keep grade "+i,b,8,62+visible++*66,706,60,TierBand(i));row.type=Image.Type.Sliced;row.pixelsPerUnitMultiplier=1.4f;
                var toggle=Check(c,row.transform,9,6,s.keepTiers[i]);toggle.onValueChanged.AddListener(value=>s.keepTiers[index]=value);
                Ui.Image("Tier icon",row.transform,72,2,55,55,TierIcon(i)).preserveAspect=true;
                Ui.Text("Tier name",row.transform,144,0,500,60,EquipmentRules.TierNames[i],27,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            }
            float filterY=80+visible*66;
            Ui.Text("Filter title",b,12,filterY,430,55,"추가 옵션 필터",27,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            var choices=Ui.Rect("Affix choices",b,0,filterY+74,714,370);
            var controls=Ui.Rect("Auto controls",b,0,0,714,288);
            Action layout=()=>{
                choices.gameObject.SetActive(s.filterEnabled);
                controls.anchoredPosition=new Vector2(0,-(filterY+80+(s.filterEnabled?370:0)));
                b.sizeDelta=new Vector2(b.sizeDelta.x,filterY+80+(s.filterEnabled?370:0)+288);
            };
            PopupSkin.Switch(b,580,filterY+4,"Enable affix filter",s.filterEnabled,value=>{s.filterEnabled=value;layout();});
            for(int i=0;i<9;i++) {
                int index=i;float x=i%2*356,y=i/2*74;
                var check=Check(c,choices,x+10,y,(s.affixMask&(1<<i))!=0);check.name="Affix filter "+((EquipmentAffixKind)i);
                check.onValueChanged.AddListener(value=>{if(value)s.affixMask|=1<<index;else s.affixMask&=~(1<<index);});
                Ui.Text("Affix",choices,x+70,y-2,272,52,EquipmentRules.AffixNames[i],23,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            }
            Ui.Text("Batch label",controls,12,0,424,65,"한 번에 사용할 망치 수",26,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            var amount=Ui.Text("Batch size",controls,498,0,112,65,s.batchSize.ToString(),34,Font(c));
            Action(c,controls,436,0,60,65,"−",()=>{s.batchSize=Math.Max(1,s.batchSize-1);amount.text=s.batchSize.ToString();});
            Action(c,controls,612,0,60,65,"+",()=>{s.batchSize=Math.Min(99,s.batchSize+1);amount.text=s.batchSize.ToString();});
            var continuing=Check(c,controls,650,92,s.continueAfterMatch);
            continuing.onValueChanged.AddListener(value=>s.continueAfterMatch=value);
            Ui.Text("Continue label",controls,10,80,624,70,"목표 장비를 찾아도 제련 계속하기",24,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            Action(c,controls,174,182,370,100,s.autoEnabled?"정지":"시작",()=>{
                if(s.autoEnabled){ForgeRuntime.Ensure(c.Main).StopAuto();c.Close();return;}
                bool keep=false;for(int i=0;i<10;i++)keep|=rates[i]>0 && s.keepTiers[i];
                if(!keep){c.Toast("유지할 등급을 선택하세요.");return;}
                if(s.filterEnabled && s.affixMask==0){c.Toast("추가 옵션 필터를 선택하세요.");return;}
                c.Close();ForgeRuntime.Ensure(c.Main).StartAuto();
            });
            layout();
        }

    }
    public sealed class ForgeLiveView : MonoBehaviour
    {
        public Action refresh; float next;
        void Update(){if(Time.unscaledTime>=next){next=Time.unscaledTime+.15f;refresh?.Invoke();}}
    }
}
