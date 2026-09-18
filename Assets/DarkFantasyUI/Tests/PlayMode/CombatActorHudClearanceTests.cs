using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Moonlit.UI.Tests
{
    public sealed class CombatActorHudClearanceTests
    {
        [Test]
        public void OutwardLayoutIsIdempotentAndNeverChangesVerticalPlacementOrScale()
        {
            var part=new Bounds(new Vector3(-.7f,4,0),new Vector3(2,3,0));
            var areas=new[]{new Rect(-1,3,2,1),new Rect(-1.5f,5,3,.6f)};
            var pieces=new[]{part};
            float first=CombatActorHudClearance.OutwardShift(pieces,pieces,areas,true,-8,8);
            Assert.Less(first,0);
            part.center+=Vector3.right*first;pieces[0]=part;
            for(int frame=0;frame<100;frame++)
                Assert.AreEqual(0,CombatActorHudClearance.OutwardShift(pieces,pieces,areas,true,-8,8),
                    "Repeated frame fitting must not accumulate outward drift.");
            Assert.AreEqual(4,part.center.y);Assert.AreEqual(new Vector3(2,3,0),part.size);
        }

        [Test]
        public void RightPassCanRequireInwardMovementWithoutEnteringCentralHudOrDrifting()
        {
            var head=new Bounds(new Vector3(3.2f,5,0),new Vector3(1.6f,1.5f,0));
            var body=new Bounds(new Vector3(3.4f,3.5f,0),new Vector3(1.4f,1.6f,0));
            var parts=new[]{head,body};
            var areas=new[]{new Rect(-1,4.7f,2,1),new Rect(3.7f,2.5f,1,2)};
            float shift=CombatActorHudClearance.SafeShift(parts,parts,areas,false,-5,5);
            Assert.Less(shift,0,"A right-side pass must move the enemy left, not push it into the screen edge.");
            for(int i=0;i<parts.Length;i++){var item=parts[i];item.center+=Vector3.right*shift;parts[i]=item;}
            for(int repeat=0;repeat<100;repeat++)
                Assert.AreEqual(0,CombatActorHudClearance.SafeShift(parts,parts,areas,false,-5,5),
                    "The chosen safe interval must stay stable across frames.");
            foreach(var item in parts)foreach(var area in areas)
                Assert.IsFalse(item.min.x<area.xMax&&item.max.x>area.xMin&&item.min.y<area.yMax&&item.max.y>area.yMin);
        }

        [Test]
        public void OwnHalfConstraintRejectsTheOtherwiseClearOppositeSide()
        {
            var head=new Bounds(new Vector3(3,4,0),new Vector3(2,2,0));
            var parts=new[]{head};
            var areas=new[]{new Rect(1,2,4,4)};
            Assert.Less(CombatActorHudClearance.SafeShift(parts,parts,areas,false,-6,6),-2);
            Assert.IsFalse(CombatActorHudClearance.TrySafeShift(parts,parts,areas,false,-6,6,-1.5f,
                float.PositiveInfinity,out float shift),"No legal own-half interval must be reported as failure, not an opposite-side fallback.");
            Assert.AreEqual(0,shift);
        }

        [UnityTest]
        public IEnumerator ActualMountedPosesClearCentralAndFixedPassHudAtBothRatiosAndNotchWithoutShrinking()
        {
#if UNITY_EDITOR
            var savedForge=ForgeState.Current;var savedCollections=CollectionProgression.Data;
            var savedRewards=RewardState.Current;bool persistence=MainScreen.PersistenceEnabled;
            float time=Time.timeScale;GameObject root=null;
            try
            {
                MainScreen.PersistenceEnabled=false;Time.timeScale=0;
                ForgeState.Current=new ForgeState();CollectionProgression.Data=CollectionProgression.Create();
                RewardState.Current=new RewardState();
                var assets=AssetDatabase.LoadAssetAtPath<MainScreenAssets>("Assets/DarkFantasyUI/Data/MainScreenAssets.asset");
                Assert.IsNotNull(assets);
                root=new GameObject("Actual mounted HUD layout");root.SetActive(false);
                var main=new RuntimeMainScreenFactory(assets).Create(root.transform);main.enabled=false;
                root.SetActive(true);
                var battle=main.GetComponent<BattleRuntime>();battle.StopAllCoroutines();
                battle.PlayerHud.SetVisible(true);battle.EnemyHud.SetVisible(true);
                var safe=main.GetComponentInParent<PortraitSafeArea>();
                var host=main.GetComponentInParent<UiScreenHost>();
                var player=battle.PlayerHud.Actor;var enemy=battle.EnemyHud.Actor;
                var companions=player.parent.GetComponent<CompanionBattleRuntime>();
                foreach(var entry in CollectionProgression.Data.categories[2].entries.Take(3))entry.unlocked=true;
                for(int index=0;index<3;index++)
                {
                    CollectionProgression.Data.categories[1].entries[index].unlocked=true;
                    CollectionProgression.Equip(CollectionProgression.Data.categories[1].entries[index],index);
                }
                foreach(int skillCount in new[]{0,3})
                foreach(int height in new[]{1920,2280})
                foreach(bool notch in new[]{false,true})
                {
                    CollectionProgression.Data.categories[0].equipped=new[]{-1,-1,-1};
                    for(int slot=0;slot<skillCount;slot++) {
                        var entry=CollectionProgression.Data.categories[0].entries[slot];entry.unlocked=true;
                        CollectionProgression.Equip(entry,slot);
                    }
                    main.GetComponentInChildren<EquippedSkillHud>(true).Refresh();
                    Rect safePixels=notch?new Rect(36,84,1008,height-150):new Rect(0,0,1080,height);
                    safe.SetPreviewMetrics(new Vector2Int(1080,height),safePixels);
                    host.SetPreviewMetrics(new Vector2Int(1080,height),safePixels);
                    foreach(int mount in new[]{0,1,2})
                    {
                        CollectionProgression.Equip(CollectionProgression.Data.categories[2].entries[mount],0);
                        companions.RefreshEquipped();
                        foreach(string state in new[]{"Idle","Basic","Strong"})
                        {
                            // Reset the same authored entrance homes to also test fitting from a fresh encounter.
                            player.localPosition=new Vector3(-2.5f,0,0);enemy.localPosition=new Vector3(2.5f,0,0);
                            foreach(var actor in new[]{player,enemy})
                            {
                                var animator=actor.GetComponent<Animator>();animator.speed=1;
                                animator.Play(state,0,0);animator.Update(0);animator.Update(state=="Idle"?0:.28f);animator.speed=0;
                            }
                            yield return null;yield return null;
                            battle.ApplyActorHudClearance();
                            var areas=(Rect[])typeof(BattleRuntime).GetField("actorProtectedHudAreas",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(battle);
                            foreach(var actor in new[]{player,enemy})
                            {
                                Assert.AreEqual(2,Mathf.Abs(actor.localScale.x));Assert.AreEqual(2,actor.localScale.y);
                                Assert.AreEqual(battle.FormationGroundOffset,actor.localPosition.y,.01f,"Both actors use the same bounded ground offset.");
                                Assert.LessOrEqual(actor.localPosition.y,.01f);
                                Assert.GreaterOrEqual(actor.localPosition.y,-3.01f);
                                foreach(var sprite in actor.GetComponentsInChildren<SpriteRenderer>().Where(x=>x.enabled && x.sprite))
                                foreach(var area in areas)
                                {
                                    var bounds=sprite.bounds;
                                    bool overlaps=area.width>0 && area.height>0 && bounds.min.x<area.xMax-.01f &&
                                        bounds.max.x>area.xMin+.01f && bounds.min.y<area.yMax-.01f && bounds.max.y>area.yMin+.01f;
                                    Assert.IsFalse(overlaps,height+" skills="+skillCount+" notch="+notch+" mount="+mount+" "+state+" "+sprite.name+" overlaps central/fixed reward HUD");
                                }
                            }
                            float centre=player.parent.position.x;
                            float HeadX(Transform actor)=>actor.GetComponentsInChildren<SpriteRenderer>().First(x=>x.enabled&&x.sprite&&x.sprite.name=="머리").bounds.center.x;
                            Assert.LessOrEqual(HeadX(player),centre-1.34f,"Mounted player head must stay on the left.");
                            Assert.GreaterOrEqual(HeadX(enemy),centre+1.34f,"Enemy must not escape the pass by crossing onto the player.");
                            battle.PlayerHud.SendMessage("LateUpdate");battle.EnemyHud.SendMessage("LateUpdate");
                            Assert.Greater(battle.EnemyHud.transform.position.x-battle.PlayerHud.transform.position.x,2.1f,
                                "The two 2.1-world-unit HP bars must not overlap.");
                            Assert.Less(Vector2.Distance(companions.Mount.saddle.position,companions.RiderHip.position),.03f);
                            Vector3 stablePlayer=player.position,stableEnemy=enemy.position;
                            for(int repeat=0;repeat<6;repeat++)battle.ApplyActorHudClearance();
                            Assert.Less(Vector3.Distance(stablePlayer,player.position),.02f);
                            Assert.Less(Vector3.Distance(stableEnemy,enemy.position),.02f);
                            var camera=battle.PlayerHud.WorldCanvas.worldCamera;
                            var design=(RectTransform)main.design;
                            float oldHeight=Mathf.Max(110,design.rect.height-PortraitSafeArea.BottomHeight-495);
                            float density=oldHeight/(2*Mathf.Max(2,oldHeight/200f));
                            var view=(RectTransform)design.Find("Live turn battle");
                            Assert.AreEqual(density,view.rect.height/(2*camera.orthographicSize),.01f,
                                "Extending the transparent viewport must not shrink the requested doubled actors.");

                            foreach(var actor in new[]{player,enemy})
                            foreach(var sprite in actor.GetComponentsInChildren<SpriteRenderer>().Where(x=>x.enabled && x.sprite))
                            {
                                Assert.GreaterOrEqual(camera.WorldToViewportPoint(sprite.bounds.min).x,-.005f);
                                Assert.GreaterOrEqual(camera.WorldToViewportPoint(sprite.bounds.min).y,-.005f);
                                Assert.LessOrEqual(camera.WorldToViewportPoint(sprite.bounds.max).x,1.005f);
                            }
                            foreach(var flat in companions.Pets.Concat(new[]{companions.Mount}))
                            {
                                Assert.GreaterOrEqual(camera.WorldToViewportPoint(flat.VisibleBounds.min).x,-.005f);
                                Assert.GreaterOrEqual(camera.WorldToViewportPoint(flat.VisibleBounds.min).y,-.005f);
                                Assert.LessOrEqual(camera.WorldToViewportPoint(flat.VisibleBounds.max).x,1.005f);
                            }
                        }
                    }
                }
            }
            finally
            {
                if(root)UnityEngine.Object.DestroyImmediate(root);
                Time.timeScale=time;MainScreen.PersistenceEnabled=persistence;
                ForgeState.Current=savedForge;CollectionProgression.Data=savedCollections;RewardState.Current=savedRewards;
            }
#else
            Assert.Fail("This real-prefab regression requires the cloud Editor PlayMode job.");
            yield break;
#endif
        }
    }
}
