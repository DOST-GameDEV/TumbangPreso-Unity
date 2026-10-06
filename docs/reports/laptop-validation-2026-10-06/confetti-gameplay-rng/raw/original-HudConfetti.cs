using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Toy-cube confetti for a legendary earned moment (UI revamp 2026-10-06): small bevelled
    /// blocks burst out of the centre, tumble under gravity and fade. A fixed pool simulated in
    /// unscaled time, drawn as one mesh; nothing is allocated per frame.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudConfetti : MaskableGraphic
    {
        private const int Count = 26;
        private const float Life = 1.25f, Gravity = -900f;
        private readonly Vector2[] _at = new Vector2[Count], _velocity = new Vector2[Count];
        private readonly float[] _spin = new float[Count], _angle = new float[Count], _size = new float[Count];
        private readonly Color[] _colour = new Color[Count];
        private float _age = Life;
        private readonly Vector2[] _points = new Vector2[8];
        private static readonly Vector2[] Unit = { new Vector2(.55f, 1), new Vector2(-.55f, 1), new Vector2(-1, .55f), new Vector2(-1, -.55f),
            new Vector2(-.55f, -1), new Vector2(.55f, -1), new Vector2(1, -.55f), new Vector2(1, .55f) };

        public bool Live => _age < Life;

        /// <summary>Bursts the pool from the centre in the given colours.</summary>
        public void Burst(Color a, Color b, Color c)
        {
            var rect = rectTransform.rect;
            for (int i = 0; i < Count; i++)
            {
                float angle = Random.Range(15f, 165f) * Mathf.Deg2Rad;
                float speed = Random.Range(380f, 820f);
                _at[i] = new Vector2(Random.Range(-rect.width * .3f, rect.width * .3f), 0);
                _velocity[i] = new Vector2(Mathf.Cos(angle) * speed * 1.3f, Mathf.Sin(angle) * speed);
                _spin[i] = Random.Range(-540f, 540f); _angle[i] = Random.Range(0f, 90f);
                _size[i] = Random.Range(9f, 17f);
                _colour[i] = i % 3 == 0 ? a : i % 3 == 1 ? b : c;
            }
            _age = 0; SetVerticesDirty();
        }

        public void Stop() { _age = Life; SetVerticesDirty(); }

        private void Update()
        {
            if (!Live) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            _age += dt;
            for (int i = 0; i < Count; i++)
            {
                _velocity[i].y += Gravity * dt; _velocity[i] *= 1 - .9f * dt;
                _at[i] += _velocity[i] * dt; _angle[i] += _spin[i] * dt;
            }
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!Live) return;
            var centre = GetPixelAdjustedRect().center;
            float fade = Mathf.Clamp01((Life - _age) / .45f);
            for (int i = 0; i < Count; i++)
            {
                var c = _colour[i]; c.a *= fade; if (c.a <= .01f) continue;
                float h = _size[i] * .5f, rad = _angle[i] * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
                var p = centre + _at[i];
                for (int k = 0; k < 8; k++) { var u = Unit[k] * h; _points[k] = p + new Vector2(u.x * cos - u.y * sin, u.x * sin + u.y * cos); }
                HudDraw.Fan(vh, p, _points, c);
            }
        }
    }
}
