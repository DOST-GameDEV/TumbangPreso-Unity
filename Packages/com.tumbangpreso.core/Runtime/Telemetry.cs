using System;
using System.Collections.Generic;
using System.Text;

namespace TumbangPreso.Core
{
    /// <summary>
    /// The event names this game will ever send, and the rules about what may travel with them.
    ///
    /// ⚠️⚠️ THESE NAMES ARE A CONTRACT AND RENAMING ONE IS A BROKEN HISTORY. `FUTURE.md` § 19.3
    /// says it in one line: *"a renamed event is a broken history"*. Nothing errors when a name
    /// changes. What happens instead is that a counter silently restarts at zero, the series it
    /// used to belong to stops growing, and the two halves can never be joined again because
    /// nothing recorded that they were the same question. Choose a name once.
    ///
    /// ⚠️⚠️ `Funnel` IS ORDERED AND APPEND-ONLY, AND THE ORDER IS THE MEANING. "How far did this
    /// player get" is an index comparison, so inserting a step in the middle silently rewrites
    /// what every already-stored profile is claiming: a player recorded at step 3 becomes a
    /// player who reached a step that did not exist when they played. Add to the END, or add a
    /// second funnel. This is `FUTURE.md` § 0.5 rule 5, which `Roster.Slippers` records for the
    /// wire, applied to a list that crosses time instead of a network.
    ///
    /// ⚠️ MIRRORED IN `ugs/cloud-code/telemetry.js`, WHICH REFUSES A NAME IT DOES NOT KNOW.
    /// `docs/TODO.md` § 90.3 is the contract in prose and § 89.6 is the standing argument for why
    /// a file written twice is the accepted cost here. `CareerAndCloudCodeTests` gates the two.
    /// </summary>
    public static class TelemetryEvents
    {
        // The first-launch funnel, in order. `FUTURE.md` § 3: "the most valuable number in this
        // document". It answers where a person who has never played this game stops.
        public const string FirstLaunch = "first_launch";
        public const string FirstSignIn = "first_sign_in";
        public const string FirstMenu = "first_menu";
        public const string FirstQueue = "first_queue";
        public const string FirstMatchStarted = "first_match_started";
        public const string FirstMatchFinished = "first_match_finished";

        // Everything else, which is counted every time rather than once.
        public const string SessionStart = "session_start";
        public const string SessionEnd = "session_end";
        public const string MatchStarted = "match_started";
        public const string MatchFinished = "match_finished";
        public const string MatchLeft = "match_left";
        public const string Pick = "pick";
        public const string SettingsSnapshot = "settings_snapshot";
        public const string Disconnect = "disconnect";

        // ⚠️ ADDED 2026-08-30, AT THE END, WHICH IS THE ONLY SAFE PLACE. `All` is compared
        // against `telemetry.js` by content rather than order, but `Funnel` is compared by both
        // and the two lists are read side by side; appending is the habit that keeps them
        // readable together. This one is not a funnel step and must never become one: it is
        // raised once per FINISHED match, and a funnel position is a thing an install passes
        // exactly once, ever. `docs/TODO.md` § 90.3.
        public const string MatchFrameRate = "match_frame_rate";

        public static readonly string[] Funnel =
        {
            FirstLaunch,
            FirstSignIn,
            FirstMenu,
            FirstQueue,
            FirstMatchStarted,
            FirstMatchFinished,
        };

        public static readonly string[] All =
        {
            FirstLaunch, FirstSignIn, FirstMenu, FirstQueue, FirstMatchStarted, FirstMatchFinished,
            SessionStart, SessionEnd, MatchStarted, MatchFinished, MatchLeft,
            Pick, SettingsSnapshot, Disconnect, MatchFrameRate,
        };
    }

    /// <summary>One counted thing, with the short labels and numbers that describe it.</summary>
    public sealed class TelemetryEvent
    {
        public string Name = "";
        public int Count = 1;
        public readonly Dictionary<string, string> Labels = new Dictionary<string, string>();
        public readonly Dictionary<string, double> Numbers = new Dictionary<string, double>();

        /// <summary>
        /// Two events fold together when they are the same question about the same thing.
        ///
        /// ⚠️ THE NUMBERS ARE PART OF THE KEY AND THAT IS DELIBERATE. Folding `match_finished`
        /// with a duration of 340 s into one with a duration of 512 s would produce a count of
        /// two carrying one of the two durations, and the server averages what it is sent. A
        /// distinct duration is a distinct row; only genuinely identical events collapse.
        /// </summary>
        public string Signature()
        {
            var builder = new StringBuilder(Name);
            AppendSorted(builder, Labels, value => value);
            AppendSorted(builder, Numbers, value => value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        private static void AppendSorted<T>(StringBuilder builder, Dictionary<string, T> map,
                                            Func<T, string> render)
        {
            if (map.Count == 0) return;
            var keys = new List<string>(map.Keys);
            keys.Sort(StringComparer.Ordinal);
            foreach (string key in keys)
            {
                builder.Append('|').Append(key).Append('=').Append(render(map[key]));
            }
        }
    }

    /// <summary>
    /// What may be sent, how much of it, and how far through the funnel somebody has got.
    ///
    /// ⚠️⚠️ NO PERSONALLY IDENTIFYING FIELD MAY EVER REACH AN EVENT, and this is the half of that
    /// rule that runs on the machine the player owns. `ugs/cloud-code/telemetry.js` enforces the
    /// same rule again, because a client is the half somebody can edit. Both halves refuse the
    /// same shapes: a parameter is a bucket label like `hero_strike` or a number, never free text
    /// somebody typed, and no event carries an identifier at all. The server already knows who
    /// called it from the authenticated session, so there is nothing here to get wrong.
    /// </summary>
    public static class TelemetryRules
    {
        // Mirrored in `telemetry.js`. `CareerAndCloudCodeTests` fails if these split.
        public const int MaxEventsPerBatch = 64;
        public const int MaxParametersPerEvent = 8;
        public const int MaxParameterLength = 32;

        /// <summary>
        /// ⚠️⚠️ A FIELD NAME THAT MEANS A PERSON IS REFUSED WHATEVER IT HOLDS. This list is
        /// deliberately about NAMES rather than values, because a value cannot be inspected for
        /// whether it identifies somebody and a name can. It is the cheap check that stops the
        /// expensive mistake: the first person to add `player_name` to an event for debugging
        /// would otherwise ship it, and telemetry is the one system where a mistake is retained
        /// rather than transient.
        /// </summary>
        private static readonly string[] RefusedParameterFragments =
        {
            "name", "email", "player", "account", "handle", "token",
            "address", "serial", "path", "device", "machine", "profile", "session",
        };

        /// <summary>
        /// ⚠️⚠️ THE SHORT ONES ARE MATCHED WHOLE OR AS A SUFFIX, NEVER AS A SUBSTRING, AND THAT
        /// SPLIT IS A MEASURED BUG RATHER THAN A REFINEMENT. `ip` as a fragment refuses
        /// **`slipper`**, which is the tsinelas pick rate: `FUTURE.md` § 3 asks for it by name,
        /// the column would have been stripped on every event, and nothing anywhere would have
        /// said so. `id` as a fragment is the same trap one letter shorter. A two-letter
        /// substring rule is a rule that refuses words it has never heard of.
        /// </summary>
        private static readonly string[] RefusedParameterWords =
        {
            "id", "ip", "mac", "uid", "guid", "user", "uuid",
        };

        public static bool IsKnownEvent(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            foreach (string known in TelemetryEvents.All)
                if (known == name) return true;
            return false;
        }

        /// <summary>Position in the funnel, or -1 for an event that is not a funnel step.</summary>
        public static int FunnelIndex(string name)
        {
            for (int i = 0; i < TelemetryEvents.Funnel.Length; i++)
                if (TelemetryEvents.Funnel[i] == name) return i;
            return -1;
        }

        /// <summary>
        /// How far this install has got, given where it had got to and what just happened.
        ///
        /// ⚠️ IT ONLY EVER GOES FORWARD. A funnel that can go backwards is not a funnel: a player
        /// who reaches the menu after finishing a match would otherwise be recorded as having
        /// stopped at the menu, and every conversion rate below that step would be wrong in the
        /// flattering direction.
        /// </summary>
        public static int FurthestFunnelStep(int reached, string name)
        {
            int index = FunnelIndex(name);
            return index > reached ? index : reached;
        }

        /// <summary>Whether a funnel step is new for an install that has reached <paramref name="reached"/>.</summary>
        public static bool IsNewFunnelStep(int reached, string name)
        {
            int index = FunnelIndex(name);
            return index >= 0 && index > reached;
        }

        public static bool IsSafeParameterName(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length > MaxParameterLength) return false;
            if (key[0] < 'a' || key[0] > 'z') return false;

            foreach (char c in key)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
                if (!ok) return false;
            }

            foreach (string fragment in RefusedParameterFragments)
                if (key.Contains(fragment)) return false;

            foreach (string word in RefusedParameterWords)
                if (key == word || key.EndsWith("_" + word) || key.StartsWith(word + "_"))
                    return false;

            return true;
        }

        /// <summary>
        /// A short, boring bucket label, or empty for anything that is not one.
        ///
        /// ⚠️ IT REFUSES RATHER THAN TRUNCATES. `AccountRules.OneLine` truncates a display name
        /// because a clipped name is still that person's name; a clipped telemetry label is a new
        /// value that will never join to the one it came from, which is the same broken history
        /// a renamed event produces.
        /// </summary>
        public static string Label(string raw)
        {
            if (string.IsNullOrEmpty(raw) || raw.Length > MaxParameterLength) return "";
            foreach (char c in raw)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                          (c >= '0' && c <= '9') || c == '_' || c == '.' || c == '-';
                if (!ok) return "";
            }
            return raw;
        }

        /// <summary>
        /// Strips everything the contract does not allow, and answers whether anything is left
        /// worth sending. Refusing the whole event is the right answer for an unknown name; a
        /// refused PARAMETER only costs a column.
        /// </summary>
        public static bool Accept(TelemetryEvent candidate)
        {
            if (candidate == null || !IsKnownEvent(candidate.Name)) return false;
            if (candidate.Count < 1) candidate.Count = 1;

            Prune(candidate.Labels, key => IsSafeParameterName(key));
            Prune(candidate.Numbers, key => IsSafeParameterName(key));

            foreach (var pair in new List<KeyValuePair<string, string>>(candidate.Labels))
                if (Label(pair.Value).Length == 0) candidate.Labels.Remove(pair.Key);

            foreach (var pair in new List<KeyValuePair<string, double>>(candidate.Numbers))
                if (double.IsNaN(pair.Value) || double.IsInfinity(pair.Value))
                    candidate.Numbers.Remove(pair.Key);

            // ⚠️ THE PARAMETER CAP DROPS THE EXTRAS RATHER THAN THE EVENT. An event with nine
            // columns is a design mistake somebody should fix; an event silently not sent is a
            // hole in a history nobody notices for a month.
            TrimTo(candidate.Labels, MaxParametersPerEvent);
            TrimTo(candidate.Numbers, MaxParametersPerEvent - candidate.Labels.Count);

            return true;
        }

        private static void Prune<T>(Dictionary<string, T> map, Func<string, bool> keep)
        {
            foreach (var pair in new List<KeyValuePair<string, T>>(map))
                if (!keep(pair.Key)) map.Remove(pair.Key);
        }

        private static void TrimTo<T>(Dictionary<string, T> map, int limit)
        {
            if (limit < 0) limit = 0;
            if (map.Count <= limit) return;

            var keys = new List<string>(map.Keys);
            keys.Sort(StringComparer.Ordinal);
            for (int i = limit; i < keys.Count; i++) map.Remove(keys[i]);
        }

        /// <summary>
        /// Folds an event into a session's buffer, merging identical ones into a count.
        ///
        /// ⚠️⚠️ THIS IS THE WHOLE REASON TELEMETRY DOES NOT COST ONE CALL PER EVENT. `FUTURE.md`
        /// § 0.3's rule about Cloud Code is *"call it once per match, never per event"*, and
        /// telemetry is the feature most able to break it: a Hero Strike match carrying nine
        /// hundred passive-defence ticks is nine hundred things somebody could send. The buffer
        /// counts locally and one batch goes out per session.
        ///
        /// ⚠️ AND THE BUFFER IS BOUNDED. A session left running overnight in a lobby must not
        /// grow a list until the process dies; past the cap the counts on events already in the
        /// buffer keep rising and no new SHAPE is added, so the numbers stay right for everything
        /// that was already interesting.
        /// </summary>
        public static bool Fold(Dictionary<string, TelemetryEvent> buffer, TelemetryEvent candidate)
        {
            if (buffer == null || !Accept(candidate)) return false;

            string signature = candidate.Signature();
            if (buffer.TryGetValue(signature, out TelemetryEvent existing))
            {
                existing.Count += candidate.Count;
                return true;
            }

            if (buffer.Count >= MaxEventsPerBatch) return false;
            buffer[signature] = candidate;
            return true;
        }
    }
}
