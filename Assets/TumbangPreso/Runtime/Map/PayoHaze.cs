using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// PAYO'S AERIAL HAZE (owner, 2026-10-18: "should add a haze"). The look's fog is linear from 80 m and whole only at
    /// 3000 m, so the far ranges fade one behind another, but across the valley (100 m to 1 km) it is a few per cent:
    /// the town, the terraces and the near ridges stood out sharp as if in a vacuum. Linear fog cannot be bent to add
    /// the middle without flattening the ranges, and the fog mode is not free to change (the build keeps only the fog
    /// variants the scenes use), so this haze is drawn over the frame by `Visual.WorldOutline`'s composite, from the
    /// camera's depth: it grows with distance (an exponential, never whole) and stays low in the valley (fading out
    /// under a ceiling), in the scene's own fog colour, so it meets the fog the materials add without a seam, warm peach by day,
    /// the night row's blue after the half-time dusk. The sky is left alone.
    ///
    /// Globals only: a scene without a `PayoHaze` has none, and this one clears them when it goes. Cost: one depth
    /// read and a few instructions a pixel in a pass that runs anyway; only Payo's far plane compiles it in
    /// (`WORLD_AO_FINE_DEPTH`).
    /// </summary>
    [ExecuteAlways]
    public sealed class PayoHaze : MonoBehaviour
    {
        /// <summary>The most the haze takes, by day and at night (the night's air is darker and clearer).</summary>
        // These are NOT saved in the scene: the code is the one place they are tuned, so a change shows without a
        // rebuild (owner tuning live, 2026-10-19: "it looks just a bit too dreamy/haze-y").
        [System.NonSerialized] public float Strength = 0.38f, NightStrength = 0.5f;
        /// <summary>Metres from the eye before any haze, and over which it reaches 63 per cent of its most.</summary>
        [System.NonSerialized] public float Start = 40f, Reach = 700f;
        /// <summary>Whole under `Base` (m), none `Thins` metres above it: a ceiling, so it stays down in the valley (owner,
        /// 2026-10-18: "the fog/haze reaches too high. it should only be within the valley").</summary>
        [System.NonSerialized] public float Base = -45f, Thins = 45f;
        /// <summary>Wisps of cloud drifting in the haze (owner, 2026-10-19: "should have some wispy moving clouds in the
        /// haze"): the most a wisp takes (scaled down at night like the haze), the two sheets' heights (m, inside the
        /// valley, under the court), the wisps' size (m), the wind's heading (degrees from north) and speed (m/s).</summary>
        [System.NonSerialized] public float Wisps = 0.7f;
        /// <summary>God rays and the lens flare (owner, 2026-10-19: "can we have some god rays and lens flare?"):
        /// how strong each is by day; both fade out through the half-time dusk (the moon gets none).</summary>
        [System.NonSerialized] public float Rays = 0.25f, Flare = 0.18f;
        [System.NonSerialized] public float WispHigh = 0f, WispLow = -24f, WispSize = 45f, WindHeading = 15f, WindSpeed = 7f;
        /// <summary>The wisps' drift heading (degrees from north), across the valley's axis so the motion shows.</summary>
        [System.NonSerialized] public float DriftHeading = 80f;

        private static readonly int HazeId = Shader.PropertyToID("_MapHaze"), BaseId = Shader.PropertyToID("_MapHazeBase"), ColourId = Shader.PropertyToID("_MapHazeColor"),
            WispId = Shader.PropertyToID("_MapHazeWisp"), WindId = Shader.PropertyToID("_MapHazeWind"),
            SunId = Shader.PropertyToID("_MapSun"), SunColourId = Shader.PropertyToID("_MapSunColor");

        // Set as soon as the scene is up too, not only each frame: an editor still renders before any LateUpdate runs.
        private void OnEnable() => Apply();
        private void LateUpdate() => Apply();

        public void Apply()
        {
            float most = Mathf.Lerp(Strength, NightStrength, PayoNight.Factor);
            Shader.SetGlobalVector(HazeId, new Vector4(most, Mathf.Max(1f, Reach), Start, Mathf.Max(1f, Thins)));
            Shader.SetGlobalVector(BaseId, new Vector4(Base, 0f, 0f, 0f));
            // At night a moonlit mist, lighter than the dark air, or it vanishes into it (owner, 2026-10-19: "i dont
            // notice the haze and fog much anymore").
            Shader.SetGlobalVector(ColourId, RenderSettings.fogColor.linear * Mathf.Lerp(1f, 2.2f, PayoNight.Factor));
            float wisps = Mathf.Min(0.85f, Wisps * (Strength > 0f ? most / Strength : 0f));
            Shader.SetGlobalVector(WispId, new Vector4(wisps, WispHigh, WispLow, 1f / Mathf.Max(1f, WispSize)));
            float heading = WindHeading * Mathf.Deg2Rad;
            float drift = DriftHeading * Mathf.Deg2Rad;
            Shader.SetGlobalVector(WindId, new Vector4(Mathf.Sin(heading), Mathf.Cos(heading), Mathf.Sin(drift) * WindSpeed, Mathf.Cos(drift) * WindSpeed));
            Light sun = RenderSettings.sun;
            float day = 1f - PayoNight.Factor;
            if (sun != null && day > 0f)
            {
                Vector3 to = -sun.transform.forward;
                Shader.SetGlobalVector(SunId, new Vector4(to.x, to.y, to.z, Rays * day));
                Color light = sun.color.linear;
                Shader.SetGlobalVector(SunColourId, new Vector4(light.r, light.g, light.b, Flare * day));
            }
            else Shader.SetGlobalVector(SunId, Vector4.zero);
            if (sun == null || day <= 0f) Shader.SetGlobalVector(SunColourId, Vector4.zero);
        }

        private void OnDisable()
        {
            Shader.SetGlobalVector(HazeId, Vector4.zero);
            Shader.SetGlobalVector(WispId, Vector4.zero);
            Shader.SetGlobalVector(SunId, Vector4.zero);
            Shader.SetGlobalVector(SunColourId, Vector4.zero);
        }
    }
}
