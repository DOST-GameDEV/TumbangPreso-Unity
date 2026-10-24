using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Sean's marks on the court: the ring where a blast lands, the ring under the player a MAGMA FIST is coming for, the
    /// ring a VOLCANIC BOMB fills as it falls, and the ring round a hot can draining to "you may reset"
    /// (`docs/reports/sean-kit-2026-10-24/feel-and-readability.md` § 1 rules 2, 3 and 5).
    ///
    /// ⚠️⚠️ THE RING IS THE HITBOX. Callers pass the resolved radius from `SeanRules`, never a look that is a little bigger
    /// or smaller; a player who stands just outside the drawn line is just outside the blast.
    ///
    /// ⚠️⚠️ DRAPED ONCE, THEN ONLY SWITCHED. Every piece is laid flat and pushed onto the court by
    /// `VfxShapes.DrapeToGround` when the mark is made, so it climbs a kerb, lies on a slope and is hidden by a wall exactly
    /// as paint would be. Draping is a raycast per vertex, so a mark that shows progress is built from pieces (bands that
    /// fill from the rim inward, arcs that drain round the clock) that are turned on and off, never re-draped per frame.
    /// The one exception is a mark given something to follow (the fist's target, a sliding can): it is small and kept flat at
    /// the court under what it follows.
    /// </summary>
    public sealed class SeanGroundMark : MonoBehaviour
    {
        public enum Mode
        {
            /// <summary>Bright at once, then gone: a blast's ring (the impact peaks at the start).</summary>
            Flash,
            /// <summary>Fills from the rim inward over its life: a bomb falling onto this spot.</summary>
            Fill,
            /// <summary>Drains round the clock over its life: a hot can cooling.</summary>
            Drain,
            /// <summary>Rides under a player until stopped: the MAGMA FIST's target.</summary>
            Follow,
        }

        private const int Pieces = 8;
        private const float Rim = 0.12f;      // the outline's width as a share of the radius

        private Mode _mode;
        private float _life, _age, _radius;
        private Color _colour;
        private Transform _follow;
        private Renderer _outline;
        private readonly Renderer[] _pieces = new Renderer[Pieces];
        private float _brightness = 1.0f;

        /// <summary>How strongly it is drawn (1 is normal). The player it is aimed at sees theirs brighter.</summary>
        public float Brightness { get => _brightness; set => _brightness = Mathf.Clamp(value, 0.2f, 2.0f); }

        public static SeanGroundMark Spawn(Vector3 at, float radius, Mode mode, float seconds, Color colour,
                                           Transform follow = null)
        {
            var go = new GameObject("SeanGroundMark_" + mode);
            if (VfxShapes.TryGroundUnder(at, 1.5f, 3.0f, out float y)) at.y = y;
            go.transform.position = at;
            var mark = go.AddComponent<SeanGroundMark>();
            mark._mode = mode; mark._life = Mathf.Max(0.05f, seconds); mark._radius = Mathf.Max(0.1f, radius);
            mark._colour = colour; mark._follow = follow;
            mark.Build();
            mark.Paint();
            return mark;
        }

        /// <summary>Ends it now (the fist arrived, the bomb landed).</summary>
        public void Stop() { if (this != null) Destroy(gameObject); }

        private void Build()
        {
            // A mark that rides something is kept flat at the court under it; a fixed one is draped once.
            bool drape = _follow == null;
            var outline = VfxShapes.Lay(transform, "Outline", Arc(40, 1.0f - Rim, 1.0f, 0, 360), _radius, 0.02f);
            _outline = Finish(outline, drape);

            for (int i = 0; i < Pieces; i++)
            {
                Mesh piece;
                if (_mode == Mode.Drain)
                    piece = Arc(6, 1.0f - Rim * 2.4f, 1.0f - Rim, i * 360f / Pieces + 2f, (i + 1) * 360f / Pieces - 2f);
                else if (_mode == Mode.Fill)
                {
                    // Band 0 is the rim's inside edge, band 7 the centre, so it fills from the outside in, as a shadow grows.
                    float outer = (1.0f - Rim) * (1.0f - i / (float)Pieces), inner = (1.0f - Rim) * (1.0f - (i + 1) / (float)Pieces);
                    piece = Arc(32, Mathf.Max(0.0f, inner), outer, 0, 360);
                }
                else continue;
                var go = VfxShapes.Lay(transform, "Piece" + i, piece, _radius, 0.018f);
                _pieces[i] = Finish(go, drape);
            }
        }

        private Renderer Finish(GameObject go, bool drape)
        {
            var r = go.GetComponent<Renderer>();
            VfxMaterial.Ghost(r, _colour, 1.0f);
            VfxMaterial.StripCollider(go);
            if (drape) VfxShapes.DrapeToGround(go);
            return r;
        }

        private void Update()
        {
            if (PresentationClock.Held) return;
            _age += Time.deltaTime;
            if (_age >= _life) { Destroy(gameObject); return; }
            if (_follow != null || _mode == Mode.Follow)
            {
                if (_follow == null) { Destroy(gameObject); return; }
                Vector3 at = _follow.position;
                if (VfxShapes.TryGroundUnder(at, 1.5f, 3.0f, out float y)) at.y = y;
                transform.position = at;
            }
            Paint();
        }

        private void Paint()
        {
            float t = Mathf.Clamp01(_age / _life);
            float alpha;
            switch (_mode)
            {
                case Mode.Flash:
                    // Full at once, gone by the end: the peak sits in the first quarter.
                    alpha = t < 0.25f ? 1.0f : 1.0f - (t - 0.25f) / 0.75f;
                    break;
                case Mode.Follow:
                    // A slow pulse so it reads as "coming", never a steady decal.
                    alpha = 0.65f + 0.35f * Mathf.Sin(_age * 9.0f);
                    break;
                default:
                    alpha = 0.85f;
                    break;
            }
            Tint(_outline, alpha);
            for (int i = 0; i < Pieces; i++)
            {
                if (_pieces[i] == null) continue;
                bool on = _mode == Mode.Fill ? t >= i / (float)Pieces : t < (Pieces - i) / (float)Pieces;
                _pieces[i].enabled = on;
                if (on) Tint(_pieces[i], _mode == Mode.Fill ? 0.35f : 0.75f);
            }
        }

        private void Tint(Renderer r, float alpha)
        {
            if (r == null || r.sharedMaterial == null) return;
            var c = new Color(_colour.r, _colour.g, _colour.b, Mathf.Clamp01(alpha * _brightness));
            r.sharedMaterial.color = c;
            if (r.sharedMaterial.HasProperty("_BaseColor")) r.sharedMaterial.SetColor("_BaseColor", c);
        }

        /// <summary>A flat arc of unit outer radius in XZ, from <paramref name="fromDeg"/> to <paramref name="toDeg"/>.</summary>
        private static Mesh Arc(int sides, float inner, float outer, float fromDeg, float toDeg)
        {
            var mesh = new Mesh { name = "SeanMarkArc" };
            var v = new Vector3[(sides + 1) * 2];
            var tris = new int[sides * 6];
            for (int i = 0; i <= sides; i++)
            {
                float a = Mathf.Deg2Rad * Mathf.Lerp(fromDeg, toDeg, i / (float)sides);
                var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                v[i * 2] = d * inner; v[i * 2 + 1] = d * outer;
                if (i == sides) continue;
                int b = i * 6, k = i * 2;
                tris[b] = k; tris[b + 1] = k + 1; tris[b + 2] = k + 2; tris[b + 3] = k + 2; tris[b + 4] = k + 1; tris[b + 5] = k + 3;
            }
            mesh.vertices = v; mesh.triangles = tris;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
