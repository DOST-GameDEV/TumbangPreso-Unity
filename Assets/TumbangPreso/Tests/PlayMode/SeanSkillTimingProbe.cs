using System;
using System.Collections;
using System.IO;
using System.Linq;
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
        public IEnumerator JoiningIgnitionRestoresOnlyItsRemainingWindowWithoutSpending()
        {
            var kit=(SeanHeroKit)_caster.AbilitySystem.Kit;
            kit.Skill2.ApplyNetworkSnapshot(0,1);
            float bank=kit.UltimateCharge;
            Assert.IsTrue(kit.RestoreJoiningIgnition(_caster,1.2f));
            yield return null;yield return null;
            Assert.IsTrue(kit.IsIgnitionCannonActive);
            Assert.AreEqual(1,kit.Skill2.ChargesRemaining);Assert.AreEqual(bank,kit.UltimateCharge);
            Assert.IsNotNull(_caster.GetComponent<Carrier>().Held.GetComponentInChildren<Visual.SeanIgnitionVisual>());
            float remaining=kit.Skill2.DurationRemaining;
            Assert.IsFalse(kit.RestoreJoiningIgnition(_caster,10));
            Assert.AreEqual(remaining,kit.Skill2.DurationRemaining,.001f);
            yield return new WaitForSeconds(1.3f);
            Assert.IsFalse(kit.IsIgnitionCannonActive);
            Assert.IsNull(_caster.GetComponent<Carrier>().Held.GetComponentInChildren<Visual.SeanIgnitionVisual>());
            Assert.IsFalse(kit.RestoreJoiningIgnition(_caster,10),"A duplicate joining record rearmed an expired charge.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator LateJoiningChargeCannotOverwriteAConsumedShotOrANewerCast()
        {
            var shoe=_caster.GetComponent<Carrier>().Held;
            var kit=(SeanHeroKit)_caster.AbilitySystem.Kit;
            shoe.ApplySnapshotState(SlipperState.InFlight,null,new Vector3(0,1,-5),Quaternion.identity,
                Vector3.forward*8,0,SlipperAffinity.FireExplosive,_caster.PlayerSlot);
            Assert.IsFalse(kit.RestoreJoiningIgnition(_caster,5),"A stale initial record rearmed an accepted fire release.");
            Assert.IsFalse(kit.IsIgnitionCannonActive);
            _caster.AbilitySystem.BindHero("sean");kit=(SeanHeroKit)_caster.AbilitySystem.Kit;
            Assert.IsTrue(shoe.HostForceEquip(_caster));
            _caster.Intent.Set(Verb.Skill2,true);yield return new WaitForSeconds(.2f);
            _caster.Intent.Set(Verb.Skill2,false);
            Assert.IsTrue(kit.IsIgnitionCannonActive);
            float remaining=kit.Skill2.DurationRemaining;
            Assert.IsFalse(kit.RestoreJoiningIgnition(_caster,.5f));
            Assert.AreEqual(remaining,kit.Skill2.DurationRemaining,.001f,"An older record shortened the new cast.");
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
        public IEnumerator AfterburnTradesTravelForAHeatWakeThatLastsLonger()
        {
            var challenges = Settings.SettingsStore.Current.AbilityChallenges;
            var saved = challenges.ToArray();
            var travel = new float[2]; var lives = new float[2];
            try
            {
                challenges.RemoveAll(row => row.VariantId == "sean.1.afterburn");
                challenges.Add(new AbilityChallengeProgress { VariantId = "sean.1.afterburn", Count = 999 });
                for (int variant = 0; variant < 2; variant++)
                {
                    _caster.AbilitySystem.BindHero("sean", new HeroBuild { HeroId = "sean",
                        Slot1VariantId = variant == 0 ? "sean.1.rush" : "sean.1.afterburn" });
                    _caster.Teleport(new Vector3(0, .12f, -8));
                    _caster.Intent.Clear(); _caster.Intent.Parked = false;
                    float start = Time.time, firstHeat = -1, lastHeat = -1;
                    bool accepted = false;
                    while (Time.time - start < 4.8f)
                    {
                        _caster.Intent.Set(Verb.Skill1, Time.time - start < .2f);
                        accepted |= _caster.AbilitySystem.LastAnswer(HeroAbilitySystem.Slot.Skill1) == HeroKit.CastOutcome.Cast;
                        travel[variant] = Mathf.Max(travel[variant], _caster.transform.position.z + 8);
                        var fields = Object.FindObjectsByType<HeroHazards.FireTrailComponent>(FindObjectsSortMode.None);
                        if (fields.Length > 0)
                        {
                            if (firstHeat < 0) firstHeat = Time.time;
                            lastHeat = Time.time;
                            foreach (var field in fields)
                            {
                                Assert.AreEqual(1, field.Radius, .001f, "Afterburn changed the trail's reach.");
                                Assert.IsNotNull(field.GetComponentInChildren<Visual.SeanHeatGround>());
                            }
                        }
                        yield return null;
                    }
                    Assert.True(accepted); Assert.Greater(firstHeat, 0);
                    lives[variant] = lastHeat - firstHeat;
                }
                File.WriteAllText("Logs/sean-afterburn-comparison.csv", FormattableString.Invariant(
                    $"variant,travel,heat_life\nrush,{travel[0]},{lives[0]}\nafterburn,{travel[1]},{lives[1]}\n"));
                Assert.Greater(travel[0], travel[1] + .8f);
                Assert.Greater(lives[1], lives[0] + .6f);
            }
            finally { challenges.Clear(); challenges.AddRange(saved); }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator FlareShotReleasesFasterWithAShorterArmingWindow()
        {
            var challenges = Settings.SettingsStore.Current.AbilityChallenges;
            var saved = challenges.ToArray();
            var speeds = new float[2]; var windows = new float[2];
            var shoe = _caster.GetComponent<Carrier>().Held;
            try
            {
                challenges.RemoveAll(row => row.VariantId == "sean.2.flare");
                challenges.Add(new AbilityChallengeProgress { VariantId = "sean.2.flare", Count = 999 });
                for (int variant = 0; variant < 2; variant++)
                {
                    _caster.AbilitySystem.BindHero("sean", new HeroBuild { HeroId = "sean",
                        Slot2VariantId = variant == 0 ? "sean.2.cannon" : "sean.2.flare" });
                    Assert.True(shoe.HostForceEquip(_caster));
                    _caster.Teleport(new Vector3(0, .12f, -8)); _caster.Intent.Clear(); _caster.Intent.Parked = false;
                    _caster.Intent.AimPoint = new Vector3(0, 1.2f, 7);
                    yield return new WaitForSeconds(.3f);
                    _caster.Intent.Set(Verb.Skill2, true);
                    yield return new WaitForSeconds(.2f);
                    _caster.Intent.Set(Verb.Skill2, false);
                    var kit = (SeanHeroKit)_caster.AbilitySystem.Kit;
                    Assert.True(kit.IsIgnitionCannonActive);
                    windows[variant] = kit.Skill2.Duration;
                    _caster.Intent.Set(Verb.SpecialAbility, true);
                    yield return new WaitForSeconds(.5f);
                    _caster.Intent.Set(Verb.SpecialAbility, false);
                    float released = Time.time;
                    while (Time.time - released < .3f)
                    {
                        if (shoe.State == SlipperState.InFlight && shoe.Affinity == SlipperAffinity.FireExplosive)
                            speeds[variant] = Mathf.Max(speeds[variant], shoe.Velocity.magnitude);
                        yield return null;
                    }
                    Assert.False(kit.IsIgnitionCannonActive);
                    yield return new WaitForSeconds(1.2f);
                }
                File.WriteAllText("Logs/sean-flare-comparison.csv", FormattableString.Invariant(
                    $"variant,flight_speed,arming_window\ncannon,{speeds[0]},{windows[0]}\nflare,{speeds[1]},{windows[1]}\n"));
                Assert.Greater(speeds[0], 8);
                Assert.Greater(speeds[1], speeds[0] * 1.18f);
                Assert.Less(windows[1], windows[0] - 2);
            }
            finally { challenges.Clear(); challenges.AddRange(saved); }
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
