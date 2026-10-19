using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// Lettering that floats: four soft drops under a line of text, each twice as far down as
    /// the last and fainter, the text twin of <see cref="HubShape.Depth"/>.
    ///
    /// ⚠️⚠️ ONE EFFECT AND NOT FOUR `Shadow` COMPONENTS, owner 2026-10-17: *"increase depth for
    /// everything such as using 4 shadows"*. Unity's `Shadow` copies EVERY vertex the effects
    /// before it produced, so four stacked components shadow each other's shadows: sixteen copies
    /// of every glyph and a muddy halo darker than any one layer was set to. This copies the
    /// lettering four times, once per layer, from the original only.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class HubDepthText : BaseMeshEffect
    {
        /// <summary>How far the deepest layer drops, in canvas units.</summary>
        public float Depth = 10.0f;
        public Color Colour = HubStyle.Ink;

        /// <summary>Contact first. Stronger than `HubShape`'s stack because a glyph's stroke is a
        /// few units wide and a faint layer under it simply vanishes.</summary>
        private static readonly float[] Alpha = { 0.60f, 0.36f, 0.20f, 0.10f };

        private static readonly List<UIVertex> Source = new List<UIVertex>();
        private static readonly List<UIVertex> Output = new List<UIVertex>();

        public static HubDepthText On(Graphic graphic, float depth)
        {
            if (graphic == null) return null;
            var effect = HubKit.Ensure<HubDepthText>(graphic.gameObject);
            effect.Depth = depth;
            graphic.SetVerticesDirty();
            return effect;
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || Depth <= 0.0f) return;
            Source.Clear();
            vh.GetUIVertexStream(Source);
            if (Source.Count == 0) return;

            Output.Clear();
            int layers = HubShape.DepthLayers;
            for (int layer = layers - 1; layer >= 0; layer--)
            {
                float reach = Depth * (1 << layer) / (1 << (layers - 1));
                var offset = new Vector3(reach * 0.15f, -reach, 0.0f);
                foreach (var original in Source)
                {
                    var v = original;
                    v.position += offset;
                    Color c = Colour;
                    c.a *= Alpha[layer] * (original.color.a / 255.0f);
                    v.color = c;
                    Output.Add(v);
                }
            }
            Output.AddRange(Source);

            vh.Clear();
            vh.AddUIVertexTriangleStream(Output);
        }
    }
}
