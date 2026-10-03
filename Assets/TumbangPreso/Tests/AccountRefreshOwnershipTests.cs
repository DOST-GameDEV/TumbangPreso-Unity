using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class AccountRefreshOwnershipTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private PlayerAccount _account;
        private GameSettings _previous;
        private AccountProfile _local;
        private string _serviceOwner;
        private int _loads, _writes;
        private readonly List<TaskCompletionSource<string>> _names = new();
        private readonly List<TaskCompletionSource<string>> _cloud = new();
        private readonly List<TaskCompletionSource<string>> _updates = new();
        private readonly List<Task> _pending = new();
        [Serializable] private sealed class Payload { public string profile; }
        private void Field(string name, object value) => typeof(PlayerAccount).GetField(name, Hidden).SetValue(_account, value);
        private static Task<string> Queue(List<TaskCompletionSource<string>> list)
        {
            var answer = new TaskCompletionSource<string>(); list.Add(answer); return answer.Task;
        }
        private Task Refresh()
        {
            var task = (Task)typeof(PlayerAccount).GetMethod("RefreshFromAuthenticationAsync", Hidden).Invoke(_account, new object[] { _local });
            _pending.Add(task); return task;
        }
        private void Replace()
        {
            Field("_profile", new AccountProfile { PlayerId = "owner-b", DisplayName = "Replacement" });
            SettingsStore.Current.AccountPlayerId = "owner-b"; SettingsStore.Current.PlayerName = "Replacement";
        }
        private static string ProfileReply(string name) => JsonUtility.ToJson(new Payload
        {
            profile = JsonUtility.ToJson(new AccountProfile { PlayerId = "owner-a", DisplayName = name })
        });
        private void Unchanged(string id = "owner-b", string name = "Replacement")
        {
            Assert.AreEqual(id, _account.PlayerId); Assert.AreEqual(name, _account.DisplayName);
            Assert.AreEqual(id, SettingsStore.Current.AccountPlayerId); Assert.AreEqual(name, SettingsStore.Current.PlayerName);
            Assert.AreEqual(0, _writes);
        }
        [SetUp] public void Before()
        {
            _previous = SettingsStore.Current;
            SettingsStore.OverrideForTests(new GameSettings { AccountPlayerId = "owner-a", PlayerName = "Original" });
            _root = new GameObject("Dormant canonical refresh ownership test"); _root.SetActive(false);
            _account = _root.AddComponent<PlayerAccount>();
            _local = new AccountProfile { PlayerId = "owner-a", DisplayName = "Original" };
            Field("_profile", _local); _serviceOwner = "owner-a"; _loads = _writes = 0;
            _names.Clear(); _cloud.Clear(); _updates.Clear(); _pending.Clear();
            Field("_refreshIdentityDispatch", (Func<(string PlayerId, string Username)>)(() => (_serviceOwner, "remote-user")));
            Field("_refreshNameDispatch", (Func<Task<string>>)(() => Queue(_names)));
            Field("_refreshLoadDispatch", (Func<Task<string>>)(() => { _loads++; return Queue(_cloud); }));
            Field("_updateNameDispatch", (Func<string, Task<string>>)(_ => Queue(_updates)));
            Field("_saveProfileDispatch", (Func<string, Task<string>>)(_ => { _writes++; return Task.FromResult("{}"); }));
        }
        [TearDown] public async Task After()
        {
            foreach (var answer in _names) answer.TrySetResult("Ignored#1234");
            foreach (var answer in _updates) answer.TrySetResult("Ignored#1234");
            foreach (var answer in _cloud) answer.TrySetResult(ProfileReply("Ignored"));
            await Task.WhenAll(_pending);
            if (_root != null) Object.DestroyImmediate(_root);
            SettingsStore.OverrideForTests(_previous);
        }
        [Test] public async Task ReplacementDuringPlayerNameCannotStartCloudLoad()
        {
            var task = Refresh(); Replace(); _names[0].SetResult("Remote#1234");
            foreach (var answer in _cloud) answer.TrySetResult(ProfileReply("Remote"));
            await task; Unchanged(); Assert.AreEqual(0, _loads);
        }
        [Test] public async Task ChangedServiceOwnerCannotContinueNameReply()
        {
            var task = Refresh(); _serviceOwner = "owner-b"; _names[0].SetResult("Remote#1234");
            foreach (var answer in _cloud) answer.TrySetResult(ProfileReply("Remote"));
            await task; Unchanged("owner-a", "Original"); Assert.AreEqual(0, _loads);
        }
        [Test] public async Task ReplacementDuringCloudLoadCannotBeOverwritten()
        {
            var task = Refresh(); _names[0].SetResult("Remote#1234"); Assert.AreEqual(1, _loads);
            Replace(); _cloud[0].SetResult(ProfileReply("Remote")); await task; Unchanged();
        }
        [Test] public async Task ChangedServiceOwnerCannotApplyCloudReply()
        {
            var task = Refresh(); _names[0].SetResult("Remote#1234"); _serviceOwner = "owner-b";
            _cloud[0].SetResult(ProfileReply("Remote")); await task; Unchanged("owner-a", "Original");
        }
        [Test] public async Task ReplacementDuringGeneratedNameCannotStartCloudLoad()
        {
            var task = Refresh(); _names[0].SetResult(""); Assert.AreEqual(1, _updates.Count);
            Replace(); _updates[0].SetResult("Generated#1234");
            foreach (var answer in _cloud) answer.TrySetResult(ProfileReply("Remote"));
            await task; Unchanged(); Assert.AreEqual(0, _loads);
        }
        [Test] public async Task StaleNameFailureCannotLoadAnotherOwnersProfile()
        {
            var task = Refresh(); Replace(); _names[0].SetException(new InvalidOperationException("Synthetic old name failure"));
            foreach (var answer in _cloud) answer.TrySetResult(ProfileReply("Remote"));
            await task; Unchanged(); Assert.AreEqual(0, _loads);
        }
        [Test] public async Task StaleCloudFailureCannotApplyLocalFallback()
        {
            var task = Refresh(); _names[0].SetResult("Remote#1234"); Replace();
            _cloud[0].SetException(new InvalidOperationException("Synthetic old cloud failure")); await task; Unchanged();
        }
        [Test] public async Task EarlierRefreshCannotOverwriteNewerAcceptedRefresh()
        {
            var first = Refresh(); var second = Refresh();
            _names[1].SetResult("Second#1234"); _cloud[0].SetResult(ProfileReply("Second")); await second;
            _names[0].SetResult("First#1234");
            foreach (var answer in _cloud) answer.TrySetResult(ProfileReply("First"));
            await first; Assert.AreEqual("Second", _account.DisplayName); Assert.AreEqual("Second", SettingsStore.Current.PlayerName);
        }
        [Test] public async Task DestroyedAccountCannotPersistPendingCloudReply()
        {
            var task = Refresh(); _names[0].SetResult("Remote#1234"); Object.DestroyImmediate(_root);
            _cloud[0].SetResult(ProfileReply("Remote")); await task;
            Assert.AreEqual("Original", SettingsStore.Current.PlayerName); Assert.AreEqual(0, _writes);
        }
        [Test] public async Task CurrentOwnerStillAdoptsCanonicalProfile()
        {
            var task = Refresh(); _names[0].SetResult("Remote#1234"); _cloud[0].SetResult(ProfileReply("Canonical")); await task;
            Assert.AreEqual("owner-a", _account.PlayerId); Assert.AreEqual("Canonical", _account.DisplayName);
            Assert.AreEqual(AccountRules.DerivedTag("owner-a"), _account.Discriminator); Assert.IsTrue(_account.IsSignedIn);
        }
        [Test] public async Task ActiveGuestKeepsItsIdentityAndReturnsToCanonicalPrimary()
        {
            var task = Refresh(); _account.SignInAsGuest("Guest"); var guestId = _account.PlayerId;
            _names[0].SetResult("Remote#1234"); _cloud[0].SetResult(ProfileReply("Canonical")); await task;
            Assert.AreEqual(guestId, _account.PlayerId); Assert.AreEqual("Original", SettingsStore.Current.PlayerName);
            _account.LeaveGuest(); Assert.AreEqual("owner-a", _account.PlayerId); Assert.AreEqual("Canonical", _account.DisplayName);
        }
        [Test] public async Task BootTimeoutLocalSnapshotStillAcceptsLateCanonicalReply()
        {
            var initial = new AccountProfile { PlayerId = "owner-a", DisplayName = "Original" };
            Field("_profile", initial); var task = Refresh();
            typeof(PlayerAccount).GetMethod("ApplyInitialFallback", Hidden).Invoke(_account, new object[] { initial, _local, "Timeout" });
            _names[0].SetResult("Remote#1234"); _cloud[0].SetResult(ProfileReply("Canonical")); await task;
            Assert.AreEqual("Canonical", _account.DisplayName); Assert.IsTrue(_account.IsSignedIn);
        }
        [Test] public async Task LegitimateSignInStillAdoptsDifferentAuthenticatedId()
        {
            _serviceOwner = "owner-b"; var task = Refresh(); _names[0].SetResult("Remote#1234");
            _cloud[0].SetResult(ProfileReply("Canonical")); await task;
            Assert.AreEqual("owner-b", _account.PlayerId); Assert.AreEqual("owner-b", SettingsStore.Current.AccountPlayerId);
            Assert.AreEqual(AccountRules.DerivedTag("owner-b"), _account.Discriminator);
        }
    }
}
