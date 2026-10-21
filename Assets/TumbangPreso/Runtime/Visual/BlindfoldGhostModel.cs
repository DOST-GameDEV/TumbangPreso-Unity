using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ BLINDFOLD, DON'T LOOK'S GHOST, AS THE OWNER APPROVED IT (2026-10-15, mockup `lineup2.png`: *"Looks good. Build
    /// Blindfold in game first"*). Each of Nemu's skills gets its own ghost, Gekko's pets the model (*"very distinct"*, *"they
    /// look like actual ghosts"*, *"scary and cute at the same time"*). This one is a small ghost child: icy pale blue (cold
    /// dread), black hair, a white cloth tied over its eyes, a wispy see-through tail and no legs, one fang.
    /// SPRUNG, the cloth drops to its neck: big hollow black eyes with cyan pinpoints, two fangs, arms reaching for your face.
    ///
    /// Voxels typed by hand, the mockup's own rows (x right, y up, z toward the viewer; its face is +z, Unity's forward).
    /// Built once per pose into a vertex-coloured mesh drawn by `Shaders/BlindfoldGhost`. Each face is shaded by which way
    /// it points, as the mockup was (top light, sides darker), so it reads as the same blocky creature from any side.
    /// ⚠️ VERTEX COLOURS ARE NOT CONVERTED TO LINEAR BY UNITY, so they go in as `.linear` here.
    /// </summary>
    public static class BlindfoldGhostModel
    {
        /// <summary>Metres per voxel: the ghost is 17 voxels tall, about 0.85 m, a small thing that fits on a face.
        /// ⚠️ Was 0.075 (1.3 m): owner on film v4, *"make the ghost look better and smaler"*.</summary>
        public const float Voxel = .05f;
        /// <summary>Half its head's depth (metres, unscaled): how far its face stands in front of its head's middle.</summary>
        public static float HeadHalfDepth => PetGhostModels.PetGhosts ? PetGhostModels.MouseHeadHalfDepth : VoxelHeadHalfDepth;
        private const float VoxelHeadHalfDepth = 4f * Voxel;
        /// <summary>The middle of its head (local, metres): where it clamps over a victim's eyes.</summary>
        public static Vector3 HeadCentre => PetGhostModels.PetGhosts ? PetGhostModels.MouseHeadCentre : VoxelHeadCentre;
        private static readonly Vector3 VoxelHeadCentre = new Vector3(0f, 12.5f * Voxel, 0f);
        /// <summary>Its eyes when sprung (local, metres), for their glow.</summary>
        public static Vector3[] Eyes => PetGhostModels.PetGhosts ? PetGhostModels.MouseEyes : VoxelEyes;
        private static readonly Vector3[] VoxelEyes = { new Vector3(-1.5f * Voxel, 12.5f * Voxel, 4.1f * Voxel), new Vector3(1.5f * Voxel, 12.5f * Voxel, 4.1f * Voxel) };

        public static readonly Color Body = Hex(0xDCEEFF), BodyShade = Hex(0xA9CBEA), Hair = Hex(0x16141E);
        public static readonly Color Cloth = Hex(0xF0EEF2), ClothShade = Hex(0xBDBACB), Hollow = Hex(0x0B0A0F);
        public static readonly Color Fang = Hex(0xF4F1EA), Pinpoint = Hex(0x7FF3FF), Aura = Hex(0x9FD6FF);

        private static Mesh _waiting, _sprung;
        // ⚠️ `PetGhostModels.PetGhosts` (off) swaps in the mint mouse; its cloth is over its eyes waiting, round its neck sprung.
        public static Mesh Waiting => PetGhostModels.PetGhosts && PetGhostModels.MouseWaiting != null ? PetGhostModels.MouseWaiting : _waiting != null ? _waiting : _waiting = GhostVoxels.Build(Shape(false), "BlindfoldGhost", Origin);
        public static Mesh Sprung => PetGhostModels.PetGhosts && PetGhostModels.MouseSprung != null ? PetGhostModels.MouseSprung : _sprung != null ? _sprung : _sprung = GhostVoxels.Build(Shape(true), "BlindfoldGhostSprung", Origin);
        // Centred on x and z (the body spans 0..8 voxel corners), standing on y 0.
        private static readonly Vector3 Origin = new Vector3(4f, 0f, 4f);

        private static Color Hex(int rgb) => GhostVoxels.Hex(rgb);

        /// <summary>The mockup's `blindfold(sprung)`, row for row.</summary>
        private static GhostVoxels Shape(bool sprung)
        {
            var m = new GhostVoxels();
            // The tail: thinning, see-through, no legs.
            m.Box(3, 0, 3, 4, 1, 4, BodyShade, .35f); m.Box(2, 2, 2, 5, 3, 5, BodyShade, .6f); m.Put(5, 1, 4, BodyShade, .35f);
            // The body, its ragged hem, and its little arms.
            m.Box(1, 4, 1, 6, 8, 6, Body); m.Box(0, 4, 0, 7, 4, 7, Body, .85f);
            foreach (var (x, z) in new[] { (0, 0), (2, 0), (4, 0), (6, 0), (7, 2), (7, 5), (0, 3), (0, 6), (3, 7), (6, 7) }) m.Remove(x, 4, z);
            m.Box(-1, 6, 3, 0, 7, 4, Body); m.Box(7, 6, 3, 8, 7, 4, Body);
            // The head, and its black hair: a cap over the top, a bob down to the cloth at the back and sides, a fringe.
            // ⚠️ NOT THE MOCKUP'S FULL-LENGTH HAIR: it covered the back and sides to the neck, and film r10's court camera,
            // seeing the ghost from behind and the side, saw a black cube.
            m.Box(0, 9, 0, 7, 16, 7, Body);
            m.Box(0, 15, 0, 7, 16, 7, Hair); m.Box(0, 13, 0, 7, 16, 0, Hair);
            m.Box(0, 14, 0, 0, 16, 6, Hair); m.Box(7, 14, 0, 7, 16, 6, Hair);
            m.Box(1, 14, 7, 6, 14, 7, Hair); m.Put(1, 13, 7, Hair); m.Put(6, 13, 7, Hair); m.Put(3, 13, 7, Hair);
            // ROUNDED, NOT A CRATE (owner on v4: *"make the ghost look better"*): its head's four upright edges and the
            // edges of its crown are cut back a voxel, so it reads as a round little head under a bob of hair.
            foreach (var (cx, cz) in new[] { (0, 0), (7, 0), (0, 7), (7, 7) })
                for (int y = 9; y <= 16; y++) m.Remove(cx, y, cz);
            for (int i = 0; i <= 7; i++) { m.Remove(i, 16, 0); m.Remove(i, 16, 7); m.Remove(0, 16, i); m.Remove(7, 16, i); }
            foreach (var (cx, cz) in new[] { (1, 0), (6, 0), (0, 1), (7, 1), (0, 6), (7, 6), (1, 7), (6, 7) }) m.Remove(cx, 9, cz);
            if (!sprung)
            {
                // The cloth over its eyes, knotted behind, its two tails hanging.
                m.Box(0, 12, 0, 7, 13, 7, Cloth); m.Box(1, 12, 7, 6, 13, 7, Cloth);
                m.Box(2, 12, -1, 3, 13, -1, ClothShade); m.Put(2, 11, -2, ClothShade); m.Put(4, 10, -2, ClothShade); m.Put(1, 10, -3, ClothShade);
                m.Box(3, 10, 7, 4, 10, 7, Hollow); m.Put(4, 10, 8, Fang);
            }
            else
            {
                // The cloth down round its neck; its eyes, hollow, with a cyan pinpoint in each; a wider mouth; arms out.
                m.Box(0, 9, 0, 7, 9, 7, Cloth);
                // ⚠️ Two wide with a gap between (the mockup's touching 3-wide eyes read as one black bar, film r10).
                foreach (int ex in new[] { 1, 5 })
                {
                    m.Box(ex, 11, 7, ex + 1, 13, 7, Hollow);
                    // Flush in the hollow, not standing out of it: a pinpoint that stood a voxel proud became a cyan slab
                    // across each eye when it jumped into the victim's lens (film r12).
                    m.Put(ex == 1 ? 2 : 5, 12, 7, Pinpoint, 1f, true);
                }
                m.Box(2, 10, 7, 5, 10, 7, Hollow); m.Put(2, 10, 8, Fang); m.Put(5, 10, 8, Fang);
                // Its arms up and forward round the sides of a head (owner on v4: *"actually latch sa face"*), little
                // hands curled in at the ends to hold on.
                m.Box(-1, 11, 3, -1, 12, 11, Body); m.Box(8, 11, 3, 8, 12, 11, Body);
                m.Box(0, 11, 11, 0, 12, 12, BodyShade); m.Box(7, 11, 11, 7, 12, 12, BodyShade);
            }
            return m;
        }
    }

    /// <summary>
    /// Nemu's ghosts are typed by hand as voxels (Blindfold, Skitter, Lookout, each its own mockup) and built here into one
    /// vertex-coloured mesh drawn by `Shaders/BlindfoldGhost`: faces only where nothing solid covers them, each shaded by the
    /// way it points (top light, sides darker). uv.x marks a glowing voxel; uv.y how far down its tail a voxel is (sway).
    /// ⚠️ VERTEX COLOURS ARE NOT CONVERTED TO LINEAR BY UNITY, so they go in as `.linear`.
    /// </summary>
    internal sealed class GhostVoxels
    {
        internal struct Cell { public Color Colour; public float Alpha; public bool Glow; }
        public readonly Dictionary<Vector3Int, Cell> V = new Dictionary<Vector3Int, Cell>();
        /// <summary>Voxels below this row are tail (they sway, more toward the tip).</summary>
        public float TailTop = 4.5f;

        public static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        public void Box(int x0, int y0, int z0, int x1, int y1, int z1, Color c, float a = 1f, bool glow = false)
        {
            for (int x = Mathf.Min(x0, x1); x <= Mathf.Max(x0, x1); x++)
            for (int y = Mathf.Min(y0, y1); y <= Mathf.Max(y0, y1); y++)
            for (int z = Mathf.Min(z0, z1); z <= Mathf.Max(z0, z1); z++)
                V[new Vector3Int(x, y, z)] = new Cell { Colour = c, Alpha = a, Glow = glow };
        }
        public void Put(int x, int y, int z, Color c, float a = 1f, bool glow = false) => Box(x, y, z, x, y, z, c, a, glow);
        public void Remove(int x, int y, int z) => V.Remove(new Vector3Int(x, y, z));

        private static readonly Vector3Int[] Normals =
            { Vector3Int.up, Vector3Int.down, Vector3Int.right, Vector3Int.left, new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };
        // The mockup's light: top bright, front full, sides and back darker, underneath darkest.
        private static readonly float[] Shade = { 1.12f, .62f, .78f, .78f, 1f, .84f };

        public static Mesh Build(GhostVoxels shape, string name, Vector3 origin)
        {
            var verts = new List<Vector3>(); var colours = new List<Color>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            foreach (var kv in shape.V)
            {
                var at = kv.Key; var cell = kv.Value;
                for (int f = 0; f < 6; f++)
                {
                    var n = Normals[f];
                    // A face shows unless an opaque voxel covers it (see-through tail voxels let faces behind them show).
                    if (shape.V.TryGetValue(at + n, out var next) && next.Alpha >= .99f && cell.Alpha >= .99f) continue;
                    if (shape.V.TryGetValue(at + n, out next) && next.Alpha < .99f && cell.Alpha < .99f) continue;
                    Vector3 normal = n;
                    Vector3 centre = (Vector3)at + Vector3.one * .5f + normal * .5f - origin;
                    Vector3 u = Mathf.Abs(normal.y) > .5f ? Vector3.right : Vector3.up;
                    Vector3 v = Vector3.Cross(normal, u);
                    int start = verts.Count;
                    verts.Add((centre - u * .5f - v * .5f) * BlindfoldGhostModel.Voxel); verts.Add((centre + u * .5f - v * .5f) * BlindfoldGhostModel.Voxel);
                    verts.Add((centre + u * .5f + v * .5f) * BlindfoldGhostModel.Voxel); verts.Add((centre - u * .5f + v * .5f) * BlindfoldGhostModel.Voxel);
                    float k = cell.Glow ? 1f : Shade[f];
                    var c = new Color(Mathf.Clamp01(cell.Colour.r * k), Mathf.Clamp01(cell.Colour.g * k), Mathf.Clamp01(cell.Colour.b * k)).linear;
                    c.a = cell.Alpha;
                    // uv.x: 1 on a glowing voxel; uv.y: how far down the tail (0 at the hem, 1 at the tip) for its sway.
                    var uv = new Vector2(cell.Glow ? 1f : 0f, Mathf.Clamp01((shape.TailTop - at.y) / shape.TailTop));
                    for (int i = 0; i < 4; i++) { colours.Add(c); uvs.Add(uv); }
                    // Wound so the face points along its normal.
                    Vector3 cross = Vector3.Cross(verts[start + 1] - verts[start], verts[start + 2] - verts[start]);
                    // (Unity's face normal for a, b, c is Cross(b - a, c - a), and that side is the one drawn.)
                    if (Vector3.Dot(cross, normal) > 0f) { tris.Add(start); tris.Add(start + 1); tris.Add(start + 2); tris.Add(start); tris.Add(start + 2); tris.Add(start + 3); }
                    else { tris.Add(start); tris.Add(start + 2); tris.Add(start + 1); tris.Add(start); tris.Add(start + 3); tris.Add(start + 2); }
                }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts); mesh.SetColors(colours); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
