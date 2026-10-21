using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// Chalk on asphalt: scattered hand marks at low contrast, the "TEXTURED BG" of the sketches.
    ///
    /// ⚠️ `Front_End_Design.md` § 1.3: decoration is free where nothing has to be read. Every mark
    /// here is under 1.5:1 against its ground (Honey at 7 per cent over Night is about 1.3:1), so
    /// it can never compete with a label, and it is drawn only on grounds, never on a control.
    /// The marks are the game's own: tsinelas outlines, the box's corners, tally strokes and X's
    /// the way kids chalk a court.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HubPattern : MaskableGraphic
    {
        public int Seed = 3;
        public float Density = 1.0f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var rng = new System.Random(Seed);
            int count = Mathf.RoundToInt(r.width * r.height / 42000f * Density);
            Color32 c = color;
            for (int i = 0; i < count; i++)
            {
                var at = new Vector2(r.xMin + (float)rng.NextDouble() * r.width, r.yMin + (float)rng.NextDouble() * r.height);
                float size = 26 + (float)rng.NextDouble() * 40;
                float angle = (float)rng.NextDouble() * Mathf.PI * 2;
                switch (rng.Next(4))
                {
                    case 0: // an X
                        Stroke(vh, at, size, angle, c); Stroke(vh, at, size, angle + Mathf.PI / 2, c); break;
                    case 1: // tally marks
                        for (int k = 0; k < 4; k++) Stroke(vh, at + Rot(new Vector2(k * 12 - 18, 0), angle), size * 0.8f, angle + Mathf.PI / 2, c);
                        Stroke(vh, at, size * 1.3f, angle + 0.5f, c); break;
                    case 2: // a box corner
                        Line(vh, at, at + Rot(new Vector2(size, 0), angle), c); Line(vh, at, at + Rot(new Vector2(0, size), angle), c); break;
                    default: // a tsinelas outline: two arcs and a strap
                        Oval(vh, at, size * 0.36f, size * 0.7f, angle, c);
                        Line(vh, at + Rot(new Vector2(-size * 0.18f, size * 0.3f), angle), at + Rot(new Vector2(0, size * 0.05f), angle), c);
                        Line(vh, at + Rot(new Vector2(size * 0.18f, size * 0.3f), angle), at + Rot(new Vector2(0, size * 0.05f), angle), c);
                        break;
                }
            }
        }

        private static Vector2 Rot(Vector2 v, float a) =>
            new Vector2(v.x * Mathf.Cos(a) - v.y * Mathf.Sin(a), v.x * Mathf.Sin(a) + v.y * Mathf.Cos(a));

        private static void Stroke(VertexHelper vh, Vector2 at, float length, float angle, Color32 c)
        {
            var half = Rot(new Vector2(length * 0.5f, 0), angle);
            Line(vh, at - half, at + half, c);
        }

        private static void Oval(VertexHelper vh, Vector2 at, float rx, float ry, float angle, Color32 c)
        {
            const int steps = 14;
            for (int s = 0; s < steps; s++)
            {
                float a = s * Mathf.PI * 2 / steps, b = (s + 1) * Mathf.PI * 2 / steps;
                Line(vh, at + Rot(new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry), angle),
                         at + Rot(new Vector2(Mathf.Cos(b) * rx, Mathf.Sin(b) * ry), angle), c);
            }
        }

        private static void Line(VertexHelper vh, Vector2 a, Vector2 b, Color32 c)
        {
            var d = b - a;
            if (d.sqrMagnitude < 0.01f) return;
            var n = new Vector2(-d.y, d.x).normalized * 2.6f;
            int i = vh.currentVertCount;
            vh.AddVert(a + n, c, Vector2.zero); vh.AddVert(b + n, c, Vector2.zero);
            vh.AddVert(b - n, c, Vector2.zero); vh.AddVert(a - n, c, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        /// <summary>A textured ground: a flat colour with chalk over it, stretched to <paramref name="root"/>.</summary>
        public static void Ground(RectTransform root, Color ground, int seed)
        {
            var fill = HubKit.Stretch(HubKit.Rect(root, "Ground")).gameObject.AddComponent<Image>();
            fill.color = ground;
            fill.raycastTarget = false;
            var chalk = HubKit.Stretch(HubKit.Rect(root, "Chalk")).gameObject.AddComponent<HubPattern>();
            chalk.Seed = seed;
            chalk.color = new Color(HubStyle.Honey.r, HubStyle.Honey.g, HubStyle.Honey.b, 0.07f);
            chalk.raycastTarget = false;
        }
    }
}
