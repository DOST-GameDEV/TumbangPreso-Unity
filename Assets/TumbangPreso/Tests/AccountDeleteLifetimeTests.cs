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
    public sealed class AccountDeleteLifetimeTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private PlayerAccount _account;
        private GameSettings _previous;
        private TaskCompletionSource<bool> _initialise, _cloud, _auth;
        private Task _pending;
        private int _authCalls, _cloudCalls, _restarts;
        private void Field(string name, object value) => typeof(PlayerAccount).GetField(name, Hidden).SetValue(_account, value);
        private void Signed(bool value) => typeof(PlayerAccount).GetProperty("IsSignedIn").SetValue(_account, value);
        private void ReplaceOwner()
        {
            Field("_profile", new AccountProfile { PlayerId = "owner-b", DisplayName = "Replacement" });
            SettingsStore.Current.AccountPlayerId = "owner-b"; SettingsStore.Current.PlayerName = "Replacement";
        }
        [SetUp] public void Before()
        {
            _previous = SettingsStore.Current;
            SettingsStore.OverrideForTests(new GameSettings { AccountPlayerId = "owner-a", PlayerName = "Original", PlayerToken = "retained-token", AccountHasPassword = true });
            _root = new GameObject("Dormant account deletion test"); _root.SetActive(false);
            _account = _root.AddComponent<PlayerAccount>();
            Field("_profile", new AccountProfile { PlayerId = "owner-a", DisplayName = "Original" }); Signed(true);
            _initialise = new TaskCompletionSource<bool>(); _cloud = new TaskCompletionSource<bool>(); _auth = new TaskCompletionSource<bool>();
            _authCalls = _cloudCalls = _restarts = 0; _pending = null;
            Field("_initialiseTask", _initialise.Task);
            Field("_deleteCloudDispatch", (Func<Task>)(() => { _cloudCalls++; return _cloud.Task; }));
            Field("_deleteAuthDispatch", (Func<string, Task>)(owner => { _authCalls++; return _auth.Task; }));
            Field("_deleteRestartDispatch", (Func<Task>)(() => { _restarts++; return Task.CompletedTask; }));
        }
        [TearDown] public async Task After()
        {
            _initialise.TrySetResult(true); _cloud.TrySetResult(true); _auth.TrySetResult(true);
            if (_pending != null)
                try { await _pending; } catch (OperationCanceledException) { }
            Object.DestroyImmediate(_root); SettingsStore.OverrideForTests(_previous);
        }
        private void AssertPrimaryUntouched()
        {
            Assert.AreEqual("owner-a", SettingsStore.Current.AccountPlayerId);
            Assert.AreEqual("Original", SettingsStore.Current.PlayerName);
            Assert.AreEqual("retained-token", SettingsStore.Current.PlayerToken);
            Assert.IsTrue(SettingsStore.Current.AccountHasPassword); Assert.AreEqual(0, _restarts);
        }
        [Test] public async Task GuestDuringInitializationCannotReachDeletion()
        {
            _pending = _account.DeleteAsync(); _account.SignInAsGuest("Guest"); _initialise.SetResult(true);
            Assert.ThrowsAsync(Is.InstanceOf<OperationCanceledException>(), async () => await _pending);
            Assert.AreEqual(0, _cloudCalls); Assert.AreEqual(0, _authCalls); AssertPrimaryUntouched();
            await Task.CompletedTask;
        }
        [Test] public async Task ReplacementDuringCloudClearCannotReachAuthDeletion()
        {
            _initialise.SetResult(true); _auth.SetResult(true); _pending = _account.DeleteAsync(); ReplaceOwner(); _cloud.SetResult(true);
            Assert.ThrowsAsync(Is.InstanceOf<OperationCanceledException>(), async () => await _pending);
            Assert.AreEqual(0, _authCalls); Assert.AreEqual("owner-b", SettingsStore.Current.AccountPlayerId);
            Assert.AreEqual("Replacement", SettingsStore.Current.PlayerName); Assert.AreEqual(0, _restarts);
            await Task.CompletedTask;
        }
        [Test] public async Task GuestDuringAuthDeletionCannotClearPrimarySettings()
        {
            _initialise.SetResult(true); _cloud.SetResult(true); _pending = _account.DeleteAsync();
            Assert.AreEqual(1, _authCalls); _account.SignInAsGuest("Guest"); _auth.SetResult(true);
            Assert.ThrowsAsync(Is.InstanceOf<OperationCanceledException>(), async () => await _pending); AssertPrimaryUntouched();
            await Task.CompletedTask;
        }
        [Test] public async Task GuestRoundTripRetiresPendingCloudDeletion()
        {
            _initialise.SetResult(true); _auth.SetResult(true); _pending = _account.DeleteAsync();
            _account.SignInAsGuest("Guest"); _account.LeaveGuest(); Signed(true); _cloud.SetResult(true);
            Assert.ThrowsAsync(Is.InstanceOf<OperationCanceledException>(), async () => await _pending);
            Assert.AreEqual(0, _authCalls); AssertPrimaryUntouched(); await Task.CompletedTask;
        }
        [Test] public async Task CurrentOwnerDeletionStillClearsAndRestarts()
        {
            _initialise.SetResult(true); _cloud.SetResult(true); _auth.SetResult(true);
            _pending = _account.DeleteAsync(); await _pending;
            Assert.AreEqual(1, _cloudCalls); Assert.AreEqual(1, _authCalls); Assert.AreEqual(1, _restarts);
            Assert.AreEqual("", SettingsStore.Current.AccountPlayerId); Assert.IsFalse(SettingsStore.Current.AccountHasPassword);
            Assert.AreNotEqual("retained-token", SettingsStore.Current.PlayerToken);
        }
        [Test] public async Task LocalOnlyDeletionDoesNotDispatchRemoteOperations()
        {
            Signed(false); _initialise.SetResult(true); _pending = _account.DeleteAsync(); await _pending;
            Assert.AreEqual(0, _cloudCalls); Assert.AreEqual(0, _authCalls); Assert.AreEqual(1, _restarts);
            Assert.AreEqual("", SettingsStore.Current.AccountPlayerId);
        }
        [TestCase(false)]
        [TestCase(true)]
        public async Task GuestDuringRestartRetiresDeletionCompletion(bool returnToPrimary)
        {
            var restart = new TaskCompletionSource<bool>();
            Field("_deleteRestartDispatch", (Func<Task>)(() => restart.Task));
            _initialise.SetResult(true); _cloud.SetResult(true); _auth.SetResult(true);
            _pending = _account.DeleteAsync(); Assert.IsFalse(_pending.IsCompleted);
            _account.SignInAsGuest("Guest");
            if (returnToPrimary) { _account.LeaveGuest(); Signed(true); }
            restart.SetResult(true);
            Assert.ThrowsAsync(Is.InstanceOf<OperationCanceledException>(), async () => await _pending);
            await Task.CompletedTask;
        }
    }
}
