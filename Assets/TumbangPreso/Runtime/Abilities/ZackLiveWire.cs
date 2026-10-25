using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ZACK'S LIVE WIRE (2026-10-25 rework, plan section 2): a sparking wire strung from his feet to the aimed point, 1.5 to
    /// 6 m. The first attacker to cross it is Zapped and it snaps; strung while Overclocked it is overcharged and Zaps every
    /// attacker once for its whole 10 s.
    ///
    /// ⚠️ IT HAS NO COLLIDER. Bodies, slippers and the can pass through it; only the host's crossing test
    /// (`ZackRules.Crossed`, feet between two physics steps, under `LiveWireReachAbove`) decides anything, and only the host
    /// Zaps (`CharacterMotor.ApplyZapped`). It is a host-owned dynamic field on the existing reliable message, kind 21.
    ///
    /// Field layout: `Position` post A, `Path` [A, B], `Forward` flat A to B, `Radius` length, `Duration` 10, `SecondScale`
    /// 1 overcharged, `Split` snapped, `FirstScale` the age it snapped at (0 while whole).
    /// </summary>
    public sealed class ZackLiveWire : MonoBehaviour
    {
        public static readonly List<ZackLiveWire> Active = new List<ZackLiveWire>();
        private static int _nextId;

        /// <summary>A snapped wire stays this long for its fall, then goes.</summary>
        public const float SnapSeconds = 0.6f;

        private WorldEffectSnapshot.Field _state;
        private ZackLiveWireLook _look;
        private float _age;
        private int _round;
        private long _epoch;
        private readonly Dictionary<CharacterMotor, Vector3> _previousFeet = new Dictionary<CharacterMotor, Vector3>();
        private readonly HashSet<int> _zapped = new HashSet<int>();
        private readonly List<HazardVolume> _hazards = new List<HazardVolume>();

        public int OwnerSlot => _state.Owner;
        public bool Snapped => _state.Split;
        public bool Overcharged => _state.SecondScale > 0.5f;
        public Vector3 PostA => _state.Path[0];
        public Vector3 PostB => _state.Path[1];
        public float Remaining => Mathf.Max(0.0f, _state.Duration - _age);
        public bool IsLive => isActiveAndEnabled && !_state.Split && Remaining > 0.02f;

        public WorldEffectSnapshot.Field Capture()
        { var f = _state; f.Source = gameObject; f.Remaining = Remaining; f.Path = (Vector3[])_state.Path.Clone(); return f; }

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
                Duration = ZackRules.LiveWireSeconds, Remaining = ZackRules.LiveWireSeconds,
                Owner = motor.PlayerSlot, SecondScale = overcharged ? 1.0f : 0.0f,
                Path = new[] { a, b }
            };
            var wire = Restore(state, 0.0f);
            MatchRpc.Instance?.BroadcastDynamicField(state);
            NetCue.Play("sfx_zack_livewire_taut", (a + b) * 0.5f);
            return wire;
        }

        // ------------------------------------------------------------------ validation and restore (every peer)

        public static bool Valid(WorldEffectSnapshot.Field f)
        {
            if (f.Type != WorldEffectSnapshot.Kind.LiveWire || f.EventId <= 0 || f.Owner < 0 || f.Owner >= Balance.PlayerCount
                || f.Path == null || f.Path.Length != 2
                || !float.IsFinite(f.Path[0].sqrMagnitude) || !float.IsFinite(f.Path[1].sqrMagnitude)
                || Mathf.Abs(f.Duration - ZackRules.LiveWireSeconds) > 0.001f
                || (f.SecondScale != 0.0f && f.SecondScale != 1.0f)
                || f.FirstScale < 0.0f || f.FirstScale > f.Duration || (!f.Split && f.FirstScale != 0.0f)
                || !ZackRules.LiveWireLengthValid(f.Radius)
                || Mathf.Abs(f.Forward.y) > 0.01f || f.Forward.sqrMagnitude < 0.99f || f.Forward.sqrMagnitude > 1.01f
                || (f.Path[0] - f.Position).sqrMagnitude > 1e-4f) return false;
            var flat = f.Path[1] - f.Path[0];
            if (Mathf.Abs(flat.y) > ZackRules.LiveWireFloorTolerance * 2.0f + 0.01f) return false;
            flat.y = 0.0f;
            return Mathf.Abs(flat.magnitude - f.Radius) < 0.01f && Vector3.Dot(flat.normalized, f.Forward) > 0.999f;
        }

        public static ZackLiveWire Restore(WorldEffectSnapshot.Field state, float elapsed)
        {
            if (!Valid(state)) return null;
            float remaining = Mathf.Max(0.0f, state.Remaining - elapsed);
            foreach (var live in Active)
                if (live != null && live.isActiveAndEnabled && live._state.EventId == state.EventId)
                {
                    bool snap = state.Split && !live._state.Split;
                    live._state = state;
                    live._age = state.Duration - remaining;
                    if (snap) live.PlaySnap();
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
            wire._look.StepTo(wire._age);
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
            _look?.StepTo(_age);
            if (_state.Split && _age - _state.FirstScale > SnapSeconds) { Retire(); return; }
            if (Remaining <= 0.0f) Retire();
        }

        /// <summary>
        /// ⚠️ HOST ONLY: an attacker's feet going from one side of the segment to the other between two physics steps, within
        /// its length and under `LiveWireReachAbove`. Taya, tagged and already-zapped-by-this-wire bodies are ignored.
        /// </summary>
        private void FixedUpdate()
        {
            if (!NetAuthority.ShouldResolve() || _state.Split || GameServices.Round == null || !GameServices.Round.RoundActive)
                return;
            var a = PostA; var b = PostB;
            foreach (var player in GameServices.Round.Players)
            {
                if (player == null) continue;
                var feet = player.transform.position;
                bool had = _previousFeet.TryGetValue(player, out var before);
                _previousFeet[player] = feet;
                if (!had || PresentationClock.BlocksInput || player.IsDefender || player.IsTagged
                    || _zapped.Contains(player.PlayerSlot)) continue;
                if (!ZackRules.Crossed(a.x, a.z, b.x, b.z, before.x, before.z, feet.x, feet.z, out float along)) continue;
                var edge = b - a;
                float sideBefore = edge.x * (before.z - a.z) - edge.z * (before.x - a.x);
                float sideNow = edge.x * (feet.z - a.z) - edge.z * (feet.x - a.x);
                float crossedAt = Mathf.Clamp01(sideBefore / (sideBefore - sideNow));
                float crossingHeight = Mathf.Lerp(before.y, feet.y, crossedAt) - Mathf.Lerp(a.y, b.y, along);
                if (crossingHeight > ZackRules.LiveWireReachAbove) continue;
                _zapped.Add(player.PlayerSlot);
                player.ApplyZapped();
                var at = Vector3.Lerp(a, b, along);
                at.y += ZackRules.LiveWireHeightAt(along);
                NetCue.Play("sfx_zack_livewire_zap", at);
                Visual.MatchFlair.Announce(Visual.MatchFlair.Kind.HeroHit, _state.Owner, player.PlayerSlot, at, 1.0f);
                if (Overcharged) continue;
                _state.Split = true;
                _state.FirstScale = _age;
                PlaySnap();
                MatchRpc.Instance?.BroadcastDynamicField(Capture());
                NetCue.Play("sfx_zack_livewire_snap", at);
                return;
            }
        }

        private void PlaySnap()
        {
            foreach (var hazard in _hazards) if (hazard != null) Destroy(hazard.gameObject);
            _hazards.Clear();
            _look?.Snap();
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
