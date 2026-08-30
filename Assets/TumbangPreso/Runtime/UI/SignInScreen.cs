using System;
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
        private const float ColumnUnits = 580.0f;

        private Canvas _canvas;
        private GameObject _root;
        private InputField _username, _password;
        private Text _heading, _error, _primaryLabel;
        private Button _signInTab, _createTab;
        private Button _guest, _back;

        /// <summary>
        /// True while this screen is the first thing the game showed, rather than something the
        /// player pressed. See <see cref="OpenAtBoot"/>.
        /// </summary>
        private bool _atBoot;

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
            if (!IsOpen || _atBoot) return;
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            Close();
            MenuSfx.Back();
        }
        private bool _creating;

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

        public void Install()
        {
            if (_canvas != null) return;

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

            var side = new GameObject("ArtSide", typeof(RectTransform));
            side.transform.SetParent(_root.transform, false);

            var sideRt = (RectTransform)side.transform;
            sideRt.anchorMin = Vector2.zero;
            sideRt.anchorMax = Vector2.one;
            sideRt.offsetMin = new Vector2(ColumnUnits, 0.0f);
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
            var rt = (RectTransform)columnGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(0.0f, 1.0f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(ColumnUnits, 0.0f);

            columnGo.GetComponent<Image>().color = UiTheme.WoodDeep;

            // ⚠️ THE COLUMN IS OPAQUE AND THE REST IS NOT. That contrast is what makes the form
            // read as the only thing you can act on, which is the whole point of the reference.
            var skin = columnGo.AddComponent<GodotPanel>();
            skin.Variation = "WoodPanel";
            skin.ApplyContentMargins = false;
            skin.Apply();

            var col = columnGo.transform;

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
            const float Logo = 300.0f;
            const float Heading = 210.0f;
            const float Tabs = 140.0f;
            const float UserField = 20.0f;
            const float PassField = -100.0f;
            const float Hint = -170.0f;
            const float Primary = -240.0f;

            BuildLogo(col, Logo);

            _heading = MenuKit.Label(col, "SIGN IN", 40, UiTheme.Cream, Centre,
                new Vector2(0.0f, Heading), new Vector2(420.0f, 54.0f));

            BuildTabs(col, Tabs);

            _username = Field(col, "USERNAME", UserField, "your username", 64, false);
            _password = Field(col, "PASSWORD", PassField, "your password", 128, true);

            // ⚠️ THE ERROR SITS UNDER THE FIELDS RATHER THAN IN A SHARED STATUS LINE AT THE TOP.
            // The old panel had one `_status` label that reported saving a profile, linking a
            // username, signing in and arming a delete, so the sentence on screen was whichever
            // of six unrelated actions ran last.
            _error = MenuKit.Label(col, "", MenuKit.MinReadableUnits, UiTheme.Danger,
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
            _guest = MenuKit.WoodButton(col, "PLAY AS GUEST", Centre,
                new Vector2(0.0f, -320.0f), new Vector2(300.0f, 48.0f), GuestPressed);

            _back = MenuKit.WoodButton(col, "BACK", Centre,
                new Vector2(0.0f, -382.0f), new Vector2(300.0f, 48.0f), Close);
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
            // render showed the fallback label and nothing said why. The splash art
            // (`SplashScreen.BuildSplashArt`) already made this choice for the same reason: a
            // `RawImage` draws whatever import settings the file arrived with, and a `.meta` is
            // a file nobody edits by hand and a re-import can reset.
            var logo = Resources.Load<Texture2D>("UI/main-menu/TUMP");

            if (logo == null)
            {
                MenuKit.Label(col, "TUMP", 30, UiTheme.Amber, Centre,
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
            var box = new GameObject("LogoBox", typeof(RectTransform));
            box.transform.SetParent(col, false);
            MenuKit.Place((RectTransform)box.transform, Centre,
                new Vector2(0.0f, y), new Vector2(360.0f, 104.0f));

            var go = new GameObject("Logo", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(box.transform, false);
            MenuKit.Stretch((RectTransform)go.transform);

            var image = go.GetComponent<RawImage>();
            image.texture = logo;
            image.raycastTarget = false;

            // ⚠️ FIT INSIDE, NOT ENVELOPE. The wordmark is 1835x527 and the slot is not that
            // shape; enveloping would crop the letters off both ends, which is the one thing a
            // logo may never be. The splash art envelopes because it is a background and cropping
            // it is correct.
            var fitter = go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = logo.width / (float)logo.height;
        }

        private void BuildTabs(Transform col, float y)
        {
            _signInTab = MenuKit.WoodButton(col, "SIGN IN", Centre,
                new Vector2(-108.0f, y), new Vector2(206.0f, 48.0f), () => SetMode(false),
                "WoodAmberButton");

            _createTab = MenuKit.WoodButton(col, "CREATE", Centre,
                new Vector2(108.0f, y), new Vector2(206.0f, 48.0f), () => SetMode(true));
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
            MenuKit.Label(col, caption, MenuKit.MinReadableUnits, UiTheme.CreamMuted,
                Centre, new Vector2(0.0f, y + 46.0f), new Vector2(420.0f, 26.0f),
                TextAnchor.MiddleCenter);

            var go = new GameObject($"Field_{caption}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(col, false);
            MenuKit.Place((RectTransform)go.transform, Centre,
                new Vector2(0.0f, y), new Vector2(420.0f, 58.0f));

            var image = go.GetComponent<Image>();
            image.color = UiTheme.Card;

            var skin = go.AddComponent<GodotPanel>();
            skin.Variation = "Card";
            skin.ApplyContentMargins = false;
            skin.Apply();

            var input = go.AddComponent<InputField>();
            input.targetGraphic = image;
            input.characterLimit = limit;
            input.lineType = InputField.LineType.SingleLine;
            if (password) input.contentType = InputField.ContentType.Password;

            var text = MenuKit.Label(go.transform, "", 20, UiTheme.Ink, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            MenuKit.Stretch(text.rectTransform, -16.0f);
            text.alignment = TextAnchor.MiddleLeft;
            input.textComponent = text;

            var ghost = MenuKit.Label(go.transform, placeholder, MenuKit.MinReadableUnits,
                UiTheme.InkMuted,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft);
            MenuKit.Stretch(ghost.rectTransform, -16.0f);
            ghost.alignment = TextAnchor.MiddleLeft;
            input.placeholder = ghost;

            return input;
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
            SetBootMode(false);
            _root.SetActive(true);
            Opened?.Invoke(true);
        }

        /// <summary>
        /// The same screen, as the first thing the game shows, once per machine.
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
            SetBootMode(true);
            _root.SetActive(true);
            Opened?.Invoke(true);
        }

        private void SetBootMode(bool atBoot)
        {
            _atBoot = atBoot;

            if (_back != null) _back.gameObject.SetActive(!atBoot);

            var caption = _guest != null ? _guest.GetComponentInChildren<Text>(true) : null;
            if (caption != null) caption.text = atBoot ? "CONTINUE AS GUEST" : "PLAY AS GUEST";
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
            _heading.text = creating ? "CREATE ACCOUNT" : "SIGN IN";
            if (_primaryLabel != null) _primaryLabel.text = creating ? "CREATE ACCOUNT" : "SIGN IN";
            _error.text = creating
                ? "Keeps everything you have played on this machine."
                : "";
            _error.color = creating ? UiTheme.CreamMuted : UiTheme.Danger;

            SetTab(_signInTab, !creating);
            SetTab(_createTab, creating);
        }

        private static void SetTab(Button button, bool on)
        {
            if (button == null) return;

            var skin = button.GetComponent<GodotButton>();
            if (skin == null) return;

            skin.Variation = on ? "WoodAmberButton" : "WoodButton";
            skin.Apply();
            skin.Refresh();
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
                _error.color = UiTheme.CreamMuted;
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
            _error.color = UiTheme.Danger;
            _error.text = message ?? "";
        }
    }
}
