using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    public sealed class BattleRuntime : MonoBehaviour
    {
        public bool IsExternalBattle { get; private set; }
        public int Wave { get; private set; }
        public int Round { get; private set; }
        public CombatActorState PlayerState { get; private set; }
        public CombatActorState EnemyState { get; private set; }
        public string LastResult { get; private set; }
        MainScreen main;
        BattleAssetCatalog assets;
        RectTransform view;
        RawImage image;
        Text status, playerHealth, enemyHealth, actionText;
        Image playerFill, enemyFill;
        GameObject stageRoot, player, enemy;
        Camera renderCamera;
        RenderTexture texture;
        Animator playerAnimator, enemyAnimator;
        CombatAnimationRelay playerRelay, enemyRelay;
        CombatAppearance appearance;
        PrimitiveSkillEffects effects;
        Coroutine battle;
        Action<bool> externalCallback;
        readonly System.Random random = new System.Random();
        bool initialized, failedAnimation;
        int externalDifficulty, externalWaves, arenaRating;
        string externalName;

        public void Initialize(MainScreen owner, MainScreenAssets mainAssets)
        {
            if (initialized) return;
            initialized = true; main = owner;
            assets = Resources.Load<BattleAssetCatalog>("Moonlit/Combat/BattleAssets");
            view = Ui.Rect("Live turn battle", main.design, 0, 495, 1080, 440);
            view.SetAsFirstSibling();
            image = view.gameObject.AddComponent<RawImage>(); image.raycastTarget = false;
            playerHealth = Ui.Text("Player health", view, 60, 0, 360, 36, "", 21, main.font);
            enemyHealth = Ui.Text("Enemy health", view, 660, 0, 360, 36, "", 21, main.font);
            Ui.Image("Player health track", view, 65, 38, 350, 13, null, new Color(.02f, .025f, .025f, .8f));
            Ui.Image("Enemy health track", view, 665, 38, 350, 13, null, new Color(.02f, .025f, .025f, .8f));
            playerFill = Ui.Image("Player health fill", view, 65, 38, 350, 13, null, new Color(.24f, .87f, .48f));
            enemyFill = Ui.Image("Enemy health fill", view, 665, 38, 350, 13, null, new Color(.9f, .28f, .23f));
            status = Ui.Text("Battle round", view, 280, 62, 520, 35, "전투 준비", 22, main.font, Ui.Gold);
            actionText = Ui.Text("Last combat action", view, 165, 102, 750, 34, "", 20, main.font);
            if (!assets || !assets.playerPrefab || !assets.controller)
            {
                status.text = "전투 에셋 준비 필요";
                actionText.text = "클라우드 CombatAssetBuilder 실행 후 전투가 시작됩니다.";
                Debug.LogWarning("[Moonlit] Combat asset catalog unavailable; combat has not been simulated.");
                return;
            }
            BuildWorld();
            battle = StartCoroutine(NormalLoop());
        }
        void BuildWorld()
        {
            stageRoot = new GameObject("Isolated live battle rendering");
            stageRoot.transform.position = new Vector3(10000, 10000, 0);
            renderCamera = new GameObject("Battle capture camera").AddComponent<Camera>();
            renderCamera.transform.SetParent(stageRoot.transform, false);
            renderCamera.transform.localPosition = new Vector3(0, 1.2f, -12);
            renderCamera.orthographic = true; renderCamera.orthographicSize = 2.2f;
            renderCamera.clearFlags = CameraClearFlags.SolidColor; renderCamera.backgroundColor = Color.clear;
            renderCamera.cullingMask = 1 << 30; renderCamera.nearClipPlane = .1f; renderCamera.farClipPlane = 30;
            renderCamera.allowHDR = false; renderCamera.allowMSAA = false;
            texture = new RenderTexture(1080, 440, 16, RenderTextureFormat.ARGB32) { name = "Moonlit combat transparent viewport" };
            texture.Create(); renderCamera.targetTexture = texture; image.texture = texture;
            player = CreateActor("Player", -2.5f, false, out playerAnimator, out playerRelay);
            enemy = CreateActor("Enemy (temporary Player prefab)", 2.5f, true, out enemyAnimator, out enemyRelay);
            appearance = player.AddComponent<CombatAppearance>(); appearance.Initialize(assets);
            effects = stageRoot.AddComponent<PrimitiveSkillEffects>(); effects.Initialize(assets);
        }
        GameObject CreateActor(string name, float x, bool mirror, out Animator animator, out CombatAnimationRelay relay)
        {
            var actor = new GameObject(name);
            actor.transform.SetParent(stageRoot.transform, false);
            actor.transform.localPosition = new Vector3(x, 0, 0);
            actor.transform.localScale = new Vector3(mirror ? -1 : 1, 1, 1);
            var motion = new GameObject("Motion"); motion.transform.SetParent(actor.transform, false);
            var rig = Instantiate(assets.playerPrefab, motion.transform, false);
            rig.name = "PlayerRig"; rig.transform.localPosition = Vector3.zero;
            rig.transform.localRotation = Quaternion.identity; rig.transform.localScale = Vector3.one;
            var renderers = rig.GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                float scale = 2.55f / Mathf.Max(.01f, bounds.size.y);
                rig.transform.localScale = Vector3.one * scale;
                // Center the actual assembled prefab, retaining all authored bone offsets.
                rig.transform.localPosition = new Vector3(
                    -(bounds.center.x - actor.transform.position.x) * scale * (mirror ? -1 : 1),
                    -(bounds.min.y - actor.transform.position.y) * scale, 0);
            }
            foreach (var item in actor.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = 30;
            foreach (var existingAnimator in rig.GetComponentsInChildren<Animator>(true)) existingAnimator.enabled = false;
            animator = actor.AddComponent<Animator>(); animator.runtimeAnimatorController = assets.controller;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            relay = actor.AddComponent<CombatAnimationRelay>();
            animator.Play("Idle", 0, 0);
            return actor;
        }
        void LateUpdate()
        {
            if (!view || !main) return;
            var design = main.design as RectTransform;
            float height = Mathf.Max(110, design.rect.height - PortraitSafeArea.BottomHeight - 495);
            view.sizeDelta = new Vector2(1080, height);
            if (renderCamera)
            {
                renderCamera.orthographicSize = Mathf.Max(2, height / 200f);
                renderCamera.aspect = 1080 / height;
                renderCamera.transform.localPosition = new Vector3(0, Mathf.Min(1.7f, height / 400f), -12);
            }
            if (appearance) appearance.Refresh(ForgeState.Current.equipped);
            RefreshHealth();
        }
        CombatActorState BuildPlayer()
        {
            var equipment = ForgeState.Current.TotalStats;
            var s = new CombatStats {
                health = Math.Max(80, equipment.health + CollectionProgression.OwnedHealth + CollectionProgression.EquippedHealth),
                attack = Math.Max(10, equipment.attack + CollectionProgression.OwnedAttack + CollectionProgression.EquippedAttack),
                speed = Math.Max(1, equipment.speed),
                criticalChance = ForgeState.Current.AffixTotal(EquipmentAffixKind.CriticalChance),
                criticalDamage = ForgeState.Current.AffixTotal(EquipmentAffixKind.CriticalDamage),
                dodge = ForgeState.Current.AffixTotal(EquipmentAffixKind.Dodge),
                lifeSteal = ForgeState.Current.AffixTotal(EquipmentAffixKind.LifeSteal),
                regeneration = ForgeState.Current.AffixTotal(EquipmentAffixKind.Regeneration),
                doubleChance = ForgeState.Current.AffixTotal(EquipmentAffixKind.DoubleChance),
                skillDamage = ForgeState.Current.AffixTotal(EquipmentAffixKind.SkillDamage)
            };
            var skills = new List<CombatSkill>();
            foreach (var entry in CollectionProgression.EquippedSkills)
                skills.Add(new CombatSkill { variant = entry.variant, cooldown = entry.Cooldown,
                    heal = entry.FixedHeal, attackBoost = entry.FixedAttackBoost, damage = entry.FixedDamage });
            return new CombatActorState(s, skills);
        }
        IEnumerator NormalLoop()
        {
            yield return null;
            while (isActiveAndEnabled)
            {
                bool won = false;
                yield return FightStage(Math.Max(1, main.stage), CombatRules.WavesPerStage, result => won = result);
                if (failedAnimation) yield break;
                int previous = main.stage;
                main.stage = CombatRules.StageAfter(main.stage, won);
                main.Refresh(); main.SaveGame();
                LastResult = won ? "스테이지 " + previous + " 클리어" : "패배 · 스테이지 " + main.stage + " 재도전";
                status.text = LastResult;
                yield return new WaitForSeconds(1.5f);
            }
        }
        IEnumerator FightStage(int difficulty, int waves, Action<bool> complete)
        {
            for (Wave = 1; Wave <= waves; Wave++)
            {
                PlayerState = BuildPlayer();
                var stats = CombatRules.StageEnemy(difficulty, Wave);
                if (arenaRating >= 0 && IsExternalBattle && externalName == "아레나")
                {
                    double scale = .85 + Math.Max(0, arenaRating) / 1000.0;
                    stats = PlayerState.stats.Copy(); stats.health *= scale; stats.attack *= scale;
                    stats.speed = Math.Max(1, stats.speed * scale);
                }
                EnemyState = new CombatActorState(stats);
                playerAnimator.Play("Idle", 0, 0); enemyAnimator.Play("Idle", 0, 0);
                actionText.text = "";
                yield return Entrance();
                bool playerFirst = CombatRules.PlayerFirst(PlayerState.stats.speed, EnemyState.stats.speed, random.NextDouble());
                for (Round = 1; Round <= CombatRules.RoundsPerWave; Round++)
                {
                    status.text = (IsExternalBattle ? externalName : "스테이지 " + main.stage) +
                        " · 웨이브 " + Wave + "/" + waves + " · 라운드 " + Round + "/15";
                    PlayerState.Regenerate(); EnemyState.Regenerate();
                    yield return ActorTurn(playerFirst);
                    if (PlayerState.Alive && EnemyState.Alive) yield return ActorTurn(!playerFirst);
                    if (failedAnimation) { complete(false); yield break; }
                    if (!PlayerState.Alive || !EnemyState.Alive) break;
                }
                bool wonWave = PlayerState.Alive && !EnemyState.Alive;
                (wonWave ? enemyAnimator : playerAnimator).Play("Death", 0, 0);
                yield return new WaitForSeconds(.85f);
                if (!wonWave) { complete(false); yield break; }
            }
            Wave = waves;
            complete(true);
        }
        IEnumerator Entrance()
        {
            for (float t = 0; t < .45f; t += Time.deltaTime)
            {
                player.transform.localPosition = new Vector3(Mathf.Lerp(-5.5f, -2.5f, t / .45f), 0, 0);
                enemy.transform.localPosition = new Vector3(Mathf.Lerp(5.5f, 2.5f, t / .45f), 0, 0);
                yield return null;
            }
            player.transform.localPosition = new Vector3(-2.5f, 0, 0);
            enemy.transform.localPosition = new Vector3(2.5f, 0, 0);
        }
        IEnumerator ActorTurn(bool isPlayer)
        {
            var actor = isPlayer ? PlayerState : EnemyState;
            var target = isPlayer ? EnemyState : PlayerState;
            if (!actor.Alive || !target.Alive || failedAnimation) yield break;
            foreach (var action in CombatRules.PlanTurn(actor, random.NextDouble))
            {
                if (!actor.Alive || !target.Alive || failedAnimation) yield break;
                var skill = action.skill;
                if (action.IsBasic)
                    yield return Strike(isPlayer, actor.stats.attack + actor.AttackBoost, false, 0);
                else if (skill.variant == 0)
                    yield return AnimatedAction(isPlayer, 1, "Buff", () => {
                        actor.Heal(skill.heal); actor.SetAttackBoost(Math.Max(actor.AttackBoost, skill.attackBoost));
                        ShowSkill(isPlayer, 0); actionText.text = "선조의 축복 · 회복 + 공격력 증가";
                    });
                else
                    yield return Strike(isPlayer, skill.damage, true, skill.variant);
            }
        }

        IEnumerator Strike(bool isPlayer, double damage, bool skill, int variant)
        {
            var actor = isPlayer ? PlayerState : EnemyState;
            var target = isPlayer ? EnemyState : PlayerState;
            yield return AnimatedAction(isPlayer, skill ? variant + 1 : 0, skill ? (variant == 1 ? "Weak" : "Strong") : "Basic", () => {
                var hit = CombatRules.Strike(actor, target, damage, skill, random.NextDouble);
                if (skill) ShowSkill(isPlayer, variant);
                if (!hit.evaded) (isPlayer ? enemyAnimator : playerAnimator).Play(target.Alive ? "Hit" : "Death", 0, 0);
                actionText.text = (isPlayer ? "플레이어" : "적") + " · " +
                    (hit.evaded ? "회피" : (hit.critical ? "치명타 " : "") + Format(hit.damage) + (skill ? " 스킬 피해" : " 피해"));
            });
        }
        IEnumerator AnimatedAction(bool isPlayer, int kind, string state, Action impact)
        {
            if (failedAnimation) yield break;
            var relay = isPlayer ? playerRelay : enemyRelay;
            var animator = isPlayer ? playerAnimator : enemyAnimator;
            relay.Arm(kind, impact); animator.Play(state, 0, 0);
            yield return new WaitForSeconds(.8f);
            if (relay.Pending)
            {
                // A missing animation event must never silently apply guessed damage.
                relay.Cancel(); failedAnimation = true;
                status.text = "전투 일시 정지 · 애니메이션 이벤트 확인 필요";
                Debug.LogError("[Moonlit] Missing OnCombatImpact event in Animator state " + state);
            }
            else if ((isPlayer ? PlayerState : EnemyState).Alive) animator.Play("Idle", 0, 0);
        }
        void ShowSkill(bool isPlayer, int variant)
        {
            Vector3 source = (isPlayer ? player : enemy).transform.position + Vector3.up * 1.2f;
            Vector3 target = (isPlayer ? enemy : player).transform.position + Vector3.up * 1.2f;
            effects.Play(variant, source, target);
        }
        public void PreviewPrimitiveSkill(int variant)
        {
            if (!effects) { main.Toast("전투 에셋 준비가 필요합니다"); return; }
            ShowSkill(true, Mathf.Clamp(variant, 0, 2));
            main.Toast(new[] { "선조의 축복", "돌날 가르기", "거석 강타" }[Mathf.Clamp(variant, 0, 2)] + " · 원시 스킬 미리보기");
        }
        public bool StartDungeon(int index, int difficulty, int waves, Action<bool> callback)
        {
            if (index < 0 || index > 3) return false;
            return StartExternal(new[] { "망치도둑", "유령마을", "침략", "좀비러시" }[index],
                Math.Max(1, difficulty) * 5, Mathf.Clamp(waves, 1, 3), -1, callback);
        }
        public bool StartArena(int opponentRating, Action<bool> callback)
            => StartExternal("아레나", 1, 1, Math.Max(0, opponentRating), callback);
        bool StartExternal(string title, int difficulty, int waves, int rating, Action<bool> callback)
        {
            if (!assets || !renderCamera || IsExternalBattle || failedAnimation) return false;
            if (battle != null) StopCoroutine(battle);
            playerRelay.Cancel(); enemyRelay.Cancel();
            IsExternalBattle = true; externalCallback = callback; externalName = title; arenaRating = rating;
            externalDifficulty = difficulty; externalWaves = waves;
            battle = StartCoroutine(ExternalLoop());
            return true;
        }
        IEnumerator ExternalLoop()
        {
            bool won = false;
            yield return FightStage(externalDifficulty, externalWaves, result => won = result);
            var callback = externalCallback; externalCallback = null; IsExternalBattle = false;
            LastResult = externalName + (won ? " 클리어" : " 패배"); status.text = LastResult;
            callback?.Invoke(won);
            if (failedAnimation) yield break;
            yield return new WaitForSeconds(1.5f);
            battle = StartCoroutine(NormalLoop());
        }
        void RefreshHealth()
        {
            if (PlayerState == null || EnemyState == null) return;
            playerHealth.text = "플레이어 " + Format(PlayerState.Health) + "/" + Format(PlayerState.stats.health);
            enemyHealth.text = "적 " + Format(EnemyState.Health) + "/" + Format(EnemyState.stats.health);
            playerFill.rectTransform.sizeDelta = new Vector2(350 * (float)(PlayerState.Health / PlayerState.stats.health), 13);
            enemyFill.rectTransform.sizeDelta = new Vector2(350 * (float)(EnemyState.Health / EnemyState.stats.health), 13);
        }
        static string Format(double value) => value >= 1e9 ? value.ToString("0.##E+0") : Math.Ceiling(value).ToString("N0");
        void OnDestroy()
        {
            externalCallback = null;
            if (renderCamera) renderCamera.targetTexture = null;
            if (texture) { texture.Release(); Destroy(texture); }
            if (stageRoot) Destroy(stageRoot);
        }
    }
}
