using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// § HOSTING OR JOINING WHILE A SESSION IS ALREADY LIVE.
    ///
    /// ⚠️⚠️ THIS IS THE ONLY TEST THAT CAN SEE THE FAULT, BECAUSE THE FIRST START ALWAYS WORKS.
    /// `NetworkManager.Shutdown()` does not shut anything down: it sets a flag, and
    /// `ShutdownInternal` runs later from the network update loop. `CanStart` refuses while
    /// `IsListening` is still true, so every start path in `NetSession` — all of which used to
    /// call `Stop()` and then start in the SAME FRAME — was silently rejected whenever a session
    /// was already up. A player hosting from a cold menu never saw it; a player backing out of a
    /// lobby and hosting again never got in. See `NetSession.EnsureStoppedAsync`.
    ///
    /// ⚠️ IT ASSERTS THE SECOND START, NOT THE FIRST. A test that only hosts once passes against
    /// the bug.
    /// </summary>
    public class SessionRestartTests
    {
        private static object Get(object o, string prop)
            => o?.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance)?.GetValue(o);

        /// <summary>Reflected so the test assembly needs no Netcode reference of its own.</summary>
        private static Component FindNetworkManager()
        {
            var t = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(x => x.FullName == "Unity.Netcode.NetworkManager");
            return t == null ? null : (Component)UnityEngine.Object.FindFirstObjectByType(t);
        }

        private static IEnumerator Await(System.Threading.Tasks.Task<bool> task, Action<bool> onDone)
        {
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception;
            onDone(task.Result);
        }

        [UnityTest]
        public IEnumerator HostingAgainWhileAlreadyHostingSucceeds()
        {
            var net = NetSession.Ensure();
            yield return null;

            bool first = false;
            yield return Await(net.StartHostAsync(), r => first = r);
            Assert.IsTrue(first, "the first host should start");

            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(net.IsNetworked, "the first host should be listening");

            // No Stop() here on purpose: the start path owns ending the previous session, and
            // that is exactly the path that was broken.
            bool second = false;
            yield return Await(net.StartHostAsync(), r => second = r);

            Assert.IsTrue(second,
                "hosting again while a session was already live must succeed; this is the " +
                "same-frame Shutdown fault. Status: " + net.Status);

            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(net.IsNetworked, "the second host should be listening");

            net.Stop();
            yield return null;
        }

        [UnityTest]
        public IEnumerator StopLeavesTheManagerListeningUntilTheNextFrame()
        {
            var net = NetSession.Ensure();
            yield return null;

            bool ok = false;
            yield return Await(net.StartHostAsync(), r => ok = r);
            Assert.IsTrue(ok);
            yield return new WaitForSecondsRealtime(0.3f);

            var nm = FindNetworkManager();
            Assert.IsNotNull(nm, "no NetworkManager in the scene");

            net.Stop();

            // ⚠️ THE CHARACTERISATION THE FIX RESTS ON. If a future NGO makes Shutdown
            // synchronous this assert fails, and `EnsureStoppedAsync`'s frame wait becomes dead
            // weight that should be deleted rather than left to rot.
            Assert.IsTrue((bool)Get(nm, "IsListening"),
                "NGO used to defer Shutdown; if this now fails, EnsureStoppedAsync can be simplified");

            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsFalse((bool)Get(nm, "IsListening"), "the shutdown should have completed by now");
        }
    }
}
