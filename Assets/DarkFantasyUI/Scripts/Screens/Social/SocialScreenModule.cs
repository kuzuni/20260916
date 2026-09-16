using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Moonlit.UI
{
    /// <summary>Runtime-only implementation of the nine social, PvP and shop references.</summary>
    public static class SocialScreenModule
    {
        static readonly Color Ink = new Color(.025f, .055f, .075f, .97f);
        static readonly Color Stone = new Color(.055f, .085f, .105f, .98f);
        static readonly Color Blue = new Color(.02f, .23f, .48f, 1f);
        static readonly Color Red = new Color(.42f, .035f, .045f, 1f);
        static readonly Color Green = new Color(.25f, 1f, .28f, 1f);

        public static void Register(UiScreenRegistry registry)
        {
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
            var frame = Ui.Panel(title + " frame", c.Root, x, y, width, height, Ink);
            Ui.Border(frame.transform, width, height, new Color(.42f, .29f, .14f), 8);
            Ui.Border(frame.transform, width, height, Ui.Gold, 2);
            Ui.Text("Title", frame.transform, 70, 18, width - 140, 70, title, 44, Font(c), Ui.Ivory);
            Ui.Image("Title rule", frame.transform, 28, 94, width - 56, 3, null, Ui.Gold);
            return frame.rectTransform;
        }

        static Button Close(ScreenContext c, Transform parent, float width, float height)
        {
            var button = Ui.Button("Close", parent, width * .5f - 42, height - 86, 84, 84, "×", Font(c), c.Close, Red, 56);
            button.gameObject.AddComponent<SocialRoundMask>();
            return button;
        }

        static Button Action(ScreenContext c, Transform parent, float x, float y, float w, float h, string label, UnityAction action)
            => Ui.Button(label, parent, x, y, w, h, label, Font(c), action, Blue, 28);

        static Image Avatar(ScreenContext c, Transform parent, float x, float y, float size, int index)
        {
            var bg = Ui.Panel("Avatar " + index, parent, x, y, size, size, new Color(.08f, .11f, .13f));
            var art = Ui.Image("Avatar artwork", bg.transform, 8, 8, size - 16, size - 16, Icon(c, index));
            art.preserveAspect = true;
            return bg;
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
            void Select(bool showSettings)
            {
                profile.gameObject.SetActive(!showSettings);
                settings.gameObject.SetActive(showSettings);
            }
            Action(c, frame, 120, h - 156, (w - 240) * .5f, 64, "프로필", () => Select(false));
            Action(c, frame, w * .5f, h - 156, (w - 240) * .5f, 64, "설정", () => Select(true));
            Close(c, frame, w, h);
            Select(settingsFirst);
        }

        static void BuildProfileTab(ScreenContext c, Transform root, float w, float h)
        {
            Avatar(c, root, 52, 38, 190, 0);
            Ui.Text("Name label", root, 275, 32, 170, 48, "이름:", 27, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
            var name = Input(c, root, 275, 80, w - 320, 64, "moonzzanf");
            Ui.Text("Gender label", root, 275, 156, 170, 44, "성별:", 27, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
            var gender = Input(c, root, 275, 202, w - 320, 64, "♂");
            Action(c, root, 52, 240, 190, 54, "아바타 변경", () => c.Toast("아바타가 로컬 미리보기에서 변경되었습니다."));
            Action(c, root, w - 150, 80, 110, 64, "저장", () => c.Toast("프로필 미리보기를 저장했습니다: " + name.text));
            Action(c, root, w - 150, 202, 110, 64, "변경", () => { gender.text = gender.text == "♂" ? "♀" : "♂"; });
            Ui.Image("Divider", root, 55, 330, w - 110, 3, null, Ui.Gold);
            Ui.Text("Server rank", root, 55, 354, w - 110, 62, "서버 5 순위", 34, Font(c));
            Action(c, root, 105, 430, 300, 82, "파워 랭킹", () => c.Open("power-ranking"));
            Action(c, root, w - 405, 430, 300, 82, "클랜 랭킹", () => c.Toast("클랜 랭킹은 데모에 연결되지 않았습니다."));
            Ui.Text("Profile note", root, 80, 555, w - 160, 100, "프로필 변경은 이 기기의 데모 상태에만 적용됩니다.", 24, Font(c), new Color(.72f,.75f,.78f));
        }

        static void BuildSettingsTab(ScreenContext c, Transform root, float w, float h)
        {
            string[] names = { "진동", "음악", "사운드 효과", "채팅 표시", "채팅 다크 모드", "클랜 채팅 미리보기" };
            for (int i = 0; i < names.Length; i++)
            {
                float y = i * 86;
                Ui.Text("Setting " + names[i], root, 52, y, w - 230, 78, names[i], 27, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
                Toggle(c, root, w - 170, y + 13, names[i], i != 4);
                Ui.Image("Rule", root, 36, y + 82, w - 72, 2, null, new Color(Ui.Gold.r, Ui.Gold.g, Ui.Gold.b, .45f));
            }
            string[] links = { "언어", "계정 (로컬 데모)", "차단 목록", "개인정보 보호" };
            for (int i = 0; i < links.Length; i++)
            {
                int index = i;
                Action(c, root, 42, 532 + i * 72, w - 84, 58, links[i], () => c.Toast(links[index] + " 화면은 연결되지 않은 데모입니다."));
            }
        }

        static Toggle Toggle(ScreenContext c, Transform parent, float x, float y, string name, bool value)
        {
            var bg = Ui.Panel(name + " toggle", parent, x, y, 128, 54, new Color(.03f,.05f,.06f));
            bg.raycastTarget = true;
            var check = Ui.Image("On", bg.transform, value ? 70 : 6, 6, 52, 42, null, value ? new Color(0,.65f,1) : new Color(.25f,.28f,.3f));
            check.raycastTarget = true;
            var toggle = bg.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = bg; toggle.graphic = check; toggle.isOn = value;
            toggle.onValueChanged.AddListener(on => { check.rectTransform.anchoredPosition = new Vector2(on ? 70 : 6, -6); check.color = on ? new Color(0,.65f,1) : new Color(.25f,.28f,.3f); });
            return toggle;
        }

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
            string player = Get(payload, "name", "moonzzanf");
            string power = Get(payload, "power", "65.5b");
            int rank = GetInt(payload, "rank", 11);
            float w, h; var frame = Frame(c, "플레이어 정보", 1370, out w, out h);
            Avatar(c, frame, 52, 120, 146, GetInt(payload, "avatarIndex", 0));
            Ui.Text("Player", frame, 220, 116, w - 270, 48, player + "  ♂", 34, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
            Ui.Text("Power", frame, 220, 166, w - 270, 44, "⚔ " + power + "     서버 순위 " + rank, 29, Font(c), Ui.Gold, TextAnchor.MiddleLeft);
            Ui.Text("Stats", frame, w - 310, 210, 260, 100, "Lv. 33 대장간\n1.46b 총 피해\n15.5b 총 체력", 22, Font(c), Ui.Ivory, TextAnchor.UpperRight);
            var scene = Ui.Panel("Companion scene", frame, 52, 288, w - 104, 210, new Color(.015f,.16f,.22f));
            Ui.Text("Scene", scene.transform, 20, 20, w - 144, 170, "☾  전투 동료 편성  ⚔  ✦", 42, Font(c), new Color(.35f,.85f,1));
            for (int i = 0; i < 9; i++)
            {
                float sw = (w - 136) / 5f;
                int row = i / 5, col = i % 5;
                var slot = Ui.Panel("Equipment slot " + i, frame, 52 + col * sw, 520 + row * 142, sw - 12, 126, new Color(.26f,.11f,.025f));
                Ui.Image("Icon", slot.transform, 13, 9, sw - 38, 79, Icon(c, i)).preserveAspect = true;
                Ui.Text("Level", slot.transform, 4, 88, sw - 20, 34, "Lv." + (108 - i), 20, Font(c));
            }
            Ui.Text("Skills", frame, 50, 814, w - 100, 48, "⚡ Lv.20   ◈ Lv.17   ✹ Lv.19   ☽ Lv.78   ✦ Lv.3", 27, Font(c), Ui.Gold);
            Ui.Image("Stats rule", frame, 60, 872, w - 120, 2, null, Ui.Gold);
            Ui.Text("Bonuses", frame, 92, 900, w - 184, 250, "+38.9% 치명타 확률  (상한 80%)\n+169% 치명타 피해\n+7.27% 블록 확률\n+1% 체력 재생\n+6% 생명력 흡수\n+39.6% 더블 찬스\n+45.2% 근접 피해", 25, Font(c), Ui.Ivory, TextAnchor.UpperLeft);
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
            string[] names = { "XrayDelta", "PureAwe", "KurtCobain", "McKennasTown", "Ty", "SerialX", "Quintessential", "prawnstar", "Marss", "moonzzanf" };
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
            Ui.Text("Power", row.transform, 212, 53, width - 230, 40, "⚔ " + power, 26, Font(c), Ui.Gold, TextAnchor.MiddleLeft);
            var data = new Dictionary<string, object> { { "name", name }, { "power", power }, { "rank", index + 1 }, { "avatarIndex", index } };
            var button = row.gameObject.AddComponent<Button>(); button.targetGraphic = row; button.onClick.AddListener(() => c.Open("player-details", data));
        }

        static void BuildChat(ScreenContext c)
        {
            float w = c.Width, h = c.Height;
            var root = Ui.Panel("Chat", c.Root, 0, 0, w, h, new Color(.015f,.04f,.055f,.96f)).rectTransform;
            var messages = new[] { "원정 준비됐나요?", "오늘 보스는 화염 저항이 높아요.", "장비를 강화하고 갈게요!", "좋아요, 5분 뒤 출발합니다.", "새로운 길드원이 참가했습니다.", "전투 기록은 로컬 데모입니다." };
            Scroll(c, root, 36, 120, w - 72, h - 310, messages.Length * 154 + 40, out var content);
            for (int i = 0; i < messages.Length; i++) ChatMessage(c, content, i, i % 2 == 0 ? "[달빛] moonzzanf" : "[기사단] Raven", messages[i], w - 72);
            string[] tabs = { "월드", "클랜", "클랜 간부" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int tab = i; Action(c, root, i * w / 3f, 20, w / 3f, 80, tabs[i], () => c.Toast(tabs[tab] + " 채팅을 선택했습니다."));
            }
            var input = Input(c, root, 120, h - 160, w - 280, 76, "");
            input.placeholder = Ui.Text("Placeholder", input.transform, 14, 4, w - 320, 68, "메시지 보내기…", 25, Font(c), new Color(.55f,.58f,.62f), TextAnchor.MiddleLeft);
            Action(c, root, w - 148, h - 160, 112, 76, "전송", () => { if (string.IsNullOrWhiteSpace(input.text)) c.Toast("메시지를 입력하세요."); else { c.Toast("로컬 미리보기에만 표시되었습니다."); input.text = ""; } });
            Action(c, root, 24, h - 160, 76, 76, "‹", c.Close);
            Ui.Text("Offline notice", root, 120, h - 78, w - 160, 40, "실시간 서버에 연결되지 않은 로컬 채팅 데모", 19, Font(c), new Color(.65f,.68f,.7f));
        }

        static void ChatMessage(ScreenContext c, Transform parent, int index, string name, string message, float width)
        {
            float y = 18 + index * 154;
            Avatar(c, parent, 18, y, 92, index);
            Ui.Text("Sender", parent, 130, y, width - 150, 45, name + "                       " + (14 + index) + ":4" + index, 24, Font(c), Ui.Gold, TextAnchor.MiddleLeft);
            var bubble = Ui.Panel("Message bubble", parent, 126, y + 48, width - 154, 72, Stone);
            Ui.Text("Message", bubble.transform, 18, 3, width - 190, 66, message, 25, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
        }

        static void BuildShop(ScreenContext c)
        {
            float w = c.Width, h = c.Height;
            var root = Ui.Panel("Shop page", c.Root, 0, 0, w, h, new Color(.015f,.045f,.06f,.97f)).rectTransform;
            Ui.Text("Shop title", root, 320, 24, 440, 80, "상점", 46, Font(c), Ui.Gold);
            Ui.Text("Wallet", root, 30, 30, 260, 60, "♛ 1.59m", 29, Font(c), Ui.Ivory);
            Ui.Text("Gems", root, w - 280, 30, 250, 60, "♦ 21", 29, Font(c), new Color(1,.16f,.35f));
            Scroll(c, root, 38, 120, w - 76, h - 145, 1830, out var content);
            Deal(c, content, 0, "자원 거래", "♛ 1k     ◈ 150\n🎟 200     ▣ 50\n⚗ 50      ⚿ 62", "₩2,800", w - 76);
            Deal(c, content, 300, "펫 거래", "◉ 660\n▣ 200\n◇ 20", "₩9,500", w - 76);
            Deal(c, content, 600, "던전 거래", "⚿ 2     🔑 2\n🔑 2     ⚿ 250", "₩27,500", w - 76);
            Ui.Text("Gem title", content, 20, 900, w - 116, 70, "보석", 42, Font(c), Ui.Gold);
            int[] gems = { 60, 220, 800, 1500, 3300 };
            string[] prices = { "₩2,800", "₩9,500", "₩34,500", "가격 미설정", "가격 미설정" };
            for (int i = 0; i < gems.Length; i++)
            {
                int col = i % 3, row = i / 3;
                float cardW = (w - 124) / 3f;
                var card = Ui.Panel("Gem offer " + gems[i], content, 20 + col * (cardW + 14), 980 + row * 360, cardW, 330, Stone);
                Ui.Text("Amount", card.transform, 8, 8, cardW - 16, 54, "♦ " + gems[i], 29, Font(c), new Color(1,.2f,.4f));
                Ui.Text("Gem art", card.transform, 8, 64, cardW - 16, 150, "♦\n♦ ♦", 46, Font(c), new Color(1,.05f,.3f));
                string price = prices[i];
                Action(c, card.transform, 12, 242, cardW - 24, 70, price, () => c.Toast(price == "가격 미설정" ? "이 상품은 가격이 구성되지 않았습니다." : "결제는 연결되지 않은 미리보기입니다."));
            }
        }

        static void Deal(ScreenContext c, Transform parent, float y, string title, string body, string price, float width)
        {
            var card = Ui.Panel(title, parent, 18, y, width - 36, 280, Stone);
            Ui.Image("Ribbon", card.transform, 0, 0, 430, 62, null, Red);
            Ui.Text("Title", card.transform, 20, 2, 390, 56, title, 31, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
            Ui.Text("Contents", card.transform, 38, 74, width - 390, 174, body, 27, Font(c), Ui.Ivory, TextAnchor.UpperLeft);
            Action(c, card.transform, width - 330, 182, 260, 72, price, () => c.Toast("결제 기능은 연결되지 않았습니다."));
        }

        static void BuildPvp(ScreenContext c)
        {
            float w = c.Width, h = c.Height;
            var root = Ui.Panel("PvP page", c.Root, 0, 0, w, h, new Color(.015f,.045f,.06f,.97f)).rectTransform;
            Ui.Text("Crest", root, 380, 24, 320, 110, "🛡", 70, Font(c), Ui.Gold);
            Ui.Text("League", root, 290, 124, 500, 70, "골드 리그", 43, Font(c), Ui.Ivory);
            Action(c, root, 320, 200, 440, 64, "🎁 시즌 종료: 4일 18시", () => c.Open("pvp-rewards"));
            string[] names = { "tewtee", "CreeGuy", "MenoT", "moonzzanf", "Guest 86680", "mrmaingo1868", "Epsylon" };
            Scroll(c, root, 90, 290, w - 180, h - 610, names.Length * 130, out var content);
            for (int i = 0; i < names.Length; i++)
            {
                int rank = 8 + i; string name = names[i]; string power = i == 3 ? "65.5b" : new[] { "212m", "12.9m", "16b", "65.5b", "5.52m", "2.57m", "821b" }[i];
                var row = Ui.Panel("PvP rank " + rank, content, 0, i * 130, w - 180, 118, i == 3 ? new Color(.02f,.25f,.48f) : Stone);
                Ui.Text("Rank", row.transform, 10, 12, 90, 90, rank.ToString(), 35, Font(c)); Avatar(c, row.transform, 105, 10, 96, i);
                Ui.Text("Player", row.transform, 220, 7, 350, 48, name, 29, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
                Ui.Text("Power", row.transform, 220, 57, 350, 44, "⚔ " + power, 25, Font(c), Ui.Gold, TextAnchor.MiddleLeft);
                var payload = new Dictionary<string, object> { { "name", name }, { "power", power }, { "rank", rank }, { "avatarIndex", i } };
                var b = row.gameObject.AddComponent<Button>(); b.targetGraphic = row; b.onClick.AddListener(() => c.Open("player-details", payload));
                Ui.Text("Stars", row.transform, w - 410, 20, 200, 70, "★ " + (15 - i), 29, Font(c), Ui.Gold);
            }
            var sticky = Ui.Panel("My sticky rank", root, 90, h - 300, w - 180, 112, new Color(.02f,.28f,.52f));
            Ui.Text("Me", sticky.transform, 20, 8, w - 220, 96, "11    moonzzanf    ⚔ 65.5b              ★ 11", 28, Font(c));
            Action(c, root, 330, h - 170, 420, 90, "도전", () => c.Open("pvp-opponents"));
        }

        static void BuildOpponents(ScreenContext c)
        {
            float w, h; var frame = Frame(c, "상대 선택", 1260, out w, out h);
            Ui.Text("Ticket note", frame, 50, 94, w - 100, 70, "도전 티켓은 매일 09:00에 보충됩니다!\n🎟 5/5", 24, Font(c));
            string[] names = { "tewtee", "CreeGuy", "MenoT", "Guest 86680", "mrmaingo1868" };
            string[] powers = { "212m", "12.9m", "16b", "5.52m", "2.57m" };
            Scroll(c, frame, 42, 180, w - 84, h - 310, names.Length * 164, out var content);
            for (int i = 0; i < names.Length; i++)
            {
                int index = i; var row = Ui.Panel("Opponent " + names[i], content, 0, i * 164, w - 84, 150, Stone);
                Avatar(c, row.transform, 18, 18, 112, i);
                var payload = new Dictionary<string, object> { { "name", names[i] }, { "power", powers[i] }, { "rank", 8 + i }, { "avatarIndex", i } };
                var avatarButton = row.gameObject.AddComponent<Button>(); avatarButton.targetGraphic = row; avatarButton.onClick.AddListener(() => c.Open("player-details", payload));
                Ui.Text("Name", row.transform, 154, 18, 330, 44, names[i], 29, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
                Ui.Text("Power", row.transform, 154, 68, 330, 40, "⚔ " + powers[i], 25, Font(c), Green, TextAnchor.MiddleLeft);
                Action(c, row.transform, w - 350, 36, 230, 80, "도전 🎟 1", () => c.Toast("로컬 데모: " + names[index] + " 도전을 선택했습니다."));
                Ui.Text("Reward", row.transform, w - 340, 4, 210, 34, "★ +" + (5 - i), 23, Font(c), Ui.Gold);
            }
            Close(c, frame, w, h);
        }

        static void BuildRewards(ScreenContext c)
        {
            float w, h; var frame = Frame(c, "골드 리그 보상", 1320, out w, out h);
            Ui.Text("Explanation", frame, 65, 105, w - 130, 90, "현재 순위(11)를 유지하면 시즌 종료 시 다음 보상을 받을 수 있습니다.", 26, Font(c));
            Ui.Text("Current rewards", frame, 74, 210, w - 148, 145, "▣ 560        ♛ 28k        🎟 672\n◉ 186        ⚗ 560        ⚿ 280", 28, Font(c), Ui.Ivory);
            Ui.Text("Timer", frame, 290, 365, w - 580, 80, "수집까지: 4일 17시", 26, Font(c), Green);
            string[] tiers = { "🥇  1", "🥈  2", "🥉  3", "4–5" };
            string[] rewards = { "▣ 910   ♛ 45.5k   🎟 1.09k\n◉ 302   ⚗ 910   ⚿ 455", "▣ 840   ♛ 42k   🎟 1k\n◉ 279   ⚗ 840   ⚿ 420", "▣ 770   ♛ 38.5k   🎟 924\n◉ 256   ⚗ 770   ⚿ 385", "▣ 700   ♛ 35k   🎟 840\n◉ 232   ⚗ 700   ⚿ 350" };
            Scroll(c, frame, 52, 465, w - 104, h - 590, tiers.Length * 188, out var content);
            for (int i = 0; i < tiers.Length; i++)
            {
                var row = Ui.Panel("Reward tier " + tiers[i], content, 0, i * 188, w - 104, 174, Stone);
                Ui.Text("Tier", row.transform, 14, 14, 170, 142, tiers[i], 34, Font(c), Ui.Gold);
                Ui.Text("Rewards", row.transform, 190, 18, w - 320, 134, rewards[i], 24, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
            }
            Close(c, frame, w, h);
        }
    }

    /// <summary>Keeps the close control's hit target rectangular while presenting a round visual.</summary>
    sealed class SocialRoundMask : MonoBehaviour { }
}
