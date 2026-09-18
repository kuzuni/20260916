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
