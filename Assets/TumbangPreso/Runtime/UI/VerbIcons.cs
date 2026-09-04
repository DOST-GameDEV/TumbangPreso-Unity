using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// What a VERB looks like on a thumb control.
    ///
    /// ⚠️⚠️ 🧑 2026-09-03, WITH A SCREENSHOT OF THE ANDROID BUILD: *"why the fuck does it have
    /// keybinds theres no keys in mobile"*, and *"ive never seen a mobile game say GRAB or lunge,
    /// usually it has an intuitive icon for it or the skill icon"*. He was looking at a phone
    /// drawing **Q** and **E** on two of its buttons: not merely words instead of pictures, but
    /// the literal names of two keys on a keyboard the device does not have, on the one surface
    /// in the game that exists because there is no keyboard.
    ///
    /// ⚠️⚠️ THE CAUSE WAS ONE FIELD AND IT WAS DOING TWO JOBS. `InputCatalogue.VerbInput` carried
    /// a single `TouchLabel` string, and `TouchHud.BuildButton` drew it with `MenuKit.Label`. A
    /// string is the only kind of answer that table could hold, so whoever filled it in wrote
    /// what the control was CALLED, and for the three Hero Strike slots the thing they were
    /// called was their keyboard key. **A field whose type cannot express a picture will be
    /// filled with a word every time**, which is the same argument `HeroAbility.Glyph`'s header
    /// makes about lookup tables: *"a lookup table keyed by id is a second place to forget, and
    /// forgetting it compiles."*
    ///
    /// ⚠️⚠️ SO THE GLYPH IS A CONSTRUCTOR PARAMETER WITH NO DEFAULT, exactly like every other
    /// field of `VerbInput`. A new verb cannot reach a phone without somebody deciding what it
    /// looks like, because `InputCatalogue.For` is a switch expression with no discard arm and
    /// `csc.rsp` turns the resulting CS8509 into an error. That is `CLAUDE.md` § 4a's whole
    /// method: *"the answer is construction, not discipline."*
    ///
    /// ⚠️ THE WORD SURVIVES, IT JUST STOPS BEING THE BUTTON. `VerbInput.TouchLabel` still names
    /// each control for the layout customiser (`TouchLayoutScreen`), where a player is dragging
    /// controls around and needs to know which one they have hold of, and for the settings panel.
    /// A picture is right on a control you press in a fight and wrong on a row you are editing.
    ///
    /// ⚠️⚠️ AND THE THREE HERO SLOTS DRAW THE ABILITY'S OWN ICON, NOT ANYTHING IN THIS FILE.
    /// `TouchHud` resolves the live kit and asks `AbilityIcons.For(kit.Skill1.Glyph)`, so a phone
    /// shows the same eighteen bespoke ability pictures the deck, the tray and character select
    /// already show. `docs/VISION.md` § 3: *"a player must be able to understand a power by
    /// looking at it"*, and the three layers *"must stay in step"*. A fourth surface drawing its
    /// own private symbols for the same three powers would be the drift that rule exists about.
    /// The glyphs below are the FALLBACK for a seat with no kit, which is Classic, where the rail
    /// is hidden anyway.
    ///
    /// ⚠️ BAKED IN CODE LIKE EVERY OTHER SURFACE IN THIS UI, and for `AbilityIcons`' recorded
    /// reason: *"a baked file that drifts from the code that wanted it is indistinguishable from
    /// a broken conversion."* White on transparent, tinted at the use site.
    /// </summary>
    public enum VerbGlyph
    {
        /// <summary>
        /// SPRINT. Three swept speed lines.
        ///
        /// ⚠️ NOT A RUNNING FIGURE. A human silhouette needs about forty pixels of height before
        /// its legs read as legs, and this is drawn inside the SMALLEST control on the layer
        /// (`TouchMetrics.MinTargetUnits`, 144 units, and the icon is a fraction of that). Speed
        /// lines survive being small, which is why every mobile game that has a sprint button
        /// uses them.
        /// </summary>
        Sprint,

        /// <summary>
        /// THROW. A tsinelas on a rising arc.
        ///
        /// ⚠️⚠️ IT IS THE SLIPPER AND NOT A GENERIC ARROW, because this is the one verb the
        /// entire game is named after and the only one that is HELD. `docs/VISION.md` § 0: the
        /// whole sport is throwing your tsinelas at a can and then going back for it. A player
        /// who has seen the character select screen already knows this silhouette.
        /// </summary>
        ThrowSlipper,

        /// <summary>
        /// GRAB, and everything else that one control does. An open hand.
        ///
        /// ⚠️⚠️ ONE PICTURE FOR THREE JOBS, WHICH IS CORRECT RATHER THAN LAZY.
        /// `InputCatalogue`'s note: *"tap picks up, tap with nothing in reach shoves, hold as the
        /// taya runs the lata reset ... one key, one action, several jobs decided by the world.
        /// A second touch button per job would be three controls for one verb."* The icon obeys
        /// the same rule the binding does: an open hand is reach, and reaching is what all three
        /// jobs are.
        /// </summary>
        Hand,

        /// <summary>JUMP. A chevron lifting off a ground line.</summary>
        Jump,

        /// <summary>
        /// LUNGE. A committed forward thrust with a swept tail.
        ///
        /// ⚠️ DELIBERATELY NOT THE SAME FAMILY AS <see cref="Sprint"/>, even though both are
        /// about moving fast. `AbilityGlyph`'s rule applies here too: *"a wrong icon is worse
        /// than a generic one, because the player trusts it once and then stops trusting all of
        /// them."* A sprint is a state you hold; a lunge is the taya's committed scoring verb
        /// with a charge and a cooldown, and confusing the two is confusing the two most
        /// important buttons the defending player has.
        /// </summary>
        Lunge,

        /// <summary>EMOTE. A face.</summary>
        Emote,

        // ---- fallbacks for a seat with no hero kit ---------------------------------
        //
        // ⚠️ THE SKILL RAIL PREFERS THE ABILITY'S OWN ICON AND THESE ARE WHAT IT FALLS BACK TO.
        // In Classic the rail is hidden entirely, so in practice these are what a Hero Strike
        // seat draws for the one frame between the layer being built and the kit arriving.

        /// <summary>The first skill slot: a chevron inside a plate.</summary>
        SkillPrimary,

        /// <summary>The second skill slot: a double chevron inside a plate.</summary>
        SkillSecondary,

        /// <summary>The ultimate: a star.</summary>
        Ultimate,
    }

    /// <summary>
    /// Procedural icons for the touch layer, baked the same way <see cref="AbilityIcons"/> bakes
    /// the ability set: signed-distance coverage in a -1..1 square, white on transparent, one
    /// texture per glyph for the life of the process.
    ///
    /// ⚠️ IT IS A SEPARATE CLASS FROM `AbilityIcons` AND NOT A FEW MORE ENUM MEMBERS ON IT. An
    /// `AbilityGlyph` answers *"what does this power do to the world"* and every one of them is
    /// drawn on an ability card somewhere; a `VerbGlyph` answers *"what does this button do to my
    /// body"*, and there is no card. Folding them together would put JUMP in the vocabulary
    /// `AbilityIcons.LabelFor` reports jobs from, and `HeroPresentationTests` asserts that set is
    /// unique per ability.
    /// </summary>
    public static class VerbIcons
    {
        /// <summary>
        /// ⚠️ 128, MATCHING `AbilityIcons`. The largest touch control is
        /// `TouchMetrics.UnitsFor(Large)` = 223 canvas units and the icon is drawn inside a
        /// fraction of that, so 128 is already above the sampling rate at every phone resolution
        /// the probe drives. Doubling it would double the bake cost of nine textures at boot to
        /// buy nothing anybody can see.
        /// </summary>
        private const int Size = 128;

        private const float Stroke = 0.17f;

        private static readonly Dictionary<VerbGlyph, Sprite> Cache =
            new Dictionary<VerbGlyph, Sprite>();

        public static Sprite For(VerbGlyph glyph)
        {
            if (Cache.TryGetValue(glyph, out var cached) && cached != null) return cached;

            var sprite = Bake(glyph);
            Cache[glyph] = sprite;
            return sprite;
        }

        /// <summary>
        /// A one-word name for the glyph, for the layout customiser and for a test that wants to
        /// say which control it is talking about.
        ///
        /// ⚠️ IT IS NOT WHAT THE BUTTON SAYS. Nothing draws this on the touch layer; that is the
        /// entire point of the file.
        /// </summary>
        public static string DescribeFor(VerbGlyph glyph)
        {
            switch (glyph)
            {
                case VerbGlyph.Sprint: return "SPEED LINES";
                case VerbGlyph.ThrowSlipper: return "TSINELAS ON AN ARC";
                case VerbGlyph.Hand: return "OPEN HAND";
                case VerbGlyph.Jump: return "LIFT OFF A LINE";
                case VerbGlyph.Lunge: return "FORWARD THRUST";
                case VerbGlyph.Emote: return "FACE";
                case VerbGlyph.SkillPrimary: return "SKILL PLATE";
                case VerbGlyph.SkillSecondary: return "SKILL PLATE";
                default: return "STAR";
            }
        }

        // ------------------------------------------------------------------ baking

        private static Sprite Bake(VerbGlyph glyph)
        {
            var pixels = new Color[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = (x + 0.5f) / Size * 2.0f - 1.0f;
                    float v = (y + 0.5f) / Size * 2.0f - 1.0f;

                    pixels[y * Size + x] =
                        new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp01(Coverage(glyph, u, v)));
                }
            }

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "verbglyph_" + glyph,
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

        private static float Coverage(VerbGlyph glyph, float u, float v)
        {
            switch (glyph)
            {
                // Three swept lines, longest in the middle, tapering back. Reads as motion at
                // any size because the only information in it is direction and repetition.
                case VerbGlyph.Sprint:
                    return Mathf.Max(
                        Segment(u, v, -0.62f, 0.44f, 0.52f, 0.44f, Stroke * 0.62f),
                        Mathf.Max(
                            Segment(u, v, -0.82f, 0.00f, 0.72f, 0.00f, Stroke * 0.72f),
                            Segment(u, v, -0.52f, -0.44f, 0.42f, -0.44f, Stroke * 0.62f)));

                // A tsinelas seen from above (a rounded sole with a thong) climbing an arc.
                case VerbGlyph.ThrowSlipper:
                    return Mathf.Max(Slipper(u, v), ThrowArc(u, v));

                case VerbGlyph.Hand:
                    return OpenHand(u, v);

                // A chevron pointing up, over the line it is leaving.
                case VerbGlyph.Jump:
                    return Mathf.Max(
                        Mathf.Max(
                            Segment(u, v, -0.52f, 0.10f, 0.0f, 0.68f, Stroke * 0.9f),
                            Segment(u, v, 0.0f, 0.68f, 0.52f, 0.10f, Stroke * 0.9f)),
                        Mathf.Max(
                            Segment(u, v, -0.34f, -0.30f, 0.34f, -0.30f, Stroke * 0.55f),
                            Segment(u, v, -0.62f, -0.68f, 0.62f, -0.68f, Stroke * 0.75f)));

                // One committed spear to the right, with a swept tail behind it.
                case VerbGlyph.Lunge:
                    return Mathf.Max(
                        Mathf.Max(
                            Segment(u, v, -0.56f, 0.0f, 0.44f, 0.0f, Stroke * 0.80f),
                            Mathf.Max(
                                Segment(u, v, 0.10f, 0.46f, 0.76f, 0.0f, Stroke * 0.86f),
                                Segment(u, v, 0.76f, 0.0f, 0.10f, -0.46f, Stroke * 0.86f))),
                        Mathf.Max(
                            Segment(u, v, -0.86f, 0.30f, -0.44f, 0.30f, Stroke * 0.42f),
                            Segment(u, v, -0.86f, -0.30f, -0.44f, -0.30f, Stroke * 0.42f)));

                case VerbGlyph.Emote:
                    return Face(u, v);

                case VerbGlyph.SkillPrimary:
                    return Mathf.Max(Plate(u, v),
                                     Chevron(u + 0.06f, v, 0.42f, Stroke * 0.95f));

                case VerbGlyph.SkillSecondary:
                    return Mathf.Max(Plate(u, v),
                                     Mathf.Max(Chevron(u + 0.24f, v, 0.42f, Stroke * 0.85f),
                                               Chevron(u - 0.10f, v, 0.42f, Stroke * 0.85f)));

                default:
                    return Star(u, v, 0.78f, 0.33f, 5);
            }
        }

        // ------------------------------------------------------------------ shapes

        /// <summary>
        /// A tsinelas from above: a rounded sole, narrowed at the arch, with the V of the thong.
        ///
        /// ⚠️⚠️ THE FIRST VERSION OF THIS READ AS A BLOB AND THE EXPORT SHEET IS WHAT SHOWED IT.
        /// It stacked three overlapping ellipses at 0.20 to 0.28 of the square and then drew four
        /// trajectory beads THROUGH the same space; at the size this is actually seen the beads
        /// merged into the sole and the whole thing became one lumpy mass. That matters more here
        /// than on any other control: THROW is `TouchSize.Large`, the one verb that is HELD, and
        /// the button a thumb rests on for a whole match.
        ///
        /// ⚠️ THE FIX WAS FEWER, BIGGER SHAPES AND A CLEAR GAP. The sole is now one long body
        /// with a waist pinched into it rather than three circles; it sits in the LOWER LEFT and
        /// the arc sweeps clear above it, so the two never overlap and each reads on its own.
        /// `CLAUDE.md` § 6.5's argument for shape over fill applies to a 128 px glyph as much as
        /// to a menu plate: a silhouette that survives being small is the whole job.
        /// </summary>
        private static float Slipper(float u, float v)
        {
            // ⚠️⚠️ TILTED 32 DEGREES AND PUSHED RIGHT, WITH THE TRAIL BEHIND IT. A tsinelas lying
            // flat reads as a footprint; one on a diagonal with a trail reads as one that has
            // been thrown, which is the verb rather than the object.
            const float cos = 0.85f, sin = 0.53f;
            float x = (u - 0.18f) * cos + (v - 0.10f) * sin;
            float y = -(u - 0.18f) * sin + (v - 0.10f) * cos;

            // ⚠️⚠️ ONE LONG OVAL IS THE WHOLE SOLE, AND THE TWO EARLIER ATTEMPTS FAILED BY ADDING
            // TO IT. The first stacked three ellipses and produced a caterpillar; the second
            // subtracted a waist out of both edges and ate the middle of the shape. **A real
            // tsinelas seen from above is an oval**, and at 128 px an oval with a thong on it is
            // unambiguous while an anatomically correct arch is four grey pixels.
            float body = EllipseDisc(x, y, 0.30f, 0.56f);

            // A little wider at the toe, which is the one asymmetry that survives the size.
            body = Mathf.Max(body, EllipseDisc(x, y - 0.30f, 0.32f, 0.20f));

            // ⚠️⚠️ THE THONG IS CUT OUT OF THE SOLE, NOT DRAWN ON TOP OF IT, AND THE FIRST
            // VERSION GOT THAT BACKWARDS. These glyphs are solid white on transparent, so a
            // white strap drawn over a white sole with `Max` is invisible by definition: the
            // export sheet showed a plain oval and the strap was doing nothing at all.
            // **Subtracting it is how every flip-flop icon in the world is drawn**: the strap
            // reads as the gap, in whatever colour is behind the glyph.
            float strapA = Segment(x, y, 0.0f, -0.32f, -0.20f, 0.08f, Stroke * 0.46f);
            float strapB = Segment(x, y, 0.0f, -0.32f, 0.20f, 0.08f, Stroke * 0.46f);

            return Sub(body, Mathf.Max(strapA, strapB));
        }

        /// <summary>
        /// The speed trail behind a thrown tsinelas.
        ///
        /// ⚠️⚠️ A TRAIL, NOT A TRAJECTORY ARC, AND THE EXPORT SHEET IS WHY. Two earlier versions
        /// drew the flight path as beads climbing across the square: at the size this is seen the
        /// beads collided with the sole and the whole glyph became one lumpy mass, and an
        /// arrowhead added on the end read as a separate broken shape. **Three dashes behind the
        /// object is the oldest and most legible way to draw motion**, it occupies the corner the
        /// slipper does not, and it cannot merge with anything because it is straight lines
        /// against a curve.
        /// </summary>
        private static float ThrowArc(float u, float v)
        {
            float a = Segment(u, v, -0.86f, -0.30f, -0.46f, -0.30f, Stroke * 0.50f);
            float b = Segment(u, v, -0.78f, -0.58f, -0.30f, -0.58f, Stroke * 0.50f);
            float c = Segment(u, v, -0.62f, -0.02f, -0.34f, -0.02f, Stroke * 0.44f);

            return Mathf.Max(a, Mathf.Max(b, c));
        }

        /// <summary>
        /// An open hand: a palm with four fingers and a thumb.
        ///
        /// ⚠️ FOUR FINGERS AND A GAP, NOT FIVE EVENLY SPACED CAPSULES. The gap between the index
        /// and the thumb is the only thing that makes a hand read as a hand at this size; five
        /// even bars read as a comb.
        /// </summary>
        private static float OpenHand(float u, float v)
        {
            float palm = Mathf.Max(EllipseDisc(u, v + 0.30f, 0.40f, 0.34f),
                                   Box(u, v + 0.36f, 0.38f, 0.26f));

            float fingers = 0.0f;
            for (int i = 0; i < 4; i++)
            {
                float x = -0.27f + i * 0.18f;
                float top = 0.46f - Mathf.Abs(i - 1.2f) * 0.09f;
                fingers = Mathf.Max(fingers, Segment(u, v, x, -0.06f, x, top, 0.075f));
            }

            float thumb = Segment(u, v, -0.30f, -0.24f, -0.66f, 0.10f, 0.085f);

            return Mathf.Max(palm, Mathf.Max(fingers, thumb));
        }

        /// <summary>A face: a ring, two eyes and a smile.</summary>
        private static float Face(float u, float v)
        {
            float head = Ring(u, v, 0.74f, Stroke * 0.80f);
            float eyeL = Disc(u + 0.26f, v - 0.22f, 0.095f);
            float eyeR = Disc(u - 0.26f, v - 0.22f, 0.095f);

            // The smile is the lower half of a ring, cut off above the centre line.
            float smile = v < -0.02f ? Ring(u, v + 0.06f, 0.40f, Stroke * 0.62f) : 0.0f;

            return Mathf.Max(head, Mathf.Max(eyeL, Mathf.Max(eyeR, smile)));
        }

        /// <summary>The rounded plate the two fallback skill glyphs sit inside.</summary>
        private static float Plate(float u, float v)
        {
            float outer = EllipseDisc(u, v, 0.86f, 0.86f);
            float inner = EllipseDisc(u, v, 0.66f, 0.66f);
            return Sub(outer, inner);
        }

        private static float Star(float u, float v, float outer, float inner, int points)
        {
            float best = 0.0f;

            for (int i = 0; i < points; i++)
            {
                float a = Mathf.PI * 0.5f + i * Mathf.PI * 2.0f / points;
                float bx = Mathf.Cos(a) * outer, by = Mathf.Sin(a) * outer;

                float a2 = a + Mathf.PI / points;
                float cx = Mathf.Cos(a2) * inner, cy = Mathf.Sin(a2) * inner;

                float a3 = a - Mathf.PI / points;
                float dx = Mathf.Cos(a3) * inner, dy = Mathf.Sin(a3) * inner;

                best = Mathf.Max(best,
                    Mathf.Max(Segment(u, v, bx, by, cx, cy, 0.085f),
                              Segment(u, v, bx, by, dx, dy, 0.085f)));
            }

            return Mathf.Max(best, Disc(u, v, inner * 0.72f));
        }

        // ------------------------------------------------------------------ primitives
        //
        // ⚠️ TRANSCRIBED FROM `AbilityIcons` RATHER THAN SHARED WITH IT, AND THAT IS A DELIBERATE
        // DUPLICATION OF ABOUT THIRTY LINES. Those helpers are `private` there and its `Size` and
        // `Stroke` are its own; exporting them would make one file's feathering constant part of
        // another file's contract, and the next person to retune an ability icon would silently
        // retune every thumb control with it. Four arithmetic functions is the cheaper copy.

        private static float Edge(float distance)
        {
            const float feather = 2.5f / Size;
            return Mathf.Clamp01(0.5f - distance / feather);
        }

        private static float Sub(float shape, float hole) => Mathf.Clamp01(shape - hole);

        private static float Disc(float u, float v, float r)
            => Edge(Mathf.Sqrt(u * u + v * v) - r);

        private static float Ring(float u, float v, float r, float thickness)
            => Edge(Mathf.Abs(Mathf.Sqrt(u * u + v * v) - r) - thickness * 0.5f);

        private static float EllipseDisc(float u, float v, float rx, float ry)
        {
            float d = Mathf.Sqrt((u / rx) * (u / rx) + (v / ry) * (v / ry)) - 1.0f;
            return Edge(d * ry);
        }

        private static float Box(float u, float v, float halfW, float halfH)
        {
            float dx = Mathf.Abs(u) - halfW;
            float dy = Mathf.Abs(v) - halfH;
            return Edge(Mathf.Max(dx, dy));
        }

        private static float Segment(float u, float v, float ax, float ay, float bx, float by,
                                     float halfThickness)
        {
            float pax = u - ax, pay = v - ay;
            float bax = bx - ax, bay = by - ay;

            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;

            return Edge(Mathf.Sqrt(dx * dx + dy * dy) - halfThickness);
        }

        private static float Chevron(float u, float v, float halfSpan, float thickness)
        {
            if (Mathf.Abs(v) > halfSpan) return 0.0f;

            float leg = u + Mathf.Abs(v) * 0.9f;
            return Edge(Mathf.Abs(leg) - thickness * 0.5f);
        }
    }
}
