using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Moonlit.UI.Tests
{
    public sealed class CombatBootstrapTests
    {
        bool previousPersistence;
        float previousTimeScale;
        ForgeState previousForge;
        CollectionSave previousCollections;
        RewardState previousRewards;
        GameObject root;

        [SetUp]
        public void Setup()
        {
            previousTimeScale = Time.timeScale; Time.timeScale = 1;
            previousPersistence = MainScreen.PersistenceEnabled;
            previousForge = ForgeState.Current;
            previousCollections = CollectionProgression.Data;
            previousRewards = RewardState.Current;
            MainScreen.PersistenceEnabled = false;
            ForgeState.Current = new ForgeState();
            CollectionProgression.Data = CollectionProgression.Create();
            RewardState.Current = new RewardState();
        }
        [TearDown]
        public void Teardown()
        {
            if (root) Object.DestroyImmediate(root);
            Time.timeScale = previousTimeScale;
            MainScreen.PersistenceEnabled = previousPersistence;
            ForgeState.Current = previousForge;
            CollectionProgression.Data = previousCollections;
            RewardState.Current = previousRewards;
        }
        static MainScreenAssets RealAssets()
        {
#if UNITY_EDITOR
            var assets = AssetDatabase.LoadAssetAtPath<MainScreenAssets>("Assets/DarkFantasyUI/Data/MainScreenAssets.asset");
            Assert.IsNotNull(assets, "Use the actual shipped bootstrap asset catalog.");
            return assets;
#else
            Assert.Ignore("The full-factory acceptance fixture loads the authored catalog in cloud Editor PlayMode.");
            return null;
#endif
        }

        static IEnumerator RequireCombatProgress(MainScreen main)
        {
            var battle = main.GetComponent<BattleRuntime>();
            Assert.IsNotNull(battle);
            float deadline = Time.realtimeSinceStartup + 20;
            while ((battle.PlayerState == null || battle.PlayerResolvedBasicAttacks + battle.EnemyResolvedBasicAttacks == 0)
                && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsNotNull(battle.PlayerState, "Actual bootstrap activation must initialize the first normal wave.");
            Assert.IsNotNull(battle.EnemyState);
            Assert.AreEqual(1, battle.Wave);
            Assert.GreaterOrEqual(battle.Round, 1);
            Assert.Greater(battle.PlayerResolvedBasicAttacks + battle.EnemyResolvedBasicAttacks, 0,
                "The real factory must advance through Animator event damage, not remain at battle-ready.");
            Assert.IsTrue(battle.PlayerState.Health < battle.PlayerState.stats.health ||
                battle.EnemyState.Health < battle.EnemyState.stats.health);
            yield return null; // LateUpdate must populate the visible health labels.
            LogAssert.NoUnexpectedReceived();
            var labels = new[] { battle.PlayerHud.HealthText, battle.EnemyHud.HealthText };
            foreach (string name in new[] { "Player health", "Enemy health" })
            {
                var label = labels.Single(t => t.name == name);
                Assert.IsNotEmpty(label.text, name);
                StringAssert.Contains("/", label.text);
                Assert.IsTrue(label.resizeTextForBestFit);
                Assert.AreEqual(18, label.resizeTextMinSize);
                Assert.AreEqual(VerticalWrapMode.Truncate, label.verticalOverflow);
            }
        }

        [UnityTest]
        public IEnumerator FullFactoryBuiltInactiveStartsAfterActivationAndAfterReactivation()
        {
            root = new GameObject("Inactive factory acceptance root"); root.SetActive(false);
            var main = new RuntimeMainScreenFactory(RealAssets()).Create(root.transform);
            LogAssert.NoUnexpectedReceived();
            var battle = main.GetComponent<BattleRuntime>();
            Assert.IsFalse(battle.isActiveAndEnabled);
            Assert.IsNull(battle.PlayerState);
            Assert.IsFalse(battle.StartDungeon(0, 1, 1, won => Assert.Fail("Inactive encounter must not be accepted.")));
            yield return null;
            Assert.IsNull(battle.PlayerState, "Inactive construction must defer the normal coroutine.");
            root.SetActive(true);
            yield return RequireCombatProgress(main);
            var previousState = battle.PlayerState;
            root.SetActive(false);
            yield return null;
            root.SetActive(true);
            float deadline = Time.realtimeSinceStartup + 20;
            while (ReferenceEquals(previousState, battle.PlayerState) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreNotSame(previousState, battle.PlayerState, "A stale stopped coroutine handle must not block reactivation.");
            yield return RequireCombatProgress(main);
        }

        [UnityTest]
        public IEnumerator ActualBootstrapAwakeStartsNormalBattleBelowItsInactiveConstructionRoot()
        {
            root = new GameObject("Actual bootstrap acceptance root"); root.SetActive(false);
            var bootstrap = root.AddComponent<MainScreenBootstrap>();
            bootstrap.assets = RealAssets();
            root.SetActive(true); // Awake -> Build -> inactive factory -> activate: the production path.
            LogAssert.NoUnexpectedReceived();
            Assert.IsNotNull(bootstrap.Screen);
            Assert.AreSame(bootstrap.Screen, bootstrap.Build());
            Assert.AreEqual(1, root.GetComponentsInChildren<BattleRuntime>(true).Length);
            yield return RequireCombatProgress(bootstrap.Screen);
        }
    }
}
