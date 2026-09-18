using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;
using Moonlit.UI;
using Object=UnityEngine.Object;

namespace Moonlit.Editor
{
    // Explicit cloud preparation only. Source bitmap pixels are never rewritten.
    public static class CompanionAssetBuilder
    {
        const string Root="Assets/DarkFantasyUI/Resources/Moonlit/Companions";
        const string Parts="Assets/DarkFantasyUI/Resources/Moonlit/CompanionParts";
        static readonly string[] Keys={"Pet0","Pet1","Pet2","Mount0","Mount1","Mount2"};
        static readonly string[] Names={"원시 꼬마","검치호 새끼","새끼 익룡","원시 랩터","야생 멧돼지","돌바퀴 수레"};
        static readonly CompanionRigType[] Types={CompanionRigType.Humanoid,CompanionRigType.Quadruped,CompanionRigType.Bird,
            CompanionRigType.BipedMount,CompanionRigType.QuadrupedMount,CompanionRigType.Vehicle};
        public static void Build()
        {
            Directory.CreateDirectory(Root+"/Rigs");AssetDatabase.Refresh();
            var catalog=AssetDatabase.LoadAssetAtPath<CompanionRigCatalog>(Root+"/CompanionRigCatalog.asset");
            if(!catalog){catalog=ScriptableObject.CreateInstance<CompanionRigCatalog>();AssetDatabase.CreateAsset(catalog,Root+"/CompanionRigCatalog.asset");}
            catalog.definitions=CompanionRigTemplates.All();catalog.entries=new CompanionRigEntry[6];
            for(int i=0;i<Keys.Length;i++)
            {
                string path=Parts+"/"+Keys[i]+".png";
                var texture=ImportTexture(path,true);
                var regions=FindParts(texture);
                var definition=catalog.definitions[(int)Types[i]];
                var prefab=BuildPrefab(Keys[i],texture,regions,definition);
                ImportTexture(Root+"/"+Keys[i]+".png",false);
                catalog.entries[i]=new CompanionRigEntry {category=i<3?1:2,variant=i%3,type=Types[i],displayName=Names[i],
                    icon=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/"+Keys[i]+".png"),prefab=prefab};
            }
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            ValidateSavedMeshes(catalog);
            Debug.Log("[Moonlit] Built six primitive companions from 48 separated SpriteSkin parts and ten reusable skeleton definitions.");
        }
        static Texture2D ImportTexture(string path,bool readable)
        {
            if(!File.Exists(path))throw new InvalidOperationException("Missing companion illustration "+path);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer){AssetDatabase.ImportAsset(path);importer=(TextureImporter)AssetImporter.GetAtPath(path);}
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.isReadable=readable;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static RectInt[] FindParts(Texture2D texture)
        {
            // Connected alpha regions preserve hair/wing tips that cross an approximate sheet cell boundary.
            var pixels=texture.GetPixels32();int w=texture.width,h=texture.height;
            var seen=new bool[pixels.Length];var queue=new Queue<int>();var regions=new List<RectInt>();
            int minPixels=Math.Max(24,w*h/20000);
            for(int start=0;start<pixels.Length;start++)
            {
                if(seen[start] || pixels[start].a<12)continue;
                queue.Enqueue(start);seen[start]=true;int minX=w,minY=h,maxX=0,maxY=0,count=0;
                while(queue.Count>0)
                {
                    int index=queue.Dequeue(),x=index%w,y=index/w;count++;
                    minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        int xx=x+dx,yy=y+dy;if(xx<0 || yy<0 || xx>=w || yy>=h)continue;
                        int neighbor=yy*w+xx;
                        if(!seen[neighbor] && pixels[neighbor].a>=12){seen[neighbor]=true;queue.Enqueue(neighbor);}
                    }
                }
                if(count>=minPixels)regions.Add(new RectInt(Math.Max(0,minX-2),Math.Max(0,minY-2),
                    Math.Min(w-1,maxX+2)-Math.Max(0,minX-2)+1,Math.Min(h-1,maxY+2)-Math.Max(0,minY-2)+1));
            }
            if(regions.Count!=8)throw new InvalidOperationException(texture.name+" needs eight detached illustrated parts; found "+regions.Count);
            var upper=regions.OrderByDescending(rect=>rect.center.y).Take(4).OrderBy(rect=>rect.center.x);
            var lower=regions.OrderBy(rect=>rect.center.y).Take(4).OrderBy(rect=>rect.center.x);
            return upper.Concat(lower).ToArray();
        }
        static GameObject BuildPrefab(string key,Texture2D texture,RectInt[] regions,CompanionSkeletonDefinition definition)
        {
            string directory=Root+"/Rigs/"+key;Directory.CreateDirectory(directory);AssetDatabase.Refresh();
            var root=new GameObject(key+" skeletal rig");root.SetActive(false);
            try
            {
                var actor=root.AddComponent<CompanionActor>();actor.rigType=definition.type;actor.shadowWidth=definition.shadowWidth;
                root.AddComponent<SortingGroup>();
                var bones=new Transform[definition.bones.Length];
                for(int i=0;i<bones.Length;i++)
                {
                    var spec=definition.bones[i];var go=new GameObject(spec.name);
                    go.transform.SetParent(spec.parent<0?root.transform:bones[spec.parent],false);
                    var parent=spec.parent<0?Vector2.zero:definition.bones[spec.parent].position;
                    go.transform.localPosition=spec.position-parent;bones[i]=go.transform;
                }
                actor.skeletonRoot=bones[0];actor.bones=bones;
                var saddle=new GameObject("Saddle");saddle.transform.SetParent(bones[1],false);
                saddle.transform.localPosition=definition.saddle-definition.bones[1].position;actor.saddle=saddle.transform;
                var skins=new List<SpriteSkin>();
                foreach(var part in definition.parts)
                {
                    RectInt region=regions[part.cell];
                    var sprite=Sprite.Create(texture,new Rect(region.x,region.y,region.width,region.height),part.pivot,region.height/part.height,0,SpriteMeshType.FullRect);
                    sprite.name=key+"_"+part.name;
                    var go=new GameObject(part.name+" sprite");go.transform.SetParent(root.transform,false);
                    go.transform.localPosition=definition.bones[part.bone].position;
                    var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=part.order;
                    renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/DarkFantasyUI/Resources/Moonlit/Combat/PrimitiveEffects.mat");
                    var influences=part.tip<0?new[]{bones[part.bone]}:new[]{bones[part.bone],bones[part.tip]};
                    SkinSprite(sprite,part,go.transform,influences);
                    string spritePath=directory+"/"+part.name+".asset";
                    var previous=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if(previous){EditorUtility.CopySerialized(sprite,previous);Object.DestroyImmediate(sprite);sprite=previous;renderer.sprite=sprite;EditorUtility.SetDirty(sprite);}
                    else AssetDatabase.CreateAsset(sprite,spritePath);
                    var skin=go.AddComponent<SpriteSkin>();skin.autoRebind=false;skin.alwaysUpdate=true;skin.forceCpuDeformation=true;
                    skin.SetRootBone(influences[0]);skin.SetBoneTransforms(influences);skins.Add(skin);
                }
                actor.skins=skins.ToArray();
                actor.animator=root.AddComponent<Animator>();actor.animator.runtimeAnimatorController=Controller(directory,root,definition,bones);
                actor.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                root.transform.localScale=Vector3.one*definition.displayScale;
                foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                root.SetActive(true);
                return PrefabUtility.SaveAsPrefabAsset(root,directory+"/"+key+".prefab");
            }
            finally{Object.DestroyImmediate(root);}
        }
        static void SkinSprite(Sprite sprite,CompanionPartSpec part,Transform renderer,Transform[] bones)
        {
            const int columns=12,rows=16;
            float width=sprite.rect.width/sprite.pixelsPerUnit,height=sprite.rect.height/sprite.pixelsPerUnit;
            var vertices=new NativeArray<Vector3>((columns+1)*(rows+1),Allocator.Temp);
            var uvs=new NativeArray<Vector2>(vertices.Length,Allocator.Temp);
            var weights=new NativeArray<BoneWeight>(vertices.Length,Allocator.Temp);
            var tangents=new NativeArray<Vector4>(vertices.Length,Allocator.Temp);
            var indices=new ushort[columns*rows*6];int index=0;
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                int id=y*(columns+1)+x;var uv=new Vector2((float)x/columns,(float)y/rows);
                vertices[id]=Vector2.Scale(uv-part.pivot,new Vector2(width,height));
                uvs[id]=new Vector2((sprite.rect.x+uv.x*sprite.rect.width)/sprite.texture.width,
                    (sprite.rect.y+uv.y*sprite.rect.height)/sprite.texture.height);
                float along=Vector2.Dot(uv-part.pivot,part.weightAxis);
                float distal=bones.Length==1?0:Mathf.SmoothStep(0,1,Mathf.Clamp01((along-.2f)/.45f));
                weights[id]=new BoneWeight {boneIndex0=0,weight0=1-distal,boneIndex1=bones.Length==1?0:1,weight1=distal};
                tangents[id]=new Vector4(1,0,0,-1);
                if(x<columns && y<rows)
                {
                    int a=id,b=id+1,c=id+columns+1,d=c+1;
                    indices[index++]=(ushort)a;indices[index++]=(ushort)c;indices[index++]=(ushort)b;
                    indices[index++]=(ushort)b;indices[index++]=(ushort)c;indices[index++]=(ushort)d;
                }
            }
            // OverrideGeometry is a runtime override and leaves serialized Sprite render data as a quad.
            // Write the same native mesh streams used by Unity's 2D Animation sprite importer.
            sprite.SetVertexCount(vertices.Length);
            sprite.SetVertexAttribute<Vector3>(VertexAttribute.Position,vertices);
            sprite.SetVertexAttribute<Vector2>(VertexAttribute.TexCoord0,uvs);
            using(var nativeIndices=new NativeArray<ushort>(indices,Allocator.Temp))sprite.SetIndices(nativeIndices);
            var bind=new NativeArray<Matrix4x4>(bones.Length,Allocator.Temp);
            var metadata=new SpriteBone[bones.Length];
            for(int i=0;i<bones.Length;i++)
            {
                bind[i]=bones[i].worldToLocalMatrix*renderer.localToWorldMatrix;
                metadata[i]=new SpriteBone {name=bones[i].name,parentId=i==0?-1:0,
                    position=i==0?renderer.InverseTransformPoint(bones[i].position):bones[0].InverseTransformPoint(bones[i].position),
                    rotation=Quaternion.identity,length=.3f};
            }
            sprite.SetBindPoses(bind);sprite.SetBones(metadata);
            sprite.SetVertexAttribute<BoneWeight>(VertexAttribute.BlendWeight,weights);
            sprite.SetVertexAttribute<Vector4>(VertexAttribute.Tangent,tangents);
            bind.Dispose();weights.Dispose();tangents.Dispose();vertices.Dispose();uvs.Dispose();
        }
        static void ValidateSavedMeshes(CompanionRigCatalog catalog)
        {
            foreach(var entry in catalog.entries)
            foreach(var skin in entry.prefab.GetComponentsInChildren<SpriteSkin>(true))
            {
                var sprite=skin.GetComponent<SpriteRenderer>().sprite;
                string path=AssetDatabase.GetAssetPath(sprite);
                if(sprite.GetVertexCount()!=221 || sprite.GetIndices().Length!=1152)
                    throw new InvalidOperationException(path+" did not retain its 12-by-16 skinned grid.");
                var uv=sprite.GetVertexAttribute<Vector2>(VertexAttribute.TexCoord0);
                if(uv.Length!=221 || Vector2.Distance(uv[0],uv[uv.Length-1])<.01f)
                    throw new InvalidOperationException(path+" lost illustrated atlas UV coordinates.");
                // Verify the native stream on disk as well as the in-memory Sprite after SaveAssets.
                string serialized=File.ReadAllText(path);
                var count=System.Text.RegularExpressions.Regex.Match(serialized,@"m_VertexCount:\s*(\d+)");
                if(!count.Success || int.Parse(count.Groups[1].Value)!=221)
                    throw new InvalidOperationException(path+" serialized a fallback quad instead of the weighted grid.");
            }
        }
        static RuntimeAnimatorController Controller(string directory,GameObject root,CompanionSkeletonDefinition definition,Transform[] bones)
        {
            string path=directory+"/Companion.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine=controller.layers[0].stateMachine;
            foreach(var item in machine.states)machine.RemoveState(item.state);
            foreach(string stateName in new[]{"Idle","Walk","Attack","Hit","Death"})
            {
                string clipPath=directory+"/"+stateName+".anim";
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,clipPath);}
                clip.ClearCurves();clip.name=stateName;clip.frameRate=30;
                float duration=stateName=="Idle"?1.5f:stateName=="Walk"?.65f:.65f;
                foreach(var bone in bones)
                {
                    string name=bone.name;if(name=="Root")continue;
                    float amplitude=name=="Head"?3:name.Contains("Tail")?7:name.Contains("Wing")?16:name=="Body"?1.2f:0;
                    if(stateName=="Walk")amplitude=name.Contains("Wheel")?180:name.Contains("Front")||name.Contains("Hind")?14:name.Contains("Wing")?23:amplitude;
                    if(stateName=="Attack")amplitude=name=="Head"?-9:name.Contains("Front")?18:name=="Body"?4:amplitude;
                    if(stateName=="Hit")amplitude=name=="Body"?-7:0;
                    if(stateName=="Death")amplitude=name=="Body"?-65:0;
                    if(name.EndsWith("Far"))amplitude=-amplitude;
                    if(name.EndsWith("Tip"))amplitude*=.55f;
                    string bonePath=AnimationUtility.CalculateTransformPath(bone,root.transform);
                    AnimationCurve curve=name.Contains("Wheel") && stateName=="Walk"?AnimationCurve.Linear(0,0,duration,360):
                        new AnimationCurve(new Keyframe(0,0),new Keyframe(duration*.25f,amplitude),
                            new Keyframe(duration*.75f,-amplitude),new Keyframe(duration,stateName=="Death"?amplitude:0));
                    AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(bonePath,typeof(Transform),"localEulerAnglesRaw.z"),curve);
                }
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=stateName=="Idle"||stateName=="Walk";
                AnimationUtility.SetAnimationClipSettings(clip,settings);
                var state=machine.AddState(stateName);state.motion=clip;state.writeDefaultValues=true;
                if(stateName=="Idle")machine.defaultState=state;EditorUtility.SetDirty(clip);
            }
            EditorUtility.SetDirty(controller);return controller;
        }
    }
}
