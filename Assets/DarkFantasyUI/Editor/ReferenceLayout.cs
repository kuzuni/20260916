using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;
using Moonlit.UI;

namespace Moonlit.Editor
{
    public static partial class MainScreenBuilder
    {
        static Sprite[] LoadSprites(string name) => AssetDatabase.LoadAllAssetsAtPath(Root+"Art/"+name+".png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();

        static void ImportArt()
        {
            foreach(string name in new[]{"MoonlitRuins","ForgeBackdrop-v2","AnvilButton-v2"}) ImportTexture(name,false);
            ImportAtlas("EquipmentIcons-v2",3,true,false);
            ImportAtlas("InterfaceIcons-v2",4,false,false);
            ImportAtlas("InterfaceFrames-v2",2,false,true);
            ImportAtlas("CompanionFrame-v2",1,false,false);
        }
        static TextureImporter ImportTexture(string name,bool multiple)
        {
            string path=Root+"Art/"+name+".png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=multiple ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.maxTextureSize=2048; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.filterMode=FilterMode.Bilinear;
            importer.spritePixelsPerUnit=100; importer.isReadable=multiple;
            importer.SaveAndReimport(); return importer;
        }
        static void ImportAtlas(string name,int columns,bool equipment,bool frameAtlas)
        {
            var importer=ImportTexture(name,true);
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Art/"+name+".png");
            Color32[] pixels=texture.GetPixels32();
            var factory=new SpriteDataProviderFactories(); factory.Init();
            var provider=factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var old=provider.GetSpriteRects();
            var rects=new SpriteRect[columns*columns+(frameAtlas ? 1 : 0)];
            for(int i=0;i<columns*columns;i++) {
                int col=i%columns, row=i/columns;
                float top=(float)row/columns, bottom=(float)(row+1)/columns;
                // The generated equipment sheet has staggered silhouettes. These cuts fall in
                // transparent gutters, preserving the hood hem and dagger tip in full.
                if(equipment) {
                    float split=col==0 ? 790f/1254 : 835f/1254;
                    top=row==0 ? 0 : row==1 ? 450f/1254 : split;
                    bottom=row==0 ? 450f/1254 : row==1 ? split : 1;
                }
                var cell=new RectInt(Mathf.RoundToInt((float)col*texture.width/columns),Mathf.RoundToInt((1-bottom)*texture.height),Mathf.RoundToInt((float)texture.width/columns),Mathf.RoundToInt((bottom-top)*texture.height));
                var bounds=AlphaBounds(pixels,texture.width,texture.height,cell);
                string spriteName=(frameAtlas ? "Frame_" : "Icon_")+i.ToString("D2");
                var previous=old.FirstOrDefault(s=>s.name==spriteName);
                rects[i]=new SpriteRect { name=spriteName,rect=bounds,pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,spriteID=previous!=null ? previous.spriteID : GUID.Generate(),border=frameAtlas || name=="CompanionFrame-v2" ? new Vector4(78,78,78,78) : Vector4.zero };
            }
            if(frameAtlas) {
                var r=rects[2].rect;
                var prior=old.FirstOrDefault(s=>s.name=="Frame_04");
                rects[4]=new SpriteRect { name="Frame_04",rect=new Rect(r.center.x-128,r.center.y-128,256,256),pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,spriteID=prior!=null ? prior.spriteID : GUID.Generate() };
            }
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
            provider.Apply(); importer.isReadable=false; importer.SaveAndReimport();
        }
        static Rect AlphaBounds(Color32[] pixels,int width,int height,RectInt region)
        {
            int left=width,right=-1,bottom=height,top=-1;
            for(int y=Mathf.Max(0,region.yMin);y<Mathf.Min(height,region.yMax);y++)
                for(int x=Mathf.Max(0,region.xMin);x<Mathf.Min(width,region.xMax);x++)
                    if(pixels[y*width+x].a>8) { left=Mathf.Min(left,x); right=Mathf.Max(right,x); bottom=Mathf.Min(bottom,y); top=Mathf.Max(top,y); }
            if(right<left) return new Rect(region.x,region.y,region.width,region.height);
            left=Mathf.Max(region.xMin,left-2); right=Mathf.Min(region.xMax-1,right+2);
            bottom=Mathf.Max(region.yMin,bottom-2); top=Mathf.Min(region.yMax-1,top+2);
            return new Rect(left,bottom,right-left+1,top-bottom+1);
        }

        static void BuildHud(MainScreen main,Transform parent)
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
        }
        static void BuildStage(MainScreen main,Transform parent)
        {
            main.stageButton=Ui.ArtButton("Stage selector",parent,320,146,440,120);
            main.stageText=Ui.Text("Stage title",parent,290,146,500,58,"어려움 4-13",45,font,Color.white);
            Ui.Image("Progress shadow",parent,409,222,256,17,null,new Color(0,.015f,.025f));
            Ui.Image("Progress cyan",parent,409,227,256,7,null,Ui.Cyan);
            for(int i=0;i<3;i++) {
                Ui.Image("Stage node rim",parent,394+i*127,211,38,38,circle,Color.black);
                Ui.Image("Stage node edge",parent,398+i*127,215,30,30,circle,new Color(0,.76f,.98f));
                Ui.Image("Stage node",parent,402+i*127,219,22,22,circle,i==2 ? new Color(.48f,1,1) : new Color(0,.52f,.83f));
            }
            main.eventButton=Ui.ArtButton("Timed event — frameless",parent,29,335,111,142);
            var eventIcon=Ui.Image("Brazier icon",main.eventButton.transform,13,0,85,105,referenceIcons[3]); eventIcon.preserveAspect=true; main.eventButton.targetGraphic=eventIcon; main.eventButton.transition=Selectable.Transition.ColorTint;
            Ui.Text("Event timer",main.eventButton.transform,-10,102,132,39,"5일 3시",29,font);
            main.fairyButton=Ui.ArtButton("Fairy gifts — frameless",parent,934,349,120,130);
            var fairy=Ui.Image("Fairy icon",main.fairyButton.transform,4,-2,114,97,referenceIcons[4]); fairy.preserveAspect=true; main.fairyButton.targetGraphic=fairy; main.fairyButton.transition=Selectable.Transition.ColorTint;
            Ui.Text("Gift timer",main.fairyButton.transform,-14,90,152,38,"47일 21시",27,font);
        }
        static void BuildForgeAndChat(MainScreen main,Transform parent)
        {
            // The anvil itself is the primary forge button. Its background contains no anvil.
            main.forgeButton=Ui.ArtButton("Anvil — forge button",parent,352,1341,384,263,AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/AnvilButton-v2.png"));
            main.forgeLevelButton=Ui.ArtButton("Forge level — management button",parent,665,1434,192,108,panels[0],true,5.7f);
            Ui.Text("Forge level",main.forgeLevelButton.transform,5,10,182,87,"대장간\n레벨 33",28,font,Color.white);
            main.autoButton=Ui.ArtButton("Automatic forging",parent,873,1434,130,108,panels[0],true,5.7f);
            main.autoText=Ui.Text("Auto label",main.autoButton.transform,4,8,122,41,"자동",27,font,Color.white);
            main.autoIcon=Ui.Image("Circular arrows",main.autoButton.transform,43,52,44,40,referenceIcons[11]); main.autoIcon.preserveAspect=true;
            main.autoIcon.rectTransform.pivot=new Vector2(.5f,.5f); main.autoIcon.rectTransform.anchoredPosition+=new Vector2(22,-20);
            Ui.Text("Forge level timer",parent,675,1550,175,31,"1일 7시",21,font);
            Ui.Image("Silver ingot",main.forgeButton.transform,105,193,41,40,referenceIcons[2]).preserveAspect=true;
            main.oreText=Ui.Text("Stone amount",main.forgeButton.transform,150,190,145,45,"40351",31,font,Ui.Ivory,TextAnchor.MiddleLeft);
            var info=Ui.ArtButton("Forge help",parent,110,1347,45,45);
            Ui.Image("Info bronze rim",info.transform,0,0,45,45,circle,Ui.Gold);
            Ui.Image("Info dark center",info.transform,3,3,39,39,circle,new Color(.04f,.035f,.03f));
            Ui.Text("Info letter",info.transform,0,-1,45,45,"i",28,font);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(info.onClick,main.ForgeManagement);
            main.chatButton=Ui.ArtButton("World chat",parent,0,1610,1080,112,panels[1],true,8);
            ((Image)main.chatButton.targetGraphic).fillCenter=false;
            var chatStone=Ui.Image("Chat stone texture",main.chatButton.transform,8,8,1064,96,panels[4],new Color(.53f,.53f,.53f)); chatStone.type=Image.Type.Tiled; chatStone.transform.SetAsFirstSibling();
            var bubble=Ui.Image("Chat bubble",main.chatButton.transform,12,33,57,61,referenceIcons[10]); bubble.preserveAspect=true;
            var badge=Badge(main.chatButton.transform,42,5,44);
            Ui.Text("Unread count",badge.transform,0,-1,44,43,"99",23,font,Color.white);
            Ui.Text("Chat preview",main.chatButton.transform,98,17,958,83,"Tacomaker: gotta be the movement sp\nGuest 41194: Lol",25,font,new Color(.84f,.82f,.76f),TextAnchor.MiddleLeft);
            AddPanelFlourish(main.chatButton.transform,1080,112);
        }
        static void BuildNavigation(MainScreen main,Transform parent)
        {
            var background=Ui.Image("Navigation shared stone panel",parent,0,1722,1080,180,panels[1]); background.type=Image.Type.Sliced; background.pixelsPerUnitMultiplier=6;
            background.fillCenter=false;
            var navStone=Ui.Image("Navigation stone texture",background.transform,10,10,1060,160,panels[4],new Color(.53f,.53f,.53f)); navStone.type=Image.Type.Tiled;
            AddPanelFlourish(background.transform,1080,180);
            main.navigation=new Button[5];
            string[] names={"Equipment","Dungeon","Companions","Quests","Shop"}; int[] glyphs={5,6,7,8,9};
            for(int i=0;i<5;i++) {
                var button=Ui.ArtButton(names[i]+" — icon navigation",background.transform,14+i*215,17,194,146);
                main.navigation[i]=button;
                var icon=Ui.Image("Menu icon",button.transform,37,4,117,118,referenceIcons[glyphs[i]]); icon.preserveAspect=true;
                button.targetGraphic=icon; button.transition=Selectable.Transition.ColorTint;
                var feedback=button.gameObject.AddComponent<ButtonFeedback>(); feedback.artwork=icon.rectTransform;
                if(i<4) Badge(button.transform,145,23,27);
                if(i>0) {
                    Ui.Image("Bronze divider",background.transform,3+i*215,30,2,110,null,new Color(.32f,.25f,.17f));
                    var diamond=Ui.Image("Divider tip",background.transform,0+i*215,28,8,8,null,new Color(.32f,.25f,.17f)); diamond.rectTransform.localRotation=Quaternion.Euler(0,0,45);
                }
            }
        }
        static void AddPanelFlourish(Transform parent,float width,float height)
        {
            var top=Ui.Rect("Upper bronze ornament",parent,width*.5f-32,-18,64,40).gameObject.AddComponent<PanelOrnament>(); top.color=Ui.Gold; top.raycastTarget=false;
            var bottom=Ui.Rect("Lower bronze ornament",parent,width*.5f-32,height-22,64,40).gameObject.AddComponent<PanelOrnament>(); bottom.color=Ui.Gold; bottom.raycastTarget=false;
        }
    }
}
