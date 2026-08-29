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
