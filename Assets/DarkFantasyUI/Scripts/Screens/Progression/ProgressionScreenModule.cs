using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Moonlit.UI
{
    /// <summary>Runtime collection and dungeon pages; state lives in shared serializable gameplay models.</summary>
    public static class ProgressionScreenModule
    {
        static readonly Color Stone = new Color(.07f,.085f,.095f,.98f);
        static readonly Color Blue = new Color(.02f,.25f,.48f,1f);
        static readonly Color Red = new Color(.38f,.025f,.018f,1f);
        static readonly Color Green = new Color(.05f,.55f,.28f,1f);
        static readonly System.Random random = new System.Random();
        static int selectedCollectionTab, selectedDungeon;
        static CollectionView activeCollection;
        internal static void ResetSession()
        {
            selectedCollectionTab = selectedDungeon = 0; activeCollection = null;
            skillIcons = null; skillRing = progressFrame = dungeonHammer = null;
            CollectionProgression.Reset(); DungeonProgression.Reset();
        }
        public static void Register(UiScreenRegistry registry)
        {
            registry.Register("skills-pets-heroes", ScreenPresentation.Page, BuildCollection, false);
            registry.Register("skill-details", ScreenPresentation.Modal, BuildSkillDetails, false);
            registry.Register("summon-probability", ScreenPresentation.Modal, BuildProbability, false);
            registry.Register("summon-probability-details", ScreenPresentation.Modal, BuildProbabilityDetails, false);
            registry.Register("summon-result", ScreenPresentation.Fullscreen, BuildSummonResult, false);
            registry.Register("summon-limit-confirm", ScreenPresentation.Modal, BuildSummonLimit, false);
            registry.Register("dungeons", ScreenPresentation.Page, BuildDungeons, false);
            registry.Register("dungeon-details", ScreenPresentation.Modal, BuildDungeonDetails, false);
        }
        sealed class CollectionView
        {
            public ScreenContext context;
            public int tab, quantity = 5, lastSummonFrame = -1;
            public RectTransform root, equipped, grid, experience, costRoot;
            public Image currencyIcon;
            public ScrollRect scroll;
            public Text title, summary, currency, diamonds, level, summonLabel, quantityLabel;
            public Button summon, ascend;
            public GameObject upgradeDot,equipDot,summonDot;
            public Button[] tabs = new Button[3];
            public readonly List<Action> refreshCards = new List<Action>();
        }
        sealed class ProbabilityPayload { public int category, level; }
        sealed class DetailPayload { public CollectionEntry entry; public Action refresh; }
        sealed class SummonLimit { public CollectionView view; public int quantity; }
        static readonly int[] SummonBatches={1,5,10,20,50,100,500};
        sealed class SummonSession { public int category; public CollectionEntry[] results; public bool[] fresh; public CollectionView parent; }
        static int Tickets(MainScreen main, int category) => category == 0 ? main.skillTickets : category == 1 ? main.petTickets : main.mountTickets;
        static void SetTickets(MainScreen main, int category, int amount)
        { if (category == 0) main.skillTickets = amount; else if (category == 1) main.petTickets = amount; else main.mountTickets = amount; }
        static string Number(double n) => n >= 1e9 ? n.ToString("0.##E+0") : n.ToString("0.##");
        static void BuildCollection(ScreenContext ctx)
        {
            AddBackdrop(ctx.Root, ctx); var font = ctx.Assets.font;
            var view = activeCollection = new CollectionView { context = ctx, root = ctx.Root, tab = selectedCollectionTab };
            view.title = Ui.Text("Collection title",ctx.Root,300,30,480,76,"",42,font);
            var wallet=PopupSkin.Panel("Summon wallet",ctx.Root,28,46,240,60).rectTransform;
            view.currencyIcon=Ui.ArtImage("Summon currency icon",wallet,0,-10,77,77,TicketIcon(view.tab));
            view.currencyIcon.preserveAspect=true;
            view.currency=Ui.Text("Currency",wallet,82,0,153,60,"",30,font);
            var diamondWallet=PopupSkin.Panel("Summon diamond wallet",ctx.Root,812,46,240,60).rectTransform;
            var diamond=ctx.Assets.interfaceIcons!=null&&ctx.Assets.interfaceIcons.Length>1?ctx.Assets.interfaceIcons[1]:PopupSkin.RewardIcon(1);
            Ui.ArtImage("Summon diamond balance icon",diamondWallet,0,-10,65,77,diamond).preserveAspect=true;
            view.diamonds=Ui.Text("Summon diamond balance",diamondWallet,72,0,163,60,"",30,font);
            FitCurrency(view.currency);FitCurrency(view.diamonds);
            var summary=PopupSkin.Panel("Collection summary frame",ctx.Root,110,120,860,64).rectTransform;
            view.summary=Ui.Text("Summary",summary,16,0,828,64,"",24,font);
            float equippedY = ctx.Height - 840;
            var content=Ui.Rect("Tab content",ctx.Root,48,205,984,Mathf.Max(230,equippedY-225));
            view.scroll=Scroll(content,0,0,984,content.rect.height); view.grid=view.scroll.content;
            var equipped=PopupSkin.Panel("Equipped panel",ctx.Root,88,equippedY,904,142).rectTransform;
            Ui.ArtImage("Equipped ribbon",equipped,0,30,220,49,PopupSkin.ParchmentRibbonArt);
            var equippedLabel=Ui.Text("Equipped label",equipped,8,30,204,49,"장착됨",30,font,new Color(.06f,.045f,.025f));
            equippedLabel.GetComponent<Outline>().effectColor=new Color(1,1,1,.2f);
            view.equipped=Ui.Rect("Equipped skills",equipped,350,8,540,126);
            var upgradeAll=PopupSkin.Button("Upgrade all",ctx.Root,240,equippedY+162,285,86,"모두 업그레이드",font,()=>{
                int count=0;
                foreach(var entry in CollectionProgression.Data.categories[view.tab].entries) {
                    bool upgraded=false;
                    while(entry.Upgrade())upgraded=true;
                    if(upgraded)count++;
                }
                RefreshCollection(view); ctx.Main.Refresh(); ctx.Main.SaveGame(); ctx.Toast(count>0?count+"개 업그레이드":"조각이 부족하거나 최대 레벨입니다.");
            },Blue,25);
            var quickEquip=PopupSkin.Button("Quick equip",ctx.Root,550,equippedY+162,285,86,"빠른 장착",font,()=>{
                CollectionProgression.QuickEquip(view.tab); RefreshCollection(view); ctx.Main.Refresh(); ctx.Toast("보유한 "+CollectionProgression.CategoryNames[view.tab]+" 편성을 갱신했습니다.");
            },Blue,28);
            view.upgradeDot=RewardNotificationDots.Create(upgradeAll.transform,265,-7,27,RewardNotificationDots.Circle(ctx.Main));
            view.equipDot=RewardNotificationDots.Create(quickEquip.transform,265,-7,27,RewardNotificationDots.Circle(ctx.Main));
            float summonY=ctx.Height-540;
            PopupSkin.Panel("Summon rail",ctx.Root,0,summonY-24,1080,206);
            var summon=PopupSkin.Button("Summon five",ctx.Root,330,summonY,390,170,"",font,()=>Summon(ctx,view),Blue,30);
            view.summon=summon;
            view.summonDot=RewardNotificationDots.Create(summon.transform,366,-7,27,RewardNotificationDots.Circle(ctx.Main));
            view.summonLabel=Ui.Text("Summon label",summon.transform,8,4,374,65,"",40,font);
            view.costRoot=Ui.Rect("Summon cost row",summon.transform,18,76,354,82);
            var quantity=PopupSkin.Button("Summon quantity",ctx.Root,180,summonY+90,132,64,"x5",font,()=>{
                view.quantity=SummonBatches[(Array.IndexOf(SummonBatches,view.quantity)+1)%SummonBatches.Length]; RefreshCollection(view);
            },Blue,27); view.quantityLabel=quantity.GetComponentInChildren<Text>();
            PopupSkin.Back("Return to main",ctx.Root,28,summonY+68,82,font,ctx.Close,true);
            PopupSkin.Button("Probability",ctx.Root,802,summonY,62,62,"i",font,()=>{
                ctx.Open("summon-probability",new ProbabilityPayload{category=view.tab,level=CollectionProgression.Data.categories[view.tab].summonLevel});
            },Stone,30);
            view.level=Ui.Text("Summon level",ctx.Root,744,summonY+64,200,42,"",26,font);
            view.experience=Ui.Rect("Summon experience",ctx.Root,744,summonY+115,200,36);
            view.ascend=PopupSkin.Button("Ascend collection",ctx.Root,744,summonY+111,200,48,"승천하기",font,()=>{
                AscendCollection(ctx,view.tab);RefreshCollection(view);
            },Blue,24);
            for(int i=0;i<3;i++){
                int tab=i; string title=CollectionProgression.CategoryNames[i];
                view.tabs[i]=PopupSkin.Button("Tab "+title,ctx.Root,48+i*328,ctx.Height-320,328,74,title,font,()=>{
                    view.tab=selectedCollectionTab=tab; RenderCollection(view);
                },Blue,29);
            }
            RenderCollection(view);
            var ticker=ctx.Root.gameObject.AddComponent<ProgressionTick>();ticker.tick=()=>RefreshCollection(view);
        }
        static void RenderCollection(CollectionView view,bool resetScroll=true)
        {
            float position=view.scroll.verticalNormalizedPosition;
            ClearChildren(view.grid); view.refreshCards.Clear();
            var entries=CollectionProgression.Data.categories[view.tab].entries;
            int visible=0;
            foreach(var entry in entries){
                if(!entry.unlocked)continue;
                int index=visible++;
                var refresh=EntryCard(view.grid,56+(index%3)*306,16+(index/3)*390,260,entry,view.context.Assets.font,
                    ()=>view.context.Open("skill-details",new DetailPayload{entry=entry,refresh=()=>RefreshCollection(view)}));
                view.refreshCards.Add(refresh);
            }
            if(visible==0)Ui.Text("Empty collection",view.grid,52,100,880,100,
                "보유한 "+CollectionProgression.CategoryNames[view.tab]+"이 없습니다.",30,view.context.Assets.font);
            view.grid.sizeDelta=new Vector2(0,Mathf.Max(view.scroll.viewport.rect.height,Mathf.CeilToInt(visible/3f)*390+24));
            view.scroll.verticalNormalizedPosition=resetScroll?1:position; RefreshCollection(view);
        }
        static void RefreshCollection(CollectionView view)
        {
            if(view==null || !view.root) return;
            var category=CollectionProgression.Data.categories[view.tab]; int owned=0; double health=0,attack=0;
            foreach(var entry in category.entries) if(entry.unlocked){owned++;health+=entry.OwnedHealth;attack+=entry.OwnedAttack;}
            if(owned!=view.refreshCards.Count){RenderCollection(view,view.refreshCards.Count==0);return;}
            view.title.text=CollectionProgression.CategoryNames[view.tab]+" "+owned+"/"+category.entries.Length;
            view.summary.text="보유 효과  체력 +"+Number(health)+"  공격력 +"+Number(attack);
            int tickets=Tickets(view.context.Main,view.tab),use=Math.Min(tickets,view.quantity),diamonds=(view.quantity-use)*100;
            view.currency.text=tickets.ToString(); view.currencyIcon.sprite=TicketIcon(view.tab);
            view.diamonds.text=view.context.Main.gems.ToString("N0");
            RenderSummonCost(view,category.CanSummon?use:0,category.CanSummon?diamonds:0);
            view.summon.interactable=category.CanSummon;
            view.upgradeDot.SetActive(CollectionProgression.HasUpgrade(view.tab));
            view.equipDot.SetActive(CollectionProgression.HasBetterEquip(view.tab));
            int availableTickets=tickets,availableDiamonds=view.context.Main.gems;
            bool affordable=CollectionProgression.PaySummon(CollectionProgression.SummonQuantity(view.tab,view.quantity),ref availableTickets,ref availableDiamonds);
            view.summonDot.SetActive(CollectionProgression.CanSummonWithTickets(view.context.Main,view.tab)&&affordable);
            view.summonLabel.text=category.CanSummon?"소환 x"+view.quantity:"소환 레벨 최대"; view.quantityLabel.text="x"+view.quantity;
            view.level.text="소환 Lv."+category.summonLevel; ClearChildren(view.experience);
            view.experience.gameObject.SetActive(category.CanSummon);
            view.ascend.gameObject.SetActive(!category.CanSummon);view.ascend.interactable=category.CanAscend;
            view.ascend.GetComponentInChildren<Text>().text=category.CanAscend?"승천하기":"만렙";
            if(category.CanSummon)Progress(view.experience,0,0,200,36,category.experience/(float)category.ExperienceRequired,
                category.experience+"/"+category.ExperienceRequired,view.context.Assets.font);
            for(int i=0;i<3;i++) PopupSkin.Select(view.tabs[i],i==view.tab);
            foreach(var refresh in view.refreshCards) refresh();
            ClearChildren(view.equipped); int slot=0;
            foreach(var entry in CollectionProgression.Equipped(view.tab)){
                EntryCard(view.equipped,slot++*172,0,112,entry,view.context.Assets.font,
                    ()=>view.context.Open("skill-details",new DetailPayload{entry=entry,refresh=()=>RefreshCollection(view)}),true);
            }
            if(slot==0) Ui.Text("Empty equipment",view.equipped,0,20,510,70,"최대 "+CollectionProgression.Capacity(view.tab)+"개 장착",25,view.context.Assets.font);
        }
        static Action EntryCard(Transform parent,float x,float y,float size,CollectionEntry entry,Font font,Action click,bool compact=false,bool probability=false)
        {
            bool largeCollection=!compact&&!probability&&size>205;
            float textScale=largeCollection?size/158f:1f;
            var button=Ui.ArtButton("Skill "+entry.Name,parent,x,y,size,compact?size+12:size+68*textScale);
            if(click!=null) button.onClick.AddListener(()=>click());
            var illustratedIcon=entry.category==0?SkillIcon(entry.grade,entry.variant):FlatCompanionCatalog.Icon(entry.category,entry.grade,entry.variant);
            if(illustratedIcon){
                var icon=Ui.ArtImage("Icon",button.transform,size*.17f,size*.17f,size*.66f,size*.66f,illustratedIcon);
                Ui.CenterAspect(icon);
            } else Ui.Text("Art pending",button.transform,size*.1f,
                size*(largeCollection ? .18f : compact ? .1f : probability ? .35f : .62f),size*.8f,size*(largeCollection ? .16f : .12f),
                "아트 보류",Mathf.RoundToInt(size*.11f),font);
            var frame=Ui.ArtImage("Slot frame",button.transform,0,0,size,size,SkillRing);
            frame.color=EquipmentRules.TierColor(entry.grade); frame.preserveAspect=true; button.targetGraphic=frame;
            if(!compact){
                var stars=Ui.Text("Collection stars",button.transform,0,0,size,size*.16f,
                    entry.ascension>0?new string('★',entry.ascension):"",Mathf.RoundToInt(size*.12f),font,Ui.Gold);
                stars.gameObject.SetActive(entry.ascension>0);
            }
            Text level=null,owned=null,fragments=null,badge=null;
            Image ownershipLock=null,badgeRibbon=null;
            if(!probability){
                level=Ui.Text("Level",button.transform,4,size-40*textScale,size-8,34*textScale,"",Mathf.RoundToInt(size*.16f),font);
                ownershipLock=Ui.ArtImage("Ownership lock",button.transform,size*.4f,size*.23f,size*.2f,size*.2f,PopupSkin.RewardIcon(6));
                ownershipLock.preserveAspect=true;
                owned=Ui.Text("Ownership",button.transform,4,size*.43f,size-8,28*textScale,"",Mathf.RoundToInt(size*.12f),font);
                badgeRibbon=Ui.Image("Equipped badge ribbon",button.transform,0,size*.44f,size,30*textScale,PopupSkin.PanelArt);
                badgeRibbon.type=Image.Type.Sliced;badgeRibbon.pixelsPerUnitMultiplier=22;
                badge=Ui.Text("Equipped badge",button.transform,0,size*.44f,size,30*textScale,"",Mathf.RoundToInt(size*.13f),font,Ui.Gold);
            }
            Ui.Text("Grade",button.transform,0,size+2*textScale,size,28*textScale,entry.category!=0&&illustratedIcon?entry.Name:EquipmentRules.TierNames[entry.grade],Mathf.RoundToInt(size*.12f),font,Ui.Gold);
            RectTransform fill=null;
            if(!compact && !probability){
                Progress(button.transform,8,size+34*textScale,size-16,28*textScale,0,"",font);
                var track=button.transform.Find("Progress"); fragments=track.Find("Value").GetComponent<Text>(); fill=track.Find("Fill").GetComponent<RectTransform>();
            }
            Action refresh=()=>{
                if(!button) return;
                if(level) level.text="Lv."+entry.level;
                if(owned) owned.text=entry.unlocked?"":"미보유";
                if(ownershipLock) ownershipLock.gameObject.SetActive(!entry.unlocked);
                if(badgeRibbon) badgeRibbon.gameObject.SetActive(CollectionProgression.IsEquipped(entry));
                if(badge) badge.text=CollectionProgression.IsEquipped(entry)?"장착됨":"";
                if(fragments) fragments.text=entry.level>=100?"최대":entry.fragments+"/"+entry.Required;
                if(fill) fill.sizeDelta=new Vector2((size-16-28*textScale)*Mathf.Clamp01(entry.fragments/(float)entry.Required),fill.sizeDelta.y);
            }; refresh(); return refresh;
        }
        static string Description(CollectionEntry entry)
        {
            if(entry.category!=0) return "장착 효과\n체력 +"+Number(entry.EquippedHealth)+"\n공격력 +"+Number(entry.EquippedAttack)+
                (FlatCompanionCatalog.Icon(entry.category,entry.grade,entry.variant)?"":"\n외형 아트 제작 보류");
            string theme=SkillCatalog.Description(entry.grade,entry.variant)+"\n";
            if(entry.variant==0) return theme+"매 3턴 · 평타 전에 발동\n체력 "+Number(entry.FixedHeal)+" 회복\n공격력 +"+Number(entry.FixedAttackBoost);
            int hits=SkillChoreography.HitTimes(entry.grade,entry.variant).Length;
            return theme+"매 "+entry.Cooldown+"턴 · "+(hits==1?"단일공격":hits+"타 연속공격")+"\n총 고정 피해 "+Number(entry.FixedDamage);
        }
        static void BuildSkillDetails(ScreenContext ctx)
        {
            var payload=ctx.Payload as DetailPayload;
            var entry=payload!=null?payload.entry:ctx.Payload as CollectionEntry ?? CollectionProgression.Data.categories[selectedCollectionTab].entries[0];
            var font=ctx.Assets.font; float h=Mathf.Min(940,ctx.Height-180);
            var panel=Panel(ctx.Root,80,(ctx.Height-h)/2,920,h,"",font,1);
            var refreshCard=EntryCard(panel,40,55,205,entry,font,null);
            Ui.Text("Name",panel,270,48,600,82,"["+EquipmentRules.TierNames[entry.grade]+"] "+entry.Name,29,font,EquipmentRules.TierColor(entry.grade),TextAnchor.MiddleLeft);
            var desc=Ui.Text("Description",panel,270,140,600,210,Description(entry),23,font,Ui.Ivory,TextAnchor.UpperLeft);
            float descriptionHeight=Mathf.Max(210,desc.preferredHeight);
            desc.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,descriptionHeight);
            float flowOffset=Mathf.Max(0,140+descriptionHeight+22-363);
            var passiveHeading=Ui.Text("Passive",panel,60,363+flowOffset,800,50,"보유 효과 · 해금 후 항상 적용",27,font,Ui.Gold);
            var passivePanel=PopupSkin.Panel("Passive frame",panel,60,418+flowOffset,800,66).rectTransform;
            var passive=Ui.Text("Passive values",passivePanel,12,0,776,66,"",28,font);
            var status=Ui.Text("Fragment status",panel,60,490+flowOffset,800,52,"",25,font);
            Button upgrade=null,preview=null,unequip=null;
            Action refresh=()=>{
                desc.text=Description(entry);
                descriptionHeight=Mathf.Max(210,desc.preferredHeight);
                desc.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,descriptionHeight);
                flowOffset=Mathf.Max(0,140+descriptionHeight+22-363);
                passiveHeading.rectTransform.anchoredPosition=new Vector2(60,-363-flowOffset);
                passivePanel.anchoredPosition=new Vector2(60,-418-flowOffset);
                status.rectTransform.anchoredPosition=new Vector2(60,-490-flowOffset);
                if(preview)((RectTransform)preview.transform).anchoredPosition=new Vector2(280,-566-flowOffset);
                if(unequip)((RectTransform)unequip.transform).anchoredPosition=new Vector2(675,-566-flowOffset);
                passive.text="체력 +"+Number(entry.OwnedHealth)+"  공격력 +"+Number(entry.OwnedAttack);
                status.text=entry.level>=100?"최대 레벨":"조각 "+entry.fragments+" / "+entry.Required+" · 업그레이드 후에도 해금 유지";
                if(upgrade) upgrade.interactable=entry.unlocked&&entry.level<100&&entry.fragments>=entry.Required;
                refreshCard(); payload?.refresh?.Invoke(); RefreshCollection(activeCollection);
            };
            upgrade=PopupSkin.Button("Upgrade",panel,55,h-185,370,86,"업그레이드",font,()=>{
                if(entry.Upgrade()){refresh();ctx.Main.Refresh();} else ctx.Toast("조각이 부족하거나 최대 레벨입니다.");
            },Blue,28);
            PopupSkin.Button("Equip",panel,455,h-185,390,86,"장착",font,()=>{
                if(!CollectionProgression.Equip(entry)){ctx.Toast("먼저 획득해야 장착할 수 있습니다.");return;}
                refresh();ctx.Main.Refresh();ctx.Toast(entry.Name+" 장착");
            },Blue,28);
            if(entry.category==0) preview=PopupSkin.Button("Preview skill",panel,280,566+flowOffset,360,72,"스킬 연출 보기",font,()=>{
                ctx.Main.screens.ShowMainPage();ctx.Main.PreviewSkill(entry.grade,entry.variant);
            },Blue,25);
            unequip=PopupSkin.Button("Unequip",panel,675,566+flowOffset,175,72,"해제",font,()=>{
                var slots=CollectionProgression.Data.categories[entry.category].equipped;int index=Array.IndexOf(slots,entry.Id);
                if(index>=0)slots[index]=-1;refresh();ctx.Main.Refresh();
            },Stone,25);
            Close(panel,410,h-48,font,ctx.Close);refresh();
        }
        static void AscendCollection(ScreenContext ctx,int category)
        {
            if(!CollectionProgression.Ascend(category))return;
            if(activeCollection!=null && activeCollection.root && activeCollection.tab==category)RenderCollection(activeCollection);
            ctx.Main.Refresh();ctx.Main.SaveGame();ctx.Toast(CollectionProgression.CategoryNames[category]+" "+CollectionProgression.Data.categories[category].ascension+"성 승천");
        }
        static void Summon(ScreenContext ctx,CollectionView view)
        {
            if(!view.root || !view.summon || !view.summon.IsInteractable() || view.lastSummonFrame==Time.frameCount)return;
            view.lastSummonFrame=Time.frameCount;
            int quantity=CollectionProgression.SummonQuantity(view.tab,view.quantity);
            if(quantity<1)return;
            if(quantity<view.quantity){ctx.Open("summon-limit-confirm",new SummonLimit{view=view,quantity=quantity});return;}
            ExecuteSummon(ctx,view,quantity);
        }
        static void ExecuteSummon(ScreenContext ctx,CollectionView view,int quantity)
        {
            if(!view.root)return;
            var category=CollectionProgression.Data.categories[view.tab];var known=new HashSet<int>();
            foreach(var entry in category.entries)if(entry.unlocked)known.Add(entry.Id);
            int tickets=Tickets(ctx.Main,view.tab),diamonds=ctx.Main.gems;
            if(!CollectionProgression.TrySummon(view.tab,quantity,ref tickets,ref diamonds,random,out var result)){
                ctx.Toast(category.CanSummon?"소환권 또는 다이아가 부족합니다.":"소환 레벨이 최대입니다.");return;
            }
            SetTickets(ctx.Main,view.tab,tickets);ctx.Main.gems=diamonds;
            var fresh=new bool[result.Length];for(int i=0;i<result.Length;i++)fresh[i]=known.Add(result[i].Id);
            ctx.Main.Refresh();RefreshCollection(view);
            ctx.Open("summon-result",new SummonSession{category=view.tab,results=result,fresh=fresh,parent=view});
        }
        static void BuildSummonLimit(ScreenContext ctx)
        {
            var payload=ctx.Payload as SummonLimit;
            if(payload==null || payload.view==null || !payload.view.root){ctx.Close();return;}
            var view=payload.view;var font=ctx.Assets.font;
            var panel=Panel(ctx.Root,110,(ctx.Height-570)/2,860,570,"소환 수량 확인",font,37);
            Ui.Text("Limited summon quantity",panel,50,130,760,128,
                "소환 레벨 100까지 남은 수량은 "+payload.quantity+"개입니다.\n"+payload.quantity+"개만 소환합니다.",29,font);
            int tickets=Math.Min(Tickets(ctx.Main,view.tab),payload.quantity),diamonds=(payload.quantity-tickets)*100;
            string cost=(tickets>0?CollectionProgression.CategoryNames[view.tab]+" 소환권 "+tickets:"")+
                (tickets>0&&diamonds>0?" + ":"")+(diamonds>0?"다이아 "+diamonds:"");
            Ui.Text("Limited summon cost",panel,40,275,780,64,cost,29,font,Ui.Gold);
            bool resolved=false;
            PopupSkin.Button("Cancel limited summon",panel,60,385,330,88,"취소",font,()=>{if(resolved)return;resolved=true;ctx.Close();},Stone,30);
            PopupSkin.Button("Confirm limited summon",panel,470,385,330,88,"알겠다 / 소환",font,()=>{
                if(resolved)return;resolved=true;ctx.Close();ExecuteSummon(view.context,view,payload.quantity);
            },Blue,30);
            Close(panel,380,520,font,()=>{resolved=true;ctx.Close();});
        }
        static void BuildSummonResult(ScreenContext ctx)
        {
            PopupSkin.FullViewportBackdrop(ctx,Resources.Load<Sprite>("Moonlit/Skills/SummonDais-v1"),Color.white);
            var session=ctx.Payload as SummonSession;var font=ctx.Assets.font;
            SummonRevealAnimation reveal=null;
            var input=ctx.Root.gameObject.AddComponent<Image>();input.color=Color.clear;input.raycastTarget=true;
            Ui.Text("Result title",ctx.Root,90,100,900,82,"소환 결과",45,font);
            if(session==null){Ui.Text("No result",ctx.Root,140,ctx.Height*.4f,800,100,"소환 후 결과를 확인할 수 있습니다.",30,font);}
            else {
                Ui.Text("Result category",ctx.Root,90,195,900,60,CollectionProgression.CategoryNames[session.category]+" · "+session.results.Length+"개",30,font);
                float resultY=session.results.Length<=5?ctx.Height*.43f:ctx.Height*.34f;
                RectTransform root;
                if(session.results.Length<=10)root=Ui.Rect("Summon result cards",ctx.Root,36,resultY,1008,650);
                else{
                    var scroll=Scroll(ctx.Root,36,300,1008,ctx.Height-650);
                    scroll.name="Summon results scroll";root=scroll.content;root.name="Summon result cards";
                    root.sizeDelta=new Vector2(0,Mathf.CeilToInt(session.results.Length/5f)*255+24);
                }
                var reveals=new CanvasGroup[session.results.Length];
                for(int i=0;i<session.results.Length;i++){
                    var entry=session.results[i];int col=i%5,row=i/5;
                    float offset=session.results.Length==1?396:0;
                    var card=Ui.Rect("Result card "+i,root,14+col*198+offset,row*255,170,246);
                    card.pivot=new Vector2(.5f,.5f);card.anchoredPosition+=new Vector2(85,-123);
                    EntryCard(card,0,0,170,entry,font,null,false,true);
                    card.GetComponentInChildren<Button>().enabled=false;
                    Ui.Text("Summon status "+i,card,0,202,170,44,session.fresh[i]?"신규 +1":"조각 +1",23,font,session.fresh[i]?Green:Ui.Gold);
                    reveals[i]=card.gameObject.AddComponent<CanvasGroup>();
                }
                reveal=root.gameObject.AddComponent<SummonRevealAnimation>();reveal.Play(reveals);
            }
            var tap=ctx.Root.gameObject.AddComponent<SummonResultTap>();tap.Initialize(reveal,ctx.Close);
            var dim=ctx.Root.parent.Find("Dim");
            if(dim)dim.gameObject.AddComponent<SummonResultTap>().ForwardTo(tap);
        }
        static ProbabilityPayload ProbabilityContext(ScreenContext ctx) =>
            ctx.Payload as ProbabilityPayload ?? new ProbabilityPayload { category=selectedCollectionTab,level=CollectionProgression.Data.categories[selectedCollectionTab].summonLevel };
        static void BuildProbability(ScreenContext ctx)
        {
            var info=ProbabilityContext(ctx);var font=ctx.Assets.font;float h=Mathf.Min(1130,ctx.Height-160);
            var panel=Panel(ctx.Root,90,(ctx.Height-h)/2,900,h,"",font,1);
            var title=Ui.Text("Probability title",panel,170,32,560,108,"",35,font);
            var scroll=Scroll(panel,50,185,800,h-400);
            Action render=()=>{
                ClearChildren(scroll.content);title.text=CollectionProgression.CategoryNames[info.category]+" 소환 확률\n레벨 "+info.level;
                var rates=CollectionProgression.Probabilities(info.level);
                for(int i=0;i<rates.Length;i++){
                    var row=PopupSkin.Panel("Rarity "+i,scroll.content,5,i*72,780,64).rectTransform;
                    row.GetComponent<Image>().color=EquipmentRules.TierColor(i);
                    Ui.Text("Name",row,20,0,470,64,EquipmentRules.TierNames[i],27,font,Ui.Ivory,TextAnchor.MiddleLeft);
                    Ui.Text("Rate",row,500,0,255,64,(rates[i]*100).ToString("0.00")+"%",27,font,Ui.Ivory,TextAnchor.MiddleRight);
                }
                scroll.content.sizeDelta=new Vector2(0,rates.Length*72);
            };
            PopupSkin.Button("Previous level",panel,50,60,86,70,"◀",font,()=>{info.level=Math.Max(1,info.level-1);render();},Blue,30);
            PopupSkin.Button("Next level",panel,765,60,86,70,"▶",font,()=>{info.level=Math.Min(100,info.level+1);render();},Blue,30);
            PopupSkin.Button("Details",panel,800,145,62,62,"i",font,()=>ctx.Open("summon-probability-details",new ProbabilityPayload{category=info.category,level=info.level}),Stone,30);
            var current=CollectionProgression.Data.categories[info.category];
            Ui.Text("Current summon level",panel,60,h-202,780,42,
                CollectionProgression.CategoryNames[info.category]+" 소환 Lv."+current.summonLevel,26,font);
            if(current.CanSummon)Progress(panel,75,h-148,750,48,current.experience/(float)current.ExperienceRequired,
                current.experience+"/"+current.ExperienceRequired,font);
            else{
                var ascend=PopupSkin.Button("Ascend summon category",panel,225,h-157,450,66,current.CanAscend?"승천하기":"만렙",font,()=>{
                    if(!CollectionProgression.Data.categories[info.category].CanAscend)return;
                    AscendCollection(ctx,info.category);ctx.Close();
                },Blue,29);
                ascend.interactable=current.CanAscend;
            }
            Close(panel,400,h-48,font,ctx.Close);render();
        }
        static void BuildProbabilityDetails(ScreenContext ctx)
        {
            var info=ProbabilityContext(ctx);var font=ctx.Assets.font;float h=Mathf.Min(1190,ctx.Height-160);
            var panel=Panel(ctx.Root,90,(ctx.Height-h)/2,900,h,CollectionProgression.CategoryNames[info.category]+" 목록 · 레벨 "+info.level,font,34);
            var scroll=Scroll(panel,35,145,830,h-230);var rates=CollectionProgression.Probabilities(info.level);
            for(int grade=0;grade<rates.Length;grade++){
                var row=Ui.Rect("Grade "+grade,scroll.content,10,grade*340,800,320);
                Ui.Image("Probability group backing",row,4,4,792,312,null,new Color(.015f,.03f,.045f));
                var rim=Ui.Image("Probability group rim",row,0,0,800,320,PopupSkin.PanelArt);
                rim.type=Image.Type.Sliced;rim.fillCenter=false;rim.pixelsPerUnitMultiplier=18;
                var header=Ui.Image("Rarity header",row,0,0,800,60,PopupSkin.PanelArt,EquipmentRules.TierColor(grade));
                header.type=Image.Type.Sliced;header.pixelsPerUnitMultiplier=12;
                Ui.Text("Tier",row,28,4,450,52,EquipmentRules.TierNames[grade],28,font,Ui.Ivory,TextAnchor.MiddleLeft);
                Ui.Text("Tier chance",row,535,4,235,52,(rates[grade]*100).ToString("0.00")+"%",28,font,Ui.Ivory,TextAnchor.MiddleRight);
                for(int variant=0;variant<3;variant++){
                    var entry=CollectionProgression.Data.categories[info.category].entries[grade*3+variant];
                    EntryCard(row,80+variant*245,74,148,entry,font,()=>ctx.Open("skill-details",new DetailPayload{entry=entry,refresh=()=>RefreshCollection(activeCollection)}),false,true);
                    Ui.Text("Chance",row,75+variant*245,258,158,36,(rates[grade]*100/3).ToString("0.0000")+"%",22,font);
                }
            }
            scroll.content.sizeDelta=new Vector2(0,rates.Length*340);Close(panel,400,h-48,font,ctx.Close);
        }

        static void BuildDungeons(ScreenContext ctx)
        {
            AddBackdrop(ctx.Root,ctx);DungeonProgression.RefreshDay(DateTime.UtcNow);var font=ctx.Assets.font;
            PopupSkin.Back("Return to main",ctx.Root,40,ctx.Height-364,96,font,ctx.Close);
            Ui.ArtImage("Dungeon title frame",ctx.Root,360,0,360,164,Resources.Load<Sprite>("Moonlit/Dungeons/DungeonTitle-v1"));
            Ui.Text("Dungeon title",ctx.Root,415,60,250,66,"던전",44,font);
            Ui.Text("Reset",ctx.Root,80,152,920,86,"던전 열쇠는 매일 자정에 보충됩니다.\n열쇠는 보상을 수령할 때만 소모됩니다.",25,font);
            var scroll=Scroll(ctx.Root,60,250,960,ctx.Height-660);var labels=new Text[4];var dots=new GameObject[4];
            Action refresh=()=>{
                DungeonProgression.RefreshDay(DateTime.UtcNow);
                for(int i=0;i<4;i++){if(labels[i])labels[i].text=DungeonProgression.Data.keys[i]+"/2";if(dots[i])dots[i].SetActive(DungeonProgression.Data.keys[i]>0);}
            };
            for(int i=0;i<4;i++){
                int index=i;
                var row=PopupSkin.IllustratedCard("Dungeon "+DungeonProgression.Names[i],scroll.content,10,i*250,920,230,DungeonBanner(i),Color.white);
                row.Find("Card rim").GetComponent<Image>().pixelsPerUnitMultiplier=18;
                Ui.Image("Readable action scrim",row,670,12,236,204,null,new Color(0,.015f,.025f,.7f));
                Ui.ArtImage("Dungeon reward icon",row,26,20,60,60,i==0?DungeonHammer():RewardVisuals.Ticket(i-1));
                Ui.Text("Name",row,96,18,470,58,DungeonProgression.Names[i],36,font,Ui.Ivory,TextAnchor.MiddleLeft);
                Ui.ArtImage("Dungeon key icon",row,702,38,52,52,PopupSkin.RewardIcon(new[]{5,2,4,3}[i]));
                labels[i]=Ui.Text("Keys",row,760,34,134,58,"",33,font);
                var open=PopupSkin.Button("Open",row,674,116,224,88,"열기",font,()=>{
                    selectedDungeon=index;ctx.Open("dungeon-details",new DungeonPayload{index=index,refresh=refresh});
                },Blue,32);
                dots[i]=RewardNotificationDots.Create(open.transform,204,-7,27,RewardNotificationDots.Circle(ctx.Main));
            }
            scroll.content.sizeDelta=new Vector2(0,1000);refresh();
            var ticker=ctx.Root.gameObject.AddComponent<ProgressionTick>();ticker.tick=refresh;
        }
        sealed class DungeonPayload { public int index; public Action refresh; }
        static void AwardDungeon(MainScreen main,int index,int amount)
        {
            if(index==0)main.ore+=amount;else if(index==1)main.skillTickets+=amount;
            else if(index==2)main.petTickets+=amount;else main.mountTickets+=amount;
            main.Refresh();main.SaveGame();RewardVisuals.Absorb(main,index==0?RewardVisuals.Kind.Hammer:(RewardVisuals.Kind)(index+1),amount);
        }
        static void BuildDungeonDetails(ScreenContext ctx)
        {
            var payload=ctx.Payload as DungeonPayload;
            int index=payload!=null?payload.index:selectedDungeon;
            DungeonProgression.RefreshDay(DateTime.UtcNow);
            int difficulty=DungeonProgression.NextDifficulty(index),lastAction=-1;
            var font=ctx.Assets.font;float h=Mathf.Min(1110,ctx.Height-180);
            var panel=PopupSkin.Panel("Dungeon detail frame",ctx.Root,105,(ctx.Height-h)/2,870,h).rectTransform;
            var painting=PopupSkin.IllustratedCard("Dungeon hero painting",panel,6,6,858,360,DungeonBanner(index),Color.white);
            painting.Find("Card rim").GetComponent<Image>().pixelsPerUnitMultiplier=18;
            Panel(panel,215,20,440,88,DungeonProgression.Names[index],font,37);
            var difficultyLabel=Ui.Text("Difficulty",panel,205,380,460,70,"",37,font);
            var scaling=Ui.Text("Difficulty scaling",panel,130,465,610,66,"",25,font);
            var rewardPanel=PopupSkin.Panel("Dungeon reward",panel,55,552,760,92).rectTransform;
            Ui.ArtImage("Reward icon",rewardPanel,28,15,62,62,index==0?DungeonHammer():RewardVisuals.Ticket(index-1));
            var reward=Ui.Text("Reward amount",rewardPanel,112,10,620,72,"",29,font);
            Ui.ArtImage("Dungeon detail key",panel,295,666,62,62,PopupSkin.RewardIcon(new[]{5,2,4,3}[index]));
            var keys=Ui.Text("Keys",panel,370,666,270,62,"",30,font);
            var sweepInfo=Ui.Text("Sweep info",panel,65,747,740,70,"",25,font);
            Button enter=null,sweep=null;
            Action refresh=()=>{
                DungeonProgression.RefreshDay(DateTime.UtcNow);
                difficultyLabel.text="난이도 "+difficulty;
                scaling.text="일반 스테이지 "+DungeonProgression.NormalStage(difficulty)+" 수준";
                reward.text=DungeonProgression.Rewards[index]+" "+DungeonProgression.Reward(index,difficulty);
                keys.text=DungeonProgression.Data.keys[index]+"/2";
                bool cleared=DungeonProgression.Data.highestCleared[index]>0;
                sweepInfo.text=cleared?"소탕: 난이도 "+DungeonProgression.SweepDifficulty(index)+" · "+DungeonProgression.Rewards[index]+" "+DungeonProgression.Reward(index,DungeonProgression.SweepDifficulty(index)):"첫 클리어 후 소탕할 수 있습니다.";
                if(enter)enter.interactable=DungeonProgression.Data.keys[index]>0;
                if(sweep)sweep.interactable=cleared&&DungeonProgression.Data.keys[index]>0;
                payload?.refresh?.Invoke();
            };
            PopupSkin.Button("Previous difficulty",panel,85,385,86,74,"◀",font,()=>{difficulty=Math.Max(1,difficulty-1);refresh();},Blue,30);
            PopupSkin.Button("Next difficulty",panel,700,385,86,74,"▶",font,()=>{difficulty=Math.Min(DungeonProgression.NextDifficulty(index),difficulty+1);refresh();},Blue,30);
            sweep=PopupSkin.Button("Previous",panel,55,h-195,350,110,"이전 스테이지\n소탕",font,()=>{
                if(lastAction==Time.frameCount)return;lastAction=Time.frameCount;
                if(DungeonProgression.TrySweep(index,out int amount)){AwardDungeon(ctx.Main,index,amount);refresh();}
                else ctx.Toast("클리어 기록 또는 열쇠가 부족합니다.");
            },Blue,30);
            enter=PopupSkin.Button("Enter",panel,465,h-195,350,110,"입장",font,()=>{
                if(lastAction==Time.frameCount)return;lastAction=Time.frameCount;
                if(!DungeonProgression.BeginEntry(index,difficulty)){ctx.Toast("열쇠가 부족하거나 전투 중입니다.");return;}
                var main=ctx.Main;int target=difficulty;
                main.Refresh();ctx.Close();
                // Close the surviving base page so the actual 1:1 battle is visible.
                main.screens.ShowMainPage();
                bool accepted=main.StartDungeon(index,target,DungeonProgression.Waves(index),won=>{
                    if(DungeonProgression.CompleteEntry(won,out int rewardIndex,out int amount)) { main.SaveGame();main.screens.Open("dungeon-reward"); }
                    else {main.Refresh();main.Toast("던전 도전에 실패했습니다.");}
                });
                if(!accepted){DungeonProgression.CancelEntry();main.Refresh();main.Toast("전투를 준비 중입니다.");}
            },Blue,34);
            Close(panel,385,h-48,font,ctx.Close);refresh();
            // The dialog can stay open across the daily reset; update its disabled actions with the key count.
            var ticker=panel.gameObject.AddComponent<ProgressionTick>();ticker.tick=refresh;
        }
        static Sprite dungeonHammer;
        static Sprite DungeonHammer()
        {
            if(dungeonHammer)return dungeonHammer;
            var texture=Resources.Load<Texture2D>("Moonlit/Forge/RewardHammer-v1");
            if(texture)dungeonHammer=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            return dungeonHammer;
        }
        static Sprite DungeonBanner(int index) => Resources.Load<Sprite>("Moonlit/Dungeons/"+new[]{"HammerThief-v1","GhostVillage-v1","Invasion-v1","ZombieRush-v1"}[index]);
        static void ClearChildren(Transform parent)
        {
            for(int i=parent.childCount-1;i>=0;i--){
                var child=parent.GetChild(i);foreach(var button in child.GetComponentsInChildren<Button>(true))button.onClick.RemoveAllListeners();
                child.gameObject.SetActive(false);child.SetParent(null,false);UnityEngine.Object.Destroy(child.gameObject);
            }
        }
        static void AddBackdrop(Transform root, ScreenContext ctx)
        {
            PopupSkin.FullViewportBackdrop(ctx);
        }

        static RectTransform Panel(Transform parent, float x, float y, float w, float h, string title, Font font, int size)
        {
            var p = PopupSkin.Panel("Ornate stone panel", parent, x, y, w, h).rectTransform;
            p.GetComponent<Image>().raycastTarget = true;
            if (!string.IsNullOrEmpty(title)) Ui.Text("Title", p, 20, 10, w - 40, Mathf.Min(100, h - 20), title, size, font, Ui.Ivory);
            return p;
        }

        static Button Close(Transform parent, float x, float y, Font font, Action close)
        {
            return PopupSkin.Button("Close", parent, x, y, 100, 100, "×", font, () => close(), Red, 52);
        }

        static ScrollRect Scroll(Transform parent, float x, float y, float w, float h)
        {
            var viewport = Ui.Rect("Viewport", parent, x, y, w, h);
            var maskImage = viewport.gameObject.AddComponent<Image>(); maskImage.color = new Color(0,0,0,.001f); maskImage.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Ui.Rect("Content", viewport, 0, 0, w, h);
            content.anchorMin = new Vector2(0,1); content.anchorMax = new Vector2(1,1); content.pivot = new Vector2(.5f,1);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Elastic; scroll.scrollSensitivity = 36;
            return scroll;
        }

        static void Progress(Transform parent, float x, float y, float w, float h, float amount, string label, Font font)
        {
            var track = Ui.Rect("Progress", parent, x, y, w, h);
            // Empty illustrated rim, live fill and live number are independent reusable layers.
            // Keep the backing inside the bevel so transparent pointed corners stay clear.
            Ui.Image("Track", track, h * .5f, h * .2f, w - h, h * .6f, null, new Color(.015f,.025f,.035f));
            Ui.Image("Fill", track, h * .5f, h * .2f, (w - h) * Mathf.Clamp01(amount), h * .6f, null, new Color(.28f,.62f,.80f));
            var frame = Ui.ArtImage("Progress frame", track, 0, 0, w, h, ProgressFrame);
            frame.type = Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = 310f / h;
            Ui.Text("Value", track, 0, 0, w, h, label, Mathf.RoundToInt(h*.65f), font);
        }

        static Sprite[] skillIcons;
        static Sprite skillRing;
        static Sprite progressFrame;
        static Sprite ProgressFrame
        {
            get
            {
                if (progressFrame) return progressFrame;
                var texture = Resources.Load<Texture2D>("Moonlit/Skills/ProgressFrame-v1");
                if (!texture) return null;
                float sx = texture.width / 1922f, sy = texture.height / 818f;
                progressFrame = Sprite.Create(texture, new Rect(28*sx,254*sy,1866*sx,310*sy),
                    new Vector2(.5f,.5f),100*sx,0,SpriteMeshType.FullRect,new Vector4(220*sx,60*sy,220*sx,60*sy));
                progressFrame.name = "ProgressFrame-v1";
                return progressFrame;
            }
        }
        static Sprite SkillRing => skillRing ? skillRing : (skillRing = Resources.Load<Sprite>("Moonlit/Skills/SkillRing-v1"));

        static Sprite SkillIcon(int grade,int variant)
        {
            if(skillIcons==null)skillIcons=new Sprite[30];
            int index=grade*3+variant;
            if(!skillIcons[index])skillIcons[index]=Resources.Load<Sprite>(SkillCatalog.IconKey(grade,variant));
            return skillIcons[index];
        }
        static Sprite TicketIcon(int category) => Resources.Load<Sprite>(category==0?
            "Moonlit/Skills/SummonTicket-v1":category==1?
            "Moonlit/Popup/PetTicketEgg-v2":"Moonlit/Popup/MountTicketHoof-v2");

        static void FitCurrency(Text label)
        {
            label.resizeTextForBestFit=true;label.resizeTextMaxSize=Ui.ReadableFontSize(30);
            label.resizeTextMinSize=Ui.ReadableFontSize(18);
            label.verticalOverflow=VerticalWrapMode.Truncate;
        }
        static void RenderSummonCost(CollectionView view,int tickets,int diamonds)
        {
            ClearChildren(view.costRoot);
            var font=view.context.Assets.font;
            bool mixed=tickets>0&&diamonds>0;
            if(tickets>0){
                float x=mixed?0:77;
                Ui.ArtImage("Summon cost icon",view.costRoot,x,0,77,77,TicketIcon(view.tab)).preserveAspect=true;
                Ui.Text("Summon cost",view.costRoot,x+78,0,mixed?66:120,77,tickets.ToString(),30,font);
                FitCurrency(view.costRoot.Find("Summon cost").GetComponent<Text>());
            }
            if(mixed)Ui.Text("Cost plus",view.costRoot,144,0,28,77,"+",28,font);
            if(diamonds>0){
                var icons=view.context.Assets.interfaceIcons;
                Sprite diamond=icons!=null&&icons.Length>1?icons[1]:PopupSkin.RewardIcon(1);
                float x=mixed?175:83;
                Ui.ArtImage(tickets>0?"Summon diamond icon":"Summon cost icon",view.costRoot,x,0,65,77,diamond).preserveAspect=true;
                Ui.Text(tickets>0?"Summon diamond cost":"Summon cost",view.costRoot,x+67,0,112,77,diamonds.ToString(),30,font);
                FitCurrency(view.costRoot.Find(tickets>0?"Summon diamond cost":"Summon cost").GetComponent<Text>());
            }
        }

    }
    public sealed class ProgressionTick : MonoBehaviour
    {
        public Action tick;
        float remaining;
        void Update(){remaining-=Time.unscaledDeltaTime;if(remaining>0)return;remaining=1f;tick?.Invoke();}
        void OnDestroy(){tick=null;}
    }
}
