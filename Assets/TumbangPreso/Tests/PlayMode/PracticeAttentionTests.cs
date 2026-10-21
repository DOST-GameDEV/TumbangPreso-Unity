using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.InputLayer;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class PracticeAttentionTests
    {
        private GameObject _owner;
        private Canvas _canvas;
        private EventSystem _events;
        private HubPracticePopup _popup;
        private HubButton _tutorial;
        private InputDeviceKind _oldKind;
        private static readonly MethodInfo SetDevice = typeof(LastInputDevice).GetMethod("Set",
            BindingFlags.Static | BindingFlags.NonPublic);
        private GameObject Shade => _tutorial.transform.Find("Body/Art/Hover").gameObject;

        [UnitySetUp]
        public IEnumerator Before()
        {
            _oldKind = LastInputDevice.Current;
            yield return PlayModeWorld.Reset();
            SetDevice.Invoke(null, new object[] { InputDeviceKind.KeyboardMouse });
            _owner = new GameObject("Practice attention review");
            _canvas = OwnerUiLayout.Canvas(_owner.transform, "PracticeAttentionCanvas", 100);
            _events = UiInputModule.Ensure();
            var root = HubKit.Stretch(HubKit.Rect(_canvas.transform, "PracticePopup"));
            _popup = root.gameObject.AddComponent<HubPracticePopup>();
            typeof(HubScreen).GetProperty("Root").SetValue(_popup, root);
            _popup.Build();
            _tutorial = (HubButton)_popup.FirstFocus;
            yield return null;
            _events.SetSelectedGameObject(null);
        }

        [UnityTearDown]
        public IEnumerator After()
        {
            _events.SetSelectedGameObject(null);
            Object.Destroy(_canvas.gameObject); Object.Destroy(_owner);
            yield return PlayModeWorld.Reset();
            SetDevice.Invoke(null, new object[] { _oldKind });
        }

        [Test]
        public void AutomaticTutorialFocusDoesNotPretendThePointerIsHovering()
        {
            _events.SetSelectedGameObject(_tutorial.gameObject);
            Assert.That(_events.currentSelectedGameObject, Is.SameAs(_tutorial.gameObject));
            Assert.That(Shade.activeSelf, Is.False);
            Assert.That(_tutorial.Shape.RingWidth, Is.Zero);
            Assert.That(_tutorial.Shape.OutlineBoost, Is.Zero);
        }

        [Test]
        public void AutomaticallySelectedTutorialStillRespondsToPointerEnterAndExit()
        {
            _events.SetSelectedGameObject(_tutorial.gameObject);
            var pointer = new PointerEventData(_events);
            _tutorial.OnPointerEnter(pointer);
            Assert.That(Shade.activeSelf, Is.True);
            Assert.That(_tutorial.Shape.RingWidth, Is.Zero);
            _tutorial.OnPointerExit(pointer);
            Assert.That(Shade.activeSelf, Is.False);
            Assert.That(_events.currentSelectedGameObject, Is.SameAs(_tutorial.gameObject));
        }

        [Test]
        public void PointerSelectionDoesNotLeaveAStickyHoverAfterExit()
        {
            var pointer = new PointerEventData(_events);
            _tutorial.OnPointerEnter(pointer);
            _events.SetSelectedGameObject(_tutorial.gameObject, pointer);
            _tutorial.OnPointerExit(pointer);
            Assert.That(Shade.activeSelf, Is.False);
            Assert.That(_tutorial.Shape.RingWidth, Is.Zero);
        }

        [Test]
        public void KeyboardNavigationStillHighlightsAndDescribesTheFocusedCard()
        {
            _events.SetSelectedGameObject(_tutorial.gameObject,
                new AxisEventData(_events) { moveDir = MoveDirection.Down });
            Assert.That(Shade.activeSelf, Is.True);
            Assert.That(_tutorial.Shape.RingWidth, Is.GreaterThan(0f));
        }

        [Test]
        public void GamepadOpeningStillHasAVisibleFirstFocus()
        {
            SetDevice.Invoke(null, new object[] { InputDeviceKind.Gamepad });
            _events.SetSelectedGameObject(_tutorial.gameObject);
            Assert.That(Shade.activeSelf, Is.True);
            Assert.That(_tutorial.Shape.RingWidth, Is.GreaterThan(0f));
        }

        [Test]
        public void MouseUseAfterGamepadFocusDoesNotLeaveAStickyCard()
        {
            SetDevice.Invoke(null, new object[] { InputDeviceKind.Gamepad });
            _events.SetSelectedGameObject(_tutorial.gameObject);
            Assert.That(Shade.activeSelf, Is.True);
            var pointer = new PointerEventData(_events) { button = PointerEventData.InputButton.Left };
            _tutorial.OnPointerEnter(pointer);
            _tutorial.OnPointerDown(pointer);
            _tutorial.OnPointerUp(pointer);
            _tutorial.OnPointerExit(pointer);
            Assert.That(Shade.activeSelf, Is.False);
            Assert.That(_tutorial.Shape.RingWidth, Is.Zero);
        }

        [Test]
        public void OtherHubButtonsRetainTheirExistingAutomaticFocusStyle()
        {
            var button = HubKit.Button(_canvas.transform, "OrdinaryAction", "ACTION", Color.white, () => { });
            _events.SetSelectedGameObject(button.gameObject);
            Assert.That(button.Shape.RingWidth, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator ActualPopupKeepsUnhoveredAndHoveredStatesAtBothWindowShapes()
        {
            _events.SetSelectedGameObject(_tutorial.gameObject);
            yield return TumpUiCapture.Capture("Practice-auto-960x540", _canvas, 960, 540, false);
            Assert.That(Shade.activeSelf, Is.False);
            yield return TumpUiCapture.Capture("Practice-auto-1600x680", _canvas, 1600, 680, false);
            var pointer = new PointerEventData(_events);
            _tutorial.OnPointerEnter(pointer);
            Assert.That(Shade.activeSelf, Is.True);
            yield return TumpUiCapture.Capture("Practice-hover-960x540", _canvas, 960, 540, false);
            _tutorial.OnPointerExit(pointer);
            Assert.That(Shade.activeSelf, Is.False);
        }
    }
}
