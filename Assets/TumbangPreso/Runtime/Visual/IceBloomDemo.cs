using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ TEMPORARY DEMO, ADDED 2026-10-10, TO BE REMOVED. One ICE BLOOM played at a point on the ground, so the
    /// sculpted and baked crystals (<see cref="IceCrystalKit"/>) can be seen in the running game instead of only
    /// turning on the film's dark floor. Owner, 2026-10-10: "the ice looks good, can you try using this in some form
    /// of ability that i can see in game, just temporarily".
    ///
    /// ⚠️ IT IS NOT AN ABILITY AND MUST NOT BECOME ONE BY ACCIDENT. It is a picture: no collider, no rigidbody, no
    /// hazard volume, no score, no stun, no sound, nothing sent to any peer and nothing read from a rule. It is
    /// reached from one debug key (`DebugPlayerSwitcher`, beside F1 to F6) and from nowhere else. A real ice ability
    /// would be built in the hero's kit, on the host, with its own contract; this file is only what it could look
    /// like.
    ///
    /// ⚠️ NOTHING CALLS IT SINCE 2026-10-11. Its key, F12, went to the cloud (`CloudDemo`), and this file is kept as the
    /// record of the first ice pass. To see it again, call `PlayInFrontOf` from that key.
    ///
    /// ⚠️ TO REMOVE IT: delete this file, and the two marked lines that name it (the key in
    /// `DebugPlayerSwitcher.Update` and the words "F12 ice bloom" in `DebugBar.KeysText`). `IceCrystalKit` can stay
    /// or go with it; nothing else calls either.
    ///
    /// WHAT IT PLAYS, from its own clock (the shape is the ice lotus of the reference: a charge, then a flower of
    /// spikes erupting in rings, standing, then shattering):
    ///  * 0 to 0.5 s, the charge: a small cold light over the ground point, a pool under it, a thin ring closing in
    ///    faster and faster, and a dozen chips drawn inward on a curl.
    ///  * 0.5 s, the bloom: twenty-three spikes in three rings, the inner ring steep and first, the middle about
    ///    forty-five degrees, the outer nearly flat and last, each shooting out of the ground past its length and
    ///    settling back, opening outward as it comes like a petal. A cluster and four stubs at the heart. A frost
    ///    disc and a frost fern spreading over the ground, a ring running out, chips thrown up, cold mist round the
    ///    edge, glints on six tips.
    ///  * to 3.4 s it stands, the glints twinkling and every crystal's own clock moving so the shader's glint
    ///    shimmers.
    ///  * 3.4 to 3.8 s, the shatter: outer ring first, each spike lights up, shivers, then topples outward on a
    ///    short hop and shrinks away, with chips flying and the mist bursting.
    ///  * 3.8 to 4.8 s the frost on the ground fades. Then it destroys itself.
    ///
    /// ⚠️ EVERY COUNT, ANGLE AND SIZE IS TYPED IN THE TABLES BELOW. Nothing here asks for a random number, so two
    /// presses give the same flower and a change to one petal is a change to one row.
    ///
    /// ⚠️ ITS CLOCK IS THE GAME'S (scaled) TIME, like every other effect on the court: it holds when the match is
    /// paused and slows with a hit stop, so it never runs on over a frozen game. And the whole look is a function
    /// of its age (<see cref="IVfxTimeline"/>), so a capture can ask for any one moment of it.
    ///
    /// ⚠️ THE HALO CARDS ARE KEPT CLEAR OF THE GROUND. A card turned to the lens whose middle is lower than half
    /// its own height is cut by the floor in a hard straight line (the film's own note on its body halo), so every
    /// standing card here sits at least half its size up, and the light that belongs ON the ground is a card laid
    /// flat.
    /// </summary>
    public sealed class IceBloomDemo : MonoBehaviour, IVfxTimeline
    {
        /// <summary>When the flower erupts, how long one spike takes to come out, when the shatter starts, how long
        /// one spike takes to fall, how long the whole shatter lasts, and how long the frost takes to fade.</summary>
        private const float Charge = 0.5f, Rise = 0.26f, Stand = 3.4f, Fall = 0.30f, Shatter = 0.40f, FrostFade = 1.0f;
        private const float Life = Stand + Shatter + FrostFade + 0.1f;

        private static readonly Color Aqua = new Color(0.05f, 0.50f, 1.00f), FrostBlue = new Color(0.03f, 0.34f, 0.80f),
            Navy = new Color(0.03f, 0.22f, 0.62f), GlintWhite = new Color(0.70f, 0.95f, 1.00f),
            MistBlue = new Color(0.40f, 0.62f, 0.92f), RingPale = new Color(0.66f, 0.92f, 1.00f),
            FernPale = new Color(0.82f, 0.94f, 1.00f);

        /// <summary>
        /// One spike of the flower. <c>Around</c> is where it stands on its ring (degrees), <c>Lean</c> how far it
        /// tips outward from upright, <c>Radius</c> how far from the middle its foot is, <c>Length</c> its length
        /// in metres, <c>Girth</c> its thickness against the model's own, <c>Roll</c> its turn about its own length
        /// (so no two show the same face), <c>Delay</c> how long after the bloom it starts out of the ground and
        /// <c>Break</c> how long after the shatter begins it goes. <c>Glint</c> puts a star on its tip.
        /// </summary>
        private readonly struct Petal
        {
            public readonly string Model;
            public readonly float Around, Lean, Radius, Length, Girth, Roll, Delay, Break;
            public readonly bool Glint;

            public Petal(string model, float around, float lean, float radius, float length, float girth, float roll,
                         float delay, float breakAfter, bool glint = false)
            {
                Model = model; Around = around; Lean = lean; Radius = radius; Length = length; Girth = girth;
                Roll = roll; Delay = delay; Break = breakAfter; Glint = glint;
            }
        }

        private static readonly Petal[] Petals =
        {
            // INNER RING: six, steep. First out of the ground, last to fall.
            new Petal(IceCrystalKit.Spike,   8f, 17f, 0.22f, 1.85f, 0.72f,  20f, 0.000f, 0.090f, true),
            new Petal(IceCrystalKit.Spike,  66f, 22f, 0.26f, 1.60f, 0.78f, 140f, 0.017f, 0.105f),
            new Petal(IceCrystalKit.Shard, 127f, 19f, 0.24f, 1.30f, 0.58f, 250f, 0.033f, 0.080f),
            new Petal(IceCrystalKit.Spike, 184f, 24f, 0.27f, 1.75f, 0.70f,  75f, 0.008f, 0.110f, true),
            new Petal(IceCrystalKit.Spike, 247f, 16f, 0.21f, 1.55f, 0.80f, 310f, 0.025f, 0.095f),
            new Petal(IceCrystalKit.Shard, 304f, 21f, 0.25f, 1.25f, 0.60f, 190f, 0.042f, 0.075f),

            // MIDDLE RING: eight, about forty-five degrees. Four frames behind the inner ring.
            new Petal(IceCrystalKit.Spike,  22f, 44f, 0.52f, 1.75f, 0.85f, 100f, 0.067f, 0.045f),
            new Petal(IceCrystalKit.Shard,  68f, 48f, 0.56f, 1.35f, 0.62f, 215f, 0.083f, 0.060f),
            new Petal(IceCrystalKit.Spike, 111f, 42f, 0.50f, 1.60f, 0.90f, 330f, 0.075f, 0.035f, true),
            new Petal(IceCrystalKit.Spike, 158f, 47f, 0.55f, 1.80f, 0.82f,  55f, 0.100f, 0.065f),
            new Petal(IceCrystalKit.Shard, 203f, 45f, 0.53f, 1.40f, 0.60f, 170f, 0.070f, 0.050f),
            new Petal(IceCrystalKit.Spike, 246f, 50f, 0.58f, 1.65f, 0.88f, 280f, 0.092f, 0.040f),
            new Petal(IceCrystalKit.Spike, 291f, 43f, 0.51f, 1.85f, 0.80f,  15f, 0.108f, 0.070f, true),
            new Petal(IceCrystalKit.Shard, 337f, 46f, 0.54f, 1.30f, 0.64f, 125f, 0.117f, 0.055f),

            // OUTER RING: nine, nearly flat. Last out, first to fall. Their tips are the flower's five metres.
            new Petal(IceCrystalKit.Spike,   3f, 72f, 0.90f, 1.80f, 0.95f,  40f, 0.133f, 0.000f),
            new Petal(IceCrystalKit.Spike,  44f, 75f, 0.95f, 1.65f, 1.00f, 160f, 0.150f, 0.015f, true),
            new Petal(IceCrystalKit.Shard,  82f, 70f, 0.88f, 1.45f, 0.66f, 290f, 0.142f, 0.030f),
            new Petal(IceCrystalKit.Spike, 123f, 74f, 0.93f, 1.90f, 0.92f,  85f, 0.167f, 0.005f),
            new Petal(IceCrystalKit.Spike, 162f, 69f, 0.87f, 1.70f, 1.00f, 200f, 0.158f, 0.020f),
            new Petal(IceCrystalKit.Shard, 201f, 76f, 0.96f, 1.40f, 0.68f, 320f, 0.183f, 0.035f),
            new Petal(IceCrystalKit.Spike, 242f, 71f, 0.90f, 1.85f, 0.94f, 115f, 0.175f, 0.010f, true),
            new Petal(IceCrystalKit.Spike, 283f, 73f, 0.92f, 1.60f, 1.00f, 235f, 0.192f, 0.025f),
            new Petal(IceCrystalKit.Spike, 322f, 70f, 0.89f, 1.75f, 0.96f, 350f, 0.200f, 0.040f),
        };

        /// <summary>A piece of the heart of the flower: where it sits, how tall it is in metres, how it is turned
        /// and tipped, and how long after the bloom it comes up.</summary>
        private readonly struct Lump
        {
            public readonly string Model;
            public readonly float X, Z, Height, Yaw, TiltX, TiltZ, Delay;

            public Lump(string model, float x, float z, float height, float yaw, float tiltX, float tiltZ, float delay)
            {
                Model = model; X = x; Z = z; Height = height; Yaw = yaw; TiltX = tiltX; TiltZ = tiltZ; Delay = delay;
            }
        }

        private static readonly Lump[] Heart =
        {
            new Lump(IceCrystalKit.Cluster,  0.00f,  0.00f, 0.95f,  35f,   0f,   0f, 0.000f),
            new Lump(IceCrystalKit.Stub,     0.40f,  0.12f, 0.46f, 110f,  14f,  -9f, 0.030f),
            new Lump(IceCrystalKit.Stub,    -0.30f,  0.33f, 0.38f, 250f, -11f,  16f, 0.050f),
            new Lump(IceCrystalKit.Stub,    -0.14f, -0.44f, 0.52f,  20f,  18f,   7f, 0.040f),
            new Lump(IceCrystalKit.Stub,     0.36f, -0.34f, 0.34f, 300f,  -8f, -15f, 0.060f),
        };

        /// <summary>
        /// A chip, used three times: drawn IN during the charge (from <c>Far</c> metres out and <c>High</c> up),
        /// thrown out of the middle at the bloom (<c>Speed</c> outward, <c>Loft</c> upward, metres a second), and
        /// thrown off the rings at the shatter (starting <c>From</c> metres out). <c>Size</c> is its height in
        /// metres and <c>Spin</c> its tumble in degrees a second.
        /// </summary>
        private readonly struct Chip
        {
            public readonly string Model;
            public readonly float Bearing, Far, High, Size, Speed, Loft, Spin, From;

            public Chip(string model, float bearing, float far, float high, float size, float speed, float loft,
                        float spin, float from)
            {
                Model = model; Bearing = bearing; Far = far; High = high; Size = size; Speed = speed; Loft = loft;
                Spin = spin; From = from;
            }
        }

        private static readonly Chip[] Chips =
        {
            new Chip(IceCrystalKit.Stub,   15f, 1.9f, 0.55f, 0.16f, 3.6f, 3.4f,  420f, 1.4f),
            new Chip(IceCrystalKit.Shard,  47f, 2.2f, 0.90f, 0.20f, 4.4f, 2.6f, -310f, 0.7f),
            new Chip(IceCrystalKit.Stub,   78f, 1.6f, 0.30f, 0.12f, 2.9f, 4.2f,  520f, 1.9f),
            new Chip(IceCrystalKit.Stub,  112f, 2.0f, 0.75f, 0.18f, 4.8f, 3.0f, -380f, 1.1f),
            new Chip(IceCrystalKit.Shard, 139f, 1.7f, 0.45f, 0.14f, 3.2f, 3.8f,  290f, 0.5f),
            new Chip(IceCrystalKit.Stub,  171f, 2.3f, 1.00f, 0.22f, 4.1f, 2.4f, -450f, 1.7f),
            new Chip(IceCrystalKit.Stub,  204f, 1.8f, 0.35f, 0.13f, 3.8f, 4.4f,  360f, 0.9f),
            new Chip(IceCrystalKit.Shard, 233f, 2.1f, 0.65f, 0.19f, 4.6f, 2.8f, -270f, 1.5f),
            new Chip(IceCrystalKit.Stub,  262f, 1.5f, 0.50f, 0.11f, 2.7f, 3.6f,  480f, 0.6f),
            new Chip(IceCrystalKit.Stub,  291f, 2.2f, 0.85f, 0.17f, 4.3f, 3.2f, -340f, 2.0f),
            new Chip(IceCrystalKit.Shard, 318f, 1.9f, 0.40f, 0.15f, 3.4f, 4.0f,  400f, 1.2f),
            new Chip(IceCrystalKit.Stub,  346f, 2.0f, 0.70f, 0.21f, 4.9f, 2.5f, -500f, 0.8f),
        };

        /// <summary>A puff of cold mist at the flower's edge: where it starts, how big it is, how fast it creeps
        /// outward and upward, and where in its slow breathing it begins.</summary>
        private readonly struct Puff
        {
            public readonly float Bearing, Radius, Size, Drift, Climb, Phase;

            public Puff(float bearing, float radius, float size, float drift, float climb, float phase)
            {
                Bearing = bearing; Radius = radius; Size = size; Drift = drift; Climb = climb; Phase = phase;
            }
        }

        private static readonly Puff[] Puffs =
        {
            new Puff( 10f, 2.2f, 1.5f, 0.12f, 0.05f, 0.0f),
            new Puff( 58f, 2.6f, 1.2f, 0.09f, 0.03f, 1.3f),
            new Puff(101f, 2.1f, 1.6f, 0.14f, 0.06f, 2.6f),
            new Puff(149f, 2.7f, 1.3f, 0.10f, 0.04f, 0.7f),
            new Puff(196f, 2.3f, 1.5f, 0.13f, 0.05f, 3.4f),
            new Puff(241f, 2.5f, 1.1f, 0.08f, 0.03f, 1.9f),
            new Puff(287f, 2.2f, 1.7f, 0.15f, 0.06f, 4.4f),
            new Puff(331f, 2.6f, 1.3f, 0.11f, 0.04f, 5.2f),
        };

        private float _elapsed;

        private Transform[] _petal;
        private Renderer[][] _petalParts;
        private float[] _petalSize, _petalTall;

        private Transform[] _lump;
        private Renderer[][] _lumpParts;
        private float[] _lumpSize;

        private Transform[] _chip;
        private Renderer[][] _chipParts;
        private float[] _chipSize;

        private Renderer[] _puff;

        // The glints, one for each petal marked for one: the petal it rides, a round light at the tip, and the two
        // thin cards crossed into a four-point star.
        private int[] _glintPetal;
        private Renderer[] _glintTip, _glintAcross, _glintUp;
        private Transform[] _glintStar;

        private Renderer _spark, _pool, _frost, _core, _ring, _fern;

        private static IceBloomDemo _live;

        public float LifeSeconds => Life;

        /// <summary>
        /// Plays one bloom in front of a body: <paramref name="metres"/> along the way it faces, on the floor there
        /// (the court's own ground query, falling back to the height the body stands at where there is none).
        /// </summary>
        public static IceBloomDemo PlayInFrontOf(Transform body, float metres = 4f)
        {
            if (body == null) return null;
            Vector3 ahead = body.forward;
            ahead.y = 0f;
            ahead = ahead.sqrMagnitude > 0.0001f ? ahead.normalized : Vector3.forward;
            Vector3 at = body.position + ahead * metres;
            at.y = VfxShapes.GroundAt(at, body.position.y, 2.0f);
            return Play(at, body.eulerAngles.y);
        }

        /// <summary>
        /// Plays one bloom standing on <paramref name="ground"/>. One at a time: a second press while one is still
        /// up replaces it, so leaning on the key cannot stack flowers until the frame rate goes.
        /// </summary>
        public static IceBloomDemo Play(Vector3 ground, float yaw)
        {
            if (_live != null) Destroy(_live.gameObject);
            var go = new GameObject("IceBloomDemo");
            go.transform.SetPositionAndRotation(ground, Quaternion.Euler(0f, yaw, 0f));
            VfxRenderTag.Attach(go);
            _live = go.AddComponent<IceBloomDemo>();
            _live.Build();
            _live.StepTo(0f);
            return _live;
        }

        private void Update()
        {
            StepTo(_elapsed + Time.deltaTime);
            if (_elapsed >= Life) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_live == this) _live = null;
        }

        // ---------------------------------------------------------------------------------------------------
        // Building. Every piece is made once, here, and only moved afterwards. A crystal or a card that could
        // not be made is simply absent (the kit has said why, once); everything below steps round a null.
        // ---------------------------------------------------------------------------------------------------

        private void Build()
        {
            var root = transform;

            _pool = IceCrystalKit.Halo(root, "pool", true);
            _frost = IceCrystalKit.Halo(root, "frost", true);
            if (_pool != null) _pool.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            if (_frost != null) _frost.transform.localPosition = new Vector3(0f, 0.030f, 0f);
            _spark = IceCrystalKit.Halo(root, "spark");
            _core = IceCrystalKit.Halo(root, "core");

            // The ring is the stock annulus with walls, thin; the fern is the stock branching crack, which is built
            // from its middle outward so that growing it by scale reads as frost spreading. Both are see-through
            // marks on the road and both own their mesh and material, so they are freed with this object.
            var ring = VfxShapes.Lay(root, "ChargeRing", VfxShapes.Collar(40, 0.03f, 0.93f), 1f, 0.020f);
            _ring = ring.GetComponent<Renderer>();
            VfxMaterial.Ghost(_ring, new Color(RingPale.r, RingPale.g, RingPale.b, 0f), 0.80f);
            var fern = VfxShapes.Lay(root, "FrostFern", VfxShapes.Fracture(6, 4, 0.018f, 17), 1f, 0.025f, 15f);
            _fern = fern.GetComponent<Renderer>();
            VfxMaterial.Ghost(_fern, new Color(FernPale.r, FernPale.g, FernPale.b, 0f), 0.30f);
            foreach (var mark in new[] { _ring, _fern })
            {
                mark.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mark.receiveShadows = false;
                mark.enabled = false;
            }

            int glints = 0;
            _petal = new Transform[Petals.Length];
            _petalParts = new Renderer[Petals.Length][];
            _petalSize = new float[Petals.Length];
            _petalTall = new float[Petals.Length];
            for (int i = 0; i < Petals.Length; i++)
            {
                if (Petals[i].Glint) glints++;
                var crystal = IceCrystalKit.Spawn(Petals[i].Model, root, out float tall, out _);
                if (crystal == null) continue;
                crystal.name = "Petal" + i;
                _petal[i] = crystal.transform;
                _petalParts[i] = IceCrystalKit.Parts(crystal);
                _petalTall[i] = tall;
                _petalSize[i] = Petals[i].Length / Mathf.Max(0.01f, tall);
                crystal.SetActive(false);
            }

            _lump = new Transform[Heart.Length];
            _lumpParts = new Renderer[Heart.Length][];
            _lumpSize = new float[Heart.Length];
            for (int i = 0; i < Heart.Length; i++)
            {
                var crystal = IceCrystalKit.Spawn(Heart[i].Model, root, out float tall, out _);
                if (crystal == null) continue;
                crystal.name = "Heart" + i;
                _lump[i] = crystal.transform;
                _lumpParts[i] = IceCrystalKit.Parts(crystal);
                _lumpSize[i] = Heart[i].Height / Mathf.Max(0.01f, tall);
                crystal.SetActive(false);
            }

            _chip = new Transform[Chips.Length];
            _chipParts = new Renderer[Chips.Length][];
            _chipSize = new float[Chips.Length];
            for (int i = 0; i < Chips.Length; i++)
            {
                var crystal = IceCrystalKit.Spawn(Chips[i].Model, root, out float tall, out _);
                if (crystal == null) continue;
                crystal.name = "Chip" + i;
                _chip[i] = crystal.transform;
                _chipParts[i] = IceCrystalKit.Parts(crystal);
                _chipSize[i] = Chips[i].Size / Mathf.Max(0.01f, tall);
                crystal.SetActive(false);
            }

            _puff = new Renderer[Puffs.Length];
            for (int i = 0; i < Puffs.Length; i++) _puff[i] = IceCrystalKit.Halo(root, "mist" + i);

            _glintPetal = new int[glints];
            _glintTip = new Renderer[glints];
            _glintAcross = new Renderer[glints];
            _glintUp = new Renderer[glints];
            _glintStar = new Transform[glints];
            int g = 0;
            for (int i = 0; i < Petals.Length; i++)
            {
                if (!Petals[i].Glint) continue;
                _glintPetal[g] = i;
                _glintTip[g] = IceCrystalKit.Halo(root, "tip" + g);
                var star = new GameObject("Glint" + g);
                star.transform.SetParent(root, false);
                _glintStar[g] = star.transform;
                _glintAcross[g] = IceCrystalKit.Halo(star.transform, "across");
                _glintUp[g] = IceCrystalKit.Halo(star.transform, "up");
                g++;
            }
        }

        // ---------------------------------------------------------------------------------------------------
        // Stepping. Everything is worked out from the age, so any moment can be asked for in any order.
        // ---------------------------------------------------------------------------------------------------

        public void StepTo(float seconds)
        {
            _elapsed = seconds;
            float age = Mathf.Max(0f, seconds);
            float bloom = age - Charge;      // seconds since the flower erupted (negative while charging)
            float broke = age - Stand;       // seconds since the shatter began (negative until it does)

            var eye = Camera.main;
            Quaternion face = eye != null ? eye.transform.rotation : transform.rotation;

            StepLights(age, bloom, broke, face, eye);
            StepGround(age, bloom, broke);
            StepPetals(age);
            StepHeart(age);
            StepChips(age, bloom, broke);
            StepMist(age, bloom, broke, face);
            StepGlints(age, face, eye);
        }

        /// <summary>The charge's light and pool, which become the flash of the bloom and of the shatter, and the
        /// deep glow that stands behind the flower while it is up.</summary>
        private void StepLights(float age, float bloom, float broke, Quaternion face, Camera eye)
        {
            float sparkSize, sparkHeight, sparkStrength, poolSize, poolStrength;
            if (bloom < 0f)
            {
                // Gathering: small, low, growing, with a flicker fast enough to read as cold fire and not a pulse.
                float u = age / Charge;
                sparkSize = Mathf.Lerp(0.25f, 1.0f, u * u);
                sparkHeight = 0.55f;
                sparkStrength = Mathf.Lerp(0.15f, 1.10f, u) * (0.86f + 0.14f * Mathf.Sin(age * 47f));
                poolSize = Mathf.Lerp(0.6f, 2.6f, u);
                poolStrength = 0.50f * u;
            }
            else if (broke >= 0f)
            {
                float k = Mathf.Clamp01(broke / 0.30f), left = (1f - k) * (1f - k);
                sparkSize = Mathf.Lerp(1.2f, 2.2f, EaseOut(k));
                sparkHeight = 1.2f;
                sparkStrength = 0.60f * left;
                poolSize = Mathf.Lerp(2.0f, 5.5f, EaseOut(k));
                poolStrength = 0.50f * left;
            }
            else
            {
                float k = Mathf.Clamp01(bloom / 0.28f), left = (1f - k) * (1f - k);
                sparkSize = Mathf.Lerp(1.2f, 2.4f, EaseOut(k));
                sparkHeight = 1.2f;
                sparkStrength = 1.00f * left;
                poolSize = Mathf.Lerp(2.0f, 6.5f, EaseOut(k));
                poolStrength = 0.80f * left;
            }

            if (_spark != null)
            {
                var t = _spark.transform;
                t.localPosition = new Vector3(0f, sparkHeight, 0f);
                t.rotation = face;
                t.localScale = Vector3.one * sparkSize;
                IceCrystalKit.Glow(_spark, Aqua, sparkStrength);
            }
            if (_pool != null)
            {
                _pool.transform.localScale = new Vector3(poolSize, poolSize, 1f);
                IceCrystalKit.Glow(_pool, Aqua, poolStrength);
            }
            if (_core != null)
            {
                // Behind the flower as the lens sees it, so the spikes stand dark-edged against it instead of being
                // washed by it. It comes up with the bloom and goes with the shatter.
                float up = bloom <= 0f ? 0f : Smooth(bloom / 0.35f) * (1f - Mathf.Clamp01(broke / Shatter));
                Vector3 home = transform.TransformPoint(0f, 1.3f, 0f);
                if (eye != null)
                {
                    // Level, never downward: a lens looking down at the flower must not push the card into the floor.
                    Vector3 away = home - eye.transform.position;
                    away.y = 0f;
                    if (away.sqrMagnitude > 0.01f) home += away.normalized * 0.9f;
                }
                var t = _core.transform;
                t.position = home;
                t.rotation = face;
                t.localScale = Vector3.one * 2.6f;
                IceCrystalKit.Glow(_core, Navy, 0.26f * up * (0.92f + 0.08f * Mathf.Sin(age * 2.1f)));
            }
        }

        /// <summary>The ring (closing in, then running out twice), the frost disc and the frost fern.</summary>
        private void StepGround(float age, float bloom, float broke)
        {
            float radius = 1f, alpha = 0f;
            if (bloom < 0f)
            {
                // It closes faster the nearer it gets: the pull, not a tween.
                float u = age / Charge;
                radius = Mathf.Lerp(2.7f, 0.28f, u * u);
                alpha = 0.75f * Smooth(u / 0.3f);
            }
            else if (bloom < 0.40f)
            {
                float k = bloom / 0.40f;
                radius = Mathf.Lerp(0.4f, 3.5f, EaseOut(k));
                alpha = 0.70f * (1f - k);
            }
            else if (broke >= 0f && broke < 0.45f)
            {
                float k = broke / 0.45f;
                radius = Mathf.Lerp(0.9f, 4.2f, EaseOut(k));
                alpha = 0.55f * (1f - k);
            }
            if (_ring != null)
            {
                _ring.transform.localScale = new Vector3(radius, 1f, radius);
                Ink(_ring, RingPale, alpha);
            }

            float grown = bloom <= 0f ? 0f : EaseOut(Mathf.Clamp01(bloom / 0.35f));
            float left = 1f - Mathf.Clamp01((age - Stand - Shatter) / FrostFade);
            float jolt = broke >= 0f ? 0.30f * Mathf.Exp(-broke * 7f) : 0f;
            if (_frost != null)
            {
                float size = 6.4f * Mathf.Lerp(0.25f, 1f, grown);
                _frost.transform.localScale = new Vector3(size, size, 1f);
                IceCrystalKit.Glow(_frost, FrostBlue, (0.55f * grown + jolt) * left);
            }
            if (_fern != null)
            {
                float reach = 2.9f * Mathf.Lerp(0.12f, 1f, grown);
                _fern.transform.localScale = new Vector3(reach, 1f, reach);
                Ink(_fern, FernPale, 0.60f * grown * left);
            }
        }

        private void StepPetals(float age)
        {
            for (int i = 0; i < Petals.Length; i++)
            {
                var at = _petal[i];
                if (at == null) continue;
                var p = Petals[i];
                float born = Charge + p.Delay, breakAt = Stand + p.Break;
                float t = (age - born) / Rise, u = (age - breakAt) / Fall;
                bool shown = t > 0f && u < 1f;
                if (at.gameObject.activeSelf != shown) at.gameObject.SetActive(shown);
                if (!shown) continue;

                // Out of the ground past its length and back: the overshoot is in the curve, not added after.
                float e = t >= 1f ? 1f : Overshoot(t);
                // It opens as it comes, like a petal: more upright at first, a little past its lean, then home,
                // with a small dying rock once it has arrived.
                float lean = p.Lean * (0.72f + 0.28f * e);
                float settled = age - born - Rise;
                if (settled > 0f) lean += 4f * Mathf.Exp(-settled * 5f) * Mathf.Sin(settled * 15f);

                float roll = p.Roll, size = 1f, lit = 1f + 1.5f * Mathf.Exp(-(age - born) * 6f);
                if (settled > 0f) lit += 0.12f * Mathf.Sin(age * 2.4f + i * 1.9f);
                Quaternion around = Quaternion.Euler(0f, p.Around, 0f);
                Vector3 outward = around * Vector3.forward;
                Vector3 foot = outward * p.Radius;

                if (u < 0f)
                {
                    // The tenth of a second before it goes: it lights from inside and shivers. The warning.
                    float tense = Mathf.Clamp01((age - (breakAt - 0.10f)) / 0.10f);
                    if (tense > 0f)
                    {
                        lit += 1.2f * tense * tense;
                        lean += 1.5f * tense * Mathf.Sin(age * 70f + i);
                    }
                }
                else
                {
                    // Broken: it topples outward, slowly then fast, on a short hop that ends under the ground, and
                    // shrinks to nothing as it goes. The flash of the crack dies in the first half.
                    float drop = u * u;
                    lean = Mathf.Lerp(lean, 92f, drop);
                    roll += ((i & 1) == 0 ? 160f : -160f) * u;
                    foot += outward * (0.55f * u);
                    foot.y += p.Length * (0.16f * u - 0.42f * drop);
                    size = 1f - drop;
                    lit += 1.6f * (1f - u) * (1f - u);
                }

                float length = _petalSize[i] * Mathf.Max(0.02f, e) * size;
                float girth = _petalSize[i] * p.Girth * Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(t * 1.8f)) * size;
                at.localPosition = foot;
                at.localRotation = around * Quaternion.Euler(lean, 0f, 0f) * Quaternion.Euler(0f, roll, 0f);
                at.localScale = new Vector3(girth, length, girth);
                // Each on its own clock, so the glints of twenty-three spikes do not shimmer in step.
                IceCrystalKit.Dress(_petalParts[i], age + i * 0.37f, lit);
            }
        }

        private void StepHeart(float age)
        {
            for (int i = 0; i < Heart.Length; i++)
            {
                var at = _lump[i];
                if (at == null) continue;
                var l = Heart[i];
                float born = Charge + l.Delay, breakAt = Stand + 0.05f + 0.015f * i;
                float t = (age - born) / Rise, u = (age - breakAt) / Fall;
                bool shown = t > 0f && u < 1f;
                if (at.gameObject.activeSelf != shown) at.gameObject.SetActive(shown);
                if (!shown) continue;

                float e = t >= 1f ? 1f : Overshoot(t);
                float size = _lumpSize[i] * Mathf.Max(0.02f, e);
                float tilt = l.TiltX, lit = 1f + 1.2f * Mathf.Exp(-(age - born) * 6f);
                var pos = new Vector3(l.X, 0f, l.Z);
                if (u >= 0f)
                {
                    float drop = u * u;
                    size *= 1f - drop;
                    pos.y += 0.35f * u - 0.90f * drop;
                    tilt += 50f * u;
                    lit += 1.4f * (1f - u) * (1f - u);
                }
                at.localPosition = pos;
                at.localRotation = Quaternion.Euler(0f, l.Yaw, 0f) * Quaternion.Euler(tilt, 0f, l.TiltZ);
                at.localScale = Vector3.one * size;
                IceCrystalKit.Dress(_lumpParts[i], age + 11f + i * 0.61f, lit);
            }
        }

        private void StepChips(float age, float bloom, float broke)
        {
            for (int i = 0; i < Chips.Length; i++)
            {
                var at = _chip[i];
                if (at == null) continue;
                var c = Chips[i];
                bool shown = false;
                float size = 0f;
                Vector3 pos = Vector3.zero;

                if (bloom < 0f)
                {
                    // Drawn in on a curl, faster the nearer it gets, and worn down as it arrives.
                    float u = age / Charge, pull = u * u;
                    pos = Polar(c.Bearing + 150f * pull, Mathf.Lerp(c.Far, 0.10f, pull), Mathf.Lerp(c.High, 0.55f, pull));
                    size = Smooth(u / 0.25f) * (1f - 0.65f * pull);
                    shown = size > 0.02f;
                }
                else
                {
                    // Thrown: out of the middle at the bloom, off the rings at the shatter. A real arc (it rises
                    // on its loft and the ground takes it), not a line.
                    float since, fromRadius, fromHeight, push, loft;
                    if (broke >= 0.04f)
                    {
                        since = broke - 0.04f; fromRadius = c.From; fromHeight = 0.25f + c.High * 0.6f;
                        push = c.Speed * 0.6f; loft = c.Loft * 0.7f;
                    }
                    else
                    {
                        since = bloom; fromRadius = 0.30f; fromHeight = 0.50f; push = c.Speed; loft = c.Loft;
                    }
                    float height = fromHeight + loft * since - 4.9f * since * since;
                    if (height > 0.02f && since < 0.9f)
                    {
                        pos = Polar(c.Bearing, fromRadius + push * since, height);
                        size = 1f - Smooth((since - 0.45f) / 0.35f);
                        shown = size > 0.02f;
                    }
                }

                if (at.gameObject.activeSelf != shown) at.gameObject.SetActive(shown);
                if (!shown) continue;
                at.localPosition = pos;
                at.localRotation = Quaternion.Euler(c.Spin * age * 0.6f, c.Spin * age, c.Bearing);
                at.localScale = Vector3.one * (_chipSize[i] * size);
                IceCrystalKit.Dress(_chipParts[i], age + 23f + i, 1.25f);
            }
        }

        private void StepMist(float age, float bloom, float broke, Quaternion face)
        {
            for (int i = 0; i < Puffs.Length; i++)
            {
                var puff = _puff[i];
                if (puff == null) continue;
                if (bloom <= 0f) { IceCrystalKit.Glow(puff, MistBlue, 0f); continue; }

                var m = Puffs[i];
                float arrived = Smooth(bloom / 0.30f);
                float strength = 0.15f * arrived * (0.85f + 0.15f * Mathf.Sin(age * 1.7f + m.Phase));
                float size = m.Size * (0.6f + 0.4f * arrived) * (1f + 0.05f * Mathf.Sin(age * 1.3f + m.Phase));
                float radius = m.Radius + m.Drift * bloom;
                if (broke > 0f)
                {
                    // The shatter blows it outward and thins it away.
                    float burst = 1f - Mathf.Exp(-broke * 5f);
                    radius += 0.9f * burst;
                    size *= 1f + 0.7f * burst;
                    strength = broke < 0.08f
                        ? Mathf.Lerp(strength, 0.32f, broke / 0.08f)
                        : 0.32f * (1f - Mathf.Clamp01((broke - 0.08f) / 1.1f));
                }
                var t = puff.transform;
                // Never lower than half its own height (see the class note on the floor cutting a card).
                t.localPosition = Polar(m.Bearing + 6f * bloom, radius, size * 0.5f + 0.03f + m.Climb * bloom);
                t.rotation = face;
                t.localScale = Vector3.one * size;
                IceCrystalKit.Glow(puff, MistBlue, strength);
            }
        }

        private void StepGlints(float age, Quaternion face, Camera eye)
        {
            for (int g = 0; g < _glintPetal.Length; g++)
            {
                int i = _glintPetal[g];
                var p = Petals[i];
                float born = Charge + p.Delay + Rise * 0.6f, gone = Stand + p.Break;
                if (_petal[i] == null || age <= born || age >= gone)
                {
                    IceCrystalKit.Glow(_glintTip[g], Aqua, 0f);
                    IceCrystalKit.Glow(_glintAcross[g], GlintWhite, 0f);
                    IceCrystalKit.Glow(_glintUp[g], GlintWhite, 0f);
                    continue;
                }

                // The tip, in the world, wherever the petal's lean and length have put it this frame; and a little
                // toward the lens, so the spike does not cut the light in half (the film does the same).
                Vector3 tip = _petal[i].TransformPoint(0f, _petalTall[i], 0f);
                if (eye != null)
                {
                    Vector3 toward = eye.transform.position - tip;
                    if (toward.sqrMagnitude > 0.01f) tip += toward.normalized * 0.25f;
                }

                // A flare as the tip arrives, then a twinkle: mostly dark, with a short bright beat, each on its
                // own pace so no two go off together.
                float arrive = Mathf.Exp(-(age - born) * 7f);
                float beat = Mathf.Max(0f, Mathf.Sin(age * (2.9f + 0.45f * g) + g * 2.1f));
                beat *= beat; beat *= beat; beat *= beat;
                float star = Mathf.Min(1.4f, 1.2f * beat + 1.3f * arrive);
                float reach = 0.40f + 0.30f * Mathf.Min(1f, star);

                if (_glintTip[g] != null)
                {
                    var t = _glintTip[g].transform;
                    t.position = tip;
                    t.rotation = face;
                    t.localScale = Vector3.one * 0.8f;
                    IceCrystalKit.Glow(_glintTip[g], Aqua, 0.28f + 0.25f * arrive);
                }
                _glintStar[g].SetPositionAndRotation(tip, face);
                if (_glintAcross[g] != null) _glintAcross[g].transform.localScale = new Vector3(reach, 0.07f, 1f);
                if (_glintUp[g] != null) _glintUp[g].transform.localScale = new Vector3(0.07f, reach, 1f);
                IceCrystalKit.Glow(_glintAcross[g], GlintWhite, star);
                IceCrystalKit.Glow(_glintUp[g], GlintWhite, star);
            }
        }

        // ---------------------------------------------------------------------------------------------------
        // Small helpers.
        // ---------------------------------------------------------------------------------------------------

        /// <summary>A see-through mark's colour and how much of it shows. One that shows nothing is not drawn.</summary>
        private static void Ink(Renderer mark, Color colour, float alpha)
        {
            if (mark == null) return;
            bool shown = alpha > 0.004f;
            if (mark.enabled != shown) mark.enabled = shown;
            if (!shown) return;
            var material = mark.sharedMaterial;
            if (material == null) return;
            colour.a = alpha;
            material.color = colour;
        }

        /// <summary>A point <paramref name="radius"/> out from the middle at <paramref name="degrees"/> round it,
        /// <paramref name="height"/> up, in the flower's own space.</summary>
        private static Vector3 Polar(float degrees, float radius, float height)
        {
            float turn = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(turn) * radius, height, Mathf.Cos(turn) * radius);
        }

        /// <summary>0 to 1 that goes about a tenth past 1 on the way and comes back to rest exactly on it.</summary>
        private static float Overshoot(float t)
        {
            float k = t - 1f;
            return 1f + 2.70158f * k * k * k + 1.70158f * k * k;
        }

        private static float EaseOut(float t)
        {
            float k = 1f - Mathf.Clamp01(t);
            return 1f - k * k * k;
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
