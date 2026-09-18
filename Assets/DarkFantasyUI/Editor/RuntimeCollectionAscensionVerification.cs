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
        static IEnumerator AwaitSummonCapture()
        {
            yield return null;
            var reveal=UnityEngine.Object.FindFirstObjectByType<SummonRevealAnimation>();
            for(int frame=0;reveal && reveal.IsRevealing && frame<300;frame++)yield return null;
        }
        static bool SummonCaptureReady(int count,bool starred)
        {
            var root=GameObject.Find("Summon result cards");if(!root)return false;
            var reveal=root.GetComponent<SummonRevealAnimation>();
            if(!reveal || reveal.IsRevealing || root.transform.childCount!=count)return false;
            foreach(Transform card in root.transform) {
                var group=card.GetComponent<CanvasGroup>();
                if(!group || group.alpha<.99f || !group.interactable)return false;
                if(starred && !card.GetComponentsInChildren<Text>().Any(t=>t.name=="Collection stars"&&t.text=="★"))return false;
            }
            return true;
        }
        static IEnumerator CaptureCollectionAscensions(MainScreen screen,Camera camera,int height,List<string> report,Action fail)
        {
            var saved=CollectionProgression.Data;
            int skill=screen.skillTickets,pet=screen.petTickets,mount=screen.mountTickets,gems=screen.gems;
            string aspect=height==1920?"9x16":"9x19";
            try {
                for(int category=0;category<3;category++) {
                    CollectionProgression.Data=CollectionProgression.Create();
                    var current=CollectionProgression.Data.categories[category];current.summonLevel=99;current.experience=70;
                    screen.skillTickets=screen.petTickets=screen.mountTickets=500;screen.gems=10000;
                    screen.screens.Open("skills-pets-heroes");yield return null;
                    GameObject.Find("Tab "+CollectionProgression.CategoryNames[category]).GetComponent<Button>().onClick.Invoke();
                    var quantity=GameObject.Find("Summon quantity").GetComponent<Button>();
                    for(int i=0;i<7 && quantity.GetComponentInChildren<Text>().text!="x500";i++)quantity.onClick.Invoke();
                    if(quantity.GetComponentInChildren<Text>().text!="x500") {report.Add("FAIL missing x500 summon option");fail();yield break;}
                    GameObject.Find("Summon five").GetComponent<Button>().onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();
                    string key=new[]{"skill","pet","mount"}[category];
                    SaveCamera(camera,"Artifacts/Runtime-"+key+"-summon-limit-confirm-"+aspect+".png",1080,height);
                    var confirm=GameObject.Find("Confirm limited summon");
                    if(!confirm){report.Add("FAIL truncated summon must require explicit confirmation");fail();yield break;}
                    confirm.GetComponent<Button>().onClick.Invoke();
                    yield return AwaitSummonCapture();
                    if(!SummonCaptureReady(30,false)){report.Add("FAIL limited summon cards not fully revealed");fail();yield break;}
                    Canvas.ForceUpdateCanvases();
                    int tickets=category==0?screen.skillTickets:category==1?screen.petTickets:screen.mountTickets;
                    if(tickets!=470 || screen.gems!=10000 || current.summonLevel!=100) {
                        report.Add("FAIL limited summon must debit exactly 30 tickets and reach 100");fail();yield break;
                    }
                    SaveCamera(camera,"Artifacts/Runtime-"+key+"-limited-summon-results-"+aspect+".png",1080,height);
                    screen.screens.CloseTop();
                    GameObject.Find("Probability").GetComponent<Button>().onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();
                    SaveCamera(camera,"Artifacts/Runtime-"+key+"-ascension-ready-"+aspect+".png",1080,height);
                    var ascend=GameObject.Find("Ascend summon category");
                    if(!ascend){report.Add("FAIL collection ascension action missing");fail();yield break;}
                    ascend.GetComponent<Button>().onClick.Invoke();yield return null;
                    if(CollectionProgression.Data.categories[category].ascension!=1 || CollectionProgression.Data.categories[category].entries.Any(e=>e.unlocked)) {
                        report.Add("FAIL category ascension did not reset its collection");fail();yield break;
                    }
                    while(screen.screens.ModalDepth>0)screen.screens.CloseTop();
                    quantity=GameObject.Find("Summon quantity").GetComponent<Button>();
                    for(int i=0;i<7 && quantity.GetComponentInChildren<Text>().text!="x5";i++)quantity.onClick.Invoke();
                    GameObject.Find("Summon five").GetComponent<Button>().onClick.Invoke();
                    yield return AwaitSummonCapture();
                    if(!SummonCaptureReady(5,true)){report.Add("FAIL ascended summon cards must finish and show one star");fail();yield break;}
                    Canvas.ForceUpdateCanvases();
                    SaveCamera(camera,"Artifacts/Runtime-"+key+"-ascended-results-"+aspect+".png",1080,height);
                    screen.screens.CloseTop();Canvas.ForceUpdateCanvases();
                    SaveCamera(camera,"Artifacts/Runtime-"+key+"-ascended-collection-"+aspect+".png",1080,height);
                    screen.screens.ShowMainPage();
                    report.Add("PASS "+key+" x500 limited to 30 after confirmation, category reset and starred new summons "+height);
                    CollectionProgression.Data=CollectionProgression.Create();
                    screen.skillTickets=screen.petTickets=screen.mountTickets=500;
                    screen.screens.Open("skills-pets-heroes");yield return null;
                    GameObject.Find("Tab "+CollectionProgression.CategoryNames[category]).GetComponent<Button>().onClick.Invoke();
                    quantity=GameObject.Find("Summon quantity").GetComponent<Button>();
                    for(int i=0;i<7 && quantity.GetComponentInChildren<Text>().text!="x500";i++)quantity.onClick.Invoke();
                    GameObject.Find("Summon five").GetComponent<Button>().onClick.Invoke();
                    yield return AwaitSummonCapture();
                    tickets=category==0?screen.skillTickets:category==1?screen.petTickets:screen.mountTickets;
                    if(!SummonCaptureReady(500,false)||tickets!=0){report.Add("FAIL full 500 summon count, charge or completed reveal");fail();yield break;}
                    Canvas.ForceUpdateCanvases();
                    SaveCamera(camera,"Artifacts/Runtime-"+key+"-500-results-top-"+aspect+".png",1080,height);
                    var scroll=GameObject.Find("Summon results scroll").GetComponent<ScrollRect>();
                    scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();yield return null;
                    var last=(RectTransform)GameObject.Find("Result card 499").transform;
                    if(!scroll.viewport.rect.Contains(scroll.viewport.InverseTransformPoint(last.TransformPoint(last.rect.center)))) {
                        report.Add("FAIL full 500 last result must be reachable in viewport");fail();yield break;
                    }
                    SaveCamera(camera,"Artifacts/Runtime-"+key+"-500-results-bottom-"+aspect+".png",1080,height);
                    screen.screens.CloseTop();screen.screens.ShowMainPage();
                    report.Add("PASS "+key+" full 500 results reveal and last-card scroll "+height);
                }
            } finally {
                while(screen.screens.ModalDepth>0)screen.screens.CloseTop();screen.screens.ShowMainPage();
                CollectionProgression.Data=saved;screen.skillTickets=skill;screen.petTickets=pet;screen.mountTickets=mount;screen.gems=gems;screen.Refresh();
            }
        }
    }
}
