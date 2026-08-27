using UnityEngine;

namespace TumbangPreso.Settings
{
    /// <summary>
    /// The anti-aliasing choices, and the one place that turns a stored index into the two
    /// switches that actually smooth an edge.
    ///
    /// ⚠️⚠️ THE GAME SHIPPED WITH ALMOST NO ANTI-ALIASING AND THE SCREENSHOTS HID IT.
    /// Measured on this branch before the change:
    ///
    ///   * `ProjectSettings/QualitySettings.asset` carried `antiAliasing: 0` on Very Low, Low,
    ///     Medium and High, and `antiAliasing: 2` on Very High and Ultra. Four of the six
    ///     levels a player can select were rendering with no MSAA at all.
    ///   * Every offscreen probe in `Assets/TumbangPreso/Editor/` builds its RenderTexture with
    ///     `antiAliasing = 4` or `8` (`PersonSwapProbe` uses 8, `ModelSheet`, `HeadToHeadProbe`,
    ///     `InGameAngleProbe`, `IterationTurnaroundProbe` and both showcase probes use 4). A
    ///     camera writing into a `targetTexture` takes the sample count off THAT texture and
    ///     ignores `QualitySettings.antiAliasing` entirely, so every image this project has
    ///     used to judge itself was anti-aliased and the played game was not.
    ///
    /// That gap is the whole reason "it looks jagged in game and clean in the shots" was never
    /// tracked down: the two paths never shared a setting.
    ///
    /// ⚠️⚠️ EVERY MODE ABOVE OFF CARRIES FXAA, AND THAT IS A HEDGE RATHER THAN A TASTE.
    /// MSAA in the built-in pipeline is applied by the RASTERISER, into whichever render target
    /// the camera ends up drawing to. Both gameplay cameras in this game have an `OnRenderImage`
    /// component (`Visual.ColourGrade`, and the spectator adds `SpectatorReplayCapture` on top),
    /// and an active image effect forces the camera off the backbuffer and into an intermediate
    /// RenderTexture that Unity allocates for it. Whether that intermediate is allocated
    /// multisampled is a decision inside the engine that depends on the rendering path, on
    /// `Camera.allowMSAA`, and on the HDR format `ColourGrade.Awake` requires for the tonemap to
    /// mean anything. If it is allocated flat, `QualitySettings.antiAliasing` is set, reported
    /// back correctly, and changes nothing on screen.
    ///
    /// FXAA has no such dependency. It is a filter over the pixels of a frame that has already
    /// been rendered, so it cannot be silently discarded by a target allocation: if the pass
    /// runs, the edges soften. Pairing it with MSAA rather than choosing between them means the
    /// setting is visible either way, and the cost of the pair is small because FXAA only blends
    /// where local luma contrast clears its threshold, which is exactly the pixels MSAA has
    /// already flattened. See `Visual.PostAntiAlias`, which also logs what the engine actually
    /// handed it so the question stops being a guess.
    ///
    /// ⚠️ "OFF" IS ROW 0 OF THE LIST RATHER THAN A SEPARATE FLAG, for the reason
    /// <see cref="SlipperHighlights"/> records at length: a second enabled bit creates a state
    /// (a sample count chosen while AA is off) that has to be stored, styled and explained, and
    /// one clamp over one list cannot represent it.
    ///
    /// ⚠️ AND THE SAMPLE COUNTS ARE 0/2/4/8, NOT AN INDEX. `QualitySettings.antiAliasing` takes
    /// the count directly and rejects anything else, which is also how the six levels are
    /// serialised in `QualitySettings.asset`.
    ///
    /// ⚠️⚠️ THE SIX QUALITY LEVELS WERE RAISED TOO, AND THE REASONING HAS TO LIVE HERE BECAUSE
    /// UNITY REWRITES THAT ASSET AND STRIPS ANY COMMENT PUT IN IT. They now read Very Low 0,
    /// Low 2, Medium 2, High 4, Very High 4, Ultra 4, against 0/0/0/0/2/2 before. Two decisions
    /// in that:
    ///
    ///  * VERY LOW STAYS AT 0. It is the only level in the file that also drops shadows and
    ///    halves the texture resolution, so it is the row that exists for a machine that cannot
    ///    afford the frame. Anti-aliasing it would be the one place this change made something
    ///    slower for somebody who had already said they could not spare it.
    ///  * ULTRA IS 4 RATHER THAN 8, AND IT IS 4 SO THAT IT MATCHES <see cref="Default"/>. In the
    ///    editor, writing `QualitySettings.antiAliasing` during play WRITES THROUGH to the asset,
    ///    so a level whose stored count differs from the mode this class applies at boot leaves
    ///    `ProjectSettings/QualitySettings.asset` dirty after every play session. Standalone
    ///    defaults to level 5 (`m_PerPlatformDefaultQuality`), which is Ultra, so matching the
    ///    two means the ordinary case touches nothing. Picking the 8x mode deliberately still
    ///    dirties it, which is a deliberate act rather than a side effect of pressing play.
    /// </summary>
    public static class AntiAliasModes
    {
        public readonly struct Entry
        {
            /// <summary>What the settings row shows.</summary>
            public readonly string Label;

            /// <summary>Passed straight to <c>QualitySettings.antiAliasing</c>. 0, 2, 4 or 8.</summary>
            public readonly int Samples;

            /// <summary>Whether <see cref="Visual.PostAntiAlias"/> runs its filter.</summary>
            public readonly bool Fxaa;

            public Entry(string label, int samples, bool fxaa)
            {
                Label = label;
                Samples = samples;
                Fxaa = fxaa;
            }
        }

        /// <summary>
        /// Ordered for display, cheapest first. The settings row iterates this directly, so a
        /// mode added here appears in the panel with no second edit.
        /// </summary>
        public static readonly Entry[] All =
        {
            new Entry("Off",             0, false),
            new Entry("FXAA",            0, true),
            new Entry("MSAA 2x + FXAA",  2, true),
            new Entry("MSAA 4x + FXAA",  4, true),
            new Entry("MSAA 8x + FXAA",  8, true),
        };

        public const int Off = 0;

        /// <summary>
        /// MSAA 4x with FXAA over it.
        ///
        /// ⚠️ 4x RATHER THAN THE 2x THE TWO TOP QUALITY LEVELS WERE ALREADY ASKING FOR. 2x
        /// resolves an edge into one intermediate step, which on the ink outline (`Toon.shader`
        /// pass 1, a `Cull Front` hull pushed 0.008 along the welded normal) is still a visible
        /// staircase at 1080p because that outline is a hard, high-contrast line against
        /// whatever is behind it. 4x gives three steps and is the point where the line reads as
        /// a line.
        ///
        /// ⚠️ AND NOT 8x, WHICH IS RESERVED FOR THE TOP ROW RATHER THAN MADE THE DEFAULT.
        /// 8x doubles the bandwidth cost of every pixel of the frame for a difference that is
        /// hard to see without pixel-peeping two stills, and this game is played on whatever
        /// the school laptop is.
        /// </summary>
        public const int Default = 3;

        /// <summary>
        /// Whether the post filter should run this session.
        ///
        /// ⚠️ A STATIC RATHER THAN A READ OF `SettingsStore.Current` PER FRAME. `PostAntiAlias`
        /// asks this question inside `OnRenderImage`, on every camera, every frame, and
        /// `SettingsStore.Current` loads and validates the whole settings file the first time it
        /// is touched. A render callback is not where that should ever be able to happen.
        ///
        /// ⚠️ SEEDED FROM THE DEFAULT ROW, NOT FROM `false`. The dedicated server never calls
        /// <see cref="Apply"/> with a player's choice, and a headless build renders nothing, so
        /// the seed only matters for the window between process start and the first
        /// `GameSettings.Apply`. Seeding it false would flicker AA on one frame after boot.
        /// </summary>
        public static bool FxaaActive { get; private set; } = All[Default].Fxaa;

        /// <summary>The sample count last requested, kept for the diagnostic line.</summary>
        public static int RequestedSamples { get; private set; } = All[Default].Samples;

        public static Entry Of(int index) => All[Mathf.Clamp(index, 0, All.Length - 1)];

        public static string LabelOf(int index) => Of(index).Label;

        /// <summary>
        /// Push a stored index at the two things that implement it.
        ///
        /// ⚠️ IT WRITES `QualitySettings.antiAliasing` RATHER THAN SWITCHING QUALITY LEVEL.
        /// The six levels differ in shadows, texture limits, skin weights and reflection probes,
        /// and none of that is what the player asked to change. Setting the one field leaves the
        /// level the platform default selected (`m_PerPlatformDefaultQuality` has Standalone on
        /// 5, Ultra) and overrides only the sample count on it.
        ///
        /// ⚠️ AND IT IS SAFE TO CALL ON A SERVER. `QualitySettings.antiAliasing` on a headless
        /// build is a stored number that nothing reads, so this needs no batch-mode guard the
        /// way `GameSettings.ApplyDisplay` does.
        /// </summary>
        public static void Apply(int index)
        {
            var entry = Of(index);

            RequestedSamples = entry.Samples;
            FxaaActive = entry.Fxaa;

            QualitySettings.antiAliasing = entry.Samples;
        }
    }
}
