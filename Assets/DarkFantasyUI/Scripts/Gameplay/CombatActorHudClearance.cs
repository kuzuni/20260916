using System.Collections.Generic;
using UnityEngine;

namespace Moonlit.UI
{
    // Read current SpriteSkin bounds after 2D Animation's order-10 deformation pass.
    // This changes world formation only: never bones, saddle poses, actor scale or camera density.
    [DefaultExecutionOrder(20)]
    public sealed class CombatActorHudClearance : MonoBehaviour
    {
        BattleRuntime battle;
        public void Initialize(BattleRuntime owner) { battle=owner; }
        void LateUpdate() { if(battle && battle.isActiveAndEnabled)battle.ApplyActorHudClearance(); }

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
            // All existing pets and the whole mount stay in view together with the rider.
            float left=float.PositiveInfinity,right=float.NegativeInfinity;
            foreach(var part in formation){left=Mathf.Min(left,part.min.x);right=Mathf.Max(right,part.max.x);}
            if(formation.Count>0)
                delta=player?Mathf.Max(delta,Mathf.Min(0,viewportLeft-left)):
                    Mathf.Min(delta,Mathf.Max(0,viewportRight-right));
            return Mathf.Abs(delta)<.005f?0:delta;
        }
    }
}
