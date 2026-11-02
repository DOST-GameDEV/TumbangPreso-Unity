using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI.Hub;

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
            // The boot kit (`boot_menus.py`): the berry ground, a red BACK, the logo and title on the left,
            // the team on a cream panel.
            HubKnit.Ground(_canvas.transform,"CreditsGround");
            var design=OwnerUiLayout.DesignArea(_canvas.transform,"CreditsComposition");
            HubKnit.Back((RectTransform)design,null,Close).name="CreditsBack";
            var logo=OwnerUiLayout.Art(design,"OriginalOwnerLogo",OwnerUiTheme.Piece.Logo);
            OwnerUiLayout.Place(logo.rectTransform,80,300,440,440*273f/407);
            logo.gameObject.AddComponent<OwnerUiMotion>().GentleFloat=true;
            var title=HubKnit.Display(design,"Heading","CREDITS",96,HubKnit.KitWhite,TextAnchor.MiddleCenter,8);
            OwnerUiLayout.Place(title.rectTransform,60,640,480,150);
            _bonus=HubKnit.Words(design,"CreditsBonus","",32,HubKnit.KitWhite,TextAnchor.UpperCenter,true,3);
            OwnerUiLayout.Place(_bonus.rectTransform,60,804,480,144);
            var panel=HubKnit.Plate(design,"CreditsPanel",Knit.Panel);
            OwnerUiLayout.Place(panel,640,50,1220,970+HubKnit.SlabHeight);
            var content=OwnerScrollColumn.Build(design,"Credits",new Rect(698,125,1066,824),out var scroll);
            Heading(content,"THE TEAM");
            foreach(var member in CreditsContent.TeamCredits)
            {
                Label(content,"TeamName",member.Name,48,OwnerUiLayout.TypeRole.Display);
                Label(content,"TeamRole",member.Role,30,OwnerUiLayout.TypeRole.Reading);
            }
            Heading(content,"ARTWORK & SOUND");foreach(var credit in CreditsContent.CourtesyCredits)Credit(content,credit);
            Heading(content,"LICENSED ASSETS");foreach(var credit in CreditsContent.CcByCredits)Credit(content,credit);
            Canvas.ForceUpdateCanvases();UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            scroll.verticalNormalizedPosition=1;
        }
        // Words on the cream panel: Darumadrop names in maroon, Grandstander roles and notes in dark ink.
        private static void Heading(Transform parent,string words)
        {
            var title=OwnerUiLayout.Text(parent,"Section",words,40,OwnerUiLayout.TypeRole.Display);
            title.color=HubKnit.KitTanDark;
            title.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight=84;
        }
        private static void Label(Transform parent,string name,string words,int size,OwnerUiLayout.TypeRole role)
        {
            var label=OwnerUiLayout.Text(parent,name,words,size,role);
            if(role==OwnerUiLayout.TypeRole.Reading){label.font=HubKnit.SentenceFont(true);label.color=HubKnit.KitInk;}
            else label.color=HubKnit.KitMaroon;
            label.verticalOverflow=VerticalWrapMode.Overflow;label.gameObject.AddComponent<TumpParagraph>();
        }
        private static void Credit(Transform parent,CreditsContent.Credit credit)
        {
            Label(parent,"CreditName",credit.Chip,36,OwnerUiLayout.TypeRole.Display);
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
            _bonus.text="Claiming +5,000 Tansan...";
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
                else _bonus.text=BonusFailureMessage(result,wallet.Status);
            }
            finally{if(this!=null)_claiming=false;}
        }
        private static string BonusFailureMessage(string result,string fallback)
            => result=="busy"?"Wallet busy. Try the code again in a moment."
             : result=="offline"?"Sign in to claim the Credits bonus."
             : result=="cancelled"?"Account changed. Enter the code again."
             : string.IsNullOrEmpty(fallback)?"Could not confirm the bonus. Try the code again.":fallback;
        private void OnDisable(){_code.Reset();_dragging=false;if(_canvas!=null)_canvas.gameObject.SetActive(false);}
    }
}
