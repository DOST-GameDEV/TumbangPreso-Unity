using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// THE SKY IN THE WATERED PADDIES (owner, 2026-10-18: "add patches reflective water puddles across the terraces as to
    /// show some watered rice fields"). The flooded paddies (PayoLand, `_SURFACE_FLOODED` and the paddies the flood map
    /// turns to water, docs/PAYO_LAND_SHADER.md) reflect the sky WITHOUT a reflection camera or a probe: the shader
    /// rebuilds the sky's own gradient, sun glow and sun disc along the reflected ray, from the colours this hands it.
    ///
    /// The colours are read from the LIVE skybox material each frame (`RenderSettings.skybox`, NeighbourhoodSky): by day
    /// that is the map's sunset sky, and through the half-time dusk it is `PayoNight`'s own sky instance, so the water
    /// turns to night with the sky and shows the moon where the sky shows her. Runs after `PayoNight` (600).
    ///
    /// Globals only: `_PaddySkyZenith`, `_PaddySkyHorizon` (w = 1 when set; the shader falls back to the ambient and the
    /// fog colour without it), `_PaddySunColour`, `_PaddySunShape` (disc radius, halo), `_PaddySunDir` (the sky's `_SunDirection`), `_PaddyCloudLight` and `_PaddyCloudShade` (the clouds' tones, w the opacity). They are already linear and carry
    /// the sky's tint and exposure as the sky shader applies them. Cost: a few material reads a frame.
    /// </summary>
    [ExecuteAlways, DefaultExecutionOrder(650)]
    public sealed class PayoPaddySky : MonoBehaviour
    {
        private static readonly int ZenithId = Shader.PropertyToID("_PaddySkyZenith"), HorizonId = Shader.PropertyToID("_PaddySkyHorizon"),
            SunId = Shader.PropertyToID("_PaddySunColour"), ShapeId = Shader.PropertyToID("_PaddySunShape"), DirId = Shader.PropertyToID("_PaddySunDir"),
            CloudId = Shader.PropertyToID("_PaddyCloudLight"), CloudShadeId = Shader.PropertyToID("_PaddyCloudShade");
        private static readonly int Zenith = Shader.PropertyToID("_Zenith"), Horizon = Shader.PropertyToID("_Horizon"), SunColor = Shader.PropertyToID("_SunColor"),
            Tint = Shader.PropertyToID("_Tint"), Exposure = Shader.PropertyToID("_Exposure"), Disc = Shader.PropertyToID("_SunDisc"), Halo = Shader.PropertyToID("_SunHalo"), Direction = Shader.PropertyToID("_SunDirection"),
            CloudLight = Shader.PropertyToID("_CloudLight"), CloudShade = Shader.PropertyToID("_CloudShade"), CloudOpacity = Shader.PropertyToID("_CloudOpacity");

        // Set as soon as the scene is up too: an editor still renders before any LateUpdate runs.
        private void OnEnable() => Apply();
        private void LateUpdate() => Apply();

        public void Apply()
        {
            var sky = RenderSettings.skybox;
            if (sky == null || !sky.HasProperty(Zenith) || !sky.HasProperty(Horizon)) { Clear(); return; }
            // NeighbourhoodSky returns sky x _Tint x unity_ColorSpaceDouble x _Exposure: the same scale here, so the water
            // holds the colour the eye sees in the sky above it.
            Color tint = sky.HasProperty(Tint) ? sky.GetColor(Tint).linear : new Color(0.5f, 0.5f, 0.5f).linear;
            float exposure = sky.HasProperty(Exposure) ? sky.GetFloat(Exposure) : 1f;
            float twice = QualitySettings.activeColorSpace == ColorSpace.Linear ? 4.59479380f : 2f;
            Vector4 scale = new Vector4(tint.r, tint.g, tint.b, 0f) * (twice * exposure);
            Vector4 Lin(int id)
            {
                Color c = sky.HasProperty(id) ? sky.GetColor(id).linear : Color.black;
                return new Vector4(c.r * scale.x, c.g * scale.y, c.b * scale.z, 1f);
            }
            Shader.SetGlobalVector(ZenithId, Lin(Zenith));
            Shader.SetGlobalVector(HorizonId, Lin(Horizon));
            Shader.SetGlobalVector(SunId, Lin(SunColor));
            // the clouds' two tones (the night takes them too), w their opacity
            Vector4 lit = Lin(CloudLight), shade = Lin(CloudShade);
            lit.w = sky.HasProperty(CloudOpacity) ? sky.GetFloat(CloudOpacity) : 0f;
            Shader.SetGlobalVector(CloudId, lit);
            Shader.SetGlobalVector(CloudShadeId, shade);
            Shader.SetGlobalVector(ShapeId, new Vector4(sky.HasProperty(Disc) ? sky.GetFloat(Disc) : 0f, sky.HasProperty(Halo) ? sky.GetFloat(Halo) : 0f, 0f, 0f));
            // Where the sky draws its sun (and at night its moon), which the key light follows.
            Vector3 d = sky.HasProperty(Direction) ? (Vector3)sky.GetVector(Direction) : Vector3.up;
            d = d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.up;
            Shader.SetGlobalVector(DirId, new Vector4(d.x, d.y, d.z, 0f));
        }

        private static void Clear()
        {
            Shader.SetGlobalVector(HorizonId, Vector4.zero);
            Shader.SetGlobalVector(ShapeId, Vector4.zero);
        }

        private void OnDisable() => Clear();
    }
}
