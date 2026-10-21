using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.Tests
{
    public sealed class LanListenerStartTests
    {
        private GameObject _root;
        private LanBeacon _beacon;
        private UdpClient _blocker;
        private UdpClient Listener => (UdpClient)typeof(LanBeacon).GetField("_listener", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_beacon);
        [SetUp] public void Before()
        {
            _root = new GameObject("Owned LAN startup scope"); _beacon = _root.AddComponent<LanBeacon>();
        }
        [TearDown] public void After()
        {
            _blocker?.Close(); _blocker = null;
            _beacon?.StopAll(); if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
        }
        private void BlockDiscoveryPort()
        {
            _blocker = new UdpClient(); _blocker.Client.ExclusiveAddressUse = true;
            _blocker.Client.Bind(new IPEndPoint(IPAddress.Any, LanBeacon.DiscoveryPort));
        }
        private void RefusedListen()
        {
            LogAssert.Expect(LogType.Warning, new Regex("^\\[Lan\\] could not listen on " + LanBeacon.DiscoveryPort + ":"));
            _beacon.StartListening(); Assert.IsFalse(_beacon.Listening);
        }
        [Test] public void FailedBindRetiresThePartiallyAllocatedListener()
        {
            BlockDiscoveryPort(); RefusedListen();
            Assert.IsNull(Listener, "A failed discovery bind retained its unused UDP socket.");
        }
        [Test] public void ARefusedListenerCanStartNormallyOnceThePortIsReleased()
        {
            BlockDiscoveryPort(); RefusedListen(); _blocker.Close(); _blocker = null;
            _beacon.StartListening(); Assert.IsTrue(_beacon.Listening); Assert.IsNotNull(Listener);
            _beacon.StopAll(); Assert.IsFalse(_beacon.Listening); Assert.IsNull(Listener);
        }
        [Test] public void StoppingANormalListenerDisposesOnlyItsOwnedSocket()
        {
            _beacon.StartListening(); Assert.IsTrue(_beacon.Listening); var socket = Listener;
            Assert.IsNotNull(socket); _beacon.StopAll(); Assert.IsNull(Listener);
            Assert.Throws<ObjectDisposedException>(() => socket.Send(new byte[] { 1 }, 1, new IPEndPoint(IPAddress.Loopback, 9)));
        }
    }
}
