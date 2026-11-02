using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ AIRBURST'S LIVE PHASE, PAINTED IN HER OWN BRUSH (`AmihanWindKit`), drawn by `AmihanAirburstLook` while
    /// `AmihanAirburstLook.Painted` is on. The owner, of the sample before it: "theres nothing unique and its all just the
    /// same reused effects": its streams were the bench's trail masks and its kick was the wisp's dish, the same pieces as
    /// DRIFT and FEATHERFALL.
    ///
    /// What it has to tell the truth about (`AmihanStorm`): play resumes ON the hit, so everyone in the half court in front
    /// of her and everyone within 10 m round her is already Whirled and thrown outward (15 m/s, 7 m/s lift) along
    /// `AmihanStorm.BlowDirection`. Behind her past 10 m nobody is touched. So it reads as ONE PUSH with a clear origin, peak
    /// and dissipation, from the victim's eyes and from above:
    ///   * ORIGIN, THE BOWL: a dome of shoved air at her feet, seen by its painted rim, blown out to 4 m in a quarter second
    ///     and torn away, with cotton and court dust kicked from under it;
    ///   * PEAK, THE PRESSURE RING: a curling lip of air running out over the court (the shockwave ring of the references,
    ///     but a wave of AIR with a crest, not a flat decal), fast then slowing. In front it runs on to the walls; behind her
    ///     it stops at 10 m, stands there and settles, and a straight wall joins the two along the line out to her sides: the
    ///     edge of what she took, drawn by the wave itself;
    ///   * THE DUST AT ITS FOOT: balls of court dust raised along the front as it passes and rolled outward behind it, so the
    ///     ground is seen to be pushed;
    ///   * LEAVES AND COTTON lifted off the court where the front passes and thrown out along the wind there, tumbling;
    ///   * THE CARRY: two woven ribbons of HISTORY wound round each thrown body's flight, in her cloth's colours (the abel
    ///     threads of `direction.md` section 8.2), so the wind is seen to carry them, growing as they fly and thinning away
    ///     behind them; and gust streams arching out across the area, drawn on outward and eaten from the root.
    ///
    /// ⚠️ ABOUT 1,800 TRIANGLES IN 9 + 2 PER THROWN BODY DRAWS. Every mesh is made once for the process; per frame only
    /// uniforms and the trails' thirty-two points each.
    /// </summary>
    public sealed class AmihanAirburstPainted : FxStudyEffect
    {
        private static float Around => AmihanRules.StormSurgeAroundRadius;
        private static float Carry => AmihanRules.StormSurgeHoldSeconds + 0.5f * AmihanRules.StormSurgeSpeed / Balance.Friction;

        private readonly FxPiece _bowl, _ring, _dust, _thrown, _kick;
        private readonly List<FxPiece> _streams = new List<FxPiece>();
        private readonly List<float> _streamStart = new List<float>(), _streamLength = new List<float>(), _streamLag = new List<float>();
        private readonly List<FxPiece> _carry = new List<FxPiece>();
        private readonly List<Func<float, Vector3>> _bodies = new List<Func<float, Vector3>>();
        private readonly Vector4[] _scratch = new Vector4[32];
        private readonly Vector3 _ground, _forward;

        // ⚠️ THE GUSTS RIDE THE FRONT, THEY DO NOT RADIATE FROM HER. a2 drew nine arches from her feet outward and from above
        // they were the spokes of a wheel, from her own eyes a row of fence posts. Now each gust is a low wavy stroke whose
        // head IS the front (`AmihanWindKit.RingAt`) and whose tail follows it a moment behind, curling over at its head like
        // a breaking wave: the wind seen pushing outward with the ring. Angle (degrees from her facing), where it starts (m),
        // how far it may run (m), how far behind the front its tail is (s), its wave's seed.
        private static readonly float[,] Gusts =
        {
            { -74f, 1.8f, 13f, 0.34f, 0.1f }, { -47f, 2.6f, 16f, 0.42f, 0.7f }, { -22f, 1.6f, 18f, 0.30f, 1.3f }, { -4f, 3.2f, 18f, 0.46f, 2.1f },
            { 15f, 2.0f, 18f, 0.36f, 2.9f }, { 36f, 2.8f, 16f, 0.40f, 3.6f }, { 61f, 1.7f, 14f, 0.32f, 4.4f }, { 83f, 2.4f, 12f, 0.38f, 5.0f },
            { 118f, 1.8f, 8.6f, 0.30f, 5.7f }, { -146f, 2.2f, 8.6f, 0.34f, 6.3f }, { 167f, 1.6f, 8.6f, 0.30f, 7.1f },
        };

        public override float Seconds => 2.8f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        { focus = _ground + _forward * 6f; eye = _ground - _forward * 6f + Vector3.up * 7f; fov = 55f; }
        public override void Sample(float age, Vector3 eye) => Pose(age);

        /// <param name="ground">Her feet on the court.</param><param name="box">The playable box (`AmihanAirburstPicture.CourtBox`).</param>
        /// <param name="bodies">Where each thrown body is at a time since the hand-back (their centres).</param>
        public AmihanAirburstPainted(Transform parent, Vector3 ground, Vector3 forward, Vector4 box, IList<Func<float, Vector3>> bodies)
            : base("airburst-painted", parent)
        {
            forward.y = 0f;
            _forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
            _ground = ground;
            var o = new Vector4(ground.x, ground.y, ground.z, 0f);
            var fwd = new Vector4(_forward.x, 0f, _forward.z, 0f);
            var ring = new Vector4(AmihanWindKit.RingStart, AmihanWindKit.RingSpeed, AmihanWindKit.RingSlowing, Around);

            _bowl = AddShared("bowl", AmihanWindKit.Dome(), AmihanWindKit.Stroke(4, 12), 12);
            _ring = AddShared("ring", AmihanWindKit.Ring(), AmihanWindKit.Stroke(3, 10), 10);
            _ring.At.position = ground;
            _ring.Set("_Fwd", fwd).Set("_Box", box);

            var dust = new AmihanWindKit.Mote[44];
            for (int i = 0; i < dust.Length; i++) dust[i] = i % 4 == 3 ? AmihanWindKit.Mote.Air : AmihanWindKit.Mote.Dust;
            _dust = AddShared("front-dust", AmihanWindKit.MoteCards("airburst-dust", dust, 71), AmihanWindKit.Motes(3, 0.80f), 0);
            _dust.Set("_Origin", o).Set("_Fwd", fwd).Set("_Ring", ring).Set("_Box", box);

            var thrown = new AmihanWindKit.Mote[64];
            for (int i = 0; i < thrown.Length; i++) thrown[i] = i % 4 == 0 ? AmihanWindKit.Mote.Cotton : i % 7 == 3 ? AmihanWindKit.Mote.Feather : AmihanWindKit.Mote.Leaf;
            _thrown = AddShared("thrown", AmihanWindKit.MoteCards("airburst-thrown", thrown, 83), AmihanWindKit.Motes(1, 0.36f), 0);
            _thrown.Set("_Origin", o).Set("_Fwd", fwd).Set("_Ring", ring).Set("_Box", box);

            var kick = new AmihanWindKit.Mote[16];
            for (int i = 0; i < kick.Length; i++) kick[i] = i % 3 == 0 ? AmihanWindKit.Mote.Cotton : AmihanWindKit.Mote.Dust;
            _kick = AddShared("kick", AmihanWindKit.MoteCards("airburst-kick", kick, 97), AmihanWindKit.Motes(2, 0.55f), 0);
            _kick.Set("_Origin", o);

            for (int i = 0; i < Gusts.GetLength(0); i++)
            {
                float angle = Gusts[i, 0], start = Gusts[i, 1], length = Gusts[i, 2];
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * _forward;
                float exit = Exit(new Vector2(ground.x, ground.z), new Vector2(dir.x, dir.z), box) - 0.8f;
                if (Mathf.Abs(angle) > 90f) exit = Mathf.Min(exit, Around - 0.3f);
                length = Mathf.Min(length, exit - start);
                if (length < 2.5f) continue;
                // Made for this cast (its length depends on where she stands): 31 points, once, never touched again.
                var piece = Add("gust-" + i, AmihanWindKit.Gust(length, Gusts[i, 4]), AmihanWindKit.Stroke(2, 20 + i), 20 + i);
                piece.At.SetPositionAndRotation(ground + dir * start + Vector3.up * 0.12f, Quaternion.LookRotation(dir, Vector3.up));
                piece.Set("_Width", 0.44f + 0.08f * (i % 3)).WindowSoft(0.02f);
                _streams.Add(piece); _streamStart.Add(start); _streamLength.Add(length); _streamLag.Add(Gusts[i, 3]);
            }

            if (bodies != null)
                foreach (var body in bodies)
                {
                    if (body == null) continue;
                    _bodies.Add(body);
                    for (int s = 0; s < 2; s++)
                    {
                        var piece = AddShared("carry-" + _carry.Count, AmihanWindKit.TrailStrip(), AmihanWindKit.Stroke(2, 40 + _carry.Count), 40 + _carry.Count);
                        piece.Set("_UseTrail", 1f).Set("_Weave", 1f).Open(-1f, 2f);
                        _carry.Add(piece);
                    }
                }
            foreach (var piece in Pieces()) if (piece.Renderer != null) VfxRenderTag.Attach(piece.Go);
        }

        private IEnumerable<FxPiece> Pieces()
        {
            yield return _bowl; yield return _ring; yield return _dust; yield return _thrown; yield return _kick;
            foreach (var s in _streams) yield return s;
            foreach (var c in _carry) yield return c;
        }

        private static float Exit(Vector2 o, Vector2 d, Vector4 box)
        {
            float t = 1000f;
            if (d.x > 1e-4f) t = Mathf.Min(t, (box.y - o.x) / d.x); else if (d.x < -1e-4f) t = Mathf.Min(t, (box.x - o.x) / d.x);
            if (d.y > 1e-4f) t = Mathf.Min(t, (box.w - o.y) / d.y); else if (d.y < -1e-4f) t = Mathf.Min(t, (box.z - o.y) / d.y);
            return Mathf.Max(0f, t);
        }

        /// <summary>Poses it. `age` is the time since the hand-back (play resuming on the hit).</summary>
        public void Pose(float age)
        {
            float a = Mathf.Max(0f, age);
            bool on = age >= 0f;

            // ORIGIN: the bowl blown out and torn away.
            float blown = FxStudyKit.EaseOut(FxStudyKit.Run(a, 0f, 0.26f));
            float r = Mathf.Lerp(0.8f, 4.2f, blown);
            _bowl.At.SetPositionAndRotation(_ground, Quaternion.LookRotation(_forward, Vector3.up));
            _bowl.At.localScale = new Vector3(r, r * Mathf.Lerp(0.75f, 0.42f, blown), r);
            _bowl.Frame(a, on && a < 0.6f ? 1f : 0f, FxStudyKit.Ease(FxStudyKit.Run(a, 0.10f, 0.55f)));
            _kick.Set("_Puff", a, 0.6f, 3.4f, 0.75f);
            _kick.Frame(a, on && a < 0.75f ? 1f : 0f);

            // PEAK: the ring, fast then slowing; behind her it stands at 10 m and settles.
            float front = AmihanWindKit.RingAt(a);
            float reachedBack = AmihanWindKit.RingWhen(Around);
            float settled = FxStudyKit.Ease(FxStudyKit.Run(a, reachedBack + 0.15f, reachedBack + 0.75f));
            // It rises out of the court over its first 0.15 s (it starts at her feet, under her own eyes), then lowers as it spreads.
            float height = Mathf.Lerp(0.35f, 1f, FxStudyKit.EaseOut(FxStudyKit.Run(a, 0f, 0.15f))) * Mathf.Lerp(1.35f, 0.55f, FxStudyKit.Ease(FxStudyKit.Run(a, 0f, 1.5f)));
            _ring.Set("_Ring", front, Around, height, settled);
            _ring.Frame(a, on && a < 2.0f ? 1f : 0f, FxStudyKit.Ease(FxStudyKit.Run(a, 0.85f, 1.95f)));

            _dust.Frame(a, on && a < 2.2f ? 1f : 0f);
            _thrown.Frame(a, on && a < 2.8f ? 1f : 0f);

            // THE GUSTS: the head is the front, the tail the front a moment ago; at its end each curls over and is eaten.
            for (int i = 0; i < _streams.Count; i++)
            {
                bool behind = Vector3.Dot(_streams[i].At.forward, _forward) < -0.01f;
                float R(float t) => behind ? Mathf.Min(AmihanWindKit.RingAt(t), Around) : AmihanWindKit.RingAt(t);
                float L = _streamLength[i], start = _streamStart[i];
                float head = Mathf.Min(1.02f, AmihanWindKit.GustBody * (R(a) - start) / L);
                // Once the front has passed its end, the curl rolls over in a fifth of a second.
                if (head >= AmihanWindKit.GustBody) head = Mathf.Min(1.02f, AmihanWindKit.GustBody + (1f - AmihanWindKit.GustBody) * FxStudyKit.EaseOut(FxStudyKit.Run(a, AmihanWindKit.RingWhen(start + L), AmihanWindKit.RingWhen(start + L) + 0.22f)) * 1.02f);
                float tail = AmihanWindKit.GustBody * (R(a - _streamLag[i]) - start) / L - 0.04f;
                tail = Mathf.Max(tail, FxStudyKit.Ease(FxStudyKit.Run(a, 0.9f, 1.7f)) * 1.05f);
                _streams[i].Open(tail, head);
                _streams[i].Frame(a, on && head > 0.02f && tail < 1f ? 1f : 0f, 0f);
            }

            // THE CARRY: woven ribbons of each thrown body's flight.
            for (int b = 0; b < _bodies.Count; b++)
                for (int s = 0; s < 2; s++)
                {
                    var piece = _carry[b * 2 + s];
                    AmihanWindKit.WriteTrail(piece.Material, _scratch, _bodies[b], a, 0.60f, 0.55f, 8f, s * Mathf.PI + b, 0.46f, 0f, Carry, Vector3.zero);
                    piece.Frame(a, on && a < Carry + 0.7f ? 1f : 0f);
                }
        }
    }
}
