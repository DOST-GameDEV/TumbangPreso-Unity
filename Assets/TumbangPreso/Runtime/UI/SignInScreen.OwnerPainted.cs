using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class SignInScreen
    {
        private InputField _ownerConfirm;
        private Toggle _ownerTerms;
        private Button _ownerReveal, _ownerConfirmReveal, _ownerTermsLink;
        private OwnerTermsView _ownerTermsView;
        private int _ownerTermsClosedFrame=-1;
        private OwnerAccountTabArt _ownerTabArt;
        private RectTransform _ownerForm;
        private RectTransform _ownerDesign;
        private RectTransform _ownerDivider;
        private GameObject _ownerTermsRow;
        private bool _ownerModePlaced;

        private void BuildNativeSignIn()
        {
            _nativeForm=true;var theme=OwnerUiTheme.Current;
            OwnerUiBackdrop.Build(_root.transform,OwnerMenuArt.Texture("login-background"),false);
            var design=_ownerDesign=OwnerUiLayout.DesignArea(_root.transform,"OwnerAccountComposition");
            var logo=OwnerMenuArt.Image(design,"OriginalOwnerLogo","login2-logo");OwnerLoginLayout.Place(logo.transform,"login2-logo");
            _ownerForm=OwnerUiLayout.Rect(design,"AccountForm");OwnerUiLayout.Fill(_ownerForm);
            var track=OwnerUiLayout.Rect(_ownerForm,"AccountTabArtwork");OwnerUiLayout.Fill(track);
            _ownerTabArt=track.gameObject.AddComponent<OwnerAccountTabArt>();
            _ownerTabArt.Left=OwnerMenuArt.Image(track,"SelectedLeft","login2-tabs-left");OwnerLoginLayout.Place(_ownerTabArt.Left.transform,"login2-tabs-left");
            _ownerTabArt.Right=OwnerMenuArt.Image(track,"SelectedRight","login2-tabs-right");OwnerLoginLayout.Place(_ownerTabArt.Right.transform,"login2-tabs-right");
            _createTab=OwnerTextButton(_ownerForm,"CreateAccountTab","SIGN UP",()=>SetMode(true),28,OwnerUiLayout.TypeRole.Accent);
            _signInTab=OwnerTextButton(_ownerForm,"SignInTab","SIGN IN",()=>SetMode(false),28,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place((RectTransform)_createTab.transform,773,346,180,58);
            OwnerUiLayout.Place((RectTransform)_signInTab.transform,950,346,180,58);

            _username=OwnerUiEntry.Create(_ownerForm,"Username","USERNAME",OwnerUiTheme.Piece.FirstField,artwork:OwnerMenuArt.Piece("login2-user-up"),embeddedGlyph:true);
            _password=OwnerUiEntry.Create(_ownerForm,"Password","ENTER PASSWORD",OwnerUiTheme.Piece.SecondField,password:true,artwork:OwnerMenuArt.Piece("login2-pass-up"),embeddedGlyph:true);
            _ownerConfirm=OwnerUiEntry.Create(_ownerForm,"ConfirmPassword","CONFIRM PASSWORD",OwnerUiTheme.Piece.ThirdField,password:true,artwork:OwnerMenuArt.Piece("login2-confirm"),embeddedGlyph:true);
            _username.characterLimit=64;
            OwnerLoginLayout.Place(_username.transform,"login2-user-up");OwnerLoginLayout.Place(_password.transform,"login2-pass-up");
            OwnerLoginLayout.Place(_ownerConfirm.transform,"login2-confirm");
            _username.onSubmit.AddListener(_=>Submit());_password.onSubmit.AddListener(_=>Submit());_ownerConfirm.onSubmit.AddListener(_=>Submit());
            // The eye is part of the supplied field art. Its transparent hit area
            // toggles visibility without redrawing or replacing the owner's icon.
            _ownerReveal=OwnerTextButton(_password.transform,"RevealPassword","",ToggleOwnerPassword,24);
            OwnerUiLayout.Place((RectTransform)_ownerReveal.transform,9,10,57,55);
            _ownerConfirmReveal=OwnerTextButton(_ownerConfirm.transform,"RevealConfirmation","",()=>ToggleOwnerField(_ownerConfirm),24);
            OwnerUiLayout.Place((RectTransform)_ownerConfirmReveal.transform,9,10,57,55);

            _ownerTermsRow=OwnerUiLayout.Rect(_ownerForm,"TermsRow").gameObject;
            OwnerUiLayout.Place((RectTransform)_ownerTermsRow.transform,663,723,555,42);
            var hit=OwnerUiLayout.Rect(_ownerTermsRow.transform,"TermsAcceptance");OwnerUiLayout.Place(hit,0,0,44,42);
            var hitImage=hit.gameObject.AddComponent<Image>();hitImage.color=Color.clear;
            _ownerTerms=hit.gameObject.AddComponent<Toggle>();_ownerTerms.targetGraphic=hitImage;_ownerTerms.transition=Selectable.Transition.None;
            var box=OwnerMenuArt.Image(hit,"OriginalCheckbox","login2-check");OwnerUiLayout.Place(box.rectTransform,12,8,22,23);
            var tick=OwnerUiGlyph.Create(hit,"Check",OwnerUiGlyph.Mark.Check,theme.ActionInk);
            OwnerUiLayout.Place(tick.rectTransform,13,9,20,22);_ownerTerms.graphic=tick;_ownerTerms.isOn=false;
            _ownerTermsLink=OwnerTextButton(_ownerTermsRow.transform,"TermsLink","TERMS & CONDITIONS",ShowOwnerTerms,28,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place((RectTransform)_ownerTermsLink.transform,39,0,361,42);
            _ownerTermsLink.GetComponentInChildren<Text>().alignment=TextAnchor.MiddleLeft;

            _nativeSubmit=OwnerPaintedAction.Create(_ownerForm,"SubmitAccount","CREATE",Submit,false,64,OwnerMenuArt.Piece("login2-primary-up"));
            PlaceOwnerAction(_nativeSubmit,"login2-primary-up");_primaryLabel=_nativeSubmit.GetComponentInChildren<Text>();
            _ownerDivider=OwnerUiLayout.Rect(_ownerForm,"AccountDivider");OwnerUiLayout.Place(_ownerDivider,668,892.5f,562,46);
            var left=OwnerUiLayout.Rect(_ownerDivider,"LeftDivider").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(left.rectTransform,0,21.5f,243,3);left.color=theme.ActionInk;left.raycastTarget=false;
            var right=OwnerUiLayout.Rect(_ownerDivider,"RightDivider").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(right.rectTransform,319,21.5f,243,3);right.color=theme.ActionInk;right.raycastTarget=false;
            var or=OwnerUiLayout.Text(_ownerDivider,"Or","OR",28,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(or.rectTransform,243,0,76,46);or.alignment=TextAnchor.MiddleCenter;or.color=theme.EnteredInk;
            _guest=OwnerPaintedAction.Create(_ownerForm,"GuestAccount","GUEST",GuestPressed,true,64,OwnerMenuArt.Piece("login2-guest"));
            PlaceOwnerAction(_guest,"login2-guest");
            _error=OwnerUiLayout.Text(_ownerForm,"AccountStatus","",28,OwnerUiLayout.TypeRole.Display);
            _error.color=theme.HintInk;OwnerUiLayout.Place(_error.rectTransform,1242,453,430,170);
            _nativeContext=OwnerUiLayout.Text(_ownerForm,"AccountContext","",28);_nativeContext.color=theme.EnteredInk;
            OwnerUiLayout.Place(_nativeContext.rectTransform,1242,634,430,150);
            _googleButton=OwnerTextButton(_ownerForm,"GoogleAccount","Continue with Google",GooglePressed,28,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place((RectTransform)_googleButton.transform,1240,800,380,56);_googleButton.gameObject.SetActive(Net.GoogleSignIn.IsAvailable);
            _formPieces=new[]{_ownerForm.gameObject};BuildOwnerWelcome(design);
        }

        private static void PlaceOwnerAction(Button button,string name)
        {
            OwnerLoginLayout.Place(button.transform,name);
            button.transform.Find("PaintedArtwork").GetComponent<Image>().sprite=OwnerMenuArt.Piece(name);
            var art=OwnerLoginLayout.Get(name);CenterOwnerCaption(button,art.Size,art.Face);
        }

        private static void CenterOwnerCaption(Button button,Vector2 sourceSize,Vector2 faceCentre)
        {
            // Center text on the painted front face, not on the extrusion or
            // transparent padding. The supplied image remains uniformly fitted.
            var size=((RectTransform)button.transform).sizeDelta;
            float scale=Mathf.Min(size.x/sourceSize.x,size.y/sourceSize.y);
            var centre=(size-sourceSize*scale)*.5f+faceCentre*scale;
            var label=button.GetComponentInChildren<Text>().rectTransform;
            label.anchorMin=label.anchorMax=label.pivot=new Vector2(.5f,.5f);
            label.sizeDelta=new Vector2(size.x-28,120);
            label.anchoredPosition=new Vector2(centre.x-size.x*.5f,size.y*.5f-centre.y);
        }

        private static Button OwnerTextButton(Transform parent,string name,string words,UnityEngine.Events.UnityAction action,int size,
            OwnerUiLayout.TypeRole role=OwnerUiLayout.TypeRole.Reading)
        {
            var rect=OwnerUiLayout.Rect(parent,name);var hit=rect.gameObject.AddComponent<UnityEngine.UI.Image>();hit.color=Color.clear;
            var button=rect.gameObject.AddComponent<OwnerTextAction>();button.targetGraphic=hit;button.transition=Selectable.Transition.None;
            var label=OwnerUiLayout.Text(rect,"Label",words,size,role);OwnerUiLayout.Fill(label.rectTransform);
            label.alignment=TextAnchor.MiddleCenter;label.color=OwnerUiTheme.Current.ActionInk;label.gameObject.AddComponent<OwnerUiMotion>();
            if(action!=null)button.onClick.AddListener(()=>{MenuSfx.Click();action();});return button;
        }
        private void BuildOwnerWelcome(RectTransform design)
        {
            _welcome=OwnerUiLayout.Rect(design,"Welcome").gameObject;OwnerUiLayout.Fill((RectTransform)_welcome.transform);
            var heading=OwnerUiLayout.Text(_welcome.transform,"WelcomeTitle","Welcome back",50,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(heading.rectTransform,650,391,620,80);heading.alignment=TextAnchor.MiddleCenter;
            _welcomeName=OwnerUiLayout.Text(_welcome.transform,"WelcomeName","",42);
            OwnerUiLayout.Place(_welcomeName.rectTransform,650,493,620,80);_welcomeName.alignment=TextAnchor.MiddleCenter;
            _welcomeHint=OwnerUiLayout.Text(_welcome.transform,"WelcomeHint","",28);
            OwnerUiLayout.Place(_welcomeHint.rectTransform,650,582,620,70);_welcomeHint.alignment=TextAnchor.MiddleCenter;
            var go=OwnerPaintedAction.Create(_welcome.transform,"ContinueAccount","CONTINUE",BootGuest,false,52,OwnerMenuArt.Piece("login-primary"));
            OwnerUiLayout.Place((RectTransform)go.transform,755,699,413,91);
            CenterOwnerCaption(go,new Vector2(431,96),new Vector2(215,37.5f));
            var change=OwnerTextButton(_welcome.transform,"OtherAccount","Use another account",LeaveWelcomeForTheForm,28,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place((RectTransform)change.transform,704,818,513,70);_welcome.SetActive(false);
        }
        private void SetNativeMode(bool creating)
        {
            _creating=creating;_ownerTabArt.SelectLeft(creating);
            _createTab.GetComponentInChildren<Text>().color=creating?OwnerUiTheme.Current.Green:OwnerUiTheme.Current.Pale;
            _signInTab.GetComponentInChildren<Text>().color=creating?OwnerUiTheme.Current.Pale:OwnerUiTheme.Current.Green;
            _primaryLabel.text=creating?"CREATE":"SIGN IN";
            _ownerConfirm.gameObject.SetActive(creating);_ownerTermsRow.SetActive(creating);
            _ownerDivider.gameObject.SetActive(creating);_guest.gameObject.SetActive(creating);
            string user=creating?"login2-user-up":"login2-user-in",pass=creating?"login2-pass-up":"login2-pass-in";
            OwnerLoginLayout.Place(_username.transform,user);_username.GetComponent<Image>().sprite=OwnerMenuArt.Piece(user);
            OwnerLoginLayout.Place(_password.transform,pass);_password.GetComponent<Image>().sprite=OwnerMenuArt.Piece(pass);
            PlaceOwnerAction(_nativeSubmit,creating?"login2-primary-up":"login2-primary-in");
            _password.contentType=InputField.ContentType.Password;_password.ForceLabelUpdate();
            _ownerConfirm.contentType=InputField.ContentType.Password;_ownerConfirm.ForceLabelUpdate();
            if(!creating)_ownerConfirm.text="";
            _ownerModePlaced=true;_error.text="";_error.color=OwnerUiTheme.Current.HintInk;_nativeContext.text="";
            Chain(_createTab,_signInTab,_username,_password,_ownerReveal,creating?_ownerConfirm:null,
                creating?_ownerConfirmReveal:null,creating?_ownerTerms:null,_nativeSubmit,creating?_guest:null,_googleButton,_back);
            _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }

        private void EnsureOwnerAccountReturn()
        {
            // Only an explicitly opened profile-management subpage has a return
            // destination. Startup login does not even construct this control.
            if(_back!=null)return;
            _back=OwnerTextButton(_ownerDesign,"SignInBack","BACK",Close,30,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place((RectTransform)_back.transform,54,40,154,64);
        }
        private void MoveOwnerRow(RectTransform rect,float y)
            =>OwnerUiSlide.Move(rect,new Vector2(rect.anchoredPosition.x,-y),!_ownerModePlaced);
        private void ToggleOwnerPassword()=>ToggleOwnerField(_password);
        private static void ToggleOwnerField(InputField field)
        {
            field.contentType=field.contentType==InputField.ContentType.Password?InputField.ContentType.Standard:InputField.ContentType.Password;
            field.ForceLabelUpdate();
        }
        private void ShowOwnerTerms()
        {
            if(_ownerTermsView!=null && _ownerTermsView.IsOpen)return;
            _ownerTermsView=OwnerTermsView.Open(transform,accepted=>
            {
                _ownerTermsClosedFrame=Time.frameCount;
                if(accepted)_ownerTerms.isOn=true;
                _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
                if(UnityEngine.EventSystems.EventSystem.current!=null)
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(_ownerTerms.gameObject);
            });
        }
        private bool ValidateOwnerRegistration()
        {
            if(!_creating)return true;
            if(_ownerConfirm.text!=_password.text)
            {
                Fail("Passwords do not match.");_ownerConfirm.Select();return false;
            }
            if(!_ownerTerms.isOn){Fail("Read and accept the play and account guidelines to create an account.");return false;}
            return true;
        }
        private void NativeBusy(bool busy)
        {
            if(!_nativeForm)return;_nativeBusy=busy;
            foreach(var control in new Selectable[]{_nativeSubmit,_googleButton,_createTab,_signInTab,_username,_ownerConfirm,_password,
                _ownerReveal,_ownerConfirmReveal,_ownerTerms,_ownerTermsLink,_guest,_back})if(control!=null)control.interactable=!busy;
            _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }
    }
}
