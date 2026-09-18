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
        const BindingFlags CombatCapturePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        static T CombatCaptureField<T>(BattleRuntime battle, string name)
            => (T)typeof(BattleRuntime).GetField(name, CombatCapturePrivate).GetValue(battle);
        static IEnumerator CombatCaptureRoutine(BattleRuntime battle, string name, params object[] args)
            => (IEnumerator)typeof(BattleRuntime).GetMethod(name, CombatCapturePrivate).Invoke(battle, args);
        static void CombatCaptureState(BattleRuntime battle, string name, CombatActorState state)
            => typeof(BattleRuntime).GetProperty(name).SetValue(battle, state);
        static void RefreshCombatCapture(BattleRuntime battle)
        {
            typeof(BattleRuntime).GetMethod("LateUpdate", CombatCapturePrivate).Invoke(battle, null);
            foreach (var hud in new[] { battle.PlayerHud, battle.EnemyHud })
                typeof(CombatWorldHud).GetMethod("LateUpdate", CombatCapturePrivate).Invoke(hud, null);
            foreach (var shadow in battle.PlayerHud.Actor.parent.GetComponentsInChildren<CombatGroundShadow>())
                typeof(CombatGroundShadow).GetMethod("LateUpdate", CombatCapturePrivate).Invoke(shadow, null);
            CombatCaptureField<Camera>(battle, "renderCamera").Render();
        }

        // Call once inside CaptureGameplay's existing per-height Safe Area / RenderTexture scope.
        static IEnumerator CaptureCombatFeedback(MainScreen screen, Camera camera, int height, List<string> report, Action fail)
        {
            var routine = CaptureCombatFeedbackRoutine(screen, camera, height, report);
            while (true)
            {
                bool moved = false; Exception error = null;
                try { moved = routine.MoveNext(); }
                catch (Exception exception) { error = exception; }
                if (error != null) { report.Add("FAIL combat feedback " + height + ": " + error); fail(); yield break; }
                if (!moved) yield break;
                yield return routine.Current;
            }
        }
        static IEnumerator CaptureCombatFeedbackRoutine(MainScreen screen, Camera camera, int height, List<string> report)
        {
            var battle = screen.GetComponent<BattleRuntime>();
            bool enabled = battle.enabled, mainEnabled = screen.enabled;
            float scale = Time.timeScale;
            var actors = new[] { battle.PlayerHud.Actor, battle.EnemyHud.Actor };
            var animators = actors.Select(actor => actor.GetComponent<Animator>()).ToArray();
            var speeds = animators.Select(animator => animator.speed).ToArray();
            string aspect = height == 1920 ? "9x16" : "9x19";
            try
            {
                screen.enabled = false; battle.StopAllCoroutines();
                var activeEffects = CombatCaptureField<PrimitiveSkillEffects>(battle, "effects");
                activeEffects.enabled = false; activeEffects.enabled = true;
                foreach(var hud in new[]{battle.PlayerHud,battle.EnemyHud}) { hud.gameObject.SetActive(false);hud.gameObject.SetActive(true); }
                CombatCaptureField<CombatAnimationRelay>(battle,"playerRelay").Cancel();
                CombatCaptureField<CombatAnimationRelay>(battle,"enemyRelay").Cancel();
                Time.timeScale = 0;
                for (int i = 0; i < actors.Length; i++)
                {
                    actors[i].localPosition = new Vector3(i == 0 ? -2.5f : 2.5f, 0, 0);
                    animators[i].Play("Idle", 0, 0); animators[i].Update(0); animators[i].speed = 0;
                }
                battle.PlayerHud.SetVisible(true); battle.EnemyHud.SetVisible(true);
                var player = new CombatActorState(new CombatStats { health = 200, attack = 20, speed = 10 },
                    new List<CombatSkill> {
                        new CombatSkill { tier=0, variant=0, cooldown=3, heal=20 },
                        new CombatSkill { tier=0, variant=1, cooldown=2, damage=30 },
                        new CombatSkill { tier=0, variant=2, cooldown=5, damage=50 }
                    });
                CombatCaptureState(battle, "PlayerState", player);
                CombatCaptureState(battle, "EnemyState", new CombatActorState(new CombatStats { health=200 }));
                yield return null;
                RefreshCombatCapture(battle);
                SaveCamera(camera, "Artifacts/Runtime-shadow-feet-idle-" + aspect + ".png", 1080, height);
                for (int side = 0; side < 2; side++)
                {
                    bool isPlayer = side == 0;
                    var victim = isPlayer ? battle.EnemyState : battle.PlayerState;
                    var actor = animators[side]; var victimActor = actors[1-side];
                    var strike = CombatCaptureRoutine(battle, "Strike", isPlayer, 20d, false, 0);
                    if (!strike.MoveNext() || !((IEnumerator)strike.Current).MoveNext())
                        throw new InvalidOperationException("Could not arm real basic action.");
                    actor.speed = 1; actor.Update(0); actor.Update(.31f); actor.speed = 0;
                    var flash = victimActor.GetComponent<CombatHitFlash>();
                    var effects = CombatCaptureField<PrimitiveSkillEffects>(battle, "effects");
                    var fragments = effects.GetComponentsInChildren<ParticleSystem>();
                    if (!flash.IsFlashing || victim.Health != 180 || fragments.Length == 0)
                        throw new InvalidOperationException("Animator event did not produce real damage, white flash and fragments.");
                    yield return null; // Let SpriteSkin update the pose; scaled time stays frozen during the white frame.
                    foreach (var particles in fragments) particles.Simulate(.04f, true, false, true);
                    RefreshCombatCapture(battle);
                    SaveCamera(camera, "Artifacts/Runtime-hit-white-" + (isPlayer ? "enemy" : "player") + "-" + aspect + ".png", 1080, height);
                    foreach (var sprite in victimActor.GetComponentsInChildren<SpriteRenderer>())
                        if (sprite.sharedMaterial.shader.name != "Moonlit/Combat/HitWhite")
                            throw new InvalidOperationException("Victim sprite did not use alpha-preserving white flash.");
                    if (Mathf.Abs(fragments.Last().main.startSize.constant - 2.64f) > .001f)
                        throw new InvalidOperationException("Impact fragments are not three times their previous size.");
                    flash.enabled = false; flash.enabled = true;
                    foreach (var particles in fragments) UnityEngine.Object.DestroyImmediate(particles.gameObject);
                    for (int i=0;i<2;i++) { animators[i].Play("Idle",0,0);animators[i].Update(0); }
                }
                // Advance the actual stage iterator across a defeated wave without replaying the player's entrance.
                var stage = CombatCaptureRoutine(battle, "FightStage", 1, 3, new Action<bool>(_ => {}));
                stage.MoveNext();
                CombatCaptureState(battle, "PlayerState", player);
                player.Damage(40); player.BeginTurn(); player.BeginTurn(); player.SetAttackBoost(7);
                Vector3 home = actors[0].localPosition;
                double health = player.Health;
                stage.MoveNext();
                battle.EnemyState.Damage(double.MaxValue);
                stage.MoveNext();
                screen.Refresh();
                RefreshCombatCapture(battle);
                SaveCamera(camera, "Artifacts/Runtime-wave-continuity-before-" + aspect + ".png", 1080, height);
                stage.MoveNext();
                var entrance = (IEnumerator)stage.Current; entrance.MoveNext();
                if (battle.Wave != 2 || !ReferenceEquals(player, battle.PlayerState) ||
                    player.Health != health || player.Turns != 2 || player.AttackBoost != 7 ||
                    actors[0].localPosition != home || !battle.PlayerHud.WorldCanvas.enabled)
                    throw new InvalidOperationException("Wave transition reset the player health, cooldown phase, buff or position.");
                actors[1].localPosition = new Vector3(2.5f,0,0);
                battle.EnemyHud.SetVisible(true);
                foreach(var animator in animators) { animator.Play("Idle",0,0);animator.Update(0); }
                screen.Refresh();
                RefreshCombatCapture(battle);
                SaveCamera(camera, "Artifacts/Runtime-wave-continuity-after-" + aspect + ".png", 1080, height);
                foreach (var state in new[] { "Basic", "Strong" })
                {
                    foreach(var animator in animators) { animator.speed=1;animator.Play(state,0,0);animator.Update(0);animator.Update(.28f);animator.speed=0; }
                    yield return null;
                    RefreshCombatCapture(battle);
                    foreach (var a in actors)
                    {
                        var feet = a.GetComponentsInChildren<SpriteRenderer>().Where(part => part.sprite &&
                            (part.sprite.name == "다리1" || part.sprite.name == "다리2")).ToArray();
                        var shadow = a.parent.Find(a.name + " ground shadow");
                        if (feet.Length != 2) throw new InvalidOperationException("Expected two imported feet for " + a.name);
                        float expectedY = feet.Min(part => part.bounds.min.y) - (a.Find("Motion").position.y - a.position.y) + .03f;
                        if (feet.Length != 2 || Mathf.Abs(shadow.position.x-feet.Average(part=>part.bounds.center.x))>.02f ||
                            Mathf.Abs(shadow.position.y-expectedY)>.02f)
                            throw new InvalidOperationException("Shadow is detached from the rendered feet during " + state);
                    }
                    SaveCamera(camera, "Artifacts/Runtime-shadow-feet-" + state.ToLowerInvariant() + "-" + aspect + ".png", 1080, height);
                }
                report.Add("PASS real Animator white hits on both victims, 3x sprite fragments, continuous wave HP/turns/position and foot shadows in idle/basic/strong " + height);
            }
            finally
            {
                Time.timeScale = scale;
                for (int i=0;i<animators.Length;i++) if(animators[i]) animators[i].speed=speeds[i];
                foreach(var actor in actors) if(actor) { var flash=actor.GetComponent<CombatHitFlash>();flash.enabled=false;flash.enabled=true; }
                screen.enabled = mainEnabled;
                battle.enabled = false; battle.enabled = enabled;
            }
        }
    }
}
