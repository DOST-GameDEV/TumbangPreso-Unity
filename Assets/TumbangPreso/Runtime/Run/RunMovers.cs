using System;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Run
{
    public enum RunMotion { Still, Sway, Pendulum, Spin, Roll, Tilt, Crumble }

    /// <summary>
    /// ONE MOVING OBSTACLE of Paete's run. Its place is a CLOSED-FORM function of one clock (<see cref="RunClock"/>):
    /// nothing about it is sent, every peer computes the same pose. The scene builder fills these fields from the
    /// layout's `movers`; <see cref="RunMovers"/> moves it and carries whoever stands on it.
    ///
    /// | Motion | What moves | The numbers |
    /// |---|---|---|
    /// | Sway | slides along `Axis` (sideways for a hung boulder or a chain, up and down for a floating rock) | `Amp` metres, `Period` s, `Phase` 0..1 |
    /// | Pendulum | swings about `Pivot` in the plane of `Axis` | `Amp` radians each way |
    /// | Spin | turns about the upright through its middle (a mill wheel) | one turn in `Period`; `Amp` is the way, +1 or -1 |
    /// | Roll | turns about `Axis` (a log, about its length) | one turn in `Period`; `Amp` is the way |
    /// | Tilt | rocks about `Axis` (a seesaw) | `Amp` degrees each way. ⚠️ ON THE CLOCK, not under a body's weight |
    /// | Crumble | falls `Amp` seconds after it is first stood on, back `Period` seconds later | ⚠️ PER PEER, see `RunMovers` |
    /// </summary>
    public sealed class RunMover : MonoBehaviour
    {
        public string Kind = "";
        public RunMotion Motion;
        public Vector3 Rest, Axis = Vector3.up, Pivot, Size = Vector3.one;
        public float RestYaw, Amp, Period = 4f, Phase, Arm, Radius;
        public Collider Solid;

        /// <summary>Crumble only: the clock when a body was first seen on it, under zero while it waits.</summary>
        [NonSerialized] public double ArmedAt = -1.0;
        [NonSerialized] public Vector3 LastPosition;
        [NonSerialized] public Quaternion LastRotation = Quaternion.identity;

        public const float CrumbleFallSeconds = 1.4f;

        public Quaternion RestRotation => Quaternion.Euler(0f, RestYaw, 0f);

        public void Pose(double t, out Vector3 position, out Quaternion rotation)
        {
            position = Rest; rotation = RestRotation;
            float period = Mathf.Max(0.05f, Period);
            // The turn's share, 0..1, kept small before it becomes a float: the clock may be hours.
            float turn = (float)((t / period + Phase) % 1.0);
            float wave = Mathf.Sin(turn * Mathf.PI * 2f);
            switch (Motion)
            {
                case RunMotion.Sway:
                    position = Rest + Axis * (Amp * wave);
                    break;
                case RunMotion.Pendulum:
                {
                    var swing = Quaternion.AngleAxis(Amp * wave * Mathf.Rad2Deg, Vector3.Cross(Axis, Vector3.up));
                    position = Pivot + swing * (Rest - Pivot);
                    rotation = swing * RestRotation;
                    break;
                }
                case RunMotion.Spin:
                    rotation = Quaternion.AngleAxis(360f * turn * Mathf.Sign(Amp == 0f ? 1f : Amp), Vector3.up) * RestRotation;
                    break;
                case RunMotion.Roll:
                    rotation = Quaternion.AngleAxis(360f * turn * Mathf.Sign(Amp == 0f ? 1f : Amp), Axis) * RestRotation;
                    break;
                case RunMotion.Tilt:
                    rotation = Quaternion.AngleAxis(Amp * wave, Axis) * RestRotation;
                    break;
                case RunMotion.Crumble:
                {
                    if (ArmedAt < 0.0) break;
                    float since = (float)(t - ArmedAt);
                    if (since < Amp) { position = Rest + Vector3.up * (Mathf.Sin(since * 55f) * 0.025f); break; }   // it shivers first
                    float fall = since - Amp;
                    position = Rest + Vector3.down * Mathf.Min(60f, 10f * fall * fall);
                    rotation = Quaternion.AngleAxis(Mathf.Min(50f, fall * 40f), Axis.sqrMagnitude > 0.5f ? Axis : Vector3.right) * RestRotation;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// MOVES EVERY <see cref="RunMover"/> AND CARRIES WHOEVER STANDS ON ONE, without a line in the motor.
    ///
    /// Each physics step, BEFORE the motors step (this runs first by its execution order):
    ///   1. every body this peer simulates that is on the ground is asked what it stands on (one short sphere cast);
    ///   2. every mover is put at its pose for the clock, and the physics scene is told once;
    ///   3. a body that stood on a mover is moved by exactly what the point under its feet moved
    ///      (`CharacterController.Move`, the controller's own collision), so a boulder carries him sideways, a floating
    ///      rock lifts him, a wheel takes him round and a log walks him off its top. The motor then steps as it always does.
    ///   4. a pendulum that reaches a body THROWS it (`CharacterMotor.ApplyImpulse` as a launch: the car's hit), which is
    ///      what "knocks you off" is; a controller is not pushed by a collider that moves into it.
    ///
    /// ⚠️ ONLY BODIES THIS PEER SIMULATES are carried or thrown, the rule every movement in the game follows; a replica
    /// is drawn where its owner says it is.
    ///
    /// ⚠️ THE CRUMBLING SLABS ARE PER PEER. A slab falls `Amp` seconds after THIS peer first sees any body on it (its
    /// own, or a replica), and is back `Period` seconds after that. Two peers see the same body arrive within a few
    /// tens of milliseconds of each other, so they agree to about that; nothing is sent. A slab that is exact for
    /// everyone needs a message (the owner's to approve) or a schedule on the clock that ignores who is standing on it.
    ///
    /// ⚠️ THE SEESAWS TIP ON THE CLOCK, not under weight: a slab that answers to bodies is state every peer would have
    /// to agree on.
    /// </summary>
    [DefaultExecutionOrder(-9000)]
    public sealed class RunMovers : MonoBehaviour
    {
        public RunMover[] Movers = Array.Empty<RunMover>();
        /// <summary>How a pendulum throws: metres a second across the ledge, and up.</summary>
        public float PendulumPush = 13f, PendulumLift = 5f;

        private struct Ride { public CharacterMotor Who; public RunMover On; public Vector3 Feet; }

        private CharacterMotor[] _motors = Array.Empty<CharacterMotor>();
        private float _rescan;
        private readonly List<Ride> _rides = new List<Ride>();
        private readonly Dictionary<CharacterMotor, CharacterController> _controllers = new Dictionary<CharacterMotor, CharacterController>();
        private readonly Dictionary<CharacterMotor, float> _thrownAt = new Dictionary<CharacterMotor, float>();
        private static readonly RaycastHit[] Hits = new RaycastHit[16];

        /// <summary>What a body stands on this step, for the probe and the HUD. Null on plain ground.</summary>
        public RunMover Under(CharacterMotor who)
        {
            foreach (var ride in _rides) if (ride.Who == who) return ride.On;
            return null;
        }

        /// <summary>Every slab back in its place (a new race).</summary>
        public void ResetAll()
        {
            foreach (var m in Movers)
            {
                if (m == null) continue;
                m.ArmedAt = -1.0;
                if (m.Solid != null) m.Solid.enabled = true;
            }
        }

        private void OnEnable()
        {
            if (Movers == null || Movers.Length == 0) Movers = GetComponentsInChildren<RunMover>(true);
            double t = RunClock.Now;
            foreach (var m in Movers)
            {
                if (m == null) continue;
                m.Pose(t, out var p, out var r);
                m.transform.SetPositionAndRotation(p, r);
                m.LastPosition = p; m.LastRotation = r;
            }
        }

        private void FixedUpdate()
        {
            if (PresentationClock.Held) return;
            _rescan -= Time.fixedDeltaTime;
            if (_rescan <= 0f) { _rescan = 1f; _motors = FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None); }
            double t = RunClock.Now;

            // 1. who stands on what
            _rides.Clear();
            foreach (var who in _motors)
            {
                if (who == null || !who.isActiveAndEnabled || !who.IsGrounded || !who.IsLocallySimulated()) continue;
                var on = MoverUnder(who.transform.position);
                if (on != null) _rides.Add(new Ride { Who = who, On = on, Feet = who.transform.position });
            }

            // 2. the movers
            foreach (var m in Movers)
            {
                if (m == null) continue;
                if (m.Motion == RunMotion.Crumble) StepCrumble(m, t);
                m.LastPosition = m.transform.position; m.LastRotation = m.transform.rotation;
                m.Pose(t, out var p, out var r);
                m.transform.SetPositionAndRotation(p, r);
            }
            Physics.SyncTransforms();

            // 3. the riders go with the point under their feet
            foreach (var ride in _rides)
            {
                var m = ride.On;
                if (m.Motion == RunMotion.Crumble && m.ArmedAt >= 0.0 && t - m.ArmedAt > m.Amp) continue;   // it has let go of him
                Vector3 now = m.transform.position + (m.transform.rotation * Quaternion.Inverse(m.LastRotation)) * (ride.Feet - m.LastPosition);
                Vector3 delta = now - ride.Feet;
                if (delta.sqrMagnitude < 1e-10f || delta.sqrMagnitude > 4f) continue;     // a teleport of the mover is not a ride
                var cc = Controller(ride.Who);
                if (cc != null && cc.enabled) cc.Move(delta);
            }

            // 4. the pendulums
            foreach (var m in Movers)
            {
                if (m == null || m.Motion != RunMotion.Pendulum) continue;
                Vector3 ball = m.transform.position;
                Vector3 travel = ball - m.LastPosition; travel.y = 0f;
                foreach (var who in _motors)
                {
                    if (who == null || !who.isActiveAndEnabled || !who.IsLocallySimulated()) continue;
                    Vector3 chest = who.transform.position + Vector3.up * 0.8f;
                    if ((chest - ball).sqrMagnitude > (m.Radius + 0.45f) * (m.Radius + 0.45f)) continue;
                    if (_thrownAt.TryGetValue(who, out float at) && Time.time - at < 0.7f) continue;
                    _thrownAt[who] = Time.time;
                    Vector3 way = travel.sqrMagnitude > 1e-6f ? travel.normalized : (chest - ball);
                    way.y = 0f;
                    way = way.sqrMagnitude > 1e-6f ? way.normalized : m.Axis;
                    who.ApplyImpulse(CharacterMotor.AsLaunch(way * PendulumPush + Vector3.up * PendulumLift));
                }
            }
        }

        private void StepCrumble(RunMover m, double t)
        {
            if (m.ArmedAt < 0.0)
            {
                var inverse = Quaternion.Inverse(m.RestRotation);
                foreach (var who in _motors)
                {
                    if (who == null || !who.isActiveAndEnabled || !who.IsGrounded) continue;
                    Vector3 local = inverse * (who.transform.position - m.Rest);
                    if (Mathf.Abs(local.x) > m.Size.x * 0.5f || Mathf.Abs(local.z) > m.Size.z * 0.5f) continue;
                    if (local.y < m.Size.y * 0.5f - 0.25f || local.y > m.Size.y * 0.5f + 0.5f) continue;
                    m.ArmedAt = t;
                    break;
                }
                return;
            }
            double since = t - m.ArmedAt;
            if (m.Solid != null) m.Solid.enabled = since < m.Amp + 0.12;
            if (since >= m.Amp + Mathf.Max(RunMover.CrumbleFallSeconds, m.Period))
            {
                m.ArmedAt = -1.0;
                if (m.Solid != null) m.Solid.enabled = true;
            }
        }

        private CharacterController Controller(CharacterMotor who)
        {
            if (!_controllers.TryGetValue(who, out var cc) || cc == null) _controllers[who] = cc = who.GetComponent<CharacterController>();
            return cc;
        }

        /// <summary>The mover a body with its feet at <paramref name="feet"/> is standing on, or null.</summary>
        private static RunMover MoverUnder(Vector3 feet)
        {
            int count = Physics.SphereCastNonAlloc(feet + Vector3.up * 0.55f, 0.3f, Vector3.down, Hits, 0.5f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; RunMover found = null; bool any = false;
            for (int i = 0; i < count; i++)
            {
                var c = Hits[i].collider;
                if (c == null || c.GetComponentInParent<CharacterMotor>() != null) continue;
                if (Hits[i].distance >= best) continue;
                best = Hits[i].distance; any = true;
                found = c.GetComponentInParent<RunMover>();
            }
            return any ? found : null;
        }
    }
}
