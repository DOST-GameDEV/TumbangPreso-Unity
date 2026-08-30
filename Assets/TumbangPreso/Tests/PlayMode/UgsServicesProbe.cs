using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Does this build's UGS project actually answer? Relay allocates, Lobby creates.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE `UgsCheck` CANNOT ANSWER IT HEADLESSLY, AND NOT KNOWING THAT
    /// COST A RUN. `UnityServices.InitializeAsync` refuses outside Play Mode with "Unity Services
    /// can only be initialized in Play Mode", so `-executeMethod UgsCheck.Run` reports the project
    /// link and then FAILS the services step for a reason that has nothing to do with the project.
    /// Batch mode also has no Hub session token, so it cannot see the signed-in account either.
    /// The editor menu item is correct and needs a person sitting in front of it; this is the same
    /// three calls from the one context that is allowed to make them.
    ///
    /// ⚠️ IT IS DELIBERATELY NOT A CATEGORY-FREE TEST. It talks to a live service, so it is slow,
    /// it needs a network, and it spends real free-tier quota. `[Category("Ugs")]` keeps it out of
    /// the default PlayMode run for the same reason `WallClock` keeps `AiDiagnosticProbe` out.
    /// Run it on purpose, after a relink or when online play is suspected:
    ///
    ///   Unity.exe -batchmode -runTests -projectPath . -testPlatform PlayMode
    ///             -testCategory "Ugs" -testResults Logs/ugs.xml -logFile Logs/ugs.log
    ///
    /// ⚠️ AND `OnlineSignInProbe` PASSING IS NOT THIS. That one asserts the boot attempt happens
    /// and settles, which is true offline and true against a project with every service switched
    /// off. It answers "did we try", this answers "did the service say yes".
    /// </summary>
    [Category("Ugs")]
    public class UgsServicesProbe
    {
        /// <summary>
        /// ⚠️ AWAITED BY POLLING RATHER THAN BY `.Wait()`. The UGS calls post their continuations
        /// to Unity's synchronisation context, which only advances while frames are being pumped,
        /// so blocking the main thread on one deadlocks instead of timing out. `UgsCheck` records
        /// the same trap for the editor's update loop.
        /// </summary>
        private static IEnumerator Await(Task task, float timeoutSeconds = 30.0f)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!task.IsCompleted)
                throw new TimeoutException($"a UGS call did not answer inside {timeoutSeconds} s");
            if (task.IsFaulted)
                throw task.Exception?.GetBaseException() ?? new Exception("unknown UGS failure");
        }

        private static IEnumerator SignedIn()
        {
            Assert.IsNotEmpty(Application.cloudProjectId,
                "no cloudProjectId, so there is no project to ask. ProjectSettings.asset line 738.");

            if (UnityServices.State == ServicesInitializationState.Uninitialized)
                yield return Await(UnityServices.InitializeAsync());

            Assert.AreEqual(ServicesInitializationState.Initialized, UnityServices.State,
                "the project id resolves but services did not come up");

            if (!AuthenticationService.Instance.IsSignedIn)
                yield return Await(AuthenticationService.Instance.SignInAnonymouslyAsync());

            Assert.IsTrue(AuthenticationService.Instance.IsSignedIn,
                "anonymous sign-in failed, and every call below authenticates with its token");
        }

        /// <summary>
        /// ⚠️ ANONYMOUS IS NOT AN IDENTITY PROVIDER AND THERE IS NOTHING TO SWITCH ON FOR IT.
        /// The dashboard's Identity Providers page reading "You have no identity providers" is the
        /// correct healthy state, and it was briefly mistaken for a misconfiguration. Username and
        /// password IS a provider and does have to be added; that is what the account upgrade path
        /// needs, and this probe does not exercise it because doing so would create a real account.
        /// </summary>
        [UnityTest]
        public IEnumerator AnonymousSignInAnswersOnThisProject()
        {
            yield return SignedIn();
            Assert.IsNotEmpty(AuthenticationService.Instance.PlayerId);
            Debug.Log($"[UgsServicesProbe] project {Application.cloudProjectId} " +
                      $"signed in as {AuthenticationService.Instance.PlayerId}");
        }

        /// <summary>
        /// The same call `NetSession.StartRelayHostAsync` makes, with the same connection count,
        /// so a pass here means the host path itself is provisioned rather than merely reachable.
        /// </summary>
        [UnityTest]
        public IEnumerator RelayAllocatesForAHostOfThree()
        {
            yield return SignedIn();

            var allocationTask = RelayService.Instance.CreateAllocationAsync(3);
            yield return Await(allocationTask);

            var codeTask = RelayService.Instance.GetJoinCodeAsync(allocationTask.Result.AllocationId);
            yield return Await(codeTask);

            Assert.IsNotEmpty(codeTask.Result, "Relay did not return a join code");
            Debug.Log($"[UgsServicesProbe] relay join code {codeTask.Result}, allocation expires on its own");
        }

        /// <summary>
        /// The `player-account` Cloud Code endpoint answers a load for the signed-in player.
        ///
        /// ⚠️⚠️ IT NOW CALLS `Net.CloudCode`, WHICH IS THE CODE THE GAME CALLS, AND THAT IS A
        /// CHANGE FROM WHAT `docs/TODO.md` § 88.4 DESCRIBES. This test used to write the URL,
        /// the `params` envelope and the bearer header out by hand, because the game's copy was
        /// private and § 88.4 judged that widening it *"would put a seam in shipping code for
        /// one probe"*. It also named the cost: *"if the call shape drifts, the probe passes
        /// while the game fails, which is the worst outcome available."* Phase 2 needed a THIRD
        /// copy for the career endpoint, so the request moved into a shared helper the shipping
        /// code uses. Calling it from here is not a seam, and the drift § 88.4 feared is gone.
        ///
        /// ⚠️ A "load" IS THE SAFE VERB TO PROBE WITH. Save would write a real profile for the
        /// probe's throwaway anonymous player, and delete would exercise the destructive path
        /// against a live project. Load proves the deploy, the publish, the roles and the bearer
        /// token all line up, which is everything that can actually be misconfigured.
        /// </summary>
        [UnityTest]
        public IEnumerator TheAccountEndpointAnswersALoad()
        {
            yield return SignedIn();

            var call = Net.CloudCode.CallAsync(
                "player-account", new { action = "load", profile = "" });
            yield return Await(call);

            // An empty profile is the correct answer for a player who has never saved one, so
            // the assertion is that the endpoint ANSWERED rather than that it had something.
            Assert.IsNotNull(call.Result,
                "player-account returned no output. 404 means it is not deployed or not " +
                "published; 403 means the service-account roles or the player token are wrong.");
            Debug.Log($"[UgsServicesProbe] player-account answered: {call.Result}");
        }

        /// <summary>
        /// The `match-record` endpoint answers a load for the signed-in player.
        ///
        /// ⚠️⚠️ THIS IS PHASE 2'S HALF OF THE SAME PROOF AND IT IS THE ONLY THING THAT CATCHES A
        /// SCRIPT THAT WAS WRITTEN BUT NEVER DEPLOYED. `ugs deploy` publishes what is in the
        /// folder; a career that silently never uploads looks exactly like a career nobody has
        /// played yet, because `CareerStore` is built to keep a local profile when the service
        /// is unreachable and to say so quietly rather than to stop the game.
        ///
        /// ⚠️ IT PROBES WITH "load" FOR THE SAME REASON THE ACCOUNT TEST DOES. A "submit" would
        /// write a real career document for the probe's throwaway anonymous player.
        /// </summary>
        [UnityTest]
        public IEnumerator TheCareerEndpointAnswersALoad()
        {
            yield return SignedIn();

            var call = Net.CloudCode.CallAsync(
                Net.CareerStore.ScriptName, new { action = "load" });
            yield return Await(call);

            Assert.IsNotNull(call.Result,
                $"{Net.CareerStore.ScriptName} returned no output. Run " +
                "`ugs deploy ugs/cloud-code` and check `ugs cloud-code scripts list`.");
            Debug.Log($"[UgsServicesProbe] {Net.CareerStore.ScriptName} answered: {call.Result}");
        }

        /// <summary>
        /// The `telemetry` endpoint answers a report for the signed-in player.
        ///
        /// ⚠️⚠️ "report" IS THIS SCRIPT'S SAFE VERB, THE WAY "load" IS THE OTHER TWO'S. A submit
        /// would write real counters and a real funnel step for the probe's throwaway anonymous
        /// player, and the funnel is the one thing in the whole design that is deliberately
        /// impossible to un-record: the first timestamp wins, forever, by construction. A probe
        /// that dirtied it would put a fake first-launch into the number `FUTURE.md` § 3 calls
        /// the most valuable in the plan.
        ///
        /// ⚠️ IT IS ALSO THE ONLY THING THAT CATCHES A TELEMETRY SCRIPT THAT WAS WRITTEN AND
        /// NEVER DEPLOYED, for the same reason the career test is: `TelemetrySink` swallows a
        /// failed batch on purpose, so a game sending into nothing looks exactly like a game
        /// nobody has played.
        /// </summary>
        [UnityTest]
        public IEnumerator TheTelemetryEndpointAcceptsABatchWithoutTouchingTheFunnel()
        {
            yield return SignedIn();

            // ⚠️⚠️ ONE NON-FUNNEL EVENT, AND THE `accepted` COUNT IS THE ASSERTION. Anything that
            // only checks "it answered" would have passed throughout the whole of § 90.5, when
            // the action was being stripped and every call landed on the default branch. This
            // asserts a number only the intended branch can produce.
            string batch = "[{\"Name\":\"session_start\",\"Count\":1,\"Params\":{}}]";
            var call = Net.CloudCode.CallAsync(
                "telemetry", new { action = "submit", events = batch });
            yield return Await(call);

            Assert.IsNotNull(call.Result,
                "telemetry returned no output. Run `ugs deploy ugs/cloud-code` and check " +
                "`ugs cloud-code scripts list`.");

            string flat = call.Result.Replace(" ", "");
            StringAssert.Contains("\"accepted\":1", flat,
                "the telemetry endpoint did not accept a known event. If `refused` is 1 instead, " +
                "the event lists in TelemetryEvents and telemetry.js have split; if the whole " +
                "shape is wrong, the parameters are being stripped again. docs/TODO.md § 90.5.");
            StringAssert.Contains("\"funnel\":{}", flat,
                "a probe submission recorded a funnel step. The funnel is deliberately impossible " +
                "to un-record, so a probe must never send one: this batch carries `session_start` " +
                "for exactly that reason. docs/TODO.md § 90.3.");

            Debug.Log($"[UgsServicesProbe] telemetry accepted a batch: {call.Result}");
        }

        /// <summary>
        /// ⚠️⚠️ A NEW EVENT NAME IS PROBED AGAINST THE SERVICE, NOT AGAINST THE FILE ON DISK.
        /// `CareerAndCloudCodeTests` compares `TelemetryEvents.All` with the TEXT of
        /// `telemetry.js`, which proves the two lists agree in the REPOSITORY and says nothing
        /// about what the service is running. `docs/TODO.md` § 90.5 is an entry about a script
        /// that deployed, read correctly on disk, and behaved like a different file for two
        /// phases. A name the deployed script does not know is not an error: it is counted,
        /// sent, and thrown away into `refused`, which is why `refused` is asserted here rather
        /// than only `accepted`.
        ///
        /// ⚠️ IT SENDS ALL EIGHT COLUMNS ON PURPOSE. `match_frame_rate` sits exactly on
        /// `MAX_PARAMETERS_PER_EVENT`, and `columnsFrom` drops a ninth by insertion order, so the
        /// widest event in the game is the one worth proving arrives whole. `docs/TODO.md` § 90.7.
        ///
        /// ⚠️ AND `"funnel":{}` AGAIN, for the reason the test above it gives: a funnel step is
        /// impossible to un-record by construction, so no probe may ever send one. A frame-rate
        /// event is not a funnel step and this asserts that it did not become one.
        /// </summary>
        [UnityTest]
        public IEnumerator TheTelemetryEndpointAcceptsEveryColumnOfAFrameRateEvent()
        {
            yield return SignedIn();

            string batch =
                "[{\"Name\":\"match_frame_rate\",\"Count\":1,\"Params\":{" +
                "\"mode\":\"Classic\",\"map\":\"Eskinita\",\"band\":\"fps_60_90\"," +
                "\"fps_avg\":58.4,\"fps_p50\":59.7,\"fps_p5\":41.2,\"fps_p1\":22.9," +
                "\"frames\":21600}}]";

            var call = Net.CloudCode.CallAsync(
                "telemetry", new { action = "submit", events = batch });
            yield return Await(call);

            Assert.IsNotNull(call.Result,
                "telemetry returned no output. Run `ugs deploy ugs/cloud-code`.");

            string flat = call.Result.Replace(" ", "");
            StringAssert.Contains("\"refused\":0", flat,
                "the deployed telemetry.js does not know `match_frame_rate`. The repository's " +
                "copy and the service's copy have split: redeploy and check the result with " +
                "`ugs cloud-code scripts get telemetry`. docs/TODO.md § 90.5.");
            StringAssert.Contains("\"accepted\":1", flat,
                "the frame-rate event was not accepted.");
            StringAssert.Contains("\"funnel\":{}", flat,
                "a frame-rate submission recorded a funnel step, which it must never do.");

            Debug.Log($"[UgsServicesProbe] telemetry accepted a frame-rate event: {call.Result}");
        }

        /// <summary>
        /// The impersonation guard, end to end, against the live endpoint. `docs/TODO.md` § 88.1c.
        ///
        /// ⚠️⚠️ THIS IS THE ONLY TEST IN THE PROJECT THAT CAN PROVE THE GUARD REFUSES ANYTHING.
        /// The core tests assert what the rule DOES with each answer; only a live call proves the
        /// endpoint gives the right answer, because the whole mechanism is a value minted by one
        /// authenticated session and checked from another.
        ///
        /// ⚠️ IT ONLY EVER ASSERTS THE REFUSAL, AND THAT IS DELIBERATE. A probe player has never
        /// saved a profile, so `attest` correctly has no handle to vouch for and mints nothing;
        /// making it mint one would mean writing a real account profile for a throwaway player,
        /// which is exactly what `TheAccountEndpointAnswersALoad` refuses to do. A made-up proof
        /// being rejected is the half that fails open if the endpoint is wrong, so it is the half
        /// worth gating on: an endpoint that answered `owned` here would be waving every impostor
        /// through, which is the fault § 88.1c exists for.
        /// </summary>
        [UnityTest]
        public IEnumerator TheAccountEndpointRefusesAHandleProofItNeverMinted()
        {
            yield return SignedIn();

            var call = Net.CloudCode.CallAsync("player-account", new
            {
                action = "verify",
                playerId = AuthenticationService.Instance.PlayerId,
                proof = "this-proof-was-never-minted",
            });
            yield return Await(call);

            Assert.IsNotNull(call.Result, "player-account did not answer a verify");
            StringAssert.Contains("\"owned\":false", call.Result.Replace(" ", ""),
                "the account endpoint vouched for a proof it never minted, which is the " +
                "impersonation guard failing open. docs/TODO.md § 88.1c.");
            Debug.Log($"[UgsServicesProbe] player-account verify refused a forged proof: {call.Result}");
        }

        [UnityTest]
        public IEnumerator LobbyCreatesAndIsCleanedUp()
        {
            yield return SignedIn();

            var options = new CreateLobbyOptions
            {
                IsPrivate = true,
                Data = new Dictionary<string, DataObject>
                {
                    { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, "TEST") }
                }
            };

            var createTask = LobbyService.Instance.CreateLobbyAsync("ugs-probe", 4, options);
            yield return Await(createTask);

            string lobbyId = createTask.Result?.Id;
            Assert.IsNotEmpty(lobbyId, "Lobby did not return an id");

            // ⚠️ DELETED IN THE SAME TEST, NOT IN A TEARDOWN. A private probe lobby left behind
            // counts against the project's live lobby quota until its heartbeat lapses, and it
            // would appear in a browse. `UgsCheck` deletes its own for the same reason.
            var deleteTask = LobbyService.Instance.DeleteLobbyAsync(lobbyId);
            yield return Await(deleteTask);
        }
    }
}
