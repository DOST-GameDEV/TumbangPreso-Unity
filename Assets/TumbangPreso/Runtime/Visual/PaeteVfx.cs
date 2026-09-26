using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ LIANA LEAP'S VINES COME OUT OF HIS ARMS. Owner, 2026-09-25: *"i want u to make it look
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
        private CharacterMotor _caster;
        private Vector3 _anchor;
        private float _age, _reach, _reel, _return;
        // ⚠️⚠️ v3 (2026-09-26). The owner, with two Marvel Rivals Groot frames: *"make the vines
        // especially look like tree vines/branches and not just green vines"*, *"entangled
        // vines/branches"*, *"smooth arm vine/branch movement extension like this"*. v2 was a neat
        // three-strand rope. Now each arm pays out FIVE STRANDS THAT WANDER OVER AND UNDER EACH OTHER,
        // each with its own typed twist rate, drift and thickness (never a loop of copies), two in
        // bark and three lit lime like Groot's, with forked TWIGS sprouting along them
        // (`GrowthTwigs`), and the reach eases out of the forearm instead of popping.
        private const int Strands = 5;
        // ⚠️ AND THE BUNDLE IS GROOT'S, READ OFF HIS HERO-PROFILE ART (owner's third reference): two
        // THICK smooth tan bark limbs that twist loosely round each other, two THIN dark vines coiled
        // tightly round those, and one thin lime-lit strand (the energised strike). Per strand: turns
        // per metre, phase, wander frequency, wander depth, distance from the centre, thickness.
        private static readonly float[] StrandTurns = { 0.30f, -0.26f, 1.35f, -1.10f, 0.62f };
        private static readonly float[] StrandPhase = { 0.0f, 3.1f, 1.3f, 4.4f, 2.2f };
        private static readonly float[] StrandWanderFreq = { 5.0f, 6.5f, 11.0f, 9.0f, 13.0f };
        private static readonly float[] StrandWanderDepth = { 0.4f, 0.5f, 0.3f, 0.35f, 0.8f };
        private static readonly float[] StrandRadius = { 0.45f, 0.55f, 1.25f, 1.1f, 0.95f };
        private static readonly float[] StrandThick = { 1.75f, 1.45f, 0.42f, 0.38f, 0.40f };
        private GrowthTwigs _twigsLeft, _twigsRight;
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
            fx._caster = caster;
            fx._reach = Mathf.Max(0.05f, reachSeconds);
            fx._reel = Mathf.Max(0.05f, reelSeconds);
            fx._return = 0.22f;
            fx._left = FindBone(caster.transform, "arm-left");
            fx._right = FindBone(caster.transform, "arm-right");
            fx._torso = FindBone(caster.transform, "torso") ?? caster.transform;
            // Two thick bark limbs, two thin dark vines coiled round them, one lit lime strand.
            // ⚠️ The lit strand is LEAF GREEN, not the eye light: on the asphalt the eye light's
            // yellow read as "yellow shit" to the owner (2026-09-26), not as a living vine.
            Color[] strand = { GrowthVfx.BarkLit, GrowthVfx.Bark, GrowthVfx.Vine, GrowthVfx.Moss, GrowthVfx.LeafGreen };
            float[] glow = { 0f, 0f, 0f, 0f, 0.55f };
            for (int i = 0; i < 2 * Strands; i++)
            {
                fx._meshes[i] = new Mesh { name = "PaeteVineStrand" };
                fx._meshes[i].MarkDynamic();
                GrowthVfx.Part(go.transform, (i < Strands ? "vine-left-" : "vine-right-") + (i % Strands), fx._meshes[i],
                               strand[i % Strands], glow[i % Strands]);
            }
            // Forked twigs along each branch, each at its own place, roll and size.
            fx._twigsLeft = new GrowthTwigs(go.transform, new[] { 0.18f, 0.33f, 0.47f, 0.61f, 0.78f },
                                            new[] { 20f, 150f, 260f, 80f, 200f }, new[] { 1.1f, 0.9f, 1.0f, 0.8f, 0.7f }, GrowthVfx.BarkLit);
            fx._twigsRight = new GrowthTwigs(go.transform, new[] { 0.24f, 0.39f, 0.55f, 0.70f, 0.86f },
                                             new[] { 300f, 110f, 210f, 20f, 170f }, new[] { 1.0f, 1.1f, 0.8f, 0.9f, 0.6f }, GrowthVfx.BarkLit);
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
            // ⚠️ IN FIRST PERSON THE BODY'S ARMS ARE HIDDEN, so a vine from them grew out of the space
            // under the camera. The owner of the cast sees it leave their own viewmodel hands instead;
            // every other screen (and the owner in third person) sees it leave the body's forearms.
            if (CameraSystem.CameraRig.TryViewmodelHand(_caster, arm == _left, out var viewHand)) return viewHand;
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
            // ⚠️ A SMOOTH EXTENSION NOW, NOT A POP (owner: *"smooth arm vine/branch movement
            // extension"*): the branches ease out of the forearm (cubic out) and ease back in.
            float outT = Mathf.Clamp01(_age / _reach);
            float backT = Mathf.Clamp01((_age - _reach - _reel) / _return);
            float extend = _age < _reach ? 1f - Mathf.Pow(1f - outT, 3f)
                         : _age < _reach + _reel ? 1f
                         : 1f - backT * backT * (3f - 2f * backT);
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
                float length = Vector3.Distance(from, tip);
                for (int s2 = 0; s2 < Strands; s2++)
                {
                    _points.Clear(); _radii.Clear();
                    for (int k = 0; k < _centre.Count; k++)
                    {
                        float t = k / (float)(_centre.Count - 1);
                        // Each strand winds at its own rate, drifts in and out on its own rhythm, and the
                        // bundle is widest at the forearm where it tears loose and closes toward the tip.
                        float a = t * length * StrandTurns[s2] * Mathf.PI * 2f + StrandPhase[s2] + (i == 0 ? 0f : 0.9f)
                                + Mathf.Sin(t * StrandWanderFreq[s2] + _age * 3f) * StrandWanderDepth[s2];
                        float r = Mathf.Lerp(0.12f, 0.025f, t) * StrandRadius[s2] * loose
                                * (0.7f + 0.3f * Mathf.Sin(t * StrandWanderFreq[s2] * 1.7f + StrandPhase[s2]));
                        _points.Add(_centre[k] + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * r);
                        _radii.Add(Mathf.Lerp(0.06f, 0.011f, t) * StrandThick[s2]);
                    }
                    GrowthVfx.Tube(_meshes[i * Strands + s2], _points, _radii, 4);
                }
                (i == 0 ? _twigsLeft : _twigsRight).Place(_centre, extend, 0.9f + 0.3f * (1f - loose), true);
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
    /// ⚠️⚠️ THE GROUND BREAKING, BEFORE ANYTHING OF HIS COMES UP THROUGH IT (owner, 2026-09-26: *"i want
    /// it so that his skills loook like theyre coming out of the ground each time and forming on the
    /// spot not just spawning in"*). Crack plates open out from the point and soil chunks are thrown
    /// clear and fall back, each chunk on its own typed throw; then the cracks close and the chunks
    /// shrink into the road. The seedling, THORN HARVEST's thorns and the sentry all break the road with this
    /// before they rise, so none of them appears at size.
    /// </summary>
    public sealed class PaeteGroundBreak : MonoBehaviour
    {
        private float _age, _size;
        private readonly List<Transform> _cracks = new List<Transform>();
        private readonly List<Transform> _chunks = new List<Transform>();
        private readonly List<Vector3> _throw = new List<Vector3>();
        private const float Life = 1.3f;

        // Per chunk: yaw of the throw, outward speed, upward speed, size. Typed, none the same.
        private static readonly float[] ChunkYaw = { 10f, 55f, 102f, 150f, 196f, 240f, 288f, 331f };
        private static readonly float[] ChunkOut = { 1.6f, 2.3f, 1.2f, 2.0f, 1.5f, 2.6f, 1.8f, 1.3f };
        private static readonly float[] ChunkUp = { 3.4f, 2.6f, 4.0f, 3.0f, 3.6f, 2.4f, 3.2f, 3.8f };
        private static readonly float[] ChunkSize = { 0.11f, 0.08f, 0.13f, 0.07f, 0.10f, 0.09f, 0.12f, 0.08f };

        public static PaeteGroundBreak Spawn(Vector3 at, float size)
        {
            var go = new GameObject("PaeteGroundBreak");
            go.transform.position = at;
            var fx = go.AddComponent<PaeteGroundBreak>();
            fx._size = size;
            float[] crackYaw = { 15f, 85f, 160f, 230f, 300f };
            float[] crackLen = { 0.8f, 0.6f, 0.9f, 0.55f, 0.75f };
            for (int i = 0; i < crackYaw.Length; i++)
            {
                var pivot = new GameObject("crack").transform;
                pivot.SetParent(go.transform, false);
                pivot.localRotation = Quaternion.Euler(0f, crackYaw[i], 0f);
                var plate = GrowthVfx.Block(pivot, "plate", new Vector3(0.08f, 0.02f, crackLen[i]), GrowthVfx.BarkDark).transform;
                plate.localPosition = new Vector3(0f, 0.012f, crackLen[i] * 0.5f);
                fx._cracks.Add(pivot);
            }
            for (int i = 0; i < ChunkYaw.Length; i++)
            {
                var chunk = GrowthVfx.Block(go.transform, "soil-chunk", Vector3.one * ChunkSize[i], i % 3 == 0 ? GrowthVfx.BarkDark : GrowthVfx.Seed).transform;
                float yaw = ChunkYaw[i] * Mathf.Deg2Rad;
                fx._chunks.Add(chunk);
                fx._throw.Add(new Vector3(Mathf.Sin(yaw) * ChunkOut[i], ChunkUp[i], Mathf.Cos(yaw) * ChunkOut[i]));
            }
            fx.transform.localScale = Vector3.one * size;
            fx.StepTo(0f);
            return fx;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= Life) { Destroy(gameObject); return; }
            StepTo(_age);
        }

        public void StepTo(float t)
        {
            float open = Mathf.Clamp01(t / 0.12f), close = Mathf.Clamp01((t - 0.8f) / 0.5f);
            for (int i = 0; i < _cracks.Count; i++) _cracks[i].localScale = new Vector3(1f, 1f, Mathf.Max(0.001f, open * (1f - close)));
            for (int i = 0; i < _chunks.Count; i++)
            {
                var v = _throw[i];
                float y = Mathf.Max(0f, v.y * t - 0.5f * 12f * t * t);
                _chunks[i].localPosition = new Vector3(v.x * t, 0.05f + y, v.z * t) * 0.5f;
                _chunks[i].localRotation = Quaternion.Euler(t * 400f + i * 40f, t * 250f, 0f);
                _chunks[i].localScale = Vector3.one * ChunkSize[i] * (1f - Mathf.Clamp01((t - 0.7f) / 0.5f));
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
            if (!glowing)
            {
                fx._seed = GrowthVfx.Part(go.transform, "seed", GrowthVfx.Leaf(size, size * 0.7f, size * 0.55f), GrowthVfx.Seed).transform;
            }
            else
            {
                // ⚠️ THE ULTIMATE THROWS A CLUSTER, NOT A SEED (Groot's Strangling Prison is *"a massive
                // vine cluster"*, research.md): a glowing heart inside six bark limbs curled round it
                // in their own directions, dark vines and leaves caught in them, 0.9 m across, so
                // what lands looks big enough to become the 4 m tree it bursts into.
                fx._seed = new GameObject("vine-cluster").transform;
                fx._seed.SetParent(go.transform, false);
                GrowthVfx.Block(fx._seed, "cluster-heart", Vector3.one * 0.30f, GrowthVfx.Glow, 1.4f).transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
                (Vector3 axis, float r, Color c)[] limbs =
                {
                    (new Vector3(1f, 0.2f, 0f), 0.34f, GrowthVfx.BarkLit), (new Vector3(0f, 1f, 0.3f), 0.38f, GrowthVfx.Bark),
                    (new Vector3(0.3f, 0f, 1f), 0.32f, GrowthVfx.BarkDark), (new Vector3(0.7f, 0.7f, -0.2f), 0.36f, GrowthVfx.Bark),
                    (new Vector3(-0.5f, 0.3f, 0.8f), 0.30f, GrowthVfx.Vine), (new Vector3(0.2f, -0.8f, 0.6f), 0.33f, GrowthVfx.Moss),
                };
                foreach (var l in limbs)
                {
                    var n = l.axis.normalized;
                    var side = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
                    var up = Vector3.Cross(n, side);
                    var pts = new List<Vector3>(); var rad = new List<float>();
                    for (int k = 0; k <= 10; k++)
                    {
                        float a = k / 10f * Mathf.PI * 1.7f;
                        pts.Add((side * Mathf.Cos(a) + up * Mathf.Sin(a)) * l.r + n * (k / 10f - 0.5f) * 0.18f);
                        rad.Add(Mathf.Lerp(0.07f, 0.02f, k / 10f));
                    }
                    var mesh = new Mesh { name = "PaeteClusterLimb" };
                    GrowthVfx.Tube(mesh, pts, rad, 5);
                    GrowthVfx.Part(fx._seed, "limb", mesh, l.c);
                }
                float[] leafYaw = { 20f, 140f, 260f };
                for (int i = 0; i < leafYaw.Length; i++)
                {
                    var leaf = GrowthVfx.Part(fx._seed, "cluster-leaf", GrowthVfx.Leaf(0.26f, 0.15f, 0.02f), i == 1 ? GrowthVfx.LeafDark : GrowthVfx.LeafGreen).transform;
                    leaf.localRotation = Quaternion.Euler(-30f, leafYaw[i], 0f);
                    leaf.localPosition = leaf.localRotation * new Vector3(0f, 0.1f, 0.38f);
                }
                fx._seed.localScale = Vector3.one * (size / 0.26f);
            }
            fx.Update();
            return fx;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / _flight);
            transform.position = Vector3.Lerp(_from, _to, t) + Vector3.up * _height * 4f * t * (1f - t);
            // A seed spins fast; the heavy cluster tumbles.
            _seed.localRotation = _seed.childCount > 1 ? Quaternion.Euler(_age * 240f, _age * 120f, 0f) : Quaternion.Euler(_age * 720f, _age * 300f, 0f);
            if (_age >= _flight) Destroy(gameObject);
        }
    }

}
