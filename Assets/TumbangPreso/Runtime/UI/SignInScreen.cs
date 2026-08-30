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
        /// <summary>⚠️ 38 per cent, measured off the reference. Narrower and the fields squeeze;
        /// wider and the art stops being the thing you notice first.</summary>
        private const float ColumnFraction = 0.38f;

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
        /// ⚠️⚠️ THE ART SIDE IS THE LIVE SCENE, NOT A TEXTURE, AND THAT IS DELIBERATE RATHER THAN
        /// LAZY. PUBG's login sits on a full-bleed key-art frame and Valorant's on a splash
        /// render; this game already has the street, the cast and the lighting running behind the
        /// menu, and a scrim over it is the same picture without shipping a 4 MB PNG that goes
        /// stale the first time the art changes. The scrim is heavy enough that white text on it
        /// is legible at every point of the animated backdrop.
        /// </summary>
        private void BuildScrim()
        {
            var scrim = MenuKit.Backdrop(_root.transform, new Color(0.0f, 0.0f, 0.0f, 0.72f));
            scrim.gameObject.name = "Scrim";
        }

        private void BuildColumn()
        {
            var columnGo = new GameObject("Column", typeof(RectTransform), typeof(Image));
            columnGo.transform.SetParent(_root.transform, false);

            var rt = (RectTransform)columnGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(ColumnFraction, 1.0f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            columnGo.GetComponent<Image>().color = UiTheme.WoodDeep;

            // ⚠️ THE COLUMN IS OPAQUE AND THE REST IS NOT. That contrast is what makes the form
            // read as the only thing you can act on, which is the whole point of the reference.
            var skin = columnGo.AddComponent<GodotPanel>();
            skin.Variation = "WoodPanel";
            skin.ApplyContentMargins = false;
            skin.Apply();

            var col = columnGo.transform;

            MenuKit.Label(col, "TUMP", 30, UiTheme.Amber, new Vector2(0.5f, 1.0f),
                new Vector2(0.0f, -70.0f), new Vector2(360.0f, 44.0f));

            _heading = MenuKit.Label(col, "SIGN IN", 40, UiTheme.Cream, new Vector2(0.5f, 1.0f),
                new Vector2(0.0f, -150.0f), new Vector2(420.0f, 54.0f));

            BuildTabs(col);

            _username = Field(col, "USERNAME", -350.0f, "your username", 64, false);
            _password = Field(col, "PASSWORD", -470.0f, "your password", 128, true);

            // ⚠️ THE ERROR SITS UNDER THE FIELDS RATHER THAN IN A SHARED STATUS LINE AT THE TOP.
            // The old panel had one `_status` label that reported saving a profile, linking a
            // username, signing in and arming a delete, so the sentence on screen was whichever
            // of six unrelated actions ran last.
            _error = MenuKit.Label(col, "", MenuKit.MinReadableUnits, UiTheme.Danger,
                new Vector2(0.5f, 1.0f), new Vector2(0.0f, -546.0f), new Vector2(420.0f, 56.0f));
            _error.horizontalOverflow = HorizontalWrapMode.Wrap;

            BuildPrimary(col);

            // ⚠️⚠️ THE GUEST BUTTON DOES TWO DIFFERENT THINGS AND THE LABEL SAYS WHICH.
            // Reached from the ACCOUNT tab it is the TOURNAMENT guest, which parks the owner's
            // profile and hands the machine to somebody else for one session. Reached at BOOT it
            // means "keep the anonymous account this machine already has and let me play", which
            // is the opposite: nothing is parked and nothing is temporary. **Two behaviours
            // behind one word is exactly the confusion this file was rebuilt to remove**, so the
            // caption changes with the mode and `BootGuest` and `Guest` are separate methods.
            _guest = MenuKit.WoodButton(col, "PLAY AS GUEST", new Vector2(0.5f, 0.0f),
                new Vector2(0.0f, 132.0f), new Vector2(300.0f, 48.0f), GuestPressed);

            _back = MenuKit.WoodButton(col, "BACK", new Vector2(0.5f, 0.0f),
                new Vector2(0.0f, 74.0f), new Vector2(300.0f, 48.0f), Close);
        }

        /// <summary>
        /// ⚠️ A SEGMENTED PAIR, NOT TWO BUTTONS. Sign in and create account are the same two
        /// fields and the same submit; making them two separate wood buttons, as the old panel
        /// did with SIGN IN and LINK USERNAME sitting beside SAVE PROFILE, asks the player to
        /// know the difference before they have typed anything. A segment says "one of these two
        /// modes is on" and the primary underneath does whichever it is.
        /// </summary>
        private void BuildTabs(Transform col)
        {
            _signInTab = MenuKit.WoodButton(col, "SIGN IN", new Vector2(0.5f, 1.0f),
                new Vector2(-108.0f, -230.0f), new Vector2(206.0f, 48.0f), () => SetMode(false),
                "WoodAmberButton");

            _createTab = MenuKit.WoodButton(col, "CREATE", new Vector2(0.5f, 1.0f),
                new Vector2(108.0f, -230.0f), new Vector2(206.0f, 48.0f), () => SetMode(true));
        }

        private void BuildPrimary(Transform col)
        {
            var button = MenuKit.WoodButton(col, "SIGN IN", new Vector2(0.5f, 1.0f),
                new Vector2(0.0f, -600.0f), new Vector2(420.0f, 62.0f), Submit,
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
            MenuKit.Label(col, caption, MenuKit.MinReadableUnits, UiTheme.CreamMuted,
                new Vector2(0.5f, 1.0f), new Vector2(-146.0f, y + 46.0f), new Vector2(320.0f, 26.0f),
                TextAnchor.MiddleLeft);

            var go = new GameObject($"Field_{caption}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(col, false);
            MenuKit.Place((RectTransform)go.transform, new Vector2(0.5f, 1.0f),
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
