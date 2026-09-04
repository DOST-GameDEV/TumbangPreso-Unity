using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The same question `LobbyTypingProbe` asks, asked of a lobby with a REAL HOST LISTENING.
    ///
    /// ⚠️⚠️ THIS IS THE GAP `docs/TODO.md` § 72 NAMES IN SO MANY WORDS, AND IT IS THE ONLY ONE
    /// LEFT. 🧑 2026-08-29: *"sa lobby hindi nagana yung player name, hindi makapag input ng
    /// name (singleplayer)"* and *"hindi maka input ng code and lobby code sa lobby"*. Both
    /// reproduce for the player. Neither reproduces headlessly, and § 72 lists four causes ruled
    /// out one at a time: nothing covers either control (`UiClickProbe` widened to `InputField`),
    /// both take and keep the caret (`LobbyTypingProbe`), legacy input is on, and `LobbyChat`'s
    /// focus grab is unreachable without typing in the chat first.
    ///
    /// ⚠️⚠️ WHAT EVERY ONE OF THOSE SHARES IS `SceneFlow.Networked == false`. Both existing
    /// probes run a lobby that has never hosted and never joined, and § 72's closing line says
    /// the untested half is *"a live NETWORKED lobby (the probes never host or join)"*. So this
    /// probe changes exactly one thing and holds everything else fixed: it starts a real host
    /// through `NetSession.StartHostAsync`, sets `SceneFlow.Networked`, and then runs
    /// `LobbyTypingProbe.Check` itself rather than a copy of it. If the fields die here and live
    /// there, the difference is the cause and the report names what took the caret.
    ///
    /// ⚠️ IT IS A MEASUREMENT FIRST AND A REGRESSION TEST SECOND, WHICH IS WHY IT IS WORTH
    /// LANDING EVEN IF IT PASSES. § 72 has been argued from a source read for two sessions; a
    /// green run here turns "we cannot reproduce it" from a statement about effort into a
    /// statement about what has actually been driven, and narrows what is left to the built
    /// player as opposed to the editor. `docs/TODO.md` § 76 makes the same argument for the
    /// tutorial can.
    ///
    /// ⚠️⚠️ IT MUST PUT `SceneFlow.Networked` AND THE SESSION BACK, WHATEVER HAPPENS. Both are
    /// process-wide statics that outlive a test: `LobbySession` is the one object § 38.12 records
    /// surviving every session with nothing resetting it, and a host left listening makes every
    /// later PlayMode test in the same run start against a live network. The teardown runs from
    /// `[UnityTearDown]` rather than from the end of the test body so an assertion failure cannot
    /// skip it.
    /// </summary>
    public class NetworkedLobbyTypingProbe
    {
        /// <summary>
        /// ⚠️⚠️ THE SETUP HALF OF `docs/TODO.md` § 126.8'S FIX, AND THIS FIXTURE GETS ONLY THE
        /// SETUP HALF ON PURPOSE. `PlayModeWorld`'s header asks for both hooks; this class
        /// already owns a `[UnityTearDown]` doing its own cleanup, and NUnit does not define an
        /// order between two teardowns of the same kind. **The setup reset is the half that
        /// protects THIS fixture**: it guarantees the world is empty and settled when the test
        /// below starts, whatever ran before it. With every fixture in the folder carrying it,
        /// no test can inherit a world at all, which is the property the entry actually wants.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        private const string OutPath = "Logs/lobby-typing-networked.txt";

        /// <summary>See `UiClickProbe.SettleFrames`, for the same reason.</summary>
        private const int SettleFrames = 120;

        private bool _previousNetworked;
        private bool _started;

        private static IEnumerator Await(System.Threading.Tasks.Task<bool> task, Action<bool> onDone)
        {
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception;
            onDone(task.Result);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneFlow.Networked = _previousNetworked;

            if (_started)
            {
                NetSession.Instance?.Stop();

                // ⚠️ `Shutdown` IS DEFERRED, AND `SessionRestartTests` IS THE TEST THAT PROVES IT.
                // Returning while the manager is still listening leaves the next test in this run
                // starting against a session that is on its way down, which is the same
                // same-frame fault § 65.1 records from the other direction.
                yield return new WaitForSecondsRealtime(0.4f);
                _started = false;
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryLobbyFieldTakesTheCaretWithAHostListening()
        {
            var report = new StringBuilder();
            var broken = new List<string>();

            if (!Application.CanStreamedLevelBeLoaded("MatchSetup"))
            {
                Assert.Ignore("MatchSetup is not in the build settings.");
                yield break;
            }

            _previousNetworked = SceneFlow.Networked;

            var net = NetSession.Ensure();
            yield return null;

            bool hosting = false;
            yield return Await(net.StartHostAsync(), r => hosting = r);

            if (!hosting)
            {
                // ⚠️ IGNORED RATHER THAN FAILED, AND ONLY FOR THIS. A batch runner with no
                // loopback socket cannot answer the question this probe exists to ask, and a red
                // test that means "the machine could not host" teaches a reader to skim the
                // failure list, which is the state § 71.7 records the protocol tripwire rotting
                // in. A field that is genuinely dead still fails below.
                Assert.Ignore("could not start a host in this environment: " + net.Status);
                yield break;
            }

            _started = true;
            yield return new WaitForSecondsRealtime(0.3f);

            Assert.IsTrue(net.IsNetworked, "the host should be listening before the lobby loads");

            // ⚠️ SET AFTER THE HOST IS UP, NOT BEFORE. `SceneFlow.Networked` is what the lobby
            // reads to decide it is a room rather than a form, and setting it while nothing is
            // listening builds the networked chrome over a dead session, which is neither of the
            // two states a player is ever in.
            SceneFlow.Networked = true;

            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < SettleFrames; i++) yield return null;
            Canvas.ForceUpdateCanvases();

            report.AppendLine("networked lobby: host listening, SceneFlow.Networked = true");
            report.AppendLine(Inventory());

            yield return LobbyTypingProbe.Check("networked lobby", report, broken);

            var join = LobbyTypingProbe.FindByName("LobbyJoinPanel");

            if (join == null)
            {
                report.AppendLine("LobbyJoinPanel: NOT PRESENT");
            }
            else
            {
                join.SetActive(true);
                for (int i = 0; i < SettleFrames; i++) yield return null;
                Canvas.ForceUpdateCanvases();

                yield return LobbyTypingProbe.Check("networked join card", report, broken);
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText(OutPath, report.ToString(), new UTF8Encoding(false));
            Debug.Log(report.ToString());

            Assert.IsEmpty(broken,
                "fields a player cannot type into with a host listening:\n"
                + string.Join("\n", broken));
        }

        /// <summary>
        /// What is actually on screen, written into the report whether or not anything failed.
        ///
        /// ⚠️ THE INVENTORY IS THE HALF THAT SURVIVES A GREEN RUN. § 72's leading suspect is
        /// `LobbyChat` taking focus, and the one thing a passing selection check cannot tell the
        /// next reader is whether the chat field was even PRESENT while it passed. A pass with no
        /// chat in the scene rules out nothing; a pass with the chat live rules out the suspect.
        /// </summary>
        private static string Inventory()
        {
            var fields = UnityEngine.Object
                .FindObjectsByType<UnityEngine.UI.InputField>(FindObjectsInactive.Include,
                                                              FindObjectsSortMode.None)
                .Select(f => $"{f.name}({(f.gameObject.activeInHierarchy ? "active" : "inactive")})")
                .ToArray();

            var chat = UnityEngine.Object.FindFirstObjectByType<LobbyChat>(FindObjectsInactive.Include);

            return "   fields: " + (fields.Length == 0 ? "none" : string.Join(", ", fields))
                   + "\n   LobbyChat: "
                   + (chat == null
                       ? "not in the scene"
                       : (chat.gameObject.activeInHierarchy ? "present and active" : "present but inactive"));
        }
    }
}
