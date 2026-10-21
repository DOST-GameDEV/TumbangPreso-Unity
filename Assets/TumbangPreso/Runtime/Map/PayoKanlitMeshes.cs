using System;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// KANLIT'S MESHES (`PayoKanlit`), read once from the baked files `tools/payo_pieces/catcher_cute.py unity` writes to
    /// `Resources/Map/PayoCatcher/` and shared by every catch. Nothing here is touched again: every moving part is moved
    /// in `PayoKanlit.shader` and `PayoKanlitVine.shader` from the numbers baked per vertex (their headers say which).
    /// File: "KNL1", int vertices, int indices, float3 position, float3 normal, float2 uv, float4 A, float4 B, int indices.
    /// </summary>
    public static class PayoKanlitMeshes
    {
        public const string Folder = "Map/PayoCatcher/";
        /// <summary>The top of the vine's lead (where the buddy's paws hold it), over the body's feet, +z its front.</summary>
        public static readonly Vector3 Lead = new Vector3(0f, 2.35f, -0.05f);

        private static Mesh _buddy, _vine;
        private static Texture2D _atlas;

        public static Mesh Buddy => _buddy != null ? _buddy : (_buddy = Load("kanlit", 0.6f));
        public static Mesh Vine => _vine != null ? _vine : (_vine = Load("kanlit_vine", 3.5f));
        public static Texture2D Atlas => _atlas != null ? _atlas : (_atlas = Resources.Load<Texture2D>(Folder + "kanlit_atlas"));

        // `grow`: how far the shader may move a vertex past the baked bounds (a flower flying off, a spinning leaf).
        private static Mesh Load(string name, float grow)
        {
            var file = Resources.Load<TextAsset>(Folder + name);
            if (file == null) return null;
            byte[] data = file.bytes;
            if (data.Length < 12 || data[0] != 'K' || data[1] != 'N' || data[2] != 'L' || data[3] != '1') { Debug.LogWarning("[Payo] " + name + " is not a KNL1 file"); return null; }
            int v = BitConverter.ToInt32(data, 4), i = BitConverter.ToInt32(data, 8), at = 12;
            float[] Floats(int count) { var f = new float[count]; Buffer.BlockCopy(data, at, f, 0, count * 4); at += count * 4; return f; }
            float[] pos = Floats(v * 3), nrm = Floats(v * 3), uv = Floats(v * 2), a = Floats(v * 4), b = Floats(v * 4);
            var tris = new int[i];
            Buffer.BlockCopy(data, at, tris, 0, i * 4);
            var P = new Vector3[v]; var N = new Vector3[v]; var U = new Vector2[v]; var A = new Vector4[v]; var B = new Vector4[v];
            for (int k = 0; k < v; k++)
            {
                P[k] = new Vector3(pos[k * 3], pos[k * 3 + 1], pos[k * 3 + 2]);
                N[k] = new Vector3(nrm[k * 3], nrm[k * 3 + 1], nrm[k * 3 + 2]);
                U[k] = new Vector2(uv[k * 2], uv[k * 2 + 1]);
                A[k] = new Vector4(a[k * 4], a[k * 4 + 1], a[k * 4 + 2], a[k * 4 + 3]);
                B[k] = new Vector4(b[k * 4], b[k * 4 + 1], b[k * 4 + 2], b[k * 4 + 3]);
            }
            var mesh = new Mesh { name = "Payo " + name + " (baked)", hideFlags = HideFlags.DontSave };
            if (v > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(P); mesh.SetNormals(N); mesh.SetUVs(0, U); mesh.SetUVs(1, A); mesh.SetUVs(2, B);
            mesh.SetTriangles(tris, 0, true);
            var bounds = mesh.bounds; bounds.Expand(grow * 2f); mesh.bounds = bounds;
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
