using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Moonlit.UI
{
    public static class Ui
    {
        public static readonly Color Gold = new Color(.79f,.62f,.36f);
        public static readonly Color Ivory = new Color(.96f,.93f,.84f);
        public static readonly Color Cyan = new Color(.2f,.88f,1f);
        public static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = new Vector2(0,1);
            r.pivot = new Vector2(0,1);
            r.anchoredPosition = new Vector2(x,-y);
            r.sizeDelta = new Vector2(w,h);
            return r;
        }
        public static Image Image(string name, Transform parent, float x, float y, float w, float h, Sprite sprite, Color? color = null)
        {
            var i = Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>();
            i.sprite = sprite; i.color = color ?? Color.white; i.raycastTarget = false;
            return i;
        }
        public static Text Text(string name, Transform parent, float x, float y, float w, float h, string value, int size, Font font, Color? color = null, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = Rect(name,parent,x,y,w,h).gameObject.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.fontStyle = FontStyle.Bold;
            t.text = value; t.color = color ?? Ivory; t.alignment = align;
            t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var o = t.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0,0,0,.9f); o.effectDistance = new Vector2(2,-2);
            return t;
        }
        public static Image Panel(string name, Transform parent, float x, float y, float w, float h, Color color)
        {
            var image = Image(name,parent,x,y,w,h,null,color);
            Border(image.transform,w,h,Gold,2);
            return image;
        }
        public static void Border(Transform parent, float w, float h, Color color, float thickness = 2)
        {
            Image("Top edge",parent,0,0,w,thickness,null,color);
            Image("Bottom edge",parent,0,h-thickness,w,thickness,null,color);
            Image("Left edge",parent,0,0,thickness,h,null,color);
            Image("Right edge",parent,w-thickness,0,thickness,h,null,color);
        }
        public static Button Button(string name, Transform parent, float x, float y, float w, float h, string value, Font font, UnityAction click = null, Color? color = null, int size = 28)
        {
            var bg = Panel(name,parent,x,y,w,h,color ?? new Color(.035f,.19f,.31f));
            bg.raycastTarget = true;
            var button = bg.gameObject.AddComponent<Button>(); button.targetGraphic = bg;
            var colors = button.colors; colors.highlightedColor = new Color(1.25f,1.25f,1.25f); colors.pressedColor = new Color(.65f,.85f,1f); button.colors = colors;
            if (!string.IsNullOrEmpty(value)) Text("Label",bg.transform,6,2,w-12,h-4,value,size,font);
            if (click != null) button.onClick.AddListener(click);
            return button;
        }
        public static Button ArtButton(string name, Transform parent, float x, float y, float w, float h, Sprite art = null, bool sliced = false, float pixelsPerUnit = 5)
        {
            var hit = Image(name,parent,x,y,w,h,null,Color.clear);
            hit.raycastTarget=true;
            var button=hit.gameObject.AddComponent<Button>();
            if(art) {
                var visual=Image("Artwork",hit.transform,0,0,w,h,art);
                if(sliced) { visual.type=UnityEngine.UI.Image.Type.Sliced; visual.pixelsPerUnitMultiplier=pixelsPerUnit; }
                else visual.preserveAspect=true;
                button.targetGraphic=visual;
                var feedback=hit.gameObject.AddComponent<ButtonFeedback>(); feedback.artwork=visual.rectTransform;
            } else { button.targetGraphic=hit; button.transition=Selectable.Transition.None; }
            var colors=button.colors; colors.highlightedColor=new Color(1.12f,1.12f,1.12f); colors.pressedColor=new Color(.8f,.88f,.94f); button.colors=colors;
            return button;
        }
        public static void Stretch(RectTransform r, float inset = 0)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.one*inset; r.offsetMax = -Vector2.one*inset;
        }
    }
}
