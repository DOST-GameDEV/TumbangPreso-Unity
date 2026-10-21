using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// STUDY: THE LANDING AND JUMP DUST (`CloudPuffField`), on the bench, so it can be looked at without a match. Left, the
    /// puff a landing kicks up; right, the smaller one a jump leaves; then, half a second on, the landing puff again as
    /// it is with reduced effects on (half its balls). A stand-in the height of a character stands behind for scale.
    /// The puffs are the game's own presets, unchanged: what is filmed here is what a match spawns.
    /// </summary>
    public sealed class FxStudyLandPuff : FxStudyEffect
    {
        private readonly CloudPuffField _field;

        public override float Seconds => 1.2f;
        public override float BloomIntensity => 0f;
        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            focus = new Vector3(0f, 0.38f, 0f); eye = focus + new Vector3(0f, 0.75f, -4.2f); fov = 30f;
        }

        public FxStudyLandPuff(Transform parent) : base("landpuff", parent)
        {
            // A plain post 1.7 m tall, 0.5 m wide: how big a character is beside the dust.
            var plain = Shader.Find("Unlit/Color");
            if (plain != null)
            {
                var grey = new Material(plain) { hideFlags = HideFlags.HideAndDontSave, color = new Color(0.30f, 0.31f, 0.33f) };
                var post = Add("scale-post", FxStudyKit.Square(false), grey, 0);
                grey.renderQueue = 1950;
                post.At.localPosition = new Vector3(-1.1f, 0.85f, 0.5f); post.At.localScale = new Vector3(0.5f, 1.7f, 1f);
                post.Frame(0f, 1f);
            }
            _field = new CloudPuffField(Root);
            var land = CloudPuffField.Puff.Landing(Root.TransformPoint(new Vector3(-1.1f, 0f, 0f)));
            var jump = CloudPuffField.Puff.Jump(Root.TransformPoint(new Vector3(0.5f, 0f, 0f)));
            var calm = CloudPuffField.Puff.Landing(Root.TransformPoint(new Vector3(1.7f, 0f, 0f)));
            calm.Share = 0.5f; calm.Turn = 1.3f;
            _field.Spawn(land, 0.10f); _field.Spawn(jump, 0.10f); _field.Spawn(calm, 0.55f);
        }

        public override void Sample(float age, Vector3 eye) => _field.Hold(age);
    }
}
