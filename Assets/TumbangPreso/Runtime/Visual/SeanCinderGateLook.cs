using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // ⚠️ CINDER GATE, QUICK PAINTED REWORK. Off until Enabled is set, so the shipped seam is unchanged.
    // One static mesh for the process (a ground seam plus a row of crossed flame tongues along the exact
    // three-metre line), one shared material, one draw per gate. Every motion lives in SeanCinderWall.shader
    // and reads only the field age, so live, late join and replay agree. The old seam renderers are
    // switched off, never deleted, so its own timeline keeps running untouched.
    public sealed class SeanCinderGateLook : MonoBehaviour
    {
        public static bool Enabled = false;
        private const int Tongues = 13;
        private static Mesh _mesh;
        private static Material _material;
        private static readonly int AgeId = Shader.PropertyToID("_Age");
        private static readonly int SplitId = Shader.PropertyToID("_SplitAge");
        private SeanCinderGate _gate;
        private Renderer _renderer;
        private MaterialPropertyBlock _block;

        public static void Attach(SeanCinderGate gate)
        {
            if (gate == null || gate.GetComponent<SeanCinderGateLook>() != null) return;
            var shader = Shader.Find("TumbangPreso/SeanCinderWall");
            if (shader == null) return;
            foreach (var old in gate.GetComponentsInChildren<Renderer>()) old.enabled = false;
            var look = gate.gameObject.AddComponent<SeanCinderGateLook>(); look._gate = gate;
            if (_material == null)
            {
                _material = new Material(shader) { name = "Cinder wall (shared)" };
                _material.SetTexture("_ErodeTex", Resources.Load<Texture2D>("Textures/UltimateFx/erode_cloud"));
            }
            var state = gate.Capture();
            var go = new GameObject("Cinder painted wall"); go.transform.SetParent(gate.transform, false);
            go.transform.SetPositionAndRotation(state.Position + Vector3.up * .016f, Quaternion.LookRotation(state.Forward));
            go.AddComponent<MeshFilter>().sharedMesh = SharedMesh();
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = _material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            look._renderer = r; look._block = new MaterialPropertyBlock();
            look.LateUpdate();
        }

        private void LateUpdate()
        {
            if (_gate == null || _renderer == null) return;
            var state = _gate.Capture();
            float age = state.Duration - _gate.Remaining;
            _renderer.GetPropertyBlock(_block);
            _block.SetFloat(AgeId, age);
            _block.SetFloat(SplitId, state.Split ? state.FirstScale : 10000f);
            _renderer.SetPropertyBlock(_block);
        }

        // uv: x across the piece, y up it (the seam: x along the line, y across it).
        // uv2: x seed, y kind (0 seam, 1 tongue), z full height (m), w phase.
        private static Mesh SharedMesh()
        {
            if (_mesh != null) return _mesh;
            int quads = 1 + Tongues * 2;
            var v = new Vector3[quads * 4]; var uv = new Vector2[quads * 4]; var uv2 = new Vector4[quads * 4];
            var tri = new int[quads * 6];
            float w = SeanGateRules.HalfWidth;
            int q = 0;
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector4 data)
            {
                int o = q * 4; v[o] = a; v[o + 1] = b; v[o + 2] = c; v[o + 3] = d;
                uv[o] = new Vector2(0, 0); uv[o + 1] = new Vector2(1, 0); uv[o + 2] = new Vector2(1, 1); uv[o + 3] = new Vector2(0, 1);
                for (int i = 0; i < 4; i++) uv2[o + i] = data;
                int t = q * 6; tri[t] = o; tri[t + 1] = o + 2; tri[t + 2] = o + 1; tri[t + 3] = o; tri[t + 4] = o + 3; tri[t + 5] = o + 2;
                q++;
            }
            // Ground seam at exactly the rule width, so the drawn line is the hit line.
            Quad(new Vector3(-w, 0, -.28f), new Vector3(-w, 0, .28f), new Vector3(w, 0, .28f), new Vector3(w, 0, -.28f), new Vector4(.37f, 0, 0, 0));
            uv[0] = new Vector2(0, 0); uv[1] = new Vector2(0, 1); uv[2] = new Vector2(1, 1); uv[3] = new Vector2(1, 0);
            for (int i = 0; i < Tongues; i++)
            {
                float s = (i * 0.618034f) % 1f;
                float x = Mathf.Lerp(-w + .14f, w - .14f, i / (float)(Tongues - 1)) + (s - .5f) * .08f;
                float h = (.42f + .38f * ((i * 0.381966f + .2f) % 1f)) * (i % 3 == 1 ? 1.25f : 1f);
                float half = .17f + .06f * s;
                var data = new Vector4(s, 1, h, i * 1.7f);
                var p = new Vector3(x, 0, 0);
                Quad(p + new Vector3(-half, 0, 0), p + new Vector3(half, 0, 0), p + new Vector3(half, 1, 0), p + new Vector3(-half, 1, 0), data);
                Quad(p + new Vector3(0, 0, -half), p + new Vector3(0, 0, half), p + new Vector3(0, 1, half), p + new Vector3(0, 1, -half), data);
            }
            _mesh = new Mesh { name = "Cinder wall (shared)" };
            _mesh.vertices = v; _mesh.uv = uv; _mesh.SetUVs(1, uv2); _mesh.triangles = tri;
            _mesh.bounds = new Bounds(new Vector3(0, .6f, 0), new Vector3(w * 2 + .6f, 1.4f, .8f));
            return _mesh;
        }
    }
}
