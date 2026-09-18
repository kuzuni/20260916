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
        public int WaveCount { get; private set; } = 3;
        public bool AwaitingExternalClaim { get; private set; }
        public CombatWorldHud PlayerHud { get; private set; }
        public CombatWorldHud EnemyHud { get; private set; }
        public int PlayerResolvedBasicAttacks { get; private set; }
        public int EnemyResolvedBasicAttacks { get; private set; }
        public CombatActorState PlayerState { get; private set; }
        public CombatActorState EnemyState { get; private set; }
        public const float SkillHudReservedHeight = 216;
        public int PlayerTurnCount => PlayerState == null ? 0 : PlayerState.Turns;
        readonly HashSet<CombatSkill> playerSkillsUsedThisTurn = new HashSet<CombatSkill>();
        readonly Dictionary<CombatSkill, int> playerSkillActivations = new Dictionary<CombatSkill, int>();
        public int PlayerSkillActivationCount(int skillIndex)
        {
            if (PlayerState == null || skillIndex < 0 || skillIndex >= PlayerState.skills.Count) return 0;
            return playerSkillActivations.TryGetValue(PlayerState.skills[skillIndex], out int count) ? count : 0;
        }
        void RecordPlayerSkill(CombatSkill skill)
        {
            playerSkillsUsedThisTurn.Add(skill);
            playerSkillActivations.TryGetValue(skill, out int count);
            playerSkillActivations[skill] = count + 1;
        }
        // Zero means due during this actor turn; after the impact, show the full next cooldown.
        public int PlayerSkillTurnsUntilReady(int skillIndex)
        {
            if (PlayerState == null || skillIndex < 0 || skillIndex >= PlayerState.skills.Count) return -1;
            var skill = PlayerState.skills[skillIndex];
            int cooldown = Math.Max(1, skill.cooldown);
            int remainder = PlayerTurnCount % cooldown;
            if (PlayerTurnCount > 0 && remainder == 0 && !playerSkillsUsedThisTurn.Contains(skill)) return 0;
            return cooldown - remainder;
        }
        public string LastResult { get; private set; }
        MainScreen main;
        BattleAssetCatalog assets;
        RectTransform view;
        RawImage image;

        GameObject stageRoot, player, enemy;
        Camera renderCamera;
        RenderTexture texture;
        readonly Rect[] protectedHudAreas = new Rect[3];
        readonly Rect[] actorProtectedHudAreas = new Rect[8];
        readonly List<Bounds> playerLayoutParts=new List<Bounds>(),enemyLayoutParts=new List<Bounds>();
        readonly List<Bounds> playerLayoutFormation=new List<Bounds>(),enemyLayoutFormation=new List<Bounds>();
        RectTransform skillStrip;
        float formationGroundDrop;
        int formationLayoutKey;
        CombatActorState formationEncounter;
        public float FormationGroundOffset => -formationGroundDrop;
        readonly List<SpriteRenderer> clearanceSprites = new List<SpriteRenderer>();
        Animator playerAnimator, enemyAnimator;
        CombatAnimationRelay playerRelay, enemyRelay;
        CombatAppearance appearance;
        CombatHitFlash playerFlash, enemyFlash;
        PrimitiveSkillEffects effects;
        Coroutine battle;
        CombatComboSequence activeCombo;
        public bool IsSkillComboRunning => activeCombo != null && activeCombo.Pending;
        public int ResolvedSkillHits => activeCombo == null ? 0 : activeCombo.ResolvedHits;
        Action<bool> externalCallback;
        readonly System.Random random = new System.Random();
        bool initialized, failedAnimation;
        bool playerDeathStarted, enemyDeathStarted;
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
            if (!assets || !assets.playerPrefab || !assets.playerPrefab.GetComponent<Animator>() ||
                !assets.playerPrefab.GetComponent<Animator>().runtimeAnimatorController)
            {
                Debug.LogWarning("[Moonlit] Combat asset catalog unavailable; combat has not been simulated.");
                return;
            }
            BuildWorld();
            stageRoot.SetActive(isActiveAndEnabled);
            StartBattleWhenReady();
        }
        void OnEnable() { StartBattleWhenReady(); }
        void OnDisable()
        {
            CancelSkillCombo();
            StopAllCoroutines(); battle = null;
            if (playerRelay) playerRelay.Cancel();
            if (enemyRelay) enemyRelay.Cancel();
            if (stageRoot) stageRoot.SetActive(false);
        }
        void StartBattleWhenReady()
        {
            // The real bootstrap builds the complete UI below an inactive parent.
            // OnEnable starts only after Initialize has prepared the world, and clears no pending encounter.
            if (!initialized || !isActiveAndEnabled || !assets || !renderCamera || battle != null || failedAnimation) return;
            if (AwaitingExternalClaim) { stageRoot.SetActive(true); return; }
            stageRoot.SetActive(true);
            battle = StartCoroutine(IsExternalBattle ? ExternalLoop() : NormalLoop());
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
            ResizeRenderTexture(view.rect.height);
            player = CreateActor("Player", -2.5f, false, out playerAnimator, out playerRelay);
            enemy = CreateActor("Enemy (temporary Player prefab)", 2.5f, true, out enemyAnimator, out enemyRelay);
            appearance = player.AddComponent<CombatAppearance>(); appearance.Initialize(assets);
            effects = stageRoot.AddComponent<PrimitiveSkillEffects>(); effects.Initialize(assets);
            playerFlash = player.AddComponent<CombatHitFlash>();
            enemyFlash = enemy.AddComponent<CombatHitFlash>();
            PlayerHud = CombatWorldHud.Create(stageRoot.transform, player, renderCamera, main.font, true);
            EnemyHud = CombatWorldHud.Create(stageRoot.transform, enemy, renderCamera, main.font, false);
            stageRoot.AddComponent<CompanionBattleRuntime>().Initialize(player.transform, renderCamera);
            stageRoot.AddComponent<CombatActorHudClearance>().Initialize(this);
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
            actor.transform.localScale *= 2; // Explicit user request: twice the authored runtime size.
            foreach (var item in actor.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = 30;
            // The prefab's Animator is the source of truth. A wrapper controller would bypass
            // the user's edited native clips and consume events on the wrong GameObject.
            animator = rig.GetComponent<Animator>();
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            relay = animator.GetComponent<CombatAnimationRelay>();
            if (!relay) relay = animator.gameObject.AddComponent<CombatAnimationRelay>();
            animator.Play("Idle", 0, 0);
            CombatGroundShadow.Create(stageRoot.transform, actor.transform);
            return actor;
        }
        void LateUpdate()
        {
            if (!view || !main) return;
            var design = main.design as RectTransform;
            // Preserve the old pixels-per-world-unit exactly; expanding the viewport must not undo the 2x actor scale.
            float oldHeight = Mathf.Max(110, design.rect.height - PortraitSafeArea.BottomHeight - 495);
            float density = oldHeight / (2 * Mathf.Max(2, oldHeight / 200f));
            float bottom = design.rect.height - PortraitSafeArea.BottomHeight - SkillHudReservedHeight;
            float height = Mathf.Max(bottom - 405, 9f * density);
            // Preserve the original projection above the strip, while allowing a compact formation
            // to use empty space at the left of that strip. Its actual occupied rectangle is protected below.
            float extension=SkillHudReservedHeight-12;
            view.anchoredPosition = new Vector2(0, -(bottom - height));
            height+=extension;
            view.sizeDelta = new Vector2(1080, height);
            if (renderCamera)
            {
                ResizeRenderTexture(height);
                renderCamera.orthographicSize = height / (2 * density);
                renderCamera.aspect = 1080 / height;
                renderCamera.transform.localPosition = new Vector3(0, renderCamera.orthographicSize - .28f - extension/density, -12);
            }
            UpdateProtectedHudAreas(design, density, bottom);
            if (appearance) appearance.Refresh(ForgeState.Current.equipped);
            RefreshHealth();
        }
        public void ApplyActorHudClearance()
        {
            if(!renderCamera||!player||!enemy||!PlayerHud.WorldCanvas.enabled||!EnemyHud.WorldCanvas.enabled)return;
            float left=renderCamera.ViewportToWorldPoint(new Vector3(.025f,0,12)).x;
            float right=renderCamera.ViewportToWorldPoint(new Vector3(.975f,0,12)).x;
            float floor=renderCamera.ViewportToWorldPoint(new Vector3(0,0,12)).y+.08f;
            var companions=stageRoot.GetComponent<CompanionBattleRuntime>();
            int key=Mathf.RoundToInt(view.rect.height)*7+(companions&&companions.Mount?companions.Mount.Variant+1:0);
            if(key!=formationLayoutKey||formationEncounter!=PlayerState){formationLayoutKey=key;formationEncounter=PlayerState;formationGroundDrop=0;}
            PrepareLayoutBounds(player,companions,true,playerLayoutParts,playerLayoutFormation);
            PrepareLayoutBounds(enemy,null,false,enemyLayoutParts,enemyLayoutFormation);
            float maximumDrop=3f;
            foreach(var part in playerLayoutFormation)maximumDrop=Mathf.Min(maximumDrop,part.min.y-floor);
            foreach(var part in enemyLayoutFormation)maximumDrop=Mathf.Min(maximumDrop,part.min.y-floor);
            float playerHead=LayoutHeadX(player,true),enemyHead=LayoutHeadX(enemy,false),centre=stageRoot.transform.position.x;
            // Separate the actual heads/HP bars, while allowing authored arms and weapons to reach inward.
            float playerMaximum=centre-1.35f-playerHead,enemyMinimum=centre+1.35f-enemyHead;
            float previousDrop=0,firstDrop=Mathf.Min(formationGroundDrop,Mathf.Max(0,maximumDrop));
            int steps=Mathf.CeilToInt((maximumDrop-firstDrop)/.04f);
            for(int step=0;step<=steps;step++) {
                float drop=Mathf.Min(maximumDrop,firstDrop+step*.04f);
                float change=previousDrop-drop;previousDrop=drop;
                TranslateBounds(playerLayoutParts,Vector3.up*change);TranslateBounds(playerLayoutFormation,Vector3.up*change);
                TranslateBounds(enemyLayoutParts,Vector3.up*change);TranslateBounds(enemyLayoutFormation,Vector3.up*change);
                bool p=CombatActorHudClearance.TrySafeShift(playerLayoutParts,playerLayoutFormation,actorProtectedHudAreas,
                    true,left,right,float.NegativeInfinity,playerMaximum,out float playerX);
                bool e=CombatActorHudClearance.TrySafeShift(enemyLayoutParts,enemyLayoutFormation,actorProtectedHudAreas,
                    false,left,right,enemyMinimum,float.PositiveInfinity,out float enemyX);
                if(!p||!e)continue;
                formationGroundDrop=drop;
                MoveFormationTo(player,true,PlayerHud,companions,playerX,-drop,left,right);
                MoveFormationTo(enemy,false,EnemyHud,null,enemyX,-drop,left,right);
                return;
            }
            // An impossible layout remains visible to the geometry/capture assertions instead of crossing sides.
        }
        void PrepareLayoutBounds(GameObject actor,CompanionBattleRuntime companions,bool isPlayer,List<Bounds> parts,List<Bounds> formation)
        {
            parts.Clear();AddRenderedBounds(actor,parts);
            if(companions&&companions.Mount)AddRenderedBounds(companions.Mount.gameObject,parts);
            formation.Clear();formation.AddRange(parts);
            var home=stageRoot.transform.position+new Vector3(isPlayer?-2.5f:2.5f,0,0);
            var offset=home-actor.transform.position;offset.z=0;
            TranslateBounds(parts,offset);TranslateBounds(formation,offset);
        }
        static void TranslateBounds(List<Bounds> parts,Vector3 offset)
        {
            for(int i=0;i<parts.Count;i++){var part=parts[i];part.center+=offset;parts[i]=part;}
        }
        float LayoutHeadX(GameObject actor,bool isPlayer)
        {
            float x=actor.transform.position.x;
            foreach(var sprite in actor.GetComponentsInChildren<SpriteRenderer>())
                if(sprite.enabled&&sprite.sprite&&sprite.sprite.name=="머리"){x=sprite.bounds.center.x;break;}
            return x-actor.transform.position.x+stageRoot.transform.position.x+(isPlayer?-2.5f:2.5f);
        }
        void MoveFormationTo(GameObject actor,bool isPlayer,CombatWorldHud hud,CompanionBattleRuntime companions,
            float shift,float ground,float left,float right)
        {
            var destination=stageRoot.transform.position+new Vector3((isPlayer?-2.5f:2.5f)+shift,ground,0);
            var offset=destination-actor.transform.position;offset.z=0;
            actor.transform.position+=offset;
            if(companions) {
                if(companions.Mount)MoveCompanionWorld(companions.Mount,offset);
                foreach(var pet in companions.Pets)if(pet) {
                    MoveCompanionWorld(pet,offset);
                    var bounds=pet.VisibleBounds;
                    float correction=bounds.min.x<left?left-bounds.min.x:bounds.max.x>right?right-bounds.max.x:0;
                    if(correction!=0){pet.transform.position+=Vector3.right*correction;pet.RefreshShadow();}
                }
            }
            if(hud)hud.RefreshPosition();
        }
        void MoveCompanionWorld(FlatCompanionActor companion,Vector3 offset)
        {
            companion.transform.position+=offset;
            companion.groundY+=offset.y;
            companion.RefreshShadow();
        }
        void AddRenderedBounds(GameObject actor,List<Bounds> result)
        {
            var flat=actor.GetComponent<FlatCompanionActor>();
            if(flat && flat.Illustration) { result.Add(flat.VisibleBounds);return; }
            clearanceSprites.Clear();actor.GetComponentsInChildren<SpriteRenderer>(false,clearanceSprites);
            foreach(var part in clearanceSprites)
                if(part.enabled && part.sprite && part.gameObject.activeInHierarchy)result.Add(part.bounds);
        }
        void UpdateProtectedHudAreas(RectTransform design, float density, float bottom)
        {
            protectedHudAreas[0] = HudWorldRect(main.stageText ? main.stageText.rectTransform : null,
                main.stageText, design, density, bottom);
            protectedHudAreas[1] = HudWorldRect(main.roundText ? main.roundText.rectTransform : null,
                main.roundText, design, density, bottom);
            protectedHudAreas[2] = default;
            if (main.waveNodes != null && main.waveNodes.Length > 0)
            {
                bool found = false;
                foreach (var node in main.waveNodes)
                {
                    if (!node) continue;
                    var rect = HudWorldRect(node.rectTransform, null, design, density, bottom);
                    // Include the decorative rim and node punch outside its fill.
                    rect = Rect.MinMaxRect(rect.xMin-.12f,rect.yMin-.12f,rect.xMax+.12f,rect.yMax+.12f);
                    if (!found) { protectedHudAreas[2] = rect; found = true; }
                    else
                    {
                        var previous = protectedHudAreas[2];
                        protectedHudAreas[2] = Rect.MinMaxRect(Mathf.Min(previous.xMin,rect.xMin),
                            Mathf.Min(previous.yMin,rect.yMin),Mathf.Max(previous.xMax,rect.xMax),Mathf.Max(previous.yMax,rect.yMax));
                    }
                }
            }
            Array.Copy(protectedHudAreas,actorProtectedHudAreas,protectedHudAreas.Length);
            actorProtectedHudAreas[3]=HudWorldRect(main.fairyButton ? main.fairyButton.transform as RectTransform : null,
                null,design,density,bottom);
            actorProtectedHudAreas[4]=HudWorldRect(main.eventButton ? main.eventButton.transform as RectTransform : null,
                null,design,density,bottom);
            if(!skillStrip) {
                var strip=main.GetComponentInChildren<EquippedSkillHud>(true);
                if(strip)skillStrip=strip.transform as RectTransform;
            }
            for(int slot=0;slot<3;slot++) {
                var item=skillStrip?skillStrip.Find("Equipped battle skill "+slot) as RectTransform:null;
                actorProtectedHudAreas[5+slot]=item&&item.gameObject.activeInHierarchy?
                    HudWorldRect(item,null,design,density,bottom):default;
            }
            if(effects)effects.FocusedFoodHeaderBounds=HudWorldRect(main.profileButton?main.profileButton.transform as RectTransform:null,
                null,design,density,bottom);
            if (PlayerHud) PlayerHud.SetProtectedAreas(protectedHudAreas);
            if (EnemyHud) EnemyHud.SetProtectedAreas(protectedHudAreas);
        }
        Rect HudWorldRect(RectTransform ui, Text text, RectTransform design, float density, float bottom)
        {
            if (!ui) return default;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(design, ui);
            float width = text ? Mathf.Min(bounds.size.x, text.preferredWidth) : bounds.size.x;
            float left = bounds.center.x - design.rect.xMin - width / 2 - 10;
            float top = design.rect.yMax - bounds.max.y - 8;
            float height = bounds.size.y + 16;
            // Same logical-to-world mapping as the transparent battle viewport, including Safe Area.
            return new Rect(stageRoot.transform.position.x + (left - 540) / density,
                stageRoot.transform.position.y + (bottom - top - height) / density - .28f,
                (width + 20) / density, height / density);
        }
        void ResizeRenderTexture(float height)
        {
            // Tall screens expose more battlefield; rasterize its actual height instead of enlarging 440 rows.
            int pixelsHigh = Mathf.Clamp(Mathf.CeilToInt(height), 64, 2048);
            if (texture && texture.height == pixelsHigh) return;
            var previous = texture;
            texture = new RenderTexture(1080, pixelsHigh, 16, RenderTextureFormat.ARGB32) {
                name = "Moonlit combat transparent viewport", filterMode = FilterMode.Bilinear
            };
            texture.Create();
            renderCamera.targetTexture = texture; image.texture = texture;
            if (previous) { previous.Release(); Destroy(previous); }
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
                skills.Add(new CombatSkill { tier = entry.grade, variant = entry.variant, cooldown = entry.Cooldown,
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
                yield return new WaitForSeconds(1.5f);
            }
        }
        IEnumerator FightStage(int difficulty, int waves, Action<bool> complete)
        {
            CancelSkillCombo();
            WaveCount = waves;
            // A stage is one continuous player encounter; only its first wave refreshes the build and heals.
            PlayerState = BuildPlayer(); playerSkillsUsedThisTurn.Clear(); playerSkillActivations.Clear();
            playerDeathStarted = false;
            for (Wave = 1; Wave <= waves; Wave++)
            {
                Round = 1;
                PlayerResolvedBasicAttacks = EnemyResolvedBasicAttacks = 0;
                var stats = CombatRules.StageEnemy(difficulty, Wave);
                if (arenaRating >= 0 && IsExternalBattle && externalName == "아레나")
                {
                    double scale = .85 + Math.Max(0, arenaRating) / 1000.0;
                    stats = PlayerState.stats.Copy(); stats.health *= scale; stats.attack *= scale;
                    stats.speed = Math.Max(1, stats.speed * scale);
                }
                EnemyState = new CombatActorState(stats);
                enemyDeathStarted = false;
                if (Wave == 1) playerAnimator.Play("Idle", 0, 0);
                enemyAnimator.Play("Idle", 0, 0);
                yield return Wave == 1 ? Entrance() : EnemyEntrance();
                bool playerFirst = CombatRules.PlayerFirst(PlayerState.stats.speed, EnemyState.stats.speed, random.NextDouble());
                for (Round = 1; Round <= CombatRules.RoundsPerWave; Round++)
                {
                    PlayerState.Regenerate(); EnemyState.Regenerate();
                    yield return ActorTurn(playerFirst);
                    if (PlayerState.Alive && EnemyState.Alive) yield return ActorTurn(!playerFirst);
                    if (failedAnimation) { complete(false); yield break; }
                    if (!PlayerState.Alive || !EnemyState.Alive || CombatRules.RoundLimitLost(Round, EnemyState.Alive)) break;
                }
                bool wonWave = PlayerState.Alive && !EnemyState.Alive;
                // Lethal impacts already started Death. Only a round-limit loss still needs to start it.
                PlayDeathOnce(!wonWave);
                yield return WaitForAuthoredAnimation(wonWave ? enemyAnimator : playerAnimator, "Death");
                if (!wonWave) { complete(false); yield break; }
            }
            Wave = waves;
            complete(true);
        }
        IEnumerator Entrance() => EnterActors(true);
        IEnumerator EnemyEntrance() => EnterActors(false);
        IEnumerator EnterActors(bool includePlayer)
        {
            if (includePlayer) PlayerHud.SetVisible(false);
            EnemyHud.SetVisible(false);
            for (float t = 0; t < .45f; t += Time.deltaTime)
            {
                if (includePlayer) player.transform.localPosition = new Vector3(Mathf.Lerp(-5.5f, -2.5f, t / .45f), 0, 0);
                enemy.transform.localPosition = new Vector3(Mathf.Lerp(5.5f, 2.5f, t / .45f), 0, 0);
                yield return null;
            }
            if (includePlayer) player.transform.localPosition = new Vector3(-2.5f, 0, 0);
            enemy.transform.localPosition = new Vector3(2.5f, 0, 0);
            PlayerHud.SetVisible(true); EnemyHud.SetVisible(true);
        }
        IEnumerator ActorTurn(bool isPlayer)
        {
            var actor = isPlayer ? PlayerState : EnemyState;
            var target = isPlayer ? EnemyState : PlayerState;
            if (!actor.Alive || !target.Alive || failedAnimation) yield break;
            if (isPlayer) playerSkillsUsedThisTurn.Clear();
            foreach (var action in CombatRules.PlanTurn(actor, random.NextDouble))
            {
                if (!actor.Alive || !target.Alive || failedAnimation) yield break;
                var skill = action.skill;
                if (action.IsBasic)
                    yield return Strike(isPlayer, actor.stats.attack + actor.AttackBoost, false, 0);
                else if (skill.variant == 0)
                    yield return BuffAction(isPlayer, skill);
                else
                {
                    yield return StrikeTier(isPlayer, skill.damage, true, skill.variant, skill.tier);
                }
            }
        }

        IEnumerator BuffAction(bool isPlayer, CombatSkill skill)
        {
            var actor = isPlayer ? PlayerState : EnemyState;
            if (skill.tier >= 2)
            {
                yield return AnimatedAction(isPlayer, 1, "Buff", () => {
                    if (isPlayer) RecordPlayerSkill(skill);
                    double healing = actor.Heal(skill.heal);
                    actor.SetAttackBoost(Math.Max(actor.AttackBoost, skill.attackBoost));
                    ShowSkill(isPlayer, 0, skill.tier);
                    (isPlayer ? PlayerHud : EnemyHud).Float("+" + Format(healing), new Color(.4f, 1, .55f));
                });
                yield break;
            }
            CombatComboSequence sequence = null;
            ShowSkill(isPlayer, 0, skill.tier);
            yield return AnimatedAction(isPlayer, 1, "Buff", () => {
                float eventTime = effects.PlaybackElapsed;
                sequence = new CombatComboSequence(0,
                    CombatComboSequence.TimesAfterEvent(new[] { SkillChoreography.BuffHealTime(skill.tier) }, eventTime),
                    () => actor.Alive && ReferenceEquals(actor, isPlayer ? PlayerState : EnemyState) && isActiveAndEnabled,
                    (_, __) => {
                        if (isPlayer) RecordPlayerSkill(skill);
                        double healing = actor.Heal(skill.heal);
                        actor.SetAttackBoost(Math.Max(actor.AttackBoost, skill.attackBoost));
                        (isPlayer ? PlayerHud : EnemyHud).Float("+" + Format(healing), new Color(.4f, 1, .55f));
                    },
                    cancelled => { if (cancelled && effects) effects.CancelSkillPlayback(); },
                    Mathf.Max(0, Mathf.Max(SkillChoreography.BuffHealTime(skill.tier), SkillChoreography.BuffDuration(skill.tier)) - eventTime));
                activeCombo = sequence;
                sequence.Advance(0);
                if (sequence.Pending) StartCoroutine(AdvanceSkillCombo(sequence));
            }, () => sequence != null && sequence.Pending);
        }

        IEnumerator Strike(bool isPlayer, double damage, bool skill, int variant)
            => StrikeTier(isPlayer, damage, skill, variant, 0);
        IEnumerator StrikeTier(bool isPlayer, double damage, bool skill, int variant, int tier)
        {
            var actor = isPlayer ? PlayerState : EnemyState;
            var target = isPlayer ? EnemyState : PlayerState;
            CombatComboSequence combo = null;
            // Anticipation starts with the motion. Only its consumed event can arm the actual combo.
            if (skill) ShowSkill(isPlayer, variant, tier);
            yield return AnimatedAction(isPlayer, skill ? variant + 1 : 0, skill ? (variant == 1 ? "Weak" : "Strong") : "Basic", () => {
                if (!skill)
                {
                    if (isPlayer) PlayerResolvedBasicAttacks++; else EnemyResolvedBasicAttacks++;
                    ResolveStrike(isPlayer, actor, target, damage, false, variant, tier, 0);
                    return;
                }
                if (isPlayer)
                    foreach (var equipped in actor.skills)
                        if (equipped.tier == tier && equipped.variant == variant) RecordPlayerSkill(equipped);
                combo = new CombatComboSequence(damage, CombatComboSequence.TimesAfterEvent(SkillChoreography.HitTimes(tier, variant), effects.PlaybackElapsed),
                    () => actor.Alive && target.Alive && ReferenceEquals(actor, isPlayer ? PlayerState : EnemyState) &&
                        ReferenceEquals(target, isPlayer ? EnemyState : PlayerState) && isActiveAndEnabled,
                    (hitIndex, portion) => ResolveStrike(isPlayer, actor, target, portion, true, variant, tier, hitIndex),
                    cancelled => { if (cancelled && effects) effects.CancelSkillPlayback(); });
                activeCombo = combo;
                combo.Advance(0); // An early event only arms delayed contacts; it never deals guessed damage.
                if (combo.Pending) StartCoroutine(AdvanceSkillCombo(combo));
            }, () => combo != null && combo.Pending);
        }
        IEnumerator AdvanceSkillCombo(CombatComboSequence combo)
        {
            while (combo.Pending)
            {
                yield return null;
                combo.Advance(Time.deltaTime);
            }
        }
        void CancelSkillCombo()
        {
            activeCombo?.Cancel();
            activeCombo = null;
            if (effects) effects.CancelSkillPlayback();
        }
        void ResolveStrike(bool isPlayer, CombatActorState actor, CombatActorState target, double damage,
            bool skill, int variant, int tier, int hitIndex)
        {
            var sourceMotion = (isPlayer ? player : enemy).transform.Find("Motion");
            var victimMotion = (isPlayer ? enemy : player).transform.Find("Motion");
            Vector3 sourcePoint = sourceMotion.position + Vector3.up * 2.4f;
            Vector3 impactPoint = victimMotion.position + Vector3.up * 2.4f;
            if (!skill) effects.PlayBasicSlash(impactPoint, isPlayer);
            var hit = CombatRules.Strike(actor, target, damage, skill, random.NextDouble);
            if (skill) effects.PlaySkillHit(tier, variant, hitIndex, sourcePoint,
                tier <= 1 ? PrimitiveSkillEffects.FocusedTarget(victimMotion, impactPoint) : impactPoint,
                !hit.evaded && hit.damage > 0);
            if (!hit.evaded && hit.damage > 0)
            {
                (isPlayer ? enemyFlash : playerFlash).Play();
                effects.PlayHitDust(impactPoint);
                if (target.Alive) (isPlayer ? enemyAnimator : playerAnimator).Play("Hit", 0, 0);
                else PlayDeathOnce(!isPlayer);
            }
            (isPlayer ? EnemyHud : PlayerHud).Float(hit.evaded ? "회피" : Format(hit.damage),
                hit.critical ? new Color(1, .16f, .12f) : Color.white);
            if (hit.healing > 0) (isPlayer ? PlayerHud : EnemyHud).Float("+" + Format(hit.healing), new Color(.4f, 1, .55f));
        }
        void PlayDeathOnce(bool isPlayer)
        {
            if (isPlayer ? playerDeathStarted : enemyDeathStarted) return;
            if (isPlayer) playerDeathStarted = true; else enemyDeathStarted = true;
            (isPlayer ? playerRelay : enemyRelay).Cancel();
            (isPlayer ? playerAnimator : enemyAnimator).Play("Death", 0, 0);
        }
        IEnumerator AnimatedAction(bool isPlayer, int kind, string state, Action impact, Func<bool> pendingCombo = null)
        {
            if (failedAnimation) yield break;
            var relay = isPlayer ? playerRelay : enemyRelay;
            var animator = isPlayer ? playerAnimator : enemyAnimator;
            relay.Arm(kind, impact); animator.Play(state, 0, 0);
            yield return WaitForAuthoredAnimation(animator, state);
            if (relay.Pending)
            {
                // A missing animation event must never silently apply guessed damage.
                relay.Cancel(); CancelSkillCombo(); failedAnimation = true;
                main.Toast("전투 일시 정지 · 애니메이션 이벤트 확인 필요");
                Debug.LogError("[Moonlit] Missing OnCombatImpact event in Animator state " + state);
            }
            else
            {
                while (pendingCombo != null && pendingCombo()) yield return null;
                if ((isPlayer ? PlayerState : EnemyState).Alive) animator.Play("Idle", 0, 0);
            }
        }
        static IEnumerator WaitForAuthoredAnimation(Animator animator, string state)
        {
            // Play is evaluated on the next animation update. Let the native clip's own
            // length, speed and transition determine completion instead of cutting it at .8s.
            yield return null;
            int stateHash = Animator.StringToHash(state);
            while (animator && animator.isActiveAndEnabled)
            {
                var current = animator.GetCurrentAnimatorStateInfo(0);
                if (current.shortNameHash == stateHash)
                {
                    if (current.normalizedTime >= 1) yield break;
                }
                else if (!animator.IsInTransition(0) ||
                    animator.GetNextAnimatorStateInfo(0).shortNameHash != stateHash)
                    yield break;
                yield return null;
            }
        }
        void ShowSkill(bool isPlayer, int variant, int tier = 0, bool preview = false)
        {
            Transform sourceMotion = (isPlayer ? player : enemy).transform.Find("Motion");
            Transform targetMotion = (isPlayer ? enemy : player).transform.Find("Motion");
            Vector3 source = sourceMotion.position + Vector3.up * 2.4f;
            Vector3 target = targetMotion.position + Vector3.up * 2.4f;
            effects.Play(tier, variant, source, target, sourceMotion, targetMotion, preview || variant == 0);
        }
        public void PreviewPrimitiveSkill(int variant) => PreviewSkill(0, variant);
        public void PreviewSkill(int tier, int variant)
        {
            if (!effects) { main.Toast("전투 에셋 준비가 필요합니다"); return; }
            ShowSkill(true, Mathf.Clamp(variant, 0, 2), Mathf.Clamp(tier, 0, 9), true);
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
            if (!isActiveAndEnabled || !assets || !renderCamera || IsExternalBattle || failedAnimation) return false;
            if (battle != null) StopCoroutine(battle);
            CancelSkillCombo();
            playerRelay.Cancel(); enemyRelay.Cancel();
            AwaitingExternalClaim = false;
            IsExternalBattle = true; externalCallback = callback; externalName = title; arenaRating = rating;
            externalDifficulty = difficulty; externalWaves = waves;
            battle = StartCoroutine(ExternalLoop());
            return true;
        }
        IEnumerator ExternalLoop()
        {
            bool won = false;
            yield return FightStage(externalDifficulty, externalWaves, result => won = result);
            var callback = externalCallback; externalCallback = null;
            LastResult = externalName + (won ? " 클리어" : " 패배");
            AwaitingExternalClaim = won && externalName != "아레나";
            IsExternalBattle = AwaitingExternalClaim;
            // Set the wait state before invoking UI: an immediate claim is safe and exactly once.
            bool waitForClaim = AwaitingExternalClaim;
            if (waitForClaim) battle = null;
            callback?.Invoke(won);
            if (failedAnimation || waitForClaim) yield break;
            yield return new WaitForSeconds(1.5f);
            battle = StartCoroutine(NormalLoop());
        }
        public bool CompleteExternalClaim()
        {
            if (!AwaitingExternalClaim) return false;
            AwaitingExternalClaim = false; IsExternalBattle = false;
            if (battle != null) StopCoroutine(battle);
            battle = null;
            StartBattleWhenReady();
            return true;
        }
        void RefreshHealth()
        {
            if (PlayerState == null || EnemyState == null) return;
            PlayerHud.Bind(PlayerState); EnemyHud.Bind(EnemyState);
        }
        static string Format(double value) => MainScreen.Compact(Math.Ceiling(value));
        void OnDestroy()
        {
            CancelSkillCombo();
            externalCallback = null;
            if (renderCamera) renderCamera.targetTexture = null;
            if (texture) { texture.Release(); Destroy(texture); }
            if (stageRoot) Destroy(stageRoot);
        }
    }
}
