using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ HER OWN VOODOO DOLL, NOT THE MONSTER (HERO-10 v3, 2026-09-29, plan 9.8c). The owner on film v6: *"i want the handheld vodoo
    /// to look diff from ult too"*. The ultimate's doll is a fat burlap sack with glowing seams; hers is the doll a witch carries: small,
    /// FLAT and gingerbread-shaped, BLACK cloth (her coat's charcoal) stitched round in magenta, two mismatched buttons for eyes (violet
    /// and crimson), a red thread mouth sewn shut, three pins through its chest with her colours on their heads. It is the doll she holds
    /// out in THE REACH (`VoodooSoulDraw`), in both views.
    ///
    /// Built from lit blocks like the cast, one unit tall, its face on +z, feet at y 0. Mitten stumps, no fingers (the cast's rule).
    /// </summary>
    public static class PhaisterHandVoodoo
    {
        private static readonly Color Cloth = new Color(0.10f, 0.08f, 0.12f, 1f);
        private static readonly Color ClothDark = new Color(0.06f, 0.05f, 0.07f, 1f);
        private static readonly Color Stitch = new Color(0.95f, 0.22f, 0.58f, 1f);
        private static readonly Color Mouth = new Color(0.86f, 0.10f, 0.16f, 1f);
        private static readonly Color PinSteel = new Color(0.80f, 0.80f, 0.86f, 1f);

        /// <summary>The doll, one unit tall, under <paramref name="parent"/> (null for the scene root) on <paramref name="layer"/>.</summary>
        public static GameObject Build(Transform parent, string name, int layer)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.gameObject.layer = layer;
            const float d = 0.16f, f = d * 0.5f + 0.006f;

            // The body: a round-shouldered head, a torso, splayed mitten arms, stubby legs; flat like a cut-out.
            B(root, "Head", new Vector3(0f, 0.74f, 0f), new Vector3(0.5f, 0.42f, d), Cloth, layer);
            B(root, "HeadTop", new Vector3(0f, 0.97f, 0f), new Vector3(0.36f, 0.06f, d), Cloth, layer);
            B(root, "Torso", new Vector3(0f, 0.38f, 0f), new Vector3(0.4f, 0.34f, d), Cloth, layer);
            var armL = B(root, "ArmL", new Vector3(-0.3f, 0.44f, 0f), new Vector3(0.28f, 0.13f, d * 0.9f), Cloth, layer);
            armL.localRotation = Quaternion.Euler(0f, 0f, 24f);
            var armR = B(root, "ArmR", new Vector3(0.3f, 0.44f, 0f), new Vector3(0.28f, 0.13f, d * 0.9f), Cloth, layer);
            armR.localRotation = Quaternion.Euler(0f, 0f, -24f);
            B(root, "LegL", new Vector3(-0.11f, 0.11f, 0f), new Vector3(0.15f, 0.24f, d * 0.9f), ClothDark, layer);
            B(root, "LegR", new Vector3(0.11f, 0.11f, 0f), new Vector3(0.15f, 0.24f, d * 0.9f), ClothDark, layer);

            // The magenta stitching round the head's edge and down the body's middle.
            B(root, "StitchTop", new Vector3(0f, 0.93f, f), new Vector3(0.44f, 0.022f, 0.012f), Stitch, layer, 0.8f);
            B(root, "StitchL", new Vector3(-0.235f, 0.74f, f), new Vector3(0.022f, 0.36f, 0.012f), Stitch, layer, 0.8f);
            B(root, "StitchR", new Vector3(0.235f, 0.74f, f), new Vector3(0.022f, 0.36f, 0.012f), Stitch, layer, 0.8f);
            for (int i = 0; i < 4; i++)
                B(root, "StitchX" + i, new Vector3(0f, 0.25f + i * 0.07f, f), new Vector3(0.09f, 0.018f, 0.012f), Stitch, layer, 0.8f)
                    .localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 35f : -35f);

            // Two mismatched buttons for eyes, and a mouth sewn shut.
            B(root, "ButtonEye", new Vector3(-0.11f, 0.79f, f), new Vector3(0.12f, 0.12f, 0.03f), SkyCircle.Violet, layer, 1.2f);
            B(root, "ButtonHoleA", new Vector3(-0.13f, 0.79f, f + 0.016f), new Vector3(0.02f, 0.02f, 0.01f), ClothDark, layer);
            B(root, "ButtonHoleB", new Vector3(-0.09f, 0.79f, f + 0.016f), new Vector3(0.02f, 0.02f, 0.01f), ClothDark, layer);
            B(root, "ButtonEye2", new Vector3(0.11f, 0.77f, f), new Vector3(0.09f, 0.09f, 0.03f), SkyCircle.Crimson, layer, 1.2f);
            B(root, "Mouth", new Vector3(0f, 0.63f, f), new Vector3(0.22f, 0.024f, 0.014f), Mouth, layer, 0.6f);
            for (int i = 0; i < 4; i++)
                B(root, "MouthStitch" + i, new Vector3(-0.08f + i * 0.053f, 0.63f, f + 0.004f), new Vector3(0.016f, 0.07f, 0.012f), Mouth, layer, 0.6f);

            // Three pins through its chest, heads out the front in her colours, points out the back.
            Color[] heads = { SkyCircle.Crimson, SkyCircle.Violet, Stitch };
            Vector3[] at = { new Vector3(-0.08f, 0.44f, 0f), new Vector3(0.07f, 0.36f, 0f), new Vector3(0.02f, 0.5f, 0f) };
            float[] tilt = { 18f, -14f, 4f };
            for (int i = 0; i < 3; i++)
            {
                var pin = new GameObject("Pin" + i).transform;
                pin.SetParent(root, false);
                pin.gameObject.layer = layer;
                pin.localPosition = at[i];
                pin.localRotation = Quaternion.Euler(tilt[i] * 0.6f, tilt[i], 0f);
                B(pin, "Shaft", Vector3.zero, new Vector3(0.018f, 0.018f, 0.34f), PinSteel, layer);
                B(pin, "Head", new Vector3(0f, 0f, 0.17f), Vector3.one * 0.055f, heads[i], layer, 1.3f);
            }
            return root.gameObject;
        }

        private static Transform B(Transform parent, string name, Vector3 at, Vector3 size, Color colour, int layer, float emission = 0f)
        {
            var t = MarionetteControl.Block(parent, name, at, size, colour, layer, emission);
            t.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return t;
        }
    }
}
