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
    public sealed class AccountSaveOwnershipTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private PlayerAccount _account;
        private GameSettings _previous;
        private TaskCompletionSource<string> _answer;
        private Task _pending;

        private static AccountProfile Profile(string id, string name) => new AccountProfile { PlayerId = id, DisplayName = name, Bio = "Local bio" };
        private void Field(string name, object value) => typeof(PlayerAccount).GetField(name, Hidden).SetValue(_account, value);
        private AccountProfile Current => (AccountProfile)typeof(PlayerAccount).GetField("_profile", Hidden).GetValue(_account);
        private void Signed(bool signed) => typeof(PlayerAccount).GetProperty("IsSignedIn").SetValue(_account, signed);
        private void Guest(bool guest) => typeof(PlayerAccount).GetProperty("IsGuest").SetValue(_account, guest);
        private Task Save() => (Task)typeof(PlayerAccount).GetMethod("SaveCloudProfileAsync", Hidden).Invoke(_account, null);
        private static string Answer(string id, string name) => JsonUtility.ToJson(new Envelope { profile = JsonUtility.ToJson(Profile(id, name)) });
        [Serializable] private sealed class Envelope { public string profile; }

        [SetUp] public void Before()
        {
            _previous = SettingsStore.Current;
            SettingsStore.OverrideForTests(new GameSettings { AccountPlayerId = "owner-a", PlayerName = "Original" });
            _root = new GameObject("Dormant account save test"); _root.SetActive(false);
            _account = _root.AddComponent<PlayerAccount>();
            Field("_profile", Profile("owner-a", "Original")); Signed(true); Guest(false);
            _answer = new TaskCompletionSource<string>();
            Field("_saveProfileDispatch", (Func<string, Task<string>>)(_ => _answer.Task));
            _pending = null;
        }
        [TearDown] public async Task After()
        {
            _answer.TrySetResult("{}");
            if (_pending != null) await _pending;
            Object.DestroyImmediate(_root);
            SettingsStore.OverrideForTests(_previous);
        }

        [Test] public async Task DelayedSaveCannotReplaceAnotherAccount()
        {
            _pending = Save(); Assert.IsFalse(_pending.IsCompleted);
            var replacement = Profile("owner-b", "Replacement"); Field("_profile", replacement);
            SettingsStore.Current.AccountPlayerId = "owner-b"; SettingsStore.Current.PlayerName = "Replacement";
            _answer.SetResult(Answer("owner-a", "Old canonical")); await _pending;
            Assert.AreSame(replacement, Current);
            Assert.AreEqual("owner-b", SettingsStore.Current.AccountPlayerId);
            Assert.AreEqual("Replacement", SettingsStore.Current.PlayerName);
        }
        [Test] public async Task DelayedSaveCannotReplaceAGuest()
        {
            _pending = Save(); var guest = Profile("guest-local", "Guest"); Field("_profile", guest); Guest(true); Signed(false);
            _answer.SetResult(Answer("owner-a", "Old canonical")); await _pending;
            Assert.AreSame(guest, Current); Assert.AreEqual("Guest", Current.DisplayName);
            Assert.AreEqual("owner-a", SettingsStore.Current.AccountPlayerId);
        }
        [Test] public async Task DelayedSaveCannotOverwriteANewerLocalEdit()
        {
            _pending = Save(); Current.DisplayName = "New edit";
            _answer.SetResult(Answer("owner-a", "Old canonical")); await _pending;
            Assert.AreEqual("New edit", Current.DisplayName);
        }
        [Test] public async Task ForeignCanonicalOwnerIsRejected()
        {
            _pending = Save(); var original = Current;
            _answer.SetResult(Answer("owner-b", "Foreign")); await _pending;
            Assert.AreSame(original, Current); Assert.AreEqual("owner-a", SettingsStore.Current.AccountPlayerId);
        }
        [Test] public async Task CurrentOwnerCanonicalSaveStillApplies()
        {
            _pending = Save(); _answer.SetResult(Answer("owner-a", "Canonical")); await _pending;
            Assert.AreEqual("Canonical", Current.DisplayName);
            Assert.AreEqual("owner-a", SettingsStore.Current.AccountPlayerId);
            Assert.AreEqual("Canonical", SettingsStore.Current.PlayerName);
        }
    }
}
