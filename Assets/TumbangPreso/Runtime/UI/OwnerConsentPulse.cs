using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>A refusal pulse on terms ink, with unchanged checkbox/link hitboxes.</summary>
    public sealed class OwnerConsentPulse : MonoBehaviour
    {
        private Graphic[] _graphics;
        private Color[] _rest;
        private float _started;
        public bool IsPulsing { get; private set; }
        public void Refuse()
        {
            if (!IsPulsing)
            {
                _graphics = GetComponentsInChildren<Graphic>(true); _rest = new Color[_graphics.Length];
                for (int i = 0; i < _graphics.Length; i++) _rest[i] = _graphics[i].color;
            }
            _started = Time.unscaledTime; IsPulsing = true; enabled = true; Update();
        }
        private void Update()
        {
            if (!IsPulsing) { enabled = false; return; }
            float age = (Time.unscaledTime - _started) / .9f;
            if (age >= 1) { Clear(); return; }
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            float amount = reduced ? .55f : .25f + .4f * Mathf.Pow(Mathf.Cos(age * Mathf.PI * 2), 2);
            for (int i = 0; i < _graphics.Length; i++)
            {
                if (_graphics[i] == null || _rest[i].a == 0) continue;
                var ink = Color.Lerp(_rest[i], OwnerUiTheme.Current.HintInk, amount);
                ink.a = _rest[i].a * (reduced ? 1 : 1 - amount * .4f); _graphics[i].color = ink;
            }
        }
        public void Clear()
        {
            if (IsPulsing)
                for (int i = 0; i < _graphics.Length; i++) if (_graphics[i] != null) _graphics[i].color = _rest[i];
            IsPulsing = false; enabled = false;
        }
        private void OnDisable() => Clear();
    }
}
