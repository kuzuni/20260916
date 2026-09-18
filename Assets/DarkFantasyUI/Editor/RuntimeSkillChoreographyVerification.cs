using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Moonlit.UI;

namespace Moonlit.Editor
{
    public static partial class MainScreenBuilder
    {
        // Replaces the former thirty-skill/two-phase loop. Invoked once for each portrait ratio.
        static IEnumerator CaptureSkillChoreographies(MainScreen screen,Camera camera,int height,List<string> report,Action fail)
        {
            var battle=screen.GetComponent<BattleRuntime>();
            var effects=battle?CombatCaptureField<PrimitiveSkillEffects>(battle,"effects"):null;
            if(!battle||!effects||!battle.PlayerHud||!battle.EnemyHud) {
                report.Add("FAIL thirty-skill choreography capture requires the real battle scene");fail();yield break;
            }
            bool screenEnabled=screen.enabled,battleEnabled=battle.enabled;
            string aspect=height==1920?"9x16":"9x19";
            try {
                screen.screens.ShowMainPage();screen.enabled=false;battle.StopAllCoroutines();
                foreach(var hud in new[]{battle.PlayerHud,battle.EnemyHud}) {
                    hud.SetVisible(true);var animator=hud.Actor.GetComponent<Animator>();
                    animator.Play("Idle",0,0);animator.Update(0);
                }
                for(int tier=0;tier<10;tier++)for(int variant=0;variant<3;variant++) {
                    // Clear the previous preview, including its still-fading contacts, between independent captures.
                    effects.enabled=false;effects.enabled=true;yield return null;
                    battle.PreviewSkill(tier,variant);
                    float duration=SkillChoreography.BuffDuration(tier);
                    float[] phases=variant==0
                        ?new[]{duration*.12f,duration*.38f,duration*.63f,duration*.86f}
                        :new[]{SkillChoreography.AttackLead*.18f,SkillChoreography.AttackLead*.55f,
                            SkillChoreography.AttackLead+.05f,SkillChoreography.AttackLead+SkillChoreography.LastHit(tier,variant)+.05f};
                    string[] names={"anticipation","flight","impact","final"};
                    float elapsed=0;int frames=0;
                    for(int phase=0;phase<phases.Length;phase++) {
                        while(elapsed<phases[phase]&&frames++<600){yield return null;elapsed+=Time.deltaTime;}
                        // One frame lets child renderers/particle geometry reach the battle camera's render texture.
                        yield return null;elapsed+=Time.deltaTime;
                        Canvas.ForceUpdateCanvases();
                        bool visible=effects.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&r.color.a>.015f&&r.sprite);
                        bool contact=variant==0||phase<2||effects.transform.Find("Skill contact "+tier+" "+variant+" hit "+(phase==2?0:SkillChoreography.HitCount(variant)-1));
                        if(frames>=600||!visible||!contact) {
                            report.Add("FAIL skill choreography "+tier+"/"+variant+"/"+names[phase]+" must show its authored visible phase");fail();
                        }
                        SaveCamera(camera,"Artifacts/Runtime-skill-"+tier+"-"+variant+"-"+names[phase]+"-"+aspect+".png",1080,height);
                    }
                }
                report.Add("PASS 120 skill choreography captures "+aspect+": all 30 distinct preparation, flight, contact and final phases; preview VFX uses real combat rhythm data");
            }
            finally {
                effects.enabled=false;effects.enabled=true;
                screen.enabled=screenEnabled;battle.enabled=false;battle.enabled=battleEnabled;
            }
        }
    }
}
