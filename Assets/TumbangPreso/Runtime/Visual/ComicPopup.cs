using System.Collections.Generic;
using TumbangPreso.UI;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Spawns high-energy, billboarded comic-book style pop-up combat text ("BONK!", "ZAP!", "KABOOM!", etc.)
    /// with bouncy scaling, pop-art outlines, and upward drift for kid-friendly arcade juice.
    /// </summary>
    public sealed class ComicPopup : MonoBehaviour
    {
        private const float Lifetime = 0.85f;
        private const float FloatSpeed = 1.4f;
        private const float BaseFontSize = 72;

        private TextMesh _textMesh;
        private TextMesh _shadowMesh;
        private Transform _cameraTransform;
        private float _elapsed;
        private Vector3 _startScale;
        private Color _mainColor;

        public static void Spawn(Vector3 worldPos, string text, Color color, float scale = 1.0f)
        {
            var go = new GameObject($"ComicPopup_{text}");
            go.transform.position = worldPos + Vector3.up * 1.3f + Random.insideUnitSphere * 0.2f;

            var popup = go.AddComponent<ComicPopup>();
            popup.Init(text, color, scale);
        }

        public static void Bonk(Vector3 pos) => Spawn(pos, "BONK!", UiTheme.HeroEarthBright, 1.2f);
        public static void Zap(Vector3 pos) => Spawn(pos, "ZAP!", UiTheme.HeroElectric, 1.25f);
        public static void Freeze(Vector3 pos) => Spawn(pos, "FREEZE!", UiTheme.HeroIceBright, 1.25f);
        public static void Kaboom(Vector3 pos) => Spawn(pos, "KABOOM!", UiTheme.HeroFireBright, 1.4f);
        public static void Clang(Vector3 pos) => Spawn(pos, "CLANG!", UiTheme.Highlight, 1.15f);
        public static void Whoa(Vector3 pos) => Spawn(pos, "WHOA!", UiTheme.HeroIce, 1.1f);
        public static void Boo(Vector3 pos) => Spawn(pos, "BOO!", UiTheme.HeroSpiritBright, 1.2f);
        public static void Super(Vector3 pos) => Spawn(pos, "SUPER!", UiTheme.Highlight, 1.5f);
        public static void Bam(Vector3 pos) => Spawn(pos, "BAM!", UiTheme.HeroFire, 1.25f);
        public static void Wheee(Vector3 pos) => Spawn(pos, "WHEEE!", UiTheme.HeroElectricBright, 1.15f);

        private void Init(string text, Color color, float scaleMultiplier)
        {
            _mainColor = color;
            _startScale = Vector3.one * (0.045f * scaleMultiplier);
            transform.localScale = Vector3.zero;

            // Shadow / Outline Mesh (Behind)
            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(transform, false);
            shadowGo.transform.localPosition = new Vector3(0.04f, -0.04f, 0.02f);
            _shadowMesh = shadowGo.AddComponent<TextMesh>();
            _shadowMesh.text = text;
            _shadowMesh.fontSize = (int)BaseFontSize;
            _shadowMesh.fontStyle = FontStyle.Bold;
            _shadowMesh.alignment = TextAlignment.Center;
            _shadowMesh.anchor = TextAnchor.MiddleCenter;
            _shadowMesh.color = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.95f);

            // Main Text Mesh (Front)
            var textGo = new GameObject("FrontText");
            textGo.transform.SetParent(transform, false);
            textGo.transform.localPosition = Vector3.zero;
            _textMesh = textGo.AddComponent<TextMesh>();
            _textMesh.text = text;
            _textMesh.fontSize = (int)BaseFontSize;
            _textMesh.fontStyle = FontStyle.Bold;
            _textMesh.alignment = TextAlignment.Center;
            _textMesh.anchor = TextAnchor.MiddleCenter;
            _textMesh.color = color;

            if (UnityEngine.Camera.main != null)
                _cameraTransform = UnityEngine.Camera.main.transform;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = _elapsed / Lifetime;

            if (t >= 1.0f)
            {
                Destroy(gameObject);
                return;
            }

            // Billboard towards active camera
            if (_cameraTransform == null && UnityEngine.Camera.main != null)
                _cameraTransform = UnityEngine.Camera.main.transform;

            if (_cameraTransform != null)
            {
                transform.rotation = _cameraTransform.rotation;
            }

            // Float upward gently
            transform.position += Vector3.up * (FloatSpeed * Time.deltaTime * (1.0f - t * 0.5f));

            // Bouncy cartoon pop-in scale curve (Overshoot & Settle)
            float scaleProgress;
            if (t < 0.2f)
            {
                float popT = t / 0.2f;
                scaleProgress = Mathf.Lerp(0.0f, 1.35f, Mathf.Sin(popT * Mathf.PI * 0.5f));
            }
            else if (t < 0.35f)
            {
                float settleT = (t - 0.2f) / 0.15f;
                scaleProgress = Mathf.Lerp(1.35f, 1.0f, settleT);
            }
            else
            {
                scaleProgress = 1.0f;
            }

            transform.localScale = _startScale * scaleProgress;

            // Fade out in the last 35% of lifetime
            if (t > 0.65f)
            {
                float fade = 1.0f - ((t - 0.65f) / 0.35f);
                if (_textMesh != null)
                {
                    var c = _mainColor;
                    c.a = fade;
                    _textMesh.color = c;
                }
                if (_shadowMesh != null)
                {
                    var sc = UiTheme.Ink;
                    sc.a = fade * 0.95f;
                    _shadowMesh.color = sc;
                }
            }
        }
    }
}
