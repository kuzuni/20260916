using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Moonlit.UI
{
    /// <summary>Runtime-only implementation of the nine social, PvP and shop references.</summary>
    public static partial class SocialScreenModule
    {
        static readonly Color Ink = new Color(.025f, .055f, .075f, .97f);
        static readonly Color Stone = new Color(.055f, .085f, .105f, .98f);
        static readonly Color Blue = new Color(.02f, .23f, .48f, 1f);
        static readonly Color Red = new Color(.42f, .035f, .045f, 1f);
        static readonly Color Green = new Color(.25f, 1f, .28f, 1f);
        const float NavigationReserve = 210f;
        static int profileAvatar;
        static string profileName = "moonzzanf";
        static bool profileFemale;
        static readonly bool[] settingValues = { false, false, false, true, false, false };
        static Sprite[] avatarPortraits, settingsIcons;

        internal static void ResetSession()
        {
            profileAvatar=0; profileName="moonzzanf"; profileFemale=false;
            for(int i=0;i<settingValues.Length;i++) settingValues[i]=i==3;
            avatarPortraits=null; settingsIcons=null; shopIllustrations=null;
            selectedLanguage=3;
            for(int i=0;i<blockedPlayers.Length;i++) blockedPlayers[i]=true;
        }

        public static void Register(UiScreenRegistry registry)
        {
            RegisterProfileSettingsDialogs(registry);
            registry.Register("profile", ScreenPresentation.Modal, c => BuildProfile(c, false));
            registry.Register("settings", ScreenPresentation.Modal, c => BuildProfile(c, true));
            registry.Register("player-details", ScreenPresentation.Modal, BuildPlayerDetails);
            registry.Register("chat", ScreenPresentation.Fullscreen, BuildChat);
            registry.Register("power-ranking", ScreenPresentation.Modal, BuildRanking);
            registry.Register("shop", ScreenPresentation.Page, BuildShop);
            registry.Register("pvp-opponents", ScreenPresentation.Modal, BuildOpponents);
            registry.Register("pvp", ScreenPresentation.Page, BuildPvp);
            registry.Register("pvp-rewards", ScreenPresentation.Modal, BuildRewards);
        }

        static Font Font(ScreenContext c) => c.Assets != null ? c.Assets.font : null;
        static Sprite PanelSprite(ScreenContext c) => c.Assets != null && c.Assets.panels != null && c.Assets.panels.Length > 1 ? c.Assets.panels[1] : null;
        static Sprite PanelSprite(ScreenContext c, int index) => c.Assets != null && c.Assets.panels != null && c.Assets.panels.Length > index ? c.Assets.panels[index] : null;
        static Sprite Icon(ScreenContext c, int index)
        {
            var icons = c.Assets != null ? c.Assets.interfaceIcons : null;
            return icons != null && icons.Length > 0 ? icons[Mathf.Abs(index) % icons.Length] : null;
        }

        static RectTransform Frame(ScreenContext c, string title, float preferredHeight, out float width, out float height)
        {
            width = Mathf.Min(940f, c.Width - 56f);
            height = Mathf.Min(preferredHeight, c.Height - 72f);
            float x = (c.Width - width) * .5f;
            float y = Mathf.Max(24f, (c.Height - height) * .5f);
            var frame = PopupSkin.Panel(title + " frame", c.Root, x, y, width, height);
            frame.raycastTarget = true;
            Ui.Text("Title", frame.transform, 70, 18, width - 140, 70, title, 44, Font(c), Ui.Ivory);
            Ui.Image("Title rule", frame.transform, 28, 94, width - 56, 3, null, Ui.Gold);
            return frame.rectTransform;
        }

        static Button Close(ScreenContext c, Transform parent, float width, float height)
            => PopupSkin.Close("Close", parent, width * .5f - 42, height - 86, 84, Font(c), c.Close, 56);

        static Button Action(ScreenContext c, Transform parent, float x, float y, float w, float h, string label, UnityAction action)
            => PopupSkin.Button(label, parent, x, y, w, h, label, Font(c), action);

        static Image SpritePanel(ScreenContext c, string name, Transform parent, float x, float y, float w, float h, int spriteIndex, Color tint)
        {
            var panel = Ui.Image(name, parent, x, y, w, h, PanelSprite(c, spriteIndex), tint);
            panel.type = Image.Type.Sliced;
            panel.pixelsPerUnitMultiplier = 5;
            return panel;
        }

        static void Currency(ScreenContext c, Transform parent, float x, float y, float iconSize, int iconIndex, string value, Color? color = null)
        {
            var icon = Ui.Image(value + " icon", parent, x, y, iconSize, iconSize, Icon(c, iconIndex));
            icon.preserveAspect = true;
            Ui.Text(value + " value", parent, x + iconSize + 8, y, 170, iconSize, value, 28, Font(c), color ?? Ui.Ivory, TextAnchor.MiddleLeft);
        }

        static Image Avatar(ScreenContext c, Transform parent, float x, float y, float size, int index)
        {
            var bg = Ui.Panel("Avatar " + index, parent, x, y, size, size, new Color(.08f, .11f, .13f));
            var art = Ui.Image("Avatar artwork", bg.transform, 8, 8, size - 16, size - 16, AvatarPortrait(index));
            art.preserveAspect = true;
            return bg;
        }

        static Sprite AvatarPortrait(int index)
        {
            if (avatarPortraits == null || !avatarPortraits[0])
            {
                var atlas = Resources.Load<Texture2D>("Moonlit/Social/AvatarPortraits-v2");
                if (!atlas) return null;
                avatarPortraits = new Sprite[20];
                float width = atlas.width / 4f, height = atlas.height / 5f;
                for (int i = 0; i < avatarPortraits.Length; i++)
                {
                    avatarPortraits[i] = Sprite.Create(atlas, new Rect(i % 4 * width, (4 - i / 4) * height, width, height),
                        new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                    avatarPortraits[i].name = "Avatar portrait " + i;
                }
            }
            return avatarPortraits[Mathf.Abs(index) % avatarPortraits.Length];
        }

        static void BuildProfile(ScreenContext c, bool settingsFirst)
        {
            float w, h;
            var frame = Frame(c, settingsFirst ? "설정" : "프로필", 1210, out w, out h);
            var host = Ui.Rect("Tab content", frame, 28, 110, w - 56, h - 232);
            var profile = Ui.Rect("Profile content", host, 0, 0, host.rect.width, host.rect.height);
            var settings = Ui.Rect("Settings content", host, 0, 0, host.rect.width, host.rect.height);

            BuildProfileTab(c, profile, w - 56, h - 232);
            BuildSettingsTab(c, settings, w - 56, h - 232);
            Button profileTab = null, settingsTab = null;
            void Select(bool showSettings)
            {
                profile.gameObject.SetActive(!showSettings);
                settings.gameObject.SetActive(showSettings);
                frame.Find("Title").GetComponent<Text>().text = showSettings ? "설정" : "프로필";
                foreach (var tab in new[] { profileTab, settingsTab })
                {
                    if (tab == null) continue;
                    PopupSkin.Select(tab, (tab == settingsTab) == showSettings);
                }
            }
            profileTab = Action(c, frame, 120, h - 156, (w - 240) * .5f, 64, "프로필", () => Select(false));
            settingsTab = Action(c, frame, w * .5f, h - 156, (w - 240) * .5f, 64, "설정", () => Select(true));
            Close(c, frame, w, h);
            Select(settingsFirst);
        }

        static void BuildProfileTab(ScreenContext c, Transform root, float w, float h)
        {
            var portrait = Avatar(c, root, 52, 38, 190, profileAvatar);
            Ui.Text("Name label", root, 275, 32, 170, 48, "이름:", 27, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
            var name = Input(c, root, 275, 80, w - 407, 64, profileName);
            name.name = "Profile name"; name.characterLimit = 16;
            name.readOnly = true;
            Ui.Text("Gender label", root, 275, 156, 170, 44, "성별:", 27, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
            var gender = Input(c, root, 275, 202, w - 407, 64, profileFemale ? "♀" : "♂");
            gender.name = "Profile gender"; gender.readOnly = true;
            gender.textComponent.color=profileFemale?new Color(1f,.12f,.3f):new Color(.1f,.65f,1f);
            System.Action refresh=()=>{
                if(name) name.text=profileName;
                if(gender) {
                    gender.text=profileFemale?"♀":"♂";
                    gender.textComponent.color=profileFemale?new Color(1f,.12f,.3f):new Color(.1f,.65f,1f);
                }
                if(portrait) portrait.transform.Find("Avatar artwork").GetComponent<Image>().sprite=AvatarPortrait(profileAvatar);
            };
            ProfileEditButton(root,115,240,"아바타 변경",()=>c.Open("profile-avatar",refresh));
            ProfileEditButton(root,w-118,80,"이름 변경",()=>c.Open("profile-name",refresh));
            ProfileEditButton(root,w-118,202,"변경",()=>c.Open("profile-gender",refresh));
            Ui.Image("Divider", root, 55, 330, w - 110, 3, null, Ui.Gold);
            Ui.Text("Server rank", root, 55, 354, w - 110, 62, "서버 5 순위", 34, Font(c));
            Action(c, root, 105, 430, 300, 82, "파워 랭킹", () => c.Open("power-ranking"));
            Action(c, root, w - 405, 430, 300, 82, "클랜 랭킹", () => c.Toast("클랜 랭킹은 데모에 연결되지 않았습니다."));
            var scenery = Resources.Load<Sprite>("Moonlit/Social/ProfileRuins-v1");
            if (scenery != null)
            {
                // This is scenery only; portrait, frames, buttons and live text remain independent.
                var painting = Ui.Image("Profile ruins painting", root, 36, 536, w - 72, Mathf.Max(1, h - 548), scenery);
                painting.preserveAspect = true;
                painting.raycastTarget = false;
            }
        }

        static Button ProfileEditButton(Transform parent,float x,float y,string name,UnityAction click)
        {
            var button=Ui.ArtButton(name,parent,x,y,64,64,PopupSkin.ActionArt,true,8);
            Ui.ArtImage("Edit pencil",button.transform,7,7,50,50,
                Resources.Load<Sprite>("Moonlit/Social/EditPencil-v1")).preserveAspect=true;
            button.onClick.AddListener(click);
            return button;
        }

        static Sprite SettingsIcon(int index)
        {
            if (settingsIcons == null || !settingsIcons[0])
            {
                var atlas = Resources.Load<Texture2D>("Moonlit/Social/SettingsIcons-v1");
                if (!atlas) return null;
                settingsIcons = new Sprite[10];
                float cellWidth = atlas.width / 5f, cellHeight = atlas.height / 2f;
                for (int i = 0; i < settingsIcons.Length; i++)
                {
                    settingsIcons[i] = Sprite.Create(atlas,
                        new Rect(i % 5 * cellWidth, (1 - i / 5) * cellHeight, cellWidth, cellHeight),
                        new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                    settingsIcons[i].name = "Settings symbol " + i;
                }
            }
            return settingsIcons[index];
        }

        static void SettingsRowArt(Transform root, float w, float y, int index)
        {
            var icon = Ui.Image("Settings icon " + index, root, 52, y + 6, 64, 64, SettingsIcon(index));
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Ui.Image("Rule", root, 36, y + 82, w - 72, 2, null,
                new Color(Ui.Gold.r, Ui.Gold.g, Ui.Gold.b, .45f)).raycastTarget = false;
        }

        static void BuildSettingsTab(ScreenContext c, Transform root, float w, float h)
        {
            string[] names = { "진동", "음악", "사운드 효과", "채팅 표시", "채팅 다크 모드", "클랜 채팅 미리보기" };
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                float y = i * 86;
                SettingsRowArt(root, w, y, i);
                Ui.Text("Setting " + names[i], root, 132, y, w - 310, 78, names[i], 27, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
                Toggle(c, root, w - 170, y + 13, names[i], settingValues[i], on => settingValues[index] = on);
            }
            string[] links = { "언어", "계정 (로컬 데모)", "차단 목록", "개인정보 보호" };
            for (int i = 0; i < links.Length; i++)
            {
                int index = i;
                float y = (6 + i) * 86;
                var hit = Ui.Image("Settings link " + i, root, 36, y, w - 72, 82, null, Color.clear);
                hit.raycastTarget = true;
                var button = hit.gameObject.AddComponent<Button>();
                button.targetGraphic = hit;
                button.onClick.AddListener(() => {
                    if(index<3)c.Open(new[]{"settings-language","settings-account","settings-blocked"}[index]);
                    else c.Toast("개인정보 보호 페이지는 데모에 연결되지 않았습니다.");
                });
                SettingsRowArt(root, w, y, 6 + i);
                Ui.Text("Setting " + links[i], root, 132, y, w - 310, 78, links[i], 27, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
            }
        }

        static Toggle Toggle(ScreenContext c, Transform parent, float x, float y, string name, bool value, UnityAction<bool> changed)
            => PopupSkin.Switch(parent, x, y, name, value, changed);

        static InputField Input(ScreenContext c, Transform parent, float x, float y, float w, float h, string initial)
        {
            var bg = Ui.Panel("Input", parent, x, y, w, h, Stone); bg.raycastTarget = true;
            var text = Ui.Text("Text", bg.transform, 14, 4, w - 28, h - 8, initial, 27, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
            text.raycastTarget = true;
            var input = bg.gameObject.AddComponent<InputField>(); input.targetGraphic = bg; input.textComponent = text; input.text = initial;
            return input;
        }

        static void BuildPlayerDetails(ScreenContext c)
        {
            var payload = c.Payload as Dictionary<string, object>;
            string player = Get(payload, "name", profileName);
            string power = Get(payload, "power", c.Main.powerText ? c.Main.powerText.text : "0");
            int rank = GetInt(payload, "rank", RewardRules.ArenaRank(RewardState.Current.arenaPoints));
            float w, h; var frame = Frame(c, "플레이어 정보", 1370, out w, out h);
            Avatar(c, frame, 52, 120, 146, GetInt(payload, "avatarIndex", profileAvatar));
            Ui.Text("Player", frame, 220, 116, w - 270, 48, player + (payload == null ? (profileFemale ? "  여성" : "  남성") : ""), 34, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
            Ui.Image("Power icon", frame, 220, 169, 36, 36, Icon(c, 12)).preserveAspect = true;
            Ui.Text("Power", frame, 266, 166, w - 316, 44, power + "     서버 순위 " + rank, 29, Font(c), Ui.Gold, TextAnchor.MiddleLeft);
            bool local=payload==null || player==profileName;
            var stats=ForgeState.Current.TotalStats;
            Ui.Text("Stats",frame,w-380,210,330,132,local ? "Lv."+ForgeState.Current.level+" 대장간\n공격력 "+MainScreen.Compact(System.Math.Max(10,stats.attack+CollectionProgression.OwnedAttack+CollectionProgression.EquippedAttack))+"\n체력 "+MainScreen.Compact(System.Math.Max(80,stats.health+CollectionProgression.OwnedHealth+CollectionProgression.EquippedHealth)) : "더미 상대",22,Font(c),Ui.Ivory,TextAnchor.UpperRight);
            var scene = Ui.Panel("Companion scene", frame, 52, 350, w - 104, 148, new Color(.015f,.16f,.22f));
            Ui.Text("Scene", scene.transform, 20, 12, w - 144, 124, "펫 "+CollectionProgression.Equipped(1).Count+"/3 · 탈것 "+CollectionProgression.Equipped(2).Count+"/1\n외형 준비 중", 27, Font(c), new Color(.35f,.85f,1));
            for (int i = 0; i < 9; i++)
            {
                float sw = (w - 136) / 5f;
                int row = i / 5, col = i % 5;
                var slot = Ui.Panel("Equipment slot " + i, frame, 52 + col * sw, 520 + row * 142, sw - 12, 126, new Color(.26f,.11f,.025f));
                var roll=local && i<6?ForgeState.Current.equipped[i]:null;
                var gearIcon=Ui.Image("Icon",slot.transform,13,9,sw-38,79,roll!=null?EquipmentArt.Icon(roll):null);
                gearIcon.preserveAspect=true;gearIcon.enabled=roll!=null;
                if(roll!=null)slot.color=EquipmentRules.TierColor(roll.tier);
                Ui.Text("Level",slot.transform,4,88,sw-20,34,i<6?(roll==null?EquipmentRules.PartNames[i]:"Lv."+roll.level):new[]{"엠블렘","날개","정령"}[i-6],20,Font(c));
            }
            var equippedSkills = CollectionProgression.EquippedSkills;
            for (int i = 0; i < 3; i++)
            {
                var entry = local && i < equippedSkills.Count ? equippedSkills[i] : null;
                Ui.Text("Equipped skill " + i, frame, 65 + i * 262, 798, 250, 72,
                    entry == null ? "스킬 " + (i + 1) + "\n비어 있음" : entry.Name + "\nLv." + entry.level,
                    22, Font(c), entry == null ? Ui.Ivory : EquipmentRules.TierColor(entry.grade));
            }
            Ui.Image("Stats rule", frame, 60, 872, w - 120, 2, null, Ui.Gold);
            string bonuses="";
            for(int i=0;i<EquipmentRules.AffixNames.Length;i++)bonuses+=EquipmentRules.AffixNames[i]+" +"+ForgeState.Current.AffixTotal((EquipmentAffixKind)i).ToString("0.##")+"%\n";
            Ui.Text("Bonuses",frame,92,900,w-184,310,local?bonuses:"더미 상대의 상세 옵션은 준비 중입니다.",23,Font(c),Ui.Ivory,TextAnchor.UpperLeft);
            Close(c, frame, w, h);
        }

        static string Get(Dictionary<string, object> d, string key, string fallback) => d != null && d.TryGetValue(key, out var v) && v != null ? v.ToString() : fallback;
        static int GetInt(Dictionary<string, object> d, string key, int fallback) => d != null && d.TryGetValue(key, out var v) && v is int n ? n : fallback;

        static ScrollRect Scroll(ScreenContext c, Transform parent, float x, float y, float w, float h, float contentHeight, out RectTransform content)
        {
            var viewport = Ui.Rect("Viewport", parent, x, y, w, h);
            var image = viewport.gameObject.AddComponent<Image>(); image.color = new Color(0,0,0,.12f); image.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Ui.Rect("Content", viewport, 0, 0, w, contentHeight);
            content.pivot = new Vector2(0, 1);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 45;
            return scroll;
        }

        static void BuildRanking(ScreenContext c)
        {
            float w, h; var frame = Frame(c, "파워 랭킹", 1310, out w, out h);
            Ui.Text("Subtitle", frame, 40, 92, w - 80, 44, "서버 5  ·  목록은 매시간 업데이트됨", 23, Font(c));
            string[] names = { "XrayDelta", "PureAwe", "KurtCobain", "McKennasTown", "Ty", "SerialX", "Quintessential", "prawnstar", "Marss", profileName };
            string[] powers = { "687t", "674t", "660t", "590t", "565t", "479t", "474t", "446t", "437t", "65.5b" };
            Scroll(c, frame, 40, 142, w - 80, h - 260, names.Length * 118, out var content);
            for (int i = 0; i < names.Length; i++) RankRow(c, content, i, names[i], powers[i], w - 80, () => { });
            Close(c, frame, w, h);
        }

        static void RankRow(ScreenContext c, Transform parent, int index, string name, string power, float width, UnityAction ignored)
        {
            var row = Ui.Panel("Rank " + (index + 1), parent, 0, index * 118, width, 108, index == 9 ? new Color(.02f,.25f,.45f) : Stone);
            Ui.Text("Rank number", row.transform, 12, 8, 90, 90, (index + 1).ToString(), 34, Font(c), index < 3 ? Ui.Gold : Ui.Ivory);
            Avatar(c, row.transform, 104, 9, 90, index);
            Ui.Text("Name", row.transform, 212, 10, width - 230, 42, name, 28, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
            Ui.Image("Power icon", row.transform, 212, 57, 34, 34, Icon(c, 12)).preserveAspect = true;
            Ui.Text("Power", row.transform, 254, 53, width - 272, 40, power, 26, Font(c), Ui.Gold, TextAnchor.MiddleLeft);
            var data = new Dictionary<string, object> { { "name", name }, { "power", power }, { "rank", index + 1 }, { "avatarIndex", index } };
            var button = row.gameObject.AddComponent<Button>(); button.targetGraphic = row; button.onClick.AddListener(() => c.Open("player-details", data));
            row.raycastTarget = true;
        }

        static void BuildChat(ScreenContext c)
        {
            float w=c.Width,h=c.Height;
            PopupSkin.FullViewportBackdrop(c,Resources.Load<Sprite>("Moonlit/Social/ProfileRuins-v1"),new Color(.65f,.75f,.85f,1));
            var root=Ui.Rect("Chat",c.Root,0,0,w,h);
            Ui.Text("Chat title",root,100,20,w-200,70,"채팅",38,Font(c));
            var channel=Ui.Rect("Chat channel 0",root,0,0,w,h);
            Scroll(c,channel,54,108,w-108,h-290,1,out var content);
            var scroll=content.GetComponentInParent<ScrollRect>();
            string[] messages={"원정 준비됐나요?","장비를 강화하고 갈게요!","다음은 유령 마을이에요.","이 장비 조합은 어때요?","준비가 끝나면 알려 주세요."};
            float next=18;int count=0;
            foreach(var message in messages)next=ChatMessage(c,content,count++,next,count%2==0?"moonzzanf":"Raven",message,w-108);
            content.sizeDelta=new Vector2(0,next+18);
            PopupSkin.Panel("Chat composer",root,0,h-174,w,174);
            var input=Input(c,root,136,h-146,w-296,82,"");input.characterLimit=80;
            input.placeholder=Ui.Text("Placeholder",input.transform,18,4,w-336,74,"메시지 보내기…",25,Font(c),new Color(.55f,.58f,.62f),TextAnchor.MiddleLeft);
            var frame=Ui.Image("Input frame",input.transform,0,0,w-296,82,PopupSkin.PanelArt);
            frame.type=Image.Type.Sliced;frame.fillCenter=false;frame.pixelsPerUnitMultiplier=12;
            Action(c,root,w-140,h-146,112,82,"전송",()=>{
                if(string.IsNullOrWhiteSpace(input.text))return;
                next=ChatMessage(c,content,count++,next,"[나] moonzzanf",input.text.Trim(),w-108);
                content.sizeDelta=new Vector2(0,next+18);input.text="";
                Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=0;
            });
            PopupSkin.Back("Chat back",root,24,h-154,96,Font(c),c.Close);
        }

        static float ChatMessage(ScreenContext c, Transform parent, int index, float y, string name, string message, float width)
        {
            var portrait=Ui.Rect("Chat portrait",parent,18,y,92,92);
            var portraitSprite=name.StartsWith("[나]") ? AvatarPortrait(profileAvatar)
                : Resources.Load<Sprite>("Moonlit/Social/"+(index%2==0?"ChatGoldKnight-v1":"ChatHornedKnight-v1"));
            var artwork=Ui.ArtImage("Avatar artwork",portrait,4,4,84,84,portraitSprite);
            artwork.preserveAspect=true;
            var rim=Ui.Image("Portrait rim",portrait,0,0,92,92,PopupSkin.PanelArt);
            rim.type=Image.Type.Sliced; rim.fillCenter=false; rim.pixelsPerUnitMultiplier=18;
            Ui.Text("Sender", parent, 130, y, width - 278, 45, name, 24, Font(c), Ui.Gold, TextAnchor.MiddleLeft);
            Ui.Text("Message time",parent,width-138,y,110,45,"14:"+(40+index%20).ToString("00"),22,Font(c),Ui.Ivory,TextAnchor.MiddleRight);
            var bubble = PopupSkin.Panel("Message bubble", parent, 126, y + 48, width - 154, 72);
            bubble.pixelsPerUnitMultiplier=14;
            var text=Ui.Text("Message", bubble.transform, 18, 3, width - 190, 66, message, 25, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
            text.supportRichText=false;
            float textHeight=Mathf.Max(66,Mathf.Ceil(text.preferredHeight));
            text.rectTransform.sizeDelta=new Vector2(width-190,textHeight);
            text.rectTransform.anchoredPosition=new Vector2(18,-8);
            bubble.rectTransform.sizeDelta=new Vector2(width-154,textHeight+16);
            return y+48+textHeight+16+18;
        }

        // Scenery fills the viewport; page controls remain clipped inside SafeArea above navigation.
        static RectTransform PageBackdrop(ScreenContext c, string name)
        {
            PopupSkin.FullViewportBackdrop(c);
            return Ui.Rect(name,c.Root,0,0,c.Width,c.Height);
        }

        static void BuildShop(ScreenContext c)
        {
            float w = c.Width, h = c.Height;
            var root = PageBackdrop(c, "Shop page");
            var title=PopupSkin.Panel("Shop title frame",root,(w-280)*.5f,34,280,88).rectTransform;
            Ui.Text("Shop title", title, 0, 0, 280, 88, "상점", 46, Font(c), Ui.Gold);
            PageWallet(c,root,"Gold wallet",54,44,0,c.Main ? (c.Main.gold/1000000f).ToString("0.00")+"m" : "1.59m");
            PageWallet(c,root,"Ruby wallet",w-324,44,1,c.Main ? c.Main.gems.ToString() : "21");
            const float shopContentTop = 180;
            Scroll(c, root, 38, shopContentTop, w - 76, Mathf.Max(360, h - shopContentTop - NavigationReserve), 1920, out var content);
            var special = PopupSkin.Panel("Daily specials header", content, 18, 0, w - 112, 110);
            Ui.Text("Daily specials title", special.transform, 24, 12, w - 160, 76, "오늘의 특가", 42, Font(c), Ui.Gold);
            Ui.Text("Daily specials hint", content, 40, 116, w - 156, 52, "버튼을 누르면 상품이 지급됩니다", 25, Font(c), Ui.Ivory);
            DailyDiamonds(c,content,w-112);
            Deal(c, content, 350, "자원 거래", "₩2,800", w - 76, 0);
            Deal(c, content, 630, "펫 거래", "₩9,500", w - 76, 1);
            Deal(c, content, 910, "던전 거래", "₩27,500", w - 76, 2);
            PopupSkin.Panel("Gem section frame",content,150,1190,w-376,70);
            Ui.Text("Gem title", content, 20, 1190, w - 116, 70, "다이아", 42, Font(c), Ui.Gold);
            int[] gems = { 600, 2200, 8000, 15000, 33000 };
            string[] prices = { "₩2,800", "₩9,500", "₩34,500", "₩60,000", "₩110,000" };
            for (int i = 0; i < gems.Length; i++)
            {
                int col = i % 3, row = i / 3;
                float cardW = (w - 124) / 3f;
                var card = PopupSkin.IllustratedCard("Gem offer " + gems[i], content, 20 + col * (cardW + 14), 1270 + row * 320, cardW, 300, ShopCardScenery, new Color(.65f,.75f,.85f));
                card.Find("Card rim").GetComponent<Image>().pixelsPerUnitMultiplier=18;
                Ui.Image("Ruby amount icon", card.transform, 20, 12, 48, 48, Icon(c, 1)).preserveAspect = true;
                Ui.Text("Amount", card.transform, 72, 8, cardW - 82, 54, gems[i].ToString(), 29, Font(c), new Color(1,.75f,.78f), TextAnchor.MiddleLeft);
                var ruby = Ui.ArtImage("Ruby artwork", card.transform, 10, 44, cardW - 20, 190, ShopIllustration(3 + i)); Ui.CenterAspect(ruby);
                string price = prices[i]; int amount=gems[i]; int lastPurchaseFrame=-1;
                Action(c, card.transform, 12, 222, cardW - 24, 66, price, () => {
                    if(lastPurchaseFrame==Time.frameCount || !c.Main || !card || !card.gameObject.activeInHierarchy)return;
                    lastPurchaseFrame=Time.frameCount;int before=c.Main.gems;c.Main.gems=RewardRules.Add(before,amount);
                    c.Main.Refresh();c.Main.SaveGame();
                    RewardVisuals.Absorb(c.Main,RewardVisuals.Kind.Diamond,c.Main.gems-before,ruby.rectTransform.TransformPoint(ruby.rectTransform.rect.center));
                });
            }
        }

        static void DailyDiamonds(ScreenContext c,Transform parent,float width)
        {
            var card=PopupSkin.Panel("Daily diamond offer",parent,18,170,width,140).rectTransform;
            Ui.Text("Daily diamond title",card,24,12,500,46,"매일 무료 다이아",30,Font(c),Ui.Gold,TextAnchor.MiddleLeft);
            var icon=Ui.Image("Daily diamond icon",card,28,71,46,46,Icon(c,1));icon.preserveAspect=true;
            Ui.Text("Daily diamond amount",card,86,64,220,58,"100",34,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            Button claim=null;GameObject claimDot=null;
            System.Action refresh=()=>{
                if(!claim)return;
                bool available=RewardState.Current.CanClaimDailyDiamonds;
                claim.interactable=available;if(claimDot)claimDot.SetActive(available);
                claim.GetComponentInChildren<Text>().text=available?"무료 받기":"수령 완료";
            };
            claim=Action(c,card,width-306,38,280,68,"무료 받기",()=>{
                if(!card || !card.gameObject.activeInHierarchy)return;
                RewardState.Current.ClaimDailyDiamonds(c.Main,icon.rectTransform.TransformPoint(icon.rectTransform.rect.center));
                refresh();
            });
            claim.name="Claim daily diamonds";
            claimDot=RewardNotificationDots.Create(claim.transform,252,-8,27,RewardNotificationDots.Circle(c.Main));
            card.gameObject.AddComponent<LiveUiRefresh>().RefreshView=refresh;refresh();
        }

        static void PageWallet(ScreenContext c, Transform root, string name, float x, float y, int icon, string value)
        {
            var wallet=SpritePanel(c,name,root,x,y,270,64,1,Color.white);
            Ui.Image("Currency icon",wallet.transform,-6,-12,76,76,Icon(c,icon)).preserveAspect=true;
            var amount=Ui.Text("Currency amount",wallet.transform,72,0,180,64,value,35,Font(c),Ui.Ivory);
            wallet.gameObject.AddComponent<LiveUiRefresh>().RefreshView=()=>{if(c.Main)amount.text=(icon==0?c.Main.gold:c.Main.gems).ToString("N0");};
            var add=Ui.ArtButton("Currency information",wallet.transform,45,30,44,44);
            Ui.Text("Add",add.transform,0,0,44,44,"+",35,Font(c),new Color(.15f,.9f,.07f));
            add.onClick.AddListener(()=>c.Open("wallet"));
        }

        static Sprite ShopCardScenery => Resources.Load<Sprite>("Moonlit/Social/ProfileRuins-v1");

        static void Deal(ScreenContext c, Transform parent, float y, string title, string price, float width, int artIndex)
        {
            var card = PopupSkin.IllustratedCard(title,parent,18,y,width-36,260,ShopCardScenery,new Color(.24f,.3f,.36f));
            card.Find("Card rim").GetComponent<Image>().pixelsPerUnitMultiplier=18;
            var artwork = Ui.ArtImage("Deal illustration", card.transform, width - 414, 8, 378, 240, ShopIllustration(artIndex));
            Ui.CenterAspect(artwork);
            artwork.raycastTarget = false;
            Ui.Image("Ribbon",card.transform,0,10,430,52,PopupSkin.RibbonArt,new Color(1,.22f,.16f));
            Ui.Text("Title", card.transform, 20, 2, 390, 56, title, 31, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
            var ticket=Resources.Load<Sprite>("Moonlit/Skills/SummonTicket-v1");
            Sprite[] icons = artIndex==0 ? new[]{Icon(c,0),RewardsScreenModule.HammerArt,ticket,RewardVisuals.Ticket(1),RewardVisuals.Ticket(2),Icon(c,1)}
                : artIndex==1 ? new[]{RewardVisuals.Ticket(1),RewardsScreenModule.HammerArt,Icon(c,1)}
                : new[]{PopupSkin.RewardIcon(5),PopupSkin.RewardIcon(2),PopupSkin.RewardIcon(4),PopupSkin.RewardIcon(3)};
            string[] values=artIndex==0 ? new[]{"골드 1,000","망치 50","스킬권 200","펫권 50","탈것권 50","다이아 62"}
                : artIndex==1 ? new[]{"펫권 660","망치 200","다이아 20"} : new[]{"망치 키 2","유령 키 2","침략 키 2","좀비 키 2"};
            for(int i=0;i<icons.Length;i++)
            {
                int col=artIndex==1?0:i%2, row=artIndex==1?i:i/2;
                var cell=PopupSkin.Panel("Reward cell "+i,card,32+col*222,76+row*52,212,48).rectTransform;
                Ui.Image("Reward icon",cell,8,7,34,34,icons[i]).preserveAspect=true;
                Ui.Text("Reward amount",cell,46,0,164,48,values[i],20,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            }
            int lastFrame=-1;
            Action(c, card.transform, width - 330, 180, 280, 68, price, () => {
                if(!c.Main || !card || !card.gameObject.activeInHierarchy || lastFrame==Time.frameCount)return;
                lastFrame=Time.frameCount;
                int[] before={c.Main.gold,c.Main.ore,c.Main.skillTickets,c.Main.petTickets,c.Main.mountTickets,c.Main.gems};
                if(artIndex==2)DungeonProgression.RefreshDay(DateTime.UtcNow);
                int[] keysBefore=(int[])DungeonProgression.Data.keys.Clone();
                if(artIndex==0) {
                    c.Main.gold=RewardRules.Add(c.Main.gold,1000);c.Main.ore=RewardRules.Add(c.Main.ore,50);
                    c.Main.skillTickets=RewardRules.Add(c.Main.skillTickets,200);c.Main.petTickets=RewardRules.Add(c.Main.petTickets,50);
                    c.Main.mountTickets=RewardRules.Add(c.Main.mountTickets,50);c.Main.gems=RewardRules.Add(c.Main.gems,62);
                } else if(artIndex==1) {
                    c.Main.petTickets=RewardRules.Add(c.Main.petTickets,660);c.Main.ore=RewardRules.Add(c.Main.ore,200);c.Main.gems=RewardRules.Add(c.Main.gems,20);
                } else {
                    // Keep every key quantity within the serializable integer wallet.
                    for(int i=0;i<4;i++)DungeonProgression.Data.keys[i]=RewardRules.Add(DungeonProgression.Data.keys[i],2);
                }
                c.Main.Refresh();c.Main.SaveGame();
                int[] after={c.Main.gold,c.Main.ore,c.Main.skillTickets,c.Main.petTickets,c.Main.mountTickets,c.Main.gems};
                var kinds=new[]{RewardVisuals.Kind.Gold,RewardVisuals.Kind.Hammer,RewardVisuals.Kind.SkillTicket,
                    RewardVisuals.Kind.PetTicket,RewardVisuals.Kind.MountTicket,RewardVisuals.Kind.Diamond};
                int[] rewardIndices=artIndex==0?new[]{0,1,2,3,4,5}:new[]{3,1,5};
                for(int i=0;i<icons.Length;i++){
                    var reward=(RectTransform)card.Find("Reward cell "+i);
                    Vector3 origin=reward.TransformPoint(reward.rect.center);
                    if(artIndex==2)RewardVisuals.Absorb(c.Main,(RewardVisuals.Kind)((int)RewardVisuals.Kind.HammerKey+i),DungeonProgression.Data.keys[i]-keysBefore[i],origin);
                    else {int index=rewardIndices[i];RewardVisuals.Absorb(c.Main,kinds[index],after[index]-before[index],origin);}
                }
            });
        }

        static Sprite[] shopIllustrations;
        static Sprite ShopIllustration(int index)
        {
            if (shopIllustrations == null || !shopIllustrations[0])
            {
                var atlas = Resources.Load<Texture2D>("Moonlit/Shop/ShopBundles-v1");
                if (!atlas) return null;
                shopIllustrations = new Sprite[8];
                // Crop transparent cell padding so each illustration fills its live card.
                var crops=new[]{new Rect(18,45,414,375),new Rect(36,15,390,411),new Rect(24,33,411,390),new Rect(24,138,411,270),
                    new Rect(15,75,411,303),new Rect(3,3,423,402),new Rect(9,0,423,402),new Rect(9,0,429,402)};
                float sx=atlas.width/1774f,sy=atlas.height/887f;
                for (int i = 0; i < shopIllustrations.Length; i++)
                {
                    var crop=crops[i]; float left=i%4*443.5f+crop.x,top=i/4*443.5f+crop.y;
                    shopIllustrations[i] = Sprite.Create(atlas, new Rect(left*sx,atlas.height-(top+crop.height)*sy,crop.width*sx,crop.height*sy),
                        new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                    shopIllustrations[i].name = "Shop illustration " + i;
                }
            }
            return shopIllustrations[Mathf.Clamp(index, 0, shopIllustrations.Length - 1)];
        }

        static void BuildPvp(ScreenContext c)
        {
            float w = c.Width, h = c.Height;
            var root = PageBackdrop(c, "PvP page");
            Ui.Image("Gold league crest", root, (w - 160) * .5f, 0, 160, 160,
                Resources.Load<Sprite>("Moonlit/Social/GoldLeagueCrest-v1")).preserveAspect = true;
            Ui.Text("League", root, 290, 158, 500, 54, "아레나", 43, Font(c), Ui.Ivory);
            var rewards = Ui.ArtButton("Season rewards", root, (w - 560) * .5f, 218, 560, 64, PanelSprite(c, 1), true, 5);
            Ui.Image("Season gift", rewards.transform, 22, 0, 64, 64,
                Resources.Load<Sprite>("Moonlit/Social/SeasonGift-v1")).preserveAspect = true;
            Ui.Text("Season timer", rewards.transform, 100, 0, 440, 64,
                "도전할 때마다 보상 · "+RewardRules.TierName(RewardState.Current.arenaPoints), 26, Font(c));
            rewards.onClick.AddListener(() => c.Open("pvp-rewards"));
            string[] names = { "tewtee", "CreeGuy", "MenoT", profileName, "Guest 86680", "mrmaingo1868", "Epsylon" };
            string[] powers = { "212m", "12.9m", "16b", "65.5b", "5.52m", "2.57m", "821b" };
            float actionY = h - NavigationReserve - 112;
            float stickyY = actionY - 178;
            float listHeight = Mathf.Max(380, stickyY - 310 - 18);
            // Local standings reflect the same points and rank used by challenge rewards.
            Scroll(c, root, 90, 290, w - 180, listHeight, 100 * 176, out var content);
            for (int rank = 1; rank <= 100; rank++)
            {
                int myRank=RewardRules.ArenaRank(RewardState.Current.arenaPoints);
                bool own=rank==myRank;int dummyIndex=rank-1-(rank>myRank?1:0);
                int points=own?RewardState.Current.arenaPoints:4900-dummyIndex*50;
                PvpRow(c,content,"PvP rank "+rank,0,(rank-1)*176,w-180,rank,
                    own?profileName:"도전자 "+(dummyIndex+1).ToString("000"),
                    own?(c.Main.powerText?c.Main.powerText.text:"0"):(100-dummyIndex).ToString(),
                    points,own?profileAvatar:dummyIndex%20,own);
            }
            PvpRow(c, root, "My sticky rank", 90, stickyY, w - 180,
                RewardRules.ArenaRank(RewardState.Current.arenaPoints), names[3], (c.Main.powerText?c.Main.powerText.text:"0"), RewardState.Current.arenaPoints, profileAvatar, true);
            var challenge=Action(c, root, 330, actionY, 420, 90, "도전", () => c.Open("pvp-opponents"));
            var challengeDot=RewardNotificationDots.Create(challenge.transform,392,-8,27,RewardNotificationDots.Circle(c.Main));
            void RefreshChallenge() {
                int remaining=RewardState.Current.ArenaRemaining(DateTime.UtcNow);
                challenge.interactable=remaining>0;challengeDot.SetActive(remaining>0);
                challenge.GetComponentInChildren<Text>().text="도전 "+remaining+"/5";
            }
            root.gameObject.AddComponent<LiveUiRefresh>().RefreshView=RefreshChallenge;RefreshChallenge();
            PopupSkin.Back("Return to main",root,40,actionY,88,Font(c),c.Close);
        }

        static void PvpRow(ScreenContext c, Transform parent, string objectName, float x, float y, float width,
            int rank, string player, string power, int stars, int avatarIndex, bool selected)
        {
            var row = SpritePanel(c, objectName, parent, x, y, width, 164, selected ? 0 : 1, Color.white);
            Ui.Text("Rank", row.transform, 10, 35, 90, 90, rank.ToString(), 35, Font(c));
            Avatar(c, row.transform, 105, 10, 96, avatarIndex).rectTransform.localScale=Vector3.one*1.5f;
            Ui.Text("Player", row.transform, 268, 30, width - 488, 48, player, 29, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
            Ui.Image("Power icon", row.transform, 268, 85, 32, 32, Icon(c, 12)).preserveAspect = true;
            Ui.Text("Power", row.transform, 308, 80, 310, 44, power, 25, Font(c), Ui.Gold, TextAnchor.MiddleLeft);
            Ui.Image("Star icon", row.transform, width - 210, 43, 38, 38, Icon(c, 15)).preserveAspect = true;
            Ui.Text("Stars", row.transform, width - 165, 35, 145, 54, stars.ToString(), 29, Font(c), Ui.Gold);
            Ui.Text("Server", row.transform, width - 216, 101, 190, 28, "서버 5", 21, Font(c),
                new Color(.7f, .72f, .74f), TextAnchor.MiddleRight);
            var payload = new Dictionary<string, object> { { "name", player }, { "power", power },
                { "rank", rank }, { "avatarIndex", avatarIndex } };
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row; row.raycastTarget = true;
            button.onClick.AddListener(() => c.Open("player-details", payload));
        }

        static void BuildOpponents(ScreenContext c)
        {
            float w, h; var frame = Frame(c, "상대 선택", 1260, out w, out h);
            var remainingNote=Ui.Text("Ticket note", frame, 50, 94, w - 100, 70, "", 24, Font(c));
            var challengeButtons=new List<Button>();var challengeDots=new List<GameObject>();
            string[] names = { "tewtee", "CreeGuy", "MenoT", "Guest 86680", "mrmaingo1868" };
            string[] powers = { "212m", "12.9m", "16b", "5.52m", "2.57m" };
            Scroll(c, frame, 42, 180, w - 84, h - 310, names.Length * 218, out var content);
            for (int i = 0; i < names.Length; i++)
            {
                int index = i; var row = Ui.Panel("Opponent " + names[i], content, 0, i * 218, w - 84, 204, Stone);
                Avatar(c, row.transform, 18, 18, 112, i).rectTransform.localScale=Vector3.one*1.5f;
                var payload = new Dictionary<string, object> { { "name", names[i] }, { "power", powers[i] }, { "rank", 8 + i }, { "avatarIndex", i } };
                var avatarButton = row.gameObject.AddComponent<Button>(); avatarButton.targetGraphic = row; avatarButton.onClick.AddListener(() => c.Open("player-details", payload));
                row.raycastTarget = true;
                Ui.Text("Name", row.transform, 210, 44, 274, 44, names[i], 29, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
                Ui.Image("Power icon", row.transform, 210, 98, 32, 32, Icon(c, 12)).preserveAspect = true;
                Ui.Text("Power", row.transform, 250, 94, 234, 40, powers[i], 25, Font(c), Green, TextAnchor.MiddleLeft);
                var challenge=Action(c, row.transform, w - 350, 62, 230, 80, "도전", () => {
                    if(!c.Main)return;
                    c.Main.StartArena(Mathf.Max(1,c.Main.stage+index-2),won=>RewardRules.FinishArena(c.Main,won));
                });
                challengeButtons.Add(challenge);
                challengeDots.Add(RewardNotificationDots.Create(challenge.transform,203,-8,27,RewardNotificationDots.Circle(c.Main)));
                Ui.Image("Reward star", row.transform, w - 340, 29, 30, 30, Icon(c, 15)).preserveAspect = true;
                Ui.Text("Reward", row.transform, w - 305, 30, 175, 34, "+25 승점", 23, Font(c), Ui.Gold);
            }
            void RefreshChallenges() {
                int remaining=RewardState.Current.ArenaRemaining(DateTime.UtcNow);
                remainingNote.text="오늘 남은 도전 "+remaining+"/5\n승리 +25 승점 / 패배 -10 승점";
                for(int i=0;i<challengeButtons.Count;i++){challengeButtons[i].interactable=remaining>0;challengeDots[i].SetActive(remaining>0);}
            }
            frame.gameObject.AddComponent<LiveUiRefresh>().RefreshView=RefreshChallenges;RefreshChallenges();
            Close(c, frame, w, h);
        }

        static void BuildRewards(ScreenContext c)
        {
            float w,h; var frame=Frame(c,"아레나 도전 보상",1420,out w,out h);
            int points=RewardState.Current.arenaPoints, rank=RewardRules.ArenaRank(points);
            Ui.Text("Explanation",frame,60,108,w-120,98,"매 도전 전투가 끝나면 보상을 받습니다.\n1~5등은 티어 보상을 대신하여 순위 보상을 받습니다.",25,Font(c));
            Ui.Text("Current rewards",frame,60,210,w-120,110,RewardRules.TierName(points)+" · 승점 "+points+" · "+rank+"등\n"+RewardRules.ArenaRewardText(points,rank),27,Font(c),Ui.Gold);
            Scroll(c,frame,40,348,w-80,h-455,50*124,out var content);
            for(int i=0;i<50;i++)
            {
                bool podium=i<5;int tier=i-5;
                var row=PopupSkin.Panel("Arena reward "+i,content,0,i*124,w-80,114).rectTransform;
                string label=podium?(i+1)+"등":RewardRules.TierLabel(tier);
                int factor=podium?50-i:tier+1;
                Ui.Text("Tier",row,18,6,260,100,label+(podium?"":"\n승점 "+RewardRules.TierRange(tier)),24,Font(c),Ui.Gold);
                Ui.Text("Rewards",row,290,10,w-400,94,"망치 "+factor*10+" · 다이아 "+factor*5+"\n골드 "+factor*100,25,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            }
            Close(c,frame,w,h);
        }
    }

}
