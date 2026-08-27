using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// § THE INK OUTLINE HAS TO BE A CLOSED SHELL, AND THIS IS WHAT PROVES IT IS.
    ///
    /// 🧑 2026-08-27: *"the issue is the outlines dont fully connect"*. The outline Pass in
    /// `TumbangPreso/Toon` inflates back faces along a normal, and the rigs carry SPLIT normals at
    /// every hard edge, so each copy of a corner pushed somewhere different and the hull tore open.
    /// `OutlineNormals.Weld` averages the normals sharing a position into the tangent channel.
    ///
    /// ⚠️ THE FIRST TEST IS THE ONE THAT MATTERS, AND IT IS A READABILITY TEST RATHER THAN A
    /// GEOMETRY ONE. `Weld` returns silently on a mesh whose CPU copy was discarded at import,
    /// and glTFast exposes no read/write toggle in the importer the way `ModelImporter` does. So
    /// the failure mode this fix is most likely to hit in the future is not a wrong average, it is
    /// no average at all, on a rig that looks completely normal in the inspector. Assert it.
    /// </summary>
    public sealed class OutlineWeldTests
    {
        private const string PersonDir = "Assets/TumbangPreso/Art/characters/persons";
        private const string PropDir = "Assets/TumbangPreso/Art/models";

        /// <summary>
        /// ⚠️ THE `env_` MODELS ARE EXCLUDED, AND THE EXCLUSION IS THE 2026-07-29 REVERT. The world
        /// toon pass shipped once, was played and was pulled for banding on large flat surfaces and
        /// for the cost of an inverted hull on every mesh in a dressed street. `EnvColourPass`
        /// carries that history. A street mesh never reaches `ToonSkin.Apply`, so it has no outline
        /// to tear and demanding a weld on one would be asserting the opposite of the design.
        ///
        /// What is left is exactly the set that DOES wear ink: the four lata, the four tsinelas and
        /// the first-person arm.
        /// </summary>
        private static IEnumerable<string> Rigs =>
            Directory.GetFiles(PersonDir, "*.glb", SearchOption.AllDirectories)
                     .Concat(Directory.GetFiles(PropDir, "*.obj", SearchOption.TopDirectoryOnly)
                                      .Where(p => !Path.GetFileName(p).StartsWith("env_")))
                     .Select(p => p.Replace('\\', '/'))
                     .OrderBy(p => p);

        private static IEnumerable<Mesh> MeshesIn(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>();

        [Test]
        public void EveryOutlinedMeshIsReadableSoTheWeldCanRun()
        {
            var unreadable = new List<string>();
            int seen = 0;

            foreach (var path in Rigs)
            {
                foreach (var mesh in MeshesIn(path))
                {
                    seen++;
                    if (!mesh.isReadable) unreadable.Add($"{Path.GetFileName(path)}:{mesh.name}");
                }
            }

            Assert.Greater(seen, 0, $"No meshes loaded from {PersonDir} or {PropDir}. They moved, or " +
                                    "glTFast is not importing them, and this suite is testing nothing.");

            Assert.IsEmpty(unreadable,
                "These rig meshes have no CPU copy, so OutlineNormals.Weld cannot read their " +
                "normals and silently leaves the outline on per-vertex normals, which is the " +
                "torn border reported on 2026-08-27. Re-enable Read/Write for them: " +
                string.Join(", ", unreadable));
        }

        /// <summary>
        /// ⚠️ THE ACTUAL CLOSURE PROPERTY, STATED AS GEOMETRY RATHER THAN AS A SCREENSHOT. If two
        /// vertices sit at the same position, the hull only stays welded when they inflate along
        /// the SAME direction. That is exactly "their tangents are equal", and it is checkable
        /// without rendering anything.
        /// </summary>
        [Test]
        public void VerticesSharingAPositionInflateTheSameWay()
        {
            int checkedMeshes = 0;

            foreach (var path in Rigs)
            {
                foreach (var mesh in MeshesIn(path))
                {
                    if (!mesh.isReadable) continue;

                    OutlineNormals.Weld(mesh);

                    var vertices = mesh.vertices;
                    var tangents = mesh.tangents;

                    Assert.AreEqual(vertices.Length, tangents.Length,
                        $"{mesh.name} has no welded tangent channel after Weld.");

                    var seen = new Dictionary<(int, int, int), Vector3>();

                    for (int i = 0; i < vertices.Length; i++)
                    {
                        var cell = (Mathf.RoundToInt(vertices[i].x * 10000.0f),
                                    Mathf.RoundToInt(vertices[i].y * 10000.0f),
                                    Mathf.RoundToInt(vertices[i].z * 10000.0f));

                        var dir = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z);

                        if (seen.TryGetValue(cell, out var first))
                        {
                            Assert.Less(Vector3.Distance(first, dir), 1e-4f,
                                $"{Path.GetFileName(path)}:{mesh.name} vertex {i} shares a " +
                                "position with an earlier vertex but inflates a different way, " +
                                "so the outline hull tears open here.");
                        }
                        else
                        {
                            seen[cell] = dir;
                        }
                    }

                    checkedMeshes++;
                }
            }

            Assert.Greater(checkedMeshes, 0, "No readable rig meshes were checked.");
        }

        /// <summary>
        /// ⚠️ A SPLIT-NORMAL RIG IS THE PREMISE OF THE WHOLE FIX, so assert the premise holds. If
        /// these rigs are ever re-exported fully smooth the weld becomes a no-op, and this test
        /// failing is the signal to re-measure rather than to delete anything: the props and any
        /// future replacement art (§4a) will still need it.
        /// </summary>
        [Test]
        public void TheRigsActuallyHaveSplitNormalsForTheWeldToFix()
        {
            bool anySplit = false;

            foreach (var path in Rigs)
            {
                foreach (var mesh in MeshesIn(path))
                {
                    if (!mesh.isReadable) continue;

                    var vertices = mesh.vertices;
                    var normals = mesh.normals;
                    if (normals.Length != vertices.Length) continue;

                    var seen = new Dictionary<(int, int, int), Vector3>();

                    for (int i = 0; i < vertices.Length && !anySplit; i++)
                    {
                        var cell = (Mathf.RoundToInt(vertices[i].x * 10000.0f),
                                    Mathf.RoundToInt(vertices[i].y * 10000.0f),
                                    Mathf.RoundToInt(vertices[i].z * 10000.0f));

                        if (seen.TryGetValue(cell, out var first))
                        {
                            if (Vector3.Distance(first, normals[i]) > 1e-3f) anySplit = true;
                        }
                        else
                        {
                            seen[cell] = normals[i];
                        }
                    }
                }
            }

            Assert.IsTrue(anySplit,
                "No rig mesh has two vertices sharing a position with differing normals. The " +
                "outline gap this fix targets cannot occur on such a mesh, so either the art " +
                "changed or the meshes are not loading. Re-measure before trusting the fix.");
        }
    }
}
