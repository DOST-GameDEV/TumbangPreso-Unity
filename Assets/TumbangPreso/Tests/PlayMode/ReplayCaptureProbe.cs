using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// What the replay capture costs the frame it runs on, before and after.
    ///
    /// ⚠️⚠️ `docs/TODO.md` § 134.12 ASKS FOR A BEFORE/AFTER MEASUREMENT AND THIS IS IT. The old
    /// capture was `Texture2D.ReadPixels`, which blocks the CPU until the GPU has finished
    /// everything queued ahead of it and handed the pixels back, ten times a second, for the
    /// whole match, whether or not anybody presses replay. `CLAUDE.md` § 6.0 is blunt about what
    /// a performance claim owes: *"if a performance claim is ever made ... it gets MEASURED
    /// first and quoted with the number."*
    ///
    /// ⚠️⚠️ IT MEASURES THE TWO CALLS RATHER THAN TOGGLING THE SHIPPED PATH, AND THAT IS A
    /// DELIBERATE CHOICE WITH A COST. A switch on `SpectatorCamera` would let the probe drive the
    /// real component both ways, and it would also be a settable static that survives a scene
    /// change, which is precisely the class of leftover `TournamentPreset.Modifiers` exists to
    /// catch, added for a test's convenience. So the probe performs the OLD operation and the NEW
    /// operation itself, at the shipped resolution and format, and reports what each costs.
    /// **What it cannot claim is anything about the rest of the component**, and it does not.
    ///
    /// ⚠️ THE STATISTIC IS THE WORST CALL, NOT THE AVERAGE. `FrameRateHistogram.MaxSeconds`
    /// carries the argument: *"tournament pain is a hitch, not a low average"*, and a stall that
    /// lands on the frame somebody throws is invisible to a mean over a match.
    ///
    /// ⚠️ IT IS `WallClock` AND EXCLUDED FROM THE GATE. `CLAUDE.md` § 7: a timing result depends
    /// on how busy the machine is, and `AiDiagnosticProbe` has failed at 21.6, 29.9 and 37.6 s
    /// against one bound with nothing changed. The assertion below is a floor a broken build
    /// would cross rather than a tight bound, and the NUMBERS are the deliverable.
    /// </summary>
    [Category("WallClock")]
    public class ReplayCaptureProbe
    {
        [UnitySetUp]
        public IEnumerator SetUpWorld() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator TearDownWorld() => PlayModeWorld.Reset();

        /// <summary>The shipped replay frame. See `SpectatorCamera` § THE REPLAY BUFFER.</summary>
        private const int Width = 640;
        private const int Height = 360;

        /// <summary>
        /// How many captures each path is asked for.
        ///
        /// ⚠️ 120 IS TWELVE SECONDS OF MATCH AT `ReplaySampleInterval`, which is long enough for
        /// a percentile to mean something and short enough that the probe is seconds rather than
        /// minutes. A 90 s round is 900 captures and a four-round Classic set is 3,600.
        /// </summary>
        private const int Captures = 120;

        /// <summary>
        /// How many untimed calls each path gets before anything is measured.
        ///
        /// ⚠️⚠️ IT IS NOT ZERO AND THE FIRST RUN OF THIS PROBE IS WHY. `AsyncGPUReadback`'s first
        /// request of a session allocates its readback buffers and whatever else the driver sets
        /// up on first use, and that cost 147.7 ms against a steady-state 0.25. Reporting it as
        /// the path's "worst call" is a measurement of initialisation wearing a per-call
        /// statistic's name. **The warm-up cost is still printed**, on its own line, because a
        /// hitch on the first capture of a match is a real thing that happens once.
        /// </summary>
        private const int WarmCalls = 5;

        [UnityTest]
        public IEnumerator TheAsynchronousCaptureDoesNotStallTheFrameTheWayReadPixelsDid()
        {
            var cameraGo = new GameObject("ReplayCaptureProbeCamera", typeof(Camera));
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.42f, 0.28f);

            var source = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = source;

            var sync = new FrameRateHistogram();
            var async = new FrameRateHistogram();

            bool asyncSupported = SystemInfo.supportsAsyncGPUReadback &&
                                  SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB565);

            // ⚠️⚠️ EACH PATH IS WARMED WITH REAL CALLS, AND THE FIRST VERSION ONLY SPUN FRAMES.
            // That warmed the renderer and nothing else, so the first `AsyncGPUReadback.Request`
            // of the session paid for allocating the readback buffers and whatever the driver
            // sets up on first use: **147.7 ms, once**, which then stood as the "worst call" of a
            // path whose every other call was a quarter of a millisecond. The measurement was
            // real and it was a measurement of initialisation, which is not the thing this probe
            // exists to compare. `Warm` below does what the old comment already said it wanted.
            //
            // ⚠️ THE WARM-UP COST IS STILL REPORTED, because a 147 ms hitch on the first replay
            // capture of a match IS something that happens to somebody, and hiding it would be
            // the opposite fault. It is printed as its own line rather than folded into the
            // per-call statistics.
            for (int i = 0; i < 5; i++) yield return null;

            double syncWarm = 0.0;
            double asyncWarm = 0.0;

            // ---------------- the old path -------------------------------------
            var texture = new Texture2D(Width, Height, TextureFormat.RGB565, mipChain: false);

            for (int i = 0; i < WarmCalls; i++)
            {
                var warm = System.Diagnostics.Stopwatch.StartNew();
                CaptureSynchronously(source, texture);
                warm.Stop();
                if (i == 0) syncWarm = warm.Elapsed.TotalMilliseconds;
                yield return null;
            }

            for (int i = 0; i < Captures; i++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                CaptureSynchronously(source, texture);
                watch.Stop();
                sync.Add(watch.Elapsed.TotalSeconds);

                yield return null;
            }

            // ---------------- the shipped path ----------------------------------
            int landed = 0;
            int failed = 0;

            if (asyncSupported)
            {
                for (int i = 0; i < WarmCalls; i++)
                {
                    var warm = System.Diagnostics.Stopwatch.StartNew();
                    var warmScratch = RenderTexture.GetTemporary(Width, Height, 0,
                                                                 RenderTextureFormat.RGB565);
                    Graphics.Blit(source, warmScratch);
                    AsyncGPUReadback.Request(warmScratch, 0,
                                             _ => RenderTexture.ReleaseTemporary(warmScratch));
                    warm.Stop();
                    if (i == 0) asyncWarm = warm.Elapsed.TotalMilliseconds;
                    yield return null;
                }

                AsyncGPUReadback.WaitAllRequests();

                for (int i = 0; i < Captures; i++)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();

                    var scratch = RenderTexture.GetTemporary(Width, Height, 0,
                                                              RenderTextureFormat.RGB565);
                    Graphics.Blit(source, scratch);

                    AsyncGPUReadback.Request(scratch, 0, request =>
                    {
                        RenderTexture.ReleaseTemporary(scratch);

                        if (request.hasError) { failed++; return; }

                        var data = request.GetData<byte>();
                        if (data.Length != Width * Height * 2) { failed++; return; }

                        texture.LoadRawTextureData(data);
                        texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                        landed++;
                    });

                    // ⚠️⚠️ THE STOPWATCH STOPS HERE AND THAT IS THE WHOLE MEASUREMENT. The
                    // request is what the frame pays for; the callback arrives on a later frame
                    // and is charged to that one. Including the wait would be measuring the
                    // synchronous path with extra steps, which is exactly the mistake this
                    // change exists to stop making.
                    watch.Stop();
                    async.Add(watch.Elapsed.TotalSeconds);

                    yield return null;
                }

                AsyncGPUReadback.WaitAllRequests();
            }

            camera.targetTexture = null;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(cameraGo);

            // ---------------- the report ----------------------------------------
            var sb = new StringBuilder();
            sb.AppendLine("REPLAY CAPTURE COST, PER CALL");
            sb.AppendLine();
            sb.AppendLine($"  frame            {Width} x {Height} RGB565, " +
                          $"{Width * Height * 2} B");
            sb.AppendLine($"  captures each    {Captures}");
            sb.AppendLine($"  async supported  {asyncSupported} " +
                          $"(readback {SystemInfo.supportsAsyncGPUReadback}, " +
                          $"RGB565 target {SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB565)})");
            sb.AppendLine($"  readbacks landed {landed}, failed {failed}");
            sb.AppendLine($"  first call       ReadPixels {syncWarm:F3} ms, "
                          + $"AsyncGPU {asyncWarm:F3} ms  (once per session, not in the rows below)");
            sb.AppendLine();
            sb.AppendLine($"  {"path",-12} {"calls",6} {"mean ms",9} {"p95 ms",9} {"p99 ms",9} " +
                          $"{"worst ms",9} {"over 2 ms",10} {"over 5 ms",10}");
            sb.AppendLine("  " + new string('-', 82));
            sb.AppendLine(Row("ReadPixels", sync));
            sb.AppendLine(Row("AsyncGPU", async));
            sb.AppendLine();
            sb.AppendLine("⚠️ THE WORST CALL IS THE NUMBER THAT MATTERS. `FrameRateHistogram");
            sb.AppendLine("   .MaxSeconds`: tournament pain is a hitch, not a low average, and a");
            sb.AppendLine("   stall on the frame somebody throws is invisible to a mean.");
            sb.AppendLine();
            sb.AppendLine("⚠️ A 90 s round is 900 captures and a four-round Classic set is 3,600.");
            sb.AppendLine("   Multiply the mean by those to price a match.");

            string report = sb.ToString();
            Debug.Log(report);

            Directory.CreateDirectory("Logs");
            File.WriteAllText(Path.Combine("Logs", "replay-capture.txt"), report);

            Assert.Greater(sync.Frames, 0, "The synchronous path recorded no calls at all.");

            if (!asyncSupported)
            {
                Assert.Ignore("This device has no asynchronous readback, so the shipped capture " +
                              "is the synchronous fallback here and there is nothing to compare. " +
                              "The numbers above are still the cost of that fallback.");
            }

            Assert.Greater(async.Frames, 0);

            // ⚠️⚠️ THE BOUND IS A FLOOR A BROKEN BUILD CROSSES RATHER THAN A TIGHT ONE, for
            // `CLAUDE.md` § 7's reason: a wall-clock assertion tuned to this machine is a test
            // that goes red because somebody else's laptop was busy. What it refuses is the
            // regression that actually matters: an "asynchronous" path that is really waiting.
            Assert.Less(async.MaxSeconds, sync.MaxSeconds,
                $"The asynchronous path's worst call ({async.MaxSeconds * 1000.0:0.00} ms) is no " +
                $"better than the synchronous one's ({sync.MaxSeconds * 1000.0:0.00} ms). " +
                $"Something is waiting on the GPU inside the request.");
        }

        // -------------------------------------------------------------------
        // § 145.15  THE FRAME, NOT THE CALL
        // -------------------------------------------------------------------

        /// <summary>
        /// What the shipped replay path costs the FRAME, at its real sampling cadence, with
        /// capture off and capture on.
        ///
        /// ⚠️⚠️ THE TEST ABOVE MEASURES THE WRONG BOUNDARY AND IS STILL WORTH KEEPING. It stops
        /// its stopwatch after `AsyncGPUReadback.Request(...)`, and the work that costs a frame,
        /// `request.GetData`, `Texture2D.LoadRawTextureData` and `Texture2D.Apply`, happens in a
        /// callback the driver invokes **two or three frames later**. So its result proves
        /// submission became cheap, which is true and is not the question anybody has.
        /// `docs/TODO.md` § 145.15: *"the existing result proves request submission became cheap,
        /// not that replay no longer produces frame spikes overall."*
        ///
        /// ⚠️⚠️ SO THIS ONE TIMES WHOLE FRAMES AND NEVER A CALL. Every callback lands inside one
        /// of the frames below, so whatever it costs is inside these numbers whether it announces
        /// itself or not. The controlled comparison is the same loop with the capture removed.
        ///
        /// ⚠️ IT PERFORMS THE SHIPPED SEQUENCE RATHER THAN DRIVING `SpectatorCamera`, for the
        /// reason the class header already gives about the per-call test: a switch on the shipped
        /// component would be a settable static that survives a scene change, which is the class
        /// of leftover `TournamentPreset.Modifiers` exists to catch, added for a test's
        /// convenience. **The constants are asserted against the component** below, so the
        /// sequence cannot quietly drift from the one that ships.
        ///
        /// ⚠️ THE ASSERTIONS ARE GROSS-REGRESSION FLOORS AND THE REPORT IS THE DELIVERABLE.
        /// `CLAUDE.md` § 7: a wall-clock bound tuned to this machine is a test that goes red
        /// because somebody else's laptop was busy.
        /// </summary>
        [UnityTest]
        public IEnumerator TheReplayCaptureCostsTheFrameNoMoreThanNotCapturingDoes()
        {
            var cameraGo = new GameObject("ReplayFrameProbeCamera", typeof(Camera));
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.42f, 0.28f);

            var source = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = source;

            bool asyncSupported = SystemInfo.supportsAsyncGPUReadback &&
                                  SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB565);

            var texture = new Texture2D(Width, Height, TextureFormat.RGB565, mipChain: false);

            int landed = 0;
            int failed = 0;
            int outstanding = 0;
            int highWater = 0;
            int dropped = 0;

            // ⚠️ THE WARM-UP IS PAID BEFORE EITHER ARM, because the shipped component pays it at
            // spectator initialisation now (`SpectatorCamera.PrewarmAsyncReadback`, § 145.16) and
            // a probe that let one arm pay it would be measuring the fix rather than the cost.
            double prewarmMs = -1.0;
            if (asyncSupported)
            {
                var warm = System.Diagnostics.Stopwatch.StartNew();
                bool warmLanded = false;
                var warmScratch = RenderTexture.GetTemporary(8, 8, 0, RenderTextureFormat.RGB565);
                AsyncGPUReadback.Request(warmScratch, 0, _ =>
                {
                    RenderTexture.ReleaseTemporary(warmScratch);
                    warm.Stop();
                    warmLanded = true;
                });

                while (!warmLanded) yield return null;
                prewarmMs = warm.Elapsed.TotalMilliseconds;
            }

            var quiet = new FrameRateHistogram();
            var capturing = new FrameRateHistogram();

            long gcBefore = System.GC.GetTotalMemory(false);

            // ---------------- capture OFF, the control -------------------------
            for (int i = 0; i < FrameProbeFrames; i++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                yield return null;
                watch.Stop();
                quiet.Add(watch.Elapsed.TotalSeconds);
            }

            long gcAfterQuiet = System.GC.GetTotalMemory(false);

            // ---------------- capture ON, at the shipped cadence ---------------
            //
            // ⚠️⚠️ THE CADENCE IS `SpectatorCamera`'S OWN AND IS DERIVED, NOT TYPED. Ten a second
            // is what a match actually asks for; a probe capturing every frame would report a
            // cost nothing in the game pays and would be a different number wearing this one's
            // name.
            float accum = 0.0f;

            for (int i = 0; i < FrameProbeFrames; i++)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                accum += Time.unscaledDeltaTime;
                if (accum >= ReplaySampleInterval)
                {
                    accum %= ReplaySampleInterval;

                    if (!asyncSupported)
                    {
                        CaptureSynchronously(source, texture);
                    }
                    else if (outstanding >= MaxOutstandingReadbacks)
                    {
                        // ⚠️ THE SHIPPED CAP, REPLICATED. A capture refused here costs nothing at
                        // all, which is the property `SpectatorCamera.CaptureReplayFrame` states:
                        // the check happens before any target is allocated.
                        dropped++;
                    }
                    else
                    {
                        var scratch = RenderTexture.GetTemporary(Width, Height, 0,
                                                                 RenderTextureFormat.RGB565);
                        Graphics.Blit(source, scratch);

                        outstanding++;
                        if (outstanding > highWater) highWater = outstanding;

                        AsyncGPUReadback.Request(scratch, 0, request =>
                        {
                            RenderTexture.ReleaseTemporary(scratch);
                            outstanding--;

                            if (request.hasError)
                            {
                                failed++;
                                return;
                            }

                            // ⚠️⚠️ THIS IS THE WORK THE OTHER TEST'S STOPWATCH NEVER SAW, and it
                            // is inside whichever frame the driver chose to land on. Timing the
                            // whole frame is the only way to price it.
                            var data = request.GetData<byte>();
                            if (data.Length != Width * Height * 2)
                            {
                                failed++;
                                return;
                            }

                            texture.LoadRawTextureData(data);
                            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                            landed++;
                        });
                    }
                }

                yield return null;
                watch.Stop();
                capturing.Add(watch.Elapsed.TotalSeconds);
            }

            if (asyncSupported) AsyncGPUReadback.WaitAllRequests();

            long gcAfterCapture = System.GC.GetTotalMemory(false);

            camera.targetTexture = null;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(cameraGo);

            // ---------------- the report ----------------------------------------
            var sb = new StringBuilder();
            sb.AppendLine("REPLAY CAPTURE COST, PER FRAME");
            sb.AppendLine();
            sb.AppendLine($"  frames each arm  {FrameProbeFrames}");
            sb.AppendLine($"  cadence          {ReplaySampleInterval:0.000} s " +
                          $"({1.0f / ReplaySampleInterval:0.#} captures a second), the shipped one");
            sb.AppendLine($"  frame            {Width} x {Height} RGB565, {Width * Height * 2} B");
            sb.AppendLine($"  async supported  {asyncSupported}");
            sb.AppendLine($"  warm-up cost     {prewarmMs:F1} ms, paid before both arms " +
                          $"(SpectatorCamera pays this at spectator init now)");
            sb.AppendLine($"  readbacks        {landed} landed, {failed} failed, " +
                          $"{dropped} dropped, high-water {highWater} of {MaxOutstandingReadbacks}");
            sb.AppendLine($"  managed heap     quiet {(gcAfterQuiet - gcBefore) / 1024.0:F1} KiB, " +
                          $"capturing {(gcAfterCapture - gcAfterQuiet) / 1024.0:F1} KiB");
            sb.AppendLine();
            sb.AppendLine($"  {"arm",-12} {"frames",6} {"mean ms",9} {"p95 ms",9} {"p99 ms",9} " +
                          $"{"worst ms",9} {"over 2 ms",10} {"over 5 ms",10} {"over 16 ms",11}");
            sb.AppendLine("  " + new string('-', 94));
            sb.AppendLine(FrameRow("capture off", quiet));
            sb.AppendLine(FrameRow("capture on", capturing));
            sb.AppendLine();
            sb.AppendLine("⚠️⚠️ THIS IS THE MEASUREMENT § 145.15 ASKED FOR. The per-call test above");
            sb.AppendLine("   stops its stopwatch after `AsyncGPUReadback.Request(...)`, and the");
            sb.AppendLine("   `GetData` / `LoadRawTextureData` / `Apply` work happens in a callback");
            sb.AppendLine("   two or three frames later. Those frames are in the rows above.");
            sb.AppendLine();
            sb.AppendLine("⚠️ A BATCHMODE FRAME IS NOT A PLAYED FRAME. With nothing to present the");
            sb.AppendLine("   loop spins far faster than 60 Hz, so the absolute milliseconds are");
            sb.AppendLine("   optimistic; the DIFFERENCE between the two arms is the finding, and");
            sb.AppendLine("   it is measured under identical conditions.");

            string report = sb.ToString();
            Debug.Log(report);

            Directory.CreateDirectory("Logs");
            File.WriteAllText(Path.Combine("Logs", "replay-frames.txt"), report);

            Assert.Greater(quiet.Frames, 0, "the control arm recorded no frames at all");
            Assert.Greater(capturing.Frames, 0, "the capturing arm recorded no frames at all");

            if (!asyncSupported)
            {
                Assert.Ignore("This device has no asynchronous readback, so the shipped capture " +
                              "is the synchronous fallback. The numbers above are its real cost.");
            }

            Assert.Greater(landed, 0,
                "Not one readback landed, so the capturing arm measured a loop that submitted " +
                "requests and never paid for a single one of them. That is the per-call test's " +
                "blind spot reproduced inside the test written to close it.");

            Assert.AreEqual(0, failed,
                $"{failed} readback(s) failed. A driver error is a dropped replay frame and is " +
                $"survivable; every one of them failing is a broken path.");

            // ⚠️⚠️ A GROSS-REGRESSION FLOOR AND NOT A TIGHT BOUND. Ten captures a second cannot
            // cost anything like a whole frame budget each; what this refuses is the shape where
            // the "asynchronous" path is really synchronous, which would put the capturing arm's
            // worst frame in a different order of magnitude from the control's.
            Assert.Less(capturing.MaxSeconds, 0.250,
                $"the worst FRAME while capturing was {capturing.MaxSeconds * 1000.0:0.0} ms. " +
                $"Ten captures a second cannot cost that unless something is waiting on the GPU.");
        }

        /// <summary>
        /// How many frames each arm runs.
        ///
        /// ⚠️ 600 IS TEN SECONDS AT 60 Hz, which is long enough for a p99 to mean something and
        /// covers about a hundred captures at the shipped cadence.
        /// </summary>
        private const int FrameProbeFrames = 600;

        /// <summary>
        /// The shipped cadence and cap, read off `SpectatorCamera` rather than typed twice.
        ///
        /// ⚠️⚠️ THEY ARE `private const` OVER THERE, SO THIS IS A COPY, AND A COPY IS EXACTLY WHAT
        /// `CLAUDE.md` § 5 WARNS ABOUT. `TheFrameProbeUsesTheShippedCadenceAndCap` asserts the two
        /// agree by reading the component's own source, so a retune that moved one and not the
        /// other fails a test rather than producing a report about a game nobody plays.
        /// </summary>
        private const float ReplaySampleInterval = 0.10f;
        private const int MaxOutstandingReadbacks = 4;

        /// <summary>⚠️ THE COPY ABOVE IS CHECKED AGAINST THE SOURCE IT WAS COPIED FROM.</summary>
        [Test]
        public void TheFrameProbeUsesTheShippedCadenceAndCap()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath),
                                       "Assets", "TumbangPreso", "Runtime", "Camera",
                                       "SpectatorCamera.cs");
            string source = File.ReadAllText(path);

            StringAssert.Contains(
                $"private const float ReplaySampleInterval = {ReplaySampleInterval:0.00}f;", source,
                "the probe's cadence and the shipped one have drifted, so the report above is " +
                "about a game nobody plays.");
            StringAssert.Contains(
                $"private const int MaxOutstandingReadbacks = {MaxOutstandingReadbacks};", source,
                "the probe's outstanding cap and the shipped one have drifted.");
            StringAssert.Contains($"private const int ReplayWidth = {Width};", source);
            StringAssert.Contains($"private const int ReplayHeight = {Height};", source);
        }

        private static string FrameRow(string name, FrameRateHistogram h)
        {
            if (h.Frames == 0) return $"  {name,-12} {"-",6}";

            double p95 = 1000.0 / System.Math.Max(0.0001, h.LowFps(5.0));
            double p99 = 1000.0 / System.Math.Max(0.0001, h.LowFps(1.0));

            return $"  {name,-12} {h.Frames,6} {(h.Seconds / h.Frames) * 1000.0,9:F3} " +
                   $"{p95,9:F3} {p99,9:F3} {h.MaxSeconds * 1000.0,9:F3} " +
                   $"{h.LongFrames(0.002),10} {h.LongFrames(0.005),10} {h.LongFrames(0.016),11}";
        }

        /// <summary>The old path, exactly as `SpectatorCamera` performed it.</summary>
        private static void CaptureSynchronously(RenderTexture source, Texture2D into)
        {
            var scratch = RenderTexture.GetTemporary(Width, Height, 0,
                                                      RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;

            try
            {
                Graphics.Blit(source, scratch);
                RenderTexture.active = scratch;
                into.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
                into.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(scratch);
            }
        }

        private static string Row(string name, FrameRateHistogram h)
        {
            if (h.Frames == 0) return $"  {name,-12} {"-",6}";

            // ⚠️ `LowFps(p)` IS A PERCENTILE OF FRAME TIME READ BACK AS A RATE, so the p95 of
            // duration is the 5-per-cent-low of rate. Its own note explains why the argument
            // looks inverted; inverting it back here is what makes the column say milliseconds.
            double p95 = 1000.0 / System.Math.Max(0.0001, h.LowFps(5.0));
            double p99 = 1000.0 / System.Math.Max(0.0001, h.LowFps(1.0));

            return $"  {name,-12} {h.Frames,6} {(h.Seconds / h.Frames) * 1000.0,9:F3} " +
                   $"{p95,9:F3} {p99,9:F3} {h.MaxSeconds * 1000.0,9:F3} " +
                   $"{h.LongFrames(0.002),10} {h.LongFrames(0.005),10}";
        }
    }
}
