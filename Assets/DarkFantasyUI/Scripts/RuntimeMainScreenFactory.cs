using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Moonlit.UI
{
    public sealed partial class RuntimeMainScreenFactory
    {
        readonly MainScreenAssets assets;
        readonly Font font;
        readonly Sprite frame, circle;
        readonly Sprite[] icons, referenceIcons, panels;
        public RuntimeMainScreenFactory(MainScreenAssets assets)
        {
            this.assets=assets; font=assets.font; icons=assets.equipmentIcons;
            referenceIcons=assets.interfaceIcons; panels=assets.panels;
            frame=panels[3]; circle=assets.circle;
        }
        public MainScreen Create(Transform root)
        {
            var camera=new GameObject("UI Camera",typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(root,false); camera.tag="MainCamera";
            camera.orthographic=true; camera.orthographicSize=960;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.006f,.012f,.02f);
            camera.transform.localPosition=new Vector3(0,0,-10); camera.nearClipPlane=.01f; camera.farClipPlane=100;
            var canvas=new GameObject("Runtime UI root",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler)).GetComponent<Canvas>();
            canvas.transform.SetParent(root,false);
            canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=10;
            var scaler=canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1080,1920); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight=0;
            var mainCanvas=LayerCanvas("MainCanvas",canvas.transform,0);
            var mainInput=mainCanvas.gameObject.AddComponent<CanvasGroup>();
            var floor=Ui.Image("Full-bleed stone",mainCanvas.transform,0,0,1080,1920,panels[4],new Color(.4f,.4f,.4f)); floor.type=Image.Type.Tiled; Ui.Stretch(floor.rectTransform);
            var world=Ui.Rect("Elastic battle viewport",mainCanvas.transform,0,0,1080,1000); world.gameObject.AddComponent<RectMask2D>();
            // Main battle scenery has its own asset; collection/shop/pass still use their existing backdrop.
            var worldArt=Ui.ArtImage("Moonlit scenery",world,0,0,1080,1080,
                Resources.Load<Sprite>("Moonlit/Main/EmptyCryptBattle-v1"));
            var safeFrame=Ui.Rect("Device Safe Area",mainCanvas.transform,0,0,1080,1920); Ui.Stretch(safeFrame);
            var design=Ui.Rect("Responsive content",safeFrame,0,0,1080,1920);
            var main=design.gameObject.AddComponent<MainScreen>(); main.font=font; main.design=design; main.icons=icons; main.slotArt=frame;
            var hud=Ui.Rect("Top HUD and events",design,0,0,1080,490);
            BuildHud(main,hud); BuildStage(main,hud);
            var bottom=Ui.Rect("Bottom equipment forge and navigation",design,0,0,1080,PortraitSafeArea.BottomHeight);
            Ui.Image("Forge backdrop",bottom,0,0,1080,680,assets.forgeBackground);
            BattleOverlayLayout.Create(main,bottom,circle);
            var slotRoot=Ui.Rect("Equipment slots",bottom,0,0,1080,400);
            var slots=new List<EquipmentSlot>();
            for(int i=0;i<assets.items.Length;i++) {
                int col=i<5 ? i : i-5; int row=i<5 ? 0 : 1;
                var slot=Object.Instantiate(assets.equipmentSlotPrefab,slotRoot);
                slot.name=assets.items[i].displayName+" Slot";
                var rect=slot.GetComponent<RectTransform>(); rect.anchoredPosition=new Vector2(122+col*171,-(65+row*177)); rect.sizeDelta=new Vector2(i==8 ? 320 : 148,148);
                // Serialized slot labels bypass Ui.Text at runtime. Set the design size explicitly
                // so repeat bindings and domain-reload-free Play sessions never compound scaling.
                slot.levelLabel.fontSize=Ui.ReadableFontSize(32);
                slot.levelLabel.rectTransform.anchorMax=new Vector2(1,.38f);
                slot.SetEmptyIcon(EquipmentPictograms.Icon(i));
                slot.Bind(null); slots.Add(slot);
                slot.name=i<6 ? new[]{"갑옷","귀걸이","모자","목걸이","반지","무기"}[i]+" Slot" : new[]{"엠블렘","날개","정령"}[i-6]+" Slot";
            }
            main.equipment=slots.ToArray();
            int before=bottom.childCount;
            BuildForgeAndChat(main,bottom);
            for(int i=before;i<bottom.childCount;i++) bottom.GetChild(i).GetComponent<RectTransform>().anchoredPosition+=new Vector2(0,935);
            var navigationCanvas=LayerCanvas("NavigationCanvas",canvas.transform,100);
            var navigationInput=navigationCanvas.gameObject.AddComponent<CanvasGroup>();
            var navigationSafe=Ui.Rect("Device Safe Area",navigationCanvas.transform,0,0,1080,1920); Ui.Stretch(navigationSafe);
            var navigationDesign=Ui.Rect("Responsive navigation",navigationSafe,0,0,1080,1920);
            var navigationBottom=Ui.Rect("Bottom navigation",navigationDesign,0,0,1080,PortraitSafeArea.BottomHeight);
            BuildNavigation(main,navigationBottom);
            for(int i=0;i<navigationBottom.childCount;i++) navigationBottom.GetChild(i).GetComponent<RectTransform>().anchoredPosition+=new Vector2(0,935);
            var safe=canvas.gameObject.AddComponent<PortraitSafeArea>(); safe.canvasRect=canvas.GetComponent<RectTransform>(); safe.safeFrame=safeFrame;
            safe.design=design; safe.bottomPanel=bottom; safe.battleViewport=world; safe.battleArt=worldArt.rectTransform;
            var pageHost=Ui.Rect("PageHost",canvas.transform,0,0,1080,1920); Ui.Stretch(pageHost);
            // Keep hierarchy order consistent with canvas sort order for dynamic page canvases.
            pageHost.SetSiblingIndex(navigationCanvas.transform.GetSiblingIndex());
            var popupRoot=Ui.Rect("PopupRoot",canvas.transform,0,0,1080,1920); Ui.Stretch(popupRoot);
            var toastCanvas=LayerCanvas("ToastCanvas",canvas.transform,1000); toastCanvas.GetComponent<GraphicRaycaster>().enabled=false;
            var toastSafe=Ui.Rect("Device Safe Area",toastCanvas.transform,0,0,1080,1920); Ui.Stretch(toastSafe);
            var toastDesign=Ui.Rect("Responsive toasts",toastSafe,0,0,1080,1920); main.toastRoot=toastDesign;
            safe.additionalSafeFrames=new[]{navigationSafe,toastSafe}; safe.additionalDesigns=new[]{navigationDesign,toastDesign}; safe.additionalBottomPanels=new[]{navigationBottom,(RectTransform)null};
            var host=canvas.gameObject.AddComponent<UiScreenHost>(); host.Initialize(assets,main,popupRoot,pageHost,mainInput,navigationInput);
            main.screens=host.Registry;
            ForgeScreenModule.Register(host.Registry);
            ProgressionScreenModule.Register(host.Registry);
            SocialScreenModule.Register(host.Registry);
            RewardsScreenModule.Register(host.Registry);
            main.InitializeGameplay(assets);
            if(!Object.FindFirstObjectByType<EventSystem>()) {
                var events=new GameObject("Runtime EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)); events.transform.SetParent(root,false);
            }
            main.Refresh(); return main;
        }
        static Canvas LayerCanvas(string name,Transform parent,int order)
        {
            var rect=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster)).GetComponent<RectTransform>();
            rect.SetParent(parent,false); Ui.Stretch(rect);
            var layer=rect.GetComponent<Canvas>(); layer.overrideSorting=true; layer.sortingOrder=order; return layer;
        }
        GameObject Badge(Transform parent,float x,float y,float size)
        {
            var rim=Ui.Image("Notification",parent,x,y,size,size,circle,Ui.Ivory);
            Ui.Image("Red dot",rim.transform,2,2,size-4,size-4,circle,new Color(1,.08f,.08f)); return rim.gameObject;
        }
    }
}
