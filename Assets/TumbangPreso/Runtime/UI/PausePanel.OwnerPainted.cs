using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class PausePanel
    {
        protected override Canvas CreateCanvas()
        {
            var canvas=OwnerUiLayout.Canvas(transform,"OwnerPauseCanvas",500);
            var dim=OwnerUiLayout.Rect(canvas.transform,"MatchDim").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(dim.rectTransform);var ink=OwnerUiTheme.Current.DeepInk;dim.color=new Color(ink.r,ink.g,ink.b,.77f);
            return canvas;
        }
        protected override void Build()
        {
            var sheet=OwnerUiLayout.Rect(Canvas.transform,"MatchMenuPaper").gameObject.AddComponent<OwnerUiPaper>();sheet.Style=OwnerUiPaper.Treatment.Dialog;
            sheet.rectTransform.anchorMin=sheet.rectTransform.anchorMax=sheet.rectTransform.pivot=new Vector2(.5f,.5f);
            sheet.rectTransform.sizeDelta=new Vector2(944,744);sheet.raycastTarget=true;
            _title=OwnerUiLayout.Text(sheet.transform,"PauseTitle","MATCH MENU",58,OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_title.rectTransform,56,54,832,105);_title.alignment=TextAnchor.MiddleCenter;
            var note=OwnerUiLayout.Text(sheet.transform,"LiveNotice","The match keeps playing while this menu is open.",29);
            note.color=OwnerUiTheme.Current.EnteredInk;note.alignment=TextAnchor.MiddleCenter;
            OwnerUiLayout.Place(note.rectTransform,102,175,740,76);
            var resume=OwnerPaintedAction.Create(sheet.transform,"ResumeMatch","RESUME",Resume,false,50);
            OwnerUiLayout.Place((RectTransform)resume.transform,266,303,413,91);
            OwnerTextAction.Create(sheet.transform,"PauseSettings","SETTINGS",OpenSettings,226,457,493,78,35);
            OwnerTextAction.Create(sheet.transform,"LeaveMatch","LEAVE MATCH",SceneFlow.LeaveMatchToMainMenu,226,588,493,78,31);
        }
    }
}
