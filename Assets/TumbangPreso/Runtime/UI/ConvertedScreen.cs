using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Base for a screen whose LAYOUT was converted from a `.tscn` and whose BEHAVIOUR is
    /// ported from the matching `.gd`.
    ///
    /// ⚠️⚠️ IT FINDS ITS NODES BY THE NAMES GODOT USED, and that pairing is the whole design.
    /// A `.tscn` carries the tree and no logic; a `.gd` carries the logic and no tree. The
    /// converter reproduces the tree with the original node names, so a ported script can reach
    /// `StartButton` or `TaglineLabel` exactly as `main_menu.gd` did through `%StartButton`.
    /// Rename a node in the Godot scene and this breaks loudly rather than silently drawing a
    /// dead button.
    ///
    /// ⚠️ AND A MISSING NODE IS AN ERROR, NOT A SHRUG. A button that converts but never gets
    /// wired looks completely correct and does nothing when clicked, which is the single most
    /// confusing failure a menu can have.
    /// </summary>
    public abstract class ConvertedScreen : MonoBehaviour
    {
        private readonly Dictionary<string, Transform> _byName = new Dictionary<string, Transform>();

        protected virtual void Start()
        {
            // ⚠️⚠️ EVERY MENU RELEASES THE MOUSE, AND ONLY THE TITLE SCREEN USED TO. A match
            // captures the pointer; a screen reached straight from a match — the results board,
            // the setup screen after a rematch, anything the pause menu leads to — inherited
            // that capture and became completely unclickable while drawing perfectly. Doing it
            // in the base class means a screen added later cannot forget.
            CursorMode.Release();

            Index(transform);
            Wire();
        }

        /// <summary>
        /// Where Escape goes on this screen. Null means "nowhere", which is correct for the
        /// title.
        ///
        /// ⚠️⚠️ EVERY GODOT SCREEN HANDLES `ui_cancel` AND NOT ONE CONVERTED SCREEN DID.
        /// `match_setup.gd::_unhandled_input`, `multiplayer_setup.gd`, `character_select.gd`
        /// and the three overlays all back out on Escape, and the conversion dropped the
        /// handler along with the input map entry. On a screen whose BACK button is also being
        /// blocked, that leaves a player with no way out of the menu at all, which is exactly
        /// how "buttons dont work, back etc" reads from the other side.
        /// </summary>
        protected virtual string CancelTarget => null;

        protected virtual void Update()
        {
            if (CancelTarget == null) return;
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            MenuSfx.Back();
            SceneFlow.Go(CancelTarget);
        }

        private void Index(Transform t)
        {
            _byName[t.name] = t;
            for (int i = 0; i < t.childCount; i++) Index(t.GetChild(i));
        }

        protected abstract void Wire();

        protected Transform Node(string name)
        {
            if (_byName.TryGetValue(name, out var t)) return t;

            Debug.LogError($"[{GetType().Name}] no node named '{name}' in the converted scene. " +
                           "The Godot scene was renamed, or the conversion dropped it.");
            return null;
        }

        /// <summary>Wire a converted button. Logs loudly rather than failing quietly.</summary>
        protected void OnClick(string nodeName, UnityEngine.Events.UnityAction action)
        {
            var t = Node(nodeName);
            if (t == null) return;

            var btn = t.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogError($"[{GetType().Name}] '{nodeName}' converted without a Button.");
                return;
            }

            btn.onClick.RemoveAllListeners();

            // ⚠️ EVERY MENU PRESS IS LOGGED, AND IT IS NOT DEBUG LEAVINGS. "The buttons don't
            // work" has at least four causes that all look identical on screen — a covered
            // raycast, a control off the viewport, a locked cursor, a dead input backend — and
            // the ONLY way to tell "the press never happened" from "the press happened and the
            // screen did not change" in a shipped .exe is a line in `Player.log`. One line per
            // deliberate press costs nothing and has already saved a session.
            btn.onClick.AddListener(() => Debug.Log($"[UI] pressed {nodeName}"));

            btn.onClick.AddListener(action);

            // ⚠️ EVERY BUTTON MAKES A SOUND. The Godot build wires ui_click and ui_hover on the
            // theme, so silence here is not "no sound designed yet", it is a regression against
            // a game that already had it.
            btn.onClick.AddListener(() => GameServices.Audio?.PlayAt("ui_click", Vector3.zero));
        }

        protected void SetText(string nodeName, string value)
        {
            var t = Node(nodeName);
            if (t == null) return;

            var text = t.GetComponent<Text>() ?? t.GetComponentInChildren<Text>();
            if (text != null) text.text = value;
        }
    }
}
