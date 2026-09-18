using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    public sealed partial class RuntimeMainScreenFactory
    {
        void BuildNavigation(MainScreen main,Transform parent)
        {
            var background=Ui.Image("Navigation shared stone panel",parent,0,1920-PortraitSafeArea.NavigationTopFromBottom,1080,180,panels[1]); background.type=Image.Type.Sliced; background.pixelsPerUnitMultiplier=6;
            background.fillCenter=false;
            var navStone=Ui.Image("Navigation stone texture",background.transform,10,10,1060,160,panels[4],new Color(.53f,.53f,.53f)); navStone.type=Image.Type.Tiled;
            AddPanelFlourish(background.transform,1080,180);
            main.navigation=new Button[4];
            string[] names={"Arena","Dungeon","Companions","Shop"}; int[] glyphs={5,6,7,9};
            for(int i=0;i<4;i++) {
                var button=Ui.ArtButton(names[i]+" — icon navigation",background.transform,10+i*270,17,250,146);
                main.navigation[i]=button;
                var icon=Ui.Image("Menu icon",button.transform,66.5f,4,117,118,referenceIcons[glyphs[i]]); icon.preserveAspect=true;
                var close=Ui.Image("Close icon",button.transform,66.5f,4,117,118,PopupSkin.CloseArt); close.preserveAspect=true;
                Ui.Text("Close mark",close.transform,0,-2,117,118,"×",66,font);
                close.gameObject.SetActive(false);
                button.targetGraphic=icon; button.transition=Selectable.Transition.ColorTint;
                var feedback=button.gameObject.AddComponent<ButtonFeedback>(); feedback.artwork=icon.rectTransform;
                if(i<3) Badge(button.transform,174.5f,23,27);
                if(i>0) {
                    Ui.Image("Bronze divider",background.transform,i*270-1,30,2,110,null,new Color(.32f,.25f,.17f));
                    var diamond=Ui.Image("Divider tip",background.transform,i*270-4,28,8,8,null,new Color(.32f,.25f,.17f)); diamond.rectTransform.localRotation=Quaternion.Euler(0,0,45);
                }
            }
        }
    }
}
