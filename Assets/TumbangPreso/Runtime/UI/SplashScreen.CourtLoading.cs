using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class SplashScreen
    {
        // The court loading surface (rotating illustrations, inline tips, lime rail) was deleted
        // on the owner's instruction, 2026-10-22: it was no longer shown anywhere. Only the
        // boot-failure link it shared with `MenuActivation` remains.
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
