# Cloud task 02 — skills/pets/heroes and dungeons (7 references)

Implement ProgressionScreenModule.Register per ARCHITECTURE.md. Own only Assets/DarkFantasyUI/Scripts/Screens/Progression/**, Art/Screens/Progression/** with metas, and Documentation/UI/Reports/progression.md. Do not edit shared files.

Routes/reference IDs: 19 skills-pets-heroes (Page); 14 skill-details (Modal); 15 summon-probability (Modal); 16 summon-probability-details (Modal); 17 summon-result (Fullscreen); 20 dungeons (Page); 04 dungeon-details (Modal).

Inspect original PNGs. Build illustrated round skill grids with separate icon/frame/level/progress/selection, equipped row, upgrade/quick equip, summon x5 control with cost, real tabs 스킬/펫/영웅. User explicitly renamed 기술트리 to 영웅. Pet/hero tab content is an inference, use cohesive local demo state and record that no separate reference was supplied.
Probability flows preserve parent state; child close returns to preceding dialog/tab. Summon result displays five separate icons with reusable reveal effects and a way back, no baked screenshot. Dungeon page uses four illustrated row cards with key counts; Open shows selected dungeon details, stage arrows, reward, entry controls. Local demo interactions must work, with resource checks and no backend/combat claims.

Use generated artwork for missing icons/banner art if available, preserve prompts; list gaps honestly. Never use the screenshots as production textures. Cross-module navigation only through context.Open.

Deliver code/report including registrations, main entry hooks, checks and visual evidence or limitations. Foundation types come from a parallel task; do not add conflicting stub types. Do not merge main.
