using System;
using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    public static partial class SocialScreenModule
    {
        static int selectedLanguage=3;
        static readonly bool[] blockedPlayers={true,true,true};
        static readonly string[] Languages={"English","Deutsch","日本語","한국어","français","español",
            "português (Brasil)","italiano","русский","Türkçe (Türkiye)","中文 (中国)"};

        static void RegisterProfileSettingsDialogs(UiScreenRegistry registry)
        {
            registry.Register("profile-name",ScreenPresentation.Modal,BuildNameDialog,false);
            registry.Register("profile-gender",ScreenPresentation.Modal,BuildGenderDialog,false);
            registry.Register("profile-avatar",ScreenPresentation.Modal,BuildAvatarDialog,false);
            registry.Register("settings-language",ScreenPresentation.Modal,BuildLanguageDialog,false);
            registry.Register("settings-blocked",ScreenPresentation.Modal,BuildBlockedDialog,false);
            registry.Register("settings-account",ScreenPresentation.Modal,BuildAccountDialog,false);
        }

        static RectTransform ChildFrame(ScreenContext c,string title,float preferredWidth,float preferredHeight,
            out float w,out float h,bool close=true)
        {
            w=Mathf.Min(preferredWidth,c.Width-80);
            h=Mathf.Min(preferredHeight,c.Height-140);
            var frame=PopupSkin.Panel("Child dialog frame",c.Root,(c.Width-w)*.5f,(c.Height-h)*.5f,w,h);
            frame.raycastTarget=true;
            Ui.ArtImage("Child crest",frame.transform,w*.5f-66,-48,132,104,PopupSkin.CrestArt).preserveAspect=true;
            if(!string.IsNullOrEmpty(title))
            {
                Ui.Text("Child title",frame.transform,40,35,w-80,70,title,40,Font(c));
                Ui.Image("Child heading rule",frame.transform,30,116,w-60,2,null,Ui.Gold);
            }
            if(close) PopupSkin.Close("Child close",frame.transform,w*.5f-48,h-50,96,Font(c),c.Close,58);
            return frame.rectTransform;
        }

        static void RefreshProfileParent(ScreenContext c)
        {
            (c.Payload as System.Action)?.Invoke();
            if(c.Main && c.Main.profileButton)
            {
                var name=c.Main.profileButton.transform.Find("Player name");
                if(name && name.TryGetComponent<Text>(out var label)) label.text=profileName;
                var portrait=c.Main.profileButton.transform.Find("Portrait");
                if(portrait && portrait.TryGetComponent<Image>(out var image)) image.sprite=AvatarPortrait(profileAvatar);
            }
        }

        static void BuildNameDialog(ScreenContext c)
        {
            var frame=ChildFrame(c,"",680,380,out float w,out float h,false);
            var input=Input(c,frame,76,76,w-152,84,"");
            input.name="Nickname input"; input.characterLimit=16;
            input.textComponent.alignment=TextAnchor.MiddleCenter;
            var placeholder=Ui.Text("Nickname placeholder",input.transform,12,0,w-176,84,"플레이어 이름 입력",30,Font(c),new Color(.5f,.52f,.55f));
            input.placeholder=placeholder;
            var inputRim=PopupSkin.Panel("Nickname input rim",input.transform,0,0,w-152,84);
            inputRim.fillCenter=false; inputRim.raycastTarget=false;
            inputRim.pixelsPerUnitMultiplier=22;
            float buttonWidth=(w-192)*.5f;
            PopupSkin.Button("Nickname cancel",frame,76,220,buttonWidth,100,"취소",Font(c),c.Close,Red,38);
            Button confirm=null;
            bool applied=false;
            bool Valid()=>!applied && c.Main && c.Main.gems>=200 && !string.IsNullOrWhiteSpace(input.text) && input.text.Trim()!=profileName;
            confirm=PopupSkin.Button("Nickname confirm",frame,w*.5f+20,220,buttonWidth,100,"",Font(c),()=>{
                if(!Valid()) return;
                applied=true;
                profileName=input.text.Trim(); c.Main.gems-=200;
                if(c.Main.gemText) c.Main.gemText.text=c.Main.gems.ToString();
                RefreshProfileParent(c); c.Close();
            });
            Ui.Text("Confirm label",confirm.transform,8,7,buttonWidth-16,42,"확인",32,Font(c));
            var ruby=Ui.ArtImage("Nickname ruby",confirm.transform,buttonWidth*.5f-62,52,34,34,Icon(c,1));
            ruby.preserveAspect=true;
            Ui.Text("Nickname price",confirm.transform,buttonWidth*.5f-22,45,95,44,"200",32,Font(c),c.Main && c.Main.gems>=200?Ui.Ivory:Color.red,TextAnchor.MiddleLeft);
            input.onValueChanged.AddListener(_=>confirm.interactable=Valid());
            confirm.interactable=Valid();
        }

        static void BuildGenderDialog(ScreenContext c)
        {
            var frame=ChildFrame(c,"",640,380,out float w,out float h);
            var checks=new Text[2];
            for(int i=0;i<2;i++)
            {
                int index=i;
                float x=w*.5f-150+i*174;
                var button=PopupSkin.Button("Gender "+i,frame,x,112,128,128,i==0?"♂":"♀",Font(c),()=>{
                    profileFemale=index==1;
                    for(int j=0;j<2;j++) checks[j].gameObject.SetActive(j==(profileFemale?1:0));
                    RefreshProfileParent(c);
                },i==0?Blue:Red,92);
                button.transform.Find("Label").GetComponent<Text>().color=i==0?new Color(.1f,.65f,1f):new Color(1f,.12f,.3f);
                checks[i]=Ui.Text("Gender selected",button.transform,88,88,48,48,"✓",46,Font(c),Green);
                checks[i].gameObject.SetActive(i==(profileFemale?1:0));
            }
        }

        static void BuildAvatarDialog(ScreenContext c)
        {
            var frame=ChildFrame(c,"아바타를 선택하세요",650,908,out float w,out float h);
            float innerWidth=w-84,cell=(innerWidth-36)/4;
            var scroll=Scroll(c,frame,42,136,innerWidth,h-196,5*(cell+12)-12,out var content);
            scroll.name="Avatar scroll";
            var checks=new Text[20];
            for(int i=0;i<20;i++)
            {
                int index=i;
                var portrait=Avatar(c,content,i%4*(cell+12),i/4*(cell+12),cell,i);
                portrait.name="Avatar choice "+i; portrait.raycastTarget=true;
                var rim=PopupSkin.Panel("Avatar rim",portrait.transform,0,0,cell,cell);
                rim.fillCenter=false; rim.raycastTarget=false;
                // Keep the reusable rim near the edge so it cannot cover portrait faces.
                rim.pixelsPerUnitMultiplier=22;
                var button=portrait.gameObject.AddComponent<Button>(); button.targetGraphic=portrait;
                checks[i]=Ui.Text("Avatar selected",portrait.transform,cell-40,cell-44,44,44,"✓",40,Font(c),Green);
                checks[i].gameObject.SetActive(i==profileAvatar);
                button.onClick.AddListener(()=>{
                    profileAvatar=index;
                    for(int j=0;j<checks.Length;j++) checks[j].gameObject.SetActive(j==index);
                    RefreshProfileParent(c);
                });
            }
        }

        static void BuildLanguageDialog(ScreenContext c)
        {
            var frame=ChildFrame(c,"언어 선택",600,940,out float w,out float h);
            var scroll=Scroll(c,frame,34,124,w-68,h-194,11*66,out var content);
            scroll.name="Language scroll";
            var group=content.gameObject.AddComponent<ToggleGroup>(); group.allowSwitchOff=false;
            for(int i=0;i<Languages.Length;i++)
            {
                int index=i;
                var row=Ui.Image("Language "+i,content,0,i*66,w-68,66,null,Color.clear); row.raycastTarget=true;
                var box=PopupSkin.Panel("Language checkbox",row.transform,24,11,44,44);
                Ui.Text("Language label",row.transform,98,0,w-186,66,Languages[i],30,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
                Ui.Image("Language rule",row.transform,0,64,w-68,1,null,new Color(.6f,.45f,.25f,.6f));
                var check=Ui.Text("Language selected",box.transform,0,-2,44,44,"✓",38,Font(c),Green);
                var toggle=row.gameObject.AddComponent<UnityEngine.UI.Toggle>();
                toggle.targetGraphic=box; toggle.graphic=check; toggle.isOn=i==selectedLanguage;
                toggle.group=group;
                toggle.onValueChanged.AddListener(on=>{
                    if(on) { selectedLanguage=index; c.Toast("언어 선택을 저장했습니다 · 번역은 데모에 연결되지 않았습니다."); }
                });
            }
        }

        static void BuildBlockedDialog(ScreenContext c)
        {
            var frame=ChildFrame(c,"차단 목록",640,1060,out float w,out float h);
            var scroll=Scroll(c,frame,34,132,w-68,h-306,380,out var content);
            scroll.name="Blocked players scroll";
            int selected=-1;
            string[] names={"[CLUES] Tomatensalat","[DE] Tresenbaer ♂","[LUX] Logien92 ♂"};
            int[] portraits={8,14,0};
            var rows=new GameObject[3];
            var checks=new Text[3];
            Button remove=null;
            var empty=Ui.Text("No blocked players",frame,50,220,w-100,90,"차단한 플레이어가 없습니다",28,Font(c));
            void Reflow()
            {
                int visible=0;
                for(int i=0;i<3;i++)
                {
                    rows[i].SetActive(blockedPlayers[i]);
                    if(blockedPlayers[i]) ((RectTransform)rows[i].transform).anchoredPosition=new Vector2(0,-visible++*124);
                }
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(1,visible*124));
                empty.gameObject.SetActive(visible==0);
            }
            for(int i=0;i<3;i++)
            {
                int index=i;
                var row=PopupSkin.Panel("Blocked player "+i,content,0,i*124,w-68,116); row.raycastTarget=true;
                rows[i]=row.gameObject;
                Avatar(c,row.transform,12,10,96,portraits[i]);
                Ui.Text("Blocked player name",row.transform,120,16,w-266,80,names[i],25,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
                var crest=Ui.ArtImage("Blocked player crest",row.transform,w-136,25,54,66,Resources.Load<Sprite>("Moonlit/Social/GoldLeagueCrest-v1"));
                crest.preserveAspect=true;
                checks[i]=Ui.Text("Blocked player selected",row.transform,w-122,75,36,36,"✓",30,Font(c),Green);
                checks[i].gameObject.SetActive(false);
                var button=row.gameObject.AddComponent<Button>(); button.targetGraphic=row;
                button.onClick.AddListener(()=>{
                    selected=index;
                    for(int j=0;j<3;j++) checks[j].gameObject.SetActive(j==selected);
                    remove.gameObject.SetActive(true);
                });
            }
            remove=Action(c,frame,w*.5f-138,h-158,276,70,"차단 해제",()=>{
                if(selected<0 || !blockedPlayers[selected]) return;
                blockedPlayers[selected]=false; selected=-1; Reflow();
                remove.gameObject.SetActive(false);
                c.Toast("데모 차단 목록에서 해제했습니다.");
            });
            remove.gameObject.SetActive(false); Reflow();
        }

        static void BuildAccountDialog(ScreenContext c)
        {
            var frame=ChildFrame(c,"계정",600,490,out float w,out float h);
            Ui.Text("Account explanation",frame,44,120,w-88,96,"게임 데이터를 안전하게 보호하려면 계정을 연결하세요",28,Font(c));
            var link=Ui.Image("Account link",frame,40,242,w*.5f-58,84,null,Color.clear); link.raycastTarget=true;
            Ui.Text("Account link label",link.transform,8,0,w*.5f-74,84,"계정 연결",30,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            var button=link.gameObject.AddComponent<Button>(); button.targetGraphic=link;
            button.onClick.AddListener(()=>c.Toast("계정 연결은 데모에 연결되지 않았습니다."));
            PopupSkin.Button("Account logout",frame,w*.5f+28,248,w*.5f-70,74,"로그아웃",Font(c),()=>c.Toast("로컬 데모입니다. 실제 로그아웃은 실행되지 않습니다."),Red,30);
            Ui.Image("Account divider",frame,30,340,w-60,2,null,Ui.Gold);
            var deletion=Ui.Image("Account delete",frame,w*.5f-106,354,212,60,null,Color.clear); deletion.raycastTarget=true;
            Ui.Text("Account delete label",deletion.transform,0,0,212,54,"계정 삭제?",28,Font(c));
            Ui.Image("Account delete underline",deletion.transform,30,48,152,1,null,Ui.Ivory);
            var deleteButton=deletion.gameObject.AddComponent<Button>(); deleteButton.targetGraphic=deletion;
            deleteButton.onClick.AddListener(()=>c.Toast("로컬 데모입니다. 실제 계정 삭제는 실행되지 않습니다."));
        }
    }
}
