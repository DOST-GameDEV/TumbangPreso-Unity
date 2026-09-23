using UnityEngine;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// A sticker arriving: it is slapped down, a touch too big and a few degrees off, and settles.
    ///
    /// ⚠️ THE ONE ENTRANCE IN THE HUB, AND IT IS STAGGERED BY <see cref="Delay"/> SO A SCREEN ARRIVES
    /// AS A HANDFUL OF STICKERS RATHER THAN AS ONE SLAB. 0.2 s per sticker and at most about 0.35 s
    /// for a whole screen: long enough to read as a gesture, short enough that a player pressing on
    /// through is never waiting for it (a press lands on the final rect; only the drawing animates).
    ///
    /// ⚠️ REDUCED UI MOTION SKIPS IT ENTIRELY: the sticker is simply there.
    /// </summary>
    public sealed class HubSlap : MonoBehaviour
    {
        public float Delay;
        public float Tilt = 3.0f;
        private float _start = -1;
        private const float Duration = 0.2f;

        public static void On(Component target, float delay, float tilt = 3.0f)
        {
            if (target == null) return;
            var slap = HubKit.Ensure<HubSlap>(target.gameObject);
            slap.Delay = delay;
            slap.Tilt = tilt;
            slap.Replay();
        }

        public void Replay()
        {
            if (HubStyle.ReducedMotion) { Settle(); enabled = false; return; }
            _start = Time.unscaledTime + Delay;
            enabled = true;
            Apply(0);
        }

        private void OnEnable() { if (_start < 0) Replay(); }

        private void Update()
        {
            float t = (Time.unscaledTime - _start) / Duration;
            if (t < 0) { Apply(0); return; }
            if (t >= 1) { Settle(); enabled = false; return; }
            Apply(t);
        }

        private void Apply(float t)
        {
            // Overshoot then settle: big at t=0, a hair under at 0.7, exact at 1.
            float ease = 1 - Mathf.Pow(1 - t, 3);
            float scale = Mathf.LerpUnclamped(1.14f, 1.0f, ease) - Mathf.Sin(t * Mathf.PI) * 0.025f;
            transform.localScale = new Vector3(scale, scale, 1);
            transform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(Tilt, 0, ease));
            var group = HubKit.Ensure<CanvasGroup>(gameObject);
            group.alpha = Mathf.Clamp01(t * 4);
        }

        private void Settle()
        {
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            var group = GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 1;
        }
    }
}
