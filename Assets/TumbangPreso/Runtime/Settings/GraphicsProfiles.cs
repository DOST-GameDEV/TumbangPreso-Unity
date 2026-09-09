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

            public Entry(string label, ShadowQuality shadows, ShadowResolution resolution,
                         float distance, int cascades, int lights, bool softParticles)
            {
                Label = label;
                Shadows = shadows;
                Resolution = resolution;
                Distance = distance;
                Cascades = cascades;
                Lights = lights;
                SoftParticles = softParticles;
            }
        }

        // ⚠️ The old player inherited Ultra's 150 m shadow distance with no quality
        // control. The competitive court is 14 m across: even Low keeps shadows
        // over a full diagonal (19.8 m), while the higher tiers spend the extra
        // range on roadside depth. Nothing changes mesh detail, textures, outline
        // visibility or ability geometry. High's shorter frustum also gives its
        // near shadows more useful texels than the old 150 m setting.
        public static readonly Entry[] All =
        {
            new Entry("Low", ShadowQuality.HardOnly, ShadowResolution.Low, 24f, 0, 1, false),
            new Entry("Balanced", ShadowQuality.All, ShadowResolution.Medium, 40f, 2, 2, false),
            new Entry("High", ShadowQuality.All, ShadowResolution.High, 70f, 4, 4, true),
        };

        public const int Default = 1;
        public static int Current { get; private set; } = Default;
        public static Entry Of(int index) => All[Mathf.Clamp(index, 0, All.Length - 1)];
        public static event System.Action AppliedInEditor;

        public static void Apply(int index)
        {
            Current = Mathf.Clamp(index, 0, All.Length - 1);
            var profile = All[Current];
            QualitySettings.shadows = profile.Shadows;
            QualitySettings.shadowResolution = profile.Resolution;
            QualitySettings.shadowDistance = profile.Distance;
            QualitySettings.shadowCascades = profile.Cascades;
            QualitySettings.pixelLightCount = profile.Lights;
            QualitySettings.softParticles = profile.SoftParticles;
            AppliedInEditor?.Invoke();
        }
    }
}
