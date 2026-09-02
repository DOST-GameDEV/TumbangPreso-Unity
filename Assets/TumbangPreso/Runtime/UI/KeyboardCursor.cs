using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Lets the keyboard drive a screen that was built for a pointer.
    ///
    /// ⚠️⚠️ `game-ui-design` MAKES THIS ITS FOURTH CORE BELIEF: *"controller navigation is the real
    /// test of UI architecture"*, and it lists `Controller Navigation Deadend` as an anti-pattern
    /// AND a sharp edge, because it is the failure that makes a screen unusable rather than ugly.
    /// The lobby has never been navigable without a mouse.
    ///
    /// ⚠️⚠️ AND THE REASON IS NOT MISSING NAVIGATION DATA, WHICH IS WHY THIS IS SMALL. Unity's
    /// `Selectable.navigation` defaults to `Automatic`, so every button on this screen already
    /// knows its spatial neighbours. **What is missing is a first selection**: with nothing
    /// selected the input module has nowhere to send a move event, so the arrow keys do nothing
    /// for ever and the screen looks like it has no keyboard support at all.
    ///
    /// ⚠️⚠️ IT DOES NOT PRE-ARM THE SELECTION, AND THAT IS A SAFETY DECISION RATHER THAN A STYLE
    /// ONE. Selecting START MATCH when the lobby opens would mean ENTER starts the match, and in
    /// this lobby the chat field is always open and ENTER is what a player presses to talk. A
    /// stray Enter that launches everybody into a match is the worst bug on this screen.
    /// **So the keyboard wakes up only once the player has used the keyboard to navigate**, which
    /// is a press that cannot be confused with anything else.
    ///
    /// ⚠️ TAB IS IMPLEMENTED HERE BECAUSE UNITY DOES NOT IMPLEMENT IT. The input module reads the
    /// Horizontal and Vertical axes; Tab is not a navigation key to it, and Tab is the key every
    /// person raised on a form reaches for first.
    ///
    /// ⚠️ AND IT STANDS DOWN WHILE SOMEBODY IS TYPING. `LobbyChat.AnyTyping` is the same flag the
    /// gameplay input reader asks, for the same reason: a player typing a message has no verbs,
    /// and stealing their Tab or their arrow keys mid-sentence is the chat eating the movement
    /// keys with the roles reversed.
    /// </summary>
    public sealed class KeyboardCursor : MonoBehaviour
    {
        private Selectable _home;

        /// <summary>
        /// Installs the cursor on a screen, with the control the keyboard should land on first.
        ///
        /// ⚠️ THE HOME IS THE PRIMARY ACTION, not the first control in the hierarchy. A player who
        /// presses an arrow key is asking "where am I", and the honest answer on a lobby is the
        /// button that starts the game.
        /// </summary>
        public static KeyboardCursor Install(GameObject owner, Selectable home)
        {
            if (owner == null || home == null) return null;

            var cursor = owner.GetComponent<KeyboardCursor>();
            if (cursor == null) cursor = owner.AddComponent<KeyboardCursor>();

            cursor._home = home;
            return cursor;
        }

        private void Update()
        {
            if (_home == null) return;
            if (LobbyChat.AnyTyping) return;

            var events = EventSystem.current;
            if (events == null) return;

            bool tab = Input.GetKeyDown(KeyCode.Tab);
            bool arrow = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)
                         || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow);

            if (!tab && !arrow) return;

            var current = events.currentSelectedGameObject;

            // ⚠️ A SELECTION ON A DEAD OR HIDDEN OBJECT IS NOT A SELECTION. Unity keeps the last
            // selected object across a screen being switched off, so without this the first arrow
            // press after closing a panel moves relative to a control nobody can see.
            var selectable = current != null ? current.GetComponent<Selectable>() : null;
            bool live = selectable != null && selectable.IsActive() && selectable.IsInteractable();

            if (!live)
            {
                events.SetSelectedGameObject(_home.gameObject);
                return;
            }

            if (!tab) return;

            // ⚠️ SHIFT+TAB GOES BACK, which is the half everybody notices missing. Up and left are
            // the same direction to `Automatic` navigation on a screen laid out in a column and a
            // row respectively, so asking for both covers either shape.
            bool back = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            Selectable next = back
                ? selectable.FindSelectableOnUp() ?? selectable.FindSelectableOnLeft()
                : selectable.FindSelectableOnDown() ?? selectable.FindSelectableOnRight();

            if (next != null) events.SetSelectedGameObject(next.gameObject);
        }
    }
}
