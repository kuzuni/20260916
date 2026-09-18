using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moonlit.UI
{
    // Ten reusable anatomical definitions. Only the six primitive entries have illustrated prefabs.
    public static class CompanionRigTemplates
    {
        static readonly Vector2 Center=new Vector2(.5f,.5f), Top=new Vector2(.5f,.92f), Base=new Vector2(.5f,.08f);
        public static CompanionSkeletonDefinition[] All()
        {
            var result=new CompanionSkeletonDefinition[10];
            for(int i=0;i<result.Length;i++) result[i]=Create((CompanionRigType)i);
            return result;
        }
        public static CompanionSkeletonDefinition Create(CompanionRigType type)
        {
            var bones=new List<CompanionBoneSpec>();
            var parts=new List<CompanionPartSpec>();
            Func<string,int,float,float,int> bone=(name,parent,x,y)=>{bones.Add(new CompanionBoneSpec(name,parent,x,y));return bones.Count-1;};
            int root=bone("Root",-1,0,0);
            int body=bone("Body",root,0,.78f);
            bool human=type==CompanionRigType.Humanoid, bird=type==CompanionRigType.Bird;
            bool vehicle=type==CompanionRigType.Vehicle, biped=type==CompanionRigType.BipedMount;
            bool longBody=type==CompanionRigType.Serpentine || type==CompanionRigType.Aquatic;
            float bodyHeight=human?.82f:bird?.6f:vehicle?.58f:.8f;
            parts.Add(new CompanionPartSpec("Body",0,body,bodyHeight,0,Center));
            int head=bone("Head",body,human?.08f:bird?.48f:vehicle?.1f:.64f,human?1.24f:bird?1.2f:vehicle?1.17f:1.1f);
            parts.Add(new CompanionPartSpec("Head",1,head,human?.9f:bird?.76f:vehicle?.55f:.66f,3,Base));
            int tail=bone("Tail",body,human?.04f:bird?-.19f:-.49f,human?1.12f:bird?.59f:.85f);
            int tailTip=bone("TailTip",tail,-.93f,.95f);
            parts.Add(new CompanionPartSpec("Tail",6,tail,human?.18f:bird?.25f:.55f,human?6:-3,human?Center:bird?new Vector2(.93f,.52f):new Vector2(.93f,.1f),human?-1:tailTip){weightAxis=Vector2.left});
            if(type==CompanionRigType.Floating)
            {
                int left=bone("AuraLeft",body,-.55f,.8f),right=bone("AuraRight",body,.55f,.8f);
                parts.Add(new CompanionPartSpec("AuraLeft",2,left,.55f,1,Center));
                parts.Add(new CompanionPartSpec("AuraRight",3,right,.55f,-1,Center));
            }
            else if(longBody)
            {
                int spine=bone("Spine",body,-.35f,.6f);
                int spineEnd=bone("SpineEnd",spine,-.8f,.4f);
                parts.Add(new CompanionPartSpec("Spine",4,spine,.45f,1,new Vector2(.9f,.5f),spineEnd){weightAxis=Vector2.left});
                int fin=bone(type==CompanionRigType.Aquatic?"Fin":"Coil",body,.14f,.6f);
                parts.Add(new CompanionPartSpec("FinOrCoil",5,fin,.48f,2,Top));
            }
            else
            {
                for(int limb=0;limb<4;limb++)
                {
                    bool front=limb<2, near=limb%2==0;
                    string name=vehicle?(front?"FrontWheel":"RearWheel")+(near?"Near":"Far"):
                        bird&&front?"Wing"+(near?"Near":"Far"):(front?"Front":"Hind")+(near?"Near":"Far");
                    float x=human?(near?.23f:-.22f):bird?(front?.12f:.04f):front?.49f:-.4f;
                    if(biped && front)x=.53f;
                    if(!near)x-=.13f;
                    float y=human?(front?1.04f:.55f):bird?(front?.98f:.53f):vehicle?.27f:biped&&front?1.08f:.67f;
                    int joint=bone(name,body,x,y);
                    int tip=bone(name+"Tip",joint,x+(bird&&front?-.55f:0),y-(bird&&front?.12f:vehicle?0:.31f));
                    var part=new CompanionPartSpec(name,limb+2,joint,vehicle?.52f:bird&&front?.72f:human&&front?.57f:biped&&front?.34f:bird?.38f:.68f,
                        near?4:-4,vehicle?Center:Top,vehicle?-1:tip);
                    if(bird&&front){part.pivot=near?new Vector2(.96f,.9f):new Vector2(.07f,.91f);part.weightAxis=near?Vector2.left:Vector2.right;}
                    else if(!vehicle)part.pivot=new Vector2(near?.65f:.35f,.93f);
                    parts.Add(part);
                }
                if(type==CompanionRigType.Insect)
                {
                    int left=bone("MiddleNear",body,.08f,.62f),right=bone("MiddleFar",body,-.05f,.64f);
                    parts.Add(new CompanionPartSpec("MiddleNear",2,left,.62f,3,Top));
                    parts.Add(new CompanionPartSpec("MiddleFar",3,right,.62f,-3,Top));
                }
            }
            int extra=bone("Extra",body,human?0:bird?.06f:.45f,human?.68f:bird?1.1f:vehicle?1.0f:.88f);
            parts.Add(new CompanionPartSpec("Extra",7,extra,human?.15f:vehicle?.22f:.24f,5,Center));
            var definition=new CompanionSkeletonDefinition {
                type=type,bones=bones.ToArray(),parts=parts.ToArray(),saddle=new Vector2(-.06f,vehicle?1.12f:1.19f),
                displayScale=type>=CompanionRigType.BipedMount?1.45f:human?.8f:bird?.72f:.85f,
                shadowWidth=type>=CompanionRigType.BipedMount?2.1f:1.0f
            };
            return definition;
        }
    }
}
