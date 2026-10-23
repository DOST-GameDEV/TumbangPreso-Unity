using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The emote wheel's pictures: one pose per slice, so a player picks by shape before reading a word (the owner's
    /// standing bar, "pictograms over text"). Each is a stick figure caught at the emote's key pose: LET'S GO arms
    /// up, NO WAY a hand on the face, TOO EASY lying back, HA HA doubled over, DANCE a point at the sky, COME ON arms
    /// spread with the hands beckoning, GG bowing over its fists.
    ///
    /// Baked in code like `VerbIcons` and for its reason: white on transparent, tinted at the use site, nothing on
    /// disk to drift from the code. The primitives are that file's, copied rather than shared for the same reason it
    /// gives about `AbilityIcons`.
    /// </summary>
    public static class EmoteGlyphs
    {
        private const int Size = 128;
        private const float Limb = 0.075f;

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>The picture for a wheel emote id, or null for an id the wheel does not offer.</summary>
        public static Sprite For(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (Cache.TryGetValue(id, out var cached) && cached != null) return cached;
            if (!Known(id)) return null;
            var sprite = Bake(id);
            Cache[id] = sprite;
            return sprite;
        }

        private static bool Known(string id)
        {
            switch (id)
            {
                case "yes": case "no": case "sit": case "crouch": case "dance": case "tpose": case "bow": return true;
                default: return false;
            }
        }

        private static Sprite Bake(string id)
        {
            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float u = (x + 0.5f) / Size * 2.0f - 1.0f, v = (y + 0.5f) / Size * 2.0f - 1.0f;
                    pixels[y * Size + x] = new Color(1, 1, 1, Mathf.Clamp01(Coverage(id, u, v)));
                }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "emoteglyph_" + id, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            tex.SetPixels(pixels);
            tex.Apply(false, false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100.0f, 0, SpriteMeshType.FullRect);
            sprite.name = tex.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static float Coverage(string id, float u, float v)
        {
            switch (id)
            {
                // LET'S GO: arms flung up in a V, legs apart off the ground, two burst ticks over each fist.
                case "yes":
                    return Max(
                        Figure(u, v, 0, 0.42f, 0, -0.18f),
                        Line(u, v, 0, 0.20f, -0.48f, 0.70f), Line(u, v, 0, 0.20f, 0.48f, 0.70f),
                        Line(u, v, 0, -0.18f, -0.30f, -0.72f), Line(u, v, 0, -0.18f, 0.30f, -0.72f),
                        Tick(u, v, -0.66f, 0.80f, -0.80f, 0.92f), Tick(u, v, 0.66f, 0.80f, 0.80f, 0.92f));

                // NO WAY: head dropped forward into one hand, the other arm hanging, shoulders slumped.
                case "no":
                    return Max(
                        Head(u, v, 0.14f, 0.36f), Line(u, v, 0, 0.16f, -0.06f, -0.22f),
                        Line(u, v, 0, 0.14f, 0.30f, -0.02f), Line(u, v, 0.30f, -0.02f, 0.26f, 0.28f),
                        Line(u, v, 0, 0.14f, -0.20f, -0.24f),
                        Line(u, v, -0.06f, -0.22f, -0.20f, -0.76f), Line(u, v, -0.06f, -0.22f, 0.12f, -0.76f));

                // TOO EASY: lying back on an elbow, the other hand behind the head, legs out along the ground.
                case "sit":
                    return Max(
                        Head(u, v, -0.44f, 0.18f), Line(u, v, -0.30f, 0.00f, 0.10f, -0.40f),
                        Line(u, v, -0.28f, -0.02f, -0.52f, -0.54f), Line(u, v, -0.30f, 0.00f, -0.12f, 0.40f),
                        Line(u, v, -0.12f, 0.40f, -0.52f, 0.40f),
                        Line(u, v, 0.10f, -0.40f, 0.84f, -0.44f), Line(u, v, 0.10f, -0.40f, 0.80f, -0.30f),
                        Line(u, v, -0.84f, -0.60f, 0.90f, -0.60f, 0.035f));

                // HA HA: doubled over, a hand on the belly, the other slapping the knee, laugh lines off the head.
                case "crouch":
                    return Max(
                        Head(u, v, 0.30f, 0.06f), Line(u, v, 0.16f, 0.06f, -0.22f, 0.20f),
                        Line(u, v, 0.02f, 0.10f, 0.06f, -0.16f), Line(u, v, 0.02f, 0.10f, 0.26f, -0.30f),
                        Line(u, v, -0.22f, 0.20f, -0.18f, -0.76f), Line(u, v, -0.22f, 0.20f, 0.14f, -0.76f),
                        Tick(u, v, 0.54f, 0.30f, 0.70f, 0.44f), Tick(u, v, 0.60f, 0.12f, 0.80f, 0.16f));

                // DANCE: one arm pointing at the sky, the other down across the hip, a hip pushed out, a note beside it.
                case "dance":
                    return Max(
                        Figure(u, v, -0.10f, 0.42f, -0.06f, -0.18f),
                        Line(u, v, -0.08f, 0.20f, 0.36f, 0.72f), Line(u, v, -0.08f, 0.20f, -0.40f, -0.08f),
                        Line(u, v, -0.06f, -0.18f, -0.40f, -0.72f), Line(u, v, -0.06f, -0.18f, 0.16f, -0.72f),
                        Disc(u - 0.62f, v + 0.10f, 0.10f), Line(u, v, 0.70f, -0.10f, 0.70f, 0.40f, 0.035f),
                        Line(u, v, 0.70f, 0.40f, 0.86f, 0.30f, 0.035f));

                // COME ON: chest out, both arms spread low with the hands turned up, two chevrons drawn in toward it.
                case "tpose":
                    return Max(
                        Figure(u, v, 0, 0.42f, 0, -0.18f),
                        Line(u, v, 0, 0.18f, -0.42f, 0.02f), Line(u, v, -0.42f, 0.02f, -0.50f, 0.22f),
                        Line(u, v, 0, 0.18f, 0.42f, 0.02f), Line(u, v, 0.42f, 0.02f, 0.50f, 0.22f),
                        Line(u, v, 0, -0.18f, -0.24f, -0.74f), Line(u, v, 0, -0.18f, 0.24f, -0.74f),
                        Line(u, v, -0.88f, 0.30f, -0.72f, 0.14f, 0.04f), Line(u, v, -0.72f, 0.14f, -0.88f, -0.02f, 0.04f),
                        Line(u, v, 0.88f, 0.30f, 0.72f, 0.14f, 0.04f), Line(u, v, 0.72f, 0.14f, 0.88f, -0.02f, 0.04f));

                // GG: bowed from the hips over both fists held together in front.
                case "bow":
                    return Max(
                        Head(u, v, 0.44f, 0.04f), Line(u, v, 0.28f, 0.12f, -0.14f, -0.06f),
                        Line(u, v, 0.18f, 0.08f, 0.18f, -0.30f), Disc(u - 0.18f, v + 0.34f, 0.11f),
                        Line(u, v, -0.14f, -0.06f, -0.18f, -0.76f), Line(u, v, -0.14f, -0.06f, 0.08f, -0.76f));

                default: return 0.0f;
            }
        }

        /// <summary>A head and a straight body from the neck to the hips.</summary>
        private static float Figure(float u, float v, float headX, float headY, float hipX, float hipY)
            => Mathf.Max(Head(u, v, headX, headY), Line(u, v, headX, headY - 0.20f, hipX, hipY));

        private static float Head(float u, float v, float x, float y) => Disc(u - x, v - y, 0.15f);

        private static float Line(float u, float v, float ax, float ay, float bx, float by, float half = Limb)
            => Segment(u, v, ax, ay, bx, by, half);

        private static float Tick(float u, float v, float ax, float ay, float bx, float by) => Segment(u, v, ax, ay, bx, by, 0.035f);

        private static float Max(params float[] parts)
        {
            float best = 0;
            foreach (float p in parts) best = Mathf.Max(best, p);
            return best;
        }

        private static float Edge(float distance)
        {
            const float feather = 2.5f / Size;
            return Mathf.Clamp01(0.5f - distance / feather);
        }

        private static float Disc(float u, float v, float r) => Edge(Mathf.Sqrt(u * u + v * v) - r);

        private static float Segment(float u, float v, float ax, float ay, float bx, float by, float halfThickness)
        {
            float pax = u - ax, pay = v - ay, bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;
            return Edge(Mathf.Sqrt(dx * dx + dy * dy) - halfThickness);
        }
    }
}
