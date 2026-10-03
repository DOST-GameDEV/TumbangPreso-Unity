using System;
using System.Collections;
using System.Collections.Generic;
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
    public sealed class PlayerHubProfileSaveLifetimeTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private PlayerAccount _previousAccount, _account;
        private CareerStore _previousCareer;
        private SocialStore _previousSocial;
        private GameSettings _previousSettings;
        private GameObject _accountObject, _hubObject;
        private PlayerHub _hub;
        private readonly List<TaskCompletionSource<string>> _answers = new List<TaskCompletionSource<string>>();

        [UnitySetUp]
        public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-tp-profile"));
            _previousAccount = GameServices.Account;
            _previousCareer = GameServices.Career;
            _previousSocial = GameServices.Social;
            _previousSettings = SettingsStore.Current;
            SettingsStore.OverrideForTests(new GameSettings { AccountPlayerId = "profile-save-owner", PlayerName = "Original" });
            _answers.Clear();
            _accountObject = new GameObject("Dormant profile UI save account");
            _accountObject.SetActive(false);
            _account = _accountObject.AddComponent<PlayerAccount>();
            typeof(PlayerAccount).GetField("_profile", Hidden).SetValue(_account,
                new AccountProfile { PlayerId = "profile-save-owner", DisplayName = "Original" });
            typeof(PlayerAccount).GetProperty("IsSignedIn").SetValue(_account, true);
            typeof(PlayerAccount).GetField("_updateNameDispatch", Hidden).SetValue(_account,
                (Func<string, Task<string>>)(_ =>
                {
                    var answer = new TaskCompletionSource<string>(); _answers.Add(answer); return answer.Task;
                }));
            typeof(PlayerAccount).GetField("_saveProfileDispatch", Hidden).SetValue(_account,
                (Func<string, Task<string>>)(_ => Task.FromResult("{}")));
            Service("Account", _account); Service("Career", null); Service("Social", null);
            _hubObject = new GameObject("Installed profile save lifetime hub");
            _hub = _hubObject.AddComponent<PlayerHub>();
            _hub.Install(); _hub.OpenTab(PlayerHub.Door.Profile);
            yield return null;
            Assert.IsTrue(_hub.IsOpen);
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            if (_account != null) typeof(PlayerAccount).GetProperty("IsSignedIn").SetValue(_account, false);
            foreach (var answer in _answers) answer.TrySetResult("Ignored#1234");
            yield return null; yield return null;
            Service("Account", _account); // Always detach the hub from its subscribed fixture account.
            if (_hubObject != null) Object.Destroy(_hubObject);
            yield return null;
            if (_accountObject != null) Object.Destroy(_accountObject);
            Service("Account", _previousAccount); Service("Career", _previousCareer); Service("Social", _previousSocial);
            SettingsStore.OverrideForTests(_previousSettings);
            yield return null;
            yield return PlayModeWorld.Reset();
        }

        private static void Service(string name, object value) => typeof(GameServices).GetProperty(name).SetValue(null, value);
        private InputField NameField => _hub.GetComponentsInChildren<InputField>(true)
            .Single(field => field.name == "PlayerNameEdit" && field.gameObject.activeInHierarchy);
        private Button Footer => _hub.GetComponentsInChildren<Button>(true).Single(button => button.name == "HubPrimary");
        private string Notice => _hub.GetComponentsInChildren<Text>(true).Single(text => text.name == "HubNotice").text;
        private bool Saving => (bool)typeof(PlayerHub).GetField("_ownerSaving", Hidden).GetValue(_hub);
        private Dictionary<string, string> Draft => (Dictionary<string, string>)typeof(PlayerHub).GetField("_ownerDraft", Hidden).GetValue(_hub);
        private void StartSave(string name)
        {
            NameField.text = name;
            Assert.IsTrue(Footer.interactable, "The current profile's save control is unavailable.");
            Footer.onClick.Invoke();
            Assert.IsTrue(Saving); Assert.IsFalse(NameField.interactable);
        }
        private void CloseAndReopen()
        {
            _hub.GetComponentsInChildren<Button>(true).Single(button => button.name == "ClosePlayerHub").onClick.Invoke();
            Assert.IsFalse(_hub.IsOpen);
            _hub.OpenTab(PlayerHub.Door.Profile);
        }

        [UnityTest]
        public IEnumerator ChangingToAGuestReleasesThePreviousOwnersSaveControls()
        {
            StartSave("Requested"); Assert.AreEqual(1, _answers.Count);
            _account.SignInAsGuest("TournamentGuest");
            yield return null;
            Assert.IsFalse(Saving, "The guest inherited the primary account's pending save state.");
            Assert.IsTrue(NameField.interactable); Assert.IsTrue(Footer.interactable);
        }

        [UnityTest]
        public IEnumerator AnOldSaveCompletionCannotClearTheCurrentGuestDraft()
        {
            StartSave("Requested");
            _account.SignInAsGuest("TournamentGuest");
            yield return null;
            NameField.text = "Guest draft";
            _answers[0].SetResult("Requested#1234");
            yield return null; yield return null;
            Assert.AreEqual("Guest draft", NameField.text, "The retired primary save rebuilt the guest's edited field.");
            Assert.AreEqual("Guest draft", Draft["PlayerNameEdit"], "The retired primary save erased the guest's unsaved draft.");
            Assert.AreEqual("Unsaved profile changes.", Notice);
        }

        [UnityTest]
        public IEnumerator ClosingAndReopeningAllowsANewSaveWhoseBusyStateSurvivesTheOldCompletion()
        {
            StartSave("First requested");
            CloseAndReopen();
            yield return null;
            StartSave("Second requested");
            Assert.AreEqual(2, _answers.Count);
            _answers[0].SetResult("First_requested#1234");
            yield return null; yield return null;
            Assert.IsTrue(Saving, "An older save released the newer save's busy state.");
            Assert.IsFalse(NameField.interactable); Assert.IsFalse(Footer.interactable);
            _answers[1].SetResult("Second_requested#1234");
            yield return null; yield return null;
            Assert.IsFalse(Saving); Assert.IsTrue(NameField.interactable);
            Assert.AreEqual("Saved.", Notice);
        }

        [UnityTest]
        public IEnumerator TheCurrentSaveStillClearsItsOwnDraftAndReleasesControlsAfterAccountNotification()
        {
            StartSave("Requested");
            _answers[0].SetResult("Requested#1234");
            yield return null; yield return null;
            Assert.AreEqual("Requested", _account.DisplayName);
            Assert.IsEmpty(Draft); Assert.IsFalse(Saving);
            Assert.IsTrue(NameField.interactable); Assert.IsTrue(Footer.interactable);
            Assert.AreEqual("Saved.", Notice);
        }

        [UnityTest]
        public IEnumerator AProfileWithoutAnAccountStillSavesItsLocalName()
        {
            Service("Account", null); _hub.SendMessage("OnDataChanged");
            yield return null;
            NameField.text = "Local profile";
            Footer.onClick.Invoke();
            yield return null;
            Assert.AreEqual(0, _answers.Count);
            Assert.AreEqual("Local profile", SettingsStore.Current.PlayerName);
            Assert.IsEmpty(Draft); Assert.IsFalse(Saving);
            Assert.IsTrue(Footer.interactable);
            Assert.AreEqual("Saved on this machine.", Notice);
            // Restore the subscribed account before hub destruction detaches from it.
            Service("Account", _account);
        }

        [UnityTest]
        public IEnumerator ChangingAccountIdentityRetiresThePreviousDeleteConfirmation()
        {
            _hub.Open(true);
            yield return null;
            _hub.GetComponentsInChildren<Button>(true)
                .Single(button => button.transform.parent.name == "Group_Delete account" && button.isActiveAndEnabled)
                .onClick.Invoke();
            yield return null;
            var delete = _hub.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "DeleteAccount" && button.isActiveAndEnabled);
            Assert.AreEqual("DELETE", delete.GetComponentInChildren<Text>().text);
            delete.onClick.Invoke(); // First press only arms the UI; never calls DeleteAsync.
            yield return null;
            Assert.IsTrue((bool)typeof(PlayerHub).GetField("_deleteArmed", Hidden).GetValue(_hub));
            Assert.AreEqual("PRESS AGAIN TO DELETE", _hub.GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "DeleteAccount" && button.isActiveAndEnabled)
                .GetComponentInChildren<Text>().text);
            _account.SignInAsGuest("TournamentGuest");
            yield return null;
            Assert.IsFalse((bool)typeof(PlayerHub).GetField("_deleteArmed", Hidden).GetValue(_hub),
                "The guest inherited another account's destructive confirmation.");
            Assert.IsFalse(_hub.GetComponentsInChildren<Button>(true)
                .Any(button => button.name == "DeleteAccount" && button.isActiveAndEnabled
                    && button.GetComponentInChildren<Text>().text == "PRESS AGAIN TO DELETE"));
            _account.LeaveGuest();
            yield return null;
            Assert.IsFalse((bool)typeof(PlayerHub).GetField("_deleteArmed", Hidden).GetValue(_hub));
            Assert.AreEqual(0, _answers.Count, "This confirmation-only route dispatched an account operation.");
        }
    }
}
