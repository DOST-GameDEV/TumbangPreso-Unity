using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Where LIANA LEAP's vines catch: the first wall or prop along his aim within
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

        /// <summary>
        /// ⚠️⚠️ THE GUARDIAN'S SPOT, KEPT OFF THE CAN (owner, 2026-09-27: *"make it so that it cant block the can too (dont let
        /// it be placed in a place it STANDS on can)"*). <see cref="GroundTarget"/>, then `PaeteRules.SentrySpotClearOfCan`
        /// against wherever the lata is now, upright or knocked down. Every peer runs this from the same accepted cast and
        /// the same synced can, and the host catches from the same spot; the cutscene pushes its staged tree by the same rule.
        /// </summary>
        public static Vector3 SentryTarget(Vector3 feet, Vector3 forward, Vector3 aimPoint, Lata lata)
        {
            Vector3 p = GroundTarget(feet, forward, aimPoint, PaeteRules.SentryThrowRange);
            return lata != null ? ClearOfCan(p, lata.transform.position, feet) : p;
        }

        /// <summary>`PaeteRules.SentrySpotClearOfCan` on world points, inside the same court margin <see cref="GroundTarget"/> keeps.</summary>
        public static Vector3 ClearOfCan(Vector3 spot, Vector3 can, Vector3 from)
        {
            PaeteRules.SentrySpotClearOfCan(spot.x, spot.z, can.x, can.z, from.x, from.z,
                                            AIController.PlayableHalfX - 0.5f, AIController.PlayableHalfZ - 0.5f, out float x, out float z);
            var p = new Vector3(x, spot.y, z);
            p.y = Slipper.GroundY(p);
            return p;
        }
    }

    /// <summary>
    /// ⚠️⚠️ BAKYA BLOOM'S SEEDLING. Owner's table: *"He summons a plant/tree that creates wooden
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

        /// <summary>
        /// The seedling for a rejoining peer and the replay (`WorldEffectSnapshot.Kind.Plant`): where,
        /// whose, how old, and how long until its next wooden slipper (FirstScale), so the rejoiner's
        /// plant fires on the same clock as everyone else's. A pulled plant is not captured.
        /// </summary>
        public Net.WorldEffectSnapshot.Field Capture() => new Net.WorldEffectSnapshot.Field
        {
            Type = Net.WorldEffectSnapshot.Kind.Plant, Source = gameObject, Position = transform.position,
            Forward = Vector3.forward, Duration = PaeteRules.PlantLifeSeconds,
            Remaining = Mathf.Clamp(PaeteRules.PlantLifeSeconds - Mathf.Max(0f, _age), 0f, PaeteRules.PlantLifeSeconds),
            Radius = 1f, Owner = OwnerSlot, FirstScale = Mathf.Max(0f, _nextShot - _age),
        };

        public bool IsPulled => _pulled;

        /// <summary>A seedling put back at <paramref name="age"/> seconds old with its shot clock; no seed flight, no second ground break.</summary>
        public static PaetePlant Restore(Vector3 at, int ownerSlot, float age, float untilShot)
        {
            var plant = Spawn(at, at, ownerSlot, 0f);
            plant._age = Mathf.Max(0.5f, age);
            plant._nextShot = plant._age + Mathf.Clamp(untilShot, 0f, PaeteRules.PlantReloadSeconds);
            return plant;
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
            if (_age < 0f && _age + dt >= 0f) Visual.PaeteGroundBreak.Spawn(transform.position, 0.7f);
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
            // The shot has grown: a soft pod pop, so its owner hears it is loaded (direction.md section 5.3).
            // Local on every peer off the same clock, like the fire.
            if (_age >= _nextShot && _age - dt < _nextShot && _age > 0.5f)
                GameServices.Audio?.PlayAt("sfx_paete_sprout_ready", transform.position);
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
    /// ⚠️⚠️ THORN HARVEST'S THORN CONSTRUCT. Owner's table: *"He creates a plant /tree construct that extends
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
            Visual.PaeteGroundBreak.Spawn(origin, 1.0f);
            return t;
        }

        public float Age => _age;

        /// <summary>The construct for a rejoiner and the replay (`Kind.Thorns`): it only draws; the slippers it took already moved on the host.</summary>
        public Net.WorldEffectSnapshot.Field Capture() => new Net.WorldEffectSnapshot.Field
        {
            Type = Net.WorldEffectSnapshot.Kind.Thorns, Source = gameObject, Position = Origin, Forward = Vector3.forward,
            Duration = PaeteRules.ThornConstructSeconds, Remaining = Mathf.Max(0f, PaeteRules.ThornConstructSeconds - _age),
            Radius = PaeteRules.ThornRange, Owner = OwnerSlot,
        };

        /// <summary>The construct put back at <paramref name="age"/>, silent, reaching for nothing.</summary>
        public static PaeteThorns Restore(Vector3 origin, int ownerSlot, float age)
        {
            var go = new GameObject("PaeteThorns");
            go.transform.position = origin;
            var t = go.AddComponent<PaeteThorns>();
            t.OwnerSlot = ownerSlot; t.Origin = origin;
            t._age = Mathf.Max(0f, age);
            t._resolved = true;
            t._body = PaeteThornBody.Build(go.transform, t._caught);
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
    /// ⚠️⚠️ MAKILING'S EMBRACE'S SENTRY. Owner's table: *"He puts down a plant/tree sentry in a
    /// location and he PULLS all players here, they get stuck on it (switchhes to tpp view) and they
    /// have to hold a button ... for like 7 seconds while stuck here"*, *"THEY CAN STILL THROW AND USE
    /// SKILLS WHILE STUCK BTW theyre js rooted"*; *"WITHIN 9 meters"*; a tag frees them.
    ///
    /// Called up through the ground after the introduction: his roots race under the court from between his hands to the spot
    /// (`PaeteRootRidge`, v5; it was a thrown seed until 2026-09-26 and a lit vein after that), the court bulges and the tree
    /// CRAWLS out (`PaeteSentryBody`: claws first, three heaves, the crown, then the eyes); a beat after the court breaks its
    /// roots catch every other player within 9 m and drag them in (a carry per body, `CharacterMotor.ApplyResolvedCarry`), and
    /// they are Rooted for the rest of its life. Host resolves; every peer draws it.
    /// </summary>
    public sealed class PaeteSentry : MonoBehaviour
    {
        public int OwnerSlot { get; private set; } = -1;
        public Vector3 Centre { get; private set; }
        public float Age => _age;

        private float _age;
        // ⚠️⚠️ THE BODY IS SHOWN THIS FAR AHEAD OF THE RULES' CLOCK (v7, 2026-09-27). The owner, asked whether play should pick up where
        // the cutscene ends instead of the tree growing and catching everyone a second time: *"Yes, no repeat"*. The cutscene's staged
        // tree ends at age (5.0 - 2.5) x 1.45 = 3.625 on its own clock (`HeroIntroductionScene.Paete.cs`: it arrives at 2.5 and runs
        // at `PaeteTreePace` to the 5.0 end), awake since 1.75; the live one starts on the rules' clock at the catch
        // (`PaeteRules.SentryCatchSeconds`, 0.3), so its body is posed 3.625 - 0.3 = 3.325 ahead: standing, awake, the same tree.
        // The catch, the 10 s life and the 7 s break-out all keep the rules' clock; only the body and its growth cues use the lead.
        public const float BodyLead = 3.325f;
        private float _lead;
        private bool _caught;
        private readonly List<CharacterMotor> _held = new List<CharacterMotor>();
        private PaeteSentryBody _body;
        private const float Flight = 0.45f;

        public static PaeteSentry Spawn(Vector3 from, Vector3 at, int ownerSlot, float age = 0f, bool handBack = false)
        {
            var go = new GameObject("PaeteSentry");
            go.transform.position = at;
            var s = go.AddComponent<PaeteSentry>();
            s.OwnerSlot = ownerSlot; s.Centre = at;
            // ⚠️ A RESTORED AGE IS ALREADY PAST THE FLIGHT. `WorldEffectSnapshot` restores with the age since the roots arrived
            // (`Capture`'s `Remaining` counts from 0, not from -Flight), and subtracting the flight again put a rejoiner's tree
            // 0.45 s behind everybody else's for its whole life (TODO HERO-9, found 2026-09-26). A fresh cast (age 0) still flies.
            s._age = age > 0f ? age : handBack ? PaeteRules.SentryCatchSeconds - 0.02f : -Flight;
            // Every live cast comes up through the cutscene now, so a restored one (a rejoiner, age > 0) is posed with the lead too.
            s._lead = handBack || age > 0f ? BodyLead : 0f;
            s._body = PaeteSentryBody.Build(go.transform);
            s._body.LifeSeconds = PaeteRules.SentryLifeSeconds + s._lead;
            s._body.CatchLead = s._lead;
            // Before it has anyone to look at, the tree faces along the seed's flight (direction.md 5.2).
            s._body.SetFacing(at - from);
            // Under a roof (Ilalim ng Tulay's deck) it stands only as tall as fits; the open sky gets it all.
            float clearance = 30f;
            foreach (var hit in Physics.RaycastAll(at + Vector3.up * 0.5f, Vector3.up, 14f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == null || hit.collider.GetComponentInParent<CharacterMotor>() != null) continue;
                clearance = Mathf.Min(clearance, hit.distance + 0.5f);
            }
            s._body.FitUnder(clearance);
            // Its ground branches part round the can (the tree itself is already kept off it: `PaeteVine.SentryTarget`).
            var lata = GameServices.Round?.Lata;
            if (lata != null) s._body.AvoidPoint(lata.transform.position);
            // ⚠️ CALLED UP THROUGH THE GROUND, NOT THROWN (owner, 2026-09-26: *"i dont want him to be throwing an
            // orb I want him to be CALLING IT FROM THE GROUND"*, then *"i also dont like that paete just throws seeds in his
            // ult"*): his roots race under the court from between his hands to the spot on the same 0.45 s the seed flew,
            // so the warning and the catch timing are unchanged. v5 (direction.md 5.14): `PaeteRootRidge`, the court heaving
            // and splitting over them with the light inside the split; `PaeteRootVein`'s lit block at the front read as a seed.
            // The cutscene already showed the roots racing there and the tree crawling out; a hand-back sends no second race.
            if (age <= 0f && !handBack) PaeteRootRidge.Race(null, new Vector3(from.x, Slipper.GroundY(from), from.z), at, Flight, staged: false);
            // Who the vines reach for is drawn on every peer from the same rule the host uses.
            var round = GameServices.Round;
            if (round != null)
                foreach (var p in round.Players)
                    if (p != null && p.PlayerSlot != ownerSlot && InReach(at, p)) s._held.Add(p);
            s._body.SetTargets(s._held);
            return s;
        }

        /// <summary>
        /// The sentry for a rejoiner and the replay (`Kind.Sentry`). Who it holds is not in the field:
        /// Rooted rides `SyncUnit`, and a rejoiner's copy only draws (the host alone catches).
        /// </summary>
        public Net.WorldEffectSnapshot.Field Capture() => new Net.WorldEffectSnapshot.Field
        {
            Type = Net.WorldEffectSnapshot.Kind.Sentry, Source = gameObject, Position = Centre, Forward = Vector3.forward,
            Duration = PaeteRules.SentryLifeSeconds + 0.6f,
            Remaining = Mathf.Clamp(PaeteRules.SentryLifeSeconds + 0.6f - Mathf.Max(0f, _age), 0f, PaeteRules.SentryLifeSeconds + 0.6f),
            Radius = PaeteRules.SentryRadius, Owner = OwnerSlot,
        };

        public static bool InReach(Vector3 centre, CharacterMotor p)
        {
            Vector3 d = p.transform.position - centre; d.y = 0f;
            return d.magnitude <= PaeteRules.SentryRadius && !p.IsAloft;
        }

        private void Update()
        {
            float before = _age;
            _age += Time.deltaTime;
            // The growth cues run on the BODY's clock (so a hand-back, already grown, plays none of them); the rules on `_age`.
            float grownBefore = before + _lead, grown = _age + _lead;
            // ⚠️ LOCAL ON EVERY PEER, NOT `NetCue`: each peer runs this same clock from the accepted
            // cast, so each plays each stage once (`audit_cue_relay.py`).
            // ⚠️⚠️ v5, IT CRAWLS OUT (owner, 2026-09-26 night: *"i also dotn want the tree to jsut spawn in or teleport in"*, *"and
            // then the tree slowly show up"*; direction.md 5.14). The court BULGES as the roots arrive (age 0), the claws break out,
            // and the trunk hauls itself up in three heaves (`PaeteSentryBody.Heaves`), each its own groan and crack and a shake that
            // grows with the tree; its eyes open last (`WakeAt`). The catch below keeps its old clock: nothing here moves a rule.
            if (grownBefore < 0f && grown >= 0f)
            {
                GameServices.Audio?.PlayAt("sfx_paete_sentry_burst", Centre);
                // ⚠️ v9: sized to the 6.6 m tree and its 2.1 m roots (was 2.2 and hauls to 3.4 m for the 9 m one): the court breaks
                // where it comes up and stops short of the can's clearance (2.4 m).
                Visual.PaeteGroundBreak.Spawn(Centre, 1.6f);
                ShakeNearby(0.22f, 0.5f);
            }
            for (int k = 0; k < PaeteSentryBody.Heaves.Length; k++)
            {
                float at = PaeteSentryBody.Heaves[k].x;
                if (grownBefore < at && grown >= at)
                {
                    // ⚠️ SIZED TO THE 9 M TREE (owner: *"REALLY big and imposing and really feel like an ult"*): each haul breaks
                    // the road wider and shakes every nearby camera harder, the last hardest (the style of `HeroHazards`' blasts:
                    // the shake scales with the thing, it is never one flat number).
                    GameServices.Audio?.PlayAtVaried("sfx_paete_sentry_heave", Centre, 1.06f - 0.06f * k, 1.08f - 0.06f * k, 1f);
                    Visual.PaeteGroundBreak.Spawn(Centre, 1.8f + 0.2f * k);
                    ShakeNearby(0.24f + 0.11f * k, 0.55f + 0.1f * k);
                }
            }
            // The tree waking: the light opens in its hollows (`PaeteSentryBody`'s WAKE beat, last).
            if (grownBefore < _body.WakeAt && grown >= _body.WakeAt)
                GameServices.Audio?.PlayAt("sfx_paete_sentry_wake", Centre);
            if (before < PaeteRules.SentryCatchSeconds && _age >= PaeteRules.SentryCatchSeconds && _held.Count > 0)
                GameServices.Audio?.PlayAt("sfx_paete_sentry_catch", Centre);
            if (before < PaeteRules.SentryLifeSeconds && _age >= PaeteRules.SentryLifeSeconds)
                GameServices.Audio?.PlayAt("sfx_paete_sentry_wilt", Centre);
            _body.Pose(grown, Centre);
            if (_age >= 0f)
                foreach (var p in _held)
                    if (p != null && p.IsRooted) PaeteRootCoil.Attach(p, Centre);
            if (_age >= PaeteRules.SentryLifeSeconds + 0.6f) Destroy(gameObject);
        }

        /// <summary>Every nearby camera takes the ground moving, harder the closer it is (0.35 to 0.8 of <paramref name="scale"/> by distance).</summary>
        private void ShakeNearby(float scale, float seconds)
        {
            var main = UnityEngine.Camera.main;
            var rig = main != null ? main.GetComponent<CameraSystem.CameraRig>() : null;
            if (rig == null) return;
            float near = Mathf.Clamp01(1f - Vector3.Distance(main.transform.position, Centre) / 18f);
            rig.Shake(Mathf.Lerp(0.45f, 1f, near) * scale * 2f, seconds);
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
