using System;
using System.Collections.Generic;

namespace TumbangPreso.Core
{
    /// <summary>
    /// What kind of moment this was.
    ///
    /// ⚠️⚠️ EVERY VALUE HERE IS SOMETHING THE GAME CAN ALREADY ESTABLISH FROM ITS OWN STATE, AND
    /// THAT IS THE ADMISSION TEST FOR A NEW ONE. `docs/TODO.md` § 147's brief is explicit that
    /// this layer must not "create fake hype by assigning arbitrary bonuses": a marker is a
    /// RECORD that something happened, so a kind whose detection would need a judgement call is a
    /// kind that does not belong on this list.
    ///
    /// ⚠️ THEY ARE NOT SCORES AND NOTHING MAY MAKE THEM ONE. `docs/VISION.md` § 4: every point in
    /// the game is awarded in `MatchDirector.AddScore`, host-side. This layer is read by the
    /// replay and the spectator and by nothing that can pay anybody.
    /// </summary>
    public enum HighlightKind
    {
        /// <summary>A throw banked off the scenery and still put the can over.</summary>
        BankShot = 0,

        /// <summary>The can went over from a long way out. `Measurement` is metres.</summary>
        LongKnockdown = 1,

        /// <summary>An attacker got very close to the taya and got away. `Measurement` is metres.</summary>
        CloseCall = 2,

        /// <summary>A tsinelas collected with the taya nearly on top of it. `Measurement` is metres.</summary>
        ClutchRetrieval = 3,

        /// <summary>The can went over near the whistle. `Measurement` is seconds left.</summary>
        LastSecondKnockdown = 4,

        /// <summary>A tsinelas collected near the whistle. `Measurement` is seconds left.</summary>
        LastSecondRetrieval = 5,

        /// <summary>Several close calls inside a short window. `Measurement` is how many.</summary>
        EvasionRun = 6,

        /// <summary>A body stopped a tsinelas.</summary>
        Block = 7,

        /// <summary>The taya caught somebody.</summary>
        Tag = 8,

        /// <summary>An ultimate was cast.</summary>
        Ultimate = 9,
    }

    /// <summary>
    /// One recorded moment, with enough on it for a replay or a spectator to find and describe it.
    ///
    /// ⚠️⚠️ IT CARRIES A TIME AND THAT IS THE FIELD THE WHOLE THING EXISTS FOR.
    /// `SpectatorCamera` already knew about banks, blocks, close calls and knockdowns, and threw
    /// every one of them away the instant it had drawn a popup: `QueueHighlight`'s own note
    /// records that *"nothing in the buffer knew WHEN the tag was, so the clip was still the last
    /// five and a half seconds whenever the key happened to be pressed."* That was fixed for the
    /// replay ring by stamping the frame. **Nothing outside that ring can still answer "what
    /// happened in this match and when",** which is what a highlight reel, a spectator ticker and
    /// a post-match summary all need.
    ///
    /// ⚠️ THE TIME IS THE MATCH CLOCK, NOT A WALL CLOCK, so a marker is comparable with anything
    /// else measured against the same match and survives a paused or slowed broadcast clock.
    /// </summary>
    public readonly struct HighlightMarker
    {
        public readonly HighlightKind Kind;

        /// <summary>Seconds since the match began, on the peer that recorded it.</summary>
        public readonly float AtSeconds;

        /// <summary>Which round. 1-based, and 0 before a match starts.</summary>
        public readonly int Round;

        /// <summary>Whose moment it was, or -1.</summary>
        public readonly int Actor;

        /// <summary>The other party (the victim, the blocker), or -1.</summary>
        public readonly int Subject;

        /// <summary>
        /// The number the kind is about: metres, seconds left, or a count.
        ///
        /// ⚠️⚠️ ONE FIELD RATHER THAN ONE PER KIND, AND THE KIND SAYS WHAT IT MEANS. A struct
        /// with `Metres`, `SecondsLeft` and `Count` on it would be three fields of which two are
        /// always zero, and a reader that picked the wrong one would get a plausible number. The
        /// enum is the discriminant and `HighlightRules.Describe` is the one place that spends it.
        /// </summary>
        public readonly float Measurement;

        /// <summary>0 to 1. What a director would sort by. See <see cref="HighlightRules.ImportanceFor"/>.</summary>
        public readonly float Importance;

        public HighlightMarker(HighlightKind kind, float atSeconds, int round, int actor,
                               int subject, float measurement, float importance)
        {
            Kind = kind;
            AtSeconds = atSeconds;
            Round = round;
            Actor = actor;
            Subject = subject;
            Measurement = measurement;
            Importance = importance;
        }

        public override string ToString() => HighlightRules.Describe(this);
    }

    /// <summary>
    /// When a moment counts as one, how important it is, and when two reports are one event.
    ///
    /// ⚠️⚠️ ENGINE-FREE, WHICH IS WHAT MAKES "DETERMINISTIC AND NOT REPEATED" ASSERTABLE.
    /// `docs/TODO.md` § 147 asks for markers that "do not fire repeatedly for one event", and
    /// that is a claim about a rule rather than about a frame: a knockdown reaches this layer from
    /// `MatchFlair.LataDown`, from the score watcher and (on a bank) from the bank detector, three
    /// times, within a few hundred milliseconds. `SpectatorCamera`'s own header records exactly
    /// that fault costing the game a feature: *"a knockdown, a tag and a sabotage are three
    /// separate triggers, and `PollHighlights` adds a fourth"*, and the answer then was to delete
    /// the trigger. Here the answer is a rule that can be tested in a millisecond.
    /// </summary>
    public static class HighlightRules
    {
        /// <summary>
        /// How long one (kind, actor) pair is considered to still be the same event.
        ///
        /// ⚠️⚠️ 1.5 s IS `Balance.DefenseTickInterval`-SHAPED RATHER THAN PICKED: it is longer
        /// than any chain of reports a single gameplay event produces (the knockdown's three
        /// arrive inside one physics step and the score watcher's within one 5 Hz snapshot, so
        /// 0.2 s at the outside) and shorter than the fastest a player can genuinely do the same
        /// thing twice. `Balance.LungeCooldown` is 1.5 s and `PunchCooldown` is 0.9: the taya
        /// cannot tag twice inside this window, so no real second event is being swallowed.
        /// </summary>
        public const float SameEventSeconds = 1.5f;

        /// <summary>
        /// How close an attacker has to get to the taya for it to be a close call.
        ///
        /// ⚠️ IT IS THE TAYA'S OWN REACH AND NOT A NUMBER OF ITS OWN. `Balance.LungeTagRadius` is
        /// 1.3 m, which is the distance at which the taya's dash actually catches somebody: an
        /// attacker who was inside it and is not stunned got away with something. A separate
        /// constant here would be a second opinion about what "nearly caught" means.
        /// </summary>
        public const float CloseCallMetres = Balance.LungeTagRadius;

        /// <summary>
        /// How far out a knockdown counts as a long one.
        ///
        /// ⚠️⚠️ IT IS THE CONFINEMENT RADIUS, WHICH MAKES IT "FROM OUTSIDE THE DANGER ZONE".
        /// `Balance.ConfinementRadius` is 7.0 and the box is 14 m across, so a throw from further
        /// than that came from beyond the arena's own edge and is a shot nobody had to enter the
        /// box for. Half the box would be arbitrary; the box itself is the thing the game is about.
        /// </summary>
        public const float LongKnockdownMetres = Balance.ConfinementRadius;

        /// <summary>
        /// How near the whistle counts as last-second.
        ///
        /// ⚠️ `Balance.ChargeFullTime` IS 2.5 s AND IS WHY. It is exactly one full throw wind-up:
        /// inside it, nothing anybody starts now can still land, so anything that DOES land was
        /// already committed. That is the definition of a last-second play rather than a
        /// round-number about clocks.
        /// </summary>
        public const float LastSecondSeconds = Balance.ChargeFullTime;

        /// <summary>How many close calls inside <see cref="EvasionWindowSeconds"/> make a run.</summary>
        public const int EvasionRunCount = 3;

        /// <summary>
        /// ⚠️ THE WINDOW IS ONE `Balance.TagStunTime` (5.0 s), which is what a single mistake in
        /// this game costs. Three escapes inside the time one failure would have removed you for
        /// is the claim worth making.
        /// </summary>
        public const float EvasionWindowSeconds = Balance.TagStunTime;

        /// <summary>
        /// Whether <paramref name="candidate"/> is another report of <paramref name="previous"/>.
        ///
        /// ⚠️ SAME KIND, SAME ACTOR, INSIDE THE WINDOW. The SUBJECT is deliberately not compared:
        /// a tag reported by `MatchFlair` and by the score watcher can disagree about the victim
        /// for one frame while a body is being reseated, and treating those as two tags is the
        /// exact double-report this rule exists to stop.
        /// </summary>
        public static bool IsSameEvent(HighlightMarker previous, HighlightMarker candidate)
            => previous.Kind == candidate.Kind
               && previous.Actor == candidate.Actor
               && candidate.AtSeconds - previous.AtSeconds < SameEventSeconds
               && candidate.AtSeconds >= previous.AtSeconds;

        /// <summary>
        /// How much a director should care, 0 to 1.
        ///
        /// ⚠️⚠️ IT IS DERIVED FROM THE MEASUREMENT WHERE THERE IS ONE AND FLAT WHERE THERE IS
        /// NOT, and the flat ones say so rather than inventing a curve. `docs/TODO.md` § 147:
        /// "severity/importance where justified". A tag is a tag; a close call at 0.2 m is a
        /// different thing from one at 1.2 m and the number is right there.
        /// </summary>
        public static float ImportanceFor(HighlightKind kind, float measurement)
        {
            switch (kind)
            {
                case HighlightKind.CloseCall:
                case HighlightKind.ClutchRetrieval:
                    // Closer is bigger. 0 m is 1.0, the full radius is 0.
                    return Clamp01(1.0f - (measurement / CloseCallMetres));

                case HighlightKind.LongKnockdown:
                    // ⚠️ THE SCALE TOPS OUT AT TWICE THE BOX, which is the far kerb on both maps.
                    return Clamp01((measurement - LongKnockdownMetres)
                                   / LongKnockdownMetres);

                case HighlightKind.LastSecondKnockdown:
                case HighlightKind.LastSecondRetrieval:
                    // Later is bigger. On the whistle is 1.0.
                    return Clamp01(1.0f - (measurement / LastSecondSeconds));

                case HighlightKind.EvasionRun:
                    return Clamp01((measurement - EvasionRunCount + 1.0f) / EvasionRunCount);

                case HighlightKind.BankShot: return 0.75f;
                case HighlightKind.Ultimate: return 0.6f;
                case HighlightKind.Tag: return 0.5f;
                case HighlightKind.Block: return 0.45f;
                default: return 0.4f;
            }
        }

        /// <summary>
        /// The line a spectator ticker or a replay caption would print.
        ///
        /// ⚠️ IT NAMES THE NUMBER. `CLAUDE.md` § 2.3: *"An entry that says '40% of the arena'
        /// beats one that says 'too big'"*, and the same is true of a caption: "BANK SHOT" and
        /// "KNOCKDOWN FROM 11.4 m" are not the same sentence.
        /// </summary>
        public static string Describe(HighlightMarker m)
        {
            switch (m.Kind)
            {
                case HighlightKind.BankShot: return "BANK SHOT";
                case HighlightKind.LongKnockdown:
                    return $"KNOCKDOWN FROM {m.Measurement:0.0} m";
                case HighlightKind.CloseCall:
                    return $"CLOSE CALL, {m.Measurement:0.00} m";
                case HighlightKind.ClutchRetrieval:
                    return $"CLUTCH RETRIEVAL, {m.Measurement:0.00} m";
                case HighlightKind.LastSecondKnockdown:
                    return $"KNOCKDOWN WITH {m.Measurement:0.0} s LEFT";
                case HighlightKind.LastSecondRetrieval:
                    return $"RETRIEVAL WITH {m.Measurement:0.0} s LEFT";
                case HighlightKind.EvasionRun:
                    return $"{(int)m.Measurement} ESCAPES IN {EvasionWindowSeconds:0} s";
                case HighlightKind.Block: return "BLOCK";
                case HighlightKind.Tag: return "TAG";
                case HighlightKind.Ultimate: return "ULTIMATE";
                default: return m.Kind.ToString().ToUpperInvariant();
            }
        }

        private static float Clamp01(float v) => v < 0.0f ? 0.0f : (v > 1.0f ? 1.0f : v);
    }

    /// <summary>
    /// A bounded record of a match's own moments.
    ///
    /// ⚠️⚠️ BOUNDED, BECAUSE AN EIGHT-ROUND HERO STRIKE MATCH IS TWELVE MINUTES AND THIS RUNS
    /// FOR ALL OF IT. `FrameRateHistogram`'s header makes the same argument about frames: a
    /// growing list is memory reallocated through a dozen doublings while somebody is playing.
    /// A match's worth of genuinely distinct moments is tens rather than thousands, so the cap is
    /// generous and is still a cap.
    ///
    /// ⚠️ THE DEDUPE IS INSIDE `Add` AND NOT AT THE CALL SITES. Every producer would otherwise
    /// have to remember it, and `CLAUDE.md` § 4a is blunt about what happens to rules somebody
    /// has to remember.
    /// </summary>
    public sealed class HighlightLog
    {
        /// <summary>⚠️ 256 IS ABOUT SIX A ROUND FOR AN EIGHT-ROUND SET, which is already generous
        /// for events that survive the dedupe rule.</summary>
        public const int Capacity = 256;

        private readonly List<HighlightMarker> _markers = new List<HighlightMarker>(64);

        /// <summary>How many distinct moments have been recorded, including any dropped by the cap.</summary>
        public int Recorded { get; private set; }

        /// <summary>How many reports were folded into an existing marker.</summary>
        public int Deduplicated { get; private set; }

        public IReadOnlyList<HighlightMarker> Markers => _markers;

        /// <summary>
        /// Records a moment, unless it is another report of one already here.
        ///
        /// ⚠️ IT SCANS BACKWARDS AND STOPS AT THE WINDOW, so the cost is the number of markers
        /// inside `SameEventSeconds` rather than the length of the log.
        /// </summary>
        public bool Add(HighlightMarker marker)
        {
            for (int i = _markers.Count - 1; i >= 0; i--)
            {
                if (marker.AtSeconds - _markers[i].AtSeconds >= HighlightRules.SameEventSeconds)
                    break;

                if (HighlightRules.IsSameEvent(_markers[i], marker))
                {
                    Deduplicated++;
                    return false;
                }
            }

            _markers.Add(marker);
            Recorded++;

            if (_markers.Count > Capacity) _markers.RemoveAt(0);
            return true;
        }

        /// <summary>Everything recorded at or after <paramref name="seconds"/>, oldest first.</summary>
        public List<HighlightMarker> Since(float seconds)
        {
            var found = new List<HighlightMarker>();
            for (int i = 0; i < _markers.Count; i++)
                if (_markers[i].AtSeconds >= seconds) found.Add(_markers[i]);

            return found;
        }

        /// <summary>
        /// The window a replay of the newest marker would need, or false when there is none.
        ///
        /// ⚠️⚠️ THIS IS THE JOIN TO THE REPLAY AND IT IS DELIBERATELY THE WHOLE OF IT.
        /// `docs/TODO.md` § 147: *"the first useful version is gameplay event -> structured marker
        /// -> replay can identify that time window"*, and anything more (choosing between markers,
        /// cutting between cameras) is a broadcast director, which that entry says not to build.
        /// The lead-in and lead-out are the caller's, because `SpectatorCamera` already owns those
        /// two numbers and has measured them against the play.
        /// </summary>
        public bool NewestWindow(float leadIn, float leadOut,
                                 out float from, out float to, out HighlightMarker marker)
        {
            from = 0.0f;
            to = 0.0f;
            marker = default;

            if (_markers.Count == 0) return false;

            marker = _markers[_markers.Count - 1];
            from = Math.Max(0.0f, marker.AtSeconds - leadIn);
            to = marker.AtSeconds + leadOut;
            return true;
        }

        public void Clear()
        {
            _markers.Clear();
            Recorded = 0;
            Deduplicated = 0;
        }

        /// <summary>The whole log as lines, newest last. For a report or a bundle.</summary>
        public List<string> Report()
        {
            var lines = new List<string>(_markers.Count);
            foreach (var m in _markers)
                lines.Add($"{m.AtSeconds,7:F1}s  r{m.Round}  seat {m.Actor,2}  " +
                          $"{HighlightRules.Describe(m)}  (importance {m.Importance:0.00})");

            return lines;
        }
    }
}
