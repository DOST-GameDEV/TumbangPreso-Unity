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
    public sealed class ZackKitAcceptanceProbe
    {
        private bool _bots, _spectator, _pinned;
        private int _seat;
        private INetProvider _net;
        private CustomRules _rules;
        private CharacterMotor _caster;
        private AbilityChallengeProgress[] _challenges;

        [UnitySetUp] public IEnumerator Before()
        {
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            _net = NetAuthority.Provider; _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            _challenges = Settings.SettingsStore.Current.AbilityChallenges.ToArray();
            yield return PlayModeWorld.Reset();
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, GameMode.HeroStrike);
            Object.FindFirstObjectByType<ReadyGate>().StartLocalCountdown();
            yield return new WaitForSeconds(3.6f);
            NetAuthority.Provider = new SoloProvider();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) input.enabled = false;
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>(FindObjectsSortMode.None)) switcher.enabled = false;
            foreach (var player in GameServices.Round.Players)
            {
                player.Intent.Clear(); player.Intent.Parked = true;
                player.Teleport(new Vector3(10 + player.PlayerSlot * 2, .12f, -10));
            }
            _caster = GameServices.Round.PlayerAt(1); _caster.IsBot = true;
            _caster.AbilitySystem.BindHero("zack");
            _caster.Teleport(new Vector3(0, .12f, -8)); _caster.transform.rotation = Quaternion.identity;
            _caster.Intent.Parked = false;
        }

        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _net;
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
            var progress = Settings.SettingsStore.Current.AbilityChallenges;
            progress.Clear(); progress.AddRange(_challenges);
        }

        private void Unlock(string id)
        {
            var progress = Settings.SettingsStore.Current.AbilityChallenges;
            progress.RemoveAll(row => row.VariantId == id);
            progress.Add(new AbilityChallengeProgress { VariantId = id, Count = 999 });
        }

        private AbilityContext Context() => new AbilityContext(_caster, _caster.GetComponent<Carrier>(), _caster.GetComponent<CombatVerbs>());

        [UnityTest, Timeout(90000)]
        public IEnumerator SnapDischargeTradesArmingTimeForActualFasterFlight()
        {
            Unlock("zack.2.discharge");
            var speeds = new float[2]; var windows = new float[2];
            var carrier = _caster.GetComponent<Carrier>(); var shoe = carrier.Held;
            for (int variant = 0; variant < 2; variant++)
            {
                _caster.AbilitySystem.BindHero("zack", new HeroBuild { HeroId = "zack",
                    Slot2VariantId = variant == 0 ? "zack.2.charge" : "zack.2.discharge" });
                Assert.True(shoe.HostForceEquip(_caster)); shoe.HostDisarm();
                shoe.transform.position = new Vector3(1, Slipper.GroundY(new Vector3(1, 0, -4)) + shoe.RestHeight, -4);
                _caster.Teleport(new Vector3(0, .12f, -8)); _caster.Intent.Clear(); _caster.Intent.Parked = false;
                _caster.Intent.AimPoint = new Vector3(0, 1.2f, 7);
                yield return new WaitForSeconds(.2f);
                _caster.Intent.Set(Verb.Skill2, true); yield return new WaitForSeconds(.2f);
                _caster.Intent.Set(Verb.Skill2, false);
                var kit = (ZackHeroKit)_caster.AbilitySystem.Kit;
                Assert.AreSame(shoe, carrier.Held); Assert.True(kit.IsOverchargeThrowActive);
                windows[variant] = kit.Skill2.Duration;
                _caster.Intent.Set(Verb.SpecialAbility, true); yield return new WaitForSeconds(.5f);
                _caster.Intent.Set(Verb.SpecialAbility, false);
                float release = Time.time;
                while (Time.time - release < .3f)
                {
                    if (shoe.State == SlipperState.InFlight && shoe.Affinity == SlipperAffinity.ElectricZap)
                        speeds[variant] = Mathf.Max(speeds[variant], shoe.Velocity.magnitude);
                    yield return null;
                }
                Assert.False(kit.IsOverchargeThrowActive);
                yield return new WaitForSeconds(1.2f);
            }
            File.WriteAllText("Logs/zack-snap-comparison.csv", FormattableString.Invariant(
                $"variant,flight_speed,arming_window\nmagnet,{speeds[0]},{windows[0]}\nsnap,{speeds[1]},{windows[1]}\n"));
            Assert.Greater(speeds[0], 8); Assert.Greater(speeds[1], speeds[0] * 1.25f);
            Assert.Less(windows[1], windows[0] - 3);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator ArcLinePunishesTheInnerLaneWhileLeavingItsOuterEdgeClear()
        {
            Unlock("zack.1.arcline");
            var center = GameServices.Round.PlayerAt(2); var edge = GameServices.Round.PlayerAt(0);
            var widths = new float[2]; var shocks = new float[2]; var edgeShocks = new float[2];
            for (int variant = 0; variant < 2; variant++)
            {
                _caster.AbilitySystem.BindHero("zack", new HeroBuild { HeroId = "zack",
                    Slot1VariantId = variant == 0 ? "zack.1.sprint" : "zack.1.arcline" });
                _caster.Teleport(new Vector3(0, .12f, -3)); _caster.Intent.Clear(); _caster.Intent.Parked = false;
                _caster.Intent.Set(Verb.Skill1, true); yield return new WaitForSeconds(.1f);
                _caster.Intent.Set(Verb.Skill1, false);
                var patch = Object.FindObjectsByType<HeroHazards.ShockTrailComponent>(FindObjectsSortMode.None)
                    .First(field => field.OwnerSlot == _caster.PlayerSlot);
                Assert.IsNotNull(patch.GetComponentInChildren<Visual.ZackSkateWake>());
                widths[variant] = patch.Radius;
                center.ClearStun(); edge.ClearStun();
                center.Teleport(patch.transform.position + new Vector3(.2f, .12f, 0));
                edge.Teleport(patch.transform.position + new Vector3(.78f, .12f, 0));
                float start = Time.time;
                while (Time.time - start < .2f)
                {
                    shocks[variant] = Mathf.Max(shocks[variant], center.StunLeft);
                    edgeShocks[variant] = Mathf.Max(edgeShocks[variant], edge.StunLeft);
                    yield return null;
                }
                center.Teleport(new Vector3(12, .12f, -12)); edge.Teleport(new Vector3(-12, .12f, -12));
                yield return new WaitForSeconds(5.5f);
            }
            File.WriteAllText("Logs/zack-arcline-comparison.csv", FormattableString.Invariant(
                $"variant,radius,inner_shock,edge_shock\nsprint,{widths[0]},{shocks[0]},{edgeShocks[0]}\narc,{widths[1]},{shocks[1]},{edgeShocks[1]}\n"));
            Assert.Less(widths[1], widths[0] * .7f);
            Assert.Greater(shocks[1], shocks[0] + .05f);
            Assert.Greater(edgeShocks[0], .1f); Assert.Zero(edgeShocks[1]);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator MagnetEndsItsChargeOnExpiryRefusalAndHeroReplacement()
        {
            var shoe = _caster.GetComponent<Carrier>().Held;
            for (int scenario = 0; scenario < 3; scenario++)
            {
                _caster.AbilitySystem.BindHero("zack");
                Assert.True(shoe.HostForceEquip(_caster)); shoe.HostDisarm();
                shoe.transform.position = new Vector3(1, Slipper.GroundY(new Vector3(1, 0, -4)) + shoe.RestHeight, -4);
                _caster.Intent.Set(Verb.Skill2, true); yield return new WaitForSeconds(.2f);
                _caster.Intent.Set(Verb.Skill2, false);
                var kit = (ZackHeroKit)_caster.AbilitySystem.Kit;
                Assert.True(kit.IsOverchargeThrowActive);
                Assert.IsNotNull(shoe.GetComponentInChildren<Visual.ZackMagnetCharge>());
                if (scenario == 0) yield return new WaitForSeconds(kit.Skill2.Duration + .15f);
                else if (scenario == 1) kit.Skill2.RollBackPredictedCast(Context());
                else _caster.AbilitySystem.BindHero("sean");
                yield return null; yield return null;
                Assert.IsNull(shoe.GetComponentInChildren<Visual.ZackMagnetCharge>(), "Carried poles survived scenario " + scenario);
                Assert.IsEmpty(Object.FindObjectsByType<Visual.MagnetRecallTrace>(FindObjectsSortMode.None),
                    "Recall trace survived scenario " + scenario);
                Assert.False(kit.Skill2.IsActive, "The old skill kept running after scenario " + scenario);
                if (scenario < 2) Assert.False(kit.IsOverchargeThrowActive);
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator RefusedSprintRemovesItsPredictedWakeImmediately()
        {
            _caster.Intent.Set(Verb.Skill1, true); yield return new WaitForSeconds(.15f);
            _caster.Intent.Set(Verb.Skill1, false);
            var kit = (ZackHeroKit)_caster.AbilitySystem.Kit;
            Assert.True(kit.Skill1.IsActive);
            Assert.IsNotEmpty(Object.FindObjectsByType<HeroHazards.ShockTrailComponent>(FindObjectsSortMode.None));
            kit.Skill1.RollBackPredictedCast(Context());
            yield return null; yield return null;
            Assert.False(kit.Skill1.IsActive);
            var fields = Object.FindObjectsByType<HeroHazards.ShockTrailComponent>(FindObjectsSortMode.None);
            bool aura = _caster.transform.Find("Vfx_Aura_ElectricSpark") != null;
            File.WriteAllText("Logs/zack-refused-sprint.csv", $"active,fields,aura\n{kit.Skill1.IsActive},{fields.Length},{aura}\n");
            Assert.IsEmpty(fields,
                "An unaccepted sprint left its predicted chase hazard running.");
            Assert.False(aura, "An unaccepted sprint left its body aura running.");
        }
    }
}
