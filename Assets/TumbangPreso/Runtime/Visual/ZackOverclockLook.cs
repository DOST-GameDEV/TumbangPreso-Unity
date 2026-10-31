using TumbangPreso.Abilities;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// OVERCLOCKED on his body (plan section 3): a gold crackle round him re-striking in short bursts, three ghost frames
    /// behind him (his own recorded pose a beat ago, gold-rimmed) and a lit trail at his feet. At 5 s left the crackle
    /// stutters; at 0 a last snap and it is gone. Every peer draws it from the kit's own 25 s clock. Render only.
    /// </summary>
    public sealed class ZackOverclockLook : MonoBehaviour
    {
        /// <summary>
        /// The bench rework (`ZackOverclockPaint`): a dark plume and arcs off the air beside him, nothing on his body, no copies of
        /// him, and an ending that plays out. False draws the lines and ghosts below exactly as they were, kept until the owner
        /// has passed the painted look in play.
        /// </summary>
        public static bool Painted = true;
        private bool _painted, _released;
        private float _releasedAge;
        private ZackOverclockPaint _paint;

        private const int Arcs = 3, Ghosts = 3, TrailPoints = 10;
        private CharacterMotor _motor;
        private ZackHeroKit _kit;
        private ZackCurrentAudio _current;
        private readonly LineRenderer[] _arcs = new LineRenderer[Arcs];
        private readonly ZackFx.Echo[] _ghosts = new ZackFx.Echo[Ghosts];
        private LineRenderer _trail, _trailInk;
        private readonly Vector3[] _feet = new Vector3[TrailPoints];
        private float _nextFoot, _age, _nextGhostTry;

        public static ZackOverclockLook Attach(CharacterMotor motor, ZackHeroKit kit, bool? painted = null)
        {
            if (motor == null) return null;
            // At the origin: the ghost frames write their roots in world space.
            var go = new GameObject("ZackOverclocked-P" + (motor.PlayerSlot + 1));
            var look = go.AddComponent<ZackOverclockLook>();
            look._motor = motor; look._kit = kit;
            look._current = ZackCurrentAudio.Attach(go.transform, "sfx_zack_overclock_loop");
            for (int i = 0; i < Arcs; i++) look._arcs[i] = ZackFx.Line(go.transform, "Crackle" + i, 6, 0.03f, ZackFx.Core, 1.5f);
            look._trailInk = ZackFx.Line(go.transform, "FootTrailInk", TrailPoints, 0.12f, ZackFx.Ink, 0.0f);
            look._trail = ZackFx.Line(go.transform, "FootTrail", TrailPoints, 0.06f, ZackFx.Body, 1.3f);
            for (int i = 0; i < TrailPoints; i++) look._feet[i] = motor.transform.position;
            look._painted = painted ?? Painted;
            if (look._painted)
            {
                foreach (var line in go.GetComponentsInChildren<LineRenderer>(true)) line.enabled = false;
                look._paint = new ZackOverclockPaint(go.transform);
            }
            return look;
        }

        public bool BelongsTo(CharacterMotor motor) => this != null && _motor == motor;

        /// <summary>It ended: one last snap and the light drains.</summary>
        public void Release()
        {
            if (this == null) return;
            // Painted, it plays its own ending and goes when that is done (`LateUpdate`).
            if (_painted) { if (!_released) { _released = true; _releasedAge = _age; } return; }
            if (_motor != null) ZackFx.Sparks(_motor.transform.position + Vector3.up * 0.9f, 0.5f, 4, _motor.PlayerSlot);
            Destroy(gameObject);
        }

        private void LateUpdate()
        {
            if (_painted) { StepPainted(); return; }
            if (_motor == null || _kit == null || !_kit.IsOverclocked) { Destroy(gameObject); return; }
            _age += Time.deltaTime;
            var at = _motor.transform.position;
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            bool firstPerson = ZackFx.InOwnFirstPerson(_motor);
            float left = _kit.OverclockLeft;
            _current?.Step(at + Vector3.up * .8f, true, left < 5 && Mathf.Repeat(_age, .25f) < .07f ? .3f : 1f);
            // At 5 s left it stutters: whole beats drop out, faster as it runs down.
            bool stutter = left < 5.0f && Mathf.Repeat(_age, Mathf.Lerp(0.18f, 0.5f, left / 5.0f)) < 0.07f;
            int strike = reduced ? 0 : Mathf.FloorToInt(_age / 0.08f);
            for (int i = 0; i < Arcs; i++)
            {
                float yaw = (strike * 97 + i * 120) % 360;
                var dir = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                float h = 0.4f + 0.45f * Mathf.Abs(Mathf.Sin(strike * 1.3f + i));
                var a = at + dir * 0.32f + Vector3.up * h;
                var b = at + Quaternion.Euler(0, 50, 0) * dir * 0.38f + Vector3.up * (h + 0.35f);
                ZackFx.Jag(_arcs[i], a, b, 0.05f, strike + i);
                bool on = !firstPerson && !stutter && (reduced || Mathf.Repeat(_age + i * 0.03f, 0.08f) < 0.05f);
                _arcs[i].enabled = on;
            }
            // The lit trail at his feet: his last metres, low on the road.
            if (Time.time >= _nextFoot)
            {
                _nextFoot = Time.time + 0.04f;
                for (int i = TrailPoints - 1; i > 0; i--) _feet[i] = _feet[i - 1];
                _feet[0] = at;
            }
            for (int i = 0; i < TrailPoints; i++)
            {
                _trail.SetPosition(i, _feet[i] + Vector3.up * 0.04f);
                _trailInk.SetPosition(i, _feet[i] + Vector3.up * 0.03f);
            }
            float trailAlpha = stutter ? 0.2f : 0.9f;
            ZackFx.Tint(_trail, new Color(ZackFx.Body.r, ZackFx.Body.g, ZackFx.Body.b, trailAlpha));
            ZackFx.Tint(_trailInk, new Color(ZackFx.Ink.r, ZackFx.Ink.g, ZackFx.Ink.b, trailAlpha * 0.7f));
            StepGhosts(firstPerson || reduced || stutter);
        }

        private void StepPainted()
        {
            if (_motor == null || _kit == null) { Destroy(gameObject); return; }
            _age += Time.deltaTime;
            // The kit can drop the state without a Release (a round's end): that ends it the same way.
            if (!_released && !_kit.IsOverclocked) { _released = true; _releasedAge = _age; }
            var at = _motor.transform.position;
            float left = _released ? 0.0f : _kit.OverclockLeft;
            _current?.Step(at + Vector3.up * .8f, !_released, left > 0.0f && left < 5 && Mathf.Repeat(_age, .25f) < .07f ? .3f : 1f);
            _paint.Pose(_age, at, _motor.transform.forward, left, _released, ZackFx.InOwnFirstPerson(_motor), Settings.SettingsStore.Current.ReducedEffects);
            if (_released && _age - _releasedAge > ZackOverclockPaint.EndSeconds)
            {
                if (Application.isPlaying) Destroy(gameObject); else gameObject.SetActive(false);
            }
        }

        private void StepGhosts(bool hidden)
        {
            for (int i = 0; i < Ghosts; i++)
            {
                if (_ghosts[i] == null && !hidden && Time.time >= _nextGhostTry)
                {
                    _ghosts[i] = ZackFx.Echo.Of(_motor.PlayerSlot, transform);
                    if (_ghosts[i] == null) _nextGhostTry = Time.time + 1.0f;
                }
                var ghost = _ghosts[i];
                if (ghost == null) continue;
                float behind = 0.07f * (i + 1);
                ghost.PoseAt(ghost.Track.Newest - behind);
                // Each frame shows on alternate beats so they read as afterimages, never as more Zacks.
                ghost.Visible(!hidden && Mathf.Repeat(_age * 12.0f + i, 2.0f) < 1.0f);
            }
        }

        private void OnDestroy() { foreach (var ghost in _ghosts) ghost?.Destroy(); _paint?.Dispose(); }
    }
}
