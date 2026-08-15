using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// A real outline for a legacy Text, in eight directions.
    ///
    /// ⚠️ UGUI'S BUILT-IN `Outline` DRAWS FOUR DIAGONAL COPIES AND THAT IS NOT WHAT GODOT DOES.
    /// Godot's `outline_size` is a radius: the glyph is grown in every direction. Four corner
    /// copies leave the top, bottom and sides of a stroke bare, which at the 5 and 6 px this
    /// game uses reads as a drop shadow smeared around a letter rather than as an outline. Eight
    /// directions at the same radius is close enough to be indistinguishable at menu sizes.
    ///
    /// ⚠️ AND THE OUTLINE IS NOT DECORATION HERE. Every menu label sits over a photograph of a
    /// street. Cream lettering with no dark edge is illegible over the bright half of it, which
    /// is most of the frame in the daytime backdrop.
    /// </summary>
    [AddComponentMenu("UI/Effects/Godot Outline")]
    public sealed class GodotOutline : BaseMeshEffect
    {
        public Color OutlineColour = Color.black;
        public float Radius = 3.0f;

        private static readonly Vector2[] Directions =
        {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
            new Vector2(0.7071f, 0.7071f), new Vector2(-0.7071f, 0.7071f),
            new Vector2(0.7071f, -0.7071f), new Vector2(-0.7071f, -0.7071f),
        };

        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || Radius <= 0.0f) return;

            var verts = new List<UIVertex>();
            helper.GetUIVertexStream(verts);

            int original = verts.Count;
            var output = new List<UIVertex>(original * (Directions.Length + 1));

            foreach (var dir in Directions)
            {
                for (int i = 0; i < original; i++)
                {
                    var v = verts[i];
                    v.position.x += dir.x * Radius;
                    v.position.y += dir.y * Radius;

                    // ⚠️ THE COPY TAKES THE OUTLINE'S ALPHA SCALED BY THE GLYPH'S, so a label
                    // faded out by a tween fades its outline with it rather than leaving a ring
                    // of ink floating on the screen.
                    var c = OutlineColour;
                    c.a *= v.color.a / 255.0f;
                    v.color = c;

                    output.Add(v);
                }
            }

            output.AddRange(verts);

            helper.Clear();
            helper.AddUIVertexTriangleStream(output);
        }
    }
}
