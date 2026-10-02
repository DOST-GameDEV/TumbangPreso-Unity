using System;
using System.Collections.Generic;
using TumbangPreso.Net;
using TumbangPreso.Core;
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
            || kind == WorldEffectSnapshot.Kind.Mirrorwake || kind == WorldEffectSnapshot.Kind.Breakwater
            || kind == WorldEffectSnapshot.Kind.Waterwall || kind == WorldEffectSnapshot.Kind.Baha;
        public static float Gather(WorldEffectSnapshot.Kind kind) => kind == WorldEffectSnapshot.Kind.Current ? RafiRules.CurrentGather
            : kind == WorldEffectSnapshot.Kind.Baha ? RafiRules.BahaWarning
            : kind == WorldEffectSnapshot.Kind.Breakwater ? .55f
            : kind == WorldEffectSnapshot.Kind.Waterwall ? RafiRules.WallGather : 0;

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

        public static RafiWaterField CastBaha(AbilityContext ctx)
        {
            if(!NetAuthority.ShouldResolve()||ctx?.Motor==null)return null;
            var forward=ctx.AimPoint-ctx.Position;forward.y=0;
            if(forward.sqrMagnitude<.01f)forward=ctx.Forward;
            forward.y=0;forward.Normalize();var right=Vector3.Cross(Vector3.up,forward);
            var origin=ctx.Position;origin.y=Slipper.FindGroundY(origin,.5f)+.015f;
            var path=new Vector3[9];float distance=0;
            for(int i=0;i<path.Length;i++)
            {
                var start=origin+right*Mathf.Lerp(-RafiRules.BahaHalfWidth,RafiRules.BahaHalfWidth,i/8f);
                float edge=RafiRules.BahaLaneExit(start.x,start.z,forward.x,forward.z,
                    AIController.PlayableMinX,AIController.PlayableMaxX,AIController.PlayableMinZ,AIController.PlayableMaxZ);
                float clear=ClearDistance(start+Vector3.up*.4f,forward,edge);
                path[i]=start+forward*clear;distance=Mathf.Max(distance,clear);
            }
            float duration=RafiRules.BahaDuration(distance);
            var state=new WorldEffectSnapshot.Field{Type=WorldEffectSnapshot.Kind.Baha,EventId=++_nextId,
                Position=origin,Forward=forward,Radius=RafiRules.BahaHalfWidth,FirstScale=RafiRules.BahaSpeed,
                SecondScale=distance,Duration=duration,Remaining=duration,Owner=ctx.Motor.PlayerSlot,Path=path};
            var field=Restore(state,0);MatchRpc.Instance?.BroadcastDynamicField(state);return field;
        }

        public static bool CanPlaceWall(AbilityContext ctx, Vector3 point)
        {
            if (ctx?.Motor == null || !float.IsFinite(point.sqrMagnitude)) return false;
            var flat=point-ctx.Position;flat.y=0;
            if (flat.sqrMagnitude<.25f || flat.sqrMagnitude>RafiRules.WallRange*RafiRules.WallRange+.01f) return false;
            var forward=flat.normalized;var right=Vector3.Cross(Vector3.up,forward);
            float floor=Slipper.FindGroundY(point,.5f);
            if(Mathf.Abs(floor-point.y)>.3f) return false;
            var left=point-right*RafiRules.WallHalfWidth;var end=point+right*RafiRules.WallHalfWidth;
            if(left.x<AIController.PlayableMinX || left.x>AIController.PlayableMaxX
                || left.z<AIController.PlayableMinZ || left.z>AIController.PlayableMaxZ
                || end.x<AIController.PlayableMinX || end.x>AIController.PlayableMaxX
                || end.z<AIController.PlayableMinZ || end.z>AIController.PlayableMaxZ) return false;
            if(Mathf.Abs(Slipper.FindGroundY(left,.5f)-floor)>.3f
                || Mathf.Abs(Slipper.FindGroundY(end,.5f)-floor)>.3f) return false;
            return ClearDistance(ctx.Position+Vector3.up*.9f,forward,flat.magnitude)>=flat.magnitude-.02f
                && ClearDistance(left+Vector3.up*.9f,right,RafiRules.WallHalfWidth*2)>=RafiRules.WallHalfWidth*2-.02f
                && ClearDistance(point+Vector3.up*.05f,Vector3.up,RafiRules.WallHeight)>=RafiRules.WallHeight-.02f;
        }

        public static RafiWaterField CastWall(AbilityContext ctx, Vector3 point)
        {
            if(!NetAuthority.ShouldResolve() || !CanPlaceWall(ctx,point)) return null;
            var forward=point-ctx.Position;forward.y=0;forward.Normalize();
            point.y=Slipper.FindGroundY(point,.5f);
            var state=new WorldEffectSnapshot.Field { Type=WorldEffectSnapshot.Kind.Waterwall,
                EventId=++_nextId, Position=point, Forward=forward, Radius=RafiRules.WallHalfWidth,
                Duration=RafiRules.WallSeconds, Remaining=RafiRules.WallSeconds,
                Owner=ctx.Motor.PlayerSlot, Path=Array.Empty<Vector3>() };
            var field=Restore(state,0);MatchRpc.Instance?.BroadcastRafiWater(state);return field;
        }

        public static bool Valid(WorldEffectSnapshot.Field f)
        {
            if (!IsWater(f.Type) || f.EventId <= 0 || f.Owner < 0 || f.Owner >= Core.Balance.PlayerCount
                || f.Forward.sqrMagnitude < .99f || f.Forward.sqrMagnitude > 1.01f || Mathf.Abs(f.Forward.y) > .01f
                || (f.Type != WorldEffectSnapshot.Kind.Baha && (f.Duration > RafiRules.WallSeconds || (f.Type != WorldEffectSnapshot.Kind.Waterwall && f.Duration > 3)))
                || f.Radius <= 0 || f.Radius > 3
                || (f.Type != WorldEffectSnapshot.Kind.Baha && f.SecondScale != 0 && f.SecondScale != 1) || f.Path == null || f.Path.Length > MaxPathPoints) return false;
            if(f.Type==WorldEffectSnapshot.Kind.Baha)
            {
                if(f.Path.Length!=9||f.FirstScale!=RafiRules.BahaSpeed||f.Radius!=RafiRules.BahaHalfWidth
                    ||!float.IsFinite(f.SecondScale)||f.SecondScale<0||f.SecondScale>RafiRules.BahaMaximumRange
                    ||Mathf.Abs(f.Duration-RafiRules.BahaDuration(f.SecondScale))>.001f||f.Split)return false;
                var right=Vector3.Cross(Vector3.up,f.Forward);
                for(int i=0;i<9;i++)
                {
                    var offset=f.Path[i]-f.Position;float along=Vector3.Dot(offset,f.Forward);
                    if(!float.IsFinite(offset.sqrMagnitude)||Mathf.Abs(offset.y)>.01f||along<-.01f||along>f.SecondScale+.01f
                        ||Mathf.Abs(Vector3.Dot(offset,right)-Mathf.Lerp(-f.Radius,f.Radius,i/8f))>.01f)return false;
                }
                return true;
            }
            if (f.Type == WorldEffectSnapshot.Kind.Mirrorwake)
            {
                if (f.Path.Length < 2 || f.Path.Length > 8 || f.FirstScale != 0) return false;
                for (int i = 0; i < f.Path.Length; i++)
                    if (!float.IsFinite(f.Path[i].sqrMagnitude) || Vector3.Distance(f.Path[i], f.Position) > 14
                        || (i > 0 && Vector3.Distance(f.Path[i], f.Path[i - 1]) > 2.01f)) return false;
            }
            else if (f.Type == WorldEffectSnapshot.Kind.Current)
            { if (f.Path.Length != 0 || f.FirstScale < 8 || f.FirstScale > 11 || f.Radius > .65f) return false; }
            else if (f.Type == WorldEffectSnapshot.Kind.Waterwall)
            {
                if (f.Path.Length != 0 || f.Radius != RafiRules.WallHalfWidth || f.Duration != RafiRules.WallSeconds
                    || f.SecondScale != 0 || f.FirstScale < 0 || f.FirstScale > f.Duration
                    || (!f.Split && f.FirstScale != 0)) return false;
            }
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

        private float Travel => _state.Type==WorldEffectSnapshot.Kind.Baha
            ? Mathf.Min(_state.SecondScale,Mathf.Max(0,_age-RafiRules.BahaWarning)*RafiRules.BahaSpeed)
            : Mathf.Max(0, _age - Gather(_state.Type)) * _state.FirstScale;
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
            else if (_state.Type == WorldEffectSnapshot.Kind.Breakwater || _state.Type == WorldEffectSnapshot.Kind.Baha) ResolveWave(travel);
            else if (_state.Type == WorldEffectSnapshot.Kind.Waterwall && !_state.Split) ResolveWall();
            _previousTravel = travel; RememberShoes();
        }
        private void RememberShoes()
        { foreach (var shoe in _shoes) if (shoe != null) _previousShoes[shoe] = shoe.transform.position; }

        private void ResolveCurrent(float travel)
        {
            if (!NetAuthority.ShouldResolve()) return;
            var before = _state.Position + Vector3.up * .85f + _state.Forward * _previousTravel;
            var now = _state.Position + Vector3.up * .85f + _state.Forward * travel;
            // Solid cover stops the current itself; players and equipment do not.
            if (ClearDistance(_state.Position + Vector3.up * .85f, _state.Forward, travel) < travel - .03f)
            { SpendCurrent(); return; }
            Slipper first = null;
            float firstTime = float.PositiveInfinity;
            foreach (var shoe in _shoes)
            {
                if (shoe == null || shoe.State != SlipperState.InFlight || shoe.IsSkimming) continue;
                var velocity = shoe.Velocity;
                if (new Vector2(velocity.x, velocity.z).sqrMagnitude < .001f) continue;
                var a = (_previousShoes.TryGetValue(shoe, out var old) ? old : shoe.transform.position) - before;
                var b = shoe.transform.position - now;
                if (!RafiRules.FirstCurrentContact(a.x, a.y, a.z, b.x, b.y, b.z, _state.Radius, out float time)) continue;
                if (time < firstTime || (time == firstTime && first != null && shoe.OwnerSlot < first.OwnerSlot))
                { first = shoe; firstTime = time; }
            }
            if (first != null && first.HostSteerFlight(_state.Forward, RafiRules.CurrentTurnDegrees))
            { NetCue.Play("sfx_rafi_intercept", first.transform.position); SpendCurrent(); }
        }
        private void ResolveWall()
        {
            var right=Vector3.Cross(Vector3.up,_state.Forward);
            Slipper first=null;float best=float.PositiveInfinity;Vector3 stop=Vector3.zero;
            foreach(var shoe in _shoes)
            {
                if(shoe==null || shoe.State!=SlipperState.InFlight || shoe.IsSkimming)continue;
                var before=(_previousShoes.TryGetValue(shoe,out var old)?old:shoe.transform.position)-_state.Position;
                var after=shoe.transform.position-_state.Position;
                float az=Vector3.Dot(before,_state.Forward),bz=Vector3.Dot(after,_state.Forward);
                if(!RafiRules.WallCrossing(Vector3.Dot(before,right),before.y,az,
                    Vector3.Dot(after,right),after.y,bz,out float time))continue;
                if(time<best || (time==best && first!=null && shoe.OwnerSlot<first.OwnerSlot))
                {
                    first=shoe;best=time;
                    stop=_state.Position+Vector3.Lerp(before,after,time)+_state.Forward*(az>=0?.15f:-.15f);
                }
            }
            if(first==null || !first.HostDropFlightAt(stop))return;
            _state.Split=true;_state.FirstScale=_age;_visual.SetState(_state);
            MatchRpc.Instance?.BroadcastRafiWater(Capture());
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
            if(_state.Type==WorldEffectSnapshot.Kind.Baha
                &&!RafiRules.BahaCrosses(side,offset.y,forward,_previousTravel,travel,limit,true))return false;
            var start = _state.Position + right * side + Vector3.up * .4f;
            return ClearDistance(start, _state.Forward, Mathf.Max(0, forward)) >= forward - .04f;
        }
        private void ResolveWave(float travel)
        {
            bool frontActive=_state.Type!=WorldEffectSnapshot.Kind.Baha
                ||_age<=RafiRules.BahaWarning+_state.SecondScale/RafiRules.BahaSpeed;
            if(frontActive) foreach (var player in GameServices.Round.Players)
            {
                if (player == null || player.PlayerSlot == _state.Owner || _hitPlayers.Contains(player.PlayerSlot)
                    || (_state.Type==WorldEffectSnapshot.Kind.Baha&&!player.IsGrounded)
                    || !Swept(player.transform.position, travel)) continue;
                _hitPlayers.Add(player.PlayerSlot);
                if (player.AbilitySystem != null && (player.AbilitySystem.IsImmuneToStuns || player.AbilitySystem.IsImmuneToTags)) continue;
                player.ApplyResolvedImpact(_state.Forward * 4.8f);
            }
            if(frontActive) foreach (var shoe in _shoes)
                if (shoe != null && shoe.State == SlipperState.Loose && !_movedShoes.Contains(shoe)
                    && Swept(shoe.transform.position, travel))
                { _movedShoes.Add(shoe); _carryLeft[shoe] = _state.Type==WorldEffectSnapshot.Kind.Baha?RafiRules.BahaCarryDistance:1.4f; }
            if(_state.Type==WorldEffectSnapshot.Kind.Baha)
                foreach(var shoe in _shoes)
                    if(shoe!=null&&shoe.State!=SlipperState.Loose&&_carryLeft.ContainsKey(shoe))_carryLeft[shoe]=0;
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
