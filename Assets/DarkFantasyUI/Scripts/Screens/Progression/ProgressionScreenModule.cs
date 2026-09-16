using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    /// <summary>Runtime uGUI implementation of reference screens 04, 14-17, 19 and 20.</summary>
    public static class ProgressionScreenModule
    {
        static readonly Color Ink = new Color(.025f, .055f, .075f, .97f);
        static readonly Color Stone = new Color(.07f, .085f, .095f, .98f);
        static readonly Color Blue = new Color(.02f, .25f, .48f, 1f);
        static readonly Color Red = new Color(.38f, .025f, .018f, 1f);
        static readonly Color Green = new Color(.05f, .55f, .28f, 1f);
        static readonly Color Gold = new Color(.88f, .63f, .18f, 1f);
        static readonly Color[] Rarity = { new Color(.22f,.27f,.31f), new Color(.03f,.30f,.58f), new Color(.02f,.45f,.23f), new Color(.55f,.32f,.02f), new Color(.55f,.02f,.06f), new Color(.35f,.04f,.55f) };

        static readonly SkillData[] Skills = {
            new SkillData("핏빛 파편", 76, 3, 0, "+10.3m 기본 피해"), new SkillData("쌍날 투척", 74, 0, 1, "+9.8m 기본 피해"),
            new SkillData("화염 폭풍", 72, 6, 2, "+12.1m 기본 피해"), new SkillData("밤의 사역마", 100, 8, 3, "+82.8m 기본 체력"),
            new SkillData("지옥의 문장", 99, 7, 4, "+11.4m 기본 피해"), new SkillData("서리 용", 94, 4, 5, "+71.2m 기본 체력"),
            new SkillData("저자세 가시", 83, 5, 6, "+85.3k 기본 피해 +682k 기본 체력"), new SkillData("망자의 행진", 86, 2, 7, "+56.8m 기본 체력"),
            new SkillData("별빛 심판", 45, 4, 8, "+4.2m 기본 피해"), new SkillData("유성", 43, 6, 2, "+8.4m 기본 피해"),
            new SkillData("심연 폭탄", 44, 3, 1, "+7.1m 기본 피해"), new SkillData("봉인된 권능", 20, 0, 4, "+3.1m 기본 피해")
        };
        static readonly DungeonData[] Dungeons = {
            new DungeonData("망치 도둑", "잿빛 대장간", "19-9", "강화석 346", 0),
            new DungeonData("유령 마을", "달빛 묘지", "18-5", "영혼석 280", 5),
            new DungeonData("침략", "무너진 성문", "17-3", "금화 12.5k", 1),
            new DungeonData("좀비 러시", "죽은 자의 정원", "19-9", "생명 물약 346", 7)
        };
        static int summonCurrency = 6830;
        static int selectedDungeon;

        public static void Register(UiScreenRegistry registry)
        {
            registry.Register("skills-pets-heroes", ScreenPresentation.Page, BuildCollection, false);
            registry.Register("skill-details", ScreenPresentation.Modal, BuildSkillDetails, false);
            registry.Register("summon-probability", ScreenPresentation.Modal, BuildProbability, false);
            registry.Register("summon-probability-details", ScreenPresentation.Modal, BuildProbabilityDetails, false);
            registry.Register("summon-result", ScreenPresentation.Fullscreen, BuildSummonResult, false);
            registry.Register("dungeons", ScreenPresentation.Page, BuildDungeons, false);
            registry.Register("dungeon-details", ScreenPresentation.Modal, BuildDungeonDetails, false);
        }

        static void BuildCollection(ScreenContext ctx)
        {
            var root = ctx.Root;
            var font = ctx.Assets.font;
            AddBackdrop(root, ctx);
            var state = new CollectionState();
            var header = Panel(root, 42, 26, 996, 100, "✦  스킬 15/18  ✦", font, 42);
            state.summary = Ui.Text("Summary", root, 105, 126, 870, 52, "+10.3m 기본 피해  +82.8m 기본 체력", 25, font, Ui.Ivory);
            // Reserve a fixed interaction rail (equipped row, actions, summon and tabs)
            // while allowing taller safe areas to expand the scrollable collection.
            var bodyHeight = Mathf.Max(650, ctx.Height - 850);
            state.content = Ui.Rect("Tab content", root, 48, 186, 984, bodyHeight);
            Panel(root, 48, 186 + bodyHeight, 984, 112, "장착됨", font, 27);
            for (var i = 0; i < 3; i++)
                SkillSlot(root, 510 + i * 142, 197 + bodyHeight, 112, Skills[9 + i], font, null, true);
            Ui.Button("Upgrade all", root, 245, 318 + bodyHeight, 280, 76, "모두 업그레이드", font,
                () => { foreach (var s in Skills) s.level++; RenderTab(ctx, state); ctx.Toast("보유 스킬을 업그레이드했습니다."); }, Blue, 26);
            Ui.Button("Quick equip", root, 555, 318 + bodyHeight, 280, 76, "빠른 장착", font,
                () => ctx.Toast("가장 강한 스킬 3개를 장착했습니다."), Blue, 26);
            var summonY = 410 + bodyHeight;
            Ui.Button("Summon five", root, 330, summonY, 420, 104, "소환 x5\n◆ 160", font, () => {
                if (summonCurrency < 160) { ctx.Toast("소환권이 부족합니다."); return; }
                summonCurrency -= 160; ctx.Open("summon-result");
            }, Blue, 30);
            Ui.Button("Probability", root, 770, summonY + 8, 76, 76, "!", font, () => ctx.Open("summon-probability"), Stone, 32);
            state.currency = Ui.Text("Currency", root, 70, summonY + 17, 230, 60, "◆ " + summonCurrency.ToString("N0"), 28, font, Green, TextAnchor.MiddleLeft);
            var tabY = summonY + 118;
            string[] tabs = { "스킬", "펫", "영웅" };
            for (var i = 0; i < tabs.Length; i++) {
                var index = i;
                state.tabs[i] = Ui.Button("Tab " + tabs[i], root, 48 + i * 328, tabY, 328, 74, tabs[i], font, () => { state.tab = index; RenderTab(ctx, state); }, i == 0 ? Blue : Stone, 28);
            }
            RenderTab(ctx, state);
        }

        static void RenderTab(ScreenContext ctx, CollectionState state)
        {
            for (var i = state.content.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(state.content.GetChild(i).gameObject);
            string[] summaries = { "+10.3m 기본 피해  +82.8m 기본 체력", "+24.6m 동료 피해  +31.2m 동료 체력", "+18.4m 영웅 피해  +96.7m 영웅 체력" };
            state.summary.text = summaries[state.tab];
            for (var i = 0; i < 3; i++) state.tabs[i].targetGraphic.color = i == state.tab ? Blue : Stone;
            if (state.tab == 0) {
                var scroll = Scroll(state.content, 0, 0, state.content.rect.width, state.content.rect.height);
                for (var i = 0; i < Skills.Length; i++) {
                    var skill = Skills[i];
                    SkillSlot(scroll.content, 24 + (i % 5) * 184, 20 + (i / 5) * 230, 150, skill, ctx.Assets.font,
                        () => ctx.Open("skill-details", skill), false);
                }
                scroll.content.sizeDelta = new Vector2(0, Mathf.Max(scroll.viewport.rect.height, 700));
            } else {
                var title = state.tab == 1 ? "달빛 동료" : "어둠의 영웅";
                var names = state.tab == 1 ? new[] { "푸른 용", "그림자 요정", "수호 늑대", "불꽃 정령", "해골 기사", "달빛 까마귀" }
                    : new[] { "검은 방랑자", "성채의 마녀", "망령 기사", "심연 사냥꾼", "별의 예언자", "피의 군주" };
                Ui.Text("Inferred title", state.content, 0, 12, 984, 58, title + "  6/12", 34, ctx.Assets.font, Ui.Gold);
                for (var i = 0; i < names.Length; i++) {
                    var skill = new SkillData(names[i], 30 + i * 7, (i + 2) % 8, i + state.tab, "+동료 전투력 " + (12 + i * 3) + "%");
                    SkillSlot(state.content, 105 + (i % 3) * 280, 90 + (i / 3) * 285, 190, skill, ctx.Assets.font,
                        () => ctx.Toast(names[i] + " 선택"), false);
                }
                Ui.Text("Inference note", state.content, 80, state.content.rect.height - 96, 824, 70,
                    "보유한 " + (state.tab == 1 ? "펫" : "영웅") + "을 선택해 편성할 수 있습니다.", 23, ctx.Assets.font, Ui.Ivory);
            }
        }

        static void BuildSkillDetails(ScreenContext ctx)
        {
            var skill = ctx.Payload as SkillData ?? Skills[6];
            var font = ctx.Assets.font;
            var h = Mathf.Min(850, ctx.Height - 180);
            var y = (ctx.Height - h) * .5f;
            var panel = Panel(ctx.Root, 80, y, 920, h, "", font, 1);
            SkillSlot(panel, 56, 78, 210, skill, font, null, false);
            Ui.Text("Name", panel, 305, 86, 550, 64, "[서사시] " + skill.name, 34, font, Green, TextAnchor.MiddleLeft);
            Ui.Text("Description", panel, 305, 155, 540, 130, "전장에 마력을 펼쳐 모든 적에게 강력한 피해를 줍니다.\n현재 레벨 " + skill.level, 25, font, Ui.Ivory, TextAnchor.UpperLeft);
            Ui.Text("Passive", panel, 60, 330, 800, 50, "패시브:", 28, font, Ui.Gold, TextAnchor.MiddleLeft);
            Panel(panel, 60, 388, 800, 92, skill.passive, font, 25);
            Ui.Button("Upgrade", panel, 80, h - 150, 350, 86, "업그레이드", font, () => { skill.level++; ctx.Toast("레벨 " + skill.level + " 달성"); }, Stone, 28);
            Ui.Button("Equip", panel, 490, h - 150, 350, 86, "장착", font, () => ctx.Toast(skill.name + " 장착됨"), Blue, 30);
            Close(panel, 410, h - 48, font, ctx.Close);
        }

        static void BuildProbability(ScreenContext ctx)
        {
            var font = ctx.Assets.font;
            var h = Mathf.Min(980, ctx.Height - 150);
            var panel = Panel(ctx.Root, 90, (ctx.Height - h) / 2, 900, h, "레벨 68\n소환 확률", font, 36);
            Ui.Button("Details", panel, 790, 105, 64, 64, "i", font, () => ctx.Open("summon-probability-details"), Stone, 28);
            var labels = new[] { "일반 ★", "희귀한 ★", "서사시 ★", "전설 ★", "궁극의 ★", "신화 ★" };
            var rates = new[] { "17.50%", "16.50%", "16.50%", "36.48%", "12.99%", "0.03%" };
            for (var i = 0; i < labels.Length; i++) {
                var row = Ui.Panel("Rarity " + i, panel, 70, 220 + i * 74, 760, 60, Rarity[i]);
                Ui.Text("Name", row.transform, 22, 2, 480, 56, labels[i], 25, font, Ui.Ivory, TextAnchor.MiddleLeft);
                Ui.Text("Rate", row.transform, 530, 2, 205, 56, rates[i], 25, font, Ui.Ivory, TextAnchor.MiddleRight);
            }
            Ui.Text("Hint", panel, 70, h - 225, 760, 60, "스킬을 소환하여 레벨 업하고 소환 확률을 높이세요!", 22, font);
            Progress(panel, 75, h - 155, 750, 54, .68f, "75/110", font);
            Close(panel, 400, h - 48, font, ctx.Close);
        }

        static void BuildProbabilityDetails(ScreenContext ctx)
        {
            var font = ctx.Assets.font;
            var h = Mathf.Min(1120, ctx.Height - 100);
            var panel = Panel(ctx.Root, 95, (ctx.Height - h) / 2, 890, h, "모든 스킬의 목록", font, 34);
            var scroll = Scroll(panel, 45, 120, 800, h - 205);
            var groups = new[] { "일반 ★       17.50%", "희귀한 ★       16.50%", "서사시 ★       16.50%", "전설 ★       36.48%" };
            for (var g = 0; g < groups.Length; g++) {
                var yy = g * 300f;
                Panel(scroll.content, 20, yy, 760, 60, groups[g], font, 24);
                for (var j = 0; j < 3; j++) {
                    var skill = Skills[(g * 3 + j) % Skills.Length];
                    SkillSlot(scroll.content, 60 + j * 245, yy + 75, 150, skill, font, () => ctx.Open("skill-details", skill), false);
                    Ui.Text("Chance", scroll.content, 50 + j * 245, yy + 230, 170, 45, g == 0 ? "5.8333%" : "5.5000%", 20, font);
                }
            }
            scroll.content.sizeDelta = new Vector2(0, groups.Length * 300);
            Close(panel, 395, h - 48, font, ctx.Close);
        }

        static void BuildSummonResult(ScreenContext ctx)
        {
            var font = ctx.Assets.font;
            AddBackdrop(ctx.Root, ctx);
            Ui.Text("Title", ctx.Root, 90, 80, 900, 90, "소환 결과", 46, font, Ui.Gold);
            Ui.Text("Subtitle", ctx.Root, 90, 170, 900, 60, "새로운 힘이 달빛 아래 깨어납니다", 24, font);
            var y = ctx.Height * .43f;
            for (var i = 0; i < 5; i++) {
                var skill = Skills[(i * 2 + 8) % Skills.Length];
                SkillSlot(ctx.Root, 45 + i * 205, y, 170, skill, font, () => ctx.Open("skill-details", skill), false);
                var glow = Ui.Image("Reveal glow", ctx.Root, 53 + i * 205, y + 8, 154, 154, null, new Color(.1f,.65f,1f,.12f));
                glow.transform.SetAsFirstSibling();
            }
            Ui.Button("Again", ctx.Root, 170, ctx.Height - 250, 340, 88, "다시 소환 x5", font, () => {
                if (summonCurrency < 160) ctx.Toast("소환권이 부족합니다."); else { summonCurrency -= 160; ctx.Toast("새 소환 결과를 확인하세요."); }
            }, Blue, 28);
            Ui.Button("Return", ctx.Root, 570, ctx.Height - 250, 340, 88, "돌아가기", font, ctx.Close, Stone, 28);
        }

        static void BuildDungeons(ScreenContext ctx)
        {
            var font = ctx.Assets.font;
            AddBackdrop(ctx.Root, ctx);
            Panel(ctx.Root, 220, 36, 640, 100, "던전", font, 44);
            Ui.Text("Reset", ctx.Root, 120, 145, 840, 86, "던전 열쇠는 매일 09:00에 보충됩니다.\n열쇠는 던전을 완료할 때만 소모됩니다.", 25, font);
            var available = ctx.Height - 450;
            var scroll = Scroll(ctx.Root, 60, 250, 960, available);
            for (var i = 0; i < Dungeons.Length; i++) {
                var dungeon = Dungeons[i];
                var row = Ui.Panel("Dungeon " + dungeon.name, scroll.content, 10, i * 250, 920, 220, new Color(.025f,.085f,.12f,.98f));
                var icon = GetIcon(ctx.Assets, dungeon.icon);
                Ui.Image("Illustration", row.transform, 20, 25, 260, 170, icon, new Color(.55f,.75f,.85f));
                Ui.Text("Name", row.transform, 305, 18, 360, 58, dungeon.name, 32, font, Ui.Ivory, TextAnchor.MiddleLeft);
                Ui.Text("Locale", row.transform, 305, 76, 360, 48, dungeon.locale, 22, font, Ui.Gold, TextAnchor.MiddleLeft);
                Ui.Text("Keys", row.transform, 680, 25, 190, 50, "⚿  2/2", 27, font);
                var index = i;
                Ui.Button("Open", row.transform, 650, 104, 220, 78, "열기", font, () => { selectedDungeon = index; ctx.Open("dungeon-details", Dungeons[index]); }, Blue, 28);
            }
            scroll.content.sizeDelta = new Vector2(0, Dungeons.Length * 250);
        }

        static void BuildDungeonDetails(ScreenContext ctx)
        {
            var dungeon = ctx.Payload as DungeonData ?? Dungeons[Mathf.Clamp(selectedDungeon, 0, Dungeons.Length - 1)];
            var font = ctx.Assets.font;
            var h = Mathf.Min(1050, ctx.Height - 180);
            var panel = Panel(ctx.Root, 105, (ctx.Height - h) / 2, 870, h, dungeon.name, font, 38);
            Ui.Image("Dungeon art", panel, 45, 120, 780, 300, GetIcon(ctx.Assets, dungeon.icon), new Color(.45f,.7f,.83f));
            Ui.Text("Locale", panel, 55, 420, 760, 65, dungeon.locale, 34, font, Ui.Ivory);
            Ui.Text("Difficulty", panel, 100, 500, 670, 72, "◀     난이도  " + dungeon.stage + "     ▶", 30, font, Ui.Gold);
            Panel(panel, 120, 592, 630, 86, "보상:  " + dungeon.reward, font, 27);
            Ui.Text("Keys", panel, 280, 690, 310, 65, "🔑  2/2", 31, font);
            Ui.Button("Previous", panel, 55, h - 150, 350, 88, "이전 스테이지\n소탕", font, () => ctx.Toast("이전 스테이지 보상을 확인했습니다."), Blue, 26);
            Ui.Button("Enter", panel, 465, h - 150, 350, 88, "입장", font, () => ctx.Toast("로컬 데모: " + dungeon.name + " 입장 준비"), Blue, 29);
            Close(panel, 385, h - 48, font, ctx.Close);
        }

        static void AddBackdrop(Transform root, ScreenContext ctx)
        {
            var image = Ui.Image("Moonlit backdrop", root, 0, 0, ctx.Width, ctx.Height, ctx.Assets.worldBackground, new Color(.28f,.48f,.62f,1f));
            image.type = Image.Type.Simple; image.preserveAspect = false;
            image.transform.SetAsFirstSibling();
        }

        static RectTransform Panel(Transform parent, float x, float y, float w, float h, string title, Font font, int size)
        {
            var p = Ui.Panel("Ornate stone panel", parent, x, y, w, h, Ink).rectTransform;
            Ui.Border(p, w, h, new Color(.7f,.47f,.2f), 5);
            Ui.Image("Top ornament", p, w * .5f - 45, -9, 90, 18, null, Ui.Gold);
            if (!string.IsNullOrEmpty(title)) Ui.Text("Title", p, 20, 10, w - 40, Mathf.Min(100, h - 20), title, size, font, Ui.Ivory);
            return p;
        }

        static Button Close(Transform parent, float x, float y, Font font, Action close)
        {
            return Ui.Button("Close", parent, x, y, 100, 100, "×", font, () => close(), Red, 52);
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

        static void SkillSlot(Transform parent, float x, float y, float size, SkillData skill, Font font, Action click, bool compact)
        {
            var totalH = compact ? size : size + 55;
            var button = Ui.Button("Skill " + skill.name, parent, x, y, size, totalH, "", font, click == null ? null : () => click(), Stone, 18);
            var frame = button.GetComponent<Image>(); frame.color = new Color(.045f,.055f,.06f,.98f);
            var icon = Ui.Image("Icon", button.transform, 10, 10, size - 20, size - 20, null, Rarity[Mathf.Abs(skill.icon) % Rarity.Length]);
            icon.sprite = null;
            Ui.Image("Rune", icon.transform, (size-20)*.24f, (size-20)*.18f, (size-20)*.52f, (size-20)*.52f, null, new Color(.95f,.2f + .1f*(skill.icon%4),.08f,.9f));
            Ui.Text("Glyph", icon.transform, 0, 0, size-20, size-20, Glyph(skill.icon), Mathf.RoundToInt(size*.34f), font, Ui.Ivory);
            Ui.Text("Level", button.transform, 4, size - 45, size - 8, 42, "Lv." + skill.level, Mathf.RoundToInt(size*.17f), font);
            if (!compact) {
                Ui.Text("Star", button.transform, 0, size - 10, size, 34, "★", Mathf.RoundToInt(size*.18f), font, Gold);
                Progress(button.transform, 9, size + 24, size - 18, 24, skill.shards / 8f, skill.shards + "/8", font);
            }
        }

        static void Progress(Transform parent, float x, float y, float w, float h, float amount, string label, Font font)
        {
            Ui.Panel("Progress", parent, x, y, w, h, new Color(.015f,.025f,.035f));
            Ui.Image("Fill", parent, x + 4, y + 4, (w - 8) * Mathf.Clamp01(amount), h - 8, null, new Color(.08f,.5f,.85f));
            Ui.Text("Value", parent, x, y, w, h, label, Mathf.RoundToInt(h*.65f), font);
        }

        static string Glyph(int index)
        {
            string[] glyphs = { "◆", "ϟ", "♨", "☾", "✹", "♜", "♆", "☠", "✦" };
            return glyphs[Mathf.Abs(index) % glyphs.Length];
        }

        static Sprite GetIcon(MainScreenAssets assets, int index)
        {
            if (assets.equipmentIcons != null && assets.equipmentIcons.Length > 0) return assets.equipmentIcons[Mathf.Abs(index) % assets.equipmentIcons.Length];
            if (assets.interfaceIcons != null && assets.interfaceIcons.Length > 0) return assets.interfaceIcons[Mathf.Abs(index) % assets.interfaceIcons.Length];
            return null;
        }

        sealed class CollectionState
        {
            public int tab;
            public RectTransform content;
            public Text summary;
            public Text currency;
            public readonly Button[] tabs = new Button[3];
        }

        sealed class SkillData
        {
            public readonly string name;
            public int level;
            public int shards;
            public readonly int icon;
            public readonly string passive;
            public SkillData(string name, int level, int shards, int icon, string passive) { this.name=name; this.level=level; this.shards=shards; this.icon=icon; this.passive=passive; }
        }

        sealed class DungeonData
        {
            public readonly string name, locale, stage, reward;
            public readonly int icon;
            public DungeonData(string name, string locale, string stage, string reward, int icon) { this.name=name; this.locale=locale; this.stage=stage; this.reward=reward; this.icon=icon; }
        }
    }
}
