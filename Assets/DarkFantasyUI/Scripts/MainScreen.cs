using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    public sealed partial class MainScreen : MonoBehaviour
    {
        public Font font;
        public Sprite slotArt;
        public Sprite[] icons;
        public Transform design;
        public Transform toastRoot;
        public EquipmentSlot[] equipment;
        public Button forgeButton, autoButton, goldButton, gemButton, profileButton, stageButton, eventButton, fairyButton, chatButton;
        public Button forgeLevelButton, playerDetailsButton;
        public Image autoIcon;
        public Button[] navigation;
        public Text oreText, powerText, goldText, gemText, autoText, stageText, roundText;
        public Image[] waveNodes;
        public int ore = 1000;
        public int gems = 0;
        public int gold = 0;
        public int forgeLevel = 1;
        public int stage = 1;
        public int successfulForges;
        public bool autoForge;
        public int autoForgeBatchSize = 1;
        public int autoForgeFilterMask = -1;
        public bool autoForgeContinue = true;
        public bool[] autoForgeKeep = new bool[4];
        public bool offlineRewardsClaimed;
        public ItemDefinition PendingCraftItem { get; private set; }
        public int PendingCraftId { get; private set; }
        public int PendingCraftLevel { get; private set; }
        EquipmentSlot pendingCraftTarget;
        int nextCraftId;
        public UiScreenRegistry screens;
        Text toast;
        Coroutine toastRoutine;
        float autoClock;
        static readonly string[] NavigationRoutes = { "pvp", "dungeons", "skills-pets-heroes", "quests", "shop" };
        EquipmentSlot inspected;

        void Start()
        {
            foreach (var slot in equipment) slot.Clicked += Inspect;
            forgeButton.onClick.AddListener(Forge);
            if(forgeLevelButton) forgeLevelButton.onClick.AddListener(() => screens.Open("forge-probability"));
            if(playerDetailsButton) playerDetailsButton.onClick.AddListener(OpenLocalPlayerDetails);
            autoButton.onClick.AddListener(() => { if(ForgeState.Current.autoEnabled) ForgeRuntime.Ensure(this).StopAuto(); else screens.Open("auto-forge"); });
            goldButton.onClick.AddListener(() => Currency(false));
            gemButton.onClick.AddListener(() => Currency(true));
            profileButton.onClick.AddListener(() => screens.Open("profile"));
            stageButton.onClick.AddListener(Stage);
            eventButton.onClick.AddListener(() => screens.Open("offline-rewards"));
            fairyButton.onClick.AddListener(() => screens.Open("progress-pass"));
            chatButton.onClick.AddListener(() => screens.Open("chat"));
            for (int i=0;i<navigation.Length;i++) { int index=i; navigation[i].onClick.AddListener(()=>Navigate(index)); }
            Refresh();
        }
        void OnDestroy() { if(equipment != null) foreach(var s in equipment) if(s) s.Clicked -= Inspect; }
        void Update()
        {
            TickGameplay();
            if(autoIcon) autoIcon.rectTransform.localRotation=Quaternion.Euler(0,0,autoForge ? -Time.unscaledTime*90 : 0);
        }
        public void Refresh()
        {
            forgeLevel=ForgeState.Current.level;
            highestClearedStage=System.Math.Max(highestClearedStage,stage-1);
            if(oreText) oreText.text=ore.ToString("N0");
            if(goldText) goldText.text=Compact(gold);
            if(gemText) gemText.text=Compact(gems);
            var stats=ForgeState.Current.TotalStats;
            if(powerText) powerText.text=Compact(System.Math.Max(80,stats.health+CollectionProgression.OwnedHealth+CollectionProgression.EquippedHealth)+System.Math.Max(10,stats.attack+CollectionProgression.OwnedAttack+CollectionProgression.EquippedAttack)*8);
            if(stageText) stageText.text="스테이지 "+stage;
            if(autoText){ autoText.text="자동";autoText.color=autoForge?Ui.Cyan:Ui.Ivory; }
            if(autoIcon)autoIcon.color=autoForge?Ui.Cyan:Color.white;
            if(forgeLevelButton) {
                var label=forgeLevelButton.transform.Find("Forge level");
                if(label)label.GetComponent<Text>().text="대장간\n레벨 "+forgeLevel;
            }
            if(gameplayInitialized) SaveGame();
        }
        public static string Compact(double value)
        {
            if(value>=1e12)return value.ToString("0.##E+0",System.Globalization.CultureInfo.InvariantCulture);
            if(value>=1e9)return (value/1e9).ToString("0.##")+"b";
            if(value>=1e6)return (value/1e6).ToString("0.##")+"m";
            if(value>=10000)return (value/1e3).ToString("0.##")+"k";
            return value.ToString("N0");
        }
        public void Forge() { ForgeRuntime.Ensure(this).BeginManual(); }
        public void ConfigureAutoForge(int hammerCount,int filterMask,bool continueAfterMatch,bool[] keep)
        {
            autoForgeBatchSize=Mathf.Max(1,hammerCount);autoForgeFilterMask=filterMask;
            autoForgeContinue=continueAfterMatch;autoForgeKeep=keep==null?new bool[4]:(bool[])keep.Clone();
            var state=ForgeState.Current;state.batchSize=autoForgeBatchSize;state.affixMask=filterMask;
            state.continueAfterMatch=continueAfterMatch;
            state.keepTiers=new bool[10];
            if(keep!=null)System.Array.Copy(keep,state.keepTiers,System.Math.Min(keep.Length,10));
            ForgeRuntime.Ensure(this).StartAuto();
        }
        public void StopAutoForge(string message="자동 제련을 멈췄습니다")
        {
            ForgeRuntime.Ensure(this).StopAuto();Toast(message);
        }

        public bool ClaimOfflineRewards(int goldReward,int oreReward) => RewardState.Current.Claim(this);

        public bool BeginCraft(ItemDefinition crafted, int cost)
        {
            // Dismissing the comparison keeps its pending item without charging again.
            if (PendingCraftItem != null) return true;
            if (crafted == null || cost <= 0 || ore < cost || crafted.rarity == ItemRarity.Companion) return false;
            pendingCraftTarget = equipment == null ? null : System.Array.Find(equipment,
                slot => slot != null && slot.item == crafted && !slot.isLocked);
            PendingCraftItem = crafted;
            PendingCraftId = ++nextCraftId;
            PendingCraftLevel = pendingCraftTarget != null ? pendingCraftTarget.level + 1 : crafted.startingLevel;
            ore -= cost;
            Refresh();
            return true;
        }

        public bool ResolveCraftedEquipment(int craftId, bool equip, int saleOre)
        {
            // An old modal's callback must never resolve a newer pending item.
            if (PendingCraftItem == null || craftId != PendingCraftId || saleOre < 0) return false;
            if (equip)
            {
                if (pendingCraftTarget == null || pendingCraftTarget.isLocked || pendingCraftTarget.item != PendingCraftItem) return false;
                pendingCraftTarget.Bind(PendingCraftItem, PendingCraftLevel, false, true);
            }
            else ore += saleOre;
            PendingCraftItem = null;
            pendingCraftTarget = null;
            successfulForges++;
            Refresh();
            return true;
        }
        IEnumerator Pulse(EquipmentSlot slot)
        {
            slot.SetSelected(true); yield return new WaitForSeconds(.65f); if(inspected != slot) slot.SetSelected(false);
        }
        public void ToggleAuto() { if(autoForge)StopAutoForge();else ForgeRuntime.Ensure(this).StartAuto(); }

        public void Inspect(EquipmentSlot slot)
        {
            int index=System.Array.IndexOf(equipment,slot);
            if(index>=6){Toast(new[]{"엠블렘","날개","정령"}[index-6]+" · 기능 준비 중");return;}
            if(slot.item == null) { Toast("아직 장착한 장비가 없습니다. 모루를 눌러 제작하세요."); return; }
            inspected=slot; slot.SetSelected(true); slot.SetNotification(false);
            screens.Open("equipment-details", slot);
        }

        void OpenLocalPlayerDetails()
        {
            screens.Open("player-details", new System.Collections.Generic.Dictionary<string, object> {
                { "name", "moonzsanf" }, { "power", powerText.text }, { "rank", 389 }, { "avatarIndex", 0 }
            });
        }

        // Retained for the unsupplied quest route only.
        void InspectLegacy(EquipmentSlot slot)
        {
            Close(); inspected=slot; slot.SetSelected(true); slot.SetNotification(false);
            var p=Modal(slot.item.displayName,680);
            Ui.Text("Rarity",p,36,84,668,42,slot.item.rarity==ItemRarity.Companion ? "동료  ·  달빛의 수호자" : "전설  ·  장착 중",24,font,Ui.Gold);
            var frame=Ui.Image("Preview frame",p,270,145,200,200,slot.frame.sprite,slot.frame.color);
            frame.type=Image.Type.Sliced; frame.pixelsPerUnitMultiplier=7;
            Ui.Image("Independent item icon",frame.transform,18,10,164,166,slot.item.icon).preserveAspect=true;
            Ui.Text("Level",frame.transform,0,155,200,40,"Lv."+slot.level,29,font);
            Ui.Text("Description",p,50,372,640,102,slot.item.description+"\n전투력 +"+(slot.level*127).ToString("N0"),25,font);
            Ui.Button("Lock toggle",p,60,510,285,84,slot.isLocked ? "잠금 해제" : "장비 잠금",font,()=>{ slot.SetLocked(!slot.isLocked); Inspect(slot); });
            Ui.Button("Upgrade",p,365,510,315,84,"강화  ·  100",font,()=>Upgrade(slot),new Color(.28f,.13f,.035f));
            Ui.Text("Hint",p,40,610,660,40,"잠긴 장비는 자동 강화에서 제외됩니다",18,font,new Color(.6f,.65f,.7f));
        }
        void Upgrade(EquipmentSlot slot)
        {
            if(slot.isLocked) { Toast("잠금을 해제한 뒤 강화하세요"); return; }
            if(ore<100) { Toast("망치이 부족합니다"); return; }
            ore-=100; successfulForges++; slot.level++; slot.Refresh(); Refresh(); Inspect(slot); Toast("강화 성공!");
        }
        bool eventClaimed, fairyClaimed;
        void ClaimEvent() { if(eventClaimed) { Toast("오늘의 보상을 이미 받았습니다"); return; } eventClaimed=true; gold+=10000; Refresh(); Close(); Toast("골드 +10,000"); }
        void ClaimFairy() { if(fairyClaimed) { Toast("요정의 선물을 이미 받았습니다"); return; } fairyClaimed=true; ore+=300; Refresh(); Close(); Toast("망치 +300"); }
        void Currency(bool ruby) { screens.Open("wallet"); }
        public void Profile() { screens.Open("profile"); }
        public void ForgeManagement() { screens.Open("forge-probability"); }
        void Stage() { Toast("스테이지 "+stage+" · 3웨이브 · 웨이브당 15라운드"); }
        public void Navigate(int index)
        {
            if (screens == null || index < 0 || index >= NavigationRoutes.Length || screens.ModalDepth > 0) return;
            string route = NavigationRoutes[index];
            if (screens.ActivePageKey == route) { screens.ShowMainPage(); return; }
            if (index == 3) screens.Register("quests", ScreenPresentation.Page, context => {
                Ui.Image("Quest page shade",context.Root,0,0,context.Width,context.Height,null,new Color(0,.015f,.025f,.65f)).raycastTarget=true;
                var panel=PopupSkin.Panel("Quest panel",context.Root,150,(context.Height-210-620)/2,780,620).rectTransform;
                Ui.Text("Quest title",panel,40,42,700,70,"모험 퀘스트",40,font,Ui.Gold);
                Ui.Text("Quest progress",panel,60,155,660,230,"장비 강화  "+successfulForges+" / 10\n\n대장간에서 장비를 강화해 보세요.\n퀘스트 보상 : 다이아 10개",29,font);
                PopupSkin.Button("Quest reward",panel,200,428,380,88,"보상 받기",font,ClaimQuest);
                PopupSkin.Close("Close",panel,348,548,84,font,context.Close);
            },false);
            screens.Open(route);
        }

        public void RefreshNavigation(string activePage)
        {
            if (navigation == null) return;
            for (int i=0; i<navigation.Length; i++)
            {
                var button=navigation[i];
                if (!button) continue;
                bool active=i<NavigationRoutes.Length && NavigationRoutes[i]==activePage;
                var icon=button.transform.Find("Menu icon");
                var close=button.transform.Find("Close icon");
                var notification=button.transform.Find("Notification");
                if (icon) icon.gameObject.SetActive(!active);
                if (close) close.gameObject.SetActive(active);
                if (notification) notification.gameObject.SetActive(!active);
                var visible=active ? close : icon;
                if (visible)
                {
                    button.targetGraphic=visible.GetComponent<Image>();
                    button.targetGraphic.color=Color.white;
                    var feedback=button.GetComponent<ButtonFeedback>();
                    if (feedback) feedback.artwork=(RectTransform)visible;
                }
            }
        }
        bool questClaimed;
        void ClaimQuest() { if(questClaimed) { Toast("이미 받은 보상입니다"); return; } if(successfulForges<10) { Toast("장비를 10회 강화하면 받을 수 있습니다"); return; } questClaimed=true; gems+=10; Refresh(); Close(); Toast("다이아 +10"); }

        Transform Modal(string title, float height)
        {
            Transform result=null;
            screens.Register("moonlit.legacy-dialog",ScreenPresentation.Modal,context=> {
            var panel=Ui.Panel("Dialog",context.Root,170,(context.Height-height)/2,740,height,new Color(.025f,.055f,.075f)); panel.raycastTarget=true;
            panel.rectTransform.anchorMin=panel.rectTransform.anchorMax=panel.rectTransform.pivot=new Vector2(.5f,.5f);
            panel.rectTransform.anchoredPosition=Vector2.zero;
            Ui.Text("Title",panel.transform,55,25,630,56,title,35,font);
            Ui.Image("Divider",panel.transform,45,94,650,2,null,Ui.Gold);
            Ui.Button("Close",panel.transform,662,12,60,60,"×",font,Close,new Color(.06f,.08f,.1f),35);
            result=panel.transform;
            });
            screens.Open("moonlit.legacy-dialog");
            return result;
        }
        void ShowInfo(string title,string body,string action,UnityEngine.Events.UnityAction callback)
        {
            Close(); var p=Modal(title,590);
            Ui.Text("Body",p,55,130,630,300,body,28,font);
            Ui.Button("Action",p,100,465,540,85,action,font,callback);
        }
        public void Close()
        {
            if(inspected) inspected.SetSelected(false); inspected=null;
            if(screens != null) screens.CloseTop();
        }
        public void Toast(string message)
        {
            if(toastRoutine != null) StopCoroutine(toastRoutine);
            if(!toast) {
                var bg=Ui.Panel("Toast",toastRoot ? toastRoot : design,140,855,800,75,new Color(.02f,.04f,.055f,.97f));
                bg.rectTransform.anchorMin=bg.rectTransform.anchorMax=bg.rectTransform.pivot=new Vector2(.5f,0);
                bg.rectTransform.anchoredPosition=new Vector2(0,995);
                toast=Ui.Text("Message",bg.transform,12,3,776,69,"",25,font,Ui.Ivory);
            }
            toast.transform.parent.SetAsLastSibling(); toast.transform.parent.gameObject.SetActive(true); toast.text=message;
            toastRoutine=StartCoroutine(HideToast());
        }
        IEnumerator HideToast() { yield return new WaitForSecondsRealtime(2); if(toast) toast.transform.parent.gameObject.SetActive(false); }
    }
}
