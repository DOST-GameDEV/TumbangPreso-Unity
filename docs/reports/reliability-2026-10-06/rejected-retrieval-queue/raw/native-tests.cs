using System;
using System.Collections.Generic;
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
    public sealed class CareerFlushOwnerLifetimeTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string OriginalOwner = "controlled-flush-owner-a";
        private const string ReplacementOwner = "controlled-flush-owner-b";
        private const string ReplacementStatus = "Replacement owner status";
        private const string Ack = "{\"verdict\":\"pending\"}";
        private GameObject _accountObject, _careerObject;
        private PlayerAccount _account, _previousAccount;
        private CareerStore _career, _previousCareer;
        private object _originalCache;

        [SetUp] public void Before()
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-tp-profile"), "Use an isolated native profile.");
            _previousAccount = GameServices.Account; _previousCareer = CareerStore.Instance;
            _accountObject = new GameObject("Dormant controlled flush account"); _accountObject.SetActive(false);
            _account = _accountObject.AddComponent<PlayerAccount>(); SetAccount(OriginalOwner);
            typeof(PlayerAccount).GetProperty("IsSignedIn").SetValue(_account, true);
            typeof(PlayerAccount).GetProperty("IsGuest").SetValue(_account, false);
            typeof(GameServices).GetProperty("Account").SetValue(null, _account);
            _careerObject = new GameObject("Dormant controlled flush store"); _careerObject.SetActive(false);
            _career = _careerObject.AddComponent<CareerStore>(); _originalCache = Cache;
            Seed(_originalCache, OriginalOwner, "original-match", "original-witness");
            Assert.AreEqual(MatchRecordRules.SubmitVerdict.Ok, MatchRecordRules.Submittable(Queue(_originalCache)[0], OriginalOwner));
        }
        [TearDown] public void After()
        {
            if (_careerObject != null) Object.DestroyImmediate(_careerObject);
            if (_accountObject != null) Object.DestroyImmediate(_accountObject);
            typeof(GameServices).GetProperty("Account").SetValue(null, _previousAccount);
            typeof(CareerStore).GetProperty("Instance").SetValue(null, _previousCareer);
        }
        private object Cache => typeof(CareerStore).GetField("_cache", Hidden).GetValue(_career);
        private static List<MatchRecord> Queue(object cache) => (List<MatchRecord>)cache.GetType().GetField("Queue").GetValue(cache);
        private static List<string> Witnesses(object cache) => (List<string>)cache.GetType().GetField("QueueWitness").GetValue(cache);
        private void SetAccount(string owner) => typeof(PlayerAccount).GetField("_profile", Hidden).SetValue(_account, new AccountProfile { PlayerId = owner });
        private static void Seed(object cache, string owner, string id, string witness)
        {
            cache.GetType().GetField("OwnerId").SetValue(cache, owner);
            Queue(cache).Clear(); Witnesses(cache).Clear();
            Queue(cache).Add(new MatchRecord { MatchId = id, Players = new[] { new PlayerMatchStats { PlayerId = owner, IsBot = false } } });
            Witnesses(cache).Add(witness);
        }
        private object Replace(int records)
        {
            var replacement = Activator.CreateInstance(Cache.GetType(), true);
            replacement.GetType().GetField("OwnerId").SetValue(replacement, ReplacementOwner);
            if (records != 0) Seed(replacement, ReplacementOwner, "replacement-match", "replacement-witness");
            typeof(CareerStore).GetField("_cache", Hidden).SetValue(_career, replacement);
            SetAccount(ReplacementOwner);
            typeof(CareerStore).GetProperty("Status").SetValue(_career, ReplacementStatus);
            return replacement;
        }
        private Task Start(Func<string, object, Task<string>> dispatch)
        {
            var method = typeof(CareerStore).GetMethod("FlushWithDispatchAsync", Hidden);
            Assert.IsNotNull(method, "The shipping-core dispatch extraction must exist before running this fixture.");
            return (Task)method.Invoke(_career, new object[] { dispatch });
        }
        private static void Request(string script, object parameters, string expectedMatch, string expectedWitness)
        {
            Assert.AreEqual(CareerStore.ScriptName, script);
            var type = parameters.GetType();
            Assert.AreEqual("submit", type.GetProperty("action").GetValue(parameters));
            var record = JsonUtility.FromJson<MatchRecord>((string)type.GetProperty("record").GetValue(parameters));
            Assert.AreEqual(expectedMatch, record.MatchId);
            Assert.AreEqual(expectedWitness, type.GetProperty("witness").GetValue(parameters));
        }
        private void Released() => Assert.IsFalse((bool)typeof(CareerStore).GetField("_flushing", Hidden).GetValue(_career));

        private static MatchRecord ValidNext() => new MatchRecord
        {
            MatchId = "next-valid-match", Players = new[] { new PlayerMatchStats { PlayerId = OriginalOwner, IsBot = false } }
        };
        [Test] public async Task PermanentlyInvalidRetrievalHeadDoesNotBlockTheNextMatchOrEraseLocalHistory()
        {
            var bad = Queue(_originalCache)[0]; bad.Players[0].Retrievals = 1;
            var history = (List<MatchRecord>)_originalCache.GetType().GetField("History").GetValue(_originalCache);
            history.Add(bad);
            Queue(_originalCache).Add(ValidNext()); Witnesses(_originalCache).Add("next-witness");
            var dispatched = new List<string>();
            await Start((script, args) =>
            {
                var record = JsonUtility.FromJson<MatchRecord>((string)args.GetType().GetProperty("record").GetValue(args));
                dispatched.Add(record.MatchId);
                if (record.Players[0].Retrievals > record.Players[0].Throws)
                    return Task.FromException<string>(new InvalidOperationException("Controlled permanent retrieval-count refusal"));
                Request(script, args, "next-valid-match", "next-witness");
                return Task.FromResult(Ack);
            });
            CollectionAssert.AreEqual(new[] { "next-valid-match" }, dispatched);
            Assert.IsEmpty(Queue(_originalCache)); Assert.IsEmpty(Witnesses(_originalCache));
            Assert.IsTrue(history.Contains(bad)); Assert.AreEqual(1, bad.Players[0].Retrievals);
            Assert.AreEqual(0, bad.Players[0].Throws); Released();
        }
        [Test] public async Task TransientFailureKeepsTheValidHeadAndItsFollowingMatchForRetry()
        {
            Queue(_originalCache).Add(ValidNext()); Witnesses(_originalCache).Add("next-witness");
            int calls = 0;
            await Start((script, args) =>
            {
                calls++; Request(script, args, "original-match", "original-witness");
                return Task.FromException<string>(new InvalidOperationException("Controlled temporary transport failure"));
            });
            Assert.AreEqual(1, calls); Assert.AreEqual(2, Queue(_originalCache).Count);
            CollectionAssert.AreEqual(new[] { "original-witness", "next-witness" }, Witnesses(_originalCache));
            Assert.AreEqual("original-match", Queue(_originalCache)[0].MatchId); Released();
        }

        [Test] public async Task ObsoleteFailureCannotOverwriteReplacementOwnerStatus()
        {
            var pending = new TaskCompletionSource<string>(); int calls = 0;
            var operation = Start((script, args) => { calls++; Request(script, args, "original-match", "original-witness"); return pending.Task; });
            Assert.AreEqual(1, calls); Assert.IsFalse(operation.IsCompleted);
            var replacement = Replace(1); pending.SetException(new InvalidOperationException("Controlled old-owner failure"));
            await operation;
            Assert.AreEqual(ReplacementStatus, _career.Status);
            Assert.AreSame(replacement, Cache); Assert.AreEqual(1, Queue(replacement).Count);
            CollectionAssert.AreEqual(new[] { "replacement-witness" }, Witnesses(replacement));
            Assert.AreEqual(1, Queue(_originalCache).Count); Released();
        }
        [TestCase(0), TestCase(1)] public async Task OwnerReplacementFromCompletionCannotContinueOrClaimItsQueue(int records)
        {
            var pending = new TaskCompletionSource<string>(); int calls = 0; object replacement = null;
            _career.Changed += () => { if (replacement == null) replacement = Replace(records); };
            var operation = Start((script, args) => { calls++; return calls == 1 ? pending.Task : Task.FromResult(Ack); });
            Assert.AreEqual(1, calls); Assert.IsFalse(operation.IsCompleted); pending.SetResult(Ack); await operation;
            Assert.IsNotNull(replacement); Assert.AreSame(replacement, Cache);
            Assert.AreEqual(1, calls, "The old flush dispatched the replacement owner's queue.");
            Assert.AreEqual(records, Queue(replacement).Count); Assert.AreEqual(ReplacementStatus, _career.Status);
            Assert.IsEmpty(Queue(_originalCache)); Released();
        }
        [Test] public async Task ObsoleteSuccessCannotAcknowledgeReplacementOwnerData()
        {
            var pending = new TaskCompletionSource<string>(); int calls = 0;
            var operation = Start((script, args) => { calls++; return pending.Task; });
            Assert.AreEqual(1, calls); Assert.IsFalse(operation.IsCompleted);
            var replacement = Replace(1); pending.SetResult(Ack); await operation;
            Assert.AreSame(replacement, Cache); Assert.AreEqual(ReplacementStatus, _career.Status);
            Assert.AreEqual(1, Queue(_originalCache).Count); Assert.AreEqual(1, Queue(replacement).Count);
            Assert.AreEqual(1, calls); Released();
        }
        [Test] public async Task SameOwnerFailureRetainsItsRecordAndWitnessForRetry()
        {
            var pending = new TaskCompletionSource<string>(); int calls = 0;
            var operation = Start((script, args) => { calls++; Request(script, args, "original-match", "original-witness"); return pending.Task; });
            Assert.IsFalse(operation.IsCompleted); pending.SetException(new InvalidOperationException("Controlled same-owner failure")); await operation;
            Assert.AreEqual(1, calls); Assert.AreSame(_originalCache, Cache); Assert.AreEqual(1, Queue(Cache).Count);
            CollectionAssert.AreEqual(new[] { "original-witness" }, Witnesses(Cache));
            Assert.AreEqual("1 match(es) waiting to upload", _career.Status); Released();
        }
        [Test] public async Task SameOwnerSuccessAcknowledgesOnlyItsQueuedRecord()
        {
            var pending = new TaskCompletionSource<string>(); int calls = 0;
            var operation = Start((script, args) => { calls++; Request(script, args, "original-match", "original-witness"); return pending.Task; });
            Assert.IsFalse(operation.IsCompleted); pending.SetResult(Ack); await operation;
            Assert.AreEqual(1, calls); Assert.AreSame(_originalCache, Cache);
            Assert.IsEmpty(Queue(Cache)); Assert.IsEmpty(Witnesses(Cache));
            Assert.AreEqual("Career saved", _career.Status); Assert.AreEqual("pending", _career.LastVerdict);
            Assert.AreEqual("original-match", _career.LastVerdictMatchId); Released();
        }
    }
}
