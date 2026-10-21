using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Letter-spacing for legacy `Text`. Wrapped labels opt into per-line centring
    /// and must reserve the added width before the text generator wraps.
    ///
    /// Originally added for one measured disagreement: the sign-in
    /// tab captions measure 107 units wide for SIGN UP and Paalalabas at the size
    /// that matches her cap height draws 96, so she tracked them out by about two
    /// units a gap. Every other caption on the screen came out inside five units
    /// of hers untouched. Warning banners now use subtle tracking at the owner's request.
    ///
    /// It moves quads, it does not re-wrap. Warning banners reserve the added
    /// width in their layout and use PerLine so wrapped reasons stay centred.
    /// Existing sign-in captions keep their original single-line treatment.
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
        public bool PerLine;

        public static void Apply(Text label, float spacing, bool perLine = false)
        {
            var tracking = label.gameObject.GetComponent<OwnerUiTracking>();
            if (tracking == null) tracking = label.gameObject.AddComponent<OwnerUiTracking>();
            tracking.Spacing = spacing;
            tracking.PerLine = perLine;
            label.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || Mathf.Approximately(Spacing, 0)) return;
            int glyphs = helper.currentVertCount / 4;
            if (glyphs < 2) return;

            var text = GetComponent<Text>();
            if (PerLine)
            {
                // CrispUiText preserves native wrapping. Line starts therefore address
                // these same character quads, without counting the generator's sentinel.
                glyphs = Mathf.Min(glyphs, text.text.Length);
                var lines = text.cachedTextGenerator.lines;
                for (int line = 0; line < lines.Count; line++)
                {
                    int first = lines[line].startCharIdx;
                    int end = line + 1 < lines.Count ? Mathf.Min(glyphs, lines[line + 1].startCharIdx) : glyphs;
                    while (end > first && char.IsWhiteSpace(text.text[end - 1])) end--;
                    TrackLine(helper, text, first, end);
                }
                return;
            }
            TrackLine(helper, text, 0, glyphs);
        }

        private void TrackLine(VertexHelper helper, Text text, int first, int end)
        {
            if (end - first < 2) return;
            float total = Spacing * (end - first - 1);
            float start = text.alignment == TextAnchor.UpperCenter || text.alignment == TextAnchor.MiddleCenter
                          || text.alignment == TextAnchor.LowerCenter ? -total * .5f
                        : text.alignment == TextAnchor.UpperRight || text.alignment == TextAnchor.MiddleRight
                          || text.alignment == TextAnchor.LowerRight ? -total : 0;

            var vertex = new UIVertex();
            for (int glyph = first; glyph < end; glyph++)
            {
                float shift = start + Spacing * (glyph - first);
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
