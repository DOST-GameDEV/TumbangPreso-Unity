using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ NEMU'S GATE (LET THEM IN v10, 2026-10-14, `docs/reports/nemu-ultimate-2026-10-11/direction.md` section 19). The owner on
    /// v9: *"siguro wag na floor cracks"* and *"js show some sort of otherwordly portal somewhere where the ghosts come from"*. A
    /// tear in the air over the court (`Shaders/NemuPortal`), the same in the cutscene (`HeroIntroductionScene.Nemu.cs`, which
    /// tears it open and pours the souls out of it) and in play (`NemuWorld`, which keeps it open while anyone is possessed).
    /// One quad that always faces the lens; its centre is the parent's point <see cref="At"/>.
    /// </summary>
    public sealed class NemuPortal
    {
        private readonly GameObject _go;
        private readonly Material _material;
        private readonly Mesh _mesh;

        /// <summary>Its centre, in the parent's space.</summary>
        public Vector3 At { get; private set; }
        /// <summary>How tall it is when open (metres).</summary>
        public float Size { get; }

        public NemuPortal(Transform parent, Vector3 at, float size)
        {
            At = at; Size = size;
            var shader = Resources.Load<Shader>("Shaders/NemuPortal");
            if (shader == null || parent == null) return;
            _mesh = new Mesh { name = "NemuPortal" };
            _mesh.SetVertices(new[] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero });
            _mesh.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
            _mesh.SetTriangles(new[] { 0, 2, 1, 1, 2, 3 }, 0);
            // The corners are laid out in the vertex stage: the bounds must hold the whole gate whichever way it is seen from.
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * size * 1.6f);
            _material = new Material(shader) { name = "NemuPortal" };
            _material.SetFloat("_Size", size * 1.5f);
            _go = new GameObject("NemuPortal");
            _go.transform.SetParent(parent, false);
            _go.transform.localPosition = at;
            _go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var r = _go.AddComponent<MeshRenderer>();
            r.sharedMaterial = _material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            VfxRenderTag.Own(_go, _material);
            _go.SetActive(false);
        }

        public void Move(Vector3 at)
        {
            At = at;
            if (_go != null) _go.transform.localPosition = at;
        }

        /// <summary>How far open (0 a slit, 1 whole), how much it shows, how hard it flares, and the clock for its churn.</summary>
        public void Set(float open, float fade, float surge, float clock)
        {
            if (_material == null) return;
            _material.SetFloat("_Open", Mathf.Clamp01(open));
            _material.SetFloat("_Fade", Mathf.Clamp01(fade));
            _material.SetFloat("_Surge", Mathf.Clamp01(surge));
            _material.SetFloat("_Clock", clock);
            if (_go != null) _go.SetActive(fade > .005f && open > .001f);
        }

        public void Dispose()
        {
            Destroy(_go); Destroy(_material); Destroy(_mesh);
        }

        private static void Destroy(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }
    }
}
