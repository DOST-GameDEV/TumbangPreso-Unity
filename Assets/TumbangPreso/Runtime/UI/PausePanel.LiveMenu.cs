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
        /// <summary>
        /// The live match menu: a warm ink card over the running match, three actions.
        ///
        /// ⚠️ ONE KIND OF OBJECT FOR THREE ACTIONS (2026-09-23 UI review). RESUME was a flat lime
        /// rectangle with no focus or press state, SETTINGS and LEAVE MATCH were floating words, so
        /// the card showed three different kinds of thing for three choices, and the column was a
        /// cold dark green (29,46,35, blue over red: `CLAUDE.md` § 6.4). The actions are now the
        /// front end's stickers (`HubKit.Button`: one focus ring, one lift, one press, one hatch),
        /// ranked by colour: chartreuse RESUME is the one primary, honey SETTINGS is ordinary, deep
        /// red LEAVE MATCH is the one destructive control, which is exactly the logo palette's role
        /// table. The card is the in-match card family's warm ink. Names and callbacks are unchanged,
        /// so every diagnostic route that clicks `ResumeMatch` or `LeaveMatch` still finds them.
        /// The live-match notice stays: it is a rule (the match does not pause), not navigation.
        /// </summary>
        protected override void Build()
        {
            var design = OwnerUiLayout.DesignArea(Canvas.transform, "LiveMenuComposition");
            var holder = OwnerUiLayout.Rect(design, "LiveMenuColumn"); OwnerUiLayout.Place(holder, 96, 182, 662, 756);
            var card = Hub.HubKit.Plate(holder, "LiveMenuCard", new Color(Hub.HubStyle.Night.r, Hub.HubStyle.Night.g, Hub.HubStyle.Night.b, 0.95f), 41, 5.0f);
            card.ShadowOffset = new Vector2(8, -9); card.raycastTarget = true;
            _title = OwnerUiLayout.Text(holder, "PauseTitle", "MATCH MENU", 64, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_title.rectTransform, 44, 40, 580, 104); _title.color = Hub.HubStyle.Honey;
            var notice = OwnerUiLayout.Text(holder, "LiveNotice", "The match keeps playing while this menu is open.", 30);
            OwnerUiLayout.Place(notice.rectTransform, 46, 150, 570, 92); notice.color = Hub.HubStyle.HoneySoft;
            notice.font = Hub.HubStyle.ReadingFont;
            var rule = OwnerUiLayout.Rect(holder, "PauseRule").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(rule.rectTransform, 46, 262, 570, 4); rule.color = new Color(Hub.HubStyle.Honey.r, Hub.HubStyle.Honey.g, Hub.HubStyle.Honey.b, 0.22f); rule.raycastTarget = false;
            Action("ResumeMatch", "RESUME", Hub.HubStyle.Chartreuse, () => Resume(), 312, 116, Hub.HubStyle.Title);
            Action("PauseSettings", "SETTINGS", Hub.HubStyle.Honey, OpenSettings, 466, 96, Hub.HubStyle.Label);
            Action("LeaveMatch", "LEAVE MATCH", Hub.HubStyle.DeepRed, SceneFlow.LeaveMatchToMainMenu, 600, 96, Hub.HubStyle.Label);
            void Action(string name, string words, Color fill, System.Action press, float y, float height, int step)
            {
                var button = Hub.HubKit.Button(holder, name, words, fill, press, step);
                OwnerUiLayout.Place((RectTransform)button.transform, 46, y, 570, height);
            }
        }
    }
}
