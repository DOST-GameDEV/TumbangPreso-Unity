using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    // Observes four real bot input writers together. It neither injects successful
    // outcomes nor asserts that random free play must produce a particular story.
    public sealed class BusyExchangeProbe
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        [UnityTest, Timeout(90000)] public IEnumerator ClassicWithFourActivePlayers() => Observe(GameMode.Classic);
        [UnityTest, Timeout(90000)] public IEnumerator HeroStrikeWithFourActivePlayers() => Observe(GameMode.HeroStrike);
        private static IEnumerator Observe(GameMode mode)
        {
            yield return MapRetrievalProbe.Load(SceneFlow.BayanPlaza, mode);
            var round = GameServices.Round; var subject = round.PlayerAt(2);
            var rig = Camera.main.GetComponent<CameraRig>(); rig.Follow(subject, true);
            var witness = new GameObject("BusyExchangeWitness").AddComponent<Camera>();
            witness.CopyFrom(Camera.main); witness.enabled = false; witness.tag = "Untagged";
            witness.cullingMask &= ~(1 << 5); witness.fieldOfView = 60;
            witness.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var travel = new float[Balance.PlayerCount]; var previous = new Vector3[Balance.PlayerCount];
            foreach (var player in round.Players)
            {
                player.Intent.Parked = false;
                var brain = player.GetComponent<AIController>() ?? player.gameObject.AddComponent<AIController>();
                brain.enabled = true; previous[player.PlayerSlot] = player.transform.position;
            }
            var events = new StringBuilder("game_seconds,kind,actor,subject,x,y,z\n");
            int acceptedThrows = 0;
            bool upright = round.Lata.IsUpright;
            var heldStates = round.Players.Select(p => p.HoldingSlipper).ToArray();
            void Event(MatchFlair.Kind kind, int actor, int target, Vector3 at, float strength)
            {
                if (kind == MatchFlair.Kind.Throw) acceptedThrows++;
                events.AppendLine(FormattableString.Invariant($"{Time.time:F4},{kind},{actor},{target},{at.x:F3},{at.y:F3},{at.z:F3}"));
            }
            MatchFlair.Presented += Event;
            string output = Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs/improvement-baseline-v1";
            string name = mode + "-busy-exchange";
            try
            {
                yield return ImprovementEvidenceProbe.Record(witness, name, 35, subject, _ =>
                {
                    if (round.Lata.IsUpright != upright)
                    {
                        upright = round.Lata.IsUpright;
                        events.AppendLine(FormattableString.Invariant($"{Time.time:F4},{(upright ? "Restore" : "CanDownState")},0,-1,0,0,0"));
                    }
                    foreach (var player in round.Players)
                    {
                        int seat = player.PlayerSlot;
                        if (heldStates[seat] != player.HoldingSlipper)
                        {
                            heldStates[seat] = player.HoldingSlipper;
                            events.AppendLine(FormattableString.Invariant($"{Time.time:F4},{(heldStates[seat] ? "Possession" : "Released")},{seat},-1,0,0,0"));
                        }
                        float step = Vector3.Distance(previous[seat], player.transform.position);
                        if (step < 1) travel[seat] += step; // Exclude catch teleport from travel.
                        previous[seat] = player.transform.position;
                    }
                }, new Vector3(3, 2.2f, -4), 1);
                Assert.AreEqual(4, round.Players.Count);
                Assert.IsTrue(travel.All(metres => metres > 2), "Every participant must actually move in the busy observation.");
                Assert.Greater(acceptedThrows, 0, "The busy observation produced no actual releases.");
            }
            finally
            {
                MatchFlair.Presented -= Event;
                Directory.CreateDirectory(Path.Combine(output, name));
                File.WriteAllText(Path.Combine(output, name, "events.csv"), events.ToString());
                File.WriteAllText(Path.Combine(output, name, "participants.txt"), string.Join("\n", travel.Select((m, i) => $"P{i + 1}: {m:F2} metres")));
                Object.Destroy(witness.gameObject);
            }
        }
    }
}
