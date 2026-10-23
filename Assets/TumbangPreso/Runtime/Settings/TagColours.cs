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
    /// ⚠️ RED BY DEFAULT, FOR EVERYONE: THE TAYA'S OWN COLOUR (owner, 2026-10-23: "change default color ng pede tayain or
    /// taggable to red instead of blue, for everyone", "it makes more sense if the taya's color matches taggable bcz it means
    /// they can be hunted by taya"). Red now reads "the red player can get you". Blue moved to the end of the list so the
    /// saved index 0 everyone holds becomes red and every other saved choice keeps its colour. Still no orange (Offense
    /// orange means "the can is down, the attackers are free") and no Off: the cue is the answer to the game's main
    /// question; `WorldCueProfile.TayaTarget` remains the authoring switch.
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
            // The taya's red (`UiTheme.Taya`): hunted in the hunter's colour.
            new Entry("Red",       UI.UiTheme.Taya),
            new Entry("Magenta",   new Color(.95f, .16f, .72f)),
            new Entry("Yellow",    new Color(1f, .86f, .08f)),
            new Entry("Cyan",      new Color(.1f, .86f, .95f)),
            new Entry("White",     new Color(.97f, .97f, .97f)),
            // The old default, kept for whoever wants it; last, so no saved index moves.
            new Entry("Blue",      new Color(.16f, .6f, 1f)),
        };

        public const int Default = 0;

        public static Color ColourOf(int index) => All[Mathf.Clamp(index, 0, All.Length - 1)].Colour;
        public static string LabelOf(int index) => All[Mathf.Clamp(index, 0, All.Length - 1)].Label;
        /// <summary>The colour the current settings ask for.</summary>
        public static Color Current => ColourOf(SettingsStore.Current.TaggableColour);
    }
}
