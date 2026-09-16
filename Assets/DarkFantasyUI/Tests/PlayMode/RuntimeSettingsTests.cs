using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Moonlit.UI.Tests
{
    public sealed class RuntimeSettingsTests
    {
        const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
        static FieldInfo Field(Type type, string name) => type.GetField(name, StaticPrivate);

        [Test]
        public void Startup_OverridesVSyncAndFrameCap_AndRunsWithoutFocus()
        {
            int previousVSync=QualitySettings.vSyncCount, previousCap=Application.targetFrameRate;
            bool previousBackground=Application.runInBackground;
            try
            {
                QualitySettings.vSyncCount=1;
                Application.targetFrameRate=15;
                Application.runInBackground=false;
                MoonlitRuntimeSettings.Apply();
                Assert.AreEqual(0,QualitySettings.vSyncCount,"VSync must not override the requested frame cap");
                Assert.AreEqual(60,Application.targetFrameRate);
                Assert.IsTrue(Application.runInBackground);
            }
            finally
            {
                QualitySettings.vSyncCount=previousVSync;
                Application.targetFrameRate=previousCap;
                Application.runInBackground=previousBackground;
            }
        }

        [Test]
        public void FreshSessions_ResetSpentCurrencySkillsClaimsAndProfile_WithoutReloadingStatics()
        {
            var progression=typeof(ProgressionScreenModule);
            var social=typeof(SocialScreenModule);
            var forge=typeof(ForgeScreenModule);
            var skills=(Array)Field(progression,"Skills").GetValue(null);
            var skill=skills.GetValue(0);
            var skillType=skill.GetType();
            var claims=(HashSet<int>)Field(forge,"passClaims").GetValue(null);
            try
            {
                for(int session=0;session<2;session++)
                {
                    Field(progression,"summonCurrency").SetValue(null,17);
                    Field(progression,"selectedCollectionTab").SetValue(null,2);
                    skillType.GetField("level").SetValue(skill,100);
                    skillType.GetField("shards").SetValue(skill,99);
                    skillType.GetField("owned").SetValue(skill,false);
                    foreach(int index in new[]{13,14,15})
                    {
                        skillType.GetField("owned").SetValue(skills.GetValue(index),false);
                        skillType.GetField("level").SetValue(skills.GetValue(index),100);
                    }
                    foreach(int index in new[]{11,16,17})
                        skillType.GetField("owned").SetValue(skills.GetValue(index),true);
                    ((int[])Field(progression,"equippedSkills").GetValue(null))[0]=0;
                    claims.Add(1);
                    Field(social,"profileName").SetValue(null,"changed profile");
                    Field(social,"profileFemale").SetValue(null,true);
                    ((bool[])Field(forge,"autoFilters").GetValue(null))[1]=false;
                    MoonlitRuntimeSettings.ResetSession();
                    Assert.AreSame(skills,Field(progression,"Skills").GetValue(null),"No domain reload or replacement model is needed");
                    Assert.AreEqual(6830,Field(progression,"summonCurrency").GetValue(null));
                    Assert.AreEqual(0,Field(progression,"selectedCollectionTab").GetValue(null));
                    Assert.AreEqual(76,skillType.GetField("level").GetValue(skill));
                    Assert.AreEqual(3,skillType.GetField("shards").GetValue(skill));
                    Assert.AreEqual(true,skillType.GetField("owned").GetValue(skill));
                    var equipped=(int[])Field(progression,"equippedSkills").GetValue(null);
                    CollectionAssert.AreEqual(new[]{15,14,13},equipped);
                    var expectedLevels=new[]{20,19,17};
                    for(int i=0;i<equipped.Length;i++)
                    {
                        var equippedSkill=skills.GetValue(equipped[i]);
                        Assert.AreEqual(expectedLevels[i],skillType.GetField("level").GetValue(equippedSkill));
                        Assert.AreEqual(true,skillType.GetField("owned").GetValue(equippedSkill),"Low-level starting equipment must remain owned after reset");
                    }
                    foreach(int index in new[]{11,16,17})
                        Assert.AreEqual(false,skillType.GetField("owned").GetValue(skills.GetValue(index)),"Summoned ownership must not survive a fresh session");
                    int ownedCount=0;
                    foreach(var entry in skills)
                        if((bool)skillType.GetField("owned").GetValue(entry)) ownedCount++;
                    Assert.AreEqual(15,ownedCount);
                    Assert.IsEmpty(claims);
                    Assert.AreEqual("moonzzanf",Field(social,"profileName").GetValue(null));
                    Assert.AreEqual(false,Field(social,"profileFemale").GetValue(null));
                    CollectionAssert.AreEqual(new[]{true,true,false,false,false,true},(bool[])Field(forge,"autoFilters").GetValue(null));
                }
            }
            finally { MoonlitRuntimeSettings.ResetSession(); }
        }

        [Test]
        public void FreshSession_RebuildsDestroyedAvatarSprites_FromRetainedStaticCache()
        {
            var portrait=typeof(SocialScreenModule).GetMethod("AvatarPortrait",StaticPrivate);
            var first=(Sprite)portrait.Invoke(null,new object[]{0});
            Assert.IsNotNull(first);
            UnityEngine.Object.DestroyImmediate(first);
            MoonlitRuntimeSettings.ResetSession();
            var next=(Sprite)portrait.Invoke(null,new object[]{0});
            Assert.IsTrue(next,"A retained array must not return a sprite destroyed when the preceding Play session ended");
            Assert.AreNotSame(first,next);
            Assert.IsNotNull(next.texture);
        }
    }
}
