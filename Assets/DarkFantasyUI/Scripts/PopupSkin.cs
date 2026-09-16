using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Moonlit.UI
{
    /// <summary>Shared reusable popup artwork. Labels, icons and hit targets remain independent.</summary>
    public static class PopupSkin
    {
        static Sprite panel, action, close;
        static Sprite Load(ref Sprite cached, string name, Vector4 border)
        {
            if (cached) return cached;
            var texture = Resources.Load<Texture2D>("Moonlit/Popup/" + name);
            if (!texture) return null;
            cached = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, border);
            cached.name = name;
            return cached;
        }
        public static Sprite PanelArt => Load(ref panel, "OrnatePanel-v1", new Vector4(220, 220, 220, 220));
        public static Sprite ActionArt => Load(ref action, "BlueAction-v1", new Vector4(300, 210, 300, 210));
        public static Sprite CloseArt => Load(ref close, "CrimsonClose-v1", Vector4.zero);

        public static Image Panel(string name, Transform parent, float x, float y, float width, float height)
        {
            var image = Ui.Image(name, parent, x, y, width, height, PanelArt);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = height > 200 ? 3 : 7;
            image.raycastTarget = true;
            if (height > 200)
            {
                var ornament = Ui.Rect("Crown filigree", image.transform, width * .5f - 42, -16, 84, 50)
                    .gameObject.AddComponent<PanelOrnament>();
                ornament.color = Ui.Gold; ornament.raycastTarget = false;
            }
            return image;
        }

        public static Button Button(string name, Transform parent, float x, float y, float width, float height,
            string label, Font font, UnityAction click = null, Color? color = null, int size = 28)
        {
            if (label == "×") return Close(name, parent, x, y, Mathf.Min(width, height), font, click, size);
            Color tint = color ?? new Color(.02f, .23f, .48f);
            bool blue = tint.b > tint.r * 1.3f && tint.b > tint.g * 1.1f;
            var button = Ui.ArtButton(name, parent, x, y, width, height, blue ? ActionArt : PanelArt, true, blue ? 8 : 7);
            if (!blue && tint.r > tint.b * 1.5f) button.targetGraphic.color = new Color(1, .55f, .5f);
            else if (!blue && tint.g > tint.b * 1.5f) button.targetGraphic.color = new Color(.6f, 1, .65f);
            Ui.Text("Label", button.transform, 16, 2, width - 32, height - 4, label, size, font);
            if (click != null) button.onClick.AddListener(click);
            return button;
        }

        public static void Select(Button button, bool selected)
        {
            var art = button.targetGraphic as Image;
            if (!art) return;
            art.sprite = selected ? ActionArt : PanelArt;
            art.type = Image.Type.Sliced;
            art.pixelsPerUnitMultiplier = selected ? 8 : 7;
            art.color = Color.white;
        }

        public static Button Close(string name, Transform parent, float x, float y, float size,
            Font font, UnityAction click, int fontSize = 52)
        {
            var button = Ui.ArtButton(name, parent, x, y, size, size, CloseArt);
            Ui.Text("Label", button.transform, 0, -1, size, size, "×", fontSize, font);
            if (click != null) button.onClick.AddListener(click);
            return button;
        }
    }
}
