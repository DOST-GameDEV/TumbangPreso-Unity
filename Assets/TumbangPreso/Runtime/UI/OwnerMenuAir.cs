using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The title street's weather: her cloud mass drifting and churning in the sky
    /// opening, her cast shadow rustling on the road, and now and then the soft
    /// shadow of a cloud passing over the whole street.
    ///
    /// ⚠️⚠️ EVERY SHAPE HERE IS HERS. 46.png is 48.png plus painted clouds and
    /// their shadow, so `tools/author_owner_menu_v3.py` lifts both layers out of
    /// the difference instead of drawing new ones. The passing cloud shadow is her
    /// cloud too, read at its smallest mip so only the silhouette's weight is left.
    /// Nothing in this file invents a shape; it only decides where hers are.
    ///
    /// ⚠️⚠️ SUBTLE IS A REQUIREMENT, NOT A TASTE. 🧑: *"make sure all main menu
    /// effects are subtle"*, and on 2026-09-19, *"it was supposed to be subtle like
    /// slay the spire 2's main menu"*. `OwnerMenuWind` records what that menu
    /// actually does, measured off its rig: many small motions in place, driven by
    /// light, rather than one big one.
    ///
    /// ⚠️⚠️ THE SHADOW RUSTLES, IT NO LONGER SWAYS, AND THE SWAY WAS THE FAULT. It
    /// slid her whole mask 46 source pixels across the road on a 74 second sine, so
    /// the ground itself appeared to swim, and the mirror at the border folded a
    /// visible vertical crease into the shade at the foot of the wall. A shadow
    /// cast by leaves stays where the tree is; what moves is its edges. Two slow
    /// warps with wavelengths of 300 to 480 pixels move neighbouring patches
    /// independently, and a small quicker one shimmers the edges, 1.5 pixels in the
    /// calm and 7 at the top of a gust. The 101 second rise of 3 pixels and the 43
    /// second breathing of its weight are kept so the pattern never sits dead still.
    ///
    /// ⚠️⚠️ THE SKY WAS EMPTY HALF THE TIME AND NOW NEVER IS. Two banks wrapped across
    /// the full 1920 pixel screen, but her sky opening is only x 975 to 1800 and
    /// mostly behind the canopy, so over 25 simulated minutes the opening held no
    /// visible cloud for 51 per cent of the time, once for 205 seconds straight,
    /// against 61 per cent cover in her own painting. Each bank now wraps just
    /// outside the opening, behind the roof on the left and the canopy on the
    /// right, and the middle and far banks are two instances half a span apart.
    /// Over a simulated hour the opening is never empty, median cover 35 per cent,
    /// 90th percentile 55. The speeds are still his: 8.4 near and 4.4 far, the
    /// 2 : 1 ratio that is the depth, with the new middle bank at 6.3 between them.
    ///
    /// ⚠️ THE BANKS SIT ON THE SKYLINE BECAUSE HER CLOUD WAS PAINTED ON IT. The lifted
    /// mass has a flat underside where the hills crossed it; the far bank floated
    /// at y -10 and showed that straight edge in open sky. Every moving bank rests
    /// its base on y 300 now, and further means smaller and lower, which is how a
    /// cloud recedes. Her two cut sides are dissolved while moving (`_CloudFade.w`).
    ///
    /// ⚠️ CHURN IS SLAY THE SPIRE 2'S CLOUD RIG, TRANSCRIBED. Its bank is nine
    /// segments each rocking about a degree on an eased 5.3 second cycle, plus a
    /// 2 per cent deform. Here that is a 1.8 per cent billow about the base on 11
    /// seconds and two slow warps that move each lobe on its own, so the mass
    /// breathes while it drifts rather than sliding past as a sticker.
    ///
    /// ⚠️ NOTHING IN THE SHADER READS _Time. Every motion comes in through a property
    /// set here, so disabling this component freezes the frame exactly, which is
    /// what `OwnerMenuSkyTests` relies on to move one bank by hand and assert that
    /// nothing outside the sky changed. The passing cloud shadow is driven by its
    /// own property for the same reason: moving a sky bank must not move the road.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class OwnerMenuAir : MonoBehaviour
    {
        private static readonly int SkyColourId = Shader.PropertyToID("_SkyColour");
        private static readonly int ShadowTintId = Shader.PropertyToID("_ShadowTint");
        private static readonly int ShadowDriftId = Shader.PropertyToID("_ShadowDrift");
        private static readonly int CloudNearId = Shader.PropertyToID("_CloudNear");
        private static readonly int CloudFarId = Shader.PropertyToID("_CloudFar");
        private static readonly int CloudFarBId = Shader.PropertyToID("_CloudFarB");
        private static readonly int CloudMidAId = Shader.PropertyToID("_CloudMidA");
        private static readonly int CloudMidBId = Shader.PropertyToID("_CloudMidB");
        private static readonly int CloudFadeId = Shader.PropertyToID("_CloudFade");
        private static readonly int CloudShadeId = Shader.PropertyToID("_CloudShade");
        private static readonly int AirTimeId = Shader.PropertyToID("_AirTime");

        /// <summary>Measured off 47.png and 48.png, which agree on it to the byte.</summary>
        public static readonly Vector4 SkyColour = new Vector4(136 / 255f, 200 / 255f, 119 / 255f, 1);

        /// <summary>The principal direction of 46.png minus 48.png over every shadowed
        /// ground pixel. Cool and blue-shifted, which is what a sky-lit shadow does
        /// and what she painted.</summary>
        public static readonly Vector4 ShadowTint = new Vector4(.7079f, .665f, 1.0149f, 1);

        private const float CloudWidth = 532, CloudHeight = 269;
        // ⚠️ HIS SPEEDS, CHOSEN 2026-09-19: near 8.4 and far 4.4 source pixels a second.
        // The middle bank is new and sits between them at 6.3.
        private const float NearSpeed = 8.4f, MidSpeed = 6.3f, FarSpeed = 4.4f;
        private const float MidScale = .84f, FarScale = .70f;
        // Where her opening ends on the right, behind the canopy. Each bank enters here
        // and leaves fully past x 975, behind the roof, before it wraps.
        private const float Entry = 1800;
        // Spans, and the rest positions that put her near bank where she painted it at
        // t = 0, found by searching an hour of simulated sky for the steadiest cover
        // with no span shorter than the opening plus the cloud (which would pop it).
        private const float NearSpan = 1370, MidSpan = 1290, FarSpan = 1300;
        private const float Skyline = 300;
        // A cloud's shadow crossing the street: once every 150 seconds, 30 pixels a
        // second right to left, about a tenth darker at its heart. Light is the motion
        // Slay the Spire 2 leans on most, and this is the one this street has.
        private const float ShadePeriod = 150, ShadeSpeed = 30, ShadeWidth = 1700, ShadeHeight = 900;

        private RawImage _image;
        private Material _material, _previous;

        private void Awake()
        {
            _image = GetComponent<RawImage>();
            OwnerMenuWind.Restart();
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
            Apply();
        }

        private static float Bank(float rest, float speed, float span, float t) =>
            Entry - Mathf.Repeat(Entry - rest + speed * t, span);

        private void Apply()
        {
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;

            // ⚠️ REDUCED MOTION IS A STILL FRAME OF HER PAINTING, NOT AN EMPTY SKY.
            // Parking the near and far banks where she drew them, with no churn, no
            // rustle, no passing shade and the extra instances out of sight,
            // reproduces 46.png exactly, so the setting costs the art nothing.
            if (reduced)
            {
                _material.SetVector(CloudNearId, new Vector4(1243, 28, CloudWidth, CloudHeight));
                _material.SetVector(CloudFarId, new Vector4(1243, -10, CloudWidth * FarScale, CloudHeight * FarScale));
                var away = new Vector4(-4000, 0, 1, 1);
                _material.SetVector(CloudFarBId, away);
                _material.SetVector(CloudMidAId, away);
                _material.SetVector(CloudMidBId, away);
                _material.SetVector(CloudFadeId, new Vector4(1f, .8f, 0, 0));
                _material.SetVector(ShadowDriftId, new Vector4(0, 0, 1, 0));
                _material.SetVector(AirTimeId, Vector4.zero);
                return;
            }

            float t = OwnerMenuWind.Now;
            float gust = OwnerMenuWind.Gust(t);
            float farW = CloudWidth * FarScale, farH = CloudHeight * FarScale;
            float midW = CloudWidth * MidScale, midH = CloudHeight * MidScale;
            _material.SetVector(CloudNearId, new Vector4(Bank(1243, NearSpeed, NearSpan, t), 28, CloudWidth, CloudHeight));
            _material.SetVector(CloudFarId, new Vector4(Bank(593, FarSpeed, FarSpan, t), Skyline - farH, farW, farH));
            _material.SetVector(CloudFarBId, new Vector4(Bank(-57, FarSpeed, FarSpan, t), Skyline - farH, farW, farH));
            _material.SetVector(CloudMidAId, new Vector4(Bank(275.5f, MidSpeed, MidSpan, t), Skyline - midH, midW, midH));
            _material.SetVector(CloudMidBId, new Vector4(Bank(-369.5f, MidSpeed, MidSpan, t), Skyline - midH, midW, midH));
            // near, far, middle opacity, churn
            _material.SetVector(CloudFadeId, new Vector4(1f, .8f, .9f, 1));

            float rise = Mathf.Sin(t * Mathf.PI * 2 / 101f) * 3f;
            float weight = .93f + .07f * Mathf.Sin(t * Mathf.PI * 2 / 43f);
            float rustle = 1.5f + 5.5f * gust;
            _material.SetVector(ShadowDriftId, new Vector4(0, rise, weight, rustle));

            int pass = Mathf.FloorToInt(t / ShadePeriod);
            float into = t - pass * ShadePeriod;
            float strength = .34f * (.8f + .4f * OwnerMenuWind.Hash(pass, 4));
            _material.SetVector(CloudShadeId, new Vector4(1920 + 200 - ShadeSpeed * into,
                380 + 120 * (OwnerMenuWind.Hash(pass, 3) - .5f), ShadeWidth, ShadeHeight));
            _material.SetVector(AirTimeId, new Vector4(t, gust, strength, 0));
        }

        private void OnDestroy()
        {
            if (_image != null && _image.material == _material) _image.material = _previous;
            if (_material != null) Destroy(_material);
        }
    }
}
