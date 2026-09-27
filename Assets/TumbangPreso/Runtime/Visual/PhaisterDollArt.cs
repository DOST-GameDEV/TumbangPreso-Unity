using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE VOODOO DOLL'S LIGHT (HERO-10 v3, model v11). Owner 2026-09-27: *"i want it to actually look like its coming out of
    /// the holes"*, *"make the glow really look like it comes from within not js drawn on"*.
    /// `phaister-doll.glb` carries its light as two more skinned meshes beside its cloth: `glow-mesh`, the light down inside every
    /// opening (the floor of each torn gap and its walls, graded hottest at the bottom by vertex colour), and `spill-mesh`, the light
    /// that falls out over the torn lips onto the cloth and the tongues licking up out of its crown. The toon paint (`ToonSkin`)
    /// treats both like cloth, so this repaints them after: `SoulGlow` (opaque, unlit) and `SoulSpill` (additive).
    /// Every place that shows the doll calls it: the review, the ultimate's body, the cutscene.
    /// </summary>
    public static class PhaisterDollArt
    {
        public const string GlowMeshName = "glow-mesh";
        public const string SpillMeshName = "spill-mesh";

        private static Material _glow;
        private static Material _spill;

        public static Material GlowMaterial => _glow != null ? _glow : (_glow = Make("Shaders/SoulGlow", "PhaisterDollSoul"));

        public static Material SpillMaterial => _spill != null ? _spill : (_spill = Make("Shaders/SoulSpill", "PhaisterDollSpill"));

        private static Material Make(string path, string name)
        {
            var shader = Resources.Load<Shader>(path);
            return shader == null ? null : new Material(shader) { name = name };
        }

        /// <summary>Paints the doll's light and its spill. Call after `ToonSkin.Apply`.</summary>
        public static void ApplyGlow(GameObject model)
        {
            if (model == null) return;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                var material = renderer.name == GlowMeshName ? GlowMaterial : renderer.name == SpillMeshName ? SpillMaterial : null;
                if (material == null) continue;
                renderer.sharedMaterials = new[] { material };
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }
    }
}
