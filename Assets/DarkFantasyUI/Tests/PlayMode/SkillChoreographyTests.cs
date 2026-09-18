using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonlit.UI.Tests
{
    public sealed class SkillChoreographyTests
    {
        static readonly Vector3 Source=new Vector3(-3,1,0),Target=new Vector3(3,1,0);
        [Test]
        public void EveryAttackHasTheCorrectNonuniformDefensiveHitSchedule()
        {
            for(int variant=1;variant<=2;variant++) {
                var schedules=new HashSet<string>();
                for(int tier=2;tier<10;tier++) {
                    var times=SkillChoreography.HitTimes(tier,variant);
                    Assert.AreEqual(variant==1?3:5,times.Length);Assert.AreEqual(SkillChoreography.AttackLead,times[0]);
                    var gaps=new List<float>();
                    for(int i=1;i<times.Length;i++){Assert.Greater(times[i],times[i-1]);gaps.Add(times[i]-times[i-1]);}
                    Assert.Greater(gaps.Max()-gaps.Min(),.02f,"Uneven rhythm "+tier+"/"+variant);
                    Assert.IsTrue(schedules.Add(string.Join(",",times.Select(v=>v.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)))));
                    times[1]=99;Assert.Less(SkillChoreography.HitTimes(tier,variant)[1],2);
                }
            }
        }
        static float Difference(SkillVisualPose a,SkillVisualPose b)
            =>Vector3.Distance(a.position,b.position)+Vector2.Distance(a.scale,b.scale)+Mathf.Abs(a.rotation-b.rotation)*.01f+Mathf.Abs(a.alpha-b.alpha);
        [Test]
        public void UnchangedHigherEraPreparationsAndTrajectoriesDifferGeometrically()
        {
            for(int a=6;a<30;a++)for(int b=a+1;b<30;b++) {
                float distance=0;
                foreach(float t in new[]{.12f,.29f,.47f,.69f,.88f}) {
                    distance+=Difference(SkillChoreography.Sample(a/3,a%3,Source,Target,t),SkillChoreography.Sample(b/3,b%3,Source,Target,t));
                }
                Assert.Greater(distance,.8f,"Visually duplicated motion "+a+" / "+b);
            }
            for(int tier=2;tier<10;tier++)for(int variant=0;variant<3;variant++) {
                var steps=new List<float>();
                var previous=SkillChoreography.Sample(tier,variant,Source,Target,0);
                for(int i=1;i<=20;i++) {
                    var pose=SkillChoreography.Sample(tier,variant,Source,Target,i/20f);
                    Assert.IsFalse(float.IsNaN(pose.position.x)||float.IsInfinity(pose.position.y));
                    Assert.Greater(pose.scale.x,0);Assert.Greater(pose.scale.y,0);
                    steps.Add(Vector3.Distance(previous.position,pose.position));previous=pose;
                }
                Assert.Greater(steps.Max()-steps.Min(),.035f,"Uniform velocity "+tier+"/"+variant);
            }
        }
        [Test]
        public void EveryBuffAccentAndAttackImpactUsesDifferentGeometry()
        {
            for(int a=2;a<10;a++)for(int b=a+1;b<10;b++) {
                float difference=0;
                for(int i=0;i<4;i++)foreach(float t in new[]{.2f,.5f,.8f})
                    difference+=Difference(SkillChoreography.BuffAccent(a,i,t,Source),SkillChoreography.BuffAccent(b,i,t,Source));
                Assert.Greater(difference,1);
            }
            for(int a=4;a<20;a++)for(int b=a+1;b<20;b++) {
                float difference=0;
                for(int i=0;i<3;i++)foreach(float t in new[]{.16f,.42f,.7f})
                    difference+=Difference(SkillChoreography.Impact(a/2,a%2+1,i,t,Target,0),SkillChoreography.Impact(b/2,b%2+1,i,t,Target,0));
                Assert.Greater(difference,1,"Duplicated impact geometry "+a+"/"+b);
            }
        }
        [Test]
        public void SinglePrimitiveRockFollowsOneParabolicLob()
        {
            var start=SixSkillChoreography.Rock(Source,Target,.12f).position;
            var apex=SixSkillChoreography.Rock(Source,Target,.57f).position;
            var end=SixSkillChoreography.Rock(Source,Target,1.02f).position;
            Assert.AreEqual(Source.x,start.x,.001f);Assert.AreEqual(Target.x,end.x,.001f);
            Assert.AreEqual((Source.x+Target.x)/2,apex.x,.001f);
            Assert.AreEqual(2.6f,apex.y-(start.y+end.y)/2,.001f);
            Assert.AreEqual(1,SkillChoreography.HitTimes(0,2).Length);
        }
        [Test]
        public void PhysicalContactsAndSupplyAccentsDoNotCloneWholeProjectiles()
        {
            var root=new GameObject("Physical skill contact textures");
            try {
                var effects=root.AddComponent<PrimitiveSkillEffects>();
                effects.Initialize(Resources.Load<BattleAssetCatalog>("Moonlit/Combat/BattleAssets"));
                var slash=Resources.Load<Sprite>(PrimitiveSkillEffects.BasicSlashKey);
                var dust=Resources.Load<Sprite>(PrimitiveSkillEffects.HitDustKey);
                Assert.IsNotNull(slash);Assert.IsNotNull(dust);
                for(int tier=2;tier<=3;tier++)for(int variant=1;variant<=2;variant++) {
                    effects.PlaySkillHit(tier,variant,0,Source,Target,true);
                    var contact=root.transform.Find("Skill contact "+tier+" "+variant+" hit 0");
                    Assert.IsNotNull(contact);
                    var expected=tier>0&&variant==1?slash:dust;
                    foreach(var piece in contact.GetComponentsInChildren<SpriteRenderer>()) {
                        Assert.AreSame(expected,piece.sprite);
                        Assert.AreNotSame(PrimitiveSkillEffects.SkillSprite(tier,variant),piece.sprite);
                    }
                }
                effects.Play(3,0,Source,Target);
                var buff=root.transform.Find("Era 3 skill 0");
                Assert.IsNotNull(buff);
                Assert.AreSame(PrimitiveSkillEffects.SkillSprite(3,0),buff.GetChild(0).GetComponent<SpriteRenderer>().sprite,
                    "The main supply crate remains the original authored sprite.");
                for(int i=0;i<4;i++)
                    Assert.AreSame(dust,buff.Find("Buff accent "+i).GetComponent<SpriteRenderer>().sprite,
                        "A supply pickup radiates dust/glow, never four miniature crates.");
            } finally {UnityEngine.Object.DestroyImmediate(root);}
        }
        [UnityTest]
        public IEnumerator LethalCancellationRemovesFutureFlightsButPreservesTheResolvedHit()
        {
            var root=new GameObject("Choreography cancellation fixture");
            try {
                var assets=Resources.Load<BattleAssetCatalog>("Moonlit/Combat/BattleAssets");
                Assert.IsNotNull(assets);
                var effects=root.AddComponent<PrimitiveSkillEffects>();effects.Initialize(assets);
                effects.Play(0,2,Source,Target,null,null,false);
                Assert.IsNotNull(root.transform.Find("Primitive falling boulder"));
                effects.PlaySkillHit(0,2,0,Source,Target,false);
                Assert.IsNull(root.transform.Find("Skill contact 0 2 hit 0"),"Evasion cannot produce a contact.");
                effects.PlaySkillHit(0,2,0,Source,Target,true);
                var impact=root.transform.Find("Skill contact 0 2 hit 0");Assert.IsNotNull(impact);
                Assert.AreEqual(6,impact.GetComponentsInChildren<MeshRenderer>().Length);
                effects.CancelSkillPlayback();yield return null;
                Assert.IsNull(root.transform.Find("Primitive falling boulder"));
                Assert.IsTrue(impact,"The lethal hit must remain visible after combo cancellation.");
                Assert.AreSame(PrimitiveSkillEffects.SkillSprite(0,2).texture,impact.GetComponentInChildren<MeshRenderer>().sharedMaterial.mainTexture);
                Assert.IsFalse(effects.IsPlaying);
                for(int frame=0;frame<4;frame++)yield return null;
                Assert.IsNull(root.transform.Find("Skill contact 0 2 hit 1"),"Cancelled future hits must not reappear.");
            } finally {UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
