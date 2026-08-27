using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Welds the inverted hull that draws the ink outline, by giving every vertex an AVERAGED
    /// normal to inflate along instead of its own.
    ///
    /// ⚠️⚠️ THIS IS WHY THE OUTLINES DID NOT FULLY CONNECT, AND IT IS A PROPERTY OF THE MESHES
    /// RATHER THAN OF THE SHADER. 🧑 2026-08-27: *"the issue is the outlines dont fully
    /// connect"*.
    ///
    /// The outline Pass in `TumbangPreso/Toon` is an inverted hull: it draws back faces pushed
    /// out along the vertex normal, so a shell peeks out from behind the silhouette. That
    /// construction assumes one normal per POSITION. These rigs break the assumption. A hard
    /// edge, a UV seam or a material split all force the importer to emit the same corner
    /// SEVERAL times, once per adjoining face, each copy carrying its own face normal. Pushing
    /// those copies along their own normals sends them to different places, so the hull tears
    /// open at exactly the corners the eye reads as the shape, and the border shows a gap.
    ///
    /// ⚠️ THE GAP IS WIDEST WHERE THE ANGLE IS SHARPEST, which is why it reads as "some parts
    /// are missing" rather than as a uniformly thin border: two normals 90° apart separate by
    /// the full outline width, while a shallow bend barely parts at all. On a Kenney mini that
    /// is the fingers, the jaw, the shoulders and the tops of the shoes.
    ///
    /// Averaging every normal that shares a position and inflating along the average welds the
    /// copies back together: they all travel to the same point, so the shell stays closed.
    ///
    /// ⚠️⚠️ THE AVERAGE GOES IN THE TANGENT CHANNEL, AND NOTHING ELSE WOULD SURVIVE THE ANIMATION.
    /// The obvious home is a spare UV channel, and it is wrong here: Unity skins POSITION, NORMAL
    /// and TANGENT for a SkinnedMeshRenderer and passes UVs through untouched, so a smoothed
    /// normal parked in UV3 would stay in bind pose and the outline would tear open the moment a
    /// limb rotated. TANGENT is skinned by the same matrices as the normal, so it tracks the
    /// bones for free.
    ///
    /// ⚠️ AND THE TANGENT IS FREE TO TAKE. `TumbangPreso/Toon` samples no normal map and never
    /// writes `o.Normal`, so nothing in either pass reads a tangent. Verified by grep before this
    /// was written. If a normal map is ever added to the toon shader, this channel has to move
    /// and there is nowhere good left to put it: the fix then is to bake the average at import
    /// time into a second UV set and re-skin it by hand, which is much more work. Do not add one
    /// casually.
    ///
    /// ⚠️ THE MESH IS EDITED IN PLACE, ONCE, AND THAT IS DELIBERATE. A smoothed normal is a pure
    /// function of the mesh, so every instance of a rig wants the identical answer and there is
    /// no reason to pay for it per character. Editing `sharedMesh` means twelve people wearing
    /// one rig cost one bake between them. It does NOT write to the `.glb`: Unity regenerates
    /// imported meshes from source, so this lives in the imported copy for the session only.
    /// </summary>
    public static class OutlineNormals
    {
        /// <summary>
        /// Meshes already welded, by entity id. Keyed on the id rather than the Mesh so a
        /// destroyed mesh cannot hold a reference alive.
        ///
        /// ⚠️ `GetEntityId` AND `HashSet<EntityId>`, NOT `GetInstanceID` AND NOT AN `int`. Unity 6.5
        /// marks BOTH `GetInstanceID` and the `EntityId`-to-`int` cast obsolete as ERRORS rather than
        /// as warnings, so neither compiles here. Storing the `EntityId` itself sidesteps both.
        /// </summary>
        private static readonly HashSet<EntityId> Welded = new HashSet<EntityId>();

        /// <summary>
        /// ⚠️ POSITIONS ARE QUANTISED BEFORE THEY ARE COMPARED. Two copies of one corner are
        /// bit-identical when they come from the same importer, but a rig that has been through
        /// a DCC round trip can differ in the last place or two, and an exact float compare then
        /// silently welds nothing at all while looking like it worked. A ten-thousandth of a
        /// model unit is far below any real feature on these rigs and far above that noise.
        /// </summary>
        private const float WeldGrid = 10000.0f;

        /// <summary>
        /// Bake averaged normals into whatever mesh this renderer draws. Safe to call repeatedly:
        /// the second call for a given mesh returns immediately.
        /// </summary>
        public static void Weld(Renderer renderer)
        {
            if (renderer == null) return;

            if (renderer is SkinnedMeshRenderer skinned)
            {
                Weld(skinned.sharedMesh);
                return;
            }

            var filter = renderer.GetComponent<MeshFilter>();
            if (filter != null) Weld(filter.sharedMesh);
        }

        public static void Weld(Mesh mesh)
        {
            // ⚠️ `isReadable` IS THE ONE THING THAT CANNOT BE WORKED AROUND HERE. A mesh imported
            // with Read/Write disabled has no CPU copy, so `mesh.vertices` comes back empty and
            // the weld would write a garbage tangent array over a perfectly good mesh. Skipping
            // leaves that renderer on per-vertex normals, which is the pre-existing look rather
            // than a new fault. `ModelImportSetup` enables it on these rigs.
            if (mesh == null || !mesh.isReadable) return;

            if (!Welded.Add(mesh.GetEntityId())) return;

            var vertices = mesh.vertices;
            var normals = mesh.normals;

            if (vertices.Length == 0 || normals.Length != vertices.Length) return;

            // Sum every normal that lands on a given position, then hand each vertex the sum for
            // its own position. Two passes rather than an O(n²) neighbour search.
            var sums = new Dictionary<(int, int, int), Vector3>(vertices.Length);

            for (int i = 0; i < vertices.Length; i++)
            {
                var cell = Cell(vertices[i]);
                sums.TryGetValue(cell, out var running);
                sums[cell] = running + normals[i];
            }

            var tangents = new Vector4[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                var summed = sums[Cell(vertices[i])];

                // ⚠️ A ZERO SUM IS REAL AND MUST FALL BACK. Two faces meeting at exactly 180°
                // (a flat card, a zero-thickness fin) cancel to nothing, and normalising that
                // gives a NaN which the GPU renders as a vertex flung to infinity: one stray
                // triangle smeared across the screen. Falling back to the vertex's own normal
                // reproduces the old behaviour for that vertex, which is a gap at worst.
                var welded = summed.sqrMagnitude > 1e-12f ? summed.normalized : normals[i];

                tangents[i] = new Vector4(welded.x, welded.y, welded.z, 1.0f);
            }

            mesh.tangents = tangents;
        }

        private static (int, int, int) Cell(Vector3 v) => (
            Mathf.RoundToInt(v.x * WeldGrid),
            Mathf.RoundToInt(v.y * WeldGrid),
            Mathf.RoundToInt(v.z * WeldGrid));
    }
}
