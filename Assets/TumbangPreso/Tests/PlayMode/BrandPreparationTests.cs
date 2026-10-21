using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class BrandPreparationTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator PreparationKeepsWiredControlsReachableAtReviewSizes()
        {
            yield return HubFlowTests.OpenHome();
            // UX-1 preserves each capability but relocates rules/settings into the hamburger.
            foreach (var size in HubFlowTests.Shapes)
            {
                yield return TumpUiCapture.Capture("Hub-preparation-controls-" + size.x + "x" + size.y,
                    TumpHub.Current.Canvas, size.x, size.y, false, checkActionBounds: true);
                foreach (string name in new[] { "LoadoutButton", "NamePlate", "MenuButton", "ModeCard", "PlayButton" })
                    Hit(Button(name));
            }
            Press(Button("MenuButton")); yield return null;
            Press(Button("MenuMATCHRULES")); yield return null;
            var custom = Object.FindFirstObjectByType<CustomGameScreen>();
            Assert.IsNotNull(custom); Assert.IsTrue(custom.IsOpen);
            custom.Close(); yield return null; yield return null;
            Assert.IsFalse(custom.IsOpen);
            Assert.IsTrue(TumpHub.Current.Canvas.enabled);
            Press(Button("MenuButton")); yield return null;
            Press(Button("MenuSETTINGS")); yield return null;
            Assert.IsNotNull(Object.FindFirstObjectByType<ConvertedSettingsPanel>());
        }

        [UnityTest]
        public IEnumerator PickerBackCanBePressedImmediatelyAfterOpening()
        {
            yield return HubFlowTests.OpenHome();
            Press(Button("LoadoutButton"));
            yield return null;
            Assert.IsInstanceOf<HubLoadout>(TumpHub.Current.Top);
            // No capture or settling delay: the first-frame BACK must already beat the preview.
            Press(Button("BackButton")); yield return null;
            Assert.IsInstanceOf<HubHome>(TumpHub.Current.Top);
            Hit(Button("LoadoutButton"));
        }

        private static Button Button(string name)
        {
            var buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            var found = buttons.FirstOrDefault(b => b.name == name && b.isActiveAndEnabled);
            Assert.IsNotNull(found, "Missing active control: " + name + "; active: " +
                string.Join(", ", buttons.Where(b => b.isActiveAndEnabled).Select(b => b.name)));
            return found;
        }

        private static PointerEventData Hit(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var canvas = button.GetComponentInParent<Canvas>();
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var point = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(rect.rect.center));
            Assert.That(point.x, Is.InRange(0, Screen.width), button.name + " is outside the screen");
            Assert.That(point.y, Is.InRange(0, Screen.height), button.name + " is outside the screen");
            var pointer = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.IsNotEmpty(hits, button.name + " has no hit area");
            Assert.AreEqual(button, hits[0].gameObject.GetComponentInParent<Button>(), button.name + " is covered by " + hits[0].gameObject.name);
            return pointer;
        }

        private static void Press(Button button) => ExecuteEvents.Execute(button.gameObject, Hit(button), ExecuteEvents.pointerClickHandler);
    }
}
