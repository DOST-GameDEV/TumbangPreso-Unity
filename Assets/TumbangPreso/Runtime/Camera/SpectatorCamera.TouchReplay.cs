using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.CameraSystem
{
    public sealed partial class SpectatorCamera
    {
        private void BuildTouchReplayReturn()
        {
            // The replay covers the touch toolbar. Its own exit must stay visible and tappable.
            _replayCanvas.gameObject.AddComponent<GraphicRaycaster>();
            var safe = UiSafeArea.Content(_replayCanvas, "ReplaySafeArea");
            _replayLabel.rectTransform.SetParent(safe, false);
            _replayExitLabel.rectTransform.SetParent(safe, false);
            var rect = OwnerUiLayout.Rect(safe, "ReplayReturnToLive");
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-28, 20); rect.sizeDelta = new Vector2(300, 144);
            var face = rect.gameObject.AddComponent<HudCard>(); face.Glass(HudDraw.Glass, 0);
            face.Border = HudDraw.Honey; face.BorderWidth = 2; face.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = face;
            button.onClick.AddListener(() => EndReplay());
            var words = OwnerUiLayout.Text(rect, "ReturnLabel", "RETURN\nTO LIVE", 32, OwnerUiLayout.TypeRole.Display);
            OwnerUiLayout.Fill(words.rectTransform); words.color = HudDraw.Ivory; words.alignment = TextAnchor.MiddleCenter;
            words.verticalOverflow = VerticalWrapMode.Overflow;
            var ink = words.gameObject.AddComponent<Outline>(); ink.effectColor = UiTheme.InGameOutline;
            ink.effectDistance = new Vector2(2, -2);
            rect.gameObject.SetActive(InputLayer.TouchHud.ShouldShow);
        }
    }
}
