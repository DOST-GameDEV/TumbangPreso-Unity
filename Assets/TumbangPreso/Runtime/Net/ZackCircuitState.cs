using System;
using TumbangPreso.Core;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public enum CircuitPhase : byte { Idle, Acquiring, Followup }

    public struct ZackCircuitState : INetworkSerializable
    {
        public const int WireBytes=58;
        public int Seat,Target,FirstTarget;
        public GameplayActionScope Scope;
        public long Sequence,Episode;
        public CircuitPhase Phase;
        public float Remaining,Cooldown,RoundClock;
        public bool Second;
        public bool IsValid => Seat>=0 && Seat<Balance.PlayerCount && Scope.IsValid && Sequence>0 && Episode>=0
            && float.IsFinite(Remaining) && float.IsFinite(Cooldown) && float.IsFinite(RoundClock)
            && Remaining>=0 && Cooldown>=0 && Cooldown<=35.001f && RoundClock>=0 && RoundClock<=36000
            && (Phase==CircuitPhase.Idle ? Remaining==0 && Target==-1 && FirstTarget==-1 && !Second
                : Phase==CircuitPhase.Acquiring ? Episode>0 && Remaining<=.4001f && ValidTarget(Target)
                    && (Second ? ValidTarget(FirstTarget) && FirstTarget!=Target : FirstTarget==-1)
                : Phase==CircuitPhase.Followup && Episode>0 && Remaining<=1.0001f && Target==-1 && ValidTarget(FirstTarget) && !Second);
        private bool ValidTarget(int target)=>target>=0 && target<Balance.PlayerCount && target!=Seat;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T:IReaderWriter
        {
            serializer.SerializeValue(ref Seat);serializer.SerializeValue(ref Scope);
            serializer.SerializeValue(ref Sequence);serializer.SerializeValue(ref Episode);
            byte phase=(byte)Phase,second=Second?(byte)1:(byte)0;
            serializer.SerializeValue(ref phase);serializer.SerializeValue(ref Target);serializer.SerializeValue(ref FirstTarget);
            serializer.SerializeValue(ref Remaining);serializer.SerializeValue(ref Cooldown);serializer.SerializeValue(ref RoundClock);
            serializer.SerializeValue(ref second);
            if(phase>2 || second>1)throw new ArgumentOutOfRangeException(nameof(Phase));
            if(serializer.IsReader){Phase=(CircuitPhase)phase;Second=second!=0;}
        }
        public static bool TryRead(ref FastBufferReader reader,out ZackCircuitState state)
        {
            state=default;if(reader.Length-reader.Position!=WireBytes || !reader.TryBeginRead(WireBytes))return false;
            try{reader.ReadNetworkSerializable(out state);return state.IsValid;}
            catch(OverflowException){return false;}catch(ArgumentException){return false;}
        }
    }

    // Private aim destination goes to the host only while this acquisition exists.
    public struct ZackCircuitAim : INetworkSerializable
    {
        public const int WireBytes=48;
        public int Seat;
        public GameplayActionScope Scope;
        public long Episode,Sequence;
        public Vector3 Point;
        public bool IsValid=>Seat>=0 && Seat<Balance.PlayerCount && Scope.IsValid && Episode>0 && Sequence>0
            && float.IsFinite(Point.x) && float.IsFinite(Point.y) && float.IsFinite(Point.z);
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T:IReaderWriter
        {
            serializer.SerializeValue(ref Seat);serializer.SerializeValue(ref Scope);
            serializer.SerializeValue(ref Episode);serializer.SerializeValue(ref Sequence);serializer.SerializeValue(ref Point);
        }
        public static bool TryRead(ref FastBufferReader reader,out ZackCircuitAim aim)
        {
            aim=default;if(reader.Length-reader.Position!=WireBytes || !reader.TryBeginRead(WireBytes))return false;
            try{reader.ReadNetworkSerializable(out aim);return aim.IsValid;}
            catch(OverflowException){return false;}catch(ArgumentException){return false;}
        }
    }
}
