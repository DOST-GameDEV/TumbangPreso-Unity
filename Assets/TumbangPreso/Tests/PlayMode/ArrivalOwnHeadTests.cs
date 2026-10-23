using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The opening never films the inside of this player's own head as it lands on their first-person eye (owner,
    /// 2026-10-23: "toward end of cutscene going to ur fpp view the camera records the inside of ur head", "fix that for
    /// all maps"). The real opening of every map in the build, in first person: every frame the main camera draws, a lens within 0.3 m of the player's own
    /// body must find that body hidden (shadows only). The last frames before the round are saved to
    /// Logs/arrival-own-head/&lt;map&gt; for a look.
    /// </summary>
    public sealed class ArrivalOwnHeadTests
    {
        private string _settings;
        private CustomRules _rules; private bool _pinned;

        [UnitySetUp] public IEnumerator Before()
        {
            _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
            _settings = JsonUtility.ToJson(Settings.SettingsStore.Current);
        }

        [UnityTearDown] public IEnumerator After()
        {
            JsonUtility.FromJsonOverwrite(_settings, Settings.SettingsStore.Current);
            yield return PlayModeWorld.Reset();
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest] public IEnumerator EskinitaOpeningHidesTheOwnHead() => Opening(SceneFlow.Eskinita);
        [UnityTest] public IEnumerator BridgeOpeningHidesTheOwnHead() => Opening(SceneFlow.IlalimNgTulay);
        [UnityTest] public IEnumerator CoveOpeningHidesTheOwnHead() => Opening(SceneFlow.LagoonCove);
        [UnityTest] public IEnumerator KantoOpeningHidesTheOwnHead() => Opening(SceneFlow.Kanto);
        [UnityTest] public IEnumerator ArenaOpeningHidesTheOwnHead() => Opening(SceneFlow.Arena);
        [UnityTest] public IEnumerator PayoOpeningHidesTheOwnHead() => Opening(SceneFlow.Payo);

        private IEnumerator Opening(string map)
        {
            SceneFlow.Networked = false;
            var rules = CustomGameRules.Defaults(GameMode.Classic); rules.ManualReady = false; rules.Bots = CustomGameRules.MaxBots;
            SceneFlow.PinSelectedRules(rules);
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false; GameLaunch.Spectator = false;
            Settings.SettingsStore.Current.CinematicCameraMotion = true;
            Settings.SettingsStore.Current.ReducedUiMotion = false;
            yield return SceneManager.LoadSceneAsync(map);
            float by = Time.realtimeSinceStartup + 15;
            while (Object.FindAnyObjectByType<ReadyGate>() == null && Time.realtimeSinceStartup < by) yield return null;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>()) reader.enabled = false;

            string folder = Path.Combine("Logs", "arrival-own-head", map); Directory.CreateDirectory(folder);
            foreach (var old in Directory.GetFiles(folder, "*.jpg")) File.Delete(old);
            var frames = new List<(Texture2D shot, float near)>();
            int seen = 0, nearFrames = 0, exposed = 0; float closest = 99, exposedAt = 99;
            var target = new RenderTexture(960, 540, 24);
            void Rendered(Camera camera)
            {
                if (camera != Camera.main || !MatchArrivalPresentation.Active) return;
                var rig = camera.GetComponent<CameraRig>();
                var me = GameServices.Round?.PlayerAt(GameLaunch.SoloSeat);
                if (rig == null || me == null || rig.Mode != CameraMode.Fpp) return;
                seen++;
                float near = 99; bool hidden = true;
                Vector3 lens = camera.transform.position;
                foreach (var r in me.GetComponentsInChildren<Renderer>(false))
                {
                    if (r == null || !r.enabled || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                    near = Mathf.Min(near, Vector3.Distance(r.bounds.ClosestPoint(lens), lens));
                    if (r.shadowCastingMode != ShadowCastingMode.ShadowsOnly) hidden = false;
                }
                closest = Mathf.Min(closest, near);
                if (near < .3f) { nearFrames++; if (!hidden) { exposed++; exposedAt = Mathf.Min(exposedAt, near); } }
            }
            Camera.onPostRender += Rendered;
            try
            {
                by = Time.realtimeSinceStartup + 45;
                while (GameServices.Round?.RoundActive != true && Time.realtimeSinceStartup < by)
                {
                    // Batch mode draws no window, so each frame is rendered here, as the player's screen would be.
                    yield return null;
                    var camera = Camera.main;
                    if (camera != null && camera.enabled && MatchArrivalPresentation.Active)
                    {
                        var me = GameServices.Round?.PlayerAt(GameLaunch.SoloSeat);
                        float near = 99;
                        if (me != null)
                            foreach (var r in me.GetComponentsInChildren<Renderer>(false))
                                if (r != null && r.enabled && !(r is ParticleSystemRenderer)) near = Mathf.Min(near, Vector3.Distance(r.bounds.ClosestPoint(camera.transform.position), camera.transform.position));
                        var was = camera.targetTexture; camera.targetTexture = target; camera.Render(); camera.targetTexture = was;
                        if (near < 3f)
                        {
                            var active = RenderTexture.active; RenderTexture.active = target;
                            var shot = new Texture2D(960, 540, TextureFormat.RGB24, false); shot.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); shot.Apply();
                            RenderTexture.active = active;
                            frames.Add((shot, near));
                            if (frames.Count > 40) { Object.Destroy(frames[0].shot); frames.RemoveAt(0); }
                        }
                    }
                }
            }
            finally { Camera.onPostRender -= Rendered; target.Release(); Object.Destroy(target); }
            for (int i = 0; i < frames.Count; i++)
            {
                File.WriteAllBytes(Path.Combine(folder, $"{i:D3}-near{frames[i].near:F2}.jpg"), frames[i].shot.EncodeToJPG(85));
                Object.Destroy(frames[i].shot);
            }
            Debug.Log($"[ArrivalOwnHead] {map}: frames {seen}, closest {closest:F2} m, near {nearFrames}, exposed {exposed}");
            Assert.IsTrue(GameServices.Round?.RoundActive == true, "The round never went live.");
            Assert.Greater(seen, 30, "The opening was never drawn in first person.");
            Assert.Greater(nearFrames, 0, "The opening never came near the player's own eye, so nothing was checked.");
            Assert.AreEqual(0, exposed, $"{map}: the lens came within {exposedAt:F2} m of the player's own drawn body.");
        }
    }
}
