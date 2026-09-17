# Turn combat and primitive skill preview

Implements the 2026-09-18 gameplay request in new owned files only. User's latest gameplay request supersedes the earlier empty-combat-view visual constraint.

## Integration

- Factory: after routes are ready, attach BattleRuntime to MainScreen and call Initialize(main, assets).
- MainScreen exposes SaveGame(); normal outcomes update main.stage, call Refresh(), then SaveGame().
- MainScreen bridge calls StartDungeon(index, difficulty, waveCount, callback) or StartArena(opponentRating, callback). Both return bool: false means no encounter started and no entry key should be consumed. On success the completion callback runs once. Close base pages to show battle.
- Skill detail preview bridge calls PreviewPrimitiveSkill(0/1/2).
- Forge API: ForgeState.Current.TotalStats, equipped, AffixTotal; EquipmentRoll fields id/tier/variant/part. Variant order Thief, Warrior, Assassin.
- Collections API: static owned/equipped HP and attack totals and EquippedSkills with fixed values/cooldowns.
- Do not run local Unity. Cloud explicitly invokes Moonlit.Editor.CombatAssetBuilder.Build before tests/captures. This imports the supplied Player prefab and PSBs into a catalog, builds seven Animator clips and three procedural sprite assets. Preserve the generated Resources/Moonlit/Combat folder from cloud as deliverable assets. Missing assets stop combat visibly rather than report a synthetic success.
- A separate offscreen camera renders actor SpriteRenderers, particles and trails to a transparent RawImage within the existing shared UI canvas. It is not a competing fullscreen camera. Real UI page/modal ordering, safe-area constraints, and click blocking remain in the existing host.

## Implemented behavior

Player appears from left and enemy from right. Both instantiate the actual supplied Player prefab. Normal stages have three one-enemy waves. Faster speed acts first; a single fair tie coin determines initiative for that wave. Each round gives both surviving actors one turn. Fifteen completed rounds with a surviving enemy lose; a killing blow in round fifteen wins. Stage win increments and stage loss decrements, clamped at one. External dungeon/arena fights resume the normal stage afterward and do not mutate normal progression.

Extra basic attack is rolled once per actor turn with a hard maximum of one extra attack. Health regeneration runs at each round start using maximum HP. Lifesteal uses actual removed HP, including overkill clamp. Chance percentages cap at 100. Critical chance/damage apply to basic hits; baseline critical multiplier is 150%, plus the affix. Skill damage uses fixed equipment-derived numbers and the skill-damage affix.

Buff executes before basics, weak/strong after basics including the extra basic. First triggers are the third/second/fifth actor turns respectively. Later triggers follow those periods. Buff heals immediately and retains the strongest fixed attack buff until wave end without accumulating repeat casts. Every wave begins with full health and resets cooldowns. These timing/recovery/buff-duration choices fill unspecified details and are centralized for adjustment. The naked player has a minimum 80 HP, 10 attack and 1 speed; equipped/collection totals replace those minima, rather than adding invisible full-set stats.

Normal enemy curve is a transparent, deterministic local balance draft: wave HP 45/53/61, attack 5/6/7, multiplied by 1.085 per stage; speed equals stage. Dungeon difficulty uses normal stage difficulty x5 per dungeon stage. Arena opponents use a local rating multiplier. There is no server or real PvP.

## Artwork and animation status

The supplied Player prefab had no gameplay Animator clips. Cloud builder authors explicit preview Idle/Basic/Hit/Buff/Weak/Strong/Death motions from the actual bone rest pose, plus root motion curves. Basic/Buff/Weak/Strong clips carry OnCombatImpact(int) events. The relay rejects wrong-kind events, consumes each action only once, and cancels interrupted actions. A missing impact event stops combat and emits an error; no timer silently substitutes damage.

Armor swaps torso and four limb sprites, hat swaps head, weapon swaps weapon; the seven imported sprites share the reference skeleton. SpriteSkin automatic name rebinding is enabled. Player reference and original PSBs remain untouched. Artist-authored animation polish is still an explicit limitation.

Exactly three primitive VFX previews exist: ancestral blessing (green rune/heal), stone crescent (ochre arc), falling boulder (cracked glowing rock). Each uses a separate procedural 2D sprite, ParticleSystem and TrailRenderer. Their sprite pixels are authored directly by deterministic Unity editor code; they are code-built effects, not claims of generated illustration. No higher-tier skill effects or pet/mount artwork are fabricated.

## Verification status

Added deterministic tests cover initiative/tie boundary, round/stage boundaries, once-only double chance, cooldown ordering, actual-damage lifesteal, regeneration, evasion, fixed skill numbers, and event gating. A PlayMode test plays the generated Animator and requires actual AnimationEvents to consume armed hits. Catalog tests require all 30 sets / 210 part sprites and three distinct VFX sprites.

At branch delivery, tests are authored but not locally run. Unity 6000.3.8f1 cloud compile/tests and per-aspect captures are mandatory pending coordinator integration. The generated assets and sprite rebind/rig appearance require cloud evidence. No local Unity execution/control, account secret reading, or _Recovery changes occurred.
