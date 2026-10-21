using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Net;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class AutomaticPlantShotPacketTests
    {
        private sealed class Peer : INetProvider
        {
            public bool Host;
            public bool IsHost => Host; public bool IsNetworked => true;
            public int LocalSlot => 1; public int LocalPeerId => 1;
            public bool IsSeatlessReferee => false;
        }
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private INetProvider _previous;
        private Peer _peer;
        private GameObject _router;
        private MatchRpc _rpc;
        private PaetePlant _plant;
        private int Shots => Object.FindObjectsByType<PaeteWoodenSlipper>(FindObjectsSortMode.None).Length;

        [UnitySetUp] public IEnumerator Before()
        {
            _previous = NetAuthority.Provider; yield return PlayModeWorld.Reset();
            _peer = new Peer(); NetAuthority.Provider = _peer;
            GameServices.Ensure(); GameServices.Match.ApplySnapshot(new int[4], 1, true);
            _router = new GameObject("Dormant actual plant shot receiver"); _router.SetActive(false);
            _rpc = _router.AddComponent<MatchRpc>();
            typeof(MatchRpc).GetProperty("PresentationMatchId").SetValue(_rpc, 123L);
            _plant = PaetePlant.Spawn(new Vector3(0, 1, -5), new Vector3(0, 0, -5), 1, 0);
            _plant.AdoptInstance(901);
            yield return null; Assert.Zero(Shots);
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_router != null) Object.Destroy(_router);
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _previous;
        }

        private void Deliver(ulong sender = 0, long match = 123, int round = 1, long instance = 901, int owner = 1, bool trailing = false)
        {
            using var writer = new FastBufferWriter(40, Allocator.Temp);
            writer.WriteValueSafe(owner); writer.WriteValueSafe(instance); writer.WriteValueSafe(match);
            writer.WriteValueSafe(round); writer.WriteValueSafe(Vector3.forward * 8);
            if (trailing) writer.WriteValueSafe((byte)1);
            Assert.AreEqual(trailing ? 37 : 36, writer.Length);
            using var reader = new FastBufferReader(writer, Allocator.Temp);
            typeof(MatchRpc).GetMethod("OnAutomaticPlantShotMsg", Hidden)
                .Invoke(_rpc, new object[] { sender, reader });
        }

        [UnityTest] public IEnumerator MatchingHostPacketCreatesTheReplicaSlipper()
        {
            Deliver(); Assert.AreEqual(1, Shots); Assert.IsFalse(_plant.ShotReady);
            yield return null;
        }
        [UnityTest] public IEnumerator OtherSenderWorldRoundOrPlantCannotFireTheReplica()
        {
            Deliver(sender: 2); Deliver(match: 122); Deliver(round: 2); Deliver(instance: 900); Deliver(owner: 4);
            Assert.Zero(Shots); Deliver(); Assert.AreEqual(1, Shots);
            yield return null;
        }
        [UnityTest] public IEnumerator HostLoopbackAndMalformedLengthCannotFireAnotherSlipper()
        {
            _peer.Host = true; Deliver(); Assert.Zero(Shots);
            _peer.Host = false; Deliver(trailing: true); Assert.Zero(Shots);
            Deliver(); Assert.AreEqual(1, Shots); yield return null;
        }
        [UnityTest] public IEnumerator DisabledRouterRetiresItsAutomaticShotSubscription()
        {
            var field = typeof(PaetePlant).GetField("AutomaticShotFired", BindingFlags.Static | BindingFlags.NonPublic);
            int Subscriptions() => ((Delegate)field.GetValue(null))?.GetInvocationList()
                .Count(d => ReferenceEquals(d.Target, _rpc)) ?? 0;
            _router.SetActive(true); Assert.AreEqual(1, Subscriptions());
            _router.SetActive(false); Assert.Zero(Subscriptions());
            _router.SetActive(true); Assert.AreEqual(1, Subscriptions());
            yield return null;
        }
    }
}
