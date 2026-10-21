using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// CONTINENTAL DRIFT ON THE SCREEN OF THE ONE IT HITS (owner, 2026-10-17, on v9: *"the Hit effect doesnt look that great
    /// (parang walanng nangyare after getting hit by the big rock shit)"*, *"make it feel like it hits harder both for POV ng
    /// player na natamaan and what everyone else sees"*, *"add vfx or shit to their screen"*). The view stays first person
    /// (unlike Amihan's wind, they keep their eyes) and takes the hit:
    ///  * THE HIT: two impact frames, the cutscene's own (ink on molten orange, lines in to the ground under them,
    ///    `Shaders/DanteImpact`), so the slam they just watched is what lands on them.
    ///  * THE FLIGHT: the view is thrown up toward the sky and rolls over with the body, easing back as they fall.
    ///  * THE LANDING (`Landed`, from `DanteDriftLaunch`): one more impact frame and the view slammed down and rolled back.
    /// Presentation only, on the camera following the body that was hit. Idle it is disabled, so it costs nothing.
    /// Reduce visual effects keeps the motion at half and drops the impact frames.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class DanteHitLens : MonoBehaviour
    {
        private const float ImpactFrames = .07f, LandFrames = .034f, LandSettle = .4f;
        private float _at = -100f, _landAt = -100f, _side = 1f;
        private Material _impact;

        /// <summary>The hit, on the camera following the body it struck.</summary>
        public static void Begin(Camera camera, int seed)
        {
            if (camera == null) return;
            var lens = camera.GetComponent<DanteHitLens>();
            if (lens == null) lens = camera.gameObject.AddComponent<DanteHitLens>();
            lens._at = Time.time; lens._landAt = -100f; lens._side = seed % 2 == 0 ? 1f : -1f;
            lens.enabled = true;
            var rig = camera.GetComponent<CameraRig>();
            if (rig != null) rig.HitLens = lens;
        }

        /// <summary>They are down: the landing's frame and slam.</summary>
        public void Landed() { if (enabled) _landAt = Time.time; }

        private static float Ease(float from, float to, float time)
        { float u = Mathf.InverseLerp(from, to, time); return u * u * (3 - 2 * u); }

        private static bool Reduced => Settings.SettingsStore.Current.ReducedEffects;

        /// <summary>The turn on the first-person view this frame (`CameraRig` adds it after its own pass).</summary>
        public Quaternion Tilt
        {
            get
            {
                float a = Time.time - _at, air = GeoRules.DriftLandSeconds;
                if (!enabled || a < 0f || a > air + 1f) return Quaternion.identity;
                // Thrown up at the sky in a snap, easing back down the arc; rolled over with the body.
                float up = a < .08f ? a / .08f : 1f - Ease(.08f, air, a);
                float pitch = -26f * up;
                float roll = 30f * _side * Mathf.Sin(Mathf.Clamp01(a / air) * Mathf.PI) * (1f - Ease(air * .7f, air + .2f, a));
                // The landing slams it down and rolls it back the other way.
                float l = Time.time - _landAt;
                if (l >= 0f && l < LandSettle)
                {
                    float hit = 1f - l / LandSettle;
                    pitch += 11f * hit * hit;
                    roll -= 8f * _side * hit * hit;
                }
                float scale = Reduced ? .5f : 1f;
                return Quaternion.Euler(pitch * scale, 0f, roll * scale);
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            float a = Time.time - _at, l = Time.time - _landAt;
            bool impact = (a >= 0f && a < ImpactFrames) || (l >= 0f && l < LandFrames);
            if (a > GeoRules.DriftLandSeconds + 1.2f && l > LandSettle) enabled = false;
            if (!impact || Reduced) { Graphics.Blit(source, destination); return; }
            if (_impact == null)
            {
                var shader = Resources.Load<Shader>("Shaders/DanteImpact");
                if (shader == null) { Graphics.Blit(source, destination); return; }
                _impact = new Material(shader) { name = "DanteHitImpact", hideFlags = HideFlags.HideAndDontSave };
            }
            _impact.SetFloat("_Impact", 1f);
            // The lines run in to the ground under them, where the stone came from.
            _impact.SetVector("_Focus", new Vector4(.5f, .18f, 0f, 0f));
            _impact.SetFloat("_Aspect", source.height > 0 ? source.width / (float)source.height : 16f / 9f);
            _impact.SetFloat("_Seed", l >= 0f && l < LandFrames ? 3f : 2f);
            Graphics.Blit(source, destination, _impact);
        }

        private void OnDestroy() { if (_impact != null) Destroy(_impact); }
    }
}
