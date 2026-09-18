using System;
using UnityEngine;

namespace Moonlit.UI
{
    public enum CompanionRigType { Humanoid, Quadruped, Bird, Serpentine, Insect, Aquatic, Floating, BipedMount, QuadrupedMount, Vehicle }
    [Serializable] public sealed class CompanionBoneSpec
    {
        public string name;
        public int parent;
        public Vector2 position;
        public CompanionBoneSpec(string name, int parent, float x, float y) { this.name=name; this.parent=parent; position=new Vector2(x,y); }
    }
    [Serializable] public sealed class CompanionPartSpec
    {
        public string name;
        public int cell, bone, tip=-1, order;
        public float height;
        public Vector2 pivot=new Vector2(.5f,.5f), weightAxis=Vector2.down;
        public CompanionPartSpec(string name,int cell,int bone,float height,int order,Vector2 pivot,int tip=-1)
        { this.name=name;this.cell=cell;this.bone=bone;this.height=height;this.order=order;this.pivot=pivot;this.tip=tip; }
    }
    [Serializable] public sealed class CompanionSkeletonDefinition
    {
        public CompanionRigType type;
        public CompanionBoneSpec[] bones;
        public CompanionPartSpec[] parts;
        public Vector2 saddle;
        public float displayScale=1, shadowWidth=1.2f;
    }
    [Serializable] public sealed class CompanionRigEntry
    {
        public int category, variant;
        public string displayName;
        public CompanionRigType type;
        public Sprite icon;
        public GameObject prefab;
    }
    public sealed class CompanionRigCatalog : ScriptableObject
    {
        public CompanionSkeletonDefinition[] definitions;
        public CompanionRigEntry[] entries;
        public CompanionRigEntry Find(int category,int grade,int variant)
        {
            if(grade!=0 || entries==null)return null; // Other eras have no authored creature art yet.
            foreach(var entry in entries) if(entry.category==category && entry.variant==variant)return entry;
            return null;
        }
        public static CompanionRigCatalog Load() => Resources.Load<CompanionRigCatalog>("Moonlit/Companions/CompanionRigCatalog");
        public static Sprite Icon(int category,int grade,int variant) => Load()?.Find(category,grade,variant)?.icon;
    }
}
