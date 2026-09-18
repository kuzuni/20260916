using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Moonlit.UI;

namespace Moonlit.Editor
{
    public static partial class MainScreenBuilder
    {
        static IEnumerator CaptureInstantScreens(MainScreen screen,Camera camera,int height,List<string> report,Action fail)
        {
            string aspect=height==1920?"9x16":"9x19";
            foreach(string route in new[]{"forge-probability","shop","chat"}) {
                screen.screens.ShowMainPage();screen.screens.Open(route);
                var layer=GameObject.Find((route=="shop"?"Page — ":"Popup Layer ")+route);
                if(!layer) { report.Add("FAIL instant screen missing "+route);fail();yield break; }
                foreach(var group in layer.GetComponentsInChildren<CanvasGroup>(true)) {
                    if(group.alpha<.999f) {
                        report.Add("FAIL screen content must be visible immediately "+route+"/"+group.name);
                        fail();yield break;
                    }
                }
                Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-"+route+"-instant-"+aspect+".png",1080,height);
                screen.screens.ShowMainPage();
                report.Add("PASS "+route+" opens immediately without entrance effects "+height);
            }
        }
    }
}
