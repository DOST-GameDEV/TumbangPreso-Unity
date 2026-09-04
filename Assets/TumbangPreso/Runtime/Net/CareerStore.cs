using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// The player's career: the profile, the match history, and the queue of records that have
    /// not reached the server yet.
    ///
    /// ⚠️⚠️ THE SERVER IS THE AUTHORITY AND THIS IS A CACHE. `FUTURE.md` § 0.5 rule 6: stats are
    /// *"written by a Cloud Code endpoint computed from a match record, never sent by a client"*.
    /// So `ugs/cloud-code/match-record.js` accumulates the real profile, and everything this
    /// class computes locally is an optimistic copy that exists so the profile screen works with
    /// the internet unplugged. Whenever the endpoint answers, its profile REPLACES this one. The
    /// two can disagree; the server wins, every time, with no merge.
    ///
    /// ⚠️⚠️ AND THAT IS WHY `ProfileRules` IS WRITTEN TWICE, ONCE HERE IN C# AND ONCE IN JS. It
    /// is the same trade `ugs/cloud-code/player-account.js` records about `DisplayNameMax`: the
    /// script cannot import the C# and the C# cannot run in Cloud Code. The C# copy has the tests
    /// and is the specification; if the two ever disagree the JS is the bug, and the symptom is a
    /// career that changes the moment a player comes back online.
    ///
    /// ⚠️⚠️ OFFLINE IS A FIRST-CLASS PATH, NOT AN ERROR PATH. `FUTURE.md` § 0.5 rule 7 and the
    /// nationals in General Santos City: a four-player match must stay playable and completable
    /// with the internet unplugged. A match played that way updates the local profile, lands in
    /// the local history, and joins the queue. Nothing here ever blocks a match, a menu or a
    /// boot: every remote call is fired and forgotten, and every failure is a warning.
    /// </summary>
    public sealed class CareerStore : MonoBehaviour
    {
        public const string ScriptName = "match-record";

        /// <summary>
        /// ⚠️ THE QUEUE IS CAPPED SO A MACHINE THAT NEVER SIGNS IN CANNOT FILL A DISK. Twenty
        /// matches is about a full evening of Hero Strike, and the oldest is dropped rather than
        /// the newest: a player coming back online cares about what they just played.
        /// </summary>
        public const int QueueLimit = 20;

        public static CareerStore Instance { get; private set; }

        /// <summary>
        /// The id this machine's player carries in a <see cref="MatchRecord"/>, and the only
        /// answer to "which line in this record is mine".
        ///
        /// ⚠️⚠️ ONE OWNER PER FACT, AND THIS ONE HAD FOUR OWNERS THAT ALL AGREED ON THE WRONG
        /// VALUE. `MatchStatsCollector.IdentifySeats`, `CareerStore.Record`, `MatchResult` and
        /// `PlayerHub` each wrote `Account?.ConnectionToken ?? NetIdentity.Token` out by hand, so
        /// the local screens agreed with the local record and every one of them disagreed with
        /// the server, which compares against `context.playerId`. Four consistent copies of a
        /// wrong id is exactly the shape that hides for two phases: nothing on this machine can
        /// see it, and the only symptom is a 422 in a log nobody reads.
        /// `MatchStatsCollector.IdentifySeats` carries the full note; `docs/TODO.md` § 94.1.
        ///
        /// ⚠️ THE FALLBACK IS `NetIdentity.Token` AND IT IS FOR THE OFFLINE CASE ONLY. It is not
        /// an account id and the endpoint will refuse a record stamped with it; `FlushAsync` drops
        /// such a record rather than retrying it, and says so.
        /// </summary>
        public static string LocalPlayerId
        {
            get
            {
                string id = GameServices.Account?.PlayerId;
                return string.IsNullOrWhiteSpace(id) ? NetIdentity.Token : id;
            }
        }

        [Serializable]
        private sealed class Cache
        {
            public PlayerProfile Profile = new PlayerProfile();
            public List<MatchRecord> History = new List<MatchRecord>();
            public List<MatchRecord> Queue = new List<MatchRecord>();

            /// <summary>
            /// One witness digest per queued record, same index.
            ///
            /// ⚠️⚠️ A PARALLEL LIST RATHER THAN A WRAPPER TYPE, AND THE REASON IS THE FILE
            /// ON DISK. `career.json` is written with `JsonUtility` and every player already has
            /// one; turning `Queue` into a list of a new type would fail to deserialise the old
            /// shape and silently drop whatever was waiting to upload, which is exactly the loss
            /// `docs/TODO.md` § 94.1 spent an entry on. An added list is absent in an old file and
            /// pads to empty, which reads as "this record has no witness", which is true.
            ///
            /// ⚠️ `PadWitnesses` KEEPS THE TWO IN STEP AND IS CALLED BEFORE EVERY READ. Two
            /// lists that can disagree about their own length is the price of the paragraph above,
            /// and one function that fixes it is the mitigation.
            /// </summary>
            public List<string> QueueWitness = new List<string>();

            /// <summary>Which account this cache belongs to. See <see cref="Load"/>.</summary>
            public string OwnerId = "";

            /// <summary>
            /// Set while a MULTIPLAYER match is running and cleared when its record is adopted.
            ///
            /// ⚠️⚠️ THIS ONE FIELD IS THE WHOLE LEAVER PENALTY, AND IT WORKS BECAUSE IT IS
            /// WRITTEN TO DISK. `FUTURE.md` § 19.8 step 3 asks for leaver penalties that
            /// distinguish a leave from a disconnect, and the obvious implementation, reporting an
            /// abandon when the player presses QUIT, penalises only the people polite enough to
            /// press it: alt-F4 would be free, so the penalty would fall entirely on honest
            /// players. **A flag on disk survives alt-F4, a crash and a power cut alike**, so the
            /// next launch finds a match that was started and never finished, and reports it.
            ///
            /// ⚠️ AND IT IS WHY A RECONNECT IS NOT PUNISHED. Coming back and playing to the
            /// whistle reaches `Record`, which clears this, so the only thing that survives to the
            /// next boot is a match this machine genuinely walked out of.
            /// `IntegrityRules.ReconnectWindowSeconds` is the same promise on the host's side.
            ///
            /// ⚠️ PRACTICE AND SOLO NEVER SET IT. Leaving a match against three bots costs
            /// nobody anything, and a cooldown for closing a practice game would be the single
            /// most infuriating thing in the build.
            /// </summary>
            public string InMatchSinceUtc = "";
        }

        [Serializable]
        private sealed class SubmitResponse
        {
            public string profile;
            public bool applied;

            /// <summary>`witnessed`, `pending`, `disputed` or `impossible`. Absent on a `load`.</summary>
            public string verdict;
        }

        [Serializable]
        private sealed class HistoryResponse
        {
            public string history;
            public int total;
        }

        private Cache _cache = new Cache();
        private bool _flushing;
        private bool _refreshing;

        /// <summary>Raised whenever the profile or the history changed, from any cause.</summary>
        public event Action Changed;

        public PlayerProfile Profile => _cache.Profile;
        public IReadOnlyList<MatchRecord> History => _cache.History;
        public int QueuedCount => _cache.Queue.Count;

        /// <summary>
        /// What the last counted match paid, for the results board. Null until one has been.
        ///
        /// ⚠️⚠️ THE CLIENT COMPUTES THIS AND THE SERVER DECIDES IT. `ugs/cloud-code/match-record.js`
        /// runs the same `Award` against the stored career, per `FUTURE.md` 0.5 rule 6, and what
        /// it writes is what the profile ends up holding. This exists so the end-of-match bar can
        /// move in the second before the endpoint answers, and because an unplugged LAN match
        /// still has to show a player what they earned (rule 7). If the two ever disagree, the
        /// server is right and the disagreement is the bug.
        ///
        /// ⚠️ IT IS NULL FOR A MATCH THAT DID NOT COUNT. `ProfileRules.Apply` returns false
        /// for a replayed record and for a spectated one, and a board that kept the previous
        /// award would animate the last match XP onto this one.
        /// </summary>
        public XpAward LastAward { get; private set; }

        /// <summary>What the last remote call had to say, for the profile screen's status line.</summary>
        public string Status { get; private set; } = "Local career";

        /// <summary>
        /// What the endpoint said about the last match submitted: `witnessed`, `pending`,
        /// `disputed` or `impossible`.
        ///
        /// ⚠️ THE END-OF-MATCH BOARD IS THE ONLY THING THAT READS IT, AND ONLY TO SAY ONE
        /// SENTENCE ONCE. `FUTURE.md` § 0.5b, phase 8 row: the surface this phase owes is "almost
        /// nothing, deliberately" and the one thing on it is "a result that is disputed says so,
        /// once". A pending result says nothing, because pending is the normal state of a match
        /// whose other players have not closed their game yet.
        /// </summary>
        public string LastVerdict { get; private set; } = "";

        public static string Path =>
            System.IO.Path.Combine(Application.persistentDataPath, "career.json");

        private void Awake()
        {
            Instance = this;
            Load();

            var account = GameServices.Account;
            if (account != null) account.Changed += OnAccountChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            var account = GameServices.Account;
            if (account != null) account.Changed -= OnAccountChanged;
        }

        // -------------------------------------------------------------------
        // § THE DISK
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ A CACHE BELONGING TO A DIFFERENT ACCOUNT IS DISCARDED, NOT MERGED. Two people
        /// share this machine at a tournament, or one person signs out and another signs in, and
        /// the career on disk is the previous owner's. Merging would hand somebody else's
        /// knockdowns to whoever signs in next, which is worse than losing an offline queue.
        /// `OwnerId` is empty on a fresh install and adopts the first account to write, so a
        /// player who has never signed in keeps everything they played offline.
        /// </summary>
        private void Load()
        {
            _cache = new Cache();

            try
            {
                // ⚠️ THE BACKUP IS TRIED BEFORE "STARTING EMPTY", which is the half that makes
                // the atomic write worth having. Starting empty is the correct LAST resort and a
                // terrible first one: a career is the one player file nothing can regenerate.
                string json = SafeStore.Read(Path,
                    text => JsonUtility.FromJson<Cache>(text) != null);

                if (json != null)
                {
                    var loaded = JsonUtility.FromJson<Cache>(json);
                    if (loaded != null) _cache = loaded;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Career] could not read {Path}, starting empty: {e.Message}");
            }

            _cache.Profile ??= new PlayerProfile();
            _cache.History ??= new List<MatchRecord>();
            _cache.Queue ??= new List<MatchRecord>();
            _cache.Profile.AppliedMatchIds ??= new List<string>();
            _cache.Profile.Modes ??= new List<ModeRecord>();
            _cache.Profile.Characters ??= new List<PickRecord>();
            _cache.Profile.Slippers ??= new List<PickRecord>();

            // ⚠️ A CAREER OFF DISK IS AS UNTRUSTED AS ONE OFF THE WIRE. It was written by
            // whatever build was installed last, and the profile screen indexes the
            // placement array by `Balance.PlayerCount`.
            ProfileRules.Normalise(_cache.Profile);
        }

        private void Save()
        {
            // ⚠️ SEE `SafeStore`. A career file is the one player file that cannot be
            // regenerated from anything, so a truncated save costs a real history.
            SafeStore.Write(Path, JsonUtility.ToJson(_cache));
        }

        private void OnAccountChanged()
        {
            string id = GameServices.Account?.PlayerId ?? "";
            if (string.IsNullOrEmpty(id)) return;

            if (string.IsNullOrEmpty(_cache.OwnerId))
            {
                _cache.OwnerId = id;
                Save();
            }
            else if (_cache.OwnerId != id)
            {
                Debug.Log("[Career] the cached career belongs to another account; starting a fresh one.");
                _cache = new Cache { OwnerId = id };
                Save();
                Changed?.Invoke();
            }

            _ = SyncAsync();
        }

        // -------------------------------------------------------------------
        // § ONE MATCH
        // -------------------------------------------------------------------

        /// <summary>
        /// Takes one finished match. Called on EVERY peer, with the host's own record.
        ///
        /// ⚠️⚠️ IT SUBMITS THIS PLAYER'S LINE AND NOBODY ELSE'S, WHICH IS A DEPARTURE FROM THE
        /// LETTER OF `FUTURE.md` § 19.2 AND IS WRITTEN UP IN `docs/TODO.md` § 89.3. The prompt
        /// says the host writes the record, and it still authors every number in it, so the hole
        /// § 2.3 names is exactly where § 2.3 left it: a host can lie about what happened.
        /// Letting the host also WRITE three other people's career documents is a second hole and
        /// a much worse one, because it is the difference between spoofing a match you were in
        /// and editing a stranger's account. Each peer calls the endpoint from its own
        /// authenticated session and the endpoint applies only `context.playerId`.
        ///
        /// ⚠️ IT IS ONE ENDPOINT CALL PER MATCH PER PLAYER, NEVER ONE PER EVENT, which is what
        /// § 0.3 and § 19.2 step 3 are actually protecting: a four-player Hero Strike match with
        /// nine hundred passive-defence ticks in it costs four calls.
        /// </summary>
        /// <summary>
        /// File a report against another player.
        ///
        /// ⚠️ IT IS FIRE AND FORGET AND IT SAYS SO. The player has already been told REPORTED
        /// by the button that called this; an error toast for a report that failed to upload would
        /// be a second sentence about somebody else's behaviour on a screen that should be about
        /// the match that just finished. The endpoint rate-limits at ten a day.
        /// </summary>
        public async void Report(string playerId, ReportReason reason)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return;
            if (!(GameServices.Account?.IsSignedIn ?? false)) return;

            try
            {
                await CloudCode.CallAsync(ScriptName,
                    new { action = "report", playerId = playerId, reason = (int)reason });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Career] report not delivered: {e.Message}");
            }
        }

        public void Record(MatchRecord record) => Record(record, "");

        /// <summary>
        /// Take a finished record into the career and queue it, with this peer's own witness
        /// digest of the same match.
        ///
        /// ⚠️⚠️ THE DIGEST IS THIS PEER'S TALLY AND NOT A HASH OF THE RECORD. See
        /// `ScoreWitness`: hashing the record would hash the host's JSON on all four machines.
        /// An empty string is the honest answer for a peer that did not see the whole match, and
        /// the endpoint treats it as silence rather than as a disagreement.
        /// </summary>
        /// <summary>
        /// Remember that a multiplayer match is running, so walking out of it is noticed.
        ///
        /// ⚠️ IT SAVES IMMEDIATELY. The entire value of the flag is that it is on disk before
        /// the thing it is protecting against happens.
        /// </summary>
        public void NoteMatchStarted(bool multiplayerWithOtherHumans)
        {
            if (!multiplayerWithOtherHumans) return;

            _cache.InMatchSinceUtc = DateTime.UtcNow.ToString("o");
            Save();
        }

        /// <summary>
        /// Report a match this machine started and never finished, once, on the next launch.
        ///
        /// ⚠️⚠️ THE FLAG IS CLEARED BEFORE THE CALL IS TRIED, WHICH IS THE OPPOSITE OF WHAT
        /// THE UPLOAD QUEUE DOES AND IS DELIBERATE. A queued match record must survive a failed
        /// send, because losing it loses a match somebody played. An abandon that fails to send is
        /// a penalty nobody received, and retrying it for ever would mean one offline evening
        /// becoming a cooldown that arrives days later attached to nothing the player remembers.
        /// **A missed penalty is the right failure.**
        /// </summary>
        public async Task ReportAbandonIfAnyAsync()
        {
            if (string.IsNullOrEmpty(_cache.InMatchSinceUtc)) return;
            if (!(GameServices.Account?.IsSignedIn ?? false)) return;

            _cache.InMatchSinceUtc = "";
            Save();

            try
            {
                string output = await CloudCode.CallAsync(ScriptName, new { action = "abandon" });
                var answer = JsonUtility.FromJson<SubmitResponse>(output);

                if (answer != null && !string.IsNullOrWhiteSpace(answer.profile))
                    AdoptRemoteProfile(answer.profile);

                Debug.Log("[Career] reported a match left early");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Career] could not report an abandoned match: {e.Message}");
            }
        }

        public void Record(MatchRecord record, string witnessDigest)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.MatchId)) return;

            // ⚠️ PLAYING TO THE WHISTLE IS WHAT CLEARS THE FLAG, and it is cleared here
            // rather than in `Adopt` so that a spectator, who has no line and returns early below,
            // does not clear a flag it never set.
            _cache.InMatchSinceUtc = "";

            string me = LocalPlayerId;
            var line = MatchRecordRules.LineFor(record, me);

            // ⚠️ A SPECTATED MATCH IS REMEMBERED BY NOBODY, AND THAT IS CORRECT. A seatless
            // referee has no line in the record, so there is nothing to add to a career and
            // nothing to submit.
            if (line == null) return;

            bool applied = ProfileRules.Apply(_cache.Profile, record, me, out XpAward award);
            LastAward = award;
            _cache.History = ProfileRules.Remember(_cache.History, record);

            // ⚠️ THE QUEUE IS WRITTEN BEFORE THE CALL IS TRIED, NOT AFTER IT FAILS. A process
            // killed mid-request is the case a queue exists for, and one that is only written in
            // the failure branch has already lost the record by then. A duplicate submission is
            // free: `ProfileRules.Apply` refuses a match id it has already counted, on the server
            // as well as here.
            if (applied)
            {
                PadWitnesses();
                _cache.Queue.Add(record);
                _cache.QueueWitness.Add(witnessDigest ?? "");

                while (_cache.Queue.Count > QueueLimit)
                {
                    _cache.Queue.RemoveAt(0);
                    if (_cache.QueueWitness.Count > 0) _cache.QueueWitness.RemoveAt(0);
                }
            }

            Save();
            Changed?.Invoke();

            _ = FlushAsync();
        }

        // -------------------------------------------------------------------
        // § THE SERVER
        // -------------------------------------------------------------------

        /// <summary>
        /// Makes the witness list the same length as the queue.
        ///
        /// ⚠️ OLD RECORDS PAD AT THE FRONT, WHICH IS WHERE THEY ARE. A career file written
        /// before this field existed has records queued and no digests; padding at the end would
        /// pair the oldest record with a digest belonging to nothing.
        /// </summary>
        private void PadWitnesses()
        {
            _cache.QueueWitness ??= new List<string>();
            while (_cache.QueueWitness.Count < _cache.Queue.Count) _cache.QueueWitness.Insert(0, "");
            while (_cache.QueueWitness.Count > _cache.Queue.Count) _cache.QueueWitness.RemoveAt(0);
        }

        /// <summary>Sends everything queued, then refreshes the profile from the server.</summary>
        public async Task SyncAsync()
        {
            await FlushAsync();
            await RefreshAsync();
        }

        /// <summary>
        /// Submits queued records, oldest first, and stops at the first failure.
        ///
        /// ⚠️ IT STOPS RATHER THAN CARRYING ON. The usual reason a submission fails is that the
        /// network is gone, and firing the rest of the queue at a service that is not there
        /// spends the boot budget on nineteen more timeouts for no gain.
        ///
        /// ⚠️⚠️ AND THAT "STOP AT THE FIRST FAILURE" RULE IS WHY A PERMANENTLY REFUSABLE RECORD
        /// HAS TO BE DROPPED BEFORE IT IS SENT. `match-record.js`'s `submit` throws for two
        /// reasons that are decided by the bytes in the record, so retrying changes nothing and
        /// the record sits at the head of the queue for ever with every later match stacked
        /// behind it. Measured on the player's machine 2026-08-30: three such records, a
        /// successful sign-in, `failed (422)` on every boot, and a career that had never once
        /// reached the server. `MatchRecordRules.Submittable` names both cases and
        /// `docs/TODO.md` § 94.1 is the entry.
        ///
        /// ⚠️ THE DROP IS LOUD AND IT IS COUNTED. `Status` says how many were abandoned, because
        /// a match silently deleted from a career is worse than one that never uploads: the
        /// player at least knows to say something about the second.
        /// </summary>
        public async Task FlushAsync()
        {
            if (_flushing || _cache.Queue.Count == 0) return;
            if (!(GameServices.Account?.IsSignedIn ?? false)) return;

            _flushing = true;
            try
            {
                int abandoned = DropUnsubmittable();

                while (_cache.Queue.Count > 0)
                {
                    var record = _cache.Queue[0];
                    string json = JsonUtility.ToJson(record);

                    PadWitnesses();
                    string witness = _cache.QueueWitness[0] ?? "";

                    string output = await CloudCode.CallAsync(
                        ScriptName, new { action = "submit", record = json, witness = witness });

                    var answer = JsonUtility.FromJson<SubmitResponse>(output);
                    if (answer != null && !string.IsNullOrWhiteSpace(answer.profile))
                        AdoptRemoteProfile(answer.profile);

                    // ⚠️⚠️ THE VERDICT IS REPORTED AND NEVER RETRIED. A disputed match is
                    // a finished piece of business: the career stats still applied, the ranked
                    // rating did not, and submitting it again would produce the same answer. The
                    // one thing that must not happen is the wedge `docs/TODO.md` § 94.1 records,
                    // where one record that can never be accepted holds up every match behind it.
                    LastVerdict = answer?.verdict ?? "";

                    _cache.Queue.RemoveAt(0);
                    if (_cache.QueueWitness.Count > 0) _cache.QueueWitness.RemoveAt(0);
                    Save();
                    Changed?.Invoke();
                }

                Status = abandoned > 0
                    ? $"Career saved; {abandoned} match(es) could not be uploaded"
                    : "Career saved";
            }
            catch (Exception e)
            {
                Status = $"{_cache.Queue.Count} match(es) waiting to upload";
                Debug.LogWarning($"[Career] submission deferred; {_cache.Queue.Count} queued: {e.Message}");
            }
            finally
            {
                _flushing = false;
            }
        }

        /// <summary>
        /// Removes every queued record the endpoint can never accept from this player, and
        /// returns how many went.
        ///
        /// ⚠️⚠️ IT IS DECIDED WITHOUT A CALL, BY `MatchRecordRules.Submittable`, AND THAT IS THE
        /// POINT. Asking the service would cost one 422 per bad record per boot and would still
        /// have to decide what a 422 means, which is unanswerable from the outside: a thrown
        /// Cloud Code error and a service that is unwell both arrive as one. The two refusals in
        /// `match-record.js`'s `submit` are pure functions of the record and the caller, so this
        /// side can answer them exactly, and everything that survives is a record whose failure
        /// really is worth retrying.
        ///
        /// ⚠️ THE LOCAL CAREER KEEPS THE MATCH. `ProfileRules.Apply` already counted it into the
        /// local profile and history when it was played, and dropping the upload does not undo
        /// that. What is lost is the match ever reaching the server, which had already happened.
        /// </summary>
        private int DropUnsubmittable()
        {
            string me = LocalPlayerId;
            int dropped = 0;

            for (int i = _cache.Queue.Count - 1; i >= 0; i--)
            {
                var verdict = MatchRecordRules.Submittable(_cache.Queue[i], me);
                if (verdict == MatchRecordRules.SubmitVerdict.Ok) continue;

                Debug.LogWarning(
                    $"[Career] abandoning queued match '{_cache.Queue[i]?.MatchId}': " +
                    MatchRecordRules.SubmitRefusal(verdict));

                _cache.Queue.RemoveAt(i);
                dropped++;
            }

            if (dropped > 0)
            {
                Save();
                Changed?.Invoke();
            }

            return dropped;
        }

        /// <summary>Replaces the local profile with the server's.</summary>
        public async Task RefreshAsync()
        {
            // ⚠️ ONE AT A TIME. `PlayerAccount.Changed` fires more than once during a boot,
            // and the career page calls this from its own REFRESH button on top of that.
            // Two refreshes in flight race to be the one that wins `AdoptRemoteProfile`.
            if (_refreshing) return;

            if (!(GameServices.Account?.IsSignedIn ?? false))
            {
                Status = _cache.Queue.Count > 0
                    ? $"{_cache.Queue.Count} match(es) waiting to upload"
                    : "Local career";
                return;
            }

            _refreshing = true;
            try
            {
                // ⚠️ BEFORE THE LOAD, so the profile this refresh adopts already carries the
                // cooldown the abandon just bought. Doing it afterwards would show the player a
                // clean profile and then a queue that refuses them, with nothing on screen having
                // changed in between.
                await ReportAbandonIfAnyAsync();

                string output = await CloudCode.CallAsync(ScriptName, new { action = "load" });
                var answer = JsonUtility.FromJson<SubmitResponse>(output);

                if (answer != null && !string.IsNullOrWhiteSpace(answer.profile))
                {
                    AdoptRemoteProfile(answer.profile);
                    Status = "Career synced";
                }
                else
                {
                    // An empty profile is the right answer for somebody who has never finished a
                    // match, exactly as an empty `accountProfile` is in `player-account.js`.
                    Status = "No matches on this account yet";
                }
            }
            catch (Exception e)
            {
                Status = "Showing the career saved on this machine";
                Debug.LogWarning($"[Career] profile refresh failed; local career kept: {e.Message}");
            }
            finally
            {
                _refreshing = false;
            }
        }

        /// <summary>
        /// Asks the server for a page of history.
        ///
        /// ⚠️ HISTORY IS PAGED AND THE PROFILE IS NOT, because they have very different sizes.
        /// A hundred four-player records is a payload nobody wants on every menu open, and
        /// `FUTURE.md` § 2.1 item 5 asks for twenty rows at a time anyway. On a failure the local
        /// history is returned, which is the last hundred matches this machine saw.
        /// </summary>
        public async Task<List<MatchRecord>> HistoryPageAsync(int offset, int limit)
        {
            if (!(GameServices.Account?.IsSignedIn ?? false)) return LocalPage(offset, limit);

            try
            {
                string output = await CloudCode.CallAsync(
                    ScriptName, new { action = "history", offset, limit });

                var answer = JsonUtility.FromJson<HistoryResponse>(output);
                if (answer == null || string.IsNullOrWhiteSpace(answer.history)) return LocalPage(offset, limit);

                var page = JsonUtility.FromJson<RecordList>("{\"items\":" + answer.history + "}");
                return page?.items ?? LocalPage(offset, limit);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Career] history page failed; showing local history: {e.Message}");
                return LocalPage(offset, limit);
            }
        }

        /// <summary>⚠️ `JsonUtility` CANNOT PARSE A BARE JSON ARRAY. It needs a named field, which
        /// is why the page is wrapped rather than read directly. This is a documented Unity
        /// limitation and not a shape choice on the server's side.</summary>
        [Serializable]
        private sealed class RecordList
        {
            public List<MatchRecord> items = new List<MatchRecord>();
        }

        private List<MatchRecord> LocalPage(int offset, int limit)
        {
            var page = new List<MatchRecord>();
            for (int i = Mathf.Max(0, offset); i < _cache.History.Count && page.Count < limit; i++)
                page.Add(_cache.History[i]);
            return page;
        }

        private void AdoptRemoteProfile(string json)
        {
            var remote = JsonUtility.FromJson<PlayerProfile>(json);
            if (remote == null) return;

            remote.Modes ??= new List<ModeRecord>();
            remote.Characters ??= new List<PickRecord>();
            remote.Slippers ??= new List<PickRecord>();
            remote.AppliedMatchIds ??= new List<string>();

            // ⚠️⚠️ THE SERVER'S PROFILE REPLACES THE LOCAL ONE WHOLE. There is no field-by-field
            // merge and there must not be: two counters that both claim to know how many matches
            // you have played cannot be reconciled without the records that produced them, and
            // the records are on the server. `AccountRules.Resolve` merges the ACCOUNT because
            // its fields are things a person typed and a person can retype; a career total is
            // arithmetic and the authority owns it.
            //
            // ⚠️ THE QUEUE IS NOT TOUCHED HERE. A record still waiting to upload is not in the
            // server's arithmetic yet, so the profile shown will be a match or two behind until
            // `FlushAsync` succeeds. That is honest, and `Status` says so.
            _cache.Profile = ProfileRules.Normalise(remote);
            Save();
            Changed?.Invoke();
        }
    }
}
