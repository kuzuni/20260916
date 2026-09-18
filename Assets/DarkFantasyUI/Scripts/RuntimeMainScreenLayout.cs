using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    public sealed partial class RuntimeMainScreenFactory
    {
        void BuildHud(MainScreen main,Transform parent)
        {
            main.profileButton=Ui.ArtButton("Player profile",parent,27,24,360,96,panels[1],true,10);
            Ui.Image("Portrait parchment",main.profileButton.transform,7,6,94,86,null,new Color(.58f,.55f,.45f));
            var portraitFrame=Ui.Image("Portrait frame",main.profileButton.transform,0,0,105,100,panels[1]); portraitFrame.type=Image.Type.Sliced; portraitFrame.pixelsPerUnitMultiplier=9;
            Ui.Image("Portrait",main.profileButton.transform,8,5,87,91,icons[0]).preserveAspect=true;
            Ui.Text("Player name",main.profileButton.transform,127,4,223,40,"moonzsanf",27,font,Ui.Ivory,TextAnchor.MiddleLeft);
            Ui.Image("Crossed swords",main.profileButton.transform,127,49,32,34,referenceIcons[12]).preserveAspect=true;
            main.powerText=Ui.Text("Combat power",main.profileButton.transform,171,44,175,45,"65.5b",35,font,new Color(1,.73f,.2f),TextAnchor.MiddleLeft);
            main.goldButton=Ui.ArtButton("Gold wallet",parent,657,45,174,56,panels[1],true,10);
            Ui.Image("Crown coin",main.goldButton.transform,-34,-11,72,74,referenceIcons[0]).preserveAspect=true;
            main.goldText=Ui.Text("Gold amount",main.goldButton.transform,42,-1,118,54,"1.55m",29,font);
            Ui.Text("Add gold",main.goldButton.transform,15,27,41,47,"+",37,font,new Color(.18f,.88f,.1f));
            main.gemButton=Ui.ArtButton("Ruby wallet",parent,898,45,149,56,panels[1],true,10);
            Ui.Image("Diamond ruby",main.gemButton.transform,-39,-14,65,77,referenceIcons[1]).preserveAspect=true;
            main.gemText=Ui.Text("Ruby amount",main.gemButton.transform,46,-1,93,54,"21",30,font);
            Ui.Text("Add ruby",main.gemButton.transform,1,27,41,47,"+",37,font,new Color(.18f,.88f,.1f));
            foreach(var value in new[]{main.powerText,main.goldText,main.gemText}) {
                value.resizeTextForBestFit=true;value.resizeTextMaxSize=value.fontSize;
                value.resizeTextMinSize=Ui.ReadableFontSize(18);
                value.verticalOverflow=VerticalWrapMode.Truncate;
            }
        }
        void BuildStage(MainScreen main,Transform parent)
        {
            main.stageButton=Ui.ArtButton("Stage selector",parent,320,146,440,120);
            main.stageText=Ui.Text("Stage title",parent,290,146,500,58,"스테이지 1",45,font,Color.white);
            var lines=new Image[2];
            for(int i=0;i<2;i++) {
                Ui.Image("Wave line track "+i,parent,413+i*127,222,127,17,null,new Color(.015f,.035f,.04f));
                lines[i]=Ui.Image("Wave line fill "+i,parent,413+i*127,227,127,7,null,Ui.Cyan);
            }
            main.waveNodes=new Image[3];
            var pulseRoots=new RectTransform[3];
            for(int i=0;i<3;i++) {
                var node=Ui.Rect("Wave node "+i,parent,394+i*127,211,38,38);
                node.pivot=new Vector2(.5f,.5f);node.anchoredPosition+=new Vector2(19,-19);
                pulseRoots[i]=node;
                Ui.Image("Stage node rim",node,0,0,38,38,circle,Color.black);
                Ui.Image("Stage node edge",node,4,4,30,30,circle,new Color(0,.76f,.98f));
                main.waveNodes[i]=Ui.Image("Stage node",node,8,8,22,22,circle,new Color(.015f,.04f,.05f));
            }
            main.gameObject.AddComponent<StageWaveProgress>().Initialize(main.waveNodes,lines,pulseRoots);
            main.roundText=Ui.Text("Battle round",parent,290,258,500,48,"라운드 1/15",26,font,Ui.Ivory);
            main.eventButton=Ui.ArtButton("Offline rewards — frameless",parent,29,335,111,142);
            var eventIcon=Ui.ArtImage("Offline reward clock and chest",main.eventButton.transform,5,0,101,100,
                Resources.Load<Sprite>("Moonlit/Main/OfflineRewardIcon-v1"));
            eventIcon.preserveAspect=true; main.eventButton.targetGraphic=eventIcon; main.eventButton.transition=Selectable.Transition.ColorTint;
            Ui.Text("Event timer",main.eventButton.transform,-10,102,132,39,"보상 수집",29,font);
            main.fairyButton=Ui.ArtButton("Progress pass — frameless",parent,934,349,120,130);
            var passIcon=Ui.ArtImage("Progress pass sword and pennant",main.fairyButton.transform,10,-8,100,100,
                Resources.Load<Sprite>("Moonlit/Main/ProgressPassIcon-v1"));
            passIcon.preserveAspect=true; main.fairyButton.targetGraphic=passIcon; main.fairyButton.transition=Selectable.Transition.ColorTint;
            Ui.Text("Gift timer",main.fairyButton.transform,-14,90,152,38,"진행 패스",27,font);
        }
        void BuildForgeAndChat(MainScreen main,Transform parent)
        {
            // The anvil itself is the primary forge button. Its background contains no anvil.
            main.forgeButton=Ui.ArtButton("Anvil — forge button",parent,352,1341,384,263,assets.anvil);
            main.forgeLevelButton=Ui.ArtButton("Forge level — management button",parent,665,1434,192,108,panels[0],true,5.7f);
            Ui.Text("Forge level",main.forgeLevelButton.transform,5,10,182,87,"대장간\n레벨 1",28,font,Color.white);
            main.autoButton=Ui.ArtButton("Automatic forging",parent,873,1434,130,108,panels[0],true,5.7f);
            main.autoText=Ui.Text("Auto label",main.autoButton.transform,4,8,122,41,"자동",27,font,Color.white);
            main.autoIcon=Ui.Image("Circular arrows",main.autoButton.transform,43,52,44,40,referenceIcons[11]); main.autoIcon.preserveAspect=true;
            main.autoIcon.rectTransform.pivot=new Vector2(.5f,.5f); main.autoIcon.rectTransform.anchoredPosition+=new Vector2(22,-20);
            Ui.Text("Forge level timer",parent,675,1550,175,31,"레벨업 관리",21,font);
            Ui.Image("Hammer currency",main.forgeButton.transform,105,193,41,40,RewardsScreenModule.HammerArt).preserveAspect=true;
            main.oreText=Ui.Text("Stone amount",main.forgeButton.transform,150,190,145,45,"1000",31,font,Ui.Ivory,TextAnchor.MiddleLeft);
            var info=Ui.ArtButton("Player details",parent,110,1347,45,45);
            main.playerDetailsButton=info;
            Ui.Image("Info bronze rim",info.transform,0,0,45,45,circle,Ui.Gold);
            Ui.Image("Info dark center",info.transform,3,3,39,39,circle,new Color(.04f,.035f,.03f));
            Ui.Text("Info letter",info.transform,0,-1,45,45,"i",28,font);
            main.chatButton=Ui.ArtButton("World chat",parent,0,1610,1080,112,panels[1],true,8);
            ((Image)main.chatButton.targetGraphic).fillCenter=false;
            var chatStone=Ui.Image("Chat stone texture",main.chatButton.transform,8,8,1064,96,panels[4],new Color(.53f,.53f,.53f)); chatStone.type=Image.Type.Tiled; chatStone.transform.SetAsFirstSibling();
            var bubble=Ui.Image("Chat bubble",main.chatButton.transform,12,33,57,61,referenceIcons[10]); bubble.preserveAspect=true;
            var badge=Badge(main.chatButton.transform,42,5,44);
            Ui.Text("Unread count",badge.transform,0,-1,44,43,"99",23,font,Color.white);
            Ui.Text("Chat preview",main.chatButton.transform,98,17,958,83,"Tacomaker: gotta be the movement sp\nGuest 41194: Lol",25,font,new Color(.84f,.82f,.76f),TextAnchor.MiddleLeft);
            AddPanelFlourish(main.chatButton.transform,1080,112);
        }
        void AddPanelFlourish(Transform parent,float width,float height)
        {
            var top=Ui.Rect("Upper bronze ornament",parent,width*.5f-32,-18,64,40).gameObject.AddComponent<PanelOrnament>(); top.color=Ui.Gold; top.raycastTarget=false;
            var bottom=Ui.Rect("Lower bronze ornament",parent,width*.5f-32,height-22,64,40).gameObject.AddComponent<PanelOrnament>(); bottom.color=Ui.Gold; bottom.raycastTarget=false;
        }
    }
}
