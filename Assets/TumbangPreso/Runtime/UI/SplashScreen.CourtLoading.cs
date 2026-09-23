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
            var artwork = LoadingArtwork.Install((RectTransform)canvas.transform);
            var design = OwnerUiLayout.DesignArea(canvas.transform, "CourtLoadingComposition");
            var logo = OwnerUiLayout.Art(design, "LoadingOwnerLogo", OwnerUiTheme.Piece.Logo);
            OwnerUiLayout.Place(logo.rectTransform, 105, 66, 235, 235 * 273f / 407);
            _loadingLabel = OwnerUiLayout.Text(design, "LoadingStatus", "GETTING READY", 46, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Place(_loadingLabel.rectTransform, 114, 757, 960, 83);
            _loadingLabel.color = OwnerUiTheme.Current.Pale;
            var statusEdge = _loadingLabel.gameObject.AddComponent<Outline>();
            statusEdge.effectColor = Hub.HubStyle.Ink; statusEdge.effectDistance = new Vector2(2, -2);
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
            // The tip is part of the loading screen. It never opens another layer or holds loading.
            var tipBand = OwnerUiLayout.Rect(design, "InlineTipBand").gameObject.AddComponent<Image>();
            OwnerUiLayout.Place(tipBand.rectTransform, 88, 907, 1475, 155);
            tipBand.color = new Color(0.11f, 0.06f, 0.03f, .9f); tipBand.raycastTarget = false;
            _storyText = OwnerUiLayout.Text(design, "InlineLoadingTip", "", Hub.HubStyle.Size(Hub.HubStyle.Label));
            OwnerUiLayout.Place(_storyText.rectTransform, 114, 930, 1390, 108);
            _storyText.font = Hub.HubStyle.ReadingFont;
            _storyText.color = OwnerUiTheme.Current.Pale; _storyText.raycastTarget = false;
            artwork.BindTip(_storyText);
            var mark = OwnerUiLayout.Rect(design, "LoadingSlipper").gameObject.AddComponent<RawImage>();
            mark.texture = Resources.Load<Texture2D>("UI/brand/tsinelas_hit"); mark.raycastTarget = false;
            float ratio = mark.texture != null ? mark.texture.width / (float)mark.texture.height : 1;
            OwnerUiLayout.Place(mark.rectTransform, 1700, 891, 95 * ratio, 95); _loadingMark = mark.rectTransform;

            _storyRoot = null;
            _artButton = null;
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
