using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>What the queue is doing right now. The UI draws exactly this.</summary>
    public enum QueueState
    {
        Idle = 0,
        Searching = 1,
        Joining = 2,
        Hosting = 3,
        Found = 4,
        Cancelled = 5,
        Refused = 6,
    }

    /// <summary>
    /// QUICK MATCH: a rating-banded queue built on the lobby browse loop that is already running.
    ///
    /// ⚠️⚠️ IT ISSUES NO SERVICE CALLS OF ITS OWN, AND THAT IS THE DESIGN RATHER THAN AN
    /// OPTIMISATION. `FUTURE.md` § 19.7: "this must not raise the query rate against the free
    /// tier." `ServerQuery` already queries UGS Lobby every 4 seconds while browsing and already
    /// heartbeats a hosted lobby every 15; this class subscribes to <see cref="ServerQuery.ServersChanged"/>,
    /// decides against the records that loop already fetched, and adds **zero** requests. The only
    /// writes it causes are the ones hosting a lobby already caused: one create and one update per
    /// count change.
    ///
    /// ⚠️⚠️ AND THE WIDENING IS LOCAL, WHICH IS THE SECOND HALF OF THE SAME ARGUMENT. A queue that
    /// republished its band every 15 seconds would be one lobby write per queuing player per step.
    /// `MatchmakingRules.Evaluate` checks the band from both sides, so a searcher whose own band
    /// has widened finds a patient host without the patient host saying anything.
    ///
    /// ⚠️ THE THREE OUTCOMES ARE JOIN, HOST, KEEP LOOKING, AND HOSTING IS NOT A FAILURE. A queue
    /// with nobody in it has to produce a room for the next person to find, or two players who
    /// press QUICK MATCH thirty seconds apart both search an empty list for ever.
    ///
    /// ⚠️ IT DRIVES THE SAME `LobbyJoinPanel` AND `NetSession` PATHS A HUMAN PRESS DOES. A queue
    /// that reached past them into the transport would prove the transport works and say nothing
    /// about whether the button does, which is the reasoning `LobbyJoinPanel.AutomationJoin`
    /// already carries.
    /// </summary>
    public sealed class Matchmaker : MonoBehaviour
    {
        /// <summary>Raised whenever anything the queue card draws has changed.</summary>
        public event Action Changed;

        /// <summary>Raised when the queue has landed the player in a lobby. The lobby redraws.</summary>
        public event Action Joined;

        public QueueState State { get; private set; } = QueueState.Idle;
        public GameMode Mode { get; private set; } = GameMode.Classic;
        public QueueStake Stake { get; private set; } = QueueStake.Casual;

        /// <summary>Seconds since QUICK MATCH was pressed. The card draws this.</summary>
        public float Elapsed { get; private set; }

        /// <summary>Why the queue refused to start, or an empty string.</summary>
        public string Refusal { get; private set; } = "";

        /// <summary>How many lobbies the last pass looked at, and why each was passed over.
        /// ⚠️ FOR THE LOG AND FOR A PROBE, NEVER FOR THE PLAYER. "Rejected 3 lobbies for
        /// SpreadTooWide" is a sentence about the matchmaker; the player wants to know the queue
        /// is still going.</summary>
        public readonly Dictionary<JoinRefusal, int> LastPassReasons = new Dictionary<JoinRefusal, int>();

        private NetSession _net;
        private bool _subscribed;
        private bool _busy;

        /// <summary>
        /// How many seats the ticket needs, which is the party size.
        /// ⚠️ `PartyRules.SeatsNeeded`: a party of three offered a lobby with two chairs is one
        /// person left standing on the menu.
        /// </summary>
        public int PartySize { get; private set; } = 1;

        // -------------------------------------------------------------------

        /// <summary>
        /// The matchmaker in this process, or null.
        ///
        /// ⚠️ A LOOKUP RATHER THAN AN `Ensure`, because the two readers of it must not
        /// CREATE one. `MatchStatsCollector` asks at the whistle whether the match was ranked and
        /// `MatchRpc` asks on a departure whether to offer a backfill seat; in a practice match,
        /// on a LAN, and in the whole of the nationals venue there is no queue at all, and the
        /// right answer to both questions there is "no".
        /// </summary>
        public static Matchmaker Current => FindFirstObjectByType<Matchmaker>();

        public static Matchmaker Ensure()
        {
            var existing = FindFirstObjectByType<Matchmaker>();
            if (existing != null) return existing;

            var net = NetSession.Ensure();
            return net.gameObject.AddComponent<Matchmaker>();
        }

        public bool IsQueueing => State == QueueState.Searching || State == QueueState.Joining
                                                                || State == QueueState.Hosting;

        /// <summary>
        /// The rating this machine queues at.
        ///
        /// ⚠️⚠️ A PLAYER WITH NO LADDER STATE QUEUES AT THE START RATING, WHICH IS MID-LADDER, AND
        /// THAT IS THE SAME DECISION AS CUTTING PLACEMENT MATCHES. `FUTURE.md` § 9: "start
        /// everyone mid-ladder with a wide rating deviation and show the tier immediately". A
        /// guest, an offline player and a brand new account all queue at 1500 and are matched
        /// against the people nearest them, which is the best guess anybody can make about
        /// somebody who has never played.
        /// </summary>
        public int LocalRating
        {
            get
            {
                var profile = GameServices.Career?.Profile;
                if (profile?.Rank == null) return (int)RatingRules.StartRating;
                return (int)Math.Round(profile.Rank.Rating);
            }
        }

        /// <summary>
        /// The pool this machine belongs to.
        ///
        /// ⚠️ THE INPUT DEVICE AND PLATFORM ARE MEASURED, NOT CONFIGURED. `FUTURE.md` § 7 asks for
        /// pools separated by both "which is free and removes the entire aim-assist argument
        /// before it starts", and a setting the player can change would be a setting somebody sets
        /// to get into a softer pool.
        /// </summary>
        public string PoolKey => MatchmakingRules.PoolKey(Mode, Stake, LocalInputDevice(),
                                                          LocalPlatform(), NetSession.ProtocolVersion);

        /// <summary>
        /// ⚠️ THERE ARE NO GAMEPAD BINDINGS IN THE INPUT MAP AS OF 2026-08-31, so this answers
        /// `KeyboardMouse` for every desktop player today and the pool key has the field ready for
        /// Phase 14. `FUTURE.md` § 0.6's own check is
        /// `grep -c Gamepad Assets/TumbangPreso/Resources/TumbangPreso.inputactions`, which is 0.
        /// </summary>
        private static InputDevice LocalInputDevice()
        {
#if UNITY_IOS || UNITY_ANDROID
            return InputDevice.Touch;
#else
            return InputDevice.KeyboardMouse;
#endif
        }

        private static PlatformFamily LocalPlatform()
        {
#if UNITY_WEBGL
            return PlatformFamily.Web;
#elif UNITY_IOS || UNITY_ANDROID
            return PlatformFamily.Mobile;
#else
            return PlatformFamily.Desktop;
#endif
        }

        /// <summary>The band being searched right now. The card draws its two numbers.</summary>
        public RatingBand Band => MatchmakingRules.BandFor(LocalRating, Elapsed);

        public float WideningProgress => MatchmakingRules.WideningProgress(Elapsed);

        public string SearchLabel => MatchmakingRules.SearchLabel(LocalRating, Elapsed);

        // -------------------------------------------------------------------

        /// <summary>
        /// Start searching.
        ///
        /// ⚠️⚠️ IT REFUSES RATHER THAN SILENTLY DOING NOTHING, AND EVERY REFUSAL HAS A SENTENCE.
        /// `CLAUDE.md` § 6.3: "a control that does nothing when pressed must not look pressable",
        /// and a dead end is a bug. A queue cooldown, a full four-stack in ranked and a guest in
        /// ranked are the three real ones, and all three are things the player can act on.
        /// </summary>
        public bool StartQueue(GameMode mode, QueueStake stake, int partySize = 1)
        {
            Mode = mode;
            Stake = stake;
            PartySize = Mathf.Clamp(partySize, 1, PartyRules.MaxSize);

            var refusal = PartyRules.CanQueue(PartySize, stake, PartyCooldowns(), PartySignedIn());
            if (refusal != PartyRefusal.None)
            {
                Refusal = PartyRules.RefusalLabel(refusal);
                State = QueueState.Refused;
                Raise();
                return false;
            }

            int cooldown = LocalCooldownSeconds();
            if (cooldown > 0)
            {
                Refusal = IntegrityRules.CooldownLabel(cooldown);
                State = QueueState.Refused;
                Raise();
                return false;
            }

            _net = NetSession.Ensure();
            Refusal = "";
            Elapsed = 0.0f;
            State = QueueState.Searching;
            LastPassReasons.Clear();

            // The browse loop is what this queue reads. It is already running whenever the join
            // card is open; starting it here is what makes QUICK MATCH work from a closed card.
            _net.Query?.StartBrowsing();
            Subscribe();

            // ⚠️ THE FIRST PASS RUNS IMMEDIATELY RATHER THAN ON THE NEXT `ServersChanged`. That
            // event only fires when the list CHANGES, and a list that has been stable for a minute
            // is exactly the case where somebody is already sitting in a joinable room.
            Evaluate();

            Raise();
            return true;
        }

        /// <summary>
        /// Stop searching and put the lobby back where it was.
        ///
        /// ⚠️⚠️ CANCELLING TAKES THE ROOM OUT OF THE POOL AND DOES NOT TEAR IT DOWN. The player
        /// pressed CANCEL on a queue, not BACK on a lobby, and they are still standing in a room
        /// with a join code they may want to read out. `NetSession.Advert` going back to `None` is
        /// the whole of "no longer offering myself to strangers".
        /// </summary>
        public void Cancel()
        {
            if (!IsQueueing && State != QueueState.Refused) return;

            State = QueueState.Cancelled;
            Elapsed = 0.0f;
            Unsubscribe();
            ClearAdvert();
            Raise();
        }

        private void ClearAdvert()
        {
            if (_net == null) return;

            _net.Advert = ServerQuery.HostedAdvert.None;
            if (_net.IsNetworked && _net.IsHost) _net.RepublishLobbyAdvert();
        }

        private void Update()
        {
            if (!IsQueueing) return;

            float before = Elapsed;
            Elapsed += Time.unscaledDeltaTime;

            // ⚠️ THE CARD IS TOLD ONCE A SECOND, NOT EVERY FRAME. It draws an elapsed time in
            // whole seconds and a bar; sixty redraws a second of the same two values is the shape
            // `docs/TODO.md` § 52.3 measured costing the HUD an eighth of its frames.
            if ((int)before != (int)Elapsed) Raise();

            // ⚠️⚠️ AND A WIDENING STEP RE-EVALUATES IMMEDIATELY RATHER THAN WAITING FOR THE LIST
            // TO CHANGE. The whole point of widening is that lobbies which were outside the band
            // are inside it now, and none of them had to move for that to become true. Without
            // this, a stable list of near-misses would never be reconsidered and the widening
            // would be a bar that fills while nothing happens behind it.
            if (MatchmakingRules.HalfWidthAt(before) != MatchmakingRules.HalfWidthAt(Elapsed))
            {
                Evaluate();
                Raise();
            }
        }

        private void Subscribe()
        {
            if (_subscribed || _net?.Query == null) return;
            _net.Query.ServersChanged += Evaluate;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _net?.Query == null) return;
            _net.Query.ServersChanged -= Evaluate;
            _subscribed = false;
        }

        private void OnDestroy() => Unsubscribe();

        // -------------------------------------------------------------------

        /// <summary>
        /// One pass over what the browse loop last fetched.
        ///
        /// ⚠️ IT IS LATCHED. `ServersChanged` can fire while a join handshake is in flight, and
        /// two starts over one transport is the fault `LobbyJoinPanel.Join` already latches
        /// against.
        /// </summary>
        private async void Evaluate()
        {
            if (!IsQueueing || _busy || _net == null) return;

            var adverts = new List<LobbyAdvert>();
            var entries = new List<ServerQuery.Entry>();

            foreach (var entry in _net.Query?.Servers ?? Array.Empty<ServerQuery.Entry>())
            {
                entries.Add(entry);
                adverts.Add(entry.AsAdvert());
            }

            RecordPass(adverts);

            int best = MatchmakingRules.Best(adverts, LocalPlayerId(), LocalRating, Elapsed,
                                             PoolKey, IsBlocked, PartyRules.SeatsNeeded(PartySize));

            if (best >= 0)
            {
                await JoinAsync(entries[best]);
                return;
            }

            // ⚠️⚠️ NOTHING TO JOIN MEANS HOST, AND IT HAPPENS ON THE FIRST PASS RATHER THAN AFTER
            // A DELAY. A queue that waited before offering itself would have both of two players
            // searching an empty list, each waiting for the other to give up first. Hosting is
            // cheap here because the lobby is already hosted: `ConvertedMatchSetup.AutoHost` has
            // a LAN room up before this class is ever reached, and going online is one call.
            await HostAsync();
        }

        private void RecordPass(List<LobbyAdvert> adverts)
        {
            LastPassReasons.Clear();

            foreach (var advert in adverts)
            {
                var refusal = MatchmakingRules.Evaluate(advert, LocalPlayerId(), LocalRating, Elapsed,
                                                        PoolKey, IsBlocked,
                                                        PartyRules.SeatsNeeded(PartySize));

                LastPassReasons.TryGetValue(refusal, out int count);
                LastPassReasons[refusal] = count + 1;
            }
        }

        private async System.Threading.Tasks.Task JoinAsync(ServerQuery.Entry entry)
        {
            _busy = true;
            State = QueueState.Joining;
            Raise();

            try
            {
                // ⚠️ THE CODE IS REMEMBERED BEFORE THE CONNECT, exactly as `LobbyJoinPanel.Connect`
                // does and for the same reason: a joiner has to be able to read the code back out
                // to a third player, and the lobby draws it from `LobbySession.JoinCode`.
                if (!string.IsNullOrEmpty(entry.JoinCode)) _net.Lobby.SetJoinCode(entry.JoinCode);

                bool ok = await _net.StartRelayClient(entry.RelayCode);
                if (this == null) return;

                if (ok)
                {
                    State = QueueState.Found;
                    Unsubscribe();
                    ClearAdvert();
                    Raise();
                    Joined?.Invoke();
                    return;
                }

                // ⚠️⚠️ A FAILED JOIN GOES BACK TO SEARCHING AND IS NOT AN ERROR THE PLAYER READS.
                // `docs/TODO.md` § 65.4: the browser can offer a lobby whose Relay allocation is
                // already gone, and a queue is the one place where that is genuinely nothing to
                // report, because the correct response is to try the next one. A join the PLAYER
                // asked for still reports, in `LobbyJoinPanel`.
                Debug.Log($"[Queue] {entry.JoinCode} did not answer, still searching");
                State = QueueState.Searching;
                Raise();
            }
            finally
            {
                _busy = false;
            }
        }

        private async System.Threading.Tasks.Task HostAsync()
        {
            _busy = true;

            try
            {
                if (!_net.IsNetworked || !_net.IsRelay)
                {
                    State = QueueState.Hosting;
                    Raise();

                    bool ok = await _net.StartRelayHost();
                    if (this == null) return;

                    if (!ok)
                    {
                        // ⚠️ THE QUEUE KEEPS LOOKING RATHER THAN DYING. A machine with no route to
                        // Relay can still be found by somebody else on its LAN, and the search
                        // costs nothing while it waits.
                        State = QueueState.Searching;
                        Raise();
                        return;
                    }
                }

                PublishAdvert();
                State = QueueState.Searching;
                Raise();
            }
            finally
            {
                _busy = false;
            }
        }

        /// <summary>
        /// Put this room in the pool, with the band and the seat extremes a searcher needs.
        ///
        /// ⚠️ THE SEAT EXTREMES ARE THE HOST'S OWN RATING UNTIL SOMEBODY ELSE ARRIVES, and there
        /// is nowhere else they could come from: a peer's rating is not on the wire and putting it
        /// there would publish three strangers' ratings into every browser in the game for one
        /// decision. `MatchmakingRules.SpreadWith` treats the host as the whole room, which is
        /// exactly true while the host is alone and is the conservative answer afterwards.
        /// </summary>
        private void PublishAdvert()
        {
            if (_net == null) return;

            _net.Advert = new ServerQuery.HostedAdvert
            {
                PoolKey = PoolKey,
                BandLow = Band.Low,
                BandHigh = Band.High,
                SeatLow = LocalRating,
                SeatHigh = LocalRating,
                Backfill = false,
                HostPlayerId = LocalPlayerId(),
            };

            _net.RepublishLobbyAdvert();
        }

        // -------------------------------------------------------------------

        private static string LocalPlayerId() => CareerStore.LocalPlayerId;

        /// <summary>
        /// ⚠️ BLOCKS ARE HONOURED BY THE QUEUE AS WELL AS BY CONNECTION APPROVAL, WHICH IS TWO
        /// GATES FOR ONE RULE ON PURPOSE. `FUTURE.md` § 6: "blocking must survive matchmaking: a
        /// blocked player is never queued into your match." Approval alone would let the queue
        /// find a blocked host, connect, and bounce the player straight back out, which reads as
        /// the queue being broken rather than as a block working.
        /// </summary>
        private static bool IsBlocked(string playerId)
        {
            var list = GameServices.Social?.List;
            return list != null && SocialRules.IsBlocked(list, playerId);
        }

        private static int LocalCooldownSeconds()
        {
            var profile = GameServices.Career?.Profile;
            if (profile == null || string.IsNullOrEmpty(profile.CooldownUntilUtc)) return 0;

            if (!DateTime.TryParse(profile.CooldownUntilUtc,
                                   System.Globalization.CultureInfo.InvariantCulture,
                                   System.Globalization.DateTimeStyles.AdjustToUniversal |
                                   System.Globalization.DateTimeStyles.AssumeUniversal,
                                   out var until)) return 0;

            double remaining = (until - DateTime.UtcNow).TotalSeconds;
            return remaining <= 0.0 ? 0 : (int)Math.Ceiling(remaining);
        }

        /// <summary>
        /// ⚠️⚠️ A PARTY IS THE HUMANS IN THIS LOBBY AND THERE IS NO PARTY SERVICE. `PartyRules`'
        /// header: the seat table already knows who is playing with whom, and a second roster
        /// would be a second source of truth about it. The leader queues and the room goes.
        ///
        /// ⚠️ ONLY THE LOCAL COOLDOWN IS KNOWN FOR CERTAIN. A peer's cooldown is on their own
        /// profile document, which this machine cannot read, so the honest answer is that the
        /// local player is checked here and every member is checked by their own client before it
        /// follows. A four-stack whose second member is on a cooldown is refused by that member's
        /// own machine, which is where the sentence can be shown to the person it is about.
        /// </summary>
        private int[] PartyCooldowns() => new[] { LocalCooldownSeconds() };

        private bool[] PartySignedIn()
            => new[] { GameServices.Account != null && GameServices.Account.IsSignedIn };

        private void Raise() => Changed?.Invoke();

        // -------------------------------------------------------------------

        /// <summary>
        /// A match that has lost a player advertises the seat rather than dying.
        ///
        /// ⚠️⚠️ THE HOST CALLS THIS, AND IT IS THE WHOLE OF BACKFILL. `FUTURE.md` § 7 asks for
        /// exactly one behaviour: "a match that loses a player advertises the seat rather than
        /// dying". `LobbySession.Depart` already holds the empty seat against the leaver's durable
        /// token and `RuleOnArrival` already hands a free seat to a newcomer mid-match, so
        /// everything under this was built for the reconnect window; what was missing was that
        /// nothing told the outside world the chair existed. The lobby record's `InProgress` flag
        /// is what stopped it: `MatchmakingRules.Evaluate` refuses a running match unless it is
        /// backfilling, so this flag is the difference between an open door and a closed one.
        ///
        /// ⚠️ IT IS NOT CALLED WHEN THE SEAT IS BEING HELD FOR SOMEBODY WHO MIGHT COME BACK.
        /// `IntegrityRules.ReconnectWindowSeconds` is the promise `RuleOnArrival` branch 1 makes,
        /// and offering the chair to a stranger inside it would break it: the player with the bad
        /// Wi-Fi comes back to find somebody else holding their score.
        /// </summary>
        public void OfferBackfillSeat(bool offering)
        {
            if (_net == null) _net = NetSession.Instance;
            if (_net == null || !_net.IsNetworked || !_net.IsHost) return;

            var advert = _net.Advert;

            // ⚠️ A ROOM THAT NEVER QUEUED STILL BACKFILLS, AND IT ADVERTISES INTO ITS OWN POOL TO
            // DO IT. A private room that loses somebody wants that seat filled just as much; what
            // it does NOT want is to have been findable before the match started, which is what
            // `HostedAdvert.None` gives it. So the pool key is set here rather than assumed.
            advert.PoolKey = PoolKey;
            advert.HostPlayerId = LocalPlayerId();
            advert.Backfill = offering;

            if (offering && advert.BandHigh <= advert.BandLow)
            {
                // ⚠️⚠️ A BACKFILL SEARCHES AT THE WIDEST BAND FROM THE FIRST SECOND, and that is
                // not laziness about the schedule. Three people are standing in a live match
                // playing three-on-one against a bot while this waits, so the cost of a wide band
                // is a slightly uneven round and the cost of a narrow one is the rest of the match.
                // `MatchmakingRules.MaxHalfWidth` is the same number the schedule ends at.
                advert.BandLow = LocalRating - MatchmakingRules.MaxHalfWidth;
                advert.BandHigh = LocalRating + MatchmakingRules.MaxHalfWidth;
                advert.SeatLow = LocalRating;
                advert.SeatHigh = LocalRating;
            }

            _net.Advert = advert;
            _net.RepublishLobbyAdvert();
        }
    }
}
