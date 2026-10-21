using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class SettingsDecisionFocusTests
    {
        private GameObject _root;
        private TumpSettingsView _view;
        private Canvas _canvas;
        private bool _motion;
        private int _backs;
        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _motion = Settings.SettingsStore.Current.ReducedUiMotion;
            _root = new GameObject("Current settings decision focus");
            _view = _root.AddComponent<TumpSettingsView>(); _backs = 0;
            _view.Open(_root.transform, () => _backs++, null, null);
            _canvas = (Canvas)typeof(TumpSettingsView).GetField("_canvas", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_view);
            yield return null;
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_root != null) Object.Destroy(_root);
            yield return null;
            Settings.SettingsStore.Current.ReducedUiMotion = _motion;
            yield return PlayModeWorld.Reset();
        }
        private Button OpenDirtyDecision()
        {
            Settings.SettingsStore.Current.ReducedUiMotion = !_motion;
            _view.Session.Preview(); Assert.IsTrue(_view.Session.Dirty);
            _view.Back();
            return _canvas.GetComponentsInChildren<Button>().Single(button => button.name == "KeepEditing");
        }
        [UnityTest] public IEnumerator KeepEditingReturnsFocusToLiveSettingsAndKeepsUnsavedChanges()
        {
            var keep = OpenDirtyDecision(); keep.Select();
            Assert.AreSame(keep.gameObject, EventSystem.current.currentSelectedGameObject);
            keep.onClick.Invoke();
            var selected = EventSystem.current.currentSelectedGameObject;
            Assert.IsNotNull(selected); Assert.IsTrue(selected.activeInHierarchy, "KEEP EDITING left focus on an inactive dialog control.");
            Assert.IsTrue(selected.transform.IsChildOf(_canvas.transform));
            Assert.IsTrue(_view.Session.Dirty); Assert.AreEqual(0, _backs);
            yield return null;
            Assert.IsTrue(EventSystem.current.currentSelectedGameObject.activeInHierarchy);
        }
        [UnityTest] public IEnumerator ClosingTheDecisionPreservesAnotherLiveScreensSelection()
        {
            var keep = OpenDirtyDecision();
            var other = OwnerUiLayout.Canvas(_root.transform, "Other live settings overlay", 900);
            var button = new GameObject("Other active action", typeof(RectTransform), typeof(Button));
            button.transform.SetParent(other.transform, false); ScreenFocus.Install(other.gameObject).Rebuild(); button.GetComponent<Button>().Select();
            keep.onClick.Invoke();
            Assert.AreSame(button, EventSystem.current.currentSelectedGameObject);
            Assert.IsTrue(_view.Session.Dirty); Assert.AreEqual(0, _backs);
            yield return null;
            Assert.AreSame(button, EventSystem.current.currentSelectedGameObject);
        }
        [UnityTest] public IEnumerator CleanBackRetainsItsExistingCallbackWithoutOpeningTheDecision()
        {
            Assert.IsFalse(_view.Session.Dirty); _view.Back();
            Assert.AreEqual(1, _backs); Assert.IsFalse(_canvas.gameObject.activeSelf);
            Assert.IsFalse(_canvas.GetComponentsInChildren<Button>().Any(button => button.name == "KeepEditing"));
            yield return null;
        }
    }
}
