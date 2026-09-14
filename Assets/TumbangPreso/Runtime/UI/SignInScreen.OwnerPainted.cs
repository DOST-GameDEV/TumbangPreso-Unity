using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class SignInScreen
    {
        private InputField _ownerEmail;
        private Toggle _ownerTerms;
        private Button _ownerReveal, _ownerTermsLink;
        private OwnerTermsView _ownerTermsView;
        private int _ownerTermsClosedFrame=-1;
        private OwnerAccountTabArt _ownerTabArt;
        private Text _ownerAlready;
        private Button _ownerSwitch;
        private RectTransform _ownerForm;
        private RectTransform _ownerDesign;
        private GameObject _ownerTermsRow;
        private bool _ownerModePlaced;

        private void BuildNativeSignIn()
        {
            _nativeForm=true;
            var theme=OwnerUiTheme.Current;
            OwnerUiBackdrop.Build(_root.transform);
            var design=_ownerDesign=OwnerUiLayout.DesignArea(_root.transform,"OwnerAccountComposition");
            var logo=OwnerUiLayout.Art(design,"OriginalOwnerLogo",OwnerUiTheme.Piece.Logo);
            OwnerUiLayout.Place(logo.rectTransform,760,44,407,273);
            logo.gameObject.AddComponent<OwnerUiMotion>().GentleFloat=true;
            _ownerForm=OwnerUiLayout.Rect(design,"AccountForm");OwnerUiLayout.Fill(_ownerForm);

            var track=OwnerUiLayout.Rect(_ownerForm,"AccountTabArtwork");OwnerUiLayout.Place(track,770,337,383,88);
            _ownerTabArt=track.gameObject.AddComponent<OwnerAccountTabArt>();
            _ownerTabArt.Left=OwnerUiLayout.Art(track,"SelectedLeft",OwnerUiTheme.Piece.AccountTabs);
            OwnerUiLayout.Fill(_ownerTabArt.Left.rectTransform);
            _ownerTabArt.Right=OwnerUiLayout.Art(track,"SelectedRight",OwnerUiTheme.Piece.AccountTabs);
            OwnerUiLayout.Fill(_ownerTabArt.Right.rectTransform);_ownerTabArt.Right.rectTransform.localScale=new Vector3(-1,1,1);
            _createTab=OwnerTextButton(_ownerForm,"CreateAccountTab","SIGN UP",()=>SetMode(true),28,OwnerUiLayout.TypeRole.Accent);
            _signInTab=OwnerTextButton(_ownerForm,"SignInTab","SIGN IN",()=>SetMode(false),28,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place((RectTransform)_createTab.transform,790,350,175,55);
            OwnerUiLayout.Place((RectTransform)_signInTab.transform,964,350,170,55);

            _username=OwnerUiEntry.Create(_ownerForm,"Username","USERNAME",OwnerUiTheme.Piece.FirstField);
            _ownerEmail=OwnerUiEntry.Create(_ownerForm,"Email","EMAIL (OPTIONAL)",OwnerUiTheme.Piece.SecondField,OwnerUiGlyph.Mark.Envelope);
            _ownerEmail.contentType=InputField.ContentType.EmailAddress;
            _password=OwnerUiEntry.Create(_ownerForm,"Password","PASSWORD",OwnerUiTheme.Piece.ThirdField,OwnerUiGlyph.Mark.Lock,true);
            _username.characterLimit=64;
            OwnerUiLayout.Place((RectTransform)_username.transform,695,454,533,78);
            OwnerUiLayout.Place((RectTransform)_ownerEmail.transform,695,547,533,77);
            OwnerUiLayout.Place((RectTransform)_password.transform,695,642,533,77);
            _username.onSubmit.AddListener(_=>Submit());_ownerEmail.onSubmit.AddListener(_=>Submit());_password.onSubmit.AddListener(_=>Submit());
            _ownerReveal=OwnerTextButton(_password.transform,"RevealPassword","",ToggleOwnerPassword,24);
            OwnerUiLayout.Place((RectTransform)_ownerReveal.transform,482,16,40,44);
            var eye=OwnerUiGlyph.Create(_ownerReveal.transform,"Eye",OwnerUiGlyph.Mark.Eye,theme.Ochre);
            OwnerUiLayout.Place(eye.rectTransform,7,8,26,26);
            _password.textComponent.rectTransform.offsetMax=new Vector2(-43,0);
            ((RectTransform)_password.placeholder.transform).offsetMax=new Vector2(-43,0);

            _ownerTermsRow=OwnerUiLayout.Rect(_ownerForm,"TermsRow").gameObject;
            OwnerUiLayout.Place((RectTransform)_ownerTermsRow.transform,700,717,535,42);
            var hit=OwnerUiLayout.Rect(_ownerTermsRow.transform,"TermsAcceptance");OwnerUiLayout.Place(hit,0,0,44,42);
            var hitImage=hit.gameObject.AddComponent<UnityEngine.UI.Image>();hitImage.color=Color.clear;
            _ownerTerms=hit.gameObject.AddComponent<Toggle>();_ownerTerms.targetGraphic=hitImage;_ownerTerms.transition=Selectable.Transition.None;
            var box=OwnerUiLayout.Art(hit,"OriginalCheckbox",OwnerUiTheme.Piece.Checkbox);OwnerUiLayout.Place(box.rectTransform,12,8,21,24);
            var tick=OwnerUiGlyph.Create(hit,"Check",OwnerUiGlyph.Mark.Check,theme.ActionInk);
            OwnerUiLayout.Place(tick.rectTransform,13,9,20,22);_ownerTerms.graphic=tick;_ownerTerms.isOn=false;
            var terms=OwnerTextButton(_ownerTermsRow.transform,"TermsLink","TERMS & CONDITIONS",ShowOwnerTerms,26,OwnerUiLayout.TypeRole.Accent);
            _ownerTermsLink=terms;OwnerUiLayout.Place((RectTransform)terms.transform,39,0,361,42);terms.GetComponentInChildren<Text>().alignment=TextAnchor.MiddleLeft;

            _ownerAlready=OwnerUiLayout.Text(_ownerForm,"AlreadyPlayed","Already played?",23,OwnerUiLayout.TypeRole.Display);
            _ownerAlready.color=theme.EnteredInk;OwnerUiLayout.Place(_ownerAlready.rectTransform,714,750,193,35);
            _ownerSwitch=OwnerTextButton(_ownerForm,"OtherAccountMode","SIGN IN",()=>SetMode(!_creating),23,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place((RectTransform)_ownerSwitch.transform,901,750,117,35);
            _ownerSwitch.GetComponentInChildren<Text>().color=theme.HintInk;
            _nativeSubmit=OwnerPaintedAction.Create(_ownerForm,"SubmitAccount","CREATE",Submit,false,64);
            OwnerUiLayout.Place((RectTransform)_nativeSubmit.transform,755,796,413,91);
            _primaryLabel=_nativeSubmit.GetComponentInChildren<Text>();
            var left=OwnerUiLayout.Art(_ownerForm,"LeftDivider",OwnerUiTheme.Piece.LeftRule);
            OwnerUiLayout.Place(left.rectTransform,705,907,242,6);
            var right=OwnerUiLayout.Art(_ownerForm,"RightDivider",OwnerUiTheme.Piece.RightRule);
            OwnerUiLayout.Place(right.rectTransform,1025,909,242,5);
            var or=OwnerUiLayout.Text(_ownerForm,"Or","OR",24,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(or.rectTransform,950,892,70,39);or.alignment=TextAnchor.MiddleCenter;or.color=theme.EnteredInk;
            _guest=OwnerPaintedAction.Create(_ownerForm,"GuestAccount","GUEST",GuestPressed,true,64);
            OwnerUiLayout.Place((RectTransform)_guest.transform,755,935,413,91);

            _error=OwnerUiLayout.Text(_ownerForm,"AccountStatus","",23,OwnerUiLayout.TypeRole.Display);
            _error.color=theme.HintInk;OwnerUiLayout.Place(_error.rectTransform,1238,454,430,140);
            _nativeContext=OwnerUiLayout.Text(_ownerForm,"AccountContext","",22);
            _nativeContext.color=theme.EnteredInk;OwnerUiLayout.Place(_nativeContext.rectTransform,1238,601,430,128);
            _googleButton=OwnerTextButton(_ownerForm,"GoogleAccount","Continue with Google",GooglePressed,26,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place((RectTransform)_googleButton.transform,1240,773,380,56);
            _googleButton.gameObject.SetActive(Net.GoogleSignIn.IsAvailable);
            _formPieces=new[]{_ownerForm.gameObject};
            BuildOwnerWelcome(design);
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
            var go=OwnerPaintedAction.Create(_welcome.transform,"ContinueAccount","CONTINUE",BootGuest,false,52);
            OwnerUiLayout.Place((RectTransform)go.transform,755,699,413,91);
            var change=OwnerTextButton(_welcome.transform,"OtherAccount","Use another account",LeaveWelcomeForTheForm,28,OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place((RectTransform)change.transform,704,818,513,70);_welcome.SetActive(false);
        }
        private void SetNativeMode(bool creating)
        {
            _creating=creating;_ownerTabArt.SelectLeft(creating);
            _createTab.GetComponentInChildren<Text>().color=creating?OwnerUiTheme.Current.Green:OwnerUiTheme.Current.IdleTabInk;
            _signInTab.GetComponentInChildren<Text>().color=creating?OwnerUiTheme.Current.IdleTabInk:OwnerUiTheme.Current.Green;
            _primaryLabel.text=creating?"CREATE":"SIGN IN";
            _ownerEmail.gameObject.SetActive(creating);_ownerTermsRow.SetActive(creating);
            OwnerUiLayout.Place((RectTransform)_password.transform,695,creating?642:547,533,77);
            MoveOwnerRow(_ownerAlready.rectTransform,creating?750:655);
            MoveOwnerRow((RectTransform)_ownerSwitch.transform,creating?750:655);
            MoveOwnerRow((RectTransform)_nativeSubmit.transform,creating?796:701);
            MoveOwnerRow((RectTransform)_ownerForm.Find("LeftDivider"),creating?907:812);
            MoveOwnerRow((RectTransform)_ownerForm.Find("RightDivider"),creating?909:814);
            MoveOwnerRow((RectTransform)_ownerForm.Find("Or"),creating?892:797);
            MoveOwnerRow((RectTransform)_guest.transform,creating?935:840);
            _ownerModePlaced=true;
            _ownerAlready.text=creating?"Already played?":"New player?";
            _ownerSwitch.GetComponentInChildren<Text>().text=creating?"SIGN IN":"SIGN UP";
            _error.text="";_error.color=OwnerUiTheme.Current.HintInk;_nativeContext.text="";
            Chain(_createTab,_signInTab,_username,creating?_ownerEmail:null,_password,_ownerReveal,
                creating?_ownerTerms:null,_nativeSubmit,_guest,_googleButton,_back);
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
        private void ToggleOwnerPassword()
        {
            _password.contentType=_password.contentType==InputField.ContentType.Password?InputField.ContentType.Standard:InputField.ContentType.Password;
            _password.ForceLabelUpdate();
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
            string email=_ownerEmail.text.Trim();
            if(email.Length>0)
            {
                try
                {
                    var parsed=new System.Net.Mail.MailAddress(email);
                    if(parsed.Address!=email || email.IndexOf(' ')>=0 || !parsed.Host.Contains("."))
                        throw new System.FormatException();
                }
                catch(System.Exception){Fail("Enter a valid email address, or leave it blank.");return false;}
            }
            if(!_ownerTerms.isOn){Fail("Read and accept the play and account guidelines to create an account.");return false;}
            return true;
        }
        private void NativeBusy(bool busy)
        {
            if(!_nativeForm)return;_nativeBusy=busy;
            foreach(var control in new Selectable[]{_nativeSubmit,_googleButton,_createTab,_signInTab,_username,_ownerEmail,_password,
                _ownerReveal,_ownerTerms,_ownerTermsLink,_ownerSwitch,_guest,_back})if(control!=null)control.interactable=!busy;
            _canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }
    }
}
