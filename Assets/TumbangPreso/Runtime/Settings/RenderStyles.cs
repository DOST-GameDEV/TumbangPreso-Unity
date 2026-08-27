using UnityEngine;

namespace TumbangPreso.Settings
{
    /// <summary>
    /// The two looks the game can be drawn in, and the one place that turns a stored index into
    /// the three switches that implement them.
    ///
    /// ⚠️⚠️ THIS IS AN EXPERIMENT WITH A DEFAULT THAT IS NOT ALLOWED TO MOVE. 🧑 wants to compare
    /// the shipped ink look against a softer post-processed one with visible colour fringing, and
    /// wants to flip between them from the settings panel while judging. So row 0 is TOON, it is
    /// <see cref="Default"/>, and every switch below reads as a NO-OP on it: a player who never
    /// opens settings renders exactly the frame this branch inherited. The Chromatic row is the
    /// only thing that changes anything, and it is opt-in.
    ///
    /// ⚠️ IT IS ONE INDEX RATHER THAN THREE BOOLEANS, for the reason <see cref="SlipperHighlights"/>
    /// and <see cref="AntiAliasModes"/> both record at length: a second flag creates states that
    /// have to be stored, styled and explained (ink outlines AND a chromatic split, or neither and
    /// nothing), and one clamp over one list cannot represent them. A style is a look with a name,
    /// not a set of independently switchable effects.
    ///
    /// ⚠️⚠️ ALL THREE SWITCHES ARE READ LIVE, EVERY FRAME, BY WHATEVER IMPLEMENTS THEM, exactly as
    /// <see cref="AntiAliasModes.FxaaActive"/> is read inside `Visual.PostAntiAlias.OnRenderImage`.
    /// That is what makes the pick change the picture while the player is looking at it, which the
    /// panel is reachable from the in-match pause menu specifically so they can do. Nothing here
    /// rebuilds a material, re-dresses a renderer or swaps a shader:
    ///
    ///   * the hull outline is suppressed by a GLOBAL shader float, `_OutlineSuppress`, which
    ///     `Toon.shader`'s OUTLINE pass multiplies its width by. See
    ///     <see cref="Visual.ToonSkin.SetOutlinesSuppressed"/> for why it is a global and not a
    ///     material property and not a second shader.
    ///   * the screen-space world outline is gated inside `Visual.WorldOutline.Live`.
    ///   * the persistent colour fringe is read by `Visual.ColourGrade.OnRenderImage` and ADDED to
    ///     the transient impact pulse rather than replacing it. See <see cref="Chromatic"/>.
    /// </summary>
    public static class RenderStyles
    {
        public readonly struct Entry
        {
            /// <summary>What the settings row shows.</summary>
            public readonly string Label;

            /// <summary>
            /// Whether the ink edges draw: `Toon.shader`'s inverted-hull OUTLINE pass on the cast
            /// and the props, AND the screen-space `Visual.WorldOutline` on the street. The two
            /// are one switch because they are one look. Turning off the hull and leaving the
            /// world edge on would ink the buildings and not the people standing in front of
            /// them, which is neither of the styles anybody asked for.
            /// </summary>
            public readonly bool InkOutlines;

            /// <summary>
            /// The persistent RGB split, in the same 0 to 1 units `ColourGrade.PulseChromatic`
            /// takes. 0 is off and is an exact no-op.
            /// </summary>
            public readonly float Chromatic;

            /// <summary>
            /// Whether the split scales radially from the frame centre instead of being a flat
            /// horizontal offset. See <see cref="Chromatic"/> for the arithmetic.
            /// </summary>
            public readonly bool RadialSplit;

            public Entry(string label, bool inkOutlines, float chromatic, bool radialSplit)
            {
                Label = label;
                InkOutlines = inkOutlines;
                Chromatic = chromatic;
                RadialSplit = radialSplit;
            }
        }

        /// <summary>
        /// Ordered for display, shipped look first. The settings row iterates this directly, so a
        /// style added here appears in the panel with no second edit.
        /// </summary>
        public static readonly Entry[] All =
        {
            new Entry("Toon (Ink Outlines)", true,  0.00f, false),
            new Entry("Chromatic",           false, 0.34f, true),
        };

        /// <summary>Row 0. Today's look, unchanged.</summary>
        public const int Toon = 0;

        /// <summary>Row 1. No ink, persistent radial colour fringing.</summary>
        public const int Chromatic = 1;

        /// <summary>
        /// ⚠️⚠️ TOON, AND IT IS NOT A PREFERENCE. This is a prototype of an alternative look on a
        /// branch whose whole point is that the two can be compared, and `Visual.WorldOutline`
        /// carries the matching rule for the same reason: *"a prototype that quietly becomes the
        /// look because it happened to be enabled ... is the failure mode to guard against"*. A
        /// player who never opens the settings panel must see the frame this branch inherited.
        ///
        /// ⚠️ AND IT IS 0 ON PURPOSE, WHICH IS THE ONE CASE WHERE THAT IS SAFE. Every other index
        /// setting in <see cref="GameSettings"/> deliberately does NOT default to its row 0
        /// (<see cref="AntiAliasModes.Default"/> is 3, <see cref="SlipperHighlights.Default"/> is
        /// not Off), because `JsonUtility` fills an absent field with the field initialiser and a
        /// silent 0 would turn a feature off for everybody upgrading. Here 0 IS the shipped
        /// behaviour, so an older `settings.json` with no `RenderStyle` line lands on exactly what
        /// that build was already drawing. `LobbyAndSettingsTests` asserts that rather than
        /// trusting it, because the day a third style is inserted at the top is the day it stops
        /// being true.
        /// </summary>
        public const int Default = Toon;

        /// <summary>
        /// Whether the ink edges draw this session.
        ///
        /// ⚠️ A STATIC RATHER THAN A READ OF `SettingsStore.Current` PER FRAME, for the reason
        /// <see cref="AntiAliasModes.FxaaActive"/> records: `Visual.WorldOutline` asks this
        /// question inside `OnRenderImage` and again in `LateUpdate`, and `SettingsStore.Current`
        /// loads and validates the whole settings file the first time it is touched. A render
        /// callback is not where that should ever be able to happen.
        ///
        /// ⚠️ SEEDED FROM <see cref="Default"/> RATHER THAN FROM `true`, so the seed cannot drift
        /// from row 0 if the table is ever reordered.
        /// </summary>
        public static bool InkOutlinesActive { get; private set; } = All[Default].InkOutlines;

        /// <summary>
        /// The always-on half of the colour split, in the units `ColourGrade.PulseChromatic` takes.
        ///
        /// ⚠️⚠️ `ColourGrade` ADDS THIS TO THE IMPACT PULSE, IT DOES NOT PICK ONE OF THE TWO, and
        /// that is the difference between a hit reading as a hit and a hit reading as the style
        /// switching off for a second. Worked through against the real peaks:
        /// `Visual.HitFeel.ChromaticPeak` is 0.10 / 0.22 / 0.35 / 0.55 by hit weight and
        /// `HeroAbilitySystem` pulses an ultimate at 0.95.
        ///
        ///   * Under a MAX, the base of 0.25 SWALLOWS the two lighter hits outright: a light tag
        ///     during Chromatic mode would produce max(0.25, 0.10) = 0.25, which is the frame not
        ///     moving at all. The feedback that exists to tell you that you were hit would fire and
        ///     show nothing.
        ///   * Under base PLUS impact, every hit moves the frame by its own full peak whatever the
        ///     base is, so the impact effect keeps the exact amplitude it was tuned at. 0.25 + 0.55
        ///     is 0.80 and still inside the range; only an ultimate (0.25 + 0.95) saturates, and an
        ///     ultimate at 0.95 was already within five per cent of the top.
        ///
        /// So it is a sum, saturated at 1.
        ///
        /// ⚠️ 0.25 IS SOLVED FOR, NOT PICKED. The shader's split constant is 0.006 in UV, so a flat
        /// horizontal split at amount `a` is `a * 0.006` of the frame width, which is 11.5 px at
        /// 1920. Under the radial profile below the offset reaches that same 0.006 at the left and
        /// right EDGES and about 0.0085 at the corners, and is zero at the centre. At 0.25 that is
        /// about 2.9 px of fringe at the edges and 4.1 px at the corners of a 1920-wide frame:
        /// clearly visible as colour fringing, and well short of the 6.6 px a light hit's 0.55 peak
        /// already puts across the whole frame, so the style never out-shouts the gameplay
        /// feedback layered on top of it.
        ///
        /// ⚠️ IT IS A NUMBER IN A TABLE RATHER THAN A SLIDER because this is an A/B between two
        /// looks, not a strength control. If 0.25 turns out to be the wrong amount, the fix is to
        /// change this one number and re-render, not to hand the player a dial for a prototype.
        /// </summary>
        public static float PersistentChromatic { get; private set; } = All[Default].Chromatic;

        /// <summary>
        /// Whether the split is radial rather than flat horizontal.
        ///
        /// ⚠️⚠️ THE SHIPPED SPLIT IS HORIZONTAL AND REAL CHROMATIC ABERRATION IS NOT. A lens
        /// disperses by refraction, so the fringe is zero on the optical axis and grows toward the
        /// edge of the image circle; a constant horizontal offset is a VHS artefact, which is a
        /// fine thing for a 0.4 s impact flash and the wrong thing to look at for a whole match.
        /// A constant offset also fringes text, the HUD centre and the crosshair, which is exactly
        /// where a player is trying to read.
        ///
        /// ⚠️ AND IT IS PER STYLE RATHER THAN GLOBAL, SO THE IMPACT PATH IS UNTOUCHED ON TOON. Row
        /// 0 sets this false, `ColourGrade` writes `_ChromaticRadial` 0, and the shader takes the
        /// same `half2(a * 0.006, 0)` branch it has always taken. The impact pulse in the shipped
        /// look is therefore byte-identical after this change. In Chromatic mode BOTH the base and
        /// the pulse go radial, which is correct: they are the same lens.
        /// </summary>
        public static bool RadialSplit { get; private set; } = All[Default].RadialSplit;

        public static Entry Of(int index) => All[Mathf.Clamp(index, 0, All.Length - 1)];

        public static string LabelOf(int index) => Of(index).Label;

        /// <summary>
        /// Push a stored index at the three things that implement it.
        ///
        /// ⚠️ IT IS SAFE TO CALL ON A SERVER, the same way <see cref="AntiAliasModes.Apply"/> is.
        /// Two of the three switches are plain statics nothing reads on a headless build, and
        /// `Shader.SetGlobalFloat` on a build with no graphics device stores a number that no draw
        /// call ever reads. So this needs no batch-mode guard the way `GameSettings.ApplyDisplay`
        /// does, and `GameSettings.Apply` can call it unconditionally.
        /// </summary>
        public static void Apply(int index)
        {
            var entry = Of(index);

            InkOutlinesActive = entry.InkOutlines;
            PersistentChromatic = Mathf.Clamp01(entry.Chromatic);
            RadialSplit = entry.RadialSplit;

            Visual.ToonSkin.SetOutlinesSuppressed(!entry.InkOutlines);
        }
    }
}
