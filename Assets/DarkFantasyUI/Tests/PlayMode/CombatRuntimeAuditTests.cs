using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonlit.UI.Tests
{
    public sealed class CombatRuntimeAuditTests
    {
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        sealed class Fixture : IDisposable
        {
            readonly ForgeState previousForge = ForgeState.Current;
            readonly CollectionSave previousCollections;
            readonly float previousTimeScale = Time.timeScale;
            public readonly GameObject root;
            public readonly MainScreen main;
            public readonly BattleRuntime battle;
            readonly MainScreenAssets assets;

            public Fixture(float height = 1920)
            {
                previousCollections = CollectionProgression.Data;
                ForgeState.Current = new ForgeState();
                CollectionProgression.Data = CollectionProgression.Create();
                Time.timeScale = 10;
                root = new GameObject("Battle audit fixture", typeof(RectTransform));
                ((RectTransform)root.transform).sizeDelta = new Vector2(1080, height);
                main = root.AddComponent<MainScreen>(); main.enabled = false; main.design = root.transform;
                assets = ScriptableObject.CreateInstance<MainScreenAssets>();
                assets.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); main.font = assets.font;
                battle = root.AddComponent<BattleRuntime>(); battle.Initialize(main, assets);
                battle.StopAllCoroutines();
            }
            public void Dispose()
            {
                battle.StopAllCoroutines();
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(assets);
                ForgeState.Current = previousForge;
                CollectionProgression.Data = previousCollections;
                Time.timeScale = previousTimeScale;
            }
        }

        [Test]
        public void ActualPrefabSwapsArmorFivePartsHatAndWeaponThenRestores()
        {
            var catalog = Resources.Load<BattleAssetCatalog>("Moonlit/Combat/BattleAssets");
            Assert.IsNotNull(catalog, "Cloud CombatAssetBuilder must run first.");
            var rig = UnityEngine.Object.Instantiate(catalog.playerPrefab);
            try
            {
                var renderers = rig.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(r => r.sprite).ToDictionary(r => r.sprite.name, r => r);
                var original = renderers.ToDictionary(p => p.Key, p => p.Value.sprite);
                var appearance = rig.AddComponent<CombatAppearance>(); appearance.Initialize(catalog);
                var items = new EquipmentRoll[6];
                items[(int)EquipmentPart.Armor] = new EquipmentRoll { id = 101, part = EquipmentPart.Armor, tier = 9, variant = 0, level = 1 };
                items[(int)EquipmentPart.Hat] = new EquipmentRoll { id = 102, part = EquipmentPart.Hat, tier = 8, variant = 1, level = 1 };
                items[(int)EquipmentPart.Weapon] = new EquipmentRoll { id = 103, part = EquipmentPart.Weapon, tier = 7, variant = 2, level = 1 };
                appearance.Refresh(items);
                var armor = catalog.Find(9, 0);
                Assert.AreSame(armor.body, renderers["몸통"].sprite);
                Assert.AreSame(armor.arm1, renderers["팔1"].sprite); Assert.AreSame(armor.arm2, renderers["팔2"].sprite);
                Assert.AreSame(armor.leg1, renderers["다리1"].sprite); Assert.AreSame(armor.leg2, renderers["다리2"].sprite);
                Assert.AreSame(catalog.Find(8, 1).head, renderers["머리"].sprite);
                Assert.AreSame(catalog.Find(7, 2).weapon, renderers["무기"].sprite);
                appearance.Refresh(new EquipmentRoll[6]);
                foreach (var part in original) Assert.AreSame(part.Value, renderers[part.Key].sprite, part.Key);
            }
            finally { UnityEngine.Object.DestroyImmediate(rig); }
        }

        [UnityTest]
        public IEnumerator ActualBattleTimesOutAfterBothActorsCompleteRoundFifteen()
        {
            using (var fixture = new Fixture())
            {
                // Health/speed equipment can survive the enemy but adds no attack: deterministic timeout.
                ForgeState.Current.equipped[(int)EquipmentPart.Armor] = new EquipmentRoll {
                    id = 201, part = EquipmentPart.Armor, tier = 9, level = 100
                };
                int callbacks = 0; bool victory = true;
                var method = typeof(BattleRuntime).GetMethod("FightStage", PrivateInstance);
                var encounter = (IEnumerator)method.Invoke(fixture.battle, new object[] {
                    100, 1, new Action<bool>(won => { callbacks++; victory = won; })
                });
                yield return fixture.battle.StartCoroutine(encounter);
                Assert.AreEqual(1, callbacks); Assert.IsFalse(victory);
                Assert.AreEqual(15, fixture.battle.Round);
                Assert.IsTrue(fixture.battle.PlayerState.Alive); Assert.IsTrue(fixture.battle.EnemyState.Alive);
                Assert.AreEqual(15, fixture.battle.PlayerResolvedBasicAttacks);
                Assert.AreEqual(15, fixture.battle.EnemyResolvedBasicAttacks);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator DungeonWaitsForClaimExactlyOnceBeforeRestartingNormalLoop()
        {
            using (var fixture = new Fixture())
            {
                fixture.main.stage = 7;
                ForgeState.Current.equipped[(int)EquipmentPart.Weapon] = new EquipmentRoll {
                    id = 301, part = EquipmentPart.Weapon, tier = 1, level = 100
                };
                ForgeState.Current.equipped[(int)EquipmentPart.Armor] = new EquipmentRoll {
                    id = 302, part = EquipmentPart.Armor, tier = 1, level = 100
                };
                int callbacks = 0; bool victory = false;
                Assert.IsTrue(fixture.battle.StartDungeon(0, 1, 1, won => { callbacks++; victory = won; }));
                Assert.IsFalse(fixture.battle.StartDungeon(0, 1, 1, won => Assert.Fail("A rejected duplicate must not callback.")));
                var routineField = typeof(BattleRuntime).GetField("battle", PrivateInstance);
                var externalRoutine = routineField.GetValue(fixture.battle);
                float deadline = Time.realtimeSinceStartup + 15;
                while (callbacks == 0 && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.AreEqual(1, callbacks); Assert.IsTrue(victory);
                Assert.IsTrue(fixture.battle.IsExternalBattle);
                Assert.IsTrue(fixture.battle.AwaitingExternalClaim);
                Assert.AreEqual(7, fixture.main.stage, "Dungeon outcome must not mutate normal stage.");
                var finishedState = fixture.battle.EnemyState;
                yield return new WaitForSeconds(2);
                Assert.AreSame(finishedState, fixture.battle.EnemyState, "A won dungeon must remain visible until reward claim.");
                Assert.IsTrue(fixture.battle.CompleteExternalClaim());
                Assert.IsFalse(fixture.battle.CompleteExternalClaim(), "A duplicate claim cannot restart twice.");
                yield return new WaitForSeconds(1.6f);
                Assert.AreNotSame(externalRoutine, routineField.GetValue(fixture.battle), "Normal loop must restart.");
                Assert.AreEqual(1, callbacks);
                Assert.IsFalse(fixture.battle.IsExternalBattle);
                Assert.AreEqual(7, fixture.main.stage, "The first normal encounter should have resumed, not completed.");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator CompactSafeAreaKeepsEntireActorAndHudInsideBattleView()
        {
            using (var fixture = new Fixture(1600))
            {
                yield return null;
                typeof(BattleRuntime).GetMethod("LateUpdate", PrivateInstance).Invoke(fixture.battle, null);
                var camera = (Camera)typeof(BattleRuntime).GetField("renderCamera", PrivateInstance).GetValue(fixture.battle);
                var actor = (GameObject)typeof(BattleRuntime).GetField("player", PrivateInstance).GetValue(fixture.battle);
                Assert.IsNotNull(camera);
                foreach (var renderer in actor.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    Assert.LessOrEqual(camera.WorldToViewportPoint(renderer.bounds.max).y, 1.001f, renderer.name + " top clipped");
                    Assert.GreaterOrEqual(camera.WorldToViewportPoint(renderer.bounds.min).y, -.001f, renderer.name + " bottom clipped");
                }
                var view = fixture.root.transform.Find("Live turn battle") as RectTransform;
                Assert.Greater(view.rect.height, 120);
                Assert.IsNull(view.Find("Last combat action"));
                Assert.IsNull(view.Find("Battle round"));
                Assert.AreEqual(2, Mathf.Abs(actor.transform.localScale.x), .001f);
                Assert.AreEqual(2, actor.transform.localScale.y, .001f);
                Assert.AreEqual(30, actor.layer);
                Assert.AreEqual(RenderMode.WorldSpace, fixture.battle.PlayerHud.WorldCanvas.renderMode);
                Assert.AreSame(camera, fixture.battle.PlayerHud.WorldCanvas.worldCamera);
                Assert.AreEqual(120 / 4f, view.rect.height / (camera.orthographicSize * 2), .01f,
                    "Expanding the viewport must preserve the old compact pixels/world-unit so doubling is not cancelled.");
            }
            yield return null;
        }
    }
}
