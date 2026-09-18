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
        static IEnumerator CaptureRewardAvailability(MainScreen screen,Camera camera,int height,List<string> report,Action fail)
        {
            var savedRewards=RewardState.Current;var savedDungeons=DungeonProgression.Data;
            var savedForge=ForgeState.Current;var savedCollections=CollectionProgression.Data;
            int stage=screen.stage,cleared=screen.highestClearedStage,gold=screen.gold,gems=screen.gems,ore=screen.ore;
            int skill=screen.skillTickets,pet=screen.petTickets,mount=screen.mountTickets;
            bool enabled=screen.enabled;string aspect=height==1920?"9x16":"9x19";
            try {
                screen.enabled=false;screen.stage=1;screen.highestClearedStage=0;
                CollectionProgression.Data=CollectionProgression.Create();ForgeState.Current=new ForgeState();screen.gold=0;
                screen.skillTickets=screen.petTickets=screen.mountTickets=0;
                var state=RewardState.Current=new RewardState {lastTickUtc=DateTimeOffset.UtcNow.ToUnixTimeSeconds()+3600};
                DungeonProgression.Data=new DungeonSave();DungeonProgression.RefreshDay(DateTime.UtcNow);
                Array.Clear(DungeonProgression.Data.keys,0,4);
                for(int i=0;i<5;i++)state.TryUseArenaAttempt(DateTime.UtcNow);
                state.dailyDiamondClaimDay=DateTime.UtcNow.AddHours(9).ToString("yyyy-MM-dd");
                var dots=screen.GetComponent<RewardNotificationDots>();dots.RefreshNow();Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-reward-dots-none-"+aspect+".png",1080,height);
                state.arenaDay="";state.dailyDiamondClaimDay="";state.accruedSeconds=60;
                screen.skillTickets=1;screen.highestClearedStage=5;DungeonProgression.Data.keys[0]=1;
                dots.RefreshNow();Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-reward-dots-available-"+aspect+".png",1080,height);
                var forgeState=ForgeState.Current=new ForgeState {level=4};
                forgeState.filledSegments=forgeState.Segments;forgeState.upgradeEndsUtcTicks=DateTime.UtcNow.AddMinutes(1).Ticks;
                screen.Refresh();dots.RefreshNow();Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-forge-timer-running-"+aspect+".png",1080,height);
                forgeState.upgradeEndsUtcTicks=DateTime.UtcNow.AddSeconds(-1).Ticks;dots.RefreshNow();Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-forge-timer-complete-"+aspect+".png",1080,height);
                forgeState.upgradeEndsUtcTicks=0;dots.RefreshNow();Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-forge-time-start-ready-"+aspect+".png",1080,height);
                ForgeState.Current=savedForge;screen.Refresh();dots.RefreshNow();
                screen.screens.Open("progress-pass");yield return null;Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-pass-claim-dot-"+aspect+".png",1080,height);
                screen.screens.CloseTop();
                screen.screens.Open("wallet");yield return null;Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-wallet-icons-"+aspect+".png",1080,height);
                screen.screens.CloseTop();
                screen.screens.Open("shop");yield return null;Canvas.ForceUpdateCanvases();
                var shopScroll=GameObject.Find("Page — shop").GetComponentInChildren<ScrollRect>();
                shopScroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-shop-daily-diamonds-"+aspect+".png",1080,height);
                var claim=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name=="Claim daily diamonds");
                if(!claim){report.Add("FAIL daily diamond shop claim is missing");fail();yield break;}
                int before=screen.gems;claim.onClick.Invoke();
                // Hold immediately: a slow first frame must not finish/destroy the reward before registration.
                var particles=screen.toastRoot.GetComponentsInChildren<RewardVisualLifetime>();
                foreach(var life in particles)life.HoldAt(.16f);
                yield return null;
                foreach(var refresh in UnityEngine.Object.FindObjectsByType<LiveUiRefresh>(FindObjectsSortMode.None))
                    refresh.RefreshView?.Invoke();
                if(particles.Length!=1 || !particles[0] || particles[0].GetComponentsInChildren<Image>().Length!=12){
                    report.Add("FAIL shop absorption particles are missing");fail();yield break;
                }
                dots.RefreshNow();Canvas.ForceUpdateCanvases();
                foreach(var bit in particles[0].GetComponentsInChildren<Image>()) {
                    Vector3 viewport=camera.WorldToViewportPoint(bit.rectTransform.TransformPoint(bit.rectTransform.rect.center));
                    if(viewport.z<=0 || viewport.x<0 || viewport.x>1 || viewport.y<0 || viewport.y>1)
                        throw new InvalidOperationException("Shop reward particle is outside the camera viewport: "+viewport);
                }
                // Compare two actual renders in the same frame; object existence alone missed invisible effects.
                var shown=ReadRewardPixels(camera,1080,height);
                var group=particles[0].GetComponent<CanvasGroup>();float alpha=group.alpha;
                Color32[] hidden;
                try { group.alpha=0;hidden=ReadRewardPixels(camera,1080,height); }
                finally { group.alpha=alpha; }
                int changed=0;
                for(int pixel=0;pixel<shown.Length;pixel++)
                    if(Math.Abs(shown[pixel].r-hidden[pixel].r)+Math.Abs(shown[pixel].g-hidden[pixel].g)+
                        Math.Abs(shown[pixel].b-hidden[pixel].b)>36)changed++;
                if(changed<400) {
                    SaveCamera(camera,"Artifacts/Runtime-shop-invisible-reward-"+aspect+".png",1080,height);
                    throw new InvalidOperationException("Shop reward exists but does not render visibly: "+changed+
                        " changed pixels. "+RewardRenderDiagnostics(particles[0],camera));
                }
                SaveCamera(camera,"Artifacts/Runtime-shop-diamond-absorption-"+aspect+".png",1080,height);
                report.Add("PASS shop reward rendered "+changed+" distinct overlay pixels inside viewport "+height);
                foreach(var life in particles)if(life)life.Resume();
                if(screen.gems!=before+100 || state.CanClaimDailyDiamonds || RewardNotificationDots.NavigationAvailable(screen,3)) {
                    report.Add("FAIL daily diamond claim did not settle exactly once");fail();
                } else report.Add("PASS reward availability markers, pass claim markers and daily shop absorption "+height);
                // The effect intentionally advances by displayed frames, not stalled wall time.
                // Finish this captured claim explicitly so slow hosted rendering cannot carry its
                // diamonds/+100 into the following pass or anvil captures.
                foreach(var life in particles)if(life){life.Resume();life.SampleAge(2f);}
                yield return null; // OnComplete destroys the layer at the end of this frame.
                if(particles.Any(life=>life)) {
                    report.Add("FAIL shop reward capture did not clean up its completed effects");fail();yield break;
                }
            } finally {
                while(screen.screens.ModalDepth>0)screen.screens.CloseTop();screen.screens.ShowMainPage();
                RewardState.Current=savedRewards;DungeonProgression.Data=savedDungeons;
                ForgeState.Current=savedForge;CollectionProgression.Data=savedCollections;
                screen.stage=stage;screen.highestClearedStage=cleared;screen.gold=gold;screen.gems=gems;screen.ore=ore;
                screen.skillTickets=skill;screen.petTickets=pet;screen.mountTickets=mount;screen.enabled=enabled;
                screen.Refresh();screen.GetComponent<RewardNotificationDots>().RefreshNow();
            }
        }
        static string RewardRenderDiagnostics(RewardVisualLifetime effect,Camera camera)
        {
            var lines=new List<string>();
            foreach(var canvas in effect.GetComponentsInParent<Canvas>(true))
                lines.Add("canvas="+canvas.name+" enabled="+canvas.enabled+" root="+canvas.isRootCanvas+
                    " mode="+canvas.renderMode+" override="+canvas.overrideSorting+" order="+canvas.sortingOrder+
                    " camera="+(canvas.worldCamera?canvas.worldCamera.name:"none"));
            foreach(var graphic in effect.GetComponentsInChildren<Graphic>(true))
                lines.Add(graphic.name+" active="+graphic.gameObject.activeInHierarchy+" enabled="+graphic.enabled+
                    " depth="+graphic.depth+" culled="+graphic.canvasRenderer.cull+
                    " alpha="+graphic.canvasRenderer.GetInheritedAlpha()+" scale="+graphic.transform.lossyScale+
                    " visibleLayer="+((camera.cullingMask&(1<<graphic.gameObject.layer))!=0));
            return string.Join("; ",lines);
        }

        static Color32[] ReadRewardPixels(Camera camera,int width,int height)
        {
            var previous=RenderTexture.active;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            try {
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=camera.targetTexture;
                image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                return image.GetPixels32();
            } finally { RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image); }
        }
        static IEnumerator CaptureAscension(MainScreen screen,Camera camera,int height,List<string> report,Action fail)
        {
            var saved=ForgeState.Current;int gold=screen.gold,gems=screen.gems;
            string aspect=height==1920?"9x16":"9x19";
            try {
                var state=ForgeState.Current=new ForgeState {level=35,filledSegments=5,upgradeEndsUtcTicks=DateTime.UtcNow.AddSeconds(-1).Ticks};
                screen.Refresh();ForgeRuntime.Ensure(screen).SyncSlots();
                screen.screens.Open("forge-probability");yield return null;Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-forge-ascension-ready-"+aspect+".png",1080,height);
                var upgrade=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name=="업그레이드");
                if(!upgrade){report.Add("FAIL ascension action missing");fail();yield break;}
                upgrade.onClick.Invoke();yield return new WaitForSecondsRealtime(.25f);Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-forge-ascended-probability-"+aspect+".png",1080,height);
                if(state.ascension!=1 || state.level!=1){report.Add("FAIL forge ascension UI did not reset to level one");fail();yield break;}
                screen.screens.CloseTop();screen.screens.Open("forge-probability-details");yield return null;Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-forge-ascended-catalogue-"+aspect+".png",1080,height);
                screen.screens.CloseTop();
                var random=new System.Random(72);var worn=state.DrawTier(0,random);worn.part=EquipmentPart.Weapon;
                var fresh=state.DrawTier(0,random);fresh.part=EquipmentPart.Weapon;
                state.equipped[(int)EquipmentPart.Weapon]=worn;state.pending.Add(fresh);
                ForgeRuntime.Ensure(screen).SyncSlots();
                screen.screens.Open("forge-comparison");yield return null;Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-comparison-new-below-"+aspect+".png",1080,height);
                state.ToggleEquip(fresh.id);ForgeRuntime.Ensure(screen).SyncSlots();
                screen.screens.CloseTop();screen.screens.Open("forge-comparison");yield return null;Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-comparison-new-above-"+aspect+".png",1080,height);
                screen.screens.CloseTop();Canvas.ForceUpdateCanvases();
                SaveCamera(camera,"Artifacts/Runtime-main-ascended-equipment-"+aspect+".png",1080,height);
                report.Add("PASS ascension claim reset and persisted new equipment marker captures "+height);
            } finally {
                while(screen.screens.ModalDepth>0)screen.screens.CloseTop();
                ForgeState.Current=saved;screen.gold=gold;screen.gems=gems;ForgeRuntime.Ensure(screen).SyncSlots();screen.Refresh();
            }
        }
    }
}
