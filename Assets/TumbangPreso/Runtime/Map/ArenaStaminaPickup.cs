using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// Arena's stamina pickup (docs/ARENA_MAP_BRIEF.md, the design's point 4): a grounded body that
    /// walks onto it with less than a full bar takes it, its bar is refilled and its fatigue
    /// cleared, and the orb is gone for `RespawnSeconds`.
    ///
    /// THE HOST DECIDES (`NetAuthority.ShouldResolve`). It calls `Stamina.RefillAndClearFatigue`
    /// on the body, which reaches the owner through `SyncUnit` like every other host change to
    /// the bar.
    ///
    /// GREY-BOX: NO MESSAGE YET, SO A CLIENT ONLY GUESSES WHETHER THE ORB IS THERE. Whether an orb
    /// is taken or back is new shared state. The agreed design gives it ONE new host message with
    /// the pickups' availability, also in the late-join snapshot, shipped under the map's
    /// protocol bump. Until that exists a peer that is not the host infers "taken" FOR DISPLAY
    /// ONLY from the replicated bodies: the same proximity test, or a bar that jumps up while its
    /// body stands here, starts the same local timer. It grants nothing, it can miss a take or
    /// disagree with the host by a moment, and a late join sees every orb as present. When the
    /// message lands, `Infer` goes and the host's availability is applied in its place.
    ///
    /// THE LOOK is the stage kit's model when the scene builder has put one under this object
    /// (a child named `Model`, parts `pickup_base`, `pickup_cell` and `pickup_halo`, violet): the
    /// cell floats and turns inside its halo over the base, and both are hidden while taken.
    /// With no model it is the GREY-BOX, built here from primitives: a violet orb floating over a
    /// dark disc. Nothing near the team hues (#f87020, #0080e8).
    ///
    /// It belongs to a layout and is switched on and off with it; it is available again on
    /// enable and on every round change.
    /// </summary>
    public sealed class ArenaStaminaPickup : MonoBehaviour
    {
        /// <summary>Seconds the orb stays gone after it is taken.</summary>
        public float RespawnSeconds = 10.0f;

        /// <summary>How close in plan a body must be, metres.</summary>
        private const float TakeRadius = 1.2f;
        /// <summary>How far above or below the spot a body's feet may be.</summary>
        private const float Reach = 1.0f;
        /// <summary>A client's display guess allows for the replica trailing its owner.</summary>
        private const float InferSlack = 0.3f;
        /// <summary>A bar that rises this much of its length in one frame was refilled, not regenerated.</summary>
        private const float RefillJump = 0.05f;

        private static readonly Color Orb = new Color(0.72f, 0.42f, 1.0f);
        private static readonly Color Base = new Color(0.20f, 0.16f, 0.30f);
        private const float OrbSize = 0.42f, OrbHeight = 0.9f, OrbBob = 0.1f;

        private CharacterMotor[] _scanned = System.Array.Empty<CharacterMotor>();
        private readonly Dictionary<CharacterMotor, float> _lastRatio = new Dictionary<CharacterMotor, float>();
        private float _rescan;
        private float _takenLeft;       // above 0 while the orb is gone
        private int _round = -1;

        /// <summary>The child the scene builder hangs the kit's model on.</summary>
        public const string ModelName = "Model";
        /// <summary>Where the kit floats its cell and halo over the base (tools/author_arena_stage.py).</summary>
        private const float ModelOrbHeight = 0.95f;
        private Transform _halo;
        private float _height = OrbHeight;
        private bool _modelled;

        private Transform _orb;
        private Material _orbMaterial, _baseMaterial;

        /// <summary>True while the orb is there to take.</summary>
        public bool Available => _takenLeft <= 0.0f;

        private void Start() => Build();

        private void OnEnable() => Restore();

        private void OnDisable() => Restore();

        private void OnDestroy()
        {
            if (_orbMaterial != null) Destroy(_orbMaterial);
            if (_baseMaterial != null) Destroy(_baseMaterial);
        }

        private void Restore()
        {
            _takenLeft = 0.0f;
            _rescan = 0.0f;
            _lastRatio.Clear();
            _round = GameServices.Match != null ? GameServices.Match.RoundNumber : -1;
            if (_orb != null) _orb.gameObject.SetActive(true);
            if (_halo != null) _halo.gameObject.SetActive(true);
        }

        private void Update()
        {
            // `RoundNumber` rides `SyncWorld`, so every peer sees the round change and resets alike.
            int round = GameServices.Match != null ? GameServices.Match.RoundNumber : -1;
            if (round != _round) Restore();

            if (_takenLeft > 0.0f)
            {
                _takenLeft = Mathf.Max(0.0f, _takenLeft - Time.deltaTime);
                _lastRatio.Clear();
            }
            else if (!PresentationClock.Held)
            {
                if (NetAuthority.ShouldResolve()) Resolve();
                else if (NetAuthority.IsNetworked && !NetAuthority.IsHost) Infer();
            }

            if (_orb == null) return;
            _orb.gameObject.SetActive(Available);
            if (_halo != null) _halo.gameObject.SetActive(Available);
            if (!Available) return;
            float t = Time.time * 1.7f + transform.position.x * 0.37f;
            _orb.localPosition = new Vector3(0.0f, _height + Mathf.Sin(t) * OrbBob, 0.0f);
            if (_modelled) _orb.localRotation = Quaternion.Euler(0.0f, t * 40.0f, 0.0f);
        }

        private IReadOnlyList<CharacterMotor> Bodies()
        {
            var round = GameServices.Round;
            if (round != null) return round.Players;

            _rescan -= Time.deltaTime;
            if (_rescan <= 0.0f)
            {
                _rescan = 1.0f;
                _scanned = FindObjectsByType<CharacterMotor>();
            }
            return _scanned;
        }

        private bool Within(CharacterMotor motor, float radius)
        {
            Vector3 d = motor.transform.position - transform.position;
            return Mathf.Abs(d.y) <= Reach && d.x * d.x + d.z * d.z <= radius * radius;
        }

        private static bool NotFull(CharacterMotor motor)
            => motor.Stamina.IsFatigued || motor.Stamina.Current < Core.Balance.StaminaMax - 0.01f;

        /// <summary>The host's decision, and the solo game's.</summary>
        private void Resolve()
        {
            foreach (var motor in Bodies())
            {
                if (motor == null || !motor.IsGrounded || !Within(motor, TakeRadius) || !NotFull(motor)) continue;
                motor.Stamina.RefillAndClearFatigue();
                _takenLeft = Mathf.Max(0.0f, RespawnSeconds);
                NetCue.PlayVaried("pickup", transform.position, 1.2f, 1.35f, 0.9f);
                return;
            }
        }

        /// <summary>
        /// A client's guess, for the orb's display only (see the summary). No bar is touched and no
        /// cue is played: the host's cue arrives through `NetCue`.
        /// </summary>
        private void Infer()
        {
            bool taken = false;
            foreach (var motor in Bodies())
            {
                if (motor == null) continue;
                float ratio = motor.Stamina.Ratio;
                bool near = motor.IsGrounded && Within(motor, TakeRadius + InferSlack);
                bool refilled = _lastRatio.TryGetValue(motor, out float before) && ratio - before > RefillJump;
                if (near && (refilled || (NotFull(motor) && Within(motor, TakeRadius)))) taken = true;
                _lastRatio[motor] = ratio;
            }
            if (taken) _takenLeft = Mathf.Max(0.0f, RespawnSeconds);
        }

        // ------------------------------------------------------------------ the grey-box look

        private void Build()
        {
            var model = transform.Find(ModelName);
            if (model != null)
            {
                foreach (var part in model.GetComponentsInChildren<Transform>(true))
                {
                    if (part.name.StartsWith("pickup_cell", System.StringComparison.Ordinal)) _orb = part;
                    else if (part.name.StartsWith("pickup_halo", System.StringComparison.Ordinal)) _halo = part;
                }

                if (_orb != null)
                {
                    _modelled = true;
                    _height = ModelOrbHeight;
                    _orb.localPosition = new Vector3(0.0f, _height, 0.0f);
                    if (_halo != null) _halo.localPosition = new Vector3(0.0f, _height, 0.0f);
                    _orb.gameObject.SetActive(Available);
                    if (_halo != null) _halo.gameObject.SetActive(Available);
                    return;
                }
            }

            // Sprites/Default: unlit, alpha blended, two-sided, and always in a build.
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) return;

            var look = new GameObject("Look").transform;
            look.SetParent(transform, false);

            _baseMaterial = new Material(shader) { color = Base };
            var disc = Part(look, PrimitiveType.Cylinder, "Base", _baseMaterial);
            disc.localScale = new Vector3(0.9f, 0.01f, 0.9f);
            disc.localPosition = new Vector3(0.0f, 0.012f, 0.0f);

            _orbMaterial = new Material(shader) { color = Orb };
            _orb = Part(look, PrimitiveType.Sphere, "Orb", _orbMaterial);
            _orb.localScale = Vector3.one * OrbSize;
            _orb.localPosition = new Vector3(0.0f, OrbHeight, 0.0f);
            _orb.gameObject.SetActive(Available);
        }

        /// <summary>A primitive with its collider taken off before physics can see it: the pickup blocks nobody.</summary>
        private static Transform Part(Transform parent, PrimitiveType shape, string name, Material material)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            var solid = go.GetComponent<Collider>();
            if (solid != null) { solid.enabled = false; Destroy(solid); }
            go.transform.SetParent(parent, false);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go.transform;
        }
    }
}
