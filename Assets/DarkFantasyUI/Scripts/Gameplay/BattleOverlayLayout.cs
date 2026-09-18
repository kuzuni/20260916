using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    // All dimensions remain in the 1080-unit safe layout; skill feedback animates beneath its scaled parent.
    public sealed class BattleOverlayLayout : MonoBehaviour
    {
        public const float SkillScale=1.5f;
        public const float SkillWidth=378*SkillScale, SkillHeight=128*SkillScale;
        public const float HudBottomGap=12;
        MainScreen main;
        RectTransform bottom;
        RectTransform reward,pass;

        public static void Create(MainScreen main,RectTransform bottom,Sprite circle)
        {
            var layout=main.gameObject.AddComponent<BattleOverlayLayout>();
            layout.main=main;layout.bottom=bottom;
            layout.reward=(RectTransform)main.eventButton.transform;
            layout.pass=(RectTransform)main.fairyButton.transform;
            layout.reward.SetParent(bottom,false);layout.pass.SetParent(bottom,false);
            // The pass label was the larger of the two (34px after readable typography).
            // Both now share its 1.3x size, instead of enlarging one while shrinking the other.
            int labelSize=Mathf.RoundToInt(Ui.ReadableFontSize(27)*1.3f);
            ResizeEntry(layout.reward,"Offline reward clock and chest","Event timer",151.5f,labelSize);
            ResizeEntry(layout.pass,"Progress pass sword and pennant","Gift timer",150,labelSize);
            var skills=Ui.Rect("Equipped battle skills",bottom,1080-24-SkillWidth,-(SkillHeight+HudBottomGap),378,128);
            skills.localScale=Vector3.one*SkillScale;
            skills.gameObject.AddComponent<EquippedSkillHud>().Initialize(main,circle);
            layout.LateUpdate();
        }
        static void ResizeEntry(RectTransform entry,string iconName,string labelName,float iconWidth,int fontSize)
        {
            entry.sizeDelta=new Vector2(180,214);
            var icon=(RectTransform)entry.Find(iconName);
            icon.sizeDelta=new Vector2(iconWidth,150);
            icon.anchoredPosition=new Vector2((180-iconWidth)*.5f,0);
            var label=entry.Find(labelName).GetComponent<Text>();
            label.fontSize=fontSize;label.horizontalOverflow=HorizontalWrapMode.Overflow;
            label.rectTransform.anchoredPosition=new Vector2(-10,-156);
            label.rectTransform.sizeDelta=new Vector2(200,58);
        }
        void LateUpdate()
        {
            if(!main || !bottom)return;
            reward.anchoredPosition=new Vector2(29,214+HudBottomGap);
            float equipmentTop=((RectTransform)main.design).rect.height-PortraitSafeArea.BottomHeight;
            float passTop=Mathf.Max(330,equipmentTop-650);
            pass.anchoredPosition=new Vector2(1080-29-180,equipmentTop-passTop);
            // Availability dots are created after this layout and retain their own pulse animation.
            PlaceNotification(reward);PlaceNotification(pass);
        }
        static void PlaceNotification(RectTransform entry)
        {
            var notification=entry.Find("Notification") as RectTransform;
            if(notification)notification.anchoredPosition=new Vector2(151,0);
        }
    }
}
