using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Net;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    [Category("Ugs")]
    public sealed class OnlineRoomPublicationProbe
    {
        private NetSession _net;
        private TaskCompletionSource<string> _publication;
        private static IEnumerator Await(Task task)
        {
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception.GetBaseException();
            if (task.IsCanceled) throw new OperationCanceledException();
        }

        [UnityTest]
        public IEnumerator RelayRoomPublishesTheCodeAndPublicListing()
        {
            if (Application.isBatchMode) Assert.Ignore("Explicit non-batch live-service check required.");
            Assert.That(Array.IndexOf(Environment.GetCommandLineArgs(), "-tp-profile"), Is.GreaterThanOrEqualTo(0));
            NetSession.RoomTitle = "Competition publication probe";
            NetSession.RoomVisibility = 0;
            _net = NetSession.Ensure();
            var hosting = _net.StartRelayHost();
            yield return Await(hosting);
            Debug.Log("[RoomPublication] host=" + hosting.Result + " status=" + _net.Status);
            Assert.IsTrue(hosting.Result, _net.Status);
            var pending = (TaskCompletionSource<string>)typeof(ServerQuery)
                .GetField("_hostLobbyCreation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_net.Query);
            Assert.IsNotNull(pending, "Host started without a room-publication operation.");
            yield return Await(pending.Task);
            Assert.IsNotEmpty(pending.Task.Result, "Relay started but the public room registration failed. Inspect Lobby creation error.");
            var options = new QueryLobbiesOptions
            {
                Count = 1,
                Filters = new System.Collections.Generic.List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.S1, _net.Lobby.JoinCode, QueryFilter.OpOptions.EQ)
                }
            };
            var lookup = LobbyService.Instance.QueryLobbiesAsync(options);
            yield return Await(lookup);
            Assert.That(lookup.Result.Results.Count, Is.EqualTo(1), "Published custom code did not resolve online.");
            var found = lookup.Result.Results[0];
            Assert.AreEqual(pending.Task.Result, found.Id);
            Assert.IsFalse(found.IsPrivate);
            Assert.AreEqual(_net.RelayJoinCode, found.Data["RelayCode"].Value);
            Debug.Log("[RoomPublication] code=" + _net.Lobby.JoinCode + " id=" + found.Id + " onlineLookup=1 public=" + !found.IsPrivate);
            yield return new WaitForSecondsRealtime(1.1f);
            var browse = _net.Query.RefreshOnlineLobbiesAsync();
            yield return Await(browse);
            bool listed = false;
            foreach (var entry in _net.Query.Servers) if (entry.Id == found.Id) listed = true;
            Assert.IsTrue(listed, "The shipping browser did not list its newly published public room.");
        }

        [UnityTest]
        public IEnumerator FailedPublicationCannotLeaveASuccessfulGhostRoom()
        {
            if (Application.isBatchMode) Assert.Ignore("Explicit non-batch live-service check required.");
            _net = NetSession.Ensure();
            typeof(ServerQuery).GetField("_createHostedIdDispatch", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_net.Query, (Func<string, int, CreateLobbyOptions, Task<string>>)((name, count, options) =>
                    Task.FromException<string>(new InvalidOperationException("Injected registration refusal"))));
            var hosting = _net.StartRelayHost();
            yield return Await(hosting);
            Assert.IsFalse(hosting.Result, "The room UI reports success although its online code was never registered.");
            Assert.IsFalse(_net.IsNetworked, "Failed registration left a live but undiscoverable room.");
            StringAssert.Contains("publish", _net.Status.ToLowerInvariant());
        }

        [UnityTest]
        public IEnumerator OnlineHostWaitsUntilItsCodeIsPublished()
        {
            if (Application.isBatchMode) Assert.Ignore("Explicit non-batch live-service check required.");
            _net = NetSession.Ensure();
            _publication = new TaskCompletionSource<string>();
            bool requested = false;
            typeof(ServerQuery).GetField("_createHostedIdDispatch", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_net.Query, (Func<string, int, CreateLobbyOptions, Task<string>>)((name, count, options) =>
                { requested = true; return _publication.Task; }));
            typeof(ServerQuery).GetField("_deleteHostedDispatch", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_net.Query, (Func<string, Task>)(_ => Task.CompletedTask));
            var hosting = _net.StartRelayHost();
            while (!requested && !hosting.IsCompleted) yield return null;
            yield return null;
            Assert.IsTrue(requested);
            Assert.IsFalse(hosting.IsCompleted, "The creator can share a custom code before it exists online.");
            _publication.SetResult("owned-delayed-fixture");
            yield return Await(hosting);
            Assert.IsTrue(hosting.Result, _net.Status);
        }

        [UnityTest]
        public IEnumerator RelayHostDoesNotOfferAnUnreachableDirectLanAddress()
        {
            if (Application.isBatchMode) Assert.Ignore("Explicit non-batch live-service check required.");
            _net = NetSession.Ensure();
            var hosting = _net.StartRelayHost();
            yield return Await(hosting);
            Assert.IsTrue(hosting.Result, _net.Status);
            Assert.IsTrue(_net.IsRelay);
            Assert.IsFalse(_net.Beacon.Advertising,
                "LAN-first code lookup selects a direct address although this host listens through Relay.");
        }

        [UnityTest]
        public IEnumerator DirectLanHostStillAdvertisesItsReachableAddress()
        {
            _net = NetSession.Ensure();
            var hosting = _net.StartHostAsync(49171);
            yield return Await(hosting);
            Assert.IsTrue(hosting.Result, _net.Status);
            Assert.IsFalse(_net.IsRelay);
            Assert.IsTrue(_net.Beacon.Advertising);
            Assert.AreEqual(49171, _net.Beacon.Port);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            _publication?.TrySetResult(null);
            _publication = null;
            if (_net != null)
            {
                if (_net.IsNetworked) _net.Stop();
                yield return Await(_net.Query.DeleteHostedLobbyAsync());
                while (_net.IsNetworked) yield return null;
                typeof(ServerQuery).GetField("_createHostedIdDispatch", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(_net.Query, null);
                typeof(ServerQuery).GetField("_deleteHostedDispatch", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(_net.Query, null);
            }
            NetSession.ClearRoomSettings();
            yield return PlayModeWorld.Reset();
        }
    }
}
