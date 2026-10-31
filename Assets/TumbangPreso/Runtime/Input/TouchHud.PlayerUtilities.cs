using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.InputLayer
{
    public sealed partial class TouchHud
    {
        private GameObject _playerTools, _infoControl, _curveControls;
        private void BuildPlayerUtilities(RectTransform root)
        {
            if (SpectatorMode) return;
            const float size = TouchMetrics.MinTargetUnits;
            var tools = OwnerUiLayout.Rect(root, "PlayerTools");
            _playerTools = tools.gameObject;
            tools.anchorMin = tools.anchorMax = tools.pivot = Vector2.one;
            tools.anchoredPosition = new Vector2(-280, -36); tools.sizeDelta = new Vector2(size * 2 + 16, size);
            var info = PlayerUtility(tools, "TouchAbilityInfo", "HOLD\nINFO", 0, 0, null);
            info.gameObject.AddComponent<TouchHoldControl>().Bind(this, "AbilityInfo"); _infoControl = info.gameObject;
            PlayerUtility(tools, "TouchPlayerMenu", "MENU", size + 16, 0,
                () => Object.FindFirstObjectByType<PauseWatcher>()?.ToggleFromUi());
            var curves = OwnerUiLayout.Rect(root, "ThrowCurveControls");
            curves.anchorMin = curves.anchorMax = curves.pivot = Vector2.up;
            curves.anchoredPosition = new Vector2(36, -385);
            curves.sizeDelta = new Vector2(size * 2 + TouchMetrics.MinGapUnits, size); _curveControls = curves.gameObject;
            PlayerUtility(curves, "TouchCurveLeft", "CURVE\nLEFT", 0, 0, null).gameObject.AddComponent<TouchHoldControl>().Bind(this, "CurveLeft");
            PlayerUtility(curves, "TouchCurveRight", "CURVE\nRIGHT", size + TouchMetrics.MinGapUnits, 0, null).gameObject.AddComponent<TouchHoldControl>().Bind(this, "CurveRight");
            RefreshPlayerUtilities();
        }
        private static Button PlayerUtility(Transform parent, string name, string words, float x, float y, System.Action action)
        {
            var button = SettingsWorkspaceRows.Action(parent, name, words, action ?? (() => {}), TouchMetrics.MinTargetUnits);
            OwnerUiLayout.Place((RectTransform)button.transform, x, y, TouchMetrics.MinTargetUnits, TouchMetrics.MinTargetUnits);
            SettingsWorkspaceRows.Slab(button, false);
            button.GetComponentInChildren<Text>().fontSize = 30;
            return button;
        }
        private void RefreshPlayerUtilities()
        {
            if (_playerTools != null && _playerTools.activeSelf == TouchButton.Customising) _playerTools.SetActive(!TouchButton.Customising);
            if (_infoControl != null) _infoControl.SetActive(SceneFlow.SelectedMode == Core.GameMode.HeroStrike && !TouchButton.Customising);
            bool charging = !TouchButton.Customising && _local != null && !_local.IsDefender && _local.HoldingSlipper && TouchInput.Pressed(Verb.SpecialAbility);
            if (_curveControls != null && _curveControls.activeSelf != charging) _curveControls.SetActive(charging);
        }
    }
}
