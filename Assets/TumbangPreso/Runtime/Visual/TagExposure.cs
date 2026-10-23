using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// CAN THE TAYA TAG THIS BODY RIGHT NOW, AND HOW CLOSE IS THAT TO CHANGING (owner, 2026-10-23: *"maybe use pacman as
    /// reference"*, *"they get slowly covered by a color highlight everytime theyre taggable"*, *"it climbs up their body
    /// really quick"*). Design: docs/reports/tag-readability-2026-10-23/README.md.
    ///
    /// One reading for every cue on every screen, from replicated state only, as a LEVEL the body fills to:
    ///   * <see cref="State.Taggable"/> (<see cref="RoundDirector.CanBeTaggedNow"/>, the call the tag makes): full. In the
    ///     last second of Vulnerable, when its end will make them safe, the level drains with the time left.
    ///   * <see cref="State.Arming"/>: the can is down, the taya is standing it up, and this body will be taggable the
    ///     moment it stands. The level climbs with the restore channel, so it is full the instant the can is up.
    ///   * <see cref="State.Safe"/>: empty. Safe is the default look; only the exception is marked.
    /// <see cref="Fill"/> is the level as drawn: it climbs quickly and drains a little slower, so every change is seen as
    /// the colour moving. `TayaIdentity` advances every body once a frame, so the fill keeps time whether or not a
    /// camera is drawing that body. Presentation only: it decides nothing.
    /// </summary>
    public static class TagExposure
    {
        public enum State { Safe, Taggable, Arming }

        /// <summary>The last stretch of Vulnerable that drains.</summary>
        public const float EndingSeconds = 1f;
        /// <summary>Feet to head when someone becomes taggable ("really quick").</summary>
        public const float ClimbSeconds = .22f;
        /// <summary>Head to feet when someone becomes safe.</summary>
        public const float DrainSeconds = .3f;

        public static State Read(CharacterMotor actor, out float level)
        {
            level = 0;
            var round = GameServices.Round;
            if (round == null || actor == null || actor.IsDefender || !round.RoundActive) return State.Safe;
            if (round.CanBeTaggedNow(actor))
            {
                level = 1;
                // Vulnerable is running out, and nothing else keeps them exposed: they are about to be safe.
                if (actor.IsVulnerable && actor.VulnerableLeft < EndingSeconds && !(actor.HoldingSlipper && actor.IsInsideBox()))
                    level = Mathf.Clamp01(actor.VulnerableLeft / EndingSeconds);
                return State.Taggable;
            }
            var can = round.Lata;
            if (can == null || can.IsUpright || !actor.IsTaggable()) return State.Safe;
            float restore = LataClockPresentation.For(can)?.RestoreRatio ?? 0;
            if (restore <= 0) return State.Safe;
            level = Mathf.Clamp01(restore);
            return State.Arming;
        }

        private sealed class Drawn { public float Fill; public int Frame = -1; public float At = -1; }
        private static readonly Dictionary<CharacterMotor, Drawn> _drawn = new Dictionary<CharacterMotor, Drawn>();
        private static int _prunedAt;

        /// <summary>
        /// The fill height as drawn this frame, 0 (feet) to 1 (over the head): the level from <see cref="Read"/>, reached by
        /// climbing or draining rather than in a jump. Advanced once a frame however many cues ask; Reduced UI motion
        /// snaps straight to the level.
        /// </summary>
        public static float Fill(CharacterMotor actor)
        {
            if (actor == null) return 0;
            int frame = Time.frameCount;
            if (frame - _prunedAt > 600) { _prunedAt = frame; Prune(); }
            if (!_drawn.TryGetValue(actor, out var drawn)) _drawn[actor] = drawn = new Drawn();
            if (drawn.Frame == frame) return drawn.Fill;
            // Real time since this body was last advanced, so a body nobody asked about for a while catches up.
            float now = Time.unscaledTime;
            float dt = drawn.At < 0 ? 0 : Mathf.Clamp(now - drawn.At, 0, 1);
            drawn.Frame = frame; drawn.At = now;
            Read(actor, out float level);
            if (Settings.SettingsStore.Current.ReducedUiMotion) drawn.Fill = level;
            else drawn.Fill = Mathf.MoveTowards(drawn.Fill, level, dt / (level > drawn.Fill ? ClimbSeconds : DrainSeconds));
            return drawn.Fill;
        }

        private static readonly List<CharacterMotor> _gone = new List<CharacterMotor>();
        private static void Prune()
        {
            _gone.Clear();
            foreach (var pair in _drawn) if (pair.Key == null) _gone.Add(pair.Key);
            foreach (var key in _gone) _drawn.Remove(key);
        }

        /// <summary>The cue colour the player chose (Accessibility, Taggable colour); the taya's blue by default.</summary>
        public static Color Colour => Settings.TagColours.Current;

        /// <summary>True when <paramref name="state"/> wants the body marked at all.</summary>
        public static bool Marked(State state) => state != State.Safe;
    }
}
