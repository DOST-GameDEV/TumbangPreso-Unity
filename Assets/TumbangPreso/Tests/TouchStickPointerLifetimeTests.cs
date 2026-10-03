using NUnit.Framework;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TumbangPreso.Tests
{
    public sealed class TouchStickPointerLifetimeTests
    {
        private GameObject _root;
        private TouchStick _stick;
        private RectTransform _base, _knob;
        private EventSystem _events;
        private bool _active;
        private float _radius;
        [SetUp] public void Before()
        {
            _active = TouchInput.Active; TouchInput.ReleaseAll(); TouchInput.Active = true;
            // Prepared disabled canvas: coordinate conversion and public handlers only.
            // No raycast/module/render/physics step or physical pointer is supplied.
            _root = new GameObject("Stick pointer lifetime", typeof(RectTransform), typeof(Canvas));
            _root.GetComponent<Canvas>().enabled = false;
            _root.AddComponent<UnityEngine.UI.GraphicRaycaster>().enabled = false;
            _events = _root.AddComponent<EventSystem>(); _events.enabled = false;
            var body = new GameObject("Bound fixed stick", typeof(RectTransform), typeof(CanvasGroup));
            body.transform.SetParent(_root.transform, false); _base = (RectTransform)body.transform;
            _base.anchorMin = _base.anchorMax = _base.pivot = new Vector2(.5f, .5f);
            _base.sizeDelta = new Vector2(100, 100); _base.anchoredPosition = new Vector2(150, 150);
            var knob = new GameObject("Knob", typeof(RectTransform)); knob.transform.SetParent(body.transform, false);
            _knob = (RectTransform)knob.transform; _knob.anchorMin = _knob.anchorMax = _knob.pivot = new Vector2(.5f, .5f);
            _radius = 50; _stick = body.AddComponent<TouchStick>();
            _stick.Bind(_base, _knob, _radius, body.GetComponent<CanvasGroup>());
        }
        [TearDown] public void After()
        {
            Object.DestroyImmediate(_root); TouchInput.ReleaseAll(); TouchInput.Active = _active;
        }
        private PointerEventData At(int id, Vector2 offset)
            => new(_events) { pointerId = id, position = RectTransformUtility.WorldToScreenPoint(null, _base.TransformPoint(offset)) };
        private void Value(Vector2 expected)
        {
            Assert.LessOrEqual(Vector2.Distance(expected, _stick.Value), .0001f, "The stick published the wrong vector.");
            Assert.LessOrEqual(Vector2.Distance(expected, TouchInput.Move), .0001f, "The touch movement table differs from the stick.");
            Assert.LessOrEqual(Vector2.Distance(expected * _radius, _knob.anchoredPosition), .0001f, "The knob no longer follows the published vector.");
        }
        [Test] public void ASecondPointerCannotReplaceTheFirstPointersSteering()
        {
            _stick.OnPointerDown(At(101, new Vector2(40, 0))); Value(new Vector2(.8f, 0));
            _stick.OnPointerDown(At(202, new Vector2(0, 30)));
            Value(new Vector2(.8f, 0));
            _stick.OnDrag(At(202, new Vector2(0, 30)));
            Value(new Vector2(.8f, 0));
        }
        [Test] public void LiftingTheSecondPointerCannotZeroStillHeldSteering()
        {
            _stick.OnPointerDown(At(101, new Vector2(40, 0))); Value(new Vector2(.8f, 0));
            var second = At(202, new Vector2(20, 0)); _stick.OnPointerDown(second);
            // Isolate lift from the separately tested steering takeover defect.
            Vector2 before = _stick.Value; Assert.Greater(before.magnitude, .1f);
            _stick.OnPointerUp(second); Value(before);
        }
        [Test] public void TheOwningPointerCanDragThroughTheCentreAndContinueSteering()
        {
            _stick.OnPointerDown(At(101, new Vector2(40, 0))); Value(new Vector2(.8f, 0));
            _stick.OnDrag(At(101, Vector2.zero)); Value(Vector2.zero);
            _stick.OnDrag(At(101, new Vector2(0, 30))); Value(new Vector2(0, .6f));
        }
        [Test] public void TheOwningPointerStillReleasesMovementAndKnob()
        {
            var first = At(101, new Vector2(40, 0)); _stick.OnPointerDown(first); Value(new Vector2(.8f, 0));
            _stick.OnPointerUp(first); Value(Vector2.zero);
        }
        [Test] public void AFreshPointerSteersAfterThePreviousOwnerReleases()
        {
            var first = At(101, new Vector2(40, 0)); _stick.OnPointerDown(first); _stick.OnPointerUp(first); Value(Vector2.zero);
            var fresh = At(202, new Vector2(10, 30)); _stick.OnPointerDown(fresh); Value(new Vector2(.2f, .6f));
            _stick.OnPointerUp(fresh); Value(Vector2.zero);
        }
        [Test] public void TheManagedDisableCallbackReleasesOwnershipBeforeAFreshPointer()
        {
            _stick.OnPointerDown(At(101, new Vector2(40, 0))); Value(new Vector2(.8f, 0));
            // Managed callback-body acceptance, not an EditMode/native player transition.
            typeof(TouchStick).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(_stick, null);
            Value(Vector2.zero);
            var fresh = At(202, new Vector2(0, 30)); _stick.OnPointerDown(fresh); Value(new Vector2(0, .6f));
            _stick.OnPointerUp(fresh); Value(Vector2.zero);
        }
        [Test] public void DirectSetValueKeepsItsClampAndExplicitZeroAllowsAFreshPointer()
        {
            _stick.OnPointerDown(At(101, new Vector2(40, 0))); Value(new Vector2(.8f, 0));
            _stick.SetValue(new Vector2(3, 4)); Value(new Vector2(.6f, .8f));
            _stick.OnDrag(At(101, new Vector2(0, 30))); Value(new Vector2(0, .6f));
            _stick.SetValue(Vector2.zero); Value(Vector2.zero);
            var fresh = At(202, new Vector2(40, 0)); _stick.OnPointerDown(fresh); Value(new Vector2(.8f, 0));
            _stick.OnPointerUp(fresh); Value(Vector2.zero);
        }
        [Test] public void ResizingKeepsCapturedSteeringNormalisedAndClamped()
        {
            _stick.OnPointerDown(At(101, new Vector2(40, 0))); Value(new Vector2(.8f, 0));
            _base.sizeDelta = new Vector2(200, 200); _radius = 100; _stick.Rescale(_radius);
            _stick.OnDrag(At(101, new Vector2(80, 0))); Value(new Vector2(.8f, 0));
            // A captured drag can leave the disc; the output still stops at its rim.
            _stick.OnDrag(At(101, new Vector2(300, 0))); Value(Vector2.right);
            _stick.OnPointerUp(At(101, new Vector2(300, 0))); Value(Vector2.zero);
        }
    }
}
