using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The one line on the title screen: what to press, and a breath so the
    /// street does not read as a still image.
    ///
    /// ⚠️⚠️ THE WORDS FOLLOW THE DEVICE, WHICH IS `CLAUDE.md` § 4a APPLIED TO THE
    /// ONE INSTRUCTION EVERY PLAYER READS FIRST. She wrote "Click anywhere to
    /// continue." and that is exactly right on a mouse and exactly wrong on a
    /// phone, where there is nothing to click, and on a pad, where anywhere is
    /// not a place you can point at. `LastInputDevice` already drives every other
    /// prompt in the game off the last device touched rather than off a setting
    /// (`docs/FUTURE.md` § 14), so this is that rule and not a new one.
    ///
    /// ⚠️ THE BREATH IS OPACITY, NOT SCALE OR POSITION. 🧑 2026-09-18: *"make sure
    /// all main menu effects are subtle"*. A caption that slides or pulses in size
    /// pulls the eye off the graffiti, which is the thing on this screen; a five
    /// per cent swing in alpha over four seconds reads as alive and never as
    /// movement. It also costs nothing under reduced motion, where it simply
    /// holds at full.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public sealed class OwnerMenuPrompt : MonoBehaviour
    {
        public Button Press;
        private Text _text;
        private int _revision = -1;
        private InputDeviceKind _kind = (InputDeviceKind)(-1);
        private float _born;

        private void Awake()
        {
            _text = GetComponent<Text>();
            _born = Time.unscaledTime;
        }

        private void OnEnable() => _born = Time.unscaledTime;

        private void Update()
        {
            if (_kind != LastInputDevice.Current || _revision != LastInputDevice.Revision)
            {
                _kind = LastInputDevice.Current;
                _revision = LastInputDevice.Revision;
                _text.text = Words(_kind);
            }

            // ⚠️ THE SENTENCE IS ALSO THE IMPLEMENTATION. "Press any button" is
            // only honest if any button works, so the pad half of that promise is
            // read here through `MenuNav`, the one place allowed to read a pad.
            // The mouse and the thumb are served by the full-screen press target
            // itself, which is why neither has a branch in this method.
            if (Press != null && Press.isActiveAndEnabled && Press.interactable
                && LastInputDevice.Current == InputDeviceKind.Gamepad && MenuNav.PadAnyPressed)
                Press.onClick.Invoke();

            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            float age = Time.unscaledTime - _born;
            // The caption arrives a beat after the street, so the first thing seen
            // is her painting and the instruction settles onto it.
            float entry = reduced ? 1 : Mathf.SmoothStep(0, 1, Mathf.Clamp01((age - .45f) / .7f));
            float breath = reduced ? 1 : .93f + .07f * Mathf.Sin(age * Mathf.PI * 2 / 3.9f);
            var colour = _text.color;
            colour.a = entry * breath;
            _text.color = colour;
        }

        /// <summary>
        /// ⚠️ "ANYWHERE" SURVIVES ON TOUCH AND HAS TO GO ON A PAD. A thumb really
        /// can press anywhere; a pad player has no pointer, so the truthful
        /// instruction names the button. The press target is the same full-screen
        /// control in all three cases, which is what `ScreenFocus` submits to.
        /// </summary>
        private static string Words(InputDeviceKind kind) => kind switch
        {
            InputDeviceKind.Touch => "Tap anywhere to continue.",
            InputDeviceKind.Gamepad => "Press any button to continue.",
            _ => "Click anywhere to continue.",
        };
    }
}
