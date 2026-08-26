using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The freeze on a landed hit, converted from `character_base.gd`'s hitstop block.
    ///
    /// ⚠️⚠️ NO INSTANCE OWNS IT, AND THAT IS THE WHOLE DESIGN. In Godot this was a scene timer
    /// whose one listener was an INSTANCE method guarding STATIC flags: free that instance
    /// inside the 60 ms window and the connection died with it, so the restore never ran.
    /// `time_scale` stayed at 0.05 **for the rest of the process**, and the active flag stayed
    /// true, which also silently disabled every future hitstop in the game.
    ///
    /// ⚠️ IT IS NOT A THEORETICAL LIFETIME BUG — IT COST A MEASUREMENT. A probe freeing the
    /// whole match between runs orphaned the timer when a hit landed on the last frame, and
    /// the next match ran at 0.05 against the probe's own 6.0 — a 120× slowdown that reads
    /// exactly like a hang. `matches=1` always finished; `matches=3` never got past match 2.
    /// The shipping game has the same shape: quitting a match on the frame somebody was hit
    /// left the MENUS running at 5% speed.
    ///
    /// So: a wall-clock deadline on UNSCALED time, cleared by whoever is alive, plus a forced
    /// restore on teardown. Nothing depends on a particular object surviving.
    ///
    /// ⚠️ AND IT IS UNSCALED TIME BY NECESSITY. Scaled time is the thing being frozen, so a
    /// deadline measured in it would take twenty times as long to expire as intended.
    /// </summary>
    public static class Hitstop
    {
        private static bool _active;
        private static float _until;
        private static float _restoreScale = 1.0f;

        /// <summary>
        /// The clock this freeze is measured against.
        ///
        /// ⚠️⚠️ IT IS REAL TIME IN THE GAME AND CAPTURED TIME UNDER A CAPTURE, AND THAT
        /// SECOND CASE IS THE ONLY REASON THIS IS NOT JUST `Time.unscaledTime`.
        /// `Time.captureDeltaTime` is what `BotBehaviourProbe` sets to make a match advance the
        /// same slice of game time every frame, and it does NOT pin `Time.unscaledTime`, which
        /// keeps running at whatever speed the machine renders. So a hitstop measured in
        /// unscaled time lasts a number of FRAMES that depends on the machine, while
        /// `Time.timeScale` is 0.05 for all of them: the wall clock gets back into the
        /// simulation through the one door the fixed step did not close.
        ///
        /// ⚠️⚠️ AND IT WAS MEASURED, NOT SUSPECTED. `docs/TODO.md` § 10 recorded the probe
        /// as deterministic on 2026-08-26 without ever running the same match twice. The first
        /// sweep that did (§ 5) ran the shipped overclock rate at the start and the end of one
        /// session and got **18 skills, 6 ultimates, 43 throws and 822 idle penalties** against
        /// **37, 19, 83 and 464**: the same build, the same seed, the same fixed step, twice as
        /// much game in the second run.
        ///
        /// ⚠️ A CAPTURE IS NEVER SET BY THE GAME. `Time.captureDeltaTime` defaults to 0 and
        /// only a probe writes it, so every shipped path takes the `unscaledDeltaTime` branch
        /// and behaves exactly as before.
        /// </summary>
        private static float _clock;

        private static void Advance()
        {
            _clock += Time.captureDeltaTime > 0.0f ? Time.captureDeltaTime
                                                   : Time.unscaledDeltaTime;
        }

        public static bool Active => _active;

        /// <summary>Freeze. Re-entrant calls during a freeze are ignored rather than
        /// extending it — three attackers landing hits together must not stack into a stall.</summary>
        public static void Trigger()
            => Trigger(Balance.HitstopDuration, Balance.HitstopTimeScale);

        /// <summary>Tiered micro-hitstop for can hits and ultimates. Values are bounded so
        /// presentation can never become a gameplay-length freeze.</summary>
        public static void Trigger(float duration, float timeScale)
        {
            if (_active) return;

            _active = true;
            _restoreScale = Time.timeScale;
            _until = _clock + Mathf.Clamp(duration, 0.02f, 0.08f);

            Time.timeScale = Mathf.Clamp(timeScale, 0.03f, 0.35f);
        }

        /// <summary>
        /// Called every frame by whatever is alive. Deliberately safe to call from many
        /// places and from none: the deadline is absolute, so a frame where nobody calls it
        /// only delays the restore, and the teardown path catches that.
        /// </summary>
        public static void Step()
        {
            // ⚠️ THE CLOCK ADVANCES EVEN WHEN NOTHING IS FROZEN, because `Trigger` reads it to
            // set a deadline and a clock that only ran during a freeze would hand out deadlines
            // in the past.
            Advance();

            if (!_active || _clock < _until) return;

            End();
        }

        /// <summary>⚠️ CALL THIS ON ANY TEARDOWN THAT COULD HAPPEN MID-FREEZE — leaving a
        /// match, ending a probe run, unloading a scene.</summary>
        public static void End()
        {
            if (!_active) return;

            _active = false;
            Time.timeScale = _restoreScale <= 0.0f ? 1.0f : _restoreScale;
        }

        /// <summary>
        /// ⚠️ STATICS SURVIVE PLAY SESSIONS WHEN DOMAIN RELOAD IS OFF, which would carry a
        /// stuck freeze into the next run — the exact failure this class exists to prevent,
        /// reintroduced by the editor rather than by a lifetime.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _active = false;
            _restoreScale = 1.0f;
            _until = 0.0f;
        }
    }
}
