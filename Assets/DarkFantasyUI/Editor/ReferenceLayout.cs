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

    }
}
