using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ NEMU'S PET GHOSTS, BEHIND A SWITCH (owner, 2026-10-19: *"start remodelling nemu's new ghosts. they should
    /// follow the same cutesy style like you did for kuro"*, then, of the boards, *"cocoa bear / scrap the rabbit"*). DON'T LOOK is
    /// a mint mouse with a cloth over its eyes, SKITTER a cocoa bear that carries the slipper under its muzzle, LOOKOUT a butter owl
    /// with big eyes and a lantern. Each is Kuro's family: one rounded block, his bead tail, an ink face, one colour.
    ///
    /// ⚠️ COSMETIC ONLY. With <see cref="PetGhosts"/> on, `BlindfoldGhostModel` and `NemuGhostModels` hand out these meshes and
    /// these attach points instead of ASTRA's voxels, and `NemuGhostBody` / `NemuBlindfold` draw them with `Shaders/PetGhost`
    /// (ASTRA's fade, sway, rim, flash and glow, from the painted atlas). No rule, timing, number, snapshot or message changes,
    /// so nothing goes on the wire and the protocol does not move. Off, nothing here is even loaded.
    /// PEEKER (signature, was DON'T LOOK) is the mouse, FETCHER (attacking, was SKITTER) the bear, KEEPER (defending, was
    /// LOOKOUT) the owl: the ids, the classes and the jobs stayed, only the names and numbers moved.
    ///
    /// Models: `Resources/Models/NemuGhosts/nemu_petghost_{mouse,bear,owl,lantern}.glb` and their atlases, written by
    /// `tools/build_nemu_ghosts_simple.py --final` (one mesh per node, so nothing is combined at run time). The numbers below are
    /// what that script prints; re-read them from it if the models change.
    /// </summary>
    public static class PetGhostModels
    {
        /// <summary>
        /// ON since the reworked kit's look pass (2026-10-29): PEEKER is the mouse, FETCHER the bear, KEEPER the owl, and their
        /// painted puffs (`NemuGhostLook`) are made for these bodies. The skills were renamed and retuned (2026-10-21) but each
        /// ghost still does its old job (hide and lunge at the eyes; fetch the slipper; mind and lift the can), so the set the
        /// owner chose for those jobs still fits. False puts ASTRA's voxel ghosts back, exactly as before.
        /// </summary>
        public static bool PetGhosts = true;

        public const string Folder = "Models/NemuGhosts";
        /// <summary>The models are exported at 1.35 times the board size (`EXPORT_SCALE` in the builder); the numbers below are
        /// typed at board size, as the builder prints them, and multiplied by this.</summary>
        public const float Scale = 1.35f;
        /// <summary>How far a pet ghost's arm may stretch past its own length (court film v1: ASTRA's 3.2 and 2.6 turned the
        /// nubs into sticks). A hand that cannot reach stops short; the lantern and the grip follow the hand, not the target.</summary>
        public const float MaxArmStretch = 1.45f;

        // ---- DON'T LOOK, the mouse (metres, y up, +z its face, standing on y 0).
        public static readonly Vector3 MouseHeadCentre = new Vector3(0f, .301f, 0f) * Scale;
        public const float MouseHeadHalfDepth = .125f * Scale;
        public static readonly Vector3[] MouseEyes = { new Vector3(-.062f, .301f, .135f) * Scale, new Vector3(.062f, .301f, .135f) * Scale };
        // ---- SKITTER, the bear. Arms reach along +z from the shoulder; the hand is at the nub's end.
        public static readonly Vector3 BearHands = new Vector3(0f, .173f, .165f) * Scale;
        public static readonly Vector3[] BearShoulder = { new Vector3(-.140f, .225f, .072f) * Scale, new Vector3(.140f, .225f, .072f) * Scale };
        public static readonly Vector3 BearHand = new Vector3(0f, 0f, .085f) * Scale;
        public static readonly Vector3[] BearEyes = { new Vector3(-.064f, .293f, .140f) * Scale, new Vector3(.064f, .293f, .140f) * Scale };
        // ---- LOOKOUT, the owl, and its lantern.
        public static readonly Vector3[] OwlShoulder = { new Vector3(-.130f, .290f, .020f) * Scale, new Vector3(.130f, .290f, .020f) * Scale };
        public static readonly Vector3 OwlHandOnArm = new Vector3(0f, 0f, .085f) * Scale;
        public static readonly Vector3[] OwlEyes = { new Vector3(-.066f, .332f, .140f) * Scale, new Vector3(.066f, .332f, .140f) * Scale };
        public static readonly Vector3 LanternGlass = new Vector3(0f, -.110f, 0f) * Scale;
        // Where each body's bead tail begins (its block's foot): below this it sways, above it it does not.
        private const float MouseTail = .170f * Scale, BearTail = .150f * Scale, OwlTail = .170f * Scale;

        private sealed class Entry { public Mesh Mesh; public Material Material; }
        private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>();
        private static readonly Dictionary<Mesh, Material> Materials = new Dictionary<Mesh, Material>();
        private static Shader _shader;

        public static Mesh MouseWaiting => Get("nemu_petghost_mouse", "waiting", MouseTail);
        public static Mesh MouseSprung => Get("nemu_petghost_mouse", "sprung", MouseTail);
        public static Mesh Bear => Get("nemu_petghost_bear", "body", BearTail);
        public static Mesh BearArm(int side) => Get("nemu_petghost_bear", side == 0 ? "arm-0" : "arm-1", 0f);
        public static Mesh Owl => Get("nemu_petghost_owl", "body", OwlTail);
        public static Mesh OwlArm(int side) => Get("nemu_petghost_owl", side == 0 ? "arm-0" : "arm-1", 0f);
        public static Mesh Lantern => Get("nemu_petghost_lantern", "lantern", 0f);

        /// <summary>The pet material for <paramref name="mesh"/> if it is one of these models; otherwise <paramref name="fallback"/>.</summary>
        public static Material MaterialFor(Mesh mesh, Material fallback)
            => mesh != null && Materials.TryGetValue(mesh, out var m) && m != null ? m : fallback;

        public static bool IsPet(Mesh mesh) => mesh != null && Materials.ContainsKey(mesh);

        private static Mesh Get(string model, string node, float tailTop)
        {
            string key = model + "/" + node;
            if (Entries.TryGetValue(key, out var e) && e.Mesh != null) return e.Mesh;
            var prefab = Resources.Load<GameObject>(Folder + "/" + model);
            Mesh mesh = null;
            if (prefab != null)
                foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
                    if (t.name == node) { var f = t.GetComponent<MeshFilter>(); if (f != null) mesh = f.sharedMesh; break; }
            if (mesh == null)
            {
                Debug.LogError("[PetGhostModels] " + Folder + "/" + model + " has no node '" + node + "'; run tools/build_nemu_ghosts_simple.py --final.");
                return null;
            }
            if (_shader == null) _shader = Shader.Find("TumbangPreso/PetGhost");
            Material material = null;
            if (_shader == null) Debug.LogError("[PetGhostModels] Shaders/PetGhost is missing from the build (GameBuilder).");
            else
            {
                material = new Material(_shader) { name = "PetGhost-" + model + "-" + node };
                var atlas = Resources.Load<Texture2D>(Folder + "/" + model + "-atlas");
                if (atlas != null) material.SetTexture("_MainTex", atlas);
                else Debug.LogError("[PetGhostModels] " + Folder + "/" + model + "-atlas is missing.");
                material.SetFloat("_TailTop", tailTop);
            }
            Entries[key] = new Entry { Mesh = mesh, Material = material };
            Materials[mesh] = material;
            return mesh;
        }
    }
}
