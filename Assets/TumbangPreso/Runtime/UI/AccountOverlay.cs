using System;
using TumbangPreso.Core;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Account management on the title screen. First launch never opens this panel. It appears
    /// automatically only after PlayerAccount marks the first score as worth keeping, and is
    /// always available from the ACCOUNT button afterwards.
    /// </summary>
    public sealed class AccountOverlay : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _panel;
        private Text _status;
        private Text _handle;
        private InputField _displayName, _bio, _country, _pronouns, _username, _password;
        private bool _deleteArmed;

        public void Install()
        {
            if (_canvas != null) return;

            _canvas = MenuKit.BuildCanvas(transform, "AccountCanvas");
            _canvas.sortingOrder = 80;
            MenuKit.WoodButton(_canvas.transform, "ACCOUNT", new Vector2(1, 1),
                new Vector2(-118, -42), new Vector2(190, 54), Open);

            _panel = new GameObject("AccountPanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(_canvas.transform, false);
            var rt = (RectTransform)_panel.transform;
            MenuKit.Place(rt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 870));
            _panel.GetComponent<Image>().color = UiTheme.WoodDeep;

            MenuKit.Label(_panel.transform, "PLAYER ACCOUNT", 34, UiTheme.Cream,
                new Vector2(0.5f, 1), new Vector2(0, -42), new Vector2(720, 60));
            _handle = MenuKit.Label(_panel.transform, "", 26, UiTheme.Amber,
                new Vector2(0.5f, 1), new Vector2(0, -96), new Vector2(720, 46));
            _status = MenuKit.Label(_panel.transform, "", 18, UiTheme.CreamMuted,
                new Vector2(0.5f, 1), new Vector2(0, -136), new Vector2(720, 42));

            _displayName = Field("DISPLAY NAME", -196, AccountRules.DisplayNameMax);
            _bio = Field("BIO", -286, AccountRules.BioMax);
            _country = Field("COUNTRY CODE (OPTIONAL)", -376, AccountRules.CountryCodeLength);
            _pronouns = Field("PRONOUNS (OPTIONAL)", -466, AccountRules.PronounsMax);
            _username = Field("USERNAME", -566, 64);
            _password = Field("PASSWORD", -656, 128, password: true);

            MenuKit.WoodButton(_panel.transform, "SAVE PROFILE", new Vector2(0.5f, 0),
                new Vector2(-245, 150), new Vector2(230, 54), SaveProfile);
            MenuKit.WoodButton(_panel.transform, "LINK USERNAME", new Vector2(0.5f, 0),
                new Vector2(0, 150), new Vector2(230, 54), LinkUsername);
            MenuKit.WoodButton(_panel.transform, "SIGN IN", new Vector2(0.5f, 0),
                new Vector2(245, 150), new Vector2(230, 54), SignIn);
            MenuKit.WoodButton(_panel.transform, "DELETE ACCOUNT", new Vector2(0.5f, 0),
                new Vector2(-260, 82), new Vector2(240, 50), DeleteAccount, "DangerButton");
            MenuKit.WoodButton(_panel.transform, "PLAY AS GUEST", new Vector2(0.5f, 0),
                new Vector2(0, 82), new Vector2(240, 50), ToggleGuest);
            MenuKit.WoodButton(_panel.transform, "CLOSE", new Vector2(0.5f, 0),
                new Vector2(260, 82), new Vector2(210, 50), Close);

            _panel.SetActive(false);
            var account = GameServices.Account;
            if (account != null)
            {
                account.Changed += Refresh;
                if (account.ShouldOfferUpgrade) OpenOffer();
            }
        }

        private InputField Field(string caption, float y, int limit, bool password = false)
        {
            MenuKit.Label(_panel.transform, caption, 18, UiTheme.CreamMuted,
                new Vector2(0.5f, 1), new Vector2(-260, y + 31), new Vector2(220, 34),
                TextAnchor.MiddleLeft);

            var go = new GameObject(caption.Replace(' ', '_'), typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_panel.transform, false);
            var image = go.GetComponent<Image>();
            image.color = UiTheme.Card;
            MenuKit.Place(image.rectTransform, new Vector2(0.5f, 1), new Vector2(110, y), new Vector2(500, 58));

            var input = go.AddComponent<InputField>();
            input.targetGraphic = image;
            input.characterLimit = limit;
            input.lineType = InputField.LineType.SingleLine;
            if (password) input.contentType = InputField.ContentType.Password;

            var text = MenuKit.Label(go.transform, "", 22, UiTheme.Ink,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460, 48), TextAnchor.MiddleLeft);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            input.textComponent = text;

            var placeholder = MenuKit.Label(go.transform, "", 20, UiTheme.InkMuted,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460, 48), TextAnchor.MiddleLeft);
            placeholder.text = password ? "password" : caption.ToLowerInvariant();
            input.placeholder = placeholder;
            return input;
        }

        private void OpenOffer()
        {
            Open();
            SetStatus("You earned something worth keeping. Link a username and password so this account survives another device.");
            GameServices.Account?.MarkUpgradeOfferShown();
        }

        private void Open()
        {
            _panel.SetActive(true);
            Refresh();
        }

        private void Close()
        {
            _deleteArmed = false;
            _panel.SetActive(false);
        }

        private void Refresh()
        {
            var a = GameServices.Account;
            if (a == null || _panel == null) return;
            _handle.text = a.LobbyName;
            _status.text = a.IsLocalOnly ? $"LOCAL PROFILE  ·  {a.Status}" : $"SIGNED IN  ·  {a.PlayerId}";
            _displayName.text = a.DisplayName;
            _bio.text = a.Bio;
            _country.text = a.Country;
            _pronouns.text = a.Pronouns;
            _username.text = a.Username;
            _password.text = "";
        }

        private async void SaveProfile()
        {
            try
            {
                SetStatus("Saving profile...");
                await GameServices.Account.SetProfileAsync(_displayName.text, _bio.text, _country.text, _pronouns.text);
            }
            catch (Exception e) { SetStatus(e.Message); }
        }

        private async void LinkUsername()
        {
            try
            {
                SetStatus("Linking without changing your player id...");
                await GameServices.Account.UpgradeAsync(_username.text, _password.text);
                Refresh();
            }
            catch (Exception e) { SetStatus(e.Message); }
        }

        private async void SignIn()
        {
            try
            {
                SetStatus("Signing in...");
                await GameServices.Account.SignInAsync(_username.text, _password.text);
                Refresh();
            }
            catch (Exception e) { SetStatus(e.Message); }
        }

        private async void DeleteAccount()
        {
            if (!_deleteArmed)
            {
                _deleteArmed = true;
                SetStatus("Press DELETE ACCOUNT again to permanently delete this account.");
                return;
            }

            try
            {
                SetStatus("Deleting account...");
                await GameServices.Account.DeleteAsync();
                _deleteArmed = false;
                Refresh();
            }
            catch (Exception e) { SetStatus(e.Message); }
        }

        private void ToggleGuest()
        {
            try
            {
                if (GameServices.Account.IsGuest)
                    GameServices.Account.LeaveGuest();
                else
                    GameServices.Account.SignInAsGuest(_displayName.text);
                Refresh();
            }
            catch (Exception e) { SetStatus(e.Message); }
        }

        private void SetStatus(string value)
        {
            if (_status != null) _status.text = value ?? "";
        }

        private void OnDestroy()
        {
            if (GameServices.Account != null) GameServices.Account.Changed -= Refresh;
        }
    }
}
