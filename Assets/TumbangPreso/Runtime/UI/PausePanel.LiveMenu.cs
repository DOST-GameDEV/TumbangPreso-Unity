using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class PausePanel
    {
        protected override Canvas CreateCanvas()
        {
            var canvas = OwnerUiLayout.Canvas(transform, "OwnerPauseCanvas", 500);
            var dim = OwnerUiLayout.Rect(canvas.transform, "MatchDim").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(dim.rectTransform); dim.color = new Color(0, 0, 0, .28f);
            return canvas;
        }
        protected override void Build()
        {
            var design = OwnerUiLayout.DesignArea(Canvas.transform, "LiveMenuComposition");
            var column = OwnerUiLayout.Rect(design, "LiveMenuColumn").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(column.rectTransform, 96, 182, 662, 756); column.color = new Color32(29, 46, 35, 245);
            _title = OwnerUiLayout.Text(column.transform, "PauseTitle", "MATCH MENU", 64, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_title.rectTransform, 37, 43, 588, 110); _title.color = OwnerUiTheme.Current.Pale;
            var notice = OwnerUiLayout.Text(column.transform, "LiveNotice", "The match keeps playing while this menu is open.", 30);
            OwnerUiLayout.Place(notice.rectTransform, 43, 164, 574, 101); notice.color = OwnerUiTheme.Current.Pale;
            var resume = OwnerUiLayout.Rect(column.transform, "ResumeMatch"); OwnerUiLayout.Place(resume, 40, 334, 580, 104);
            var face = resume.gameObject.AddComponent<Image>(); face.color = OwnerUiTheme.Current.Lime;
            var button = resume.gameObject.AddComponent<Button>(); button.targetGraphic = face;
            var label = OwnerUiLayout.Text(resume, "Label", "RESUME", 50, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Fill(label.rectTransform); label.color = OwnerUiTheme.Current.Green; label.alignment = TextAnchor.MiddleCenter;
            button.onClick.AddListener(() => { MenuSfx.Click(); Resume(); });
            var settings = OwnerTextAction.Create(column.transform, "PauseSettings", "SETTINGS", OpenSettings, 42, 491, 574, 80, 38);
            settings.GetComponentInChildren<Text>().color = OwnerUiTheme.Current.Pale;
            var leave = OwnerTextAction.Create(column.transform, "LeaveMatch", "LEAVE MATCH", SceneFlow.LeaveMatchToMainMenu, 42, 623, 574, 76, 32);
            leave.GetComponentInChildren<Text>().color = OwnerUiTheme.Current.Orange;
        }
    }
}
