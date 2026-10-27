using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests.Trailer
{
    /// <summary>
    /// REAL GAMEPLAY FOR THE TRAILER (owner, 2026-10-27: "u dont rlly show our game"). Bots play an actual Classic
    /// match on the Tondo street, cast as the story's kids (Totoy is seat 0, round 1's taya), and three cameras record
    /// the same match every frame at 24 fps on a fixed capture clock:
    ///   fpv    the spectator's real first-person view of a chosen seat (the game's own eye, hands and lens);
    ///   line   a low tracking camera behind the throwing line, telephoto on the action (slipper, can, taya);
    ///   side   a low side-on tracking camera across the court, for throws, retrievals and tags.
    /// Plus `telemetry.tsv`, one row a frame (can, roles, slippers in flight), so moments can be found and cut.
    /// Output: Logs/trailer/gameplay/&lt;take&gt;/{fpv,line,side}/f_#####.jpg and telemetry.tsv.
    /// </summary>
    public sealed class TrailerGameplay
    {
        private const int Width = 1920, Height = 1080, Fps = 24;
        private string _settings;

        [UnitySetUp] public IEnumerator Before() { yield return PlayModeWorld.Reset(); _settings = JsonUtility.ToJson(Settings.SettingsStore.Current); }

        [UnityTearDown] public IEnumerator After()
        {
            Time.captureFramerate = 0;
            GameLaunch.Spectator = false; GameLaunch.AllBots = false;
            JsonUtility.FromJsonOverwrite(_settings, Settings.SettingsStore.Current);
            yield return PlayModeWorld.Reset();
        }

        /// <summary>Totoy as the taya, then in first person once he is attacking.</summary>
        [UnityTest, Timeout(7200000)] public IEnumerator Street() => Record(SceneFlow.Eskinita, "street2", 0, 130.0f,
            new[] { "totoy", "tikboy", "bebang", "jun_jun" });

        private IEnumerator Record(string map, string take, int fpvSeat, float seconds, string[] cast)
        {
            LogAssert.ignoreFailingMessages = true;
            SceneFlow.Networked = false;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false; GameLaunch.Spectator = true; GameLaunch.AllBots = true;
            Settings.SettingsStore.Current.CinematicCameraMotion = false;
            Settings.SettingsStore.Current.ReducedUiMotion = true;
            yield return SceneManager.LoadSceneAsync(map);
            while (HubLoading.Preparing) yield return null;
            yield return null;

            // The story's kids in the four seats.
            var book = RosterBook.Load();
            var people = Roster.GetPeople(GameMode.Classic);
            foreach (var motor in GameServices.Round.Players)
            {
                if (motor.PlayerSlot < 0 || motor.PlayerSlot >= cast.Length) continue;
                var art = book.FindPersonArt(cast[motor.PlayerSlot]);
                var visual = motor.GetComponent<Visual.CharacterVisual>();
                if (art == null || art.Model == null || visual == null) continue;
                for (int i = 0; i < people.Count; i++) if (people[i].Id == cast[motor.PlayerSlot]) motor.CharacterIndex = i;
                visual.ApplyModel(art.Model, Color.white, art.Clips, art.Palette != null && art.Palette.Length == 16 ? art.Palette : null);
            }

            // Into a live round (the arrival plays as it would for a spectator).
            float waited = 0.0f;
            while (waited < 90.0f && (!GameServices.Round.RoundActive || PresentationClock.Held)) { waited += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(GameServices.Round.RoundActive, "no live round");

            var spectator = Object.FindFirstObjectByType<SpectatorCamera>();
            Assert.IsNotNull(spectator, "no spectator camera");
            var camera = spectator.GetComponent<Camera>();
            foreach (var c in Object.FindObjectsByType<Canvas>()) if (c.renderMode == RenderMode.ScreenSpaceOverlay) c.enabled = false;

            string root = Path.Combine("Logs", "trailer", "gameplay", take);
            foreach (var view in new[] { "fpv", "line", "side" })
            {
                string folder = Path.Combine(root, view);
                if (Directory.Exists(folder)) foreach (var old in Directory.GetFiles(folder)) File.Delete(old);
                Directory.CreateDirectory(folder);
            }

            var target = new RenderTexture(Width, Height, 24, camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32,
                camera.allowHDR ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default);
            target.Create();
            var resolved = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            resolved.Create();
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            var telemetry = new StringBuilder("frame\tround\tdefender\tcan_x\tcan_y\tcan_z\tcan_up\tflying\theld\tfpv_seat\tp0\tp1\tp2\tp3\n");
            var line = new Rig(); var side = new Rig();
            int frame = 0;
            int pov = -1;

            var shot = new GameObject("~TrailerGameplayShot").AddComponent<LateFrameShot>();
            shot.Shoot = _ =>
            {
                var round = GameServices.Round;
                var lata = round.Lata;
                var slippers = Object.FindObjectsByType<Slipper>();
                var flying = slippers.Where(s => s.State == SlipperState.InFlight).ToArray();
                Vector3 can = lata != null ? lata.transform.position : Vector3.zero;
                var defender = round.Players.FirstOrDefault(p => p.IsDefender);

                // What the tracking cameras look at: a slipper in flight (leading toward the can), else the can when it
                // is down, else the taya.
                Vector3 subject = defender != null ? defender.transform.position + Vector3.up * 0.8f : can;
                if (flying.Length > 0) subject = Vector3.Lerp(flying[0].transform.position, can + Vector3.up * 0.3f, 0.35f);
                else if (lata != null && !lata.IsUpright) subject = can + Vector3.up * 0.4f;

                Vector3 pos = camera.transform.position; Quaternion rot = camera.transform.rotation; float fov = camera.fieldOfView;
                var marks = Object.FindObjectsByType<Canvas>().Where(c => c.enabled && c.renderMode == RenderMode.WorldSpace).ToArray();
                foreach (var c in marks) c.enabled = false;
                var plates = Object.FindObjectsByType<Visual.CharacterNameplate>().SelectMany(n => n.GetComponentsInChildren<Renderer>()).Where(r => r.enabled).ToArray();
                foreach (var r in plates) r.enabled = false;
                Render(camera, target, resolved, image, Path.Combine(root, "fpv", $"f_{frame:00000}.jpg"));
                var arms = Object.FindObjectsByType<ViewmodelArms>().SelectMany(a => a.GetComponentsInChildren<Renderer>()).Where(r => r.enabled).ToArray();
                foreach (var r in arms) r.enabled = false;
                line.Aim(camera, new Vector3(can.x + 1.2f, can.y + 2.3f, can.z + 14.0f), subject, 24.0f, 1.0f / Fps);
                Render(camera, target, resolved, image, Path.Combine(root, "line", $"f_{frame:00000}.jpg"));
                side.Aim(camera, new Vector3(can.x - 6.4f, can.y + 1.3f, can.z + 2.5f), subject, 34.0f, 1.0f / Fps);
                Render(camera, target, resolved, image, Path.Combine(root, "side", $"f_{frame:00000}.jpg"));
                foreach (var r in arms) r.enabled = true;
                foreach (var c in marks) if (c != null) c.enabled = true;
                foreach (var r in plates) if (r != null) r.enabled = true;
                camera.transform.SetPositionAndRotation(pos, rot); camera.fieldOfView = fov;

                var row = new StringBuilder();
                row.Append(frame).Append('\t').Append(GameServices.Match != null ? GameServices.Match.RoundNumber : 0).Append('\t').Append(defender != null ? defender.PlayerSlot : -1)
                   .Append('\t').Append(can.x.ToString("F2")).Append('\t').Append(can.y.ToString("F2")).Append('\t').Append(can.z.ToString("F2"))
                   .Append('\t').Append(lata != null && lata.IsUpright ? 1 : 0).Append('\t').Append(flying.Length)
                   .Append('\t').Append(slippers.Count(s => s.State == SlipperState.Held)).Append('\t').Append(pov);
                foreach (var p in round.Players.OrderBy(p => p.PlayerSlot)) row.Append('\t').Append($"{p.transform.position.x:F1},{p.transform.position.z:F1}");
                telemetry.AppendLine(row.ToString());
            };

            Time.captureFramerate = Fps;
            int count = Mathf.CeilToInt(seconds * Fps);
            try
            {
                for (frame = 0; frame < count; frame++)
                {
                    if (pov != fpvSeat && spectator.TryWatchPlayer(fpvSeat)) pov = fpvSeat;
                    yield return shot.Take("frame");
                    if (frame % 240 == 0) Debug.Log($"[TrailerGameplay] {take}: frame {frame} of {count}, round {GameServices.Match?.RoundNumber}");
                }
            }
            finally
            {
                Time.captureFramerate = 0;
                File.WriteAllText(Path.Combine(root, "telemetry.tsv"), telemetry.ToString());
                Object.Destroy(shot.gameObject);
                Object.Destroy(image);
                target.Release(); Object.Destroy(target);
                resolved.Release(); Object.Destroy(resolved);
            }
            Debug.Log($"[TrailerGameplay] {take}: {count} frames to {root}");
        }

        /// <summary>A camera operator: a fixed base it drifts around a little, a damped aim that lags the action.</summary>
        private sealed class Rig
        {
            private Vector3 _aim, _eye; private bool _has, _hasEye;

            public void Aim(Camera camera, Vector3 eye, Vector3 subject, float fov, float dt)
            {
                if (!_has) { _aim = subject; _has = true; }
                _aim = Vector3.Lerp(_aim, subject, 1.0f - Mathf.Exp(-5.0f * dt));
                // Never behind a post or a chair: slide in along the sight line past the first piece of scenery.
                var hits = Physics.RaycastAll(_aim, (eye - _aim).normalized, Vector3.Distance(eye, _aim), ~0, QueryTriggerInteraction.Ignore);
                float clear = Vector3.Distance(eye, _aim);
                foreach (var h in hits) if (h.collider.GetComponentInParent<CharacterMotor>() == null && h.collider.GetComponentInParent<Slipper>() == null && h.collider.GetComponentInParent<Lata>() == null) clear = Mathf.Min(clear, h.distance - 0.3f);
                Vector3 seen = _aim + (eye - _aim).normalized * Mathf.Max(2.5f, clear);
                _eye = _hasEye ? Vector3.Lerp(_eye, seen, 1.0f - Mathf.Exp(-3.0f * dt)) : seen; _hasEye = true;
                camera.transform.SetPositionAndRotation(_eye, Quaternion.LookRotation(_aim - _eye, Vector3.up));
                camera.fieldOfView = fov;
            }
        }

        private static void Render(Camera camera, RenderTexture target, RenderTexture resolved, Texture2D image, string path)
        {
            var prior = camera.targetTexture;
            camera.targetTexture = target;
            camera.Render();
            camera.targetTexture = prior;
            Graphics.Blit(target, resolved);
            var was = RenderTexture.active;
            RenderTexture.active = resolved;
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); image.Apply();
            RenderTexture.active = was;
            File.WriteAllBytes(path, image.EncodeToJPG(93));
        }
    }
}
