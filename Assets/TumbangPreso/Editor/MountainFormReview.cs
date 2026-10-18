using System;
using System.Linq;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// `-mountain` on `HeroTurnaroundProbe.RunOne` or `ClipMotionStrip.Run` (rig dante): Dante photographed in MOUNTAIN FORM, his armour
    /// formed (`DanteMountainArmour`), so it is judged on the body in every clip (owner, 2026-10-17: *"THROOUGHLY CHECK OUT HOW
    /// THE ARMOR WILL WORK SA GAME MISMO AND IF IT LOOKS GREAT KASI baka it looks bad with some animations"*).
    /// </summary>
    internal static class MountainFormReview
    {
        public static bool On => Environment.GetCommandLineArgs().Contains("-mountain");

        /// <summary>Armour on <paramref name="model"/>, whose front is <paramref name="yaw"/> round its <paramref name="pivot"/>.</summary>
        public static DanteMountainArmour Dress(GameObject model, Transform pivot, float yaw)
        {
            if (!On || model == null) return null;
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = skin.sharedMesh; if (mesh == null) continue;
                var counts = new int[skin.bones.Length];
                foreach (var w in mesh.boneWeights) if (w.weight0 >= .99f) counts[w.boneIndex0]++;
                Debug.Log($"[Mountain] skin {skin.name} verts {mesh.vertexCount} bones " +
                          string.Join(", ", skin.bones.Select((b, i) => $"{(b != null ? b.name : "null")}:{counts[i]} s{(b != null ? b.lossyScale.x : 0f):F2}")));
            }
            var front = new GameObject("MountainFront").transform;
            front.SetParent(pivot, false);
            front.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var armour = DanteMountainArmour.Build(model, front);
            if (armour != null) armour.StepTo(DanteMountainArmour.FormedAt + 4f);
            return armour;
        }
    }
}
