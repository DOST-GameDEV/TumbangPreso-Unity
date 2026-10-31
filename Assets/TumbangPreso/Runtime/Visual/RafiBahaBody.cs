using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ ILYAS'S BAHA: ONE COHERENT BODY OF WATER. The owner, of the streak-built version: "its too noisy. i need it looking
    /// exactly like the reference". It wears one painted picture (`baha_body.png`, `tools/build_baha_body_texture.py`), has a
    /// soft blue pool under it and soft torn splashes along its front edge. The bench (`FxStudyRafiBaha`) and the match
    /// (`RafiBahaFlood`) both play this.
    ///
    /// ⚠️ THE RULE STILL DECIDES WHERE THE WATER GOES (`RafiRules`): after the 0.8 s warning each of the nine lanes runs at 5 m/s
    /// to its own end, and the grid's front is those nine numbers, blended so the silhouette bends round what stops it. What a
    /// stopped lane holds is torn away after its break, so it leaves no stub of front beside the cover. Look only.
    ///
    /// ⚠️ GPU-DRIVEN: three shared meshes built once and moved in the shader (`RafiBahaBody.shader`), three draws, about 5,900
    /// triangles, three materials for the life of the game. Each frame sets a clock, nine lane fronts, nine break clocks.
    /// </summary>
    public sealed class RafiBahaBody
    {
        private const int Lanes = 9, Columns = 61, Rows = 49, Around = 14, Clusters = 60, Lead = 32;
        private const float Wide = RafiRules.BahaHalfWidth * 1.12f, BodyLength = 9f, High = 0.62f;
        private static Mesh _body, _pool, _spray, _crest, _boat;
        private static Material _bodyPaint, _poolPaint, _sprayPaint, _crestPaint, _shadowPaint, _wetPaint, _paperPaint, _puddlePaint;
        private static readonly int AgeId = Shader.PropertyToID("_Age"), RiseId = Shader.PropertyToID("_Rise"), RunId = Shader.PropertyToID("_Run"),
            DrainId = Shader.PropertyToID("_Drain"), LaneZId = Shader.PropertyToID("_LaneZ"), LaneHitId = Shader.PropertyToID("_LaneHit"), LaneGoneId = Shader.PropertyToID("_LaneGone"), WetId = Shader.PropertyToID("_Wet");
        // ⚠️ A FEW PAPER BOATS RIDE IT, the owner's "keep the paper boats, fewer": which lane, where along the picture, its bob.
        private static readonly Vector3[] Boats = { new Vector3(3f, 0.64f, 0.3f), new Vector3(5f, 0.52f, 2.1f), new Vector3(2f, 0.42f, 4.4f) };
        private readonly Transform[] _boats = new Transform[Boats.Length];
        private readonly float[] _side = new float[Lanes];

        private readonly float _maxEnd;
        private readonly float[] _end = new float[Lanes], _z = new float[Lanes], _hit = new float[Lanes], _gone = new float[Lanes];
        private readonly MeshRenderer _bodyR, _poolR, _sprayR, _crestR, _shadowR, _wetR;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private readonly RafiBahaPuddles _puddles;

        /// <summary>`ends` are the nine lanes' ends along the cast, in metres, from the rule.</summary>
        public RafiBahaBody(Transform root, float[] ends)
        {
            BuildShared();
            for (int i = 0; i < Lanes; i++) { _end[i] = ends[i]; _maxEnd = Mathf.Max(_maxEnd, ends[i]); _side[i] = Mathf.Lerp(-RafiRules.BahaHalfWidth, RafiRules.BahaHalfWidth, i / 8f); }
            _maxEnd = Mathf.Max(0.5f, _maxEnd);
            _puddles = RafiBahaPuddles.Build(root, _end, RafiRules.BahaWarning + _maxEnd / RafiRules.BahaSpeed + RafiRules.BahaCarryTail * 0.5f, _puddlePaint);
            _wetR = Part(root, "wet", _pool, _wetPaint);
            _shadowR = Part(root, "shadow", _body, _shadowPaint);
            _poolR = Part(root, "pool", _pool, _poolPaint);
            _bodyR = Part(root, "body", _body, _bodyPaint);
            _sprayR = Part(root, "spray", _spray, _sprayPaint);
            _crestR = Part(root, "crest", _crest, _crestPaint);
            for (int b = 0; b < _boats.Length; b++) _boats[b] = Part(root, "paper-boat", _boat, _paperPaint).transform;
            float r = RafiRules.BahaHalfWidth;
            var reach = new Bounds(new Vector3(0f, 1.5f, _maxEnd * 0.5f), new Vector3(r * 4f, 6f, _maxEnd + 16f));
            _bodyR.localBounds = reach; _sprayR.localBounds = reach; _crestR.localBounds = reach; _shadowR.localBounds = reach;
        }

        private static MeshRenderer Part(Transform root, string name, Mesh mesh, Material paint)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = paint;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.enabled = name == "paper-boat";
            // An effect, not a prop: the ground queries and the prop checks pass over it.
            VfxRenderTag.Attach(go);
            return renderer;
        }

        private static void BuildShared()
        {
            if (_body != null && _bodyPaint != null) return;
            // ---- the body: a plain grid, x across and uv.y tail to front; the shader places every row.
            var v = new Vector3[Columns * Rows]; var uv = new Vector2[v.Length];
            for (int s = 0; s < Rows; s++)
                for (int c = 0; c < Columns; c++)
                {
                    float u = c / (Columns - 1f), q = s / (Rows - 1f);
                    v[s * Columns + c] = new Vector3(Mathf.Lerp(-Wide, Wide, u), 0f, 0f);
                    uv[s * Columns + c] = new Vector2(u, q);
                }
            var t = new int[(Columns - 1) * (Rows - 1) * 6]; int n = 0;
            for (int s = 0; s < Rows - 1; s++)
                for (int c = 0; c < Columns - 1; c++)
                {
                    int i = s * Columns + c;
                    t[n++] = i; t[n++] = i + Columns; t[n++] = i + 1; t[n++] = i + 1; t[n++] = i + Columns; t[n++] = i + Columns + 1;
                }
            _body = new Mesh { name = "IlyasBahaBody", hideFlags = HideFlags.HideAndDontSave };
            _body.vertices = v; _body.uv = uv; _body.triangles = t; _body.UploadMeshData(true);

            // ---- the crest: a tube along the front edge, its place and its radius all in the shader.
            var cv = new Vector3[Columns * Around]; var cuv = new Vector2[cv.Length];
            for (int s = 0; s < Around; s++)
                for (int c = 0; c < Columns; c++)
                {
                    cv[s * Columns + c] = new Vector3(Mathf.Lerp(-Wide * 0.95f, Wide * 0.95f, c / (Columns - 1f)), 0f, 0f);
                    cuv[s * Columns + c] = new Vector2(s / (Around - 1f), 0f);
                }
            var ct = new int[(Columns - 1) * (Around - 1) * 6]; n = 0;
            for (int s = 0; s < Around - 1; s++)
                for (int c = 0; c < Columns - 1; c++)
                {
                    int i = s * Columns + c;
                    ct[n++] = i; ct[n++] = i + Columns; ct[n++] = i + 1; ct[n++] = i + 1; ct[n++] = i + Columns; ct[n++] = i + Columns + 1;
                }
            _crest = new Mesh { name = "IlyasBahaCrest", hideFlags = HideFlags.HideAndDontSave };
            _crest.vertices = cv; _crest.uv = cuv; _crest.triangles = ct; _crest.UploadMeshData(true);
            _boat = PaperBoat();

            _pool = RafiWaterPaint.Keep(FxStudyKit.Square(true), "IlyasBahaPool", 0f);

            // ---- the spray: one quad a cluster. Vertex xy its corner, z its kind, uv its fixed seeds, so every peer agrees.
            var sv = new Vector3[Clusters * 4]; var seeds = new Vector4[sv.Length]; var st = new int[Clusters * 6];
            var random = new System.Random(5151);
            for (int d = 0; d < Clusters; d++)
            {
                bool lead = d < Lead;
                var seed = new Vector4(lead ? (d + (float)random.NextDouble()) / Lead : (d - Lead + 0.5f) / (Clusters - Lead), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
                for (int k = 0; k < 4; k++)
                {
                    sv[d * 4 + k] = new Vector3(k == 0 || k == 3 ? -1f : 1f, k < 2 ? -1f : 1f, lead ? 0f : 1f);
                    seeds[d * 4 + k] = seed;
                }
                st[d * 6] = d * 4; st[d * 6 + 1] = d * 4 + 2; st[d * 6 + 2] = d * 4 + 1; st[d * 6 + 3] = d * 4; st[d * 6 + 4] = d * 4 + 3; st[d * 6 + 5] = d * 4 + 2;
            }
            _spray = new Mesh { name = "IlyasBahaSpray", hideFlags = HideFlags.HideAndDontSave };
            _spray.vertices = sv; _spray.SetUVs(0, seeds); _spray.triangles = st; _spray.UploadMeshData(true);

            var shader = Resources.Load<Shader>("Shaders/RafiBahaBody");
            if (shader == null) return;
            Material Paint(int mode, int queue)
            {
                var m = new Material(shader) { name = "Ilyas Baha " + mode, hideFlags = HideFlags.HideAndDontSave, renderQueue = queue };
                m.SetFloat("_Mode", mode); m.SetFloat("_Radius", RafiRules.BahaHalfWidth); m.SetFloat("_High", High); m.SetFloat("_Length", BodyLength);
                m.SetTexture("_BodyTex", FxStudyKit.Mask("baha_body")); m.SetTexture("_SprayTex", FxStudyKit.Mask("baha_spray")); m.SetTexture("_NoiseTex", FxStudyKit.Mask("erode_cloud"));
                return m;
            }
            _wetPaint = Paint(5, 2996); _puddlePaint = Paint(7, 2997); _shadowPaint = Paint(4, 2998); _poolPaint = Paint(1, 2999); _bodyPaint = Paint(0, 3002); _crestPaint = Paint(3, 3004); _sprayPaint = Paint(2, 3006);
            _paperPaint = Paint(6, 3003); _paperPaint.SetFloat("_ZW", 1f);
        }

        /// <summary>
        /// A folded paper boat, 0.7 m long so it reads at play distance: from above a pointed hull with raised bow and stern,
        /// and the tent of its folded middle standing out of it. Flat faces, as paper is.
        /// </summary>
        private static Mesh PaperBoat()
        {
            var v = new System.Collections.Generic.List<Vector3>();
            void Tri(Vector3 a, Vector3 b, Vector3 c) { v.Add(a); v.Add(b); v.Add(c); }
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { Tri(a, b, c); Tri(a, c, d); }
            // From above: a long hull with square shoulders and two raised ends, not a diamond; down its middle the tall narrow
            // fold of the sail, a triangle along the hull.
            Vector3 bow = new Vector3(0f, 0.22f, 0.40f), stern = new Vector3(0f, 0.22f, -0.40f);
            Vector3 l1 = new Vector3(-0.15f, 0.12f, 0.20f), r1 = new Vector3(0.15f, 0.12f, 0.20f), l0 = new Vector3(-0.15f, 0.12f, -0.20f), r0 = new Vector3(0.15f, 0.12f, -0.20f);
            Vector3 k0 = new Vector3(0f, 0f, -0.22f), k1 = new Vector3(0f, 0f, 0.22f);
            Quad(k0, k1, l1, l0); Quad(k1, k0, r0, r1); Tri(k1, bow, l1); Tri(bow, k1, r1); Tri(k0, l0, stern); Tri(r0, k0, stern);
            Tri(l1, bow, r1); Tri(r0, stern, l0); Quad(l0, l1, r1, r0);
            Vector3 s0 = new Vector3(-0.045f, 0.12f, -0.19f), s1 = new Vector3(-0.045f, 0.12f, 0.19f), t0 = new Vector3(0.045f, 0.12f, -0.19f), t1 = new Vector3(0.045f, 0.12f, 0.19f), peak = new Vector3(0f, 0.40f, 0f);
            Tri(s0, s1, peak); Tri(t1, t0, peak); Tri(s1, t1, peak); Tri(t0, s0, peak);
            var normals = new Vector3[v.Count]; var tris = new int[v.Count];
            for (int i = 0; i < v.Count; i += 3)
            {
                var f = Vector3.Cross(v[i + 1] - v[i], v[i + 2] - v[i]).normalized;
                normals[i] = normals[i + 1] = normals[i + 2] = f; tris[i] = i; tris[i + 1] = i + 1; tris[i + 2] = i + 2;
            }
            var mesh = new Mesh { name = "IlyasBahaPaperBoat", hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = v.ToArray(); mesh.normals = normals; mesh.triangles = tris; mesh.RecalculateBounds(); mesh.UploadMeshData(true);
            return mesh;
        }

        // The shader's `heightAt`: the same sum, for what floats on it.
        private static float Height(float x, float q)
        {
            float half = 0.88f * Mathf.Pow(Mathf.Clamp01(Mathf.Min(q + 0.055f, 0.72f) / 0.72f), 0.6f), side = Mathf.Clamp01(Mathf.Abs(x * 1.06f) / (RafiRules.BahaHalfWidth * 1.06f));
            float e = 0.72f + 0.28f * Mathf.Sqrt(Mathf.Max(0f, 1f - side * side)), fall = 1f - Mathf.SmoothStep(0f, 1f, (q - (e - 0.10f)) / 0.088f);
            float prof = 0.03f + 0.34f * Mathf.SmoothStep(0f, 1f, (q - 0.26f) / 0.54f) + 0.63f * Mathf.SmoothStep(0f, 1f, (q - 0.70f) / 0.24f);
            float u = Mathf.Clamp01(Mathf.Abs(x) / (RafiRules.BahaHalfWidth * 1.06f) / Mathf.Max(0.08f, half));
            return prof * (1f - u * u * u * u) * fall;
        }

        /// <summary>Poses it `t` seconds after the cast (below 0 it is not there yet).</summary>
        public void Pose(float t)
        {
            _puddles?.Pose(t);
            float warn = RafiRules.BahaWarning, speed = RafiRules.BahaSpeed;
            float travel = Mathf.Clamp((t - warn) * speed, 0f, _maxEnd);
            float allStopped = warn + _maxEnd / speed;
            float drain = FxStudyKit.Ease(FxStudyKit.Run(t, allStopped + 0.05f, allStopped + RafiRules.BahaCarryTail));
            float rise = 0.5f * FxStudyKit.Ease(FxStudyKit.Run(t, 0f, warn)) + 0.5f * FxStudyKit.EaseOut(FxStudyKit.Run(t, warn, warn + 0.35f));
            float run = FxStudyKit.Ease(FxStudyKit.Run(t, warn, warn + 0.2f));
            for (int i = 0; i < Lanes; i++)
            {
                _z[i] = Mathf.Min(travel, _end[i]);
                float since = t - (warn + _end[i] / speed);
                _hit[i] = since >= 0f ? since : -1f;
                _gone[i] = _hit[i];
            }
            // What is torn away after a break: the stopped lanes, and an outermost lane cut off from the body by them, which
            // would otherwise run on as a thin separate tongue. Look only: the rule still sweeps that lane.
            if (_hit[7] >= 0f && _end[7] < _end[8]) _gone[8] = Mathf.Max(_gone[8], _hit[7]);
            if (_hit[1] >= 0f && _end[1] < _end[0]) _gone[0] = Mathf.Max(_gone[0], _hit[1]);
            bool on = t >= 0f && drain < 0.999f;
            foreach (var r in new[] { _bodyR, _poolR, _sprayR, _crestR, _shadowR, _wetR })
            {
                if (r.enabled != on) r.enabled = on;
                if (!on) continue;
                _block.Clear();
                _block.SetFloat(AgeId, Mathf.Max(0f, t)); _block.SetFloat(RiseId, rise); _block.SetFloat(RunId, run); _block.SetFloat(DrainId, drain); _block.SetFloat(WetId, 1f - drain);
                _block.SetFloatArray(LaneZId, _z); _block.SetFloatArray(LaneHitId, _hit); _block.SetFloatArray(LaneGoneId, _gone);
                r.SetPropertyBlock(_block);
            }
            // The pool lies under the body and trails it: from just behind the front back along it, a little wider.
            float len = Mathf.Min(BodyLength, travel + 0.3f);
            _poolR.transform.localPosition = new Vector3(0.5f, 0.008f, travel - len * 0.5f + 0.2f);
            _poolR.transform.localScale = new Vector3(Wide * 2f + 2.2f, 1f, len + 2.4f);
            // Where the water has been the road stays wet: from the caster's line to just behind the front.
            float wetTo = Mathf.Max(0.1f, travel - 0.6f);
            _wetR.transform.localPosition = new Vector3(0f, 0.005f, wetTo * 0.5f);
            _wetR.transform.localScale = new Vector3(Wide * 1.75f, 1f, wetTo);

            for (int b = 0; b < _boats.Length; b++)
            {
                var seat = Boats[b]; int lane = (int)seat.x; float q = seat.y;
                float front = _z[lane], length = Mathf.Min(BodyLength, front + 0.3f), z = front - (1f - q) * length + 0.35f;
                bool shown = t >= 0f && front > 2.6f && drain < 0.9f && _hit[lane] < 0.35f;
                if (_boats[b].gameObject.activeSelf != shown) _boats[b].gameObject.SetActive(shown);
                if (!shown) continue;
                // Floats on the water's own sum (the shader's `heightAt`), pitched with its slope, turning in the flow.
                float x = _side[lane] + 0.25f * Mathf.Sin(t * 0.8f + seat.z);
                float y = Height(x, q) * High * rise * (1f - drain), ahead = Height(x, q + 0.03f) * High * rise;
                _boats[b].localPosition = new Vector3(x, y + 0.015f + 0.018f * Mathf.Sin(t * 3.7f + seat.z) - 0.2f * drain, z);
                float pitch = -Mathf.Atan2(ahead - y, 0.03f * Mathf.Max(0.5f, length)) * Mathf.Rad2Deg;
                _boats[b].localRotation = Quaternion.Euler(pitch + 3f * Mathf.Sin(t * 3.1f + seat.z), 24f * Mathf.Sin(t * 1.1f + seat.z) + seat.z * 40f, 6f * Mathf.Sin(t * 2.6f + seat.z) + 30f * drain);
                _boats[b].localScale = Vector3.one * Mathf.Clamp01((front - 2.6f) * 1.5f + 0.1f);
            }
        }
    }
}
