using UnityEngine;
using UnityEngine.EventSystems;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The mouse half of <see cref="ModelPreview"/>: drag to turn the view, wheel to zoom,
    /// right-click to snap back to the framed shot.
    ///
    /// ⚠️⚠️ THIS IS THE COMPONENT THAT DID NOT EXIST, AND ITS ABSENCE MADE THE PANEL'S OWN HINT
    /// LINE A LIE. `ModelPreview.Orbit` and `.Zoom` were both written and both public, and
    /// nothing in the project ever called either one, while the CHARACTER screen printed *"Drag
    /// to turn the view · scroll to zoom · right-click to reset"* under the meters. Reported as
    /// *"model isnt movable"*, which is exactly what it was.
    ///
    /// ⚠️ A SEPARATE FILE, NOT A NESTED CLASS. One MonoBehaviour per file is a rule this repo
    /// learned the hard way: a second one in the same file loads with a null script in a built
    /// player and the screen comes up dead with no error.
    ///
    /// ⚠️ AND IT SITS ON THE RAWIMAGE, NOT ON THE PANEL. Godot uses `_gui_input` rather than
    /// `_unhandled_input` for the same reason: the wood panel, the tabs and both buttons are in
    /// front of this surface, and a drag that starts on a button must belong to the button.
    /// UGUI's event system routes to the topmost raycast target, which is that rule for free.
    /// </summary>
    public sealed class ModelPreviewInput : MonoBehaviour,
        IDragHandler, IScrollHandler, IPointerClickHandler
    {
        private ModelPreview _preview;

        public void Bind(ModelPreview preview) => _preview = preview;

        public void OnDrag(PointerEventData e)
        {
            // Left only. A right-drag is the reset gesture's press and must not also spin the
            // subject on the way to releasing it.
            if (e.button != PointerEventData.InputButton.Left || _preview == null) return;

            _preview.Orbit(e.delta);
        }

        public void OnScroll(PointerEventData e)
        {
            if (_preview == null) return;

            // ⚠️ A TILE HANDS THE WHEEL BACK TO THE PAGE, and implementing IScrollHandler at all
            // is what would otherwise steal it: Unity walks UP the hierarchy for a scroll and
            // stops at the first component that handles it, so a tutorial tile would swallow
            // every wheel notch the cursor happened to be over. Passing it to the parent is what
            // keeps the tutorial's own scroll view working.
            if (!_preview.WheelZooms)
            {
                var parent = transform.parent;

                if (parent != null)
                    ExecuteEvents.ExecuteHierarchy(parent.gameObject, e, ExecuteEvents.scrollHandler);

                return;
            }

            // ⚠️ `scrollDelta.y` IS IN NOTCHES ON WINDOWS AND IN PIXELS ELSEWHERE, so it is
            // reduced to its SIGN before being scaled by the zoom step. Feeding the raw value in
            // makes one wheel click cross the whole zoom range on a trackpad.
            _preview.Zoom(Mathf.Sign(e.scrollDelta.y) * (Mathf.Abs(e.scrollDelta.y) > 0.01f ? 1.0f : 0.0f));
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Right || _preview == null) return;

            _preview.ResetView();
        }
    }
}
