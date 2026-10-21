using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    // Tint only: the painted field and its pointer/caret geometry stay in place.
    [RequireComponent(typeof(Image))]
    public sealed class OwnerFieldPulse : MonoBehaviour
    {
        public const float Seconds = .72f;
        private Image _image;
        private Color _rest;
        private float _began;
        public bool IsPulsing { get; private set; }

        private void Awake() { _image = GetComponent<Image>(); enabled = false; }

        public void Refuse()
        {
            if (!IsPulsing) _rest = _image.color;
            _began = Time.unscaledTime;
            IsPulsing = true;
            enabled = true;
            Update();
        }

        private void Update()
        {
            float age = (Time.unscaledTime - _began) / Seconds;
            if (age >= 1) { Clear(); return; }
            float wave = Settings.SettingsStore.Current.ReducedUiMotion
                ? 1 : Mathf.Pow(Mathf.Sin(age * Mathf.PI * 2), 2);
            var tint = Color.Lerp(_rest, OwnerUiTheme.Current.HintInk, .32f * wave);
            tint.a = _rest.a;
            _image.color = tint;
        }

        public void Clear()
        {
            if (IsPulsing && _image != null) _image.color = _rest;
            IsPulsing = false;
            enabled = false;
        }

        private void OnDisable() => Clear();
    }
}
