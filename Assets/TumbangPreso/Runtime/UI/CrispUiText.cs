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
            settings.scaleFactor = Mathf.Max(nativeScale,
                Mathf.Min(nativeScale * 2f, 160f / Mathf.Max(1, fontSize)));
            m_DisableFontTextureRebuiltCallback = true;
            try
            {
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
    }
}
