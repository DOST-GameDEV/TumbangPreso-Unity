using UnityEngine;

namespace TumbangPreso.Settings
{
    /// <summary>
    /// The colour a taggable body fills with (owner, 2026-10-23: *"make it changable in settings too with diff colors"*).
    /// Design: docs/reports/tag-readability-2026-10-23/README.md.
    ///
    /// ⚠️ LOCAL ONLY, NEVER REPLICATED, for the reason <see cref="SlipperHighlights"/> records: it is a choice about one
    /// player's screen. Two peers may see the same runner fill in different colours, and that is correct.
    ///
    /// ⚠️ NO RED OR ORANGE, AND NO OFF. Offense orange already means "the can is down, the attackers are free", so a red
    /// or orange fill would say the opposite of what it means, and red is the taya's own colour. The cue is the answer to the game's main question, so it
    /// has no Off; `WorldCueProfile.TayaTarget` remains the authoring switch.
    /// </summary>
    public static class TagColours
    {
        public readonly struct Entry
        {
            public readonly string Label;
            public readonly Color Colour;
            public Entry(string label, Color colour) { Label = label; Colour = colour; }
        }

        /// <summary>Ordered for display; the settings row iterates this directly.</summary>
        public static readonly Entry[] All =
        {
            // Pac-Man's frightened blue: "can be caught". The taya is red (`UiTheme.Taya`), so hunter and hunted never
            // share a colour. Lifted a step from `UiTheme.Defense` so the fill does not go muddy in shade.
            new Entry("Blue",      new Color(.16f, .6f, 1f)),
            new Entry("Magenta",   new Color(.95f, .16f, .72f)),
            new Entry("Yellow",    new Color(1f, .86f, .08f)),
            new Entry("Cyan",      new Color(.1f, .86f, .95f)),
            new Entry("White",     new Color(.97f, .97f, .97f)),
        };

        public const int Default = 0;

        public static Color ColourOf(int index) => All[Mathf.Clamp(index, 0, All.Length - 1)].Colour;
        public static string LabelOf(int index) => All[Mathf.Clamp(index, 0, All.Length - 1)].Label;
        /// <summary>The colour the current settings ask for.</summary>
        public static Color Current => ColourOf(SettingsStore.Current.TaggableColour);
    }
}
