using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    /// <summary>
    /// WHIRLED, AS THE PLAYER IT HAPPENED TO SEES IT (owner, 2026-10-03: *"think abt what the ppl in fpp would see (if they get
    /// hit) i want u to try to communicate wind and whirling better"*). Every Whirled tell was on the body (`WhirledMark`), and
    /// in first person the player's own body is hidden, so once they landed they saw nothing but an icon for 2.5 s.
    ///
    /// Now, on their own screen: THE HIT, streaks rushing out past the lens from the middle for a third of a second; then for
    /// as long as Whirled runs, wind streaks WHIRLING round the edge of their view (never across the middle, where they aim),
    /// gusts flicking across it, and the view itself rolling gently, dizzy, all fading as the status runs out. Drawn in the
    /// camera's own space, so it follows every turn; the roll only in first person and never in reduced effects.
    ///
    /// ⚠️ Presentation only, local to the camera that follows the player: it reads `IsWhirled` and `WhirledLeft` and changes
    /// nothing about the body, the aim or the wire.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class WhirledView : MonoBehaviour
    {
        private const int Swirls = 6, Gusts = 4, Rays = 10, Samples = 14;
        private const float Depth = .5f;
        private CameraRig _rig;
        private UnityEngine.Camera _camera;
        private Transform _host;
        private readonly LineRenderer[] _swirl = new LineRenderer[Swirls];
        private readonly LineRenderer[] _gust = new LineRenderer[Gusts];
        private readonly LineRenderer[] _ray = new LineRenderer[Rays];
        private readonly Vector3[] _points = new Vector3[Samples];
        private float _age = -1f, _hitAge = 99f;
        private bool _was;

        /// <summary>Degrees of dizzy roll for the rig to add about the line of sight this frame (0 when not Whirled).</summary>
        public float Roll { get; private set; }

        public static WhirledView Attach(CameraRig rig)
        {
            var view = rig.GetComponent<WhirledView>();
            if (view == null) view = rig.gameObject.AddComponent<WhirledView>();
            view._rig = rig;
            return view;
        }

        private void Build()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            _host = new GameObject("WhirledView").transform;
            _host.SetParent(transform, false);
            var taper = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(.4f, 1f), new Keyframe(1f, .1f));
            for (int i = 0; i < Swirls; i++) _swirl[i] = Line("WhirledSwirl" + i, i % 2 == 0 ? WindVfx.Core : WindVfx.SheetBody, taper);
            for (int i = 0; i < Gusts; i++) _gust[i] = Line("WhirledGust" + i, WindVfx.Core, taper);
            for (int i = 0; i < Rays; i++) _ray[i] = Line("WhirledRay" + i, i % 2 == 0 ? WindVfx.Core : WindVfx.SheetBody, taper);
        }

        private LineRenderer Line(string name, Color colour, AnimationCurve taper)
        {
            var go = new GameObject(name); go.transform.SetParent(_host, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false; line.positionCount = Samples; line.numCapVertices = 0; line.numCornerVertices = 3;
            line.widthCurve = taper; line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
            line.alignment = LineAlignment.View;
            VfxMaterial.Ghost(line, new Color(colour.r, colour.g, colour.b, .7f), .8f);
            line.enabled = false;
            return line;
        }

        private void LateUpdate()
        {
            var body = _rig != null ? _rig.Following : null;
            bool whirled = body != null && body.IsWhirled;
            if (!whirled && !_was && _hitAge > 1f) { Roll = 0f; return; }
            if (_host == null) Build();
            float dt = Time.deltaTime;
            if (whirled && !_was) { _age = 0f; _hitAge = 0f; }
            _was = whirled;
            if (whirled) _age += dt;
            _hitAge += dt;
            bool calm = WindVfx.Reduced;
            float left = whirled ? Mathf.Clamp01(body.WhirledLeft / Core.StatusRules.WhirledSeconds) : 0f;
            float on = whirled ? Mathf.Clamp01(_age / .15f) * Mathf.Clamp01(left * 3f) : 0f;

            // Lay the frame out at Depth in front of the lens, sized to the view.
            float fov = _camera != null ? _camera.fieldOfView : 70f;
            float halfH = Depth * Mathf.Tan(fov * .5f * Mathf.Deg2Rad), aspect = _camera != null ? _camera.aspect : 16f / 9f;
            float halfW = halfH * aspect;

            // THE WHIRL: streaks circling the edge of the view, each running round its own arc.
            for (int i = 0; i < Swirls; i++)
            {
                var line = _swirl[i];
                if (on <= .01f) { line.enabled = false; continue; }
                float spin = (calm ? .3f : 1f) * _age * 3.4f + i * (Mathf.PI * 2f / Swirls);
                float reach = .82f + .1f * (i % 3);
                for (int k = 0; k < Samples; k++)
                {
                    float u = k / (Samples - 1f);
                    float a = spin + u * .9f;
                    float r = reach - .06f * u;
                    _points[k] = new Vector3(Mathf.Cos(a) * halfW * r, Mathf.Sin(a) * halfH * r, Depth);
                }
                line.SetPositions(_points);
                line.widthMultiplier = .017f * on;
                line.enabled = true;
            }

            // GUSTS flicking across the top and bottom of the view, one every so often.
            for (int i = 0; i < Gusts; i++)
            {
                var line = _gust[i];
                float cycle = Mathf.Repeat(_age * .9f + i * .27f, 1f);
                if (on <= .01f || cycle > .45f) { line.enabled = false; continue; }
                float u0 = cycle / .45f;
                float y = (i % 2 == 0 ? .7f : -.72f) * halfH + .05f * halfH * Mathf.Sin(i * 3f);
                float dir = i < 2 ? 1f : -1f;
                for (int k = 0; k < Samples; k++)
                {
                    float x = Mathf.Lerp(-1.3f, 1.3f, Mathf.Clamp01(u0 * 1.4f - .4f + k / (Samples - 1f) * .4f)) * dir;
                    _points[k] = new Vector3(x * halfW, y + .03f * halfH * Mathf.Sin(x * 6f), Depth);
                }
                line.SetPositions(_points);
                line.widthMultiplier = .011f * on;
                line.enabled = true;
            }

            // THE HIT: streaks rushing outward from the middle past the lens, a third of a second.
            float hit = Mathf.Clamp01(1f - _hitAge / .35f);
            for (int i = 0; i < Rays; i++)
            {
                var line = _ray[i];
                if (hit <= .01f || calm) { line.enabled = false; continue; }
                float a = (i + .3f * Mathf.Sin(i * 7f)) * Mathf.PI * 2f / Rays;
                float travel = 1f - hit;
                for (int k = 0; k < Samples; k++)
                {
                    float r = Mathf.Lerp(.25f + .7f * travel, .55f + 1.0f * travel, k / (Samples - 1f));
                    _points[k] = new Vector3(Mathf.Cos(a) * halfW * r, Mathf.Sin(a) * halfH * r, Depth);
                }
                line.SetPositions(_points);
                line.widthMultiplier = .02f * hit;
                line.enabled = true;
            }

            // DIZZY: the view rolls gently while it runs. The rig applies it in its own first-person pass (`CameraRig`), so it
            // is never stacked on a frame the rig did not write; a roll about the line of sight leaves the aim where it was.
            Roll = calm || on <= .01f ? 0f : 5f * on * Mathf.Sin(_age * 4.2f) + 2f * on * Mathf.Sin(_age * 2.3f + 1f);
        }
    }
}
