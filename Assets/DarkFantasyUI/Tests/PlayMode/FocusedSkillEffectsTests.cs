using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonlit.UI.Tests
{
    public sealed class FocusedSkillEffectsTests
    {
        static readonly Vector3 Source=new Vector3(-3,1,0),Target=new Vector3(3,1,0);
        [TestCase(0,1,8,1.20f)]
        [TestCase(0,2,1,1.02f)]
        [TestCase(1,1,5,.86f)]
        [TestCase(1,2,1,.98f)]
        public void EarlySkillArrivalsAreDelayedAndHaveExactlyTheRequestedHitCount(int tier,int variant,int count,float first)
        {
            var times=SkillChoreography.HitTimes(tier,variant);
            Assert.AreEqual(count,times.Length);Assert.AreEqual(first,times[0],.0001f);
            Assert.Greater(times[0],SkillChoreography.AttackLead,"The Animator event arms the action before contact.");
            for(int i=1;i<count;i++)Assert.Greater(times[i],times[i-1]);
            Assert.AreEqual(count,SkillChoreography.HitCount(tier,variant));
            Assert.AreEqual(times.Last(),SkillChoreography.LastHit(tier,variant));
        }
        [Test]
        public void BothFoodsPulseExactlyThreeTimesThenVanishAtTheHealingMarker()
        {
            int peaks=0;
            for(int i=1;i<1199;i++) {
                float before=SixSkillChoreography.FoodScale((i-1)*.001f);
                float current=SixSkillChoreography.FoodScale(i*.001f);
                float after=SixSkillChoreography.FoodScale((i+1)*.001f);
                if(current>before&&current>=after)peaks++;
            }
            Assert.AreEqual(3,peaks);
            foreach(int tier in new[]{0,1}) {
                Assert.AreEqual(1.2f,SkillChoreography.BuffHealTime(tier),.0001f);
                Assert.AreEqual(1.85f,SkillChoreography.BuffDuration(tier),.0001f);
            }
            Assert.AreEqual(0,SixSkillChoreography.FoodScale(1.2f));
            Assert.AreEqual(0,SixSkillChoreography.Food(Source,1.2f).alpha);
            Assert.Greater(SixSkillChoreography.Food(Source,.2f).position.y,Source.y+1);
        }
        [Test]
        public void EightBonesFormOneRotatingRingThenSwingIntoTheHeadInOrder()
        {
            var centre=Source+new Vector3(0,.15f,-1);
            for(int i=0;i<8;i++) {
                var pose=SixSkillChoreography.Bone(Source,Target,i,.35f);
                Assert.AreEqual(1.25f,Vector3.Distance(centre,pose.position),.001f);
                Assert.Greater(Vector3.Distance(pose.position,SixSkillChoreography.Bone(Source,Target,i,.45f).position),.1f);
                for(int j=0;j<i;j++)Assert.Greater(Vector3.Distance(pose.position,SixSkillChoreography.Bone(Source,Target,j,.35f).position),.8f);
                float arrival=SkillChoreography.HitTimes(0,1)[i];
                var end=SixSkillChoreography.Bone(Source,Target,i,arrival);
                Assert.Less(Vector3.Distance(end.position,SixSkillChoreography.Head(Target)+Vector3.back),.001f);
                Assert.Greater(Mathf.Abs(end.rotation-SixSkillChoreography.Bone(Source,Target,i,arrival-.15f).rotation),25);
                Assert.AreEqual(0,SixSkillChoreography.Bone(Source,Target,i,arrival+.001f).alpha);
            }
        }
        [Test]
        public void RockRisesFastSlowsAtItsApexAndAcceleratesIntoItsOneImpact()
        {
            float fastRise=SixSkillChoreography.LobProgress(.2f)-SixSkillChoreography.LobProgress(0);
            float apexDrift=SixSkillChoreography.LobProgress(.5f)-SixSkillChoreography.LobProgress(.4f);
            float earlyFall=SixSkillChoreography.LobProgress(.8f)-SixSkillChoreography.LobProgress(.7f);
            float lastFall=SixSkillChoreography.LobProgress(1)-SixSkillChoreography.LobProgress(.9f);
            Assert.Greater(fastRise,.4f);Assert.Less(apexDrift,.025f);
            Assert.Greater(lastFall,earlyFall*3);
            Assert.AreEqual(1,SixSkillChoreography.LobProgress(1),.0001f);
        }
        [Test]
        public void FiveArrowsAreIndividualCurvedFlightsRatherThanOneFiveArrowImage()
        {
            Assert.AreEqual(1,Enumerable.Range(0,5).Count(i=>SixSkillChoreography.Arrow(Source,Target,i,.25f).alpha>0));
            for(int i=0;i<5;i++) {
                float departure=SixSkillChoreography.Departure(1,i),arrival=SkillChoreography.HitTimes(1,1)[i];
                var start=SixSkillChoreography.Arrow(Source,Target,i,departure);
                var midpoint=SixSkillChoreography.Arrow(Source,Target,i,(departure+arrival)/2);
                var end=SixSkillChoreography.Arrow(Source,Target,i,arrival);
                Assert.Greater(midpoint.position.y,(start.position.y+end.position.y)/2+.5f);
                Assert.Less(Vector3.Distance(end.position,SixSkillChoreography.Head(Target)+Vector3.back),.001f);
                Assert.AreEqual(0,SixSkillChoreography.Arrow(Source,Target,i,arrival+.01f).alpha);
            }
        }
        [UnityTest]
        public IEnumerator FoodStaysAboveTheActualDoubledHeadWhileTheAuraStaysOnTheBody()
        {
            var root=new GameObject("Actual focused head fixture");
            Sprite headSprite=null;
            try {
                var motion=new GameObject("Motion").transform;motion.SetParent(root.transform);
                motion.position=new Vector3(-3,0,0);
                var headObject=new GameObject("Head sprite",typeof(SpriteRenderer));headObject.transform.SetParent(motion,false);
                headObject.transform.localPosition=Vector3.up*4.35f;headObject.transform.localScale=Vector3.one*1.5f;
                headSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
                headSprite.name="머리";var head=headObject.GetComponent<SpriteRenderer>();head.sprite=headSprite;
                var effects=root.AddComponent<PrimitiveSkillEffects>();
                effects.Initialize(Resources.Load<BattleAssetCatalog>("Moonlit/Combat/BattleAssets"));
                Vector3 body=motion.position+Vector3.up*2.4f;
                Assert.AreEqual(5.1f,head.bounds.max.y,.001f);
                Assert.Less(Vector3.Distance(head.bounds.center,SixSkillChoreography.Head(PrimitiveSkillEffects.FocusedTarget(motion,body))),.001f);
                effects.Play(0,0,body,Target,motion,null,false);effects.EarlyPlaybackTimeOverride=.2f;
                yield return null;yield return null;
                var food=root.transform.Find("Primitive ancestral blessing/Overhead food").GetComponent<SpriteRenderer>();
                Assert.GreaterOrEqual(food.bounds.min.y,head.bounds.max.y+.29f,"Peak food size must still leave a gap above the actual head.");
                motion.position+=Vector3.right*2;
                yield return null;yield return null;
                Assert.AreEqual(head.bounds.center.x,food.bounds.center.x,.001f);
                effects.EarlyPlaybackTimeOverride=1.3f;yield return null;yield return null;
                var aura=root.transform.Find("Green healing body aura/Healing body glow");
                Assert.IsNotNull(aura);Assert.AreEqual(motion.position.y+1.8f,aura.position.y,.001f);
            } finally {Object.DestroyImmediate(root);if(headSprite)Object.DestroyImmediate(headSprite);}
        }
        [UnityTest]
        public IEnumerator ChickenAndAppleKeepThreeVisiblePulsesBetweenTheActualHeadAndProfileHeader()
        {
            foreach(int tier in new[]{0,1})foreach(float headroom in new[]{1.4f,5f}) {
                var root=new GameObject("Food headroom fixture");
                Sprite headSprite=null;
                try {
                    var motion=new GameObject("Motion").transform;motion.SetParent(root.transform);
                    motion.position=new Vector3(-3,0,0);
                    var headObject=new GameObject("Head sprite",typeof(SpriteRenderer));headObject.transform.SetParent(motion,false);
                    headObject.transform.localPosition=Vector3.up*4.35f;headObject.transform.localScale=Vector3.one*1.5f;
                    headSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
                    headSprite.name="머리";var head=headObject.GetComponent<SpriteRenderer>();head.sprite=headSprite;
                    var effects=root.AddComponent<PrimitiveSkillEffects>();
                    effects.Initialize(Resources.Load<BattleAssetCatalog>("Moonlit/Combat/BattleAssets"));
                    effects.FocusedFoodHeaderBounds=new Rect(-6,head.bounds.max.y+headroom,6,1.3f);
                    var art=PrimitiveSkillEffects.SkillSprite(tier,0);
                    float size=effects.FocusedFoodSize(art,head.bounds);
                    if(headroom<2)Assert.Less(size,PrimitiveSkillEffects.FocusedFoodBaseSize,"Short-screen headroom must constrain the maximum pulse.");
                    else Assert.AreEqual(PrimitiveSkillEffects.FocusedFoodBaseSize,size,.0001f,"Tall-screen food must retain the original size.");
                    effects.Play(tier,0,motion.position+Vector3.up*2.4f,Target,motion,null,false);
                    string effectName=tier==0?"Primitive ancestral blessing":"Era 1 skill 0";
                    SpriteRenderer food=null;
                    foreach(float peak in new[]{.2f,.6f,1f}) {
                        effects.EarlyPlaybackTimeOverride=peak;yield return null;yield return null;
                        food=root.transform.Find(effectName+"/Overhead food").GetComponent<SpriteRenderer>();
                        Assert.IsTrue(food.enabled);
                        Assert.Greater(food.bounds.size.y,.7f,"Each of the three peaks must remain a legible food sprite.");
                        Assert.GreaterOrEqual(food.bounds.min.y,head.bounds.max.y+.29f);
                        Assert.LessOrEqual(food.bounds.max.y,effects.FocusedFoodHeaderBounds.yMin-.07f,
                            "Peak food must not be occluded by the actual profile header.");
                        float peakHeight=food.bounds.size.y;
                        effects.EarlyPlaybackTimeOverride=peak+.19f;yield return null;yield return null;
                        Assert.Less(food.bounds.size.y,peakHeight*.7f,"Each peak must shrink before the next pulse.");
                    }
                    // The actor can rise during its animation; recompute from live head bounds, not its initial position.
                    motion.position+=Vector3.up*.2f;
                    effects.EarlyPlaybackTimeOverride=1f;yield return null;yield return null;
                    Assert.GreaterOrEqual(food.bounds.min.y,head.bounds.max.y+.29f);
                    Assert.LessOrEqual(food.bounds.max.y,effects.FocusedFoodHeaderBounds.yMin-.07f);
                    effects.EarlyPlaybackTimeOverride=1.3f;yield return null;yield return null;
                    Assert.IsFalse(food.enabled);
                    Assert.IsNotNull(root.transform.Find("Green healing body aura"));
                } finally {Object.DestroyImmediate(root);if(headSprite)Object.DestroyImmediate(headSprite);}
            }
        }
        [UnityTest]
        public IEnumerator PausedLastFrameAndRockFragmentsSurviveUntilExplicitCleanup()
        {
            var root=new GameObject("Stable late focused capture");
            try {
                var effects=root.AddComponent<PrimitiveSkillEffects>();
                effects.Initialize(Resources.Load<BattleAssetCatalog>("Moonlit/Combat/BattleAssets"));
                effects.Play(0,2,Source,Target,null,null,true);
                effects.EarlyPlaybackTimeOverride=1.07f;
                yield return null;yield return null;
                Assert.IsNotNull(root.transform.Find("Primitive falling boulder"),"A seek beyond the flight must not discard its playback on a slow frame.");
                var impact=root.transform.Find("Skill contact 0 2 hit 0");Assert.IsNotNull(impact);
                var meshes=impact.GetComponentsInChildren<MeshRenderer>();
                Assert.AreEqual(6,meshes.Length);
                yield return new WaitForSeconds(1.15f);
                Assert.IsTrue(impact);
                Assert.IsTrue(meshes.All(mesh=>mesh&&mesh.sharedMaterial&&mesh.GetComponent<MeshFilter>().sharedMesh),
                    "Paused capture assets use effect lifetime, not a timed Destroy that can expire during rendering.");
                effects.enabled=false;yield return null;
                Assert.IsNull(root.transform.Find("Skill contact 0 2 hit 0"));
            } finally {Object.DestroyImmediate(root);}
        }
        [UnityTest]
        public IEnumerator RuntimeSpawnsEightSeparateBonesFiveSeparateArrowsAndOneOverheadFood()
        {
            var root=new GameObject("Focused six-skill fixture");
            try {
                var effects=root.AddComponent<PrimitiveSkillEffects>();
                effects.Initialize(Resources.Load<BattleAssetCatalog>("Moonlit/Combat/BattleAssets"));
                effects.Play(0,1,Source,Target,null,null,false);
                effects.EarlyPlaybackTimeOverride=.35f;yield return null;yield return null;
                var bones=root.transform.Find("Primitive stone crescent").GetComponentsInChildren<SpriteRenderer>();
                Assert.AreEqual(8,bones.Length);Assert.IsTrue(bones.All(b=>b.enabled));
                Assert.IsTrue(bones.All(b=>b.sprite==PrimitiveSkillEffects.SkillSprite(0,1)));
                effects.enabled=false;effects.enabled=true;yield return null;
                effects.Play(1,1,Source,Target,null,null,false);
                effects.EarlyPlaybackTimeOverride=.25f;yield return null;yield return null;
                var arrows=root.transform.Find("Era 1 skill 1").GetComponentsInChildren<SpriteRenderer>();
                Assert.AreEqual(5,arrows.Length);Assert.AreEqual(1,arrows.Count(a=>a.enabled));
                Assert.IsTrue(arrows.All(a=>a.sprite==PrimitiveSkillEffects.SkillSprite(1,1)));
                effects.enabled=false;effects.enabled=true;yield return null;
                effects.Play(1,0,Source,Target,null,null,false);
                effects.EarlyPlaybackTimeOverride=.2f;yield return null;yield return null;
                var food=root.transform.Find("Era 1 skill 0").GetComponentsInChildren<SpriteRenderer>();
                Assert.AreEqual(1,food.Length);Assert.IsTrue(food[0].enabled);
                effects.EarlyPlaybackTimeOverride=1.3f;yield return null;yield return null;
                Assert.IsFalse(food[0].enabled);
                var aura=root.transform.Find("Green healing body aura");Assert.IsNotNull(aura);
                var glow=aura.GetComponentInChildren<SpriteRenderer>();
                Assert.Greater(glow.color.g,glow.color.r);Assert.Greater(glow.color.a,0);
                Assert.Less(glow.transform.position.y,Source.y);
            } finally {Object.DestroyImmediate(root);}
        }
    }
}
