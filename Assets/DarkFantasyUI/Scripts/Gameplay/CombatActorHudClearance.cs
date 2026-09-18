using System.Collections.Generic;
using UnityEngine;

namespace Moonlit.UI
{
    // Read current SpriteSkin bounds after 2D Animation's order-10 deformation pass.
    // Runtime diagnostics only: never reanchor actors to compensate for authored animation.
    // The pure interval helpers below remain available for historical geometry tests, not actor placement.
    [DefaultExecutionOrder(20)]
    public sealed class CombatActorHudClearance : MonoBehaviour
    {
        BattleRuntime battle;
        public void Initialize(BattleRuntime owner) { battle=owner; }
        void LateUpdate() { if(battle && battle.isActiveAndEnabled)battle.ApplyActorHudClearance(); }

        // The fixed pass is on the right, so the enemy sometimes needs an inward shift.
        // Subtract each part/UI collision interval from the viewport's legal translation range.
        // Pick the nearest surviving X position, preserving ground, scale and the complete formation.
        public static float SafeShift(IList<Bounds> parts,IList<Bounds> formation,Rect[] areas,
            bool player,float viewportLeft,float viewportRight)
            =>TrySafeShift(parts,formation,areas,player,viewportLeft,viewportRight,
                float.NegativeInfinity,float.PositiveInfinity,out float shift)?shift:0;

        static readonly List<Vector2> intervals=new List<Vector2>();
        public static bool TrySafeShift(IList<Bounds> parts,IList<Bounds> formation,Rect[] areas,
            bool player,float viewportLeft,float viewportRight,float minimumShift,float maximumShift,out float shift)
        {
            shift=0;if(formation.Count==0)return true;
            float left=float.PositiveInfinity,right=float.NegativeInfinity;
            foreach(var part in formation){left=Mathf.Min(left,part.min.x);right=Mathf.Max(right,part.max.x);}
            float minimum=Mathf.Max(viewportLeft-left,minimumShift),maximum=Mathf.Min(viewportRight-right,maximumShift);
            if(minimum>maximum)return false;
            intervals.Clear();intervals.Add(new Vector2(minimum,maximum));
            const float gap=.12f;
            foreach(var part in parts)foreach(var area in areas) {
                if(area.width<=0||area.height<=0||part.max.y<area.yMin-gap||part.min.y>area.yMax+gap)continue;
                float low=area.xMin-gap-part.max.x,high=area.xMax+gap-part.min.x;
                for(int i=intervals.Count-1;i>=0;i--) {
                    var range=intervals[i];
                    if(high<=range.x||low>=range.y)continue;
                    intervals.RemoveAt(i);
                    if(low>=range.x)intervals.Add(new Vector2(range.x,Mathf.Min(range.y,low)));
                    if(high<=range.y)intervals.Add(new Vector2(Mathf.Max(range.x,high),range.y));
                }
                if(intervals.Count==0)return false;
            }
            float best=0,distance=float.PositiveInfinity;
            foreach(var range in intervals) {
                float candidate=Mathf.Clamp(0,range.x,range.y);
                if(Mathf.Abs(candidate)<distance-.001f ||
                    (Mathf.Abs(Mathf.Abs(candidate)-distance)<.001f&&(player?candidate<best:candidate>best))) {
                    best=candidate;distance=Mathf.Abs(candidate);
                }
            }
            shift=Mathf.Abs(best)<.005f?0:best;return true;
        }

        public static float OutwardShift(IList<Bounds> parts, IList<Bounds> formation, Rect[] protectedAreas,
            bool player, float viewportLeft, float viewportRight)
        {
            float delta=0;
            const float gap=.18f;
            // A part moved away from one rectangle can enter another, so solve the short ordered set repeatedly.
            for(int pass=0;pass<protectedAreas.Length+1;pass++)
                foreach(var part in parts)
                    foreach(var area in protectedAreas)
                    {
                        if(area.width<=0 || area.height<=0 || part.max.y<area.yMin-gap || part.min.y>area.yMax+gap)continue;
                        if(part.max.x+delta<=area.xMin-gap || part.min.x+delta>=area.xMax+gap)continue;
                        delta=player?Mathf.Min(delta,area.xMin-gap-part.max.x):
                            Mathf.Max(delta,area.xMax+gap-part.min.x);
                    }
            // Keep the rider and whole mount in view; smaller trailing pets reflow separately.
            float left=float.PositiveInfinity,right=float.NegativeInfinity;
            foreach(var part in formation){left=Mathf.Min(left,part.min.x);right=Mathf.Max(right,part.max.x);}
            if(formation.Count>0)
                delta=player?Mathf.Max(delta,Mathf.Min(0,viewportLeft-left)):
                    Mathf.Min(delta,Mathf.Max(0,viewportRight-right));
            return Mathf.Abs(delta)<.005f?0:delta;
        }
    }
}
