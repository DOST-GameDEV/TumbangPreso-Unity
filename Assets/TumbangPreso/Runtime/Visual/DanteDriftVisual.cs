using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>Render-only sequencing of Dante's authored seismic fracture vocabulary.</summary>
    public sealed class DanteDriftVisual : MonoBehaviour, IVfxTimeline
    {
        readonly DanteDriftFault[] _bands = new DanteDriftFault[GeoRules.DriftBlasts];
        // The longhorn's fist coming down on each band (`DanteDriftFist`). It stands BESIDE this object under the same
        // parent, not under it: this object's children are the five bands and nothing else.
        DanteDriftFist _fist;
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
            view._fist = DanteDriftFist.Build(parent, origin, forward, halfWidth, reach);
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
            if (_fist != null) _fist.StepTo(seconds);
        }
        void OnDestroy()
        {
            // The fists live beside the marks, so they are not taken with them unless the whole wave goes: take them here.
            if (_fist == null) return;
            _fist.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(_fist.gameObject);
        }
    }
}
