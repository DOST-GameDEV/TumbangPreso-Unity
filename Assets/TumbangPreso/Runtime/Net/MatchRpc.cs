using System;
using System.Collections;
using System.Collections.Generic;
using TumbangPreso.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// The gameplay RPC and message transport, converted from the @rpc surface spread across
    /// carrier.gd, character_base.gd and main.gd.
    ///
    /// ⚠️ EVERY VERB IS A REQUEST TO THE HOST, NEVER A LOCAL RESOLUTION. A client that
    /// resolved its own tag would be authoritative over somebody else's stun. The pattern is
    /// always the same: the client asks, the host decides using the same rule the solo game
    /// uses, and the host broadcasts what happened. That is why NetAuthority.ShouldResolve
    /// exists and why nothing here calls a gameplay method directly.
    ///
    /// ⚠️ POSITION AND FACING TRAVEL WITH THE REQUEST. The host must judge the verb against
    /// where the client believed it was standing, not where the host currently thinks it is,
    /// otherwise every lunge is judged a frame or two late and misses on a lagged connection
    /// while looking like a direct hit on the client's screen.
    ///
    /// ⚠️ AND THE VISUAL HALF IS SEPARATE FROM THE RESOLUTION HALF. A charge-up read
    /// broadcasts on its own because the other players need to see a wind-up
    /// before it resolves; folding it into the result would show the tell and the tag on the
    /// same frame, which removes the only warning the game gives.
    /// </summary>
    public sealed class MatchRpc : MonoBehaviour
    {
        public static MatchRpc Instance { get; private set; }

        private NetworkManager _nm;
        private readonly LobbySeatInfo[] _replicatedSeats = new LobbySeatInfo[Balance.PlayerCount];
        /// <summary>
        /// The messaging manager these handlers are registered ON, not merely whether they once
        /// were.
        ///
        /// ⚠️⚠️ A BOOL HERE MEANT THE GAME COULD BE JOINED EXACTLY ONCE PER LAUNCH.
        /// `NetworkManager.Shutdown` DESTROYS its `CustomMessagingManager`, and `StartClient`
        /// builds a new one. Every handler registered on the old instance dies with it, but the
        /// flag saying "registered" survived, so `RegisterHandlers` returned early on the second
        /// session and this router registered **nothing at all**. A client would connect, be
        /// seated by `NetSession`'s own low-level message, and then hear no `Seating`, no
        /// `SyncWorld`, no `StartMatch` and no `SyncUnit` for the rest of the process.
        ///
        /// 🧑 2026-08-28, and it is as exact a description of a process-lifetime flag as anybody
        /// could write: *"so i was able to start a game when i first opened and i could join as
        /// non host"*, *"afterwards i couldnt"*, *"i could only join a game again after
        /// restart"*.
        ///
        /// ⚠️ COMPARING THE INSTANCE IS SELF-HEALING, WHICH A RESET CALL WOULD NOT BE. Clearing a
        /// flag from `Stop` works only while every teardown path remembers to call it, and
        /// remembering is what failed here: `OnDestroy` unregisters, `Stop` did not, and NGO can
        /// replace the manager without either being involved. Asking "is this the manager I
        /// registered on" cannot be forgotten by a future caller.
        /// </summary>
        private Unity.Netcode.CustomMessagingManager _handlersOn;
        private bool _snapshotRequestStarted;
        private float _matchSyncLeft;
        private readonly Dictionary<int, double> _lastAcceptedMoveAt = new Dictionary<int, double>();

        private const float MatchSyncInterval = 0.20f;
        private const float MoveBaseLeeway = 0.85f;
        private const float MoveMaxMetresPerSecond = 28.0f;
        private const float IntentPoseLeeway = 2.25f;

        public LobbySeatInfo GetSeatInfo(int slot)
        {
            if (slot < 0 || slot >= Balance.PlayerCount) return null;
            if (NetAuthority.IsHost)
            {
                var lobby = NetSession.Instance?.Lobby;
                var peer = lobby?.PeerInSeat(slot);
                if (peer != null)
                {
                    return new LobbySeatInfo
                    {
                        Seat = slot,
                        PeerId = peer.PeerId,
                        Name = peer.Name,
                        Occupied = true,
                        Spectator = peer.Spectator,
                        CharacterPick = peer.CharacterPick,
                        CanPick = peer.CanPick,
                        SlipperPick = peer.SlipperPick,

                        // ⚠️ THE HOST ANSWERS ITS OWN QUESTION FROM ITS OWN SET, so the lobby
                        // draws the same tick on the host's screen that the broadcast puts on
                        // everybody else's. See `LobbySeatInfo.Ready`.
                        Ready = _lobbyReady.Contains(peer.PeerId),

                        // ⚠️⚠️ THE BANNER, THE LOOK AND THE CUSTOM CHARACTER ARE ANSWERED HERE
                        // TOO, AND THE FIRST TWO WERE NOT. `MatchInstaller.BuildSeat` calls
                        // `GetSeatInfo`, and on the HOST that is this branch rather than the
                        // replicated table, so anything missing from this object is a field the
                        // host draws blank on its own screen while every client draws it
                        // correctly. That is the hardest kind of cosmetic bug to see, because the
                        // machine reporting it is the only one it is wrong on.
                        Banner = peer.Banner ?? new BannerSelection(),
                        Look = peer.Look ?? "",
                        Custom = peer.Custom ?? "",
                        Build = peer.Build ?? "",
                    };
                }
                return new LobbySeatInfo { Seat = slot, Occupied = false };
            }
            return _replicatedSeats[slot] ?? new LobbySeatInfo { Seat = slot, Occupied = false };
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[Net] Refusing a second MatchRpc router. Custom message handlers must have one owner.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            for (int i = 0; i < Balance.PlayerCount; i++)
            {
                _replicatedSeats[i] = new LobbySeatInfo { Seat = i, Occupied = false };
            }
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// How the two POSITION streams are sent, and the only two messages in this file that do
        /// not go reliably.
        ///
        /// ⚠️⚠️ THE DEFAULT IS `ReliableSequenced` AND IT WAS BEING TAKEN, WHICH IS THE WHOLE OF
        /// 🧑 2026-08-29: *"idk if its bcz we are using hamachi or its genuinely broken but the
        /// bots go out of bounds and lan is highly buggy ... for online servers it isnt like
        /// that"*. `SendNamedMessageToAll` has a `NetworkDelivery` parameter with a default, and
        /// every call in this file omitted it, so `SyncUnit` went out reliably at one message per
        /// body per fixed step: four seats at 50 Hz is 200 guaranteed-delivery messages a second
        /// carrying nothing but a pose that the next one replaces.
        ///
        /// ⚠️⚠️ RELIABLE IS NOT "THE SAME BUT SAFER" FOR A SNAPSHOT STREAM, IT IS ACTIVELY WORSE,
        /// and head-of-line blocking is why. A sequenced channel may not deliver message N+1
        /// until N has arrived, so ONE lost packet holds up every pose behind it until the
        /// retransmit lands, and then the whole backlog arrives at once. On the receiving end
        /// that is a body frozen for a beat and then moved a long way in one step, and
        /// `CharacterMotor.ApplyNetworkTransform` treats a jump over 3 m as a correction and
        /// SNAPS: through a wall, through the chalk, wherever the straight line goes. That is
        /// "the bots go out of bounds" exactly, and the same burst at smaller amplitudes is the
        /// jitter reported beside it. Retransmitting a pose that has already been superseded is
        /// spending the link to deliver something the receiver will discard.
        ///
        /// ⚠️ WHICH IS ALSO WHY LAN WAS THE HALF THAT BROKE. Nothing here is LAN-specific; a
        /// reliable stream at this rate simply needs a link with very little loss to look fine,
        /// and Hamachi is a VPN with a smaller MTU and real packet loss. The relay path was not
        /// better designed, it was luckier.
        ///
        /// ⚠️ SEQUENCED RATHER THAN BARE UNRELIABLE, so an older pose that overtakes a newer one
        /// is DROPPED instead of applied. Both messages carry a complete state rather than a
        /// delta, so a lost one costs 20 ms of smoothing and nothing else, but an out-of-order
        /// one applied would drag the body backwards.
        ///
        /// ⚠️⚠️ AND EVERY OTHER MESSAGE IN THIS FILE STAYS RELIABLE. Chat, seating, the start
        /// whistle, the ready tally, the slipper's state changes and the lata going over are
        /// EVENTS: each one happens once and nothing later repeats it, so a dropped one is a
        /// point never scored or a match that never begins. Only a stream that fully replaces
        /// itself every step can afford to lose a packet, and exactly two of them do.
        /// </summary>
        private const NetworkDelivery PoseDelivery = NetworkDelivery.UnreliableSequenced;

        /// <summary>
        /// How the finished match record is sent, and the other place in this file that does
        /// not take the default.
        ///
        /// ⚠️⚠️ IT IS FRAGMENTED BECAUSE THE MESSAGE IS BIGGER THAN A PACKET, AND THE DEFAULT
        /// WOULD NOT HAVE FAILED LOUDLY. Every other message in this file is tens of bytes;
        /// a `MatchRecord` is four players times twenty-six fields of JSON, which MEASURES
        /// **2312 bytes** at full length, which is past the transport's single-packet payload.
        /// `ReliableSequenced` cannot split a message, so an oversized one is refused by the
        /// transport rather than delivered in pieces: the host logs a line nobody reads and
        /// every client silently gets no end-of-match summary and no career entry, which is
        /// exactly the failure the protocol bump for this message was meant to make
        /// impossible. `ReliableFragmentedSequenced` is the pipeline that exists for this.
        ///
        /// ⚠️ AND MTU IS SMALLER THAN THE NUMBER YOU WOULD GUESS ON THE LINK THEY ACTUALLY
        /// PLAY ON. `PoseDelivery`'s note above records that Hamachi is a VPN with a smaller
        /// MTU and real loss, and that the relay path *"was not better designed, it was
        /// luckier"*. A payload sized against a 1500-byte assumption is the same mistake one
        /// layer up.
        ///
        /// ⚠️ STILL RELIABLE AND STILL SEQUENCED. It is an EVENT that happens once per match
        /// and nothing later repeats it, which is the same test `PoseDelivery`'s note applies
        /// to everything else here: only a stream that fully replaces itself every step can
        /// afford to lose a packet, and a match record is the opposite of that.
        /// </summary>
        private const NetworkDelivery RecordDelivery = NetworkDelivery.ReliableFragmentedSequenced;

        private void OnEnable() => NetSession.ClientDisconnected += HandleClientDisconnected;

        private void OnDisable() => NetSession.ClientDisconnected -= HandleClientDisconnected;

        /// <summary>
        /// The host is gone. Leave, from wherever this peer happens to be.
        ///
        /// ⚠️⚠️ THIS USED TO LIVE ON THE LOBBY SCREEN, WHICH DOES NOT EXIST IN A MATCH. So a
        /// client whose host quit mid-round stayed in the arena forever, driving a body nobody
        /// was refereeing, with the disconnect sitting in the log and nothing acting on it. 🧑
        /// 2026-08-27: *"i closed server and i didnt get kicked out on non host accounts"*, and
        /// the client's `Player.log` carries `[Net] disconnected: Disconnected due to host
        /// shutting down.` on the line where nothing happened.
        ///
        /// ⚠️ `MatchRpc` IS THE ONE OWNER BECAUSE IT IS THE ONE OBJECT THAT IS ALWAYS THERE. It
        /// is `DontDestroyOnLoad`, so it survives every scene the player can be in when the host
        /// vanishes: the lobby, the arena, the character select and the result board.
        /// `ConvertedMatchSetup` had the only copy and it covered exactly one of those.
        ///
        /// ⚠️⚠️ THERE IS NO `IsHost` GUARD HERE AND ADDING ONE BREAKS IT, WHICH IS EXACTLY WHAT
        /// HAPPENED. `NetSession.IsHost` is `_nm == null || !_nm.IsListening || _nm.IsServer`, so
        /// **it answers TRUE the moment the transport stops listening**, which is precisely the
        /// state a peer is in while it is being disconnected. The guard therefore fired on every
        /// client it was meant to protect, and the handler did nothing at all: 🧑 2026-08-27,
        /// *"when i quit as host i still stayed on the game as non host, it didnt close or
        /// disconnect"*, with `[Net] disconnected: Disconnected due to host shutting down.` in
        /// the client's log on the line where nothing happened.
        ///
        /// ⚠️ IT IS SAFE WITHOUT ONE. `NetSession.OnClientDisconnected` returns early for a host
        /// watching somebody else leave, so this event is not raised there at all, and a host
        /// ending its OWN session goes through `Stop`, which sets `_localShutdown` and suppresses
        /// the event before it is raised. What is left is a peer that genuinely lost its session,
        /// and sending that peer back to the join screen is right whichever role it held.
        /// </summary>
        private void HandleClientDisconnected(string reason)
        {
            // ⚠️⚠️ THE LOBBY, NOT `MultiplayerSetup`, AND THIS LINE IS THE WHOLE OF 🧑 2026-08-29:
            // *"sometimes ppl go back to Old ui when they disconnect, they shoudl stay in lobby
            // screen but js get kicked out of current lobby and go back to their own"*.
            //
            // `MultiplayerSetup` is the retired pre-lobby form. `SceneFlow.MultiplayerSetup`'s
            // own note says nothing has navigated to it since § 68.5 and that it is kept only so
            // the redesign can be reverted in one line: this was the last caller, and it was
            // dropping a disconnected player into a screen that is no longer part of the game.
            // It read as "sometimes" because it is a race. `ConvertedMatchSetup` subscribes to
            // the SAME event and handles it correctly in place, so which of the two a player got
            // depended on handler order and on whether the lobby screen happened to be loaded.
            //
            // ⚠️ ALREADY ON THE LOBBY MEANS DO NOTHING, and that is not an optimisation. The
            // lobby's own handler shows the reason, clears the ready tally and opens the join
            // panel; reloading the scene from here would destroy the alert it had just written
            // and hand the player a silent screen instead of the one actionable line they get
            // (a protocol mismatch is a thing they can fix). Two owners, one event, and the one
            // that is on screen wins.
            //
            // ⚠️ `Networked` IS SET SO THE LOBBY COMES UP AS A LOBBY. `ConvertedMatchSetup`
            // derives `IsLobby` from it, and arriving with it false would land the player on the
            // PRACTICE tab with no chat, no seats and no way back to multiplayer except the tab
            // bar. `NetSession.OnClientDisconnected` has already called `Lobby.Reset()` by the
            // time this runs, so what they arrive in is their OWN empty lobby, which is exactly
            // what was asked for.
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                == UI.SceneFlow.MatchSetup)
            {
                return;
            }

            UI.SceneFlow.Networked = true;
            UI.SceneFlow.Go(UI.SceneFlow.MatchSetup);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Initialize(NetworkManager nm)
        {
            _nm = nm;
            RegisterHandlers();

            // ⚠️ A CLIENT ASKS FOR THE WORLD ONCE ITS ARENA EXISTS, rather than trusting the
            // snapshot the host sent at connect time. Transport finishes before SceneFlow has
            // finished building the seats, so on a cold relaunch that first snapshot lands in
            // an empty scene and the joiner sits there with no lata and no seat.
            if (!NetAuthority.IsHost && isActiveAndEnabled && !_snapshotRequestStarted)
            {
                _snapshotRequestStarted = true;
                StartCoroutine(RequestSnapshotWhenArenaReady());
            }
        }

        private void RegisterHandlers()
        {
            if (_nm == null || _nm.CustomMessagingManager == null) return;
            if (ReferenceEquals(_handlersOn, _nm.CustomMessagingManager)) return;

            var cm = _nm.CustomMessagingManager;

            cm.RegisterNamedMessageHandler("Identify", OnIdentifyMsg);
            cm.RegisterNamedMessageHandler("Seating", OnSeatingMsg);
            cm.RegisterNamedMessageHandler("ReqSeat", OnReqSeatMsg);
            cm.RegisterNamedMessageHandler("DeclareReady", OnDeclareReadyMsg);
            cm.RegisterNamedMessageHandler("ReadyTally", OnReadyTallyMsg);
            cm.RegisterNamedMessageHandler("BeginCountdown", OnBeginCountdownMsg);
            cm.RegisterNamedMessageHandler("VoteRematch", OnVoteRematchMsg);
            cm.RegisterNamedMessageHandler("RematchTally", OnRematchTallyMsg);
            cm.RegisterNamedMessageHandler("BeginRematch", OnBeginRematchMsg);
            cm.RegisterNamedMessageHandler("SyncMap", OnSyncMapMsg);
            cm.RegisterNamedMessageHandler("SelectMap", OnSelectMapMsg);
            cm.RegisterNamedMessageHandler("SyncMode", OnSyncModeMsg);
            cm.RegisterNamedMessageHandler("SelectMode", OnSelectModeMsg);
            cm.RegisterNamedMessageHandler("SyncDiff", OnSyncDiffMsg);
            cm.RegisterNamedMessageHandler("SelectDiff", OnSelectDiffMsg);
            cm.RegisterNamedMessageHandler("SyncRules", OnSyncRulesMsg);
            cm.RegisterNamedMessageHandler("SelectRules", OnSelectRulesMsg);
            cm.RegisterNamedMessageHandler("SyncLobbyPicks", OnSyncLobbyPicksMsg);
            cm.RegisterNamedMessageHandler("SelectLobbyPick", OnSelectLobbyPickMsg);
            cm.RegisterNamedMessageHandler("SyncPicks", OnSyncPicksMsg);
            cm.RegisterNamedMessageHandler("SyncWorld", OnSyncWorldMsg);
            cm.RegisterNamedMessageHandler("SyncLata", OnSyncLataMsg);
            cm.RegisterNamedMessageHandler("SyncSlipper", OnSyncSlipperMsg);
            cm.RegisterNamedMessageHandler("LataPose", OnLataPoseMsg);
            cm.RegisterNamedMessageHandler("SlipperPose", OnSlipperPoseMsg);
            cm.RegisterNamedMessageHandler("SubmitMove", OnSubmitMoveMsg);
            cm.RegisterNamedMessageHandler("SyncUnit", OnSyncUnitMsg);
            cm.RegisterNamedMessageHandler("ReqPunch", OnReqPunchMsg);
            cm.RegisterNamedMessageHandler("ReqLunge", OnReqLungeMsg);
            cm.RegisterNamedMessageHandler("ReqShove", OnReqShoveMsg);
            cm.RegisterNamedMessageHandler("ReqGrab", OnReqGrabMsg);
            cm.RegisterNamedMessageHandler("ReqThrow", OnReqThrowMsg);
            cm.RegisterNamedMessageHandler("ReqReset", OnReqResetMsg);
            cm.RegisterNamedMessageHandler("ReqEmote", OnReqEmoteMsg);
            cm.RegisterNamedMessageHandler("PlayEmote", OnPlayEmoteMsg);
            cm.RegisterNamedMessageHandler("StartMatch", OnStartMatchMsg);
            cm.RegisterNamedMessageHandler("ReqSnapshot", OnReqSnapshotMsg);
            cm.RegisterNamedMessageHandler("SkipBuffer", OnSkipBufferMsg);
            cm.RegisterNamedMessageHandler("SyncAbility", OnSyncAbilityMsg);
            cm.RegisterNamedMessageHandler("RebindSeat", OnRebindSeatMsg);
            cm.RegisterNamedMessageHandler("ReqCue", OnReqCueMsg);
            cm.RegisterNamedMessageHandler("PlayCue", OnPlayCueMsg);
            cm.RegisterNamedMessageHandler("Flair", OnFlairMsg);
            cm.RegisterNamedMessageHandler("ReqAbility", OnReqAbilityMsg);
            cm.RegisterNamedMessageHandler("PlayAbility", OnPlayAbilityMsg);
            cm.RegisterNamedMessageHandler("CastDenied", OnCastDeniedMsg);
            cm.RegisterNamedMessageHandler("ReqMash", OnReqMashMsg);
            cm.RegisterNamedMessageHandler("ThrowCharge", OnThrowChargeMsg);
            cm.RegisterNamedMessageHandler("ReqThrowCharge", OnReqThrowChargeMsg);
            cm.RegisterNamedMessageHandler("PlayAction", OnPlayActionMsg);
            cm.RegisterNamedMessageHandler("PlayStyle", OnPlayStyleMsg);
            cm.RegisterNamedMessageHandler("Score", OnScoreMsg);
            cm.RegisterNamedMessageHandler("Tsinelas", OnTsinelasMsg);
            cm.RegisterNamedMessageHandler("SelectMapVote", OnSelectMapVoteMsg);
            cm.RegisterNamedMessageHandler("MapVoteTally", OnMapVoteTallyMsg);
            cm.RegisterNamedMessageHandler("MatchRecord", OnMatchRecordMsg);
            cm.RegisterNamedMessageHandler("Chat", OnChatMsg);
            cm.RegisterNamedMessageHandler("ChatLine", OnChatLineMsg);
            cm.RegisterNamedMessageHandler("ReqTime", OnReqTimeMsg);
            cm.RegisterNamedMessageHandler("SyncTime", OnSyncTimeMsg);

            _handlersOn = cm;
        }

        private bool TrySenderSeat(ulong senderClientId, out int seat)
        {
            seat = -1;
            if (!NetAuthority.IsHost) return false;

            var peer = NetSession.Instance?.Lobby?.PeerById((int)senderClientId);
            if (peer == null || peer.Spectator || peer.Seat < 0) return false;

            seat = peer.Seat;
            return true;
        }

        private bool SenderOwnsClaimedSeat(ulong senderClientId, int claimedSlot,
                                           out CharacterMotor unit)
        {
            unit = null;
            if (!TrySenderSeat(senderClientId, out int seat) || seat != claimedSlot) return false;
            unit = Unit(seat);
            return unit != null;
        }

        private bool SenderMayConfigureLobby(ulong senderClientId)
        {
            if (!NetAuthority.IsHost) return false;
            var lobby = NetSession.Instance?.Lobby;
            return lobby != null && lobby.IsLeader((int)senderClientId);
        }

        /// <summary>
        /// True when this message is the HOST speaking to this client.
        ///
        /// ⚠️⚠️ EVERY "PLAY THIS" HANDLER NEEDS THIS AND MOST OF THEM DID NOT HAVE IT. Netcode
        /// refuses client-to-client named messages at the sender, so this is not the last line of
        /// defence, but a handler that never looks at who sent it is one transport change away
        /// from letting a peer play an emote, an ability or a sound on somebody else's screen.
        /// The rule is cheap and it is the same rule the request handlers already apply from the
        /// other direction with `NetAuthority.IsHost`.
        ///
        /// ⚠️ IT IS NOT A HOST-LOOPBACK GUARD. A listen host's own local client id IS
        /// `ServerClientId`, so this passes on the host; the loopback guards say `IsHost` and are
        /// a separate question.
        /// </summary>
        private static bool FromHost(ulong senderClientId)
            => senderClientId == NetworkManager.ServerClientId;

        private static bool ValidSlot(int slot) => slot >= 0 && slot < Balance.PlayerCount;

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static bool Finite(Vector3 value) =>
            Finite(value.x) && Finite(value.y) && Finite(value.z);

        private static bool PlausibleIntentPose(CharacterMotor unit, Vector3 position)
        {
            if (unit == null || !Finite(position)) return false;
            Vector3 delta = position - unit.transform.position;
            return delta.sqrMagnitude <= IntentPoseLeeway * IntentPoseLeeway;
        }

        private bool AcceptMove(int slot, CharacterMotor unit, Vector3 position,
                                float yaw, Vector3 velocity)
        {
            if (unit == null || !Finite(position) || !Finite(yaw) || !Finite(velocity)) return false;
            if (Mathf.Abs(position.x) > AIController.PlayableHalfX + 1.0f ||
                Mathf.Abs(position.z) > AIController.PlayableHalfZ + 1.0f ||
                position.y < -5.0f || position.y > 20.0f)
                return false;

            double now = Time.realtimeSinceStartupAsDouble;
            double dt = _lastAcceptedMoveAt.TryGetValue(slot, out double previous)
                ? Math.Max(0.0, Math.Min(2.0, now - previous))
                : Time.fixedDeltaTime;
            float allowance = MoveBaseLeeway + MoveMaxMetresPerSecond * (float)dt;
            if ((position - unit.transform.position).sqrMagnitude > allowance * allowance)
                return false;

            if (velocity.magnitude > MoveMaxMetresPerSecond + 8.0f) return false;

            _lastAcceptedMoveAt[slot] = now;
            return true;
        }

        private static CharacterMotor Unit(int slot)
        {
            var round = GameServices.Round;
            return round != null ? round.PlayerAt(slot) : null;
        }

        /// <summary>
        /// The slipper that answers to a seat.
        ///
        /// ⚠️⚠️ THE FOUR ARE REMEMBERED, BECAUSE `FixedUpdate` ASKS FOR ALL OF THEM ON EVERY
        /// PHYSICS STEP. This was a whole-scene `FindObjectsByType<Slipper>` per call, so a host
        /// paid four scene-wide type scans and four fresh arrays fifty times a second, for four
        /// objects that are created once per match and then live for the whole of it. It is the
        /// same shape of cost `CLAUDE.md` section 7.1 records a HUD string rebuild being caught
        /// for, on the one code path that only ever runs while somebody is actually connected.
        ///
        /// ⚠️ THE CACHE IS VALIDATED PER CALL, NOT REFRESHED ON A TIMER. A rate limit would let
        /// the host broadcast a stale slipper for up to its interval, and `BroadcastSlipperState`
        /// is what every other peer draws that object from. The three things that can invalidate
        /// an entry are checked on the frame they happen: the object being destroyed (Unity's own
        /// null answers for that), the object being switched off, which is what
        /// `FindObjectsInactive.Exclude` used to filter, and its owner changing.
        ///
        /// ⚠️ A MISS REFILLS THE WHOLE TABLE, so a fresh arena costs ONE scan rather than four.
        /// The sweep keeps FIRST match per seat, which is what the loop it replaced returned.
        /// </summary>
        /// ⚠️⚠️ KEYED ON `SeatOfOrigin`, NOT ON `OwnerSlot`, AND THE DIFFERENCE IS A REAL BUG THAT
        /// A TWO-PROCESS LAN RUN FOUND (`docs/TODO.md` § 78.1). `OwnerSlot` is rewritten every
        /// round — `SliceRunner.EquipOwnedSlippers` disowns the taya's shoe to -1 — and the loop
        /// below skips anything negative, so **the defender's tsinelas became unaddressable on
        /// both peers at once**: the host's tick did `BroadcastSlipperStateIfChanged(null)` and
        /// stopped sending it, and a client could not have applied it either. Every non-host peer
        /// therefore drew the taya carrying a slipper for the whole round.
        /// `Slipper.SeatOfOrigin` is assigned once per match on every peer and never moves.
        private static readonly Slipper[] _slippersBySeat = new Slipper[Balance.PlayerCount];

        /// ⚠️⚠️ INACTIVE OBJECTS ARE INCLUDED, AND THAT IS THE SECOND HALF OF § 78.1. Keying on
        /// `SeatOfOrigin` alone did NOT fix the taya's tsinelas, and the verification run is what
        /// said so. `SliceRunner.EquipOwnedSlippers` does not merely disown the defender's shoe,
        /// it **switches the object off** — `slipper.gameObject.SetActive(false)`, host-side, to
        /// take it out of `Carrier.TryPickup` and out of the render. `FindObjectsInactive.Exclude`
        /// then hid it from this sweep too, so the host could not find the object it had just
        /// parked and therefore never broadcast a word about it. **An object being switched off
        /// is a fact the other peers need, so it must stay findable in order to be sent.**
        ///
        /// ⚠️ THE CACHED ENTRY NO LONGER TESTS `activeInHierarchy` EITHER, for the same reason:
        /// the round a seat becomes taya, that test would evict a perfectly good entry and the
        /// refill below would decline to put it back.
        private static Slipper FindSlipper(int seatOfOrigin)
        {
            if (seatOfOrigin >= 0 && seatOfOrigin < _slippersBySeat.Length)
            {
                var cached = _slippersBySeat[seatOfOrigin];
                if (cached != null && cached.SeatOfOrigin == seatOfOrigin)
                    return cached;
            }

            System.Array.Clear(_slippersBySeat, 0, _slippersBySeat.Length);

            foreach (var s in FindObjectsByType<Slipper>(FindObjectsInactive.Include))
            {
                int seat = s.SeatOfOrigin;
                if (seat < 0 || seat >= _slippersBySeat.Length) continue;
                if (_slippersBySeat[seat] == null) _slippersBySeat[seat] = s;
            }

            return seatOfOrigin >= 0 && seatOfOrigin < _slippersBySeat.Length
                ? _slippersBySeat[seatOfOrigin]
                : null;
        }

        /// <summary>
        /// The live world stream. Unit transforms are emitted by each motor on the physics
        /// step; the host owns the two props and emits them here on the same cadence. Match,
        /// score, tournament-clock, and ability-meter state is slower and travels at 5 Hz.
        /// A reconnect still requests an immediate full snapshot rather than waiting for either.
        /// </summary>
        private void FixedUpdate()
        {
            if (!NetAuthority.IsNetworked || !NetAuthority.IsHost ||
                _nm == null || _nm.CustomMessagingManager == null)
                return;

            HostStepResetChannels();

            BroadcastLataStateIfChanged();
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
                BroadcastSlipperStateIfChanged(FindSlipper(slot));

            _matchSyncLeft -= Time.fixedDeltaTime;
            if (_matchSyncLeft > 0.0f) return;
            _matchSyncLeft = MatchSyncInterval;

            BroadcastMatchState();
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var unit = Unit(slot);
                if (unit != null) BroadcastAbilityState(slot, unit);
            }
        }

        // -------------------------------------------------------------------
        // IDENTITY AND SEATING
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ THE ACCOUNT ID AND THE HANDLE PROOF TRAVEL WITH THE NAME, AND THEY ARE WHY THE
        /// PROTOCOL IS 16. `docs/TODO.md` § 88.1c. This message is read field by field in order,
        /// so the two new values are a wire change even though nothing else moved: a peer writing
        /// five where the host reads seven misreads every field after the third. Neither value is
        /// trusted here; together they let the host ask the account endpoint one question.
        /// </summary>
        public void IdentifyServerRpc(string token, string name, string accountPlayerId,
                                      string handleProof, int charPick, int canPick, int slipperPick)
            => IdentifyServerRpc(token, name, accountPlayerId, handleProof, charPick, canPick,
                                 slipperPick, LocalCosmetics.Encoded(charPick),
                                 LocalCosmetics.CustomCharacter(),
                                 LocalCosmetics.HeroBuild(charPick));

        /// <summary>
        /// ⚠️⚠️ THE COSMETICS CLAIM IS ONE FIELD AND IT IS WHY THE PROTOCOL IS 17. It carries the
        /// banner, the palette and the two facts that authorise them, encoded by `BannerCodec`.
        /// **One field rather than eighteen**, for the reason the paragraph above gives about
        /// reading this message in order: a banner is four ids, three trackers, a palette, an XP
        /// figure and up to six mastery pairs, and every one of those would be another chance to
        /// write the halves out of step. `audit_wire_payloads.py` compares a writer to its reader
        /// field by field, so one field is one thing for it to check.
        ///
        /// ⚠️ NOTHING HERE IS TRUSTED. `HandleIdentify` runs `BannerRules.Authorise` and stores
        /// the ANSWER; the claim itself is never kept and never rebroadcast.
        /// </summary>
        /// <param name="custom">
        /// ⚠️⚠️ THE CUSTOM CHARACTER, AND IT IS WHY THE PROTOCOL IS 19. Same argument as the
        /// cosmetics claim above: it is ONE versioned string (`CustomCharacterRules.EncodeWire`,
        /// a `C3` frame) carrying twenty fields, rather than twenty fields a writer and a reader
        /// have to be kept in step by hand. Empty means "playing as a roster character", which is
        /// also what every build before this one sends, so a mixed-build room degrades to the
        /// roster rather than to a broken hero.
        /// </param>
        public void IdentifyServerRpc(string token, string name, string accountPlayerId,
                                      string handleProof, int charPick, int canPick, int slipperPick,
                                      string cosmetics, string custom = "", string build = "")
        {
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            if (NetAuthority.IsHost)
            {
                HandleIdentify(0, token, name, accountPlayerId, handleProof, charPick, canPick,
                               slipperPick, cosmetics, custom, build);
                return;
            }

            using var writer = new FastBufferWriter(1024, Allocator.Temp);
            writer.WriteValueSafe(token ?? "");
            writer.WriteValueSafe(name ?? "");
            writer.WriteValueSafe(accountPlayerId ?? "");
            writer.WriteValueSafe(handleProof ?? "");
            writer.WriteValueSafe(charPick);
            writer.WriteValueSafe(canPick);
            writer.WriteValueSafe(slipperPick);
            writer.WriteValueSafe(cosmetics ?? "");
            writer.WriteValueSafe(custom ?? "");
            writer.WriteValueSafe(build ?? "");
            _nm.CustomMessagingManager.SendNamedMessage("Identify", NetworkManager.ServerClientId, writer);
        }

        private void OnIdentifyMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;

            reader.ReadValueSafe(out string token);
            reader.ReadValueSafe(out string name);
            reader.ReadValueSafe(out string accountPlayerId);
            reader.ReadValueSafe(out string handleProof);
            reader.ReadValueSafe(out int charPick);
            reader.ReadValueSafe(out int canPick);
            reader.ReadValueSafe(out int slipperPick);
            reader.ReadValueSafe(out string cosmetics);

            // ⚠️ READ ONLY IF IT IS THERE, WHICH IS THE SAME GUARD `OnSyncLobbyPicksMsg` PUTS ON
            // THE SPECTATOR COUNT AND FOR THE SAME REASON: `FastBufferReader` THROWS past the end
            // of a payload, and a message handler that throws drops everything queued behind it.
            // `NetSession.ProtocolVersion` 19 refuses a mixed room at approval, so this can only
            // fire in a build where the two halves of this method have drifted, which is exactly
            // when a dead handler is hardest to diagnose.
            string custom = "";
            if (reader.Length > reader.Position) reader.ReadValueSafe(out custom);
            string build = "";
            if (reader.Length > reader.Position) reader.ReadValueSafe(out build);

            HandleIdentify(senderClientId, token, name, accountPlayerId, handleProof, charPick,
                           canPick, slipperPick, cosmetics, custom, build);
        }

        private void HandleIdentify(ulong senderClientId, string token, string name,
                                    string accountPlayerId, string handleProof,
                                    int charPick, int canPick, int slipperPick,
                                    string cosmetics, string custom, string build)
        {
            int peerId = (int)senderClientId;
            var lobby = NetSession.Instance?.Lobby;
            if (lobby == null) return;

            var record = lobby.Admit(peerId, token, name);

            // ⚠️ THE SECOND ARRIVAL PATH, AND IT NEEDS THE GUARD AS MUCH AS THE FIRST. A peer
            // reaches `Admit` through the approval hello and again through this message, and a
            // check wired into only one of them is a check with a documented way around it.
            NetSession.Instance?.VerifyArrival(peerId, accountPlayerId, handleProof);

            int resolvedCharPick = charPick >= 0 ? charPick : 0;
            int resolvedCanPick = canPick >= 0 ? canPick : 0;
            int resolvedSlipperPick = slipperPick >= 0 ? slipperPick : 0;
            lobby.SetPicks(peerId, resolvedCharPick, resolvedCanPick, resolvedSlipperPick);
            HostAuthoriseCosmetics(peerId, cosmetics, resolvedCharPick, custom, build);

            // ⚠️ THE MODE IS THE FIRST THING A JOINER IS TOLD, for the reason `HostStartMatch`
            // gives: everything below it is interpreted through the mode, and a late joiner may
            // be about to build an arena from it.
            SyncModeClientRpc((int)UI.SceneFlow.SelectedMode);

            // ⚠⚠ AND THE MAP AND THE DIFFICULTY GO WITH IT, WHICH THEY NEVER DID. `SelectMap`
            // and `SelectDiff` only ever travelled when the host CYCLED them, so a peer joining a
            // lobby the host had already set up was told the mode and nothing else. Its lobby drew
            // whatever map its own menu last held, and `SceneFlow.SelectedMap` is exactly what
            // `SceneFlow.StartMatch` loads: a joiner who never saw the host touch the arrows
            // loaded a DIFFERENT ARENA on start, which from the other side of the room reads as
            // "it only started for the host".
            SyncMapClientRpc(Mathf.Max(0, System.Array.IndexOf(UI.SceneFlow.Maps, UI.SceneFlow.SelectedMap)));
            SyncDifficultyClientRpc(Settings.SettingsStore.Current.AiDifficulty);

            // ⚠️⚠️ THE SEAT GOES **AFTER** THE MODE AND THE MAP, AND IT USED TO GO FIRST. This is
            // the same ordering rule `HostStartMatch` states three paragraphs of reasoning for,
            // and the mid-match path was the one place that broke it. `OnSeatingMsg` is not just a
            // seat: when it carries `inProgress` it calls `UI.SceneFlow.StartMatch()`, which loads
            // `SceneFlow.SelectedMap`. Sent first, it fired on a REJOINING player whose
            // `SelectedMap` and `SelectedMode` were still whatever their own menu last held, so a
            // player rejoining a Hero Strike match on Ilalim ng Tulay loaded Classic on Eskinita,
            // alone, and the map that arrived one line later had nothing left to correct.
            //
            // ⚠️ THE SEND IS ORDERED, SO THE ORDER HERE IS THE ORDER THERE. Named messages go out
            // on a reliable sequenced channel, which is exactly what makes writing them in the
            // wrong order a real bug rather than a race that usually works.
            if (senderClientId != _nm.LocalClientId) SendSeating((int)senderClientId);

            NetSession.Instance?.SetStatus($"{lobby.PeerCount} connected, seat {record.Seat}");

            BroadcastReadyTally();

            HostLateJoin(peerId);
            BroadcastLobbyPicks();
            BroadcastPicks();
            BroadcastWorldSnapshot();
        }

        /// <summary>
        /// Decide what one peer is allowed to wear, and write only the answer onto its record.
        ///
        /// ⚠️⚠️ EVERY DECISION IS HOST-SIDE, WHICH IS `LobbySession`'S OWN RULE APPLIED TO
        /// COSMETICS: *"a client asks; this answers. Nothing here may be driven from a client
        /// message without the host re-checking it."* A peer sends what it wants to wear and the
        /// XP and mastery that would authorise it; `BannerRules.Authorise` runs here, once, and
        /// the room is told the RESULT. **Four peers each normalising their own copy would be
        /// four answers to one question**, which is the shape `docs/TODO.md` § 94.1 records four
        /// hand-written copies of.
        ///
        /// ⚠️ THE CLAIM IS NOT STORED. `PeerRecord.Banner` holds the authorised selection and
        /// nothing on the record can be read back as an unchecked id, because there is no
        /// unchecked id on it.
        ///
        /// ⚠️⚠️ AND IT RUNS ON EVERY PICK CHANGE, NOT ONLY ON ARRIVAL. The palette is a fact
        /// about the player AND the character (`FUTURE.md` PHASE 5's favourite loadout per
        /// character), so a claim authorised once at join would dress a peer who switched
        /// character in the palette of the one they walked in with.
        /// </summary>
        /// <param name="custom">
        /// ⚠️⚠️ THE PEER'S CUSTOM CHARACTER, AND THE HOST RE-ENCODES IT RATHER THAN STORING WHAT
        /// ARRIVED. `CustomCharacterRules.Normalise` clamps every index into its own list and
        /// resolves `HeroKitId` through `KitFor`, so a modified client cannot claim a hat that
        /// does not exist or a kit built out of three heroes: what the room receives is what this
        /// machine wrote. Same arrangement as the banner one line up, and `docs/TODO.md` § 110.5
        /// is why the kit half of it matters more than the hat half.
        /// </param>
        public void HostAuthoriseCosmetics(int peerId, string cosmetics, int charPick,
                                           string custom = "", string build = "")
        {
            if (!NetAuthority.IsHost) return;

            var record = NetSession.Instance?.Lobby?.PeerById(peerId);
            if (record == null) return;

            // ⚠️⚠️ AN UNRECOGNISED FRAME IS REFUSED, NOT DECODED, AND THE DIFFERENCE MATTERS.
            // `CustomCharacterRules.DecodeWire` answers a DEFAULT character for a version it does
            // not know, which is the right answer when you are reading your own save file and the
            // wrong one here: it would put a stranger in the seat of a peer who is playing as a
            // roster hero. An empty frame means "roster character" and so does a frame this build
            // cannot read, so both land on empty.
            record.Custom = !string.IsNullOrEmpty(custom) && custom.StartsWith("C3:")
                ? CustomCharacterRules.EncodeWire(CustomCharacterRules.DecodeWire(custom))
                : "";

            // ⚠️ AN EMPTY FRAME IS A PEER ON AN OLDER BUILD OR A PLAYER WEARING NOTHING, AND
            // BOTH WANT THE SAME ANSWER. `BannerCodec.DecodeClaim` never throws and answers an
            // empty claim, which authorises to an empty banner: no decoration, drawn deliberately.
            var claim = BannerCodec.DecodeClaim(cosmetics);

            record.Banner = BannerRules.Authorise(claim);

            string characterId = Core.Roster.PersonIdAt(UI.SceneFlow.SelectedMode, charPick);
            if (!string.IsNullOrEmpty(record.Custom))
                characterId = CustomCharacterRules.KitFor(
                    CustomCharacterRules.DecodeWire(record.Custom).HeroKitId);

            record.Build = UI.SceneFlow.SelectedMode == GameMode.HeroStrike
                ? HeroBuildRules.Encode(HeroBuildRules.Decode(build, characterId), characterId)
                : "";

            // ⚠️ THE WHOLE LOOK, NOT ONLY THE EARNED PALETTE. `BannerRules.AuthoriseLook`
            // checks the reward half and clamps the free half, so what lands in the seat table is
            // a decision about both. See `LobbySeatInfo.Look`.
            record.Look = LookCodec.Encode(BannerRules.AuthoriseLook(claim, characterId));
        }

        /// <summary>
        /// The host telling ONE peer which chair it holds, plus the lobby facts that travel with
        /// it. The host applies its own locally rather than posting itself a packet.
        /// </summary>
        private void SendSeating(int peerId)
        {
            if (!NetAuthority.IsHost) return;
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            var lobby = NetSession.Instance?.Lobby;
            var record = lobby?.PeerById(peerId);
            if (record == null) return;

            if ((ulong)peerId == _nm.LocalClientId)
            {
                NetSession.Instance?.SetLocalSeating(record.Seat, record.Spectator);
                return;
            }

            using var writer = new FastBufferWriter(128, Allocator.Temp);
            writer.WriteValueSafe(record.Seat);
            writer.WriteValueSafe(record.Spectator);
            writer.WriteValueSafe(lobby.LeaderPeerId);
            writer.WriteValueSafe(lobby.MatchInProgress);
            writer.WriteValueSafe(lobby.JoinCode ?? "");
            _nm.CustomMessagingManager.SendNamedMessage("Seating", (ulong)peerId, writer);
        }

        // -------------------------------------------------------------------
        // SECTION: CHOOSING A CHAIR
        //
        // ⚠⚠ THE LOBBY'S FOUR SEAT BUTTONS WERE NOT CONNECTED TO THE NETWORK AT ALL. They
        // wrote `GameLaunch.SoloSeat`, which only the OFFLINE practice match reads, while the
        // networked rows are drawn from `NetSession.LocalSlot`; and `RefreshSeats` then made every
        // one of them non-interactable unless `NetAuthority.IsHost`. So a client could not press
        // them at all, and the host pressing them moved a number nothing in a networked match ever
        // looks at. 🧑, 2026-08-27: "a player cannot switch from p1 to p4".
        //
        // ⚠️ IT IS THE SAME IDIOM AS THE MAP AND THE MODE, deliberately: the client ASKS,
        // `LobbySession.TryTakeSeat` decides, and the host tells the mover its new seat and tells
        // everybody the new roster. A seat handed out by the peer that wants it is a peer that can
        // sit down on top of somebody else.
        // -------------------------------------------------------------------

        /// <summary>Ask the host for <paramref name="seat"/>, or for -1 to spectate.</summary>
        public void RequestSeatServerRpc(int seat)
        {
            if (NetAuthority.IsHost)
            {
                HostAssignSeat(_nm != null ? (int)_nm.LocalClientId : 0, seat);
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(seat);
            _nm.CustomMessagingManager.SendNamedMessage("ReqSeat", NetworkManager.ServerClientId, writer);
        }

        private void OnReqSeatMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;

            reader.ReadValueSafe(out int seat);

            // ⚠️ THE PERSON COMES FROM THE SENDER'S TRANSPORT ID, NEVER FROM THE PAYLOAD. The
            // message names a chair, not a player; a peer that could name the player could move
            // somebody else out of theirs.
            HostAssignSeat((int)senderClientId, seat);
        }

        private void HostAssignSeat(int peerId, int seat)
        {
            if (!NetAuthority.IsHost) return;

            var lobby = NetSession.Instance?.Lobby;
            if (lobby == null || !lobby.TryTakeSeat(peerId, seat)) return;

            // ⚠️ MOVING SEATS CLEARS YOUR READY. The arrangement you agreed to is not the one
            // on screen any more, and a tick left standing would count towards a gate that has
            // changed underneath it.
            _lobbyReady.Remove(peerId);

            SendSeating(peerId);
            BroadcastLobbyPicks();
            BroadcastReadyTally();
        }

        private void OnSeatingMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int seat);
            reader.ReadValueSafe(out bool spectator);
            reader.ReadValueSafe(out int leaderId);
            reader.ReadValueSafe(out bool inProgress);
            string joinCode = "";
            if (reader.Length > reader.Position)
            {
                reader.ReadValueSafe(out joinCode);
            }

            var net = NetSession.Instance;
            if (net != null)
            {
                if (!string.IsNullOrEmpty(joinCode))
                {
                    net.Lobby.SetJoinCode(joinCode);
                }

                // ⚠️ THE CLIENT'S COPY OF "IS A MATCH RUNNING" IS WRITTEN HERE AND WAS NOT
                // WRITTEN ANYWHERE. The flag arrived on this message and was read for the scene
                // load two lines below, then dropped, so a client's `LobbySession` said false for
                // the whole of a running match. The lobby screen reads it to grey the seat rows
                // out, which is the difference between a button that explains itself and one that
                // silently does nothing when the host's `TryTakeSeat` refuses it.
                net.Lobby.MatchInProgress = inProgress;

                // ⚠️ AND SO WAS THE LEADER, ON THE LINE ABOVE IT. `leaderId` was read off the
                // wire and dropped; see `LobbySession.ApplyLeaderFromHost`. The lobby's guest
                // button names the host with it.
                net.Lobby.ApplyLeaderFromHost(leaderId);

                net.SetLocalSeating(seat, spectator);
            }

            if (inProgress && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != UI.SceneFlow.SelectedMap)
            {
                UI.SceneFlow.StartMatch();
            }
            else
            {
                var installer = FindFirstObjectByType<MatchInstaller>();
                installer?.RebindLocalSeat(seat, spectator);
            }
        }

        // -------------------------------------------------------------------
        // THE READY GATE
        // -------------------------------------------------------------------

        /// <summary>
        /// "I am ready", or "I am not any more", from whichever peer this is.
        ///
        /// ⚠️⚠️ IT CARRIES NO PEER ID, AND THAT IS THE FIX RATHER THAN A SAVING. It used to
        /// write the id the caller had to hand, and every caller reached for
        /// `NetAuthority.LocalSlot`, which is a SEAT. The host then keyed its ready set by a
        /// seat from one peer and a transport id from another, so a host in seat 1 and a client
        /// with id 1 shared one entry and the gate stayed a vote short for the whole lobby. The
        /// sender is now whatever NGO authenticated at the door, which is also the only value a
        /// client cannot lie about: a peer that could name itself could ready somebody else.
        /// `ProtocolVersion` went to 3 for it.
        ///
        /// ⚠️ THE FIELD WAS DELETED RATHER THAN READ AND DISCARDED. Keeping it balanced the two
        /// halves for `tools/audit_wire_payloads.py`, but it left a value on the wire that the
        /// host must remember to ignore, and remembering is exactly what failed the first time.
        /// A field that cannot be trusted should not be sent.
        ///
        /// ⚠️ THE TOGGLE IS FOR THE LOBBY. The in-match <see cref="ReadyGate"/> only ever says
        /// true, because a pre-round press there starts a countdown that cannot be recalled; the
        /// LOBBY button is a toggle and needs both.
        ///
        /// ⚠️ THE HOST'S OWN ID COMES FROM `NetAuthority.LocalPeerId`, never from `_nm`
        /// directly: `IsHost` is true offline too, where there is no `NetworkManager` to ask.
        /// </summary>
        /// <returns>
        /// False when the press could not be delivered, so the caller can hold it and try again.
        ///
        /// ⚠️⚠️ `IsListening` IS NOT `IsConnectedClient`, AND THE GAP BETWEEN THEM EATS A READY
        /// PRESS. `NetAuthority.IsNetworked` reads `IsListening`, which goes true the instant
        /// `StartClient` is called, well before connection approval finishes. Everything that
        /// asks "am I networked" therefore answers yes during the join, and a `SendNamedMessage`
        /// on that transport goes nowhere and reports nothing. A player who pressed R inside that
        /// window had their vote vanish, watched the prompt clear, and had no way to tell that
        /// nothing had been sent: `HostDeclareReady` is idempotent, so a resend is free, but
        /// nothing was resending.
        /// </returns>
        public bool DeclareReadyServerRpc(bool ready = true)
        {
            if (NetAuthority.IsHost)
            {
                HostDeclareReady(NetAuthority.LocalPeerId, ready);
                return true;
            }

            if (_nm == null || _nm.CustomMessagingManager == null || !_nm.IsConnectedClient)
                return false;

            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(ready);
            _nm.CustomMessagingManager.SendNamedMessage("DeclareReady", NetworkManager.ServerClientId, writer);
            return true;
        }

        private void OnDeclareReadyMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;

            // ⚠️ THE SENDER IS NGO'S, NOT THE PAYLOAD'S. The peer id used to travel here and be
            // thrown away; it is not written any more, so there is nothing to remember to
            // ignore. See `DeclareReadyServerRpc`.
            reader.ReadValueSafe(out bool ready);

            HostDeclareReady((int)senderClientId, ready);
        }

        // -------------------------------------------------------------------
        // SECTION: THE LOBBY READY GATE
        //
        // ⚠⚠ READY IN THE LOBBY WENT NOWHERE AND STARTING WAS THE HOST'S BUTTON ALONE.
        // `DeclareReady` has always been routed to `FindFirstObjectByType<ReadyGate>()`, and
        // `ReadyGate` is a component of the ARENA: in the `MatchSetup` scene there is no such
        // object, so every READY press in the lobby, the host's included, resolved to a null and
        // did nothing at all. The tally on screen was a local bool. 🧑, 2026-08-27: "when
        // all player ready up and the game starts, it only starts for the host."
        //
        // ⚠️ IT COUNTS SEATED GUESTS, NOT CHARACTERS, for the reason `ReadyGate` gives at
        // length: the empty chairs are played by bots and a bot cannot press a key. Spectators are
        // excluded on the same rule, and so is the host because the host sees START rather than
        // READY. A host plus three ready guests is therefore 3/3, not an impossible 3/4.
        //
        // ⚠️⚠️ READY DOES NOT START THE MATCH. THE HOST'S BUTTON DOES, AND ONLY IT.
        // 🧑 2026-08-27: *"i also dont like that if u click ready it auto starts, i want to have
        // to click start match to start it as host"*. The gate reached quorum and called
        // `HostStartMatch` itself, so the last person to tick a box decided when four people
        // were dropped into an arena, and the host's own START button became decoration it could
        // never get to press. Readiness is now what it says on the button: an ANSWER, drawn on
        // every screen by `ReadyTally`, that the host reads before choosing its moment.
        //
        // ⚠️ AND THE HOST IS NOT BLOCKED ON IT EITHER. START stays live whatever the tally says,
        // because a lobby of one host and three bots is a legitimate match and waiting for a
        // quorum of one would be a gate with nothing on the other side of it.
        //
        // ⚠️ THERE IS STILL EXACTLY ONE PATH INTO AN ARENA, `HostStartMatch`, so the broadcast
        // that carries every other peer in with it cannot be forgotten on one of them.
        // -------------------------------------------------------------------

        /// <summary>Raised with (ready, expected) on every peer whenever the lobby tally moves.</summary>
        public static event Action<int, int> OnLobbyReadyChanged;

        private readonly HashSet<int> _lobbyReady = new HashSet<int>();

        private void HostDeclareReady(int peerId, bool ready)
        {
            if (!NetAuthority.IsHost) return;

            // In a match the pre-round gate owns this press, and it runs its own countdown.
            var gate = FindFirstObjectByType<ReadyGate>();
            if (gate != null)
            {
                if (ready) gate.DeclareReady(peerId);
                return;
            }

            var lobby = NetSession.Instance?.Lobby;
            if (lobby == null) return;

            var peer = lobby.PeerById(peerId);

            // ⚠️⚠️ A REFUSED READY USED TO BE SILENT, AND SILENCE IS INDISTINGUISHABLE FROM A
            // MESSAGE THAT NEVER ARRIVED. 🧑 2026-08-28: LAN plays, but over Relay the other
            // devices sit on *"Ready! Waiting for other players..."* forever. From the client
            // that is one symptom; from the host it is three different faults (the press never
            // reached us, the peer is not in the lobby table, or it is in it without a chair)
            // and nothing said which. The host's log now names it.
            if (peer == null || peer.Spectator || peer.Seat < 0)
            {
                Debug.LogWarning($"[NetReady] refused peer {peerId}: " +
                                 (peer == null ? "not in the lobby table"
                                  : peer.Spectator ? "spectator"
                                  : $"no seat (Seat={peer.Seat})"));
                return;
            }

            bool moved = ready ? _lobbyReady.Add(peerId) : _lobbyReady.Remove(peerId);
            if (!moved) return;

            Debug.Log($"[NetReady] peer {peerId} seat {peer.Seat} ready={ready} " +
                      $"-> {LobbyReadyCount()} of {LobbyExpectedReady()}");

            BroadcastReadyTally();

            // ⚠️⚠️ THE ROSTER GOES OUT TOO, BECAUSE THE TICK LIVES ON IT. `ReadyTally` carries the
            // COUNT and `SyncLobbyPicks` carries the per-seat `Ready` the nameplates draw; without
            // this line the number under the button moved and not one tick over anybody's head
            // did, which is the same "works on the host's screen or nobody's" shape
            // `docs/TODO.md` § 55 is a whole section about.
            BroadcastLobbyPicks();
        }

        /// <summary>
        /// ⚠️ COUNTED AGAINST THE LIVE LOBBY RATHER THAN TRUSTED. A peer that readied and then
        /// moved to a spectator slot is still in the set until something removes it, and a tally
        /// that counts a press nobody can retract starts the match on three players' behalf.
        /// </summary>
        private int LobbyReadyCount()
        {
            var lobby = NetSession.Instance?.Lobby;
            if (lobby == null) return 0;

            int count = 0;
            foreach (int peerId in _lobbyReady)
            {
                if (lobby.IsReadyVoter(peerId, NetAuthority.LocalPeerId)) count++;
            }

            return count;
        }

        private int LobbyExpectedReady()
        {
            var lobby = NetSession.Instance?.Lobby;
            return lobby == null ? 0 : lobby.ReadyVoterCount(NetAuthority.LocalPeerId);
        }

        public void BroadcastReadyTally()
        {
            if (!NetAuthority.IsHost) return;

            int ready = LobbyReadyCount();
            int expected = LobbyExpectedReady();

            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(16, Allocator.Temp);
                writer.WriteValueSafe(ready);
                writer.WriteValueSafe(expected);
                _nm.CustomMessagingManager.SendNamedMessageToAll("ReadyTally", writer);
            }

            OnLobbyReadyChanged?.Invoke(ready, expected);
        }

        private void OnReadyTallyMsg(ulong senderClientId, FastBufferReader reader)
        {
            // ⚠️ THE HOST IS ITS OWN CLIENT AND `SendNamedMessageToAll` LOOPS BACK TO IT. See
            // the section on the loopback; the host raised the event itself one line earlier.
            if (NetAuthority.IsHost) return;
            if (!FromHost(senderClientId)) return;

            reader.ReadValueSafe(out int ready);
            reader.ReadValueSafe(out int expected);
            OnLobbyReadyChanged?.Invoke(ready, expected);
        }

        // -------------------------------------------------------------------
        // SECTION: CHAT
        //
        // 🧑 2026-08-28: *"yea maybe add a chat to our game too that works in lobby and ingame"*.
        // Four people in a lobby had no way to say anything to each other, and four people in a
        // match had no way to call a play. Emotes travel (§ 38.3) and are not the same thing.
        //
        // ⚠️⚠️ THIS IS THE ONE THING IN THE WHOLE PUBG BATCH THAT MOVES `ProtocolVersion`, 5 to 6,
        // so both machines must be rebuilt from this branch or they refuse each other at approval.
        // `docs/TODO.md` § 59.4 records what a bump costs and § 68.2 records why every other part
        // of this work was deliberately built without one. Bump it ONCE, here.
        //
        // ⚠️⚠️ THE SENDER IS NGO'S AUTHENTICATED CLIENT ID AND THE NAME IS LOOKED UP HOST-SIDE.
        // The peer never writes who it is. § 54 settled this for `DeclareReady` after the opposite
        // shipped: a field the host has to remember to ignore is a field that gets trusted, and
        // there every caller reached for `NetAuthority.LocalSlot`, which is a SEAT, so the host
        // keyed its set by a seat from one peer and a transport id from another. A chat line is
        // the one message in this game where a spoofable name is not merely a bug.
        //
        // ⚠️ AND THE HOST CLAMPS BOTH LENGTH AND RATE. § 38.9 found two request channels any
        // client could flood; a text channel is the obvious third, and it is the only one whose
        // payload is variable-length. Both limits are enforced HERE, on the authority, not in the
        // UI, because the UI is the half an attacker does not run.
        // -------------------------------------------------------------------

        /// <summary>
        /// The longest line the host will relay, in characters.
        ///
        /// ⚠️ 120 IS A WIRE BOUND AND A LAYOUT BOUND AT ONCE. `LobbyChat` draws about 64
        /// characters per line at its authored size, so this is under two wrapped lines in the log
        /// and cannot push the panel past the height it was given.
        /// </summary>
        public const int MaxChatLength = 120;

        /// <summary>Seconds a peer must wait between lines. See the flood note above.</summary>
        public const float MinChatInterval = 0.6f;

        /// <summary>Raised on every peer with (who, what) when a line is relayed.</summary>
        public static event Action<string, string> OnChatLine;

        private readonly Dictionary<int, float> _lastChatAt = new Dictionary<int, float>();

        /// <summary>
        /// Says something. Returns false when there was nowhere to send it, which the caller draws
        /// rather than swallowing.
        ///
        /// ⚠️ SAME `IsConnectedClient` TEST `DeclareReadyServerRpc` USES, and for the reason its
        /// header gives at length: `IsListening` goes true at `StartClient`, well before approval,
        /// so a line typed during the join window would go to a transport with nowhere to send it
        /// and report nothing.
        /// </summary>
        public bool SendChatServerRpc(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            if (NetAuthority.IsHost)
            {
                HostRelayChat(NetAuthority.LocalPeerId, text);
                return true;
            }

            if (_nm == null || _nm.CustomMessagingManager == null || !_nm.IsConnectedClient)
                return false;

            string trimmed = ClampChatLine(text);

            // ⚠️ SIZED FROM THE STRING RATHER THAN A FIXED 16 LIKE THE FLAG MESSAGES ABOVE.
            // `FastBufferWriter` does not grow past its capacity; a 120-character line is up to
            // 480 bytes as UTF-32-safe UTF-8 plus a four-byte length prefix, and writing past the
            // end of a Temp buffer is not a clean failure.
            using var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize(trimmed) + 8,
                                                    Allocator.Temp);
            writer.WriteValueSafe(trimmed);
            _nm.CustomMessagingManager.SendNamedMessage("Chat", NetworkManager.ServerClientId, writer);
            return true;
        }

        private void OnChatMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;

            reader.ReadValueSafe(out string text);
            HostRelayChat((int)senderClientId, text);
        }

        /// <summary>
        /// ⚠️ THE NAME COMES OUT OF THE LOBBY, NOT OFF THE WIRE. A spectator has no seat and still
        /// has a name, which is why this reads `PeerRecord` rather than resolving a seat: a
        /// spectator who cannot speak is a person sitting in the room being ignored, and they are
        /// usually the one who knows why the last round went wrong.
        /// </summary>
        private void HostRelayChat(int peerId, string text)
        {
            if (!NetAuthority.IsHost) return;
            if (string.IsNullOrWhiteSpace(text)) return;

            float now = Time.unscaledTime;

            if (_lastChatAt.TryGetValue(peerId, out float last) && now - last < MinChatInterval)
                return;

            _lastChatAt[peerId] = now;

            var peer = NetSession.Instance?.Lobby?.PeerById(peerId);

            string who = peer != null && !string.IsNullOrWhiteSpace(peer.Name)
                ? peer.Name
                : (peer != null && peer.Seat >= 0 ? $"P{peer.Seat + 1}" : "SOMEBODY");

            string line = ClampChatLine(text);

            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(
                    FastBufferWriter.GetWriteSize(who) + FastBufferWriter.GetWriteSize(line) + 16,
                    Allocator.Temp);

                writer.WriteValueSafe(who);
                writer.WriteValueSafe(line);
                _nm.CustomMessagingManager.SendNamedMessageToAll("ChatLine", writer);
            }

            // ⚠️ THE HOST RAISES ITS OWN. `SendNamedMessageToAll` loops back into the host (§ 38.1),
            // and `OnChatLineMsg` refuses the host for that reason, so without this line the one
            // person who cannot leave the lobby is the one person who cannot see it.
            OnChatLine?.Invoke(who, line);
        }

        // -------------------------------------------------------------------
        // § THE BROADCAST CLOCK
        //
        // ⚠⚠ SPECTATORS MAY STOP THE MATCH, AND THIS REVERSES A WRITTEN RULE. `SpectatorCamera`'s
        // broadcast block said, in terms: *"Pause and speed manipulation are offline-only by
        // construction: a remote viewer must never acquire authority over a live tournament simply
        // by spectating"*, and refused every time control with `LIVE NETWORK · TIME CONTROLS
        // LOCKED`. 🧑 2026-08-30, asked which of the two pauses he meant and answering plainly:
        // *"pause is for spectatotr"*, *"give spectators the authority to pause, all of them can
        // pause"*, *"make sure time pauses if u pause as well as everything happening and spectator
        // can move"*, *"liek in game like mobile legends"*.
        //
        // The old rule was protecting a tournament against a stranger. This game's spectators are
        // the four people waiting for the next match and whoever is casting it, and the ask is a
        // broadcast feature: MLBB's observers stop the game to talk over a fight. **All of them
        // can pause**, which he said twice, so there is no leader check here.
        //
        // ⚠⚠ THE HOST IS STILL THE ONLY WRITER, AND THAT IS NOT A CONTRADICTION. A spectator
        // ASKS (`ReqTime`) and the host DECIDES and TELLS EVERYONE (`SyncTime`), which is
        // `CLAUDE.md` § 4's rule that state is produced in one place. Four peers each writing their
        // own `Time.timeScale` is four different matches, and the two that mattered would drift
        // apart inside a second.
        //
        // ⚠ A PLAYER CANNOT. The request is refused unless the sender's `PeerRecord.Spectator` is
        // set, so somebody losing cannot stop the round they are losing.
        // -------------------------------------------------------------------

        /// <summary>
        /// A spectator asking the host to stop or slow the match. Host-applied, then broadcast.
        /// </summary>
        public void RequestTimeScaleServerRpc(float scale)
        {
            if (NetAuthority.IsHost)
            {
                HostSetTimeScale(scale);
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;

            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(scale);
            _nm.CustomMessagingManager.SendNamedMessage("ReqTime", NetworkManager.ServerClientId, writer);
        }

        private void OnReqTimeMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;

            reader.ReadValueSafe(out float scale);

            // ⚠ THE SENDER MUST BE A WATCHER. `TrySenderSeat` answers the opposite question, so
            // this asks the lobby directly: a peer with a chair is playing and may not stop the
            // match it is playing in.
            var peer = NetSession.Instance?.Lobby?.PeerById((int)senderClientId);
            if (peer == null || !peer.Spectator) return;

            HostSetTimeScale(scale);
        }

        /// <summary>
        /// Apply a broadcast clock on the host and hand the same number to every peer.
        ///
        /// ⚠ CLAMPED HOST-SIDE, NOT TRUSTED FROM THE WIRE. 0 is the pause and 1 is normal; a
        /// hostile or corrupted 50 would run the match at fifty times speed on four machines.
        /// </summary>
        public void HostSetTimeScale(float scale)
        {
            if (!NetAuthority.IsHost) return;

            float safe = Mathf.Clamp(scale, 0.0f, 1.0f);

            // ⚠ THE HITSTOP IS ENDED FIRST, on every peer, because it is the other writer of
            // `Time.timeScale` in this project and it restores to 1 when it expires — which would
            // quietly un-pause a paused match a fraction of a second later.
            Hitstop.End();
            Time.timeScale = safe;
            TimeScaleChanged?.Invoke(safe);

            if (_nm == null || _nm.CustomMessagingManager == null) return;

            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(safe);
            _nm.CustomMessagingManager.SendNamedMessageToAll("SyncTime", writer);
        }

        private void OnSyncTimeMsg(ulong senderClientId, FastBufferReader reader)
        {
            // ⚠ THE HOST IS ITS OWN CLIENT AND `SendNamedMessageToAll` LOOPS BACK TO IT. See
            // § THE LOOPBACK: applying this again on the host would be harmless but the guard is
            // the house style and it keeps the event from firing twice.
            if (NetAuthority.IsHost) return;
            if (!FromHost(senderClientId)) return;

            reader.ReadValueSafe(out float scale);

            Hitstop.End();
            Time.timeScale = Mathf.Clamp(scale, 0.0f, 1.0f);
            TimeScaleChanged?.Invoke(Time.timeScale);
        }

        /// <summary>
        /// Raised on every peer when the broadcast clock moves, so a HUD can say why the world
        /// stopped. A player who is not told is looking at a frozen game with no explanation.
        /// </summary>
        public static event System.Action<float> TimeScaleChanged;

        private void OnChatLineMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (NetAuthority.IsHost) return;
            if (!FromHost(senderClientId)) return;

            reader.ReadValueSafe(out string who);
            reader.ReadValueSafe(out string line);

            OnChatLine?.Invoke(who, line);
        }

        /// <summary>
        /// ⚠️ NEWLINES AND CARRIAGE RETURNS ARE STRIPPED, NOT JUST THE LENGTH CAPPED. A legacy
        /// `Text` honours a `\n`, so one pasted line could otherwise be twenty rows tall and push
        /// every other message out of the log: a length cap alone bounds the CHARACTERS and not
        /// the HEIGHT, and height is what the panel has a fixed amount of.
        /// </summary>
        public static string ClampChatLine(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            string flat = text.Replace('\r', ' ').Replace('\n', ' ').Trim();

            return flat.Length <= MaxChatLength ? flat : flat.Substring(0, MaxChatLength);
        }

        public void BeginCountdownClientRpc()
        {
            if (!NetAuthority.IsHost) return;

            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(16, Allocator.Temp);
                _nm.CustomMessagingManager.SendNamedMessageToAll("BeginCountdown", writer);
            }
        }

        private void OnBeginCountdownMsg(ulong senderClientId, FastBufferReader reader)
        {
            FindFirstObjectByType<ReadyGate>()?.StartLocalCountdown();
        }

        // -------------------------------------------------------------------
        // THE REMATCH VOTE
        //
        // ⚠️⚠️ THREE MESSAGES, NOT ONE, AND THE MIDDLE ONE IS THE POINT. A vote that only
        // travelled peer-to-host would start the rematch correctly and leave the other three
        // players staring at a button they had already pressed, with no way to tell whether
        // anybody else had. `match_result.gd` draws the tally for the same reason: waiting is
        // only tolerable when you can see what you are waiting for.
        //
        // ⚠️ IT MIRRORS THE READY GATE ABOVE DELIBERATELY, down to taking the sender id NGO
        // supplies at the door. The two are the same problem (count the PEERS, not the
        // characters, because bot-filled seats cannot press anything) and a second shape for it
        // is a second thing to get wrong.
        // -------------------------------------------------------------------

        /// <returns>False when the vote could not be delivered. See `DeclareReadyServerRpc`.</returns>
        public bool VoteRematchServerRpc()
        {
            if (NetAuthority.IsHost)
            {
                FindFirstObjectByType<UI.MatchResult>()?.HostReceiveVote(NetAuthority.LocalPeerId);
                return true;
            }

            if (_nm == null || _nm.CustomMessagingManager == null || !_nm.IsConnectedClient)
                return false;

            using var writer = new FastBufferWriter(1, Allocator.Temp);
            _nm.CustomMessagingManager.SendNamedMessage("VoteRematch", NetworkManager.ServerClientId, writer);
            return true;
        }

        private void OnVoteRematchMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            FindFirstObjectByType<UI.MatchResult>()?.HostReceiveVote((int)senderClientId);
        }

        /// <summary>
        /// PHASE 12: this peer's ballot for the next map. `docs/TODO.md` § 130.12 and § 130.18.
        ///
        /// ⚠️⚠️ IT IS THE HALF § 130.12 DELIBERATELY DID NOT BUILD, AND THE REASON IT COULD BE
        /// BUILT NOW IS THAT SOMETHING ELSE HAD ALREADY PAID FOR THE BUMP. That entry shipped the
        /// rotation over the existing `SelectMap` broadcast specifically so it would not move
        /// `ProtocolVersion`, because moving it forces the Windows player and the .apk to be
        /// rebuilt and shipped together (`CLAUDE.md` § 4a). LAST TSINELAS's match half moved it to
        /// 22 in the same commit, so the ballot rides a bump that was already being paid rather
        /// than costing a second dual rebuild later. **This is the cheap moment and there is not
        /// another one until the next bump.**
        ///
        /// ⚠️ THE SEAT IS RESOLVED ON THE HOST FROM THE SENDER, NEVER TAKEN FROM THE PAYLOAD.
        /// `TrySenderSeat` is the same guard every other peer-to-host message uses: a client that
        /// could name its own seat could cast three ballots and hand itself the map.
        /// </summary>
        public bool SelectMapVoteServerRpc(int mapIndex)
        {
            if (NetAuthority.IsHost)
            {
                FindFirstObjectByType<UI.MatchResult>()?.HostReceiveMapVote(NetAuthority.LocalSlot, mapIndex);
                return true;
            }

            if (_nm == null || _nm.CustomMessagingManager == null || !_nm.IsConnectedClient)
                return false;

            using var writer = new FastBufferWriter(8, Allocator.Temp);
            writer.WriteValueSafe(mapIndex);
            _nm.CustomMessagingManager.SendNamedMessage("SelectMapVote", NetworkManager.ServerClientId, writer);
            return true;
        }

        private void OnSelectMapVoteMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            if (!TrySenderSeat(senderClientId, out int seat)) return;

            reader.ReadValueSafe(out int mapIndex);
            FindFirstObjectByType<UI.MatchResult>()?.HostReceiveMapVote(seat, mapIndex);
        }

        /// <summary>
        /// HOST ONLY. The whole ballot, so every board can draw who wants what.
        ///
        /// ⚠️ THE WHOLE TABLE TRAVELS FOR `BroadcastTsinelas`' REASON: a "seat 2 voted for map 1"
        /// delta that is dropped leaves a board permanently one vote out with no way to notice,
        /// and four integers sent on the handful of frames somebody presses the chip costs less
        /// than the code to detect that drift.
        ///
        /// ⚠️ AND IT IS THE DISPLAY ONLY. `MapRotationRules.Decide` runs on the host and the
        /// answer reaches every peer through the `SelectMap` broadcast the rotation already used,
        /// which is `SceneFlow.AdvanceMapRotation`'s own note: four peers each tallying is four
        /// different maps.
        /// </summary>
        public void MapVoteTallyClientRpc(int[] votes)
        {
            if (!NetAuthority.IsHost) return;
            if (_nm == null || _nm.CustomMessagingManager == null || votes == null) return;

            int count = Mathf.Min(votes.Length, Core.Balance.PlayerCount);

            using var writer = new FastBufferWriter(8 + (count * 4), Allocator.Temp);
            writer.WriteValueSafe(count);
            for (int i = 0; i < count; i++) writer.WriteValueSafe(votes[i]);

            _nm.CustomMessagingManager.SendNamedMessageToAll("MapVoteTally", writer);
        }

        private void OnMapVoteTallyMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (NetAuthority.IsHost) return;
            if (!FromHost(senderClientId)) return;

            reader.ReadValueSafe(out int count);
            if (count < 0 || count > Core.Balance.PlayerCount) return;

            var votes = new int[Core.Balance.PlayerCount];
            for (int i = 0; i < votes.Length; i++) votes[i] = Core.MapRotationRules.NoVote;

            for (int i = 0; i < count; i++)
            {
                reader.ReadValueSafe(out int vote);
                votes[i] = vote;
            }

            FindFirstObjectByType<UI.MatchResult>()?.ApplyNetworkMapVotes(votes);
        }

        /// <summary>HOST ONLY. Broadcasts "n of m have voted" so every screen can draw it.</summary>
        public void RematchTallyClientRpc(int votes, int expected)
        {
            if (!NetAuthority.IsHost) return;
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            using var writer = new FastBufferWriter(32, Allocator.Temp);
            writer.WriteValueSafe(votes);
            writer.WriteValueSafe(expected);
            _nm.CustomMessagingManager.SendNamedMessageToAll("RematchTally", writer);
        }

        private void OnRematchTallyMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int votes);
            reader.ReadValueSafe(out int expected);
            FindFirstObjectByType<UI.MatchResult>()?.ShowTally(votes, expected);
        }

        /// <summary>HOST ONLY. Every playing peer has voted; everyone starts.</summary>
        public void BeginRematchClientRpc()
        {
            if (!NetAuthority.IsHost) return;
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            using var writer = new FastBufferWriter(16, Allocator.Temp);
            _nm.CustomMessagingManager.SendNamedMessageToAll("BeginRematch", writer);
        }

        private void OnBeginRematchMsg(ulong senderClientId, FastBufferReader reader)
        {
            FindFirstObjectByType<UI.MatchResult>()?.BeginRematchLocally();
        }

        // -------------------------------------------------------------------
        // MOVEMENT AND POSITION SYNCHRONIZATION
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ `grounded` IS THE OWNER'S OWN `IsGrounded` AND NOTHING ELSE CAN SUPPLY IT. The
        /// host does not run gravity for a client-driven body, so its copy of that body's
        /// `_grounded` is stale; without this field on THIS payload the relay below has nothing
        /// truthful to send on. See `CharacterMotor.StepNetworkReplica`.
        /// </summary>
        public void SubmitMoveServerRpc(int slot, Vector3 pos, float yaw, Vector3 velocity,
                                        bool grounded)
        {
            if (NetAuthority.IsHost)
            {
                ApplyUnitMove(slot, pos, yaw, velocity, grounded);
                SyncUnitTransformClientRpc(slot, pos, yaw, velocity);
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(pos);
            writer.WriteValueSafe(yaw);
            writer.WriteValueSafe(velocity);
            writer.WriteValueSafe(grounded);
            _nm.CustomMessagingManager.SendNamedMessage("SubmitMove", NetworkManager.ServerClientId,
                                                        writer, PoseDelivery);
        }

        private void OnSubmitMoveMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;

            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out Vector3 pos);
            reader.ReadValueSafe(out float yaw);
            reader.ReadValueSafe(out Vector3 velocity);
            reader.ReadValueSafe(out bool grounded);

            if (!SenderOwnsClaimedSeat(senderClientId, slot, out var unit)) return;
            if (!AcceptMove(slot, unit, pos, yaw, velocity))
            {
                SyncUnitTransformClientRpc(slot, unit.transform.position,
                                           unit.transform.eulerAngles.y, unit.Velocity);
                return;
            }

            ApplyUnitMove(slot, pos, yaw, velocity, grounded);
            SyncUnitTransformClientRpc(slot, pos, yaw, velocity);
        }

        /// <summary>
        /// ⚠️ THIS IS WHAT MAKES THE RELAY HONEST. Storing the owner's `grounded` onto the host's
        /// copy is what lets `SyncUnitTransformClientRpc` read `unit.IsGrounded` off the unit for
        /// EVERY seat, host-driven and client-driven alike, exactly as it already reads stun and
        /// stamina. Without this the relay would send the host's stale false back out to
        /// everybody and the pose would be wrong on three screens instead of one.
        /// </summary>
        private static void ApplyUnitMove(int slot, Vector3 pos, float yaw, Vector3 velocity,
                                          bool grounded)
        {
            var unit = Unit(slot);
            if (unit == null) return;

            unit.ApplyNetworkTransform(pos, yaw, velocity, grounded,
                                       reconcileLocal: false, force: true);
        }

        public void SyncUnitTransformClientRpc(int slot, Vector3 pos, float yaw, Vector3 velocity)
        {
            if (!NetAuthority.IsHost) return;
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            var unit = Unit(slot);
            if (unit == null) return;

            using var writer = new FastBufferWriter(192, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(pos);
            writer.WriteValueSafe(yaw);
            writer.WriteValueSafe(velocity);

            // ⚠️ READ OFF THE UNIT, LIKE EVERY OTHER FIELD BELOW, WHICH IS ONLY CORRECT BECAUSE
            // `ApplyUnitMove` HAS ALREADY STORED THE OWNER'S VALUE. For a host-driven body this
            // is the real simulated flag; for a client-driven one it is what that client last
            // submitted. Inferring it from `velocity` on the receiving end is what this replaced.
            writer.WriteValueSafe(unit.IsGrounded);
            writer.WriteValueSafe(unit.StunLeft);
            writer.WriteValueSafe(unit.StunTotal);
            writer.WriteValueSafe((int)unit.StunElement);
            writer.WriteValueSafe(unit.StunBreakPresses);
            writer.WriteValueSafe(unit.StunMashPresses);
            writer.WriteValueSafe(unit.TripLeft);
            writer.WriteValueSafe(unit.TripTotal);
            writer.WriteValueSafe(unit.MashPresses);
            writer.WriteValueSafe(unit.MashRemoved);
            writer.WriteValueSafe(unit.Stamina.Current);
            writer.WriteValueSafe(unit.Stamina.IdleSeconds);
            writer.WriteValueSafe(unit.Stamina.FatigueLeft);
            _nm.CustomMessagingManager.SendNamedMessageToAll("SyncUnit", writer, PoseDelivery);
        }

        private void OnSyncUnitMsg(ulong senderClientId, FastBufferReader reader)
        {
            // ⚠️ THE HOST IS ITS OWN CLIENT AND `SendNamedMessageToAll` LOOPS BACK TO IT.
            // Netcode invokes the handler locally for the listen host, so every broadcast the
            // host sent was also applied ON the host, a second time, over authoritative state it
            // had just produced. See § THE LOOPBACK.
            if (NetAuthority.IsHost) return;

            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out Vector3 pos);
            reader.ReadValueSafe(out float yaw);
            reader.ReadValueSafe(out Vector3 velocity);
            reader.ReadValueSafe(out bool grounded);
            reader.ReadValueSafe(out float stunLeft);
            reader.ReadValueSafe(out float stunTotal);
            reader.ReadValueSafe(out int stunElement);
            reader.ReadValueSafe(out int stunBreakPresses);
            reader.ReadValueSafe(out int stunMashPresses);
            reader.ReadValueSafe(out float tripLeft);
            reader.ReadValueSafe(out float tripTotal);
            reader.ReadValueSafe(out int tripMashPresses);
            reader.ReadValueSafe(out float tripMashRemoved);
            reader.ReadValueSafe(out float staminaCurrent);
            reader.ReadValueSafe(out float staminaIdle);
            reader.ReadValueSafe(out float fatigueLeft);

            var unit = Unit(slot);
            if (unit == null) return;

            bool local = slot == NetAuthority.LocalSlot;
            unit.ApplyNetworkTransform(pos, yaw, velocity, grounded, reconcileLocal: local);
            unit.ApplyNetworkState(stunLeft, stunTotal, (StunElement)stunElement,
                                   stunBreakPresses, stunMashPresses,
                                   tripLeft, tripTotal, tripMashPresses, tripMashRemoved,
                                   staminaCurrent, staminaIdle, fatigueLeft);
        }

        // -------------------------------------------------------------------
        // COMBAT VERBS
        // -------------------------------------------------------------------

        public void RequestPunchServerRpc(int slot, Vector3 from, Vector3 facing)
        {
            if (NetAuthority.IsHost)
            {
                var who = Unit(slot);
                if (who != null && who.IsDefender)
                {
                    who.GetComponent<CombatVerbs>()?.HostResolvePunch(from, facing);
                }
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(from);
            writer.WriteValueSafe(facing);
            _nm.CustomMessagingManager.SendNamedMessage("ReqPunch", NetworkManager.ServerClientId, writer);
        }

        private void OnReqPunchMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out Vector3 from);
            reader.ReadValueSafe(out Vector3 facing);

            if (!SenderOwnsClaimedSeat(senderClientId, slot, out var who)) return;
            if (!PlausibleIntentPose(who, from) || !Finite(facing)) return;
            if (who != null && who.IsDefender)
            {
                if (who.GetComponent<CombatVerbs>()?.HostResolvePunch(from, facing) == true)
                    BroadcastAction(slot, "punch", senderClientId);
            }
        }

        public void RequestLungeServerRpc(int slot, Vector3 from, Vector3 facing, float power)
        {
            if (NetAuthority.IsHost)
            {
                var who = Unit(slot);
                if (who != null && who.IsDefender)
                {
                    who.GetComponent<CombatVerbs>()?.HostResolveLunge(from, facing, power);
                }
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(from);
            writer.WriteValueSafe(facing);
            writer.WriteValueSafe(power);
            _nm.CustomMessagingManager.SendNamedMessage("ReqLunge", NetworkManager.ServerClientId, writer);
        }

        private void OnReqLungeMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out Vector3 from);
            reader.ReadValueSafe(out Vector3 facing);
            reader.ReadValueSafe(out float power);

            if (!SenderOwnsClaimedSeat(senderClientId, slot, out var who)) return;
            if (!PlausibleIntentPose(who, from) || !Finite(facing) || !Finite(power)) return;
            power = Mathf.Clamp(power, Balance.LungeMinPower, 1.0f);
            if (who != null && who.IsDefender)
            {
                if (who.GetComponent<CombatVerbs>()?.HostResolveLunge(from, facing, power) == true)
                    BroadcastAction(slot, "lunge", senderClientId);
            }
        }

        public void RequestShoveServerRpc(int slot, Vector3 from, Vector3 facing)
        {
            if (NetAuthority.IsHost)
            {
                var who = Unit(slot);
                if (who != null && !who.IsDefender)
                {
                    who.GetComponent<CombatVerbs>()?.HostResolveShove(from, facing);
                }
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(from);
            writer.WriteValueSafe(facing);
            _nm.CustomMessagingManager.SendNamedMessage("ReqShove", NetworkManager.ServerClientId, writer);
        }

        private void OnReqShoveMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out Vector3 from);
            reader.ReadValueSafe(out Vector3 facing);

            if (!SenderOwnsClaimedSeat(senderClientId, slot, out var who)) return;
            if (!PlausibleIntentPose(who, from) || !Finite(facing)) return;
            if (who != null && !who.IsDefender)
            {
                if (who.GetComponent<CombatVerbs>()?.HostResolveShove(from, facing) == true)
                    BroadcastAction(slot, "shove", senderClientId);
            }
        }

        // ⚠️⚠️ `LungeCharge` AND `ShoveCharge` ARE DELETED, NOT MOVED. They were a second
        // protocol for a job `PlayAction` now does: a pair of host-only broadcasts that named a
        // clip and a bool, with NO PRODUCTION CALL SITE anywhere in the tree since they were
        // written. Two protocols for one verb is how one of them stops being maintained, and the
        // one that had never been called was always going to be that one.
        // `tools/audit_request_call_sites.py` is what found them and is what stops the next pair.

        public void RequestGrabServerRpc(int slot, int slipperOwnerSlot)
        {
            if (NetAuthority.IsHost)
            {
                var who = Unit(slot);
                var slipper = FindSlipper(slipperOwnerSlot);
                if (who != null && slipper != null && slipper.CanBeGrabbedBy(who))
                {
                    who.GetComponent<Carrier>()?.HostPickUp(slipper);
                }
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(slipperOwnerSlot);
            _nm.CustomMessagingManager.SendNamedMessage("ReqGrab", NetworkManager.ServerClientId, writer);
        }

        private void OnReqGrabMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out int slipperOwnerSlot);

            if (!SenderOwnsClaimedSeat(senderClientId, slot, out var who)) return;
            var slipper = FindSlipper(slipperOwnerSlot);
            if (who != null && slipper != null && slipper.CanBeGrabbedBy(who))
            {
                who.GetComponent<Carrier>()?.HostPickUp(slipper);
            }
        }

        public void RequestThrowServerRpc(int slot, Vector3 origin, Vector3 aimPoint,
                                          float charge, float spin = 0.0f)
        {
            if (NetAuthority.IsHost)
            {
                var who = Unit(slot);
                var carrier = who != null ? who.GetComponent<Carrier>() : null;
                if (carrier != null && carrier.Held != null && GameServices.Round != null && GameServices.Round.CanThrow(who))
                {
                    carrier.HostThrowAt(origin, aimPoint, charge, spin);
                }
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(origin);
            writer.WriteValueSafe(aimPoint);
            writer.WriteValueSafe(charge);
            writer.WriteValueSafe(spin);
            _nm.CustomMessagingManager.SendNamedMessage("ReqThrow", NetworkManager.ServerClientId, writer);
        }

        private void OnReqThrowMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out Vector3 origin);
            reader.ReadValueSafe(out Vector3 aimPoint);
            reader.ReadValueSafe(out float charge);
            reader.ReadValueSafe(out float spin);

            if (!SenderOwnsClaimedSeat(senderClientId, slot, out var who)) return;
            if (!PlausibleIntentPose(who, origin) || !Finite(aimPoint) ||
                !Finite(charge) || !Finite(spin)) return;
            charge = Mathf.Clamp01(charge);
            spin = Mathf.Clamp(spin, -Balance.MaxPektusSpin, Balance.MaxPektusSpin);
            var carrier = who != null ? who.GetComponent<Carrier>() : null;
            if (carrier != null && carrier.Held != null && GameServices.Round != null && GameServices.Round.CanThrow(who))
            {
                carrier.HostThrowAt(origin, aimPoint, charge, spin);
            }
        }

        // -------------------------------------------------------------------
        // THE DEFENDER'S RESET CHANNEL
        //
        // ⚠️⚠️ IT IS A CHANNEL, NOT A BUTTON, AND THE OLD MESSAGE TREATED IT AS A BUTTON. A taya
        // holds Grab inside the ring for `Lata.ResetChannelTime` to stand the can back up, and
        // that hold is the entire counterplay: the attackers get a window in which the defender
        // is committed and standing still. `ReqReset` used to carry one slot and nothing else,
        // so the host restored the can the instant it arrived. A client could therefore send it
        // with no hold at all, from anywhere on the map, as often as it liked.
        //
        // ⚠️⚠️ AND THE HOST MEASURES THE HOLD ITSELF RATHER THAN BELIEVING A REPORTED DURATION.
        // The owner sends START, CANCEL and COMPLETE; the host stamps its own clock at START,
        // drops the channel on its own physics step the moment the defender leaves
        // `Balance.InteractionRadius`, loses the role, or is stunned, and refuses a COMPLETE that
        // arrives early. A number in a payload is a number the sender chose.
        //
        // ⚠️ ONE CHANNEL PER SEAT. A second START simply restamps, which is what a legitimate
        // re-press after an interruption looks like.
        // -------------------------------------------------------------------

        public enum ResetPhase : byte { Start = 0, Cancel = 1, Complete = 2 }

        /// <summary>Slot to the host's own start timestamp for an open reset channel.</summary>
        private readonly Dictionary<int, float> _resetChannelStart = new Dictionary<int, float>();

        /// <summary>
        /// ⚠️ ONE PHYSICS STEP OF SLACK, AND NOT A FRAME MORE THAN THAT. A client's local clock
        /// reaches the channel time up to one step before the host's does, purely because the two
        /// processes step at different offsets. Refusing that COMPLETE would make the bar fill and
        /// nothing happen, which is the worst of both.
        /// </summary>
        private const float ResetChannelLeeway = 0.05f;

        public void RequestLataResetServerRpc(int slot, ResetPhase phase)
        {
            if (NetAuthority.IsHost)
            {
                HostApplyResetPhase(slot, phase);
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe((byte)phase);
            _nm.CustomMessagingManager.SendNamedMessage("ReqReset", NetworkManager.ServerClientId, writer);
        }

        private void OnReqResetMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out byte phase);
            if (phase > (byte)ResetPhase.Complete) return;
            if (!SenderOwnsClaimedSeat(senderClientId, slot, out _)) return;

            HostApplyResetPhase(slot, (ResetPhase)phase);
        }

        private void HostApplyResetPhase(int slot, ResetPhase phase)
        {
            if (!NetAuthority.IsHost) return;

            if (phase == ResetPhase.Cancel)
            {
                _resetChannelStart.Remove(slot);
                return;
            }

            if (!HostMayChannelReset(slot))
            {
                _resetChannelStart.Remove(slot);
                return;
            }

            if (phase == ResetPhase.Start)
            {
                if (!_resetChannelStart.ContainsKey(slot))
                {
                    _resetChannelStart[slot] = Time.time;

                    // The defender already played their own reach-down on the frame they pressed.
                    BroadcastActionExceptOwner(slot, "grab");
                }
                return;
            }

            var lata = GameServices.Round?.Lata;
            if (lata == null) return;
            if (!_resetChannelStart.TryGetValue(slot, out float startedAt)) return;
            if (Time.time - startedAt < lata.ResetChannelTime - ResetChannelLeeway) return;

            _resetChannelStart.Remove(slot);
            lata.HostRestore();
            UI.Hud.ReportStyle(slot, 24.0f, "BANGON!");
            BroadcastLataState();
        }

        /// <summary>Every condition `Carrier.StepDefender` checks, re-checked by the owner of the can.</summary>
        private bool HostMayChannelReset(int slot)
        {
            var round = GameServices.Round;
            var lata = round?.Lata;
            var who = Unit(slot);

            if (lata == null || who == null || lata.IsUpright) return false;
            if (!who.IsDefender || !who.CanAct()) return false;

            Vector3 a = who.transform.position;
            Vector3 b = lata.transform.position;
            a.y = 0.0f;
            b.y = 0.0f;
            return Vector3.Distance(a, b) <= Balance.InteractionRadius;
        }

        /// <summary>
        /// Drops any channel whose conditions stopped holding, on the host's own step rather
        /// than at COMPLETE time. Without this a defender could open a channel legitimately, walk
        /// out of the ring, and still land the reset a second later.
        /// </summary>
        /// <summary>When each open reset channel last had its reach-down relayed to the peers.</summary>
        private readonly Dictionary<int, float> _lastResetGestureRelay = new Dictionary<int, float>();

        private void HostStepResetChannels()
        {
            if (_resetChannelStart.Count == 0) return;

            List<int> dead = null;
            foreach (var kv in _resetChannelStart)
            {
                if (!HostMayChannelReset(kv.Key))
                {
                    (dead ??= new List<int>()).Add(kv.Key);
                    continue;
                }

                // ⚠️⚠️ THE REACH-DOWN IS RELAYED FOR THE WHOLE HOLD, NOT ONCE AT THE START. 🧑
                // 2026-08-29, of the animation work: *"make sure everyone sees this not just host
                // or client"*. `Carrier.StepDefender` re-fires the gesture every
                // `ViewmodelArms.GrabSeconds` because the channel runs for
                // `Balance.ResetChannelTime`, 1.5 s, and one 0.40 s reach leaves two thirds of
                // the longest hold in the game with nothing moving in it. That repeat was purely
                // LOCAL: `ResetPhase.Start` is sent once, so the taya saw themselves reaching
                // over and over while the other three saw one reach and then a statue for 1.1 s.
                //
                // ⚠️ RELAYED FROM THE HOST ON ITS OWN CLOCK RATHER THAN BY A NEW WIRE PHASE.
                // Adding a `Repeat` to `ResetPhase` would be a protocol change, and § 59.4 is
                // what a protocol bump costs: both machines rebuilt off the same commit or they
                // refuse each other at approval. The host already knows the channel is open and
                // already ticks every physics step, so it can produce the repeat without anybody
                // sending anything new.
                //
                // ⚠️ AND IT SKIPS THE OWNER, like the `Start` relay above it, because that peer
                // is the one already playing it locally on its own timer.
                float now = Time.time;
                float last = _lastResetGestureRelay.TryGetValue(kv.Key, out float t) ? t : kv.Value;

                if (now - last >= CameraSystem.ViewmodelArms.GrabSeconds)
                {
                    _lastResetGestureRelay[kv.Key] = now;
                    BroadcastActionExceptOwner(kv.Key, "grab");
                }
            }

            if (dead == null) return;

            foreach (int slot in dead)
            {
                _resetChannelStart.Remove(slot);
                _lastResetGestureRelay.Remove(slot);
            }
        }

        public void RequestEmoteServerRpc(int slot, string id)
        {
            if (NetAuthority.IsHost)
            {
                var who = Unit(slot);
                var player = who != null ? who.GetComponent<Social.EmotePlayer>() : null;
                if (player != null && player.CanEmote())
                {
                    PlayEmoteClientRpc(slot, id);
                }
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(id ?? "");
            _nm.CustomMessagingManager.SendNamedMessage("ReqEmote", NetworkManager.ServerClientId, writer);
        }

        private void OnReqEmoteMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out string id);

            if (!SenderOwnsClaimedSeat(senderClientId, slot, out var who)) return;
            var player = who != null ? who.GetComponent<Social.EmotePlayer>() : null;
            if (player != null && player.CanEmote())
            {
                PlayEmoteClientRpc(slot, id);
            }
        }

        private void PlayEmoteClientRpc(int slot, string id)
        {
            if (!NetAuthority.IsHost) return;
            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(64, Allocator.Temp);
                writer.WriteValueSafe(slot);
                writer.WriteValueSafe(id ?? "");
                _nm.CustomMessagingManager.SendNamedMessageToAll("PlayEmote", writer);
            }
        }

        private void OnPlayEmoteMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!FromHost(senderClientId)) return;

            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out string id);
            if (!ValidSlot(slot)) return;

            Unit(slot)?.GetComponent<Social.EmotePlayer>()?.Play(id);
        }

        // -------------------------------------------------------------------
        // WORLD SOUND, AND THE ONE ABILITY EFFECT THAT MOVES SOMEBODY ELSE'S BODY
        //
        // ⚠️⚠️ THE CUE RELAY IS THE ANSWER TO A MEASURED FAULT, NOT A CONVENIENCE.
        // `tools/audit_audio_reach.py` reports every `GameServices.Audio` call whose enclosing
        // method sits behind an open `NetAuthority.ShouldResolve()` return, and two of them are
        // the loudest events in the game: `Carrier.HostThrowAt` plays `throw_release` and
        // `Lata.HostKnockDown` plays `lata_seal`. Both are host-only, so in a networked match a
        // client has never heard a throw leave a hand or the can go over. `TumbangPreso.NetCue`
        // is the call site's half; this is the wire.
        //
        // ⚠️ IT SENDS PER CLIENT RATHER THAN `SendNamedMessageToAll`, BECAUSE THE PEER THAT MADE
        // THE SOUND HAS ALREADY PLAYED IT. `NetCue` plays locally first so the player who threw
        // hears it on the frame they threw, with no round trip; relaying to everyone would give
        // that one peer the sound twice, a few tens of milliseconds apart, which is a flam rather
        // than an echo and is worse than either.
        // -------------------------------------------------------------------

        /// <summary>Play a world cue on every peer except the one that already played it.</summary>
        public void BroadcastCue(string id, Vector3 position, float volumeScale)
        {
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            if (!NetAuthority.IsHost)
            {
                using var ask = new FastBufferWriter(96, Allocator.Temp);
                ask.WriteValueSafe(id ?? "");
                ask.WriteValueSafe(position);
                ask.WriteValueSafe(volumeScale);
                _nm.CustomMessagingManager.SendNamedMessage("ReqCue", NetworkManager.ServerClientId, ask);
                return;
            }

            HostRelayCue(id, position, volumeScale, _nm.LocalClientId);
        }

        /// <summary>
        /// ⚠️ `except` IS THE PEER THAT ALREADY HEARD IT, and on the host's own cue that is the
        /// host. A dedicated server is a referee with no seat (`NetAuthority.IsSeatlessReferee`),
        /// so on the VPS path nothing is excluded that anybody was listening on: the local
        /// `PlayAt` in `NetCue` goes to a machine with no player at it and this reaches all four.
        /// </summary>
        private void HostRelayCue(string id, Vector3 position, float volumeScale, ulong except)
        {
            if (!NetAuthority.IsHost || _nm == null || _nm.CustomMessagingManager == null) return;

            foreach (ulong client in _nm.ConnectedClientsIds)
            {
                if (client == except) continue;

                using var writer = new FastBufferWriter(96, Allocator.Temp);
                writer.WriteValueSafe(id ?? "");
                writer.WriteValueSafe(position);
                writer.WriteValueSafe(volumeScale);
                _nm.CustomMessagingManager.SendNamedMessage("PlayCue", client, writer);
            }
        }

        /// <summary>
        /// ⚠️⚠️ A CUE IS THE ONE MESSAGE A CLIENT CAN SEND AS OFTEN AS IT LIKES, AND THE HOST
        /// FANS EACH ONE OUT TO EVERY PEER. That makes it the cheapest amplifier in the protocol:
        /// one client sending a cue every frame costs itself 60 messages a second and costs the
        /// host 60 times the peer count, on the audio thread, at whatever world position it
        /// chose. The budget below is well above anything play produces (a throw, a bounce and a
        /// footfall in the same tenth of a second is three) and well below anything that hurts.
        /// </summary>
        private const int CueBudgetPerSecond = 25;

        private readonly Dictionary<ulong, float> _cueWindowStart = new Dictionary<ulong, float>();
        private readonly Dictionary<ulong, int> _cueWindowCount = new Dictionary<ulong, int>();

        private bool CueBudgetAllows(ulong senderClientId)
        {
            float now = Time.realtimeSinceStartup;

            if (!_cueWindowStart.TryGetValue(senderClientId, out float start) || now - start >= 1.0f)
            {
                _cueWindowStart[senderClientId] = now;
                _cueWindowCount[senderClientId] = 1;
                return true;
            }

            int count = _cueWindowCount.TryGetValue(senderClientId, out int c) ? c : 0;
            if (count >= CueBudgetPerSecond) return false;

            _cueWindowCount[senderClientId] = count + 1;
            return true;
        }

        private void OnReqCueMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;

            reader.ReadValueSafe(out string id);
            reader.ReadValueSafe(out Vector3 position);
            reader.ReadValueSafe(out float volumeScale);

            // ⚠️ THE ID IS VALIDATED AGAINST THE CATALOGUE, NOT TRUSTED. `PlayAtVaried` looks a
            // cue up by string, so an unknown id is a silent miss on every peer and a long one is
            // a long string relayed four times. Only a cue this build actually owns travels.
            if (!Audio.AudioCues.IsKnown(id)) return;
            if (!Finite(position) || !Finite(volumeScale)) return;
            if (position.sqrMagnitude > 250000.0f) return;
            if (!CueBudgetAllows(senderClientId)) return;

            volumeScale = Mathf.Clamp(volumeScale, 0.0f, 1.5f);

            // ⚠️ THE HOST PLAYS IT TOO. It is not the sender, so it did not play it locally, and
            // a host that only relayed would be the one machine that could not hear a client's
            // throw. It is excluded from the relay below for the opposite reason, so both
            // branches together mean every peer plays every cue exactly once.
            GameServices.Audio?.PlayAtVaried(id, position, 0.94f, 1.06f, volumeScale);
            HostRelayCue(id, position, volumeScale, senderClientId);
        }

        // -------------------------------------------------------------------
        // § THE VISUAL HALF OF A CUE
        //
        // ⚠️⚠️ EVERY POPUP, BURST, STAR, CAMERA PUNCH AND STYLE AWARD IN A HOST-RESOLVED VERB
        // WAS DRAWN ON ONE SCREEN. 🧑 2026-08-29: *"ur final task is to make sure that all host
        // sided shit is seen by everyone and not js host"*. `NetCue` had already separated
        // deciding from announcing for SOUND; the things you look at were still written on the
        // line after the resolution, inside the same `ShouldResolve()` gate.
        // `tools/audit_presentation_reach.py` counted 41 of them across seven methods.
        //
        // ⚠️ IT CARRIES A KIND AND SEATS, NOT A DESCRIPTION OF AN EFFECT. Every peer already has
        // all four bodies and the whole roster; what it lacks is the event. `Visual.MatchFlair`
        // rebuilds the presentation from its own scene, which is also what makes a client's
        // camera punch land on the client's camera.
        // -------------------------------------------------------------------

        /// <summary>HOST ONLY. Tells every other peer to draw one match moment.</summary>
        public void BroadcastFlair(byte kind, int actor, int subject, Vector3 at, float strength)
        {
            // ⚠️ A CLIENT THAT REACHES `MatchFlair.Announce` DRAWS AND SENDS NOTHING. The host is
            // the only peer that may decide a tag happened, so it is the only one whose account
            // of it may travel; the guard is here rather than at each call site for the same
            // reason `MatchDirector.AddScore` keeps its own.
            if (!NetAuthority.IsHost || _nm == null || _nm.CustomMessagingManager == null) return;
            if (!Finite(at) || !Finite(strength)) return;

            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(kind);
            writer.WriteValueSafe(actor);
            writer.WriteValueSafe(subject);
            writer.WriteValueSafe(at);
            writer.WriteValueSafe(strength);
            HostRelayFlair(writer);
        }

        private void HostRelayFlair(FastBufferWriter writer)
        {
            foreach (ulong clientId in _nm.ConnectedClientsIds)
            {
                // ⚠️ NOT BACK TO THE HOST'S OWN CLIENT ID. `MatchFlair.Announce` has already
                // drawn it here, on the frame it happened; the same rule `HostRelayCue` states.
                if (clientId == _nm.LocalClientId) continue;
                _nm.CustomMessagingManager.SendNamedMessage("Flair", clientId, writer);
            }
        }

        private void OnFlairMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!FromHost(senderClientId)) return;

            reader.ReadValueSafe(out byte kind);
            reader.ReadValueSafe(out int actor);
            reader.ReadValueSafe(out int subject);
            reader.ReadValueSafe(out Vector3 at);
            reader.ReadValueSafe(out float strength);

            if (!Finite(at) || !Finite(strength)) return;
            if (actor < -1 || actor >= Balance.PlayerCount) return;
            if (subject < -1 || subject >= Balance.PlayerCount) return;

            // ⚠️ `Play`, NOT `Announce`. A replicated copy must not relay itself onward, which is
            // the same loop `NetCue.SuppressRelay` exists to break for sounds.
            Visual.MatchFlair.Play((Visual.MatchFlair.Kind)kind, actor, subject, at, strength);
        }

        private void OnPlayCueMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!FromHost(senderClientId)) return;

            reader.ReadValueSafe(out string id);
            reader.ReadValueSafe(out Vector3 position);
            reader.ReadValueSafe(out float volumeScale);
            if (!Finite(position) || !Finite(volumeScale)) return;

            GameServices.Audio?.PlayAtVaried(id, position, 0.94f, 1.06f, volumeScale);
        }

        // ⚠️⚠️ `ReqBlink` IS DELETED, AND THE VERB IT CARRIED IS NOT. Phaister's blink
        // knockback had its own private request message because the ability layer had no cast
        // rpc of any kind, so one power out of eighteen was wired by hand. `ReqAbility` now
        // replicates every cast, the host runs the same kit code the solo game runs, and the
        // blink resolves inside it: the bespoke channel had become a second protocol for a job
        // the general one already does. See § HERO ABILITIES.

        // -------------------------------------------------------------------
        // HERO ABILITIES
        // -------------------------------------------------------------------

        /// <summary>
        /// The owning client has predicted a cast. It sends only the slot and cast frame; the
        /// host owns the kit, re-checks its cooldown, charge, role, and stun state, then decides
        /// every victim. No message ever contains a victim list or a score result.
        /// </summary>
        public void RequestAbilityCastServerRpc(int claimedSlot, int abilitySlot,
                                                Vector3 position, Vector3 forward,
                                                Vector3 aimPoint, float heldSeconds)
        {
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            if (NetAuthority.IsHost)
            {
                var system = Unit(claimedSlot)?.AbilitySystem;
                var slot = (Abilities.HeroAbilitySystem.Slot)Mathf.Clamp(abilitySlot, 0, 2);
                if (system?.ApplyNetworkCast(slot, position, forward, aimPoint,
                                             heldSeconds, authoritative: true)
                    == Abilities.HeroKit.CastOutcome.Cast)
                {
                    BroadcastAbilityCast(claimedSlot, abilitySlot, position, forward,
                                         aimPoint, heldSeconds, null);
                    BroadcastAbilityState(claimedSlot, Unit(claimedSlot));
                }
                return;
            }

            using var writer = new FastBufferWriter(128, Allocator.Temp);
            writer.WriteValueSafe(claimedSlot);
            writer.WriteValueSafe(abilitySlot);
            writer.WriteValueSafe(position);
            writer.WriteValueSafe(forward);
            writer.WriteValueSafe(aimPoint);
            writer.WriteValueSafe(heldSeconds);
            _nm.CustomMessagingManager.SendNamedMessage("ReqAbility", NetworkManager.ServerClientId, writer);
        }

        private void OnReqAbilityMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;

            reader.ReadValueSafe(out int claimedSlot);
            reader.ReadValueSafe(out int abilitySlot);
            reader.ReadValueSafe(out Vector3 position);
            reader.ReadValueSafe(out Vector3 forward);
            reader.ReadValueSafe(out Vector3 aimPoint);
            reader.ReadValueSafe(out float heldSeconds);

            if (abilitySlot < 0 || abilitySlot > 2) return;
            if (!SenderOwnsClaimedSeat(senderClientId, claimedSlot, out var unit)) return;

            // ⚠️⚠️ FROM HERE DOWN EVERY REFUSAL ANSWERS THE SENDER. Above this line the message is
            // malformed or is claiming a seat it does not hold, and the host cannot know what the
            // sender predicted; below it the sender is the verified owner of a seat that really
            // did predict this cast, so a silent `return` is the host charging a player for an
            // ability it then declined to run. See the § note above `HostDenyAbilityCast`.
            if (!PlausibleIntentPose(unit, position) || !Finite(forward) ||
                !Finite(aimPoint) || !Finite(heldSeconds))
            {
                HostDenyAbilityCast(senderClientId, claimedSlot, abilitySlot);
                return;
            }

            heldSeconds = Mathf.Clamp(heldSeconds, 0.0f, 30.0f);
            var system = unit.AbilitySystem;
            if (system == null)
            {
                HostDenyAbilityCast(senderClientId, claimedSlot, abilitySlot);
                return;
            }

            var slot = (Abilities.HeroAbilitySystem.Slot)abilitySlot;
            var outcome = system.ApplyNetworkCast(slot, position, forward, aimPoint,
                                                  heldSeconds, authoritative: true);
            if (outcome != Abilities.HeroKit.CastOutcome.Cast)
            {
                // ⚠️ `Missing` IS REFUSED LIKE THE REST AND THAT IS DELIBERATE. A hero with no
                // second skill cannot have predicted one, so this is unreachable for that reason;
                // if it ever becomes reachable, the client having spent nothing means the refund
                // is a no-op rather than a gift. Refusing everything that is not `Cast` keeps the
                // rule "the host answers every request it did not run" true without a list.
                HostDenyAbilityCast(senderClientId, claimedSlot, abilitySlot);
                return;
            }

            BroadcastAbilityCast(claimedSlot, abilitySlot, position, forward,
                                 aimPoint, heldSeconds, senderClientId);
            BroadcastAbilityState(claimedSlot, unit);
        }

        /// <summary>Host announcement. Every observer runs presentation; only the host resolves.</summary>
        public void BroadcastAbilityCast(int slot, int abilitySlot, Vector3 position,
                                         Vector3 forward, Vector3 aimPoint, float heldSeconds,
                                         ulong? exceptClientId)
        {
            if (!NetAuthority.IsHost || _nm == null || _nm.CustomMessagingManager == null) return;

            foreach (ulong clientId in _nm.ConnectedClientsIds)
            {
                if (clientId == _nm.LocalClientId ||
                    (exceptClientId.HasValue && clientId == exceptClientId.Value))
                    continue;

                using var writer = new FastBufferWriter(128, Allocator.Temp);
                writer.WriteValueSafe(slot);
                writer.WriteValueSafe(abilitySlot);
                writer.WriteValueSafe(position);
                writer.WriteValueSafe(forward);
                writer.WriteValueSafe(aimPoint);
                writer.WriteValueSafe(heldSeconds);
                _nm.CustomMessagingManager.SendNamedMessage("PlayAbility", clientId, writer);
            }
        }

        private void OnPlayAbilityMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (NetAuthority.IsHost || senderClientId != NetworkManager.ServerClientId) return;

            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out int abilitySlot);
            reader.ReadValueSafe(out Vector3 position);
            reader.ReadValueSafe(out Vector3 forward);
            reader.ReadValueSafe(out Vector3 aimPoint);
            reader.ReadValueSafe(out float heldSeconds);

            if (slot < 0 || slot >= Balance.PlayerCount || abilitySlot < 0 || abilitySlot > 2)
                return;

            Unit(slot)?.AbilitySystem?.ApplyNetworkCast(
                (Abilities.HeroAbilitySystem.Slot)abilitySlot,
                position, forward, aimPoint, heldSeconds, authoritative: false);
        }

        // -------------------------------------------------------------------
        // § THE REFUSAL, WHICH IS THE OTHER HALF OF A PREDICTED CAST
        //
        // ⚠️⚠️ A CLIENT PREDICTS EVERY CAST AND THE HOST USED TO REFUSE IN SILENCE.
        // `HeroAbilitySystem.Cast` runs the kit locally FIRST and then asks, so by the time
        // `OnReqAbilityMsg` drops a request the owner has already spent the cooldown, played the
        // confirm and drawn the effect. Every refusal in that handler was a bare `return`. The
        // client was then running a match the host was not refereeing, and nothing anywhere would
        // ever tell it so.
        //
        // ⚠️⚠️ AND THE ONE FIX THAT USED TO PAPER OVER IT WAS CORRECTLY REMOVED, WHICH IS WHY THIS
        // IS NEEDED NOW. Until `docs/TODO.md` § 71 the owner's cooldown was simply assigned from
        // the host's 5 Hz `SyncAbility`, so a refused cast healed itself: the host's copy still
        // read zero, the client took that zero, and the ability came back. That is the Phaister
        // *"spammable teleport (lan problem)"* bug, and `HeroAbility.ApplyNetworkSnapshot`'s
        // `mayLower` guard closed it by making the owner's cooldown raise-only. Closing it turned
        // a self-healing divergence into a permanent one: correct, and half a fix. The host may
        // still take an ability away at any time. What it could not do was give one back after
        // refusing to act, and a refusal is exactly when it must.
        //
        // ⚠️ IT IS SENT ONLY TO THE PEER THAT ASKED. Nobody else predicted anything, so nobody
        // else has anything to take back, and a broadcast would invite three other kits to roll
        // back a cast they never made.
        //
        // ⚠️ IT CARRIES NO REASON, ON PURPOSE. Six guards refuse for six reasons and the player
        // needs one outcome from all of them: the power back, and one beat that says it did not
        // go off. A reason code on the wire is a thing to keep in step for no gameplay gain.
        //
        // ⚠️⚠️ IT IS NOT SENT WHEN THE SENDER DOES NOT OWN THE SEAT IT CLAIMS. That request is
        // malformed or hostile rather than refused, this peer cannot know what the sender
        // actually predicted, and answering it would be the host taking direction about which kit
        // to touch from an unverified claim. `SenderOwnsClaimedSeat` stays a bare return.
        // -------------------------------------------------------------------

        /// <summary>Tells one client the cast it predicted was refused, so it can take it back.</summary>
        public void HostDenyAbilityCast(ulong clientId, int slot, int abilitySlot)
        {
            if (!NetAuthority.IsHost || _nm == null || _nm.CustomMessagingManager == null) return;

            // ⚠️ THE HOST NEVER DENIES ITSELF. Its own casts never travel as a request at all;
            // `RequestAbilityCastServerRpc` resolves them in its `IsHost` branch, so a refusal
            // there is just the kit saying no locally, which the deck already answers.
            if (clientId == _nm.LocalClientId) return;

            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(abilitySlot);
            _nm.CustomMessagingManager.SendNamedMessage("CastDenied", clientId, writer);
        }

        private void OnCastDeniedMsg(ulong senderClientId, FastBufferReader reader)
        {
            // ⚠️ THE HOST IS ITS OWN CLIENT AND ONLY THE HOST MAY REFUSE. The first half keeps a
            // listen host out of a path that would roll back authoritative state; the second is
            // the rule every "play this" handler in this file carries (`FromHost`).
            if (NetAuthority.IsHost || !FromHost(senderClientId)) return;

            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out int abilitySlot);

            if (!ValidSlot(slot) || abilitySlot < 0 || abilitySlot > 2) return;

            // ⚠️ ONLY THIS PEER'S OWN SEAT. `RollBackPredictedCast` checks the same thing from
            // the other end; a refusal naming somebody else's seat is a message this peer has no
            // business acting on, and the other three kits are replicas that never predicted.
            if (slot != NetAuthority.LocalSlot) return;

            Unit(slot)?.AbilitySystem?.RollBackPredictedCast(
                (Abilities.HeroAbilitySystem.Slot)abilitySlot);
        }

        /// <summary>One client mash press; the host decides which active state it answers.</summary>
        public void RequestMashServerRpc(int claimedSlot)
        {
            if (_nm == null || _nm.CustomMessagingManager == null) return;
            if (NetAuthority.IsHost) return;

            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(claimedSlot);
            _nm.CustomMessagingManager.SendNamedMessage("ReqMash", NetworkManager.ServerClientId, writer);
        }

        private void OnReqMashMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int claimedSlot);
            if (!SenderOwnsClaimedSeat(senderClientId, claimedSlot, out var unit)) return;

            if (unit.IsTripped) unit.MashRecover();
            else if (unit.StunElement != StunElement.None) unit.MashOutOfStun();

            SyncUnitTransformClientRpc(claimedSlot, unit.transform.position,
                                       unit.transform.eulerAngles.y, unit.Velocity);
        }

        /// <summary>Replicates a throw wind-up, which is counterplay rather than decoration.</summary>
        public void SetThrowCharge(int claimedSlot, bool active)
        {
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            if (!NetAuthority.IsHost)
            {
                using var ask = new FastBufferWriter(16, Allocator.Temp);
                ask.WriteValueSafe(claimedSlot);
                ask.WriteValueSafe(active);
                _nm.CustomMessagingManager.SendNamedMessage("ReqThrowCharge", NetworkManager.ServerClientId, ask);
                return;
            }

            BroadcastThrowCharge(claimedSlot, active, null);
        }

        private void OnReqThrowChargeMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int claimedSlot);
            reader.ReadValueSafe(out bool active);
            if (!SenderOwnsClaimedSeat(senderClientId, claimedSlot, out _)) return;
            BroadcastThrowCharge(claimedSlot, active, senderClientId);
        }

        private void BroadcastThrowCharge(int slot, bool active, ulong? except)
        {
            foreach (ulong clientId in _nm.ConnectedClientsIds)
            {
                if (clientId == _nm.LocalClientId || (except.HasValue && clientId == except.Value))
                    continue;
                using var writer = new FastBufferWriter(16, Allocator.Temp);
                writer.WriteValueSafe(slot);
                writer.WriteValueSafe(active);
                _nm.CustomMessagingManager.SendNamedMessage("ThrowCharge", clientId, writer);
            }
        }

        private void OnThrowChargeMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (senderClientId != NetworkManager.ServerClientId) return;
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out bool active);
            Unit(slot)?.GetComponent<Carrier>()?.ApplyObservedCharge(active);
        }

        /// <summary>
        /// Street Hype for one seat, sent to the one peer playing it.
        ///
        /// ⚠️⚠️ IT IS SENT TO ONE PEER, NOT BROADCAST, and that is not an optimisation. Hype is a
        /// personal quantity: `Hud.ApplyStyle` refuses any slot that is not the local one, so a
        /// broadcast would be three messages that every recipient throws away. See
        /// `Hud.ReportStyle` for why this exists at all, which is that Classic's entire
        /// bottom-of-screen identity was host-only.
        /// </summary>
        public void BroadcastStyle(int slot, float amount, string callout)
        {
            if (!NetAuthority.IsHost || _nm == null || _nm.CustomMessagingManager == null) return;
            if (!ValidSlot(slot)) return;

            var peer = NetSession.Instance?.Lobby?.PeerInSeat(slot);
            if (peer == null) return;

            var clientId = (ulong)peer.PeerId;
            if (clientId == _nm.LocalClientId) return;
            if (!_nm.ConnectedClients.ContainsKey(clientId)) return;

            using var writer = new FastBufferWriter(96, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(amount);
            writer.WriteValueSafe(callout ?? "");
            _nm.CustomMessagingManager.SendNamedMessage("PlayStyle", clientId, writer);
        }

        /// <summary>
        /// A point was awarded. Broadcast so every peer can react to it.
        ///
        /// ⚠️⚠️ IT CARRIES THE KIND, NOT THE POINTS, AND NOT THE TOTAL. The totals are already
        /// replicated by `SyncWorld` and `MatchDirector.ApplySnapshot` sets them from the host's
        /// own numbers, so sending a value here would give a client two sources for one fact.
        /// What a client could not obtain was WHICH EVENT happened, and both the toast and the
        /// sting read exactly that: `MatchRules.PointsFor(e)` and the event's own label.
        ///
        /// ⚠️ BROADCAST RATHER THAN SENT TO THE SEAT, unlike `BroadcastStyle` directly above.
        /// Street Hype is a personal quantity and `Hud.ApplyStyle` refuses a slot that is not the
        /// local one; a score is the MATCH reacting. `Hud.OnScored` plays the sting for anybody's
        /// award on purpose and only the TOAST is filtered to the local seat, which is the
        /// original's rule and is written out at that call site.
        ///
        /// ⚠️ `DefenseTick` AND THE TWO PENALTIES ARE SENT TOO, at roughly one a second while
        /// they apply. `Hud.OnScored` discards the first outright and gives the other two a sound
        /// and no words, so this is a few bytes a second to keep the event faithful rather than
        /// to teach the receiver a rule the sender should not be making for it.
        /// </summary>
        public void BroadcastScore(int slot, Core.ScoreEvent e)
        {
            if (!NetAuthority.IsHost || _nm == null || _nm.CustomMessagingManager == null) return;
            if (!ValidSlot(slot)) return;

            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe((int)e);
            _nm.CustomMessagingManager.SendNamedMessageToAll("Score", writer);
        }

        /// <summary>
        /// PHASE 12: the LAST TSINELAS STANDING stock table, host to every peer.
        /// `docs/TODO.md` § 130.13, and it is why `NetSession.ProtocolVersion` is 22.
        ///
        /// ⚠️⚠️ THE WHOLE TABLE TRAVELS, NOT THE DECREMENT, AND THAT IS THE SAME ARGUMENT
        /// `SyncWorld` MAKES ABOUT THE SCORE. A "player 2 lost one" message is a delta, and a
        /// delta that is dropped or reordered leaves a peer permanently one tsinelas out with no
        /// way to notice; four small integers sent on the handful of frames a tag happens costs
        /// less than the code to detect that drift. `BroadcastScore` next door sends the KIND
        /// rather than the delta for the opposite reason, and both are the same rule: send the
        /// thing the receiver cannot reconstruct.
        ///
        /// ⚠️ IT IS SENT ON THE WHISTLE AS WELL AS ON EVERY TAG, so a peer that joined mid-round
        /// or missed a packet is corrected at the start of the next round rather than staying
        /// wrong until the match ends.
        ///
        /// ⚠️ THE COUNT IS WRITTEN FIRST AND THE READER TRUSTS IT ONLY AS FAR AS `Balance
        /// .PlayerCount`. This is a `FastBufferWriter` message read field by field in order, which
        /// is the trap `ProtocolVersion` 16, 17 and § 89.5 all record; a length-prefixed loop that
        /// clamped nothing would let a malformed packet read off the end of the buffer.
        /// </summary>
        public void BroadcastTsinelas(int[] stocks, int defenderSlot)
        {
            if (!NetAuthority.IsHost || _nm == null || _nm.CustomMessagingManager == null) return;
            if (stocks == null) return;

            int count = Mathf.Min(stocks.Length, Core.Balance.PlayerCount);

            using var writer = new FastBufferWriter(12 + (count * 4), Allocator.Temp);
            writer.WriteValueSafe(defenderSlot);
            writer.WriteValueSafe(count);
            for (int i = 0; i < count; i++) writer.WriteValueSafe(stocks[i]);

            _nm.CustomMessagingManager.SendNamedMessageToAll("Tsinelas", writer);
        }

        /// <summary>
        /// Sends the finished match record to every peer, once, at the end of the match.
        ///
        /// ⚠️⚠️ THE HOST IS THE ONLY MACHINE THAT COUNTED THE MATCH, SO WITHOUT THIS A CLIENT
        /// HAS NO CAREER AT ALL. `MatchStatsCollector` is host-gated for the same reason
        /// `AddScore` is, which leaves the other three players with nothing to show on the
        /// end-of-match summary and nothing to submit for their own profile. This is the one
        /// message that carries a whole match, and it is one message per match.
        ///
        /// ⚠️ IT IS SENT AND NOT REQUESTED. A client that had to ask would have to know when to
        /// ask, and the moment it knows the match ended is a snapshot edge that can arrive
        /// before the host has finished writing the record.
        ///
        /// ⚠️ EVERY PEER RECEIVES THE WHOLE RECORD, INCLUDING THE OTHER THREE LINES, and that is
        /// deliberate: `FUTURE.md` § 2.1 item 6 draws the full four-player scoreboard in the
        /// match detail. It carries no account ids for anybody a peer does not already see in
        /// the lobby, because the ids in it are the same durable tokens seating already uses.
        /// </summary>
        public void BroadcastMatchRecord(Core.MatchRecord record)
        {
            if (!NetAuthority.IsHost || _nm == null || _nm.CustomMessagingManager == null) return;
            if (record == null) return;

            string json = JsonUtility.ToJson(record);
            if (string.IsNullOrEmpty(json)) return;

            // ⚠️⚠️ SIZED FROM THE STRING RATHER THAN FROM A CONSTANT, AND THE BUFFER IS NOT THE
            // SAME QUESTION AS THE PACKET. `FastBufferWriter` needs room for the whole message
            // in memory, which is what this is; splitting it across the wire is `RecordDelivery`
            // and is decided separately. A fixed buffer picked by eye is the shape that starts
            // truncating silently the day somebody adds a stat to `PlayerMatchStats`.
            using var writer = new FastBufferWriter(
                FastBufferWriter.GetWriteSize(json) + 64, Allocator.Temp);
            writer.WriteValueSafe(json);
            _nm.CustomMessagingManager.SendNamedMessageToAll("MatchRecord", writer, RecordDelivery);
        }

        private void OnMatchRecordMsg(ulong senderClientId, FastBufferReader reader)
        {
            // ⚠️ THE HOST IS ITS OWN CLIENT AND `SendNamedMessageToAll` LOOPS BACK TO IT. It
            // adopted the record before sending; taking it again here would submit the same
            // match twice. `ProfileRules.Apply` would refuse the duplicate, but a wasted
            // endpoint call per match is a cost with no upside. See § THE LOOPBACK.
            if (NetAuthority.IsHost) return;
            if (!FromHost(senderClientId)) return;

            reader.ReadValueSafe(out string json);
            if (string.IsNullOrWhiteSpace(json)) return;

            var record = JsonUtility.FromJson<Core.MatchRecord>(json);
            if (record == null) return;

            // ⚠️ NORMALISED ON ARRIVAL, BECAUSE THIS ARRIVED FROM ANOTHER MACHINE. The host
            // already normalised it, and that is exactly why a peer cannot assume it did: the
            // sender is the one party this client has no reason to trust about its own career.
            Core.MatchRecordRules.Normalise(record);
            GameServices.Stats?.Adopt(record);
        }

        private void OnScoreMsg(ulong senderClientId, FastBufferReader reader)
        {
            // ⚠️ THE HOST IS ITS OWN CLIENT AND `SendNamedMessageToAll` LOOPS BACK TO IT. It
            // raised `Scored` itself one line before sending; replaying it here would double
            // every toast and every sting on the host. See § THE LOOPBACK.
            if (NetAuthority.IsHost) return;
            if (!FromHost(senderClientId)) return;

            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out int rawEvent);

            if (!ValidSlot(slot)) return;

            // ⚠️ AN UNKNOWN EVENT IS DROPPED RATHER THAN CAST. A build that speaks a newer
            // `ScoreEvent` should be refused by the protocol check long before this, but a cast
            // of an out-of-range int would reach `MatchRules.PointsFor` as a value it has no case
            // for and pay whatever its default is.
            if (!System.Enum.IsDefined(typeof(Core.ScoreEvent), rawEvent)) return;

            GameServices.Match?.ApplyNetworkScoreEvent(slot, (Core.ScoreEvent)rawEvent);
        }

        /// <summary>
        /// ⚠️ THE HOST IGNORES ITS OWN LOOPBACK, exactly as `OnScoreMsg` above does and for the
        /// same reason: `SendNamedMessageToAll` comes back to the sender, and re-applying the
        /// table the host just computed would raise `StocksChanged` twice per tag on one machine.
        /// </summary>
        private void OnTsinelasMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (NetAuthority.IsHost) return;
            if (!FromHost(senderClientId)) return;

            // ⚠️⚠️ THE TAYA'S SLOT TRAVELS WITH THE TABLE AND IS NOT INFERRED ON THIS PEER.
            // `docs/TODO.md` § 130.13. The taya's stock is 0 by definition, so a receiver that
            // worked out the slot for itself and got it wrong would read the real taya as an
            // eliminated attacker and switch their body off. `MatchDirector.DefenderSlot` is
            // derived from a round number that arrives in a DIFFERENT message at 5 Hz, so on the
            // whistle there is a window where this packet has the new round's stocks and the peer
            // still has the old round's number. Four bytes removes the race outright.
            reader.ReadValueSafe(out int defenderSlot);
            reader.ReadValueSafe(out int count);
            if (count < 0 || count > Core.Balance.PlayerCount) return;

            var stocks = new int[Core.Balance.PlayerCount];
            for (int i = 0; i < count; i++)
            {
                reader.ReadValueSafe(out int stock);
                stocks[i] = stock;
            }

            GameServices.Tsinelas?.ApplyNetworkStocks(stocks, defenderSlot);
        }

        private void OnPlayStyleMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!FromHost(senderClientId)) return;

            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out float amount);
            reader.ReadValueSafe(out string callout);
            if (!ValidSlot(slot) || !Finite(amount)) return;

            UI.Hud.ApplyStyle(slot, Mathf.Clamp(amount, 0.0f, 100.0f), callout);
        }

        /// <summary>The transport this seat is played on, or null for a bot or an empty chair.</summary>
        private ulong? SeatOwnerClientId(int slot)
        {
            var peer = NetSession.Instance?.Lobby?.PeerInSeat(slot);
            return peer != null ? (ulong?)peer.PeerId : null;
        }

        /// <summary>
        /// Announce an action to everybody EXCEPT the peer playing that seat.
        ///
        /// ⚠️ THE OWNER HAS ALREADY PLAYED IT. Every verb a client can press is predicted on the
        /// presser's own screen so the arm answers the key immediately; sending it back would
        /// restart the clip a round trip later, which reads as a stutter on precisely the player
        /// who is playing well. It is the same rule `HostRelayCue` states for a sound.
        /// </summary>
        public void BroadcastActionExceptOwner(int slot, string action)
            => BroadcastAction(slot, action, SeatOwnerClientId(slot));

        /// <summary>Host-side third-person action announcement for ordinary combat verbs.</summary>
        public void BroadcastAction(int slot, string action, ulong? exceptClientId = null)
        {
            if (!NetAuthority.IsHost || _nm == null || _nm.CustomMessagingManager == null) return;

            foreach (ulong clientId in _nm.ConnectedClientsIds)
            {
                if (clientId == _nm.LocalClientId ||
                    (exceptClientId.HasValue && clientId == exceptClientId.Value))
                    continue;
                using var writer = new FastBufferWriter(64, Allocator.Temp);
                writer.WriteValueSafe(slot);
                writer.WriteValueSafe(action ?? "");
                _nm.CustomMessagingManager.SendNamedMessage("PlayAction", clientId, writer);
            }
        }

        private void OnPlayActionMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (senderClientId != NetworkManager.ServerClientId) return;
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out string action);
            Unit(slot)?.GetComponentInChildren<Visual.CharacterAnimator>()?.PlayAction(action);
        }

        // -------------------------------------------------------------------
        // LOBBY SETUP SYNCHRONIZATION (N5)
        // -------------------------------------------------------------------

        public static event Action<int> OnMapChanged;
        public static event Action<int> OnDifficultyChanged;
        public static event Action<int[]> OnLobbyPicksSynced;
        public static event Action<LobbySeatInfo[]> OnLobbyRosterSynced;
        public static event Action OnMatchStarted;

        public void HostStartMatch()
        {
            if (!NetAuthority.IsHost) return;

            // ⚠⚠ THE LOBBY IS TOLD THE MATCH IS RUNNING, AND IT NEVER USED TO BE.
            // `LobbySession.MatchInProgress` is the switch behind three separate rules: `Depart`
            // only HOLDS a dropped player's chair while it is set, `RuleOnArrival` only answers
            // Spectate rather than Refuse while it is set, and `TryTakeSeat` refuses a seat change
            // once it is set. Left false, a player who dropped mid-match lost their seat and their
            // score to the next arrival, and anyone joining a running match was turned away.
            var lobby = NetSession.Instance?.Lobby;

            // The rules rotate one taya across four fixed seats. With filler bots disabled an
            // empty chair cannot defend its round, so the host must wait for four people rather
            // than starting a match with an inert body. The lobby button carries the same gate;
            // this check is the authoritative backstop for every other caller.
            if (!AIController.BotsEnabled &&
                (lobby == null || lobby.OccupiedSeatCount() < Balance.PlayerCount))
            {
                Debug.LogWarning("[Lobby] start refused: bots are off and not all four seats are occupied.");
                return;
            }

            lobby?.StartMatch();
            _lobbyReady.Clear();

            // ⚠️ THE HOST STOPS TALKING ABOUT A MATCH IT HAS NOT STARTED YET. See
            // `_loadingOwnArena`: everything below tells four peers to load an arena, and the
            // host then loads its own while `FixedUpdate` keeps writing `SyncWorld` at 5 Hz
            // carrying a `MatchInProgress` that is still false. `docs/TODO.md` § 82.3.
            _loadingOwnArena = true;

            // ⚠️⚠️ THE MODE GOES FIRST, BEFORE `StartMatch`, AND THE ORDER IS THE WHOLE POINT.
            // `OnStartMatchMsg` calls `UI.SceneFlow.StartMatch()`, which loads the arena scene
            // and builds every seat through `MatchInstaller`, and `MatchInstaller` reads
            // `SceneFlow.SelectedMode` to choose the roster AND to decide whether to install a
            // `HeroAbilitySystem` at all. A mode that arrives one message later arrives after the
            // bodies exist, which is exactly the *"other ppl not seeing the character"* fault:
            // the client builds the whole match in whatever mode its own menu happened to hold.
            // See § THE GAME MODE, WHICH WAS NEVER REPLICATED AT ALL.
            SyncModeClientRpc((int)UI.SceneFlow.SelectedMode);

            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(16, Allocator.Temp);
                _nm.CustomMessagingManager.SendNamedMessageToAll("StartMatch", writer);
            }
            OnMatchStarted?.Invoke();
        }

        private void OnStartMatchMsg(ulong senderClientId, FastBufferReader reader)
        {
            // ⚠️ THE HOST IS ITS OWN CLIENT AND `SendNamedMessageToAll` LOOPS BACK TO IT.
            // Netcode invokes the handler locally for the listen host, so every broadcast the
            // host sent was also applied ON the host, a second time, over authoritative state it
            // had just produced. See § THE LOOPBACK.
            if (NetAuthority.IsHost) return;

            OnMatchStarted?.Invoke();
            UI.SceneFlow.StartMatch();
        }

        public void SelectMapServerRpc(int mapIndex)
        {
            if (NetAuthority.IsHost)
            {
                SyncMapClientRpc(mapIndex);
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(mapIndex);
            _nm.CustomMessagingManager.SendNamedMessage("SelectMap", NetworkManager.ServerClientId, writer);
        }

        private void OnSelectMapMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            if (!SenderMayConfigureLobby(senderClientId)) return;
            reader.ReadValueSafe(out int mapIndex);
            SyncMapClientRpc(mapIndex);
        }

        private void SyncMapClientRpc(int mapIndex)
        {
            if (!NetAuthority.IsHost) return;
            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(16, Allocator.Temp);
                writer.WriteValueSafe(mapIndex);
                _nm.CustomMessagingManager.SendNamedMessageToAll("SyncMap", writer);
            }
            OnMapChanged?.Invoke(mapIndex);
        }

        private void OnSyncMapMsg(ulong senderClientId, FastBufferReader reader)
        {
            // ⚠️ THE HOST IS ITS OWN CLIENT AND `SendNamedMessageToAll` LOOPS BACK TO IT.
            // Netcode invokes the handler locally for the listen host, so every broadcast the
            // host sent was also applied ON the host, a second time, over authoritative state it
            // had just produced. See § THE LOOPBACK.
            if (NetAuthority.IsHost) return;

            reader.ReadValueSafe(out int mapIndex);
            OnMapChanged?.Invoke(mapIndex);
        }

        // -------------------------------------------------------------------
        // § THE GAME MODE, WHICH WAS NEVER REPLICATED AT ALL
        //
        // ⚠️⚠️ THIS IS THE ROOT OF *"its heavily broken with other ppl not seeing the
        // character"* (🧑, 2026-08-27) AND IT IS BIGGER THAN THE SKINS. `UI.SceneFlow.SelectedMode`
        // is a plain static set by whoever last touched the mode toggle in `ConvertedMatchSetup`.
        // The map is replicated (`SyncMap`), the difficulty is replicated (`SyncDiff`), the picks
        // are replicated, the seats are replicated. **The mode is not, and it decides more than
        // any of them.**
        //
        // ⚠️⚠️ A CLIENT WHOSE MENU LAST SAID CLASSIC, JOINING A HERO STRIKE MATCH, BUILDS A
        // DIFFERENT GAME. `MatchInstaller` reads `SelectedMode` in at least three places:
        //
        //   * `_book.PersonArt(motor.CharacterIndex, SceneFlow.SelectedMode)` resolves the model
        //     against `Roster.GetPeople(mode)`, which is the twelve street characters in Classic
        //     and the five heroes in Hero Strike. **A hero index looked up in the street cast is
        //     a different person**, and past the end of the list `Resolve` falls back to `art[0]`
        //     so several seats collapse onto the same wrong body. That is *"they see the older
        //     version of the skin"* precisely: the older roster.
        //   * `if (SceneFlow.SelectedMode == GameMode.HeroStrike)` gates installing
        //     `HeroAbilitySystem` at all, so a client in the wrong mode gives four seats no kit.
        //   * `CharacterMotor.Mode` feeds `Roster.GetPeople(Mode)` for the nameplate, so the
        //     labels disagree with the bodies.
        //
        // ⚠️ IT IS SENT THE SAME WAY THE MAP IS, AND DELIBERATELY NOT AS PART OF THE PICK TABLE.
        // The mode has to be true BEFORE any seat is built, and the pick table arrives after the
        // arena exists. Same shape as `SelectMap` so there is one idiom for "a lobby setting the
        // host owns": client asks, host decides, host tells everybody.
        // -------------------------------------------------------------------

        public static event Action<int> OnModeChanged;

        public void SelectModeServerRpc(int mode)
        {
            if (NetAuthority.IsHost)
            {
                SyncModeClientRpc(mode);
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(mode);
            _nm.CustomMessagingManager.SendNamedMessage("SelectMode", NetworkManager.ServerClientId, writer);
        }

        private void OnSelectModeMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            if (!SenderMayConfigureLobby(senderClientId)) return;
            reader.ReadValueSafe(out int mode);
            SyncModeClientRpc(mode);
        }

        public void SyncModeClientRpc(int mode)
        {
            if (!NetAuthority.IsHost) return;
            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(16, Allocator.Temp);
                writer.WriteValueSafe(mode);
                _nm.CustomMessagingManager.SendNamedMessageToAll("SyncMode", writer);
            }
            ApplyMode(mode);
        }

        private void OnSyncModeMsg(ulong senderClientId, FastBufferReader reader)
        {
            // ⚠️ THE HOST IS ITS OWN CLIENT AND `SendNamedMessageToAll` LOOPS BACK TO IT.
            // Netcode invokes the handler locally for the listen host, so every broadcast the
            // host sent was also applied ON the host, a second time, over authoritative state it
            // had just produced. See § THE LOOPBACK.
            if (NetAuthority.IsHost) return;

            reader.ReadValueSafe(out int mode);
            ApplyMode(mode);
        }

        /// <summary>
        /// ⚠️ IT WRITES `SceneFlow.SelectedMode` DIRECTLY RATHER THAN RAISING AN EVENT AND HOPING
        /// SOMEBODY LISTENS. Every reader of the mode reads that static, so the static is the
        /// thing that has to be right; the event is for screens that want to redraw.
        /// </summary>
        private static void ApplyMode(int mode)
        {
            var wanted = mode == (int)GameMode.HeroStrike ? GameMode.HeroStrike : GameMode.Classic;
            UI.SceneFlow.SelectedMode = wanted;

            // ⚠️ THE LIVE SEATS ARE CORRECTED TOO, because a mode message can arrive after the
            // arena has been built: on a late join the host sends this from `HostSyncPeer` when
            // the client already has four bodies standing in the street. `CharacterMotor.Mode`
            // feeds the roster lookup behind the nameplate, so leaving it stale is a screen that
            // disagrees with the models.
            var round = GameServices.Round;
            if (round != null)
            {
                foreach (var p in round.Players)
                    if (p != null) p.Mode = wanted;
            }

            OnModeChanged?.Invoke(mode);
        }

        public void SelectDifficultyServerRpc(int difficulty)
        {
            if (NetAuthority.IsHost)
            {
                SyncDifficultyClientRpc(difficulty);
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(16, Allocator.Temp);
            writer.WriteValueSafe(difficulty);
            _nm.CustomMessagingManager.SendNamedMessage("SelectDiff", NetworkManager.ServerClientId, writer);
        }

        private void OnSelectDiffMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            if (!SenderMayConfigureLobby(senderClientId)) return;
            reader.ReadValueSafe(out int diff);
            SyncDifficultyClientRpc(diff);
        }

        private void SyncDifficultyClientRpc(int difficulty)
        {
            if (!NetAuthority.IsHost) return;
            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(16, Allocator.Temp);
                writer.WriteValueSafe(difficulty);
                _nm.CustomMessagingManager.SendNamedMessageToAll("SyncDiff", writer);
            }
            OnDifficultyChanged?.Invoke(difficulty);
        }

        /// <summary>
        /// PHASE 12: the match FORMAT, which rides beside the mode rather than replacing it.
        ///
        /// ⚠⚠ IT IS ITS OWN MESSAGE AND NOT A FIELD ON `SelectMode`, and the reason is
        /// `LobbySeatInfo`'s: **a field added to one half of a named message is not an error, it
        /// is silently misread bytes** (`docs/TODO.md` § 38.6, and `tools/audit_wire_payloads.py`
        /// exists because of it). Widening an existing message costs a protocol break either way,
        /// and a new name makes a build that does not know the format ignore it rather than read
        /// a mode out of the wrong four bytes.
        ///
        /// ⚠️ AND `ProtocolVersion` STILL MOVES, 20 -> 21, because a host on 21 running LAST
        /// TSINELAS STANDING and a client on 20 playing standard rules would be two different
        /// games sharing a scoreboard. `NetSession.ProtocolVersion`: both machines rebuild off
        /// the same branch or they refuse each other at approval, by design.
        /// </summary>
        public static event Action<int> OnFormatChanged;

        /// <summary>
        /// The whole custom rule set, agreed by the room.
        ///
        /// ⚠️⚠️ IT REPLACES THE FORMAT-ONLY PAIR RATHER THAN SITTING BESIDE IT, AND THAT IS
        /// `docs/TODO.md` § 38.5's RULE. That entry found **three dead protocols and one verb that
        /// had never travelled at all**, and the cause each time was a second path added beside a
        /// first one. The format is a FIELD of `CustomRules`, so a message that carried it alone
        /// would be a second, narrower statement of the same fact, and the two would disagree the
        /// first time somebody changed the rounds and the format in one press.
        ///
        /// ⚠️⚠️ THE PAYLOAD IS `CustomGameRules.ToWire`, WHICH ALREADY EXISTS FOR THIS.
        /// Its own header says so: *"The compact wire form, so a lobby advert and the approval
        /// hello can carry a rule set without a second protocol."* Fields are appended and never
        /// inserted, and a SHORT string is read as defaults, so the record can grow without this
        /// message changing shape again.
        ///
        /// ⚠️ THE PASSWORD IS NOT IN IT AND CANNOT BE. `ToWire` drops it and `Parse` clears it,
        /// deliberately: a lobby advert is readable by everybody in the pool, so a password in it
        /// is a lock with the key taped to the door. The host holds it and compares what a joiner
        /// sends.
        ///
        /// ⚠️⚠️ AND IT IS WHAT MOVED `NetSession.ProtocolVersion` TO 23. A peer that has
        /// never heard of this message plays the SHIPPED round count and the SHIPPED clock while
        /// the host plays the custom ones, which is *"two different games sharing one
        /// scoreboard"*, the exact sentence that constant's own note uses. `CLAUDE.md` § 4a's
        /// consequence follows: **the Windows player and the .apk are rebuilt from one commit and
        /// shipped together**, or they refuse each other correctly and it reads as a bug.
        /// </summary>
        public static event Action<string> OnRulesChanged;

        /// <summary>
        /// ⚠️ KEPT AS THE NAME EVERY CALLER ALREADY USES, and it now sends the whole set. The
        /// lobby's RULES dropdown changes one field of a rule set that has eight others, and it
        /// has no business knowing that the transport takes a string: it hands over the format it
        /// picked and this builds the message from the live rule set.
        /// </summary>
        public void SelectFormatServerRpc(int format)
        {
            var rules = UI.SceneFlow.SelectedRules.Clone();
            rules.Format = format >= 0 && format <= (int)MatchFormat.Mirror
                ? (MatchFormat)format : MatchFormat.Standard;

            SelectRulesServerRpc(Core.CustomGameRules.ToWire(rules));
        }

        public void SelectRulesServerRpc(string wire)
        {
            if (NetAuthority.IsHost)
            {
                SyncRulesClientRpc(wire);
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;

            // ⚠️ 256 RATHER THAN 16. The old payload was one int; this one is nine numbers and
            // eight separators, about forty characters today and room to grow. A writer sized to
            // the message it happens to carry is a writer that throws the day a field is appended.
            using var writer = new FastBufferWriter(256, Allocator.Temp);
            writer.WriteValueSafe(wire ?? "");
            _nm.CustomMessagingManager.SendNamedMessage("SelectRules", NetworkManager.ServerClientId, writer);
        }

        private void OnSelectRulesMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            if (!SenderMayConfigureLobby(senderClientId)) return;
            reader.ReadValueSafe(out string wire);
            SyncRulesClientRpc(wire);
        }

        private void SyncRulesClientRpc(string wire)
        {
            if (!NetAuthority.IsHost) return;

            // ⚠️⚠️ THE HOST CLAMPS BEFORE IT BROADCASTS, so what the room agrees on is
            // already inside every bound. `CustomGameRules`' header: *"EVERY BOUND IN HERE IS A
            // BOUND ON THE HOST, NOT A SUGGESTION TO IT ... each one is clamped on the way in and
            // again on the way out of the wire."* This is the way out.
            var clamped = Core.CustomGameRules.Parse(wire, UI.SceneFlow.SelectedMode);
            string safe = Core.CustomGameRules.ToWire(clamped);

            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(256, Allocator.Temp);
                writer.WriteValueSafe(safe);
                _nm.CustomMessagingManager.SendNamedMessageToAll("SyncRules", writer);
            }

            OnRulesChanged?.Invoke(safe);
            OnFormatChanged?.Invoke((int)clamped.Format);
        }

        private void OnSyncRulesMsg(ulong senderClientId, FastBufferReader reader)
        {
            // ⚠️ See `OnSyncDiffMsg`: the host is its own client and a broadcast loops back.
            if (NetAuthority.IsHost) return;

            reader.ReadValueSafe(out string wire);

            // ⚠️⚠️ THE CLIENT CLAMPS TOO, AND THAT IS NOT PARANOIA ABOUT THE HOST. It is
            // the same argument `NetSession.ApproveConnection` makes about the account id: the
            // host is a PLAYER in this room, on somebody's laptop, and `docs/VISION.md` § 4's
            // *"the host decides everything that scores"* is a statement about authority rather
            // than about trust. A 900 second round arriving from a modified host would otherwise
            // be drawn on this machine's clock.
            var clamped = Core.CustomGameRules.Parse(wire, UI.SceneFlow.SelectedMode);

            OnRulesChanged?.Invoke(Core.CustomGameRules.ToWire(clamped));
            OnFormatChanged?.Invoke((int)clamped.Format);
        }

        private void OnSyncDiffMsg(ulong senderClientId, FastBufferReader reader)
        {
            // ⚠️ THE HOST IS ITS OWN CLIENT AND `SendNamedMessageToAll` LOOPS BACK TO IT.
            // Netcode invokes the handler locally for the listen host, so every broadcast the
            // host sent was also applied ON the host, a second time, over authoritative state it
            // had just produced. See § THE LOOPBACK.
            if (NetAuthority.IsHost) return;

            reader.ReadValueSafe(out int diff);
            OnDifficultyChanged?.Invoke(diff);
        }

        /// <summary>
        /// ⚠️⚠️ THE COSMETICS CLAIM RIDES THIS MESSAGE AS WELL AS `Identify`, AND THAT IS NOT
        /// REDUNDANT. The palette is remembered PER CHARACTER (`FUTURE.md` PHASE 5's favourite
        /// loadout), so changing character changes what this peer is wearing. A claim sent only
        /// at join would leave everybody dressed in the palette of whoever they were holding when
        /// they walked into the lobby, which is the one thing a per-character loadout must not do.
        /// </summary>
        public void SelectLobbyPickServerRpc(int character, int can, int slipper)
        {
            string cosmetics = LocalCosmetics.Encoded(character);

            // ⚠️⚠️ THE CUSTOM CHARACTER RIDES THIS TOO, AND NOT ONLY `Identify`, FOR THE SAME
            // REASON THE CLAIM DOES. The creator's KEEP AND USE button and the MAKE YOUR OWN row
            // on character select both change what this player is bringing while they are already
            // in a lobby. A frame sent only at join would leave every peer wearing whoever they
            // walked in as, which is the one thing a per-character choice must not do.
            string custom = LocalCosmetics.CustomCharacter();
            string build = LocalCosmetics.HeroBuild(character, custom);

            if (NetAuthority.IsHost)
            {
                var lobby = NetSession.Instance?.Lobby;
                if (lobby != null)
                {
                    // ⚠️ HOST'S OWN PEER ID COMES FROM LOCAL CLIENT ID, NEVER FROM LOCAL SEAT.
                    // LocalSlot is 0-3 (a seat) while _peers is keyed by transport client ID.
                    int hostPeerId = _nm != null ? (int)_nm.LocalClientId : 0;
                    lobby.SetPicks(hostPeerId, character, can, slipper);

                    // ⚠️ THE HOST AUTHORISES ITS OWN CLAIM TOO, RATHER THAN TRUSTING ITSELF.
                    // A host wearing a title it has not earned would be the one seat in the room
                    // nobody checked, and `docs/TODO.md` § 94.1's lesson is that the copy nobody
                    // checks is the copy that is wrong.
                    HostAuthoriseCosmetics(hostPeerId, cosmetics, character, custom, build);
                    BroadcastLobbyPicks();
                }
                return;
            }

            if (_nm == null || _nm.CustomMessagingManager == null) return;
            using var writer = new FastBufferWriter(1024, Allocator.Temp);
            writer.WriteValueSafe(0);
            writer.WriteValueSafe(character);
            writer.WriteValueSafe(can);
            writer.WriteValueSafe(slipper);
            writer.WriteValueSafe(cosmetics ?? "");
            writer.WriteValueSafe(custom ?? "");
            writer.WriteValueSafe(build ?? "");
            _nm.CustomMessagingManager.SendNamedMessage("SelectLobbyPick", NetworkManager.ServerClientId, writer);
        }

        public void SelectLobbyPickServerRpc(int peerId, int character, int can, int slipper)
            => SelectLobbyPickServerRpc(character, can, slipper);

        private void OnSelectLobbyPickMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;
            reader.ReadValueSafe(out int peerId);
            reader.ReadValueSafe(out int character);
            reader.ReadValueSafe(out int can);
            reader.ReadValueSafe(out int slipper);
            reader.ReadValueSafe(out string cosmetics);

            // ⚠️ SAME LENGTH GUARD AS `OnIdentifyMsg`, AND FOR THE SAME REASON: a handler that
            // throws past the end of a payload drops every message queued behind it.
            string custom = "";
            if (reader.Length > reader.Position) reader.ReadValueSafe(out custom);
            string build = "";
            if (reader.Length > reader.Position) reader.ReadValueSafe(out build);

            var lobby = NetSession.Instance?.Lobby;
            if (lobby != null)
            {
                lobby.SetPicks((int)senderClientId, character, can, slipper);
                HostAuthoriseCosmetics((int)senderClientId, cosmetics, character, custom, build);
                BroadcastLobbyPicks();
            }
        }

        public void BroadcastLobbyPicks()
        {
            if (!NetAuthority.IsHost) return;
            var lobby = NetSession.Instance?.Lobby;
            if (lobby == null) return;

            var seats = new LobbySeatInfo[Balance.PlayerCount];
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var peer = lobby.PeerInSeat(slot);
                if (peer != null)
                {
                    seats[slot] = new LobbySeatInfo
                    {
                        Seat = slot,
                        PeerId = peer.PeerId,
                        Name = peer.Name ?? "",
                        Occupied = true,
                        Spectator = peer.Spectator,
                        CharacterPick = peer.CharacterPick,
                        CanPick = peer.CanPick,
                        SlipperPick = peer.SlipperPick,
                        Ready = _lobbyReady.Contains(peer.PeerId),

                        // ⚠️⚠️ THE AUTHORISED BANNER, NEVER THE CLAIM. `HostAuthoriseCosmetics`
                        // has already run `BannerRules.Authorise` on whatever this peer sent, so
                        // what goes out to the room is a decision rather than a request. See
                        // `LobbySeatInfo.Banner`.
                        Banner = peer.Banner ?? new BannerSelection(),
                        Look = peer.Look ?? "",
                        Custom = peer.Custom ?? "",
                        Build = peer.Build ?? "",
                    };
                }
                else
                {
                    seats[slot] = new LobbySeatInfo
                    {
                        Seat = slot,
                        PeerId = -1,
                        Name = "",
                        Occupied = false,
                        Spectator = false,
                        CharacterPick = -1,
                        CanPick = -1,
                        SlipperPick = -1,
                        Ready = false,
                        Banner = new BannerSelection(),
                        Look = "",
                        Custom = "",
                        Build = "",
                    };
                }
                _replicatedSeats[slot] = seats[slot];
            }

            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(512, Allocator.Temp);
                writer.WriteValueSafe(Balance.PlayerCount);
                for (int i = 0; i < Balance.PlayerCount; i++)
                {
                    var s = seats[i];
                    writer.WriteValueSafe(s.Seat);
                    writer.WriteValueSafe(s.PeerId);
                    writer.WriteValueSafe(s.Name ?? "");
                    writer.WriteValueSafe(s.Occupied);
                    writer.WriteValueSafe(s.Spectator);
                    writer.WriteValueSafe(s.CharacterPick);
                    writer.WriteValueSafe(s.CanPick);
                    writer.WriteValueSafe(s.SlipperPick);
                    writer.WriteValueSafe(s.Ready);

                    // ⚠️⚠️ ONE FIELD PER SEAT, AND IT IS WHY THE PROTOCOL IS 17. `BannerCodec`
                    // encodes the four ids and the trackers into one string, for the reason
                    // `IdentifyServerRpc` gives: this loop and its reader are kept in step by
                    // hand, and five more fields per seat is twenty more chances to write them
                    // out of order. `audit_wire_payloads.py` checks one against the other.
                    writer.WriteValueSafe(BannerCodec.EncodeSelection(s.Banner));
                    writer.WriteValueSafe(s.Look ?? "");

                    // ⚠️ ONE MORE STRING PER SEAT AND IT IS WHY THE PROTOCOL IS 19. It is a
                    // whole custom character in a `C3` frame, already normalised by the host, so
                    // a client that receives it can build the seat without asking anything else.
                    // Empty is the roster case and is what three of the four seats usually carry.
                    writer.WriteValueSafe(s.Custom ?? "");
                    writer.WriteValueSafe(s.Build ?? "");
                }

                // ⚠️⚠️ THE GALLERY RIDES THE ROSTER, BECAUSE A SPECTATOR HAS NO SEAT AND SO NO
                // ROW IN THE TABLE ABOVE. 🧑 2026-08-29: *"make it so taht more than 4 ppl can
                // join, like up to 8 ppl can join but only the first 4 are players and last 4 are
                // spectators"*. Four people can now be in the room with nothing anywhere on a
                // client that says so — `LobbySeatInfo` is per SEAT by construction, and a client
                // cannot count them itself because `LobbySeatInfo`'s own header records that a
                // client's `LobbySession` is deliberately unpopulated.
                //
                // ⚠️ ON THIS MESSAGE RATHER THAN A FIFTH ONE. It already goes out on every seat
                // change, every ready press and every world snapshot, which is exactly when the
                // number can move. `docs/TODO.md` § 38.5 found three verbs with two protocols
                // each and the dead one being the maintained one; a message for one int that an
                // existing broadcast has a natural place for is how that starts.
                writer.WriteValueSafe(lobby.SpectatorCount());

                _nm.CustomMessagingManager.SendNamedMessageToAll("SyncLobbyPicks", writer);
            }

            var table = new int[Balance.PlayerCount * 4];
            for (int i = 0; i < table.Length; i++) table[i] = -1;
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                if (seats[slot].Occupied)
                {
                    table[slot * 4] = slot;
                    table[slot * 4 + 1] = seats[slot].CharacterPick;
                    table[slot * 4 + 2] = seats[slot].CanPick;
                    table[slot * 4 + 3] = seats[slot].SlipperPick;
                }
            }

            // ⚠️ THE HOST SETS ITS OWN, because `SendNamedMessageToAll` is not applied on the
            // sender (see § THE LOOPBACK) so `OnSyncLobbyPicksMsg` never runs here.
            SpectatorsWatching = lobby.SpectatorCount();

            OnLobbyPicksSynced?.Invoke(table);
            OnLobbyRosterSynced?.Invoke(seats);
        }

        /// <summary>
        /// How many people are in the room without a seat, as the host last said.
        ///
        /// ⚠️ REPLICATED RATHER THAN COUNTED, for the reason `LobbySeatInfo`'s header gives: a
        /// client's own `LobbySession` is deliberately not populated, so asking it is asking a
        /// table nobody fills in.
        /// </summary>
        public static int SpectatorsWatching { get; private set; }

        private void OnSyncLobbyPicksMsg(ulong senderClientId, FastBufferReader reader)
        {
            // ⚠️ THE HOST IS ITS OWN CLIENT AND `SendNamedMessageToAll` LOOPS BACK TO IT.
            // Netcode invokes the handler locally for the listen host, so every broadcast the
            // host sent was also applied ON the host, a second time, over authoritative state it
            // had just produced. See § THE LOOPBACK.
            if (NetAuthority.IsHost) return;

            reader.ReadValueSafe(out int count);
            var seats = new LobbySeatInfo[Mathf.Max(count, Balance.PlayerCount)];
            for (int i = 0; i < count; i++)
            {
                reader.ReadValueSafe(out int seat);
                reader.ReadValueSafe(out int peerId);
                reader.ReadValueSafe(out string name);
                reader.ReadValueSafe(out bool occupied);
                reader.ReadValueSafe(out bool spectator);
                reader.ReadValueSafe(out int charPick);
                reader.ReadValueSafe(out int canPick);
                reader.ReadValueSafe(out int slipperPick);
                reader.ReadValueSafe(out bool ready);
                reader.ReadValueSafe(out string banner);
                reader.ReadValueSafe(out string look);
                reader.ReadValueSafe(out string custom);
                reader.ReadValueSafe(out string build);

                var info = new LobbySeatInfo
                {
                    Seat = seat,
                    PeerId = peerId,
                    Name = name,
                    Occupied = occupied,
                    Spectator = spectator,
                    CharacterPick = charPick,
                    CanPick = canPick,
                    SlipperPick = slipperPick,
                    Ready = ready,

                    // ⚠️ NOT RE-AUTHORISED HERE, AND THAT IS THE ARRANGEMENT RATHER THAN AN
                    // OMISSION. The host has already decided; a client that checked again would
                    // need every peer's XP to do it, which is exactly the thing `BannerClaim`'s
                    // header says must stop at the host.
                    Banner = BannerCodec.DecodeSelection(banner),
                    Look = look ?? "",

                    // ⚠️ NOT RE-NORMALISED HERE EITHER. The host already ran the frame through
                    // `CustomCharacterRules.Normalise` and re-encoded it, so what arrives is a
                    // decision. `MatchInstaller` decodes it once when it builds the seat.
                    Custom = custom ?? "",
                    Build = build ?? "",
                };
                if (seat >= 0 && seat < _replicatedSeats.Length)
                {
                    _replicatedSeats[seat] = info;
                }
                if (i < seats.Length) seats[i] = info;
            }

            // ⚠️ READ AFTER THE SEATS AND ONLY IF IT IS THERE. `FastBufferReader` throws past the
            // end of a payload, and a message handler that throws drops everything queued behind
            // it. Protocol 13 guarantees a sender that writes this field, and the length check is
            // what makes a mixed-build room a missing NUMBER rather than a dead lobby.
            if (reader.Length > reader.Position)
            {
                reader.ReadValueSafe(out int watching);
                SpectatorsWatching = Mathf.Max(0, watching);
            }

            var table = new int[Balance.PlayerCount * 4];
            for (int i = 0; i < table.Length; i++) table[i] = -1;
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                if (slot < _replicatedSeats.Length && _replicatedSeats[slot] != null && _replicatedSeats[slot].Occupied)
                {
                    table[slot * 4] = slot;
                    table[slot * 4 + 1] = _replicatedSeats[slot].CharacterPick;
                    table[slot * 4 + 2] = _replicatedSeats[slot].CanPick;
                    table[slot * 4 + 3] = _replicatedSeats[slot].SlipperPick;
                }
            }

            OnLobbyPicksSynced?.Invoke(table);
            OnLobbyRosterSynced?.Invoke(seats);
            ApplyRosterToLiveSeats();
        }

        /// <summary>
        /// Re-applies the replicated roster to bodies that already exist.
        ///
        /// ⚠️⚠️ `MatchInstaller.BuildSeat` READS THIS TABLE ONCE, WHEN THE ARENA IS BUILT, AND
        /// NOTHING RE-READ IT AFTERWARDS. So a client saw the names and the bot flags as they were
        /// at the moment its own scene loaded, and every later change was invisible to it:
        /// somebody joining an empty seat mid-match stayed a nameless bot on three screens, and
        /// somebody dropping stayed a named human on three screens while a bot drove their body.
        /// The host sees neither, because `HostPeerLeft` and `HostLateJoin` fix its own copy
        /// directly. Same family as § 32.2 and § 36.1.
        ///
        /// ⚠️ IT IS IDEMPOTENT AND THAT IS THE POINT. `SyncPicks`'s own note records the fault of
        /// applying art only when an index CHANGED: the common case on a joining client is a table
        /// that agrees with what is already there, and the one message whose job is to make the
        /// seats right decided there was nothing to do.
        ///
        /// ⚠️ THE LOCAL SEAT IS NOT TOUCHED. `ApplyRebindLocalSeat` owns it, including the input
        /// reader and the camera, and a roster packet arriving mid-rebind must not undo half of it.
        /// </summary>
        private void ApplyRosterToLiveSeats()
        {
            if (GameServices.Round == null) return;

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                if (slot == NetAuthority.LocalSlot) continue;

                var unit = Unit(slot);
                if (unit == null) continue;

                var info = slot < _replicatedSeats.Length ? _replicatedSeats[slot] : null;
                bool human = info != null && info.Occupied && !info.Spectator;

                unit.PlayerName = human ? info.Name : "";
                unit.IsBot = !human;
            }
        }

        /// <summary>
        /// The colours a seat is actually painted in: the authored palette, recoloured by
        /// whatever the host said this seat is allowed to wear.
        ///
        /// ⚠️⚠️ THE LOCAL SEAT READS ITS OWN SETTINGS AND EVERY OTHER SEAT READS THE WIRE, AND
        /// THE ASYMMETRY IS THE POINT. `MatchInstaller.BuildSeat` carries the long-standing
        /// version of this note: **guessing a remote peer's palette from this machine's settings
        /// would dress a stranger in the local player's choice.** The local seat is the one case
        /// where the local answer is the true one, and it is also the only seat whose choice can
        /// change without a packet.
        ///
        /// ⚠️ AN UNKNOWN OR EMPTY ID IS THE AUTHORED PALETTE, because `PaletteVariants.For` says
        /// so: nothing equipped, a variant this build has never heard of, and a malformed id all
        /// want the character to look normal rather than to look broken.
        /// </summary>
        private static UnityEngine.Color[] PaletteForSeat(int slot, UnityEngine.Color[] authored,
                                                          GameMode mode, int charIndex)
        {
            string characterId = Core.Roster.PersonIdAt(mode, charIndex);

            // ⚠️⚠️ THE LOCAL SEAT READS ITS OWN SETTINGS AND EVERY OTHER SEAT READS THE
            // WIRE, AND THE ASYMMETRY IS DELIBERATE. The local player's dial has to answer
            // instantly while they are dragging it, before any round trip; a remote peer's look
            // is whatever the host authorised, and guessing it from this machine's settings would
            // dress a stranger in the local player's choice.
            var look = slot == NetAuthority.LocalSlot
                ? Settings.SettingsStore.LookFor(characterId)
                : LookCodec.Decode(Instance?.GetSeatInfo(slot)?.Look ?? "");

            return Visual.PaletteVariants.For(authored, look);
        }

        // -------------------------------------------------------------------
        // PICKS SYNCHRONIZATION
        // -------------------------------------------------------------------

        /// <summary>
        /// Put every seat in the character its owner picked.
        ///
        /// ⚠️⚠️ THIS METHOD HAD THREE SEPARATE FAULTS AND TOGETHER THEY ARE
        /// *"apparently only host sees the skin of other players"* AND *"in heroes gamemode,
        /// frequently they see the older version of the skin"* (🧑, 2026-08-27). All three are
        /// invisible on the host, because the host never runs the client half of this.
        ///
        /// ⚠️⚠️ 1. IT RESOLVED THE ART AGAINST THE WRONG ROSTER, AND THAT IS THE HERO STRIKE
        /// BUG EXACTLY. `RosterBook` has two overloads: `PersonArt(index)` resolves against
        /// `Roster.People`, which is the CLASSIC twelve, and `PersonArt(index, mode)` resolves
        /// against `Roster.GetPeople(mode)`. This called the first one. In Hero Strike a
        /// `CharacterIndex` is an index into the FIVE HEROES, so every client took a hero index,
        /// looked it up in the street cast, and applied a completely different character's
        /// model. `Roster.At` returns null past the end and `Resolve` then falls back to
        /// `art[0]`, so out-of-range picks all collapsed onto the same wrong body. **That is
        /// literally "the older version of the skin": it is the Classic roster, which is the
        /// older one.** `MatchInstaller` line 494 has always used the mode-aware overload, which
        /// is why a locally spawned seat looked right and a replicated one did not.
        ///
        /// ⚠️⚠️ 2. IT ONLY APPLIED THE MODEL WHEN THE INDEX CHANGED. The guard was
        /// `who.CharacterIndex != charIndex`, so a seat that already carried the right NUMBER
        /// was never given the right ART. That is the common case on a joining client: the seats
        /// are built from whatever the lobby table said at spawn time and then this sync arrives
        /// agreeing with it, so the one message whose whole job is to fix the model decided
        /// there was nothing to do. Applying art is idempotent; skipping it is not.
        ///
        /// ⚠️⚠️ 3. IT DROPPED THE PET. `MatchInstaller` passes `art.PetModel` as a sixth
        /// argument and this passed five, so every client rebuilt Nemu without Kuro. Her entire
        /// kit is him (`docs/TODO.md` § 28), so on a client she was a hero with three powers that
        /// referenced an object that was not there.
        ///
        /// ⚠️ THE MODE IS READ FROM `SceneFlow.SelectedMode`, WHICH IS REPLICATED AS OF THE SAME
        /// SESSION AND WAS NOT BEFORE IT. See § THE GAME MODE, WHICH WAS NEVER REPLICATED AT ALL:
        /// the host now sends it ahead of `StartMatch` and ahead of a late joiner's snapshot, so
        /// both ends agree before any seat exists. Sending it again inside this table would be a
        /// second source of truth for the same fact, and it would arrive too late to matter:
        /// the seats are already built by the time a pick table is read.
        /// </summary>
        public void SyncPicksClientRpc(int[] table)
        {
            if (table == null) return;

            var book = RosterBook.Load();
            var mode = UI.SceneFlow.SelectedMode;

            for (int i = 0; i + 3 < table.Length; i += 4)
            {
                int slot = table[i];
                int charIndex = table[i + 1];

                var who = Unit(slot);
                if (who == null) continue;

                if (charIndex >= 0)
                {
                    who.CharacterIndex = charIndex;

                    var person = book != null ? book.PersonArt(charIndex, mode) : null;
                    if (person != null && person.Model != null)
                    {
                        var vis = who.GetComponent<Visual.CharacterVisual>();

                        // ⚠️⚠️ 5. AND IT REPAINTED EVERY SEAT IN ITS AUTHORED COLOURS, WHICH
                        // WOULD HAVE UNDONE THE PALETTE THE MOMENT A PICK CHANGED. This method is
                        // the client's only correction for WHICH character a seat is, so it runs
                        // after `MatchInstaller` has already dressed the seat correctly; passing
                        // `person.Palette` here would have been a fifth fault of exactly the kind
                        // the four above are, and invisible on the host for the same reason.
                        vis?.ApplyModel(person.Model, person.Tint, person.Clips,
                                        PaletteForSeat(slot, person.Palette, mode, charIndex),
                                        person.PetModel);
                    }

                    // ⚠️⚠️ 4. AND IT FIXED THE ART WITHOUT FIXING THE POWERS, WHICH IS
                    // 🧑 2026-08-29: *"some clients dont see the correct ability effects but host
                    // do"*. The three faults above are all about the MODEL; this table also
                    // carries the only correction a client ever gets for WHICH HERO a seat is,
                    // and `MatchInstaller` binds the kit exactly once, at spawn, from whatever
                    // the lobby table said at that moment. A client that built its arena before
                    // the picks landed - which § 82.1 shows is routinely the FASTER machine,
                    // not a rare one - therefore ended up with the right body and somebody
                    // else's kit, and `ApplyNetworkCast` resolves the replicated cast through
                    // `AbilityFor(slot)`, so slot 1 of the wrong hero is what it played.
                    //
                    // ⚠️ THE HOST IS RIGHT BY CONSTRUCTION, which is exactly the shape of the
                    // report: it spawns from its own authoritative table and never needs this
                    // message. Nobody watching the host could see it.
                    //
                    // ⚠️ REBOUND ONLY WHEN THE HERO ACTUALLY CHANGES. `BindHero` builds a fresh
                    // `HeroKit`, which drops every cooldown and the ultimate charge with it, and
                    // `BroadcastPicks` goes out on every seat change and inside every world
                    // snapshot. Rebinding on each of those would hand a client a full ultimate
                    // meter several times a round.
                    RebindKitIfHeroChanged(who);
                }

                ApplySlipperSkin(slot, table[i + 3]);
            }
        }

        /// <summary>
        /// Give this seat the kit its CURRENT `CharacterIndex` calls for, if it is not already
        /// holding it.
        ///
        /// ⚠️⚠️ IT COMPARES THE KIT'S TYPE RATHER THAN REMEMBERING AN INDEX, so it cannot
        /// drift out of step with the thing it is guarding. `HeroAbilitySystem` exposes the kit it
        /// built and `CreateKitFor` is the one function that maps a hero id to a kit; asking
        /// whether the built kit is the same TYPE the id would produce answers "is this the right
        /// hero" without a second field for anybody to forget to write.
        ///
        /// ⚠️ CLASSIC HAS NO KITS AND MUST NOT GROW ONE HERE. `MatchInstaller` only adds the
        /// component in Hero Strike (`CLAUDE.md` § 1: the two modes are not variants of each
        /// other), so a null component is the correct state in Classic and not a seat to repair.
        /// </summary>
        private static void RebindKitIfHeroChanged(CharacterMotor who)
        {
            var abilities = who.AbilitySystem;
            if (abilities == null) return;

            var heroPeople = Core.Roster.GetPeople(GameMode.HeroStrike);
            if (heroPeople == null || heroPeople.Count == 0) return;

            string heroId = who.CharacterIndex >= 0 && who.CharacterIndex < heroPeople.Count
                ? heroPeople[who.CharacterIndex].Id
                : "dante";

            var wanted = Abilities.HeroAbilitySystem.CreateKitFor(heroId);
            if (wanted == null) return;
            if (abilities.Kit != null && abilities.Kit.GetType() == wanted.GetType()) return;

            HeroBuild build = who.PlayerSlot == NetAuthority.LocalSlot
                ? Settings.SettingsStore.CheckedHeroBuildFor(heroId)
                : HeroBuildRules.Decode(Instance?.GetSeatInfo(who.PlayerSlot)?.Build, heroId);
            abilities.BindHero(heroId, build);
        }

        public void BroadcastPicks()
        {
            if (!NetAuthority.IsHost) return;

            var round = GameServices.Round;
            if (round == null) return;

            var table = new int[Balance.PlayerCount * 4];

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var who = round.PlayerAt(slot);

                table[slot * 4] = slot;
                table[slot * 4 + 1] = who != null ? who.CharacterIndex : -1;
                table[slot * 4 + 2] = SkinOfLataFor(slot);
                table[slot * 4 + 3] = SkinOfSlipperFor(slot);
            }

            if (_nm != null && _nm.CustomMessagingManager != null)
            {
                using var writer = new FastBufferWriter(128, Allocator.Temp);
                writer.WriteValueSafe(table);
                _nm.CustomMessagingManager.SendNamedMessageToAll("SyncPicks", writer);
            }

            SyncPicksClientRpc(table);
        }

        private void OnSyncPicksMsg(ulong senderClientId, FastBufferReader reader)
        {
            // ⚠️ THE HOST IS ITS OWN CLIENT AND `SendNamedMessageToAll` LOOPS BACK TO IT.
            // Netcode invokes the handler locally for the listen host, so every broadcast the
            // host sent was also applied ON the host, a second time, over authoritative state it
            // had just produced. See § THE LOOPBACK.
            if (NetAuthority.IsHost) return;

            reader.ReadValueSafe(out int[] table);
            SyncPicksClientRpc(table);
        }

        private static int SkinOfLataFor(int slot)
        {
            var lata = GameServices.Round?.Lata;
            return lata != null && GameServices.Match?.DefenderSlot == slot ? lata.SkinIndex : -1;
        }

        private static int SkinOfSlipperFor(int slot)
        {
            var s = FindSlipper(slot);
            return s != null ? s.SkinIndex : -1;
        }

        private static void ApplySlipperSkin(int slot, int skin)
        {
            var s = FindSlipper(slot);
            if (s != null && skin >= 0) s.SkinIndex = skin;
        }

        // -------------------------------------------------------------------
        // PROP AND WORLD STATE REPLICATION
        // -------------------------------------------------------------------

        public void SyncLataClientRpc(Vector3 pos, Quaternion rot, bool isUpright, int skinIndex)
        {
            var lata = GameServices.Round?.Lata;
            if (lata == null) return;

            lata.ApplySnapshotState(pos, rot, isUpright, skinIndex);
        }

        public void SyncSlipperClientRpc(int seatOfOrigin, int ownerSlot, bool inPlay,
                                         int holderSlot, Vector3 pos, Quaternion rot, int state,
                                         Vector3 velocity, float pektusSpin, int affinity,
                                         int throwerSlot)
        {
            // ⚠️ LOOKED UP BY SEAT, WRITTEN WITH THE OWNER. `docs/TODO.md` § 78.1: this line read
            // `FindSlipper(ownerSlot)`, so a disowned taya slipper arrived as -1 and was dropped.
            var s = FindSlipper(seatOfOrigin);
            if (s == null) return;

            // ⚠️ OWNERSHIP IS APPLIED BEFORE THE STATE, not derived on this side. The host rewrites
            // it every round and it drives the foot arrow and the owner glow; re-deriving it here
            // would be a second implementation of `EquipOwnedSlippers`' rule, free to drift.
            s.OwnerSlot = ownerSlot;

            // ⚠️⚠️ SWITCHED ON BEFORE THE STATE IS APPLIED, AND OFF AFTER IT, WHICH IS NOT
            // SYMMETRY FOR ITS OWN SAKE. Coming back into play, the object has to exist before
            // `ApplySnapshotState` puts it in a hand. Going OUT of play, the state has to be
            // applied FIRST so `ReleasePreviousHolder` runs and the taya's `Carrier` actually
            // lets go: deactivating first would leave that hand still pointing at a switched-off
            // shoe, which is the half-cleared relationship `Slipper.ReleasePreviousHolder`'s own
            // note is about.
            if (inPlay && !s.gameObject.activeSelf) s.gameObject.SetActive(true);

            var holder = holderSlot >= 0 ? Unit(holderSlot) : null;
            s.ApplySnapshotState((SlipperState)state, holder, pos, rot, velocity,
                                 pektusSpin, (SlipperAffinity)affinity, throwerSlot);

            if (!inPlay && s.gameObject.activeSelf) s.gameObject.SetActive(false);
        }

        /// <summary>
        /// The replicated match and round, applied on this peer.
        ///
        /// ⚠️ IT IS AN APPLY, NOT A SEND, DESPITE THE NAME. `HostSyncPeer` calls it on the host
        /// to refresh the host's own copy and then calls `BroadcastMatchState`, which is what
        /// actually puts `SyncWorld` on the wire.
        /// </summary>
        public void SyncWorldSnapshotClientRpc(int roundNumber, int defenderSlot,
                                               float timeLeft, int[] scores,
                                               bool inProgress, bool roundActive)
        {
            // ⚠️⚠️ A PACKET THE HOST WROTE BEFORE ITS OWN ARENA LOADED IS DROPPED WHOLE.
            // `MatchDirector.IsPreStartSnapshot` carries the full account and the quote;
            // `docs/TODO.md` § 82. In one line: the host keeps streaming `SyncWorld` at 5 Hz
            // while it loads the arena it has just told everybody to load, and every one of
            // those packets says the match is NOT running. A client that finished loading first
            // read that as the match ending one second after it began.
            //
            // ⚠️ THE GUARD IS HERE, NOT INSIDE `ApplySnapshot`, BECAUSE THERE ARE TWO DIRECTORS.
            // The same `inProgress` goes to `RoundDirector.ApplySnapshot` on the next line, which
            // clears `RoundActive` and with it `CanAct`. Refusing the match half and applying the
            // round half swaps a phantom result board for a body that cannot move.
            if (GameServices.Match != null && GameServices.Match.IsPreStartSnapshot(inProgress))
                return;

            bool wasRoundActive = GameServices.Round != null && GameServices.Round.RoundActive;

            GameServices.Match?.ApplySnapshot(scores, roundNumber, inProgress);
            GameServices.Round?.ApplySnapshot(timeLeft, roundActive, defenderSlot, inProgress);

            if (NetAuthority.IsHost) return;

            ApplyNetworkRoundBoundary(wasRoundActive, roundActive, inProgress, roundNumber);
        }

        /// <summary>
        /// Raise and lower the intermission card on a CLIENT, which never hears the event.
        ///
        /// ⚠️⚠️ `IntermissionStarted` IS HOST-ONLY AND MUST STAY THAT WAY. It is raised by
        /// `MatchDirector.BeginIntermission`, which sits behind `SliceRunner`'s
        /// `NetAuthority.ShouldResolve()`, and `SliceRunner` is itself wired to it:
        /// `OnIntermission` calls `ResetWorld`, which teleports all four bodies and hands out the
        /// tsinelas, and then schedules `Advance`, which calls `AdvanceRound`. Raising it on a
        /// client would give every peer its own authority over the round number, and four peers
        /// each advancing a match is four matches (`VISION.md` § 4). So the CARD gets a signal
        /// and the runner does not.
        ///
        /// ⚠️ THE SIGNAL IS DERIVED RATHER THAN SENT, AND IT COSTS NO WIRE CHANGE. During the
        /// host's intermission `RoundActive` is false while `MatchInProgress` is still true and
        /// `RoundNumber` has not moved yet (`AdvanceRound` increments it when the buffer ends).
        /// That combination happens at no other time: a match ENDING drops `inProgress` with it,
        /// which is what the third argument rules out.
        ///
        /// ⚠️ AND IT IS AN EDGE, NOT A STATE. `SyncWorld` arrives at 5 Hz, so acting on the value
        /// would re-raise the card ten times over one intermission and restart its timeline on
        /// every packet.
        /// </summary>
        private static void ApplyNetworkRoundBoundary(bool wasRoundActive, bool roundActive,
                                                      bool inProgress, int roundNumber)
        {
            // ⚠️⚠️ THE ANNOUNCER'S PER-ROUND STATE IS RESET HERE, BECAUSE A CLIENT NEVER GETS
            // `RoundStarted`. 🧑 2026-08-29: *"wrong sfx played for non host, 30 seconds played
            // even tho no 30 seconds yet"*. `VoiceDirector.OnRoundStarted` clears `_clock30Said`
            // and `_clock10Said` so each warning speaks once PER ROUND, and it is wired to
            // `MatchDirector.RoundStarted` — which `ApplySnapshot`'s header records as
            // deliberately not raised on a client, because its other subscribers teleport bodies
            // and advance rounds. So on a client the two flags were set in round one and never
            // cleared again: rounds two through eight got no clock warnings at all, and any
            // warning spoken at the wrong moment was spent for the rest of the match.
            //
            // ⚠️ THE EDGE IS THE ONE THE CARD ALREADY USES AND COSTS NO WIRE CHANGE. `roundActive`
            // going false → true is a round beginning and happens at no other time; that is the
            // same derivation this method's header spends four paragraphs defending for the
            // intermission card, reused rather than re-invented.
            //
            // ⚠️ AND IT IS ABOVE THE CARD'S NULL GUARD ON PURPOSE. The announcer is not the card
            // and must not stop working on a screen that has no card on it.
            if (!wasRoundActive && roundActive)
                GameServices.Voice?.OnRoundStarted(roundNumber);

            var card = FindFirstObjectByType<UI.RoleSwapCard>();
            if (card == null) return;

            if (wasRoundActive && !roundActive && inProgress)
            {
                int next = roundNumber + 1;
                card.ShowForShot(next, Core.MatchRules.DefenderSlotFor(next));
                return;
            }

            // ⚠️ THE HOST HIDES THIS CARD FROM `RoundStarted`, which a client never gets either.
            // Without this the card stays up over the whole of the next round, which is worse
            // than never showing it: it is a full-screen panel over live play.
            if (!wasRoundActive && roundActive) card.DismissAndPractice();
        }

        /// <summary>
        /// Set the moment the host commits to loading an arena, cleared the first time its own
        /// `MatchDirector` says the match is running. While it is up, this host has told four
        /// people to start a match it has not started itself yet.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE THE HOST WAS BROADCASTING "NO MATCH RUNNING" THROUGH ITS OWN
        /// ARENA LOAD, FIVE TIMES A SECOND. `docs/TODO.md` § 82.3. § 82.1 fixed the receiving
        /// end — `MatchDirector.IsPreStartSnapshot` drops the packet on the client, and that is
        /// the half that had to exist because the honest sender cannot be relied on across
        /// versions — so this is bandwidth and honesty rather than correctness. It is still worth
        /// having: a stream of packets asserting something false is a trap for the next person
        /// reading a capture.
        ///
        /// ⚠️⚠️ `LobbySession.MatchInProgress` IS THE OBVIOUS GATE AND IT IS WRONG. § 82.3 wrote
        /// that down so nobody spends the hour twice: it is set by `HostStartMatch` and cleared
        /// only by `NetSession`'s shutdown path, so it is **still true while the result board is
        /// up**. Gating on `lobby.MatchInProgress &amp;&amp; !match.MatchInProgress` would suppress
        /// the packet that ENDS the match, which is the one packet in the whole exchange that
        /// must not be dropped.
        ///
        /// ⚠️ SO IT IS A ONE-SHOT LATCH, NOT A STATE. It answers "is the host mid-load", which no
        /// existing flag answers, and it can only ever suppress packets that repeat a value the
        /// host is about to contradict.
        /// </summary>
        private bool _loadingOwnArena;

        /// <summary>
        /// The host has committed to an arena and is about to load it. See
        /// <see cref="_loadingOwnArena"/>.
        ///
        /// ⚠️ THE REMATCH PATH CALLS THIS TOO. `UI.MatchResult.BeginRematchLocally` reloads on
        /// every peer and has the identical race; a latch set only in `HostStartMatch` would be
        /// correct for the first match of a session and for no other.
        /// </summary>
        public static void HostBeginningArenaLoad()
        {
            if (!NetAuthority.IsHost || Instance == null) return;

            Instance._loadingOwnArena = true;
        }

        private void BroadcastMatchState()
        {
            if (!NetAuthority.IsHost || _nm == null || _nm.CustomMessagingManager == null) return;

            var match = GameServices.Match;
            var round = GameServices.Round;
            if (match == null) return;

            // ⚠️ CLEARED BY THE HOST'S OWN DIRECTOR, NOT BY A SCENE EVENT. `SliceRunner.Begin`
            // is what sets `MatchInProgress`, so this is the first packet after the host's arena
            // is genuinely live, which is exactly the moment the suppression stops being true.
            if (match.MatchInProgress) _loadingOwnArena = false;

            // ⚠️ AND THE SUPPRESSION IS ONLY EVER OF A FALSE `inProgress`. If the host somehow
            // reaches here with the latch up and a live match, the line above has already cleared
            // it; there is no path where this drops a packet carrying new information.
            if (_loadingOwnArena) return;

            var scores = new int[Balance.PlayerCount];
            for (int i = 0; i < scores.Length; i++) scores[i] = match.ScoreFor(i);

            using var writer = new FastBufferWriter(256, Allocator.Temp);
            writer.WriteValueSafe(match.RoundNumber);
            writer.WriteValueSafe(match.DefenderSlot);
            // WARNING  THE FALLBACK IS THIS MATCH'S ROUND LENGTH, NOT THE SHIPPED 90.
            // `RoundDirector.RoundLength` reads `SceneFlow.SelectedRoundSeconds`, so a custom
            // lobby's clock is what a joining peer is told when there is no live round to read
            // one off. A literal 90 here would hand a client on a 120 second match a clock
            // thirty seconds short before the first tick, and it would read as a desync.
            writer.WriteValueSafe(round != null ? round.TimeLeft
                                                : UI.SceneFlow.SelectedRoundSeconds);
            writer.WriteValueSafe(scores);
            writer.WriteValueSafe(match.MatchInProgress);
            writer.WriteValueSafe(round != null && round.RoundActive);
            writer.WriteValueSafe(round != null ? round.TayaCampSeconds : 0.0f);
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
                writer.WriteValueSafe(round != null ? round.AttackerIdleSeconds(slot) : 0.0f);
            _nm.CustomMessagingManager.SendNamedMessageToAll("SyncWorld", writer);
        }

        // -------------------------------------------------------------------
        // § THE PROP STREAM, AND WHY IT DOES NOT SEND WHAT HAS NOT CHANGED
        //
        // ⚠️⚠️ A CAN AND FOUR TSINELAS AT REST WERE COSTING FIVE MESSAGES A STEP, FOREVER. The
        // world tick is the physics step, 50 Hz, so an idle arena was sending 250 prop messages a
        // second to every peer whether or not a single one of them had moved. Most of a round is
        // three slippers lying in the road and a can standing still.
        //
        // ⚠️ ON A LAN THAT IS MERELY WASTE; ON RELAY IT IS THE BUDGET. Every byte goes out to the
        // allocation and back down to each peer, and this game is played on venue wifi and
        // Philippine home connections (`NetSession.Configure`'s 30 second timeout is the same
        // observation from the other end).
        //
        // ⚠️⚠️ THE KEEPALIVE IS NOT OPTIONAL AND IT IS WHY THIS IS SAFE. A joiner who missed the
        // one packet that said "the can went over" would believe it upright until it moved again,
        // which on a can that has come to rest is the rest of the round. Twice a second costs
        // almost nothing and bounds that window at half a second, and a reconnect still asks for a
        // full snapshot rather than waiting for it.
        //
        // ⚠️ AND THE UNCONDITIONAL SENDERS STAY. `Carrier` calls `BroadcastSlipperState` directly
        // on a grab and on a throw, and the reset channel calls `BroadcastLataState` on a restore:
        // those are EVENTS, and an event may never wait for a poll to notice it.
        // -------------------------------------------------------------------

        private const float PropKeepaliveSeconds = 0.5f;

        /// <summary>How far a prop must move before it is worth a packet. One centimetre.</summary>
        private const float PropMoveEpsilon = 0.01f;

        // -------------------------------------------------------------------
        // § THE PROP STREAM IS A POSE STREAM TOO, AND `PoseDelivery`'S NOTE MISSED IT
        //
        // ⚠️⚠️ 🧑 2026-08-29 SAID *"the bots AND SLIPPERS were going out of map"*, AND ONLY THE
        // BOTS HALF WAS FIXED. `docs/TODO.md` § 71.3 moved `SyncUnit` and `SubmitMove` to
        // `PoseDelivery` and left everything else reliable on a stated rule: *"the slipper's
        // state changes and the lata going over are EVENTS: each one happens once and nothing
        // later repeats it"*. That sentence is true of a grab and of a throw. It is NOT true of
        // this file's own `BroadcastSlipperStateIfChanged`, which is a POSITION stream wearing
        // the same message name: it fires on `FixedUpdate` whenever the shoe has moved more than
        // `PropMoveEpsilon`, and a tsinelas in flight moves about 0.3 m per step. A thrown
        // slipper was therefore 50 reliable messages a second, per slipper, which is the exact
        // shape `PoseDelivery`'s own header calls *"actively worse"*.
        //
        // ⚠️⚠️ SO THE TSINELAS HALF OF THE REPORT HAD ITS TRANSPORT CAUSE LEFT IN PLACE. One lost
        // packet head-of-line blocked the shoe's whole backlog and delivered it at once, and
        // `Slipper.ApplySnapshotState` writes the arriving position straight onto the transform
        // with no correction filter of any kind, so a burst is not smoothed there the way
        // `ApplyNetworkTransform` at least tries to smooth a body. The § 71.3 clamp is what kept
        // it inside the walls; it did not stop the shoe teleporting along them.
        //
        // ⚠️⚠️ AND THE ANSWER IS NOT TO FLIP THE MESSAGE, IT IS TO SPLIT IT IN TWO. `SyncSlipper`
        // is genuinely two things at once. Its position fully replaces itself every step and can
        // afford to be lost; its STATE, its HOLDER, its affinity and its thrower are the events
        // § 71.3 was protecting, and a dropped one is a shoe stuck in the wrong hand for the rest
        // of the round. So `SyncSlipper` keeps every field and stays reliable, and a new
        // `SlipperPose` carries a position and nothing else on `PoseDelivery`.
        //
        // ⚠️⚠️ THE FIRST DRAFT OF THIS SENT THE SAME MESSAGE ON TWO DIFFERENT CHANNELS DEPENDING
        // ON WHAT HAD CHANGED, AND THAT WAS WRONG IN A WAY WORTH RECORDING, because it looks
        // strictly cheaper and it is not. Two channels have NO ordering between them: only
        // `UnreliableSequenced` drops an old packet, and only against others on its own channel.
        // A pose sent one step BEFORE a throw could therefore arrive one step AFTER the reliable
        // throw packet, and since that pose carried the whole payload it would put the tsinelas
        // back into the hand it had just left, re-run `ReleasePreviousHolder` and `NotifyEquipped`
        // for a grab that had already ended, and correct itself 20 ms later. That is § 38.8's
        // two-authors buzz arriving by a new road. **A message that carries no state cannot do
        // it**, which is why the split is by PAYLOAD and not by delivery flag.
        //
        // ⚠️ THE KEEPALIVE IS WHAT MAKES THIS SAFE RATHER THAN MERELY CHEAPER, and it was already
        // here for a different reason. Every discrete field is re-sent reliably twice a second
        // whether or not it changed, so even a peer that missed the reliable edge AND the two
        // unreliable poses either side of it is corrected within `PropKeepaliveSeconds`.
        //
        // ⚠️ THE LATA IS THE SAME SPLIT FOR THE SAME REASON. A can that has been hit ROLLS, and
        // a roll is a pose stream; `IsUpright` going over is the event that scores. Position-only
        // packets go unreliable, the upright bit and the skin never do.
        //
        // ⚠️ THE UNCONDITIONAL SENDERS ARE UNTOUCHED AND STAY RELIABLE. `Carrier` calls
        // `BroadcastSlipperState` directly on a grab and on a throw and the reset channel calls
        // `BroadcastLataState` on a restore. Those are pure events with no stream behind them, so
        // they take the default and this change cannot reach them.
        // -------------------------------------------------------------------

        // ⚠️ THERE IS NO `PropEventDelivery` CONSTANT. `SyncSlipper` and `SyncLata` take
        // `SendNamedMessageToAll`'s reliable DEFAULT exactly as they always have, so no event
        // caller had to change and none can be broken by forgetting an argument. Only the two new
        // pose messages name a delivery, and they name `PoseDelivery`.

        private Vector3 _lastLataPosition = new Vector3(float.NaN, float.NaN, float.NaN);
        private bool _lastLataUpright;
        private float _lataKeepaliveLeft;

        private readonly Dictionary<int, Vector3> _lastSlipperPosition = new Dictionary<int, Vector3>();
        private readonly Dictionary<int, int> _lastSlipperState = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _lastSlipperHolder = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _lastSlipperAffinity = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _lastSlipperThrower = new Dictionary<int, int>();

        /// <summary>Ownership travels now, so a change of it is a discrete change. See
        /// <see cref="Slipper.SeatOfOrigin"/> and `docs/TODO.md` § 78.1.</summary>
        private readonly Dictionary<int, int> _lastSlipperOwner = new Dictionary<int, int>();

        /// <summary>Whether the object is switched on. The taya's tsinelas is parked with
        /// `SetActive(false)` and that never reached a client. See § 78.1.</summary>
        private readonly Dictionary<int, int> _lastSlipperActive = new Dictionary<int, int>();
        private readonly Dictionary<int, float> _slipperKeepaliveLeft = new Dictionary<int, float>();

        private void BroadcastLataStateIfChanged()
        {
            var lata = GameServices.Round?.Lata;
            if (lata == null) return;

            _lataKeepaliveLeft -= Time.fixedDeltaTime;

            bool moved = (lata.transform.position - _lastLataPosition).sqrMagnitude
                         > PropMoveEpsilon * PropMoveEpsilon;

            bool toppled = lata.IsUpright != _lastLataUpright;
            bool keepalive = _lataKeepaliveLeft <= 0.0f;

            if (!moved && !toppled && !keepalive) return;

            _lastLataPosition = lata.transform.position;
            _lastLataUpright = lata.IsUpright;
            _lataKeepaliveLeft = PropKeepaliveSeconds;

            // ⚠️ A ROLL IS A POSE AND TRAVELS AS ONE; GOING OVER IS THE EVENT THAT SCORES AND
            // TRAVELS AS THE FULL RELIABLE SNAPSHOT. See the § note above.
            if (toppled || keepalive) BroadcastLataState();
            else BroadcastLataPose();
        }

        private void BroadcastSlipperStateIfChanged(Slipper slipper)
        {
            if (slipper == null) return;

            // ⚠️⚠️ THE KEY IS THE SEAT OF ORIGIN AND THE OWNER IS NOW A WATCHED FIELD. Both halves
            // of that matter and `docs/TODO.md` § 78.1 is why. These dictionaries used to be keyed
            // on `OwnerSlot`, which goes to -1 the round its seat becomes taya, so the taya's shoe
            // both fell out of the table and stopped being reachable at all. And because ownership
            // now travels rather than being re-derived on the far side, a CHANGE of owner is a
            // discrete change like any other: without it, the round that disowns a slipper would
            // send nothing and every client would keep the previous round's owner on its foot
            // arrow and its owner glow.
            int seat = slipper.SeatOfOrigin;
            int owner = slipper.OwnerSlot;
            int state = (int)slipper.State;
            int holder = slipper.Holder != null ? slipper.Holder.PlayerSlot : -1;

            // ⚠️ AFFINITY AND THROWER JOIN STATE AND HOLDER AS DISCRETE FIELDS, AND THEY WERE NOT
            // WATCHED BEFORE. Both already travel in the payload and neither is derivable from a
            // position: `Affinity` is what makes a pektus curve read as one, and `ThrowerSlot` is
            // who a bank is credited to. While every packet went reliably it did not matter which
            // fields a re-send was for, because none could be lost. Deciding the channel by what
            // changed makes the question live, so the set has to be the whole discrete payload
            // rather than the two fields somebody happened to be tracking for the rate limit.
            int affinity = (int)slipper.Affinity;
            int thrower = slipper.ThrowerSlot;

            // ⚠️⚠️ WHETHER THE OBJECT IS SWITCHED ON IS DISCRETE STATE AND IT NEVER TRAVELLED.
            // `EquipOwnedSlippers` parks the taya's tsinelas with `SetActive(false)` behind the
            // host gate, so on a client it stayed on, stayed in that seat's hand, and the taya
            // walked the whole round carrying a shoe (§ 78.1).
            int active = slipper.gameObject.activeSelf ? 1 : 0;

            if (seat < 0) return;

            float left = _slipperKeepaliveLeft.TryGetValue(seat, out float k) ? k : 0.0f;
            left -= Time.fixedDeltaTime;

            // ⚠️⚠️ A CARRIED SHOE IS NOT WORTH A SINGLE POSE PACKET, AND IT WAS COSTING FIFTY A
            // SECOND. `Slipper.ApplySnapshotPose` returns immediately while the state is `Held`,
            // because `Carrier` parents the tsinelas to the carry anchor on every peer and the
            // hand is its only author (the § 38.8 buzz). So every one of those packets was sent,
            // routed, and discarded on arrival by design. A tsinelas is in somebody's hand for a
            // large part of a round and there are four of them: this is § 38.18's finding again,
            // one object further in, and it is the same answer, do not send what nobody applies.
            //
            // ⚠️ THE DISCRETE HALF IS UNAFFECTED. Picking it up and throwing it are state changes
            // and still go reliably the moment they happen, and the keepalive still re-sends the
            // holder twice a second, so a peer that missed the grab is corrected on the same
            // half-second bound as everything else.
            // ⚠️ A PARKED SHOE SENDS NO POSE EITHER, for the same reason a carried one does not:
            // it is switched off on every peer that has heard about it, so its position is not a
            // thing anybody draws. The discrete edge that parks it still goes reliably.
            bool carried = slipper.State == SlipperState.Held || active == 0;

            bool moved = !carried
                         && (!_lastSlipperPosition.TryGetValue(seat, out var previous)
                             || (slipper.transform.position - previous).sqrMagnitude
                                > PropMoveEpsilon * PropMoveEpsilon);

            bool discrete = !_lastSlipperState.TryGetValue(seat, out int lastState) || lastState != state
                            || !_lastSlipperHolder.TryGetValue(seat, out int lastHolder) || lastHolder != holder
                            || !_lastSlipperAffinity.TryGetValue(seat, out int lastAff) || lastAff != affinity
                            || !_lastSlipperThrower.TryGetValue(seat, out int lastThr) || lastThr != thrower
                            || !_lastSlipperOwner.TryGetValue(seat, out int lastOwner) || lastOwner != owner
                            || !_lastSlipperActive.TryGetValue(seat, out int lastActive) || lastActive != active;

            bool keepalive = left <= 0.0f;

            if (!moved && !discrete && !keepalive)
            {
                _slipperKeepaliveLeft[seat] = left;
                return;
            }

            _lastSlipperPosition[seat] = slipper.transform.position;
            _lastSlipperState[seat] = state;
            _lastSlipperHolder[seat] = holder;
            _lastSlipperAffinity[seat] = affinity;
            _lastSlipperThrower[seat] = thrower;
            _lastSlipperOwner[seat] = owner;
            _lastSlipperActive[seat] = active;
            _slipperKeepaliveLeft[seat] = PropKeepaliveSeconds;

            // ⚠️⚠️ THIS BRANCH IS THE TSINELAS HALF OF 🧑'S *"the bots and slippers were going
            // out of map"*, AND IT IS THE HALF § 71.3 DID NOT FIX. A shoe in flight moves every
            // step, so before this it was 50 guaranteed-delivery messages a second carrying a
            // position the next one replaces. See the § note above.
            if (discrete || keepalive) BroadcastSlipperState(slipper);
            else BroadcastSlipperPose(slipper);
        }

        /// <summary>
        /// Sends the whole authoritative can, reliably, immediately and outside the world tick.
        ///
        /// ⚠️ EVERY FIELD, EVERY TIME, ON THE RELIABLE DEFAULT. This is the EVENT half of the
        /// split described above; `BroadcastLataPose` is the stream half.
        /// </summary>
        public void BroadcastLataState()
        {
            if (!NetAuthority.IsHost || _nm == null || _nm.CustomMessagingManager == null) return;
            var lata = GameServices.Round?.Lata;
            if (lata == null) return;

            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(lata.transform.position);
            writer.WriteValueSafe(lata.transform.rotation);
            writer.WriteValueSafe(lata.IsUpright);
            writer.WriteValueSafe(lata.SkinIndex);
            _nm.CustomMessagingManager.SendNamedMessageToAll("SyncLata", writer);
        }

        /// <summary>The rolling can, and nothing else about it.</summary>
        public void BroadcastLataPose()
        {
            if (!NetAuthority.IsHost || _nm == null || _nm.CustomMessagingManager == null) return;
            var lata = GameServices.Round?.Lata;
            if (lata == null) return;

            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(lata.transform.position);
            writer.WriteValueSafe(lata.transform.rotation);
            _nm.CustomMessagingManager.SendNamedMessageToAll("LataPose", writer, PoseDelivery);
        }

        /// <summary>
        /// Sends one whole authoritative slipper, reliably, immediately and on the world tick.
        ///
        /// ⚠️ EVERY FIELD, EVERY TIME, ON THE RELIABLE DEFAULT. `Carrier` calls this on a grab and
        /// on a throw and both are events. This is the EVENT half of the split described above;
        /// `BroadcastSlipperPose` is the stream half.
        /// </summary>
        public void BroadcastSlipperState(Slipper slipper)
        {
            if (!NetAuthority.IsHost || slipper == null ||
                _nm == null || _nm.CustomMessagingManager == null)
                return;

            int holderSlot = slipper.Holder != null ? slipper.Holder.PlayerSlot : -1;
            using var writer = new FastBufferWriter(128, Allocator.Temp);

            // ⚠️⚠️ THE KEY FIELD IS `SeatOfOrigin` AND OWNERSHIP FOLLOWS IT AS ORDINARY PAYLOAD.
            // It used to be `OwnerSlot` doing both jobs, which is `docs/TODO.md` § 78.1: the taya's
            // shoe goes to owner -1 for a round and became unaddressable rather than merely
            // disowned. The seat never moves, so it can be addressed; the owner does move, so it
            // is sent.
            writer.WriteValueSafe(slipper.SeatOfOrigin);
            writer.WriteValueSafe(slipper.OwnerSlot);

            // ⚠️ WHETHER IT IS IN PLAY AT ALL. `EquipOwnedSlippers` switches the taya's shoe off
            // host-side; without this the client kept it on and in that seat's hand (§ 78.1).
            writer.WriteValueSafe(slipper.gameObject.activeSelf);
            writer.WriteValueSafe(holderSlot);
            writer.WriteValueSafe(slipper.transform.position);
            writer.WriteValueSafe(slipper.transform.rotation);
            writer.WriteValueSafe((int)slipper.State);
            writer.WriteValueSafe(slipper.Velocity);
            writer.WriteValueSafe(slipper.PektusSpin);
            writer.WriteValueSafe((int)slipper.Affinity);
            writer.WriteValueSafe(slipper.ThrowerSlot);
            _nm.CustomMessagingManager.SendNamedMessageToAll("SyncSlipper", writer);
        }

        /// <summary>One slipper's flight path, and nothing else about it.</summary>
        public void BroadcastSlipperPose(Slipper slipper)
        {
            if (!NetAuthority.IsHost || slipper == null ||
                _nm == null || _nm.CustomMessagingManager == null)
                return;

            // ⚠️ ADDRESSED BY SEAT, LIKE `SyncSlipper`. A pose keyed on a field that goes -1 for
            // the taya is a pose nobody can apply; see `Slipper.SeatOfOrigin`.
            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(slipper.SeatOfOrigin);
            writer.WriteValueSafe(slipper.transform.position);
            writer.WriteValueSafe(slipper.transform.rotation);
            writer.WriteValueSafe(slipper.Velocity);
            _nm.CustomMessagingManager.SendNamedMessageToAll("SlipperPose", writer, PoseDelivery);
        }

        public void BroadcastWorldSnapshot()
        {
            if (!NetAuthority.IsHost) return;

            BroadcastPicks();

            var match = GameServices.Match;
            var round = GameServices.Round;
            if (match == null) return;

            var scores = new int[Balance.PlayerCount];
            for (int i = 0; i < scores.Length; i++) scores[i] = match.ScoreFor(i);

            bool roundActive = round != null && round.RoundActive;
            // WARNING  SAME FALLBACK AS THE SNAPSHOT WRITER ABOVE AND FOR THE SAME REASON.
            // Two places answering one question have to answer it the same way; `docs/TODO.md`
            // section 38.6's audit exists because a writer and a reader that disagree are not an
            // error, they are silently misread bytes.
            float timeLeft = round != null ? round.TimeLeft : UI.SceneFlow.SelectedRoundSeconds;

            SyncWorldSnapshotClientRpc(match.RoundNumber, match.DefenderSlot, timeLeft, scores,
                                       match.MatchInProgress, roundActive);
            BroadcastMatchState();

            if (round?.Lata != null)
            {
                var l = round.Lata;
                SyncLataClientRpc(l.transform.position, l.transform.rotation, l.IsUpright, l.SkinIndex);

                BroadcastLataState();
            }

            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var s = FindSlipper(slot);
                if (s != null)
                {
                    int holderSlot = s.Holder != null ? s.Holder.PlayerSlot : -1;
                    SyncSlipperClientRpc(s.SeatOfOrigin, s.OwnerSlot, s.gameObject.activeSelf,
                        holderSlot, s.transform.position, s.transform.rotation, (int)s.State,
                        s.Velocity, s.PektusSpin, (int)s.Affinity, s.ThrowerSlot);

                    BroadcastSlipperState(s);
                }

                var unit = Unit(slot);
                if (unit != null)
                {
                    SyncUnitTransformClientRpc(slot, unit.transform.position, unit.transform.eulerAngles.y, unit.Velocity);
                    BroadcastAbilityState(slot, unit);
                }
            }
        }

        // -------------------------------------------------------------------
        // § ABILITY STATE ACROSS A RECONNECT
        //
        // ⚠️⚠️ 🧑 2026-08-27: *"or if u retain ur skill cooldowns and charges and shi"*. Before
        // this the answer was no. The world snapshot carried the round, the scores, the clock,
        // the lata, the slippers, the picks and every unit transform, and **not one byte of
        // ability state**, so a client that dropped and came back rebuilt its kit from the
        // constructor: cooldowns zero, charges full, ultimate meter empty.
        //
        // ⚠️⚠️ AND IT CUTS BOTH WAYS. Reconnecting to refresh a 62 s cooldown is the cheat;
        // losing 115 banked charge to a dropped packet is the bug that actually gets reported.
        // The host never had either, because its own kits are continuous objects that were never
        // rebuilt, which is exactly why this survived every single-machine test.
        //
        // ⚠️ IT IS A SEPARATE NAMED MESSAGE RATHER THAN MORE FIELDS ON `SyncWorld`, because
        // `SyncWorld` is per-MATCH and this is per-SEAT. Widening it would have meant packing four
        // seats' kits into one payload and unpacking them against a seat order the receiving side
        // has to already agree about, which is the shape of bug § 32.2 records three of.
        // -------------------------------------------------------------------

        private void BroadcastAbilityState(int slot, CharacterMotor unit)
        {
            if (!NetAuthority.IsHost) return;

            var kit = unit != null && unit.AbilitySystem != null ? unit.AbilitySystem.Kit : null;
            if (kit == null) return;

            float s1Cd = kit.Skill1 != null ? kit.Skill1.CooldownRemaining : 0.0f;
            int s1Ch = kit.Skill1 != null ? kit.Skill1.ChargesRemaining : 0;
            float s2Cd = kit.Skill2 != null ? kit.Skill2.CooldownRemaining : 0.0f;
            int s2Ch = kit.Skill2 != null ? kit.Skill2.ChargesRemaining : 0;
            float ultCd = kit.Ultimate != null ? kit.Ultimate.CooldownRemaining : 0.0f;

            SyncAbilityStateClientRpc(slot, kit.UltimateCharge, s1Cd, s1Ch, s2Cd, s2Ch, ultCd);

            if (_nm == null || _nm.CustomMessagingManager == null) return;

            using var writer = new FastBufferWriter(64, Allocator.Temp);
            writer.WriteValueSafe(slot);
            writer.WriteValueSafe(kit.UltimateCharge);
            writer.WriteValueSafe(s1Cd);
            writer.WriteValueSafe(s1Ch);
            writer.WriteValueSafe(s2Cd);
            writer.WriteValueSafe(s2Ch);
            writer.WriteValueSafe(ultCd);
            _nm.CustomMessagingManager.SendNamedMessageToAll("SyncAbility", writer);
        }

        /// <summary>
        /// ⚠️ THE HOST APPLIES NOTHING. Its kit is the authority and is already correct; letting
        /// it write its own broadcast back over itself would round-trip every value through the
        /// wire's precision for no reason, and would overwrite a cooldown that started in the
        /// same frame the snapshot was built.
        /// </summary>
        public void SyncAbilityStateClientRpc(int slot, float ultimateCharge,
                                              float skill1Cooldown, int skill1Charges,
                                              float skill2Cooldown, int skill2Charges,
                                              float ultimateCooldown)
        {
            if (NetAuthority.IsHost) return;

            var unit = Unit(slot);
            var kit = unit != null && unit.AbilitySystem != null ? unit.AbilitySystem.Kit : null;
            if (kit == null) return;

            // ⚠️⚠️ THE OWNER'S OWN COOLDOWNS MAY BE RAISED BY THIS AND NOT LOWERED, WHILE A
            // ROUND IS LIVE. That is the spammable-teleport fix; `HeroAbility
            // .ApplyNetworkSnapshot` carries the whole chain, and the short version is that a
            // host which REFUSED a cast reports the state it actually has, which is no cooldown,
            // and assigning that over the cooldown the owner just spent predicting hands the
            // ability straight back.
            //
            // ⚠️⚠️ AND IT IS GATED ON THE ROUND BEING LIVE, WHICH IS NOT A DETAIL. `SliceRunner
            // .ResetWorld` calls `ResetKit` on every seat at a round boundary, and that is a
            // legitimate clearing that MUST reach the owner or it starts the next round holding
            // a cooldown nothing will ever tick away. The intermission is exactly when the round
            // clock is stopped, so asking the round is asking the right question rather than
            // special-casing the reset. A reconnecting client is covered too: its kit is rebuilt
            // at zero, so raising it to the host's value is what the rule already does.
            bool mine = slot == NetAuthority.LocalSlot;
            bool roundLive = GameServices.Round != null && GameServices.Round.RoundActive;

            kit.ApplyNetworkSnapshot(ultimateCharge, skill1Cooldown, skill1Charges,
                                     skill2Cooldown, skill2Charges, ultimateCooldown,
                                     mayLower: !mine || !roundLive);
        }

        private void OnSyncAbilityMsg(ulong senderClientId, FastBufferReader reader)
        {
            // ⚠️ THE HOST IS ITS OWN CLIENT AND `SendNamedMessageToAll` LOOPS BACK TO IT.
            // Netcode invokes the handler locally for the listen host, so every broadcast the
            // host sent was also applied ON the host, a second time, over authoritative state it
            // had just produced. See § THE LOOPBACK.
            if (NetAuthority.IsHost) return;

            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out float ultimateCharge);
            reader.ReadValueSafe(out float s1Cd);
            reader.ReadValueSafe(out int s1Ch);
            reader.ReadValueSafe(out float s2Cd);
            reader.ReadValueSafe(out int s2Ch);
            reader.ReadValueSafe(out float ultCd);

            SyncAbilityStateClientRpc(slot, ultimateCharge, s1Cd, s1Ch, s2Cd, s2Ch, ultCd);
        }

        private void OnSyncWorldMsg(ulong senderClientId, FastBufferReader reader)
        {
            // ⚠️ THE HOST IS ITS OWN CLIENT AND `SendNamedMessageToAll` LOOPS BACK TO IT.
            // Netcode invokes the handler locally for the listen host, so every broadcast the
            // host sent was also applied ON the host, a second time, over authoritative state it
            // had just produced. See § THE LOOPBACK.
            if (NetAuthority.IsHost) return;

            reader.ReadValueSafe(out int roundNumber);
            reader.ReadValueSafe(out int defenderSlot);
            reader.ReadValueSafe(out float timeLeft);
            reader.ReadValueSafe(out int[] scores);
            reader.ReadValueSafe(out bool inProgress);
            reader.ReadValueSafe(out bool roundActive);
            reader.ReadValueSafe(out float tayaCampSeconds);
            var attackerIdle = new float[Balance.PlayerCount];
            for (int slot = 0; slot < attackerIdle.Length; slot++)
                reader.ReadValueSafe(out attackerIdle[slot]);

            SyncWorldSnapshotClientRpc(roundNumber, defenderSlot, timeLeft, scores, inProgress,
                                       roundActive);
            GameServices.Round?.ApplyNetworkTournamentState(tayaCampSeconds, attackerIdle);
        }

        private void OnSyncLataMsg(ulong senderClientId, FastBufferReader reader)
        {
            // ⚠️ THE HOST IS ITS OWN CLIENT AND `SendNamedMessageToAll` LOOPS BACK TO IT.
            // Netcode invokes the handler locally for the listen host, so every broadcast the
            // host sent was also applied ON the host, a second time, over authoritative state it
            // had just produced. See § THE LOOPBACK.
            if (NetAuthority.IsHost) return;

            reader.ReadValueSafe(out Vector3 pos);
            reader.ReadValueSafe(out Quaternion rot);
            reader.ReadValueSafe(out bool isUpright);
            reader.ReadValueSafe(out int skinIndex);

            SyncLataClientRpc(pos, rot, isUpright, skinIndex);
        }

        private void OnSyncSlipperMsg(ulong senderClientId, FastBufferReader reader)
        {
            // ⚠️ THE HOST IS ITS OWN CLIENT AND `SendNamedMessageToAll` LOOPS BACK TO IT.
            // Netcode invokes the handler locally for the listen host, so every broadcast the
            // host sent was also applied ON the host, a second time, over authoritative state it
            // had just produced. See § THE LOOPBACK.
            if (NetAuthority.IsHost) return;

            reader.ReadValueSafe(out int seatOfOrigin);
            reader.ReadValueSafe(out int ownerSlot);
            reader.ReadValueSafe(out bool inPlay);
            reader.ReadValueSafe(out int holderSlot);
            reader.ReadValueSafe(out Vector3 pos);
            reader.ReadValueSafe(out Quaternion rot);
            reader.ReadValueSafe(out int state);
            reader.ReadValueSafe(out Vector3 velocity);
            reader.ReadValueSafe(out float pektusSpin);
            reader.ReadValueSafe(out int affinity);
            reader.ReadValueSafe(out int throwerSlot);

            SyncSlipperClientRpc(seatOfOrigin, ownerSlot, inPlay, holderSlot, pos, rot, state,
                                 velocity, pektusSpin, affinity, throwerSlot);
        }

        // ⚠️ BOTH POSE HANDLERS APPLY A POSITION AND REFUSE TO TOUCH ANYTHING ELSE, which is what
        // makes them safe to receive out of order with respect to the reliable channel. See the
        // § note above `BroadcastLataStateIfChanged` and `Slipper.ApplySnapshotPose`.

        private void OnLataPoseMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (NetAuthority.IsHost) return;

            reader.ReadValueSafe(out Vector3 pos);
            reader.ReadValueSafe(out Quaternion rot);

            GameServices.Round?.Lata?.ApplySnapshotPose(pos, rot);
        }

        private void OnSlipperPoseMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (NetAuthority.IsHost) return;

            reader.ReadValueSafe(out int seatOfOrigin);
            reader.ReadValueSafe(out Vector3 pos);
            reader.ReadValueSafe(out Quaternion rot);
            reader.ReadValueSafe(out Vector3 velocity);

            if (!ValidSlot(seatOfOrigin)) return;

            FindSlipper(seatOfOrigin)?.ApplySnapshotPose(pos, rot, velocity);
        }

        // -------------------------------------------------------------------
        // LATE JOIN AND DISCONNECT
        // -------------------------------------------------------------------

        public void HostLateJoin(int peerId)
        {
            if (!NetAuthority.IsHost) return;

            // ⚠️⚠️ THE SEAT HANDOVER IS NOT GATED BY `_spawned`, AND IT USED TO BE. That set
            // exists to send the world snapshot ONCE, which is a bandwidth question. Handing a
            // chair over is a correctness one, and it was sharing the same early return: a peer
            // whose id was already in the set skipped the whole method, so the host kept its own
            // `AIController` on that chair and went on transmitting its copy at 50 Hz over
            // whatever the arriving player submitted. The client moves for one frame and is
            // snapped back forever, which reads as a body that cannot move at all.
            //
            // ⚠️ IT IS REACHABLE. `HandleIdentify` calls this on EVERY identify, not only the
            // first, and `_spawned` is cleared only by `HostPeerLeft`. A second identify from a
            // live peer, or a reconnect that reuses a client id before the old one is retired,
            // both land on it.
            //
            // ⚠️ AND THE HANDOVER IS IDEMPOTENT, which is what makes running it every time free:
            // `Destroy` on a component that is already gone does not happen because of the null
            // check, `IsBot = false` is a write of the same value, and `ForgetInputSource` only
            // invalidates a cache. `docs/TODO.md` § 62.2.
            HostTakeSeatBackFromBot(peerId);

            if (!_spawned.Add(peerId)) return;

            HostSyncPeer(peerId);
        }

        /// <summary>
        /// A peer has arrived and holds a chair: stop the host driving it. See `HostLateJoin`.
        /// </summary>
        private void HostTakeSeatBackFromBot(int peerId)
        {
            var lobby = NetSession.Instance?.Lobby;
            var peerRecord = lobby?.PeerById(peerId);
            if (peerRecord != null && peerRecord.Seat >= 0)
            {
                // The chair changes hands here too, so the same per-seat host bookkeeping
                // `HostPeerLeft` drops has to go: the arriving player must not inherit a movement
                // rate window or a reset channel opened by the bot that was sitting there.
                _resetChannelStart.Remove(peerRecord.Seat);
                _lastAcceptedMoveAt.Remove(peerRecord.Seat);

                var unit = Unit(peerRecord.Seat);
                if (unit != null)
                {
                    var ai = unit.GetComponent<AIController>();
                    if (ai != null) Destroy(ai);

                    unit.IsBot = false;
                    unit.PlayerName = peerRecord.Name;

                    // ⚠⚠ THE MIRROR OF `HostPeerLeft`'S CALL. The returning player drives this
                    // body now, so the host must STOP broadcasting it or its own stale copy
                    // fights the transforms that player is submitting at 50 Hz.
                    //
                    // ⚠️ `Destroy` IS DEFERRED TO THE END OF THE FRAME, so `GetComponent` would
                    // still answer "there is an AIController here" if the cache were rebuilt on
                    // this line. It is INVALIDATED here and rebuilt on the next physics step,
                    // by which time the component is really gone.
                    unit.ForgetInputSource();
                }
            }
        }

        /// <summary>
        /// Asks the host to rehydrate this process once its arena objects exist. Transport
        /// connection can finish before the client-controlled SceneFlow has built its seats, so
        /// the connection-time snapshot alone is not sufficient for a cold app relaunch.
        ///
        /// ⚠️ IT TRAVELS AS A NAMED MESSAGE, NOT AN [ServerRpc]. This component is a plain
        /// MonoBehaviour on the Relay path, so there is no NetworkBehaviour to carry one, and
        /// every other request in this file already goes through CustomMessagingManager.
        /// </summary>
        public void RequestWorldSnapshot()
        {
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            if (NetAuthority.IsHost)
            {
                HostSyncPeer((int)_nm.LocalClientId);
                return;
            }

            using var writer = new FastBufferWriter(sizeof(byte), Allocator.Temp);
            writer.WriteValueSafe((byte)0);
            _nm.CustomMessagingManager.SendNamedMessage("ReqSnapshot", NetworkManager.ServerClientId, writer);
        }

        /// <summary>
        /// ⚠️⚠️ A SNAPSHOT REQUEST FANS OUT TO EVERYBODY, SO IT NEEDS A FLOOR. `HostSyncPeer`
        /// ends in `BroadcastWorldSnapshot`, which writes the match state, the can, four
        /// slippers, four transforms and four ability kits to every peer. One client asking for
        /// that on every frame costs the host sixty full world snapshots a second times the peer
        /// count. Twice a second is far more than a cold rejoin needs and is unnoticeable.
        /// </summary>
        private const float SnapshotRequestInterval = 0.5f;

        private readonly Dictionary<ulong, float> _lastSnapshotRequest = new Dictionary<ulong, float>();

        private void OnReqSnapshotMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;

            float now = Time.realtimeSinceStartup;
            if (_lastSnapshotRequest.TryGetValue(senderClientId, out float last) &&
                now - last < SnapshotRequestInterval)
                return;

            _lastSnapshotRequest[senderClientId] = now;
            HostSyncPeer((int)senderClientId);
        }

        private IEnumerator RequestSnapshotWhenArenaReady()
        {
            // NetBootstrap starts transport and scene loading independently. Wait for the
            // client-owned arena to finish installing before asking the host to rehydrate it.
            // This also handles a slow disk or a cold app relaunch without timing guesses.
            while (_nm != null && _nm.IsClient && !NetAuthority.IsHost)
            {
                var round = GameServices.Round;
                if (round != null && round.Lata != null &&
                    round.Players.Count >= Balance.PlayerCount)
                {
                    yield return null; // let camera and HUD finish their Start methods
                    RequestWorldSnapshot();
                    yield break;
                }

                yield return null;
            }
        }

        private void HostSyncPeer(int peerId)
        {
            if (!NetAuthority.IsHost) return;

            var lobby = NetSession.Instance?.Lobby;
            var peerRecord = lobby?.PeerById(peerId);
            if (peerRecord != null && peerRecord.Seat >= 0)
            {
                var match = GameServices.Match;
                var round = GameServices.Round;
                SendRebindLocalSeat(peerId,
                                    peerRecord.Seat,
                                    match != null ? match.DefenderSlot : -1,
                                    round != null && round.RoundActive,
                                    peerRecord.Name);
            }

            // The joiner needs the whole world state, not just its own seat. Broadcast is
            // intentionally idempotent and also repairs any packet-lagged observer.
            BroadcastWorldSnapshot();
        }

        /// <summary>
        /// One player saying they are done reading the role swap. See `BufferSkipVote`.
        ///
        /// ⚠️ IT RETURNS WHETHER THE VOTE REACHED THE WIRE, like `DeclareReadyServerRpc` and for
        /// the same reason: `IsListening` is true from `StartClient` and not from approval, so a
        /// press made during the join window has nowhere to go and the caller has to know that
        /// rather than believe it voted.
        /// </summary>
        public bool RequestSkipBufferServerRpc()
        {
            if (NetAuthority.IsHost)
            {
                FindFirstObjectByType<BufferSkipVote>()?.HostCastVote(NetAuthority.LocalPeerId);
                return true;
            }

            if (_nm == null || !_nm.IsListening || _nm.CustomMessagingManager == null) return false;

            // ⚠️ NO PAYLOAD AT ALL. The vote carries no data and the voter is `senderClientId`,
            // which the transport supplies and the sender cannot type. A placeholder byte here
            // made `tools/audit_wire_payloads.py` report a writer/reader mismatch, correctly:
            // one field written and none read is exactly the shape of a field somebody forgot to
            // parse, and the audit cannot tell a deliberate filler from that. `StartMatch` is the
            // precedent for a genuinely empty message.
            using var writer = new FastBufferWriter(8, Allocator.Temp);
            _nm.CustomMessagingManager.SendNamedMessage("SkipBuffer", NetworkManager.ServerClientId, writer);
            return true;
        }

        /// <summary>
        /// ⚠️ THE VOTER IS THE SENDER, NEVER A NUMBER IN THE PAYLOAD. A claimed peer id on this
        /// message would let one client vote on everybody else's behalf and end the intermission
        /// alone. `senderClientId` comes from the transport and cannot be typed by the sender,
        /// which is the same rule `SenderOwnsClaimedSeat` applies to every other request here.
        /// </summary>
        private void OnSkipBufferMsg(ulong senderClientId, FastBufferReader reader)
        {
            if (!NetAuthority.IsHost) return;

            FindFirstObjectByType<BufferSkipVote>()?.HostCastVote((int)senderClientId);
        }

        public void HostPeerLeft(int peerId)
        {
            if (!NetAuthority.IsHost) return;

            // ⚠️ THE SKIP VOTE HAS THE SAME HOLE AS THE READY GATE AND THE REMATCH VOTE: a peer
            // that quits mid-buffer drops the denominator, and with nobody re-evaluating the
            // players still waiting sit on a gate that is already satisfied.
            FindFirstObjectByType<BufferSkipVote>()?.OnPeerLeft(peerId);

            _spawned.Remove(peerId);

            // ⚠️ THE PER-PEER RATE BUDGETS ARE KEYED BY TRANSPORT ID AND MUST BE DROPPED WITH IT.
            // Client ids are handed out monotonically rather than reused, so a lobby that runs
            // all evening otherwise accumulates one dictionary entry per connection forever.
            _cueWindowStart.Remove((ulong)peerId);
            _cueWindowCount.Remove((ulong)peerId);
            _lastSnapshotRequest.Remove((ulong)peerId);

            // ⚠️ THE LOBBY TALLY HAS THE SAME HOLE `ReadyGate.OnPeerLeft` CLOSES. A peer that
            // quits after readying drops the expected count, and with nobody re-evaluating the
            // players still sitting there wait on a gate that is already satisfied.
            _lobbyReady.Remove(peerId);

            var lobby = NetSession.Instance?.Lobby;
            if (lobby != null)
            {
                // ⚠️⚠️ `Depart` IS CALLED EXACTLY ONCE, HERE, AND IT RETURNS THE RECORD IT
                // REMOVED. `NetSession.OnClientDisconnected` used to call it as well, one line
                // before this method: the FIRST call removed the peer and held the seat, so the
                // lookup here found nothing, `seat` was -1, and the bot takeover below never ran.
                // A player who dropped left a body nobody drove, which is a 1-vs-3 becoming a
                // 0-vs-3 for the rest of the round. Reading the seat off the return value is what
                // makes a second lookup impossible rather than merely unnecessary.
                var departed = lobby.Depart(peerId);
                int seat = departed != null ? departed.Seat : -1;

                if (seat >= 0)
                {
                    // Per-seat host bookkeeping belongs to whoever is driving the chair, and a
                    // bot is about to. A half-finished reset channel or a movement rate window
                    // left over from the peer that dropped would be applied to its replacement.
                    _resetChannelStart.Remove(seat);
                    _lastAcceptedMoveAt.Remove(seat);

                    var unit = Unit(seat);
                    if (unit != null)
                    {
                        if (AIController.BotsEnabled)
                        {
                            unit.IsBot = true;
                            if (unit.GetComponent<AIController>() == null)
                                unit.gameObject.AddComponent<AIController>();
                        }
                        else
                        {
                            // Bots-off lobbies reserve the chair for reconnection but do not
                            // install a replacement driver. Release any last remote input so the
                            // disconnected body cannot keep walking on a stale held key.
                            unit.Intent.Clear();
                            unit.Intent.CommitFrame();
                        }

                        // ⚠⚠ THE SEAT JUST CHANGED HANDS AND `CharacterMotor` CACHES WHO DRIVES IT.
                        // Without this the host keeps treating the body as remote-driven and
                        // never broadcasts its transform, so the bot that just took over is a
                        // statue on every client's screen. See `StepNetworkTransform`.
                        unit.ForgetInputSource();
                    }

                    // ⚠️⚠️ AND THE ROOM SAYS THE SEAT IS OPEN, WHICH IS THE WHOLE OF
                    // BACKFILL. `FUTURE.md` § 7 asks for exactly one behaviour, "a match that
                    // loses a player advertises the seat rather than dying", and everything under
                    // it was already built for the reconnect window: `LobbySession.Depart` holds
                    // the chair against the durable token and `RuleOnArrival` hands a free seat to
                    // a newcomer mid-match. **What was missing was that nothing told the outside
                    // world the chair existed**, because a lobby record with `InProgress` set is
                    // refused by `MatchmakingRules.Evaluate` unless it is also backfilling.
                    //
                    // ⚠️ ONLY WHILE A MATCH IS RUNNING. A seat opening in the LOBBY is
                    // already advertised by the seat count, and flagging a backfill there would
                    // put a room that never queued into the pool.
                    if (lobby.MatchInProgress)
                        FindFirstObjectByType<Matchmaker>()?.OfferBackfillSeat(true);
                }
            }

            FindFirstObjectByType<ReadyGate>()?.OnPeerLeft(peerId);

            // ⚠️ AND THE LOBBY TALLY IS REDRAWN, in the lobby only: in a match the pre-round gate
            // on the line above owns this question. A peer leaving changes both halves of
            // "n of m ready", so without this the remaining screens keep the departed peer in
            // their denominator.
            //
            // ⚠️ IT NO LONGER STARTS THE MATCH. It used to, on the reasoning that a departure can
            // satisfy a gate nobody else can now move; that reasoning belonged to a gate that
            // started matches, and READY does not start matches any more. A peer quitting the
            // lobby dropping three other people into an arena is the same surprise from a worse
            // direction. See the LOBBY READY GATE section above.
            if (lobby != null && !lobby.MatchInProgress && FindFirstObjectByType<ReadyGate>() == null)
            {
                BroadcastReadyTally();
            }

            // ⚠️ THE REMATCH VOTE HAS THE SAME HOLE AND IS CLOSED AT THE SAME PLACE. A peer that
            // quits from the result screen drops the expected count, and with nobody
            // re-evaluating, the players still watching wait forever on a gate that is already
            // satisfied. See MatchResult.OnPeerLeft.
            FindFirstObjectByType<UI.MatchResult>()?.OnPeerLeft(peerId);

            BroadcastWorldSnapshot();
        }

        /// <summary>
        /// The host tells only the reconnecting process which seat it controls. The world
        /// bodies are scene objects rather than NetworkObjects, so Netcode cannot transfer
        /// ownership for us; input, camera, HUD, and role presentation must be rebound as one
        /// atomic operation.
        /// </summary>
        private void SendRebindLocalSeat(int peerId, int seat, int defenderSlot,
                                         bool roundActive, string playerName)
        {
            if (_nm == null || _nm.CustomMessagingManager == null) return;

            if ((ulong)peerId == _nm.LocalClientId)
            {
                ApplyRebindLocalSeat(seat, defenderSlot, roundActive, playerName);
                return;
            }

            using var writer = new FastBufferWriter(256, Allocator.Temp);
            writer.WriteValueSafe(seat);
            writer.WriteValueSafe(defenderSlot);
            writer.WriteValueSafe(roundActive);
            writer.WriteValueSafe(playerName ?? "");
            _nm.CustomMessagingManager.SendNamedMessage("RebindSeat", (ulong)peerId, writer);
        }

        private void OnRebindSeatMsg(ulong senderClientId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int seat);
            reader.ReadValueSafe(out int defenderSlot);
            reader.ReadValueSafe(out bool roundActive);
            reader.ReadValueSafe(out string playerName);

            ApplyRebindLocalSeat(seat, defenderSlot, roundActive, playerName);
        }

        private void ApplyRebindLocalSeat(int seat, int defenderSlot, bool roundActive,
                                          string playerName)
        {
            var net = NetSession.Instance;
            net?.ApplyAssignedSeat(seat);

            var round = GameServices.Round;
            if (round == null || seat < 0) return;

            CharacterMotor local = null;
            foreach (var unit in round.Players)
            {
                if (unit == null) continue;

                unit.IsDefender = unit.PlayerSlot == defenderSlot;

                // ⚠️⚠️ `RoundActive` IS NOT WRITTEN HERE ANY MORE, AND IT IS THE SECOND HALF OF
                // THE BUG § 62.2 FIXED. `CharacterMotor.RoundActive` defaults to TRUE and that
                // default is what makes the pre-round free-roam window work: the director says
                // the round is not active, correctly, while the bodies say they may act, so
                // everybody can walk around the arena they are about to play in. Steering is
                // gated on `CanAct()`, which is `RoundActive && !IsStunned`, so a body with the
                // flag off cannot move a centimetre.
                //
                // This line stamped the host's `roundActive` onto all four bodies with no regard
                // for whether a match had started, and the log shows it running on a client
                // immediately after the arena installs. § 62.2 fixed the OTHER writer,
                // `RoundDirector.ApplySnapshot`, and the client still could not move: 🧑
                // 2026-08-27, on the very next build, *"i can move as host now yes, but u cant
                // move as non host again"*.
                //
                // ⚠️ ONE OWNER. `RoundDirector.ApplySnapshot` owns round state, arrives at 5 Hz,
                // and carries the `matchInProgress` gate that makes it agree with the host. A
                // second writer for one fact is what produced §§ 53.1, 57.1, 60 and 62.1 as well;
                // this is the fifth time in one evening and the answer is the same every time.
                var reader = unit.GetComponent<PlayerInputReader>();
                if (unit.PlayerSlot == seat)
                {
                    local = unit;
                    unit.IsBot = false;
                    unit.PlayerName = playerName;

                    var ai = unit.GetComponent<AIController>();
                    if (ai != null)
                    {
                        ai.enabled = false;
                        Destroy(ai);
                    }

                    if (reader == null) unit.gameObject.AddComponent<PlayerInputReader>();
                    else reader.enabled = true;
                }
                else if (reader != null)
                {
                    reader.enabled = false;
                    Destroy(reader);
                }

                unit.GetComponentInChildren<Visual.CharacterNameplate>()?.Refresh();
            }

            if (local == null) return;

            var spectator = FindFirstObjectByType<CameraSystem.SpectatorCamera>();
            if (spectator != null) spectator.enabled = false;

            var camera = UnityEngine.Camera.main;
            var rig = camera != null ? camera.GetComponent<CameraSystem.CameraRig>() : null;
            if (rig == null) rig = FindFirstObjectByType<CameraSystem.CameraRig>();
            if (rig != null)
            {
                rig.Follow(local);
                rig.SetAimSource(CameraSystem.AimSource.Mouse);
                rig.SetActive(true);
            }

            UI.Hud.Instance?.Bind(local);
            var youCard = FindFirstObjectByType<UI.YouCard>();
            if (youCard != null)
            {
                youCard.Bind(local);
                youCard.Refresh();
            }

            var pause = FindFirstObjectByType<PauseWatcher>();
            if (pause != null) pause.Local = local;

            foreach (var slipper in FindObjectsByType<Slipper>(FindObjectsSortMode.None))
                slipper.SetOwnerGlow(slipper.OwnerSlot == seat);
        }

        private readonly HashSet<int> _spawned = new HashSet<int>();
    }
}
