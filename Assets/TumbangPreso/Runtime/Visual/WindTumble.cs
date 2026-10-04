using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ BLOWN BACK BY HER WIND (owner, 2026-10-03, AIRBURST v3.2: *"refine animation of getting pushed back make it really
    /// look liek they lost control of their body and are pushed back by wind"*, *"make it WAY more dramatic"*).
    ///
    /// A Whirled body thrown by the wind is not a person moving: the wind has it. So for as long as it travels fast, this layer
    /// takes the body from the authored pose and gives it to the air:
    ///  * THE BODY goes over backwards, away from the wind (the chest leads, the head trails), and is spun round by it (Whirled
    ///    is a spin), wobbling on its third axis so the tumble never reads as a clean rotation.
    ///  * THE ARMS windmill out of phase, flung wide, grabbing at nothing; THE LEGS kick, one up while the other trails.
    ///  * THE HEAD whips back on the throw and snaps round with each turn.
    /// Its strength follows the body's own measured speed (every peer measures it the same way from the replicated motion),
    /// so it is total at the throw, eases as they slow, and is gone a beat after they are back on the court and slow. The
    /// cutscene's copies of the real players use the same pose (<see cref="Pose"/>), so what the cutscene promised is what
    /// play shows.
    ///
    /// Presentation only, on every peer, from replicated state (`IsWhirled`, the body's motion). It never moves the motor,
    /// its capsule, its contact or anything sent. Runs after the authored body (-50) and her air step (-24).
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public sealed class WindTumble : MonoBehaviour
    {
        /// <summary>Metres per second at which the wind has the body completely.</summary>
        public const float FullSpeed = 7.0f;
        private CharacterMotor _body;
        private CharacterVisual _visual;
        private GameObject _model;
        private Transform _root, _torso, _head, _armLeft, _armRight, _legLeft, _legRight;
        private Quaternion _rRoot, _rTorso, _rHead, _rArmL, _rArmR, _rLegL, _rLegR;
        private Vector3 _pRoot;
        private bool _applied;
        private Vector3 _last, _travel = Vector3.forward;
        private float _amount, _clock, _spin;
        private bool _seeded;

        /// <summary>How much the wind has the body this frame, 0 to 1 (probes and the camera).</summary>
        public float Amount => _amount;

        public static WindTumble Attach(CharacterMotor body)
        {
            if (body == null) return null;
            var existing = body.GetComponent<WindTumble>();
            return existing != null ? existing : body.gameObject.AddComponent<WindTumble>();
        }

        private void Awake() { _body = GetComponent<CharacterMotor>(); _visual = GetComponent<CharacterVisual>(); }
        private void Update() => Restore();
        private void OnDisable() { Restore(); _amount = 0; }

        private void Restore()
        {
            if (!_applied) return;
            if (_root != null) { _root.localRotation = _rRoot; _root.localPosition = _pRoot; }
            if (_torso != null) _torso.localRotation = _rTorso;
            if (_head != null) _head.localRotation = _rHead;
            if (_armLeft != null) _armLeft.localRotation = _rArmL;
            if (_armRight != null) _armRight.localRotation = _rArmR;
            if (_legLeft != null) _legLeft.localRotation = _rLegL;
            if (_legRight != null) _legRight.localRotation = _rLegR;
            _applied = false;
        }

        private void Resolve()
        {
            var model = _visual != null ? _visual.Model : null;
            if (model == _model) return;
            Restore();
            _model = model;
            _root = _torso = _head = _armLeft = _armRight = _legLeft = _legRight = null;
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
                        case "head": if (_head == null) _head = bone; break;
                        case "arm-left": if (_armLeft == null) _armLeft = bone; break;
                        case "arm-right": if (_armRight == null) _armRight = bone; break;
                        case "leg-left": if (_legLeft == null) _legLeft = bone; break;
                        case "leg-right": if (_legRight == null) _legRight = bone; break;
                    }
                }
                if (_root != null) break;
            }
        }

        private void LateUpdate()
        {
            Restore();
            if (_body == null) { Destroy(this); return; }
            Resolve();
            float dt = Mathf.Max(0f, Time.deltaTime);
            var at = transform.position;
            if (!_seeded) { _last = at; _seeded = true; }
            var moved = at - _last; _last = at;
            var flat = new Vector3(moved.x, 0f, moved.z);
            float speed = dt > 1e-5f ? flat.magnitude / dt : 0f;
            if (flat.sqrMagnitude > 1e-6f) _travel = Vector3.Slerp(_travel, flat.normalized, 1f - Mathf.Exp(-12f * dt));
            // The wind has them while they are Whirled and travelling fast; it lets go over a short beat once they slow.
            float want = _body.IsWhirled ? Mathf.Clamp01((speed - 1.5f) / (FullSpeed - 1.5f)) : 0f;
            if (!_body.IsGrounded && _body.IsWhirled) want = Mathf.Max(want, .7f);
            _amount = Mathf.MoveTowards(_amount, want, dt * (want > _amount ? 8f : 2.2f));
            if (_amount <= .001f)
            {
                if (!_body.IsWhirled) Destroy(this);
                return;
            }
            _clock += dt;
            _spin += dt * 540f * _amount;
            if (_root == null) return;
            _rRoot = _root.localRotation; _pRoot = _root.localPosition;
            if (_torso != null) _rTorso = _torso.localRotation;
            if (_head != null) _rHead = _head.localRotation;
            if (_armLeft != null) _rArmL = _armLeft.localRotation;
            if (_armRight != null) _rArmR = _armRight.localRotation;
            if (_legLeft != null) _rLegL = _legLeft.localRotation;
            if (_legRight != null) _rLegR = _legRight.localRotation;
            _applied = true;
            var travelLocal = transform.InverseTransformDirection(_travel);
            Pose(_root, _torso, _head, _armLeft, _armRight, _legLeft, _legRight, travelLocal, _amount, _clock, _spin,
                 Settings.SettingsStore.Current.ReducedEffects);
        }

        /// <summary>
        /// Lay the tumble over the current pose. <paramref name="travel"/> is the way the wind carries the body, in the body
        /// root's parent frame; <paramref name="amount"/> 0 to 1; <paramref name="clock"/> seconds since the throw;
        /// <paramref name="spin"/> degrees the wind has turned it. Shared with the cutscene's copies (`HeroIntroductionScene.AmihanLane.cs`).
        /// </summary>
        public static void Pose(Transform root, Transform torso, Transform head, Transform armLeft, Transform armRight,
                                Transform legLeft, Transform legRight, Vector3 travel, float amount, float clock, float spin, bool calm)
        {
            if (amount <= 0f) return;
            travel.y = 0f;
            if (travel.sqrMagnitude < 1e-6f) travel = Vector3.back;
            travel.Normalize();
            float flail = calm ? .45f : 1f;
            if (root != null)
            {
                // Over backwards away from the wind: tip about the axis across the travel, the chest leading.
                var across = Vector3.Cross(Vector3.up, travel);
                float over = 70f * amount + 12f * Mathf.Sin(clock * 6.5f) * amount;
                float wobble = 18f * Mathf.Sin(clock * 9.3f + 1.1f) * amount * flail;
                var tumble = Quaternion.AngleAxis(-over, across) * Quaternion.AngleAxis(wobble, travel)
                           * Quaternion.AngleAxis(spin, Vector3.up);
                root.localRotation = tumble * root.localRotation;
                // Pivot about the middle of the body, not the feet: lift the root so the tipped body does not sink into the court.
                root.localPosition += Vector3.up * .45f * amount;
            }
            if (torso != null) torso.localRotation *= Quaternion.Euler(-22f * amount, 14f * Mathf.Sin(clock * 7f) * amount * flail, 0f);
            if (head != null) head.localRotation *= Quaternion.Euler(-34f * amount + 10f * Mathf.Sin(clock * 11f) * amount * flail,
                                                                     22f * Mathf.Sin(clock * 8.3f + .4f) * amount * flail, 0f);
            // Windmilling arms, out of phase, flung wide (raw: -x raises forward, roll spreads; the authoring tool's convention).
            float wl = clock * 15f, wr = clock * 15f + 2.4f;
            if (armLeft != null)
                armLeft.localRotation = Quaternion.Slerp(armLeft.localRotation,
                    Quaternion.Euler(-90f + 110f * Mathf.Sin(wl) * flail, 20f * Mathf.Cos(wl), 20f + 30f * Mathf.Cos(wl * .5f) * flail), amount);
            if (armRight != null)
                armRight.localRotation = Quaternion.Slerp(armRight.localRotation,
                    Quaternion.Euler(-90f + 110f * Mathf.Sin(wr) * flail, -20f * Mathf.Cos(wr), -20f - 30f * Mathf.Cos(wr * .5f) * flail), amount);
            // Kicking legs: one up while the other trails.
            float k = Mathf.Sin(clock * 11f) * flail;
            if (legLeft != null) legLeft.localRotation = Quaternion.Slerp(legLeft.localRotation, Quaternion.Euler(-45f * k - 20f, 0f, -12f), amount);
            if (legRight != null) legRight.localRotation = Quaternion.Slerp(legRight.localRotation, Quaternion.Euler(45f * k - 20f, 0f, 12f), amount);
        }
    }
}
