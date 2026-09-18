using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Moonlit.UI;

namespace Moonlit.Editor
{
    public static partial class MainScreenBuilder
    {
        static IEnumerator CaptureForgePassFeedback(MainScreen screen,Camera camera,int height,List<string> report,Action fail)
        {
            string aspect=height==1920?"9x16":"9x19";
            var savedForge=ForgeState.Current;var savedReward=RewardState.Current;
            int cleared=screen.highestClearedStage;
            ForgeHammerMotion motion=null;
            try {
                screen.screens.ShowMainPage();
                motion=ForgeHammerMotion.Play(screen.forgeButton.transform);
                // Newly added Graphics need a canvas-registration frame before camera-only rendering.
                // The PlayableGraph uses manual time, so this does not advance any hammer phase.
                yield return null;
                int strike=0;
                foreach(double t in new[]{.20,.53,.86}) {
                    motion.Sample(t);
                    if(motion.ImpactCount!=++strike || motion.GetComponentInChildren<ForgeImpactSparks>().VisibleSparkCount!=18) {
                        report.Add("FAIL forge strike sparks "+strike);fail();yield break;
                    }
                    Canvas.ForceUpdateCanvases();
                    var sparks=motion.GetComponentInChildren<ForgeImpactSparks>();
                    var shown=ReadRewardPixels(camera,1080,height);
                    Color32[] hidden;
                    // Same frame, same hammer pose, same anvil art: only the procedural burst changes.
                    try { sparks.enabled=false;hidden=ReadRewardPixels(camera,1080,height); }
                    finally { sparks.enabled=true;Canvas.ForceUpdateCanvases(); }
                    int changed=0;
                    for(int pixel=0;pixel<shown.Length;pixel++)
                        if(Math.Abs(shown[pixel].r-hidden[pixel].r)+Math.Abs(shown[pixel].g-hidden[pixel].g)+
                            Math.Abs(shown[pixel].b-hidden[pixel].b)>36)changed++;
                    SaveCamera(camera,"Artifacts/Runtime-anvil-strike-"+strike+"-"+aspect+".png",1080,height);
                    if(changed<400) {
                        var viewport=camera.WorldToViewportPoint(sparks.rectTransform.position);
                        throw new InvalidOperationException("Forge strike "+strike+" burst exists but is not visibly rendered: "+
                            changed+" changed pixels, depth="+sparks.depth+
                            ", culled="+sparks.canvasRenderer.cull+", alpha="+sparks.canvasRenderer.GetInheritedAlpha()+
                            ", viewport="+viewport+", layer="+sparks.gameObject.layer);
                    }
                    report.Add("PASS forge strike "+strike+" rendered "+changed+" burst-only pixels "+height);
                }
                UnityEngine.Object.DestroyImmediate(motion.gameObject);motion=null;
                ForgeState.Current=new ForgeState();screen.Refresh();
                var equipment=ForgeState.Current.DrawTier(0,new System.Random(7));equipment.part=EquipmentPart.Weapon;
                ForgeState.Current.equipped[(int)EquipmentPart.Weapon]=equipment;
                screen.Refresh();var toast=PowerChangeToast.Ensure(screen);
                DOTween.Goto(toast,.35f,false);Canvas.ForceUpdateCanvases();
                if(!toast.Visible || !toast.Increased) {report.Add("FAIL power increase toast");fail();yield break;}
                SaveCamera(camera,"Artifacts/Runtime-power-increased-"+aspect+".png",1080,height);
                ForgeState.Current.equipped[(int)EquipmentPart.Weapon]=null;screen.Refresh();
                DOTween.Goto(toast,.35f,false);Canvas.ForceUpdateCanvases();
                if(!toast.Visible || toast.Increased) {report.Add("FAIL power decrease toast");fail();yield break;}
                SaveCamera(camera,"Artifacts/Runtime-power-decreased-"+aspect+".png",1080,height);
                DOTween.Complete(toast,true);
                RewardState.Current=new RewardState();screen.highestClearedStage=237;
                for(int i=0;i<44;i++)RewardState.Current.passClaimed[i]=true;
                screen.screens.Open("progress-pass");Canvas.ForceUpdateCanvases();
                var layer=GameObject.Find("Popup Layer progress-pass");
                var scroll=layer.GetComponentInChildren<ScrollRect>();
                if(scroll.content.anchoredPosition.y<9000 || scroll.content.Find("Node")) {
                    report.Add("FAIL pass first claimable auto scroll or obsolete nodes");fail();yield break;
                }
                SaveCamera(camera,"Artifacts/Runtime-pass-first-claimable-"+aspect+".png",1080,height);
                // Move to the actual 237-stage boundary to show a partially filled connector.
                scroll.content.anchoredPosition=new Vector2(0,45*216);
                Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-pass-current-stage-fill-"+aspect+".png",1080,height);
                report.Add("PASS three anvil impacts, power up/down and pass claimable scroll/progress captures "+height);
            } finally {
                if(motion)UnityEngine.Object.DestroyImmediate(motion.gameObject);
                screen.screens.ShowMainPage();ForgeState.Current=savedForge;RewardState.Current=savedReward;
                screen.highestClearedStage=cleared;screen.Refresh();DOTween.Complete(PowerChangeToast.Ensure(screen),true);
            }
        }
    }
}
