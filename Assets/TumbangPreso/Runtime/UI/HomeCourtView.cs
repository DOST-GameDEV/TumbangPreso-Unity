using System;
using UnityEngine;

namespace TumbangPreso.UI
{
    // PDF41's quiet side-menu idea, newly composed around the court illustration.
    // The logo is original artwork; every label and hit target remains native.
    public static class HomeCourtView
    {
        public static Canvas Build(Transform owner, Action settings, Action credits)
        {
            var canvas = OwnerUiLayout.Canvas(owner, "OwnerHomeCanvas", 100);
            var background = OwnerUiLayout.Rect(canvas.transform, "HomeCourtIllustration");
            OwnerUiLayout.Fill(background);
            background.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            background.gameObject.AddComponent<HomeCourtScene>();
            var design = OwnerUiLayout.DesignArea(canvas.transform, "HomeCourtComposition");
            var logo = OwnerUiLayout.Art(design, "OriginalOwnerLogo", OwnerUiTheme.Piece.Logo);
            OwnerUiLayout.Place(logo.rectTransform, 116, 80, 540, 540 * 273f / 407);
            Action(design, "StartButton", "PLAY", () => SceneFlow.Go(SceneFlow.ModeSelect), 138, 486, 504, 104, 66, true);
            Action(design, "TutorialButton", "LEARN TO PLAY", SceneFlow.StartTraining, 151, 625, 465, 80, 42);
            Action(design, "SettingsButton", "SETTINGS", settings, 151, 729, 465, 80, 42);
            Action(design, "QuitButton", "QUIT", SceneFlow.Quit, 151, 833, 465, 80, 42);
            Action(design, "CreditsButton", "CREDITS", credits, 151, 940, 238, 58, 30);
            var version = OwnerUiLayout.Text(design, "GameVersion", "", 28, OwnerUiLayout.TypeRole.Reading);
            OwnerUiLayout.Place(version.rectTransform, 1350, 1020, 470, 36);
            version.alignment = TextAnchor.MiddleRight;
            version.color = OwnerUiTheme.Current.DeepInk; GameVersion.ApplyTo(version);
            canvas.GetComponent<InputLayer.ScreenFocus>().Rebuild();
            return canvas;
        }

        private static void Action(Transform parent, string name, string words, Action click,
            float x, float y, float width, float height, int size, bool primary = false)
        {
            var root = OwnerUiLayout.Rect(parent, name); OwnerUiLayout.Place(root, x, y, width, height);
            var hit = root.gameObject.AddComponent<UnityEngine.UI.Image>(); hit.color = Color.clear;
            var paint = OwnerUiLayout.Rect(root, "HomeBrush").gameObject.AddComponent<HomeMenuStroke>();
            OwnerUiLayout.Fill(paint.rectTransform); paint.Primary = primary; paint.raycastTarget = false;
            var label = OwnerUiLayout.Text(root, "Label", words, size,
                primary ? OwnerUiLayout.TypeRole.Display : OwnerUiLayout.TypeRole.Accent);
            OwnerUiLayout.Place(label.rectTransform, primary ? 44 : 6, primary ? 2 : 0,
                width - (primary ? 132 : 12), height - (primary ? 7 : 9));
            var button = root.gameObject.AddComponent<HomeMenuAction>();
            button.targetGraphic = hit; button.transition = UnityEngine.UI.Selectable.Transition.None;
            button.Configure(label, paint);
            button.onClick.AddListener(() => { MenuSfx.Click(); click?.Invoke(); });
        }
    }
}
