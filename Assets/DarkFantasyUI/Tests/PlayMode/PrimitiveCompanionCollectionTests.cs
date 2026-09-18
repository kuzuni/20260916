using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Moonlit.UI.Tests
{
    public sealed class PrimitiveCompanionCollectionTests
    {
        GameObject root;
        UiScreenHost host;
        MainScreenAssets assets;

        [SetUp]
        public void SetUp()
        {
            MoonlitRuntimeSettings.ResetSession();
            root = new GameObject("progression test root", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var popup = Child("popups", root.transform);
            var pages = Child("pages", root.transform);
            var main = Child("main", root.transform).gameObject.AddComponent<CanvasGroup>();
            var navigation = Child("navigation", root.transform).gameObject.AddComponent<CanvasGroup>();
            var screen = root.AddComponent<MainScreen>();
            screen.enabled = false;
            assets = ScriptableObject.CreateInstance<MainScreenAssets>();
            assets.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            screen.font = assets.font;
            screen.toastRoot = root.transform;
            screen.oreText = Label("ore"); screen.goldText = Label("gold"); screen.gemText = Label("gems");
            screen.powerText = Label("power"); screen.stageText = Label("stage"); screen.autoText = Label("auto");
            screen.equipment = new EquipmentSlot[0];
            host = root.AddComponent<UiScreenHost>();
            host.Initialize(assets, screen, popup, pages, main, navigation);
            host.SetPreviewMetrics(new Vector2Int(1080, 1920), new Rect(0, 0, 1080, 1920));
            ProgressionScreenModule.Register(host.Registry);
            screen.screens=host.Registry;
            new GameObject("events", typeof(EventSystem));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(assets);
            foreach (var events in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                Object.DestroyImmediate(events.gameObject);
            MoonlitRuntimeSettings.ResetSession();
        }

        [UnityTest]
        public IEnumerator PrimitiveCompanionArtAndNamesAppearAcrossCollectionDetailsProbabilityAndResultsWithoutFreeUnlocks()
        {
            var catalog=CompanionRigCatalog.Load();
            Assert.IsNotNull(catalog,"Cloud preparation must build the six authored companion entries before runtime tests.");
            Assert.IsTrue(CollectionProgression.Data.categories.All(category=>category.entries.All(entry=>!entry.unlocked)));
            var main=root.GetComponent<MainScreen>();
            string[][] expected={new[]{"원시 꼬마","검치호 새끼","새끼 익룡"},new[]{"원시 랩터","야생 멧돼지","돌바퀴 수레"}};
            for(int category=1;category<=2;category++) {
                host.Registry.Open("skills-pets-heroes");yield return null;
                GameObject.Find("Tab "+CollectionProgression.CategoryNames[category]).GetComponent<Button>().onClick.Invoke();
                Assert.IsNotNull(GameObject.Find("Empty collection"));
                var entries=CollectionProgression.Data.categories[category].entries;
                Assert.IsTrue(entries.All(entry=>!entry.unlocked),"Authored artwork must not grant ownership.");
                for(int variant=0;variant<3;variant++) {
                    var entry=entries[variant];
                    Assert.AreEqual(expected[category-1][variant],entry.Name);
                    Assert.IsNotNull(CompanionRigCatalog.Icon(category,0,variant));
                    entry.unlocked=true;
                }
                GameObject.Find("Tab "+CollectionProgression.CategoryNames[category]).GetComponent<Button>().onClick.Invoke();
                var content=GameObject.Find("Tab content").GetComponentInChildren<ScrollRect>().content;
                var cards=content.GetComponentsInChildren<Button>();
                Assert.AreEqual(3,cards.Length);
                for(int variant=0;variant<3;variant++) {
                    var entry=entries[variant];var card=cards[variant];
                    Assert.AreSame(CompanionRigCatalog.Icon(category,0,variant),card.transform.Find("Icon").GetComponent<Image>().sprite);
                    Assert.IsNull(card.transform.Find("Art pending"));
                    Assert.AreEqual(entry.Name,card.transform.Find("Grade").GetComponent<Text>().text);
                    card.onClick.Invoke();yield return null;
                    var detail=GameObject.Find("Popup Layer skill-details");
                    Assert.AreSame(CompanionRigCatalog.Icon(category,0,variant),
                        detail.GetComponentsInChildren<Image>().Single(image=>image.name=="Icon").sprite);
                    Assert.IsTrue(detail.GetComponentsInChildren<Text>().Single(label=>label.name=="Name").text.Contains(entry.Name));
                    Assert.IsFalse(detail.GetComponentsInChildren<Text>().Single(label=>label.name=="Description").text.Contains("보류"));
                    host.CloseTop();yield return null;
                }
                host.Registry.Open("summon-probability-details");yield return null;
                var probability=GameObject.Find("Popup Layer summon-probability-details");
                var primitive=probability.GetComponentsInChildren<RectTransform>().Single(rect=>rect.name=="Grade 0");
                Assert.AreEqual(3,primitive.GetComponentsInChildren<Image>().Count(image=>image.name=="Icon"));
                var medieval=probability.GetComponentsInChildren<RectTransform>().Single(rect=>rect.name=="Grade 1");
                Assert.AreEqual(3,medieval.GetComponentsInChildren<Text>().Count(label=>label.name=="Art pending"));
                Assert.IsNull(CompanionRigCatalog.Icon(category,1,0));
                Assert.IsFalse(entries[3].unlocked);
                host.CloseTop();yield return null;
                main.petTickets=main.mountTickets=5;
                GameObject.Find("Summon five").GetComponent<Button>().onClick.Invoke();
                var results=GameObject.Find("Summon result cards");
                results.GetComponent<SummonRevealAnimation>().Complete();
                var resultCards=results.GetComponentsInChildren<Button>();
                Assert.AreEqual(5,resultCards.Length);
                foreach(var card in resultCards) {
                    var icon=card.transform.Find("Icon").GetComponent<Image>();
                    Assert.IsTrue(Enumerable.Range(0,3).Any(variant=>icon.sprite==CompanionRigCatalog.Icon(category,0,variant)));
                    Assert.Contains(card.transform.Find("Grade").GetComponent<Text>().text,expected[category-1]);
                    Assert.IsNull(card.transform.Find("Art pending"));
                }
                Assert.IsTrue(entries.Skip(3).All(entry=>!entry.unlocked),"Primitive summons must not unlock another era.");
                host.Registry.ShowMainPage();yield return null;
            }
            LogAssert.NoUnexpectedReceived();
        }
        Text Label(string name)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));
            go.transform.SetParent(root.transform,false);return go.GetComponent<Text>();
        }
        static RectTransform Child(string name,Transform parent)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false);Ui.Stretch(rect);return rect;
        }
    }
}
