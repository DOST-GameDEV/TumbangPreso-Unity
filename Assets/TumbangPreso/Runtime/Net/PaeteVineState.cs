using TumbangPreso.Core;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Net
{
    public enum PaeteVinePhase : byte { Terrain, Player, Ended }
    // Host decision, not a client-selected victim. Epochs bind both bodies.
    public struct PaeteVineState : INetworkSerializable
    {
        public const int WireBytes=81;
        public GameplayActionScope Scope;
        public long Sequence;
        public int Owner,Target,TargetEpoch;
        public PaeteVinePhase Phase;
        public Vector3 Anchor,CasterEnd,TargetEnd;
        public float Duration,RoundClock;
        private static bool Point(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&p.sqrMagnitude<100000000f;
        public bool IsValid=>Scope.IsValid&&Sequence>0&&Owner>=0&&Owner<Balance.PlayerCount
            &&Target>=-1&&Target<Balance.PlayerCount&&Target!=Owner&&TargetEpoch>=0
            &&Phase>=PaeteVinePhase.Terrain&&Phase<=PaeteVinePhase.Ended
            &&(Phase!=PaeteVinePhase.Player||Target>=0)
            &&(Phase!=PaeteVinePhase.Terrain||Target==-1)
            &&Point(Anchor)&&Point(CasterEnd)&&Point(TargetEnd)
            &&!float.IsNaN(Duration)&&Duration>=0&&Duration<=3
            &&!float.IsNaN(RoundClock)&&RoundClock>=0&&RoundClock<=3600;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T:IReaderWriter
        {
            serializer.SerializeValue(ref Scope);serializer.SerializeValue(ref Sequence);
            serializer.SerializeValue(ref Owner);serializer.SerializeValue(ref Target);serializer.SerializeValue(ref TargetEpoch);
            byte phase=(byte)Phase;serializer.SerializeValue(ref phase);if(serializer.IsReader)Phase=(PaeteVinePhase)phase;
            serializer.SerializeValue(ref Anchor);serializer.SerializeValue(ref CasterEnd);serializer.SerializeValue(ref TargetEnd);
            serializer.SerializeValue(ref Duration);serializer.SerializeValue(ref RoundClock);
        }
        public static bool TryRead(ref FastBufferReader reader,out PaeteVineState state)
        {
            state=default;
            if(reader.Length-reader.Position!=WireBytes||!reader.TryBeginRead(WireBytes))return false;
            reader.ReadNetworkSerializable(out state);return state.IsValid;
        }
    }
}
