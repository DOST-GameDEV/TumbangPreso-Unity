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

        public static void Bake(RosterBook book, string onlyId = null)
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            int count = 0;
            foreach (var entry in book.People.Where(e => e != null && e.Model != null && (onlyId == null || e.Id == onlyId)))
                foreach (string side in new[] { "right", "left" })
                {
                    // From the arms' own model where the entry has one (`RosterBookBuilder.FirstPersonArmModels`).
                    Mesh generated = Extract(entry.ArmModel != null ? entry.ArmModel : entry.Model, "arm-" + side);
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

        /// <summary>How wide a redesigned hero's first-person fist is, across its larger side: Dante's, the arm the owner
        /// chose the placement with (his body fist is 0.116 across, at the 4.375 his arm was first scaled by).</summary>
        public const float NaturalFist = 0.507f;

        public static Mesh Extract(GameObject model, string boneName)
        {
            bool hasForearm = false;
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
                // ⚠️ THE CUT FOLLOWS THE ELBOW. A redesigned hero has `forearm-<side>` under `arm-<side>` and
                // the hand is skinned to it, so cutting by the arm bone alone took only the upper arm (owner,
                // 2026-10-05, of Paete: "now paete's hands are weird.."). The forearm's vertices are taken
                // too, in the arm bone's bind space, where the arm is straight. -1 on a seven-bone rig.
                int fore = Array.FindIndex(renderer.bones, b => b != null && b.name == "fore" + boneName);
                hasForearm |= fore >= 0;
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
                    if (fore >= 0)
                        weight += (w.boneIndex0 == fore ? w.weight0 : 0) + (w.boneIndex1 == fore ? w.weight1 : 0)
                            + (w.boneIndex2 == fore ? w.weight2 : 0) + (w.boneIndex3 == fore ? w.weight3 : 0);
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
            // ⚠️ A REDESIGNED HERO'S ARM IS SIZED BY ITS FIST, NOT ITS LENGTH. Scaling every arm to one length gave each
            // hero a different hand: a long-armed body got a small fist and a short-armed one a big fist. Owner,
            // 2026-10-06, of the nine side by side: "the arms have inconsistent hand size compared to how dante's looks.
            // you should overlay dante's arms and try to match the hands". So the fist (the far 14 per cent of the arm)
            // is brought to Dante's size across (`NaturalFist`), and the arm keeps its far end at `ArmLength`, where every
            // action expects the hand; its shoulder end falls wherever its own proportions put it, off the screen.
            // ⚠️ NOT AN ARM THAT ENDS IN A POINT (Paete's braids): its far end is under half its widest section, there
            // is no fist to match, and it keeps the length rule. Seven-bone rigs have no forearm and keep it too.
            float shift = 0f;
            if (hasForearm)
            {
                float reach = last - first, fist = 0f, widest = 0f;
                Vector2 lo = Vector2.positiveInfinity, hi = Vector2.negativeInfinity, allLo = lo, allHi = hi;
                foreach (var p in positions)
                {
                    var across = new Vector2(p.z, p.y);
                    allLo = Vector2.Min(allLo, across); allHi = Vector2.Max(allHi, across);
                    if (p.x * side - first < reach * .86f) continue;
                    lo = Vector2.Min(lo, across); hi = Vector2.Max(hi, across);
                }
                fist = Mathf.Max(hi.x - lo.x, hi.y - lo.y); widest = Mathf.Max(allHi.x - allLo.x, allHi.y - allLo.y);
                if (fist > 1e-4f && fist > widest * .5f)
                {
                    scale = NaturalFist / fist;
                    shift = ViewmodelArms.ArmLength - reach * scale;
                }
            }
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 p = positions[i], n = normals[i];
                // +Y reaches the first-person hand; -Z is its upper surface. Two
                // sign changes preserve winding for both left and right source arms.
                positions[i] = new Vector3(-side*p.z, side*p.x-first, -p.y) * scale + Vector3.up * shift;
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
