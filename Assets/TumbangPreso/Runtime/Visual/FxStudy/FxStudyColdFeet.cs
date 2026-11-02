using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// COLD FEET ALONE ON THE BENCH (`CheskaColdFeetLook`), on a level floor and a clock cut short: what the look is, with no
    /// map and no body, from the caster's height, from three quarters and from above. The hold in a match is 7.5 s and
    /// nothing changes in the middle of it but the sheen, so the bench plays 3 s.
    /// </summary>
    public sealed class FxStudyColdFeet : FxStudyEffect
    {
        public enum View { Three, Eye, Above }
        private const float Lead = 0.3f, Life = 3f;
        private readonly CheskaColdFeetLook _look;
        private readonly View _view;

        public FxStudyColdFeet(Transform parent, View view) : base("coldfeet", parent)
        {
            _view = view;
            _look = CheskaColdFeetLook.Build(Root, Core.CryoRules.ColdFeetRadius, -1, true);
        }

        public override float Seconds => Lead + Life + 0.1f;
        public override Color FloorColour => new Color(0.30f, 0.27f, 0.25f);
        public override Color FloorLine => new Color(0.25f, 0.23f, 0.21f);
        public override float FloorDim => 0.7f;
        public override float FloorDimAt => 30f;

        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            switch (_view)
            {
                case View.Eye: eye = new Vector3(0f, 1.25f, -4.9f); focus = new Vector3(0f, 0.1f, 0f); fov = 65f; break;
                case View.Above: eye = new Vector3(0f, 9f, -0.3f); focus = Vector3.zero; fov = 42f; break;
                default: eye = new Vector3(3.2f, 3.0f, -5.4f); focus = new Vector3(0f, 0.1f, 0f); fov = 48f; break;
            }
        }

        public override void Sample(float age, Vector3 eye)
        {
            bool on = age >= Lead;
            foreach (var r in _look.Parts) if (r != null) r.enabled = on;
            _look.Pose(age - Lead, Life);
        }
    }
}
