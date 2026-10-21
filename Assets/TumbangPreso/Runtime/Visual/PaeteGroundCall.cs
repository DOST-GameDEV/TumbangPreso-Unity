using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // =============================================================================================
    // ⚠️⚠️ MAKILING'S EMBRACE IS CALLED FROM THE GROUND, v5 (owner, 2026-09-26 night): *"i also dont like that paete just throws
    // seeds in his ult"*, *"REDIRECT IT I WANNT IT TO LOOK LIKE HE GOES TO THE GHHROUND AND HIS ROOTS CONNECT TO IT AND HE IS
    // CHANNELLING HIS POWER AND HE GLOWS AND SHIT AND THEN HIS ROOTS TRAVEL TO THE GROUND AND THEN THE tree slowly show up"*.
    // `docs/reports/paete-kit-2026-09-25/direction.md` section 5.14 is the design; these are its pieces, shared by the cutscene
    // (`HeroIntroductionScene.Paete`, posed from the scene clock while the world is paused) and by play (`PaeteGroundCall`, on
    // `Update` after the cutscene hands the world back), so what he does in one is what he does in the other:
    //
    //   PaeteLight        a soft light (`SpiritGlow.shader`): the beads that run down his roots, the cracks and rings in the court.
    //   PaeteEyeLight     finds his two eyes on his own face (palette slot 10 on the head bone), for the lights put in front of them.
    //   PaeteChannelGlow  HE GLOWS: a second drawing of his own body in `SpiritVeins.shader`, lighting his vines and his edges.
    //   PaeteGroundRoots  HIS ROOTS CONNECT: roots out of his hands and his knee, dug into the court, taut on the heaves.
    //   PaeteRootRidge    HIS ROOTS TRAVEL: three roots racing under the court to the spot. The court heaves over them and splits
    //                     with the light INSIDE the cracks; there is no light at the front (`PaeteRootVein` had one, and from the
    //                     court a lit block running along the ground is a seed rolling).
    //   PaeteGroundCall   in play: keeps him down on his knee and joined to the ground while the roots travel and the tree crawls
    //                     out, lowers his own view to kneel height, and lets walking cancel all of it (an animation never roots).
    // =============================================================================================

    /// <summary>A soft light (`Resources/Shaders/SpiritGlow`): a quad, a billboard, or a band laid along a given mesh.</summary>
    public sealed class PaeteLight
    {
        private static readonly int ColourId = Shader.PropertyToID("_Color"), FacingId = Shader.PropertyToID("_Facing");
        private static Mesh _quad;
        public readonly Transform Transform;
        public readonly Renderer Renderer;
        private readonly Color _colour;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        private PaeteLight(Transform transform, Renderer renderer, Color colour)
        { Transform = transform; Renderer = renderer; _colour = colour; }

        /// <summary>A unit quad in XY, uv 0 to 1, with bounds wide enough for the shader to billboard it.</summary>
        public static Mesh Quad
        {
            get
            {
                if (_quad != null) return _quad;
                _quad = new Mesh { name = "PaeteLightQuad", hideFlags = HideFlags.DontSave };
                _quad.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) };
                _quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
                _quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                _quad.RecalculateNormals();
                _quad.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
                return _quad;
            }
        }

        public static PaeteLight Create(Transform parent, string name, Color colour, Mesh mesh = null, bool billboard = true, bool band = false,
                                        float falloff = 2f, float core = .6f, float lift = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh ?? Quad;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var shader = Resources.Load<Shader>("Shaders/SpiritGlow");
            if (shader != null)
            {
                var m = new Material(shader) { name = name };
                m.SetFloat("_Billboard", billboard ? 1f : 0f); m.SetFloat("_Band", band ? 1f : 0f);
                m.SetFloat("_Falloff", falloff); m.SetFloat("_Core", core); m.SetFloat("_Lift", lift);
                renderer.sharedMaterial = m;
                VfxRenderTag.Own(go, m);
            }
            else VfxMaterial.Ghost(renderer, colour, 1f);
            renderer.enabled = false;
            return new PaeteLight(go.transform, renderer, colour);
        }

        /// <summary>Place it in its parent's space and set its strength (0 hides it; above 1 is brighter).</summary>
        public void Set(Vector3 position, Vector3 scale, Quaternion rotation, float strength, Vector3? worldFacing = null)
        {
            if (Transform == null) return;
            Transform.localPosition = position; Transform.localScale = scale; Transform.localRotation = rotation;
            var c = _colour; c.a = Mathf.Max(0f, strength);
            _block.SetColor(ColourId, c);
            _block.SetVector(FacingId, worldFacing.HasValue ? (Vector4)worldFacing.Value.normalized + new Vector4(0, 0, 0, 1) : Vector4.zero);
            Renderer.SetPropertyBlock(_block);
            Renderer.enabled = strength > .002f;
        }

        public void Hide() { if (Renderer != null) Renderer.enabled = false; }
    }

    /// <summary>
    /// ⚠️ HIS EYES, FOUND ON HIS OWN FACE (owner: *"make his eyes glow or smth"*; his face is fixed: no mask, no brows, no mouth,
    /// every part typed by hand, so the glow is ADDED in front of the eyes, never painted on). The eye light is palette slot 10
    /// (`tools/build_paete_voxel.py` `EYE`), so the vertices of that cell bound to the head are his two eyes: split into two by the
    /// widest pair, each gives a centre and an outward normal in the head bone's space, and the light is placed there every frame,
    /// wherever the pose has his head. Moved here from `HeroIntroductionScene.Paete` (v4's `PaeteEyes`) so play can light them too.
    /// </summary>
    public sealed class PaeteEyeLight
    {
        private Transform _bone;
        private readonly Vector3[] _centre = new Vector3[2], _normal = new Vector3[2];

        public static PaeteEyeLight Find(IList<Renderer> renderers, Transform head)
        {
            if (renderers == null || head == null) return null;
            foreach (var r in renderers)
            {
                if (!(r is SkinnedMeshRenderer skin) || skin.sharedMesh == null) continue;
                int bone = System.Array.IndexOf(skin.bones, head);
                if (bone < 0) continue;
                var mesh = skin.sharedMesh;
                var weights = mesh.boneWeights; var vertices = mesh.vertices; var normals = mesh.normals; var uv = mesh.uv; var binds = mesh.bindposes;
                if (weights.Length != vertices.Length || uv.Length != vertices.Length || bone >= binds.Length) continue;
                var points = new List<Vector3>(); var faces = new List<Vector3>();
                for (int i = 0; i < vertices.Length; i++)
                {
                    var w = weights[i];
                    float weight = (w.boneIndex0 == bone ? w.weight0 : 0f) + (w.boneIndex1 == bone ? w.weight1 : 0f)
                                 + (w.boneIndex2 == bone ? w.weight2 : 0f) + (w.boneIndex3 == bone ? w.weight3 : 0f);
                    if (weight < 0.5f) continue;
                    // Slot 10 by the toon shader's rule: columns 4 and 5, Unity rows 0 to 3.
                    int row = Mathf.FloorToInt(uv[i].y * 16f), column = Mathf.FloorToInt(uv[i].x * 16f);
                    if (row < 0 || row > 3 || column / 2 != 2) continue;
                    points.Add(binds[bone].MultiplyPoint3x4(vertices[i]));
                    faces.Add(normals.Length == vertices.Length ? binds[bone].MultiplyVector(normals[i]) : Vector3.zero);
                }
                if (points.Count < 4) continue;
                var mid = Vector3.zero; foreach (var p in points) mid += p; mid /= points.Count;
                var far = points[0]; foreach (var p in points) if ((p - mid).sqrMagnitude > (far - mid).sqrMagnitude) far = p;
                var other = far; foreach (var p in points) if ((p - far).sqrMagnitude > (other - far).sqrMagnitude) other = p;
                var axis = (other - far).normalized;
                var eyes = new PaeteEyeLight { _bone = head };
                int[] count = new int[2];
                for (int i = 0; i < points.Count; i++)
                {
                    int e = Vector3.Dot(points[i] - mid, axis) < 0f ? 0 : 1;
                    eyes._centre[e] += points[i]; eyes._normal[e] += faces[i]; count[e]++;
                }
                if (count[0] == 0 || count[1] == 0) continue;
                for (int e = 0; e < 2; e++) { eyes._centre[e] /= count[e]; eyes._normal[e] = eyes._normal[e].normalized; }
                return eyes;
            }
            return null;
        }

        /// <summary>Eye <paramref name="index"/>'s world centre, a little proud of the face, and its outward direction.</summary>
        public bool TryEye(int index, out Vector3 world, out Vector3 outward)
        {
            world = default; outward = Vector3.forward;
            if (_bone == null) return false;
            outward = _bone.TransformDirection(_normal[index]);
            if (outward.sqrMagnitude < 1e-6f) outward = _bone.forward;
            outward.Normalize();
            world = _bone.TransformPoint(_centre[index]) + outward * 0.03f;
            return true;
        }
    }

    /// <summary>
    /// ⚠️⚠️ HE GLOWS (direction.md 5.14, beat 3). For each body renderer, a SHELL renderer that draws the same mesh on the same bones
    /// in `SpiritVeins.shader` (additive, pulled a hair toward the lens), so the light is on HIS surface: his vines, his lit moss, his
    /// eyes, and a jade edge. ⚠️ A SHELL, NOT AN EXTRA MATERIAL SLOT: material slots map one-to-one onto submeshes, and an extra slot
    /// re-draws only the LAST submesh (`TumbangPreso/Toon`'s header records the same trap for the ink). One material per glow,
    /// driven directly (never through a property block: `MaterialKit.Dress` owns the body's block, `CLAUDE.md` section 87's lesson).
    /// ⚠️ A shell added after first person hid its body would draw inside his own head: <see cref="Sync"/> keeps each shell off
    /// while its source is shadow-only or hidden, every frame.
    /// </summary>
    public sealed class PaeteChannelGlow
    {
        private static readonly int StrengthId = Shader.PropertyToID("_Strength"), SweepId = Shader.PropertyToID("_SweepY"),
            PulseId = Shader.PropertyToID("_PulseY"), PulseStrengthId = Shader.PropertyToID("_PulseStrength"), RimId = Shader.PropertyToID("_Rim"),
            SlotsA = Shader.PropertyToID("_SlotGlow0"), SlotsB = Shader.PropertyToID("_SlotGlow1"), SlotsC = Shader.PropertyToID("_SlotGlow2"),
            SlotsD = Shader.PropertyToID("_SlotGlow3");
        private readonly List<Renderer> _sources = new List<Renderer>(), _shells = new List<Renderer>();
        private readonly Material _material;
        // In play each shell follows its source (first person hides his own body); in the cutscene the stage's capture switch owns them.
        private bool _follow = true;
        public Material Material => _material;
        /// <summary>The least any of it glows, whatever its paint (`SpiritVeins.shader`, `_FlatWeight`): for the limbs he grows.</summary>
        public PaeteChannelGlow Flat(float weight) { if (_material != null) _material.SetFloat("_FlatWeight", weight); return this; }

        private PaeteChannelGlow(Material material) { _material = material; }

        /// <summary>
        /// Shells for every renderer in <paramref name="sources"/>. Each shell is parented to <paramref name="parent"/> when given (the
        /// cutscene's stage, so its capture visibility covers them) or else under its source (so it dies with a rebuilt model).
        /// <paramref name="ownPalette"/> false (the custom character borrowing his kit) keeps only the edge light: its palette slots
        /// are not his, and slot 4 on a person is not a vine.
        /// </summary>
        public static PaeteChannelGlow Attach(IEnumerable<Renderer> sources, Transform parent = null, bool ownPalette = true, bool effects = false)
        {
            var shader = Resources.Load<Shader>("Shaders/SpiritVeins");
            if (shader == null || sources == null) return null;
            var glow = new PaeteChannelGlow(new Material(shader) { name = "PaeteChannelGlow", hideFlags = HideFlags.DontSave }) { _follow = parent == null };
            if (!ownPalette)
            {
                glow._material.SetVector(SlotsA, Vector4.zero); glow._material.SetVector(SlotsB, Vector4.zero);
                glow._material.SetVector(SlotsC, Vector4.zero); glow._material.SetVector(SlotsD, Vector4.zero);
                glow._material.SetFloat(RimId, 1.1f);
            }
            foreach (var source in sources)
            {
                if (source == null || source.GetComponent<PaeteGlowShellTag>() != null || source is ParticleSystemRenderer) continue;
                // An effect's own piece is skipped (a glow on a glow), unless it is asked for by name: the arms he grows are
                // tagged as effects and are still his body (`effects`; they stayed dark until this was added).
                if (!effects && source.gameObject.GetComponent<VfxRenderTag>() != null) continue;
                Renderer shell = null;
                if (source is SkinnedMeshRenderer skin && skin.sharedMesh != null)
                {
                    var go = new GameObject("PaeteChannelGlow");
                    go.transform.SetParent(parent != null ? parent : skin.transform, false);
                    if (parent == null) { go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one; }
                    var s = go.AddComponent<SkinnedMeshRenderer>();
                    s.sharedMesh = skin.sharedMesh; s.bones = skin.bones; s.rootBone = skin.rootBone;
                    s.localBounds = skin.localBounds; s.updateWhenOffscreen = true; s.quality = skin.quality;
                    // ⚠️ SKINNED AGAIN FOR EVERY RENDER, AS THE BODY IT COPIES IS (owner, 2026-10-08, circling him in the wide shot after
                    // the dive: "his ghost and his main body arent aligned"). The cutscene poses his body and renders it at once, by
                    // hand, with the world paused; the body's own renderers are told to re-skin per render for that, and this
                    // shell was not, so it was drawn in the pose of the last engine update: a pose behind whenever he moved.
                    s.forceMatrixRecalculationPerRender = true;
                    shell = s;
                }
                else if (source is MeshRenderer && source.GetComponent<MeshFilter>()?.sharedMesh != null)
                {
                    var go = new GameObject("PaeteChannelGlow");
                    go.transform.SetParent(source.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
                    shell = go.AddComponent<MeshRenderer>();
                }
                if (shell == null) continue;
                shell.gameObject.AddComponent<PaeteGlowShellTag>();
                int count = shell is SkinnedMeshRenderer sk ? sk.sharedMesh.subMeshCount : shell.GetComponent<MeshFilter>().sharedMesh.subMeshCount;
                var mats = new Material[Mathf.Max(1, count)];
                for (int i = 0; i < mats.Length; i++) mats[i] = glow._material;
                shell.sharedMaterials = mats;
                shell.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                shell.receiveShadows = false;
                shell.gameObject.layer = source.gameObject.layer;
                glow._sources.Add(source); glow._shells.Add(shell);
            }
            glow.Set(0f, -1000f, -1000f, 0f);
            return glow;
        }

        /// <summary>The light on him: <paramref name="strength"/> 0 is off; lit above <paramref name="sweepY"/> (world metres); a pulse band at <paramref name="pulseY"/>.</summary>
        private float _wave;

        /// <summary>
        /// Rings of light running across him from one point (`SpiritVeins.shader`, `_WaveFrom`): the point (world), how far
        /// each of three rings has run from it (metres; 0 is none), how thick they are, how bright. Call before `Set`.
        /// </summary>
        public void Waves(Vector3 from, Vector3 radii, float thick, float strength)
        {
            if (_material == null) return;
            _wave = Mathf.Max(0f, strength);
            _material.SetVector("_WaveFrom", new Vector4(from.x, from.y, from.z, _wave));
            _material.SetVector("_WaveRadii", new Vector4(radii.x, radii.y, radii.z, thick));
        }

        public void Set(float strength, float sweepY, float pulseY, float pulseStrength)
        {
            if (_material == null) return;
            _material.SetFloat(StrengthId, Mathf.Max(0f, strength));
            _material.SetFloat(SweepId, sweepY);
            _material.SetFloat(PulseId, pulseY);
            _material.SetFloat(PulseStrengthId, Mathf.Max(0f, pulseStrength));
            for (int i = 0; i < _shells.Count; i++) if (_shells[i] != null) _shells[i].enabled = (strength > 0.002f || _wave > 0.002f) && (!_follow || Shown(i));
        }

        private bool Shown(int i)
        {
            var source = _sources[i];
            return source != null && source.enabled && source.gameObject.activeInHierarchy && !source.forceRenderingOff
                   && source.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }

        /// <summary>Per frame in play: a shell shows only while its source is drawn (first person hides his own body).</summary>
        public void Sync(float strength) { for (int i = 0; i < _shells.Count; i++) if (_shells[i] != null) _shells[i].enabled = strength > 0.002f && (!_follow || Shown(i)); }

        /// <summary>For the cutscene's capture toggle: its own copy's renderers are forced off outside the capture.</summary>
        public IEnumerable<Renderer> Shells => _shells;

        /// <summary>
        /// Each shell takes the mesh its source wears NOW. `PaeteLivingBody` gives his body a private mesh whose vines and
        /// branches move, and a shell left on the first one would light where a vine used to be.
        /// </summary>
        public void Rebind()
        {
            for (int i = 0; i < _shells.Count; i++)
                if (_shells[i] is SkinnedMeshRenderer shell && _sources[i] is SkinnedMeshRenderer source && source.sharedMesh != null)
                    shell.sharedMesh = source.sharedMesh;
        }

        public void Dispose()
        {
            foreach (var shell in _shells) if (shell != null) PaeteProp.Kill(shell.gameObject);
            _shells.Clear(); _sources.Clear();
            if (_material != null) PaeteProp.Kill(_material);
        }
    }

    /// <summary>Marks a channel-glow shell so a second glow never shells a shell.</summary>
    public sealed class PaeteGlowShellTag : MonoBehaviour { }

    /// <summary>
    /// ⚠️⚠️ HIS ROOTS CONNECT TO THE GROUND (direction.md 5.14, beat 2). The braids of his forearms (his arms ARE braided vines) come
    /// apart into roots that spread from each hand like fingers and dig into the court, and two more leave his knee. His own bark, never
    /// something coming up out of the court. Each root is typed: which hand, its compass round his facing, how far it reaches, how high
    /// it arches before it dives, its girth and its delay. They stay in while he channels; on each heave they go TAUT (the arch pulls
    /// straight); when he stands they come out of the court and draw back into the forearms. A bead of light runs down each root on
    /// every pulse, and the court round his hands glows up through hairline cracks and throws out a ring on each pulse.
    /// Everything is posed from the time handed in, in the parent's space (the cutscene's stage, or a world object in play).
    /// </summary>
    public sealed class PaeteGroundRoots
    {
        // (from: 0 left hand, 1 right hand, 2 knee), compass (degrees round his facing, 0 ahead, + to his right), reach (m),
        // arch (m), girth (m), delay (s). Each its own numbers; none is one shape turned round a circle.
        private static readonly (int from, float compass, float reach, float arch, float girth, float delay)[] Rows =
        {
            (0, -38f, 0.64f, 0.15f, 0.062f, 0.00f), (0, 8f, 0.56f, 0.19f, 0.054f, 0.04f), (0, -96f, 0.50f, 0.13f, 0.050f, 0.02f),
            (0, 152f, 0.36f, 0.11f, 0.044f, 0.07f), (1, 34f, 0.62f, 0.17f, 0.060f, 0.01f), (1, -14f, 0.52f, 0.14f, 0.052f, 0.05f),
            (1, 98f, 0.48f, 0.16f, 0.048f, 0.03f), (1, -154f, 0.34f, 0.10f, 0.042f, 0.08f), (2, 168f, 0.44f, 0.10f, 0.052f, 0.09f),
            (2, -132f, 0.38f, 0.09f, 0.046f, 0.11f),
        };
        // The hairline cracks round each hand: compass, length. The light shows up through them.
        private static readonly (float compass, float length)[] CrackRows =
        {
            (-60f, 0.42f), (-10f, 0.34f), (40f, 0.46f), (110f, 0.30f), (175f, 0.38f), (-130f, 0.28f),
        };

        private readonly Transform _root;
        private readonly Mesh[] _meshes = new Mesh[Rows.Length];
        private readonly PaeteLight[] _beads = new PaeteLight[Rows.Length];
        private readonly Mesh[] _crackMeshes = new Mesh[CrackRows.Length * 2];
        private readonly PaeteLight[] _crackGlow = new PaeteLight[CrackRows.Length * 2];
        private readonly PaeteLight[] _rings = new PaeteLight[3];
        /// <summary>
        /// ⚠️ THE PULSE RINGS' SIZE AND STRENGTH, 1 FOR EVERYBODY ELSE AND LESS ON HIS OWN SCREEN (TODO HERO-9, "NEXT" row (4)). In
        /// first person his hands are a metre under his lens, so a ring that swells to 2.2 m at 0.9 filled the bottom half of his
        /// view three times in a row, where from across the court it is a small bright pulse. `PaeteGroundCall` sets 0.45 and 0.35.
        /// </summary>
        public float RingSize = 1f, RingStrength = 1f;

        // ⚠️⚠️ HIS ARMS ARE PLANTED IN THE COURT (owner, 2026-10-08, on a frame of him kneeling with his arms their modelled length
        // and these roots lying at his hands as thin separate pieces: *"instead of keeping the same length, do the same thing you
        // did with the fpv arms and the leap swing where the arms grow out"*, and of what that means here: *"make it so it really
        // looks like his arms are being planted into the ground"*). When his body is bound (`BindBody`), the eight roots typed
        // from his hands above are not drawn: `PaeteRootArms` grows new wood on from the eight strands of his own braided
        // forearms, in his body's own material and paint, straight down into the court at one mouth an arm, and on under it.
        // What this class adds round each mouth is THE COURT GIVING WAY: six slabs of it tipped up by what went in, on a mound of
        // turned soil. They jump as the arms are driven in, settle, and lift again on each of the tree's hauls, as if something
        // under them were pulling. The roots from his knee, the hairline cracks, the beads of light and the rings are as before.
        private PaeteBodyWood _wood;
        private PaeteRootArms _arms;
        private PaeteLivingBody _living;
        /// <summary>False on his own first-person screen, where his body is not drawn (`PaeteGroundCall`).</summary>
        public bool BodyShown = true;
        /// <summary>
        /// The slow growth after the drive (0 to 1: the roots running on under the court and surfacing), how far his vines have
        /// crept down his arms (0 to 1), and when on the caller's clock he began to drop and his palms hit. Play leaves them
        /// alone: it picks up with his roots in and his vines down.
        /// </summary>
        public float Spread = 1f, Creep = 1f, DropAt = -99f, SlamAt = -99f;
        /// <summary>The cutscene's lens is under the court: his limbs, which run on under it, are not drawn there (`PaeteRootArms.Under`).</summary>
        public bool LensUnder { set { if (_arms != null) _arms.Under = value; } }
        /// <summary>The arms grown into the court, once his body is bound: their renderer, so his glow can light them too.</summary>
        public Renderer ArmsRenderer => _arms != null ? _arms.Renderer : null;
        // The slabs round each mouth, six an arm (his left's first): where round the mouth (degrees), how far out (m), width and
        // length (m), how far it tips (degrees), its skew (degrees), which earth, and how late it jumps (of the dig).
        private static readonly (float round, float far, float width, float length, float tilt, float skew, int earth, float late)[] SlabRows =
        {
            (18f, 0.17f, 0.20f, 0.16f, 34f, 9f, 0, 0.00f), (74f, 0.20f, 0.15f, 0.21f, 27f, -14f, 1, 0.03f), (131f, 0.16f, 0.22f, 0.14f, 41f, 6f, 2, 0.01f),
            (188f, 0.19f, 0.17f, 0.19f, 30f, -8f, 0, 0.05f), (247f, 0.15f, 0.19f, 0.15f, 38f, 17f, 1, 0.02f), (306f, 0.21f, 0.14f, 0.22f, 24f, -11f, 2, 0.04f),
            (-27f, 0.18f, 0.18f, 0.18f, 36f, -7f, 1, 0.01f), (39f, 0.15f, 0.21f, 0.15f, 29f, 12f, 2, 0.04f), (102f, 0.20f, 0.16f, 0.20f, 43f, -16f, 0, 0.00f),
            (163f, 0.17f, 0.20f, 0.17f, 26f, 5f, 1, 0.03f), (221f, 0.21f, 0.15f, 0.23f, 33f, -10f, 2, 0.05f), (283f, 0.16f, 0.23f, 0.14f, 39f, 15f, 0, 0.02f),
        };
        private readonly Transform[] _slabs = new Transform[SlabRows.Length], _mounds = new Transform[2];
        private readonly List<Vector3> _points = new List<Vector3>(20);
        private readonly List<float> _radii = new List<float>(20);
        private readonly List<Vector3> _line = new List<Vector3>(4);

        public static readonly Color Light = new Color(0.85f, 1.0f, 0.42f, 1f);

        public PaeteGroundRoots(Transform parent, Color[] palette)
        {
            _root = new GameObject("PaeteGroundRoots").transform;
            _root.SetParent(parent, false);
            Color bark = palette != null && palette.Length == 16 ? palette[13] : GrowthVfx.Bark;
            Color dark = palette != null && palette.Length == 16 ? palette[14] : GrowthVfx.BarkDark;
            Color lit = palette != null && palette.Length == 16 ? palette[15] : GrowthVfx.BarkLit;
            Color vine = palette != null && palette.Length == 16 ? palette[4] : GrowthVfx.Vine;
            var shades = new[] { bark, dark, lit, vine };
            for (int i = 0; i < Rows.Length; i++)
            {
                _meshes[i] = new Mesh { name = "PaeteGroundRoot" };
                _meshes[i].MarkDynamic();
                PaeteInk.Part(_root, "ground-root-" + i, _meshes[i], shades[i % shades.Length]);
                _beads[i] = PaeteLight.Create(_root, "ground-root-bead-" + i, Light, falloff: 2.0f, core: 0.8f, lift: 0.04f);
            }
            var courtDark = new Color(0.20f, 0.14f, 0.09f, 1f);
            for (int i = 0; i < _crackMeshes.Length; i++)
            {
                _crackMeshes[i] = new Mesh { name = "PaeteHandCrack" };
                _crackMeshes[i].MarkDynamic();
                var crack = GrowthVfx.Part(_root, "hand-crack-" + i, _crackMeshes[i], courtDark);
                crack.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _crackGlow[i] = PaeteLight.Create(_root, "hand-crack-light-" + i, Light, _crackMeshes[i], billboard: false, band: true, falloff: 1.4f, core: 0.7f);
            }
            for (int i = 0; i < _rings.Length; i++)
                _rings[i] = PaeteLight.Create(_root, "pulse-ring-" + i, Light, PaeteBands.Ring(40, 0.82f, 1.0f), billboard: false, band: true, falloff: 1.3f, core: 0.4f);
        }

        public Transform Root => _root;

        /// <summary>
        /// Bind his body (its skinned renderers), so his arms themselves grow into the court. False, and the roots typed from his
        /// hands are drawn as before, when the body has no braided forearms to grow from (the custom character borrowing his kit)
        /// or its mesh cannot be read. <paramref name="living"/> also sets the vines and branches of his own mesh moving
        /// (`PaeteLivingBody`): for a body that is this effect's alone to change, which the cutscene's copy is.
        /// </summary>
        public bool BindBody(IList<Renderer> body, Color[] palette, bool living)
        {
            if (_root == null || _arms != null) return _arms != null;
            _wood = PaeteBodyWood.Read(body);
            if (_wood == null || !_wood.HasArms) { _wood = null; return false; }
            _arms = new PaeteRootArms(_root, _wood, palette);
            if (living)
            {
                _living = PaeteLivingBody.Attach(_wood);
                if (_living != null) _root.gameObject.AddComponent<PaeteLivingBodyGuard>().Body = _living;
            }
            var earth = new[] { new Color(0.36f, 0.27f, 0.18f, 1f), new Color(0.30f, 0.22f, 0.15f, 1f), GrowthVfx.Seed };
            var soil = new[] { new Color(0.24f, 0.17f, 0.11f, 1f), new Color(0.21f, 0.15f, 0.10f, 1f) };
            for (int i = 0; i < _slabs.Length; i++)
            {
                _slabs[i] = GrowthVfx.Block(_root, "mouth-slab-" + i, Vector3.one, earth[SlabRows[i].earth]).transform;
                _slabs[i].localScale = Vector3.zero;
            }
            for (int i = 0; i < _mounds.Length; i++)
            {
                var mound = GrowthVfx.Part(_root, "mouth-mound-" + i, VfxShapes.Prism(7, 0.5f, 0.55f, 0.12f, 25f * i + 10f, 71 + i), soil[i]);
                mound.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _mounds[i] = mound.transform;
                _mounds[i].localScale = Vector3.zero;
            }
            return true;
        }

        /// <summary>His arms are bound and drawn into the court now (not on his own first-person screen).</summary>
        public bool ArmsPlanted => _arms != null && _arms.Planted;

        /// <summary>Where arm <paramref name="arm"/> (0 his left) goes into the court, in the parent's space.</summary>
        public Vector3 Mouth(int arm) => _arms != null ? _arms.Entry(arm) : Vector3.zero;

        /// <summary>
        /// Pose at <paramref name="t"/> (seconds, the caller's clock). Hands and knee in the parent's space; <paramref name="facingYaw"/>
        /// the way he faces (degrees, parent space); <paramref name="court"/> the court's height there; <paramref name="grow"/> 0 to 1 how
        /// far the roots have dug in; <paramref name="taut"/> 0 to 1 pulled straight (a heave); <paramref name="retract"/> 0 to 1 drawn back
        /// out of the court into the arms; <paramref name="channel"/> 0 to 1 the light in the court and down the roots;
        /// <paramref name="pulses"/> the times of the three pulses (a bead down each root, then a ring).
        /// </summary>
        public void Pose(float t, Vector3 left, Vector3 right, Vector3 knee, float facingYaw, float court, float grow, float taut, float retract,
                         float channel, IList<float> pulses)
        {
            if (_root == null) return;
            var face = Quaternion.Euler(0f, facingYaw, 0f);
            // HIS ARMS INTO THE COURT (`BindBody`). On his own first-person screen his body is not drawn, so the limbs begin at
            // the court, at the hands handed in, and his first-person arms reach down to them (`PaeteGroundCall`).
            bool arms = _arms != null;
            if (arms)
            {
                _arms.Pose(t, left, right, facingYaw, court, grow, Spread, taut, retract, channel, pulses, Creep, BodyShown);
                if (_arms.Planted) { left = _arms.Entry(0) + Vector3.up * 0.03f; right = _arms.Entry(1) + Vector3.up * 0.03f; }
                PoseMouths(t, face, court, grow, taut, retract, pulses, channel);
            }
            _living?.Pose(t, DropAt, SlamAt, taut, channel, pulses, retract);
            for (int i = 0; i < Rows.Length; i++)
            {
                var row = Rows[i];
                Vector3 from = row.from == 0 ? left : row.from == 1 ? right : knee;
                float g = Mathf.Clamp01((grow * 1.15f - row.delay * 2.2f)) * (1f - Mathf.Clamp01(retract * 1.2f - row.delay));
                if (arms && row.from != 2)
                {
                    // This root is one of his arm's limbs now; the bead of light runs down that instead.
                    _meshes[i].Clear();
                    float lit = 0f, far = 0f;
                    if (pulses != null)
                        foreach (float p in pulses)
                        {
                            float s = (t - p - row.delay * 0.5f) / 0.28f;
                            if (s < 0f || s > 1f) continue;
                            lit = Mathf.Max(lit, Mathf.Sin(s * Mathf.PI)); far = s;
                        }
                    if (lit > 0.01f && channel > 0.01f && _arms.LimbGrown(i) > 0.3f)
                        _beads[i].Set(_arms.LimbPoint(i, far) + Vector3.up * 0.04f, Vector3.one * (0.16f + 0.05f * lit), Quaternion.identity, 1.4f * lit * channel);
                    else _beads[i].Hide();
                    continue;
                }
                if (g <= 0.01f) { _meshes[i].Clear(); _beads[i].Hide(); continue; }
                var dir = face * Quaternion.Euler(0f, row.compass, 0f) * Vector3.forward;
                var side = Vector3.Cross(Vector3.up, dir);
                float start = from.y;
                _points.Clear(); _radii.Clear();
                const int Samples = 14;
                int shown = Mathf.Max(2, Mathf.RoundToInt(Samples * g));
                for (int k = 0; k <= shown; k++)
                {
                    float u = k / (float)Samples;
                    // Out of the hand, over a low arch, and down into the court past its reach (the tip is 12 cm under it). Taut, the
                    // arch pulls nearly straight: a rope hauled on.
                    float arch = row.arch * Mathf.Sin(u * Mathf.PI) * (1f - 0.75f * taut);
                    float fall = Mathf.Lerp(start, court, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u * 1.25f)));
                    float dive = -0.12f * Mathf.SmoothStep(0.72f, 1f, u);
                    float writhe = 0.035f * Mathf.Sin(u * 8f + t * 5f + i) * Mathf.Sin(u * Mathf.PI) * (1f - taut);
                    _points.Add(from + dir * (row.reach * u) + side * writhe + Vector3.up * (fall - from.y + arch + dive));
                    _radii.Add(Mathf.Lerp(row.girth, row.girth * 0.35f, u) * (1f + 0.08f * taut));
                }
                PaeteInk.Tube(_meshes[i], _points, _radii, 5);
                // A bead of light runs down this root on each pulse, from the hand into the court.
                float bead = 0f; int at = 0;
                if (pulses != null)
                    foreach (float p in pulses)
                    {
                        float s = (t - p - row.delay * 0.5f) / 0.28f;
                        if (s < 0f || s > 1f) continue;
                        bead = Mathf.Max(bead, Mathf.Sin(s * Mathf.PI));
                        at = Mathf.Clamp(Mathf.RoundToInt(s * (_points.Count - 1)), 0, _points.Count - 1);
                    }
                if (bead > 0.01f && channel > 0.01f) _beads[i].Set(_points[at] + Vector3.up * 0.03f, Vector3.one * (0.16f + 0.05f * bead), Quaternion.identity, 1.4f * bead * channel);
                else _beads[i].Hide();
            }
            // The hairline cracks round each hand, the light showing up through them.
            for (int h = 0; h < 2; h++)
            {
                Vector3 hand = h == 0 ? left : right;
                for (int c = 0; c < CrackRows.Length; c++)
                {
                    int index = h * CrackRows.Length + c;
                    float open = Mathf.Clamp01(grow * 1.4f - c * 0.08f) * (1f - retract);
                    if (open <= 0.01f) { _crackMeshes[index].Clear(); _crackGlow[index].Hide(); continue; }
                    var row = CrackRows[c];
                    var dir = face * Quaternion.Euler(0f, row.compass + (h == 0 ? -8f : 8f), 0f) * Vector3.forward;
                    _line.Clear();
                    var p0 = new Vector3(hand.x, court, hand.z) + dir * (arms ? 0.24f : 0.10f);
                    _line.Add(p0);
                    float length = row.length * (arms ? 0.55f : 1f);
                    _line.Add(p0 + dir * length * open * 0.5f + Vector3.Cross(Vector3.up, dir) * 0.03f);
                    _line.Add(p0 + dir * length * open);
                    // With his arms in the court these start at the rim of the broken ground and are finer: at their old width
                    // they were the thin dark roots lying at his hands that the owner's frame showed (2026-10-08).
                    PaeteBands.Flat(_crackMeshes[index], _line, arms ? 0.02f : 0.035f, court + 0.012f);
                    float flicker = 0.85f + 0.15f * Mathf.Sin(t * 17f + index * 1.7f);
                    _crackGlow[index].Set(Vector3.up * 0.004f, Vector3.one, Quaternion.identity, 1.1f * channel * open * flicker);
                }
            }
            // A ring of light out of the court on each pulse, from between his hands.
            var mid = (left + right) * 0.5f; mid.y = court + 0.02f;
            for (int r = 0; r < _rings.Length; r++)
            {
                float p = pulses != null && r < pulses.Count ? pulses[r] : -99f;
                float s = (t - p - 0.18f) / 0.45f;
                if (s < 0f || s > 1f || channel <= 0.01f) { _rings[r].Hide(); continue; }
                float radius = (0.4f + 1.8f * s) * RingSize;
                _rings[r].Set(mid, new Vector3(radius, 1f, radius), Quaternion.identity, 0.9f * (1f - s) * channel * RingStrength);
            }
        }

        /// <summary>
        /// THE COURT GIVING WAY ROUND EACH ARM. Six slabs an arm stand round its mouth, each tipped up on its inner edge by what
        /// went in, on a low mound of turned soil. They jump as the arms are driven in (out past their rest and back: the ground
        /// gives), ride each heartbeat a little, and lift again on each haul and settle, each a moment after the one before it.
        /// </summary>
        private void PoseMouths(float t, Quaternion face, float court, float grow, float taut, float retract, IList<float> pulses, float channel)
        {
            float gone = 1f - Mathf.Clamp01(retract * 1.3f);
            float throb = 0f;
            if (pulses != null) foreach (float p in pulses) throb = Mathf.Max(throb, GrowthVfx.Envelope(t, p + 0.04f, 0.05f, p + 0.26f, 0.16f));
            for (int i = 0; i < _slabs.Length; i++)
            {
                var row = SlabRows[i];
                int arm = i / 6;
                float up = GrowthVfx.Pop(Mathf.Clamp01((grow - row.late * 2f) / 0.24f));
                if (up <= 0.001f || gone <= 0.001f || !_arms.Planted) { _slabs[i].localScale = Vector3.zero; continue; }
                // Settled it leans three quarters of its tip; a haul lifts it past that, a heartbeat a little.
                float heave = up * (0.75f + 0.45f * taut + 0.10f * throb * channel) * gone;
                var radial = face * Quaternion.Euler(0f, row.round, 0f) * Vector3.forward;
                float tip = row.tilt * heave;
                _slabs[i].localPosition = _arms.Entry(arm) + radial * (row.far + 0.02f * taut)
                                          + Vector3.up * (0.005f + 0.3f * row.length * Mathf.Sin(tip * Mathf.Deg2Rad) + 0.02f * taut);
                // Its inner edge up, its outer edge in the court.
                _slabs[i].localRotation = Quaternion.LookRotation(radial, Vector3.up) * Quaternion.Euler(tip, row.skew, (i % 2 == 0 ? 5f : -7f) * heave);
                _slabs[i].localScale = new Vector3(row.width, 0.05f, row.length) * Mathf.Clamp01(up * 2f) * Mathf.Clamp01(gone * 2f);
            }
            for (int arm = 0; arm < _mounds.Length; arm++)
            {
                float up = GrowthVfx.Pop(Mathf.Clamp01(grow / 0.2f)) * gone;
                if (up <= 0.001f || !_arms.Planted) { _mounds[arm].localScale = Vector3.zero; continue; }
                _mounds[arm].localPosition = _arms.Entry(arm) + Vector3.up * 0.002f;
                _mounds[arm].localRotation = Quaternion.Euler(0f, arm == 0 ? 20f : 205f, 0f);
                _mounds[arm].localScale = new Vector3(0.27f, (0.22f + 0.10f * taut + 0.03f * throb) * up, 0.25f) * (arm == 0 ? 1f : 0.92f);
            }
        }

        public void Dispose()
        {
            // His body first: the private mesh his vines moved in goes back before anything else is torn down.
            _living?.Dispose(); _living = null;
            _arms?.Dispose(); _arms = null;
            if (_root != null) PaeteProp.Kill(_root.gameObject);
        }
    }

    /// <summary>Flat band and ring meshes laid on the court (uv v runs across, for `SpiritGlow`'s band).</summary>
    public static class PaeteBands
    {
        /// <summary>A flat band along <paramref name="line"/>, <paramref name="width"/> across, at height <paramref name="y"/> (NaN keeps each point's own y).</summary>
        public static void Flat(Mesh mesh, IList<Vector3> line, float width, float y)
        {
            int n = line.Count;
            if (n < 2) { mesh.Clear(); return; }
            var vertices = new Vector3[n * 2]; var uv = new Vector2[n * 2]; var triangles = new int[(n - 1) * 6];
            for (int i = 0; i < n; i++)
            {
                var along = line[Mathf.Min(n - 1, i + 1)] - line[Mathf.Max(0, i - 1)]; along.y = 0f;
                var across = along.sqrMagnitude > 1e-6f ? Vector3.Cross(Vector3.up, along.normalized) : Vector3.right;
                float taper = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(i / 3f)) * Mathf.Lerp(1f, 0.4f, Mathf.Clamp01((i - (n - 3)) / 3f));
                var p = line[i]; if (!float.IsNaN(y)) p.y = y;
                vertices[i * 2] = p + across * width * 0.5f * taper; vertices[i * 2 + 1] = p - across * width * 0.5f * taper;
                float u = i / (float)Mathf.Max(1, n - 1);
                uv[i * 2] = new Vector2(u, 0f); uv[i * 2 + 1] = new Vector2(u, 1f);
                if (i == n - 1) continue;
                int b = i * 6, a = i * 2;
                triangles[b] = a; triangles[b + 1] = a + 2; triangles[b + 2] = a + 1;
                triangles[b + 3] = a + 1; triangles[b + 4] = a + 2; triangles[b + 5] = a + 3;
            }
            mesh.Clear();
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
        }

        /// <summary>A flat ring of unit outer radius in XZ, <paramref name="inner"/> to <paramref name="outer"/>, uv v across it.</summary>
        public static Mesh Ring(int sides, float inner, float outer)
        {
            var mesh = new Mesh { name = "PaeteRing", hideFlags = HideFlags.DontSave };
            var v = new Vector3[(sides + 1) * 2]; var uv = new Vector2[v.Length]; var tris = new int[sides * 6];
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                v[i * 2] = d * inner; v[i * 2 + 1] = d * outer;
                uv[i * 2] = new Vector2(i / (float)sides, 0f); uv[i * 2 + 1] = new Vector2(i / (float)sides, 1f);
                if (i == sides) continue;
                int b = i * 6, k = i * 2;
                tris[b] = k; tris[b + 1] = k + 1; tris[b + 2] = k + 2; tris[b + 3] = k + 2; tris[b + 4] = k + 1; tris[b + 5] = k + 3;
            }
            mesh.vertices = v; mesh.uv = uv; mesh.triangles = tris;
            mesh.RecalculateNormals(); mesh.bounds = new Bounds(Vector3.zero, new Vector3(2f, 0.2f, 2f));
            return mesh;
        }
    }

    /// <summary>
    /// ⚠️⚠️ HIS ROOTS TRAVEL THROUGH THE GROUND (direction.md 5.14, beat 4; it replaces `PaeteRootVein`). Three roots race from under his
    /// hands to where the tree will stand, on the same clock the seed once flew, so the catch lands when it always did. What shows is
    /// what a root tunnelling just under a court would do to it:
    ///  * the court HEAVES over it: slabs of it tip up as the front passes and settle back (a mole's ridge, each slab typed);
    ///  * it SPLITS along the line, and the light is INSIDE the split (a dark crack with a band of his light in it, flickering);
    ///  * its woven back BREAKS THE SURFACE in two or three humps and dives again (his bark, `PaeteRope`);
    ///  * at the front the court bulges and soil is kicked up. ⚠️ THERE IS NO LIGHT AT THE FRONT: `PaeteRootVein` carried a lime block
    ///    there and the owner saw it as a seed ("i also dont like that paete just throws seeds in his ult").
    /// After it arrives the humps sink and the cracks close. `Staged` (the cutscene) poses it only from the scene clock.
    /// </summary>
    public sealed class PaeteRootRidge : MonoBehaviour
    {
        // Per root: sideways bow at the middle (m), a second wave's size and phase, the split's width, and a small start delay.
        private static readonly (float bow, float wave, float phase, float width, float delay)[] Roots =
        {
            (0.50f, 0.15f, 0.4f, 0.16f, 0.00f), (-0.40f, 0.12f, 2.1f, 0.14f, 0.04f), (0.06f, 0.18f, 4.0f, 0.12f, 0.07f),
        };
        // Per root, the slabs that tip up over it: (fraction along, sideways m, width, length, tilt degrees, yaw). Typed.
        private static readonly Vector4[][] Slabs =
        {
            new[] { new Vector4(0.10f, 0.10f, 0.26f, 18f), new Vector4(0.22f, -0.08f, 0.22f, 22f), new Vector4(0.35f, 0.12f, 0.30f, 16f),
                    new Vector4(0.47f, -0.10f, 0.24f, 24f), new Vector4(0.60f, 0.09f, 0.28f, 19f), new Vector4(0.72f, -0.11f, 0.22f, 21f),
                    new Vector4(0.84f, 0.10f, 0.26f, 17f) },
            new[] { new Vector4(0.14f, -0.09f, 0.22f, 20f), new Vector4(0.28f, 0.10f, 0.26f, 17f), new Vector4(0.41f, -0.12f, 0.20f, 23f),
                    new Vector4(0.55f, 0.08f, 0.28f, 18f), new Vector4(0.68f, -0.10f, 0.24f, 22f), new Vector4(0.80f, 0.11f, 0.20f, 16f) },
            new[] { new Vector4(0.08f, 0.08f, 0.20f, 21f), new Vector4(0.20f, -0.10f, 0.24f, 18f), new Vector4(0.33f, 0.09f, 0.22f, 24f),
                    new Vector4(0.46f, -0.08f, 0.26f, 17f), new Vector4(0.58f, 0.12f, 0.20f, 20f), new Vector4(0.71f, -0.09f, 0.28f, 19f),
                    new Vector4(0.86f, 0.10f, 0.22f, 22f) },
        };
        // Per root, where its back breaks the surface: (centre fraction, half-span fraction, height m).
        private static readonly Vector3[][] Humps =
        {
            new[] { new Vector3(0.30f, 0.07f, 0.20f), new Vector3(0.66f, 0.06f, 0.16f) },
            new[] { new Vector3(0.20f, 0.06f, 0.15f), new Vector3(0.52f, 0.08f, 0.22f), new Vector3(0.80f, 0.05f, 0.14f) },
            new[] { new Vector3(0.42f, 0.07f, 0.18f), new Vector3(0.74f, 0.06f, 0.17f) },
        };
        private const int Samples = 40;

        public bool Staged { get; private set; }
        private Vector3 _from, _to;
        private float _seconds, _age, _court;
        private bool _groundFromWorld;
        private readonly List<Vector3>[] _paths = new List<Vector3>[3];
        private readonly Mesh[] _crackMesh = new Mesh[3], _glowMesh = new Mesh[3];
        private readonly PaeteLight[] _glow = new PaeteLight[3];
        private readonly List<Transform>[] _slabs = new List<Transform>[3];
        private readonly List<PaeteRope>[] _humps = new List<PaeteRope>[3];
        private readonly Transform[] _front = new Transform[3];
        private readonly Transform[] _clods = new Transform[6];
        private readonly List<Vector3> _shown = new List<Vector3>(48), _arch = new List<Vector3>(12);
        private Renderer[] _crackRenderers = new Renderer[3];

        /// <summary>
        /// Race from <paramref name="from"/> to <paramref name="to"/> in <paramref name="seconds"/>. In play <paramref name="parent"/> is
        /// null and the points are world points (the court read under each one); in the cutscene the parent is its stage, the points are
        /// in its space and <paramref name="court"/> is the court's height there.
        /// </summary>
        public static PaeteRootRidge Race(Transform parent, Vector3 from, Vector3 to, float seconds, bool staged, float court = float.NaN)
        {
            var go = new GameObject("PaeteRootRidge");
            if (parent != null) go.transform.SetParent(parent, false);
            var ridge = go.AddComponent<PaeteRootRidge>();
            ridge.Staged = staged;
            ridge._from = from; ridge._to = to; ridge._seconds = Mathf.Max(0.1f, seconds);
            ridge._groundFromWorld = parent == null && float.IsNaN(court);
            ridge._court = float.IsNaN(court) ? 0f : court;
            ridge.Build();
            if (!staged)
            {
                // The roots racing under the court have a sound of their own (`tools/build_paete_audio.py` `root_vein`).
                GameServices.Audio?.PlayAt("sfx_paete_root_vein", Vector3.Lerp(from, to, 0.5f));
                PaeteGroundBreak.Spawn(from, 0.55f);
            }
            ridge.Pose(0f);
            return ridge;
        }

        private float CourtAt(Vector3 p) => _groundFromWorld ? Slipper.GroundY(p) : _court;

        private void Build()
        {
            Vector3 d = _to - _from; d.y = 0f;
            Vector3 side = d.sqrMagnitude > 1e-4f ? Vector3.Cross(Vector3.up, d.normalized) : Vector3.right;
            var courtDark = new Color(0.20f, 0.14f, 0.09f, 1f);
            var earth = new[] { new Color(0.36f, 0.27f, 0.18f, 1f), new Color(0.30f, 0.22f, 0.15f, 1f), GrowthVfx.Seed };
            var bark = PaeteProp.Palette;
            Color[] cords = bark != null && bark.Length == 16 ? new[] { bark[14], bark[13], bark[15] } : new[] { GrowthVfx.BarkDark, GrowthVfx.Bark, GrowthVfx.BarkLit };
            for (int r = 0; r < 3; r++)
            {
                var row = Roots[r];
                var path = _paths[r] = new List<Vector3>(Samples + 1);
                for (int k = 0; k <= Samples; k++)
                {
                    float u = k / (float)Samples;
                    // It leaves from under his hands a little apart, wanders, and gathers again where the tree will stand.
                    float wander = row.bow * Mathf.Sin(u * Mathf.PI) + row.wave * Mathf.Sin(u * 9f + row.phase) * Mathf.Sin(u * Mathf.PI);
                    var p = Vector3.Lerp(_from, _to, u) + side * (wander + (r - 1) * 0.12f * (1f - u));
                    p.y = CourtAt(p);
                    path.Add(p);
                }
                _crackMesh[r] = new Mesh { name = "PaeteRidgeCrack" }; _crackMesh[r].MarkDynamic();
                var crack = GrowthVfx.Part(transform, "ridge-crack-" + r, _crackMesh[r], courtDark);
                _crackRenderers[r] = crack.GetComponent<Renderer>();
                _glowMesh[r] = new Mesh { name = "PaeteRidgeLight" }; _glowMesh[r].MarkDynamic();
                _glow[r] = PaeteLight.Create(transform, "ridge-light-" + r, PaeteGroundRoots.Light, _glowMesh[r], billboard: false, band: true, falloff: 1.6f, core: 0.9f);
                _slabs[r] = new List<Transform>();
                for (int s = 0; s < Slabs[r].Length; s++)
                {
                    var slab = GrowthVfx.Block(transform, "ridge-slab", Vector3.one, earth[(r + s) % earth.Length]).transform;
                    _slabs[r].Add(slab);
                }
                _humps[r] = new List<PaeteRope>();
                for (int h = 0; h < Humps[r].Length; h++)
                    _humps[r].Add(new PaeteRope(transform, "ridge-back", cords, new[] { 0.058f, 0.050f, 0.044f }, 0.055f, 6.5f, r * 1.3f + h));
                _front[r] = GrowthVfx.Block(transform, "ridge-bulge", Vector3.one, earth[r % earth.Length]).transform;
            }
            for (int c = 0; c < _clods.Length; c++)
                _clods[c] = GrowthVfx.Block(transform, "ridge-clod", Vector3.one * 0.07f, c % 2 == 0 ? GrowthVfx.Seed : courtDark).transform;
        }

        private void Update()
        {
            if (Staged) return;
            _age += Time.deltaTime;
            Pose(_age);
        }

        /// <summary>Pose at <paramref name="age"/> seconds since the roots left. It sinks back after arriving and removes itself in play.</summary>
        public void Pose(float age)
        {
            float sink = Mathf.Clamp01((age - _seconds - 0.25f) / 0.7f);
            if (!Staged && sink >= 1f) { Destroy(gameObject); return; }
            for (int r = 0; r < 3; r++)
            {
                var row = Roots[r];
                var path = _paths[r];
                float reach = Mathf.Clamp01((age - row.delay) / Mathf.Max(0.05f, _seconds - row.delay));
                // The front eases in (a root pushing off) and races at the end.
                reach = reach * reach * (3f - 2f * reach) * 0.35f + reach * 0.65f;
                int shown = Mathf.Clamp(Mathf.RoundToInt(reach * Samples), 0, Samples);
                _shown.Clear();
                for (int k = 0; k <= shown; k++) _shown.Add(path[k]);
                bool live = _shown.Count >= 2 && sink < 1f;
                // The split, and the light inside it: narrower as it closes.
                if (live)
                {
                    PaeteBands.Flat(_crackMesh[r], _shown, row.width * 0.55f * (1f - 0.8f * sink), float.NaN);
                    OffsetUp(_crackMesh[r], 0.012f);
                    PaeteBands.Flat(_glowMesh[r], _shown, row.width * 0.42f * (1f - sink), float.NaN);
                    OffsetUp(_glowMesh[r], 0.02f);
                    _crackRenderers[r].enabled = true;
                    float flicker = 0.8f + 0.2f * Mathf.Sin(age * 23f + r * 2f);
                    _glow[r].Set(Vector3.zero, Vector3.one, Quaternion.identity, 1.25f * flicker * (1f - sink));
                }
                else { _crackMesh[r].Clear(); _glowMesh[r].Clear(); _crackRenderers[r].enabled = false; _glow[r].Hide(); }
                // The slabs tip up as the front passes, then settle back, and sink flat as it closes.
                for (int s = 0; s < _slabs[r].Count; s++)
                {
                    var row4 = Slabs[r][s];
                    float passed = reach - row4.x;
                    var slab = _slabs[r][s];
                    if (passed < 0f || sink >= 1f) { slab.localScale = Vector3.zero; continue; }
                    float heave = Mathf.Clamp01(passed / 0.06f) * (1f - 0.6f * Mathf.Clamp01((passed - 0.06f) / 0.25f)) * (1f - sink);
                    int k = Mathf.Clamp(Mathf.RoundToInt(row4.x * Samples), 1, Samples - 1);
                    var along = (path[k + 1] - path[k - 1]); along.y = 0f; along.Normalize();
                    var across = Vector3.Cross(Vector3.up, along);
                    var at = path[k] + across * row4.y;
                    slab.localPosition = new Vector3(at.x, at.y + 0.02f + 0.06f * heave, at.z);
                    slab.localRotation = Quaternion.LookRotation(along, Vector3.up) * Quaternion.Euler(-row4.w * heave, 0f, (row4.y > 0f ? 1f : -1f) * 12f * heave);
                    slab.localScale = new Vector3(row4.z, 0.05f, row4.z * 0.8f) * Mathf.Clamp01(heave * 3f + 0.35f);
                }
                // Its woven back breaks the surface in humps and dives again.
                for (int h = 0; h < _humps[r].Count; h++)
                {
                    var hump = Humps[r][h];
                    float grown = Mathf.Clamp01((reach - (hump.x - hump.y)) / (2f * hump.y));
                    if (grown <= 0.02f || sink >= 1f) { _humps[r][h].Clear(); continue; }
                    _arch.Clear();
                    int a0 = Mathf.Clamp(Mathf.RoundToInt((hump.x - hump.y) * Samples), 0, Samples);
                    int a1 = Mathf.Clamp(Mathf.RoundToInt((hump.x - hump.y + 2f * hump.y * grown) * Samples), a0 + 1, Samples);
                    for (int k = a0; k <= a1; k++)
                    {
                        float u = (k - a0) / Mathf.Max(1f, (hump.y * 2f * Samples));
                        var p = path[k];
                        p.y += hump.z * Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI) * (1f - sink) - 0.06f - 0.2f * sink;
                        _arch.Add(p);
                    }
                    _humps[r][h].Draw(_arch, 0.8f);
                }
                // The front: the court bulging over the tip (no light), until it arrives.
                var front = _front[r];
                if (reach > 0.02f && reach < 0.999f && sink <= 0f)
                {
                    var tip = path[shown];
                    float pulse = 0.85f + 0.15f * Mathf.Sin(age * 40f + r);
                    front.localPosition = tip + Vector3.up * 0.03f;
                    var along = shown > 0 ? tip - path[shown - 1] : Vector3.forward; along.y = 0f;
                    front.localRotation = along.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(along.normalized, Vector3.up) : Quaternion.identity;
                    front.localScale = new Vector3(0.30f, 0.09f * pulse, 0.36f);
                }
                else front.localScale = Vector3.zero;
            }
            // Soil kicked up at the fronts: two clods per root on short hops, cycling.
            for (int c = 0; c < _clods.Length; c++)
            {
                int r = c / 2;
                var row = Roots[r];
                float reach = Mathf.Clamp01((age - row.delay) / Mathf.Max(0.05f, _seconds - row.delay));
                if (reach <= 0.02f || reach >= 0.999f) { _clods[c].localScale = Vector3.zero; continue; }
                float cycle = 0.2f;
                float s = Mathf.Repeat(age - row.delay - (c % 2) * cycle * 0.5f, cycle);
                var tip = _paths[r][Mathf.Clamp(Mathf.RoundToInt(reach * Samples), 0, Samples)];
                _clods[c].localPosition = tip + Vector3.up * (0.04f + 2.6f * s - 12f * s * s) + Vector3.right * ((c % 2 == 0 ? 0.1f : -0.08f));
                _clods[c].localRotation = Quaternion.Euler(s * 900f, s * 500f, 0f);
                _clods[c].localScale = Vector3.one * 0.07f;
            }
        }

        private static readonly List<Vector3> Scratch = new List<Vector3>(96);
        private static void OffsetUp(Mesh mesh, float by)
        {
            mesh.GetVertices(Scratch);
            for (int i = 0; i < Scratch.Count; i++) Scratch[i] += Vector3.up * by;
            mesh.SetVertices(Scratch);
            mesh.RecalculateBounds();
        }
    }

    /// <summary>
    /// ⚠️⚠️ IN PLAY, HE STAYS DOWN AND JOINED TO THE GROUND (direction.md 5.14, the live table). Attached to the caster on every peer
    /// when MAKILING'S EMBRACE goes live, after the cutscene hands the world back. It owns what his BODY shows while the roots travel and
    /// the tree crawls out: his roots in the court (from his hands, or from his own viewmodel hands on his screen), the light in him and
    /// in his eyes, and his own view lowered to kneel height. It changes no rule: the tree, the catch and the timing are `PaeteSentry`'s.
    ///
    ///  0 to 0.45    the channel: still down, roots in, a pulse as the roots leave for the spot.
    ///  0.75         the grip: the claws take them; his roots tighten.
    ///  0.85 to 1.95 the heaves: his roots go taut on each of the tree's hauls; the light flares.
    ///  1.95         the rise: his hands come out of the court and the roots draw back into his forearms.
    ///  2.25 on      the embrace, then the light and the eyes fade.
    ///
    /// ⚠️ WALKING CANCELS IT, ALL OF IT (AGENTS.md: an animation never adds rooting). The cast clip is stopped
    /// (`CharacterAnimator.CancelHeroAction`), the roots snap out of the court, the view comes back up; the tree does not care.
    /// </summary>
    public sealed class PaeteGroundCall : MonoBehaviour
    {
        /// <summary>His view drops this far and tips this far down while he kneels (the taya's squat is the precedent: an offset, never the aim).</summary>
        public const float FppEyeDrop = 0.55f, FppLookDown = 8f;
        /// <summary>When he stands (seconds since the cast went live), and when this is gone.</summary>
        public const float RiseAt = 1.95f, Life = 3.1f;
        /// <summary>The tree's hauls, as live seconds: the roots arrive at 0.45 and it heaves at 0.40, 0.80 and 1.20 of its age.</summary>
        public static readonly float[] Heaves = { 0.85f, 1.25f, 1.65f };
        private static readonly float[] Pulses = { 0.05f, 0.75f, 1.25f };

        private CharacterMotor _caster;
        private CharacterAnimator _animator;
        private PaeteGroundRoots _roots;
        private PaeteChannelGlow _glow, _armGlow, _grownGlow;
        private int _grownCount;
        private PaeteEyeLight _eyes;
        private readonly PaeteLight[] _eyeLights = new PaeteLight[2];
        private Transform _stage, _head;
        private Renderer _headRenderer;
        private float _age, _cancelledAt = -1f, _yaw;
        private Vector3 _feet;
        // His body's own arms are bound to the roots (`PaeteGroundRoots.BindBody`): they grow into the court themselves.
        private bool _bound;

        /// <summary>
        /// Start it on <paramref name="caster"/>, facing <paramref name="landing"/>. Returns the court point between his hands, where the
        /// roots leave for the spot.
        /// </summary>
        public static Vector3 Begin(CharacterMotor caster, Vector3 landing)
        {
            if (caster == null) return landing;
            var old = caster.GetComponent<PaeteGroundCall>();
            if (old != null) old.End();
            var call = caster.gameObject.AddComponent<PaeteGroundCall>();
            call._caster = caster;
            call._animator = caster.GetComponent<CharacterAnimator>();
            call._feet = caster.transform.position;
            Vector3 toward = landing - call._feet; toward.y = 0f;
            if (toward.sqrMagnitude < 0.01f) toward = caster.transform.forward;
            call._yaw = Mathf.Atan2(toward.x, toward.z) * Mathf.Rad2Deg;
            call.Build();
            return call.HandsGround();
        }

        /// <summary>0 to 1: how far down his own first-person view is (read by `CameraRig.ApplyFpp`).</summary>
        public static float KneelWeight(CharacterMotor who)
        {
            if (who == null) return 0f;
            var call = who.GetComponent<PaeteGroundCall>();
            return call != null ? call.Kneel : 0f;
        }

        /// <summary>A small lift of his view on each of the tree's hauls, 0 to 1.</summary>
        public static float HeaveJolt(CharacterMotor who)
        {
            if (who == null) return 0f;
            var call = who.GetComponent<PaeteGroundCall>();
            if (call == null || call._cancelledAt >= 0f) return 0f;
            float j = 0f;
            foreach (float h in Heaves) j = Mathf.Max(j, GrowthVfx.Envelope(call._age, h, 0.06f, h + 0.3f, 0.22f));
            return j;
        }

        private float Kneel
        {
            get
            {
                float down = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_age - RiseAt) / 0.3f));
                if (_cancelledAt >= 0f) down *= 1f - Mathf.Clamp01((_age - _cancelledAt) / 0.15f);
                return down;
            }
        }

        private Quaternion Facing => Quaternion.Euler(0f, _yaw, 0f);
        private Vector3 HandsGround()
        {
            var p = _feet + Facing * new Vector3(0f, 0f, 1.0f);
            p.y = Slipper.GroundY(p);
            return p;
        }

        private void Build()
        {
            _stage = new GameObject("PaeteGroundCall").transform;
            _roots = new PaeteGroundRoots(_stage, PaeteProp.Palette);
            var visual = _caster.GetComponent<CharacterVisual>();
            var model = visual != null ? visual.Model : null;
            bool paete = model != null && model.name.ToLowerInvariant().Contains("paete");
            if (model != null)
            {
                var renderers = new List<Renderer>();
                foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                    if (r is SkinnedMeshRenderer) renderers.Add(r);
                _glow = PaeteChannelGlow.Attach(renderers, null, paete);
                foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name == "head") _head = t;
                if (paete) _eyes = PaeteEyeLight.Find(renderers, _head);
                foreach (var r in renderers) { _headRenderer = r; break; }
                // His arms themselves go into the court (not the custom character's: it has no braid to grow from). ⚠️ Not
                // `living`: that swaps his body's mesh for a private one, and the live body is not this effect's alone (a
                // replay may copy it mid-kneel and be left holding a mesh that is destroyed when he stands).
                if (paete) _bound = _roots.BindBody(renderers, PaeteProp.Palette, living: false);
            }
            // On HIS screen, his own first-person hands glow too.
            if (CameraSystem.CameraRig.TryViewmodelArmRenderers(_caster, out var left, out var right))
                _armGlow = PaeteChannelGlow.Attach(new Renderer[] { left, right }, null, paete);
            for (int e = 0; e < 2; e++)
                _eyeLights[e] = PaeteLight.Create(_stage, "PaeteLiveEye" + e, new Color(0.82f, 1.0f, 0.55f, 1f), falloff: 2.0f, core: 1.3f, lift: 0.10f);
        }

        private void Update()
        {
            if (_caster == null) { End(); return; }
            _age += Time.deltaTime;
            if (_age >= Life) { End(); return; }
            // Walking cancels it (an animation never roots him): the clip stops, the roots snap out, the view comes up.
            bool moving = Moving();
            if (_cancelledAt < 0f && _age > 0.1f && _age < 2.6f && moving)
            {
                _cancelledAt = _age;
                _animator?.CancelHeroAction("hero-paete-sentry", "ground-call");
                var mid = HandsGround();
                PaeteBarkShatter.Spawn(mid + Vector3.up * 0.15f, 5);
            }
            Pose();
        }

        private Vector3 _lastPos;
        private bool _havePos;

        /// <summary>
        /// Is he walking off? ⚠️ Read from where his body actually GOES between frames, not from `CharacterMotor.Velocity`: on a peer
        /// that does not simulate him his body is moved by snapshots and his motor's own velocity says nothing, so the kneel would never
        /// end on anybody else's screen. His own machine also reads the stick, so it ends the instant he pushes.
        /// </summary>
        private bool Moving()
        {
            var p = _caster.transform.position;
            float dt = Time.deltaTime, speed = 0f;
            if (_havePos && dt > 1e-4f) { var d = p - _lastPos; d.y = 0f; speed = d.magnitude / dt; }
            _lastPos = p; _havePos = true;
            bool pushing = _caster.Intent != null && _caster.IsLocallySimulated() && _caster.Intent.Move.sqrMagnitude > 0.04f;
            return speed > 1.2f || pushing;
        }

        private void Pose()
        {
            // His roots: from his own first-person hands on his screen, from his body's hands everywhere else.
            var face = Facing;
            // Where his straight arms come down (they were 0.40 and 0.38 out, under arms held wide).
            Vector3 left = _feet + face * new Vector3(-0.27f, 0f, 1.0f), right = _feet + face * new Vector3(0.25f, 0f, 1.02f);
            // ⚠️ With his arms bound (2026-10-08) the roots no longer leave his first-person hands, wherever his view has them:
            // his arms are PLANTED, at two places in the court that do not move when he looks about, and on his own screen it
            // is his first-person arms that reach down to those places (below, `PaeteVineHands.ReachTo`, the leap's own limbs).
            if (!_bound && CameraSystem.CameraRig.TryViewmodelHand(_caster, true, out var vl) && CameraSystem.CameraRig.TryViewmodelHand(_caster, false, out var vr))
            { left = vl; right = vr; }
            else
            {
                left.y = Slipper.GroundY(left) + 0.06f; right.y = Slipper.GroundY(right) + 0.06f;
            }
            var knee = _feet + face * new Vector3(0.22f, 0f, -0.42f);
            knee.y = Slipper.GroundY(knee) + 0.04f;
            float court = Slipper.GroundY(HandsGround());
            float rise = Mathf.Clamp01((_age - RiseAt) / 0.22f);
            float cancel = _cancelledAt >= 0f ? Mathf.Clamp01((_age - _cancelledAt) / 0.12f) : 0f;
            float retract = Mathf.Max(rise, cancel);
            float grow = Mathf.Clamp01(_age / 0.14f);
            float taut = 0f;
            foreach (float h in Heaves) taut = Mathf.Max(taut, GrowthVfx.Envelope(_age, h - 0.05f, 0.08f, h + 0.3f, 0.18f));
            taut = Mathf.Max(taut, GrowthVfx.Envelope(_age, 0.72f, 0.05f, 0.95f, 0.15f));
            float channel = (1f - Mathf.Clamp01((_age - RiseAt) / 0.6f)) * (1f - cancel);
            // In his own first-person view the pulse rings stay a small faint pulse round his hands (see `RingSize`).
            bool ownView = _headRenderer != null && _headRenderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            _roots.RingSize = ownView ? 0.45f : 1f;
            _roots.RingStrength = ownView ? 0.35f : 1f;
            _roots.BodyShown = !ownView;
            // His vines and branches strain while he is down with his arms in the court (`PaeteVineBones`, the bones of 2026-10-08).
            PaeteVineBones.Strain(_caster.transform, (1f - retract) * (0.7f + 0.3f * taut));
            _roots.Pose(_age, left, right, knee, _yaw, court, grow, taut, retract, channel, Pulses);
            // On his own screen: each first-person arm grows its limbs (the leap's, `PaeteVineHands.BuildReach`) down to where
            // that arm goes into the court, and 45 cm under it (a limb comes to a point over its last stretch, and that point
            // should be under the court, the limb still stout where it passes the surface). ⚠️ NOT SEEN: nothing films his own
            // view outside Play. The
            // call is good for one frame, so when this ends his arms are his own again.
            if (_bound && ownView && _roots.ArmsPlanted)
                for (int arm = 0; arm < 2; arm++)
                    // ⚠️ TO THE MOUTH ITSELF, NOT UNDER IT (owner, 2026-10-08, in first person: "fpv planted arms dont align with the
                    // ground hole stamps"). They were aimed 45 cm under the court so the limb would still be stout at the surface;
                    // but his first-person arms are drawn OVER the world, the court hides none of them, and a point under the
                    // court is seen nearer his feet than the hole is: the limbs ended on bare court short of both holes.
                    CameraSystem.PaeteVineHands.ReachTo(_caster, arm == 0, _roots.Mouth(arm) + Vector3.up * 0.04f, Mathf.Clamp01(grow * (1f - retract)));
            // The light in him: all of him lit, flaring on each haul; a pulse down his arms as the roots leave and at the grip.
            float flare = 0f;
            foreach (float h in Heaves) flare = Mathf.Max(flare, GrowthVfx.Envelope(_age, h, 0.05f, h + 0.35f, 0.25f));
            float strength = (0.9f + 0.5f * flare) * (1f - Mathf.Clamp01((_age - (RiseAt + 0.3f)) / 0.8f)) * (1f - cancel);
            float feetY = _caster.transform.position.y;
            float pulseY = feetY - 10f, pulseStrength = 0f;
            foreach (float p in Pulses)
            {
                float s = (_age - p) / 0.3f;
                if (s < 0f || s > 1f) continue;
                pulseY = Mathf.Lerp(feetY + 1.7f, feetY + 0.05f, s); pulseStrength = 1.6f * Mathf.Sin(s * Mathf.PI);
            }
            _glow?.Set(strength, -1000f, pulseY, pulseStrength);
            _glow?.Sync(strength);
            // ⚠️ WHAT HE HAS GROWN GLOWS WITH HIM (owner, 2026-10-08, looking down in first person at lit arms ending in unlit
            // limbs: "glow isnt applying to his vine extensions"). The arms planted in the court and the first-person limbs
            // that reach down to them are built after the glow was hung on his body and his hands, and the first-person ones
            // only when they first reach; so they get a glow of their own, made again whenever one more of them exists.
            var grown = new List<Renderer>();
            if (_roots != null && _roots.ArmsRenderer != null) grown.Add(_roots.ArmsRenderer);
            foreach (var reach in CameraSystem.PaeteVineHands.ReachRenderers) if (reach != null) grown.Add(reach);
            if (grown.Count != _grownCount)
            {
                _grownGlow?.Dispose();
                _grownGlow = grown.Count > 0 ? PaeteChannelGlow.Attach(grown, null, true, effects: true)?.Flat(.7f) : null;
                _grownCount = grown.Count;
            }
            _grownGlow?.Set(strength * 0.9f, -1000f, pulseY, pulseStrength);
            _grownGlow?.Sync(strength * 0.9f);
            if (_armGlow != null)
            {
                var eye = Camera.main != null ? Camera.main.transform.position.y : feetY + 1.2f;
                float armPulse = feetY - 10f;
                foreach (float p in Pulses)
                {
                    float s = (_age - p) / 0.3f;
                    if (s >= 0f && s <= 1f) armPulse = Mathf.Lerp(eye - 0.1f, eye - 0.9f, s);
                }
                _armGlow.Set(strength * 0.8f, -1000f, armPulse, pulseStrength * 0.8f);
                _armGlow.Sync(strength * 0.8f);
            }
            // His eyes, lit (not on his own screen: in first person they are inside his view).
            float eyes = (1f - Mathf.Clamp01((_age - 2.6f) / 0.5f)) * (1f - cancel * 0.6f) * (1f + 0.4f * flare);
            for (int e = 0; e < 2; e++)
            {
                if (_eyes == null || ownView || !_eyes.TryEye(e, out var world, out var outward)) { _eyeLights[e].Hide(); continue; }
                _eyeLights[e].Set(world, new Vector3(0.13f, 0.055f, 1f), Quaternion.identity, 1.2f * eyes, outward);
            }
        }

        private void End()
        {
            _roots?.Dispose(); _roots = null;
            _glow?.Dispose(); _glow = null;
            _armGlow?.Dispose(); _armGlow = null;
            _grownGlow?.Dispose(); _grownGlow = null; _grownCount = 0;
            if (_stage != null) PaeteProp.Kill(_stage.gameObject);
            PaeteProp.Kill(this);
        }

        private void OnDestroy()
        {
            _roots?.Dispose(); _glow?.Dispose(); _armGlow?.Dispose(); _grownGlow?.Dispose();
            if (_stage != null) PaeteProp.Kill(_stage.gameObject);
        }
    }
}
