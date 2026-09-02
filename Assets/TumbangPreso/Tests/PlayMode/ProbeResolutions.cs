namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Every screen shape this game is measured at, in one place.
    ///
    /// ⚠️⚠️ IT IS SHARED SO THAT ADDING A SHAPE REACHES EVERY PROBE AT ONCE. `AspectRatioProbes`
    /// carried this list privately and `InputSurfaceProbe` would have carried a copy, which is
    /// two lists that agree until somebody edits one. `docs/TODO.md` § 124.11 is what that costs:
    /// *"a green probe for a screen nobody can reach is worse than a red one"*, and a probe
    /// driving eight resolutions while its sibling drives ten is the same fault said quietly.
    ///
    /// ⚠️⚠️ THE PHONE SHAPES ARE PART OF THE DEFAULT SET NOW, NOT A SEPARATE LIST. 🧑: *"anytime
    /// we add a feature, make sure all controller and mobile is considered"*. A mobile-only list
    /// that only the mobile probe reads is exactly the arrangement that lets a desktop UI change
    /// ship without anybody looking at it on a phone. Every layout probe in this project drives
    /// all of them.
    /// </summary>
    public static class ProbeResolutions
    {
        /// <summary>
        /// 16:9 from 720p to 1440p, the common laptop panel, 16:10, both ultrawides, and 4:3.
        ///
        /// ⚠️ ALL NINE ARE TALLER THAN THE WINDOW 🧑 ACTUALLY PLAYS IN, which is `CLAUDE.md`
        /// § 6.2b's third row and is why `Short wide window` is in the phone list below rather
        /// than being left implicit.
        /// </summary>
        public static readonly (int W, int H, string Name)[] Desktop =
        {
            (1280,  720, "16:9 720p"),
            (1600,  900, "16:9 900p"),
            (1920, 1080, "16:9 1080p"),
            (2560, 1440, "16:9 1440p"),
            (1366,  768, "16:9 laptop"),
            (1920, 1200, "16:10"),
            (2560, 1080, "21:9"),
            (3440, 1440, "21:9 1440p"),
            (1024,  768, "4:3"),
        };

        /// <summary>
        /// The shapes a thumb meets, plus the window he plays in.
        ///
        /// ⚠️ 19.5:9 IS THE NOTCHED FLAGSHIP AND 20:9 IS EVERY MID-RANGE ANDROID SOLD IN THE LAST
        /// FIVE YEARS. Both are far WIDER than 16:9 in landscape, so they are the opposite crop
        /// from 4:3: the canvas matches on HEIGHT, so a phone gets a short, very wide canvas and
        /// anything anchored to the vertical centre crowds. `2340x1080` and `2400x1080` are the
        /// two panels that actually ship.
        ///
        /// ⚠️⚠️ AND THE LAST ROW IS NOT A PHONE. `CLAUDE.md` § 6.2b: *"`Fullscreen` is false in
        /// his settings.json. He plays in a short wide window, and all nine probe resolutions are
        /// taller than it."* A 1600x680 window is the same aspect problem as a phone and is the
        /// shape the person reviewing this work is looking at, so it belongs in the set that
        /// every layout claim is made against.
        /// </summary>
        public static readonly (int W, int H, string Name)[] Phone =
        {
            (2340, 1080, "19.5:9 phone"),
            (2400, 1080, "20:9 phone"),
            (1600,  680, "short wide window"),
        };

        /// <summary>Every shape, desktop and phone, which is what a layout claim must survive.</summary>
        public static (int W, int H, string Name)[] All()
        {
            var all = new (int, int, string)[Desktop.Length + Phone.Length];

            Desktop.CopyTo(all, 0);
            Phone.CopyTo(all, Desktop.Length);

            return all;
        }
    }
}
