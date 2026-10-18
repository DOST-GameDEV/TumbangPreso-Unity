using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// THE BOARD FOR BASTION b4's FIELD: three candidates at the same moment from the same cameras, before one is filmed. The
    /// owner, of b3: "polish it more. the field is really ugly". All three drop b3's frame; they differ in what the heat and
    /// the earth in it look like (`FxStudyBastionHeat.shader`, `_Mode`):
    ///   * LICKS: thin tongues of heat off the slabs, hot low, cooling to a smoky red and eaten as they rise, sparks and
    ///     grit through them;
    ///   * SHIMMER AND EMBERS: almost clear, streaks of rising air, sparks and grit drifting up;
    ///   * MAGMA VEIL: a thin rising skin of cooling magma, hot cracks and a near-clear crust.
    /// All three stand on the same b4 slabs with the same six carved stones held up along the zone's sides and top, so
    /// only the field differs.
    /// Each is held at 2.6 s after the start (0.15 s after the first slipper struck it, so the strike shows too).
    /// A picture every tenth of a second: 0.0 to 0.2 the three from the thrower's side, 0.3 to 0.5 the three from behind him.
    /// </summary>
    public sealed class FxStudyDanteBastionBoard : FxStudyEffect
    {
        private static readonly FxStudyDanteBastion.Look[] Candidates = { FxStudyDanteBastion.Look.Licks, FxStudyDanteBastion.Look.Shimmer, FxStudyDanteBastion.Look.Veil };
        private const float Held = 2.6f;
        private readonly FxStudyDanteBastion[] _walls = new FxStudyDanteBastion[3];

        public FxStudyDanteBastionBoard(Transform parent) : base("dantebastionboard", parent)
        {
            // The three walls hang under this board's root and go with it. (Their few small materials are not this board's to
            // free, `Dispose` not being virtual; a board is filmed a handful of times, in the editor only.)
            for (int k = 0; k < 3; k++) _walls[k] = new FxStudyDanteBastion(Root, Candidates[k]);
        }

        public override float Seconds => 0.6f;
        public override Color FloorColour => new Color(0.200f, 0.200f, 0.212f);
        public override Color FloorLine => new Color(0.150f, 0.150f, 0.160f);
        public override float FloorDim => 0.55f;
        public override float FloorDimAt => 12f;
        public override float BloomIntensity => 0.30f;

        private static int ShotOf(float age) => Mathf.Clamp(Mathf.FloorToInt(age * 10f + 0.001f), 0, 5);

        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            bool behind = ShotOf(age) >= 3;
            focus = new Vector3(-0.4f, 1.25f, 0.0f);
            float a = Mathf.Deg2Rad * (behind ? -150f : -30f);
            eye = focus + new Vector3(Mathf.Cos(a) * 6.4f, 0.8f, Mathf.Sin(a) * 6.4f);
            fov = 40f;
        }

        public override void Sample(float age, Vector3 eye)
        {
            int shown = ShotOf(age) % 3;
            for (int k = 0; k < 3; k++)
            {
                _walls[k].Root.gameObject.SetActive(k == shown);
                if (k == shown) _walls[k].Sample(Held, eye);
            }
        }
    }
}
