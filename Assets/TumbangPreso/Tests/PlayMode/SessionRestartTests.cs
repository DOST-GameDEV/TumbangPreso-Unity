using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code. `PlayModeWorld.Reset` has the
        /// mechanism and why BOTH hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

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
            int frames = 0;
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!task.IsCompleted)
            {
                if (frames++ > 600 || Time.realtimeSinceStartup > deadline)
                    throw new TimeoutException("network operation exceeded 600 frames or 20 seconds");
                yield return null;
            }
            if (task.IsFaulted) throw task.Exception;
            onDone(task.Result);
        }

        private static IEnumerator Await(Task task)
        {
            int frames = 0;
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!task.IsCompleted)
            {
                if (frames++ > 600 || Time.realtimeSinceStartup > deadline)
                    throw new TimeoutException("queue operation exceeded 600 frames or 20 seconds");
                yield return null;
            }
            if (task.IsFaulted) throw task.Exception;
        }

        private static Task InvokeQueue(Matchmaker queue, string method, params object[] args)
            => (Task)typeof(Matchmaker).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                                       .Invoke(queue, args);

        private static void SetQueueNet(Matchmaker queue, NetSession net)
            => typeof(Matchmaker).GetField("_net", BindingFlags.Instance | BindingFlags.NonPublic)
                                 .SetValue(queue, net);

        /// <summary>
        /// ⚠️⚠️ A NESTED SESSION STILL HOSTS (QA, 2026-09-26: *"Could not open an online room. (relay allocation failed: There
        /// is no NetworkManager assigned to this instance!)"*). Netcode's `Initialize` returns silently for a NetworkManager
        /// whose GameObject has a parent, and its clean-up then throws that message. Before the fix this host start failed
        /// with it; `NetSession.PrepareManagerForStart` puts the object back at the root first.
        /// </summary>
        [UnityTest]
        public IEnumerator HostingWorksEvenWhenTheSessionWasNestedUnderSomething()
        {
            var net = NetSession.Ensure();
            yield return null;
            var holder = new GameObject("~TestHolder");
            // Netcode reports the nesting itself, the moment it happens; that report is the fault being set up, not a failure.
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("cannot be nested"));
            net.transform.SetParent(holder.transform, true);
            try
            {
                bool hosted = false;
                yield return Await(net.StartHostAsync(), r => hosted = r);
                Assert.IsTrue(hosted, "a nested session must still host. Status: " + net.Status);
                Assert.IsNull(net.transform.parent, "the session object must be back at the root");
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.IsTrue(net.IsNetworked, "the host should be listening");
            }
            finally
            {
                net.Stop();
                if (net.transform.parent == holder.transform) net.transform.SetParent(null, true);
                UnityEngine.Object.Destroy(holder);
            }
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

        [UnityTest]
        public IEnumerator StopDuringHostRestartPreventsThePendingStart()
        {
            var net = NetSession.Ensure();
            yield return null;
            bool first = false;
            yield return Await(net.StartHostAsync(), value => first = value);
            Assert.IsTrue(first, net.Status);

            var restart = net.StartHostAsync();
            Assert.IsFalse(restart.IsCompleted, "the restart must be waiting for NGO shutdown");
            net.Stop();
            bool restarted = true;
            yield return Await(restart, value => restarted = value);
            Assert.IsFalse(restarted, "STOP must invalidate a host start still awaiting shutdown");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsFalse(net.IsNetworked, "the cancelled host restart opened a new room");
        }

        [UnityTest]
        public IEnumerator CancelledDelayedHostCannotReadvertiseOrReviveQueue()
        {
            var net = NetSession.Ensure();
            var queue = Matchmaker.Ensure();
            SetQueueNet(queue, net);
            foreach (bool result in new[] { true, false })
            {
                var delayed = new TaskCompletionSource<bool>();
                var host = InvokeQueue(queue, "HostAsync", (Func<Task<bool>>)(() => delayed.Task));
                Assert.AreEqual(QueueState.Hosting, queue.State);

                queue.Cancel();
                delayed.SetResult(result);
                yield return Await(host);

                Assert.AreEqual(QueueState.Cancelled, queue.State);
                Assert.IsEmpty(net.Advert.PoolKey, "a cancelled host advertised itself to QUICK MATCH");
                Assert.IsFalse(queue.IsQueueing);
            }
        }

        [UnityTest]
        public IEnumerator CancelledDelayedJoinCannotEmitJoined()
        {
            var net = NetSession.Ensure();
            var queue = Matchmaker.Ensure();
            SetQueueNet(queue, net);
            int joined = 0;
            queue.Joined += () => joined++;
            var entry = new ServerQuery.Entry { JoinCode = "" };
            foreach (bool result in new[] { true, false })
            {
                var delayed = new TaskCompletionSource<bool>();
                var join = InvokeQueue(queue, "JoinAsync", entry,
                    (Func<Task<bool>>)(() => delayed.Task));
                Assert.AreEqual(QueueState.Joining, queue.State);

                queue.Cancel();
                delayed.SetResult(result);
                yield return Await(join);

                Assert.AreEqual(QueueState.Cancelled, queue.State);
                Assert.AreEqual(0, joined, "a cancelled join entered the found-lobby route");
            }
        }

        [UnityTest]
        public IEnumerator CancelledClientHandshakeClosesOnlyItsOwnTransport()
        {
            var net = NetSession.Ensure();
            yield return null;
            using var cancelled = new System.Threading.CancellationTokenSource();
            bool started = false;
            yield return Await(net.StartClientAsync("127.0.0.1", 18679, cancelled.Token),
                               value => started = value);
            Assert.IsTrue(started, net.Status);
            Assert.IsTrue(net.IsNetworked, "StartClient did not enter its pending handshake");

            cancelled.Cancel();
            yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsFalse(net.IsNetworked, "cancelled client socket kept listening");

            using var superseded = new System.Threading.CancellationTokenSource();
            bool secondClient = false;
            yield return Await(net.StartClientAsync("127.0.0.1", 18679, superseded.Token),
                               value => secondClient = value);
            Assert.IsTrue(secondClient, net.Status);
            var newerHost = net.StartHostAsync(18680);
            superseded.Cancel();
            bool hosted = false;
            yield return Await(newerHost, value => hosted = value);
            Assert.IsTrue(hosted, "a new host could not start after cancelling the old client");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(net.IsNetworked && net.IsHost,
                "the old client cancellation shut down its successor");
            net.Stop();
        }

        [UnityTest]
        public IEnumerator QueueJoinWaitsForSeatAfterClientTransportStarts()
        {
            var net = NetSession.Ensure();
            yield return null;
            bool started = false;
            yield return Await(net.StartClientAsync("127.0.0.1", 18679), value => started = value);
            Assert.IsTrue(started, net.Status);

            var seated = net.WaitForConnectionAsync(timeoutSeconds: 1f);
            Assert.IsFalse(seated.IsCompleted, "transport start was mistaken for an assigned seat");
            yield return null;
            typeof(NetSession).GetField("_everConnected", BindingFlags.Instance | BindingFlags.NonPublic)
                              .SetValue(net, true);
            net.ApplyAssignedSeat(1);
            bool joined = false;
            yield return Await(seated, value => joined = value);
            Assert.IsTrue(joined, "an admitted peer with an assigned seat was not released to the queue");
            yield return null;
            bool alreadySeated = false;
            yield return Await(net.WaitForConnectionAsync(), value => alreadySeated = value);
            Assert.IsTrue(alreadySeated, "Update clearing the completed attempt must not turn an admitted client into a failed join");
            net.Stop();
        }

        [UnityTest]
        public IEnumerator QueueJoinTimeoutClosesAnUnseatedClient()
        {
            var net = NetSession.Ensure();
            yield return null;
            bool started = false;
            yield return Await(net.StartClientAsync("127.0.0.1", 18679), value => started = value);
            Assert.IsTrue(started, net.Status);

            bool joined = true;
            yield return Await(net.WaitForConnectionAsync(timeoutSeconds: 0.05f),
                               value => joined = value);
            Assert.IsFalse(joined, "an unseated client was accepted into the queue room");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsFalse(net.IsNetworked, "the timed-out client kept a listening transport");
            Assert.That(net.Status, Does.Contain("timed out"), "Transport cleanup hid the actionable reason behind 'offline'.");
        }

        [UnityTest]
        public IEnumerator DestroyedQueueCannotPublishAfterDelayedHostCompletion()
        {
            var net = NetSession.Ensure();
            var queue = Matchmaker.Ensure();
            SetQueueNet(queue, net);
            var delayed = new TaskCompletionSource<bool>();
            var host = InvokeQueue(queue, "HostAsync", (Func<Task<bool>>)(() => delayed.Task));
            Assert.AreEqual(QueueState.Hosting, queue.State);

            UnityEngine.Object.Destroy(queue);
            yield return null;
            delayed.SetResult(true);
            yield return Await(host);
            Assert.IsEmpty(net.Advert.PoolKey, "a destroyed queue published its old advert");
        }

        [UnityTest]
        public IEnumerator OldHostCompletionCannotClearNewAttemptOrItsAdvert()
        {
            var net = NetSession.Ensure();
            var queue = Matchmaker.Ensure();
            SetQueueNet(queue, net);
            var oldCompletion = new TaskCompletionSource<bool>();
            var old = InvokeQueue(queue, "HostAsync", (Func<Task<bool>>)(() => oldCompletion.Task));
            queue.Cancel();

            var newCompletion = new TaskCompletionSource<bool>();
            var current = InvokeQueue(queue, "HostAsync", (Func<Task<bool>>)(() => newCompletion.Task));
            oldCompletion.SetResult(false);
            yield return Await(old);
            Assert.AreEqual(QueueState.Hosting, queue.State,
                "an older failure changed the new hosting attempt's state");

            newCompletion.SetResult(true);
            yield return Await(current);
            Assert.AreEqual(QueueState.Searching, queue.State);
            Assert.IsNotEmpty(net.Advert.PoolKey, "the current host did not publish its queue advert");
        }
    }
}
