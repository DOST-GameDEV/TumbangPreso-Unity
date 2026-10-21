using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// MOUNTAIN FORM, the mountain's armour CONTINENTAL DRIFT leaves on him (owner, 2026-10-17: *"AFTER HIS ULT he retains the rock
    /// armor for 30 secs"*, *"give him armor all oer his body"*, *"make sure the armor looks good with his horn"*). He is from
    /// Montalban, where Bernardo Carpio is held between the mountains and the earth shakes when he strains: the ult wakes
    /// that mountain in Dante, and it closes over him.
    ///
    /// THE DESIGN (owner, 2026-10-18, over eleven turnarounds in `Logs/dante-armour`): a BULK SUIT of columnar basalt, Toph's
    /// earth suit as the measure of bulk but not of look. Every piece rides one of his rigid bones, placed from that bone's own
    /// rest-pose vertices, so it moves with every clip and never stretches:
    ///  * the torso, a breastplate over a belly plate, a back plate and side plates, a collar that leaves the face bare, a belt
    ///    of blocks with one molten line through it, and two layers of tassets past the coat's hem; a range of columns off his back;
    ///  * the shoulders, a domed pauldron and a hanging plate outside the head box, and a range of columns over the horn's side;
    ///  * the arms, a bracer on each bone and a great boulder fist; the legs, thigh bracers, greaves, boots and toe caps;
    ///  * the head keeps his face and hair bare (owner: no rocks on his hair); his obsidian horn gets molten veins and a basalt
    ///    collar, and its exact mirror in stone grows on his right temple (`MirroredHorn`); his gold eye burns.
    /// Every plate is split by one molten seam along it, and the columns are cut through to the core: the suit glows along its lines.
    /// ONE CLOCK (`StepTo`), the cutscene's: pieces arrive on the charge-up's beats, complete at `FormedAt`, hold, then over the
    /// last `ShedSeconds` come off one at a time onto the court, cool and sink in.
    /// Hidden from his own first-person view (`DanteGauntletHands` draws his stone hands there). Presentation only.
    /// </summary>
    public sealed class DanteMountainArmour : MonoBehaviour
    {
        /// <summary>The cutscene second every piece is on him: the end of THE RISE, just before THE LEAP.</summary>
        public const float FormedAt = 2.0f;
        /// <summary>The cutscene's last second (`HeroIntroductionScene.Dante`'s DnEnd): play takes the armour's clock from here.</summary>
        public const float CutEndAt = 3.33f + GeoRules.DriftCutsceneTail;
        public const float FlyTime = .26f;
        /// <summary>
        /// THE SHEDDING (owner, 2026-10-18: *"as ult nears end (starting at 20 seconds) parts of it falll off until at 30 seconds its
        /// all compltely fallen off"*): over its last `ShedSeconds` the suit comes off a stone at a time. Each loosens (a tremble),
        /// drops off him, tumbles to the court where he was and stays there, its lava cooling, then sinks into the court
        /// (`DebrisSeconds` after it dropped). Crown and shoulders first, the stone horn last.
        /// </summary>
        public const float ShedSeconds = 10f, LoosenSeconds = .45f, DebrisSeconds = 1.6f;
        // The charge-up's beats (`HeroIntroductionScene.Dante.cs`): the three waves of stone land at .84, 1.14 and 1.44.
        // ⚠️ THE LEGS COME AS HE STANDS (1.56 to 1.8), NOT WHILE HE KNEELS: plated during the plant, his shins lie on the court and
        // their plates went 5 cm into it (`UltimateIntroductionProbe`'s support plane). The earth climbs him boots first as he rises.
        private const float FistsAt = .84f, BracersAt = 1.14f, ShouldersAt = 1.44f, LegsAt = 1.62f, ChestAt = 1.28f, HeadAt = 1.70f;

        private enum Arrive { Fly, Rise, Grow }

        private sealed class Piece
        {
            public Transform Node, Bone;
            public Vector3 Rest, Normal, Scale;
            public Quaternion Turn;
            public float LockAt;
            public Arrive How;
            public Material Seam;
            public int Seed;
            public bool Uniform;    // grows evenly from its root (the stone horn), not along its normal
            public float DropAt;    // when it comes off, on the clock (set from its order once `EndAt` is known)
            public float Order;     // 0 the first to come off, 1 the last
            public bool Dropped;    // off his body, lying where it fell
            public Vector3 FromWorld, Out, LyingScale; public Quaternion FromTurn; public float Ground;
        }

        private struct Frame
        {
            public Transform Bone;
            public Vector3 R, U, F, Min, Max;
            public Vector3 C => (Min + Max) * .5f;
            public Vector3 E => (Max - Min) * .5f;
            public Vector3 Local(Vector3 rest) => R * rest.x + U * rest.y + F * rest.z;
            public Vector3 LocalDir(Vector3 rest) => (R * rest.x + U * rest.y + F * rest.z).normalized;
            public Quaternion Basis => Quaternion.LookRotation(F, U);
        }

        private static readonly Color Hot = new Color(1f, .38f, .05f), Cooled = new Color(.55f, .16f, .06f) * .55f;

        private readonly List<Piece> _pieces = new List<Piece>(48);
        private readonly List<DanteDriftLive.Glow> _veins = new List<DanteDriftLive.Glow>(4);
        private readonly List<(Vector3 from, Vector3 to)> _veinPath = new List<(Vector3, Vector3)>(2);
        private DanteDriftLive.Glow _eye;
        private Vector3 _eyeAt;
        private CharacterMotor _owner;
        private Transform _worldRoot;
        private DanteWardBadge _badge;
        private Renderer[] _renderers;
        private bool _visible;
        private float _endAt = FormedAt + GeoRules.MountainSeconds;

        /// <summary>The clock second it is gone by (the crumble's end).</summary>
        public float EndAt { get => _endAt; set => _endAt = value; }
        /// <summary>When the last of its debris has sunk into the court: the armour may be removed after this.</summary>
        public float GoneAt => _endAt + DebrisSeconds;
        private Transform _debris;
        public float Age { get; private set; }
        public IReadOnlyList<Renderer> Renderers => _renderers;
        public int PieceCount => _pieces.Count;
        /// <summary>
        /// ⚠️ THE BODY IT WAS BUILT ON IS GONE: his model was rebuilt under it (`CharacterVisual.ApplyModel`, a skin or a roster
        /// refresh mid-form), so every stone went with the old bones and the veins' bone is a destroyed transform. Nothing here
        /// can be moved onto the new rig; the kit throws this suit away and dresses the new body (`DanteHeroKit.Tick`).
        /// </summary>
        public bool Orphaned => _body == null || _pieces.Exists(p => p.Node == null);
        private SkinnedMeshRenderer _body;

        /// <summary>On his live body.</summary>
        public static DanteMountainArmour Attach(CharacterMotor owner)
        {
            var visual = owner != null ? owner.GetComponent<CharacterVisual>() : null;
            return visual != null && visual.Model != null ? Build(visual.Model, owner.transform, owner) : null;
        }

        /// <summary>On any copy of his model (the cutscene's): <paramref name="worldRoot"/> is the body's root, facing his front.</summary>
        public static DanteMountainArmour Build(GameObject model, Transform worldRoot, CharacterMotor owner = null)
        {
            SkinnedMeshRenderer skin = null;
            foreach (var s in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (s.name == "body-mesh" && s.sharedMesh != null) { skin = s; break; }
            if (skin == null) return null;
            var go = new GameObject("DanteMountainArmour");
            go.transform.SetParent(worldRoot, false);
            var armour = go.AddComponent<DanteMountainArmour>();
            armour._owner = owner; armour._worldRoot = worldRoot; armour._body = skin;
            armour.Make(skin, model);
            if (owner != null) armour._badge = DanteWardBadge.Create(worldRoot, owner, UI.AbilityGlyph.DanteFissure);
            armour.StepTo(0f);
            return armour;
        }

        private int BoneIndex(SkinnedMeshRenderer skin, string name)
        {
            for (int i = 0; i < skin.bones.Length; i++) if (skin.bones[i] != null && skin.bones[i].name == name) return i;
            return -1;
        }

        private SkinnedMeshRenderer[] _skins;

        /// <summary>The skin that carries <paramref name="name"/>'s vertices (his head is its own mesh) and the bone's index in it.</summary>
        private bool FindBone(string name, out SkinnedMeshRenderer skin, out int bone)
        {
            skin = null; bone = -1; int best = 0;
            foreach (var s in _skins)
            {
                int i = BoneIndex(s, name); if (i < 0) continue;
                int count = 0; foreach (var w in s.sharedMesh.boneWeights) if (w.boneIndex0 == i && w.weight0 >= .99f) count++;
                if (count > best) { best = count; skin = s; bone = i; }
            }
            return skin != null;
        }

        /// <summary>
        /// A bone's rest points in its rest frame (his right, up and front, from the bind pose): those carried by it alone, or with
        /// <paramref name="minWeight"/> below 1 also those it shares (his coat's tail is blended between the torso and the legs).
        /// </summary>
        private Frame Basis(SkinnedMeshRenderer skin, int bone, out List<Vector3> points, float minWeight = .99f)
        {
            var mesh = skin.sharedMesh;
            var bind = mesh.bindposes[bone];
            var up = skin.transform.InverseTransformDirection(_worldRoot.up);
            var front = skin.transform.InverseTransformDirection(_worldRoot.forward);
            var f = new Frame { Bone = skin.bones[bone] };
            f.U = bind.MultiplyVector(up).normalized;
            f.F = bind.MultiplyVector(front); f.F = (f.F - Vector3.Dot(f.F, f.U) * f.U).normalized;
            f.R = Vector3.Cross(f.U, f.F);
            var vertices = mesh.vertices; var weights = mesh.boneWeights;
            points = new List<Vector3>(vertices.Length / 4);
            for (int i = 0; i < vertices.Length; i++)
            {
                var w = weights[i];
                float weight = (w.boneIndex0 == bone ? w.weight0 : 0) + (w.boneIndex1 == bone ? w.weight1 : 0)
                             + (w.boneIndex2 == bone ? w.weight2 : 0) + (w.boneIndex3 == bone ? w.weight3 : 0);
                if (weight < minWeight) continue;
                var q = bind.MultiplyPoint3x4(vertices[i]);
                points.Add(new Vector3(Vector3.Dot(q, f.R), Vector3.Dot(q, f.U), Vector3.Dot(q, f.F)));
            }
            return f;
        }

        /// <summary>
        /// The bounds of the points <paramref name="keep"/> keeps, by the 3rd and 97th percentile on each axis: the body's own
        /// surface, not what stands off it (the buckle, the collar's points, a hair spike), which a plate would float over.
        /// </summary>
        private static Frame Bounds(Frame f, List<Vector3> points, System.Func<Vector3, bool> keep)
        {
            var xs = new List<float>(points.Count); var ys = new List<float>(points.Count); var zs = new List<float>(points.Count);
            foreach (var p in points) if (keep == null || keep(p)) { xs.Add(p.x); ys.Add(p.y); zs.Add(p.z); }
            if (xs.Count == 0) return f;
            xs.Sort(); ys.Sort(); zs.Sort();
            float Q(List<float> v, float q) => v[Mathf.Clamp(Mathf.RoundToInt(q * (v.Count - 1)), 0, v.Count - 1)];
            f.Min = new Vector3(Q(xs, .03f), Q(ys, .03f), Q(zs, .03f));
            f.Max = new Vector3(Q(xs, .97f), Q(ys, .97f), Q(zs, .97f));
            if (Debug.isDebugBuild && Application.isEditor)
                Debug.Log($"[Mountain] frame {f.Bone.name} min{f.Min:F3} max{f.Max:F3} from {xs.Count}");
            return f;
        }

        /// <summary>The bone's full extent (every vertex): where the coat's hem and the shoes' toes reach.</summary>
        private static Frame Whole(Frame f, List<Vector3> points)
        {
            if (points.Count == 0) return f;
            f.Min = f.Max = points[0];
            foreach (var p in points) { f.Min = Vector3.Min(f.Min, p); f.Max = Vector3.Max(f.Max, p); }
            return f;
        }

        private void Make(SkinnedMeshRenderer body, GameObject model)
        {
            // ⚠️ THIS MODEL'S SKINS ONLY, NEVER THE BODY ROOT'S. A model swapped in the same frame the form begins
            // (`CharacterVisual.ApplyModel` destroys the old one at the frame's end) left both under the root, and the old
            // head won `FindBone`'s tie: the horn's veins were lit on a bone that was gone a frame later.
            _skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (_skins.Length == 0) _skins = new[] { body };
            if (FindBone("torso", out var s, out int b)) { var f = Basis(s, b, out var pts); var core = Bounds(f, pts, null); Basis(s, b, out var coat, .2f); Torso(core, Whole(f, coat)); Shoulders(core); }
            foreach (var side in new[] { "left", "right" })
            {
                if (FindBone("arm-" + side, out s, out b)) { var f = Basis(s, b, out var pts); UpperArm(Bounds(f, pts, null)); }
                if (FindBone("forearm-" + side, out s, out b)) { var f = Basis(s, b, out var pts); Forearm(Bounds(f, pts, null)); }
                // The leg without its shoe: the shin is what the plates sit on.
                if (FindBone("leg-" + side, out s, out b))
                {
                    var f = Basis(s, b, out var pts); var whole = Bounds(f, pts, null);
                    float shoe = whole.Min.y + (whole.Max.y - whole.Min.y) * .28f;
                    Leg(Bounds(f, pts, p => p.y > shoe), whole, Whole(f, pts).Min.y);
                }
            }
            if (FindBone("head", out s, out b)) Head(s, b);
            var all = new List<Renderer>();
            foreach (var p in _pieces) all.AddRange(p.Node.GetComponentsInChildren<Renderer>(true));
            foreach (var g in _veins) all.Add(g.Renderer);
            if (_eye != null) all.Add(_eye.Renderer);
            _renderers = all.ToArray();
        }

        // ------------------------------------------------------------------ the pieces
        //
        // COLUMNAR BASALT (owner on the first armour: *"ang panget ng armor design mo"*, *"make smth comletley new nna itsura"*):
        // the six-sided columns lava cracks into as it cools. Geometric, so it belongs on the game's block bodies; volcanic, so
        // it is the same earth as the cascade. Every stone is a chamfered hex column cut through once, with the molten core
        // showing only in that cut: light inside the stone, never an outline round it.

        // The cutscene's own basalt (`HeroIntroductionScene.Dante`'s gauntlet stones), so the suit he forms in the cut is the suit he
        // wears in play: warm dark stone, not a neutral grey (which beside his orange seams read cold and flat).
        // Kept darker than the cut's stones (`Logs/mf/poses_v4`: at the cut's own values the suit sat in the court's tan).
        private static readonly Color Basalt = new Color(.23f, .19f, .17f), BasaltDark = new Color(.16f, .13f, .115f), BasaltPale = new Color(.3f, .25f, .21f);
        private static readonly Color Lava = new Color(1f, .43f, .08f, .92f);

        private static void Dress(Transform node, Color colour)
        {
            var renderer = node.GetComponent<Renderer>();
            MaterialKit.Dress(renderer, colour);
            ToonSkin.Apply(renderer, .0035f);
            VfxRenderTag.Attach(node.gameObject);
        }

        private static Material Core(Transform parent, int sides, float y, float radius, float height, int seed)
        {
            var core = VfxShapes.Stand(parent, "MoltenCore", VfxShapes.Prism(sides, 1f, 1f, 0f, 0f, seed), 1);
            core.transform.localPosition = new Vector3(0f, y, 0f);
            core.transform.localScale = new Vector3(radius, height, radius);
            var r = core.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            VfxMaterial.Ghost(r, Lava, .55f);
            return r.sharedMaterial;
        }

        /// <summary>
        /// A basalt column standing out of the bone along <paramref name="normal"/>: a short foot, the cut (the molten core), and the
        /// shaft, chamfered and slightly narrower at its top. <paramref name="cut"/> is where along it the cut is (0 to 1).
        /// </summary>
        private Piece Column(Frame f, string role, Vector3 rest, Vector3 normal, float radius, float height, float lockAt, Arrive how,
                             int seed, Color colour, float cut = .3f, float top = .86f, float twist = 0f, int sides = 6)
        {
            var node = new GameObject(role).transform;
            node.SetParent(f.Bone, false);
            const float gap = .07f;
            var foot = VfxShapes.Stand(node, "Foot", VfxShapes.Prism(sides, 1f, .98f, .04f, 0f, seed), 1).transform;
            foot.localScale = new Vector3(1f, cut, 1f);
            Dress(foot, colour * .9f);
            var shaft = VfxShapes.Stand(node, "Shaft", VfxShapes.Prism(sides, 1f, top, .06f, 0f, seed + 1), 1).transform;
            shaft.localPosition = new Vector3(0f, cut + gap, 0f);
            shaft.localScale = new Vector3(.97f, Mathf.Max(.05f, 1f - cut - gap), .97f);
            shaft.localRotation = Quaternion.Euler(0f, 7f + seed % 5 * 3f, 0f);
            Dress(shaft, colour);
            var seam = Core(node, sides, cut - .01f, .9f, gap + .02f, seed + 2);
            var piece = Keep(f, node, rest, normal, new Vector3(radius, height, radius), lockAt, how, seam, twist);
            piece.Seed = seed;
            return piece;
        }

        /// <summary>A flat faceted slab lying on the bone, its face along <paramref name="normal"/>, split by a molten groove.</summary>
        private Piece Slab(Frame f, string role, Vector3 rest, Vector3 normal, Vector3 size, float lockAt, Arrive how, int seed, Color colour, float twist = 0f)
        {
            var node = new GameObject(role).transform;
            node.SetParent(f.Bone, false);
            foreach (float side in new[] { -1f, 1f })
            {
                var half = VfxShapes.Stand(node, side < 0 ? "SlabA" : "SlabB", VfxShapes.Prism(4, 1f, .9f, .05f, .785f, seed + (side < 0 ? 0 : 1)), 1).transform;
                // ⚠️ APART, SO THE SEAM SHOWS. At .26 the two halves (each .35 wide) overlapped and covered the groove: on the body
                // the seams read as orange specks (`Logs/mf/poses_v3`). At .38 there is a cut between them, narrow at the plate's
                // foot and opening a little to its face (the halves taper), and the molten groove fills it: a line along the plate.
                half.localPosition = new Vector3(side * .38f, 0f, side * .02f);
                half.localScale = new Vector3(.48f, 1f, .98f);
                half.localRotation = Quaternion.Euler(0f, side * 4f, 0f);
                Dress(half, side < 0 ? colour : colour * 1.08f);
            }
            var groove = VfxShapes.Stand(node, "MoltenGroove", VfxShapes.Prism(4, 1f, 1f, 0f, .785f, seed + 2), 1);
            groove.transform.localScale = new Vector3(.07f, .8f, .9f);
            groove.transform.localRotation = Quaternion.Euler(0f, 4f, 0f);
            var r = groove.GetComponent<Renderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            VfxMaterial.Ghost(r, Lava, .55f);
            var piece = Keep(f, node, rest, normal, size, lockAt, how, r.sharedMaterial, twist);
            piece.Seed = seed;
            return piece;
        }

        private Piece Keep(Frame f, Transform node, Vector3 rest, Vector3 normal, Vector3 scale, float lockAt, Arrive how, Material seam, float twist)
        {
            var n = normal.normalized;
            var p = new Piece
            {
                Node = node, Bone = f.Bone, Rest = f.Local(rest), Normal = f.LocalDir(n), Scale = scale,
                Turn = f.Basis * Quaternion.FromToRotation(Vector3.up, n) * Quaternion.Euler(0f, twist, 0f),
                LockAt = lockAt, How = how, Seam = seam, Seed = _pieces.Count * 7 + 3, Order = ShedOrder(node.name, _pieces.Count),
            };
            node.localPosition = p.Rest; node.localRotation = p.Turn; node.localScale = Vector3.zero;
            // Kept out of the replay's model fingerprint and pose tracks: it rides his bones but is not his model.
            node.gameObject.AddComponent<DanteMountainPiece>();
            _pieces.Add(p);
            return p;
        }

        // A shoulder's columns: (across share, along share, radius, height, lean out deg, lean back deg).
        private static readonly (float x, float z, float r, float h, float outLean, float backLean)[] HornShoulder =
        {
            (.0f, .1f, .056f, .21f, 24f, 6f), (.6f, -.45f, .046f, .15f, 40f, 20f), (-.4f, .6f, .04f, .11f, 16f, -8f),
            (.85f, .4f, .034f, .08f, 55f, 0f),
        };
        private static readonly (float x, float z, float r, float h, float outLean, float backLean)[] OtherShoulder =
        {
            (.0f, .0f, .052f, .11f, 30f, 8f), (.6f, -.5f, .04f, .08f, 46f, 18f),
        };

        // THE SUIT (owner, 2026-10-18: *"i want really bulk armor hindi ung parts of the body lang"*, Toph's earth suit as the
        // measure of bulk and not of look). Basalt, not Toph's sandstone cobbles. The bulk was first built as a shell of boulders
        // round every bone and is now built as plates (the note over `Plate`): the same bulk, with shapes that read.

        /// <summary>Its place in the shedding: the high and far pieces first, the core of the suit late, the stone horn last.</summary>
        private static float ShedOrder(string role, int index)
        {
            float jitter = Hash(index + 977, 9);
            if (role.Contains("Crown") || role.Contains("Range") || role.Contains("Shoulder")) return .02f + .3f * jitter;
            if (role.Contains("Hood") || role.Contains("Knuckle") || role.Contains("Boot")) return .15f + .4f * jitter;
            if (role.Contains("Fist") || role.Contains("HornSeat")) return .82f + .12f * jitter;
            if (role.Contains("Chest") || role.Contains("MountainBack")) return .64f + .16f * jitter;
            return .25f + .6f * jitter;
        }

        private static float Hash(int seed, int salt) { uint h = (uint)(seed * 73856093) ^ (uint)(salt * 19349663); h ^= h >> 13; h *= 1274126177u; h ^= h >> 16; return (h & 0xFFFF) / 65535f; }

        /// <summary>One boulder of the suit, out of the bone along <paramref name="dir"/>.</summary>
        private Piece Rock(Frame f, string role, Vector3 at, Vector3 dir, float radius, float height, float lockAt, Arrive how, int seed)
        {
            bool molten = seed % 3 == 0;
            var colour = seed % 4 == 0 ? BasaltPale : seed % 4 == 1 ? BasaltDark : Basalt;
            int sides = 5 + seed % 3;
            if (molten) return Column(f, role, at, dir, radius, height, lockAt, how, seed, colour, .4f + .1f * Hash(seed, 1), .72f, seed * 37f % 360f, sides);
            var node = VfxShapes.Stand(f.Bone, role, VfxShapes.Prism(sides, 1f, .7f + .12f * Hash(seed, 2), .22f, 0f, seed), 1).transform;
            Dress(node, colour);
            var piece = Keep(f, node, at, dir, new Vector3(radius, height, radius), lockAt, how, null, seed * 37f % 360f);
            piece.Seed = seed;
            return piece;
        }

        // ------------------------------------------------------------------ the suit, plate by plate
        //
        // ⚠️⚠️ PLATES, NOT A HEAP (2026-10-18, turnarounds `Logs/mf/poses_v2`). The first suit was every bone wrapped in rings of
        // similar boulders, about 250 of them: from any side it read as one grey dome with his head on top, the arms sank into it,
        // every clip looked like every other, and the molten seams were specks with no direction. The brief is a deliberate,
        // bulky stone warrior. So the suit is built like armour: a few big plates, each with one job and its own place, layered
        // over each other (the breastplate over the belly plate, the belt over the tassets, the pauldron over the arm's plate),
        // and one molten seam per plate running along it, so the lines lead the eye: down the chest, round the belt, along the
        // limbs to the boulder fists. The face stays bare (the old gorget ring rose to his eyes).

        /// <summary>
        /// A plate whose long side runs along <paramref name="along"/> (in the bone's frame) and whose face looks along
        /// <paramref name="normal"/>; <paramref name="size"/> is (across, thickness, along). Its molten seam runs its length.
        /// </summary>
        private Piece Plate(Frame f, string role, Vector3 rest, Vector3 normal, Vector3 along, Vector3 size, float lockAt, Arrive how, int seed, Color colour)
        {
            var p = Slab(f, role, rest, normal, size, lockAt, how, seed, colour);
            var n = normal.normalized;
            var forward = along - Vector3.Dot(along, n) * n;
            if (forward.sqrMagnitude < 1e-6f) forward = Vector3.Cross(n, Vector3.right);
            p.Turn = f.Basis * Quaternion.LookRotation(forward.normalized, n);
            p.Node.localRotation = p.Turn;
            return p;
        }

        /// <summary>
        /// A BRACER: <paramref name="count"/> plates round a limb bone from <paramref name="from"/> to <paramref name="to"/> along its
        /// long axis (0 his right, 1 his up), each a share of the way round and a little more, so they overlap into a closed tube
        /// with a seam down every plate.
        /// </summary>
        private void Bracer(Frame f, string role, int axis, float from, float to, int count, float thick, float spread, float lockFrom,
                            float lockStep, Arrive how, int seed, float turn = 0f)
        {
            var c = f.C; var e = f.E;
            float mid = (from + to) * .5f, length = Mathf.Abs(to - from);
            float ra = axis == 0 ? e.y : e.x, rb = e.z;
            float round = Mathf.PI * (ra + rb);
            var axisDir = axis == 0 ? Vector3.right : Vector3.up;
            for (int i = 0; i < count; i++)
            {
                float a = (i + turn) / count * Mathf.PI * 2f;
                Vector3 dir, at;
                if (axis == 0) { dir = new Vector3(0f, Mathf.Cos(a), Mathf.Sin(a)); at = new Vector3(mid, c.y + dir.y * ra, c.z + dir.z * rb); }
                else { dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)); at = new Vector3(c.x + dir.x * ra, mid, c.z + dir.z * rb); }
                Plate(f, role + i, at, dir, axisDir, new Vector3(round / count * spread, thick, length), lockFrom + lockStep * i, how, seed + i,
                      i % 2 == 0 ? Basalt : BasaltPale);
            }
        }

        private void Torso(Frame f, Frame whole)
        {
            var c = f.C; var e = f.E;
            float h = f.Max.y - f.Min.y, hem = whole.Min.y;
            if (Debug.isDebugBuild && Application.isEditor) Debug.Log($"[Mountain] coat min{whole.Min:F3} max{whole.Max:F3}");
            // A point on a ring round him at angle a (0 his front), `grow` times out to the core's (0) or the coat's (1) surface on
            // that side: the coat's tail flares far out behind him at the hem, so the back is not the front mirrored.
            Vector3 Rim(float a, float y, float coat, float grow)
            {
                float sx = Mathf.Sin(a), cz = Mathf.Cos(a);
                float x = Mathf.Lerp(sx > 0f ? f.Max.x - c.x : c.x - f.Min.x, sx > 0f ? whole.Max.x - c.x : c.x - whole.Min.x, coat);
                float z = Mathf.Lerp(cz > 0f ? f.Max.z - c.z : c.z - f.Min.z, cz > 0f ? whole.Max.z - c.z : c.z - whole.Min.z, coat);
                return new Vector3(c.x + sx * x * grow, y, c.z + cz * z * grow);
            }
            Vector3 Tangent(float a) => new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));

            // THE BREASTPLATE: two plates, the chest over the belly, each with its seam straight down the middle: the first
            // line the eye takes on him, from his chin to his belt.
            Plate(f, "MountainChest0", new Vector3(c.x, f.Min.y + h * .66f, f.Max.z + .002f), new Vector3(0f, .18f, 1f), Vector3.up,
                  new Vector3(e.x * 2.15f, .05f, h * .52f), ChestAt, Arrive.Fly, 500, BasaltPale);
            Plate(f, "MountainChest1", new Vector3(c.x, f.Min.y + h * .3f, f.Max.z - .006f), new Vector3(0f, -.08f, 1f), Vector3.up,
                  new Vector3(e.x * 1.85f, .044f, h * .44f), ChestAt - .06f, Arrive.Fly, 502, Basalt);
            // THE BACK: one broad plate, and the range of columns rising off it.
            Plate(f, "MountainBack", new Vector3(c.x, f.Min.y + h * .56f, f.Min.z - .002f), new Vector3(0f, .1f, -1f), Vector3.up,
                  new Vector3(e.x * 2.1f, .05f, h * .8f), ChestAt + .04f, Arrive.Fly, 504, Basalt);
            foreach (float side in new[] { -1f, 1f })
                Plate(f, "MountainSide" + (side < 0 ? "L" : "R"), new Vector3(side * (e.x + .004f) + c.x, f.Min.y + h * .45f, c.z), new Vector3(side, 0f, 0f), Vector3.up,
                      new Vector3(e.z * 1.7f, .04f, h * .62f), ChestAt + .08f, Arrive.Fly, side < 0 ? 506 : 508, BasaltDark);
            float[] range = { .11f, .16f, .09f };
            for (int i = 0; i < 3; i++)
                Column(f, "MountainRange" + i, c + new Vector3(e.x * (-.45f + .45f * i), e.y * (.45f + .2f * (i % 2)), -e.z * 1.25f),
                       new Vector3(.4f * (i - 1), .55f, -.85f), .05f - .006f * (i % 2), range[i], ChestAt + .26f + .05f * i, Arrive.Grow, 221 + 3 * i, BasaltPale, .25f);

            // THE COLLAR: low plates round the neck over the coat's collar and its gold points, leaning OUT, never up over his jaw.
            // None at his chin.
            for (int i = 0; i < 6; i++)
            {
                float a = (i + .5f) / 6f * Mathf.PI * 2f + Mathf.PI / 6f;
                if (Mathf.Abs(Mathf.DeltaAngle(0f, a * Mathf.Rad2Deg)) < 40f) continue;
                var dir = new Vector3(Mathf.Sin(a), .55f, Mathf.Cos(a));
                Plate(f, "MountainCollar" + i, Rim(a, f.Max.y - .008f, 0f, .82f), dir, Tangent(a), new Vector3(.035f, .028f, e.x * .62f),
                      ChestAt + .12f + .02f * i, Arrive.Fly, 1060 + i, BasaltDark);
            }

            // THE BELT: twelve blocks, all the way round over the coat's gold band, one molten line running through every one.
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                var at = Rim(a, f.Min.y + .012f, .35f, 1f);
                float ring = 2f * Mathf.PI * Mathf.Lerp(e.x, e.z, .5f) * 1.1f;
                Plate(f, "MountainBelt" + i, at, new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)), Tangent(a), new Vector3(.034f, .04f, ring / 12f * 1.12f),
                      ChestAt - .16f + .012f * i, Arrive.Rise, 540 + i, BasaltDark);
            }
            // THE TASSETS: two layers of hanging plates from under the belt past the coat's hem, the short outer layer over the
            // joins of the long inner one, tipped out at the bottom; behind him they follow the coat's tail out.
            for (int layer = 0; layer < 2; layer++)
                for (int i = 0; i < 8; i++)
                {
                    float a = (i + .5f * layer) / 8f * Mathf.PI * 2f + Mathf.PI / 8f;
                    float back = Mathf.Clamp01(-Mathf.Cos(a));
                    float top = f.Min.y - .004f, bottom = hem - (layer == 0 ? .028f : -.03f);
                    float y = (top + bottom) * .5f;
                    var at = Rim(a, y, Mathf.Lerp(.55f, 1f, back), layer == 0 ? 1.02f : 1.1f);
                    var dir = new Vector3(Mathf.Sin(a), -.32f, Mathf.Cos(a));
                    float ring = 2f * Mathf.PI * Mathf.Lerp(e.x, Mathf.Lerp(e.z, whole.E.z, back), .5f);
                    Plate(f, "MountainTasset" + (layer * 10 + i), at, dir, Vector3.down,
                          new Vector3(ring / 8f * (layer == 0 ? 1.3f : 1.05f), .034f, top - bottom), LegsAt + .14f + .015f * i - .04f * layer, Arrive.Rise,
                          560 + layer * 20 + i, layer == 0 ? Basalt : BasaltPale);
                }
        }

        /// <summary>
        /// THE PAULDRONS, on the torso's shoulder line and not the upper arm (his idle drops the arms 45 degrees: armour on the arm
        /// swung out like blades). A great domed boulder on each shoulder outside his head box, a plate hanging off it over the arm,
        /// and over the obsidian horn's side a range of columns: the horn answered, the shoulders not a pair.
        /// </summary>
        private void Shoulders(Frame f)
        {
            var c = f.C; var e = f.E;
            foreach (float side in new[] { -1f, 1f })
            {
                bool hornSide = side < 0f;
                string s = hornSide ? "L" : "R";
                var top = new Vector3(c.x + side * (e.x + .026f), f.Max.y - .004f, c.z + e.z * .05f);
                var dome = VfxShapes.Stand(f.Bone, "MountainShoulder" + s, VfxShapes.Prism(7, 1f, .62f, .3f, 0f, hornSide ? 233 : 273), 1).transform;
                Dress(dome, Basalt);
                var lean = Quaternion.Euler(0f, 0f, -side * 28f) * Vector3.up;
                var piece = Keep(f, dome, top, lean, new Vector3(.105f, .085f, .105f), ShouldersAt, Arrive.Grow, null, 0f);
                piece.Seed = hornSide ? 233 : 273;
                Plate(f, "MountainShoulderPlate" + s, top + new Vector3(side * .03f, -.04f, 0f), new Vector3(side, -.35f, 0f), Vector3.down,
                      new Vector3(e.z * 1.5f, .032f, .06f), ShouldersAt + .05f, Arrive.Fly, hornSide ? 240 : 280, BasaltPale);
                var rows = hornSide ? HornShoulder : OtherShoulder;
                for (int i = 0; i < rows.Length; i++)
                {
                    var r = rows[i];
                    var dir = Quaternion.Euler(-r.backLean, 0f, -side * r.outLean) * Vector3.up;
                    Column(f, "MountainShoulder" + s + i, top + new Vector3(side * r.x * .03f, .02f, r.z * e.z * .45f), dir,
                           r.r * .9f, r.h, ShouldersAt + .08f + .04f * i, Arrive.Grow, 231 + 3 * i + (hornSide ? 0 : 40), i % 2 == 0 ? Basalt : BasaltPale,
                           .22f + .05f * (i % 3));
                }
            }
        }

        // THE ARMS (owner, 2026-10-18: *"really cover his arms iwth rocks"*, *"gusto ko ung big rocks kanina sa arm niya sa old ult"*):
        // a bracer on each bone, and the old cutscene's boulder fist past the hand, the suit's focal weight.
        private void UpperArm(Frame f)
        {
            float outward = Mathf.Sign(f.C.x == 0f ? 1f : f.C.x);
            float joint = outward > 0f ? f.Min.x : f.Max.x, elbow = outward > 0f ? f.Max.x : f.Min.x;
            Bracer(f, "MountainUpperArm", 0, Mathf.Lerp(joint, elbow, .3f), Mathf.Lerp(joint, elbow, .95f), 4, .03f, 1.2f,
                   ShouldersAt - .12f, .04f, Arrive.Fly, outward < 0f ? 600 : 650, .125f);
        }

        private void Forearm(Frame f)
        {
            float outward = Mathf.Sign(f.C.x == 0f ? 1f : f.C.x);
            bool hornSide = outward < 0f;
            float elbow = outward > 0f ? f.Min.x : f.Max.x, hand = outward > 0f ? f.Max.x : f.Min.x;
            var c = f.C; var e = f.E;
            float thick = Mathf.Max(e.y, e.z);
            Bracer(f, "MountainForearm", 0, Mathf.Lerp(elbow, hand, .08f), Mathf.Lerp(elbow, hand, .58f), 5, .036f, 1.22f,
                   BracersAt - .1f, .04f, Arrive.Fly, hornSide ? 700 : 760, .1f);
            // THE BOULDER FIST: one great rock over the hand and past it, knuckle stones on its face.
            Column(f, "MountainFist", new Vector3(Mathf.Lerp(elbow, hand, .74f), c.y, c.z), new Vector3(outward, 0f, 0f), thick * 1.75f,
                   Mathf.Abs(hand - elbow) * .5f, FistsAt, Arrive.Fly, 470 + (hornSide ? 0 : 9), BasaltDark, .45f, .8f, 18f, 6);
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f + 30f) * Mathf.Deg2Rad;
                Rock(f, "MountainKnuckle" + i, new Vector3(hand + outward * .02f, c.y + Mathf.Cos(a) * e.y * .7f, c.z + Mathf.Sin(a) * e.z * .7f),
                     new Vector3(outward, Mathf.Cos(a) * .5f, Mathf.Sin(a) * .5f), thick * .75f, thick * .6f, FistsAt + .05f + .04f * i, Arrive.Fly, 480 + i * 3 + (hornSide ? 0 : 9));
            }
        }

        /// <summary>
        /// THE LEGS: a bracer on the thigh and a greave on the shin, both closed all round (the coat's skirt is skinned to the legs),
        /// then a boot ring and a cap over the toe. ⚠️ THE BOOT AND THE TOE SIT ABOVE THE SOLE: a plate's lowest edge is its centre less
        /// half its length, kept over the sole, so a planted foot never shows stone sunk into the court.
        /// </summary>
        private void Leg(Frame f, Frame whole, float sole)
        {
            float outward = Mathf.Sign(f.C.x == 0f ? 1f : f.C.x);
            float foot = whole.Min.y, hip = f.Max.y, len = hip - foot;
            int seed = outward > 0 ? 800 : 850;
            Bracer(f, "MountainThigh", 1, hip - len * .06f, hip - len * .42f, 4, .038f, 1.2f, LegsAt + .1f, .025f, Arrive.Rise, seed, .5f);
            Bracer(f, "MountainGreave", 1, hip - len * .44f, hip - len * .7f, 4, .036f, 1.2f, LegsAt + .03f, .025f, Arrive.Rise, seed + 10);
            var boot = whole; boot.Min = new Vector3(whole.Min.x, foot, whole.Min.z); boot.Max = new Vector3(whole.Max.x, foot + len * .3f, whole.Max.z);
            float bootLow = foot + .016f, bootHigh = foot + len * .26f;
            Bracer(boot, "MountainBoot", 1, bootLow, bootHigh, 5, .032f, 1.15f, LegsAt - .04f, .015f, Arrive.Rise, seed + 30, .5f);
            // THE SOLE (film v10: in the leap's hang his white sneaker soles faced the lens, two white cards under the stone): a thin
            // dark plate under the sole's lowest vertex (`sole`, every vertex, not the percentile `foot`), 2.5 mm deep at the rig's
            // size, so it covers the white from below and sinks no visible distance into the court.
            Plate(f, "MountainBootSole", new Vector3(whole.C.x, sole + .0005f, whole.C.z), Vector3.down, Vector3.forward,
                  new Vector3(whole.E.x * 2.05f, .003f, whole.E.z * 2.05f), LegsAt - .06f, Arrive.Rise, seed + 45, BasaltDark);
            Plate(f, "MountainToe", new Vector3(whole.C.x, foot + len * .2f, whole.Max.z - .03f), new Vector3(0f, 1f, .45f), Vector3.forward,
                  new Vector3(whole.E.x * 1.6f, .03f, .06f), LegsAt - .05f, Arrive.Rise, seed + 40, BasaltDark);
        }

        /// <summary>
        /// His horn, mirrored: every triangle connected to the points that stand out past the head box on his left (so its base
        /// inside the head comes too), flipped across his face into the head bone's space, its winding turned back. Built about
        /// its own root (returned) so it grows out of the temple.
        /// </summary>
        private static Mesh MirroredHorn(SkinnedMeshRenderer skin, int bone, Frame f, List<Vector3> seeds, out Vector3 root)
        {
            root = Vector3.zero;
            var mesh = skin.sharedMesh; var bind = mesh.bindposes[bone];
            var vertices = mesh.vertices; var tris = mesh.triangles;
            int count = vertices.Length;
            var rest = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                var q = bind.MultiplyPoint3x4(vertices[i]);
                rest[i] = new Vector3(Vector3.Dot(q, f.R), Vector3.Dot(q, f.U), Vector3.Dot(q, f.F));
            }
            // Joined by triangles, and by shared positions (a faceted mesh splits its vertices).
            var parent = new int[count]; for (int i = 0; i < count; i++) parent[i] = i;
            int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
            void Join(int a, int b) { a = Find(a); b = Find(b); if (a != b) parent[a] = b; }
            for (int t = 0; t < tris.Length; t += 3) { Join(tris[t], tris[t + 1]); Join(tris[t], tris[t + 2]); }
            var at = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < count; i++)
            {
                var key = Vector3Int.RoundToInt(rest[i] * 4000f);
                if (at.TryGetValue(key, out int other)) Join(i, other); else at[key] = i;
            }
            var seedSet = new HashSet<Vector3Int>(); foreach (var p in seeds) seedSet.Add(Vector3Int.RoundToInt(p * 4000f));
            var hornSets = new HashSet<int>();
            for (int i = 0; i < count; i++) if (seedSet.Contains(Vector3Int.RoundToInt(rest[i] * 4000f))) hornSets.Add(Find(i));
            if (hornSets.Count == 0) return null;
            var outV = new List<Vector3>(); var outT = new List<int>();
            for (int t = 0; t < tris.Length; t += 3)
            {
                if (!hornSets.Contains(Find(tris[t]))) continue;
                for (int k = 2; k >= 0; k--)
                {
                    var r = rest[tris[t + k]];
                    outT.Add(outV.Count); outV.Add(new Vector3(-r.x, r.y, r.z));
                }
            }
            if (outV.Count == 0) return null;
            // Its root: the end nearest the head's middle.
            float nearest = float.PositiveInfinity;
            foreach (var v in outV) nearest = Mathf.Min(nearest, v.x);
            var baseCentre = Vector3.zero; int n = 0;
            foreach (var v in outV) if (v.x < nearest + .02f) { baseCentre += v; n++; }
            baseCentre /= Mathf.Max(1, n);
            root = f.Local(baseCentre);
            for (int i = 0; i < outV.Count; i++) outV[i] = f.Local(outV[i]) - root;
            var horn = new Mesh { name = "MountainStoneHorn" };
            horn.SetVertices(outV); horn.SetTriangles(outT, 0); horn.RecalculateNormals(); horn.RecalculateBounds();
            if (Debug.isDebugBuild && Application.isEditor) Debug.Log($"[Mountain] mirrored horn {outT.Count / 3} triangles");
            return horn;
        }

        private void Head(SkinnedMeshRenderer skin, int bone)
        {
            var basis = Basis(skin, bone, out var points);
            var all = Bounds(basis, points, null);
            float box = all.Max.x, half = (all.Min.y + all.Max.y) * .5f;
            bool IsHorn(Vector3 p) => p.x < -box * 1.08f && p.y > half;
            var f = Bounds(basis, points, p => !IsHorn(p));
            var horn = new List<Vector3>();
            foreach (var p in points) if (IsHorn(p)) horn.Add(p);
            var c = f.C; var e = f.E;
            if (horn.Count >= 6)
            {
                horn.Sort((a, b) => b.x.CompareTo(a.x));
                var root = Vector3.zero; int n = Mathf.Min(6, horn.Count);
                for (int i = 0; i < n; i++) root += horn[i]; root /= n;
                Vector3 tip = root, bend = root; float far = 0f, off = 0f;
                foreach (var p in horn) { float d = (p - root).sqrMagnitude; if (d > far) { far = d; tip = p; } }
                var axis = (tip - root).normalized;
                foreach (var p in horn)
                {
                    var rel = p - root; float d = (rel - Vector3.Dot(rel, axis) * axis).sqrMagnitude;
                    if (d > off && Vector3.Dot(rel, axis) > 0f) { off = d; bend = p; }
                }
                var seat = new Vector3(-box * .96f, root.y + .008f, root.z + .01f);
                // Molten veins up the obsidian.
                _veinPath.Add((f.Local(root), f.Local(bend))); _veinPath.Add((f.Local(bend), f.Local(tip)));
                for (int i = 0; i < 4; i++) _veins.Add(DanteDriftLive.AddGlow(f.Bone, "MountainHornVein" + i, new Color(1f, .5f, .1f), 1.6f, .7f));
                // THE SECOND HORN (owner, 2026-10-18: *"it should match or mirror the other horn but in rock form in terms of size and
                // placement"*): his own horn's geometry, every triangle of it, mirrored onto his right temple and turned to basalt,
                // with the same molten veins up it.
                var stone = MirroredHorn(skin, bone, f, horn, out var mirroredRoot);
                if (stone != null)
                {
                    var hornNode = VfxShapes.Stand(f.Bone, "MountainStoneHorn", stone, 1).transform;
                    Dress(hornNode, BasaltPale);
                    var piece = new Piece
                    {
                        Node = hornNode, Bone = f.Bone, Rest = mirroredRoot, Normal = Vector3.zero, Scale = Vector3.one, Turn = Quaternion.identity,
                        LockAt = HeadAt + .14f, How = Arrive.Grow, Seed = 301, Uniform = true, Order = 1f,
                    };
                    hornNode.localPosition = piece.Rest; hornNode.localRotation = Quaternion.identity; hornNode.localScale = Vector3.zero;
                    _pieces.Add(piece);
                    Vector3 M(Vector3 p) => new Vector3(-p.x, p.y, p.z);
                    _veinPath.Add((f.Local(M(root)), f.Local(M(bend)))); _veinPath.Add((f.Local(M(bend)), f.Local(M(tip))));
                    for (int i = 0; i < 4; i++) _veins.Add(DanteDriftLive.AddGlow(f.Bone, "MountainStoneHornVein" + i, new Color(1f, .5f, .1f), 1.6f, .7f));
                    // A basalt collar seats each horn (owner, 2026-10-18: the rocks on his horns stay; none over his hair).
                    var collar = new Vector3(-box * .97f, root.y + .006f, root.z + .008f);
                    Column(f, "MountainHornSeatL", collar, root - collar + Vector3.up * .01f, .056f, .034f, HeadAt, Arrive.Grow, 311, BasaltDark, .45f, .9f);
                    Column(f, "MountainHornSeatR", M(collar), M(root) - M(collar) + Vector3.up * .01f, .056f, .034f, HeadAt + .04f, Arrive.Grow, 314, BasaltDark, .45f, .9f);
                }
            }
            // His gold left eye, burning.
            _eyeAt = f.Local(new Vector3(c.x - e.x * .42f, f.Min.y + (f.Max.y - f.Min.y) * .31f, f.Max.z + .006f));
            _eye = DanteDriftLive.AddGlow(f.Bone, "MountainEye", new Color(1f, .62f, .18f), 2.2f, .8f);
            _eye.Renderer.sharedMaterial.SetFloat("_Billboard", 1f); _eye.Renderer.sharedMaterial.SetFloat("_Band", 0f);
        }

        // ------------------------------------------------------------------ the clock

        private static float Ease(float from, float to, float time)
        { float u = Mathf.InverseLerp(from, to, time); return u * u * (3 - 2 * u); }

        /// <summary>Pose every piece at <paramref name="t"/> on the cutscene's clock (`FormedAt` is complete).</summary>
        public void StepTo(float t)
        {
            Age = t;
            if (Orphaned) return;
            _visible = t < GoneAt;
            float shedFrom = _endAt - ShedSeconds;
            float heat = 1f - .45f * Ease(FormedAt, shedFrom, t);
            float warn = Ease(shedFrom - 1f, shedFrom + 1f, t);
            float breath = .12f * Mathf.Sin(t * 2.6f);
            var seamColour = Color.Lerp(Hot, Cooled, warn);
            if (_badge != null) _badge.StepTo(t - FormedAt, _endAt - FormedAt);
            float ground = _worldRoot.position.y;
            for (int i = 0; i < _pieces.Count; i++)
            {
                var p = _pieces[i];
                if (p.Node == null) continue;
                p.DropAt = shedFrom + (ShedSeconds - .35f) * p.Order;
                if (t >= p.DropAt) { Shed(p, t, i, seamColour); continue; }
                if (p.Dropped) Reattach(p);
                float a = t - p.LockAt;
                Vector3 pos = p.Rest; float grow = 1f; float size = 1f;
                if (p.How == Arrive.Grow)
                {
                    grow = Ease(-.12f, 0f, a);
                    if (grow <= 0f) { p.Node.localScale = Vector3.zero; continue; }
                }
                else
                {
                    float fly = Mathf.Clamp01((a + FlyTime) / FlyTime);
                    if (fly <= 0f) { p.Node.localScale = Vector3.zero; continue; }
                    if (fly < 1f)
                    {
                        var restWorld = p.Bone.TransformPoint(p.Rest);
                        Vector3 from;
                        // Out of the court's surface, never from under it (a piece under the court is a piece through it).
                        if (p.How == Arrive.Rise) from = new Vector3(restWorld.x, ground + .01f, restWorld.z);
                        else
                        {
                            var d = restWorld - _worldRoot.position; d.y = 0f;
                            d = d.sqrMagnitude > 1e-4f ? d.normalized : _worldRoot.forward;
                            d = Quaternion.Euler(0f, (p.Seed % 11 - 5) * 6f, 0f) * d;
                            from = _worldRoot.position + d * 1.7f + Vector3.up * .02f;
                        }
                        float arrive = Ease(0f, 1f, fly);
                        var world = Vector3.Lerp(from, restWorld, arrive) + Vector3.up * (Mathf.Sin(fly * Mathf.PI) * (p.How == Arrive.Rise ? .1f : .7f));
                        pos = p.Bone.InverseTransformPoint(world);
                        size = Mathf.Lerp(.6f, 1f, arrive);
                    }
                }
                // The jolt as it locks, the tremble as it cools, the crumble into the court.
                // ⚠️ A LOCK IS A BLOW (film v1: the suit's stones arrived small and quick and the covering did not read): it
                // overshoots into him and kicks back out, and its seam flares white-hot orange for a beat.
                if (a >= 0f) pos += p.Normal * (.022f * Mathf.Max(0f, 1f - a / .1f) * Mathf.Cos(a * 40f));
                // Loosening before it drops: a growing tremble and a nudge out of the suit.
                float loose = Ease(p.DropAt - LoosenSeconds, p.DropAt, t);
                float tremble = (warn * .8f + loose * 7f);
                var turn = p.Turn * Quaternion.Euler(Mathf.Sin(t * 47f + i * 1.7f) * tremble, 0f, Mathf.Sin(t * 53f + i) * tremble);
                if (p.How != Arrive.Grow || !p.Uniform) pos += p.Normal * (.012f * loose);
                float flash = a >= 0f ? 1f + .28f * Mathf.Max(0f, 1f - a / .08f) : 1f;
                var scale = p.Scale * size * flash;
                if (p.How == Arrive.Grow && p.Uniform) scale *= grow;
                else if (p.How == Arrive.Grow) scale = new Vector3(scale.x * Mathf.Lerp(.5f, 1f, grow), scale.y * grow, scale.z * Mathf.Lerp(.5f, 1f, grow));
                p.Node.localPosition = pos; p.Node.localRotation = turn; p.Node.localScale = _visible ? scale : Vector3.zero;
                if (p.Seam != null)
                {
                    float lit = a >= 0f ? 1f : 0f, flare = a >= 0f ? Mathf.Max(0f, 1f - a / .3f) * 1.8f : 0f;
                    p.Seam.SetColor("_EmissionColor", seamColour * (((.5f + breath + .1f * Mathf.Sin(t * 4.2f + i * .6f)) * heat + flare + loose * .8f) * lit));
                }
            }
            // The horn's veins ignite once the second horn has grown, and burn with the seams.
            float fall = Ease(_endAt - .8f, _endAt, t);
            float vein = Ease(HeadAt + .2f, HeadAt + .3f, t) * (1f - fall) * heat * (1.2f + .3f * Mathf.Sin(t * 5f)) + 1.4f * Mathf.Max(0f, 1f - Mathf.Abs(t - HeadAt - .28f) / .12f);
            for (int i = 0; i < _veins.Count; i++)
            {
                if (_veins[i]?.Node == null) continue;
                var (from, to) = _veinPath[i / 2];
                var d = to - from;
                var turn = Quaternion.FromToRotation(Vector3.right, d.normalized) * Quaternion.Euler(90f * (i % 2), 0f, 0f);
                DanteDriftLive.Light(_veins[i], (from + to) * .5f, new Vector3(d.magnitude * 1.1f, .028f, .028f), turn, _visible ? vein : 0f);
            }
            if (_eye?.Node != null)
            {
                float eye = (2.4f * Mathf.Max(0f, 1f - Mathf.Abs(t - FormedAt) / .15f) + .9f * Ease(FormedAt - .1f, FormedAt, t)) * (1f - fall) * Mathf.Lerp(.7f, 1f, heat);
                DanteDriftLive.Light(_eye, _eyeAt, Vector3.one * .055f * (1f + .4f * Mathf.Max(0f, 1f - Mathf.Abs(t - FormedAt) / .15f)), Quaternion.identity, _visible ? eye : 0f);
            }
        }

        /// <summary>A stone that has come off: it falls from where it was on him, tumbling away from his middle, lies on the court where
        /// he was, cools, and sinks in. Off his body it no longer follows him.</summary>
        private void Shed(Piece p, float t, int i, Color seamColour)
        {
            if (!p.Dropped)
            {
                if (_debris == null)
                {
                    _debris = new GameObject("DanteMountainDebris").transform;
                    VfxRenderTag.Attach(_debris.gameObject);
                }
                // Where it was on his body as it lets go.
                p.FromWorld = p.Node.position; p.FromTurn = p.Node.rotation;
                var away = p.FromWorld - _worldRoot.position; away.y = 0f;
                p.Out = away.sqrMagnitude > 1e-4f ? away.normalized : Quaternion.Euler(0f, p.Seed * 47f, 0f) * Vector3.forward;
                // ⚠️ THE COURT UNDER THE STONE, NOT HIS FEET: a stone that lets go while he is in the air (a jump, a launch, a
                // ledge) fell to where his root was and lay there floating. On the court his root is the court; off it, the ray.
                p.Ground = _owner == null || _owner.IsGrounded ? _worldRoot.position.y
                    : Mathf.Min(_worldRoot.position.y, Slipper.FindGroundY(p.FromWorld, .1f));
                p.Node.SetParent(_debris, true);
                p.LyingScale = p.Node.localScale;
                p.Dropped = true;
            }
            float d = t - p.DropAt;
            float h = Mathf.Max(0f, p.FromWorld.y - p.Ground - .03f), up = .9f;
            float land = (up + Mathf.Sqrt(up * up + 19.6f * h)) / 9.8f;
            float ft = Mathf.Min(d, land);
            var at = p.FromWorld + p.Out * (1.1f * ft) + Vector3.up * (up * ft - 4.9f * ft * ft);
            float spin = ft * (360f + p.Seed % 7 * 60f);
            // Lying, then into the court.
            float sink = Ease(land + .6f, DebrisSeconds, d);
            at.y = Mathf.Max(p.Ground + .02f - .08f * sink, at.y) - .06f * sink;
            p.Node.position = at;
            p.Node.rotation = Quaternion.AngleAxis(spin, Vector3.Cross(Vector3.up, p.Out)) * p.FromTurn;
            p.Node.localScale = d < DebrisSeconds ? p.LyingScale * (1f - .5f * sink) : Vector3.zero;
            if (p.Seam != null) p.Seam.SetColor("_EmissionColor", seamColour * (.7f * (1f - Mathf.Clamp01(d / .7f))));
        }

        private void Reattach(Piece p)
        {
            p.Node.SetParent(p.Bone, false);
            p.Dropped = false;
        }

        private void OnEnable() => Camera.onPreCull += BeforeCamera;
        private void OnDisable() => Camera.onPreCull -= BeforeCamera;

        private void BeforeCamera(Camera camera)
        {
            if (_owner == null || _renderers == null) return;
            var rig = camera.GetComponent<CameraRig>();
            bool hidden = rig != null && rig.IsLocalFpp && rig.IsFollowing(_owner);
            foreach (var r in _renderers) if (r != null) r.forceRenderingOff = hidden && (_debris == null || !r.transform.IsChildOf(_debris));
        }

        private void OnDestroy()
        {
            foreach (var p in _pieces) if (p.Node != null) Destroy(p.Node.gameObject);
            foreach (var g in _veins) if (g?.Node != null) Destroy(g.Node.gameObject);
            if (_eye?.Node != null) Destroy(_eye.Node.gameObject);
            if (_badge != null) Destroy(_badge.gameObject);
            if (_debris != null) Destroy(_debris.gameObject);
        }
    }
}
