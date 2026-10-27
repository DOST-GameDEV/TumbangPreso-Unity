using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Map;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests.Trailer
{
    /// <summary>
    /// Renders the trailer's in-engine shots to PNG frames at 24 a second on each film's own clock, with no UI on the
    /// picture (letterbox, cards and grade are added in the edit by tools/trailer_animatic.py). Output:
    /// Logs/trailer/frames/&lt;film&gt;/frame_####.png and shots.txt (name, first frame, last frame).
    /// Run one film at a time: TrailerRecording.Street, TrailerRecording.Arena.
    /// </summary>
    public sealed class TrailerRecording
    {
        private const int Width = 1920, Height = 1080;
        private const float Fps = 24.0f;
        private string _settings;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _settings = JsonUtility.ToJson(Settings.SettingsStore.Current);
        }

        [UnityTearDown] public IEnumerator After()
        {
            MapFilm.Stop();
            HoldClock(false);
            JsonUtility.FromJsonOverwrite(_settings, Settings.SettingsStore.Current);
            yield return PlayModeWorld.Reset();
        }

        [UnityTest, Timeout(3600000)] public IEnumerator Street() =>
            Record<TrailerStreetFilm>(SceneFlow.Eskinita, "street", TrailerStreetFilm.Length, TrailerStreetFilm.Shots);

        [UnityTest, Timeout(3600000)] public IEnumerator Arena() =>
            Record<TrailerArenaFilm>(SceneFlow.Arena, "arena", TrailerArenaFilm.Length, TrailerArenaFilm.Shots);

        private IEnumerator Record<T>(string map, string name, float length, (string name, float from, float to)[] shots) where T : MapFilm
        {
            LogAssert.ignoreFailingMessages = true;
            SceneFlow.Networked = false;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false; GameLaunch.Spectator = false; GameLaunch.AllBots = false;
            Settings.SettingsStore.Current.CinematicCameraMotion = true;
            Settings.SettingsStore.Current.ReducedUiMotion = false;
            yield return SceneManager.LoadSceneAsync(map);
            while (HubLoading.Preparing) yield return null;
            for (int i = 0; i < 4; i++) yield return null;
            foreach (var gate in Object.FindObjectsByType<ReadyGate>()) gate.enabled = false;
            foreach (var old in Object.FindObjectsByType<MatchArrivalPresentation>()) old.Cancel();
            foreach (var ai in Object.FindObjectsByType<AIController>()) ai.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>()) reader.enabled = false;
            HoldClock(true);
            yield return null;

            var camera = Camera.main;
            Assert.IsNotNull(camera, map + " has no camera");
            var rig = camera.GetComponent<CameraSystem.CameraRig>();
            if (rig != null) rig.SetActive(false);
            var players = GameServices.Round.Players.OrderBy(p => p.PlayerSlot).ToArray();

            if (MapFilm.Instance != null) Object.Destroy(MapFilm.Instance.gameObject);
            yield return null;
            var go = new GameObject("Trailer film (" + name + ")");
            SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
            var film = go.AddComponent<T>();
            Assert.IsTrue(film.Begin(camera, players, camera.transform.position, camera.transform.rotation, camera.fieldOfView, false), name + " refused to begin");

            string folder = Path.Combine("Logs", "trailer", "frames", name);
            if (Directory.Exists(folder)) foreach (var old in Directory.GetFiles(folder)) File.Delete(old);
            Directory.CreateDirectory(folder);
            File.WriteAllLines(Path.Combine(folder, "shots.txt"), shots.Select(s => $"{s.name}\t{Mathf.RoundToInt(s.from * Fps)}\t{Mathf.RoundToInt(s.to * Fps) - 1}"));

            var target = new RenderTexture(Width, Height, 24, camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32,
                camera.allowHDR ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.Default);
            target.antiAliasing = 1; target.Create();
            var resolved = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            resolved.Create();
            var prior = camera.targetTexture;
            camera.targetTexture = target;
            var shot = new GameObject("~TrailerShot").AddComponent<LateFrameShot>();
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            shot.Shoot = path =>
            {
                camera.Render();
                Graphics.Blit(target, resolved);
                var was = RenderTexture.active;
                RenderTexture.active = resolved;
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); image.Apply();
                RenderTexture.active = was;
                File.WriteAllBytes(path, image.EncodeToPNG());
            };

            film.HeldAge = 0.0f; film.Roll();
            yield return null; yield return null;
            int count = Mathf.CeilToInt(length * Fps);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    film.HeldAge = i / Fps;
                    yield return null;
                    yield return shot.Take(Path.Combine(folder, $"frame_{i:0000}.png"));
                }
            }
            finally
            {
                camera.targetTexture = prior;
                Object.Destroy(shot.gameObject);
                Object.Destroy(image);
                target.Release(); Object.Destroy(target);
                resolved.Release(); Object.Destroy(resolved);
            }
            Debug.Log($"[TrailerRecording] {name}: {count} frames to {folder}");
            film.HeldAge = -1.0f; film.End();
            yield return null;
        }

        private static void HoldClock(bool on) =>
            typeof(PresentationClock).GetMethod(on ? "Hold" : "Release", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
    }
}
