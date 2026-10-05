using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Netcode;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class NetShutdownOriginTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator RequestedStopIsDistinguishedFromUnexpectedManagerShutdown()
        {
            var net = NetSession.Ensure();
            var start = net.StartHostAsync(18762);
            while (!start.IsCompleted) yield return null;
            Assert.IsTrue(start.Result);
            LogAssert.Expect(UnityEngine.LogType.Log, new Regex(@"\[NetLifecycle\] requested-stop server=True relay=False origin=.*", RegexOptions.Singleline));
            LogAssert.Expect(UnityEngine.LogType.Log, "[NetLifecycle] server-stopped wasHost=True requestedStop=True");
            net.Stop();
            while (net.GetComponent<NetworkManager>().IsListening) yield return null;
            yield return null;

            start = net.StartHostAsync(18762);
            while (!start.IsCompleted) yield return null;
            Assert.IsTrue(start.Result);
            LogAssert.Expect(UnityEngine.LogType.Log, "[NetLifecycle] server-stopped wasHost=True requestedStop=False");
            net.GetComponent<NetworkManager>().Shutdown();
            while (net.GetComponent<NetworkManager>().IsListening) yield return null;
            yield return null;
        }
    }
}
