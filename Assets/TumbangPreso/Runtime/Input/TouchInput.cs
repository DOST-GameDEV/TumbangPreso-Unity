using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.InputLayer
{
    /// <summary>
    /// What the on-screen thumb layer is currently reporting, for the local seat.
    ///
    /// ⚠️⚠️ IT IS A HELD TABLE AND NOT AN EVENT STREAM, WHICH IS WHAT MAKES TOUCH TESTABLE
    /// HEADLESSLY. `InputIntent`'s class note is the rule: *edges are derived, not reported*, and
    /// a producer that reported its own edges could report an impossible pair. A touch button
    /// therefore says only "a finger is on me right now"; `PlayerInputReader` folds that into the
    /// same held set the keyboard writes, and `InputIntent` diffs it. So a probe presses a touch
    /// button by writing this table, and the whole downstream game cannot tell the difference
    /// between that, a finger, a pad and a key. That is the same argument that makes a bot press
    /// the same buttons a human does.
    ///
    /// ⚠️⚠️ AND IT IS WHY HOLD-TO-AIM WORKS ON A THUMB WITH NO EXTRA CODE. `docs/TODO.md`
    /// § 124.1 added five hold-to-aim powers, and `HeroAbility.CastsOnReleaseOnly` decides which
    /// edge casts. A touch button that is held while the finger is down produces exactly the same
    /// press-hold-release the keyboard produces, so `HoldAim`, `AimRangeFor` and the release cast
    /// all work unchanged. Reporting a tap as one frame of "pressed" is the fault that would have
    /// pinned every held power to `AimRangeFor`'s MINIMUM, which is the bot fault that same entry
    /// records against `AIController.Consider`.
    ///
    /// ⚠️ STATIC, LIKE `LobbyChat.AnyTyping`, BECAUSE THERE IS EXACTLY ONE LOCAL PLAYER. The
    /// touch layer draws once, over whichever seat the camera is following. A per-seat instance
    /// would be a table three bots never read.
    /// </summary>
    public static class TouchInput
    {
        private static readonly HashSet<Verb> Held = new HashSet<Verb>();

        /// <summary>
        /// True while the on-screen layer is drawn. Everything below is inert when it is not, so
        /// a desktop build cannot pick up a stale press from a probe that forgot to release one.
        /// </summary>
        public static bool Active { get; set; }

        /// <summary>The left thumb's stick, already normalised to the -1..1 box Move expects.</summary>
        public static Vector2 Move { get; set; }

        /// <summary>
        /// This frame's drag on the look side of the screen, in the units a mouse reports.
        ///
        /// ⚠️ THE READER ZEROES IT AFTER READING, because a drag is a delta and a finger that
        /// stops moving without lifting reports nothing new. Leaving the last value standing
        /// turns a paused finger into a camera that keeps spinning, which is
        /// `InputIntent.LookDelta`'s own note.
        /// </summary>
        public static Vector2 LookDelta { get; set; }

        public static bool Pressed(Verb verb) => Active && Held.Contains(verb);

        public static void Set(Verb verb, bool pressed)
        {
            if (pressed) Held.Add(verb);
            else Held.Remove(verb);
        }

        /// <summary>
        /// Everything up, and the axes to zero.
        ///
        /// ⚠️ CALLED WHEN THE LAYER IS HIDDEN AND WHEN A PROBE FINISHES. A verb left held here
        /// survives a scene load, because a static does, and the next match would start with the
        /// player already sprinting. The same fault `PlayerInputReader.OnDisable` guards.
        /// </summary>
        public static void ReleaseAll()
        {
            Held.Clear();
            Move = Vector2.zero;
            LookDelta = Vector2.zero;
        }

        /// <summary>Whatever is held right now, for a probe's report.</summary>
        public static IEnumerable<Verb> HeldVerbs => Held;
    }
}
