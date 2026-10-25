using UnityEngine;
using Knot = TumbangPreso.Visual.FxStudyKit.Knot;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ KEPT AS FILMED: BANK SHOT b3, so it can be filmed again by name (`zackbankb3`) beside later passes. The owner's
    /// word on it, 2026-10-17: "zack's bank shot definietly needs work". Its successor is `FxStudyZackBank`.
    ///
    /// STUDY J, BANK SHOT (second rough sample, b3). Zack loads the tsinelas he is holding for eight seconds
    /// (`ZackHeroKit.BankShotAbility`); its next throw keeps 85 per cent of its speed on the first wall bank, and under
    /// Overclock two banks are credited. The picture has four jobs, each a thing a player must read AT MATCH DISTANCE
    /// (the first sample was hairlines on the shoe's outline and could not be seen from 8 m):
    ///   * WHICH SHOE, AND THAT IT IS LIVE: a crackling gold AURA stands off the shoe, about two fists across (three
    ///     jagged rings of the bolt at three tilts, snapping between shapes fourteen times a second, over a soft gold
    ///     glow), so the shoe reads as charged at a glance;
    ///   * HOW MANY BANKS IT WILL PAY: one bright HEART orbits the shoe for each, tied to it by a short bolt. Two hearts
    ///     half a turn apart under Overclock, one otherwise. Counting dots is the one count that survives distance;
    ///   * HOW LONG IT HAS LEFT: over the last 1.5 s of the eight the aura and hearts sputter (whole strikes drop out,
    ///     more and more often) and dim, and when the load runs out they FIZZLE: the rings are eaten, the hearts pop,
    ///     a few sparks go up, and the shoe is plain;
    ///   * WHAT A CREDITED BANK IS: electricity grounding into the wall. The heart it spends jumps to the wall as a
    ///     short arc, a gold scorch-star is left on the wall for a beat, and sparks are thrown back off it. Its own
    ///     form: the white-blue star stays the tell's (the load and Quick Circuit), never the bank's.
    /// In flight the loaded shoe drags a short gold streak BEHIND it (it starts a quarter metre back of the shoe, so the
    /// shoe itself stays in clear view); after its last bank the streak is eaten and the shoe flies on plain.
    ///
    /// ⚠️ MADE ONCE, SHARED BY EVERY SHOE. Every mesh and every material here is built the first time any shoe needs
    /// them and is then shared by all: a shoe owns only its objects and one property block each, which is where its
    /// clock, how much shows and how far it is eaten go. Nothing is built while it plays; a frame moves transforms and
    /// writes property blocks. The sparks are one prebuilt mesh a burst, flown in the shader (`ZackSparks.shader`).
    ///
    /// Built from what is approved: `FxStudyKit.Sweep`, the fifth trail (`FxStudyTrails.Dress`), Quick Circuit's bolt
    /// and its masks (`zack_bolt`, `zack_ground_star`, `zack_spark_sheet`) and colours. Nothing new is painted.
    ///
    /// The bench has no character and no court: the real `tsinelas_classic` model as the shoe, two stand-in walls, and a
    /// throw along a fixed route at the rule's speeds (thrown at 10 m/s, 85 per cent kept at each bank).
    /// </summary>
    public sealed class FxStudyZackBankB3 : FxStudyEffect
    {
        /// <summary>What a bench film of it shows.</summary>
        public enum Show { Overclock, Single, RunsOut, EightMetres }

        private const int MaxCharges = 2, Shapes = 3, Tilts = 3, SparksPerBurst = 8;
        private const float Strikes = 14f, Load = 0.30f, Throw = 1.40f, Kept = 0.85f, Speed = 10f, LoadSeconds = 8f;
        private const float Aura = 0.30f, Orbit = 0.40f, Turns = 2.2f;

        // Quick Circuit's colours: AMBER the air and the road, LEMON the charge, INK and SPARK the tell's cool pair.
        private static readonly Color AmberDeep = new Color(1.00f, 0.50f, 0.04f), Amber = new Color(1.00f, 0.64f, 0.08f),
                                      AmberHot = new Color(1.00f, 0.84f, 0.34f), AmberVeil = new Color(1.00f, 0.54f, 0.06f);
        private static readonly Color LemonDeep = new Color(1.00f, 0.80f, 0.10f), Lemon = new Color(1.00f, 0.95f, 0.28f),
                                      White = new Color(1.00f, 1.00f, 0.90f), LemonVeil = new Color(1.00f, 0.86f, 0.18f);
        private static readonly Color Ink = new Color(0.10f, 0.06f, 0.26f), Spark = new Color(0.86f, 0.95f, 1.00f);

        // The throw: from his hand to a side wall (x = 2.2), off it to a back wall (z = 5.0), and away into the room.
        private static readonly Vector3[] Route = { new Vector3(0f, 1.0f, 0.3f), new Vector3(2.1f, 1.0f, 2.2f), new Vector3(0.0f, 1.0f, 4.9f), new Vector3(-1.9f, 1.0f, 2.6f) };
        private static readonly Vector3[] Facing = { Vector3.left, Vector3.back };

        // ---------------------------------------------------------------- shared, made once

        private static readonly int AgeId = Shader.PropertyToID("_Age"), OpacityId = Shader.PropertyToID("_Opacity"), ErodeId = Shader.PropertyToID("_Erode"),
                                    WindowId = Shader.PropertyToID("_Window"), TileId = Shader.PropertyToID("_Tile"), NowId = Shader.PropertyToID("_Now"), BornId = Shader.PropertyToID("_Born");
        private static Material _boltMat, _auraGlowMat, _heartGlowMat, _streakMat, _scorchMat, _burstMat, _sparkMat, _plainMat;
        private static Mesh[] _rings, _tethers;
        private static Mesh _blade, _square, _sparkMesh;

        /// <summary>How many distinct materials and meshes every Bank Shot on every shoe shares (a probe's count).</summary>
        public static int SharedMaterials => 8;

        private static void M(Material m, string name, float v) { if (m != null) m.SetFloat(name, v); }
        private static void M(Material m, string name, float x, float y, float z = 0f, float w = 0f) { if (m != null) m.SetVector(name, new Vector4(x, y, z, w)); }
        private static void C(Material m, Color deep, Color body, Color hot, Color halo)
        { if (m == null) return; m.SetColor("_Deep", deep); m.SetColor("_Body", body); m.SetColor("_Hot", hot); m.SetColor("_HaloColor", halo); }
        private static Material Queued(Material m, int order) { if (m != null) { m.renderQueue = 3000 + order; m.hideFlags = HideFlags.HideAndDontSave; } return m; }

        private static void Make()
        {
            if (_boltMat != null) return;
            // Quick Circuit's bolt, as approved: a thin white line, a flat lemon body, its forks; nothing scrolls.
            _boltMat = Queued(FxStudyKit.Ribbon("zack_bolt", "erode_streak"), 20);
            C(_boltMat, LemonDeep, Lemon, White, LemonVeil);
            M(_boltMat, "_Scroll", 0f, 0f); M(_boltMat, "_Cut", 0.10f, 0.50f); M(_boltMat, "_Stops", 0.40f, 0.72f, 0.94f); M(_boltMat, "_Ends", 0.03f, 0.03f, 0f);
            M(_boltMat, "_ErodeTile", 1.5f, 1f); M(_boltMat, "_Sweep", 1.0f); M(_boltMat, "_Soft", 0.10f); M(_boltMat, "_Halo", 0.45f); M(_boltMat, "_Add", 0.80f);
            M(_boltMat, "_Gain", 1.3f); M(_boltMat, "_Emit", 0.9f); M(_boltMat, "_Hue", 6f, 4f, 0.2f, 1f);
            _auraGlowMat = Glow(3, AmberHot, Amber);
            _heartGlowMat = Glow(30, White, Lemon);
            // The streak: the approved fifth trail in Quick Circuit's amber. (`Dress` wants a piece; a throwaway one is made
            // for it and its material kept.)
            var dressed = new FxPiece("dress", null, null, FxStudyKit.Ribbon(FxStudyTrails.MaskOf(4)), 8);
            FxStudyTrails.Dress(dressed, 4).Colours(AmberDeep, Amber, AmberHot, AmberVeil).Set("_Scroll", 2.0f, 0f).Set("_Hue", 7f, 5f, 0.3f, 1f)
                   .Set("_Add", 0.78f).Set("_Halo", 0.55f).Set("_Ends", 0.30f, 0.06f, 0.10f);
            _streakMat = Queued(dressed.Material, 8);
            if (Application.isPlaying) Object.Destroy(dressed.Go); else Object.DestroyImmediate(dressed.Go);
            // The scorch-star on the wall: the strike Quick Circuit leaves on the road, in its amber with a white heart.
            _scorchMat = Queued(FxStudyKit.Ribbon("zack_ground_star", "erode_radial"), 4);
            C(_scorchMat, AmberDeep, Amber, White, AmberVeil);
            M(_scorchMat, "_Ends", 0f, 0f, 0f); M(_scorchMat, "_Scroll", 0f, 0f); M(_scorchMat, "_Cut", 0.10f, 0.42f); M(_scorchMat, "_Stops", 0.40f, 0.74f, 0.96f);
            M(_scorchMat, "_Polar", 1f); M(_scorchMat, "_Sweep", 0.9f); M(_scorchMat, "_Soft", 0.10f); M(_scorchMat, "_Halo", 0.6f); M(_scorchMat, "_Add", 0.55f);
            M(_scorchMat, "_Gain", 1.35f); M(_scorchMat, "_Emit", 0.7f);
            // The load's burst: the tell's white-blue star with its ink edge.
            _burstMat = Queued(FxStudyKit.Ribbon("zack_ground_star", "erode_radial"), 31);
            C(_burstMat, Ink, Ink, Spark, Ink);
            M(_burstMat, "_Ends", 0f, 0f, 0f); M(_burstMat, "_Scroll", 0f, 0f); M(_burstMat, "_Cut", 0.08f, 0.36f); M(_burstMat, "_Stops", 0.62f, 0.62f, 0.90f);
            M(_burstMat, "_Polar", 1f); M(_burstMat, "_Sweep", 0.9f); M(_burstMat, "_Soft", 0.10f); M(_burstMat, "_Halo", 0f); M(_burstMat, "_Add", 0.10f); M(_burstMat, "_Gain", 1.3f); M(_burstMat, "_Emit", 0.8f);
            var sparkShader = Shader.Find("TumbangPreso/ZackSparks");
            if (sparkShader != null)
            {
                _sparkMat = Queued(new Material(sparkShader), 40);
                _sparkMat.SetTexture("_MaskTex", FxStudyKit.Mask("zack_spark_sheet"));
                _sparkMat.SetColor("_Deep", LemonDeep); _sparkMat.SetColor("_Body", Lemon); _sparkMat.SetColor("_Hot", White);
            }
            _plainMat = Queued(FxStudyKit.Ribbon(null), 1);
            M(_plainMat, "_Ends", 0f, 0f, 0f); M(_plainMat, "_Scroll", 0f, 0f); M(_plainMat, "_Halo", 0f); M(_plainMat, "_Add", 0f); M(_plainMat, "_Cut", 0f, 0.1f);
            M(_plainMat, "_Stops", 0.2f, 0.3f, 0.6f); M(_plainMat, "_Emit", 0f);

            // The aura's rings: three tilts, three shapes each, every one a jagged closed loop about two fists across.
            _rings = new Mesh[Tilts * Shapes];
            for (int tilt = 0; tilt < Tilts; tilt++)
                for (int s = 0; s < Shapes; s++)
                {
                    Quaternion turn = tilt == 0 ? Quaternion.identity : tilt == 1 ? Quaternion.Euler(70f, 0f, 20f) : Quaternion.Euler(-20f, 0f, 75f);
                    var knots = new Knot[10];
                    for (int k = 0; k < knots.Length; k++)
                    {
                        float a = (k % 9) / 9f * Mathf.PI * 2f + (k % 9 == 0 ? 0f : (FxStudyKit.Hash(k, tilt * 17 + s * 5 + 1) - 0.5f) * 0.4f);
                        float r = Aura * (k % 9 == 0 ? 1f : 0.80f + 0.40f * FxStudyKit.Hash(k, tilt * 17 + s * 5 + 2));
                        knots[k] = new Knot(turn * new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), 0.10f);
                    }
                    _rings[tilt * Shapes + s] = Shared(FxStudyKit.Sweep(knots, turn * Vector3.up, perKnot: 1, across: 4));
                }
            // A heart's tether: a short jagged bolt one metre down +z (it is stretched to the distance it spans).
            _tethers = new Mesh[Shapes];
            for (int s = 0; s < Shapes; s++)
            {
                var knots = new Knot[5];
                for (int k = 0; k < knots.Length; k++)
                {
                    float t = k / 4f, mid = k == 0 || k == 4 ? 0f : 1f;
                    knots[k] = new Knot(new Vector3((FxStudyKit.Hash(k, s * 11 + 3) - 0.5f) * 0.25f * mid, (FxStudyKit.Hash(k, s * 11 + 4) - 0.5f) * 0.25f * mid, t), 0.12f);
                }
                _tethers[s] = Shared(FxStudyKit.Sweep(knots, Vector3.up, perKnot: 1, across: 4));
            }
            var blade = new[] { new Knot(0f, 0f, -1.35f, 0.02f), new Knot(0f, 0f, -0.95f, 0.16f), new Knot(0f, 0f, -0.55f, 0.28f), new Knot(0f, 0f, -0.25f, 0.30f) };
            _blade = Shared(FxStudyKit.Sweep(blade, Vector3.up, perKnot: 6, across: 4));
            _square = Shared(FxStudyKit.Square(false));
            _sparkMesh = Shared(SparkBurst());
        }

        private static Mesh Shared(Mesh mesh) { mesh.hideFlags = HideFlags.HideAndDontSave; return mesh; }

        private static Material Glow(int order, Color core, Color edge)
        {
            var m = Queued(FxStudyKit.Ribbon("glow_soft"), order);
            C(m, edge * 0.6f, edge, core, edge);
            M(m, "_Ends", 0f, 0f, 0f); M(m, "_Scroll", 0f, 0f); M(m, "_Halo", 0f); M(m, "_Add", 1f); M(m, "_Sprite", 1f); M(m, "_Emit", 0f);
            M(m, "_Cut", 0f, 0.55f); M(m, "_Stops", 0.30f, 0.50f, 0.95f);
            return m;
        }

        // One burst of sparks: every spark's whole flight written into its four corners (see `ZackSparks.shader`).
        private static Mesh SparkBurst()
        {
            int n = SparksPerBurst;
            var vertices = new Vector3[n * 4]; var normals = new Vector3[n * 4]; var uv = new Vector2[n * 4]; var corner = new Vector2[n * 4]; var colours = new Color[n * 4];
            var triangles = new int[n * 6];
            for (int i = 0; i < n; i++)
            {
                float turn = (FxStudyKit.Hash(i, 91) - 0.5f) * 2.6f, lift = -0.2f + 2.4f * FxStudyKit.Hash(i, 92), speed = 3.0f + 3.0f * FxStudyKit.Hash(i, 93);
                // Thrown back off the surface (+z here), fanned across it, some up.
                var throwV = new Vector3(Mathf.Sin(turn) * speed, lift, Mathf.Cos(turn) * speed);
                float life = 0.20f + 0.14f * FxStudyKit.Hash(i, 94), delay = 0.03f * FxStudyKit.Hash(i, 95);
                int cell = i % 4; float u0 = (cell % 2) * 0.5f, v0 = (cell / 2) * 0.5f;
                for (int c = 0; c < 4; c++)
                {
                    float across = c % 2 == 0 ? -1f : 1f, along = c < 2 ? -1f : 1f;
                    vertices[i * 4 + c] = Vector3.zero; normals[i * 4 + c] = throwV;
                    corner[i * 4 + c] = new Vector2(across, along);
                    uv[i * 4 + c] = new Vector2(u0 + (along > 0f ? 0.5f : 0f), v0 + (across > 0f ? 0.5f : 0f));
                    colours[i * 4 + c] = new Color(delay, 0f, 0f, life);
                }
                triangles[i * 6] = i * 4; triangles[i * 6 + 1] = i * 4 + 2; triangles[i * 6 + 2] = i * 4 + 1;
                triangles[i * 6 + 3] = i * 4 + 1; triangles[i * 6 + 4] = i * 4 + 2; triangles[i * 6 + 5] = i * 4 + 3;
            }
            var mesh = new Mesh { name = "ZackSparkBurst" };
            mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.uv2 = corner; mesh.colors = colours; mesh.triangles = triangles;
            // The sparks are put where they fly by the shader; the bounds have to hold all of that.
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 4f);
            return mesh;
        }

        /// <summary>One drawn thing on one shoe: an object, a shared mesh and material, and its own property block.</summary>
        private sealed class Part
        {
            public readonly GameObject Go;
            public readonly Transform At;
            private readonly MeshRenderer _renderer;
            private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

            public Part(string name, Transform parent, Mesh mesh, Material material)
            {
                Go = new GameObject(name) { hideFlags = HideFlags.DontSave };
                At = Go.transform; At.SetParent(parent, false);
                Go.AddComponent<MeshFilter>().sharedMesh = mesh;
                _renderer = Go.AddComponent<MeshRenderer>();
                _renderer.sharedMaterial = material;
                _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _renderer.receiveShadows = false;
                if (material == null) _renderer.enabled = false;
            }
            public void Window(float tail, float head, float soft) => _block.SetVector(WindowId, new Vector4(tail, head, soft, 0f));
            public void Tile(float x, float y, float z, float w) => _block.SetVector(TileId, new Vector4(x, y, z, w));
            public void Clock(float now, float born) { _block.SetFloat(NowId, now); _block.SetFloat(BornId, born); }
            public void Frame(float age, float opacity, float erode = 0f)
            {
                bool shown = opacity > 0.002f && erode < 0.999f && _renderer.sharedMaterial != null;
                if (Go.activeSelf != shown) Go.SetActive(shown);
                if (!shown) return;
                _block.SetFloat(AgeId, age); _block.SetFloat(OpacityId, Mathf.Clamp01(opacity)); _block.SetFloat(ErodeId, Mathf.Clamp01(erode));
                _renderer.SetPropertyBlock(_block);
            }
            public void Face(Vector3 eye) { var to = At.position - eye; if (to.sqrMagnitude > 1e-6f) At.rotation = Quaternion.LookRotation(to, Vector3.up); }
        }

        // ---------------------------------------------------------------- this one

        private readonly Show _show;
        private readonly int _charges;
        private readonly float[] _leg = new float[3], _legSpeed = new float[3];
        private readonly Transform _shoeAt, _centre, _trail;
        private readonly Part[] _ring = new Part[Tilts * Shapes];
        private readonly Part[,] _tether = new Part[MaxCharges, Shapes];
        private readonly Part[] _heart = new Part[MaxCharges];
        private readonly Part _auraGlow, _loadBurst, _streak, _fizzle;
        private readonly Part[] _jump = new Part[MaxCharges], _scorch = new Part[MaxCharges], _flash = new Part[MaxCharges], _sparks = new Part[MaxCharges];

        public override float Seconds => _show == Show.RunsOut ? 2.6f : 3.0f;
        public override Color FloorColour => new Color(0.150f, 0.152f, 0.160f);
        public override Color FloorLine => new Color(0.118f, 0.120f, 0.128f);
        public override float BloomIntensity => 0.26f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            if (_show == Show.EightMetres)
            {
                // A target standing 8 m in front of him, at a player's eye height, through the game's own field of view.
                eye = Route[0] + new Vector3(0f, 0.5f, -8f); focus = Route[0] + new Vector3(0f, 0.1f, 0f); fov = 70f;
                return;
            }
            if (_show == Show.RunsOut) { focus = Route[0]; eye = focus + new Vector3(0.6f, 0.4f, -2.4f); fov = 40f; return; }
            // Level, from the room's open corner: both walls face it.
            eye = new Vector3(-1.4f, 1.45f, -1.9f); focus = new Vector3(0.5f, 1.0f, 2.8f); fov = 52f;
        }

        public FxStudyZackBankB3(Transform parent) : this(parent, Show.Overclock) { }

        public FxStudyZackBankB3(Transform parent, Show show) : base("zackbankb3", parent)
        {
            Make();
            _show = show;
            _charges = show == Show.Single ? 1 : 2;
            float speed = Speed;
            for (int i = 0; i < 3; i++) { _legSpeed[i] = speed; _leg[i] = Vector3.Distance(Route[i], Route[i + 1]) / speed; speed *= Kept; }

            // The stand-ins: two walls and the shoe.
            for (int side = 0; side < 2; side++)
            {
                var wall = new Part("wall", Root, _square, _plainMat);
                wall.At.localPosition = side == 0 ? new Vector3(2.22f, 1.0f, 2.4f) : new Vector3(0.2f, 1.0f, 5.02f);
                wall.At.localRotation = Quaternion.LookRotation(-Facing[side]);
                wall.At.localScale = new Vector3(side == 0 ? 5.6f : 4.8f, 2.0f, 1f);
                wall.Frame(0f, 1f);
                SetPlain(wall, new Color(0.30f, 0.30f, 0.32f));
            }
            _shoeAt = Group("shoe");
            var model = Resources.Load<GameObject>("Models/tsinelas_classic");
            var filter = model != null ? model.GetComponentInChildren<MeshFilter>() : null;
            Mesh shoeMesh = filter != null ? filter.sharedMesh : null;
            var shoe = new Part("stand-in-shoe", _shoeAt, shoeMesh, _plainMat);
            SetPlain(shoe, new Color(0.86f, 0.80f, 0.68f));
            if (shoeMesh != null)
            {
                // Its long way down +z and its sole on the shoe group's floor, 0.46 m long.
                var b = shoeMesh.bounds;
                bool longX = b.size.x > b.size.z;
                float scale = 0.46f / Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.z));
                shoe.At.localRotation = longX ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
                shoe.At.localScale = Vector3.one * scale;
                shoe.At.localPosition = -(shoe.At.localRotation * (b.center * scale)) + Vector3.up * (b.extents.y * scale);
            }

            // Everything that rides the shoe hangs from its middle, which does not turn with it.
            _centre = Group("charge");
            _auraGlow = new Part("aura-glow", _centre, _square, _auraGlowMat);
            for (int i = 0; i < _ring.Length; i++) _ring[i] = new Part("aura-" + i, _centre, _rings[i], _boltMat);
            for (int c = 0; c < MaxCharges; c++)
            {
                for (int s = 0; s < Shapes; s++) _tether[c, s] = new Part("tether-" + c + "-" + s, _centre, _tethers[s], _boltMat);
                _heart[c] = new Part("heart-" + c, _centre, _square, _heartGlowMat);
            }
            _loadBurst = new Part("load-burst", Root, _square, _burstMat);
            _fizzle = new Part("fizzle-sparks", Root, _sparkMesh, _sparkMat);
            _trail = Group("trail");
            _streak = new Part("streak", _trail, _blade, _streakMat);
            for (int c = 0; c < MaxCharges; c++)
            {
                _jump[c] = new Part("jump-" + c, Root, _tethers[c % Shapes], _boltMat);
                _scorch[c] = new Part("scorch-" + c, Root, _square, _scorchMat);
                _flash[c] = new Part("flash-" + c, Root, _square, _heartGlowMat);
                _sparks[c] = new Part("sparks-" + c, Root, _sparkMesh, _sparkMat);
            }
        }

        // A stand-in's flat colour, through its block (the plain material is shared).
        private static void SetPlain(Part part, Color colour)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor("_Hot", colour); block.SetFloat(OpacityId, 1f);
            part.Go.GetComponent<MeshRenderer>().SetPropertyBlock(block);
        }

        // Where the thrown shoe is `t` seconds after the throw, which way it goes, which leg it is on, and when that began.
        private Vector3 Flight(float t, out Vector3 going, out int leg, out float legStart)
        {
            legStart = 0f;
            for (leg = 0; leg < 3; leg++)
            {
                if (t < legStart + _leg[leg] || leg == 2) break;
                legStart += _leg[leg];
            }
            going = (Route[leg + 1] - Route[leg]).normalized;
            return Route[leg] + going * (_legSpeed[leg] * (t - legStart));
        }

        public override void Sample(float age, Vector3 eye)
        {
            // The load's own clock: on the bench that shows it running out, the film begins six seconds into the eight.
            float loaded = _show == Show.RunsOut ? age + 6.0f - Load : age - Load;
            float flown = _show == Show.RunsOut ? -1f : age - Throw;
            bool thrown = flown >= 0f;
            int strike = Mathf.FloorToInt(age * Strikes);

            // THE SHOE: in his hand (bobbing a little), then thrown along the route, its toe the way it goes.
            Vector3 going = Vector3.forward; int leg = 0; float legStart = 0f;
            Vector3 at = thrown ? Flight(flown, out going, out leg, out legStart) : Route[0] + Vector3.up * (0.02f * Mathf.Sin(age * 5f));
            _shoeAt.localPosition = at - Vector3.up * 0.05f;
            _shoeAt.localRotation = thrown ? Quaternion.LookRotation(going) * Quaternion.Euler(0f, 0f, 25f * Mathf.Sin(flown * 9f)) : Quaternion.Euler(0f, -20f, 0f);
            Vector3 centre = at + Vector3.up * 0.04f;
            _centre.localPosition = centre;

            int banked = thrown ? Mathf.Min(_charges, leg) : 0;
            float left = LoadSeconds - loaded;
            bool live = loaded >= 0f && (thrown || left > 0f) && banked < _charges;
            // THE LAST 1.5 s: whole strikes drop out, more and more often, and what shows dims.
            float sputter = thrown ? 0f : FxStudyKit.Run(1.5f - left, 0f, 1.5f);
            bool dropped = FxStudyKit.Hash(strike, 77) < 0.70f * sputter;
            float dim = 1f - 0.5f * sputter;
            // THE FIZZLE when it runs out: the rings are eaten, the hearts pop, sparks go up.
            float expired = thrown ? -1f : -left;
            float fizzle = FxStudyKit.Run(expired, 0f, 0.25f);
            bool fizzling = expired >= 0f && expired < 0.25f;
            float landed = FxStudyKit.EaseOut(FxStudyKit.Run(loaded, 0f, 0.10f));

            // THE AURA: three rings at their tilts, one shape of each at a time, over the soft glow.
            int shape = strike % Shapes;
            for (int i = 0; i < _ring.Length; i++)
            {
                var ring = _ring[i];
                ring.At.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, landed) * (1f + 0.25f * fizzle);
                ring.Tile(0.9f, 1f, FxStudyKit.Hash(strike, 31 + i), 0f);
                ring.Frame(age, (live && !dropped || fizzling) && i % Shapes == shape ? dim : 0f, fizzling ? fizzle : 0f);
            }
            _auraGlow.At.localScale = Vector3.one * 0.95f;
            _auraGlow.Face(eye);
            _auraGlow.Frame(age, live && !dropped ? (0.30f + 0.05f * Mathf.Sin(age * 23f)) * dim : 0f);

            // THE HEARTS, one per bank still to pay, orbiting, each tied to the shoe by a snapping bolt.
            for (int c = 0; c < MaxCharges; c++)
            {
                bool on = c < _charges && c >= banked && (live && !dropped || fizzling) && loaded >= c * 0.08f;
                float a = (age * Turns + c * 0.5f) * Mathf.PI * 2f;
                Vector3 offset = new Vector3(Mathf.Cos(a), 0.30f * Mathf.Sin(a), Mathf.Sin(a)) * Orbit;
                float pop = fizzling ? 1f + 0.6f * fizzle : 1f;
                _heart[c].At.localPosition = offset;
                _heart[c].At.localScale = Vector3.one * 0.30f * pop * Mathf.Lerp(0.3f, 1f, landed);
                _heart[c].Face(eye);
                _heart[c].Frame(age, on ? (fizzling ? 1f - fizzle : 1f) * dim : 0f);
                for (int s = 0; s < Shapes; s++)
                {
                    var tether = _tether[c, s];
                    tether.At.localRotation = Quaternion.LookRotation(offset.normalized);
                    tether.At.localScale = new Vector3(1f, 1f, Orbit * Mathf.Lerp(0.3f, 1f, landed));
                    tether.Frame(age, on && !fizzling && (strike + c) % Shapes == s ? 0.9f * dim : 0f);
                }
            }

            // THE LOAD: the tell's burst on the shoe, turned to the eye.
            float burst = FxStudyKit.Run(loaded, 0f, 0.22f);
            _loadBurst.At.localPosition = centre;
            _loadBurst.Face(eye);
            _loadBurst.At.localScale = Vector3.one * Mathf.Lerp(0.4f, 0.95f, FxStudyKit.Pop(FxStudyKit.Run(loaded, 0f, 0.06f)));
            _loadBurst.Frame(age, loaded >= 0f && burst < 1f && _show != Show.RunsOut ? 1f : 0f, Mathf.Max(0f, burst - 0.25f) / 0.75f);
            _fizzle.At.localPosition = centre;
            _fizzle.At.localRotation = Quaternion.LookRotation(Vector3.up);
            _fizzle.Clock(age, age - expired);
            _fizzle.Frame(age, expired >= 0f && expired < 0.6f ? 1f : 0f);

            // THE STREAK, behind a throw that still carries a charge; eaten in a moment after the last bank.
            _trail.localPosition = centre;
            _trail.localRotation = Quaternion.LookRotation(going);
            float sinceLast = banked >= _charges ? flown - legStart : -1f;
            float spent = banked >= _charges ? FxStudyKit.Run(sinceLast, 0f, 0.15f) : 0f;
            _streak.Window(-1f, FxStudyKit.EaseOut(FxStudyKit.Run(flown, 0f, 0.10f)) * 1.1f, 0.07f);
            _streak.Frame(age, thrown && spent < 1f ? 0.85f : 0f, spent);

            // THE BANKS: the heart grounds into the wall.
            float bankAt = 0f;
            for (int c = 0; c < MaxCharges; c++)
            {
                bankAt += _leg[c];
                float since = flown - bankAt;
                bool credited = c < _charges && thrown && since >= 0f;
                Vector3 hit = Route[c + 1], normal = Facing[c];
                // The arc: from the wall to the shoe as it leaves, for a tenth of a second.
                Vector3 toShoe = centre - hit;
                _jump[c].At.localPosition = hit + normal * 0.02f;
                if (toShoe.sqrMagnitude > 1e-4f) _jump[c].At.localRotation = Quaternion.LookRotation(toShoe.normalized);
                _jump[c].At.localScale = new Vector3(1.6f, 1.6f, Mathf.Max(0.05f, toShoe.magnitude));
                _jump[c].Frame(age, credited && since < 0.12f && (strike % 2 == 0 || since < 0.04f) ? 1f : 0f);
                // The scorch-star left on the wall: lands, holds a beat, is eaten outward from its heart.
                _scorch[c].At.localPosition = hit + normal * 0.03f;
                _scorch[c].At.localRotation = Quaternion.LookRotation(normal) * Quaternion.Euler(0f, 0f, 40f * c);
                _scorch[c].At.localScale = Vector3.one * (1.1f * Mathf.Lerp(0.5f, 1f, FxStudyKit.Pop(FxStudyKit.Run(since, 0f, 0.06f))));
                _scorch[c].Frame(age, credited && since < 0.6f ? 1f : 0f, FxStudyKit.Run(since, 0.30f, 0.55f));
                _flash[c].At.localPosition = hit + normal * 0.15f;
                _flash[c].At.localScale = Vector3.one * 0.8f;
                _flash[c].Face(eye);
                _flash[c].Frame(age, credited ? 0.6f * (1f - FxStudyKit.Run(since, 0f, 0.12f)) : 0f);
                // The sparks thrown back off it: one prebuilt burst, flown in the shader from the moment of the bank.
                _sparks[c].At.localPosition = hit + normal * 0.03f;
                _sparks[c].At.localRotation = Quaternion.LookRotation(normal);
                _sparks[c].Clock(age, Throw + bankAt);
                _sparks[c].Frame(age, credited && since < 0.5f ? 1f : 0f);
            }
        }
    }
}
