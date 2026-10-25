using System;
using System.Globalization;
using System.IO;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    // Explicit local-player transport fixture. Only its owner camera is posed;
    // gameplay state, ownership checks and received view data remain untouched.
    [DefaultExecutionOrder(31000)]
    public sealed class NetObservedViewProbe : MonoBehaviour
    {
        [Serializable] private sealed class Receipt
        {
            public string role, error; public int local, samples, published, received, relayed;
            public long match; public float maxEyeError, minFov = 999, maxFov, sentAt;
            public bool watched, unsubscribed;
        }
        private string _role, _path;
        private StreamWriter _csv;
        private float _started, _liveAt = -1, _sampleAt, _seatAt, _nextGesture;
        private CameraRig _ownerRig;
        private SpectatorCamera _observer;
        private ReadyGate _readyGate;
        private bool _startSent, _readySent;
        private readonly Receipt _receipt = new();
        private bool _unsubscribed, _finished;
        private static string Arg(string key)
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, key);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            if (Arg("-tp-viewtrace") == null || Array.IndexOf(Environment.GetCommandLineArgs(), "-tp-tournament") >= 0) return;
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tp-tournament") >= 0) return;
            string path = Arg("-tp-viewtrace"); if (path == null) return;
            var root = new GameObject("~NetObservedViewProbe"); DontDestroyOnLoad(root);
            var probe = root.AddComponent<NetObservedViewProbe>();
            probe._path = Path.GetFullPath(path); probe._role = Arg("-tp-viewrole") ?? "host";
            probe._receipt.role = probe._role; probe._started = Time.realtimeSinceStartup;
            Directory.CreateDirectory(Path.GetDirectoryName(probe._path));
            probe._csv = new StreamWriter(probe._path) { AutoFlush = true };
            probe._csv.WriteLine("time,role,match,round,slot,epoch,serial,sent,eye_x,eye_y,eye_z,pitch,yaw,roll,fov,action,aim,published,received,relayed,applied_error");
            root.AddComponent<NetObservedViewDriver>().Owner = probe;
        }
        private void Update()
        {
            if (_finished) return;
            if (Time.realtimeSinceStartup - _started > 160) { Finish("No complete live view before deadline."); return; }
            var rpc = MatchRpc.Instance; var net = NetSession.Instance;
            _receipt.local = NetAuthority.LocalSlot;
            if (_role == "observer" && rpc != null && net != null && NetAuthority.IsNetworked
                && !net.Lobby.MatchInProgress && !GameLaunch.Spectator && Time.unscaledTime >= _seatAt)
            { _seatAt = Time.unscaledTime + 1; rpc.RequestSeatServerRpc(-1); }
            // Use the real lobby seat action before starting. A running match correctly
            // refuses seat changes; the direct-arena bootstrap cannot set up this fixture.
            if (_role == "host" && !_startSent && rpc != null && net != null && NetAuthority.IsHost
                && !net.Lobby.MatchInProgress && net.Lobby.PlayingPeerCount() >= 2)
            {
                bool observerSeated = false;
                foreach (var peer in net.Lobby.Peers) observerSeated |= peer.Spectator && peer.Seat < 0;
                var hub = UI.Hub.TumpHub.Current;
                if (observerSeated && hub?.Top is UI.Hub.HubLobby && hub.Canvas.enabled)
                {
                    _startSent = true; hub.Host.SelectMap(SceneFlow.Eskinita); hub.Host.StartGame();
                    Debug.Log("[ViewProbe] Started from lobby with two players and a spectator.");
                }
            }
            var gate = FindFirstObjectByType<ReadyGate>();
            if (_readyGate != gate) { _readyGate = gate; _readySent = false; }
            if (!_readySent && gate != null && gate.AwaitingReady && rpc != null)
                _readySent = rpc.DeclareReadyServerRpc();
            if (GameServices.Round?.RoundActive != true || GameServices.Match?.IsWarmupBuffer == true || rpc == null) return;
            if (NetAuthority.IsHost)
                foreach (var actor in GameServices.Round.Players)
                { var brain = actor != null ? actor.GetComponent<AIController>() : null; if (brain != null) brain.enabled = false; }
            if (_role == "observer" && GameLaunch.Spectator)
            {
                if (_observer == null) _observer = FindAnyObjectByType<SpectatorCamera>();
                if (_observer != null && !_unsubscribed && _observer.FollowedPlayer?.PlayerSlot != 1) _observer.TryWatchPlayer(1);
                if (_observer != null && _liveAt >= 0 && Time.realtimeSinceStartup - _liveAt > 20 && !_unsubscribed)
                { _observer.ExecuteViewCommand(SpectatorCamera.ViewCommand.FreeFlight); _unsubscribed = true; _receipt.unsubscribed = true; }
            }
            bool flowing = _role == "owner" ? rpc.SpectatorViewsPublished > 0
                : _role == "observer" ? rpc.SpectatorViewsReceived > 0 : rpc.SpectatorViewsRelayed > 0;
            if (_liveAt < 0 && flowing) _liveAt = Time.realtimeSinceStartup;
            if (_liveAt >= 0 && Time.realtimeSinceStartup - _liveAt > 29) Finish(null);
        }
        internal void PoseOwnerCamera()
        {
            if (_finished || _role != "owner" || NetAuthority.LocalSlot != 1 || GameServices.Round?.RoundActive != true
                || PresentationClock.Held || SpectatorCutscene.Hides) return;
            var actor = GameServices.Round.PlayerAt(1); if (actor == null) return;
            if (_ownerRig == null || !_ownerRig.IsFollowing(actor))
                foreach (var rig in FindObjectsByType<CameraRig>()) if (rig.IsFollowing(actor)) { _ownerRig = rig; break; }
            if (_ownerRig?.Camera == null || !_ownerRig.Camera.enabled) return;
            float t = (float)(NetworkManager.Singleton?.ServerTime.Time ?? Time.unscaledTime);
            var eye = actor.transform.position + new Vector3(.09f * Mathf.Sin(t), 1.2f + .15f * Mathf.Sin(t * 1.1f), .08f * Mathf.Cos(t));
            _ownerRig.Camera.transform.SetPositionAndRotation(eye, Quaternion.Euler(20 + 13 * Mathf.Sin(t), 125 + 17 * Mathf.Cos(t * .7f), 7 * Mathf.Sin(t * .8f)));
            _ownerRig.Camera.fieldOfView = 91 + 5 * Mathf.Sin(t * .9f);
            if (Time.unscaledTime >= _nextGesture)
            { _nextGesture = Time.unscaledTime + 2; _ownerRig.GetComponentInChildren<ViewmodelArms>(true)?.PlayAction("grab"); }
        }
        private void LateUpdate()
        {
            if (_finished || _liveAt < 0 || Time.unscaledTime < _sampleAt || MatchRpc.Instance == null) return;
            _sampleAt = Time.unscaledTime + .1f;
            var actor = GameServices.Round?.PlayerAt(1); if (actor == null) return;
            var rpc = MatchRpc.Instance; SpectatorViewState state;
            bool has = _role == "owner" ? ObservedPlayerView.TryLocal(actor, out state) : ObservedPlayerView.TryGet(actor, out state, out _);
            if (!has || (_role == "observer" && _unsubscribed)) return;
            _receipt.samples++; _receipt.local = NetAuthority.LocalSlot; _receipt.match = state.Match;
            _receipt.published = rpc.SpectatorViewsPublished; _receipt.received = rpc.SpectatorViewsReceived; _receipt.relayed = rpc.SpectatorViewsRelayed;
            _receipt.minFov = Mathf.Min(_receipt.minFov, state.Fov); _receipt.maxFov = Mathf.Max(_receipt.maxFov, state.Fov);
            float error = _role == "observer" && _observer != null ? Vector3.Distance(state.Eye, _observer.transform.position) : 0;
            _receipt.maxEyeError = Mathf.Max(_receipt.maxEyeError, error); _receipt.sentAt = state.SentAt;
            _receipt.watched |= _role == "observer" && _observer != null && _observer.IsWatchingEyes(actor);
            var e = state.Rotation.eulerAngles;
            string F(float value) => value.ToString("F5", CultureInfo.InvariantCulture);
            _csv.WriteLine(string.Join(",", F(Time.unscaledTime), _role, state.Match, state.Round, state.Slot, state.Epoch,
                state.Sequence, F(state.SentAt), F(state.Eye.x), F(state.Eye.y), F(state.Eye.z), F(e.x), F(e.y), F(e.z), F(state.Fov),
                state.Action.ToString(), state.Aim.ToString(), rpc.SpectatorViewsPublished, rpc.SpectatorViewsReceived, rpc.SpectatorViewsRelayed, F(error)));
        }
        private void Finish(string error)
        {
            if (_finished) return; _finished = true; _receipt.error = error;
            _csv?.Dispose(); _csv = null; File.WriteAllText(_path + ".result.json", JsonUtility.ToJson(_receipt, true));
            Application.Quit(error == null ? 0 : 1);
        }
        private void OnDestroy() { _csv?.Dispose(); }
    }
    [DefaultExecutionOrder(15000)]
    public sealed class NetObservedViewDriver : MonoBehaviour
    {
        public NetObservedViewProbe Owner;
        private void LateUpdate() => Owner?.PoseOwnerCamera();
    }
}
