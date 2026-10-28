using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ZACK'S LIVE WIRE (owner refinement, 2026-10-26): a sparking wire strung from his feet to the aimed point, 1.5 to 6 m,
    /// that STAYS ON THE COURT. An attacker who crosses a lit wire is Zapped and the wire goes dark for
    /// `ZackRules.LiveWireDarkSeconds`, then rearms; nobody can be stun-locked on it because the victim has that whole dark
    /// window to step away. Strung while Overclocked it is overcharged: while he stays Overclocked it never goes dark, but
    /// the attacker it just Zapped has `LiveWireOverchargeGrace` against it. It leaves only when the round ends, when he
    /// strings a new one, or when he looks at it and holds the skill to reel it in (`MatchRpc.ZackReel`).
    ///
    /// ⚠️ IT HAS NO COLLIDER. Bodies, slippers and the can pass through it; only the host's crossing test
    /// (`ZackRules.Crossed`, feet between two physics steps, under `LiveWireReachAbove`) decides anything, and only the host
    /// Zaps (`CharacterMotor.ApplyZapped`). It is a host-owned dynamic field on the existing reliable message, kind 21.
    ///
    /// Field layout: `Position` post A, `Path` [A, B], `Forward` flat A to B, `Radius` length, `Duration`
    /// `LiveWireLifeBound` (a validation bound, never reached inside a round), `SecondScale` 1 overcharged, `FirstScale` the
    /// age it last went dark (0 lit since it was strung), `Split` reeled in.
    /// </summary>
    public sealed class ZackLiveWire : MonoBehaviour
    {
        public static readonly List<ZackLiveWire> Active = new List<ZackLiveWire>();
        private static int _nextId;

        /// <summary>A reeled wire whips back into his hand over this long, then goes.</summary>
        public const float ReelSeconds = 0.35f;

        private WorldEffectSnapshot.Field _state;
        private ZackLiveWireLook _look;
        private float _age, _reeledAt = -1.0f;
        private int _round;
        private long _epoch;
        private readonly Dictionary<CharacterMotor, Vector3> _previousFeet = new Dictionary<CharacterMotor, Vector3>();
        private readonly Dictionary<int, float> _graceUntil = new Dictionary<int, float>();
        private readonly List<HazardVolume> _hazards = new List<HazardVolume>();

        public int OwnerSlot => _state.Owner;
        public int EventId => _state.EventId;
        public bool Reeled => _state.Split;
        public bool StrungOvercharged => _state.SecondScale > 0.5f;
        public Vector3 PostA => _state.Path[0];
        public Vector3 PostB => _state.Path[1];
        public float Age => _age;

        /// <summary>Dark (harmless, unlit) after a normal trip; an overcharged wire with a live Overclock never goes dark.</summary>
        public bool Dark => !OverchargedNow && ZackRules.LiveWireDark(_state.FirstScale, _age);

        /// <summary>0 to 1 back to lit while dark (1 when lit), for the rearm crawl.</summary>
        public float Rearm => !Dark ? 1.0f : Mathf.Clamp01((_age - _state.FirstScale) / ZackRules.LiveWireDarkSeconds);

        /// <summary>Overcharged right now: strung while Overclocked and its owner is still Overclocked (every peer knows both).</summary>
        public bool OverchargedNow => StrungOvercharged && OwnerOverclocked();

        public bool IsLive => isActiveAndEnabled && !_state.Split;
        public bool IsArmed => IsLive && !Dark;

        private bool OwnerOverclocked()
        {
            var owner = GameServices.Round?.BodyAt(_state.Owner);
            return owner != null && owner.AbilitySystem?.Kit is ZackHeroKit zack && zack.IsOverclocked;
        }

        public WorldEffectSnapshot.Field Capture()
        {
            var f = _state; f.Source = gameObject; f.Remaining = Mathf.Max(0.0f, _state.Duration - _age);
            f.Path = (Vector3[])_state.Path.Clone(); return f;
        }

        /// <summary>His live wire, if he has one strung (not reeling in).</summary>
        public static ZackLiveWire OwnedBy(int slot)
        {
            foreach (var wire in Active)
                if (wire != null && wire.IsLive && wire._state.Owner == slot) return wire;
            return null;
        }

        // ------------------------------------------------------------------ placement (owner and host both ask)

        /// <summary>
        /// A wire from <paramref name="a"/> (his feet) to <paramref name="b"/> (the aim): length within 1.5 to 6 m, both ends
        /// inside the playable rectangle, both floors within `LiveWireFloorTolerance` of his, and no wall between them at
        /// waist height.
        /// </summary>
        public static bool CanPlace(CharacterMotor motor, Vector3 a, Vector3 b)
        {
            if (motor == null || !float.IsFinite(a.sqrMagnitude) || !float.IsFinite(b.sqrMagnitude)) return false;
            var flat = b - a; flat.y = 0.0f;
            float length = flat.magnitude;
            if (!ZackRules.LiveWireLengthValid(length)) return false;
            if (!Inside(a) || !Inside(b)) return false;
            float floor = Slipper.FindGroundY(motor.transform.position, 0.5f);
            float fa = Slipper.FindGroundY(a, 0.5f), fb = Slipper.FindGroundY(b, 0.5f);
            if (Mathf.Abs(fa - floor) > ZackRules.LiveWireFloorTolerance || Mathf.Abs(fb - floor) > ZackRules.LiveWireFloorTolerance)
                return false;
            var from = new Vector3(a.x, fa + 0.9f, a.z);
            return RafiWaterField.ClearDistance(from, flat / length, length) >= length - 0.02f;
        }

        private static bool Inside(Vector3 p)
            => p.x >= AIController.PlayableMinX && p.x <= AIController.PlayableMaxX
               && p.z >= AIController.PlayableMinZ && p.z <= AIController.PlayableMaxZ;

        /// <summary>⚠️ HOST ONLY. Strings the wire, retiring his old one, and broadcasts it with a fresh event id.</summary>
        public static ZackLiveWire Cast(CharacterMotor motor, Vector3 a, Vector3 b, bool overcharged)
        {
            if (!NetAuthority.ShouldResolve() || !CanPlace(motor, a, b)) return null;
            a.y = Slipper.FindGroundY(a, 0.5f);
            b.y = Slipper.FindGroundY(b, 0.5f);
            var flat = b - a; flat.y = 0.0f;
            float length = flat.magnitude;
            var state = new WorldEffectSnapshot.Field
            {
                Type = WorldEffectSnapshot.Kind.LiveWire, EventId = ++_nextId,
                Position = a, Forward = flat / length, Radius = length,
                Duration = ZackRules.LiveWireLifeBound, Remaining = ZackRules.LiveWireLifeBound,
                Owner = motor.PlayerSlot, SecondScale = overcharged ? 1.0f : 0.0f,
                Path = new[] { a, b }
            };
            var wire = Restore(state, 0.0f);
            MatchRpc.Instance?.BroadcastDynamicField(state);
            NetCue.Play("sfx_zack_livewire_taut", (a + b) * 0.5f);
            return wire;
        }

        /// <summary>
        /// ⚠️ HOST ONLY. Whether this seat may reel this wire in now: it is his, still strung, he can act and stands within the
        /// reel reach (plus the host's leeway for a pose that crossed the wire late).
        /// </summary>
        public static ZackLiveWire HostReelable(CharacterMotor motor, int eventId)
        {
            if (motor == null || !(motor.AbilitySystem?.Kit is ZackHeroKit) || !motor.IsDefender || !motor.CanAct()
                || GameServices.Round?.RoundActive != true) return null;
            var wire = OwnedBy(motor.PlayerSlot);
            if (wire == null || wire._state.EventId != eventId) return null;
            return wire.FlatDistanceTo(motor.transform.position) <= ZackRules.LiveWireReelRange + ZackRules.LiveWireReelLeeway
                ? wire : null;
        }

        /// <summary>Flat metres from a point to the wire's segment.</summary>
        public float FlatDistanceTo(Vector3 p)
        {
            Vector3 a = PostA, b = PostB; a.y = b.y = p.y = 0.0f;
            var ab = b - a; float u = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0.0f;
            return Vector3.Distance(p, a + ab * u);
        }

        /// <summary>⚠️ HOST ONLY. The reel completed: every peer whips the wire back into his hand and retires it.</summary>
        public void HostReelIn()
        {
            if (!NetAuthority.ShouldResolve() || _state.Split) return;
            _state.Split = true;
            BeginReel();
            MatchRpc.Instance?.BroadcastDynamicField(Capture());
            NetCue.Play("sfx_zack_livewire_taut", (PostA + PostB) * 0.5f);
        }

        // ------------------------------------------------------------------ validation and restore (every peer)

        public static bool Valid(WorldEffectSnapshot.Field f)
        {
            if (f.Type != WorldEffectSnapshot.Kind.LiveWire || f.EventId <= 0 || f.Owner < 0 || f.Owner >= Balance.PlayerCount
                || f.Path == null || f.Path.Length != 2
                || !float.IsFinite(f.Path[0].sqrMagnitude) || !float.IsFinite(f.Path[1].sqrMagnitude)
                || Mathf.Abs(f.Duration - ZackRules.LiveWireLifeBound) > 0.001f
                || (f.SecondScale != 0.0f && f.SecondScale != 1.0f)
                || !float.IsFinite(f.FirstScale) || f.FirstScale < 0.0f || f.FirstScale > f.Duration
                || !ZackRules.LiveWireLengthValid(f.Radius)
                || Mathf.Abs(f.Forward.y) > 0.01f || f.Forward.sqrMagnitude < 0.99f || f.Forward.sqrMagnitude > 1.01f
                || (f.Path[0] - f.Position).sqrMagnitude > 1e-4f) return false;
            var flat = f.Path[1] - f.Path[0];
            if (Mathf.Abs(flat.y) > ZackRules.LiveWireFloorTolerance * 2.0f + 0.01f) return false;
            flat.y = 0.0f;
            return Mathf.Abs(flat.magnitude - f.Radius) < 0.01f && Vector3.Dot(flat.normalized, f.Forward) > 0.999f;
        }

        /// <summary>
        /// A saved clip from protocol 182 or 183 (clip schema 16) recorded the old 10 s wire that snapped on its first Zap. Read
        /// it in that meaning: its age is kept against the new lifetime bound, and a snapped wire is drawn leaving (as a reel)
        /// rather than as a dark wire that the old game never had. Replay only; never a live snapshot.
        /// </summary>
        public static WorldEffectSnapshot.Field FromRecordedV16(WorldEffectSnapshot.Field old)
        {
            const float oldLife = 10.0f;
            float age = float.IsFinite(old.Duration) && float.IsFinite(old.Remaining) ? Mathf.Clamp(old.Duration - old.Remaining, 0.0f, oldLife) : 0.0f;
            old.Duration = ZackRules.LiveWireLifeBound;
            old.Remaining = ZackRules.LiveWireLifeBound - age;
            old.FirstScale = 0.0f;
            return old;
        }

        public static ZackLiveWire Restore(WorldEffectSnapshot.Field state, float elapsed)
        {
            if (!Valid(state)) return null;
            float remaining = Mathf.Max(0.0f, state.Remaining - elapsed);
            foreach (var live in Active)
                if (live != null && live.isActiveAndEnabled && live._state.EventId == state.EventId)
                {
                    bool reel = state.Split && !live._state.Split;
                    // An older snapshot never un-darkens a wire this peer already saw trip someone later.
                    float dark = Mathf.Max(live._state.FirstScale, state.FirstScale);
                    bool newlyDark = dark > live._state.FirstScale;
                    live._state = state;
                    live._state.FirstScale = dark;
                    live._state.Split = live._state.Split || reel;
                    live._age = Mathf.Max(live._age, state.Duration - remaining);
                    if (newlyDark) live._look?.Discharge();
                    if (reel) live.BeginReel();
                    return live;
                }
            if (remaining <= 0.0f || state.Split) return null;
            // A new cast retires his old wire on every peer, so one seat never has two.
            foreach (var old in Active.ToArray())
                if (old != null && old._state.Owner == state.Owner) old.Retire();
            var go = new GameObject("ZackLiveWire-P" + (state.Owner + 1));
            var wire = go.AddComponent<ZackLiveWire>();
            wire._state = state;
            wire._age = state.Duration - remaining;
            wire._round = GameServices.Match?.RoundNumber ?? 0;
            wire._epoch = MatchRpc.Instance?.PresentationMatchId ?? 0;
            wire._look = ZackLiveWireLook.Build(go.transform, state);
            wire.StepLook();
            wire.RegisterHazards();
            Active.Add(wire);
            return wire;
        }

        /// <summary>Discs along the wire so attacker bots steer round it (`HazardMap`); they go with the wire.</summary>
        private void RegisterHazards()
        {
            int count = Mathf.Max(2, Mathf.CeilToInt(_state.Radius / 0.8f) + 1);
            for (int i = 0; i < count; i++)
            {
                var disc = new GameObject("WireHazard" + i);
                disc.transform.SetParent(transform, false);
                disc.transform.position = Vector3.Lerp(PostA, PostB, i / (float)(count - 1));
                _hazards.Add(HazardVolume.Attach(disc, 0.5f, _state.Owner));
            }
        }

        // ------------------------------------------------------------------ life

        private void Update()
        {
            if ((GameServices.Round != null && !GameServices.Round.RoundActive)
                || (GameServices.Match?.RoundNumber ?? 0) != _round
                || (MatchRpc.Instance?.PresentationMatchId ?? 0) != _epoch)
            { Retire(); return; }
            _age += Time.deltaTime;
            StepLook();
            if (_reeledAt >= 0.0f && _age - _reeledAt > ReelSeconds) { Retire(); return; }
            if (_age >= _state.Duration) Retire();
        }

        private void StepLook()
        {
            if (_look == null) return;
            var owner = GameServices.Round?.BodyAt(_state.Owner);
            var hand = owner != null ? owner.transform.position + Vector3.up * 0.9f : (PostA + PostB) * 0.5f;
            bool aimed = owner != null && owner.AbilitySystem?.Kit is ZackHeroKit zack && zack.ReelAimedEvent == _state.EventId;
            _look.Present(Dark, Rearm, OverchargedNow, hand, aimed);
            _look.StepTo(_age);
        }

        /// <summary>
        /// ⚠️ HOST ONLY: an attacker's feet going from one side of the segment to the other between two physics steps, within
        /// its length and under `LiveWireReachAbove`. Taya and tagged bodies are ignored; a dark wire does nothing; an
        /// overcharged one ignores the attacker it Zapped for the grace.
        /// </summary>
        private void FixedUpdate()
        {
            if (!NetAuthority.ShouldResolve() || _state.Split || GameServices.Round == null || !GameServices.Round.RoundActive)
                return;
            var a = PostA; var b = PostB;
            bool dark = Dark, overcharged = OverchargedNow;
            foreach (var player in GameServices.Round.Players)
            {
                if (player == null) continue;
                var feet = player.transform.position;
                bool had = _previousFeet.TryGetValue(player, out var before);
                _previousFeet[player] = feet;
                if (!had || dark || PresentationClock.BlocksInput || player.IsDefender || player.IsTagged) continue;
                if (_graceUntil.TryGetValue(player.PlayerSlot, out float until) && _age < until) continue;
                if (!ZackRules.Crossed(a.x, a.z, b.x, b.z, before.x, before.z, feet.x, feet.z, out float along)) continue;
                var edge = b - a;
                float sideBefore = edge.x * (before.z - a.z) - edge.z * (before.x - a.x);
                float sideNow = edge.x * (feet.z - a.z) - edge.z * (feet.x - a.x);
                float crossedAt = Mathf.Clamp01(sideBefore / (sideBefore - sideNow));
                float crossingHeight = Mathf.Lerp(before.y, feet.y, crossedAt) - Mathf.Lerp(a.y, b.y, along);
                if (crossingHeight > ZackRules.LiveWireReachAbove) continue;
                player.ApplyZapped();
                var at = Vector3.Lerp(a, b, along);
                at.y += ZackRules.LiveWireHeightAt(along);
                NetCue.Play("sfx_zack_livewire_zap", at);
                Visual.MatchFlair.Announce(Visual.MatchFlair.Kind.HeroHit, _state.Owner, player.PlayerSlot, at, 1.0f);
                if (overcharged)
                {
                    _graceUntil[player.PlayerSlot] = _age + ZackRules.LiveWireOverchargeGrace;
                    continue;
                }
                // A lit normal wire trips one attacker and goes dark; it stays where it is and rearms.
                _state.FirstScale = Mathf.Max(_age, 0.001f);
                _look?.Discharge();
                MatchRpc.Instance?.BroadcastDynamicField(Capture());
                return;
            }
        }

        private void BeginReel()
        {
            if (_reeledAt >= 0.0f) return;
            _reeledAt = _age;
            foreach (var hazard in _hazards) if (hazard != null) Destroy(hazard.gameObject);
            _hazards.Clear();
            _look?.Reel();
        }

        private void Retire()
        {
            Active.Remove(this);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        /// <summary>Every wire one seat strung (-1 for all): round reset, kit reset.</summary>
        public static void RetireOwnedBy(int slot)
        {
            foreach (var live in Active.ToArray())
                if (live != null && (slot < 0 || live._state.Owner == slot)) live.Retire();
        }

        private void OnEnable() { if (_state.EventId > 0 && !Active.Contains(this)) Active.Add(this); }
        private void OnDisable() { Active.Remove(this); }
    }
}
