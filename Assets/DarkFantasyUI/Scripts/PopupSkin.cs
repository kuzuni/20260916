using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Moonlit.UI
{
    /// <summary>Shared reusable popup artwork. Labels, icons and hit targets remain independent.</summary>
    public static class PopupSkin
    {
        static Sprite panel, action, close, crest;
        static Sprite Load(ref Sprite cached, string name, Vector4 border, Rect? sourceRect = null, Vector2? sourceSize = null)
        {
            if (cached) return cached;
            var texture = Resources.Load<Texture2D>("Moonlit/Popup/" + name);
            if (!texture) return null;
            // Importers/platform limits may downscale the PNG. Keep crop coordinates, slice
            // borders and pixel density in the same imported space before creating the sprite.
            var designSize = sourceSize ?? new Vector2(texture.width, texture.height);
            float sx = texture.width / designSize.x, sy = texture.height / designSize.y;
            var rect = sourceRect ?? new Rect(0, 0, designSize.x, designSize.y);
            rect = new Rect(rect.x * sx, rect.y * sy, rect.width * sx, rect.height * sy);
            var scaledBorder = new Vector4(border.x * sx, border.y * sy, border.z * sx, border.w * sy);
            cached = Sprite.Create(texture, rect,
                new Vector2(.5f, .5f), 100 * sx, 0, SpriteMeshType.FullRect, scaledBorder);
            cached.name = name;
            return cached;
        }
        public static Sprite PanelArt => Load(ref panel, "OrnatePanel-v1", new Vector4(220, 220, 220, 220));
        // V2 keeps ornaments in the fixed corner slices. Exclude the export's transparent
        // padding so the visible face fills the live button and does not shrink behind its label.
        public static Sprite ActionArt => Load(ref action, "BlueAction-v2", new Vector4(240, 150, 240, 150),
            new Rect(48, 120, 2076, 488), new Vector2(2172, 724));
        public static Sprite CloseArt => Load(ref close, "CrimsonClose-v1", Vector4.zero);
        public static Sprite CrestArt => Load(ref crest, "PanelCrest-v1", Vector4.zero);

        public static Image Panel(string name, Transform parent, float x, float y, float width, float height)
        {
            // Generated stone has slightly translucent interior pixels. An opaque inset
            // prevents equipment labels from ghosting through the modal, while preserving
            // the illustrated frame's transparent outer corners and independent hit target.
            if (height > 200)
                Ui.Image(name + " opaque backing", parent, x + 20, y + 20,
                    Mathf.Max(1, width - 40), Mathf.Max(1, height - 40), null,
                    new Color(.02f, .03f, .04f, 1)).raycastTarget = false;
            var image = Ui.Image(name, parent, x, y, width, height, PanelArt);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = height > 200 ? 3 : 7;
            image.raycastTarget = true;
            if (height > 200)
            {
                var ornament = Ui.Image("Crown filigree", image.transform, width * .5f - 90, -34, 180, 72, CrestArt);
                ornament.preserveAspect = true;
                ornament.raycastTarget = false;
            }
            return image;
        }

        public static Button Button(string name, Transform parent, float x, float y, float width, float height,
            string label, Font font, UnityAction click = null, Color? color = null, int size = 28)
        {
            if (label == "×") return Close(name, parent, x, y, Mathf.Min(width, height), font, click, size);
            Color tint = color ?? new Color(.02f, .23f, .48f);
            bool blue = tint.b - tint.r > .12f && tint.b - tint.g > .08f;
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
