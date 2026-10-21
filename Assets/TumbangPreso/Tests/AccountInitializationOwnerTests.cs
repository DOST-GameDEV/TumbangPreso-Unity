using System;
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
    public sealed class AccountInitializationOwnerTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private PlayerAccount _account;
        private GameSettings _previous;
        private TaskCompletionSource<bool> _remote, _budget;
        private Task _pending;
        private void Field(string name, object value) => typeof(PlayerAccount).GetField(name, Hidden).SetValue(_account, value);
        [SetUp] public void Before()
        {
            _previous = SettingsStore.Current;
            SettingsStore.OverrideForTests(new GameSettings { AccountPlayerId = "owner-a", PlayerName = "Primary" });
            _root = new GameObject("Dormant initialization owner check"); _root.SetActive(false);
            _account = _root.AddComponent<PlayerAccount>();
            Field("_profile", new AccountProfile { PlayerId = "owner-a", DisplayName = "Primary" });
            _remote = new TaskCompletionSource<bool>(); _budget = new TaskCompletionSource<bool>(); _pending = null;
            Field("_initialiseDispatch", (Func<AccountProfile, Task>)(_ => _remote.Task));
            Field("_initialiseDelayDispatch", (Func<Task>)(() => _budget.Task));
        }
        private Task Start()
            => _pending = (Task)typeof(PlayerAccount).GetMethod("InitialiseInternalAsync", Hidden).Invoke(_account, null);
        [TearDown] public async Task After()
        {
            _budget.TrySetResult(true); _remote.TrySetResult(true);
            if (_pending != null) await _pending;
            Object.DestroyImmediate(_root); SettingsStore.OverrideForTests(_previous);
        }
        [Test] public async Task TimeoutFallbackPreservesActiveGuestAndPrimaryReturn()
        {
            var pending = Start(); _account.SignInAsGuest("Guest"); var guestId = _account.PlayerId;
            _budget.SetResult(true); await pending;
            Assert.AreEqual(guestId, _account.PlayerId); Assert.AreEqual("Guest", _account.DisplayName);
            Assert.AreEqual("owner-a", SettingsStore.Current.AccountPlayerId);
            _account.LeaveGuest(); Assert.AreEqual("owner-a", _account.PlayerId); Assert.AreEqual("Primary", _account.DisplayName);
        }
        [Test] public async Task FailedInitializationDoesNotReplaceAnotherAccount()
        {
            var pending = Start();
            var replacement = new AccountProfile { PlayerId = "owner-b", DisplayName = "Replacement" };
            Field("_profile", replacement); SettingsStore.Current.AccountPlayerId = "owner-b";
            SettingsStore.Current.PlayerName = "Replacement";
            _remote.SetException(new InvalidOperationException("Synthetic initialization failure")); await pending;
            Assert.AreEqual("owner-b", _account.PlayerId); Assert.AreEqual("Replacement", _account.DisplayName);
            Assert.AreEqual("owner-b", SettingsStore.Current.AccountPlayerId);
        }
        [Test] public async Task CurrentOwnerTimeoutStillUsesLocalProfile()
        {
            var pending = Start(); _budget.SetResult(true); await pending;
            Assert.AreEqual("owner-a", _account.PlayerId); Assert.AreEqual("Primary", _account.DisplayName);
            Assert.IsTrue(_account.IsLocalOnly); Assert.IsFalse(_account.IsSignedIn);
        }
    }
}
