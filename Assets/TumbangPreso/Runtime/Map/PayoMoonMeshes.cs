using System;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// BUWAN'S AND MAYARI'S MESHES (`PayoMoon`), read once from the baked files `tools/payo_pieces/moon_cute.py unity`
    /// writes to `Resources/Map/PayoMoon/` and shared. Nothing here is touched again: every moving part is moved in
    /// `PayoMoon.shader` and `PayoMoonSky.shader` from the numbers baked per vertex (their headers say which).
    /// File (the Kanlit format): "KNL1", int vertices, int indices, float3 position, float3 normal, float2 uv, float4 A,
    /// float4 B, int indices.
    /// </summary>
    public static class PayoMoonMeshes
    {
        public const string Folder = "Map/PayoMoon/";

        private static Mesh _moon, _goddess, _meteors, _comet, _burst, _glow;
        private static Texture2D _atlas;

        // `grow`: how far (in the mesh's units, moon radii) the shader may move a vertex past the baked bounds.
        public static Mesh Moon => _moon != null ? _moon : (_moon = Load("moon", 1.5f));
        public static Mesh Goddess => _goddess != null ? _goddess : (_goddess = Load("goddess", 0.6f));
        public static Mesh Meteors => _meteors != null ? _meteors : (_meteors = Load("meteors", 7.0f));
        public static Mesh Comet => _comet != null ? _comet : (_comet = Load("comet", 0.5f));
        public static Mesh Burst => _burst != null ? _burst : (_burst = Load("burst", 1.6f));
        public static Mesh Glow => _glow != null ? _glow : (_glow = Load("glow", 6.0f));
        public static Texture2D Atlas => _atlas != null ? _atlas : (_atlas = Resources.Load<Texture2D>(Folder + "moon_atlas"));

        private static Mesh Load(string name, float grow)
        {
            var file = Resources.Load<TextAsset>(Folder + name);
            if (file == null) { Debug.LogWarning("[Payo] Resources/" + Folder + name + ".bytes is missing (tools/payo_pieces/moon_cute.py unity)"); return null; }
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
            var mesh = new Mesh { name = "Payo moon " + name + " (baked)", hideFlags = HideFlags.DontSave };
            if (v > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(P); mesh.SetNormals(N); mesh.SetUVs(0, U); mesh.SetUVs(1, A); mesh.SetUVs(2, B);
            mesh.SetTriangles(tris, 0, true);
            var bounds = mesh.bounds; bounds.Expand(grow * 2f); mesh.bounds = bounds;
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
