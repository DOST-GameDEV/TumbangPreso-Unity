using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    // One host-owned crossing. Observers/replay never acquire a gameplay collider.
    public sealed class SeanCinderGate : MonoBehaviour
    {
        public static readonly List<SeanCinderGate> Active = new List<SeanCinderGate>();
        private static int _nextId;
        private WorldEffectSnapshot.Field _state;
        private SeanCinderVisual _visual;
        private readonly Vector3[] _previous = new Vector3[Balance.PlayerCount];
        private readonly bool[] _eligible = new bool[Balance.PlayerCount];
        private readonly int[] _side = new int[Balance.PlayerCount];
        private float _age;
        private int _round;
        private long _match;
        public int Owner => _state.Owner;
        public bool Spent => _state.Split;
        public float Remaining => Mathf.Max(0, _state.Duration - _age);
        public float ActiveRemaining => Spent ? 0 : Remaining;
        public WorldEffectSnapshot.Field Capture()
        { var state = _state; state.Source = gameObject; state.Remaining = Remaining; return state; }

        public static bool Valid(WorldEffectSnapshot.Field state)
            => state.Type == WorldEffectSnapshot.Kind.CinderGate && state.EventId > 0
                && state.Owner >= 0 && state.Owner < Balance.PlayerCount
                && Mathf.Abs(state.Forward.y) < .001f && Mathf.Abs(state.Forward.sqrMagnitude - 1) < .001f
                && state.Duration == SeanGateRules.TotalSeconds && state.Radius == SeanGateRules.HalfWidth
                && (state.Split ? Mathf.Abs(state.SecondScale) == 1 : state.SecondScale == 0) && state.Path != null && state.Path.Length == 0
                && state.FirstScale >= 0 && state.FirstScale <= state.Duration
                && (state.Split || state.FirstScale == 0);

        public static bool CanPlace(AbilityContext context, Vector3 point)
        {
            if (context?.Motor == null || !context.Motor.IsDefender || !float.IsFinite(point.sqrMagnitude)) return false;
            var delta = point - context.Position; delta.y = 0;
            if (delta.sqrMagnitude < .25f || delta.sqrMagnitude > SeanGateRules.PlacementRange * SeanGateRules.PlacementRange + .01f) return false;
            var normal = delta.normalized; var right = Vector3.Cross(Vector3.up, normal);
            float floor = Slipper.FindGroundY(point, .5f);
            if (Mathf.Abs(point.y - floor) > SeanGateRules.GroundTolerance) return false;
            var left = point - right * SeanGateRules.HalfWidth; var end = point + right * SeanGateRules.HalfWidth;
            foreach (var edge in new[] { left, end })
                if (edge.x < AIController.PlayableMinX || edge.x > AIController.PlayableMaxX
                    || edge.z < AIController.PlayableMinZ || edge.z > AIController.PlayableMaxZ
                    || Mathf.Abs(Slipper.FindGroundY(edge, .5f) - floor) > SeanGateRules.GroundTolerance) return false;
            return RafiWaterField.ClearDistance(context.Position + Vector3.up * .5f, normal, delta.magnitude) >= delta.magnitude - .02f
                && RafiWaterField.ClearDistance(left + Vector3.up * .15f, right, SeanGateRules.HalfWidth * 2) >= SeanGateRules.HalfWidth * 2 - .02f;
        }

        public static SeanCinderGate Cast(AbilityContext context, Vector3 point)
        {
            if (!NetAuthority.ShouldResolve() || !CanPlace(context, point)) return null;
            var normal = point - context.Position; normal.y = 0; normal.Normalize();
            point.y = Slipper.FindGroundY(point, .5f);
            _nextId = _nextId == int.MaxValue ? 1 : _nextId + 1;
            var state = new WorldEffectSnapshot.Field { Type = WorldEffectSnapshot.Kind.CinderGate,
                EventId = _nextId, Owner = context.Motor.PlayerSlot, Position = point, Forward = normal,
                Radius = SeanGateRules.HalfWidth, Duration = SeanGateRules.TotalSeconds,
                Remaining = SeanGateRules.TotalSeconds, Path = Array.Empty<Vector3>() };
            var gate = Restore(state, 0);
            MatchRpc.Instance?.BroadcastDynamicField(state);
            return gate;
        }

        public static SeanCinderGate Restore(WorldEffectSnapshot.Field state, float elapsed)
        {
            if (!WorldEffectSnapshot.Valid(state) || !float.IsFinite(elapsed) || elapsed < 0) return null;
            float remaining = Mathf.Max(0, state.Remaining - elapsed);
            if (remaining <= 0) return null;
            foreach (var live in Active)
                if (live != null && live.Owner == state.Owner && live._state.EventId == state.EventId)
                {
                    // Repeated/older updates cannot rewind a warning or revive a spent seam.
                    float age = Mathf.Max(live._age, state.Duration - remaining);
                    if (live.Spent && !state.Split) return live;
                    live._state = state; live._age = age;
                    live._visual.SetState(state); live._visual.StepTo(age); live.BindOwner();
                    return live;
                }
            var go = new GameObject("Sean Cinder Gate");
            var gate = go.AddComponent<SeanCinderGate>(); gate._state = state;
            gate._age = state.Duration - remaining;
            gate._round = GameServices.Match?.RoundNumber ?? 0;
            gate._match = MatchRpc.Instance?.PresentationMatchId ?? 0;
            gate._visual = SeanCinderVisual.Build(go.transform, state); gate._visual.StepTo(gate._age);
            Active.Add(gate); gate.RememberPlayers(false); gate.BindOwner();
            return gate;
        }

        private void BindOwner()
        {
            var owner = GameServices.Round?.PlayerAt(Owner);
            if (owner?.AbilitySystem?.Kit is SeanHeroKit kit) kit.AdoptGate(this);
        }

        private void Update()
        {
            var owner = GameServices.Round?.PlayerAt(Owner);
            if (GameServices.Round?.RoundActive != true || owner == null || !owner.IsDefender
                || !(owner.AbilitySystem?.Kit is SeanHeroKit)
                || (GameServices.Match?.RoundNumber ?? 0) != _round
                || (MatchRpc.Instance?.PresentationMatchId ?? 0) != _match)
            { Retire(); return; }
            _age += Time.deltaTime;
            _visual.StepTo(_age);
            if (Remaining <= 0) Retire();
        }

        private bool GroundedCandidate(CharacterMotor player)
            => player != null && player.isActiveAndEnabled && player.PlayerSlot != Owner
                && !player.IsTagged && player.IsGrounded && !player.IsFlying
                && Mathf.Abs(player.transform.position.y - _state.Position.y) <= SeanGateRules.GroundTolerance;

        private void RememberPlayers(bool allowHistory)
        {
            var round = GameServices.Round; if (round == null) return;
            foreach (var player in round.Players)
            {
                if (player == null || player.PlayerSlot < 0 || player.PlayerSlot >= Balance.PlayerCount) continue;
                int slot = player.PlayerSlot; _previous[slot] = player.transform.position;
                bool eligible = GroundedCandidate(player);
                _eligible[slot] = allowHistory && eligible;
                int side = SeanGateRules.Side(Vector3.Dot(player.transform.position - _state.Position, _state.Forward));
                if (!_eligible[slot] || side != 0) _side[slot] = side;
            }
        }

        private void FixedUpdate()
        {
            if (!NetAuthority.ShouldResolve() || GameServices.Round?.RoundActive != true || PresentationClock.BlocksInput) return;
            if (Spent || !SeanGateRules.IsArmed(_age)) { RememberPlayers(false); return; }
            CharacterMotor first = null; float earliest = float.PositiveInfinity; int approach = 0;
            var right = Vector3.Cross(Vector3.up, _state.Forward);
            foreach (var player in GameServices.Round.Players)
            {
                if (!GroundedCandidate(player)) continue;
                int slot = player.PlayerSlot;
                if (slot < 0 || slot >= Balance.PlayerCount || !_eligible[slot]) continue;
                var before = _previous[slot] - _state.Position; var now = player.transform.position - _state.Position;
                var capsule = player.GetComponent<CharacterController>();
                float radius = capsule != null ? capsule.radius * Mathf.Abs(player.transform.lossyScale.x) : .4f;
                if (!SeanGateRules.TryCrossing(Vector3.Dot(before, right), Vector3.Dot(before, _state.Forward),
                    Vector3.Dot(now, right), Vector3.Dot(now, _state.Forward), radius, _side[slot], out float time, out int side)) continue;
                if (time < earliest || (time == earliest && first != null && slot < first.PlayerSlot))
                { first = player; earliest = time; approach = side; }
            }
            RememberPlayers(true);
            if (first == null) return;
            _state.Split = true; _state.FirstScale = _age; _state.SecondScale = approach; _visual.SetState(_state);
            // Even an immune rival consumes the one crossing; no later body is hit.
            if (first.AbilitySystem == null || (!first.AbilitySystem.IsImmuneToStuns && !first.AbilitySystem.IsImmuneToTags))
                first.ApplyResolvedImpact(_state.Forward * approach * Mathf.Sqrt(2 * Balance.Friction * SeanGateRules.PushDistance));
            MatchRpc.Instance?.BroadcastDynamicField(Capture()); BindOwner();
        }

        private void Retire() { gameObject.SetActive(false); Destroy(gameObject); }
        private void OnDisable() { Active.Remove(this); }
    }
}
