using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The hit, drawn on the replay's picture and under its camcorder frame (owner, October 31: "make replay and tag replay
    /// look wayy better"). A comic star on the point of contact, ink lines rushing in at it, and a dark edge that closes as
    /// the picture slows, so the eye is already on the touch when it lands. It decides nothing and runs on real time.
    /// Reduced effects or reduced motion leave only the picture.
    /// </summary>
    public sealed class ReplayImpact : MonoBehaviour
    {
        private const float StarSeconds = .3f, LinesSeconds = .42f;
        private Vignette _edge;
        private Burst _star, _lines;
        private float _firedAt = -100, _closing;
        public bool Fired => _firedAt > -100;
        /// <summary>Where the last hit was drawn, 0 to 1 across the picture.</summary>
        public Vector2 FiredAt { get; private set; }

        public static ReplayImpact Build(Transform parent)
        {
            var root = OwnerUiLayout.Rect(parent, "ReplayImpact"); OwnerUiLayout.Fill(root);
            var impact = root.gameObject.AddComponent<ReplayImpact>();
            impact._edge = OwnerUiLayout.Rect(root, "ReplayEdge").gameObject.AddComponent<Vignette>();
            OwnerUiLayout.Fill(impact._edge.rectTransform); impact._edge.raycastTarget = false; impact._edge.color = new Color(.07f, .03f, .02f, 0);
            impact._lines = Part(root, "ReplayHitLines", 3400, new Color(1, .97f, .88f, 1), 54, 900, 1700, 0);
            impact._star = Part(root, "ReplayHitStar", 520, HudDraw.Honey, 13, 92, 260, .42f);
            return impact;
        }
        private static Burst Part(RectTransform root, string name, float size, Color colour, int spikes, float inner, float outer, float core)
        {
            var part = OwnerUiLayout.Rect(root, name).gameObject.AddComponent<Burst>();
            part.raycastTarget = false; part.color = colour; part.Spikes = spikes; part.Inner = inner; part.Outer = outer; part.Core = core;
            var rect = part.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = Vector2.one * size;
            part.enabled = false;
            return part;
        }

        /// <summary>How far the dark edge has closed, 0 to 1: as the picture slows and through the held frame.</summary>
        public float Closing { set { _closing = Mathf.Clamp01(value); } }

        /// <summary>The hit lands at this point of the picture, 0 to 1 across it. A point off the picture lands at its nearest edge.</summary>
        public void Fire(Vector2 at)
        {
            FiredAt = new Vector2(Mathf.Clamp(at.x, .08f, .92f), Mathf.Clamp(at.y, .1f, .9f));
            _firedAt = Time.unscaledTime;
            var look = Settings.SettingsStore.Current;
            if (look.ReducedEffects || look.ReducedUiMotion) return;
            foreach (var part in new[] { _star, _lines })
            {
                var rect = part.rectTransform; rect.anchorMin = rect.anchorMax = FiredAt;
                rect.anchoredPosition = Vector2.zero; rect.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
                part.enabled = true;
            }
        }

        private void Update()
        {
            var edge = _edge.color; edge.a = _closing * .72f; _edge.color = edge;
            float since = Time.unscaledTime - _firedAt;
            if (_star.enabled)
            {
                float u = since / StarSeconds;
                if (u >= 1) _star.enabled = false;
                else
                {
                    float grow = 1 - (1 - u) * (1 - u) * (1 - u);
                    _star.rectTransform.localScale = Vector3.one * Mathf.Lerp(.35f, 1.15f, grow);
                    var c = _star.color; c.a = u < .55f ? 1 : 1 - (u - .55f) / .45f; _star.color = c;
                }
            }
            if (_lines.enabled)
            {
                float u = since / LinesSeconds;
                if (u >= 1) _lines.enabled = false;
                else
                {
                    _lines.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.25f, 1, 1 - (1 - u) * (1 - u));
                    var c = _lines.color; c.a = .8f * (1 - u * u); _lines.color = c;
                }
            }
        }

        /// <summary>Spikes round a centre, each a sharp triangle of its own length; `Core` also fills the middle in ivory.</summary>
        private sealed class Burst : MaskableGraphic
        {
            public int Spikes = 12;
            public float Inner = 100, Outer = 240, Core;
            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();
                var random = new System.Random(Spikes * 7919);
                float step = Mathf.PI * 2 / Spikes;
                for (int i = 0; i < Spikes; i++)
                {
                    float angle = i * step + (float)random.NextDouble() * step * .3f;
                    float length = Mathf.Lerp(Inner, Outer, .35f + .65f * (float)random.NextDouble());
                    float wide = step * (Core > 0 ? .5f : .1f + .12f * (float)random.NextDouble());
                    int at = vh.currentVertCount;
                    vh.AddVert(Polar(angle - wide, Inner), color, Vector2.zero);
                    vh.AddVert(Polar(angle + wide, Inner), color, Vector2.zero);
                    vh.AddVert(Polar(angle, length), color, Vector2.zero);
                    vh.AddTriangle(at, at + 1, at + 2);
                }
                if (Core <= 0) return;
                int centre = vh.currentVertCount;
                var ivory = new Color(1, .98f, .9f, color.a);
                vh.AddVert(Vector3.zero, ivory, Vector2.zero);
                for (int i = 0; i <= Spikes; i++) vh.AddVert(Polar(i * step, Inner * 1.08f), i % 2 == 0 ? ivory : color, Vector2.zero);
                for (int i = 0; i < Spikes; i++) vh.AddTriangle(centre, centre + 1 + i, centre + 2 + i);
            }
            private static Vector3 Polar(float angle, float radius) => new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        }

        /// <summary>Clear in the middle, the graphic's colour at the rim: one ring of vertices fading out to the frame.</summary>
        private sealed class Vignette : MaskableGraphic
        {
            private const int Segments = 40;
            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();
                var rect = GetPixelAdjustedRect();
                Color clear = color; clear.a = 0;
                for (int i = 0; i <= Segments; i++)
                {
                    float angle = i * Mathf.PI * 2 / Segments, x = Mathf.Cos(angle), y = Mathf.Sin(angle);
                    vh.AddVert(new Vector3(rect.center.x + x * rect.width * .36f, rect.center.y + y * rect.height * .34f), clear, Vector2.zero);
                    // Out to the square that holds the frame's corners.
                    float reach = 1 / Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                    vh.AddVert(new Vector3(rect.center.x + x * reach * rect.width * .5f, rect.center.y + y * reach * rect.height * .5f), color, Vector2.zero);
                }
                for (int i = 0; i < Segments; i++)
                {
                    int a = i * 2;
                    vh.AddTriangle(a, a + 1, a + 3); vh.AddTriangle(a, a + 3, a + 2);
                }
            }
        }
    }
}
