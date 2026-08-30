using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// Counts what happens in a session and sends it once, at the end.
    ///
    /// ⚠️⚠️ ONE BATCH PER SESSION, NEVER ONE CALL PER EVENT. `FUTURE.md` § 0.3's only hard rule
    /// about Cloud Code is *"call it once per match, never per event"*, and telemetry is the
    /// feature most able to break it: a Hero Strike match carries nine hundred passive-defence
    /// ticks and every one of them is a thing somebody could send. `TelemetryRules.Fold` collapses
    /// identical events into a count in memory and this class posts the totals.
    ///
    /// ⚠️ THERE ARE THREE FLUSH POINTS AND THAT IS STILL "ONCE PER SESSION" IN THE SENSE THAT
    /// MATTERS. Quit is the primary one. A finished match flushes too, because a crash after a
    /// match would otherwise cost the match-level numbers that match was the whole point of. A
    /// NEW funnel step flushes as well, and that one is bounded by construction: there are six
    /// steps in the whole funnel and an install passes each exactly once, ever. Each flush SENDS
    /// AND CLEARS, so nothing is ever counted twice.
    ///
    /// ⚠️⚠️ AND NOTHING HERE BLOCKS ANYTHING. Every send is fire and forget, every failure is a
    /// warning, and an unreachable service costs the numbers rather than the game. `FUTURE.md`
    /// § 0.5 rule 7: LAN and offline play may never sit behind a service, and telemetry is the
    /// last thing in this project that should be allowed to break that.
    ///
    /// ⚠️ NO IDENTIFIER IS EVER SENT. The endpoint keys everything on the authenticated session's
    /// player id, which a client cannot set, so there is no id in the payload to leak or to have
    /// to strip later. `TelemetryRules` refuses a parameter whose NAME means a person.
    /// </summary>
    public sealed class TelemetrySink : MonoBehaviour
    {
        private readonly Dictionary<string, TelemetryEvent> _buffer =
            new Dictionary<string, TelemetryEvent>();

        private float _sessionStartedAt;
        private bool _sending;

        /// <summary>How many batches this session has posted. Read by the probe.</summary>
        public int BatchesSent { get; private set; }

        /// <summary>How many events are waiting to go. Read by the probe and by tests.</summary>
        public int Pending => _buffer.Count;

        public static bool Enabled => SettingsStore.Current.TelemetryEnabled;

        private void Awake()
        {
            _sessionStartedAt = Time.realtimeSinceStartup;
            if (!Enabled) return;

            Note(TelemetryEvents.SessionStart);

            // ⚠️ THE FUNNEL OPENS HERE RATHER THAN ON THE SPLASH, because this object exists
            // before the first scene of the session whatever that scene is (`GameServices`), and
            // a funnel whose first step depends on somebody having opened the right scene cannot
            // measure a first launch at all.
            NoteFunnel(TelemetryEvents.FirstLaunch);
            NoteHardware();
        }

        // -------------------------------------------------------------------
        // COUNTING
        // -------------------------------------------------------------------

        /// <summary>Counts one occurrence of a known event.</summary>
        public void Note(string name, Dictionary<string, string> labels = null,
                         Dictionary<string, double> numbers = null)
        {
            if (!Enabled) return;

            var candidate = new TelemetryEvent { Name = name, Count = 1 };
            if (labels != null)
                foreach (var pair in labels) candidate.Labels[pair.Key] = pair.Value;
            if (numbers != null)
                foreach (var pair in numbers) candidate.Numbers[pair.Key] = pair.Value;

            if (!TelemetryRules.Fold(_buffer, candidate))
                Debug.Log($"[Telemetry] dropped '{name}': unknown, or the session buffer is full.");
        }

        /// <summary>
        /// Records a funnel step if this install has never reached it, and flushes if so.
        ///
        /// ⚠️ THE STEP IS WRITTEN TO DISK BEFORE THE SEND IS TRIED, for the reason
        /// `CareerStore`'s queue records: a process killed mid-request is the case durability
        /// exists for. The server is idempotent about funnel steps anyway (the first timestamp
        /// wins), so a step recorded locally and sent twice costs nothing, while a step sent and
        /// not recorded would be re-sent every launch forever.
        /// </summary>
        public void NoteFunnel(string step)
        {
            if (!Enabled) return;

            var settings = SettingsStore.Current;
            if (!TelemetryRules.IsNewFunnelStep(settings.TelemetryFunnelStep, step)) return;

            settings.TelemetryFunnelStep =
                TelemetryRules.FurthestFunnelStep(settings.TelemetryFunnelStep, step);
            SettingsStore.Save();

            Note(step);
            Flush("funnel");
        }

        /// <summary>
        /// ⚠️ A COARSE HARDWARE PICTURE, AND DELIBERATELY NOT A FINGERPRINT. `FUTURE.md` § 3 asks
        /// for "FPS distribution by hardware", which needs enough to tell a 6600 from an
        /// integrated part and nothing else. Core count and a memory bucket are properties of a
        /// machine, not of a person, and `TelemetryRules` would refuse a parameter named after
        /// one anyway.
        /// </summary>
        private void NoteHardware()
        {
            Note(TelemetryEvents.SettingsSnapshot,
                labels: new Dictionary<string, string>
                {
                    ["gpu"] = TelemetryRules.Label(GraphicsKey()),
                },
                numbers: new Dictionary<string, double>
                {
                    ["cores"] = SystemInfo.processorCount,
                    ["ram_gb"] = Mathf.Round(SystemInfo.systemMemorySize / 1024f),
                    ["screen_w"] = Screen.width,
                    ["screen_h"] = Screen.height,
                });
        }

        /// <summary>
        /// ⚠️ THE GPU NAME IS REDUCED TO A LABEL RATHER THAN SENT AS TYPED. Vendor strings carry
        /// spaces, brackets and revision suffixes, so as free text they would produce a hundred
        /// distinct values for a dozen real cards, and `TelemetryRules.Label` refuses anything
        /// with a space in it outright. This keeps letters and digits and joins the rest with
        /// underscores, which is what makes the column groupable.
        /// </summary>
        private static string GraphicsKey()
        {
            string raw = SystemInfo.graphicsDeviceName ?? "";
            var builder = new System.Text.StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                if (char.IsLetterOrDigit(c)) builder.Append(c);
                else if (builder.Length > 0 && builder[builder.Length - 1] != '_') builder.Append('_');
                if (builder.Length >= TelemetryRules.MaxParameterLength) break;
            }
            return builder.ToString().Trim('_');
        }

        // -------------------------------------------------------------------
        // THE EVENTS THE GAME ACTUALLY RAISES
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ THE FUNNEL STEP IS "SIGN-IN SETTLED", NOT "SIGNED IN". A player who reaches the
        /// menu on a local profile because the venue has no internet has not dropped out of the
        /// funnel; they have got past the step. Recording only the online case would make an
        /// unplugged hall read as a total sign-in failure, which is the exact opposite of what
        /// this project treats as working. The `online` label is what carries the difference.
        /// </summary>
        public void NoteSignInSettled(bool signedIn)
        {
            NoteFunnel(TelemetryEvents.FirstSignIn);
            Note(TelemetryEvents.SettingsSnapshot,
                labels: new Dictionary<string, string> { ["online"] = signedIn ? "yes" : "no" });

            if (signedIn) BackfillFunnel();
        }

        /// <summary>
        /// Re-notes every funnel step this install reached while it had nowhere to send them.
        ///
        /// ⚠️⚠️ THIS IS THE OFFLINE-FIRST CASE AND IT IS NOT AN EDGE CASE IN THIS PROJECT. A
        /// first launch in a hall with no internet records its steps locally and sends nothing.
        /// With one counter it would never send them again, and the server would report an
        /// install that got all the way to a finished LAN match as an install that never
        /// launched. That is the funnel `FUTURE.md` § 3 calls the most valuable number in the
        /// plan, wrong in the one venue the game is being built for.
        ///
        /// ⚠️ A RESENT STEP COSTS NOTHING. `telemetry.js` keeps the FIRST timestamp for a funnel
        /// step and ignores every later one, so this is safe to run on every signed-in session.
        /// </summary>
        private void BackfillFunnel()
        {
            var settings = SettingsStore.Current;
            for (int i = settings.TelemetryFunnelSent + 1;
                 i <= settings.TelemetryFunnelStep && i < TelemetryEvents.Funnel.Length;
                 i++)
                Note(TelemetryEvents.Funnel[i]);

            Flush("backfill");
        }

        public void NoteMenuReached() => NoteFunnel(TelemetryEvents.FirstMenu);

        public void NoteQueueOpened() => NoteFunnel(TelemetryEvents.FirstQueue);

        public void NoteMatchStarted(string mode, string map, int humans, int bots)
        {
            NoteFunnel(TelemetryEvents.FirstMatchStarted);
            Note(TelemetryEvents.MatchStarted,
                labels: new Dictionary<string, string>
                {
                    ["mode"] = TelemetryRules.Label(mode),
                    ["map"] = TelemetryRules.Label(map),
                },
                numbers: new Dictionary<string, double> { ["seats"] = humans, ["bots"] = bots });
        }

        /// <summary>
        /// ⚠️ THE PICK RATES ARE ONE EVENT PER SEAT PER MATCH, NOT ONE PER SELECTION CHANGE.
        /// `FUTURE.md` § 3 asks for "character and tsinelas pick and win rates", and a player who
        /// cycles the roster twelve times before starting has picked once. Counting the cycling
        /// would make the most-browsed character look like the most-played one.
        /// </summary>
        public void NotePick(string mode, string character, string slipper)
        {
            Note(TelemetryEvents.Pick,
                labels: new Dictionary<string, string>
                {
                    ["mode"] = TelemetryRules.Label(mode),
                    ["character"] = TelemetryRules.Label(character),
                    ["slipper"] = TelemetryRules.Label(slipper),
                });
        }

        /// <summary>
        /// ⚠️ THE EVENT IS BUFFERED BEFORE THE FUNNEL STEP, AND THE ORDER IS LOAD-BEARING.
        /// `NoteFunnel` flushes when the step is new, and `Flush` sends and clears; noting the
        /// step first would send a batch that does not contain the match this method was called
        /// for, and `_sending` would then make the second flush a no-op, leaving the match event
        /// buffered until quit. One flush, everything in it.
        /// </summary>
        public void NoteMatchFinished(string mode, string map, int rounds, float seconds, int placement)
        {
            Note(TelemetryEvents.MatchFinished,
                labels: new Dictionary<string, string>
                {
                    ["mode"] = TelemetryRules.Label(mode),
                    ["map"] = TelemetryRules.Label(map),
                },
                numbers: new Dictionary<string, double>
                {
                    ["rounds"] = rounds,
                    ["seconds"] = Math.Round(seconds, 1),
                    ["placement"] = placement,
                });

            // The step flushes when it is new; the explicit flush covers every later match.
            NoteFunnel(TelemetryEvents.FirstMatchFinished);
            Flush("match");
        }

        /// <summary>
        /// ⚠️⚠️ LEAVE RATE BY ROUND IS THE ONE MATCH NUMBER THAT CANNOT BE RECONSTRUCTED LATER.
        /// `FUTURE.md` § 3 asks for it by name. A finished match is in the career history and
        /// could be recounted from there; a match somebody walked out of round two of leaves no
        /// record anywhere else in this project, and it is the number that says whether the
        /// eight-round Hero Strike set is too long.
        /// </summary>
        public void NoteMatchLeft(string mode, int round)
        {
            Note(TelemetryEvents.MatchLeft,
                labels: new Dictionary<string, string> { ["mode"] = TelemetryRules.Label(mode) },
                numbers: new Dictionary<string, double> { ["round"] = round });
        }

        public void NoteDisconnect(string reasonClass)
        {
            Note(TelemetryEvents.Disconnect,
                labels: new Dictionary<string, string> { ["reason"] = TelemetryRules.Label(reasonClass) });
        }

        // -------------------------------------------------------------------
        // SENDING
        // -------------------------------------------------------------------

        private void OnApplicationQuit()
        {
            if (!Enabled) return;

            Note(TelemetryEvents.SessionEnd, numbers: new Dictionary<string, double>
            {
                ["seconds"] = Math.Round(Time.realtimeSinceStartup - _sessionStartedAt, 1),
            });

            // ⚠️⚠️ THE QUIT FLUSH IS FIRE AND FORGET AND USUALLY LOSES THE RACE, AND THAT IS
            // ACCEPTED RATHER THAN OVERLOOKED. Unity tears the process down without waiting for
            // a `Task`, so a batch started here often never reaches the wire. Blocking quit on a
            // network call to fix it would mean a game that hangs for several seconds on exit
            // whenever the venue Wi-Fi is bad, which is a real cost to a real player in exchange
            // for a number. The match flush above is what makes that trade survivable: everything
            // worth having has usually already gone.
            //
            // ⚠️ A CRASH IS MEASURED AS THE ABSENCE OF THIS EVENT, which is why it is sent at all
            // and why it is not worth much effort to guarantee. `session_start` minus
            // `session_end` on the server is the crash-and-kill rate `FUTURE.md` § 3 asks for,
            // and neither half of that subtraction needs to be perfect to be useful.
            Flush("quit");
        }

        /// <summary>Sends everything buffered and clears it. Safe to call at any time.</summary>
        public void Flush(string reason)
        {
            if (!Enabled || _sending || _buffer.Count == 0) return;

            var account = GameServices.Account;
            if (account == null || !account.IsSignedIn) return;

            var payload = new List<object>(_buffer.Count);
            int funnelInFlight = SettingsStore.Current.TelemetryFunnelSent;

            foreach (var candidate in _buffer.Values)
            {
                var args = new Dictionary<string, object>();
                foreach (var pair in candidate.Labels) args[pair.Key] = pair.Value;
                foreach (var pair in candidate.Numbers) args[pair.Key] = pair.Value;
                payload.Add(new { Name = candidate.Name, Count = candidate.Count, Params = args });

                funnelInFlight = TelemetryRules.FurthestFunnelStep(funnelInFlight, candidate.Name);
            }

            _buffer.Clear();
            _ = SendAsync(payload, reason, funnelInFlight);
        }

        private async Task SendAsync(List<object> events, string reason, int funnelInFlight)
        {
            _sending = true;
            try
            {
                // ⚠️⚠️ THE BATCH GOES AS A JSON **STRING**, NOT AS AN ARRAY. `docs/TODO.md`
                // § 90.5: declaring the parameter as `JSON` made the service drop the entire
                // parameter block, so `action` disappeared with it and every call landed on the
                // default branch with no error anywhere. `String` is the only type these scripts
                // have proven, and `CareerStore` already sends its whole record this way.
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(events);
                await CloudCode.CallAsync("telemetry", new { action = "submit", events = json });
                BatchesSent++;

                // ⚠️ THE DELIVERY MARK MOVES ONLY ON SUCCESS, AND ONLY AS FAR AS THIS BATCH
                // ACTUALLY CARRIED. Moving it optimistically would lose exactly the funnel steps
                // `BackfillFunnel` exists to rescue, and moving it to the local step would claim
                // delivery for steps still sitting in a buffer this batch did not include.
                var settings = SettingsStore.Current;
                if (funnelInFlight > settings.TelemetryFunnelSent)
                {
                    settings.TelemetryFunnelSent = funnelInFlight;
                    SettingsStore.Save();
                }
            }
            catch (Exception e)
            {
                // ⚠️ A LOST BATCH IS NOT RETRIED AND IS NOT QUEUED, WHICH IS THE OPPOSITE OF
                // WHAT `CareerStore` DOES WITH A MATCH RECORD, ON PURPOSE. A career is the
                // player's; losing it is a thing they would notice and mind. Telemetry is ours,
                // it is a sample rather than a ledger, and a disk queue for it would spend the
                // player's storage and a future session's bandwidth on a number that is only
                // worth having in aggregate.
                Debug.LogWarning($"[Telemetry] {reason} batch not delivered: {e.Message}");
            }
            finally
            {
                _sending = false;
            }
        }
    }
}
