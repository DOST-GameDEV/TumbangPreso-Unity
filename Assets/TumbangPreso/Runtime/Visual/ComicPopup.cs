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
        private const float Lifetime = 1.35f;
        private const float FloatSpeed = 1.25f;
        private const float BaseFontSize = 64;

        private TextMesh _textMesh;
        private readonly List<TextMesh> _outlineMeshes = new List<TextMesh>(8);
        private Transform _cameraTransform;
        private float _elapsed;
        private Vector3 _startScale;
        private Color _mainColor;
        private float _tiltAngle;

        /// <summary>
        /// ⚠️⚠️ THE CALLOUTS ARE CAPPED AND STACKED, BECAUSE A HERO EXCHANGE FIRES FOUR AT ONCE.
        /// Every ability, every tag and every knockdown spawns one, and they all spawn at head
        /// height on whoever they happened to.
        /// </summary>
        private const int MaxLive = 4;

        private static readonly List<ComicPopup> Live = new List<ComicPopup>(8);

        public static void Spawn(Vector3 worldPos, string text, Color color, float scale = 1.0f)
        {
            Live.RemoveAll(p => p == null);

            while (Live.Count >= MaxLive)
            {
                var oldest = Live[0];
                Live.RemoveAt(0);
                if (oldest != null) Destroy(oldest.gameObject);
            }

            var go = new GameObject($"ComicPopup_{text}");

            float lift = 1.4f + Live.Count * 0.48f;
            go.transform.position = worldPos + Vector3.up * lift + Random.insideUnitSphere * 0.10f;

            var popup = go.AddComponent<ComicPopup>();
            popup.Init(text, color, scale);
            Live.Add(popup);
        }

        private void OnDestroy() => Live.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Live.Clear();

        public static void Bonk(Vector3 pos) => Spawn(pos, "BONK!", UiTheme.HeroEarthBright, 1.25f);
        public static void Zap(Vector3 pos) => Spawn(pos, "ZAP!", UiTheme.HeroElectricBright, 1.3f);
        public static void Freeze(Vector3 pos) => Spawn(pos, "FREEZE!", UiTheme.HeroIceBright, 1.3f);
        public static void Kaboom(Vector3 pos) => Spawn(pos, "KABOOM!", UiTheme.HeroFireBright, 1.5f);
        public static void Clang(Vector3 pos) => Spawn(pos, "CLANG!", UiTheme.Highlight, 1.2f);
        public static void Whoa(Vector3 pos) => Spawn(pos, "WHOA!", UiTheme.HeroIce, 1.15f);
        public static void Boo(Vector3 pos) => Spawn(pos, "BOO!", UiTheme.HeroSpiritBright, 1.25f);
        public static void Super(Vector3 pos) => Spawn(pos, "SUPER!", UiTheme.Highlight, 1.6f);
        public static void Bam(Vector3 pos) => Spawn(pos, "BAM!", UiTheme.HeroFire, 1.3f);
        public static void Wheee(Vector3 pos) => Spawn(pos, "WHEEE!", UiTheme.HeroElectricBright, 1.2f);

        private void Init(string text, Color color, float scaleMultiplier)
        {
            _mainColor = color;
            _startScale = Vector3.one * (0.048f * scaleMultiplier);
            _tiltAngle = Random.Range(-7.0f, 7.0f);
            transform.localScale = Vector3.zero;

            var font = MenuKit.Font;
            var fontMaterial = font != null ? font.material : null;

            // Multi-direction ink outline for dramatic comic book contrast
            Vector2[] outlineOffsets =
            {
                new Vector2(0.045f, 0.0f),
                new Vector2(-0.045f, 0.0f),
                new Vector2(0.0f, 0.045f),
                new Vector2(0.0f, -0.045f),
                new Vector2(0.055f, -0.055f) // Drop shadow corner
            };

            for (int i = 0; i < outlineOffsets.Length; i++)
            {
                var outlineGo = new GameObject($"Outline_{i}");
                outlineGo.transform.SetParent(transform, false);
                outlineGo.transform.localPosition = new Vector3(outlineOffsets[i].x, outlineOffsets[i].y, 0.015f);

                var tm = outlineGo.AddComponent<TextMesh>();
                if (font != null) tm.font = font;
                if (fontMaterial != null)
                {
                    var mr = outlineGo.GetComponent<MeshRenderer>();
                    if (mr != null) mr.sharedMaterial = fontMaterial;
                }
                tm.text = text;
                tm.fontSize = (int)BaseFontSize;
                tm.fontStyle = FontStyle.Bold;
                tm.alignment = TextAlignment.Center;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.color = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 0.96f);
                _outlineMeshes.Add(tm);
            }

            // Main Text Mesh (Front in vibrant comic hero palette)
            var textGo = new GameObject("FrontText");
            textGo.transform.SetParent(transform, false);
            textGo.transform.localPosition = Vector3.zero;

            _textMesh = textGo.AddComponent<TextMesh>();
            if (font != null) _textMesh.font = font;
            if (fontMaterial != null)
            {
                var mr = textGo.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = fontMaterial;
            }
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

            // Billboard towards active camera with slight comic tilt
            if (_cameraTransform == null && UnityEngine.Camera.main != null)
                _cameraTransform = UnityEngine.Camera.main.transform;

            if (_cameraTransform != null)
            {
                transform.rotation = _cameraTransform.rotation * Quaternion.Euler(0, 0, _tiltAngle);
            }

            // Float upward with decelerating arcade drift
            float driftRate = FloatSpeed * (1.0f - t * 0.45f);
            transform.position += Vector3.up * (driftRate * Time.deltaTime);

            // Punchy cartoon pop-in scale curve: dramatic explosive overshoot -> settle
            float scaleProgress;
            if (t < 0.14f)
            {
                float popT = t / 0.14f;
                scaleProgress = Mathf.Lerp(0.0f, 1.48f, Mathf.Sin(popT * Mathf.PI * 0.5f));
            }
            else if (t < 0.28f)
            {
                float settleT = (t - 0.14f) / 0.14f;
                scaleProgress = Mathf.Lerp(1.48f, 1.0f, settleT);
            }
            else
            {
                scaleProgress = 1.0f + Mathf.Sin((t - 0.28f) * 3.0f) * 0.04f;
            }

            transform.localScale = _startScale * scaleProgress;

            // Dramatic smooth fade out in final 35% of lifetime
            if (t > 0.65f)
            {
                float fade = 1.0f - ((t - 0.65f) / 0.35f);
                if (_textMesh != null)
                {
                    var c = _mainColor;
                    c.a = fade;
                    _textMesh.color = c;
                }
                for (int i = 0; i < _outlineMeshes.Count; i++)
                {
                    if (_outlineMeshes[i] != null)
                    {
                        var sc = UiTheme.Ink;
                        sc.a = fade * 0.96f;
                        _outlineMeshes[i].color = sc;
                    }
                }
            }
        }
    }
}
