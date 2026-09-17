using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Moonlit.Editor
{
    // Explicit cloud asset preparation. Never runs when the local editor opens.
    public sealed class CelestialThumbnailBuilder : IPreprocessBuildWithReport
    {
        public const string Output = "Assets/DarkFantasyUI/Resources/Moonlit/Forge/CelestialThumbnails";
        const string Source = "Assets/Art/ChihuahuaEquipmentThemes/10_Holy/";
        public int callbackOrder => -110;
        public void OnPreprocessBuild(BuildReport report) { Build(); }

        [Serializable] sealed class LayerManifest { public int[] canvas; public LayerPart[] parts; }
        [Serializable] sealed class LayerPart { public string key; public int[] bbox; }

        public static void Build()
        {
            foreach (string variant in Moonlit.UI.EquipmentRules.VariantFolders)
            {
                string source = Source + variant + "/";
                var manifest = JsonUtility.FromJson<LayerManifest>(File.ReadAllText(source + "layers.json"));
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(File.ReadAllBytes(source + "rigging_original.png")))
                    throw new InvalidDataException("Cannot read celestial source " + source);
                try
                {
                    if (manifest.canvas == null || texture.width != manifest.canvas[0] || texture.height != manifest.canvas[1])
                        throw new InvalidDataException("Celestial source dimensions disagree with the supplied part manifest.");
                    WritePart(texture, manifest.parts.First(p => p.key == "Body"), variant, "armor");
                    WritePart(texture, manifest.parts.First(p => p.key == "Weapon"), variant, "weapon");
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string variant in Moonlit.UI.EquipmentRules.VariantFolders)
            foreach (string part in new[] { "armor", "earring", "hat", "necklace", "ring", "weapon" })
            {
                string path = Output + "/" + variant + "/" + part + ".png";
                if (!File.Exists(path)) throw new FileNotFoundException("Missing celestial equipment illustration", path);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (!importer) throw new InvalidDataException("No TextureImporter for " + path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
                if (!AssetDatabase.LoadAssetAtPath<Sprite>(path))
                    throw new InvalidDataException("Celestial sprite import failed: " + path);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("CelestialThumbnailBuilder: 18 illustrations imported; six exact source crops and twelve generated isolated items.");
        }

        static void WritePart(Texture2D source, LayerPart part, string variant, string name)
        {
            int[] box = part.bbox;
            int width = box[2] - box[0], height = box[3] - box[1];
            if (width <= 0 || height <= 0 || width > 512 || height > 512)
                throw new InvalidDataException("Source part does not fit its lossless 512px thumbnail.");
            var output = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            try
            {
                output.SetPixels32(new Color32[512 * 512]);
                // The supplied manifest uses image top-left; Unity texture pixels use bottom-left.
                output.SetPixels((512 - width) / 2, (512 - height) / 2, width, height,
                    source.GetPixels(box[0], source.height - box[3], width, height));
                output.Apply();
                string directory = Output + "/" + variant;
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(directory + "/" + name + ".png", output.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(output); }
        }
    }
}
