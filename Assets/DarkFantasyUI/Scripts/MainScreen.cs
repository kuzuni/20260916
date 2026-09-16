using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    public sealed class MainScreen : MonoBehaviour
    {
        public Font font;
        public Sprite slotArt;
        public Sprite[] icons;
        public Transform design;
        public Transform toastRoot;
        public EquipmentSlot[] equipment;
        public Button forgeButton, autoButton, goldButton, gemButton, profileButton, stageButton, eventButton, fairyButton, chatButton;
        public Button forgeLevelButton;
        public Image autoIcon;
        public Button[] navigation;
        public Text oreText, powerText, goldText, gemText, autoText, stageText;
        public int ore = 40351;
        public int gems = 21;
        public int gold = 1550000;
        public int forgeLevel = 33;
        public int stage = 13;
        public int successfulForges;
        public bool autoForge;
        public UiScreenRegistry screens;
        Text toast;
        Coroutine toastRoutine;
        float autoClock;
        int selectedMenu;
        EquipmentSlot inspected;

        void Start()
        {
            foreach (var slot in equipment) slot.Clicked += Inspect;
            forgeButton.onClick.AddListener(Forge);
            if(forgeLevelButton) forgeLevelButton.onClick.AddListener(ForgeManagement);
            autoButton.onClick.AddListener(ToggleAuto);
            goldButton.onClick.AddListener(() => Currency(false));
            gemButton.onClick.AddListener(() => Currency(true));
            profileButton.onClick.AddListener(Profile);
            stageButton.onClick.AddListener(Stage);
            eventButton.onClick.AddListener(() => ShowInfo("심연의 축제", "이벤트 종료까지 5일 3시간\n\n어둠 속에서 횃불을 모으고\n전설 장비를 찾아보세요.", "일일 보상 받기", ClaimEvent));
            fairyButton.onClick.AddListener(() => ShowInfo("달빛 요정의 선물", "모험가를 위한 작은 축복\n\n요정이 강화석 300개를 준비했어요.", "선물 받기", ClaimFairy));
            chatButton.onClick.AddListener(() => ShowInfo("월드 채팅 · 미리보기", "Tacoma : 오늘도 전설 장비 도전!\nGuest 41194 : 달빛 폐허 분위기 좋네요.\n\n현재는 로컬 UI 데모입니다.", "확인", Close));
            for (int i=0;i<navigation.Length;i++) { int index=i; navigation[i].onClick.AddListener(()=>Navigate(index)); }
            Refresh();
        }
        void OnDestroy() { if(equipment != null) foreach(var s in equipment) if(s) s.Clicked -= Inspect; }
        void Update()
        {
            if (autoForge && (screens == null || screens.ModalDepth == 0)) { autoClock += Time.deltaTime; if(autoClock>=1.4f) { autoClock=0; Forge(); } }
            if(autoIcon) autoIcon.rectTransform.localRotation=Quaternion.Euler(0,0,autoForge ? -Time.unscaledTime*90 : 0);
        }
        public void Refresh()
        {
            oreText.text = ore.ToString();
            goldText.text = (gold/1000000f).ToString("0.00")+"m";
            gemText.text = gems.ToString();
            powerText.text = (65.5f+successfulForges*.12f).ToString("0.0")+"b";
            stageText.text = "어려움 4-"+stage;
            autoText.text = "자동";
            autoText.color = autoForge ? Ui.Cyan : Ui.Ivory;
            if(autoIcon) autoIcon.color=autoForge ? Ui.Cyan : Color.white;
        }
        public void Forge()
        {
            if(ore<100) { autoForge=false; Refresh(); Toast("강화석이 부족합니다"); return; }
            var available = System.Array.FindAll(equipment,s=>s.item != null && !s.isLocked && s.item.rarity!=ItemRarity.Companion);
            if(available.Length==0) { autoForge=false; Refresh(); Toast("강화할 수 있는 장비가 없습니다"); return; }
            var target=available[successfulForges%available.Length];
            ore-=100; successfulForges++; target.level++; target.Refresh();
            Refresh(); Toast(target.item.displayName+" 강화 성공  ·  Lv."+target.level);
            StartCoroutine(Pulse(target));
        }
        IEnumerator Pulse(EquipmentSlot slot)
        {
            slot.SetSelected(true); yield return new WaitForSeconds(.65f); if(inspected != slot) slot.SetSelected(false);
        }
        public void ToggleAuto() { autoForge=!autoForge; autoClock=0; Refresh(); Toast(autoForge ? "자동 강화를 시작합니다 · 1회 100 강화석" : "자동 강화를 멈췄습니다"); }

        public void Inspect(EquipmentSlot slot)
        {
            if(slot.item == null) { Toast("비어 있는 장비 슬롯입니다"); return; }
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
            if(ore<100) { Toast("강화석이 부족합니다"); return; }
            ore-=100; successfulForges++; slot.level++; slot.Refresh(); Refresh(); Inspect(slot); Toast("강화 성공!");
        }
        bool eventClaimed, fairyClaimed;
        void ClaimEvent() { if(eventClaimed) { Toast("오늘의 보상을 이미 받았습니다"); return; } eventClaimed=true; gold+=10000; Refresh(); Close(); Toast("골드 +10,000"); }
        void ClaimFairy() { if(fairyClaimed) { Toast("요정의 선물을 이미 받았습니다"); return; } fairyClaimed=true; ore+=300; Refresh(); Close(); Toast("강화석 +300"); }
        void Currency(bool ruby) { ShowInfo(ruby ? "루비" : "골드",ruby ? "보유 루비  "+gems+"\n\n루비는 모험 보상으로 획득할 수 있습니다." : "보유 골드  "+gold.ToString("N0")+"\n\n모험과 이벤트에서 골드를 모으세요.","확인",Close); }
        public void Profile() { ShowInfo("moonzsanf", "그림자 검객  ·  Lv.108\n\n전투력 "+powerText.text+"\n최고 기록 : 어려움 4-"+stage+"\n대장간 레벨 "+forgeLevel,"확인",Close); }
        public void ForgeManagement() { ShowInfo("대장간  ·  레벨 "+forgeLevel,"모루를 눌러 장비를 강화하세요.\n\n강화 1회 : 강화석 100개\n자동 강화에서는 잠긴 장비가 제외됩니다.\n\n남은 시간  1일 7시","확인",Close); }
        void Stage() { ShowInfo("달빛 폐허  ·  4-"+stage, "고대 성당 너머의 어둠\n\n권장 전투력 62.0b\n완료 보상 : 골드 5,000 · 강화석 150\n\nUI 데모에서 스테이지 완료를 시뮬레이션합니다.","스테이지 완료 체험",()=>{ stage++; gold+=5000; ore+=150; Refresh(); Close(); Toast("스테이지 완료! 다음 구역으로 이동합니다"); }); }
        void Navigate(int index)
        {
            selectedMenu=index;
            for(int i=0;i<navigation.Length;i++) navigation[i].targetGraphic.color=i==index ? Color.white : new Color(.9f,.9f,.9f,1);
            if(index==0) { Close(); return; }
            if(index==1) ShowInfo("던전", "달빛 폐허\n어려움 4-"+stage+"\n\n현재 전투력 "+powerText.text,"스테이지 보기",Stage);
            if(index==2) Inspect(equipment[equipment.Length-1]);
            if(index==3) ShowInfo("모험 퀘스트", "장비 강화  "+successfulForges+" / 10\n\n대장간에서 장비를 강화해 보세요.\n퀘스트 보상 : 루비 10개","보상 받기",ClaimQuest);
            if(index==4) ShowInfo("교환소", "강화석 보급 상자\n\n골드 10,000 → 강화석 500\n보유 골드 "+gold.ToString("N0"),"교환하기",()=>{ if(gold<10000) { Toast("골드가 부족합니다"); return; } gold-=10000; ore+=500; Refresh(); Close(); Toast("강화석 500개를 받았습니다"); });
        }
        bool questClaimed;
        void ClaimQuest() { if(questClaimed) { Toast("이미 받은 보상입니다"); return; } if(successfulForges<10) { Toast("장비를 10회 강화하면 받을 수 있습니다"); return; } questClaimed=true; gems+=10; Refresh(); Close(); Toast("루비 +10"); }

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
