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
    /// ⚠️⚠️ 🧑 2026-08-29: *"vote to skip buffer time"*. `Balance.WarmupBufferDuration` is 15 s
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
            var asset = Resources.Load<InputActionAsset>("TumbangPreso");
            var map = asset != null ? asset.FindActionMap("Gameplay", false) : null;
            _readyUp = map != null ? map.FindAction("ReadyUp", false) : null;
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
            _votes.Clear();
            _votedLocally = false;
            _sendPending = false;
        }

        private void Update()
        {
            var match = GameServices.Match;

            if (match == null || !match.IsWarmupBuffer)
            {
                Showing = false;
                return;
            }

            VotesNeeded = Needed();
            Votes = NetAuthority.IsNetworked ? _votes.Count : (_votedLocally ? 1 : 0);
            Showing = true;

            // ⚠️ A HELD VOTE IS RETRIED, for the reason `ReadyGate._readySendPending` exists:
            // `NetAuthority.IsNetworked` is true from `StartClient` onward rather than from
            // approval, so a press made during the join window goes to a transport with nowhere
            // to send it and would otherwise be swallowed silently.
            if (_sendPending && MatchRpc.Instance != null &&
                MatchRpc.Instance.RequestSkipBufferServerRpc())
            {
                _sendPending = false;
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

            _sendPending = MatchRpc.Instance == null ||
                           !MatchRpc.Instance.RequestSkipBufferServerRpc();
        }

        /// <summary>
        /// Host-side. Records one peer's vote and ends the buffer once everybody has voted.
        /// </summary>
        public void HostCastVote(int peerId)
        {
            if (!NetAuthority.IsHost) return;

            var match = GameServices.Match;
            if (match == null || !match.IsWarmupBuffer) return;

            _votes.Add(peerId);
            Votes = _votes.Count;
            VotesNeeded = Needed();

            if (_votes.Count < VotesNeeded) return;

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
            if (!NetAuthority.IsHost) return;

            _votes.Remove(peerId);

            var match = GameServices.Match;
            if (match == null || !match.IsWarmupBuffer) return;

            VotesNeeded = Needed();
            Votes = _votes.Count;

            if (_votes.Count > 0 && _votes.Count >= VotesNeeded) match.SkipBuffer();
        }

        private static int Needed()
        {
            if (!NetAuthority.IsNetworked) return 1;

            var lobby = NetSession.Instance?.Lobby;
            if (lobby == null) return 1;

            return Mathf.Max(1, lobby.PlayingPeerCount(NetAuthority.LocalPeerId));
        }
    }
}
