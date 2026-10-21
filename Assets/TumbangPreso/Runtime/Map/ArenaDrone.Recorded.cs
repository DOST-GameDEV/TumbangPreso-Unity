using UnityEngine;

namespace TumbangPreso.Map
{
    public sealed partial class ArenaDrone
    {
        // Rendering identity only; the landing mark is outside the body hierarchy.
        public Transform RecordedLandingMark=>_spot;
    }
}
