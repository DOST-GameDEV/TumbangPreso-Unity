using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class ThrowChargeRelayTests
    {
        [Test]
        public void AnAcceptedRemoteChargeAppearsOnTheListenHost()
        {
            var oldRound=GameServices.Round;var oldNet=NetSession.Instance;var oldProvider=NetAuthority.Provider;
            var root=new GameObject("Remote charge routing contract");
            try
            {
                var net=root.AddComponent<NetSession>();SetStatic(typeof(NetSession),"Instance",net);
                NetAuthority.Provider=new SoloProvider();
                var peer=net.Lobby.Admit(9,"charge-contract","Peer");
                var round=root.AddComponent<RoundDirector>();SetStatic(typeof(GameServices),"Round",round);
                var seat=new GameObject("Remote player");seat.transform.SetParent(root.transform);
                var unit=seat.AddComponent<CharacterMotor>();unit.PlayerSlot=peer.Seat;unit.RoundActive=true;
                var carrier=seat.AddComponent<Carrier>();
                var shoe=new GameObject("Held test slipper");shoe.transform.SetParent(root.transform);
                typeof(Carrier).GetProperty("Held").GetSetMethod(true).Invoke(carrier,new object[]{shoe.AddComponent<Slipper>()});
                round.Register(unit);
                var rpc=root.AddComponent<MatchRpc>();
                Send(rpc,9,peer.Seat,true);
                Assert.GreaterOrEqual(carrier.ObservedChargePower,0,"The accepted tell was forwarded to peers but omitted on the host.");
                Assert.AreEqual(.5f,carrier.ObservedChargePower,.001f,"Received progress must not restart the visible windup.");
                Assert.AreEqual(-.65f,carrier.ObservedPektusSpin,.001f);
                Send(rpc,9,peer.Seat,true,float.NaN,1);
                Assert.AreEqual(.5f,carrier.ObservedChargePower,.001f,"NaN changed the observed clock.");
                Assert.AreEqual(-.65f,carrier.ObservedPektusSpin,.001f,"A malformed clock changed spin.");
                Send(rpc,99,peer.Seat,false);
                Assert.GreaterOrEqual(carrier.ObservedChargePower,0,"An unseated sender cancelled another player's preparation.");
                Send(rpc,9,peer.Seat,false);
                Assert.Less(carrier.ObservedChargePower,0,"Cancellation must clear the host's preparation.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                SetStatic(typeof(GameServices),"Round",oldRound);SetStatic(typeof(NetSession),"Instance",oldNet);
                NetAuthority.Provider=oldProvider;
            }
        }
        private static void Send(MatchRpc rpc,ulong sender,int slot,bool active,float seconds=1.25f,float spin=-.65f)
        {
            using var writer=new FastBufferWriter(32,Allocator.Temp);
            writer.WriteValueSafe(slot);writer.WriteValueSafe(active);
            writer.WriteValueSafe(seconds);writer.WriteValueSafe(spin);
            using var reader=new FastBufferReader(writer,Allocator.Temp);
            typeof(MatchRpc).GetMethod("OnReqThrowChargeMsg",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(rpc,new object[]{sender,reader});
        }
        private static void SetStatic(System.Type type,string name,object value)
            =>type.GetProperty(name).GetSetMethod(true).Invoke(null,new[]{value});
    }
}
