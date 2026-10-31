using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// THE SHAPE PHAISTER LEAVES BEHIND WHEN SHE TELEPORTS (`PhaisterLook` directions A and B). Her pose is baked ONCE, at
    /// the release, into one mesh in world space; `PhaisterGhost.shader` does everything after that from the effect's clock:
    /// A draws it toward the destination and lets it drip away (Pataya's RTVFX Sketch #35 "Teleport": the body is pulled to
    /// the target and the pose it leaves behind drips and erodes), B twists it down into its pool.
    ///
    /// ⚠️ ONE BAKE, NEVER PER FRAME. `VoodooSoulDraw` bakes a body every frame and its cost scales with the body; this is one
    /// bake at a cast and then a static mesh. Cost: one renderer, two passes (depth, then colour), her own triangle count.
    /// </summary>
    internal sealed class PhaisterGhostBody
    {
        public readonly GameObject Go;
        private readonly Material _ink;
        private readonly Mesh _mesh;

        /// <summary>
        /// The ghost of <paramref name="model"/> as it stands now, standing on <paramref name="ground"/>, heading for
        /// <paramref name="target"/>. Null when there is nothing drawn to bake.
        /// </summary>
        public static PhaisterGhostBody Bake(Transform model, Transform parent, Vector3 ground, Vector3 target, int mode)
        {
            if (model == null) return null;
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var indices = new List<int>();
            var scratch = new Mesh();
            float top = ground.y + 0.5f;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (r == null || !r.enabled || r.GetComponent<VfxRenderTag>() != null) continue;
                Mesh source; Matrix4x4 toWorld;
                if (r is SkinnedMeshRenderer skinned)
                {
                    if (skinned.sharedMesh == null) continue;
                    skinned.BakeMesh(scratch, true);
                    source = scratch;
                    toWorld = Matrix4x4.TRS(skinned.transform.position, skinned.transform.rotation, Vector3.one);
                }
                else if (r is MeshRenderer)
                {
                    var filter = r.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable) continue;
                    source = filter.sharedMesh;
                    toWorld = r.transform.localToWorldMatrix;
                }
                else continue;

                int at = vertices.Count;
                var sv = source.vertices; var sn = source.normals;
                for (int i = 0; i < sv.Length; i++)
                {
                    var w = toWorld.MultiplyPoint3x4(sv[i]);
                    vertices.Add(w);
                    normals.Add(sn.Length == sv.Length ? toWorld.MultiplyVector(sn[i]).normalized : Vector3.up);
                    if (w.y > top) top = w.y;
                }
                for (int s = 0; s < source.subMeshCount; s++)
                    foreach (int i in source.GetTriangles(s)) indices.Add(at + i);
            }
            PhaisterLook.Free(scratch);
            if (indices.Count == 0) return null;
            return new PhaisterGhostBody(parent, vertices, normals, indices, ground, target, top - ground.y, mode);
        }

        private PhaisterGhostBody(Transform parent, List<Vector3> v, List<Vector3> n, List<int> t, Vector3 ground, Vector3 target, float height, int mode)
        {
            _mesh = new Mesh { name = "PhaisterGhost", indexFormat = v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            _mesh.SetVertices(v); _mesh.SetNormals(n); _mesh.SetTriangles(t, 0);
            // Placed in the world by its own vertices, and it moves: never let the camera cull it.
            _mesh.bounds = new Bounds(ground, Vector3.one * 60f);
            Go = PhaisterLook.Draw("PhaisterGhost", parent, _mesh);
            _ink = PhaisterLook.Paint(Go, "PhaisterGhost");
            if (_ink == null) return;
            _ink.SetFloat("_Mode", mode);
            _ink.SetVector("_Base", new Vector4(ground.x, ground.y, ground.z, Mathf.Max(0.5f, height)));
            _ink.SetVector("_Target", target);
        }

        public void StepTo(float seconds) { if (_ink != null) _ink.SetFloat("_Age", seconds); }

        /// <summary>The baked mesh is this ghost's alone; free it with the effect.</summary>
        public void Free() { PhaisterLook.Free(_mesh); }

        public int Triangles => _mesh != null ? (int)(_mesh.GetIndexCount(0) / 3) : 0;
    }
}
