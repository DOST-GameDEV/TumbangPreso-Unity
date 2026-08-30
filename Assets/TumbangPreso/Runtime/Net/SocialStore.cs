using System;
using System.IO;
using System.Threading.Tasks;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// The player's friends, blocks and everybody's presence.
    ///
    /// ⚠️⚠️ THE SERVER IS THE AUTHORITY AND THIS IS A CACHE, WHICH IS EXACTLY `CareerStore`'S
    /// ARRANGEMENT AND FOR THE SAME REASONS. `FUTURE.md` § 0.5 rule 6: a client never writes what
    /// it owns. A friend request lands in somebody else's document, so it can only be made by the
    /// endpoint; what this class does is ask, cache the answer, and draw it.
    ///
    /// ⚠️⚠️ AND OFFLINE IS A FIRST-CLASS PATH, NOT AN ERROR PATH. The nationals are in General
    /// Santos City and `docs/TODO.md` § 97 made "works with the cable out" a release gate.
    /// **Nothing here ever blocks a match, a menu or a boot**: every call is fired and forgotten,
    /// every failure is a warning, and a machine that has never reached the service draws an
    /// empty rail rather than an error.
    ///
    /// ⚠️⚠️ THE ONE THING IT WRITES ON A TIMER IS PRESENCE, AT `SocialRules.PresenceWriteSeconds`
    /// AND NOT AT `ServerQuery`'S 4 s. `FUTURE.md` § 19.6 rules that out by name: a lobby list
    /// goes stale in seconds and presence changes when somebody presses PLAY, so writing it at
    /// the lobby rate would be fifteen times the writes for a fact that does not move.
    ///
    /// ⚠️ THE BLOCK LIST IS READ BY `NetSession` AT APPROVAL, WHICH IS THE ONLY PLACE A BLOCK CAN
    /// ACTUALLY DO ANYTHING TODAY. There is no matchmaker until Phase 7, so "a blocked player is
    /// never queued into your match" is, in this build, "a blocked player cannot join the lobby
    /// you host". `docs/TODO.md` § 102.
    /// </summary>
    public sealed class SocialStore : MonoBehaviour
    {
        public const string ScriptName = "social";

        public static SocialStore Instance { get; private set; }

        [Serializable]
        private sealed class Cache
        {
            public SocialList List = new SocialList();

            /// <summary>⚠️ WHICH ACCOUNT THIS BELONGS TO. `CareerStore.Load` carries the full
            /// note: two accounts on one machine share `Application.persistentDataPath`, so a
            /// cache with no owner is one player's friends list drawn under another's name.</summary>
            public string OwnerId = "";
        }

        private Cache _cache = new Cache();
        private float _nextPresence;
        private bool _loading;
        private bool _writing;

        /// <summary>Raised whenever the list changes, so a rail can redraw without polling it.</summary>
        public event Action Changed;

        /// <summary>The list as it was last known. Never null.</summary>
        public SocialList List => _cache.List ?? (_cache.List = new SocialList());

        private static string Path =>
            System.IO.Path.Combine(Application.persistentDataPath, "social.json");

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }

            Instance = this;
            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // -------------------------------------------------------------------
        // § THE LOCAL CACHE
        // -------------------------------------------------------------------

        private void Load()
        {
            try
            {
                if (File.Exists(Path))
                    _cache = JsonUtility.FromJson<Cache>(File.ReadAllText(Path)) ?? new Cache();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Social] could not read {Path}: {e.Message}");
                _cache = new Cache();
            }

            // ⚠️ A CACHE BELONGING TO SOMEBODY ELSE IS DISCARDED RATHER THAN MERGED. Two accounts
            // on one machine is the tournament-guest case (`docs/TODO.md` § 97), and merging two
            // friends lists would put one player's friends on another player's screen.
            string me = CareerStore.LocalPlayerId;
            if (!string.IsNullOrEmpty(_cache.OwnerId) && _cache.OwnerId != me)
                _cache = new Cache();

            _cache.List = SocialRules.Normalise(_cache.List);
        }

        private void Save()
        {
            try
            {
                _cache.OwnerId = CareerStore.LocalPlayerId;
                _cache.List = SocialRules.Normalise(_cache.List);
                File.WriteAllText(Path, JsonUtility.ToJson(_cache, prettyPrint: true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Social] could not write {Path}: {e.Message}");
            }

            Changed?.Invoke();
        }

        // -------------------------------------------------------------------
        // § THE SERVICE
        // -------------------------------------------------------------------

        /// <summary>
        /// Asks the endpoint for the list, including the presence of the friends it will draw.
        ///
        /// ⚠️ ONE AT A TIME. A rail that redraws on `Changed` and refreshes on open can otherwise
        /// have three of these in flight, each finishing over the last, and the newest answer is
        /// not necessarily the one that lands last.
        /// </summary>
        public async void Refresh()
        {
            if (_loading) return;
            _loading = true;

            try
            {
                string output = await CloudCode.CallAsync(ScriptName, new { action = "load" });
                Adopt(output);
            }
            catch (Exception e)
            {
                // ⚠️ A WARNING AND THE CACHED LIST, NEVER AN EMPTY ONE. The offline player still
                // gets to see who their friends are; they simply all read OFFLINE, which is
                // honest, because this machine cannot tell.
                Debug.LogWarning($"[Social] load failed, keeping the cached list: {e.Message}");
            }
            finally
            {
                _loading = false;
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE ENDPOINT'S ANSWER REPLACES THE LOCAL LIST WITH NO MERGE, WHICH IS
        /// `CareerStore.AdoptRemoteProfile`'S RULE AND FOR THE SAME REASON. The two can disagree —
        /// somebody accepted a request while this machine was asleep — and a merge would either
        /// resurrect a friendship that was ended or drop one that was made. **The server wins,
        /// every time.**
        /// </summary>
        private void Adopt(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var envelope = JsonUtility.FromJson<ListEnvelope>(json);
                if (envelope == null || string.IsNullOrEmpty(envelope.list)) return;

                _cache.List = SocialRules.Normalise(JsonUtility.FromJson<SocialList>(envelope.list));
                Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Social] could not read the endpoint's list: {e.Message}");
            }
        }

        [Serializable]
        private sealed class ListEnvelope
        {
            public string list = "";
        }

        /// <summary>
        /// Ask to be somebody's friend.
        ///
        /// ⚠️⚠️ THE LOCAL CHECK IS FOR THE BUTTON AND THE REMOTE CHECK IS FOR THE TRUTH. This
        /// refuses early so a player gets a reason instead of a silence (`SocialRules.
        /// WhyCannotRequest`), and the endpoint refuses again against the RECIPIENT's document,
        /// which is the only side that can see whether they blocked you.
        /// </summary>
        public async void Request(string playerId, string theirHandle)
        {
            if (!SocialRules.CanRequest(List, CareerStore.LocalPlayerId, playerId)) return;
            await Post(new
            {
                action = "request",
                playerId,
                handle = MyHandle,
                theirHandle = theirHandle ?? "",
            });
        }

        public async void Accept(string playerId)
            => await Post(new { action = "accept", playerId, handle = MyHandle });

        public async void Decline(string playerId)
            => await Post(new { action = "decline", playerId, handle = MyHandle });

        public async void Remove(string playerId)
            => await Post(new { action = "remove", playerId, handle = MyHandle });

        public async void Block(string playerId)
            => await Post(new { action = "block", playerId, handle = MyHandle });

        public async void Unblock(string playerId)
            => await Post(new { action = "unblock", playerId, handle = MyHandle });

        /// <summary>
        /// ⚠️ EVERY WRITE GOES THROUGH ONE METHOD, so there is one place that adopts the answer
        /// and one place that swallows a failure. Six copies of a try/catch is six chances for one
        /// of them to leave the local list ahead of the server's.
        /// </summary>
        private async Task Post(object parameters)
        {
            if (_writing) return;
            _writing = true;

            try
            {
                Adopt(await CloudCode.CallAsync(ScriptName, parameters));
            }
            catch (Exception e)
            {
                // ⚠️⚠️ NOTHING IS APPLIED LOCALLY ON A FAILURE, AND THAT IS THE DIFFERENCE
                // BETWEEN THIS AND `CareerStore`. A match record is this machine's own fact and
                // is queued until it lands; a friendship is a fact about two people and there is
                // no such thing as a local one. Showing it as done and having it vanish on the
                // next load is worse than the press appearing not to work.
                Debug.LogWarning($"[Social] write failed: {e.Message}");
            }
            finally
            {
                _writing = false;
            }
        }

        private static string MyHandle => GameServices.Account?.LobbyName ?? "";

        // -------------------------------------------------------------------
        // § PRESENCE
        // -------------------------------------------------------------------

        /// <summary>
        /// Tell the service where this player is, at most once every
        /// <see cref="SocialRules.PresenceWriteSeconds"/>.
        ///
        /// ⚠️⚠️ IT IS DRIVEN FROM `Update` RATHER THAN FROM EVERY SCREEN THAT CHANGES STATE, and
        /// that is the same argument `PlayerNameplate` records for watching the overlays instead
        /// of being told about them: **a heartbeat that depends on every screen remembering to
        /// send it is a heartbeat that stops working the first time somebody adds a screen.**
        ///
        /// ⚠️ THE STATE IS DERIVED FROM WHAT THE GAME IS ACTUALLY DOING, not stored. One less
        /// field to keep in step, and it cannot be left saying IN A MATCH after a match ends.
        /// </summary>
        private void Update()
        {
            if (Time.unscaledTime < _nextPresence) return;
            _nextPresence = Time.unscaledTime + SocialRules.PresenceWriteSeconds;

            if (!SocialRules.IsAddressable(GameServices.Account?.PlayerId)) return;

            SendPresence();
        }

        private async void SendPresence()
        {
            try
            {
                await CloudCode.CallAsync(ScriptName, new
                {
                    action = "presence",
                    state = (int)CurrentState,
                    joinCode = CurrentJoinCode,
                    handle = MyHandle,
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Social] presence not written: {e.Message}");
            }
        }

        /// <summary>
        /// ⚠️ `Queued` IS NEVER PRODUCED HERE AND THAT IS CORRECT. Phase 7 owns the queue; the
        /// value exists in `PresenceState` so the vocabulary does not have to change on the wire
        /// later, and `Social.cs` says so.
        /// </summary>
        private static PresenceState CurrentState
        {
            get
            {
                if (GameServices.Round != null)
                    return GameLaunch.Spectator ? PresenceState.Spectating : PresenceState.InMatch;

                return PresenceState.Menu;
            }
        }

        /// <summary>
        /// The lobby a friend could join, or nothing.
        ///
        /// ⚠️⚠️ IT IS PUBLISHED ONLY WHILE THERE IS A LOBBY THAT CAN ACTUALLY BE JOINED. A join
        /// code that outlives its lobby is a JOIN button that sends somebody to a room that
        /// closed, which reads as the game being broken rather than as the friend having left;
        /// `SocialRules.IsJoinable` refuses a stale one at the other end as well, so this is the
        /// same rule at both ends of a cached document.
        /// </summary>
        private static string CurrentJoinCode
        {
            get
            {
                var lobby = NetSession.Instance?.Lobby;
                if (lobby == null || !NetAuthority.IsNetworked) return "";
                if (lobby.MatchInProgress) return "";

                return lobby.JoinCode ?? "";
            }
        }
    }
}
