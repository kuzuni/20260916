using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Moonlit.UI;
using Object=UnityEngine.Object;

namespace Moonlit.Editor
{
    public static partial class MainScreenBuilder
    {
        static IEnumerator VerifyRuntime(MainScreen screen)
        {
            var report=new List<string>();
            var safe=Object.FindFirstObjectByType<PortraitSafeArea>();
            var screenHost=Object.FindFirstObjectByType<UiScreenHost>();
            var canvas=screen.GetComponentInParent<Canvas>().rootCanvas; var camera=canvas.worldCamera;
            var oldTarget=camera.targetTexture; RenderTexture target=null; GameObject hardware=null;
            Directory.CreateDirectory("Artifacts");
            // Simulated physical display metrics; controls must stay within each OS safe rectangle.
            string[] names={"9x16","9x19","9x16-notch","9x19-notch","9x19-side-insets","9x16-resized-back"};
            int[] heights={1920,2280,1920,2280,2280,1920};
            Rect[] areas={new Rect(0,0,1080,1920),new Rect(0,0,1080,2280),new Rect(0,60,1080,1740),new Rect(0,84,1080,2076),new Rect(36,84,1008,2076),new Rect(0,0,1080,1920)};
            bool success=true;
            for(int i=0;i<names.Length;i++) {
                if(target) { camera.targetTexture=oldTarget; target.Release(); Object.DestroyImmediate(target); }
                target=new RenderTexture(1080,heights[i],24); camera.targetTexture=target;
                safe.SetPreviewMetrics(new Vector2Int(1080,heights[i]),areas[i]);
                screenHost.SetPreviewMetrics(new Vector2Int(1080,heights[i]),areas[i]);
                yield return null; yield return null;
                Canvas.ForceUpdateCanvases(); safe.Apply(); Canvas.ForceUpdateCanvases();
                try {
                    if(Mathf.Abs(canvas.pixelRect.height-heights[i])>1) throw new Exception("Canvas did not use the target display resolution");
                    var buttons=canvas.GetComponentsInChildren<Button>().Where(b=>b.gameObject.activeInHierarchy).ToArray();
                    foreach(var button in buttons) { AssertInsideSafe(button.GetComponent<RectTransform>(),camera,areas[i]); AssertRaycast(button); }
                    foreach(var text in canvas.GetComponentsInChildren<Text>().Where(t=>t.gameObject.activeInHierarchy)) AssertInsideSafe(text.rectTransform,camera,areas[i]);
                    var bootstrap=Object.FindFirstObjectByType<MainScreenBootstrap>();
                    if(bootstrap.Build()!=screen || Object.FindObjectsByType<MainScreen>(FindObjectsSortMode.None).Length!=1) throw new Exception("Bootstrap must be idempotent");
                    if(Mathf.Abs(safe.bottomPanel.rect.height-PortraitSafeArea.BottomHeight)>.1f) throw new Exception("Bottom controls changed aspect ratio");
                    var battleImage=safe.battleArt.GetComponent<Image>();
                    if(!battleImage.sprite || battleImage.sprite.name!="ForestBattle-v1" || battleImage.raycastTarget)
                        throw new Exception("Main battle must load its independent non-interactive forest scenery");
                    if(battleImage.sprite==bootstrap.assets.worldBackground)
                        throw new Exception("Main battle scenery must not replace the shared page/card backdrop");
                    screen.Profile(); Canvas.ForceUpdateCanvases();
                    var modal=Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).First(r=>r.name=="Dialog");
                    AssertInsideSafe(modal,camera,areas[i]);
                    Vector2 center=RectTransformUtility.WorldToScreenPoint(camera,modal.TransformPoint(modal.rect.center));
                    if(Vector2.Distance(center,areas[i].center)>2) throw new Exception("Dialog is not centered in the safe area");
                    screen.Close();
                    report.Add("PASS "+names[i]+" 1080x"+heights[i]+" safe="+areas[i]+" buttons="+buttons.Length+" logicalHeight="+safe.LogicalHeight.ToString("0"));
                    if(i==2 || i==3 || i==4) hardware=SimulatedHardware(canvas,heights[i]);
                    SaveCamera(camera,"Artifacts/Runtime-"+names[i]+".png",1080,heights[i]);
                    if(hardware) { Object.DestroyImmediate(hardware); hardware=null; }
                } catch(Exception e) { report.Add("FAIL "+names[i]+": "+e); success=false; break; }
            }
            if(success) yield return CaptureAllRoutes(screen, screenHost, safe, canvas, camera, report,
                value => { target = value; camera.targetTexture = value; }, () => target, () => success=false);
            if(success) {
                try { VerifyInteractions(screen); report.Add("PASS runtime slot binding, pending craft cost, auto settings entry, independent management, navigation, insufficient-resource handling"); }
                catch(Exception e) { report.Add("FAIL interactions: "+e); success=false; }
            }
            if(hardware) Object.DestroyImmediate(hardware);
            camera.targetTexture=oldTarget;
            if(target) { target.Release(); Object.DestroyImmediate(target); }
            safe.ClearPreviewMetrics();
            screenHost.ClearPreviewMetrics();
            File.WriteAllText("Artifacts/Verification.txt",(success ? "PASS" : "FAIL")+" — runtime generation and responsive safe-area validation\n"+string.Join("\n",report)+"\nUnity "+Application.unityVersion+"\nDevice cutouts were simulated in the Editor; physical hardware was not used.");
            if(success) Debug.Log("[Moonlit] Runtime generation and all six viewport checks passed.");
            else Debug.LogError("[Moonlit] Validation failed; see Artifacts/Verification.txt");
            EditorApplication.isPlaying=false;
        }

        static IEnumerator CaptureAllRoutes(MainScreen screen, UiScreenHost host, PortraitSafeArea safe,
            Canvas canvas, Camera camera, List<string> report, Action<RenderTexture> setTarget,
            Func<RenderTexture> getTarget, Action fail)
        {
            string[] routes={
                "forge-probability","forge-probability-details","forge-item-details","dungeon-details",
                "progress-pass","profile","settings","equipment-details","forge-comparison","offline-rewards",
                "player-details","auto-forge","chat","skill-details","summon-probability",
                "summon-probability-details","summon-result","power-ranking","skills-pets-heroes","dungeons",
                "shop","pvp-opponents","pvp","pvp-rewards",
                "profile-name","profile-gender","profile-avatar","settings-language","settings-blocked","settings-account"
            };
            int[] heights={1920,2280};
            Rect[] areas={new Rect(0,60,1080,1740),new Rect(36,84,1008,2076)};
            foreach(var route in routes) foreach(var aspect in new[]{0,1})
            {
                var old=getTarget(); if(old) { camera.targetTexture=null; old.Release(); Object.DestroyImmediate(old); }
                var target=new RenderTexture(1080,heights[aspect],24); setTarget(target);
                safe.SetPreviewMetrics(new Vector2Int(1080,heights[aspect]),areas[aspect]);
                host.SetPreviewMetrics(new Vector2Int(1080,heights[aspect]),areas[aspect]);
                host.Registry.ShowMainPage();
                yield return null; yield return null; Canvas.ForceUpdateCanvases();
                bool pageRoute = route == "skills-pets-heroes" || route == "dungeons" || route == "shop" || route == "pvp";
                // The selected entry intentionally renders a close icon. Build its unobscured
                // reference on main, then let the host independently apply that state on open.
                if (pageRoute) screen.RefreshNavigation(route);
                var navigationPixels = pageRoute ? ReadNavigationPixels(screen, camera) : null;
                if (pageRoute) screen.RefreshNavigation(null);
                bool childRoute=route.StartsWith("profile-") || route.StartsWith("settings-");
                if(childRoute) host.Registry.Open(route.StartsWith("profile-")?"profile":"settings");
                host.Registry.Open(route);
                yield return null; yield return null; Canvas.ForceUpdateCanvases();
                var layer=GameObject.Find((host.ActivePageKey==route ? "Page — " : "Popup Layer ")+route);
                var routeRoot=layer ? layer.transform.Find("SafeArea") as RectTransform : null;
                if(!layer || !routeRoot || routeRoot.rect.height<=0 || (host.ActivePageKey!=route && host.ModalDepth!=(childRoute?2:1)))
                { report.Add("FAIL route "+route+" did not build in a resized safe layer"); fail(); yield break; }
                SaveCamera(camera,"Artifacts/Runtime-"+route+"-"+(aspect==0?"9x16":"9x19")+".png",1080,heights[aspect]);
                bool navigationFailed = false;
                try
                {
                    if (navigationPixels != null) AssertNavigationVisible(navigationPixels, ReadNavigationPixels(screen, camera), screen);
                    if (pageRoute) AssertPageJoinsNavigation(screen, camera, routeRoot);
                    if (route == "shop" || route == "pvp") AssertPageOccludesMain(canvas, camera, routeRoot);
                    foreach (var button in screen.navigation)
                    {
                        AssertInsideSafe(button.GetComponent<RectTransform>(), camera, areas[aspect]);
                        if (host.ActivePageKey == route)
                        {
                            if (!button.IsInteractable()) throw new Exception("Page disabled navigation: " + button.name);
                            AssertRaycast(button);
                        }
                        else if (button.IsInteractable())
                            throw new Exception("Modal leaked navigation input: " + button.name);
                    }
                }
                catch (Exception e)
                {
                    report.Add("FAIL route " + route + " navigation " + (aspect == 0 ? "9:16" : "9:19") + ": " + e);
                    fail(); navigationFailed = true;
                }
                if (navigationFailed) yield break;
                report.Add("PASS route "+route+" "+(aspect==0?"9:16 notch":"9:19 side-insets")+" navigation="+(host.ActivePageKey==route?"clickable":"blocked"));
                if(route=="progress-pass") {
                    // Capture the first three claimed states as separate art overlays too.
                    int claimed=0;
                    foreach(var card in layer.GetComponentsInChildren<RectTransform>(true)) {
                        if(card.name!="Free reward" || claimed>=3) continue;
                        card.GetComponentInChildren<Button>(true).onClick.Invoke();
                        claimed++;
                    }
                    yield return null; Canvas.ForceUpdateCanvases();
                    SaveCamera(camera,"Artifacts/Runtime-progress-pass-claimed-"+(aspect==0?"9x16":"9x19")+".png",1080,heights[aspect]);
                }
                host.Registry.ShowMainPage();
            }
        }
        // Raycast sorting can pass even while a lower-order page obscures the rendered icons.
        // Compare actual pixels against main with the same selected navigation artwork.
        static Color[] ReadNavigationPixels(MainScreen screen, Camera camera)
        {
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            var sample = new Texture2D(16, 16, TextureFormat.RGB24, false);
            var pixels = new Color[screen.navigation.Length * 256];
            try
            {
                RenderTexture.active = camera.targetTexture;
                for (int i = 0; i < screen.navigation.Length; i++)
                {
                    var icon = screen.navigation[i].targetGraphic.rectTransform;
                    var center = RectTransformUtility.WorldToScreenPoint(camera, icon.TransformPoint(icon.rect.center));
                    int x = Mathf.Clamp(Mathf.RoundToInt(center.x) - 8, 0, camera.targetTexture.width - 16);
                    int y = Mathf.Clamp(Mathf.RoundToInt(center.y) - 8, 0, camera.targetTexture.height - 16);
                    sample.ReadPixels(new Rect(x, y, 16, 16), 0, 0);
                    sample.Apply();
                    Array.Copy(sample.GetPixels(), 0, pixels, i * 256, 256);
                }
                return pixels;
            }
            finally { RenderTexture.active = previous; Object.DestroyImmediate(sample); }
        }

        static void AssertNavigationVisible(Color[] expected, Color[] actual, MainScreen screen)
        {
            for (int button = 0; button < screen.navigation.Length; button++)
            {
                float difference = 0;
                for (int pixel = button * 256; pixel < (button + 1) * 256; pixel++)
                    difference += Mathf.Abs(expected[pixel].r - actual[pixel].r)
                        + Mathf.Abs(expected[pixel].g - actual[pixel].g) + Mathf.Abs(expected[pixel].b - actual[pixel].b);
                difference /= 256 * 3;
                if (difference > .025f)
                    throw new Exception("Navigation icon visually obscured: " + screen.navigation[button].name + " mean RGB difference=" + difference);
            }
        }

        // Compare actual rendered geometry, independently of the shared layout constant.
        // A short page mask exposed a strip of the main chat above the navigation rail.
        static void AssertPageJoinsNavigation(MainScreen screen, Camera camera, RectTransform pageRoot)
        {
            var mask = pageRoot.GetComponent<RectMask2D>();
            var rail = (RectTransform)screen.navigation[0].transform.parent;
            var pageEdge = RectTransformUtility.WorldToScreenPoint(camera,
                pageRoot.TransformPoint(new Vector2(pageRoot.rect.center.x, pageRoot.rect.yMin + mask.padding.y)));
            var railEdge = RectTransformUtility.WorldToScreenPoint(camera,
                rail.TransformPoint(new Vector2(rail.rect.center.x, rail.rect.yMax)));
            if (Mathf.Abs(pageEdge.y - railEdge.y) > 1f)
                throw new Exception("Page/navigation seam mismatch: page=" + pageEdge.y + " rail=" + railEdge.y);
        }

        // Toggle the underlying main canvas between two renders of the same page.
        // Any pixel change inside page content means HUD/scenery from main is leaking through.
        static void AssertPageOccludesMain(Canvas canvas, Camera camera, RectTransform pageRoot)
        {
            var mainGroup = canvas.transform.Find("MainCanvas").GetComponent<CanvasGroup>();
            float previousAlpha = mainGroup.alpha;
            try
            {
                var shown = ReadPagePixels(camera, pageRoot);
                mainGroup.alpha = 0;
                var hidden = ReadPagePixels(camera, pageRoot);
                float difference = 0;
                for (int i = 0; i < shown.Length; i++)
                    difference += Mathf.Abs(shown[i].r - hidden[i].r)
                        + Mathf.Abs(shown[i].g - hidden[i].g) + Mathf.Abs(shown[i].b - hidden[i].b);
                difference /= shown.Length * 3;
                if (difference > .002f)
                    throw new Exception("Main canvas bleeds through page; mean RGB difference=" + difference);
            }
            finally { mainGroup.alpha = previousAlpha; Canvas.ForceUpdateCanvases(); }
        }

        static Color[] ReadPagePixels(Camera camera, RectTransform pageRoot)
        {
            Canvas.ForceUpdateCanvases(); camera.Render();
            var previous = RenderTexture.active;
            var target = camera.targetTexture;
            var sample = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                sample.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); sample.Apply();
                var pixels = new Color[32 * 56];
                var rect = pageRoot.rect;
                float bottom = rect.yMin + pageRoot.GetComponent<RectMask2D>().padding.y;
                // Include a dense footer sample so narrow chat leaks do not fall between rows.
                for (int row = 0; row < 56; row++) for (int column = 0; column < 32; column++)
                {
                    var local = new Vector2(Mathf.Lerp(rect.xMin + 4, rect.xMax - 4, (column + .5f) / 32),
                        row < 48 ? Mathf.Lerp(bottom + 2, rect.yMax - 4, (row + .5f) / 48)
                            : Mathf.Lerp(bottom + 2, bottom + 26, (row - 48 + .5f) / 8));
                    var point = RectTransformUtility.WorldToScreenPoint(camera, pageRoot.TransformPoint(local));
                    pixels[row * 32 + column] = sample.GetPixel(Mathf.Clamp(Mathf.RoundToInt(point.x), 0, target.width - 1),
                        Mathf.Clamp(Mathf.RoundToInt(point.y), 0, target.height - 1));
                }
                return pixels;
            }
            finally { RenderTexture.active = previous; Object.DestroyImmediate(sample); }
        }

        static void VerifyInteractions(MainScreen screen)
        {
            var offlineArt=screen.eventButton.targetGraphic as Image;
            var passArt=screen.fairyButton.targetGraphic as Image;
            if(!offlineArt || !offlineArt.sprite || offlineArt.sprite.name!="OfflineRewardIcon-v1" ||
                !passArt || !passArt.sprite || passArt.sprite.name!="ProgressPassIcon-v1")
                throw new Exception("Main reward buttons must load their dedicated clock/chest and sword/pass artwork");
            if(offlineArt.raycastTarget || passArt.raycastTarget || !offlineArt.preserveAspect || !passArt.preserveAspect)
                throw new Exception("Main reward artwork must preserve aspect and use independent hit targets");
            screen.eventButton.onClick.Invoke();
            if(!GameObject.Find("Popup Layer offline-rewards")) throw new Exception("Offline reward icon opened the wrong route");
            screen.Close();
            screen.fairyButton.onClick.Invoke();
            if(!GameObject.Find("Popup Layer progress-pass")) throw new Exception("Progress pass icon opened the wrong route");
            screen.Close();
            int ore=screen.ore,total=screen.equipment.Sum(s=>s.level),locked=screen.equipment[0].level;
            screen.forgeLevelButton.onClick.Invoke(); if(screen.ore!=ore || screen.screens.ModalDepth!=1) throw new Exception("Forge probability route failed"); screen.Close();
            bool hadPending = screen.PendingCraftItem != null;
            screen.forgeButton.onClick.Invoke(); if(screen.ore!=ore-(hadPending?0:100) || screen.screens.ModalDepth!=1) throw new Exception("Comparison route cost or decision UI failed"); screen.Close();
            screen.autoButton.onClick.Invoke(); if(screen.autoForge || screen.screens.ModalDepth!=1) throw new Exception("Auto settings route conflicted with the old immediate toggle"); screen.Close();
            screen.equipment[1].Button.onClick.Invoke(); if(screen.screens.ModalDepth!=1) throw new Exception("Runtime slot click handler missing"); screen.Close();
            for(int i=0;i<screen.navigation.Length;i++) { screen.navigation[i].onClick.Invoke(); if(i!=3 && string.IsNullOrEmpty(Object.FindFirstObjectByType<UiScreenHost>().ActivePageKey)) throw new Exception("Navigation route missing at index "+i); screen.screens.ShowMainPage(); }
            var blank=Object.Instantiate(screen.equipment[1],screen.design); blank.Bind(null);
            if(blank.icon.enabled || blank.levelLabel.text!="" || blank.lockedBadge.activeSelf || blank.notificationBadge.activeSelf) throw new Exception("Empty slot retains stale content");
            blank.Bind(screen.equipment[2].item,8,true,true);
            if(blank.icon.sprite!=screen.equipment[2].item.icon || blank.levelLabel.text!="Lv.8" || !blank.lockedBadge.activeSelf) throw new Exception("Slot rebind failed");
            Object.DestroyImmediate(blank.gameObject);
            screen.ore=0; screen.Forge(); if(screen.ore<0 || screen.autoForge) throw new Exception("Insufficient resources guard failed");
        }
        static void AssertInsideSafe(RectTransform rect,Camera camera,Rect safe)
        {
            var corners=new Vector3[4]; rect.GetWorldCorners(corners);
            foreach(var corner in corners) {
                Vector2 point=RectTransformUtility.WorldToScreenPoint(camera,corner);
                if(point.x<safe.xMin-1 || point.x>safe.xMax+1 || point.y<safe.yMin-1 || point.y>safe.yMax+1) throw new Exception("Outside safe area: "+rect.name+" at "+point+" vs "+safe);
            }
        }
        static GameObject SimulatedHardware(Canvas canvas,int height)
        {
            var parent=Ui.Rect("Simulated device cutout — validation only",canvas.transform,0,0,1080,height);
            Ui.Image("Camera cutout",parent,380,18,320,68,AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),Color.black);
            Ui.Image("Home gesture indicator",parent,400,height-31,280,9,null,new Color(.7f,.7f,.7f));
            return parent.gameObject;
        }
        static void SaveCamera(Camera camera,string path,int width,int height)
        {
            var active=RenderTexture.active;
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active=camera.targetTexture;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,width,height),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG());
            RenderTexture.active=active; Object.DestroyImmediate(image);
        }
    }
}
