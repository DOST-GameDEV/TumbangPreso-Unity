using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// A short jagged electric arc standing off a shock trail anchor, rebuilt on a timer.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE A FLOOR HAZARD SEEN FROM EYE HEIGHT IS A LINE. Zack's shock trail
    /// was one flat translucent disc, and the player it most needs to warn is running at it in
    /// first person, where a disc on the ground is edge-on and about two pixels tall. The arc is
    /// the part that has a SILHOUETTE from the angle the danger is approached from.
    ///
    /// ⚠️ THE JITTER IS THE READ, NOT DECORATION. A straight bar reads as a post or a bollard,
    /// which is a thing you walk around; a line that changes shape four times a second reads as
    /// current, which is a thing you do not touch. Rebuilding the segments is what buys that,
    /// and it is why this is a component rather than static geometry.
    ///
    /// ⚠️⚠️ KNEE HEIGHT, NEVER HIGHER. `docs/VISION.md` § 2 rule 5 requires a mid-fight frame to
    /// still show the lata, the chalk and every player. Six of these are live during a sprint,
    /// and six head-height arcs would be a fence across the arena. At 0.45 m they mark the
    /// ground without hiding anybody standing behind them.
    ///
    /// `docs/Hero_Strike_Balance.md` § 3.2.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArcFlicker : MonoBehaviour
    {
        private const int Segments = 4;
        private const float Height = 0.45f;
        private const float RebuildInterval = 0.09f;

        private readonly Transform[] _segments = new Transform[Segments];
        private float _radius = 1.0f;
        private float _next;

        public void Build(float radius)
        {
            _radius = Mathf.Max(0.2f, radius);

            for (int i = 0; i < Segments; i++)
            {
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = $"ArcSeg_{i}";
                seg.transform.SetParent(transform, false);

                VfxMaterial.Ghost(seg.GetComponent<Renderer>(),
                                  new Color(1.0f, 0.98f, 0.55f, 0.85f), 1.6f);
                VfxMaterial.StripCollider(seg);

                _segments[i] = seg.transform;
            }

            Reshape();
        }

        private void Update()
        {
            _next -= Time.deltaTime;
            if (_next > 0.0f) return;

            _next = RebuildInterval;
            Reshape();
        }

        /// <summary>
        /// Walks a point up from the floor, kicking it sideways at every step, and stretches one
        /// thin cube along each leg.
        ///
        /// ⚠️ THE SIDEWAYS KICK IS SCALED TO THE HAZARD RADIUS. At the trail's 1.0 m a fixed
        /// offset would throw the arc outside the circle it is supposed to be marking, which is
        /// the same class of fault as an effect reaching past its own telegraph.
        /// </summary>
        private void Reshape()
        {
            Vector3 from = Vector3.zero;
            float step = Height / Segments;

            for (int i = 0; i < Segments; i++)
            {
                var seg = _segments[i];
                if (seg == null) continue;

                // The tip converges back toward the axis, so the arc tapers instead of
                // wandering off. A bolt that leans is a bolt that looks like it fell over.
                float spread = _radius * 0.30f * (1.0f - i / (float)Segments);

                Vector3 to = new Vector3(Random.Range(-spread, spread),
                                         step * (i + 1),
                                         Random.Range(-spread, spread));

                Vector3 leg = to - from;
                float len = leg.magnitude;
                if (len < 0.0001f) continue;

                seg.localPosition = from + leg * 0.5f;
                seg.localRotation = Quaternion.LookRotation(leg / len, Vector3.up);
                seg.localScale = new Vector3(0.045f, 0.045f, len);

                from = to;
            }
        }
    }
}
