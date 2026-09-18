using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Moonlit.UI.Tests
{
    public sealed class SummonResultTapTests
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

        static void PauseReveal(SummonRevealAnimation reveal)
        {
            var sequence=(DG.Tweening.Sequence)typeof(SummonRevealAnimation).GetField("sequence",
                System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(reveal);
            DG.Tweening.TweenExtensions.Pause(sequence);
        }
        static PointerEventData Click() => new PointerEventData(EventSystem.current) {
            button=PointerEventData.InputButton.Left,position=new Vector2(100,100),pressPosition=new Vector2(100,100)
        };

        [UnityTest]
        public IEnumerator EveryCategoryTapCompletesThenDismissesWithoutAnotherPurchase()
        {
            var main=root.GetComponent<MainScreen>();
            for(int category=0;category<3;category++) {
                main.skillTickets=main.petTickets=main.mountTickets=5;main.gems=1000;
                host.Registry.Open("skills-pets-heroes");yield return null;
                GameObject.Find("Tab "+CollectionProgression.CategoryNames[category]).GetComponent<Button>().onClick.Invoke();
                GameObject.Find("Summon five").GetComponent<Button>().onClick.Invoke();
                var results=GameObject.Find("Summon result cards");
                var reveal=results.GetComponent<SummonRevealAnimation>();PauseReveal(reveal);
                Assert.IsNull(GameObject.Find("Continue"));
                Assert.AreEqual(1,host.ModalDepth);
                Assert.IsTrue(reveal.IsRevealing);
                var card=results.GetComponentInChildren<Button>();
                Assert.IsFalse(card.enabled,"Result cards must route taps to the reveal rather than opening skill details.");
                ExecuteEvents.ExecuteHierarchy(card.gameObject,Click(),ExecuteEvents.pointerClickHandler);
                Assert.IsFalse(reveal.IsRevealing);
                Assert.AreEqual(1,host.ModalDepth,"First tap finishes the reveal without closing.");
                Assert.IsTrue(results.GetComponentsInChildren<CanvasGroup>().All(group=>group.alpha==1&&group.blocksRaycasts));
                Assert.AreEqual(5,CollectionProgression.Data.categories[category].entries.Sum(entry=>entry.fragments));
                Assert.AreEqual(1000,main.gems);
                yield return null;
                // Also verify the outside-SafeArea dim forwards to the same input owner.
                var dim=GameObject.Find("Popup Layer summon-result").transform.Find("Dim");
                ExecuteEvents.Execute(dim.gameObject,Click(),ExecuteEvents.pointerClickHandler);
                Assert.AreEqual(0,host.ModalDepth);
                Assert.AreEqual(5,CollectionProgression.Data.categories[category].entries.Sum(entry=>entry.fragments));
                Assert.AreEqual(0,category==0?main.skillTickets:category==1?main.petTickets:main.mountTickets);
                host.Registry.ShowMainPage();yield return null;
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator FiveHundredResultsRemainScrollableAndDragNeverSkipsOrDismisses()
        {
            var main=root.GetComponent<MainScreen>();main.skillTickets=500;
            host.Registry.Open("skills-pets-heroes");yield return null;
            var quantity=GameObject.Find("Summon quantity").GetComponent<Button>();
            for(int i=0;i<7&&quantity.GetComponentInChildren<Text>().text!="x500";i++)quantity.onClick.Invoke();
            GameObject.Find("Summon five").GetComponent<Button>().onClick.Invoke();
            var results=GameObject.Find("Summon result cards");
            var reveal=results.GetComponent<SummonRevealAnimation>();PauseReveal(reveal);
            var tap=GameObject.Find("Popup Layer summon-result").GetComponentInChildren<SummonResultTap>();
            var drag=Click();drag.dragging=true;tap.OnPointerClick(drag);
            Assert.IsTrue(reveal.IsRevealing);Assert.AreEqual(1,host.ModalDepth);
            drag.dragging=false;drag.position+=Vector2.up*100;tap.OnPointerClick(drag);
            Assert.IsTrue(reveal.IsRevealing,"A displaced pointer must not be treated as a tap.");
            reveal.Complete();
            var scroll=GameObject.Find("Summon results scroll").GetComponent<ScrollRect>();
            scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();yield return null;
            var last=(RectTransform)GameObject.Find("Result card 499").transform;
            Assert.IsTrue(scroll.viewport.rect.Contains(scroll.viewport.InverseTransformPoint(last.TransformPoint(last.rect.center))));
            tap.OnPointerClick(drag);Assert.AreEqual(1,host.ModalDepth);
            tap.OnPointerClick(Click());Assert.AreEqual(0,host.ModalDepth);
            Assert.AreEqual(500,CollectionProgression.Data.categories[0].entries.Sum(entry=>entry.fragments));
            Assert.AreEqual(0,main.skillTickets);
        }

        [UnityTest]
        public IEnumerator CollectionBalancesAndCostsUseMainHudCurrencySizingInAllTabs()
        {
            var main=root.GetComponent<MainScreen>();main.skillTickets=main.petTickets=main.mountTickets=2;main.gems=1000;
            foreach(int height in new[]{1920,2280}) {
                host.SetPreviewMetrics(new Vector2Int(1080,height),new Rect(0,0,1080,height));
                host.Registry.Open("skills-pets-heroes");yield return null;
                for(int category=0;category<3;category++) {
                    GameObject.Find("Tab "+CollectionProgression.CategoryNames[category]).GetComponent<Button>().onClick.Invoke();
                    Assert.AreEqual(new Vector2(77,77),GameObject.Find("Summon currency icon").GetComponent<RectTransform>().sizeDelta);
                    Assert.AreEqual(new Vector2(65,77),GameObject.Find("Summon diamond balance icon").GetComponent<RectTransform>().sizeDelta);
                    Assert.AreEqual(Ui.ReadableFontSize(30),GameObject.Find("Currency").GetComponent<Text>().resizeTextMaxSize);
                    var row=GameObject.Find("Summon cost row").GetComponent<RectTransform>();
                    var icons=row.GetComponentsInChildren<Image>();Assert.AreEqual(2,icons.Length);
                    foreach(var icon in icons)Assert.AreEqual(77,icon.rectTransform.rect.height);
                    foreach(RectTransform child in row) {
                        var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(row,child);
                        Assert.GreaterOrEqual(bounds.min.x,row.rect.xMin-.1f);
                        Assert.LessOrEqual(bounds.max.x,row.rect.xMax+.1f);
                    }
                    Assert.AreEqual("2",row.Find("Summon cost").GetComponent<Text>().text);
                    Assert.AreEqual("300",row.Find("Summon diamond cost").GetComponent<Text>().text);
                }
                host.Registry.ShowMainPage();yield return null;
            }
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
