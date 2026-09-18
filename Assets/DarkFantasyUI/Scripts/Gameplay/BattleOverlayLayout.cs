using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    // Positions follow the user's green/pink/red annotations relative to the equipment panel.
    public sealed class BattleOverlayLayout : MonoBehaviour
    {
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
            var rewardLabel=layout.reward.Find("Event timer").GetComponent<Text>();
            rewardLabel.fontSize=24;rewardLabel.horizontalOverflow=HorizontalWrapMode.Overflow;
            var skills=Ui.Rect("Equipped battle skills",bottom,678,-136,378,128);
            skills.gameObject.AddComponent<EquippedSkillHud>().Initialize(main,circle);
            layout.LateUpdate();
        }
        void LateUpdate()
        {
            if(!main || !bottom)return;
            reward.anchoredPosition=new Vector2(29,145);
            float equipmentTop=((RectTransform)main.design).rect.height-PortraitSafeArea.BottomHeight;
            float passTop=Mathf.Max(330,equipmentTop-650);
            pass.anchoredPosition=new Vector2(934,equipmentTop-passTop);
        }
    }
}
