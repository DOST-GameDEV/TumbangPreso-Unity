using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The title street's weather: her two cloud banks crossing the sky opening
    /// and her cast shadow swaying across the wall and the road.
    ///
    /// ⚠️⚠️ EVERY SHAPE HERE IS HERS. 46.png is 48.png plus painted clouds and
    /// their shadow, so `tools/author_owner_menu_v3.py` lifts both layers out of
    /// the difference instead of drawing new ones. Nothing in this file invents a
    /// cloud; it only decides where hers are this frame.
    ///
    /// ⚠️⚠️ SUBTLE IS A REQUIREMENT, NOT A TASTE, AND IT IS WHY THE NUMBERS ARE
    /// THIS SMALL. 🧑: *"make sure all main menu effects are subtle"*. The sway is
    /// 46 source pixels over 74 seconds, which is 1.2 px a second at the fastest
    /// part of the cycle: alive when you sit on the screen, invisible while you
    /// are reading it. The three periods are coprime-ish on purpose so the street
    /// never visibly repeats.
    ///
    /// ⚠️ THE SHADOW SWAYS, IT DOES NOT SWEEP. Her mask is exactly one screen
    /// wide, so a travelling drift has to do something at the edge: wrapping puts
    /// a seam on screen and a fade window puts a travelling bright band on it.
    /// Mirroring at the border is continuous, and a mirror only reads as a mirror
    /// if you cross it, so the drift stays inside a slow oscillation.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class OwnerMenuAir : MonoBehaviour
    {
        private static readonly int SkyColourId = Shader.PropertyToID("_SkyColour");
        private static readonly int ShadowTintId = Shader.PropertyToID("_ShadowTint");
        private static readonly int ShadowDriftId = Shader.PropertyToID("_ShadowDrift");
        private static readonly int CloudNearId = Shader.PropertyToID("_CloudNear");
        private static readonly int CloudFarId = Shader.PropertyToID("_CloudFar");
        private static readonly int CloudFadeId = Shader.PropertyToID("_CloudFade");

        /// <summary>Measured off 47.png and 48.png, which agree on it to the byte.</summary>
        public static readonly Vector4 SkyColour = new Vector4(136 / 255f, 200 / 255f, 119 / 255f, 1);

        /// <summary>The principal direction of 46.png minus 48.png over every shadowed
        /// ground pixel. Cool and blue-shifted, which is what a sky-lit shadow does
        /// and what she painted.</summary>
        public static readonly Vector4 ShadowTint = new Vector4(.7079f, .665f, 1.0149f, 1);

        // Her cloud mass was painted at this size and this height. The far bank is
        // the same art smaller and higher, which is depth rather than a second
        // drawing; at 4.9 per cent sky, mostly behind a canopy, one mass reads as
        // weather rather than as a repeat.
        private const float CloudWidth = 532, CloudHeight = 269;
        private const float NearScale = 1f, FarScale = .70f;
        private const float NearSpeed = 4.2f, FarSpeed = 2.2f;
        private const float NearTop = 28, FarTop = -10;

        private RawImage _image;
        private Material _material, _previous;
        private float _elapsed;

        private void Awake()
        {
            _image = GetComponent<RawImage>();
            var shader = Resources.Load<Shader>("UI/OwnerMenuAir");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[OwnerMenuAir] Air shader unavailable; the plate is drawn as supplied.");
                return;
            }
            _previous = _image.material;
            _material = new Material(shader) { name = "OwnerMenuAir", hideFlags = HideFlags.DontSave };
            _material.SetTexture("_SkyMask", OwnerMenuArt.Texture("main2-sky-mask"));
            _material.SetTexture("_Cloud", OwnerMenuArt.Texture("main2-cloud"));
            _material.SetTexture("_Shadow", OwnerMenuArt.Texture("main2-shadow"));
            _material.SetVector(SkyColourId, SkyColour);
            _material.SetVector(ShadowTintId, ShadowTint);
            _image.material = _material;
            Apply();
        }

        private void LateUpdate()
        {
            if (_material == null) return;
            if (!Settings.SettingsStore.Current.ReducedUiMotion) _elapsed += Time.unscaledDeltaTime;
            Apply();
        }

        private void Apply()
        {
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            float time = reduced ? 0 : _elapsed;

            // ⚠️ REDUCED MOTION IS A STILL FRAME OF HER PAINTING, NOT AN EMPTY SKY.
            // Parking the clouds at the x she drew them at and the shadow at zero
            // drift reproduces 46.png exactly, so the setting costs the art nothing.
            float span = 1920 + CloudWidth;
            float near = reduced ? 1243 : Mathf.Repeat(time * NearSpeed + 1243 + CloudWidth, span) - CloudWidth;
            float far = reduced ? 1243 : Mathf.Repeat(time * FarSpeed + 780 + CloudWidth, span) - CloudWidth;
            _material.SetVector(CloudNearId, new Vector4(near, NearTop, CloudWidth * NearScale, CloudHeight * NearScale));
            _material.SetVector(CloudFarId, new Vector4(far, FarTop, CloudWidth * FarScale, CloudHeight * FarScale));
            _material.SetVector(CloudFadeId, new Vector4(1f, .8f, 0, 0));

            float sway = reduced ? 0 : Mathf.Sin(time * Mathf.PI * 2 / 74f) * 46f;
            float rise = reduced ? 0 : Mathf.Sin(time * Mathf.PI * 2 / 101f) * 13f;
            float weight = reduced ? 1 : .9f + .1f * Mathf.Sin(time * Mathf.PI * 2 / 43f);
            _material.SetVector(ShadowDriftId, new Vector4(sway, rise, weight, 0));
        }

        private void OnDestroy()
        {
            if (_image != null && _image.material == _material) _image.material = _previous;
            if (_material != null) Destroy(_material);
        }
    }
}
