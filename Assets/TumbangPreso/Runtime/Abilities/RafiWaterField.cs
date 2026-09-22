using System;
using System.Collections.Generic;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    // Host-owned moving effects. Rendering has its own pure timeline so a replay
    // cannot acquire a collider, hit a player or steer a live slipper.
    public sealed class RafiWaterField : MonoBehaviour
    {
        public static readonly List<RafiWaterField> Active = new List<RafiWaterField>();
        public const int MaxPathPoints = 9;
        private static int _nextId;
        private WorldEffectSnapshot.Field _state;
        private RafiWaterVisual _visual;
        private Slipper[] _shoes;
        private readonly Dictionary<Slipper, Vector3> _previousShoes = new Dictionary<Slipper, Vector3>();
        private readonly HashSet<int> _hitPlayers = new HashSet<int>();
        private readonly HashSet<Slipper> _movedShoes = new HashSet<Slipper>();
        private readonly Dictionary<Slipper, float> _carryLeft = new Dictionary<Slipper, float>();
        private float _age, _previousTravel;
        private int _round;
        private long _epoch;
        public float Remaining => Mathf.Max(0, _state.Duration - _age);
        public WorldEffectSnapshot.Field Capture()
        { var f = _state; f.Source = gameObject; f.Remaining = Remaining; return f; }
        public static bool IsWater(WorldEffectSnapshot.Kind kind) => kind == WorldEffectSnapshot.Kind.Current
            || kind == WorldEffectSnapshot.Kind.Mirrorwake || kind == WorldEffectSnapshot.Kind.Breakwater;
        public static float Gather(WorldEffectSnapshot.Kind kind) => kind == WorldEffectSnapshot.Kind.Current ? .18f
            : kind == WorldEffectSnapshot.Kind.Breakwater ? .55f : 0;

        public static RafiWaterField Cast(AbilityContext ctx, WorldEffectSnapshot.Kind kind,
            float radius, float speed, float duration, bool alternate, Vector3[] path = null)
        {
            if (!NetAuthority.ShouldResolve() || ctx?.Motor == null) return null;
            var forward = ctx.AimPoint - ctx.Position; forward.y = 0;
            if (forward.sqrMagnitude < .01f) forward = ctx.Forward;
            forward.Normalize();
            var state = new WorldEffectSnapshot.Field { Type = kind, EventId = ++_nextId,
                Position = ctx.Position, Forward = forward, Radius = radius, FirstScale = speed,
                SecondScale = alternate ? 1 : 0, Duration = duration, Remaining = duration,
                Owner = ctx.Motor.PlayerSlot, Path = path ?? Array.Empty<Vector3>() };
            if (kind == WorldEffectSnapshot.Kind.Breakwater)
            {
                state.Path = new Vector3[9]; var right = Vector3.Cross(Vector3.up, forward);
                for (int i = 0; i < state.Path.Length; i++)
                {
                    var start = state.Position + right * Mathf.Lerp(-radius, radius, i / 8f);
                    state.Path[i] = start + forward * ClearDistance(start + Vector3.up * .4f, forward, 8);
                }
            }
            var field = Restore(state, 0);
            MatchRpc.Instance?.BroadcastRafiWater(state);
            return field;
        }

        public static bool Valid(WorldEffectSnapshot.Field f)
        {
            if (!IsWater(f.Type) || f.EventId <= 0 || f.Owner < 0 || f.Owner >= Core.Balance.PlayerCount
                || f.Forward.sqrMagnitude < .99f || f.Forward.sqrMagnitude > 1.01f || Mathf.Abs(f.Forward.y) > .01f
                || f.Duration > 3 || f.Radius <= 0 || f.Radius > 3
                || (f.SecondScale != 0 && f.SecondScale != 1) || f.Path == null || f.Path.Length > MaxPathPoints) return false;
            if (f.Type == WorldEffectSnapshot.Kind.Mirrorwake)
            {
                if (f.Path.Length < 2 || f.Path.Length > 8 || f.FirstScale != 0) return false;
                for (int i = 0; i < f.Path.Length; i++)
                    if (!float.IsFinite(f.Path[i].sqrMagnitude) || Vector3.Distance(f.Path[i], f.Position) > 14
                        || (i > 0 && Vector3.Distance(f.Path[i], f.Path[i - 1]) > 2.01f)) return false;
            }
            else if (f.Type == WorldEffectSnapshot.Kind.Current)
            { if (f.Path.Length != 0 || f.FirstScale < 8 || f.FirstScale > 11 || f.Radius > .65f) return false; }
            else
            {
                if (f.Path.Length != 9 || f.FirstScale != 5 || f.Radius != 3) return false;
                for (int i = 0; i < 9; i++)
                    if (!float.IsFinite(f.Path[i].sqrMagnitude) || Vector3.Distance(f.Path[i], f.Position) > 9) return false;
            }
            return true;
        }

        public static RafiWaterField Restore(WorldEffectSnapshot.Field state, float elapsed)
        {
            if (!WorldEffectSnapshot.Valid(state)) return null;
            float remaining = Mathf.Max(0, state.Remaining - elapsed);
            if (remaining <= 0) return null;
            // Reliable spawn/contact updates share an identity with join snapshots.
            foreach (var live in Active)
                if (live != null && live.isActiveAndEnabled && live._state.EventId == state.EventId)
                {
                    live._state = state; live._age = state.Duration - remaining;
                    live._visual.SetState(state); live._visual.StepTo(live._age); return live;
                }
            var go = new GameObject("Rafi-" + state.Type);
            var field = go.AddComponent<RafiWaterField>();
            field._state = state; field._age = state.Duration - remaining;
            field._round = GameServices.Match?.RoundNumber ?? 0;
            field._epoch = MatchRpc.Instance?.PresentationMatchId ?? 0;
            field._visual = RafiWaterVisual.Build(go.transform, state);
            field._visual.StepTo(field._age);
            field._shoes = FindObjectsByType<Slipper>(FindObjectsSortMode.None);
            foreach (var shoe in field._shoes) field._previousShoes[shoe] = shoe.transform.position;
            field._previousTravel = field.Travel;
            Active.Add(field); return field;
        }

        private float Travel => Mathf.Max(0, _age - Gather(_state.Type)) * _state.FirstScale;
        private void Update()
        {
            if ((GameServices.Round != null && !GameServices.Round.RoundActive)
                || (GameServices.Match?.RoundNumber ?? 0) != _round
                || (MatchRpc.Instance?.PresentationMatchId ?? 0) != _epoch)
            { gameObject.SetActive(false); Destroy(gameObject); return; }
            _age += Time.deltaTime; _visual.StepTo(_age);
            if (Remaining <= 0) { gameObject.SetActive(false); Destroy(gameObject); }
        }

        private void FixedUpdate()
        {
            if (!NetAuthority.ShouldResolve() || GameServices.Round == null || !GameServices.Round.RoundActive
                || PresentationClock.BlocksInput || _state.Type == WorldEffectSnapshot.Kind.Mirrorwake) return;
            float travel = Travel;
            if (_age < Gather(_state.Type)) { RememberShoes(); return; }
            if (_state.Type == WorldEffectSnapshot.Kind.Current && !_state.Split) ResolveCurrent(travel);
            else if (_state.Type == WorldEffectSnapshot.Kind.Breakwater) ResolveWave(travel);
            _previousTravel = travel; RememberShoes();
        }
        private void RememberShoes()
        { foreach (var shoe in _shoes) if (shoe != null) _previousShoes[shoe] = shoe.transform.position; }

        private void ResolveCurrent(float travel)
        {
            var before = _state.Position + Vector3.up * .85f + _state.Forward * _previousTravel;
            var now = _state.Position + Vector3.up * .85f + _state.Forward * travel;
            // Solid cover stops the current itself; players and equipment do not.
            if (ClearDistance(_state.Position + Vector3.up * .85f, _state.Forward, travel) < travel - .03f)
            { SpendCurrent(); return; }
            foreach (var shoe in _shoes)
            {
                if (shoe == null || shoe.State != SlipperState.InFlight) continue;
                var a = (_previousShoes.TryGetValue(shoe, out var old) ? old : shoe.transform.position) - before;
                var b = shoe.transform.position - now; var delta = b - a;
                float t = delta.sqrMagnitude > .00001f ? Mathf.Clamp01(-Vector3.Dot(a, delta) / delta.sqrMagnitude) : 0;
                var closest = a + delta * t;
                if (Mathf.Abs(closest.y) > .70f || new Vector2(closest.x, closest.z).magnitude > _state.Radius) continue;
                if (shoe.HostSteerFlight(_state.Forward, 40))
                { NetCue.Play("sfx_rafi_intercept", shoe.transform.position); SpendCurrent(); break; }
            }
        }
        private void SpendCurrent()
        { _state.Split = true; _visual.SetState(_state); MatchRpc.Instance?.BroadcastRafiWater(Capture()); }

        private bool Swept(Vector3 point, float travel)
        {
            var offset = point - _state.Position;
            if (offset.y > .70f || offset.y < -.4f) return false;
            var right = Vector3.Cross(Vector3.up, _state.Forward);
            float side = Vector3.Dot(offset, right), forward = Vector3.Dot(offset, _state.Forward);
            if (Mathf.Abs(side) > _state.Radius || forward < _previousTravel - .45f || forward > travel + .45f) return false;
            int lane = Mathf.Clamp(Mathf.RoundToInt((side / _state.Radius + 1) * 4), 0, 8);
            float limit = Vector3.Dot(_state.Path[lane] - _state.Position, _state.Forward);
            if (forward > limit) return false;
            var start = _state.Position + right * side + Vector3.up * .4f;
            return ClearDistance(start, _state.Forward, Mathf.Max(0, forward)) >= forward - .04f;
        }
        private void ResolveWave(float travel)
        {
            foreach (var player in GameServices.Round.Players)
            {
                if (player == null || player.PlayerSlot == _state.Owner || _hitPlayers.Contains(player.PlayerSlot)
                    || !Swept(player.transform.position, travel)) continue;
                _hitPlayers.Add(player.PlayerSlot);
                if (player.AbilitySystem != null && (player.AbilitySystem.IsImmuneToStuns || player.AbilitySystem.IsImmuneToTags)) continue;
                player.ApplyResolvedImpact(_state.Forward * 4.8f);
            }
            foreach (var shoe in _shoes)
                if (shoe != null && shoe.State == SlipperState.Loose && !_movedShoes.Contains(shoe)
                    && Swept(shoe.transform.position, travel))
                { _movedShoes.Add(shoe); _carryLeft[shoe] = 1.4f; }
            foreach (var shoe in _shoes)
                if (shoe != null && shoe.State == SlipperState.Loose && _carryLeft.TryGetValue(shoe, out float left) && left > 0)
                {
                    float wanted = Mathf.Min(left, 5 * Time.fixedDeltaTime);
                    float moved = shoe.HostSweepLoose(_state.Forward * wanted);
                    _carryLeft[shoe] = moved < wanted - .005f ? 0 : Mathf.Max(0, left - moved);
                }
        }
        public static float ClearDistance(Vector3 origin, Vector3 direction, float length)
        {
            float result = length;
            foreach (var hit in Physics.RaycastAll(origin, direction, length, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<CharacterMotor>() != null
                    || hit.collider.GetComponentInParent<Slipper>() != null
                    || hit.collider.GetComponentInParent<Lata>() != null) continue;
                result = Mathf.Min(result, Mathf.Max(0, hit.distance - .06f));
            }
            return result;
        }
        private void OnEnable() { if (_state.EventId > 0 && !Active.Contains(this)) Active.Add(this); }
        private void OnDisable() { Active.Remove(this); }
    }
}
