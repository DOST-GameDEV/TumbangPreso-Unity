using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The plates' woven grey for a panel of any size (owner, 2026-11-08: the training panel's weave was stretched and
    /// too see-through). The plate picture draws the stroke and slab; this lays the weave inside it at its own scale,
    /// cropped to the panel rather than stretched, clipped to the plate's rounded inside, as see-through as the F1 plates
    /// (the picture carries their weave over grey; it is drawn at 82 per cent, as they are).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class KitPanelWeave : MonoBehaviour
    {
        private RawImage _weave;

        public static KitPanelWeave On(RectTransform panel, float alpha = .9f)
        {
            var clip = Hub.HubKit.Stretch(OwnerUiLayout.Rect(panel, "PanelWeaveClip"), 3);
            clip.SetAsFirstSibling();
            clip.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var shape = clip.gameObject.AddComponent<Image>();
            shape.sprite = Hub.HubKnit.Knitted(LiveHud.Folder + "ref-inner", new Vector4(14, 14, 14, 14));
            shape.type = Image.Type.Sliced; shape.raycastTarget = false;
            clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var weave = Hub.HubKit.Stretch(OwnerUiLayout.Rect(clip, "PanelWeave")).gameObject.AddComponent<RawImage>();
            weave.texture = Resources.Load<Texture2D>(LiveHud.Folder + "panel-weave");
            weave.raycastTarget = false; weave.color = new Color(1, 1, 1, alpha);
            var fit = clip.gameObject.AddComponent<KitPanelWeave>();
            fit._weave = weave; fit.Fit();
            return fit;
        }

        /// <summary>
        /// The F1 plates' dark grey box, for a band of the panel (`top` true from the top edge, else from the bottom):
        /// nearly solid, inside the plate's stroke, drawn over the weave and under the words.
        /// </summary>
        public static Image Box(RectTransform panel, string name, bool top, float height)
        {
            var box = OwnerUiLayout.Rect(panel, name).gameObject.AddComponent<Image>();
            box.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            // The F1 header box itself, as a band: 75 per cent, its inner shadow, square inside, rounded only at the plate's corners.
            box.sprite = Hub.HubKnit.Knitted(LiveHud.Folder + (top ? "band-top-690x" : "band-bottom-690x") + height, Vector4.zero);
            box.raycastTarget = false;
            var r = box.rectTransform;
            r.anchorMin = new Vector2(0, top ? 1 : 0); r.anchorMax = new Vector2(1, top ? 1 : 0); r.pivot = new Vector2(.5f, top ? 1 : 0);
            r.offsetMin = new Vector2(0, top ? -height : 0); r.offsetMax = new Vector2(0, top ? 0 : height);
            box.transform.SetSiblingIndex(1);
            return box;
        }

        private void OnRectTransformDimensionsChange() => Fit();

        private void Fit()
        {
            if (_weave == null || _weave.texture == null) return;
            var size = ((RectTransform)transform).rect.size;
            float w = Mathf.Min(1, size.x / _weave.texture.width), h = Mathf.Min(1, size.y / _weave.texture.height);
            _weave.uvRect = new Rect(0, 1 - h, w, h);
        }
    }
}
