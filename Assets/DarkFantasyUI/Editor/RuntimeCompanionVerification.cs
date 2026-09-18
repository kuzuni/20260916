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
        static IEnumerator CapturePrimitiveCompanions(MainScreen screen,Camera camera,int height,List<string> report,Action fail)
        {
            var routines=new Stack<IEnumerator>();
            routines.Push(CapturePrimitiveCompanionFrames(screen,camera,height,report));
            try
            {
                while(routines.Count>0)
                {
                    var routine=routines.Peek();
                    bool moved=false;Exception error=null;
                    try{moved=routine.MoveNext();}catch(Exception exception){error=exception;}
                    if(error!=null){report.Add("FAIL whole-PNG companion capture "+height+": "+error);fail();yield break;}
                    if(!moved){(routine as IDisposable)?.Dispose();routines.Pop();continue;}
                    if(routine.Current is IEnumerator nested){routines.Push(nested);continue;}
                    yield return routine.Current;
                }
            }
            finally{while(routines.Count>0)(routines.Pop() as IDisposable)?.Dispose();}
        }
        static IEnumerator CapturePrimitiveCompanionFrames(MainScreen screen,Camera camera,int height,List<string> report)
        {
            var battle=screen.GetComponent<BattleRuntime>();
            var actor=battle.PlayerHud.Actor;
            var system=actor.parent.GetComponent<CompanionBattleRuntime>();
            if(!system)throw new InvalidOperationException("Missing whole-PNG companion runtime.");
            string saved=JsonUtility.ToJson(CollectionProgression.Data);
            bool enabled=screen.enabled,battleEnabled=battle.enabled;
            float scale=Time.timeScale;
            string aspect=height==1920?"9x16":"9x19";
            var animators=new[]{CaptureAuthoredAnimator(actor),CaptureAuthoredAnimator(battle.EnemyHud.Actor)};
            var speeds=animators.Select(item=>item.speed).ToArray();
            try
            {
                screen.enabled=false;battle.StopAllCoroutines();Time.timeScale=0;
                CombatCaptureField<CombatAnimationRelay>(battle,"playerRelay").Cancel();
                CombatCaptureField<CombatAnimationRelay>(battle,"enemyRelay").Cancel();
                actor.localPosition=new Vector3(-2.5f,0,0);battle.EnemyHud.Actor.localPosition=new Vector3(2.5f,0,0);
                for(int category=1;category<=2;category++)
                {
                    CollectionProgression.Data.categories[category].equipped=new[]{-1,-1,-1};
                    for(int variant=0;variant<3;variant++)CollectionProgression.Data.categories[category].entries[variant].unlocked=true;
                }
                for(int variant=0;variant<3;variant++)CollectionProgression.Equip(CollectionProgression.Data.categories[1].entries[variant],variant);
                foreach(var animator in animators){animator.Play("Idle",0,0);animator.Update(0);animator.speed=0;}
                system.RefreshEquipped();yield return null;yield return null;
                if(system.Pets.Count!=3)throw new InvalidOperationException("Three equipped primitive pets must be instantiated.");
                battle.PlayerHud.SetVisible(true);battle.EnemyHud.SetVisible(true);
                RefreshCombatCapture(battle);
                SaveCamera(camera,"Artifacts/Runtime-companions-three-pets-"+aspect+".png",1080,height);
                for(int mount=0;mount<3;mount++)
                {
                    CollectionProgression.Equip(CollectionProgression.Data.categories[2].entries[mount],0);system.RefreshEquipped();
                    foreach(string state in new[]{"Idle","Basic"})
                    {
                        animators[0].speed=1;animators[0].Play(state,0,0);animators[0].Update(0);animators[0].Update(state=="Basic"?CaptureAuthoredPoseTime(animators[0],state):0);animators[0].speed=0;
                        yield return null;yield return null;
                        if(!system.Mount || Vector2.Distance(system.Mount.saddle.position,system.RiderHip.position)>.03f)
                            throw new InvalidOperationException("Mounted rider must follow the actual saddle bone.");
                        foreach(var pet in system.Pets)
                            if(pet.transform.position.x>=actor.Find("Motion").position.x)
                                throw new InvalidOperationException("Pet formation must remain behind the player.");
                        RefreshCombatCapture(battle);
                        VerifyCompanionHudClearance(battle,system,height+" "+mount+" "+state);
                        SaveCamera(camera,"Artifacts/Runtime-companions-mount-"+mount+"-"+state.ToLowerInvariant()+"-"+aspect+".png",1080,height);
                        if(state=="Basic")
                        {
                            var safe=screen.GetComponentInParent<PortraitSafeArea>();
                            var host=screen.GetComponentInParent<UiScreenHost>();
                            var previousPixels=safe.ScreenPixels;var previousSafe=safe.SafePixels;
                            try {
                                var inset=new Rect(36,84,1008,height-150);
                                safe.SetPreviewMetrics(new Vector2Int(1080,height),inset);
                                host.SetPreviewMetrics(new Vector2Int(1080,height),inset);
                                yield return null;yield return null;
                                RefreshCombatCapture(battle);
                                VerifyCompanionHudClearance(battle,system,height+" notch "+mount);
                                SaveCamera(camera,"Artifacts/Runtime-companions-mount-"+mount+"-basic-notch-"+aspect+".png",1080,height);
                            } finally {
                                safe.SetPreviewMetrics(previousPixels,previousSafe);
                                host.SetPreviewMetrics(previousPixels,previousSafe);
                            }
                        }
                    }
                }
                for(int category=1;category<=2;category++)
                    for(int variant=0;variant<3;variant++)
                        yield return CaptureFlatCompanionAssembly(category,variant,height,aspect);
                report.Add("PASS six right-facing static whole PNGs, three pets, simple mounted back anchors, no live companion Animator/SpriteSkin and clear stage and fixed reward/pass HUD "+height);
            }
            finally
            {
                CollectionProgression.Data=JsonUtility.FromJson<CollectionSave>(saved);
                system.RefreshEquipped();Time.timeScale=scale;
                for(int i=0;i<animators.Length;i++)if(animators[i])animators[i].speed=speeds[i];
                screen.enabled=enabled;battle.enabled=false;battle.enabled=battleEnabled;
            }
        }
        static void VerifyCompanionHudClearance(BattleRuntime battle,CompanionBattleRuntime companions,string context)
        {
            battle.ApplyActorHudClearance();
            var areas=CombatCaptureField<Rect[]>(battle,"actorProtectedHudAreas");
            var actors=new[]{battle.PlayerHud.Actor,battle.EnemyHud.Actor};
            foreach(var actor in actors)
            {
                if(Mathf.Abs(Mathf.Abs(actor.localScale.x)-2)>.001f || Mathf.Abs(actor.localScale.y-2)>.001f ||
                    Mathf.Abs(actor.localPosition.y-battle.FormationGroundOffset)>.01f || actor.localPosition.y>0.01f || actor.localPosition.y< -3.01f)throw new InvalidOperationException("Actor size/ground changed: "+context);
                foreach(var sprite in actor.GetComponentsInChildren<SpriteRenderer>().Where(x=>x.enabled && x.sprite))
                foreach(var area in areas)
                {
                    var bounds=sprite.bounds;
                    if(area.width>0 && area.height>0 && bounds.min.x<area.xMax-.01f && bounds.max.x>area.xMin+.01f &&
                        bounds.min.y<area.yMax-.01f && bounds.max.y>area.yMin+.01f)
                        throw new InvalidOperationException("Actual actor part covers stage/wave/round/pass/rewards: "+context+" "+sprite.name);
                }
            }
            float centre=actors[0].parent.position.x;
            float HeadX(Transform actor)=>actor.GetComponentsInChildren<SpriteRenderer>().First(x=>x.enabled&&x.sprite&&x.sprite.name=="머리").bounds.center.x;
            if(HeadX(actors[0])>centre-1.34f||HeadX(actors[1])<centre+1.34f)
                throw new InvalidOperationException("Actor crossed into the opposing head/HP lane: "+context);
            battle.PlayerHud.SendMessage("LateUpdate");battle.EnemyHud.SendMessage("LateUpdate");
            if(battle.EnemyHud.transform.position.x-battle.PlayerHud.transform.position.x<=2.1f)
                throw new InvalidOperationException("Mounted actor world HP bars overlap: "+context);
            var mountBounds=companions.Mount.VisibleBounds;
            foreach(var area in areas)
                if(area.width>0&&area.height>0&&mountBounds.min.x<area.xMax-.01f&&mountBounds.max.x>area.xMin+.01f&&
                    mountBounds.min.y<area.yMax-.01f&&mountBounds.max.y>area.yMin+.01f)
                    throw new InvalidOperationException("Mounted artwork covers fixed HUD: "+context);
            if(Vector2.Distance(companions.Mount.saddle.position,companions.RiderHip.position)>.03f)
                throw new InvalidOperationException("World layout detached rider from unchanged saddle: "+context);
            var camera=battle.PlayerHud.WorldCanvas.worldCamera;
            foreach(var actor in actors)
            foreach(var sprite in actor.GetComponentsInChildren<SpriteRenderer>().Where(x=>x.enabled && x.sprite))
                if(camera.WorldToViewportPoint(sprite.bounds.min).x<-.005f || camera.WorldToViewportPoint(sprite.bounds.min).y<-.005f ||
                    camera.WorldToViewportPoint(sprite.bounds.max).x>1.005f)
                    throw new InvalidOperationException("World layout clipped an actor: "+context+" "+sprite.name);
            foreach(var flat in companions.Pets.Concat(new[]{companions.Mount}))
            {
                var bounds=flat.VisibleBounds;
                if(camera.WorldToViewportPoint(bounds.min).x<-.005f || camera.WorldToViewportPoint(bounds.min).y<-.005f || camera.WorldToViewportPoint(bounds.max).x>1.005f)
                    throw new InvalidOperationException("World layout clipped whole companion artwork: "+context+" "+flat.name);
                if(flat.GetComponentsInChildren<SpriteRenderer>().Length!=1 || flat.GetComponentsInChildren<Animator>().Length!=0 ||
                    flat.GetComponentsInChildren<UnityEngine.U2D.Animation.SpriteSkin>().Length!=0 ||
                    flat.Illustration.flipX || flat.Illustration.transform.localScale.x<=0)
                    throw new InvalidOperationException("Companion must remain one unmirrored, unrigged illustration: "+flat.name);
            }
            camera.Render();
        }
        static IEnumerator CaptureFlatCompanionAssembly(int category,int variant,int height,string aspect)
        {
            var root=new GameObject("Whole PNG companion capture");
            var target=new RenderTexture(1080,height,24);
            Camera camera=null;
            try
            {
                root.transform.position=new Vector3(20000,20000,0);
                var actor=FlatCompanionCatalog.Create(category,variant,root.transform,"Flat companion "+category+" "+variant);
                if(!actor || FlatCompanionCatalog.Bounds(category,variant)==null)
                    throw new InvalidOperationException("Missing whole artwork/opaque bounds "+category+" "+variant);
                Vector3 position=actor.Illustration.transform.localPosition;
                Quaternion rotation=actor.Illustration.transform.localRotation;
                Vector3 scale=actor.Illustration.transform.localScale;
                camera=new GameObject("Whole companion closeup camera").AddComponent<Camera>();
                camera.transform.SetParent(root.transform,false);camera.orthographic=true;camera.cullingMask=1<<30;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.09f,.1f,.13f);
                camera.targetTexture=target;camera.nearClipPlane=.1f;camera.farClipPlane=30;
                yield return null;yield return null;
                if(actor.Illustration.transform.localPosition!=position || actor.Illustration.transform.localRotation!=rotation ||
                    actor.Illustration.transform.localScale!=scale)
                    throw new InvalidOperationException("Static whole illustration changed its authored pose.");
                var bounds=actor.VisibleBounds;
                camera.aspect=1080f/height;
                camera.orthographicSize=Mathf.Max(bounds.extents.y+.2f,(bounds.extents.x+.2f)/camera.aspect);
                camera.transform.position=new Vector3(bounds.center.x,bounds.center.y,-12);
                camera.Render();
                SaveCamera(camera,"Artifacts/Runtime-companion-flat-"+category+"-"+variant+"-"+aspect+".png",1080,height);
            }
            finally{if(camera)camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
