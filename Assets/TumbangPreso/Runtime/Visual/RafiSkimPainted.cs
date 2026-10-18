using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    // ⚠️ SKIM, PAINTED. A coat of Rafi water on the loaded slipper, drawn with the stream shader (`RafiStream`): the
    // streak picture read twice at two speeds and multiplied, so the coat churns rather than sliding, and the tubes
    // swell and pinch from the clock. It never turns. When the load ends it dies in place: the tail is walked round the
    // loop and the erosion picture tears it away.
    //
    // ⚠️ VISIBILITY PASS (2026-10-18). The first film showed a thin blue band on the sole's bottom edge, readable only
    // close up and gone at game distance, on a pale sunlit court with no bloom. What changed, and why:
    //   * THE RIM WRAPS THE EDGE. It is a fat oval tube centred on the SOLE's mid-thickness (not its underside), its
    //     height taller than the sole, so the water rolls up over the top and bottom faces round the whole outline.
    //   * IT COVERS THE FACES. A thin water film lies over the footbed and the underside (submesh 1, at a lower share),
    //     so from above and below the slipper reads as wet, not outlined.
    //   * IT RUNS OFF. Five short tapered streams hang off the heel and the sides; the shader's pictures and swell run
    //     down them from the same clock, so drops travel down and off.
    //   * IT IS PAINTED, NOT LIT. Covers what is behind (`_Light` near 0), a saturated blue body over a deep navy shade
    //     with the white foam lines pushed up, three bands that hold their contrast on a pale court.
    // Off until `Enabled` is set; the line renderer coat (`RafiSkimCoating`) stays the shipped look until then.
    // One mesh per coat, built once; one shared material; per-frame values go in a property block per submesh.
    public sealed class RafiSkimPainted : MonoBehaviour
    {
        public static bool Enabled = false;

        private static Material _shared;
        private static readonly int AgeId = Shader.PropertyToID("_Age"), OpacityId = Shader.PropertyToID("_Opacity"),
            TailId = Shader.PropertyToID("_Tail"), SeedId = Shader.PropertyToID("_Seed");
        private const float DieSeconds = 0.35f;
        // The film over the faces is a share of the rim's strength: enough to read as wet, not enough to hide the shoe.
        private const float FilmShare = 0.5f;

        private RafiHeroKit _kit;
        private Slipper _shoe;
        private MeshFilter _target;
        private Mesh _source;
        private Renderer _sourceRenderer;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _block;
        private float _age, _dying = -1f, _seed;

        public static void Ensure(MeshFilter target, Slipper shoe, RafiHeroKit kit)
        {
            if (target == null || target.sharedMesh == null || shoe == null || kit == null || !kit.IsSkimLoadedFor(shoe)) return;
            var existing = target.GetComponentInChildren<RafiSkimPainted>(true);
            if (existing != null)
            {
                if (existing._shoe == shoe && existing._kit == kit && existing._source == target.sharedMesh && existing._dying < 0) return;
                existing.gameObject.SetActive(false); Destroy(existing.gameObject);
            }
            var go = new GameObject("SkimPaintedCoat"); go.layer = target.gameObject.layer;
            go.transform.SetParent(target.transform, false);
            var fx = go.AddComponent<RafiSkimPainted>();
            fx._kit = kit; fx._shoe = shoe; fx._target = target; fx._source = target.sharedMesh;
            fx._sourceRenderer = target.GetComponent<Renderer>(); fx._seed = Random.value * 10f;
            fx.Build();
        }

        private static Material Shared()
        {
            if (_shared != null) return _shared;
            var shader = Resources.Load<Shader>("Shaders/RafiStream");
            if (shader == null) throw new System.InvalidOperationException("Rafi stream shader is missing.");
            _shared = new Material(shader) { name = "Skim painted coat" };
            _shared.SetTexture("_MaskTex", Resources.Load<Texture2D>("Textures/UltimateFx/water_flow"));
            _shared.SetTexture("_LineTex", Resources.Load<Texture2D>("Textures/UltimateFx/water_lines"));
            _shared.SetTexture("_ErodeTex", Resources.Load<Texture2D>("Textures/UltimateFx/water_foam"));
            // A loop, not a stream: no thinning tail and no rounded nose, so the seam does not show while it lives.
            _shared.SetFloat("_Head", 1.01f); _shared.SetFloat("_Thin", 0.001f); _shared.SetFloat("_Nose", 0.0001f);
            _shared.SetVector("_Tile1", new Vector4(5f, 1f, 0, 0)); _shared.SetVector("_Scroll1", new Vector4(1.6f, 0.5f, 0, 0));
            _shared.SetVector("_Tile2", new Vector4(3f, 0.7f, 0, 0)); _shared.SetVector("_Scroll2", new Vector4(-0.9f, -0.3f, 0, 0));
            _shared.SetVector("_LineTile", new Vector4(4f, 1f, 0, 0)); _shared.SetVector("_LineScroll", new Vector4(2.2f, 0.6f, 0, 0));
            // The shoe is about 0.43 m long; the stream's wobble would throw the coat off the sole.
            _shared.SetVector("_Wobble", new Vector4(0.004f, 3f, 5f, 0));
            _shared.SetVector("_Ripple", new Vector4(0.2f, 3f, 18f, 9f));
            // ⚠️ Daylight values. The stream's own colours are tuned to glow; on a pale court a glow goes pale. These
            // are painted bands: deep shade, saturated body, white foam, and a body that is solid almost everywhere.
            _shared.SetColor("_Deep", new Color(0.02f, 0.16f, 0.40f, 1f));
            _shared.SetColor("_Body", new Color(0.05f, 0.42f, 0.86f, 1f));
            _shared.SetColor("_Pale", new Color(0.42f, 0.80f, 1f, 1f));
            _shared.SetColor("_Foam", new Color(0.97f, 1f, 1f, 1f));
            _shared.SetVector("_Cut", new Vector4(0.0f, 0.2f, 0, 0));
            _shared.SetFloat("_LineGain", 1.5f);
            _shared.SetFloat("_Rim", 0.9f);
            _shared.SetFloat("_HueDrift", 8f);
            _shared.SetFloat("_Light", 0.06f);
            return _shared;
        }

        private void Build()
        {
            _block = new MaterialPropertyBlock();
            var bounds = _source.bounds; var size = bounds.size;
            Vector3 along = size.x >= size.y && size.x >= size.z ? Vector3.right : size.y >= size.z ? Vector3.up : Vector3.forward;
            Vector3 normal = size.y <= size.x && size.y <= size.z ? Vector3.up : size.x <= size.z ? Vector3.right : Vector3.forward;
            if (Mathf.Abs(Vector3.Dot(along, normal)) > .5f) normal = Vector3.up;
            Vector3 across = Vector3.Cross(normal, along);
            float length = Mathf.Max(size.x, size.y, size.z);
            float width = Mathf.Abs(Vector3.Dot(size, across));
            float thick = Mathf.Abs(Vector3.Dot(size, normal));
            // The bounds' height includes the strap; the sole is the bottom slab of it, about a thirteenth of the length.
            float sole = Mathf.Min(thick, length * .075f);
            Vector3 bottom = bounds.center - normal * thick * .5f;
            Vector3 c = bottom + normal * sole * .5f;
            // Same broad toe, close waist and tucked heel as the shipped coat: what is drawn hugs what skims.
            var knots = new[]{
                c+along*length*.46f, c+along*length*.39f+across*width*.44f, c+along*length*.16f+across*width*.52f,
                c-along*length*.19f+across*width*.35f, c-along*length*.41f+across*width*.27f, c-along*length*.46f,
                c-along*length*.40f-across*width*.29f, c-along*length*.18f-across*width*.37f, c+along*length*.17f-across*width*.52f,
                c+along*length*.39f-across*width*.42f};
            const int per = 5, round = 9; int n = knots.Length; int ring = n * per;
            // An oval round the edge: out past the outline by a sixteenth of the length, and taller than the sole so
            // it rolls over both faces.
            float outR = length * .062f, upR = sole * .5f + length * .04f;
            var spine = new Vector3[ring];
            for (int i = 0; i < ring; i++)
            {
                int k = i / per; float t = (i % per) / (float)per;
                Vector3 p0 = knots[(k + n - 1) % n], p1 = knots[k], p2 = knots[(k + 1) % n], p3 = knots[(k + 2) % n];
                spine[i] = 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (3f * p1 - p0 - 3f * p2 + p3) * t * t * t);
            }
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>(); var centres = new List<Vector3>();
            var rimTris = new List<int>(); var filmTris = new List<int>();

            // The rim.
            for (int i = 0; i <= ring; i++)
            {
                Vector3 p = spine[i % ring], tangent = (spine[(i + 1) % ring] - spine[(i + ring - 1) % ring]).normalized;
                Vector3 side = Vector3.Cross(normal, tangent).normalized;
                if (Vector3.Dot(side, p - c) < 0) side = -side;
                for (int j = 0; j < round; j++)
                {
                    float a = j / (float)(round - 1) * Mathf.PI * 2f;
                    Vector3 off = side * Mathf.Cos(a) * outR + normal * Mathf.Sin(a) * upR;
                    verts.Add(p + off); norms.Add(off.normalized);
                    uvs.Add(new Vector2(i / (float)ring, j / (float)(round - 1))); centres.Add(p);
                }
            }
            for (int i = 0; i < ring; i++) for (int j = 0; j < round - 1; j++)
            {
                int a = i * round + j, b = a + round;
                rimTris.Add(a); rimTris.Add(b); rimTris.Add(a + 1); rimTris.Add(a + 1); rimTris.Add(b); rimTris.Add(b + 1);
            }

            // The film over the footbed and the underside: a fan from the middle to just inside the rim, each vertex
            // its own centre so the shader's swell leaves it lying flat on the face.
            for (int face = -1; face <= 1; face += 2)
            {
                Vector3 lift = normal * (face * (sole * .5f + .005f));
                int middle = verts.Count;
                verts.Add(c + lift); norms.Add(normal * face); uvs.Add(new Vector2(0.5f, 0.5f)); centres.Add(c + lift);
                int first = verts.Count;
                for (int i = 0; i <= ring; i++)
                {
                    Vector3 p = c + (spine[i % ring] - c) * .98f + lift;
                    verts.Add(p); norms.Add(normal * face); uvs.Add(new Vector2(i / (float)ring, face > 0 ? 0.25f : 0.75f)); centres.Add(p);
                }
                for (int i = 0; i < ring; i++)
                {
                    filmTris.Add(middle);
                    if (face > 0) { filmTris.Add(first + i + 1); filmTris.Add(first + i); }
                    else { filmTris.Add(first + i); filmTris.Add(first + i + 1); }
                }
            }

            // Five streams running off the heel and the sides, tapered to a point, each its own stretch of the loop's
            // u so the tail that eats the rim eats them in the same walk.
            int[] from = { ring / 2 - 2, ring / 2 + 3, ring / 4, ring * 3 / 4 + 1, ring * 3 / 8 };
            float[] reach = { .085f, .065f, .055f, .07f, .05f };
            const int segs = 6, dripRound = 7;
            for (int d = 0; d < from.Length; d++)
            {
                Vector3 root = spine[from[d] % ring];
                Vector3 outward = Vector3.ProjectOnPlane(root - c, normal).normalized;
                Vector3 dir = (-normal * .85f + outward * .45f - along * .15f).normalized;
                Vector3 start = root + outward * outR * .4f - normal * upR * .55f;
                float u0 = from[d] / (float)ring;
                int baseV = verts.Count;
                for (int i = 0; i <= segs; i++)
                {
                    float t = i / (float)segs;
                    Vector3 p = start + dir * reach[d] * t;
                    float r = Mathf.Lerp(outR * .6f, outR * .12f, t);
                    // A bead near the tip: the drop about to leave.
                    r *= 1f + .6f * Mathf.Exp(-Mathf.Pow((t - .8f) / .1f, 2f));
                    Vector3 s1 = Vector3.Cross(dir, normal).sqrMagnitude > 1e-4f ? Vector3.Cross(dir, normal).normalized : across;
                    Vector3 s2 = Vector3.Cross(dir, s1).normalized;
                    for (int j = 0; j < dripRound; j++)
                    {
                        float a = j / (float)(dripRound - 1) * Mathf.PI * 2f;
                        Vector3 off = (s1 * Mathf.Cos(a) + s2 * Mathf.Sin(a)) * r;
                        verts.Add(p + off); norms.Add(off.normalized);
                        uvs.Add(new Vector2(Mathf.Repeat(u0 + t * .12f, 1f), j / (float)(dripRound - 1))); centres.Add(p);
                    }
                }
                for (int i = 0; i < segs; i++) for (int j = 0; j < dripRound - 1; j++)
                {
                    int a = baseV + i * dripRound + j, b = a + dripRound;
                    rimTris.Add(a); rimTris.Add(b); rimTris.Add(a + 1); rimTris.Add(a + 1); rimTris.Add(b); rimTris.Add(b + 1);
                }
            }

            var mesh = new Mesh { name = "Skim painted coat" };
            mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetUVs(0, uvs); mesh.SetUVs(1, centres);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(rimTris, 0); mesh.SetTriangles(filmTris, 1);
            mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            _renderer = gameObject.AddComponent<MeshRenderer>();
            var shared = Shared();
            _renderer.sharedMaterials = new[] { shared, shared };
            _renderer.receiveShadows = false; _renderer.shadowCastingMode = ShadowCastingMode.Off;
            VfxRenderTag.Own(gameObject, shared);
            Sample(0f);
        }

        private void LateUpdate() => Sample(Time.deltaTime);

        private void Sample(float dt)
        {
            _age += dt;
            bool alive = _kit != null && _shoe != null && _kit.IsSkimLoadedFor(_shoe) && _target != null && _target.sharedMesh == _source;
            if (!alive && _dying < 0) _dying = 0f;
            if (_dying >= 0) _dying += dt;
            if (_dying > DieSeconds || _target == null) { gameObject.SetActive(false); Destroy(gameObject); return; }
            float rise = Mathf.SmoothStep(0, 1, Mathf.Clamp01(_age / .2f));
            float tail = _dying < 0 ? 0f : Mathf.SmoothStep(0, 1, _dying / DieSeconds);
            _block.SetFloat(AgeId, _age); _block.SetFloat(SeedId, _seed); _block.SetFloat(TailId, tail);
            _block.SetFloat(OpacityId, rise);
            _renderer.SetPropertyBlock(_block, 0);
            _block.SetFloat(OpacityId, FilmShare * rise);
            _renderer.SetPropertyBlock(_block, 1);
            if (_sourceRenderer != null)
            {
                _renderer.enabled = _sourceRenderer.enabled;
                _renderer.forceRenderingOff = _sourceRenderer.forceRenderingOff;
            }
        }
    }
}
