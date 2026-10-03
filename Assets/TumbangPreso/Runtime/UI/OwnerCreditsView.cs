using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TumbangPreso.Core;
using TumbangPreso.Net;

namespace TumbangPreso.UI
{
    public sealed class OwnerCreditsView : MonoBehaviour
    {
        private Canvas _canvas;
        private Action _back;
        private readonly CreditsCodeSequence _code = new CreditsCodeSequence();
        private UnityEngine.UI.Text _bonus;
        private bool _claiming, _dragging;
        private Vector2 _dragStart;
        private string _rewardRequest;
        public void Open(Transform owner,Action back)
        {
            _back=back;if(_canvas==null)Build(owner);
            _code.Reset(); _dragging=false; _bonus.text="";
            _canvas.gameObject.SetActive(true);_canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
        }
        private void Build(Transform owner)
        {
            _canvas=OwnerUiLayout.Canvas(owner,"OwnerCreditsCanvas",800);
            var ground=OwnerUiLayout.Rect(_canvas.transform,"CreditsGround").gameObject.AddComponent<UnityEngine.UI.Image>();
            OwnerUiLayout.Fill(ground.rectTransform);ground.color=new Color32(239,225,198,255);ground.raycastTarget=false;
            var design=OwnerUiLayout.DesignArea(_canvas.transform,"CreditsComposition");
            var margin=OwnerUiLayout.Rect(design,"StudioMargin").gameObject.AddComponent<UnityEngine.UI.Image>();
            OwnerUiLayout.Place(margin.rectTransform,0,0,605,1080);margin.color=new Color32(102,42,48,255);margin.raycastTarget=false;
            var back=OwnerTextAction.CreateBack(design,"CreditsBack",Close,54,30);
            back.GetComponentInChildren<UnityEngine.UI.Text>().color=new Color32(245,232,198,255);
            var logo=OwnerUiLayout.Art(design,"OriginalOwnerLogo",OwnerUiTheme.Piece.Logo);
            OwnerUiLayout.Place(logo.rectTransform,93,249,460,460*273f/407);
            logo.gameObject.AddComponent<OwnerUiMotion>().GentleFloat=true;
            var title=OwnerUiLayout.Text(design,"Heading","CREDITS",62,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform,91,608,470,172);title.alignment=TextAnchor.MiddleCenter;title.color=new Color32(245,232,198,255);
            _bonus=OwnerUiLayout.Text(design,"CreditsBonus","",32,OwnerUiLayout.TypeRole.Reading);
            OwnerUiLayout.Place(_bonus.rectTransform,91,804,470,144);_bonus.alignment=TextAnchor.UpperCenter;
            _bonus.color=new Color32(245,232,198,255);
            var content=OwnerScrollColumn.Build(design,"Credits",new Rect(698,125,1066,824),out var scroll);
            Heading(content,"THE TEAM");
            foreach(var member in CreditsContent.TeamCredits)
            {
                Label(content,"TeamName",member.Name,36,OwnerUiLayout.TypeRole.Display);
                Label(content,"TeamRole",member.Role,30,OwnerUiLayout.TypeRole.Reading);
            }
            Heading(content,"ARTWORK & SOUND");foreach(var credit in CreditsContent.CourtesyCredits)Credit(content,credit);
            Heading(content,"LICENSED ASSETS");foreach(var credit in CreditsContent.CcByCredits)Credit(content,credit);
            Canvas.ForceUpdateCanvases();UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scroll.verticalNormalizedPosition=1;
        }
        private static void Heading(Transform parent,string words)
        {
            var title=OwnerUiLayout.Text(parent,"Section",words,34,OwnerUiLayout.TypeRole.Accent);
            title.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight=84;
        }
        private static void Label(Transform parent,string name,string words,int size,OwnerUiLayout.TypeRole role)
        {
            var label=OwnerUiLayout.Text(parent,name,words,size,role);
            label.color=role==OwnerUiLayout.TypeRole.Reading?OwnerUiTheme.Current.EnteredInk:OwnerUiTheme.Current.ActionInk;
            label.verticalOverflow=VerticalWrapMode.Overflow;label.gameObject.AddComponent<TumpParagraph>();
        }
        private static void Credit(Transform parent,CreditsContent.Credit credit)
        {
            Label(parent,"CreditName",credit.Chip,32,OwnerUiLayout.TypeRole.Accent);
            Label(parent,"CreditBody",credit.Body,30,OwnerUiLayout.TypeRole.Reading);
        }
        private void Close(){_code.Reset();_dragging=false;_canvas.gameObject.SetActive(false);_back?.Invoke();}
        private void Update()
        {
            if(_canvas==null || !_canvas.gameObject.activeSelf)return;
            if(InputLayer.MenuNav.CancelPressed && !ScreenTakeover.EscapeIsSpoken)
            { ScreenTakeover.ConsumeEscape();Close();return; }
            if(_claiming)return;
            var keys=Keyboard.current;var pad=Gamepad.current;
            int direction=-1,count=0;
            void Add(int value){direction=value;count++;}
            if((keys?.upArrowKey.wasPressedThisFrame??false)||(pad?.dpad.up.wasPressedThisFrame??false))Add(0);
            if((keys?.downArrowKey.wasPressedThisFrame??false)||(pad?.dpad.down.wasPressedThisFrame??false))Add(1);
            if((keys?.leftArrowKey.wasPressedThisFrame??false)||(pad?.dpad.left.wasPressedThisFrame??false))Add(2);
            if((keys?.rightArrowKey.wasPressedThisFrame??false)||(pad?.dpad.right.wasPressedThisFrame??false))Add(3);
            // Directional drags keep the hidden sequence available to mouse and touch users.
            var touch=Touchscreen.current?.primaryTouch;
            bool touching=touch!=null && (touch.press.isPressed||touch.press.wasReleasedThisFrame);
            bool down=touching?touch.press.wasPressedThisFrame:(Mouse.current?.leftButton.wasPressedThisFrame??false);
            bool up=touching?touch.press.wasReleasedThisFrame:(Mouse.current?.leftButton.wasReleasedThisFrame??false);
            Vector2 position=touching?touch.position.ReadValue():Mouse.current?.position.ReadValue()??Vector2.zero;
            if(down){_dragStart=position;_dragging=true;}
            if(up && _dragging)
            {
                _dragging=false;var delta=position-_dragStart;
                if(delta.magnitude>=50)Add(Mathf.Abs(delta.y)>=Mathf.Abs(delta.x)?(delta.y>0?0:1):(delta.x<0?2:3));
            }
            if(count>1)_code.Reset();
            else if(count==1)EnterDirection(direction);
        }
        private void EnterDirection(int direction)
        {
            if(_canvas==null || !_canvas.gameObject.activeSelf || _claiming)return;
            if(_code.Push(direction))_ = RedeemCode();
        }
        private async System.Threading.Tasks.Task RedeemCode()
        {
            _claiming=true;
            _rewardRequest??=Guid.NewGuid().ToString("N");
            try
            {
                var wallet=WalletStore.Instance;
                if(wallet==null){_bonus.text="Sign in to claim the Credits bonus.";return;}
                string result=await wallet.ClaimCreditsCodeAsync(_rewardRequest);
                if(this==null)return;
                if(result=="credits-code-granted" || result=="credits-code-already")
                {
                    _rewardRequest=null;_bonus.text="+5,000 TANSAN!";
                    GameServices.Audio?.PlayUi("match_win");
                }
                else _bonus.text=wallet.Status;
            }
            finally{if(this!=null)_claiming=false;}
        }
        private void OnDisable(){_code.Reset();_dragging=false;if(_canvas!=null)_canvas.gameObject.SetActive(false);}
    }
}
