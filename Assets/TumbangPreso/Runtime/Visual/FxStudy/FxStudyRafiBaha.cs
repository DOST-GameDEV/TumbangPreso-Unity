using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// BAHA ALONE ON THE DARK BENCH: `RafiBahaBody`, played from the rule's own numbers. `rafibahapeak` is the match camera (no
    /// cover, following the body, framed to the owner's reference); the others have a dark block stopping two lanes.
    /// </summary>
    public sealed class FxStudyRafiBaha : FxStudyEffect
    {
        public enum View { ThreeQuarter, Side, Behind, Peak, Day }
        private const float Run = 12f, CoverAt = 6.5f, CoverSide = 1.875f, CoverHalf = 0.85f;
        private static Material _block;
        private readonly View _view;
        private readonly float _seconds, _maxEnd;
        private readonly RafiBahaBody _body;

        public FxStudyRafiBaha(Transform parent, View view = View.ThreeQuarter) : base("rafibaha", parent)
        {
            _view = view;
            float r = RafiRules.BahaHalfWidth;
            bool cover = view != View.Peak;
            var ends = new float[9];
            for (int i = 0; i < 9; i++)
            {
                float side = Mathf.Lerp(-r, r, i / 8f);
                bool blocked = cover && side > CoverSide - CoverHalf && side < CoverSide + CoverHalf;
                ends[i] = blocked ? CoverAt - 0.5f - 0.06f : Run;
                _maxEnd = Mathf.Max(_maxEnd, ends[i]);
            }
            // Long enough to see the puddles it leaves dry.
            _seconds = RafiRules.BahaDuration(_maxEnd) + 6f;
            _body = new RafiBahaBody(Root, ends);
            if (!cover) return;
            // The cover: a plain dark block, drawn flat so it adds no light of its own to the bench.
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "cover"; block.hideFlags = HideFlags.DontSave;
            Object.DestroyImmediate(block.GetComponent<Collider>());
            block.transform.SetParent(Root, false);
            block.transform.localPosition = new Vector3(CoverSide, 0.5f, CoverAt);
            block.transform.localScale = new Vector3(CoverHalf * 2f, 1f, 1f);
            if (_block == null)
            {
                var unlit = Shader.Find("Unlit/Color");
                if (unlit != null) { _block = new Material(unlit) { hideFlags = HideFlags.HideAndDontSave }; _block.color = new Color(0.30f, 0.30f, 0.31f); }
            }
            if (_block != null) block.GetComponent<MeshRenderer>().sharedMaterial = _block;
        }

        public override float Seconds => _seconds;
        public override float BloomIntensity => 0.30f;
        // The reference's floor, as measured: near-black with a cool cast and a clear pale grid.
        public override Color FloorColour => _view == View.Day ? new Color(0.60f, 0.48f, 0.40f) : new Color(0.094f, 0.118f, 0.165f);
        public override Color FloorLine => _view == View.Day ? new Color(0.52f, 0.41f, 0.34f) : new Color(0.30f, 0.34f, 0.38f);
        public override float FloorDim => 0.85f;
        public override float FloorDimAt => 30f;
        public override float FloorCell => 1.2f;

        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            float t = age - 0.2f, follow = Mathf.Clamp((t - RafiRules.BahaWarning) * RafiRules.BahaSpeed, 0f, _maxEnd);
            switch (_view)
            {
                // The road's own colour and the player's height, looking along the run: the puddles left behind.
                case View.Day: focus = new Vector3(0f, 0.2f, 6f); eye = new Vector3(1.2f, 1.6f, -1.5f); fov = 58f; return;
                case View.Side: focus = new Vector3(0f, 0.35f, 6f); eye = focus + new Vector3(12.5f, 1.8f, 0f); fov = 42f; return;
                case View.Behind: focus = new Vector3(0f, 0.2f, 5.5f); eye = new Vector3(0.3f, 1.55f, -0.6f); fov = 62f; return;
                case View.Peak: focus = new Vector3(0f, 0.1f, Mathf.Max(follow - 3.2f, 0.6f)); eye = focus + new Vector3(6.4f, 8.6f, 6.4f); fov = 46f; return;
                default: focus = new Vector3(0f, 0.2f, 6f); eye = focus + new Vector3(6.5f, 9.5f, 6.5f); fov = 46f; return;
            }
        }

        public override void Sample(float age, Vector3 eye) => _body.Pose(age - 0.2f);
    }
}
