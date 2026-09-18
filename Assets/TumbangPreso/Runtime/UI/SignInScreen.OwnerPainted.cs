using System;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Her login, redrawn 2026-09-18 and rebuilt against the new sheet.
    ///
    /// ⚠️⚠️ THE SHAPE OF THE SCREEN CHANGED, NOT JUST ITS SKIN. The September 15
    /// version put one message line in an empty column to the RIGHT of the form,
    /// 1242 units across, because that is where there was room. She redrew it
    /// with a red line under each field, which is the convention every player
    /// already owns and which says WHICH field is wrong without them guessing.
    /// So `_error` is no longer the screen's only voice: three per-field lines
    /// carry everything a field can say about itself, and `_error` keeps the
    /// things that are about the whole attempt (the service is down, the
    /// network went away, "Creating your account...").
    ///
    /// ⚠️⚠️ VALIDATION IS LIVE AND IT IS NOT A SUBMIT GATE. `WatchOwnerFields`
    /// runs every frame and reports what it can see; `Submit` still checks the
    /// same rules before it calls anything, because a screen that only refuses
    /// at the end is the screen she replaced. The two use one set of rules
    /// (`OwnerFieldFault`) so they cannot disagree about whether a password is
    /// long enough.
    ///
    /// ⚠️ THE TAB IS A TRACK AND A PILL NOW, not two baked plates. She drew one
    /// plate with the pill on the left and its mirror with the pill on the
    /// right, which is a slide drawn as two frames; `tools/extract_login_v3.py`
    /// separates them so it can actually slide. `OwnerUiSlide` is the same
    /// easing the rest of the front end uses, and reduced motion still gets it
    /// instantly.
    ///
    /// ⚠️ SIZES AND COLOURS ARE MEASURED OFF HER OWN INK, not chosen. Paalalabas
    /// at 28 puts a 20 unit cap height on the field captions, which is what she
    /// drew; at 34 it puts 24 on the two links, which is what she drew. The inks
    /// all resolved to constants `OwnerUiTheme` already carried: #bc8749 is
    /// `Ochre`, #c81721 is `HintInk`, #901219 is `ActionInk`, #0f5913 is `Green`.
    /// The one that moved is `IdleTabInk`, from #a12e34 to the measured #b12f36.
    /// </summary>
    public sealed partial class SignInScreen
    {
        // ---- her composition, in the 1920x1080 design area ----------------
        // Everything a sprite occupies comes from login-layout-v3.json. These are
        // the things that are not sprites: where her text sits, and the second
        // position each moving piece has.

        private const float FieldTextX = 75, FieldTextWidth = 400, FieldTextHeight = 44;
        private const float FieldMarkX = 480, FieldMarkY = 22;
        private const float CaptionCentreX = 204, CaptionCentreY = 40;
        private const float PillSignInX = 941;

        private static readonly Vector2 TabLeftCentre = new Vector2(860, 346);
        private static readonly Vector2 TabRightCentre = new Vector2(1034, 346);

        private static readonly Rect PrimarySignUp = new Rect(746, 787, 424, 92);
        private static readonly Rect PrimarySignIn = new Rect(746, 703, 424, 92);
        private static readonly Rect GuestSeat = new Rect(746, 930, 422, 92);
        private static readonly Rect GoogleSeat = new Rect(746, 845, 424, 93);

        private static readonly Rect TermsRow = new Rect(676, 738, 460, 40);
        private static readonly Rect ForgotRow = new Rect(683, 631, 420, 40);
        private static readonly Rect StatusSignUp = new Rect(676, 1024, 546, 48);
        private static readonly Rect StatusSignIn = new Rect(676, 948, 546, 48);

        private static readonly Vector2 DividerSignUp = new Vector2(671, 878);
        private static readonly Vector2 DividerSignIn = new Vector2(671, 794);

        // Type, fitted to her ink rather than picked. See the class header.
        private const int CaptionSize = 28, LinkSize = 34, OrSize = 34, FaultSize = 22, ActionSize = 61;

        // ⚠️ THE ONLY TRACKED CAPTIONS ON THE SCREEN. Her tab words measure 107
        // units against Paalalabas's own 96 at the size that matches her cap
        // height, so she spread them by about two units a gap. Everything else on
        // the form came out inside five units of her ink untouched.
        private const float TabTracking = 1.8f;

        private InputField _ownerConfirm;
        private Toggle _ownerTerms;
        private Button _ownerReveal, _ownerConfirmReveal, _ownerTermsLink, _ownerForgot;
        private OwnerTermsView _ownerTermsView;
        private int _ownerTermsClosedFrame = -1;
        private RectTransform _ownerForm, _ownerDesign, _ownerPill, _ownerDivider;
        private GameObject _ownerTermsRow, _ownerForgotRow;
        private Image _ownerGuestArt, _ownerGoogleArt;
        private Text _faultUser, _faultPass, _faultConfirm;
        private OwnerFieldMark _markUser;
        private Image _revealPassArt, _revealConfirmArt;
        private bool _ownerModePlaced;
        private string _serverUserFault, _serverPassFault;
        private bool _userWasGood, _passWasGood, _confirmWasGood;

        private void BuildNativeSignIn()
        {
            _nativeForm = true;
            var theme = OwnerUiTheme.Current;
            OwnerUiBackdrop.Build(_root.transform, OwnerMenuArt.Texture("login-background"), false);
            var design = _ownerDesign = OwnerUiLayout.DesignArea(_root.transform, "OwnerAccountComposition");

            var logo = OwnerMenuArt.Image(design, "OriginalOwnerLogo", "login3-logo");
            OwnerLoginLayout.Place(logo.transform, "login3-logo");
            Enter(logo.gameObject, 0f);

            _ownerForm = OwnerUiLayout.Rect(design, "AccountForm");
            OwnerUiLayout.Fill(_ownerForm);

            // ---- the segmented tab ------------------------------------------
            var track = OwnerMenuArt.Image(_ownerForm, "AccountTabTrack", "login3-tabs-track");
            OwnerLoginLayout.Place(track.transform, "login3-tabs-track");
            var pill = OwnerMenuArt.Image(_ownerForm, "AccountTabPill", "login3-tabs-pill");
            OwnerLoginLayout.Place(pill.transform, "login3-tabs-pill");
            _ownerPill = pill.rectTransform;
            _createTab = OwnerTextButton(_ownerForm, "CreateAccountTab", "SIGN UP",
                () => ChooseMode(true), CaptionSize, OwnerUiLayout.TypeRole.Accent);
            _signInTab = OwnerTextButton(_ownerForm, "SignInTab", "SIGN IN",
                () => ChooseMode(false), CaptionSize, OwnerUiLayout.TypeRole.Accent);
            PlaceCentred((RectTransform)_createTab.transform, TabLeftCentre, new Vector2(184, 58));
            PlaceCentred((RectTransform)_signInTab.transform, TabRightCentre, new Vector2(184, 58));
            OwnerUiTracking.Apply(_createTab.GetComponentInChildren<Text>(), TabTracking);
            OwnerUiTracking.Apply(_signInTab.GetComponentInChildren<Text>(), TabTracking);
            // ⚠️ THE PILL GETS NO ENTRY MOTION AND THAT IS NOT AN OVERSIGHT.
            // `OwnerUiMotion` rewrites `anchoredPosition` from the rest position it
            // captured, every LateUpdate; `OwnerUiSlide` writes the same field to
            // move the pill between halves. Both on one rect and the slide is
            // silently undone. The same rule keeps the primary plate and the
            // divider clear of it below.
            Enter(track.gameObject, .05f);

            // ---- the three fields -------------------------------------------
            _username = Field("Username", "USERNAME", "login3-field-user", false);
            _password = Field("Password", "PASSWORD", "login3-field-pass", true);
            _ownerConfirm = Field("ConfirmPassword", "CONFIRM PASSWORD", "login3-field-confirm", true);
            _username.characterLimit = 64;
            _username.onSubmit.AddListener(_ => Submit());
            _password.onSubmit.AddListener(_ => Submit());
            _ownerConfirm.onSubmit.AddListener(_ => Submit());
            Enter(_username.gameObject, .10f); Enter(_password.gameObject, .15f);
            Enter(_ownerConfirm.gameObject, .20f);

            // ⚠️ THE MARK SHARES THE EYE'S BOX SO THE RIGHT EDGE OF A FIELD NEVER
            // SHIFTS. Her invalid disc is 29x30 and the eyes are 45x34; both are
            // drawn into the same 45x34 seat, centred, so toggling either one
            // moves nothing.
            _markUser = OwnerFieldMark.Create(_username.transform, "UsernameMark");
            // ⚠️ THE TWO EYE BUTTONS KEEP THEIR OLD NAMES. Three PlayMode fixtures
            // press RevealPassword and RevealConfirmation by name, and the control
            // they are pressing is the same control; a rename here would have been
            // a rename dressed up as a redesign.
            _revealPassArt = Reveal(_password, "RevealPassword", out _ownerReveal, ToggleOwnerPassword);
            _revealConfirmArt = Reveal(_ownerConfirm, "RevealConfirmation", out _ownerConfirmReveal,
                () => ToggleOwnerField(_ownerConfirm));

            _faultUser = Fault("UsernameFault", 489);
            _faultPass = Fault("PasswordFault", 596);
            _faultConfirm = Fault("ConfirmFault", 700);

            // ---- terms, sign-up only ----------------------------------------
            _ownerTermsRow = OwnerUiLayout.Rect(_ownerForm, "TermsRow").gameObject;
            OwnerUiLayout.Place((RectTransform)_ownerTermsRow.transform, TermsRow.x, TermsRow.y, TermsRow.width, TermsRow.height);
            var hit = OwnerUiLayout.Rect(_ownerTermsRow.transform, "TermsAcceptance");
            OwnerUiLayout.Place(hit, 0, 0, 44, 40);
            var hitImage = hit.gameObject.AddComponent<Image>();
            hitImage.color = Color.clear;
            _ownerTerms = hit.gameObject.AddComponent<Toggle>();
            _ownerTerms.targetGraphic = hitImage;
            _ownerTerms.transition = Selectable.Transition.None;
            var box = OwnerMenuArt.Image(hit, "OriginalCheckbox", "login3-checkbox");
            OwnerUiLayout.Place(box.rectTransform, 0, 7, 21, 24);
            var tick = OwnerUiGlyph.Create(hit, "Check", OwnerUiGlyph.Mark.Check, theme.ActionInk);
            OwnerUiLayout.Place(tick.rectTransform, 1, 8, 19, 22);
            _ownerTerms.graphic = tick;
            _ownerTerms.isOn = false;
            _ownerTerms.onValueChanged.AddListener(_ => MenuSfx.Tick());
            _ownerTermsLink = OwnerTextButton(_ownerTermsRow.transform, "TermsLink", "TERMS & CONDITIONS",
                ShowOwnerTerms, LinkSize, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place((RectTransform)_ownerTermsLink.transform, 38, 0, 400, 40);
            var termsLabel = _ownerTermsLink.GetComponentInChildren<Text>();
            termsLabel.alignment = TextAnchor.MiddleLeft;
            OwnerUiRule.Under(termsLabel);
            Enter(_ownerTermsRow, .25f);

            // ---- forgot password, sign-in only ------------------------------
            _ownerForgotRow = OwnerUiLayout.Rect(_ownerForm, "ForgotRow").gameObject;
            OwnerUiLayout.Place((RectTransform)_ownerForgotRow.transform, ForgotRow.x, ForgotRow.y, ForgotRow.width, ForgotRow.height);
            var key = OwnerMenuArt.Image(_ownerForgotRow.transform, "OriginalKey", "login3-key");
            OwnerUiLayout.Place(key.rectTransform, 0, 5, 31, 28);
            _ownerForgot = OwnerTextButton(_ownerForgotRow.transform, "ForgotPassword", "FORGOT PASSWORD?",
                ForgotPressed, LinkSize, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place((RectTransform)_ownerForgot.transform, 43, 0, 360, 40);
            var forgotLabel = _ownerForgot.GetComponentInChildren<Text>();
            forgotLabel.alignment = TextAnchor.MiddleLeft;
            OwnerUiRule.Under(forgotLabel);
            Enter(_ownerForgotRow, .25f);

            // ---- the actions -------------------------------------------------
            _nativeSubmit = PaintedAction("SubmitAccount", "CREATE", "login3-primary", PrimarySignUp, Submit, theme.ActionInk);
            _primaryLabel = _nativeSubmit.GetComponentInChildren<Text>();
            _guest = PaintedAction("GuestAccount", "GUEST", "login3-guest", GuestSeat, GuestPressed, theme.GuestInk);
            _ownerGuestArt = _guest.transform.Find("PaintedArtwork").GetComponent<Image>();
            _googleButton = PaintedAction("GoogleAccount", "", "login3-google", GoogleSeat, GooglePressed, theme.ActionInk);
            _ownerGoogleArt = _googleButton.transform.Find("PaintedArtwork").GetComponent<Image>();
            // ⚠️ ON THE ARTWORK, NOT THE BUTTON. `OwnerPaintedAction` deliberately
            // animates the painted child inside a stationary hit box, and the
            // primary's own rect is what the mode switch slides.
            EnterArt(_nativeSubmit, .30f);
            EnterArt(_guest, .38f);
            EnterArt(_googleButton, .38f);

            // ---- the OR divider ---------------------------------------------
            _ownerDivider = OwnerUiLayout.Rect(_ownerForm, "AccountDivider");
            OwnerUiLayout.Place(_ownerDivider, DividerSignUp.x, DividerSignUp.y, 562, 40);
            var left = OwnerMenuArt.Image(_ownerDivider, "LeftDivider", "login3-rule-left");
            OwnerUiLayout.Place(left.rectTransform, 0, 18, 241, 5);
            var right = OwnerMenuArt.Image(_ownerDivider, "RightDivider", "login3-rule-right");
            OwnerUiLayout.Place(right.rectTransform, 320, 18, 242, 6);
            var or = OwnerUiLayout.Text(_ownerDivider, "Or", "OR", OrSize, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(or.rectTransform, 249, 0, 64, 40);
            or.alignment = TextAnchor.MiddleCenter;
            or.color = theme.ActionInk;
            Enter(or.gameObject, .34f);

            // ---- the one line that is about the attempt rather than a field ---
            _error = OwnerUiLayout.Text(_ownerForm, "AccountStatus", "", FaultSize, OwnerUiLayout.TypeRole.Display);
            _error.color = theme.HintInk;
            _error.alignment = TextAnchor.MiddleCenter;
            OwnerUiLayout.Place(_error.rectTransform, StatusSignUp.x, StatusSignUp.y, StatusSignUp.width, StatusSignUp.height);
            _nativeContext = OwnerUiLayout.Text(_ownerForm, "AccountContext", "", FaultSize, OwnerUiLayout.TypeRole.Display);
            _nativeContext.color = theme.EnteredInk;
            _nativeContext.alignment = TextAnchor.MiddleCenter;
            OwnerUiLayout.Place(_nativeContext.rectTransform, StatusSignUp.x, StatusSignUp.y, StatusSignUp.width, StatusSignUp.height);

            _formPieces = new[] { _ownerForm.gameObject };
            BuildOwnerWelcome(design);
        }

        // -------------------------------------------------------------------
        // Construction helpers. Each one exists because the thing it builds is
        // built more than once, not to make a universal control.

        private InputField Field(string name, string caption, string piece, bool password)
        {
            var art = OwnerMenuArt.Image(_ownerForm, name, piece);
            art.raycastTarget = true;
            OwnerLoginLayout.Place(art.transform, piece);
            var input = art.gameObject.AddComponent<InputField>();
            input.targetGraphic = art;
            input.transition = Selectable.Transition.None;
            input.lineType = InputField.LineType.SingleLine;
            input.contentType = password ? InputField.ContentType.Password : InputField.ContentType.Standard;
            input.characterLimit = password ? 128 : 254;
            input.caretColor = OwnerUiTheme.Current.ActionInk;
            input.customCaretColor = true;
            input.selectionColor = new Color(.73f, .81f, .27f, .45f);
            var area = OwnerUiLayout.Rect(art.transform, "EditableArea");
            var box = OwnerLoginLayout.Get(piece);
            OwnerUiLayout.Place(area, FieldTextX, (box.height - FieldTextHeight) * .5f, FieldTextWidth, FieldTextHeight);
            // ⚠️ THE ENTERED TEXT IS LYDIAN AND THE CAPTION IS PAALALABAS, which is
            // her drawing rather than a default: a name typed into the field reads
            // as a name, and the caption reads as a label. Lydian at 31 measures
            // 215 units for "Stardust_Slayer84" against the 216 she drew.
            var text = OwnerUiLayout.Text(area, "EnteredText", "", 31);
            OwnerUiLayout.Fill(text.rectTransform);
            text.color = OwnerUiTheme.Current.EnteredInk;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            input.textComponent = text;
            var hint = OwnerUiLayout.Text(area, "Placeholder", caption, CaptionSize, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Fill(hint.rectTransform);
            hint.color = OwnerUiTheme.Current.Ochre;
            input.placeholder = hint;
            return input;
        }

        private Text Fault(string name, float y)
        {
            var text = OwnerUiLayout.Text(_ownerForm, name, "", FaultSize, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(text.rectTransform, 682, y, 540, 30);
            text.alignment = TextAnchor.MiddleLeft;
            text.color = OwnerUiTheme.Current.HintInk;
            text.raycastTarget = false;
            text.gameObject.AddComponent<OwnerFieldFade>();
            return text;
        }

        private Image Reveal(InputField field, string name, out Button button, UnityEngine.Events.UnityAction action)
        {
            var seat = OwnerLoginLayout.Get("login3-eye-open");
            var art = OwnerMenuArt.Image(field.transform, "RevealArt", "login3-eye-open");
            OwnerUiLayout.Place(art.rectTransform, FieldMarkX, FieldMarkY, seat.width, seat.height);
            button = OwnerTextButton(field.transform, name, "", () => { action(); }, 24);
            OwnerUiLayout.Place((RectTransform)button.transform, FieldMarkX - 6, FieldMarkY - 8, seat.width + 12, seat.height + 16);
            art.transform.SetAsLastSibling();
            return art;
        }

        private Button PaintedAction(string name, string words, string piece, Rect seat,
            Action click, Color ink)
        {
            var button = OwnerPaintedAction.Create(_ownerForm, name, words, click, false, ActionSize,
                OwnerMenuArt.Piece(piece));
            OwnerUiLayout.Place((RectTransform)button.transform, seat.x, seat.y, seat.width, seat.height);
            var label = button.GetComponentInChildren<Text>();
            label.color = ink;
            label.alignment = TextAnchor.MiddleCenter;
            // ⚠️ CENTRED ON THE PAINTED FACE, NOT ON THE RECT. Every one of her
            // action plates carries a darker extrusion along its bottom edge, so
            // the middle of the rect is below the middle of the surface a word
            // sits on. Measured off her own captions on all three plates, the
            // face centre is (204, 40) into a 424x92 seat, to within two units.
            OwnerUiLayout.Place(label.rectTransform, CaptionCentreX - 200, CaptionCentreY - 34, 400, 68);
            return button;
        }

        private static void PlaceCentred(RectTransform rect, Vector2 centre, Vector2 size)
            => OwnerUiLayout.Place(rect, centre.x - size.x * .5f, centre.y - size.y * .5f, size.x, size.y);

        private static void EnterArt(Button button, float delay)
        {
            var art = button.transform.Find("PaintedArtwork");
            Enter(art != null ? art.gameObject : button.gameObject, delay);
        }

        private static void Enter(GameObject piece, float delay)
        {
            var motion = piece.GetComponent<OwnerUiMotion>();
            if (motion == null) motion = piece.AddComponent<OwnerUiMotion>();
            motion.EntryDelay = delay;
        }

        private static Button OwnerTextButton(Transform parent, string name, string words,
            UnityEngine.Events.UnityAction action, int size,
            OwnerUiLayout.TypeRole role = OwnerUiLayout.TypeRole.Reading)
        {
            var rect = OwnerUiLayout.Rect(parent, name);
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var button = rect.gameObject.AddComponent<OwnerTextAction>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            var label = OwnerUiLayout.Text(rect, "Label", words, size, role);
            OwnerUiLayout.Fill(label.rectTransform);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = OwnerUiTheme.Current.ActionInk;
            label.gameObject.AddComponent<OwnerUiMotion>();
            if (action != null) button.onClick.AddListener(() => { MenuSfx.Click(); action(); });
            return button;
        }
    }
}
