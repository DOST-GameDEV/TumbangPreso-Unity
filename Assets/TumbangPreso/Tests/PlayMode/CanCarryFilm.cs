using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// FILM, not a gate: CAN-CARRY-1024 and the CONTACT-1024 hot can on Eskinita at true game speed (30 frames per game
    /// second). Shots: the taya carries the can back and sets it down, then picks it up again and lets go mid-walk; a molten
    /// strike flies the can off the road and skids it. Views: the taya's own screen (first person, arms and all) and a court
    /// camera from the side. Runs only with TUMP_CARRY_FILM=1; frames under TUMP_EVIDENCE (or Logs)/can-carry-film/&lt;shot&gt;-&lt;view&gt;,
    /// each with frames.csv, for ffmpeg at 30 fps.
    /// </summary>
    [Category("Review")]
    public sealed class CanCarryFilm
    {
        private const int Fps = 30;
        private INetProvider _net;
        private int _seat;
        private bool _active;

        [UnitySetUp] public IEnumerator Before()
        {
            _net = NetAuthority.Provider; _seat = GameLaunch.SoloSeat;
            yield return PlayModeWorld.Reset();
            _active = Environment.GetEnvironmentVariable("TUMP_CARRY_FILM") == "1";
            if (!_active) yield break;
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.Classic);
            var gate = Object.FindFirstObjectByType<ReadyGate>();
            if (gate != null && gate.AwaitingReady)
            {
                gate.StartLocalCountdown();
                yield return new WaitUntil(() => !gate.CountingDown);
            }
            NetAuthority.Provider = new SoloProvider();
            GameServices.Match.StartMatch(); GameServices.Round.BeginRound();
            yield return null;
        }

        [UnityTearDown] public IEnumerator After()
        {
            Time.captureFramerate = 0; Hitstop.End(); Time.timeScale = 1;
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _net; GameLaunch.SoloSeat = _seat;
        }

        private static string Root => Path.Combine(Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs", "can-carry-film");

        /// <summary>Two views drawn after every LateUpdate (the pose the player actually sees).</summary>
        [DefaultExecutionOrder(10000)]
        private sealed class Recorder : MonoBehaviour
        {
            public Camera Owner, Court;
            public CharacterMotor Subject;
            public Func<Vector3> CourtFocus;
            public Vector3 CourtOffset = new Vector3(0, 1.9f, -5.2f);
            public string Shot;
            public bool Rolling, OwnerOn = true, CourtOn = true;
            private string[] Views => OwnerOn && CourtOn ? new[] { "owner", "court" } : OwnerOn ? new[] { "owner" } : new[] { "court" };
            private RenderTexture _hdr, _ldr;
            private Texture2D _pixels;
            private int _frame;
            private StringBuilder _log;

            public void Begin(string shot)
            {
                Shot = shot; _frame = 0; Rolling = true;
                _log = new StringBuilder().AppendLine("frame,game_seconds");
                foreach (var view in Views) Directory.CreateDirectory(Path.Combine(Root, shot + "-" + view));
                if (_hdr == null)
                {
                    _hdr = new RenderTexture(1280, 720, 24, RenderTextureFormat.DefaultHDR) { antiAliasing = 4 };
                    _ldr = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);
                    _pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                }
            }

            public void End()
            {
                Rolling = false;
                foreach (var view in Views)
                    File.WriteAllText(Path.Combine(Root, Shot + "-" + view, "frames.csv"), _log.ToString());
            }

            private void LateUpdate()
            {
                if (!Rolling) return;
                if (CourtFocus != null)
                {
                    Vector3 f = CourtFocus();
                    Court.transform.position = Vector3.Lerp(Court.transform.position, f + CourtOffset, _frame == 0 ? 1 : .12f);
                    Court.transform.LookAt(f + Vector3.up * .5f);
                }
                if (OwnerOn) Draw(Owner, "owner");
                if (CourtOn) DrawCourt();
                _log.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1:F4}", _frame, _frame / (float)Fps));
                _frame++;
            }

            private void DrawCourt()
            {
                // The court sees the taya's whole body (their own screen draws it as a shadow only) and none of the
                // first-person arms.
                var body = Subject != null ? Subject.GetComponentsInChildren<Renderer>() : new Renderer[0];
                var modes = new ShadowCastingMode[body.Length];
                for (int i = 0; i < body.Length; i++) { modes[i] = body[i].shadowCastingMode; if (modes[i] == ShadowCastingMode.ShadowsOnly) body[i].shadowCastingMode = ShadowCastingMode.On; }
                var arms = new System.Collections.Generic.List<Renderer>();
                foreach (var a in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None))
                    foreach (var r in a.GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; arms.Add(r); }
                try { Draw(Court, "court"); }
                finally
                {
                    for (int i = 0; i < body.Length; i++) if (body[i] != null) body[i].shadowCastingMode = modes[i];
                    foreach (var r in arms) if (r != null) r.enabled = true;
                }
            }

            private void Draw(Camera camera, string view)
            {
                if (camera == null) return;
                var target = camera.targetTexture;
                camera.targetTexture = _hdr;
                ComicPopup.PrepareView(camera);
                camera.Render();
                camera.targetTexture = target;
                Graphics.Blit(_hdr, _ldr);
                var active = RenderTexture.active;
                RenderTexture.active = _ldr;
                _pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                _pixels.Apply();
                RenderTexture.active = active;
                File.WriteAllBytes(Path.Combine(Root, Shot + "-" + view, $"{_frame:D5}.jpg"), _pixels.EncodeToJPG(92));
            }

            private void OnDestroy()
            {
                if (_hdr != null) { _hdr.Release(); Destroy(_hdr); _ldr.Release(); Destroy(_ldr); Destroy(_pixels); }
            }
        }

        private static IEnumerator Seconds(float s) { for (int i = 0, n = Mathf.CeilToInt(s * Fps); i < n; i++) yield return null; }

        private static void Face(CharacterMotor who, Vector3 at)
        {
            Vector3 d = at - who.transform.position; d.y = 0;
            if (d.sqrMagnitude > 1e-4f) who.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }

        /// <summary>Knock the can down where it stands, then lay it <paramref name="off"/> from its mark with the taya beside it.</summary>
        private static IEnumerator Stage(CharacterMotor taya, Lata can, Vector3 off)
        {
            while (can.ProtectionLeft > 0) yield return null;
            if (can.IsUpright) { can.HostKnockDown(-1); Hitstop.End(); }
            yield return Seconds(Balance.ToppleTime + .2f);
            Vector3 lay = can.Mark + off;
            can.transform.position = new Vector3(lay.x, can.transform.position.y, lay.z);
            taya.Teleport(can.transform.position + off.normalized * .6f);
            for (int i = 0; i <= Balance.SpawnSettleFrames; i++) yield return new WaitForFixedUpdate();
            Face(taya, can.Mark);
        }

        [UnityTest, Timeout(300000)] public IEnumerator FilmTheCarryAndTheHotCan()
        {
            if (!_active) Assert.Ignore("Set TUMP_CARRY_FILM=1 to film.");
            Lata.LogCarry = true;
            var round = GameServices.Round;
            var can = round.Lata;
            var taya = round.PlayerAt(GameServices.Match.DefenderSlot);

            // The taya is this screen's player, so first person, the arms and the HUD are the ones they would see.
            GameLaunch.SoloSeat = taya.PlayerSlot;
            var switcher = Object.FindFirstObjectByType<DebugPlayerSwitcher>();
            if (switcher != null)
                typeof(DebugPlayerSwitcher).GetMethod("Assign", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(switcher, new object[] { taya.PlayerSlot });
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            if (switcher != null) switcher.enabled = false;
            foreach (var actor in round.Players)
            {
                actor.Intent.Clear(); actor.Intent.CommitFrame();
                actor.Intent.Parked = actor != taya;
                if (actor != taya) actor.Teleport(can.Mark + new Vector3(-6, 0, 6));
            }
            var rig = Object.FindFirstObjectByType<CameraRig>();
            Assert.IsNotNull(rig); rig.SetActive(true); rig.Follow(taya);

            var courtGo = new GameObject("~CarryFilmCourt");
            var court = courtGo.AddComponent<Camera>();
            court.CopyFrom(Camera.main); court.fieldOfView = 42; court.enabled = false;
            var recorder = new GameObject("~CarryFilmRecorder").AddComponent<Recorder>();
            recorder.Owner = Camera.main; recorder.Court = court; recorder.Subject = taya;
            Time.captureFramerate = Fps;
            try
            {
                // Each carry shot runs twice: once on the taya's own screen (first person), once on another player's
                // screen watching from the court, because each peer draws the held can for its own view.
                var other = System.Linq.Enumerable.First(round.Players, p => p != taya);
                foreach (bool own in new[] { true, false })
                {
                    rig.Follow(own ? taya : other);
                    recorder.OwnerOn = own; recorder.CourtOn = !own;
                    // Ahead of the walk and to the side, so the held can faces the lens instead of the taya's back.
                    recorder.CourtOffset = new Vector3(-3.4f, 1.7f, -4.0f);

                    // 1. CARRY: hold Grab beside the can; it comes up, the taya walks it home and plants it.
                    yield return Stage(taya, can, Vector3.right * 4.4f);
                    recorder.CourtFocus = () => Vector3.Lerp(taya.transform.position, can.Mark, .5f);
                    recorder.Begin("carry");
                    yield return Seconds(.5f);
                    taya.Intent.Set(Verb.Grab, true);
                    float limit = 9;
                    while (!can.IsUpright && (limit -= 1f / Fps) > 0) yield return null;
                    taya.Intent.Set(Verb.Grab, false);
                    yield return Seconds(1.2f);
                    recorder.End();
                    Assert.IsTrue(can.IsUpright, "The carry reached the mark on film.");

                    // 2. DROP: picked up again and let go halfway; it falls at the taya's feet, still down.
                    yield return Stage(taya, can, Vector3.right * 3.6f);
                    recorder.Begin("drop");
                    yield return Seconds(.4f);
                    taya.Intent.Set(Verb.Grab, true);
                    limit = 3;
                    while (can.Carrying != Lata.CarryPhase.Carry && (limit -= 1f / Fps) > 0) yield return null;
                    yield return Seconds(.7f);
                    taya.Intent.Set(Verb.Grab, false);
                    yield return Seconds(1.6f);
                    recorder.End();
                    Assert.IsFalse(can.IsUpright, "Letting go left it down.");
                    can.HostRestore(); Hitstop.End();
                    yield return Seconds(.2f);
                }

                // 3. HOT CAN: a molten strike lifts it off the road on its arc, lands and skids (court view is the shot).
                recorder.OwnerOn = false; recorder.CourtOn = true;
                taya.Teleport(can.Mark + new Vector3(-4, 0, -4));
                for (int i = 0; i <= Balance.SpawnSettleFrames; i++) yield return new WaitForFixedUpdate();
                while (can.ProtectionLeft > 0) yield return null;
                Vector3 home = can.Mark;
                recorder.CourtFocus = () => home + Vector3.forward * 1.5f;
                recorder.CourtOffset = new Vector3(4.6f, 1.4f, 0);
                recorder.Begin("hotcan");
                yield return Seconds(.5f);
                can.HostKnockDown(-1);
                can.HostMoltenHit(Vector3.forward);
                yield return Seconds(2.4f);
                recorder.End();
            }
            finally
            {
                Time.captureFramerate = 0;
                Lata.LogCarry = false;
                Object.Destroy(recorder.gameObject); Object.Destroy(courtGo);
            }
        }
    }
}
