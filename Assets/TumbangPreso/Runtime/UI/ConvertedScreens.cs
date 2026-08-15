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
            Index(transform);
            Wire();
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

    /// <summary>
    /// Ported from `main_menu.gd`.
    ///
    /// ⚠️ THE THREE OVERLAYS ARE CHILDREN OF THIS SCENE, shown in place, exactly as the Godot
    /// scene instances them. Switching scenes for them would tear down and rebuild the title
    /// screen behind a panel the player is about to close in a few seconds.
    ///
    /// ⚠️ AND THE PENNANTS RE-UNFURL WHEN ONE CLOSES. `_unfurl()` is called again on every
    /// panel's back, not only on load, which is where the screen gets its sense of life.
    /// Without it, coming back from SETTINGS reveals a static column.
    /// </summary>
    public sealed class ConvertedMainMenu : ConvertedScreen
    {
        protected override void Wire()
        {
            OnClick("StartButton", () => SceneFlow.Go(SceneFlow.ModeSelect));
            OnClick("QuitButton", SceneFlow.Quit);

            Overlay("SettingsButton", "SettingsPanel");
            Overlay("TutorialButton", "TutorialPanel");
            Overlay("CreditsButton", "CreditsPanel");

            // The title screen is where the mouse comes back. A match captures it.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            GameServices.Music?.Play("menu", GameServices.MenuTrack);
        }

        private void Overlay(string button, string panel)
        {
            var node = Node(panel);
            if (node == null) return;

            var overlay = node.GetComponent<ConvertedOverlay>();

            if (overlay != null)
            {
                overlay.BackPressed -= Unfurl;
                overlay.BackPressed += Unfurl;
            }

            OnClick(button, () =>
            {
                node.gameObject.SetActive(true);

                // ⚠️ THE TUTORIAL REOPENS ON PAGE ONE. `main_menu.gd` calls
                // `reset_to_first_page()` on every open, because a player who closed it on
                // page four and comes back wants the start, not where they left off.
                var tutorial = node.GetComponent<ConvertedTutorialPanel>();
                if (tutorial != null) tutorial.ResetToFirstPage();
            });
        }

        private void Unfurl()
        {
            var entrance = GetComponent<PennantEntrance>();
            if (entrance != null) entrance.Play();
        }
    }

    /// <summary>Ported from `mode_select.gd`.</summary>
    public sealed class ConvertedModeSelect : ConvertedScreen
    {
        protected override void Wire()
        {
            OnClick("SoloButton", () =>
            {
                SceneFlow.Networked = false;
                SceneFlow.Go(SceneFlow.MatchSetup);
            });

            OnClick("MultiButton", () =>
            {
                SceneFlow.Networked = true;
                SceneFlow.Go(SceneFlow.MultiplayerSetup);
            });

            OnClick("BackButton", () => SceneFlow.Go(SceneFlow.MainMenu));
        }
    }

    /// <summary>
    /// The version stamp every screen carries in the corner.
    ///
    /// ⚠️ IT IS READ FROM THE PROJECT, NOT TYPED. The Godot build prints the version from
    /// `application/config/version`, and a hand-typed copy is wrong the first time somebody
    /// bumps one and not the other. Screenshots get sent to sponsors with that number on them.
    /// </summary>
    public sealed class VersionStamp : MonoBehaviour
    {
        private void Start()
        {
            var text = GetComponent<Text>();
            if (text != null) text.text = "v" + Application.version;
        }
    }
}
