using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Diagnostics;
using UnityEngine;

namespace TumbangPreso.Tests
{
    public sealed class FailureBundleAbandonTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void NetworkDiagnosticsRetainTheRecordedAbandonmentAfterSimulationRetires(bool abandoned)
        {
            Assert.IsTrue(Environment.GetCommandLineArgs().Contains("-tp-profile"));
            var retained = new Dictionary<PropertyInfo, object>();
            foreach (string name in new[] { "Cause", "RawReason", "RoundNumber", "TotalRounds", "AuthorityRevoked" })
            {
                var property = typeof(MatchAbandon).GetProperty(name);
                retained[property] = property.GetValue(null);
            }
            var originalMatch = GameServices.Match;
            GameObject root = null;
            try
            {
                MatchAbandon.Forget();
                string expected = null;
                const string privateReason = "timeout private-session-marker";
                if (abandoned)
                {
                    root = new GameObject("Dormant diagnostic match"); root.SetActive(false);
                    var match = root.AddComponent<MatchDirector>();
                    typeof(GameServices).GetProperty("Match").SetValue(null, match);
                    match.ApplySnapshot(new[] { 10, 20, 30, 40 }, 3, true);
                    int total = match.TotalRounds;
                    MatchAbandon.Note(privateReason, false);
                    Assert.AreNotEqual(SessionEndCause.None, MatchAbandon.Cause);
                    Assert.AreEqual(3, MatchAbandon.RoundNumber);
                    Assert.AreEqual(total, MatchAbandon.TotalRounds);
                    expected = MatchAbandon.Diagnostic;

                    match.ResetForNewMatch();
                    MatchAbandon.Clear();
                    Assert.AreEqual(0, match.RoundNumber);
                    Assert.IsFalse(match.MatchInProgress);
                    Assert.IsFalse(MatchAbandon.AuthorityRevoked);
                    Assert.AreEqual(expected, MatchAbandon.Diagnostic, "Simulation retirement erased the recorded failure context.");
                }

                // This is the exact formatter used by Write's NETWORK section, without
                // creating a diagnostic file in the player's normal persistentDataPath.
                string summary = (string)typeof(FailureBundle).GetMethod("NetworkSummary",
                    BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                if (abandoned) StringAssert.Contains(expected, summary,
                    "The bundle lost the classified cause and original round after live state reset.");
                else StringAssert.DoesNotContain("session end", summary, "A normal session acquired an abandonment diagnostic.");
                StringAssert.DoesNotContain("private-session-marker", summary, "Raw host-authored reason entered the failure bundle.");
            }
            finally
            {
                typeof(GameServices).GetProperty("Match").SetValue(null, originalMatch);
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                foreach (var entry in retained) entry.Key.SetValue(null, entry.Value);
            }
        }
    }
}
