using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    public sealed partial class ViewmodelArms
    {
        /// <summary>
        /// ⚠️ SET WHILE A PROP IS HELD IN THE LEFT HAND (Phaister's manika). The left hand rests BELOW the frame, so a prop in
        /// it would be invisible on her own screen; while this is set the hand rises into the lower left of the view and
        /// stays there, easing in and out over a quarter second.
        /// </summary>
        public bool HoldingProp { get; set; }

        /// <summary>
        /// The body these arms belong to. ⚠️ THE ONE HONEST ANSWER TO "IS THIS HER OWN SCREEN": `NetAuthority.LocalSlot` is 0
        /// under the solo provider while the solo seat is 1 (`GameLaunch.SoloSeat`), so a first-person prop keyed on the slot
        /// went to nobody (HERO-10 film v2). The arms are bound to exactly the body the camera is.
        /// </summary>
        public CharacterMotor BoundCharacter => _characterMotor;

        /// <summary>True when <paramref name="who"/> is the body a first-person view on this peer belongs to.</summary>
        public static bool IsFirstPersonFor(CharacterMotor who)
        {
            if (who == null) return false;
            foreach (var arms in Object.FindObjectsByType<ViewmodelArms>())
                if (arms != null && arms.isActiveAndEnabled && arms._characterMotor == who) return true;
            return false;
        }

        private float _heldPropLift;
        private Vector3 _heldPropOffset;
        private bool _heldPropApplied;

        private void RestoreHeldProp()
        {
            if (!_heldPropApplied) return;
            if (_leftPivot != null) _leftPivot.localPosition -= _heldPropOffset;
            _heldPropApplied = false;
        }

        private void ApplyHeldProp()
        {
            _heldPropLift = Mathf.MoveTowards(_heldPropLift, HoldingProp ? 1f : 0f, Time.deltaTime * 4f);
            if (_heldPropLift <= 0f || _leftPivot == null) return;
            float e = _heldPropLift * _heldPropLift * (3f - 2f * _heldPropLift);
            _heldPropOffset = new Vector3(.10f, .30f, .08f) * e;
            _leftPivot.localPosition += _heldPropOffset;
            _heldPropApplied = true;
        }

        /// <summary>
        /// ⚠️ THE FIRST-PERSON LEFT ARM, FOR A PROP HELD IN IT (HERO-10: Phaister's manika, owner: *"show it FPP and TPP okay?
        /// i want ppl to see and hher to see that shees using it"*). A prop parented here rides every first-person gesture.
        /// Null until the arms are built.
        /// </summary>
        public Transform LeftHandForProps() => _leftArm;

        /// <summary>
        /// Where the palm is in <see cref="LeftHandForProps"/>'s space: the far end of the arm mesh along its longest axis,
        /// the end farther from the pivot. MEASURED from the mesh rather than typed, the lesson `CharacterVisual.PalmCentre`
        /// records (eight guessed hand offsets, eight wrong places).
        /// </summary>
        public Vector3 LeftPalmOffset()
        {
            if (_leftArm == null || _leftArmRenderer == null) return Vector3.zero;
            var filter = _leftArmRenderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) return Vector3.zero;
            var b = filter.sharedMesh.bounds;
            Vector3 size = b.size;
            int axis = size.x > size.y ? (size.x > size.z ? 0 : 2) : (size.y > size.z ? 1 : 2);
            Vector3 low = b.center, high = b.center;
            low[axis] = b.min[axis] + size[axis] * 0.08f;
            high[axis] = b.max[axis] - size[axis] * 0.08f;
            Vector3 a = _leftArm.InverseTransformPoint(_leftArmRenderer.transform.TransformPoint(low));
            Vector3 c = _leftArm.InverseTransformPoint(_leftArmRenderer.transform.TransformPoint(high));
            return a.sqrMagnitude > c.sqrMagnitude ? a : c;
        }
    }
}
