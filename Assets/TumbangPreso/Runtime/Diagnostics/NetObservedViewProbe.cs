using System;
using System.Globalization;
using System.IO;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    // Explicit transport fixture: posed owner camera; optional private-view mode
    // applies host statuses and draws an owner marker through existing presentation.
    // No movement, casts or scores are forced; ordinary packet ownership still applies.
    [DefaultExecutionOrder(31000)]
    public sealed class NetObservedViewProbe : MonoBehaviour
    {
        [Serializable] private sealed class Receipt
        {
            public string role, error; public int local, samples, published, received, relayed;
            public long match; public float maxEyeError, minFov = 999, maxFov, sentAt;
            public bool watched, unsubscribed;
            public int maxPhantoms, maxHexShoes, maxFakePlayers, maxFakeShoes, markerSamples, beaconSamples, renderedPhantoms, renderedMarkerSamples;
            public bool privateRequested, privateReleased;
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
        private bool _private;
        private float _privateStatusAt;
        private Visual.GroundReticle _ownerMarker;
        private bool _privateShot;
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
            probe._private = Array.IndexOf(Environment.GetCommandLineArgs(), "-tp-viewprivate") >= 0;
            probe._receipt.privateRequested = probe._private;
            Directory.CreateDirectory(Path.GetDirectoryName(probe._path));
            probe._csv = new StreamWriter(probe._path) { AutoFlush = true };
            probe._csv.WriteLine("time,role,match,round,slot,epoch,serial,sent,eye_x,eye_y,eye_z,pitch,yaw,roll,fov,action,aim,published,received,relayed,applied_error,phantoms,marker,private");
            root.AddComponent<NetObservedViewDriver>().Owner = probe;
        }
        private void Update()
        {
            if (_finished) return;
            if (_liveAt >= 0 && Time.realtimeSinceStartup - _liveAt > 29)
            {
                // End the observer first, then its owner, then the host. Simultaneous
                // timers let host shutdown interrupt a client's final receipt.
                string folder = Path.GetDirectoryName(_path);
                bool observerDone = File.Exists(Path.Combine(folder, "observer.csv.result.json"));
                bool ownerDone = File.Exists(Path.Combine(folder, "owner.csv.result.json"));
                if (_role == "observer" || (_role == "owner" && observerDone) || (_role == "host" && observerDone && ownerDone))
                { Finish(null); return; }
            }
            if (Time.realtimeSinceStartup - _started > 160) { Finish("No complete live view before deadline."); return; }
            if (_liveAt >= 0 && _role != "host" && NetworkManager.Singleton?.IsConnectedClient != true)
            { Finish("Peer link ended before fixture completion."); return; }
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
            {
                foreach (var actor in GameServices.Round.Players)
                { var brain = actor != null ? actor.GetComponent<AIController>() : null; if (brain != null) brain.enabled = false; }
                if (_private && Time.unscaledTime >= _privateStatusAt)
                {
                    _privateStatusAt = Time.unscaledTime + 6;
                    var subject = GameServices.Round.PlayerAt(1); subject?.ApplyHexed(); subject?.ApplyDisoriented();
                }
            }
            if (_role == "observer" && GameLaunch.Spectator)
            {
                if (_observer == null) _observer = FindAnyObjectByType<SpectatorCamera>();
                UI.Hud.Instance?.SetSpectatorControlsVisible(false); // Keep this fixture's world capture unobstructed.
                if (_observer != null && !_unsubscribed && _observer.FollowedPlayer?.PlayerSlot != 1) _observer.TryWatchPlayer(1);
                if (_observer != null && _liveAt >= 0 && Time.realtimeSinceStartup - _liveAt > 20 && !_unsubscribed)
                { _observer.ExecuteViewCommand(SpectatorCamera.ViewCommand.FreeFlight); _unsubscribed = true; _receipt.unsubscribed = true; }
                if (_unsubscribed && _observer != null && _observer.PrivatePhantomCount == 0 && !_observer.PrivateMarkerVisible)
                    _receipt.privateReleased = true;
            }
            bool flowing = _role == "owner" ? rpc.SpectatorViewsPublished > 0
                : _role == "observer" ? rpc.SpectatorViewsReceived > 0 : rpc.SpectatorViewsRelayed > 0;
            if (_liveAt < 0 && flowing) _liveAt = Time.realtimeSinceStartup;
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
            if (_private)
            {
                if (_ownerMarker == null) _ownerMarker = actor.GetComponentInChildren<Visual.GroundReticle>(true);
                if (_ownerMarker != null)
                {
                    Vector3 along = _ownerRig.Camera.transform.forward; along.y = 0; along.Normalize();
                    var point = actor.transform.position + along * 4 + Vector3.Cross(Vector3.up, along) * (.8f * Mathf.Sin(t * .8f));
                    _ownerMarker.SetStyle(Mathf.Sin(t * .5f) > 0 ? Visual.GroundReticle.Style.Maw : Visual.GroundReticle.Style.Ember);
                    _ownerMarker.SetBeacon(Mathf.Cos(t * .45f) > 0);
                    _ownerMarker.Show(point, 3 + .5f * Mathf.Sin(t), UI.UiTheme.HeroSpiritBright); _ownerMarker.Tick(0);
                }
            }
        }
        private void LateUpdate()
        {
            if (_finished || _liveAt < 0 || Time.unscaledTime < _sampleAt || MatchRpc.Instance == null) return;
            _sampleAt = Time.unscaledTime + .1f;
            var actor = GameServices.Round?.PlayerAt(1); if (actor == null) return;
            var rpc = MatchRpc.Instance; SpectatorViewState state;
            bool has;
            if (_role == "owner") { state = rpc.LastPublishedSpectatorView; has = state.IsValid; }
            else has = ObservedPlayerView.TryGet(actor, out state, out _);
            if (!has || (_role == "observer" && _unsubscribed)) return;
            _receipt.samples++; _receipt.local = NetAuthority.LocalSlot; _receipt.match = state.Match;
            _receipt.published = rpc.SpectatorViewsPublished; _receipt.received = rpc.SpectatorViewsReceived; _receipt.relayed = rpc.SpectatorViewsRelayed;
            _receipt.minFov = Mathf.Min(_receipt.minFov, state.Fov); _receipt.maxFov = Mathf.Max(_receipt.maxFov, state.Fov);
            float error = _role == "observer" && _observer != null ? Vector3.Distance(state.Eye, _observer.transform.position) : 0;
            _receipt.maxEyeError = Mathf.Max(_receipt.maxEyeError, error); _receipt.sentAt = state.SentAt;
            _receipt.watched |= _role == "observer" && _observer != null && _observer.IsWatchingEyes(actor);
            int hex = 0, players = 0, shoes = 0;
            foreach (var phantom in state.Private.Phantoms)
            {
                if (phantom.Kind == SpectatorPhantom.HexShoe) hex++;
                else if (phantom.Kind == SpectatorPhantom.DisorientedPlayer) players++;
                else if (phantom.Kind == SpectatorPhantom.DisorientedShoe) shoes++;
            }
            _receipt.maxPhantoms = Mathf.Max(_receipt.maxPhantoms, state.Private.Phantoms.Length);
            _receipt.maxHexShoes = Mathf.Max(_receipt.maxHexShoes, hex); _receipt.maxFakePlayers = Mathf.Max(_receipt.maxFakePlayers, players);
            _receipt.maxFakeShoes = Mathf.Max(_receipt.maxFakeShoes, shoes); if (state.Private.Marker.Present) _receipt.markerSamples++;
            if (state.Private.Marker.Present && state.Private.Marker.Beacon) _receipt.beaconSamples++;
            if (_role == "observer" && _observer != null)
            {
                _receipt.renderedPhantoms = Mathf.Max(_receipt.renderedPhantoms, _observer.PrivatePhantomCount);
                if (_observer.PrivateMarkerVisible) _receipt.renderedMarkerSamples++;
            }
            if (_private && !_privateShot && _role != "host" && state.Private.Phantoms.Length >= 7
                && state.Private.Marker.Present && Time.realtimeSinceStartup - _liveAt > 1)
            { _privateShot = true; ScreenCapture.CaptureScreenshot(_path + ".private.png"); }
            using var privateBytes = new FastBufferWriter(SpectatorPrivateView.MaxWireBytes, Allocator.Temp);
            state.Private.Write(privateBytes); string privateData = Convert.ToBase64String(privateBytes.ToArray());
            var e = state.Rotation.eulerAngles;
            string F(float value) => value.ToString("F5", CultureInfo.InvariantCulture);
            _csv.WriteLine(string.Join(",", F(Time.unscaledTime), _role, state.Match, state.Round, state.Slot, state.Epoch,
                state.Sequence, F(state.SentAt), F(state.Eye.x), F(state.Eye.y), F(state.Eye.z), F(e.x), F(e.y), F(e.z), F(state.Fov),
                state.Action.ToString(), state.Aim.ToString(), rpc.SpectatorViewsPublished, rpc.SpectatorViewsReceived, rpc.SpectatorViewsRelayed,
                F(error), state.Private.Phantoms.Length, state.Private.Marker.Present ? 1 : 0, privateData));
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
