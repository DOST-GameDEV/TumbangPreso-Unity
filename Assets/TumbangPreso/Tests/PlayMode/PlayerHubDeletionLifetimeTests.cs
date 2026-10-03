using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class PlayerHubDeletionLifetimeTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private PlayerAccount _previousAccount, _account;
        private CareerStore _previousCareer;
        private SocialStore _previousSocial;
        private GameSettings _previousSettings;
        private GameObject _accountObject, _hubObject;
        private PlayerHub _hub;
        private TaskCompletionSource<bool> _answer;
        private static void Service(string name, object value) => typeof(GameServices).GetProperty(name).SetValue(null, value);
        private void Field(string name, object value) => typeof(PlayerAccount).GetField(name, Hidden).SetValue(_account, value);
        private string Notice => _hub.GetComponentsInChildren<Text>(true).Single(t => t.name == "HubNotice").text;
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-tp-profile"));
            _previousAccount = GameServices.Account; _previousCareer = GameServices.Career;
            _previousSocial = GameServices.Social; _previousSettings = SettingsStore.Current;
            SettingsStore.OverrideForTests(new GameSettings { AccountPlayerId = "delete-ui-owner", PlayerName = "Primary" });
            _accountObject = new GameObject("Dormant deletion UI account"); _accountObject.SetActive(false);
            _account = _accountObject.AddComponent<PlayerAccount>();
            Field("_profile", new AccountProfile { PlayerId = "delete-ui-owner", DisplayName = "Primary" });
            typeof(PlayerAccount).GetProperty("IsSignedIn").SetValue(_account, true);
            Field("_initialiseTask", Task.CompletedTask); _answer = new TaskCompletionSource<bool>();
            Field("_deleteCloudDispatch", (Func<Task>)(() => Task.CompletedTask));
            Field("_deleteAuthDispatch", (Func<string, Task>)(_ => _answer.Task));
            Field("_deleteRestartDispatch", (Func<Task>)(() => Task.CompletedTask));
            Service("Account", _account); Service("Career", null); Service("Social", null);
            _hubObject = new GameObject("Installed deletion lifetime hub"); _hub = _hubObject.AddComponent<PlayerHub>();
            _hub.Install(); _hub.Open(true); yield return null;
        }
        [UnityTearDown] public IEnumerator After()
        {
            _answer.TrySetResult(true); yield return null; yield return null;
            Service("Account", _account);
            if (_hubObject != null) Object.Destroy(_hubObject); yield return null;
            if (_accountObject != null) Object.Destroy(_accountObject);
            Service("Account", _previousAccount); Service("Career", _previousCareer); Service("Social", _previousSocial);
            SettingsStore.OverrideForTests(_previousSettings); yield return null; yield return PlayModeWorld.Reset();
        }
        private void StartDelete()
        {
            // Execute the exact operator used by the already-confirmed Delete control.
            typeof(PlayerHub).GetField("_deleteArmed", Hidden).SetValue(_hub, true);
            typeof(PlayerHub).GetMethod("DeleteAccount", Hidden).Invoke(_hub, null);
            Assert.AreEqual("Deleting...", Notice);
        }
        [UnityTest] public IEnumerator CompletedDeletionCannotRepaintAReopenedSheet()
        {
            StartDelete(); _hub.GetComponentsInChildren<Button>(true).Single(b => b.name == "ClosePlayerHub").onClick.Invoke();
            _hub.Open(true);
            _answer.SetResult(true); yield return null; yield return null;
            Assert.IsTrue(_hub.IsOpen); Assert.AreNotEqual("Account deleted.", Notice);
        }
        [UnityTest] public IEnumerator CancelledOwnerDeletionCannotPaintGuestErrors()
        {
            StartDelete(); _account.SignInAsGuest("Guest"); yield return null;
            string before = Notice; Assert.AreNotEqual("Deleting...", before);
            _answer.SetResult(true); yield return null; yield return null;
            Assert.AreEqual(before, Notice); Assert.AreEqual("Guest", _account.DisplayName);
        }
        [UnityTest] public IEnumerator ActiveDeletionStillReportsSuccessAfterIdentityChanges()
        {
            StartDelete(); _answer.SetResult(true); yield return null; yield return null;
            Assert.AreEqual("", SettingsStore.Current.AccountPlayerId);
            Assert.AreEqual("Account deleted.", Notice); Assert.IsTrue(_hub.IsOpen);
        }
        [UnityTest] public IEnumerator GuestRoundTripCannotPaintTheRetiredDeletionError()
        {
            StartDelete(); _account.SignInAsGuest("Guest"); yield return null;
            _account.LeaveGuest(); yield return null;
            string before = Notice; Assert.AreNotEqual("Deleting...", before);
            _answer.SetResult(true); yield return null; yield return null;
            Assert.AreEqual(before, Notice); Assert.AreEqual("Primary", _account.DisplayName);
        }
    }
}
