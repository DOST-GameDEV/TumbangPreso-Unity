using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class OwnerCharacterStoryView
    {
        private void Build()
        {
            _canvas = OwnerUiLayout.Canvas(transform, "OwnerCharacterStoryCanvas", 730);
            var background = OwnerUiLayout.Rect(_canvas.transform, "BiographyPaper").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(background.rectTransform); background.color = new Color32(233, 211, 177, 255); background.raycastTarget = false;
            var root = OwnerUiLayout.DesignArea(_canvas.transform, "CharacterStoryComposition");
            OwnerTextAction.CreateBack(root, "CloseCharacterStory", Close, 55, 25);
            _title = OwnerUiLayout.Text(root, "StoryTitle", "", 76, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_title.rectTransform, 91, 126, 1720, 131);
            _origin = OwnerUiLayout.Text(root, "StoryOrigin", "", 42, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(_origin.rectTransform, 95, 277, 1710, 77);
            _court = OwnerUiLayout.Text(root, "StoryCourt", "", 30);
            OwnerUiLayout.Place(_court.rectTransform, 98, 364, 1702, 65); _court.color = OwnerUiTheme.Current.EnteredInk;
            var rule = OwnerUiLayout.Rect(root, "BiographyRule").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(rule.rectTransform, 95, 444, 1718, 3); rule.color = OwnerUiTheme.Current.ActionInk; rule.raycastTarget = false;
            _portrait = OwnerUiLayout.Rect(root, "StoryPortrait").gameObject.AddComponent<Image>();
            _portrait.preserveAspect = true; _portrait.raycastTarget = false;
            OwnerUiLayout.Place(_portrait.rectTransform, 141, 493, 493, 413);
            _shortLine = OwnerUiLayout.Text(root, "PersonalityLine", "", 31, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(_shortLine.rectTransform, 93, 930, 607, 113); _shortLine.alignment = TextAnchor.MiddleCenter;
            var content = OwnerScrollColumn.Build(root, "StoryReading", new Rect(796, 496, 990, 535), out _scroll);
            _body = OwnerUiLayout.Text(content, "StoryText", "", 35); _body.alignment = TextAnchor.UpperLeft;
            _body.color = OwnerUiTheme.Current.EnteredInk; _body.lineSpacing = 1.12f; _body.gameObject.AddComponent<TumpParagraph>();
            ScreenTakeover.Register(this, () => _canvas != null && _canvas.gameObject.activeInHierarchy);
        }
    }
}
