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
                Vector3 before = battle.PlayerHud.transform.position;
                actor.position += Vector3.right * .75f;
                yield return null;
                Assert.AreEqual(.75f,battle.PlayerHud.transform.position.x-before.x,.02f);
                Assert.AreEqual(RenderMode.WorldSpace,battle.PlayerHud.WorldCanvas.renderMode);
                Assert.IsNull(root.transform.Find("Live turn battle/Battle round"));
                Assert.IsNull(root.transform.Find("Live turn battle/Last combat action"));

                typeof(BattleRuntime).GetProperty("PlayerState").SetValue(battle,new CombatActorState(new CombatStats { health=100,attack=20 }));
                var victim = new CombatActorState(new CombatStats { health=100 });
                typeof(BattleRuntime).GetProperty("EnemyState").SetValue(battle,victim);
                var attack = (IEnumerator)typeof(BattleRuntime).GetMethod("Strike",Private).Invoke(battle,new object[] {true,20d,false,0});
                Assert.IsTrue(attack.MoveNext());
                var motion = (IEnumerator)attack.Current; Assert.IsTrue(motion.MoveNext());
                var animator = (Animator)typeof(BattleRuntime).GetField("playerAnimator",Private).GetValue(battle);
                animator.Update(0); animator.Update(.31f);
                Assert.AreEqual(80,victim.Health);
                var stage = battle.EnemyHud.transform.parent;
                var number = stage.Find("Enemy damage number");
                Assert.IsNotNull(number,"A hit creates a world number for the target.");
                Assert.IsNull(stage.Find("Player damage number"),"Damage must not appear above its attacker.");
                Assert.AreEqual(RenderMode.WorldSpace,number.GetComponent<Canvas>().renderMode);
                Assert.AreEqual(30,number.gameObject.layer);
                StringAssert.Contains("20",number.GetComponentInChildren<Text>().text);
                Assert.AreEqual(battle.EnemyHud.transform.position.x,number.position.x,.05f);
                stage.gameObject.SetActive(false);
                yield return null;
                Assert.IsTrue(!number,"Disabling combat must clear transient numbers even when their coroutine stops early.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(assets); }
        }

        [UnityTest]
        public IEnumerator IllustratedStoneImpactUsesSpriteTextureInsteadOfWhiteSquareParticles()
        {
            var root = new GameObject("Illustrated VFX fixture");
            try
            {
                var effects = root.AddComponent<PrimitiveSkillEffects>();
                effects.Initialize(Resources.Load<BattleAssetCatalog>("Moonlit/Combat/BattleAssets"));
                effects.Play(0,1,Vector3.zero,Vector3.right*3);
                yield return new WaitForSeconds(PrimitiveSkillEffects.AttackFlightDuration+.03f);
                var particles = root.GetComponentInChildren<ParticleSystem>();
                Assert.IsNotNull(particles);
                var sheet = particles.textureSheetAnimation;
                Assert.IsTrue(sheet.enabled);
                Assert.AreEqual(ParticleSystemAnimationMode.Sprites,sheet.mode);
                var art = PrimitiveSkillEffects.SkillSprite(0,1);
                Assert.AreSame(art,sheet.GetSprite(0));
                Assert.AreSame(art.texture,particles.GetComponent<ParticleSystemRenderer>().sharedMaterial.mainTexture);
                Assert.AreEqual(30,particles.gameObject.layer);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
