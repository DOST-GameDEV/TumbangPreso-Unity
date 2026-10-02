using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using NUnit.Framework;
using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class LanReceiveOwnershipTests
    {
        private GameObject _root;
        private LanBeacon _beacon;
        private UdpClient _old, _current, _sender;
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        [SetUp] public void Before()
        {
            _root = new GameObject("Owned LAN callback scope"); _beacon = _root.AddComponent<LanBeacon>();
            _old = Bound(); _current = Bound(); _sender = new UdpClient();
        }
        [TearDown] public void After()
        {
            _beacon?.StopAll(); _old?.Close(); _current?.Close(); _sender?.Close();
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
        }
        private static UdpClient Bound() => new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        private static string Payload(string name) => LanBeacon.BuildPayload(12345, 1, 4, false, "", name, 1, 1, 12, "other-process");
        private void Send(UdpClient socket, string name)
        {
            var data = Encoding.UTF8.GetBytes(Payload(name));
            _sender.Send(data, data.Length, (IPEndPoint)socket.Client.LocalEndPoint);
        }
        private IAsyncResult Completed(UdpClient socket, string name)
        {
            var result = socket.BeginReceive(null, socket); Send(socket, name);
            Assert.IsTrue(result.AsyncWaitHandle.WaitOne(1500), "Owned loopback datagram did not complete.");
            return result;
        }
        private void Adopt(UdpClient socket)
        {
            typeof(LanBeacon).GetField("_listener", Hidden).SetValue(_beacon, socket);
            typeof(LanBeacon).GetProperty("Listening").SetValue(_beacon, socket != null);
        }
        private void Receive(IAsyncResult result) => typeof(LanBeacon).GetMethod("OnReceive", Hidden).Invoke(_beacon, new object[] { result });
        private void Drain() => typeof(LanBeacon).GetMethod("DrainInbox", Hidden).Invoke(_beacon, null);
        private int Pending
        {
            get
            {
                var inbox = typeof(LanBeacon).GetField("_inbox", Hidden).GetValue(_beacon);
                return (int)inbox.GetType().GetProperty("Count").GetValue(inbox);
            }
        }
        [Test] public void RetiredCompletionCannotArmTheReplacementBeforeItsOwnReceiveStarts()
        {
            var previous = Completed(_old, "Previous room");
            // Real StartListening publishes the new listener/flag before BeginReceive.
            // Hold that valid pre-arm boundary while delivering a retired completion.
            Adopt(_current); Receive(previous); Send(_current, "Replacement room");
            Thread.Sleep(120);
            Assert.AreEqual(0, Pending, "The retired callback armed and consumed the replacement's first datagram.");
            Assert.Greater(_current.Available, 0, "Only the replacement's own startup may arm its first receive.");
            Assert.IsTrue(_beacon.Listening);
        }
        [Test] public void CurrentReceiveParsesAndRearmsForTheNextRealPacket()
        {
            Adopt(_current); Receive(Completed(_current, "First room"));
            Send(_current, "Next room"); Assert.IsTrue(SpinWait.SpinUntil(() => Pending >= 2, 1500));
            Drain(); Assert.AreEqual("Next room", _beacon.Entries.Single().HostName); Assert.IsTrue(_beacon.Listening);
        }
        [Test] public void CompletionAfterStopCannotRestoreDiscoveryOrEntries()
        {
            var result = Completed(_old, "Stopped room"); Adopt(_old); _beacon.StopAll(); Receive(result); Drain();
            Assert.IsFalse(_beacon.Listening); Assert.IsEmpty(_beacon.Entries); Assert.AreEqual(0, Pending);
        }
    }
}
