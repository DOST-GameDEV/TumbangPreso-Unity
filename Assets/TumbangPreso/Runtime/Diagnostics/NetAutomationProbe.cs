using System;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    /// <summary>
    /// Presses the real ready and rematch controls for multi-process verification runs.
    ///
    /// ⚠️⚠️ THIS IS A COMMAND-LINE DRIVER, NOT A SECOND MATCH PATH. Both actions enter through
    /// the same public methods a button uses, so the host still counts transport peers, the
    /// ready countdown still runs, and only `MatchDirector.StartMatch` starts a match. It is
    /// inactive unless a switch below is present.
    ///
    ///   -tp-autostart 2     wait for two playing peers, then each process presses READY
    ///   -tp-autorematch     press REMATCH when the real result board appears
    /// </summary>
    public sealed class NetAutomationProbe : MonoBehaviour
    {
        public const string AutoStartSwitch = "-tp-autostart";
        public const string AutoRematchSwitch = "-tp-autorematch";

        private const float SettleSeconds = 0.75f;

        private int _expectedPeers;
        private bool _autoRematch;
        private bool _readySent;
        private bool _rematchSent;
        private bool _rematchObserved;
        private float _readyStableFor;
        private float _resultStableFor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            string[] args;
            try { args = Environment.GetCommandLineArgs(); }
            catch { return; }

            int expectedPeers = IntArgument(args, AutoStartSwitch);
            bool autoRematch = Has(args, AutoRematchSwitch);
            if (expectedPeers <= 0 && !autoRematch) return;

            var go = new GameObject("~NetAutomationProbe")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            DontDestroyOnLoad(go);

            var probe = go.AddComponent<NetAutomationProbe>();
            probe._expectedPeers = expectedPeers;
            probe._autoRematch = autoRematch;
        }

        private void Update()
        {
            StepReady();
            StepRematch();
        }

        /// <summary>
        /// ⚠️⚠️ ONLY THE HOST CAN COUNT PEERS, SO ONLY THE HOST WAITS FOR THE COUNT. `LobbySession`
        /// is populated by the connection-approval path, which runs on the server: on a client the
        /// table is empty and `PlayingPeerCount` floors at 1 forever. Gating both processes on it
        /// deadlocked the run, because the client sat waiting for a second peer it can never see
        /// while the host sat waiting for a press the client was never going to send.
        ///
        /// ⚠️ A CLIENT PRESSING EARLY IS SAFE, and that is what makes the asymmetry sound. The
        /// host opens `AwaitingNetReady` in `MatchInstaller.BuildReadyGate` as it loads the arena,
        /// which happens before any client can finish connecting, and `DeclareReady` is a set add.
        /// The countdown still starts only when the host's own quorum is met.
        /// </summary>
        private void StepReady()
        {
            if (_readySent || _expectedPeers <= 0 || !NetAuthority.IsNetworked) return;

            var net = NetSession.Instance;
            var gate = FindFirstObjectByType<ReadyGate>();
            if (net == null || gate == null || !gate.AwaitingReady)
            {
                _readyStableFor = 0.0f;
                return;
            }

            int playing = net.Lobby.PlayingPeerCount(NetAuthority.LocalPeerId);
            if (NetAuthority.IsHost && playing < _expectedPeers)
            {
                _readyStableFor = 0.0f;
                return;
            }

            _readyStableFor += Time.unscaledDeltaTime;
            if (_readyStableFor < SettleSeconds) return;

            _readySent = true;
            MatchRpc.Instance?.DeclareReadyServerRpc();

            Debug.Log(NetAuthority.IsHost
                ? $"[NetAuto] READY submitted with {playing} playing peers."
                : "[NetAuto] READY submitted from a client peer.");
        }

        private void StepRematch()
        {
            if (!_autoRematch || _rematchObserved) return;

            var result = FindFirstObjectByType<MatchResult>();
            if (result == null) return;

            if (_rematchSent)
            {
                if (result.IsVisible) return;

                _rematchObserved = true;
                Debug.Log("[NetAuto] REMATCH began after the peer vote.");
                return;
            }

            if (!result.IsVisible)
            {
                _resultStableFor = 0.0f;
                return;
            }

            _resultStableFor += Time.unscaledDeltaTime;
            if (_resultStableFor < SettleSeconds) return;

            _rematchSent = true;
            result.RequestRematch();
            Debug.Log("[NetAuto] REMATCH vote submitted from the result board.");
        }

        private static bool Has(string[] args, string name)
        {
            if (args == null) return false;

            foreach (string value in args)
                if (string.Equals(value, name, StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        private static int IntArgument(string[] args, string name)
        {
            if (args == null) return 0;

            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) continue;
                return int.TryParse(args[i + 1], out int value) ? value : 0;
            }

            return 0;
        }
    }
}
