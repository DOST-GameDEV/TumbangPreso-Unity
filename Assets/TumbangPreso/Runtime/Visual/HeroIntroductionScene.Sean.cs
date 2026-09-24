using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // SEAN, SUPERNOVA, 3.4 s. plan.md § 1.
        //
        // "Waits for one opening. Makes it count." He grew up assembling lantern frames in San
        // Fernando and "learned to notice the small mistake that throws a whole shape off"
        // (CHARACTER_ORIGINS.md), so his fire is BUILT, not summoned: a parol frame of five fire
        // sticks lays itself between his cupped hands one stick at a time while he watches it,
        // the star fills with flame, and behind him a dusk falls with paper-lantern stars
        // drifting up (his memory of home, drawn only in the intro). When his head snaps up the
        // world holds its breath; the coil pulls the lantern in, flames lick his feet, and on the
        // rise the lantern bursts into sparks that race up ahead of the live leap.
        //
        // ⚠️ THE PAROL IS A PENTAGRAM OF FIVE STICKS because that is how a parol frame is
        // actually built: five bamboo sticks, each joining a point to the point two along. It is
        // his craft made visible, not a generic star particle.
        // =========================================================================================
        private int _duskLow, _duskGlow, _duskHigh, _parolFill;
        private readonly List<int> _parolSticks = new List<int>(5), _lanterns = new List<int>(6), _lanternGlows = new List<int>(6),
            _footFlames = new List<int>(6), _sparks = new List<int>(8);
        private static readonly Vector3[] LanternSeats =
        {
            new Vector3(-2.6f, .4f, -4.2f), new Vector3(1.9f, .2f, -5.3f), new Vector3(-4.6f, .7f, -2.2f),
            new Vector3(3.9f, .5f, -2.9f), new Vector3(-.4f, .1f, -6.2f), new Vector3(5.2f, .3f, .6f),
        };

        private void BuildSean()
        {
            _duskLow = Wall("DuskGround", 0, 1.0f, new Color(.14f, .05f, .03f, .88f));
            _duskGlow = Wall("DuskHorizon", 1.0f, 2.3f, new Color(.96f, .45f, .12f, .62f), emission: .55f);
            _duskHigh = Wall("DuskSky", 2.3f, 11, new Color(.21f, .08f, .05f, .86f), emission: .12f, cap: true);
            var stick = VfxShapes.Prism(4, 1, 1);
            for (int i = 0; i < 5; i++) _parolSticks.Add(Add("ParolStick" + i, stick, new Color(1, .62f, .16f, .95f), .8f));
            _parolFill = Add("ParolFlame", VfxShapes.TwoSided(VfxShapes.Star(5, .42f, 3)), new Color(1, .42f, .08f, .8f), .9f);
            for (int i = 0; i < LanternSeats.Length; i++)
            {
                _lanternGlows.Add(Add("LanternGlow" + i, VfxShapes.TwoSided(VfxShapes.Splat(12, .1f, 20 + i)), new Color(1, .55f, .15f, .35f), .6f));
                _lanterns.Add(Add("PaperLantern" + i, VfxShapes.TwoSided(VfxShapes.StarOutline(5, .45f, 30 + i)), new Color(1, .78f, .3f, .95f), .7f));
            }
            for (int i = 0; i < 6; i++)
                _footFlames.Add(Add("CoilFlame" + i, VfxShapes.Tongue(5, .24f, .15f, .35f, .08f, 240 + i), new Color(1, i % 2 == 0 ? .32f : .58f, .04f, .7f), .6f));
            for (int i = 0; i < 8; i++)
                _sparks.Add(Add("RiseSpark" + i, VfxShapes.TwoSided(VfxShapes.Star(4, .4f, 50 + i)), new Color(1, .7f, .25f, .95f), .9f));
        }

        private void SampleSean(float t)
        {
            float leave = 1 - Ease(Seconds - .45f, Seconds - .05f, t);
            // Dusk falls as the lantern starts to build, not at the first frame: the plant and
            // the shoulder roll happen on the real court.
            float dusk = Ease(.6f, 1.3f, t) * leave;
            // The held breath after the head snaps up: the horizon dims, then flares on the rise.
            float breath = 1 - .45f * Ease(1.8f, 1.95f, t) * (1 - Ease(2.4f, 2.6f, t));
            Tint(_duskLow, dusk); Tint(_duskHigh, dusk);
            Tint(_duskGlow, dusk * breath * (1 + .35f * Ease(2.9f, 3.05f, t)));

            // The parol, between his palms, facing out of his chest.
            Vector3 centre = BothPalms + new Vector3(0, .12f, .16f);
            float gather = Ease(2.4f, 2.7f, t), burst = Ease(2.95f, 3.1f, t);
            centre = Vector3.Lerp(centre, new Vector3(0, 1.0f, .28f), gather);
            float parolSize = .23f * (1 - gather * .35f) * (1 - burst);
            for (int i = 0; i < 5; i++)
            {
                // Stick i joins outer point i to outer point i + 2: the frame of a real parol.
                float a0 = (90 + i * 72) * Mathf.Deg2Rad, a1 = (90 + (i + 2) * 72) * Mathf.Deg2Rad;
                var p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * parolSize;
                var p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * parolSize;
                float laid = Ease(.85f + i * .16f, .98f + i * .16f, t);
                // Each stick arrives from outside and settles into its seat: laid one by one.
                var mid = (p0 + p1) * .5f;
                var from = mid * 3.2f + Vector3.forward * .15f;
                var at = centre + Vector3.Lerp(from, mid, laid);
                var along = (p1 - p0);
                var rotation = Quaternion.FromToRotation(Vector3.up, along.normalized);
                Place(_parolSticks[i], at - rotation * Vector3.up * along.magnitude * .5f,
                    new Vector3(.012f, Mathf.Max(.001f, along.magnitude), .012f), rotation, laid * leave * (1 - burst));
            }
            float fill = Ease(1.7f, 1.95f, t) * (1 - burst);
            Place(_parolFill, centre + Vector3.back * .01f, Vector3.one * parolSize * .95f, Quaternion.Euler(90, 0, 0) * Quaternion.Euler(0, 18, 0),
                fill * leave * (1 + .5f * Flash(t, 2.62f)));

            // Paper-lantern stars drifting up behind him: his home, arriving with the dusk.
            for (int i = 0; i < _lanterns.Count; i++)
            {
                float born = .8f + i * .18f, age = Mathf.Max(0, t - born);
                var seat = LanternSeats[i];
                var at = seat + new Vector3(Mathf.Sin(age * .8f + i) * .15f, age * (.55f + (i % 3) * .12f), 0);
                var face = Quaternion.LookRotation(-new Vector3(at.x, 0, at.z - 3).normalized, Vector3.up) * Quaternion.Euler(90, 0, 0);
                float seen = Ease(born, born + .4f, t) * dusk;
                Place(_lanterns[i], at, Vector3.one * (.22f + (i % 2) * .06f), face * Quaternion.Euler(0, age * 20, 0), seen);
                Place(_lanternGlows[i], at + face * Vector3.down * .02f, Vector3.one * (.34f + (i % 2) * .08f), face, seen * (_reducedEffects ? .7f : .75f + .25f * Mathf.Sin(t * 4 + i)));
            }

            // The coil: flames lick up round his feet while he is low, and gather in on the rise.
            float coil = Ease(2.3f, 2.6f, t) * (1 - Ease(3.05f, 3.3f, t));
            for (int i = 0; i < _footFlames.Count; i++)
            {
                float a = i * Mathf.PI / 3 + t * 1.2f;
                float radius = Mathf.Lerp(.95f, .55f, Ease(2.55f, 3f, t));
                Place(_footFlames[i], new Vector3(Mathf.Cos(a) * radius, .04f, Mathf.Sin(a) * radius),
                    new Vector3(.8f, Mathf.Lerp(.5f, 1.1f, coil), .8f), Quaternion.Euler(-14, -a * Mathf.Rad2Deg, 0), coil * leave);
            }

            // The rise: the lantern breaks into sparks that race up ahead of him.
            for (int i = 0; i < _sparks.Count; i++)
            {
                float age = t - (2.97f + i * .015f);
                bool alive = age >= 0;
                float u = Mathf.Clamp01(age / .42f);
                float angle = i * Mathf.PI * 2 / _sparks.Count;
                var at = new Vector3(0, 1.0f, .28f) + new Vector3(Mathf.Cos(angle) * .35f * u, u * 1.6f + LiftAt(t), Mathf.Sin(angle) * .25f * u);
                Place(_sparks[i], at, Vector3.one * .07f * (1 - u * .5f), Quaternion.Euler(-90, 0, angle * Mathf.Rad2Deg), alive ? (1 - u * u) * leave : 0);
            }
        }
    }
}
