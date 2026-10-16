using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// CONCUSSED, AS EVERYONE ELSE SEES IT (Basilio's Boulder and Continental Drift; `docs/reports/dante-presentation-2026-10-16/
    /// plan.md` section 3): three small basalt pebbles circling the victim's head, slow, because the victim walks slow. They
    /// replace the generic dizzy stars for this one status, so a Concussed player reads as hit by stone, not by a cartoon.
    ///
    /// Each pebble is typed (its own sides, size, tilt and seed): one dust-ochre, two basalt, never three of a kind. The orbit is
    /// a tilted ellipse at 105 degrees a second (the stars run 320) with a short bob, and the ring follows the replicated
    /// Concussed clock: it lives exactly while <see cref="CharacterMotor.IsConcussed"/> holds, settles in over 0.15 s and, in
    /// the last second, the pebbles sink and drop away rather than fade (stone never fades by alpha).
    ///
    /// Hidden on the victim's own first-person screen: the ring would sit on their lens. They feel it instead (the ochre rim,
    /// the double image, the ringing: `ConcussedPerception`).
    /// </summary>
    public sealed class ConcussedPebbles : MonoBehaviour
    {
        private const float HeadHeight = 1.78f;
        private const float DegreesPerSecond = 105f;

        private struct Pebble
        {
            public Transform Node;
            public float Phase, Radius, Lift, Spin, Size;
            public Vector3 SpinAxis;
        }

        // Typed, one row each: phase (deg), orbit radius, height offset, spin (deg/s), size, sides, seed, colour.
        private static readonly (float phase, float radius, float lift, float spin, float size, int sides, int seed, int colour)[] Rows =
        {
            (  0f, .40f,  .02f, 140f, .110f, 5, 11, 0),
            (128f, .37f, -.03f, -95f, .085f, 4, 23, 1),
            (241f, .42f,  .05f, 180f, .070f, 6, 37, 0),
        };

        private static readonly Color Basalt = new Color(.31f, .27f, .24f), DustOchre = new Color(.42f, .32f, .22f);

        private CharacterMotor _owner;
        private readonly Pebble[] _pebbles = new Pebble[3];
        private float _age;

        /// <summary>Attaches (or keeps) the ring on <paramref name="owner"/>. It removes itself when the status ends.</summary>
        public static ConcussedPebbles Attach(CharacterMotor owner)
        {
            if (owner == null) return null;
            var existing = owner.GetComponentInChildren<ConcussedPebbles>(true);
            if (existing != null) return existing;
            var go = new GameObject("~ConcussedPebbles");
            go.transform.SetParent(owner.transform, false);
            go.transform.localPosition = new Vector3(0f, HeadHeight, 0f);
            var ring = go.AddComponent<ConcussedPebbles>();
            ring._owner = owner;
            ring.Build();
            return ring;
        }

        private void Build()
        {
            for (int i = 0; i < Rows.Length; i++)
            {
                var r = Rows[i];
                var mesh = VfxShapes.Prism(r.sides, 1f, .62f, .32f, 18f, r.seed);
                var part = GrowthVfx.Part(transform, "pebble-" + i, mesh, r.colour == 1 ? DustOchre : Basalt);
                part.transform.localScale = new Vector3(r.size, r.size * .78f, r.size * .9f);
                part.transform.localRotation = Quaternion.Euler(r.seed * 7f, r.seed * 13f, r.seed * 3f);
                _pebbles[i] = new Pebble
                {
                    Node = part.transform, Phase = r.phase, Radius = r.radius, Lift = r.lift, Spin = r.spin, Size = r.size,
                    SpinAxis = new Vector3(.3f + .2f * i, 1f, .5f - .3f * i).normalized,
                };
            }
        }

        private void LateUpdate()
        {
            if (_owner == null || !_owner.IsConcussed) { Destroy(gameObject); return; }
            _age += Time.deltaTime;
            bool ownLens = ViewmodelArms.IsFirstPersonFor(_owner);
            float settle = Mathf.Clamp01(_age / .15f);
            float left = _owner.ConcussedLeft;
            float drop = 1f - Mathf.Clamp01(left / 1f);              // the last second: they sink and fall away
            for (int i = 0; i < _pebbles.Length; i++)
            {
                var p = _pebbles[i];
                if (p.Node == null) continue;
                p.Node.gameObject.SetActive(!ownLens);
                if (ownLens) continue;
                float a = (p.Phase + _age * DegreesPerSecond) * Mathf.Deg2Rad;
                // A tilted ellipse: wider side to side than front to back, so it reads as a ring from any side.
                float x = Mathf.Cos(a) * p.Radius, z = Mathf.Sin(a) * p.Radius * .82f;
                float y = p.Lift + Mathf.Sin(a * 2f + i) * .035f + x * .12f;
                // Each falls in turn in the last second, staggered, heavier than air.
                float fall = Mathf.Clamp01(drop * 1.6f - i * .2f);
                y -= fall * fall * .9f;
                p.Node.localPosition = new Vector3(x, y, z) * Mathf.Lerp(.6f, 1f, settle);
                p.Node.localRotation = Quaternion.AngleAxis(_age * p.Spin, p.SpinAxis);
                float s = p.Size * Mathf.Lerp(.4f, 1f, settle) * (1f - Mathf.Clamp01((fall - .85f) / .15f));
                p.Node.localScale = new Vector3(s, s * .78f, s * .9f);
            }
        }
    }
}
