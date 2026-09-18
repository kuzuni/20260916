using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    public static class ForgeBatchDropdown
    {
        public static Dropdown Create(Transform parent,MainScreen main,Font font,ForgeState state)
        {
            var background=Ui.Image("Batch dropdown",parent,436,0,238,65,PopupSkin.ActionArt);
            background.type=Image.Type.Sliced;background.pixelsPerUnitMultiplier=8;background.raycastTarget=true;
            var dropdown=background.gameObject.AddComponent<Dropdown>();
            dropdown.targetGraphic=background;
            var caption=Ui.Text("Batch size",background.transform,12,0,172,65,"",32,font);
            Ui.Text("Dropdown arrow",background.transform,187,0,42,65,"▼",24,font,Ui.Gold);
            dropdown.captionText=caption;
            var template=Ui.Image("Batch choices template",background.transform,0,68,238,366,PopupSkin.PanelArt);
            template.type=Image.Type.Sliced;template.pixelsPerUnitMultiplier=8;template.raycastTarget=true;
            var viewport=Ui.Rect("Viewport",template.transform,8,8,222,350);
            var viewportImage=viewport.gameObject.AddComponent<Image>();viewportImage.color=new Color(.015f,.025f,.04f,.97f);viewportImage.raycastTarget=true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content=Ui.Rect("Content",viewport,0,0,222,58);
            var item=Ui.Image("Item",content,0,0,222,58,null,new Color(.025f,.1f,.17f));
            item.raycastTarget=true;
            var toggle=item.gameObject.AddComponent<Toggle>();toggle.targetGraphic=item;
            var check=Ui.Image("Selected",item.transform,5,6,212,46,PopupSkin.ActionArt,new Color(.5f,.85f,1));
            check.type=Image.Type.Sliced;check.pixelsPerUnitMultiplier=8;toggle.graphic=check;
            var label=Ui.Text("Choice value",item.transform,12,0,198,58,"1",29,font);
            var scroll=template.gameObject.AddComponent<ScrollRect>();
            scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;
            scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=42;
            dropdown.template=template.rectTransform;dropdown.itemText=label;
            for(int value=1;value<=99;value++)dropdown.options.Add(new Dropdown.OptionData(value.ToString()));
            dropdown.SetValueWithoutNotify(Mathf.Clamp(state.batchSize,1,99)-1);
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(index=>{
                state.batchSize=index+1;
                if(main){main.autoForgeBatchSize=state.batchSize;main.SaveGame();}
            });
            template.gameObject.SetActive(false);
            return dropdown;
        }
    }
}
