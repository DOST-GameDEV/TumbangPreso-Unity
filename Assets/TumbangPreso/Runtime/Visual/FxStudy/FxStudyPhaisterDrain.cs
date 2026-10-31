using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// CURSE: DRAIN ON THE BENCH (`PhaisterDrainLook`), on 1.6 m grey capsules for the victim and for her about 5 m away, from a
    /// game camera's height and angle, at the rule's own 1x timing throughout. The very class the game draws, so what passes here
    /// is what ships. "phaisterdrain" on the dark floor, "phaisterdrainday" on a pale warm floor like the court's paving,
    /// "phaisterdrainabove" from high, "phaisterdrainbreak" with the reach broken 1.2 s in.
    /// </summary>
    public sealed class FxStudyPhaisterDrain : FxStudyEffect
    {
        public enum Variant { Bench, Day, Above, Break }
        private static readonly Vector3 Victim = Vector3.zero, Her = new Vector3(-4.6f, 0f, -2.4f);
        private const float Height = 1.6f, Pre = 0.3f;
        private readonly PhaisterDrainLook _look;
        private readonly Variant _variant;

        public override float Seconds => _variant == Variant.Break ? Pre + 2.6f : Pre + PhaisterDrainLook.End + 0.15f;
        public override float FloorCell => 0.75f;
        public override Color FloorColour => _variant == Variant.Day ? new Color(0.80f, 0.76f, 0.68f) : base.FloorColour;
        public override Color FloorLine => _variant == Variant.Day ? new Color(0.70f, 0.66f, 0.58f) : base.FloorLine;
        public override float FloorDim => _variant == Variant.Day ? 0.95f : base.FloorDim;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            eye = _variant == Variant.Above ? new Vector3(-1.0f, 8.0f, -4.0f) : new Vector3(1.6f, 3.0f, -6.6f);
            focus = new Vector3(-1.0f, 1.25f, -0.3f);
            fov = 44f;
        }

        public FxStudyPhaisterDrain(Transform parent, Variant variant) : base("phaisterdrain-" + variant, parent)
        {
            _variant = variant;
            foreach (var at in new[] { Victim, Her })
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                c.hideFlags = HideFlags.DontSave;
                Object.DestroyImmediate(c.GetComponent<Collider>());
                c.transform.SetParent(Root, false);
                c.transform.position = at + Vector3.up * Height * 0.5f;
                c.transform.localScale = new Vector3(0.5f, Height * 0.5f, 0.5f);
                c.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { hideFlags = HideFlags.DontSave, color = new Color(0.42f, 0.42f, 0.44f) };
            }
            _look = new PhaisterDrainLook(Root);
        }

        public override void Sample(float age, Vector3 eye)
        {
            float t = age - Pre;
            Vector3 toVictim = (Victim - Her).normalized;
            Vector3 hand = Her + Vector3.up * 1.15f + toVictim * 0.38f;
            _look.Pose(t, _variant == Variant.Break ? 1.2f : -1f, Victim + Vector3.up * Height, hand, eye, false);
        }
    }
}
