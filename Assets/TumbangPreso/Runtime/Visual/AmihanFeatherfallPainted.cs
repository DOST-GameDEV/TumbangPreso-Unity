using System;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ FEATHERFALL, PAINTED IN HER OWN BRUSH (`AmihanWindKit`), drawn by `AmihanFeatherfallLook` while
    /// `AmihanFeatherfallLook.Painted` is on. The owner, of the sample before it: "the trail suddenly shortens. the vfx itself
    /// looks pretty low effort". That sample's updraft let go of the court and climbed as the seconds went (its clock), so it
    /// visibly shortened; its tapes and rings were the whirlwind's; it cost about 40,000 triangles (most of them the cloud
    /// puff field's whole mesh) for a thin ribbon.
    ///
    /// The rule it shows (`AmihanRules`): she alone is thrown 2.8 m up in 0.45 s, held for what is left of 5 s while she moves
    /// and throws, and comes down at 3.5 m a second. So, from the references (`ROUNDS.md` beside the drafts):
    ///   * THE UPDRAFT SHE RIDES: an open column from the court to her soles, three painted blades climbing it in a spiral
    ///     (Venti's upcurrent, the paraglider's updraft column). It is always exactly as tall as the gap under her, so it
    ///     grows with her as she rises and shrinks with her as she comes down; when she moves its foot trails where she was
    ///     0.35 s ago, so it leans after her and never jumps. In her last second it thins;
    ///   * FEATHERS AND COTTON riding it, turning round her as they climb (the updraft column's leaves). When the wind lets
    ///     her go every one is let go where it is and falls the way a feather falls, drifting out and rocking: the name of
    ///     the ability, played out;
    ///   * A CUSHION OF AIR under her soles while she is held (Pharah's hover, Echo's glide), lit from above, its swirl
    ///     turning, with a gold thread round its rim that runs out with her 5 s, so her time left reads from under her and
    ///     from across the court. When she lands it squashes flat and is gone into the landing's puff;
    ///   * TWO RIBBONS OF HISTORY wound round her path: where she has been, as wide as she was moving fast, so the take-off
    ///     leaves a spiral from the court that thins away behind her, a move aloft leaves a wake, and a stop leaves nothing;
    ///   * A PUFF RING at the take-off and a wider one at the landing: balls of air and court dust kicked out along the court.
    ///
    /// ⚠️ ABOUT 800 TRIANGLES IN 7 DRAWS, every mesh made once for the process (`AmihanWindKit`), every motion in the shaders.
    /// </summary>
    public sealed class AmihanFeatherfallPainted : FxStudyEffect
    {
        private const float High = AmihanRules.UpdraftHeight, Rise = AmihanRules.UpdraftRiseSeconds, Hold = AmihanRules.UpdraftSeconds;
        /// <summary>How long after the wind lets her go the last feather has settled.</summary>
        public const float FallLife = 2.3f;
        public const float TakeOffLife = 0.55f, LandLife = 0.70f;

        /// <summary>One moment of a flight, as the picture needs it. Times are seconds; a time not yet come is below 0.</summary>
        public struct Moment
        {
            /// <summary>Since she took off.</summary>
            public float Age;
            /// <summary>Her soles over the court (m), and the court under her now.</summary>
            public float Gap; public Vector3 Ground;
            /// <summary>The share of her 5 s still to come while the wind holds her; below 0 once it has let her go.</summary>
            public float Left;
            /// <summary>When (age) the wind let her go, how high she was then and the court under her then.</summary>
            public float ReleaseAge, ReleaseGap; public Vector3 ReleaseGround;
            /// <summary>Since she landed (below 0 before), and where.</summary>
            public float SinceLand; public Vector3 LandGround;
            public Vector3 LaunchGround;
            /// <summary>Where her soles were at an age: the trail's history and the updraft's foot read it.</summary>
            public Func<float, Vector3> Feet;
        }

        private readonly FxPiece _column, _cushion, _motes, _takeOff, _landing;
        private readonly FxPiece[] _trail = new FxPiece[2];
        private readonly Vector4[] _scratch = new Vector4[32];

        public override float Seconds => Rise + Hold + High / AmihanRules.UpdraftDescentSpeed + FallLife;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        { focus = new Vector3(0f, 1.9f, 0f); eye = focus + new Vector3(5.2f, -0.4f, -6.6f); fov = 38f; }
        public override void Sample(float age, Vector3 eye) { }

        public AmihanFeatherfallPainted(Transform parent) : base("featherfall-painted", parent)
        {
            // Everything is placed in the world by the caller or the shader: the root stays where the parent puts it.
            _column = AddShared("updraft", AmihanWindKit.Tube(), AmihanWindKit.Stroke(0, 10), 10);
            _cushion = AddShared("cushion", AmihanWindKit.Cushion(), AmihanWindKit.Stroke(1, 12), 12);
            var riders = new AmihanWindKit.Mote[24];
            for (int i = 0; i < riders.Length; i++) riders[i] = i % 4 == 3 ? AmihanWindKit.Mote.Cotton : AmihanWindKit.Mote.Feather;
            _motes = AddShared("feathers", AmihanWindKit.MoteCards("featherfall", riders, 31), AmihanWindKit.Motes(0, 0.38f), 0);
            var puff = new AmihanWindKit.Mote[14];
            for (int i = 0; i < puff.Length; i++) puff[i] = i % 2 == 0 ? AmihanWindKit.Mote.Air : AmihanWindKit.Mote.Dust;
            _takeOff = AddShared("take-off", AmihanWindKit.MoteCards("featherfall-puff", puff, 57), AmihanWindKit.Motes(2, 0.30f), 0);
            _landing = AddShared("landing", AmihanWindKit.MoteCards("featherfall-puff", puff, 57), AmihanWindKit.Motes(2, 0.34f), 0);
            for (int i = 0; i < _trail.Length; i++)
            {
                _trail[i] = AddShared("trail-" + i, AmihanWindKit.TrailStrip(), AmihanWindKit.Stroke(2, 14 + i), 14 + i);
                _trail[i].Set("_UseTrail", 1f).Open(-1f, 2f);
            }
            foreach (var piece in new[] { _column, _cushion, _motes, _takeOff, _landing, _trail[0], _trail[1] })
                if (piece.Renderer != null) VfxRenderTag.Attach(piece.Go);
        }

        public void Pose(Moment m)
        {
            float a = Mathf.Max(0f, m.Age);
            bool landed = m.SinceLand >= 0f;
            bool held = m.Left >= 0f && !landed;
            float sinceRelease = m.ReleaseAge < 1e3f ? m.Age - m.ReleaseAge : -1f;
            Vector3 feet = m.Feet != null ? m.Feet(m.Age) : m.Ground + Vector3.up * m.Gap;

            // THE UPDRAFT: as tall as the gap; up with the take-off, thinner in her last second, gone over 0.45 s once the
            // wind lets her go (shrinking with her as she comes down, never cut).
            float strength = held ? FxStudyKit.Ease(FxStudyKit.Run(a, 0f, 0.2f)) * Mathf.Lerp(0.55f, 1f, FxStudyKit.Ease(m.Left * Hold)) : 0f;
            float letGo = sinceRelease >= 0f ? 1f - FxStudyKit.Ease(FxStudyKit.Run(sinceRelease, 0f, 0.45f)) : 1f;
            float shown = held ? 1f : 0.55f * letGo * (landed ? 0f : 1f);
            if (!held && sinceRelease >= 0f) strength = Mathf.Max(strength, 0.55f * letGo);
            _column.At.SetPositionAndRotation(m.Ground, Quaternion.identity);
            Vector3 foot = m.Feet != null ? m.Feet(m.Age - 0.35f) - feet : Vector3.zero;
            foot.y = 0f; foot = Vector3.ClampMagnitude(foot, 1.3f);
            _column.Set("_Shape", Mathf.Max(0.05f, m.Gap - 0.02f), 0.78f, 0.40f, Mathf.Max(0.05f, strength)).Set("_Lag", foot.x, 0f, foot.z, 0f);
            _column.Frame(a, m.Gap > 0.05f ? shown * Mathf.Clamp01(m.Gap / 0.25f) : 0f, 0f);

            // THE CUSHION: pops under her soles as she arrives at the top, held, smaller as she comes down, squashed flat and
            // spread on the landing.
            float pop = FxStudyKit.Pop(FxStudyKit.Run(a, 0.18f, 0.48f), 0.22f);
            float squash = landed ? FxStudyKit.Ease(FxStudyKit.Run(m.SinceLand, 0f, 0.16f)) : 0f;
            float size = 0.74f * Mathf.Max(0f, pop) * (held ? 1f : 0.85f) * (1f + 0.5f * squash);
            _cushion.At.SetPositionAndRotation(feet - Vector3.up * 0.02f, Quaternion.Euler(0f, -a * 25f, 0f));
            _cushion.At.localScale = new Vector3(size, size * (1f - 0.8f * squash), size);
            float breath = 0.5f + 0.5f * Mathf.Sin(a * 3.3f);
            _cushion.Set("_Cushion", held ? Mathf.Clamp01(m.Left) : 0f, breath, 0f, 0f);
            _cushion.Frame(a, size > 0.02f && (!landed || m.SinceLand < 0.16f) ? 1f - squash * 0.6f : 0f, 0f);

            // THE FEATHERS AND COTTON: riding the updraft, let go when the wind lets her go.
            _motes.Set("_Origin", m.Ground.x, m.Ground.y, m.Ground.z, m.Gap)
                  .Set("_Release", m.ReleaseAge < 1e3f ? m.ReleaseAge : 1e4f, m.ReleaseGap, 0f, 0f)
                  .Set("_ReleaseOrigin", m.ReleaseGround.x, m.ReleaseGround.y, m.ReleaseGround.z, 0f);
            bool motesLive = m.Age >= 0f && (sinceRelease < 0f || sinceRelease < FallLife + 0.4f);
            _motes.Frame(m.Age, motesLive ? FxStudyKit.Ease(FxStudyKit.Run(m.Age, 0f, 0.25f)) : 0f);

            // THE TRAIL: two ribbons of history wound round her path, laid from the take-off to the landing.
            float until = landed ? m.Age - m.SinceLand : 1e4f;
            for (int i = 0; i < _trail.Length; i++)
            {
                AmihanWindKit.WriteTrail(_trail[i].Material, _scratch, m.Feet ?? (_ => feet), m.Age, 0.95f, 0.38f, 5.5f, i * Mathf.PI,
                    0.42f, 0f, until, Vector3.up * 0.45f);
                _trail[i].Frame(a, m.Age >= 0f && m.Age < until + 1.0f ? 1f : 0f);
            }

            // THE PUFFS: where she left the court, and where she comes back to it.
            _takeOff.Set("_Origin", m.LaunchGround.x, m.LaunchGround.y, m.LaunchGround.z, 0f).Set("_Puff", m.Age, 0.25f, 1.30f, TakeOffLife);
            _takeOff.Frame(m.Age, m.Age >= 0f && m.Age <= TakeOffLife ? 1f : 0f);
            _landing.Set("_Origin", m.LandGround.x, m.LandGround.y, m.LandGround.z, 0f).Set("_Puff", m.SinceLand, 0.35f, 1.70f, LandLife);
            _landing.Frame(m.SinceLand, landed && m.SinceLand <= LandLife ? 1f : 0f);
        }

        /// <summary>True once everything has settled: the landing's puff is over and the last feather is down.</summary>
        public static bool Over(float age, float releaseAge, float sinceLand)
            => sinceLand > LandLife && (releaseAge > 1e3f || age - releaseAge > FallLife + 0.4f);
    }
}
