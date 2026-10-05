using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Net;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class HostedLobbyLifetimeTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private ServerQuery _query;
        private readonly List<TaskCompletionSource<string>> _creates = new List<TaskCompletionSource<string>>();
        private readonly List<Task> _operations = new List<Task>();
        private readonly List<string> _deleted = new List<string>();
        private readonly List<(string Id, object Options)> _updates = new List<(string, object)>();
        private TaskCompletionSource<bool> _authentication, _deletion;
        private TaskCompletionSource<bool> _updateWait;

        [SetUp]
        public void Before()
        {
            Assert.IsTrue(Array.IndexOf(Environment.GetCommandLineArgs(), "-tp-profile") >= 0);
            Assert.IsFalse(NetIdentity.IsOnline, "Hosted lobby tests must never initialize or call SDK services.");
            _creates.Clear(); _operations.Clear(); _deleted.Clear(); _updates.Clear();
            _authentication = _deletion = _updateWait = null;
            _root = new GameObject("Dormant hosted lobby lifetime"); _root.SetActive(false);
            _query = _root.AddComponent<ServerQuery>();
            Field("_hostAuthDispatch", (Func<Task<bool>>)(() => _authentication?.Task ?? Task.FromResult(true)));
            // Contravariant object arguments keep this fixture free of SDK assembly references.
            Field("_createHostedIdDispatch", (Func<string, int, object, Task<string>>)((name, capacity, options) =>
            {
                Assert.AreEqual(LobbySession.MaxPlayers, capacity);
                var result = new TaskCompletionSource<string>(); _creates.Add(result); return result.Task;
            }));
            Field("_updateHostedDispatch", (Func<string, object, Task>)((id, options) =>
            { _updates.Add((id, options)); return Task.CompletedTask; }));
            Field("_deleteHostedDispatch", (Func<string, Task>)(id =>
            { _deleted.Add(id); return _deletion?.Task ?? Task.CompletedTask; }));
        }

        [TearDown]
        public async Task After()
        {
            _authentication?.TrySetResult(false); _deletion?.TrySetResult(true);
            _updateWait?.TrySetResult(true);
            for (int i = 0; i < _creates.Count; i++) _creates[i].TrySetResult("cleanup-lobby-" + i);
            await Task.WhenAll(_operations);
            // Drive local cleanup explicitly: dormant EditMode has no runtime OnDestroy.
            await _query.DeleteHostedLobbyAsync();
            if (_root != null) Object.DestroyImmediate(_root);
        }

        private void Field(string name, object value) => typeof(ServerQuery).GetField(name, Hidden).SetValue(_query, value);
        private T Read<T>(string name) => (T)typeof(ServerQuery).GetField(name, Hidden).GetValue(_query);
        private Task<string> Create(string name)
        {
            var task = _query.CreateHostedLobbyAsync(name, name, "synthetic-relay", 1, 1, ServerQuery.HostedAdvert.None);
            _operations.Add(task); return task;
        }
        private Task Delete()
        {
            var task = _query.DeleteHostedLobbyAsync(); _operations.Add(task); return task;
        }
        private static async Task Settle() { await Task.Yield(); await Task.Yield(); }

        [Test]
        public async Task SlowPublicationSerializesAndCoalescesToTheLatestRoomCounts()
        {
            var creating = Create("Current"); _creates[0].SetResult("lobby-current"); await creating;
            _updateWait = new TaskCompletionSource<bool>();
            int concurrent = 0, peak = 0;
            string published = null;
            Field("_updateHostedDispatch", (Func<string, object, Task>)(async (id, options) =>
            {
                _updates.Add((id, options)); concurrent++; peak = Math.Max(peak, concurrent);
                if (_updates.Count == 1) await _updateWait.Task;
                published = DataValue(options, "Occupied"); concurrent--;
            }));
            var first = _query.UpdateHostedLobbyAsync(2, 2, false, ServerQuery.HostedAdvert.None);
            var middle = _query.UpdateHostedLobbyAsync(3, 3, false, ServerQuery.HostedAdvert.None);
            var latest = _query.UpdateHostedLobbyAsync(4, 4, true, ServerQuery.HostedAdvert.None);
            _operations.AddRange(new[] { first, middle, latest });
            _updateWait.SetResult(true);
            await Task.WhenAll(first, middle, latest);
            Assert.AreEqual("4", published, "An older response overwrote the latest occupied count.");
            Assert.AreEqual(1, peak, "The same lobby had concurrent publication requests.");
            Assert.AreEqual(2, _updates.Count, "Intermediate queued counts were sent instead of coalesced.");
            Assert.AreEqual("1", DataValue(_updates[1].Options, "InProgress"));
        }
        [Test]
        public async Task RetiredWriterCannotDrainOrDelayTheReplacementRoom()
        {
            var firstRoom = Create("First"); _creates[0].SetResult("lobby-first"); await firstRoom;
            _updateWait = new TaskCompletionSource<bool>();
            Field("_updateHostedDispatch", (Func<string, object, Task>)((id, options) =>
            {
                _updates.Add((id, options));
                return id == "lobby-first" ? _updateWait.Task : Task.CompletedTask;
            }));
            var old = _query.UpdateHostedLobbyAsync(2, 2, false, ServerQuery.HostedAdvert.None);
            var queued = _query.UpdateHostedLobbyAsync(3, 3, false, ServerQuery.HostedAdvert.None);
            _operations.AddRange(new[] { old, queued });
            await Delete();
            var newRoom = Create("Second"); _creates[1].SetResult("lobby-second"); await newRoom;
            await _query.UpdateHostedLobbyAsync(1, 1, false, ServerQuery.HostedAdvert.None);
            Assert.AreEqual(2, _updates.Count, "Replacement publication waited for the retired room.");
            Assert.AreEqual("lobby-second", _updates[1].Id);
            _updateWait.SetResult(true); await Task.WhenAll(old, queued);
            Assert.AreEqual(2, _updates.Count, "Retired pending counts were published after replacement.");
            Assert.AreEqual("lobby-second", Read<string>("_activeHostLobbyId"));
        }

        [Test]
        public async Task FailedOlderPublicationStillDrainsTheLatestCounts()
        {
            var room = Create("Current"); _creates[0].SetResult("lobby-current"); await room;
            _updateWait = new TaskCompletionSource<bool>();
            Field("_updateHostedDispatch", (Func<string, object, Task>)(async (id, options) =>
            {
                _updates.Add((id, options));
                if (_updates.Count == 1)
                {
                    await _updateWait.Task;
                    throw new InvalidOperationException("synthetic publication failure");
                }
            }));
            var first = _query.UpdateHostedLobbyAsync(2, 2, false, ServerQuery.HostedAdvert.None);
            var latest = _query.UpdateHostedLobbyAsync(4, 4, true, ServerQuery.HostedAdvert.None);
            _operations.AddRange(new[] { first, latest });
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex("\\[NetIdentity\\] Lobby update failed.*synthetic publication failure"));
            _updateWait.SetResult(true); await Task.WhenAll(first, latest);
            Assert.AreEqual(2, _updates.Count);
            Assert.AreEqual("4", DataValue(_updates[1].Options, "Occupied"));
        }

        private static string DataValue(object options, string key)
        {
            var data = (IDictionary)options.GetType().GetProperty("Data").GetValue(options);
            var value = data[key];
            return (string)value.GetType().GetProperty("Value").GetValue(value);
        }

        [Test]
        public async Task OldStopCannotDeleteTheNewLobbyWhenItsCreationFinishesFirst()
        {
            var first = Create("First"); var stopping = Delete(); var second = Create("Second");
            Assert.AreEqual(2, _creates.Count);
            _creates[1].SetResult("lobby-second"); await second; await Settle();
            Assert.AreEqual("lobby-second", Read<string>("_activeHostLobbyId"));
            CollectionAssert.DoesNotContain(_deleted, "lobby-second", "Old cleanup selected the new room's advertisement.");
            _creates[0].SetResult("lobby-first"); await first; await stopping; await Settle();
            Assert.AreEqual("lobby-second", Read<string>("_activeHostLobbyId"));
            CollectionAssert.AreEqual(new[] { "lobby-first" }, _deleted);
        }

        [Test]
        public async Task RetiredCreationCannotAdoptItsIdOrClearTheNewCreationFlag()
        {
            var first = Create("First"); var second = Create("Second");
            _creates[0].SetResult("lobby-first"); await first; await Settle();
            Assert.IsNull(Read<string>("_activeHostLobbyId"), "A retired creation became the current advertisement.");
            Assert.IsTrue(Read<bool>("_creatingLobby"), "Old finally cleared the newer creation's pending state.");
            _creates[1].SetResult("lobby-second"); await second; await Settle();
            Assert.AreEqual("lobby-second", Read<string>("_activeHostLobbyId"));
            CollectionAssert.AreEqual(new[] { "lobby-first" }, _deleted);
        }

        [Test]
        public async Task AStoppedAuthenticationWaitCannotStartAnObsoleteLobbyAllocation()
        {
            _authentication = new TaskCompletionSource<bool>();
            var creating = Create("Stopped"); var stopping = Delete();
            _authentication.SetResult(true); await Settle();
            Assert.AreEqual(0, _creates.Count, "An obsolete authentication wait allocated another hosted lobby.");
            Assert.IsNull(await creating); await stopping;
            Assert.IsNull(Read<string>("_activeHostLobbyId")); Assert.IsEmpty(_deleted);
        }

        [Test]
        public async Task CurrentCreationAppliesTheLatestBufferedCountsOnlyOnce()
        {
            var creating = Create("Current");
            await _query.UpdateHostedLobbyAsync(2, 2, false, ServerQuery.HostedAdvert.None);
            await _query.UpdateHostedLobbyAsync(3, 4, true, ServerQuery.HostedAdvert.None);
            Assert.IsEmpty(_updates);
            _creates[0].SetResult("lobby-current"); Assert.AreEqual("lobby-current", await creating);
            Assert.AreEqual(1, _updates.Count); Assert.AreEqual("lobby-current", _updates[0].Id);
            Assert.AreEqual("3", DataValue(_updates[0].Options, "Seated"));
            Assert.AreEqual("4", DataValue(_updates[0].Options, "Occupied"));
            Assert.AreEqual("1", DataValue(_updates[0].Options, "InProgress"));
            Assert.IsFalse(Read<bool>("_hasPendingCounts"));
        }

        [Test]
        public async Task CurrentSuccessfulCreateAndDeleteStillUseTheirOwnLobbyId()
        {
            var creating = Create("Current"); _creates[0].SetResult("lobby-current");
            Assert.AreEqual("lobby-current", await creating);
            await Delete();
            Assert.IsNull(Read<string>("_activeHostLobbyId")); Assert.IsFalse(Read<bool>("_creatingLobby"));
            CollectionAssert.AreEqual(new[] { "lobby-current" }, _deleted);
        }

        [Test]
        public async Task ADeletionAlreadySentToTheOldIdDoesNotClearTheNewLobbyWhenItCompletes()
        {
            var first = Create("First"); _creates[0].SetResult("lobby-first"); await first;
            _deletion = new TaskCompletionSource<bool>();
            var deleting = Delete();
            var second = Create("Second"); _creates[1].SetResult("lobby-second"); await second;
            _deletion.SetResult(true); await deleting;
            Assert.AreEqual("lobby-second", Read<string>("_activeHostLobbyId"));
            CollectionAssert.AreEqual(new[] { "lobby-first" }, _deleted);
        }

        [Test]
        public async Task AuthenticationRefusalSettlesBothCreationAndItsRequestedCleanup()
        {
            _authentication = new TaskCompletionSource<bool>();
            var creating = Create("Refused"); var deleting = Delete();
            _authentication.SetResult(false);
            Assert.IsNull(await creating); await deleting;
            Assert.AreEqual(0, _creates.Count); Assert.IsEmpty(_deleted);
            Assert.IsFalse(Read<bool>("_creatingLobby"));
        }
    }
}
