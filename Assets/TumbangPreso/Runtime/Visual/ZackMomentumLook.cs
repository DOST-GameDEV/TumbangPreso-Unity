using TumbangPreso.Abilities;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// MOMENTUM on his body (plan section 3), in three steps: at 1 s sparks at his heels, at 2 s a short gold ribbon from
    /// them, at 3 s (full) three ghost frames trail him. A stop breaks the ribbon into sparks; a hit snaps it away.
    ///
    /// ⚠️ NOTHING OF IT IS ON THE WIRE. The peer that moves him reads the kit's own count; every other peer counts the same
    /// rule (`ZackRules.StepMomentum`) from the speed it sees him travel at, so all four screens show the same step within a
    /// frame or two. It decides nothing.
    /// </summary>
    public sealed class ZackMomentumLook : MonoBehaviour
    {
        private const int RibbonPoints = 8, Ghosts = 3;
        private CharacterMotor _motor;
        private ZackHeroKit _kit;
        private LineRenderer _heelA, _heelB, _ribbon, _ribbonInk;
        private readonly Vector3[] _heels = new Vector3[RibbonPoints];
        private readonly ZackFx.Echo[] _ghosts = new ZackFx.Echo[Ghosts];
        private float _observed, _stoppedFor, _nextHeel, _age, _nextGhostTry;
        private Vector3 _last;
        private int _step;

        public static ZackMomentumLook Attach(CharacterMotor motor, ZackHeroKit kit)
        {
            if (motor == null) return null;
            var go = new GameObject("ZackMomentum-P" + (motor.PlayerSlot + 1));
            var look = go.AddComponent<ZackMomentumLook>();
            look._motor = motor; look._kit = kit; look._last = motor.transform.position;
            look._heelA = ZackFx.Line(go.transform, "HeelSparkA", 4, 0.022f, ZackFx.Core, 1.5f);
            look._heelB = ZackFx.Line(go.transform, "HeelSparkB", 4, 0.022f, ZackFx.Core, 1.5f);
            look._ribbonInk = ZackFx.Line(go.transform, "RibbonInk", RibbonPoints, 0.09f, ZackFx.Ink, 0.0f);
            look._ribbon = ZackFx.Line(go.transform, "Ribbon", RibbonPoints, 0.045f, ZackFx.Body, 1.3f);
            for (int i = 0; i < RibbonPoints; i++) look._heels[i] = motor.transform.position;
            return look;
        }

        public bool BelongsTo(CharacterMotor motor) => this != null && _motor == motor;

        /// <summary>Seconds of running this screen counts for him.</summary>
        private float Seconds(float dt)
        {
            var at = _motor.transform.position;
            if (_motor.IsLocallySimulated()) { _last = at; return _kit.MomentumSeconds; }
            var moved = at - _last; moved.y = 0.0f;
            _last = at;
            float speed = dt > 1e-4f ? moved.magnitude / dt : 0.0f;
            // A jump of metres in a frame is a teleport (a Backtrack, a respawn), not a run.
            bool running = speed >= ZackRules.MomentumRunSpeed && speed < 30.0f && !_motor.IsStunned;
            if (speed >= 30.0f) _observed = 0.0f;
            _observed = ZackRules.StepMomentum(_observed, ref _stoppedFor, running, dt);
            return _observed;
        }

        private void LateUpdate()
        {
            if (_motor == null || _kit == null || _motor.AbilitySystem?.Kit != _kit) { Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            _age += dt;
            int step = ZackRules.MomentumStep(Seconds(dt));
            var at = _motor.transform.position;
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            if (step == 0 && _step >= 2) ZackFx.Sparks(at + Vector3.up * 0.15f, 0.25f, 3, _motor.PlayerSlot);
            _step = step;
            // Step 1: sparks at the heels, re-striking.
            bool heel = step >= 1 && (reduced || Mathf.Repeat(_age, 0.12f) < 0.06f);
            _heelA.enabled = heel; _heelB.enabled = heel;
            if (heel)
            {
                var back = -_motor.transform.forward; back.y = 0.0f;
                var side = _motor.transform.right;
                int strike = Mathf.FloorToInt(_age / 0.12f);
                ZackFx.Jag(_heelA, at + side * 0.12f + Vector3.up * 0.03f, at + side * 0.16f + back * 0.3f + Vector3.up * 0.1f, 0.04f, strike);
                ZackFx.Jag(_heelB, at - side * 0.12f + Vector3.up * 0.03f, at - side * 0.16f + back * 0.3f + Vector3.up * 0.1f, 0.04f, strike + 3);
            }
            // Step 2: a short gold ribbon from his heels.
            if (Time.time >= _nextHeel)
            {
                _nextHeel = Time.time + 0.03f;
                for (int i = RibbonPoints - 1; i > 0; i--) _heels[i] = _heels[i - 1];
                _heels[0] = at;
            }
            if (step < 2) for (int i = 0; i < RibbonPoints; i++) _heels[i] = at;
            for (int i = 0; i < RibbonPoints; i++)
            {
                _ribbon.SetPosition(i, _heels[i] + Vector3.up * (0.08f + 0.02f * i));
                _ribbonInk.SetPosition(i, _heels[i] + Vector3.up * (0.07f + 0.02f * i));
            }
            _ribbon.enabled = step >= 2; _ribbonInk.enabled = step >= 2;
            // Step 3: three ghost frames, unless Overclocked is already drawing its own.
            bool ghosts = step >= 3 && !_kit.IsOverclocked && !reduced && !ZackFx.InOwnFirstPerson(_motor);
            for (int i = 0; i < Ghosts; i++)
            {
                if (_ghosts[i] == null && ghosts && Time.time >= _nextGhostTry)
                {
                    _ghosts[i] = ZackFx.Echo.Of(_motor.PlayerSlot, transform);
                    if (_ghosts[i] == null) _nextGhostTry = Time.time + 1.0f;
                }
                var ghost = _ghosts[i];
                if (ghost == null) continue;
                if (ghosts) ghost.PoseAt(ghost.Track.Newest - 0.06f * (i + 1));
                ghost.Visible(ghosts && Mathf.Repeat(_age * 12.0f + i, 2.0f) < 1.0f);
            }
        }

        private void OnDestroy() { foreach (var ghost in _ghosts) ghost?.Destroy(); }
    }
}
