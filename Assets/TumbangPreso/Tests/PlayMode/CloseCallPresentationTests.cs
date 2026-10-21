using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Diagnostics;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class CloseCallPresentationTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        private static int Escapes => MatchHighlights.Log.Markers.Count(m => m.Kind == HighlightKind.CloseCall);

        [UnityTest]
        public IEnumerator EscapeRequiresAnUprightCanFacingThreatAndACompletedEpisode()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            var round = GameServices.Round; var taya = round.PlayerAt(0); var runner = round.PlayerAt(1);
            taya.Intent.Parked = false; runner.Intent.Parked = false;
            taya.Teleport(new Vector3(0, .1f, -3)); taya.transform.forward = Vector3.forward;
            runner.Teleport(new Vector3(0, .1f, -2));
            yield return new WaitForSeconds(2.1f);
            round.Lata.HostKnockDown(2); yield return null;
            MatchHighlights.BeginMatch();
            MatchHighlights.NoteRetrieval(1, .5f, 45);
            Assert.IsFalse(MatchHighlights.Log.Markers.Any(m => m.Kind == HighlightKind.ClutchRetrieval));
            runner.Teleport(new Vector3(0, .1f, -1.1f)); yield return null;
            Assert.AreEqual(0, Escapes, "A down can is not a tag threat.");
            round.Lata.HostRestore(); taya.transform.forward = Vector3.back;
            runner.Teleport(new Vector3(0, .1f, -2)); yield return null;
            runner.Teleport(new Vector3(0, .1f, -1.1f)); yield return null;
            Assert.AreEqual(0, Escapes, "Proximity behind a taya is not a legal punch opportunity.");
            taya.transform.forward = Vector3.forward;
            runner.Teleport(new Vector3(0, .1f, -1.75f)); yield return null;
            for (int i = 0; i < 4; i++)
            {
                runner.Teleport(new Vector3(0, .1f, i % 2 == 0 ? -1.65f : -1.75f)); yield return null;
            }
            Assert.AreEqual(0, Escapes, "Threshold jitter must remain a single unfinished episode.");
            runner.Teleport(new Vector3(0, .1f, -1.1f)); yield return null;
            Assert.AreEqual(1, Escapes);
            int score = GameServices.Match.ScoreFor(1);
            runner.Teleport(new Vector3(0, .1f, -2)); yield return null;
            round.ResolveTag(taya, runner); yield return null;
            Assert.AreEqual(1, Escapes, "Accepted tag teleport must not be counted as escape.");
            Assert.AreEqual(score, GameServices.Match.ScoreFor(1), "Recognition cannot award points.");
        }

        [UnityTest]
        public IEnumerator EvasionRunsBelongToOnePlayerAndResetOnACatch()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            MatchHighlights.BeginMatch();
            MatchHighlights.NoteCloseCall(1, 0, .5f);
            MatchHighlights.NoteCloseCall(2, 0, .5f);
            MatchHighlights.NoteCloseCall(3, 0, .5f);
            Assert.IsFalse(MatchHighlights.Log.Markers.Any(m => m.Kind == HighlightKind.EvasionRun));
            yield return new WaitForSeconds(1.55f);
            MatchHighlights.NoteCloseCall(1, 0, .5f);
            MatchHighlights.ResetEvasion(1);
            yield return new WaitForSeconds(1.55f);
            MatchHighlights.NoteCloseCall(1, 0, .5f);
            Assert.IsFalse(MatchHighlights.Log.Markers.Any(m => m.Kind == HighlightKind.EvasionRun));
            yield return new WaitForSeconds(1.55f);
            MatchHighlights.NoteCloseCall(1, 0, .5f);
            yield return new WaitForSeconds(1.55f);
            MatchHighlights.NoteCloseCall(1, 0, .5f);
            Assert.AreEqual(1, MatchHighlights.Log.Markers.Count(m => m.Kind == HighlightKind.EvasionRun && m.Actor == 1));
        }
    }
}
