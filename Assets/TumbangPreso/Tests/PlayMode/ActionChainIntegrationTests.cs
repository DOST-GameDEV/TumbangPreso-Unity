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
            GameServices.Round.enabled = true; Hitstop.End();
            UI.Hud.Instance.ShowReadyPrompt(false);
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
            shoe.HostThrow(actor, GameServices.Round.Lata.transform.position + new Vector3(0, .25f, -2), new Vector3(0, 1.5f, 12));
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
            Assert.AreEqual(0, match.LastHostChainResult.Bonus, "Retired accuracy awards must not double-pay current knockdown bonuses.");
            Assert.AreEqual(first + 2 * MatchRules.PointsFor(ScoreEvent.LataKnocked) + 50, match.ScoreFor(1),
                "The bonus must be awarded once through the score authority.");
            Assert.AreEqual(second + MatchRules.PointsFor(ScoreEvent.LataKnocked), match.ScoreFor(2));
            if (mode == GameMode.HeroStrike)
                Assert.AreEqual(Mathf.Min(kit.UltimateCost, charge + Balance.UltimateChargeLataKnock), kit.UltimateCharge, .001f);

            yield return Hit(1);
            Assert.AreEqual(first + 3 * MatchRules.PointsFor(ScoreEvent.LataKnocked) + 100, match.ScoreFor(1));
            Assert.AreEqual(MatchMomentKind.MultiKnockdown, match.LastMoment.Kind);
            Assert.AreEqual(1, match.LastMoment.Actor); Assert.AreEqual(50, match.LastMoment.Bonus);
            yield return new WaitForSecondsRealtime(.08f);
            Assert.AreEqual("MULTI KNOCKDOWN", Object.FindAnyObjectByType<UI.MatchMomentBanner>().Phrase);
            yield return GameplayShots.Render(Camera.main, mode + "-accuracy-milestone", true, outDir: "Logs/announcement-awards-1001");

            int catchBonuses = 0;
            void Bonus(int seat, ScoreEvent score) { if (MatchRules.IsChainBonus(score) && seat == 0) catchBonuses += MatchRules.PointsFor(score); }
            match.Scored += Bonus;
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
            Assert.AreEqual(25, catchBonuses);
            Assert.AreEqual(MatchMomentKind.DoubleCatch, match.LastMoment.Kind);
            yield return new WaitForSecondsRealtime(.08f);
            yield return GameplayShots.Render(Camera.main, mode + "-double-catch-milestone", true, outDir: "Logs/announcement-awards-1001");
            match.Scored -= Bonus;
            round.EndRound(); match.AdvanceRound();
            Assert.AreEqual(0, match.HostAccuracyChainFor(1)); Assert.AreEqual(0, match.HostAccuracyChainFor(2));
            Assert.IsFalse(match.LastHostChainResult.Applied);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator FirstLateMultiBonusesStackOnceAndCanDownResetsCatches()
        {
            yield return Open(GameMode.Classic); yield return ReadyCan();
            var match = GameServices.Match; var round = GameServices.Round; var can = round.Lata;
            typeof(RoundDirector).GetProperty("TimeLeft").SetValue(round,10f);
            int before = match.ScoreFor(1);
            can.HostKnockDown(1);
            Assert.AreEqual(before+200,match.ScoreFor(1),"First and last-ten-second bonuses independently qualify.");
            can.HostKnockDown(1); Assert.AreEqual(before+200,match.ScoreFor(1),"A down can cannot be scored twice.");
            yield return ReadyCan(); can.HostKnockDown(1);
            Assert.AreEqual(before+350,match.ScoreFor(1));
            yield return ReadyCan(); can.HostKnockDown(1);
            Assert.AreEqual(before+550,match.ScoreFor(1),"Third accepted knockdown also pays Multi Knockdown.");
            Assert.AreEqual(MatchMomentKind.MultiKnockdown,match.LastMoment.Kind);
            yield return ReadyCan();
            var taya=round.PlayerAt(0); var victim=round.PlayerAt(1);
            victim.ClearStun();victim.Teleport(new Vector3(0,.18f,2));
            round.ResolveTag(taya,victim);
            Assert.AreEqual(1,match.LastHostChainResult.Count);
            // An uncredited physical knockdown still breaks the taya's catch window.
            can.HostKnockDown(-1); yield return ReadyCan();
            var second=round.PlayerAt(2); second.ClearStun();second.Teleport(new Vector3(0,.18f,2));
            round.ResolveTag(taya,second);
            Assert.AreEqual(1,match.LastHostChainResult.Count);
            victim.ClearStun();can.HostKnockDown(1);
            Assert.AreEqual(before+700,match.ScoreFor(1),"Being tagged resets the knockdown streak; only the late bonus remains.");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator FourLegalCatchesIncludeARecoveredVictimAndSproutKeepsItsBaseAward()
        {
            yield return Open(GameMode.Classic); yield return ReadyCan();
            var match=GameServices.Match;var round=GameServices.Round;var taya=round.PlayerAt(0);
            int start=match.ScoreFor(0), passive=0;
            void Passive(int seat, ScoreEvent score) { if(seat==0 && score==ScoreEvent.DefenseTick) passive+=MatchRules.PointsFor(score); }
            match.Scored+=Passive;
            try
            {
                var first=round.PlayerAt(1);
                for(int seat=1;seat<=3;seat++)
                {
                    var victim=round.PlayerAt(seat);victim.ClearStun();victim.Teleport(new Vector3(0,.18f,2));
                    round.ResolveTag(taya,victim);
                    Assert.AreEqual(seat,match.LastHostChainResult.Count);
                    Assert.AreEqual(seat==1?0:25,match.LastHostChainResult.Bonus);
                    if(seat<3)
                    {
                        float until=Time.time+2, realDeadline=Time.realtimeSinceStartup+15;
                        while(Time.time<until&&Time.realtimeSinceStartup<realDeadline)yield return null;
                        Assert.GreaterOrEqual(Time.time,until,"The actual active-play clock must keep advancing.");
                    }
                }
                float deadline=Time.realtimeSinceStartup+15;
                while(first.IsTagged&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.IsFalse(first.IsTagged,"The repeated victim must really finish recovery first.");
                first.Teleport(new Vector3(0,.18f,2));round.ResolveTag(taya,first);
                Assert.AreEqual(4,match.LastHostChainResult.Count);
                Assert.AreEqual(MatchMomentKind.MultiCatch,match.LastMoment.Kind);
                Assert.AreEqual(25,match.LastMoment.Bonus);
                Assert.AreEqual(start+4*MatchRules.PointsFor(ScoreEvent.Tag)+75+passive,match.ScoreFor(0));
                int attacker=match.ScoreFor(2);
                round.Lata.HostKnockDown(2,ScoreEvent.SproutKnock);
                Assert.AreEqual(attacker+PaeteRules.SproutKnockPoints+50,match.ScoreFor(2),
                    "A global first-knock bonus must preserve the authored Sprout base reward.");
            }
            finally { match.Scored-=Passive; }
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
