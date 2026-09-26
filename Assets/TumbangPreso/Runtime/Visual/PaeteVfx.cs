using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ KAPIT-BAGING'S VINES COME OUT OF HIS ARMS. Owner, 2026-09-25: *"i want u to make it look
    /// like his vines actually come from his arm when he swings"*. The model's forearms ARE a braid
    /// of pointed vines (v16), so each vine here starts at the tip of a forearm, on the bone, every
    /// frame: it grows to the anchor (the reach), stays taut while he is reeled (shortening as he
    /// closes), and draws back into the arm (the return). Nothing appears from empty air.
    ///
    /// Six beats (direction.md § 1): the TELL is the palm glow during the wind-up (see
    /// `PaeteVfx.PalmGlow`), the RELEASE is the vines leaving, the TRAVEL is the reach and the reel,
    /// the CONTACT is the knot that wraps the anchor with a burst of leaves, the LINGER is the taut
    /// line, the DISSIPATE is the return into the forearm with a few leaves falling.
    /// </summary>
    public sealed class PaeteVineReach : MonoBehaviour
    {
        private Transform _left, _right, _torso;
        private Vector3 _anchor;
        private float _age, _reach, _reel, _return;
        // ⚠️ v2 (2026-09-26, owner: *"thoroughly improve the animation of vines"*): each arm's vine is
        // THREE STRANDS BRAIDED round one centreline, the forearm's own braid (model v16/v17) paid
        // out, rather than one smooth tube. The braid tightens as it goes taut on the reel, and the
        // strands are bark, vine and moss so it reads as rope made of plant at a glance.
        private const int Strands = 3;
        private readonly Mesh[] _meshes = new Mesh[2 * Strands];
        private readonly List<Vector3> _centre = new List<Vector3>(24);
        private readonly List<Transform> _rosette = new List<Transform>();
        private readonly List<Vector3> _points = new List<Vector3>(24);
        private readonly List<float> _radii = new List<float>(24);
        private readonly List<Transform> _leaves = new List<Transform>();
        private readonly List<float> _leafAt = new List<float>();
        private Transform _knot;
        private bool _burst;

        public static PaeteVineReach Build(CharacterMotor caster, Vector3 anchor, float reachSeconds, float reelSeconds)
        {
            if (caster == null) return null;
            var go = new GameObject("PaeteVineReach");
            var fx = go.AddComponent<PaeteVineReach>();
            fx._anchor = anchor;
            fx._reach = Mathf.Max(0.05f, reachSeconds);
            fx._reel = Mathf.Max(0.05f, reelSeconds);
            fx._return = 0.22f;
            fx._left = FindBone(caster.transform, "arm-left");
            fx._right = FindBone(caster.transform, "arm-right");
            fx._torso = FindBone(caster.transform, "torso") ?? caster.transform;
            Color[] strand = { GrowthVfx.Vine, GrowthVfx.Bark, GrowthVfx.Moss };
            for (int i = 0; i < 2 * Strands; i++)
            {
                fx._meshes[i] = new Mesh { name = "PaeteVineStrand" };
                fx._meshes[i].MarkDynamic();
                GrowthVfx.Part(go.transform, (i < Strands ? "vine-left-" : "vine-right-") + (i % Strands), fx._meshes[i], strand[i % Strands]);
            }
            // Leaves riding each vine, typed at their own fractions along it.
            float[] at = { 0.22f, 0.41f, 0.63f, 0.80f, 0.34f, 0.57f, 0.74f };
            for (int i = 0; i < at.Length; i++)
            {
                var leaf = GrowthVfx.Part(go.transform, "vine-leaf", GrowthVfx.Leaf(0.13f + 0.02f * (i % 3), 0.08f, 0.012f),
                                          i % 2 == 0 ? GrowthVfx.LeafGreen : GrowthVfx.LeafDark).transform;
                fx._leaves.Add(leaf);
                fx._leafAt.Add(at[i]);
            }
            fx._knot = GrowthVfx.Block(go.transform, "anchor-knot", new Vector3(0.22f, 0.22f, 0.22f), GrowthVfx.Vine).transform;
            fx._knot.gameObject.SetActive(false);
            // The anchor mark (Kinich's glyph, research.md § 2): six leaves opening round the knot
            // in a rosette, so the catch point reads from across the court.
            for (int i = 0; i < 6; i++)
            {
                var leaf = GrowthVfx.Part(fx._knot.parent, "anchor-leaf", GrowthVfx.Leaf(0.30f, 0.16f, 0.02f),
                                          i % 2 == 0 ? GrowthVfx.LeafGreen : GrowthVfx.LeafDark).transform;
                leaf.gameObject.SetActive(false);
                fx._rosette.Add(leaf);
            }
            fx.Step(0f);
            return fx;
        }

        private static Transform FindBone(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        /// <summary>
        /// The tip of a forearm, whichever way the rig's axes run: of the two points one arm's length
        /// out along the bone's local x, the one further from the torso is the hand.
        /// </summary>
        private Vector3 Hand(Transform arm)
        {
            if (arm == null) return transform.position;
            Vector3 a = arm.TransformPoint(new Vector3(0.47f, 0f, 0f));
            Vector3 b = arm.TransformPoint(new Vector3(-0.47f, 0f, 0f));
            Vector3 c = _torso != null ? _torso.position : arm.position;
            return (a - c).sqrMagnitude > (b - c).sqrMagnitude ? a : b;
        }

        private void Update() => Step(Time.deltaTime);

        public void Step(float dt)
        {
            _age += dt;
            float life = _reach + _reel + _return;
            if (_age >= life) { Destroy(gameObject); return; }

            // How far along the line the tip is: out over the reach, held, back over the return.
            float extend = _age < _reach ? GrowthVfx.Pop(_age / _reach * 0.9f)
                         : _age < _reach + _reel ? 1f
                         : 1f - Mathf.Clamp01((_age - _reach - _reel) / _return);
            extend = Mathf.Clamp01(extend);
            float taut = _age < _reach ? 0.08f : 0.02f;

            for (int i = 0; i < 2; i++)
            {
                Vector3 from = Hand(i == 0 ? _left : _right);
                // The two vines meet the anchor a hand apart, so it reads as two arms reaching.
                Vector3 to = _anchor + (i == 0 ? Vector3.left : Vector3.right) * 0.08f;
                Vector3 tip = Vector3.Lerp(from, to, extend);
                GrowthVfx.Curve(_centre, from, tip, 18, taut * Vector3.Distance(from, tip) * 0.25f, 0.06f * (1f - extend * 0.6f), i * 1.7f + _age * 6f);
                // The braid: each strand winds round the centreline; loose while it reaches (wide,
                // few turns), tight once it has caught (narrow, the rope pulled taut).
                float loose = _age < _reach ? 1f : Mathf.Lerp(1f, 0.55f, Mathf.Clamp01((_age - _reach) / 0.12f));
                Vector3 axis = (tip - from).sqrMagnitude > 1e-6f ? (tip - from).normalized : Vector3.forward;
                Vector3 side = Vector3.Cross(axis, Vector3.up);
                if (side.sqrMagnitude < 1e-6f) side = Vector3.right;
                side.Normalize();
                Vector3 up = Vector3.Cross(side, axis);
                float turns = 2.2f + Vector3.Distance(from, tip) * 0.55f;
                for (int s2 = 0; s2 < Strands; s2++)
                {
                    _points.Clear(); _radii.Clear();
                    for (int k = 0; k < _centre.Count; k++)
                    {
                        float t = k / (float)(_centre.Count - 1);
                        float a = t * turns * Mathf.PI * 2f + s2 * Mathf.PI * 2f / Strands + (i == 0 ? 0f : 0.9f);
                        // Wide at the forearm where the braid unravels, closing to a point at the tip.
                        float r = Mathf.Lerp(0.07f, 0.016f, t) * loose;
                        _points.Add(_centre[k] + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * r);
                        // As thick as a forearm strand where it leaves the arm (0.034 m), thin at the tip.
                        _radii.Add(Mathf.Lerp(0.046f, 0.012f, t));
                    }
                    GrowthVfx.Tube(_meshes[i * Strands + s2], _points, _radii, 4);
                }
                // Leaves ride the braid: the first vine carries four, the second three.
                int first = i == 0 ? 0 : 4, count = i == 0 ? 4 : 3;
                for (int k = 0; k < count; k++)
                {
                    var leaf = _leaves[first + k];
                    float f = _leafAt[first + k];
                    int index = Mathf.Clamp(Mathf.RoundToInt(f * (_centre.Count - 1)), 1, _centre.Count - 2);
                    leaf.position = _centre[index];
                    Vector3 along = (_centre[index + 1] - _centre[index - 1]).normalized;
                    leaf.rotation = Quaternion.LookRotation(along, Vector3.up) * Quaternion.Euler(0f, (k % 2 == 0 ? 40f : -40f), (k % 2 == 0 ? 30f : -30f));
                    leaf.localScale = Vector3.one * Mathf.Clamp01(extend * 1.4f - f * 0.4f);
                }
            }

            // Contact: a knot wraps the anchor when the vines arrive, with one burst of leaves.
            bool arrived = _age >= _reach && _age < _reach + _reel + _return * 0.5f;
            _knot.gameObject.SetActive(arrived);
            if (arrived)
            {
                _knot.position = _anchor;
                float k = Mathf.Clamp01((_age - _reach) / 0.12f);
                _knot.localScale = Vector3.one * (0.10f + 0.14f * GrowthVfx.Pop(k));
                _knot.rotation = Quaternion.Euler(0f, _age * 90f, 18f);
                if (!_burst) { _burst = true; PaeteLeafBurst.Spawn(_anchor, 6, 1.4f); }
            }
            // The rosette opens with the catch and closes as the vines let go.
            float bloom = arrived ? GrowthVfx.Pop(Mathf.Clamp01((_age - _reach) / 0.16f)) : 0f;
            Vector3 face = (_torso != null ? _torso.position : transform.position) - _anchor;
            if (face.sqrMagnitude < 1e-4f) face = Vector3.back;
            var facing = Quaternion.LookRotation(face.normalized, Vector3.up);
            for (int i = 0; i < _rosette.Count; i++)
            {
                var leaf = _rosette[i];
                leaf.gameObject.SetActive(arrived);
                if (!arrived) continue;
                var petal = facing * Quaternion.Euler(0f, 0f, i * 60f) * Quaternion.Euler(-90f + 55f * bloom, 0f, 0f);
                leaf.SetPositionAndRotation(_anchor + petal * new Vector3(0f, 0f, 0.14f), petal);
                leaf.localScale = Vector3.one * bloom;
            }
        }
    }

    /// <summary>
    /// A handful of leaves thrown out from a point and fluttering down: the motif particle of every
    /// growth effect (direction.md § 2), with narra's disc-shaped seed pods among them. Each leaf's
    /// throw is set from its own index, so two bursts never repeat one pattern.
    /// </summary>
    public sealed class PaeteLeafBurst : MonoBehaviour
    {
        private readonly List<Transform> _bits = new List<Transform>();
        private readonly List<Vector3> _velocity = new List<Vector3>();
        private readonly List<Vector3> _spin = new List<Vector3>();
        private float _age, _life;

        public static PaeteLeafBurst Spawn(Vector3 at, int count, float strength)
        {
            if (GrowthVfx.Reduced) count = Mathf.Max(2, count / 2);
            var go = new GameObject("PaeteLeafBurst");
            go.transform.position = at;
            var fx = go.AddComponent<PaeteLeafBurst>();
            fx._life = 1.4f;
            for (int i = 0; i < count; i++)
            {
                bool pod = i % 4 == 3;
                var mesh = pod ? GrowthVfx.Leaf(0.09f, 0.09f, 0.02f) : GrowthVfx.Leaf(0.12f + 0.015f * (i % 3), 0.07f, 0.012f);
                var bit = GrowthVfx.Part(go.transform, pod ? "narra-pod" : "leaf", mesh,
                    pod ? GrowthVfx.Seed : (i % 2 == 0 ? GrowthVfx.LeafGreen : GrowthVfx.LeafDark)).transform;
                float a = i * 2.39996f; // the golden angle: even, never a ring
                var dir = new Vector3(Mathf.Cos(a), 0.9f + 0.25f * (i % 3), Mathf.Sin(a));
                fx._bits.Add(bit);
                fx._velocity.Add(dir.normalized * strength * (0.8f + 0.1f * (i % 4)));
                fx._spin.Add(new Vector3(200f + 37f * i, 140f - 23f * i, 90f + 11f * i));
            }
            return fx;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            if (_age >= _life) { Destroy(gameObject); return; }
            for (int i = 0; i < _bits.Count; i++)
            {
                // Leaves fall slowly (drag), which is the whole read: a leaf, not a chip.
                _velocity[i] = Vector3.Lerp(_velocity[i], new Vector3(0f, -0.7f, 0f), dt * 3.2f);
                _bits[i].position += _velocity[i] * dt;
                _bits[i].Rotate(_spin[i] * dt, Space.Self);
                // Fade by thinning (direction.md § 2): shrink, never dim.
                _bits[i].localScale = Vector3.one * Mathf.Clamp01((_life - _age) / 0.5f);
            }
        }
    }

    /// <summary>
    /// The tell of every Paete cast: a green glow gathering in the palms (research.md § 3, from
    /// Groot's ultimate). A small emissive seed at each forearm tip that swells over the wind-up.
    /// </summary>
    public sealed class PaetePalmGlow : MonoBehaviour
    {
        private Transform _arm, _torso;
        private Transform _glow;
        private float _age, _life;

        public static void Attach(CharacterMotor caster, float seconds)
        {
            if (caster == null) return;
            foreach (string bone in new[] { "arm-left", "arm-right" })
            {
                Transform arm = null, torso = null;
                foreach (var t in caster.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == bone) arm = t;
                    if (t.name == "torso") torso = t;
                }
                if (arm == null) continue;
                var go = new GameObject("PaetePalmGlow");
                var fx = go.AddComponent<PaetePalmGlow>();
                fx._arm = arm; fx._torso = torso; fx._life = Mathf.Max(0.12f, seconds);
                fx._glow = GrowthVfx.Block(go.transform, "palm-glow", Vector3.one * 0.12f, GrowthVfx.Glow, 1.6f).transform;
                fx._glow.rotation = Quaternion.Euler(45f, 45f, 0f);
                fx.Update();
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= _life) { Destroy(gameObject); return; }
            Vector3 a = _arm.TransformPoint(new Vector3(0.47f, 0f, 0f));
            Vector3 b = _arm.TransformPoint(new Vector3(-0.47f, 0f, 0f));
            Vector3 c = _torso != null ? _torso.position : _arm.position;
            transform.position = (a - c).sqrMagnitude > (b - c).sqrMagnitude ? a : b;
            float k = Mathf.Clamp01(_age / _life);
            _glow.localScale = Vector3.one * Mathf.Lerp(0.04f, 0.14f, k);
            _glow.Rotate(0f, 360f * Time.deltaTime, 0f, Space.World);
        }
    }

    /// <summary>
    /// A seed thrown in an arc from his hand to where it lands (the seedling's and the sentry's
    /// throw: *"seeds are thrown and pop up"*, research.md § 3). It spins, and lands on its clock.
    /// </summary>
    public sealed class PaeteSeedArc : MonoBehaviour
    {
        private Vector3 _from, _to;
        private float _age, _flight, _height;
        private Transform _seed;

        public static PaeteSeedArc Throw(Vector3 from, Vector3 to, float flightSeconds, float size, bool glowing)
        {
            var go = new GameObject("PaeteSeedArc");
            var fx = go.AddComponent<PaeteSeedArc>();
            fx._from = from; fx._to = to; fx._flight = Mathf.Max(0.1f, flightSeconds);
            fx._height = 0.6f + 0.12f * Vector3.Distance(from, to);
            fx._seed = GrowthVfx.Part(go.transform, "seed", GrowthVfx.Leaf(size, size * 0.7f, size * 0.55f),
                glowing ? GrowthVfx.Glow : GrowthVfx.Seed, glowing ? 1.2f : 0f).transform;
            fx.Update();
            return fx;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / _flight);
            transform.position = Vector3.Lerp(_from, _to, t) + Vector3.up * _height * 4f * t * (1f - t);
            _seed.localRotation = Quaternion.Euler(_age * 720f, _age * 300f, 0f);
            if (_age >= _flight) Destroy(gameObject);
        }
    }

    /// <summary>
    /// ROOTED's body tell (direction.md § 6: every status has one readable without its icon): roots
    /// coiled up the shins, a little higher each second the sentry holds them, and shaking when the
    /// player struggles against them. Attached to any body that gains Rooted, on every peer.
    /// </summary>
    public sealed class PaeteRootCoil : MonoBehaviour
    {
        private CharacterMotor _body;
        private readonly Mesh[] _coils = new Mesh[3];
        private readonly List<Vector3> _points = new List<Vector3>();
        private readonly List<float> _radii = new List<float>();
        private float _age;

        public static void Attach(CharacterMotor body)
        {
            if (body == null || body.GetComponentInChildren<PaeteRootCoil>() != null) return;
            var go = new GameObject("PaeteRootCoil");
            go.transform.SetParent(body.transform, false);
            var fx = go.AddComponent<PaeteRootCoil>();
            fx._body = body;
            for (int i = 0; i < fx._coils.Length; i++)
            {
                fx._coils[i] = new Mesh { name = "PaeteRootCoil" };
                fx._coils[i].MarkDynamic();
                GrowthVfx.Part(go.transform, "coil-" + i, fx._coils[i], i == 1 ? GrowthVfx.Vine : GrowthVfx.BarkDark);
            }
        }

        private void Update()
        {
            if (_body == null || !_body.IsRooted)
            {
                // The roots letting go: the hold finished, a tag landed or the sentry died. Local on
                // every peer off the replicated clock, like the gain below.
                if (_body != null) GameServices.Audio?.PlayAtVaried("sfx_paete_root_break", _body.transform.position, 0.95f, 1.05f, 0.8f);
                Destroy(gameObject); return;
            }
            if (_age <= 0f) GameServices.Audio?.PlayAtVaried("sfx_status_rooted", _body.transform.position, 0.95f, 1.05f, 0.8f);
            _age += Time.deltaTime;
            float climb = Mathf.Clamp01(_age / 1.2f) * 0.95f + 0.15f;
            float shake = _body.IsStruggling ? Mathf.Sin(_age * 38f) * 0.035f : 0f;
            for (int c = 0; c < _coils.Length; c++)
            {
                _points.Clear(); _radii.Clear();
                float phase = c * 2.09f;
                int n = 14;
                for (int i = 0; i <= n; i++)
                {
                    float t = i / (float)n;
                    float angle = phase + t * Mathf.PI * 3.2f;
                    float r = 0.42f - 0.10f * t;
                    _points.Add(new Vector3(Mathf.Cos(angle) * r + shake, t * climb, Mathf.Sin(angle) * r));
                    _radii.Add(Mathf.Lerp(0.06f, 0.015f, t));
                }
                GrowthVfx.Tube(_coils[c], _points, _radii, 4);
            }
        }
    }

    /// <summary>
    /// The seedling's body, posed from its age by `PaetePlant` (so a replay or a pause shows the
    /// frame asked for). Stages, from the owner's notes:
    ///  * pop-up (0 to 0.35 s): squash and stretch out of the seed;
    ///  * rooted (to 15 s): four roots gripping the ground, the pod upright, leaves bright;
    ///  * loosening (15 s on): *"a visual indicator showing that it can be pulled out ... make the
    ///    model gradually change too"*: the roots lift out of the soil, a ring of loose earth shows,
    ///    the pod droops and the leaves dry, a little more every second;
    ///  * firing: the pod pulls back, then snaps forward (research.md § 2, the plant-shooter recoil),
    ///    and a wooden slipper visibly grows in the pod between shots;
    ///  * pulled: lifted, roots tearing up, flung toward the puller, gone.
    /// </summary>
    public sealed class PaetePlantBody : MonoBehaviour
    {
        private Transform _root, _stem, _pod, _shoe, _soil;
        private readonly Transform[] _roots = new Transform[4];
        private readonly Renderer[] _leafRenderers = new Renderer[3];

        public static PaetePlantBody Build(Transform parent)
        {
            var go = new GameObject("PaetePlantBody");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<PaetePlantBody>();
            b._root = go.transform;
            // Roots: four, each its own angle and length.
            float[] yaw = { 20f, 110f, 205f, 290f };
            float[] len = { 0.42f, 0.36f, 0.46f, 0.33f };
            for (int i = 0; i < 4; i++)
            {
                var pivot = new GameObject("root-" + i).transform;
                pivot.SetParent(b._root, false);
                pivot.localRotation = Quaternion.Euler(0f, yaw[i], 0f);
                var mesh = new Mesh { name = "PaetePlantRoot" };
                GrowthVfx.Tube(mesh, new List<Vector3> { new Vector3(0, 0.10f, 0), new Vector3(0, 0.03f, len[i] * 0.5f), new Vector3(0, -0.04f, len[i]) },
                               new List<float> { 0.05f, 0.035f, 0.012f }, 4);
                GrowthVfx.Part(pivot, "root", mesh, GrowthVfx.BarkDark);
                b._roots[i] = pivot;
            }
            b._soil = GrowthVfx.Block(b._root, "loose-soil", new Vector3(0.9f, 0.03f, 0.9f), GrowthVfx.Seed).transform;
            b._soil.gameObject.SetActive(false);
            b._stem = new GameObject("stem").transform;
            b._stem.SetParent(b._root, false);
            var trunk = GrowthVfx.Block(b._stem, "trunk", new Vector3(0.20f, 0.60f, 0.20f), GrowthVfx.Bark).transform;
            trunk.localPosition = new Vector3(0f, 0.30f, 0f);
            var ring = GrowthVfx.Block(b._stem, "moss-ring", new Vector3(0.24f, 0.06f, 0.24f), GrowthVfx.Moss).transform;
            ring.localPosition = new Vector3(0f, 0.10f, 0f);
            b._pod = new GameObject("pod").transform;
            b._pod.SetParent(b._stem, false);
            b._pod.localPosition = new Vector3(0f, 0.62f, 0f);
            var head = GrowthVfx.Block(b._pod, "pod-head", new Vector3(0.44f, 0.34f, 0.40f), GrowthVfx.LeafDark).transform;
            head.localPosition = new Vector3(0f, 0.17f, 0f);
            var lip = GrowthVfx.Block(b._pod, "pod-lip", new Vector3(0.30f, 0.10f, 0.08f), GrowthVfx.Vine).transform;
            lip.localPosition = new Vector3(0f, 0.20f, 0.20f);
            b._shoe = GrowthVfx.Block(b._pod, "growing-slipper", new Vector3(0.10f, 0.03f, 0.24f), GrowthVfx.BarkLit).transform;
            b._shoe.localPosition = new Vector3(0f, 0.36f, 0.06f);
            float[] leafYaw = { -40f, 60f, 170f };
            for (int i = 0; i < 3; i++)
            {
                var leaf = GrowthVfx.Part(b._pod, "leaf-" + i, GrowthVfx.Leaf(0.34f - 0.04f * i, 0.18f, 0.02f), i == 1 ? GrowthVfx.LeafDark : GrowthVfx.LeafGreen).transform;
                leaf.localRotation = Quaternion.Euler(-30f, leafYaw[i], 0f);
                leaf.localPosition = new Vector3(0f, 0.34f, 0f) + leaf.localRotation * new Vector3(0f, 0f, 0.14f);
                b._leafRenderers[i] = leaf.GetComponent<Renderer>();
            }
            return b;
        }

        public void Pose(float age, float loosen, bool pullable, float shotGrowth, float sinceShot)
        {
            bool landed = age >= 0f;
            _root.gameObject.SetActive(landed);
            if (!landed) return;
            float pop = GrowthVfx.Pop(age / 0.35f);
            float squash = age < 0.35f ? 1f + 0.25f * Mathf.Sin(age / 0.35f * Mathf.PI) : 1f;
            _stem.localScale = new Vector3(pop * squash, pop / squash, pop * squash);

            // Loosening: roots lift out, the soil ring shows, the pod droops, the leaves dry.
            for (int i = 0; i < 4; i++)
            {
                _roots[i].localPosition = new Vector3(0f, 0.12f * loosen, 0f);
                _roots[i].localScale = Vector3.one * pop;
                _roots[i].GetChild(0).localRotation = Quaternion.Euler(-28f * loosen, 0f, 0f);
            }
            _soil.gameObject.SetActive(pullable);
            if (pullable)
            {
                // The "you can pull this" read: loose earth that breathes, so the eye catches it.
                float pulse = 1f + 0.06f * Mathf.Sin(age * 5.0f);
                _soil.localScale = new Vector3(0.9f * pulse, 0.03f, 0.9f * pulse);
            }
            // Recoil: back 0.12 s, then snap forward past rest, then settle.
            float pitch = sinceShot < 0.12f ? -28f * (sinceShot / 0.12f)
                        : sinceShot < 0.22f ? Mathf.Lerp(-28f, 22f, (sinceShot - 0.12f) / 0.10f)
                        : sinceShot < 0.5f ? Mathf.Lerp(22f, 0f, (sinceShot - 0.22f) / 0.28f) : 0f;
            _pod.localRotation = Quaternion.Euler(pitch + 30f * loosen, 0f, 12f * loosen);
            _shoe.localScale = Vector3.one * Mathf.Clamp01(shotGrowth);
            for (int i = 0; i < 3; i++)
            {
                var r = _leafRenderers[i];
                if (r == null || r.sharedMaterial == null) continue;
                Color fresh = i == 1 ? GrowthVfx.LeafDark : GrowthVfx.LeafGreen;
                Color c = Color.Lerp(fresh, GrowthVfx.Dry, loosen);
                r.sharedMaterial.color = c;
                if (r.sharedMaterial.HasProperty("_BaseColor")) r.sharedMaterial.SetColor("_BaseColor", c);
            }
        }

        /// <summary>The pull-out: up, roots tearing free, tipped toward the puller, then gone.</summary>
        public void PosePulled(float t, Vector3 towardPuller)
        {
            towardPuller.y = 0f;
            Vector3 dir = towardPuller.sqrMagnitude > 0.01f ? towardPuller.normalized : Vector3.back;
            float heave = Mathf.Clamp01(t / 0.35f);
            float fling = Mathf.Clamp01((t - 0.35f) / 0.6f);
            _root.localPosition = Vector3.up * (0.5f * GrowthVfx.Pop(heave)) + dir * 0.8f * fling + Vector3.down * 1.2f * fling * fling;
            _root.localRotation = Quaternion.AngleAxis(-70f * fling, Vector3.Cross(Vector3.up, dir));
            for (int i = 0; i < 4; i++) _roots[i].GetChild(0).localRotation = Quaternion.Euler(-60f * heave, 0f, 0f);
            _soil.gameObject.SetActive(false);
            _root.localScale = Vector3.one * (1f - Mathf.Clamp01((t - 0.7f) / 0.25f));
        }
    }

    /// <summary>
    /// BAWI's construct: a ring of square thorns bursting out of the ground round a knot, and a
    /// thorn vine out to each caught slipper that goes taut, holds a beat and hauls it home.
    /// </summary>
    public sealed class PaeteThornBody : MonoBehaviour
    {
        private readonly List<Transform> _spikes = new List<Transform>();
        private readonly List<Slipper> _targets = new List<Slipper>();
        private readonly List<Mesh> _vines = new List<Mesh>();
        private readonly List<Vector3> _points = new List<Vector3>();
        private readonly List<float> _radii = new List<float>();
        private Transform _knot;

        public static PaeteThornBody Build(Transform parent, List<Slipper> targets)
        {
            var go = new GameObject("PaeteThornBody");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<PaeteThornBody>();
            b._knot = GrowthVfx.Block(go.transform, "thorn-knot", new Vector3(0.36f, 0.26f, 0.36f), GrowthVfx.BarkDark).transform;
            // Seven thorns, each its own height and lean, round the knot.
            float[] h = { 0.78f, 0.62f, 0.90f, 0.55f, 0.84f, 0.66f, 0.72f };
            for (int i = 0; i < h.Length; i++)
            {
                float a = i * Mathf.PI * 2f / h.Length + 0.3f * (i % 2);
                var outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var pivot = new GameObject("thorn-" + i).transform;
                pivot.SetParent(go.transform, false);
                pivot.localPosition = outward * 0.34f;
                pivot.localRotation = Quaternion.LookRotation(outward) * Quaternion.Euler(-62f + 6f * (i % 3), 0f, 0f);
                var mesh = new Mesh { name = "PaeteThorn" };
                GrowthVfx.Tube(mesh, new List<Vector3> { Vector3.zero, new Vector3(0, 0, h[i] * 0.55f), new Vector3(0, 0, h[i]) },
                               new List<float> { 0.07f, 0.04f, 0.004f }, 4);
                GrowthVfx.Part(pivot, "thorn", mesh, i % 3 == 0 ? GrowthVfx.Vine : GrowthVfx.Bark);
                b._spikes.Add(pivot);
            }
            foreach (var shoe in targets)
            {
                var mesh = new Mesh { name = "PaeteThornVine" };
                mesh.MarkDynamic();
                GrowthVfx.Part(go.transform, "thorn-vine", mesh, GrowthVfx.Vine);
                b._targets.Add(shoe);
                b._vines.Add(mesh);
            }
            return b;
        }

        public void Pose(float age, Vector3 origin)
        {
            float life = PaeteRules.ThornConstructSeconds;
            float grow = GrowthVfx.Pop(age / 0.2f);
            float wither = Mathf.Clamp01((life - age) / 0.6f);
            for (int i = 0; i < _spikes.Count; i++)
                _spikes[i].localScale = Vector3.one * Mathf.Clamp01(GrowthVfx.Pop((age - 0.02f * i) / 0.2f)) * wither;
            _knot.localScale = new Vector3(0.36f, 0.26f, 0.36f) * grow * wither;
            _knot.localPosition = Vector3.up * 0.13f * grow;

            float hold = PaeteRules.ThornHoldSeconds, yank = PaeteRules.ThornYankSeconds;
            for (int i = 0; i < _targets.Count; i++)
            {
                var shoe = _targets[i];
                _points.Clear(); _radii.Clear();
                if (shoe == null || age > hold + yank + 0.25f) { _vines[i].Clear(); continue; }
                Vector3 from = origin + Vector3.up * 0.3f;
                Vector3 to = shoe.transform.position;
                Vector3 tip = Vector3.Lerp(from, to, GrowthVfx.Pop(Mathf.Clamp01(age / 0.18f)));
                float slack = age < hold ? 0.02f : 0f;
                GrowthVfx.Curve(_points, from, tip, 12, slack * Vector3.Distance(from, tip), 0.04f, i * 1.3f + age * 8f);
                for (int k = 0; k < _points.Count; k++) _radii.Add(Mathf.Lerp(0.05f, 0.015f, k / (float)(_points.Count - 1)));
                GrowthVfx.Tube(_vines[i], _points, _radii, 4);
            }
        }
    }

    /// <summary>
    /// ⚠️⚠️ YAKAP NG MAKILING'S SENTRY, v2 (owner, 2026-09-26: *"i want u to make a very nice plant
    /// sentry"*, with Marvel Rivals' Groot named as the reference). v1 was a bark block, a glowing
    /// cube and eight flat spokes: it said "something landed" and nothing about a tree.
    ///
    /// Groot's Strangling Prison (research.md § 2) gives the read: a bright core held up by the
    /// plant, spiked vines erupting radially, bodies held upright round it. Here, typed part by part
    /// (the owner's rule, *"do it one by one dont try to mass generate it"*), each with its own
    /// numbers:
    ///  * the GROUND breaks first: eight crack plates radiating from the seed and a ring of soil;
    ///  * a squat STEPPED TRUNK erupts out of it (three tiers, each turned against the last, bark
    ///    plates and moss rings, the same bark as his body) with a Pop overshoot;
    ///  * six BUTTRESS ROOTS flare out and dive into the road round it, like a narra's;
    ///  * five CROWN BRANCHES curl up and in, CRADLING the core like fingers holding a seed, two
    ///    leaves each; they flex slowly while it lives;
    ///  * the CORE is the only light: a glowing seed that breathes, with four spores orbiting it;
    ///  * eight THORNED VINES race out ALONG THE GROUND (they grow, they do not scale), each its own
    ///    length and wave, three thorns up its back and a tip that curls up at the end;
    ///  * a braided TETHER to every body it caught, taut from the trunk to the waist for as long as
    ///    they are rooted (the roots at the shins are `PaeteRootCoil`), swaying a little;
    ///  * the WITHER, the last 0.9 s: the crown opens and droops, the core dims and shrinks, the vines
    ///    pull back into the ground, the trunk sinks, and narra pods drop.
    /// Every part is posed from `age` alone (no `_Time`), so pause, replay and a probe agree.
    /// </summary>
    public sealed class PaeteSentryBody : MonoBehaviour
    {
        private Transform _trunk, _core, _crownRoot;
        private readonly List<Transform> _cracks = new List<Transform>();
        private readonly List<Transform> _buttress = new List<Transform>();
        private readonly List<Transform> _claws = new List<Transform>();
        private readonly List<Transform> _spores = new List<Transform>();
        private readonly List<Mesh> _vineMeshes = new List<Mesh>();
        private readonly List<Transform> _vineTips = new List<Transform>();
        private readonly List<Transform> _thorns = new List<Transform>();
        private readonly List<CharacterMotor> _targets = new List<CharacterMotor>();
        private readonly List<Mesh> _tethers = new List<Mesh>();
        private readonly List<Vector3> _points = new List<Vector3>();
        private readonly List<float> _radii = new List<float>();
        private bool _podsDropped;

        // The eight ground vines: yaw, length, wave phase. Each typed, none the same.
        private static readonly float[] VineYaw = { 8f, 52f, 93f, 141f, 183f, 226f, 268f, 317f };
        private static readonly float[] VineLength = { 2.6f, 2.1f, 2.9f, 2.3f, 2.7f, 2.0f, 3.0f, 2.4f };
        private static readonly float[] VinePhase = { 0.2f, 1.9f, 3.1f, 0.8f, 2.5f, 4.0f, 1.3f, 3.6f };
        private const int VineSamples = 16;
        private static readonly float[] ClawYaw = { 0f, 74f, 142f, 213f, 287f };

        public static PaeteSentryBody Build(Transform parent)
        {
            var go = new GameObject("PaeteSentryBody");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<PaeteSentryBody>();
            var root = go.transform;

            // The ground breaking: eight crack plates, and the soil ring.
            float[] crackLen = { 0.9f, 0.7f, 1.05f, 0.75f, 0.95f, 0.65f, 1.1f, 0.8f };
            for (int i = 0; i < crackLen.Length; i++)
            {
                var pivot = new GameObject("crack-" + i).transform;
                pivot.SetParent(root, false);
                pivot.localRotation = Quaternion.Euler(0f, i * 45f + 20f, 0f);
                var plate = GrowthVfx.Block(pivot, "plate", new Vector3(0.10f, 0.02f, crackLen[i]), GrowthVfx.BarkDark).transform;
                plate.localPosition = new Vector3(0f, 0.01f, 0.4f + crackLen[i] * 0.5f);
                b._cracks.Add(pivot);
            }
            GrowthVfx.Block(root, "soil-ring", new Vector3(1.5f, 0.04f, 1.5f), GrowthVfx.Seed).transform.localRotation = Quaternion.Euler(0f, 45f, 0f);

            // ⚠️ v3 (2026-09-26): v2's three stacked tier blocks read as a CRATE and its lit bark plates
            // as labels, and the whole thing stood no taller than the bodies it held. The trunk is a
            // square tube now (four sides, like his horns), tapering and leaning, 2.1 m to the crown,
            // so it towers over a caught player the way Groot's prison does, with two moss collars,
            // three branch stubs each carrying its own leaf cluster, and bark knots on the faces.
            b._trunk = new GameObject("trunk").transform;
            b._trunk.SetParent(root, false);
            var trunkMesh = new Mesh { name = "PaeteSentryTrunk" };
            GrowthVfx.Tube(trunkMesh, new List<Vector3> { new Vector3(0f, -0.05f, 0f), new Vector3(0.04f, 0.55f, -0.02f),
                                                          new Vector3(-0.03f, 1.15f, 0.03f), new Vector3(0.02f, 1.70f, -0.02f),
                                                          new Vector3(0f, 2.10f, 0f) },
                           new List<float> { 0.62f, 0.50f, 0.42f, 0.36f, 0.34f }, 4);
            GrowthVfx.Part(b._trunk, "trunk", trunkMesh, GrowthVfx.Bark).transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            // Moss tufts on the bark (v3's collars read as flat tables round the trunk): each its own
            // place, size and lean, hugging the faces.
            (Vector3 at, Vector3 size, float yaw)[] tufts = { (new Vector3(0.36f, 0.62f, 0.10f), new Vector3(0.14f, 0.22f, 0.30f), 20f),
                (new Vector3(-0.22f, 1.45f, -0.26f), new Vector3(0.26f, 0.16f, 0.12f), -40f), (new Vector3(-0.30f, 0.30f, 0.34f), new Vector3(0.28f, 0.14f, 0.14f), 35f) };
            foreach (var tf in tufts)
            {
                var tuft = GrowthVfx.Block(b._trunk, "moss-tuft", tf.size, GrowthVfx.Moss).transform;
                tuft.localPosition = tf.at; tuft.localRotation = Quaternion.Euler(0f, tf.yaw, 0f);
            }
            // Bark knots: small dark blocks sunk in the faces, each its own place.
            (Vector3 at, float yaw)[] knots = { (new Vector3(0.30f, 0.95f, 0.26f), 40f), (new Vector3(-0.28f, 1.30f, 0.22f), -35f), (new Vector3(0.05f, 0.35f, -0.44f), 180f) };
            foreach (var k in knots)
            {
                var knot = GrowthVfx.Block(b._trunk, "bark-knot", new Vector3(0.16f, 0.12f, 0.06f), GrowthVfx.BarkDark).transform;
                knot.localPosition = k.at; knot.localRotation = Quaternion.Euler(0f, k.yaw, 0f);
            }
            // Three branch stubs off the trunk, each with a cluster of three leaves.
            (float y, float yaw, float len)[] stubs = { (1.05f, 60f, 0.45f), (1.35f, 200f, 0.38f), (0.80f, 300f, 0.34f) };
            foreach (var st in stubs)
            {
                var pivot = new GameObject("stub").transform;
                pivot.SetParent(b._trunk, false);
                pivot.localPosition = new Vector3(0f, st.y, 0f);
                pivot.localRotation = Quaternion.Euler(0f, st.yaw, 0f);
                var mesh = new Mesh { name = "PaeteSentryStub" };
                GrowthVfx.Tube(mesh, new List<Vector3> { new Vector3(0, 0, 0.25f), new Vector3(0, 0.14f, 0.25f + st.len * 0.6f), new Vector3(0, 0.30f, 0.25f + st.len) },
                               new List<float> { 0.09f, 0.06f, 0.02f }, 4);
                GrowthVfx.Part(pivot, "stub", mesh, GrowthVfx.BarkDark);
                for (int k = 0; k < 3; k++)
                {
                    var leaf = GrowthVfx.Part(pivot, "stub-leaf", GrowthVfx.Leaf(0.30f - 0.05f * k, 0.17f, 0.02f),
                                              k == 1 ? GrowthVfx.LeafDark : GrowthVfx.LeafGreen).transform;
                    leaf.localPosition = new Vector3(0f, 0.32f, 0.25f + st.len);
                    leaf.localRotation = Quaternion.Euler(-40f + 25f * k, -50f + 50f * k, 0f);
                }
            }

            // Six buttress roots, hugging the road: out from the trunk low, bending straight down into it.
            float[] rootYaw = { 15f, 75f, 130f, 200f, 250f, 315f };
            float[] rootReach = { 1.10f, 0.90f, 1.25f, 0.95f, 1.15f, 0.85f };
            for (int i = 0; i < rootYaw.Length; i++)
            {
                var pivot = new GameObject("buttress-" + i).transform;
                pivot.SetParent(b._trunk, false);
                pivot.localRotation = Quaternion.Euler(0f, rootYaw[i], 0f);
                var mesh = new Mesh { name = "PaeteSentryButtress" };
                float r = rootReach[i];
                GrowthVfx.Tube(mesh, new List<Vector3> { new Vector3(0, 0.50f, 0.34f), new Vector3(0, 0.22f, 0.52f),
                                                         new Vector3(0, 0.07f, 0.52f + r * 0.5f), new Vector3(0, -0.06f, 0.52f + r) },
                               new List<float> { 0.20f, 0.15f, 0.08f, 0.02f }, 4);
                GrowthVfx.Part(pivot, "root", mesh, i % 2 == 0 ? GrowthVfx.BarkDark : GrowthVfx.Bark);
                b._buttress.Add(pivot);
            }

            // The crown: five thick branches curling up and in round the core, each with a leaf
            // cluster at its tip, the fingers holding the seed.
            b._crownRoot = new GameObject("crown").transform;
            b._crownRoot.SetParent(b._trunk, false);
            b._crownRoot.localPosition = new Vector3(0f, 2.05f, 0f);
            for (int i = 0; i < ClawYaw.Length; i++)
            {
                var pivot = new GameObject("claw-" + i).transform;
                pivot.SetParent(b._crownRoot, false);
                pivot.localRotation = Quaternion.Euler(0f, ClawYaw[i], 0f);
                var mesh = new Mesh { name = "PaeteSentryClaw" };
                float lean = 0.06f * (i % 3);
                GrowthVfx.Tube(mesh, new List<Vector3> { new Vector3(0, -0.05f, 0.22f), new Vector3(0, 0.25f, 0.55f + lean),
                                                         new Vector3(0, 0.62f, 0.56f + lean), new Vector3(0, 0.90f, 0.26f) },
                               new List<float> { 0.15f, 0.11f, 0.07f, 0.02f }, 4);
                GrowthVfx.Part(pivot, "branch", mesh, i % 2 == 0 ? GrowthVfx.Bark : GrowthVfx.BarkDark);
                for (int k = 0; k < 3; k++)
                {
                    var leaf = GrowthVfx.Part(pivot, "leaf", GrowthVfx.Leaf(0.32f - 0.05f * k, 0.18f, 0.02f),
                                              k == 1 ? GrowthVfx.LeafDark : GrowthVfx.LeafGreen).transform;
                    leaf.localPosition = new Vector3(0.05f * (k - 1), 0.84f - 0.12f * k, 0.30f + 0.08f * k);
                    leaf.localRotation = Quaternion.Euler(-60f + 30f * k, 40f * (k - 1), 0f);
                }
                b._claws.Add(pivot);
            }

            // The core: a faceted seed, two nested glowing blocks turned against each other (the
            // inner brighter), so it reads as a lit gem rather than v2's flat card; and four spores.
            b._core = new GameObject("sentry-core").transform;
            b._core.SetParent(b._crownRoot, false);
            GrowthVfx.Block(b._core, "core-shell", Vector3.one, GrowthVfx.LeafGreen, 0.9f).transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            GrowthVfx.Block(b._core, "core-heart", Vector3.one * 0.80f, GrowthVfx.Glow, 1.5f).transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            for (int i = 0; i < 4; i++)
                b._spores.Add(GrowthVfx.Block(b._crownRoot, "spore-" + i, Vector3.one * 0.07f, GrowthVfx.Glow, 1.2f).transform);

            // The eight thorned ground vines: a mesh each, rebuilt while they grow, three thorns and a tip.
            for (int i = 0; i < VineYaw.Length; i++)
            {
                var mesh = new Mesh { name = "PaeteSentryVine" };
                mesh.MarkDynamic();
                GrowthVfx.Part(root, "ground-vine-" + i, mesh, i % 2 == 0 ? GrowthVfx.Vine : GrowthVfx.Moss);
                b._vineMeshes.Add(mesh);
                for (int k = 0; k < 3; k++)
                {
                    var thornMesh = new Mesh { name = "PaeteSentryThorn" };
                    GrowthVfx.Tube(thornMesh, new List<Vector3> { Vector3.zero, new Vector3(0, 0.12f, 0.03f), new Vector3(0, 0.24f, 0.08f) },
                                   new List<float> { 0.05f, 0.028f, 0.003f }, 4);
                    b._thorns.Add(GrowthVfx.Part(root, "thorn", thornMesh, GrowthVfx.BarkDark).transform);
                }
                var tip = GrowthVfx.Part(root, "vine-tip-leaf", GrowthVfx.Leaf(0.24f, 0.14f, 0.02f), GrowthVfx.LeafGreen).transform;
                b._vineTips.Add(tip);
            }
            return b;
        }

        public void SetTargets(List<CharacterMotor> targets)
        {
            foreach (var t in targets)
            {
                var mesh = new Mesh { name = "PaeteSentryTether" };
                mesh.MarkDynamic();
                GrowthVfx.Part(transform, "tether", mesh, GrowthVfx.Vine);
                _targets.Add(t);
                _tethers.Add(mesh);
            }
        }

        /// <summary>A ground vine's centreline at growth <paramref name="grow"/> (0 to 1), in local space.</summary>
        private void VinePath(int i, float grow, float age)
        {
            _points.Clear();
            float yaw = VineYaw[i] * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
            var side = new Vector3(dir.z, 0f, -dir.x);
            float len = VineLength[i] * grow;
            for (int k = 0; k <= VineSamples; k++)
            {
                float u = k / (float)VineSamples;
                float d = 0.45f + len * u;
                // A wave along the ground that writhes a little while it lives; the tip curls up.
                float wave = Mathf.Sin(u * 7.0f + VinePhase[i] + age * 1.6f) * 0.16f * u;
                float lift = 0.05f + Mathf.Pow(u, 6f) * 0.55f * grow;
                _points.Add(dir * d + side * wave + Vector3.up * lift);
            }
        }

        public void Pose(float age, Vector3 centre)
        {
            bool landed = age >= 0f;
            for (int c = 0; c < transform.childCount; c++) transform.GetChild(c).gameObject.SetActive(landed);
            if (!landed) { foreach (var m in _tethers) m.Clear(); return; }

            float life = PaeteRules.SentryLifeSeconds;
            // The wither runs the last 0.9 s of its life and a little past it.
            float wither = Mathf.Clamp01((age - (life - 0.4f)) / 0.9f);
            float alive = 1f - wither;

            // 0 to 0.12: the ground breaks.
            for (int i = 0; i < _cracks.Count; i++)
                _cracks[i].localScale = new Vector3(1f, 1f, Mathf.Clamp01((age - 0.01f * i) / 0.12f));

            // 0.06 to 0.36: the trunk erupts, with overshoot; it sinks back into the road as it withers.
            float erupt = GrowthVfx.Pop((age - 0.06f) / 0.30f);
            _trunk.localScale = new Vector3(Mathf.Max(0.001f, erupt), Mathf.Max(0.001f, erupt), Mathf.Max(0.001f, erupt));
            _trunk.localPosition = Vector3.down * (0.9f * wither * wither);

            // 0.14 to 0.44: the buttress roots flare out, each a beat after the last.
            for (int i = 0; i < _buttress.Count; i++)
                _buttress[i].localScale = Vector3.one * Mathf.Max(0.001f, GrowthVfx.Pop((age - 0.14f - 0.03f * i) / 0.30f));

            // 0.24 to 0.6: the crown unfurls; alive, it flexes slowly; withering, it opens and droops.
            for (int i = 0; i < _claws.Count; i++)
            {
                float unfurl = GrowthVfx.Pop((age - 0.24f - 0.04f * i) / 0.36f);
                float flex = Mathf.Sin(age * 1.7f + i * 1.3f) * 4f;
                float open = -55f * (1f - Mathf.Clamp01(unfurl)) + flex + 70f * wither;
                _claws[i].localRotation = Quaternion.Euler(0f, ClawYaw[i], 0f) * Quaternion.Euler(open, 0f, 0f);
                _claws[i].localScale = Vector3.one * Mathf.Max(0.001f, Mathf.Clamp01(unfurl * 1.2f));
            }

            // The core: grows in with the crown, breathes, dims and shrinks as it withers.
            float coreIn = GrowthVfx.Pop((age - 0.3f) / 0.3f);
            float breathe = 1f + 0.09f * Mathf.Sin(age * 3.8f) + 0.04f * Mathf.Sin(age * 9.1f);
            _core.localPosition = new Vector3(0f, 0.52f, 0f);
            _core.localRotation = Quaternion.Euler(0f, age * 35f, 0f);
            _core.localScale = Vector3.one * Mathf.Max(0.001f, 0.50f * coreIn * breathe * (1f - 0.8f * wither));
            for (int i = 0; i < _spores.Count; i++)
            {
                float a = age * (1.4f + 0.2f * i) + i * Mathf.PI * 0.5f;
                _spores[i].localPosition = new Vector3(Mathf.Cos(a) * 0.46f, 0.52f + Mathf.Sin(a * 1.7f) * 0.14f, Mathf.Sin(a) * 0.46f);
                _spores[i].localScale = Vector3.one * 0.07f * Mathf.Clamp01(coreIn) * alive;
            }

            // 0.2 to 0.62: the ground vines race out, then writhe; they pull back into the ground as it withers.
            for (int i = 0; i < _vineMeshes.Count; i++)
            {
                float grow = Mathf.Clamp01((age - 0.2f - 0.025f * i) / 0.42f);
                grow = 1f - (1f - grow) * (1f - grow);
                grow *= alive;
                if (grow <= 0.01f)
                {
                    _vineMeshes[i].Clear();
                    for (int k = 0; k < 3; k++) _thorns[i * 3 + k].localScale = Vector3.zero;
                    _vineTips[i].localScale = Vector3.zero;
                    continue;
                }
                VinePath(i, grow, age);
                _radii.Clear();
                for (int k = 0; k < _points.Count; k++) _radii.Add(Mathf.Lerp(0.13f, 0.03f, k / (float)VineSamples));
                GrowthVfx.Tube(_vineMeshes[i], _points, _radii, 5);
                // Three thorns up the vine's back, at their own fractions, each out when the vine reaches it.
                for (int k = 0; k < 3; k++)
                {
                    int at = Mathf.Clamp(Mathf.RoundToInt((0.3f + 0.22f * k) * VineSamples), 1, VineSamples - 1);
                    var thorn = _thorns[i * 3 + k];
                    Vector3 along = (_points[at + 1] - _points[at - 1]).normalized;
                    thorn.localPosition = _points[at] + Vector3.up * 0.05f;
                    thorn.localRotation = Quaternion.LookRotation(along, Vector3.up) * Quaternion.Euler(-10f, 0f, 0f);
                    thorn.localScale = Vector3.one * Mathf.Clamp01((grow - (0.3f + 0.22f * k)) * 5f);
                }
                var tip = _vineTips[i];
                Vector3 last = _points[_points.Count - 1], before = _points[_points.Count - 2];
                tip.localPosition = last;
                tip.localRotation = Quaternion.LookRotation((last - before).normalized, Vector3.up);
                tip.localScale = Vector3.one * grow;
            }

            // The tethers: a braid-thick vine to each held body, taut while they are rooted.
            float catchAt = PaeteRules.SentryCatchSeconds;
            for (int i = 0; i < _targets.Count; i++)
            {
                var p = _targets[i];
                bool held = p != null && (age <= catchAt + 0.7f || p.IsRooted) && wither < 0.5f;
                if (!held) { _tethers[i].Clear(); continue; }
                Vector3 from = new Vector3(0f, 0.9f * erupt, 0f);
                Vector3 to = transform.InverseTransformPoint(p.transform.position + Vector3.up * 0.85f);
                Vector3 tip = Vector3.Lerp(from, to, GrowthVfx.Pop(Mathf.Clamp01(age / catchAt)));
                GrowthVfx.Curve(_points, from, tip, 14, 0.06f * Vector3.Distance(from, tip), 0.05f, i * 2.1f + age * 2.5f);
                _radii.Clear();
                for (int k = 0; k < _points.Count; k++) _radii.Add(Mathf.Lerp(0.10f, 0.04f, k / (float)(_points.Count - 1)));
                GrowthVfx.Tube(_tethers[i], _points, _radii, 5);
            }

            // The pods: once, as the wither starts, the tree lets go of its seeds.
            if (wither > 0.05f && !_podsDropped)
            {
                _podsDropped = true;
                PaeteLeafBurst.Spawn(transform.position + Vector3.up * 1.6f, 9, 1.6f);
            }
        }
    }
}
