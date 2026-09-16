using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Moonlit.UI.Tests
{
    public sealed class UiScreenHostTests
    {
        GameObject root;
        UiScreenHost host;
        CanvasGroup main, navigation;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("test root", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var popup = Child("popups", root.transform); var pages = Child("pages", root.transform);
            main = Child("main", root.transform).gameObject.AddComponent<CanvasGroup>();
            navigation = Child("navigation", root.transform).gameObject.AddComponent<CanvasGroup>();
            var screen = root.AddComponent<MainScreen>();
            screen.enabled = false;
            host = root.AddComponent<UiScreenHost>(); host.Initialize(null, screen, popup, pages, main, navigation);
            new GameObject("events", typeof(EventSystem));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            foreach (var events in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                Object.DestroyImmediate(events.gameObject);
        }

        [UnityTest]
        public IEnumerator ModalStack_PopsOnlyTop_RestoresParentAndFocus()
        {
            int parentValue = 0;
            host.Registry.Register("parent", ScreenPresentation.Modal, context => {
                var state = new GameObject("preserved state"); state.transform.SetParent(context.Root, false); parentValue = 17;
                var opener = new GameObject("child opener", typeof(RectTransform), typeof(Image), typeof(Button)); opener.transform.SetParent(context.Root, false);
                EventSystem.current.SetSelectedGameObject(opener);
                opener.GetComponent<Button>().onClick.AddListener(() => context.Open("child"));
            });
            host.Registry.Register("child", ScreenPresentation.Modal, context => new GameObject("child content").transform.SetParent(context.Root, false));
            host.Registry.Open("parent"); yield return null;
            var opener = GameObject.Find("child opener"); opener.GetComponent<Button>().onClick.Invoke(); yield return null;
            Assert.AreEqual(2, host.ModalDepth); Assert.IsFalse(main.blocksRaycasts); Assert.IsFalse(navigation.blocksRaycasts);
            host.CloseTop(); yield return null;
            Assert.AreEqual(1, host.ModalDepth); Assert.AreEqual(17, parentValue); Assert.IsNotNull(GameObject.Find("preserved state"));
            Assert.AreSame(opener, EventSystem.current.currentSelectedGameObject);
        }

        [UnityTest]
        public IEnumerator DuplicateTap_IsIdempotent_AndDestructionIsClean()
        {
            int builds = 0;
            host.Registry.Register("one", ScreenPresentation.Modal, context => { builds++; new GameObject("content").transform.SetParent(context.Root, false); });
            host.Registry.Open("one"); host.Registry.Open("one");
            Assert.AreEqual(1, host.ModalDepth); Assert.AreEqual(1, builds);
            host.CloseTop(); host.CloseTop(); yield return null;
            Assert.AreEqual(0, host.ModalDepth); Assert.IsTrue(main.blocksRaycasts); Assert.IsTrue(navigation.blocksRaycasts);
            Assert.IsNull(GameObject.Find("content"));
        }

        [UnityTest]
        public IEnumerator PagesReplace_CloseModals_AndBackdropBlocksClicks()
        {
            int clicks = 0;
            var navButton = new GameObject("nav button", typeof(RectTransform), typeof(Image), typeof(Button)); navButton.transform.SetParent(navigation.transform, false);
            navButton.GetComponent<Button>().onClick.AddListener(() => clicks++);
            host.Registry.Register("page-a", ScreenPresentation.Page, context => new GameObject("page state").transform.SetParent(context.Root, false));
            host.Registry.Register("page-b", ScreenPresentation.Page, context => { });
            host.Registry.Register("modal", ScreenPresentation.Modal, context => { });
            host.Registry.Open("page-a"); host.Registry.Open("modal"); yield return null;
            Assert.IsFalse(navButton.GetComponent<Button>().IsInteractable());
            host.Registry.Open("page-b"); yield return null;
            Assert.AreEqual(0, host.ModalDepth); Assert.AreEqual("page-b", host.ActivePageKey); Assert.IsNull(GameObject.Find("page state")); Assert.AreEqual(0, clicks);
        }

        [UnityTest]
        public IEnumerator SafeAreaChange_RefitsEveryOpenLayerWithoutRebuilding()
        {
            RectTransform parent = null, child = null;
            host.Registry.Register("parent", ScreenPresentation.Modal, context => parent = context.Root);
            host.Registry.Register("child", ScreenPresentation.Modal, context => child = context.Root);
            host.Registry.Open("parent"); host.Registry.Open("child");
            host.SetPreviewMetrics(new Vector2Int(1080, 2280), new Rect(36, 84, 1008, 2076));
            yield return null;
            Assert.AreEqual(2, host.ModalDepth);
            Assert.That(parent.rect.height, Is.EqualTo(1080f * 2076f / 1008f).Within(.1f));
            Assert.That(child.rect.height, Is.EqualTo(parent.rect.height).Within(.1f));
            Assert.That(child.localScale.x, Is.EqualTo(1008f / 1080f).Within(.001f));
            host.ClearPreviewMetrics();
        }

        static RectTransform Child(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); Ui.Stretch(rect); return rect;
        }
    }
}
