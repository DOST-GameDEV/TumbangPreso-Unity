using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using Knot = TumbangPreso.Visual.FxStudyKit.Knot;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ BAHA IN THE MATCH, DRAWN AS ILYAS'S WATER (its live look, not its cutscene). Behind `RafiWaterVisual.NewBaha`; the
    /// glassy crest it replaces is still there, one word away. The first quick pass laid a flat blue band over the whole
    /// lane and was "nearly invisible" on the sunlit court; it also drew the lane to its full length even where cover had
    /// already stopped part of the front. This tells the truth of the rule (`RafiWaterField.CastBaha`, `RafiRules`):
    ///
    ///   * THE WARNING (0.8 s, `BahaWarning`): across the caster's line, its full width (`BahaHalfWidth` either side), foam is
    ///     drawn in on the ground, an arc runs inward and the wave rises out of it. Nothing moves yet; nothing hits yet.
    ///   * THE WAVE: nine stretches of one rolling wave, one for each of the rule's nine lanes, each at the rule's own place
    ///     (`min(travel, its lane's end)`, travel being 5 m/s from the end of the warning, `BahaSpeed`). It stands 0.62 m:
    ///     what it nudges is a GROUNDED rival between 0.4 m under and 0.7 m over its line, so it is drawn knee high, the
    ///     height a hop clears. Where a lane is stopped by cover or by the court's edge, that stretch breaks against it,
    ///     throws a crescent of splash, a fat drop and a puff of spray, and falls; the rest run on.
    ///   * THE SHEET: shallow water on the ground behind the wave, the last 5 m of its run, opening only as far as the
    ///     wave has gone in each lane, then running out over the rule's carry tail (`BahaCarryTail`, 0.6 s).
    ///
    /// ⚠️ k2 (2026-10-29), AFTER THE FIRST FILM. k1 built the sheet and the wave face from the streak picture alone, and on
    /// the sunlit road it read as "a carpet of thin blue strands, like scribbles, grass or hair", with no body of water
    /// and no wave. So every broad surface now stands on the wall's and the current's VEIL (`RafiWaterPaint.Veil`/`Wet`:
    /// an even body colour, real cover), the strands are gone, and the only pictures laid over it are broad: big foam
    /// patches drifting on the sheet, and a thick white curling crest of the wall's splash lobes along the wave's top.
    /// The read it is built for: a shallow sheet of moving water with a white curling lip, running across the court.
    ///
    /// ⚠️ MADE OF THE APPROVED WATER (`RafiWaterPaint`), sized to this ability. Shapes are built once (the wave's meshes are
    /// shared by every Baha; the sheet is one mesh made at the cast from that cast's own lane ends); everything that moves
    /// is a transform or a property block. Nothing is rebuilt per frame.
    ///
    /// ⚠️ A PURE FUNCTION OF THE FIELD'S STATE AND AGE, but for the spray puffs, which are events (each lane's puff is let go
    /// once, the first frame its break is seen, so a joiner who arrives after a break sees no puff for it). Look only:
    /// no collider, no rule, nothing on the wire.
    /// </summary>
    public sealed class RafiBahaFlood
    {
        /// <summary>A film or a replay has no main camera: it says where the lens is, and the drops turn to it.</summary>
        public static Vector3? StagedEye;
        /// <summary>How tall the wave stands (m): under the 0.7 m the rule reaches, so a hop is seen to clear it.</summary>
        public const float FrontHigh = 0.62f;
        /// <summary>How much of the run behind the wave is still wet (m).</summary>
        public const float BodyBehind = 5f;
        private const int Lanes = 9;
        // The wave's uneven top: each lane's share of the full height, so the edge is never ruled.
        private static readonly float[] LaneHigh = { 0.88f, 0.97f, 0.93f, 1.00f, 0.91f, 0.98f, 0.94f, 1.00f, 0.86f };
        private static readonly float[] LanePhase = { 0.2f, 1.8f, 3.1f, 0.9f, 4.2f, 2.5f, 5.4f, 3.6f, 1.1f };

        private static Mesh _face, _crest, _foamLine, _lying, _square, _splash;

        public readonly Transform Root;
        private readonly WorldEffectSnapshot.Field _state;
        private readonly float[] _side = new float[Lanes], _wide = new float[Lanes], _end = new float[Lanes];
        private readonly bool[] _sprayed = new bool[Lanes];
        private readonly float _maxEnd;
        private readonly Mesh _bodyMesh;
        private readonly RafiWaterPaint.Part _sheet, _patches, _gatherFoam, _gatherArc;
        private readonly RafiWaterPaint.Part[] _front = new RafiWaterPaint.Part[Lanes], _crestParts = new RafiWaterPaint.Part[Lanes],
            _break = new RafiWaterPaint.Part[Lanes], _drops = new RafiWaterPaint.Part[Lanes];

        /// <summary>
        /// ⚠️ THE SECOND BAHA (`RafiBahaBody`): one coherent body of water matched to the owner's reference, in place of this k2
        /// look. The owner, of k2: "did you actually take a look at what baha looks like? it looks horrible". On; false gives
        /// the k2 look below exactly. Read when a Baha is built. Look only: the rule, the timing and the wire are untouched.
        /// </summary>
        public static bool BahaV2 = true;
        private readonly RafiBahaBody _second;

        public static RafiBahaFlood Build(Transform parent, WorldEffectSnapshot.Field state) => new RafiBahaFlood(parent, state);

        private RafiBahaFlood(Transform parent, WorldEffectSnapshot.Field state)
        {
            _state = state;
            var go = new GameObject("IlyasBahaFlood") { hideFlags = HideFlags.DontSave };
            Root = go.transform; Root.SetParent(parent, false);
            if (BahaV2)
            {
                var ends = new float[Lanes];
                for (int i = 0; i < Lanes; i++)
                    ends[i] = state.Path != null && state.Path.Length == Lanes ? Mathf.Max(0f, Vector3.Dot(state.Path[i] - state.Position, state.Forward)) : state.SecondScale;
                _second = new RafiBahaBody(Root, ends);
                return;
            }
            BuildMeshes();
            float r = state.Radius, s = Mathf.Repeat(state.EventId * 0.37f, 1f);
            for (int i = 0; i < Lanes; i++)
            {
                // ⚠️ THE RULE'S LANES. A point is in lane `round((side / radius + 1) * 4)` and is swept only up to that lane's
                // end, so lane i owns the band 0.375 m either side of its middle, cut at the lane's outer edge.
                float mid = Mathf.Lerp(-r, r, i / (float)(Lanes - 1)), half = r / (Lanes - 1) * 0.5f;
                float from = Mathf.Max(-r, mid - half), to = Mathf.Min(r, mid + half);
                _side[i] = (from + to) * 0.5f; _wide[i] = (to - from) * 1.12f;
                _end[i] = state.Path != null && state.Path.Length == Lanes ? Mathf.Max(0f, Vector3.Dot(state.Path[i] - state.Position, state.Forward)) : state.SecondScale;
                _maxEnd = Mathf.Max(_maxEnd, _end[i]);
            }
            _maxEnd = Mathf.Max(0.5f, _maxEnd);

            _bodyMesh = Body(r);
            _sheet = new RafiWaterPaint.Part("sheet", Root, _bodyMesh, RafiWaterPaint.Wet).ErodeTile(new Vector4(_maxEnd / 2.4f, r * 2f / 2.4f, s, 0f));
            _sheet.At.localPosition = new Vector3(0f, 0.012f, 0f);
            // Big cells: one tile of foam about every 2.5 m along and 2.5 m across, so they read as patches, not threads.
            _patches = new RafiWaterPaint.Part("foam-patches", Root, _bodyMesh, RafiWaterPaint.FoamPatches)
                .Tile(new Vector4(_maxEnd / 2.5f, r * 2f / 2.5f, s, 0.3f)).ErodeTile(new Vector4(_maxEnd / 2.4f, r * 2f / 2.4f, s, 0f));
            _patches.At.localPosition = new Vector3(0f, 0.018f, 0f);
            _gatherFoam = new RafiWaterPaint.Part("gather-foam", Root, _foamLine, RafiWaterPaint.Lip).Tile(new Vector4(3f, 0.94f, s, 0.02f)).ErodeTile(new Vector4(2f, 1f, s, 0f));
            _gatherFoam.At.localPosition = new Vector3(0f, 0.02f, 0.05f);
            _gatherArc = new RafiWaterPaint.Part("gather-arc", Root, _lying, RafiWaterPaint.Ring).ErodeTile(new Vector4(1.3f, 1.3f, s * 0.31f, s * 0.17f));
            _gatherArc.At.localPosition = new Vector3(0f, 0.016f, 0f);
            for (int i = 0; i < Lanes; i++)
            {
                float across = _side[i] / 2.4f;
                _front[i] = new RafiWaterPaint.Part("wave-" + i, Root, _face, RafiWaterPaint.Veil).ErodeTile(new Vector4(0.8f, _wide[i] / 2.4f, s, across));
                // The crest's lobes run on from lane to lane: each lane's picture is slid across by its own place.
                _crestParts[i] = new RafiWaterPaint.Part("crest-" + i, _front[i].At, _crest, RafiWaterPaint.Splash)
                    .Tile(new Vector4(_wide[i] / 1.1f, 0.94f, s + _side[i] / 1.1f, 0.02f)).ErodeTile(new Vector4(2f, 1f, s + i * 0.21f, 0f));
                _break[i] = new RafiWaterPaint.Part("break-" + i, Root, _splash, RafiWaterPaint.Crest)
                    .Tile(new Vector4(2f, 0.94f, 0.13f + s + i * 0.29f, 0.02f)).ErodeTile(new Vector4(2f, 1f, s + i * 0.17f, 0f));
                _drops[i] = new RafiWaterPaint.Part("drop-" + i, Root, _square, RafiWaterPaint.Drop).Tile(RafiWaterPaint.DropCell(i % 4));
            }
        }

        private static void BuildMeshes()
        {
            if (_face != null) return;
            // The wave, one metre wide (each lane scales it to its own width): from the sheet behind, up a rising face to its
            // crown, curling over into a falling lip, as the wall's curtain is swept from its foot up and over its shoulder.
            // u runs foot to lip, v across. Its foot is faint and joins the sheet; it is whole from a third of the way up.
            var knots = new[]
            {
                new Knot(new Vector3(0f, 0.00f, -1.10f), 1f), new Knot(new Vector3(0f, 0.05f, -0.70f), 1f), new Knot(new Vector3(0f, 0.20f, -0.35f), 1f),
                new Knot(new Vector3(0f, 0.44f, -0.08f), 1f), new Knot(new Vector3(0f, 0.60f, 0.10f), 1f), new Knot(new Vector3(0f, 0.58f, 0.26f), 1f),
                new Knot(new Vector3(0f, 0.42f, 0.34f), 1f),
            };
            var face = FxStudyKit.Sweep(knots, new Vector3(0f, -0.91f, 0.42f), perKnot: 6, across: 4,
                tint: u => new Color(1f, 1f, 1f, FxStudyKit.Ease(Mathf.InverseLerp(0f, 0.30f, u))));
            _face = RafiWaterPaint.Keep(face, "IlyasBahaWave");
            // The crest: a thick roll of the wall's splash lobes standing along the wave's top and leaning forward over its
            // face, its root inside the curl and its lobes up and out.
            var crest = new Knot[5];
            for (int k = 0; k < crest.Length; k++) crest[k] = new Knot(new Vector3(Mathf.Lerp(-0.5f, 0.5f, k / 4f), 0.60f + 0.02f * Mathf.Sin(k * 2.7f), 0.20f), 0.40f, -28f);
            _crest = RafiWaterPaint.Keep(FxStudyKit.Sweep(crest, Vector3.back, perKnot: 5, across: 5, cup: 0.25f), "IlyasBahaCrest");
            // The warning's foam: a strip lying along the caster's line, the lane's full width and a little past it.
            var foam = new Knot[7];
            for (int k = 0; k < foam.Length; k++) foam[k] = new Knot(new Vector3(Mathf.Lerp(-RafiRules.BahaHalfWidth - 0.1f, RafiRules.BahaHalfWidth + 0.1f, k / 6f), 0f, 0f), 0.55f);
            _foamLine = RafiWaterPaint.Keep(FxStudyKit.Sweep(foam, Vector3.up, perKnot: 4, across: 4), "IlyasBahaGatherFoam");
            _lying = RafiWaterPaint.Keep(FxStudyKit.Square(true), "IlyasBahaLying", 0f);
            _square = RafiWaterPaint.Keep(FxStudyKit.Square(false), "IlyasBahaDrop", 0f);
            // Where a stretch breaks against cover: the current's splash collar opened into a crescent thrown back toward
            // where the water came from (the cover takes the other half), a lane wide.
            _splash = RafiWaterPaint.Keep(FxStudyKit.Shell(new[] { new Vector2(0.22f, 0.00f), new Vector2(0.30f, 0.22f), new Vector2(0.40f, 0.46f), new Vector2(0.46f, 0.62f) }, -90f, 200f, -25f, 10, 24), "IlyasBahaBreak");
        }

        // The sheet: lying over the lane, each column running only to its own lane's end. u is the distance along the lane
        // as a share of the longest lane, so one open window draws every lane exactly as far as the wave has gone.
        private Mesh Body(float r)
        {
            const int columns = 13;
            int rows = Mathf.Clamp(Mathf.CeilToInt(_maxEnd / 0.5f), 1, 140);
            var vertices = new Vector3[(rows + 1) * columns]; var uvs = new Vector2[vertices.Length]; var colours = new Color[vertices.Length];
            for (int c = 0; c < columns; c++)
            {
                float side = Mathf.Lerp(-r, r, c / (columns - 1f));
                int lane = Mathf.Clamp(Mathf.RoundToInt((side / r + 1f) * 4f), 0, Lanes - 1);
                for (int row = 0; row <= rows; row++)
                {
                    float z = Mathf.Min(row * _maxEnd / rows, _end[lane]);
                    int i = row * columns + c;
                    vertices[i] = new Vector3(side, 0f, z);
                    uvs[i] = new Vector2(z / _maxEnd, c / (columns - 1f));
                    colours[i] = Color.white;
                }
            }
            var triangles = new int[rows * (columns - 1) * 6]; int n = 0;
            for (int row = 0; row < rows; row++)
                for (int c = 0; c < columns - 1; c++)
                {
                    int i = row * columns + c;
                    triangles[n++] = i; triangles[n++] = i + columns; triangles[n++] = i + 1;
                    triangles[n++] = i + 1; triangles[n++] = i + columns; triangles[n++] = i + columns + 1;
                }
            var mesh = new Mesh { name = "IlyasBahaSheet", hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = vertices; mesh.uv = uvs; mesh.colors = colours; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var b = mesh.bounds; b.Expand(0.1f); mesh.bounds = b;
            mesh.UploadMeshData(true);
            return mesh;
        }

        /// <summary>The spray where a stretch breaks: the approved cloud's puffs in the water's tones, a lane's worth.</summary>
        private static CloudPuffField.Puff BreakSpray(Vector3 at, float turn)
        {
            var look = RafiWaterPaint.Day;
            return new CloudPuffField.Puff
            {
                At = at, Scale = 1.15f, Life = 0.42f, Reach = 0.55f, Lift = 0.45f, Turn = turn, Share = 0.85f,
                Lit = look.Foam, Shade = look.Pale, Line = look.Body,
            };
        }

        /// <summary>Poses it `seconds` after the cast. `eye` is where it is seen from.</summary>
        public void Pose(float seconds, Vector3 eye)
        {
            if (_second != null) { _second.Pose(seconds); return; }
            float age = Mathf.Max(0f, seconds), warn = RafiRules.BahaWarning, speed = RafiRules.BahaSpeed;
            float travel = Mathf.Clamp((age - warn) * speed, 0f, Mathf.Max(0f, _state.SecondScale));
            float allStopped = warn + _maxEnd / speed, tail = RafiRules.BahaCarryTail;
            float drained = FxStudyKit.Run(age, allStopped, allStopped + tail);

            // ---- the warning: the foam drawn in to the line, an arc run inward, and it lets go as the wave leaves.
            float drawn = FxStudyKit.Ease(FxStudyKit.Run(age, 0f, warn * 0.7f)), leaving = FxStudyKit.Run(age, warn, warn + 0.35f);
            _gatherFoam.At.localScale = new Vector3(1f, 1f, Mathf.Lerp(2.6f, 1f, drawn) + 0.1f * Mathf.Sin(age * 6f));
            _gatherFoam.Frame(age, 0.95f * (1f - leaving), Mathf.Max(Mathf.Lerp(0.55f, 0.12f, drawn), leaving));
            float k = age / warn;
            if (k < 1f)
            {
                float broad = RafiRules.BahaHalfWidth * 2f + 1.7f, size = Mathf.Lerp(1.25f, 0.72f, FxStudyKit.EaseOut(k));
                _gatherArc.At.localScale = new Vector3(broad * size, 1f, 2.2f * size);
                _gatherArc.Frame(age, 0.55f * Mathf.Sin(Mathf.PI * Mathf.Sqrt(k)), 0.25f + 0.7f * k);
            }
            else _gatherArc.Hide();

            // ---- the sheet: the last stretch of the run behind the wave, under the wave's own foot, running out over the
            // carry tail. Its far end thins over two metres, so it reads as water spreading thin, not a cut rug.
            float tailAt = (travel - BodyBehind) / _maxEnd, headAt = (travel - 0.5f) / _maxEnd, soft = 1.0f / _maxEnd;
            _sheet.Window(tailAt, headAt, soft);
            _patches.Window(tailAt, headAt, soft);
            bool running = travel > 0.05f;
            _sheet.Frame(age, running ? 0.82f : 0f, Mathf.Max(0.04f, drained));
            _patches.Frame(age, running ? 0.90f : 0f, Mathf.Max(0.10f, drained));

            // ---- the wave, lane by lane.
            float rise = FxStudyKit.Ease(FxStudyKit.Run(age, 0.15f, warn));
            for (int i = 0; i < Lanes; i++)
            {
                float stopAt = warn + _end[i] / speed, since = age - stopAt;
                float z = Mathf.Min(travel, _end[i]);
                // Breaking: it is thrown a little on, sinks into itself and is eaten from the top, a third of a second.
                float crash = since > 0f ? FxStudyKit.Run(since, 0f, 0.32f) : 0f;
                float high = Mathf.Max(0.02f, (0.08f + 0.92f * rise) * LaneHigh[i] * (1f + 0.05f * Mathf.Sin(age * 6.5f + LanePhase[i])) * (1f - crash));
                var front = _front[i];
                front.At.localPosition = new Vector3(_side[i], 0f, z + 0.12f * crash);
                front.At.localScale = new Vector3(_wide[i], high, 1f + 0.25f * crash);
                bool standing = crash < 1f;
                front.Frame(age, standing ? 0.88f : 0f, 0.04f + 0.96f * crash);
                _crestParts[i].Frame(age, standing && rise > 0.25f ? 0.97f : 0f, 0.05f + 0.95f * crash);

                // ---- where it broke: the crescent thrown up and back, falling under its own weight, and its spray.
                float q = since / 0.5f;
                var splash = _break[i];
                if (since >= 0f && q < 1f && _end[i] > 0.05f)
                {
                    splash.At.localPosition = new Vector3(_side[i], 0f, _end[i] + 0.1f);
                    splash.At.localScale = new Vector3(_wide[i] / 0.92f, (4f * q * (1f - q) * 0.95f + 0.06f) * (FrontHigh / 0.62f) * 1.2f, 1f + 0.4f * q);
                    splash.Frame(age, 0.95f, FxStudyKit.Run(q, 0.4f, 1f) * 0.95f);
                    if (!_sprayed[i] && since < 0.25f)
                    {
                        _sprayed[i] = true;
                        try { RafiSkimWater.Spray?.Invoke(BreakSpray(Root.TransformPoint(new Vector3(_side[i], 0f, _end[i])), i * 1.3f)); } catch (System.Exception) { }
                    }
                }
                else splash.Hide();

                // ---- and one big drop off it.
                float at = since - 0.03f * (i % 3);
                var drop = _drops[i];
                if (since >= 0f && at >= 0f && _end[i] > 0.05f)
                {
                    var thrown = new Vector3((i - 4) * 0.12f, 2.8f + 0.4f * (i % 2), -1.1f - 0.2f * (i % 3));
                    var here = new Vector3(_side[i], 0.45f, _end[i] + 0.05f) + thrown * at + Vector3.down * (4.9f * at * at);
                    if (here.y > 0.02f)
                    {
                        drop.At.localPosition = here;
                        float size = 0.30f + 0.06f * (i % 3), fast = (thrown + Vector3.down * (9.8f * at)).magnitude;
                        drop.At.localScale = new Vector3(size, size * Mathf.Lerp(1.3f, 2f, Mathf.Clamp01(fast / 5f)), 1f);
                        drop.Face(eye);
                        drop.Frame(at, Mathf.Clamp01(here.y / 0.15f + 0.35f));
                    }
                    else drop.Hide();
                }
                else drop.Hide();
            }
        }

        public void Dispose()
        {
            if (_bodyMesh != null) { if (Application.isPlaying) Object.Destroy(_bodyMesh); else Object.DestroyImmediate(_bodyMesh); }
            if (Root == null) return;
            if (Application.isPlaying) Object.Destroy(Root.gameObject); else Object.DestroyImmediate(Root.gameObject);
        }
    }
}
