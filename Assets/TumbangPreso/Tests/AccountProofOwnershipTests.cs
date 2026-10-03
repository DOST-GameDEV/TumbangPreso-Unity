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
    public sealed class AccountProofOwnershipTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private PlayerAccount _account;
        private GameSettings _previous;
        private readonly List<TaskCompletionSource<string>> _answers = new();
        private readonly List<Task<string>> _pending = new();
        private void Field(string name, object value) => typeof(PlayerAccount).GetField(name, Hidden).SetValue(_account, value);
        private void Signed(bool value) => typeof(PlayerAccount).GetProperty("IsSignedIn").SetValue(_account, value);
        private void ReplaceOwner() => Field("_profile", new AccountProfile { PlayerId = "owner-b", DisplayName = "Replacement" });
        private void CacheProof()
        {
            Field("_proof", "primary-proof"); Field("_proofExpiresUtc", DateTime.UtcNow.AddHours(2));
            // Original source has no owner field; the baseline still exercises its getter.
            typeof(PlayerAccount).GetField("_proofOwner", Hidden)?.SetValue(_account, "owner-a");
        }
        private Task<string> Request()
        {
            var task = _account.EnsureHandleProofAsync(); _pending.Add(task); return task;
        }
        private static string Answer(string proof) => JsonUtility.ToJson(new Reply
        {
            proof = proof, expires = DateTime.UtcNow.AddHours(2).ToString("O")
        });
        [Serializable] private sealed class Reply { public string proof; public string expires; }
        [SetUp] public void Before()
        {
            _previous = SettingsStore.Current;
            SettingsStore.OverrideForTests(new GameSettings { AccountPlayerId = "owner-a", PlayerName = "Original" });
            _root = new GameObject("Dormant account proof test"); _root.SetActive(false);
            _account = _root.AddComponent<PlayerAccount>();
            Field("_profile", new AccountProfile { PlayerId = "owner-a", DisplayName = "Original" }); Signed(true);
            _answers.Clear(); _pending.Clear();
            Field("_proofDispatch", (Func<Task<string>>)(() =>
            {
                var answer = new TaskCompletionSource<string>(); _answers.Add(answer); return answer.Task;
            }));
        }
        [TearDown] public async Task After()
        {
            foreach (var answer in _answers) answer.TrySetResult("{}");
            await Task.WhenAll(_pending); Object.DestroyImmediate(_root);
            SettingsStore.OverrideForTests(_previous);
        }
        [Test] public void CachedPrimaryProofIsHiddenFromGuest()
        {
            CacheProof(); _account.SignInAsGuest("Guest"); Assert.AreEqual("", _account.HandleProof);
        }
        [Test] public void CachedPrimaryProofIsHiddenFromReplacementOwner()
        {
            CacheProof(); ReplaceOwner(); Assert.AreEqual("", _account.HandleProof);
        }
        [Test] public async Task CurrentOwnerCachedProofStillReusesWithoutDispatch()
        {
            CacheProof(); Assert.AreEqual("primary-proof", await Request()); Assert.AreEqual(0, _answers.Count);
        }
        [Test] public async Task ReplacementOwnerRequestsItsOwnProof()
        {
            CacheProof(); ReplaceOwner(); var task = Request(); Assert.IsFalse(task.IsCompleted);
            _answers[0].SetResult(Answer("replacement-proof"));
            Assert.AreEqual("replacement-proof", await task); Assert.AreEqual("replacement-proof", _account.HandleProof);
        }
        [Test] public async Task DelayedAttestCannotPublishToGuest()
        {
            var task = Request(); _account.SignInAsGuest("Guest"); _answers[0].SetResult(Answer("old-proof"));
            Assert.AreEqual("", await task); Assert.AreEqual("", _account.HandleProof);
        }
        [Test] public async Task DelayedAttestCannotPublishToReplacementOwner()
        {
            var task = Request(); ReplaceOwner(); _answers[0].SetResult(Answer("old-proof"));
            Assert.AreEqual("", await task); Assert.AreEqual("", _account.HandleProof);
        }
        [Test] public async Task GuestRoundTripRetiresThePendingAttest()
        {
            var task = Request(); _account.SignInAsGuest("Guest"); _account.LeaveGuest(); Signed(true);
            _answers[0].SetResult(Answer("old-proof")); Assert.AreEqual("", await task); Assert.AreEqual("", _account.HandleProof);
        }
        [Test] public async Task EarlierFailureCannotClearNewerAcceptedProof()
        {
            var first = Request(); var second = Request();
            _answers[1].SetResult(Answer("current-proof")); Assert.AreEqual("current-proof", await second);
            _answers[0].SetException(new InvalidOperationException("Old local test failure")); await first;
            Assert.AreEqual("current-proof", _account.HandleProof);
        }
        [Test] public async Task CurrentOwnerAttestStillPublishesProof()
        {
            var task = Request(); _answers[0].SetResult(Answer("current-proof"));
            Assert.AreEqual("current-proof", await task); Assert.AreEqual("current-proof", _account.HandleProof);
        }
    }
}
