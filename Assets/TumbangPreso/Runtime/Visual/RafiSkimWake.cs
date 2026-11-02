using TumbangPreso.Core;
using UnityEngine;
using Knot = TumbangPreso.Visual.FxStudyKit.Knot;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ WHAT A SKIMMED TSINELAS LEAVES ON THE GROUND. SKIM's rule (`Slipper.Skim.cs`): at its first touch of the ground
    /// a loaded throw does not stop, it slides on along the way it was flying, flat, at 5 m/s (`RafiRules.SkimSpeed`),
    /// for up to 2 m (`SkimDistance`), stopped early only by solid world geometry, and then rests for normal retrieval.
    /// The first quick pass drew nothing of that at all: its coat lived only in the hand and died on the throw, so the one
    /// moment the ability changes the game was invisible. So this is the slide, drawn as Ilyas's water:
    ///
    ///   * THE TOUCH: a fan of the wall's splash lobes thrown up round the point it lands, about knee high, an arc running
    ///     out on the ground, fat drops and a puff of the approved cloud as spray;
    ///   * THE SLIDE: a clear wet track laid on the ground BEHIND the shoe and only as far as it has gone, so its length is
    ///     the slide's own (it is a straight line, as the rule's is); a bow wave of splash lobes either side of the shoe and
    ///     two lines of foam on the ground opening behind it in a V, riding with it;
    ///   * THE REST: the bow wave sinks, two drops fall off, and the track and the V are eaten in place by their own noise.
    ///     A trail is history: it never turns and never follows.
    ///
    /// ⚠️ k2 (2026-10-29), AFTER THE FIRST FILM: "the wake is a tiny splash a few pixels wide, and from the victim's view it
    /// is almost invisible". It has to read from 6 m. So the track is 0.6 m wide and stands on the veil's even wet body
    /// (`RafiWaterPaint.Wet`) with the streaks over it; the bow wave is twice as long and stands 0.3 m; a V of foam lines
    /// 1.4 m long trails it; the touch fan reaches about 0.5 m; the drops are bigger. It stays small beside the court: the
    /// slide is 2 m, and nothing is drawn past it.
    ///
    /// ⚠️ A PURE FUNCTION OF WHAT IS HANDED IN: the touch point, the way, the age since the touch, how far it has slid and
    /// when (if yet) it stopped. Nothing is kept from the frame before, so a film can step it outside Play and a peer that
    /// first sees a slide halfway draws the same frame as one that saw it begin (`RafiSkimWater` hands it those from the
    /// slipper's own replicated state). Look only: no collider, no rule, nothing on the wire.
    /// </summary>
    public sealed class RafiSkimWake
    {
        /// <summary>How long the track takes to be eaten once the shoe rests, and how long the whole wake outlives the rest.</summary>
        public const float Dry = 1.0f, Tail = Dry + 0.1f;
        /// <summary>The slide's length by the rule: the track is built this long and opened as far as the shoe has gone.</summary>
        public const float Reach = RafiRules.SkimDistance;
        private const float TrackBack = 0.18f, TrackFront = 0.06f, TrackLength = TrackBack + Reach + TrackFront, TrackWide = 0.60f;

        // ⚠️ FEW AND FAT, as the wall's and the current's: four at the touch, two when it rests. (across, up, along m/s; size m.)
        private static readonly Vector4[] TouchThrow = { new Vector4(-1.1f, 3.0f, 0.9f, 0.16f), new Vector4(1.2f, 3.3f, 0.6f, 0.15f), new Vector4(0.2f, 2.6f, -0.9f, 0.14f), new Vector4(-0.3f, 3.4f, 1.3f, 0.12f) };
        private static readonly Vector4[] RestThrow = { new Vector4(-0.7f, 1.9f, 0.6f, 0.11f), new Vector4(0.8f, 2.1f, 0.4f, 0.10f) };

        private static Mesh _track, _finLeft, _finRight, _vLeft, _vRight, _crown, _lying, _square;

        public readonly Transform Root;
        private readonly Transform _bow;
        private readonly RafiWaterPaint.Part _wet, _trackPart, _ring, _crownPart, _left, _right, _lineLeft, _lineRight;
        private readonly RafiWaterPaint.Part[] _drops = new RafiWaterPaint.Part[6];
        private readonly float _seed;

        /// <summary>Built under `parent`, lying at the touch point (the parent's origin) and running along its +z.</summary>
        public RafiSkimWake(Transform parent, float seed)
        {
            BuildMeshes();
            _seed = seed;
            var go = new GameObject("IlyasSkimWake") { hideFlags = HideFlags.DontSave };
            Root = go.transform; Root.SetParent(parent, false);
            float s = seed;
            _wet = new RafiWaterPaint.Part("wet", Root, _track, RafiWaterPaint.Wet).ErodeTile(new Vector4(TrackLength / 1.2f, 1f, s, 0f));
            _wet.At.localPosition = new Vector3(0f, 0.010f, 0f);
            _trackPart = new RafiWaterPaint.Part("track", Root, _track, RafiWaterPaint.Track)
                .Tile(new Vector4(TrackLength / 1.75f, 1.0f, s, 0f)).ErodeTile(new Vector4(TrackLength / 1.2f, 1f, s, 0f));
            _trackPart.At.localPosition = new Vector3(0f, 0.014f, 0f);
            _ring = new RafiWaterPaint.Part("arc", Root, _lying, RafiWaterPaint.Ring).ErodeTile(new Vector4(1.3f, 1.3f, s * 0.31f, s * 0.17f));
            _ring.At.localPosition = new Vector3(0f, 0.018f, 0f);
            _crownPart = new RafiWaterPaint.Part("crown", Root, _crown, RafiWaterPaint.Crest)
                .Tile(new Vector4(2f, 0.94f, 0.13f + s, 0.02f)).ErodeTile(new Vector4(2f, 1f, s, 0f));
            _bow = new GameObject("bow") { hideFlags = HideFlags.DontSave }.transform;
            _bow.SetParent(Root, false);
            _left = new RafiWaterPaint.Part("bow-left", _bow, _finLeft, RafiWaterPaint.Splash).Tile(new Vector4(2.2f, 0.94f, 0.2f + s, 0.02f)).ErodeTile(new Vector4(2f, 1f, s, 0f));
            _right = new RafiWaterPaint.Part("bow-right", _bow, _finRight, RafiWaterPaint.Splash).Tile(new Vector4(2.2f, 0.94f, 0.61f + s, 0.02f)).ErodeTile(new Vector4(2f, 1f, 0.5f + s, 0f));
            _lineLeft = new RafiWaterPaint.Part("v-left", _bow, _vLeft, RafiWaterPaint.Lip).Tile(new Vector4(2.4f, 0.94f, s, 0.02f)).ErodeTile(new Vector4(2f, 1f, s, 0f));
            _lineRight = new RafiWaterPaint.Part("v-right", _bow, _vRight, RafiWaterPaint.Lip).Tile(new Vector4(2.4f, 0.94f, 0.5f + s, 0.02f)).ErodeTile(new Vector4(2f, 1f, 0.3f + s, 0f));
            _lineLeft.At.localPosition = _lineRight.At.localPosition = new Vector3(0f, 0.02f, 0f);
            for (int i = 0; i < _drops.Length; i++)
                _drops[i] = new RafiWaterPaint.Part("drop-" + i, Root, _square, RafiWaterPaint.Drop).Tile(RafiWaterPaint.DropCell(i % 4));
        }

        private static void BuildMeshes()
        {
            if (_track != null) return;
            // The track: a lying strip along +z, u along it (0 behind the touch, 1 a little past the slide's full length).
            var track = new Knot[6];
            for (int k = 0; k < track.Length; k++) track[k] = new Knot(new Vector3(0f, 0f, -TrackBack + TrackLength * k / (track.Length - 1f)), TrackWide);
            _track = RafiWaterPaint.Keep(FxStudyKit.Sweep(track, Vector3.up, perKnot: 8, across: 4), "IlyasSkimTrack", 0.05f);
            // The bow wave: on each side a blade of splash lobes standing on the ground, from the shoe's toe back and out,
            // rising toward the back (a V that opens behind it, as water thrown off a skimming stone does). Faced -x so the
            // sweep stands its v upward on both sides: the lobes' tips are the blade's top.
            _finLeft = RafiWaterPaint.Keep(Fin(-1f), "IlyasSkimBowLeft");
            _finRight = RafiWaterPaint.Keep(Fin(1f), "IlyasSkimBowRight");
            // The V of foam lines on the ground, from the shoe's sides back and out, 1.4 m.
            _vLeft = RafiWaterPaint.Keep(VLine(-1f), "IlyasSkimVLeft", 0.05f);
            _vRight = RafiWaterPaint.Keep(VLine(1f), "IlyasSkimVRight", 0.05f);
            // The touch fan: the current's splash collar turned upright, a flaring cup of lobes from the ground up and out,
            // about knee high at its fullest.
            _crown = RafiWaterPaint.Keep(FxStudyKit.Shell(new[] { new Vector2(0.12f, 0.00f), new Vector2(0.20f, 0.18f), new Vector2(0.32f, 0.38f), new Vector2(0.42f, 0.52f) }, 0f, 360f, -30f, 10, 28), "IlyasSkimCrown");
            _lying = RafiWaterPaint.Keep(FxStudyKit.Square(true), "IlyasSkimLying", 0f);
            _square = RafiWaterPaint.Keep(FxStudyKit.Square(false), "IlyasSkimDrop", 0f);
        }

        private static Mesh Fin(float side)
        {
            // (across, height of the blade's middle, along; blade height)
            var knots = new[]
            {
                new Knot(new Vector3(side * 0.07f, 0.04f, 0.22f), 0.08f),
                new Knot(new Vector3(side * 0.16f, 0.09f, -0.04f), 0.18f),
                new Knot(new Vector3(side * 0.28f, 0.14f, -0.34f), 0.28f),
                new Knot(new Vector3(side * 0.40f, 0.13f, -0.62f), 0.26f),
                new Knot(new Vector3(side * 0.52f, 0.08f, -0.86f), 0.14f),
            };
            return FxStudyKit.Sweep(knots, Vector3.left, perKnot: 6, across: 4, cup: 0.15f);
        }

        private static Mesh VLine(float side)
        {
            var knots = new Knot[5];
            for (int k = 0; k < knots.Length; k++)
            {
                float t = k / (knots.Length - 1f);
                knots[k] = new Knot(new Vector3(side * Mathf.Lerp(0.12f, 0.72f, t), 0f, Mathf.Lerp(0.10f, -1.30f, t)), Mathf.Lerp(0.16f, 0.10f, t));
            }
            return FxStudyKit.Sweep(knots, Vector3.up, perKnot: 6, across: 3);
        }

        /// <summary>
        /// Poses the wake `age` seconds after the touch. `slid` is how far the shoe is past the touch along the way (m);
        /// `restedAt` is the age it stopped sliding, or below 0 while it still slides. `eye` is where it is seen from.
        /// </summary>
        public void Pose(float age, float slid, float restedAt, Vector3 eye)
        {
            float t = Mathf.Max(0f, age);
            slid = Mathf.Clamp(slid, 0f, Reach);
            bool resting = restedAt >= 0f && t >= restedAt;
            float since = resting ? t - restedAt : 0f;
            float dry = resting ? FxStudyKit.Ease(since / Dry) : 0f;

            // ---- the track: opened from behind the touch to just under the shoe's heel, eaten in place once it rests.
            float head = (TrackBack + slid + 0.02f) / TrackLength;
            _wet.Window(0.0f, head, 0.05f);
            _trackPart.Window(0.0f, head, 0.05f);
            float rise = FxStudyKit.EaseOut(FxStudyKit.Run(t, 0f, 0.08f));
            _wet.Frame(t, 0.80f * rise, Mathf.Max(0.04f, dry));
            _trackPart.Frame(t, 0.95f * rise, Mathf.Max(0.05f, dry));

            // ---- the touch: the fan is thrown up and falls back, the arc runs out, all in about half a second.
            float q = t / 0.55f;
            if (q < 1f)
            {
                float high = 4f * q * (1f - q) * 0.95f + 0.08f;
                _crownPart.At.localScale = new Vector3(1f + 0.8f * q, high, 1f + 0.8f * q);
                _crownPart.Frame(t, 0.97f, FxStudyKit.Run(q, 0.45f, 1f) * 0.95f);
            }
            else _crownPart.Hide();
            float k = t / 0.6f;
            if (k < 1f)
            {
                float size = Mathf.Lerp(0.45f, 1.8f, FxStudyKit.EaseOut(k));
                _ring.At.localScale = new Vector3(size, 1f, size);
                _ring.Frame(t, 0.7f * Mathf.Sin(Mathf.PI * Mathf.Sqrt(k)), 0.25f + 0.70f * k);
            }
            else _ring.Hide();

            // ---- the bow wave and the V: on the shoe while it slides, swelling up in the first instant. When the shoe rests
            // the bow wave sinks and the V lies where it is and dries with the track.
            _bow.localPosition = new Vector3(0f, 0f, slid);
            float stand = FxStudyKit.EaseOut(FxStudyKit.Run(t, 0f, 0.10f)) * (1f - FxStudyKit.Ease(since / 0.25f));
            var bowScale = new Vector3(1f, Mathf.Max(0.02f, stand), 1f + 0.12f * Mathf.Sin(t * 19f + _seed));
            _left.At.localScale = _right.At.localScale = bowScale;
            float bowEaten = resting ? FxStudyKit.Run(since, 0f, 0.28f) : 0.08f;
            _left.Frame(t, stand > 0.02f ? 0.97f : 0f, bowEaten);
            _right.Frame(t, stand > 0.02f ? 0.97f : 0f, bowEaten);
            // The V opens to its full length over the first 0.3 m of slide, so it never reaches behind the touch.
            float open = Mathf.Clamp01((slid + 0.1f) / 1.3f);
            _lineLeft.At.localScale = _lineRight.At.localScale = new Vector3(open, 1f, open);
            _lineLeft.Frame(t, 0.95f * rise, Mathf.Max(0.06f, dry));
            _lineRight.Frame(t, 0.95f * rise, Mathf.Max(0.06f, dry));

            // ---- the drops.
            for (int i = 0; i < TouchThrow.Length; i++) Throw(_drops[i], TouchThrow[i], Vector3.zero, t - 0.02f * i, eye);
            for (int i = 0; i < RestThrow.Length; i++) Throw(_drops[TouchThrow.Length + i], RestThrow[i], new Vector3(0f, 0.05f, slid), resting ? since - 0.03f * i : -1f, eye);
        }

        // A drop `at` seconds after it was thrown from `from`: its arc under its own weight, gone where it meets the ground.
        private void Throw(RafiWaterPaint.Part drop, Vector4 thrown, Vector3 from, float at, Vector3 eye)
        {
            if (at < 0f) { drop.Hide(); return; }
            var v = new Vector3(thrown.x, thrown.y, thrown.z);
            Vector3 here = from + new Vector3(0f, 0.10f, 0f) + v * at + Vector3.down * (4.9f * at * at);
            if (here.y < 0.02f) { drop.Hide(); return; }
            drop.At.localPosition = here;
            float speed = (v + Vector3.down * (9.8f * at)).magnitude;
            drop.At.localScale = new Vector3(thrown.w, thrown.w * Mathf.Lerp(1.2f, 1.9f, Mathf.Clamp01(speed / 4f)), 1f);
            drop.Face(eye);
            drop.Frame(at, Mathf.Clamp01(here.y / 0.12f + 0.4f));
        }

        public void Dispose()
        {
            if (Root == null) return;
            if (Application.isPlaying) Object.Destroy(Root.gameObject); else Object.DestroyImmediate(Root.gameObject);
        }
    }
}
