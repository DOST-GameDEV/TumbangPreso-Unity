using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Eleven ways to put a word on a street, sharing one alphabet.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE "GIVE EVERY BUSINESS A DIFFERENT SIGN" WAS ALREADY DONE ONCE AND
    /// DID NOT WORK. `docs/Ilalim_Ng_Tulay.md` § 9.2 named one sign language per job and the
    /// implementation answered it with six different STRINGS on six near-identical horizontal
    /// rectangles: same 5-by-7 face, same stroke weight, same flat plate, same wall mounting,
    /// same 2.5 to 2.8 m height. In `ilalim_street_life_v14.png` and `ilalim_corridor_v14.png`
    /// they read as one sign painter with a stencil set, which is exactly the complaint.
    /// Changing the words is not changing the signs.
    ///
    /// ⚠️⚠️ AND THE FIX IS NOT A SECOND ALPHABET. `CLAUDE.md` § 3 and the note on
    /// <see cref="PcExpressSignAuthor.Font"/>: two blocky faces in one map is two things to keep
    /// in step, and the one that drifts is always the one nobody is looking at. So there is
    /// still exactly one glyph table in this repository, and <see cref="LetterStyle"/> is what
    /// varies: glyph aspect, stroke weight, tracking, slant and relief. A 0.50-aspect condensed
    /// stencil and a 0.95-aspect heavy hand-painted sign share every glyph definition and no
    /// visual DNA at all.
    ///
    /// ⚠️ EVERY BUILDER HERE TAGS ITS ROOT WITH `AirborneByDesign` AND A REASON, and that is not
    /// boilerplate. `MapGeometryCheck` walks `GetComponentInParent`, so one tag on the sign root
    /// covers every letter bar under it; without it a wall-mounted fascia is thirty-odd
    /// renderers hanging over open pavement and the gate goes red with a page of findings that
    /// are all the same finding.
    /// </summary>
    internal static class StreetSignKit
    {
        // ------------------------------------------------------------------
        // Letter styling
        // ------------------------------------------------------------------

        /// <summary>
        /// How one line of the shared 5-by-7 face is drawn.
        ///
        /// ⚠️ `Aspect` IS GLYPH WIDTH OVER GLYPH HEIGHT AND IT IS THE LEVER THAT MATTERS MOST.
        /// The shipped painter solved glyph width from the plate width and the character count,
        /// so a short word on a wide board produced fat letters and a long word on the same
        /// board produced thin ones, and every sign converged on "whatever fits". Fixing the
        /// aspect per STYLE and scaling the whole run down only if it overflows is what lets a
        /// vertical banner and a roof sign look like different trades.
        /// </summary>
        internal readonly struct LetterStyle
        {
            public readonly float Aspect;
            public readonly float Weight;
            public readonly float Tracking;
            public readonly float Slant;
            public readonly float Relief;

            public LetterStyle(float aspect, float weight, float tracking, float slant, float relief)
            {
                Aspect = aspect;
                Weight = weight;
                Tracking = tracking;
                Slant = slant;
                Relief = relief;
            }
        }

        /// <summary>Enamel on concrete. Narrow, even, upright: a regulation notice.</summary>
        internal static readonly LetterStyle Stencil = new LetterStyle(0.62f, 1.00f, 0.18f, 0.00f, 0.30f);

        /// <summary>A brush and a steady hand. Wide and heavy, loose between letters.</summary>
        internal static readonly LetterStyle HandPainted = new LetterStyle(0.78f, 1.30f, 0.24f, 0.00f, 0.34f);

        /// <summary>Squeezed to fit a blade or a banner. Tight tracking, thin strokes.</summary>
        internal static readonly LetterStyle Condensed = new LetterStyle(0.48f, 0.95f, 0.10f, 0.00f, 0.28f);

        /// <summary>Roof letters and pylons, read from across the boulevard.</summary>
        internal static readonly LetterStyle Display = new LetterStyle(0.95f, 1.35f, 0.30f, 0.00f, 0.55f);

        /// <summary>Leaning, because half the repair shops in Manila lean their type.</summary>
        internal static readonly LetterStyle Italic = new LetterStyle(0.66f, 1.15f, 0.14f, 0.26f, 0.32f);

        /// <summary>Printed on cloth. Light strokes, wide set, no relief to speak of.</summary>
        internal static readonly LetterStyle Cloth = new LetterStyle(0.74f, 0.82f, 0.22f, 0.00f, 0.16f);

        /// <summary>Chalk. Thin, uneven spacing, barely proud of the board.</summary>
        internal static readonly LetterStyle Chalked = new LetterStyle(0.70f, 0.88f, 0.26f, 0.00f, 0.20f);

        /// <summary>Paint straight on render, sun-bleached. Very wide, very heavy.</summary>
        internal static readonly LetterStyle Mural = new LetterStyle(1.05f, 1.45f, 0.34f, 0.00f, 0.10f);

        // ------------------------------------------------------------------
        // Palette. Named, because a sign's colour is part of which trade it is.
        // ------------------------------------------------------------------

        internal static readonly Color Ink = new Color(0.960f, 0.950f, 0.910f);
        internal static readonly Color SignWood = new Color(0.230f, 0.150f, 0.105f);
        internal static readonly Color SignCream = new Color(0.900f, 0.830f, 0.650f);
        internal static readonly Color SignMaroon = new Color(0.430f, 0.120f, 0.130f);
        internal static readonly Color Chalkboard = new Color(0.105f, 0.150f, 0.125f);
        internal static readonly Color TarpBlue = new Color(0.160f, 0.360f, 0.620f);
        internal static readonly Color BawalRed = new Color(0.720f, 0.180f, 0.150f);
        internal static readonly Color RustedTin = new Color(0.470f, 0.335f, 0.245f);
        internal static readonly Color RustStreak = new Color(0.360f, 0.215f, 0.140f);
        internal static readonly Color SunBleach = new Color(0.700f, 0.685f, 0.625f);

        /// <summary>Paint that has been on a wall for fifteen summers. ⚠️ IT KEEPS ITS VALUE AND
        /// LOSES ITS SATURATION, which is what sun actually does to pigment. A pale ink on a
        /// cream facade has no contrast left at all and reads as a blank panel.</summary>
        internal static readonly Color MuralInk = new Color(0.335f, 0.285f, 0.270f);
        internal static readonly Color PylonSteel = new Color(0.330f, 0.345f, 0.360f);
        internal static readonly Color ShopGreen = new Color(0.130f, 0.390f, 0.290f);
        internal static readonly Color ShopOchre = new Color(0.780f, 0.560f, 0.180f);
        internal static readonly Color ShopPlum = new Color(0.380f, 0.180f, 0.320f);
        internal static readonly Color PlasticWhite = new Color(0.880f, 0.880f, 0.855f);
        internal static readonly Color DarkMetal = new Color(0.175f, 0.190f, 0.210f);

        // ------------------------------------------------------------------
        // Primitives. Everything below is built from these two.
        // ------------------------------------------------------------------

        private static GameObject Plate(Transform parent, string name, Vector3 centre,
                                        Vector3 size, Color tint)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = centre;
            go.transform.localScale = size;
            Tint(go, tint);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void Tint(GameObject go, Color colour)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            // ⚠️ A REAL MATERIAL, NOT A PROPERTY BLOCK. `MaterialKit.Dress` writes into a
            // `MaterialPropertyBlock`, which is a RUNTIME override that is never serialised into
            // a scene file, so every plate built at edit time would load back white. Same reason
            // and same fix as `IlalimNgTulayBuilder.Paint`.
            var material = new Material(Visual.MaterialKit.Lit.shader) { name = $"Sign_{go.name}" };
            material.color = colour;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.06f);
            renderer.sharedMaterial = material;
        }

        /// <summary>
        /// One line of the shared face, laid out in the parent plate's own local space.
        ///
        /// ⚠️ THE RUN IS CENTRED AND ONLY SHRINKS IF IT OVERFLOWS. `centreY` and `glyphH` are
        /// fractions of the plate, so a style keeps its proportions on a 0.9 m A-board and on a
        /// 4 m roof sign, and a word that is too long for its board loses size rather than
        /// losing its letterforms.
        /// </summary>
        internal static void PaintLine(Transform plate, string text, float centreY, float glyphH,
                                       string name, Color ink, in LetterStyle style)
        {
            const float margin = 0.06f;

            // ⚠⚠⚠ THE PLATE IS A NON-UNIFORMLY SCALED CUBE, SO A RATIO IN ITS LOCAL SPACE IS
            // NOT A RATIO ON THE WALL, AND FORGETTING THAT MADE EVERY WIDE SIGN ILLEGIBLE. The
            // first version set `glyphW = glyphH * Aspect` in the plate's own 0..1 space. On the
            // 1.80 by 0.92 m tin sheet that is a local ratio of 0.78 and a WORLD ratio of
            // 0.78 x 1.80 / 0.92 = 1.53: letters twice as wide as they are tall. Worse, the run
            // then overflowed and was scaled down to fit, which shrank the column PITCH while
            // `Weight` kept the stroke thickness at 1.3 times the original pitch, so adjacent lit
            // columns merged. `ilalim_pavement_east_v20.png` shows the result: "GOMA" as four
            // unreadable slabs. Correcting by the plate's own aspect makes `LetterStyle.Aspect`
            // mean what it says on every board, whatever shape the board is.
            //
            // ⚠ `lossyScale`, NOT `localScale`. Several of these plates are children of a
            // rotated sign root and one (`RoofLetters`) is a bare carrier, so the parent chain
            // carries scale the plate itself does not.
            Vector3 plateScale = plate.lossyScale;
            float shapeCorrection = plateScale.x > 0.0001f
                ? Mathf.Abs(plateScale.y) / Mathf.Abs(plateScale.x)
                : 1.0f;

            float glyphW = glyphH * style.Aspect * shapeCorrection;
            float gap = glyphH * style.Tracking * shapeCorrection;
            float total = text.Length * glyphW + Mathf.Max(0, text.Length - 1) * gap;
            float room = 1.0f - margin * 2.0f;

            if (total > room && total > 0.0f)
            {
                float k = room / total;
                glyphW *= k;
                gap *= k;
                glyphH *= k;
                total = room;
            }

            var root = new GameObject(name);
            root.transform.SetParent(plate, false);

            float x = -total * 0.5f;
            float thickness = glyphW / 5.0f * style.Weight;

            foreach (char c in text)
            {
                if (c == ' ' || !PcExpressSignAuthor.Font.TryGetValue(c, out var rows))
                {
                    x += glyphW + gap;
                    continue;
                }

                for (int col = 0; col < 5; col++)
                {
                    int run = -1;

                    for (int row = 0; row <= 7; row++)
                    {
                        bool lit = row < 7 && rows[row][col] == '1';

                        if (lit && run < 0) run = row;
                        if (lit || run < 0) continue;

                        float top = centreY + glyphH * (0.5f - run / 7.0f);
                        float bottom = centreY + glyphH * (0.5f - row / 7.0f);
                        float barY = (top + bottom) * 0.5f;

                        var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        bar.name = "g";
                        bar.transform.SetParent(root.transform, false);
                        // The shear is a WORLD lean, so it takes the same correction the
                        // glyph width does. Without it an italic sign leans further on a wide
                        // board than on a tall one.
                        bar.transform.localPosition = new Vector3(
                            x + glyphW * (col + 0.5f) / 5.0f
                              + style.Slant * (barY - centreY) * shapeCorrection,
                            barY,
                            -0.5f - style.Relief * 0.5f);
                        bar.transform.localScale = new Vector3(thickness, top - bottom, style.Relief);
                        Tint(bar, ink);
                        Object.DestroyImmediate(bar.GetComponent<Collider>());

                        run = -1;
                    }
                }

                x += glyphW + gap;
            }
        }

        /// <summary>Several lines stacked down the plate, sharing one style.</summary>
        internal static void PaintLines(Transform plate, string[] lines, string name, Color ink,
                                        in LetterStyle style, float fill = 0.72f)
        {
            float lineHeight = 1.0f / (lines.Length + 0.6f);
            for (int i = 0; i < lines.Length; i++)
            {
                float centreY = 0.5f - lineHeight * (i + 0.8f);
                PaintLine(plate, lines[i], centreY, lineHeight * fill, $"{name}_L{i}", ink, style);
            }
        }

        private static GameObject Root(Transform parent, string name, Vector3 position, float yaw,
                                       string reason)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
            AirborneByDesign.Attach(go, reason);
            return go;
        }

        // ------------------------------------------------------------------
        // The eleven systems.
        //
        // ⚠️ EACH ONE TAKES ITS ANCHOR AS THE POINT WHERE IT MEETS THE WORLD, not as its own
        // centre. A blade's anchor is the bracket on the wall, a pylon's is the foot of its
        // post, an A-board's is the pavement between its legs. That is what lets the caller
        // place them from the surface height (`SurfaceTop`) rather than by solving for a
        // centre, which is how the shipped map buried a dozen props by 62 mm.
        // ------------------------------------------------------------------

        /// <summary>Timber-framed painted board, wall-flush. The pisonet rate sign.</summary>
        internal static GameObject FramedFascia(Transform parent, string name, Vector3 wallPoint,
                                                float yaw, Vector2 size, Color face, Color ink,
                                                string[] lines, string reason)
        {
            var root = Root(parent, name, wallPoint, yaw, reason);

            Plate(root.transform, "Frame", new Vector3(0.0f, 0.0f, 0.045f),
                  new Vector3(size.x + 0.20f, size.y + 0.20f, 0.10f), SignWood);
            var board = Plate(root.transform, "Face", Vector3.zero,
                              new Vector3(size.x, size.y, 0.05f), face);

            // A second, thinner inner frame is what tells a framed fascia from a plain board at
            // twenty metres: it doubles the shadow line the ink outline draws.
            Plate(root.transform, "InnerLip", new Vector3(0.0f, 0.0f, -0.010f),
                  new Vector3(size.x - 0.10f, size.y - 0.10f, 0.03f), SignWood);

            PaintLines(board.transform, lines, name, ink, HandPainted);
            return root;
        }

        /// <summary>Projecting vertical blade on two brackets, read while walking past it.</summary>
        internal static GameObject Blade(Transform parent, string name, Vector3 wallPoint,
                                         float yaw, Vector2 size, Color face, Color ink,
                                         string[] lines, string reason)
        {
            var root = Root(parent, name, wallPoint, yaw, reason);

            foreach (float y in new[] { size.y * 0.36f, -size.y * 0.36f })
            {
                Plate(root.transform, $"Bracket_{y:F2}", new Vector3(0.0f, y, 0.30f),
                      new Vector3(0.05f, 0.05f, 0.62f), DarkMetal);
            }

            var board = Plate(root.transform, "Face", Vector3.zero,
                              new Vector3(size.x, size.y, 0.07f), face);
            Plate(root.transform, "Edge", new Vector3(0.0f, 0.0f, 0.035f),
                  new Vector3(size.x + 0.07f, size.y + 0.07f, 0.05f), DarkMetal);

            PaintLines(board.transform, lines, name, ink, Italic);
            return root;
        }

        /// <summary>Small chalk board on two legs, standing on the pavement.</summary>
        internal static GameObject ABoard(Transform parent, string name, Vector3 groundPoint,
                                          float yaw, Vector2 size, string[] lines, string reason)
        {
            var root = Root(parent, name, groundPoint, yaw, reason);

            // The legs REACH THE GROUND, which is why the anchor is the pavement and not the
            // board. They are the only part of an A-board the resting check can see.
            foreach (float x in new[] { -size.x * 0.36f, size.x * 0.36f })
            {
                Plate(root.transform, $"Leg_{x:F2}", new Vector3(x, size.y * 0.30f, 0.0f),
                      new Vector3(0.055f, size.y * 0.60f + 0.20f, 0.055f), SignWood);
            }

            var board = Plate(root.transform, "Face",
                              new Vector3(0.0f, size.y * 0.62f + 0.16f, 0.0f),
                              new Vector3(size.x, size.y, 0.05f), Chalkboard);
            Plate(root.transform, "Rail", new Vector3(0.0f, size.y * 0.62f + 0.16f, 0.030f),
                  new Vector3(size.x + 0.08f, size.y + 0.08f, 0.03f), SignWood);

            PaintLines(board.transform, lines, name, Ink, Chalked);
            return root;
        }

        /// <summary>Enamel notice painted flat on concrete. No frame, no depth.</summary>
        internal static GameObject Placard(Transform parent, string name, Vector3 wallPoint,
                                           float yaw, Vector2 size, Color face, string[] lines,
                                           string reason)
        {
            var root = Root(parent, name, wallPoint, yaw, reason);
            var board = Plate(root.transform, "Face", Vector3.zero,
                              new Vector3(size.x, size.y, 0.04f), face);
            PaintLines(board.transform, lines, name, Ink, Stencil);
            return root;
        }

        /// <summary>Printed cloth, lashed at the corners, with a slight sag along the bottom.</summary>
        internal static GameObject Tarpaulin(Transform parent, string name, Vector3 wallPoint,
                                             float yaw, Vector2 size, Color face, string[] lines,
                                             string reason)
        {
            var root = Root(parent, name, wallPoint, yaw, reason);
            var board = Plate(root.transform, "Face", Vector3.zero,
                              new Vector3(size.x, size.y, 0.02f), face);

            // Four eyelets and a sagging hem. Cloth is the one sign material with no straight
            // bottom edge, and that is the whole reason a tarpaulin reads as a tarpaulin.
            foreach (float sx in new[] { -1.0f, 1.0f })
            {
                foreach (float sy in new[] { -1.0f, 1.0f })
                {
                    Plate(root.transform, $"Eyelet_{sx:F0}_{sy:F0}",
                          new Vector3(sx * size.x * 0.46f, sy * size.y * 0.44f, -0.012f),
                          new Vector3(0.05f, 0.05f, 0.03f), DarkMetal);
                }
            }

            Plate(root.transform, "Sag", new Vector3(0.0f, -size.y * 0.50f, 0.005f),
                  new Vector3(size.x * 0.82f, size.y * 0.10f, 0.02f), face);

            PaintLines(board.transform, lines, name, Ink, Cloth);
            return root;
        }

        /// <summary>Double-sided box on its own post, above head height.</summary>
        internal static GameObject Pylon(Transform parent, string name, Vector3 groundPoint,
                                         float yaw, Vector2 size, float height, Color face,
                                         Color ink, string[] lines, string reason)
        {
            var root = Root(parent, name, groundPoint, yaw, reason);

            Plate(root.transform, "Post", new Vector3(0.0f, height * 0.5f, 0.0f),
                  new Vector3(0.14f, height, 0.14f), PylonSteel);
            Plate(root.transform, "Collar", new Vector3(0.0f, height - size.y * 0.62f, 0.0f),
                  new Vector3(0.24f, 0.12f, 0.24f), DarkMetal);

            var box = Plate(root.transform, "Face", new Vector3(0.0f, height, 0.0f),
                            new Vector3(size.x, size.y, 0.24f), face);
            var back = Plate(root.transform, "FaceBack", new Vector3(0.0f, height, 0.0f),
                             new Vector3(size.x, size.y, 0.24f), face);
            back.transform.localRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);

            PaintLines(box.transform, lines, name, ink, Display);
            PaintLines(back.transform, lines, name + "_B", ink, Display);
            return root;
        }

        /// <summary>Small panel hung on two drop rods, under an awning.</summary>
        internal static GameObject HungPanel(Transform parent, string name, Vector3 hangPoint,
                                             float yaw, Vector2 size, float drop, Color face,
                                             Color ink, string[] lines, string reason)
        {
            var root = Root(parent, name, hangPoint, yaw, reason);

            foreach (float x in new[] { -size.x * 0.38f, size.x * 0.38f })
            {
                Plate(root.transform, $"Rod_{x:F2}", new Vector3(x, -drop * 0.5f, 0.0f),
                      new Vector3(0.03f, drop, 0.03f), DarkMetal);
            }

            var board = Plate(root.transform, "Face",
                              new Vector3(0.0f, -drop - size.y * 0.5f, 0.0f),
                              new Vector3(size.x, size.y, 0.045f), face);
            PaintLines(board.transform, lines, name, ink, Condensed);
            return root;
        }

        /// <summary>
        /// Letters straight on the render. No plate at all.
        ///
        /// ⚠️ THIS ONE IS THE MOST USEFUL OF THE ELEVEN AND IT IS THE ONE THAT LOOKS LIKE THE
        /// LEAST WORK. Every other system adds a rectangle to a street that already has too
        /// many rectangles; this adds a word and nothing else, so it breaks the row of boards
        /// without lengthening it. Sun-bleached ink on a facade is also the only signage that
        /// can be big without being loud.
        /// </summary>
        internal static GameObject PaintedWall(Transform parent, string name, Vector3 wallPoint,
                                               float yaw, Vector2 size, Color ink, string[] lines,
                                               string reason)
        {
            var root = Root(parent, name, wallPoint, yaw, reason);

            // An invisible carrier: the letters are laid out in a plate's local space, so the
            // plate has to exist. It is scaled to the lettering area and never rendered.
            var carrier = new GameObject("Area");
            carrier.transform.SetParent(root.transform, false);
            carrier.transform.localScale = new Vector3(size.x, size.y, 0.02f);

            PaintLines(carrier.transform, lines, name, ink, Mural);
            return root;
        }

        /// <summary>Tall narrow printed banner, letters stacked one per line.</summary>
        internal static GameObject VerticalBanner(Transform parent, string name, Vector3 wallPoint,
                                                  float yaw, Vector2 size, Color face, Color ink,
                                                  string word, string reason)
        {
            var root = Root(parent, name, wallPoint, yaw, reason);

            foreach (float y in new[] { size.y * 0.48f, -size.y * 0.48f })
            {
                Plate(root.transform, $"Band_{y:F2}", new Vector3(0.0f, y, 0.020f),
                      new Vector3(size.x + 0.06f, 0.07f, 0.06f), DarkMetal);
            }

            var board = Plate(root.transform, "Face", Vector3.zero,
                              new Vector3(size.x, size.y, 0.035f), face);

            var stacked = new string[word.Length];
            for (int i = 0; i < word.Length; i++) stacked[i] = word[i].ToString();

            PaintLines(board.transform, stacked, name, ink, Condensed, 0.86f);
            return root;
        }

        /// <summary>Hand-painted corrugated sheet nailed to a post frame, with rust running down.</summary>
        internal static GameObject TinSheet(Transform parent, string name, Vector3 groundPoint,
                                            float yaw, Vector2 size, float height, Color face,
                                            Color ink, string[] lines, string reason)
        {
            var root = Root(parent, name, groundPoint, yaw, reason);

            foreach (float x in new[] { -size.x * 0.42f, size.x * 0.42f })
            {
                Plate(root.transform, $"Post_{x:F2}", new Vector3(x, height * 0.5f, 0.04f),
                      new Vector3(0.07f, height, 0.07f), SignWood);
            }

            var board = Plate(root.transform, "Face", new Vector3(0.0f, height, 0.0f),
                              new Vector3(size.x, size.y, 0.03f), face);

            // Corrugation, as five shallow ribs. The sheet is what makes the sign cheap-looking
            // on purpose, and a flat quad cannot say that.
            //
            // ⚠⚠ THEY SIT ON THE SHEET, NOT IN FRONT OF THE LETTERING, AND THE FIRST VERSION
            // HAD THEM 13 mm PROUD. The board face ends at z = -0.015 and the painted letters
            // occupy -0.025 to -0.015, so ribs at -0.018 with a 0.02 depth spanned -0.028 to
            // -0.008 and swallowed the letters whole: five dark vertical planks interleaved
            // through every glyph. In `ilalim_pavement_east_v20.png` the word GOMA is four
            // unreadable slabs and it looks like the text is mirrored. Anything drawn on a sign
            // face has to stay BEHIND the ink.
            for (int i = 0; i < 5; i++)
            {
                Plate(root.transform, $"Rib_{i}",
                      new Vector3((i - 2) * size.x * 0.19f, height, -0.0158f),
                      new Vector3(size.x * 0.055f, size.y * 0.98f, 0.004f),
                      Color.Lerp(face, RustStreak, 0.22f));
            }

            // Rust runs down the sheet, and it is behind the ink for the same reason.
            foreach (float x in new[] { -size.x * 0.30f, size.x * 0.18f })
            {
                Plate(root.transform, $"Rust_{x:F2}",
                      new Vector3(x, height - size.y * 0.30f, -0.0162f),
                      new Vector3(0.06f, size.y * 0.44f, 0.004f), RustStreak);
            }

            PaintLines(board.transform, lines, name, ink, HandPainted);
            return root;
        }

        /// <summary>Free-standing letters on a parapet, carried by a visible truss.</summary>
        internal static GameObject RoofLetters(Transform parent, string name, Vector3 parapetPoint,
                                               float yaw, float width, float glyphHeight,
                                               Color ink, string word, string reason)
        {
            var root = Root(parent, name, parapetPoint, yaw, reason);

            float trussHeight = glyphHeight * 0.55f;
            Plate(root.transform, "TrussRail", new Vector3(0.0f, trussHeight * 0.5f, 0.0f),
                  new Vector3(width, 0.06f, 0.06f), DarkMetal);

            for (int i = -2; i <= 2; i++)
            {
                Plate(root.transform, $"TrussLeg_{i}",
                      new Vector3(i * width * 0.22f, trussHeight * 0.25f, 0.0f),
                      new Vector3(0.05f, trussHeight * 0.5f, 0.05f), DarkMetal);
            }

            var carrier = new GameObject("Area");
            carrier.transform.SetParent(root.transform, false);
            carrier.transform.localPosition = new Vector3(0.0f, trussHeight + glyphHeight * 0.5f, 0.0f);
            carrier.transform.localScale = new Vector3(width, glyphHeight, 0.22f);

            PaintLine(carrier.transform, word, 0.0f, 0.94f, name, ink, Display);
            return root;
        }
    }
}
