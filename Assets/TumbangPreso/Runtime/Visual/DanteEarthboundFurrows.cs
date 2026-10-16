using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// EARTHBOUND, SHOWN (Basilio's passive: shoves move him half as far; `docs/reports/dante-presentation-2026-10-16/plan.md`
    /// section 1). Before this nothing on screen said WHY he moved less: a shove simply travelled half as far. Now:
    ///  * CONTACT: he leans INTO the shove (chest forward, hips down, 0.35 s, posed on the torso after the animator), and six
    ///    typed grit pieces kick up off his heels;
    ///  * the slide scrapes two heel FURROWS into the court along the path he really slid, ending exactly where he stopped
    ///    (measured from his own positions every frame, never a fixed length, so the furrow is the passive's proof);
    ///  * LINGER: they stay 2.5 s, step from fresh scrape to grey, then crumble flat from the start of the slide to its end.
    /// Presentation only: no lockout, no rule. Every peer runs it from what it sees. The peer that simulates him knows the
    /// shove's own velocity (`CharacterMotor.ExternalFlatSpeed`); the others read a slide as a stagger (every shove staggers)
    /// while he is moving faster than a stumble. Hero Strike only, like the passive.
    /// </summary>
    public sealed class DanteEarthboundFurrows : MonoBehaviour
    {
        private const float StartSpeed = 2.0f;          // m/s of shove before a slide counts
        private const float EndSpeed = .55f;            // below this the slide is over and the furrow ends where he stands
        private const float HeelHalfGap = .13f;         // heel to body centre, sideways
        private const float LingerSeconds = 2.5f;
        private const float BraceSeconds = .35f;

        private static readonly Color Scrape = new Color(.24f, .20f, .17f), Spoil = new Color(.42f, .32f, .22f),
                                      Grey = new Color(.40f, .38f, .36f), Grit = new Color(.31f, .27f, .24f);

        private CharacterMotor _motor;
        private Furrow _current;
        private readonly List<Furrow> _old = new List<Furrow>();
        private Vector3 _last;
        private float _brace = 99f;
        private Vector3 _braceDir;
        private Transform _torso, _hips;

        private sealed class Furrow
        {
            public GameObject Root;
            public Mesh Mesh;
            public Material Groove, Lip;
            public readonly List<Vector3> Path = new List<Vector3>();
            public readonly List<Vector3> Side = new List<Vector3>();
            public float Age, EndedAt = -1f;
            public readonly List<Transform> Grit = new List<Transform>();
            public readonly List<Vector3> GritVelocity = new List<Vector3>();
        }

        /// <summary>The newest furrow's length along its path, m (0 if none): it must equal the slide it records.</summary>
        public float LatestFurrowLength
        {
            get
            {
                var f = _current ?? (_old.Count > 0 ? _old[_old.Count - 1] : null);
                if (f == null) return 0f;
                float d = 0f;
                for (int i = 1; i < f.Path.Count; i++) d += Vector3.Distance(f.Path[i - 1], f.Path[i]);
                return d;
            }
        }

        /// <summary>How many furrows (one per slide, each a pair of heel grooves) are on the court now.</summary>
        public int FurrowCount => _old.Count + (_current != null ? 1 : 0);

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            _last = transform.position;
        }

        private float SlideSpeed(float observed)
        {
            if (_motor == null || _motor.Mode != GameMode.HeroStrike) return 0f;
            if (_motor.IsLocallySimulated()) return _motor.ExternalFlatSpeed;
            return _motor.IsStunned ? observed : 0f;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 pos = transform.position;
            Vector3 step = pos - _last; step.y = 0f;
            _last = pos;
            float observed = step.magnitude / dt;
            float slide = SlideSpeed(observed);

            if (_current == null && slide > StartSpeed && step.sqrMagnitude > 1e-6f && _motor.IsGrounded)
                Begin(pos, step.normalized);
            else if (_current != null)
            {
                if (step.sqrMagnitude > 1e-6f) Extend(pos, step.normalized);
                if (slide < EndSpeed || !_motor.IsGrounded) { _current.EndedAt = _current.Age; _old.Add(_current); _current = null; }
            }

            if (_current != null) Step(_current, dt);
            for (int i = _old.Count - 1; i >= 0; i--)
            {
                var f = _old[i];
                Step(f, dt);
                if (f.Age - f.EndedAt > LingerSeconds + .6f) { Destroy(f.Root); Destroy(f.Mesh); Destroy(f.Groove); Destroy(f.Lip); _old.RemoveAt(i); }
            }
            Brace(dt);
        }

        // ---------------------------------------------------------------- contact

        private void Begin(Vector3 at, Vector3 dir)
        {
            var f = new Furrow { Root = new GameObject("~EarthboundFurrow") };
            f.Mesh = new Mesh { name = "earthbound-furrow" };
            f.Mesh.MarkDynamic();
            var mf = f.Root.AddComponent<MeshFilter>(); mf.sharedMesh = f.Mesh;
            var mr = f.Root.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Two submeshes: the dark scraped groove, and the dust-ochre spoil pushed up at its sides.
            VfxMaterial.Solid(mr, Scrape);
            f.Groove = mr.sharedMaterial;
            var lipHolder = new GameObject("lip-material");
            lipHolder.transform.SetParent(f.Root.transform, false);
            var lipRenderer = lipHolder.AddComponent<MeshRenderer>();
            VfxMaterial.Solid(lipRenderer, Spoil);
            f.Lip = lipRenderer.sharedMaterial;
            lipRenderer.enabled = false;
            mr.sharedMaterials = new[] { f.Groove, f.Lip };
            Extend(at, dir, f);
            Kick(f, at, dir);
            _current = f;
            _brace = 0f;
            _braceDir = dir;
        }

        // Six grit pieces off the heels, typed: (heel -1 left / +1 right, size, up speed, back speed, sideways, spin seed).
        private static readonly (float heel, float size, float up, float back, float side, int seed)[] GritRows =
        {
            (-1f, .060f, 2.6f, 1.9f, -.6f, 3), (-1f, .045f, 3.1f, 1.2f, -.2f, 7), (-1f, .035f, 2.2f, 2.4f, -.9f, 12),
            ( 1f, .055f, 2.9f, 1.6f,  .5f, 5), ( 1f, .040f, 3.4f, 2.1f,  .8f, 9), ( 1f, .030f, 2.0f, 1.1f,  .3f, 15),
        };

        private void Kick(Furrow f, Vector3 at, Vector3 dir)
        {
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            float ground = Slipper.GroundY(at);
            foreach (var g in GritRows)
            {
                var mesh = VfxShapes.Prism(4 + g.seed % 3, 1f, .6f, .35f, 22f, g.seed);
                var part = GrowthVfx.Part(f.Root.transform, "grit", mesh, g.seed % 2 == 0 ? Spoil : Grit);
                var t = part.transform;
                t.position = new Vector3(at.x, ground + .04f, at.z) + side * (g.heel * HeelHalfGap) - dir * .1f;
                t.localScale = Vector3.one * g.size;
                t.rotation = Quaternion.Euler(g.seed * 17f, g.seed * 31f, g.seed * 7f);
                f.Grit.Add(t);
                // Kicked BACK against the slide (the heels dig in), up and a little out.
                f.GritVelocity.Add(-dir * g.back + side * g.side + Vector3.up * g.up);
            }
        }

        // ---------------------------------------------------------------- the furrow, measured

        private void Extend(Vector3 at, Vector3 dir) => Extend(at, dir, _current);

        private static void Extend(Vector3 at, Vector3 dir, Furrow f)
        {
            Vector3 flat = new Vector3(at.x, Slipper.GroundY(at) + .012f, at.z);
            if (f.Path.Count > 0 && (flat - f.Path[f.Path.Count - 1]).sqrMagnitude < .0025f) return;   // 5 cm steps
            f.Path.Add(flat);
            f.Side.Add(Vector3.Cross(Vector3.up, dir).normalized);
        }

        private void Step(Furrow f, float dt)
        {
            f.Age += dt;
            float g = Physics.gravity.y;
            for (int i = 0; i < f.Grit.Count; i++)
            {
                var t = f.Grit[i];
                if (t == null) continue;
                var v = f.GritVelocity[i];
                float ground = Slipper.GroundY(t.position) + .02f;
                if (t.position.y > ground || v.y > 0f)
                {
                    v.y += g * dt;
                    t.position += v * dt;
                    t.Rotate(new Vector3(400f, 230f, 90f) * dt, Space.Self);
                    if (t.position.y < ground) { t.position = new Vector3(t.position.x, ground, t.position.z); v = Vector3.zero; }
                    f.GritVelocity[i] = v;
                }
            }
            Rebuild(f);
        }

        /// <summary>
        /// Two grooves (one per heel), each a dark scraped strip with a dust-ochre spoil lip at each side. While lingering:
        /// fresh, then grey; then they crumble flat from the start of the slide to its end, narrowing to nothing segment by
        /// segment (a scrape fills in, it never fades).
        /// </summary>
        private static void Rebuild(Furrow f)
        {
            int n = f.Path.Count;
            if (n < 2) return;
            float since = f.EndedAt < 0f ? 0f : f.Age - f.EndedAt;
            float grey = Mathf.Clamp01((since - 1.2f) / .6f);
            float crumble = Mathf.Clamp01((since - (LingerSeconds - .4f)) / .9f);
            var verts = new List<Vector3>(n * 8);
            var tris = new List<int>(n * 12);
            var lips = new List<int>(n * 24);
            Color groove = Color.Lerp(Scrape, Grey * .85f, grey), lip = Color.Lerp(Spoil, Grey, grey);
            for (int heel = -1; heel <= 1; heel += 2)
            {
                int start = verts.Count;
                for (int i = 0; i < n; i++)
                {
                    float along = i / (float)(n - 1);
                    // Each segment fills in when the crumble front passes it; the heel's start digs in, its end fades out in width.
                    float fill = Mathf.Clamp01((crumble * 1.25f - along) / .25f);
                    float w = .045f * Mathf.Clamp01(along * 6f + .4f) * Mathf.Clamp01((1f - along) * 8f + .5f) * (1f - fill);
                    Vector3 c = f.Path[i] + f.Side[i] * (heel * HeelHalfGap);
                    Vector3 s = f.Side[i];
                    verts.Add(c - s * (w * 1.9f) + Vector3.up * .004f);
                    verts.Add(c - s * w);
                    verts.Add(c + s * w);
                    verts.Add(c + s * (w * 1.9f) + Vector3.up * .004f);
                }
                for (int i = 0; i < n - 1; i++)
                    for (int k = 0; k < 3; k++)
                    {
                        int a = start + i * 4 + k, b = a + 4;
                        var into = k == 1 ? tris : lips;
                        into.Add(a); into.Add(b); into.Add(a + 1);
                        into.Add(a + 1); into.Add(b); into.Add(b + 1);
                    }
            }
            f.Mesh.Clear();
            f.Mesh.subMeshCount = 2;
            f.Mesh.SetVertices(verts);
            f.Mesh.SetTriangles(tris, 0);
            f.Mesh.SetTriangles(lips, 1);
            f.Mesh.RecalculateNormals();
            f.Mesh.RecalculateBounds();
            if (f.Groove != null) f.Groove.color = groove;
            if (f.Lip != null) f.Lip.color = lip;
            // Grit settles into the court with the crumble.
            foreach (var t in f.Grit) if (t != null && crumble > 0f) t.localScale = Vector3.one * Mathf.Max(0f, t.localScale.x * (1f - crumble * .08f));
        }

        // ---------------------------------------------------------------- the brace

        /// <summary>He leans INTO the shove: chest forward and down toward where the push came from, hips sunk, 0.35 s.</summary>
        private void Brace(float dt)
        {
            if (_brace > BraceSeconds) return;
            _brace += dt;
            if (_torso == null || _hips == null) FindBones();
            if (_torso == null) return;
            float k = Mathf.Sin(Mathf.Clamp01(_brace / BraceSeconds) * Mathf.PI);
            // Into the push: lean against its direction (the push goes along _braceDir; he leans back toward its source).
            Vector3 axis = Vector3.Cross(Vector3.up, -_braceDir);
            _torso.rotation = Quaternion.AngleAxis(16f * k, axis) * _torso.rotation;
            if (_hips != null) _hips.position += Vector3.down * (.06f * k);
        }

        private void FindBones()
        {
            var visual = GetComponent<CharacterVisual>();
            var model = visual != null ? visual.Model : null;
            if (model == null) return;
            foreach (var t in model.GetComponentsInChildren<Transform>())
            {
                if (t.name == "torso") _torso = t;
                else if (t.name == "hips" || t.name == "root") _hips = _hips ?? t;
            }
        }

        private void OnDestroy()
        {
            void Drop(Furrow f) { if (f == null) return; if (f.Root != null) Destroy(f.Root); if (f.Mesh != null) Destroy(f.Mesh); }
            Drop(_current);
            foreach (var f in _old) Drop(f);
        }
    }
}
