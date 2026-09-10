using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>A brief source-to-hand recall trace. It never searches for victims or owns a collider.</summary>
    public sealed class MagnetRecallTrace : MonoBehaviour, IVfxTimeline
    {
        public const float LifeSeconds = 0.22f;
        float IVfxTimeline.LifeSeconds => LifeSeconds;
        private Vector3 _from, _fallback;
        private Transform _hand;
        private LineRenderer _line;
        private float _elapsed;

        public static GameObject Spawn(Vector3 from, Transform hand, Vector3 fallback)
        {
            var go = new GameObject("MagnetRecallTrace");
            var trace = go.AddComponent<MagnetRecallTrace>();
            trace._from = from;
            trace._hand = hand;
            trace._fallback = fallback;
            var line = go.AddComponent<LineRenderer>();
            trace._line = line;
            line.useWorldSpace = true;
            line.positionCount = 9;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            VfxMaterial.Ghost(line, UI.UiTheme.HeroElectricBright, 1.0f);
            trace.StepTo(0);
            Destroy(go, LifeSeconds);
            return go;
        }

        private void LateUpdate() => StepTo(_elapsed + Time.deltaTime);

        public void StepTo(float seconds)
        {
            _elapsed = seconds;
            if (_line == null) return;
            float t = Mathf.Clamp01(seconds / LifeSeconds);
            Vector3 end = _hand != null ? _hand.position : _fallback;
            Vector3 start = Vector3.Lerp(_from, end, 1 - Mathf.Pow(1 - t, 3));
            Vector3 along = end - start;
            Vector3 side = Vector3.Cross(along, Vector3.up).normalized;
            if (side.sqrMagnitude < .01f) side = Vector3.right;
            for (int i = 0; i < _line.positionCount; i++)
            {
                float u = i / (float)(_line.positionCount - 1);
                float bend = Mathf.Sin(i * 2.7f + seconds * 90) * Mathf.Sin(u * Mathf.PI) * .055f * (1 - t);
                _line.SetPosition(i, i == 0 ? start : i == _line.positionCount - 1 ? end
                    : Vector3.Lerp(start, end, u) + side * bend);
            }
            _line.startWidth = Mathf.Lerp(.05f, .008f, t);
            _line.endWidth = .012f * (1 - t);
            if (_line.sharedMaterial != null)
            {
                var color = UI.UiTheme.HeroElectricBright;
                color.a = .82f * (1 - t);
                _line.sharedMaterial.color = color;
                _line.sharedMaterial.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * (1 - t));
            }
        }
    }
}
