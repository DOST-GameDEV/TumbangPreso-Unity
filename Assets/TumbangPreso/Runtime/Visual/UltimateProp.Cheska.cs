using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE ICE OF CHESKA'S ABSOLUTE ZERO (2026-10-10): the block that comes down on each player, the glacier, the spikes
    /// that race out round the court, the chunks it breaks into and the pines of her ridge. They are built the way the hand
    /// companions and Paete's reworked plants are (`tools/build_ultimate_props_cheska.py` on `tools/hand_companion_kit.py`):
    /// a painted atlas beside the models, round forms for everything soft, and the ice's clean planes painted face by face.
    ///
    /// The glacier, the spikes, the chunks and the pines are solid things and are dressed as the cast is (<see cref="Spawn"/>
    /// with <see cref="IcePalette"/>: the paint, the toon light, the ink line).
    /// ⚠️ A BLOCK IS NOT: a player stands inside it and must still be read. Its atlas carries ALPHA (clear in the middle of
    /// each pane, frosted toward the edges, solid where frost, cracks and snow are painted), and <see cref="SpawnIceBlock"/>
    /// draws it with the cast's own blended shader, so it takes the same light as everything round it.
    /// </summary>
    public static partial class UltimateProp
    {
        /// <summary>Her ice and her ridge. Every piece of these models is painted; the sixteen are what a flat piece would wear.</summary>
        public static readonly Color[] IcePalette =
        {
            Hex(0x7FD3EA), Hex(0x3E9CC4), Hex(0xC4F0FB), Hex(0xFFFFFF), Hex(0xEAFCFF), Hex(0xFF9EC4), Hex(0x2F7391), Hex(0x1F5270),
            Hex(0x20262E), Hex(0x6A5A72), Hex(0xFFFFFF), Hex(0xCFE6F2), Hex(0x3B9EBA), Hex(0x1F6F86), Hex(0xA3ECF7), Hex(0x11485A),
        };

        // Where an importer may have put a model's painted atlas (`ToonSkin` looks in the same three).
        private static readonly string[] PaintNames = { "_BaseMap", "_MainTex", "baseColorTexture" };

        /// <summary>
        /// The block <paramref name="name"/> as see-through painted ice. Returns the one material all of it wears. Null if the
        /// model is missing; dressed as a solid thing if its paint could not be found (a block nobody can see into is still
        /// a block of ice).
        /// </summary>
        public static GameObject SpawnIceBlock(string name, Transform parent, out Material ice)
        {
            ice = null;
            var go = Raw(name, parent);
            if (go == null) return null;
            Texture paint = null;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var from = r.sharedMaterial;
                if (from == null) continue;
                foreach (string property in PaintNames)
                    if (paint == null && from.HasProperty(property)) paint = from.GetTexture(property);
            }
            if (paint == null) { ToonSkin.Apply(go, ToonSkin.PersonOutlineWidth, IcePalette); return go; }
            ice = new Material(ToonSkin.TransparentShader) { name = "UltimateIceBlock-" + name, mainTexture = paint, color = Color.white };
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = ice;
            VfxRenderTag.Own(go, ice);
            return go;
        }

        /// <summary>
        /// ⚠️ THE ICE OF HER LOTUS (`lotuspetal_a`, `_b`, `_c`, `lotusshard`; `tools/build_ultimate_lotus_cheska.py`): the same
        /// painted cut crystal as the rest of her ice, drawn by its own shader (`Shaders/UltimateIceLotus`) because a flower
        /// of light needs what the cast's shader has no word for: light inside it, deep blue where it turns from the light,
        /// a lit rim, a glint that runs up it, and to be eaten away from its tip when it goes.
        /// <paramref name="ice"/> is the ONE material every piece of the flower wears: pass the same variable for each piece
        /// and the first call makes it (each piece's own moment goes in a property block). Null if the model is missing;
        /// dressed as her solid ice is if the shader or the paint could not be found, and <paramref name="ice"/> is then left null.
        /// </summary>
        public static GameObject SpawnLitIce(string name, Transform parent, ref Material ice)
        {
            var go = Raw(name, parent);
            if (go == null) return null;
            if (ice == null)
            {
                var shader = Resources.Load<Shader>("Shaders/UltimateIceLotus");
                Texture paint = null;
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    var from = r.sharedMaterial;
                    if (from == null) continue;
                    foreach (string property in PaintNames)
                        if (paint == null && from.HasProperty(property)) paint = from.GetTexture(property);
                }
                if (shader == null || !shader.isSupported || paint == null) { ToonSkin.Apply(go, ToonSkin.PersonOutlineWidth, IcePalette); return go; }
                ice = new Material(shader) { name = "UltimateIceLotus", mainTexture = paint };
                // The first piece made owns it; the whole flower is the stage's and dies with it.
                VfxRenderTag.Own(go, ice);
            }
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = ice;
            return go;
        }
    }
}
