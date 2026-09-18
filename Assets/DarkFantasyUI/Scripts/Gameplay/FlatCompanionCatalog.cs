using System;
using UnityEngine;

namespace Moonlit.UI
{
    // The current companion presentation uses the supplied calm, right-facing whole illustrations.
    // Older separated rigs remain archived assets and are never loaded by this runtime catalog.
    public static class FlatCompanionCatalog
    {
        [Serializable] public sealed class ArtBounds
        {
            public int category,variant;
            public float x,y,width,height,backX=.5f,backY=.72f;
        }
        [Serializable] sealed class BoundsFile { public ArtBounds[] entries; }
        public static string Key(int category,int variant)
            => "Moonlit/Companions/Flat/"+(category==1?"Pet":"Mount")+variant+"-v1";
        public static Sprite Icon(int category,int grade,int variant)
            => grade==0 && category>=1 && category<=2 && variant>=0 && variant<3?Resources.Load<Sprite>(Key(category,variant)):null;
        public static string Name(int category,int grade,int variant)
        {
            if(grade!=0 || category<1 || category>2 || variant<0 || variant>2)return "";
            return category==1?new[]{"검치호 새끼","늑대 새끼","멧돼지 새끼"}[variant]:
                new[]{"멧돼지","매머드","코뿔소"}[variant];
        }
        public static ArtBounds Bounds(int category,int variant)
        {
            var file=Resources.Load<TextAsset>("Moonlit/Companions/Flat/Bounds-v1");
            if(!file)return null;
            var data=JsonUtility.FromJson<BoundsFile>(file.text);
            return Array.Find(data.entries??Array.Empty<ArtBounds>(),x=>x.category==category && x.variant==variant);
        }
        public static FlatCompanionActor Create(int category,int variant,Transform parent,string label)
        {
            var sprite=Icon(category,0,variant);
            if(!sprite)return null;
            var root=new GameObject(label);root.transform.SetParent(parent,false);root.layer=30;
            var actor=root.AddComponent<FlatCompanionActor>();
            actor.Initialize(category,variant,sprite,Bounds(category,variant));
            return actor;
        }
    }
}
