using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class SignInScreen
    {
        private bool _nativeForm;
        private bool _nativeBusy;
        private Button _nativeSubmit;
        private Text _nativeContext;
        private void BuildPreviousNativeSignIn()
        {
            _nativeForm = true;
            var f = TumpUiTheme.Current;
            var root = (RectTransform)_root.transform;
            TumpUiFactory.Ground(root, f.Cream);
            var scene = TumpUiFactory.Rect(root, "StreetIllustration").gameObject.AddComponent<RawImage>();
            scene.rectTransform.anchorMin = new Vector2(.46f, 0); scene.rectTransform.anchorMax = Vector2.one;
            scene.rectTransform.offsetMin = scene.rectTransform.offsetMax = Vector2.zero;
            scene.gameObject.AddComponent<TumpBackdrop>().AlignCropRight = true;
            var paper = TumpUiFactory.Rect(root, "SignInPaper").gameObject.AddComponent<TumpPaperEdge>();
            paper.color = f.Cream; paper.raycastTarget = false;
            paper.rectTransform.anchorMin = paper.rectTransform.anchorMax = new Vector2(0, .5f);
            paper.rectTransform.pivot = new Vector2(0, .5f);
            paper.rectTransform.anchoredPosition = new Vector2(24, 0); paper.rectTransform.sizeDelta = new Vector2(898, 1044);
            var logo = TumpUiFactory.Art(paper.transform, "OriginalLogo", TumpUiFactory.Sprite("UI/brand/tump_logo"));
            TumpUiFactory.Place(logo.rectTransform, 180, 34, 480, 250);
            var pieces = new List<GameObject>();
            _signInTab = TumpUiFactory.Button(paper.transform, "SignInTab", "Sign in", () => SetMode(false), TumpSurface.Form.Pebble, f.Apricot, 34);
            _createTab = TumpUiFactory.Button(paper.transform, "CreateAccountTab", "Create account", () => SetMode(true), TumpSurface.Form.Pebble, f.Apricot, 34);
            TumpUiFactory.Place((RectTransform)_signInTab.transform, 100, 276, 300, 92);
            TumpUiFactory.Place((RectTransform)_createTab.transform, 430, 276, 362, 92);
            pieces.Add(_signInTab.gameObject); pieces.Add(_createTab.gameObject);
            _nativeContext = TumpUiFactory.Text(paper.transform, "AccountContext", "", 24);
            TumpUiFactory.Place(_nativeContext.rectTransform, 102, 368, 690, 64); pieces.Add(_nativeContext.gameObject);
            var userLabel = TumpUiFactory.Text(paper.transform, "UsernameLabel", "Username", 32, true);
            TumpUiFactory.Place(userLabel.rectTransform, 102, 430, 670, 56); pieces.Add(userLabel.gameObject);
            _username = TumpUiFactory.Field(paper.transform, "Username", "Username");
            TumpUiFactory.Place((RectTransform)_username.transform, 100, 486, 692, 88); pieces.Add(_username.gameObject);
            var passLabel = TumpUiFactory.Text(paper.transform, "PasswordLabel", "Password", 32, true);
            TumpUiFactory.Place(passLabel.rectTransform, 102, 586, 670, 56); pieces.Add(passLabel.gameObject);
            _password = TumpUiFactory.Field(paper.transform, "Password", "Password", password: true);
            TumpUiFactory.Place((RectTransform)_password.transform, 100, 642, 692, 88); pieces.Add(_password.gameObject);
            _password.onSubmit.AddListener(_ => Submit());
            _error = TumpUiFactory.Text(paper.transform, "AccountStatus", "", 26);
            _error.color = f.Brick; TumpUiFactory.Place(_error.rectTransform, 104, 738, 684, 74); pieces.Add(_error.gameObject);
            var primary = TumpUiFactory.Button(paper.transform, "SubmitAccount", "Sign in", Submit, TumpSurface.Form.Slap, f.Lime, 42);
            _nativeSubmit = primary;
            TumpUiFactory.Place((RectTransform)primary.transform, 164, 810, 560, 104); pieces.Add(primary.gameObject);
            _primaryLabel = primary.GetComponentInChildren<Text>();
            _googleButton = TumpUiFactory.Button(paper.transform, "GoogleAccount", "Continue with Google", GooglePressed, TumpSurface.Form.Link, f.Cream, 30);
            TumpUiFactory.Place((RectTransform)_googleButton.transform, 146, 922, 610, 76); pieces.Add(_googleButton.gameObject);
            _guest = TumpUiFactory.Button(root, "GuestAccount", "Continue as guest", GuestPressed, TumpSurface.Form.Pebble, f.Cream, 34);
            TumpUiFactory.Anchor((RectTransform)_guest.transform, new Vector2(1, 0), new Vector2(-342, 88), new Vector2(556, 104));
            _back = TumpUiFactory.BackButton(root, "SignInBack", Close);
            TumpUiFactory.Place((RectTransform)_back.transform, 36, 16, 162, 76);
            _formPieces = pieces.ToArray();
            _welcome = TumpUiFactory.Rect(paper.transform, "Welcome").gameObject;
            TumpUiFactory.Stretch((RectTransform)_welcome.transform);
            var hello = TumpUiFactory.Text(_welcome.transform, "WelcomeTitle", "Welcome back", 52, true);
            TumpUiFactory.Place(hello.rectTransform, 104, 378, 680, 90);
            _welcomeName = TumpUiFactory.Text(_welcome.transform, "WelcomeName", "", 44, true);
            TumpUiFactory.Place(_welcomeName.rectTransform, 104, 480, 680, 90);
            _welcomeHint = TumpUiFactory.Text(_welcome.transform, "WelcomeHint", "", 28);
            TumpUiFactory.Place(_welcomeHint.rectTransform, 104, 586, 680, 80);
            var continueButton = TumpUiFactory.Button(_welcome.transform, "ContinueAccount", "Continue", BootGuest, TumpSurface.Form.Slap, f.Lime, 44);
            TumpUiFactory.Place((RectTransform)continueButton.transform, 166, 694, 560, 106);
            var other = TumpUiFactory.Button(_welcome.transform, "OtherAccount", "Use another account", LeaveWelcomeForTheForm, TumpSurface.Form.Link, f.Cream, 30);
            TumpUiFactory.Place((RectTransform)other.transform, 136, 826, 616, 82);
            _welcome.SetActive(false);
        }
        private void SetPreviousNativeMode(bool creating)
        {
            _creating = creating;
            _nativeContext.text = creating ? "Keep this device's progress in a new account." : "Continue with an existing account.";
            _primaryLabel.text = creating ? "Create account" : "Sign in";
            _googleButton.GetComponentInChildren<Text>().text = creating ? "Connect Google account" : "Sign in with Google";
            _error.text = ""; _error.color = TumpUiTheme.Current.Brick;
            _signInTab.GetComponent<TumpSurface>().Selected = !creating;
            _createTab.GetComponent<TumpSurface>().Selected = creating;
            _signInTab.GetComponent<TumpSurface>().SetVerticesDirty(); _createTab.GetComponent<TumpSurface>().SetVerticesDirty();
            _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }
        private void PreviousNativeBusy(bool busy)
        {
            if (!_nativeForm) return;
            _nativeBusy = busy;
            foreach (var selectable in new Selectable[] { _nativeSubmit, _googleButton, _signInTab, _createTab, _username, _password, _guest })
                if (selectable != null) selectable.interactable = !busy;
        }
    }
}
