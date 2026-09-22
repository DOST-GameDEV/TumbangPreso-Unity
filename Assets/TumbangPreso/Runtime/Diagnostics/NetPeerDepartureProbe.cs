using System;
using System.Collections;
using System.IO;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    // Opt-in real transport review. The runner kills only its own departing process
    // for the drop case; the orderly case calls the real NetSession.Stop path.
    public sealed class NetPeerDepartureProbe : MonoBehaviour
    {
        [Serializable] private sealed class Receipt
        {
            public bool passed, spectator, visibleToast, bot;
            public int local, expected, notices;
            public long match;
            public string text, error, mode;
        }
        private string _path;
        private int _expected;
        private float _started;
        private bool _ready, _done, _capturing;
        private static string Arg(string key)
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, key);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            if (Environment.GetCommandLineArgs().Contains("-tp-tournament") || Arg("-tp-departure-review") == null) return;
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(Arg("-tp-review-mode") == "hero" ? GameMode.HeroStrike : GameMode.Classic));
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Environment.GetCommandLineArgs().Contains("-tp-tournament")) return;
            string path = Arg("-tp-departure-review"); if (path == null) return;
            var root = new GameObject("~PeerDepartureReview"); DontDestroyOnLoad(root);
            var probe = root.AddComponent<NetPeerDepartureProbe>(); probe._path = Path.GetFullPath(path);
            probe._expected = int.Parse(Arg("-tp-departure-seat") ?? "0"); probe._started = Time.realtimeSinceStartup;
            Directory.CreateDirectory(Path.GetDirectoryName(probe._path));
        }
        private void Update()
        {
            if (_done || _capturing) return;
            try
            {
                if (Time.realtimeSinceStartup - _started > 90) throw new InvalidOperationException("Peer-departure review timed out.");
                var rpc = MatchRpc.Instance; var round = GameServices.Round;
                if (!NetAuthority.IsNetworked || rpc == null || round == null || !round.RoundActive ||
                    GameServices.Match.IsWarmupBuffer || NetAuthority.LocalSlot != _expected || rpc.PresentationMatchId <= 0) return;
                foreach (var brain in FindObjectsByType<AIController>()) brain.enabled = false;
                foreach (var input in FindObjectsByType<PlayerInputReader>()) input.enabled = false;
                foreach (var switcher in FindObjectsByType<DebugPlayerSwitcher>()) switcher.enabled = false;
                foreach (var actor in round.Players) { actor.Intent.Clear(); actor.Intent.Parked = true; }
                if (!_ready)
                {
                    if (NetAuthority.IsHost && NetworkManager.Singleton.ConnectedClientsIds.Count < 5) return;
                    File.WriteAllText(_path + ".ready", rpc.PresentationMatchId.ToString()); _ready = true;
                }
                if (_expected == 1)
                {
                    if (File.Exists(_path + ".leave"))
                    {
                        _done = true; NetSession.Instance.Stop();
                        File.WriteAllText(_path + ".left", "Called real NetSession.Stop");
                    }
                    return;
                }
                if (rpc.PeerDepartureNotices == 0) return;
                _capturing = true; StartCoroutine(Capture(rpc));
            }
            catch (Exception error) { Finish(new Receipt { error = error.ToString() }); }
        }
        private IEnumerator Capture(MatchRpc rpc)
        {
            yield return new WaitForEndOfFrame();
            var row = new Receipt { local = NetAuthority.LocalSlot, expected = _expected,
                spectator = GameLaunch.Spectator, match = rpc.PresentationMatchId,
                mode = SceneFlow.SelectedMode.ToString(),
                notices = rpc.PeerDepartureNotices, text = rpc.LastPeerDepartureText,
                bot = GameServices.Round.PlayerAt(1)?.IsBot == true };
            row.visibleToast = FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None)
                .Any(text => text.text == row.text && text.isActiveAndEnabled && text.canvas != null && text.canvas.enabled);
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            if (image == null)
            {
                row.error = "No player backbuffer. Run the hidden-window player without -batchmode.";
                Finish(row); yield break;
            }
            File.WriteAllBytes(_path + ".png", image.EncodeToPNG()); Destroy(image);
            row.passed = row.notices == 1 && row.visibleToast && row.local == row.expected
                && (_expected != 0 || row.bot) && (_expected >= 0 || row.spectator);
            if (!row.passed) row.error = "The actual departure notice, visible HUD, seat or bot state did not match.";
            Finish(row);
        }
        private void Finish(Receipt row)
        { _done = true; File.WriteAllText(_path, JsonUtility.ToJson(row, true)); }
    }
}
