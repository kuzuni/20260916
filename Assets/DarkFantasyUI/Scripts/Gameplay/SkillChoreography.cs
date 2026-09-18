using System;
using UnityEngine;

namespace Moonlit.UI
{
    public readonly struct SkillVisualPose
    {
        public readonly Vector3 position;
        public readonly Vector2 scale;
        public readonly float rotation,alpha;
        public SkillVisualPose(Vector3 position,float rotation=0,float x=1,float y=1,float alpha=1)
        {this.position=position;this.rotation=rotation;scale=new Vector2(x,y);this.alpha=alpha;}
    }
    // All times are local skill seconds. These authored rhythms also schedule real combat hits.
    public static class SkillChoreography
    {
        public const float AttackLead=.65f;
        static readonly float[][] Weak={
            new[]{0f,.24f,.63f},new[]{0f,.12f,.34f},new[]{0f,.29f,.41f},new[]{0f,.38f,.91f},new[]{0f,.18f,.57f},
            new[]{0f,.32f,.52f},new[]{0f,.16f,.61f},new[]{0f,.09f,.48f},new[]{0f,.35f,.78f},new[]{0f,.27f,.66f}
        };
        static readonly float[][] Strong={
            new[]{0f,.21f,.49f,.88f,1.38f},new[]{0f,.34f,.49f,.86f,1.25f},new[]{0f,.11f,.37f,.58f,1.03f},
            new[]{0f,.17f,.32f,.73f,1.16f},new[]{0f,.29f,.47f,.94f,1.47f},new[]{0f,.13f,.42f,.61f,1.31f},
            new[]{0f,.25f,.39f,.79f,1.42f},new[]{0f,.08f,.31f,.43f,1.12f},new[]{0f,.36f,.52f,.98f,1.53f},
            new[]{0f,.19f,.46f,.81f,1.61f}
        };
        static readonly float[] BuffSeconds={1.05f,1.35f,1.12f,1.25f,1.46f,1.54f,1.38f,1.22f,1.48f,1.7f};
        static int Tier(int tier)=>Mathf.Clamp(tier,0,9);
        public static float[] HitTimes(int tier,int variant)
        {
            tier=Tier(tier);
            if(tier<=1)return SixSkillChoreography.Arrivals(tier,variant);
            if(variant==0)return new[]{BuffHealTime(tier)};
            var times=(float[])(variant==1?Weak[tier]:Strong[tier]).Clone();
            for(int i=0;i<times.Length;i++)times[i]+=AttackLead;
            return times;
        }
        public static int HitCount(int variant)=>variant==0?1:variant==1?3:5;
        public static int HitCount(int tier,int variant)=>HitTimes(tier,variant).Length;
        public static float BuffHealTime(int tier)=>Tier(tier)<=1?SixSkillChoreography.FoodVanishTime:.3f;
        public static float BuffDuration(int tier)=>Tier(tier)<=1?SixSkillChoreography.BuffEnd:BuffSeconds[Tier(tier)];
        public static float LastHit(int tier,int variant)
        {var hits=HitTimes(tier,variant);return hits[hits.Length-1];}
        public static float Duration(int tier,int variant)=>variant==0?BuffDuration(tier):LastHit(tier,variant)+.55f;
        public static float In(float t)=>Mathf.Clamp01(t)*Mathf.Clamp01(t)*Mathf.Clamp01(t);
        public static float Out(float t)=>1-In(1-t);
        public static float Window(float t,float from,float to)=>Mathf.Clamp01((t-from)/(to-from));
        static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>Vector3.LerpUnclamped(a,b,t);
        static Vector3 Lob(Vector3 a,Vector3 b,float t,float height,float rise=.3f,float fall=.62f)
        {
            var apex=Lerp(a,b,.55f)+Vector3.up*height;
            if(t<rise)return Lerp(a,apex,Out(t/rise));
            if(t<fall)return apex+Vector3.right*((t-rise)*.12f);
            return Lerp(apex,b,In(Window(t,fall,1)));
        }
        public static SkillVisualPose Sample(int tier,int variant,Vector3 source,Vector3 target,float time,int hit=0)
        {
            tier=Tier(tier);float t=Mathf.Clamp01(time),d=target.x>=source.x?1:-1;
            Vector3 a=source,b=target,p;float r=0,x=1,y=1,alpha=1;
            if(tier<=1) {
                if(variant==0)return SixSkillChoreography.Food(source,t*SixSkillChoreography.BuffEnd);
                if(variant==1)return tier==0?SixSkillChoreography.Bone(source,target,hit,t*LastHit(tier,variant)):
                    SixSkillChoreography.Arrow(source,target,hit,t*LastHit(tier,variant));
                return tier==0?SixSkillChoreography.Rock(source,target,t*LastHit(tier,variant)):
                    SixSkillChoreography.Sword(source,target,t*LastHit(tier,variant));
            }
            if(variant==0)return Buff(tier,source,t,d);
            if(hit>0 && tier==0)a=target+new Vector3(-d*(.6f+hit*.16f),.12f,0);
            if(variant==1)switch(tier) {
                case 0:p=Lob(a,b,t,1.25f/(hit+1),.25f,.56f);r=d*(t*230+hit*60);y=1-.18f*In(t);break;
                case 1:float fan=(hit-1)*.5f;p=Lerp(a,b,In(Window(t,.16f,1)))+Vector3.up*(fan*Mathf.Sin(t*Mathf.PI));x=.65f+.65f*Out(t);r=-fan*15;break;
                case 2:p=Lerp(a,b,Out(Window(t,.28f,1)));p.x-=d*Mathf.Sin(Window(t,0,.28f)*Mathf.PI)*.4f;x=t<.28f?.72f:1.35f;y=.82f;r=hit%2==0?-3:3;break;
                case 3:p=Lerp(a,b,In(Window(t,.18f,.94f)))+Vector3.down*1.35f;
                    p.x+=d*Mathf.Sin(t*Mathf.PI*6)*.06f;p.y+=Mathf.Abs(Mathf.Sin(t*Mathf.PI*4))*.08f;r=-d*Mathf.Sin(t*Mathf.PI)*5;x=1.08f;y=1-.1f*Mathf.Sin(t*Mathf.PI);break;
                case 4:p=b+new Vector3(d*(1-t)*(hit-1)*1.2f,4*(1-In(Window(t,.46f,1))),0);y=.3f+1.9f*In(t);x=.55f+.25f*Mathf.Sin(t*Mathf.PI);r=d*8*(1-t);alpha=t<.4f?.35f:1;break;
                case 5:float orbit=(1-t)*1.1f;p=Lerp(a,b,In(t))+new Vector3(Mathf.Cos(t*9+hit)*orbit,Mathf.Sin(t*9+hit)*orbit,0);r=d*(-70+150*Out(t));x=.7f+.6f*In(t);break;
                case 6:float cut=Out(Window(t,.37f,.82f));p=Lerp(a,b,cut)+new Vector3(-d*.5f*Mathf.Sin(t*9),Mathf.Sin(t*9)*.55f*(1-t),0);r=d*(hit%2==0?55:-55);x=t<.37f?.12f:1.35f;y=1.3f;alpha=t<.25f?.15f:1;break;
                case 7:float phase=t<.24f?0:t<.61f?.47f:Out(Window(t,.61f,1));p=Lerp(a,b,phase)+Vector3.up*Mathf.Sin(t*18+hit)*.13f*(1-t);alpha=(t>.21f&&t<.31f)||(t>.53f&&t<.65f)?.08f:1;x=t>.65f?1.5f:.8f;y=.72f;break;
                case 8:p=Lerp(a,b,Out(Window(t,.3f,1)))+Vector3.up*Mathf.Sin(t*Mathf.PI*2.5f+hit*.4f)*(1-t)*1.0f;r=d*(-35+70*Mathf.Sin(t*5));x=.6f+1.05f*Out(t);y=.8f;break;
                default:p=Lob(a,b,t,2.1f,.2f,.6f);r=d*(-52+52*In(t));x=.72f+.45f*In(t);y=1.3f;break;
            }
            else switch(tier) {
                case 0:p=Lob(a,b,t,3.1f/(1+hit*.22f),.22f,.67f);r=d*(Out(t)*135+hit*48);x=1+.18f*In(Window(t,.7f,1));y=1-.2f*In(Window(t,.7f,1));break;
                case 1:a+=new Vector3(-d*.5f,-.8f,0);p=Lob(a,b,t,3.8f,.35f,.72f);r=d*360*Out(t);x=y=.8f+.35f*Out(t);break;
                case 2:p=Lob(a,b,t,.72f,.19f,.47f);p.x+=d*Mathf.Sin(t*21)*(1-t)*.09f;r=d*t*480;x=y=t<.72f?.78f:1+.3f*In(Window(t,.72f,1));break;
                case 3:p=b+new Vector3(d*Mathf.Sin(t*8+hit)*.65f*(1-t),3.6f*(1-In(Window(t,.54f,1))),0);r=d*15*Mathf.Sin(t*7);y=.8f+.5f*In(t);break;
                case 4:p=Lerp(b+new Vector3(-d*3.2f,4,0),b,In(Window(t,.35f,1)));p.y+=Mathf.Sin(t*Mathf.PI)*.3f;r=d*(-38+hit*4);x=1+.4f*In(t);y=.75f;break;
                case 5:float collapse=1-Out(Window(t,.18f,.72f));p=b+new Vector3(Mathf.Cos(t*12+hit)*collapse,Mathf.Sin(t*12+hit)*collapse,0);x=y=t<.72f?1.25f-.85f*Out(t):.4f+1.7f*In(Window(t,.72f,1));r=-t*260;break;
                case 6:p=b+new Vector3(Mathf.Cos(t*16+hit)*Mathf.Pow(1-t,2)*1.5f,Mathf.Sin(t*16+hit)*Mathf.Pow(1-t,2)*1.5f,0);r=t*420;x=1.3f-.95f*Out(t);y=.6f+.9f*Mathf.Sin(t*Mathf.PI);break;
                case 7:float q=hit%2==0?1:-1;p=b+new Vector3(q*(1-Out(Window(t,.55f,1)))*1.35f,Mathf.Sin(t*Mathf.PI*4)*(1-t)*.3f,0);r=q*(90-t*180);alpha=t<.55f?.25f:1;x=y=t<.55f?.55f:1.35f;break;
                case 8:p=b+Vector3.up*(-2.0f+2.0f*Out(Window(t,.28f,1)));p.x+=d*Mathf.Sin(t*10+hit)*(1-t)*.18f;r=d*Mathf.Sin(t*4)*8;y=.45f+1.1f*Out(t);x=.75f;break;
                default:p=b+new Vector3(d*Mathf.Sin(t*15+hit)*(1-t)*.32f,4.2f*(1-In(Window(t,.67f,1))),0);r=d*(hit-2)*6;y=.3f+1.7f*In(t);x=t<.67f?.7f:1.1f;alpha=t<.4f?.25f:1;break;
            }
            return new SkillVisualPose(p+Vector3.back,r,x,y,alpha);
        }
        static SkillVisualPose Buff(int tier,Vector3 source,float t,float d)
        {
            Vector3 p;float x=1,y=1,r=0,alpha=1;
            float settle=Out(t);
            switch(tier) {
                case 0:p=source+new Vector3(d*(.95f-.8f*Out(Window(t,.18f,.7f))),.25f+Mathf.Abs(Mathf.Sin(t*9))*.28f*(1-t),0);r=d*(-22+32*Mathf.Sin(t*7));x=1-.22f*Window(t,.65f,1);y=1+.12f*Mathf.Sin(t*14);break;
                case 1:p=source+Vector3.up*(.2f+1.15f*Out(Window(t,0,.3f))-.35f*In(Window(t,.74f,1)));r=d*22*Mathf.Sin(t*Mathf.PI);x=y=.7f+.35f*settle;break;
                case 2:p=source+new Vector3(d*(.5f-.35f*settle),.65f+Mathf.Sin(t*Mathf.PI)*.4f,0);r=d*(Mathf.Sin(t*24)*12*(1-t)-45*Window(t,.65f,1));x=1+.08f*Mathf.Sin(t*20);break;
                case 3:p=source+Vector3.up*(2.5f*(1-In(Window(t,0,.45f)))+.15f+Mathf.Abs(Mathf.Sin(Window(t,.45f,1)*8))*.3f*(1-t));x=1+.25f*Mathf.Sin(t*Mathf.PI);y=1-.2f*Mathf.Sin(t*Mathf.PI);break;
                case 4:p=source+new Vector3(Mathf.Sin(t*12)*(1-t)*.65f,.45f+Mathf.Cos(t*12)*(1-t)*.5f,0);r=-t*180;x=1.4f-.4f*settle;y=.55f+.45f*settle;break;
                case 5:p=source+new Vector3(Mathf.Cos(t*8)*.75f*(1-t),.2f+1.0f*Out(t),0);r=t*125;x=y=.45f+1.0f*Mathf.Sin(t*Mathf.PI*.8f);break;
                case 6:p=source+new Vector3(d*(t<.45f?-.6f:.6f)*(1-t),.65f,0);r=t<.45f?-25:25;x=t<.45f?1.3f:.75f;y=t<.45f?.65f:1.3f;alpha=Mathf.Abs(t-.45f)<.07f?.1f:1;break;
                case 7:p=source+new Vector3(Mathf.Round(Mathf.Sin(t*17))* .32f*(1-t),.45f+Mathf.Round(Mathf.Cos(t*13))*.2f*(1-t),0);r=Mathf.Floor(t*6)*60;alpha=.45f+.55f*Mathf.Abs(Mathf.Sin(t*19));x=y=.7f+.3f*settle;break;
                case 8:p=source+new Vector3(Mathf.Sin(t*9)*.3f*(1-t),-.65f+1.6f*Out(t),0);r=-25* Mathf.Sin(t*7);x=.55f+.5f*settle;y=1.2f-.2f*settle;break;
                default:p=source+Vector3.up*(2.4f*(1-Out(Window(t,.1f,.5f)))+.65f);r=0;x=1.5f-.6f*settle;y=.7f+.3f*settle;alpha=Window(t,0,.12f);break;
            }
            alpha*=1-In(Window(t,.8f,1));
            return new SkillVisualPose(p+Vector3.back,r,x,y,alpha);
        }
        public static SkillVisualPose BuffAccent(int tier,int piece,float t,Vector3 source)
        {
            tier=Tier(tier);float a=piece*Mathf.PI*.5f,fade=Mathf.Sin(Mathf.Clamp01(t)*Mathf.PI);
            Vector3 p=source;float r=0,x=.25f,y=.25f;
            switch(tier) {
                case 0:p+=new Vector3((piece-1.5f)*.18f,.3f+Out(t)*.65f,0);r=piece*47+t*60;x=y=.12f*(1-t);break;
                case 1:p+=new Vector3(Mathf.Cos(a)*.45f,Out(t)*1.6f-.2f,0);x=.1f;y=.5f*(1-t);break;
                case 2:p+=new Vector3(Mathf.Sin(a+t*8)*.38f,Out(t)*1.1f,0);r=-t*180;x=y=.12f+Mathf.Sin(t*Mathf.PI)*.13f;break;
                case 3:p+=new Vector3(Mathf.Cos(a)*Out(t)*.8f,Mathf.Sin(a)*Out(t)*.5f+.2f,0);r=0;x=.35f*(1-t);y=.15f;break;
                case 4:p+=new Vector3(Mathf.Cos(a)*.85f,Mathf.Sin(a)*.85f+.4f,0);r=-a*Mathf.Rad2Deg;x=.17f;y=.65f*fade;break;
                case 5:p+=new Vector3(Mathf.Cos(a+t*5)*.75f*(1-t),Mathf.Sin(a+t*5)*.75f*(1-t)+Out(t),0);r=t*180;x=y=.3f*fade;break;
                case 6:p+=new Vector3((piece%2==0?-1:1)*.8f*(1-Out(t)),(piece/2)*.65f,0);x=.45f*fade;y=.2f;r=piece%2==0?45:-45;break;
                case 7:p+=new Vector3(Mathf.Round(Mathf.Cos(a+t*9))*.5f,Mathf.Round(Mathf.Sin(a+t*9))*.5f+.4f,0);x=y=.18f;fade*=Mathf.Abs(Mathf.Sin(t*18+piece));break;
                case 8:p+=new Vector3(Mathf.Sin(a+t*7)*.6f*(1-t),-1+Out(t)*2,0);r=t*120;x=.18f;y=.4f*fade;break;
                default:p+=new Vector3((piece-1.5f)*.38f,2.2f*(1-Out(t)),0);x=.14f;y=.8f*fade;r=0;break;
            }
            return new SkillVisualPose(p+Vector3.back*1.1f,r,x,y,fade*.75f);
        }
        // Distinct impact geometry: quantity alone is never the only per-skill difference.
        public static SkillVisualPose Impact(int tier,int variant,int piece,float t,Vector3 point,int hit)
        {
            tier=Tier(tier);t=Mathf.Clamp01(t);float angle=piece*2.399963f+hit*.43f;
            Vector3 p=point;float r=0,x=.4f,y=.4f,alpha=1-Out(t);
            float spread=Out(t),wave=Mathf.Sin(t*Mathf.PI);
            switch(tier*2+(variant==2?1:0)) {
                case 0:p+=new Vector3(Mathf.Cos(angle)*spread*.9f,Mathf.Abs(Mathf.Sin(angle))*wave*.6f-t*.3f,0);r=piece*67+t*210;x=y=.26f*(1-t);break;
                case 1:p+=new Vector3((piece-2)*.48f*spread,-.6f+wave*.3f,0);x=.4f+.65f*spread;y=.18f*(1-t);r=piece*35;break;
                case 2:p+=new Vector3((piece-1)*.2f,-.3f+piece*.3f,0);x=.75f*(1-t);y=.2f;r=piece*12-12;break;
                case 3:p+=new Vector3(Mathf.Cos(angle)*spread*1.3f,wave*(.5f+piece*.13f)-t*.6f,0);r=t*320+piece*72;x=y=.27f*(1-t);break;
                case 4:p+=new Vector3((piece-1)*.34f*spread,Mathf.Sin(angle)*spread*.2f,0);x=1.15f*(1-t);y=.14f;r=piece*10;break;
                case 5:p+=new Vector3(Mathf.Cos(angle)*spread,Mathf.Sin(angle)*spread,0);r=angle*Mathf.Rad2Deg;x=.25f+.7f*spread;y=.24f*(1-t);break;
                case 6:p+=new Vector3((piece-2)*.34f,-1.35f+t*.1f,0);x=.35f*(1-t);y=.1f;r=0;break;
                case 7:p+=new Vector3(Mathf.Cos(angle)*spread*.7f,-.5f+spread*(.4f+piece*.35f),0);x=.4f+.4f*spread;y=.5f+spread*.6f;r=piece*33;break;
                case 8:p+=new Vector3((piece-1)*.32f,1.2f*(1-t),0);x=.15f*(1-t);y=1.6f;r=0;alpha=piece%2==0?1-t:wave;break;
                case 9:p+=new Vector3((piece-2)*.45f*spread,wave*.9f-t*.6f,0);x=.45f;y=.17f*(1-t);r=-35+piece*20;break;
                case 10:p+=new Vector3(Mathf.Cos(angle+t*8)*spread*.75f,Mathf.Sin(angle+t*8)*spread*.75f,0);r=-t*360;x=.55f*(1-t);y=.15f;break;
                case 11:p+=new Vector3(Mathf.Cos(angle)*spread*.2f,Mathf.Sin(angle)*spread*.2f,0);x=y=(.25f+In(t)*2.2f)*(piece%2==0?1:.6f);r=t*70;alpha=1-t;break;
                case 12:p+=new Vector3((piece-1)*.25f,0,0);r=piece%2==0?55:-55;x=1.25f*(1-t);y=.12f;alpha=Window(t,0,.08f)*(1-t);break;
                case 13:p+=new Vector3(Mathf.Cos(angle+t*10)*(1-spread)*1.25f,Mathf.Sin(angle+t*10)*(1-spread)*1.25f,0);r=t*400;x=.45f*(1-t);y=.8f*(1-t);break;
                case 14:p+=new Vector3(Mathf.Round(Mathf.Cos(angle+t*15))*spread*.6f,Mathf.Round(Mathf.Sin(angle+t*15))*spread*.6f,0);r=90*piece;x=.3f;y=.3f;alpha=t<.25f?1:t<.4f?0:1-t;break;
                case 15:p+=new Vector3((piece%2==0?-1:1)*(1-spread)*1.1f,(piece-2)*.17f,0);r=piece*90;x=.65f*wave;y=.18f;alpha=wave;break;
                case 16:p+=new Vector3(Mathf.Cos(angle+t*5)*.65f,Mathf.Sin(angle+t*5)*.65f,0);r=(angle+t*5)*Mathf.Rad2Deg;x=.65f*(1-t);y=.25f;break;
                case 17:p+=new Vector3((piece-2)*.32f,-.8f+spread*1.6f,0);r=Mathf.Sin(t*7)*8;x=.24f*(1-t);y=.55f+spread*.5f;break;
                case 18:p+=new Vector3(Mathf.Cos(angle)*spread*.85f,Mathf.Sin(angle)*spread*.85f,0);r=angle*Mathf.Rad2Deg;x=.8f*(1-t);y=.16f;break;
                default:p+=new Vector3((piece-2)*.3f,1.2f*(1-spread),0);r=piece%2==0?10:-10;x=.2f+wave*.25f;y=1.7f*(1-t);alpha=1-t;break;
            }
            return new SkillVisualPose(p+Vector3.back*1.15f,r,x,y,alpha);
        }
    }
}
