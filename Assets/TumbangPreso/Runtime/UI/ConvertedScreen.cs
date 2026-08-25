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
        /// <summary>
        /// Every converted node, indexed by the name Godot gave it.
        ///
        /// ⚠️⚠️ A LIST PER NAME, BECAUSE NAMES ARE NOT UNIQUE AND ASSUMING THEY WERE KILLED THE
        /// BACK BUTTON. 🧑 on this build: *"btw back buttons dont work in some pages like in pic
        /// 1"*, pic 1 being the Single Player setup screen. This was a
        /// `Dictionary&lt;string, Transform&gt;` written as `_byName[t.name] = t`, so on a name
        /// collision the LAST node indexed silently won and every earlier one was unreachable.
        /// Counted in the converted scenes: `MatchSetup` carries TWO nodes called `BackButton`
        /// and `MainMenu` carries THREE. The wiring therefore landed on whichever the recursive
        /// walk happened to reach last, and the one the player could actually see got no
        /// listener at all.
        ///
        /// It failed in the one way this class exists to prevent, too: `Node()` logs loudly when
        /// a name is MISSING, and a collision is not a miss — it found a node, returned it, and
        /// reported nothing. The screen drew a perfect button that did nothing, which is the
        /// exact failure the header calls the most confusing a menu can have.
        ///
        /// ⚠️ IT IS A GODOT-SIDE PROPERTY THAT DID NOT SURVIVE. There, `%BackButton` is a scene-
        /// unique name and the editor refuses to create a second one in the same scene, so the
        /// original could rely on uniqueness. The converter reproduces the TREE, which has no
        /// such guarantee once a name appears in two branches.
        /// </summary>
        private readonly Dictionary<string, List<Transform>> _byName =
            new Dictionary<string, List<Transform>>();

        protected virtual void Start()
        {
            // ⚠️⚠️ EVERY MENU RELEASES THE MOUSE, AND ONLY THE TITLE SCREEN USED TO. A match
            // captures the pointer; a screen reached straight from a match — the results board,
            // the setup screen after a rematch, anything the pause menu leads to — inherited
            // that capture and became completely unclickable while drawing perfectly. Doing it
            // in the base class means a screen added later cannot forget.
            CursorMode.Release();

            // Legacy Text is rasterised into the Canvas at its final transform. Allowing
            // fractional canvas pixels softens every Darumadrop edge, especially after a
            // fullscreen resolution change. Godot snaps this UI to physical pixels; do the
            // same for every converted screen from the shared base so one panel cannot regress.
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvas.pixelPerfect = true;

            // ⚠️ THE SCALER IS FIXED HERE RATHER THAN IN THE IMPORTER because the converted
            // screens are committed scene assets: their CanvasScaler was serialised by an
            // importer run that has already happened, so an importer-only change reaches
            // nothing that ships. See AspectSafeCanvas for what match-on-height cropped.
            AspectSafeCanvas.ApplyToParentOf(this);

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

        /// <summary>
        /// What Escape actually does. Returns false when this screen has nowhere to go, which is
        /// correct for the title.
        ///
        /// ⚠️⚠️ IT IS AN ACTION, NOT A SCENE NAME, BECAUSE HALF THE SCREENS DO NOT CHANGE SCENE.
        /// `CancelTarget` alone could only express "go to scene X", so the four screens that back
        /// out by CLOSING IN PLACE — character select, credits, settings, tutorial — had no way
        /// to say what they do and were left with `null`. Escape was therefore dead on exactly
        /// the screens the header above says the original backs out of, while the three that
        /// happen to be scene changes worked. Overriding this lets an overlay hand back its own
        /// `Close`/`Dismiss`, which is the same method its BACK button already calls, so the key
        /// and the button cannot drift apart.
        /// </summary>
        protected virtual bool Cancel()
        {
            if (CancelTarget == null) return false;

            SceneFlow.Go(CancelTarget);
            return true;
        }

        protected virtual void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            // ⚠️ THE SOUND FOLLOWS THE DECISION. Playing it before asking would click on a
            // screen that then does nothing, which reads as a press that was swallowed.
            if (Cancel()) MenuSfx.Back();
        }

        private void Index(Transform t)
        {
            if (!_byName.TryGetValue(t.name, out var bucket))
                _byName[t.name] = bucket = new List<Transform>();

            bucket.Add(t);

            for (int i = 0; i < t.childCount; i++) Index(t.GetChild(i));
        }

        protected abstract void Wire();

        /// <summary>
        /// The first node with this name, for the readers that want exactly one — a label to set,
        /// a panel to show. See <see cref="Nodes"/> for why "first" needs saying at all.
        /// </summary>
        protected Transform Node(string name)
        {
            var all = Nodes(name);
            return all.Count > 0 ? all[0] : null;
        }

        /// <summary>
        /// EVERY node with this name, in tree order. Empty and logged loudly when there are none.
        /// </summary>
        protected IReadOnlyList<Transform> Nodes(string name)
        {
            if (_byName.TryGetValue(name, out var all) && all.Count > 0) return all;

            Debug.LogError($"[{GetType().Name}] no node named '{name}' in the converted scene. " +
                           "The Godot scene was renamed, or the conversion dropped it.");

            return System.Array.Empty<Transform>();
        }

        /// <summary>
        /// Wire a converted button. Logs loudly rather than failing quietly.
        ///
        /// ⚠️⚠️ IT WIRES EVERY NODE OF THAT NAME, NOT THE FIRST. See the note on `_byName`: two
        /// `BackButton`s in one converted scene meant one of them was dead, and which one was
        /// decided by tree order rather than by anything a reader could see. Nodes that share a
        /// name are the same logical control — the converter took the name from Godot, where it
        /// is unique per scene — so giving all of them the same handler is the behaviour the
        /// original had, and it cannot leave a visible button inert.
        ///
        /// ⚠️ AND THE COLLISION IS REPORTED. It is legitimate here but it is also how the bug
        /// hid, so it says so once per wiring rather than passing in silence.
        /// </summary>
        protected void OnClick(string nodeName, UnityEngine.Events.UnityAction action)
        {
            var all = Nodes(nodeName);
            if (all.Count == 0) return;

            if (all.Count > 1)
                Debug.Log($"[UI] '{nodeName}' matches {all.Count} nodes; wiring all of them.");

            foreach (var t in all) WireOne(nodeName, t, action);
        }

        private void WireOne(string nodeName, Transform t, UnityEngine.Events.UnityAction action)
        {
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

        /// <summary>
        /// Writes a headline and shrinks it until it fits the box it was authored in.
        ///
        /// ⚠️⚠️ THE CONVERTED BANNERS OVERFLOW AND CANNOT TELL YOU THEY HAVE. Every one of them
        /// carries `m_HorizontalOverflow: 1`, which is `Overflow`: the string neither wraps nor
        /// shrinks, it simply draws past the edge of its plate. On `CharacterSelect` the ribbon
        /// is 614 px, the label box inside it is 424, and the font is 66 pt, so "SINGLE PLAYER"
        /// lands about right and "CHOOSE YOUR HERO" runs a good hundred pixels out of the
        /// yellow. 🧑: *"choose your hero overfills the box too"*.
        ///
        /// ⚠️ THIS IS THE THIRD TIME THE SAME SETTING HAS DONE THE SAME THING in one session:
        /// the objective card's "-5 / SECOND" ran off the screen edge, the deck tile's "RECAST"
        /// would have hung out of a 60 px tile, and now the banner. `Overflow` is the default
        /// these screens were converted with, so assume any authored label can overflow and size
        /// it against the string rather than trusting the author's font choice.
        ///
        /// ⚠️ IT SHRINKS RATHER THAN WRAPS. The ribbon is 101 px tall and holds one line by
        /// design; wrapping "CHOOSE YOUR HERO" would put a second line outside the plate, which
        /// trades an overflow sideways for an overflow downwards.
        ///
        /// ⚠️ AND IT ONLY EVER SHRINKS, never grows. Raising a short headline to fill the plate
        /// would make the banner change size from screen to screen, and the ribbon is a fixed
        /// piece of art that the rest of the layout is positioned against.
        /// </summary>
        protected void SetHeadline(string nodeName, string value, int authoredSize)
        {
            var node = Node(nodeName);
            if (node == null) return;

            var text = node.GetComponent<Text>() ?? node.GetComponentInChildren<Text>();
            if (text == null) return;

            text.text = value;
            text.fontSize = authoredSize;

            var rect = text.rectTransform;
            float room = rect.rect.width;

            // A rect that has not been laid out yet reports 0 and would drive the font to its
            // floor. Leaving it at the authored size is the safe answer: it is what shipped.
            if (room <= 1.0f) return;

            // ⚠️ MEASURED THROUGH THIS LABEL, not a spare font metric, for the reason
            // `Hud.WorstCaseNameWidth` gives: `preferredWidth` is what this exact component will
            // lay out to, same font, same generator settings.
            const int floorSize = 24;
            while (text.fontSize > floorSize && text.preferredWidth > room)
                text.fontSize -= 2;
        }
    }
}
