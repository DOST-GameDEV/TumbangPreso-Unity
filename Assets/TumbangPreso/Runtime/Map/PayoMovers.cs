using System;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// MOVES A LAYOUT'S SLABS AND CARRIES WHOEVER STANDS ON ONE, without a line in the motor: the run's own way
    /// (`Run.RunMovers`, proven there by play probes), kept to the one motion this map has.
    ///
    /// Each physics step, BEFORE the motors step (this runs first by its execution order):
    ///   1. every body THIS PEER simulates that is on the ground is asked what it stands on (one short sphere cast);
    ///   2. every slab is put at its place for the clock, and the physics scene is told once;
    ///   3. a body that stood on a slab is moved by exactly what the slab moved (`CharacterController.Move`, the
    ///      controller's own collision), so a lift raises it and a ferry carries it across.
    ///
    /// ⚠️ ONLY BODIES THIS PEER SIMULATES are carried, the rule every movement in the game follows; a replica is
    /// drawn where its owner says it is. Nothing is sent. One of these sits on each layout's `Features` object, so
    /// a layout's slabs exist only while that layout stands (`ArenaStage.SetFeatures`), and never in a break.
    /// </summary>
    [DefaultExecutionOrder(-9000)]
    public sealed class PayoMovers : MonoBehaviour
    {
        public PayoMover[] Movers = Array.Empty<PayoMover>();

        /// <summary>
        /// The clock the slabs run on: in a session the server clock every peer shares, offline this peer's own
        /// game clock (the physics clock inside a physics step, so a slab's step matches the step it is taken in).
        /// </summary>
        public static double Clock
        {
            get
            {
                var nm = Unity.Netcode.NetworkManager.Singleton;
                if (NetAuthority.IsNetworked && nm != null && nm.IsListening) return nm.ServerTime.Time;
                return Time.inFixedTimeStep ? Time.fixedTimeAsDouble : Time.timeAsDouble;
            }
        }

        private struct Ride { public CharacterMotor Who; public PayoMover On; public Vector3 Feet; }

        private CharacterMotor[] _motors = Array.Empty<CharacterMotor>();
        private float _rescan;
        private readonly List<Ride> _rides = new List<Ride>();
        private readonly Dictionary<CharacterMotor, CharacterController> _controllers = new Dictionary<CharacterMotor, CharacterController>();
        private static readonly RaycastHit[] Hits = new RaycastHit[16];

        /// <summary>What a body stands on this step, for the probe. Null on plain stone.</summary>
        public PayoMover Under(CharacterMotor who)
        {
            foreach (var ride in _rides) if (ride.Who == who) return ride.On;
            return null;
        }

        private void OnEnable()
        {
            if (Movers == null || Movers.Length == 0) Movers = GetComponentsInChildren<PayoMover>(true);
            _rescan = 0.0f;
            double t = Clock;
            foreach (var m in Movers)
            {
                if (m == null) continue;
                Vector3 p = m.Pose(t);
                m.transform.position = p;
                m.LastPosition = p;
            }
            Physics.SyncTransforms();
        }

        private void FixedUpdate()
        {
            if (PresentationClock.Held || Movers.Length == 0) return;
            _rescan -= Time.fixedDeltaTime;
            if (_rescan <= 0.0f) { _rescan = 1.0f; _motors = FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None); }
            double t = Clock;

            _rides.Clear();
            foreach (var who in _motors)
            {
                if (who == null || !who.isActiveAndEnabled || !who.IsGrounded || !who.IsLocallySimulated()) continue;
                var on = MoverUnder(who.transform.position);
                if (on != null) _rides.Add(new Ride { Who = who, On = on, Feet = who.transform.position });
            }

            foreach (var m in Movers)
            {
                if (m == null) continue;
                m.LastPosition = m.transform.position;
                m.transform.position = m.Pose(t);
            }
            Physics.SyncTransforms();

            foreach (var ride in _rides)
            {
                Vector3 delta = ride.On.transform.position - ride.On.LastPosition;
                if (delta.sqrMagnitude < 1e-10f || delta.sqrMagnitude > 4.0f) continue;     // a jump of the clock is not a ride
                var cc = Controller(ride.Who);
                if (cc != null && cc.enabled) cc.Move(delta);
            }
        }

        private CharacterController Controller(CharacterMotor who)
        {
            if (!_controllers.TryGetValue(who, out var cc) || cc == null) _controllers[who] = cc = who.GetComponent<CharacterController>();
            return cc;
        }

        /// <summary>The slab a body with its feet at `feet` is standing on, or null.</summary>
        private static PayoMover MoverUnder(Vector3 feet)
        {
            int count = Physics.SphereCastNonAlloc(feet + Vector3.up * 0.55f, 0.3f, Vector3.down, Hits, 0.5f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; PayoMover found = null; bool any = false;
            for (int i = 0; i < count; i++)
            {
                var c = Hits[i].collider;
                if (c == null || c.GetComponentInParent<CharacterMotor>() != null) continue;
                if (Hits[i].distance >= best) continue;
                best = Hits[i].distance; any = true;
                found = c.GetComponentInParent<PayoMover>();
            }
            return any ? found : null;
        }
    }
}
