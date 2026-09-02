using TumbangPreso.Abilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// The hold-to-read ability panel. Hold the key, the three powers slide in with their full
    /// descriptions; let go and they slide out.
    ///
    /// ⚠️⚠️ THIS EXISTS SO THE HUD CAN STAY QUIET. 🧑 2026-08-23: *"games like valorant
    /// overwatch league etc dont clog their screen with text, to see how abilities work they
    /// usually click a button and then let go when they dont wanna see it anymore"*. Correct,
    /// and it is the standard answer to a real tension: a player needs the full text exactly
    /// twice, when learning the hero and when they forget mid-match, and needs it gone the rest
    /// of the time. The deck at the bottom of the screen therefore carries only what is true
    /// RIGHT NOW (icon, key, name, whether it is up) and every sentence lives here.
    ///
    /// ⚠️ IT IS A HOLD, NOT A TOGGLE, AND THAT IS THE WHOLE INTERACTION. A toggle leaves the
    /// panel up when the player gets jumped, which is the moment it does the most damage. A
    /// hold cannot be left on by accident: the screen is clear the instant the hand moves.
    ///
    /// ⚠️ NOTHING HERE TOUCHES `InputIntent`, AND THAT IS DELIBERATE. `PlayerInputReader`'s note
    /// is that it is the only place that reads hardware, because a bot and a human must press
    /// the same table. This key changes no world state at all: it opens a panel on the local
    /// player's own screen. Routing it through the intent table would mean every AI unit
    /// carries a verb that can never do anything.
    /// </summary>
    public sealed class AbilityInspectPanel : MonoBehaviour
    {
        private const float SlideDistance = 45.0f;
        private const float OpenSpeed = 8.5f;
        private const float CloseSpeed = 12.0f;
        private const float Stagger = 0.12f;

        private CanvasGroup _group;
        private RectTransform _rt;
        private InputAction _hold;

        private readonly Card[] _cards = new Card[3];
        private Text _title;
        private Text _hint;

        private float _open;          // 0 closed, 1 open
        private HeroKit _boundKit;

        private sealed class Card
        {
            public RectTransform Rt;
            public CanvasGroup Group;
            public Image Tile;
            public Image Glyph;
            public Text Key;
            public Text Name;
            public Text Kind;
            public Text Body;
            public Text Meta;
        }

        /// <summary>
        /// Where the tray rests, measured up from the bottom of the screen.
        ///
        /// ⚠️⚠️ 196, NOT 16, AND AT 16 THIS PANEL WAS DRAWN THROUGH EVERY OTHER BOTTOM ELEMENT
        /// IN THE GAME. 🧑 2026-08-29, holding TAB in a Hero Strike match: *"broken ui placement
        /// here when u hold tab"*, with the three cards sitting on top of the ability deck.
        ///
        /// ⚠️ THE ARITHMETIC IS ALREADY WRITTEN DOWN IN `Hud`, WHICH IS HOW WRONG THIS WAS. That
        /// file works out the bottom band in full for the intermission lines: the hero deck spans
        /// **y 14 to 92** (`DeckBottomMargin` + `DeckHeight`), the Classic deck **24 to 124**, the
        /// inspect hint **132 to 150** and the ready prompt plate **156 to 190**. This tray is 236
        /// tall with a bottom pivot, so resting at 16 put it at **16 to 252**: straight through
        /// both decks, the hint that tells you to open it, and the prompt plate, all four.
        ///
        /// ⚠️ SO IT CLEARS THE TALLEST THING BELOW IT RATHER THAN BEING NUDGED. 196 is six px
        /// over the prompt plate's 190, which is the same clearance `Hud` leaves between its own
        /// two stacked lines. Stacking upward from the taller deck is what makes one number
        /// correct in both modes, and that is `Hud`'s own rule for this band.
        /// </summary>
        private const float RestY = 196.0f;

        public static AbilityInspectPanel Create(Transform parent)
        {
            var go = new GameObject("AbilityInspect", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var panel = go.AddComponent<AbilityInspectPanel>();
            panel.Build();
            return panel;
        }

        private void Build()
        {
            _rt = gameObject.GetComponent<RectTransform>();
            if (_rt == null) _rt = gameObject.AddComponent<RectTransform>();

            // Bottom-center horizontal tray inspired by modern hero shooters (Valorant style)
            _rt.anchorMin = new Vector2(0.5f, 0.0f);
            _rt.anchorMax = new Vector2(0.5f, 0.0f);
            _rt.pivot = new Vector2(0.5f, 0.0f);
            _rt.anchoredPosition = new Vector2(0, RestY);
            _rt.sizeDelta = new Vector2(1060, 276);

            // ⚠️ THE WOOD SET, NOT A SLATE-BLUE GLASS OF ITS OWN. This tray opens over a
            // live match with the wooden scoreboard and clock already on screen, so it was the
            // single worst place in the game for the imported cold palette to land. See
            // `UiTheme.HeroPlate`.
            var bg = gameObject.AddComponent<Image>();
            bg.sprite = GodotTheme.Box(UiTheme.HeroPlate, UiTheme.HeroRim, 2, 8);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;

            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0.0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            var column = gameObject.AddComponent<VerticalLayoutGroup>();
            column.childControlHeight = true;
            column.childControlWidth = true;
            column.childForceExpandHeight = false;
            column.childForceExpandWidth = true;
            column.spacing = 6.0f;
            column.padding = new RectOffset(14, 14, 10, 10);

            // ---- Top Header Row (Title on left, Hold Hint on right) ----
            var headerRow = new GameObject("HeaderRow", typeof(RectTransform));
            headerRow.transform.SetParent(transform, false);
            var headerHlg = headerRow.AddComponent<HorizontalLayoutGroup>();
            headerHlg.childControlHeight = true;
            headerHlg.childControlWidth = true;
            headerHlg.childForceExpandHeight = true;
            headerHlg.childForceExpandWidth = false;
            Height(headerRow, 26);

            _title = Label(headerRow.transform, "Title", 24, UiTheme.Amber, TextAnchor.MiddleLeft);
            _title.fontStyle = FontStyle.Bold;
            _title.text = "HERO POWERS";
            var titleLe = _title.gameObject.AddComponent<LayoutElement>();
            titleLe.flexibleWidth = 1.0f;

            _hint = Label(headerRow.transform, "Hint", MenuKit.MinReadableUnits, UiTheme.Cream,
                          TextAnchor.MiddleRight);
            _hint.text = "HOLD [TAB] TO INSPECT";
            var hintLe = _hint.gameObject.AddComponent<LayoutElement>();
            hintLe.minWidth = 220;

            // ---- Cards Row (3 side-by-side columns) ----
            var cardsRow = new GameObject("CardsRow", typeof(RectTransform));
            cardsRow.transform.SetParent(transform, false);
            var cardsHlg = cardsRow.AddComponent<HorizontalLayoutGroup>();
            cardsHlg.childControlHeight = true;
            cardsHlg.childControlWidth = true;
            cardsHlg.childForceExpandHeight = true;
            cardsHlg.childForceExpandWidth = true;
            cardsHlg.spacing = 10.0f;
            Height(cardsRow, 178);

            for (int i = 0; i < _cards.Length; i++)
            {
                _cards[i] = BuildCard(cardsRow.transform, i);
            }

            gameObject.SetActive(false);
        }

        private Card BuildCard(Transform parent, int slotIndex)
        {
            var card = new Card();

            var go = new GameObject($"AbilityCard_{slotIndex}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            card.Rt = (RectTransform)go.transform;
            card.Group = go.AddComponent<CanvasGroup>();

            var bg = go.AddComponent<Image>();
            bg.sprite = GodotTheme.Box(UiTheme.HeroPlateRaised, UiTheme.HeroRim, 2, 6);
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = false;

            var cardCol = go.AddComponent<VerticalLayoutGroup>();
            cardCol.childControlHeight = true;
            cardCol.childControlWidth = true;
            cardCol.childForceExpandHeight = false;
            cardCol.childForceExpandWidth = true;
            cardCol.spacing = 6.0f;
            cardCol.padding = new RectOffset(10, 10, 8, 8);

            // Top section: Icon Tile + Name + Role + Cooldown
            var topSection = new GameObject("TopSection", typeof(RectTransform));
            topSection.transform.SetParent(go.transform, false);
            var topHlg = topSection.AddComponent<HorizontalLayoutGroup>();
            topHlg.childControlHeight = true;
            topHlg.childControlWidth = true;
            topHlg.childForceExpandHeight = true;
            topHlg.childForceExpandWidth = false;
            topHlg.spacing = 8.0f;
            Height(topSection, 52);

            // Icon tile with key badge
            var tileGo = new GameObject("Tile");
            tileGo.transform.SetParent(topSection.transform, false);
            card.Tile = tileGo.AddComponent<Image>();
            card.Tile.sprite = GodotTheme.Box(UiTheme.HeroPlateSunk, UiTheme.HeroRim, 2, 6);
            card.Tile.type = Image.Type.Sliced;
            card.Tile.raycastTarget = false;

            var tileLe = tileGo.AddComponent<LayoutElement>();
            tileLe.minWidth = 50;
            tileLe.preferredWidth = 50;
            tileLe.minHeight = 50;
            tileLe.preferredHeight = 50;
            tileLe.flexibleHeight = 0.0f;

            var glyphGo = new GameObject("Glyph");
            glyphGo.transform.SetParent(tileGo.transform, false);
            card.Glyph = glyphGo.AddComponent<Image>();
            card.Glyph.color = UiTheme.HeroGlyphOn;
            card.Glyph.preserveAspect = true;
            card.Glyph.raycastTarget = false;
            MenuKit.Stretch(card.Glyph.rectTransform);
            card.Glyph.rectTransform.offsetMin = new Vector2(6, 6);
            card.Glyph.rectTransform.offsetMax = new Vector2(-6, -6);

            var chipGo = new GameObject("KeyChip");
            chipGo.transform.SetParent(tileGo.transform, false);
            var chip = chipGo.AddComponent<Image>();
            chip.sprite = GodotTheme.Box(UiTheme.WoodDark, new Color(0, 0, 0, 0), 0, 4);
            chip.type = Image.Type.Sliced;
            chip.raycastTarget = false;
            var chipRt = chip.rectTransform;
            chipRt.anchorMin = new Vector2(1.0f, 0.0f);
            chipRt.anchorMax = new Vector2(1.0f, 0.0f);
            chipRt.pivot = new Vector2(1.0f, 0.0f);
            chipRt.anchoredPosition = new Vector2(-1, 1);
            chipRt.sizeDelta = new Vector2(24, 18);

            card.Key = Label(chipGo.transform, "Key", MenuKit.MinReadableUnits, UiTheme.Cream,
                             TextAnchor.MiddleCenter);
            card.Key.fontStyle = FontStyle.Bold;
            MenuKit.Stretch(card.Key.rectTransform);

            // Name + Role header stack
            var nameStack = new GameObject("NameStack", typeof(RectTransform));
            nameStack.transform.SetParent(topSection.transform, false);
            var nameCol = nameStack.AddComponent<VerticalLayoutGroup>();
            nameCol.childControlHeight = true;
            nameCol.childControlWidth = true;
            nameCol.childForceExpandHeight = false;
            nameCol.childForceExpandWidth = true;
            nameCol.spacing = 1.0f;
            var nameStackLe = nameStack.AddComponent<LayoutElement>();
            nameStackLe.flexibleWidth = 1.0f;

            // ⚠️⚠️ THE BOX IS 1.35x THE TYPE, AND AT 1:1 THE NAME AND THE COOLDOWN DREW ON TOP OF
            // EACH OTHER. 🧑 2026-08-29, of this tray in a live match: *"tab is unreadable and so
            // much fucked of text overflow and format"*, with `DEMONIC CARAPACE` overlapping
            // `62s CD · 4s` and `TITAN FISSURE` overlapping `OBJECTIVE`.
            //
            // A 21-unit label was being forced into a 22 px box and an 18-unit one into 18 px.
            // Legacy `Text` draws at its font's LINE HEIGHT, which is meaningfully taller than
            // the point size, so a box sized to the number clips the glyphs; and because
            // `nameCol` has `childControlHeight`, the group holds each child at that height and
            // the surplus is simply painted over the row underneath. **A text box sized to its
            // font size is not a text box that fits its text.**
            //
            // ⚠️ 1.35 IS THE RATIO THE PICKER ALREADY USES. `ConvertedCharacterSelect.
            // HeroTaglineHeight` multiplies a measured line by 1.35 for the same reason, and
            // `HeroPickerLayoutProbe`'s `MaxSlack` note records that a real line measures about
            // 1.16, so 1.35 is one line plus a descender rather than a guess.
            card.Name = Label(nameStack.transform, "Name", 21, UiTheme.Cream, TextAnchor.MiddleLeft);
            card.Name.fontStyle = FontStyle.Bold;
            Height(card.Name.gameObject, 28);

            var metaRow = new GameObject("MetaRow", typeof(RectTransform));
            metaRow.transform.SetParent(nameStack.transform, false);
            var metaHlg = metaRow.AddComponent<HorizontalLayoutGroup>();
            metaHlg.childControlHeight = true;
            metaHlg.childControlWidth = true;
            metaHlg.childForceExpandHeight = true;
            metaHlg.childForceExpandWidth = false;
            metaHlg.spacing = 6.0f;
            // ⚠️ 24, NOT 18, FOR THE REASON ON `card.Name` DIRECTLY ABOVE: an 18-unit label in an
            // 18 px box paints its descenders over whatever is under it.
            Height(metaRow, 24);

            card.Kind = Label(metaRow.transform, "Kind", MenuKit.MinReadableUnits, UiTheme.Amber,
                              TextAnchor.MiddleLeft);
            card.Kind.fontStyle = FontStyle.Bold;

            card.Meta = Label(metaRow.transform, "Meta", MenuKit.MinReadableUnits, UiTheme.Highlight,
                              TextAnchor.MiddleLeft);
            card.Meta.fontStyle = FontStyle.Bold;

            // ⚠️⚠️ EVERY LABEL ON THIS PANEL IS AT `MenuKit.MinReadableUnits` OR ABOVE AS OF
            // 2026-08-29, AND FIVE OF THEM WERE BELOW IT. 🧑: *"mahirap basahin yung text sa
            // skill description"*. The body was 15 units, the kind and the cooldown 14, the key
            // chip and the header hint 15, against a floor of 18 that `AspectRatioProbes`
            // already asserts for exactly this reason and that `LobbyChrome.BuildIdentity`'s note
            // cites by name when it rejected a 14-unit caption for being unreadable.
            //
            // ⚠️⚠️ AND THIS PANEL IS THE WORST PLACE IN THE GAME TO HAVE BEEN UNDER IT.
            // `docs/VISION.md` § 3 gives the ability text exactly three homes: LEARN on character
            // select, RECALL behind the hold key, PLAY on the deck. The deck deliberately carries
            // no sentences at all, so this tray is where a player is meant to actually READ what
            // a power does. Prose nobody can read at the one place it is allowed to be prose
            // makes the whole three-layer answer fail at its middle layer.
            //
            // ⚠️ THE HINT ALSO CAME OFF `CreamMuted`. Muted is for a label whose job is to be
            // ignorable; this one tells you which key you are holding to keep the panel open.
            //
            // ⚠️ `minHeight` MOVED WITH THE TYPE, 90 to 108. The note below says the card is tall
            // enough for four lines AT THIS SIZE, which stopped being true the moment the size
            // changed: four lines of 15 is 90 and four of 18 is 108. A floor left behind is how
            // an `Overflow` label starts drawing over the card under it.
            // Description body with generous padding and clean font
            // ⚠️⚠️ THE TRAY IS THE ONE PLACE THAT DOES NOT TRUNCATE. It exists to hold the
            // sentences the deck deliberately refuses to carry, so cutting them off here would
            // leave the full text nowhere in the game at all. `Overflow` rather than
            // `Truncate`, and the card is tall enough for four lines at this size.
            // ⚠️⚠️ 21, NOT `MinReadableUnits`. 🧑 2026-08-29, of these cards: *"also the texthere
            // is hard to read"*. `MenuKit.MinReadableUnits` is 18 and it is a FLOOR — the size
            // below which a fitter is forbidden to shrink text — and this label was authored AT
            // the floor, so the one panel in the game whose entire job is to be read was set to
            // the smallest type the project permits anywhere.
            //
            // ⚠️ IT IS AFFORDABLE HERE AND NOWHERE ELSE, which is why this is not a global
            // change. Every other 18 in the HUD is a label competing with the match for space
            // while the round runs; this tray only exists while the player is HOLDING a key and
            // deliberately not playing, so it may spend the room. `VISION.md` § 3 is the rule it
            // serves: the deck carries no sentences and every sentence lives behind this hold.
            const int BodySize = 20;

            card.Body = Label(go.transform, "Body", BodySize, UiTheme.Cream,
                              TextAnchor.UpperLeft);
            card.Body.horizontalOverflow = HorizontalWrapMode.Wrap;
            card.Body.verticalOverflow = VerticalWrapMode.Overflow;
            var bodyLe = card.Body.gameObject.AddComponent<LayoutElement>();
            bodyLe.flexibleHeight = 1.0f;

            // ⚠️ THE FLOOR MOVES WITH THE SIZE, AND THE NOTE DIRECTLY ABOVE THIS BLOCK IS THE
            // WARNING FOR EXACTLY THIS: *"a floor left behind is how an `Overflow` label starts
            // drawing over the card under it"*. Four lines at 21 is 126, where four at 18 was
            // 108. The panel's own height grows by the same 18 px.
            bodyLe.minHeight = 120;

            return card;
        }

        // ------------------------------------------------------------------ runtime

        public void Bind(HeroKit kit)
        {
            if (kit == null || kit == _boundKit) return;

            _boundKit = kit;
            Color hero = UiTheme.ColorForHero(kit.HeroId);
            _title.text = (kit.HeroName + "  ·  HERO POWERS").ToUpperInvariant();
            _title.color = hero;

            Fill(_cards[0], kit.Skill1, "Skill1", hero);
            Fill(_cards[1], kit.Skill2, "Skill2", hero);
            Fill(_cards[2], kit.Ultimate, "Ultimate", hero);
        }

        private static void Fill(Card card, HeroAbility ability, string action, Color hero)
        {
            if (card == null) return;

            if (ability == null)
            {
                card.Rt.gameObject.SetActive(false);
                return;
            }

            card.Rt.gameObject.SetActive(true);
            card.Tile.color = Color.white;
            card.Glyph.sprite = AbilityIcons.For(ability.Glyph);
            card.Glyph.color = hero;
            card.Key.text = Hud.KeyLabelFor(action);
            card.Name.text = ability.Name;
            card.Name.color = hero;
            card.Kind.text = $"[{AbilityIcons.LabelFor(ability.Glyph)}]";
            card.Kind.color = hero;
            card.Body.text = ability.Description;

            if (ability.Cooldown > 0.0f)
            {
                card.Meta.text = ability.Duration > 0.0f
                    ? $"· {ability.Cooldown:0.#}s CD ({ability.Duration:0.#}s DURATION)"
                    : $"· {ability.Cooldown:0.#}s CD";
            }
            else if (ability.UsesCharges)
            {
                card.Meta.text = ability.MaxCharges == 1 ? "· 1 CHARGE" : $"· {ability.MaxCharges} CHARGES";
            }
            else
            {
                card.Meta.text = "· OBJECTIVE CHARGE";
            }
        }

        public void Tick(HeroKit kit, float dt)
        {
            if (kit == null)
            {
                if (gameObject.activeSelf && _open <= 0.0f) gameObject.SetActive(false);
                return;
            }

            Bind(kit);

            bool held = HoldPressed();
            float speed = held ? OpenSpeed : CloseSpeed;
            _open = Mathf.MoveTowards(_open, held ? 1.0f : 0.0f, speed * dt);

            if (_open <= 0.0f)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }

            if (!gameObject.activeSelf) gameObject.SetActive(true);

            float eased = held ? EaseOutBack(_open) : EaseInQuad(_open);

            _group.alpha = Mathf.Clamp01(_open * 1.35f);
            _rt.anchoredPosition = new Vector2(0, RestY + (1.0f - eased) * -SlideDistance);

            for (int i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                if (card == null || card.Group == null) continue;

                float begin = i * Stagger;
                float local = Mathf.InverseLerp(begin, begin + (1.0f - Stagger * 2.0f), _open);
                float cardEase = held ? EaseOutBack(local) : local;

                card.Group.alpha = Mathf.Clamp01(local * 1.4f);
                card.Rt.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.0f, cardEase);
            }
        }

        /// <summary>
        /// Bind a kit and hold the tray fully open, for a capture.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE THIS PANEL IS OTHERWISE UNPHOTOGRAPHABLE, AND AN UNPHOTOGRAPHED
        /// PANEL IS AN UNJUDGED ONE. The tray is a HOLD: it is only ever on screen while a key is
        /// physically down, and `tools/shoot_player.ps1` records that synthesised keystrokes do
        /// not reach the game window from a background shell, only mouse clicks do. So the one
        /// surface in Hero Strike carrying every ability's full text had no path to a screenshot
        /// at all, in the editor or in a player.
        ///
        /// ⚠️ IT CHANGES NOTHING ABOUT THE HOLD. `Tick` still drives `_open` from the live key
        /// every frame, so anything that calls this and then keeps ticking goes straight back to
        /// closed. It is for a probe that renders one frame and exits, and nothing in the game
        /// calls it.
        /// </summary>
        public void OpenForCapture(HeroKit kit)
        {
            Bind(kit);

            _open = 1.0f;
            gameObject.SetActive(true);

            if (_group != null) _group.alpha = 1.0f;
            if (_rt != null) _rt.anchoredPosition = new Vector2(0, RestY);

            foreach (var card in _cards)
            {
                if (card?.Group == null) continue;

                card.Group.alpha = 1.0f;
                card.Rt.localScale = Vector3.one;
            }
        }

        private bool HoldPressed()
        {
            if (_hold == null)
            {
                var asset = Resources.Load<InputActionAsset>("TumbangPreso");
                if (asset == null) return false;

                Settings.Rebinding.Load(asset);
                var map = asset.FindActionMap("Player", false);
                _hold = map?.FindAction("AbilityInfo", false);
                _hold?.Enable();
            }

            return _hold != null && _hold.IsPressed();
        }

        private static float EaseOutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1.0f;
            float p = t - 1.0f;
            return 1.0f + c3 * p * p * p + c1 * p * p;
        }

        private static float EaseInQuad(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t;
        }

        // ------------------------------------------------------------------ helpers

        private static void Height(GameObject go, float height)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
        }

        private static Text Label(Transform parent, string name, int size, Color colour,
                                  TextAnchor align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var t = go.AddComponent<Text>();
            t.font = MenuKit.Font;
            t.fontSize = size;
            t.color = colour;
            t.alignment = align;
            t.alignByGeometry = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            // ⚠️⚠️ EVERY LABEL ON THIS TRAY GETS AN INK OUTLINE, AND IT IS THE ANSWER TO "BLURRY"
            // RATHER THAN TO "TOO SMALL". 🧑 2026-08-29, holding TAB: *"text here is genuinely
            // blurry too btw"*, *"the text when u click tab to see skills maybe add outline to
            // the gren shit idk"*. He is describing the ability NAME, which is drawn in that
            // hero's accent — Dante's is a mid green — directly onto `UiTheme.HeroPlateRaised`,
            // a mid-dark brown.
            //
            // ⚠️ IT IS A CONTRAST PROBLEM WEARING A SHARPNESS PROBLEM'S CLOTHES. The tray is
            // authored at 1060 units wide and the canvas scales it down to whatever the window
            // is, so a 20-unit glyph can land on eight or nine real pixels. At that size legacy
            // `Text` has almost no edge left, and a mid green on a mid brown has almost no
            // luminance step either, so the two failures compound into something that reads as
            // out of focus. Raising the point size alone cannot fix it, because the scale factor
            // moves with the window and the panel is already as tall as the band allows.
            //
            // ⚠️ SO THE FIX IS THE SAME ONE THE REST OF THE GAME ALREADY USES ON ITS ART: an ink
            // edge. `ToonSkin.Ink` is what every character and prop is outlined in, and this
            // borrows the same near-black so the UI and the world agree about what an outline is.
            // One pixel each way is enough to give a glyph a hard boundary at any scale, which is
            // exactly what was missing.
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(Visual.ToonSkin.Ink.r, Visual.ToonSkin.Ink.g,
                                            Visual.ToonSkin.Ink.b, 0.85f);
            outline.effectDistance = new Vector2(1.0f, -1.0f);
            outline.useGraphicAlpha = true;

            return t;
        }
    }
}
