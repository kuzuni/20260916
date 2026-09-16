using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Moonlit.UI.Tests
{
    public sealed class SocialScreenLayoutTests
    {
        GameObject root;
        UiScreenHost host;
        MainScreenAssets assets;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("social layout test", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            // Match the runtime canvas: the hosted test Game View need not be 1080 pixels wide.
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0;
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
            host = root.AddComponent<UiScreenHost>();
            host.Initialize(assets, screen, popup, pages, main, navigation);
            host.SetPreviewMetrics(new Vector2Int(1080, 1920), new Rect(0, 0, 1080, 1920));
            SocialScreenModule.Register(host.Registry);
            new GameObject("events", typeof(EventSystem));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(assets);
            foreach (var events in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                Object.DestroyImmediate(events.gameObject);
        }

        [UnityTest]
        public IEnumerator Shop_ReservesNavigation_AndHasOnlyRequiredOffersAtBothAspectRatios()
        {
            foreach (var height in new[] { 1920, 2280 })
            {
                host.SetPreviewMetrics(new Vector2Int(1080, height), new Rect(0, 0, 1080, height));
                host.Registry.Open("shop");
                yield return null;
                var scroll = GameObject.Find("Shop page").GetComponentInChildren<ScrollRect>();
                Assert.LessOrEqual(scroll.GetComponent<RectTransform>().rect.height, scroll.transform.parent.GetComponent<RectTransform>().rect.height - 330f);
                CollectionAssert.AreEquivalent(new[] { "Gem offer 60", "Gem offer 220", "Gem offer 800", "Gem offer 1500", "Gem offer 3300" },
                    scroll.content.Cast<Transform>().Where(t => t.name.StartsWith("Gem offer ")).Select(t => t.name));
                Assert.AreEqual("가격 미설정", GameObject.Find("Gem offer 1500").GetComponentInChildren<Button>().name);
                Assert.AreEqual("가격 미설정", GameObject.Find("Gem offer 3300").GetComponentInChildren<Button>().name);
                host.CloseTop();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Pvp_ActionAndStickyRowStayAboveReservedNavigation()
        {
            host.SetPreviewMetrics(new Vector2Int(1080, 1920), new Rect(0, 80, 1080, 1740));
            host.Registry.Open("pvp");
            yield return null;
            var action = GameObject.Find("도전").GetComponent<RectTransform>();
            var sticky = GameObject.Find("My sticky rank").GetComponent<RectTransform>();
            float safeHeight = action.transform.parent.GetComponent<RectTransform>().rect.height;
            Assert.LessOrEqual(-action.anchoredPosition.y + action.rect.height, safeHeight - 210f);
            Assert.LessOrEqual(-sticky.anchoredPosition.y + sticky.rect.height, safeHeight - 210f);
        }

        [UnityTest]
        public IEnumerator RankingChildClose_PreservesParentScrollInstanceAndPosition()
        {
            host.Registry.Open("power-ranking");
            yield return null;
            var scroll = Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Single();
            scroll.verticalNormalizedPosition = .42f;
            Canvas.ForceUpdateCanvases();
            var row = GameObject.Find("Rank 5").GetComponent<RectTransform>();
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, row.TransformPoint(row.rect.center)) };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits);
            Assert.AreSame(row.GetComponent<Button>(), hits[0].gameObject.GetComponentInParent<Button>());
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return null;
            host.CloseTop();
            yield return null;
            Assert.AreSame(scroll, Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Single());
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(.42f).Within(.01f));
        }

        [UnityTest]
        public IEnumerator PointerInsideModalFrame_DoesNotDismissThroughBackdrop()
        {
            host.Registry.Open("profile"); yield return null;
            Canvas.ForceUpdateCanvases();
            var frame = GameObject.Find("프로필 frame").GetComponent<RectTransform>();
            var point = new Vector2(frame.rect.xMin + 18, frame.rect.center.y);
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, frame.TransformPoint(point)) };
            Assert.IsTrue(new Rect(0, 0, Screen.width, Screen.height).Contains(pointer.position),
                $"Profile frame test point {pointer.position} must lie inside the actual {Screen.width}x{Screen.height} test viewport.");
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits);
            Assert.IsTrue(hits[0].gameObject.transform.IsChildOf(frame), "The modal frame or one of its children must intercept the pointer.");
            Assert.AreNotEqual("Dim", hits[0].gameObject.name);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.AreEqual(1, host.ModalDepth);
        }

        static RectTransform Child(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Ui.Stretch(rect);
            return rect;
        }
    }
}
