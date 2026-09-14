using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class SeanSkillTimingProbe
    {
        private bool _bots, _spectator, _pinned;
        private int _seat;
        private INetProvider _net;
        private CustomRules _rules;
        private CharacterMotor _caster;

        [UnitySetUp] public IEnumerator Before()
        {
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            _net = NetAuthority.Provider; _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider = new SoloProvider();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) reader.enabled = false;
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>(FindObjectsSortMode.None)) switcher.enabled = false;
            foreach (var player in GameServices.Round.Players)
            {
                player.Intent.Clear(); player.Intent.Parked = true;
                player.Teleport(new Vector3(6, .12f, 7 + player.PlayerSlot));
            }
            _caster = GameServices.Round.PlayerAt(1);
            _caster.IsBot = true;
            _caster.Teleport(new Vector3(0, .12f, -8)); _caster.transform.rotation = Quaternion.identity;
            _caster.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, "sean");
            _caster.AbilitySystem.BindHero("sean"); _caster.AbilitySystem.Kit.AddUltimateCharge(100);
            _caster.Intent.Parked = false;
            _caster.Intent.AimPoint = new Vector3(0, 1, 7);
            yield return new WaitForSeconds(.2f);
        }

        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _net;
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator IgnitionLeavesTheHandWhenTheEmpoweredThrowIsReleased()
        {
            var carrier = _caster.GetComponent<Carrier>(); var shoe = carrier.Held;
            Assert.IsNotNull(shoe);
            _caster.Intent.Set(Verb.Skill2, true);
            yield return new WaitForSeconds(.2f);
            _caster.Intent.Set(Verb.Skill2, false);
            var kit = (SeanHeroKit)_caster.AbilitySystem.Kit;
            Assert.True(kit.IsIgnitionCannonActive, "The real skill press did not load Ignition.");
            Assert.IsNotNull(shoe.GetComponentInChildren<Visual.SeanIgnitionVisual>(),
                "The loaded fire is missing from the actual held slipper.");
            _caster.Intent.Set(Verb.SpecialAbility, true);
            yield return new WaitForSeconds(.5f);
            _caster.Intent.Set(Verb.SpecialAbility, false);
            bool fireFlight = false;
            float start = Time.time;
            while (Time.time - start < .25f)
            {
                fireFlight |= shoe.State == SlipperState.InFlight && shoe.Affinity == SlipperAffinity.FireExplosive;
                yield return null;
            }
            var stale = _caster.GetComponentsInChildren<ParticleSystem>();
            int liveHandEmitters = 0;
            foreach (var ps in stale)
                if (ps.name == "Vfx_HandAura_FireEmber" && ps.isEmitting) liveHandEmitters++;
            File.WriteAllText("Logs/sean-ignition-release.csv", FormattableString.Invariant(
                $"fire_flight,charged,held,live_hand_emitters\n{fireFlight},{kit.IsIgnitionCannonActive},{carrier.Held != null},{liveHandEmitters}\n"));
            Assert.True(fireFlight, "The actual released slipper never entered empowered flight.");
            Assert.IsNull(carrier.Held);
            Assert.False(kit.IsIgnitionCannonActive);
            Assert.Zero(liveHandEmitters, "Ignition is still emitting on the empty hand after consumption.");
            Assert.IsNull(shoe.GetComponentInChildren<Visual.SeanIgnitionVisual>(),
                "The carried ember survived its empowered release.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator ReplicatedFireReleaseConsumesOnceAndCleansItsFlightEffect()
        {
            var shoe = _caster.GetComponent<Carrier>().Held;
            var kit = (SeanHeroKit)_caster.AbilitySystem.Kit;
            _caster.Intent.Set(Verb.Skill2, true);
            yield return new WaitForSeconds(.2f);
            _caster.Intent.Set(Verb.Skill2, false);
            Assert.True(kit.IsIgnitionCannonActive);
            Vector3 at = new Vector3(0, 1.2f, -6);
            shoe.ApplySnapshotState(SlipperState.InFlight, null, at, Quaternion.identity,
                Vector3.forward * 8, 0, SlipperAffinity.FireExplosive, _caster.PlayerSlot);
            Assert.False(kit.IsIgnitionCannonActive, "The accepted remote fire throw retained its carried charge.");
            var flight = shoe.transform.Find("FireSlipperVfx"); Assert.IsNotNull(flight);
            // A repeated flight snapshot cannot consume a subsequently loaded charge.
            kit.IsIgnitionCannonActive = true;
            shoe.ApplySnapshotState(SlipperState.InFlight, null, at, Quaternion.identity,
                Vector3.forward * 8, 0, SlipperAffinity.FireExplosive, _caster.PlayerSlot);
            Assert.True(kit.IsIgnitionCannonActive);
            Assert.AreSame(flight, shoe.transform.Find("FireSlipperVfx"));
            shoe.ApplySnapshotState(SlipperState.Loose, null, at, Quaternion.identity,
                Vector3.zero, 0, SlipperAffinity.Normal, -1);
            yield return null;
            Assert.IsNull(shoe.transform.Find("FireSlipperVfx"));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator IgnitionCleansUpOnExpiryRefusalAndHeroReplacement()
        {
            var shoe = _caster.GetComponent<Carrier>().Held;
            var kit = (SeanHeroKit)_caster.AbilitySystem.Kit;
            var context = new AbilityContext(_caster, _caster.GetComponent<Carrier>(), _caster.GetComponent<CombatVerbs>());
            for (int scenario = 0; scenario < 3; scenario++)
            {
                kit.Skill2.RefillForSandbox();
                _caster.Intent.Set(Verb.Skill2, true);
                yield return new WaitForSeconds(.2f);
                _caster.Intent.Set(Verb.Skill2, false);
                var visual = shoe.GetComponentInChildren<Visual.SeanIgnitionVisual>();
                Assert.IsNotNull(visual, "The loaded visual never appeared.");
                var source = shoe.GetComponentInChildren<MeshFilter>().GetComponent<Renderer>();
                foreach (var part in visual.GetComponentsInChildren<Renderer>())
                    Assert.AreEqual(source.shadowCastingMode, part.shadowCastingMode,
                        "A hidden body slipper exposed its effect in first person.");
                if (scenario == 0) yield return new WaitForSeconds(kit.Skill2.Duration + .1f);
                else if (scenario == 1) kit.Skill2.RollBackPredictedCast(context);
                else _caster.AbilitySystem.BindHero("zack");
                yield return null; yield return null;
                Assert.IsNull(shoe.GetComponentInChildren<Visual.SeanIgnitionVisual>(),
                    "The ember survived expiry, refusal or hero replacement: " + scenario);
                // BindHero replaces the kit instance. Inspect the live kit after
                // replacement, not an obsolete instance retained only by this test.
                if (scenario < 2) Assert.False(kit.IsIgnitionCannonActive);
                else Assert.IsInstanceOf<ZackHeroKit>(_caster.AbilitySystem.Kit);
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator SupernovaWaitsForLandingDuringAnElevatedCast()
        {
            _caster.Teleport(new Vector3(0, 20, -8));
            _caster.Intent.Set(Verb.Ultimate, true);
            float start = Time.time, impactHeight = -1;
            bool impact = false, groundedAtImpact = false, accepted = false;
            while (Time.time - start < 4)
            {
                if (Time.time - start > .2f) _caster.Intent.Set(Verb.Ultimate, false);
                accepted |= _caster.AbilitySystem.LastAnswer(HeroAbilitySystem.Slot.Ultimate) == HeroKit.CastOutcome.Cast;
                var crater = Object.FindFirstObjectByType<HeroHazards.SupernovaCraterComponent>();
                if (crater != null)
                {
                    impact = true; groundedAtImpact = _caster.IsGrounded; impactHeight = _caster.transform.position.y;
                    break;
                }
                yield return null;
            }
            File.WriteAllText("Logs/sean-supernova-contact.csv", FormattableString.Invariant(
                $"accepted,impact,grounded,height,elapsed\n{accepted},{impact},{groundedAtImpact},{impactHeight},{Time.time-start}\n"));
            Assert.True(accepted, "The elevated real ultimate press was not accepted.");
            Assert.True(impact, "The ultimate never produced its landing impact.");
            Assert.True(groundedAtImpact, "The timeout detonated Supernova while Sean was still airborne.");
        }
    }
}
