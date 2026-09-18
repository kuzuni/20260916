using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI.Tests
{
    public sealed class BattleOverlayScaleTests
    {
        [TestCase(1920)]
        [TestCase(2280)]
        [TestCase(1740)]
        public void EnlargedEntriesAndSkillsStayInsidePortraitAndAboveEquipment(int height)
        {
            CollectionProgression.Reset();
            var root=new GameObject("HUD scale fixture",typeof(RectTransform));
            root.SetActive(false);
            try {
                var design=(RectTransform)root.transform;design.pivot=new Vector2(0,1);design.sizeDelta=new Vector2(1080,height);
                var main=root.AddComponent<MainScreen>();main.enabled=false;main.design=design;
                main.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                main.eventButton=Ui.ArtButton("Offline",design,0,0,111,142);
                Ui.Image("Offline reward clock and chest",main.eventButton.transform,5,0,101,100,null);
                Ui.Text("Event timer",main.eventButton.transform,-10,102,132,39,"보상 수집",29,main.font);
                main.fairyButton=Ui.ArtButton("Pass",design,0,0,120,130);
                Ui.Image("Progress pass sword and pennant",main.fairyButton.transform,10,-8,100,100,null);
                Ui.Text("Gift timer",main.fairyButton.transform,-14,90,152,38,"진행 패스",27,main.font);
                var bottom=Ui.Rect("Equipment panel",design,0,height-PortraitSafeArea.BottomHeight,1080,PortraitSafeArea.BottomHeight);
                BattleOverlayLayout.Create(main,bottom,null);
                var skills=(RectTransform)bottom.Find("Equipped battle skills");
                Assert.AreEqual(new Vector2(378,128),skills.sizeDelta,"Slot logic/feedback keeps its original coordinate system.");
                Assert.AreEqual(Vector3.one*1.5f,skills.localScale);
                var skillBounds=BoundsIn(skills,design);
                Assert.That(skillBounds.width,Is.EqualTo(567).Within(.01f));
                Assert.That(skillBounds.height,Is.EqualTo(192).Within(.01f));
                var equipmentBounds=BoundsIn(bottom,design);
                Assert.That(skillBounds.yMin-equipmentBounds.yMax,Is.EqualTo(12).Within(.01f));
                foreach(var button in new[]{main.eventButton,main.fairyButton}) {
                    var rect=(RectTransform)button.transform;var bounds=BoundsIn(rect,design);
                    Assert.GreaterOrEqual(bounds.xMin,0);Assert.LessOrEqual(bounds.xMax,1080);
                    Assert.GreaterOrEqual(bounds.yMin,-height);Assert.LessOrEqual(bounds.yMax,0);
                    Assert.IsFalse(bounds.Overlaps(skillBounds),"Reward controls must not intersect equipped skills.");
                }
                var rewardLabel=main.eventButton.transform.Find("Event timer").GetComponent<Text>();
                var passLabel=main.fairyButton.transform.Find("Gift timer").GetComponent<Text>();
                Assert.AreEqual(Mathf.RoundToInt(Ui.ReadableFontSize(27)*1.3f),rewardLabel.fontSize);
                Assert.AreEqual(rewardLabel.fontSize,passLabel.fontSize);
                foreach(var label in new[]{rewardLabel,passLabel}) {
                    var bounds=BoundsIn(label.rectTransform,design);
                    Assert.GreaterOrEqual(bounds.xMin,0);Assert.LessOrEqual(bounds.xMax,1080);
                }
                Assert.AreEqual(new Vector2(151.5f,150),((RectTransform)main.eventButton.transform.Find("Offline reward clock and chest")).sizeDelta);
                Assert.AreEqual(new Vector2(150,150),((RectTransform)main.fairyButton.transform.Find("Progress pass sword and pennant")).sizeDelta);
            } finally {Object.DestroyImmediate(root);CollectionProgression.Reset();}
        }
        static Rect BoundsIn(RectTransform rect,Transform parent)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            var min=parent.InverseTransformPoint(corners[0]);var max=parent.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
    }
}
