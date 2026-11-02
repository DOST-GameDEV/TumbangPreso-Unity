using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// HER SKY CIRCLE ON THE BENCH, alone, at its real size and height over a 1.6 m stand-in for the doll, on its real clock:
    /// open to 3.0 s, 0.8 s to close, the same sums as `VoodooSkyCircle`. In a match it arrives already open at
    /// `SkyCircle.FullyOpenAge`, because the cutscene opened it. "phaistersky" sees it from a player's height across the court
    /// on the dark floor; "phaisterskyday" against a daylight blue; "phaisterskyunder" from right under it.
    /// </summary>
    public sealed class FxStudyPhaisterSky : FxStudyEffect
    {
        public enum View { Court, Day, Under }
        private readonly View _view;
        private readonly PhaisterSkySeal _seal;
        private static readonly Vector3 Feet = Vector3.zero;

        public override float Seconds => 4.6f;
        public override Color FloorColour => _view == View.Court ? new Color(0.165f, 0.170f, 0.165f) : new Color(0.545f, 0.705f, 0.875f);
        public override Color FloorLine => FloorColour;
        public override float FloorDim => _view == View.Court ? 0.45f : 1f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            if (_view == View.Under) { eye = new Vector3(0.8f, 1.5f, -3.4f); focus = new Vector3(0f, 7f, 0.4f); fov = 75f; return; }
            eye = new Vector3(1.5f, 1.7f, -15.0f); focus = new Vector3(0f, 4.2f, 0f); fov = 48f;
        }

        public FxStudyPhaisterSky(Transform parent, View view) : base("phaistersky", parent)
        {
            _view = view;
            Stand(Root, Feet, 1.6f);
            _seal = new PhaisterSkySeal(Root, 0);
        }

        internal static GameObject Stand(Transform root, Vector3 at, float height)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            c.hideFlags = HideFlags.DontSave;
            Object.DestroyImmediate(c.GetComponent<Collider>());
            c.transform.SetParent(root, false);
            c.transform.position = at + Vector3.up * height * 0.5f;
            c.transform.localScale = new Vector3(0.6f, height * 0.5f, 0.6f);
            c.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { hideFlags = HideFlags.DontSave, color = new Color(0.42f, 0.42f, 0.44f) };
            return c;
        }

        public override void Sample(float age, Vector3 eye)
        {
            float a = age - 0.1f;
            float shut = Mathf.Clamp01((a - VoodooSkyCircle.OpenSeconds) / VoodooSkyCircle.DrawInSeconds);
            float opening = shut > 0f ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(shut / 0.35f)) : -1f;
            float scale = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((shut - 0.25f) / 0.75f));
            _seal.Pose(a, Feet + Vector3.up * VoodooSkyCircle.Height, Feet, scale, a * 8f * Mathf.Deg2Rad, shut >= 1f ? 0f : 1f, opening, -1f, 1f - shut);
        }
    }

    /// <summary>
    /// THE DOLL'S RULES IN ORDER, on a 1.6 m stand-in for it and one for her 5 m away, from a game camera. The wake, its retinue, a
    /// point paid to her at 3 s, a tag at 4.6 s and its whole 5 s stun at 1x, and getting up. "phaisterdollday" on a pale floor.
    /// </summary>
    public sealed class FxStudyPhaisterDoll : FxStudyEffect
    {
        private const float Stun = 5f, ScoreAt = 3f, TagAt = 4.6f;
        private static readonly Vector3 Feet = Vector3.zero, Her = new Vector3(-4.4f, 0f, -1.8f);
        private readonly bool _day;
        private readonly PhaisterDollLook _look;
        private readonly GameObject _body;
        private PhaisterDollExit _exit;

        public override float Seconds => TagAt + Stun + 1.6f + PhaisterDollExit.Seconds;
        public override float FloorCell => 0.75f;
        public override bool ChequeredFloor => !_day;
        public override Color FloorColour => new Color(0.80f, 0.76f, 0.68f);
        public override Color FloorLine => new Color(0.70f, 0.66f, 0.58f);
        public override float FloorDim => 0.95f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        { eye = new Vector3(1.6f, 3.3f, -6.0f); focus = new Vector3(-1.2f, 0.9f, 0f); fov = 44f; }

        public FxStudyPhaisterDoll(Transform parent, bool day) : base("phaisterdoll", parent)
        {
            _day = day;
            _body = FxStudyPhaisterSky.Stand(Root, Feet, 1.6f);
            FxStudyPhaisterSky.Stand(Root, Her, 1.6f);
            _look = new PhaisterDollLook(Root);
        }

        public override void Sample(float age, Vector3 eye)
        {
            float clock = Mathf.Max(age - 0.1f, 0f);
            // The end of the round, after it has got up: the body and its moths go, the exit plays from where it stood.
            float end = age - (TagAt + Stun + 1.6f);
            if (end >= 0f)
            {
                if (_exit == null) { _exit = PhaisterDollLook.PlayExit(Feet); _exit.transform.SetParent(Root, true); _exit.enabled = false; _look.Destroy(); _body.SetActive(false); }
                _exit.Step(PhaisterDollExit.StartAge + end);
                return;
            }
            _look.Pose(clock, Feet, Feet + Vector3.up * 2.15f, age >= TagAt ? age - TagAt : -1f, Stun, age >= ScoreAt ? age - ScoreAt : -1f, Her, eye);
        }
    }
}
