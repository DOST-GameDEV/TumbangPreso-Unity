using System;
using System.Collections;
using System.IO;
using System.Linq;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Diagnostics
{
    // Actual transport and per-peer ownership, with a standardized world camera.
    // No owner-glow flag is injected. The fixture only parks actors and loose shoes.
    public sealed class NetRecallRenderProbe : MonoBehaviour
    {
        [Serializable] private sealed class Receipt
        {
            public bool passed, spectator;
            public string error, mode;
            public int local, expected, changedPixels, width = 1280, height = 720;
            public long epoch;
            public int[] owners;
            public bool[] drawing, shaded;
            public float[] positions;
            public float changedFraction;
            public string scope = "Actual local peers and ordinary ownership; standardized world-camera A/B, not a human or WAN review.";
        }
        private string _path;
        private int _expected;
        private float _started, _stableAt = -1;
        private bool _staged, _spectateAsked, _capturing, _done, _settingApplied;
        private static string Argument(string name)
        { var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, name); return at >= 0 && at + 1 < args.Length ? args[at + 1] : null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            if (Argument("-tp-recall-render") == null) return;
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(Argument("-tp-recall-mode") == "hero" ? GameMode.HeroStrike : GameMode.Classic));
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            string path = Argument("-tp-recall-render"); if (path == null) return;
            var root = new GameObject("~NetRecallRenderProbe"); DontDestroyOnLoad(root);
            var probe = root.AddComponent<NetRecallRenderProbe>(); probe._path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(probe._path));
            probe._expected = int.Parse(Argument("-tp-recall-seat") ?? "0"); probe._started = Time.realtimeSinceStartup;
        }
        private static Vector3 At(int owner) => owner == 3 ? new Vector3(0, .1f, 1.5f)
            : new Vector3(owner == 1 ? -1.5f : 1.5f, .1f, -1);
        private void Update()
        {
            if (_capturing) return;
            try
            {
                if (!_done && Time.realtimeSinceStartup - _started > 75) throw new InvalidOperationException("Recall peer did not reach its capture state.");
                var rpc = MatchRpc.Instance; var round = GameServices.Round;
                if (!NetAuthority.IsNetworked || rpc == null || round == null) return;
                if (_expected < 0 && NetAuthority.LocalSlot >= 0 && !_spectateAsked
                    && NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
                { _spectateAsked = true; rpc.RequestSeatServerRpc(-1); }
                foreach (var brain in FindObjectsByType<AIController>()) brain.enabled = false;
                foreach (var input in FindObjectsByType<PlayerInputReader>()) input.enabled = false;
                foreach (var switcher in FindObjectsByType<DebugPlayerSwitcher>()) switcher.enabled = false;
                foreach (var actor in round.Players) { actor.Intent.Clear(); actor.Intent.Parked = true; }
                if (_done) return;
                if (!round.RoundActive || GameServices.Match.IsWarmupBuffer || NetAuthority.LocalSlot != _expected) return;
                var ready = FindAnyObjectByType<ReadyGate>(); if (ready != null && ready.CountingDown) return;
                if (!_settingApplied)
                {
                    SettingsStore.Current.SlipperHighlight = SlipperHighlights.Default;
                    SettingsStore.RaiseSlipperHighlightChanged(); _settingApplied = true;
                }
                if (NetAuthority.IsHost && !_staged)
                {
                    if (NetworkManager.Singleton.ConnectedClientsIds.Count < 4) return;
                    foreach (var actor in round.Players) actor.Teleport(new Vector3(6, .12f, 4 + actor.PlayerSlot));
                    foreach (int owner in new[] { 1, 2, 3 })
                    {
                        var shoe = FindObjectsByType<Slipper>(FindObjectsInactive.Include).Single(s => s.SeatOfOrigin == owner);
                        shoe.HostDisarm(); shoe.gameObject.SetActive(true); shoe.transform.rotation = Quaternion.identity;
                        var at = At(owner); at.y = Slipper.GroundY(at) + shoe.RestHeight; shoe.transform.position = at;
                    }
                    _staged = true; rpc.BroadcastWorldSnapshot();
                }
                var shoes = FindObjectsByType<Slipper>().Where(s => s.SeatOfOrigin >= 1 && s.SeatOfOrigin <= 3)
                    .OrderBy(s => s.SeatOfOrigin).ToArray();
                if (shoes.Length != 3 || shoes.Any(s => s.State != SlipperState.Loose
                    || Vector2.Distance(new Vector2(s.transform.position.x, s.transform.position.z),
                        new Vector2(At(s.SeatOfOrigin).x, At(s.SeatOfOrigin).z)) > .15f))
                { _stableAt = -1; return; }
                if (_stableAt < 0) _stableAt = Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup - _stableAt < 1.2f) return;
                _capturing = true; StartCoroutine(Capture(shoes));
            }
            catch (Exception error) { Complete(new Receipt { expected = _expected, local = NetAuthority.LocalSlot,
                spectator = GameLaunch.Spectator, error = error.ToString() }); }
        }
        private IEnumerator Capture(Slipper[] shoes)
        {
            yield return new WaitForEndOfFrame();
            var receipt = new Receipt { expected = _expected, local = NetAuthority.LocalSlot, spectator = GameLaunch.Spectator,
                mode = GameServices.Round.PlayerAt(1).Mode.ToString(), epoch = GameServices.Match.PresentationMatchId,
                owners = shoes.Select(s => s.OwnerSlot).ToArray(), positions = shoes.SelectMany(s => new[] { s.transform.position.x, s.transform.position.y, s.transform.position.z }).ToArray() };
            try
            {
                var beams = shoes.Select(s => s.GetComponent<SlipperBeam>()).ToArray();
                receipt.drawing = beams.Select(b => b != null && b.Drawing).ToArray();
                receipt.shaded = beams.Select(b => b != null && b.Shaded).ToArray();
                for (int i = 0; i < shoes.Length; i++)
                {
                    bool expected = receipt.local == receipt.owners[i] && !receipt.spectator;
                    if (receipt.drawing[i] != expected || (expected && !receipt.shaded[i]))
                        throw new InvalidOperationException("Incorrect peer beam ownership/shader for shoe " + receipt.owners[i]);
                }
                receipt.changedPixels = RenderPair(beams, receipt.drawing);
                receipt.changedFraction = receipt.changedPixels / (float)(receipt.width * receipt.height);
                bool shouldDraw = receipt.drawing.Any(x => x);
                if (shouldDraw ? receipt.changedPixels < 8 || receipt.changedFraction >= .12f : receipt.changedPixels != 0)
                    throw new InvalidOperationException("Peer beam pixels do not match visible ownership: " + receipt.changedPixels);
                receipt.passed = true;
            }
            catch (Exception error) { receipt.error = error.ToString(); }
            Complete(receipt);
        }
        private int RenderPair(SlipperBeam[] beams, bool[] drawing)
        {
            var camera = Camera.main; if (camera == null) throw new InvalidOperationException("No real peer camera.");
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            var position = camera.transform.position; var rotation = camera.transform.rotation;
            float fov = camera.fieldOfView, aspect = camera.aspect; bool ortho = camera.orthographic;
            var arms = FindObjectsByType<ViewmodelArms>().SelectMany(a => a.GetComponentsInChildren<Renderer>(true))
                .Select(r => (renderer: r, enabled: r.enabled)).ToArray();
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var colours = beams.Select(b => b != null ? b.Colour : Color.white).ToArray();
            string stem = Path.Combine(Path.GetDirectoryName(_path), Path.GetFileNameWithoutExtension(_path));
            try
            {
                foreach (var arm in arms) arm.renderer.enabled = false;
                camera.transform.position = new Vector3(0, 2.7f, -6);
                camera.transform.LookAt(new Vector3(0, .35f, -1)); camera.fieldOfView = 60;
                camera.orthographic = false; camera.aspect = 1280f / 720; camera.targetTexture = target;
                camera.Render(); // Settle camera-dependent render state before the same-frame pair.
                for (int i = 0; i < beams.Length; i++) if (beams[i] != null) beams[i].Set(false, colours[i]);
                camera.Render(); RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
                var before = texture.GetPixels32(); File.WriteAllBytes(stem + "-beam-off.png", texture.EncodeToPNG());
                for (int i = 0; i < beams.Length; i++) if (beams[i] != null) beams[i].Set(drawing[i], colours[i]);
                camera.Render(); RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
                var after = texture.GetPixels32(); File.WriteAllBytes(stem + "-beam-on.png", texture.EncodeToPNG());
                int changed = 0;
                for (int i = 0; i < before.Length; i++)
                    if (Mathf.Abs(before[i].r - after[i].r) + Mathf.Abs(before[i].g - after[i].g) + Mathf.Abs(before[i].b - after[i].b) > 9) changed++;
                return changed;
            }
            finally
            {
                for (int i = 0; i < beams.Length; i++) if (beams[i] != null) beams[i].Set(drawing[i], colours[i]);
                foreach (var arm in arms) if (arm.renderer != null) arm.renderer.enabled = arm.enabled;
                camera.transform.SetPositionAndRotation(position, rotation); camera.fieldOfView = fov;
                camera.aspect = aspect; camera.orthographic = ortho; camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
                target.Release(); Destroy(target); Destroy(texture);
            }
        }
        private void Complete(Receipt receipt)
        {
            _done = true; _capturing = false;
            File.WriteAllText(_path, JsonUtility.ToJson(receipt, true));
            Debug.Log("[NetRecallRender] " + (receipt.passed ? "PASS" : "FAIL " + receipt.error));
        }
    }
}
