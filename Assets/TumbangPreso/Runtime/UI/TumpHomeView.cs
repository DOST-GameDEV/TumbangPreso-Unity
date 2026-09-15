using System;
using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>Quiet title composition. Artwork stays separate from editable controls.</summary>
    public sealed class TumpHomeView : MonoBehaviour
    {
        private Canvas _canvas;
        public void Build(Transform owner, Action settings, Action credits)
            => _canvas=HomeCourtView.Build(owner,settings,credits);

        private void BuildPrevious(Transform owner, Action settings, Action credits)
        {
            var f = TumpUiTheme.Current;
            _canvas = TumpUiFactory.Canvas(owner, "TumpHomeCanvas", 100);
            var root = (RectTransform)_canvas.transform;
            var street = TumpUiFactory.Rect(root, "StreetIllustration").gameObject.AddComponent<RawImage>();
            TumpUiFactory.Stretch(street.rectTransform); street.gameObject.AddComponent<TumpBackdrop>();
            var edge = TumpUiFactory.Rect(root, "TitlePaper").gameObject.AddComponent<TumpPaperEdge>();
            edge.color = f.Cream; edge.raycastTarget = false;
            edge.rectTransform.anchorMin = Vector2.zero; edge.rectTransform.anchorMax = new Vector2(0, 1);
            edge.rectTransform.offsetMin = new Vector2(-40, -20); edge.rectTransform.offsetMax = new Vector2(738, 20);
            var navigation = TumpUiFactory.Rect(root, "TitleNavigation");
            navigation.anchorMin = navigation.anchorMax = new Vector2(0, .5f); navigation.pivot = new Vector2(0, .5f);
            navigation.anchoredPosition = Vector2.zero; navigation.sizeDelta = new Vector2(738, 1080);
            var logo = TumpUiFactory.Art(navigation, "OriginalTumpLogo", f.Logo != null ? f.Logo : TumpUiFactory.Sprite("UI/brand/tump_logo"));
            TumpUiFactory.Place(logo.rectTransform, 80, 28, 584, 396);
            var play = TumpUiFactory.Button(navigation, "StartButton", "Play", () => SceneFlow.Go(SceneFlow.ModeSelect), TumpSurface.Form.Slap, f.Lime, 68);
            TumpUiFactory.Place((RectTransform)play.transform, 124, 462, 490, 128);
            Link(navigation, "TutorialButton", "Learn to play", 620, SceneFlow.StartTraining);
            Link(navigation, "SettingsButton", "Settings", 718, settings);
            Link(navigation, "QuitButton", "Quit", 816, SceneFlow.Quit);
            var credit = TumpUiFactory.Button(navigation, "CreditsButton", "Credits", credits, TumpSurface.Form.Link, f.Cream, 30);
            TumpUiFactory.Place((RectTransform)credit.transform, 250, 956, 230, 76);
            var version = TumpUiFactory.Text(navigation, "GameVersion", "", 22);
            version.alignment = TextAnchor.MiddleCenter; version.color = f.Olive;
            TumpUiFactory.Place(version.rectTransform, 160, 1034, 410, 34); GameVersion.ApplyTo(version);
            _canvas.GetComponent<ScreenFocus>().Rebuild();
        }
        private static void Link(Transform root, string name, string label, float y, Action click)
        {
            var button = TumpUiFactory.Button(root, name, label, click, TumpSurface.Form.Link, TumpUiTheme.Current.Cream, 44);
            TumpUiFactory.Place((RectTransform)button.transform, 124, y, 490, 86);
        }
        public void Suspend() { if (_canvas != null) _canvas.gameObject.SetActive(false); }
        public void Resume() { if (_canvas != null) { _canvas.gameObject.SetActive(true); _canvas.GetComponent<ScreenFocus>().Rebuild(); } }
        private void OnDisable() => Suspend();
    }
}
