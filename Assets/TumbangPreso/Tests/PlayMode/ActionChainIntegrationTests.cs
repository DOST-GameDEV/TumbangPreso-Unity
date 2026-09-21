using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ActionChainIntegrationTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest] public IEnumerator ClassicAcceptedOutcomesFeedTheHostChain() => AcceptedOutcomes(GameMode.Classic);
        [UnityTest] public IEnumerator HeroAcceptedOutcomesKeepBaseUltimateCharge() => AcceptedOutcomes(GameMode.HeroStrike);

        private static IEnumerator Open(GameMode mode)
        {
            yield return MapRetrievalProbe.Load("Eskinita", mode);
            foreach (var player in GameServices.Round.Players)
                player.Teleport(new Vector3(5, player.transform.position.y, -5 + player.PlayerSlot * 3));
        }
        private static IEnumerator ReadyCan()
        {
            var can = GameServices.Round.Lata;
            if (!can.IsUpright) can.HostRestore();
            float until = Time.time + 4;
            while (can.IsProtected && Time.time < until) yield return null;
            Assert.IsFalse(can.IsProtected);
        }
        private static Slipper OwnShoe(int seat)
        {
            foreach (var shoe in Object.FindObjectsByType<Slipper>())
                if (shoe.OwnerSlot == seat) return shoe;
            Assert.Fail("No owned shoe for " + seat); return null;
        }
        private static IEnumerator Hit(int seat)
        {
            yield return ReadyCan();
            var actor = GameServices.Round.PlayerAt(seat); var shoe = OwnShoe(seat);
            Assert.IsTrue(shoe.HostForceEquip(actor));
            int before = GameServices.Round.Lata.HostKnockdownSerial;
            shoe.HostThrow(actor, GameServices.Round.Lata.transform.position + new Vector3(0, 1.2f, -2), Vector3.forward * 12);
            float until = Time.time + 2;
            while (GameServices.Round.Lata.IsUpright && Time.time < until) yield return new WaitForFixedUpdate();
            Assert.AreEqual(before + 1, GameServices.Round.Lata.HostKnockdownSerial);
        }
        private static IEnumerator AcceptedOutcomes(GameMode mode)
        {
            yield return Open(mode);
            var match = GameServices.Match; var round = GameServices.Round;
            int first = match.ScoreFor(1), second = match.ScoreFor(2);
            yield return Hit(1); Assert.AreEqual(1, match.HostAccuracyChainFor(1));
            yield return Hit(2); Assert.AreEqual(1, match.HostAccuracyChainFor(1));
            Assert.AreEqual(1, match.HostAccuracyChainFor(2));
            var kit = round.PlayerAt(1).AbilitySystem?.Kit;
            float charge = kit != null ? kit.UltimateCharge : 0;
            yield return Hit(1);
            Assert.AreEqual(2, match.HostAccuracyChainFor(1));
            Assert.AreEqual(10, match.LastHostChainResult.Bonus, "The rule result is ready for future accepted bonus transport.");
            Assert.AreEqual(first + 2 * MatchRules.PointsFor(ScoreEvent.LataKnocked), match.ScoreFor(1),
                "Pending chain integration must not quietly add unreplicated points.");
            Assert.AreEqual(second + MatchRules.PointsFor(ScoreEvent.LataKnocked), match.ScoreFor(2));
            if (mode == GameMode.HeroStrike)
                Assert.AreEqual(Mathf.Min(kit.UltimateCost, charge + Balance.UltimateChargeLataKnock), kit.UltimateCharge, .001f);

            round.Lata.HostRestore();
            var taya = round.PlayerAt(0); var a = round.PlayerAt(1); var b = round.PlayerAt(2);
            Assert.IsTrue(OwnShoe(1).HostForceEquip(a));
            a.ClearStun(); a.Teleport(new Vector3(0, .18f, -2.2f));
            taya.Intent.Parked = false; taya.ClearStun(); taya.Teleport(new Vector3(0, .18f, -3.2f));
            taya.transform.forward = Vector3.forward;
            Assert.IsTrue(taya.GetComponent<CombatVerbs>().HostResolvePunch(taya.transform.position, Vector3.forward));
            Assert.AreEqual(0, match.HostAccuracyChainFor(1));
            Assert.AreEqual(1, match.LastHostChainResult.Count);
            yield return new WaitForSeconds(Balance.PunchCooldown + .05f);
            Assert.IsTrue(OwnShoe(2).HostForceEquip(b));
            b.ClearStun(); b.Teleport(new Vector3(0, .18f, -2.2f));
            taya.Teleport(new Vector3(0, .18f, -3.2f));
            Assert.IsTrue(taya.GetComponent<CombatVerbs>().HostResolvePunch(taya.transform.position, Vector3.forward));
            Assert.AreEqual(2, match.LastHostChainResult.Count);
            Assert.AreEqual(ChainMilestone.DoubleCatch, match.LastHostChainResult.Milestone);
            round.EndRound(); match.AdvanceRound();
            Assert.AreEqual(0, match.HostAccuracyChainFor(1)); Assert.AreEqual(0, match.HostAccuracyChainFor(2));
            Assert.IsFalse(match.LastHostChainResult.Applied);
        }

        [UnityTest]
        public IEnumerator AConsumedFlightPreservesAccuracyButATrueMissBreaksIt()
        {
            yield return Open(GameMode.Classic); yield return Hit(2); yield return ReadyCan();
            var match = GameServices.Match; var round = GameServices.Round; var actor = round.PlayerAt(2);
            var miss = OwnShoe(2); Assert.IsTrue(miss.HostForceEquip(actor));
            miss.HostThrow(actor, new Vector3(-4, 2, -4), Vector3.forward * 2);
            // Hit is already ready, so it launches while the other shot is airborne.
            yield return Hit(1);
            float until = Time.time + 4;
            while (miss.State == SlipperState.InFlight && Time.time < until) yield return new WaitForFixedUpdate();
            Assert.AreEqual(SlipperState.Loose, miss.State);
            Assert.AreEqual(1, match.HostAccuracyChainFor(2), "Another player's consumed can cycle is no contest.");
            yield return ReadyCan(); Assert.IsTrue(miss.HostForceEquip(actor));
            miss.HostThrow(actor, new Vector3(-4, 2, -4), Vector3.forward * 2);
            until = Time.time + 4;
            while (miss.State == SlipperState.InFlight && Time.time < until) yield return new WaitForFixedUpdate();
            Assert.AreEqual(SlipperState.Loose, miss.State); Assert.AreEqual(0, match.HostAccuracyChainFor(2));
        }
    }
}
