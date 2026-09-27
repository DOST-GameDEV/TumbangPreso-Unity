using System.Collections.Generic;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TumbangPreso
{
    /// <summary>
    /// Lets the players agree to end the intermission early instead of watching a 15 second
    /// clock run down.
    ///
    /// ⚠️⚠️ 🧑 2026-08-29: *"vote to skip buffer time"*. `Balance.WarmupBufferDuration` was 15 s
    /// and it runs between every round, so a four round Classic match spends 45 s of its length
    /// with nobody playing. The buffer is not padding, it exists so the role swap can be read and
    /// the next taya can find their mark, but that is a job which is finished the moment
    /// everybody has understood it, and how long that takes is the players' answer rather than a
    /// constant's.
    ///
    /// ⚠️ IT IS A VOTE, NOT A BUTTON, AND UNANIMOUS RATHER THAN A MAJORITY. Ending the
    /// intermission early takes reading time away from whoever has not finished reading, and the
    /// one player who most needs that time is the one who just became the taya. A majority can
    /// outvote exactly that person. Waiting for everybody costs a few seconds when somebody is
    /// slow and never robs anyone; the clock is still there as the backstop, so a player who
    /// never presses anything loses nothing at all.
    ///
    /// ⚠️⚠️ IT COUNTS PEERS, NEVER CHARACTERS, and this is the trap `ReadyGate`'s header already
    /// records from the other side. A match always has four bodies because empty seats are
    /// bot-filled, and a bot cannot press a key: counting characters leaves a solo host waiting
    /// forever for three bots to agree. Spectators are excluded for the same reason, they hold no
    /// seat and can never vote. `LobbySession.PlayingPeerCount` is the one source.
    ///
    /// ⚠️ THE VOTE IS TALLIED ON THE HOST AND NOWHERE ELSE. Advancing a round is a decision, and
    /// `CLAUDE.md` § 4 keeps decisions on one machine. A client presses a key and says so; it
    /// does not get to conclude anything from its own press.
    /// </summary>
    public sealed class BufferSkipVote : MonoBehaviour
    {
        /// <summary>
        /// Who has voted, by peer id. Host-side, and a set so mashing the key cannot vote twice.
        /// </summary>
        private readonly HashSet<int> _votes = new HashSet<int>();

        private InputAction _readyUp;
        private bool _sendPending;
        private bool _votedLocally;
        private bool _hasTally;
        private float _nextSend;
        private long _scopeMatch;
        private int _scopeRound = -1;
        private byte _voteMask;

        /// <summary>What the HUD draws. 0 of 0 while there is no buffer running.</summary>
        public static int Votes { get; private set; }
        public static int VotesNeeded { get; private set; }

        /// <summary>True while a vote is worth showing: a live buffer with more than one voter,
        /// or any buffer at all offline.</summary>
        public static bool Showing { get; private set; }

        private void Awake()
        {
            // ⚠️ THE SAME ASSET AND THE SAME ACTION NAME `ReadyGate` USES, deliberately. READY is
            // already the key that means "I am done waiting, get on with it", it is already in
            // the rebind panel under ROUND AND SCREEN, and `Hud` already draws its live label.
            // A second key for the same sentence is a second thing to teach and a second thing to
            // rebind. ⚠️ The two can never fire together: `ReadyGate` only listens during the
            // pre-round window and this only during an intermission.
            //
            // ⚠️⚠️ `"Player"`, AND IT SHIPPED AS `"Gameplay"`, WHICH IS A MAP THAT DOES NOT
            // EXIST. 🧑 2026-08-29: *"r skip doesnt work too"*. `TumbangPreso.inputactions` has
            // exactly ONE action map and it is called `Player`; every other resolver in the game
            // asks for that name (`ReadyGate`, `Hud`, `EmoteWheel`, `Rebinding`,
            // `AbilityInspectPanel`, `SpectatorCamera`, `PlayerInputReader`). This one asked for
            // `Gameplay`, got null back, and `FindAction` was never reached — so `_readyUp` was
            // null for the entire lifetime of the component and `Update`'s
            // `if (_readyUp == null ... ) return;` swallowed every press in silence.
            //
            // ⚠️ `throwIfNotFound: false` IS WHAT MADE IT SILENT, and it is still false, because
            // `PlayerInputReader` is the one resolver that should throw and it already passes
            // true. The guard against a repeat is that the name now matches the seven other call
            // sites, so a rename of the map breaks all eight together instead of leaving one
            // behind.
            var asset = Resources.Load<InputActionAsset>("TumbangPreso");
            var map = asset != null ? asset.FindActionMap("Player", false) : null;
            _readyUp = map != null ? map.FindAction("ReadyUp", false) : null;

            // ⚠️ ENABLED HERE TOO, LIKE `ReadyGate.Awake`. An action that is resolved but not
            // enabled never reports a press either, so fixing only the name would have moved the
            // silence one line down. Enabling a map twice is a no-op.
            map?.Enable();
        }

        private void OnEnable()
        {
            if (GameServices.Match != null)
                GameServices.Match.IntermissionStarted += OnIntermission;
        }

        private void OnDisable()
        {
            if (GameServices.Match != null)
                GameServices.Match.IntermissionStarted -= OnIntermission;

            Showing = false;
        }

        private void OnIntermission(int nextRound, int nextDefenderSlot)
        {
            _scopeRound = -1;
            EnsureScope();
            PublishTally();
        }

        private void EnsureScope()
        {
            var match = GameServices.Match;
            long identity = match?.PresentationMatchId ?? 0;
            int round = match?.RoundNumber ?? -1;
            if (_scopeMatch == identity && _scopeRound == round) return;
            _scopeMatch = identity; _scopeRound = round;
            _votes.Clear(); _votedLocally = _sendPending = _hasTally = false;
            _nextSend = 0; _voteMask = 0; Votes = VotesNeeded = 0;
        }

        private void Update()
        {
            var match = GameServices.Match;

            if (match == null || !match.IsWarmupBuffer || HalftimePresentation.Playing)
            {
                Showing = false;
                if (match == null || !match.IsWarmupBuffer) _sendPending = false;
                return;
            }

            EnsureScope();
            if (NetAuthority.ShouldResolve() && RefreshTally()) PublishTally();
            Showing = !GameLaunch.Spectator && _hasTally && !UI.Hub.HubLoading.Visible &&
                (NetAuthority.IsNetworked || NetAuthority.ShouldResolve());
            if (!Showing) return;

            // ⚠️ A HELD VOTE IS RETRIED, for the reason `ReadyGate._readySendPending` exists:
            // `NetAuthority.IsNetworked` is true from `StartClient` onward rather than from
            // approval, so a press made during the join window goes to a transport with nowhere
            // to send it and would otherwise be swallowed silently.
            if (_sendPending && Time.unscaledTime >= _nextSend)
            {
                _nextSend = Time.unscaledTime + .5f;
                // A local send is not acceptance. The host's seated vote mask
                // acknowledges this vote; duplicates stay free on the host.
                MatchRpc.Instance?.RequestSkipBufferServerRpc();
            }

            if (_votedLocally) return;
            if (_readyUp == null || !_readyUp.WasPressedThisFrame()) return;

            _votedLocally = true;

            if (!NetAuthority.IsNetworked)
            {
                // Solo: there is nobody to agree with, so the press IS the decision.
                match.SkipBuffer();
                return;
            }

            _sendPending = true;
            _nextSend = 0;
        }

        /// <summary>
        /// Host-side. Records one peer's vote and ends the buffer once everybody has voted.
        /// </summary>
        public void HostCastVote(int peerId)
        {
            if (!NetAuthority.ShouldResolve() || !EligiblePeer(peerId)) return;

            var match = GameServices.Match;
            if (match == null || !match.IsWarmupBuffer || HalftimePresentation.Playing) return;

            EnsureScope();
            if (!_votes.Add(peerId)) return;
            PublishTally();

            if (Votes < VotesNeeded) return;

            match.SkipBuffer();
        }

        /// <summary>
        /// ⚠️ A PEER THAT LEAVES MID-BUFFER MUST NOT HOLD THE VOTE OPEN. Same hole
        /// `ReadyGate.OnPeerLeft` and `MatchResult.OnPeerLeft` close: the denominator drops and
        /// nothing re-evaluates, so the remaining players wait on a gate that is already
        /// satisfied. Called from `MatchRpc.HostPeerLeft`.
        /// </summary>
        public void OnPeerLeft(int peerId)
        {
            if (!NetAuthority.ShouldResolve()) return;

            EnsureScope();
            _votes.Remove(peerId);

            var match = GameServices.Match;
            if (match == null || !match.IsWarmupBuffer || HalftimePresentation.Playing) return;

            PublishTally();

            if (Votes > 0 && Votes >= VotesNeeded) match.SkipBuffer();
        }

        private static bool EligiblePeer(int peerId) => NetAuthority.IsNetworked
            ? NetSession.Instance?.Lobby.IsSeatedPeer(peerId) == true
            : peerId == NetAuthority.LocalPeerId && !GameLaunch.Spectator;

        private bool RefreshTally()
        {
            int count = 0; byte mask = 0;
            foreach (int peer in _votes)
            {
                if (!EligiblePeer(peer)) continue;
                count++;
                int seat = NetSession.Instance?.Lobby.PeerById(peer)?.Seat ?? NetAuthority.LocalSlot;
                if (seat >= 0 && seat < Core.Balance.PlayerCount) mask |= (byte)(1 << seat);
            }
            int needed = Needed();
            bool changed = !_hasTally || Votes != count || VotesNeeded != needed || _voteMask != mask;
            Votes = count; VotesNeeded = needed; _voteMask = mask; _hasTally = true;
            if (_votes.Contains(NetAuthority.LocalPeerId)) _sendPending = false;
            return changed;
        }

        public void PublishTally(ulong? peer = null)
        {
            if (!NetAuthority.ShouldResolve() || GameServices.Match?.IsWarmupBuffer != true) return;
            EnsureScope(); RefreshTally();
            MatchRpc.Instance?.BroadcastBufferVotes(Votes, VotesNeeded, _voteMask, peer);
        }

        public void ApplyNetworkTally(int votes, int needed, byte mask)
        {
            if (NetAuthority.IsHost || GameServices.Match?.IsWarmupBuffer != true) return;
            EnsureScope(); Votes = votes; VotesNeeded = needed; _voteMask = mask; _hasTally = true;
            int seat = NetAuthority.LocalSlot;
            if (seat >= 0 && seat < Core.Balance.PlayerCount && (mask & (1 << seat)) != 0)
            { _sendPending = false; _votedLocally = true; }
        }

        private static int Needed()
        {
            if (!NetAuthority.IsNetworked) return 1;

            var lobby = NetSession.Instance?.Lobby;
            if (lobby == null) return 1;

            return Mathf.Max(1, lobby.SeatedPeerCount());
        }
    }
}
