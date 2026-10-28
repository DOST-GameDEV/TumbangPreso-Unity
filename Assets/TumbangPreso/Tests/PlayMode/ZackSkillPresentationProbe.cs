using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class ZackSkillPresentationProbe
    {
        private bool _bots, _spectator, _pinned;
        private int _seat;
        private INetProvider _net;
        private CustomRules _rules;
        [UnitySetUp] public IEnumerator Before()
        {
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            _net = NetAuthority.Provider; _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _net;
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest]
        public IEnumerator LightningGeometryConnectsItsRequestedStartAndEnd()
        {
            var start = new Vector3(2, 8, 1); var end = new Vector3(-1, .1f, -.7f);
            var bolt = Abilities.HeroHazards.SpawnLightningBolt(start, end, Color.white, .4f);
            Assert.IsNotNull(bolt);
            var vertices = bolt.GetComponentsInChildren<MeshFilter>()
                .SelectMany(f => f.sharedMesh.vertices.Select(v => f.transform.TransformPoint(v))).ToArray();
            Assert.IsNotEmpty(vertices);
            float from = vertices.Min(v => Vector3.Distance(v, start));
            float to = vertices.Min(v => Vector3.Distance(v, end));
            File.WriteAllText("Logs/zack-lightning-endpoints.csv", FormattableString.Invariant(
                $"start_error,end_error,vertices\n{from},{to},{vertices.Length}\n"));
            Assert.Less(from, .15f, "The bolt ignores its requested sky endpoint.");
            Assert.Less(to, .15f, "The bolt misses its requested contact endpoint.");
            yield return new WaitForSeconds(.5f);
            Assert.True(bolt == null, "The strike remained after its authored duration.");
        }

        private static Net.WorldEffectSnapshot.Field Wire(bool overcharged, bool reeled = false)
        {
            Vector3 a = new Vector3(-2, 0, 0), b = new Vector3(2, 0, 0);
            return new Net.WorldEffectSnapshot.Field { Type = Net.WorldEffectSnapshot.Kind.LiveWire, EventId = 7, Owner = 1,
                Position = a, Forward = Vector3.right, Radius = 4, Duration = ZackRules.LiveWireLifeBound,
                Remaining = ZackRules.LiveWireLifeBound, SecondScale = overcharged ? 1 : 0, Split = reeled,
                FirstScale = reeled ? 2 : 0, Path = new[] { a, b } };
        }

        private static Net.WorldEffectSnapshot.Field Copy(float length)
            => new Net.WorldEffectSnapshot.Field { Type = Net.WorldEffectSnapshot.Kind.Afterimage, EventId = 9, Owner = 1,
                Position = Vector3.zero, Forward = Vector3.forward, Radius = length, Duration = ZackRules.AfterimageLife(length),
                Remaining = ZackRules.AfterimageLife(length), Path = System.Array.Empty<Vector3>() };

        [UnityTest]
        public IEnumerator TheFieldContractRefusesBadWiresAndCopies()
        {
            Assert.IsTrue(Net.WorldEffectSnapshot.Valid(Wire(false)));
            Assert.IsTrue(Net.WorldEffectSnapshot.Valid(Wire(true, reeled: true)));
            var longWire = Wire(false); longWire.Path = new[] { longWire.Position, longWire.Position + Vector3.right * 7 }; longWire.Radius = 7;
            Assert.IsFalse(Net.WorldEffectSnapshot.Valid(longWire), "A 7 m wire is past the 6 m rule.");
            var lying = Wire(false); lying.Radius = 3;
            Assert.IsFalse(Net.WorldEffectSnapshot.Valid(lying), "The length must match the posts.");
            // A clip saved under protocol 182/183 holds the old 10 s wire; it is read in its own meaning, never rejected.
            var old = Wire(false); old.Duration = 10; old.Remaining = 6; old.Split = true; old.FirstScale = 3;
            Assert.IsFalse(Net.WorldEffectSnapshot.Valid(old), "The old lifetime is not a live wire any more.");
            var translated = Abilities.ZackLiveWire.FromRecordedV16(old);
            Assert.IsTrue(Net.WorldEffectSnapshot.Valid(translated));
            Assert.AreEqual(4, translated.Duration - translated.Remaining, .001f, "Its recorded age is kept.");
            Assert.IsTrue(translated.Split, "An old snapped wire is drawn leaving.");
            var stale = Wire(false); stale.FirstScale = -1;
            Assert.IsFalse(Net.WorldEffectSnapshot.Valid(stale), "A negative dark time is refused.");
            Assert.IsTrue(Net.WorldEffectSnapshot.Valid(Copy(15)));
            Assert.IsTrue(Net.WorldEffectSnapshot.Valid(Copy(4)));
            var fast = Copy(15); fast.Duration = fast.Remaining = 1;
            Assert.IsFalse(Net.WorldEffectSnapshot.Valid(fast), "A copy's life is solved from its length, never sent free.");
            Assert.IsTrue(Net.WorldEffectSnapshot.UsesDynamicIdentity(Net.WorldEffectSnapshot.Kind.Afterimage));
            Assert.IsTrue(Net.WorldEffectSnapshot.UsesDynamicIdentity(Net.WorldEffectSnapshot.Kind.LiveWire));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheWireSagsAsTheRulesSayAndCarriesNoCollider()
        {
            var root = new GameObject("WireStage");
            try
            {
                var look = Visual.ZackLiveWireLook.Build(root.transform, Wire(true), renderOnly: true);
                look.StepTo(1.0f);
                yield return null;
                Assert.IsEmpty(root.GetComponentsInChildren<Collider>().Where(c => c.enabled), "The wire and its posts must never block.");
                var line = root.GetComponentsInChildren<LineRenderer>().First(l => l.name == "Wire");
                var middle = line.GetPosition(line.positionCount / 2);
                float ground = Visual.VfxShapes.GroundAt(Vector3.zero, 0);
                Assert.AreEqual(ZackRules.LiveWireSagHeight, middle.y - ground, .06f, "The drawn sag is not the rule's sag.");
                Assert.IsNotNull(root.GetComponentsInChildren<LineRenderer>().FirstOrDefault(l => l.name == "WireOvercharged"),
                    "Overcharged is doubled.");
                var anchor = root.GetComponentsInChildren<Transform>().First(t => t.name == "AnchorA");
                var surfaces = anchor.GetComponentsInChildren<Renderer>();
                Assert.Greater(surfaces.Length, 3, "The wire needs a mounted reel, cleat and socket, not a bare pole.");
                Assert.Less(surfaces.Max(r => r.bounds.max.y) - ground, .40f, "The complete anchor stays low on the court.");
                Assert.IsEmpty(Object.FindObjectsByType<Abilities.HazardVolume>(FindObjectsSortMode.None)
                    .Where(h => h.transform.IsChildOf(root.transform)), "A look registers no hazard: the field does.");
            }
            finally { Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator ReplaysDrawZacksFieldsAsPresentationOnly()
        {
            var stage = new GameObject("ReplayStage");
            try
            {
                foreach (var field in new[] { Wire(false), Wire(true, reeled: true), Copy(15), Copy(3) })
                {
                    using (var view = new CameraSystem.RecordedFieldView(stage.transform, field))
                    {
                        view.Sample(field, 0.5f); view.Visible(true);
                        yield return null;
                        Assert.IsEmpty(view.Root.GetComponentsInChildren<Collider>().Where(c => c.enabled));
                        Assert.IsEmpty(view.Root.GetComponentsInChildren<AudioSource>(), "Replay looks must not create live current emitters.");
                        Assert.IsEmpty(view.Root.GetComponentsInChildren<Abilities.ZackLiveWire>());
                        Assert.IsEmpty(view.Root.GetComponentsInChildren<Abilities.ZackAfterimage>());
                    }
                }
                Assert.IsEmpty(Abilities.ZackLiveWire.Active); Assert.IsEmpty(Abilities.ZackAfterimage.Active);
            }
            finally { Object.Destroy(stage); }
        }

        [UnityTest]
        public IEnumerator LiveWireCurrentFollowsSfxGainAndStopsWhenDark()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(ReadyGate.HudRevealSeconds + 3.6f);
            var root = new GameObject("Live wire audio lifecycle");
            float volume = Settings.SettingsStore.Current.SfxVolume;
            try
            {
                var look = Visual.ZackLiveWireLook.Build(root.transform, Wire(false));
                look.StepTo(1);
                yield return null;
                var source = root.GetComponentInChildren<AudioSource>();
                Assert.IsNotNull(source);
                Assert.IsTrue(source.isPlaying, "The live wire needs actual current playback.");
                Assert.IsTrue(source.loop);
                Assert.AreEqual(1, source.spatialBlend);
                Settings.SettingsStore.Current.SfxVolume = 0;
                look.StepTo(1.1f);
                Assert.Zero(source.volume, "The loop must obey the SFX slider.");
                Settings.SettingsStore.Current.SfxVolume = volume;
                look.StepTo(1.2f);
                Assert.Greater(source.volume, 0);
                look.Present(true, 0, false, Vector3.up);
                look.StepTo(1.3f);
                Assert.IsFalse(source.isPlaying, "A dark wire carries no current: its hum must stop at once.");
                look.Present(false, 1, false, Vector3.up);
                look.StepTo(1.4f);
                Assert.IsTrue(source.isPlaying, "Rearmed, the hum returns.");
            }
            finally { Settings.SettingsStore.Current.SfxVolume = volume; Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator InspectCardsShowTheWholeCurrentDescriptionInBothRoles()
        {
            var root = new GameObject("Zack inspect review");
            try
            {
                var canvas = OwnerUiLayout.Canvas(root.transform, "ZackInspectCanvas");
                var panel = AbilityInspectPanel.Create(canvas.transform);
                var kit = new Abilities.ZackHeroKit();
                foreach (bool defending in new[] { false, true })
                {
                    kit.SetRole(defending, null); panel.OpenForCapture(kit);
                    Canvas.ForceUpdateCanvases(); yield return null; Canvas.ForceUpdateCanvases();
                    string name = defending ? "Zack-inspect-live-wire" : "Zack-inspect-full-throttle";
                    yield return TumpUiCapture.Capture(name, canvas, 1600, 680, false);
                    var bodies = panel.GetComponentsInChildren<UnityEngine.UI.Text>().Where(t => t.name == "Body").ToArray();
                    Assert.AreEqual(3, bodies.Length);
                    CollectionAssert.AreEquivalent(new[] { kit.Skill1.LiveDescription, kit.Skill2.LiveDescription, kit.Ultimate.LiveDescription },
                        bodies.Select(t => t.text).ToArray());
                    foreach (var body in bodies)
                        Assert.LessOrEqual(body.preferredHeight, body.rectTransform.rect.height + 1, name + ": " + body.text);
                    foreach (var title in panel.GetComponentsInChildren<UnityEngine.UI.Text>().Where(t => t.name == "Name"))
                        Assert.LessOrEqual(title.preferredHeight, title.rectTransform.rect.height + 1, name + ": " + title.text);
                }
            }
            finally { Object.Destroy(root); }
        }
    }
}
