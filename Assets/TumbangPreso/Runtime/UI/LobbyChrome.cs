using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Which arrangement the setup screen draws.
    ///
    /// ⚠️⚠️ `Classic` IS KEPT AND IT IS NOT DEAD CODE. 🧑 2026-08-28: *"dont delete old huds and ui
    /// tho keep them incase ur shit turns ugly"*. It is the authored converted layout exactly as
    /// it shipped, and switching back is this one enum rather than a revert, because everything
    /// `Street` does is a repositioning applied at runtime to the SAME nodes. No node is created
    /// that `Classic` needs, none is destroyed, and none is renamed.
    /// </summary>
    public enum LobbyStyle
    {
        /// <summary>Two centred wooden columns, as converted from the .tscn.</summary>
        Classic,

        /// <summary>The room is the picture: furniture pushed to the edges, cast in the middle.</summary>
        Street,
    }

    /// <summary>
    /// Rearranges the setup screen's authored furniture into the `Street` layout.
    ///
    /// ⚠️⚠️ IT MOVES WHAT IS ALREADY THERE AND BUILDS ALMOST NOTHING. `ConvertedScreen` finds
    /// every control by the name Godot gave it and `Node()` logs an error on a miss, so a redesign
    /// that rebuilt this screen would have to reproduce fourteen exact names or break the wiring
    /// silently. Repositioning keeps `SeatButton0..3`, `PrimaryButton`, `StartButton`,
    /// `MapValueLabel` and the rest exactly where the script expects to find them, keeps their
    /// handlers, keeps their `GodotButton` skins, and keeps `MatchSetup.unity` almost unchanged on
    /// disk so `SceneScriptCheck` has nothing new to refuse.
    ///
    /// ⚠️⚠️ AND THE PANELS STAY OPAQUE WOOD. `UiTheme.HeroPlate`'s note is explicit that a
    /// translucent near-black plate is COMBAT chrome, where the court behind it is the subject,
    /// and that menu chrome is FURNITURE and may be opaque. 🧑 has already rejected the other
    /// answer once: *"the brown shit looks ugly"*. The way the room becomes the picture here is by
    /// making the furniture SMALLER and pushing it to the edges, which is what the reference
    /// screenshots actually do, rather than by making it see-through.
    ///
    /// ⚠️ EVERY STEP IS INDIVIDUALLY GUARDED. A missing node leaves that piece in its authored
    /// place and logs, rather than throwing halfway through and leaving the screen in a state
    /// that is neither layout.
    /// </summary>
    public static class LobbyChrome
    {
        /// <summary>
        /// The default, and the only place it is decided.
        ///
        /// ⚠️ A FIELD RATHER THAN A CONST so a probe can photograph both without a rebuild, and
        /// so reverting is one assignment. `LobbyStyleTests` asserts that every name the screen
        /// reaches for still resolves under both.
        /// </summary>
        public static LobbyStyle Style = LobbyStyle.Street;

        /// <summary>
        /// Width of the furniture columns in the authored 1920x1080 space.
        ///
        /// ⚠️⚠️ MEASURED OFF `Logs/shots-runtime/Lobby-v2.png` RATHER THAN CHOSEN. At 660 and 560
        /// the two columns cover 1220 of 1920 px, so the clear band the cast has to stand in was
        /// 700 px wide and the leftmost of the four was entirely behind the config panel.
        /// Narrowing them to 580 and 500 gives the middle 840 px, which is what four bodies at
        /// `LobbyCast.Spacing` 1.75 occupy at the lobby framing.
        /// </summary>
        private const float LeftWidth = 580.0f;
        private const float RightWidth = 500.0f;
        private const float EdgeMargin = 48.0f;
        private const float BottomMargin = 40.0f;

        /// <summary>
        /// How much the two authored columns shrink in the `Street` arrangement.
        ///
        /// ⚠️⚠️ MEASURED OFF THE RENDERS, AND TIGHTENED TWICE. The config panel draws 820 px wide
        /// and the seat panel 560, so unscaled the clear band between them is 320 px and four
        /// characters need about 700. 0.72 and 0.86 opened it to 625, which fit the cast at
        /// `LobbyCast.Spacing` 1.20 and stopped fitting the moment the spacing was widened on
        /// request: `Lobby-v10.png` has the outer two behind the furniture again. 0.66 and 0.78
        /// give 846 px, which holds the wider line with about 70 px of margin at each end.
        ///
        /// ⚠️ THIS IS NEAR THE FLOOR AND THE FLOOR IS REAL. The smallest type in the left column
        /// is the map detail line at 20 units, which at 0.66 renders as 13: below that it stops
        /// being a sentence and becomes texture. Widening the band any further has to come from
        /// moving the camera back, not from shrinking the furniture again.
        ///
        /// ⚠️ THE TWO DIFFER BECAUSE THEIR CONTENTS DO. The seat panel is four rows of a name and
        /// a tick, and it is the thing a player reads to find out who is here, so it keeps more of
        /// its size. The config panel is four labelled cyclers whose values are short words, and
        /// it survives being small.
        ///
        /// ⚠️ AND NEITHER MAY GO BELOW ABOUT 0.65, because `MenuKit.MinReadableUnits` is a floor on
        /// the AUTHORED font size and a scale multiplies whatever survives it. `AspectRatioProbes`
        /// checks the authored number and cannot see a scaled parent, so this is the one place
        /// that bound has to be respected by hand.
        /// </summary>
        private const float LeftScale = 0.66f;
        private const float RightScale = 0.78f;

        /// <summary>How tall the gradient bands are, as a fraction of the screen.</summary>
        private const float TopBandFraction = 0.24f;
        private const float BottomBandFraction = 0.30f;

        /// <summary>
        /// How dark each band gets at the screen edge.
        ///
        /// ⚠️ THE BOTTOM IS LIGHTER THAN THE TOP, WHICH IS THE OPPOSITE OF THE OBVIOUS CHOICE. The
        /// top band sits behind the banner and the tabs and has nothing but sky under it; the
        /// bottom band has the CAST'S LEGS under it, and the whole point of the arrangement is that
        /// the room is the picture. 0.30 is enough for cream type over a bright road and little
        /// enough that a character standing in it still reads as lit.
        /// </summary>
        private const float TopBandAlpha = 0.52f;
        private const float BottomBandAlpha = 0.30f;

        /// <summary>
        /// How far below the top the settings stack begins.
        ///
        /// ⚠️ MEASURED AGAINST THE BANNER, NOT GUESSED. `MatchSetup.tscn` puts the `Banner` at
        /// 648x144 anchored top-left with an anchored y of -112, so its plate runs from about y 40
        /// to y 184. 208 clears it with a 24 px gap and still leaves the settings block well above
        /// the cast's heads.
        /// </summary>
        /// <summary>The compact settings drawer begins just below the banner.</summary>
        private const float SettingsY = 112.0f;
        private const float SettingsWidth = 620.0f;
        private const float SettingsCollapsedWidth = 460.0f;
        private const float SettingsHeaderHeight = 56.0f;
        private const float SettingsBodyHeight = 430.0f;

        /// <summary>How far below the top edge the lobby card sits.</summary>
        private const float CardY = 26.0f;

        /// <summary>START and BACK stay clear of the banner and the collapsed settings summary.</summary>
        private const float ActionStackY = 470.0f;

        private const float TabHeight = 52.0f;
        private const float TabWidth = 260.0f;

        /// <summary>
        /// Applies the arrangement. Safe to call once per screen load and nowhere else.
        /// </summary>
        /// <param name="root">The screen's own transform, already indexed by the caller.</param>
        /// <param name="find">How to reach a node by its Godot name.</param>
        /// <param name="onTab">Raised with the chosen tab: false for practice, true for lobby.</param>
        public static Tabs Apply(Transform root, Func<string, Transform> find,
                                 bool isLobby, Action<bool> onTab)
        {
            if (Style != LobbyStyle.Street) return null;
            if (root == null || find == null) return null;

            SoftenScrim(root, find);
            var lobbyDrawer = MoveColumns(find);
            EnlargePrimaryActions(find);
            BuildIdentity(root, find);

            var tabs = BuildTabs(root, find, isLobby, onTab);
            tabs.LobbyDrawer = lobbyDrawer;
            tabs.SetActive(isLobby);
            return tabs;
        }

        /// <summary>Makes START MATCH and READY read as the lobby's primary action instead of as
        /// another ordinary row. Both exist because the same screen serves multiplayer and
        /// practice; only the active one is visible.</summary>
        private static void EnlargePrimaryActions(Func<string, Transform> find)
        {
            foreach (string name in new[] { "StartButton", "PrimaryButton" })
            {
                var node = find(name);
                if (node == null) continue;

                var element = node.GetComponent<LayoutElement>();
                if (element == null) element = node.gameObject.AddComponent<LayoutElement>();
                element.minHeight = 116.0f;
                element.preferredHeight = 116.0f;
                element.flexibleHeight = 0.0f;

                var label = node.GetComponentInChildren<Text>();
                if (label != null) label.fontSize = Mathf.Max(label.fontSize, 42);
            }
        }

        /// <summary>Shows the active local profile in the lobby without duplicating the editable
        /// field from Settings. The lobby is where other players see this name, so hiding it only
        /// in Settings makes identity feel accidental.</summary>
        private static void BuildIdentity(Transform root, Func<string, Transform> find)
        {
            var banner = find("Banner");
            Transform parent = banner != null ? banner.parent : root;

            string playerName = Settings.SettingsStore.Current.PlayerName;
            if (string.IsNullOrWhiteSpace(playerName)) playerName = "Player";
            else playerName = playerName.Trim();

            var chip = new GameObject("LobbyIdentity");
            chip.transform.SetParent(parent, false);

            var image = chip.AddComponent<Image>();
            image.sprite = GodotTheme.WoodBox(UiTheme.WoodDeep, UiTheme.WoodEdge);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = false;

            var rect = image.rectTransform;
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-48.0f, -34.0f);
            rect.sizeDelta = new Vector2(420.0f, 56.0f);

            var caption = MenuKit.Label(chip.transform, "PLAYER NAME", 14, UiTheme.Amber,
                                        Vector2.zero, Vector2.zero, Vector2.zero,
                                        TextAnchor.MiddleCenter);
            caption.raycastTarget = false;
            caption.rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
            caption.rectTransform.anchorMax = new Vector2(0.38f, 1.0f);
            caption.rectTransform.offsetMin = new Vector2(12.0f, 0.0f);
            caption.rectTransform.offsetMax = Vector2.zero;

            var fieldGo = new GameObject("PlayerNameEdit");
            fieldGo.transform.SetParent(chip.transform, false);
            var fieldImage = fieldGo.AddComponent<Image>();
            fieldImage.sprite = GodotTheme.WoodBox(UiTheme.WoodDark, UiTheme.WoodEdge);
            fieldImage.type = Image.Type.Sliced;
            fieldImage.color = Color.white;

            var fieldRect = fieldImage.rectTransform;
            fieldRect.anchorMin = new Vector2(0.38f, 0.0f);
            fieldRect.anchorMax = Vector2.one;
            fieldRect.offsetMin = new Vector2(4.0f, 7.0f);
            fieldRect.offsetMax = new Vector2(-8.0f, -7.0f);

            var placeholder = MenuKit.Label(fieldGo.transform, "CLICK TO SET NAME  ✎", 17,
                                            UiTheme.CreamMuted, Vector2.zero, Vector2.zero,
                                            Vector2.zero, TextAnchor.MiddleLeft);
            placeholder.raycastTarget = false;
            MenuKit.Stretch(placeholder.rectTransform, 12.0f);

            var typed = MenuKit.Label(fieldGo.transform, playerName, 20, UiTheme.Cream,
                                      Vector2.zero, Vector2.zero, Vector2.zero,
                                      TextAnchor.MiddleLeft);
            typed.raycastTarget = false;
            typed.supportRichText = false;
            MenuKit.Stretch(typed.rectTransform, 12.0f);

            var field = fieldGo.AddComponent<InputField>();
            field.targetGraphic = fieldImage;
            field.placeholder = placeholder;
            field.textComponent = typed;
            field.text = playerName == "Player" &&
                         string.IsNullOrWhiteSpace(Settings.SettingsStore.Current.PlayerName)
                ? ""
                : playerName;
            field.characterLimit = Core.Balance.PlayerNameMax;
            field.lineType = InputField.LineType.SingleLine;
            field.onEndEdit.AddListener(raw =>
            {
                string clean = Settings.GameSettings.SanitiseName(raw);
                Settings.SettingsStore.Current.PlayerName = clean;
                Settings.SettingsStore.Save();
                field.SetTextWithoutNotify(clean);
            });
        }

        /// <summary>
        /// The two tab buttons, handed back to the screen that owns them.
        ///
        /// ⚠️⚠️ THEY ARE NOT KEPT IN A STATIC. `LobbyChrome` is a static helper and a static field
        /// holding a scene object survives the scene that made it: a second load of `MatchSetup`
        /// would find a reference to a destroyed button that still answers a C# `!= null` check
        /// only until Unity's overload runs, and the tab would look wired and do nothing. The
        /// screen holds these for exactly as long as it exists.
        /// </summary>
        public sealed class Tabs
        {
            public Button Practice;
            public Button Multiplayer;
            public GameObject LobbyDrawer;

            /// <summary>
            /// ⚠️ THE VARIATION IS SWAPPED AND RE-APPLIED, NOT THE IMAGE COLOUR. `GodotButton`
            /// carries five authored states per variation and writes the Image itself on hover,
            /// press and disable; tinting the graphic directly is overwritten by whichever state
            /// the skin resolves next, which reads as a tab that forgets it is selected the first
            /// time the mouse crosses it.
            /// </summary>
            public void SetActive(bool lobby)
            {
                Paint(Practice, !lobby);
                Paint(Multiplayer, lobby);
                if (LobbyDrawer != null) LobbyDrawer.SetActive(lobby);
            }

            private static void Paint(Button button, bool active)
            {
                if (button == null) return;

                var skin = button.GetComponent<GodotButton>();
                if (skin == null) return;

                skin.Variation = active ? "WoodAmberButton" : "WoodButton";
                skin.Apply();
                skin.Refresh();
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE SCRIM CHANGES SHAPE, NOT STRENGTH, AND THAT IS THE WHOLE DIFFERENCE BETWEEN
        /// "the arena is the background" AND "the arena is the picture". It is authored as one
        /// full-screen dim over the live map, which is correct when two opaque panels sit in the
        /// middle of the frame and there is nothing else to look at. With four characters standing
        /// in the middle of that frame it is a grey sheet over the only thing worth seeing.
        ///
        /// Two vertical gradients do the same job for the text: dark at the top where the banner
        /// and the tabs sit, dark at the bottom where the furniture sits, and clean through the
        /// middle band where the cast is. The alpha at the edges is the authored value, so nothing
        /// gets HARDER to read than it already was.
        ///
        /// ⚠️ THE GRADIENT IS A GENERATED TEXTURE RATHER THAN A STACK OF PLATES. Two coplanar
        /// translucent plates sort arbitrarily, which `VISION.md` § 2 rule 3 records shipping a
        /// trail that drew a different colour per drop; a vertical ramp in one texture cannot.
        /// </summary>
        private static void SoftenScrim(Transform root, Func<string, Transform> find)
        {
            var scrim = find("Scrim");
            if (scrim == null) return;

            var image = scrim.GetComponent<Image>();
            if (image == null) return;

            Color authored = image.color;

            // The flat sheet goes; the two bands carry its weight at the edges.
            image.color = new Color(authored.r, authored.g, authored.b, authored.a * 0.18f);
            image.raycastTarget = false;

            // ⚠️⚠️ THE BANDS ARE INK, NOT THE AUTHORED SCRIM COLOUR, AND REUSING THAT COLOUR
            // WASHED THE BOTTOM THIRD OF THE SCREEN OUT. `Logs/shots-runtime/Lobby-v8.png` has a
            // pale grey haze over the road and the cast's legs with a visible horizontal edge
            // where it starts, because the authored scrim is a LIGHT wash: correct as a flat dim
            // over a whole screen with two opaque panels on it, and exactly backwards as a
            // gradient whose job is to make cream text read over a bright street.
            //
            // ⚠️ `UiTheme.Ink` IS THE RIGHT COLOUR BY THE PALETTE'S OWN RULE. Its entry is "text,
            // borders, pressed fills": it is already what every dark edge in this UI is made of,
            // and darkening under light type is the one thing a scrim is for.
            var band = new Color(UiTheme.Ink.r, UiTheme.Ink.g, UiTheme.Ink.b, 1.0f);

            Band(scrim, "ScrimTop", band, TopBandFraction, TopBandAlpha, fromTop: true);
            Band(scrim, "ScrimBottom", band, BottomBandFraction, BottomBandAlpha, fromTop: false);
        }

        private static void Band(Transform sibling, string name, Color tint, float fraction,
                                 float alpha, bool fromTop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(sibling.parent, false);
            go.transform.SetSiblingIndex(sibling.GetSiblingIndex() + 1);

            var image = go.AddComponent<Image>();
            image.sprite = Ramp(fromTop);
            image.type = Image.Type.Simple;
            image.color = new Color(tint.r, tint.g, tint.b, Mathf.Clamp01(alpha));
            image.raycastTarget = false;

            var rt = image.rectTransform;

            if (fromTop)
            {
                rt.anchorMin = new Vector2(0.0f, 1.0f - fraction);
                rt.anchorMax = new Vector2(1.0f, 1.0f);
            }
            else
            {
                rt.anchorMin = new Vector2(0.0f, 0.0f);
                rt.anchorMax = new Vector2(1.0f, fraction);
            }

            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Sprite _rampDown;
        private static Sprite _rampUp;

        /// <summary>
        /// A one-pixel-wide vertical alpha ramp, cached.
        ///
        /// ⚠️ THE CURVE IS SQUARED RATHER THAN LINEAR. A linear ramp has a visible edge where it
        /// reaches zero, because the eye finds the discontinuity in the FIRST DERIVATIVE, not in
        /// the value. Squaring puts the fade's own falloff to zero at the same point and the band
        /// ends without a line across the screen.
        /// </summary>
        private static Sprite Ramp(bool fromTop)
        {
            ref Sprite cached = ref fromTop ? ref _rampDown : ref _rampUp;
            if (cached != null) return cached;

            const int steps = 64;

            var tex = new Texture2D(1, steps, TextureFormat.RGBA32, false)
            {
                name = fromTop ? "ScrimRampDown" : "ScrimRampUp",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            for (int y = 0; y < steps; y++)
            {
                // v is 0 at the screen edge and 1 where the band meets the clean middle.
                float v = y / (float)(steps - 1);
                float toEdge = fromTop ? v : 1.0f - v;

                float alpha = toEdge * toEdge;
                tex.SetPixel(0, y, new Color(1.0f, 1.0f, 1.0f, alpha));
            }

            tex.Apply();

            cached = Sprite.Create(tex, new Rect(0, 0, 1, steps), new Vector2(0.5f, 0.5f), 100.0f);
            cached.name = tex.name;

            return cached;
        }

        /// <summary>
        /// Pushes the two authored columns into the bottom corners and lets the middle of the
        /// frame belong to the arena.
        ///
        /// ⚠️⚠️ THE PARENT'S LAYOUT GROUP IS DISABLED, NOT DELETED. `Columns` is a
        /// `HorizontalLayoutGroup` that centres its two children and drives their rects every
        /// frame; leaving it on would fight every anchor set below and win, because a layout group
        /// writes its children's rects during the layout pass and an anchor set from a script runs
        /// before it. Disabling rather than destroying is what makes `Classic` a one-line revert:
        /// the component and its authored spacing, padding and alignment are all still there.
        ///
        /// ⚠️ AND EACH COLUMN GETS A `ContentSizeFitter`. Once the parent stops driving them their
        /// height is whatever the rect says, which for a layout-driven node is the 100x100
        /// placeholder every converted container carries. Fitting to preferred height is what
        /// makes a column as tall as the rows inside it, and anchoring the pivot to the BOTTOM is
        /// what makes it grow upward from the corner instead of down off the screen.
        /// </summary>
        private static GameObject MoveColumns(Func<string, Transform> find)
        {
            var columns = find("Columns");
            var left = find("LeftColumn");
            var right = find("RightColumn");

            if (columns == null || left == null || right == null)
            {
                Debug.LogWarning("[LobbyChrome] the authored columns are missing; " +
                                 "keeping the Classic arrangement.");
                return null;
            }

            var group = columns.GetComponent<LayoutGroup>();
            if (group != null) group.enabled = false;

            var columnsRect = columns as RectTransform;
            if (columnsRect != null) MenuKit.Stretch(columnsRect, 0.0f);

            // ⚠️⚠️ THE ACTION STACK GOES UP TOO. START, the status line and BACK were the last
            // thing left in the bottom half, and leaving them there meant the middle was clear and
            // the bottom-left was not, which is the same complaint one corner over. Everything the
            // player operates lives in the top band now, and everything below it is the room.
            Corner(left as RectTransform, LeftWidth, toLeft: true, toTop: true);

            if (left is RectTransform leftRect)
                leftRect.anchoredPosition = new Vector2(EdgeMargin, -ActionStackY);

            // ⚠️ THE LOBBY CARD GOES TOP-RIGHT, OPPOSITE THE SETTINGS. Once `LiftSettings` moved
            // the four cyclers under the banner and the `P1..P4` rows were removed, the right card
            // was a short block floating alone in the bottom corner with a screen of empty road
            // above it. Both blocks of INFORMATION now sit along the top and the bottom belongs to
            // the two things you ACT with: START and the chat.
            // ⚠️ THE LOBBY CARD HUGS THE TOP EDGE, level with the tabs rather than level with
            // the settings. It is the shortest block on the screen and it was leaving a band of
            // empty road above it.
            Corner(right as RectTransform, RightWidth, toLeft: false, toTop: true);

            GameObject lobbyDrawer = null;
            if (right is RectTransform rightRect)
            {
                rightRect.anchoredPosition = new Vector2(-EdgeMargin, -(CardY + 68.0f));
                lobbyDrawer = BuildLobbyDrawer(rightRect);
            }

            LiftSettings(find, left);
            return lobbyDrawer;
        }

        /// <summary>Keeps network mechanics available without permanently covering the fourth
        /// character. The card opens on demand; chat follows the bottom edge of whichever state
        /// is visible.</summary>
        private static GameObject BuildLobbyDrawer(RectTransform details)
        {
            if (details == null || details.parent == null) return null;

            var toggle = MenuKit.WoodButton(details.parent, "LOBBY & SERVERS  ▼", Vector2.one,
                                            new Vector2(-EdgeMargin, -CardY),
                                            new Vector2(RightWidth, 58.0f), null,
                                            "WoodAmberButton");
            toggle.name = "LobbyDrawerToggle";
            toggle.transform.localScale = new Vector3(RightScale, RightScale, 1.0f);

            var toggleRect = toggle.transform as RectTransform;
            if (toggleRect != null)
            {
                toggleRect.anchorMin = Vector2.one;
                toggleRect.anchorMax = Vector2.one;
                toggleRect.pivot = Vector2.one;
            }

            var label = toggle.GetComponentInChildren<Text>();
            details.gameObject.SetActive(false);
            bool open = false;

            toggle.onClick.AddListener(() =>
            {
                open = !open;
                details.gameObject.SetActive(open);
                if (label != null)
                    label.text = open ? "CLOSE LOBBY DETAILS  ▲" : "LOBBY & SERVERS  ▼";

                var canvas = details.GetComponentInParent<Canvas>();
                var chat = canvas != null ? canvas.GetComponentInChildren<LobbyChat>(true) : null;
                chat?.PlaceBelowTopRight(144.0f, open ? 548.0f : 300.0f, 392.0f);
            });

            return toggle.gameObject;
        }

        /// <summary>
        /// Moves the MAP / MODE / BOTS / CHARACTER block up under the banner, and leaves the
        /// action buttons in the bottom corner.
        ///
        /// ⚠️⚠️ THE LEFT COLUMN WAS TWO DIFFERENT THINGS IN ONE STACK. 🧑 2026-08-28, pointing at
        /// the settings block sitting above START: *"maybe put this right below lobby? looks ugly
        /// there"*. They are two different KINDS of control and the reference separates them for a
        /// reason: the four cyclers are SETTINGS, which you read and adjust before you are ready,
        /// and START is the ACTION, which wants to be alone in the corner your hand rests in.
        /// Stacked together, the action reads as the fifth row of the settings.
        ///
        /// ⚠️ THE NODES ARE REPARENTED, NOT REBUILT. `ConvertedScreen` indexes every node by name
        /// in `Start`, BEFORE this runs, and it holds `Transform` references: moving one to a new
        /// parent does not change what `Node("MapValueLabel")` returns. Rebuilding them would.
        ///
        /// ⚠️ AND THE DETAIL LINE GOES WITH THEM. "ESKINITA  Urban side street" is a caption on
        /// the MAP row, not a status line; leaving it at the bottom would strand it under a START
        /// button describing a map picker that is no longer next to it.
        /// </summary>
        private static void LiftSettings(Func<string, Transform> find, Transform leftColumn)
        {
            var config = find("ConfigPanel");
            if (config == null || leftColumn == null) return;

            var host = new GameObject("SettingsDrawer");
            host.transform.SetParent(leftColumn.parent, false);

            var rect = host.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.0f, 1.0f);
            rect.anchorMax = new Vector2(0.0f, 1.0f);
            rect.pivot = new Vector2(0.0f, 1.0f);
            rect.anchoredPosition = new Vector2(EdgeMargin, -SettingsY);
            rect.sizeDelta = new Vector2(SettingsWidth, 100.0f);

            var layout = host.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            // The closed rail is deliberately narrower than the expanded controls. Let each
            // child keep its own width so opening the drawer can grow right temporarily without
            // making the always-visible header cover the first character.
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;

            var fitter = host.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // The drawer header is the only furniture visible until the player asks to edit the
            // match. This follows the lobby hierarchy in the reference: cast first, settings on
            // demand, primary action always available.
            var toggle = MenuKit.WoodButton(host.transform, "MATCH SETTINGS  ▼", Vector2.zero,
                                            Vector2.zero,
                                            new Vector2(SettingsCollapsedWidth, SettingsHeaderHeight), null,
                                            "WoodAmberButton");
            var toggleElement = toggle.gameObject.AddComponent<LayoutElement>();
            toggleElement.minHeight = SettingsHeaderHeight;
            toggleElement.preferredHeight = SettingsHeaderHeight;

            var summary = MenuKit.Label(host.transform, "", 18, UiTheme.CreamMuted,
                                        Vector2.zero, Vector2.zero,
                                        new Vector2(SettingsCollapsedWidth, 28.0f),
                                        TextAnchor.MiddleLeft);
            summary.raycastTarget = false;
            var summaryElement = summary.gameObject.AddComponent<LayoutElement>();
            summaryElement.minHeight = 28.0f;
            summaryElement.preferredHeight = 28.0f;

            var body = new GameObject("SettingsBody");
            body.transform.SetParent(host.transform, false);
            var bodyImage = body.AddComponent<Image>();
            bodyImage.rectTransform.sizeDelta = new Vector2(SettingsWidth, SettingsBodyHeight);
            bodyImage.sprite = GodotTheme.WoodBox(UiTheme.WoodDeep, UiTheme.WoodEdge);
            bodyImage.type = Image.Type.Sliced;
            bodyImage.color = Color.white;

            var bodyElement = body.AddComponent<LayoutElement>();
            bodyElement.minHeight = SettingsBodyHeight;
            bodyElement.preferredHeight = SettingsBodyHeight;

            var bodyLayout = body.AddComponent<VerticalLayoutGroup>();
            bodyLayout.padding = new RectOffset(18, 18, 16, 16);
            bodyLayout.spacing = 8;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;

            // Use the authored rows and their wired buttons, but not ConfigPanel's oversized
            // 634x540 face. The face is what made a few controls read as a giant opaque card.
            var rows = Descend(config, "Rows");
            if (rows != null)
            {
                rows.SetParent(body.transform, false);
                var rowsElement = rows.GetComponent<LayoutElement>();
                if (rowsElement == null) rowsElement = rows.gameObject.AddComponent<LayoutElement>();
                rowsElement.minHeight = 338.0f;
                rowsElement.preferredHeight = 338.0f;
                rowsElement.flexibleHeight = 0.0f;
                Narrow(rows as RectTransform, SettingsWidth - 36.0f);
            }

            var detail = find("DetailBox");
            if (detail != null)
            {
                detail.SetParent(body.transform, false);
                var detailElement = detail.GetComponent<LayoutElement>();
                if (detailElement == null)
                    detailElement = detail.gameObject.AddComponent<LayoutElement>();
                detailElement.minHeight = 44.0f;
                detailElement.preferredHeight = 44.0f;
            }

            config.gameObject.SetActive(false);

            Func<string, string> value = name =>
            {
                var node = find(name);
                var label = node != null ? node.GetComponent<Text>() : null;
                if (label == null && node != null) label = node.GetComponentInChildren<Text>();
                return label != null ? label.text.Trim() : "?";
            };

            Action refreshSummary = () =>
            {
                summary.text = $"{value("MapValueLabel")}  •  {value("ModeValueLabel")}  •  " +
                               $"{value("DifficultyValueLabel")}";
                MenuKit.Fit(summary, SettingsCollapsedWidth - 18.0f, 13);
            };

            var toggleLabel = toggle.GetComponentInChildren<Text>();
            bool open = false;
            body.SetActive(false);
            refreshSummary();

            toggle.onClick.AddListener(() =>
            {
                open = !open;
                body.SetActive(open);
                summary.gameObject.SetActive(!open);
                if (toggleLabel != null)
                    toggleLabel.text = open ? "CLOSE MATCH SETTINGS  ▲" : "MATCH SETTINGS  ▼";
                if (!open) refreshSummary();
            });
        }

        /// <summary>
        /// Swaps a converted row between stacked and side by side.
        ///
        /// ⚠️⚠️ THE COMPONENT IS REPLACED, BECAUSE A LAYOUT GROUP'S ORIENTATION IS ITS TYPE.
        /// `HorizontalLayoutGroup` and `VerticalLayoutGroup` are two classes with no shared switch,
        /// so turning a row is destroy-and-add, and the authored spacing, padding and alignment are
        /// carried across by hand rather than re-picked: they are what makes the row look like the
        /// rest of this UI.
        ///
        /// ⚠️ `DestroyImmediate`, NOT `Destroy`. `Destroy` defers to the end of the frame, so
        /// the new group would spend this frame fighting the old one over the same children and
        /// the layout that lands is whichever ran last. This is a controlled one-shot at screen
        /// build, which is the case `DestroyImmediate` is for.
        /// </summary>
        /// <summary>
        /// The first descendant of <paramref name="root"/> with this name.
        ///
        /// ⚠️ SCOPED TO A SUBTREE ON PURPOSE. `ConvertedScreen.Node` searches the whole screen
        /// and `MatchSetup` carries two `Rows`, two `Face`s, two `Shadow`s and four `Label`s;
        /// which one you get is tree order, which is not a thing a reader can see. Everything this
        /// class reaches for inside a panel is found from that panel.
        /// </summary>
        private static Transform Descend(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var hit = Descend(root.GetChild(i), name);
                if (hit != null) return hit;
            }

            return null;
        }

        private static void Corner(RectTransform column, float width, bool toLeft, bool toTop)
        {
            if (column == null) return;

            float y = toTop ? 1.0f : 0.0f;

            column.anchorMin = new Vector2(toLeft ? 0.0f : 1.0f, y);
            column.anchorMax = new Vector2(toLeft ? 0.0f : 1.0f, y);
            column.pivot = new Vector2(toLeft ? 0.0f : 1.0f, y);
            column.anchoredPosition = new Vector2(toLeft ? EdgeMargin : -EdgeMargin,
                                                  toTop ? -SettingsY : BottomMargin);
            column.sizeDelta = new Vector2(width, column.sizeDelta.y);

            // ⚠️⚠️ THE SIZE IS SET **AND** THE COLUMN IS SCALED, AND THE SCALE IS THE HALF THAT
            // ACTUALLY WORKS. Three renders in a row (`Logs/shots-runtime/Lobby-v2..v5.png`) came
            // back with an 820 px panel inside a rect that `LobbyChrome.ReportColumns` measured at
            // 580, because a rect handed to a layout system is a REQUEST: the authored
            // `VerticalLayoutGroup`, its children's `LayoutElement` minimums and their own
            // `ContentSizeFitter`s each get to overrule it, and `Narrow` below only reaches the
            // first two. `localScale` is outside that argument entirely. Nothing in Unity's layout
            // reads it, so it cannot be overruled, and it shrinks the panel WITH its type, its
            // borders and its spacing, which is what "compact furniture" means and what setting a
            // width alone would not have done even if it had held.
            //
            // ⚠️ THE PIVOT IS THE CORNER THE COLUMN IS ANCHORED TO, so it shrinks TOWARD that
            // corner and the margin stays the margin. With a centred pivot the same scale would
            // have pulled the panel away from the edge by half the difference.
            float scale = toLeft ? LeftScale : RightScale;
            column.localScale = new Vector3(scale, scale, 1.0f);

            var fitter = column.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = column.gameObject.AddComponent<ContentSizeFitter>();

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Narrow(column, width);
        }

        /// <summary>
        /// Makes the column's contents actually the column's width.
        ///
        /// ⚠️⚠️ SETTING THE COLUMN'S OWN `sizeDelta` IS NOT ENOUGH AND `Logs/shots-runtime/
        /// Lobby-v3.png` IS THE PROOF: the column was set to 580 and the config panel inside it
        /// still measured 820 on screen, so the clear band the cast stands in was 240 px narrower
        /// than the arithmetic said and the two left-hand characters were behind the furniture
        /// from the knee up.
        ///
        /// The cause is that the authored `VerticalLayoutGroup` ships with `childControlWidth`
        /// OFF, which means it POSITIONS its children and does not SIZE them: a child keeps
        /// whatever width the .tscn gave it and simply overhangs a parent that got smaller.
        /// Turning control on, and forcing expansion so a narrower child grows back to the new
        /// width rather than sitting in the middle of it, is what makes the number mean something.
        ///
        /// ⚠️ THE CHILDREN'S OWN `LayoutElement` IS OVERRIDDEN TOO, because a `preferredWidth`
        /// authored on the panel outranks the group's expansion and would win.
        /// </summary>
        private static void Narrow(RectTransform column, float width)
        {
            var group = column.GetComponent<HorizontalOrVerticalLayoutGroup>();

            if (group != null)
            {
                group.childControlWidth = true;
                group.childForceExpandWidth = true;
            }

            for (int i = 0; i < column.childCount; i++)
            {
                var child = column.GetChild(i) as RectTransform;
                if (child == null) continue;

                var element = child.GetComponent<LayoutElement>();

                if (element != null)
                {
                    // ⚠️⚠️ `minWidth` AS WELL AS `preferredWidth`, AND ONLY DOING THE SECOND IS
                    // WHY `Logs/shots-runtime/Lobby-v4.png` STILL HAS AN 820 px PANEL IN A 580 px
                    // COLUMN. Unity's layout resolves a child's width as at least its `minWidth`
                    // whatever the group wants, so an authored minimum outranks both the group's
                    // control and its expansion. The BACK button, which has no authored minimum,
                    // stretched to the new width in that same frame: two children of one group
                    // disagreeing is what named the cause.
                    if (element.minWidth > 0.0f) element.minWidth = width;
                    if (element.preferredWidth > 0.0f) element.preferredWidth = width;
                }

                // ⚠️ AND A `ContentSizeFitter` ON THE CHILD OUTRANKS EVERYTHING ABOVE, because it
                // writes the rect itself after the group has finished. The horizontal half has to
                // stand down; the vertical half is usually the only reason the fitter is there,
                // so it is left alone.
                var fitter = child.GetComponent<ContentSizeFitter>();
                if (fitter != null) fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                child.sizeDelta = new Vector2(width, child.sizeDelta.y);
            }
        }

        /// <summary>
        /// Reports what the columns actually ended up being, once the layout has run.
        ///
        /// ⚠️⚠️ THIS IS HERE BECAUSE THREE RENDERS IN A ROW DISAGREED WITH THE ARITHMETIC AND
        /// NOTHING COULD SAY WHY. A screenshot shows that a panel is too wide; it cannot show
        /// whether the column was set correctly and a child overhung it, whether the anchor was
        /// wrong, or whether a `ContentSizeFitter` rewrote the rect afterwards, and those three
        /// have three different fixes. `UiProbe`'s header makes the same argument about a white
        /// rectangle having four indistinguishable causes.
        /// </summary>
        public static void ReportColumns(Func<string, Transform> find)
        {
            if (find == null) return;

            // `find` is ConvertedScreen.Node, whose index contains only nodes imported from the
            // authored scene. SettingsStrip is created by Apply at runtime, so asking that index
            // for it emits a missing-node error and aborts PlayMode screenshot tests. Its child
            // ConfigPanel is indexed and reports the same final bounds after reparenting.
            foreach (string name in new[] { "LeftColumn", "RightColumn", "ConfigPanel", "SeatPanel",
                                            "StartButton" })
            {
                var node = find(name) as RectTransform;
                if (node == null) continue;

                var corners = new Vector3[4];
                node.GetWorldCorners(corners);

                Debug.Log($"[LobbyChrome] {name} rect {node.rect.width:F0}x{node.rect.height:F0} " +
                          $"screen x {corners[0].x:F0}..{corners[2].x:F0} " +
                          $"y {corners[0].y:F0}..{corners[2].y:F0}");
            }
        }

        /// <summary>
        /// `PRACTICE` and `MULTIPLAYER` across the top, which is the one piece of the reference's
        /// navigation this game actually has two of.
        ///
        /// ⚠️⚠️ THE REFERENCE'S OTHER TABS ARE NOT INVENTED. PUBG's row is PLAY / CUSTOMIZATION /
        /// REWARDS / CAREER and the mobile shot's is RANK / SEASON / WORKSHOP / MISSIONS /
        /// INVENTORY. This game has none of those, and a nav bar of five tabs where three do
        /// nothing is worse than a nav bar of two that both work: a dead tab is a promise the
        /// build does not keep, and it is the first thing anybody clicks.
        ///
        /// ⚠️ SWITCHING IS IN PLACE, WITH NO SCENE LOAD. A reload here would tear down the map
        /// preview's cached arenas, both render textures and the whole cast, and `SceneFlow.Go`'s
        /// one-load-per-frame latch would not even deduplicate it, because that latch is scoped to
        /// a single frame on purpose.
        /// </summary>
        private static Tabs BuildTabs(Transform root, Func<string, Transform> find,
                                      bool isLobby, Action<bool> onTab)
        {
            var banner = find("Banner");
            Transform parent = banner != null ? banner.parent : root;

            var bar = new GameObject("LobbyTabBar");
            bar.transform.SetParent(parent, false);

            var barRect = bar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 1.0f);
            barRect.anchorMax = new Vector2(0.5f, 1.0f);
            barRect.pivot = new Vector2(0.5f, 1.0f);
            barRect.anchoredPosition = new Vector2(0.0f, -34.0f);
            barRect.sizeDelta = new Vector2((TabWidth * 2.0f) + 12.0f, TabHeight);

            var layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            return new Tabs
            {
                Practice = Tab(bar.transform, "PracticeTab", "PRACTICE", !isLobby,
                               () => onTab?.Invoke(false)),
                Multiplayer = Tab(bar.transform, "MultiplayerTab", "MULTIPLAYER", isLobby,
                                  () => onTab?.Invoke(true)),
            };
        }

        private static Button Tab(Transform parent, string name, string text, bool active,
                                  Action onClick)
        {
            // ⚠️ THE ACTIVE TAB USES THE PRIMARY VARIATION RATHER THAN A TINT. `GodotButton`
            // carries five authored states per variation, and colouring the Image directly fights
            // whichever state the skin writes next, which is how a hovered button ends up the
            // wrong colour a frame later.
            var button = MenuKit.WoodButton(parent, text, Vector2.zero, Vector2.zero,
                                            new Vector2(TabWidth, TabHeight), onClick,
                                            active ? "WoodAmberButton" : "WoodButton");
            button.name = name;

            var element = button.gameObject.AddComponent<LayoutElement>();
            element.minHeight = TabHeight;
            element.preferredHeight = TabHeight;

            var label = button.GetComponentInChildren<Text>();

            // ⚠️ FITTED, BECAUSE "MULTIPLAYER" IS ELEVEN CHARACTERS IN A 260 px BOX AND THE
            // AUTHORED WOOD BUTTON FONT IS SIZED FOR "BACK". See `MenuKit.Fit`, and the four
            // recorded times a label has run out of its box in this project.
            if (label != null) MenuKit.Fit(label, TabWidth - 44.0f);

            return button;
        }
    }
}
