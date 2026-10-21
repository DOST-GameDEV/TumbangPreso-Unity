using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// A field answering the two things a player does to it: taking it, and typing in it.
    ///
    /// ⚠️⚠️ THE THREE LOGIN FIELDS WERE COMPLETELY SILENT AND THEY ARE THE MOST PRESSED
    /// CONTROLS ON THE SCREEN. Every button on her login plays a click, the checkbox ticks, the
    /// pill toggles, a fault buzzes and a field turning valid chimes, so the one thing that made
    /// no sound at all was the thing a player spends the whole screen doing: you could take a
    /// field, type a whole password and submit it having heard nothing until the button.
    /// 🧑: *"atleast sounds when u interact with shit"*.
    ///
    /// ⚠️⚠️ THE KEYSTROKE IS A DIFFERENT CUE FROM A CLICK AND IT IS DELIBERATELY QUIET.
    /// `ui_tick` is the checkbox's cue, which is the smallest sound in the set, and it is played
    /// through `PlayUiVaried` at **35 per cent** with a pitch window so a held key does not comb
    /// into a tone. A click per character at full volume is the difference between a game that
    /// responds and a game that nags, and this screen is the first one every player meets.
    ///
    /// ⚠️ THE CUE IS PER CHARACTER, NOT PER `onValueChanged`. Unity raises that event for a
    /// paste, for a backspace and for a programmatic `text` assignment as well as for a press,
    /// so the length is compared: the screen restoring a remembered username must not sound like
    /// somebody typing it. **Deleting still sounds**, because deleting is still typing.
    ///
    /// ⚠️ AND IT IS SILENT WHILE THE FIELD IS BEING FILLED IN FOR THE PLAYER. `SetWithoutSound`
    /// is what `SignInScreen` uses when it writes a remembered value in, which is the same
    /// reason `Toggle.SetIsOnWithoutNotify` exists one control along.
    /// </summary>
    [RequireComponent(typeof(InputField))]
    public sealed class OwnerFieldSound : MonoBehaviour, ISelectHandler, IPointerDownHandler
    {
        private InputField _field;
        private int _length;
        private bool _quiet;

        private void Awake()
        {
            _field = GetComponent<InputField>();
            _length = _field.text != null ? _field.text.Length : 0;
            _field.onValueChanged.AddListener(Changed);
        }

        private void OnDisable() => _quiet = false;

        /// <summary>Writes a value in without it sounding like the player typed it.</summary>
        public void SetWithoutSound(string words)
        {
            _quiet = true;
            _field.text = words;
            _length = words != null ? words.Length : 0;
            _quiet = false;
        }

        private void Changed(string words)
        {
            int length = words != null ? words.Length : 0;
            if (_quiet || !_field.isFocused || Mathf.Abs(length - _length) != 1)
            {
                _length = length;
                return;
            }

            _length = length;
            var audio = GameServices.Audio;
            if (audio == null) return;

            // ⚠️ NOT THROUGH `MenuSfx.Tick`. That route is one cue per FRAME, which is right for
            // a press arriving through three layers and wrong here: the pitch window is what
            // stops a fast typist hearing a machine gun, and the volume is what keeps a
            // keystroke under a click.
            audio.PlayUiVaried("ui_tick", .93f, 1.07f, .35f);
        }

        /// <summary>Taking the field, by pointer or by pad.</summary>
        public void OnSelect(BaseEventData what) => Taken();

        public void OnPointerDown(PointerEventData what) => Taken();

        private void Taken()
        {
            // ⚠️ `MenuSfx` DEDUPES PER FRAME, so a pointer press that both selects the field and
            // lands on it plays one sound rather than two. That guarantee is the reason this
            // does not have to know which of its two entry points fired.
            if (_field != null && _field.interactable) MenuSfx.Click();
        }
    }
}
