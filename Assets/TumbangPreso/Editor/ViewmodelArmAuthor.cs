using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TumbangPreso.CameraSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.EditorTools
{
    // Bake the actual sleeve, hand and accessory vertices into the first-person
    // arm's existing coordinate frame. No second costume transcription to drift.
    public static class ViewmodelArmAuthor
    {
        public const string Folder = "Assets/TumbangPreso/Resources/Models/RosterArms";
        public static void Run()
        {
            Bake(RosterBook.Load());
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }

        public static void Bake(RosterBook book)
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            int count = 0;
            foreach (var entry in book.People.Where(e => e != null && e.Model != null))
                foreach (string side in new[] { "right", "left" })
                {
                    Mesh generated = Extract(entry.Model, "arm-" + side);
                    if (generated == null) throw new InvalidOperationException(entry.Id + " has no " + side + " arm geometry");
                    string path = Folder + "/" + entry.Id + "_" + side + ".asset";
                    var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (existing == null) AssetDatabase.CreateAsset(generated, path);
                    else
                    {
                        EditorUtility.CopySerialized(generated, existing);
                        UnityEngine.Object.DestroyImmediate(generated);
                        EditorUtility.SetDirty(existing);
                    }
                    count++;
                }
            Debug.Log("[RosterArms] Baked " + count + " arms from the live roster meshes.");
        }

        public static Mesh Extract(GameObject model, string boneName)
        {
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh source = renderer.sharedMesh;
                if (source == null) continue;
                int bone = Array.FindIndex(renderer.bones, b => b != null && b.name == boneName);
                if (bone < 0) continue;
                var weights = source.boneWeights;
                var vertices = source.vertices;
                var sourceNormals = source.normals;
                var sourceUv = source.uv;
                Matrix4x4 bind = source.bindposes[bone];
                var remap = new Dictionary<int,int>();
                bool Owned(int i)
                {
                    BoneWeight w = weights[i];
                    float weight = (w.boneIndex0 == bone ? w.weight0 : 0)
                        + (w.boneIndex1 == bone ? w.weight1 : 0)
                        + (w.boneIndex2 == bone ? w.weight2 : 0)
                        + (w.boneIndex3 == bone ? w.weight3 : 0);
                    return weight > .99f;
                }
                var indices = source.triangles;
                for (int t = 0; t < indices.Length; t += 3)
                {
                    if (!Owned(indices[t]) || !Owned(indices[t+1]) || !Owned(indices[t+2])) continue;
                    for (int k = 0; k < 3; k++)
                    {
                        int original = indices[t+k];
                        if (!remap.TryGetValue(original,out int index))
                        {
                            index = positions.Count; remap.Add(original,index);
                            positions.Add(bind.MultiplyPoint3x4(vertices[original]));
                            normals.Add(bind.MultiplyVector(sourceNormals[original]).normalized);
                            uv.Add(sourceUv[original]);
                        }
                        triangles.Add(index);
                    }
                }
            }
            if (positions.Count == 0) return null;
            float side = Mathf.Sign(positions.Average(p => p.x));
            float first = positions.Min(p => p.x * side);
            float last = positions.Max(p => p.x * side);
            float scale = ViewmodelArms.ArmLength / (last - first);
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 p = positions[i], n = normals[i];
                // +Y reaches the first-person hand; -Z is its upper surface. Two
                // sign changes preserve winding for both left and right source arms.
                positions[i] = new Vector3(-side*p.z, side*p.x-first, -p.y) * scale;
                normals[i] = new Vector3(-side*n.z,side*n.x,-n.y).normalized;
            }
            var mesh = new Mesh { name = model.name + "_" + boneName,
                indexFormat = positions.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(positions); mesh.SetNormals(normals); mesh.SetUVs(0,uv);
            mesh.SetTriangles(triangles,0); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
