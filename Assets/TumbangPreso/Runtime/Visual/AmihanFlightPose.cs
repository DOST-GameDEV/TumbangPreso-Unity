using UnityEngine;

namespace TumbangPreso.Visual
{
    // After the authored body (-50), before Carrier (0) samples its hand. This layer
    // is attached only by Featherfall and never changes the movement or gait rig.
    [DefaultExecutionOrder(-25)]
    public sealed class AmihanFlightPose : MonoBehaviour
    {
        private CharacterMotor _body;
        private CharacterVisual _visual;
        private CharacterAnimator _animator;
        private Abilities.HeroKit _kit;
        private AmihanHoverRing _ring;
        private Vector2 _legLeft, _legLeftSpeed, _legRight, _legRightSpeed;
        private int _movementEpoch;
        private GameObject _model;
        private readonly Transform[] _bones = new Transform[7];
        private readonly Quaternion[] _rotations = new Quaternion[7];
        private Vector3 _rootPosition;
        private Vector2 _lean;
        private float _descent, _clock, _landing = -1;
        private bool _applied, _wasDescending;
        public Transform LeftPalm { get; private set; }
        public Transform RightPalm { get; private set; }
        public bool BelongsTo(CharacterMotor body)
            => enabled && _body == body && _kit == body.AbilitySystem?.Kit && _movementEpoch == body.MovementEpoch;

        public static AmihanFlightPose Attach(CharacterMotor body)
        {
            AmihanFlightPose pose = null;
            foreach (var candidate in body.GetComponents<AmihanFlightPose>())
                if (candidate.BelongsTo(body)) { pose = candidate; break; }
                else candidate.Cancel();
            if (pose == null) pose = body.gameObject.AddComponent<AmihanFlightPose>();
            pose._body = body;
            pose._visual = body.GetComponent<CharacterVisual>();
            pose._animator = body.GetComponent<CharacterAnimator>();
            pose._kit = body.AbilitySystem?.Kit;
            pose._movementEpoch = body.MovementEpoch;
            pose._landing = -1;
            pose._wasDescending = false;
            return pose;
        }

        private void Restore()
        {
            if (!_applied) return;
            for (int i = 0; i < _bones.Length; i++)
                if (_bones[i] != null) _bones[i].localRotation = _rotations[i];
            if (_bones[0] != null) _bones[0].localPosition = _rootPosition;
            _applied = false;
        }

        private void Update() => Restore();
        private void OnDisable() => Restore();
        private void OnDestroy()
        {
            Restore();
            if (LeftPalm != null) Destroy(LeftPalm.gameObject);
            if (RightPalm != null) Destroy(RightPalm.gameObject);
        }

        public void Cancel()
        {
            Restore();
            if (_animator != null) _animator.CancelHeroAction("hero-amihan-updraft", "updraft-lift");
            enabled = false;
            Destroy(this);
        }

        private void Resolve()
        {
            var model = _visual != null ? _visual.Model : null;
            if (model == _model) return;
            Restore();
            if (LeftPalm != null) Destroy(LeftPalm.gameObject);
            if (RightPalm != null) Destroy(RightPalm.gameObject);
            LeftPalm = RightPalm = null;
            System.Array.Clear(_bones, 0, _bones.Length);
            _model = model;
            if (model == null) return;
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!skin.enabled || skin.sharedMesh == null) continue;
                for (int i = 0; i < skin.bones.Length; i++)
                {
                    var bone = skin.bones[i];
                    if (bone == null) continue;
                    switch (bone.name)
                    {
                        case "root": _bones[0] = bone; break;
                        case "torso": _bones[1] = bone; break;
                        case "head": _bones[2] = bone; break;
                        case "leg-left": _bones[3] = bone; break;
                        case "leg-right": _bones[4] = bone; break;
                        case "arm-left":
                            if (LeftPalm == null) LeftPalm = Palm(skin, i, "FeatherfallLeftPalm");
                            break;
                        case "arm-right":
                            if (RightPalm == null) RightPalm = Palm(skin, i, "FeatherfallRightPalm");
                            break;
                    }
                }
            }
        }

        private static Transform Palm(SkinnedMeshRenderer skin, int index, string name)
        {
            if (!CharacterVisual.PalmCentre(skin, index, out var centre)) return null;
            var palm = new GameObject(name).transform;
            palm.SetParent(skin.bones[index], false);
            palm.localPosition = centre;
            return palm;
        }

        private void LateUpdate()
        {
            Restore();
            if (_body == null || !BelongsTo(_body) || _kit?.HeroId != "amihan") { Cancel(); return; }
            Resolve();
            float dt = Time.deltaTime;
            bool descending = _body.IsFlying && !_body.IsAloft;
            if (_wasDescending && _body.IsGrounded && !_body.IsFlying) _landing = 0;
            _wasDescending = descending;
            if (_landing >= 0) _landing += dt;
            bool flying = _body.IsFlying;
            if (!flying && (_landing < 0 || _landing >= .28f)) { Cancel(); return; }
            if (_model == null) return;
            if (_body.IsStunned || _body.IsTripped || _body.IsRooted || _body.IsSwimming || _body.IsEdgeRecovering) return;

            var velocity = _body.transform.InverseTransformDirection(_body.Velocity);
            var target = flying ? new Vector2(Mathf.Clamp(velocity.z * 1.6f, -7, 10), Mathf.Clamp(-velocity.x * 2, -9, 9)) : Vector2.zero;
            _lean = Vector2.Lerp(_lean, target, 1 - Mathf.Exp(-7 * dt));
            _descent = Mathf.MoveTowards(_descent, descending ? 1 : 0, dt * 5);
            for (int i = 0; i < _bones.Length; i++)
                if (_bones[i] != null) _rotations[i] = _bones[i].localRotation;
            if (_bones[0] != null) _rootPosition = _bones[0].localPosition;
            _applied = true;

            float land = _landing >= 0 ? Mathf.Sin(Mathf.Clamp01(_landing / .28f) * Mathf.PI) : 0;
            // RIDING THE UPDRAFT. The owner on v4 (2026-10-03): *"doesnt feel like she's flyying and doesnt feel like she has
            // air pushing her up"*. v4 sat her on the breeze (knees forward, arms at her sides), which read as a chair in the
            // sky. Now the air under her carries her body: legs hang loose and trail with a lazy kick, chest up, arms spread
            // wide palms down as if leaning on the air, and each gust that breaks against her soles (`AmihanHoverRing.Push`)
            // lifts her, opens her arms and swings her legs, then she settles back onto it. She reaches down to land.
            if (_ring == null) _ring = GetComponentInChildren<AmihanHoverRing>();
            float push = _ring != null ? _ring.Push : 0;
            float rest = flying ? 1 - .7f * _descent : 0;
            _clock += dt; // the presentation clock: a held match holds the sway too
            float drift = Mathf.Sin(_clock * 1.1f + 1.3f);
            Turn(1, new Vector3(_lean.x + 5 * land - 5 * rest - 4 * push, 0, _lean.y + 2.5f * drift * rest));
            Turn(2, new Vector3(-_lean.x * .45f - 3 * land - 6 * rest - 3 * push, 0, -_lean.y * .4f - 1.5f * drift * rest));
            bool launch = _animator != null && _animator.CurrentClipName == "hero-amihan-updraft" && _animator.IsPlayingAction;
            if (!launch)
            {
                // A charge/throw owns the arms; its standing clip must not straighten the airborne legs.
                float reach = 6 * _descent;
                Dangle(ref _legLeft, ref _legLeftSpeed, velocity, push, 38, 1.3f, 0f, dt);
                Dangle(ref _legRight, ref _legRightSpeed, velocity, push, 31, 1.75f, 2.1f, dt);
                Turn(3, new Vector3((8 + _legLeft.x) * rest - reach - 8 * land, 0, (-5 + _legLeft.y) * rest + 3 * land));
                Turn(4, new Vector3((4 + _legRight.x) * rest + reach + 6 * land, 0, (5 + _legRight.y) * rest - 3 * land));
                if (_animator == null || !_animator.IsPlayingAction)
                {
                    // Arms spread wide on the air, rising with each gust and sinking back onto it.
                    float spread = (38 + 4 * drift + 14 * push) * rest;
                    Turn(5, new Vector3(-6 * rest, 0, -spread));
                    Turn(6, new Vector3(-6 * rest, 0, spread));
                }
            }
            // Carried: a slow bob on the column, a lift on every gust.
            float carried = (.03f * Mathf.Sin(_clock * 2.0f) + .07f * push) * rest;
            if (_bones[0] != null) _bones[0].localPosition += Vector3.up * carried + Vector3.down * (.025f * land);
        }

        /// <summary>
        /// DANGLING (owner: *"make her legs feel like theyre dangling"*, *"she looks like she is RIGIDLy flying"*): each leg is a
        /// loose, under-damped pendulum. It trails behind her travel (x back, y to the side), swings past when she stops, is
        /// kicked by each gust and drifts on its own slow rhythm, the two legs at different rates so they never move as one.
        /// </summary>
        private void Dangle(ref Vector2 angle, ref Vector2 speed, Vector3 velocity, float push, float stiffness, float rate, float offset, float dt)
        {
            dt = Mathf.Min(dt, 1f / 30f);
            var target = new Vector2(Mathf.Clamp(velocity.z * 3.5f, -8, 16) + 6 * Mathf.Sin(_clock * rate + offset),
                Mathf.Clamp(-velocity.x * 2.5f, -9, 9) + 3 * Mathf.Sin(_clock * rate * .7f + offset + 1));
            speed += (stiffness * (target - angle) - 3.2f * speed) * dt;
            speed.x += push * 140 * dt;
            angle += speed * dt;
            angle = Vector2.ClampMagnitude(angle, 30);
        }

        private void Turn(int index, Vector3 angles)
        {
            if (_bones[index] != null) _bones[index].localRotation *= Quaternion.Euler(angles);
        }
    }
}
