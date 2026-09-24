using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    public sealed partial class CharacterMotor
    {
        private EdgeRecoveryKind _edgeKind;
        private Vector3 _edgeGrip,_edgeLanding,_edgeOutward,_edgeStart;
        private byte _edgePhase;
        private float _edgeElapsed,_edgeRatio,_edgePendingUntil,_edgeRequestAt=-99;
        private ulong _lastNetworkPoseSerial;
        public bool IsEdgeRecovering=>_edgeKind!=EdgeRecoveryKind.None;
        public EdgeRecoveryKind EdgeKind=>_edgeKind;
        public Vector3 EdgeGrip=>_edgeGrip;
        public Vector3 EdgeOutward=>_edgeOutward;
        public byte EdgePhase=>_edgePhase; // 0 reach/catch, 1 effort while hanging, 2 pull over lip
        public float EdgePhaseRatio=>_edgeRatio;
        public float EdgeMashRatio=>Mathf.Clamp01(_mashRemoved/Mathf.Max(.01f,_tripTotal-Balance.MinTripDown));
        private Vector3 HangingFeet=>_edgeGrip+_edgeOutward*(_cc.radius+.14f)-Vector3.up*1.28f;

        public bool BeginEdgeRecovery(MapEdgeAnchor anchor)
        {
            if(!NetAuthority.ShouldResolve()||IsEdgeRecovering||anchor.Kind==EdgeRecoveryKind.None||!gameObject.activeInHierarchy)return false;
            ApplyFallRecovery();
            _edgeKind=anchor.Kind;_edgeGrip=anchor.Grip;_edgeLanding=anchor.Landing;_edgeOutward=anchor.Outward;
            _edgeStart=transform.position;_edgeElapsed=0;_edgeRatio=0;_edgePhase=0;_edgePendingUntil=0;
            transform.rotation=Quaternion.LookRotation(-anchor.Outward,Vector3.up);
            _velocity=Vector3.zero;_externalVelocity=Vector3.zero;_grounded=false;_spawnSettle=0;
            Net.MatchRpc.Instance?.BeginEdgeMovementOwnership(_playerSlot);
            return true;
        }

        public bool TryBeginLagoonEdgeRecovery()
        {
            if(!NetAuthority.ShouldResolve()||PresentationClock.BlocksInput||IsEdgeRecovering||!CanMove()||Time.time-_edgeRequestAt<.12f)return false;
            _edgeRequestAt=Time.time;
            return MapEdgeGeometry.TryLagoon(this,out var anchor)&&BeginEdgeRecovery(anchor);
        }

        private bool StepEdgeRecoveryFixed(float dt)
        {
            if(!IsEdgeRecovering&&IsLocallySimulated()&&Intent.JustPressed(Verb.Jump)&&CanMove()&&IsSwimming)
            {
                if(NetAuthority.ShouldResolve())TryBeginLagoonEdgeRecovery();
                else if(Time.time-_edgeRequestAt>=.18f&&MapEdgeGeometry.TryLagoon(this,out _))
                {
                    _edgeRequestAt=Time.time;_edgePendingUntil=Time.time+.6f;
                    Net.MatchRpc.Instance?.RequestEdgeClimbServerRpc(_playerSlot,MovementEpoch);
                }
            }
            if(!IsEdgeRecovering)
            {
                if(Time.time>=_edgePendingUntil)return false;
                // Wait for host acceptance rather than predicting an ordinary water
                // jump through the bridge while its climb request is in flight.
                Intent.CommitFrame();return true;
            }
            // This is a movement constraint, not a pause of the character's
            // resource clocks. Preserve ordinary stunned/idle stamina behavior.
            if(NetAuthority.ShouldResolve()||IsLocallySimulated())
            {Stamina.StepFatigue(dt);Stamina.Step(dt,false,false);}
            if(IsLocallySimulated()&&Intent.JustPressed(Verb.Jump))RecoverFromInput();
            if(!NetAuthority.ShouldResolve())
            {StepNetworkReplica(dt);Intent.CommitFrame();return true;}

            _edgeElapsed+=dt;var feet=HangingFeet;
            if(_edgePhase==0)
            {
                float duration=_edgeKind==EdgeRecoveryKind.Lagoon?.48f:.30f;
                _edgeRatio=Mathf.Clamp01(_edgeElapsed/duration);float t=Mathf.SmoothStep(0,1,_edgeRatio);
                var at=Vector3.Lerp(_edgeStart,feet,t);
                // Leave the underside horizontally before reaching up from water.
                if(_edgeKind==EdgeRecoveryKind.Lagoon)
                {
                    float horizontal=Mathf.SmoothStep(0,1,Mathf.Clamp01(_edgeRatio*1.8f));
                    at.x=Mathf.Lerp(_edgeStart.x,feet.x,horizontal);at.z=Mathf.Lerp(_edgeStart.z,feet.z,horizontal);
                }
                SetEdgePose(at,false);
                if(_edgeRatio>=1){_edgePhase=1;_edgeElapsed=0;_edgeRatio=0;}
            }
            else if(_edgePhase==1)
            {
                SetEdgePose(feet,false);_edgeRatio=EdgeMashRatio;
                if(_tripLeft<=Balance.MinTripDown){_edgePhase=2;_edgeElapsed=0;_edgeRatio=0;}
            }
            else
            {
                _edgeRatio=Mathf.Clamp01(_edgeElapsed/Balance.MinTripDown);_tripLeft=Balance.MinTripDown*(1-_edgeRatio);
                var raised=feet;raised.y=Mathf.Max(_edgeGrip.y+.16f,_edgeLanding.y+.16f);
                var over=_edgeLanding;over.y=raised.y;Vector3 at;
                if(_edgeRatio<.44f)at=Vector3.Lerp(feet,raised,Mathf.SmoothStep(0,1,_edgeRatio/.44f));
                else if(_edgeRatio<.76f)at=Vector3.Lerp(raised,over,Mathf.SmoothStep(0,1,(_edgeRatio-.44f)/.32f));
                else at=Vector3.Lerp(over,_edgeLanding,Mathf.SmoothStep(0,1,(_edgeRatio-.76f)/.24f));
                SetEdgePose(at,_edgeRatio>=1);
                if(_edgeRatio>=1)
                {
                    ResetEdgeRecovery();_tripLeft=0;_tripTotal=0;AdvanceRecoveryEpisode();
                    _tripImmuneUntil=Time.time+Balance.TripGraceAfterGetUp;
                    Net.MatchRpc.Instance?.BeginEdgeMovementOwnership(_playerSlot);
                }
            }
            Intent.CommitFrame();
            // Edge trajectories belong to the host even for a normally client-driven
            // seat. Old owner movement is rejected until the climb finishes.
            if(NetAuthority.IsHost)Net.MatchRpc.Instance?.SyncUnitTransformClientRpc(_playerSlot,transform.position,transform.eulerAngles.y,_velocity);
            return true;
        }

        private void SetEdgePose(Vector3 feet,bool grounded)
        {
            float yaw=Mathf.Atan2(-_edgeOutward.x,-_edgeOutward.z)*Mathf.Rad2Deg;
            bool enabled=_cc.enabled;_cc.enabled=false;transform.SetPositionAndRotation(feet,Quaternion.Euler(0,yaw,0));_cc.enabled=enabled;
            _grounded=grounded;_networkGrounded=grounded;_velocity=Vector3.zero;_externalVelocity=Vector3.zero;_fallSpeed=0;
            _networkTargetPosition=feet;_networkTargetYaw=yaw;_networkTargetVelocity=Vector3.zero;
            _networkSmoothVelocity=Vector3.zero;_networkYawVelocity=0;
        }

        private void ResetEdgeRecovery()
        {_edgeKind=EdgeRecoveryKind.None;_edgePhase=0;_edgeRatio=0;_edgeElapsed=0;_edgePendingUntil=0;}

        public void ApplyEdgeRecoverySnapshot(EdgeRecoveryKind kind,Vector3 grip,Vector3 outward,byte phase,float ratio)
        {
            if(kind==EdgeRecoveryKind.None&&IsEdgeRecovering)
            {_tripLeft=0;_tripTotal=0;_pendingRecovery.Clear();}
            if(kind!=EdgeRecoveryKind.None||IsEdgeRecovering)_edgePendingUntil=0;
            _edgeKind=kind;_edgeGrip=grip;_edgeOutward=outward;_edgePhase=phase;_edgeRatio=Mathf.Clamp01(ratio);
        }

        public bool AcceptNetworkPoseSerial(ulong serial)
        {
            if(serial==0||serial<=_lastNetworkPoseSerial)return false;
            _lastNetworkPoseSerial=serial;return true;
        }
    }
}
