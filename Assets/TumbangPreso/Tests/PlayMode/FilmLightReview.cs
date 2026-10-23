using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Gameplay in each map's own daylight beside gameplay in its opening film's light (owner, 2026-10-23: "the scenes
    /// dont match in lighting", "can u show me the game in the lighting of the cutscene?"). Each film blends the map toward a
    /// time of day (`MapFilm.Tint`) and the match snaps back to the authored day when it ends. This renders the same first
    /// person and wide views both ways, the film's mood at full weight exactly as the film applies it, into
    /// Logs/lighting-compare. Review only: nothing is asserted beyond staging.
    /// </summary>
    public sealed class FilmLightReview
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        private string _settings;

        [UnitySetUp] public IEnumerator Before() { yield return PlayModeWorld.Reset(); _settings = JsonUtility.ToJson(Settings.SettingsStore.Current); }
        [UnityTearDown] public IEnumerator After() { JsonUtility.FromJsonOverwrite(_settings, Settings.SettingsStore.Current); yield return PlayModeWorld.Reset(); }

        [UnityTest] public IEnumerator Eskinita() => Compare(SceneFlow.Eskinita, typeof(Map.TondoFilm), "Dusk");
        [UnityTest] public IEnumerator Bridge() => Compare(SceneFlow.IlalimNgTulay, typeof(Map.ErmitaFilm), "Night");
        [UnityTest] public IEnumerator Cove() => Compare(SceneFlow.LagoonCove, typeof(Map.SitangkaiFilm), "Golden");
        [UnityTest] public IEnumerator Kanto() => Compare(SceneFlow.Kanto, typeof(Map.BgcFilm), "Bright");
        [UnityTest] public IEnumerator Payo() => Compare(SceneFlow.Payo, typeof(Map.BanaueFilm), "Dawn");

        // The real opening of each map, as it now plays (owner, 2026-10-23: "the cutscene looks completely different from the
        // game"): four frames through the film, beside the gameplay stills above.
        [UnityTest] public IEnumerator EskinitaFilmFrames() => FilmFrames(SceneFlow.Eskinita);
        [UnityTest] public IEnumerator BridgeFilmFrames() => FilmFrames(SceneFlow.IlalimNgTulay);
        [UnityTest] public IEnumerator CoveFilmFrames() => FilmFrames(SceneFlow.LagoonCove);
        [UnityTest] public IEnumerator KantoFilmFrames() => FilmFrames(SceneFlow.Kanto);
        [UnityTest] public IEnumerator PayoFilmFrames() => FilmFrames(SceneFlow.Payo);

        private IEnumerator FilmFrames(string map)
        {
            SceneFlow.Networked = false;
            var rules = CustomGameRules.Defaults(GameMode.Classic); rules.ManualReady = false; rules.Bots = CustomGameRules.MaxBots;
            SceneFlow.PinSelectedRules(rules);
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false; GameLaunch.Spectator = false;
            Settings.SettingsStore.Current.CinematicCameraMotion = true; Settings.SettingsStore.Current.ReducedUiMotion = false;
            yield return SceneManager.LoadSceneAsync(map);
            string folder = Path.Combine("Logs", "lighting-compare"); Directory.CreateDirectory(folder);
            float[] marks = { .2f, .45f, .7f, .9f }; int next = 0;
            float by = Time.realtimeSinceStartup + 60;
            while (next < marks.Length && Time.realtimeSinceStartup < by && GameServices.Round?.RoundActive != true)
            {
                yield return null;
                var playing = Map.MapFilm.Instance;
                if (playing == null || !playing.Playing || Camera.main == null) continue;
                if (playing.Age < marks[next] * playing.Duration) continue;
                Save(Camera.main, Path.Combine(folder, $"{map}-cutscene-{next + 1}.png"));
                next++;
            }
            Assert.AreEqual(marks.Length, next, map + ": the film did not play through.");
        }

        private IEnumerator Compare(string map, System.Type film, string moodName)
        {
            yield return MapRetrievalProbe.Load(map);
            Map.MatchOpening.Stop();
            float by = Time.realtimeSinceStartup + 20;
            while (MatchArrivalPresentation.Active && Time.realtimeSinceStartup < by) yield return null;
            var round = GameServices.Round;
            var me = round.PlayerAt(GameLaunch.SoloSeat);
            var can = round.Lata.transform.position;
            // Stand the local seat a few metres off the can, facing it, so the first-person view frames the court.
            Vector3 back = (me.transform.position - can); back.y = 0; back = back.sqrMagnitude > 1 ? back.normalized : Vector3.back;
            Vector3 spot = can + back * 5f; spot.y = me.transform.position.y;
            me.Teleport(spot); me.transform.rotation = Quaternion.LookRotation(-back);
            var camera = Camera.main;
            var rig = camera.GetComponent<CameraRig>(); rig.Follow(me, true); rig.SetActive(true);
            yield return new WaitForSeconds(1f);

            string folder = Path.Combine("Logs", "lighting-compare"); Directory.CreateDirectory(folder);
            yield return Shots(camera, folder, map, "day", can, back);

            // The film's light, applied the way the film applies it, from a component that never runs (its object is inactive).
            var holder = new GameObject("FilmLightReview"); holder.SetActive(false);
            var instance = (Map.MapFilm)holder.AddComponent(film);
            typeof(Map.MapFilm).GetProperty("Camera", Any).SetValue(instance, camera);
            typeof(Map.MapFilm).GetMethod("SnapshotLight", Any).Invoke(instance, null);
            var mood = film.GetField(moodName, Any).GetValue(null);
            typeof(Map.MapFilm).GetMethod("Tint", Any).Invoke(instance, new[] { mood, (object)1f });
            try { yield return Shots(camera, folder, map, "film", can, back); }
            finally { typeof(Map.MapFilm).GetMethod("RestoreLight", Any).Invoke(instance, null); Object.Destroy(holder); }
        }

        private static IEnumerator Shots(Camera camera, string folder, string map, string light, Vector3 can, Vector3 back)
        {
            yield return null;
            Save(camera, Path.Combine(folder, $"{map}-fpp-{light}.png"));
            var was = camera.transform; Vector3 at = was.position; Quaternion turn = was.rotation; float fov = camera.fieldOfView;
            Vector3 wide = can + back * 9f + Vector3.Cross(Vector3.up, back) * 5f + Vector3.up * 5.5f;
            camera.transform.SetPositionAndRotation(wide, Quaternion.LookRotation(can + Vector3.up * .5f - wide)); camera.fieldOfView = 55;
            var hands = camera.GetComponentInChildren<ViewmodelArms>(true);
            bool shown = hands != null && hands.gameObject.activeSelf;
            if (hands != null) hands.gameObject.SetActive(false);
            Save(camera, Path.Combine(folder, $"{map}-wide-{light}.png"));
            if (hands != null) hands.gameObject.SetActive(shown);
            camera.transform.SetPositionAndRotation(at, turn); camera.fieldOfView = fov;
        }

        private static void Save(Camera camera, string path)
        {
            const int w = 1280, h = 720;
            var rt = new RenderTexture(w, h, 24, camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32);
            var ldr = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var read = new Texture2D(w, h, TextureFormat.RGB24, false);
            var target = camera.targetTexture;
            try
            {
                camera.targetTexture = rt; camera.Render(); camera.targetTexture = target;
                Graphics.Blit(rt, ldr);
                var active = RenderTexture.active; RenderTexture.active = ldr;
                read.ReadPixels(new Rect(0, 0, w, h), 0, 0); read.Apply(); RenderTexture.active = active;
                File.WriteAllBytes(path, read.EncodeToPNG());
            }
            finally { camera.targetTexture = target; Object.Destroy(rt); Object.Destroy(ldr); Object.Destroy(read); }
        }
    }
}
