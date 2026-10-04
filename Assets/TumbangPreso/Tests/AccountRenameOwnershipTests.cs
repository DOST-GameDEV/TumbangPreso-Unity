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
    public sealed class AccountRenameOwnershipTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private PlayerAccount _account;
        private GameSettings _previous;
        private readonly List<TaskCompletionSource<string>> _answers = new();
        private readonly List<Task> _pending = new();
        private int _changed;
        private void Field(string name, object value) => typeof(PlayerAccount).GetField(name, Hidden).SetValue(_account, value);
        private AccountProfile Current => (AccountProfile)typeof(PlayerAccount).GetField("_profile", Hidden).GetValue(_account);
        private void Signed(bool value) => typeof(PlayerAccount).GetProperty("IsSignedIn").SetValue(_account, value);
        private Task Rename(string name)
        {
            var task = _account.SetProfileAsync(name, "Requested bio", "", "");
            _pending.Add(task); return task;
        }
        [SetUp] public void Before()
        {
            _previous = SettingsStore.Current;
            SettingsStore.OverrideForTests(new GameSettings { AccountPlayerId = "owner-a", PlayerName = "Original" });
            _root = new GameObject("Dormant account rename test"); _root.SetActive(false);
            _account = _root.AddComponent<PlayerAccount>();
            Field("_profile", new AccountProfile { PlayerId = "owner-a", DisplayName = "Original" });
            Signed(true);
            _answers.Clear(); _pending.Clear(); _changed = 0;
            _account.Changed += () => _changed++;
            Field("_updateNameDispatch", (Func<string, Task<string>>)(_ =>
            {
                var answer = new TaskCompletionSource<string>(); _answers.Add(answer); return answer.Task;
            }));
            Field("_saveProfileDispatch", (Func<string, Task<string>>)(_ => Task.FromResult("{}")));
        }
        [TearDown] public async Task After()
        {
            foreach (var answer in _answers) answer.TrySetResult("Ignored#1234");
            await Task.WhenAll(_pending);
            Object.DestroyImmediate(_root);
            SettingsStore.OverrideForTests(_previous);
        }
        [Test] public async Task DelayedRenameCannotChangeGuestIdentity()
        {
            var task = Rename("Requested"); Assert.IsFalse(task.IsCompleted);
            _account.SignInAsGuest("Guest"); var guest = Current; _changed = 0;
            _answers[0].SetResult("Old_remote#1234"); await task;
            Assert.AreSame(guest, Current); Assert.AreEqual("Guest", _account.DisplayName);
            Assert.AreEqual(0, _changed); Assert.AreEqual("Original", SettingsStore.Current.PlayerName);
        }
        [Test] public async Task DelayedRenameCannotChangeReplacementAccount()
        {
            var task = Rename("Requested");
            var replacement = new AccountProfile { PlayerId = "owner-b", DisplayName = "Replacement" };
            Field("_profile", replacement);
            SettingsStore.Current.AccountPlayerId = "owner-b"; SettingsStore.Current.PlayerName = "Replacement";
            _answers[0].SetResult("Old_remote#1234"); await task;
            Assert.AreSame(replacement, Current); Assert.AreEqual("Replacement", _account.DisplayName);
            Assert.AreEqual("Replacement", SettingsStore.Current.PlayerName); Assert.AreEqual(0, _changed);
        }
        [Test] public async Task GuestRoundTripRetiresThePrimaryPendingRename()
        {
            var task = Rename("Requested");
            _account.SignInAsGuest("Guest"); _account.LeaveGuest(); Signed(true); _changed = 0;
            _answers[0].SetResult("Old_remote#1234"); await task;
            Assert.AreEqual("Requested", _account.DisplayName);
            Assert.AreEqual("Original", SettingsStore.Current.PlayerName); Assert.AreEqual(0, _changed);
        }
        [Test] public async Task EarlierRenameCannotOverwriteLaterAcceptedRename()
        {
            var first = Rename("First"); var second = Rename("Second");
            _answers[1].SetResult("Second#1234"); await second;
            _answers[0].SetResult("First#1234"); await first;
            Assert.AreEqual("Second", _account.DisplayName);
            Assert.AreEqual("Second", SettingsStore.Current.PlayerName); Assert.AreEqual(1, _changed);
        }
        [Test] public async Task SignedOutOwnerDoesNotApplyDelayedRemoteRename()
        {
            var task = Rename("Requested"); Signed(false);
            _answers[0].SetResult("Old_remote#1234"); await task;
            Assert.AreEqual("Requested", _account.DisplayName);
            Assert.AreEqual("Original", SettingsStore.Current.PlayerName); Assert.AreEqual(0, _changed);
        }
        [Test] public async Task CurrentOwnerRenameStillApplies()
        {
            var task = Rename("Requested"); _answers[0].SetResult("Canonical_name#1234"); await task;
            Assert.AreEqual("Canonical name", _account.DisplayName);
            Assert.AreEqual("Canonical name", SettingsStore.Current.PlayerName); Assert.AreEqual(1, _changed);
        }
        [Test] public async Task OfflineRenameStillAppliesWithoutDispatch()
        {
            Signed(false); await Rename("Offline name");
            Assert.AreEqual("Offline name", _account.DisplayName);
            Assert.AreEqual("Offline name", SettingsStore.Current.PlayerName);
            Assert.AreEqual(0, _answers.Count); Assert.AreEqual(1, _changed);
        }
    }
}
