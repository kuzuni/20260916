using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Moonlit.UI
{
    /// <summary>Runtime-only forge, equipment, offline reward, and progress-pass screens.</summary>
    public static class ForgeScreenModule
    {
        static readonly Color Ink = new Color(.025f, .04f, .055f, .97f);
        static readonly Color Slate = new Color(.055f, .09f, .12f, .98f);
        static readonly Color Blue = new Color(.015f, .23f, .43f);
        static readonly Color Red = new Color(.38f, .035f, .035f);
        static readonly Color Orange = new Color(1f, .52f, .12f);
        static bool offlineClaimed;
        static bool comparisonResolved;
        static readonly HashSet<int> passClaims = new HashSet<int>();

        static readonly string[] Tiers =
        {
            "원시적", "중세의", "근대 초기", "현대의", "우주", "항성간", "다중 우주", "양자", "지하 세계", "신성한"
        };
        static readonly string[] Rates33 = { "0%", "0%", "0%", "0%", "0%", "0%", "28%", "58%", "13%", "1%" };
        static readonly string[] Rates34 = { "0%", "0%", "0%", "0%", "0%", "0%", "11%", "64%", "23%", "2%" };
        static readonly Color[] TierColors =
        {
            new Color(.22f,.20f,.18f), new Color(.02f,.25f,.45f), new Color(.02f,.34f,.16f), new Color(.44f,.34f,.03f),
            new Color(.42f,.02f,.04f), new Color(.23f,.03f,.46f), new Color(.02f,.36f,.43f), new Color(.04f,.12f,.48f),
            new Color(.30f,.10f,.07f), new Color(.55f,.22f,.02f)
        };

        public static void Register(UiScreenRegistry registry)
        {
            registry.Register("forge-probability", ScreenPresentation.Modal, BuildProbability, false);
            registry.Register("forge-probability-details", ScreenPresentation.Modal, BuildProbabilityDetails, false);
            registry.Register("forge-item-details", ScreenPresentation.Modal, BuildItemDetails, false);
            registry.Register("equipment-details", ScreenPresentation.Modal, BuildEquipmentDetails, true);
            registry.Register("forge-comparison", ScreenPresentation.Modal, BuildComparison, false);
            registry.Register("offline-rewards", ScreenPresentation.Modal, BuildOfflineRewards, false);
            registry.Register("auto-forge", ScreenPresentation.Modal, BuildAutoForge, false);
            registry.Register("progress-pass", ScreenPresentation.Modal, BuildProgressPass, false);
        }

        static Font Font(ScreenContext c) => c.Assets != null ? c.Assets.font : null;
        static Sprite Icon(ScreenContext c, int index)
        {
            var icons = c.Assets != null ? c.Assets.equipmentIcons : null;
            return icons != null && icons.Length > 0 ? icons[Mathf.Abs(index) % icons.Length] : null;
        }

        static RectTransform Frame(ScreenContext c, string title, float width, float height, out RectTransform body, bool close = true)
        {
            width = Mathf.Min(width, c.Width - 36);
            height = Mathf.Min(height, c.Height - 36);
            var root = Ui.Rect(title + " Dialog", c.Root, (c.Width - width) * .5f, (c.Height - height) * .5f, width, height);
            var shadow = Ui.Panel("Ornate stone frame", root, 0, 0, width, height, Ink);
            Ui.Border(shadow.transform, width, height, new Color(.22f,.13f,.07f), 12);
            Ui.Border(shadow.transform, width, height, Ui.Gold, 4);
            Ui.Image("Top diamond", shadow.transform, width * .5f - 18, -12, 36, 36, null, Ui.Gold);
            Ui.Text("Title", root, 44, 20, width - 88, 72, title, 44, Font(c), Ui.Ivory);
            Ui.Image("Title rule", root, 70, 94, width - 140, 3, null, Ui.Gold);
            body = Ui.Rect("Live content", root, 30, 112, width - 60, height - 150);
            if (close)
            {
                var b = Ui.Button("Close", root, width * .5f - 42, height - 46, 84, 84, "×", Font(c), c.Close, Red, 58);
                b.transform.SetAsLastSibling();
            }
            return root;
        }

        static Button Action(ScreenContext c, Transform p, float x, float y, float w, float h, string label, UnityAction click, Color? color = null)
            => Ui.Button(label, p, x, y, w, h, label, Font(c), click, color ?? Blue, 30);

        static void BuildProbability(ScreenContext c)
        {
            Frame(c, "확률 정보", 820, 1370, out var b);
            Ui.Text("Subtitle", b, 0, 0, b.rect.width, 48, "제련 확률", 30, Font(c), Ui.Gold);
            Action(c, b, b.rect.width - 70, 0, 58, 58, "i", () => c.Open("forge-probability-details"), new Color(.1f,.1f,.1f));
            Ui.Text("Wallet", b, 20, 62, b.rect.width - 40, 52, "♛ 1.59m       ◆ 21", 31, Font(c));
            Ui.Text("Levels", b, 260, 116, b.rect.width - 280, 48, "레벨 33    ▶    레벨 34", 29, Font(c), Ui.Ivory, TextAnchor.MiddleRight);
            for (var i = 0; i < Tiers.Length; i++)
            {
                var y = 174 + i * 72;
                var row = Ui.Panel("Rarity " + Tiers[i], b, 14, y, b.rect.width - 28, 61, TierColors[i]);
                Ui.Text("Icon", row.transform, 12, 3, 54, 54, Glyph(i), 29, Font(c), Ui.Gold);
                Ui.Text("Name", row.transform, 64, 3, 300, 54, Tiers[i] + " ★", 27, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
                Ui.Text("Current", row.transform, 430, 3, 100, 54, Rates33[i], 27, Font(c));
                Ui.Text("Next", row.transform, 570, 3, 100, 54, Rates34[i], 27, Font(c), i > 5 ? Ui.Cyan : new Color(.7f,.7f,.7f));
            }
            Ui.Text("Progress label", b, 0, 906, b.rect.width, 42, "업그레이드 진행 중…", 27, Font(c));
            var progress = Ui.Panel("Timer bar", b, 82, 952, b.rect.width - 164, 52, Color.black);
            Ui.Image("Timer fill", progress.transform, 5, 5, (b.rect.width - 174) * .72f, 42, null, new Color(.02f,.38f,.85f));
            Ui.Text("Timer", progress.transform, 0, 0, progress.rectTransform.rect.width, 52, "22시 53분", 27, Font(c));
            Action(c, b, 230, 1025, 300, 90, "건너뛰기\n◆ 190", () => c.Toast("루비가 부족합니다."), Slate);
        }

        static string Glyph(int i) => new[] { "⚒", "†", "⌁", "▰", "✦", "◉", "▣", "⚛", "♜", "♛" }[i % 10];

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

        static void BuildProbabilityDetails(ScreenContext c)
        {
            Frame(c, "모든 장비의 목록", 760, 1330, out var b);
            Scroll(c, b, 0, 0, b.rect.width, b.rect.height - 18, 2580, out var content);
            var item = 0;
            for (var tier = 0; tier < 5; tier++)
            {
                var top = tier * 510;
                var header = Ui.Panel("Tier " + Tiers[tier], content, 8, top, content.rect.width - 16, 66, TierColors[tier]);
                Ui.Text("Tier", header.transform, 18, 2, 420, 60, Glyph(tier) + "  " + Tiers[tier] + " ★", 29, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
                Ui.Text("Rate", header.transform, 520, 2, 130, 60, Rates33[tier], 27, Font(c), Ui.Ivory, TextAnchor.MiddleRight);
                for (var row = 0; row < 3; row++) for (var col = 0; col < 5; col++)
                {
                    var index = item++;
                    var x = 12 + col * 134; var y = top + 78 + row * 132;
                    var slot = Ui.Button("Equipment " + index, content, x, y, 110, 104, "", Font(c), () => c.Open("forge-item-details", ItemAt(c, index)), Slate);
                    Ui.Image("Item icon", slot.transform, 12, 8, 86, 78, Icon(c, index));
                    Ui.Text("Star", slot.transform, 0, 76, 110, 25, "★", 22, Font(c), Ui.Gold);
                    Ui.Text("Rate", content, x - 4, y + 101, 118, 28, tier == 0 ? "0.0000%" : (tier + 1) + ".2500%", 17, Font(c));
                }
            }
        }

        static ItemDefinition ItemAt(ScreenContext c, int index)
        {
            var items = c.Assets != null ? c.Assets.items : null;
            return items != null && items.Length > 0 ? items[index % items.Length] : null;
        }

        static void BuildItemDetails(ScreenContext c)
        {
            Frame(c, "모든 장비의 목록", 660, 990, out var b);
            var item = c.Payload as ItemDefinition;
            var name = item != null && !string.IsNullOrEmpty(item.displayName) ? item.displayName : "[원시적] 발 감싸기";
            Ui.Panel("Item slot", b, 22, 18, 132, 132, new Color(.25f,.12f,.02f));
            Ui.Image("Separate item icon", b, 40, 32, 96, 96, item != null ? item.icon : Icon(c, 5));
            Ui.Text("Name", b, 174, 18, b.rect.width - 190, 58, name, 29, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
            Ui.Text("Health", b, 174, 75, b.rect.width - 190, 50, "2k 체력", 27, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
            var description = item != null && !string.IsNullOrEmpty(item.description) ? item.description : "장비은(는) 아래 목록에서 2개의 고유한 하위 스탯을 굴립니다.";
            Ui.Panel("Stats panel", b, 10, 170, b.rect.width - 20, 610, Slate);
            Ui.Text("Description", b, 28, 184, b.rect.width - 56, 82, description, 24, Font(c), Ui.Ivory, TextAnchor.UpperLeft);
            Ui.Image("Rule", b, 28, 272, b.rect.width - 56, 2, null, Ui.Gold);
            var stats = "+1% - 12% 치명타 확률\n+1% - 80% 치명타 피해\n+1% - 5% 블록 확률\n+1% - 4% 체력 재생\n+1% - 20% 생명력 흡수\n+1% - 20% 더블 찬스\n+1% - 15% 피해\n+1% - 50% 근접 피해\n+1% - 15% 원거리 피해\n+1% - 40% 공격 속도\n+1% - 30% 스킬 피해\n-1% - 7% 스킬 재사용 대기시간\n+1% - 15% 체력";
            Ui.Text("Possible stats", b, 32, 290, b.rect.width - 64, 455, stats, 23, Font(c), Ui.Ivory, TextAnchor.UpperLeft);
            Ui.Panel("Next tier", b, 12, 800, b.rect.width - 24, 65, new Color(.02f,.16f,.25f));
            Ui.Text("Next tier label", b, 30, 800, b.rect.width - 60, 65, "†  중세의 ★                                      0%", 25, Font(c), new Color(.2f,.65f,1f), TextAnchor.MiddleLeft);
        }

        static void BuildEquipmentDetails(ScreenContext c)
        {
            Frame(c, "장착됨", 820, 480, out var b);
            var slot = c.Payload as EquipmentSlot;
            var item = slot != null ? slot.item : ItemAt(c, 0);
            Ui.Panel("Rarity frame", b, 32, 24, 168, 168, new Color(.38f,.16f,.015f));
            Ui.Image("Equipment icon", b, 48, 38, 136, 124, item != null ? item.icon : Icon(c, 0));
            var level = slot != null ? slot.level : (item != null ? item.startingLevel : 109);
            Ui.Text("Level", b, 32, 150, 168, 42, "Lv." + level + " ★", 24, Font(c), Ui.Ivory);
            var name = item != null && !string.IsNullOrEmpty(item.displayName) ? item.displayName : "성스러운 벨트";
            Ui.Text("Item details", b, 225, 25, b.rect.width - 245, 195,
                "[신성한] " + name + "\n1.81b 체력\n+1.62% 블록 확률\n+30.1% 공격 속도", 29, Font(c), Orange, TextAnchor.UpperLeft);
            Ui.Text("Hint", b, 225, 230, b.rect.width - 245, 55, "장비 슬롯을 다시 눌러 다른 장비를 확인하세요.", 20, Font(c), new Color(.65f,.68f,.72f), TextAnchor.MiddleLeft);
        }

        static void BuildComparison(ScreenContext c)
        {
            Frame(c, "장비 비교", 820, 810, out var b, false);
            ComparisonCard(c, b, 10, 10, "장착됨", "[신성한] 머큐리 샌들", "1.79b 체력  ▲\n+8.48% 치명타 확률\n+13.2% 체력", 1, Orange);
            ComparisonCard(c, b, 10, 285, "새로운!", "[양자] 반중력 부츠", "110m 체력  ▼\n+5.27% 생명력 흡수\n+15% 더블 찬스", 2, new Color(.75f,.35f,1f));
            var status = Ui.Text("Decision", b, 0, 565, b.rect.width, 38, comparisonResolved ? "결정 완료" : "판매 또는 장착을 선택하세요.", 22, Font(c));
            var sell = Action(c, b, 20, 612, 320, 96, "판매", () => ResolveComparison(c, status, "새 장비를 판매했습니다. +120 망치"), Red);
            var equip = Action(c, b, 380, 612, 320, 96, "장착", () => ResolveComparison(c, status, "새 장비를 장착했습니다."), Blue);
            if (comparisonResolved) { sell.interactable = false; equip.interactable = false; }
        }

        static void ComparisonCard(ScreenContext c, Transform p, float x, float y, string tag, string name, string stats, int icon, Color color)
        {
            var card = Ui.Panel(tag, p, x, y, 720, 255, Slate);
            Ui.Text("Tag", card.transform, 14, 2, 180, 42, tag, 23, Font(c), color, TextAnchor.MiddleLeft);
            Ui.Panel("Slot", card.transform, 20, 50, 142, 152, new Color(color.r*.35f,color.g*.35f,color.b*.35f));
            Ui.Image("Icon", card.transform, 35, 63, 112, 112, Icon(c, icon));
            Ui.Text("Name", card.transform, 185, 48, 500, 48, name, 27, Font(c), color, TextAnchor.MiddleLeft);
            Ui.Text("Stats", card.transform, 185, 100, 500, 126, stats, 24, Font(c), Ui.Ivory, TextAnchor.UpperLeft);
        }

        static void ResolveComparison(ScreenContext c, Text status, string message)
        {
            if (comparisonResolved) return;
            comparisonResolved = true; status.text = message; c.Toast(message);
            foreach (var button in status.transform.parent.GetComponentsInChildren<Button>()) button.interactable = false;
        }

        static void BuildOfflineRewards(ScreenContext c)
        {
            Frame(c, "오프라인 보상", 690, 790, out var b);
            Ui.Text("Elapsed", b, 0, 8, b.rect.width, 54, "수집 시간: 2분 34초", 28, Font(c), new Color(.1f,1f,.25f));
            RewardIcon(c, b, 90, 100, "♛", "1.13/초", Ui.Gold);
            RewardIcon(c, b, 360, 100, "⚒", "1.14/분", new Color(.8f,.82f,.86f));
            Ui.Image("Divider", b, 60, 340, b.rect.width - 120, 3, null, Ui.Gold);
            Ui.Text("Totals", b, 0, 375, b.rect.width, 82, "♛ 174.22      ⚒ 2.31", 34, Font(c));
            var label = offlineClaimed ? "수집 완료" : "수집";
            Button claim = null;
            claim = Action(c, b, 135, 500, 360, 110, label, () =>
            {
                if (offlineClaimed) { c.Toast("이미 수집한 보상입니다."); return; }
                offlineClaimed = true; claim.interactable = false;
                claim.GetComponentInChildren<Text>().text = "수집 완료"; c.Toast("오프라인 보상을 수집했습니다.");
            });
            claim.interactable = !offlineClaimed;
        }

        static void RewardIcon(ScreenContext c, Transform p, float x, float y, string glyph, string rate, Color color)
        {
            Ui.Panel("Reward", p, x, y, 170, 170, new Color(.22f,.10f,.02f));
            Ui.Text("Glyph", p, x, y, 170, 150, glyph, 68, Font(c), color);
            Ui.Text("Rate", p, x - 10, y + 175, 190, 42, rate, 27, Font(c), new Color(.1f,1f,.2f));
        }

        static void BuildAutoForge(ScreenContext c)
        {
            Frame(c, "자동 제련", 790, 1320, out var b);
            Ui.Text("Keep", b, 12, 0, 200, 45, "유지", 28, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
            var keep = new List<Toggle>();
            for (var i = 0; i < 4; i++)
            {
                var row = Ui.Panel("Keep " + Tiers[6+i], b, 10, 55 + i*76, b.rect.width-20, 64, TierColors[6+i]);
                keep.Add(Check(c, row.transform, 12, 9, i == 3));
                Ui.Text("Name", row.transform, 78, 2, 430, 60, Glyph(6+i) + "  " + Tiers[6+i] + " ★", 25, Font(c), Ui.Ivory, TextAnchor.MiddleLeft);
                Ui.Text("Rate", row.transform, 540, 2, 120, 60, Rates33[6+i], 25, Font(c), Ui.Ivory, TextAnchor.MiddleRight);
            }
            Ui.Text("Filter label", b, 0, 370, b.rect.width-35, 42, "필터", 24, Font(c), Ui.Ivory, TextAnchor.MiddleRight);
            var filters = new[] { "치명타 확률", "치명타 피해", "블록 확률", "체력 재생", "생명력 흡수", "더블 찬스" };
            for (var i=0;i<filters.Length;i++)
            {
                var row=Ui.Panel("Filter "+filters[i],b,10,420+i*70,b.rect.width-20,58,Slate);
                Check(c,row.transform,12,6,i==0||i==1||i==5);
                Ui.Text("Label",row.transform,78,0,row.rectTransform.rect.width-90,58,filters[i],24,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            }
            Ui.Text("Hammer label",b,15,860,430,55,"한 번에 사용된 망치 수",24,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            var amount=Ui.Text("Hammer count",b,510,860,135,55,"22",30,Font(c));
            Action(c,b,650,860,50,27,"▲",()=> { amount.text=(Mathf.Min(99,int.Parse(amount.text)+1)).ToString(); },Slate,18);
            Action(c,b,650,888,50,27,"▼",()=> { amount.text=(Mathf.Max(1,int.Parse(amount.text)-1)).ToString(); },Slate,18);
            Ui.Text("Continue",b,15,935,570,58,"목표 장비를 찾으면 제련 계속하기",22,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            Check(c,b,630,944,true);
            Action(c,b,190,1025,350,100,"시작",()=>
            {
                if (!keep.Exists(t=>t.isOn)) c.Toast("유지할 등급을 하나 이상 선택하세요.");
                else c.Toast(amount.text+"개 망치로 자동 제련을 시작합니다.");
            });
        }

        static Button Action(ScreenContext c, Transform p, float x, float y, float w, float h, string label, UnityAction click, Color color, int size)
            => Ui.Button(label,p,x,y,w,h,label,Font(c),click,color,size);

        static Toggle Check(ScreenContext c, Transform p, float x, float y, bool value)
        {
            var bg=Ui.Panel("Checkbox",p,x,y,48,48,new Color(.02f,.03f,.04f)); bg.raycastTarget=true;
            var mark=Ui.Text("Checkmark",bg.transform,0,-3,48,48,"✓",35,Font(c),new Color(1f,.78f,.28f));
            var toggle=bg.gameObject.AddComponent<Toggle>(); toggle.targetGraphic=bg; toggle.graphic=mark; toggle.isOn=value;
            return toggle;
        }

        static void BuildProgressPass(ScreenContext c)
        {
            Frame(c, "진행 패스", 820, 1370, out var b);
            Ui.Text("Prompt",b,20,0,390,72,"전투를 진행하여 보상을\n받으세요!",25,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            Action(c,b,450,4,270,68,"₩13,900 ◆",()=>c.Toast("프리미엄 구매는 데모에서 연결되지 않습니다."),new Color(.40f,.20f,.02f));
            Ui.Panel("Free tab",b,8,90,350,60,Blue); Ui.Text("Free",b,8,90,350,60,"무료",28,Font(c));
            Ui.Panel("Premium tab",b,370,90,350,60,new Color(.40f,.20f,.02f)); Ui.Text("Premium",b,370,90,350,60,"프리미엄",28,Font(c));
            Scroll(c,b,0,165,b.rect.width,b.rect.height-180,1210,out var content);
            var stages=new[]{"어려움 3-1","어려움 3-15","어려움 4-1","어려움 4-15","어려움 5-1","어려움 5-15"};
            for(var i=0;i<stages.Length;i++) PassRow(c,content,i,i*195,stages[i]);
        }

        static void PassRow(ScreenContext c, Transform p, int index, float y, string stage)
        {
            Ui.Text("Stage",p,220,y,280,42,stage,23,Font(c),Ui.Ivory);
            Ui.Image("Timeline",p,357,y+42,7,150,null,new Color(.1f,.65f,1f));
            Ui.Image("Node",p,347,y+88,27,27,null,Ui.Cyan);
            var free=Ui.Panel("Free reward",p,10,y+45,330,125,new Color(.03f,.12f,.17f));
            Ui.Text("Rewards",free.transform,18,8,195,105,index%2==0?"🎟 220\n◆ 100":"◉ 150\n◆ 100",24,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            Button claim=null;
            claim=Action(c,free.transform,230,30,82,66,passClaims.Contains(index)?"✓":"받기",()=>
            {
                if(!passClaims.Add(index)){c.Toast("이미 받은 보상입니다.");return;}
                claim.interactable=false; claim.GetComponentInChildren<Text>().text="✓"; c.Toast(stage+" 무료 보상을 받았습니다.");
            },passClaims.Contains(index)?Slate:Blue,18);
            claim.interactable=!passClaims.Contains(index);
            var premium=Ui.Panel("Premium reward",p,382,y+45,330,125,new Color(.18f,.10f,.025f));
            Ui.Text("Premium rewards",premium.transform,18,8,210,105,index%2==0?"🎟 220\n◆ 5":"♛ 40k\n◆ 5",24,Font(c),Ui.Ivory,TextAnchor.MiddleLeft);
            Ui.Text("Lock",premium.transform,250,15,60,80,"▣",40,Font(c),Ui.Gold);
        }
    }
}
