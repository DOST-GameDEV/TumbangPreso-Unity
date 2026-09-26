using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Where KAPIT-BAGING's vines catch: the first wall or prop along his aim within
    /// `PaeteRules.VineRange`, else the ground at full range. It always finds somewhere (research.md
    /// § 2, Kinich: the grapple never visibly fails). Bodies, slippers and the can are not scenery.
    /// </summary>
    public static class PaeteVine
    {
        public static Vector3 FindAnchor(Vector3 feet, Vector3 forward, Vector3 aimPoint)
        {
            Vector3 origin = feet + Vector3.up * 1.3f;
            Vector3 dir = aimPoint - origin;
            if (dir.sqrMagnitude < 0.25f) dir = forward;
            dir.Normalize();

            Vector3 anchor = origin + dir * PaeteRules.VineRange;
            float best = float.PositiveInfinity;
            foreach (var hit in Physics.RaycastAll(origin, dir, PaeteRules.VineRange, ~0, QueryTriggerInteraction.Ignore))
            {
                var c = hit.collider;
                if (c == null || hit.distance >= best) continue;
                if (c.GetComponentInParent<CharacterMotor>() != null) continue;
                if (c.GetComponentInParent<Slipper>() != null) continue;
                if (c.GetComponentInParent<Lata>() != null) continue;
                best = hit.distance;
                anchor = hit.point + hit.normal * 0.25f;
            }
            // Aimed at the sky with nothing to catch: the vines take the ground under that point.
            if (float.IsPositiveInfinity(best)) anchor.y = Mathf.Min(anchor.y, feet.y + 0.2f);
            anchor.x = Mathf.Clamp(anchor.x, -AIController.PlayableHalfX + 0.4f, AIController.PlayableHalfX - 0.4f);
            anchor.z = Mathf.Clamp(anchor.z, -AIController.PlayableHalfZ + 0.4f, AIController.PlayableHalfZ - 0.4f);
            anchor.y = Mathf.Max(anchor.y, feet.y + 0.05f);
            return anchor;
        }

        /// <summary>A throw target: <paramref name="aimPoint"/> on the ground, pulled in to <paramref name="range"/> and kept inside the court.</summary>
        public static Vector3 GroundTarget(Vector3 feet, Vector3 forward, Vector3 aimPoint, float range)
        {
            Vector3 d = aimPoint - feet; d.y = 0f;
            if (d.sqrMagnitude < 0.04f) { d = forward; d.y = 0f; d = d.normalized * range; }
            if (d.magnitude > range) d = d.normalized * range;
            Vector3 p = feet + d;
            p.x = Mathf.Clamp(p.x, -AIController.PlayableHalfX + 0.5f, AIController.PlayableHalfX - 0.5f);
            p.z = Mathf.Clamp(p.z, -AIController.PlayableHalfZ + 0.5f, AIController.PlayableHalfZ - 0.5f);
            p.y = Slipper.GroundY(p);
            return p;
        }
    }

    /// <summary>
    /// ⚠️⚠️ PUNLANG TSINELAS'S SEEDLING. Owner's table: *"He summons a plant/tree that creates wooden
    /// slippers taht shoot slippers on command"*; on pulling it out: *"invincible for the first 15
    /// seconds but after that make a visual indicator showing that it can be pulled out? maybe make
    /// the model gradually change too"*, *"i want the animation for pull out to be good"*.
    ///
    /// It exists on every peer from the accepted cast, on its own clock. Every peer draws it and
    /// decides locally whether a shot is ready (the clock is the same everywhere); ONLY THE HOST
    /// resolves what a wooden slipper hits and whether a pull-out is legal, and the pull reaches
    /// everyone through `MatchRpc.BroadcastPlantPulled`.
    /// </summary>
    public sealed class PaetePlant : MonoBehaviour
    {
        public static readonly List<PaetePlant> Live = new List<PaetePlant>();

        public int OwnerSlot { get; private set; } = -1;
        public float Age => _age;
        public bool Landed => _age >= 0f;
        public bool Pullable => _age >= PaeteRules.PlantRootedSeconds && !_pulled;
        public bool ShotReady => Landed && !_pulled && _age >= _nextShot;
        /// <summary>How far the next wooden slipper has grown, 0 to 1, for the pod.</summary>
        public float ShotGrowth => Mathf.Clamp01(1f - (_nextShot - _age) / PaeteRules.PlantReloadSeconds);

        private float _age, _nextShot, _pulledAge, _recoil = 99f;
        private bool _pulled;
        private Vector3 _pullFrom;
        private readonly Dictionary<CharacterMotor, float> _pulling = new Dictionary<CharacterMotor, float>();
        private readonly HashSet<CharacterMotor> _requested = new HashSet<CharacterMotor>();
        private PaetePlantBody _body;

        public static PaetePlant Spawn(Vector3 from, Vector3 at, int ownerSlot, float flightSeconds = 0.35f)
        {
            // One plant each: a new cast replaces the old, which withers where it stands.
            foreach (var old in Live.ToArray()) if (old != null && old.OwnerSlot == ownerSlot) old.Wither();
            var go = new GameObject("PaetePlant");
            go.transform.position = at;
            var plant = go.AddComponent<PaetePlant>();
            plant.OwnerSlot = ownerSlot;
            plant._age = -Mathf.Max(0f, flightSeconds);
            plant._nextShot = PaeteRules.PlantFirstShotSeconds;
            plant._body = PaetePlantBody.Build(go.transform);
            if (flightSeconds > 0f) PaeteSeedArc.Throw(from, at, flightSeconds, 0.14f, false);
            Live.Add(plant);
            plant._body.Pose(plant._age, 0f, false, 0f, 99f);
            return plant;
        }

        public static PaetePlant OwnedBy(int ownerSlot)
        {
            foreach (var p in Live) if (p != null && p.OwnerSlot == ownerSlot && !p._pulled) return p;
            return null;
        }

        /// <summary>Where a wooden slipper leaves the pod.</summary>
        public Vector3 Muzzle => transform.position + Vector3.up * 0.95f;

        /// <summary>
        /// The command (the ability's second press, on every peer): if a wooden slipper has grown,
        /// the pod pulls back and snaps forward and throws it at <paramref name="aimPoint"/>.
        /// </summary>
        public bool Fire(Vector3 aimPoint)
        {
            if (!ShotReady) return false;
            _nextShot = _age + PaeteRules.PlantReloadSeconds;
            _recoil = 0f;
            Vector3 target = aimPoint;
            PaeteWoodenSlipper.Spawn(Muzzle, target, OwnerSlot);
            GameServices.Audio?.PlayAt("sfx_paete_sprout_fire", transform.position);
            return true;
        }

        public void Wither()
        {
            if (_pulled) return;
            _age = Mathf.Max(_age, PaeteRules.PlantLifeSeconds - 0.6f);
        }

        /// <summary>
        /// Host: <paramref name="who"/> has held Interact at a plant long enough. Legal only for an
        /// opponent in reach of a plant past its rooted 15 s.
        /// </summary>
        public static bool HostTryUproot(CharacterMotor who)
        {
            if (!NetAuthority.ShouldResolve() || who == null) return false;
            PaetePlant best = null; float bestDistance = float.MaxValue;
            foreach (var p in Live)
            {
                if (p == null || p.OwnerSlot == who.PlayerSlot || !p.Pullable) continue;
                float d = Flat(p.transform.position - who.transform.position).magnitude;
                if (d <= PaeteRules.PlantPullReach + 0.35f && d < bestDistance) { best = p; bestDistance = d; }
            }
            if (best == null) return false;
            Net.MatchRpc.Instance?.BroadcastPlantPulled(best.OwnerSlot, who.PlayerSlot);
            if (!NetAuthority.IsNetworked) ApplyPulled(best.OwnerSlot, who.PlayerSlot);
            return true;
        }

        /// <summary>Every peer: the plant owned by <paramref name="ownerSlot"/> comes out of the ground.</summary>
        public static void ApplyPulled(int ownerSlot, int pullerSlot)
        {
            var plant = OwnedBy(ownerSlot);
            if (plant == null) return;
            plant._pulled = true;
            plant._pulledAge = 0f;
            var puller = GameServices.Round?.PlayerAt(pullerSlot);
            plant._pullFrom = puller != null ? puller.transform.position : plant.transform.position + Vector3.back;
            GameServices.Audio?.PlayAt("sfx_paete_sprout_uproot", plant.transform.position);
            PaeteLeafBurst.Spawn(plant.transform.position + Vector3.up * 0.4f, 7, 1.8f);
            puller?.GetComponentInChildren<CharacterSquashStretch>()?.Stretch(0.22f);
        }

        private void OnDestroy()
        {
            Live.Remove(this);
            // A plant that withers or is pulled mid-hold must not leave a puller frozen in the heave.
            foreach (var p in _pulling.Keys) if (p != null) p.PullingPlantProgress = 0f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            _recoil += dt;
            if (_pulled)
            {
                _pulledAge += dt;
                _body.PosePulled(_pulledAge, _pullFrom - transform.position);
                if (_pulledAge >= 0.95f) Destroy(gameObject);
                return;
            }
            if (_age >= PaeteRules.PlantLifeSeconds) { Destroy(gameObject); return; }
            float loosen = Mathf.Clamp01((_age - PaeteRules.PlantRootedSeconds) / (PaeteRules.PlantLifeSeconds - PaeteRules.PlantRootedSeconds));
            _body.Pose(_age, loosen, Pullable, ShotReady ? 1f : ShotGrowth, _recoil);
            StepPullers(dt);
        }

        /// <summary>
        /// The pull-out is read where the input is: every body this peer simulates (the local human,
        /// and on the host its bots) holding Interact next to someone else's pullable plant fills a
        /// hold; at `PaeteRules.PlantPullSeconds` the host pulls it, a client asks the host once.
        /// </summary>
        private void StepPullers(float dt)
        {
            var round = GameServices.Round;
            if (round == null || !Pullable) { _pulling.Clear(); return; }
            foreach (var p in round.Players)
            {
                if (p == null || p.PlayerSlot == OwnerSlot || !p.IsLocallySimulated()) continue;
                bool holding = p.Intent != null && p.Intent.Pressed(Verb.Interact) && p.CanAct()
                    && Flat(transform.position - p.transform.position).magnitude <= PaeteRules.PlantPullReach;
                if (!holding) { _pulling.Remove(p); _requested.Remove(p); p.PullingPlantProgress = 0f; continue; }
                _pulling.TryGetValue(p, out float held);
                held += dt;
                _pulling[p] = held;
                p.PullingPlantProgress = Mathf.Clamp01(held / PaeteRules.PlantPullSeconds);
                if (held < PaeteRules.PlantPullSeconds || _requested.Contains(p)) continue;
                _requested.Add(p);
                if (NetAuthority.ShouldResolve()) HostTryUproot(p);
                else Net.MatchRpc.Instance?.RequestUproot(p.PlayerSlot, p.transform.position);
            }
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }

    /// <summary>
    /// A wooden slipper from the seedling. Owner: *"maybe lessened plus"*: a knockdown pays
    /// `ScoreEvent.SproutKnock`, half a real one. It flies on every peer from the same muzzle and
    /// aim; only the host asks the can. It is never picked up: it lies where it lands and withers.
    /// </summary>
    public sealed class PaeteWoodenSlipper : MonoBehaviour
    {
        private Vector3 _velocity;
        private int _owner;
        private bool _landed, _scored;
        private float _restAge;
        private Transform _body;

        public static PaeteWoodenSlipper Spawn(Vector3 origin, Vector3 target, int ownerSlot)
        {
            var go = new GameObject("PaeteWoodenSlipper");
            go.transform.position = origin;
            var s = go.AddComponent<PaeteWoodenSlipper>();
            s._owner = ownerSlot;
            s._velocity = Slipper.SolveArc(origin, target, PaeteRules.WoodenSlipperSpeed);
            s._body = new GameObject("body").transform;
            s._body.SetParent(go.transform, false);
            GrowthVfx.Block(s._body, "sole", new Vector3(0.12f, 0.035f, 0.28f), GrowthVfx.BarkLit);
            var strap = GrowthVfx.Block(s._body, "strap-a", new Vector3(0.02f, 0.05f, 0.10f), GrowthVfx.Vine).transform;
            strap.localPosition = new Vector3(0.03f, 0.03f, 0.04f); strap.localRotation = Quaternion.Euler(0, 30, 0);
            var strap2 = GrowthVfx.Block(s._body, "strap-b", new Vector3(0.02f, 0.05f, 0.10f), GrowthVfx.Vine).transform;
            strap2.localPosition = new Vector3(-0.03f, 0.03f, 0.04f); strap2.localRotation = Quaternion.Euler(0, -30, 0);
            return s;
        }

        private void FixedUpdate()
        {
            if (_landed) return;
            float dt = Time.fixedDeltaTime;
            _velocity.y -= Balance.Gravity * dt;
            transform.position += _velocity * dt;
            _body.Rotate(0f, 0f, 900f * dt, Space.Self);
            if (_velocity.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(_velocity);

            if (!_scored && NetAuthority.ShouldResolve())
            {
                var lata = GameServices.Round?.Lata;
                if (lata != null && lata.IsUpright && lata.Connects(transform.position))
                {
                    _scored = true;
                    lata.HostKnockDown(_owner, ScoreEvent.SproutKnock);
                    _velocity = -_velocity * 0.25f;
                }
            }

            float ground = Slipper.GroundY(transform.position);
            if (transform.position.y <= ground + 0.03f && _velocity.y < 0f)
            {
                _landed = true;
                transform.position = new Vector3(transform.position.x, ground + 0.02f, transform.position.z);
                transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
                _body.localRotation = Quaternion.identity;
                GameServices.Audio?.PlayAt("sfx_paete_sprout_land", transform.position);
            }
            if (transform.position.y < -20f) Destroy(gameObject);
        }

        private void Update()
        {
            if (!_landed) return;
            _restAge += Time.deltaTime;
            float left = PaeteRules.WoodenSlipperWitherSeconds - _restAge;
            if (left <= 0f) { PaeteLeafBurst.Spawn(transform.position, 3, 0.7f); Destroy(gameObject); return; }
            if (left < 0.6f) _body.localScale = Vector3.one * (left / 0.6f);
        }
    }

    /// <summary>
    /// ⚠️⚠️ BAWI'S THORN CONSTRUCT. Owner's table: *"He creates a plant /tree construct that extends
    /// towards all players and PULL their slippers towards it"*, and: *"i want it too be ALL, even
    /// the ones on the hands"*.
    ///
    /// Every slipper within `PaeteRules.ThornRange` of his feet that is not his own is caught: out
    /// of a hand, out of the air, off the ground. Caught, held a beat (Scorpion's hold, research.md
    /// § 2), then yanked to land `ThornLandDistance` from the construct. Never the lata: moving the
    /// objective would be scoring outside `MatchDirector.AddScore`. Every peer draws it; only the
    /// host moves slippers, and their positions already stream to everyone.
    /// </summary>
    public sealed class PaeteThorns : MonoBehaviour
    {
        public int OwnerSlot { get; private set; } = -1;
        public Vector3 Origin { get; private set; }

        private float _age;
        private readonly List<Slipper> _caught = new List<Slipper>();
        private readonly List<Vector3> _landAt = new List<Vector3>();
        private bool _resolved;
        private PaeteThornBody _body;

        public static PaeteThorns Spawn(Vector3 origin, int ownerSlot)
        {
            var go = new GameObject("PaeteThorns");
            go.transform.position = origin;
            var t = go.AddComponent<PaeteThorns>();
            t.OwnerSlot = ownerSlot; t.Origin = origin;
            // Which slippers the thorns reach for: the same test on every peer, so the vines every
            // player sees go to the slippers the host is moving.
            foreach (var shoe in Object.FindObjectsByType<Slipper>())
            {
                if (shoe == null || !shoe.gameObject.activeInHierarchy || shoe.OwnerSlot == ownerSlot) continue;
                Vector3 d = shoe.transform.position - origin; d.y = 0f;
                if (d.magnitude > PaeteRules.ThornRange) continue;
                t._caught.Add(shoe);
                Vector3 dir = d.sqrMagnitude > 0.01f ? d.normalized : Vector3.forward;
                t._landAt.Add(origin + dir * PaeteRules.ThornLandDistance);
            }
            t._body = PaeteThornBody.Build(go.transform, t._caught);
            GameServices.Audio?.PlayAt("sfx_paete_thorn_burst", origin);
            return t;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            _body.Pose(_age, Origin);
            if (_age >= PaeteRules.ThornConstructSeconds) Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            if (!NetAuthority.ShouldResolve()) return;
            if (!_resolved)
            {
                _resolved = true;
                foreach (var shoe in _caught)
                    if (shoe != null && shoe.HostSnatch()) Net.MatchRpc.Instance?.BroadcastSlipperState(shoe);
                return;
            }
            float start = PaeteRules.ThornHoldSeconds, end = start + PaeteRules.ThornYankSeconds;
            if (_age < start || _age > end + 0.1f) return;
            float dt = Time.fixedDeltaTime;
            for (int i = 0; i < _caught.Count; i++)
            {
                var shoe = _caught[i];
                if (shoe == null || shoe.State != SlipperState.Loose) continue;
                Vector3 to = _landAt[i] - shoe.transform.position; to.y = 0f;
                float remaining = Mathf.Max(0.02f, end - _age);
                float step = Mathf.Min(to.magnitude, to.magnitude * dt / remaining);
                if (step < 0.001f) continue;
                shoe.HostSweepLoose(to.normalized * step);
            }
        }
    }

    /// <summary>
    /// ⚠️⚠️ YAKAP NG MAKILING'S SENTRY. Owner's table: *"He puts down a plant/tree sentry in a
    /// location and he PULLS all players here, they get stuck on it (switchhes to tpp view) and they
    /// have to hold a button ... for like 7 seconds while stuck here"*, *"THEY CAN STILL THROW AND USE
    /// SKILLS WHILE STUCK BTW theyre js rooted"*; *"WITHIN 9 meters"*; a tag frees them.
    ///
    /// Thrown as a glowing seed after the introduction (Groot's Strangling Prison, research.md § 2);
    /// it bursts into the sentry, a beat later the vines catch every other player within 9 m and drag
    /// them in (a carry per body, `CharacterMotor.ApplyResolvedCarry`), and they are Rooted for the
    /// rest of its life. Host resolves; every peer draws it.
    /// </summary>
    public sealed class PaeteSentry : MonoBehaviour
    {
        public int OwnerSlot { get; private set; } = -1;
        public Vector3 Centre { get; private set; }
        public float Age => _age;

        private float _age;
        private bool _caught;
        private readonly List<CharacterMotor> _held = new List<CharacterMotor>();
        private PaeteSentryBody _body;
        private const float Flight = 0.45f;

        public static PaeteSentry Spawn(Vector3 from, Vector3 at, int ownerSlot, float age = 0f)
        {
            var go = new GameObject("PaeteSentry");
            go.transform.position = at;
            var s = go.AddComponent<PaeteSentry>();
            s.OwnerSlot = ownerSlot; s.Centre = at;
            s._age = age - Flight;
            s._body = PaeteSentryBody.Build(go.transform);
            if (age <= 0f) PaeteSeedArc.Throw(from, at, Flight, 0.26f, true);
            // Who the vines reach for is drawn on every peer from the same rule the host uses.
            var round = GameServices.Round;
            if (round != null)
                foreach (var p in round.Players)
                    if (p != null && p.PlayerSlot != ownerSlot && InReach(at, p)) s._held.Add(p);
            s._body.SetTargets(s._held);
            return s;
        }

        public static bool InReach(Vector3 centre, CharacterMotor p)
        {
            Vector3 d = p.transform.position - centre; d.y = 0f;
            return d.magnitude <= PaeteRules.SentryRadius && !p.IsAloft;
        }

        private void Update()
        {
            float before = _age;
            _age += Time.deltaTime;
            // ⚠️ LOCAL ON EVERY PEER, NOT `NetCue`: each peer runs this same clock from the accepted
            // cast, so each plays each stage once (`audit_cue_relay.py`).
            if (before < 0f && _age >= 0f) GameServices.Audio?.PlayAt("sfx_paete_sentry_burst", Centre);
            if (before < PaeteRules.SentryCatchSeconds && _age >= PaeteRules.SentryCatchSeconds && _held.Count > 0)
                GameServices.Audio?.PlayAt("sfx_paete_sentry_catch", Centre);
            if (before < PaeteRules.SentryLifeSeconds && _age >= PaeteRules.SentryLifeSeconds)
                GameServices.Audio?.PlayAt("sfx_paete_sentry_wilt", Centre);
            _body.Pose(_age, Centre);
            if (_age >= 0f)
                foreach (var p in _held)
                    if (p != null && p.IsRooted) PaeteRootCoil.Attach(p);
            if (_age >= PaeteRules.SentryLifeSeconds + 0.6f) Destroy(gameObject);
        }

        // When each caught body's drag arrives, and it is rooted (host only).
        private readonly Dictionary<CharacterMotor, float> _rootAt = new Dictionary<CharacterMotor, float>();

        private void FixedUpdate()
        {
            if (_age < PaeteRules.SentryCatchSeconds || !NetAuthority.ShouldResolve()) return;
            if (!_caught)
            {
                _caught = true;
                foreach (var p in _held)
                {
                    if (p == null || p.IsTagged) continue;
                    Vector3 d = Centre - p.transform.position; d.y = 0f;
                    float distance = d.magnitude;
                    float arrive = 0f;
                    if (distance > PaeteRules.SentryHoldDistance + 0.05f)
                    {
                        arrive = PaeteRules.SentryPullArriveSeconds(distance);
                        p.ApplyResolvedCarry(d.normalized * PaeteRules.SentryPullSpeedFor(distance) + Vector3.up * 1.2f,
                                             PaeteRules.SentryPullHoldSeconds(distance));
                    }
                    // ⚠️⚠️ ROOTED WHEN THE DRAG ARRIVES, NOT ON THE SAME STEP. The first play of this kit
                    // (`PaeteKitPlayProbe`, 2026-09-26) found both bodies rooted where they stood, 2.5 m
                    // out: Rooted zeroes external velocity (`StatusSpeedScale`) and releases the carry's
                    // commitment, so rooting at the catch killed the pull it was meant to finish. The carry
                    // itself holds the body for the drag, so nobody walks out of it.
                    _rootAt[p] = _age + arrive;
                }
            }
            if (_rootAt.Count == 0) return;
            float left = PaeteRules.SentryLifeSeconds - _age;
            foreach (var p in new List<CharacterMotor>(_rootAt.Keys))
            {
                if (p == null || p.IsTagged) { _rootAt.Remove(p); continue; }
                if (_age < _rootAt[p]) continue;
                _rootAt.Remove(p);
                if (left > 0f) p.ApplyRooted(left);
            }
        }
    }
}
