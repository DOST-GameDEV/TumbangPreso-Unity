using UnityEngine;

namespace TumbangPreso.Settings
{
    /// <summary>Quality changes spend detail on the street, never hide gameplay tells.</summary>
    public static class GraphicsProfiles
    {
        public readonly struct Entry
        {
            public readonly string Label;
            public readonly ShadowQuality Shadows;
            public readonly ShadowResolution Resolution;
            public readonly float Distance;
            public readonly int Cascades;
            public readonly int Lights;
            public readonly bool SoftParticles;
            /// <summary>The share of the desktop's pixels a fullscreen frame is drawn at; 1 is native.</summary>
            public readonly float RenderScale;
            /// <summary>Whether this tier trims the project quality level's own extras (see `Low`).</summary>
            public readonly bool Lean;

            public Entry(string label, ShadowQuality shadows, ShadowResolution resolution,
                         float distance, int cascades, int lights, bool softParticles,
                         float renderScale = 1f, bool lean = false)
            {
                Label = label;
                Shadows = shadows;
                Resolution = resolution;
                Distance = distance;
                Cascades = cascades;
                Lights = lights;
                SoftParticles = softParticles;
                RenderScale = renderScale;
                Lean = lean;
            }
        }

        // ⚠️ The old player inherited Ultra's 150 m shadow distance with no quality
        // control. The competitive court is 14 m across: even Low keeps shadows
        // over a full diagonal (19.8 m), while the higher tiers spend the extra
        // range on roadside depth. Nothing changes mesh detail, textures, outline
        // visibility or ability geometry. High's shorter frustum also gives its
        // near shadows more useful texels than the old 150 m setting.
        //
        // ⚠️⚠️ LOW IS THE INTEGRATED-GRAPHICS TIER (owner, 2026-10-29: "its crazy laggy without a gpu. which is not
        // optimal. even more so for the payo map"). An integrated GPU's cost follows its fill rate, pixels times
        // full-screen passes, so Low spends less on both: a fullscreen frame drawn at 70 per cent of the desktop's
        // width and height (49 per cent of the pixels, scaled up by the display), no MSAA whatever the anti-alias
        // pick (FXAA still runs when picked), no extra per-pixel lights (the sun stays per pixel; lamps and fires
        // light per vertex), hard shadows to 15 m (the players and slippers round the eye keep theirs), LOD bias 0.6 (Ultra's is 2,
        // so far meshes drop to their lighter levels sooner), no anisotropic filtering, soft vegetation or realtime reflection probes.
        // `Visual.WorldOutline` adds its own Low cuts (no depth-normals prepass, one edge sample a pixel) and
        // `Map.PayoDetail` Payo's. Balanced (the default) and High are unchanged, and switching back to them
        // restores exactly the project quality level's own values.
        public static readonly Entry[] All =
        {
            new Entry("Low", ShadowQuality.HardOnly, ShadowResolution.Low, 15f, 0, 0, false, 0.7f, true),
            new Entry("Balanced", ShadowQuality.All, ShadowResolution.Medium, 40f, 2, 2, false),
            new Entry("High", ShadowQuality.All, ShadowResolution.High, 70f, 2, 4, true),   // two cascades (owner, 2026-10-31, comparing 4 and 2: "yeah the cascades are good")
        };

        public const int Default = 1;

        /// <summary>
        /// THE STRIPES ON THE SUNLIT FLOORS (owner, 2026-10-31: "figure out a way to not have those stripey lines on the
        /// shading"). They were shadow acne, the floor shadowing itself in rows of shadow-map texels: with shadows off, or at
        /// three times the sun's normal bias, they are gone, while three times the depth bias alone leaves them. So every map's
        /// sun gets at least this normal bias. Measured at 0.48 on Lagoon and 0.6 on Payo; the long pole shadows keep their shape.
        /// </summary>
        private const float SunNormalBias = 0.6f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void WatchSunBias()
        {
            RaiseSunBias();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => RaiseSunBias();
        }

        private static void RaiseSunBias()
        {
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional && light.shadows != LightShadows.None && light.shadowNormalBias < SunNormalBias)
                    light.shadowNormalBias = SunNormalBias;
        }
        public static int Current { get; private set; } = Default;
        public static Entry Of(int index) => All[Mathf.Clamp(index, 0, All.Length - 1)];
        public static event System.Action AppliedInEditor;

        /// <summary>The fullscreen render scale of the tier in force.</summary>
        public static float RenderScale => Of(Current).RenderScale;

        // The project quality level's own values, read before the first Lean tier touches them.
        private static bool _baseRead;
        private static float _baseLodBias;
        private static AnisotropicFiltering _baseAniso;
        private static bool _baseSoftVegetation, _baseReflectionProbes;

        public static void Apply(int index)
        {
            float scaleWas = RenderScale;
            Current = Mathf.Clamp(index, 0, All.Length - 1);
            var profile = All[Current];
            QualitySettings.shadows = profile.Shadows;
            QualitySettings.shadowResolution = profile.Resolution;
            QualitySettings.shadowDistance = profile.Distance;
            QualitySettings.shadowCascades = profile.Cascades;
            QualitySettings.pixelLightCount = profile.Lights;
            QualitySettings.softParticles = profile.SoftParticles;

            if (!_baseRead)
            {
                _baseRead = true;
                _baseLodBias = QualitySettings.lodBias;
                _baseAniso = QualitySettings.anisotropicFiltering;
                _baseSoftVegetation = QualitySettings.softVegetation;
                _baseReflectionProbes = QualitySettings.realtimeReflectionProbes;
            }
            QualitySettings.lodBias = profile.Lean ? Mathf.Min(0.6f, _baseLodBias) : _baseLodBias;
            QualitySettings.anisotropicFiltering = profile.Lean ? AnisotropicFiltering.Disable : _baseAniso;
            QualitySettings.softVegetation = !profile.Lean && _baseSoftVegetation;
            QualitySettings.realtimeReflectionProbes = !profile.Lean && _baseReflectionProbes;

            // The MSAA cap rides on the anti-alias pick; re-pushed here so a tier change alone takes effect.
            AntiAliasModes.ApplySamples();

            // A fullscreen window takes the new scale at once; the settings own the resolution (`GameSettings.ApplyDisplay`).
            if (!Mathf.Approximately(scaleWas, profile.RenderScale) && SettingsStore.Loaded && SettingsStore.Current.Fullscreen)
                SettingsStore.Current.ApplyDisplay();

            AppliedInEditor?.Invoke();
        }

        /// <summary>
        /// THE FIRST-RUN TIER (owner, 2026-10-29: "its crazy laggy without a gpu"). Picked once, only while the player
        /// has never chosen (see `GameSettings.GraphicsTierDetected`): Low for a phone or tablet, an integrated GPU
        /// (Intel's integrated graphics, AMD's Vega and Radeon "Graphics"/7x0M laptop parts, Qualcomm Adreno on
        /// Windows), a GPU reporting under 2 GB of its own memory, an old shader model or no hardware device;
        /// otherwise the default. Intel Arc A- and B-series cards are discrete and keep the default.
        /// </summary>
        public static int Detect()
        {
            if (Application.isMobilePlatform) return 0;
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return Default;
            string name = (SystemInfo.graphicsDeviceName ?? "").ToLowerInvariant();
            string vendor = (SystemInfo.graphicsDeviceVendor ?? "").ToLowerInvariant();
            if (IsIntegrated(name, vendor)) return 0;
            if (SystemInfo.graphicsMemorySize > 0 && SystemInfo.graphicsMemorySize < 2048) return 0;
            if (SystemInfo.graphicsShaderLevel < 45) return 0;
            return Default;
        }

        public static bool IsIntegrated(string name, string vendor)
        {
            if (name.Contains("intel") || vendor.Contains("intel"))
                return !(name.Contains("arc(tm) a") || name.Contains("arc a") || name.Contains("arc(tm) b") || name.Contains("arc b"));
            if (name.Contains("adreno") || name.Contains("qualcomm")) return true;
            if (name.Contains("vega") && !name.Contains("rx vega")) return true;
            if (name.Contains("radeon") && !name.Contains("rx") && !name.Contains("pro w") && !name.Contains("vii")
                && (name.Contains("graphics") || System.Text.RegularExpressions.Regex.IsMatch(name, @"radeon(\(tm\))? \d{3}m"))) return true;
            return false;
        }
    }
}
