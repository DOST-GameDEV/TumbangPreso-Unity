using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class OwnerPasswordLifetimeTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private PlayerAccount _previousAccount, _account;
        private GameObject _accountObject, _ownerObject;
        private OwnerPasswordView _view, _replacement;
        private TaskCompletionSource<bool> _answer;
        private Task _saveTask;
        private int _closed;
        private string _closedNotice;

        [UnitySetUp]
        public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-tp-profile"));
            _previousAccount = GameServices.Account;
            _accountObject = new GameObject("Dormant password view account");
            _accountObject.SetActive(false);
            _account = _accountObject.AddComponent<PlayerAccount>();
            typeof(PlayerAccount).GetField("_profile", Hidden).SetValue(_account,
                new AccountProfile { PlayerId = "password-view-owner", DisplayName = "Primary" });
            typeof(GameServices).GetProperty("Account").SetValue(null, _account);
            _ownerObject = new GameObject("Password lifetime owner");
            _closed = 0; _closedNotice = ""; _saveTask = null;
            _view = OwnerPasswordView.Open(_ownerObject.transform, () =>
            {
                _closed++;
                _closedNotice = _view.GetComponentsInChildren<Text>(true)
                    .Single(text => text.name == "PasswordStatus").text;
            });
            _answer = new TaskCompletionSource<bool>();
            typeof(OwnerPasswordView).GetField("_changePasswordDispatch", Hidden).SetValue(_view,
                (Func<string, string, Task>)((current, next) =>
                {
                    Assert.AreEqual("synthetic-current", current);
                    Assert.AreEqual("Test-only1", next);
                    return _answer.Task;
                }));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            _answer?.TrySetResult(true);
            yield return null; yield return null;
            if (_saveTask != null && _saveTask.IsFaulted) _ = _saveTask.Exception;
            if (_ownerObject != null) Object.Destroy(_ownerObject);
            yield return null;
            if (_accountObject != null) Object.Destroy(_accountObject);
            typeof(GameServices).GetProperty("Account").SetValue(null, _previousAccount);
            yield return null;
            yield return PlayModeWorld.Reset();
        }

        private T Named<T>(string name) where T : Component
            => _view.GetComponentsInChildren<T>(true).Single(component => component.name == name);
        private void BeginSave()
        {
            Named<InputField>("CurrentPassword").text = "synthetic-current";
            Named<InputField>("NewPassword").text = "Test-only1";
            Named<InputField>("ConfirmNewPassword").text = "Test-only1";
            // This is the Task-returning operator bound to the Save button.
            // Observe it so baseline MissingReference failures remain visible.
            _saveTask = (Task)typeof(OwnerPasswordView).GetMethod("SaveAsync", Hidden).Invoke(_view, null);
            Assert.IsFalse(_saveTask.IsCompleted);
            Assert.IsFalse(Named<Button>("SavePassword").interactable);
        }
        private void AssertSaveTaskCompletedNormally()
        {
            Assert.IsTrue(_saveTask.IsCompleted, "A completed dispatch left its UI operator pending.");
            Assert.IsFalse(_saveTask.IsFaulted, _saveTask.Exception?.ToString());
        }
        private IEnumerator ClosedContinuation(bool failure)
        {
            BeginSave();
            Named<Button>("PasswordBack").onClick.Invoke();
            Assert.IsFalse(_view.IsOpen); Assert.AreEqual(1, _closed);
            _replacement = OwnerPasswordView.Open(_ownerObject.transform);
            yield return null;
            Assert.IsTrue(_view == null, "Cancel did not destroy its old password form.");
            if (failure) _answer.SetException(new InvalidOperationException("wrong password"));
            else _answer.SetResult(true);
            yield return null; yield return null;
            AssertSaveTaskCompletedNormally();
            Assert.AreEqual(1, _closed, "The retired request invoked its close callback again.");
            Assert.IsTrue(_replacement.IsOpen, "The retired request closed the new password form.");
        }

        [UnityTest]
        public IEnumerator ACancelledFormsLateSuccessLeavesDestroyedControlsAlone()
            => ClosedContinuation(false);

        [UnityTest]
        public IEnumerator ACancelledFormsLateFailureLeavesDestroyedControlsAlone()
            => ClosedContinuation(true);

        [UnityTest]
        public IEnumerator CurrentFormSuccessStillAcknowledgesAndClosesOnce()
        {
            BeginSave(); _answer.SetResult(true);
            yield return null; yield return null;
            AssertSaveTaskCompletedNormally();
            Assert.AreEqual(1, _closed); Assert.AreEqual("Password changed.", _closedNotice);
            Assert.IsTrue(_view == null);
        }

        [UnityTest]
        public IEnumerator CurrentFormFailureKeepsItsFieldFaultAndAllowsRetry()
        {
            BeginSave(); _answer.SetException(new InvalidOperationException("wrong password"));
            yield return null; yield return null;
            AssertSaveTaskCompletedNormally();
            Assert.AreEqual(0, _closed); Assert.IsTrue(_view.IsOpen);
            Assert.AreEqual("That is not your current password.", Named<Text>("CurrentPasswordFault").text);
            Assert.IsTrue(Named<Button>("SavePassword").interactable);
        }
    }
}
