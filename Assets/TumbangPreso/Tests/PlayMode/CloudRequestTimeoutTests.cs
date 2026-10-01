using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class CloudRequestTimeoutTests
    {
        private static UnityWebRequest Request(string url) => (UnityWebRequest)typeof(Net.CloudCode)
            .GetMethod("CreateRequest", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { url, "{}", "fixture-only" });
        [UnityTest, Timeout(40000)] public IEnumerator StalledServiceRequestSettlesWithinItsTimeout() => Run(false);
        [UnityTest, Timeout(15000)] public IEnumerator NormalLocalServiceResponseStillCompletes() => Run(true);
        private IEnumerator Run(bool reply)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            TcpClient peer = null;
            using var request = Request("http://127.0.0.1:" + port + "/");
            try
            {
                Assert.AreEqual("POST", request.method);
                Assert.AreEqual("application/json", request.GetRequestHeader("Content-Type"));
                var accept = listener.AcceptTcpClientAsync(); var operation = request.SendWebRequest();
                float began = Time.realtimeSinceStartup, until = began + 5;
                while (!accept.IsCompleted && Time.realtimeSinceStartup < until) yield return null;
                Assert.IsTrue(accept.IsCompleted, "The local server never accepted the actual production-built request.");
                peer = accept.Result;
                if (reply)
                {
                    byte[] body = Encoding.UTF8.GetBytes("{\"output\":{\"ok\":true}}");
                    byte[] headers = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: " + body.Length + "\r\nConnection: close\r\n\r\n");
                    var stream = peer.GetStream(); stream.Write(headers, 0, headers.Length); stream.Write(body, 0, body.Length); stream.Flush();
                }
                until = began + (reply ? 5 : request.timeout > 0 ? request.timeout + 5 : 2);
                while (!operation.isDone && Time.realtimeSinceStartup < until) yield return null;
                Directory.CreateDirectory("Logs/http-timeout1002");
                File.WriteAllText("Logs/http-timeout1002/" + reply + ".txt",
                    $"timeout={request.timeout}\nsettled={operation.isDone}\nresult={request.result}\nelapsed={Time.realtimeSinceStartup-began}\n");
                Assert.IsTrue(operation.isDone, "A stalled service request has no bounded completion; its caller remains busy.");
                if (reply)
                {
                    Assert.AreEqual(UnityWebRequest.Result.Success, request.result);
                    Assert.AreEqual("{\"output\":{\"ok\":true}}", request.downloadHandler.text);
                }
                else
                {
                    Assert.Greater(request.timeout, 0); Assert.AreNotEqual(UnityWebRequest.Result.Success, request.result);
                }
            }
            finally { request.Abort(); peer?.Dispose(); listener.Stop(); }
        }
    }
}
