using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The one breeze on the title street. `OwnerMenuAir`, `OwnerMenuLeaves` and
    /// `OwnerRoadDust` all read their clock and their gusts from here, so a gust
    /// rustles the cast shadow, lifts the sand and pushes a leaf in the same
    /// second, and every effect blows the same way.
    ///
    /// ⚠️⚠️ THE THREE EFFECTS USED TO DISAGREE ABOUT THE WIND. The clouds and the
    /// dust travelled right while the leaves were "carried left by the same wind
    /// that moves the dust", so the street had two breezes at once. The whole
    /// street blows right to left now: off the canopy, across the road, towards
    /// the graffiti, which also walks the eye back to the logo.
    ///
    /// ⚠️ THE REFERENCE IS SLAY THE SPIRE 2'S MAIN MENU, MEASURED, NOT REMEMBERED.
    /// 🧑: *"it was supposed to be subtle like slay the spire 2's main menu"*. Its
    /// Spine rig (`mainmenu/bottom`, `top`, `logo`) was decoded frame by frame for
    /// this pass: one 160 second loop in which nothing large travels. Its cloud
    /// bank is nine segments each rocking 0.3 to 1.5 degrees on eased 5.3 second
    /// cycles plus a 2 per cent mesh deform, its glows pulse on 4 seconds, its
    /// stars twinkle in short sparse bursts and its boats bob 3 to 16 units. Life
    /// comes from many small correlated motions in place, not from one big one,
    /// and that is the rule every number in these four files follows.
    ///
    /// ⚠️ EVERYTHING IS A PURE FUNCTION OF THE CLOCK. No state is integrated frame
    /// to frame, so a frame rate hitch cannot move a leaf somewhere it was never
    /// checked to be, and the prototype the numbers were tuned in
    /// (`docs/reports/title-weather-2026-09-24/`) reproduces the game exactly.
    /// </summary>
    public static class OwnerMenuWind
    {
        private static float _origin = float.NaN;

        /// <summary>Seconds since the title street was built. Every effect shares it.</summary>
        public static float Now
        {
            get
            {
                if (float.IsNaN(_origin)) _origin = Time.unscaledTime;
                return Time.unscaledTime - _origin;
            }
        }

        /// <summary>Called once per title screen, so her clouds start where she painted them.</summary>
        public static void Restart() => _origin = Time.unscaledTime;

        public const float GustPeriod = 31f;

        /// <summary>
        /// 0 in the calm, up to 1 at the top of a gust. One gust in each 31 second
        /// window, placed at a hashed moment inside it, so the rhythm never locks to
        /// a beat the player could count. Rise 2.6 s, hold 1.6 s, fall 7 s: a breeze
        /// arrives quickly and dies away slowly, which is what makes it read as air.
        /// </summary>
        public static float Gust(float t)
        {
            int k = Mathf.FloorToInt(t / GustPeriod);
            float g = 0;
            for (int j = k - 1; j <= k + 1; j++)
            {
                float start = j * GustPeriod + (Hash(j, 11) * .6f + .2f) * GustPeriod;
                float d = t - start;
                if (d < 0) continue;
                const float rise = 2.6f, hold = 1.6f, fall = 7f;
                float e;
                if (d < rise) e = Smooth(0, 1, d / rise);
                else if (d < rise + hold) e = 1;
                else if (d < rise + hold + fall) e = 1 - Smooth(0, 1, (d - rise - hold) / fall);
                else e = 0;
                g = Mathf.Max(g, e * (.65f + .35f * Hash(j, 12)));
            }
            return g;
        }

        public static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3 - 2 * t);
        }

        // ⚠️ AN INTEGER HASH, NOT fract(sin(n)). The sine trick differs between a
        // float and a double and between GPUs; this is exact integer arithmetic,
        // so the prototype and the game plan the same leaf for the same cycle.
        public static float Hash(int a, int b) => Finish(Mix(Mix(0x9E3779B9u, a), b));
        public static float Hash(int a, int b, int c) => Finish(Mix(Mix(Mix(0x9E3779B9u, a), b), c));

        private static uint Mix(uint n, int key)
        {
            unchecked
            {
                n ^= (uint)key;
                n = n * 747796405u + 2891336453u;
                uint w = ((n >> (int)((n >> 28) + 4u)) ^ n) * 277803737u;
                return (w >> 22) ^ w;
            }
        }

        private static float Finish(uint n) => n / 4294967295f;

        /// <summary>The left edge of her road at a painted row, in source pixels.</summary>
        public static float GroundLeft(float y)
        {
            if (y < 644) return Mathf.Lerp(1104, 987, Mathf.InverseLerp(563, 644, y));
            if (y < 679) return Mathf.Lerp(987, 817, Mathf.InverseLerp(644, 679, y));
            if (y < 697) return Mathf.Lerp(817, 545, Mathf.InverseLerp(679, 697, y));
            if (y < 735) return Mathf.Lerp(545, 0, Mathf.InverseLerp(697, 735, y));
            return Mathf.Lerp(0, -160, Mathf.InverseLerp(735, 810, y));
        }

        public static float GroundRight(float y) => Mathf.Lerp(1730, 2050, Mathf.InverseLerp(565, 810, y));

        private const int SunWidth = 240, SunHeight = 135;
        private static float[] _sun;
        private static bool _sunTried;

        /// <summary>
        /// 1 where her street is in direct sun, 0 under her cast shadow. A dust mote
        /// only glints and a leaf only throws a shadow where the sun actually
        /// reaches, which is the difference between light and a sprite laid on top.
        ///
        /// ⚠️ READ BACK FROM THE GPU ONCE, AT AN EIGHTH OF THE SIZE, RATHER THAN BY
        /// MAKING `main2-shadow` READABLE. `OwnerMenuEditsAuthor.Prepare` owns that
        /// import and rewrites it on every run, so a readable flag set by hand would
        /// silently revert. With no graphics device the map is simply absent and
        /// everything counts as sunlit, which is the safe direction to be wrong in.
        /// </summary>
        public static float Sun(float x, float y)
        {
            if (!_sunTried) BuildSun();
            if (_sun == null) return 1;
            int xi = Mathf.Clamp((int)(x * SunWidth / 1920f), 0, SunWidth - 1);
            int yi = Mathf.Clamp((int)(y * SunHeight / 1080f), 0, SunHeight - 1);
            return _sun[yi * SunWidth + xi];
        }

        private static void BuildSun()
        {
            _sunTried = true;
            var shadow = OwnerMenuArt.Texture("main2-shadow");
            if (shadow == null || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var target = RenderTexture.GetTemporary(SunWidth, SunHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;
            Texture2D read = null;
            try
            {
                Graphics.Blit(shadow, target);
                RenderTexture.active = target;
                read = new Texture2D(SunWidth, SunHeight, TextureFormat.RGBA32, false, true);
                read.ReadPixels(new Rect(0, 0, SunWidth, SunHeight), 0, 0);
                read.Apply(false);
                var pixels = read.GetPixels32();
                var sun = new float[SunWidth * SunHeight];
                // Read-back rows run bottom up; her pixels run top down.
                for (int y = 0; y < SunHeight; y++)
                for (int x = 0; x < SunWidth; x++)
                    sun[y * SunWidth + x] = 1 - pixels[(SunHeight - 1 - y) * SunWidth + x].r / 255f;
                _sun = sun;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[OwnerMenuWind] Sun map unavailable, treating the street as sunlit: " + exception.Message);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                if (read != null) Object.Destroy(read);
            }
        }
    }
}
