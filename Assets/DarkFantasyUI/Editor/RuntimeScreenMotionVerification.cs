using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Moonlit.UI;

namespace Moonlit.Editor
{
    public static partial class MainScreenBuilder
    {
        static IEnumerator CaptureScreenMotions(MainScreen screen,Camera camera,int height,List<string> report,Action fail)
        {
            string aspect=height==1920?"9x16":"9x19";
            foreach(string route in new[]{"forge-probability","shop","chat"}) {
                screen.screens.ShowMainPage();screen.screens.Open(route);
                var layer=GameObject.Find((route=="shop"?"Page — ":"Popup Layer ")+route);
                var motion=layer?layer.GetComponentInChildren<UiScreenMotion>():null;
                if(!motion || motion.Sequential!=(route!="forge-probability")) {
                    report.Add("FAIL screen animation route mode "+route);fail();yield break;
                }
                DOTween.Goto(motion,.08f,false);Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-"+route+"-entering-"+aspect+".png",1080,height,false);
                motion.Complete();Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-"+route+"-entered-"+aspect+".png",1080,height);
                bool sequential=motion.Sequential;
                screen.screens.ShowMainPage();
                report.Add("PASS "+route+" DOTween "+(sequential?"sequential contents":"modal fade")+" "+height);
            }
        }
    }
}
