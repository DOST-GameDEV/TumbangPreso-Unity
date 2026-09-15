using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class SplashScreen
    {
        private static Sprite _courtProgressSprite;
        private void BuildCourtLoadingSurface()
        {
            _ownerLoading = true;
            var canvas = OwnerUiLayout.Canvas(transform, "OwnerLoadingCanvas", 500);
            _canvas = canvas.gameObject; HideConvertedContent();
            var art = OwnerUiLayout.Rect(canvas.transform, "LoadingStreetIllustration").gameObject.AddComponent<RawImage>();
            art.texture = Resources.Load<Texture2D>("UI/composition-redesign/loading-street"); art.raycastTarget = false;
            OwnerUiLayout.Fill(art.rectTransform);
            if (art.texture != null)
            {
                var fit = art.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = art.texture.width / (float)art.texture.height;
            }
            var design = OwnerUiLayout.DesignArea(canvas.transform, "CourtLoadingComposition");
            var logo = OwnerUiLayout.Art(design, "LoadingOwnerLogo", OwnerUiTheme.Piece.Logo);
            OwnerUiLayout.Place(logo.rectTransform, 105, 66, 235, 235 * 273f / 407);
            _loadingLabel = OwnerUiLayout.Text(design, "LoadingStatus", "GETTING READY", 46, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_loadingLabel.rectTransform, 114, 757, 960, 83);
            _loadingLabel.color = OwnerUiTheme.Current.Pale;
            if (_courtProgressSprite == null)
            {
                var white = Texture2D.whiteTexture;
                _courtProgressSprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height), Vector2.one * .5f);
                _courtProgressSprite.hideFlags = HideFlags.DontSave;
            }
            var rail = OwnerUiLayout.Rect(design, "ProgressTrack").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(rail.rectTransform, 116, 868, 810, 9); rail.sprite = _courtProgressSprite;
            rail.color = new Color(1, 1, 1, .22f); rail.raycastTarget = false;
            _loadingFill = OwnerUiLayout.Rect(design, "ActualProgress").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(_loadingFill.rectTransform, 116, 868, 810, 9);
            _loadingFill.sprite = _courtProgressSprite; _loadingFill.color = OwnerUiTheme.Current.Lime;
            _loadingFill.raycastTarget = false; _loadingFill.type = Image.Type.Filled;
            _loadingFill.fillMethod = Image.FillMethod.Horizontal; _loadingFill.fillAmount = 0;
            _artButton = LoadingLink(design, "LoadingStories", "STORIES & TIPS", () => ShowStory(true), 109, 917, 435, OwnerUiTheme.Current.Pale, true);
            var mark = OwnerUiLayout.Rect(design, "LoadingSlipper").gameObject.AddComponent<RawImage>();
            mark.texture = Resources.Load<Texture2D>("UI/brand/tsinelas_hit"); mark.raycastTarget = false;
            float ratio = mark.texture != null ? mark.texture.width / (float)mark.texture.height : 1;
            OwnerUiLayout.Place(mark.rectTransform, 1700, 891, 95 * ratio, 95); _loadingMark = mark.rectTransform;

            _storyRoot = OwnerUiLayout.Rect(design, "LoadingStoryRoot").gameObject;
            OwnerUiLayout.Fill((RectTransform)_storyRoot.transform);
            var blocker = _storyRoot.AddComponent<Image>(); blocker.color = new Color(0, 0, 0, .48f);
            var sheet = OwnerUiLayout.Rect(_storyRoot.transform, "StorySheet").gameObject.AddComponent<OwnerUiPaper>();
            sheet.Style = OwnerUiPaper.Treatment.Dialog; sheet.raycastTarget = true;
            OwnerUiLayout.Place(sheet.rectTransform, 400, 266, 1108, 552);
            var title = OwnerUiLayout.Text(sheet.transform, "StoryHeading", "FROM THE STREET", 48, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(title.rectTransform, 55, 37, 992, 75);
            _storyText = OwnerUiLayout.Text(sheet.transform, "StoryText", "", 32);
            OwnerUiLayout.Place(_storyText.rectTransform, 57, 147, 990, 263); _storyText.alignment = TextAnchor.UpperLeft;
            _storyText.color = OwnerUiTheme.Current.EnteredInk;
            LoadingLink(sheet.transform, "LoadingStoryClose", "CLOSE", () => ShowStory(false), 54, 452, 232, OwnerUiTheme.Current.ActionInk);
            LoadingLink(sheet.transform, "LoadingStoryNext", "NEXT STORY", () => { _storyIndex++; RefreshStory(); }, 714, 452, 332, OwnerUiTheme.Current.ActionInk);
            InputLayer.ScreenFocus.Install(_storyRoot); _storyRoot.SetActive(false);
            var fade = OwnerUiLayout.Rect(canvas.transform, "LoadingFade").gameObject.AddComponent<Image>();
            OwnerUiLayout.Fill(fade.rectTransform); fade.color = new Color32(49, 24, 27, 255); fade.raycastTarget = false; _fade = fade;
        }

        private static Button LoadingLink(Transform parent, string name, string words, System.Action action, float x, float y, float width, Color ink, bool onDark = false)
        {
            var root = OwnerUiLayout.Rect(parent, name); OwnerUiLayout.Place(root, x, y, width, 68);
            var hit = root.gameObject.AddComponent<Image>(); hit.color = Color.clear;
            var label = OwnerUiLayout.Text(root, "Label", words, 30, OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Fill(label.rectTransform); label.color = Color.white;
            var button = root.gameObject.AddComponent<Button>(); button.targetGraphic = label;
            var colours = button.colors; colours.normalColor = ink;
            colours.highlightedColor = onDark ? OwnerUiTheme.Current.Lime : OwnerUiTheme.Current.Green;
            colours.selectedColor = colours.highlightedColor; colours.pressedColor = OwnerUiTheme.Current.Ochre;
            colours.fadeDuration = .08f; button.colors = colours;
            button.onClick.AddListener(() => { MenuSfx.Click(); action(); });
            return button;
        }
    }
}
