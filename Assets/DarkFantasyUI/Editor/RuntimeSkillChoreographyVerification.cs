using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                    if(tier<2) {
                        yield return CaptureEarlySkillContactPhases(screen,camera,height,battle,effects,tier,variant,report,fail);
                        continue;
                    }
                    // Clear the previous preview, including its still-fading contacts, between independent captures.
                    effects.enabled=false;effects.enabled=true;yield return null;
                    battle.PreviewSkill(tier,variant);
                    float duration=SkillChoreography.BuffDuration(tier);
                    float[] phases=variant==0
                        ?new[]{duration*.12f,duration*.38f,duration*.63f,duration*.86f}
                        :new[]{SkillChoreography.AttackLead*.18f,SkillChoreography.AttackLead*.55f,
                            SkillChoreography.AttackLead+.05f,SkillChoreography.LastHit(tier,variant)+.05f};
                    string[] names={"anticipation","flight","impact","final"};
                    float elapsed=0;int frames=0;
                    for(int phase=0;phase<phases.Length;phase++) {
                        while(elapsed<phases[phase]&&frames++<600){yield return null;elapsed+=Time.deltaTime;}
                        // One frame lets child renderers/particle geometry reach the battle camera's render texture.
                        yield return null;elapsed+=Time.deltaTime;
                        Canvas.ForceUpdateCanvases();
                        bool visible=effects.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&r.color.a>.015f&&r.sprite);
                        bool contact=variant==0||phase<2||effects.transform.Find("Skill contact "+tier+" "+variant+" hit "+(phase==2?0:SkillChoreography.HitCount(tier,variant)-1));
                        if(frames>=600||!visible||!contact) {
                            report.Add("FAIL skill choreography "+tier+"/"+variant+"/"+names[phase]+" must show its authored visible phase");fail();
                        }
                        SaveCamera(camera,"Artifacts/Runtime-skill-"+tier+"-"+variant+"-"+names[phase]+"-"+aspect+".png",1080,height);
                    }
                }
                report.Add("PASS skill choreography captures "+aspect+": six early-era skills driven by actual Animator-authorized contacts/healing plus 24 later-era rhythm previews");
            }
            finally {
                effects.enabled=false;effects.enabled=true;
                screen.enabled=screenEnabled;battle.enabled=false;battle.enabled=battleEnabled;
            }
        }
        // Optional fast cloud entry point; the full thirty-skill helper already includes these samples.
        static IEnumerator CaptureSixEarlySkillChoreographies(MainScreen screen,Camera camera,int height,List<string> report,Action fail)
        {
            var battle=screen.GetComponent<BattleRuntime>();
            var effects=battle?CombatCaptureField<PrimitiveSkillEffects>(battle,"effects"):null;
            if(!battle||!effects||!battle.PlayerHud||!battle.EnemyHud) {
                report.Add("FAIL six-skill capture requires the real battle scene");fail();yield break;
            }
            bool screenEnabled=screen.enabled,battleEnabled=battle.enabled;
            try {
                screen.screens.ShowMainPage();screen.enabled=false;battle.StopAllCoroutines();
                for(int tier=0;tier<2;tier++)for(int variant=0;variant<3;variant++)
                    yield return CaptureEarlySkillContactPhases(screen,camera,height,battle,effects,tier,variant,report,fail);
            }
            finally {
                effects.enabled=false;effects.enabled=true;
                screen.enabled=screenEnabled;battle.enabled=false;battle.enabled=battleEnabled;
            }
        }

        // Fresh real actions at explicit motion times cannot skip phases on slow cloud frames.
        static IEnumerator CaptureEarlySkillContactPhases(MainScreen screen,Camera camera,int height,
            BattleRuntime battle,PrimitiveSkillEffects effects,int tier,int variant,List<string> report,Action fail)
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var previousPlayer=battle.PlayerState;var previousEnemy=battle.EnemyState;
            var playerProperty=typeof(BattleRuntime).GetProperty("PlayerState");
            var enemyProperty=typeof(BattleRuntime).GetProperty("EnemyState");
            var animator=battle.PlayerHud.Actor.GetComponent<Animator>();
            var enemyAnimator=battle.EnemyHud.Actor.GetComponent<Animator>();
            float speed=animator.speed,enemySpeed=enemyAnimator.speed;
            string aspect=height==1920?"9x16":"9x19";
            float[] contacts=SkillChoreography.HitTimes(tier,variant);
            float[] phases;
            string[] names;
            if(variant==0) {
                phases=new[]{.20f,.60f,1.00f,SkillChoreography.BuffHealTime(tier)+.04f,1.65f};
                names=new[]{"food-pulse-1","food-pulse-2","food-pulse-3","heal","body-aura"};
            } else if(variant==1) {
                phases=new[]{.20f,.65f,contacts[0]-.08f,contacts[0]+.04f,contacts[contacts.Length/2]+.04f,contacts[contacts.Length-1]+.04f};
                names=new[]{"anticipation","formation","flight","impact","middle-contact","final"};
            } else {
                phases=new[]{.20f,contacts[0]-.18f,contacts[0]+.04f,contacts[0]+.20f};
                names=new[]{"anticipation","flight","impact","final"};
            }
            try {
                for(int phase=0;phase<phases.Length;phase++) {
                    typeof(BattleRuntime).GetMethod("CancelSkillCombo",flags).Invoke(battle,null);
                    battle.StopAllCoroutines();
                    effects.enabled=false;effects.enabled=true;
                    foreach(var actorHud in new[]{battle.PlayerHud,battle.EnemyHud}) {
                        var flash=actorHud.Actor.GetComponent<CombatHitFlash>();flash.enabled=false;flash.enabled=true;
                        actorHud.SetVisible(true);
                    }
                    animator.speed=1;enemyAnimator.speed=1;
                    animator.Play("Idle",0,0);animator.Update(0);
                    enemyAnimator.Play("Idle",0,0);enemyAnimator.Update(0);
                    yield return null;
                    var skill=new CombatSkill {tier=tier,variant=variant,cooldown=variant==0?3:variant==1?2:5,
                        heal=240,attackBoost=48,damage=variant==1?120:288};
                    var actor=new CombatActorState(new CombatStats {health=1000,attack=10},
                        new List<CombatSkill>{skill});actor.Damage(500);
                    var target=new CombatActorState(new CombatStats {health=10000,attack=1});
                    playerProperty.SetValue(battle,actor);enemyProperty.SetValue(battle,target);
                    IEnumerator action=(IEnumerator)(variant==0
                        ?typeof(BattleRuntime).GetMethod("BuffAction",flags).Invoke(battle,new object[]{true,skill})
                        :typeof(BattleRuntime).GetMethod("StrikeTier",flags).Invoke(battle,new object[]{true,skill.damage,true,variant,tier}));
                    if(!action.MoveNext()||!(action.Current is IEnumerator motion)||!motion.MoveNext()) {
                        report.Add("FAIL actual early skill action could not be armed "+tier+"/"+variant);fail();yield break;
                    }
                    float sample=phases[phase],eventTime=variant==0?.3f:SkillChoreography.AttackLead;
                    effects.EarlyPlaybackTimeOverride=sample;
                    animator.Update(0);
                    if(sample<eventTime) animator.Update(sample);
                    else {
                        animator.Update(eventTime+.001f);
                        battle.StopAllCoroutines();
                        var combo=CombatCaptureField<CombatComboSequence>(battle,"activeCombo");
                        if(combo==null) {report.Add("FAIL early skill Animator event did not arm its sequence");fail();yield break;}
                        combo.Advance(sample-eventTime);
                        if(sample>eventTime+.001f) animator.Update(sample-eventTime-.001f);
                    }
                    animator.speed=0;
                    if(variant>0 && sample>=contacts[0]) enemyAnimator.Update(.04f);
                    enemyAnimator.speed=0;
                    battle.PlayerHud.Bind(actor);battle.EnemyHud.Bind(target);
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    int expected=variant==0 ? (sample>=SkillChoreography.BuffHealTime(tier)?1:0) : contacts.Count(t=>t<=sample);
                    var sequence=CombatCaptureField<CombatComboSequence>(battle,"activeCombo");
                    int resolved=sequence==null?0:sequence.ResolvedHits;
                    double expectedHealth=variant==0?10000:10000-skill.damage*expected/contacts.Length;
                    bool visible=effects.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&r.gameObject.activeInHierarchy&&
                        (!(r is SpriteRenderer sprite)||sprite.sprite&&sprite.color.a>.015f));
                    bool contact=variant==0||expected==0||effects.transform.Find("Skill contact "+tier+" "+variant+" hit "+(expected-1));
                    if(resolved!=expected||Math.Abs(target.Health-expectedHealth)>.00001||
                        Math.Abs(actor.Health-(500+(variant==0?240*expected:0)))>.00001||!visible||!contact) {
                        report.Add("FAIL early skill "+tier+"/"+variant+"/"+names[phase]+": actual hits="+resolved+
                            " expected="+expected+" targetHP="+target.Health+" playerHP="+actor.Health+" visible="+visible+" contact="+contact);
                        fail();
                    }
                    SaveCamera(camera,"Artifacts/Runtime-skill-"+tier+"-"+variant+"-"+names[phase]+"-"+aspect+".png",1080,height);
                }
                report.Add("PASS actual early skill "+tier+"/"+variant+" "+aspect+": "+phases.Length+
                    " bounded contact/food samples; unchanged total, delayed event-authorized damage/healing");
            }
            finally {
                typeof(BattleRuntime).GetMethod("CancelSkillCombo",flags).Invoke(battle,null);
                battle.StopAllCoroutines();effects.EarlyPlaybackTimeOverride=-1;
                animator.speed=speed;enemyAnimator.speed=enemySpeed;
                animator.Play("Idle",0,0);animator.Update(0);enemyAnimator.Play("Idle",0,0);enemyAnimator.Update(0);
                playerProperty.SetValue(battle,previousPlayer);enemyProperty.SetValue(battle,previousEnemy);
            }
        }
    }
}
