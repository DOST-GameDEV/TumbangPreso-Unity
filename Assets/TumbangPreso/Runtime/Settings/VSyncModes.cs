using UnityEngine;

namespace TumbangPreso.Settings
{
    /// <summary>
    /// Whether the game waits for the display before showing a frame.
    ///
    /// ⚠️ IT IS A SETTING RATHER THAN A CONSTANT FOR THE SAME REASON ANTI-ALIASING IS: the right
    /// answer depends on the machine, and this one is played on whatever the school laptop is. On a
    /// display that can keep up, vsync removes tearing for free. On a machine that cannot hold the
    /// refresh rate, it is the difference between a frame arriving late and a frame arriving half a
    /// refresh late, which reads as much worse stutter than the raw frame rate suggests.
    ///
    /// ⚠️⚠️ THE HALF ROW IS THE ONE WORTH HAVING AND THE ONE PEOPLE LEAVE OUT. A machine that
    /// renders this map at 45 fps on a 60 Hz panel spends every third frame waiting, and the
    /// resulting judder is uneven: some frames hold for one refresh and some for two. Locking to
    /// every SECOND refresh gives a steady 30, and a steady 30 reads as smoother than an unsteady
    /// 45 because the eye tracks the inconsistency rather than the average.
    ///
    /// ⚠️ `vSyncCount` IS NOT A FRAME RATE AND SETTING `targetFrameRate` DOES NOT REPLACE IT. It
    /// counts vertical blanks to wait for, so its meaning depends on the panel: 1 is 60 fps on a
    /// 60 Hz display and 144 on a 144 Hz one. Unity IGNORES `targetFrameRate` entirely whenever
    /// `vSyncCount` is above zero, so the two must never both be used to mean the same thing.
    /// </summary>
    public static class VSyncModes
    {
        public readonly struct Entry
        {
            public readonly string Label;

            /// <summary>Passed straight to <see cref="QualitySettings.vSyncCount"/>.</summary>
            public readonly int Count;

            public Entry(string label, int count)
            {
                Label = label;
                Count = count;
            }
        }

        public static readonly Entry[] All =
        {
            new Entry("Off",           0),
            new Entry("On",            1),
            new Entry("Half Refresh",  2),
        };

        /// <summary>
        /// ⚠️ ON, AND THE DEFAULT IS DELIBERATELY NOT "OFF" EVEN THOUGH OFF IS FASTER. Tearing is a
        /// defect a player cannot fix by adjusting anything in the game, and it is most visible
        /// exactly where this game lives: a camera panning horizontally across a street of hard
        /// vertical edges. Uncapped frames are worth having on a machine that is struggling, which
        /// is why the row exists, but somebody who never opens settings should not be shown a torn
        /// frame to save a millisecond they did not ask to save.
        /// </summary>
        public const int Default = 1;

        public static int Current { get; private set; } = All[Default].Count;

        public static Entry Of(int index) => All[Mathf.Clamp(index, 0, All.Length - 1)];

        public static string LabelOf(int index) => Of(index).Label;

        /// <summary>
        /// Push a stored index at the engine.
        ///
        /// ⚠️ SAFE ON A HEADLESS SERVER, the same way <see cref="AntiAliasModes.Apply"/> is.
        /// `QualitySettings.vSyncCount` is meaningless without a display and setting it does
        /// nothing there rather than failing, so the dedicated server build does not need a branch.
        /// </summary>
        public static void Apply(int index)
        {
            var entry = Of(index);

            Current = entry.Count;
            QualitySettings.vSyncCount = entry.Count;
        }
    }
}
