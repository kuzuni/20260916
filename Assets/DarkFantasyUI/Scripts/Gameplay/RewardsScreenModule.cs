using System;
using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    public static class RewardsScreenModule
    {
        public static void Register(UiScreenRegistry registry)
        {
            registry.Register("offline-rewards",ScreenPresentation.Modal,Offline,false);
            registry.Register("progress-pass",ScreenPresentation.Modal,Pass,false);
            registry.Register("wallet",ScreenPresentation.Modal,Wallet,false);
            registry.Register("dungeon-reward",ScreenPresentation.Modal,DungeonReward,false);
        }
        static Sprite hammer;
        public static Sprite HammerArt {
            get {
                if(hammer)return hammer;
                var t=Resources.Load<Texture2D>("Moonlit/Forge/RewardHammer-v1");
                if(!t)return PopupSkin.RewardIcon(0);
                hammer=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),100);
                hammer.name="RewardHammer-v1";return hammer;
            }
        }
        static RectTransform Panel(ScreenContext c,string title,float requested)
        {
            float h=Mathf.Min(requested,c.Height-80);
            var panel=PopupSkin.Panel(title+" frame",c.Root,80,(c.Height-h)/2,920,h).rectTransform;
            panel.GetComponent<Image>().raycastTarget=true;
            Ui.Text("Title",panel,40,24,840,70,title,44,c.Assets.font,Ui.Gold);
            PopupSkin.Close("Close",panel,418,h-96,84,c.Assets.font,c.Close);
            return panel;
        }
        static readonly Color Blue = new Color(.015f,.23f,.43f);
        static readonly Color Red = new Color(.38f,.035f,.035f);
        static Sprite passStone;
        static Sprite[] passChests;
        static Font Font(ScreenContext c) => c.Assets.font;
        static Sprite CurrencyIcon(ScreenContext c,int index)
            => c.Assets.interfaceIcons != null && c.Assets.interfaceIcons.Length > index ? c.Assets.interfaceIcons[index] : null;
        static Button Action(ScreenContext c,Transform p,float x,float y,float w,float h,string label,UnityEngine.Events.UnityAction click,Color? color=null,int size=30)
            => PopupSkin.Button(label,p,x,y,w,h,label,Font(c),click,color??Blue,size);

        static void Offline(ScreenContext c)
        {
            Frame(c,"오프라인 보상",740,900,out var b);
            var elapsed=Ui.Text("Elapsed",b,0,8,b.rect.width,54,"",30,Font(c));
            RewardRule(b,80,b.rect.width);
            float left=(b.rect.width-420)*.5f;
            RewardIcon(c,b,left,118,"Gold reward",CurrencyIcon(c,0),"1/초");
            RewardIcon(c,b,left+250,118,"Forge reward",HammerArt,"1/분");
            RewardRule(b,366,b.rect.width);
            var totals=PopupSkin.Panel("Reward totals",b,60,416,b.rect.width-120,86);
            var goldIcon=Ui.Image("Gold total icon",totals.transform,18,12,62,62,CurrencyIcon(c,0));goldIcon.preserveAspect=true;
            var gold=Ui.Text("Gold total",totals.transform,88,8,136,70,"0",35,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            var hammerIcon=Ui.Image("Forge total icon",totals.transform,298,12,62,62,HammerArt);hammerIcon.preserveAspect=true;
            var ore=Ui.Text("Forge total",totals.transform,370,8,116,70,"0",35,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            foreach(var value in new[]{gold,ore}) {
                value.resizeTextForBestFit=true;value.resizeTextMinSize=18;value.resizeTextMaxSize=value.fontSize;
                value.verticalOverflow=VerticalWrapMode.Truncate;
            }
            var claim=Action(c,b,(b.rect.width-360)*.5f,570,360,124,"수집",()=>{
                int beforeGold=c.Main.gold,beforeOre=c.Main.ore;
                if(!RewardState.Current.Claim(c.Main))return;
                RewardVisuals.Absorb(c.Main,RewardVisuals.Kind.Gold,c.Main.gold-beforeGold,goldIcon.transform.position);
                RewardVisuals.Absorb(c.Main,RewardVisuals.Kind.Hammer,c.Main.ore-beforeOre,hammerIcon.transform.position);
            });
            claim.name="Claim";
            claim.GetComponentInChildren<Text>().fontSize=Ui.ReadableFontSize(42);
            void Refresh() {
                var state=RewardState.Current;state.Advance(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                var span=TimeSpan.FromSeconds(state.accruedSeconds);
                string time=(span.Days>0?span.Days+"일 ":"")+(span.Hours>0?span.Hours+"시 ":"")+span.Minutes+"분 "+span.Seconds+"초";
                elapsed.text="수집 시간: <color=#35FF35>"+time+"</color>";
                gold.text=state.GoldAvailable.ToString("N0");ore.text=state.HammersAvailable.ToString("N0");
                claim.interactable=state.GoldAvailable>0||state.HammersAvailable>0;
            }
            b.gameObject.AddComponent<LiveUiRefresh>().RefreshView=Refresh;Refresh();
        }

        static void Pass(ScreenContext c)
        {
            float h=Mathf.Min(1360,c.Height-240),w=940;
            var root=Ui.Rect("진행 패스 Dialog",c.Root,(c.Width-w)/2,(c.Height-h)/2+25,w,h);
            var backing=Ui.Image("Pass stone backing",root,6,6,w-12,h-12,PassStone(c.Assets),Color.white);
            backing.type=Image.Type.Tiled;backing.pixelsPerUnitMultiplier=.45f;backing.raycastTarget=true;
            var rim=Ui.Image("Pass stone frame",root,0,0,w,h,PopupSkin.PanelArt);
            rim.type=Image.Type.Sliced;rim.fillCenter=false;rim.pixelsPerUnitMultiplier=3;rim.raycastTarget=true;
            Ui.ArtImage("Pass sword header",root,20,-162,900,381,PopupSkin.PassHeaderArt).preserveAspect=true;
            Ui.Text("Title",root,90,46,760,100,"진행 패스",52,Font(c));
            var b=Ui.Rect("Live content",root,30,155,880,h-215);
            Ui.Text("Prompt",b,20,0,390,110,"전투를 진행하여 보상을\n받으세요!",30,Font(c));
            var purchase=Ui.ArtButton("₩13,900",b,465,4,375,100,PopupSkin.GoldActionArt,true,8);
            Ui.Text("Price",purchase.transform,18,0,258,100,"₩13,900",36,Font(c));
            purchase.onClick.AddListener(()=>c.Toast("프리미엄 구매는 데모에서 연결되지 않습니다."));
            Ui.Image("Premium ruby",purchase.transform,288,20,65,65,CurrencyIcon(c,1)).preserveAspect=true;
            var freeTab=Ui.Image("Free tab",b,0,126,422,74,PopupSkin.ActionArt);freeTab.type=Image.Type.Sliced;freeTab.pixelsPerUnitMultiplier=8;
            Ui.Text("Free",b,0,126,422,74,"무료",34,Font(c));
            var premiumTab=Ui.Image("Premium tab",b,458,126,422,74,PopupSkin.GoldActionArt);premiumTab.type=Image.Type.Sliced;premiumTab.pixelsPerUnitMultiplier=8;
            Ui.Text("Premium",b,458,126,422,74,"프리미엄",34,Font(c));
            Scroll(c,b,0,212,894,b.rect.height-212,100*216,out var content);
            var claims=new Button[100];var checks=new Image[100];
            for(int i=0;i<100;i++) PassRow(c,content,i,i*216,claims,checks);
            void Refresh() {
                for(int i=0;i<100;i++) {
                    bool claimed=RewardState.Current.passClaimed[i];
                    claims[i].interactable=!claimed&&c.Main.highestClearedStage>=(i+1)*5;
                    claims[i].gameObject.SetActive(!claimed);checks[i].gameObject.SetActive(claimed);
                }
            }
            b.gameObject.AddComponent<LiveUiRefresh>().RefreshView=Refresh;Refresh();
            PopupSkin.Close("Close",root,w/2-50,h-50,100,Font(c),c.Close,64);
        }
        static void PassRow(ScreenContext c,Transform p,int index,float y,Button[] claims,Image[] checks)
        {
            var frame=Ui.Image("Stage frame",p,305,y,270,48,PopupSkin.ActionArt);frame.type=Image.Type.Sliced;frame.pixelsPerUnitMultiplier=9;
            Ui.Text("Stage milestone "+index,p,305,y,270,48,"스테이지 "+((index+1)*5),26,Font(c),Ui.Ivory);
            Ui.Image("Timeline glow",p,435,y+48,10,168,null,new Color(.1f,.65f,1f));
            Ui.Image("Timeline",p,439,y+48,2,168,null,Color.white);
            var node=Ui.Image("Node",p,430,y+112,20,20,null,Ui.Gold);node.rectTransform.localRotation=Quaternion.Euler(0,0,45);
            var free=PassCard("Free reward "+index,p,8,y+52,new Color(.65f,.85f,1f));
            var ticket=RewardVisuals.Ticket(index%3);
            PassReward(c,free,20,14,ticket,"10");
            PassReward(c,free,20,77,HammerArt,"100");
            checks[index]=Ui.ArtImage("Claimed reward check",free,312,58,84,78,Resources.Load<Sprite>("Moonlit/Forge/ClaimedCheck-v1"));
            checks[index].preserveAspect=true;
            claims[index]=Action(c,free,310,74,82,54,"받기",()=>{
                if(!RewardState.Current.ClaimPass(c.Main,index))return;
                RewardVisuals.Absorb(c.Main,RewardVisuals.Kind.Hammer,100,free.position);
                var kind=index%3==0?RewardVisuals.Kind.SkillTicket:index%3==1?RewardVisuals.Kind.PetTicket:RewardVisuals.Kind.MountTicket;
                RewardVisuals.Absorb(c.Main,kind,10,free.position);
                claims[index].gameObject.SetActive(false);checks[index].gameObject.SetActive(true);
            },Blue,18);
            claims[index].name="Claim milestone "+index;
            // Preserve the reference's locked premium demonstration; no new purchase/reward policy is introduced.
            int demo=index%6;
            var premium=PassCard("Premium reward "+index,p,462,y+52,new Color(.7f,.5f,.3f));
            Ui.ArtImage("Premium chest",premium,222,16,182,124,PassChest(demo)).preserveAspect=true;
            var original=demo%3==0?RewardVisuals.Ticket(0):PopupSkin.RewardIcon(demo%3==1?0:4);
            string amount=demo%3==0?"220":demo%3==1?"200":"500";
            PassReward(c,premium,20,14,demo==3?CurrencyIcon(c,0):original,demo==3?"40k":amount);
            if(demo!=3)PassReward(c,premium,20,77,CurrencyIcon(c,1),"5");
            Ui.ArtImage("Premium lock",premium,340,4,55,55,PopupSkin.RewardIcon(6)).preserveAspect=true;
        }

        static RectTransform Frame(ScreenContext c, string title, float width, float height, out RectTransform body, bool close = true)
        {
            width = Mathf.Min(width, c.Width - 36);
            height = Mathf.Min(height, c.Height - 36);
            var root = Ui.Rect(title + " Dialog", c.Root, (c.Width - width) * .5f, (c.Height - height) * .5f, width, height);
            var shadow = PopupSkin.Panel("Ornate stone frame", root, 0, 0, width, height);
            shadow.raycastTarget = true;
            Ui.Text("Title", root, 44, 20, width - 88, 72, title, 44, Font(c), Ui.Ivory);
            Ui.Image("Title rule", root, 70, 94, width - 140, 3, null, Ui.Gold);
            body = Ui.Rect("Live content", root, 30, 112, width - 60, height - 150);
            if (close)
            {
                var b = PopupSkin.Button("Close", root, width * .5f - 42, height - 46, 84, 84, "×", Font(c), c.Close, Red, 58);
                b.transform.SetAsLastSibling();
            }
            return root;
        }

        static ScrollRect Scroll(ScreenContext c, Transform parent, float x, float y, float w, float h, float contentHeight, out RectTransform content)
        {
            var viewport = Ui.Rect("Scroll viewport", parent, x, y, w, h);
            var maskImage = viewport.gameObject.AddComponent<Image>();
            maskImage.color = new Color(0,0,0,.04f); maskImage.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Ui.Rect("Scrollable content", viewport, 0, 0, w - 14, contentHeight);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(.5f, 1); content.sizeDelta = new Vector2(-14, contentHeight);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 36;
            var rail = Ui.Image("Scrollbar", viewport, w - 9, 0, 7, h, null, new Color(.1f,.1f,.1f,.8f));
            var sb = rail.gameObject.AddComponent<Scrollbar>(); sb.direction = Scrollbar.Direction.BottomToTop;
            var handle = Ui.Image("Handle", rail.transform, 0, 0, 7, Mathf.Max(60, h * h / contentHeight), null, Ui.Gold);
            handle.raycastTarget = true; sb.handleRect = handle.rectTransform; sb.targetGraphic = handle; scroll.verticalScrollbar = sb;
            return scroll;
        }

        static void RewardRule(Transform parent, float y, float width)
        {
            Ui.Image("Reward divider", parent, 52, y, width - 104, 2, null, Ui.Gold);
            Ui.Image("Divider ornament", parent, width * .5f - 30, y - 11, 60, 24,
                PopupSkin.CrestArt).preserveAspect = true;
        }

        static void RewardIcon(ScreenContext c, Transform p, float x, float y, string name, Sprite icon, string rate)
        {
            var frameArt = c.Assets != null && c.Assets.equipmentSlotPrefab
                ? c.Assets.equipmentSlotPrefab.equipmentFrame : PopupSkin.PanelArt;
            var frame = Ui.Image(name, p, x, y, 170, 170, frameArt);
            frame.type = Image.Type.Sliced; frame.pixelsPerUnitMultiplier = 7;
            Ui.Image("Reward illustration", frame.transform, 24, 22, 122, 126, icon).preserveAspect = true;
            Ui.Text("Rate", frame.transform, -10, 176, 190, 48, rate, 30, Font(c), new Color(.1f,1f,.2f));
        }

        static Sprite PassStone(MainScreenAssets assets)
        {
            if(passStone) return passStone;
            if(assets.panels==null || assets.panels.Length<3 || !assets.panels[2]) return PopupSkin.PanelArt;
            var source=assets.panels[2];
            var r=source.rect; var border=source.border;
            // Use the entire cracked face, excluding the independently rendered gold frame.
            passStone=Sprite.Create(source.texture,
                new Rect(r.x+border.x,r.y+border.y,r.width-border.x-border.z,r.height-border.y-border.w),
                new Vector2(.5f,.5f),source.pixelsPerUnit,0,SpriteMeshType.FullRect);
            passStone.name="Pass cracked stone face";
            return passStone;
        }

        static Sprite PassChest(int index)
        {
            if(passChests==null || !passChests[0]) {
                var atlas=Resources.Load<Texture2D>("Moonlit/Forge/PassChests-v1");
                if(!atlas) return null;
                passChests=new Sprite[4];
                float w=atlas.width/2f,h=atlas.height/2f;
                for(int i=0;i<4;i++) {
                    passChests[i]=Sprite.Create(atlas,new Rect(i%2*w,(1-i/2)*h,w,h),
                        new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
                    passChests[i].name="Pass chest "+i;
                }
            }
            return passChests[Mathf.Clamp(index,0,3)];
        }

        static RectTransform PassCard(string name,Transform parent,float x,float y,Color tint)
        {
            var card=PopupSkin.IllustratedCard(name,parent,x,y,410,144,
                Resources.Load<Sprite>("Moonlit/Social/ProfileRuins-v1"),tint);
            // This compact card needs a thin empty rim, leaving its scenery visible edge to edge.
            card.Find("Card rim").GetComponent<Image>().pixelsPerUnitMultiplier=22;
            return card;
        }

        static void PassReward(ScreenContext c,Transform parent,float x,float y,Sprite icon,string amount)
        {
            Ui.Image("Reward contrast",parent,x-4,y,218,48,null,new Color(.015f,.025f,.04f,.62f));
            Ui.Image("Reward icon",parent,x,y,48,48,icon).preserveAspect=true;
            Ui.Text("Reward amount",parent,x+58,y,150,48,amount,29,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
        }

        static void DungeonReward(ScreenContext c)
        {
            var state=DungeonProgression.Data;
            if(!state.pendingClaim){c.Close();return;}
            int index=state.pendingIndex, amount=DungeonProgression.Reward(index,state.pendingDifficulty);
            float h=720;
            var p=PopupSkin.Panel("Dungeon clear reward",c.Root,140,(c.Height-h)/2,800,h).rectTransform;
            Ui.Text("Title",p,40,30,720,80,"던전 클리어",44,c.Assets.font,Ui.Gold);
            Ui.Text("Dungeon name",p,40,124,720,64,DungeonProgression.Names[index]+" · 난이도 "+state.pendingDifficulty,29,c.Assets.font);
            var kind=index==0?RewardVisuals.Kind.Hammer:(RewardVisuals.Kind)(index+1);
            var icon=RewardVisuals.Icon(c.Main,kind);
            var frame=Ui.Image("Reward slot frame",p,290,228,220,220,c.Assets.equipmentSlotPrefab?c.Assets.equipmentSlotPrefab.equipmentFrame:PopupSkin.PanelArt);
            frame.type=Image.Type.Sliced;frame.pixelsPerUnitMultiplier=7;
            Ui.ArtImage("Reward illustration",frame.transform,28,20,164,150,icon).preserveAspect=true;
            Ui.Text("Reward count",frame.transform,15,165,190,50,amount.ToString("N0"),34,c.Assets.font);
            bool claimed=false;
            PopupSkin.Button("Claim dungeon reward",p,180,530,440,112,"보상 받기",c.Assets.font,()=>{
                if(claimed)return;
                if(!DungeonProgression.ClaimEntry(out int rewardIndex,out int value))return;
                claimed=true;
                if(rewardIndex==0)c.Main.ore=RewardRules.Add(c.Main.ore,value);
                else if(rewardIndex==1)c.Main.skillTickets=RewardRules.Add(c.Main.skillTickets,value);
                else if(rewardIndex==2)c.Main.petTickets=RewardRules.Add(c.Main.petTickets,value);
                else c.Main.mountTickets=RewardRules.Add(c.Main.mountTickets,value);
                c.Main.Refresh();c.Main.SaveGame();
                var origin=frame.transform.position;
                c.Close();c.Main.screens.ShowMainPage();
                RewardVisuals.Absorb(c.Main,kind,value,origin);
                c.Main.CompleteDungeonClaim();
            });
        }

        static void Wallet(ScreenContext c)
        {
            var p=Panel(c,"재화",960);
            var text=Ui.Text("Wallet balances",p,100,145,720,570,"",31,c.Assets.font,Ui.Ivory,TextAnchor.MiddleLeft);
            void Refresh() {
                var m=c.Main;
                text.text="다이아    "+m.gems.ToString("N0")+"\n\n골드    "+m.gold.ToString("N0")+"\n\n스킬소환권    "+m.skillTickets+"\n\n펫소환권    "+m.petTickets+"\n\n탈것소환권    "+m.mountTickets+"\n\n망치    "+m.ore.ToString("N0");
            }
            p.gameObject.AddComponent<LiveUiRefresh>().RefreshView=Refresh;Refresh();
        }
    }
}
