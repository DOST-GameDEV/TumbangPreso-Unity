using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Builds the two decoration layers a Godot StyleBox needs, in the only order UGUI allows.
    ///
    /// ⚠️⚠️ A CHILD CAN NEVER DRAW BEHIND ITS PARENT'S OWN GRAPHIC, AND THAT COST A WHOLE PASS.
    /// `wood_style` draws its cartoon drop shadow OUTSIDE the box, six pixels grown and five
    /// down, which no nine-slice can express without moving the box's own edges. The obvious
    /// port is a shadow child added behind with `SetAsFirstSibling`, and it does not work: in
    /// UGUI a parent's Image is rendered BEFORE all of its children, so the shadow landed on top
    /// of the face. Every wood panel came out as a flat translucent-navy rectangle and every
    /// wood button read purple with a grey-blue edge — the shadow's own colour, over the face
    /// it was supposed to sit under.
    ///
    /// So the object that owns the control keeps a transparent Image for raycasting only, and
    /// the visuals are two `ignoreLayout` children: Shadow first, Face second. Anything else the
    /// control carries (a Button's label) is added after both and draws on top of them.
    /// </summary>
    internal static class SkinLayers
    {
        public static Image Face(Transform owner) => Layer(owner, "Face", 1);

        public static Image Shadow(Transform owner)
        {
            var image = Layer(owner, "Shadow", 0);

            image.sprite = GodotTheme.ShadowBox();
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.pixelsPerUnitMultiplier = 1.0f;

            const float grow = GodotTheme.WoodShadowSize;
            const float drop = GodotTheme.WoodShadowOffsetY;

            var rt = image.rectTransform;
            rt.offsetMin = new Vector2(-grow, -grow - drop);
            rt.offsetMax = new Vector2(grow, grow - drop);

            return image;
        }

        private static Image Layer(Transform owner, string name, int index)
        {
            var existing = owner.Find(name);
            var image = existing != null ? existing.GetComponent<Image>() : null;

            if (image == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(owner, false);
                image = go.AddComponent<Image>();
            }

            image.raycastTarget = false;

            // ⚠️ IT MUST OPT OUT OF THE LAYOUT. A PanelContainer carries a layout group for its
            // content margins, and without this the decoration is treated as content and pushes
            // the real children sideways.
            var element = image.GetComponent<LayoutElement>();
            if (element == null) element = image.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = true;

            var rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            image.transform.SetSiblingIndex(index);
            return image;
        }

        /// <summary>The control's own Image: invisible, but still the raycast target.</summary>
        public static void MakeHitArea(Image image)
        {
            if (image == null) return;

            image.sprite = null;
            image.color = new Color(0, 0, 0, 0);
        }
    }
}
