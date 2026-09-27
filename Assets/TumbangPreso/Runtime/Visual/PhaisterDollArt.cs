using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE VOODOO DOLL'S LIGHT (HERO-10 v3, owner 2026-09-27: *"make it look liek its glowing inside and it stitched tgthr"*).
    /// `phaister-doll.glb` carries its split seams, its grin, the slit under its X eye and the ring round its button as a third
    /// skinned mesh, `glow-mesh`; the toon paint (`ToonSkin`) treats it like cloth, so this repaints it with `SoulGlow` after.
    /// Every place that shows the doll calls it: the review, the ultimate's body, the cutscene.
    /// </summary>
    public static class PhaisterDollArt
    {
        public const string GlowMeshName = "glow-mesh";

        /// <summary>The soul light, her violet-magenta.</summary>
        public static readonly Color Soul = new Color(1.0f, 0.33f, 0.90f, 1.0f);

        /// <summary>The hot line down the middle of every split.</summary>
        public static readonly Color SoulCore = new Color(1.0f, 0.86f, 0.97f, 1.0f);

        private static Material _glow;

        public static Material GlowMaterial
        {
            get
            {
                if (_glow != null) return _glow;
                var shader = Resources.Load<Shader>("Shaders/SoulGlow");
                if (shader == null) return null;
                _glow = new Material(shader) { name = "PhaisterDollSoul" };
                _glow.SetColor("_Color", Soul);
                _glow.SetColor("_Core", SoulCore);
                return _glow;
            }
        }

        /// <summary>Paints the doll's glow mesh with its soul light. Call after `ToonSkin.Apply`.</summary>
        public static void ApplyGlow(GameObject model)
        {
            if (model == null) return;
            var material = GlowMaterial;
            if (material == null) return;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                if (renderer != null && renderer.name == GlowMeshName)
                {
                    renderer.sharedMaterials = new[] { material };
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
        }
    }
}
