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
            var canvas=screen.GetComponentInParent<Canvas>(); var camera=canvas.worldCamera;
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
                yield return null; yield return null;
                Canvas.ForceUpdateCanvases(); safe.Apply(); Canvas.ForceUpdateCanvases();
                try {
                    if(Mathf.Abs(canvas.pixelRect.height-heights[i])>1) throw new Exception("Canvas did not use the target display resolution");
                    var buttons=screen.GetComponentsInChildren<Button>().Where(b=>b.gameObject.activeInHierarchy).ToArray();
                    foreach(var button in buttons) { AssertInsideSafe(button.GetComponent<RectTransform>(),camera,areas[i]); AssertRaycast(button); }
                    foreach(var text in screen.GetComponentsInChildren<Text>().Where(t=>t.gameObject.activeInHierarchy)) AssertInsideSafe(text.rectTransform,camera,areas[i]);
                    var bootstrap=Object.FindFirstObjectByType<MainScreenBootstrap>();
                    if(bootstrap.Build()!=screen || Object.FindObjectsByType<MainScreen>(FindObjectsSortMode.None).Length!=1) throw new Exception("Bootstrap must be idempotent");
                    if(Mathf.Abs(safe.bottomPanel.rect.height-PortraitSafeArea.BottomHeight)>.1f) throw new Exception("Bottom controls changed aspect ratio");
                    screen.Profile(); Canvas.ForceUpdateCanvases();
                    var modal=screen.design.Find("Modal overlay/Dialog").GetComponent<RectTransform>();
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
            if(success) {
                try { VerifyInteractions(screen); report.Add("PASS runtime slot binding, forge, lock exclusion, auto toggle, independent management, navigation, insufficient-resource handling"); }
                catch(Exception e) { report.Add("FAIL interactions: "+e); success=false; }
            }
            if(hardware) Object.DestroyImmediate(hardware);
            camera.targetTexture=oldTarget;
            if(target) { target.Release(); Object.DestroyImmediate(target); }
            safe.ClearPreviewMetrics();
            File.WriteAllText("Artifacts/Verification.txt",(success ? "PASS" : "FAIL")+" — runtime generation and responsive safe-area validation\n"+string.Join("\n",report)+"\nUnity "+Application.unityVersion+"\nDevice cutouts were simulated in the Editor; physical hardware was not used.");
            if(success) Debug.Log("[Moonlit] Runtime generation and all six viewport checks passed.");
            else Debug.LogError("[Moonlit] Validation failed; see Artifacts/Verification.txt");
            EditorApplication.isPlaying=false;
        }
        static void VerifyInteractions(MainScreen screen)
        {
            int ore=screen.ore,total=screen.equipment.Sum(s=>s.level),locked=screen.equipment[0].level;
            screen.forgeLevelButton.onClick.Invoke(); if(screen.ore!=ore || !screen.design.Find("Modal overlay")) throw new Exception("Forge management failed"); screen.Close();
            screen.forgeButton.onClick.Invoke(); if(screen.ore!=ore-100 || screen.equipment.Sum(s=>s.level)!=total+1 || screen.equipment[0].level!=locked) throw new Exception("Forge cost / locked item exclusion failed");
            screen.autoButton.onClick.Invoke(); if(!screen.autoForge) throw new Exception("Auto toggle failed"); screen.autoButton.onClick.Invoke();
            screen.equipment[1].Button.onClick.Invoke(); if(!screen.design.Find("Modal overlay")) throw new Exception("Runtime slot click handler missing"); screen.Close();
            foreach(var nav in screen.navigation) { nav.onClick.Invoke(); screen.Close(); }
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
