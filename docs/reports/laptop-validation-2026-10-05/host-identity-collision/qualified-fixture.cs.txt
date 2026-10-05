using System;
using System.Collections;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class HostIdentityCollisionTests
    {
        private NetSession _session;
        private NetworkManager _manager;
        [Serializable] private sealed class Hello
        {
            public int Protocol;
            public string SkillContract, Token, Name;
        }
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _session = NetSession.Ensure();
            var start = _session.StartHostAsync(18764);
            while (!start.IsCompleted) yield return null;
            Assert.IsTrue(start.Result);
            _manager = _session.GetComponent<NetworkManager>();
            Assert.IsTrue(_manager.IsHost && _manager.IsListening);
            Assert.IsNotNull(_session.Lobby.PeerById((int)_manager.LocalClientId));
        }
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        private NetworkManager.ConnectionApprovalResponse Request(ulong peer, string token)
        {
            var response = new NetworkManager.ConnectionApprovalResponse();
            var hello = new Hello { Protocol = NetSession.ProtocolVersion,
                SkillContract = SkillContractFingerprint.Current, Token = token, Name = "Native arrival" };
            _manager.ConnectionApprovalCallback(new NetworkManager.ConnectionApprovalRequest {
                ClientNetworkId = peer, Payload = Encoding.UTF8.GetBytes(JsonUtility.ToJson(hello)) }, response);
            return response;
        }
        [UnityTest] public IEnumerator RemoteHostIdentityIsRefusedBeforeItCanReplaceTheRoomOwner()
        {
            int id = (int)_manager.LocalClientId;
            var host = _session.Lobby.PeerById(id);
            int leader = _session.Lobby.LeaderPeerId;
            var response = Request(101, host.Token);
            if (response.Approved)
                _session.Lobby.Admit(101, host.Token, "Colliding arrival", out _);
            bool keepsHost = ReferenceEquals(host, _session.Lobby.PeerById(id));
            bool keepsLeader = leader == _session.Lobby.LeaderPeerId;
            bool listening = _manager.IsListening;
            Debug.Log("[HostCollision] approved=" + response.Approved + " keepsHost=" + keepsHost
                + " leaderBefore=" + leader + " leaderAfter=" + _session.Lobby.LeaderPeerId + " listening=" + listening);
            Assert.IsTrue(!response.Approved && keepsHost && keepsLeader && listening,
                "Collision must be refused with host record, leader and transport preserved: approved="
                + response.Approved + " keepsHost=" + keepsHost + " keepsLeader=" + keepsLeader + " listening=" + listening);
            yield return null;
        }
        [UnityTest] public IEnumerator UniqueRemoteIdentityRemainsAdmissible()
        {
            Assert.IsTrue(Request(101, "native-unique-remote").Approved);
            yield return null;
        }
        [UnityTest] public IEnumerator RemoteReconnectStillReclaimsItsOwnSeat()
        {
            var old = _session.Lobby.Admit(101, "native-remote-reconnect", "Old remote");
            int seat = old.Seat;
            Assert.IsTrue(Request(102, old.Token).Approved);
            var current = _session.Lobby.Admit(102, old.Token, "New remote", out int retired);
            Assert.AreEqual(101, retired); Assert.AreEqual(seat, current.Seat);
            Assert.IsNotNull(_session.Lobby.PeerById((int)_manager.LocalClientId));
            yield return null;
        }
        [UnityTest] public IEnumerator LocalHostApprovalDoesNotRefuseItself()
        {
            var host = _session.Lobby.PeerById((int)_manager.LocalClientId);
            Assert.IsTrue(Request(_manager.LocalClientId, host.Token).Approved);
            yield return null;
        }
    }
}
