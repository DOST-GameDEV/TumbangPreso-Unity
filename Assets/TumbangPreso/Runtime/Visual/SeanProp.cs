using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Sean's modelled props (`tools/build_sean_props.py`, `Resources/Models/SeanProps`): the flying MAGMA FIST, the Puddle
    /// Punch's giant fist, the VOLCANIC BOMB, Mayon and the Molten fist. Loaded and dressed the way the cast is (`ToonSkin`, the
    /// ink outline) in his own palette (the builder's `SEAN_PALETTE`, same hex values).
    ///
    /// ⚠️ NO CUT-IN GLOW, UNLIKE `DanteProp`. Dante's light is trapped in stone, so his props carry `glow-*` cups; Sean's lava
    /// IS the light (`docs/reports/sean-kit-2026-10-24/plan.md` § 0 rule 2), so the gold and amber slots are simply the
    /// brightest flat colours of the palette, and the hottest is gold, never white.
    ///
    /// Every caller keeps its first-pass shape as the fallback: a missing model logs once and the old primitives draw.
    /// </summary>
    public static class SeanProp
    {
        public const string ResourceFolder = "Models/SeanProps";

        /// <summary>CORE, AMBER, EMBER, CHERRY, DEEP, GLASS, SHEEN, GLINT, STEAM, ASH, DUSK, FAR, SUMMIT, INK, CRUST, RUST.</summary>
        public static readonly Color[] Palette =
        {
            Hex(0xFFC83A), Hex(0xFFA92E), Hex(0xE8452A), Hex(0xD8261C), Hex(0x8E1712), Hex(0x17121A), Hex(0x3A2A35), Hex(0xB3261E),
            Hex(0xC9C3C0), Hex(0x6D6668), Hex(0x2B1520), Hex(0x3B1E2A), Hex(0xFFB347), Hex(0x120C0E), Hex(0x2A1416), Hex(0x5A2A20),
        };

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        private static bool _warned;

        /// <summary>
        /// MAYON AWAKENS v2's mountain in the court: the backdrop's DUSK and FAR (a silhouette against a dusk sky) become warm
        /// basalt browns, so on a sunny court it reads as rock with lava down it and not a black witch's hat (film v5).
        /// </summary>
        public static Color[] CourtMountainPalette
        {
            get
            {
                var p = (Color[])Palette.Clone();
                p[10] = Hex(0x6B3A2E);   // DUSK, the upper cone
                p[11] = Hex(0x8A4A33);   // FAR, the foot
                return p;
            }
        }

        /// <summary>The prop <paramref name="name"/> under <paramref name="parent"/>, dressed in his palette; null if missing.</summary>
        public static GameObject Spawn(string name, Transform parent) => Spawn(name, parent, Palette);

        /// <summary>The prop dressed in <paramref name="palette"/> (sixteen colours in `Palette`'s slot order).</summary>
        public static GameObject Spawn(string name, Transform parent, Color[] palette)
        {
            var source = HeroPropAssets.Load(ResourceFolder, name);
            if (source == null)
            {
                if (!_warned) Debug.LogWarning("[SeanProp] Models/SeanProps/" + name + " is missing; run tools/build_sean_props.py.");
                _warned = true;
                return null;
            }
            var go = Object.Instantiate(source, parent, false);
            go.name = "SeanProp-" + name;
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) { c.enabled = false; Object.Destroy(c); }
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                VfxRenderTag.Attach(r.gameObject);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            ToonSkin.Apply(go, ToonSkin.PersonOutlineWidth, palette ?? Palette);
            return go;
        }

        /// <summary>A named node anywhere under the prop.</summary>
        public static Transform Find(GameObject model, string name) => PaeteProp.Find(model, name);
    }
}
