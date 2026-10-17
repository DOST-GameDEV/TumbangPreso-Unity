using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE MODELLED PROPS OF THE RESTAGED ULTIMATE CUTSCENES (2026-10-09). `tools/build_ultimate_props.py` types each one part
    /// by part into `Resources/Models/UltimateProps`; this loads one and dresses it the way the cast is dressed (`ToonSkin`, the
    /// ink outline) in its own sixteen-slot palette. The slot numbers and hexes match the builder's table.
    ///
    /// They replace the stock prisms the cutscenes were staged with. The owner, once the staging stood: "continue polishing all
    /// the ultimates. i dont want to see anything bare in textures, anything looking unpolished, no vfx etc."
    /// </summary>
    public static class UltimateProp
    {
        public const string ResourceFolder = "Models/UltimateProps";

        /// <summary>Rafi's shark and the lepa: sea slate, cream, the mouth, and the Sama boat's red, teal and yellow.</summary>
        public static readonly Color[] SeaPalette =
        {
            Hex(0x4F7391), Hex(0x36536C), Hex(0xF1ECE2), Hex(0xC8324A), Hex(0xFFFFFF), Hex(0x12161C), Hex(0xFFFFFF), Hex(0x2B4257),
            Hex(0xA56A3C), Hex(0x6F4526), Hex(0xF4E6C2), Hex(0xE0483A), Hex(0x2AA6A0), Hex(0xC9A36F), Hex(0xF2C230), Hex(0x20262E),
        };

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        private static readonly int PresenceId = Shader.PropertyToID("_Presence"), BaseYId = Shader.PropertyToID("_BaseY");

        /// <summary>The prop <paramref name="name"/> under <paramref name="parent"/>, dressed; null if it is missing.</summary>
        public static GameObject Spawn(string name, Transform parent, Color[] palette, float width = ToonSkin.PersonOutlineWidth)
        {
            var go = Raw(name, parent);
            if (go != null) ToonSkin.Apply(go, width, palette);
            return go;
        }

        private static GameObject Raw(string name, Transform parent)
        {
            var source = HeroPropAssets.Load(ResourceFolder, name);
            if (source == null)
            {
                Debug.LogWarning("[UltimateProp] Models/UltimateProps/" + name + " is missing; run tools/build_ultimate_props.py.");
                return null;
            }
            var go = Object.Instantiate(source, parent, false);
            go.name = "UltimateProp-" + name;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) { c.enabled = false; PaeteProp.Kill(c); }
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                VfxRenderTag.Attach(r.gameObject);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return go;
        }

        /// <summary>
        /// ⚠️ THE SAME PROP AS A SPIRIT: drawn by the shader Makiling is drawn by in Paete's sky (`Shaders/SpiritGhost`: only its
        /// front shows, a body of light with a brighter rim), in <paramref name="palette"/> pulled most of the way to
        /// <paramref name="tint"/>. The owner asked for exactly that likeness ("ghostlike ... like paete's ult makiling").
        /// Returns the one material every part of it wears, for <see cref="Presence"/>; null if the shader is missing, in which
        /// case the prop is left dressed as a solid thing.
        /// </summary>
        public static GameObject SpawnSpirit(string name, Transform parent, Color[] palette, Color tint, Color rim, out Material spirit)
        {
            spirit = null;
            var go = Raw(name, parent);
            if (go == null) return null;
            var shader = Resources.Load<Shader>("Shaders/SpiritGhost");
            if (shader == null) { ToonSkin.Apply(go, ToonSkin.PersonOutlineWidth, palette); return go; }
            spirit = new Material(shader) { name = "UltimateSpirit-" + name };
            var slots = new Vector4[16];
            for (int i = 0; i < 16; i++) slots[i] = Color.Lerp(palette[i], tint, .62f).linear;
            spirit.SetVectorArray("_Palette", slots);
            spirit.SetColor("_Tint", tint); spirit.SetColor("_RimColor", rim);
            spirit.SetFloat("_Alpha", .46f); spirit.SetFloat("_RimAlpha", .7f); spirit.SetFloat("_Glow", .9f);
            // It has no feet to rise out of the ground from: wholly there at any height.
            spirit.SetFloat(BaseYId, -10000f); spirit.SetFloat("_FormRim", .6f);
            // No painted face on these: put the face window where the atlas holds nothing.
            spirit.SetVector("_FaceRect", new Vector4(2f, 2f, 3f, 3f));
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var from = r.sharedMaterial;
                if (from != null && from.mainTexture != null) spirit.mainTexture = from.mainTexture;
                r.sharedMaterial = spirit;
            }
            VfxRenderTag.Own(go, spirit);
            return go;
        }

        /// <summary>How much of a spirit is there, 0 to 1.</summary>
        public static void Presence(Material spirit, float amount)
        {
            if (spirit != null) spirit.SetFloat(PresenceId, Mathf.Clamp01(amount));
        }
    }
}
