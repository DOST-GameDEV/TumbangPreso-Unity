using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class OwnerCharacterStoryView
    {
        /// <summary>
        /// The boot board `hub-story` (`tools/ui_artboards/boot_heroes.py`): BACK and MEET NAME on the berry weave,
        /// one cream woven panel holding the hometown, the home court, a rule, the portrait on a kit card with the
        /// personality line under it, and the story in Grandstander beside it.
        /// </summary>
        private void Build()
        {
            _canvas = OwnerUiLayout.Canvas(transform, "OwnerCharacterStoryCanvas", 730);
            var root = OwnerUiLayout.DesignArea(_canvas.transform, "CharacterStoryComposition");
            var ground = HubKnit.Ground(root);
            ground.raycastTarget = true;
            var back = HubKnit.Back(root, TumpHub.Current, Close);
            back.name = "CloseCharacterStory";
            _title = HubKnit.Title(root, "", "", out var under);
            under.gameObject.SetActive(false);

            var panel = HubKnit.FaceOf(HubKnit.Piece(root, "StoryPanel", Knit.Panel, 60, 190, 1800, 830));
            _origin = HubKnit.Display(panel, "StoryOrigin", "", 54, HubKnit.KitMaroon);
            HubKnit.In(_origin.rectTransform, 70, 46, 1600, 68);
            _court = HubKnit.Words(panel, "StoryCourt", "", 34, HubKnit.KitInk);
            HubKnit.In(_court.rectTransform, 72, 126, 1600, 48);
            var rule = HubKit.Rect(panel, "BiographyRule").gameObject.AddComponent<Image>();
            rule.color = HubKnit.KitTan; rule.raycastTarget = false;
            HubKnit.In(rule.rectTransform, 60, 195, 1680, 3);

            var card = HubKnit.FaceOf(HubKnit.Piece(panel, "StoryPortraitCard", Knit.Card, 70, 240, 500, 410));
            _portrait = HubKit.Rect(card, "StoryPortrait").gameObject.AddComponent<Image>();
            _portrait.preserveAspect = true; _portrait.raycastTarget = false;
            HubKnit.In(_portrait.rectTransform, 50, 25, 400, 360);
            _shortLine = HubKnit.Display(panel, "PersonalityLine", "", 34, HubKnit.KitMaroon, TextAnchor.UpperCenter);
            _shortLine.horizontalOverflow = HorizontalWrapMode.Wrap;
            _shortLine.lineSpacing = .9f;
            HubKnit.In(_shortLine.rectTransform, 60, 690, 520, 110);

            var content = OwnerScrollColumn.Build(panel, "StoryReading", new Rect(680, 240, 1070, 600), out _scroll);
            // No bar, as on the board: the wheel and a drag still scroll a story longer than the box.
            if (_scroll.verticalScrollbar != null) { _scroll.verticalScrollbar.gameObject.SetActive(false); _scroll.verticalScrollbar = null; }
            _body = HubKnit.Words(content, "StoryText", "", 36, HubKnit.KitInk, TextAnchor.UpperLeft);
            _body.lineSpacing = 1.6f;   // the board's 58 pitch at 36 (Grandstander's line is about 30 at 36)
            _body.gameObject.AddComponent<TumpParagraph>();
            ScreenTakeover.Register(this, () => _canvas != null && _canvas.gameObject.activeInHierarchy);
        }

        /// <summary>The paper spread before the boot kit, kept for reference; not called.</summary>
        private void BuildPaperSpread()
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
