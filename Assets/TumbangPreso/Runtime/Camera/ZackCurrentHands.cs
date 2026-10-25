using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    // Current lives on the bare forearms. No magnets, coins, tools or floating props.
    public sealed class ZackCurrentHands : ViewmodelArms.HandCompanion
    {
        private Transform _root;
        private readonly Transform[] _arms = new Transform[2];
        private readonly LineRenderer[] _ink = new LineRenderer[4], _arcs = new LineRenderer[4];
        private float _clock;

        public override bool Build(ViewmodelArms arms)
        {
            _arms[0] = arms.LeftHandForProps(); _arms[1] = arms.RightHandForProps();
            if (_arms[0] == null || _arms[1] == null) return false;
            _root = new GameObject("~HandCompanion Zack current").transform;
            _root.SetParent(arms.transform, false);
            for (int i = 0; i < 4; i++)
            {
                _ink[i] = ZackFx.Line(_root, "CurrentInk" + i, 7, .027f, ZackFx.Ink, 0);
                _arcs[i] = ZackFx.Line(_root, "Current" + i, 7, .012f, ZackFx.Core, 1.2f);
                foreach (var line in new[] { _ink[i], _arcs[i] })
                { line.useWorldSpace = false; line.gameObject.layer = arms.gameObject.layer; line.enabled = false; }
            }
            return true;
        }

        public override void Step(ViewmodelArms arms, in ViewmodelArms.HandMood mood, float dt)
        {
            _clock += dt;
            bool powered = mood.Overclock > 0 || mood.Momentum >= 3 || mood.Casting;
            bool stutter = mood.Overclock > 0 && mood.Overclock < 5 && Mathf.Repeat(_clock, .23f) < .07f;
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            int strike = Mathf.FloorToInt(_clock / .085f);
            for (int i = 0; i < 4; i++)
            {
                int side = i / 2;
                bool visible = powered && !mood.Tagged && !stutter && (reduced || ((strike + i) % 3 != 0));
                _ink[i].enabled = _arcs[i].enabled = visible;
                if (!visible) continue;
                float sign = side == 0 ? 1 : -1;
                float x = (i % 2 == 0 ? -.10f : .10f) * sign;
                var a = _root.InverseTransformPoint(_arms[side].TransformPoint(new Vector3(x, .48f, .33f)));
                var b = _root.InverseTransformPoint(_arms[side].TransformPoint(new Vector3(x + .045f * sign, .73f, .27f)));
                // Keep the current along the outer hand; it never spans the aiming space or the carried shoe.
                ZackFx.Jag(_arcs[i], a, b, reduced ? .006f : .018f, strike * 17 + i);
                for (int p = 0; p < _arcs[i].positionCount; p++) _ink[i].SetPosition(p, _arcs[i].GetPosition(p));
            }
        }

        public override void Destroy() { if (_root != null) HandCompanionProp.Kill(_root.gameObject); }
    }
}
