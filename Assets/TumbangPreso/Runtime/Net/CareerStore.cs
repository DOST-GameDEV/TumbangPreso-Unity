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

        [Serializable]
        private sealed class Cache
        {
            public PlayerProfile Profile = new PlayerProfile();
            public List<MatchRecord> History = new List<MatchRecord>();
            public List<MatchRecord> Queue = new List<MatchRecord>();

            /// <summary>Which account this cache belongs to. See <see cref="Load"/>.</summary>
            public string OwnerId = "";
        }

        [Serializable]
        private sealed class SubmitResponse
        {
            public string profile;
            public bool applied;
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
                if (File.Exists(Path))
                {
                    var loaded = JsonUtility.FromJson<Cache>(File.ReadAllText(Path));
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
            try
            {
                File.WriteAllText(Path, JsonUtility.ToJson(_cache));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Career] could not write {Path}: {e.Message}");
            }
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
        public void Record(MatchRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.MatchId)) return;

            string me = GameServices.Account?.ConnectionToken ?? NetIdentity.Token;
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
                _cache.Queue.Add(record);
                while (_cache.Queue.Count > QueueLimit) _cache.Queue.RemoveAt(0);
            }

            Save();
            Changed?.Invoke();

            _ = FlushAsync();
        }

        // -------------------------------------------------------------------
        // § THE SERVER
        // -------------------------------------------------------------------

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
        /// </summary>
        public async Task FlushAsync()
        {
            if (_flushing || _cache.Queue.Count == 0) return;
            if (!(GameServices.Account?.IsSignedIn ?? false)) return;

            _flushing = true;
            try
            {
                while (_cache.Queue.Count > 0)
                {
                    var record = _cache.Queue[0];
                    string json = JsonUtility.ToJson(record);

                    string output = await CloudCode.CallAsync(
                        ScriptName, new { action = "submit", record = json });

                    var answer = JsonUtility.FromJson<SubmitResponse>(output);
                    if (answer != null && !string.IsNullOrWhiteSpace(answer.profile))
                        AdoptRemoteProfile(answer.profile);

                    _cache.Queue.RemoveAt(0);
                    Save();
                    Changed?.Invoke();
                }

                Status = "Career saved";
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
