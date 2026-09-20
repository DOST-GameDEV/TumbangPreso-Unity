using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    // Opt-in presentation delivery witness. No request guards or wire contracts
    // are changed; the host resolves one legal punch through the existing verb.
    [DefaultExecutionOrder(-250)]
    public sealed class NetCatchPresentationProbe : MonoBehaviour
    {
        private StreamWriter _writer;
        private float _started, _next;
        private bool _prepared, _punched, _shot;
        private int _tags;
        private CatchReconstruction _view;
        private static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args, key);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            if (Argument("-tp-catchtrace") == null) return;
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            string path = Argument("-tp-catchtrace"); if (path == null) return;
            var go = new GameObject("~NetCatchPresentationProbe"); DontDestroyOnLoad(go);
            var probe = go.AddComponent<NetCatchPresentationProbe>(); probe._started = Time.realtimeSinceStartup;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            probe._writer = new StreamWriter(path) { AutoFlush = true };
            probe._writer.WriteLine("real,elapsed,local,playing,stun,tayaCanAct,tags,tayaX,tayaZ,victimX,victimZ,score,holding,taggable,mode");
        }
        private void OnEnable() => MatchFlair.Presented += OnMoment;
        private void OnDisable() { MatchFlair.Presented -= OnMoment; _writer?.Dispose(); _writer = null; }
        private void OnMoment(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        { if (kind == MatchFlair.Kind.Tag && actor == 0 && subject == 1) _tags++; }
        private void Update()
        {
            if (Time.realtimeSinceStartup - _started > 35) { Application.Quit(); return; }
            var round = GameServices.Round; var match = GameServices.Match;
            if (!NetAuthority.IsNetworked || round == null || match == null) return;
            foreach (var ai in FindObjectsByType<AIController>()) ai.enabled = false;
            foreach (var input in FindObjectsByType<PlayerInputReader>()) input.enabled = false;
            if (!round.RoundActive || match.IsWarmupBuffer) return;
            var taya = round.PlayerAt(0); var victim = round.PlayerAt(1);
            if (taya == null || victim == null || round.Lata == null) return;
            float elapsed = SceneFlow.SelectedRoundSeconds - round.TimeLeft;
            int local = NetAuthority.LocalSlot;
            if (!int.TryParse(Argument("-tp-catchseat"), out int expectedSeat) || local != expectedSeat) return;
            var ready = FindAnyObjectByType<ReadyGate>();
            if (ready != null && ready.CountingDown) return;
            if (elapsed < 3) return;
            foreach (var actor in round.Players) if (actor != null) { actor.Intent.Clear(); actor.Intent.Parked = false; }
            if (!_prepared)
            {
                _prepared = true; Vector3 can = round.Lata.transform.position;
                if (NetAuthority.IsHost)
                { taya.Teleport(can + Vector3.back * 3.2f); taya.transform.forward = Vector3.forward; }
                for (int i = 2; i < 4; i++)
                    if (round.PlayerAt(i) != null && (NetAuthority.IsHost || local == i))
                        round.PlayerAt(i).Teleport(can + new Vector3(-6, 0, 3 + i));
            }
            // The owner walks through normal movement replication. A staged
            // teleport could be overwritten by initial seating/round snapshots.
            if (local == 1 && elapsed < 10)
            {
                Vector3 toward = round.Lata.transform.position + Vector3.back * 2.2f - victim.transform.position;
                toward.y = 0; victim.transform.forward = Vector3.forward;
                if (toward.sqrMagnitude > .015f)
                    victim.Intent.Move = new Vector2(toward.normalized.x, toward.normalized.z);
            }
            if (NetAuthority.IsHost && !_punched && elapsed >= 10)
            {
                _punched = true;
                bool accepted = taya.GetComponent<CombatVerbs>().HostResolvePunch(taya.transform.position, Vector3.forward);
                Debug.Log("[CatchPeer] host punch accepted=" + accepted + " victimStun=" + victim.StunLeft + " held=" + victim.HoldingSlipper + " at=" + victim.transform.position);
            }
            if (NetAuthority.IsHost && elapsed >= 10.1f && elapsed < 10.8f) taya.Intent.Move = Vector2.up;
            if (_view == null) _view = FindAnyObjectByType<CatchReconstruction>();
            bool playing = _view != null && _view.Playing;
            if (playing && !_shot) { _shot = true; StartCoroutine(Shot()); }
            if (_writer == null || Time.realtimeSinceStartup < _next) return;
            _next = Time.realtimeSinceStartup + .04f;
            Vector3 a = taya.transform.position, b = victim.transform.position;
            _writer.WriteLine(FormattableString.Invariant($"{Time.realtimeSinceStartup-_started:F4},{elapsed:F4},{local},{(playing?1:0)},{victim.StunLeft:F4},{(taya.CanAct()?1:0)},{_tags},{a.x:F4},{a.z:F4},{b.x:F4},{b.z:F4},{match.ScoreFor(0)},{(victim.HoldingSlipper?1:0)},{(victim.IsTaggable()?1:0)},{(int)SceneFlow.SelectedMode}"));
        }
        private IEnumerator Shot()
        {
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.ChangeExtension(Argument("-tp-catchtrace"), ".png"), image.EncodeToPNG()); Destroy(image);
        }
    }
}
