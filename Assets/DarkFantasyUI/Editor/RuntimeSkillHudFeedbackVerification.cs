using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Moonlit.UI;

namespace Moonlit.Editor
{
    public static partial class MainScreenBuilder
    {
        static IEnumerator CaptureSkillHudFeedback(MainScreen screen,Camera camera,int height,List<string> report,Action fail)
        {
            var battle=screen.GetComponent<BattleRuntime>();
            var hud=screen.GetComponentInChildren<EquippedSkillHud>(true);
            if(!battle || !hud || !battle.PlayerHud || !battle.EnemyHud) {
                report.Add("FAIL skill HUD capture requires real battle actors");fail();yield break;
            }
            var collections=CollectionProgression.Data;
            var previousPlayer=battle.PlayerState;var previousEnemy=battle.EnemyState;
            bool screenEnabled=screen.enabled,battleEnabled=battle.enabled,hudEnabled=hud.enabled;
            var playerProperty=typeof(BattleRuntime).GetProperty("PlayerState");
            var enemyProperty=typeof(BattleRuntime).GetProperty("EnemyState");
            var effects=CombatCaptureField<PrimitiveSkillEffects>(battle,"effects");
            float savedEffectClock=effects.PlaybackElapsed,savedEffectOverride=effects.EarlyPlaybackTimeOverride;
            try {
                screen.screens.ShowMainPage();screen.enabled=false;hud.enabled=false;battle.StopAllCoroutines();
                CollectionProgression.Data=CollectionProgression.Create();
                var skills=new List<CombatSkill>();
                for(int i=0;i<3;i++) {
                    var entry=CollectionProgression.Data.categories[0].entries[i];
                    entry.unlocked=true;CollectionProgression.Equip(entry,i);
                    skills.Add(new CombatSkill{tier=0,variant=i,cooldown=entry.Cooldown,
                        heal=entry.FixedHeal,attackBoost=entry.FixedAttackBoost,damage=entry.FixedDamage});
                }
                var actor=new CombatActorState(new CombatStats{health=10000,attack=10,speed=10},skills);
                actor.Damage(100);
                playerProperty.SetValue(battle,actor);
                enemyProperty.SetValue(battle,new CombatActorState(new CombatStats{health=10000,attack=1,speed=1}));
                foreach(var actorHud in new[]{battle.PlayerHud,battle.EnemyHud}) {
                    actorHud.SetVisible(true);
                    var idle=CaptureAuthoredAnimator(actorHud.Actor);idle.Play("Idle",0,0);idle.Update(0);
                }
                hud.Refresh();yield return null;
                actor.BeginTurn();hud.Refresh();
                var indicators=hud.GetComponentsInChildren<SkillHudFeedback>();
                foreach(var indicator in indicators)SeekHudFeedback(indicator,.08f);
                Canvas.ForceUpdateCanvases();
                var first=indicators.First();
                var shade=first.transform.Find("Turn cooldown shade").GetComponent<Image>();
                if(shade.fillAmount<=2f/3f || shade.fillAmount>=1f) {
                    report.Add("FAIL skill cooldown must visibly interpolate from three to two turns");fail();yield break;
                }
                string aspect=height==1920?"9x16":"9x19";
                SaveCamera(camera,"Artifacts/Runtime-skill-hud-cooldown-transition-"+aspect+".png",1080,height);
                // Third player turn plans Buff first; drive its actual Animator event, not a VFX preview or counter write.
                actor.BeginTurn();
                var turn=(IEnumerator)typeof(BattleRuntime).GetMethod("ActorTurn",BindingFlags.Instance|BindingFlags.NonPublic)
                    .Invoke(battle,new object[]{true});
                if(!turn.MoveNext() || !(turn.Current is IEnumerator buff) || !buff.MoveNext() ||
                    !(buff.Current is IEnumerator action) || !action.MoveNext()) {
                    report.Add("FAIL skill HUD capture could not arm the actual buff action");fail();yield break;
                }
                int before=battle.PlayerSkillActivationCount(0);
                var animator=CaptureAuthoredAnimator(battle.PlayerHud.Actor);
                animator.Update(0);
                float eventTime=CaptureAuthoredEventTime(animator,"Buff",1);
                CaptureEffectClock(effects,eventTime);
                animator.Update(eventTime+.001f);
                battle.StopAllCoroutines();
                var sequence=CombatCaptureField<CombatComboSequence>(battle,"activeCombo");
                if(sequence==null || battle.PlayerSkillActivationCount(0)!=before) {
                    report.Add("FAIL food buff must not heal or activate HUD before its three pulses");fail();yield break;
                }
                CaptureEffectClock(effects,SkillChoreography.BuffHealTime(0)+.04f);
                sequence.Advance(SkillChoreography.BuffHealTime(0)-eventTime+.001f);
                if(battle.PlayerSkillActivationCount(0)!=before+1) {
                    report.Add("FAIL skill HUD activation must come from one real Animator-authorized delayed heal");fail();yield break;
                }
                hud.Refresh();
                foreach(var indicator in indicators)SeekHudFeedback(indicator,.1f);
                Canvas.ForceUpdateCanvases();
                var flash=first.transform.Find("Skill activation flash").GetComponent<Image>();
                var icon=first.transform.Find("Skill icon");
                if(flash.color.a<=.1f || icon.localScale.x<=1.1f) {
                    report.Add("FAIL actual skill activation must flash and enlarge its equipped HUD icon");fail();yield break;
                }
                SaveCamera(camera,"Artifacts/Runtime-skill-hud-activation-"+aspect+".png",1080,height);
                report.Add("PASS equipped skill radial/turn transition and actual Animator activation feedback "+height);
            }
            finally {
                foreach(var indicator in hud.GetComponentsInChildren<SkillHudFeedback>(true))
                    DOTween.Kill(indicator);
                var cleanupEffects=CombatCaptureField<PrimitiveSkillEffects>(battle,"effects");
                if(cleanupEffects){CaptureEffectClock(cleanupEffects,savedEffectClock);cleanupEffects.EarlyPlaybackTimeOverride=savedEffectOverride;}
                CollectionProgression.Data=collections;
                playerProperty.SetValue(battle,previousPlayer);enemyProperty.SetValue(battle,previousEnemy);
                screen.enabled=screenEnabled;hud.enabled=hudEnabled;hud.Refresh();
                battle.enabled=false;battle.enabled=battleEnabled;
            }
        }
        static void SeekHudFeedback(SkillHudFeedback feedback,float time)
        {
            var tweens=DOTween.TweensByTarget(feedback,false);
            if(tweens==null)return;
            foreach(var tween in tweens.ToArray()){tween.Pause();tween.GotoWithCallbacks(time);}
        }
    }
}
