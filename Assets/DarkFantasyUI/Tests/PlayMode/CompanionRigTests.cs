using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

namespace Moonlit.UI.Tests
{
    public sealed class CompanionRigTests
    {
        [Test]
        public void TenAnatomicalDefinitionsAndSixActualPrimitivePartRigsArePresent()
        {
            var catalog=CompanionRigCatalog.Load();
            Assert.IsNotNull(catalog,"Cloud CompanionAssetBuilder.Build must prepare the actual separated art.");
            Assert.AreEqual(10,catalog.definitions.Length);
            CollectionAssert.AreEquivalent(Enum.GetValues(typeof(CompanionRigType)),catalog.definitions.Select(d=>d.type));
            foreach(var definition in catalog.definitions)
            {
                Assert.GreaterOrEqual(definition.bones.Length,7);
                for(int i=0;i<definition.bones.Length;i++)
                    Assert.Less(definition.bones[i].parent,i,"Parents must precede children.");
            }
            Assert.AreEqual(6,catalog.entries.Length);
            foreach(var entry in catalog.entries)
            {
                Assert.IsNotNull(entry.icon);Assert.IsNotNull(entry.prefab);
                Assert.AreEqual(8,entry.prefab.GetComponentsInChildren<SpriteSkin>(true).Length);
                var actor=entry.prefab.GetComponent<CompanionActor>();
                Assert.AreEqual(entry.type,actor.rigType);
                Assert.IsNotNull(actor.saddle);Assert.IsNotNull(actor.animator.runtimeAnimatorController);
                CollectionAssert.AreEquivalent(new[]{"Idle","Walk","Attack","Hit","Death"},
                    actor.animator.runtimeAnimatorController.animationClips.Select(clip=>clip.name));
                foreach(var skin in actor.skins)
                {
                    var sprite=skin.GetComponent<SpriteRenderer>().sprite;
                    Assert.IsNotNull(sprite);Assert.Greater(sprite.GetVertexCount(),100);
                    Assert.AreEqual(221,sprite.GetVertexCount(),"Persist all internal skinning vertices.");
                    Assert.AreEqual(1152,sprite.GetIndices().Length);
                    var uv=sprite.GetVertexAttribute<Vector2>(VertexAttribute.TexCoord0);
                    Assert.AreEqual(221,uv.Length);
                    Assert.Greater(Vector2.Distance(uv[0],uv[uv.Length-1]),.01f,"Texture UVs must span the actual illustrated part.");
                    foreach(var point in uv)
                    {
                        Assert.That(point.x,Is.InRange(sprite.rect.xMin/sprite.texture.width-.0001f,sprite.rect.xMax/sprite.texture.width+.0001f));
                        Assert.That(point.y,Is.InRange(sprite.rect.yMin/sprite.texture.height-.0001f,sprite.rect.yMax/sprite.texture.height+.0001f));
                    }
                    Assert.AreEqual(skin.boneTransforms.Length,sprite.GetBones().Length);
                    Assert.AreEqual(skin.boneTransforms.Length,sprite.GetBindPoses().Length);
                    var weights=sprite.GetVertexAttribute<BoneWeight>(VertexAttribute.BlendWeight);
                    bool distal=false;
                    foreach(var weight in weights)
                    {
                        Assert.AreEqual(1,weight.weight0+weight.weight1+weight.weight2+weight.weight3,.0001f);
                        Assert.That(weight.boneIndex0,Is.InRange(0,skin.boneTransforms.Length-1));
                        distal |= weight.weight1>.05f;
                    }
                    if(skin.boneTransforms.Length>1)Assert.IsTrue(distal,"The secondary joint must influence real mesh vertices.");
                }
            }
            Assert.IsNull(catalog.Find(1,1,0),"Only the six illustrated primitive creatures are implemented.");
        }

        [UnityTest]
        public IEnumerator EveryCompanionUsesRealSpriteSkinDeformationWithStationaryActorRoot()
        {
            foreach(var entry in CompanionRigCatalog.Load().entries)
            {
                var instance=UnityEngine.Object.Instantiate(entry.prefab);
                try
                {
                    var actor=instance.GetComponent<CompanionActor>();actor.animator.enabled=false;
                    foreach(var skin in actor.skins){skin.alwaysUpdate=true;skin.forceCpuDeformation=true;}
                    yield return null;yield return null;
                    var head=actor.skins.Single(skin=>skin.name=="Head sprite");
                    var before=head.GetDeformedVertexPositionData().ToArray();
                    Vector3 position=instance.transform.position;
                    Quaternion rotation=instance.transform.rotation;
                    actor.Bone("Head").localRotation=Quaternion.Euler(0,0,18);
                    yield return null;yield return null;
                    var after=head.GetDeformedVertexPositionData().ToArray();
                    Assert.AreEqual(before.Length,after.Length);
                    Assert.IsTrue(before.Where((vertex,i)=>Vector3.Distance(vertex,after[i])>.02f).Any(),
                        entry.displayName+" must deform actual skinned vertices, not bob a whole sprite.");
                    Assert.AreEqual(position,instance.transform.position);Assert.AreEqual(rotation,instance.transform.rotation);
                    var limb=actor.skins.FirstOrDefault(skin=>skin.boneTransforms.Length==2);
                    if(limb)
                    {
                        var limbBefore=limb.GetDeformedVertexPositionData().ToArray();
                        limb.boneTransforms[1].localRotation=Quaternion.Euler(0,0,24);
                        yield return null;yield return null;
                        var limbAfter=limb.GetDeformedVertexPositionData().ToArray();
                        Assert.IsTrue(limbBefore.Where((vertex,i)=>Vector3.Distance(vertex,limbAfter[i])>.01f).Any());
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(instance);}
            }
        }

        [UnityTest]
        public IEnumerator ThreeEquippedPetsAndEachMountFollowPlayerAndRestoreOnUnequip()
        {
            var previous=CollectionProgression.Data;
            var root=new GameObject("Companion equipped fixture",typeof(RectTransform));
            var assets=ScriptableObject.CreateInstance<MainScreenAssets>();
            try
            {
                CollectionProgression.Data=CollectionProgression.Create();
                for(int category=1;category<=2;category++)for(int variant=0;variant<3;variant++)
                    CollectionProgression.Data.categories[category].entries[variant].unlocked=true;
                for(int variant=0;variant<3;variant++)CollectionProgression.Equip(CollectionProgression.Data.categories[1].entries[variant],variant);
                ((RectTransform)root.transform).sizeDelta=new Vector2(1080,2280);
                var main=root.AddComponent<MainScreen>();main.enabled=false;main.design=root.transform;
                assets.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");main.font=assets.font;
                var battle=root.AddComponent<BattleRuntime>();battle.Initialize(main,assets);battle.StopAllCoroutines();
                var actor=battle.PlayerHud.Actor;actor.GetComponent<Animator>().enabled=false;
                var rig=actor.Find("Motion/PlayerRig");Vector3 normal=rig.localPosition;
                var system=actor.parent.GetComponent<CompanionBattleRuntime>();
                Assert.IsNotNull(system);
                yield return null;yield return null;
                Assert.AreEqual(3,system.Pets.Count);
                foreach(var pet in system.Pets)
                {
                    Assert.Less(pet.transform.position.x,actor.position.x);
                    Assert.IsNotNull(pet.transform.parent.Find(pet.name+" ground shadow"));
                }
                for(int variant=0;variant<3;variant++)
                {
                    CollectionProgression.Equip(CollectionProgression.Data.categories[2].entries[variant],0);
                    system.RefreshEquipped();
                    yield return null;yield return null;
                    Assert.IsNotNull(system.Mount);
                    Vector3 saddle=system.Mount.saddle.position,hip=system.RiderHip.position;
                    Assert.AreEqual(saddle.x,hip.x,.02f);Assert.AreEqual(saddle.y,hip.y,.02f);
                    Assert.IsFalse(actor.parent.Find(actor.name+" ground shadow").gameObject.activeSelf);
                    Vector3 before=system.Mount.transform.position;
                    actor.position+=Vector3.right*.4f;
                    yield return null;yield return null;
                    Assert.AreEqual(before.x+.4f,system.Mount.transform.position.x,.02f);
                    Assert.AreEqual(system.Mount.saddle.position.x,system.RiderHip.position.x,.02f);
                }
                CollectionProgression.Data.categories[2].equipped[0]=-1;system.RefreshEquipped();
                yield return null;yield return null;
                Assert.IsNull(system.Mount);Assert.AreEqual(normal,rig.localPosition);
                Assert.IsTrue(actor.parent.Find(actor.name+" ground shadow").gameObject.activeSelf);
                CollectionProgression.Data.categories[1].equipped=new[]{-1,-1,-1};system.RefreshEquipped();
                Assert.AreEqual(0,system.Pets.Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(assets);
                CollectionProgression.Data=previous;
            }
        }
    }
}
