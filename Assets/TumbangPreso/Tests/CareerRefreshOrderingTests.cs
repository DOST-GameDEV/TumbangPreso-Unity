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
    public sealed class CareerRefreshOrderingTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _accountObject, _careerObject;
        private PlayerAccount _account, _previousAccount;
        private CareerStore _career, _previousCareer;
        private readonly Dictionary<string, byte[]> _files = new Dictionary<string, byte[]>();
        private static string Path => System.IO.Path.Combine(ProfilePaths.Root, "career.json");
        [Serializable] private sealed class ReplyEnvelope { public string profile; public string verdict; }

        [SetUp] public void Before()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-tp-profile"), "Use an isolated profile.");
            _previousAccount = GameServices.Account; _previousCareer = CareerStore.Instance;
            foreach (string suffix in new[] { "", SafeStore.BackupSuffix, SafeStore.TempSuffix })
                _files[Path + suffix] = File.Exists(Path + suffix) ? File.ReadAllBytes(Path + suffix) : null;
            _accountObject = new GameObject("Dormant career order account"); _accountObject.SetActive(false);
            _account = _accountObject.AddComponent<PlayerAccount>();
            typeof(PlayerAccount).GetField("_profile", Hidden).SetValue(_account, new AccountProfile { PlayerId = "career-order-owner" });
            typeof(PlayerAccount).GetProperty("IsSignedIn").SetValue(_account, true);
            typeof(PlayerAccount).GetProperty("IsGuest").SetValue(_account, false);
            typeof(GameServices).GetProperty("Account").SetValue(null, _account);
            _careerObject = new GameObject("Dormant career order store"); _careerObject.SetActive(false);
            _career = _careerObject.AddComponent<CareerStore>();
            Cache.GetType().GetField("OwnerId").SetValue(Cache, "career-order-owner");
            Cache.GetType().GetField("InMatchSinceUtc").SetValue(Cache, "");
            Queue.Clear(); Witnesses.Clear();
        }
        [TearDown] public void After()
        {
            if (_careerObject != null) Object.DestroyImmediate(_careerObject);
            if (_accountObject != null) Object.DestroyImmediate(_accountObject);
            typeof(GameServices).GetProperty("Account").SetValue(null, _previousAccount);
            typeof(CareerStore).GetProperty("Instance").SetValue(null, _previousCareer);
            foreach (var file in _files)
                if (file.Value == null) { if (File.Exists(file.Key)) File.Delete(file.Key); }
                else File.WriteAllBytes(file.Key, file.Value);
        }
        private object Cache => typeof(CareerStore).GetField("_cache", Hidden).GetValue(_career);
        private List<MatchRecord> Queue => (List<MatchRecord>)Cache.GetType().GetField("Queue").GetValue(Cache);
        private List<string> Witnesses => (List<string>)Cache.GetType().GetField("QueueWitness").GetValue(Cache);
        private Task Refresh(Func<string, object, Task<string>> call)
            => (Task)typeof(CareerStore).GetMethod("RefreshWithDispatchAsync", Hidden).Invoke(_career, new object[] { call });
        private Task Flush(Func<string, object, Task<string>> call)
            => (Task)typeof(CareerStore).GetMethod("FlushWithDispatchAsync", Hidden).Invoke(_career, new object[] { call });
        private void Seed()
        {
            Queue.Add(new MatchRecord { MatchId = "earned-match", Online = true, Players = new[] {
                new PlayerMatchStats { PlayerId = "career-order-owner", IsBot = false } } });
            Witnesses.Add("earned-witness");
        }
        private static string Reply(int xp, string verdict = null)
            => JsonUtility.ToJson(new ReplyEnvelope { profile = JsonUtility.ToJson(new PlayerProfile { PlayerId = "career-order-owner", Xp = xp }), verdict = verdict });
        [Test] public async Task OlderRefreshCannotEraseAcknowledgedCareer()
        {
            var stale = new TaskCompletionSource<string>();
            Task refresh = Refresh((script, args) => stale.Task);
            Seed(); await Flush((script, args) => Task.FromResult(Reply(420, "pending")));
            Assert.IsEmpty(Queue); Assert.IsEmpty(Witnesses);
            stale.SetResult(Reply(77)); await refresh;
            Assert.AreEqual(420, _career.Profile.Xp);
        }
        [Test] public async Task OrdinaryRefreshStillAdoptsServerCareer()
        {
            await Refresh((script, args) => Task.FromResult(Reply(77)));
            Assert.AreEqual(77, _career.Profile.Xp);
        }
        [Test] public async Task OrdinarySubmissionStillAcknowledgesCareer()
        {
            Seed(); await Flush((script, args) => Task.FromResult(Reply(420, "pending")));
            Assert.AreEqual(420, _career.Profile.Xp); Assert.IsEmpty(Queue); Assert.IsEmpty(Witnesses);
        }
    }
}
