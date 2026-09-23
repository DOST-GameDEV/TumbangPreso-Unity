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
        private ReadyGate _lastReadyGate;
        private bool _rematchSent;
        private bool _rematchObserved;
        private float _readyStableFor;
        private float _resultStableFor;
        private float _lobbyStableFor;
        private bool _lobbyStartSent;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ConfigureReviewRules()
        {
            var args=Environment.GetCommandLineArgs();
            int rounds=IntArgument(args,"-tp-review-rounds"),seconds=IntArgument(args,"-tp-review-seconds");
            if(!Has(args,AutoRematchSwitch) || Has(args,"-tp-tournament") || rounds<=0 || seconds<=0)return;
            int at=Array.IndexOf(args,"-tp-review-mode");
            bool classic=at>=0&&at+1<args.Length&&string.Equals(args[at+1],"classic",StringComparison.OrdinalIgnoreCase);
            var rules=Core.CustomGameRules.Defaults(classic?Core.GameMode.Classic:Core.GameMode.HeroStrike);
            rules.Rounds=Mathf.Clamp(rounds,Core.CustomGameRules.MinRounds,Core.CustomGameRules.MaxRounds);
            rules.RoundSeconds=Mathf.Clamp(seconds,Core.CustomGameRules.MinRoundSeconds,Core.CustomGameRules.MaxRoundSeconds);
            rules.ManualReady=!Has(args,"-tp-review-automatic-arrival");
            SceneFlow.PinSelectedRules(rules);
            Debug.Log("[NetAuto] Explicit review rules: "+Core.CustomGameRules.ToWire(rules));
        }

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
            StepLobby();
            StepReady();
            StepRematch();
        }

        private void StepLobby()
        {
            // The same START GAME action as the native custom lobby. Direct-arena matrix
            // runs have no hub and retain their original ready-only automation.
            if (_expectedPeers <= 0 || _lobbyStartSent || !NetAuthority.IsNetworked || !NetAuthority.IsHost) return;
            var hub = UI.Hub.TumpHub.Current;
            if (hub == null || !(hub.Top is UI.Hub.HubLobby) || !hub.Canvas.enabled ||
                NetSession.Instance.Lobby.PlayingPeerCount() < _expectedPeers)
            { _lobbyStableFor = 0; return; }
            _lobbyStableFor += Time.unscaledDeltaTime;
            if (_lobbyStableFor < SettleSeconds) return;
            _lobbyStartSent = true;
            Debug.Log("[NetAuto] START GAME from the visible hub lobby.");
            hub.Host.StartGame();
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
            if (_expectedPeers <= 0 || !NetAuthority.IsNetworked) return;
            // Automatic rooms must prove ReadyGate's own post-introduction acknowledgement.
            // A diagnostic READY press here would conceal a broken automatic arrival.
            if (!SceneFlow.SelectedRules.ManualReady) return;

            var net = NetSession.Instance;
            var gate = FindFirstObjectByType<ReadyGate>();
            if(gate!=_lastReadyGate){_lastReadyGate=gate;_readySent=false;_readyStableFor=0;}
            if(_readySent)return;
            if (net == null || gate == null || !gate.AwaitingReady)
            {
                _readyStableFor = 0.0f;
                return;
            }

            int playing = net.Lobby.PlayingPeerCount();
            if (NetAuthority.IsHost && playing < _expectedPeers)
            {
                _readyStableFor = 0.0f;
                return;
            }

            _readyStableFor += Time.unscaledDeltaTime;
            if (_readyStableFor < SettleSeconds) return;

            // ⚠️ A PRESS THAT WAS NOT DELIVERED IS NOT A PRESS. The client half of this ran
            // before connection approval finished on the first two-process run of it, and logged
            // a submission the host never received. `DeclareReadyServerRpc` reports delivery now,
            // so the probe holds the press and tries again on the next tick.
            if (MatchRpc.Instance == null || !MatchRpc.Instance.DeclareReadyServerRpc())
            {
                _readyStableFor = SettleSeconds;
                return;
            }

            _readySent = true;

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
                if (result.IsVisible || GameServices.Round==null || !GameServices.Round.RoundActive) return;
                var gate=FindFirstObjectByType<ReadyGate>();
                if(gate!=null && (gate.AwaitingReady||gate.CountingDown))return;

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
