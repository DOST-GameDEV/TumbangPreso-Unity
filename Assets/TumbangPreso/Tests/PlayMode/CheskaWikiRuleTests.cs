using System.Collections;
using NUnit.Framework;
using TumbangPreso.Abilities;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class CheskaWikiRuleTests
    {
        private INetProvider _net;
        private CustomRules _rules;
        private bool _pinned, _bots, _spectator;
        private int _seat;
        [UnitySetUp] public IEnumerator Before()
        {
            _net = NetAuthority.Provider; _rules = SceneFlow.SelectedRules.Clone();
            _pinned = SceneFlow.RulesPinned; _bots = GameLaunch.AllBots;
            _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset(); NetAuthority.Provider = _net;
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }
        private IEnumerator Load(GameMode mode = GameMode.HeroStrike)
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita, mode);
            GameServices.Round.BeginRound();
            foreach (var who in GameServices.Round.Players)
            {
                who.enabled = false; who.GetComponent<Carrier>().enabled = false;
                who.GetComponent<CombatVerbs>().enabled = false;
                if (who.AbilitySystem != null) who.AbilitySystem.enabled = false;
                who.IsDefender = false; who.Teleport(new Vector3(6, .12f, 6 + who.PlayerSlot));
            }
        }
        [UnityTest] public IEnumerator ColdFeetRetainsItsRealFieldBeyondFiveSecondsAndExpiresAtSevenPointFive()
        {
            yield return Load();
            var caster = GameServices.Round.PlayerAt(1); caster.AbilitySystem.BindHero("cheska");
            caster.Teleport(new Vector3(-3, .12f, -4));
            var context = new AbilityContext(caster, caster.GetComponent<Carrier>(), caster.GetComponent<CombatVerbs>(),
                caster.transform.position, Vector3.forward, new Vector3(-3, .12f, -1));
            using (NetCue.SuppressRelay()) caster.AbilitySystem.Kit.Skill1.Activate(context);
            var field = Object.FindFirstObjectByType<HeroHazards.IceSheetComponent>();
            Assert.IsNotNull(field); Assert.AreEqual(7.5f, field.Duration, .001f);
            yield return new WaitForSeconds(5.2f);
            Assert.IsTrue(field != null && field.gameObject.activeInHierarchy,
                "The real field expired at the superseded five-second duration.");
            Assert.That(field.Remaining, Is.InRange(2.1f, 2.4f));
            yield return new WaitForSeconds(2.5f); yield return null;
            Assert.IsTrue(field == null, "The field must finish at the prescribed simulation lifetime.");
        }
        [UnityTest] public IEnumerator AbsoluteZeroWaitsThenFreezesEveryPlayerAndLeavesTheFullThawChill()
        {
            yield return Load();
            var caster = GameServices.Round.PlayerAt(1); caster.AbilitySystem.BindHero("cheska");
            var context = new AbilityContext(caster, caster.GetComponent<Carrier>(), caster.GetComponent<CombatVerbs>());
            var ultimate = caster.AbilitySystem.Kit.Ultimate;
            using (NetCue.SuppressRelay())
            {
                ultimate.Activate(context); ultimate.Tick(context, 1.49f);
                foreach (var who in GameServices.Round.Players)
                    Assert.IsFalse(who.IsFrozen, "Absolute Zero fired before its prescribed1.5-second delay.");
                ultimate.Tick(context, .02f);
            }
            foreach (var who in GameServices.Round.Players)
            {
                Assert.IsTrue(who.IsFrozen, "The current Wiki says every player, including the caster.");
                Assert.AreEqual(StatusRules.FrozenSeconds, who.StunLeft, .001f);
                Assert.AreEqual(StatusRules.FrozenSeconds + StatusRules.ChilledSeconds, who.ChilledLeft, .001f);
            }
            Assert.IsFalse(ultimate.IsWindingUp);
        }
        [UnityTest] public IEnumerator CheskasLandedHeroStrikeShoveAppliesChillingTouch() => Shove(GameMode.HeroStrike, "cheska", true);
        [UnityTest] public IEnumerator ClassicShovesDoNotGainAHeroPassive() => Shove(GameMode.Classic, "cheska", false);
        [UnityTest] public IEnumerator AnotherHerosShoveDoesNotGainChillingTouch() => Shove(GameMode.HeroStrike, "sean", false);
        private IEnumerator Shove(GameMode mode, string hero, bool chilled)
        {
            yield return Load(mode);
            var caster = GameServices.Round.PlayerAt(1); var victim = GameServices.Round.PlayerAt(2);
            // Classic deliberately has no ability component.
            caster.AbilitySystem?.BindHero(hero);
            caster.Teleport(new Vector3(0, .12f, -4)); victim.Teleport(new Vector3(0, .12f, -3));
            var verbs = caster.GetComponent<CombatVerbs>();
            Assert.IsTrue(caster.CanAct()); Physics.SyncTransforms();
            using (NetCue.SuppressRelay())
                Assert.IsTrue(verbs.HostResolveShove(caster.transform.position, Vector3.forward));
            Assert.AreEqual(Time.time, verbs.LastShoveLandedAt, .001f, "The real shove must hit its target.");
            Assert.AreEqual(chilled, victim.IsChilled);
            if (chilled) Assert.AreEqual(StatusRules.ChilledSeconds, victim.ChilledLeft, .001f);
        }
    }
}
