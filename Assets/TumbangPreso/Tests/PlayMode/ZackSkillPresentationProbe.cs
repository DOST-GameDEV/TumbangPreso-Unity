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

        [UnityTest, Timeout(60000)]
        public IEnumerator ReplicatedMagnetShotConsumesOnceWithoutEndingThunderstrike()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider = new SoloProvider();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var player in GameServices.Round.Players) { player.Intent.Clear(); player.Intent.Parked = true; }
            var caster = GameServices.Round.PlayerAt(1); caster.IsBot = true;
            caster.AbilitySystem.BindHero("zack"); caster.Intent.Parked = false;
            var carrier = caster.GetComponent<Carrier>(); var shoe = carrier.Held;
            var kit = (Abilities.ZackHeroKit)caster.AbilitySystem.Kit;
            var context = new Abilities.AbilityContext(caster, carrier, caster.GetComponent<CombatVerbs>());
            // Isolate the snapshot contract with both independent buffs present.
            kit.IsOverchargeThrowActive = true; kit.Ultimate.Activate(context);
            yield return new WaitForSeconds(.55f);
            Assert.True(kit.IsThunderstrikeActive);
            var at = caster.transform.position + Vector3.up + Vector3.forward;
            shoe.ApplySnapshotState(SlipperState.InFlight, null, at, Quaternion.identity,
                Vector3.forward * 8, 0, SlipperAffinity.ElectricZap, caster.PlayerSlot);
            Assert.False(kit.IsOverchargeThrowActive);
            Assert.True(kit.IsThunderstrikeActive, "A consumed Magnet shot ended the independent ultimate window.");
            var flight = shoe.transform.Find("ZapSlipperVfx"); Assert.IsNotNull(flight);
            kit.IsOverchargeThrowActive = true;
            shoe.ApplySnapshotState(SlipperState.InFlight, null, at, Quaternion.identity,
                Vector3.forward * 8, 0, SlipperAffinity.ElectricZap, caster.PlayerSlot);
            Assert.True(kit.IsOverchargeThrowActive, "A repeated flight snapshot consumed another charge.");
            Assert.AreSame(flight, shoe.transform.Find("ZapSlipperVfx"));
            shoe.ApplySnapshotState(SlipperState.Loose, null, at, Quaternion.identity,
                Vector3.zero, 0, SlipperAffinity.Normal, -1);
            yield return null;
            Assert.IsNull(shoe.transform.Find("ZapSlipperVfx"));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator MagnetRecallsItsShoeAndClearsTheChargeAfterTheRealThrow()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider = new SoloProvider();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var player in GameServices.Round.Players) { player.Intent.Clear(); player.Intent.Parked = true; }
            var caster = GameServices.Round.PlayerAt(1); caster.IsBot = true;
            caster.AbilitySystem.BindHero("zack"); caster.Teleport(new Vector3(0, .12f, -8));
            caster.Intent.Parked = false; caster.Intent.AimPoint = new Vector3(0, 1.2f, 7);
            var carrier = caster.GetComponent<Carrier>(); var shoe = carrier.Held;
            Assert.IsNotNull(shoe); Assert.True(shoe.HostDisarm());
            var from = new Vector3(1, 0, -4); from.y = Slipper.GroundY(from) + shoe.RestHeight;
            shoe.transform.position = from;
            caster.Intent.Set(Verb.Skill2, true);
            yield return new WaitForSeconds(.2f);
            caster.Intent.Set(Verb.Skill2, false);
            var kit = (Abilities.ZackHeroKit)caster.AbilitySystem.Kit;
            Assert.AreSame(shoe, carrier.Held, "The real Magnet press did not return the owned shoe.");
            Assert.True(kit.IsOverchargeThrowActive);
            Assert.IsNotNull(shoe.GetComponentInChildren<Visual.ZackMagnetCharge>());
            caster.Intent.Set(Verb.SpecialAbility, true);
            yield return new WaitForSeconds(.5f);
            caster.Intent.Set(Verb.SpecialAbility, false);
            bool chargedFlight = false; float released = Time.time;
            while (Time.time - released < .3f)
            {
                chargedFlight |= shoe.State == SlipperState.InFlight && shoe.Affinity == SlipperAffinity.ElectricZap;
                yield return null;
            }
            Assert.True(chargedFlight); Assert.False(kit.IsOverchargeThrowActive);
            Assert.IsNull(carrier.Held);
            Assert.IsNull(shoe.GetComponentInChildren<Visual.ZackMagnetCharge>());
            Assert.IsEmpty(Object.FindObjectsByType<Visual.MagnetRecallTrace>(FindObjectsSortMode.None));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator SprintIncludesTheInitialPatchInItsSixPatchLimit()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider = new SoloProvider();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var player in GameServices.Round.Players) { player.Intent.Clear(); player.Intent.Parked = true; }
            var caster = GameServices.Round.PlayerAt(1);
            caster.IsBot = true; caster.AbilitySystem.BindHero("zack");
            caster.Teleport(new Vector3(0, .12f, -8)); caster.Intent.Parked = false;
            float start = Time.time; int most = 0; bool accepted = false;
            while (Time.time - start < 2.75f)
            {
                caster.Intent.Set(Verb.Skill1, Time.time - start < .2f);
                accepted |= caster.AbilitySystem.LastAnswer(Abilities.HeroAbilitySystem.Slot.Skill1) == Abilities.HeroKit.CastOutcome.Cast;
                most = Mathf.Max(most, Object.FindObjectsByType<Abilities.HeroHazards.ShockTrailComponent>(FindObjectsSortMode.None)
                    .Count(field => field.OwnerSlot == caster.PlayerSlot));
                yield return null;
            }
            File.WriteAllText("Logs/zack-sprint-patch-count.csv", $"accepted,max_patches\n{accepted},{most}\n");
            Assert.True(accepted); Assert.GreaterOrEqual(most, 4);
            Assert.LessOrEqual(most, 6, "The initial patch escaped the live trail limit.");
        }
    }
}
