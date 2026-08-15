using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>Where a unit's facing comes from.</summary>
    public enum AimSource
    {
        /// <summary>Facing follows the movement direction.</summary>
        Movement,

        /// <summary>Facing follows the cursor. Changes how throwing and the taya's verbs aim.</summary>
        Mouse,
    }

    /// <summary>
    /// The follow camera.
    ///
    /// ⚠️⚠️ THE SELF-HIDE WALKS RENDERERS BY TYPE AND NEVER NAMES A MESH, and that is the
    /// single most important thing to preserve here. Because a roster character is the same rig
    /// wearing a different palette, hiding "the local player's own body" has to work for any
    /// pick automatically. The moment this file names a mesh or a child path, adding a
    /// character breaks the camera for that character only, which is the worst kind of bug to
    /// find: it looks like an art problem and it is a code problem.
    ///
    /// ⚠️ THE EYE HEIGHT AND CAPSULE BELONG TO THE PERSON ROLE, NOT TO A MODEL. 1.25 and 1.6.
    /// Everything ever tuned against a Person assumes them, so they are constants here rather
    /// than measurements off whatever mesh happens to be instanced.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public const float EyeHeight = 1.25f;
        public const float CapsuleHeight = 1.6f;

        [SerializeField] private CharacterMotor _target;
        [SerializeField] private AimSource _aimSource = AimSource.Mouse;

        [Header("Follow")]
        [SerializeField] private Vector3 _offset = new Vector3(0.0f, 11.0f, -9.0f);
        [SerializeField] private float _followLerp = 8.0f;
        [SerializeField] private float _pitchDegrees = 46.0f;

        private readonly List<Renderer> _hidden = new List<Renderer>();
        private CharacterMotor _hiddenFor;

        private float _shakeLeft;
        private float _shakeMagnitude;

        public AimSource Aim => _aimSource;
        public CharacterMotor Target => _target;

        public void Follow(CharacterMotor target)
        {
            _target = target;
            RefreshSelfHide();
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            if (_hiddenFor != _target) RefreshSelfHide();

            Vector3 focus = _target.transform.position + Vector3.up * EyeHeight;
            Vector3 wanted = focus + _offset;

            transform.position = Vector3.Lerp(transform.position, wanted,
                                              1.0f - Mathf.Exp(-_followLerp * Time.deltaTime));
            transform.rotation = Quaternion.Euler(_pitchDegrees, 0.0f, 0.0f);

            ApplyShake();
        }

        /// <summary>
        /// ⚠️ BY TYPE, NEVER BY NAME. See the class note: naming a mesh makes the camera
        /// character-specific, and every future roster addition is then a camera bug waiting
        /// to be reported as an art bug.
        /// </summary>
        private void RefreshSelfHide()
        {
            foreach (var r in _hidden)
                if (r != null) r.enabled = true;

            _hidden.Clear();
            _hiddenFor = _target;

            if (_target == null) return;

            foreach (var r in _target.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                if (!r.enabled) continue;

                r.enabled = false;
                _hidden.Add(r);
            }
        }

        /// <summary>
        /// ⚠️⚠️ SHAKE IS FOR THE PERSON IT HAPPENED TO, AND ONLY THEM. A body block shakes the
        /// BLOCKER's camera, because the block is a thing the blocker did and they need to feel
        /// it. Shaking every camera would tell three other players that something happened to
        /// them when nothing did.
        ///
        /// ⚠️ AND SHAKE IS NOT HITSTOP. Hitstop writes a GLOBAL time scale, which is acceptable
        /// for a shove on a long cooldown and completely wrong for a body block that can fire
        /// as fast as three attackers can throw. Never route a block through time scale.
        /// </summary>
        public void Shake(float magnitude, float duration)
        {
            _shakeMagnitude = Mathf.Max(_shakeMagnitude, magnitude);
            _shakeLeft = Mathf.Max(_shakeLeft, duration);
        }

        private void ApplyShake()
        {
            if (_shakeLeft <= 0.0f) return;

            _shakeLeft -= Time.deltaTime;
            float falloff = Mathf.Clamp01(_shakeLeft);

            transform.position += new Vector3(
                (Random.value - 0.5f) * 2.0f * _shakeMagnitude * falloff,
                (Random.value - 0.5f) * 2.0f * _shakeMagnitude * falloff,
                0.0f);

            if (_shakeLeft <= 0.0f) _shakeMagnitude = 0.0f;
        }

        /// <summary>
        /// Where the target should face this frame, in world space.
        ///
        /// ⚠️ THE AIM SOURCE CHANGES THE GAME, NOT JUST THE FEEL. Under Mouse the player can
        /// face one way and run another, which is what makes a retrieval run out of the box
        /// while still aiming at the can possible. Under Movement they cannot. Keep both, and
        /// keep the choice explicit.
        /// </summary>
        public Vector3 AimPointFor(CharacterMotor who, UnityEngine.Camera cam)
        {
            if (who == null) return Vector3.zero;

            if (_aimSource == AimSource.Movement)
            {
                Vector2 move = who.Intent.MoveAxis;
                Vector3 dir = new Vector3(move.x, 0.0f, move.y);

                if (dir.sqrMagnitude < 0.01f) dir = who.transform.forward;
                return who.transform.position + dir.normalized * 10.0f;
            }

            if (cam == null) return who.transform.position + who.transform.forward * 10.0f;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            var ground = new Plane(Vector3.up, new Vector3(0.0f, who.transform.position.y, 0.0f));

            return ground.Raycast(ray, out float enter)
                ? ray.GetPoint(enter)
                : who.transform.position + who.transform.forward * 10.0f;
        }
    }
}
