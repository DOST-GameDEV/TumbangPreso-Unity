using System;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Sign in, or make an account. One screen, one job.
    ///
    /// ⚠️⚠️ THE LAYOUT IS THE RIOT CLIENT'S AND 🧑 HANDED IT OVER AS THE REFERENCE: *"look at
    /// their signup screens"*. A narrow form column down one side, the game's own art filling the
    /// rest, micro-labels above two fields, a small round primary, and tiny footer links. **The
    /// thing worth copying is not the colours, it is how little is on it.** Valorant's sign-in
    /// asks for two things and offers three links. The panel this replaces asked for six things
    /// and offered six buttons, and 🧑 counted them: *"theres liek 20 shits at once"*.
    ///
    /// ⚠️⚠️ AND IT IS A SEPARATE SCREEN, NOT A PANEL OVER THE MENU. 🧑: *"usually u dont open up
    /// login in the actual game screen yet"*. The old `AccountOverlay` opened a password field on
    /// top of the live street with the play buttons still visible underneath it, which is the
    /// arrangement no shipping game uses. Signing in is a mode: everything else goes away.
    ///
    /// ⚠️ NOTHING HERE EVER OPENS BY ITSELF. `PlayerAccount` signs in anonymously at boot and the
    /// player reaches the menu already playable, which is Phase 1's rule and the single most
    /// important thing about this flow: **never block a first-time player on a form.** This
    /// screen is only ever reached by pressing something.
    /// </summary>
    public sealed class SignInScreen : MonoBehaviour
    {
        /// <summary>
        /// The column's width in canvas UNITS, not as a fraction of the screen.
        ///
        /// ⚠️⚠️ IT WAS 38 PER CENT AND THAT IS WHY THE BOX LOOKED ENORMOUS AND THE ART LOOKED
        /// CHOPPED. 🧑, opening the build: *"the art is cut off... can u properly cut it at the
        /// appropriate amt and maybe make the box smaller"*. A fraction sizes the column against
        /// the WINDOW, and `AspectSafeCanvas` scales the canvas on the SHORT axis, so on the
        /// short wide window he actually plays in the canvas is about 2250 units across and 38
        /// per cent of it is **860 units of wood around a 420-unit form**. The form never got
        /// bigger; the empty wood either side of it did, and it took the art's space with it.
        ///
        /// ⚠️ 580 UNITS IS THE FORM PLUS ONE MARGIN EITHER SIDE (420 + 80 + 80), which is what
        /// the Riot reference's column actually is: a form-width column, not a percentage.
        ///
        /// ⚠️⚠️ AND A FIXED WIDTH CANNOT SWALLOW A NARROW SCREEN, WHICH IS THE ONE THING A
        /// FRACTION BUYS. `AspectSafeCanvas` uses `Expand`, so the scale is
        /// `min(w/1920, h/1080)` and the canvas is therefore **never narrower than 1920 units**
        /// at any resolution the game ships at. 580 of 1920 is 30 per cent in the worst case
        /// (4:3) and about 26 on his window. There is no shape where this column squeezes the
        /// form or eats the picture.
        /// </summary>
        private const float ColumnUnits = 560.0f;

        /// <summary>
        /// The card's height BEFORE it is measured, and it is only ever a starting value.
        ///
        /// ⚠️ `FitCardToContent` overwrites this at the end of `BuildColumn`. It is left here
        /// because the card is built and skinned before its content exists, and a `PaperSkin`
        /// baked against a zero-height rect pins the sprite to the 20-unit floor.
        /// </summary>
        private const float CardHeight = 900.0f;

        /// <summary>
        /// The air between the outermost thing on the card and the card's own edge.
        ///
        /// ⚠️ 56, WHICH IS THE FIELD PITCH LESS ONE GAP RATHER THAN A ROUND NUMBER. The column's
        /// tightest pitch is 64 units (caption to box) and its loosest is 120 (between blocks);
        /// a margin inside that range reads as part of the same rhythm, and one outside it reads
        /// as the card being a different object from the form on it.
        /// </summary>
        private const float CardMarginY = 56.0f;

        /// <summary>How far in from the left edge the card sits. ⚠️ Wide enough that the cast in
        /// the key art is never behind it at 16:9 and never off screen at 4:3, where
        /// `AspectSafeCanvas` gives the canvas about 1920 units against about 2250 on his
        /// window.</summary>
        private const float CardMargin = 96.0f;

        private Canvas _canvas;
        private GameObject _root;
        private InputField _username, _password;
        private Text _error, _primaryLabel;
        private Button _signInTab, _createTab;
        private Button _guest, _back;

        /// <summary>
        /// True while this screen is the first thing the game showed, rather than something the
        /// player pressed. See <see cref="OpenAtBoot"/>.
        /// </summary>
        private bool _atBoot;

        /// <summary>
        /// Every control in the column below the wordmark, so the welcome-back state can hide the
        /// whole form in one line.
        ///
        /// ⚠️⚠️ RECORDED BY INDEX AT BUILD TIME RATHER THAN AS A LIST OF NAMES, AND THAT IS THE
        /// SAME ARGUMENT `PlayerNameplate`'s header makes about chrome. A named list is a list
        /// somebody has to remember to extend, and the row added next year is the row that draws
        /// through the welcome message. Everything built after the logo is the form, by
        /// construction.
        /// </summary>
        private GameObject[] _formPieces = System.Array.Empty<GameObject>();

        private GameObject _welcome;
        private Text _welcomeName;
        private Text _welcomeHint;

        /// <summary>
        /// How long the welcome-back state holds before it lets itself out, in unscaled seconds.
        ///
        /// ⚠️⚠️ IT IS A BEAT, NOT A GATE. 🧑 2026-09-01 chose "every launch, auto-skip after a
        /// beat" from three options, over "only when not attached" and over a hard press on every
        /// launch. **A returning player must not have to press anything to reach their own game**,
        /// and a player who wants to switch accounts must not be carried past the screen by a
        /// timer, so any key or button press cancels the hold outright. `docs/TODO.md` § 114.5.
        /// </summary>
        public const float WelcomeHold = 1.2f;

        /// <summary>Unscaled time the welcome state lets go, or -1 when nothing is pending.</summary>
        private float _autoPassAt = -1.0f;

        /// <summary>Whether this screen is showing. Read by `PlayerHub.Update` so the two do not
        /// both answer one Escape.</summary>
        public bool IsOpen => _root != null && _root.activeSelf;

        /// <summary>
        /// Escape leaves this screen, unless it is the first thing the game showed.
        ///
        /// ⚠️⚠️ AT BOOT IT IS INERT ON PURPOSE AND THAT IS NOT AN OVERSIGHT. `OpenAtBoot` hides
        /// BACK for the same reason: there is nothing behind this screen, so a dismissal would
        /// drop the player onto a menu the game has not decided they may use yet, with the
        /// account question unanswered and no way back to it. **The escape from the boot screen
        /// is CONTINUE AS GUEST**, which is one press and is the whole reason the gate is
        /// acceptable at all (`docs/TODO.md` § 97).
        ///
        /// ⚠️ OPENED BY A PRESS, IT BEHAVES LIKE EVERY OTHER SCREEN, because a player who
        /// pressed something to get here has somewhere to go back to.
        /// </summary>
        private void Update()
        {
            if (!IsOpen) return;

            // ⚠️⚠️ THE HOLD IS ON UNSCALED TIME AND CANCELS ON ANY PRESS. `Time.time` would stop
            // with the clock, and this screen is the one place in the game where the clock's
            // state is decided by whatever the last scene did. `Input.anyKeyDown` covers the
            // mouse as well as the keyboard, which is the point: a player reaching for CONTINUE
            // has already said they are here, and being dropped through the screen mid-reach is
            // exactly the thing that makes a boot feel out of control.
            if (_autoPassAt > 0.0f)
            {
                if (Input.anyKeyDown)
                {
                    _autoPassAt = -1.0f;
                    if (_welcomeHint != null) _welcomeHint.text = "";
                }
                else if (Time.unscaledTime >= _autoPassAt)
                {
                    _autoPassAt = -1.0f;
                    BootGuest();
                    return;
                }
            }

            if (_atBoot) return;
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            // ⚠️ SPENT, so the screen underneath does not back out on the same press. See
            // `ScreenTakeover.ConsumeEscape`.
            ScreenTakeover.ConsumeEscape();
            Close();
            MenuSfx.Back();
        }
        private bool _creating;

        /// <summary>CONTINUE WITH GOOGLE, or null in a build with no client id. See `BuildForm`.</summary>
        private Button _googleButton;

        /// <summary>The keys this form takes, written down. See `BuildForm`.</summary>
        private Text _keyHint;

        /// <summary>The chalk bar under whichever tab is live. See <see cref="BuildTabs"/>.</summary>

        /// <summary>Raised when the player leaves, whether they signed in or not, so the hub can
        /// come back up where it was.</summary>
        public event Action Closed;

        /// <summary>
        /// Raised with true on open and false on close, so the hub can get out of the way.
        ///
        /// ⚠️⚠️ THE ART SIDE HAS TO BE ART, AND THE FIRST RENDER HAD THE HUB IN IT. The
        /// reference is a form column beside a picture; leaving the four-tab panel lit under the
        /// 72 per cent scrim put a half-covered ACCOUNT tab there instead, with its rows sliced
        /// down the middle by the column edge. **Two screens on screen at once is the thing this
        /// whole rebuild is against**, and it is exactly what the old panel did over the menu.
        /// </summary>
        public event Action<bool> Opened;

        private void OnDestroy() => ScreenTakeover.Unregister(this);

        public void Install()
        {
            if (_canvas != null) return;

            // ⚠️ REGISTERED AS A TAKEOVER. It covers everything, including the hub, and the
            // register is how a screen underneath finds out. See `ScreenTakeover.EscapeIsSpoken`.
            ScreenTakeover.Register(this, () => IsOpen);

            _canvas = MenuKit.BuildCanvas(transform, "SignInCanvas");

            // ⚠️ ABOVE THE HUB'S 500. Signing in is reached FROM the hub and has to cover it; a
            // password field with a stats table showing through it is the thing this replaces.
            // See `PlayerHub.Install` for why both numbers are far above the converted screens.
            _canvas.sortingOrder = 510;

            _root = new GameObject("SignInRoot", typeof(RectTransform));
            _root.transform.SetParent(_canvas.transform, false);
            MenuKit.Stretch((RectTransform)_root.transform);

            BuildScrim();
            BuildColumn();

            _root.SetActive(false);
        }

        /// <summary>
        /// What sits behind the form, which is the key art when there is one and a heavy black
        /// backdrop over the live scene when there is not.
        ///
        /// ⚠️⚠️ THE SCRIM IS GONE WHEN THE ART IS THERE, AND 🧑 ASKED FOR THAT IN ONE LINE:
        /// *"also nno nneed to darkenn it"*. It was 72 per cent over the live street, then 55 per
        /// cent over the art. **Both numbers were paying for legibility this screen does not
        /// need**: every word on it sits on an opaque wood column, and the art side carries no
        /// text at all, so the scrim was knocking back the one thing the player is meant to look
        /// at in order to protect text that is not on it. `UiRows.Band`'s note is the same rule
        /// read the other way: a number tuned against one background is not a number, and this
        /// one had already been retuned once without asking what it was still for.
        ///
        /// ⚠️ THE FALLBACK KEEPS ITS 72 PER CENT, AND IT IS NOT THE SAME SITUATION. With no key
        /// art the art side IS the live, moving, fully lit street with the menu's own buttons on
        /// it, and that has to be knocked back or this stops being a separate screen and goes
        /// back to being a password field over the menu, which is what the whole rebuild removed.
        /// </summary>
        private void BuildScrim()
        {
            BuildKeyArt();

            if (HasKeyArt) return;

            var scrim = MenuKit.Backdrop(_root.transform, new Color(0.0f, 0.0f, 0.0f, 0.72f));
            scrim.gameObject.name = "Scrim";
        }

        private bool HasKeyArt;

        /// <summary>
        /// The cast, behind the form, which is what the reference actually does.
        ///
        /// ⚠️⚠️ 🧑: *"also can u put the frigging splash art there now ? i already gave u it
        /// right"*. `docs/TODO.md` § 97 listed this as the one thing left out of the boot screen
        /// and said why it was left: it is a look change and that entry was a flow change. The
        /// art is already in `Resources` for the loading screen (§ 95c), so this costs one load.
        ///
        /// ⚠️⚠️ AND IT REPLACES A CLAIM THIS FILE MADE THAT IS NO LONGER TRUE. `BuildScrim`'s old
        /// note argued the art side should be the LIVE SCENE rather than a texture, *"without
        /// shipping a 4 MB PNG that goes stale the first time the art changes"*. That was a good
        /// argument when there was no key art; there is one now, it ships anyway for the loading
        /// screen, and **the live scene is not visible here at all when this screen opens at
        /// boot** because the menu behind it is a different canvas. The old note is corrected
        /// rather than deleted, per `CLAUDE.md` § 3.
        ///
        /// ⚠️ IT ENVELOPES, like the loading screen, because it is a background and cropping it
        /// is correct. The logo above the form uses `FitInParent` for the opposite reason.
        ///
        /// ⚠️⚠️ AND IT ENVELOPES THE ART SIDE, NOT THE SCREEN, WHICH IS THE WHOLE OF THE BUG 🧑
        /// OPENED THE BUILD AND FOUND: *"This shhit is horrible bro the art is cut off"*. The
        /// picture was fitted to the FULL canvas and then a third of it was covered by the
        /// column, so the crop was computed for a frame nobody could see and what was left was
        /// an off-centre window into it. Two things went wrong at once and they compounded:
        ///
        /// - **Horizontally**, the cast is composed around the middle of the frame, and the
        ///   column sat on the left third of that frame. The character on the far left was
        ///   behind the wood and the whole group was pushed off-centre in what remained.
        /// - **Vertically**, his window is wider than 16:9 and the art is 16:9, so enveloping it
        ///   to the full canvas matched WIDTH and cut the top and bottom off. That is where the
        ///   chopped heads came from.
        ///
        /// **Enveloping the region the art is actually seen in fixes both at once, and it is
        /// arithmetic rather than taste.** The art side is `canvasWidth - 580` by
        /// `canvasHeight`; on his short wide window that is about 1670x1080, which is 1.55, and
        /// the picture is 1.78. A region NARROWER than the picture matches HEIGHT, so the full
        /// height of the art is on screen, nothing is cut off the top or the bottom, and the
        /// only crop is a symmetric slice off each end. At 1920x1080 that slice is the widest it
        /// gets and still leaves the middle 70 per cent, which holds every character: the cast
        /// spans 13 to 87 per cent of the frame.
        ///
        /// ⚠️ THE CROP IS CENTRED AND IS NOT BIASED UPWARD, WHICH WAS TRIED ON PAPER FIRST. A
        /// pivot above centre would keep the sky and lose the road, but the lata and the tsinelas
        /// ARE the bottom of this picture and they are the game's two objects. There is no crop
        /// worth taking that cuts them.
        ///
        /// ⚠️⚠️ AND THE OVERFLOW IS MASKED RATHER THAN COVERED. Enveloping means the picture is
        /// bigger than its region by construction, and the old arrangement relied on the column
        /// being opaque and later in the hierarchy to hide the part that spilled under it. That
        /// is the same class of assumption as § 99: it is true until somebody reorders two lines
        /// or gives the column a translucent skin. A `RectMask2D` makes the region's edge the
        /// picture's edge whatever is drawn beside it.
        /// </summary>
        private void BuildKeyArt()
        {
            var art = Resources.Load<Texture2D>("UI/splash_art");
            HasKeyArt = art != null;
            if (art == null) return;

            // ⚠️⚠️ THE ART IS FULL BLEED NOW AND THE FORM FLOATS ON TOP OF IT. It used to be
            // masked to the strip right of a 580-unit wooden column, which is what `docs/TODO.md`
            // § 100 fixed and is still a compromise: the cast was cropped by a rectangle whose
            // only reason to exist was that a panel was standing next to it. 🧑 2026-09-01, on
            // this screen: *"u can overhaul the wole lobby and login bcz its ugly as fuck"*.
            //
            // ⚠️ § 6.2c QUESTION 2 STILL HOLDS AND IS THE REASON THIS IS SAFE: the image is now
            // fitted to the region it is SEEN in, because that region is the whole canvas. The
            // card is a floating object over it rather than a wall beside it, which is the
            // construction the game's own sticker logo is drawn in.
            var side = new GameObject("ArtSide", typeof(RectTransform));
            side.transform.SetParent(_root.transform, false);

            var sideRt = (RectTransform)side.transform;
            sideRt.anchorMin = Vector2.zero;
            sideRt.anchorMax = Vector2.one;
            sideRt.offsetMin = Vector2.zero;
            sideRt.offsetMax = Vector2.zero;
            side.AddComponent<RectMask2D>();

            var go = new GameObject("KeyArt", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(side.transform, false);

            // ⚠️ CENTRED ANCHORS, NOT STRETCHED, BECAUSE THE FITTER IS WHAT SIZES THIS RECT.
            // `AspectRatioFitter` drives `sizeDelta`, and on a stretched rect `sizeDelta` is an
            // inset from the parent's edges rather than a size, so the two disagree about what
            // the number means and the result depends on which ran last.
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Centre;
            rt.anchorMax = Centre;
            rt.pivot = Centre;
            rt.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<RawImage>();
            image.texture = art;

            // ⚠️⚠️ THE ART IS THE BLOCKER NOW, AND WITHOUT THIS THE MENU UNDERNEATH IS STILL
            // CLICKABLE THROUGH IT. The scrim used to be what stopped a press on the art side
            // reaching the title screen behind, and deleting it takes that with it: at boot the
            // player would have been able to press PLAY through the picture, on a screen that
            // exists to ask a question first. `RectMask2D` is an `ICanvasRaycastFilter`, so the
            // block stops exactly where the picture does, and the opaque column takes the rest.
            image.raycastTarget = true;

            var fitter = go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = art.width / (float)art.height;
        }

        private void BuildColumn()
        {
            var columnGo = new GameObject("Column", typeof(RectTransform), typeof(Image));
            columnGo.transform.SetParent(_root.transform, false);

            // ⚠️ ANCHORED TO THE LEFT EDGE AND SIZED IN UNITS, NOT TO A FRACTION OF THE WIDTH.
            // See `ColumnUnits`: the fraction is what made the box enormous on his window and
            // took the picture's space with it. Full height either way, which is the reference.
            // ⚠️⚠️ THE COLUMN BLEEDS OFF THREE EDGES OF THE SCREEN, AND THAT IS WHAT KILLS THE
            // DARK NOTCH IN THE CORNER. 🧑 2026-09-01, with a crop of the top-left corner:
            // *"whats that empty space bcz of roudned age"*. A `WoodCraft` panel is rounded on
            // all four corners, which is correct for a card floating in the middle of a screen
            // and wrong for a board that runs off the edge of one: at the screen's own corner you
            // saw the background through the radius.
            //
            // ⚠️ IT BLEEDS RATHER THAN SQUARING ITS CORNERS, so there is still exactly one panel
            // shape in the front end. `Bleed` is one unit more than the largest radius the slab
            // can draw (`RoundFraction` 0.09 of a 96-unit tall surface, plus its keyline and rim),
            // so the curve is always outside the visible frame. **The RIGHT edge is not bled**,
            // because that edge is the one the player actually sees and `ColumnEdge` draws the lit
            // line down it.
            // ⚠️⚠️ ONE CARD, CENTRED IN THE LEFT THIRD, INSTEAD OF A WALL DOWN THE WHOLE EDGE.
            // The old column ran the full height of the screen with 28 units of bleed off three
            // sides, so the screen was half wood and half picture with a hard seam between them.
            // A card is `Art/ui/TUMP.png`'s own construction (a cut-out lying on a surface) and it
            // is what makes the key art read as ONE image with something on it rather than as two
            // panels sharing a window.
            //
            // ⚠️ THE HEIGHT IS THE FORM ADDED UP, NOT A FRACTION OF THE SCREEN. The wordmark sits
            // at +330 with a 104-unit box and the key hint at about -480, which is 862 of content;
            // `CardHeight` is that plus one margin either side. `CLAUDE.md` § 6.2c question 1: a
            // percentage of the window is not a size, and `AspectSafeCanvas` scales on the SHORT
            // axis, so one fraction is two different widths at two aspect ratios.
            var rt = (RectTransform)columnGo.transform;
            rt.anchorMin = new Vector2(0.0f, 0.5f);
            rt.anchorMax = new Vector2(0.0f, 0.5f);
            rt.pivot = new Vector2(0.0f, 0.5f);
            rt.anchoredPosition = new Vector2(CardMargin, 0.0f);
            rt.sizeDelta = new Vector2(ColumnUnits, CardHeight);

            // ⚠⚠ THE COLUMN IS A PLANK NOW AND IT WAS A FLAT FILL WITH A NINE-PATCH ON IT.
            // 🧑 2026-09-01: *"our UI is ugly and repetitive and unimaginative"*. `UiMaterials`
            // carries the whole argument; the short version is that the light in this front end
            // comes from above now, so a raised surface is bright along its top edge and a
            // recessed one is dark along it, and the player reads which is which without being
            // told. A flat `WoodDeep` rectangle with the same bevel on all four sides said
            // nothing about anything.
            //
            // ⚠️ HIS AUTHORED NINE-PATCH STAYS ON THE BUTTONS AND LEAVES THE BACKGROUND.
            // `docs/VISION.md` § 6: his UI art IS the design system, and every CONTROL on this
            // screen is still drawn with it. This is the surface behind them, which was never
            // authored art: it was `Image.color = WoodDeep`.
            // ⚠⚠ THE COLUMN IS DRAWN IN HIS OWN LANGUAGE NOW. `WoodCraft`'s header carries the
            // sampling; the short version is that a `UiMaterials` plank is a rounded rect with a
            // dark outline over a flat face, and every board 🧑 authored has a BRIGHT keyline
            // outside a dark rim over a full-height ramp with a varnish band near the top. This
            // screen puts the two next to each other on the same pixels, because the primary
            // button below is drawn from `GodotTheme` and the column behind it was not.
            var face = columnGo.GetComponent<Image>();
            PaperSkin.Apply(columnGo, PaperCraft.Surface.Sheet);
            face.raycastTarget = true;

            var col = columnGo.transform;

            // ⚠⚠ THE COLUMN'S RIGHT EDGE IS A LIT LINE, AND IT IS THE ONE THING THAT MAKES THIS
            // READ AS A PHYSICAL BOARD IN FRONT OF THE ART RATHER THAN AS A CROP OF IT. Two units
            // of `WoodEdge` down the full height, on the side the light is not coming from.
// ⚠️ THE LIT WOODEN EDGE STRIP IS GONE WITH THE COLUMN. It existed to stop a
            // full-height brown wall dissolving into a brown photograph; a cream card carries its
            // own die-cut halo on all four sides and needs no seam.

            // ⚠️⚠️ EVERY ROW IS PLACED FROM THE COLUMN'S CENTRE, NOT FROM ITS TOP AND BOTTOM,
            // AND THE OLD ARRANGEMENT IS WHAT 🧑 CALLED *"ugly big ass space i hate this ui its
            // so ugly"*. The form was pinned to the top of a full-height column and the two
            // footer buttons to the bottom of it, so the taller the window the bigger the void
            // between them: at 1080 it is most of the screen of nothing.
            //
            // ⚠️ THE COLUMN STAYS FULL HEIGHT, WHICH IS THE REFERENCE, AND ONLY THE CONTENT
            // MOVES. `FUTURE.md` PHASE 1 and § 92.2: *"a sign-in is a narrow column beside art"*.
            // Shrinking the column to fit the form would make it a floating card, which is a
            // different screen and not the one that was asked for.
            //
            // ⚠️ ONE ANCHOR MEANS THE BLOCK CANNOT SPLIT. Two anchors is a layout that is correct
            // at one window height, which is fault 3 of § 92.1 and the reason `UiRows` exists.
            // The block is about 780 units tall and the canvas matches HEIGHT at a 1080
            // reference, so it fits every shape the game ships at by construction.
            // ⚠⚠ THREE BLOCKS, NOT NINE ROWS, AND THE GAPS BETWEEN THEM ARE WHAT SAY SO.
            // `game-ui-design`'s ordering tools are position, size, weight and colour in that
            // order, and this column was using only the last two. The blocks are IDENTITY (the
            // wordmark and the heading), FORM (which mode, and the two fields) and ACTIONS. Inside
            // a block the pitch is 80 units; between blocks it is 120. Nothing here is a new
            // number for its own sake: the pitch is the field height plus its caption.
            //
            // ⚠⚠⚠ THE HEADING IS GONE AND IT WAS SAYING THE SAME WORD AS THE TAB ABOVE IT AND
            // THE BUTTON BELOW IT. 🧑 2026-09-01: *"I donnt want redundannt UI"*. On the CREATE
            // tab this column read **CREATE / CREATE ACCOUNT / CREATE ACCOUNT** inside 340 units
            // of screen: the live tab, a 40-unit heading and the primary button, three statements
            // of one fact. `SetMode` was writing the same string into two labels on purpose.
            //
            // **The tab strip IS the heading.** It states which of the two things you are doing,
            // it is the control that changes it, and it carries an amber chalk bar under the live
            // half; a separate line restating it is a heading that cannot be acted on. The
            // wordmark above says which game this is, the strip says which door, the button says
            // what happens. Three statements, three different facts.
            //
            // ⚠️ AND THE 100 UNITS IT FREED WENT INTO THE GAPS RATHER THAN INTO THE COLUMN'S
            // LENGTH. The block pitch is what `game-ui-design` calls the last ordering tool and
            // 🧑 has complained about this column's spacing twice (*"ugly big ass space i hate
            // this ui its so ugly"*): the three blocks are IDENTITY, FORM and ACTIONS, 80 units
            // inside a block and 120 between them, and the whole column is 90 units shorter than
            // it was.
            //
            // ⚠️⚠️ THE FORM DROPPED 30 UNITS BECAUSE THE TAB ROW HAD NOWHERE TO BREATHE.
            // 🧑 2026-09-01, with a crop of the tabs and the two fields: *"everything below this
            // looks good"*, *"this part looks too tight"*. Measured off `Logs/ui/07-signin.png`:
            // the live tab's bottom edge sat at 124 and the USERNAME caption's box topped out at
            // 119, **five units of air between a control and the label of the next one**, while
            // the pitch between the two field blocks below it is 24 and the pitch between blocks
            // is 120. One gap in the column was an order of magnitude tighter than its neighbours,
            // which is what "too tight" always means.
            //
            // ⚠️⚠️ AND THEN THE TOP BLOCK WENT UP AS WELL, ON REQUEST: 🧑, looking at the
            // result, *"raise tump and sign in and create to create space for username and
            // password"*. Dropping the form alone had already opened the gap, and he wanted the
            // air on the other side of it too. `Logo` 290 to 330 and `Tabs` 158 to 200.
            //
            // The arithmetic, so the next person does not have to re-derive it: the wordmark's
            // box is 104 tall and centred on `Logo`, so its underside is at 278. A tab hangs from
            // `Tabs - IdleTabHeight / 2` = 174 and the live one stands `LiveTabHeight` 60 above
            // that, so its top is 234: **44 units under the wordmark.** The USERNAME caption's box
            // tops out at 89, which is **85 units under the tab row**, against the 5 it had when
            // he called it *"too tight"*.
            //
            // ⚠️ AND NOTHING BELOW `Primary` MOVED BY MORE THAN THE SAME 30, because he said
            // everything below the tabs already looks good and the actions block is measured
            // against itself.
            const float Logo = 330.0f;
            const float Tabs = 200.0f;
            const float UserField = 30.0f;
            const float PassField = -82.0f;
            const float Hint = -152.0f;
            const float Primary = -226.0f;

            BuildLogo(col, Logo);

            // ⚠️ EVERYTHING FROM HERE DOWN IS THE FORM. See `_formPieces`: the welcome-back state
            // hides this whole range and leaves the wordmark, so the two states are the same
            // column with the same logo in the same place rather than two screens.
            int formStart = col.childCount;



            BuildTabs(col, Tabs);

            _username = Field(col, "USERNAME", UserField, "your username", 64, false);
            _password = Field(col, "PASSWORD", PassField, "your password", 128, true);

            // ⚠️ THE ERROR SITS UNDER THE FIELDS RATHER THAN IN A SHARED STATUS LINE AT THE TOP.
            // The old panel had one `_status` label that reported saving a profile, linking a
            // username, signing in and arming a delete, so the sentence on screen was whichever
            // of six unrelated actions ran last.
            _error = MenuKit.Label(col, "", PaperKit.Caption, UiTheme.MenuRed,
                Centre, new Vector2(0.0f, Hint), new Vector2(420.0f, 56.0f));
            _error.horizontalOverflow = HorizontalWrapMode.Wrap;

            BuildPrimary(col, Primary);

            // ⚠️⚠️ THE GUEST BUTTON DOES TWO DIFFERENT THINGS AND THE LABEL SAYS WHICH.
            // Reached from the ACCOUNT tab it is the TOURNAMENT guest, which parks the owner's
            // profile and hands the machine to somebody else for one session. Reached at BOOT it
            // means "keep the anonymous account this machine already has and let me play", which
            // is the opposite: nothing is parked and nothing is temporary. **Two behaviours
            // behind one word is exactly the confusion this file was rebuilt to remove**, so the
            // caption changes with the mode and `BootGuest` and `Guest` are separate methods.
            // ⚠️ THE ESCAPE SITS DIRECTLY UNDER THE PRIMARY, NOT AT THE BOTTOM OF THE COLUMN.
            // It is the second thing a player might do here, so it belongs beside the first;
            // parked at the bottom it was separated from the form by a screen of nothing and
            // read as unrelated chrome. **A choice and its alternative are one group.**
            // ⚠⚠ THE GOOGLE BUTTON IS BUILT ONLY IF THIS BUILD HAS A CLIENT ID, AND THE TWO
            // BUTTONS UNDER IT MOVE UP WHEN IT IS NOT. 🧑 2026-09-01: *"can we add some sort of
            // authentication too? like an option to sign inn with google acct or connect google
            // acct"*. `GoogleSignIn.IsAvailable` is false until somebody puts a client id in
            // `Resources/google_oauth.txt`, and the two ways of handling that are both wrong:
            // a visible button that explains why it cannot work is `docs/TODO.md` § 108's dead
            // EQUIP button with an apology on it, and a HIDDEN button that leaves its 62 units of
            // hole behind is the *"ugly big ass space"* this column was rebuilt to remove. So the
            // row does not exist and the column closes up.
            //
            // ⚠️ IT IS PLAIN WOOD, NOT GREEN. `GodotTheme`'s rule is that green means ACT and
            // there is one action per screen; the primary button IS the action here and a second
            // green control beside it would make the player choose between two equals. This is the
            // other way to do the same thing, so it reads as the alternative it is.
            //
            // ⚠️ AND THE VERB FOLLOWS THE TAB, exactly like the primary. On CREATE it CONNECTS
            // Google to the account this machine already has, keeping every match played on it; on
            // SIGN IN it moves to whichever account owns that Google identity. `SetMode`'s note is
            // the long version and `PlayerAccount.LinkGoogleAsync` is the other half.
            bool google = GoogleSignIn.IsAvailable;
            float guestY = google ? -364.0f : -298.0f;
            float backY = google ? -428.0f : -362.0f;

            if (google)
            {
                _googleButton = PaperKit.Chip(col, "GoogleButton", "CONTINUE WITH GOOGLE");
                _googleButton.onClick.AddListener(GooglePressed);
                MenuKit.Place((RectTransform)_googleButton.transform, Centre,
                              new Vector2(0.0f, -298.0f), new Vector2(420.0f, 54.0f));
            }

            _guest = PaperKit.Chip(col, "GuestButton", "PLAY AS GUEST");
            _guest.onClick.AddListener(GuestPressed);
            MenuKit.Place((RectTransform)_guest.transform, Centre,
                          new Vector2(0.0f, guestY), new Vector2(300.0f, 48.0f));

            // ⚠⚠ BACK IS THE THIRD THING ON THIS SCREEN AND IT WAS DRAWN AS THE SECOND. It was
            // the same 300x48 wood plate as PLAY AS GUEST, so the column ended with two identical
            // buttons and the player had to READ them to tell an escape hatch from a way to play.
            // `game-ui-design`'s ordering is position, size, weight, colour: this is one step down
            // in size and one in weight, and it is still a 44-unit target.
            // ⚠️⚠️ BACK IS THE ONLY CONTROL ON THIS SCREEN WITH NO SURFACE AT ALL, WHICH IS
            // THE HIERARCHY STATED IN SHAPE. Below the green primary there are three ways to leave
            // without an account, and drawing all three as identical slabs is what made the old
            // column read as a list of equals. Google and GUEST are paper chips; BACK is a word.
            _back = PaperKit.Chip(col, "BackButton", "BACK");
            _back.onClick.AddListener(Close);
            MenuKit.Place((RectTransform)_back.transform, Centre,
                          new Vector2(0.0f, backY), new Vector2(220.0f, 44.0f));

            var backSkin = _back.GetComponent<PaperSkin>();
            if (backSkin != null) backSkin.enabled = false;

            var backPlate = _back.GetComponent<Image>();
            if (backPlate != null) backPlate.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);

            var backLabel = _back.transform.Find("Label")?.GetComponent<Text>();
            if (backLabel != null) backLabel.color = UiTheme.PaperInkSoft;

            // ⚠⚠ THE KEYS ARE ON THE SCREEN, AND `game-ui-design` LISTS THEIR ABSENCE AS A SHARP
            // EDGE BY NAME (`No Keyboard Shortcut Display`). This form has always taken TAB and
            // ENTER and has never said so, so every player has moused between two fields and
            // hunted for a button. ⚠️ It names the keys rather than drawing glyphs, because this
            // build has **zero gamepad bindings** (`FUTURE.md` § 0.6 checked it) and a controller
            // glyph on a keyboard-only build is the `Input Prompt Mismatch` edge one page over.
            //
            // ⚠️ AND IT IS THE LAST THING IN THE COLUMN, under everything it describes, at the
            // muted weight. A hint that competes with the action it explains is a second heading.
            _keyHint = MenuKit.Label(col, "TAB to move  ·  ENTER to sign in  ·  ESC to go back",
                PaperKit.Caption, UiTheme.PaperInkSoft, Centre,
                new Vector2(0.0f, backY - 52.0f), new Vector2(460.0f, 26.0f));
            _keyHint.raycastTarget = false;

            // ⚠️ THE CHAIN IS BUILT AFTER EVERY CONTROL EXISTS, in the order a person reads them.
            // See `Chain`: the tabs are the first stop because the first question this screen
            // asks is which of the two things you are doing.
            Chain(_signInTab, _createTab, _username, _password,
                  _primaryLabel != null ? _primaryLabel.GetComponentInParent<Button>() : null,
                  _googleButton, _guest, _back);

            FocusRing.Attach(_guest.gameObject, 4.0f);
            FocusRing.Attach(_back.gameObject, 4.0f);
            if (_googleButton != null) FocusRing.Attach(_googleButton.gameObject, 4.0f);
            if (_primaryLabel != null)
            {
                var primary = _primaryLabel.GetComponentInParent<Button>();
                if (primary != null) FocusRing.Attach(primary.gameObject, 4.0f);
            }

            _formPieces = new GameObject[col.childCount - formStart];
            for (int i = formStart; i < col.childCount; i++)
                _formPieces[i - formStart] = col.GetChild(i).gameObject;

            BuildWelcome(col);

            FitCardToContent(rt, col);
        }

        /// <summary>
        /// Sets the card's height from what is actually on it.
        ///
        /// ⚠️⚠️ IT WAS A CONSTANT AND THE CONSTANT WAS WRONG IN BOTH DIRECTIONS AT ONCE, WHICH IS
        /// WHY THIS IS ARITHMETIC AND NOT A BETTER NUMBER. `docs/TODO.md` § 119.11: *"the login
        /// card is 900 units tall around about 700 of content"*, and 🧑, of the screen:
        /// *"LOGIN can be improved"*.
        ///
        /// **Measured off the offsets this file actually uses.** Without a Google button the
        /// content runs from the wordmark's box top at +382 down to the key hint's bottom at
        /// -427: 809 units in a 900-unit card, which is 68 units of margin above and **23 below**.
        /// A card whose top margin is three times its bottom one does not read as badly spaced, it
        /// reads as slipping off its own frame.
        ///
        /// ⚠️⚠️ AND WITH `GoogleSignIn.IsAvailable` TRUE IT OVERFLOWED. That branch pushes GUEST
        /// to -364, BACK to -428 and the hint to -480, so the last line of the column sits **43
        /// units below the card's own bottom edge** and draws on the key art. Nobody has seen it,
        /// because no build in this repository has a client id in it yet (§ 115.8 is the
        /// credential): the layout is correct today and breaks on the day somebody pastes a
        /// string into a text file. **A card sized against its content cannot have that bug.**
        ///
        /// ⚠️ THE MARGIN IS ONE NUMBER AND IT IS `CardMarginY`, applied symmetrically, so the
        /// arithmetic above cannot come back however the column is rearranged. `CLAUDE.md` § 6.2c
        /// question 1: size a panel against its CONTENT and state the arithmetic.
        ///
        /// ⚠️ THE WELCOME STATE IS INCLUDED IN THE UNION AND IS SMALLER THAN THE FORM, so the card
        /// is the form's size in both states. A card that resized when the state changed would be
        /// a second layout nobody has photographed, which is § 6.2b's first row.
        /// </summary>
        private static void FitCardToContent(RectTransform card, Transform col)
        {
            float top = 0.0f;
            float bottom = 0.0f;
            bool any = false;

            for (int i = 0; i < col.childCount; i++)
            {
                var child = col.GetChild(i) as RectTransform;
                if (child == null) continue;

                // ⚠️ THE PIVOT IS READ RATHER THAN ASSUMED, because the tab pair hangs from a
                // bottom pivot (see `Hang`) and everything else is centred. Getting this wrong
                // would size the card against a rect nothing occupies.
                float height = child.sizeDelta.y;
                float centre = child.anchoredPosition.y + ((0.5f - child.pivot.y) * height);

                float childTop = centre + (height * 0.5f);
                float childBottom = centre - (height * 0.5f);

                if (!any)
                {
                    top = childTop;
                    bottom = childBottom;
                    any = true;
                    continue;
                }

                if (childTop > top) top = childTop;
                if (childBottom < bottom) bottom = childBottom;
            }

            if (!any) return;

            // ⚠️ THE CARD IS CENTRED ON THE SCREEN AND ITS CONTENT IS NOT CENTRED ON THE CARD, so
            // the height has to cover the FURTHER of the two extents in both directions rather
            // than their span. Sizing to `top - bottom` and leaving the content where it is would
            // put the same overflow back on whichever side reaches further.
            float reach = Mathf.Max(Mathf.Abs(top), Mathf.Abs(bottom));
            card.sizeDelta = new Vector2(card.sizeDelta.x, (reach + CardMarginY) * 2.0f);
        }

        /// <summary>
        /// The state a returning player meets: their own handle, and the screen letting itself
        /// out.
        ///
        /// ⚠️⚠️ IT IS A STATE OF THIS SCREEN RATHER THAN A SCREEN OF ITS OWN, WHICH IS
        /// `CLAUDE.md` § 6.2b's FIRST ROW APPLIED BEFORE THE FAULT RATHER THAN AFTER IT. That row
        /// is the receipt for shipping the sign-in screen photographed only as `Open()` while
        /// players met `OpenAtBoot()`. A third screen would be a third layout nobody had looked
        /// at; the same column with the form swapped for two lines is one screen with two states,
        /// and `PlayerHubLayoutProbe` photographs both.
        ///
        /// ⚠️ THE TWO BUTTONS ARE THE TWO ANSWERS AND BOTH ARE VISIBLE. § 6.3: a door is a thing
        /// that looks pressable, and a screen that only leaves on a timer is a screen with no
        /// door at all. CONTINUE is the one the timer would have taken; SIGN IN AS SOMEBODY ELSE
        /// puts the form back and cancels the hold, which is the whole reason the hold cancels on
        /// any press.
        /// </summary>
        private void BuildWelcome(Transform col)
        {
            _welcome = new GameObject("Welcome", typeof(RectTransform));
            _welcome.transform.SetParent(col, false);
            MenuKit.Stretch((RectTransform)_welcome.transform);

            var root = _welcome.transform;

            MenuKit.Label(root, "WELCOME BACK", 34, UiTheme.PaperInkSoft, Centre,
                new Vector2(0.0f, 120.0f), new Vector2(460.0f, 54.0f));

            // ⚠️ THE HANDLE IS THE ONE THING ON THIS STATE, so it is the biggest thing on it and
            // the only amber one. § 6.2 question 1: everything else is sized against it.
            _welcomeName = MenuKit.Label(root, "", PaperKit.Display, UiTheme.PaperInk, Centre,
                new Vector2(0.0f, 46.0f), new Vector2(460.0f, 64.0f));

            _welcomeHint = MenuKit.Label(root, "press anything to stay here",
                PaperKit.Caption, UiTheme.PaperInkSoft, Centre,
                new Vector2(0.0f, -6.0f), new Vector2(460.0f, 30.0f));

            MenuKit.WoodButton(root, "CONTINUE", Centre,
                new Vector2(0.0f, -80.0f), new Vector2(420.0f, 62.0f), BootGuest,
                "WoodPrimaryButton");

            var other = PaperKit.Chip(root, "OtherAccountButton", "SIGN IN AS SOMEBODY ELSE");
            other.onClick.AddListener(LeaveWelcomeForTheForm);
            MenuKit.Place((RectTransform)other.transform, Centre,
                          new Vector2(0.0f, -156.0f), new Vector2(420.0f, 48.0f));

            _welcome.SetActive(false);
        }

        /// <summary>Swaps the two states. ⚠️ The wordmark belongs to neither and never moves.</summary>
        private void ShowWelcome(bool welcome)
        {
            foreach (var piece in _formPieces)
                if (piece != null) piece.SetActive(!welcome);

            if (_welcome != null) _welcome.SetActive(welcome);
        }

        /// <summary>
        /// ⚠️ CANCELS THE HOLD AS WELL AS SWITCHING, because a player who pressed this and was
        /// then dropped into the menu a fraction of a second later would have pressed a button
        /// that did the opposite of what it says.
        /// </summary>
        private void LeaveWelcomeForTheForm()
        {
            _autoPassAt = -1.0f;
            ShowWelcome(false);
            SetMode(false);
        }

        /// <summary>
        /// ⚠️ A SEGMENTED PAIR, NOT TWO BUTTONS. Sign in and create account are the same two
        /// fields and the same submit; making them two separate wood buttons, as the old panel
        /// did with SIGN IN and LINK USERNAME sitting beside SAVE PROFILE, asks the player to
        /// know the difference before they have typed anything. A segment says "one of these two
        /// modes is on" and the primary underneath does whichever it is.
        /// </summary>
        /// <summary>Everything in the column is placed from its centre. See `BuildColumn`.</summary>
        private static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);

        /// <summary>
        /// The real wordmark from the title screen, not the word typed out.
        ///
        /// ⚠️⚠️ 🧑 ASKED FOR IT BY NAME: *"can u use the real TUMP text logo taht we have on
        /// title screen too"*. It was a 30-unit amber `Label` reading "TUMP", which is the game's
        /// name in the menu font rather than the game's LOGO, and this is the first screen a
        /// player ever sees. `Resources/UI/main-menu/TUMP.png` is the same asset the title screen
        /// draws, so the two cannot drift.
        ///
        /// ⚠️ `preserveAspect`, BECAUSE THE SOURCE IS 1835x527 AND THE BOX IS NOT. Without it the
        /// wordmark is squashed to whatever rect it was given, which is the one thing a logo may
        /// never be.
        ///
        /// ⚠️ AND A MISSING FILE FALLS BACK TO THE WORD. `Resources.Load` answers null rather
        /// than throwing, and a boot screen with a hole where the name should be is worse than
        /// one with the name set in the menu font.
        /// </summary>
        private void BuildLogo(Transform col, float y)
        {
            // ⚠️⚠️ `Texture2D` AND A `RawImage`, NOT `Sprite` AND AN `Image`, AND THE FIRST
            // VERSION SILENTLY FELL BACK TO THE WORD BECAUSE OF IT. `TUMP.png.meta` carries
            // `textureType: 0` and `spriteMode: 0`, so the asset is a plain texture and
            // `Resources.Load<Sprite>` answers **null** for a file that is right there. The
            // render showed the fallback label and nothing said why. `BuildArt` below makes the
            // same choice for the same reason: a `RawImage` draws whatever import settings the
            // file arrived with, and a `.meta` is a file nobody edits by hand and a re-import can
            // reset. ⚠️ The boot screen used to make it a third time in
            // `SplashScreen.BuildSplashArt`, which is deleted (`docs/TODO.md` § 114.3); this
            // screen is the only place `UI/splash_art` is drawn now.
            var logo = Resources.Load<Texture2D>("UI/main-menu/TUMP");

            if (logo == null)
            {
                MenuKit.Label(col, "TUMP", 34, UiTheme.PaperInk, Centre,
                    new Vector2(0.0f, y), new Vector2(360.0f, 44.0f));
                return;
            }

            // ⚠️⚠️ THE FITTER NEEDS A BOX OF ITS OWN, AND WITHOUT ONE THE WORDMARK ATE THE
            // SCREEN. `AspectRatioFitter.FitInParent` sizes the rect against its PARENT, not
            // against whatever size the rect was given, so putting it straight on a child of the
            // full-height column made the logo as wide as the column and drew it straight through
            // the username field. The first render of this showed TUMP three hundred pixels tall
            // behind the form.
            //
            // ⚠️ SO: a plain rect that owns the 360x104 slot, and the image fitted inside THAT.
            // One extra object, and the fitter's rule now applies to the space the layout meant.
            // ⚠️⚠️ THE WORDMARK IS NAILED TO A WOOD PLAQUE NOW, AND IT IS DRAWN UNTINTED.
            // 🧑 2026-09-01: **"LOGIN can be improved, especially TUMP logo in login"**.
            //
            // **The tint was the problem and no amount of choosing a better one fixes it.**
            // Sampling `Resources/UI/main-menu/TUMP.png` (1835x527): it is about 60 per cent warm
            // off-white in the `e0d0c0` family, which is the letter FACES, and about 40 per cent
            // `303030` to `404040`, which is a dark outline and a drop shadow baked into the file.
            // A `RawImage` colour MULTIPLIES, so tinting the whole asset `PaperInk` `3b2415` takes
            // the faces to about `201206` and the outline to about `0b0704`: **two things that
            // were four values apart end up one value apart, and the mark collapses into a brown
            // blob with a slightly darker rim.** That is what the last two passes were looking at.
            // Multiply can only darken, so there is no tint that lightens the faces back out.
            //
            // ⚠️⚠️ SO GIVE IT A DARK GROUND AND STOP TINTING IT. On `PaperCraft.Surface.Sign` the
            // asset draws in its authored colours: off-white letters with their own dark outline,
            // which is **exactly the picture the title screen shows** and the one composition this
            // mark was actually drawn for. It also puts the identity block on the same side of the
            // inversion as everything else in this pass: on a cream field the marker is the one
            // DARK thing (see `PaperCraft.Surface.Sign`, and § 119.10 for who decided it).
            //
            // ⚠️ AND IT IS THE MOST FAITHFUL USE OF HIS ART IN THE PROJECT, not a treatment of it.
            // `docs/VISION.md` § 6 and `CLAUDE.md` § 6.4: do not repaint his art. Every previous
            // version of this method tinted the file two or three times; this one draws it at
            // `Color.white`, which is the file.
            var plaque = new GameObject("LogoPlaque", typeof(RectTransform), typeof(Image));
            plaque.transform.SetParent(col, false);
            MenuKit.Place((RectTransform)plaque.transform, Centre,
                new Vector2(0.0f, y), new Vector2(LogoPlaqueWidth, LogoPlaqueHeight));

            PaperSkin.Apply(plaque, PaperCraft.Surface.Sign);
            plaque.GetComponent<Image>().raycastTarget = false;

            // ⚠️ THE FITTER'S BOX IS INSET FROM THE PLAQUE AND RAISED BY `PaperCraft.Drop`, which
            // is the same correction `PaperKit.CentreOnFace` makes for lettering and for the same
            // reason: the plaque draws its cast shadow inside its own bottom six units, so a mark
            // centred on the RECT is three units low on the FACE. `FitInParent` measures against
            // the parent, so the inset has to be a real rect and not an offset on the image.
            var box = new GameObject("LogoBox", typeof(RectTransform));
            box.transform.SetParent(plaque.transform, false);
            MenuKit.Stretch((RectTransform)box.transform, -LogoInset);
            ((RectTransform)box.transform).offsetMin =
                new Vector2(LogoInset, LogoInset + PaperCraft.Drop);

            // ⚠️⚠️⚠️ THE WORDMARK IS CARVED INTO THE PLANK RATHER THAN LAID ON IT, ON REQUEST.
            // 🧑 2026-09-01: *"it would look better if tump looked engraved into the wood like
            // color as opposed to just floating"*. He is right about the read: the asset is white
            // letters and the column is `WoodCraft` wood, so the game's name was the brightest
            // object on the screen and belonged to no surface. A carved sign is also what the
            // rest of this front end now claims to be made of.
            //
            // ⚠️⚠️⚠️ TWO COPIES, NOT THREE, AND THE THIRD ONE IS WHY THE FIRST ATTEMPT WAS MUD.
            // 🧑, on that attempt: *"make this look stamped/engraved/ better, it doesnt look
            // great right now"*. **The asset is not a silhouette.** `TUMP.png` is cream painted
            // letters that already carry a dark ink outline AND a grey drop shadow baked into the
            // file, so a uniform tint colours all three at once: the first version drew a dark
            // copy, a light copy and a face copy of a texture that is itself three things, which
            // is nine layers of edge in a 90-unit-tall word. It read as a brown blob with a halo.
            //
            // **So: one groove and one face.**
            //
            //   1. a copy tinted INK, three units UP -> the shadowed near wall of the cut. The
            //      asset's own baked outline is doing the right job here for once: tinted to ink
            //      it IS the wall.
            //   2. the face, dead centre, in a PALE WARM WOOD rather than a dark one.
            //
            // ⚠️⚠️ THE FACE IS LIGHTER THAN THE PLANK AND NOT DARKER, WHICH IS THE ONE PLACE THIS
            // DEPARTS FROM A TEXTBOOK ENGRAVE. A groove cut in wood and left bare is darker than
            // the board, and at this size that is an unreadable game name on the first screen a
            // player ever sees. A groove cut and then PAINTED is what every sari-sari sign in the
            // country actually is, and it is lighter. `VISION.md` opens on a street game; the sign
            // is the thing that survives the rain.
            //
            // ⚠️ THREE UNITS AND NOT TWO. The wordmark draws about 90 units tall, and at two the
            // groove was inside the asset's own baked outline and invisible.
            //
            // ⚠️⚠️ AND THIS IS A TREATMENT, NOT A REPAINT OF HIS ART. `CLAUDE.md` § 6.4 forbids
            // repainting authored art and 🧑 asked for this one directly, which is what that rule
            // defers to. The FILE is untouched and the title screen still draws it white; only
            // this screen tints its own three copies.
            // ⚠️⚠️ THE WORDMARK IS PRINTED ON THE CARD NOW, NOT CARVED INTO WOOD. The carve
            // was two copies of the mark, a dark groove offset three units up under a pale face,
            // and it was tuned against `UiTheme.WoodPanelFace`: on cream the pale face is lighter
            // than the card it sits on and the whole word disappears. 🧑 asked for the carve by
            // name on the wooden column (`docs/TODO.md` § 117.10) and that column is gone; what
            // survives the material change is the SHADOW, which is what makes a printed mark sit
            // on paper rather than float over it.
            //
            // ⚠️ THE PNG IS UNTOUCHED. `VISION.md` § 6: his art is the design system. This tints
            // a `RawImage`, which is a treatment applied to the file, not an edit of it.
            // ⚠️⚠️ ONE LAYER, AT `Color.white`. The shadow copy went with the tint: it existed to
            // make a flat ink silhouette sit ON the cream rather than float over it, and the
            // plaque's own cast shadow does that job for the whole block now. Two marks three
            // units apart, both dark, on a dark ground, would be a blur.
            var image = Engraved(box.transform, logo, "Logo", 0.0f, Color.white);
            image.raycastTarget = false;
        }

        /// <summary>
        /// The plaque the wordmark is nailed to.
        ///
        /// ⚠️ SIZED AGAINST THE MARK'S OWN ASPECT, NOT AGAINST THE COLUMN. `TUMP.png` is
        /// 1835x527, so 3.48:1; at 420 wide less 2 x 26 of inset the mark is 368 wide and about
        /// **106 tall**, and 120 units of plaque less the inset and the six-unit shadow leaves it
        /// 62. So the fit is decided by HEIGHT and the mark draws about 216 x 62 in the middle of
        /// the plaque, which is a signboard with margins rather than a mark with a frame drawn
        /// round it. ⚠️ It must stay taller than `IdleTabHeight` + the tab row's air, or the
        /// identity block and the FORM block below it stop being two blocks: at `Logo` 330 the
        /// plaque's underside is at 270 and the live tab's top is at 234.
        /// </summary>
        private const float LogoPlaqueWidth = 420.0f;
        private const float LogoPlaqueHeight = 120.0f;
        private const float LogoInset = 26.0f;

        /// <summary>
        /// One layer of the carved wordmark: the texture, fitted, tinted and nudged.
        ///
        /// ⚠️ `FitInParent` SIZES AGAINST THE PARENT, so all three layers share `LogoBox` and
        /// therefore arrive at exactly the same size. Giving each its own box would be three
        /// fitters measuring three rects and the layers would drift apart by a pixel at some
        /// aspect ratios, which on a carve reads as a blurry logo rather than as a misalignment.
        /// </summary>
        private static RawImage Engraved(Transform box, Texture2D logo, string name, float lift,
                                         Color tint)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(box, false);
            MenuKit.Stretch((RectTransform)go.transform);
            ((RectTransform)go.transform).anchoredPosition = new Vector2(0.0f, lift);

            var image = go.GetComponent<RawImage>();
            image.texture = logo;
            image.color = tint;
            image.raycastTarget = false;

            // ⚠️ FIT INSIDE, NOT ENVELOPE. The wordmark is 1835x527 and the slot is not that
            // shape; enveloping would crop the letters off both ends, which is the one thing a
            // logo may never be. The splash art envelopes because it is a background and cropping
            // it is correct.
            var fitter = go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = logo.width / (float)logo.height;

            return image;
        }

        /// <summary>
        /// The two modes, as one segmented control rather than as two buttons.
        ///
        /// ⚠⚠ THEY WERE TWO IDENTICAL PILLS AND THE ONLY THING SAYING WHICH ONE YOU WERE ON WAS
        /// THEIR COLOUR. `game-ui-design` names that twice: as a pattern (*"clear visual focus
        /// indicator, NOT just colour change ... visible for colourblind users"*) and as the
        /// `colorblind-failure` sharp edge. Amber against wood is also the ACCENT of this whole
        /// front end, so the lit tab and the primary action were saying the same thing in the same
        /// colour eight rows apart.
        ///
        /// **Three things say it now and only one of them is colour:** the pair sits in one
        /// RECESSED well, so they read as two halves of one control rather than as two choices;
        /// the live half is lit; and a chalk bar sits under it. The bar is the part that survives
        /// a colourblind player, a bad monitor and a photograph.
        ///
        /// ⚠️ 52 UNITS TALL, UP FROM 48, WHICH IS A TOUCH-TARGET FLOOR AND NOT A LOOK.
        /// `game-ui-design`'s `small-touch-target` rule is 44 minimum and 48 for a comfortable
        /// controller target; these are authored in canvas units that scale on the short axis, so
        /// 52 at the 1080 reference is never under 44 on a shipped window.
        /// </summary>
        private void BuildTabs(Transform col, float y)
        {
            // ⚠⚠⚠ THE WELL IS GONE. 🧑 2026-09-01, with a crop of exactly this control:
            // *"wtf is this"*. Two buttons inside a dark box is not a segmented control, it is two
            // buttons inside a dark box: the box added a third rectangle to a screen whose whole
            // complaint was too many identical rectangles, and it read as a black bar because a
            // recessed plank at this size is mostly its own shadow.
            //
            // **A tab strip is a LINE and the live tab sits ON it.** That is the metaphor every
            // player already knows, it costs one chalk rule instead of a plate, and it is the same
            // rule the heading two rows up already follows. The inactive tab is a sunken plate and
            // the live one is a raised amber one, so the pair differ in RELIEF as well as in
            // colour, which is what `UiMaterials.CarvedButton` was written for.
            // ⚠️⚠️ THE 436-UNIT STRIP RULE IS DELETED. 🧑 2026-09-01, with a crop of exactly
            // this: **"this line looks weird"**. It ran under BOTH tabs and 20 units past each of
            // them, so what a reader saw was a line sticking out either side of a pair of buttons
            // with a brighter amber line on top of half of it. The metaphor it was reaching for
            // (*"a tab strip is a LINE with the live tab standing on it"*) needs the line to be
            // read as the FLOOR the tabs stand on, and it cannot be when the tabs are opaque
            // plates that already have their own bright keylines.
            //
            // ⚠️ THE AMBER MARKER STAYS AND DOES THE WHOLE JOB. It is the half of the live-tab
            // signal that is not colour-plus-relief, it is the piece that survives a photograph,
            // and one bar under one tab is unambiguous in a way that two overlapping rules are
            // not.

            // ⚠⚠ THE LIVE TAB IS RAISED AND THE IDLE ONE IS RECESSED, WHICH IS THE SAME FIX
            // THE LOBBY'S TAB PAIR TOOK ON THE SAME DAY. A solid amber plate beside a plain wood
            // one says "this one" in hue alone, which `game-ui-design` lists as
            // `colorblind-failure`, and it also spent this screen's accent on a control that
            // states where you already are rather than on the button that does the thing.
            // `GodotTheme`'s `WoodTabIdleButton` note has the argument; the amber chalk bar below
            // and the four units of extra height are the other two signals.
            // ⚠️⚠️ TWO PAPER CHIPS, TOLD APART BY SURFACE AND WEIGHT AND NEVER BY HUE.
            // 🧑 2026-09-01, of the wooden pair: **"its also weird that create is just a
            // rectanhle"**. `PaperCraft` answers that at the level the complaint was made: the
            // live one is a filled `Token`, a pill with a physical lip, and the idle one is a
            // `Ghost`, two hairlines with almost nothing inside them. One is an object and the
            // other is an outline, and the difference survives a photograph.
            _signInTab = PaperKit.Chip(col, "SignInTab", "SIGN IN");
            _createTab = PaperKit.Chip(col, "CreateTab", "CREATE");

            _signInTab.onClick.AddListener(() => SetMode(false));
            _createTab.onClick.AddListener(() => SetMode(true));

            MenuKit.Place((RectTransform)_signInTab.transform, Centre,
                          new Vector2(-105.0f, y), new Vector2(198.0f, LiveTabHeight));
            MenuKit.Place((RectTransform)_createTab.transform, Centre,
                          new Vector2(105.0f, y), new Vector2(198.0f, IdleTabHeight));

            // ⚠️⚠️ BOTH TABS HANG FROM ONE BOTTOM EDGE, so the live one grows UPWARD out of the
            // row instead of swelling around its own centre. A pair that grows from the middle
            // reads as two boxes at two sizes; a pair that shares a floor reads as one standing
            // forward, which is the whole metaphor.
            Hang(_signInTab, -105.0f, y);
            Hang(_createTab, 105.0f, y);

            // ⚠️ THE MARKER IS A SIBLING OF THE TABS AND NOT A CHILD OF EITHER, so switching
            // modes moves one object instead of showing one and hiding another. Two markers is
            // two things to keep in step and one of them is always the one somebody forgets.
            // ⚠️ THE AMBER BAR SITS ON THE STRIP LINE, NOT BELOW IT, so the live tab reads as
            // standing on the rule while the other one hangs off it. Same y as the rule above.
            // ⚠️⚠️⚠️ THE AMBER MARKER BAR IS DELETED. 🧑 2026-09-01, with a crop of this row:
            // *"this stray light brown adn yellow line dont make sense i think its leftover stuff
            // from old ui, adapt it into our current"*. He is right about where it came from: the
            // bar and the 436-unit strip rule under it were both written for the pass BEFORE the
            // tabs had two shapes, when the only difference between them was a fill and a
            // colourblind player had nothing to read. **That is no longer true.** The live tab is
            // raised, its face is `793e1f` against `36180c`, its label is full cream against
            // muted, and it now stands eight units taller than its neighbour: four signals, three
            // of which survive a photograph in greyscale.
            //
            // A fifth one drawn as a loose chalk bar floating under one of two plates is not
            // reinforcement, it is a leftover, and it is the last mark on this screen that does
            // not belong to a control. `game-ui-design`'s rule is that a state must not be told by
            // COLOUR ALONE; it does not ask for a mark per state.

            FocusRing.Attach(_signInTab.gameObject, 4.0f);
            FocusRing.Attach(_createTab.gameObject, 4.0f);
        }

        /// <summary>
        /// How tall the live tab is, and how tall the other one is.
        ///
        /// ⚠️ THE SAME PAIR THE LOBBY USES, ON PURPOSE. `LobbyChrome.LiveTabHeight` carries the
        /// argument: a shape difference beats a fill difference at small sizes, and it is the one
        /// signal a colourblind player and a greyscale screenshot both keep. Two screens with two
        /// different tab conventions would be worse than either.
        /// </summary>
        private const float LiveTabHeight = 60.0f;
        private const float IdleTabHeight = 52.0f;

        /// <summary>The row both tabs hang from, remembered so <see cref="SetMode"/> can move
        /// them without knowing the column's geometry.</summary>
        private float TabsY;

        /// <summary>
        /// Hangs a tab from the row's shared bottom edge, at the height its state calls for.
        ///
        /// ⚠️ THE PIVOT IS THE BOTTOM, so growing the live tab moves its TOP edge and leaves the
        /// floor alone. With a centred pivot the taller tab would also drop four units below its
        /// neighbour, and a row whose baseline moves when you press it is worse than a row with no
        /// difference at all.
        /// </summary>
        private void Hang(Button tab, float x, float y, bool live = false)
        {
            if (tab == null) return;

            TabsY = y;

            var rt = (RectTransform)tab.transform;
            rt.pivot = new Vector2(0.5f, 0.0f);
            rt.anchoredPosition = new Vector2(x, y - (IdleTabHeight * 0.5f));
            rt.sizeDelta = new Vector2(198.0f, live ? LiveTabHeight : IdleTabHeight);
        }

        private void BuildPrimary(Transform col, float y)
        {
            var button = MenuKit.WoodButton(col, "SIGN IN", Centre,
                new Vector2(0.0f, y), new Vector2(420.0f, 62.0f), Submit,
                "WoodPrimaryButton");

            _primaryLabel = button.GetComponentInChildren<Text>();
        }

        private InputField Field(Transform col, string caption, float y, string placeholder,
                                 int limit, bool password)
        {
            // ⚠️ THE LABEL IS ABOVE THE BOX AND TINY, which is the reference's arrangement and is
            // not a style choice: a caption to the LEFT of a field, which is what the old panel
            // did, forces every field to be narrow enough to leave room for the widest caption,
            // and "COUNTRY CODE (OPTIONAL)" was the widest.
            // ⚠️⚠️ CENTRED, ON REQUEST, AND IT OVERRIDES THE REFERENCE ON PURPOSE. 🧑, looking
            // at the render: *"put USER NAME and PASSWORD Text in the middle bcz everything is
            // cetnnered excepot for them"*. He is right about this screen: the logo, the heading,
            // the segmented pair, both fields, the primary and the guest button are all centred
            // on one axis, so two left-aligned micro-labels were the only things off it, and one
            // exception in a column of eight reads as a mistake rather than as a hierarchy.
            //
            // ⚠️ THE RIOT LAYOUT PUTS THEM LEFT AND THAT IS STILL TRUE, but its column is a
            // left-aligned form: the fields, the button and the links all share a left edge, so
            // the caption is aligned to something. Ours is centred, so copying the caption's
            // alignment without copying the column's would be copying the look and not the rule.
            // `FUTURE.md` § 0.5b: **name the mechanism, then check whether this game's content
            // has the shape the mechanism assumes.**
            var captionLabel = MenuKit.Label(col, caption, PaperKit.Caption,
                UiTheme.PaperInkSoft,
                Centre, new Vector2(0.0f, y + 44.0f), new Vector2(420.0f, 24.0f),
                TextAnchor.MiddleLeft);
            captionLabel.raycastTarget = false;

            var go = new GameObject($"Field_{caption}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(col, false);
            MenuKit.Place((RectTransform)go.transform, Centre,
                new Vector2(0.0f, y), new Vector2(420.0f, 58.0f));

            // ⚠⚠ THE FIELD IS PAPER NOW RATHER THAN A FLAT CREAM RECTANGLE WITH AN INK
            // BORDER. 🧑 2026-09-01: *"make sure all ui isnt generated in the same way but
            // follows a central theme bcz old issue was it read as repetitive with everyone just
            // being brown and boring"*. **Cream is already a SURFACE in his art and not only a
            // text colour**: these two boxes are the only thing on this screen that is not brown,
            // and they were drawn by the same `GodotTheme.Box` that draws every wooden plate in
            // the game, at a different fill.
            //
            // `WoodCraft.Surface.PaperField` is built by different rules from any wooden surface:
            // no keyline, no rim, no varnish band, no ramp, a small CONSTANT corner radius rather
            // than a fraction of the height, a two-dimensional fibre speckle because paper has no
            // grain direction, one ink hairline, and a shadow along the TOP edge only, which is
            // the near wall of anything cut into a surface lit from above. Two surfaces that
            // share a palette and nothing else cannot read as the same object.
            var image = go.GetComponent<Image>();
            PaperSkin.Apply(go, PaperCraft.Surface.Tray);

            var input = go.AddComponent<InputField>();
            input.targetGraphic = image;
            input.characterLimit = limit;
            input.lineType = InputField.LineType.SingleLine;
            if (password) input.contentType = InputField.ContentType.Password;

            var text = MenuKit.Label(go.transform, "", PaperKit.Body, UiTheme.PaperInk,
                new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            MenuKit.Stretch(text.rectTransform, -16.0f);
            text.alignment = TextAnchor.MiddleLeft;
            input.textComponent = text;

            var ghost = MenuKit.Label(go.transform, placeholder, PaperKit.Caption,
                UiTheme.PaperInkSoft,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            MenuKit.Stretch(ghost.rectTransform, -16.0f);
            ghost.alignment = TextAnchor.MiddleLeft;
            input.placeholder = ghost;

            // ⚠⚠ A FIELD WITH THE KEYBOARD LOOKS DIFFERENT FROM ONE WITHOUT, AND UNTIL NOW IT
            // DID NOT. Unity's default is a barely-visible tint on the target graphic, over a
            // cream card, which is `game-ui-design`'s `colorblind-failure` on the one control on
            // the screen where knowing where you are typing IS the interaction. See `FocusRing`.
            FocusRing.Attach(go, 4.0f);

            // ⚠⚠ ENTER SUBMITS FROM EITHER FIELD, AND IT USED TO SUBMIT FROM NEITHER. The form
            // had `onSubmit` on nothing: a player who typed a password and pressed Enter, which is
            // what everybody does, got nothing at all and had to reach for the mouse. `onSubmit`
            // fires for both fields, so the reflex works from wherever the caret is.
            input.onSubmit.AddListener(_ => Submit());

            return input;
        }

        /// <summary>
        /// TAB walks the form, and SHIFT+TAB walks it backwards.
        ///
        /// ⚠⚠ UNITY'S BUILT-IN TAB NAVIGATION IS `Selectable.navigation`, WHICH IS OFF BY
        /// DEFAULT ON EVERYTHING BUILT IN CODE. `game-ui-design` calls a menu you cannot leave
        /// without a pointer a `Controller Navigation Deadend`, and it lists it as an anti-pattern
        /// AND a sharp edge because it is the failure that makes a screen unusable rather than
        /// ugly. This is the keyboard half; the same explicit chain is what a gamepad would walk
        /// when Phase 14 gives this build its first stick binding.
        ///
        /// ⚠️ IT IS AN EXPLICIT CHAIN AND NOT `Automatic`. Unity's automatic mode picks the
        /// nearest selectable by DIRECTION on screen, and this column has a two-up segmented
        /// control at the top of it: from the password field, "up" is ambiguous between two tabs
        /// that sit at the same height, and the answer changes with the window's aspect.
        /// </summary>
        private static void Chain(params Selectable[] order)
        {
            for (int i = 0; i < order.Length; i++)
            {
                if (order[i] == null) continue;

                var nav = new Navigation { mode = Navigation.Mode.Explicit };

                for (int back = i - 1; back >= 0; back--)
                    if (order[back] != null) { nav.selectOnUp = order[back]; nav.selectOnLeft = order[back]; break; }

                for (int forward = i + 1; forward < order.Length; forward++)
                    if (order[forward] != null) { nav.selectOnDown = order[forward]; nav.selectOnRight = order[forward]; break; }

                order[i].navigation = nav;
            }
        }

        // -------------------------------------------------------------------
        // § BEHAVIOUR
        // -------------------------------------------------------------------

        public void Open()
        {
            // ⚠️⚠️ THE FIELDS ARE CLEARED BEFORE THE MODE IS SET, NOT AFTER, AND THE FIRST
            // RENDER OF THIS SCREEN IS WHY. `SetMode` writes the line explaining what CREATE
            // ACTUALLY DOES ("keeps everything you have played on this machine"), and clearing
            // the error afterwards wiped it every time: the screenshot shows a blank gap where
            // the one sentence distinguishing the two modes should be.
            _username.text = GameServices.Account?.Username ?? "";
            _password.text = "";
            _error.text = "";

            SetMode(GameServices.Account != null && !GameServices.Account.HasPassword);

            // ⚠️ REACHED BY A PRESS IT IS ALWAYS THE FORM. The welcome-back state answers "is this
            // still you" at boot; a player who pressed SIGN IN has already said it is not.
            //
            // ⚠️⚠️ AND IT RUNS BEFORE `SetBootMode`, WHICH IS AN ORDER DEPENDENCY A PROBE FOUND
            // RATHER THAN A PREFERENCE. `ShowWelcome` reactivates every form piece, and `_back`
            // is one of them, so calling it AFTER `SetBootMode` undid the one thing `SetBootMode`
            // does: `TheLoginStepAppearsEveryLaunchAndOnePressLeavesIt` came back red with **BACK
            // visible on the boot screen**, which dismisses to nothing at all. `SetBootMode` is
            // the narrower statement and it goes last.
            ShowWelcome(false);
            SetBootMode(false);
            _autoPassAt = -1.0f;

            _root.SetActive(true);
            Opened?.Invoke(true);
            FocusFirstField();
        }

        /// <summary>
        /// Puts the caret in the first field the player would type in.
        ///
        /// ⚠⚠ A FORM THAT OPENS WITH NOTHING FOCUSED COSTS A CLICK BEFORE IT COSTS A KEYSTROKE,
        /// and on the boot screen that click is the first thing anybody does in this game.
        /// `game-ui-design`: *"remember last position when returning to menu"*, and the position
        /// on arriving is the first thing you have to fill in.
        ///
        /// ⚠️ IT PICKS THE EMPTY ONE. A returning player's username is already filled in from
        /// the account, so focusing it would make them tab past their own name to reach the
        /// password. This is one line and it is the difference between the form knowing what you
        /// came to do and not.
        /// </summary>
        private void FocusFirstField()
        {
            var target = string.IsNullOrEmpty(_username.text) ? _username : _password;
            target.Select();
            target.ActivateInputField();
        }

        /// <summary>
        /// The same screen, opened on CREATE, for a caller who already knows the player wants to
        /// attach a credential rather than switch accounts.
        ///
        /// ⚠️ IT IS <see cref="Open"/> WITH THE MODE FORCED, NOT A THIRD ENTRY POINT. The hub's
        /// CONNECT row knows which of the two verbs it means; `Open` guesses from
        /// `HasPassword`, and for a player who HAS a password but no Google account that guess is
        /// the wrong one.
        /// </summary>
        public void OpenForUpgrade()
        {
            Open();
            SetMode(true);
        }

        /// <summary>
        /// The same screen, as the LOGIN step of the boot sequence, on every launch.
        ///
        /// ⚠️⚠️ "ONCE PER MACHINE" IS GONE AND THAT WAS 🧑'S CALL ON 2026-09-01, WITH THE FLOW
        /// WRITTEN OUT: *"i want it to strictly be this: UNITY -> BH STUDIOS ANIMATION -> LOGIN ->
        /// MAIN MENU -> LOBBY"*. `GameSettings.AccountChoiceMade` gated this on the FIRST launch
        /// of an install, so the step he drew existed for one boot and then vanished for ever.
        /// The flag is still written and still read by `ShouldOfferUpgrade`; it no longer decides
        /// whether this screen appears. `docs/TODO.md` § 114.5.
        ///
        /// ⚠️⚠️ AND A RETURNING PLAYER IS NOT ASKED A QUESTION THEY HAVE ALREADY ANSWERED. With a
        /// username attached this opens on the welcome-back state, which names them and lets go
        /// after <see cref="WelcomeHold"/>. **A login step that costs a press on every launch is
        /// a tax**, and the reason it is worth having at all is that a player who wants to switch
        /// accounts currently has to find the ACCOUNT tab to do it.
        ///
        /// ⚠️⚠️ THIS REVERSES `docs/TODO.md` § 92.3, WHICH CALLED THE BOOT BEHAVIOUR "THE ONE
        /// THING THAT MUST NOT MOVE". 🧑, 2026-08-31: *"i want this like pubg but they have ann
        /// option to continue right as a guest"*. `GameSettings.AccountChoiceMade` carries the
        /// full argument for why both the old rule and this can be true: the rule was about a
        /// FORM appearing unasked, and this is a CHOICE with a one-press escape.
        ///
        /// ⚠️⚠️ THE ESCAPE IS THE ENTIRE DESIGN AND IT MUST NEVER NEED THE NETWORK.
        /// `FUTURE.md` § 0.5 rule 7 and the nationals in General Santos City: the game has to
        /// reach a match with the cable out. CONTINUE AS GUEST does not call a service, does not
        /// await anything and cannot fail; the anonymous account is already signed in behind the
        /// loading screen, or has already fallen back to the local profile, before this screen is
        /// ever built. **If this button ever grows an `await`, this screen has become the thing
        /// § 92.3 refused.**
        ///
        /// ⚠️ BACK IS HIDDEN, because at boot there is nothing behind it. A button that dismisses
        /// a screen to reveal nothing is how a player gets stuck on a black frame.
        /// </summary>
        public void OpenAtBoot()
        {
            _username.text = "";
            _password.text = "";
            _error.text = "";

            // ⚠️ CREATE RATHER THAN SIGN IN, because a first-time player has no account to sign
            // in to and the CREATE copy is the line that says what happens to what they have
            // already played. A returning player who wants SIGN IN presses one segment.
            SetMode(true);

            // ⚠️ BOTH HALVES ARE REQUIRED. `HasPassword` is a settings flag and `Username` is the
            // profile's, and an install that has one without the other is a half-attached account
            // that must be shown the form rather than greeted by name.
            var account = GameServices.Account;
            bool attached = account != null && account.HasPassword &&
                            !string.IsNullOrWhiteSpace(account.Username);

            // ⚠️⚠️ `ShowWelcome` BEFORE `SetBootMode`, AND THE ORDER IS LOAD-BEARING. See the
            // note in `Open`: `ShowWelcome(false)` reactivates every form piece including `_back`,
            // so running it second put BACK back on a screen with nothing behind it.
            ShowWelcome(attached);
            SetBootMode(true);

            if (attached)
            {
                if (_welcomeName != null) _welcomeName.text = account.LobbyName;
                if (_welcomeHint != null) _welcomeHint.text = "press anything to stay here";
                _autoPassAt = Time.unscaledTime + WelcomeHold;
            }
            else
            {
                _autoPassAt = -1.0f;
            }

            _root.SetActive(true);
            Opened?.Invoke(true);
        }

        private void SetBootMode(bool atBoot)
        {
            _atBoot = atBoot;

            if (_back != null) _back.gameObject.SetActive(!atBoot);

            var caption = _guest != null ? _guest.GetComponentInChildren<Text>(true) : null;
            if (caption != null) caption.text = atBoot ? "CONTINUE AS GUEST" : "PLAY AS GUEST";

            // ⚠⚠ THE KEY HINT FOLLOWS THE LAST VISIBLE BUTTON, NOT THE LAST BUTTON. BACK is
            // hidden at boot, so a hint anchored under it floated 60 units below nothing on the
            // one screen every player meets first. `CLAUDE.md` § 6.2b row 1: a screen with a mode
            // has two layouts and you have looked at one.
            if (_keyHint != null)
            {
                float anchor = atBoot ? _guest.transform.localPosition.y
                                      : _back.transform.localPosition.y;
                var rect = (RectTransform)_keyHint.transform;
                rect.anchoredPosition = new Vector2(0.0f, anchor - 46.0f);

                // ⚠️ ESC IS NOT OFFERED AT BOOT, because it does nothing there: the screen is not
                // dismissable when there is nothing behind it (see `Update`). A hint naming a key
                // that is inert is worse than no hint, and `game-ui-design`'s `Inconsistent Button
                // Behavior` is the same rule for a control.
                _keyHint.text = atBoot
                    ? (_creating ? "TAB to move  ·  ENTER to create" : "TAB to move  ·  ENTER to sign in")
                    : (_creating ? "TAB to move  ·  ENTER to create  ·  ESC to go back"
                                 : "TAB to move  ·  ENTER to sign in  ·  ESC to go back");
            }
        }

        /// <summary>
        /// ⚠️ THE CHOICE IS RECORDED WHICHEVER WAY IT WENT, so the screen is shown once per
        /// machine and never again. Creating an account, signing in and continuing as a guest are
        /// all answers to the question; only closing the screen without answering is not, and at
        /// boot there is no way to do that.
        /// </summary>
        private static void RememberTheChoiceWasMade()
        {
            var settings = Settings.SettingsStore.Current;
            if (settings == null || settings.AccountChoiceMade) return;

            settings.AccountChoiceMade = true;
            Settings.SettingsStore.Save();
        }

        private void Close()
        {
            _root.SetActive(false);
            Opened?.Invoke(false);
            Closed?.Invoke();
        }

        /// <summary>
        /// ⚠️⚠️ CREATE AND SIGN IN ARE DIFFERENT CALLS AND THE DIFFERENCE MATTERS TO THE PLAYER'S
        /// PROGRESS. `UpgradeAsync` attaches a username to the anonymous account this machine has
        /// been playing on, so everything earned so far is kept; `SignInAsync` moves to a
        /// different account and this machine's anonymous progress is left behind. The heading
        /// says which one is about to happen, because the panel this replaces had both as
        /// same-sized buttons in a row of three and nothing told anybody.
        /// </summary>
        private void SetMode(bool creating)
        {
            _creating = creating;

            // ⚠️ THE PRIMARY IS THE ONLY LABEL THAT CHANGES WITH THE MODE NOW. This used to write
            // the identical string into a heading as well; see `BuildColumn`'s note on the three
            // CREATEs.
            if (_primaryLabel != null) _primaryLabel.text = creating ? "CREATE ACCOUNT" : "SIGN IN";
            _error.text = creating
                ? "Keeps everything you have played on this machine."
                : "";
            _error.color = creating ? UiTheme.PaperInkSoft : UiTheme.MenuRed;

            // ⚠️ THE GOOGLE VERB FOLLOWS THE TAB TOO, so the two ways of doing the same thing
            // agree about which thing it is. CONNECT keeps this machine's progress; SIGN IN moves
            // to another account. See `PlayerAccount.LinkGoogleAsync`.
            if (_googleButton != null)
            {
                var googleLabel = _googleButton.GetComponentInChildren<Text>();
                if (googleLabel != null)
                {
                    googleLabel.text = creating ? "CONNECT A GOOGLE ACCOUNT" : "SIGN IN WITH GOOGLE";
                    MenuKit.Fit(googleLabel, 420.0f - 32.0f);
                }
            }

            SetTab(_signInTab, !creating);
            SetTab(_createTab, creating);

            // ⚠️ THE HEIGHT MOVES WITH THE PAINT, which is the half of this that is not a
            // colour. See `BuildTabs`: the marker bar that used to do this job is deleted.
            Hang(_signInTab, -105.0f, TabsY, !creating);
            Hang(_createTab, 105.0f, TabsY, creating);

            // ⚠️ THE VERB FOLLOWS THE TAB AND THE KEY LIST FOLLOWS THE MODE, so this defers to
            // `SetBootMode` rather than writing a string that names ESC on a screen where ESC
            // does nothing. Both run on open; this one runs first.
            if (_keyHint != null)
                _keyHint.text = _atBoot
                    ? (creating ? "TAB to move  ·  ENTER to create" : "TAB to move  ·  ENTER to sign in")
                    : (creating ? "TAB to move  ·  ENTER to create  ·  ESC to go back"
                                : "TAB to move  ·  ENTER to sign in  ·  ESC to go back");
        }

        /// <summary>
        /// ⚠⚠ THE LIVE TAB IS TALLER AS WELL AS AMBER, AND THE SIZE IS THE HALF THAT SURVIVES A
        /// COLOURBLIND PLAYER OR A PHOTOGRAPH. `game-ui-design`'s ordering tools are position,
        /// size, weight and colour; this pair was using the last one alone. Four units is small
        /// enough not to move the row and large enough to read as "this one is in front".
        /// </summary>
        private static void SetTab(Button button, bool on)
        {
            if (button == null) return;

            var rect = (RectTransform)button.transform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, on ? LiveTabHeight : IdleTabHeight);

            // ⚠️⚠️ `Live` AGAINST `Ghost`, WHICH THE LOBBY MOVED TO AND THIS SCREEN DID NOT.
            // `docs/TODO.md` § 119.11 named this as the first thing that pass left undone, and it
            // was an omission rather than a decision: the lobby's pair was changed after
            // `Logs/shots-runtime/Lobby-v52.png` measured `Token` against `Ghost` at **4 per cent
            // apart in value**, and the login screen was not re-shot between those two changes.
            // Two screens with two tab conventions is worse than either, which is the argument
            // `LiveTabHeight` already makes about the heights.
            //
            // ⚠️ AND THAT IS A VALUE INVERSION OF ABOUT 10:1 rather than a hue: a wood-dark pill
            // with cream lettering, which spends no colour and puts a little of 🧑's own wood back
            // on a card that is otherwise entirely paper.
            var skin = button.GetComponent<PaperSkin>();
            if (skin != null)
            {
                skin.Surface = on ? PaperCraft.Surface.Live : PaperCraft.Surface.Ghost;
                skin.Rebuild();
            }

            var label = button.transform.Find("Label")?.GetComponent<Text>();
            if (label == null) return;

            // ⚠️ CREAM ON THE LIVE ONE NOW, BECAUSE IT IS WOOD-DARK. `PaperButton` reads the
            // surface and does exactly this on its own, but it only does it when something makes
            // it look; this method is the something on the frame the mode changes.
            label.fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
            label.color = on ? UiTheme.Cream : UiTheme.PaperInkSoft;

            var chip = button.GetComponent<PaperButton>();
            if (chip != null) chip.Restyle();
        }

        /// <summary>
        /// ⚠️ VALIDATED HERE RATHER THAN LET THROUGH TO THE SERVICE. An empty username reaches
        /// UGS as a request that fails with a message written for a developer, and the player
        /// reads it. Saying what is missing is one line and it is the difference between a form
        /// that helps and one that scolds.
        /// </summary>
        private async void Submit()
        {
            string username = _username.text?.Trim() ?? "";
            string password = _password.text ?? "";

            if (string.IsNullOrEmpty(username)) { Fail("Enter a username."); return; }
            if (string.IsNullOrEmpty(password)) { Fail("Enter a password."); return; }

            var account = GameServices.Account;
            if (account == null) { Fail("Accounts are not available right now."); return; }

            try
            {
                _error.color = UiTheme.PaperInkSoft;
                _error.text = _creating ? "Creating your account..." : "Signing in...";

                if (_creating) await account.UpgradeAsync(username, password);
                else await account.SignInAsync(username, password);

                RememberTheChoiceWasMade();
                Close();
            }
            catch (Exception e)
            {
                Fail(e.Message);
            }
        }

        /// <summary>
        /// The Google half of <see cref="Submit"/>, and it is deliberately the same shape.
        ///
        /// ⚠⚠ THE PLAYER IS TOLD TO LOOK AT THEIR BROWSER, BECAUSE THE NEXT THING THAT HAPPENS
        /// IS OUTSIDE THIS GAME. `Application.OpenURL` hands a tab to whatever browser is default,
        /// and on this machine that window can open BEHIND a full-screen game; a player watching
        /// a frozen-looking button with no message would reasonably press it again and start a
        /// second flow. One sentence, on the screen, before the browser opens.
        ///
        /// ⚠️ AND THE BUTTON IS DISABLED FOR THE DURATION for the same reason: two listeners on
        /// two loopback ports, one consent screen, and only one of them can ever be answered.
        /// </summary>
        private async void GooglePressed()
        {
            var account = GameServices.Account;
            if (account == null) { Fail("Accounts are not available right now."); return; }

            if (_googleButton != null) _googleButton.interactable = false;

            try
            {
                _error.color = UiTheme.PaperInkSoft;
                _error.text = "Finish signing in on the browser window.";

                if (_creating) await account.LinkGoogleAsync();
                else await account.SignInWithGoogleAsync();

                RememberTheChoiceWasMade();
                Close();
            }
            catch (Exception e)
            {
                Fail(e.Message);
            }
            finally
            {
                if (_googleButton != null) _googleButton.interactable = true;
            }
        }

        private void GuestPressed()
        {
            if (_atBoot) BootGuest();
            else Guest();
        }

        /// <summary>
        /// CONTINUE AS GUEST at boot: record the answer and get out of the way.
        ///
        /// ⚠️⚠️ IT DELIBERATELY DOES NOT CALL `SignInAsGuest`, AND CALLING IT WOULD HAVE BEEN
        /// THE OBVIOUS MISTAKE. That method is the TOURNAMENT guest: it parks the owner's profile
        /// in `_primaryProfile` and hands the machine to somebody else for a session, and
        /// `LeaveGuest` throws away what the guest earned. Running it here would make every
        /// first-time player a temporary user of their own game and quietly bin their first
        /// evening's progress.
        ///
        /// ⚠️ THERE IS NOTHING TO DO BECAUSE IT HAS ALREADY HAPPENED. `PlayerAccount` signs in
        /// anonymously behind the loading screen, or settles to the local profile if there is no
        /// service, before this screen exists. "Continue as guest" is the player accepting the
        /// account they were already given, so the only state that changes is that we stop
        /// asking.
        /// </summary>
        private void BootGuest()
        {
            RememberTheChoiceWasMade();
            Close();
        }

        private void Guest()
        {
            try
            {
                GameServices.Account?.SignInAsGuest(GameServices.Account.DisplayName);
                RememberTheChoiceWasMade();
                Close();
            }
            catch (Exception e) { Fail(e.Message); }
        }

        private void Fail(string message)
        {
            // ⚠️ `MenuRed`, NOT `Danger`. `f80000` MEANS downed or out of bounds in the match,
            // and the sibling of `CLAUDE.md` § 6.4 is that a colour with a meaning is not a paint.
            // It is also unreadably hot on cream.
            _error.color = UiTheme.MenuRed;
            _error.text = message ?? "";
        }
    }
}
