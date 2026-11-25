using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The final whistle on a real arena, offline (owner handoff 2026-11-25: the ending felt abrupt and bodies could be left
    /// floating under the result board). MATCH OVER over the live court for `MatchEndBeat.Duration`, the cast parked and
    /// settled, then the existing board once: on the timer's whistle, on a score target's, on a draw, and with a real
    /// AIRBURST released at the whistle.
    /// </summary>
    public sealed class MatchEndBeatTests
    {
        [UnitySetUp] public IEnumerator Before()
        {
            // NUnit keeps one fixture instance for every case: the counts start again for each one.
            _reveals = 0; _revealedMatch = 0; _revealedWinner = -9;
            yield return PlayModeWorld.Reset();
        }
        [UnityTearDown] public IEnumerator After()
        {
            PresentationClock.RequestScale(1.0f);
            Time.timeScale = 1.0f;
            yield return PlayModeWorld.Reset();
        }

        /// <summary>For other tests: the board appears after the whistle's beat. Fails after a bounded wait.</summary>
        internal static IEnumerator UntilBoard(MatchResult result, float seconds = MatchEndBeat.Duration + 2.0f)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while ((result == null || !result.IsVisible) && Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(result != null && result.IsVisible, "The result board was not revealed after the whistle's beat.");
        }

        private int _reveals;
        private long _revealedMatch;
        private int _revealedWinner = -9;
        private void Count(long match, int winner) { _reveals++; _revealedMatch = match; _revealedWinner = winner; }

        /// <summary>
        /// The beat itself, as the player sees it: MATCH OVER at once with no board, the board at the deadline and only
        /// once, the winner the whistle locked, and single player's clock stopped under the board.
        /// </summary>
        private IEnumerator Beat(MatchResult result, int expectedWinner)
        {
            var match = GameServices.Match;
            long id = match.PresentationMatchId;
            yield return null;
            Assert.IsTrue(MatchEndBeat.Holding, "The whistle did not start the beat.");
            Assert.AreEqual(id, MatchEndBeat.Instance.MatchId);
            Assert.IsTrue(Hud.MatchOverShowing, "MATCH OVER is not on the screen at the whistle.");
            Assert.IsFalse(result.IsVisible, "The board appeared on the whistle's own frame.");

            float until = Time.realtimeSinceStartup + 1.0f;
            while (Time.realtimeSinceStartup < until) yield return null;
            Assert.IsTrue(MatchEndBeat.Holding, "The beat ended before its deadline.");
            Assert.IsTrue(Hud.MatchOverShowing, "MATCH OVER left before the board.");
            Assert.IsFalse(result.IsVisible, "The board appeared a second early.");
            Assert.AreEqual(0, _reveals);

            yield return UntilBoard(result);
            Assert.AreEqual(1, _reveals, "The board was revealed more than once.");
            Assert.AreEqual(id, _revealedMatch);
            Assert.AreEqual(expectedWinner, _revealedWinner, "The board's winner is not the one the whistle locked.");
            Assert.IsFalse(Hud.MatchOverShowing, "MATCH OVER is still up over the board.");
            Assert.GreaterOrEqual(MatchEndBeat.Instance.Age, MatchEndBeat.Duration - 0.05f, "The board came before the deadline.");

            for (int i = 0; i < 10; i++) yield return null;
            Assert.AreEqual(1, _reveals, "A later frame revealed the board again.");
            Assert.AreEqual(1, MatchEndBeat.Instance.RevealCount);
            Assert.AreEqual(0.0f, Time.timeScale, "Single player's clock is not stopped under the board.");
        }

        private static void PinRules(int rounds, int target)
        {
            var rules = CustomGameRules.Defaults(GameMode.Classic); rules.Rounds = rounds; rules.ScoreTarget = target;
            SceneFlow.PinSelectedRules(rules);
        }

        private static IEnumerator AssertRested(IEnumerable<CharacterMotor> bodies, string why)
        {
            yield return null;
            foreach (var body in bodies)
            {
                // What is physically under the capsule, not WorldGround's court floor: a body may rightly rest on an awning
                // or a step that the court floor does not count. It must be standing on it, and it must be something drawn.
                Assert.IsTrue(Support(body, out Vector3 feet, out RaycastHit under), $"{why}: seat {body.PlayerSlot} has nothing under it");
                bool drawn = Drawn(under.point);
                Debug.Log($"[MatchEndBeat] {why} seat={body.PlayerSlot} feet={feet.y:0.000} support={under.point.y:0.000} on={under.collider.name} " +
                          $"drawn={drawn} grounded={body.IsGrounded} flying={body.IsFlying} carried={body.IsCarried} rest={body.RestsForMatchEnd}");
                Assert.IsFalse(body.IsFlying, $"{why}: seat {body.PlayerSlot} still flying under the board.");
                Assert.IsFalse(body.IsCarried, $"{why}: seat {body.PlayerSlot} still carried under the board.");
                Assert.IsTrue(body.IsGrounded, $"{why}: seat {body.PlayerSlot} is not standing on anything under the board.");
                Assert.Less(Mathf.Abs(feet.y - under.point.y), 0.35f, $"{why}: seat {body.PlayerSlot} is floating or sunk over {under.collider.name}.");
                Assert.IsTrue(drawn, $"{why}: seat {body.PlayerSlot} stands on {under.collider.name}, which nothing draws.");
            }
        }

        /// <summary>The nearest surface under the capsule's base (bodies, tsinelas and the can ignored).</summary>
        private static bool Support(CharacterMotor body, out Vector3 feet, out RaycastHit under)
        {
            var cc = body.GetComponent<CharacterController>();
            float radius = cc != null ? cc.radius * 0.9f : 0.25f;
            feet = cc != null ? body.transform.TransformPoint(cc.center - Vector3.up * (cc.height * 0.5f)) : body.transform.position;
            under = default;
            float best = float.PositiveInfinity;
            foreach (var hit in Physics.SphereCastAll(feet + Vector3.up * (radius + 0.3f), radius, Vector3.down, 3.0f, ~0, QueryTriggerInteraction.Ignore))
            {
                var c = hit.collider;
                if (c == null || c.GetComponentInParent<CharacterMotor>() != null || c.GetComponentInParent<Slipper>() != null
                    || c.GetComponentInParent<Lata>() != null || hit.distance <= 0.0f) continue;
                if (hit.distance < best) { best = hit.distance; under = hit; }
            }
            return !float.IsInfinity(best);
        }

        /// <summary>Something visible is at `point` (an invisible blocker would leave a body standing on air).</summary>
        private static bool Drawn(Vector3 point)
        {
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || r.forceRenderingOff || r is ParticleSystemRenderer
                    || r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly) continue;
                var b = r.bounds; b.Expand(0.3f);
                if (b.Contains(point)) return true;
            }
            return false;
        }

        /// <summary>
        /// Updraft held aloft, a wind carry with lift (the Airburst/Storm Surge shape) and a long fall with no support in
        /// reach, all at the timer's whistle. Every body must be standing on the court when the board is up.
        /// </summary>
        [UnityTest, Timeout(90000)]
        public IEnumerator AirborneBodiesAtTheFinalWhistleRestBeforeTheBoard()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            PinRules(1, 0);
            var result = Object.FindAnyObjectByType<MatchResult>();
            Assert.IsNotNull(result);
            MatchEndBeat.Revealed += Count;
            try
            {
                var flyer = GameServices.Round.PlayerAt(1);
                var carried = GameServices.Round.PlayerAt(2);
                var faller = GameServices.Round.PlayerAt(3);
                var standing = GameServices.Round.PlayerAt(0);
                float flyerFloor = flyer.transform.position.y, carriedFloor = carried.transform.position.y;
                float fallerFloor = faller.SpawnPosition.y;

                flyer.BeginFlight(AmihanRules.UpdraftHeight, AmihanRules.UpdraftRiseSeconds, AmihanRules.UpdraftDescentSpeed);
                faller.Teleport(faller.SpawnPosition + Vector3.up * 45.0f);
                yield return new WaitForSeconds(0.8f);
                carried.ApplyResolvedCarry(new Vector3(0.0f, 7.0f, 6.0f), 3.0f);
                yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                Assert.Greater(flyer.transform.position.y, flyerFloor + 2.0f, "Control: Updraft lifted the body.");
                Assert.IsTrue(carried.IsCarried, "Control: the wind carry took the body.");
                Assert.Greater(faller.transform.position.y, fallerFloor + 25.0f, "Control: the long fall began high.");
                int winner = GameServices.Match.WinningSlot;

                GameServices.Round.EndRound(); GameServices.Match.BeginIntermission();
                Assert.IsFalse(GameServices.Match.MatchInProgress, "The whistle locked the result.");
                Assert.IsTrue(GameServices.Match.HasCompleted);
                Assert.IsTrue(flyer.Intent.Parked && standing.Intent.Parked, "The whistle did not park the cast.");

                yield return Beat(result, winner);
                yield return AssertRested(new[] { flyer, carried, faller, standing }, "timer whistle");
                Assert.Less(Mathf.Abs(carried.transform.position.y - carriedFloor), 3.0f, "carry: thrown off the court.");
            }
            finally { MatchEndBeat.Revealed -= Count; }
        }

        /// <summary>A real AIRBURST released from the taya's mark at the whistle: everyone it lifts is down before the board.</summary>
        [UnityTest, Timeout(90000)]
        public IEnumerator AirburstReleasedAtTheWhistleSettlesBeforeTheBoard()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            PinRules(1, 0);
            var result = Object.FindAnyObjectByType<MatchResult>();
            MatchEndBeat.Revealed += Count;
            try
            {
                var round = GameServices.Round;
                var caster = round.PlayerAt(0);
                Vector3 toward = Vector3.zero;
                foreach (var body in round.Players) if (body != caster) toward += body.transform.position - caster.transform.position;
                toward.y = 0.0f; if (toward.sqrMagnitude < 1e-4f) toward = Vector3.forward;
                var storm = Abilities.AmihanStorm.Spawn(caster.transform.position, toward.normalized, caster.PlayerSlot, null);
                storm.Release(afterCutscene: true);
                yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                int winner = GameServices.Match.WinningSlot;

                round.EndRound(); GameServices.Match.BeginIntermission();
                yield return Beat(result, winner);
                yield return AssertRested(round.Players, "airburst whistle");
                if (storm != null) Object.Destroy(storm.gameObject);
            }
            finally { MatchEndBeat.Revealed -= Count; }
        }

        /// <summary>A score target reached in round one of three ends the match through the same beat.</summary>
        [UnityTest, Timeout(90000)]
        public IEnumerator ScoreTargetFinishHoldsTheSameBeat()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            int points = MatchRules.PointsFor(ScoreEvent.LataKnocked);
            PinRules(3, points);
            var result = Object.FindAnyObjectByType<MatchResult>();
            MatchEndBeat.Revealed += Count;
            try
            {
                var match = GameServices.Match;
                int scorer = (match.DefenderSlot + 1) % Balance.PlayerCount;
                match.AddScore(scorer, ScoreEvent.LataKnocked);
                Assert.IsTrue(match.MatchInProgress, "Control: the target ends the match at the round's end, not on the award.");
                GameServices.Round.EndRound(); match.BeginIntermission();
                Assert.IsFalse(match.MatchInProgress);
                Assert.Less(match.RoundNumber, match.TotalRounds, "Control: this ended on the target, before the last round.");
                Assert.IsFalse(HalftimePresentation.Playing, "A decided match opened a break.");
                yield return Beat(result, scorer);
            }
            finally { MatchEndBeat.Revealed -= Count; }
        }

        /// <summary>A tie at the top is a draw (-1), and a draw gets exactly the same beat.</summary>
        [UnityTest, Timeout(90000)]
        public IEnumerator DrawHoldsTheSameBeat()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            PinRules(1, 0);
            var result = Object.FindAnyObjectByType<MatchResult>();
            MatchEndBeat.Revealed += Count;
            try
            {
                var match = GameServices.Match;
                int a = (match.DefenderSlot + 1) % Balance.PlayerCount, b = (match.DefenderSlot + 2) % Balance.PlayerCount;
                match.AddScore(a, ScoreEvent.LataKnocked); match.AddScore(b, ScoreEvent.LataKnocked);
                // The round's first knockdown carries a bonus (`MatchDirector.HostScoreMoment`): the second scorer is given
                // the same award so the top is level.
                for (int i = 0; i < 4 && match.ScoreFor(b) < match.ScoreFor(a); i++) match.AddScore(b, ScoreEvent.FirstKnockdownBonus);
                Assert.AreEqual(match.ScoreFor(a), match.ScoreFor(b));
                Assert.Less(match.ScoreFor(match.DefenderSlot), match.ScoreFor(a), "Control: the taya is not ahead of the tie.");
                Assert.AreEqual(-1, match.WinningSlot, "Control: the scores are a draw.");
                GameServices.Round.EndRound(); match.BeginIntermission();
                yield return Beat(result, -1);
            }
            finally { MatchEndBeat.Revealed -= Count; }
        }

        /// <summary>
        /// A rematch from the board leaves nothing of the last ending in the next match: no beat, no banner, no board, the
        /// clock running and the new match's own whistle still free to start its own beat.
        /// </summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator RematchLeavesNothingOfTheLastEnding()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            PinRules(1, 0);
            var result = Object.FindAnyObjectByType<MatchResult>();
            MatchEndBeat.Revealed += Count;
            try
            {
                long first = GameServices.Match.PresentationMatchId;
                GameServices.Round.EndRound(); GameServices.Match.BeginIntermission();
                yield return Beat(result, GameServices.Match.WinningSlot);
                result.RequestRematch();
                // Offline, REMATCH loads the next court; it is begun the way the fixture began the first.
                float until = Time.realtimeSinceStartup + 60.0f;
                while (Time.realtimeSinceStartup < until && (GameServices.Match == null || !GameServices.Match.MatchInProgress
                       || GameServices.Match.PresentationMatchId == first || GameServices.Round == null || !GameServices.Round.RoundActive))
                {
                    var runner = Object.FindFirstObjectByType<SliceRunner>();
                    if (runner != null && !runner.Running && GameServices.Round != null && GameServices.Round.Players.Count >= Balance.PlayerCount) runner.Begin();
                    var gate = Object.FindFirstObjectByType<ReadyGate>();
                    if (gate != null && gate.AwaitingReady && !gate.CountingDown) gate.StartLocalCountdown();
                    yield return null;
                }
                Assert.IsTrue(GameServices.Match != null && GameServices.Match.MatchInProgress, "The rematch never started.");
                Assert.AreNotEqual(first, GameServices.Match.PresentationMatchId);
                Assert.IsFalse(MatchEndBeat.Holding, "The last match's beat survived into the rematch.");
                Assert.IsFalse(MatchEndBeat.Instance != null && MatchEndBeat.Instance.Active, "A beat is active in the new match.");
                Assert.IsFalse(Hud.MatchOverShowing, "MATCH OVER survived into the rematch.");
                Assert.IsFalse(MatchResult.BoardShowing, "The board survived into the rematch.");
                Assert.AreEqual(1.0f, PresentationClock.RequestedScale, "The board's stopped clock survived into the rematch.");
            }
            finally { MatchEndBeat.Revealed -= Count; }
        }
    }
}
