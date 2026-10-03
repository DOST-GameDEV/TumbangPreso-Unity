using System.Collections.Generic;
using System.IO;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// AMIHAN'S CUTSCENE FACES, baked into her own head mesh (owner, 2026-10-03, on the overlay card: *"i like the idea but u
    /// did bad implementation bcz theres a box on the face of amihan"*). A card laid over her face never shades like the face
    /// under it. So each look is a copy of `head-mesh` itself: the old eye and mouth blocks are re-coloured to her own skin
    /// texel (they stand only 2.6 mm proud, so they vanish into the face), and the look's new blocks are added in the same
    /// layer with the same ink texel, normals and head weight. Same geometry, same normals, same material: nothing to see
    /// but the expression. `HeroIntroductionScene.Amihan.cs` swaps the copied body's head mesh to these.
    ///
    /// Run: Unity -batchmode -executeMethod TumbangPreso.EditorTools.AmihanFaceAuthor.BakeFromCommandLine
    /// </summary>
    public static class AmihanFaceAuthor
    {
        private const string Folder = "Assets/TumbangPreso/Resources/" + VoxelFace.ResourceFolder;

        public static void BakeFromCommandLine()
        {
            Bake();
            EditorApplication.Exit(0);
        }

        [MenuItem("Tumbang Preso/Amihan/Bake cutscene faces")]
        public static void Bake()
        {
            var model = RosterBook.Load().FindPersonArt("amihan")?.Model;
            SkinnedMeshRenderer head = null;
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (skin.sharedMesh != null && skin.sharedMesh.name.Contains("head")) head = skin;
            if (head == null) throw new System.InvalidOperationException("Amihan has no head-mesh.");
            var source = head.sharedMesh;
            var vertices = source.vertices; var normals = source.normals; var uv = source.uv; var weights = source.boneWeights;

            // The face: the side whose centre carries two front-facing layers (the skin plane and the blocks proud of it).
            foreach (float sign in new[] { 1f, -1f })
            {
                var front = new Vector3(0, 0, sign);
                float skinD = float.MaxValue, inkD = float.MinValue; Vector2 skinUv = default, inkUv = default;
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (Vector3.Dot(normals[i], front) < .9f || Mathf.Abs(vertices[i].x) > .11f) continue;
                    float d = Vector3.Dot(vertices[i], front);
                    if (d < skinD) { skinD = d; skinUv = uv[i]; }
                    if (d > inkD) { inkD = d; inkUv = uv[i]; }
                }
                if (inkD - skinD < .001f || (inkUv - skinUv).sqrMagnitude < 1e-6f) continue;
                // The old blocks' extent (their front faces).
                float xMin = float.MaxValue, xMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue;
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (Vector3.Dot(normals[i], front) < .9f || Mathf.Abs(vertices[i].x) > .11f) continue;
                    if (Vector3.Dot(vertices[i], front) < inkD - .0008f) continue;
                    xMin = Mathf.Min(xMin, vertices[i].x); xMax = Mathf.Max(xMax, vertices[i].x);
                    yMin = Mathf.Min(yMin, vertices[i].y); yMax = Mathf.Max(yMax, vertices[i].y);
                }
                // Every vertex of the old blocks (front and sides): ink-coloured, inside their extent, in front of the skin plane.
                var old = new List<int>();
                for (int i = 0; i < vertices.Length; i++)
                    if ((uv[i] - inkUv).sqrMagnitude < 1e-6f && vertices[i].x >= xMin - .002f && vertices[i].x <= xMax + .002f
                        && vertices[i].y >= yMin - .002f && vertices[i].y <= yMax + .002f && Vector3.Dot(vertices[i], front) > skinD - .0005f)
                        old.Add(i);
                // A head weight to copy, and the winding the mesh uses for a front-facing quad.
                int sample = old.Count > 0 ? old[0] : 0;
                Directory.CreateDirectory(Folder);
                foreach (VoxelFace.Look look in System.Enum.GetValues(typeof(VoxelFace.Look)))
                {
                    if (look == VoxelFace.Look.Rest) continue;
                    var mesh = Object.Instantiate(source);
                    mesh.name = VoxelFace.MeshName(look);
                    // ⚠️ THE VERTEX COUNT NEVER CHANGES (film v4 r9: a head mesh with more vertices than the one the skinned
                    // renderer was built for stopped rendering, "does not match the expected mesh data size"). The old blocks'
                    // own vertices are reused as the new blocks' corners; their old triangles are dropped and the new ones laid.
                    var v = (Vector3[])vertices.Clone(); var n = (Vector3[])normals.Clone(); var t = (Vector2[])uv.Clone();
                    var gone = new HashSet<int>(old);
                    var source0 = source.GetTriangles(0);
                    var tris = new List<int>(source0.Length);
                    for (int k = 0; k < source0.Length; k += 3)
                        if (!gone.Contains(source0[k]) && !gone.Contains(source0[k + 1]) && !gone.Contains(source0[k + 2]))
                            tris.AddRange(new[] { source0[k], source0[k + 1], source0[k + 2] });
                    float cx = (xMin + xMax) * .5f;
                    var rows = VoxelFace.Blocks[look];
                    if (rows.GetLength(0) * 4 > old.Count) throw new System.InvalidOperationException(look + " needs more corners than the old blocks have.");
                    for (int r = 0; r < rows.GetLength(0); r++)
                    {
                        int[] at = { old[r * 4], old[r * 4 + 1], old[r * 4 + 2], old[r * 4 + 3] };
                        var corners = new[] { (rows[r, 0], rows[r, 2]), (rows[r, 0], rows[r, 3]), (rows[r, 1], rows[r, 3]), (rows[r, 1], rows[r, 2]) };
                        for (int c = 0; c < 4; c++)
                        {
                            v[at[c]] = new Vector3(cx + corners[c].Item1, yMin + corners[c].Item2, 0) + front * inkD;
                            n[at[c]] = front; t[at[c]] = inkUv;
                        }
                        // Wound so the quad faces out along `front` (a triangle's front is where Cross(b - a, c - a) points).
                        var normal = Vector3.Cross(v[at[1]] - v[at[0]], v[at[2]] - v[at[0]]);
                        if (Vector3.Dot(normal, front) > 0) tris.AddRange(new[] { at[0], at[1], at[2], at[0], at[2], at[3] });
                        else tris.AddRange(new[] { at[0], at[2], at[1], at[0], at[3], at[2] });
                    }
                    // The old corners not reused are no longer drawn; give them her skin anyway.
                    for (int k = rows.GetLength(0) * 4; k < old.Count; k++) t[old[k]] = skinUv;
                    mesh.vertices = v; mesh.normals = n; mesh.uv = t;
                    mesh.SetTriangles(tris, 0);
                    mesh.RecalculateBounds();
                    string path = Folder + "/" + mesh.name + ".asset";
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.CreateAsset(mesh, path);
                    Debug.LogWarning($"[AmihanFaceAuthor] {path}: {old.Count} old block vertices re-skinned, {rows.GetLength(0)} new blocks");
                }
                AssetDatabase.SaveAssets();
                return;
            }
            throw new System.InvalidOperationException("Amihan's face layers were not found.");
        }
    }
}
