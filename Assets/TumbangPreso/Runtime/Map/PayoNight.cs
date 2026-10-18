using UnityEngine;
using UnityEngine.Rendering;
using TumbangPreso.Visual;

namespace TumbangPreso.Map
{
    /// <summary>
    /// PAYO TURNS TO NIGHT AT HALF TIME (owner, 2026-10-12: "i meant the map changes to night at half time", "with
    /// fireflies and shooting stars"). The first half is played by day; through the half-time break the sun goes,
    /// the sky and the air turn to the "PayoNight" row of `WorldLookProfile`, the moon (the goddess Mayari) stands
    /// over the valley to the north, and the second half is played under her.
    ///
    /// ⚠️ NOTHING IS SENT. How far into the night it is (`Factor`, 0 day to 1 night) is a pure function of what
    /// every peer already holds: the round number, the match's round count and, in the break after the middle
    /// round, that break's shared clock (`HalftimePresentation`, the clock the stage's own travel reads). A peer
    /// that joins in the second half is in the night at once; a dropped packet cannot leave a peer in the day.
    ///
    /// ⚠️ IT WRITES OVER THE LOOK EVERY FRAME, AND ONLY WHILE IT IS NOT DAY. `WorldLookPresentation` applies a
    /// map's row once and again when the lighting style's weight changes, and holds its row privately. The scene
    /// as built IS the "Payo" row (the Arena and Lagoon pattern), so by day this touches nothing and the look
    /// system is exactly what it is on every map. From the first frame of dusk it writes the ambient trilight,
    /// the fog, the key light and a sky material of its own each `LateUpdate`, blended between the two rows, and
    /// on the frame it is day again it puts the day row back once. The sky is an INSTANCE (the map's .mat is a
    /// shared asset: a colour written through would survive into the editor, `SkyEvent`'s lesson).
    ///
    /// The night's life (stars, shooting stars, fireflies, the fire bowls) is `PayoNightLife`, which reads `Factor`.
    /// </summary>
    [DefaultExecutionOrder(600)]
    public sealed class PayoNight : MonoBehaviour
    {
        public const string DayRow = "Payo", NightRow = "PayoNight";

        /// <summary>0 by day, 1 at night, between the two through the half-time break. Every peer computes the same.</summary>
        public static float Factor { get; private set; }

        /// <summary>The scene's key light, and where it points by day (the sun) and by night (the moon, from over the valley).</summary>
        public Light Sun;
        public Quaternion SunDay = Quaternion.identity, SunNight = Quaternion.identity;
        /// <summary>The goddess in the sky: drawn only at night, faded in by her renderers' `_Color` alpha.</summary>
        public Renderer[] Mayari = System.Array.Empty<Renderer>();
        /// <summary>Things that exist only at night (the art's `night` list): switched on as the dusk passes its middle.</summary>
        public GameObject[] NightOnly = System.Array.Empty<GameObject>();
        /// <summary>Things that glow at night (lit windows, lamps): their materials' emission is turned up with the night.</summary>
        public Renderer[] NightGlow = System.Array.Empty<Renderer>();
        /// <summary>Which of that renderer's materials glows (the same length as `NightGlow`).</summary>
        public int[] NightGlowIndex = System.Array.Empty<int>();
        /// <summary>Its emission at full night (the same length). By day it is the material's own (a window's is black, the
        /// turquoise inlay's a fifth of this).</summary>
        public Color[] NightGlowColour = System.Array.Empty<Color>();
        /// <summary>Lamps that are not fires (the lantern poles): off by day, brought up to `LampIntensity` with the night.</summary>
        public Light[] Lamps = System.Array.Empty<Light>();
        public float LampIntensity = 1.4f;
        /// <summary>The moon as the sky shader's own disc: its radius in radians and its halo, at full night.</summary>
        public float MoonDisc = 0.0f, MoonHalo = 0.0f;
        /// <summary>A probe or the review may hold the night at a value (0 to 1); under 0 it follows the match.</summary>
        [System.NonSerialized] public float Hold = -1.0f;

        private WorldLookProfile.MapLook _day, _night;
        private Material _sky, _skyFrom;
        private float _applied = -1.0f;
        private MaterialPropertyBlock _block;
        private static readonly int ColorId = Shader.PropertyToID("_Color"), EmissionId = Shader.PropertyToID("_EmissionColor");
        private Color[] _glowBase;

        private void OnEnable()
        {
            Factor = 0.0f; _applied = -1.0f;
            // A match of any length gets its half-time break here, and with it the dusk (see `HalftimePresentation.MiddleBreakMinRounds`).
            HalftimePresentation.MiddleBreakMinRounds = 2;
        }

        private void OnDisable()
        {
            if (_applied > 0.0f) Apply(0.0f);
            Factor = 0.0f;
            HalftimePresentation.MiddleBreakMinRounds = HalftimePresentation.DefaultMiddleBreakMinRounds;
            if (_sky != null) { if (RenderSettings.skybox == _sky && _skyFrom != null) RenderSettings.skybox = _skyFrom; Destroy(_sky); _sky = null; }
        }

        /// <summary>
        /// How far into the night the match is. Day before a match and through its first half; night from the round
        /// after the middle one; and in the break after the middle round, the dusk: it rides the part of that break
        /// in which the live stage is on screen (at half time the replay and the standings come first, on a still
        /// picture, and the sky turns while the stage rebuilds).
        /// </summary>
        public static float FactorFor(MatchDirector match, HalftimePresentation hp)
        {
            if (match == null || match.RoundNumber < 1) return 0.0f;
            int half = match.TotalRounds / 2;
            if (half < 1) return 0.0f;
            if (match.RoundNumber > half) return 1.0f;
            if (match.RoundNumber < half || hp == null || !hp.Active || hp.MatchId != match.PresentationMatchId || hp.CompletedRound != half) return 0.0f;

            float span = Mathf.Max(1.0f, hp.StageShowDuration - 1.0f);
            return Smooth((hp.StageShowAge + 1.0f) / span);
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3.0f - 2.0f * t);
        }

        private void LateUpdate()
        {
            float n = Hold >= 0.0f ? Mathf.Clamp01(Hold) : FactorFor(GameServices.Match, HalftimePresentation.Instance);
            Factor = n;
            // By day nothing is written: the look system's own values stand. The frame it turns back to day the day is put back once.
            if (n <= 0.0f && _applied <= 0.0f) return;
            Apply(n);
        }

        private bool Rows()
        {
            if (_day != null && _night != null) return true;
            Find(out _day, out _night);
            return _day != null && _night != null;
        }

        /// <summary>The two rows: the authored profile's, or the code's own when the asset was saved before they existed.</summary>
        public static void Find(out WorldLookProfile.MapLook day, out WorldLookProfile.MapLook night)
        {
            var profile = WorldLookProfile.Current;
            day = profile.Find(DayRow); night = profile.Find(NightRow);
            if (day != null && night != null) return;
            var fresh = ScriptableObject.CreateInstance<WorldLookProfile>();
            if (day == null) day = fresh.Find(DayRow);
            if (night == null) night = fresh.Find(NightRow);
            if (Application.isPlaying) Destroy(fresh); else DestroyImmediate(fresh);
        }

        /// <summary>
        /// Put the scene `n` of the way from the day row to the night row. Public so the editor's review can pose a
        /// night still without a match (it then restores with `Apply(0)`).
        /// </summary>
        public void Apply(float n)
        {
            if (!Rows()) return;
            _applied = n;
            var a = _day; var b = _night;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(a.Sky, b.Sky, n);
            RenderSettings.ambientEquatorColor = Color.Lerp(a.Equator, b.Equator, n);
            RenderSettings.ambientGroundColor = Color.Lerp(a.Ground, b.Ground, n);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.Lerp(a.Fog, b.Fog, n);
            RenderSettings.fogStartDistance = Mathf.Lerp(a.FogStart, b.FogStart, n);
            RenderSettings.fogEndDistance = Mathf.Lerp(a.FogEnd, b.FogEnd, n);

            if (Sun != null)
            {
                // The sun does not swing through the ground to become the moon: it dims to nearly nothing at the
                // middle of the dusk, and the light that comes back up is the moon's, from her side of the sky.
                float dip = 1.0f - 0.92f * Mathf.Sin(Mathf.Clamp01(n) * Mathf.PI);
                Sun.color = Color.Lerp(a.Sun, b.Sun, n);
                Sun.intensity = Mathf.Lerp(a.SunIntensity, b.SunIntensity, n) * dip;
                Sun.shadowStrength = Mathf.Lerp(a.ShadowStrength, b.ShadowStrength, n);
                Sun.transform.rotation = n < 0.5f ? SunDay : SunNight;
            }

            Sky(a, b, n);
            Clouds(a, b, n);

            // Mayari: unseen until the sun has all but gone, then she comes up out of the dark.
            float seen = Smooth((n - 0.45f) / 0.5f);
            _block ??= new MaterialPropertyBlock();
            foreach (var r in Mayari)
            {
                if (r == null) continue;
                bool on = seen > 0.002f;
                if (r.enabled != on) r.enabled = on;
                if (!on) continue;
                r.GetPropertyBlock(_block);
                _block.SetColor(ColorId, new Color(1.0f, 1.0f, 1.0f, seen));
                r.SetPropertyBlock(_block);
            }

            bool dark = n > 0.5f;
            foreach (var go in NightOnly) if (go != null && go.activeSelf != dark) go.SetActive(dark);
            foreach (var lamp in Lamps)
            {
                if (lamp == null) continue;
                bool on = seen > 0.01f;
                if (lamp.enabled != on) lamp.enabled = on;
                if (on) lamp.intensity = LampIntensity * seen;
            }

            if (NightGlow.Length > 0)
            {
                if (_glowBase == null || _glowBase.Length != NightGlow.Length)
                {
                    _glowBase = new Color[NightGlow.Length];
                    for (int i = 0; i < NightGlow.Length; i++)
                    {
                        var all = NightGlow[i] != null ? NightGlow[i].sharedMaterials : null;
                        int index = i < NightGlowIndex.Length ? NightGlowIndex[i] : 0;
                        var m = all != null && index < all.Length ? all[index] : null;
                        _glowBase[i] = m != null && m.HasProperty(EmissionId) ? m.GetColor(EmissionId) : Color.white;
                    }
                }
                for (int i = 0; i < NightGlow.Length; i++)
                {
                    var r = NightGlow[i];
                    if (r == null) continue;
                    int index = i < NightGlowIndex.Length ? NightGlowIndex[i] : 0;
                    r.GetPropertyBlock(_block, index);
                    _block.SetColor(EmissionId, i < NightGlowColour.Length ? Color.Lerp(_glowBase[i], NightGlowColour[i], seen) : _glowBase[i] * seen);
                    r.SetPropertyBlock(_block, index);
                }
            }

            if (n <= 0.0f && _sky != null)
            {
                // Day again: the sky the look system (or the scene) had is handed back.
                if (RenderSettings.skybox == _sky && _skyFrom != null) RenderSettings.skybox = _skyFrom;
            }
        }

        private void Sky(WorldLookProfile.MapLook a, WorldLookProfile.MapLook b, float n)
        {
            var live = RenderSettings.skybox;
            if (live == null || n <= 0.0f) return;
            if (live != _sky)
            {
                // The look system swaps the scene's sky for its own instance when the style's weight changes: ours follows whichever is live.
                _skyFrom = live;
                if (_sky == null) _sky = new Material(live) { name = live.name + " (Payo night)", hideFlags = HideFlags.HideAndDontSave };
                else { _sky.shader = live.shader; _sky.CopyPropertiesFromMaterial(live); }
                RenderSettings.skybox = _sky;
            }

            Set("_Zenith", Color.Lerp(a.Zenith, b.Zenith, n));
            Set("_Horizon", Color.Lerp(a.Horizon, b.Horizon, n));
            Set("_Ground", Color.Lerp(a.Fog, b.Fog, n) * Mathf.Lerp(0.8f, 0.6f, n));
            Set("_CloudLight", Color.Lerp(a.CloudLight, b.CloudLight, n));
            Set("_CloudShade", Color.Lerp(a.CloudShade, b.CloudShade, n));
            Set("_SunColor", Color.Lerp(a.Sun, b.Sun, n));
            if (Sun != null && _sky.HasProperty("_SunDirection")) _sky.SetVector("_SunDirection", -Sun.transform.forward);
            float moon = Smooth((n - 0.5f) / 0.5f);
            if (_sky.HasProperty("_SunDisc")) _sky.SetFloat("_SunDisc", MoonDisc * moon);
            if (_sky.HasProperty("_SunHalo")) _sky.SetFloat("_SunHalo", MoonHalo * moon);
        }

        private void Set(string id, Color colour) { if (_sky.HasProperty(id)) _sky.SetColor(id, colour); }

        private Renderer _cloudRenderer;
        private float _cloudScan;

        // The look's voxel clouds keep a material of their own (an instance made at run time, never an asset): at
        // night they take the night row's cloud colours, so a white cumulus does not stand in a dark sky.
        private void Clouds(WorldLookProfile.MapLook a, WorldLookProfile.MapLook b, float n)
        {
            if (_cloudRenderer == null && Time.unscaledTime >= _cloudScan)
            {
                _cloudScan = Time.unscaledTime + 1.0f;
                var clouds = FindFirstObjectByType<BlockyClouds>();
                _cloudRenderer = clouds != null ? clouds.GetComponentInChildren<Renderer>(true) : null;
            }
            var m = _cloudRenderer != null ? _cloudRenderer.sharedMaterial : null;
            if (m == null || (m.hideFlags & HideFlags.DontSave) == 0) return;      // only ever a run-time instance
            m.SetColor("_LitColor", Color.Lerp(a.CloudLight, b.CloudLight, n));
            m.SetColor("_ShadeColor", Color.Lerp(a.CloudShade, b.CloudShade, n));
            m.SetColor("_AirColor", Color.Lerp(a.Horizon, b.Horizon, n));
            m.SetColor("_ZenithColor", Color.Lerp(a.Zenith, b.Zenith, n));
        }
    }
}
