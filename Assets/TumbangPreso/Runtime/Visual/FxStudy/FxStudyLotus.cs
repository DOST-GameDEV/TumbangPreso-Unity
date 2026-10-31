using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// STUDY D, THE ICE LOTUS. Our own effect, on the bench: a cold light gathers in the air, falls, and a flower of ice
    /// spikes erupts in three rings, stands shimmering in a low mist, and shatters, leaving frost that is eaten away.
    ///
    /// The crystals are the game's own sculpted ones, loaded by name through `IceCrystalKit` and never changed here.
    /// `IceBloomDemo` is the temporary in-game version of the same idea; this is built for the bench with what the study
    /// has learned since, and shares no table with it:
    ///   * the charge's two arcs are STROKES ON A SHELL round the orb (`FxStudyKit.Arch`), the streak map running along
    ///     them, not rings of geometry;
    ///   * the chips are the rough dabs of `fleck_sheet`, each its own, in ice tones;
    ///   * the mist is SHEETS, not smoke balls: low shells hugging the floor round the flower, the cloud map drifting
    ///     outward over them and eating them. Built first as clusters of balls, it read as a scatter of solid blue
    ///     balls and was the one thing that spoiled the piece; ground mist has no round edge anywhere;
    ///   * when it breaks, the ice is SEEN breaking: every spike snaps and a sliver of it (the sculpted spike or shard
    ///     again, small and drawn thin) is thrown out on an arc, tumbling, lands, lies a moment and is gone. Before,
    ///     the spikes sank into the floor and only flat chips flew. ⚠️ SLIVERS, NOT LUMPS: the first pieces were
    ///     `ice_stub` and `ice_shard` at their own proportions, half a metre across, and the shatter was a hail of
    ///     ice cubes that hung in the air for a second and a half. Thin, small, fast, and down inside half a second;
    ///   * the frost on the floor is our `frost_fern`, cut by a threshold and eaten by the cloud map, never faded;
    ///   * light is over white where it should bloom (the orb, the flash, the tips), and the camera does the glow.
    ///
    /// 7.6 seconds: charge (0 to 1.55), flash and fall (1.55 to 1.9), frost strikes the floor (1.9), the three rings come
    /// up inner first with overshoot (1.95 to 2.6), stand (to 5.2), shatter outer first (5.2 to 5.8) with its pieces
    /// down by about 6.6, frost goes (to 7.3).
    ///
    /// ⚠️ THE ERUPTION IS HELD UNDER WHITE. Its first build put 22 per cent of the frame at pure white for a sixth of a
    /// second (2.0 to 2.1 s): the landing flash, the radial burst, the frost's flare and every spike's arrival all
    /// peaked together. Each is lower now and the flash is over before the spikes are up, so the flower is seen
    /// arriving. `tools/cutscene_rework/fxclip.py lotus <tag>` prints the share; keep it under 12.
    ///
    /// ⚠️ EVERY SPIKE IS TYPED (`Petals`): where on its ring, how far it leans out, how far from the middle, how long,
    /// how thick, how it is turned, how long after the bloom it starts. Twenty-four, in three rings, no two alike.
    /// </summary>
    public sealed class FxStudyLotus : FxStudyEffect
    {
        private const float BloomAt = 1.95f, ShatterAt = 5.20f, OrbHeight = 1.55f;
        // Public for the floor's users (`FxStudyLotusFrost`, and through it ABSOLUTE ZERO): the floor wears these.
        public static readonly Color Ice = new Color(0.62f, 0.86f, 1f), IceDeep = new Color(0.16f, 0.36f, 0.86f), IceHot = new Color(0.92f, 0.98f, 1f);
        private static readonly Color MistDeep = new Color(0.26f, 0.42f, 0.82f), MistBody = new Color(0.52f, 0.72f, 0.98f), MistPale = new Color(0.86f, 0.95f, 1f);
        private static readonly Color[] Tones = { new Color(0.70f, 0.90f, 1f), Color.white, new Color(0.50f, 0.78f, 1f), new Color(0.84f, 0.95f, 1f), new Color(0.60f, 0.70f, 1f) };

        private readonly struct Petal
        {
            public readonly string Model; public readonly float Around, Lean, Radius, Length, Girth, Roll, Delay;
            public Petal(string model, float around, float lean, float radius, float length, float girth, float roll, float delay)
            { Model = model; Around = around; Lean = lean; Radius = radius; Length = length; Girth = girth; Roll = roll; Delay = delay; }
        }

        private static readonly Petal[] Petals =
        {
            // THE HEART: six, steep, first out of the floor.
            new Petal(IceCrystalKit.Spike,  14f, 14f, 0.20f, 1.95f, 0.74f,  40f, 0.00f),
            new Petal(IceCrystalKit.Spike,  78f, 20f, 0.25f, 1.70f, 0.80f, 150f, 0.02f),
            new Petal(IceCrystalKit.Shard, 136f, 17f, 0.22f, 1.40f, 0.60f, 265f, 0.05f),
            new Petal(IceCrystalKit.Spike, 195f, 22f, 0.26f, 1.85f, 0.72f,  95f, 0.01f),
            new Petal(IceCrystalKit.Spike, 252f, 15f, 0.21f, 1.60f, 0.82f, 320f, 0.04f),
            new Petal(IceCrystalKit.Shard, 312f, 19f, 0.24f, 1.35f, 0.62f, 205f, 0.06f),
            // THE MIDDLE RING: eight, at about forty-five degrees.
            new Petal(IceCrystalKit.Spike,  30f, 42f, 0.52f, 1.80f, 0.86f, 110f, 0.09f),
            new Petal(IceCrystalKit.Shard,  74f, 47f, 0.57f, 1.40f, 0.64f, 230f, 0.12f),
            new Petal(IceCrystalKit.Spike, 118f, 40f, 0.50f, 1.65f, 0.92f, 345f, 0.10f),
            new Petal(IceCrystalKit.Spike, 166f, 46f, 0.55f, 1.90f, 0.84f,  60f, 0.14f),
            new Petal(IceCrystalKit.Shard, 210f, 44f, 0.53f, 1.45f, 0.62f, 180f, 0.11f),
            new Petal(IceCrystalKit.Spike, 254f, 49f, 0.58f, 1.70f, 0.90f, 290f, 0.13f),
            new Petal(IceCrystalKit.Spike, 300f, 41f, 0.51f, 1.95f, 0.82f,  25f, 0.15f),
            new Petal(IceCrystalKit.Shard, 344f, 45f, 0.54f, 1.35f, 0.66f, 135f, 0.16f),
            // THE OUTER RING: ten, lying nearly flat, last.
            new Petal(IceCrystalKit.Spike,   8f, 71f, 0.90f, 1.85f, 0.96f,  50f, 0.18f),
            new Petal(IceCrystalKit.Spike,  46f, 74f, 0.94f, 1.70f, 1.00f, 170f, 0.20f),
            new Petal(IceCrystalKit.Shard,  80f, 69f, 0.88f, 1.50f, 0.68f, 300f, 0.19f),
            new Petal(IceCrystalKit.Spike, 116f, 73f, 0.93f, 1.95f, 0.94f,  90f, 0.22f),
            new Petal(IceCrystalKit.Spike, 152f, 68f, 0.87f, 1.75f, 1.00f, 215f, 0.21f),
            new Petal(IceCrystalKit.Shard, 188f, 75f, 0.96f, 1.45f, 0.70f, 330f, 0.24f),
            new Petal(IceCrystalKit.Spike, 224f, 70f, 0.90f, 1.90f, 0.96f, 125f, 0.23f),
            new Petal(IceCrystalKit.Spike, 262f, 72f, 0.92f, 1.65f, 1.00f, 245f, 0.25f),
            new Petal(IceCrystalKit.Shard, 296f, 69f, 0.89f, 1.50f, 0.70f,  10f, 0.26f),
            new Petal(IceCrystalKit.Spike, 332f, 73f, 0.93f, 1.80f, 0.98f, 355f, 0.27f),
        };

        // One sheet of ground mist: where round the flower it lies (degrees; the camera is at 270), how much of the
        // round it covers, from and to how far out (m), how long after the bloom it starts, how long it lasts, and its
        // own corner of the cloud map.
        private readonly struct Sheet
        {
            public readonly float Around, Span, From, To, Delay, Life, Seed;
            public Sheet(float around, float span, float from, float to, float delay, float life, float seed)
            { Around = around; Span = span; From = from; To = to; Delay = delay; Life = life; Seed = seed; }
        }

        private static readonly Sheet[] Sheets =
        {
            new Sheet(205f, 135f, 0.7f, 3.4f, 0.10f, 3.9f, 0.13f),
            new Sheet(332f, 125f, 0.8f, 3.6f, 0.25f, 3.7f, 0.47f),
            new Sheet( 82f, 150f, 0.6f, 3.1f, 0.40f, 3.6f, 0.71f),
            new Sheet(268f, 115f, 1.0f, 3.9f, 0.55f, 3.4f, 0.29f),
            new Sheet(150f, 105f, 0.9f, 3.5f, 0.70f, 3.2f, 0.88f),
        };

        // The broken ice: one piece off every spike, and a second off every other one.
        private const int Pieces = 36;

        private readonly Transform[] _petal = new Transform[Petals.Length];
        private readonly Renderer[][] _parts = new Renderer[Petals.Length][];
        private readonly float[] _tall = new float[Petals.Length];
        private Transform _heart; private Renderer[] _heartParts;
        private readonly FxPiece _pool, _orb, _orbGlow, _flash, _fern, _fernGlow, _burst;
        private readonly FxPiece[] _arc = new FxPiece[2];
        private readonly Transform _arcs;
        private readonly FxFlecks _chips;
        private readonly bool _flowerOnly;
        private readonly FxPiece[] _mist = new FxPiece[Sheets.Length];
        private readonly FxPiece _mistBurst;
        private readonly Transform[] _piece = new Transform[Pieces];
        private readonly Renderer[][] _pieceParts = new Renderer[Pieces][];
        private readonly float[] _pieceTall = new float[Pieces];

        public override float Seconds => 7.6f;
        public override float FloorCell => 0.75f;
        public override Color FloorColour => new Color(0.050f, 0.058f, 0.095f);
        public override Color FloorLine => new Color(0.085f, 0.10f, 0.16f);
        public override float FloorDim => 0.5f;
        public override float FloorDimAt => 9f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        { eye = new Vector3(0f, 3.5f, -4.5f); focus = new Vector3(0f, 0.55f, 0f); fov = 40f; }

        /// <summary>The bench's clock: the flower starts up at this age, snaps at this one, and its pieces are down by this one.</summary>
        public const float BloomAge = BloomAt, ShatterAge = ShatterAt, PiecesDownAge = 6.65f;

        public FxStudyLotus(Transform parent) : this(parent, false) { }

        /// <summary>The flower alone, for whoever stands it somewhere else (ABSOLUTE ZERO): the spikes, their heart and the
        /// pieces they break into, posed by `PoseFlower`. No orb, frost, mist or chips.</summary>
        public FxStudyLotus(Transform parent, bool flowerOnly) : base("lotus", parent)
        {
            _flowerOnly = flowerOnly;
            if (flowerOnly) { BuildFlower(); return; }
            _pool = AddGlow("pool", 1, IceHot, IceDeep, lying: true);
            _pool.At.localPosition = new Vector3(0f, 0.01f, 0f);
            // The frost on the floor: a fern, and its light.
            _fernGlow = AddGlow("fern-glow", 2, Ice, IceDeep, lying: true);
            _fernGlow.At.localPosition = new Vector3(0f, 0.012f, 0f);
            _fern = DressFern(AddFlat("fern", 3, "frost_fern", lying: true, erode: "erode_cloud"));
            _fern.At.localPosition = new Vector3(0f, 0.016f, 0f);
            _burst = DressBurst(AddFlat("burst", 4, "erode_radial", lying: true, erode: "erode_cloud"));
            _burst.At.localPosition = new Vector3(0f, 0.02f, 0f);

            // The mist: low shells round the flower (`DressMist`).
            for (int i = 0; i < Sheets.Length; i++)
            {
                var m = Sheets[i];
                _mist[i] = DressMist(Add("mist-" + i, MistSheet(m.Around, m.Span, m.From, m.To), FxStudyKit.Ribbon("erode_cloud", "erode_cloud"), 6), m.Seed);
            }
            // And the breath of mist the shatter throws: one shell all the way round, pushed outward.
            _mistBurst = DressMistRing(Add("mist-burst", MistRing(), FxStudyKit.Ribbon("erode_cloud", "erode_cloud"), 7));

            BuildFlower();

            // The charge: the orb, and two strokes that run round it on a shell.
            _orbGlow = AddGlow("orb-glow", 8, Color.white, Ice);
            _orb = Add("orb", FxStudyKit.Ball(), FxStudyKit.Orb(), 9);
            _orb.Set("_Core", Color.white).Set("_Rim", Ice).Set("_Power", 2.4f).Set("_RimAlpha", 0.15f).Set("_Emit", 1.2f);
            _arcs = Group("arcs");
            for (int i = 0; i < 2; i++)
            {
                _arc[i] = Add("arc-" + i, FxStudyKit.Arch(Vector3.zero, Vector3.one * (i == 0 ? 0.62f : 0.80f), -150f, 150f, 26f, i * 90f), FxStudyKit.Ribbon("erode_streak", "erode_cloud"), 10, _arcs);
                _arc[i].Colours(IceDeep, Ice, IceHot, Ice).Set("_Scroll", 0f, 0f).Set("_Cut", 0.34f, 0.44f).Set("_Stops", 0.46f, 0.66f, 0.92f).Set("_Ends", 0.30f, 0.10f, 0.25f)
                       .Set("_Add", 1f).Set("_Emit", 1.0f).Set("_Soft", 0.15f).Set("_Hue", 10f, 0f, i * 0.5f, 1f);
            }
            _flash = AddGlow("flash", 12, Color.white, Ice, mask: "flare_star");
            _flash.Set("_Sprite", 0f).Set("_Emit", 1.5f);

            _chips = AddFlecks("chips", 14, "fleck_sheet");
            _chips.Grid = 4;
            _chips.Piece.Colours(IceDeep, Ice, IceHot, Ice).Set("_Gain", 1.0f).Set("_Stops", 0.38f, 0.62f, 0.96f).Set("_Cut", 0.10f, 0.75f).Set("_Emit", 0.6f);
        }

        // The crystals. A missing model leaves a gap; nothing throws.
        private void BuildFlower()
        {
            for (int i = 0; i < Petals.Length; i++)
            {
                var crystal = IceCrystalKit.Spawn(Petals[i].Model, Root, out float tall, out _);
                if (crystal == null) continue;
                crystal.hideFlags = HideFlags.DontSave;
                _petal[i] = crystal.transform; _parts[i] = IceCrystalKit.Parts(crystal); _tall[i] = Mathf.Max(0.01f, tall);
                crystal.SetActive(false);
            }
            for (int k = 0; k < Pieces; k++)
            {
                var piece = IceCrystalKit.Spawn(k % 3 == 0 ? IceCrystalKit.Shard : IceCrystalKit.Spike, Root, out float tall, out _);
                if (piece == null) continue;
                piece.hideFlags = HideFlags.DontSave;
                _piece[k] = piece.transform; _pieceParts[k] = IceCrystalKit.Parts(piece); _pieceTall[k] = Mathf.Max(0.01f, tall);
                piece.SetActive(false);
            }
            var heart = IceCrystalKit.Spawn(IceCrystalKit.Cluster, Root, out float heartTall, out _);
            if (heart != null) { heart.hideFlags = HideFlags.DontSave; _heart = heart.transform; _heartParts = IceCrystalKit.Parts(heart); _heart.localScale = Vector3.one * (0.8f / Mathf.Max(0.01f, heartTall)); heart.SetActive(false); }
        }

        public override void Sample(float age, Vector3 eye)
        {
            if (_flowerOnly) { PoseFlower(age); return; }
            // ---- the charge: an orb in the air that swells and steadies, two strokes running round it, chips drawn in.
            float charged = FxStudyKit.Run(age, 0.05f, 1.45f);
            float fall = FxStudyKit.EaseIn(FxStudyKit.Run(age, 1.68f, 1.90f));
            var orbAt = new Vector3(0f, Mathf.Lerp(OrbHeight, 0.12f, fall), 0f);
            float orbSize = (0.10f + 0.16f * FxStudyKit.Ease(charged)) * (1f + 0.06f * Mathf.Sin(age * 11f)) * (1f + 0.6f * FxStudyKit.Envelope(age, 1.52f, 0.06f, 1.62f, 0.10f));
            _orb.At.localPosition = orbAt; _orb.At.localScale = Vector3.one * orbSize;
            _orb.Frame(age, age < 0.05f || age > 1.93f ? 0f : 1f);
            _orbGlow.At.localPosition = orbAt; _orbGlow.At.localScale = Vector3.one * orbSize * 6f;
            _orbGlow.Face(eye); _orbGlow.Frame(age, age > 1.93f ? 0f : 0.7f * FxStudyKit.Ease(FxStudyKit.Run(age, 0.05f, 0.4f)));
            _arcs.localPosition = orbAt;
            for (int i = 0; i < _arc.Length; i++)
            {
                // Each on its own tilted ring, turning its own way; the stroke itself runs along the shell as well.
                _arc[i].At.localRotation = Quaternion.Euler(i == 0 ? 28f : -34f, age * (i == 0 ? 250f : -190f), i == 0 ? 12f : 40f);
                _arc[i].At.localScale = Vector3.one * Mathf.Lerp(1.25f, 0.85f, charged);
                _arc[i].Set("_Tile", 1f, 1f, i * 0.4f - age * 1.1f, i * 0.3f);
                _arc[i].Frame(age, FxStudyKit.Envelope(age, 0.15f + i * 0.2f, 0.2f, 1.45f, 0.12f), 0.15f + 0.15f * Mathf.Sin(age * 3f + i));
            }
            _pool.At.localScale = Vector3.one * Mathf.Lerp(1.2f, 2.6f, charged);
            _pool.Frame(age, age > 1.93f ? 0f : 0.35f * FxStudyKit.Ease(FxStudyKit.Run(age, 0.2f, 1.2f)));

            // ---- the flash before it falls, and the white where it lands.
            // The landing's flash is smaller than the charge's and over in a tenth of a second: it marks the strike, and the
            // flower comes up after it and is not hidden behind it.
            float flash = Mathf.Max(FxStudyKit.Envelope(age, 1.52f, 0.04f, 1.60f, 0.12f), 0.55f * FxStudyKit.Envelope(age, 1.88f, 0.03f, 1.93f, 0.08f));
            _flash.At.localPosition = age < 1.8f ? orbAt : new Vector3(0f, 0.3f, 0f);
            _flash.At.localScale = Vector3.one * (age < 1.8f ? 2.6f : 2.8f);
            _flash.Face(eye); _flash.Frame(age, flash);

            // ---- frost strikes the floor a moment before the ice, stays under it, and is eaten after the shatter.
            PoseFern(_fern, age, 5.4f, 15f, 1.90f, FxStudyKit.Run(age, ShatterAt + 0.7f, 7.3f));
            PoseFernGlow(_fernGlow, age, 6.5f, 1.90f, ShatterAt + 0.3f, 0.6f);
            // The radial burst is the shatter's alone. Under the eruption it was pale grey fingers lying across the frost.
            PoseBurst(_burst, age, ShatterAt, 3f, 8f);

            PoseFlower(age);

            // ---- mist. While the flower stands it lies low round it: each sheet is let in by the cloud map, drifts outward
            // (its picture travels along it and it widens a little) and is eaten by the same map, more and more, until it
            // is gone. Nothing fades as a whole.
            for (int i = 0; i < _mist.Length; i++) PoseMist(_mist[i], age, BloomAt + Sheets[i].Delay, Sheets[i].Life, Sheets[i].Seed);
            {
                float start = ShatterAt + 0.22f, push = FxStudyKit.EaseOut(FxStudyKit.Run(age, start, start + 0.9f));
                float wide = Mathf.Lerp(0.45f, 1.45f, push);
                _mistBurst.At.localScale = new Vector3(wide, Mathf.Lerp(0.7f, 1.7f, push), wide);
                _mistBurst.Set("_Tile", 0.9f, 3f, 0.2f - (age - start) * 0.35f, 0.4f);
                _mistBurst.Frame(age, age < start || age > start + 1.5f ? 0f : 0.46f, Mathf.Lerp(0.12f, 1f, Mathf.Pow(FxStudyKit.Run(age, start + 0.15f, start + 1.5f), 1.3f)));
            }

            // ---- chips: drawn in to the orb, thrown out by the bloom, thrown wide by the shatter.
            _chips.Begin(eye);
            for (int i = 0; i < 18; i++)
            {
                float from = 0.15f + 1.2f * FxStudyKit.Hash(i, 1), life = FxStudyKit.Run(age, from, from + 0.42f);
                float turn = FxStudyKit.Hash(i, 2) * Mathf.PI * 2f + age * 2.5f, far = Mathf.Lerp(1.1f, 0.15f, FxStudyKit.EaseIn(life));
                var off = new Vector3(Mathf.Cos(turn), 0.6f * (FxStudyKit.Hash(i, 3) - 0.5f), Mathf.Sin(turn)) * far;
                _chips.Add(new Vector3(0f, OrbHeight, 0f) + off, -off, 0.10f + 0.10f * FxStudyKit.Hash(i, 4), 0.05f + 0.03f * FxStudyKit.Hash(i, 5),
                           age > 1.5f ? 0f : 0.85f * FxStudyKit.Envelope(age, from, 0.05f, from + 0.30f, 0.12f), i * 5 + 1, Tones[i % Tones.Length]);
            }
            Burst(age, BloomAt + 0.02f, 22, 2.2f, 30);
            Burst(age, ShatterAt + 0.28f, 34, 3.4f, 60);
            _chips.End(age);
        }

        /// <summary>The flower at this age of the bench's clock. The bench calls it from `Sample`; ABSOLUTE ZERO drives its own.</summary>
        public void PoseFlower(float age)
        {
            // ---- the flower. Each spike comes up through the floor past its length and back, opening as it comes; stands;
            // then trembles and SNAPS, the outer ring first: it is gone in a tenth of a second and its pieces are in the air.
            for (int i = 0; i < Petals.Length; i++)
            {
                if (_petal[i] == null) continue;
                var p = Petals[i];
                float up = FxStudyKit.Pop(FxStudyKit.Run(age, BloomAt + p.Delay, BloomAt + p.Delay + 0.30f), 0.22f);
                float breakAt = BreakAt(i);
                float broken = FxStudyKit.Run(age, breakAt, breakAt + 0.10f);
                bool shown = age >= BloomAt + p.Delay && broken < 0.999f;
                if (_petal[i].gameObject.activeSelf != shown) _petal[i].gameObject.SetActive(shown);
                if (!shown) continue;
                float lean = p.Lean * (0.70f + 0.30f * up) + 3f * Mathf.Sin(age * 1.7f + i) * 0.3f;
                // The tremble before it breaks, then over and down.
                float tense = FxStudyKit.Run(age, ShatterAt, breakAt);
                lean += 1.4f * tense * Mathf.Sin(age * 64f + i * 1.7f);
                float length = p.Length * up * (1f - 0.55f * FxStudyKit.EaseIn(broken)) / _tall[i];
                float girth = p.Girth * Mathf.Lerp(0.5f, 1f, up) * (p.Length / _tall[i]) * 0.55f;
                Quaternion around = Quaternion.Euler(0f, p.Around, 0f);
                _petal[i].localPosition = around * new Vector3(0f, -0.04f - 0.10f * broken, p.Radius);
                _petal[i].localRotation = around * Quaternion.Euler(lean, 0f, 0f) * Quaternion.Euler(0f, p.Roll, 0f);
                _petal[i].localScale = new Vector3(girth, length, girth);
                // Brighter as it arrives and as it cracks; its glint shimmers because its clock moves.
                float lit = 1f + 0.7f * FxStudyKit.Envelope(age, BloomAt + p.Delay + 0.04f, 0.06f, BloomAt + p.Delay + 0.18f, 0.5f) + 1.0f * tense + 1.2f * broken;
                IceCrystalKit.Dress(_parts[i], age + i * 0.37f, lit);
            }
            if (_heart != null)
            {
                bool shown = age >= BloomAt && age < ShatterAt + 0.45f;
                if (_heart.gameObject.activeSelf != shown) _heart.gameObject.SetActive(shown);
                if (shown)
                {
                    float up = FxStudyKit.Pop(FxStudyKit.Run(age, BloomAt, BloomAt + 0.3f), 0.2f) * (1f - FxStudyKit.EaseIn(FxStudyKit.Run(age, ShatterAt + 0.2f, ShatterAt + 0.45f)));
                    _heart.localPosition = new Vector3(0f, -0.05f, 0f);
                    var one = _heart.localScale; _heart.localScale = new Vector3(Mathf.Abs(one.x), Mathf.Abs(one.x) * Mathf.Max(0.02f, up), Mathf.Abs(one.x));
                    IceCrystalKit.Dress(_heartParts, age, 1f + 0.6f * FxStudyKit.Envelope(age, BloomAt + 0.04f, 0.06f, BloomAt + 0.2f, 0.5f));
                }
            }

            // ---- the pieces. Each leaves its spike where it snapped, lined up with it, and is thrown out and up; it tumbles
            // about its own middle, comes down, slides a hand's width, lies along the floor and is gone.
            for (int k = 0; k < Pieces; k++)
            {
                if (_piece[k] == null) continue;
                int from = k < Petals.Length ? k : (k - Petals.Length) * 2;
                var p = Petals[from];
                float t = age - BreakAt(from) - 0.03f - 0.03f * FxStudyKit.Hash(k, 11);
                Quaternion around = Quaternion.Euler(0f, p.Around, 0f), stood = around * Quaternion.Euler(p.Lean, 0f, 0f);
                // The first piece is the spike's upper half, the second one comes off nearer its foot.
                float along = k < Petals.Length ? 0.50f + 0.30f * FxStudyKit.Hash(k, 12) : 0.18f + 0.20f * FxStudyKit.Hash(k, 12);
                float length = (k < Petals.Length ? 0.36f : 0.26f) + 0.38f * FxStudyKit.Hash(k, 13);
                Vector3 start = around * new Vector3(0f, -0.04f, p.Radius) + stood * Vector3.up * (p.Length * along);
                Vector3 outward = around * Vector3.forward, side = around * Vector3.right;
                Vector3 flat = outward * (1.0f + 2.4f * FxStudyKit.Hash(k, 14)) + side * (1.4f * (FxStudyKit.Hash(k, 15) - 0.5f));
                float rise = 1.0f + 2.4f * FxStudyKit.Hash(k, 16);
                const float Gravity = 15f, Rest = 0.05f;
                float lands = (rise + Mathf.Sqrt(rise * rise + 2f * Gravity * Mathf.Max(0f, start.y - Rest))) / Gravity;
                float gone = FxStudyKit.EaseIn(FxStudyKit.Run(t, lands + 0.12f, lands + 0.50f));
                bool shown = t >= 0f && gone < 0.999f;
                if (_piece[k].gameObject.activeSelf != shown) _piece[k].gameObject.SetActive(shown);
                if (!shown) continue;
                float f = Mathf.Min(t, lands), slid = FxStudyKit.EaseOut(FxStudyKit.Run(t, lands, lands + 0.30f));
                Vector3 middle = start + flat * (f + 0.10f * slid) + Vector3.up * (rise * f - 0.5f * Gravity * f * f);
                middle.y = Mathf.Max(middle.y, Rest);
                var axis = new Vector3(FxStudyKit.Hash(k, 17) - 0.5f, 0.4f * (FxStudyKit.Hash(k, 18) - 0.5f), FxStudyKit.Hash(k, 19) - 0.5f).normalized;
                float spin = (520f + 700f * FxStudyKit.Hash(k, 20)) * (k % 2 == 0 ? 1f : -1f);
                Quaternion turned = Quaternion.AngleAxis(spin * f, axis) * stood * Quaternion.Euler(0f, p.Roll + k * 47f, 0f);
                Quaternion lying = Quaternion.LookRotation(Vector3.up, flat.normalized) * Quaternion.Euler(0f, k * 61f, 0f);
                Quaternion facing = Quaternion.Slerp(turned, lying, FxStudyKit.Ease(FxStudyKit.Run(t, lands - 0.06f, lands + 0.14f)));
                float size = length / _pieceTall[k] * FxStudyKit.Pop(FxStudyKit.Run(t, 0f, 0.07f), 0.15f) * (1f - gone);
                float girth = (k % 3 == 0 ? 0.34f : 0.50f) + 0.22f * FxStudyKit.Hash(k, 21);
                _piece[k].localRotation = facing;
                _piece[k].localScale = new Vector3(size * girth, size, size * girth);
                // Its origin is its foot; it has to turn about its middle or it swings like a bat.
                _piece[k].localPosition = middle - facing * Vector3.up * (length * 0.5f * (1f - gone));
                IceCrystalKit.Dress(_pieceParts[k], age + k * 0.23f, 1f + 0.7f * FxStudyKit.Envelope(t, 0f, 0.03f, 0.10f, 0.35f));
            }
        }

        // ================================================================ THE FLOOR, for whoever wears it
        //
        // The frost, its light, the burst and the mist, dressed and posed exactly as the bench does it and called by the
        // bench itself (above), so the lotus and every user of its floor are one code. ABSOLUTE ZERO wears it through
        // `FxStudyLotusFrost`; the owner, 2026-10-30: "for cheska, her ult can use the floor vfx used in lotus".
        // ⚠️ Moving these out of the constructor and `Sample` changed no value and no order: `fxbench lotus` films the same.

        /// <summary>The frost on the floor (`frost_fern`, eaten by `erode_cloud`). Pale blue-white line art, under the flower
        /// and never over it: it was near white and a good deal brighter than the crystals standing on it. It glows at the
        /// strike and at no other time (`PoseFern` sets its gain and emit).</summary>
        public static FxPiece DressFern(FxPiece fern)
            => fern.Colours(IceDeep, new Color(0.50f, 0.74f, 0.98f), new Color(0.78f, 0.91f, 1f), Ice).Set("_Cut", 0.10f, 0.55f).Set("_Soft", 0.16f).Set("_Burn", 0.6f).Set("_Emit", 0f).Set("_Hue", 12f, 0f, 0.4f, 1f);

        /// <summary>The frost at <paramref name="age"/>: it strikes at <paramref name="at"/>, opens from a fifth of
        /// <paramref name="size"/> metres past its size and back in 0.3 s, flares as it lands, and is eaten by
        /// <paramref name="eaten"/> (0 whole, 1 gone). <paramref name="gain"/> scales its brightness: 1 on the bench, where
        /// it is light on a dark floor; a sunlit road needs more, because there it covers the road.</summary>
        public static void PoseFern(FxPiece fern, float age, float size, float yaw, float at, float eaten, float gain = 1f)
        {
            float struck = FxStudyKit.Pop(FxStudyKit.Run(age, at, at + 0.30f), 0.12f);
            fern.At.localScale = Vector3.one * (size * Mathf.Lerp(0.2f, 1f, struck)); fern.At.localRotation = Quaternion.Euler(0f, yaw, 0f);
            fern.Frame(age, age < at ? 0f : 1f, eaten);
            float strike = FxStudyKit.Envelope(age, at, 0.03f, at + 0.06f, 0.26f);
            fern.Set("_Gain", gain * (0.46f + 0.50f * strike)).Set("_Emit", 0.45f * strike);
        }

        /// <summary>The frost's light: a soft round glow under it, brighter at the strike, from <paramref name="at"/> until
        /// <paramref name="outAt"/> and gone <paramref name="fall"/> seconds after.</summary>
        public static void PoseFernGlow(FxPiece glow, float age, float size, float at, float outAt, float fall, float strength = 1f)
        {
            float strike = FxStudyKit.Envelope(age, at, 0.03f, at + 0.06f, 0.26f);
            glow.At.localScale = Vector3.one * size;
            glow.Frame(age, strength * (0.12f + 0.18f * strike) * FxStudyKit.Envelope(age, at, 0.10f, outAt, fall));
        }

        /// <summary>The radial burst (`erode_radial`): thrown out at the time given, from one size to another (metres
        /// across) in 0.4 s, and torn away inside half a second.</summary>
        public static FxPiece DressBurst(FxPiece burst) => burst.Colours(IceDeep, Ice, IceHot, Ice).Set("_Cut", 0.56f, 0.86f).Set("_Soft", 0.2f).Set("_Emit", 0.25f);

        public static void PoseBurst(FxPiece burst, float age, float at, float from, float to)
        {
            burst.At.localScale = Vector3.one * Mathf.Lerp(from, to, FxStudyKit.EaseOut(FxStudyKit.Run(age, at, at + 0.40f)));
            float shatterBurst = FxStudyKit.Envelope(age, at + 0.02f, 0.03f, at + 0.12f, 0.30f);
            burst.Frame(age, 0.34f * shatterBurst, 0.3f + 0.5f * FxStudyKit.Run(age, at + 0.1f, at + 0.45f));
        }

        /// <summary>One sheet of ground mist: a low shell <paramref name="span"/> degrees round, centred <paramref name="around"/>
        /// degrees, from <paramref name="from"/> to <paramref name="to"/> metres out.</summary>
        public static Mesh MistSheet(float around, float span, float from, float to)
        {
            var profile = new[] { new Vector2(from, 0.05f), new Vector2(Mathf.Lerp(from, to, 0.35f), 0.17f), new Vector2(Mathf.Lerp(from, to, 0.70f), 0.12f), new Vector2(to, 0.04f) };
            return FxStudyKit.Shell(profile, around, span, 12f, 26, 22);
        }

        /// <summary>The mist: the cloud map its picture and the cloud map what eats it. Thin, pale, mostly covering and only a
        /// little light, and never over white: mist does not glow.</summary>
        public static FxPiece DressMist(FxPiece mist, float seed)
            => mist.Colours(MistDeep, MistBody, MistPale, MistBody).Set("_Halo", 0f).Set("_Add", 0.35f).Set("_Emit", 0f).Set("_Gain", 1f)
                   .Set("_Cut", 0.36f, 0.82f).Set("_Stops", 0.52f, 0.72f, 0.98f).Set("_Curve", 1.3f)
                   .Set("_Ends", 0.30f, 0.40f, 0.28f).Set("_Scroll", 0f, 0f).Set("_Soft", 0.30f)
                   .Set("_ErodeTile", 1.3f, 0.9f, seed, seed * 1.7f).Set("_ErodeScroll", 0.10f, 0.03f)
                   .Set("_Hue", 6f, 0f, seed, 1f).Set("_Warp", 0.05f, 1.5f, seed * 6f, 0.8f);

        /// <summary>A sheet's life from <paramref name="start"/>: let in by the cloud map, drifting outward (its picture
        /// travels along it and it widens a little) and eaten by the same map, more and more, until it is gone. Nothing
        /// fades as a whole.</summary>
        public static void PoseMist(FxPiece mist, float age, float start, float lifeSeconds, float seed)
        {
            float life = FxStudyKit.Run(age, start, start + lifeSeconds);
            float wide = 1f + 0.22f * life;
            mist.At.localScale = new Vector3(wide, 1f + 0.5f * life, wide);
            mist.Set("_Tile", 0.8f, 1.4f, seed - (age - start) * 0.10f, seed * 2f);
            float eaten = Mathf.Max(1f - 0.80f * FxStudyKit.EaseOut(FxStudyKit.Run(age, start, start + 0.7f)), Mathf.Lerp(0.20f, 1f, Mathf.Pow(FxStudyKit.Run(age, start + lifeSeconds * 0.45f, start + lifeSeconds), 1.5f)));
            mist.Frame(age, age < start || life >= 1f ? 0f : 0.30f, eaten);
        }

        /// <summary>The breath of mist all the way round (the shatter's): a closed shell along a profile of (radius, height)
        /// in metres, the bench's own when none is given.</summary>
        public static Mesh MistRing(Vector2[] profile = null)
            => FxStudyKit.Shell(profile ?? new[] { new Vector2(0.4f, 0.08f), new Vector2(1.2f, 0.32f), new Vector2(2.2f, 0.24f), new Vector2(3.2f, 0.05f) }, 0f, 360f, 0f, 24, 48);

        public static FxPiece DressMistRing(FxPiece ring)
            => ring.Colours(MistDeep, MistBody, MistPale, MistBody).Set("_Halo", 0f).Set("_Add", 0.40f).Set("_Emit", 0f).Set("_Gain", 1f)
                   .Set("_Cut", 0.30f, 0.78f).Set("_Stops", 0.50f, 0.70f, 0.98f).Set("_Curve", 1.2f)
                   // It closes on itself, so nothing fades toward its sides and both maps go round it a whole number of times.
                   .Set("_Ends", 0.25f, 0.40f, 0f).Set("_Scroll", 0f, 0f).Set("_Soft", 0.30f).Set("_ErodeTile", 1.2f, 3f, 0.3f, 0.6f).Set("_ErodeScroll", 0.25f, 0f)
                   .Set("_Hue", 6f, 0f, 0.5f, 1f);

        // When a spike snaps: the outer ring first, each a hair off the next.
        private static float BreakAt(int i) => ShatterAt + 0.30f - Petals[i].Delay * 0.9f + 0.04f * FxStudyKit.Hash(i, 3);

        // Chips thrown from the flower: up and out on an arc, tumbling end over end is left to their rough shapes.
        private void Burst(float age, float at, int count, float reach, int salt)
        {
            float lived = age - at;
            if (lived <= 0f || lived > 1.3f) return;
            for (int i = 0; i < count; i++)
            {
                float turn = (i + FxStudyKit.Hash(i, salt)) / count * Mathf.PI * 2f, speed = reach * (0.45f + 0.75f * FxStudyKit.Hash(i, salt + 1));
                float rise = 1.6f + 2.4f * FxStudyKit.Hash(i, salt + 2);
                var flat = new Vector3(Mathf.Cos(turn), 0f, Mathf.Sin(turn));
                Vector3 pos = flat * (0.5f + speed * lived * (1f - 0.3f * lived)) + Vector3.up * Mathf.Max(0.03f, 0.3f + rise * lived - 4.2f * lived * lived);
                Vector3 going = flat * speed + Vector3.up * (rise - 8.4f * lived);
                float big = FxStudyKit.Hash(i, salt + 3);
                _chips.Add(pos, going, 0.10f + 0.22f * big * big, 0.06f + 0.08f * big, (0.6f + 0.4f * FxStudyKit.Hash(i, salt + 4)) * FxStudyKit.Envelope(lived, 0f, 0.03f, 0.55f + 0.3f * big, 0.40f),
                           i * 7 + salt, Tones[(i + salt) % Tones.Length]);
            }
        }
    }
}
