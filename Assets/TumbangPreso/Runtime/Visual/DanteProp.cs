using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Basilio's modelled props (`tools/build_dante_props.py`, `Resources/Models/DanteProps`): the mountain beast of CONTINENTAL
    /// DRIFT. Loaded and dressed the way the cast is (`ToonSkin`, the ink outline) in his own palette (the builder's
    /// `DANTE_PALETTE`, same hex values), and its light repainted after: every `glow-*` node is a cut-in opening in the
    /// `SoulGlow` layout (a mouth on the stone, a cup sunk behind it), so the eyes and seams glow from INSIDE the rock.
    /// Each spawned prop gets its own glow materials, so a scene can ignite its eyes apart from its seams (`SetGlow`).
    /// </summary>
    public static class DanteProp
    {
        public const string ResourceFolder = "Models/DanteProps";

        public static readonly Color[] Palette =
        {
            Hex(0x4F453D), Hex(0x5C4D40), Hex(0x3A322C), Hex(0x6E6052), Hex(0xFF6B14), Hex(0xFF9E2E), Hex(0x8C290F), Hex(0x6B5238),
            Hex(0x1E140C), Hex(0x140E0A), Hex(0x2A2430), Hex(0x1A1620), Hex(0x463C34), Hex(0x7A6A58), Hex(0x33291F), Hex(0x55473A),
        };

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        /// <summary>The prop <paramref name="name"/> under <paramref name="parent"/>, dressed, its light cut in; null if missing.</summary>
        public static GameObject Spawn(string name, Transform parent)
        {
            var source = HeroPropAssets.Load(ResourceFolder, name);
            if (source == null) { Debug.LogWarning("[DanteProp] Models/DanteProps/" + name + " is missing; run tools/build_dante_props.py."); return null; }
            var go = Object.Instantiate(source, parent, false);
            go.name = "DanteProp-" + name;
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) { c.enabled = false; Object.Destroy(c); }
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                VfxRenderTag.Attach(r.gameObject);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            ToonSkin.Apply(go, ToonSkin.PersonOutlineWidth, Palette);
            var shader = Resources.Load<Shader>("Shaders/SoulGlow");
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.name.StartsWith("glow-")) continue;
                if (shader == null) { r.enabled = false; continue; }
                // Never white: the cup's hottest vertex is molten hot (1, .62, .18) and the intensity stays at 1.
                var m = new Material(shader) { name = "DanteGlow-" + r.name };
                m.SetColor("_Tint", Color.white); m.SetFloat("_Intensity", 1f); m.SetFloat("_Pulse", .18f); m.SetFloat("_Rate", 1.6f);
                m.SetFloat("_Open", .35f);
                r.sharedMaterials = new[] { m };
                r.receiveShadows = false;
                VfxRenderTag.Own(r.gameObject, m);
            }
            return go;
        }

        /// <summary>A named node anywhere under the prop.</summary>
        public static Transform Find(GameObject model, string name) => PaeteProp.Find(model, name);

        /// <summary>
        /// How lit a glow node is, 0 (dark, the opening is a black hole in the rock) to 1 (molten). Its pulse is kept.
        /// </summary>
        public static void SetGlow(GameObject model, string node, float lit)
        {
            var t = Find(model, node);
            var r = t != null ? t.GetComponent<Renderer>() : null;
            if (r == null || r.sharedMaterial == null) return;
            r.sharedMaterial.SetFloat("_Intensity", Mathf.Clamp01(lit));
        }
    }
}
