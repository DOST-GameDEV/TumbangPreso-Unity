using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>Render-only sequencing of Dante's authored seismic fracture vocabulary.</summary>
    public sealed class DanteDriftVisual : MonoBehaviour, IVfxTimeline
    {
        readonly DanteSeismicVisual[] _bands = new DanteSeismicVisual[GeoRules.DriftBlasts];
        public float LifeSeconds => GeoRules.DriftSeconds;
        public static DanteDriftVisual Build(Transform parent, Vector3 origin, Vector3 forward, float halfWidth, float reach)
        {
            var root = new GameObject("ContinentalDriftFractures"); root.transform.SetParent(parent, false);
            var view = root.AddComponent<DanteDriftVisual>();
            float depth = reach / GeoRules.DriftBlasts;
            for (int i = 0; i < view._bands.Length; i++)
            {
                var fx = DanteSeismicVisual.Recorded(origin + forward * (i * depth),
                    Vector3.forward, 6, true, false, false, GeoRules.DriftVisualTail);
                fx.transform.SetParent(root.transform, true);
                fx.transform.rotation = Quaternion.LookRotation(forward);
                fx.transform.localScale = new Vector3(halfWidth / 6, 1, depth / 6.6f);
                fx.gameObject.SetActive(false); view._bands[i] = fx;
            }
            view.StepTo(0); return view;
        }
        public void StepTo(float seconds)
        {
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
