using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class SocialRefreshOrderingTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _accountObject, _socialObject;
        private PlayerAccount _account, _previousAccount;
        private SocialStore _social, _previousSocial;
        private readonly Dictionary<string, byte[]> _files = new Dictionary<string, byte[]>();
        private static string Path => System.IO.Path.Combine(ProfilePaths.Root, "social.json");

        [SetUp] public void Before()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-tp-profile"), "Use an isolated profile.");
            _previousAccount = GameServices.Account; _previousSocial = SocialStore.Instance;
            foreach (string suffix in new[] { "", SafeStore.BackupSuffix, SafeStore.TempSuffix })
                _files[Path + suffix] = File.Exists(Path + suffix) ? File.ReadAllBytes(Path + suffix) : null;
            _accountObject = new GameObject("Dormant social order account"); _accountObject.SetActive(false);
            _account = _accountObject.AddComponent<PlayerAccount>();
            typeof(PlayerAccount).GetField("_profile", Hidden).SetValue(_account, new AccountProfile { PlayerId = "order-owner" });
            typeof(PlayerAccount).GetProperty("IsSignedIn").SetValue(_account, true);
            typeof(PlayerAccount).GetProperty("IsGuest").SetValue(_account, false);
            typeof(GameServices).GetProperty("Account").SetValue(null, _account);
            _socialObject = new GameObject("Dormant social order store"); _socialObject.SetActive(false);
            _social = _socialObject.AddComponent<SocialStore>();
            _social.List.Friends.Clear(); _social.List.Incoming.Clear(); _social.List.Outgoing.Clear(); _social.List.Blocked.Clear();
        }
        [TearDown] public void After()
        {
            if (_socialObject != null) Object.DestroyImmediate(_socialObject);
            if (_accountObject != null) Object.DestroyImmediate(_accountObject);
            typeof(GameServices).GetProperty("Account").SetValue(null, _previousAccount);
            typeof(SocialStore).GetProperty("Instance").SetValue(null, _previousSocial);
            foreach (var file in _files)
                if (file.Value == null) { if (File.Exists(file.Key)) File.Delete(file.Key); }
                else File.WriteAllBytes(file.Key, file.Value);
        }
        private Task Refresh(Func<string, object, Task<string>> call)
            => (Task)typeof(SocialStore).GetMethod("RefreshAsync", Hidden).Invoke(_social, new object[] { call });
        private Task<bool> Post(Func<string, object, Task<string>> call)
            => (Task<bool>)typeof(SocialStore).GetMethod("PostAsync", Hidden).Invoke(_social, new object[] { new { action = "accept", playerId = "new-friend" }, call });
        [Serializable] private sealed class Envelope { public string list; }
        private static string Reply(params string[] friends)
        {
            var list = new SocialList();
            foreach (string id in friends) list.Friends.Add(new FriendRef { PlayerId = id });
            return JsonUtility.ToJson(new Envelope { list = JsonUtility.ToJson(list) });
        }
        private void Released()
        {
            Assert.IsFalse((bool)typeof(SocialStore).GetField("_loading", Hidden).GetValue(_social));
            Assert.IsFalse((bool)typeof(SocialStore).GetField("_writing", Hidden).GetValue(_social));
        }

        [Test] public async Task OlderRefreshCannotEraseAcknowledgedFriend()
        {
            var stale = new TaskCompletionSource<string>();
            Task refresh = Refresh((script, args) => stale.Task);
            Assert.IsTrue(await Post((script, args) => Task.FromResult(Reply("new-friend"))));
            stale.SetResult(Reply()); await refresh;
            Assert.IsTrue(SocialRules.IsFriend(_social.List, "new-friend")); Released();
        }
        [Test] public async Task OlderRefreshCannotRestoreRemovedFriend()
        {
            _social.List.Friends.Add(new FriendRef { PlayerId = "old-friend" });
            var stale = new TaskCompletionSource<string>();
            Task refresh = Refresh((script, args) => stale.Task);
            Assert.IsTrue(await Post((script, args) => Task.FromResult(Reply())));
            stale.SetResult(Reply("old-friend")); await refresh;
            Assert.IsFalse(SocialRules.IsFriend(_social.List, "old-friend")); Released();
        }
        [Test] public async Task OrdinaryRefreshStillAdoptsServerList()
        {
            await Refresh((script, args) => Task.FromResult(Reply("server-friend")));
            Assert.IsTrue(SocialRules.IsFriend(_social.List, "server-friend")); Released();
        }
        [Test] public async Task OrdinaryWriteStillAdoptsAcknowledgement()
        {
            Assert.IsTrue(await Post((script, args) => Task.FromResult(Reply("new-friend"))));
            Assert.IsTrue(SocialRules.IsFriend(_social.List, "new-friend")); Released();
        }
        [Test] public async Task RefreshDuringWriteIsDeferredWithoutDispatch()
        {
            await Refresh((script, args) => Task.FromResult(Reply()));
            var pending = new TaskCompletionSource<string>(); int loads = 0;
            Task<bool> write = Post((script, args) => pending.Task);
            await Refresh((script, args) => { loads++; return Task.FromResult(Reply()); });
            Assert.AreEqual(0, loads);
            Assert.IsTrue((bool)typeof(SocialStore).GetField("_refreshPending", Hidden).GetValue(_social));
            pending.SetResult(Reply("new-friend")); Assert.IsTrue(await write); Released();
        }
        [Test] public async Task FailedWriteAndOlderLoadKeepCacheAndScheduleFreshRead()
        {
            _social.List.Friends.Add(new FriendRef { PlayerId = "cached-friend" });
            var stale = new TaskCompletionSource<string>();
            Task refresh = Refresh((script, args) => stale.Task);
            Assert.IsFalse(await Post((script, args) => Task.FromException<string>(new InvalidOperationException("Controlled write failure"))));
            stale.SetResult(Reply()); await refresh;
            Assert.IsTrue(SocialRules.IsFriend(_social.List, "cached-friend"));
            Assert.IsTrue((bool)typeof(SocialStore).GetField("_refreshPending", Hidden).GetValue(_social));
            await Refresh((script, args) => Task.FromResult(Reply("server-friend")));
            Assert.IsTrue(SocialRules.IsFriend(_social.List, "server-friend")); Released();
        }
    }
}
