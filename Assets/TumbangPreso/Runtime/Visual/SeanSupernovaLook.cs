using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    // SEAN'S SUPERNOVA, the live landing look (not the cutscene). Off until `Enabled` is turned on, so the game is
    // unchanged by this file alone.
    //
    // ⚠️ What it draws, all from ONE mesh built once for the process and ONE shared material, one draw per landing:
    //   * a BURST DOME of painted fire that swells to the blast radius in a third of a second and is eaten from the top;
    //   * a GROUND RING that runs out to exactly the rule's radius (5.4 m) so what is drawn is what is hit;
    //   * a CROWN OF CHUNKY TONGUES round the crater rim and a few inside it, each with its own height, phase and
    //     breathing, burning for the crater's life and then eroding in place. Nothing rotates or slides as one shape.
    // The fire is three crisp value bands (white-yellow, orange, deep red) over a picture panned at two speeds and
    // multiplied by itself, the same painting as `SeanFireThrow` at `_Stylise` 2.
    // ⚠️ GPU ONLY: per frame the only write is two floats through a property block. No mesh writes, no allocation.
    public sealed class SeanSupernovaLook : MonoBehaviour
    {
        public static bool Enabled = false;

        private static Mesh _mesh;
        private static Material _material;
        private static readonly int AgeId = Shader.PropertyToID("_Age");
        private static readonly int LifeId = Shader.PropertyToID("_Life");

        private MaterialPropertyBlock _block;
        private MeshRenderer _renderer;
        private float _age, _life;

        public static void Spawn(Vector3 ground, float radius, float life)
        {
            if (!Enabled) return;
            if (_material == null)
            {
                var shader = Shader.Find("TumbangPreso/SeanSupernova");
                if (shader == null) return;
                _material = new Material(shader) { name = "SeanSupernova (shared)" };
            }
            if (_mesh == null) _mesh = BuildMesh();

            var go = new GameObject("SeanSupernovaLook");
            go.transform.position = ground + Vector3.up * .02f;
            go.transform.localScale = Vector3.one * radius;
            go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var look = go.AddComponent<SeanSupernovaLook>();
            look._renderer = go.AddComponent<MeshRenderer>();
            look._renderer.sharedMaterial = _material;
            look._renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            look._renderer.receiveShadows = false;
            look._block = new MaterialPropertyBlock();
            look._life = Mathf.Max(1f, life);
            look.Push();
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age > _life + .6f) { Destroy(gameObject); return; }
            Push();
        }

        private void Push()
        {
            _block.SetFloat(AgeId, _age);
            _block.SetFloat(LifeId, _life);
            _renderer.SetPropertyBlock(_block);
        }

        // Vertex colour: r = part (0 dome, .5 ring, 1 tongue), g = piece seed, b = piece height.
        // Object space is unit radius; the transform's scale is the rule's radius.
        private static Mesh BuildMesh()
        {
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var c = new List<Color>();
            var t = new List<int>();

            // Dome: a low hemisphere, 24 x 8.
            const int seg = 24, rings = 8;
            int b0 = v.Count;
            for (int y = 0; y <= rings; y++)
            for (int x = 0; x <= seg; x++)
            {
                float a = x / (float)seg * Mathf.PI * 2f, e = y / (float)rings * Mathf.PI * .5f;
                v.Add(new Vector3(Mathf.Cos(a) * Mathf.Cos(e), Mathf.Sin(e) * .55f, Mathf.Sin(a) * Mathf.Cos(e)));
                uv.Add(new Vector2(x / (float)seg, y / (float)rings));
                c.Add(new Color(0, 0, 1, 1));
            }
            for (int y = 0; y < rings; y++)
            for (int x = 0; x < seg; x++)
            {
                int i = b0 + y * (seg + 1) + x;
                t.Add(i); t.Add(i + seg + 1); t.Add(i + 1);
                t.Add(i + 1); t.Add(i + seg + 1); t.Add(i + seg + 2);
            }

            // Ground ring: an annulus from .80 to 1.06 of the radius, lifted a hair.
            b0 = v.Count;
            for (int x = 0; x <= 48; x++)
            {
                float a = x / 48f * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v.Add(d * .80f + Vector3.up * .01f); uv.Add(new Vector2(x / 48f * 6f, 0)); c.Add(new Color(.5f, 0, 1, 1));
                v.Add(d * 1.06f + Vector3.up * .01f); uv.Add(new Vector2(x / 48f * 6f, 1)); c.Add(new Color(.5f, 0, 1, 1));
            }
            for (int x = 0; x < 48; x++)
            {
                int i = b0 + x * 2;
                t.Add(i); t.Add(i + 1); t.Add(i + 2);
                t.Add(i + 2); t.Add(i + 1); t.Add(i + 3);
            }

            // Tongues: two crossed cards each, 14 on the rim and 7 inside. Seeded, so every landing has the same set.
            var rng = new System.Random(5401);
            for (int k = 0; k < 21; k++)
            {
                bool rim = k < 14;
                float a = rim ? (k + (float)rng.NextDouble() * .6f) / 14f * Mathf.PI * 2f : (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = rim ? .78f + (float)rng.NextDouble() * .14f : .15f + (float)rng.NextDouble() * .5f;
                var p = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                float w = .10f + (float)rng.NextDouble() * .07f;
                float h = .28f + (float)rng.NextDouble() * .22f;
                float seed = (float)rng.NextDouble();
                for (int card = 0; card < 2; card++)
                {
                    float ca = a + card * Mathf.PI * .5f;
                    var side = new Vector3(-Mathf.Sin(ca), 0, Mathf.Cos(ca)) * w;
                    int i = v.Count;
                    v.Add(p - side); v.Add(p + side); v.Add(p - side * .6f + Vector3.up * h); v.Add(p + side * .6f + Vector3.up * h);
                    uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(0, 1)); uv.Add(new Vector2(1, 1));
                    var col = new Color(1, seed, h, 1);
                    c.Add(col); c.Add(col); c.Add(col); c.Add(col);
                    t.Add(i); t.Add(i + 2); t.Add(i + 1);
                    t.Add(i + 1); t.Add(i + 2); t.Add(i + 3);
                }
            }

            var m = new Mesh { name = "SeanSupernova (built once)" };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetColors(c); m.SetTriangles(t, 0);
            // Tongues grow and the dome swells in the shader, so the bounds cover the tallest pose.
            m.bounds = new Bounds(Vector3.up * .5f, new Vector3(2.4f, 1.6f, 2.4f));
            m.UploadMeshData(true);
            return m;
        }
    }
}
