using UnityEngine;

namespace TumbangPreso.Settings
{
    /// <summary>
    /// The lighting styles the Graphics tab offers as picture cards, and the one place that turns
    /// a stored index into the weight the world look is drawn at.
    ///
    /// ⚠️⚠️ THREE SLOTS, AND EACH ONE IS A LIGHTING THAT ALREADY EXISTS OR NOTHING. The owner
    /// asked for a style row like PUBG Mobile's (2026-09-25). On 2026-09-26 the owner renamed and
    /// reordered it: the bright look is STANDARD, the default, in slot 1; `main`'s lighting is
    /// NOSTALGIC, in slot 2; slot 3 is a placeholder.
    ///
    ///   * STANDARD IS WEIGHT 1, the bright PEAK-style look (LIGHT-1) as
    ///     `Resources/WorldLookProfile.asset` authors it. It was called "Bright" until 2026-09-26.
    ///   * NOSTALGIC IS WEIGHT 0, AND THAT IS MAIN'S LIGHTING EXACTLY, NOT AN APPROXIMATION OF IT.
    ///     It was called "Classic" until 2026-09-26. `main` has no world look at all: every map
    ///     draws its authored sun, ambient, fog and sky. Those authored values are byte-identical
    ///     on `main` and on this branch (checked scene by scene on 2026-09-25: RenderSettings and
    ///     the directional light), and weight 0 is the value every consumer of the look already
    ///     treats as "the scene's own lighting": `Visual.WorldLookPresentation` writes the recorded
    ///     values back, the cast shader and `Visual.WorldOutline` fall back to black ink, the grade
    ///     skips the bright part.
    ///   * THE THIRD SLOT IS NOT SELECTABLE. A card that could be picked and then did nothing, or
    ///     silently drew one of the other two, is § 6.3's dead end. It is shown so the row reads
    ///     as three, and it is refused by <see cref="Selectable"/> and by
    ///     <see cref="GameSettings.Validate"/> both.
    ///
    /// ⚠️⚠️ THE SWAP MOVED THE STORED INDICES, SO THE STORED FIELD WAS RENAMED WITH IT.
    /// `GameSettings.LightingLook` holds this order; the old `GameSettings.LightingStyle` held the
    /// old one (0 Classic, 1 Bright) and <see cref="FromLegacy"/> carries a player's pick across.
    /// Reusing the old field would have silently swapped every saved choice.
    ///
    /// ⚠️ IT IS ONE INDEX, NOT A SLIDER, for the reason <see cref="RenderStyles"/> records: a
    /// style is a look with a name. A half-weight look is a state nobody designed.
    ///
    /// ⚠️⚠️ AND IT IS SEPARATE FROM <see cref="RenderStyles"/>, WHICH IS THE INK AND CHROMATIC
    /// EXPERIMENT. The two answer different questions (how light falls, how edges are drawn), and
    /// folding them into one table would make every pairing a row of its own.
    ///
    /// ⚠️ LOCAL ONLY, NEVER ON THE WIRE. Two peers on different styles see the same match in
    /// different light, which is correct for a graphics preference.
    /// </summary>
    public static class LightingStyles
    {
        public readonly struct Entry
        {
            /// <summary>What the card's caption shows.</summary>
            public readonly string Label;

            /// <summary>
            /// The world look's weight under this style, multiplied into
            /// `Visual.WorldCueProfile.WorldLighting` by `WorldCueProfile.LightingWeight`.
            /// </summary>
            public readonly float LookWeight;

            /// <summary>
            /// The card's picture, a Resources path. Rendered in engine by
            /// `WorldCourtCueTests.LightingStyleThumbnails` from ONE camera on one map, so the only
            /// difference between two cards is the lighting. Null draws an empty slot.
            /// </summary>
            public readonly string Thumbnail;

            /// <summary>False for the placeholder slot. See <see cref="Selectable"/>.</summary>
            public readonly bool Available;

            public Entry(string label, float lookWeight, string thumbnail, bool available)
            {
                Label = label;
                LookWeight = lookWeight;
                Thumbnail = thumbnail;
                Available = available;
            }
        }

        /// <summary>Ordered for display. The Graphics tab draws one card per row, in this order.</summary>
        public static readonly Entry[] All =
        {
            new Entry("Standard",    1f, "UI/lighting-styles/standard",  true),
            new Entry("Nostalgic",   0f, "UI/lighting-styles/nostalgic", true),
            new Entry("Coming soon", 0f, null,                           false),
        };

        /// <summary>Slot 1: the bright look from LIGHT-1, the default. Was "Bright".</summary>
        public const int Standard = 0;

        /// <summary>Slot 2: the lighting on `main`, each map's authored rig. Was "Classic".</summary>
        public const int Nostalgic = 1;

        /// <summary>Slot 3: a placeholder, not selectable.</summary>
        public const int Placeholder = 2;

        /// <summary>
        /// ⚠️⚠️ STANDARD, the bright look, on the owner's instruction (2026-09-26), and also what
        /// this branch drew for everybody before the row existed. `JsonUtility` gives a
        /// `settings.json` written before `GameSettings.LightingLook` existed the field
        /// initialiser, so a fresh or upgraded file lands here unless <see cref="FromLegacy"/>
        /// finds an older pick. <see cref="RenderStyles.Default"/> has the same argument for its own row 0.
        /// </summary>
        public const int Default = Standard;

        /// <summary>
        /// The live multiplier on the world look's weight.
        ///
        /// ⚠️ A STATIC PUSHED BY <see cref="Apply"/>, NEVER A READ OF `SettingsStore.Current`, for
        /// <see cref="RenderStyles.InkOutlinesActive"/>'s reason: the look reads it inside
        /// camera callbacks and every Update, and the first touch of `SettingsStore.Current` loads
        /// and validates the whole settings file. Seeded from <see cref="Default"/> so an editor
        /// test that never boots settings draws the shipped look.
        /// </summary>
        public static float LookWeight { get; private set; } = All[Default].LookWeight;

        public static Entry Of(int index) => All[Mathf.Clamp(index, 0, All.Length - 1)];

        public static bool Selectable(int index) => index >= 0 && index < All.Length && All[index].Available;

        /// <summary>
        /// A pick stored in the pre-2026-09-26 order (0 Classic, 1 Bright) in this order, or -1 when
        /// nothing was stored. Anything else stored there lands on the default.
        /// </summary>
        public static int FromLegacy(int legacy) => legacy < 0 ? -1 : legacy == 0 ? Nostalgic : Default;

        /// <summary>A stored index made safe: in the table and pickable, otherwise the default.</summary>
        public static int Normalize(int index) => Selectable(index) ? index : Default;

        /// <summary>
        /// Push a stored index at the look. ⚠️ Nothing is rebuilt: `Visual.WorldLookPresentation`
        /// compares its applied weight with `WorldCueProfile.LightingWeight` every Update and
        /// re-applies on a change, which is what makes a card change the picture behind the
        /// pause menu while the player is looking at it. Safe on a server: it stores a number.
        /// </summary>
        public static void Apply(int index) => LookWeight = Mathf.Clamp01(Of(Normalize(index)).LookWeight);
    }
}
