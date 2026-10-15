using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    public sealed partial class NemuBlindfold
    {
        private long _networkId = NemuGhostFrame.NewId();
        private int _networkVictim = -1, _networkVictimEpoch;
        public NemuGhostPose CaptureNetwork() => new NemuGhostPose { Kind = 1, Id = _networkId, Phase = (byte)((int)_state + 1),
            Position = Spot, Forward = Facing, Auxiliary = _lungeFrom, Age = _age, PhaseAge = _stateAge,
            Target = _victim != null ? _victim.PlayerSlot : -1, TargetEpoch = _victim != null ? _victim.MovementEpoch : 0 };
        public void ApplyNetwork(NemuGhostPose pose, float elapsed)
        {
            _networkId = pose.Id; Spot = pose.Position; Facing = pose.Forward;
            _age = pose.Age + elapsed; _stateAge = pose.PhaseAge + elapsed;
            _state = (State)(pose.Phase - 1); _lungeFrom = pose.Auxiliary;
            _networkVictim = pose.Target; _networkVictimEpoch = pose.TargetEpoch;
            _victim = pose.Target >= 0 ? GameServices.Round?.PlayerAt(pose.Target) : null;
            if (_victim != null && _victim.MovementEpoch != pose.TargetEpoch) _victim = null;
            Draw();
        }
    }

    public sealed partial class NemuSkitter
    {
        private long _networkId = NemuGhostFrame.NewId();
        private bool _networkDriven;
        private Vector3 _networkAt;
        private Quaternion _networkFacing;
        public NemuGhostPose CaptureNetwork() => new NemuGhostPose { Kind = 2, Id = _networkId,
            Phase = (byte)(_leaving ? 4 : _carrying ? 2 : 1), Position = Position, Forward = Forward,
            Auxiliary = ReachFor ?? Vector3.zero, Age = _age, PhaseAge = _leaving ? _age - _leftAt : Mathf.Max(0, _age - _grabAt),
            Progress = _halfLength, Flags = (byte)((_lengthAlongX ? 1 : 0) | (ReachFor.HasValue ? 2 : 0) | (_delivered ? 4 : 0)), Target = -1 };
        public void ApplyNetwork(NemuGhostPose pose, float elapsed)
        {
            bool fresh = !_networkDriven;
            _networkId = pose.Id; _networkDriven = true; _networkAt = pose.Position;
            _networkFacing = Quaternion.LookRotation(pose.Forward);
            if (fresh) { Position = pose.Position; _lastPosition = Position; _facing = _networkFacing; }
            _age = pose.Age + elapsed; _leaving = pose.Phase == 4; _carrying = pose.Phase == 2;
            _leftAt = _age - pose.PhaseAge - elapsed;
            _grabAt = _age - pose.PhaseAge - elapsed;
            _halfLength = pose.Progress; _lengthAlongX = (pose.Flags & 1) != 0; _delivered = (pose.Flags & 4) != 0;
            ReachFor = (pose.Flags & 2) != 0 ? pose.Auxiliary : (Vector3?)null;
        }
        private void StepNetworkPose(float dt)
        {
            if (!_networkDriven) return;
            Position = Vector3.Lerp(Position, _networkAt, 1f - Mathf.Exp(-25f * dt));
            _facing = Quaternion.Slerp(_facing, _networkFacing, 1f - Mathf.Exp(-25f * dt));
        }
    }

    public sealed partial class NemuLookout
    {
        private long _networkId = NemuGhostFrame.NewId();
        public NemuGhostPose CaptureNetwork() => new NemuGhostPose { Kind = 3, Id = _networkId,
            Phase = (byte)(_leaving ? 4 : _age - _cheerAt < CheerSeconds ? 2 : 1), Position = _canAt + _side,
            Forward = Forward, Auxiliary = Watching ?? Vector3.zero, Age = _age,
            PhaseAge = _leaving ? _age - _leftAt : Mathf.Max(0, _age - _cheerAt), Progress = Mathf.Clamp01(Lift),
            Flags = (byte)((Scared ? 1 : 0) | (Watching.HasValue ? 2 : 0)), Target = -1 };
        public void ApplyNetwork(NemuGhostPose pose, float elapsed)
        {
            _networkId = pose.Id; _age = pose.Age + elapsed;
            _leaving = pose.Phase == 4; _leftAt = _age - pose.PhaseAge - elapsed;
            if (pose.Phase == 2) _cheerAt = _age - pose.PhaseAge - elapsed;
            Lift = pose.Progress; Scared = (pose.Flags & 1) != 0;
            Watching = (pose.Flags & 2) != 0 ? pose.Auxiliary : (Vector3?)null;
        }
    }
}
