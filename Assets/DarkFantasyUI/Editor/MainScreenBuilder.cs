using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Moonlit.UI;
using Object = UnityEngine.Object;

namespace Moonlit.Editor
{
    [InitializeOnLoad]
    public static partial class MainScreenBuilder
    {
        const string Root="Assets/DarkFantasyUI/";
        const string Request="Library/Moonlit.command";
        static Font font;
        static Sprite frame, circle;
        static Sprite[] icons, referenceIcons, panels;
        static double nextPoll;
        static double verifyAt;
        static MainScreenBuilder() { EditorApplication.update+=Poll; EditorApplication.playModeStateChanged+=OnPlay; }
        static void Poll()
        {
            if(verifyAt>0 && EditorApplication.isPlaying && EditorApplication.timeSinceStartup>verifyAt) { verifyAt=0; Verify(); }
            if(EditorApplication.timeSinceStartup<nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            nextPoll=EditorApplication.timeSinceStartup+1;
            if(!File.Exists(Request)) return;
            string command=File.ReadAllText(Request).Trim(); File.Delete(Request);
            try {
                if(command=="build") Build();
                if(command=="capture") Capture();
                if(command=="verify") { SessionState.SetBool("Moonlit.Verify",true); if(EditorApplication.isPlaying) verifyAt=EditorApplication.timeSinceStartup+1; else EditorApplication.isPlaying=true; }
                if(command=="stop") EditorApplication.isPlaying=false;
                if(command=="reload") { AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(); }
            } catch(Exception e) { Debug.LogException(e); File.WriteAllText("Library/Moonlit.result",e.ToString()); }
        }

        [MenuItem("Moonlit/Build Main Screen")]
        public static void Build()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before rebuilding.");
            var active=EditorSceneManager.GetActiveScene();
            if(active.isDirty && !active.GetRootGameObjects().Any(g=>g.name=="Moonlit Main Canvas" || g.GetComponent<MainScreenBootstrap>())) throw new InvalidOperationException("Save the current scene before building the separate main-screen scene.");
            ImportArt();
            font=AssetDatabase.LoadAssetAtPath<Font>(Root+"Fonts/NotoSansCJKkr-Bold.otf");
            if(!font) throw new InvalidOperationException("Korean bold font not imported.");
            icons=LoadSprites("EquipmentIcons-v2");
            referenceIcons=LoadSprites("InterfaceIcons-v2");
            panels=LoadSprites("InterfaceFrames-v2");
            frame=panels[3];
            circle=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var items=CreateItems();
            var prefab=CreateSlotPrefab();
            const string assetPath=Root+"Data/MainScreenAssets.asset";
            var assets=AssetDatabase.LoadAssetAtPath<MainScreenAssets>(assetPath);
            if(!assets) { assets=ScriptableObject.CreateInstance<MainScreenAssets>(); AssetDatabase.CreateAsset(assets,assetPath); }
            assets.font=font; assets.circle=circle; assets.equipmentIcons=icons; assets.interfaceIcons=referenceIcons; assets.panels=panels; assets.items=items;
            assets.equipmentSlotPrefab=prefab.GetComponent<EquipmentSlot>();
            assets.worldBackground=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/MoonlitRuins.png");
            assets.forgeBackground=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/ForgeBackdrop-v2.png");
            assets.anvil=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/AnvilButton-v2.png");
            EditorUtility.SetDirty(assets);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var bootstrap=new GameObject("Moonlit Runtime Bootstrap").AddComponent<MainScreenBootstrap>(); bootstrap.assets=assets;
            EditorSceneManager.SaveScene(scene,Root+"Scenes/MoonlitMain.unity");
            var builds=EditorBuildSettings.scenes.Where(s=>s.path!=Root+"Scenes/MoonlitMain.unity").ToList();
            builds.Insert(0,new EditorBuildSettingsScene(Root+"Scenes/MoonlitMain.unity",true)); EditorBuildSettings.scenes=builds.ToArray();
            AssetDatabase.SaveAssets(); Selection.activeGameObject=bootstrap.gameObject; ConfigureGameView();
            File.WriteAllText("Library/Moonlit.result","BUILD OK — scene contains only MainScreenBootstrap; UI generated at runtime; assets and slot prefab referenced by MainScreenAssets");
            Debug.Log("[Moonlit] Runtime-only scene prepared. Press Play to create the UI.");
        }
        static ItemDefinition[] CreateItems()
        {
            string[] names={"그림자 두건","서리강철 갑옷","심연의 장갑","푸른 달의 목걸이","금단의 마도서","태양의 날개","달빛 단검","수호자의 벨트","루비 갑충"};
            int[] levels={108,107,108,107,107,74,110,109,56};
            var items=new ItemDefinition[9];
            for(int i=0;i<9;i++) {
                string path=Root+"Data/Item_"+i.ToString("D2")+".asset";
                var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if(!item) { item=ScriptableObject.CreateInstance<ItemDefinition>(); AssetDatabase.CreateAsset(item,path); }
                item.displayName=names[i]; item.icon=icons[i]; item.startingLevel=levels[i]; item.rarity=i==8 ? ItemRarity.Companion : ItemRarity.Legendary;
                item.categoryBadgeIcon=i==1 ? referenceIcons[14] : i==5 ? referenceIcons[12] : null;
                item.description=i==8 ? "어둠 속에서도 함께하는 충직한 동료." : "달빛 폐허의 힘이 깃든 전설 장비.";
                EditorUtility.SetDirty(item); items[i]=item;
            }
            return items;
        }
        static GameObject CreateSlotPrefab()
        {
            var r=Ui.Rect("EquipmentSlot",null,0,0,148,148);
            var slot=r.gameObject.AddComponent<EquipmentSlot>();
            slot.frame=Ui.Image("Frame — reusable 9-slice",r,0,0,148,148,frame); Ui.Stretch(slot.frame.rectTransform);
            slot.equipmentFrame=frame; slot.companionFrame=LoadSprites("CompanionFrame-v2")[0];
            slot.frame.type=Image.Type.Sliced; slot.frame.pixelsPerUnitMultiplier=7;
            slot.frame.raycastTarget=true; slot.Button.targetGraphic=slot.frame;
            var tint=slot.Button.colors; tint.highlightedColor=new Color(1.2f,1.2f,1.2f); tint.pressedColor=new Color(.7f,.83f,1f); slot.Button.colors=tint;
            slot.icon=Ui.Image("Item icon — independent sprite",r,0,0,118,118,null); slot.icon.preserveAspect=true;
            slot.icon.rectTransform.pivot=new Vector2(.5f,.5f);
            slot.icon.rectTransform.anchorMin=new Vector2(.09f,.16f); slot.icon.rectTransform.anchorMax=new Vector2(.91f,.94f); slot.icon.rectTransform.offsetMin=slot.icon.rectTransform.offsetMax=Vector2.zero;
            var scrim=Ui.Image("Level shadow",r,0,0,130,36,null,new Color(.015f,.012f,.006f,.65f));
            scrim.rectTransform.anchorMin=new Vector2(.06f,.045f); scrim.rectTransform.anchorMax=new Vector2(.94f,.29f); scrim.rectTransform.offsetMin=scrim.rectTransform.offsetMax=Vector2.zero;
            slot.levelLabel=Ui.Text("Level — independent text",r,0,0,148,42,"",32,font);
            slot.levelLabel.rectTransform.anchorMin=new Vector2(0,.025f); slot.levelLabel.rectTransform.anchorMax=new Vector2(1,.31f); slot.levelLabel.rectTransform.offsetMin=slot.levelLabel.rectTransform.offsetMax=Vector2.zero;
            slot.starLabel=Ui.Text("Rarity star",r,0,0,28,28,"",26,font,new Color(1,.7f,.15f));
            Ui.Image("Star icon",slot.starLabel.transform,0,0,28,28,referenceIcons[15]);
            var star=slot.starLabel.rectTransform; star.anchorMin=star.anchorMax=new Vector2(.5f,0); star.pivot=new Vector2(.5f,.5f); star.anchoredPosition=new Vector2(0,-2);
            slot.lockedBadge=Ui.Panel("Lock badge",r,113,-7,36,40,new Color(.9f,.84f,.65f)).gameObject;
            Ui.Image("Lock shackle",slot.lockedBadge.transform,10,5,16,20,circle,new Color(.8f,.12f,.06f));
            Ui.Image("Lock aperture",slot.lockedBadge.transform,13,8,10,14,circle,new Color(.9f,.84f,.65f));
            Ui.Image("Lock body",slot.lockedBadge.transform,8,17,20,17,null,new Color(.8f,.12f,.06f));
            Ui.Image("Keyhole",slot.lockedBadge.transform,16,21,4,7,null,new Color(.05f,.025f,.015f));
            var locked=slot.lockedBadge.GetComponent<RectTransform>(); locked.anchorMin=locked.anchorMax=new Vector2(1,1); locked.anchoredPosition=new Vector2(-35,7);
            var category=Ui.Image("Equipment category badge",r,0,0,40,42,circle,new Color(.94f,.91f,.81f));
            category.rectTransform.anchorMin=category.rectTransform.anchorMax=new Vector2(1,1); category.rectTransform.anchoredPosition=new Vector2(-36,8);
            slot.categoryBadge=Ui.Image("Category icon",category.transform,3,3,34,36,null); slot.categoryBadge.preserveAspect=true;
            slot.notificationBadge=Badge(r,118,8,22);
            var badge=slot.notificationBadge.GetComponent<RectTransform>(); badge.anchorMin=badge.anchorMax=new Vector2(1,1); badge.anchoredPosition=new Vector2(-28,-8);
            slot.selection=Ui.Rect("Selection highlight",r,0,0,148,148).gameObject; Ui.Stretch(slot.selection.GetComponent<RectTransform>());
            var select=slot.selection.AddComponent<Image>(); select.sprite=frame; select.type=Image.Type.Sliced; select.pixelsPerUnitMultiplier=6; select.fillCenter=false; select.color=Ui.Cyan; select.raycastTarget=false;
            slot.Bind(null);
            var prefab=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Root+"Prefabs/EquipmentSlot.prefab"); Object.DestroyImmediate(r.gameObject); return prefab;
        }
        static GameObject Badge(Transform p,float x,float y,float size)
        {
            var rim=Ui.Image("Notification",p,x,y,size,size,circle,Ui.Ivory);
            Ui.Image("Red dot",rim.transform,2,2,size-4,size-4,circle,new Color(1,.08f,.08f)); return rim.gameObject;
        }
        static void ConfigureGameView()
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;
            var type=assembly.GetType("UnityEditor.GameView");
            if(type==null) return;
            var view=EditorWindow.GetWindow(type); view.Show();
            // Unity exposes custom Game view sizes only through editor internals.
            try {
                var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
                var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var sizes=singleton.GetProperty("instance").GetValue(null);
                var groupType=assembly.GetType("UnityEditor.GameViewSizeGroupType");
                var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{Enum.Parse(groupType,"Standalone")});
                var gt=group.GetType();
                int count=(int)gt.GetMethod("GetTotalCount").Invoke(group,null); int selected=-1;
                for(int i=0;i<count;i++) {
                    var size=gt.GetMethod("GetGameViewSize").Invoke(group,new object[]{i});
                    if((int)size.GetType().GetProperty("width").GetValue(size)==1080 && (int)size.GetType().GetProperty("height").GetValue(size)==1920) { selected=i; break; }
                }
                if(selected<0) {
                    var sizeType=assembly.GetType("UnityEditor.GameViewSize");
                    var kindType=assembly.GetType("UnityEditor.GameViewSizeType");
                    var size=Activator.CreateInstance(sizeType,new object[]{Enum.Parse(kindType,"FixedResolution"),1080,1920,"Moonlit Portrait"});
                    gt.GetMethod("AddCustomSize").Invoke(group,new[]{size}); selected=count;
                }
                type.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(view,selected);
            } catch(Exception e) { Debug.LogWarning("[Moonlit] Set Game view to 1080 x 1920 manually if needed: "+e.Message); }
        }
        [MenuItem("Moonlit/Capture Main Screen")]
        public static void Capture()
        {
            SessionState.SetBool("Moonlit.Verify",true);
            if(EditorApplication.isPlaying) verifyAt=EditorApplication.timeSinceStartup+1;
            else EditorApplication.isPlaying=true;
        }
        static void OnPlay(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("Moonlit.Verify",false)) return;
            SessionState.SetBool("Moonlit.Verify",false);
            verifyAt=EditorApplication.timeSinceStartup+1;
        }
        static void Verify()
        {
            var screen=Object.FindFirstObjectByType<MainScreen>();
            if(!screen) { Directory.CreateDirectory("Artifacts"); File.WriteAllText("Artifacts/Verification.txt","FAIL: runtime bootstrap did not create MainScreen"); EditorApplication.isPlaying=false; return; }
            screen.StartCoroutine(VerifyRuntime(screen));
        }
        static void AssertRaycast(Button button)
        {
            if(!button) throw new Exception("Missing interactive button");
            var rect=button.GetComponent<RectTransform>(); var canvas=button.GetComponentInParent<Canvas>();
            var position=RectTransformUtility.WorldToScreenPoint(canvas.worldCamera,rect.TransformPoint(rect.rect.center));
            var pointer=new PointerEventData(EventSystem.current) { position=position };
            var results=new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer,results);
            if(results.Count==0 || results[0].gameObject.GetComponentInParent<Button>()!=button) throw new Exception("Blocked button hit target: "+button.name);
        }
    }
}
