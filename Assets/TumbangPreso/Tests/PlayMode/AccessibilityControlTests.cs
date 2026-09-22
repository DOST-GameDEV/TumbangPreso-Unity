using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.InputLayer;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class AccessibilityControlTests
    {
        private string _settings;
        private bool _bots, _spectator, _pinned, _touch, _forceTouch;
        private int _seat;
        private CustomRules _rules;
        [UnitySetUp] public IEnumerator Before()
        {
            _settings = JsonUtility.ToJson(SettingsStore.Current);
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned; _touch = TouchInput.Active;
            _forceTouch = TouchHud.ForceVisible; TouchHud.ForceVisible = true;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            TouchInput.ReleaseAll(); TouchInput.Active = _touch;
            TouchHud.ForceVisible = _forceTouch;
            SettingsStore.Restore(JsonUtility.FromJson<GameSettings>(_settings));
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules); if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }
        private static IEnumerator Start(int seat, GameMode mode = GameMode.Classic)
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, mode);
            GameServices.Round.BeginRound();
            var switcher = Object.FindFirstObjectByType<DebugPlayerSwitcher>(FindObjectsInactive.Include);
            Assert.IsNotNull(switcher);
            typeof(DebugPlayerSwitcher).GetMethod("Assign", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(switcher, new object[] { seat });
            Assert.IsNotNull(GameServices.Round.PlayerAt(seat).GetComponent<PlayerInputReader>(), "A claimed bot seat must have a real local input reader.");
            foreach (var brain in Object.FindObjectsByType<AIController>()) brain.enabled = false;
            foreach (var actor in GameServices.Round.Players) actor.Intent.Parked = actor.PlayerSlot != seat;
            TouchInput.ReleaseAll(); TouchInput.Active = true;
            yield return null;
        }

        [UnityTest] public IEnumerator SprintToggleSurvivesReleaseAndClearsAtMenuBoundary()
        {
            SettingsStore.Current.ToggleSprint = true;
            yield return Start(1);
            var actor = GameServices.Round.PlayerAt(1); var reader = actor.GetComponent<PlayerInputReader>();
            Assert.IsTrue(reader.enabled);
            TouchInput.Set(Verb.Sprint, true); yield return null;
            TouchInput.Set(Verb.Sprint, false); yield return null;
            Assert.IsTrue(actor.Intent.Pressed(Verb.Sprint), "The physical release cancelled toggle sprint.");
            TouchInput.Set(Verb.Sprint, true); yield return null;
            Assert.IsFalse(actor.Intent.Pressed(Verb.Sprint));
            TouchInput.Set(Verb.Sprint, false); yield return null;
            TouchInput.Set(Verb.Sprint, true); yield return null;
            reader.DiscardMenuButtonsUntilRelease(); yield return null;
            Assert.IsFalse(actor.Intent.Pressed(Verb.Sprint), "Closing a menu resumed a latched sprint.");
            TouchInput.Set(Verb.Sprint, false); yield return null;
            SettingsStore.Current.ToggleSprint = false;
            TouchInput.Set(Verb.Sprint, true); yield return null;
            Assert.IsTrue(actor.Intent.Pressed(Verb.Sprint));
            TouchInput.Set(Verb.Sprint, false); yield return null;
            Assert.IsFalse(actor.Intent.Pressed(Verb.Sprint), "Original hold mode no longer follows release.");
        }

        [UnityTest] public IEnumerator ToggleRestoreCanBeCancelledThenFinishWithoutHolding()
        {
            SettingsStore.Current.ToggleRestore = true;
            yield return Start(0);
            var actor = GameServices.Round.PlayerAt(0); var carrier = actor.GetComponent<Carrier>();
            var lata = GameServices.Round.Lata; Assert.IsTrue(actor.IsDefender);
            yield return new WaitForSeconds(lata.ProtectionLeft + .05f);
            lata.HostKnockDown(1); yield return new WaitForSeconds(.2f);
            Assert.IsFalse(lata.IsUpright, "The fixture must knock down the can after its real restore protection.");
            actor.Teleport(lata.transform.position + Vector3.back * .6f); yield return null;
            Assert.IsTrue(carrier.HasResetTarget);
            Assert.IsTrue(actor.CanAct(), $"Restore setup: active={actor.RoundActive}, stun={actor.IsStunned}, blocked={PresentationClock.BlocksInput}");
            TouchInput.Set(Verb.Grab, true); yield return null;
            Assert.IsTrue(actor.Intent.Pressed(Verb.Grab), $"Restore press lost: touch={TouchInput.Pressed(Verb.Grab)}, reader={actor.GetComponent<PlayerInputReader>().enabled}, parked={actor.Intent.Parked}, target={carrier.HasResetTarget}");
            TouchInput.Set(Verb.Grab, false); yield return new WaitForSeconds(.25f);
            Assert.Greater(carrier.ChannelRatio, 0, "The released button did not keep the real channel running.");
            TouchInput.Set(Verb.Grab, true); yield return null; yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, carrier.ChannelRatio, .001f, "The second press did not cancel.");
            TouchInput.Set(Verb.Grab, false); yield return null;
            TouchInput.Set(Verb.Grab, true); yield return null;
            TouchInput.Set(Verb.Grab, false); yield return new WaitForSeconds(lata.ResetChannelTime + .3f);
            Assert.IsTrue(lata.IsUpright, "A toggled restore never reached the actual can outcome.");
            Assert.IsFalse(actor.Intent.Pressed(Verb.Grab), "Restore stayed latched after the can was upright.");
        }

        [UnityTest] public IEnumerator FovPreferenceMovesTheLiveFirstPersonLensAndDiscardRestoresIt()
        {
            SettingsStore.Current.FirstPersonFov = 95;
            yield return Start(1);
            var camera = Camera.main; Assert.IsNotNull(camera);
            Assert.IsTrue(camera.GetComponent<CameraRig>().IsLocalFpp);
            using (var session = new TumpSettingsSession())
            {
                SettingsStore.Current.FirstPersonFov = 110; session.Preview();
                yield return new WaitForSecondsRealtime(.65f);
                Assert.AreEqual(110, camera.fieldOfView, .8f);
                session.Discard(); yield return new WaitForSecondsRealtime(.65f);
                Assert.AreEqual(95, camera.fieldOfView, .8f);
            }
        }

        [UnityTest] public IEnumerator LargerSettingsReflowAndDiscardRestoresOriginalRows()
        {
            SettingsStore.Current.LargerText = false;
            var root = new GameObject("AccessibilitySettingsReview");
            var view = root.AddComponent<TumpSettingsView>(); view.Open(root.transform, () => {}, null, null); view.ShowSection(4);
            var canvas = GameObject.Find("OwnerSettingsCanvas").GetComponent<Canvas>();
            yield return TumpUiCapture.Capture("accessibility-settings-normal-wide", canvas, 1920, 1080, false, checkActionBounds: true);
            float originalHeight = GameObject.Find("LargerText").GetComponent<LayoutElement>().preferredHeight;
            var toggle = GameObject.Find("LargerTextValue").GetComponent<Toggle>(); toggle.isOn = true;
            yield return null; Canvas.ForceUpdateCanvases();
            var row = GameObject.Find("LargerText");
            var label = row.transform.Find("Label").GetComponent<Text>();
            var control = (RectTransform)row.transform.Find("Control");
            Assert.Greater(label.fontSize, 31);
            Assert.Less(control.anchoredPosition.y, -70, "The large control still shares the label's line.");
            yield return TumpUiCapture.Capture("accessibility-settings-large-wide", canvas, 1920, 1080, false, checkActionBounds: true);
            yield return TumpUiCapture.Capture("accessibility-settings-large-4x3", canvas, 1280, 960, false, checkActionBounds: true);
            canvas.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition = 0;
            yield return TumpUiCapture.Capture("accessibility-settings-large-bottom-4x3", canvas, 1280, 960, false, checkActionBounds: true);
            view.Session.Discard(); yield return null;
            Assert.AreEqual(31, label.fontSize); Assert.AreEqual(0, control.anchoredPosition.y);
            Assert.AreEqual(originalHeight, row.GetComponent<LayoutElement>().preferredHeight, "Discard must restore the original row, including the existing touch-target floor.");
        }

        [UnityTest] public IEnumerator EnlargedHeroHudPreservesScreenMarginsAndControlSeparation()
        {
            SettingsStore.Current.HudScale = 1.2f; SettingsStore.Current.LargerText = true;
            yield return Start(1, GameMode.HeroStrike);
            GameServices.Match.StartMatch(); yield return null;
            var actor = GameServices.Round.PlayerAt(1); TumpUiCapture.StageHudReview(actor);
            var canvas = GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            foreach (var size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,960) })
                yield return TumpUiCapture.Capture("accessibility-hud-large-"+size.x+"x"+size.y, canvas, size.x, size.y, false, true,
                    inspectViewport: () => AssertHudBounds(canvas));
        }
        private static void AssertHudBounds(Canvas canvas)
        {
            var root = (RectTransform)canvas.transform;
            foreach (string name in new[] { "MatchScores", "CanReadout", "LocalState", "ContextualAction", "PowerSeals", "SpectatorReadout" })
            {
                var rect = (RectTransform)root.Find(name); var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    var point = root.InverseTransformPoint(corner);
                    Assert.That(point.x, Is.InRange(root.rect.xMin - 1, root.rect.xMax + 1), name+" horizontal crop");
                    Assert.That(point.y, Is.InRange(root.rect.yMin - 1, root.rect.yMax + 1), name+" vertical crop");
                }
            }
        }

        [UnityTest] public IEnumerator MutedAnnouncerStillCaptionsDeliveredCalloutsAndDisablingHidesThem()
        {
            SettingsStore.Current.CalloutCaptions = true; SettingsStore.Current.AnnouncerVolume = 0;
            yield return Start(1, GameMode.HeroStrike);
            GameServices.Match.StartMatch(); yield return null;
            var caption = GameObject.Find("OwnerMatchCanvas").GetComponent<CalloutCaption>();
            var voice = GameServices.Voice; Assert.IsNotNull(voice);
            voice.Play("clock_10"); yield return null;
            Assert.AreEqual("Ten seconds left!", caption.VisibleText);
            var voices = (AudioSource[])typeof(Audio.VoiceDirector).GetField("_voices", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(voice);
            Assert.IsFalse(voices.Any(source => source.isPlaying));
            SettingsStore.Current.HighContrastHud = true; SettingsStore.Current.LargerText = true;
            yield return null; yield return null;
            var canvas = caption.GetComponent<Canvas>();
            yield return TumpUiCapture.Capture("accessibility-contrast-caption-wide", canvas, 1920,1080,false,true);
            voice.Play("count_go"); yield return null;
            Assert.AreEqual("Begin!", caption.VisibleText);
            SettingsStore.Current.CalloutCaptions = false; yield return null;
            Assert.AreEqual("", caption.VisibleText);
            SettingsStore.Current.HighContrastHud = false; yield return null;
            Assert.IsFalse(canvas.GetComponentsInChildren<Image>().Any(image => image.name.EndsWith("ContrastBacking") && image.enabled));
        }

        [UnityTest] public IEnumerator ReducedParticlesKeepTheStatusEmitterAndRestoreItsOriginalDensity()
        {
            SettingsStore.Current.ReducedEffects = false;
            var host = new GameObject("AccessibilityAuraReview");
            var aura = Visual.AbilityVfx.AttachAura(host.transform, Visual.AbilityVfx.Aura.ElectricSpark, 10);
            var particles = aura.GetComponent<ParticleSystem>(); float original = particles.emission.rateOverTimeMultiplier;
            Assert.Greater(original, 0);
            SettingsStore.Current.ReducedEffects = true; yield return null;
            Assert.Greater(particles.emission.rateOverTimeMultiplier, 0, "The visible status cue was removed.");
            Assert.Less(particles.emission.rateOverTimeMultiplier, original);
            Assert.IsTrue(particles.isPlaying);
            SettingsStore.Current.ReducedEffects = false; yield return null;
            Assert.AreEqual(original, particles.emission.rateOverTimeMultiplier);
            Object.Destroy(host);
        }

        [UnityTest] public IEnumerator CurrentRoleMarkersShareTheSameEightMetreView()
        {
            yield return Start(1, GameMode.HeroStrike); GameServices.Match.StartMatch(); yield return null;
            var defender = GameServices.Round.PlayerAt(GameServices.Match.DefenderSlot);
            var attacker = GameServices.Round.PlayerAt(2);
            Assert.AreNotSame(defender, attacker);
            foreach (var actor in GameServices.Round.Players)
            { actor.Intent.Parked = true; actor.Teleport(new Vector3(actor.PlayerSlot * 2 - 3, actor.transform.position.y, 6)); }
            float floor = defender.transform.position.y;
            defender.Teleport(new Vector3(-1.3f, floor, 0)); attacker.Teleport(new Vector3(1.3f, floor, 0));
            yield return null;
            Assert.IsTrue(defender.GetComponentInChildren<Visual.CharacterNameplate>().transform.Find("NameplateRing").gameObject.activeInHierarchy);
            Assert.IsTrue(attacker.GetComponentInChildren<Visual.CharacterNameplate>().transform.Find("NameplateRing").gameObject.activeInHierarchy);
            var cameraObject = new GameObject("AccessibilityRoleWitness"); var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 50; camera.nearClipPlane = .05f; camera.farClipPlane = 400;
            camera.transform.position = new Vector3(0, floor + 4.2f, -8); camera.transform.LookAt(new Vector3(0, floor + .05f, 0));
            typeof(GameplayShots).GetMethod("Grade", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { camera });
            var live = Camera.main;
            try
            {
                live.tag = "Untagged"; camera.tag = "MainCamera"; yield return null;
                yield return GameplayShots.Render(camera, "accessibility-role-markers-v3", false, "Logs/accessibility-roles", observedSubject: attacker);
            }
            finally { live.tag = "MainCamera"; Object.Destroy(cameraObject); }
        }

        [UnityTest] public IEnumerator LargeAbilityReferencesKeepAllSevenHeroesDescriptionsVisible()
        {
            SettingsStore.Current.LargerText = true;
            yield return Start(1, GameMode.HeroStrike);
            var root = new GameObject("AccessibilityReferenceReview");
            var canvas = OwnerUiLayout.Canvas(root.transform, "AccessibilityReferenceCanvas");
            var powers = root.AddComponent<TumpPowerReadout>(); powers.Build(canvas.transform);
            canvas.transform.Find("PowerSeals").gameObject.SetActive(false);
            foreach (var person in Roster.GetPeople(GameMode.HeroStrike))
            {
                var kit = Abilities.HeroAbilitySystem.CreateKitFor(person.Id);
                powers.OpenForCapture(kit); yield return null;
                var reference = canvas.transform.Find("HeldPowerReference");
                Assert.Greater(reference.GetComponentsInChildren<Text>().First(text => text.name == "PowerDescription0").fontSize, 28);
                var expected = new[] { kit.Skill1.EffectiveName, kit.Skill2.EffectiveName, kit.Ultimate.EffectiveName };
                yield return TumpUiCapture.Capture("accessibility-reference-v4-"+person.Id+"-4x3", canvas,1280,960,false,true,
                    inspectViewport: () => CollectionAssert.AreEquivalent(expected,
                        reference.GetComponentsInChildren<Text>().Where(text => text.name.StartsWith("PowerName")).Select(text => text.text).ToArray()));
            }
            powers.CloseCapture(); Object.Destroy(root);
        }
    }
}
