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
    public sealed class SignInViewLifetimeTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private PlayerAccount _previousAccount, _account;
        private GameSettings _previousSettings;
        private GameObject _accountObject, _screenObject;
        private SignInScreen _screen;
        private readonly List<TaskCompletionSource<bool>> _answers = new List<TaskCompletionSource<bool>>();
        private int _closed;

        [UnitySetUp]
        public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-tp-profile"));
            _previousAccount = GameServices.Account; _previousSettings = SettingsStore.Current;
            SettingsStore.OverrideForTests(new GameSettings
            { AccountPlayerId = "signin-view-primary", AccountHasPassword = true, PlayerName = "Primary" });
            _answers.Clear(); _closed = 0;
            _accountObject = new GameObject("Dormant sign-in view account");
            _accountObject.SetActive(false);
            _account = _accountObject.AddComponent<PlayerAccount>();
            SetProfile("signin-view-primary");
            typeof(GameServices).GetProperty("Account").SetValue(null, _account);
            _screenObject = new GameObject("Installed sign-in lifetime screen");
            _screen = _screenObject.AddComponent<SignInScreen>();
            _screen.Install();
            typeof(SignInScreen).GetField("_credentialDispatch", Hidden).SetValue(_screen,
                (Func<bool, string, string, Task>)((creating, username, password) =>
                {
                    Assert.IsFalse(creating); Assert.IsNotEmpty(username); Assert.IsNotEmpty(password);
                    return Pending();
                }));
            typeof(SignInScreen).GetField("_googleDispatch", Hidden).SetValue(_screen,
                (Func<bool, Task>)(_ => Pending()));
            _screen.Closed += () => _closed++;
            _screen.Open();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            foreach (var answer in _answers) answer.TrySetResult(true);
            yield return null; yield return null;
            if (_screenObject != null) Object.Destroy(_screenObject);
            yield return null;
            if (_accountObject != null) Object.Destroy(_accountObject);
            typeof(GameServices).GetProperty("Account").SetValue(null, _previousAccount);
            SettingsStore.OverrideForTests(_previousSettings);
            yield return null;
            yield return PlayModeWorld.Reset();
        }

        private void SetProfile(string id) => typeof(PlayerAccount).GetField("_profile", Hidden).SetValue(_account,
            new AccountProfile { PlayerId = id, DisplayName = "Primary", Username = "primary.user" });
        private Task Pending()
        {
            var answer = new TaskCompletionSource<bool>(); _answers.Add(answer); return answer.Task;
        }
        private T Named<T>(string name) where T : Component
            => _screen.GetComponentsInChildren<T>(true).Single(component => component.name == name);
        private void BeginCredentials()
        {
            Named<InputField>("Username").text = "requested.user";
            Named<InputField>("Password").text = "Synthetic-only1!";
            Named<Button>("SubmitAccount").onClick.Invoke();
            Assert.AreEqual(1, _answers.Count);
            Assert.IsFalse(Named<InputField>("Username").interactable);
        }
        private void CloseAndReopen()
        {
            // Close is the operator called by non-boot Escape, including while Back is disabled.
            _screen.SendMessage("Close");
            Assert.IsFalse(_screen.IsOpen); Assert.AreEqual(1, _closed);
            _screen.Open();
            Named<InputField>("Username").text = "fresh.user";
            Named<InputField>("Password").text = "Fresh-only2!";
        }

        [UnityTest]
        public IEnumerator AReopenedFormAllowsEditingAndExitWithoutStartingAnotherPendingAuthentication()
        {
            BeginCredentials(); CloseAndReopen();
            yield return null;
            Assert.IsTrue(Named<InputField>("Username").interactable, "Reopened fields inherited the hidden request's lock.");
            Assert.IsTrue(Named<InputField>("Password").interactable);
            Assert.IsTrue(((Button)typeof(SignInScreen).GetField("_back", Hidden).GetValue(_screen)).interactable);
            Assert.IsFalse(Named<Button>("SubmitAccount").interactable, "Reopening allowed a second overlapping SDK request.");
            Assert.IsFalse(((Button)typeof(SignInScreen).GetField("_guest", Hidden).GetValue(_screen)).interactable);
            _screen.SendMessage("GuestPressed");
            Assert.IsFalse(_account.IsGuest, "A reopened form changed account ownership while authentication was pending.");
            Named<Button>("SubmitAccount").onClick.Invoke();
            Assert.AreEqual(1, _answers.Count);
            _answers[0].SetResult(true);
            yield return null; yield return null;
            Assert.IsTrue(_screen.IsOpen); Assert.IsTrue(Named<Button>("SubmitAccount").interactable);
        }

        [UnityTest]
        public IEnumerator AHiddenCredentialsSuccessCannotCloseTheReopenedForm()
        {
            BeginCredentials(); CloseAndReopen();
            _answers[0].SetResult(true);
            yield return null; yield return null;
            Assert.IsTrue(_screen.IsOpen, "An obsolete successful sign-in closed the fresh form.");
            Assert.AreEqual(1, _closed); Assert.AreEqual("fresh.user", Named<InputField>("Username").text);
            Assert.AreEqual("Fresh-only2!", Named<InputField>("Password").text);
        }

        [UnityTest]
        public IEnumerator AHiddenCredentialsFailureCannotMarkTheReopenedCredentialsAsWrong()
        {
            BeginCredentials(); CloseAndReopen();
            _answers[0].SetException(new InvalidOperationException("Invalid credentials"));
            yield return null; yield return null;
            Assert.IsTrue(_screen.IsOpen); Assert.AreEqual(1, _closed);
            Assert.IsEmpty(Named<Text>("UsernameFault").text, "An old reply marked the new credentials as invalid.");
            Assert.IsEmpty(Named<Text>("AccountStatus").text);
            Assert.IsTrue(Named<Button>("SubmitAccount").interactable);
        }

        [UnityTest]
        public IEnumerator AHiddenGoogleSuccessCannotCloseTheReopenedFormOrStartAnotherBrowserFlow()
        {
            _screen.SendMessage("GooglePressed");
            Assert.AreEqual(1, _answers.Count);
            CloseAndReopen();
            _screen.SendMessage("GooglePressed");
            Assert.AreEqual(1, _answers.Count, "Reopening dispatched another browser authentication flow.");
            _answers[0].SetResult(true);
            yield return null; yield return null;
            Assert.IsTrue(_screen.IsOpen); Assert.AreEqual(1, _closed);
            Assert.AreEqual("fresh.user", Named<InputField>("Username").text);
        }

        [UnityTest]
        public IEnumerator TheCurrentSignInStillClosesAfterItsAccountIdentityChanges()
        {
            BeginCredentials();
            SetProfile("signin-view-new-account"); // A valid sign-in changes ID on the same account component.
            _answers[0].SetResult(true);
            yield return null; yield return null;
            Assert.IsFalse(_screen.IsOpen); Assert.AreEqual(1, _closed);
            Assert.IsTrue(SettingsStore.Current.AccountChoiceMade);
        }

        [UnityTest]
        public IEnumerator TheCurrentCredentialFailureStillDisplaysItsFaultAndAllowsRetry()
        {
            BeginCredentials();
            _answers[0].SetException(new InvalidOperationException("Invalid credentials"));
            yield return null; yield return null;
            Assert.IsTrue(_screen.IsOpen); Assert.AreEqual(0, _closed);
            Assert.AreEqual("TUMP ID or password invalid.", Named<Text>("UsernameFault").text);
            Assert.IsTrue(Named<Button>("SubmitAccount").interactable);
        }
    }
}
