using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ AMIHAN RUNS ON THE AIR (owner, 2026-10-02: *"make amihans footstteps feel light and when she runs let her
    /// float a bit and make it look like she flying"*, then *"think abt how hhigh shhe hhas to float and also consider how
    /// shhe would jump tag throw etc and move"*). The numbers and every verb's answer are in
    /// `docs/reports/amihan-presentation-2026-10-02/light-body.md`.
    ///
    /// Her ordinary run lifts her soles 15 to 23 cm off the court (AIRBURST v3, 2026-10-03, the owner: *"make her shit feel
    /// lighter"*; it was 12 to 19), and walking 3 cm. The shared foot plant drops the hips about 7 cm when her legs are
    /// furthest apart; this lifts them by the same amount at the same moments, so her hips and head glide level while the
    /// legs scissor in the air beneath: flying, not bouncing. 23 cm is under a third of a jump's 0.84 m apex, under the 25 cm
    /// at which the gap reads as a jump, and far below Featherfall's 2.8 m, the one height at which she cannot be tagged; her
    /// shadow and ring stay on the court. Small curls of wind trail from her heels while she runs.
    ///
    /// The float keeps its OWN clock. The gait hands over to any action in 0.08 s; a float that rode the gait would drop her
    /// 19 cm in two frames at the start of every throw, tag and jump. So it rises over 0.22 s and settles over 0.2 s: she
    /// sinks onto her toes to charge a throw, a quick tag barely dips, a jump carries the float up and lets it go, and she
    /// lands on her toes (a small squash in place of the shared heavy one).
    ///
    /// Presentation only, on every peer, from the body's replicated state. Speed, capsule, contact, tag reach, throw origin
    /// and landing time are the game's. Runs after the authored body (-50) and before Carrier (0) samples her hand, so the
    /// held slipper rides with her. Featherfall's own layer (`AmihanFlightPose`, -25) owns her whenever she truly flies.
    /// </summary>
    [DefaultExecutionOrder(-24)]
    public sealed class AmihanAirStep : MonoBehaviour
    {
        /// <summary>Metres her soles clear the court walking; her feet still touch, the steps are just quick and quiet.</summary>
        public const float WalkLift = .03f;
        /// <summary>Metres her soles clear the court running, as the passing foot goes under her.</summary>
        public const float RunLow = .15f;
        /// <summary>Metres her soles clear the court running, with the legs furthest apart.</summary>
        public const float RunHigh = .23f;
        /// <summary>A heel curl's length, metres, trailing back and up from the lifting heel.</summary>
        private const float CurlLength = .34f;
        public const float RiseSeconds = .22f, SettleSeconds = .2f, AirSettleSeconds = .35f, StatusSettleSeconds = .1f;
        /// <summary>Her landing squash. The shared landing squashes every body 12 to 30 percent; she lands on her toes.</summary>
        public const float LandingSquash = .06f;

        private CharacterAnimator _animator;
        private CharacterMotor _body;
        private CharacterVisual _visual;
        private GameObject _model;
        private Transform _root, _torso, _legLeft, _legRight;
        private Vector3 _rootPosition;
        private Quaternion _torsoRotation, _legLeftRotation, _legRightRotation;
        private bool _applied, _wasGrounded = true;
        private float _run, _walk, _air, _fall, _curlClock;
        private readonly WindVfx.Ribbon[] _curls = new WindVfx.Ribbon[2];
        private Transform _curlHost;

        /// <summary>Lift above the court this frame, metres (probes and films).</summary>
        public float Lift { get; private set; }

        /// <summary>Give the body this layer exactly when its gait is hers (`CharacterAnimator.ResolveGaitStyle`).</summary>
        public static void Sync(CharacterAnimator animator, bool hers)
        {
            if (animator == null) return;
            var existing = animator.GetComponent<AmihanAirStep>();
            if (hers && existing == null) animator.gameObject.AddComponent<AmihanAirStep>();
            else if (!hers && existing != null)
            {
                existing.Restore();
                // A rebind in an editor test (one driver walked through every roster body) must not log Destroy's edit-mode error.
                if (Application.isPlaying) Destroy(existing); else DestroyImmediate(existing);
            }
        }

        private void Update() => Restore();
        private void OnDisable() { Restore(); _run = _walk = _air = _fall = 0; Lift = 0; HideCurls(); }
        private void OnDestroy() { if (_curlHost != null) { if (Application.isPlaying) Destroy(_curlHost.gameObject); else DestroyImmediate(_curlHost.gameObject); } }

        private void HideCurls() { foreach (var curl in _curls) curl?.Set(0, 0); }

        /// <summary>
        /// Her two heel curls: a short open hook of wind each, curling back and up off the heel, on their own host under the body
        /// (never the skinned bones, which the body re-poses every frame). Built on her first run; nobody else carries them.
        /// </summary>
        private void BuildCurls()
        {
            _curlHost = new GameObject("AmihanHeelCurls").transform;
            _curlHost.SetParent(transform, false);
            for (int i = 0; i < 2; i++)
            {
                var spine = new Vector3[12];
                for (int k = 0; k < spine.Length; k++)
                {
                    float u = k / (spine.Length - 1f), a = u * 1.5f * Mathf.PI;
                    // Back along the travel and up, hooking over at the end: an open curl, never a closed ring.
                    spine[k] = new Vector3(0, .05f * (1 - Mathf.Cos(a)) + .06f * u, -CurlLength * u + .05f * Mathf.Sin(a));
                }
                var host = new GameObject(i == 0 ? "HeelCurlLeft" : "HeelCurlRight").transform;
                host.SetParent(_curlHost, false);
                _curls[i] = WindVfx.Build(host, "HeelCurl", spine, .05f, WindVfx.Flat(spine), 3.0f, .3f, 230f + i);
            }
        }

        /// <summary>Pose the curls: each trails from its heel as that foot lifts behind her, while she runs.</summary>
        private void StepCurls(float lift, float dt)
        {
            float amount = _run * (1 - _air);
            if (amount <= .01f || _body.Velocity.sqrMagnitude < .5f) { HideCurls(); return; }
            if (_curlHost == null) BuildCurls();
            _curlClock += dt;
            var travel = _body.Velocity; travel.y = 0;
            var face = travel.sqrMagnitude > .01f ? Quaternion.LookRotation(travel.normalized) : transform.rotation;
            float phase = _animator.GaitPhase * 2f * Mathf.PI;
            for (int i = 0; i < 2; i++)
            {
                // The foot behind her is the one lifting off: left in one half of the cycle, right in the other.
                float behind = Mathf.Clamp01((i == 0 ? Mathf.Sin(phase) : -Mathf.Sin(phase)) * 1.6f);
                var host = _curls[i].GameObject.transform.parent;
                host.SetPositionAndRotation(transform.position + face * new Vector3(i == 0 ? -.1f : .1f, lift + .04f, -.12f), face);
                float calm = WindVfx.Reduced ? .6f : 1f;
                _curls[i].Set(.55f * amount * behind * calm, WindVfx.Reduced ? 0 : _curlClock * 6f, Mathf.Lerp(.35f, 1f, behind), 0, .3f + .5f * (1 - behind));
            }
        }

        private void Restore()
        {
            if (!_applied) return;
            if (_root != null) _root.localPosition = _rootPosition;
            if (_torso != null) _torso.localRotation = _torsoRotation;
            if (_legLeft != null) _legLeft.localRotation = _legLeftRotation;
            if (_legRight != null) _legRight.localRotation = _legRightRotation;
            _applied = false;
        }

        /// <summary>The bones the VISIBLE skin is bound to (a hero body also carries a hidden second rig).</summary>
        private void Resolve()
        {
            var model = _visual != null ? _visual.Model : null;
            if (model == _model) return;
            Restore();
            _model = model;
            _root = _torso = _legLeft = _legRight = null;
            if (model == null) return;
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!skin.enabled || skin.bones == null) continue;
                foreach (var bone in skin.bones)
                {
                    if (bone == null) continue;
                    switch (bone.name)
                    {
                        case "root": if (_root == null) _root = bone; break;
                        case "torso": if (_torso == null) _torso = bone; break;
                        case "leg-left": if (_legLeft == null) _legLeft = bone; break;
                        case "leg-right": if (_legRight == null) _legRight = bone; break;
                    }
                }
                if (_root != null) break;
            }
        }

        private static float Approach(float value, float target, float seconds, float dt)
            => Mathf.MoveTowards(value, target, dt / Mathf.Max(.01f, seconds));

        private void LateUpdate()
        {
            Restore();
            Lift = 0;
            if (_animator == null) _animator = GetComponent<CharacterAnimator>();
            if (_body == null) _body = GetComponent<CharacterMotor>();
            if (_visual == null) _visual = GetComponent<CharacterVisual>();
            if (_body == null || _animator == null) return;
            Resolve();
            if (_root == null) return;
            float dt = Mathf.Max(0, Time.deltaTime);

            bool grounded = _body.IsGrounded;
            bool held = _body.IsFlying || _body.IsStunned || _body.IsTripped || _body.IsRooted || _body.IsFrozen
                || _body.IsEdgeRecovering || _body.IsSwimming;
            // The gait's own amount already knows every authored action, the charge, emotes and cutscenes; only its
            // timing is replaced here.
            bool gait = grounded && !held && _animator.LocomotionArmAmount > .5f;
            float run = _animator.GaitRunWeight;

            float runTarget = gait ? run : 0;
            float walkTarget = gait ? 1 - run : 0;
            float settle = held ? StatusSettleSeconds : !grounded ? AirSettleSeconds : SettleSeconds;
            _run = Approach(_run, runTarget, runTarget > _run ? RiseSeconds : settle, dt);
            _walk = Approach(_walk, walkTarget, walkTarget > _walk ? RiseSeconds : settle, dt);

            // Landing: on her toes. Replaces the shared squash on the same frame, before it shows, and only where the
            // motor squashed at all (`CharacterMotor`'s landing floor), so stepping off a kerb gains nothing.
            if (grounded && !_wasGrounded && !_body.IsFlying && _fall > Core.Balance.LandSfxMinSpeed)
                (GetComponentInChildren<CharacterSquashStretch>() ?? GetComponentInParent<CharacterSquashStretch>())?.Squash(LandingSquash);
            _fall = grounded ? 0 : Mathf.Max(_fall, -_body.Velocity.y);
            _wasGrounded = grounded;

            // In the air on an ordinary jump (not Featherfall): legs trail back together and she leans into the travel.
            string clip = _animator.CurrentClipName;
            bool jumping = !grounded && !held && !_animator.IsPlayingAction && (clip == "jump" || clip == "fall");
            _air = Approach(_air, jumping ? 1 : 0, jumping ? .12f : .1f, dt);

            if (_run <= .001f && _walk <= .001f && _air <= .001f) { HideCurls(); return; }

            _rootPosition = _root.localPosition;
            if (_torso != null) _torsoRotation = _torso.localRotation;
            if (_legLeft != null) _legLeftRotation = _legLeft.localRotation;
            if (_legRight != null) _legRightRotation = _legRight.localRotation;
            _applied = true;

            // Level glide: highest exactly when the legs are furthest apart (the shared foot plant's lowest hips).
            // Two footfalls a cycle; +1 at passing.
            float passing = Mathf.Cos(4f * Mathf.PI * _animator.GaitPhase);
            float apart = .5f - .5f * passing;
            float lift = _run * Mathf.Lerp(RunLow, RunHigh, apart) + _walk * WalkLift;
            if (lift > 0) _root.position += transform.up * lift;
            Lift = lift;
            StepCurls(lift, dt);

            if (_air > .001f)
            {
                var velocity = transform.InverseTransformDirection(_body.Velocity);
                float forward = Mathf.Clamp01(new Vector2(velocity.x, velocity.z).magnitude / 4f);
                // Raw +x pitches a leg back and the torso forward (`HeroAbilityClips` conventions).
                if (_legLeft != null) _legLeft.localRotation *= Quaternion.Euler(_air * (14 + 10 * forward), 0, 0);
                if (_legRight != null) _legRight.localRotation *= Quaternion.Euler(_air * (22 + 8 * forward), 0, 0);
                if (_torso != null) _torso.localRotation *= Quaternion.Euler(_air * 8 * forward, 0, 0);
            }
        }
    }
}
