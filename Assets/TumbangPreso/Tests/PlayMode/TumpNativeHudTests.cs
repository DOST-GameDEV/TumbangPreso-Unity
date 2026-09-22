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
            foreach (var size in TumpUiCapture.HudViewports)
                yield return TumpUiCapture.Capture("CourtHud-Classic-" + size.x + "x" + size.y,
                    canvas, size.x, size.y, false, true, checkActionBounds: true);
            // VISUAL-1.4 budget: the permanent HUD in ordinary FPP play stays under about 8
            // percent of a 1920x1080 frame. Measured on the real canvas at that size.
            float share = 1; string detail = "";
            yield return TumpUiCapture.Capture("CourtHud-area-1920x1080", canvas, 1920, 1080, false, true,
                inspectViewport: () => share = TumpUiCapture.HudShare(canvas, out detail));
            Debug.Log($"[HudArea] Classic ordinary play: {share * 100:0.00}% of 1920x1080; {detail}");
            Assert.Less(share, .08f, "Permanent HUD exceeds the VISUAL-1.4 budget: " + detail);
            // The transient layer: pictogram feed, a score pop into a chip, the hit mark and a toast.
            var readout = Object.FindFirstObjectByType<TumpMatchReadout>();
            var lata = GameServices.Round.Lata;
            Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Block, 2, 0, lata.transform.position);
            Visual.MatchFlair.Play(Visual.MatchFlair.Kind.Tag, GameServices.Match.DefenderSlot, (GameServices.Match.DefenderSlot + 2) % 4, lata.transform.position);
            Visual.MatchFlair.Play(Visual.MatchFlair.Kind.LataDown, 1, -1, lata.transform.position);
            readout.ScorePopForShot(1, ScoreEvent.LataKnocked); readout.Hit(UiTheme.Offense); hud.ShowToast("TAGGED", 2);
            yield return null;
            foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720), TumpUiCapture.OwnerWindow, new Vector2Int(960, 540) })
            {
                readout.ScorePopForShot(1, ScoreEvent.LataKnocked); readout.Hit(UiTheme.Offense);
                yield return TumpUiCapture.Capture("CourtHud-moments-" + size.x + "x" + size.y, canvas, size.x, size.y, false, true, checkActionBounds: true);
            }
            // VISUAL-1.4 accessibility frames: HUD 120 percent and High contrast, same moment.
            var settings = Settings.SettingsStore.Current;
            float scaleBefore = settings.HudScale; bool contrastBefore = settings.HighContrastHud;
            try
            {
                settings.HudScale = 1.2f; settings.HighContrastHud = true; yield return null;
                foreach (var size in new[] { new Vector2Int(1920, 1080), TumpUiCapture.OwnerWindow, new Vector2Int(960, 540) })
                {
                    readout.ScorePopForShot(1, ScoreEvent.LataKnocked); readout.Hit(UiTheme.Offense);
                    yield return TumpUiCapture.Capture("CourtHud-moments-hud120-contrast-" + size.x + "x" + size.y, canvas, size.x, size.y, false, true, checkActionBounds: true);
                }
            }
            finally { settings.HudScale = scaleBefore; settings.HighContrastHud = contrastBefore; }
            yield return null;
            // VISUAL-1.6 reticle states, one frame each: idle cooldown, half charge with left
            // pektus, full charge with right pektus, refused (can protected), taya in reach.
            try
            {
                var states = new (string name, float charge, float spin, float cooldown, bool refused, bool reach)[]
                {
                    ("cooldown", 0, 0, .6f, false, false), ("charge-left", .6f, -.55f, 0, false, false),
                    ("charge-full-right", 1, .9f, 0, false, false), ("refused", .7f, 0, 0, true, false), ("taya-reach", 0, 0, 0, false, true),
                };
                foreach (var state in states)
                {
                    readout.ReticleForShot(state.charge, state.spin, state.cooldown, state.refused, state.reach); yield return null;
                    yield return TumpUiCapture.Capture("CourtHud-reticle-" + state.name + "-1280x720", canvas, 1280, 720, false, true);
                }
            }
            finally { readout.EndReticleShot(); }
            // VISUAL-1.1: an armed attacker inside the box sees the Defense-blue frame, and no sentence.
            var effects = Object.FindFirstObjectByType<TumpHudEffects>();
            var can = GameServices.Round.Lata;
            float protectedUntil = Time.unscaledTime + 3;
            while (can.IsProtected && Time.unscaledTime < protectedUntil) yield return null;
            if (!can.IsUpright) can.HostRestore();
            local.Teleport(can.transform.position + new Vector3(0, 0, -2.5f)); yield return null;
            float frameBy = Time.unscaledTime + 1;
            while (!effects.DangerFrameVisible && Time.unscaledTime < frameBy) yield return null;
            if (local.IsTaggable())
            {
                Assert.IsTrue(effects.DangerFrameVisible, "A taggable attacker must see the danger frame.");
                Assert.AreNotEqual("You can be tagged", canvas.GetComponentsInChildren<Text>().First(t => t.name == "ActionPrompt").text);
                foreach (var size in new[] { new Vector2Int(1920, 1080), TumpUiCapture.OwnerWindow })
                    yield return TumpUiCapture.Capture("CourtHud-danger-" + size.x + "x" + size.y, canvas, size.x, size.y, false, true);
            }
            else Debug.Log("[HudDanger] staged attacker was not taggable (no slipper in hand); frame capture skipped.");
            bool before = local.IsDefender; local.IsDefender = true; yield return null;
            Assert.AreEqual("Defender", canvas.GetComponentsInChildren<Text>().First(t => t.name == "LocalRole").text);
            local.IsDefender = before;
            local.ApplyFallRecovery(); yield return null;
            var prompt = canvas.GetComponentsInChildren<Text>().First(t => t.name == "ActionPrompt");
            Assert.That(prompt.text.ToLowerInvariant(), Does.Contain("get up").Or.Contain("getting up"));
            yield return TumpUiCapture.Capture("CourtHud-recovery", canvas, 960, 540, false, true, checkActionBounds: true);
            local.ClearTrip();
            hud.ShowToast("Slipper returning · 10.0s", 1); yield return null;
            Assert.IsTrue(canvas.GetComponentsInChildren<Text>().First(t => t.name == "MatchToast").enabled);
            var swap = Object.FindFirstObjectByType<RoleSwapCard>();
            swap.ShowForShot(2, (GameServices.Match.DefenderSlot + 1) % 4); yield return null;
            var intermission = GameObject.Find("OwnerRoundSwapCanvas").GetComponent<Canvas>();
            Assert.IsEmpty(intermission.GetComponentsInChildren<PaperSkin>(true));
            Assert.IsNotNull(intermission.GetComponentsInChildren<Image>().First(i=>i.name=="NextDefenderPortrait").sprite);
            foreach(var size in TumpUiCapture.HudViewports)
            {
                swap.ShowForShot(2,(GameServices.Match.DefenderSlot+1)%4);
                yield return TumpUiCapture.Capture("CourtBreak-"+size.x+"x"+size.y,intermission,size.x,size.y,false,true,checkActionBounds:true);
            }
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
            float share = 1; string detail = "";
            yield return TumpUiCapture.Capture("OwnerHud-Hero-v1", canvas, 1920, 1080, false, true,
                inspectViewport: () => share = TumpUiCapture.HudShare(canvas, out detail));
            Debug.Log($"[HudArea] Hero Strike ordinary play: {share * 100:0.00}% of 1920x1080; {detail}");
            Assert.Less(share, .08f, "Permanent Hero Strike HUD exceeds the VISUAL-1.4 budget: " + detail);
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
                foreach (var size in TumpUiCapture.HudViewports)
                    yield return TumpUiCapture.Capture("CourtHud-held-skills-" + size.x + "x" + size.y,
                        canvas, size.x, size.y, false, true, checkActionBounds: true);
            }
            finally { readout.CloseCapture(); }
            hud.EnterSpectatorMode(); yield return null;
            Assert.IsFalse(canvas.transform.Find("PowerSeals").gameObject.activeSelf);
            Assert.IsFalse(canvas.transform.Find("LocalState").gameObject.activeSelf);
            foreach (var size in TumpUiCapture.HudViewports)
                yield return TumpUiCapture.Capture("CourtHud-spectator-" + size.x + "x" + size.y,
                    canvas, size.x, size.y, false, true, checkActionBounds: true);
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
            Assert.AreEqual(8,GameServices.Match.TotalRounds,"Normal matches must start with the owner's eight-round default.");
            TumpUiCapture.StageHudReview(GameServices.Round.PlayerAt(GameLaunch.SoloSeat));
            yield return null;
        }
    }
}
