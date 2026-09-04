using System;
using TumbangPreso.InputLayer;
using UnityEngine.InputSystem;

namespace TumbangPreso.Settings
{
    /// <summary>What a finished rebind did.</summary>
    public enum RebindOutcome
    {
        /// <summary>The control was accepted and the override is saved.</summary>
        Bound,

        /// <summary>Another action in the same context already holds it. Nothing changed.</summary>
        Conflict,

        /// <summary>The player backed out. Nothing changed.</summary>
        Cancelled,
    }

    /// <summary>
    /// One interactive rebind, from "listen" to "saved", for any screen that wants one.
    ///
    /// ⚠️⚠️ IT IS A CLASS RATHER THAN A SECOND COPY BECAUSE THERE ARE TWO SCREENS NOW, AND THIS
    /// REPOSITORY HAS ALREADY PAID FOR THE OTHER ANSWER. `ConvertedSettingsPanel.BeginRebind` was
    /// the only rebind in the game and carried eight ⚠️ notes, every one of them a fault somebody
    /// hit: the target binding index must be the PAGE'S device or the operation quietly edits the
    /// keyboard (§ 125.6), the candidate paths must be restricted or a keyboard press on the pad
    /// page silently rewrites a key, the action must be disabled or the captured press also fires
    /// the verb, the applied override must be REMOVED before the conflict check or a refusal
    /// leaves two verbs sharing a control. `CONTROLLER MAP` needs all eight and a copy would have
    /// them until the ninth was found. `docs/TODO.md` § 38.5's three dead protocols are what a
    /// second path costs here.
    ///
    /// ⚠️ IT REPORTS AND NEVER DRAWS OR SOUNDS. The caller owns its own status line, its own
    /// button labels and its own `MenuSfx`, because a settings row and a callout on a picture of
    /// a controller say the same thing in different places. This returns facts.
    /// </summary>
    public sealed class RebindSession : IDisposable
    {
        /// <summary>The action being listened for, for a screen that wants to draw "…" on it.</summary>
        public string Action { get; private set; }

        private InputActionRebindingExtensions.RebindingOperation _operation;
        private InputAction _target;
        private int _index;
        private Action<RebindOutcome, string> _finished;
        private bool _closed;

        /// <summary>
        /// Why <paramref name="action"/> cannot be rebound on <paramref name="device"/>, or null
        /// when it can.
        ///
        /// ⚠️⚠️ ASK THIS BEFORE `Begin`, AND § 125.6 IS WHY IT MATTERS RATHER THAN BEING TIDY.
        /// `ScreenInputCatalogue` records a `null` pad path as a written-down answer rather than
        /// an omission (`ToggleFullscreen` has none, because a phone has no window and a pad
        /// player is not the player who alt-tabs). Listening on such a row would hand
        /// `Rebinding.TryRebind` a pad control for an action with no pad binding, and the
        /// fallback that used to sit there wrote it over the KEYBOARD binding: *"the row then
        /// read Button South, the key stopped working, and Reset All was the only way back."*
        /// `TryRebind` refuses that now as well; this is the half that explains it to the player
        /// instead of making them press a button to find out.
        /// </summary>
        public static string RefusalFor(InputActionAsset asset, string action,
                                        InputDeviceKind device)
        {
            if (Rebinding.HasBindingFor(asset, action, device)) return null;

            bool pad = device == InputDeviceKind.Gamepad;

            // ⚠️ THE MOVEMENT ROWS GET THEIR OWN SENTENCE, because the generic one would
            // contradict what the row is showing. On a pad they read "Left Stick", which is true,
            // and "has no gamepad control" beside it is not the explanation a player needs: the
            // stick does all four and no direction of it is separately bindable.
            if (pad && Rebinding.IsMovePart(action))
                return "Movement is one control on a pad. The stick does all four.";

            return $"\"{Rebinding.LabelFor(action)}\" has no " +
                   $"{(pad ? "gamepad" : "keyboard or mouse")} control to rebind.";
        }

        /// <summary>
        /// Starts listening. Returns null when the rebind could not be started at all, which the
        /// caller should have ruled out with <see cref="RefusalFor"/> first.
        /// </summary>
        public static RebindSession Begin(InputActionAsset asset, string action,
                                          InputDeviceKind device,
                                          Action<RebindOutcome, string> finished)
        {
            // ⚠️⚠️ THE TARGET IS THIS DEVICE'S BINDING INDEX, NOT `ResolveActionAndBindingIndex`'S.
            // That one calls `FirstKeyboardBinding` and always answers the key, which was correct
            // when an action had one binding. A rebind started from a gamepad screen has to write
            // its override onto the PAD's index or the operation quietly edits the keyboard.
            if (!Rebinding.ResolveBindingIndexFor(asset, action, device,
                                                  out var target, out int index))
                return null;

            var session = new RebindSession
            {
                Action = action,
                _target = target,
                _index = index,
                _finished = finished,
            };

            bool pad = device == InputDeviceKind.Gamepad;

            // The action must be disabled while it is being rebound, or the press being captured
            // also fires the verb it is bound to.
            target.Disable();

            // ⚠️⚠️ THE CANDIDATES ARE RESTRICTED TO THE SCREEN'S OWN DEVICE, AND WITHOUT THAT THE
            // SCREEN IS A LIE. On a gamepad page a keyboard press would otherwise be accepted,
            // and `TryRebind` writes the override onto the binding for the device that was
            // PRESSED: the player would be looking at a pad row, press a key, and have their
            // KEYBOARD binding silently changed while the row in front of them did not move.
            //
            // ⚠️ TWO `WithControlsHavingToMatchPath` CALLS, BECAUSE "KEYBOARD AND MOUSE" IS TWO
            // DEVICES AND ONE PAGE. The call is additive and a control matching ANY listed path
            // is accepted, so the desktop page takes a key or a mouse button; the pad page names
            // `<Gamepad>` twice, which costs nothing and keeps this a single chain.
            // `Rebinding.PathIsFor` carries the same grouping on the reading side.
            //
            // ⚠️ THE CANCEL IS THE PAD'S OWN B ON A PAD SCREEN, because a pad player must be able
            // to abort without reaching for a keyboard (`CLAUDE.md` § 4a: *how is this reached on
            // a pad?*). Escape still works there as well; the caller's own `Update` hands it to
            // `Cancel`, so § 6.3's rule holds and Escape backs out the innermost layer.
            session._operation = target.PerformInteractiveRebinding()
                .WithTargetBinding(index)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsHavingToMatchPath(pad ? "<Gamepad>" : "<Keyboard>")
                .WithControlsHavingToMatchPath(pad ? "<Gamepad>" : "<Mouse>")
                .WithCancelingThrough(pad ? "<Gamepad>/buttonEast" : "<Keyboard>/escape")
                .OnCancel(op => session.Close(RebindOutcome.Cancelled, null))
                .OnComplete(op => session.Complete(asset, op.selectedControl))
                .Start();

            return session;
        }

        private void Complete(InputActionAsset asset, InputControl control)
        {
            // The override the operation already applied is undone first, because the conflict
            // check has to run against the other actions and report a refusal rather than leave
            // two verbs sharing one control.
            _target.RemoveBindingOverride(_index);

            // ⚠️ THE LINE ABOVE IS A BINDING CHANGE TOO, even though it is undoing one, and
            // `Rebinding.Revision` is what lets a screen cache a key label. The net effect is zero
            // only when `TryRebind` goes on to accept; on a refusal this is the write that
            // restores the original control.
            Rebinding.Invalidate();

            string conflict = Rebinding.TryRebind(asset, Action, control);

            Close(conflict == null ? RebindOutcome.Bound : RebindOutcome.Conflict, conflict);
        }

        /// <summary>Backs out without changing anything. Safe to call more than once.</summary>
        public void Cancel() => Close(RebindOutcome.Cancelled, null);

        private void Close(RebindOutcome outcome, string conflict)
        {
            if (_closed) return;
            _closed = true;

            _operation?.Dispose();
            _operation = null;

            // ⚠️ THE ACTION IS RE-ENABLED ON EVERY PATH, INCLUDING THE ONES NOBODY TESTS. A
            // rebind abandoned because the screen was destroyed mid-listen would otherwise leave
            // a verb disabled for the rest of the session, and the symptom is one control that
            // has silently stopped working in a match nobody connects to a menu they closed.
            _target?.Enable();

            var report = _finished;
            _finished = null;
            report?.Invoke(outcome, conflict);
        }

        /// <summary>
        /// ⚠️ DISPOSING DOES NOT REPORT. A screen tearing down is not a decision the player made,
        /// so a `Cancelled` callback into a half-destroyed screen is a null reference waiting to
        /// happen. The action is still re-enabled, which is the half that matters.
        /// </summary>
        public void Dispose()
        {
            if (_closed) return;
            _closed = true;

            _operation?.Dispose();
            _operation = null;
            _target?.Enable();
            _finished = null;
        }
    }
}
