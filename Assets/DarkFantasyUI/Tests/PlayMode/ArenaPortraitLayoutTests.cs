using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Moonlit.UI.Tests
{
    public sealed class ArenaPortraitLayoutTests
    {
        [UnityTest]
        public IEnumerator ArenaPortraitsGrowOneAndAHalfTimesWithoutCoveringLabelsOrAdjacentRows()
        {
#if UNITY_EDITOR
            bool persistence=MainScreen.PersistenceEnabled;
            var root=new GameObject("Arena portrait layout");root.SetActive(false);
            try {
                MoonlitRuntimeSettings.ResetSession();MainScreen.PersistenceEnabled=false;
                var assets=AssetDatabase.LoadAssetAtPath<MainScreenAssets>("Assets/DarkFantasyUI/Data/MainScreenAssets.asset");
                var screen=new RuntimeMainScreenFactory(assets).Create(root.transform);
                screen.enabled=false;screen.GetComponent<BattleRuntime>().enabled=false;root.SetActive(true);
                foreach(int height in new[]{1920,2280}) {
                    root.GetComponentInChildren<PortraitSafeArea>().SetPreviewMetrics(new Vector2Int(1080,height),new Rect(0,0,1080,height));
                    var host=Object.FindFirstObjectByType<UiScreenHost>();
                    host.SetPreviewMetrics(new Vector2Int(1080,height),new Rect(0,0,1080,height));
                    screen.screens.Open("pvp");yield return null;Canvas.ForceUpdateCanvases();
                    var rank=GameObject.Find("PvP rank 1").transform;
                    AssertPortrait(rank,144,"Player");
                    AssertPortrait(GameObject.Find("My sticky rank").transform,144,"Player");
                    screen.screens.Open("pvp-opponents");yield return null;Canvas.ForceUpdateCanvases();
                    var opponent=GameObject.Find("Opponent tewtee").transform;
                    AssertPortrait(opponent,168,"Name");
                    screen.screens.ShowMainPage();
                }
                LogAssert.NoUnexpectedReceived();
            } finally {Object.DestroyImmediate(root);MoonlitRuntimeSettings.ResetSession();MainScreen.PersistenceEnabled=persistence;}
#else
            Assert.Ignore("Cloud Editor fixture uses the authored runtime catalog.");
            yield return null;
#endif
        }
        static void AssertPortrait(Transform row,float size,string labelName)
        {
            var portrait=row.Cast<Transform>().First(child=>child.name.StartsWith("Avatar "));
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(row,portrait);
            var label=RectTransformUtility.CalculateRelativeRectTransformBounds(row,row.Find(labelName));
            var rect=((RectTransform)row).rect;
            Assert.AreEqual(size,bounds.size.x,.01f);Assert.AreEqual(size,bounds.size.y,.01f);
            Assert.GreaterOrEqual(bounds.min.x,rect.xMin);Assert.LessOrEqual(bounds.max.x,rect.xMax);
            Assert.GreaterOrEqual(bounds.min.y,rect.yMin);Assert.LessOrEqual(bounds.max.y,rect.yMax);
            Assert.Less(bounds.max.x,label.min.x,"Enlarged portrait must leave the player name unobstructed.");
        }
    }
}
