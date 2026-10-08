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
        private bool _arrivalWalking;
        private GameObject _arrivalModel;
        private Transform _arrivalLegL, _arrivalLegR, _arrivalGroundRoot;
        private TagSoleVertex[] _arrivalSoleL, _arrivalSoleR;
        private float _arrivalFloor;
        private int _arrivalGesture;

        // A presentation-only greeting. No gameplay action, emote RPC or physical root movement.
        public void SetArrivalPose(int slot, float weight)
        {
            var model = GetComponent<CharacterVisual>()?.Model;
            if (_arrivalBones == null || _arrivalModel != model)
            {
                if (model == null) return;
                RestoreArrivalPose(); ClearArrivalGround();
                _arrivalModel = model;
                _arrivalLegL = _arrivalLegR = null;
                _arrivalBones = new Transform[4];
                foreach (var bone in model.GetComponentsInChildren<Transform>(true))
                {
                    int index = bone.name == "torso" ? 0 : bone.name == "head" ? 1 :
                        bone.name == "arm-left" ? 2 : bone.name == "arm-right" ? 3 : -1;
                    if (index >= 0) _arrivalBones[index] = bone;
                    if (bone.name == "leg-left") _arrivalLegL = bone;
                    if (bone.name == "leg-right") _arrivalLegR = bone;
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
            if (_arrivalWeight == 0) { RestoreArrivalPose(); _arrivalWalking = false; }
        }

        public void SetArrivalGesture(int gesture) => _arrivalGesture = Mathf.Clamp(gesture, 0, 5);
        public void SetArrivalGround(Transform root, float floor)
        {
            if (_arrivalWeight <= 0 || root == null || !float.IsFinite(floor)) return;
            if (_arrivalGroundRoot != root)
            {
                _arrivalSoleL = TagSolePoints(_arrivalLegL);
                _arrivalSoleR = TagSolePoints(_arrivalLegR);
            }
            _arrivalGroundRoot = root; _arrivalFloor = floor;
        }
        public void ClearArrivalGround()
        { _arrivalGroundRoot = null; _arrivalSoleL = _arrivalSoleR = null; }
        public void RefreshArrivalGround() => GroundArrival();
        private void GroundArrival()
        {
            if (_arrivalGroundRoot == null) return;
            float lowest = float.PositiveInfinity;
            if (_arrivalSoleL != null) foreach (var point in _arrivalSoleL)
                if (point.Valid) lowest = Mathf.Min(lowest, point.Height);
            if (_arrivalSoleR != null) foreach (var point in _arrivalSoleR)
                if (point.Valid) lowest = Mathf.Min(lowest, point.Height);
            if (float.IsFinite(lowest)) _arrivalGroundRoot.position += Vector3.up * (_arrivalFloor + .005f - lowest);
        }

        /// <summary>
        /// The held body walks, or stands again: the character's OWN walk clip in place of its idle, for a film
        /// that moves the body itself (the Arena's opening walks the cast out of a tunnel). Run it on with
        /// `AdvanceHeld`. Nothing outside an arrival pose.
        /// </summary>
        public void SetArrivalGait(bool walking)
        {
            if (_arrivalWeight <= 0 || !_graph.IsValid() || walking == _arrivalWalking) return;
            _arrivalWalking = walking;
            RestoreArrivalPose();
            // ⚠️ A CROSSFADE, NOT A CUT (owner, 2026-10-06: "immediately goes A-pose before starting the idle
            // animation. it should flow instead of snapping"). `Play` sets the blend up (the clip being left on
            // input 0, the new one on input 1, weight 0); the weight is normally run on by the game's clock, which
            // is held here, so this used to jump it straight to the new clip's first frame. `AdvanceHeld` runs it.
            Play(walking ? Walk : Idle, loop: true, force: true);
            _gaitWeight = 0; _layers.SetInputWeight(1, 0);
            _graph.Evaluate(0);
        }

        /// <summary>
        /// Run the held idle on by `seconds`. The arrival holds the game's clock, and the graph with it: a
        /// film that keeps a body on screen for many seconds (the Arena's opening) calls this each frame so
        /// the body breathes. Nothing outside an arrival pose (`SetArrivalPose` above zero).
        /// </summary>
        public void AdvanceHeld(float seconds)
        {
            if (_arrivalWeight <= 0 || !_graph.IsValid()) return;
            RestoreArrivalPose();
            seconds = Mathf.Clamp(seconds, 0f, 0.1f);
            // The crossfade a change of gait began, over 0.4 s of this film's own time.
            if (_weight < 1f && _mixer.IsValid())
            {
                _weight = Mathf.Min(1f, _weight + seconds / 0.4f);
                float eased = _weight * _weight * (3f - 2f * _weight);
                _mixer.SetInputWeight(0, 1f - eased); _mixer.SetInputWeight(1, eased);
                if (_weight >= 1f) RetireOutgoing();
            }
            _graph.Evaluate(seconds);
        }

        // The new-map visual root follows an eased entrance path. Match the
        // walk's calibrated stride to that travel while its crossfade uses time.
        public void AdvanceHeldTravel(float seconds, float planarMetres)
        {
            if(_arrivalWalking&&_current==Walk&&_graph.IsValid())
            {
                var front=Front();
                if(front.IsValid()&&float.IsFinite(planarMetres))
                {
                    front.SetSpeed(0);
                    front.SetTime(front.GetTime()+Mathf.Max(0,planarMetres)/Mathf.Max(.05f,_walkReference));
                }
            }
            AdvanceHeld(seconds);
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
                if (_arrivalGesture == 1) // Plaza: loosen the shoulders before the game.
                    offset = i == 0 ? new Vector3(0, (_arrivalSlot - 1.5f) * 10, 0) : i == 1 ? new Vector3(-6, 0, 0)
                        : new Vector3(-55, i == 2 ? 18 : -18, i == 2 ? -35 : 35);
                else if (_arrivalGesture == 2) // Under the bridge: a quick nod and compact salute.
                    offset = i == 0 ? new Vector3(4, 0, 0) : i == 1 ? new Vector3(8, (_arrivalSlot - 1.5f) * 4, 0)
                        : i == 2 ? new Vector3(-15, 0, -10) : new Vector3(-70, 0, 15);
                else if (_arrivalGesture == 3) // Roof: glance out at the skyline, then acknowledge the camera.
                    offset = i == 0 ? new Vector3(0, 12, 0) : i == 1 ? new Vector3(-5, 24, 0)
                        : i == 2 ? new Vector3(-25, 0, -18) : new Vector3(-90, 10, 20);
                else if (_arrivalGesture == 4) // Cove: an open, relaxed wave.
                    offset = i == 0 ? new Vector3(0, -6, 2) : i == 1 ? new Vector3(-3, -8, -3)
                        : i == 2 ? new Vector3(-18, 0, -15) : new Vector3(-120, -10, 35);
                else if (_arrivalGesture == 5) // Crossing: a brisk ready stance with one raised hand.
                    offset = i == 0 ? new Vector3(3, (_arrivalSlot % 2 == 0 ? -1 : 1) * 12, 0)
                        : i == 1 ? new Vector3(2, 0, 0) : i == 2 ? new Vector3(-50, 15, -20) : new Vector3(-85, -15, 20);
                bone.localRotation *= Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(offset), _arrivalWeight);
            }
            _arrivalApplied = true;
            GroundArrival();
        }
    }
}
