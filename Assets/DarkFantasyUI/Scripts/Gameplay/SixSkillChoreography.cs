using UnityEngine;

namespace Moonlit.UI
{
    // Motion-local seconds: the same arrival schedule drives visuals and authoritative combat hits.
    public static class SixSkillChoreography
    {
        public const float FoodPulseSeconds=.4f,FoodVanishTime=1.2f,BuffEnd=1.85f;
        public static float[] Arrivals(int tier,int variant)
        {
            if(variant==0)return new[]{FoodVanishTime};
            if(variant==2)return new[]{tier==0?1.02f:.98f};
            int count=tier==0?8:5;var times=new float[count];
            for(int i=0;i<count;i++)times[i]=(tier==0?1.20f:.86f)+(tier==0?.13f:.18f)*i;
            return times;
        }
        public static float Departure(int tier,int index)=>(tier==0?.62f:.2f)+(tier==0?.13f:.18f)*index;
        public static float FoodScale(float seconds)
        {
            if(seconds>=FoodVanishTime)return 0;
            float phase=Mathf.Clamp(seconds,0,FoodVanishTime)/FoodPulseSeconds;
            return .78f+.5f*Mathf.Pow(Mathf.Sin(Mathf.PI*phase),2);
        }
        public static Vector3 Head(Vector3 target)=>target+Vector3.up*.5f;
        public static SkillVisualPose Food(Vector3 source,float seconds)
        {
            float size=FoodScale(seconds);
            return new SkillVisualPose(source+new Vector3(0,1.5f,-1),0,size,size,size>0?1:0);
        }
        static Vector3 Ring(Vector3 source,int index,float seconds)
        {
            float angle=index*Mathf.PI/4+seconds*2.8f;
            float radius=1.25f*Mathf.Clamp01(seconds/.2f);
            return source+new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius+.15f,-1);
        }
        public static SkillVisualPose Bone(Vector3 source,Vector3 target,int index,float seconds)
        {
            float launch=Departure(0,index),arrival=1.2f+.13f*index;
            if(seconds<launch) {
                var p=Ring(source,index,seconds);
                return new SkillVisualPose(p,index*45+seconds*160,.7f,.7f);
            }
            float t=Mathf.Clamp01((seconds-launch)/(arrival-launch)),d=target.x>=source.x?1:-1;
            var a=Ring(source,index,launch);var b=Head(target)+Vector3.back;
            var control=b+new Vector3(-d*.6f,1.7f,0);
            float q=t*t*(3-2*t);
            var position=(1-q)*(1-q)*a+2*(1-q)*q*control+q*q*b;
            // The final quarter rotates the shaft sharply downward: a swing onto the head, not a straight missile.
            float angle=Mathf.Lerp(d*-35,d*-145,SkillChoreography.In(SkillChoreography.Window(t,.65f,1)));
            return new SkillVisualPose(position,angle,.7f,.7f,seconds<=arrival?1:0);
        }
        public static SkillVisualPose Arrow(Vector3 source,Vector3 target,int index,float seconds)
        {
            float launch=Departure(1,index),arrival=.86f+.18f*index;
            float t=Mathf.Clamp01((seconds-launch)/(arrival-launch)),q=t*t*(3-2*t);
            var a=source+new Vector3(0,index*.1f,-1);var b=Head(target)+Vector3.back;
            float h=.65f+index*.12f;
            var p=Vector3.Lerp(a,b,q)+Vector3.up*(4*h*q*(1-q));
            var tangent=(b-a)+Vector3.up*(4*h*(1-2*q));
            float angle=Mathf.Atan2(tangent.y,tangent.x)*Mathf.Rad2Deg;
            return new SkillVisualPose(p,angle,1,1,seconds>=launch&&seconds<=arrival?1:0);
        }
        public static float LobProgress(float normalizedTime)
        {
            float t=Mathf.Clamp01(normalizedTime);
            if(t<=.32f)return .48f*SkillChoreography.Out(t/.32f);
            if(t<=.58f)return Mathf.Lerp(.48f,.54f,(t-.32f)/.26f);
            return .54f+.46f*SkillChoreography.In((t-.58f)/.42f);
        }
        public static SkillVisualPose Rock(Vector3 source,Vector3 target,float seconds)
        {
            float t=SkillChoreography.Window(seconds,.12f,1.02f),q=LobProgress(t);
            var p=Vector3.Lerp(source,Head(target),q)+Vector3.up*(4*2.6f*q*(1-q))+Vector3.back;
            return new SkillVisualPose(p,t*95,1,1,seconds<=1.02f?1:0);
        }
        public static SkillVisualPose Sword(Vector3 source,Vector3 target,float seconds)
        {
            float d=target.x>=source.x?1:-1;
            float t=SkillChoreography.Window(seconds,.32f,.98f),q=SkillChoreography.In(t);
            var a=source+new Vector3(d*.5f,1.45f,0);var b=Head(target);
            var p=Vector3.Lerp(a,b,q)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*1.0f)+Vector3.back;
            float appear=SkillChoreography.Out(SkillChoreography.Window(seconds,0,.22f));
            return new SkillVisualPose(p,d*Mathf.Lerp(55,-105,q),appear,appear,seconds<=.98f?1:0);
        }
    }
}
