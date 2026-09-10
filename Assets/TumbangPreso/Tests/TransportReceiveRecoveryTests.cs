using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;
using Unity.Networking.Transport;

namespace TumbangPreso.Tests
{
    public sealed class TransportReceiveRecoveryTests
    {
        [Test]
        public void EmptyUdpPacketsDoNotRetireTheServersReceivePool()
        {
            using var server=NetworkDriver.Create();
            Assert.AreEqual(0,server.Bind(NetworkEndpoint.LoopbackIpv4.WithPort(0)));
            Assert.AreEqual(0,server.Listen());
            var endpoint=server.GetLocalEndpoint();
            using(var sender=new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp))
            {
                var at=new IPEndPoint(IPAddress.Loopback,endpoint.Port);
                for(int batch=0;batch<64;batch++)
                {
                    for(int i=0;i<32;i++)sender.SendTo(Array.Empty<byte>(),at);
                    Thread.Sleep(2);server.ScheduleUpdate().Complete();
                }
            }
            using var client=NetworkDriver.Create();
            var request=client.Connect(endpoint);Assert.IsTrue(request.IsCreated);
            var timer=Stopwatch.StartNew();NetworkConnection accepted=default;
            while(timer.Elapsed.TotalSeconds<2 && !accepted.IsCreated)
            {
                client.ScheduleUpdate().Complete();server.ScheduleUpdate().Complete();
                accepted=server.Accept();Thread.Sleep(2);
            }
            Assert.IsTrue(accepted.IsCreated,"Empty UDP receives exhausted the buffers needed by a legitimate join.");
        }
    }
}
