using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class TouchLayoutHudLifecycleTests
    {
        private const string LayoutKey = "tumbangpreso.touchlayout";
        private GameObject _root;
        private TumpTouchLayoutView _view;
        private bool _force, _customising, _hadLayout;
        private string _layout;
        private object _layoutCache;
        private static readonly FieldInfo LayoutCache = typeof(TouchLayoutStore)
            .GetField("_file", BindingFlags.Static | BindingFlags.NonPublic);

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _force = TouchHud.ForceVisible; _customising = TouchButton.Customising;
            _hadLayout = PlayerPrefs.HasKey(LayoutKey); _layout = PlayerPrefs.GetString(LayoutKey, "");
            _layoutCache = LayoutCache.GetValue(null); LayoutCache.SetValue(null, null);
            TouchHud.ForceVisible = false; TouchButton.Customising = false;
            TouchInput.ReleaseAll(); TouchInput.Active = false;
            _root = new GameObject("Touch editor lifecycle owner");
            _view = _root.AddComponent<TumpTouchLayoutView>();
        }

        [UnityTearDown] public IEnumerator After()
        {
            if (_view != null) Object.DestroyImmediate(_view);
            yield return PlayModeWorld.Reset();
            if (_hadLayout) PlayerPrefs.SetString(LayoutKey, _layout); else PlayerPrefs.DeleteKey(LayoutKey);
            PlayerPrefs.Save(); LayoutCache.SetValue(null, _layoutCache);
            TouchHud.ForceVisible = _force; TouchButton.Customising = _customising;
            TouchInput.ReleaseAll(); TouchInput.Active = false;
        }

        private void Cancel()
        {
            var canvas = (Canvas)typeof(TumpTouchLayoutView)
                .GetField("_canvas", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_view);
            var cancel = canvas.GetComponentsInChildren<UnityEngine.UI.Button>()
                .Single(button => button.name == "CancelTouchLayout");
            Assert.IsTrue(cancel.IsInteractable()); cancel.onClick.Invoke();
            Assert.IsFalse(_view.IsOpen);
        }

        [UnityTest] public IEnumerator CancelThenImmediateReopenUsesFreshControlsInsteadOfTheRetiringPreview()
        {
            try
            {
                _view.Open(_root.transform, () => { }); var retiring = TouchHud.Instance;
                Assert.IsNotNull(retiring); Cancel();
                _view.Open(_root.transform, () => { }); var replacement = TouchHud.Instance;
                Assert.AreNotSame(retiring, replacement, "Immediate reopen borrowed the preview scheduled for destruction.");
                Assert.IsNotNull(replacement); Assert.IsTrue(replacement.Canvas.gameObject.activeInHierarchy);
                Cancel();
            }
            finally
            {
                // Close before the deferred HUD destruction even on the original assertion failure.
                if (_view != null) Object.DestroyImmediate(_view);
            }
            yield return null;
        }

        [UnityTest] public IEnumerator CancellingTheEditorPreservesItsBorrowedGameplayHud()
        {
            TouchHud.ForceVisible = true; var borrowed = TouchHud.Install();
            var parent = borrowed.Canvas.transform.parent;
            _view.Open(_root.transform, () => { }); Cancel(); yield return null;
            Assert.AreSame(borrowed, TouchHud.Instance); Assert.IsTrue(borrowed.isActiveAndEnabled);
            Assert.AreSame(parent, borrowed.Canvas.transform.parent); Assert.IsFalse(TouchButton.Customising);
        }

        [UnityTest] public IEnumerator RetiringAnOldHudDoesNotReleaseTheReplacementsHeldInput()
        {
            TouchHud.ForceVisible = true; var old = TouchHud.Install();
            var replacement = new GameObject("Replacement touch lifecycle owner").AddComponent<TouchHud>();
            Assert.AreSame(replacement, TouchHud.Instance);
            TouchInput.Set(Verb.Sprint, true); TouchInput.Move = Vector2.right;
            Object.Destroy(old.gameObject); yield return null;
            Assert.AreSame(replacement, TouchHud.Instance); Assert.IsTrue(TouchInput.Active);
            Assert.IsTrue(TouchInput.Pressed(Verb.Sprint), "An old HUD teardown released the replacement's held input.");
            Assert.AreEqual(Vector2.right, TouchInput.Move);
        }
    }
}
