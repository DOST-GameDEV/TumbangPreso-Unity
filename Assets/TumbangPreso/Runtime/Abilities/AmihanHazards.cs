using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ WHIRLWIND'S GALE: THE GAMEPLAY HALF. `AmihanGaleFront` draws it; this decides who it hit.
    ///
    /// Owner's table: *"Create an arc-shaped gale that inflicts Whirled on players it hits as it
    /// swiftly moves forward. The gale lasts 2.5 s."* It exists on every peer (the cast is
    /// replicated, and a rejoiner gets it from `WorldEffectSnapshot`), it moves on its own clock
    /// from its accepted origin, and ONLY THE HOST resolves contact, by distance to the arc
    /// (`CLAUDE.md` § 4: contact by distance, never a trigger volume). The status the host applies
    /// reaches everybody through `SyncUnit`, and the body tell and the contact burst are drawn on
    /// every peer from that, so the picture and the rule are one event.
    /// </summary>
    public sealed class AmihanGale : MonoBehaviour
    {
        public int OwnerSlot { get; private set; } = -1;
        public Vector3 Origin { get; private set; }
        public Vector3 Forward { get; private set; }
        public float Duration => AmihanRules.WhirlwindSeconds;
        public float Remaining => Mathf.Max(0.0f, Duration - _age);

        private float _age;
        private readonly HashSet<int> _hit = new HashSet<int>();
        private AmihanGaleFront _front;

        public static AmihanGale Spawn(Vector3 origin, Vector3 forward, int ownerSlot, float age = 0.0f)
        {
            forward.y = 0.0f;
            forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            var go = new GameObject("AmihanGale");
            var gale = go.AddComponent<AmihanGale>();
            gale.OwnerSlot = ownerSlot; gale.Origin = origin; gale.Forward = forward;
            gale._front = AmihanGaleFront.Build(go.transform, origin, forward, AmihanRules.WhirlwindSeconds,
                AmihanRules.WhirlwindSpeed, AmihanRules.WhirlwindStart, AmihanRules.WhirlwindWidth);
            gale._age = Mathf.Clamp(age, 0.0f, AmihanRules.WhirlwindSeconds);
            gale._front.StepTo(gale._age);
            return gale;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_front != null) _front.StepTo(_age);
            if (_age >= Duration) Destroy(gameObject);
        }

        /// <summary>
        /// Where the arc's centre line is, as a forward offset from its middle, at a lateral offset
        /// <paramref name="x"/>: a parabola through the chord ends with <see cref="AmihanRules.WhirlwindBow"/>
        /// at the middle. The same bow `AmihanGaleFront` draws, to the few centimetres a parabola
        /// and a circular arc differ by at this chord.
        /// </summary>
        public static float ArcOffset(float x)
        {
            float half = AmihanRules.WhirlwindWidth * 0.5f;
            float k = Mathf.Clamp01(Mathf.Abs(x) / half);
            return AmihanRules.WhirlwindBow * (1.0f - k * k) - AmihanRules.WhirlwindBow;
        }

        private void FixedUpdate()
        {
            if (!NetAuthority.ShouldResolve()) return;
            var round = GameServices.Round;
            if (round == null || !round.RoundActive) return;

            // ⚠️ THE SWEPT BAND, NOT THE FRONT'S POSITION THIS STEP. At 5.5 m/s a physics step moves
            // the front 11 cm, which is inside the 0.9 m depth, but a frame hitch can move it
            // further; testing the band between last step and this one cannot tunnel.
            float dt = Time.fixedDeltaTime;
            float now = AmihanRules.WhirlwindStart + AmihanRules.WhirlwindSpeed * Mathf.Min(_age, Duration);
            float before = now - AmihanRules.WhirlwindSpeed * dt;
            Vector3 right = Vector3.Cross(Vector3.up, Forward);
            float half = AmihanRules.WhirlwindWidth * 0.5f;
            float depth = AmihanRules.WhirlwindDepth * 0.5f;

            foreach (var p in round.Players)
            {
                if (p == null || p.PlayerSlot == OwnerSlot || _hit.Contains(p.PlayerSlot)) continue;
                Vector3 d = p.transform.position - Origin; d.y = 0.0f;
                float x = Vector3.Dot(d, right), z = Vector3.Dot(d, Forward);
                if (Mathf.Abs(x) > half + 0.3f) continue;
                float arc = ArcOffset(x);
                if (z < before + arc - depth || z > now + arc + depth) continue;
                // A body 2.8 m up is over the gale, not in it.
                if (p.IsAloft) continue;

                _hit.Add(p.PlayerSlot);
                p.ApplyWhirled();
            }
        }

        /// <summary>This gale as a world-effect field, for a rejoiner and for the replay.</summary>
        public Net.WorldEffectSnapshot.Field Capture() => new Net.WorldEffectSnapshot.Field
        {
            Type = Net.WorldEffectSnapshot.Kind.Gale, Source = gameObject,
            Position = Origin, Forward = Forward, Duration = Duration, Remaining = Remaining,
            Radius = AmihanRules.WhirlwindWidth * 0.5f, Owner = OwnerSlot,
        };
    }

    /// <summary>
    /// ⚠️⚠️ STORM SURGE: THE GAMEPLAY HALF. `AmihanStormFan` draws it; this pushes.
    ///
    /// Owner's table: *"After a 2.5 s delay, unleash a map-wide, fan-shaped wind in the target
    /// direction that greatly pushes back all players and slippers caught inside, almost to the
    /// edge of the arena."* And on the distance: *"very far, the rsn for this is we want them to
    /// fall off the map or pushed to the edge"* (2026-09-25).
    ///
    /// It is spawned on every peer at the START of the 2.5 s (the ultimate's wind-up), so the
    /// telegraph every player reads is the real fan, and it is released by the ultimate's
    /// activation at the END of it, on the same clock. Only the host pushes; a body the host does
    /// not simulate is carried by the `Carry` message (`CharacterMotor.ApplyResolvedCarry`), and a
    /// slipper's position already streams to everyone.
    /// </summary>
    public sealed class AmihanStorm : MonoBehaviour
    {
        public int OwnerSlot { get; private set; } = -1;
        public Vector3 Origin { get; private set; }
        public Vector3 Forward { get; private set; }
        public float Age => _age;
        public bool Released => _released;

        private float _age;
        private bool _released;
        private HeroAbility _source;
        private AmihanStormFan _fan;
        private readonly Dictionary<Slipper, float> _blown = new Dictionary<Slipper, float>();

        public static AmihanStorm Spawn(Vector3 origin, Vector3 forward, int ownerSlot, HeroAbility source, float age = 0.0f)
        {
            forward.y = 0.0f;
            forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            var go = new GameObject("AmihanStorm");
            var storm = go.AddComponent<AmihanStorm>();
            storm.OwnerSlot = ownerSlot; storm.Origin = origin; storm.Forward = forward; storm._source = source;
            storm._fan = AmihanStormFan.Build(go.transform, origin, forward, AmihanRules.StormSurgeGatherSeconds);
            storm._age = Mathf.Max(0.0f, age);
            storm._fan.StepTo(storm._age);
            GameServices.Audio?.PlayAt("sfx_amihan_storm_gather", origin);
            return storm;
        }

        /// <summary>
        /// True if <paramref name="point"/> is inside the fan: within the half angle either side of
        /// the direction, and within the map-wide range. The same test for bodies and slippers.
        /// </summary>
        public static bool InsideFan(Vector3 origin, Vector3 forward, Vector3 point)
        {
            Vector3 d = point - origin; d.y = 0.0f;
            if (d.sqrMagnitude < 0.04f) return false; // her own spot
            if (d.magnitude > AmihanRules.StormSurgeRange) return false;
            return Vector3.Angle(forward, d) <= AmihanRules.StormSurgeHalfAngle;
        }

        /// <summary>
        /// The direction a body at <paramref name="point"/> is blown: outward from her along the fan,
        /// leaning on the fan's own direction so nobody is thrown sideways out of it.
        /// </summary>
        public static Vector3 BlowDirection(Vector3 origin, Vector3 forward, Vector3 point)
        {
            Vector3 d = point - origin; d.y = 0.0f;
            Vector3 radial = d.sqrMagnitude > 0.01f ? d.normalized : forward;
            Vector3 dir = (radial * 0.6f + forward * 0.4f);
            return dir.sqrMagnitude > 0.001f ? dir.normalized : forward;
        }

        /// <summary>The wind leaves: called by the ultimate's activation at the end of the delay.</summary>
        public void Release()
        {
            if (_released) return;
            _released = true;
            _age = Mathf.Max(_age, AmihanRules.StormSurgeGatherSeconds);
            GameServices.Audio?.PlayAt("sfx_amihan_storm_release", Origin);
            PunchNearbyCamera();
            if (!NetAuthority.ShouldResolve()) return;
            var round = GameServices.Round;
            if (round == null) return;

            float hold = AmihanRules.StormSurgeHoldSeconds;
            foreach (var p in round.Players)
            {
                if (p == null || p.PlayerSlot == OwnerSlot) continue;
                if (!InsideFan(Origin, Forward, p.transform.position)) continue;
                Vector3 dir = BlowDirection(Origin, Forward, p.transform.position);
                // ⚠️ THE CAN IS NOT A SLIPPER AND IS NOT TOUCHED. The table names players and
                // slippers; moving the objective would be a third thing nobody asked for.
                p.ApplyResolvedCarry(dir * AmihanRules.StormSurgeSpeed + Vector3.up * AmihanRules.StormSurgeLift, hold);
            }
            foreach (var shoe in FindObjectsByType<Slipper>())
                if (shoe != null && shoe.State == SlipperState.Loose && InsideFan(Origin, Forward, shoe.transform.position))
                    _blown[shoe] = AmihanRules.StormSurgeDistance;
        }

        private void PunchNearbyCamera()
        {
            var camera = Camera.main;
            if (camera == null) return;
            var rig = camera.GetComponent<CameraSystem.CameraRig>();
            float distance = Vector3.Distance(camera.transform.position, Origin);
            float falloff = Mathf.InverseLerp(30.0f, 4.0f, distance);
            bool inFan = InsideFan(Origin, Forward, camera.transform.position);
            if (inFan) falloff = Mathf.Max(falloff, 0.85f);
            if (falloff <= 0.01f) return;
            rig?.ImpactPunch(Forward, 1.3f * falloff);
            rig?.Shake(0.28f * falloff, 1.0f);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_fan != null) _fan.StepTo(_age);
            // ⚠️ A GATHER WHOSE ULTIMATE WAS RESET (a round ending mid-delay) GOES WITH IT, so no
            // fan is left on the road telegraphing a wind that will never come.
            if (!_released && _source != null && !_source.IsWindingUp && _age > 0.1f) { Destroy(gameObject); return; }
            if (_fan != null && _age >= _fan.LifeSeconds) Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            if (!_released || _blown.Count == 0 || !NetAuthority.ShouldResolve()) return;
            float dt = Time.fixedDeltaTime;
            var done = new List<Slipper>();
            foreach (var pair in _blown)
            {
                var shoe = pair.Key;
                float left = pair.Value;
                if (shoe == null || shoe.State != SlipperState.Loose || left <= 0.01f) { done.Add(shoe); continue; }
                // Decelerating against `Friction` from the speed that slides the full distance, so a
                // slipper travels exactly `StormSurgeDistance` unless a wall stops it first.
                float speed = Mathf.Sqrt(2.0f * Balance.Friction * left);
                float step = Mathf.Min(left, speed * dt);
                Vector3 dir = BlowDirection(Origin, Forward, shoe.transform.position);
                Vector3 wanted = shoe.transform.position + dir * step;
                wanted.x = Mathf.Clamp(wanted.x, -AIController.PlayableHalfX, AIController.PlayableHalfX);
                wanted.z = Mathf.Clamp(wanted.z, -AIController.PlayableHalfZ, AIController.PlayableHalfZ);
                Vector3 delta = wanted - shoe.transform.position;
                float moved = shoe.HostSweepLoose(delta);
                float remaining = moved < delta.magnitude - 0.01f ? 0.0f : left - moved;
                _blownNext[shoe] = remaining;
            }
            foreach (var pair in _blownNext) _blown[pair.Key] = pair.Value;
            _blownNext.Clear();
            foreach (var shoe in done) _blown.Remove(shoe);
        }

        private readonly Dictionary<Slipper, float> _blownNext = new Dictionary<Slipper, float>();
    }
}
