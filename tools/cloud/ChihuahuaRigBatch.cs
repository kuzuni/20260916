// Runs only in the disposable Linux cloud project, never the local editor.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.U2D.Animation;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D;
using Unity.Mathematics;

public static class ChihuahuaRigBatch
{
    const string Root = "Assets/Art/ChihuahuaEquipmentThemes";
    const string Reference = Root + "/Reference/character_base.psb";
    static readonly BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    static Assembly AnimationAssembly => typeof(ICharacterDataProvider).Assembly;
    static Type T(string name) => AnimationAssembly.GetType("UnityEditor.U2D.Animation." + name, true);
    static object New(string name) => Activator.CreateInstance(T(name), true);
    static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Flags).Invoke(obj, args);
    static object Get(object obj, string name) => obj.GetType().GetProperty(name, Flags).GetValue(obj);
    static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Flags).SetValue(obj, value);
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }

    [Serializable] public class LayerReport
    {
        public string name;
        public string spriteId;
        public RectInt spritePosition;
        public SpriteBone[] bones;
        public Vertex2DMetaData[] vertices;
        public int[] indices;
        public Vector2Int[] edges;
        public float maxWeightSumError;
    }
    [Serializable] public class FileReport
    {
        public string path;
        public string assetGuid;
        public int skeletonBoneCount;
        public LayerReport[] layers;
    }
    [Serializable] public class Report
    {
        public string unityVersion;
        public string geometryAlgorithm = "Unity 2D Animation OutlineGenerator + Triangulator (Auto Geometry)";
        public int outlineDetail = 10;
        public int alphaTolerance = 10;
        public int subdivisions = 0;
        public bool generateWeights = true;
        public int processedFiles;
        public FileReport[] files;
    }

    static ISpriteEditorDataProvider Provider(string path)
    {
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(AssetImporter.GetAtPath(path));
        Require(provider != null, "Missing sprite data provider: " + path);
        provider.InitSpriteEditorDataProvider();
        return provider;
    }

    static void WorldBone(List<SpriteBone> bones, int index, out Vector3 position, out Quaternion rotation)
    {
        var bone = bones[index];
        if (bone.parentId < 0) { position = bone.position; rotation = bone.rotation; return; }
        Require(bone.parentId < bones.Count && bone.parentId != index, "Invalid bone parent");
        WorldBone(bones, bone.parentId, out var parentPosition, out var parentRotation);
        position = parentPosition + parentRotation * bone.position;
        rotation = parentRotation * bone.rotation;
    }

    static LayerReport Generate(ISpriteEditorDataProvider provider, SpriteRect rect, CharacterPart part, List<SpriteBone> bones)
    {
        var mesh = New("SpriteMeshData");
        Call(mesh, "SetFrame", rect.rect);
        var boneData = (IList)Get(mesh, "bones");
        for (int i = 0; i < bones.Count; ++i)
        {
            var b = bones[i];
            WorldBone(bones, i, out var position, out var rotation);
            var item = New("SpriteBoneData");
            Set(item, "parentId", b.parentId);
            Set(item, "localPosition", (Vector2)b.position);
            Set(item, "localRotation", b.rotation);
            Set(item, "position", (Vector2)position);
            Set(item, "endPosition", (Vector2)(position + rotation * Vector3.right * b.length));
            Set(item, "depth", position.z);
            Set(item, "length", b.length);
            boneData.Add(item);
        }
        var controller = New("SpriteMeshDataController");
        Set(controller, "spriteMeshData", mesh);
        Call(controller, "OutlineFromAlpha", New("OutlineGenerator"), provider.GetDataProvider<ITextureDataProvider>(), 0.1f, (byte)10);
        Require((int)Get(mesh, "vertexCount") > 4, "Alpha outline was not generated: " + rect.name);
        Call(controller, "Triangulate", New("Triangulator"));
        var positions = (Vector2[])Get(mesh, "vertices");
        var indices = (int[])Get(mesh, "indices");
        var edges = ((int2[])Get(mesh, "edges")).Select(e => new Vector2Int(e.x, e.y)).ToArray();
        Require(positions.Length > 4 && indices.Length >= 3 && indices.Length % 3 == 0, "Invalid triangulation: " + rect.name);
        if (bones.Count > 1)
            Call(controller, "CalculateWeights", New("BoundedBiharmonicWeightsGenerator"), null, 0.1f);
        var weights = (Array)Get(mesh, "vertexWeights");
        var convert = T("EditableBoneWeightUtility").GetMethod("ToBoneWeight", BindingFlags.Static | BindingFlags.Public);
        var vertices = new Vertex2DMetaData[positions.Length];
        float maxError = 0;
        for (int i = 0; i < vertices.Length; ++i)
        {
            var weight = bones.Count == 1 ? new BoneWeight { weight0 = 1, boneIndex0 = 0 } : (BoneWeight)convert.Invoke(null, new object[] { weights.GetValue(i), true });
            var values = new[] { weight.weight0, weight.weight1, weight.weight2, weight.weight3 };
            var boneIndices = new[] { weight.boneIndex0, weight.boneIndex1, weight.boneIndex2, weight.boneIndex3 };
            for (int j = 0; j < 4; ++j)
                Require(!float.IsNaN(values[j]) && !float.IsInfinity(values[j]) && values[j] >= 0 && (values[j] == 0 || boneIndices[j] >= 0 && boneIndices[j] < bones.Count), "Invalid bone weight");
            maxError = Mathf.Max(maxError, Mathf.Abs(values.Sum() - 1));
            Require(positions[i].x >= -1 && positions[i].y >= -1 && positions[i].x <= rect.rect.width + 1 && positions[i].y <= rect.rect.height + 1, "Vertex outside sprite bounds");
            vertices[i] = new Vertex2DMetaData { position = positions[i], boneWeight = weight };
        }
        Require(maxError < 0.001f, "Unweighted geometry: " + rect.name);
        Require(indices.All(i => i >= 0 && i < vertices.Length), "Invalid triangle index");
        var meshProvider = provider.GetDataProvider<ISpriteMeshDataProvider>();
        meshProvider.SetVertices(rect.spriteID, vertices);
        meshProvider.SetIndices(rect.spriteID, indices);
        meshProvider.SetEdges(rect.spriteID, edges);
        return new LayerReport { name = rect.name, spriteId = rect.spriteID.ToString(), spritePosition = part.spritePosition,
            bones = bones.ToArray(), vertices = vertices, indices = indices, edges = edges, maxWeightSumError = maxError };
    }

    public static void Run()
    {
        try
        {
            Require(Application.isBatchMode && Application.platform == RuntimePlatform.LinuxEditor && Environment.GetCommandLineArgs().Contains("-moonlitCloudRig"), "Cloud-only command refused");
            Require(Application.unityVersion == "6000.3.8f1", "Wrong Unity version");
            var referenceBytes = File.ReadAllBytes(Reference + ".meta");
            var referenceProvider = Provider(Reference);
            var referenceRects = referenceProvider.GetSpriteRects().ToDictionary(r => r.name);
            var referenceCharacter = referenceProvider.GetDataProvider<ICharacterDataProvider>().GetCharacterData();
            var referenceBones = referenceProvider.GetDataProvider<ISpriteBoneDataProvider>();
            Require(referenceCharacter.bones.Length == 11 && referenceCharacter.parts.Length == 7, "Reference must have 11 bones and 7 parts");
            var paths = Directory.GetFiles(Root, "rigging_layers.psb", SearchOption.AllDirectories).OrderBy(p => p).ToArray();
            Require(paths.Length == 30, "Expected 30 target PSBs");
            var reports = new List<FileReport>();
            foreach (var path in paths)
            {
                var provider = Provider(path);
                var rects = provider.GetSpriteRects();
                Require(rects.Length == 7 && rects.All(r => referenceRects.ContainsKey(r.name)), "Unexpected parts in " + path);
                var characterProvider = provider.GetDataProvider<ICharacterDataProvider>();
                var character = characterProvider.GetCharacterData();
                var boneProvider = provider.GetDataProvider<ISpriteBoneDataProvider>();
                var layerReports = new List<LayerReport>();
                var parts = new List<CharacterPart>();
                foreach (var rect in rects)
                {
                    var rr = referenceRects[rect.name];
                    var rp = referenceCharacter.parts.Single(p => p.spriteId == rr.spriteID.ToString());
                    var targetPart = character.parts.Single(p => p.spriteId == rect.spriteID.ToString());
                    var bones = referenceBones.GetBones(rr.spriteID).ToList();
                    var offset = (Vector2)(rp.spritePosition.position - targetPart.spritePosition.position);
                    for (int i = 0; i < bones.Count; ++i)
                    {
                        var b = bones[i];
                        if (b.parentId < 0) b.position += (Vector3)offset;
                        bones[i] = b;
                    }
                    boneProvider.SetBones(rect.spriteID, bones);
                    targetPart.bones = (int[])rp.bones.Clone();
                    targetPart.parentGroup = rp.parentGroup;
                    targetPart.order = rp.order;
                    parts.Add(targetPart);
                    layerReports.Add(Generate(provider, rect, targetPart, bones));
                }
                character.bones = (SpriteBone[])referenceCharacter.bones.Clone();
                character.parts = parts.ToArray();
                character.characterGroups = (CharacterGroup[])referenceCharacter.characterGroups.Clone();
                character.pivot = referenceCharacter.pivot;
                characterProvider.SetCharacterData(character);
                provider.Apply();
                EditorUtility.SetDirty(provider.targetObject);
                AssetDatabase.WriteImportSettingsIfDirty(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                var verify = Provider(path);
                var vc = verify.GetDataProvider<ICharacterDataProvider>().GetCharacterData();
                Require(JsonUtility.ToJson(new BoneArray { bones = vc.bones }) == JsonUtility.ToJson(new BoneArray { bones = referenceCharacter.bones }), "Skeleton changed after import: " + path);
                foreach (var layer in layerReports)
                {
                    var guid = new GUID(layer.spriteId);
                    var vm = verify.GetDataProvider<ISpriteMeshDataProvider>();
                    Require(vm.GetVertices(guid).Length == layer.vertices.Length && vm.GetIndices(guid).SequenceEqual(layer.indices), "Mesh did not survive reimport: " + layer.name);
                    Require(verify.GetDataProvider<ISpriteBoneDataProvider>().GetBones(guid).Count == layer.bones.Length, "Bones did not survive reimport");
                }
                var output = Path.Combine("RiggingOutput", path + ".meta");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.Copy(path + ".meta", output, true);
                reports.Add(new FileReport { path = path, assetGuid = AssetDatabase.AssetPathToGUID(path), skeletonBoneCount = vc.bones.Length, layers = layerReports.ToArray() });
                Debug.Log("RIG_PASS " + path + " 11 bones / 7 independently generated weighted meshes");
            }
            Require(File.ReadAllBytes(Reference + ".meta").SequenceEqual(referenceBytes), "Reference metadata modified");
            var report = new Report { unityVersion = Application.unityVersion, processedFiles = reports.Count, files = reports.ToArray() };
            File.WriteAllText("RiggingOutput/verification.json", JsonUtility.ToJson(report, true));
            File.WriteAllText("RiggingOutput/SUCCESS.txt", "30 PSBs; 11 reference bones each; 210 auto-generated weighted meshes; reimport checks passed.\n");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Directory.CreateDirectory("RiggingOutput");
            File.WriteAllText("RiggingOutput/FAILURE.txt", exception.ToString());
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }
    [Serializable] class BoneArray { public SpriteBone[] bones; }
}
