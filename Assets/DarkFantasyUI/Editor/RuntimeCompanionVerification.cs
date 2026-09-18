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
            var routine=CapturePrimitiveCompanionFrames(screen,camera,height,report);
            while(true)
            {
                bool moved=false;Exception error=null;
                try{moved=routine.MoveNext();}catch(Exception exception){error=exception;}
                if(error!=null){report.Add("FAIL primitive rig capture "+height+": "+error);fail();yield break;}
                if(!moved)yield break;yield return routine.Current;
            }
        }
        static IEnumerator CapturePrimitiveCompanionFrames(MainScreen screen,Camera camera,int height,List<string> report)
        {
            var battle=screen.GetComponent<BattleRuntime>();
            var actor=battle.PlayerHud.Actor;
            var system=actor.parent.GetComponent<CompanionBattleRuntime>();
            if(!system || !CompanionRigCatalog.Load())throw new InvalidOperationException("Missing prepared native companion rigs.");
            string saved=JsonUtility.ToJson(CollectionProgression.Data);
            bool enabled=screen.enabled,battleEnabled=battle.enabled;
            float scale=Time.timeScale;
            string aspect=height==1920?"9x16":"9x19";
            var animators=new[]{actor.GetComponent<Animator>(),battle.EnemyHud.Actor.GetComponent<Animator>()};
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
                        animators[0].speed=1;animators[0].Play(state,0,0);animators[0].Update(0);animators[0].Update(state=="Basic"?.24f:0);animators[0].speed=0;
                        yield return null;yield return null;
                        if(!system.Mount || Vector2.Distance(system.Mount.saddle.position,system.RiderHip.position)>.03f)
                            throw new InvalidOperationException("Mounted rider must follow the actual saddle bone.");
                        foreach(var pet in system.Pets)
                            if(pet.transform.position.x>=actor.Find("Motion").position.x)
                                throw new InvalidOperationException("Pet formation must remain behind the player.");
                        RefreshCombatCapture(battle);
                        SaveCamera(camera,"Artifacts/Runtime-companions-mount-"+mount+"-"+state.ToLowerInvariant()+"-"+aspect+".png",1080,height);
                    }
                }
                foreach(var entry in CompanionRigCatalog.Load().entries)
                    yield return CaptureCompanionAssembly(entry,height,aspect);
                report.Add("PASS six separated-part SpriteSkin rigs, three pets behind player, all three saddles follow idle/attack and per-creature shadows "+height);
            }
            finally
            {
                CollectionProgression.Data=JsonUtility.FromJson<CollectionSave>(saved);
                system.RefreshEquipped();Time.timeScale=scale;
                for(int i=0;i<animators.Length;i++)if(animators[i])animators[i].speed=speeds[i];
                screen.enabled=enabled;battle.enabled=false;battle.enabled=battleEnabled;
            }
        }
        static IEnumerator CaptureCompanionAssembly(CompanionRigEntry entry,int height,string aspect)
        {
            var root=new GameObject("Companion assembly capture");
            var target=new RenderTexture(1080,height,24);
            Camera camera=null;
            try
            {
                root.transform.position=new Vector3(20000,20000,0);
                var instance=UnityEngine.Object.Instantiate(entry.prefab,root.transform,false);
                var actor=instance.GetComponent<CompanionActor>();actor.groundY=root.transform.position.y;actor.InitializeShadow(root.transform);
                camera=new GameObject("Companion closeup camera").AddComponent<Camera>();
                camera.transform.SetParent(root.transform,false);camera.orthographic=true;camera.cullingMask=1<<30;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.09f,.1f,.13f);
                camera.targetTexture=target;camera.nearClipPlane=.1f;camera.farClipPlane=30;
                foreach(string state in new[]{"Idle","Walk"})
                {
                    actor.animator.speed=1;actor.animator.Play(state,0,0);actor.animator.Update(0);actor.animator.Update(state=="Walk"?.22f:0);actor.animator.speed=0;
                    yield return null;yield return null;
                    var sprites=instance.GetComponentsInChildren<SpriteRenderer>();
                    if(sprites.Length!=8)throw new InvalidOperationException("Expected eight separate skinned parts in "+entry.displayName);
                    Bounds bounds=sprites[0].bounds;foreach(var sprite in sprites)bounds.Encapsulate(sprite.bounds);
                    camera.aspect=1080f/height;
                    camera.orthographicSize=Mathf.Max(bounds.extents.y+.2f,(bounds.extents.x+.2f)/camera.aspect);
                    camera.transform.position=new Vector3(bounds.center.x,bounds.center.y,-12);
                    camera.Render();
                    SaveCamera(camera,"Artifacts/Runtime-companion-rig-"+entry.category+"-"+entry.variant+"-"+state.ToLowerInvariant()+"-"+aspect+".png",1080,height);
                }
            }
            finally{if(camera)camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
