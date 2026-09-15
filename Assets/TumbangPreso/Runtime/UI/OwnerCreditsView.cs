using System;
using UnityEngine;

namespace TumbangPreso.UI
{
    public sealed class OwnerCreditsView : MonoBehaviour
    {
        private Canvas _canvas;
        private Action _back;
        public void Open(Transform owner,Action back)
        {
            _back=back;if(_canvas==null)Build(owner);
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
            var back=OwnerTextAction.Create(design,"CreditsBack","BACK",Close,54,30,172,70,30);
            back.GetComponentInChildren<UnityEngine.UI.Text>().color=new Color32(245,232,198,255);
            var logo=OwnerUiLayout.Art(design,"OriginalOwnerLogo",OwnerUiTheme.Piece.Logo);
            OwnerUiLayout.Place(logo.rectTransform,93,249,460,460*273f/407);
            logo.gameObject.AddComponent<OwnerUiMotion>().GentleFloat=true;
            var title=OwnerUiLayout.Text(design,"Heading","CREDITS",62,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform,91,608,470,172);title.alignment=TextAnchor.MiddleCenter;title.color=new Color32(245,232,198,255);
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
        private void Close(){_canvas.gameObject.SetActive(false);_back?.Invoke();}
        private void Update()
        {
            if(_canvas==null || !_canvas.gameObject.activeSelf || !InputLayer.MenuNav.CancelPressed || ScreenTakeover.EscapeIsSpoken)return;
            ScreenTakeover.ConsumeEscape();Close();
        }
        private void OnDisable(){if(_canvas!=null)_canvas.gameObject.SetActive(false);}
    }
}
