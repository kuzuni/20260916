using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.U2D.Animation;

namespace Moonlit.UI.Tests
{
    public sealed class FlatCompanionTests
    {
        [TestCase(1,0,"검치호 새끼")]
        [TestCase(1,1,"늑대 새끼")]
        [TestCase(1,2,"멧돼지 새끼")]
        [TestCase(2,0,"멧돼지")]
        [TestCase(2,1,"매머드")]
        [TestCase(2,2,"코뿔소")]
        public void WholeArtAndOpaqueBoundsProduceOneUnriggedRightFacingSprite(int category,int variant,string name)
        {
            var sprite=FlatCompanionCatalog.Icon(category,0,variant);
            var bounds=FlatCompanionCatalog.Bounds(category,variant);
            Assert.IsNotNull(sprite,"Actual whole PNG must be imported.");
            Assert.IsNotNull(bounds,"Explicit opaque bounds prevent padded art floating above its feet.");
            Assert.AreEqual(name,FlatCompanionCatalog.Name(category,0,variant));
            Assert.Greater(bounds.width,0);Assert.Greater(bounds.height,0);
            Assert.GreaterOrEqual(bounds.x,0);Assert.GreaterOrEqual(bounds.y,0);
            Assert.LessOrEqual(bounds.x+bounds.width,sprite.texture.width);
            Assert.LessOrEqual(bounds.y+bounds.height,sprite.texture.height);
            Assert.IsNull(FlatCompanionCatalog.Icon(category,1,variant),"Do not claim new era companion art.");
            var root=new GameObject("Whole companion assertion");
            try
            {
                root.transform.position=new Vector3(100,200,0);
                var actor=FlatCompanionCatalog.Create(category,variant,root.transform,name);
                Assert.IsNotNull(actor);
                Assert.AreEqual(1,actor.GetComponentsInChildren<SpriteRenderer>().Length);
                Assert.IsEmpty(actor.GetComponentsInChildren<SpriteSkin>());
                Assert.IsEmpty(actor.GetComponentsInChildren<Animator>());
                Assert.AreSame(sprite,actor.Illustration.sprite);
                Assert.IsFalse(actor.Illustration.flipX);Assert.IsFalse(actor.Illustration.flipY);
                Assert.Greater(actor.Illustration.transform.localScale.x,0);
                Assert.AreEqual(category==2?2.45f:1.2f,actor.VisibleBounds.size.y,.001f);
                Assert.AreEqual(actor.transform.position.y,actor.VisibleBounds.min.y,.001f);
                Assert.AreEqual(actor.transform.position.x,actor.VisibleBounds.center.x,.001f);
                Assert.AreEqual(Quaternion.identity,actor.Illustration.transform.localRotation);
                Assert.Greater(actor.saddle.position.y,actor.VisibleBounds.min.y);
                Assert.Less(actor.saddle.position.y,actor.VisibleBounds.max.y);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [UnityTest]
        public IEnumerator WholeCompanionPoseRemainsStaticAcrossFrames()
        {
            var root=new GameObject("Static companion pose assertion");
            try
            {
                var actor=FlatCompanionCatalog.Create(2,0,root.transform,"Static boar");
                Assert.IsNotNull(actor);
                var image=actor.Illustration.transform;
                Vector3 position=image.localPosition,scale=image.localScale;
                Quaternion rotation=image.localRotation;
                for(int i=0;i<8;i++)
                {
                    yield return null;
                    Assert.AreEqual(position,image.localPosition);Assert.AreEqual(scale,image.localScale);
                    Assert.AreEqual(rotation,image.localRotation);
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
