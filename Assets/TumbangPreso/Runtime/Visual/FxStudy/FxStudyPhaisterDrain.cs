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

    /// <summary>
    /// VOODOO, HER PASSIVE, ON THE BENCH (`PhaisterPassiveLook`): both capsules run the same way, 4.5 m/s times the rule's own
    /// shares, so the gap opening between them is the rule. A mark lands at 1 s and lives for 3 s. On the pale floor.
    /// </summary>
    public sealed class FxStudyPhaisterPassive : FxStudyEffect
    {
        private const float MarkAt = 1.0f, MarkLife = 3.0f, Speed = 4.5f, Height = 1.6f;
        private readonly PhaisterPassiveLook _them, _her;
        private readonly Transform _themBody, _herBody;
        private float _last;

        public override float Seconds => 5.0f;
        public override float FloorCell => 0.75f;
        public override Color FloorColour => new Color(0.80f, 0.76f, 0.68f);
        public override Color FloorLine => new Color(0.70f, 0.66f, 0.58f);
        public override float FloorDim => 0.95f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        { float x = _still ? -1.0f : -5.2f + Speed * age; eye = new Vector3(x, 3.4f, -6.6f); focus = new Vector3(x, 0.5f, 0f); fov = 40f; }

        private readonly bool _still;

        /// <summary>`still`: both stand, 3.6 m apart, which is the case where a speed effect has no motion to lean on.</summary>
        public FxStudyPhaisterPassive(Transform parent, bool still = false) : base("phaisterpassive", parent)
        {
            _still = still;
            _themBody = Capsule(); _herBody = Capsule();
            _them = new PhaisterPassiveLook(Root);
            _her = new PhaisterPassiveLook(Root);
        }

        private Transform Capsule()
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            c.hideFlags = HideFlags.DontSave;
            Object.DestroyImmediate(c.GetComponent<Collider>());
            c.transform.SetParent(Root, false);
            c.transform.localScale = new Vector3(0.5f, Height * 0.5f, 0.5f);
            c.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { hideFlags = HideFlags.DontSave, color = new Color(0.42f, 0.42f, 0.44f) };
            return c.transform;
        }

        /// <summary>How far a runner has come at `age`, in closed form: a bench effect keeps no state between frames.</summary>
        private static float Ran(float age, float share)
        {
            float during = Mathf.Clamp(age - MarkAt, 0f, MarkLife);
            return Speed * (age - during) + Speed * share * during;
        }

        public override void Sample(float age, Vector3 eye)
        {
            float dt = Mathf.Max(1f / 240f, age - _last); _last = age;
            float mark = age - MarkAt;
            bool on = mark >= 0f && mark < MarkLife;
            float themShare = VoodooRules.PassiveSpeedScale(false), herShare = VoodooRules.PassiveSpeedScale(true);
            Vector3 themFeet = _still ? new Vector3(0.6f, 0f, 1.1f) : new Vector3(-7.25f + Ran(age, themShare) + 4f, 0f, 1.1f), herFeet = _still ? new Vector3(-2.8f, 0f, -0.2f) : new Vector3(-7.25f + Ran(age, herShare) + 2.6f, 0f, -1.1f);
            _themBody.position = themFeet + Vector3.up * Height * 0.5f;
            _herBody.position = herFeet + Vector3.up * Height * 0.5f;
            Vector3 themMoving = _still ? Vector3.zero : Vector3.right * Speed * (on ? themShare : 1f);
            _them.PoseVictim(Mathf.Max(0f, mark), themFeet, Vector3.right, themMoving, on, false, dt);
            _her.PoseHaste(Mathf.Max(0f, mark), herFeet, themFeet, _still ? Vector3.zero : Vector3.right * Speed * (on ? herShare : 1f), Vector3.right, on, dt);
        }
    }
}
