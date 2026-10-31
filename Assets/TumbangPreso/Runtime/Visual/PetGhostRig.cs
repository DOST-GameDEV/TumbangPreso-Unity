using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ PEEKER'S MOUSE AND FETCHER'S BEAR, POSED AS PARTS, THE WAY KURO IS (owner 2026-10-29 on the kit film k1: *"the peeker
    /// looks to stiff unlike how we did kuro.. its a single rigid model, but i need it to be more ghost like and jumpy, with an
    /// animated face preferably"*; and *"bear ghost also doesnt look too ghosty.. looks more like a regular pet tbh"*, which he
    /// clarified as *"looks/acts"*).
    ///
    /// `GhostPetCompanion` is the reference: Kuro is separate named parts (body, eyes, mouth, tail tiers, arm wisps) posed
    /// every frame by springs, bob, squash and stretch; his eyes blink by their own height; his faces are small extra meshes
    /// shown one set at a time (`KuroExpressions`). This is the same, for two of Nemu's skill ghosts:
    ///  * THE PARTS come from `nemu_petghost_{mouse,bear}_parts.glb` (`tools/build_nemu_ghost_parts.py`): the approved head,
    ///    a body that tapers into two tail tiers (no feet), ears, the mouse's paws and cloth, and the face. Every node's
    ///    position is its pivot, read off the model here, so no attach number is typed twice.
    ///  * THE TAIL IS CLOTH IN WATER: each tier is a damped spring that trails the way the ghost moves and sways on its own
    ///    clock, the lower tier later and further, and it thins toward its tip (`_Fade` per tier) while the head stays solid
    ///    enough to read on the sunlit court.
    ///  * THE HEAD (a "core" holding the head and its face) squashes and stretches with speed and takes the caller's hop,
    ///    peek and shimmy (<see cref="CoreOffset"/>, <see cref="CoreEuler"/>, <see cref="CoreSquash"/>); the ears are springs
    ///    that whip when the head turns.
    ///  * THE FACE: it blinks (by eye height, Kuro's way), its eyes look at <see cref="LookAt"/> within the face, and it wears
    ///    one of a few authored faces (<see cref="Eyes"/>, <see cref="MouthShape"/>), switched by turning renderers on and off.
    ///
    /// ⚠️ GPU-FRIENDLY AND COSMETIC. Static meshes posed by transforms; no mesh is rebuilt and nothing is allocated per frame.
    /// The caller poses the ROOT exactly where it always posed the one-piece body (the skill's own point, the lens pose in the
    /// victim's camera, the hold point the slipper rides), so no rule, number or message changes and every peer draws it from
    /// state it already has. Behind <see cref="Enabled"/> (true); false is the one-piece body as before.
    /// </summary>
    public sealed class PetGhostRig
    {
        /// <summary>The parts rigs for the mouse and the bear. False puts the one-piece pet bodies back, unchanged.</summary>
        public static bool Enabled = true;
        public static bool On => Enabled && PetGhostModels.PetGhosts;

        public enum Pet { Mouse, Bear }
        public enum Face { Plain, Sneak, Wide, Happy }
        public enum Mouth { None, Small, Sneak, Grin, Giggle }

        // ---- what the caller sets each frame
        public Face Eyes = Face.Plain;
        public Mouth MouthShape = Mouth.Small;
        /// <summary>Mouse: its cloth, 0 pushed up on its brow (peeking), 1 down over its eyes (hiding).</summary>
        public float Cover = 1f;
        /// <summary>Mouse: the cloth has dropped round its neck (sprung).</summary>
        public bool ClothAtNeck;
        /// <summary>Where its eyes look (world), or null for straight ahead.</summary>
        public Vector3? LookAt;
        /// <summary>The head's own hop, peek and shimmy, in the root's space (metres, degrees, scale).</summary>
        public Vector3 CoreOffset, CoreEuler, CoreSquash = Vector3.one;
        /// <summary>The ears: +1 pricked up and forward, 0 at rest, -1 flat back.</summary>
        public float Ears;
        /// <summary>Mouse: its paws, 0 tucked, 1 reaching forward (the lunge), 2 up on a face (the cling).</summary>
        public float Paws;

        public Transform Root { get; }
        public int Renderers => _all.Count;

        private const string Folder = "Models/NemuGhosts";
        private sealed class Part
        {
            public Transform T; public MeshRenderer R; public Vector3 Rest; public Quaternion RestRot; public Vector3 RestScale;
            public float Fade = 1f; public bool Shown;
        }
        private float _flash, _clock;
        private readonly Dictionary<string, Part> _parts = new Dictionary<string, Part>();
        private readonly List<Part> _all = new List<Part>();
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private readonly Pet _pet;
        private Transform _core, _body;
        private Part _tail0, _tail1, _earL, _earR, _cloth, _clothNeck, _pawL, _pawR;
        private Vector3 _lastRoot, _vel, _lookShift;
        private Quaternion _lastRot = Quaternion.identity;
        private Vector2 _t0, _t0v, _t1, _t1v, _ear, _earV;
        private float _seed;
        private bool _started, _visible = true;
        private static readonly int FadeId = Shader.PropertyToID("_Fade"), FlashId = Shader.PropertyToID("_Flash"),
            ClockId = Shader.PropertyToID("_Clock"), SwayId = Shader.PropertyToID("_Sway"), TrailId = Shader.PropertyToID("_Trail"),
            ShadeKeepId = Shader.PropertyToID("_ShadeKeep"), RimId = Shader.PropertyToID("_Rim");
        private static readonly Dictionary<Pet, Material> Materials = new Dictionary<Pet, Material>();
        private readonly Color _rim;

        /// <summary>Hangs the pet's parts under <paramref name="root"/> (the transform the skill already poses). Null when the
        /// model is missing, so the caller keeps its one-piece body.</summary>
        public static PetGhostRig Build(Transform root, Pet pet, Color rim)
        {
            var model = pet == Pet.Mouse ? "nemu_petghost_mouse_parts" : "nemu_petghost_bear_parts";
            var prefab = Resources.Load<GameObject>(Folder + "/" + model);
            if (prefab == null) { Debug.LogError("[PetGhostRig] " + Folder + "/" + model + " is missing; run tools/build_nemu_ghost_parts.py."); return null; }
            var material = MaterialFor(pet, model);
            if (material == null) return null;
            return new PetGhostRig(root, pet, prefab, material, rim);
        }

        private static Material MaterialFor(Pet pet, string model)
        {
            if (Materials.TryGetValue(pet, out var m) && m != null) return m;
            var shader = Shader.Find("TumbangPreso/PetGhost");
            if (shader == null) { Debug.LogError("[PetGhostRig] Shaders/PetGhost is missing from the build (GameBuilder)."); return null; }
            m = new Material(shader) { name = "PetGhostRig-" + model };
            var atlas = Resources.Load<Texture2D>(Folder + "/" + model + "-atlas");
            if (atlas != null) m.SetTexture("_MainTex", atlas);
            // The tail is bent by its own parts here, so the shader's own bead sway is off.
            m.SetFloat("_TailTop", 0f);
            Materials[pet] = m;
            return m;
        }

        private PetGhostRig(Transform root, Pet pet, GameObject prefab, Material material, Color rim)
        {
            Root = root; _pet = pet; _rim = rim;
            _seed = pet == Pet.Mouse ? .37f : 1.9f;
            var pivots = new Dictionary<string, (Vector3 at, Mesh mesh)>();
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
            {
                var f = t.GetComponent<MeshFilter>();
                if (f == null || f.sharedMesh == null) continue;
                pivots[t.name] = (prefab.transform.InverseTransformPoint(t.position), f.sharedMesh);
            }
            Vector3 headAt = pivots.TryGetValue("head", out var h) ? h.at : Vector3.zero;
            Vector3 bodyAt = pivots.TryGetValue("body", out var b) ? b.at : Vector3.zero;
            _core = Node("Core", root, headAt);
            _coreHome = headAt;
            _body = Node("Wisp", root, bodyAt);
            Part Make(string name, Transform parent, Vector3 parentAt)
            {
                if (!pivots.TryGetValue(name, out var p)) return null;
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = p.at - parentAt;
                go.AddComponent<MeshFilter>().sharedMesh = p.mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                var part = new Part { T = go.transform, R = r, Rest = go.transform.localPosition, RestRot = Quaternion.identity, RestScale = Vector3.one };
                _parts[name] = part; _all.Add(part);
                return part;
            }
            Make("body", _body, bodyAt);
            _tail0 = Make("tail-0", _body, bodyAt);
            if (_tail0 != null) { _tail0.Fade = .88f; _tail1 = Make("tail-1", _tail0.T, pivots["tail-0"].at); if (_tail1 != null) _tail1.Fade = .68f; }
            foreach (var name in pivots.Keys)
            {
                if (_parts.ContainsKey(name) || name == "body" || name.StartsWith("tail")) continue;
                bool underBody = name.StartsWith("paw") || name == "cloth-neck";
                Make(name, underBody ? _body : _core, underBody ? bodyAt : headAt);
            }
            _parts.TryGetValue("ear-l", out _earL); _parts.TryGetValue("ear-r", out _earR);
            _parts.TryGetValue("cloth", out _cloth); _parts.TryGetValue("cloth-neck", out _clothNeck);
            _parts.TryGetValue("paw-l", out _pawL); _parts.TryGetValue("paw-r", out _pawR);
            VfxRenderTag.Attach(root.gameObject);
        }

        private static Transform Node(string name, Transform parent, Vector3 at)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            return go.transform;
        }

        /// <summary>Shows or hides every part at once (the lens hides, the skill's fade reaches nothing).</summary>
        public void Visible(bool on)
        {
            _visible = on;
            foreach (var p in _all) if (!on) p.R.enabled = false;
        }

        private static float Spring(ref float x, ref float v, float target, float stiff, float damp, float dt)
        {
            v += ((target - x) * stiff - v * damp) * dt;
            x += v * dt;
            return x;
        }

        /// <summary>
        /// Poses every part for this frame, after the caller has posed the root. <paramref name="clock"/> is the ghost's own
        /// age (every peer has it), <paramref name="fade"/> and <paramref name="flash"/> what the one-piece body would have worn.
        /// </summary>
        public void Pose(float clock, float dt, float fade, float flash)
        {
            if (Root == null) return;
            dt = Mathf.Clamp(dt, 0f, .05f);
            Vector3 at = Root.position;
            Quaternion rot = Root.rotation;
            if (!_started || dt <= 0f) { _lastRoot = at; _lastRot = rot; _started = true; }
            Vector3 v = dt > 0f ? (at - _lastRoot) / dt : Vector3.zero;
            if (v.sqrMagnitude > 400f) v = Vector3.zero;   // a teleport (the lens pose) is not a motion
            _vel = Vector3.Lerp(_vel, v, 1f - Mathf.Exp(-10f * dt));
            float turn = dt > 0f ? Quaternion.Angle(_lastRot, rot) / dt : 0f;
            _lastRoot = at; _lastRot = rot;
            Vector3 local = Quaternion.Inverse(rot) * _vel;
            float scale = Mathf.Max(.01f, Root.lossyScale.y);
            local /= scale;
            float speed = local.magnitude;

            // ---- the head: squash and stretch with speed (long along its motion, thin across), and the caller's acting.
            float stretch = Mathf.Clamp(speed * .035f, 0f, .22f);
            Vector3 along = new Vector3(1f - stretch * .45f, 1f - stretch * .25f + Mathf.Clamp(local.y * .03f, -.08f, .12f), 1f + stretch);
            _core.localScale = Vector3.Scale(CoreSquash, along);
            _core.localRotation = Quaternion.Euler(CoreEuler);
            _core.localPosition = _coreHome + CoreOffset;

            // ---- the tail: two damped springs trailing the motion and swaying on their own clock, the lower later.
            float t = clock + _seed;
            Vector2 drag = new Vector2(Mathf.Clamp(local.z * 6f + Mathf.Abs(local.y) * 2f, -35f, 55f), Mathf.Clamp(-local.x * 6f, -40f, 40f));
            Vector2 sway0 = new Vector2(4f * Mathf.Sin(t * 2.3f), 9f * Mathf.Sin(t * 3.1f));
            Vector2 sway1 = new Vector2(6f * Mathf.Sin(t * 2.3f - 1.1f), 15f * Mathf.Sin(t * 3.1f - 1.2f));
            Spring(ref _t0.x, ref _t0v.x, drag.x * .6f + sway0.x, 90f, 9f, dt); Spring(ref _t0.y, ref _t0v.y, drag.y * .6f + sway0.y, 90f, 9f, dt);
            Spring(ref _t1.x, ref _t1v.x, drag.x * .5f + sway1.x + _t0.x * .3f, 55f, 6f, dt); Spring(ref _t1.y, ref _t1v.y, drag.y * .5f + sway1.y + _t0.y * .3f, 55f, 6f, dt);
            if (_tail0 != null) _tail0.T.localRotation = Quaternion.Euler(_t0.x, 0f, _t0.y);
            if (_tail1 != null) _tail1.T.localRotation = Quaternion.Euler(_t1.x, 0f, _t1.y);
            _body.localRotation = Quaternion.Euler(Mathf.Clamp(local.z * 2f, -10f, 14f), 0f, Mathf.Clamp(-local.x * 2f, -10f, 10f));

            // ---- the ears: springs that whip when the head turns, pricked or flattened by the caller.
            Spring(ref _ear.x, ref _earV.x, -Ears * 28f + Mathf.Clamp(turn * .04f, 0f, 25f) + 5f * Mathf.Sin(t * 1.7f), 140f, 10f, dt);
            Spring(ref _ear.y, ref _earV.y, -Ears * 6f + Mathf.Clamp(local.z * 3f, -10f, 20f), 120f, 9f, dt);
            if (_earL != null) _earL.T.localRotation = Quaternion.Euler(_ear.x + _ear.y, 0f, _ear.x * .5f);
            if (_earR != null) _earR.T.localRotation = Quaternion.Euler(_ear.x + _ear.y, 0f, -_ear.x * .5f);

            // ---- the mouse's cloth: slid up onto its brow to peek, down over its eyes to hide; round its neck once it springs.
            if (_cloth != null)
            {
                float up = 1f - Mathf.Clamp01(Cover);
                _cloth.T.localPosition = _cloth.Rest + Vector3.up * (.068f * PetGhostModels.Scale * up);
                // Pushed up onto the rounded crown it hugs a little tighter.
                _cloth.T.localScale = new Vector3(1f - .05f * up, 1f, 1f - .05f * up);
            }
            if (_pawL != null) PosePaw(_pawL, -1f, t);
            if (_pawR != null) PosePaw(_pawR, 1f, t);

            // ---- the face: which set shows, the blink (by eye height, Kuro's way), and where the eyes look.
            float blink = Blink(t);
            Vector3 shift = Vector3.zero;
            if (LookAt.HasValue)
            {
                Vector3 d = _core.InverseTransformPoint(LookAt.Value) - _coreFaceCentre();
                if (d.z > .05f) shift = new Vector3(Mathf.Clamp(d.x / d.z, -1f, 1f), Mathf.Clamp(d.y / d.z, -1f, 1f), 0f) * (.013f * PetGhostModels.Scale);
            }
            _lookShift = Vector3.Lerp(_lookShift, shift, 1f - Mathf.Exp(-16f * dt));

            foreach (var p in _all)
            {
                string n = p.T.name;
                bool show = true;
                if (n.StartsWith("eye")) show = EyeShows(n);
                else if (n.StartsWith("mouth")) show = MouthShows(n);
                else if (n == "cloth") show = !ClothAtNeck;
                else if (n == "cloth-neck") show = ClothAtNeck;
                if (n.StartsWith("eye") || n.StartsWith("mouth"))
                {
                    p.T.localPosition = p.Rest + (n.StartsWith("eye") ? _lookShift : _lookShift * .3f);
                    bool blinks = n.StartsWith("eye") && !n.StartsWith("eye-happy");
                    p.T.localScale = blinks ? new Vector3(1f, 1f - .9f * blink, 1f) : Vector3.one;
                }
                p.Shown = show;
                Shade(p, fade, flash, clock);
            }
            _flash = flash; _clock = clock;
        }

        /// <summary>The same pose seen at another fade (a camera that sees it fainter); nothing moves.</summary>
        public void Refade(float fade)
        {
            foreach (var p in _all) Shade(p, fade, _flash, _clock);
        }

        private void Shade(Part p, float fade, float flash, float clock)
        {
            {
                float f = Mathf.Clamp01(fade) * p.Fade;
                p.R.enabled = _visible && p.Shown && f > .005f;
                if (!p.R.enabled) return;
                _block.SetFloat(FadeId, f);
                _block.SetFloat(FlashId, flash);
                _block.SetFloat(ClockId, clock);
                _block.SetFloat(SwayId, 0f);
                _block.SetVector(TrailId, Vector4.zero);
                _block.SetFloat(ShadeKeepId, 1f);
                _block.SetColor(RimId, _rim);
                p.R.SetPropertyBlock(_block);
            }
        }

        /// <summary>The core's rest: the head's pivot, where it was made. The caller's offset is added to it each frame.</summary>
        private Vector3 _coreHome;

        private Vector3 _coreFaceCentre()
        {
            if (_parts.TryGetValue("eye-l", out var l) && _parts.TryGetValue("eye-r", out var r)) return (l.Rest + r.Rest) * .5f;
            return Vector3.zero;
        }

        private void PosePaw(Part paw, float side, float t)
        {
            // Tucked under its chin, paddling a little; out in front at the lunge; up on the victim's cheeks at the cling.
            float reach = Mathf.Clamp01(Paws), cheek = Mathf.Clamp01(Paws - 1f);
            Vector3 paddle = new Vector3(0f, .008f * Mathf.Sin(t * 6f + side), .006f * Mathf.Sin(t * 6f + side + 1f)) * PetGhostModels.Scale;
            Vector3 forward = new Vector3(side * -.01f, .03f, .06f) * PetGhostModels.Scale * reach;
            Vector3 up = new Vector3(side * .03f, .09f + .01f * Mathf.Sin(t * 18f + side), .05f) * PetGhostModels.Scale * cheek;
            paw.T.localPosition = paw.Rest + paddle + forward + up;
            paw.T.localRotation = Quaternion.Euler(-50f * reach - 40f * cheek, 0f, side * 20f * cheek);
        }

        private bool EyeShows(string n)
        {
            string set = Eyes == Face.Sneak ? "eye-sneak-" : Eyes == Face.Wide ? "eye-wide-" : Eyes == Face.Happy ? "eye-happy-" : "eye-";
            // The mouse is the only one with a sneaky face; on the bear "sneak" is its plain eyes.
            if (Eyes == Face.Sneak && !_parts.ContainsKey("eye-sneak-l")) set = "eye-";
            return n.StartsWith(set) && n.Length == set.Length + 1;
        }

        private bool MouthShows(string n)
        {
            switch (MouthShape)
            {
                case Mouth.None: return false;
                case Mouth.Sneak: return _parts.ContainsKey("mouth-sneak") ? n == "mouth-sneak" : n == "mouth";
                case Mouth.Grin: return n == "mouth-grin";
                case Mouth.Giggle: return n == "mouth-giggle";
                default: return n == "mouth";
            }
        }

        /// <summary>A blink every few seconds, now and then a double one; 0 open, 1 shut. From its own clock, so every peer agrees.</summary>
        private float Blink(float t)
        {
            float period = _pet == Pet.Mouse ? 2.9f : 3.6f;
            float c = Mathf.Repeat(t, period);
            float b = Mathf.Clamp01(1f - Mathf.Abs(c - (period - .3f)) / .07f);
            if (Mathf.Repeat(t, period * 3f) > period * 2f) b = Mathf.Max(b, Mathf.Clamp01(1f - Mathf.Abs(c - (period - .12f)) / .06f));
            return b;
        }
    }

    /// <summary>
    /// What the mouse and the bear DO with their parts: the acting, shared by the skills and by the court film so the film
    /// shows the game's own choreography. Every number is a function of the ghost's own clock and state.
    /// </summary>
    public static class PetGhostActing
    {
        public enum MousePhase { Waiting, Lunging, Covering, LettingGo, Sinking }

        /// <summary>
        /// ⚠️ THE MOUSE IS A NERVOUS, PLAYFUL HIDER (owner: *"more ghost like and jumpy"*). While it waits it is never still:
        ///  * DARTS: every 1.9 s a little hop in the air, wound up first (it dips and squashes), sprung (it stretches as it goes
        ///    up and sideways), and settled with an overshoot as it lands on nothing; each hop goes the other way.
        ///  * PEEKS: a 3.6 s game of hide and seek with its cloth: hidden under it, shivering; the cloth flicks up onto its brow
        ///    (a spring, it overshoots), it peeks one way, then the other, sly eyed and smirking, then DUCKS: the cloth snaps
        ///    down and it squashes into it, ears flat, and pops back up.
        ///  * Sprung, it lunges wide eyed and grinning, ears back, paws out; it clings giggling, shimmying with each giggle,
        ///    paws on the victim's cheeks; it lets go still giggling. Unsprung it ducks into its cloth and sinks.
        /// Adds the hop to <paramref name="at"/> (drawing only: the spot and the trigger never move) and sets the rig.
        /// </summary>
        public static void Mouse(PetGhostRig rig, MousePhase phase, float age, float phaseAge, Quaternion facing, ref Vector3 at)
        {
            if (rig == null) return;
            rig.CoreOffset = Vector3.zero; rig.CoreEuler = Vector3.zero; rig.CoreSquash = Vector3.one;
            rig.ClothAtNeck = phase != MousePhase.Waiting && phase != MousePhase.Sinking;
            float k = PetGhostModels.Scale;
            switch (phase)
            {
                case MousePhase.Waiting:
                {
                    // The dart.
                    int hop = Mathf.FloorToInt(age / 1.9f);
                    float u = age - hop * 1.9f, side = (hop & 1) == 0 ? 1f : -1f;
                    Vector3 off = Vector3.zero; Vector3 squash = Vector3.one;
                    if (u < .14f) { float a = Mathf.Sin(u / .14f * Mathf.PI * .5f); off.y = -.035f * a; squash = new Vector3(1f + .12f * a, 1f - .16f * a, 1f + .12f * a); }
                    else if (u < .42f)
                    {
                        float a = (u - .14f) / .28f;
                        off = new Vector3(side * .13f * Ease(a), .17f * Mathf.Sin(a * Mathf.PI) - .035f * (1f - a), 0f);
                        float s = Mathf.Sin(a * Mathf.PI) * .17f; squash = new Vector3(1f - s * .5f, 1f + s, 1f - s * .5f);
                    }
                    else
                    {
                        float a = u - .42f, ring = Mathf.Exp(-9f * a) * Mathf.Sin(a * 24f);
                        off = new Vector3(side * .13f * (1f - Ease(Mathf.Clamp01((u - .9f) / .9f))), -.025f * ring, 0f);
                        squash = new Vector3(1f + .10f * ring, 1f - .13f * ring, 1f + .10f * ring);
                    }
                    at += facing * (off * k);
                    rig.CoreSquash = squash;
                    // The peek.
                    float p = Mathf.Repeat(age + .6f, 3.6f), cover = 1f, yaw = 0f, look = 0f;
                    rig.Ears = 0f;
                    if (p < 1f) { cover = 1f; rig.CoreEuler = new Vector3(0f, 0f, 2.5f * Mathf.Sin(age * 31f)); rig.Ears = -.4f; }
                    else if (p < 1.25f) { float a = (p - 1f) / .25f; cover = 1f - Overshoot(a); rig.Ears = Overshoot(a); }
                    else if (p < 1.95f) { cover = 0f; yaw = 30f * Overshoot(Mathf.Clamp01((p - 1.25f) / .2f)); look = -1f; rig.Ears = 1f; }
                    else if (p < 2.65f) { cover = 0f; yaw = 30f - 60f * Overshoot(Mathf.Clamp01((p - 1.95f) / .2f)); look = 1f; rig.Ears = 1f; }
                    else if (p < 2.85f)
                    {
                        float a = (p - 2.65f) / .2f;
                        cover = Ease(Mathf.Clamp01(a * 2f)); yaw = -30f * (1f - a); rig.Ears = -1f;
                        rig.CoreOffset = new Vector3(0f, -.05f * k * Mathf.Sin(a * Mathf.PI), 0f);
                        rig.CoreSquash = Vector3.Scale(rig.CoreSquash, new Vector3(1f + .14f * Mathf.Sin(a * Mathf.PI), 1f - .2f * Mathf.Sin(a * Mathf.PI), 1f + .14f * Mathf.Sin(a * Mathf.PI)));
                    }
                    else rig.Ears = -.3f;
                    rig.Cover = cover;
                    rig.CoreEuler += new Vector3(0f, yaw, -yaw * .2f);
                    rig.Eyes = PetGhostRig.Face.Sneak; rig.MouthShape = PetGhostRig.Mouth.Sneak;
                    rig.Paws = 0f;
                    // With no one to watch it sweeps the way it peeks.
                    if (!rig.LookAt.HasValue && look != 0f) rig.LookAt = at + facing * new Vector3(look * 2f, 0f, 2f);
                    break;
                }
                case MousePhase.Lunging:
                {
                    float a = Mathf.Clamp01(phaseAge / .22f);
                    rig.Eyes = PetGhostRig.Face.Wide; rig.MouthShape = PetGhostRig.Mouth.Grin; rig.Ears = -1f; rig.Paws = 1f;
                    rig.CoreSquash = new Vector3(1f - .12f * a, 1f - .08f * a, 1f + .3f * a);
                    break;
                }
                case MousePhase.Covering:
                {
                    // Three giggles a second, each a little bounce and a shimmy, fading and coming back.
                    float g = Mathf.Abs(Mathf.Sin(phaseAge * 3f * Mathf.PI));
                    float shimmy = Mathf.Sin(phaseAge * 22f) * (.5f + .5f * g);
                    rig.Eyes = PetGhostRig.Face.Happy; rig.MouthShape = PetGhostRig.Mouth.Giggle; rig.Paws = 2f;
                    rig.Ears = .5f + .5f * Mathf.Sin(phaseAge * 9f);
                    rig.CoreOffset = new Vector3(shimmy * .008f, .012f * g, 0f) * k;
                    rig.CoreEuler = new Vector3(-6f * g, 0f, 11f * shimmy);
                    rig.CoreSquash = new Vector3(1f + .06f * g, 1f - .07f * g, 1f + .06f * g);
                    break;
                }
                case MousePhase.LettingGo:
                {
                    rig.Eyes = PetGhostRig.Face.Happy; rig.MouthShape = PetGhostRig.Mouth.Giggle; rig.Paws = 1f; rig.Ears = 1f;
                    rig.CoreEuler = new Vector3(0f, 0f, 14f * Mathf.Sin(phaseAge * 18f));
                    break;
                }
                default:
                {
                    // Never sprung: it ducks into its cloth and sinks.
                    rig.Cover = 1f; rig.Ears = -1f; rig.Eyes = PetGhostRig.Face.Sneak; rig.MouthShape = PetGhostRig.Mouth.Sneak;
                    rig.CoreSquash = new Vector3(1.1f, .85f, 1.1f);
                    break;
                }
            }
        }

        public enum BearPhase { Outbound, Reaching, Carrying, Offering, Leaving }

        /// <summary>
        /// The bear's face and head (owner: *"looks/acts"* like a regular pet): it looks at the slipper on its way out, eyes
        /// wide as it reaches; it looks at Nemu bringing it back; it BEAMS holding it out (happy eyes, a grin, bobbing
        /// eagerly); it gives a little wave going (the wave is its arm, posed by the skill), still beaming.
        /// </summary>
        public static void Bear(PetGhostRig rig, BearPhase phase, float age, float lift)
        {
            if (rig == null) return;
            rig.CoreOffset = Vector3.zero; rig.CoreEuler = Vector3.zero; rig.CoreSquash = Vector3.one;
            float k = PetGhostModels.Scale;
            switch (phase)
            {
                case BearPhase.Outbound: rig.Eyes = PetGhostRig.Face.Plain; rig.MouthShape = PetGhostRig.Mouth.None; rig.Ears = .6f; break;
                case BearPhase.Reaching: rig.Eyes = PetGhostRig.Face.Wide; rig.MouthShape = PetGhostRig.Mouth.Small; rig.Ears = 1f; break;
                case BearPhase.Carrying:
                    rig.Eyes = PetGhostRig.Face.Plain; rig.MouthShape = PetGhostRig.Mouth.Small; rig.Ears = .3f;
                    // Snatched and floating back up: the head rises off its paws and settles (`lift` 1 at the grab, easing to 0).
                    rig.CoreOffset = new Vector3(0f, .05f * k * lift, 0f);
                    rig.CoreSquash = new Vector3(1f - .08f * lift, 1f + .14f * lift, 1f - .08f * lift);
                    break;
                case BearPhase.Offering:
                {
                    float bob = Mathf.Sin(age * 6f);
                    rig.Eyes = PetGhostRig.Face.Happy; rig.MouthShape = PetGhostRig.Mouth.Grin; rig.Ears = 1f;
                    rig.CoreOffset = new Vector3(0f, (.02f + .015f * bob) * k, 0f);
                    rig.CoreEuler = new Vector3(-8f, 0f, 6f * Mathf.Sin(age * 3f));
                    rig.CoreSquash = new Vector3(1f - .03f * bob, 1f + .05f * bob, 1f - .03f * bob);
                    break;
                }
                default: rig.Eyes = PetGhostRig.Face.Happy; rig.MouthShape = PetGhostRig.Mouth.Giggle; rig.Ears = 1f; break;
            }
        }

        private static float Ease(float u) => u * u * (3f - 2f * u);
        /// <summary>0 to 1 past the end and back: a spring that overshoots and settles.</summary>
        private static float Overshoot(float u) { u = Mathf.Clamp01(u); return 1f - Mathf.Exp(-6f * u) * Mathf.Cos(u * 9f) * (1f - u); }
    }

    /// <summary>
    /// ⚠️ THE BEAR GLIDES, IT DOES NOT RUN (owner on FETCHER: *"looks/acts"* like a regular pet, a dog fetching). The rule's path
    /// and speed are untouched; this is only where its body is DRAWN against that path:
    ///  * it flies HIGH over the court while it is far from the slipper and SWOOPS down onto it as it closes in, so it is on
    ///    the rule's own point at the grab;
    ///  * a FLOAT LAG: the drawn body is a damped spring behind the rule's point, so it overshoots and settles on every turn;
    ///  * it LEANS into its path and BANKS into its turns, and FLICKERS for a moment as it whips round;
    ///  * once it has the slipper there is no lag and no height (its paws are on the slipper the host moves); the head and
    ///    tail carry the life instead.
    /// One per bear, made once; nothing allocated per frame.
    /// </summary>
    public sealed class BearGlide
    {
        private Vector3 _lag, _lagV, _lastPoint, _lastVel;
        private float _height, _weight, _flick, _bank;
        private bool _started;

        /// <summary>The drawn offset from the rule's point (world), the extra lean and bank, and how much it flickers (0 to 1).</summary>
        public Vector3 Offset { get; private set; }
        public float Lean { get; private set; }
        public float Bank => _bank;
        public float Flicker => _flick;

        /// <param name="free">1 outbound (it may lag and fly high), 0 when its paws must be on the rule's point.</param>
        /// <param name="goalDistance">How far it still has to go to the slipper (flat), for the swoop.</param>
        public void Step(Vector3 point, Quaternion heading, float free, float goalDistance, float dt)
        {
            if (!_started || dt <= 0f) { _lastPoint = point; _started = true; Offset = Vector3.zero; return; }
            Vector3 vel = (point - _lastPoint) / dt;
            if (vel.sqrMagnitude > 900f) vel = _lastVel;
            Vector3 accel = (vel - _lastVel) / dt;
            _lastPoint = point; _lastVel = vel;
            _weight = Mathf.MoveTowards(_weight, Mathf.Clamp01(free), dt * (free > _weight ? 2.5f : 9f));
            // The float lag: thrown back against every change of velocity, then pulled home, overshooting.
            _lagV += (-_lag * 60f - _lagV * 7f - accel * .02f) * dt;
            _lag += _lagV * dt;
            if (_lag.sqrMagnitude > .36f) _lag = _lag.normalized * .6f;
            // High while it is far, swooping down over the last two metres.
            float far = Mathf.SmoothStep(0f, 1f, (goalDistance - .5f) / 2f);
            _height = Mathf.Lerp(_height, .32f * far, 1f - Mathf.Exp(-8f * dt));
            Offset = (_lag + Vector3.up * _height) * _weight;
            float flat = new Vector2(vel.x, vel.z).magnitude;
            Lean = Mathf.Clamp(flat * 3f, 0f, 28f) * _weight - (1f - far) * 12f * _weight;
            Vector3 right = heading * Vector3.right;
            _bank = Mathf.Lerp(_bank, Mathf.Clamp(-Vector3.Dot(accel, right) * .9f, -35f, 35f) * _weight, 1f - Mathf.Exp(-10f * dt));
            float whip = Mathf.Abs(Vector3.Dot(accel, right));
            _flick = Mathf.Max(_flick - dt * 5f, whip > 30f ? Mathf.Clamp01((whip - 30f) / 60f) * _weight : 0f);
        }
    }
}
