using System;
using System.Collections;
using System.IO;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Diagnostics
{
    // Opt-in local peer qualification. Outcomes are staged host physical throws;
    // participant state and earned-moment delivery use the actual transport.
    public sealed class NetMomentProbe : MonoBehaviour
    {
        private StreamWriter _trace;
        private float _started, _next, _settledAt = -1;
        private MatchDirector _match;
        private int _moments, _first, _three, _five;
        private bool _running;
        private MatchMoment _last;
        private static string Argument(string key)
        { var args = Environment.GetCommandLineArgs(); int at = Array.IndexOf(args,key); return at >= 0 && at+1 < args.Length ? args[at+1] : null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tp-tournament") >= 0) return;
            if (Argument("-tp-momenttrace") == null) return;
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(Argument("-tp-momentmode") == "hero" ? GameMode.HeroStrike : GameMode.Classic));
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-tp-tournament") >= 0) return;
            string path = Argument("-tp-momenttrace"); if (path == null) return;
            var go = new GameObject("~NetMomentProbe"); DontDestroyOnLoad(go);
            var probe = go.AddComponent<NetMomentProbe>(); probe._started = Time.realtimeSinceStartup;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            probe._trace = new StreamWriter(path) { AutoFlush = true };
            probe._trace.WriteLine("real,local,round,epoch,score1,moments,first,three,five,banner,charge1");
        }
        private void OnDisable()
        { if (_match != null) _match.MomentPresented -= OnMoment; _trace?.Dispose(); _trace = null; }
        private void OnMoment(MatchMoment moment)
        {
            _moments++; _last = moment;
            if (moment.Kind == MatchMomentKind.FirstKnockdown) _first++;
            if (moment.Kind == MatchMomentKind.AccurateThree) _three++;
            if (moment.Kind == MatchMomentKind.AccurateFive) _five++;
            if (moment.Kind == MatchMomentKind.AccurateThree) StartCoroutine(Shot());
        }
        private IEnumerator Shot()
        {
            yield return new WaitForSecondsRealtime(.3f); yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.ChangeExtension(Argument("-tp-momenttrace"), ".png"), image.EncodeToPNG()); Destroy(image);
        }
        private void Update()
        {
            if (Time.realtimeSinceStartup - _started > 62) { Application.Quit(); return; }
            var match = GameServices.Match; var round = GameServices.Round;
            if (!NetAuthority.IsNetworked || match == null || round == null) return;
            if (_match != match)
            { if (_match != null) _match.MomentPresented -= OnMoment; _match = match; match.MomentPresented += OnMoment; }
            foreach (var ai in FindObjectsByType<AIController>()) ai.enabled = false;
            foreach (var reader in FindObjectsByType<PlayerInputReader>()) reader.enabled = false;
            foreach (var switcher in FindObjectsByType<DebugPlayerSwitcher>()) switcher.enabled = false;
            foreach (var actor in round.Players) { actor.Intent.Clear(); actor.Intent.Parked = true; }
            if (!round.RoundActive || match.IsWarmupBuffer || round.Lata == null) return;
            if (!int.TryParse(Argument("-tp-momentseat"), out int expected) || NetAuthority.LocalSlot != expected) return;
            var ready = FindAnyObjectByType<ReadyGate>(); if (ready != null && ready.CountingDown) return;
            if (SceneFlow.SelectedRoundSeconds - round.TimeLeft < 3) return;
            Hud.Instance?.ShowReadyPrompt(false);
            if (NetAuthority.IsHost && !_running) { _running = true; StartCoroutine(Throws()); }
            if (match.ScoreFor(1) >= 705 && _settledAt < 0) _settledAt = Time.realtimeSinceStartup;
            if (_settledAt > 0 && Time.realtimeSinceStartup - _settledAt > 6) { Application.Quit(); return; }
            if (_trace == null || Time.realtimeSinceStartup < _next) return;
            _next = Time.realtimeSinceStartup + .08f;
            var banner = FindAnyObjectByType<MatchMomentBanner>();
            float charge = round.PlayerAt(1)?.AbilitySystem?.Kit?.UltimateCharge ?? 0;
            _trace.WriteLine(FormattableString.Invariant($"{Time.realtimeSinceStartup-_started:F3},{NetAuthority.LocalSlot},{match.RoundNumber},{match.PresentationMatchId},{match.ScoreFor(1)},{_moments},{_first},{_three},{_five},{(banner != null && banner.Showing ? 1 : 0)},{charge:F3}"));
        }
        private IEnumerator Throws()
        {
            var round = GameServices.Round; var match = GameServices.Match;
            foreach (var actor in round.Players)
                if (actor.PlayerSlot == 0 || actor.IsBot) actor.Teleport(new Vector3(6, actor.transform.position.y, -5+actor.PlayerSlot*3));
            var thrower = round.PlayerAt(1); var shoe = FindObjectsByType<Slipper>().First(s => s.OwnerSlot == 1);
            for (int hit = 0; hit < 6; hit++)
            {
                if (!round.Lata.IsUpright) round.Lata.HostRestore();
                float until = Time.time + 4;
                while (round.Lata.IsProtected && Time.time < until) yield return null;
                if (!shoe.HostForceEquip(thrower)) throw new InvalidOperationException("Owned physical throw fixture could not equip");
                int serial = round.Lata.HostKnockdownSerial;
                shoe.HostThrow(thrower, round.Lata.transform.position + new Vector3(0,1.2f,-2), Vector3.forward*12);
                until = Time.time + 2;
                while (round.Lata.IsUpright && Time.time < until) yield return null;
                if (round.Lata.HostKnockdownSerial != serial+1) throw new InvalidOperationException("A staged physical throw did not produce one real knockdown");
                yield return new WaitForSecondsRealtime(.15f);
            }
            if (match.ScoreFor(1) != 705) throw new InvalidOperationException("Expected600base+105capped bonus, got " + match.ScoreFor(1));
            Net.MatchRpc.Instance.BroadcastWorldSnapshot();
            Net.MatchRpc.Instance.BroadcastMatchMoment(_last); Net.MatchRpc.Instance.BroadcastMatchMoment(_last);
            yield return new WaitForSecondsRealtime(2);
            round.EndRound(); match.AdvanceRound();
            Net.MatchRpc.Instance.BroadcastWorldSnapshot();
            Net.MatchRpc.Instance.BroadcastMatchMoment(_last); // Old-round recognition must be dropped.
        }
    }
}
