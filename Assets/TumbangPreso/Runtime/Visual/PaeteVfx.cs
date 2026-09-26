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
    /// shrink into the road. The seedling, BAWI's thorns and the sentry all break the road with this
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
        // ⚠️⚠️ v2 (2026-09-26). v1's pod was a green cube and its growing slipper a block whose pose
        // wrote `localScale = growth` over the block's own size, so a grown shot rendered as a 1 m tan
        // cube over the plant (found filming the rise). Now, part by part: a curved stem of bark into
        // moss; four leaves fanned at its foot and two up it; a POD of five petal-leaves that opens
        // round a real wooden slipper (sole and thong strap, like the ones it throws) as the slipper
        // grows, and closes after the shot; the slipper grows from its own size, never from 1.
        private Transform _root, _stem, _pod, _shoe, _soil;
        private readonly Transform[] _roots = new Transform[4];
        private readonly List<Renderer> _leafRenderers = new List<Renderer>();
        private readonly List<Color> _leafFresh = new List<Color>();
        private readonly List<Transform> _petals = new List<Transform>();
        private static readonly float[] PetalYaw = { 0f, 74f, 146f, 214f, 288f };

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
            // The stem: bark at the foot, moss toward the pod, with a gentle lean and a curve back.
            var stemMesh = new Mesh { name = "PaetePlantStem" };
            GrowthVfx.Tube(stemMesh, new List<Vector3> { new Vector3(0f, 0f, 0f), new Vector3(0.03f, 0.25f, 0.01f), new Vector3(0.01f, 0.50f, -0.02f), new Vector3(-0.01f, 0.66f, 0f) },
                           new List<float> { 0.10f, 0.08f, 0.065f, 0.06f }, 5);
            GrowthVfx.Part(b._stem, "stem", stemMesh, GrowthVfx.Bark);
            var collar = GrowthVfx.Block(b._stem, "moss-collar", new Vector3(0.16f, 0.07f, 0.16f), GrowthVfx.Moss).transform;
            collar.localPosition = new Vector3(0f, 0.58f, 0f); collar.localRotation = Quaternion.Euler(0f, 20f, 0f);
            // Four leaves fanned at its foot and two up the stem, each its own size and turn.
            (Vector3 at, float yaw, float pitch, float size)[] leaves =
            {
                (new Vector3(0f, 0.06f, 0f), 30f, -12f, 0.34f), (new Vector3(0f, 0.05f, 0f), 125f, -8f, 0.30f),
                (new Vector3(0f, 0.07f, 0f), 210f, -15f, 0.36f), (new Vector3(0f, 0.05f, 0f), 300f, -10f, 0.28f),
                (new Vector3(0.02f, 0.32f, 0f), 80f, -35f, 0.24f), (new Vector3(-0.01f, 0.44f, 0f), 250f, -40f, 0.22f),
            };
            for (int i = 0; i < leaves.Length; i++)
            {
                var l = leaves[i];
                Color fresh = i % 2 == 0 ? GrowthVfx.LeafGreen : GrowthVfx.LeafDark;
                var leaf = GrowthVfx.Part(b._stem, "leaf", GrowthVfx.Leaf(l.size, l.size * 0.55f, 0.02f), fresh).transform;
                leaf.localRotation = Quaternion.Euler(l.pitch, l.yaw, 0f);
                leaf.localPosition = l.at + leaf.localRotation * new Vector3(0f, 0f, l.size * 0.45f);
                b._leafRenderers.Add(leaf.GetComponent<Renderer>()); b._leafFresh.Add(fresh);
            }

            // The pod: a cup of five petal-leaves round the growing slipper.
            b._pod = new GameObject("pod").transform;
            b._pod.SetParent(b._stem, false);
            b._pod.localPosition = new Vector3(0f, 0.64f, 0f);
            GrowthVfx.Block(b._pod, "pod-base", new Vector3(0.20f, 0.10f, 0.20f), GrowthVfx.Moss).transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            for (int i = 0; i < PetalYaw.Length; i++)
            {
                var hinge = new GameObject("petal").transform;
                hinge.SetParent(b._pod, false);
                hinge.localRotation = Quaternion.Euler(0f, PetalYaw[i], 0f);
                Color fresh = i % 2 == 0 ? GrowthVfx.LeafDark : GrowthVfx.Vine;
                var petal = GrowthVfx.Part(hinge, "petal-leaf", GrowthVfx.Leaf(0.30f - 0.02f * (i % 3), 0.17f, 0.025f), fresh).transform;
                petal.localPosition = new Vector3(0f, 0.02f, 0.14f);
                b._petals.Add(hinge);
                b._leafRenderers.Add(petal.GetComponent<Renderer>()); b._leafFresh.Add(fresh);
            }
            // The wooden slipper growing in it: a sole and a thong strap, the shape it throws.
            b._shoe = new GameObject("growing-slipper").transform;
            b._shoe.SetParent(b._pod, false);
            b._shoe.localPosition = new Vector3(0f, 0.16f, 0f);
            b._shoe.localRotation = Quaternion.Euler(-60f, 0f, 0f);
            GrowthVfx.Block(b._shoe, "sole", new Vector3(0.13f, 0.035f, 0.28f), GrowthVfx.BarkLit);
            var strapA = GrowthVfx.Block(b._shoe, "strap-a", new Vector3(0.02f, 0.05f, 0.10f), GrowthVfx.Vine).transform;
            strapA.localPosition = new Vector3(0.03f, 0.03f, 0.04f); strapA.localRotation = Quaternion.Euler(0f, 30f, 0f);
            var strapB = GrowthVfx.Block(b._shoe, "strap-b", new Vector3(0.02f, 0.05f, 0.10f), GrowthVfx.Vine).transform;
            strapB.localPosition = new Vector3(-0.03f, 0.03f, 0.04f); strapB.localRotation = Quaternion.Euler(0f, -30f, 0f);
            return b;
        }

        public void Pose(float age, float loosen, bool pullable, float shotGrowth, float sinceShot)
        {
            bool landed = age >= 0f;
            _root.gameObject.SetActive(landed);
            if (!landed) return;
            // ⚠️ IT RISES OUT OF THE SOIL, IT DOES NOT SCALE IN (owner, 2026-09-26). The stem pushes up
            // from under the road over 0.45 s with a small overshoot, then squashes as it settles; the
            // roots creep out after it.
            float rise = GrowthVfx.Pop(age / 0.45f);
            float squash = age > 0.35f && age < 0.6f ? 1f + 0.18f * Mathf.Sin((age - 0.35f) / 0.25f * Mathf.PI) : 1f;
            _stem.localScale = new Vector3(squash, 1f / squash, squash);
            _stem.localPosition = Vector3.down * (0.95f * (1f - rise));
            float pop = Mathf.Clamp01((age - 0.15f) / 0.4f);

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
            // The pod opens as the slipper ripens: closed bud at 0, petals splayed at 1.
            float grown = Mathf.Clamp01(shotGrowth);
            for (int i = 0; i < _petals.Count; i++)
            {
                float open = Mathf.Lerp(-72f, -22f, grown) + 4f * Mathf.Sin(age * 1.8f + i);
                _petals[i].localRotation = Quaternion.Euler(0f, PetalYaw[i], 0f) * Quaternion.Euler(open, 0f, 0f);
            }
            _shoe.localScale = Vector3.one * Mathf.Max(0.001f, grown);
            for (int i = 0; i < _leafRenderers.Count; i++)
            {
                var r = _leafRenderers[i];
                if (r == null || r.sharedMaterial == null) continue;
                Color c = Color.Lerp(_leafFresh[i], GrowthVfx.Dry, loosen);
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
        private readonly List<Vector3> _spikeRest = new List<Vector3>();
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
                b._spikeRest.Add(pivot.localPosition);
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
            {
                // Up out of the road, not scaled in: each thorn punches up from below on its own beat.
                float up = GrowthVfx.Pop((age - 0.03f * i) / 0.22f);
                _spikes[i].localPosition = _spikeRest[i] + Vector3.down * (0.9f * (1f - up) + 0.6f * (1f - wither));
                _spikes[i].localScale = Vector3.one * (up > 0.001f ? 1f : 0f);
            }
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
        private readonly List<GrowthTwigs> _branchTwigs = new List<GrowthTwigs>();
        private readonly List<Transform> _thorns = new List<Transform>();
        private readonly List<CharacterMotor> _targets = new List<CharacterMotor>();
        private readonly List<Mesh> _tethers = new List<Mesh>();
        private readonly List<Vector3> _points = new List<Vector3>();
        private readonly List<float> _radii = new List<float>();
        private bool _podsDropped;

        // The eight ground vines: yaw, length, wave phase. Each typed, none the same.
        private static readonly float[] VineYaw = { 8f, 52f, 93f, 141f, 183f, 226f, 268f, 317f };
        private static readonly float[] VineLength = { 3.4f, 2.8f, 3.8f, 3.0f, 3.5f, 2.6f, 3.9f, 3.1f };
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

            // ⚠️⚠️ v4 (2026-09-26), the owner: *"it doesnt look that imposing"*, *"it doesnt make sense
            // too that the characters get pulled to smth that small"*, *"really refine and texture and
            // make it all detailed"*, and Groot's walls as the picture (research.md, "Groot's whole
            // arsenal"): a mass of THICK TWISTED TRUNKS BRAIDED round each other with dark vines wound
            // through them. v3 was one 2.1 m post, no taller than the bodies it held. v4 stands 4.3 m:
            // five trunks spiral up round a hollow heart where the core glows between them, each its own
            // girth, twist and colour; three dark vines wind round the bundle; green light shows through
            // cracks in the bark; bracket fungus and moss grow on it; the crown opens at the top.
            b._trunk = new GameObject("trunk").transform;
            b._trunk.SetParent(root, false);
            (float phase, float turns, float r0, float r1, float girth, Color bark)[] trunks =
            {
                (0.0f, 0.55f, 0.62f, 0.34f, 0.34f, GrowthVfx.BarkLit),
                (1.3f, 0.48f, 0.66f, 0.30f, 0.29f, GrowthVfx.Bark),
                (2.5f, 0.60f, 0.58f, 0.36f, 0.31f, GrowthVfx.BarkLit),
                (3.8f, 0.52f, 0.64f, 0.28f, 0.27f, GrowthVfx.BarkDark),
                (5.0f, 0.57f, 0.60f, 0.33f, 0.30f, GrowthVfx.Bark),
            };
            foreach (var tr in trunks)
            {
                var pts = new List<Vector3>(); var rad = new List<float>();
                for (int k = 0; k <= 12; k++)
                {
                    float u = k / 12f, a = tr.phase + u * tr.turns * Mathf.PI * 2f;
                    // Wide at the foot, pinched at the waist round the heart, flaring into the crown.
                    float r = Mathf.Lerp(tr.r0, tr.r1, u) * (1f - 0.28f * Mathf.Sin(u * Mathf.PI));
                    pts.Add(new Vector3(Mathf.Cos(a) * r, -0.08f + u * 3.9f, Mathf.Sin(a) * r));
                    rad.Add(tr.girth * Mathf.Lerp(1.25f, 0.72f, u));
                }
                var mesh = new Mesh { name = "PaeteSentryTrunk" };
                GrowthVfx.Tube(mesh, pts, rad, 6);
                GrowthVfx.Part(b._trunk, "trunk", mesh, tr.bark);
            }
            // Three dark vines winding round the bundle, each its own pitch and height.
            (float phase, float turns, float top, float r)[] coils = { (0.4f, 1.6f, 3.4f, 0.78f), (2.4f, 1.3f, 2.9f, 0.74f), (4.4f, 1.9f, 3.7f, 0.70f) };
            foreach (var c in coils)
            {
                var pts = new List<Vector3>(); var rad = new List<float>();
                for (int k = 0; k <= 28; k++)
                {
                    float u = k / 28f, a = c.phase + u * c.turns * Mathf.PI * 2f;
                    float r = c.r * (1f - 0.25f * Mathf.Sin(u * Mathf.PI));
                    pts.Add(new Vector3(Mathf.Cos(a) * r, 0.1f + u * c.top, Mathf.Sin(a) * r));
                    rad.Add(Mathf.Lerp(0.07f, 0.035f, u));
                }
                var mesh = new Mesh { name = "PaeteSentryCoil" };
                GrowthVfx.Tube(mesh, pts, rad, 5);
                GrowthVfx.Part(b._trunk, "vine-coil", mesh, GrowthVfx.Vine);
            }
            // Green light through cracks in the bark, low and high, so the tree reads as alive.
            (Vector3 at, float yaw, float h)[] cracks = { (new Vector3(0.38f, 1.25f, 0.30f), 38f, 0.55f), (new Vector3(-0.42f, 2.35f, 0.18f), -60f, 0.45f),
                                                          (new Vector3(0.05f, 0.75f, -0.50f), 175f, 0.40f), (new Vector3(0.30f, 2.9f, -0.32f), 130f, 0.35f) };
            foreach (var cr in cracks)
            {
                var glow = GrowthVfx.Block(b._trunk, "glow-crack", new Vector3(0.07f, cr.h, 0.05f), GrowthVfx.Glow, 1.3f).transform;
                glow.localPosition = cr.at; glow.localRotation = Quaternion.Euler(0f, cr.yaw, 12f);
            }
            // Bracket fungus: flat shelves on the bark, three of them, each its own size.
            (Vector3 at, float yaw, float w)[] shelves = { (new Vector3(0.62f, 1.05f, -0.10f), 80f, 0.34f), (new Vector3(0.58f, 1.22f, 0.05f), 95f, 0.24f),
                                                           (new Vector3(-0.50f, 1.85f, -0.30f), 235f, 0.30f) };
            foreach (var sh in shelves)
            {
                var shelf = GrowthVfx.Block(b._trunk, "bracket-fungus", new Vector3(sh.w, 0.06f, sh.w * 0.6f), GrowthVfx.BarkLit).transform;
                shelf.localPosition = sh.at; shelf.localRotation = Quaternion.Euler(0f, sh.yaw, -8f);
                var cap = GrowthVfx.Block(shelf, "fungus-rim", new Vector3(1.05f, 0.5f, 0.35f), GrowthVfx.Dry).transform;
                cap.localPosition = new Vector3(0f, -0.4f, 0.35f);
            }
            // Moss tufts on the bark, each its own place, size and lean.
            (Vector3 at, Vector3 size, float yaw)[] tufts = { (new Vector3(0.66f, 0.55f, 0.12f), new Vector3(0.16f, 0.30f, 0.36f), 20f),
                (new Vector3(-0.40f, 2.70f, -0.36f), new Vector3(0.32f, 0.20f, 0.14f), -40f), (new Vector3(-0.58f, 0.40f, 0.42f), new Vector3(0.36f, 0.18f, 0.18f), 35f) };
            foreach (var tf in tufts)
            {
                var tuft = GrowthVfx.Block(b._trunk, "moss-tuft", tf.size, GrowthVfx.Moss).transform;
                tuft.localPosition = tf.at; tuft.localRotation = Quaternion.Euler(0f, tf.yaw, 0f);
            }
            // Leaf litter round the foot: fallen leaves lying on the road, each its own turn.
            (Vector3 at, float yaw)[] litter = { (new Vector3(1.3f, 0.02f, 0.4f), 30f), (new Vector3(-1.1f, 0.02f, 0.9f), 140f), (new Vector3(0.4f, 0.02f, -1.4f), 250f),
                                                 (new Vector3(-1.5f, 0.02f, -0.6f), 300f), (new Vector3(1.0f, 0.02f, -1.0f), 80f), (new Vector3(0.2f, 0.02f, 1.6f), 200f) };
            for (int i = 0; i < litter.Length; i++)
            {
                var leaf = GrowthVfx.Part(root, "litter", GrowthVfx.Leaf(0.24f, 0.14f, 0.01f), i % 2 == 0 ? GrowthVfx.Dry : GrowthVfx.LeafDark).transform;
                leaf.localPosition = litter[i].at; leaf.localRotation = Quaternion.Euler(0f, litter[i].yaw, 0f);
            }

            // Six buttress roots, hugging the road: out from the bundle low, bending down into it.
            float[] rootYaw = { 15f, 75f, 130f, 200f, 250f, 315f };
            float[] rootReach = { 1.6f, 1.3f, 1.8f, 1.4f, 1.7f, 1.2f };
            for (int i = 0; i < rootYaw.Length; i++)
            {
                var pivot = new GameObject("buttress-" + i).transform;
                pivot.SetParent(b._trunk, false);
                pivot.localRotation = Quaternion.Euler(0f, rootYaw[i], 0f);
                var mesh = new Mesh { name = "PaeteSentryButtress" };
                float r = rootReach[i];
                GrowthVfx.Tube(mesh, new List<Vector3> { new Vector3(0, 0.85f, 0.45f), new Vector3(0, 0.36f, 0.80f),
                                                         new Vector3(0, 0.10f, 0.80f + r * 0.5f), new Vector3(0, -0.06f, 0.80f + r) },
                               new List<float> { 0.30f, 0.22f, 0.12f, 0.03f }, 5);
                GrowthVfx.Part(pivot, "root", mesh, i % 2 == 0 ? GrowthVfx.BarkDark : GrowthVfx.Bark);
                b._buttress.Add(pivot);
            }

            // The crown: five thick branches opening from the top of the bundle and curling in round
            // the core, each with a leaf cluster, the fingers holding the seed high over the prisoners.
            b._crownRoot = new GameObject("crown").transform;
            b._crownRoot.SetParent(b._trunk, false);
            b._crownRoot.localPosition = new Vector3(0f, 3.75f, 0f);
            for (int i = 0; i < ClawYaw.Length; i++)
            {
                var pivot = new GameObject("claw-" + i).transform;
                pivot.SetParent(b._crownRoot, false);
                pivot.localRotation = Quaternion.Euler(0f, ClawYaw[i], 0f);
                var mesh = new Mesh { name = "PaeteSentryClaw" };
                float lean = 0.08f * (i % 3);
                GrowthVfx.Tube(mesh, new List<Vector3> { new Vector3(0, -0.10f, 0.30f), new Vector3(0, 0.30f, 0.80f + lean),
                                                         new Vector3(0, 0.85f, 0.82f + lean), new Vector3(0, 1.25f, 0.38f) },
                               new List<float> { 0.22f, 0.15f, 0.09f, 0.025f }, 5);
                GrowthVfx.Part(pivot, "branch", mesh, i % 2 == 0 ? GrowthVfx.BarkLit : GrowthVfx.Bark);
                for (int k = 0; k < 4; k++)
                {
                    var leaf = GrowthVfx.Part(pivot, "leaf", GrowthVfx.Leaf(0.42f - 0.05f * k, 0.24f, 0.02f),
                                              k % 2 == 1 ? GrowthVfx.LeafDark : GrowthVfx.LeafGreen).transform;
                    leaf.localPosition = new Vector3(0.07f * (k - 1.5f), 1.15f - 0.16f * k, 0.44f + 0.10f * k);
                    leaf.localRotation = Quaternion.Euler(-60f + 25f * k, 35f * (k - 1.5f), 0f);
                }
                b._claws.Add(pivot);
            }

            // The core: a faceted seed held in the crown, two nested glowing blocks turned against each
            // other (the shell leaf-green, the heart the eye light), and four spores circling it.
            b._core = new GameObject("sentry-core").transform;
            b._core.SetParent(b._crownRoot, false);
            GrowthVfx.Block(b._core, "core-shell", Vector3.one, GrowthVfx.LeafGreen, 0.9f).transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            GrowthVfx.Block(b._core, "core-heart", Vector3.one * 0.80f, GrowthVfx.Glow, 1.5f).transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            for (int i = 0; i < 4; i++)
                b._spores.Add(GrowthVfx.Block(b._crownRoot, "spore-" + i, Vector3.one * 0.09f, GrowthVfx.Glow, 1.2f).transform);

            // The eight thorned ground vines: a mesh each, rebuilt while they grow, three thorns and a tip.
            for (int i = 0; i < VineYaw.Length; i++)
            {
                var mesh = new Mesh { name = "PaeteSentryVine" };
                mesh.MarkDynamic();
                // ⚠️ v4: bark branches, as the owner asked (*"tree vines/branches and not just green vines"*).
                GrowthVfx.Part(root, "ground-branch-" + i, mesh, i % 3 == 1 ? GrowthVfx.BarkDark : i % 3 == 2 ? GrowthVfx.BarkLit : GrowthVfx.Bark);
                b._vineMeshes.Add(mesh);
                b._branchTwigs.Add(new GrowthTwigs(root, new[] { 0.28f + 0.03f * (i % 3), 0.52f, 0.74f - 0.04f * (i % 2) },
                                                   new[] { 40f + 25f * i, 200f - 15f * i, 310f + 10f * i }, new[] { 1.4f, 1.2f, 1.0f }, GrowthVfx.BarkLit));
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
                GrowthVfx.Part(transform, "tether", mesh, GrowthVfx.BarkLit);
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
                float d = 1.0f + len * u;
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
            // ⚠️ IT COMES UP OUT OF THE ROAD, TWISTING (owner, 2026-09-26: *"coming out of the ground each
            // time and forming on the spot"*). The braided trunks screw up from 4.3 m below over 0.5 s,
            // turning a quarter turn as they rise, overshoot a little and settle; withering, they sink back.
            float erupt = GrowthVfx.Pop((age - 0.06f) / 0.50f);
            _trunk.localScale = Vector3.one;
            _trunk.localPosition = Vector3.down * (4.4f * (1f - Mathf.Clamp(erupt, 0f, 1.08f)) + 0.9f * wither * wither);
            _trunk.localRotation = Quaternion.Euler(0f, -95f * (1f - Mathf.Clamp01(erupt)), 0f);

            // 0.14 to 0.44: the buttress roots flare out, each a beat after the last.
            for (int i = 0; i < _buttress.Count; i++)
                _buttress[i].localScale = Vector3.one * Mathf.Max(0.001f, Mathf.Clamp01((age - 0.40f - 0.04f * i) / 0.30f));

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
            _core.localPosition = new Vector3(0f, 0.75f, 0f);
            _core.localRotation = Quaternion.Euler(0f, age * 35f, 0f);
            _core.localScale = Vector3.one * Mathf.Max(0.001f, 0.72f * coreIn * breathe * (1f - 0.8f * wither));
            for (int i = 0; i < _spores.Count; i++)
            {
                float a = age * (1.4f + 0.2f * i) + i * Mathf.PI * 0.5f;
                _spores[i].localPosition = new Vector3(Mathf.Cos(a) * 0.66f, 0.75f + Mathf.Sin(a * 1.7f) * 0.18f, Mathf.Sin(a) * 0.66f);
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
                    _branchTwigs[i].Place(_points, 0f, 1f, false);
                    continue;
                }
                VinePath(i, grow, age);
                _radii.Clear();
                for (int k = 0; k < _points.Count; k++) _radii.Add(Mathf.Lerp(0.22f, 0.04f, k / (float)VineSamples));
                GrowthVfx.Tube(_vineMeshes[i], _points, _radii, 6);
                _branchTwigs[i].Place(_points, grow, 1f, false);
                // Three thorns up the vine's back, at their own fractions, each out when the vine reaches it.
                for (int k = 0; k < 3; k++)
                {
                    int at = Mathf.Clamp(Mathf.RoundToInt((0.3f + 0.22f * k) * VineSamples), 1, VineSamples - 1);
                    var thorn = _thorns[i * 3 + k];
                    Vector3 along = (_points[at + 1] - _points[at - 1]).normalized;
                    thorn.localPosition = _points[at] + Vector3.up * 0.14f;
                    thorn.localRotation = Quaternion.LookRotation(along, Vector3.up) * Quaternion.Euler(-10f, 0f, 0f);
                    thorn.localScale = Vector3.one * 1.6f * Mathf.Clamp01((grow - (0.3f + 0.22f * k)) * 5f);
                }
                var tip = _vineTips[i];
                Vector3 last = _points[_points.Count - 1], before = _points[_points.Count - 2];
                tip.localPosition = last;
                tip.localRotation = Quaternion.LookRotation((last - before).normalized, Vector3.up);
                tip.localScale = Vector3.one * 1.6f * grow;
            }

            // The tethers: a braid-thick vine to each held body, taut while they are rooted.
            float catchAt = PaeteRules.SentryCatchSeconds;
            for (int i = 0; i < _targets.Count; i++)
            {
                var p = _targets[i];
                bool held = p != null && (age <= catchAt + 0.7f || p.IsRooted) && wither < 0.5f;
                if (!held) { _tethers[i].Clear(); continue; }
                Vector3 from = new Vector3(0f, 1.5f * erupt, 0f);
                Vector3 to = transform.InverseTransformPoint(p.transform.position + Vector3.up * 0.85f);
                Vector3 tip = Vector3.Lerp(from, to, GrowthVfx.Pop(Mathf.Clamp01(age / catchAt)));
                GrowthVfx.Curve(_points, from, tip, 14, 0.06f * Vector3.Distance(from, tip), 0.05f, i * 2.1f + age * 2.5f);
                _radii.Clear();
                for (int k = 0; k < _points.Count; k++) _radii.Add(Mathf.Lerp(0.17f, 0.06f, k / (float)(_points.Count - 1)));
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
