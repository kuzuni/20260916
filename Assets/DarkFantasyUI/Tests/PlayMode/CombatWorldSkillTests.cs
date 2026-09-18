using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Moonlit.UI.Tests
{
    public sealed class CombatWorldSkillTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [Test]
        public void AllThirtySkillIllustrationsAreDistinctImportedSprites()
        {
            var seen = new System.Collections.Generic.HashSet<Sprite>();
            for (int tier = 0; tier < 10; tier++)
                for (int variant = 0; variant < 3; variant++)
                {
                    var sprite = PrimitiveSkillEffects.SkillSprite(tier, variant);
                    Assert.IsNotNull(sprite, PrimitiveSkillEffects.ResourceKey(tier, variant));
                    Assert.IsTrue(seen.Add(sprite), "Every era/variant must have its own illustration.");
                    Assert.Greater(sprite.rect.width, 16); Assert.Greater(sprite.rect.height, 16);
                }
            Assert.AreEqual(30,seen.Count);
        }

        [UnityTest]
        public IEnumerator WorldHealthFollowsVictimAndDamageFloatsBelongToThatVictim()
        {
            var root = new GameObject("World combat HUD fixture",typeof(RectTransform));
            var assets = ScriptableObject.CreateInstance<MainScreenAssets>();
            try
            {
                var main = root.AddComponent<MainScreen>(); main.enabled = false; main.design = root.transform;
                ((RectTransform)root.transform).sizeDelta = new Vector2(1080,1920);
                assets.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); main.font = assets.font;
                var battle = root.AddComponent<BattleRuntime>(); battle.Initialize(main,assets); battle.StopAllCoroutines();
                var actor = battle.PlayerHud.Actor;
                var animator = AuthoredAnimationTestSupport.ActorAnimator(actor);
                float animationSpeed = animator.speed;
                animator.Play("Idle",0,0); animator.Update(0); animator.speed=0;
                // Creation has only queued Idle. Settle its SpriteSkin and head HUD before measuring translation.
                yield return null; yield return null;
                Vector3 before = battle.PlayerHud.transform.position;
                actor.position += Vector3.right * .75f;
                yield return null; yield return null;
                Assert.AreEqual(.75f,battle.PlayerHud.transform.position.x-before.x,.02f);
                Assert.AreEqual(RenderMode.WorldSpace,battle.PlayerHud.WorldCanvas.renderMode);
                Assert.IsNull(root.transform.Find("Live turn battle/Battle round"));
                Assert.IsNull(root.transform.Find("Live turn battle/Last combat action"));

                typeof(BattleRuntime).GetProperty("PlayerState").SetValue(battle,new CombatActorState(new CombatStats { health=100,attack=20 }));
                var victim = new CombatActorState(new CombatStats { health=100 });
                typeof(BattleRuntime).GetProperty("EnemyState").SetValue(battle,victim);
                animator.speed=animationSpeed; // The actual event-driven attack must no longer be frozen.
                var attack = (IEnumerator)typeof(BattleRuntime).GetMethod("Strike",Private).Invoke(battle,new object[] {true,20d,false,0});
                Assert.IsTrue(attack.MoveNext());
                var motion = (IEnumerator)attack.Current; Assert.IsTrue(motion.MoveNext());
                animator.Update(0); animator.Update(AuthoredAnimationTestSupport.ImpactTime(animator,"Basic",0)+.01f);
                Assert.AreEqual(80,victim.Health);
                var stage = battle.EnemyHud.transform.parent;
                var number = stage.Find("Enemy damage number");
                Assert.IsNotNull(number,"A hit creates a world number for the target.");
                Assert.IsNull(stage.Find("Player damage number"),"Damage must not appear above its attacker.");
                Assert.AreEqual(RenderMode.WorldSpace,number.GetComponent<Canvas>().renderMode);
                Assert.AreEqual(30,number.gameObject.layer);
                var damageLabel = number.GetComponentInChildren<Text>();
                StringAssert.Contains("20",damageLabel.text);
                Assert.AreEqual(Color.white,damageLabel.color,"Ordinary damage must be white.");
                Assert.AreEqual(117,damageLabel.fontSize,"Three times the former 39-point amount.");
                Assert.AreEqual(new Vector2(6,-6),damageLabel.GetComponent<Outline>().effectDistance);
                Assert.IsNotNull(damageLabel.GetComponents<Shadow>().FirstOrDefault(effect => !(effect is Outline)));
                Assert.Greater(number.position.y,battle.EnemyHud.transform.position.y,"Large amounts float above the HP bar.");
                Assert.AreEqual(battle.EnemyHud.transform.position.x,number.position.x,.05f);
                stage.gameObject.SetActive(false);
                yield return null;
                Assert.IsTrue(!number,"Disabling combat must clear transient numbers even when their coroutine stops early.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(assets); }
        }

        [UnityTest]
        public IEnumerator EntranceHidesHeadBarsUntilHomeThenBarsStayCenteredOnEachLungingHead()
        {
            var root=new GameObject("Entrance HUD fixture",typeof(RectTransform));
            var assets=ScriptableObject.CreateInstance<MainScreenAssets>();
            try
            {
                var main=root.AddComponent<MainScreen>();main.enabled=false;main.design=root.transform;
                ((RectTransform)root.transform).sizeDelta=new Vector2(1080,1920);
                assets.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");main.font=assets.font;
                var battle=root.AddComponent<BattleRuntime>();battle.Initialize(main,assets);battle.StopAllCoroutines();
                Assert.IsFalse(battle.PlayerHud.WorldCanvas.enabled);
                Assert.IsFalse(battle.EnemyHud.WorldCanvas.enabled);
                var entrance=(IEnumerator)typeof(BattleRuntime).GetMethod("Entrance",Private).Invoke(battle,null);
                Assert.IsTrue(entrance.MoveNext());
                Assert.IsFalse(battle.PlayerHud.WorldCanvas.enabled,"Moving past the reward buttons must not draw HP over them.");
                yield return battle.StartCoroutine(entrance);
                Assert.IsTrue(battle.PlayerHud.WorldCanvas.enabled);Assert.IsTrue(battle.EnemyHud.WorldCanvas.enabled);
                Assert.AreEqual(-2.5f,battle.PlayerHud.Actor.localPosition.x,.001f);
                Assert.AreEqual(2.5f,battle.EnemyHud.Actor.localPosition.x,.001f);
                battle.PlayerHud.Bind(new CombatActorState(new CombatStats { health=64172007 }));
                Assert.AreEqual("64.17m/64.17m",battle.PlayerHud.HealthText.text);
                var fill=(RectTransform)battle.PlayerHud.transform.Find("Health fill");
                Assert.AreEqual(48,fill.rect.height,"Health bars are four times the prior 12-unit height.");
                Assert.AreEqual(fill.anchoredPosition.y,battle.PlayerHud.HealthText.rectTransform.anchoredPosition.y,.001f);
                Assert.AreEqual(52,battle.PlayerHud.HealthText.fontSize);
                Assert.Greater(battle.PlayerHud.HealthText.transform.GetSiblingIndex(),fill.GetSiblingIndex(),"Text renders over the fill.");
                var animator=(Animator)typeof(BattleRuntime).GetField("playerAnimator",Private).GetValue(battle);
                animator.Play("Basic",0,0);animator.Update(0);animator.Update(.36f);animator.speed=0;
                yield return null;
                typeof(CombatWorldHud).GetMethod("LateUpdate",Private).Invoke(battle.PlayerHud,null);
                typeof(CombatWorldHud).GetMethod("LateUpdate",Private).Invoke(battle.EnemyHud,null);
                foreach(var hud in new[]{battle.PlayerHud,battle.EnemyHud})
                {
                    var head=hud.Actor.GetComponentsInChildren<SpriteRenderer>().First(r=>r.sprite&&r.sprite.name=="머리");
                    Assert.AreEqual(head.bounds.center.x,hud.transform.position.x,.002f,
                        "Melee HP follows its own head without an outward separation offset.");
                    Assert.AreEqual(head.bounds.max.y+.5f,hud.transform.position.y,.002f);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(assets); }
        }

        [UnityTest]
        public IEnumerator BuffPreviewFollowsTheMovingSourceInsteadOfStayingAtEntranceSpawn()
        {
            var root=new GameObject("Moving buff preview fixture");
            var actor=new GameObject("Preview actor");
            try
            {
                var effects=root.AddComponent<PrimitiveSkillEffects>();
                effects.Initialize(Resources.Load<BattleAssetCatalog>("Moonlit/Combat/BattleAssets"));
                effects.Play(0,0,Vector3.zero,Vector3.right*5,actor.transform,null);
                var sprite=root.GetComponentInChildren<SpriteRenderer>();
                Assert.IsNotNull(sprite);Assert.Less(sprite.transform.position.x,1);
                actor.transform.position=Vector3.right*3;
                yield return null;
                Assert.AreEqual(3,sprite.transform.position.x,.001f,"The overhead food must travel with the actor walking in.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(actor); }
        }

        [UnityTest]
        public IEnumerator SingleRockImpactUsesActualTextureMeshFragmentsInsteadOfWholeRockClones()
        {
            var root=new GameObject("Actual rock fragment fixture");
            try {
                var effects=root.AddComponent<PrimitiveSkillEffects>();
                effects.Initialize(Resources.Load<BattleAssetCatalog>("Moonlit/Combat/BattleAssets"));
                effects.Play(0,2,Vector3.zero,Vector3.right*3);
                effects.EarlyPlaybackTimeOverride=1.07f;
                yield return null;yield return null;
                var impact=root.transform.Find("Skill contact 0 2 hit 0");
                Assert.IsNotNull(impact);
                var fragments=impact.GetComponentsInChildren<MeshRenderer>();
                Assert.AreEqual(6,fragments.Length);
                Assert.IsEmpty(impact.GetComponentsInChildren<SpriteRenderer>(),"Each piece must be a cut mesh, never a scaled whole rock.");
                var art=PrimitiveSkillEffects.SkillSprite(0,2);
                foreach(var piece in fragments) {
                    Assert.AreSame(art.texture,piece.sharedMaterial.mainTexture);
                    Assert.Greater(piece.GetComponent<MeshFilter>().sharedMesh.triangles.Length,0);
                    Assert.AreEqual(30,piece.gameObject.layer);
                }
            } finally {UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
