using System;
using System.IO;
using System.Linq;
using UnityEditor;
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
            ReferencePlayerAnimatorBuilder.ValidateCommitted();
            // The prefab Animator is the authority: never generate or replace user-authored clips.
            catalog.controller = prefab.GetComponent<Animator>().runtimeAnimatorController;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[Moonlit] Combat assets built from the actual Player prefab, 210 PSD sprites, the assigned user-authored Animator and three primitive VFX.");
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
