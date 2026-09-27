using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ PHAISTER'S MODELLED PROPS (HERO-10): `tools/build_phaister_props.py` types the butterfly, moth, beetle, manika and
    /// hat pin part by part into `Resources/Models/PhaisterProps`; this loads one and dresses it the way the cast is dressed
    /// (`ToonSkin`, the ink outline) in HER sixteen-slot palette. The slot numbers and hexes match the builder's table.
    ///
    /// ⚠️ THE INSECTS TAKE A THINNER INK. The cast's outline (`ToonSkin.PersonOutlineWidth`, about 1.1 cm) round a 5 mm wing
    /// plate drew the wing as a black blob with a coloured centre; `InsectOutlineWidth` keeps the magenta edge readable.
    /// </summary>
    public static class PhaisterProp
    {
        public const string ResourceFolder = "Models/PhaisterProps";
        public const int WingBlack = 0, WingVein = 1, WingEdge = 2, BodyGlow = 3, MothWing = 4, MothVein = 5, MothEdge = 6,
                         Shell = 7, Ink = 8, Cloth = 9, ClothDark = 10, PinGold = 11, PinLilac = 12, PinCrimson = 13, Bone = 14, Fuzz = 15;

        public const float InsectOutlineWidth = 0.0045f;

        public static readonly Color[] Palette =
        {
            Hex(0x1A1020), Hex(0x07040A), Hex(0xE0287E), Hex(0xFF6AB8), Hex(0x6A3AA8), Hex(0x3E1F6E), Hex(0xC9A2F0), Hex(0x4A2A6A),
            Hex(0x14101C), Hex(0xE0A078), Hex(0xA8683C), Hex(0xF8B824), Hex(0x9838D8), Hex(0x8C1424), Hex(0xF2E6DA), Hex(0x9C78C8),
        };

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        /// <summary>
        /// Her palette with the manika's cloth in <paramref name="cloth"/>: the doll wearing its victim's colour (owner, plan
        /// question 4: *"yes"*). The stitches, X eyes and pin stay hers. `ToonSkin` caches per palette, so one array per victim.
        /// </summary>
        public static Color[] ClothedIn(Color cloth)
        {
            var p = (Color[])Palette.Clone();
            p[Cloth] = cloth;
            p[ClothDark] = Color.Lerp(cloth, Color.black, 0.35f);
            return p;
        }

        /// <summary>The prop <paramref name="name"/> under <paramref name="parent"/>, dressed; null if it is missing.</summary>
        public static GameObject Spawn(string name, Transform parent, Color[] palette = null, float width = ToonSkin.PersonOutlineWidth)
        {
            var source = HeroPropAssets.Load(ResourceFolder, name);
            if (source == null)
            {
                Debug.LogWarning("[PhaisterProp] Models/PhaisterProps/" + name + " is missing; run tools/build_phaister_props.py.");
                return null;
            }
            var go = Object.Instantiate(source, parent, false);
            go.name = "PhaisterProp-" + name;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) { c.enabled = false; Kill(c); }
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                VfxRenderTag.Attach(r.gameObject);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            ToonSkin.Apply(go, width, palette ?? Palette);
            return go;
        }

        public static Transform Find(GameObject model, string name) => PaeteProp.Find(model, name);

        /// <summary>
        /// ⚠️⚠️ THE COURT UNDER <paramref name="at"/>, THE SURFACE PLAYERS STAND ON (`Slipper.GroundY`, the highest thing under it that
        /// is not a body, a slipper or the can). HERO-10 film v9: every flat mark of hers placed with `VfxShapes.GroundPoint` or
        /// draped with `VfxShapes.DrapeToGround` was invisible on Bayan Plaza (the aim sigil's rings, the swarm's shove ring, OMEN's
        /// ring and dim, the pin's sweep) while everything standing drew: both prefer the map's named floor groups, and the plaza's
        /// paving stands above that floor, so a mark a few centimetres over it was under the tiles. Paete's veins hit the same thing
        /// (`HERO_KIT_METHOD.md` section 8: flat effects sit on `Slipper.GroundY`). Her flat marks are placed on this and not draped.
        /// </summary>
        public static Vector3 OnCourt(Vector3 at) => new Vector3(at.x, Slipper.GroundY(at + Vector3.up * 0.5f), at.z);

        /// <summary>
        /// ⚠️ FADE AN EFFECT MATERIAL BY WRITING BOTH COLOUR PROPERTIES. `Material.color` is `_Color` on one pipeline and the
        /// ghost template reads `_BaseColor` on the other, so writing only `.color` left her OMEN ring, sigils and dim stuck at
        /// the alpha 0 they were created with (HERO-10 film v3: none of them showed). `AmihanVfx` writes both for the same reason.
        /// </summary>
        public static void SetAlpha(Material m, float alpha)
        {
            if (m == null) return;
            var c = m.color; c.a = alpha; m.color = c;
            if (m.HasProperty("_BaseColor")) { var b = m.GetColor("_BaseColor"); b.a = alpha; m.SetColor("_BaseColor", b); }
        }

        private static void Kill(Object o)
        {
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }
    }
}
