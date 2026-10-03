using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    // AIRBURST presentation evidence (`docs/reports/amihan-presentation-2026-10-02`). Uses only runtime API that
    // existed before the presentation change, so the same fixture films the shipped BEFORE and the AFTER candidate.
    public sealed partial class AmihanKitPlayProbe
    {
        private sealed class AirburstStage
        {
            public CharacterMotor Caster, Victim, Outsider, Behind;
            public Vector3 Origin, Forward;
        }

        /// <summary>Amihan on the local seat, one player inside the fan, one outside its edge, one behind her.</summary>
        private static AirburstStage StageAirburst()
        {
            var round = GameServices.Round;
            var can = Flat(round.Lata.transform.position);
            var origin = can + new Vector3(0, .12f, -8.5f);
            var caster = Amihan(GameLaunch.SoloSeat, origin);
            caster.transform.rotation = Quaternion.identity;
            var stage = new AirburstStage { Caster = caster, Origin = origin, Forward = Vector3.forward };
            int next = 0;
            CharacterMotor Other()
            {
                for (; next < 4; next++)
                {
                    var p = round.PlayerAt(next);
                    if (p != null && p != caster) { next++; return p; }
                }
                return null;
            }
            stage.Victim = Other(); stage.Outsider = Other(); stage.Behind = Other();
            void Put(CharacterMotor who, Vector3 offset)
            {
                if (who == null) return;
                who.Teleport(origin + offset); who.Intent.Clear(); who.Intent.Parked = true;
                Face(who, origin);
            }
            // Inside: 5.5 m down the lane, 1.2 m off its axis (12 degrees). Outside: 62 degrees off the axis.
            Put(stage.Victim, new Vector3(1.2f, 0, 5.5f));
            Put(stage.Outsider, new Vector3(6.2f, 0, 3.3f));
            Put(stage.Behind, new Vector3(-1.5f, 0, -3.0f));
            caster.AbilitySystem.Kit.AddUltimateCharge(100);
            Camera.main.GetComponent<CameraRig>().Follow(caster);
            return stage;
        }

        /// <summary>
        /// TUMP_AIRBURST_TOLERATE_LOGS=1 is for filming the shipped BEFORE only: its cutscene throws while it is built
        /// (three Motifs sharing one GeneratedMeshOwner), and the film must still show what players then see. Every
        /// exception is counted and written to the film's summary; the AFTER run leaves this off so any log fails.
        /// </summary>
        private sealed class LoggedExceptions : IDisposable
        {
            public int Count; public string First = "";
            private readonly bool _tolerate;
            public LoggedExceptions()
            {
                _tolerate = Environment.GetEnvironmentVariable("TUMP_AIRBURST_TOLERATE_LOGS") == "1";
                if (_tolerate) LogAssert.ignoreFailingMessages = true;
                Application.logMessageReceived += Heard;
            }
            private void Heard(string message, string stack, LogType type)
            {
                if (type != LogType.Exception && type != LogType.Error) return;
                if (Count++ == 0) First = message.Length > 160 ? message.Substring(0, 160) : message;
            }
            public void Dispose()
            {
                Application.logMessageReceived -= Heard;
                if (_tolerate) LogAssert.ignoreFailingMessages = false;
            }
        }

        private static RawImage UltimateOverlay()
        {
            foreach (var image in Object.FindObjectsByType<RawImage>(FindObjectsSortMode.None))
                if (image.name == "UltimateScene" && image.enabled && image.isActiveAndEnabled && image.texture != null) return image;
            return null;
        }

        /// <summary>
        /// The whole Airburst on every view at true game speed (30 frames per game second, the phase on the film clock):
        /// lead-in, the 5.6 s cutscene on her screen, the 1.5 s windup, the release, recovery and aftermath.
        /// Variants: "fx", "body" (live effects hidden, for judging the body alone) and "low" (Low profile plus
        /// reduced effects). Runs only with TUMP_AIRBURST_FILM=1; frames under TUMP_EVIDENCE/airburst-&lt;variant&gt;,
        /// with timing.csv carrying every measured frame.
        /// </summary>
        [UnityTest, Timeout(900000)] public IEnumerator FilmAirburstWithEffects() => FilmAirburst("fx");
        [UnityTest, Timeout(900000)] public IEnumerator FilmAirburstBodyOnly() => FilmAirburst("body");
        [UnityTest, Timeout(900000)] public IEnumerator FilmAirburstOnLow() => FilmAirburst("low");

        private IEnumerator FilmAirburst(string variant)
        {
            if (Environment.GetEnvironmentVariable("TUMP_AIRBURST_FILM") != "1") Assert.Ignore("Film only: set TUMP_AIRBURST_FILM=1.");
            bool bodyOnly = variant == "body", low = variant == "low";
            var stage = StageAirburst();
            var caster = stage.Caster;
            yield return null;
            Assert.AreEqual("amihan_ultimate", caster.AbilitySystem.Kit.Ultimate.Id);
            var round = GameServices.Round;
            var wide = Film.Make("AirburstWide", 50);
            var observer = Film.Make("AirburstObserver", 62);
            var victimCam = Film.Make("AirburstVictim", 60);
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            int graphics = Settings.GraphicsProfiles.Current;
            if (low) { Settings.SettingsStore.Current.ReducedEffects = true; Settings.GraphicsProfiles.Apply(0); }
            var timing = new StringBuilder().AppendLine("frame,seconds,phase,overlay,winding,released,victim_whirled,outsider_whirled,palm_forward_m,clock,action,fans,phase_age,phase_shot,eye");
            int sceneFirst = -1, sceneLast = -1, handback = -1, release = -1, whirledAt = -1, peakFrame = -1;
            float peak = float.MinValue, clockAtScene = -1, clockAtSceneEnd = -1;
            bool outsiderWhirled = false, sawPhase = false;
            int frame = 0; double clockBase = Time.realtimeSinceStartupAsDouble;
            SharedUltimatePhase.FilmClock = () => clockBase + frame / 30.0;
            var hand = caster.GetComponent<CharacterVisual>()?.HandAnchor;
            string root = null;
            var logged = new LoggedExceptions();
            try
            {
                using (var film = new Film("airburst-" + variant, "owner", "observer", "victim", "wide"))
                {
                    root = film.Root;
                    const int frames = 30 * 11;
                    for (int f = 0; f < frames; f++)
                    {
                        frame = f; film.Frame = f;
                        float t = f / 30f;
                        caster.Intent.Set(Verb.Ultimate, t > 1.0f && t < 1.2f);
                        yield return null;
                        var phase = SharedUltimatePhase.Instance;
                        bool inPhase = phase != null && phase.Active;
                        var overlay = UltimateOverlay();
                        var ult = caster.AbilitySystem.Kit.Ultimate;
                        var storm = Object.FindFirstObjectByType<AmihanStorm>();
                        if (overlay != null)
                        {
                            // The theme plays from the introduction's own source, not `AudioDirector`, so the film logs it (Paete's and Phaister's rule).
                            if (sceneFirst < 0) { sceneFirst = f; clockAtScene = round.TimeLeft; film.Cues.AppendLine(FormattableString.Invariant($"{f / 30.0:F3},sfx_ult_theme_amihan,1,0.2")); }
                            sceneLast = f; clockAtSceneEnd = round.TimeLeft;
                        }
                        if (inPhase) sawPhase = true;
                        if (handback < 0 && sawPhase && overlay == null && !inPhase) handback = f;
                        if (release < 0 && storm != null && storm.Released) release = f;
                        if (whirledAt < 0 && stage.Victim != null && stage.Victim.IsWhirled) whirledAt = f;
                        outsiderWhirled |= stage.Outsider != null && stage.Outsider.IsWhirled;
                        float forward = hand != null ? Vector3.Dot(hand.position - caster.transform.position, caster.transform.forward) : 0;
                        if (handback >= 0 && f <= handback + 75 && forward > peak) { peak = forward; peakFrame = f; }
                        int fans = Object.FindObjectsByType<AmihanStormFan>(FindObjectsSortMode.None).Length;
                        var animator = caster.GetComponentInChildren<CharacterAnimator>();
                        timing.AppendLine(FormattableString.Invariant(
                            $"{f},{t:F3},{inPhase},{overlay != null},{ult.IsWindingUp},{storm != null && storm.Released},{stage.Victim != null && stage.Victim.IsWhirled},{stage.Outsider != null && stage.Outsider.IsWhirled},{forward:F3},{round.TimeLeft:F3},{animator?.CurrentClipName},{fans},{UltimatePhaseView.LastAge:F3},{UltimatePhaseView.LastShot},{UltimatePhaseView.LastEye.x:F2} {UltimatePhaseView.LastEye.y:F2} {UltimatePhaseView.LastEye.z:F2}"));

                        if (bodyOnly)
                            foreach (var fan in Object.FindObjectsByType<AmihanStormFan>(FindObjectsSortMode.None))
                                foreach (var r in fan.GetComponentsInChildren<Renderer>(true)) r.forceRenderingOff = true;
                        var at = caster.transform.position;
                        if (overlay != null) film.Save(overlay.texture, "owner"); else film.Shoot(Camera.main, "owner");
                        if (stage.Outsider != null)
                        {
                            // Behind and above the outsider's own (large) head, looking at her and down her lane:
                            // what a player standing just outside the fan reads. Eye height alone sits inside the head.
                            var o = stage.Outsider.transform.position;
                            var away = Flat(o - at); away = away.sqrMagnitude > .01f ? away.normalized : Vector3.right;
                            observer.transform.position = o + away * 1.6f + Vector3.up * 2.3f;
                            observer.transform.LookAt(at + Vector3.up * .8f + Vector3.forward * 2.5f);
                        }
                        film.Shoot(observer, "observer");
                        if (stage.Victim != null)
                        {
                            var v = stage.Victim.transform.position;
                            var toHer = Flat(at - v); toHer = toHer.sqrMagnitude > .01f ? toHer.normalized : Vector3.back;
                            victimCam.transform.position = v - toHer * 2.4f + Vector3.Cross(Vector3.up, toHer) * 1.1f + Vector3.up * 1.7f;
                            victimCam.transform.LookAt(at + Vector3.up * .9f);
                        }
                        film.Shoot(victimCam, "victim");
                        wide.transform.position = stage.Origin + new Vector3(9.5f, 7.0f, 2.0f);
                        wide.transform.LookAt(stage.Origin + new Vector3(0, .6f, 5.0f));
                        film.Shoot(wide, "wide");
                    }
                }
            }
            finally
            {
                logged.Dispose();
                SharedUltimatePhase.FilmClock = null;
                caster.Intent.Clear();
                Object.Destroy(wide.gameObject); Object.Destroy(observer.gameObject); Object.Destroy(victimCam.gameObject);
                Settings.SettingsStore.Current.ReducedEffects = reduced;
                Settings.GraphicsProfiles.Apply(graphics);
                if (root != null)
                {
                    File.WriteAllText(Path.Combine(root, "timing.csv"), timing.ToString());
                    File.WriteAllText(Path.Combine(root, "shots.txt"), UltimatePhaseView.LastShotReport);
                }
            }
            string summary = FormattableString.Invariant(
                $"variant {variant}; logged errors {logged.Count} (first: {logged.First}); scene frames {sceneFirst}-{sceneLast}; handback {handback}; release {release}; victim whirled {whirledAt}; palm peak frame {peakFrame} ({peak:F3} m); outsider whirled {outsiderWhirled}; clock {clockAtScene:F3} to {clockAtSceneEnd:F3}");
            if (root != null) File.WriteAllText(Path.Combine(root, "summary.txt"), summary + Environment.NewLine);
            Debug.Log("[AirburstFilm] " + summary);
            Note("airburst_" + variant, summary);
            Assert.GreaterOrEqual(sceneFirst, 0, "The cutscene never came up.");
            Assert.AreEqual(clockAtScene, clockAtSceneEnd, 1e-4f, "The round clock ran under the cutscene.");
            Assert.GreaterOrEqual(release, 0, "Airburst never released.");
            Assert.That(release - handback, Is.InRange(42, 48), "The release is 1.5 s after the handback.");
            Assert.That(peakFrame - release, Is.InRange(-2, 5), "The body's forward drive must land on the gameplay release.");
            Assert.GreaterOrEqual(whirledAt, 0); Assert.LessOrEqual(whirledAt - release, 1);
            Assert.IsFalse(outsiderWhirled, "A player outside the 60 degree fan was caught.");
        }

        /// <summary>
        /// A round boundary during the live windup takes the gathering fan with it: no storm, no fan pieces and no
        /// release afterwards, and nobody in the lane is thrown.
        /// </summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator AirburstWindupRetiresAtTheRoundBoundary()
        {
            var stage = StageAirburst();
            var caster = stage.Caster;
            yield return null;
            int frame = 0; double clockBase = Time.realtimeSinceStartupAsDouble;
            int previous = Time.captureFramerate; Time.captureFramerate = 30;
            SharedUltimatePhase.FilmClock = () => clockBase + frame / 30.0;
            bool sawWindup = false, sawFan = false, released = false;
            int endedAt = -1, fansAfter = -1, stormsAfter = -1;
            var logged = new LoggedExceptions();
            try
            {
                for (int f = 0; f < 30 * 9; f++)
                {
                    frame = f;
                    caster.Intent.Set(Verb.Ultimate, f < 6);
                    yield return null;
                    var ult = caster.AbilitySystem.Kit.Ultimate;
                    var storm = Object.FindFirstObjectByType<AmihanStorm>();
                    released |= storm != null && storm.Released;
                    sawFan |= Object.FindFirstObjectByType<AmihanStormFan>() != null;
                    if (endedAt < 0 && ult.IsWindingUp && ult.WindupRemaining < .8f)
                    {
                        sawWindup = true;
                        // The real boundary: the round ends and the next begins, which resets every kit
                        // (`HeroAbilitySystem.ResetKit`). EndRound alone only clears the live flag.
                        GameServices.Round.EndRound();
                        GameServices.Match.AdvanceRound();
                        endedAt = f;
                    }
                    if (endedAt >= 0 && f == endedAt + 3)
                    {
                        fansAfter = Object.FindObjectsByType<AmihanStormFan>(FindObjectsSortMode.None).Length;
                        stormsAfter = Object.FindObjectsByType<AmihanStorm>(FindObjectsSortMode.None).Length;
                    }
                    if (endedAt >= 0 && f > endedAt + 60) break;
                }
            }
            finally
            {
                logged.Dispose();
                SharedUltimatePhase.FilmClock = null; Time.captureFramerate = previous; caster.Intent.Clear();
            }
            Note("airburst_round_end_fans_after", fansAfter);
            Note("airburst_round_end_logged_errors", logged.Count + " " + logged.First);
            Assert.IsTrue(sawWindup, "The live windup never started.");
            Assert.IsTrue(sawFan, "The live fan never appeared.");
            Assert.AreEqual(0, stormsAfter, "The storm outlived its round.");
            Assert.AreEqual(0, fansAfter, "Fan pieces outlived the round.");
            Assert.IsFalse(released, "A storm released after its round ended.");
            Assert.IsFalse(stage.Victim != null && stage.Victim.IsWhirled, "Someone was thrown by a retired storm.");
        }
    }
}
