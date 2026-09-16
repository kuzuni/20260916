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
            new SkillData("별빛 심판", 45, 4, 8, "+4.2m 기본 피해"), new SkillData("유성", 43, 6, 9, "+8.4m 기본 피해"),
            new SkillData("심연 폭탄", 44, 3, 10, "+7.1m 기본 피해"), new SkillData("봉인된 권능", 20, 0, 11, "+3.1m 기본 피해"),
            new SkillData("치유의 날개", 88, 1, 12, "+62.4m 기본 체력"),
            new SkillData("붉은 악마", 17, 4, 13, "+2.4m 기본 피해",true),
            new SkillData("심연의 군주", 19, 4, 14, "+2.8m 기본 피해",true),
            new SkillData("번개 강타", 20, 0, 15, "+3.1m 기본 피해",true),
            new SkillData("비전 얼음창", 1, 0, 16, "+1.2m 기본 피해",false),
            new SkillData("공허 구체", 1, 0, 17, "+1.5m 기본 피해",false)
        };
        // First three rows follow reference 19. The final three unowned entries are local demo content.
        static readonly int[] SkillDisplayOrder={0,1,2,3,4,5,6,12,7,8,9,10,13,14,15,11,16,17};
        static string SkillCollectionTitle => "스킬 "+Array.FindAll(Skills,s=>s.owned).Length+"/"+Skills.Length;
        static readonly DungeonData[] Dungeons = {
            new DungeonData("망치 도둑", "잿빛 대장간", "19-9", "강화석 346", 0),
            new DungeonData("유령 마을", "달빛 묘지", "18-5", "영혼석 280", 5),
            new DungeonData("침략", "무너진 성문", "17-3", "금화 12.5k", 1),
            new DungeonData("좀비 러시", "죽은 자의 정원", "19-9", "생명 물약 346", 7)
        };
        static int summonCurrency = 6830;
        static int selectedDungeon;
        static int selectedCollectionTab;
        static readonly int[] equippedSkills = { 15, 14, 13 };
        static readonly int[] selectedCompanions = { 0, 0 };
        static int summonSequence;
        static CollectionState activeCollection;

        internal static void ResetSession()
        {
            summonCurrency=6830; selectedDungeon=0; selectedCollectionTab=0; summonSequence=0; activeCollection=null;
            equippedSkills[0]=15; equippedSkills[1]=14; equippedSkills[2]=13;
            Array.Clear(selectedCompanions,0,selectedCompanions.Length);
            foreach(var skill in Skills) skill.Reset();
            skillIcons=null;
        }

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
            activeCollection = state;
            state.root = root;
            state.tab = selectedCollectionTab;
            state.title = Ui.Text("Collection title", root, 330, 38, 420, 70, "스킬 15/18", 44, font);
            var wallet = PopupSkin.Panel("Summon wallet",root,28,44,220,60).rectTransform;
            var ticket=Resources.Load<Sprite>("Moonlit/Skills/SummonTicket-v1");
            Ui.ArtImage("Summon currency icon",wallet,-8,-6,68,68,ticket).preserveAspect=true;
            state.currency=Ui.Text("Currency",wallet,65,0,146,60,FormatSummonCurrency(),34,font);
            var summary=PopupSkin.Panel("Collection summary frame",root,145,120,790,58).rectTransform;
            state.summary = Ui.Text("Summary", summary, 12, 0, 766, 58, "+10.3m 기본 피해  +82.8m 기본 체력", 25, font, Ui.Ivory);
            // The reference keeps the title/grid at the top and equipment/actions at the bottom.
            // Extra portrait height belongs to the scenery gap, not oversized grid rows.
            var equippedY = ctx.Height - 840;
            state.content = Ui.Rect("Tab content", root, 48, 205, 984, Mathf.Min(627, equippedY-225));
            var equippedPanel=PopupSkin.Panel("Equipped panel",root,88,equippedY,904,132).rectTransform;
            Ui.ArtImage("Equipped ribbon",equippedPanel,-2,18,228,49,PopupSkin.ParchmentRibbonArt);
            var equippedLabel=Ui.Text("Equipped label",equippedPanel,8,18,206,49,"장착됨",31,font,new Color(.06f,.045f,.025f));
            equippedLabel.GetComponent<Outline>().effectColor=new Color(1,1,1,.2f);
            state.equipped = Ui.Rect("Equipped skills", equippedPanel, 440, 8, 440, 124);
            RenderEquipped(state, font);
            PopupSkin.Button("Upgrade all", root, 242, equippedY + 162, 282, 88, "모두 업그레이드", font,
                () => {
                    int upgraded = 0;
                    foreach (var s in Skills) if (s.TryUpgrade()) upgraded++;
                    RefreshCollection(ctx, state);
                    ctx.Toast(upgraded > 0 ? "보유 스킬 " + upgraded + "개를 업그레이드했습니다." : "업그레이드할 스킬이 없습니다.");
                }, Blue, 26);
            PopupSkin.Button("Quick equip", root, 550, equippedY + 162, 282, 88, "빠른 장착", font,
                () => {
                    var candidates = new List<int>();
                    for (var i = 0; i < Skills.Length; i++) if (Skills[i].owned) candidates.Add(i);
                    candidates.Sort((a,b) => Skills[b].level != Skills[a].level ? Skills[b].level.CompareTo(Skills[a].level) : a.CompareTo(b));
                    for (var i = 0; i < 3; i++) equippedSkills[i] = candidates[i];
                    RenderEquipped(state, font);
                    RefreshSkillLabels(state.content);
                    ctx.Toast("레벨이 가장 높은 스킬 3개를 장착했습니다.");
                }, Blue, 26);
            var summonY = ctx.Height - 540;
            var summonRail=PopupSkin.Panel("Summon rail",root,0,summonY-24,1080,206).rectTransform;
            var summon = PopupSkin.Button("Summon five", root, 364, summonY, 342, 154, "", font, null, Blue, 30);
            Ui.Text("Summon label",summon.transform,8,8,326,65,"소환 x5",43,font);
            Ui.ArtImage("Summon cost icon",summon.transform,94,84,52,52,ticket).preserveAspect=true;
            Ui.Text("Summon cost",summon.transform,151,75,110,68,"160",42,font);
            PopupSkin.Button("Summon quantity",root,234,summonY+96,104,62,"x5",font,()=>ctx.Toast("한 번에 스킬 5개를 소환합니다."),Blue,29);
            PopupSkin.Back("Return to main",root,28,summonY+68,82,font,ctx.Close,true);
            state.summon = summon;
            summon.onClick.AddListener(() => {
                if (!summon.IsInteractable() || state.lastSummonFrame == Time.frameCount) return;
                if (summonCurrency < 160) { ctx.Toast("소환권이 부족합니다."); return; }
                state.lastSummonFrame = Time.frameCount;
                summonCurrency -= 160;
                var session = new SummonSession(state);
                ResolveSummon(session);
                RefreshCollection(ctx, state);
                ctx.Open("summon-result", session);
            });
            PopupSkin.Button("Probability", root, 776, summonY + 8, 58, 58, "i", font, () => ctx.Open("summon-probability"), Stone, 32);
            Ui.Text("Summon level",root,737,summonY+66,140,40,"Lv.68",27,font);
            Progress(root,733,summonY+112,156,36,.68f,"75/110",font);
            var tabY = ctx.Height - 320;
            string[] tabs = { "스킬", "펫", "영웅" };
            for (var i = 0; i < tabs.Length; i++) {
                var index = i;
                state.tabs[i] = PopupSkin.Button("Tab " + tabs[i], root, 48 + i * 328, tabY, 328, 74, tabs[i], font, () => { state.tab = selectedCollectionTab = index; RenderTab(ctx, state); }, i == state.tab ? Blue : Stone, 28);
            }
            RenderTab(ctx, state);
        }

        static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                var buttons = child.GetComponentsInChildren<Button>(true);
                foreach (var button in buttons) button.onClick.RemoveAllListeners();
                child.gameObject.SetActive(false);
                child.SetParent(null, false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        static void RenderEquipped(CollectionState state, Font font)
        {
            ClearChildren(state.equipped);
            for (var i = 0; i < 3; i++) SkillSlot(state.equipped, i * 144, 0, 112, Skills[equippedSkills[i]], font, null, true);
        }

        static void RenderTab(ScreenContext ctx, CollectionState state)
        {
            ClearChildren(state.content);
            string[] summaries = { "+10.3m 기본 피해  +82.8m 기본 체력", "+24.6m 동료 피해  +31.2m 동료 체력", "+18.4m 영웅 피해  +96.7m 영웅 체력" };
            state.summary.text = summaries[state.tab];
            state.title.text = new[] { SkillCollectionTitle, "펫 6/12", "영웅 6/12" }[state.tab];
            for (var i = 0; i < 3; i++) PopupSkin.Select(state.tabs[i], i == state.tab);
            if (state.tab == 0) {
                var scroll = Scroll(state.content, 0, 0, state.content.rect.width, state.content.rect.height);
                for (var i = 0; i < Skills.Length; i++) {
                    var skill = Skills[SkillDisplayOrder[i]];
                    SkillSlot(scroll.content, 60 + (i % 5) * 184, 12 + (i / 5) * 205, 150, skill, ctx.Assets.font,
                        () => ctx.Open("skill-details", new SkillDetailsPayload(skill, state)), false);
                }
                scroll.content.sizeDelta = new Vector2(0, Mathf.Max(scroll.viewport.rect.height,12+Mathf.CeilToInt(Skills.Length/5f)*205));
            } else {
                var companionScroll=Scroll(state.content,0,0,state.content.rect.width,state.content.rect.height);
                companionScroll.content.sizeDelta=new Vector2(0,820);
                var companionRoot=companionScroll.content;
                var title = state.tab == 1 ? "달빛 동료" : "어둠의 영웅";
                var names = state.tab == 1 ? new[] { "푸른 용", "그림자 요정", "수호 늑대", "불꽃 정령", "해골 기사", "달빛 까마귀" }
                    : new[] { "검은 방랑자", "성채의 마녀", "망령 기사", "심연 사냥꾼", "별의 예언자", "피의 군주" };
                Ui.Text("Inferred title", companionRoot, 0, 12, 984, 58, title + "  6/12", 34, ctx.Assets.font, Ui.Gold);
                for (var i = 0; i < names.Length; i++) {
                    var selectedName = names[i]; var selectionIndex = i; var selectionTab = state.tab;
                    var skill = new SkillData(names[i], 30 + i * 7, (i + 2) % 8, i + state.tab, "+동료 전투력 " + (12 + i * 3) + "%");
                    SkillSlot(companionRoot, 105 + (i % 3) * 280, 90 + (i / 3) * 285, 190, skill, ctx.Assets.font,
                        () => { selectedCompanions[selectionTab-1]=selectionIndex; RenderTab(ctx,state); ctx.Toast(selectedName + " 편성 완료"); }, false);
                    if (selectedCompanions[state.tab-1] == i)
                        Ui.Text("Selected " + i,companionRoot,105+(i%3)*280,250+(i/3)*285,190,38,"✓ 편성 중",21,ctx.Assets.font,Green);
                }
                Ui.Text("Inference note", companionRoot, 80, 724, 824, 70,
                    "보유한 " + (state.tab == 1 ? "펫" : "영웅") + "을 선택해 편성할 수 있습니다.", 23, ctx.Assets.font, Ui.Ivory);
            }
        }

        static void BuildSkillDetails(ScreenContext ctx)
        {
            var payload = ctx.Payload as SkillDetailsPayload;
            var skill = payload != null ? payload.skill : ctx.Payload as SkillData ?? Skills[6];
            var parent = payload != null ? payload.parent : activeCollection;
            var font = ctx.Assets.font;
            var h = Mathf.Min(850, ctx.Height - 180);
            var y = (ctx.Height - h) * .5f;
            var panel = Panel(ctx.Root, 80, y, 920, h, "", font, 1);
            SkillSlot(panel, 56, 78, 210, skill, font, null, false);
            Ui.Text("Name", panel, 305, 86, 550, 64, "[서사시] " + skill.name, 34, font, Green, TextAnchor.MiddleLeft);
            Ui.Text("Passive", panel, 60, 330, 800, 50, "패시브:", 28, font, Ui.Gold, TextAnchor.MiddleLeft);
            Panel(panel, 60, 388, 800, 92, skill.passive, font, 25);
            var description = Ui.Text("Description", panel, 305, 155, 540, 130, "전장에 마력을 펼쳐 모든 적에게 강력한 피해를 줍니다.\n현재 레벨 " + skill.level, 25, font, Ui.Ivory, TextAnchor.UpperLeft);
            Button upgrade = null;
            upgrade = PopupSkin.Button("Upgrade", panel, 80, h - 150, 350, 86, skill.IsMaxLevel ? "최대 레벨" : "업그레이드", font, () => {
                if (!skill.owned) { ctx.Toast("먼저 스킬을 획득하세요."); return; }
                if (!skill.TryUpgrade()) return;
                description.text = "전장에 마력을 펼쳐 모든 적에게 강력한 피해를 줍니다.\n현재 레벨 " + skill.level;
                RefreshCollection(ctx, parent);
                RefreshSkillLabels(panel);
                upgrade.interactable = !skill.IsMaxLevel;
                if (skill.IsMaxLevel) upgrade.GetComponentInChildren<Text>().text = "최대 레벨";
                ctx.Toast("레벨 " + skill.level + " 달성");
            }, Stone, 28);
            upgrade.interactable = skill.owned && !skill.IsMaxLevel;
            PopupSkin.Button("Equip", panel, 490, h - 150, 350, 86, "장착", font, () => {
                if (!skill.owned) { ctx.Toast("먼저 스킬을 획득하세요."); return; }
                Equip(skill);
                RefreshCollection(ctx, parent);
                ctx.Toast(skill.name + " 장착됨");
            }, Blue, 30);
            Close(panel, 410, h - 48, font, ctx.Close);
        }

        static void BuildProbability(ScreenContext ctx)
        {
            var font = ctx.Assets.font;
            var h = Mathf.Min(980, ctx.Height - 150);
            var panel = Panel(ctx.Root, 90, (ctx.Height - h) / 2, 900, h, "레벨 68\n소환 확률", font, 36);
            PopupSkin.Button("Details", panel, 790, 105, 64, 64, "i", font, () => ctx.Open("summon-probability-details"), Stone, 28);
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
            var groups = new[] { "일반 ★       17.50%", "희귀한 ★       16.50%", "서사시 ★       16.50%", "전설 ★       36.48%", "궁극의 ★       12.99%", "신화 ★       0.03%" };
            var chances=new[]{"5.8333%","5.5000%","5.5000%","12.1600%","4.3300%","0.0100%"};
            for (var g = 0; g < groups.Length; g++) {
                var yy = g * 300f;
                Panel(scroll.content, 20, yy, 760, 60, groups[g], font, 24);
                for (var j = 0; j < 3; j++) {
                    var skill = Skills[SkillDisplayOrder[g * 3 + j]];
                    SkillSlot(scroll.content, 60 + j * 245, yy + 75, 150, skill, font, () => ctx.Open("skill-details", skill), false);
                    Ui.Text("Chance", scroll.content, 50 + j * 245, yy + 230, 170, 45, chances[g], 20, font);
                }
            }
            scroll.content.sizeDelta = new Vector2(0, groups.Length * 300);
            Close(panel, 395, h - 48, font, ctx.Close);
        }

        static void BuildSummonResult(ScreenContext ctx)
        {
            var font = ctx.Assets.font;
            var session = ctx.Payload as SummonSession;
            if (session == null)
            {
                session = new SummonSession(activeCollection);
                // A direct preview route must not grant free shards or spend currency.
                for (var i = 0; i < session.results.Length; i++) session.results[i] = Skills[i];
            }
            AddBackdrop(ctx.Root, ctx);
            UnityEngine.Events.UnityAction close = () => { RefreshCollection(ctx, session.parent); ctx.Close(); };
            PopupSkin.Button("Return to collection", ctx.Root, 34, 34, 150, 70, "‹ 이전", font, close, Stone, 25);
            Ui.Text("Title", ctx.Root, 90, 80, 900, 90, "소환 결과", 46, font, Ui.Gold);
            Ui.Text("Subtitle", ctx.Root, 90, 170, 900, 60, "새로운 힘이 달빛 아래 깨어납니다", 24, font);
            var results = Ui.Rect("Summon result cards", ctx.Root, 0, 0, ctx.Width, ctx.Height);
            RenderSummonResults(ctx, results, session);
            var again = PopupSkin.Button("Again", ctx.Root, 170, ctx.Height - 250, 340, 88, "다시 소환 x5", font, null, Blue, 28);
            again.onClick.AddListener(() => {
                if (session.resolving || session.lastDecisionFrame == Time.frameCount) return;
                if (summonCurrency < 160) { ctx.Toast("소환권이 부족합니다."); return; }
                session.resolving = true;
                session.lastDecisionFrame = Time.frameCount;
                summonCurrency -= 160;
                ResolveSummon(session);
                RenderSummonResults(ctx, results, session);
                RefreshCollection(ctx, session.parent);
                session.resolving = false;
                ctx.Toast("새 소환 결과를 확인하세요.");
            });
            PopupSkin.Button("Return", ctx.Root, 570, ctx.Height - 250, 340, 88, "돌아가기", font, close, Stone, 28);
        }

        static void Equip(SkillData skill)
        {
            var index = Array.IndexOf(Skills, skill);
            if (index < 0 || Array.IndexOf(equippedSkills, index) >= 0) return;
            var replace = 0;
            for (var i = 1; i < equippedSkills.Length; i++)
                if (Skills[equippedSkills[i]].level < Skills[equippedSkills[replace]].level) replace = i;
            equippedSkills[replace] = index;
        }

        static void RefreshCollection(ScreenContext ctx, CollectionState state)
        {
            if (state == null || state.root == null) return;
            state.currency.text = FormatSummonCurrency();
            if(state.tab==0) state.title.text=SkillCollectionTitle;
            state.summon.interactable = true;
            RefreshSkillLabels(state.root);
            RenderEquipped(state, ctx.Assets.font);
        }

        static void RefreshSkillLabels(Transform root)
        {
            foreach (var skill in Skills)
            {
                var slots = root.GetComponentsInChildren<Button>(true);
                foreach (var slot in slots)
                {
                    if (slot.name != "Skill " + skill.name) continue;
                    SetSkillProgressState(slot.transform, skill);
                    var labels = slot.GetComponentsInChildren<Text>(true);
                    foreach (var label in labels)
                    {
                        if (label.name == "Level") label.text = "Lv." + skill.level;
                        else if (label.name == "Value")
                        {
                            label.text = skill.shards + "/"+skill.ShardsRequired;
                            var progress = label.transform.parent as RectTransform;
                            var fill = progress != null ? progress.Find("Fill") as RectTransform : null;
                            if (fill != null) fill.sizeDelta = new Vector2((label.rectTransform.rect.width - label.rectTransform.rect.height) * Mathf.Clamp01(skill.shards / (float)skill.ShardsRequired), fill.sizeDelta.y);
                        }
                        else if (label.name == "Ownership") label.text = skill.owned ? "" : "미보유";
                    }
                }
            }
        }

        static void ResolveSummon(SummonSession session)
        {
            for (var i = 0; i < session.results.Length; i++)
            {
                // A repeatable local sequence makes state assertions stable without implying a server RNG.
                var index = (summonSequence * 5 + i * 5 + 11) % Skills.Length;
                var skill = Skills[index];
                session.wasNew[i] = !skill.owned;
                skill.owned = true;
                skill.shards++;
                session.results[i] = skill;
            }
            summonSequence++;
        }

        static void RenderSummonResults(ScreenContext ctx, RectTransform root, SummonSession session)
        {
            ClearChildren(root);
            var y = ctx.Height * .43f;
            for (var i = 0; i < session.results.Length; i++)
            {
                var skill = session.results[i];
                SkillSlot(root, 45 + i * 205, y, 170, skill, ctx.Assets.font,
                    () => ctx.Open("skill-details", new SkillDetailsPayload(skill, session.parent)), false);
                Ui.Text("Summon status " + i, root, 45 + i * 205, y + 225, 170, 42,
                    session.wasNew[i] ? "신규 획득" : "+1 조각", 20, ctx.Assets.font, session.wasNew[i] ? Green : Ui.Gold);
                var glow = Ui.Image("Reveal glow", root, 53 + i * 205, y + 8, 154, 154, null, new Color(.1f,.65f,1f,.12f));
                glow.transform.SetAsFirstSibling();
            }
        }

        static string FormatSummonCurrency() => summonCurrency >= 1000 ? (summonCurrency / 1000f).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "k" : summonCurrency.ToString();

        static void BuildDungeons(ScreenContext ctx)
        {
            var font = ctx.Assets.font;
            AddBackdrop(ctx.Root, ctx);
            PopupSkin.Back("Return to main",ctx.Root,40,ctx.Height-364,96,font,ctx.Close);
            Panel(ctx.Root, 370, 36, 340, 100, "던전", font, 44);
            Ui.Text("Reset", ctx.Root, 120, 145, 840, 86, "던전 열쇠는 매일 09:00에 보충됩니다.\n열쇠는 던전을 완료할 때만 소모됩니다.", 25, font);
            var available = ctx.Height - 660;
            var scroll = Scroll(ctx.Root, 60, 250, 960, available);
            for (var i = 0; i < Dungeons.Length; i++) {
                var dungeon = Dungeons[i];
                var banner = DungeonBanner(dungeon);
                var row = PopupSkin.IllustratedCard("Dungeon " + dungeon.name, scroll.content, 10, i * 250, 920, 230, banner, Color.white);
                Ui.Image("Readable action scrim",row,670,12,236,204,null,new Color(0,.015f,.025f,.65f));
                Sprite reward = i==0 ? DungeonHammer() : i==1 ? Resources.Load<Sprite>("Moonlit/Skills/SummonTicket-v1") : PopupSkin.RewardIcon(i==2?0:1);
                Ui.ArtImage("Dungeon reward icon",row,26,20,60,60,reward).preserveAspect=true;
                Ui.Text("Name",row,96,18,470,58,dungeon.name,36,font,Ui.Ivory,TextAnchor.MiddleLeft);
                Ui.ArtImage("Dungeon key icon",row,702,38,52,52,PopupSkin.RewardIcon(new[]{5,2,4,3}[i])).preserveAspect=true;
                Ui.Text("Keys",row,760,34,134,58,"2/2",33,font);
                var index = i;
                PopupSkin.Button("Open",row,674,116,224,88,"열기",font,()=>{selectedDungeon=index;ctx.Open("dungeon-details",Dungeons[index]);},Blue,32);
            }
            scroll.content.sizeDelta = new Vector2(0, Dungeons.Length * 250);
        }

        static void BuildDungeonDetails(ScreenContext ctx)
        {
            var dungeon = ctx.Payload as DungeonData ?? Dungeons[Mathf.Clamp(selectedDungeon, 0, Dungeons.Length - 1)];
            var font = ctx.Assets.font;
            var h = Mathf.Min(1050, ctx.Height - 180);
            var panel = PopupSkin.Panel("Dungeon detail frame",ctx.Root,105,(ctx.Height-h)/2,870,h).rectTransform;
            var banner = DungeonBanner(dungeon);
            PopupSkin.IllustratedCard("Dungeon hero painting",panel,6,6,858,390,banner,Color.white);
            Panel(panel,235,20,400,90,dungeon.name,font,38);
            Ui.Text("Difficulty label",panel,285,414,300,44,"난이도",28,font);
            var difficulty=Ui.Text("Difficulty",panel,285,460,300,72,dungeon.stage,48,font,Ui.Ivory);
            int stage=int.Parse(dungeon.stage.Split('-')[1]); string chapter=dungeon.stage.Split('-')[0];
            PopupSkin.Button("Previous difficulty",panel,166,434,92,92,"◀",font,()=>{stage=Mathf.Max(1,stage-1);difficulty.text=chapter+"-"+stage;},Blue,40);
            var rewardPanel=PopupSkin.Panel("Dungeon reward",panel,64,562,742,104).rectTransform;
            int index=Array.IndexOf(Dungeons,dungeon);
            Sprite reward=index==0?DungeonHammer():index==1?Resources.Load<Sprite>("Moonlit/Skills/SummonTicket-v1"):index==2?ctx.Assets.interfaceIcons?[0]:PopupSkin.RewardIcon(1);
            Ui.Text("Reward label",rewardPanel,125,15,175,72,"보상:",28,font);
            Ui.Image("Reward icon",rewardPanel,315,16,65,72,reward).preserveAspect=true;
            Ui.Text("Reward amount",rewardPanel,394,15,220,72,index==1?"280":index==2?"12.5k":"346",40,font,Ui.Ivory,TextAnchor.MiddleLeft);
            Ui.ArtImage("Dungeon detail key",panel,342,701,65,65,PopupSkin.RewardIcon(new[]{5,2,4,3}[Mathf.Clamp(index,0,3)])).preserveAspect=true;
            Ui.Text("Keys",panel,416,694,150,75,"2/2",38,font);
            PopupSkin.Button("Previous",panel,55,h-218,350,120,"이전 스테이지\n소탕",font,()=>ctx.Toast("이전 스테이지 보상을 확인했습니다."),Blue,31);
            PopupSkin.Button("Enter",panel,465,h-218,350,120,"입장",font,()=>ctx.Toast("로컬 데모: "+dungeon.name+" 입장 준비"),Blue,35);
            Close(panel, 385, h - 48, font, ctx.Close);
        }

        static void DungeonPainting(Transform parent, float x, float y, float width, float height, Sprite sprite)
        {
            var crop = Ui.Rect("Dungeon painting crop", parent, x, y, width, height);
            crop.gameObject.AddComponent<RectMask2D>();
            float scale = Mathf.Max(width / sprite.rect.width, height / sprite.rect.height);
            float paintedWidth = sprite.rect.width * scale, paintedHeight = sprite.rect.height * scale;
            Ui.Image("Generated dungeon banner", crop, (width-paintedWidth)*.5f, (height-paintedHeight)*.5f,
                paintedWidth, paintedHeight, sprite);
        }

        static Sprite dungeonHammer;
        static Sprite DungeonHammer()
        {
            if(dungeonHammer) return dungeonHammer;
            var texture=Resources.Load<Texture2D>("Moonlit/Forge/RewardHammer-v1");
            if(texture) dungeonHammer=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            return dungeonHammer;
        }

        static Sprite DungeonBanner(DungeonData dungeon)
        {
            if (dungeon == Dungeons[0]) return Resources.Load<Sprite>("Moonlit/Dungeons/HammerThief-v1");
            if (dungeon == Dungeons[1]) return Resources.Load<Sprite>("Moonlit/Dungeons/GhostVillage-v1");
            if (dungeon == Dungeons[2]) return Resources.Load<Sprite>("Moonlit/Dungeons/Invasion-v1");
            if (dungeon == Dungeons[3]) return Resources.Load<Sprite>("Moonlit/Dungeons/ZombieRush-v1");
            return null;
        }

        static void AddBackdrop(Transform root, ScreenContext ctx)
        {
            var image = Ui.Image("Moonlit backdrop", root, 0, 0, ctx.Width, ctx.Height, ctx.Assets.worldBackground, new Color(.28f,.48f,.62f,1f));
            image.type = Image.Type.Simple; image.preserveAspect = false;
            image.transform.SetAsFirstSibling();
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

        static void SkillSlot(Transform parent, float x, float y, float size, SkillData skill, Font font, Action click, bool compact)
        {
            var totalH = compact ? size + 12 : size + 55;
            var button = Ui.ArtButton("Skill " + skill.name, parent, x, y, size, totalH);
            if (click != null) button.onClick.AddListener(() => click());
            var icon = Ui.Image("Icon", button.transform, size * .17f, size * .17f, size * .66f, size * .66f, SkillIcon(skill.icon));
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var frame = Ui.Image("Slot frame", button.transform, 0, 0, size, size, SkillRing);
            frame.preserveAspect = true;
            frame.raycastTarget = false;
            button.targetGraphic = frame;
            button.transition = Selectable.Transition.ColorTint;
            Ui.Text("Level", button.transform, 4, size - (compact ? 44 : 45), size - 8, compact ? 34 : 42, "Lv." + skill.level, Mathf.RoundToInt(size*.17f), font);
            Ui.Text("Ownership", button.transform, 4, 8, size - 8, 34, skill.owned ? "" : "미보유", Mathf.RoundToInt(size*.14f), font, Ui.Ivory);
            Ui.Text("Star", button.transform, 0, size - 10, size, compact ? 22 : 34, "★", Mathf.RoundToInt(size*.18f), font, Gold);
            if (!compact) {
                Progress(button.transform, 9, size + 24, size - 18, 28, skill.shards / (float)skill.ShardsRequired, skill.shards + "/"+skill.ShardsRequired, font);
                Ui.Text("Maximum level", button.transform, 0, size + 24, size, 28, "최대", Mathf.RoundToInt(size*.17f), font);
                var equippedBadge=Ui.Rect("Equipped badge",button.transform,0,size*.27f,size,size*.43f);
                Ui.ArtImage("Equipped lock",equippedBadge,size*.36f,0,size*.28f,size*.28f,PopupSkin.RewardIcon(6)).preserveAspect=true;
                var ribbon=Ui.Image("Equipped badge ribbon",equippedBadge,-4,size*.23f,size+8,size*.20f,PopupSkin.PanelArt);
                ribbon.type=Image.Type.Sliced; ribbon.pixelsPerUnitMultiplier=22;
                Ui.Text("Equipped badge label",equippedBadge,0,size*.23f,size,size*.20f,"장착됨",Mathf.RoundToInt(size*.15f),font);
                SetSkillProgressState(button.transform, skill);
            }
        }

        static void SetSkillProgressState(Transform slot, SkillData skill)
        {
            var progress = slot.Find("Progress");
            var maximum = slot.Find("Maximum level");
            if (progress) progress.gameObject.SetActive(!skill.IsMaxLevel);
            if (maximum) maximum.gameObject.SetActive(skill.IsMaxLevel);
            var equipped=slot.Find("Equipped badge");
            if(equipped) equipped.gameObject.SetActive(Array.IndexOf(equippedSkills,Array.IndexOf(Skills,skill))>=0);
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

        static Sprite SkillIcon(int index)
        {
            if (skillIcons == null || !skillIcons[0])
            {
                var atlas = Resources.Load<Texture2D>("Moonlit/Skills/SkillIcons-v1");
                if (!atlas) return null;
                var extras=Resources.Load<Texture2D>("Moonlit/Skills/SkillIcons-extra-v1");
                if(!extras) return null;
                skillIcons = new Sprite[18];
                int cellWidth = atlas.width / 4, cellHeight = atlas.height / 3;
                for (int i = 0; i < 12; i++)
                {
                    // PNG rows run top-to-bottom; Unity sprite rectangles start at the bottom.
                    var rect = new Rect((i % 4) * cellWidth, (2 - i / 4) * cellHeight, cellWidth, cellHeight);
                    skillIcons[i] = Sprite.Create(atlas, rect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                    skillIcons[i].name = "Skill illustration " + i;
                }
                float extraWidth=extras.width/3f,extraHeight=extras.height/2f;
                for(int i=0;i<6;i++)
                {
                    skillIcons[12+i]=Sprite.Create(extras,new Rect(i%3*extraWidth,(1-i/3)*extraHeight,extraWidth,extraHeight),
                        new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
                    skillIcons[12+i].name="Skill illustration "+(12+i);
                }
            }
            return skillIcons[Mathf.Abs(index) % skillIcons.Length];
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
            public RectTransform root;
            public RectTransform content;
            public RectTransform equipped;
            public Text summary;
            public Text title;
            public Text currency;
            public Button summon;
            public int lastSummonFrame = -1;
            public readonly Button[] tabs = new Button[3];
        }

        sealed class SkillData
        {
            public const int MaximumLevel = 100;
            public readonly string name;
            public int level;
            public int shards;
            public readonly int icon;
            public readonly string passive;
            public bool owned;
            readonly int initialLevel, initialShards;
            readonly bool initialOwned;
            public void Reset() { level=initialLevel; shards=initialShards; owned=initialOwned; }
            public int ShardsRequired => level<=20?5:8;
            public bool IsMaxLevel => level >= MaximumLevel;
            public bool TryUpgrade()
            {
                if (!owned || IsMaxLevel) return false;
                level++;
                return true;
            }
            public SkillData(string name, int level, int shards, int icon, string passive,bool? owned=null) { this.name=name; this.level=initialLevel=level; this.shards=initialShards=shards; this.icon=icon; this.passive=passive; this.owned=initialOwned=owned??level>20; }
        }

        sealed class SkillDetailsPayload
        {
            public readonly SkillData skill;
            public readonly CollectionState parent;
            public SkillDetailsPayload(SkillData skill, CollectionState parent) { this.skill=skill; this.parent=parent; }
        }

        sealed class SummonSession
        {
            public readonly CollectionState parent;
            public readonly SkillData[] results = new SkillData[5];
            public readonly bool[] wasNew = new bool[5];
            public bool resolving;
            public int lastDecisionFrame = -1;
            public SummonSession(CollectionState parent) { this.parent=parent; }
        }

        sealed class DungeonData
        {
            public readonly string name, locale, stage, reward;
            public readonly int icon;
            public DungeonData(string name, string locale, string stage, string reward, int icon) { this.name=name; this.locale=locale; this.stage=stage; this.reward=reward; this.icon=icon; }
        }
    }
}
