using System.Collections.Generic;
using System.Linq;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TumbangPreso.Map
{
    /// <summary>
    /// PAYO'S DETAIL LEVEL (owner, 2026-10-29: "its crazy laggy without a gpu ... even more so for the payo map"). One
    /// switch every Payo piece obeys: `Low` on the game's Low graphics tier (`GraphicsProfiles.Current == 0`, only READ
    /// here; the game-wide tier itself is `Settings.GraphicsProfiles`), or wherever `Override` says (a probe's A/B:
    /// 0 low, 1 full, under 0 follows the tier). Full is the approved look, untouched.
    ///
    /// On Low the driver, once per change: hides the grass and rice blades, the market crowd and the plaza's night life
    /// sparks; takes the dressing's plants and vines and the far houses out of the shadow map; holds the far houses at
    /// their lowest level; keeps only the `NightLights` map lights nearest the court (the windows and bowls still glow,
    /// that is emission). `PayoFlora` reads `FloraReach` and `ThinFar` itself, `PayoHaze` reads `HazeRays`/`HazeWisps`,
    /// `PayoNightLife` reads `Fireflies`.
    /// </summary>
    public static class PayoDetail
    {
        /// <summary>A probe's hold: 0 low, 1 full, under 0 the game's graphics tier decides.</summary>
        public static int Override = -1;
        public static bool Low => Override >= 0 ? Override == 0 : GraphicsProfiles.Current == 0;

        /// <summary>The instanced plants' draw and level-of-detail distances, as a share of full.</summary>
        public static float FloraReach => Low ? 0.55f : 1f;
        /// <summary>Past this many metres, every other instanced plant is skipped (0: none).</summary>
        public static float ThinFar => Low ? 90f : 0f;
        public static float HazeRays => Low ? 0f : 1f;
        public static float HazeWisps => Low ? 0.5f : 1f;
        public static bool Fireflies => !Low;
        /// <summary>Map point lights kept at night on Low (the nearest the court); every one on full.</summary>
        public const int NightLights = 4;
        /// <summary>Far houses beyond this (m from the court) held at their lowest level on Low.</summary>
        public const float FarHouse = 120f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= Loaded;
            SceneManager.sceneLoaded += Loaded;
            Loaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void Loaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == "Payo" && go.GetComponent<PayoDetailDriver>() == null) go.AddComponent<PayoDetailDriver>();
        }
    }

    /// <summary>Applies `PayoDetail` to the map's own renderers and lights when the level changes. No per-frame work past one compare.</summary>
    [DisallowMultipleComponent]
    public sealed class PayoDetailDriver : MonoBehaviour
    {
        private int _applied = -1;
        private readonly List<Renderer> _hidden = new List<Renderer>();
        private readonly List<(Renderer r, ShadowCastingMode mode)> _casters = new List<(Renderer, ShadowCastingMode)>();
        private readonly List<LODGroup> _farHouses = new List<LODGroup>();
        private readonly List<(Light l, int mask)> _culledLights = new List<(Light, int)>();

        public string Summary { get; private set; } = "";

        private void LateUpdate()
        {
            int want = PayoDetail.Low ? 0 : 1;
            if (want == _applied) return;
            Restore();
            _applied = want;
            TrimFarShadows();
            if (want == 0) ApplyLow();
        }

        private void OnDisable() { Restore(); _applied = -1; }

        private void Restore()
        {
            foreach (var r in _hidden) if (r != null) r.enabled = true;
            foreach (var (r, mode) in _casters) if (r != null) r.shadowCastingMode = mode;
            foreach (var g in _farHouses) if (g != null) g.ForceLOD(-1);
            foreach (var (l, mask) in _culledLights) if (l != null) l.cullingMask = mask;
            _hidden.Clear(); _casters.Clear(); _farHouses.Clear(); _culledLights.Clear();
            Summary = "full";
        }

        private void Hide(Renderer r) { if (r != null && r.enabled) { r.enabled = false; _hidden.Add(r); } }
        private void NoShadow(Renderer r) { if (r != null && r.shadowCastingMode != ShadowCastingMode.Off) { _casters.Add((r, r.shadowCastingMode)); r.shadowCastingMode = ShadowCastingMode.Off; } }

        /// <summary>How far from the court's centre (metres, flat) props and plants keep casting shadows on every tier.</summary>
        public const float ShadowKeep = 40f;

        // EVERY TIER: plants, trees, houses and props past `ShadowKeep` leave the shadow map (owner, 2026-10-30, on the
        // shadow passes being most of Payo's triangles: "go with #3 first. props near the main arena still need
        // shadows"). The court, its stones and the plaza dressing round it keep theirs; the land and terraces keep
        // theirs (they shade the paddies). Measured by each renderer's bounds, so a far piece of a near group still goes.
        private void TrimFarShadows()
        {
            var root = transform;
            foreach (var name in new[] { "Dressing", "Market", "Fires", "Plaza", "Life", "Actors" })
            {
                var group = root.Find(name);
                if (group == null) continue;
                foreach (var r in group.GetComponentsInChildren<Renderer>(true))
                {
                    if (r == null || r is ParticleSystemRenderer) continue;
                    var c = r.bounds.ClosestPoint(Vector3.zero);
                    if (new Vector2(c.x, c.z).magnitude > ShadowKeep) NoShadow(r);
                }
            }
        }

        private void ApplyLow()
        {
            var root = transform;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                string n = child.name;
                if (n == "PayoBlades" || n == "PayoGrass") foreach (var r in child.GetComponentsInChildren<Renderer>(true)) Hide(r);
            }
            // The market crowd: figures under the market named "<model> <pose>" (PayoSceneBuilder.Market), each with its LODGroup.
            foreach (var g in root.GetComponentsInChildren<LODGroup>(true))
            {
                string n = g.name;
                if (n.EndsWith(" stand") || n.EndsWith(" cheer") || n.EndsWith(" clap") || n.EndsWith(" sit"))
                    foreach (var r in g.GetComponentsInChildren<Renderer>(true)) Hide(r);
            }
            var dress = root.Find("Dressing");
            if (dress != null)
                foreach (Transform group in dress)
                {
                    string n = group.name;
                    bool plant = n.StartsWith("plant_") || n.StartsWith("cover_") || n.StartsWith("payo_plant") || n.StartsWith("tree_");
                    bool house = n.StartsWith("house");
                    if (plant) foreach (var r in group.GetComponentsInChildren<Renderer>(true)) NoShadow(r);
                    if (house)
                        foreach (var g in group.GetComponentsInChildren<LODGroup>(true))
                        {
                            var at = g.transform.position;
                            if (new Vector2(at.x, at.z).magnitude < PayoDetail.FarHouse) continue;
                            var lods = g.GetLODs();
                            if (lods.Length > 1) { g.ForceLOD(lods.Length - 1); _farHouses.Add(g); }
                            foreach (var r in g.GetComponentsInChildren<Renderer>(true)) NoShadow(r);
                        }
                }
            // The plaza's vines and planter plants: thin leaves whose shadow is a few pixels on Low's short shadow range.
            var plaza = root.Find("Plaza");
            if (plaza != null)
                foreach (var r in plaza.GetComponentsInChildren<Renderer>(true))
                    if (r.sharedMaterials.Any(m => m != null && m.shader != null && m.shader.name.Contains("Foliage"))) NoShadow(r);
            // Night lights: only the few nearest the court stay; the rest are left to their emissive glow.
            var lights = root.GetComponentsInChildren<Light>(true).Where(l => l.type != LightType.Directional)
                .OrderBy(l => new Vector2(l.transform.position.x, l.transform.position.z).sqrMagnitude).ToList();
            // Lit nothing (culling mask 0), not disabled: `PayoNight` switches its lamps on and off with the dusk every frame.
            for (int i = PayoDetail.NightLights; i < lights.Count; i++) if (lights[i].cullingMask != 0) { _culledLights.Add((lights[i], lights[i].cullingMask)); lights[i].cullingMask = 0; }
            Summary = $"low: {_hidden.Count} renderers hidden, {_casters.Count} out of the shadow map, {_farHouses.Count} far houses at their lowest level, {_culledLights.Count} of {lights.Count} map lights off";
        }
    }
}
