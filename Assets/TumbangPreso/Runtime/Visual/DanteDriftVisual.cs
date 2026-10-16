using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>Render-only sequencing of Dante's authored seismic fracture vocabulary.</summary>
    public sealed class DanteDriftVisual : MonoBehaviour, IVfxTimeline
    {
        readonly DanteDriftFault[] _bands = new DanteDriftFault[GeoRules.DriftBlasts];
        // The live cascade around the fault ink (fuse, heaves, dust edges, map-wide quake, the going under): `DanteDriftLive`.
        DanteDriftLive _live;
        public DanteDriftLive Live => _live;
        public float LifeSeconds => GeoRules.DriftSeconds;
        public static DanteDriftVisual Build(Transform parent, Vector3 origin, Vector3 forward, float halfWidth, float reach)
        {
            var root = new GameObject("ContinentalDriftFractures"); root.transform.SetParent(parent, false);
            var view = root.AddComponent<DanteDriftVisual>();
            float depth = reach / GeoRules.DriftBlasts;
            for (int i = 0; i < view._bands.Length; i++)
            {
                var fx = DanteDriftFault.Build(root.transform, origin + forward * (i * depth),
                    forward, halfWidth, depth, i);
                fx.gameObject.SetActive(false); view._bands[i] = fx;
            }
            // A sibling of the fault patches, so the patch layout stays exactly five children.
            view._live = DanteDriftLive.Build(parent, origin, forward, halfWidth, reach);
            view.StepTo(0); return view;
        }
        public void StepTo(float seconds)
        {
            if (_live != null) _live.StepTo(seconds);
            for (int i = 0; i < _bands.Length; i++)
            {
                float age = seconds - i * GeoRules.DriftInterval;
                bool visible = age >= 0 && age < GeoRules.DriftVisualTail;
                _bands[i].gameObject.SetActive(visible);
                if (visible) _bands[i].StepTo(age);
            }
        }
    }
}
