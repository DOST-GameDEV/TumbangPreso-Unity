using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class ReplayArchiveRecoveryTests
    {
        private bool _allBots, _spectator, _pinned;
        private int _soloSeat;
        private Core.CustomRules _rules;

        [UnitySetUp] public IEnumerator Before()
        {
            _allBots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator;
            _soloSeat = GameLaunch.SoloSeat;
            _pinned = UI.SceneFlow.RulesPinned; _rules = UI.SceneFlow.SelectedRules.Clone();
            yield return PlayModeWorld.Reset();
        }

        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = _allBots; GameLaunch.Spectator = _spectator;
            GameLaunch.SoloSeat = _soloSeat;
            UI.SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) UI.SceneFlow.PinSelectedRules(_rules); else UI.SceneFlow.UnpinSelectedRules();
        }

        [UnityTest] public IEnumerator ReenabledArchiveRetainsANewActualCatch() => Catch(true);
        [UnityTest] public IEnumerator UninterruptedArchiveStillRetainsANewActualCatch() => Catch(false);

        private static IEnumerator Catch(bool interrupt)
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            var archive = Object.FindAnyObjectByType<MatchReplayArchive>();
            Assert.IsNotNull(archive);
            var round = GameServices.Round;
            foreach (var actor in round.Players)
                actor.Teleport(new Vector3(6, .12f, -6 + actor.PlayerSlot * 3));
            var defender = round.PlayerAt(0); var victim = round.PlayerAt(1);
            defender.Teleport(new Vector3(0, .12f, -4));
            victim.Teleport(new Vector3(0, .12f, -3));
            defender.transform.forward = Vector3.forward;
            if (interrupt)
            {
                archive.enabled = false;
                yield return null;
                archive.enabled = true;
            }
            // Build a complete fresh lead-in through the real history LateUpdate.
            yield return new WaitForSeconds(2.5f);
            Assert.IsTrue(defender.GetComponent<CombatVerbs>().HostResolvePunch(
                defender.transform.position, defender.transform.forward));
            yield return new WaitForSeconds(1.6f);
            Assert.AreEqual(1, archive.Clips.Count,
                "A fresh accepted catch was not retained after archive recovery: " + archive.LastSkip);
            Assert.IsTrue(RecordedMatchClip.TryDecode(archive.Clips[0].Bytes, out var clip, out var error), error);
            Assert.AreEqual("CATCH", clip.Reason); Assert.AreEqual(1, clip.Subject);
        }

        [UnityTest] public IEnumerator DisabledArchiveDoesNotRecordEvenWhenRebound()
        {
            yield return MapRetrievalProbe.Load("Eskinita");
            var archive = Object.FindAnyObjectByType<MatchReplayArchive>();
            var history = archive.GetComponent<MatchPoseHistory>();
            yield return new WaitForSeconds(.2f);
            archive.enabled = false;
            archive.Bind(history);
            var fields = (List<RecordedFieldFrame>)typeof(MatchReplayArchive)
                .GetField("_fields", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(archive);
            int count = fields.Count;
            yield return new WaitForSeconds(.2f);
            Assert.AreEqual(count, fields.Count, "A disabled archive still consumed live pose samples.");
        }
    }
}
