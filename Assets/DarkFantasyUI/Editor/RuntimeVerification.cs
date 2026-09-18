using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Moonlit.UI;
using Object=UnityEngine.Object;

namespace Moonlit.Editor
{
    public static partial class MainScreenBuilder
    {
        static IEnumerator VerifyRuntimeGuarded(MainScreen screen)
        {
            var failures=new List<string>();
            yield return CaptureGuarded(VerifyRuntime(screen),failures,()=>{});
            if(failures.Count>0){
                Directory.CreateDirectory("Artifacts");
                File.WriteAllText("Artifacts/Verification.txt","FAIL — runtime capture exception\n"+string.Join("\n",failures));
                EditorApplication.isPlaying=false;
            }
        }
        // Unity does not propagate exceptions from nested yielded enumerators to their parent.
        // Flatten each capture so a failed assertion writes evidence and exits rather than idling until CI timeout.
        static IEnumerator CaptureGuarded(IEnumerator operation,List<string> report,Action fail)
        {
            var pending=new Stack<IEnumerator>();pending.Push(operation);
            while(pending.Count>0){
                var current=pending.Peek();bool moved=false;Exception error=null;
                try{moved=current.MoveNext();}catch(Exception e){error=e;}
                if(error!=null){
                    report.Add("FAIL "+current.GetType().Name+": "+error);fail();
                    while(pending.Count>0){try{(pending.Pop() as IDisposable)?.Dispose();}catch(Exception cleanup){report.Add("FAIL capture cleanup: "+cleanup.Message);}}
                    yield break;
                }
                if(!moved){(pending.Pop() as IDisposable)?.Dispose();continue;}
                if(current.Current is IEnumerator child){pending.Push(child);continue;}
                yield return current.Current;
            }
        }

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
                    if(!battleImage.sprite || battleImage.sprite.name!="EmptyCryptBattle-v1" || battleImage.raycastTarget)
                        throw new Exception("Main battle must load its independent non-interactive empty crypt scenery");
                    if(safe.battleArt.parent!=safe.battleViewport || !screen.GetComponent<BattleRuntime>() || !GameObject.Find("Live turn battle"))
                        throw new Exception("Main battle must retain independent scenery and a live combat viewport");
                    if(battleImage.sprite==bootstrap.assets.worldBackground)
                        throw new Exception("Main battle scenery must not replace the shared page/card backdrop");
                    screen.Profile(); Canvas.ForceUpdateCanvases();
                    var modal=Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).First(r=>r.name=="프로필 frame");
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
            if(success) yield return CaptureGameplay(screen,camera,report,()=>success=false);
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
                yield return WaitForBattleCaptureReady(screen);
                Canvas.ForceUpdateCanvases();
                bool pageRoute = route == "skills-pets-heroes" || route == "dungeons" || route == "shop" || route == "pvp";
                // The selected entry intentionally renders a close icon. Build its unobscured
                // reference on main, then let the host independently apply that state on open.
                if (pageRoute) screen.RefreshNavigation(route);
                var navigationPixels = pageRoute ? ReadNavigationPixels(screen, camera) : null;
                if (pageRoute) screen.RefreshNavigation(null);
                if(route=="summon-result"){screen.skillTickets=100;screen.petTickets=100;screen.mountTickets=100;screen.Refresh();}
                foreach(var parent in CaptureParents(route)) {
                    host.Registry.Open(parent);
                    if(host.ActivePageKey!=parent && !GameObject.Find("Popup Layer "+parent)) {
                        report.Add("FAIL route "+route+" missing parent "+parent); fail(); yield break;
                    }
                }
                int expectedDepth=host.ModalDepth+(pageRoute?0:1);
                if(route=="forge-comparison" || route=="equipment-details") {
                    var worn=new EquipmentRoll{id=8001,tier=4,level=25,part=EquipmentPart.Armor,variant=0,
                        affixes=new[]{new EquipmentAffix{kind=EquipmentAffixKind.CriticalChance,percent=10},new EquipmentAffix{kind=EquipmentAffixKind.SkillDamage,percent=15}}};
                    ForgeState.Current.equipped[0]=worn;ForgeRuntime.Ensure(screen).SyncSlots();screen.Refresh();
                    if(route=="forge-comparison") {
                        ForgeState.Current.pending.Clear();
                        ForgeState.Current.pending.Add(new EquipmentRoll{id=8002,tier=4,level=26,part=EquipmentPart.Armor,variant=1,
                            affixes=new[]{new EquipmentAffix{kind=EquipmentAffixKind.DoubleChance,percent=10},new EquipmentAffix{kind=EquipmentAffixKind.Regeneration,percent=3}}});
                        host.Registry.Open(route);
                    } else host.Registry.Open(route,worn);
                } else if(route=="summon-result") {
                    screen.skillTickets=100;screen.petTickets=100;screen.mountTickets=100;screen.Refresh();
                    if(aspect==1)GameObject.Find("Summon quantity").GetComponent<Button>().onClick.Invoke();
                    GameObject.Find("Summon five").GetComponent<Button>().onClick.Invoke();
                } else host.Registry.Open(route);
                yield return null; yield return null; Canvas.ForceUpdateCanvases();
                var layer=GameObject.Find((host.ActivePageKey==route ? "Page — " : "Popup Layer ")+route);
                var routeRoot=layer ? layer.transform.Find("SafeArea") as RectTransform : null;
                if(!layer || !routeRoot || routeRoot.rect.height<=0 || (host.ActivePageKey!=route && host.ModalDepth!=expectedDepth))
                { report.Add("FAIL route "+route+" did not build in a resized safe layer"); fail(); yield break; }
                if(route=="summon-result") {
                    SaveCamera(camera,"Artifacts/Runtime-summon-result-revealing-"+(aspect==0?"9x16":"9x19")+".png",1080,heights[aspect]);
                    var reveal=layer.GetComponentInChildren<SummonRevealAnimation>();
                    float deadline=Time.realtimeSinceStartup+20f;
                    while(reveal && reveal.IsRevealing && Time.realtimeSinceStartup<deadline) yield return null;
                    Canvas.ForceUpdateCanvases();
                    var cards=reveal?reveal.GetComponentsInChildren<CanvasGroup>():new CanvasGroup[0];
                    int expectedCards=aspect==0?5:10;
                    if(!reveal || reveal.IsRevealing || cards.Length!=expectedCards ||
                        cards.Any(card=>card.alpha<.999f || !card.interactable)) {
                        report.Add("FAIL summon result completion "+heights[aspect]+" expected="+expectedCards+" actual="+cards.Length);
                        fail();yield break;
                    }
                    report.Add("PASS summon result completed "+expectedCards+" cards "+heights[aspect]);
                }
                SaveCamera(camera,"Artifacts/Runtime-"+route+"-"+(aspect==0?"9x16":"9x19")+".png",1080,heights[aspect]);
                foreach(var label in layer.GetComponentsInChildren<Text>()) {
                    if(string.IsNullOrWhiteSpace(label.text) || !label.text.Any(char.IsLetterOrDigit)) continue;
                    if(label.preferredHeight>label.rectTransform.rect.height+4)
                        report.Add("REVIEW typography "+route+" "+heights[aspect]+" "+label.name+
                            " font="+label.fontSize+" preferredHeight="+label.preferredHeight.ToString("0.0")+
                            " boxHeight="+label.rectTransform.rect.height.ToString("0.0"));
                }
                bool navigationFailed = false;
                try
                {
                    if (navigationPixels != null) AssertNavigationVisible(navigationPixels, ReadNavigationPixels(screen, camera), screen);
                    if (pageRoute) AssertPageJoinsNavigation(screen, camera, routeRoot);
                    if (route == "shop" || route == "pvp") AssertPageOccludesMain(canvas, camera, routeRoot);
                    if (route == "progress-pass") AssertPassStoneVisible(camera, layer, report);
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
                report.Add("PASS route "+route+" "+(aspect==0?"9:16 notch":"9:19 side-insets")+" navigation="+(host.ActivePageKey==route?"clickable":"blocked")+" modalDepth="+host.ModalDepth);
                if(route=="progress-pass") {
                    screen.highestClearedStage=Math.Max(screen.highestClearedStage,15);
                    for(int i=0;i<3;i++)RewardState.Current.ClaimPass(screen,i);
                    yield return new WaitForSecondsRealtime(.25f);
                    yield return null; Canvas.ForceUpdateCanvases();
                    SaveCamera(camera,"Artifacts/Runtime-progress-pass-claimed-"+(aspect==0?"9x16":"9x19")+".png",1080,heights[aspect]);
                }
                if(route=="auto-forge") {
                    layer.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=0;
                    yield return null;Canvas.ForceUpdateCanvases();
                    SaveCamera(camera,"Artifacts/Runtime-auto-forge-bottom-"+(aspect==0?"9x16":"9x19")+".png",1080,heights[aspect]);
                }
                bool extraFailed=false;
                if(route=="skills-pets-heroes")
                    yield return CaptureCollectionPaymentStates(screen,camera,heights[aspect],report,()=>{extraFailed=true;fail();});
                if(route=="auto-forge")
                    yield return CaptureAutoForgeFilterStates(layer,camera,heights[aspect],report,()=>{extraFailed=true;fail();});
                if(extraFailed)yield break;
                host.Registry.ShowMainPage();
            }
        }

        static IEnumerator CaptureCollectionPaymentStates(MainScreen screen,Camera camera,int height,
            List<string> report,Action fail)
        {
            int skill=screen.skillTickets,pet=screen.petTickets,mount=screen.mountTickets,gems=screen.gems;
            string savedCollection=JsonUtility.ToJson(CollectionProgression.Data);
            var wallet=GameObject.Find("Summon currency icon").GetComponent<Image>();
            int originalCategory=Array.FindIndex(new[]{0,1,2},category=>RewardVisuals.Ticket(category)==wallet.sprite);
            string[] categories={"skill","pet","mount"},modes={"ticket-only","diamond-only","mixed"};
            try
            {
                for(int category=1;category<3;category++) {
                    foreach(var entry in CollectionProgression.Data.categories[category].entries)entry.unlocked=false;
                    GameObject.Find("Tab "+CollectionProgression.CategoryNames[category]).GetComponent<Button>().onClick.Invoke();
                    yield return null;Canvas.ForceUpdateCanvases();
                    SaveCamera(camera,"Artifacts/Runtime-collection-"+(category==1?"pet":"mount")+"-empty-"+(height==1920?"9x16":"9x19")+".png",1080,height);
                }
                for(int category=0;category<3;category++)
                    foreach(int id in new[]{0,2,4,6,8,10,12})
                        CollectionProgression.Data.categories[category].entries[id].unlocked=true;
                for(int category=0;category<3;category++)for(int mode=0;mode<3;mode++)
                {
                    int tickets=mode==0?10:mode==1?0:2;
                    if(category==0)screen.skillTickets=tickets;
                    else if(category==1)screen.petTickets=tickets;
                    else screen.mountTickets=tickets;
                    screen.gems=1000;
                    GameObject.Find("Tab "+CollectionProgression.CategoryNames[category]).GetComponent<Button>().onClick.Invoke();
                    screen.Refresh();
                    yield return null;Canvas.ForceUpdateCanvases();
                    bool failed=false;
                    try
                    {
                        var row=GameObject.Find("Summon cost row");
                        var labels=row.GetComponentsInChildren<Text>();
                        var icons=row.GetComponentsInChildren<Image>();
                        string primary=mode==0?"5":mode==1?"500":"2";
                        if(labels.First(label=>label.name=="Summon cost").text!=primary ||
                            icons.Length!=(mode==2?2:1) || labels.Length!=(mode==2?3:1))
                            throw new Exception("Wrong cost quantities or extra zero-cost elements");
                        var ticketIcon=RewardVisuals.Ticket(category);
                        var primaryIcon=icons.First(icon=>icon.name=="Summon cost icon");
                        var diamondIcon=RewardVisuals.Icon(screen,RewardVisuals.Kind.Diamond);
                        if(!ticketIcon || !diamondIcon || wallet.sprite!=ticketIcon ||
                            primaryIcon.sprite!=(mode==1?diamondIcon:ticketIcon))
                            throw new Exception("Wrong category or primary cost sprite");
                        if(mode==2 && (labels.First(label=>label.name=="Cost plus").text!="+" ||
                            labels.First(label=>label.name=="Summon diamond cost").text!="300" ||
                            icons.First(icon=>icon.name=="Summon diamond icon").sprite!=diamondIcon))
                            throw new Exception("Mixed payment did not show ticket + diamond costs");
                        SaveCamera(camera,"Artifacts/Runtime-collection-"+categories[category]+"-"+modes[mode]+"-"+(height==1920?"9x16":"9x19")+".png",1080,height);
                        report.Add("PASS collection cost "+categories[category]+" "+modes[mode]+" "+height);
                    }
                    catch(Exception e){report.Add("FAIL collection cost "+categories[category]+" "+modes[mode]+": "+e);fail();failed=true;}
                    if(failed)yield break;
                }
            }
            finally
            {
                screen.skillTickets=skill;screen.petTickets=pet;screen.mountTickets=mount;screen.gems=gems;
                CollectionProgression.Data=JsonUtility.FromJson<CollectionSave>(savedCollection);
                var originalTab=GameObject.Find("Tab "+CollectionProgression.CategoryNames[Math.Max(0,originalCategory)]);
                if(originalTab)originalTab.GetComponent<Button>().onClick.Invoke();
                screen.Refresh();
            }
        }

        static IEnumerator CaptureAutoForgeFilterStates(GameObject layer,Camera camera,int height,
            List<string> report,Action fail)
        {
            var state=ForgeState.Current;
            bool originalEnabled=state.filterEnabled;
            int originalMask=state.affixMask,originalLevel=state.level;
            var originalTiers=(bool[])state.keepTiers.Clone();
            var scroll=layer.GetComponentInChildren<ScrollRect>();
            float originalScroll=scroll.verticalNormalizedPosition;
            var toggles=layer.GetComponentsInChildren<Toggle>(true);
            var master=toggles.First(toggle=>toggle.name=="Enable affix filter toggle");
            var affixes=toggles.Where(toggle=>toggle.name.StartsWith("Affix filter ")).ToArray();
            try
            {
                foreach(bool enabled in new[]{false,true})
                {
                    master.isOn=enabled;scroll.verticalNormalizedPosition=1;
                    yield return null;Canvas.ForceUpdateCanvases();
                    bool failed=false;
                    try
                    {
                        var rates=EquipmentRules.TierProbabilities(state.level);
                        var grades=layer.GetComponentsInChildren<Image>().Where(image=>image.name.StartsWith("Keep grade ")).ToArray();
                        if(state.filterEnabled!=enabled || affixes.Length!=9 ||
                            affixes.Any(toggle=>toggle.gameObject.activeInHierarchy!=enabled))
                            throw new Exception("Affix filter visibility does not match switch");
                        if(state.level!=originalLevel || state.affixMask!=originalMask || !state.keepTiers.SequenceEqual(originalTiers))
                            throw new Exception("Toggling filter changed retained selections or forge level");
                        if(grades.Length!=rates.Count(rate=>rate>0) ||
                            grades.Any(grade=>rates[int.Parse(grade.name.Substring("Keep grade ".Length))]<=0))
                            throw new Exception("Forge displayed an unavailable grade");
                        SaveCamera(camera,"Artifacts/Runtime-auto-forge-filter-"+(enabled?"on":"off")+"-"+(height==1920?"9x16":"9x19")+".png",1080,height);
                        report.Add("PASS auto forge filter "+(enabled?"on":"off")+" "+height+" retained selections, available grades only");
                    }
                    catch(Exception e){report.Add("FAIL auto forge filter "+height+": "+e);fail();failed=true;}
                    if(failed)yield break;
                }
            }
            finally
            {
                if(master)master.isOn=originalEnabled;
                if(scroll)scroll.verticalNormalizedPosition=originalScroll;
            }
        }

        static string[] CaptureParents(string route)
        {
            if(route.StartsWith("profile-")) return new[]{"profile"};
            if(route.StartsWith("settings-")) return new[]{"settings"};
            switch(route) {
                case "forge-probability-details": return new[]{"forge-probability"};
                case "forge-item-details": return new[]{"forge-probability","forge-probability-details"};
                case "dungeon-details": return new[]{"dungeons"};
                case "skill-details":
                case "summon-probability":
                case "summon-result": return new[]{"skills-pets-heroes"};
                case "summon-probability-details": return new[]{"skills-pets-heroes","summon-probability"};
                case "power-ranking": return new[]{"profile"};
                case "pvp-opponents":
                case "pvp-rewards": return new[]{"pvp"};
                default: return Array.Empty<string>();
            }
        }

        // Prove the painted face contributes to the rendered image. A valid loaded sprite
        // can still be completely hidden by an opaque sibling supplied by a frame helper.
        static void AssertPassStoneVisible(Camera camera, GameObject layer, List<string> report)
        {
            var backing=layer.GetComponentsInChildren<Image>().First(i=>i.name=="Pass stone backing");
            if(!backing.sprite) throw new Exception("Pass stone sprite missing");
            var original=backing.color;
            var previous=RenderTexture.active;
            var sample=new Texture2D(16,16,TextureFormat.RGB24,false);
            try {
                // Empty strip between the prompt and premium purchase, clear of live text.
                var center=RectTransformUtility.WorldToScreenPoint(camera,
                    backing.rectTransform.TransformPoint(new Vector2(backing.rectTransform.rect.center.x,-229)));
                var area=new Rect(Mathf.RoundToInt(center.x)-8,Mathf.RoundToInt(center.y)-8,16,16);
                Color[] Read() {
                    Canvas.ForceUpdateCanvases(); camera.Render();
                    RenderTexture.active=camera.targetTexture;
                    sample.ReadPixels(area,0,0); sample.Apply();
                    return sample.GetPixels();
                }
                var normal=Read();
                backing.color=Color.magenta;
                var tinted=Read();
                float difference=0;
                for(int i=0;i<normal.Length;i++) difference+=Mathf.Abs(normal[i].g-tinted[i].g);
                difference/=normal.Length;
                if(difference<.01f) throw new Exception("Pass stone face is visually occluded; tint response="+difference);
                report.Add("PASS pass stone face contributes rendered pixels; green-channel tint response="+difference);
            }
            finally {
                backing.color=original; Canvas.ForceUpdateCanvases();
                RenderTexture.active=previous; Object.DestroyImmediate(sample);
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
            int ore=screen.ore;
            screen.forgeLevelButton.onClick.Invoke(); if(screen.ore!=ore || screen.screens.ModalDepth!=1) throw new Exception("Forge probability route failed"); screen.Close();
            screen.autoButton.onClick.Invoke(); if(screen.autoForge || screen.screens.ModalDepth!=1) throw new Exception("Auto settings must not start forging"); screen.Close();
            for(int i=0;i<screen.navigation.Length;i++) { screen.navigation[i].onClick.Invoke(); if(string.IsNullOrEmpty(Object.FindFirstObjectByType<UiScreenHost>().ActivePageKey)) throw new Exception("Navigation route missing at index "+i); screen.screens.ShowMainPage(); }
            var blank=Object.Instantiate(screen.equipment[1],screen.design); blank.roll=null;blank.Bind(null);
            if(blank.icon.enabled || blank.levelLabel.text!="" || blank.lockedBadge.activeSelf || blank.notificationBadge.activeSelf) throw new Exception("Empty slot retains stale content");
            var item=ForgeRuntime.Ensure(screen).Definition(new EquipmentRoll{tier=0,level=8,part=EquipmentPart.Armor});
            blank.Bind(item,8,true,true);
            if(blank.icon.sprite!=item.icon || blank.levelLabel.text!="Lv.8" || !blank.lockedBadge.activeSelf) throw new Exception("Slot rebind failed");
            Object.DestroyImmediate(blank.gameObject);
        }
        static IEnumerator CaptureGameplay(MainScreen screen,Camera camera,List<string> report,Action fail)
        {
            while(screen.screens.ModalDepth>0)screen.screens.CloseTop();
            screen.screens.ShowMainPage();
            var battle=screen.GetComponent<BattleRuntime>();
            float combatDeadline=Time.realtimeSinceStartup+8;
            while(battle && (battle.PlayerState==null || battle.PlayerResolvedBasicAttacks+battle.EnemyResolvedBasicAttacks==0) && Time.realtimeSinceStartup<combatDeadline)yield return null;
            if(!battle || battle.PlayerState==null || battle.EnemyState==null || battle.PlayerResolvedBasicAttacks+battle.EnemyResolvedBasicAttacks==0) {
                report.Add("FAIL real bootstrap did not progress into an animation-event combat turn");fail();yield break;
            }
            Canvas.ForceUpdateCanvases();
            if(!battle.PlayerHud || !battle.EnemyHud || !battle.PlayerHud.HealthText || string.IsNullOrEmpty(battle.PlayerHud.HealthText.text)) {
                report.Add("FAIL world health bars not ready");fail();yield break;
            }
            if(screen.roundText==null || !screen.roundText.text.StartsWith("라운드 ") || screen.waveNodes==null || screen.waveNodes.Length!=3) {
                report.Add("FAIL stage nodes/round label binding");fail();yield break;
            }
            if(screen.powerText.cachedTextGenerator.lineCount>1) {
                report.Add("FAIL large power value wraps in the main HUD");fail();yield break;
            }
            SaveCamera(camera,"Artifacts/Runtime-live-combat.png",1080,camera.targetTexture.height);
            report.Add("PASS real bootstrap starts combat and resolves animation-event attacks; large HUD power remains one line");
            ForgeState.Current.pending.Clear();ForgeState.Current.autoEnabled=false;
            int ore=screen.ore;
            var forge=ForgeRuntime.Ensure(screen);
            bool revealed=false, prematureComparison=false;Exception revealError=null;
            float started=Time.realtimeSinceStartup,revealedAt=0;
            Action onReveal=()=>{
                revealed=true;revealedAt=Time.realtimeSinceStartup;
                prematureComparison=screen.screens.ModalDepth!=0;
                try { SaveCamera(camera,"Artifacts/Runtime-forge-equipment-reveal.png",1080,camera.targetTexture.height); }
                catch(Exception e){revealError=e;}
            };
            forge.HandRevealed+=onReveal;
            screen.forgeButton.onClick.Invoke();
            if(screen.ore!=ore-1 || screen.screens.ModalDepth!=0 || !forge.Busy) {
                forge.HandRevealed-=onReveal;report.Add("FAIL forge must spend one hammer and wait for animation");fail();yield break;
            }
            float deadline=Time.realtimeSinceStartup+15;
            while(forge.Busy && Time.realtimeSinceStartup<deadline)yield return null;
            forge.HandRevealed-=onReveal;
            if(!revealed || prematureComparison || revealedAt-started<.95f || revealError!=null) {
                report.Add("FAIL observed forge reveal/order: revealed="+revealed+" elapsed="+(revealedAt-started)+" error="+revealError);fail();yield break;
            }
            if(forge.Busy || Time.realtimeSinceStartup-revealedAt<.49f || screen.screens.ModalDepth!=1 || ForgeState.Current.Pending==null) {
                report.Add("FAIL forge comparison must follow anvil and half-second reveal");fail();yield break;
            }
            SaveCamera(camera,"Artifacts/Runtime-forge-timed-result.png",1080,camera.targetTexture.height);
            int id=ForgeState.Current.Pending.id;
            screen.Close();screen.Forge();
            if(screen.ore!=ore-1 || ForgeState.Current.Pending.id!=id) {report.Add("FAIL pending forge result charged twice");fail();yield break;}
            screen.Close();
            report.Add("PASS one-hammer forge sequence, delayed comparison and persistent pending result");
            var previousTarget=camera.targetTexture;
            var safe=Object.FindFirstObjectByType<PortraitSafeArea>();
            var host=Object.FindFirstObjectByType<UiScreenHost>();
            int previousCaptureRate=Time.captureFramerate;
            float previousTimeScale=Time.timeScale;
            Time.captureFramerate=30;Time.timeScale=1;
            try {
                foreach(int height in new[]{1920,2280}) {
                    var previewTarget=new RenderTexture(1080,height,24);camera.targetTexture=previewTarget;
                    try {
                        var area=height==1920?new Rect(0,60,1080,1740):new Rect(36,84,1008,2076);
                        safe.SetPreviewMetrics(new Vector2Int(1080,height),area);
                        host.SetPreviewMetrics(new Vector2Int(1080,height),area);
                        yield return null;yield return null;Canvas.ForceUpdateCanvases();
                        yield return CaptureGuarded(CaptureSkillChoreographies(screen,camera,height,report,fail),report,fail);
                        yield return CaptureGuarded(CaptureBattleOverlay(screen,camera,height,report,fail),report,fail);
                        yield return CaptureGuarded(CaptureRewardAvailability(screen,camera,height,report,fail),report,fail);
                    yield return CaptureGuarded(CaptureCollectionDetails(screen,camera,height,report,fail),report,fail);
                yield return CaptureGuarded(CaptureInstantScreens(screen,camera,height,report,fail),report,fail);
                        yield return CaptureGuarded(CaptureForgePassFeedback(screen,camera,height,report,fail),report,fail);
                        yield return CaptureGuarded(CaptureSkillHudFeedback(screen,camera,height,report,fail),report,fail);
                        yield return CaptureGuarded(CaptureCombatFeedback(screen,camera,height,report,fail),report,fail);
                yield return CaptureGuarded(CapturePrimitiveCompanions(screen,camera,height,report,fail),report,fail);
                        yield return CaptureGuarded(CaptureAscension(screen,camera,height,report,fail),report,fail);
                        yield return CaptureGuarded(CaptureCollectionAscensions(screen,camera,height,report,fail),report,fail);
                        yield return CaptureGuarded(CaptureDungeonClaims(screen,camera,height,report,fail),report,fail);
                    }
                    finally {camera.targetTexture=previousTarget;previewTarget.Release();Object.DestroyImmediate(previewTarget);}
                }
            }
            finally {Time.captureFramerate=previousCaptureRate;Time.timeScale=previousTimeScale;}
            report.Add("CAPTURE all 30 themed skills in four choreography phases at both aspect ratios");
        }

        static IEnumerator CaptureBattleOverlay(MainScreen screen,Camera camera,int height,List<string> report,Action fail)
        {
            var battle=screen.GetComponent<BattleRuntime>();
            var progress=screen.GetComponent<StageWaveProgress>();
            var hud=screen.GetComponentInChildren<EquippedSkillHud>(true);
            string saved=JsonUtility.ToJson(CollectionProgression.Data);
            bool screenEnabled=screen.enabled,battleEnabled=battle.enabled;
            try {
                screen.enabled=false;battle.StopAllCoroutines();
                foreach(var actor in new[]{battle.PlayerHud.Actor,battle.EnemyHud.Actor}) {
                    var animator=CaptureAuthoredAnimator(actor);animator.Play("Idle",0,0);animator.Update(0);
                }
                battle.PlayerHud.SetVisible(true);battle.EnemyHud.SetVisible(true);
                for(int i=0;i<3;i++) {
                    var entry=CollectionProgression.Data.categories[0].entries[i];
                    entry.unlocked=true;CollectionProgression.Equip(entry,i);
                }
                hud.Refresh();
                yield return new WaitForSeconds(1.1f);
                Canvas.ForceUpdateCanvases();
                string aspect=height==1920?"9x16":"9x19";
                if(screen.navigation.Length!=4 || hud.GetComponentsInChildren<Button>().Length!=3) {
                    report.Add("FAIL annotated HUD: four navigation buttons and three equipped skills required");fail();yield break;
                }
                SaveCamera(camera,"Artifacts/Runtime-equipped-battle-skills-"+aspect+".png",1080,height);
                progress.SetProgress(900,1);progress.SetProgress(900,2);
                var tweens=DOTween.TweensByTarget(progress,false);
                if(tweens==null || tweens.Count==0) {report.Add("FAIL wave transition has no tween");fail();yield break;}
                foreach(var tween in tweens)tween.Goto(.2f,false);
                Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-wave-line-filling-"+aspect+".png",1080,height);
                foreach(var tween in tweens)tween.Goto(.5f,false);
                Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-wave-node-pop-"+aspect+".png",1080,height);
                foreach(var tween in tweens)tween.Goto(.7f,false);
                for(int kind=0;kind<3;kind++) {
                    var target=kind==2?battle.PlayerHud:battle.EnemyHud;
                    target.Float(kind==2?"+16k":"16k",kind==2?new Color(.3f,1,.42f):kind==1?new Color(1,.15f,.18f):Color.white);
                    yield return null;Canvas.ForceUpdateCanvases();
                    SaveCamera(camera,"Artifacts/Runtime-combat-number-"+new[]{"normal","critical","healing"}[kind]+"-"+aspect+".png",1080,height);
                    yield return new WaitForSeconds(1.1f);
                }
                report.Add("PASS annotated combat HUD, equipped turn indicators, animated wave line/node and three amount colors "+height);
            }
            finally {
                CollectionProgression.Data=JsonUtility.FromJson<CollectionSave>(saved);
                screen.enabled=screenEnabled;
                battle.enabled=false;battle.enabled=battleEnabled;
                if(progress)progress.SetProgress(screen.stage,battle.Wave);
                if(hud)hud.Refresh();
            }
        }

        static IEnumerator CaptureDungeonClaims(MainScreen screen,Camera camera,int height,List<string> report,Action fail)
        {
            var previous=DungeonProgression.Data;
            int ore=screen.ore,skill=screen.skillTickets,pet=screen.petTickets,mount=screen.mountTickets;
            try
            {
                for(int index=0;index<4;index++)
                {
                    DungeonProgression.Data=new DungeonSave { pendingClaim=true,pendingIndex=index,pendingDifficulty=2 };
                    int before=index==0?screen.ore:index==1?screen.skillTickets:index==2?screen.petTickets:screen.mountTickets;
                    screen.screens.Open("dungeon-reward");
                    yield return null;Canvas.ForceUpdateCanvases();
                    string aspect=height==1920?"9x16":"9x19";
                    SaveCamera(camera,"Artifacts/Runtime-dungeon-"+index+"-claim-"+aspect+".png",1080,height);
                    var claim=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name=="Claim dungeon reward");
                    if(!claim) { report.Add("FAIL dungeon reward claim button missing");fail();yield break; }
                    claim.onClick.Invoke();claim.onClick.Invoke();
                    int after=index==0?screen.ore:index==1?screen.skillTickets:index==2?screen.petTickets:screen.mountTickets;
                    if(DungeonProgression.Data.pendingClaim || DungeonProgression.Data.keys[index]!=1 ||
                        after-before!=DungeonProgression.Reward(index,2))
                    { report.Add("FAIL dungeon claim must charge one key and grant once for "+index);fail();yield break; }
                    var effects=screen.toastRoot.GetComponentsInChildren<RewardVisualLifetime>();
                    if(effects.Length==0) { report.Add("FAIL dungeon reward absorption missing");fail();yield break; }
                    // The reward tween is unscaled. Seek its actual sequences so slow render frames cannot skip the capture.
                    foreach(var effect in effects)
                    {
                        var tweens=DOTween.TweensByTarget(effect,false);
                        if(tweens!=null)foreach(var tween in tweens)tween.Goto(.45f,false);
                    }
                    Canvas.ForceUpdateCanvases();
                    SaveCamera(camera,"Artifacts/Runtime-dungeon-"+index+"-absorption-"+aspect+".png",1080,height);
                    foreach(var effect in effects)if(effect)Object.DestroyImmediate(effect.gameObject);
                }
                report.Add("PASS dungeon reward modal, single key/claim and four currency absorption captures at "+height);
            }
            finally
            {
                while(screen.screens.ModalDepth>0)screen.screens.CloseTop();
                DungeonProgression.Data=previous;
                screen.ore=ore;screen.skillTickets=skill;screen.petTickets=pet;screen.mountTickets=mount;
                screen.Refresh();
            }
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
