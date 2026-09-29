using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ WHAT WORKS THE VOODOO DOLL: A MARIONETTE CONTROL AND THE GLOVES THAT HOLD IT (HERO-10 v3, cutscene v7, 2026-09-29). The
    /// owner on film v6: *"I want the portal (eye) to be diff from the thing that controls it too"*, then a reference photo (white-gloved
    /// hands working a wooden marionette control) with *"like this"* and *"i want the wires on its head to look like this"*.
    ///
    /// So THE CIRCLE is only the portal the monster comes out of, and its wires hang from a wooden CONTROL: a main bar across its
    /// shoulders (a wire to each mitten hand at its ends) and a short head bar across it (a wire to the crown at its front, one to the
    /// back at its rear), the attachment points lit in her violet. In the cutscene two big white MITTEN GLOVES (no fingers or thumbs,
    /// the cast's rule) reach down out of the eye and work it; in play the control hangs over the doll's head on its own for the round
    /// (`VoodooSkyCircle`). ⚠️ Neither is hers: she never controls the doll (plan 9.2).
    ///
    /// Built from lit blocks like the cast, in the control's own local space: +x the doll's right, +z its front, the bars at y 0.
    /// </summary>
    public static class MarionetteControl
    {
        public const float BarHalf = 0.62f, HeadBarHalf = 0.4f;
        /// <summary>Its size over the doll (the doll is Paete's size), and how high over its crown it hangs in play.</summary>
        public const float DollScale = 1.5f, AboveCrown = 1.3f, WireLength = 1.3f;
        public static readonly Color Wood = new Color(0.50f, 0.31f, 0.17f, 1f);
        public static readonly Color WoodDark = new Color(0.33f, 0.19f, 0.10f, 1f);
        public static readonly Color Glove = new Color(0.93f, 0.92f, 0.95f, 1f);
        public static readonly Color Cuff = new Color(0.78f, 0.76f, 0.84f, 1f);
        public static readonly Color Sleeve = new Color(0.12f, 0.05f, 0.16f, 1f);

        /// <summary>The wire points, in the control's space: 0 crown (head bar, front), 1 left hand, 2 right hand, 3 back (head bar, rear).</summary>
        public static Vector3 Anchor(int index)
        {
            switch (index)
            {
                case 1: return new Vector3(-BarHalf, -0.06f, 0f);
                case 2: return new Vector3(BarHalf, -0.06f, 0f);
                case 3: return new Vector3(0f, -0.06f, -HeadBarHalf);
                default: return new Vector3(0f, -0.06f, HeadBarHalf * 0.8f);
            }
        }

        /// <summary>The control: two crossed wooden bars with end caps, the wire points lit. Returns its root (scale it as a whole).</summary>
        public static Transform BuildControl(Transform parent, int layer)
        {
            var root = new GameObject("MarionetteControl").transform;
            root.SetParent(parent, false);
            root.gameObject.layer = layer;
            Block(root, "MainBar", Vector3.zero, new Vector3(BarHalf * 2f + 0.1f, 0.075f, 0.09f), Wood, layer);
            Block(root, "HeadBar", new Vector3(0f, 0.07f, 0f), new Vector3(0.085f, 0.075f, HeadBarHalf * 2f + 0.08f), Wood, layer);
            Block(root, "Peg", new Vector3(0f, 0.14f, 0f), new Vector3(0.05f, 0.1f, 0.05f), WoodDark, layer);
            foreach (float x in new[] { -1f, 1f })
                Block(root, "Cap", new Vector3(x * (BarHalf + 0.06f), 0f, 0f), new Vector3(0.05f, 0.1f, 0.12f), WoodDark, layer);
            for (int i = 0; i < 4; i++)
                Block(root, "WirePoint" + i, Anchor(i) + Vector3.up * 0.02f, Vector3.one * 0.07f, SkyCircle.Violet, layer, 1.6f);
            return root;
        }

        /// <summary>
        /// A mitten glove with its cuff and a sleeve running back into the dark: the mitten along +z (it grips a bar under its palm),
        /// the sleeve along -z. <paramref name="sleeve"/> is the sleeve's length in metres before scale.
        /// </summary>
        public static Transform BuildGlove(Transform parent, int layer, float sleeve)
        {
            var root = new GameObject("PuppeteerGlove").transform;
            root.SetParent(parent, false);
            root.gameObject.layer = layer;
            // The white glows a little from inside: in her night a plain white glove reads grey.
            Block(root, "Mitten", new Vector3(0f, 0f, 0.12f), new Vector3(0.3f, 0.16f, 0.34f), Glove, layer, 0.35f);
            Block(root, "MittenTip", new Vector3(0f, -0.02f, 0.33f), new Vector3(0.26f, 0.13f, 0.1f), Glove, layer, 0.35f);
            Block(root, "Knuckles", new Vector3(0f, 0.07f, 0.2f), new Vector3(0.28f, 0.05f, 0.14f), Glove, layer, 0.35f);
            Block(root, "Cuff", new Vector3(0f, 0.01f, -0.1f), new Vector3(0.36f, 0.22f, 0.12f), Cuff, layer, 0.25f);
            Block(root, "CuffStitch", new Vector3(0f, 0.125f, -0.1f), new Vector3(0.3f, 0.012f, 0.03f), SkyCircle.Crimson, layer, .8f);
            Block(root, "Sleeve", new Vector3(0f, 0.01f, -0.16f - sleeve * 0.5f), new Vector3(0.24f, 0.18f, sleeve), Sleeve, layer);
            return root;
        }

        internal static Transform Block(Transform parent, string name, Vector3 at, Vector3 size, Color colour, int layer, float emission = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            if (collider != null) { collider.enabled = false; Object.Destroy(collider); }
            go.layer = layer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            VfxMaterial.Solid(r, colour, emission);
            return go.transform;
        }
    }
}
