using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Moonlit.UI;

namespace Moonlit.Editor
{
    public static partial class MainScreenBuilder
    {
        static IEnumerator CaptureCollectionDetails(MainScreen screen,Camera camera,int height,List<string> report,Action fail)
        {
            var saved=CollectionProgression.Data;
            string aspect=height==1920?"9x16":"9x19";bool valid=true;
            try {
                for(int category=0;category<3;category++) {
                    CollectionProgression.Data=CollectionProgression.Create();
                    var entry=CollectionProgression.Data.categories[category].entries[0];
                    entry.unlocked=true;entry.fragments=3;
                    screen.screens.Open("skills-pets-heroes");yield return null;
                    GameObject.Find("Tab "+CollectionProgression.CategoryNames[category]).GetComponent<Button>().onClick.Invoke();
                    screen.screens.Open("skill-details",entry);
                    for(int frame=0;frame<24;frame++)yield return null;
                    Canvas.ForceUpdateCanvases();
                    var detail=GameObject.Find("Popup Layer skill-details");
                    var buttons=detail.GetComponentsInChildren<Button>();
                    var equip=buttons.SingleOrDefault(button=>button.name=="Equip");
                    var description=detail.GetComponentsInChildren<Text>().Single(text=>text.name=="Description");
                    var passive=detail.GetComponentsInChildren<Text>().Single(text=>text.name=="Passive");
                    var dc=new Vector3[4];var pc=new Vector3[4];
                    description.rectTransform.GetWorldCorners(dc);passive.rectTransform.GetWorldCorners(pc);
                    if(!equip||equip.GetComponentInChildren<Text>().text!="장착"||
                        buttons.Any(button=>button.name.StartsWith("Equip slot "))||
                        description.preferredHeight>description.rectTransform.rect.height+1||dc[0].y<=pc[1].y+4) {
                        valid=false;report.Add("FAIL collection detail must have one Equip action and separated readable description "+category+"/"+aspect);fail();
                    }
                    string key=new[]{"skill","pet","mount"}[category];
                    SaveCamera(camera,"Artifacts/Runtime-"+key+"-details-owned-"+aspect+".png",1080,height);
                    screen.screens.ShowMainPage();yield return null;
                }
                if(valid)report.Add("PASS skill/pet/mount owned details: one equip button and readable separated descriptions "+aspect);
            }
            finally {CollectionProgression.Data=saved;screen.screens.ShowMainPage();}
        }
    }
}
