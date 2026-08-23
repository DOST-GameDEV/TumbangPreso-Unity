using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// What KIND of thing an ability is, drawn as a shape rather than spelled out.
    ///
    /// ⚠️⚠️ THE GLYPH DESCRIBES THE SHAPE OF THE EFFECT, NOT THE ELEMENT. Fire, ice and
    /// lightning are already carried by the hero's colour, and a flame icon on a fire hero's
    /// three powers tells the player nothing about which of the three to press. What they need
    /// at a glance is *what it does to the world*: does it put something on the ground, put a
    /// wall in front of me, move me, protect me, or hit everything at once. Two heroes with
    /// completely different fiction share a glyph when they share a job, on purpose.
    ///
    /// ⚠️ ADD A GLYPH RATHER THAN REUSING A NEAR MISS. A wrong icon is worse than a generic
    /// one, because the player trusts it once and then stops trusting all of them.
    /// </summary>
    public enum AbilityGlyph
    {
        /// <summary>A patch of ground that does something to whoever stands on it.</summary>
        Zone,

        /// <summary>A solid thing placed in the world that bodies and slippers cannot pass.</summary>
        Wall,

        /// <summary>Moves the caster. Rushes, dashes, grinds.</summary>
        Dash,

        /// <summary>Protects the caster. Armour, immunity, carapace.</summary>
        Shield,

        /// <summary>Goes off around the caster and hits everything in reach.</summary>
        Burst,

        /// <summary>Sends something away from the caster at a target.</summary>
        Projectile,

        /// <summary>The caster stops being fully present. Phase, ghost, untargetable.</summary>
        Phase,

        /// <summary>Comes down from above onto a place. Slams, strikes, fissures.</summary>
        Slam,

        /// <summary>Changes the tsinelas rather than the world. Overcharge, curve, empower.</summary>
        Empower,
    }

    /// <summary>
    /// Procedural icons for the ability deck, the hold-to-inspect panel and character select.
    ///
    /// ⚠️⚠️ THEY ARE BAKED IN CODE, LIKE EVERY OTHER SURFACE IN THIS UI. `GodotTheme` already
    /// paints every box, border and shadow in the game into a cached texture rather than
    /// shipping PNGs, for the reason its own note gives: a baked file that drifts from the
    /// code that wanted it is indistinguishable from a broken conversion. Icons follow the same
    /// rule, and it also means the ability art is not blocked on the team's art queue
    /// (`CLAUDE.md` § 4a). When real icons land, replace `Bake` and nothing else changes.
    ///
    /// ⚠️ WHITE ON TRANSPARENT, TINTED AT THE USE SITE. One texture per glyph, ever, and the
    /// hero colour arrives through `Image.color`. Baking a coloured icon per hero would be
    /// five times the textures for no visual difference.
    /// </summary>
    public static class AbilityIcons
    {
        private const int Size = 128;

        private static readonly Dictionary<AbilityGlyph, Sprite> Cache =
            new Dictionary<AbilityGlyph, Sprite>();

        public static Sprite For(AbilityGlyph glyph)
        {
            if (Cache.TryGetValue(glyph, out var cached) && cached != null) return cached;

            var sprite = Bake(glyph);
            Cache[glyph] = sprite;
            return sprite;
        }

        /// <summary>
        /// A one-word name for the glyph, for the inspect panel's "what is this" line.
        ///
        /// ⚠️ IT SAYS THE JOB, NOT THE SHAPE. "GROUND ZONE" is useful; "circle" is noise.
        /// </summary>
        public static string LabelFor(AbilityGlyph glyph)
        {
            switch (glyph)
            {
                case AbilityGlyph.Zone: return "GROUND ZONE";
                case AbilityGlyph.Wall: return "BLOCKER";
                case AbilityGlyph.Dash: return "MOBILITY";
                case AbilityGlyph.Shield: return "PROTECTION";
                case AbilityGlyph.Burst: return "AREA BURST";
                case AbilityGlyph.Projectile: return "PROJECTILE";
                case AbilityGlyph.Phase: return "EVASION";
                case AbilityGlyph.Slam: return "FROM ABOVE";
                case AbilityGlyph.Empower: return "TSINELAS BUFF";
                default: return "POWER";
            }
        }

        // ------------------------------------------------------------------ baking

        private static Sprite Bake(AbilityGlyph glyph)
        {
            var pixels = new Color[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // Centred, normalised to roughly -1..1
                    float u = (x + 0.5f) / Size * 2.0f - 1.0f;
                    float v = (y + 0.5f) / Size * 2.0f - 1.0f;

                    float a = Coverage(glyph, u, v);
                    pixels[y * Size + x] = new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp01(a));
                }
            }

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "abilityglyph_" + glyph,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            tex.SetPixels(pixels);
            tex.Apply(false, false);

            var sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f),
                                       100.0f, 0, SpriteMeshType.FullRect);
            sprite.name = tex.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        /// <summary>
        /// The nine glyphs, as signed-distance coverage in a -1..1 square.
        ///
        /// ⚠️⚠️ REDRAWN 2026-08-23 BECAUSE THEY WERE UNREADABLE AT THE SIZE THEY ARE ACTUALLY
        /// DRAWN. A deck tile is 70 x 58 with the glyph inset to about 50 x 38, so a 128 px
        /// sprite lands on screen at roughly 40 px. The old set was line art at 0.06 to 0.09
        /// stroke, which is 4 to 6 texture pixels, which is ONE AND A HALF SCREEN PIXELS after
        /// the downscale. Every one of them mushed into a grey smudge, and the Zone glyph in
        /// particular (two concentric rings, a diamond and two tick markers) came out as a
        /// three pixel funnel.
        ///
        /// ⚠️⚠️ THE RULES THAT REPLACED IT, AND THEY ARE THE VALORANT AND OVERWATCH RULES:
        ///
        ///   1. **One stroke weight, and it is fat.** `Stroke` below is 0.16 of the half-square,
        ///      which is about 10 texture pixels and 3 screen pixels. Nothing is thinner.
        ///   2. **At most three elements.** A glyph is recognised by SILHOUETTE, and detail
        ///      inside the silhouette is invisible at 40 px. Every marker, notch, secondary ring
        ///      and twin trail is gone.
        ///   3. **Solid mass beats outline.** A filled shield reads instantly; a shield drawn as
        ///      a rim reads as a ring.
        ///
        /// The test is whether it survives at 24 px. If a change fails that, it fails.
        /// </summary>
        private static float Coverage(AbilityGlyph glyph, float u, float v)
        {
            switch (glyph)
            {
                case AbilityGlyph.Zone:
                    // A patch of ground, seen at an angle, with a marked spot in the middle.
                    // ⚠️ THE CORE IS SMALL. At 0.30 by 0.17 the ring and the core were close
                    // enough in size to read as an EYE, which is a different icon entirely.
                    // ⚠️⚠️ THE CENTRE DOT IS GONE, AND THAT IS WHAT STOPPED IT BEING AN EYE. A
                    // thick ellipse ring with a small dot inside it IS the eye icon, in every
                    // icon set there has ever been, and rounding the ellipse off did not shift
                    // the read at all: the pupil was doing the work. Two rounds of contact sheet
                    // to find that out, which is the whole argument for rendering every change.
                    //
                    // ⚠️ AND A BARE RING IS THE RIGHT ANSWER ANYWAY, because it is what the
                    // ability actually PUTS ON THE COURT. `GroundReticle` draws a rim and a
                    // translucent fill; the glyph is now a small picture of the telegraph the
                    // player is about to see, rather than a symbol they have to learn separately.
                    return EllipseRing(u, v, 0.82f, 0.54f, 0.26f);

                case AbilityGlyph.Wall:
                    // Three slabs standing on one line. The line is what says "placed", and the
                    // uneven heights are what stop it reading as a bar chart.
                    return Mathf.Max(Mathf.Max(
                        Box(u + 0.44f, v + 0.16f, 0.18f, 0.50f),
                        Box(u, v - 0.02f, 0.20f, 0.68f)),
                        Box(u - 0.44f, v + 0.16f, 0.18f, 0.50f));

                case AbilityGlyph.Dash:
                    // Two fat chevrons. Motion, pointing the way the caster goes.
                    return Mathf.Max(
                        Chevron(u - 0.30f, v, 0.62f, Stroke),
                        Chevron(u + 0.16f, v, 0.62f, Stroke));

                case AbilityGlyph.Shield:
                    // A solid crest, and nothing inside it.
                    // ⚠️⚠️ THE KNOCKED-OUT BAND WAS CUTTING IT IN HALF. At 40 px the gap read
                    // as two shapes stacked on each other, and with the sharp taper below it the
                    // lower half came out as a funnel. A shield is recognised by its SILHOUETTE
                    // (flat top, straight shoulders, point at the bottom), so the fix is to stop
                    // putting anything inside the silhouette at all.
                    return Crest(u, v, 1.36f, 1.72f);

                case AbilityGlyph.Burst:
                    // Solid core, six fat rays. Six rather than eight: at 40 px, eight spokes
                    // touch each other at the hub and the whole thing fills in as a disc.
                    return Mathf.Max(
                        Disc(u, v, 0.26f),
                        Spokes(u, v, 6, 0.40f, 0.94f, Stroke));

                case AbilityGlyph.Projectile:
                    // A head, a gap, and ONE trail behind it.
                    // ⚠️⚠️ THE GAP IS THE ICON. Butted straight against its trail the head
                    // stopped being a projectile and became the bell of a trumpet: one
                    // continuous shape that flares. Separated, the eye reads a thing that has
                    // LEFT something behind, which is what a projectile is.
                    return Mathf.Max(
                        RightTriangle(u - 0.30f, v, 0.44f, 0.60f),
                        Box(u + 0.60f, v, 0.26f, Stroke * 0.55f));

                case AbilityGlyph.Phase:
                    // A thick crescent with the caster half gone: here and not here at once.
                    return Mathf.Max(
                        Sub(Disc(u, v, 0.84f), Disc(u - 0.34f, v + 0.05f, 0.76f)),
                        Disc(u - 0.40f, v - 0.14f, 0.17f));

                case AbilityGlyph.Slam:
                    // Down arrow onto a line at the BOTTOM.
                    // ⚠️⚠️ IT WAS UPSIDE DOWN, AND THE RENDER IS WHAT CAUGHT IT. Box(u, v - k)
                    // centres at v = +k and v runs UP the texture, so the ground line sat at the
                    // TOP of the tile with the arrow hanging below it: a thing dangling off a
                    // ceiling rather than a slam coming down onto the court. Exactly why every
                    // glyph change gets a picture (CLAUDE.md 6.1).
                    return Mathf.Max(Mathf.Max(
                        Box(u, v - 0.27f, Stroke, 0.45f),
                        DownTriangle(u, v + 0.18f, 0.46f, 0.44f)),
                        Box(u, v + 0.80f, 0.66f, 0.11f));

                case AbilityGlyph.Empower:
                    // A bolt, drawn as one solid stroke rather than a rimmed diamond with a
                    // spike inside it. Three capsules meeting end to end, so the joints are
                    // round and it holds together when it is four pixels wide.
                    return Bolt(u, v);

                default:
                    return Disc(u, v, 0.62f);
            }
        }

        // ------------------------------------------------------------------ primitives

        /// <summary>
        /// The one stroke weight in the set.
        ///
        /// ⚠️⚠️ 0.16 OF THE HALF-SQUARE, WHICH IS ABOUT 10 OF 128 TEXTURE PIXELS AND 3 OF THE
        /// 40 SCREEN PIXELS A DECK GLYPH ACTUALLY OCCUPIES. Anything thinner disappears, and
        /// that is not a guess: the previous set ran 0.06 to 0.09 and every glyph in it was a
        /// smudge. If a new glyph "needs" a finer line, it needs fewer parts instead.
        /// </summary>
        private const float Stroke = 0.16f;

        private static float Edge(float distance)
        {
            const float feather = 2.5f / Size;
            return Mathf.Clamp01(0.5f - distance / feather);
        }

        private static float Sub(float shape, float hole) => Mathf.Clamp01(shape - hole);

        // ⚠️ `Ring` AND `Diamond` WERE DELETED WITH THE OLD LINE-ART SET, NOT MISLAID. Both drew
        // strokes thinner than `Stroke` by construction and neither survived the 24 px test, so
        // keeping them around as primitives would only make the next glyph easy to draw badly.

        private static float Disc(float u, float v, float r)
            => Edge(Mathf.Sqrt(u * u + v * v) - r);

        private static float EllipseDisc(float u, float v, float rx, float ry)
        {
            float d = Mathf.Sqrt((u / rx) * (u / rx) + (v / ry) * (v / ry)) - 1.0f;
            return Edge(d * ry);
        }

        private static float EllipseRing(float u, float v, float rx, float ry, float thickness)
        {
            float d = Mathf.Sqrt((u / rx) * (u / rx) + (v / ry) * (v / ry)) - 1.0f;
            return Edge(Mathf.Abs(d) * ry - thickness * 0.5f);
        }

        private static float Box(float u, float v, float halfW, float halfH)
        {
            float dx = Mathf.Abs(u) - halfW;
            float dy = Mathf.Abs(v) - halfH;
            return Edge(Mathf.Max(dx, dy));
        }

        /// <summary>
        /// A capsule between two points.
        ///
        /// ⚠️ THE ROUND CAPS ARE WHY THE BOLT SURVIVES BEING FOUR PIXELS WIDE. Three boxes
        /// meeting at an angle leave a notch at every joint, and a notch at this size is a break
        /// in the stroke.
        /// </summary>
        private static float Segment(float u, float v, float ax, float ay, float bx, float by,
                                     float halfThickness)
        {
            float pax = u - ax, pay = v - ay;
            float bax = bx - ax, bay = by - ay;

            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;

            return Edge(Mathf.Sqrt(dx * dx + dy * dy) - halfThickness);
        }

        private static float Bolt(float u, float v)
        {
            float upper = Segment(u, v, 0.20f, 0.86f, -0.26f, 0.08f, Stroke * 0.85f);
            float cross = Segment(u, v, -0.26f, 0.08f, 0.20f, 0.02f, Stroke * 0.85f);
            float lower = Segment(u, v, 0.20f, 0.02f, -0.16f, -0.86f, Stroke * 0.85f);
            return Mathf.Max(upper, Mathf.Max(cross, lower));
        }

        /// <summary>A fat chevron pointing right, of the given vertical span.</summary>
        private static float Chevron(float u, float v, float halfSpan, float thickness)
        {
            if (Mathf.Abs(v) > halfSpan) return 0.0f;

            float leg = u + Mathf.Abs(v) * 0.9f;
            return Edge(Mathf.Abs(leg) - thickness * 0.5f);
        }

        private static float DownTriangle(float u, float v, float halfW, float height)
        {
            if (v < -height || v > 0.0f) return 0.0f;

            float t = Mathf.InverseLerp(-height, 0.0f, v);
            return Edge(Mathf.Abs(u) - halfW * t);
        }

        /// <summary>
        /// A triangle whose POINT is at the +u end and whose base is at -u.
        ///
        /// ⚠️⚠️ IT USED TO POINT BACKWARDS, AND ONLY THE CONTACT SHEET SHOWED IT. The taper ran
        /// the other way, so the projectile glyph was a left-pointing wedge sitting to the right
        /// of its own trail: an arrow flying away from the streak it had supposedly left. It
        /// looked deliberate enough at 128 px to survive a code read.
        /// </summary>
        private static float RightTriangle(float u, float v, float halfH, float length)
        {
            if (u < -length || u > 0.0f) return 0.0f;

            float t = Mathf.InverseLerp(-length, 0.0f, u);
            return Edge(Mathf.Abs(v) - halfH * (1.0f - t));
        }

        /// <summary>A solid shield crest: straight shoulders, tapering to a point.</summary>
        private static float Crest(float u, float v, float width, float height)
        {
            if (v > height * 0.5f || v < -height * 0.5f) return 0.0f;

            float t = Mathf.InverseLerp(height * 0.5f, -height * 0.5f, v);
            // ⚠️⚠️ THE EXPONENT IS THE DIFFERENCE BETWEEN A SHIELD AND A PENNANT. A crest is
            // recognised by STRAIGHT SHOULDERS that hold almost full width for the top half and
            // then break to a point. At 1.55 the width was already down to 66% at half height,
            // which is a triangle with a rounded top; at 4.0 it is still 94% there and the taper
            // happens in the bottom third, where it belongs.
            float curve = 1.0f - Mathf.Pow(t, 4.0f);
            return Edge(Mathf.Abs(u) - width * 0.5f * Mathf.Max(0.02f, curve));
        }

        private static float Spokes(float u, float v, int count, float inner, float outer,
                                    float thickness)
        {
            float best = 0.0f;

            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.PI * 2.0f * i / count;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                float su = u * cos + v * sin;
                float sv = -u * sin + v * cos;

                best = Mathf.Max(best, Segment(su, sv, inner, 0.0f, outer, 0.0f, thickness * 0.5f));
            }

            return best;
        }
    }
}
