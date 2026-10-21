using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// STUDY E, THE SCORCH. Our own effect: something strikes the floor and leaves a charred, cracked patch whose cracks
    /// glow and cool from white-yellow to dark red to nothing, with rubble thrown up and left lying, dark smoke lit
    /// orange from underneath, and embers. Our own crack network (`scorch_fissure`), char (`scorch_char`) and timing; the
    /// reference gave only the kind of stages.
    ///
    ///   * THE CRACKS COOL BY A THRESHOLD, NOT BY FADING: the fissure picture is hottest along the strike's line and dim
    ///     at its edges, and the cut through it rises with age, so the thin outer cracks go dark first and the heart of
    ///     the wound is the last thing glowing. Only the hot part is pushed over white for the camera to bloom.
    ///   * THE CHAR is the same kind of picture laid under it, black, covering the floor; it is eaten at the end.
    ///   * THE RUBBLE is real chunks (`FxStudyKit.Lump`, a typed stone profile pushed out of true), each typed: where it
    ///     leaves from, how it is thrown, how big, how it tumbles. They fly on arcs, land, and stay.
    ///   * THE SMOKE is `FxSmoke` in dark greys whose underside tone is a burnt orange: lit from below by the cracks,
    ///     and only while they are hot (the tone cools to soot as they do). ⚠️ MANY SMALL BALLS. Its first film had
    ///     forty-six a third of a metre across with a flat orange underside each, and read as a heap of painted
    ///     bowling balls standing on the wound.
    ///   * THE EMBERS are the rough dabs of `fleck_sheet` in four oranges.
    /// 4.4 seconds: strike (0.12), cracks at their hottest (0.2 to 0.6), cooling (to about 2.6), char eaten (3.4 to 4.2).
    /// </summary>
    public sealed class FxStudyScorch : FxStudyEffect
    {
        private const float StrikeAt = 0.12f;
        private static readonly Color[] Tones = { new Color(1f, 0.62f, 0.14f), new Color(1f, 0.84f, 0.40f), new Color(1f, 0.42f, 0.08f), new Color(1f, 0.94f, 0.72f) };

        // One chunk of rubble: where it leaves the floor, its throw (metres a second), its size, and how fast it tumbles.
        private readonly struct Chunk
        {
            public readonly Vector3 From, Throw; public readonly float Size, Spin, Seed;
            public Chunk(float x, float z, float vx, float vy, float vz, float size, float spin, float seed)
            { From = new Vector3(x, 0.05f, z); Throw = new Vector3(vx, vy, vz); Size = size; Spin = spin; Seed = seed; }
        }

        private static readonly Chunk[] Chunks =
        {
            new Chunk(-0.6f,  0.1f, -1.6f, 4.6f,  0.9f, 0.34f,  260f, 1.3f),
            new Chunk( 0.3f, -0.2f,  1.2f, 5.2f,  1.4f, 0.42f, -210f, 2.9f),
            new Chunk( 0.9f,  0.2f,  2.4f, 3.8f,  0.3f, 0.26f,  340f, 4.4f),
            new Chunk(-0.1f,  0.3f, -0.5f, 5.8f,  2.0f, 0.30f, -300f, 0.7f),
            new Chunk( 1.4f, -0.1f,  3.0f, 3.2f, -0.6f, 0.22f,  420f, 5.6f),
            new Chunk(-1.1f, -0.2f, -2.6f, 3.4f, -0.4f, 0.24f, -380f, 3.8f),
            new Chunk( 0.6f,  0.4f,  0.8f, 4.4f,  2.6f, 0.36f,  190f, 6.2f),
            new Chunk(-0.4f, -0.3f, -1.0f, 3.0f, -1.5f, 0.18f, -460f, 2.1f),
            new Chunk( 0.1f,  0.1f,  0.4f, 6.4f, -0.9f, 0.15f,  520f, 7.7f),
            new Chunk(-0.8f,  0.2f, -2.0f, 4.0f,  1.6f, 0.13f, -340f, 8.3f),
            new Chunk( 1.1f,  0.0f,  1.8f, 4.8f, -1.2f, 0.16f,  280f, 9.1f),
            new Chunk( 0.5f, -0.3f,  0.2f, 3.6f, -2.2f, 0.12f, -600f, 6.9f),
        };

        private readonly FxPiece _char, _cracks, _pool, _flash, _bolt;
        private readonly FxPiece[] _rock = new FxPiece[Chunks.Length];
        private readonly FxSmoke[] _smoke = new FxSmoke[2];
        private readonly FxFlecks _embers;

        public override float Seconds => 4.4f;
        public override float FloorCell => 0.75f;
        public override Color FloorColour => new Color(0.22f, 0.22f, 0.235f);
        public override Color FloorLine => new Color(0.30f, 0.30f, 0.32f);
        public override float FloorDim => 0.6f;
        public override float FloorDimAt => 14f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        { eye = new Vector3(-2.7f, 2.9f, -5.3f); focus = new Vector3(0.1f, 0.70f, 0.1f); fov = 40f; }

        public FxStudyScorch(Transform parent) : base("scorch", parent)
        {
            // The wound lies a little askew to the camera. Its two pictures are twice as wide as high and are laid down
            // so, or the cracks would be squeezed one way.
            var lie = Quaternion.Euler(0f, -18f, 0f);
            _char = AddFlat("char", 1, "scorch_char", lying: true, erode: "erode_cloud");
            var soot = new Color(0.015f, 0.013f, 0.012f);
            _char.Colours(soot, soot, new Color(0.05f, 0.04f, 0.035f), soot).Set("_Cut", 0.12f, 0.40f).Set("_Add", 0f).Set("_Soft", 0.10f).Set("_Emit", 0f);
            _char.At.localPosition = new Vector3(0f, 0.010f, 0f); _char.At.localRotation = lie; _char.At.localScale = new Vector3(6.4f, 1f, 3.2f);
            _pool = AddGlow("pool", 2, new Color(1f, 0.90f, 0.60f), new Color(1f, 0.42f, 0.06f), lying: true);
            _pool.At.localPosition = new Vector3(0f, 0.014f, 0f);
            _cracks = AddFlat("cracks", 3, "scorch_fissure", lying: true, erode: "erode_cloud");
            _cracks.Colours(new Color(0.55f, 0.04f, 0.0f), new Color(1f, 0.36f, 0.02f), new Color(1f, 0.92f, 0.55f), new Color(1f, 0.4f, 0.05f))
                   .Set("_Halo", 0.8f).Set("_Soft", 0.10f).Set("_Hue", 8f, 0f, 0.4f, 1f);
            _cracks.At.localPosition = new Vector3(0f, 0.018f, 0f); _cracks.At.localRotation = lie; _cracks.At.localScale = new Vector3(6.4f, 1f, 3.2f);

            // Rubble: one stone profile, each chunk pushed out of true its own way. Its tones go through the smoke's
            // shader (stepped light, dark underside), which is why its vertex alpha is cleared: there that is "eaten".
            var stone = new[] { new Vector2(0.02f, 0f), new Vector2(0.62f, 0.06f), new Vector2(0.90f, 0.38f), new Vector2(0.78f, 0.74f), new Vector2(0.40f, 0.96f), new Vector2(0.02f, 1.0f) };
            var rockShader = Shader.Find(FxSmoke.ShaderName);
            for (int i = 0; i < Chunks.Length; i++)
            {
                var mesh = FxStudyKit.Lump(stone, Chunks[i].Seed, 0.50f, 10, 0.4f);
                var colours = new Color[mesh.vertexCount];
                for (int k = 0; k < colours.Length; k++) colours[k] = new Color(1f, 1f, 1f, 0f);
                mesh.colors = colours;
                // Rubble is no part of any cluster: it brings no direction to be lit by, only a full dark underside.
                var alone = new System.Collections.Generic.List<Vector4>(mesh.vertexCount);
                for (int k = 0; k < mesh.vertexCount; k++) alone.Add(new Vector4(0f, 0f, 0f, 1f));
                mesh.SetUVs(1, alone);
                Material look = rockShader != null ? new Material(rockShader) { hideFlags = HideFlags.HideAndDontSave } : null;
                if (look != null)
                {
                    look.SetVector("_Rim", new Vector4(2f, 0f, 0f, 0f));     // stone has a clean edge
                    look.SetColor("_Lit", new Color(0.40f, 0.385f, 0.39f)); look.SetColor("_Mid", new Color(0.225f, 0.215f, 0.22f)); look.SetColor("_Dark", new Color(0.085f, 0.075f, 0.075f));
                }
                _rock[i] = Add("rock-" + i, mesh, look, 0);
                if (look != null) look.renderQueue = 2450;
            }

            // Smoke: dark greys, lit orange from underneath.
            var lit = new Color(0.44f, 0.41f, 0.40f); var mid = new Color(0.20f, 0.18f, 0.18f); var under = new Color(0.50f, 0.19f, 0.04f);
            _smoke[0] = AddSmoke("smoke-near", 31, 70, lit, mid, under, oneMass: true);
            _smoke[1] = AddSmoke("smoke-far", 32, 58, lit, mid, under, oneMass: true);

            // The strike: a streak of light down onto the floor, and the white where it lands.
            _bolt = Add("bolt", FxStudyKit.Sweep(new[] { new FxStudyKit.Knot(1.4f, 5.2f, 1.0f, 0.10f), new FxStudyKit.Knot(0.6f, 2.4f, 0.4f, 0.55f), new FxStudyKit.Knot(0f, 0.05f, 0f, 0.20f) }, Vector3.back, perKnot: 12, across: 6),
                        FxStudyKit.Ribbon("erode_streak", "erode_cloud"), 9);
            _bolt.Colours(new Color(1f, 0.40f, 0.05f), new Color(1f, 0.78f, 0.30f), Color.white, new Color(1f, 0.5f, 0.1f))
                 .Set("_Scroll", 0f, 0f).Set("_Cut", 0.20f, 0.50f).Set("_Ends", 0.15f, 0.05f, 0.25f).Set("_Add", 1f).Set("_Emit", 1.6f).Set("_Soft", 0.15f);
            _flash = AddGlow("flash", 10, Color.white, new Color(1f, 0.70f, 0.25f));
            _flash.Set("_Sprite", 0f).Set("_Emit", 1.2f);
            _flash.At.localPosition = new Vector3(0f, 0.3f, 0f);

            _embers = AddFlecks("embers", 12, "fleck_sheet");
            _embers.Grid = 4;
            _embers.Piece.Colours(new Color(0.8f, 0.2f, 0f), new Color(1f, 0.6f, 0.1f), new Color(1f, 0.95f, 0.7f), new Color(1f, 0.5f, 0.1f)).Set("_Stops", 0.38f, 0.62f, 0.96f).Set("_Cut", 0.10f, 0.75f).Set("_Emit", 1.0f);
        }

        public override void Sample(float age, Vector3 eye)
        {
            float since = age - StrikeAt;

            // ---- the strike: the streak runs down its length in a twelfth of a second and is gone.
            _bolt.Open(-0.2f, FxStudyKit.Run(age, 0.02f, StrikeAt) * 1.2f);
            _bolt.Set("_Tile", 1f, 1f, -age * 6f, 0f);
            _bolt.Frame(age, FxStudyKit.Envelope(age, 0.02f, 0.03f, StrikeAt + 0.02f, 0.10f), FxStudyKit.Run(age, StrikeAt, StrikeAt + 0.14f));
            _flash.At.localScale = Vector3.one * Mathf.Lerp(1.2f, 3.4f, FxStudyKit.EaseOut(FxStudyKit.Run(age, StrikeAt, StrikeAt + 0.12f)));
            _flash.Face(eye); _flash.Frame(age, 0.8f * FxStudyKit.Envelope(age, StrikeAt - 0.01f, 0.02f, StrikeAt + 0.05f, 0.14f));

            // ---- the char: burnt outward from the strike, and eaten at the very end.
            _char.Frame(age, since < 0f ? 0f : 1f, Mathf.Max(1f - FxStudyKit.EaseOut(FxStudyKit.Run(age, StrikeAt, StrikeAt + 0.22f)), FxStudyKit.Run(age, 3.40f, 4.25f)));

            // ---- the cracks: white-hot, then cooling by a rising cut, the thin outer ones first.
            float cooled = FxStudyKit.Run(age, StrikeAt + 0.35f, 2.75f);
            float cut = Mathf.Lerp(0.04f, 0.96f, Mathf.Pow(cooled, 0.8f));
            _cracks.Set("_Cut", cut, cut + 0.10f);
            // What is still showing goes from yellow-white down to a dull red as it cools.
            _cracks.Set("_Stops", cut + 0.12f, Mathf.Lerp(0.30f, 1.2f, cooled), Mathf.Lerp(0.70f, 1.6f, cooled));
            _cracks.Set("_Emit", 2.2f * (1f - cooled));
            _cracks.Set("_Gain", Mathf.Lerp(1.5f, 0.8f, cooled) * (1f + 0.06f * Mathf.Sin(age * 23f)));
            // The last of the cracks goes before the char does: in the second film a few dull red marks were still lying on a
            // floor the char had already left.
            _cracks.Frame(age, since < 0f ? 0f : 1f, Mathf.Max(1f - FxStudyKit.EaseOut(FxStudyKit.Run(age, StrikeAt, StrikeAt + 0.16f)), Mathf.Max(0.2f * cooled, FxStudyKit.Run(age, 3.0f, 3.6f))));
            _pool.At.localScale = new Vector3(7.4f, 1f, 4.4f);
            _pool.Frame(age, since < 0f ? 0f : 0.55f * (1f - FxStudyKit.Ease(FxStudyKit.Run(age, StrikeAt + 0.2f, 2.4f))));

            // ---- rubble: thrown, tumbling, landing, lying.
            for (int i = 0; i < _rock.Length; i++)
            {
                var c = Chunks[i];
                float t = since - 0.02f * i;
                if (t < 0f) { _rock[i].Frame(age, 0f); continue; }
                // It comes down where a thrown stone does; after that it has slid a hand's width and stopped.
                float flight = 2f * c.Throw.y / 9.8f, rest = c.Size * 0.42f;
                float f = Mathf.Min(t, flight);
                Vector3 at = c.From + new Vector3(c.Throw.x, 0f, c.Throw.z) * f + Vector3.up * (c.Throw.y * f - 4.9f * f * f);
                float slid = FxStudyKit.EaseOut(FxStudyKit.Run(t, flight, flight + 0.35f));
                at += new Vector3(c.Throw.x, 0f, c.Throw.z) * (0.12f * slid);
                at.y = Mathf.Max(at.y, rest);
                _rock[i].At.localPosition = at;
                _rock[i].At.localRotation = Quaternion.Euler(c.Spin * f * 0.7f + c.Seed * 40f, c.Spin * f + c.Seed * 70f + 25f * slid, c.Seed * 20f);
                _rock[i].At.localScale = Vector3.one * (c.Size * FxStudyKit.Pop(FxStudyKit.Run(t, 0f, 0.10f), 0.1f));
                _rock[i].Frame(age, 1f);
            }

            // ---- smoke off the wound, leaning the way the strike came.
            // Its underside is the cracks' light on it, so it goes out as they do.
            var underlight = Color.Lerp(new Color(0.44f, 0.16f, 0.035f), new Color(0.060f, 0.052f, 0.052f), FxStudyKit.Ease(FxStudyKit.Run(age, StrikeAt + 0.5f, 2.3f)));
            for (int i = 0; i < _smoke.Length; i++) if (_smoke[i] != null) _smoke[i].Piece.Set("_Dark", underlight);
            // Two COLUMNS, each from a narrow foot, rising and leaning its own way. Spread along the wound and rising
            // slowly, as they were first, they were a heap lying on it.
            if (_smoke[0] != null) _smoke[0].Pose(age, StrikeAt + 0.05f, 3.5f, new Vector3(-0.35f, 0.08f, 0.0f), new Vector3(0.34f, 0.06f, 0.24f), new Vector3(-0.42f, 1.05f, 0.12f), 26f, 0.30f);
            if (_smoke[1] != null) _smoke[1].Pose(age, StrikeAt + 0.22f, 3.2f, new Vector3(0.95f, 0.08f, 0.25f), new Vector3(0.28f, 0.06f, 0.22f), new Vector3(0.30f, 0.92f, 0.20f), -22f, 0.26f);

            // ---- embers: thrown by the strike, then lifting off the cracks while they are hot.
            _embers.Begin(eye);
            for (int i = 0; i < 44; i++)
            {
                float turn = (i + FxStudyKit.Hash(i, 1)) / 44f * Mathf.PI * 2f, speed = 1.5f + 3.2f * FxStudyKit.Hash(i, 2), rise = 2.2f + 3.0f * FxStudyKit.Hash(i, 3);
                if (since <= 0f) break;
                var flat = new Vector3(Mathf.Cos(turn), 0f, Mathf.Sin(turn) * 0.6f);
                Vector3 at = flat * (0.3f + speed * since * (1f - 0.25f * since)) + Vector3.up * Mathf.Max(0.03f, 0.2f + rise * since - 4.4f * since * since);
                _embers.Add(at, flat * speed + Vector3.up * (rise - 8.8f * since), 0.10f + 0.20f * FxStudyKit.Hash(i, 4), 0.06f,
                            FxStudyKit.Envelope(since, 0f, 0.02f, 0.45f + 0.5f * FxStudyKit.Hash(i, 5), 0.35f), i * 5 + 2, Tones[i % Tones.Length]);
            }
            for (int i = 0; i < 48; i++)
            {
                float from = StrikeAt + 0.25f + 1.9f * FxStudyKit.Hash(i, 6), lived = age - from;
                if (lived <= 0f || lived > 1.3f) continue;
                var start = Quaternion.Euler(0f, -18f, 0f) * new Vector3((FxStudyKit.Hash(i, 7) - 0.5f) * 4.2f, 0.05f, (FxStudyKit.Hash(i, 8) - 0.5f) * 1.0f);
                Vector3 at = start + new Vector3(0.25f * Mathf.Sin(lived * 5f + i), 0.9f * lived + 0.35f * lived * lived, 0.1f * Mathf.Cos(lived * 4f + i));
                _embers.Add(at, new Vector3(0.2f * Mathf.Cos(lived * 5f + i), 1f, 0f), 0.07f + 0.08f * FxStudyKit.Hash(i, 9), 0.055f,
                            (0.6f + 0.4f * Mathf.Sin(lived * 31f + i)) * FxStudyKit.Envelope(lived, 0f, 0.08f, 0.7f, 0.5f), i * 3 + 1, Tones[(i + 1) % Tones.Length]);
            }
            _embers.End(age);
        }
    }
}
