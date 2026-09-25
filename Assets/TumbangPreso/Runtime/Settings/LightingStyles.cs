using UnityEngine;

namespace TumbangPreso.Settings
{
    /// <summary>
    /// The lighting styles the Graphics tab offers as picture cards, and the one place that turns
    /// a stored index into the weight the world look is drawn at.
    ///
    /// ⚠️⚠️ THREE SLOTS, AND EACH ONE IS A LIGHTING THAT ALREADY EXISTS OR NOTHING. The owner
    /// asked for a style row like PUBG Mobile's (2026-09-25): slot 1 is the lighting on `main`,
    /// slot 2 is the bright PEAK-style look from `lighting/peak-bright-overhaul` (LIGHT-1), and
    /// slot 3 is a placeholder.
    ///
    ///   * CLASSIC IS WEIGHT 0, AND THAT IS MAIN'S LIGHTING EXACTLY, NOT AN APPROXIMATION OF IT.
    ///     `main` has no world look at all: every map draws its authored sun, ambient, fog and
    ///     sky. Those authored values are byte-identical on `main` and on this branch (checked
    ///     scene by scene on 2026-09-25: RenderSettings and the directional light), and weight 0
    ///     is the value every consumer of the look already treats as "the scene's own lighting":
    ///     `Visual.WorldLookPresentation` writes the recorded values back, the cast shader and
    ///     `Visual.WorldOutline` fall back to black ink, the grade skips the bright part.
    ///   * BRIGHT IS WEIGHT 1, the look as `Resources/WorldLookProfile.asset` authors it.
    ///   * THE THIRD SLOT IS NOT SELECTABLE. A card that could be picked and then did nothing, or
    ///     silently drew one of the other two, is § 6.3's dead end. It is shown so the row reads
    ///     as three, and it is refused by <see cref="Selectable"/> and by
    ///     <see cref="GameSettings.Validate"/> both.
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
            new Entry("Classic",     0f, "UI/lighting-styles/classic", true),
            new Entry("Bright",      1f, "UI/lighting-styles/bright",  true),
            new Entry("Coming soon", 0f, null,                         false),
        };

        /// <summary>Slot 1: the lighting on `main`, each map's authored rig.</summary>
        public const int Classic = 0;

        /// <summary>Slot 2: the bright look from LIGHT-1.</summary>
        public const int Bright = 1;

        /// <summary>Slot 3: a placeholder, not selectable.</summary>
        public const int Placeholder = 2;

        /// <summary>
        /// ⚠️⚠️ BRIGHT, BECAUSE THAT IS WHAT THIS BRANCH ALREADY DRAWS FOR EVERYBODY. Before this
        /// row existed the look ran at `WorldLighting` 1 with no way to turn it off, so a player
        /// who never opens the Graphics tab has to keep seeing it. `JsonUtility` gives a
        /// `settings.json` written before this field existed the field initialiser, so an upgrade
        /// lands here too, which is the change-nothing answer. <see cref="RenderStyles.Default"/>
        /// has the same argument for its own row 0.
        /// </summary>
        public const int Default = Bright;

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
