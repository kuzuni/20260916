using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Moonlit.UI
{
    /// <summary>Shared reusable popup artwork. Labels, icons and hit targets remain independent.</summary>
    public static class PopupSkin
    {
        static Sprite panel, action, close, crest, crimson, gold, ribbon, parchmentRibbon;
        static Sprite[] switchParts;
        static Sprite checkbox;
        public static Sprite CheckboxArt
        {
            get
            {
                if (checkbox) return checkbox;
                var texture=Resources.Load<Texture2D>("Moonlit/Forge/CheckboxFrame-v1");
                if (!texture) return null;
                checkbox=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),
                    new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
                checkbox.name="CheckboxFrame-v1";
                return checkbox;
            }
        }
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
        public static Sprite CrimsonActionArt => Load(ref crimson, "CrimsonAction-v1", new Vector4(240, 150, 240, 150),
            new Rect(48, 116, 2076, 504), new Vector2(2172, 724));
        public static Sprite GoldActionArt => Load(ref gold, "GoldAction-v1", new Vector4(140, 70, 140, 70),
            new Rect(12, 157, 2066, 486), new Vector2(2089, 753));
        public static Sprite RibbonArt => Load(ref ribbon, "EquippedRibbon-v1", Vector4.zero,
            new Rect(96, 235, 1752, 350), new Vector2(1942, 809));
        public static Sprite ParchmentRibbonArt => Load(ref parchmentRibbon, "EquippedParchment-v1", Vector4.zero,
            new Rect(96, 235, 1752, 350), new Vector2(1942, 809));
        public static Sprite CloseArt => Load(ref close, "CrimsonClose-v1", Vector4.zero);
        public static Sprite CrestArt => Load(ref crest, "PanelCrest-v1", Vector4.zero);

        static Sprite passHeader;
        static Sprite[] rewardIcons;
        public static Sprite PassHeaderArt => Load(ref passHeader, "PassHeader-v1", Vector4.zero);
        public static Sprite RewardIcon(int index)
        {
            if (rewardIcons == null || !rewardIcons[0])
            {
                var atlas = Resources.Load<Texture2D>("Moonlit/Popup/RewardIcons-v1");
                if (!atlas) return null;
                rewardIcons = new Sprite[8];
                float w=atlas.width/4f, h=atlas.height/2f;
                for(int i=0;i<8;i++) rewardIcons[i]=Sprite.Create(atlas,
                    new Rect(i%4*w,(1-i/4)*h,w,h),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            }
            return rewardIcons[Mathf.Clamp(index,0,7)];
        }

        // Art fills the card; its reusable empty rim is independent of rewards and controls.
        public static RectTransform IllustratedCard(string name, Transform parent, float x, float y,
            float width, float height, Sprite painting, Color tint)
        {
            var root=Ui.Rect(name,parent,x,y,width,height);
            Ui.Image("Card backing",root,4,4,width-8,height-8,null,new Color(.015f,.025f,.035f));
            if(painting)
            {
                var crop=Ui.Rect("Card painting crop",root,6,6,width-12,height-12);
                crop.gameObject.AddComponent<RectMask2D>();
                float scale=Mathf.Max((width-12)/painting.rect.width,(height-12)/painting.rect.height);
                float w=painting.rect.width*scale,h=painting.rect.height*scale;
                Ui.Image("Card painting",crop,(width-12-w)/2,(height-12-h)/2,w,h,painting,tint);
            }
            var rim=Ui.Image("Card rim",root,0,0,width,height,PanelArt);
            rim.type=Image.Type.Sliced; rim.fillCenter=false; rim.pixelsPerUnitMultiplier=6;
            return root;
        }

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

        static Sprite SwitchArt(int index)
        {
            if (switchParts == null || !switchParts[0])
            {
                var atlas = Resources.Load<Texture2D>("Moonlit/Social/SettingsSwitch-v1");
                if (!atlas) return null;
                var regions = new[] { new Rect(96,122,830,286), new Rect(1015,122,830,286), new Rect(834,462,276,276) };
                float sx = atlas.width / 1942f, sy = atlas.height / 809f;
                switchParts = new Sprite[3];
                for (int i = 0; i < switchParts.Length; i++)
                {
                    var r = regions[i];
                    switchParts[i] = Sprite.Create(atlas,
                        new Rect(r.x * sx, atlas.height - (r.y + r.height) * sy, r.width * sx, r.height * sy),
                        new Vector2(.5f,.5f), 100 * sx, 0, SpriteMeshType.FullRect,
                        i < 2 ? new Vector4(142 * sx, 0, 142 * sx, 0) : Vector4.zero);
                    switchParts[i].name = i == 0 ? "Switch off track" : i == 1 ? "Switch on track" : "Switch thumb";
                }
            }
            return switchParts[index];
        }

        public static Toggle Switch(Transform parent, float x, float y, string name, bool value, UnityAction<bool> changed)
        {
            var bg = Ui.Image(name + " toggle", parent, x, y, 128, 54, null, Color.clear);
            bg.raycastTarget = true;
            var track = Ui.Image("Track", bg.transform, 0, 0, 128, 54, SwitchArt(value ? 1 : 0));
            track.type = Image.Type.Sliced; track.pixelsPerUnitMultiplier = 5.26f;
            var thumb = Ui.Image("Thumb", bg.transform, 6, 6, 42, 42, SwitchArt(2));
            thumb.preserveAspect = true;
            var toggle = bg.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = track;
            // Toggle.graphic fades to zero when off; the movable thumb must remain visible.
            toggle.graphic = null; toggle.transition = Selectable.Transition.None;
            toggle.SetIsOnWithoutNotify(value);
            void Paint(bool on) {
                thumb.rectTransform.anchoredPosition = new Vector2(on ? 80 : 6, -6);
                track.sprite = SwitchArt(on ? 1 : 0);
            }
            Paint(value);
            toggle.onValueChanged.AddListener(on => { Paint(on); changed?.Invoke(on); });
            return toggle;
        }

        public static Button Button(string name, Transform parent, float x, float y, float width, float height,
            string label, Font font, UnityAction click = null, Color? color = null, int size = 28)
        {
            if (label == "×") return Close(name, parent, x, y, Mathf.Min(width, height), font, click, size);
            Color tint = color ?? new Color(.02f, .23f, .48f);
            bool blue = tint.b - tint.r > .12f && tint.b - tint.g > .08f;
            bool red = !blue && tint.r > tint.g * 1.5f && tint.r > tint.b * 1.5f;
            var button = Ui.ArtButton(name, parent, x, y, width, height,
                blue ? ActionArt : red ? CrimsonActionArt : PanelArt, true, blue || red ? 8 : 7);
            if (!blue && !red && tint.g > tint.b * 1.5f) button.targetGraphic.color = new Color(.6f, 1, .65f);
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

        public static Button Back(string name, Transform parent, float x, float y, float size,
            Font font, UnityAction click, bool square = false)
        {
            var button = Ui.ArtButton(name, parent, x, y, size, size, square ? PanelArt : CloseArt, square, 5);
            Ui.Text("Back arrow", button.transform, 0, -2, size, size, "◀", Mathf.RoundToInt(size * .43f), font,
                square ? new Color(.96f,.12f,.08f) : Ui.Ivory);
            if (click != null) button.onClick.AddListener(click);
            return button;
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
