using UnityEngine;
using UnityEngine.Playables;

namespace TumbangPreso.Visual
{
    public sealed partial class CharacterAnimator
    {
        private Transform[] _arrivalBones;
        private readonly Quaternion[] _arrivalUnder = new Quaternion[4];
        private bool _arrivalApplied;
        private int _arrivalSlot;
        private float _arrivalWeight;

        // A presentation-only greeting. No gameplay action, emote RPC or physical root movement.
        public void SetArrivalPose(int slot, float weight)
        {
            if (_arrivalBones == null)
            {
                var model = GetComponent<CharacterVisual>()?.Model;
                if (model == null) return;
                _arrivalBones = new Transform[4];
                foreach (var bone in model.GetComponentsInChildren<Transform>(true))
                {
                    int index = bone.name == "torso" ? 0 : bone.name == "head" ? 1 :
                        bone.name == "arm-left" ? 2 : bone.name == "arm-right" ? 3 : -1;
                    if (index >= 0) _arrivalBones[index] = bone;
                }
            }
            if (weight > 0 && _arrivalWeight <= 0 && _graph.IsValid())
            {
                // Spawn can precede the first grounded physics sample. Do not freeze the
                // fall/landing clip underneath the greetings while the presentation clock holds.
                RestoreArrivalPose(); RestoreEdgeRecoveryPose(); RestoreIntroductionPose();
                RestoreTagBody(); RestoreResetRaise(); RestoreLocomotionArms();
                RestoreThrowBody(); RestoreLocomotionWeight(); RestoreChargeOffsets();
                Play(Idle, loop: true, force: true);
                _weight = 1; _mixer.SetInputWeight(0, 0); _mixer.SetInputWeight(1, 1);
                RetireOutgoing(); _gaitWeight = 0; _layers.SetInputWeight(1, 0);
                _graph.Evaluate(0);
            }
            _arrivalSlot = Mathf.Clamp(slot, 0, 3);
            _arrivalWeight = Mathf.Clamp01(weight);
            if (_arrivalWeight == 0) RestoreArrivalPose();
        }

        private void RestoreArrivalPose()
        {
            if (!_arrivalApplied || _arrivalBones == null) return;
            for (int i = 0; i < _arrivalBones.Length; i++)
                if (_arrivalBones[i] != null) _arrivalBones[i].localRotation = _arrivalUnder[i];
            _arrivalApplied = false;
        }

        private void ApplyArrivalPose()
        {
            if (_arrivalWeight <= 0 || _arrivalBones == null) return;
            for (int i = 0; i < _arrivalBones.Length; i++)
            {
                var bone = _arrivalBones[i]; if (bone == null) continue;
                _arrivalUnder[i] = bone.localRotation;
                Vector3 offset;
                if (i == 0) offset = new Vector3(0, (_arrivalSlot - 1.5f) * 7, 0);
                else if (i == 1) offset = new Vector3(-4, (1.5f - _arrivalSlot) * 8, 0);
                else if (_arrivalSlot == 0) offset = i == 2 ? new Vector3(-15, 0, -12) : new Vector3(-125, 0, 25);
                else if (_arrivalSlot == 1) offset = i == 2 ? new Vector3(-55, 25, -22) : new Vector3(-55, -25, 22);
                else if (_arrivalSlot == 2) offset = i == 2 ? new Vector3(-25, 0, -40) : new Vector3(-10, 0, 12);
                else offset = i == 2 ? new Vector3(-110, 0, -20) : new Vector3(-15, 0, 20);
                bone.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(offset), _arrivalWeight);
            }
            _arrivalApplied = true;
        }
    }
}
