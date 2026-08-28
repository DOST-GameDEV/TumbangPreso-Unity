using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The one place the front end asks for a sound.
    ///
    /// ⚠️⚠️ EVERY CONTROL IN THE GODOT BUILD MAKES A NOISE AND THE CONVERSION WAS SILENT.
    /// `arrow_button.gd` hooks `ui_hover` and `ui_click` for every pennant in the game in one
    /// place, and the wood buttons carry their own two connections at their call sites. A
    /// converted menu that only plays a click on a successful action is a regression against a
    /// game that already had the whole layer, and silence reads as "unfinished" to a player far
    /// faster than a missing feature does.
    ///
    /// ⚠️⚠️ AND THEN THE OPPOSITE HAPPENED: ONE PRESS FIRED THE CLICK UP TO THREE TIMES. Fixing
    /// the silence added the sound in three independent layers, each of which is individually
    /// correct and none of which knows about the others:
    ///
    ///   1. the CONTROL, on pointer down: `GodotButton`, `ArrowButtonView` and
    ///      `TextureButtonFeedback` each play a click when they are pressed;
    ///   2. the WIRING, on click: `ConvertedScreen.WireOne` adds a click listener to every
    ///      converted button it hooks up, so a screen cannot forget;
    ///   3. the HANDLER: `Cycle`, `TakeSeat`, `SelectTab` and the two COPY buttons each play one
    ///      as well, because they were written when 1 and 2 did not exist.
    ///
    /// A map arrow has all three. `AudioDirector.PlayAtVaried` takes a pooled voice and plays
    /// immediately with no dedupe, so three copies of one 40 ms recording start in the same
    /// frame at the same position: they sum, which is about **+9.5 dB** over a single press, and
    /// the pitch window is 1.0 for `PlayAt` so they do not even decorrelate. It reads as a
    /// clipped, phasey clack on the arrows and a doubled one on every wood button, next to a
    /// clean single click on the runtime-built controls that only have layer 1.
    ///
    /// ⚠️⚠️ THE FIX IS HERE AND NOT AT THE CALL SITES, AND THAT IS THE WHOLE POINT. Deleting two
    /// of the three layers is a nine-file edit that has to be got right in every file, leaves the
    /// rule written down nowhere, and regresses to SILENCE the first time somebody removes the
    /// wrong one. **One press is one sound** is a property of the sound layer, so it is enforced
    /// in the sound layer: all three layers may keep asking, and the first ask in a frame is the
    /// one that plays.
    ///
    /// ⚠️ IT IS PER CUE, NOT GLOBAL. A frame may legitimately carry a click and an error, or a
    /// back and a hover (the pointer leaving a button and entering the next one is one movement
    /// across a boundary). What may not happen twice is the SAME cue.
    ///
    /// ⚠️ AND IT IS A FRAME, NOT A TIMER. A press is one frame's worth of events by construction:
    /// pointer-down, the `Button.onClick` it raises, and the handler that runs inside it all
    /// happen in the same `Update`. A time window would also swallow a genuine second press from
    /// somebody clicking fast, which is a real thing to do on a map arrow.
    /// </summary>
    public static class MenuSfx
    {
        public static void Hover() => Play("ui_hover");
        public static void Click() => Play("ui_click");
        public static void Back() => Play("ui_back");
        public static void Error() => Play("ui_error");

        /// <summary>
        /// Plays a named UI cue, at most once per frame.
        ///
        /// ⚠️ PUBLIC, BECAUSE A CONTROL MAY OWN A DIFFERENT PRESS SOUND. See
        /// <see cref="GodotButton.PressCue"/>: a BACK button plays `ui_back`, which is what
        /// Escape has always played for the same action, and until that field existed the button
        /// and the key that do the identical thing sounded different.
        /// </summary>
        public static void Play(string cue)
        {
            if (string.IsNullOrEmpty(cue)) return;

            int frame = Time.frameCount;

            // ⚠️ THE EDITOR RESETS `frameCount` BETWEEN PLAY SESSIONS AND THIS DICTIONARY IS A
            // STATIC THAT SURVIVES THEM, so a stale entry from the last run can be AHEAD of the
            // current frame and swallow the first press of the new one. `!=` rather than `>=`
            // costs nothing and cannot get stuck.
            if (_lastFrame.TryGetValue(cue, out int last) && last == frame) return;

            _lastFrame[cue] = frame;

            var audio = GameServices.Audio;
            if (audio != null) audio.PlayAt(cue, Vector3.zero);
        }

        private static readonly Dictionary<string, int> _lastFrame = new Dictionary<string, int>();
    }
}
