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

        /// <summary>
        /// What each of Unity's six quality levels stores in its own <c>antiAliasing</c> field,
        /// in the order they appear in <c>ProjectSettings/QualitySettings.asset</c>:
        /// Very Low, Low, Medium, High, Very High, Ultra.
        ///
        /// ⚠️⚠️ THIS TABLE EXISTED ONLY AS PROSE IN THIS FILE'S HEADER AND THAT IS EXACTLY HOW IT
        /// DRIFTED. `docs/TODO.md` § 125.14: Ultra had been committed as 0 while the header said
        /// 4, and the disagreement sat in the repository as an uncommitted diff that read like
        /// somebody's stray edit. A number with a documented intent and no check is the same shape
        /// as `GameBuilder.ConfigureSplash` in `CLAUDE.md` § 6.4: *a colour set in
        /// `ProjectSettings.asset` is not set.* `QualitySettingsAssetTests` reads these six.
        ///
        /// ⚠️⚠️ AND THE HEADER'S ARGUMENT FOR ULTRA BEING 4 IS NOW STALE, WHICH IS WORTH KNOWING
        /// BEFORE ANYBODY "FIXES" THIS AGAIN. It reads *"Ultra is 4 so that it matches
        /// <see cref="Default"/> ... so matching the two means the ordinary case touches
        /// nothing"*, and it was true when `Default` was index 3, MSAA 4x + FXAA. **`Default` is
        /// 1 now, FXAA alone, whose `Samples` is 0**, for the measured reason further up this
        /// file (MSAA puts a white keyline round every distant silhouette through the tonemap).
        /// So the two no longer match and the protection that sentence describes is not the one
        /// in force. The table below is kept at the RENDERING intent rather than bent to suit the
        /// default, because <see cref="Apply"/> overwrites the active level at boot from the
        /// player's own setting: **the stored number is never what the game renders with.**
        ///
        /// ⚠️⚠️ AND THE MEASUREMENT THAT USED TO SIT HERE IS FALSIFIED, WHICH IS WORTH KEEPING
        /// RATHER THAN QUIETLY REPLACING. It read: *"MEASURED RATHER THAN ASSUMED, 2026-09-03: a
        /// full batchmode PlayMode suite, 155 tests and eighteen minutes of play, left
        /// `ProjectSettings/QualitySettings.asset` completely clean. The write-through the header
        /// warns about is an INTERACTIVE editor behaviour."* **It is not interactive.**
        /// `GameSettings.Apply` calls <see cref="Apply"/> at boot in batch mode too, and in the
        /// editor `QualitySettings.antiAliasing` IS the serialized asset, so writing the live
        /// value writes the file. Re-measured 2026-09-05 on `837eb0a`: a plain
        /// `-batchmode -runTests -testPlatform EditMode` launch moved Ultra from 4 to 0, twice in
        /// a row, on a clean checkout.
        ///
        /// ⚠️⚠️ THAT STOPPED BEING COSMETIC THE DAY THE QUALIFICATION STARTED REFUSING A DIRTY
        /// TREE. `docs/TODO.md` § 145.1 and § 149.11: a gate that rewrites a tracked file while it
        /// runs is a gate that can never come out QUALIFIED, however green every test is.
        /// `QualityLevelStamp` regenerates the asset from THIS TABLE at editor load and at quit,
        /// which is `GameBuilder.ConfigureSplash`'s and `ShaderWarmupCollection`'s shape:
        /// **both places or neither**, resolved by making one of the two generate the other.
        ///
        /// ⚠️ THE LEVEL AT RISK IS STILL WHICHEVER `m_PerPlatformDefaultQuality` SELECTS FOR THE
        /// CURRENT BUILD TARGET: Standalone is 5 (Ultra) and **Android is 2 (Medium)**, so
        /// switching the target to build an .apk moves which row gets rewritten.
        /// </summary>
        public static readonly int[] QualityLevelSamples = { 0, 2, 2, 4, 4, 4 };

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
        /// ⚠️⚠️ THE DEFAULT MOVED FROM MSAA 4x + FXAA TO FXAA ALONE, BECAUSE MSAA PUTS A WHITE
        /// KEYLINE ROUND EVERY DISTANT SILHOUETTE ON THIS FRAME. 🧑 2026-08-28, having tested all
        /// three: *"off and fxaa gets rid of the outlines. msaa brings it back"*. That isolates it
        /// exactly, and the reasoning above about 2x versus 4x is still correct and still beside
        /// the point.
        ///
        /// ⚠️ IT IS MSAA MEETING A TONEMAP, NOT MSAA BEING WRONG. Multisample resolve AVERAGES its
        /// samples, and on this camera it does so in linear HDR, BEFORE `ColourGrade` runs its ACES
        /// curve. That curve is compressive, so averaging and then tonemapping is not the same
        /// operation as tonemapping and then averaging:
        ///
        ///     tonemap(mean(sky, roof))  is much brighter than  mean(tonemap(sky), tonemap(roof))
        ///
        /// At a roofline against the sky, half the samples carry a sky value well above 1.0 and
        /// half carry a dark roof. Their mean lands high on the curve, where it is flat, so the
        /// edge pixel resolves BRIGHTER than either surface it sits between. Every silhouette gets
        /// a pale rim, which is exactly what was reported and only on the MSAA rows.
        ///
        /// ⚠️ AND THE TONEMAP CORRECTION MADE IT VISIBLE RATHER THAN CAUSING IT. While the
        /// pre-scale was 0.552 nothing in the frame could exceed 0.648, so sky and roof were close
        /// together and their mean was close to both. Lifting white to 0.90 spread them apart, and
        /// the same latent fault became a line you can see. Reverting the tonemap would hide this
        /// and re-darken the whole game; it is not the trade to make.
        ///
        /// ⚠️ THE REAL FIX IS A TONEMAPPED RESOLVE, AND THE BUILT-IN PIPELINE DOES NOT OFFER ONE.
        /// Engines that want both apply a reversible tonemap before the resolve and undo it after,
        /// so the average is taken in a perceptual space. That needs control of the resolve step,
        /// which `OnRenderImage` does not have. The alternative, turning `allowHDR` off, would clamp
        /// every value at 1.0 before `ColourGrade` ever saw it and leave the ACES curve nothing to
        /// roll off, which is the tonemap deleted by another name.
        ///
        /// So FXAA alone is the default: it runs AFTER the tonemap on display-referred values,
        /// which is the space its thresholds were designed for, and it cannot produce this artefact
        /// at all. The MSAA rows are KEPT rather than deleted, because they are correct on a map
        /// that grades nothing and a player who prefers geometric edges can still pick one.
        public const int Default = 1;

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

            // ⚠️⚠️ IN THE EDITOR THAT LINE ABOVE IS A FILE WRITE, AND IT MADE EVERY UNITY RUN
            // LEAVE THE WORKING TREE DIRTY. `QualitySettings.antiAliasing` is the serialized
            // project asset in the editor, so applying the player's own setting at boot rewrites
            // `ProjectSettings/QualitySettings.asset` on the quality level the current build
            // target selects. Cosmetic until `tools/qualify.py` started refusing a dirty tree
            // (`docs/TODO.md` § 145.1); after that it is a gate that **cannot come out QUALIFIED
            // however green every test is**, because running it dirties the thing it certifies.
            // § 149.11.
            //
            // ⚠️ THE EVENT IS THE SEAM AND `QualityLevelStamp` IS THE ONE SUBSCRIBER. This
            // assembly may not reference `UnityEditor`, and the fix is an editor operation
            // (`EditorUtility.ClearDirty`): the live value stays applied, which is what the frame
            // needs, and the object stops being marked for saving, which is what the repository
            // needs. Nothing about what the game RENDERS changes.
            AppliedInEditor?.Invoke();
        }

        /// <summary>
        /// Raised after every <see cref="Apply"/>, for the editor to undo the side effect.
        ///
        /// ⚠️ IT IS RAISED IN PLAYERS TOO AND NOTHING SUBSCRIBES THERE. A `#if UNITY_EDITOR`
        /// around the raise would put a compile-time branch in the one method whose behaviour has
        /// to be identical on both sides; an event with no subscribers is free.
        /// </summary>
        public static event System.Action AppliedInEditor;
    }
}
