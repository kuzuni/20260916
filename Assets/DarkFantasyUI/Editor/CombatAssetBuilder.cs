using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Moonlit.UI;
using Object = UnityEngine.Object;

namespace Moonlit.Editor
{
    // Deliberately explicit: never runs from InitializeOnLoad or editor polling.
    // Invoke only in the authorized Unity 6000.3.8f1 cloud job.
    public sealed class CombatAssetBuilder : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;
        public void OnPreprocessBuild(BuildReport report) { Build(); }
        const string Root = "Assets/DarkFantasyUI/Resources/Moonlit/Combat";
        const string Art = "Assets/Art/ChihuahuaEquipmentThemes/";
        static readonly string[] Tiers = {
            "01_Primitive", "02_Medieval", "03_EarlyModern", "04_Modern", "05_Cyber",
            "06_Future", "07_Space", "08_Immortal", "09_Infinite", "10_Holy" };
        // Match EquipmentRules variant order.
        static readonly string[] Variants = EquipmentRules.VariantFolders;

        public static void Build()
        {
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "Reference/Player.prefab");
            if (!prefab) throw new InvalidOperationException("The supplied reference Player prefab is missing.");
            string catalogPath = Root + "/BattleAssets.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<BattleAssetCatalog>(catalogPath);
            if (!catalog)
            {
                catalog = ScriptableObject.CreateInstance<BattleAssetCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }
            catalog.playerPrefab = prefab;
            catalog.appearances = new BattleAppearanceSet[30];
            for (int tier = 0; tier < 10; tier++)
                for (int variant = 0; variant < 3; variant++)
                {
                    string path = Art + Tiers[tier] + "/" + Variants[variant] + "/rigging_layers.psb";
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                    if (sprites.Length != 7) throw new InvalidOperationException(path + " must import exactly seven parts; got " + sprites.Length);
                    Func<string, Sprite> part = name => sprites.FirstOrDefault(x => x.name == name)
                        ?? throw new InvalidOperationException(path + " missing imported part " + name);
                    catalog.appearances[tier * 3 + variant] = new BattleAppearanceSet {
                        tier = tier, variant = variant,
                        weapon = part("무기"), head = part("머리"), body = part("몸통"),
                        arm1 = part("팔1"), arm2 = part("팔2"), leg1 = part("다리1"), leg2 = part("다리2")
                    };
                }
            ImportEraSkillArt();
            catalog.buffSprite = SpriteAsset("AncestralBlessing", 0);
            catalog.weakSprite = SpriteAsset("StoneCrescent", 1);
            catalog.strongSprite = SpriteAsset("FallingBoulder", 2);
            string materialPath = Root + "/PrimitiveEffects.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material)
            {
                var shader = Shader.Find("Sprites/Default");
                if (!shader) throw new InvalidOperationException("Sprites/Default shader missing.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            catalog.effectMaterial = material;
            catalog.controller = BuildController(prefab);
            ReferencePlayerAnimatorBuilder.Build();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[Moonlit] Combat assets built from the actual Player prefab, 210 PSD sprites, seven Animator states and three primitive VFX.");
        }

        static void ImportEraSkillArt()
        {
            for (int tier = 0; tier < 10; tier++)
                foreach (string variant in new[] { "Buff", "Weak", "Strong" })
                {
                    string path = Root + "/Skills/Tier" + tier.ToString("00") + "/" + variant + ".png";
                    if (!File.Exists(path)) continue; // The art acceptance test reports incomplete production bundles.
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (!importer) { AssetDatabase.ImportAsset(path); importer = AssetImporter.GetAtPath(path) as TextureImporter; }
                    if (!importer) throw new InvalidOperationException("Cannot import skill sprite " + path);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 128;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings); importer.SaveAndReimport();
                }
        }

        static RuntimeAnimatorController BuildController(GameObject prefab)
        {
            string path = Root + "/PlayerCombat.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            foreach (var child in machine.states) machine.RemoveState(child.state);
            var source = Object.Instantiate(prefab);
            source.name = "PlayerRig";
            try
            {
                foreach (string name in new[] { "Idle", "Basic", "Hit", "Buff", "Weak", "Strong", "Death" })
                {
                    string clipPath = Root + "/" + name + ".anim";
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, clipPath); }
                    clip.ClearCurves(); clip.name = name; clip.frameRate = 30;
                    float duration = name == "Idle" ? 1.6f : .72f;
                    Curve(clip, "Motion", "m_LocalPosition.x", 0, name == "Basic" ? .7f : name == "Weak" ? .35f : name == "Hit" ? -.12f : 0, 0, duration);
                    Curve(clip, "Motion", "m_LocalPosition.y", 0, name == "Buff" ? .15f : name == "Strong" ? .35f : name == "Idle" ? .035f : 0,
                        name == "Death" ? -.4f : 0, duration);
                    Curve(clip, "Motion", "localEulerAnglesRaw.z", 0, name == "Death" ? -55 : name == "Hit" ? 6 : name == "Basic" ? -7 : 0,
                        name == "Death" ? -85 : 0, duration);
                    foreach (var bone in source.GetComponentsInChildren<Transform>(true))
                    {
                        float swing = 0;
                        if (bone.name == "bone_7") swing = name == "Basic" ? -75 : name == "Weak" ? -45 : name == "Strong" ? -120 : name == "Buff" ? 50 : 0;
                        if (bone.name == "bone_5") swing = name == "Basic" ? 20 : name == "Strong" ? 45 : name == "Buff" ? -30 : 0;
                        if (bone.name == "bone_2") swing = name == "Hit" ? 8 : name == "Buff" ? -8 : 0;
                        if (bone.name == "bone_4" || bone.name == "bone_3") swing = name == "Basic" ? 7 : 0;
                        if (!bone.name.StartsWith("bone_", StringComparison.Ordinal)) continue;
                        string bonePath = "Motion/PlayerRig/" + AnimationUtility.CalculateTransformPath(bone, source.transform);
                        float angle = bone.localEulerAngles.z;
                        Curve(clip, bonePath, "localEulerAnglesRaw.z", angle, angle + swing, angle, duration);
                    }
                    var settings = AnimationUtility.GetAnimationClipSettings(clip);
                    settings.loopTime = name == "Idle";
                    AnimationUtility.SetAnimationClipSettings(clip, settings);
                    int kind = name == "Basic" ? 0 : name == "Buff" ? 1 : name == "Weak" ? 2 : name == "Strong" ? 3 : -1;
                    AnimationUtility.SetAnimationEvents(clip, kind < 0 ? Array.Empty<AnimationEvent>() : new[] {
                        new AnimationEvent { time = kind >= 2 ? PrimitiveSkillEffects.AttackFlightDuration : .3f,
                            functionName = "OnCombatImpact", intParameter = kind }
                    });
                    var state = machine.AddState(name); state.motion = clip;
                    state.writeDefaultValues = true;
                    if (name == "Idle") machine.defaultState = state;
                    EditorUtility.SetDirty(clip);
                }
                EditorUtility.SetDirty(controller);
                return controller;
            }
            finally { Object.DestroyImmediate(source); }
        }
        static void Curve(AnimationClip clip, string path, string property, float start, float middle, float end, float duration)
        {
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                new AnimationCurve(new Keyframe(0, start), new Keyframe(duration * .4f, middle), new Keyframe(duration, end)));
        }

        static Sprite SpriteAsset(string name, int variant)
        {
            string path = Root + "/" + name + ".png";
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float u = (x - 63.5f) / 58, v = (y - 63.5f) / 58, radius = Mathf.Sqrt(u * u + v * v);
                    float angle = Mathf.Atan2(v, u);
                    float alpha = 0;
                    Color color;
                    if (variant == 0)
                    {
                        float ring = Mathf.Abs(radius - .67f);
                        bool cross = (Mathf.Abs(u) < .1f && Mathf.Abs(v) < .42f) || (Mathf.Abs(v) < .1f && Mathf.Abs(u) < .42f);
                        alpha = cross ? 1 : Mathf.Clamp01((.065f - ring) * 35);
                        if (Mathf.Cos(angle * 8) > .85f && radius > .77f && radius < .94f) alpha = 1;
                        color = Color.Lerp(new Color(.08f, .58f, .3f), new Color(.8f, 1, .62f), (v + 1) / 2);
                    }
                    else if (variant == 1)
                    {
                        float outer = Mathf.Clamp01((.92f - radius) * 30);
                        float cutout = Mathf.Sqrt((u - .26f) * (u - .26f) + (v - .05f) * (v - .05f));
                        alpha = outer * Mathf.Clamp01((cutout - .67f) * 30);
                        color = Color.Lerp(new Color(.66f, .36f, .13f), new Color(1, .95f, .65f), Mathf.Clamp01(radius));
                    }
                    else
                    {
                        float jagged = .7f + Mathf.Sin(angle * 7) * .075f + Mathf.Cos(angle * 11) * .035f;
                        alpha = Mathf.Clamp01((jagged - radius) * 30);
                        bool crack = Mathf.Abs(u + v * .33f) < .028f || (v < 0 && Mathf.Abs(v - u * .7f + .17f) < .025f);
                        color = crack ? new Color(1, .48f, .13f) : Color.Lerp(new Color(.21f, .13f, .1f), new Color(.66f, .43f, .23f), (v + 1) / 2);
                    }
                    color.a = alpha; texture.SetPixel(x, y, color);
                }
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
