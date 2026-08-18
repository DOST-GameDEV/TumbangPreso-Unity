using UnityEngine;

namespace TumbangPreso.Settings
{
    /// <summary>
    /// The colour palette for § THE LANDED HIGHLIGHT, from `settings_manager.gd`.
    ///
    /// ⚠️⚠️ IT LIVES HERE AND NOT IN THE CORE PACKAGE, and that is the asmdef's rule rather
    /// than a preference: `UnityEngine.Color` is an engine type and the balance layer carries
    /// no engine reference at all. The STRENGTH the rim is lit at is a tuned number and lives
    /// in `Balance.LandedRimStrength`; the colours are a presentation choice and live here.
    ///
    /// ⚠️ "OFF" IS ROW 0 OF THE LIST RATHER THAN A SENTINEL BESIDE IT. The Godot original
    /// records why: a separate enabled flag makes a second piece of state (a colour chosen
    /// while the feature is off) that has to be styled and explained, and a list whose first
    /// row is Off cannot represent it. It also makes the clamp on load honest, exactly like
    /// the AI difficulty: one <c>Clamp</c> over the whole list, with no value living outside
    /// the range it is clamped to.
    ///
    /// ⚠️ THE COLOURS ARE WRITTEN FOR A RIM TERM, NOT FOR A FILL. `TumbangPreso/Toon` mixes
    /// the rim into the lit base by a facing-angle ramp, so a desaturated hue arrives washed
    /// out. These are pushed to the saturated corner on purpose.
    /// </summary>
    public static class SlipperHighlights
    {
        public readonly struct Entry
        {
            public readonly string Label;
            public readonly Color Colour;

            public Entry(string label, Color colour)
            {
                Label = label;
                Colour = colour;
            }
        }

        /// <summary>
        /// Ordered for display. The settings row iterates this directly, so a colour added
        /// here appears in the panel with no second edit.
        /// </summary>
        public static readonly Entry[] All =
        {
            new Entry("Off",    new Color(0.0f,  0.0f,  0.0f)),
            new Entry("Blue",   new Color(0.18f, 0.55f, 1.0f)),
            new Entry("Purple", new Color(0.79f, 0.13f, 1.0f)),
            new Entry("Red",    new Color(1.0f,  0.16f, 0.16f)),
            new Entry("Yellow", new Color(1.0f,  0.95f, 0.05f)),
        };

        public const int Off = 0;

        /// <summary>
        /// Blue, and it is the one choice in this file that is about THIS game rather than
        /// about the reference it was drawn from.
        ///
        /// ⚠️ THE OWNER GLOW THIS RIM SHARES A CHANNEL WITH IS GOLD, the arena is warm dust
        /// and wood, and both maps are lit warm. Blue is the only entry in the palette that
        /// cannot be mistaken for either the other indicator or the floor behind it. Yellow as
        /// a default would have shipped a "where did it go" cue in the same colour as the
        /// "this one is yours" cue.
        /// </summary>
        public const int Default = 1;

        public static bool Enabled(int index) => index != Off;

        /// <summary>The colour for a stored index, clamped the same way the setting is.</summary>
        public static Color ColourOf(int index)
            => All[Mathf.Clamp(index, 0, All.Length - 1)].Colour;

        public static string LabelOf(int index)
            => All[Mathf.Clamp(index, 0, All.Length - 1)].Label;
    }
}
