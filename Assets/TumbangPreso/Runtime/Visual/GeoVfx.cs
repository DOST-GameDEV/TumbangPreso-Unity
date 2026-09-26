using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ FIRST-PASS BODIES FOR DANTE'S NEW GEO KIT (ABILITY-2): the boulder and the barrier, built
    /// from stepped stone slabs with his gold veins so they read as his (Geo: stepped slabs, gold veins,
    /// dust; plan § 5). They are placeholders for the presentation pass the plan orders (a typed model,
    /// the six beats, sounds) and are recorded as such in TODO ABILITY-2; they exist so the gameplay can
    /// be played and filmed now.
    /// </summary>
    public static class DanteBoulderVisual
    {
        private static readonly Color Stone = new Color(0.47f, 0.40f, 0.33f), StoneDark = new Color(0.32f, 0.27f, 0.22f),
                                      Gold = new Color(0.93f, 0.74f, 0.26f);

        public static void Build(Transform parent)
        {
            var core = GrowthVfx.Block(parent, "boulder-core", new Vector3(0.52f, 0.46f, 0.50f), Stone).transform;
            core.localRotation = Quaternion.Euler(12f, 25f, 8f);
            var cap = GrowthVfx.Block(parent, "boulder-cap", new Vector3(0.36f, 0.22f, 0.40f), StoneDark).transform;
            cap.localPosition = new Vector3(0.05f, 0.24f, -0.03f); cap.localRotation = Quaternion.Euler(-8f, 40f, 5f);
            var chip = GrowthVfx.Block(parent, "boulder-chip", new Vector3(0.24f, 0.26f, 0.20f), StoneDark).transform;
            chip.localPosition = new Vector3(-0.22f, -0.08f, 0.14f); chip.localRotation = Quaternion.Euler(20f, -15f, 30f);
            var vein = GrowthVfx.Block(parent, "boulder-vein", new Vector3(0.04f, 0.40f, 0.52f), Gold, 0.3f).transform;
            vein.localPosition = new Vector3(0.12f, 0.0f, 0.0f); vein.localRotation = Quaternion.Euler(0f, 25f, 18f);
        }
    }

    public static class DanteBarrierVisual
    {
        private static readonly Color Stone = new Color(0.55f, 0.47f, 0.38f), Gold = new Color(0.93f, 0.74f, 0.26f);

        /// <summary>Three stepped slabs across his front at the barrier's width, gold seams between.</summary>
        public static GameObject Build(Transform owner)
        {
            var root = new GameObject("DanteBarrier");
            root.transform.SetParent(owner, false);
            root.transform.localPosition = new Vector3(0f, 0f, Core.GeoRules.BarrierForward);
            float w = Core.GeoRules.BarrierWidth;
            var left = GrowthVfx.Block(root.transform, "slab-left", new Vector3(w * 0.34f, 1.7f, 0.14f), Stone).transform;
            left.localPosition = new Vector3(-w * 0.33f, 0.85f, 0.06f); left.localRotation = Quaternion.Euler(0f, -12f, 0f);
            var mid = GrowthVfx.Block(root.transform, "slab-mid", new Vector3(w * 0.36f, 2.1f, 0.16f), Stone).transform;
            mid.localPosition = new Vector3(0f, 1.05f, 0.12f);
            var right = GrowthVfx.Block(root.transform, "slab-right", new Vector3(w * 0.32f, 1.55f, 0.14f), Stone).transform;
            right.localPosition = new Vector3(w * 0.34f, 0.78f, 0.05f); right.localRotation = Quaternion.Euler(0f, 14f, 0f);
            var seamA = GrowthVfx.Block(root.transform, "seam-a", new Vector3(0.04f, 1.6f, 0.18f), Gold, 0.35f).transform;
            seamA.localPosition = new Vector3(-w * 0.165f, 0.8f, 0.1f);
            var seamB = GrowthVfx.Block(root.transform, "seam-b", new Vector3(0.04f, 1.5f, 0.18f), Gold, 0.35f).transform;
            seamB.localPosition = new Vector3(w * 0.17f, 0.75f, 0.1f);
            return root;
        }
    }
}
