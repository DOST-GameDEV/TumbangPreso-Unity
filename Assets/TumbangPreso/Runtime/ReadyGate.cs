using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TumbangPreso
{
    /// <summary>
    /// The pre-round free-roam window and the 3 · 2 · 1 that follows it, converted from the
    /// ready-phase half of `main.gd` (roughly lines 1036-1195).
    ///
    /// 2026-07-28: *"add a 3 2 1 timer before each match starts too."* Everyone is already
    /// spawned and free to wander; pressing READY starts a countdown, and the round only
    /// actually begins when the countdown finishes — not on the press itself. Whoever
    /// wandered off gets teleported back to their role spawn the instant the round begins,
    /// the same way an ordinary intermission between rounds already does.
    ///
    /// ⚠️ THE COUNTDOWN GUARD IS NOT COSMETIC. Without <see cref="_countingDown"/>, a second
    /// READY press mid-count restarts it, and a player mashing R never starts the round at all.
    ///
    /// ⚠️⚠️ THE NETWORKED GATE COUNTS PEERS, NEVER CHARACTERS. A match always has four
    /// characters — the empty seats are bot-filled — and an AI cannot press R, so counting
    /// characters leaves a solo host waiting forever for three bots to agree. Spectators are
    /// excluded for the same reason: they hold no seat and can never press. The count comes
    /// from `LobbySession.PlayingPeerCount` and nowhere else.
    ///
    /// ⚠️ A SECOND PRESS IS IDEMPOTENT. Votes go into a set, so mashing R cannot ready you
    /// twice or start the countdown early.
    /// </summary>
    public sealed class ReadyGate : MonoBehaviour
    {
        /// <summary>Raised when the countdown completes and the round should actually start.
        /// In Godot this called `MatchManager.begin_next_round()`.</summary>
        public event Action RoundShouldBegin;

        /// <summary>Ticks as they should appear on the HUD: "3", "2", "1", then "GO!".</summary>
        public event Action<string> CountdownTick;

        /// <summary>Raised when the countdown display should clear.</summary>
        public event Action CountdownHidden;

        /// <summary>Raised with true when the "press READY" prompt should show.</summary>
        public event Action<bool> ReadyPromptChanged;

        /// <summary>The body-language read on the ready press. Purely visual: the countdown
        /// and the round start are unchanged, this just means the OTHER players can see it
        /// happen in the world instead of only on a HUD.</summary>
        public event Action<CharacterMotor> ReadyGestureRequested;

        public const float TickSeconds = 1.0f;
        public const float GoSeconds = 0.5f;

        private bool _awaitingLocalReady;
        private bool _countingDown;

        /// <summary>Peers that have declared ready. Host-side; a set, so a second press from
        /// the same peer changes nothing.</summary>
        private readonly System.Collections.Generic.HashSet<int> _netReady =
            new System.Collections.Generic.HashSet<int>();

        /// <summary>Raised with (ready, expected) whenever the tally moves, so a screen can
        /// show "2 / 3 ready" without polling.</summary>
        public event Action<int, int> NetReadyChanged;

        public bool AwaitingNetReady { get; private set; }

        /// <summary>HOST ONLY. Opens the networked phase and clears any stale votes.</summary>
        public void OpenNetworked()
        {
            if (!NetAuthority.IsHost) return;

            _netReady.Clear();
            AwaitingNetReady = true;
            _awaitingLocalReady = true;
            _countingDown = false;

            ReadyPromptChanged?.Invoke(true);
            RaiseNetReady();
        }

        /// <summary>
        /// Any peer to the host: "I am ready."
        ///
        /// ⚠️ THE HOST'S OWN PRESS ARRIVES HERE TOO, and in Godot it came through with a
        /// sender id of 0 rather than the host's real id. Resolve it at the door rather than
        /// adding a second code path, or the host can never satisfy its own gate.
        /// </summary>
        public void DeclareReady(int peerId)
        {
            if (!NetAuthority.IsHost || !AwaitingNetReady) return;

            if (peerId == 0) peerId = NetAuthority.LocalSlot;

            if (!_netReady.Add(peerId)) return;   // idempotent

            RaiseNetReady();

            if (_netReady.Count >= ExpectedReadyCount()) BeginNetCountdown();
        }

        /// <summary>
        /// ⚠️ RE-CHECKED WHENEVER A PEER LEAVES, NOT ONLY WHEN ONE PRESSES. A peer that
        /// disconnects mid-vote drops the expected count, and if nobody re-evaluates, the
        /// remaining players sit on a gate that is already satisfied.
        /// </summary>
        public void OnPeerLeft(int peerId)
        {
            if (!NetAuthority.IsHost || !AwaitingNetReady) return;

            _netReady.Remove(peerId);
            RaiseNetReady();

            if (_netReady.Count >= ExpectedReadyCount()) BeginNetCountdown();
        }

        private int ExpectedReadyCount()
        {
            var lobby = Net.NetSession.Instance?.Lobby;
            return lobby?.PlayingPeerCount(NetAuthority.LocalSlot) ?? 1;
        }

        private void RaiseNetReady()
            => NetReadyChanged?.Invoke(_netReady.Count, ExpectedReadyCount());

        private void BeginNetCountdown()
        {
            if (_countingDown) return;

            AwaitingNetReady = false;

            // Last-chance pick sweep before match starts: broadcast full table to late joiners and peers
            Net.MatchRpc.Instance?.BroadcastPicks();
            Net.MatchRpc.Instance?.BeginCountdownClientRpc();

            StartCoroutine(RunReadyCountdown());
        }

        /// <summary>Starts the 3, 2, 1, GO countdown locally on clients.</summary>
        public void StartLocalCountdown()
        {
            if (_countingDown) return;
            _awaitingLocalReady = false;
            AwaitingNetReady = false;
            StartCoroutine(RunReadyCountdown());
        }

        private InputAction _readyUp;
        private CharacterMotor _local;

        public bool AwaitingReady => _awaitingLocalReady;
        public bool CountingDown => _countingDown;

        /// <summary>Open the free-roam window. The round does not start until READY.</summary>
        public void Open(CharacterMotor local)
        {
            _local = local;
            _awaitingLocalReady = true;
            _countingDown = false;
            ReadyPromptChanged?.Invoke(true);
        }

        private void Awake()
        {
            var asset = Resources.Load<InputActionAsset>("TumbangPreso");
            var map = asset != null ? asset.FindActionMap("Player", false) : null;
            _readyUp = map != null ? map.FindAction("ReadyUp", false) : null;
            map?.Enable();
        }

        private void Update()
        {
            if (_countingDown || !_awaitingLocalReady) return;
            if (_readyUp == null || !_readyUp.WasPressedThisFrame()) return;

            if (_local != null) ReadyGestureRequested?.Invoke(_local);

            // Networked: the press is a VOTE, and the countdown waits for everyone. Solo: the
            // press IS the start.
            if (NetAuthority.IsNetworked)
            {
                if (NetAuthority.IsHost) DeclareReady(NetAuthority.LocalSlot);
                else Net.MatchRpc.Instance?.DeclareReadyServerRpc(NetAuthority.LocalSlot);
            }
            else
            {
                StartCoroutine(RunReadyCountdown());
            }
        }

        /// <summary>
        /// Runs once, between the ready press and the round actually starting.
        ///
        /// ⚠️ EVERY PEER RUNS THIS, NOT JUST THE HOST, once netcode lands. The round start is
        /// host-gated downstream, so a client running the same countdown gets the 3 · 2 · 1
        /// for free; syncing only the finished result would give a client no countdown at all.
        /// </summary>
        private IEnumerator RunReadyCountdown()
        {
            _countingDown = true;
            ReadyPromptChanged?.Invoke(false);

            foreach (var tick in new[] { "3", "2", "1" })
            {
                CountdownTick?.Invoke(tick);
                yield return new WaitForSeconds(TickSeconds);
            }

            CountdownTick?.Invoke("GO!");
            yield return new WaitForSeconds(GoSeconds);

            CountdownHidden?.Invoke();
            _awaitingLocalReady = false;
            _countingDown = false;

            RoundShouldBegin?.Invoke();
        }
    }
}
