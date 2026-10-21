using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // Observes the existing payload, including loose shoes and replica snapshots.
    // Never changes the shoe's state, mesh or authored material array.
    //
    // BOULDER's crust, beat by beat (`docs/reports/dante-presentation-2026-10-16/plan.md` section 3), from the age this peer
    // has seen the load for (the host's deadline does not cross the wire; a late joiner sees it settled):
    //  * TELL/RELEASE: ten typed grit pieces rise off the court into his palm; the crust closes over the slipper in three
    //    steps from the palm outward (`_Creep`).
    //  * THE CRUST IS STONE YOU CAN SEE (owner on kit v1, 2026-10-17, of the loaded slipper on his screen: "what even is this it
    //    looks horrible i dont get wtf its supposed to do what it does what it communicates"). v1 only painted a grey plate
    //    pattern onto the slipper's own surface, so nothing read as a rock. Now thirteen typed basalt chunks pack round its
    //    heel, rims, sole and toe (heel first, then the middle, then the toe), the strap and buckle left showing so it is still
    //    his slipper, and molten seams glow between them: a stone in a sandal, the throw that hits like a rock.
    //  * LINGER: a slow molten seam; a grain drops off it when he runs.
    //  * WARNING (last 3 s of the 15): the seam cools, molten to dull red to grey, and the crust flakes at its edges.
    //  * TRAVEL: a short grit trail behind the flying slipper.
    //  * CONTACT: on a player the crust bursts into six chunks (the victim's pebble ring is `ConcussedPebbles`); on the court
    //    it cracks and drops four (the payload stays loaded on a loose slipper, so the crust stays too: the picture never
    //    lies about the rule); expired in hand, it flakes away.
    public sealed class DanteBoulderCoating : MonoBehaviour
    {
        private static Shader _shader;
        private Slipper _shoe;
        private bool _world;
        private MeshFilter _target, _copy;
        private Renderer _source;
        private MeshRenderer _renderer;
        private Mesh _mesh;
        private Material _material;
        private Vector3 _along, _across, _normal, _centre;
        private float _length, _width, _thick;
        private Transform[] _chunks = new Transform[0];
        private Renderer[] _chunkRenderers = new Renderer[0];
        private MaterialPropertyBlock _block;
        private bool _wasLoaded;
        private float _loadedAge, _grainClock;
        private SlipperState _lastState;
        private CharacterMotor _lastHolder;
        private static bool MountainHeldBy(CharacterMotor holder)
            => holder != null && holder.AbilitySystem?.Kit is Abilities.DanteHeroKit dante && dante.IsMountainForm;
        private Vector3 _lastPos;
        private const float LoadSeconds = Core.GeoRules.BoulderLoadSeconds, WarnSeconds = 3f;

        public static void TrackWorld(Slipper shoe)
        {
            if (_shader == null) _shader = Resources.Load<Shader>("Shaders/BoulderLoad");
            var effect = shoe.GetComponent<DanteBoulderCoating>();
            if (effect == null) effect = shoe.gameObject.AddComponent<DanteBoulderCoating>();
            effect._world = true;
            effect._shoe = shoe;
        }

        /// <summary>
        /// MOUNTAIN FORM's slipper is stone too (owner, 2026-10-18: a stronger throw that hits like a rock): held by Dante in the
        /// form, and on the flight it leaves his hand on. Read from the holder's kit, which every peer ticks, so nothing extra
        /// crosses the wire.
        /// </summary>
        public static bool MountainHeld(Slipper shoe)
            => shoe != null && shoe.State == SlipperState.Held && shoe.Holder != null
               && shoe.Holder.AbilitySystem?.Kit is Abilities.DanteHeroKit dante && dante.IsMountainForm;

        private bool _mountainFlight;
        private bool Loaded()
        {
            if (_shoe == null || !_shoe.isActiveAndEnabled) return false;
            // ⚠️ OUR BOULDER STANDS IN FRONT OF THIS ONE (owner 2026-10-17, after a side-by-side film: "we're still keeping our
            // design"). A Concussed shoe is `DanteBoulderStone`'s while `DanteBoulderStone.Rocks` is on, so this crust, its
            // bursts, its grit and its trail all stand down for it (the guard is here rather than only in `Sample` because
            // `Beats` reads the same answer, and both looks must never draw on one shoe). MOUNTAIN FORM's stone is a different
            // rule and is not ours to switch off: it still draws, on any shoe that is not Boulder-loaded.
            if (_shoe.Affinity == SlipperAffinity.Concussed) return !DanteBoulderStone.Rocks;
            return MountainHeld(_shoe) || (_mountainFlight && _shoe.State == SlipperState.InFlight);
        }

        public static void MatchOwner(MeshFilter target, Slipper shoe)
        {
            var effect = target.GetComponent<DanteBoulderCoating>();
            if (effect == null)
            {
                if (shoe == null || (shoe.Affinity != SlipperAffinity.Concussed && !MountainHeld(shoe))) return;
                effect = target.gameObject.AddComponent<DanteBoulderCoating>();
            }
            effect._shoe = shoe;
            effect._target = target;
            effect.Sample();
        }

        private void LateUpdate() { Beats(); Sample(); }
        private void OnDisable() { if (_renderer != null) _renderer.enabled = false; HideChunks(); }
        private void HideChunks() { foreach (var c in _chunks) if (c != null && c.gameObject.activeSelf) c.gameObject.SetActive(false); }
        private void Sample()
        {
            bool loaded = Loaded() && (_world || _shoe.State == SlipperState.Held);
            if (!loaded)
            {
                if (_renderer != null) _renderer.enabled = false;
                HideChunks();
                return;
            }
            if (_target == null && _world) _target = _shoe.GetComponentInChildren<MeshFilter>();
            if (_target == null || _target.sharedMesh == null) return;
            if (_renderer == null)
            {
                if (_shader == null) throw new System.InvalidOperationException("Boulder load shader is missing.");
                var go = new GameObject("BoulderStoneInlay");
                go.transform.SetParent(_target.transform, false);
                _copy = go.AddComponent<MeshFilter>();
                _renderer = go.AddComponent<MeshRenderer>();
                _renderer.receiveShadows = false; _renderer.forceRenderingOff = true;
                _material = new Material(_shader) { name = "Boulder stone inlay" };
                VfxRenderTag.Own(go, _material);
                _block = new MaterialPropertyBlock();
            }
            _source = _target.GetComponent<Renderer>();
            if (_mesh != _target.sharedMesh) { RefreshMesh(); BuildChunks(); }
            // The crust's own clock: three steps closing over .25 to .45 s, the last 3 s cooling and flaking.
            float creep = _loadedAge < .25f ? 0f : _loadedAge < .32f ? 1f : _loadedAge < .39f ? 2f : 3f;
            float late = Mathf.Clamp01((_loadedAge - (LoadSeconds - WarnSeconds)) / WarnSeconds);
            if (_world && _shoe.State != SlipperState.Held) { creep = 3f; late = 0f; }
            if (_shoe.Affinity != SlipperAffinity.Concussed) late = 0f;   // MOUNTAIN FORM's stone does not cool on a 15 s load
            _block.SetFloat("_Creep", creep);
            _block.SetFloat("_Cool", late);
            _block.SetFloat("_Flake", late * late * .85f);
            _renderer.SetPropertyBlock(_block);
            _renderer.gameObject.layer = _target.gameObject.layer;
            bool shown = _source == null || _source.enabled;
            // The painted seam layer is retired (kit film r5: its plate outlines read as orange rectangles on the slipper); the
            // magma is each chunk's own molten joint now. Its renderer stays as the chunks' parent and the payload's visible flag
            // (enabled exactly while the stone shows), but never draws its own surface (`forceRenderingOff` below).
            _renderer.enabled = shown;
            // The chunks close in the same three steps, and break off one by one as it flakes (the rubble is `Beats`).
            for (int i = 0; i < _chunks.Length; i++)
            {
                if (_chunks[i] == null) continue;
                bool on = shown && creep >= Chunks[i].Group + 1 && late * late * .85f < (i + 1) / (Chunks.Length + 1f);
                if (_chunks[i].gameObject.activeSelf != on) _chunks[i].gameObject.SetActive(on);
                _chunks[i].gameObject.layer = _target.gameObject.layer;
            }
            if (_source != null)
            {
                _renderer.forceRenderingOff = true;
                _renderer.shadowCastingMode = _source.shadowCastingMode == ShadowCastingMode.ShadowsOnly
                    ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.Off;
                foreach (var r in _chunkRenderers)
                    if (r != null) { r.forceRenderingOff = _source.forceRenderingOff; r.shadowCastingMode = _source.shadowCastingMode; }
            }
        }

        /// <summary>The crust's clock on every copy; the bursts, grains and trail on the world shoe only (the first-person copy only wears the crust).</summary>
        private void Beats()
        {
            if (_shoe == null) return;
            var state = _shoe.State;
            // A MOUNTAIN FORM slipper keeps its stone for the flight it leaves his hand on.
            if (state == SlipperState.InFlight && _lastState == SlipperState.Held && MountainHeldBy(_lastHolder)) _mountainFlight = true;
            else if (state != SlipperState.InFlight) _mountainFlight = false;
            _lastHolder = _shoe.Holder;
            bool loaded = Loaded();
            Vector3 at = _shoe.transform.position;
            float dt = Time.deltaTime;
            if (loaded && !_wasLoaded)
            {
                _loadedAge = 0f;
                if (_world && state == SlipperState.Held) DanteBoulderGrit.Gather(at);
            }
            if (loaded) _loadedAge += dt;
            if (!_world) { _wasLoaded = loaded; return; }
            if (!loaded && _wasLoaded)
            {
                // Spent on a body in flight: burst. Expired in hand: flake away.
                if (_lastState == SlipperState.InFlight) DanteStatusShatter.Rubble(at, at - _lastPos, 6, 1f, false);
                else DanteStatusShatter.Rubble(at, Vector3.down, 3, .6f, true);
            }
            else if (loaded && _lastState == SlipperState.InFlight && state != SlipperState.InFlight && state != SlipperState.Held)
                DanteStatusShatter.Rubble(at, at - _lastPos, 4, .8f, true);     // landed on the court: it cracks, chips drop
            if (loaded)
            {
                float speed = dt > 0f ? (at - _lastPos).magnitude / dt : 0f;
                _grainClock -= dt;
                if (state == SlipperState.InFlight && _grainClock <= 0f) { DanteBoulderGrit.Drop(at, true); _grainClock = .045f; }
                else if (state == SlipperState.Held && speed > 3.2f && _grainClock <= 0f) { DanteBoulderGrit.Drop(at, false); _grainClock = .42f; }
            }
            _wasLoaded = loaded; _lastState = state; _lastPos = at;
        }

        // Where each chunk packs on: which face it pushes out of, where along the slipper (u, -.5 heel to .5 toe), its
        // footprint and height as fractions of the slipper's width, its turn, and the step it closes on (0 heel, 1 middle, 2 toe).
        private enum Face { Top, Bottom, Right, Left, Heel, Toe }
        private struct Chunk
        {
            public Face Face; public float U, V, Foot, Height, Yaw; public int Group; public Color Tone;
            public Chunk(Face face, float u, float v, float foot, float height, float yaw, int group, float tone)
            { Face = face; U = u; V = v; Foot = foot; Height = height; Yaw = yaw; Group = group; Tone = new Color(.20f * tone, .16f * tone, .13f * tone, 1f); }
        }
        private static readonly Chunk[] Chunks =
        {
            new Chunk(Face.Heel, -.5f, 0f, .80f, .45f, 12f, 0, 1.15f), new Chunk(Face.Top, -.36f, .05f, .62f, .40f, -20f, 0, 1f),
            new Chunk(Face.Right, -.30f, .1f, .50f, .38f, 30f, 0, .9f), new Chunk(Face.Left, -.32f, -.1f, .48f, .36f, -8f, 0, 1.2f),
            new Chunk(Face.Bottom, -.30f, 0f, .70f, .25f, 5f, 0, .85f),
            new Chunk(Face.Right, -.02f, -.1f, .48f, .34f, 15f, 1, 1.1f), new Chunk(Face.Left, .04f, .1f, .46f, .32f, -25f, 1, .95f),
            new Chunk(Face.Bottom, 0f, .05f, .72f, .24f, 40f, 1, 1.05f),
            new Chunk(Face.Toe, .5f, 0f, .70f, .40f, -10f, 2, 1f), new Chunk(Face.Top, .38f, -.08f, .55f, .34f, 25f, 2, 1.25f),
            new Chunk(Face.Right, .30f, 0f, .42f, .30f, -30f, 2, 1f), new Chunk(Face.Left, .32f, 0f, .40f, .30f, 10f, 2, .9f),
            new Chunk(Face.Bottom, .30f, 0f, .60f, .22f, -15f, 2, 1.1f),
        };

        /// <summary>
        /// The stone: each chunk a low, lumpy five-sided rock pushing out of its face, its open base sunk into the slipper, a molten
        /// joint where it meets it. Each one sits on the slipper's REAL surface (kit film r5: placed from the mesh's bounds they
        /// floated off the first-person slipper like debris): the vertex furthest out along its face at its place along the sole,
        /// sized from the slipper's own width there.
        /// </summary>
        private void BuildChunks()
        {
            foreach (var c in _chunks) if (c != null) Destroy(c.gameObject);
            var parts = new System.Collections.Generic.List<Renderer>(Chunks.Length * 2);
            _chunks = new Transform[Chunks.Length];
            var verts = _mesh.isReadable ? _mesh.vertices : null;
            for (int i = 0; i < Chunks.Length; i++)
            {
                var k = Chunks[i];
                Vector3 outward, side;
                switch (k.Face)
                {
                    case Face.Top: outward = _normal; side = _along; break;
                    case Face.Bottom: outward = -_normal; side = _along; break;
                    case Face.Right: outward = _across; side = _along; break;
                    case Face.Left: outward = -_across; side = _along; break;
                    case Face.Heel: outward = -_along; side = _across; break;
                    default: outward = _along; side = _across; break;
                }
                // The surface point and the slipper's width at this chunk's place.
                Vector3 at = _centre + _along * (k.U * _length) + outward * (k.Face == Face.Top || k.Face == Face.Bottom ? _thick : _width) * .5f;
                float sliceWidth = _width;
                if (verts != null && verts.Length > 0)
                {
                    bool end = k.Face == Face.Heel || k.Face == Face.Toe;
                    float best = float.NegativeInfinity, lo = float.PositiveInfinity, hi = float.NegativeInfinity;
                    foreach (var v in verts)
                    {
                        var d = v - _centre;
                        if (!end && Mathf.Abs(Vector3.Dot(d, _along) / Mathf.Max(_length, .0001f) - k.U) > .09f) continue;
                        float a = Vector3.Dot(d, _across);
                        lo = Mathf.Min(lo, a); hi = Mathf.Max(hi, a);
                        float score = Vector3.Dot(d, outward) - Mathf.Abs(a / Mathf.Max(_width, .0001f) - k.V) * .3f * _width;
                        if (score > best) { best = score; at = v; }
                    }
                    if (hi > lo && !end) sliceWidth = Mathf.Clamp(hi - lo, _width * .5f, _width);
                }
                // Big enough to overlap their neighbours into one packed crust (kit film r6: at about half this they read as knobs, a
                // spiked slipper, not a stone).
                float height = k.Height * sliceWidth * .95f, foot = k.Foot * sliceWidth * 1.1f;
                var turn = Quaternion.LookRotation(side, outward) * Quaternion.Euler(0f, k.Yaw, 0f);
                var holder = new GameObject("BoulderChunk" + i).transform;
                holder.SetParent(_renderer.transform, false);
                holder.localPosition = at; holder.localRotation = turn;
                // The rock, a third of it sunk into the slipper so it is packed on, not resting on it.
                var rock = new GameObject("Rock").transform; rock.SetParent(holder, false);
                rock.localPosition = Vector3.up * (height * .15f);
                rock.localScale = new Vector3(foot, height, foot);
                rock.gameObject.AddComponent<MeshFilter>().sharedMesh = VfxShapes.Prism(5, 1f, .78f, .3f, 0f, 11 + i * 7);
                var r = rock.gameObject.AddComponent<MeshRenderer>(); VfxMaterial.Solid(r, k.Tone); parts.Add(r);
                // Its molten joint: a thin hot band just proud of its foot, the magma squeezed out between stone and slipper.
                var joint = new GameObject("Joint").transform; joint.SetParent(holder, false);
                joint.localPosition = Vector3.up * (height * -.3f);
                joint.localScale = new Vector3(foot * 1.1f, height * .2f, foot * 1.1f);
                joint.gameObject.AddComponent<MeshFilter>().sharedMesh = VfxShapes.Prism(5, 1f, 1f, .3f, 0f, 11 + i * 7);
                var j = joint.gameObject.AddComponent<MeshRenderer>(); VfxMaterial.Solid(j, new Color(1f, .42f, .08f), .9f); parts.Add(j);
                _chunks[i] = holder;
            }
            _chunkRenderers = parts.ToArray();
        }

        private void RefreshMesh()
        {
            _mesh = _target.sharedMesh;
            _copy.sharedMesh = _mesh;
            var materials = new Material[Mathf.Max(1, _mesh.subMeshCount)];
            for (int i = 0; i < materials.Length; i++) materials[i] = _material;
            _renderer.sharedMaterials = materials;
            Vector3 size = _mesh.bounds.size;
            Vector3 along = size.x >= size.y && size.x >= size.z ? Vector3.right : size.y >= size.z ? Vector3.up : Vector3.forward;
            Vector3 normal = size.y <= size.x && size.y <= size.z ? Vector3.up : size.x <= size.z ? Vector3.right : Vector3.forward;
            if (Mathf.Abs(Vector3.Dot(along, normal)) > .5f) normal = along == Vector3.up ? Vector3.right : Vector3.up;
            Vector3 across = Vector3.Cross(normal, along);
            float length = Mathf.Max(size.x, size.y, size.z);
            float width = Mathf.Abs(Vector3.Dot(size, across));
            _along = along; _across = across; _normal = normal; _centre = _mesh.bounds.center;
            _length = length; _width = width; _thick = Mathf.Abs(Vector3.Dot(size, normal));
            _block.SetVector("_Centre", _mesh.bounds.center);
            _block.SetVector("_Along", along / Mathf.Max(length, .0001f));
            _block.SetVector("_Across", across / Mathf.Max(width, .0001f));
            _block.SetFloat("_Lift", Mathf.Max(length * .002f, .0001f));
            _renderer.SetPropertyBlock(_block);
        }
    }
}
