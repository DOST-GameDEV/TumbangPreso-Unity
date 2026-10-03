using System.Collections;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ObjectiveEconomyTests
    {
        private readonly HeroAbilitySystem[] _systems = new HeroAbilitySystem[4];
        private INetProvider _provider;
        private bool _tutorial, _pinned;
        private CustomRules _rules;
        private sealed class Replica : INetProvider
        {
            public bool IsHost => false; public bool IsNetworked => true;
            public int LocalSlot => 0; public int LocalPeerId => 1; public bool IsSeatlessReferee => false;
        }
        [UnitySetUp] public IEnumerator Before()
        {
            _provider = NetAuthority.Provider; _tutorial = GameLaunch.GuidedTutorial;
            _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = new SoloProvider(); GameLaunch.GuidedTutorial = false;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            GameServices.Ensure(); GameServices.Round.Clear();
            for (int seat = 0; seat < _systems.Length; seat++)
            {
                var root = new GameObject("Objective seat " + seat);
                var body = root.AddComponent<CharacterMotor>(); body.enabled = false;
                body.PlayerSlot = seat; body.Mode = GameMode.HeroStrike;
                body.IsDefender = seat == 0; GameServices.Round.Register(body);
                var system = root.AddComponent<HeroAbilitySystem>(); system.enabled = false;
                system.BindHero("zack"); _systems[seat] = system;
            }
            GameServices.Match.StartMatch();
        }
        [UnityTearDown] public IEnumerator After()
        {
            foreach (var system in _systems) if (system != null) Object.Destroy(system.gameObject);
            yield return PlayModeWorld.Reset();
            NetAuthority.Provider = _provider; GameLaunch.GuidedTutorial = _tutorial;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }
        [Test] public void OnlyKnockdownsAndTagsGrantActivityIncomeAndZackDiscounts()
        {
            GameServices.Round.BeginRound();
            var system = _systems[1]; var kit = system.Kit; kit.PracticeMode = false;
            kit.Skill1.ApplyNetworkSnapshot(20, 0, true);
            system.OnThrowReleased(); system.OnOwnSlipperRetrieved();
            Assert.AreEqual(0, kit.UltimateCharge);
            Assert.AreEqual(20, kit.Skill1.CooldownRemaining, .001f);
            system.OnLataKnocked();
            Assert.AreEqual(1, kit.UltimateCharge); Assert.AreEqual(15, kit.Skill1.CooldownRemaining, .001f);
            system.OnTagScored();
            Assert.AreEqual(2, kit.UltimateCharge); Assert.AreEqual(10, kit.Skill1.CooldownRemaining, .001f);
            Assert.AreEqual(0, GameServices.Match.ScoreFor(1), "Ultimate income does not mint scoreboard points.");
        }
        [Test] public void DefenderGetsOnePointPerRoundDespiteResetAndStalePracticeFlag()
        {
            foreach (var system in _systems) system.Kit.PracticeMode = true;
            _systems[0].Kit.Skill1.ApplyNetworkSnapshot(20, 0, true);
            GameServices.Round.BeginRound();
            Assert.AreEqual(1, _systems[0].Kit.UltimateCharge);
            Assert.AreEqual(15, _systems[0].Kit.Skill1.CooldownRemaining, .001f);
            for (int i = 1; i < 4; i++) Assert.AreEqual(0, _systems[i].Kit.UltimateCharge);
            GameServices.Round.BeginRound(); _systems[0].OnDefenderRoundStarted();
            GameServices.Round.EndRound(); GameServices.Round.BeginRound();
            Assert.AreEqual(1, _systems[0].Kit.UltimateCharge, "Repeated entry cannot farm the grant.");
            GameServices.Match.AdvanceRound(); GameServices.Round.BeginRound();
            Assert.AreEqual(1, _systems[1].Kit.UltimateCharge);
            Assert.AreEqual(1, _systems[0].Kit.UltimateCharge, "Existing charge survives rotation.");
            foreach (var system in _systems) system.ResetKitForMatch();
            GameServices.Match.StartMatch(); GameServices.Round.BeginRound();
            Assert.AreEqual(1, _systems[0].Kit.UltimateCharge, "A new match gets its own first-round grant.");
        }
        [Test] public void ReplicaTutorialAndClassicDoNotMintDefenderIncome()
        {
            NetAuthority.Provider = new Replica(); GameServices.Round.BeginRound();
            Assert.AreEqual(0, _systems[0].Kit.UltimateCharge);
            NetAuthority.Provider = new SoloProvider(); GameLaunch.GuidedTutorial = true;
            GameServices.Round.BeginRound(); Assert.AreEqual(0, _systems[0].Kit.UltimateCharge);
            GameLaunch.GuidedTutorial = false;
            var body = _systems[0].GetComponent<CharacterMotor>(); body.Mode = GameMode.Classic;
            GameServices.Round.BeginRound(); Assert.AreEqual(0, _systems[0].Kit.UltimateCharge);
            body.Mode = GameMode.HeroStrike; GameServices.Round.BeginRound();
            Assert.AreEqual(1, _systems[0].Kit.UltimateCharge);
        }
        [Test] public void FullMeterCapsAndResetCannotReissueTheSameRoundGrant()
        {
            var system = _systems[0]; system.Kit.AddUltimateCharge(system.Kit.UltimateCost);
            GameServices.Round.BeginRound(); Assert.AreEqual(system.Kit.UltimateCost, system.Kit.UltimateCharge);
            system.ResetKitForMatch(); GameServices.Round.BeginRound();
            Assert.AreEqual(0, system.Kit.UltimateCharge, "A same-round kit reset cannot reissue a consumed grant.");
        }
    }
}
