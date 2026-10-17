using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Render-only CONTINENTAL DRIFT in play: the cutscene's own cascade (`DanteDriftLive`), continued from the cutscene's last
    /// frame. The per-band fault ink that erupted again after the cut is retired (owner, 2026-10-17: *"hindi na dapat uulit"*).
    /// </summary>
    public sealed class DanteDriftVisual : MonoBehaviour, IVfxTimeline
    {
        DanteDriftLive _live;
        public DanteDriftLive Live => _live;
        public float LifeSeconds => GeoRules.DriftSeconds;
        public static DanteDriftVisual Build(Transform parent, Vector3 origin, Vector3 forward, float halfWidth, float reach)
        {
            var root = new GameObject("ContinentalDriftFractures"); root.transform.SetParent(parent, false);
            var view = root.AddComponent<DanteDriftVisual>();
            view._live = DanteDriftLive.Build(root.transform, origin, forward, halfWidth, reach);
            view.StepTo(0); return view;
        }
        public void StepTo(float seconds)
        {
            if (_live != null) _live.StepTo(seconds);
        }
    }
}
