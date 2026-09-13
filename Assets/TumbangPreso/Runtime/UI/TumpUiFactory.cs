using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>New native view primitives. No dependencies on the retired visual builders.</summary>
    public static class TumpUiFactory
    {
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        public static TumpUiTheme Theme => TumpUiTheme.Current;

        public static RectTransform Rect(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); return rect;
        }

        public static Canvas Canvas(Transform owner, string name, int order = 100)
        {
            var root = Rect(null, name);
            var scene = owner.gameObject.scene;
            if (scene.IsValid()) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root.gameObject, scene);
            root.gameObject.AddComponent<CanvasLifetime>().Bind(owner.gameObject);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true; canvas.sortingOrder = order; canvas.pixelPerfect = true;
            var scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Theme.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            root.gameObject.AddComponent<GraphicRaycaster>();
            InputLayer.UiInputModule.Ensure();
            InputLayer.ScreenFocus.Install(root.gameObject);
            return canvas;
        }

        public static void Stretch(RectTransform rect, float inset = 0)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset); rect.offsetMax = new Vector2(-inset, -inset);
        }
        public static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }
        public static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }

        public static Image Ground(Transform parent, Color color, bool blocks = true)
        {
            var rect = Rect(parent, "Ground"); Stretch(rect);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = blocks; return image;
        }

        public static Text Text(Transform parent, string name, string words, int size = 0, bool bold = false, bool display = false)
        {
            var rect = Rect(parent, name);
            var text = rect.gameObject.AddComponent<Text>();
            text.text = words; text.fontSize = size > 0 ? size : Theme.BodySize;
            text.font = display || bold ? Theme.Display : Theme.Body;
            text.fontStyle = FontStyle.Normal;
            text.color = Theme.DeepOlive;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.lineSpacing = 1.08f;
            return text;
        }

        public static TumpSurface Surface(Transform parent, string name, TumpSurface.Form form, Color fill, bool outline = true)
        {
            var rect = Rect(parent, name);
            var graphic = rect.gameObject.AddComponent<TumpSurface>();
            graphic.Shape = form; graphic.Face = fill; graphic.Outline = outline;
            graphic.raycastTarget = false;
            return graphic;
        }

        public static Button Button(Transform parent, string name, string words, Action action,
            TumpSurface.Form form = TumpSurface.Form.Slap, Color? fill = null, int size = 0)
        {
            var surface = Surface(parent, name, form, fill ?? Theme.Lime);
            surface.raycastTarget = true;
            var button = surface.gameObject.AddComponent<TumpButton>();
            button.targetGraphic = surface; button.transition = Selectable.Transition.None;
            if (action != null) button.onClick.AddListener(() => { MenuSfx.Click(); action(); });
            var text = Text(surface.transform, "Text", words, size > 0 ? size : Theme.HeadingSize, true, true);
            text.alignment = TextAnchor.MiddleCenter;
            if (fill.HasValue && fill.Value == Theme.Brick) text.color = Theme.Cream;
            Stretch(text.rectTransform, 16);
            return button;
        }

        public static Sprite Sprite(string resource)
        {
            if (Sprites.TryGetValue(resource, out var known) && known != null) return known;
            var sprite = Resources.Load<Sprite>(resource);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(resource);
                if (texture == null) return null;
                sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
            }
            Sprites[resource] = sprite;
            return sprite;
        }

        public static Image Art(Transform parent, string name, Sprite sprite)
        {
            var image = Rect(parent, name).gameObject.AddComponent<Image>();
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
            image.enabled = sprite != null; return image;
        }

        public static Button Portrait(Transform parent, string id, string name, Action select, bool selected)
        {
            var button = Button(parent, "Portrait_" + id, "", select, TumpSurface.Form.Portrait, Theme.Apricot);
            button.GetComponent<TumpSurface>().Selected = selected;
            var art = Art(button.transform, "Portrait", Sprite("UI/portraits/" + id));
            Stretch(art.rectTransform, 8); art.rectTransform.offsetMin = new Vector2(8, 70);
            var label = button.GetComponentInChildren<Text>();
            label.text = name; label.font = Theme.Display; label.fontSize = 28;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = new Vector2(1, 0);
            label.rectTransform.pivot = new Vector2(.5f, 0);
            label.rectTransform.offsetMin = new Vector2(8, 6); label.rectTransform.offsetMax = new Vector2(-8, 68);
            return button;
        }

        public static RectTransform Column(Transform parent, string name, float gap = 0)
        {
            var rect = Rect(parent, name);
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            layout.spacing = gap > 0 ? gap : Theme.Gap;
            return rect;
        }
        public static void Height(Component item, float height, float width = -1)
        {
            var element = item.GetComponent<LayoutElement>() ?? item.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height;
            element.flexibleHeight = 0;
            if (width > 0) element.minWidth = element.preferredWidth = width;
        }

        public static RectTransform Scroll(Transform parent, string name, out ScrollRect scroll)
        {
            var root = Rect(parent, name);
            scroll = root.gameObject.AddComponent<ScrollRect>();
            var viewport = Rect(root, "Viewport"); Stretch(viewport);
            var image = viewport.gameObject.AddComponent<Image>(); image.color = Color.white;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = Column(viewport, "Content");
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 48;
            return content;
        }

        public static InputField Field(Transform parent, string name, string hint, string value = "", bool password = false)
        {
            var surface = Surface(parent, name, TumpSurface.Form.Ticket, Theme.Cream);
            surface.raycastTarget = true;
            var field = surface.gameObject.AddComponent<InputField>();
            field.targetGraphic = surface; field.characterLimit = 80;
            field.contentType = password ? InputField.ContentType.Password : InputField.ContentType.Standard;
            var text = Text(surface.transform, "Value", value); Stretch(text.rectTransform, 20);
            var placeholder = Text(surface.transform, "Hint", hint); Stretch(placeholder.rectTransform, 20);
            placeholder.color = Theme.Olive;
            field.textComponent = text; field.placeholder = placeholder;
            field.customCaretColor = true; field.caretColor = Theme.Brick;
            field.selectionColor = new Color(Theme.Yellow.r, Theme.Yellow.g, Theme.Yellow.b, .7f);
            field.SetTextWithoutNotify(value); return field;
        }
    }
}
