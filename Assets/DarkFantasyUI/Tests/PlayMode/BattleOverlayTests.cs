using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Moonlit.UI.Tests
{
    public sealed class BattleOverlayTests
    {
        [UnityTest]
        public IEnumerator FactoryKeepsAnnotatedControlsAboveEquipmentAndRefreshesEquippedSkills()
        {
#if UNITY_EDITOR
            bool persistence=MainScreen.PersistenceEnabled;
            var root=new GameObject("Battle overlay acceptance");root.SetActive(false);
            try {
                MoonlitRuntimeSettings.ResetSession();MainScreen.PersistenceEnabled=false;
                var assets=AssetDatabase.LoadAssetAtPath<MainScreenAssets>("Assets/DarkFantasyUI/Data/MainScreenAssets.asset");
                var screen=new RuntimeMainScreenFactory(assets).Create(root.transform);
                screen.enabled=false;root.SetActive(true);
                var hud=screen.GetComponentInChildren<EquippedSkillHud>(true);
                Assert.IsNotNull(hud);
                Assert.AreEqual(0,hud.GetComponentsInChildren<Button>().Length);
                for(int i=0;i<3;i++){
                    var entry=CollectionProgression.Data.categories[0].entries[i];
                    entry.unlocked=true;CollectionProgression.Equip(entry,i);
                }
                hud.Refresh();yield return null;
                var buttons=hud.GetComponentsInChildren<Button>();
                Assert.AreEqual(3,buttons.Length);
                for(int i=0;i<3;i++) {
                    Assert.AreSame(PrimitiveSkillEffects.SkillSprite(0,i),buttons[i].transform.Find("Skill icon").GetComponent<Image>().sprite);
                    Assert.IsNotEmpty(buttons[i].GetComponentsInChildren<Text>().Single().text);
                }
                foreach(int height in new[]{1600,1920,2280}) {
                    root.GetComponentInChildren<PortraitSafeArea>().SetPreviewMetrics(new Vector2Int(1080,height),new Rect(0,0,1080,height));
                    Canvas.ForceUpdateCanvases();yield return null;
                    var bottom=screen.design.Find("Bottom equipment forge and navigation");
                    Assert.AreSame(bottom,screen.eventButton.transform.parent);
                    Assert.AreSame(bottom,screen.fairyButton.transform.parent);
                    Assert.LessOrEqual(((RectTransform)hud.transform).anchoredPosition.y*-1+((RectTransform)hud.transform).rect.height,0,
                        "Equipped skill controls must remain above the equipment panel.");
                    var eventRect=(RectTransform)screen.eventButton.transform;
                    Assert.Greater(eventRect.anchoredPosition.y,0);
                    var passRect=(RectTransform)screen.fairyButton.transform;
                    Assert.Greater(passRect.anchoredPosition.y,eventRect.anchoredPosition.y,"Pass belongs above the lower reward/skill row.");
                    Assert.AreEqual(4,screen.navigation.Length);
                }
                CollectionProgression.Data.categories[0].equipped[1]=-1;
                hud.Refresh();
                Assert.AreEqual(2,hud.GetComponentsInChildren<Button>().Length,"Unequipping immediately removes the battle skill.");
                LogAssert.NoUnexpectedReceived();
            }
            finally {
                Object.DestroyImmediate(root);MoonlitRuntimeSettings.ResetSession();MainScreen.PersistenceEnabled=persistence;
            }
#else
            Assert.Ignore("Cloud Editor fixture uses the authored runtime catalog.");
            yield return null;
#endif
        }
    }
}
