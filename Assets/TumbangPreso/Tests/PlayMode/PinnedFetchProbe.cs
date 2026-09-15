using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// C1 regression, from the traced Ilalim outlier rather than from an idea of one.
    ///
    /// `BotBehaviourProbe` on the C1-only build, Hero Strike on Ilalim ng Tulay seed 4242: seat 0
    /// stood in Fetch at (-3.34, 0.08, 9.90) pressing left for 77 seconds while its shoe lay loose
    /// at (-8.30, 0.26, 11.92), and was charged 68 unretrieved-slipper penalties. Its reported
    /// velocity stayed at walking speed, so `AIController.StepUnstick` never fired
    /// (`AiStuckWatch` has the cause). This puts one attacker bot and its shoe in exactly those
    /// places, parks every other seat, steps at 1/60 s with time scale 1 and requires the bot to
    /// hold its own shoe before the tournament grace period runs out.
    /// </summary>
    public sealed class PinnedFetchProbe
    {
        private const float Step = 1.0f / 60.0f;
        private static readonly Vector3 PinnedAt = new Vector3(-3.34f, 0.08f, 9.90f);
        private static readonly Vector3 ShoeAt = new Vector3(-8.30f, 0.26f, 11.92f);

        private bool _bots, _spectator, _pinned;
        private int _seat;
        private CustomRules _rules;

        [UnitySetUp] public IEnumerator Before()
        {
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            _pinned = SceneFlow.RulesPinned; _rules = SceneFlow.SelectedRules.Clone();
            yield return PlayModeWorld.Reset();
        }

        [UnityTearDown] public IEnumerator After()
        {
            Time.captureDeltaTime = 0.0f;
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator ABotPressedAgainstTheIlalimPillarStillFetchesItsShoe()
        {
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameLaunch.AllBots = true; GameLaunch.Spectator = false;
            yield return SceneManager.LoadSceneAsync("IlalimNgTulay");
            for (int i = 0; i < 25; i++) yield return null;

            var round = GameServices.Round;
            var runner = Object.FindFirstObjectByType<SliceRunner>();
            Assert.IsNotNull(round); Assert.IsNotNull(runner);
            runner.Begin();
            Time.captureDeltaTime = Step;
            for (int i = 0; i < 30; i++) yield return null;

            CharacterMotor runnerBody = null;
            foreach (var p in round.Players)
                if (p != null && !p.IsDefender && runnerBody == null) runnerBody = p;
            Assert.IsNotNull(runnerBody, "No attacker seat to test.");

            // Every other seat is parked, so nothing but the geometry stands between the bot and its shoe.
            foreach (var p in round.Players)
            {
                if (p == null || p == runnerBody) continue;
                var brain = p.GetComponent<AIController>();
                if (brain != null) brain.enabled = false;
                p.Intent.Clear(); p.Intent.CommitFrame(); p.Intent.Parked = true;
                p.Teleport(new Vector3(6.0f, p.transform.position.y, -12.0f + 2.0f * p.PlayerSlot));
            }

            Slipper shoe = null;
            foreach (var s in Object.FindObjectsByType<Slipper>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (s.OwnerSlot == runnerBody.PlayerSlot) shoe = s;
            Assert.IsNotNull(shoe, "The attacker has no labelled shoe.");

            runnerBody.Teleport(PinnedAt);
            shoe.ApplySnapshotState(SlipperState.Loose, null, ShoeAt, Quaternion.identity, Vector3.zero,
                                    0.0f, SlipperAffinity.Normal, -1);
            yield return null;
            Assert.AreEqual(SlipperState.Loose, shoe.State, "The shoe could not be placed loose.");
            Assert.IsFalse(runnerBody.HoldingSlipper, "The attacker still holds a shoe.");

            var log = new StringBuilder();
            float held = -1.0f, longestStill = 0.0f, still = 0.0f;
            Vector3 last = runnerBody.transform.position;
            float limit = Balance.SlipperUnretrievedGracePeriod;
            for (float t = 0.0f; t < limit; t += Step)
            {
                yield return null;
                Vector3 now = runnerBody.transform.position;
                still = (new Vector2(now.x - last.x, now.z - last.z).magnitude < 0.002f) ? still + Step : 0.0f;
                longestStill = Mathf.Max(longestStill, still);
                last = now;
                if (Mathf.Repeat(t, 0.5f) < Step)
                    log.AppendLine($"t={t:F2} at={now} plan={runnerBody.GetComponent<AIController>()?.Plan} vel={runnerBody.Velocity} shoe={shoe.State}@{shoe.transform.position}");
                if (shoe.State == SlipperState.Held && shoe.Holder == runnerBody) { held = t; break; }
            }

            Time.captureDeltaTime = 0.0f;
            log.AppendLine($"held at {held:F2}s, longest motionless spell {longestStill:F2}s");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/pinned-fetch-probe.txt", log.ToString());
            Debug.Log(log.ToString());

            Assert.GreaterOrEqual(held, 0.0f,
                $"The bot did not retrieve its shoe within the {limit:F0} s grace period; longest motionless spell {longestStill:F2} s. See Logs/pinned-fetch-probe.txt.");
        }
    }
}
