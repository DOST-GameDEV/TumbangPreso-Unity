using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Hands the mouse wheel to a <see cref="ScrollRect"/> from anywhere inside a panel.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE UNITY'S WHEEL IS DELIVERED BY RAYCAST AND A PANEL IS MOSTLY
    /// HOLES. The event module takes whatever the pointer is over, walks UP the hierarchy for
    /// the first `IScrollHandler`, and drops the event entirely when the raycast hits nothing or
    /// hits something outside the list. So the wheel worked over a key cap, did nothing over the
    /// gap above it, and did nothing at all over the panel's own margins, its heading, or the
    /// strip beside the scrollbar. 🧑 2026-08-27, on the fourth report of this: *"u cant scroll
    /// by using mouse scroll or laptop pad scroll ... it feels so clunky/doesnt work at all"*.
    ///
    /// ⚠️ THIS IS THE OUTER HALF AND THE INVISIBLE VIEWPORT GRAPHIC IS THE INNER ONE. The graphic
    /// closes the gaps INSIDE the list; this catches the wheel over everything else the panel
    /// covers. Either alone leaves a dead region, and a dead region is what "clunky" means.
    ///
    /// ⚠️ IT FORWARDS RATHER THAN IMPLEMENTING ITS OWN SCROLLING, so the step, the clamping and
    /// the scrollbar all stay owned by the one `ScrollRect` and cannot drift from it.
    ///
    /// ⚠️ AND IT NEVER STEALS FROM AN INNER LIST. `IScrollHandler` bubbles from the deepest
    /// handler outwards, so a nested scroll view under the cursor consumes the event before this
    /// ever sees it. A dropdown's own list keeps its wheel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScrollWheelRelay : MonoBehaviour, IScrollHandler
    {
        [SerializeField] private ScrollRect _target;

        /// <summary>
        /// Installs the relay on <paramref name="where"/>, pointed at <paramref name="target"/>.
        ///
        /// ⚠️ THE HOST OBJECT MUST BE RAYCASTABLE OR THIS IS INERT, which is the same trap it
        /// was written to fix. A panel with an authored background image already is; one without
        /// gets the same invisible full-rect graphic the viewport gets.
        /// </summary>
        public static void Install(GameObject where, ScrollRect target)
        {
            if (where == null || target == null) return;

            var relay = where.GetComponent<ScrollWheelRelay>();
            if (relay == null) relay = where.AddComponent<ScrollWheelRelay>();
            relay._target = target;

            if (where.GetComponent<Graphic>() != null) return;

            var image = where.AddComponent<Image>();
            image.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            image.raycastTarget = true;
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (_target == null || !_target.isActiveAndEnabled) return;

            _target.OnScroll(eventData);
        }
    }
}
