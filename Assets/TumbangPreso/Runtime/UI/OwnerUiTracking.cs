using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Letter-spacing for a single-line label, because legacy `Text` has none.
    ///
    /// ⚠️⚠️ IT EXISTS FOR ONE MEASURED DISAGREEMENT, NOT AS A TYPE FEATURE. Her
    /// tab captions measure 107 units wide for SIGN UP and Paalalabas at the size
    /// that matches her cap height draws 96, so she tracked them out by about two
    /// units a gap. Every other caption on the screen came out inside five units
    /// of hers untouched, which is why nothing else carries this.
    ///
    /// ⚠️ IT MOVES QUADS, IT DOES NOT RE-WRAP. `Text` emits four vertices per
    /// character in reading order, so shifting each quad by its index times the
    /// spacing is exact for one line and wrong for two. Everything it is applied
    /// to is a single centred caption; do not put it on a paragraph.
    ///
    /// ⚠️ THE ROW IS RE-CENTRED BY HALF THE TOTAL so a centred caption stays
    /// centred. Without that, tracking a label out pushes it right by the whole
    /// amount and the caption walks off its own pill.
    /// </summary>
    [RequireComponent(typeof(Text))]
    [DisallowMultipleComponent]
    public sealed class OwnerUiTracking : BaseMeshEffect
    {
        public float Spacing;

        public static void Apply(Text label, float spacing)
        {
            var tracking = label.gameObject.GetComponent<OwnerUiTracking>();
            if (tracking == null) tracking = label.gameObject.AddComponent<OwnerUiTracking>();
            tracking.Spacing = spacing;
            label.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || Mathf.Approximately(Spacing, 0)) return;
            int glyphs = helper.currentVertCount / 4;
            if (glyphs < 2) return;

            var text = GetComponent<Text>();
            float total = Spacing * (glyphs - 1);
            float start = text.alignment == TextAnchor.UpperCenter || text.alignment == TextAnchor.MiddleCenter
                          || text.alignment == TextAnchor.LowerCenter ? -total * .5f
                        : text.alignment == TextAnchor.UpperRight || text.alignment == TextAnchor.MiddleRight
                          || text.alignment == TextAnchor.LowerRight ? -total : 0;

            var vertex = new UIVertex();
            for (int glyph = 0; glyph < glyphs; glyph++)
            {
                float shift = start + Spacing * glyph;
                for (int corner = 0; corner < 4; corner++)
                {
                    int index = glyph * 4 + corner;
                    helper.PopulateUIVertex(ref vertex, index);
                    vertex.position.x += shift;
                    helper.SetUIVertex(vertex, index);
                }
            }
        }
    }
}
