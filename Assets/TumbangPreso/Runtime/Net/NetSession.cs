using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
#if MULTIPLAY_SDK
using Unity.Services.Multiplay;
#endif
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// The live network session: the adapter between the transport and the game's own
    /// bookkeeping.
    ///
    /// ⚠️⚠️ THE LOBBY LOGIC IS NOT IN HERE AND MUST NOT MOVE IN HERE. Seating, reconnection,
    /// seat reclaim, leader election and join codes live in <see cref="LobbySession"/>, which
    /// knows nothing about any transport and is therefore unit tested in milliseconds. This
    /// class is deliberately thin: it starts and stops a transport, and it forwards connection
    /// events into that bookkeeping. Every line of rules that creeps in here is a line that can
    /// only be tested with four machines.
    ///
    /// ⚠️ AND IT ANSWERS NetAuthority. Every host-authoritative path in the game already asks
    /// "do I decide this, or do I ask?" through that seam, so making multiplayer real is a
    /// matter of installing this provider rather than editing every verb.
    /// </summary>
    public sealed class NetSession : MonoBehaviour, INetProvider
    {
        private static string LocalLobbyName()
        {
            string account = GameServices.Account?.LobbyName;
            if (!string.IsNullOrWhiteSpace(account)) return account;
            string local = Settings.SettingsStore.Current.PlayerName;
            return Core.AccountRules.Handle(local, "");
        }
        public static NetSession Instance { get; private set; }

        public const int DefaultPort = LobbySession.DefaultPort;

        public LobbySession Lobby { get; } = new LobbySession();

        /// <summary>The online pool browser. Idle until a screen calls StartBrowsing.</summary>
        public ServerQuery Query { get; private set; }

        /// <summary>The local network beacon for LAN game discovery.</summary>
        public LanBeacon Beacon => _beacon;

        public event Action<string> StatusChanged;

        /// <summary>
        /// Raised on a CLIENT when its connection ends, with the host's reason or "" if none.
        ///
        /// ⚠️⚠️ A REFUSED APPROVAL LEFT THE PLAYER SITTING IN A LOBBY THAT SAID CONNECTED.
        /// `ConvertedMultiplayerSetup.Join` navigates to the lobby the moment `StartClient`
        /// returns true, and that only means the TRANSPORT was told to start: approval has not
        /// happened yet and can still be refused for a protocol mismatch or a full lobby. The
        /// refusal arrives here, several seconds later, on a screen that has already been left
        /// behind, so the reason was written to a status label nobody was looking at. 🧑
        /// 2026-08-27, off two laptops: *"this is the one that joined its just stuck here"*, on a
        /// lobby reading LOBBY · CONNECTED with every other seat drawn as a bot.
        ///
        /// ⚠️ THE REASON IS THE PAYLOAD BECAUSE IT IS THE ONLY ACTIONABLE PART. A version
        /// mismatch is a thing the player can actually fix, and it is indistinguishable from a
        /// host that vanished unless somebody prints it.
        /// </summary>
        /// <summary>
        /// What a peer is told when the host closes the session on purpose.
        ///
        /// ⚠️ IT IS A SENTENCE, NOT A CODE. `PlayerFacingDisconnectReason` passes a reason
        /// straight through to the lobby's alert line, so this is read by a player and not by
        /// software. `LobbySession.MatchFullMessage` is written the same way for the same reason.
        /// </summary>
        public const string HostLeftMessage = "The host left the game.";

        public static event Action<string> ClientDisconnected;

        /// <summary>Why the last client connection ended. Read once by the join screen, which
        /// clears it, so a stale reason cannot be shown over a later successful join.</summary>
        public static string LastDisconnectReason { get; set; } = "";

        /// <summary>
        /// Raised on THIS process whenever its own seat or spectator flag changes.
        ///
        /// ⚠️⚠️ THE LOBBY SCREEN HAD NO WAY TO KNOW IT HAD BEEN SEATED. `LocalSlot` is written
        /// from three places (the seat-assignment message, `Seating`, and a mid-match rebind) and
        /// not one of them told anybody, so `ConvertedMatchSetup` drew the seat rows once at
        /// `Start` and then only ever redrew them when a pick table happened to arrive. A joiner
        /// seated in P2 kept the "◀ YOU" marker on P1 until something else moved.
        /// </summary>
        public event Action SeatingChanged;

        private NetworkManager _nm;
        private UnityTransport _utp;

        [Serializable]
        private sealed class ConnectionHello
        {
            public int Protocol;
            public string Token;
            public string Name;

            // ⚠️⚠️ THESE TWO ARE THE IMPERSONATION GUARD ON THE WIRE, `docs/TODO.md` § 88.1c.
            // `AccountPlayerId` is what the peer says its account is, and `HandleProof` is a
            // short-lived value the account endpoint minted for THAT account and nobody else.
            // Neither is trusted here: the pair only lets the host ask the endpoint one question.
            //
            // ⚠️ THE PROOF IS NOT A CREDENTIAL AND MUST NEVER BE REPLACED BY ONE. The obvious
            // shortcut is to put the peer's own UGS access token in this payload and let the host
            // check it, which hands whoever is hosting the ability to act as that player against
            // every service in the project. A peer-hosted game means the host is a stranger.
            public string AccountPlayerId;
            public string HandleProof;
        }

        /// <summary>
        /// The custom-message schema spoken by this build. This is deliberately separate from
        /// the marketing version: a peer with an older movement or ability payload cannot safely
        /// join and "mostly work". That failure presents as wrong characters, missing powers,
        /// or a frozen body, which is much worse than a clear version-mismatch refusal.
        /// </summary>
        // ⚠️ 2 to 3 REMOVED THE PEER ID FROM `DeclareReady` AND `VoteRematch`; 3 to 4 GAVE
        // `DeclareReady` ITS `ready` BOOL AND ADDED `ReadyTally` FOR THE LOBBY GATE. Both landed
        // the same day from two branches, and a build carrying the first would misread the
        // second's ready press as a peer id. 4 to 5 ADDED `Score`, which is what makes an award
        // audible and visible on a peer that is not the host (`docs/TODO.md` § 57.3).
        // One bump per incompatible shape, not per day.
        //
        // ⚠️⚠️ 5 to 6 ADDED `Chat` AND `ChatLine`, AND IT IS THE ONLY BUMP THE WHOLE PUBG LOBBY
        // BATCH COSTS. 🧑 2026-08-28: *"yea maybe add a chat to our game too that works in lobby
        // and ingame"*. Everything else in that batch (the lobby landing straight from
        // MULTIPLAYER, the auto-host, the in-lobby join panel, the cast standing in the arena,
        // the START/READY split) is drawn from state that was ALREADY replicated, which is why
        // `docs/TODO.md` § 68.2 held every bump until this one message so there is exactly one.
        // Both machines must be rebuilt from this branch or they refuse each other at approval,
        // by design; § 59.2 is what makes the refusal say so instead of hanging.
        //
        // ⚠️⚠️ 6 to 7 OPENED THE SLIPPER ROSTER TO NINE, AND 7 to 8 ADDS THE FIFTH AND SIXTH
        // CAN. No message changed shape either time, which is exactly why both need the bump:
        // `can_index` and `slipper_index` travel as bare ints and mean nothing on their own,
        // so a peer on 7 reading a `can_index` of 4 or 5 does not error, it indexes past the
        // end of a four-entry table. `Roster.CanArt` clamps rather than throwing, so the
        // visible result is two players looking at different cans in the same match with
        // nothing in either log. A roster that only GROWS still breaks the wire.
        //
        // ⚠️ 8 to 9 APPENDS LOAFERS AS SLIPPER INDEX 9, in the same batch and for exactly the
        // same reason. Both bumps landed on 2026-08-28; they are listed apart rather than
        // merged because the roster they widen is a different one, and a future reader
        // bisecting a mismatch needs to know which table grew.
        //
        // ⚠️ 9 to 10 IS THE ART SWAP, AND IT IS A BUMP FOR A REASON THE OTHERS ARE NOT.
        // The slipper list is still ten entries and no index moved, so nothing here is
        // strictly unreadable by a peer on 9. What changed is which SHOE two of those indices
        // resolve to: PANTULOG at 2 became the fuzzy house slipper, and PAMBAHAY at 6 became
        // the rubber bathroom slide with that shoe's stats rather than the flip-flop's.
        //
        // ⚠️ A STAT CHANGE IS NOT A WIRE CHANGE, BUT AN UNMATCHED PAIR OF BUILDS IS STILL A
        // BUG. Two peers on either side of this would each apply their OWN table to the same
        // `slipper_index`, so one player's PAMBAHAY throws 4/2/4 and the other's throws
        // 3/2/4 in the same match, and every prediction between them drifts with no error
        // anywhere. Refusing at approval is the cheaper failure.
        // ⚠️⚠️ 10 to 11 ADDS `CastDenied`, AND IT IS A BUMP FOR THE OPPOSITE REASON TO 9 to 10.
        // That one changed no message at all and bumped because a shared table had been re-pointed.
        // This one adds a NAME to the wire: the host now answers a refused `ReqAbility` instead of
        // dropping it, and a peer built on 10 has no handler registered for the reply.
        //
        // ⚠️ THE FAILURE WOULD BE QUIET RATHER THAN LOUD, WHICH IS WHY IT IS WORTH THE BUMP.
        // Netcode logs an unregistered named message and carries on, so a mixed pair would play:
        // the 11 host would refuse a cast and believe it had said so, and the 10 client would sit
        // on a cooldown it can no longer get back, because `HeroAbility.ApplyNetworkSnapshot` is
        // raise-only for the owner while a round is live. That is a player quietly losing an
        // ultimate, in a build that otherwise looks fine. Refusing at approval is cheaper.
        //
        // ⚠️ THE PROP STREAM'S DELIVERY SPLIT IN THE SAME BATCH IS NOT PART OF THIS. `SyncSlipper`
        // and `SyncLata` kept every field and every field order; only the channel some of their
        // packets travel on changed, and delivery is not in the payload. It is recorded here
        // rather than given its own number because it needs none.
        // ⚠️⚠️ 11 TO 12, FOR A CHANGE OF FIELD MEANING, WHICH IS THE MOST DANGEROUS KIND AND THE
        // ONE `audit_wire_payloads.py` CANNOT SEE. `docs/TODO.md` § 78.1: `SyncSlipper` and
        // `SlipperPose` are now addressed by `Slipper.SeatOfOrigin` where their first field used
        // to be `OwnerSlot`, and `SyncSlipper` gained `OwnerSlot` back as ordinary payload.
        //
        // The audit compares the writer and the reader field by field and both halves moved
        // together, so it reads 0 mismatched and is right to: the two ends of THIS build agree.
        // What it cannot check is a build on 11 talking to a build on 12, where the field count
        // differs by one and every field after the first is read from the wrong offset — a
        // silently misread position, state and holder rather than an error. § 38.20's last bullet
        // says this in general terms; this is the case it was written for.
        //
        // ⚠️ AND THE MEANING CHANGE ALONE WOULD DESERVE IT EVEN AT THE SAME FIELD COUNT. An 11
        // peer would read a seat index as an owner, which for the taya's tsinelas is exactly the
        // -1 this change exists to stop happening.
        /// <summary>
        /// ⚠️ 13 SINCE 2026-08-29. `Flair` is a new named message (`Visual.MatchFlair`), so a
        /// build without its handler drops every one of them and its players see none of the
        /// tags, blocks, bank shots or zaps the host is announcing — a silent half-working match
        /// rather than a refusal, which is the case this number exists to prevent. Both machines
        /// rebuild from the same branch; that is by design.
        ///
        /// ⚠️⚠️ **14 SINCE 2026-08-30**, for `ReqTime` and `SyncTime`, the two messages behind
        /// the spectator pause (`MatchRpc` § THE BROADCAST CLOCK). This one is the strongest case
        /// this number has ever had: a peer without the `SyncTime` handler **does not stop**.
        /// The spectator calls a pause, three screens freeze, one carries on playing a match
        /// nobody else is in, and the two versions then disagree about every position for as long
        /// as the pause lasts. That is worse than either a refusal or a missing effect.
        /// ⚠️⚠️ **15 SINCE 2026-08-30**, for `MatchRecord`, the one message that carries a
        /// whole finished match to every peer (`MatchRpc.BroadcastMatchRecord`). A peer without
        /// the handler still plays the match correctly and then silently gets no end-of-match
        /// summary and no career entry for a game it played, which is the quiet kind of wrong
        /// this number exists to turn into a refusal. `docs/TODO.md` § 89.
        ///
        /// ⚠️⚠️ **16 SINCE 2026-08-30**, for the two fields the impersonation guard puts in the
        /// approval hello and in `Identify` (`docs/TODO.md` § 88.1c and § 90.1). `Identify` is a
        /// `FastBufferWriter` message read field by field in order, so a peer on 15 writes five
        /// values where a host on 16 reads seven and every field after the third is read from the
        /// wrong offset. That is the case `audit_wire_payloads.py` cannot see, because both ends
        /// of THIS build agree; § 89.5 records the same trap one message earlier.
        ///
        /// ⚠️ AND THE QUIET HALF WOULD BE WORSE THAN THE LOUD ONE. Even if the payload were
        /// tolerant, a 15 peer carries no proof, so on a 16 host it arrives unverified and any
        /// account handle it claims is demoted to a host-allocated tag. Everybody on the older
        /// build would silently be renamed in a lobby that looked like it was working.
        ///
        /// ⚠️⚠️ **17 SINCE 2026-08-31**, for cosmetics: one field on `Identify`, one on
        /// `SelectLobbyPick`, and two per seat on `SyncLobbyPicks` (`docs/TODO.md` § 101).
        /// **All three are `FastBufferWriter` messages read field by field in order**, which is
        /// the same trap 16 and § 89.5 record: a peer on 16 writes seven values where a host on 17
        /// reads eight, and every field after that is read from the wrong offset. `SyncLobbyPicks`
        /// is the worst of the three, because its per-seat loop would go out of phase on seat 0
        /// and mis-read the name, the picks and the ready flag of every seat after it. **A lobby
        /// where everybody is wearing the wrong face is not a cosmetic bug.**
        ///
        /// ⚠️ THE SPECTATOR COUNT AT THE END OF `SyncLobbyPicks` IS WHY IT CANNOT BE MADE
        /// TOLERANT THE WAY THAT FIELD WAS. `OnSyncLobbyPicksMsg` reads the count with a
        /// `reader.Length > reader.Position` guard, which works for ONE trailing value. These two
        /// are inside the per-seat loop, ahead of everything the loop reads next, so there is no
        /// position at which "is there more" answers the right question.
        /// </summary>
        /// ⚠️⚠️ 18 IS THE LOOK FRAME AND THE QUEUE. `LobbySeatInfo.PaletteId` became
        /// `LobbySeatInfo.Look` and carries a `LookCodec` frame rather than a bare palette id, so
        /// a 17 build and an 18 build would read one another's seat table and dress every remote
        /// player from a string neither recognises. That is the exact fault the paragraph above
        /// is about, one field along.
        ///
        /// ⚠️⚠️ 19 IS THE CUSTOM CHARACTER, SINCE 2026-08-31, AND IT IS THE FIRST FIELD IN THIS
        /// LIST THAT DECIDES A GAMEPLAY THING RATHER THAN A COSMETIC ONE. `LobbySeatInfo.Custom`
        /// carries a `CustomCharacterRules` `C3` frame, and inside it is `HeroKitId`: which hero's
        /// skills and ultimate that seat brings into Hero Strike (`docs/TODO.md` § 110.5). A peer
        /// that could not read the field would draw a stranger AND read the wrong ability tells
        /// off them, which `docs/VISION.md` § 4 says is a skill the whole competitive mode rests
        /// on. One field on `Identify`, one on `SelectLobbyPick`, one per seat on
        /// `SyncLobbyPicks`, § 112.
        ///
        /// ⚠️ THE TWO PEER-TO-HOST MESSAGES READ IT UNDER A LENGTH GUARD AND `SyncLobbyPicks`
        /// CANNOT, which is the split the paragraph above already records: a trailing field can be
        /// made tolerant and a field inside a per-seat loop cannot. This constant is what stops
        /// the second case from ever arising.
        public const int ProtocolVersion = 19;

        /// <summary>
        /// What this machine's hosted lobby publishes to QUICK MATCH, or
        /// <see cref="ServerQuery.HostedAdvert.None"/> when it is not offering itself to
        /// strangers.
        ///
        /// ⚠️⚠️ A ROOM IS NOT IN THE QUEUE UNTIL SOMEBODY PUTS IT THERE, AND THE DEFAULT IS
        /// OUT. Every lobby in this game auto-hosts on arrival (`ConvertedMatchSetup.AutoHost`),
        /// so if the default were "in the pool" then pressing PLAY would silently offer the
        /// player's room to the internet, and somebody waiting for one friend would find three
        /// strangers in their chairs. `Matchmaker` sets this when the player presses QUICK MATCH
        /// and clears it when they cancel.
        ///
        /// ⚠️ IT LIVES HERE RATHER THAN ON `Matchmaker` BECAUSE THIS CLASS OWNS BOTH WRITERS.
        /// The advert is written by `StartRelayHost` and again by every `PublishLobbyCounts`, and
        /// a value the matchmaker held would have to be fetched by a class that must keep working
        /// when no matchmaker exists at all (LAN, practice, the whole of the nationals venue).
        /// </summary>
        public ServerQuery.HostedAdvert Advert { get; set; } = ServerQuery.HostedAdvert.None;

        private const string SeatAssignmentMessage = "tp.seat.assignment.v1";
        private readonly Dictionary<ulong, ConnectionHello> _helloByClient =
            new Dictionary<ulong, ConnectionHello>();
        /// <summary>
        /// ⚠️ THE MANAGER THIS IS REGISTERED ON, for the reason `MatchRpc._handlersOn` spells
        /// out at length: `Shutdown` destroys the `CustomMessagingManager` and a bool saying
        /// "already done" then refuses to register on its replacement, so the seat message
        /// stopped arriving after the first session of a process.
        /// </summary>
        private Unity.Netcode.CustomMessagingManager _seatHandlerOn;
        private LanBeacon _beacon;
#if MULTIPLAY_SDK
        private IServerQueryHandler _serverQueryHandler;
#endif

        // INetProvider
        public bool IsHost => _nm == null || !_nm.IsListening || _nm.IsServer;
        public bool IsNetworked => _nm != null && _nm.IsListening;
        public int LocalSlot { get; private set; }

        /// <summary>
        /// ⚠️ THE TRANSPORT'S OWN ID, NEVER THE SEAT. NGO gives the listen host and a dedicated
        /// referee `NetworkManager.ServerClientId`, which is 0, and that 0 is a real identity
        /// rather than a placeholder: `LobbySession` keys `_peers` by it and the ready and
        /// rematch gates count it. Offline there is no transport, and 0 is then the only peer
        /// there is, so the same answer is correct for a different reason.
        /// </summary>
        public int LocalPeerId => _nm != null && _nm.IsListening ? (int)_nm.LocalClientId : 0;

        public bool IsSeatlessReferee => _nm != null && _nm.IsServer && !_nm.IsClient;

        public string Status { get; private set; } = "offline";
        public string RelayJoinCode { get; private set; }
        public bool IsRelay { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _nm = GetComponent<NetworkManager>();
            if (_nm == null) _nm = gameObject.AddComponent<NetworkManager>();

            var rpc = GetComponent<MatchRpc>();
            if (rpc == null) rpc = gameObject.AddComponent<MatchRpc>();
            rpc.Initialize(_nm);

            _utp = GetComponent<UnityTransport>();
            if (_utp == null) _utp = gameObject.AddComponent<UnityTransport>();

            _nm.NetworkConfig ??= new NetworkConfig();
            _nm.NetworkConfig.NetworkTransport = _utp;
            _nm.NetworkConfig.ConnectionApproval = true;

            // ⚠️ SCENE MANAGEMENT OFF. The game loads its own scenes through SceneFlow, and
            // letting the netcode also drive scene loads means two systems racing to decide
            // which scene a client is in. The symptom is a client stuck on a black screen while
            // the host plays on.
            _nm.NetworkConfig.EnableSceneManagement = false;

            _beacon = GetComponent<LanBeacon>();
            if (_beacon == null) _beacon = gameObject.AddComponent<LanBeacon>();

            // ⚠️ THE ONLINE BROWSER SITS BESIDE THE LAN BEACON, NOT INSIDE IT. They answer
            // different questions: "what is on this network" and "what is on the pool", and
            // a build that cannot reach the pool must still find a game on the LAN.
            Query = GetComponent<ServerQuery>();
            if (Query == null) Query = gameObject.AddComponent<ServerQuery>();

            NetAuthority.Provider = this;

            _nm.OnClientConnectedCallback += OnClientConnected;
            _nm.OnClientDisconnectCallback += OnClientDisconnected;
            _nm.ConnectionApprovalCallback += ApproveConnection;
        }

        private void OnDestroy()
        {
            if (_nm != null)
            {
                _nm.OnClientConnectedCallback -= OnClientConnected;
                _nm.OnClientDisconnectCallback -= OnClientDisconnected;
                _nm.ConnectionApprovalCallback -= ApproveConnection;

                if (_seatHandlerOn != null && _nm.CustomMessagingManager != null)
                    _nm.CustomMessagingManager.UnregisterNamedMessageHandler(SeatAssignmentMessage);
            }

            if (Instance == this)
            {
                Instance = null;

                // ⚠️ HAND AUTHORITY BACK TO SOLO. Leaving a dead provider installed makes every
                // host-side path answer "not the host" after a disconnect, and the single
                // player game silently stops awarding points.
                NetAuthority.Provider = new SoloProvider();
            }
        }

        /// <summary>
        /// ⚠️ CREATED ON DEMAND SO SINGLE PLAYER PAYS NOTHING FOR IT. A NetworkManager that
        /// exists but never listens is harmless, but building it lazily keeps the offline game
        /// genuinely offline, which is what makes the headless probes reproducible.
        /// </summary>
        public static NetSession Ensure()
        {
            if (Instance != null) return Instance;

            var go = new GameObject("~NetSession");
            return go.AddComponent<NetSession>();
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// How many frames a previous session is given to finish shutting down. At 60 fps this
        /// is a fifth of a second, which is far longer than the one frame NGO actually needs;
        /// the bound exists so a transport wedged open can never hang the button that pressed it.
        /// </summary>
        private const int ShutdownWaitFrames = 12;

        /// <summary>
        /// Ends any live session and WAITS FOR IT TO ACTUALLY BE OVER before returning.
        ///
        /// ⚠️⚠️ THIS EXISTS BECAUSE `NetworkManager.Shutdown()` DOES NOT SHUT ANYTHING DOWN, AND
        /// EVERY START PATH USED TO ASSUME IT DID. All four opened with the same two lines,
        /// `if (_nm.IsListening) Stop();` followed immediately by a start, and NGO's `Shutdown`
        /// only sets a flag: `ShutdownInternal` runs later, from the network update loop at
        /// `PostLateUpdate`. `CanStart` refuses outright while `IsListening` is still true, so
        /// the start in the SAME FRAME was rejected every time. Measured directly:
        ///
        ///     straight after Stop() (same frame): IsListening=True ShutdownInProgress=True
        ///     SAME-FRAME restart returned False; status='failed to start hosting'
        ///
        /// So hosting or joining worked from a cold menu and failed whenever a session was
        /// already live — backing out of a lobby and hosting again, or retrying a join. That is
        /// exactly the reported shape: 🧑 2026-08-28, *"sometimes it says failed to join online
        /// host via relay. it's consistent because sometimes i get it to work"*, and *"i cant
        /// also seem to host in lan"*.
        ///
        /// ⚠️ THE RELAY PATHS WERE HIT LESS OFTEN, NOT EXEMPT. They happen to `await` a sign-in
        /// and an allocation between the stop and the start, so a frame usually passes by luck.
        /// A cached sign-in and a fast allocation can both continue synchronously, and then they
        /// fail identically. One gate for all four rather than four paths with different odds.
        ///
        /// ⚠️ FRAMES, NOT `Task.Yield`. A yielded continuation can resume inside the same frame,
        /// which is the very thing being waited out. `Awaitable.NextFrameAsync` is a real frame
        /// boundary, and `ShutdownInternal` has run by the next frame's Update.
        /// </summary>
        private async Task EnsureStoppedAsync()
        {
            if (_nm == null || !_nm.IsListening) return;

            Stop();

            for (int frame = 0; frame < ShutdownWaitFrames; frame++)
            {
                if (this == null) return;
                if (_nm == null || !_nm.IsListening) return;

                try
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            // ⚠️ SAID OUT LOUD RATHER THAN RETRIED FOREVER. If the transport is still up after
            // this long the start below will fail on its own and report why; a silent extra wait
            // would just move the same failure somewhere harder to find.
            if (_nm != null && _nm.IsListening)
            {
                Debug.LogWarning($"[Net] the previous session was still listening after " +
                                 $"{ShutdownWaitFrames} frames; starting anyway.");
            }
        }

        public async Task<bool> StartHostAsync(int port = DefaultPort, bool dedicated = false)
        {
            await EnsureStoppedAsync();

            Configure("0.0.0.0", port);
            ConfigureClientHello();
            IsRelay = false;
            RelayJoinCode = null;

            Lobby.IsDedicated = dedicated;
            Lobby.OpenLobby(new System.Random(Environment.TickCount));

            if (dedicated)
            {
                Application.targetFrameRate = 60;
                QualitySettings.vSyncCount = 0;
            }

            bool ok = dedicated ? _nm.StartServer() : _nm.StartHost();

            // ⚠️ ALL FOUR START PATHS REGISTER THE SEAT HANDLER, and this one was the last that
            // did not. A listen host is its own client, so `SendSeatAssignment` applies its seat
            // locally and it has never needed the message; that is an argument for it being
            // harmless, not for it being absent. Four routes to one outcome differing in any
            // detail is how `docs/TODO.md` sections 53.1, 57.1, 60, 62.1 and 63.1 each happened.
            if (ok) RegisterSeatHandler();
            SetStatus(ok
                ? $"hosting on {port}, join code {Lobby.JoinCode}"
                : "failed to start hosting");

            if (ok)
            {
                LocalSlot = dedicated ? -1 : 0;

                _beacon.HostName = LocalLobbyName();
                _beacon.JoinCode = Lobby.JoinCode;
                _beacon.Port = port;
                _beacon.InProgress = false;
                PublishLobbyCounts();
                _beacon.StartAdvertising();

                if (dedicated)
                {
                    _ = StartMultiplayServerAsync(port);
                }
            }

            return ok;
        }

        private void Update()
        {
#if MULTIPLAY_SDK
            if (_serverQueryHandler != null)
            {
                _serverQueryHandler.CurrentPlayers = (ushort)Lobby.SeatedPeerCount();
                _serverQueryHandler.MaxPlayers = (ushort)LobbySession.MaxPlayers;
                _serverQueryHandler.Map = UI.SceneFlow.SelectedMap;
                _serverQueryHandler.UpdateServerCheck();
            }
#endif

            // Only while actually hosting. `IsHost` is deliberately true offline as well, which
            // is right for the authority questions it answers and wrong here.
            if (_nm == null || !_nm.IsListening || !_nm.IsServer) return;

            RefreshBeaconCounts();

            // ⚠️ THE UGS PUSH IS STILL EVENT-DRIVEN, AND ONLY ON A REAL EDGE. The relay lobby is
            // a network call with its own coalescing; running it every frame would be a request
            // per frame for a value that changes twice a match. The beacon above is four local
            // field writes and needs no such gate.
            if (Lobby.MatchInProgress == _publishedInProgress) return;

            _publishedInProgress = Lobby.MatchInProgress;
            PublishLobbyCounts();
        }

        /// <summary>
        /// The last <see cref="LobbySession.MatchInProgress"/> pushed to the relay lobby, so the
        /// edge can be spotted without every caller that starts or ends a match remembering to
        /// announce it. See <see cref="RefreshBeaconCounts"/>.
        /// </summary>
        private bool _publishedInProgress;

        /// <summary>
        /// Registers this process with the Multiplay fleet and starts answering SQP queries.
        /// </summary>
        // ⚠⚠ DEFERRED, 2026-08-20, NOT IN PROGRESS. This superseded the E.2 decision
        // (`Unity_UGS_Networking_Prompts.md`, 2026-08-19) that chose Multiplay Hosting over the
        // retired Singapore VPS: com.unity.services.multiplay cannot be installed on Unity 6000.5 at all.
        // Every published version of it, 1.1.1 through 1.3.1, ships
        // Editor/Authoring/Assets/CreateMultiplayConfigMenu.cs, which calls EndNameEditAction.
        // Unity 6000.5 marks that obsolete as an ERROR rather than a warning, so the package's
        // authoring assembly fails to build and takes the whole project's compile down with it.
        // The package was therefore removed from the manifest.
        //
        // ⚠ Checked, not assumed, that the consolidated package does not rescue this.
        // com.unity.services.multiplayer@2.3.0 does ship a Server/ assembly, but its contents are
        // Sessions and Matchmaker server support (MultiplayerServerService,
        // MatchmakerServerExtensions). There is no ServerQueryHandler, no allocation callback
        // surface, and no server.json reader. SQP and fleet allocation both still need the
        // blocked package.
        //
        // ⚠ RELAY PEER HOSTING IS NOW THE PRIMARY ONLINE PATH, not a fallback behind this. See
        // NetSession.StartHost's Relay branch and Port_Plan.md Phase 5. The body below is gated
        // on MULTIPLAY_SDK, defined nowhere, kept rather than deleted because the fleet path is
        // real shipped behaviour. Two ways back, whichever lands first: Unity publishes a
        // multiplay build that compiles on 6.5, in which case re-add the package and define
        // MULTIPLAY_SDK and nothing else changes; or this is re-ported onto
        // MultiplayerServerService.CreateSessionAsync, which is Unity's actual replacement API but
        // exposes sessions rather than a ServerQueryHandler, so the SQP heartbeat in Update
        // becomes the service's job rather than ours. That port needs a real fleet to verify, so
        // it is deliberately not guessed at here. The dedicated Linux server target can serve
        // clients when explicitly launched, but no VPS deployment or fleet is active.
        public async Task StartMultiplayServerAsync(int port, string serverName = "Tumbang Preso Dedicated", string map = "Eskinita")
        {
#if !MULTIPLAY_SDK
            Debug.LogWarning(
                "[Net] Multiplay fleet registration skipped: com.unity.services.multiplay is not " +
                "installed because it does not compile on Unity 6000.5. An explicitly launched " +
                "dedicated host can accept direct clients, but it cannot report itself to a fleet.");
            SetStatus($"dedicated server on port {port} (fleet registration unavailable)");
            await Task.CompletedTask;
#else
            try
            {
                await Unity.Services.Core.UnityServices.InitializeAsync();
                var serverConfig = MultiplayService.Instance.ServerConfig;
                ushort serverPort = serverConfig != null && serverConfig.Port != 0 ? serverConfig.Port : (ushort)port;
                ushort queryPort = serverConfig != null && serverConfig.QueryPort != 0 ? serverConfig.QueryPort : (ushort)(port + 1);

                _serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(
                    (ushort)LobbySession.MaxPlayers,
                    serverName,
                    "TumbangPreso",
                    Application.version,
                    map);

                await MultiplayService.Instance.ReadyServerForPlayersAsync();
                SetStatus($"multiplay server ready on port {serverPort} (query {queryPort})");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Net] Multiplay initialization skipped/failed: {e.Message}");
            }
#endif
        }

        /// <summary>
        /// Connect to a host.
        ///
        /// ⚠️⚠️ IT SPLITS `host:port` HERE, AND NOTHING DID BEFORE, WHICH BROKE EVERY LAN JOIN
        /// MADE BY CLICKING A DISCOVERED GAME. `Configure` hands its argument straight to
        /// `UnityTransport.SetConnectionData`, which wants a bare address, and the join field is
        /// filled with `192.168.1.144:8910` from three directions: `LanBeacon` advertises
        /// `ip:port` and `ConvertedMultiplayerSetup.OnLanRowClicked` copies that string into the
        /// box verbatim, the online browser does the same, and the field's own help text says
        /// *"An online code, or a LAN address. Port defaults to 8910"*, which tells a player the
        /// port is optional and therefore that writing it is allowed.
        ///
        /// The whole string then went in as the HOSTNAME. It is not an address and it is not a
        /// resolvable name, so the transport refused to start, `StartClient` returned false and
        /// the screen said **"Could not reach 192.168.1.144:8910"** while naming the machine it
        /// had just been told about by that machine. 🧑 2026-08-27, with both firewalls off:
        /// *"they are detected on lan and server but i cant join"*.
        ///
        /// ⚠️ THE EXPLICIT `port` ARGUMENT STILL WINS WHEN THE STRING CARRIES NONE, so
        /// `-tp-join 127.0.0.1 7777` is unchanged. When the string carries one it is the more
        /// specific of the two and takes precedence.
        ///
        /// ⚠️ ONE COLON ONLY, AND THE TAIL MUST PARSE AS A PORT. A bare IPv6 literal is full of
        /// colons and is a valid address on its own; splitting on the last colon would turn
        /// `fe80::1` into a host of `fe80:` and a port of `1`. Bracketed IPv6 with a port
        /// (`[::1]:8910`) is handled separately for the same reason.
        /// </summary>
        public async Task<bool> StartClientAsync(string address, int port = DefaultPort)
        {
            await EnsureStoppedAsync();

            address = SplitHostPort(address, ref port);

            Configure(address, port);
            ConfigureClientHello();
            IsRelay = false;
            RelayJoinCode = null;

            bool ok = _nm.StartClient();
            if (ok) RegisterSeatHandler();
            SetStatus(ok ? $"connecting to {address}:{port}" : "failed to connect");
            return ok;
        }

        /// <summary>
        /// Allocates a Relay session and starts hosting through Unity Transport.
        ///
        /// ⚠️ CAPACITY IS MaxConnections (12), NOT MaxPlayers (4). Four seats is a rules
        /// constraint, twelve connections is a capacity ceiling so spectators can join a full game.
        ///
        /// ⚠️ JOIN CODE PRESERVATION. The game's 4-character confusable-free join code is
        /// generated in LobbySession as usual; the UGS Relay join code is an internal transport
        /// handle mapped behind this session.
        /// </summary>
        public async Task<bool> StartRelayHost(int maxConnections = LobbySession.MaxConnections)
        {
            await EnsureStoppedAsync();

            SetStatus("signing in to online services...");

            // ⚠ NO FALLBACK HERE, DELIBERATELY. Relay needs a real signed-in session, so there
            // is nothing to degrade to: the local token works for LAN and cannot allocate a
            // relay. The status now carries which of the three situations stopped it rather
            // than the single "authentication failed" that covered all of them.
            bool authOk = await NetIdentity.EnsureSignedInAsync();
            if (!authOk)
            {
                SetStatus($"cannot go online: {NetIdentity.StateReason}");
                return false;
            }

            SetStatus("allocating relay server...");
            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
                string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                RelayJoinCode = relayCode;
                IsRelay = true;

                // ⚠ Was new RelayServerData(allocation, "dtls"). That constructor belonged to
                // com.unity.transport 1.x and exists in none of the packages this project resolves
                // today. The conversion now lives in Unity.Services.Relay.Models.AllocationUtils,
                // shipped by com.unity.services.multiplayer, as an extension method.
                var relayServerData = allocation.ToRelayServerData("dtls");
                _utp.SetRelayServerData(relayServerData);
                ConfigureTimeouts();

                Lobby.IsDedicated = false;
                Lobby.OpenLobby(new System.Random(Environment.TickCount));
                // ⚠️ THE RELAY PATHS ARE THE ONLY ONES ALLOWED TO SPEND A SERVICE CALL ON THE WAY
                // TO A MATCH. See PrimeHandleProofAsync: LAN and direct-address joins may never.
                await PrimeHandleProofAsync();
                ConfigureClientHello();

                bool ok = _nm.StartHost();

                // ⚠️ THE RELAY HOST REGISTERS THE SEAT HANDLER TOO, exactly as `StartHost` does.
                // A listen host is also its own client, so the message can reach it, and the two
                // start paths differing at all is how one of them ends up missing a step nobody
                // notices until a player is stuck. Same reasoning as `docs/TODO.md` § 60: two
                // routes to one outcome, one of them a subset.
                if (ok) RegisterSeatHandler();
                SetStatus(ok
                    ? $"relay hosting active, code {Lobby.JoinCode} (relay {relayCode})"
                    : "failed to start relay host");

                if (ok)
                {
                    LocalSlot = 0;
                    _beacon.HostName = LocalLobbyName();
                    _beacon.JoinCode = Lobby.JoinCode;
                    _beacon.Port = DefaultPort;
                    _beacon.InProgress = false;
                    PublishLobbyCounts();
                    _beacon.StartAdvertising();

                    if (Query != null)
                    {
                        _ = Query.CreateHostedLobbyAsync(
                            LocalLobbyName(),
                            Lobby.JoinCode,
                            relayCode,
                            Lobby.SeatedPeerCount(),
                            Lobby.OccupiedSeatCount(),
                            Advert);
                    }
                }

                if (!ok)
                {
                    IsRelay = false;
                    RelayJoinCode = null;
                }

                return ok;
            }
            catch (Exception e)
            {
                IsRelay = false;
                RelayJoinCode = null;
                SetStatus($"relay allocation failed: {e.Message}");
                NetIdentity.ReportServiceCallFailed("Relay allocation", e);
                return false;
            }
        }

        /// <summary>
        /// Connects to a host through a UGS Relay join code.
        /// </summary>
        public async Task<bool> StartRelayClient(string relayJoinCode)
        {
            await EnsureStoppedAsync();

            if (string.IsNullOrWhiteSpace(relayJoinCode))
            {
                SetStatus("invalid relay join code");
                return false;
            }

            SetStatus("signing in to online services...");

            // ⚠ NO FALLBACK HERE, DELIBERATELY. Relay needs a real signed-in session, so there
            // is nothing to degrade to: the local token works for LAN and cannot allocate a
            // relay. The status now carries which of the three situations stopped it rather
            // than the single "authentication failed" that covered all of them.
            bool authOk = await NetIdentity.EnsureSignedInAsync();
            if (!authOk)
            {
                SetStatus($"cannot go online: {NetIdentity.StateReason}");
                return false;
            }

            SetStatus($"joining relay allocation {relayJoinCode}...");
            try
            {
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode.Trim());
                var relayServerData = joinAllocation.ToRelayServerData("dtls");
                _utp.SetRelayServerData(relayServerData);
                ConfigureTimeouts();

                IsRelay = true;
                RelayJoinCode = relayJoinCode.Trim();

                // ⚠️ THE RELAY PATHS ARE THE ONLY ONES ALLOWED TO SPEND A SERVICE CALL ON THE WAY
                // TO A MATCH. See PrimeHandleProofAsync: LAN and direct-address joins may never.
                await PrimeHandleProofAsync();
                ConfigureClientHello();
                bool ok = _nm.StartClient();
                if (ok) RegisterSeatHandler();
                else
                {
                    IsRelay = false;
                    RelayJoinCode = null;
                }
                SetStatus(ok ? "connecting to relay host..." : "failed to start relay client");
                return ok;
            }
            catch (Exception e)
            {
                IsRelay = false;
                RelayJoinCode = null;
                SetStatus($"relay connection failed: {e.Message}");
                NetIdentity.ReportServiceCallFailed("Relay join", e);
                return false;
            }
        }

        /// <summary>
        /// ⚠️⚠️ TRUE WHILE WE ARE THE ONES ENDING IT. `Shutdown` raises the same
        /// `OnClientDisconnectCallback` a refusal does, so without this a player pressing BACK
        /// was told why they had been thrown out of a lobby they had just chosen to leave. 🧑
        /// 2026-08-27: *"this shit shows even if i close on my own"*, over
        /// `[Disconnect Event][Client-0][TransportClientId-0][TransportShutdown]`.
        ///
        /// ⚠️ `StartClient` CALLS `Stop` FIRST when a session is already live, so this also covers
        /// the disconnect that a re-join produces on the way out of the old connection.
        /// </summary>
        private bool _localShutdown;

        /// <summary>
        /// ⚠️⚠️ TRUE ONLY ONCE THIS PEER HAS ACTUALLY BEEN LET IN, AND IT IS WHAT STOPS THE
        /// DISCONNECT HANDLER FIRING DURING A JOIN. `OnClientDisconnectCallback` is raised for a
        /// connection that never completed as readily as for one that was lost: a refused
        /// approval, a retry inside `MaxConnectAttempts`, and a transport rebind all reach it
        /// while the player is still on their way in. § 62.1 sends a disconnected peer back to
        /// the join screen, and without this that navigation fired mid-handshake and bounced the
        /// player out of the lobby they had just entered. 🧑 2026-08-28, one build after it
        /// landed: *"oh shit now i cant join any game wtf"*.
        ///
        /// ⚠️ CLEARED ON THE WAY OUT AND ON THE WAY IN, so a second join starts from false and
        /// cannot inherit the last session's answer.
        /// </summary>
        private bool _everConnected;

        public void Stop()
        {
            _localShutdown = true;
            _everConnected = false;
            _beacon.StopAll();

            // ⚠⚠ THE BROADCAST CLOCK IS HANDED BACK, OR A SESSION THAT ENDS MID-PAUSE FREEZES THE
            // PROCESS. Spectators may stop a live match (`MatchRpc` § THE BROADCAST CLOCK), and
            // `Time.timeScale` is a global that outlives this object: a host that quits while the
            // game is paused, or a client dropped during one, would walk back to a title screen
            // whose every animation and button had stopped, with nothing on screen saying why.
            // `MatchResult`'s own header records that exact failure happening once already, from
            // a different writer, and this is the same lifetime rule: whoever can stop time
            // restores it on every exit path including death.
            Time.timeScale = 1.0f;
            if (Query != null) _ = Query.DeleteHostedLobbyAsync();

            // ⚠️⚠️ A HOST TELLS ITS PEERS IT IS LEAVING. IT USED TO JUST STOP ANSWERING.
            // 🧑 2026-08-29: *"disconnect logic is thoroughly broken ... if lobby host leaves the
            // game or disconnects all other palyers stay in the game"*.
            //
            // `Shutdown()` alone is not a goodbye. NGO only sets a flag and tears the transport
            // down from its own update loop (see `WaitForShutdown` below), and whatever the
            // client end eventually notices, it notices through `DisconnectTimeoutMS` — which is
            // a silence timer. So three players carried on playing a match with no referee for as
            // long as that timer runs, and the reports of people "staying in the game" are that
            // window seen from the room.
            //
            // ⚠️ `DisconnectClient` IS AN ACTUAL MESSAGE AND CARRIES A REASON, which is the other
            // half: `PlayerFacingDisconnectReason` turns it into the line the lobby prints, so a
            // player who was dropped is told they were dropped rather than watching a lobby empty
            // itself. A timeout can only ever say "disconnected".
            //
            // ⚠️ THE LIST IS COPIED BEFORE IT IS WALKED. `DisconnectClient` mutates
            // `ConnectedClientsIds` as it goes, and enumerating a collection while it removes
            // from itself is an exception on the way out of a match.
            if (_nm != null && _nm.IsListening && _nm.IsServer)
            {
                var leaving = new List<ulong>(_nm.ConnectedClientsIds);
                foreach (ulong clientId in leaving)
                {
                    if (clientId == _nm.LocalClientId) continue;
                    try { _nm.DisconnectClient(clientId, HostLeftMessage); }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[Net] could not tell {clientId} the host is leaving: {e.Message}");
                    }
                }
            }

            if (_nm != null && _nm.IsListening) _nm.Shutdown();

            _helloByClient.Clear();

            // ⚠️⚠️ `EndMatch` IS NOT ENOUGH HERE AND THAT IS WHY `Reset` EXISTS. `NetSession`
            // owns ONE `LobbySession` for the lifetime of the process, so host, quit to menu,
            // host again used to reach `OpenLobby` with the previous session's peer table,
            // its leader id and `MatchInProgress` still set. A brand new lobby then believed
            // it already had four players, obeyed a leader whose transport was gone, and
            // answered Spectate to the first person who tried to join it.
            Lobby.EndMatch();
            Lobby.Reset();
            Lobby.IsDedicated = false;
            LocalSlot = 0;

            // ⚠️ THE NEXT SESSION MUST RE-APPLY ITS SEAT EVEN IF IT IS THE SAME NUMBER. See
            // ApplyAssignedSeat: without clearing this, hosting again after leaving a lobby
            // where you sat in seat 0 would drop the host's own seat 0 as "no change" and the
            // arena would never be told to wire anything up.
            _seatApplied = false;
            IsRelay = false;
            RelayJoinCode = null;
            SetStatus("offline");
        }

        public void BrowseLan() => _beacon.StartListening();

        public System.Collections.Generic.IEnumerable<LanEntry> LanEntries => _beacon.Entries;

        /// <summary>
        /// Pull a trailing `:port` off an address, leaving a bare host the transport can use.
        /// Returns the address unchanged when it carries no port. See <see cref="StartClient"/>.
        /// </summary>
        public static string SplitHostPort(string address, ref int port)
        {
            if (string.IsNullOrWhiteSpace(address)) return address;

            address = address.Trim();

            // `[::1]:8910` and `[::1]`. The brackets are what make an IPv6 port unambiguous, so
            // they are the only shape in which one is read.
            if (address.StartsWith("["))
            {
                int close = address.IndexOf(']');
                if (close < 0) return address;

                string inner = address.Substring(1, close - 1);
                if (close + 2 < address.Length && address[close + 1] == ':' &&
                    int.TryParse(address.Substring(close + 2), out int bracketed) &&
                    bracketed > 0 && bracketed <= 65535)
                    port = bracketed;

                return inner;
            }

            // Exactly one colon is `host:port`. More than one is a bare IPv6 literal and is left
            // alone; see the note on StartClient for why splitting it would corrupt it.
            int first = address.IndexOf(':');
            if (first < 0 || first != address.LastIndexOf(':')) return address;

            string head = address.Substring(0, first);
            string tail = address.Substring(first + 1);

            if (head.Length == 0) return address;
            if (!int.TryParse(tail, out int parsed) || parsed <= 0 || parsed > 65535) return address;

            port = parsed;
            return head;
        }

        private void Configure(string address, int port)
        {
            _utp.SetConnectionData(address, (ushort)port);
            ConfigureTimeouts();
        }

        /// <summary>
        /// The transport's patience, and nothing else.
        ///
        /// ⚠️ A GENEROUS TIMEOUT ON PURPOSE. This game is played on venue wifi and Philippine
        /// home connections, and a peer briefly stalling is normal. Dropping them fast means
        /// dropping them often, and a dropped seat costs the other three a real player for
        /// the rest of the match.
        ///
        /// ⚠️⚠️ SPLIT OUT OF `Configure` BECAUSE THE RELAY PATHS COULD NOT CALL THAT AND SO GOT
        /// NONE OF THIS. `Configure` opens with `SetConnectionData`, which resets the transport's
        /// protocol to plain UnityTransport and would undo the `SetRelayServerData` the relay
        /// paths had just done. So relay ran on whatever the last LAN attempt happened to leave
        /// behind, or on UTP's own defaults in a process that had never touched LAN — a 1000 ms
        /// connect timeout, on the one route in this game that goes through a datacentre rather
        /// than across the room. The more latent path had the less patient settings.
        /// </summary>
        private void ConfigureTimeouts()
        {
            // ⚠️⚠️ 8000, DOWN FROM 30000, AND THIRTY SECONDS IS NOT A TIMEOUT ANYBODY CAN PLAY
            // THROUGH. This is the silence timer: how long a peer keeps believing in a machine it
            // has stopped hearing from. `Stop` now sends a real `DisconnectClient` so an orderly
            // exit is instant and never reaches this at all, but the case this covers is the one
            // that cannot say goodbye — alt-F4, a pulled cable, a laptop lid. Half a minute of
            // four people standing in a match whose host is gone is most of a round.
            //
            // ⚠️ AND IT IS NOT SET SHORTER THAN THAT, because it is also what a real network
            // hiccup is measured against. The VPS is 48 ms from Manila and the LAN is under 2;
            // eight seconds of complete silence on either is a machine that has gone, not a
            // machine that is late.
            _utp.DisconnectTimeoutMS = 8000;
            _utp.ConnectTimeoutMS = 2000;
            _utp.MaxConnectAttempts = 12;
        }

        /// <summary>
        /// ⚠️⚠️ TRUE ONCE A SEAT HAS ACTUALLY BEEN APPLIED, AND IT IS WHAT MAKES THE TWO SEAT
        /// PROTOCOLS IDEMPOTENT. `LocalSlot` alone cannot answer "has this been set yet", because
        /// its own default is 0 and 0 is a real seat: without this flag the host's first
        /// announcement of seat 0 looks like a no-op and is dropped. See
        /// <see cref="ApplyAssignedSeat"/>.
        /// </summary>
        private bool _seatApplied;

        public void SetLocalSeating(int seat, bool spectator)
        {
            if (_seatApplied && LocalSlot == seat && GameLaunch.Spectator == spectator) return;

            _seatApplied = true;
            LocalSlot = seat;
            GameLaunch.Spectator = spectator;
            SetStatus($"seated in slot {seat} (spectator={spectator})");
            SeatingChanged?.Invoke();
        }

        public void SetStatusForHost()
        {
            SetStatus($"{Lobby.PeerCount} connected");
        }

        /// <summary>
        /// Republishes the three counts every browser reads: how many chairs are being PLAYED,
        /// how many are UNAVAILABLE (played plus held for a dropped peer), and how many sockets
        /// are attached.
        ///
        /// ⚠️⚠️ ONE NUMBER USED TO SERVE ALL THREE AND IT WAS `Lobby.PeerCount`. That counts
        /// connections, so two players and six spectators advertised 8/4 and the LAN browser
        /// struck the lobby out as full; the other direction, a seat held for somebody who
        /// dropped mid-match, advertised as free and then refused whoever pressed join. Both are
        /// reports of "the server browser lies", and neither is fixable while the concepts share
        /// a field. `LobbySession.OccupiedSeatCount` and `ConnectedHumanCount` are the two
        /// missing questions.
        /// </summary>
        /// <summary>
        /// The local half: four counts written onto the beacon, costing nothing and safe to run
        /// every frame.
        ///
        /// ⚠️⚠️ SPLIT OUT OF <see cref="PublishLobbyCounts"/> BECAUSE `InProgress` DOES NOT
        /// CHANGE ON A CONNECT OR A DISCONNECT, AND THOSE WERE THE ONLY THINGS THAT PUBLISHED.
        /// A match starting is not either one, so the beacon went on advertising
        /// `InProgress = false` for the whole match. The browser drew a running game as "IN THE
        /// LOBBY", and worse, `LanEntry.IsJoinable` opens with `!InProgress`, so it offered JOIN
        /// on it. That is the third instance of the fault this method's own note is about: the
        /// server browser telling the truth requires the fields to be RE-READ, not merely to
        /// exist.
        /// </summary>
        private void RefreshBeaconCounts()
        {
            if (_beacon == null) return;

            _beacon.MaxPlayers = LobbySession.MaxPlayers;
            _beacon.MaxConnections = LobbySession.MaxConnections;
            _beacon.Players = Lobby.SeatedPeerCount();
            _beacon.Occupied = Lobby.OccupiedSeatCount();
            _beacon.Connections = Lobby.ConnectedHumanCount();
            _beacon.InProgress = Lobby.MatchInProgress;
        }

        /// <summary>
        /// Push the current counts and the current <see cref="Advert"/> to the lobby record.
        ///
        /// ⚠️⚠️ IT IS THE SAME WRITE `PublishLobbyCounts` ALREADY MAKES ON EVERY SEAT
        /// CHANGE, NOT A SECOND ONE. `Matchmaker` changes the advert at three moments and all
        /// three are moments the outside world has to be told about anyway: entering the queue,
        /// cancelling it, and opening a backfill seat. Giving the matchmaker its own writer would
        /// be a second place that decides what a lobby record says, which is the shape
        /// `docs/TODO.md` § 38.5 found three dead protocols in.
        /// </summary>
        public void RepublishLobbyAdvert() => PublishLobbyCounts();

        private void PublishLobbyCounts()
        {
            if (_beacon == null) return;

            RefreshBeaconCounts();

            if (Query != null && IsRelay)
            {
                _ = Query.UpdateHostedLobbyAsync(Lobby.SeatedPeerCount(),
                                                 Lobby.OccupiedSeatCount(),
                                                 Lobby.MatchInProgress,
                                                 Advert);
            }
        }

        /// <summary>
        /// Put the player's durable reconnect identity into Netcode's approval payload. A
        /// NetworkClientId belongs to one transport connection and necessarily changes after a
        /// disconnect; using it as identity is what made a returning defender reappear in the
        /// attacker's old seat.
        /// </summary>
        private void ConfigureClientHello()
        {
            var settings = Settings.SettingsStore.Current;
            var account = GameServices.Account;
            var hello = new ConnectionHello
            {
                Protocol = ProtocolVersion,
                Token = account?.ConnectionToken ?? NetIdentity.Token,
                Name = LocalLobbyName(),

                // ⚠️ WHATEVER IS CACHED, AND NEVER A NETWORK CALL FROM HERE. This runs inside
                // every start path including the two LAN ones, and `FUTURE.md` § 0.5 rule 7 says
                // a LAN match may never sit behind a login. `PrimeHandleProofAsync` fetches one
                // on the relay paths, before this; empty here is a normal, playable state.
                AccountPlayerId = account != null && account.IsSignedIn ? account.PlayerId : "",
                HandleProof = account?.HandleProof ?? "",
            };

            _nm.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(hello));
        }

        /// <summary>
        /// Fetches a handle proof before an ONLINE start path builds its hello.
        ///
        /// ⚠️⚠️ IT IS CALLED ON THE RELAY PATHS ONLY, AND THAT IS THE RULE RATHER THAN AN
        /// OPTIMISATION. `FUTURE.md` § 0.5 rule 7: LAN, direct-address joins, Practice and
        /// Training may never sit behind a login, so none of them may spend a service call on the
        /// way to a match. A LAN peer arrives with no proof and keeps the name it claims, exactly
        /// as it did before the guard existed.
        /// </summary>
        private static async Task PrimeHandleProofAsync()
        {
            var account = GameServices.Account;
            if (account == null) return;
            await account.EnsureHandleProofAsync();
        }

        /// <summary>
        /// One answer per (player id, proof) pair, so a reconnect inside the fast-reconnect
        /// window and a second `Identify` from a live peer cost nothing.
        ///
        /// ⚠️ AN `Unreachable` ANSWER IS DELIBERATELY NOT CACHED. It describes the network for a
        /// moment rather than the player, and caching it would hold a whole lobby unverified for
        /// the rest of the session because the Wi-Fi dropped one packet while the first peer was
        /// arriving.
        /// </summary>
        private readonly Dictionary<string, (Core.AccountRules.HandleCheck Check, string Handle)>
            _handleChecks = new Dictionary<string, (Core.AccountRules.HandleCheck, string)>();

        /// <summary>
        /// The host side of `docs/TODO.md` § 88.1c: asks the account endpoint whether an arriving
        /// peer owns the handle it claimed, then re-resolves the lobby name from the answer.
        ///
        /// ⚠️⚠️ IT IS FIRE AND FORGET AND NOTHING WAITS ON IT. The seat, the picks and the whole
        /// lobby are already resolved by the time this starts. If it never answers, the lobby is
        /// the lobby that shipped before the guard, which is the only acceptable failure mode for
        /// a game that has to run in a hall with the internet unplugged.
        /// </summary>
        /// <summary>
        /// A disconnect reason as one of a handful of groupable buckets.
        ///
        /// ⚠️ THE BUCKETS ARE MATCHED AGAINST WHAT THIS FILE ITSELF WRITES. `ApproveConnection`
        /// composes the version and capacity sentences a few hundred lines above, and `Admit`
        /// composes the replacement one, so these substrings are not guesses about a vendor
        /// string. If one of those sentences is reworded, this reads `other` rather than lying,
        /// which is the right way round for a number nobody is watching.
        /// </summary>
        private static string ClassifyDisconnect(string reason, bool wasLocal)
        {
            if (wasLocal) return "local";
            if (string.IsNullOrWhiteSpace(reason)) return "dropped";
            if (reason.Contains("protocol") || reason.Contains("version")) return "version";
            if (reason.Contains("full")) return "full";
            if (reason.Contains("Replaced")) return "replaced";
            if (reason.Contains("identity")) return "identity";
            return "other";
        }

        /// <summary>
        /// The entry point for the other arrival path. `MatchRpc.HandleIdentify` admits a peer
        /// too, so the guard has to hang off both or it has a documented way around it.
        /// </summary>
        public void VerifyArrival(int peerId, string accountPlayerId, string proof)
            => VerifyArrivalAsync(peerId, accountPlayerId, proof);

        private async void VerifyArrivalAsync(int peerId, string accountPlayerId, string proof)
        {
            if (!IsHost || string.IsNullOrEmpty(accountPlayerId) || string.IsNullOrEmpty(proof))
                return;

            // ⚠️ ONLY ONLINE. On LAN there is no endpoint to ask and no login to sit behind, and
            // asking anyway would put a several-second service timeout in the path of a hall full
            // of machines joining off the beacon.
            if (!IsRelay) return;

            // ⚠️⚠️ THE WHOLE BODY IS GUARDED BECAUSE THIS IS `async void`. Nothing awaits it, so
            // an exception escaping here has no caller to land in and takes the process with it.
            // A guard that fails must cost a name, never a match.
            try
            {
                string key = accountPlayerId + "|" + proof;
                if (!_handleChecks.TryGetValue(key, out var answer))
                {
                    answer = await PlayerAccount.VerifyHandleAsync(accountPlayerId, proof);
                    if (answer.Check != Core.AccountRules.HandleCheck.Unreachable)
                        _handleChecks[key] = answer;
                }

                if (Lobby == null) return;
                if (!Lobby.ApplyHandleCheck(peerId, accountPlayerId, answer.Check, answer.Handle)) return;

                if (answer.Check == Core.AccountRules.HandleCheck.NotOwned)
                {
                    var record = Lobby.PeerById(peerId);
                    Debug.LogWarning(
                        $"[Net] peer {peerId} claimed a handle it cannot prove; seated as " +
                        $"{record?.Name}. docs/TODO.md § 88.1c.");
                }

                MatchRpc.Instance?.BroadcastLobbyPicks();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Net] handle verification for peer {peerId} failed; " +
                                 $"the claimed name stands: {e.Message}");
            }
        }

        private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
                                       NetworkManager.ConnectionApprovalResponse response)
        {
            var hello = DecodeHello(request.Payload);
            bool protocolMatches = hello != null && hello.Protocol == ProtocolVersion;
            bool hasCapacity = _nm == null ||
                               Math.Max(_nm.ConnectedClientsIds.Count, _helloByClient.Count)
                               < LobbySession.MaxConnections;

            // ⚠️⚠️ THE BLOCK LIST IS ENFORCED HERE, AND THIS IS THE ONLY PLACE IN THE GAME IT
            // CAN DO ANYTHING TODAY. `FUTURE.md` § 6 asks for *"blocking, which must survive
            // matchmaking: a blocked player is never queued into your match"*, and there is no
            // matchmaker until Phase 7. **In this build that requirement is "a blocked player
            // cannot join the lobby you host"**, which is the same guarantee for the only way
            // two players can currently end up in one room. `docs/TODO.md` § 102.
            //
            // ⚠️ IT IS THE ACCOUNT ID, NOT THE CLAIMED NAME. A block keyed on a handle is a block
            // somebody escapes by renaming themselves, and § 88.1c spent a whole entry on the
            // difference between a claim and an identity. The id in the hello is unverified at
            // this instant — `VerifyArrivalAsync` runs after seating — but a liar's only gain is
            // sending an id that is NOT on the list, which is the same as not being blocked.
            // **A block cannot be defeated by lying about who you are, only by not being you.**
            //
            // ⚠️ AND IT IS THE HOST'S OWN LIST. A client's block list is nobody else's business
            // and does not travel; the person who owns the room decides who is in it.
            bool blocked = hello != null &&
                           Core.SocialRules.IsBlocked(GameServices.Social?.List, hello.AccountPlayerId);

            response.Approved = protocolMatches && hasCapacity && !blocked;
            response.CreatePlayerObject = false;
            response.Pending = false;
            // ⚠️ THE REFUSAL SAYS WHAT THE ROOM HOLDS. "Lobby is full" is true of a room with
            // four players and true of a room with four players and four spectators, and only
            // one of those two is a thing the person reading it can do anything about — namely
            // wait for somebody to leave rather than for a match to end. See
            // `LobbySession.MaxSpectators`.
            // ⚠️ THE BLOCK'S REASON DOES NOT SAY IT IS A BLOCK. `SocialRules.WhyCannotRequest`
            // carries the same rule for friend requests: telling somebody they have been blocked
            // is how a block becomes an argument, and the host is a player in the same room.
            // "Could not join" is what every shipping game says.
            response.Reason = !protocolMatches
                ? $"Game version mismatch (network protocol {ProtocolVersion})"
                : blocked
                    ? "Could not join this game."
                    : hasCapacity
                        ? string.Empty
                        : $"This game is full: {LobbySession.MaxPlayers} players and "
                          + $"{LobbySession.MaxSpectators} spectators.";

            if (response.Approved) _helloByClient[request.ClientNetworkId] = hello;
        }

        private static ConnectionHello DecodeHello(byte[] payload)
        {
            try
            {
                if (payload != null && payload.Length > 0)
                {
                    var decoded = JsonUtility.FromJson<ConnectionHello>(Encoding.UTF8.GetString(payload));
                    if (decoded != null && !string.IsNullOrWhiteSpace(decoded.Token))
                    {
                        decoded.Token = decoded.Token.Trim();
                        if (decoded.Token.Length > 128)
                            decoded.Token = decoded.Token.Substring(0, 128);
                        decoded.Name = string.IsNullOrWhiteSpace(decoded.Name) ? "Player" : decoded.Name.Trim();
                        return decoded;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Net] Ignoring malformed connection identity: {e.Message}");
            }

            return null;
        }

        private void RegisterSeatHandler()
        {
            if (_nm?.CustomMessagingManager == null) return;
            if (ReferenceEquals(_seatHandlerOn, _nm.CustomMessagingManager)) return;

            _nm.CustomMessagingManager.RegisterNamedMessageHandler(
                SeatAssignmentMessage, OnSeatAssignmentMessage);
            _seatHandlerOn = _nm.CustomMessagingManager;
        }

        private void OnSeatAssignmentMessage(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int seat);
            ApplyAssignedSeat(seat);
        }

        private void SendSeatAssignment(ulong clientId, int seat)
        {
            if (_nm == null) return;

            if (_nm.IsClient && clientId == _nm.LocalClientId)
            {
                ApplyAssignedSeat(seat);
                return;
            }

            using var writer = new FastBufferWriter(sizeof(int), Allocator.Temp);
            writer.WriteValueSafe(seat);
            _nm.CustomMessagingManager.SendNamedMessage(SeatAssignmentMessage, clientId, writer);
        }

        /// <summary>
        /// Applies the host's authoritative seat on this process.
        ///
        /// ⚠️⚠️ THERE ARE TWO SEAT PROTOCOLS AND THIS IS THE ONE THAT DOES LESS, WHICH IS WHY A
        /// CLIENT COULD END UP UNABLE TO MOVE. The host announces a seat TWICE by two unrelated
        /// routes: `NetSession.OnClientConnected` admits the peer and sends its own
        /// `tp.seat.assignment.v1`, which lands here; and `MatchRpc.HandleIdentify` admits the
        /// same peer again and sends `Seating`, which lands in `OnSeatingMsg` and calls
        /// `SetLocalSeating`. Only the second one carries the join code, the leader, whether a
        /// match is in progress, and the `SeatingChanged` notification that makes the ARENA move
        /// the camera, the HUD, the ready gate and the `PlayerInputReader` onto the new chair.
        ///
        /// 🧑 2026-08-27, from the joining laptop: *"i can move camera and see updates but i cant
        /// move"*, and its `Player.log` reads `[Net] connected as seat 2` with **no**
        /// `[Net] seated in slot 1` anywhere. That is this method having run and `SetLocalSeating`
        /// having not: the seat number was applied and nothing was told about it.
        ///
        /// ⚠️ SO THIS RAISES `SeatingChanged` TOO. It is the same fix as `docs/TODO.md` § 53.1
        /// one layer down: whichever of the two messages wins the race, the arena hears about it.
        /// `MatchInstaller.FollowLocalSeat` is idempotent, so both winning costs nothing.
        ///
        /// ⚠️⚠️ AND THE DUPLICATION ITSELF IS THE REAL DEFECT, NOT THIS SYMPTOM. Two protocols
        /// for one fact, one of them a subset of the other, is exactly the shape § 53.1 and
        /// § 57.1 were. `docs/TODO.md` § 60 carries retiring one of them; it is not done here,
        /// because deleting a seat path while two laptops are mid-test is how a working build
        /// becomes an unworking one.
        /// </summary>
        /// ⚠️⚠️ AND IT IS IDEMPOTENT NOW, WHICH IS WHAT STOPPED THE JOIN FROM BOUNCING. Measured
        /// on two real peers over a loopback transport, ONE join produced three seat
        /// announcements and SIX `SeatingChanged` events:
        ///
        ///     [Net] connected as seat 2          <- this method, from tp.seat.assignment.v1
        ///     [NetSeat] seat changed: ...        <- x2, from the duplicated raise below
        ///     [Net] seated in slot 1             <- SetLocalSeating, from Seating
        ///     [NetSeat] seat changed: ...
        ///     [Net] connected as seat 2          <- this method AGAIN, same seat
        ///     [NetSeat] seat changed: ... x2
        ///     [Net] connected as seat 2          <- and again, seconds later
        ///     [NetSeat] seat changed: ... x2
        ///
        /// Every one of those rebuilds the local seat: `MatchInstaller` moves the camera, the
        /// HUD, the input reader and the `PlayerInputReader` onto the chair again. Doing that
        /// six times for one join is what a joining player sees as the view snapping about. 🧑
        /// 2026-08-28: *"when a non host player tries to join, it just bounces back and forth a
        /// lot of times"*.
        ///
        /// ⚠️ THE RAISE WAS LITERALLY WRITTEN TWICE, and that is a plain duplicated line rather
        /// than a race guard: the paragraph above it argues for raising the event HERE AS WELL AS
        /// in `SetLocalSeating`, which is one raise, not two. Redundancy across the two protocols
        /// is deliberate; redundancy inside one call is not.
        ///
        /// ⚠️ REPEATS ARE DROPPED RATHER THAN THE SECOND PROTOCOL BEING DELETED. Retiring one of
        /// them is `docs/TODO.md` § 60 and is still the right end state; until then, making both
        /// idempotent costs nothing and means whichever wins the race, the arena is rebuilt once.
        public void ApplyAssignedSeat(int seat)
        {
            bool spectator = seat < 0;
            if (_seatApplied && LocalSlot == seat && GameLaunch.Spectator == spectator) return;

            _seatApplied = true;
            LocalSlot = seat;
            GameLaunch.Spectator = spectator;
            SetStatus(seat >= 0 ? $"connected as seat {seat + 1}" : "connected as spectator");
            SeatingChanged?.Invoke();
        }

        // -------------------------------------------------------------------

        private void OnClientConnected(ulong clientId)
        {
            if (!IsHost)
            {
                if (clientId == _nm.LocalClientId)
                {
                    _everConnected = true;
                    MatchRpc.Instance?.Initialize(_nm);
                    SetStatus("connected");
                    var s = Settings.SettingsStore.Current;
                    int charPick = s.CharacterPick >= 0 ? s.CharacterPick : 0;
                    int canPick = s.CanPick >= 0 ? s.CanPick : 0;
                    int slipperPick = s.SlipperPick >= 0 ? s.SlipperPick : 0;
                    var identity = GameServices.Account;
                    MatchRpc.Instance?.IdentifyServerRpc(
                        identity?.ConnectionToken ?? NetIdentity.Token,
                        LocalLobbyName(),
                        identity != null && identity.IsSignedIn ? identity.PlayerId : "",
                        identity?.HandleProof ?? "",
                        charPick, canPick, slipperPick);
                }
                return;
            }

            MatchRpc.Instance?.Initialize(_nm);

            // ⚠️ THE HOST SEATS THEM, AND THE HOST DECIDES. A client sends its token and name;
            // where it sits is not up to it. See LobbySession.RuleOnArrival for why the branch
            // order matters: a returning player outranks a newcomer for their own seat.
            var settings = Settings.SettingsStore.Current;
            ConnectionHello hello;
            if (clientId == _nm.LocalClientId)
            {
                hello = new ConnectionHello
                {
                    Token = GameServices.Account?.ConnectionToken ?? NetIdentity.Token,
                    Name = LocalLobbyName()
                };
            }
            else if (!_helloByClient.TryGetValue(clientId, out hello))
            {
                Debug.LogWarning($"[Net] Connected client {clientId} has no approved identity; disconnecting.");
                _nm.DisconnectClient(clientId, "Missing approved identity");
                return;
            }

            var record = Lobby.Admit((int)clientId, hello.Token, hello.Name,
                                     out int replacedPeerId);
            SendSeatAssignment(clientId, record.Seat);

            // ⚠️ AFTER THE SEAT, NOT BEFORE IT. The peer is playing by the end of this method;
            // the guard only decides what it is CALLED. `docs/TODO.md` § 88.1c.
            VerifyArrivalAsync((int)clientId, hello.AccountPlayerId, hello.HandleProof);

            // A relaunch can establish the new socket before the generous 30 second timeout
            // retires the old one. The durable token has already moved the seat above, so the
            // stale transport is now both unnecessary and dangerous: without disconnecting it,
            // it can keep submitting movement and verbs for the same player.
            if (replacedPeerId >= 0 && replacedPeerId != (int)clientId &&
                _nm.ConnectedClients.ContainsKey((ulong)replacedPeerId))
            {
                _helloByClient.Remove((ulong)replacedPeerId);
                _nm.DisconnectClient((ulong)replacedPeerId, "Replaced by reconnect");
            }

            // ⚠️ THE HOST NEVER SENDS ITSELF AN IdentifyServerRpc, so its own character, can and
            // slipper picks reach the lobby from here or not at all. Every other seat's picks
            // still arrive on the RPC.
            if (clientId == _nm.LocalClientId)
            {
                int charPick = settings.CharacterPick >= 0 ? settings.CharacterPick : 0;
                int canPick = settings.CanPick >= 0 ? settings.CanPick : 0;
                int slipperPick = settings.SlipperPick >= 0 ? settings.SlipperPick : 0;
                Lobby.SetPicks((int)clientId, charPick, canPick, slipperPick);

                // ⚠️⚠️ AND ITS OWN COSMETICS, FOR THE SAME REASON AND IN THE SAME PLACE. The
                // host never sends itself an `Identify`, so this is the only path on which its
                // banner and palette are ever authorised; without it the host is the one seat in
                // the room wearing nothing, on every screen including its own. `docs/TODO.md`
                // § 101. **It still goes through `BannerRules.Authorise`** rather than being
                // trusted: the copy nobody checks is the copy that is wrong (§ 94.1).
                // ⚠️ AND ITS CUSTOM CHARACTER RIDES THE SAME CALL, for the same reason: this is
                // the only path on which the HOST's own seat is ever told what it is bringing.
                // Without it the one seat that cannot fail to be there is the one seat that never
                // brings a custom character, on its own screen and on everybody else's.
                MatchRpc.Instance?.HostAuthoriseCosmetics(
                    (int)clientId, LocalCosmetics.Encoded(charPick), charPick,
                    LocalCosmetics.CustomCharacter());
            }

            PublishLobbyCounts();
            MatchRpc.Instance?.BroadcastLobbyPicks();
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (IsHost && clientId != _nm.LocalClientId)
            {
                // ⚠️ THE SEAT IS HELD, NOT FREED, so a reconnecting player gets their own chair
                // back rather than finding a stranger in it holding their score.
                MatchRpc.Instance?.HostPeerLeft((int)clientId);
                _helloByClient.Remove(clientId);
                PublishLobbyCounts();
                MatchRpc.Instance?.BroadcastLobbyPicks();
                SetStatus($"{Lobby.PeerCount} connected");
                return;
            }

            // ⚠️⚠️ THE REASON IS THE WHOLE POINT OF THIS BRANCH NOW. A refused approval arrives
            // here as an ordinary disconnect, so a build-version mismatch, a full lobby and a
            // host that vanished were all one word: "disconnected". The player then has nothing
            // to act on, and a version mismatch in particular is a thing they CAN fix.
            string reason = _nm != null ? _nm.DisconnectReason : null;
            SetStatus(string.IsNullOrWhiteSpace(reason) ? "disconnected" : $"disconnected: {reason}");

            // ⚠️⚠️ THE REASON IS REDUCED TO A CLASS BEFORE IT IS COUNTED, AND THE SENTENCE IS
            // NEVER SENT. `FUTURE.md` § 3 asks for a disconnect rate, which needs four or five
            // buckets; the string itself is host-authored free text that a modified peer could
            // put anything at all into, and `TelemetryRules.Label` would refuse it for having a
            // space in it anyway. A bucket is groupable, a sentence is a hundred distinct values
            // for one cause. `docs/TODO.md` § 90.3.
            GameServices.Telemetry?.NoteDisconnect(ClassifyDisconnect(reason, _localShutdown));

            // ⚠️ A DISCONNECT WE ASKED FOR IS NOT AN EVENT ANYBODY NEEDS TELLING ABOUT. The
            // player is already navigating; announcing it and dragging them to the join screen
            // would fight the button they just pressed.
            bool wasLocal = _localShutdown;
            bool wasConnected = _everConnected;
            _localShutdown = false;
            _everConnected = false;

            LocalSlot = 0;
            _seatApplied = false;
            IsRelay = false;
            RelayJoinCode = null;
            _helloByClient.Clear();
            Lobby.Reset();

            // ⚠️ A CONNECTION THAT NEVER COMPLETED IS NOT A DISCONNECTION. The join screen is
            // already showing the attempt and reports its own failure; navigating from here as
            // well takes the player out of a lobby they are still arriving in.
            if (wasLocal || !wasConnected) return;

            LastDisconnectReason = PlayerFacingDisconnectReason(reason);
            ClientDisconnected?.Invoke(LastDisconnectReason);
            return;
        }

        /// <summary>
        /// The reason in words a player can act on, or "" when there is nothing worth saying.
        ///
        /// ⚠️⚠️ NETCODE'S OWN EVENT ENVELOPE IS NOT PLAYER-FACING TEXT.
        /// `[Disconnect Event][Client-0][TransportClientId-0][TransportShutdown]
        /// NetworkConnectionManager was shutdown. The transport was shutdown.` is a diagnostic,
        /// and it was being printed on a menu in the game's own font. It also says nothing: it
        /// describes the mechanism, never the cause.
        ///
        /// ⚠️ WHAT IS KEPT IS WHAT THE HOST ITSELF WROTE. `ApproveConnection` sets
        /// `response.Reason` to "Game version mismatch (network protocol 5)" or "Lobby is full",
        /// and NGO delivers exactly that string. Those two are the whole point of the mechanism:
        /// a version mismatch is a thing the player CAN fix, and it is the likeliest failure
        /// whenever two machines were built from different commits.
        /// </summary>
        private static string PlayerFacingDisconnectReason(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Lost connection to the host.";

            raw = raw.Trim();

            // Netcode wraps its own transport events in brackets. Nothing the host authors does.
            if (raw.StartsWith("[")) return "Lost connection to the host.";

            return raw;
        }

        public void SetStatus(string s)
        {
            Status = s;
            StatusChanged?.Invoke(s);
            Debug.Log($"[Net] {s}");
        }
    }
}
