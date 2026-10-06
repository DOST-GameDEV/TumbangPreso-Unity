using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace TumbangPreso.PlayTests
{
    public sealed class RulesArrowHitTests
    {
        private CustomGameScreen screen;private CustomRules rules;private bool pinned,networked;
        private string savedWire;private int savedFormat,savedDifficulty;private CursorLockMode cursorLock;private bool cursorVisible;
        private bool botsEnabled;private Difficulty activeDifficulty;
        private string settingsPath;private byte[] settingsBytes,backupBytes,tempBytes;
        [UnitySetUp] public IEnumerator Before()
        {
            rules=SceneFlow.SelectedRules.Clone();pinned=SceneFlow.RulesPinned;networked=SceneFlow.Networked;
            savedWire=Settings.SettingsStore.Current.CustomRulesWire;savedFormat=Settings.SettingsStore.Current.MatchFormat;cursorLock=Cursor.lockState;cursorVisible=Cursor.visible;
            savedDifficulty=Settings.SettingsStore.Current.AiDifficulty;activeDifficulty=AIController.ActiveDifficulty;botsEnabled=AIController.BotsEnabled;
            settingsPath=Settings.SettingsStore.Path;
            settingsBytes=File.Exists(settingsPath)?File.ReadAllBytes(settingsPath):null;
            backupBytes=File.Exists(settingsPath+".bak")?File.ReadAllBytes(settingsPath+".bak"):null;
            tempBytes=File.Exists(settingsPath+".tmp")?File.ReadAllBytes(settingsPath+".tmp"):null;
            yield return PlayModeWorld.Reset();SceneFlow.Networked=false;
            var edit=CustomGameRules.Defaults(GameMode.HeroStrike);edit.Bots=0;SceneFlow.SetSelectedRules(edit);
            screen=CustomGameScreen.Ensure();screen.Open();yield return null;
            var canvas=screen.GetComponentsInChildren<Canvas>(true).FirstOrDefault();
            if(canvas==null)canvas=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c=>c.name=="OwnerCustomGameCanvas");
            var tab=canvas.GetComponentsInChildren<Button>(true).First(b=>b.name=="RoomRulesTab");tab.onClick.Invoke();
            Canvas.ForceUpdateCanvases();yield return null;
        }
        [UnityTearDown] public IEnumerator After()
        {
            if(screen!=null)Object.Destroy(screen.gameObject);yield return PlayModeWorld.Reset();
            SceneFlow.AdoptRemoteRules(rules);if(pinned)SceneFlow.PinSelectedRules(rules);else SceneFlow.UnpinSelectedRules();SceneFlow.Networked=networked;
            SceneFlow.SelectedRules.Password=rules.Password;
            Settings.SettingsStore.Current.CustomRulesWire=savedWire;Settings.SettingsStore.Current.MatchFormat=savedFormat;Settings.SettingsStore.Current.AiDifficulty=savedDifficulty;
            AIController.ApplyDifficulty((int)activeDifficulty);AIController.BotsEnabled=botsEnabled;
            Assert.AreEqual(settingsPath,Settings.SettingsStore.Path,"Fixture changed launch profile.");
            RestoreSettingsFile(settingsPath,settingsBytes);RestoreSettingsFile(settingsPath+".bak",backupBytes);RestoreSettingsFile(settingsPath+".tmp",tempBytes);
            Cursor.lockState=cursorLock;Cursor.visible=cursorVisible;
        }
        private static void RestoreSettingsFile(string path,byte[] before)
        {
            if(before!=null)File.WriteAllBytes(path,before);else if(File.Exists(path))File.Delete(path);
        }
        [UnityTest]public IEnumerator VisibleNextGlyphReceivesPointerAndAdvancesBots()
        {
            yield return ClickVisibleGlyph("BotsNext");Assert.Greater(SceneFlow.SelectedRules.Bots,0);Assert.AreEqual(0,SceneFlow.SelectedRules.BotDifficulty);
        }
        [UnityTest]public IEnumerator VisiblePreviousGlyphStillReceivesPointerAndWrapsBots()
        {
            yield return ClickVisibleGlyph("BotsPrevious");Assert.Greater(SceneFlow.SelectedRules.Bots,0);Assert.AreEqual(2,SceneFlow.SelectedRules.BotDifficulty);
        }
        private IEnumerator ClickVisibleGlyph(string name)
        {
            var canvas=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c=>c.name=="OwnerCustomGameCanvas");
            var button=canvas.GetComponentsInChildren<Button>(true).First(b=>b.name==name);Assert.IsTrue(button.interactable);
            var glyph=button.GetComponentInChildren<OwnerUiGlyph>();var rect=glyph.rectTransform;
            var point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center));
            var data=new PointerEventData(EventSystem.current){position=point};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Assert.IsNotEmpty(hits,"No raycast at the drawn arrow centre.");var target=hits[0].gameObject.GetComponentInParent<Button>();
            Assert.AreSame(button,target,"The visible glyph centre is outside the intended button's hit area.");
            ExecuteEvents.Execute(target.gameObject,data,ExecuteEvents.pointerClickHandler);yield return null;
        }
    }
}
