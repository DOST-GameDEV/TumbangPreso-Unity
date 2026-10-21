using System.Collections.Generic;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ LOOKOUT, THE LITTLE GHOST NEMU LEAVES MINDING THE CAN (owner 2026-10-15: *"spawn siya ghost na tutulungan siya
    /// magdefend (pede ren magtayo lata) but medyo small and not that good but it helps in some cases"*, *"only stays for like
    /// 15 seconds"*; the look from the approved lineup, `Visual.NemuGhostModels.Lookout`).
    ///
    /// KEEPER now (owner 2026-10-21). The rule lives in `NemuHeroKit.Lookout` (the 15 s clock, the 1.5 s stand-up after every
    /// knockdown); this is only the ghost. Attackers no longer scare it, so `Scared` stays false in play:
    ///  * MINDING: it floats beside the can, lantern held out and swinging, turning to watch the nearest attacker (and
    ///    scanning round when there is none).
    ///  * LIFTING (<see cref="Lift"/> 0 to 1): ⚠️ IT ACTUALLY LIFTS THE CAN (owner on v1: *"make it look liek its actually
    ///    lifting the can man"*). It flies to the fallen can's rim, both hands on it, and heaves in pulses, squashing with
    ///    each one; the can's drawn mesh rises with it (`Lata.ApplyRaisePresentation` reads <see cref="LiftFor"/>) and its
    ///    hands ride the rising rim (`Lata.PresentedEnds`). Its lantern dangles from its wrist onto the can, a ring of warm
    ///    light fills round the can's foot as it gets there, and it cheers when the can is up (<see cref="Cheer"/>).
    ///  * SCARED: an attacker is right on it; it drops the can (which sinks back as it loses ground), shrinks back behind
    ///    it with its hands over its eyes, shivering, and its lantern gutters.
    ///  * LEAVING: its time is up (or its job done); it sinks away into its lantern's glow.
    /// The can's own transform is never touched here: that is the host's state, and only `Lata.HostRestore` stands it up.
    /// </summary>
    public sealed partial class NemuLookout : MonoBehaviour
    {
        public static readonly List<NemuLookout> Live = new List<NemuLookout>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLive() => Live.Clear();

        public int OwnerSlot { get; private set; } = -1;
        public bool Leaving => _leaving;
        /// <summary>How far through standing the can up it is (0 when it is not trying).</summary>
        public float Lift { get; set; }
        public bool Scared { get; set; }
        /// <summary>The attacker it is watching, if any.</summary>
        public Vector3? Watching { get; set; }
        /// <summary>Where it is drawn and which way it faces (flat, world).</summary>
        public Vector3 Position => _at;
        public Vector3 Forward => _facing * Vector3.forward;

        private const float PopSeconds = .35f, LeaveSeconds = .6f, CheerSeconds = .7f, Scale = .85f;
        private static readonly Color Warm = NemuGhostModels.LookoutLight;
        private NemuGhostBody _body, _lantern;
        // The pet owl's slow orbit and hop round its point (`PetGhostFlight`, drawing only, behind `PetGhostModels.PetGhosts`).
        private readonly PetGhostFlight _flight = new PetGhostFlight(PetGhostFlight.Kind.Owl);
        private readonly NemuGhostBody[] _arms = new NemuGhostBody[2];
        private NemuSmokeBatch _fx;
        private Transform _can;
        private Lata _lata;
        private Vector3 _canAt, _side = Vector3.right, _at, _lastAt;
        private Quaternion _facing = Quaternion.identity;
        private float _age, _leftAt, _scare, _lift, _engage, _cheerAt = -10f, _swing, _swingVel, _roll, _rollVel;
        private bool _leaving, _placed;

        /// <summary>
        /// How far up the can is DRAWN by a Lookout heaving at it (0 when none is): `Lata` tilts its mesh by this, as it does
        /// for the taya's own raise.
        /// </summary>
        public static float LiftFor(Lata can)
        {
            float best = 0f;
            if (can == null) return 0f;
            foreach (var l in Live)
                if (l != null && !l._leaving && l._can == can.transform) best = Mathf.Max(best, l._lift * l._engage);
            return Mathf.Clamp01(best);
        }

        /// <summary>Puts Nemu's Lookout by the can (one per Nemu: her last one goes).</summary>
        public static NemuLookout Summon(Transform can, Vector3 fromOwner, int ownerSlot)
        {
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null && Live[i].OwnerSlot == ownerSlot) Live[i].Leave();
            var go = new GameObject("NemuLookout");
            var lookout = go.AddComponent<NemuLookout>();
            lookout.OwnerSlot = ownerSlot;
            lookout._can = can;
            lookout._lata = can != null ? can.GetComponent<Lata>() : null;
            lookout._canAt = can != null ? can.position : fromOwner;
            // It floats on the side of the can nearest her.
            Vector3 side = fromOwner - lookout._canAt; side.y = 0f;
            lookout._side = side.sqrMagnitude > 1e-4f ? side.normalized : Vector3.right;
            lookout._facing = Quaternion.LookRotation(-lookout._side);
            var rim = new Color(1f, .8f, .5f, 1f);
            lookout._body = new NemuGhostBody(go.transform, "LookoutBody", NemuGhostModels.Lookout, rim);
            for (int s = 0; s < 2; s++)
                lookout._arms[s] = new NemuGhostBody(lookout._body.Transform, s == 0 ? "LookoutArmL" : "LookoutArmR", NemuGhostModels.LookoutArm(s), rim);
            lookout._lantern = new NemuGhostBody(lookout._body.Transform, "LookoutLantern", NemuGhostModels.Lantern, rim);
            lookout._fx = new NemuSmokeBatch(go.transform, "LookoutFx", 6, 48, 1);
            return lookout;
        }

        /// <summary>
        /// A Keeper is on the job at this can (owner 2026-10-21: *"when Keeper is resetting the can it wont prompt you and you cant
        /// like reset it too"*): the can is its to stand up, so the taya gets no RESET CAN prompt and no channel while it minds it
        /// (`Carrier.HasResetTarget`, and the host's `MatchRpc.HostMayChannelReset`). Every peer runs the ghost, so all agree.
        /// </summary>
        public static bool Keeping(Lata can)
        {
            if (can == null) return false;
            foreach (var l in Live) if (l != null && !l._leaving && l._can == can.transform) return true;
            return false;
        }

        public static NemuLookout OwnedBy(int ownerSlot)
        {
            foreach (var l in Live) if (l != null && l.OwnerSlot == ownerSlot && !l.Leaving) return l;
            return null;
        }

        /// <summary>The can is up: a little hop of joy, arms up, and a burst of warm sparks off the rim.</summary>
        public void Cheer() => _cheerAt = _age;

        public void Leave()
        {
            if (_leaving) return;
            _leaving = true; _leftAt = _age;
        }

        private void OnEnable() => Live.Add(this);
        private void OnDisable() => Live.Remove(this);
        private void OnDestroy() { _fx?.Dispose(); _fx = null; }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            if (_can != null) _canAt = _can.position;
            _scare = Mathf.MoveTowards(_scare, Scared ? 1f : 0f, dt * 5f);
            // Its grip follows the rule's clock, quickly, and lets go fast when scared (the can sinks back).
            _lift = Mathf.MoveTowards(_lift, Lift, dt * (Lift < _lift ? 3f : 1.5f));
            _engage = Mathf.MoveTowards(_engage, Lift > 0f && !Scared && (_lata == null || !_lata.IsUpright) ? 1f : 0f, dt * 2.5f);
            float p = Mathf.Clamp01(_age / PopSeconds);
            float pop = p >= 1f ? 1f : Mathf.Max(0f, 1f + Mathf.Sin(p * Mathf.PI * 1.25f) * (1f - p) * .5f - (1f - p) * (1f - p));
            float fade = 1f, sink = 0f, leaveU = 0f;
            if (_leaving)
            {
                leaveU = Mathf.Clamp01((_age - _leftAt) / LeaveSeconds);
                fade = 1f - leaveU * leaveU; sink = .5f * leaveU * leaveU;
                if (leaveU >= 1f) { Destroy(gameObject); return; }
            }
            float cheer = Mathf.Clamp01((_age - _cheerAt) / CheerSeconds);
            bool cheering = cheer < 1f;
            float hop = cheering ? Mathf.Sin(cheer * Mathf.PI * 2f) * (1f - cheer) : 0f;

            // MINDING: beside the can, bobbing.
            Vector3 mind = _canAt + _side * .7f + Vector3.up * (.15f + .05f * Mathf.Sin(_age * 2.4f));
            // SCARED: back behind it, low.
            Vector3 hide = _canAt - _side * .6f + Vector3.up * .05f;
            // LIFTING: at the fallen can's rim, facing it, heaving.
            Vector3 bottom = _canAt, top = _canAt + Vector3.up * .3f;
            bool ends = _lata != null && _lata.PresentedEnds(out bottom, out top);
            Vector3 away = top - bottom; away.y = 0f;
            away = away.sqrMagnitude > 1e-4f ? away.normalized : _side;
            float heaveClock = _age * 2.6f * Mathf.PI;
            float heave = Mathf.Max(0f, Mathf.Sin(heaveClock)) * _engage;
            Vector3 lift = top + away * (.32f + .05f * heave) + Vector3.up * (-.3f * Scale - .03f * heave);
            if (!ends) lift = _canAt + _side * .3f + Vector3.up * .3f;
            Vector3 target = Vector3.Lerp(Vector3.Lerp(mind, lift, _engage), hide, _scare);
            target += Vector3.up * (hop * .25f - sink);
            // It flies there, not teleports (a soft follow), except on its first frame.
            _at = _placed ? Vector3.Lerp(_at, target, Mathf.Clamp01(dt * 9f)) : target;
            _placed = true;
            // Scared, it shivers.
            Vector3 shiver = _scare > .01f ? new Vector3(Mathf.Sin(_age * 61f), 0f, Mathf.Sin(_age * 53f + 1f)) * .015f * _scare : Vector3.zero;
            Vector3 at = _at + shiver;

            // Where it looks: the can while lifting, the attacker while minding, otherwise scanning round.
            Vector3 look = _engage > .5f ? -away
                : Watching.HasValue ? Watching.Value - at
                : Quaternion.AngleAxis(Mathf.Sin(_age * .9f) * 40f, Vector3.up) * -_side;
            look.y = 0f;
            if (look.sqrMagnitude > 1e-4f)
                _facing = Quaternion.Slerp(_facing, Quaternion.LookRotation(look.normalized), Mathf.Clamp01(dt * 6f));

            // Squash with each heave (down and wide as it pulls), stretch as it hops.
            float squash = -heave * .18f + hop * .2f + .03f * Mathf.Sin(_age * 2.4f);
            float size = Scale * pop * Mathf.Lerp(1f, .75f, _scare) * Mathf.Lerp(1f, .5f, leaveU);
            var scale = new Vector3(1f - squash * .5f, 1f + squash, 1f - squash * .5f) * size;
            // It leans back against the can's weight while it heaves.
            Quaternion facing = _facing * Quaternion.AngleAxis(-12f * _engage - 8f * heave, Vector3.right);
            float flash = Mathf.Max((1f - p) * .7f, cheering ? (1f - cheer) * .5f : 0f);
            if (PetGhostModels.PetGhosts && _body != null)
            {
                // The pet owl glides round its minding spot while nothing happens; it is back on the point to lift, hide or
                // cheer, and keeps close while it watches an attacker. Drawing only: the can and its spot do not move.
                float free = (1f - _engage) * (1f - _scare) * (cheering ? 0f : 1f) * (Watching.HasValue ? .35f : 1f) * (p >= 1f && !_leaving ? 1f : 0f);
                _flight.Apply(ref at, ref facing, _age, free, dt);
                _body.Trail = _flight.TrailLocal;
            }
            _body?.Pose(at, facing, scale, fade, _age, .05f + .04f * _engage, flash);

            PoseArms(top, bottom, ends, heave, hop, fade, flash);
            PoseLantern(at, fade, flash, dt);
            DrawFx(at, top, bottom, fade, p, cheer, leaveU, heaveClock);
            _lastAt = at;
        }

        private void PoseArms(Vector3 top, Vector3 bottom, bool ends, float heave, float hop, float fade, float flash)
        {
            if (_body == null) return;
            var body = _body.Transform;
            Vector3 across = Vector3.Cross(Vector3.up, (top - bottom).normalized);
            if (across.sqrMagnitude < 1e-4f) across = body.right; else across.Normalize();
            float cheer = Mathf.Clamp01((_age - _cheerAt) / CheerSeconds);
            for (int s = 0; s < 2; s++)
            {
                if (_arms[s] == null) continue;
                float sign = s == 0 ? -1f : 1f;
                Vector3 shoulder = NemuGhostModels.LookoutShoulder[s];
                // Minding: the left arm hangs, swaying; the right holds its lantern out and up.
                Vector3 hand = s == 0
                    ? shoulder + new Vector3(-.06f, -.14f, .04f + .03f * Mathf.Sin(_age * 2f))
                    : shoulder + new Vector3(.14f, .05f + .02f * Mathf.Sin(_age * 2.4f), .12f);
                // Lifting: both hands on the rim, one each side of it, gripping harder on each heave.
                if (ends && _engage > .01f)
                {
                    Vector3 grip = body.InverseTransformPoint(top + across * sign * (.06f - .015f * heave) + Vector3.up * .02f);
                    hand = Vector3.Lerp(hand, grip, _engage);
                }
                // Scared: hands over its eyes.
                hand = Vector3.Lerp(hand, new Vector3(sign * .06f, .47f, .24f), _scare);
                // Cheering: both arms up.
                if (cheer < 1f) hand = Vector3.Lerp(hand, shoulder + new Vector3(sign * .12f, .22f, .04f), Mathf.Sin(cheer * Mathf.PI));
                Vector3 to = hand - shoulder;
                float length = to.magnitude;
                Quaternion rot = length > 1e-4f ? Quaternion.LookRotation(to / length, Vector3.up) : Quaternion.identity;
                _arms[s].PoseLocal(shoulder, rot, fade, _age, flash);
                _arms[s].Transform.localScale = new Vector3(1f, 1f, Mathf.Clamp(length / NemuGhostModels.LookoutHandOnArm.z, .5f, PetGhostModels.PetGhosts ? PetGhostModels.MaxArmStretch : 2.6f));
            }
        }

        /// <summary>The lantern hangs from its right hand on a pendulum: it swings when the ghost moves, rattles when it is scared.</summary>
        private void PoseLantern(Vector3 at, float fade, float flash, float dt)
        {
            if (_lantern == null || _body == null || _arms[1] == null) return;
            var body = _body.Transform;
            Vector3 hand = _arms[1].Transform.TransformPoint(NemuGhostModels.LookoutHandOnArm);
            // A pendulum pushed by how the hand moves.
            Vector3 accel = dt > 0f ? (at - _lastAt) / dt : Vector3.zero;
            float pushX = Vector3.Dot(accel, body.forward), pushZ = Vector3.Dot(accel, body.right);
            _swingVel += (-40f * _swing - 2.5f * _swingVel - pushX * 60f) * dt; _swing += _swingVel * dt;
            _rollVel += (-40f * _roll - 2.5f * _rollVel + pushZ * 60f) * dt; _roll += _rollVel * dt;
            _swing = Mathf.Clamp(_swing, -50f, 50f); _roll = Mathf.Clamp(_roll, -50f, 50f);
            float rattle = _scare * 14f * Mathf.Sin(_age * 47f);
            Quaternion world = Quaternion.AngleAxis(_swing + 4f * Mathf.Sin(_age * 1.7f), body.right)
                * Quaternion.AngleAxis(_roll + rattle, body.forward) * Quaternion.LookRotation(Flat(body.forward), Vector3.up);
            _lantern.PoseLocal(body.InverseTransformPoint(hand), Quaternion.Inverse(body.rotation) * world, fade, _age, flash);
            // (The body squashes; the lantern keeps its own shape.)
            var ls = body.lossyScale;
            _lantern.Transform.localScale = new Vector3(Scale / Mathf.Max(.01f, ls.x), Scale / Mathf.Max(.01f, ls.y), Scale / Mathf.Max(.01f, ls.z));
        }

        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward; }

        private void DrawFx(Vector3 at, Vector3 top, Vector3 bottom, float fade, float pop, float cheer, float leaveU, float heaveClock)
        {
            if (_fx == null || _body == null) return;
            _fx.Begin();
            var body = _body.Transform;
            Vector3 glass = _lantern != null ? _lantern.Transform.TransformPoint(NemuGhostModels.LookoutGlass) : at;
            if (fade > .005f)
            {
                // Its lantern: warm, brighter as it lifts, guttering when it is scared.
                float flicker = .85f + .15f * Mathf.Sin(_age * 17f) * Mathf.Sin(_age * 5f + 1f);
                float gutter = _scare > .01f ? Mathf.Lerp(1f, .25f + .25f * Mathf.Abs(Mathf.Sin(_age * 29f)), _scare) : 1f;
                float strength = (.75f + _lift * .7f) * gutter * flicker * fade;
                _fx.Light(glass, .55f + _lift * .3f, strength, Warm);
                _fx.Light(glass, .16f, 1.6f * gutter * fade, Warm);
                // The pool of its light on the road.
                _fx.Light(new Vector3(glass.x, _canAt.y + .02f, glass.z), 1.3f, .22f * strength, Warm, default, 1.2f);
                // Embers rising off it.
                for (int i = 0; i < 3; i++)
                {
                    float u = Mathf.Repeat(_age * .6f + i * .33f, 1f);
                    Vector3 drift = new Vector3(Mathf.Sin(_age * 2f + i * 2.1f), 0f, Mathf.Cos(_age * 1.7f + i)) * .06f * u;
                    _fx.Light(glass + Vector3.up * (u * .5f) + drift, .05f, 1.1f * (1f - u) * gutter * fade, Warm);
                }
                // Its eyes, dimmer when scared (they shut behind its hands).
                foreach (var eye in NemuGhostModels.LookoutEyePoints)
                    _fx.Light(body.TransformPoint(eye), .07f, .9f * (1f - _scare * .8f) * fade, Warm);

                if (_engage > .01f && _lift > .001f)
                {
                    // ITS HANDS ON THE RIM, glowing as they grip.
                    _fx.Light(top, .3f, (.5f + .4f * Mathf.Max(0f, Mathf.Sin(heaveClock))) * _engage * fade, Warm);
                    // A RING OF WARM LIGHT FILLING ROUND THE CAN'S FOOT as it gets there.
                    const int Ring = 14;
                    float lit = _lift * Ring;
                    for (int i = 0; i < Ring; i++)
                    {
                        float k = Mathf.Clamp01(lit - i);
                        if (k <= 0f) break;
                        float a = i / (float)Ring * Mathf.PI * 2f + Mathf.PI * .5f;
                        _fx.Light(_canAt + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * .5f + Vector3.up * .03f, .16f, 1f * k * _engage * fade, Warm, default, .6f);
                    }
                    // Each heave: effort sparks off its head, and dust off the can's foot.
                    float beat = Mathf.Repeat(heaveClock / (Mathf.PI * 2f), 1f);
                    if (beat < .5f)
                    {
                        float u = beat * 2f;
                        for (int i = 0; i < 3; i++)
                        {
                            float a = i * 2.1f + Mathf.Floor(heaveClock / (Mathf.PI * 2f)) * 1.3f;
                            Vector3 dir = (body.up * 1.2f + body.right * Mathf.Cos(a) + body.forward * Mathf.Sin(a) * .5f).normalized;
                            _fx.Light(body.TransformPoint(new Vector3(0f, .6f, 0f)) + dir * (.05f + u * .2f), .05f, 1.1f * (1f - u) * _engage * fade, Warm);
                        }
                        _fx.Puff(bottom + Vector3.up * .05f, .25f + u * .25f, .25f * (1f - u) * _engage, .4f + i0(heaveClock), u, .3f);
                    }
                }
                // SCARED: her smoke gathering round it.
                if (_scare > .01f)
                    for (int i = 0; i < 2; i++)
                        _fx.Puff(at + Vector3.up * .3f + body.right * (i == 0 ? -.2f : .2f), .4f, .3f * _scare * fade, .2f + i * .4f, .3f, .7f);
            }
            // SUMMON: its lantern flares and it comes out of a puff.
            if (pop < 1f)
            {
                _fx.Light(glass, 1.2f * (1f - pop) + .2f, 1.5f * (1f - pop), Warm);
                _fx.Puff(at + Vector3.up * .3f, .6f + pop * .4f, .5f * (1f - pop), .5f, pop, .8f);
            }
            // CHEER: a burst of warm sparks off the can's rim.
            if (cheer < 1f)
            {
                Vector3 rim = _canAt + Vector3.up * .3f;
                for (int i = 0; i < 10; i++)
                {
                    float a = i * Mathf.PI * .2f;
                    Vector3 dir = new Vector3(Mathf.Cos(a), 1.1f, Mathf.Sin(a)).normalized;
                    _fx.Light(rim + dir * (.1f + cheer * .6f) - Vector3.up * cheer * cheer * .3f, .08f * (1f - cheer) + .02f, 1.5f * (1f - cheer), Warm, dir * .1f);
                }
                _fx.Light(rim, .9f * (1f - cheer), 1.2f * (1f - cheer), Warm);
            }
            // LEAVING: it sinks into its lantern's glow, the glow last to go.
            if (_leaving)
                _fx.Light(glass, .5f * (1f - leaveU) + .1f, 1.4f * (1f - leaveU * leaveU), Warm);
            _fx.End(_age);
        }

        private static float i0(float clock) => Mathf.Repeat(Mathf.Floor(clock / (Mathf.PI * 2f)) * .37f, 1f);

        /// <summary>Every Lookout on the court goes (the round ended).</summary>
        public static void RetireAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null) Destroy(Live[i].gameObject);
            Live.Clear();
        }
    }
}
