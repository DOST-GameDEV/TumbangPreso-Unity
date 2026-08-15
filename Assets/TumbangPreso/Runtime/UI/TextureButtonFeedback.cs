using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Hover and press feedback for a bare TextureButton: the selector arrows.
    ///
    /// ⚠️ THESE MAKE A NOISE TOO. `arrow_button.gd` covers the pennants and the wood set covers
    /// the buttons, but the little arrows either side of MAP and BOTS are plain TextureButtons
    /// with their own two connections at their call site in the Godot build. Leaving them silent
    /// makes the one control a player clicks most on that screen the only dead one.
    /// </summary>
    public sealed class TextureButtonFeedback : MonoBehaviour,
        UnityEngine.EventSystems.IPointerEnterHandler,
        UnityEngine.EventSystems.IPointerExitHandler,
        UnityEngine.EventSystems.IPointerDownHandler,
        UnityEngine.EventSystems.IPointerUpHandler
    {
        private Image _image;
        private Vector3 _home = Vector3.one;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _home = transform.localScale;
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e)
        {
            transform.localScale = _home * 1.12f;
            if (_image != null) _image.color = UiTheme.Amber;
            MenuSfx.Hover();
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e)
        {
            transform.localScale = _home;
            if (_image != null) _image.color = Color.white;
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e)
        {
            transform.localScale = _home * 0.92f;
            MenuSfx.Click();
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e)
        {
            transform.localScale = _home * 1.12f;
        }
    }
}
