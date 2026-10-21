using System.Collections.Generic;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ SKITTER, THE GHOST NEMU SENDS FOR HER SLIPPER (owner 2026-10-15: *"Spawn siya ghost na kukunin slipper for her"*;
    /// the look from the approved lineup, `Visual.NemuGhostModels.Skitter`). Kuro is no longer sent: each skill has its own
    /// ghost, Gekko's pets the model. It pops out beside her, scrambles to the slipper, grabs it and hurries it back.
    ///
    /// Only the ability moves it (<see cref="Step"/>, from `NemuHeroKit.Skitter.OnTick`), so every peer's copy runs the same
    /// path from the accepted cast; only the host moves the slipper (the ability's gate). This draws it and, at the end,
    /// lets it go (<see cref="Leave"/>: up and gone).
    ///
    /// ⚠️ IT HOLDS THE SLIPPER, VISIBLY (owner on v1: *"doesnt look like theyre holding the slippers"*, *"not enough vfx, not
    /// enough animation"*). Its arms are their own meshes: scurrying they flail; near the slipper (<see cref="ReachFor"/>)
    /// they stretch out at it, claws snatching; at the grab (<see cref="Grab"/>) the body squashes with a green flash and a
    /// burst; carrying, its two hands close round the slipper's two ends, held crosswise in front of its grin
    /// (<see cref="CarryRotation"/>, which the host writes to the slipper), swinging with its scurry. Ectoplasm trails behind
    /// it and drips off it; its yellow eyes glow.
    /// </summary>
    public sealed partial class NemuSkitter : MonoBehaviour
    {
        public static readonly List<NemuSkitter> Live = new List<NemuSkitter>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLive() => Live.Clear();

        public int OwnerSlot { get; private set; } = -1;
        public Vector3 Position { get; private set; }
        /// <summary>Within reach of the last place it was sent.</summary>
        public bool Arrived { get; private set; }
        /// <summary>Where the slipper hangs while it carries (between its hands, riding its bob).</summary>
        public Vector3 HoldPoint => Position + Vector3.up * _bob + _facing * (NemuGhostModels.SkitterHands + OfferLift);
        /// <summary>Waiting by her with the slipper held out for her to take (owner 2026-10-15); set by the ability each step.</summary>
        public bool Offering { get; set; }
        private float _offer;
        /// <summary>Holding it out to her: raised and pushed forward, bobbing eagerly (hand space, metres).</summary>
        private Vector3 OfferLift => new Vector3(0f, .1f + .035f * Mathf.Sin(_age * 5f), .08f) * _offer;
        public bool Leaving => _leaving;
        /// <summary>The slipper it is about to grab (its arms stretch at it), or null.</summary>
        public Vector3? ReachFor { get; set; }
        public bool Carrying => _carrying;
        /// <summary>Which way it faces (flat, world).</summary>
        public Vector3 Forward => _facing * Vector3.forward;

        private const float PopSeconds = .3f, LeaveSeconds = .45f, GrabSeconds = .3f, Reach = .3f, Scale = 1f;
        private static readonly Color Ecto = NemuGhostModels.SkitterBody, EyeGlow = NemuGhostModels.SkitterEyes;
        private NemuGhostBody _body;
        // The pet bear's lively circles round its point (`PetGhostFlight`, drawing only, behind `PetGhostModels.PetGhosts`).
        private readonly PetGhostFlight _flight = new PetGhostFlight(PetGhostFlight.Kind.Bear);
        private readonly NemuGhostBody[] _arms = new NemuGhostBody[2];
        private NemuSmokeBatch _fx;
        private Quaternion _facing = Quaternion.identity;
        private float _age, _leftAt = -1f, _grabAt = -10f, _bob, _halfLength = .14f, _speed;
        private bool _leaving, _carrying, _lengthAlongX, _delivered;
        private Vector3 _velocity, _lastPosition;
        private readonly Vector3[] _trail = new Vector3[6];
        private float _trailClock;

        /// <summary>Pops a Skitter out beside Nemu (one per Nemu: her last one is let go).</summary>
        public static NemuSkitter Summon(Vector3 at, Vector3 facing, int ownerSlot)
        {
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null && Live[i].OwnerSlot == ownerSlot) Live[i].Leave();
            var go = new GameObject("NemuSkitter");
            var skitter = go.AddComponent<NemuSkitter>();
            skitter.OwnerSlot = ownerSlot;
            skitter.Position = at;
            skitter._lastPosition = at;
            for (int i = 0; i < skitter._trail.Length; i++) skitter._trail[i] = at;
            facing.y = 0f;
            skitter._facing = Quaternion.LookRotation(facing.sqrMagnitude > 1e-4f ? facing.normalized : Vector3.forward);
            var rim = new Color(.55f, 1f, .5f, 1f);
            skitter._body = new NemuGhostBody(go.transform, "SkitterBody", NemuGhostModels.Skitter, rim);
            for (int side = 0; side < 2; side++)
                skitter._arms[side] = new NemuGhostBody(skitter._body.Transform, side == 0 ? "SkitterArmL" : "SkitterArmR", NemuGhostModels.SkitterArm(side), rim);
            skitter._fx = new NemuSmokeBatch(go.transform, "SkitterFx", 6, 40, 1);
            return skitter;
        }

        public static NemuSkitter OwnedBy(int ownerSlot)
        {
            foreach (var s in Live) if (s != null && s.OwnerSlot == ownerSlot && !s.Leaving) return s;
            return null;
        }

        /// <summary>Tests put it straight somewhere (as they used to put Kuro, `GhostPetCompanion.ApplyCastAnchor`).</summary>
        public void Teleport(Vector3 at) { Position = at; _velocity = Vector3.zero; }

        /// <summary>Tests put its hands straight on <paramref name="hold"/>.</summary>
        public void TeleportHands(Vector3 hold) => Teleport(hold - (HoldPoint - Position));

        /// <summary>One step toward <paramref name="goal"/> at <paramref name="speed"/> m/s, turning to face where it goes.</summary>
        public void Step(Vector3 goal, float speed, float dt)
        {
            if (_leaving) return;
            Vector3 to = goal - Position;
            float d = to.magnitude;
            if (d > 1e-4f && dt > 0f)
            {
                Vector3 move = to * Mathf.Min(1f, speed * dt / d);
                _velocity = move / dt;
                Position += move;
                Vector3 flat = new Vector3(to.x, 0f, to.z);
                if (flat.sqrMagnitude > 1e-4f)
                    _facing = Quaternion.Slerp(_facing, Quaternion.LookRotation(flat.normalized), Mathf.Clamp01(dt * 12f));
            }
            else _velocity = Vector3.zero;
            Arrived = (goal - Position).sqrMagnitude <= Reach * Reach;
        }

        /// <summary>Turn to face <paramref name="point"/> (waiting by her, it looks at her, not where it last ran).</summary>
        public void Face(Vector3 point, float dt)
        {
            Vector3 flat = point - Position; flat.y = 0f;
            if (flat.sqrMagnitude > 1e-4f)
                _facing = Quaternion.Slerp(_facing, Quaternion.LookRotation(flat.normalized), Mathf.Clamp01(dt * 8f));
        }

        /// <summary>
        /// It snatches the slipper: from now its hands close round the slipper's two ends. <paramref name="halfLength"/> is half
        /// the slipper's length; <paramref name="lengthAlongX"/> says which of the slipper's own axes that length lies on.
        /// </summary>
        public void Grab(float halfLength, bool lengthAlongX)
        {
            if (_carrying) return;
            _carrying = true; _grabAt = _age; ReachFor = null;
            _halfLength = Mathf.Clamp(halfLength, .06f, .25f); _lengthAlongX = lengthAlongX;
        }

        /// <summary>
        /// The slipper's rotation in its hands: crosswise in front of it (its length along Skitter's right), sole down, toe
        /// tipped a little and swinging with its scurry. The host writes this to the slipper, and the slipper broadcast
        /// carries it.
        /// </summary>
        public Quaternion CarryRotation
        {
            get
            {
                float swing = Mathf.Sin(_age * 13f) * Mathf.Clamp01(_speed * .4f) * 14f;
                Quaternion along = _lengthAlongX ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
                return _facing * Quaternion.Euler(-10f, 0f, swing) * along;
            }
        }

        /// <summary>Done (delivered, dropped, cancelled): it zips up and fades. <paramref name="delivered"/>: a happy spin.</summary>
        public void Leave(bool delivered = false)
        {
            if (_leaving) return;
            _leaving = true; _leftAt = _age; _delivered = delivered; _carrying = false;
        }

        private void OnEnable() => Live.Add(this);
        private void OnDisable() => Live.Remove(this);
        private void OnDestroy() { _fx?.Dispose(); _fx = null; }

        private void Update()
        {
            float dt = Time.deltaTime;
            StepNetworkPose(dt);
            _age += dt;
            // Speed from where it actually got to (the ability steps it; tests teleport it).
            Vector3 moved = Position - _lastPosition; _lastPosition = Position;
            float measured = dt > 0f ? new Vector3(moved.x, 0f, moved.z).magnitude / dt : 0f;
            _speed = Mathf.Lerp(_speed, Mathf.Min(measured, 8f), Mathf.Clamp01(dt * 10f));
            _offer = Mathf.MoveTowards(_offer, Offering && _carrying && !_leaving ? 1f : 0f, dt * 4f);

            // POP: out of a green burst with an overshoot.
            float p = Mathf.Clamp01(_age / PopSeconds);
            float pop = p >= 1f ? 1f : 1f + Mathf.Sin(p * Mathf.PI * 1.25f) * (1f - p) * .6f - (1f - p) * (1f - p);
            pop = Mathf.Max(0f, pop);
            float fade = 1f, rise = 0f, spin = 0f, leaveU = 0f;
            if (_leaving)
            {
                leaveU = Mathf.Clamp01((_age - _leftAt) / LeaveSeconds);
                fade = 1f - leaveU * leaveU; rise = 1.4f * leaveU * leaveU;
                spin = _delivered ? 540f * leaveU * leaveU : 0f;
                if (leaveU >= 1f) { Destroy(gameObject); return; }
            }

            // THE SCURRY: quick hops that squash on landing and stretch in the air, leaning into where it goes.
            float run = Mathf.Clamp01(_speed / 3f);
            float stride = _age * (9f + _speed * 2.5f);
            float hop = Mathf.Abs(Mathf.Sin(stride));
            // WAITING: little eager bounces in place, "here, take it".
            float eager = _offer * (1f - run) * Mathf.Abs(Mathf.Sin(_age * 6f));
            _bob = Mathf.Lerp(.03f * Mathf.Sin(_age * 3f), .09f * hop, run) + .05f * eager;
            if (_networkDriven && _carrying)
                foreach (var shoe in BotSlipperInventory.All)
                    if (shoe != null && shoe.OwnerSlot == OwnerSlot && shoe.State == SlipperState.Loose)
                    { Position = shoe.transform.position - Vector3.up * _bob - _facing * (NemuGhostModels.SkitterHands + OfferLift); break; }
            float squash = Mathf.Lerp(.04f * Mathf.Sin(_age * 3f), (hop - .5f) * .22f, run);
            // THE GRAB: a hard squash and a green flash.
            float g = Mathf.Clamp01((_age - _grabAt) / GrabSeconds);
            float grabPulse = g < 1f ? Mathf.Sin(g * Mathf.PI) * (1f - g) : 0f;
            squash -= grabPulse * .45f;
            squash += (eager - .5f * _offer * (1f - run)) * .14f;
            var scale = new Vector3(1f - squash * .5f, 1f + squash, 1f - squash * .5f) * (Scale * pop * Mathf.Lerp(1f, .4f, leaveU));
            float lean = Mathf.Clamp(_speed * 5f, 0f, 22f) - (_carrying ? 8f : 0f);
            Quaternion facing = _facing * Quaternion.AngleAxis(spin, Vector3.up) * Quaternion.AngleAxis(lean, Vector3.right)
                * Quaternion.AngleAxis(Mathf.Sin(stride * .5f) * 7f * run, Vector3.forward);
            float flash = Mathf.Max(grabPulse * 1.2f, (1f - p) * .8f);
            Vector3 at = Position + Vector3.up * (_bob + rise);
            if (PetGhostModels.PetGhosts && _body != null)
            {
                // The pet bear circles round its point while it is free (never while it reaches, carries or leaves: then it is
                // on the point), and spins for joy as it grabs. Drawing only: `Position` and the hold point do not move.
                float free = !_carrying && !ReachFor.HasValue && !_leaving ? 1f - run : 0f;
                _flight.Apply(ref at, ref facing, _age, free, dt);
                if (g < 1f) facing *= Quaternion.AngleAxis(360f * g * g * (3f - 2f * g), Vector3.up);
                _body.Trail = _flight.TrailLocal;
            }
            _body?.Pose(at, facing, scale, fade, _age, .08f + _speed * .015f, Mathf.Clamp01(flash));

            PoseArms(fade, flash, run, stride);
            DrawFx(at, fade, p, g, leaveU);
        }

        private void PoseArms(float fade, float flash, float run, float stride)
        {
            if (_body == null) return;
            var body = _body.Transform;
            for (int side = 0; side < 2; side++)
            {
                if (_arms[side] == null) continue;
                Vector3 shoulder = NemuGhostModels.SkitterShoulder[side];
                float sign = side == 0 ? -1f : 1f;
                Vector3 hand;
                if (_carrying)
                    // Its hands on the slipper's two ends, gripping tight (a little squeeze in time with the scurry).
                    hand = NemuGhostModels.SkitterHands + OfferLift + Vector3.right * sign * (_halfLength + .015f - .01f * Mathf.Abs(Mathf.Sin(stride)));
                else if (ReachFor.HasValue)
                {
                    // Stretching out at the slipper, claws snatching, one then the other.
                    Vector3 target = body.InverseTransformPoint(ReachFor.Value);
                    float snatch = Mathf.Sin(_age * 22f + side * Mathf.PI);
                    hand = target + Vector3.right * sign * (.08f + .04f * snatch) + Vector3.up * (.05f + .03f * snatch);
                }
                else
                    // Flailing as it scurries: arms out in front, pumping in turn; idle, they hang and flex.
                    hand = shoulder + new Vector3(sign * .07f, -.12f - .05f * Mathf.Sin(stride + side * Mathf.PI) * run,
                        .2f + .08f * Mathf.Sin(stride + side * Mathf.PI) * run + .02f * Mathf.Sin(_age * 4f + side));
                Vector3 to = hand - shoulder;
                float length = to.magnitude;
                Quaternion rot = length > 1e-4f ? Quaternion.LookRotation(to / length, Vector3.up) : Quaternion.identity;
                float stretch = Mathf.Clamp(length / NemuGhostModels.SkitterHand.z, .5f, PetGhostModels.PetGhosts ? PetGhostModels.MaxArmStretch : 3.2f);
                _arms[side].PoseLocal(shoulder, rot, fade, _age, flash);
                _arms[side].Transform.localScale = new Vector3(1f, 1f, stretch);
            }
        }

        private void DrawFx(Vector3 at, float fade, float pop, float grab, float leaveU)
        {
            if (_fx == null) return;
            _fx.Begin();
            if (fade > .005f && _body != null)
            {
                var body = _body.Transform;
                // Its eyes.
                foreach (var eye in NemuGhostModels.SkitterEyePoints)
                    _fx.Light(body.TransformPoint(eye), .09f, (_carrying ? 1.1f : .8f) * fade, EyeGlow);
                // Its aura.
                _fx.Light(body.TransformPoint(new Vector3(0f, .4f, 0f)), .75f, .28f * fade, Ecto);

                // ECTOPLASM TRAIL: where it was a moment ago, thinning out behind it.
                _trailClock += Time.deltaTime;
                if (_trailClock > .035f)
                {
                    _trailClock = 0f;
                    for (int i = _trail.Length - 1; i > 0; i--) _trail[i] = _trail[i - 1];
                    _trail[0] = at + Vector3.up * .3f;
                }
                float run = Mathf.Clamp01(_speed / 3f);
                for (int i = 1; i < _trail.Length; i++)
                {
                    float k = 1f - i / (float)_trail.Length;
                    Vector3 along = _trail[i - 1] - _trail[i];
                    _fx.Light(_trail[i], .22f * k + .05f, .7f * k * run * fade, Ecto, along);
                }
                // DRIPS: blobs of it falling off its underside.
                for (int d = 0; d < 3; d++)
                {
                    float u = Mathf.Repeat(_age * 1.4f + d * .37f, 1f);
                    Vector3 from = body.TransformPoint(new Vector3((d - 1) * .09f, .22f, (d % 2) * .06f - .03f));
                    _fx.Light(from - Vector3.up * (u * u * .45f), .06f * (1f - u * .5f), .9f * (1f - u) * fade, Ecto);
                }
                // HOLDING IT: a green glow in its grip, round the slipper.
                if (_carrying)
                {
                    Vector3 hold = body.TransformPoint(NemuGhostModels.SkitterHands + OfferLift);
                    Vector3 right = body.right * (_halfLength + .02f);
                    _fx.Light(hold, .4f, .55f * fade, Ecto, right * 2f);
                    _fx.Light(hold + right, .14f, .9f * fade, Ecto);
                    _fx.Light(hold - right, .14f, .9f * fade, Ecto);
                    // OFFERING: a slow green pulse ringing the slipper, "it's yours, take it".
                    if (_offer > .01f)
                    {
                        float beat = Mathf.Repeat(_age * 1.3f, 1f);
                        for (int i = 0; i < 10; i++)
                        {
                            float a = i * Mathf.PI / 5f + _age;
                            Vector3 ring = (body.right * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * (.18f + beat * .22f);
                            _fx.Light(hold + ring, .07f, .9f * (1f - beat) * _offer * fade, Ecto);
                        }
                    }
                }
                // REACHING: its claws glow as they close in.
                if (ReachFor.HasValue)
                    _fx.Light(ReachFor.Value + Vector3.up * .05f, .35f, .5f + .3f * Mathf.Sin(_age * 22f), Ecto);
            }
            // SUMMON: a green ring bursting out of a puff of her smoke.
            if (pop < 1f)
            {
                float r = .15f + pop * .6f;
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4f;
                    _fx.Light(Position + Vector3.up * .3f + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r, .14f, 1.3f * (1f - pop), Ecto);
                }
                _fx.Puff(Position + Vector3.up * .3f, .6f + pop * .5f, .55f * (1f - pop), .3f, pop, .9f);
            }
            // GRAB: sparks flying out of its hands.
            if (grab < 1f && _body != null)
            {
                Vector3 hold = _body.Transform.TransformPoint(NemuGhostModels.SkitterHands);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4f + .3f;
                    Vector3 dir = new Vector3(Mathf.Cos(a), .6f + .4f * Mathf.Sin(a * 3f), Mathf.Sin(a)).normalized;
                    _fx.Light(hold + dir * (.08f + grab * .45f), .1f * (1f - grab) + .03f, 1.4f * (1f - grab), Ecto, dir * .12f);
                }
            }
            // LEAVING: a streak up, and a puff where it was.
            if (_leaving)
            {
                _fx.Light(Position + Vector3.up * (.4f + leaveU * 1.2f), .3f, 1.2f * (1f - leaveU), Ecto, Vector3.up * (.6f * leaveU + .1f));
                _fx.Puff(Position + Vector3.up * .35f, .5f + leaveU * .4f, .45f * (1f - leaveU), .7f, leaveU, .9f);
            }
            _fx.End(_age);
        }

        /// <summary>Every Skitter on the court goes (the round ended).</summary>
        public static void RetireAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null) Destroy(Live[i].gameObject);
            Live.Clear();
        }
    }
}
