using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// C3 in docs/CLAUDE_ENGINEERING_LANE.md, TODO 149.5: what a whole-scene slipper lookup
    /// actually costs in a real arena with four bots playing, measured rather than argued.
    ///
    /// Two halves. The unit cost of each query shape (active-only, include-inactive, with and
    /// without a sort mode) is timed in the loaded arena, beside the cost of scanning an
    /// already-held array of the same slippers, which is the best case any registry could reach.
    /// The call frequency needs every production call site counted, and this fixture does NOT
    /// edit production code to get it: when the scratch-copy meter from
    /// docs/reports/claude-engineering-2026-09-15/c3/instrument_slipper_lookups.py has been applied to a scratch copy, `TumbangPreso.LookupMeter`
    /// exists and its per-site table is printed; in the live checkout that half reports that the
    /// meter is absent rather than zero.
    ///
    /// The match is stepped at a fixed 1/60 s with time scale 1, the same ordinary-speed
    /// simulation BotBehaviourProbe uses, so per-second rates are per simulated second.
    /// </summary>
    [Category("WallClock")]
    public class SlipperLookupCostProbe
    {
        private const float FixedStep = 1.0f / 60.0f;
        private CustomRules _entryRules;
        private bool _entryPin, _entryBots;

        [UnitySetUp]
        public IEnumerator ResetWorldBefore()
        {
            _entryRules = UI.SceneFlow.SelectedRules.Clone();
            _entryPin = UI.SceneFlow.RulesPinned;
            _entryBots = GameLaunch.AllBots;
            yield return PlayModeWorld.Reset();
        }

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        [TearDown]
        public void TearDown()
        {
            if (_entryRules != null)
            {
                UI.SceneFlow.AdoptRemoteRules(_entryRules);
                if (_entryPin) UI.SceneFlow.PinSelectedRules(_entryRules); else UI.SceneFlow.UnpinSelectedRules();
                GameLaunch.AllBots = _entryBots;
            }
            Hitstop.End();
            Time.timeScale = 1.0f;
            Time.captureDeltaTime = 0.0f;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator HeroStrikeUnderTheBridge() => Measure(GameMode.HeroStrike, "IlalimNgTulay");

        [UnityTest, Timeout(600000)]
        public IEnumerator ClassicOnEskinita() => Measure(GameMode.Classic, "Eskinita");

        private IEnumerator Measure(GameMode mode, string map)
        {
            UI.SceneFlow.PinSelectedRules(CustomGameRules.Defaults(mode));
            GameLaunch.AllBots = true;
            UnityEngine.Random.InitState(20260823);
            Hitstop.End();
            Time.timeScale = 1.0f;

            var load = SceneManager.LoadSceneAsync(map, LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");
            for (int i = 0; i < 25; i++) yield return null;

            var match = GameServices.Match;
            var runner = Object.FindFirstObjectByType<SliceRunner>();
            Assert.IsNotNull(match, "The arena registered no match.");
            Assert.IsNotNull(runner, "The arena built no SliceRunner.");

            UnityEngine.Random.InitState(20260823);
            Time.captureDeltaTime = FixedStep;
            for (int i = 0; i < 120; i++) yield return null;

            var meter = System.Type.GetType("TumbangPreso.LookupMeter, TumbangPreso.Runtime")
                        ?? FindType("TumbangPreso.LookupMeter");
            meter?.GetMethod("Reset")?.Invoke(null, null);

            runner.Begin();

            var frameWall = Stopwatch.StartNew();
            int frames = 0;
            long gcBefore = System.GC.CollectionCount(0);
            while (match.MatchInProgress && frames < 64000)
            {
                frames++;
                yield return null;
            }
            frameWall.Stop();
            Time.captureDeltaTime = 0.0f;

            float simulated = frames * FixedStep;
            double wallMsPerFrame = frameWall.Elapsed.TotalMilliseconds / Mathf.Max(1, frames);

            var log = new StringBuilder();
            log.AppendLine($"slipper lookup cost  ·  {mode}  ·  {map}");
            log.AppendLine($"{frames} frames, {simulated:F1}s simulated at {FixedStep * 1000f:F1} ms, match ended {!match.MatchInProgress}");
            log.AppendLine($"mean wall time per frame (batchmode editor, whole frame incl. rendering) {wallMsPerFrame:F3} ms; gen0 collections {System.GC.CollectionCount(0) - gcBefore}");
            log.AppendLine($"scene counts: Slipper active {Object.FindObjectsByType<Slipper>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length}, " +
                           $"Slipper incl inactive {Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length}, " +
                           $"MonoBehaviour {Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Length}, " +
                           $"Transform {Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length}, " +
                           $"Component {Object.FindObjectsByType<Component>(FindObjectsSortMode.None).Length}");

            log.AppendLine();
            log.AppendLine("unit cost in this arena, 20000 calls each (mean microseconds, bytes per call):");
            Unit(log, "FindObjectsByType<Slipper>(Exclude)", () => Object.FindObjectsByType<Slipper>(FindObjectsInactive.Exclude));
            double bytesPerLookup = _lastUnitBytes;
            Unit(log, "FindObjectsByType<Slipper>(SortMode.None)", () => Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None));
            Unit(log, "FindObjectsByType<Slipper>(Exclude, None)", () => Object.FindObjectsByType<Slipper>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            Unit(log, "FindObjectsByType<Slipper>(Include)", () => Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include));
            Unit(log, "FindObjectsByType<Slipper>(Include, None)", () => Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            // Calibration: proves the allocation counter sees managed allocations in this runtime,
            // so a 0 B row above is a measurement rather than a blind counter.
            Unit(log, "calibration: new Slipper[4] (must report a non-zero size)", () => new Slipper[4]);
            var held = new List<Slipper>(Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            Unit(log, "registry best case: scan a held List<Slipper> with an active check", () =>
            {
                int n = 0;
                for (int i = 0; i < held.Count; i++) if (held[i] != null && held[i].isActiveAndEnabled) n++;
                return null;
            });

            log.AppendLine();
            if (meter == null)
            {
                log.AppendLine("call frequency: LookupMeter ABSENT (live checkout). Frequency is measured only in the instrumented scratch copy; see the C3 report.");
            }
            else
            {
                var sitesField = meter.GetField("Sites");
                var sites = (IDictionary)sitesField.GetValue(null);
                long calls = 0, ticks = 0, bytes = 0;
                var rows = new List<(string site, long calls, long ticks, long bytes, long returned)>();
                foreach (DictionaryEntry e in sites)
                {
                    var s = e.Value; var t = s.GetType();
                    long c = (long)t.GetField("Calls").GetValue(s), k = (long)t.GetField("Ticks").GetValue(s);
                    long b = (long)t.GetField("Bytes").GetValue(s), r = (long)t.GetField("Returned").GetValue(s);
                    rows.Add(((string)e.Key, c, k, b, r)); calls += c; ticks += k; bytes += b;
                }
                rows.Sort((a, b2) => b2.calls.CompareTo(a.calls));
                double msPerTick = 1000.0 / Stopwatch.Frequency;
                log.AppendLine("call frequency by production site (instrumented copy; us/call includes the meter's own Stopwatch overhead; B/call* is the unit-rate figure above):");
                log.AppendLine($"{"site",-44} {"calls",9} {"per sim s",10} {"per frame",10} {"us/call",8} {"ms total",9} {"B/call*",7} {"avg n",6}");
                foreach (var r in rows)
                    log.AppendLine($"{r.site,-44} {r.calls,9} {r.calls / simulated,10:F1} {(double)r.calls / frames,10:F3} {r.ticks * msPerTick * 1000.0 / Mathf.Max(1, (int)r.calls),8:F2} {r.ticks * msPerTick,9:F1} {bytesPerLookup,7:F0} {(double)r.returned / Mathf.Max(1, (int)r.calls),6:F2}");
                double totalMs = ticks * msPerTick;
                log.AppendLine($"TOTAL calls {calls} ({calls / simulated:F1}/sim s, {(double)calls / frames:F2}/frame), time {totalMs:F1} ms " +
                               $"({totalMs / frames:F4} ms/frame = {100.0 * totalMs / frames / wallMsPerFrame:F2}% of mean frame wall time), " +
                               $"allocation at the unit rate {calls * bytesPerLookup / 1024.0:F0} KiB ({calls * bytesPerLookup / simulated:F0} B/sim s)");
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText($"Logs/slipper-lookup-cost-{mode}-{map}.txt", log.ToString());
            Debug.Log(log.ToString());

            Assert.IsFalse(match.MatchInProgress, $"{mode} on {map}: the measured match did not finish.");
        }

        private static System.Type FindType(string name)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(name);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>
        /// ⚠️ ALLOCATION IS READ AS HEAP GROWTH OVER SMALL BATCHES, AND THE CALIBRATION ROW IS
        /// WHAT MAKES IT A MEASUREMENT. `GC.GetAllocatedBytesForCurrentThread` read 0 B even for a
        /// plain `new Slipper[4]` on this Mono, and pausing the collector throws in the editor.
        /// So each batch reads `GC.GetTotalMemory(false)` before and after 1000 calls, batches a
        /// collection shrank are discarded, and the median batch is reported per call.
        /// </summary>
        private static double _lastUnitBytes;

        private static void Unit(StringBuilder log, string label, System.Func<Slipper[]> query)
        {
            const int n = 20000;
            for (int i = 0; i < 200; i++) query();
            var watch = Stopwatch.StartNew();
            for (int i = 0; i < n; i++) query();
            watch.Stop();

            const int batch = 1000;
            var grown = new List<long>();
            for (int b = 0; b < 15; b++)
            {
                long before = System.GC.GetTotalMemory(false);
                for (int i = 0; i < batch; i++) query();
                long delta = System.GC.GetTotalMemory(false) - before;
                if (delta >= 0) grown.Add(delta);
            }
            grown.Sort();
            _lastUnitBytes = grown.Count > 0 ? (double)grown[grown.Count / 2] / batch : double.NaN;
            log.AppendLine($"  {label,-70} {watch.Elapsed.TotalMilliseconds * 1000.0 / n,8:F3} us  {_lastUnitBytes,6:F1} B  ({grown.Count}/15 batches without a collection)");
        }
    }
}
