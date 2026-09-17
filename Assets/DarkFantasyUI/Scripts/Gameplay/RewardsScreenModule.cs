using System;
using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    public static class RewardsScreenModule
    {
        public static void Register(UiScreenRegistry registry)
        {
            registry.Register("offline-rewards",ScreenPresentation.Modal,Offline,false);
            registry.Register("progress-pass",ScreenPresentation.Modal,Pass,false);
            registry.Register("wallet",ScreenPresentation.Modal,Wallet,false);
        }
        static RectTransform Panel(ScreenContext c,string title,float requested)
        {
            float h=Mathf.Min(requested,c.Height-80);
            var panel=PopupSkin.Panel(title+" frame",c.Root,80,(c.Height-h)/2,920,h).rectTransform;
            panel.GetComponent<Image>().raycastTarget=true;
            Ui.Text("Title",panel,40,24,840,70,title,44,c.Assets.font,Ui.Gold);
            PopupSkin.Close("Close",panel,418,h-96,84,c.Assets.font,c.Close);
            return panel;
        }
        static void Offline(ScreenContext c)
        {
            var p=Panel(c,"오프라인 보상",1040);
            var time=Ui.Text("Accumulation time",p,70,124,780,70,"",30,c.Assets.font,Ui.Cyan);
            var coin=c.Assets.interfaceIcons[0];
            Ui.ArtImage("Offline gold illustration",p,198,220,180,180,coin).preserveAspect=true;
            Ui.ArtImage("Offline hammer illustration",p,542,220,180,180,PopupSkin.RewardIcon(0)).preserveAspect=true;
            Ui.Text("Gold rate",p,125,412,320,52,"골드 1 / 초",31,c.Assets.font,Ui.Gold);
            Ui.Text("Hammer rate",p,475,412,320,52,"망치 1 / 분",31,c.Assets.font,Ui.Gold);
            var totals=PopupSkin.Panel("Accrued rewards",p,96,502,728,160);
            var gold=Ui.Text("Gold total",totals.transform,30,20,324,110,"0",36,c.Assets.font);
            var hammer=Ui.Text("Forge total",totals.transform,374,20,324,110,"0",36,c.Assets.font);
            Ui.Text("Continuous accrual",p,75,681,770,62,"접속 중에도, 게임을 꺼도 계속 쌓입니다.",25,c.Assets.font);
            var claim=PopupSkin.Button("Claim",p,235,774,450,100,"수집",c.Assets.font,()=>{
                if(RewardState.Current.Claim(c.Main)) c.Toast("쌓인 골드와 망치를 받았습니다.");
            });
            void Refresh()
            {
                var s=RewardState.Current; s.Advance(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                time.text="누적 시간 "+TimeSpan.FromSeconds(s.accruedSeconds).ToString(@"dd\일\ hh\시\ mm\분\ ss\초");
                gold.text="골드 "+s.GoldAvailable.ToString("N0"); hammer.text="망치 "+s.HammersAvailable.ToString("N0");
                claim.interactable=s.GoldAvailable>0||s.HammersAvailable>0;
            }
            p.gameObject.AddComponent<LiveUiRefresh>().RefreshView=Refresh; Refresh();
        }
        static void Pass(ScreenContext c)
        {
            var p=Panel(c,"진행 패스",1540);
            Ui.Text("Pass description",p,65,112,790,90,"5스테이지마다 보상을 받으세요!\n최종 보상: 스테이지 500",29,c.Assets.font);
            var progress=Ui.Text("Best stage",p,65,205,790,54,"",28,c.Assets.font,Ui.Cyan);
            var viewport=Ui.Rect("Pass viewport",p,45,285,830,p.rect.height-405);
            var image=viewport.gameObject.AddComponent<Image>(); image.color=new Color(0,0,0,.12f); image.raycastTarget=true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content=Ui.Rect("Pass content",viewport,0,0,830,100*178);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var claimButtons=new Button[100];
            string[] labels={"스킬소환권","펫소환권","탈것소환권"};
            for(int i=0;i<100;i++)
            {
                int index=i;float y=i*178;
                Ui.Text("Stage milestone "+i,content,295,y,240,40,"스테이지 "+((i+1)*5),25,c.Assets.font,Ui.Gold);
                var left=PopupSkin.IllustratedCard("Hammer reward "+i,content,8,y+42,332,126,c.Assets.worldBackground,new Color(.5f,.65f,.8f));
                Ui.ArtImage("Hammer",left,18,14,86,86,PopupSkin.RewardIcon(0)).preserveAspect=true;
                Ui.Text("Amount",left,112,24,200,65,"망치 100",29,c.Assets.font);
                var right=PopupSkin.IllustratedCard("Ticket reward "+i,content,356,y+42,332,126,c.Assets.worldBackground,new Color(.5f,.65f,.8f));
                Ui.ArtImage("Ticket",right,10,23,70,70,Resources.Load<Sprite>("Moonlit/Skills/SummonTicket-v1")).preserveAspect=true;
                Ui.Text("Amount",right,82,14,236,92,labels[i%3]+"\n10개",24,c.Assets.font);
                claimButtons[i]=PopupSkin.Button("Claim milestone "+i,content,703,y+55,119,96,"받기",c.Assets.font,()=>{
                    if(RewardState.Current.ClaimPass(c.Main,index)) c.Toast("망치 100 · "+labels[index%3]+" 10");
                });
            }
            void Refresh()
            {
                progress.text="최고 클리어: 스테이지 "+c.Main.highestClearedStage;
                for(int i=0;i<100;i++) {
                    bool claimed=RewardState.Current.passClaimed[i];
                    claimButtons[i].interactable=!claimed && c.Main.highestClearedStage>=(i+1)*5;
                    claimButtons[i].GetComponentInChildren<Text>().text=claimed?"완료":"받기";
                }
            }
            p.gameObject.AddComponent<LiveUiRefresh>().RefreshView=Refresh;Refresh();
        }
        static void Wallet(ScreenContext c)
        {
            var p=Panel(c,"재화",960);
            var text=Ui.Text("Wallet balances",p,100,145,720,570,"",31,c.Assets.font,Ui.Ivory,TextAnchor.MiddleLeft);
            void Refresh() {
                var m=c.Main;
                text.text="다이아    "+m.gems.ToString("N0")+"\n\n골드    "+m.gold.ToString("N0")+"\n\n스킬소환권    "+m.skillTickets+"\n\n펫소환권    "+m.petTickets+"\n\n탈것소환권    "+m.mountTickets+"\n\n망치    "+m.ore.ToString("N0");
            }
            p.gameObject.AddComponent<LiveUiRefresh>().RefreshView=Refresh;Refresh();
        }
    }
}
