using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// "This thing is meant to be in the air, and here is why."
    ///
    /// ⚠️⚠️ THE EXEMPTION LIVES IN THE SCENE, NOT IN THE CHECKER'S SOURCE. `MapGeometryCheck`
    /// flags every renderer that hangs over nothing, which is the whole point of it, and a
    /// railway viaduct eight metres up is a legitimate hit. The first instinct is a name list
    /// inside the checker. That list then rots the moment a prop is renamed, and worse, it is
    /// invisible to anyone reading the scene: the object looks unchecked rather than
    /// deliberately excused.
    ///
    /// ⚠️ THE REASON IS REQUIRED AND IS PRINTED IN THE REPORT. An exemption with no reason is
    /// how a genuine floating prop gets silenced by whoever was tired of seeing it in the
    /// output. Every excused object appears in `Logs/map-geometry-check.txt` with this string
    /// next to it, so the excuses are read every time the check runs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AirborneByDesign : MonoBehaviour
    {
        [Tooltip("Why this object is allowed to hang in the air. Printed by MapGeometryCheck.")]
        public string Reason = "";

        /// <summary>Tag an object and its children as deliberately airborne.</summary>
        public static AirborneByDesign Attach(GameObject go, string reason)
        {
            var mark = go.GetComponent<AirborneByDesign>();
            if (mark == null) mark = go.AddComponent<AirborneByDesign>();

            mark.Reason = reason;
            return mark;
        }
    }
}
