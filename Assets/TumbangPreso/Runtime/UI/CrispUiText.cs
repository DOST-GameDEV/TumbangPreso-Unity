using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    // Keep the legacy Text API and authored font metrics while sampling small glyphs more finely.
    public sealed class CrispUiText : Text
    {
        private readonly UIVertex[] _quad = new UIVertex[4];

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            // InputField reads this generator's vertices for caret/selection coordinates.
            // Keep its native pixel-to-canvas contract, including fields created by shared factories.
            if (font == null || !font.dynamic || resizeTextForBestFit || GetComponentInParent<InputField>() != null)
            {
                base.OnPopulateMesh(mesh);
                return;
            }

            var settings = GetGenerationSettings(rectTransform.rect.size);
            float nativeScale = Mathf.Max(.01f, settings.scaleFactor);
            // Bound atlas cost for large headings. Small labels get up to twice the glyph detail.
            float crispScale = Mathf.Max(nativeScale,
                Mathf.Min(nativeScale * 2f, 160f / Mathf.Max(1, fontSize)));
            m_DisableFontTextureRebuiltCallback = true;
            try
            {
                // ⚠️⚠️ A WRAPPED LABEL MUST BREAK WHERE IT WAS MEASURED, OR IT DRAWS OVER ITS
                // NEIGHBOUR (2026-10-19, character select). Layout reads `preferredHeight` from the
                // NATIVE generation, and a legacy font is generated at a whole pixel size: 28 units
                // at a 0.446 canvas scale is 12.49 px, generated at 12, so the native text measures
                // about 4 per cent narrower than the same words at twice the scale. "ATTACKING ·
                // PROJECTILE" fitted one native line in its 340-unit box and drew as two, and the
                // cooldown under it landed on the ability's sentence: the owner's screenshot of
                // NEMU, reproduced on Dante at 1600x680 in `CharSelect-v1-locked-1600x680.png`.
                // When the two resolutions disagree on the line count, draw at the native one, so
                // the picture is the layout's picture. Agreeing labels keep the finer glyphs.
                if (crispScale > nativeScale && horizontalOverflow == HorizontalWrapMode.Wrap
                    && !string.IsNullOrEmpty(text) && WrapsDifferently(settings, crispScale))
                {
                    base.OnPopulateMesh(mesh);
                    return;
                }
                settings.scaleFactor = crispScale;
                cachedTextGenerator.PopulateWithErrors(text, settings, gameObject);
                var vertices = cachedTextGenerator.verts;
                mesh.Clear();
                if (vertices.Count == 0) return;
                float unitsPerPixel = 1f / settings.scaleFactor;
                Vector2 origin = (Vector2)vertices[0].position * unitsPerPixel;
                Vector2 rounding = PixelAdjustPoint(origin) - origin;
                for (int i = 0; i < vertices.Count; i++)
                {
                    int corner = i & 3;
                    _quad[corner] = vertices[i];
                    _quad[corner].position *= unitsPerPixel;
                    _quad[corner].position += (Vector3)rounding;
                    if (corner == 3) mesh.AddUIVertexQuad(_quad);
                }
            }
            finally { m_DisableFontTextureRebuiltCallback = false; }
        }

        /// <summary>A generator only for comparing line counts; the drawn text never comes from it.</summary>
        private static TextGenerator s_LineProbe;

        /// <summary>True when the native and crisp generations wrap these words onto different
        /// numbers of lines. Called inside the font-rebuild guard, like the population itself.</summary>
        private bool WrapsDifferently(TextGenerationSettings native, float crispScale)
        {
            if (s_LineProbe == null) s_LineProbe = new TextGenerator();
            s_LineProbe.PopulateWithErrors(text, native, gameObject);
            int nativeLines = s_LineProbe.lineCount;
            native.scaleFactor = crispScale;
            s_LineProbe.PopulateWithErrors(text, native, gameObject);
            return s_LineProbe.lineCount != nativeLines;
        }
    }
}
