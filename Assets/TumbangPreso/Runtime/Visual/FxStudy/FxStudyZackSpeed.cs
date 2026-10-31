using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// MOMENTUM AND OVERCLOCK ON THE BENCH (`ZackMomentumPaint`, `ZackOverclockPaint`), the very classes the game draws, on a
    /// 1.5 m stand-in that runs an S at the rule's own speeds: stands, runs and stops, Momentum counted by `ZackRules` itself. The
    /// camera trails him at a game camera's height. "day" is the pale warm floor, like the court's paving, with no bloom.
    /// </summary>
    public sealed class FxStudyZackSpeed : FxStudyEffect
    {
        public enum Show { Momentum, Overclock, OverclockEnd, Zap }
        private const float Height = 1.5f;
        private readonly Show _show;
        private readonly bool _day;
        private readonly Transform _body;
        private readonly ZackMomentumPaint _momentum;
        private readonly ZackOverclockPaint _overclock;
        private readonly ZackZapPaint _zap;
        private float _last, _seconds, _stopped, _along;
        private Vector3 _feet;
        // The bench's cost line: the most drawn at once and what a frame's posing took.
        private int _peakDraws, _frames;
        private long _ticks, _alloc;
        private bool _reported;

        private float StartAt => _show == Show.OverclockEnd ? 0f : 0.4f;
        private float StopAt => _show == Show.OverclockEnd ? 8.0f : _show == Show.Overclock ? 7.0f : 4.0f;
        private float FuelAtStart => _show == Show.OverclockEnd ? 6.0f : ZackRules.OverclockSeconds;
        public override float Seconds => _show == Show.Zap ? 4.6f : _show == Show.OverclockEnd ? 8.0f : StopAt + 1.1f;
        public override float FloorCell => 0.75f;
        public override Color FloorColour => _day ? new Color(0.80f, 0.76f, 0.68f) : base.FloorColour;
        public override Color FloorLine => _day ? new Color(0.70f, 0.66f, 0.58f) : base.FloorLine;
        public override float FloorDim => _day ? 0.95f : base.FloorDim;
        public override float FloorDimAt => _day ? 40f : base.FloorDimAt;

        public override void Shot(float age, out Vector3 eye, out Vector3 focus, out float fov)
        {
            if (_show == Show.Zap) { eye = new Vector3(2.6f, 3.0f, -7.4f); focus = new Vector3(2.6f, 0.8f, 0f); fov = 42f; return; }
            eye = _feet + new Vector3(-1.4f, 3.1f, -7.6f);
            focus = _feet + new Vector3(-1.4f, 0.65f, 0f);
            fov = 42f;
        }

        public FxStudyZackSpeed(Transform parent, Show show, bool day) : base("zackspeed", parent)
        {
            _show = show; _day = day;
            var c = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            c.hideFlags = HideFlags.DontSave;
            Object.DestroyImmediate(c.GetComponent<Collider>());
            c.transform.SetParent(Root, false);
            c.transform.localScale = new Vector3(0.55f, Height * 0.5f, 0.55f);
            c.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { hideFlags = HideFlags.DontSave, color = new Color(0.86f, 0.62f, 0.10f) };
            _body = c.transform;
            if (show == Show.Zap)
            {
                var rival = Object.Instantiate(c, Root);
                rival.transform.position = new Vector3(6f, Height * 0.5f, 0f);
                c.transform.position = Vector3.up * (Height * 0.5f);
                _zap = new ZackZapPaint(Root);
                return;
            }
            _momentum = new ZackMomentumPaint(Root);
            if (show != Show.Momentum) _overclock = new ZackOverclockPaint(Root);
            if (show == Show.OverclockEnd) _seconds = ZackRules.MomentumRampSeconds;
        }

        public override void Sample(float age, Vector3 eye)
        {
            if (_zap != null) { SampleZap(age); return; }
            float dt = Mathf.Max(0f, age - _last);
            _last = age;
            bool running = age >= StartAt && age < StopAt;
            _seconds = ZackRules.StepMomentum(_seconds, ref _stopped, running, dt);
            float fuel = FuelAtStart - age;
            bool clocked = _overclock != null && fuel > 0f;
            if (running) _along += Balance.AttackerRunSpeed * ZackRules.SpeedScale(_seconds, clocked) * dt;
            // An S, so nothing here is only ever seen dead straight.
            _feet = new Vector3(_along, 0f, 1.3f * Mathf.Sin(_along * 0.33f));
            var heading = new Vector3(1f, 0f, 1.3f * 0.33f * Mathf.Cos(_along * 0.33f)).normalized;
            _body.position = _feet + Vector3.up * (Height * 0.5f);

            long before = System.GC.GetAllocatedBytesForCurrentThread();
            var clock = System.Diagnostics.Stopwatch.StartNew();
            _momentum.Pose(age, ZackRules.MomentumStep(_seconds), _feet, heading, false, false);
            _overclock?.Pose(age, _feet, heading, Mathf.Max(0f, fuel), fuel <= 0f, false, false);
            clock.Stop();
            if (age > 0f) { _ticks += clock.ElapsedTicks; _alloc += System.GC.GetAllocatedBytesForCurrentThread() - before; _frames++; }
            _peakDraws = Mathf.Max(_peakDraws, _momentum.LiveDraws + (_overclock != null ? _overclock.LiveDraws : 0));
            if (age >= Seconds - 1e-3f && !_reported)
            {
                _reported = true;
                Debug.Log("[ZackSpeedCost] " + _show + " peak draws " + _peakDraws + ", pose "
                          + (_ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency / Mathf.Max(1, _frames)).ToString("0.000")
                          + " ms a frame, allocated " + _alloc + " bytes over " + _frames + " frames");
            }
        }
        // Searching to 0.7 s, then locked on: the lock builds to 2.4 s, it Zaps, Grounded to the release at 4.0 s.
        private void SampleZap(float age)
        {
            var feet = new Vector3(6f, 0f, 0f);
            bool up = age < 4.0f;
            if (!up) { _zap.Hide(); return; }
            float on = FxStudyKit.Run(age, 0f, 0.09f);
            bool held = age >= 0.7f;
            _zap.Pose(age, on, new Vector3(0.2f, 1.0f, -0.28f), new Vector3(0.52f, 1.05f, 0f), held ? feet + Vector3.up * 0.9f : new Vector3(4f, 1f, 0f), held, 1, feet,
                      FxStudyKit.Run(age, 0.7f, 2.4f), age >= 2.4f, false);
        }
    }
}
