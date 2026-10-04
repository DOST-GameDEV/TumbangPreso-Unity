using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.Tests
{
    public sealed class CareerReportAcknowledgementTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _accountObject, _careerObject;
        private PlayerAccount _account, _previousAccount;
        private CareerStore _career, _previousCareer;

        [SetUp]
        public void Before()
        {
            _previousAccount = GameServices.Account;
            _previousCareer = CareerStore.Instance;
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-tp-profile"),
                "Reporting checks require an isolated profile.");
            _accountObject = new GameObject("Dormant report account");
            _accountObject.SetActive(false);
            _account = _accountObject.AddComponent<PlayerAccount>();
            // Awake remains dormant: no authentication initialization or SDK access.
            SetOwner("report-owner-a");
            SetEligibility(true, false);
            typeof(GameServices).GetProperty("Account").SetValue(null, _account);
            _careerObject = new GameObject("Dormant report store");
            _careerObject.SetActive(false);
            _career = _careerObject.AddComponent<CareerStore>();
        }

        [TearDown]
        public void After()
        {
            if (_careerObject != null) Object.DestroyImmediate(_careerObject);
            if (_accountObject != null) Object.DestroyImmediate(_accountObject);
            typeof(GameServices).GetProperty("Account").SetValue(null, _previousAccount);
            typeof(CareerStore).GetProperty("Instance").SetValue(null, _previousCareer);
        }

        private void SetOwner(string id)
            => typeof(PlayerAccount).GetField("_profile", Private).SetValue(_account,
                new AccountProfile { PlayerId = id });

        private void SetEligibility(bool signedIn, bool guest)
        {
            typeof(PlayerAccount).GetProperty("IsSignedIn").SetValue(_account, signedIn);
            typeof(PlayerAccount).GetProperty("IsGuest").SetValue(_account, guest);
        }

        private Task<bool> Submit(Func<Task<string>> dispatch)
            => (Task<bool>)typeof(CareerStore).GetMethod("ReportForOwnerAsync", Private)
                .Invoke(_career, new object[] { _account, "report-owner-a", dispatch });

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public async Task OfflineAndGuestReportsNeverDispatch(bool signedIn, bool guest)
        {
            SetEligibility(signedIn, guest);
            int calls = 0;
            bool accepted = await Submit(() => { calls++; return Task.FromResult("{\"applied\":true}"); });
            Assert.IsFalse(accepted);
            Assert.AreEqual(0, calls, "An ineligible reporter reached the dispatch boundary.");
        }

        [Test]
        public async Task AnOlderOwnerCannotStartAReport()
        {
            SetOwner("report-owner-b");
            int calls = 0;
            Assert.IsFalse(await Submit(() => { calls++; return Task.FromResult("{\"applied\":true}"); }));
            Assert.AreEqual(0, calls);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public async Task BlankSubjectsAreRefusedByThePublicApi(string subject)
        {
            Assert.IsFalse(await _career.ReportAsync(subject, ReportReason.Other));
        }

        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("null", false)]
        [TestCase("{}", false)]
        [TestCase("{\"applied\":false}", false)]
        [TestCase("{\"ok\":true}", false)]
        [TestCase("{\"applied\":\"true\"}", false)]
        [TestCase("{\"applied\":1}", false)]
        [TestCase("{\"profile\":\"{}\",\"applied\":true}", true)]
        public async Task OnlyTheEndpointBooleanAcknowledgementConfirmsDelivery(string output, bool accepted)
        {
            var pending = new TaskCompletionSource<string>();
            int calls = 0;
            string previousStatus = _career.Status;
            var task = Submit(() => { calls++; return pending.Task; });
            Assert.AreEqual(1, calls);
            Assert.IsFalse(task.IsCompleted, "Dispatch was mistaken for report acknowledgement.");
            pending.SetResult(output);
            Assert.AreEqual(accepted, await task);
            Assert.AreEqual(previousStatus, _career.Status, "Reporting changed unrelated career status.");
        }

        [TestCase("owner")]
        [TestCase("guest")]
        [TestCase("offline")]
        [TestCase("replaced-account")]
        [TestCase("destroyed-store")]
        public async Task ObsoleteReportCompletionCannotAcknowledgeCurrentUi(string change)
        {
            var pending = new TaskCompletionSource<string>();
            var task = Submit(() => pending.Task);
            Assert.IsFalse(task.IsCompleted);
            switch (change)
            {
                case "owner": SetOwner("report-owner-b"); break;
                case "guest": SetEligibility(true, true); break;
                case "offline": SetEligibility(false, false); break;
                case "replaced-account": typeof(GameServices).GetProperty("Account").SetValue(null, _previousAccount); break;
                case "destroyed-store": Object.DestroyImmediate(_careerObject); break;
            }
            pending.SetResult("{\"applied\":true}");
            Assert.IsFalse(await task, "A report owned by an obsolete account/context was acknowledged.");
        }

        [Test]
        public async Task FailedDispatchReturnsFalseAndAllowsALaterAttempt()
        {
            LogAssert.Expect(LogType.Warning, "[Career] report not delivered: controlled report refusal");
            Assert.IsFalse(await Submit(() => Task.FromException<string>(
                new InvalidOperationException("controlled report refusal"))));
            Assert.IsTrue(await Submit(() => Task.FromResult("{\"applied\":true}")),
                "A failed request blocked a later valid report.");
        }
    }
}
