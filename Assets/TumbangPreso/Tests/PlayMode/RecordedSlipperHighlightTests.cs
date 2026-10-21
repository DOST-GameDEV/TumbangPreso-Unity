using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class RecordedSlipperHighlightTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator CurrentLandingHighlightCannotPaintHistoricalHeldSlipper() => HistoricalNonLoose(10);

        [UnityTest]
        public IEnumerator CurrentLandingHighlightCannotPaintHistoricalFlyingSlipper() => HistoricalNonLoose(10.5f);

        [UnityTest]
        public IEnumerator HistoricalLooseRestoresInitialHighlightAfterHeldAndFlightFrames() => HistoricalNonLoose(12, true);

        private IEnumerator HistoricalNonLoose(float recordedTime, bool looseControl = false)
        {
            int setting = SettingsStore.Current.SlipperHighlight;
            bool bots = GameLaunch.AllBots, spectator = GameLaunch.Spectator, pinned = UI.SceneFlow.RulesPinned;
            int seat = GameLaunch.SoloSeat; var rules = UI.SceneFlow.SelectedRules.Clone();
            GameObject owner = null;
            try
            {
                yield return MapRetrievalProbe.Load("Eskinita");
                owner = new GameObject("Recorded slipper highlight owner");
                SettingsStore.Current.SlipperHighlight = SlipperHighlights.Default;
                SettingsStore.RaiseSlipperHighlightChanged();
                var round = GameServices.Round; var actor = round.PlayerAt(1);
                var shoe = actor.GetComponent<Carrier>().Held; Assert.IsNotNull(shoe);
                Assert.AreEqual(1, shoe.OwnerSlot); Assert.AreEqual(SlipperState.Held, shoe.State);
                var shoeModel = MatchReplayArchive.PropModel(shoe.gameObject);
                var sourceRenderer = shoeModel.GetComponentsInChildren<Renderer>(true).First(r => r.GetComponent<VfxRenderTag>() == null);
                var block = new MaterialPropertyBlock(); sourceRenderer.GetPropertyBlock(block);
                Assert.AreEqual(Balance.OwnerRimStrength, block.GetFloat("_RimStrength"), .001f,
                    "The actual local ownership wiring must supply the ordinary owner rim before flight.");
                var shoeHistory = new MatchPoseHistory.Track(actor, shoeModel); shoeHistory.Record(10);
                var target = round.Lata.transform.position + new Vector3(1.5f, 0, 2);
                shoe.HostThrow(actor, target + Vector3.up * 1.5f, Vector3.zero);
                Assert.AreEqual(SlipperState.InFlight, shoe.State);
                shoeHistory.Record(10.5f); shoeHistory.Record(11);
                var shoePose = shoeHistory.Retain(10, 11); Assert.IsNotNull(shoePose);
                Assert.AreEqual((int)SlipperState.Held, shoePose.StateAt(10).State & 255);
                Assert.AreEqual((int)SlipperState.InFlight, shoePose.StateAt(10.5f).State & 255);
                float until = Time.time + 2;
                while (shoe.State != SlipperState.Loose && Time.time < until) yield return new WaitForFixedUpdate();
                Assert.AreEqual(SlipperState.Loose, shoe.State, "The actual throw must genuinely land before playback.");
                if (looseControl)
                {
                    shoeHistory.Record(12); shoePose = shoeHistory.Retain(10, 12);
                    Assert.AreEqual((int)SlipperState.Loose, shoePose.StateAt(12).State & 255);
                }
                sourceRenderer.GetPropertyBlock(block);
                Assert.AreEqual(Balance.LandedRimStrength, block.GetFloat("_RimStrength"), .001f);
                AssertColour(SlipperHighlights.ColourOf(SlipperHighlights.Default), block.GetColor("_RimColor"));
                RecordedObjectTrack Track(GameObject source, RecordedObjectKind kind, int seat, int skin, string person)
                {
                    var history = new MatchPoseHistory.Track(actor, source); history.Record(10); history.Record(looseControl ? 12 : 11);
                    return new RecordedObjectTrack { Kind = kind, Seat = seat, Skin = skin, Person = person,
                        VisualKey = MatchReplayArchive.VisualKey(source), Pose = history.Retain(10, 11) };
                }
                var body = Track(actor.GetComponent<CharacterVisual>().Model, RecordedObjectKind.Player, 1,
                    actor.CharacterIndex, Roster.PersonIdAt(actor.Mode, actor.CharacterIndex));
                var can = Track(MatchReplayArchive.PropModel(round.Lata.gameObject), RecordedObjectKind.Can, -1, round.Lata.SkinIndex, "");
                var prop = new RecordedObjectTrack { Kind = RecordedObjectKind.Slipper, Seat = shoe.SeatOfOrigin, Skin = shoe.SkinIndex,
                    VisualKey = MatchReplayArchive.VisualKey(shoeModel), Pose = shoePose };
                var clip = new RecordedMatchClip { MatchId = 1, Id = 1, Round = 1, Actor = 1, Subject = -1,
                    Mode = actor.Mode, Map = "Eskinita", Reason = "Recorded slipper highlight regression", Start = 10,
                    End = looseControl ? 12 : 11, Contact = 10.5f, Objects = new[] { body, can, prop } };
                using (var view = new RecordedWorldView(owner.transform, clip))
                {
                    Assert.IsTrue(view.Ready, view.UnavailableReason);
                    var items = (IEnumerable)typeof(RecordedWorldView).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
                    MatchPoseHistory.Copy copy = null;
                    foreach (var item in items)
                    {
                        var itemTrack = (RecordedObjectTrack)item.GetType().GetField("Track").GetValue(item);
                        if (itemTrack.Kind == RecordedObjectKind.Slipper)
                            copy = (MatchPoseHistory.Copy)item.GetType().GetField("Copy").GetValue(item);
                    }
                    Assert.IsNotNull(copy);
                    if (looseControl) { view.Draw(10, false); view.Draw(10.5f, false); }
                    view.Draw(recordedTime, false);
                    copy.Renderers[0].GetPropertyBlock(block);
                    Assert.AreEqual(looseControl ? Balance.LandedRimStrength : Balance.OwnerRimStrength, block.GetFloat("_RimStrength"), .001f,
                        looseControl ? "Initial copied Loose highlight was not restored after held and flight frames."
                            : "Current landed rim leaked onto historical " + (SlipperState)(shoePose.StateAt(recordedTime).State & 255) + " pose.");
                    AssertColour(looseControl ? SlipperHighlights.ColourOf(SlipperHighlights.Default) : ToonSkin.Ink, block.GetColor("_OutlineColor"));
                    sourceRenderer.GetPropertyBlock(block);
                    Assert.AreEqual(Balance.LandedRimStrength, block.GetFloat("_RimStrength"), .001f,
                        "Historical rendering must not change the live landing highlight.");
                }
            }
            finally
            {
                SettingsStore.Current.SlipperHighlight = setting; SettingsStore.RaiseSlipperHighlightChanged();
                GameLaunch.AllBots = bots; GameLaunch.Spectator = spectator; GameLaunch.SoloSeat = seat;
                UI.SceneFlow.AdoptRemoteRules(rules);
                if (pinned) UI.SceneFlow.PinSelectedRules(rules); else UI.SceneFlow.UnpinSelectedRules();
                if (owner != null) Object.DestroyImmediate(owner);
            }
        }

        private static void AssertColour(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, .001f); Assert.AreEqual(expected.g, actual.g, .001f);
            Assert.AreEqual(expected.b, actual.b, .001f); Assert.AreEqual(expected.a, actual.a, .001f);
        }
    }
}
