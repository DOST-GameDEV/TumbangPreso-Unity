using System.Linq;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// What her login DOES: the mode switch, the live field state, and the four
    /// small verbs the screen owns.
    ///
    /// ⚠️⚠️ ONE SET OF RULES, READ BY TWO CALLERS. `OwnerFieldFault` is the only
    /// place that decides whether a username or a password is usable, and both
    /// the per-frame watcher and `Submit` ask it. The previous screen checked
    /// the confirmation in one place and the length nowhere, so a password the
    /// form accepted could still be refused by the service with UGS's own
    /// wording, which 🧑 met as **"create acct doesnt work"** with an error
    /// message this game never wrote (`PlayerAccount.UpgradeAsync`'s header).
    ///
    /// ⚠️⚠️ THE RULES ARE UGS'S, NOT THIS SCREEN'S GUESS. Username is 3 to 20 of
    /// letters, digits and `.-_@`; password is 8 to 30 with an upper, a lower, a
    /// digit and a symbol. Checking only the length she wrote on the mock would
    /// have shipped a form that says a password is fine and then fails on it,
    /// which is the exact failure above in a new costume.
    /// </summary>
    public sealed partial class SignInScreen
    {
        private void ChooseMode(bool creating)
        {
            if (_creating == creating) return;
            MenuSfx.Toggle();
            SetMode(creating);
        }

        private void SetNativeMode(bool creating)
        {
            _creating = creating;
            var theme = OwnerUiTheme.Current;

            OwnerUiSlide.Move(_ownerPill,
                new Vector2(creating ? OwnerLoginLayout.Get("login3-tabs-pill").x : PillSignInX,
                            _ownerPill.anchoredPosition.y), !_ownerModePlaced);
            _createTab.GetComponentInChildren<Text>().color = creating ? theme.Green : theme.IdleTabInk;
            _signInTab.GetComponentInChildren<Text>().color = creating ? theme.IdleTabInk : theme.Green;

            // ⚠️ THE CAPTION ON THE ONE GREEN PLATE IS THE ONLY WORD THAT CHANGES
            // WITH THE MODE. She drew CREATE on the sign-up sheet and LOG IN on the
            // sign-in one, on the same plate at two heights, so this is a caption
            // and a move rather than two buttons.
            _primaryLabel.text = creating ? "CREATE" : "LOG IN";
            var seat = creating ? PrimarySignUp : PrimarySignIn;
            OwnerUiSlide.Move((RectTransform)_nativeSubmit.transform, new Vector2(seat.x, -seat.y), !_ownerModePlaced);

            _ownerConfirm.gameObject.SetActive(creating);
            _ownerTermsRow.SetActive(creating);
            _ownerForgotRow.SetActive(!creating);
            _faultConfirm.gameObject.SetActive(creating);
            _guest.gameObject.SetActive(creating);
            _googleButton.gameObject.SetActive(!creating && Net.GoogleSignIn.IsAvailable);

            var divider = creating ? DividerSignUp : DividerSignIn;
            OwnerUiSlide.Move(_ownerDivider, new Vector2(divider.x, -divider.y), !_ownerModePlaced);
            // ⚠️ THE DIVIDER GOES WITH THE SECOND ROUTE, NOT WITH THE MODE. On
            // sign-in it separates LOG IN from Google; on sign-up it separates
            // CREATE from GUEST. With Google unavailable in a build that has no
            // client id (`docs/TODO.md` § 115.8) the sign-in side has no second
            // route at all, and an OR with nothing after it is a broken sentence.
            _ownerDivider.gameObject.SetActive(creating || Net.GoogleSignIn.IsAvailable);

            var status = creating ? StatusSignUp : StatusSignIn;
            OwnerUiLayout.Place(_error.rectTransform, status.x, status.y, status.width, status.height);
            OwnerUiLayout.Place(_nativeContext.rectTransform, status.x, status.y, status.width, status.height);

            _password.contentType = InputField.ContentType.Password;
            _password.ForceLabelUpdate();
            _ownerConfirm.contentType = InputField.ContentType.Password;
            _ownerConfirm.ForceLabelUpdate();
            RefreshReveal();
            if (!creating) _ownerConfirm.text = "";

            // ⚠️ HER SIGN-IN SHEET CALLS THE FIRST FIELD TUMP ID AND THE SIGN-UP
            // ONE USERNAME, and they are the same field: one is what you are
            // choosing, the other is what you already chose.
            ((Text)_username.placeholder).text = creating ? "USERNAME" : "TUMP ID";

            _ownerModePlaced = true;
            ClearOwnerFaults();
            _error.text = "";
            _error.color = theme.HintInk;
            _nativeContext.text = "";

            Chain(_createTab, _signInTab, _username, _password, _ownerReveal,
                creating ? _ownerConfirm : null, creating ? _ownerConfirmReveal : null,
                creating ? (Selectable)_ownerTerms : _ownerForgot, _nativeSubmit,
                creating ? _guest : _googleButton, _back);
            _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }

        // -------------------------------------------------------------------
        // Live field state.

        /// <summary>
        /// What is wrong with one value, or null when nothing is.
        ///
        /// ⚠️ IT ANSWERS null FOR AN EMPTY FIELD ON PURPOSE. A form that marks
        /// every field red before the player has typed anything is a form that
        /// tells them they have already failed. `Submit` has its own empty check
        /// and says "Enter a username." there, where it is an answer to a press.
        /// </summary>
        private static string OwnerFieldFault(string value, bool password, string confirmAgainst = null)
        {
            value ??= "";
            if (value.Length == 0) return null;
            if (confirmAgainst != null)
                return value == confirmAgainst ? null : "Passwords do not match.";

            // ⚠⚠ THE RULES THEMSELVES MOVED TO `AccountRules` AND THE SENTENCES CAME WITH THEM.
            // CHANGE PASSWORD is a third screen that has to refuse exactly what the service
            // refuses, and this form is no longer the only reader. What stayed here is the line
            // above and the one below it, which are this SCREEN's policy rather than the rule:
            // an empty field is not a fault yet, and a confirmation is about the pair.
            return password ? AccountRules.PasswordFault(value) : AccountRules.UsernameFault(value);
        }

        /// <summary>
        /// Reads the three fields every frame and says what it can see.
        ///
        /// ⚠️⚠️ THE SERVER'S VERDICT OUTRANKS THE LOCAL ONE UNTIL THE FIELD IS
        /// EDITED. "Username already taken." is a fact only the service knows,
        /// and a local rule that says the same username is fine would erase it on
        /// the very next keystroke, so the player would watch their own error
        /// disappear without having fixed anything. It is cleared by a change to
        /// the value it was about, which is the only event that can make it stale.
        ///
        /// ⚠️ SIGN-IN CARRIES NO LOCAL RULES AT ALL. A password that predates a
        /// rule change is still that account's password, and a screen that
        /// refuses to let somebody type it has locked them out of their own game
        /// over a message it invented.
        /// </summary>
        private void WatchOwnerFields()
        {
            if (_faultUser == null) return;

            string user = _username.text ?? "";
            string pass = _password.text ?? "";
            string confirm = _ownerConfirm.text ?? "";

            if (_serverUserFault != null && user != _serverUserValue) _serverUserFault = null;
            if (_serverPassFault != null && pass != _serverPassValue) _serverPassFault = null;

            string userFault = _serverUserFault ?? (_creating ? OwnerFieldFault(user, false) : null);
            string passFault = _serverPassFault ?? (_creating ? OwnerFieldFault(pass, true) : null);
            string confirmFault = _creating ? OwnerFieldFault(confirm, true, pass) : null;

            _faultUser.text = userFault ?? "";
            _faultPass.text = passFault ?? "";
            _faultConfirm.text = confirmFault ?? "";

            bool userGood = user.Length > 0 && userFault == null;
            bool passGood = pass.Length > 0 && passFault == null;
            bool confirmGood = _creating && confirm.Length > 0 && confirmFault == null;

            _markUser.Show(user.Length == 0 ? OwnerFieldMark.State.None
                : userFault != null ? OwnerFieldMark.State.Refused : OwnerFieldMark.State.Accepted);

            // ⚠️ THE CHIME IS THE EDGE, NOT THE STATE. A field is valid on every
            // keystroke after the one that fixed it, so playing on the state would
            // play the sound on every letter of a long password.
            if ((userGood && !_userWasGood) || (passGood && !_passWasGood) || (confirmGood && !_confirmWasGood))
                MenuSfx.Valid();
            _userWasGood = userGood; _passWasGood = passGood; _confirmWasGood = confirmGood;
        }

        private string _serverUserValue = "", _serverPassValue = "";

        private void ClearOwnerFaults()
        {
            _serverUserFault = _serverPassFault = null;
            if (_faultUser != null) { _faultUser.text = ""; _faultPass.text = ""; _faultConfirm.text = ""; }
            if (_markUser != null) _markUser.Show(OwnerFieldMark.State.None);
            _userWasGood = _passWasGood = _confirmWasGood = false;
        }

        /// <summary>
        /// Puts a failed attempt under the field it is about, and answers whether
        /// it found one.
        ///
        /// ⚠️⚠️ SIGN-IN GETS ONE LINE AND NOT TWO, EVEN THOUGH SHE DREW TWO.
        /// Her sign-in sheet has "ID invalid." under the first field and
        /// "Password invalid." under the second, and the service does not tell
        /// anybody which of the two was wrong: `SignInWithUsernamePasswordAsync`
        /// refuses the pair. Showing both would be two claims where there is one
        /// fact, and showing either alone would be a guess the player then acts
        /// on. The sentence names the pair.
        /// </summary>
        private bool RouteOwnerFault(string message)
        {
            if (!_nativeForm || _faultUser == null || string.IsNullOrEmpty(message)) return false;
            string lower = message.ToLowerInvariant();

            if (!_creating && (lower.Contains("invalid") || lower.Contains("wrong")
                               || lower.Contains("credential") || lower.Contains("not found")))
            {
                _serverUserValue = _username.text ?? "";
                _serverUserFault = "TUMP ID or password invalid.";
                _faultUser.text = _serverUserFault;
                MenuSfx.Error();
                return true;
            }

            bool aboutUser = lower.Contains("username") || lower.Contains("taken")
                             || lower.Contains("already exists") || lower.Contains("in use");
            if (aboutUser)
            {
                _serverUserValue = _username.text ?? "";
                _serverUserFault = lower.Contains("taken") || lower.Contains("exists") || lower.Contains("in use")
                    ? "Username already taken." : message;
                _faultUser.text = _serverUserFault;
                MenuSfx.Error();
                return true;
            }

            if (lower.Contains("password"))
            {
                _serverPassValue = _password.text ?? "";
                _serverPassFault = message;
                _faultPass.text = message;
                MenuSfx.Error();
                return true;
            }

            return false;
        }

        // -------------------------------------------------------------------
        // The small verbs.

        private void ToggleOwnerPassword() => ToggleOwnerField(_password);

        private void ToggleOwnerField(InputField field)
        {
            field.contentType = field.contentType == InputField.ContentType.Password
                ? InputField.ContentType.Standard : InputField.ContentType.Password;
            field.ForceLabelUpdate();
            RefreshReveal();
        }

        /// <summary>
        /// ⚠️ THE EYE SHOWS THE STATE, WHICH IS THE OPPOSITE OF THE VERB. She drew
        /// an open eye on the empty field and a struck one on the filled, masked
        /// one, so the mark says "this is hidden" rather than "press to hide".
        /// </summary>
        private void RefreshReveal()
        {
            if (_revealPassArt == null) return;
            _revealPassArt.sprite = OwnerMenuArt.Piece(
                _password.contentType == InputField.ContentType.Password ? "login3-eye-open" : "login3-eye-shut");
            _revealConfirmArt.sprite = OwnerMenuArt.Piece(
                _ownerConfirm.contentType == InputField.ContentType.Password ? "login3-eye-open" : "login3-eye-shut");
        }

        /// <summary>
        /// ⚠️⚠️ THERE IS NO PASSWORD RESET TO SEND ANYBODY TO, AND SAYING SO IS
        /// THE HONEST VERSION OF THIS CONTROL. UGS's username-password provider
        /// has no recovery flow this game has built, so the link opens nothing.
        /// 🧑 asked for her sheet to work; a link that silently does nothing is
        /// § 6.3's dead end, and one that pretends to send an email is worse.
        /// The line names the one route that does exist. `docs/TODO.md` § 150.
        /// </summary>
        /// <summary>
        /// ⚠⚠ THIS LINK OPENS NOTHING AND THAT IS THE HONEST ANSWER, NOT AN UNFINISHED ONE.
        /// UGS's username-password provider holds no address, so there is no reset mail to send,
        /// and the Admin API this client can reach has no reset either. A link that pretends to
        /// send one is worse than a dead end, and a dead end is § 6.3's bug, so it names the one
        /// route that exists instead.
        ///
        /// ⚠ AND THE ROUTE DEPENDS ON WHETHER THIS BUILD HAS GOOGLE. With a client id
        /// (`docs/TODO.md` § 115.8) the button is already on this screen and a player who
        /// connected it has a second door. Without one there is nothing to offer but a new TUMP
        /// ID, and saying so beats implying they missed a step.
        /// </summary>
        private void ForgotPressed()
        {
            _error.color = OwnerUiTheme.Current.HintInk;
            _error.text = Net.GoogleSignIn.IsAvailable
                ? "There is no password reset. If you connected Google, sign in with it below, "
                  + "then set a new one in ACCOUNT."
                : "There is no password reset. Play as a guest or make a new TUMP ID.";
        }

        private void ShowOwnerTerms()
        {
            if (_ownerTermsView != null && _ownerTermsView.IsOpen) return;
            _ownerTermsView = OwnerTermsView.Open(transform, accepted =>
            {
                _ownerTermsClosedFrame = Time.frameCount;
                if (accepted) _ownerTerms.isOn = true;
                _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
                if (UnityEngine.EventSystems.EventSystem.current != null)
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(_ownerTerms.gameObject);
            });
        }

        private bool ValidateOwnerRegistration()
        {
            if (!_creating) return true;
            string fault = OwnerFieldFault(_username.text, false);
            if (fault != null) { _faultUser.text = fault; _username.Select(); MenuSfx.Error(); return false; }
            fault = OwnerFieldFault(_password.text, true);
            if (fault != null) { _faultPass.text = fault; _password.Select(); MenuSfx.Error(); return false; }
            fault = OwnerFieldFault(_ownerConfirm.text, true, _password.text);
            if (fault != null || string.IsNullOrEmpty(_ownerConfirm.text))
            {
                _faultConfirm.text = fault ?? "Passwords do not match.";
                _ownerConfirm.Select(); MenuSfx.Error(); return false;
            }
            if (!_ownerTerms.isOn)
            {
                Fail("Read and accept the play and account guidelines to create an account.");
                return false;
            }
            return true;
        }

        private void NativeBusy(bool busy)
        {
            if (!_nativeForm) return;
            _nativeBusy = busy;
            foreach (var control in new Selectable[]
            {
                _nativeSubmit, _googleButton, _createTab, _signInTab, _username, _ownerConfirm, _password,
                _ownerReveal, _ownerConfirmReveal, _ownerTerms, _ownerTermsLink, _ownerForgot, _guest, _back
            }) if (control != null) control.interactable = !busy;
            _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }

        private void EnsureOwnerAccountReturn()
        {
            // Only an explicitly opened profile-management subpage has a return
            // destination. Startup login does not even construct this control.
            if (_back != null) return;
            _back = OwnerTextButton(_ownerDesign, "SignInBack", "BACK", Close, LinkSize, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place((RectTransform)_back.transform, 54, 40, 154, 64);
        }

        private void BuildOwnerWelcome(RectTransform design)
        {
            _welcome = OwnerUiLayout.Rect(design, "Welcome").gameObject;
            OwnerUiLayout.Fill((RectTransform)_welcome.transform);
            var heading = OwnerUiLayout.Text(_welcome.transform, "WelcomeTitle", "WELCOME BACK", 54,
                OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(heading.rectTransform, 650, 391, 620, 80);
            heading.alignment = TextAnchor.MiddleCenter;
            heading.color = OwnerUiTheme.Current.ActionInk;
            _welcomeName = OwnerUiLayout.Text(_welcome.transform, "WelcomeName", "", 42);
            OwnerUiLayout.Place(_welcomeName.rectTransform, 650, 493, 620, 80);
            _welcomeName.alignment = TextAnchor.MiddleCenter;
            _welcomeHint = OwnerUiLayout.Text(_welcome.transform, "WelcomeHint", "", FaultSize,
                OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_welcomeHint.rectTransform, 650, 582, 620, 70);
            _welcomeHint.alignment = TextAnchor.MiddleCenter;
            var go = OwnerPaintedAction.Create(_welcome.transform, "ContinueAccount", "CONTINUE", BootGuest,
                false, ActionSize, OwnerMenuArt.Piece("login3-primary"));
            OwnerUiLayout.Place((RectTransform)go.transform, PrimarySignUp.x, 699, PrimarySignUp.width, PrimarySignUp.height);
            var caption = go.GetComponentInChildren<Text>();
            caption.color = OwnerUiTheme.Current.ActionInk;
            caption.alignment = TextAnchor.MiddleCenter;
            OwnerUiLayout.Place(caption.rectTransform, CaptionCentreX - 200, CaptionCentreY - 34, 400, 68);
            var change = OwnerTextButton(_welcome.transform, "OtherAccount", "USE ANOTHER ACCOUNT",
                LeaveWelcomeForTheForm, LinkSize, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place((RectTransform)change.transform, 704, 818, 513, 70);
            _welcome.SetActive(false);
        }
    }
}
