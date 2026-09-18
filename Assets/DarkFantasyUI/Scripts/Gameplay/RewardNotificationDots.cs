using System;
using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    /// <summary>Availability markers share the same conditions as their claim or entry actions.</summary>
    public sealed class RewardNotificationDots : MonoBehaviour
    {
        MainScreen main;
        GameObject[] navigation;
        GameObject offline,pass,forge;
        Text forgeTimer;
        float next;
        public void Initialize(MainScreen screen,Sprite circle)
        {
            main=screen;
            forgeTimer=screen.forgeLevelButton.transform.parent.Find("Forge level timer").GetComponent<Text>();
            navigation=new GameObject[screen.navigation.Length];
            for(int i=0;i<navigation.Length;i++)navigation[i]=screen.navigation[i].transform.Find("Notification").gameObject;
            offline=Create(screen.eventButton.transform,87,0,27,circle);
            pass=Create(screen.fairyButton.transform,87,0,27,circle);
            forge=Create(screen.forgeLevelButton.transform,((RectTransform)screen.forgeLevelButton.transform).rect.width-25,-5,27,circle);
            RefreshNow();
        }
        public static GameObject Create(Transform parent,float x,float y,float size,Sprite circle)
        {
            var rim=Ui.Image("Notification",parent,x,y,size,size,circle,Ui.Ivory);
            Ui.Image("Red dot",rim.transform,2,2,size-4,size-4,circle,new Color(1,.08f,.08f));
            rim.gameObject.SetActive(false);NotificationPulse.Ensure(rim.gameObject);return rim.gameObject;
        }
        public static Sprite Circle(MainScreen screen)
        {
            if(!screen || screen.navigation==null || screen.navigation.Length==0)return null;
            var dot=screen.navigation[0].transform.Find("Notification");
            return dot?dot.GetComponent<Image>().sprite:null;
        }
        public static bool NavigationAvailable(MainScreen screen,int index)
        {
            if(!screen)return false;
            switch(index) {
                case 0:return RewardState.Current.ArenaRemaining(DateTime.UtcNow)>0;
                case 1:
                    DungeonProgression.RefreshDay(DateTime.UtcNow);
                    return Array.Exists(DungeonProgression.Data.keys,k=>k>0);
                case 2:return screen.skillTickets>0||screen.petTickets>0||screen.mountTickets>0;
                case 3:return RewardState.Current.CanClaimDailyDiamonds;
                default:return false;
            }
        }
        public void RefreshNow()
        {
            if(!main || navigation==null)return;
            RewardState.Current.Advance(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            for(int i=0;i<navigation.Length;i++)navigation[i].SetActive(!main.navigation[i].transform.Find("Close icon").gameObject.activeSelf && NavigationAvailable(main,i));
            offline.SetActive(RewardState.Current.CanClaim);
            pass.SetActive(RewardState.Current.HasPassReward(main.highestClearedStage));
            var state=ForgeState.Current;int phase=state.UpgradePhase(DateTime.UtcNow);
            forge.SetActive(phase==1 || phase==3 || (phase==0 && main.gold>=state.SegmentCost));
            if(forgeTimer) {
                var remaining=TimeSpan.FromSeconds(Math.Ceiling(state.RemainingSeconds(DateTime.UtcNow)));
                forgeTimer.text=phase==2?((int)remaining.TotalHours).ToString("00")+":"+remaining.Minutes.ToString("00")+":"+remaining.Seconds.ToString("00")
                    :phase==3?(state.level==35?"승천 가능":"업글 완료"):phase==1?"시간 업글":phase==4?"만렙":"";
            }
        }
        void Update(){if(Time.unscaledTime<next)return;next=Time.unscaledTime+.2f;RefreshNow();}
    }
}
