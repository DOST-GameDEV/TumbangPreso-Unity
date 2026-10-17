using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// STUDY I, THE BOULDER (a rough sample; earth, which the bench had never built). Basilio's BOULDER (`dante_skill2`)
    /// does not throw a rock: it loads the tsinelas he is HOLDING for 15 s (`GeoRules.BoulderLoadSeconds`), and the next
    /// throw of it Concusses the player it hits. There is no area and nothing on the ground is dangerous, so the picture
    /// is the shoe itself: stone torn out of the floor closes on it like a fist, it is carried and thrown with the rock
    /// on it, and on a body the rock bursts. ⚠️ NO RING ON THE GROUND, ANYWHERE: a ring would claim an area that is not there.
    ///
    /// ⚠️ THE SHOE IS STILL A SHOE. The stone grips its heel and sole and leaves the toe and the strap out in front.
    /// Stone and shoe together are about 0.54 m long, 0.47 wide and 0.38 high against the shoe's own 0.51 m hit
    /// capsule: the picture is no bigger than what hits (3 cm over in length).
    ///
    /// Earth is SOLID, so unlike the wind this is mostly modelled stone and not ribbons:
    ///   * THE STONE is four rocks pressed together (`tools/build_dante_rocks.py`): a big one over the heel, a broad
    ///     one under the sole, two smaller at its sides. Carved and baked the way the Lagoon Cove stones are, and
    ///     drawn by `FxStudyRock.shader` with the brushed edge wear the owner approved there. No ink line;
    ///   * HIS MOLTEN LIGHT IS IN THE GAPS between the pressed rocks (the bake knows where they are), and it is the
    ///     effect's light, since nothing in the game blooms. Dark while the stone gathers; the last rock landing
    ///     lights it; it breathes while carried and goes out when the stone breaks;
    ///   * THE SPLIT the rocks come out of is a picture of its own (`dante_split`, `tools/build_dante_fx_textures.py`):
    ///     a pale broken lip, a black depth with real width, one hot seam down the middle. It is torn open and
    ///     throws up three small stones of the same family along its lip, its seam cools, and it closes like a mouth;
    ///   * THE DUST is the bench's CLOUD, small (`FxCloud`, called as it is): the owner, of the flat drawn puffs this
    ///     had: "i want a smaller version of this to replace the 2d clouds in dante's ability". One puff where each rock
    ///     lands, three shed behind and below the stone in flight, two thrown down and out at the hit. A puff is that
    ///     emitter fired for a moment, made by the cloud's own builder (`FxCloud.Build`) under a parent that is scaled
    ///     down, so its balls, its reach and its line shrink together and the approved look holds. All nine are made
    ///     when the effect is; while it plays a puff is switched on, handed its age, and switched off. Nothing is built
    ///     or allocated then, and the cloud's mesh is never touched after it is made (its shader moves it);
    ///   * THE GRIT is the rough dabs of `fleck_sheet`, few; the flash at the hit is `flare_star` and `glow_soft`.
    /// Weight in every part: a rock heaves out slowly and then falls INTO its seat, the stone is knocked by each one
    /// that lands, the throw pulls back first, the rocks of the burst fly on real arcs away from what was hit, hop
    /// once, lie, and are eaten (dark stone shows through the holes).
    ///
    /// 5.4 seconds on the bench: cast 0.20, the last rock lands 0.92, carried to 2.20, pulled back to 2.50, hit at 2.90.
    /// The two grey bodies and the cream shoe are stand-ins for the bench only.
    /// </summary>
    // KEEP THIS LOOK. Of the films this made (court c3, bench u4) the owner said on 2026-10-12: "looks good. just make sure
    // its optimized". What it draws is frozen as filmed (kept in tools/cutscene_rework/approved/danteboulder_c3); how
    // cheaply it draws it may change, proved against those frames. The faults we listed ourselves (small dust, modest
    // molten light, puffs that peak late) were in front of him when he said it: they are not ours to fix unasked.
    public sealed class FxStudyDanteBoulder : FxStudyEffect
    {
        /// <summary>The three moments of it, for a match, which plays them apart: the CAST (the split, its lip, the
        /// landing dust, the blink as the gaps light), the FLIGHT (its dust and chips), and the HIT (the rocks thrown
        /// off, the rubble, the star, the dust). The bench and the court film play all three, with the rocks gathering
        /// and being carried between them; a match draws those rocks on the real shoe itself (`DanteBoulderStone`) and
        /// asks this only for what happens round it.</summary>
        public const int PartCast = 1, PartFlight = 2, PartHit = 4, PartAll = 7;
        private readonly int _parts;
        public const string RockShader = "TumbangPreso/FxStudyRock", Model = "Models/AbilityFx/dante_boulder";
        private const float CastAt = 0.20f, SnapAt = 0.92f, WindAt = 2.20f, ThrowAt = 2.50f, Flight = 0.40f, HitAt = ThrowAt + Flight, Gravity = 9.8f;
        // Where it is held and where it hits, in the effect's own space: +x is the way it is thrown, the floor is y 0.
        // The bench's are 4.2 m apart; a court film gives its own (`FxStudyDanteBoulder(parent, hand, target, ...)`).
        private Vector3 Hand, Target;
        // The bench shows a stand-in shoe and two bodies; a court film has the real ones and none of these.
        private readonly bool _standIns;
        // How big the real shoe is beside the stand-in the rocks were carved round (1 on the bench).
        private float _fit;
        // How much of its laid-on light is kept (1 on the bench; a sunlit court takes less).
        private readonly float _light;
        /// <summary>Toward the light the stone is shaded by, in world space. The bench's own; a court film hands it
        /// the map's sun so the stone is lit from the side everything round it is.</summary>
        public static Vector3 KeyLight = new Vector3(0.35f, 0.85f, -0.40f);

        /// <summary>The sixteen colours of `tools/build_hands_dante.py`: stone, its light, dark and deepest, gold and its
        /// light, the molten seam and its core, then moss, an accent and the bench's stand-ins.</summary>
        public static readonly Color[] Palette =
        {
            Hex(0x8f8577), Hex(0xb9ad9b), Hex(0x62594f), Hex(0x46403a), Hex(0xdfb248), Hex(0xf6dc86), Hex(0xff7a1c), Hex(0xffd257),
            Hex(0x3d6335), Hex(0x3fa65c), Hex(0xc9682d), Hex(0xefe6d2), Hex(0xd9a93a), Hex(0x2f8f4a), Hex(0x3d6335), Hex(0x3a3a40),
        };
        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        private static readonly Color DustDeep = new Color(0.40f, 0.32f, 0.24f), Dust = new Color(0.70f, 0.60f, 0.46f), DustHot = new Color(0.95f, 0.88f, 0.72f);
        private static readonly Color Molten = new Color(1f, 0.48f, 0.11f), MoltenHot = new Color(1f, 0.93f, 0.60f);
        private static readonly Color[] GritTones = { new Color(0.72f, 0.62f, 0.48f), new Color(0.50f, 0.43f, 0.36f), new Color(0.93f, 0.74f, 0.30f), new Color(0.86f, 0.80f, 0.68f) };

        private static readonly int HeatId = Shader.PropertyToID("_Heat"), FlashId = Shader.PropertyToID("_Flash"), InkId = Shader.PropertyToID("_Ink"), GoneId = Shader.PropertyToID("_Gone");
        private static Material _stone, _painted, _covering;

        // One modelled piece: a lump, the heart, a chunk of rubble or a stand-in.
        private sealed class Rock
        {
            public Transform At; public Renderer Renderer; public MaterialPropertyBlock Block = new MaterialPropertyBlock();
            public Vector3 Seat;                 // a lump's place on the finished boulder, from the middle of the shoe's sole
            public float Rest = 0.05f;           // how high its middle lies when it is on the floor
            public void Show(bool shown) { if (At != null && At.gameObject.activeSelf != shown) At.gameObject.SetActive(shown); }
            public void Dress(float heat, float flash, float ink, float gone = 0f)
            {
                if (Renderer == null) return;
                Block.SetFloat(HeatId, heat); Block.SetFloat(FlashId, Mathf.Clamp01(flash)); Block.SetFloat(InkId, ink); Block.SetFloat(GoneId, Mathf.Clamp01(gone));
                Renderer.SetPropertyBlock(Block);
            }
        }

        // One thrown piece: its push (m/s), its size, how it tumbles and lies, and how long after it is thrown it starts
        // to be eaten.
        private readonly struct Throw
        {
            public readonly Vector3 Push; public readonly float Size, Spin, Yaw, Gone;
            public Throw(float x, float y, float z, float size, float spin, float yaw, float gone) { Push = new Vector3(x, y, z); Size = size; Spin = spin; Yaw = yaw; Gone = gone; }
        }
        // The four rocks in the order they land: the one under the sole, the two at its sides, the big one last.
        private static readonly string[] LumpNames = { "jaw", "knuckle-l", "knuckle-r", "crown" };
        private static readonly float[] Leave = { 0.20f, 0.28f, 0.36f, 0.44f }, Arrive = { 0.54f, 0.67f, 0.79f, SnapAt };
        // It hit a body coming from -x, so nothing is thrown on through him: the pieces go back, up and to the sides, hard.
        private static readonly Throw[] LumpThrows =
        {
            // ⚠️ LOW, and never up the middle. Thrown 4 to 6 m/s upward, as these first were, the rocks rose straight through
            // the eye line of the player who had just been hit, and with the dust his whole view was covered.
            new Throw(-1.9f, 1.4f, -0.8f, 1f, -260f, 110f, 1.50f),
            new Throw(-1.2f, 2.2f, -3.6f, 1f,  420f, -30f, 1.36f),
            new Throw(-1.4f, 2.0f,  3.7f, 1f, -380f, 200f, 1.62f),
            new Throw(-3.8f, 2.4f, -1.4f, 1f,  300f,  40f, 1.72f),
        };
        private static readonly Throw[] ChunkThrows =
        {
            new Throw(-2.6f, 2.6f,  2.2f, 0.95f,  520f,  10f, 1.20f),
            new Throw(-1.0f, 2.9f, -2.4f, 0.80f, -480f,  95f, 1.44f),
            new Throw(-4.0f, 1.8f,  0.6f, 0.70f,  610f, 170f, 1.26f),
        };
        // What is knocked off when the crown lands: small, down and a little out.
        private static readonly Throw[] CrumbThrows =
        {
            new Throw(-0.6f, 1.0f, -0.6f, 0.50f,  420f,  20f, 0.80f),
            new Throw( 0.7f, 1.4f, -0.4f, 0.42f, -500f, 140f, 0.92f),
            new Throw( 0.1f, 0.8f,  0.7f, 0.36f,  640f, 260f, 0.72f),
        };

        private readonly Rock[] _lumps = new Rock[LumpNames.Length], _chunks = new Rock[ChunkThrows.Length], _crumbs = new Rock[CrumbThrows.Length];
        private readonly Rock[] _lip = new Rock[3];
        private readonly Rock _shoe = new Rock(), _caster = new Rock(), _victim = new Rock();
        private readonly FxPiece _split, _splitGlow, _snapGlow, _hitGlow, _star;
        private readonly FxPiece _grit;
        private Mesh _gritMesh;
        private readonly List<Spark> _sparks = new List<Spark>();

        // THE DUST: nine puffs of the bench's cloud. Four where the rocks land, three in flight, two at the hit.
        // Twelve balls, and the emitter is on for exactly one ball's life, so a puff uses ALL twelve: a cloud's mesh holds
        // every ball it could ever have out, and a puff that is on for less draws the rest as nothing and still sends them.
        private const int Puffs = 9, PuffBalls = 12;
        private const float PuffOn = 0.36f, PuffLife = 0.36f;
        private static readonly float[] LipSizes = { 0.62f, 0.80f, 0.70f };
        private readonly FxCloud[] _puff = new FxCloud[Puffs];
        private readonly float[] _puffBorn = new float[Puffs];
        private readonly Transform[] _puffHolder = new Transform[Puffs];
        // One emitter for all nine: the cloud's own puff (its column's ball sizes and its approved size curve, untouched).
        // A column climbs five times its own width; a puff of dust is a BILLOW, about as high as it is wide, so it rises
        // 0.8 m and spreads 0.3 m before it is scaled. (At the column's 1.5 m and 0.5 m the first puffs were a scatter
        // of specks.) A puff's place, lean and size are its parent's.
        private static readonly FxCloud.Emitter PuffEmitter = Billow();
        private static FxCloud.Emitter Billow()
        {
            var e = FxCloud.Emitter.Puff(Vector3.zero, new Vector3(0f, 0.8f, 0f), 0f, PuffOn, PuffLife);
            e.Widen = 0.30f; e.Weave = 0.12f; e.Spread = 0.20f;
            return e;
        }

        public override float Seconds => 5.4f;
        public override Color FloorColour => new Color(0.200f, 0.200f, 0.212f);
        public override Color FloorLine => new Color(0.150f, 0.150f, 0.160f);
        public override float FloorDim => 0.55f;
        public override float FloorDimAt => 12f;
        public override float BloomIntensity => 0.30f;

        // The camera: close on his hand for the cast, back for the throw, close on the hit.
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            Vector3 f0 = Hand + new Vector3(0.1f, -0.30f, 0f), e0 = f0 + new Vector3(1.25f, 0.75f, -2.35f);
            float far = (Target - Hand).magnitude / 4.2f;
            Vector3 f1 = new Vector3((Hand.x + Target.x) * 0.5f, 0.85f, (Hand.z + Target.z) * 0.5f), e1 = f1 + new Vector3(0.5f, 1.15f, -8.4f * far);
            Vector3 f2 = Target + new Vector3(-0.55f, -0.40f, 0f), e2 = f2 + new Vector3(-0.9f, 1.05f, -4.0f);
            float back = FxStudyKit.Ease(FxStudyKit.Run(age, 1.85f, 2.40f)), near = FxStudyKit.Ease(FxStudyKit.Run(age, 3.05f, 3.65f));
            focus = Vector3.Lerp(Vector3.Lerp(f0, f1, back), f2, near);
            eye = Vector3.Lerp(Vector3.Lerp(e0, e1, back), e2, near);
            fov = 34f;
        }

        public FxStudyDanteBoulder(Transform parent) : this(parent, new Vector3(-2.2f, 1.05f, 0f), new Vector3(2.0f, 1.0f, 0f), true, 1f, 1f) { }

        /// <summary>When the stone strikes: a match starts a hit's copy of the effect at this age.</summary>
        public const float HitMoment = HitAt;
        /// <summary>When it is cast: the ground splits 0.08 s before this, and the first rock leaves at it.</summary>
        public const float CastMoment = CastAt;

        /// <summary>For a film on a court: where it is held and where it hits (in its own space, +x the throw), without
        /// the bench's stand-ins, at the real shoe's size, with this share of its laid-on light.</summary>
        public FxStudyDanteBoulder(Transform parent, Vector3 hand, Vector3 target, bool standIns, float fit, float light, int parts = PartAll) : base("danteboulder", parent)
        {
            Hand = hand; Target = target; _standIns = standIns; _fit = fit; _light = light; _parts = parts;
            BuildRocks();

            // The split under his hand: the picture is a pale lip, a black depth and a hot seam, read in three steps.
            _splitGlow = AddGlow("split-glow", 1, MoltenHot, Molten, lying: true);
            PlaceSplit();
            _split = AddFlat("split", 2, "dante_split", lying: true, erode: "erode_cloud");
            _split.Colours(new Color(0.74f, 0.65f, 0.50f), new Color(0.030f, 0.022f, 0.018f), MoltenHot, Molten)
                  .Set("_Cut", 0.08f, 0.16f).Set("_Stops", 0.40f, 0.62f, 0.90f).Set("_Add", 0f).Set("_Soft", 0.08f).Set("_Emit", 0f);
            PlaceSplit();

            BuildDust();

            _snapGlow = AddGlow("snap-glow", 8, MoltenHot, Molten);
            _hitGlow = AddGlow("hit-glow", 9, MoltenHot, Molten);
            _star = AddFlat("star", 10, "flare_star", lying: false);
            _star.Colours(Molten, new Color(1f, 0.80f, 0.36f), Color.white, Molten).Set("_Emit", 1.2f);

            // The grit: thrown on the GPU (`FxStudyGrit.shader`) from a mesh made when the effect is placed, dressed exactly as
            // the bench's flecks were (`FxFlecks`: the sheet's settings, then these).
            _gritMesh = new Mesh { name = "grit", hideFlags = HideFlags.HideAndDontSave };
            _grit = Add("grit", _gritMesh, GritMaterial(), 12);
            _grit.Set("_Ends", 0f, 0f, 0f).Set("_Halo", 0f).Set("_Add", 1f).Set("_Scroll", 0f, 0f).Set("_Cut", 0.05f, 0.5f).Set("_Stops", 0.3f, 0.6f, 0.95f);
            _grit.Colours(DustDeep, Dust, DustHot, Dust).Set("_Add", 0.10f).Set("_Stops", 0.38f, 0.70f, 0.98f).Set("_Cut", 0.10f, 0.55f).Set("_Emit", 0f).Set("_Gravity", Gravity);
            BuildGrit();
        }

        // ------------------------------------------------------------------ kept between casts

        /// <summary>
        /// ⚠️ A MATCH KEEPS ITS MOMENTS AND PLAYS THEM AGAIN (`DanteBoulderMoment`). Making a moment's copy of this effect is its
        /// pieces, materials, clouds and models: about a millisecond each time, every cast and every hit. So a copy that has
        /// played is not thrown away: it is put aside here, switched off, and the next cast or hit of the same part takes it,
        /// placed again (`Retarget`), instead of making a new one. Its picture is the same: `Sample` is a pure function of
        /// the age, so a copy played twice draws what a new one would. `Pool` off makes and frees a copy every time, as
        /// before. What is put aside lives under `SpareHolder` (one hidden object, not saved), and goes with its scene.
        /// </summary>
        public static bool Pool = true;
        public static Transform SpareHolder;
        private static readonly Dictionary<int, Stack<FxStudyDanteBoulder>> Spare = new Dictionary<int, Stack<FxStudyDanteBoulder>>();

        /// <summary>A copy of this effect for one moment of a match (the cast or the hit): one put aside, placed again, or a new one.</summary>
        public static FxStudyDanteBoulder ForMoment(Transform parent, Vector3 hand, Vector3 target, float fit, int parts)
        {
            if (Pool && Spare.TryGetValue(parts, out var kept))
                while (kept.Count > 0)
                {
                    var fx = kept.Pop();
                    if (fx == null || fx.Root == null) continue;
                    fx.Root.SetParent(parent, false);
                    fx.Retarget(hand, target, fit);
                    fx.Root.gameObject.SetActive(true);
                    return fx;
                }
            return new FxStudyDanteBoulder(parent, hand, target, false, fit, 0.6f, parts);
        }

        /// <summary>Done with a moment's copy: put aside for the next (`Pool`), or freed.</summary>
        public static void Release(FxStudyDanteBoulder fx)
        {
            if (fx == null || fx.Root == null) return;
            if (!Pool) { fx.Dispose(); return; }
            if (SpareHolder == null)
            {
                SpareHolder = new GameObject("~DanteBoulderSpare") { hideFlags = HideFlags.HideAndDontSave }.transform;
                SpareHolder.gameObject.SetActive(false);
            }
            fx.Root.SetParent(SpareHolder, false);
            if (!Spare.TryGetValue(fx._parts, out var kept)) Spare[fx._parts] = kept = new Stack<FxStudyDanteBoulder>();
            kept.Push(fx);
        }

        /// <summary>The same effect, held at `hand` and striking at `target`, at the real shoe's size: everything that was placed
        /// when it was made from those is placed again (the split, the dust, the grit's arcs); the rest is posed by `Sample`.</summary>
        public void Retarget(Vector3 hand, Vector3 target, float fit)
        {
            Hand = hand; Target = target; _fit = fit;
            PlaceSplit();
            BuildDust();
            BuildGrit();
            // The map's light, on the stone and on the dust.
            Stone(true);
            foreach (var cloud in _puff) if (cloud != null && cloud.Piece.Material != null) cloud.Piece.Material.SetVector("_Key", KeyLight);
        }

        private void PlaceSplit()
        {
            if (_splitGlow != null) _splitGlow.At.localPosition = new Vector3(Hand.x, 0.008f, 0f);
            if (_split != null) { _split.At.localPosition = new Vector3(Hand.x, 0.014f, 0f); _split.At.localRotation = Quaternion.Euler(0f, 22f, 0f); }
        }

        // One spark of grit: where it starts and the push it is given (the effect's own space, m/s), when (on its clock), how
        // long and wide, which dab of the sheet and what tint. The same numbers the bench's flecks were laid from each frame.
        private readonly struct Spark
        {
            public readonly Vector3 From, Push; public readonly float At, Length, Width; public readonly int Cell; public readonly Color Tint;
            public Spark(Vector3 from, Vector3 push, float at, float length, float width, int cell, Color tint) { From = from; Push = push; At = at; Length = length; Width = width; Cell = cell; Tint = tint; }
        }

        private static Material GritMaterial()
        {
            var shader = Shader.Find("TumbangPreso/FxStudyGrit");
            if (shader == null) return null;
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            var sheet = FxStudyKit.Mask("fleck_sheet");
            if (sheet != null) material.SetTexture("_MaskTex", sheet);
            var eaten = FxStudyKit.Mask("erode_streak");
            if (eaten != null) material.SetTexture("_ErodeTex", eaten);
            return material;
        }

        /// <summary>The grit's arcs, worked out once for where the effect is held and strikes, into one mesh the GPU poses.</summary>
        private void BuildGrit()
        {
            _sparks.Clear();
            // Shaken off as the crown lands: ten, round the hand.
            if ((_parts & PartCast) != 0)
                for (int i = 0; i < 10; i++)
                {
                    float turnBy = (i + FxStudyKit.Hash(i, 1)) / 10f * Mathf.PI * 2f, speed = 0.5f + 1.1f * FxStudyKit.Hash(i, 2), rise = 0.4f + 1.4f * FxStudyKit.Hash(i, 3);
                    var flat = new Vector3(Mathf.Cos(turnBy), 0f, Mathf.Sin(turnBy));
                    _sparks.Add(new Spark(Hand + flat * 0.22f, flat * speed + Vector3.up * rise, SnapAt, 0.07f + 0.06f * FxStudyKit.Hash(i, 4), 0.055f, i * 5 + 2, GritTones[i % GritTones.Length]));
                }
            // Shed in flight: six, off the path, thrown on and down.
            if ((_parts & PartFlight) != 0)
                for (int i = 0; i < 6; i++)
                {
                    float f = 0.15f + 0.13f * i;
                    var push = new Vector3(3.0f, 0.6f + 1.2f * FxStudyKit.Hash(i, 9), (FxStudyKit.Hash(i, 10) - 0.5f) * 1.6f);
                    _sparks.Add(new Spark(Path(f), push, ThrowAt + f * Flight, 0.08f, 0.055f, i * 7 + 3, GritTones[i % GritTones.Length]));
                }
            // Thrown by the burst: eighteen, back and up and to the sides.
            if ((_parts & PartHit) != 0)
                for (int i = 0; i < 18; i++)
                {
                    float turnBy = (i + FxStudyKit.Hash(i, 5)) / 18f * Mathf.PI * 2f, speed = 1.4f + 3.0f * FxStudyKit.Hash(i, 6), rise = 1.0f + 3.6f * FxStudyKit.Hash(i, 7);
                    var flat = new Vector3(Mathf.Cos(turnBy) * 0.7f - 0.6f, 0f, Mathf.Sin(turnBy));
                    _sparks.Add(new Spark(Target + flat * 0.15f, flat * speed + Vector3.up * rise, HitAt, 0.08f + 0.09f * FxStudyKit.Hash(i, 8), 0.06f, i * 3 + 1, GritTones[(i + 1) % GritTones.Length]));
                }
            if (_gritMesh == null) return;
            // Four corners a spark, as `FxFlecks.Add` lays them: tail and head, either side; the sheet's dab, four to a side.
            int n = _sparks.Count;
            var starts = new Vector3[n * 4]; var pushes = new Vector3[n * 4]; var colours = new Color[n * 4];
            var uv = new Vector2[n * 4]; var thrown = new Vector2[n * 4]; var corner = new Vector3[n * 4]; var triangles = new int[n * 6];
            const int g = 4; const float step = 1f / g;
            for (int k = 0; k < n; k++)
            {
                var s = _sparks[k];
                int cell = ((s.Cell % (g * g)) + g * g) % (g * g);
                float u0 = (cell % g) / (float)g, v0 = (cell / g) / (float)g;
                var colour = new Color(s.Tint.r, s.Tint.g, s.Tint.b, 0.95f);
                for (int c = 0; c < 4; c++)
                {
                    int i = k * 4 + c;
                    starts[i] = s.From; pushes[i] = s.Push; colours[i] = colour; thrown[i] = new Vector2(s.At, s.Length);
                    corner[i] = new Vector3(c % 2 == 0 ? -1f : 1f, c < 2 ? 0f : 1f, s.Width);
                }
                uv[k * 4] = new Vector2(u0, v0); uv[k * 4 + 1] = new Vector2(u0, v0 + step); uv[k * 4 + 2] = new Vector2(u0 + step, v0); uv[k * 4 + 3] = new Vector2(u0 + step, v0 + step);
                int b = k * 4;
                triangles[k * 6] = b; triangles[k * 6 + 1] = b + 2; triangles[k * 6 + 2] = b + 1; triangles[k * 6 + 3] = b + 1; triangles[k * 6 + 4] = b + 2; triangles[k * 6 + 5] = b + 3;
            }
            _gritMesh.Clear();
            _gritMesh.vertices = starts; _gritMesh.normals = pushes; _gritMesh.colors = colours; _gritMesh.uv = uv;
            _gritMesh.SetUVs(1, thrown); _gritMesh.SetUVs(2, corner); _gritMesh.triangles = triangles;
            // The sparks fly where the vertices are not: the bounds are the reach of every arc, generously.
            _gritMesh.bounds = new Bounds((Hand + Target) * 0.5f, new Vector3(Mathf.Abs(Target.x - Hand.x) + 12f, 10f, Mathf.Abs(Target.z - Hand.z) + 12f));
        }

        // ------------------------------------------------------------------ the modelled stone

        /// <summary>The stone's baked mask (`tools/build_dante_rocks.py`): break distance, shoulder, occlusion, gaps.</summary>
        public const string Mask = "Models/AbilityFx/dante_boulder_mask";
        /// <summary>The bench's stand-ins (a shoe, a body): never drawn in a match.</summary>
        public const string Bench = "Models/AbilityFx/dante_boulder_bench";

        /// <summary>The rock material: `baked` reads the stone's baked mask (the rocks), without it a piece is flat
        /// palette slots (the bench's stand-ins).</summary>
        /// <summary>The stone that covers the whole shoe, for every view but his own first person
        /// (`tools/build_dante_rocks.py world`): six rocks, and a baked mask of its own, since its rocks press differently.</summary>
        public const string WorldModel = "Models/AbilityFx/dante_boulder_world", WorldMask = "Models/AbilityFx/dante_boulder_world_mask";

        public static Material Stone(bool baked = false, bool world = false)
        {
            var shader = Shader.Find(RockShader);
            if (shader == null) return null;
            if (_stone == null) _stone = new Material(shader) { name = "FxStudyRock", hideFlags = HideFlags.HideAndDontSave };
            if (_painted == null) _painted = new Material(shader) { name = "FxStudyRock-baked", hideFlags = HideFlags.HideAndDontSave };
            if (_covering == null) _covering = new Material(shader) { name = "FxStudyRock-baked-world", hideFlags = HideFlags.HideAndDontSave };
            var material = baked ? world ? _covering : _painted : _stone;
            // Set every time it is asked for, not once: a material forgets an array (it is not one of its saved
            // properties) whenever its shader is loaded again, and the stone then draws black.
            // An array is not converted for a linear project the way one colour is: hand it over converted.
            var linear = new List<Vector4>(16);
            foreach (var colour in Palette) linear.Add(colour.linear);
            material.SetVectorArray("_Palette", linear);
            var eaten = FxStudyKit.Mask("erode_cloud");
            if (eaten != null) material.SetTexture("_ErodeTex", eaten);
            var mask = baked ? Resources.Load<Texture2D>(world ? WorldMask : Mask) : null;
            if (mask != null) material.SetTexture("_MaskTex", mask);
            material.SetFloat("_Baked", mask != null ? 1f : 0f);
            // The edge wear's widths and lifts, said HERE and not left to the shader's defaults: a material that
            // already exists keeps the numbers it was made with when its shader's defaults are edited, so a board was
            // once filmed (and shown, and approved for filming) with widths the shader file no longer named.
            material.SetVector("_Band", new Vector4(0.006f, 0.022f, 0.38f, 0.06f));
            // How noisy the wear is: its shape all as first approved (grain and fray, swell, cuts in the line).
            material.SetVector("_Wear", new Vector4(1f, 1f, 1f, 0f));
            // ⚠️ KEEP. THE FINE NOISE TONED DOWN A LITTLE, PICKED BY THE OWNER 2026-10-17 off the three-column board
            // (`danteboulder_edge_wear_nobloomr1.jpg`): "use the toned down a little one". The fray read at 0.55 and the grain
            // at 0.50 of their approved frequency (broader strokes, fewer specks), the grain biting at 0.60. The band's
            // width and lift, its broken inner side, the line and its cuts are untouched. Both stones and the rubble use it.
            // Backed up in `tools/cutscene_rework/approved/danteboulder_c3/noise_2026-10-17/`.
            material.SetVector("_Noise", new Vector4(0.55f, 0.50f, 0.60f, 0f));
            material.SetVector("_Line", new Vector4(0.0015f, 0.0055f, 0.42f, 0.22f));
            material.SetFloat("_Reach", 0.04f);
            material.SetVector("_Key", KeyLight);
            return material;
        }

        private void BuildRocks()
        {
            // Every piece exists even when the model does not: a piece with nothing to draw is skipped, never missing.
            for (int i = 0; i < _lumps.Length; i++) _lumps[i] = new Rock();
            for (int i = 0; i < _chunks.Length; i++) _chunks[i] = new Rock();
            for (int i = 0; i < _crumbs.Length; i++) _crumbs[i] = new Rock();
            for (int i = 0; i < _lip.Length; i++) _lip[i] = new Rock();
            var source = Resources.Load<GameObject>(Model);
            var material = Stone();
            if (source == null || material == null)
            {
                Debug.LogWarning("[FxStudy] the boulder's stone is missing (" + (source == null ? Model + ": run tools/build_dante_boulder_fx.py" : RockShader) + "); it plays without it.");
                return;
            }
            var model = Object.Instantiate(source, Root, false);
            model.name = "stone";
            // The bench's stand-ins come from a file of their own and hang under the same root, so one search finds all.
            var standIns = Resources.Load<GameObject>(Bench);
            if (standIns != null) Object.Instantiate(standIns, model.transform, false).name = "stand-ins";
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.hideFlags = HideFlags.DontSave;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                // The stone reads its bake; the bench's stand-ins are flat colours.
                bool standIn = renderer.name == "shoe" || renderer.name == "body";
                renderer.sharedMaterial = standIn ? material : Stone(true);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            Transform Part(string name)
            {
                foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
                return null;
            }
            void Bind(Rock rock, Transform part, bool copy = false)
            {
                if (part == null) return;
                if (copy) { part = Object.Instantiate(part.gameObject, part.parent, false).transform; part.gameObject.hideFlags = HideFlags.DontSave; }
                rock.At = part; rock.Renderer = part.GetComponent<Renderer>(); rock.Seat = part.localPosition;
                var filter = part.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null) rock.Rest = filter.sharedMesh.bounds.extents.y;
                part.SetParent(Root, false);
            }
            for (int i = 0; i < _lumps.Length; i++) Bind(_lumps[i], Part(LumpNames[i]));
            for (int i = 0; i < _chunks.Length; i++) Bind(_chunks[i], Part("chunk" + i % 3), true);
            for (int i = 0; i < _crumbs.Length; i++) Bind(_crumbs[i], Part("chunk" + (i + 1) % 3), true);
            for (int i = 0; i < _lip.Length; i++) Bind(_lip[i], Part("chunk" + (i + 2) % 3), true);
            if (_standIns) { Bind(_shoe, Part("shoe")); Bind(_caster, Part("body"), true); Bind(_victim, Part("body")); }
            else foreach (var name in new[] { "shoe", "body" }) { var gone = Part(name); if (gone != null) gone.gameObject.SetActive(false); }
            for (int i = 0; i < 3; i++) { var pattern = Part("chunk" + i); if (pattern != null) pattern.gameObject.SetActive(false); }
        }

        // ------------------------------------------------------------------ where the boulder is

        // Its flight, by the share of it flown: a flat throw with a little arc.
        private Vector3 Path(float f) => Vector3.Lerp(Hand + new Vector3(-0.45f, 0.12f, 0f), Target, f) + Vector3.up * (0.34f * 4f * f * (1f - f));

        /// <summary>Where the shoe is at `age`, in the effect's own space: in the stone while it is whole, then dropped
        /// where the stone broke. A court film puts the real tsinelas here.</summary>
        public void ShoePose(float age, out Vector3 at, out Quaternion turn)
        {
            Boulder(age, out at, out turn, out _);
            if (age < HitAt) return;
            Boulder(HitAt, out Vector3 brokeAt, out Quaternion brokeTurn, out _);
            at = Thrown(brokeAt, new Vector3(-1.1f, 1.6f, -0.3f), age - HitAt, 0.03f * _fit, out _, out _);
            turn = Quaternion.Slerp(brokeTurn, Quaternion.Euler(0f, 60f, 0f), FxStudyKit.Ease(FxStudyKit.Run(age - HitAt, 0.25f, 0.60f)));
        }

        private void Boulder(float age, out Vector3 at, out Quaternion turn, out float scale)
        {
            // Toe toward +x, the way it will be thrown.
            var held = Quaternion.Euler(0f, 90f, 0f);
            scale = 1f;
            if (age < ThrowAt)
            {
                // Carried: a slow sway, so the bench sees it from more than one side. Then pulled back and up to throw.
                float pull = FxStudyKit.EaseOut(FxStudyKit.Run(age, WindAt, ThrowAt - 0.04f));
                at = Hand + new Vector3(0f, 0.022f * Mathf.Sin(age * 2.3f), 0f) + new Vector3(-0.45f, 0.12f, 0f) * pull;
                turn = Quaternion.Euler(7f * Mathf.Sin(age * 1.4f), 26f * Mathf.Sin(age * 1.05f - 0.6f), -22f * pull) * held;
                // Every lump that lands knocks it: away from the side it came on, and back.
                for (int i = 0; i < _lumps.Length; i++)
                {
                    float since = age - Arrive[i];
                    if (since <= 0f) continue;
                    Vector3 from = turn * _lumps[i].Seat;
                    at -= from.normalized * ((i == _lumps.Length - 1 ? 0.060f : 0.034f) * Mathf.Exp(-13f * since) * Mathf.Cos(31f * since));
                }
                // And it rings once when the last one is home: bigger, then back, and still.
                float rung = age - SnapAt;
                if (rung > 0f) scale = 1f + 0.13f * Mathf.Exp(-9f * rung) * Mathf.Cos(27f * rung);
                return;
            }
            float f = FxStudyKit.Run(age, ThrowAt, HitAt);
            at = Path(f);
            turn = Quaternion.AngleAxis(-560f * f, Vector3.forward) * Quaternion.Euler(0f, 0f, -22f) * held;
        }

        // A piece thrown from `from` with `push` (m/s), `t` seconds on: an arc, one short hop where it comes down, then
        // still. `settled` is 0 in the air and 1 once it lies.
        private static Vector3 Thrown(Vector3 from, Vector3 push, float t, float rest, out float flown, out float settled)
        {
            float drop = Mathf.Max(0f, from.y - rest);
            float land = (push.y + Mathf.Sqrt(push.y * push.y + 2f * Gravity * drop)) / Gravity;
            flown = Mathf.Min(t, land);
            var flat = new Vector3(push.x, 0f, push.z);
            Vector3 at = from + flat * flown + Vector3.up * (push.y * flown - 0.5f * Gravity * flown * flown);
            const float hop = 0.24f;
            float h = FxStudyKit.Run(t, land, land + hop);
            settled = FxStudyKit.Ease(h);
            if (t > land)
            {
                at += flat * (0.16f * FxStudyKit.EaseOut(h));
                at.y = rest + 0.11f * 4f * h * (1f - h);
            }
            return at;
        }

        private static void PoseThrown(Rock rock, Throw thrown, Vector3 from, Quaternion fromTurn, float t, float fit = 1f)
        {
            if (rock.At == null) return;
            // It is EATEN where it lies (the shader's cloud map at a rising cut): holes, then flakes, then nothing.
            float gone = FxStudyKit.Run(t, thrown.Gone, thrown.Gone + 0.60f);
            if (t < 0f || gone >= 0.999f) { rock.Show(false); return; }
            Vector3 at = Thrown(from, thrown.Push, t, rock.Rest * thrown.Size * fit, out float flown, out float settled);
            var tumbling = Quaternion.Euler(thrown.Spin * flown * 0.8f, thrown.Spin * flown, thrown.Spin * flown * 0.3f) * fromTurn;
            rock.Show(true);
            rock.At.localPosition = at;
            rock.At.localRotation = Quaternion.Slerp(tumbling, Quaternion.Euler(0f, thrown.Yaw, 0f), settled);
            rock.At.localScale = Vector3.one * (thrown.Size * fit);
            // Its cracks go out as it flies.
            rock.Dress(Mathf.Exp(-5f * t), 0f, 0.008f * Mathf.Min(1f, thrown.Size) * (1f - gone), gone);
        }

        // The nine puffs: where each is, which way its dust goes (its own up), how big, and when it is fired. All fixed
        // when the effect is built.
        private void BuildDust()
        {
            // His dust: a warm tan, its shadow, and the line a darker tone of the same. Opaque, as the cloud is.
            Color lit = new Color(0.80f, 0.70f, 0.54f), shade = new Color(0.62f, 0.51f, 0.38f), lineTone = new Color(0.42f, 0.33f, 0.24f);
            void Place(int i, float born, Vector3 at, Vector3 way, float size)
            {
                // Only the dust of the moments this copy plays is made at all.
                if ((_parts & (i < 4 ? PartCast : i < 7 ? PartFlight : PartHit)) == 0) return;
                // Made already (this copy is being placed again): moved, nothing new made.
                if (_puffHolder[i] != null)
                {
                    _puffHolder[i].localPosition = at; _puffHolder[i].localRotation = Quaternion.FromToRotation(Vector3.up, way.normalized); _puffHolder[i].localScale = Vector3.one * size;
                    _puffBorn[i] = born;
                    return;
                }
                // SIZED BY ITS PARENT (the cloud's own rule): a puff is about 1.5 m across before it is scaled, so 0.20 is a puff of 30 cm, its biggest ball 12 cm through the middle.
                var holder = Group("dust-" + i);
                holder.localPosition = at;
                holder.localRotation = Quaternion.FromToRotation(Vector3.up, way.normalized);
                holder.localScale = Vector3.one * size;
                // Three arrangements of balls among the nine, so neighbours differ; each arrangement is one shared mesh.
                var cloud = FxCloud.Build(holder, "cloud", 3 + i % 3, PuffBalls, lit, shade, lineTone, FxStudyCloud.Line);
                var material = cloud.Piece.Material;
                if (material != null)
                {
                    material.SetVector("_Key", KeyLight);
                    // Its material is this effect's to free with the rest: handed to a piece that is never drawn. (Making
                    // a piece moves a material's place in the drawing order, so the cloud's own is put back.)
                    Add("dust-owner-" + i, FxStudyKit.Square(false), material, 0).Frame(0f, 0f);
                    material.renderQueue = 2501;
                }
                cloud.Piece.Go.SetActive(false);
                _puff[i] = cloud; _puffBorn[i] = born; _puffHolder[i] = holder;
            }
            // Where each rock lands: below the stone and ahead of it, and thrown FORWARD AND DOWN. The stone is held in
            // front of him, so dust thrown up or back from it is dust in his own eyes: the first of these, thrown up and
            // toward him, put one ball across half his view as the big rock landed. The big one's is a third larger.
            for (int i = 0; i < _lumps.Length; i++)
            {
                Boulder(Arrive[i], out Vector3 thenAt, out Quaternion thenTurn, out _);
                Vector3 seat = thenTurn * (_lumps[i].Seat * _fit);
                var side = new Vector3(0f, 0f, seat.z >= 0f ? 1f : -1f);
                Place(i, Arrive[i], thenAt + new Vector3(0.16f, -0.14f, seat.z * 0.8f), Vector3.right * 0.9f + Vector3.down * 0.5f + side * 0.5f, i == _lumps.Length - 1 ? 0.20f : 0.15f);
            }
            // ⚠️ IN FLIGHT: BEHIND AND BELOW THE STONE, AND NOT NEAR HIS HAND. The flat puffs were shed from the hand onward
            // and the first sat in front of the thrower's own eyes. These start three tenths of the way out, hang under the
            // path, and drift back.
            for (int k = 0; k < 3; k++)
            {
                float f = 0.30f + 0.22f * k;
                Place(4 + k, ThrowAt + f * Flight + 0.03f, Path(f) + new Vector3(-0.10f, -0.16f, 0f), new Vector3(-0.8f, 0.35f, k == 1 ? 0.3f : -0.3f), 0.15f);
            }
            // ⚠️ AT THE HIT: DOWN AND OUT, SMALL, AND NOWHERE NEAR HIS EYES. Born a quarter of a metre under the contact and
            // toward the thrower, thrown down and to either side: more than half a metre from the eye line of the player hit.
            for (int k = 0; k < 2; k++)
                Place(7 + k, HitAt + 0.01f * k, Target + new Vector3(-0.22f, -0.26f, k == 0 ? -0.16f : 0.16f), new Vector3(-0.5f, -0.45f, k == 0 ? -0.8f : 0.8f), 0.30f);
        }

        // ------------------------------------------------------------------ one frame

        public override void Sample(float age, Vector3 eye)
        {
            Boulder(age, out Vector3 at, out Quaternion turn, out float ring);
            Boulder(HitAt, out Vector3 brokeAt, out Quaternion brokeTurn, out _);
            float sinceSnap = age - SnapAt, sinceHit = age - HitAt;
            bool whole = age < HitAt;

            // ---- the lumps: heaved out of the split, then they fall INTO their seats; after the hit, thrown.
            for (int i = 0; i < _lumps.Length; i++)
            {
                var lump = _lumps[i];
                if (lump.At == null) continue;
                // Whole, the rocks are this effect's only when it plays everything (in a match they are on the real shoe);
                // broken, they are the hit's.
                if (whole ? _parts != PartAll : (_parts & PartHit) == 0) { lump.Show(false); continue; }
                if (!whole) { PoseThrown(lump, LumpThrows[i], brokeAt + brokeTurn * (lump.Seat * _fit), brokeTurn, sinceHit, _fit); continue; }
                float u = FxStudyKit.Run(age, Leave[i], Arrive[i]);
                if (age < Leave[i]) { lump.Show(false); continue; }
                lump.Show(true);
                Vector3 seat = at + turn * (lump.Seat * (ring * _fit));
                // Out of the floor along the split; over a point above and to one side; down into the seat.
                float spread = (i - 2f) / 2f;
                var from = new Vector3(Hand.x + 0.70f * spread, -0.14f, 0.26f * Mathf.Sin(i * 2.4f + 1f));
                Vector3 over = Vector3.Lerp(from, seat, 0.45f) + new Vector3(0.34f * Mathf.Sin(i * 1.9f), 0.50f + 0.10f * i, 0.38f * Mathf.Cos(i * 1.7f));
                // A slow heave first (an eighth of the way in a third of the time), then it speeds up all the way in.
                float p = u < 0.34f ? 0.13f * FxStudyKit.Ease(u / 0.34f) : 0.13f + 0.87f * FxStudyKit.EaseIn((u - 0.34f) / 0.66f);
                lump.At.localPosition = Vector3.Lerp(Vector3.Lerp(from, over, p), Vector3.Lerp(over, seat, p), p);
                lump.At.localRotation = Quaternion.Euler(140f * (1f - p) * (i % 2 == 0 ? 1f : -1f), 220f * (1f - p), 90f * (1f - p) * Mathf.Sin(i * 1.3f)) * turn;
                // It lands a little big and is pressed home.
                float landed = FxStudyKit.EaseOut(FxStudyKit.Run(age, Arrive[i], Arrive[i] + 0.16f));
                lump.At.localScale = Vector3.one * (_fit * Mathf.Lerp(0.60f, 1f, FxStudyKit.Ease(u)) * (u >= 1f ? Mathf.Lerp(1.16f, 1f, landed) * ring : 1f));
                // The cracks: dead until the crown is home, then lit from the crown outward; a flash, then breathing.
                float lit = sinceSnap - 0.035f * (_lumps.Length - 1 - i);
                float heat = lit < 0f ? 0f : Mathf.Min(1f, lit / 0.05f) * (0.88f + 0.12f * Mathf.Sin(age * 5.2f + i)) + 1.6f * Mathf.Exp(-6f * lit) + (age > ThrowAt ? 0.3f : 0f);
                lump.Dress(heat, 0.45f * (u >= 1f ? 1f - landed : 0f), 0.008f);
            }

            // ---- the split's lip: three small stones thrown up where it tears, sat on its edge, eaten as it closes.
            for (int i = 0; i < _lip.Length; i++)
            {
                var stone = _lip[i];
                if (stone.At == null) continue;
                if ((_parts & PartCast) == 0) { stone.Show(false); continue; }
                float up = FxStudyKit.Pop(FxStudyKit.Run(age, CastAt - 0.04f + 0.03f * i, CastAt + 0.14f + 0.03f * i), 0.25f);
                float eaten = FxStudyKit.Run(age, 1.50f + 0.08f * i, 2.00f + 0.08f * i);
                stone.Show(age > CastAt - 0.04f + 0.03f * i && eaten < 0.999f);
                float size = LipSizes[i];
                var along = Quaternion.Euler(0f, 22f, 0f) * new Vector3(-0.62f + 0.58f * i, 0f, i == 1 ? 0.20f : -0.17f);
                stone.At.localPosition = new Vector3(Hand.x, stone.Rest * size * (up - 0.35f), 0f) + along;
                stone.At.localRotation = Quaternion.Euler(12f * (i - 1), 70f * i + 20f, 9f);
                stone.At.localScale = Vector3.one * (size * Mathf.Max(0.01f, up));
                stone.Dress(0f, 0f, 0f, eaten);
            }

            // ---- rubble: what the crown knocks off as it lands, and what the burst throws besides the lumps.
            for (int i = 0; i < _crumbs.Length; i++)
                PoseThrown(_crumbs[i], CrumbThrows[i], Hand + new Vector3(0.14f * (i - 1), -0.12f, 0f), Quaternion.identity, (_parts & PartCast) != 0 ? sinceSnap - 0.03f * i : -1f);
            for (int i = 0; i < _chunks.Length; i++)
                PoseThrown(_chunks[i], ChunkThrows[i], brokeAt, brokeTurn, (_parts & PartHit) != 0 ? sinceHit - 0.012f * i : -1f);

            // ---- the bench's stand-ins: the shoe the stone grips (it drops when the stone breaks), and the two bodies.
            if (_shoe.At != null)
            {
                ShoePose(age, out Vector3 shoeAt, out Quaternion shoeTurn);
                _shoe.At.localPosition = shoeAt;
                _shoe.At.localRotation = shoeTurn;
                _shoe.Dress(0f, 0f, 0.006f);
            }
            if (_caster.At != null) { _caster.At.localPosition = new Vector3(Hand.x - 0.10f, 0f, 0.62f); _caster.Dress(0f, 0f, 0.010f); }
            if (_victim.At != null)
            {
                // He is knocked: a lean away and back, and the faintest blink.
                float knock = sinceHit > 0f ? Mathf.Exp(-5f * sinceHit) * Mathf.Cos(13f * sinceHit) : 0f;
                _victim.At.localPosition = new Vector3(Target.x + 0.36f, 0f, 0f);
                _victim.At.localRotation = Quaternion.Euler(0f, 0f, -11f * knock);
                _victim.Dress(0f, 0f, 0.010f);
            }

            // ---- the split: torn open along its length, popped wide, its seam cooling, then it closes like a mouth.
            float torn = FxStudyKit.EaseOut(FxStudyKit.Run(age, CastAt - 0.08f, CastAt + 0.10f));
            float gape = FxStudyKit.Pop(FxStudyKit.Run(age, CastAt - 0.08f, CastAt + 0.16f), 0.3f) * (1f - 0.78f * FxStudyKit.Ease(FxStudyKit.Run(age, 1.45f, 1.95f)));
            float cooled = FxStudyKit.Ease(FxStudyKit.Run(age, SnapAt + 0.05f, 1.55f));
            _split.At.localScale = new Vector3(2.5f * Mathf.Lerp(0.30f, 1f, torn), 1f, 1.25f * gape);
            _split.Set("_Stops", 0.40f, Mathf.Lerp(0.62f, 1.3f, cooled), Mathf.Lerp(0.90f, 1.7f, cooled));
            _split.Frame(age, age < CastAt - 0.08f || (_parts & PartCast) == 0 ? 0f : 1f, FxStudyKit.Run(age, 1.80f, 2.10f));
            _splitGlow.At.localScale = new Vector3(2.3f * torn, 1f, 0.9f * gape);
            _splitGlow.Frame(age, age < CastAt - 0.08f || (_parts & PartCast) == 0 ? 0f : 0.45f * _light * (1f - cooled));

            // ---- dust: each puff is on from the moment it is fired until its last ball has shrunk away.
            for (int i = 0; i < Puffs; i++)
            {
                var cloud = _puff[i];
                if (cloud == null) continue;
                float t = age - _puffBorn[i];
                bool on = t >= 0f && t <= PuffOn + PuffLife;
                if (cloud.Piece.Go.activeSelf != on) cloud.Piece.Go.SetActive(on);
                if (on) cloud.Pose(t, PuffEmitter);
            }

            // ---- light: a blink as the cracks come alight, and at the hit a blink and a drawn star at the contact.
            _snapGlow.At.localPosition = at; _snapGlow.At.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.4f, FxStudyKit.EaseOut(FxStudyKit.Run(sinceSnap, 0f, 0.14f)));
            _snapGlow.Face(eye); _snapGlow.Frame(age, ((_parts & PartCast) != 0 ? 0.45f : 0f) * _light * FxStudyKit.Envelope(sinceSnap, -0.02f, 0.03f, 0.05f, 0.20f));
            _hitGlow.At.localPosition = Target + new Vector3(-0.30f, 0f, 0f); _hitGlow.At.localScale = Vector3.one * Mathf.Lerp(0.4f, 0.9f, FxStudyKit.EaseOut(FxStudyKit.Run(sinceHit, 0f, 0.12f)));
            _hitGlow.Face(eye); _hitGlow.Frame(age, ((_parts & PartHit) != 0 ? 0.25f : 0f) * _light * FxStudyKit.Envelope(sinceHit, -0.01f, 0.02f, 0.05f, 0.16f));
            // (Toward the thrower from the contact and half the size it was: laid over the body it tinted the whole player yellow.)
            _star.At.localPosition = Target + new Vector3(-0.34f, 0f, -0.10f);
            _star.At.localScale = Vector3.one * (1.5f * FxStudyKit.Pop(FxStudyKit.Run(sinceHit, 0f, 0.09f)));
            _star.Face(eye); _star.At.localRotation *= Quaternion.Euler(0f, 0f, 18f);
            _star.Frame(age, sinceHit < 0f || (_parts & PartHit) == 0 ? 0f : 1f, FxStudyKit.Run(sinceHit, 0.06f, 0.22f));

            // ---- grit: a little shaken off as the crown lands, a few chips shed in flight, more thrown by the burst. Every
            // spark is an arc worked out when the effect was placed (`BuildGrit`); the GPU poses it from the clock. Shown while
            // any spark is in the air, exactly when the flecks used to have one to draw.
            bool flying = false;
            foreach (var s in _sparks)
            {
                float st = age - s.At;
                if (st > 0f && s.From.y + s.Push.y * st - 0.5f * Gravity * st * st >= 0.02f) { flying = true; break; }
            }
            _grit.Frame(age, flying ? 1f : 0f);
        }
    }
}
