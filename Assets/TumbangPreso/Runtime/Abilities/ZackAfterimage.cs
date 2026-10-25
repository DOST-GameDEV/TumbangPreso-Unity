using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ZACK'S AFTERIMAGE (2026-10-25 rework, plan section 2): an electric copy of him that runs straight ahead for 3 s and
    /// vanishes if the taya touches it.
    ///
    /// ⚠️ IT IS A HOST-OWNED DYNAMIC FIELD, NOT A BODY. `WorldEffectSnapshot.Kind.Afterimage` on the existing reliable
    /// dynamic-field message (Rafi's route): the host solves the path ONCE against static geometry and the playable rectangle,
    /// then sends only spawn and pop. Every peer draws the same run from the field's start, direction, length and age. It is
    /// never registered in `round.Players`, has no collider, and never blocks, scores, carries, tags, picks up or knocks the can.
    ///
    /// Field layout: `Position` start (his feet), `Forward` flat run direction, `Radius` path length, `Duration`
    /// `ZackRules.AfterimageLife(length)`, `FirstScale` copy index (0 or 1), `SecondScale` 1 when he held a slipper, `Split` popped.
    /// </summary>
    public sealed class ZackAfterimage : MonoBehaviour
    {
        public static readonly List<ZackAfterimage> Active = new List<ZackAfterimage>();
        private static int _nextId;

        /// <summary>A popped copy stays this long for its flecks, then goes.</summary>
        public const float PopSeconds = 0.4f;

        private WorldEffectSnapshot.Field _state;
        private ZackAfterimageBody _body;
        private ZackCurrentAudio _current;
        private float _age, _poppedAt = -1.0f;
        private int _round;
        private long _epoch;

        public int OwnerSlot => _state.Owner;
        public bool Popped => _state.Split;
        public float Age => _age;
        public float Remaining => Mathf.Max(0.0f, _state.Duration - _age);
        public int EventId => _state.EventId;

        /// <summary>Where the copy's feet are now, on every peer, from the field and its age alone.</summary>
        public Vector3 Feet => PositionAt(_state, _age);

        /// <summary>Live and unpopped: what an AI taya may chase.</summary>
        public bool IsDecoy => isActiveAndEnabled && !_state.Split && Remaining > 0.02f;

        public static Vector3 PositionAt(WorldEffectSnapshot.Field f, float age)
            => f.Position + f.Forward * ZackRules.AfterimageTravel(age, f.Radius);

        public WorldEffectSnapshot.Field Capture()
        { var f = _state; f.Source = gameObject; f.Remaining = Remaining; return f; }

        // ------------------------------------------------------------------ the host's cast

        /// <summary>
        /// ⚠️ HOST ONLY. Spawns <paramref name="copies"/> copies from <paramref name="start"/> along his flat aim (fanned by
        /// `ZackRules.AfterimageYaw`), each path solved once here, and broadcasts each complete field with a fresh event id.
        /// </summary>
        public static void Cast(CharacterMotor motor, Vector3 start, Vector3 forward, int copies, bool holding)
        {
            if (!NetAuthority.ShouldResolve() || motor == null) return;
            forward.y = 0.0f;
            if (forward.sqrMagnitude < 1e-4f) forward = motor.transform.forward;
            forward.y = 0.0f;
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
            forward.Normalize();
            start.y = Slipper.FindGroundY(start, 0.5f);
            copies = Mathf.Clamp(copies, 1, 2);
            for (int i = 0; i < copies; i++)
            {
                var dir = Quaternion.Euler(0.0f, ZackRules.AfterimageYaw(i, copies), 0.0f) * forward;
                dir.y = 0.0f; dir.Normalize();
                float length = SolvePath(start, dir);
                var state = new WorldEffectSnapshot.Field
                {
                    Type = WorldEffectSnapshot.Kind.Afterimage, EventId = ++_nextId,
                    Position = start, Forward = dir, Radius = length,
                    Duration = ZackRules.AfterimageLife(length), Remaining = ZackRules.AfterimageLife(length),
                    Owner = motor.PlayerSlot, FirstScale = i, SecondScale = holding ? 1.0f : 0.0f,
                    Path = Array.Empty<Vector3>()
                };
                Restore(state, 0.0f);
                MatchRpc.Instance?.BroadcastDynamicField(state);
            }
        }

        /// <summary>How far a copy runs from <paramref name="start"/> along <paramref name="dir"/>: the full reach, cut at the
        /// first static wall (chest height) and at the playable rectangle's edge.</summary>
        public static float SolvePath(Vector3 start, Vector3 dir)
        {
            float reach = ZackRules.AfterimageReach;
            float edge = reach;
            if (dir.x > 1e-4f) edge = Mathf.Min(edge, (AIController.PlayableMaxX - start.x) / dir.x);
            else if (dir.x < -1e-4f) edge = Mathf.Min(edge, (AIController.PlayableMinX - start.x) / dir.x);
            if (dir.z > 1e-4f) edge = Mathf.Min(edge, (AIController.PlayableMaxZ - start.z) / dir.z);
            else if (dir.z < -1e-4f) edge = Mathf.Min(edge, (AIController.PlayableMinZ - start.z) / dir.z);
            edge = Mathf.Clamp(edge, 0.0f, reach);
            float clear = RafiWaterField.ClearDistance(start + Vector3.up * 0.9f, dir, edge);
            return Mathf.Clamp(Mathf.Min(edge, clear), 0.0f, reach);
        }

        // ------------------------------------------------------------------ validation and restore (every peer)

        public static bool Valid(WorldEffectSnapshot.Field f)
        {
            if (f.Type != WorldEffectSnapshot.Kind.Afterimage || f.EventId <= 0 || f.Owner < 0 || f.Owner >= Balance.PlayerCount
                || Mathf.Abs(f.Forward.y) > 0.01f || f.Forward.sqrMagnitude < 0.99f || f.Forward.sqrMagnitude > 1.01f
                || !float.IsFinite(f.Radius) || f.Radius < 0.0f || f.Radius > ZackRules.AfterimageReach + 0.01f
                || Mathf.Abs(f.Duration - ZackRules.AfterimageLife(f.Radius)) > 0.001f
                || (f.FirstScale != 0.0f && f.FirstScale != 1.0f) || (f.SecondScale != 0.0f && f.SecondScale != 1.0f)
                || f.Path == null || f.Path.Length != 0) return false;
            return true;
        }

        /// <summary>A spawn, a pop or a join snapshot: the same identity updates the live copy; otherwise one is made.</summary>
        public static ZackAfterimage Restore(WorldEffectSnapshot.Field state, float elapsed)
        {
            if (!Valid(state)) return null;
            float remaining = Mathf.Max(0.0f, state.Remaining - elapsed);
            foreach (var live in Active)
                if (live != null && live.isActiveAndEnabled && live._state.EventId == state.EventId)
                {
                    bool pop = state.Split && !live._state.Split;
                    live._state = state;
                    live._age = state.Duration - remaining;
                    if (pop) live.PlayPop();
                    return live;
                }
            if (remaining <= 0.0f || state.Split) return null;
            var go = new GameObject("ZackAfterimage-P" + (state.Owner + 1));
            var copy = go.AddComponent<ZackAfterimage>();
            copy._state = state;
            copy._age = state.Duration - remaining;
            copy._round = GameServices.Match?.RoundNumber ?? 0;
            copy._epoch = MatchRpc.Instance?.PresentationMatchId ?? 0;
            copy._body = ZackAfterimageBody.Build(go.transform, state);
            copy._body.StepTo(copy._age);
            copy._current = ZackCurrentAudio.Attach(go.transform, "sfx_zack_afterimage_loop");
            Active.Add(copy);
            return copy;
        }

        // ------------------------------------------------------------------ life

        private void Update()
        {
            if ((GameServices.Round != null && !GameServices.Round.RoundActive)
                || (GameServices.Match?.RoundNumber ?? 0) != _round
                || (MatchRpc.Instance?.PresentationMatchId ?? 0) != _epoch)
            { Retire(); return; }
            _age += Time.deltaTime;
            _body?.StepTo(_age);
            if (_state.Split && _poppedAt >= 0.0f && Time.time - _poppedAt > PopSeconds) { Retire(); return; }
            _current?.Step(Feet + Vector3.up * .8f, !_state.Split && Remaining > 0);
            if (Remaining <= 0.0f)
            {
                if (!_state.Split && NetAuthority.ShouldResolve()) NetCue.Play("sfx_zack_afterimage_fizzle", Feet);
                Retire();
            }
        }

        /// <summary>⚠️ HOST ONLY: the taya's feet within `AfterimagePopRadius` (flat) of the copy's pop it. Never a trigger.</summary>
        private void FixedUpdate()
        {
            if (!NetAuthority.ShouldResolve() || _state.Split || GameServices.Round == null || !GameServices.Round.RoundActive
                || PresentationClock.BlocksInput) return;
            var feet = Feet;
            foreach (var player in GameServices.Round.Players)
            {
                if (player == null || !player.IsDefender || player.IsTagged) continue;
                var d = player.transform.position - feet;
                if (Mathf.Abs(d.y) > 1.5f) continue;
                d.y = 0.0f;
                if (d.sqrMagnitude > ZackRules.AfterimagePopRadius * ZackRules.AfterimagePopRadius) continue;
                _state.Split = true;
                PlayPop();
                MatchRpc.Instance?.BroadcastDynamicField(Capture());
                NetCue.Play("sfx_zack_afterimage_pop", feet);
                return;
            }
        }

        private void PlayPop()
        {
            if (_poppedAt >= 0.0f) return;
            _poppedAt = Time.time;
            _current?.Step(Feet, false);
            _body?.Pop();
        }

        private void Retire()
        {
            Active.Remove(this);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        /// <summary>Every copy one seat sent (a round reset, a rejoin replacing the world).</summary>
        public static void RetireOwnedBy(int slot)
        {
            foreach (var live in Active.ToArray())
                if (live != null && (slot < 0 || live._state.Owner == slot)) live.Retire();
        }

        private void OnEnable() { if (_state.EventId > 0 && !Active.Contains(this)) Active.Add(this); }
        private void OnDisable() { Active.Remove(this); }
    }
}
