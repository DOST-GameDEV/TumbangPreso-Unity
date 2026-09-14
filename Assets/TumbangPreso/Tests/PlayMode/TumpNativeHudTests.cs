using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class TumpNativeHudTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest]
        public IEnumerator ClassicHudReflectsTheRealRoundRoleAndRecoveryState()
        {
            yield return Open(GameMode.Classic);
            var hud = Hud.Instance; var canvas = GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            Assert.IsTrue(hud.NativePresentation); Assert.IsEmpty(canvas.GetComponentsInChildren<PaperSkin>(true));
            Assert.AreEqual(4, canvas.GetComponentsInChildren<RectTransform>().Count(r => r.name.StartsWith("ScoreRow")));
            Assert.AreEqual(4, canvas.GetComponentsInChildren<Image>().Count(i => i.name == "PlayerPortrait" && i.enabled && i.sprite != null),
                "A sprite reference alone is not a visible portrait.");
            Assert.IsFalse(canvas.transform.Find("PowerSeals").gameObject.activeSelf, "Classic has no hero power UI.");
            var local = Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).First(m => m.PlayerSlot == GameLaunch.SoloSeat);
            yield return TumpUiCapture.Capture("OwnerHud-Classic-v1", canvas, 1920, 1080, false, true);
            bool before = local.IsDefender; local.IsDefender = true; yield return null;
            Assert.AreEqual("Defender", canvas.GetComponentsInChildren<Text>().First(t => t.name == "LocalRole").text);
            local.IsDefender = before;
            local.ApplyFallRecovery(); yield return null;
            var prompt = canvas.GetComponentsInChildren<Text>().First(t => t.name == "ActionPrompt");
            Assert.That(prompt.text.ToLowerInvariant(), Does.Contain("get up").Or.Contain("getting up"));
            yield return TumpUiCapture.Capture("OwnerHud-recovery-v1", canvas, 1280, 720, false, true);
            local.ClearTrip();
            hud.ShowToast("Slipper returning · 10.0s", 1); yield return null;
            Assert.IsTrue(canvas.GetComponentsInChildren<Text>().First(t => t.name == "MatchToast").enabled);
            var swap = Object.FindFirstObjectByType<RoleSwapCard>();
            swap.ShowForShot(2, (GameServices.Match.DefenderSlot + 1) % 4); yield return null;
            var intermission = GameObject.Find("OwnerRoundSwapCanvas").GetComponent<Canvas>();
            Assert.IsEmpty(intermission.GetComponentsInChildren<PaperSkin>(true));
            Assert.IsNotNull(intermission.GetComponentsInChildren<Image>().First(i=>i.name=="NextDefenderPortrait").sprite);
            yield return TumpUiCapture.Capture("OwnerRoundSwap-v1", intermission, 1920, 1080,false);
            swap.DismissAndPractice(); Assert.IsFalse(intermission.gameObject.activeSelf);
        }
        [UnityTest]
        public IEnumerator HeroPowerDetailsAndSpectatorCleanFeedKeepTheirLiveContracts()
        {
            yield return Open(GameMode.HeroStrike);
            var hud = Hud.Instance; var canvas = GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            var local = Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).First(m => m.PlayerSlot == GameLaunch.SoloSeat);
            Assert.IsTrue(canvas.transform.Find("PowerSeals").gameObject.activeSelf);
            Assert.AreEqual(3, canvas.GetComponentsInChildren<OwnerAbilitySeal>().Length);
            yield return TumpUiCapture.Capture("OwnerHud-Hero-v1", canvas, 1920, 1080, false, true);
            var readout = Object.FindFirstObjectByType<TumpPowerReadout>();
            var kit = local.GetComponent<Abilities.HeroAbilitySystem>().Kit;
            try
            {
                readout.OpenForCapture(kit); yield return null;
                var names = canvas.GetComponentsInChildren<Text>().Where(t => t.name.StartsWith("PowerName")).Select(t => t.text).ToArray();
                CollectionAssert.AreEquivalent(new[] { kit.Skill1.EffectiveName, kit.Skill2.EffectiveName, kit.Ultimate.EffectiveName }, names);
                float duration=kit.Skill1.Duration;
                var setter=typeof(Abilities.HeroAbility).GetProperty("Duration").GetSetMethod(true);
                try
                {
                    setter.Invoke(kit.Skill1,new object[]{duration+1});yield return null;
                    Assert.That(canvas.GetComponentsInChildren<Text>().First(t=>t.name=="PowerTiming0").text,
                        Does.Contain((duration+1).ToString("0.#")+"s duration"),"A live kit must refresh changed timing details.");
                }
                finally{setter.Invoke(kit.Skill1,new object[]{duration});}
                yield return null;
                yield return TumpUiCapture.Capture("OwnerHud-held-skills-v1", canvas, 1280, 720, false, true);
            }
            finally { readout.CloseCapture(); }
            hud.EnterSpectatorMode(); yield return null;
            Assert.IsFalse(canvas.transform.Find("PowerSeals").gameObject.activeSelf);
            Assert.IsFalse(canvas.transform.Find("LocalState").gameObject.activeSelf);
            yield return TumpUiCapture.Capture("OwnerHud-spectator-v1", canvas, 1280, 960, false, true);
            hud.SetCleanFeed(true); Assert.IsFalse(canvas.gameObject.activeSelf);
            hud.SetCleanFeed(false); Assert.IsTrue(canvas.gameObject.activeSelf);
            hud.ExitSpectatorMode(); yield return null;
            Assert.IsTrue(canvas.transform.Find("LocalState").gameObject.activeSelf);
        }
        private static IEnumerator Open(GameMode mode)
        {
            SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(mode));
            yield return SceneManager.LoadSceneAsync("Eskinita"); yield return new WaitForSecondsRealtime(.4f);
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSecondsRealtime(3.7f);
            Assert.IsNotNull(Hud.Instance);
        }
    }
}
