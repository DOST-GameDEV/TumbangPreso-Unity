using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TumbangPreso.Map
{
    /// <summary>
    /// TREE LEAVES KEEP THEIR FULL MESH NEAR THE ARENA, ON EVERY MAP (owner, 2026-10-31: "the tree leaves lod is too close to the
    /// arena before the lower quality meshes apply", then "preferably ensure that this is documented properly and doesnt
    /// happen with other maps"). A LODGroup switches by screen height, so the same authored numbers switch at different
    /// distances with each plant's size, the field of view and lodBias, and a small tree dropped to its low mesh in plain view.
    /// So when a scene loads, any LODGroup with leaves (`LagoonFoliage`, `KantoFoliage`) and at least two levels has its
    /// first switch pushed out to <see cref="NearestSwitchMetres"/> if it would happen nearer. It is a rule here rather than
    /// numbers in each map's builder so that a new map obeys it unasked. Single-level groups are only a cull and are left alone,
    /// and so is the Low tier, which spends less on purpose (integrated GPUs).
    /// </summary>
    internal static class FoliageLodFloor
    {
        /// <summary>The nearest a tree's leaves may leave their full mesh, metres from the camera.</summary>
        internal const float NearestSwitchMetres = 80f;
        /// <summary>The widest vertical field of view assumed: a wider view makes the same tree smaller on screen.</summary>
        private const float WidestVerticalFov = 75f;
        private const int LowTier = 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Watch()
        {
            Apply();
            SceneManager.sceneLoaded += (scene, mode) => Apply();
        }

        private static void Apply()
        {
            if (GraphicsProfiles.Current == LowTier) return;
            float span = 2f * Mathf.Tan(WidestVerticalFov * .5f * Mathf.Deg2Rad);
            int moved = 0;
            foreach (var group in Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None))
            {
                var levels = group.GetLODs();
                if (levels.Length < 2 || !HasLeaves(levels)) continue;
                var scale = group.transform.lossyScale;
                float size = group.size * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                float wanted = size * QualitySettings.lodBias / (NearestSwitchMetres * span);
                if (levels[0].screenRelativeTransitionHeight <= wanted) continue;
                levels[0].screenRelativeTransitionHeight = wanted;
                for (int i = 1; i < levels.Length; i++)
                    if (levels[i].screenRelativeTransitionHeight >= levels[i - 1].screenRelativeTransitionHeight)
                        levels[i].screenRelativeTransitionHeight = levels[i - 1].screenRelativeTransitionHeight * .5f;
                group.SetLODs(levels);
                moved++;
            }
            if (moved > 0) Debug.Log($"[FoliageLodFloor] {moved} leaf LOD groups held to their full mesh within {NearestSwitchMetres} m");
        }

        private static bool HasLeaves(LOD[] levels)
        {
            foreach (var level in levels)
                foreach (var renderer in level.renderers)
                {
                    if (renderer == null) continue;
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == null || material.shader == null) continue;
                        string shader = material.shader.name;
                        if (shader == "TumbangPreso/LagoonFoliage" || shader == "TumbangPreso/KantoFoliage") return true;
                    }
                }
            return false;
        }
    }
}
