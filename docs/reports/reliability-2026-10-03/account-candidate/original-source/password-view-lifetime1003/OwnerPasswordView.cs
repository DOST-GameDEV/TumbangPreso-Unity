using System;
using System.Threading.Tasks;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// CHANGE PASSWORD: the only screen in this game that can change one.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE UGS CANNOT SEND A RESET AND NEVER WILL. The
    /// username-password provider holds no address, so there is no mail to send,
    /// and the Admin API this client could reach has no reset either. A player
    /// who forgets their password and has no second identity attached has lost
    /// the account. **Everything this game can do about that happens before the
    /// day they forget**: change it while they still know it, which is here, and
    /// attach Google so there is a second door, which is CONNECT on the hub's
    /// account tab. `docs/TODO.md` § 153.15.
    ///
    /// ⚠️⚠️ IT IS A SHEET OF ITS OWN RATHER THAN A THIRD MODE ON HER LOGIN.
    /// `SignInScreen.SetNativeMode` is a bool and her two sheets are two halves
    /// of one drawing: the pill slides between them, the primary plate moves
    /// between two heights she drew, and the divider follows the second route.
    /// A third state threaded through that would have been a rewrite of the one
    /// screen § 153 just finished measuring against her art, to add a screen she
    /// did not draw. This borrows her pieces instead — her field plates, her key,
    /// her eye, her green plate — so it belongs to the same login without editing
    /// it.
    ///
    /// ⚠️ THE CURRENT PASSWORD IS NEVER CHECKED HERE. Only the service can say
    /// whether it is right, and a local check would need the old password stored
    /// somewhere it must never be. What this screen refuses locally is the NEW
    /// one, by `AccountRules`, in this game's wording rather than UGS's.
    /// </summary>
    public sealed class OwnerPasswordView : MonoBehaviour
    {
        private const int Caption = 28, Fault = 22, Title = 46;
        private static readonly Rect Sheet = new Rect(450, 150, 1020, 780);

        private Canvas _canvas;
        private InputField _current, _next, _confirm;
        private Text _currentFault, _nextFault, _confirmFault, _status;
        private Button _save;
        private Action _closed;
        private bool _busy;

        public bool IsOpen => _canvas != null && _canvas.gameObject.activeSelf;

        public static OwnerPasswordView Open(Transform owner, Action closed = null)
        {
            var node = new GameObject("OwnerPasswordView");
            node.transform.SetParent(owner, false);
            var view = node.AddComponent<OwnerPasswordView>();
            view.Build(closed);
            return view;
        }

        private void Build(Action closed)
        {
            _closed = closed;
            _canvas = OwnerUiLayout.Canvas(transform, "OwnerPasswordCanvas", 960);
            ScreenTakeover.Register(this, () => IsOpen);

            var scrim = OwnerUiLayout.Rect(_canvas.transform, "ModalBlocker").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(scrim.rectTransform); scrim.color = new Color(0, 0, 0, .24f); scrim.raycastTarget = true;

            var design = OwnerUiLayout.DesignArea(_canvas.transform, "PasswordComposition");
            var paper = OwnerUiLayout.Rect(design, "PasswordSheet").gameObject.AddComponent<OwnerUiPaper>();
            paper.Style = OwnerUiPaper.Treatment.Dialog;
            OwnerUiLayout.Place(paper.rectTransform, Sheet.x, Sheet.y, Sheet.width, Sheet.height);
            paper.raycastTarget = true;

            var title = OwnerUiLayout.Text(design, "PasswordTitle", "CHANGE PASSWORD", Title, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform, 505, 196, 910, 70);
            title.alignment = TextAnchor.MiddleCenter; title.color = OwnerUiTheme.Current.ActionInk;

            // ⚠️ HER OWN SENTENCE ABOUT WHAT THIS IS FOR, not a subtitle. A player
            // opening this screen is either being careful or has been told to be,
            // and the second reading is the one that needs the line.
            var note = OwnerUiLayout.Text(design, "PasswordNote",
                "There is no password reset, so keep this one somewhere safe.", Caption);
            OwnerUiLayout.Place(note.rectTransform, 505, 262, 910, 40);
            note.alignment = TextAnchor.MiddleCenter; note.color = OwnerUiTheme.Current.EnteredInk;

            _current = PasswordField(design, "CurrentPassword", "CURRENT PASSWORD", 330, "login3-key");
            _next = PasswordField(design, "NewPassword", "NEW PASSWORD", 452, null);
            _confirm = PasswordField(design, "ConfirmNewPassword", "CONFIRM NEW PASSWORD", 574, null);

            _currentFault = FaultLine(design, "CurrentPasswordFault", 404);
            _nextFault = FaultLine(design, "NewPasswordFault", 526);
            _confirmFault = FaultLine(design, "ConfirmNewPasswordFault", 648);

            _status = OwnerUiLayout.Text(design, "PasswordStatus", "", Caption);
            OwnerUiLayout.Place(_status.rectTransform, 505, 690, 910, 44);
            _status.alignment = TextAnchor.MiddleCenter; _status.color = OwnerUiTheme.Current.HintInk;

            _save = OwnerPaintedAction.Create(design, "SavePassword", "SAVE", Save, false, 48);
            OwnerUiLayout.Place((RectTransform)_save.transform, 755, 742, 413, 91);

            var back = OwnerUiLayout.Rect(design, "PasswordBack");
            OwnerUiLayout.Place(back, 600, 852, 720, 46);
            var hit = back.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var button = back.gameObject.AddComponent<Button>();
            button.targetGraphic = hit; button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => Close());
            var label = OwnerUiLayout.Text(back, "Label", "CANCEL", Caption, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Fill(label.rectTransform); label.alignment = TextAnchor.MiddleCenter;

            _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }

        /// <summary>
        /// One of her password plates, with her key on the first and her eye on all three.
        ///
        /// ⚠️ THE CURRENT FIELD GETS THE KEY AND NOT THE LOCK. She drew `login3-key` for FORGOT
        /// PASSWORD, which is the same idea in the same sheet: the lock is the thing you are
        /// setting, the key is the one you already have.
        /// </summary>
        private InputField PasswordField(Transform parent, string name, string caption, float y, string icon)
        {
            var art = OwnerMenuArt.Image(parent, name, "login3-field-pass");
            art.raycastTarget = true;
            var box = OwnerLoginLayout.Get("login3-field-pass");
            OwnerUiLayout.Place(art.rectTransform, 686, y, box.width, box.height);

            var input = art.gameObject.AddComponent<InputField>();
            input.targetGraphic = art;
            input.transition = Selectable.Transition.None;
            input.lineType = InputField.LineType.SingleLine;
            input.contentType = InputField.ContentType.Password;
            input.characterLimit = 128;
            input.caretColor = OwnerUiTheme.Current.ActionInk;
            input.customCaretColor = true;
            input.selectionColor = new Color(.73f, .81f, .27f, .45f);

            var area = OwnerUiLayout.Rect(art.transform, "EditableArea");
            OwnerUiLayout.Place(area, 74, (box.height - 44) * .5f, 380, 44);
            var text = OwnerUiLayout.Text(area, "Text", "", 31, OwnerUiLayout.TypeRole.Reading);
            OwnerUiLayout.Fill(text.rectTransform); text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.black; text.supportRichText = false;
            var placeholder = OwnerUiLayout.Text(area, "Placeholder", caption, Caption, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Fill(placeholder.rectTransform); placeholder.alignment = TextAnchor.MiddleLeft;
            placeholder.color = OwnerUiTheme.Current.Ochre;
            input.textComponent = text; input.placeholder = placeholder;

            if (icon != null)
            {
                var key = OwnerMenuArt.Image(art.transform, "OriginalKey", icon);
                var keyBox = OwnerLoginLayout.Get(icon);
                OwnerUiLayout.Place(key.rectTransform, 30, (box.height - keyBox.height) * .5f, keyBox.width, keyBox.height);
                key.raycastTarget = false;
            }

            var eye = OwnerMenuArt.Image(art.transform, "Reveal" + name, "login3-eye-shut");
            var eyeBox = OwnerLoginLayout.Get("login3-eye-shut");
            OwnerUiLayout.Place(eye.rectTransform, box.width - eyeBox.width - 22,
                (box.height - eyeBox.height) * .5f, eyeBox.width, eyeBox.height);
            var reveal = eye.gameObject.AddComponent<Button>();
            reveal.targetGraphic = eye; reveal.transition = Selectable.Transition.None;
            reveal.onClick.AddListener(() =>
            {
                bool hidden = input.contentType == InputField.ContentType.Password;
                input.contentType = hidden ? InputField.ContentType.Standard : InputField.ContentType.Password;
                input.ForceLabelUpdate();
                eye.sprite = OwnerMenuArt.Piece(hidden ? "login3-eye-open" : "login3-eye-shut");
                MenuSfx.Tick();
            });

            input.onValueChanged.AddListener(value => Live());
            input.onSubmit.AddListener(value => Save());
            return input;
        }

        private Text FaultLine(Transform parent, string name, float y)
        {
            var line = OwnerUiLayout.Text(parent, name, "", Fault, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(line.rectTransform, 690, y, 540, 30);
            line.alignment = TextAnchor.MiddleLeft;
            line.color = OwnerUiTheme.Current.HintInk;
            return line;
        }

        /// <summary>
        /// ⚠️ EMPTY IS NOT A FAULT YET, the same policy her login uses: a form that marks every
        /// field red before anything has been typed tells the player they have already failed.
        /// </summary>
        private void Live()
        {
            _nextFault.text = _next.text.Length == 0 ? "" : AccountRules.PasswordFault(_next.text) ?? "";
            _confirmFault.text = _confirm.text.Length == 0 || _confirm.text == _next.text
                ? "" : "Passwords do not match.";
            if (_current.text.Length > 0) _currentFault.text = "";
            _save.interactable = !_busy;
        }

        /// <summary>The fire-and-forget the two callers need; `SaveAsync` owns the guard as well
        /// so a second press mid-flight cannot start a second request.</summary>
        private void Save() { _ = SaveAsync(); }

        private async Task SaveAsync()
        {
            if (_busy) return;

            _currentFault.text = _current.text.Length == 0 ? "Enter your current password." : "";
            _nextFault.text = AccountRules.PasswordFault(_next.text) ?? "";
            _confirmFault.text = _confirm.text == _next.text ? "" : "Passwords do not match.";
            if (_currentFault.text.Length + _nextFault.text.Length + _confirmFault.text.Length > 0)
            {
                MenuSfx.Error();
                return;
            }

            _busy = true; _save.interactable = false;
            _status.text = "Changing your password...";
            _status.color = OwnerUiTheme.Current.EnteredInk;
            try
            {
                var account = GameServices.Account;
                if (account == null) throw new InvalidOperationException("The account service is not available here.");
                await account.ChangePasswordAsync(_current.text, _next.text);
                _status.text = "Password changed.";
                _status.color = OwnerUiTheme.Current.Green;
                MenuSfx.Valid();
                Close();
            }
            catch (Exception e)
            {
                // ⚠️⚠️ A WRONG CURRENT PASSWORD IS THE ONLY LIKELY FAILURE AND IT GOES UNDER THAT
                // FIELD. UGS answers a wrong one with its own wording about credentials, which
                // names nothing a player can act on; the field it is about is the whole answer.
                string message = e.Message ?? "";
                string lower = message.ToLowerInvariant();
                if (lower.Contains("invalid") || lower.Contains("wrong") || lower.Contains("credential")
                    || lower.Contains("unauthorized") || lower.Contains("password is incorrect"))
                {
                    _currentFault.text = "That is not your current password.";
                    _status.text = "";
                }
                else
                {
                    _status.text = message;
                    _status.color = OwnerUiTheme.Current.HintInk;
                }
                MenuSfx.Error();
            }
            finally
            {
                _busy = false;
                if (_save != null) _save.interactable = true;
            }
        }

        private void Update()
        {
            if (IsOpen && InputLayer.MenuNav.CancelPressed) { ScreenTakeover.ConsumeEscape(); Close(); }
        }

        private void Close()
        {
            if (!IsOpen) return;
            _canvas.gameObject.SetActive(false);
            ScreenTakeover.ConsumeEscape();
            _closed?.Invoke();
            Destroy(gameObject);
        }

        private void OnDestroy() => ScreenTakeover.Unregister(this);
    }
}
