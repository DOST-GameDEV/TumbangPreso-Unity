using System;
using System.Collections;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TumbangPreso.Diagnostics
{
    public sealed partial class OwnerUiPlayerReview
    {
        // Focused native follow-up for127.3. Uses the actual match canvas and existing
        // screenshot path without depending on retired front-end menu automation.
        private IEnumerator MatchChatOnly()
        {
            Stage("first match-chat line at enlarged HUD size");
            SceneFlow.Networked = false;
            SceneFlow.PinSelectedRules(Core.CustomGameRules.Defaults(Core.GameMode.HeroStrike));
            SettingsStore.Current.HudScale = 1.2f;
            SettingsStore.Current.LargerText = true;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            yield return WaitFor(() => GameObject.Find("OwnerMatchCanvas") != null && GameServices.Round != null, 30);
            var canvas = GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            var chat = LobbyChat.Attach(canvas.transform, true);
            chat.PlaceBottomRight(38, 232, 540);
            const string message = "LOCAL UI REVIEW: a readable chat line.";
            typeof(LobbyChat).GetMethod("AddLocal", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(chat, new object[] { message });
            var row = chat.transform.Find("ChatLine5").GetComponent<Text>();
            foreach (var size in new[] { new Vector2Int(1680, 720), new Vector2Int(960, 540) })
            {
                Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
                yield return WaitFor(() => Screen.width == size.x && Screen.height == size.y, 8);
                yield return Shot("match-chat-first-line-large-" + size.x + "x" + size.y);
                if (row.text != message || row.preferredHeight > row.rectTransform.rect.height + .5f ||
                    row.cachedTextGenerator.characterCountVisible < row.text.Length ||
                    ((RectTransform)chat.transform).anchoredPosition.x >= 0)
                    throw new InvalidOperationException("Native match chat lost content, clipped its row or lost its right anchor: " + row.text);
            }
            Stage("native first message fully rendered at both enlarged-HUD sizes");
        }

        private IEnumerator AccessibleClick(string name)
        {
            yield return WaitFor(() => Find(name) != null);
            // Use the shipped focus-following scroll behavior, then the normal
            // raycast/click path. Never click a clipped off-screen control.
            Find(name).Select(); yield return null; yield return null;
            yield return Click(name);
        }
        private IEnumerator AccessibilityOnly()
        {
            Stage("accessibility settings through real preparation route");
            yield return WaitFor(() => Find("GuestAccount") != null || Find("ContinueAccount") != null || Find("StartButton") != null, 80);
            if (Find("GuestAccount") != null) yield return Click("GuestAccount");
            else if (Find("ContinueAccount") != null) yield return Click("ContinueAccount");
            var preferences = SettingsStore.Current;
            preferences.Fullscreen = false; preferences.LargerText = false;
            preferences.CalloutCaptions = preferences.HighContrastHud = preferences.ReducedEffects = false;
            preferences.ToggleSprint = preferences.ToggleRestore = false; preferences.HudScale = 1; preferences.FirstPersonFov = 95;
            Screen.SetResolution(1280,960,FullScreenMode.Windowed);
            yield return WaitFor(() => Screen.width == 1280 && Screen.height == 960, 8);
            yield return EnterSettingsFromHome(); yield return Click("SettingsSection4");
            yield return AccessibleClick("LargerTextValue");
            if (!SettingsStore.Current.LargerText) throw new InvalidOperationException("The actual larger-text toggle did not apply.");
            yield return AccessibleClick("HudScaleValue");
            if (SettingsStore.Current.HudScale <= 1) throw new InvalidOperationException("HUD size slider did not accept the pointer.");
            yield return Shot("accessibility-settings-large-4x3");
            foreach (string name in new[] { "SprintControlValue", "RestoreControlValue" })
            { yield return AccessibleClick(name); yield return AccessibleClick("Option1"); }
            yield return AccessibleClick("FirstPersonFovValue");
            foreach (string name in new[] { "CalloutCaptionsValue", "HighContrastHudValue", "ReducedEffectsValue" })
                yield return AccessibleClick(name);
            preferences = SettingsStore.Current;
            if (!preferences.ToggleSprint || !preferences.ToggleRestore || !preferences.CalloutCaptions ||
                !preferences.HighContrastHud || !preferences.ReducedEffects || Mathf.Approximately(preferences.FirstPersonFov,95))
                throw new InvalidOperationException("A native accessibility control failed to update its preference.");
            yield return Shot("accessibility-settings-comfort-4x3");
            yield return Click("TumpSaveSettings");
            var view = UnityEngine.Object.FindFirstObjectByType<TumpSettingsView>();
            if (view.Session.Dirty) throw new InvalidOperationException("Save left accessibility changes staged.");
            Stage("native accessibility save and discard");
            yield return AccessibleClick("LargerTextValue");
            yield return Click("TumpSettingsBack"); yield return Click("DiscardAndBack");
            if (!SettingsStore.Current.LargerText) throw new InvalidOperationException("Discard lost the saved larger-text preference.");
            yield return Click("BackButton"); yield return WaitFor(() => GameObject.Find("OwnerPlayCanvas") != null);
            yield return Click("BackButton"); yield return WaitFor(() => GameObject.Find("OwnerHomeCanvas") != null);

            Stage("large owner HUD and muted announcer caption in native Hero play");
            yield return Click("StartButton"); yield return Click("HeroStrikeButton"); yield return Click("PracticeButton");
            yield return Click("PrimaryButton"); yield return StartReadyRound();
            var matchCanvas = GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            // Offline practice has no network chat. Exercise the same production
            // display locally without sending a message or claiming network coverage.
            var chat = LobbyChat.Attach(matchCanvas.transform,true); chat.PlaceBottomRight(38,232,540);
            typeof(LobbyChat).GetMethod("AddLocal",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .Invoke(chat,new object[]{"LOCAL UI REVIEW: a readable chat line."});
            SettingsStore.Current.LargerText = false; yield return null;
            SettingsStore.Current.LargerText = true; yield return null;
            if (((RectTransform)chat.transform).anchoredPosition.x >= 0)
                throw new InvalidOperationException("Rescaling match chat lost its actual right-hand anchor.");
            SettingsStore.Current.AnnouncerVolume = 0;
            GameServices.Voice.Play("count_go"); yield return null;
            var caption = GameObject.Find("OwnerMatchCanvas").GetComponent<CalloutCaption>();
            if (caption.VisibleText != "Begin!") throw new InvalidOperationException("Muted native announcer did not show its real caption.");
            yield return Shot("accessibility-owner-contrast-caption-4x3");
            SettingsStore.Current.HighContrastHud = false;
            Screen.SetResolution(1680,720,FullScreenMode.Windowed);
            yield return WaitFor(() => Screen.width == 1680 && Screen.height == 720, 8);
            yield return Shot("accessibility-owner-large-wide");

            Stage("larger native training card after local match-chat display check");
            bool guided = GameLaunch.GuidedTutorial;
            try
            {
                GameLaunch.GuidedTutorial = true;
                yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
                yield return WaitFor(() => UnityEngine.Object.FindFirstObjectByType<GuidedTrainingHud>() != null, 15);
                yield return Shot("accessibility-training-large-wide");
            }
            finally { GameLaunch.GuidedTutorial = guided; }
            // The spectator route already qualifies actual replay/window transitions.
            // Retain larger text so these are the changed overlay dimensions.
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            yield return SpectatorOnly();
            Stage("native accessibility options, save-discard and enlarged owner-training-spectator views verified");
        }
    }
}
