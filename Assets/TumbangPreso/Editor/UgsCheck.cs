using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Measures whether Unity Gaming Services is actually wired up, instead of trusting that
    /// somebody clicked through the dashboard.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE THE UGS SETUP STEPS ARE THE ONE PART OF THE PORT NO SCRIPT CAN
    /// DO. Signing in, linking a cloud project, enabling Anonymous authentication, Relay, Lobby
    /// and Multiplay Hosting, and adding billing all happen in the Unity account, behind a
    /// password. N0 step 4 asked for them, they were never done, and nothing in the repository
    /// noticed: `ServerQuery` swallows the failure and `NetIdentity` falls back to a local
    /// token, which is correct behaviour for a LAN venue with no internet and completely
    /// indistinguishable from an unconfigured project. So the state was assumed for a week.
    ///
    /// ⚠️ EVERY CHECK HERE IS A LIVE CALL, NOT A SETTING READ. A dashboard toggle leaves no
    /// trace in the repository, so the only honest way to answer "is Anonymous sign-in on" is
    /// to sign in anonymously and report what came back. The service errors are printed
    /// verbatim for the same reason: the message names which toggle is off.
    ///
    /// Run headless (note: NO -quit, the check exits itself once the async calls return):
    ///   Unity.exe -batchmode -nographics -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.UgsCheck.Run -logFile -
    ///
    /// Or from an open editor: Tumbang Preso > Check UGS Wiring.
    /// </summary>
    public static class UgsCheck
    {
        /// <summary>
        /// ⚠️ RESULTS GO TO A FILE. Same reason as HeadlessCheck: EditorApplication.Exit kills
        /// the process before Unity flushes its log buffer, so a check that only logged would
        /// leave no evidence it ran at all.
        /// </summary>
        private const string ResultPath = "Logs/ugs-check.txt";

        /// <summary>
        /// A run that hangs is worse than a run that fails, because a hung batchmode editor
        /// holds the project lock and the NEXT launch then silently does nothing.
        /// </summary>
        private const double TimeoutSeconds = 90.0;

        private static readonly StringBuilder Report = new StringBuilder();
        private static int _failures;

        [MenuItem("Tumbang Preso/Check UGS Wiring")]
        public static void RunFromMenu() => Start(false);

        public static void Run() => Start(true);

        private static void Start(bool exitWhenDone)
        {
            Report.Clear();
            _failures = 0;

            Task<bool> work = CheckAsync();
            double deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;

            // ⚠️ POLLED FROM EditorApplication.update RATHER THAN task.Wait(). The UGS calls
            // post their continuations to Unity's synchronisation context, which is pumped by
            // the editor loop. Blocking the main thread on the task therefore deadlocks: the
            // thread that would run the continuation is the thread waiting for it.
            void Pump()
            {
                if (!work.IsCompleted && EditorApplication.timeSinceStartup < deadline) return;

                EditorApplication.update -= Pump;

                if (!work.IsCompleted)
                {
                    Report.AppendLine($"FAIL : timed out after {TimeoutSeconds:0} s with calls still in flight");
                    _failures++;
                }
                else if (work.IsFaulted)
                {
                    Report.AppendLine($"THREW: {work.Exception?.GetBaseException()}");
                    _failures++;
                }

                Report.AppendLine(_failures > 0
                    ? $"RESULT: {_failures} step(s) not done. The failing lines name what to do."
                    : "RESULT: OK. Sign-in, project link, Anonymous auth, Relay and Lobby all answered.");

                Flush();
                Debug.Log(Report.ToString());

                if (exitWhenDone) EditorApplication.Exit(_failures > 0 ? 1 : 0);
            }

            EditorApplication.update += Pump;
        }

        private static async Task<bool> CheckAsync()
        {
            // ---- Step 1 · the editor is signed in to a Unity account -------------------
            // ⚠️ BATCHMODE CANNOT ANSWER THIS AND MUST NOT PRETEND TO. The editor's access
            // token is handed in by the Hub as -accessToken when a person launches it. A
            // headless run has no Hub session, so the token is empty whether or not the
            // account is signed in, and the first version of this check told a signed-in user
            // to go and sign in. Unknown is reported as unknown; the services call below
            // proves the account for real, because an unauthenticated editor cannot reach them.
            string accessToken = CloudProjectSettings.accessToken;
            bool signedIn = !string.IsNullOrEmpty(accessToken);

            if (signedIn)
            {
                Check("step 1, editor signed in to a Unity account", true, "");
                Report.AppendLine($"       user: {CloudProjectSettings.userName}");
            }
            else if (Application.isBatchMode)
            {
                Report.AppendLine("?    : step 1, sign-in not observable in batchmode (no Hub session token).");
                Report.AppendLine("       Run Tumbang Preso > Check UGS Wiring from an open editor to see it,");
                Report.AppendLine("       or read it off step 3 below, which cannot pass while signed out.");
            }
            else
            {
                Check("step 1, editor signed in to a Unity account", false,
                    "sign in from the account menu top right of the editor, or in Unity Hub");
            }

            // ---- Step 2 · a cloud project is linked ------------------------------------
            // ⚠️ READ OFF ProjectSettings.asset, NOT off CloudProjectSettings.organizationId.
            // That property is deprecated in Unity 6, and this is the file the link actually
            // writes, so the file is both the more stable and the more honest source.
            string cloudProjectId = ReadProjectSetting("cloudProjectId");
            string organizationId = ReadProjectSetting("organizationId");
            string projectName = ReadProjectSetting("projectName");

            bool linked = !string.IsNullOrEmpty(cloudProjectId);
            Check("step 2, a UGS project is linked (cloudProjectId is written)", linked,
                "Project Settings > Services, create or select a project");
            Report.AppendLine($"       cloudProjectId : {Show(cloudProjectId)}");
            Report.AppendLine($"       organizationId : {Show(organizationId)}");
            Report.AppendLine($"       projectName    : {Show(projectName)}");

            if (!linked)
            {
                // Everything below needs the project id to route to. Reporting four more
                // failures caused by one missing link would just bury the one that matters.
                Report.AppendLine("skip : steps 3 to 5 need the project link first");
                return false;
            }

            // ---- Step 3a · services core initialises ----------------------------------
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                    await UnityServices.InitializeAsync();

                Check("step 3, UnityServices initialised", UnityServices.State == ServicesInitializationState.Initialized,
                    "the project id resolves but the services did not come up");
            }
            catch (Exception e)
            {
                Check("step 3, UnityServices initialised", false, e.Message);
                return false;
            }

            // ---- Step 3b · Anonymous sign-in is enabled --------------------------------
            // ⚠️ THIS IS THE ONE THAT IS OFF BY DEFAULT and the single most likely thing to be
            // missed. Every other service call below is authenticated with the token it mints,
            // so when this is off, Relay and Lobby fail with an unrelated-looking 401.
            try
            {
                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();

                Check("step 3, Anonymous sign-in enabled", AuthenticationService.Instance.IsSignedIn,
                    "Dashboard > Authentication > enable Anonymous sign-in");
                Report.AppendLine($"       PlayerId: {AuthenticationService.Instance.PlayerId}");
            }
            catch (Exception e)
            {
                Check("step 3, Anonymous sign-in enabled", false,
                    $"Dashboard > Authentication > enable Anonymous sign-in ({e.Message})");
                return false;
            }

            // ---- Step 3c · Relay answers ----------------------------------------------
            // The same call NetSession.StartRelayHostAsync makes, with the same connection
            // count, so a pass here means the host path itself is provisioned.
            try
            {
                var allocation = await RelayService.Instance.CreateAllocationAsync(3);
                string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                Check("step 3, Relay allocates", !string.IsNullOrEmpty(relayCode),
                    "Dashboard > Relay > enable");
                Report.AppendLine($"       relay join code: {relayCode} (allocation expires on its own)");
            }
            catch (Exception e)
            {
                Check("step 3, Relay allocates", false, $"Dashboard > Relay > enable ({e.Message})");
            }

            // ---- Step 3d · Lobby answers ----------------------------------------------
            string lobbyId = null;
            try
            {
                var options = new CreateLobbyOptions
                {
                    IsPrivate = true,
                    Data = new Dictionary<string, DataObject>
                    {
                        { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, "TEST") }
                    }
                };

                Lobby lobby = await LobbyService.Instance.CreateLobbyAsync("ugs-check", 4, options);
                lobbyId = lobby?.Id;
                Check("step 3, Lobby creates", !string.IsNullOrEmpty(lobbyId), "Dashboard > Lobby > enable");
            }
            catch (Exception e)
            {
                Check("step 3, Lobby creates", false, $"Dashboard > Lobby > enable ({e.Message})");
            }
            finally
            {
                // ⚠️ A private test lobby left behind still counts against the project's live
                // lobby quota until its heartbeat lapses, and it would show up in a browse.
                if (!string.IsNullOrEmpty(lobbyId))
                {
                    try { await LobbyService.Instance.DeleteLobbyAsync(lobbyId); }
                    catch (Exception e) { Report.AppendLine($"warn : test lobby {lobbyId} not deleted: {e.Message}"); }
                }
            }

            // ---- Step 4 · Multiplay Hosting -------------------------------------------
            // ⚠️ NOT VERIFIABLE FROM HERE, AND SAYING SO IS THE POINT. Fleet allocation needs a
            // build config and a Linux server build uploaded, and the client SDK that would
            // report in is not installed at all: com.unity.services.multiplay does not compile
            // on Unity 6000.5 at any published version (see NetSession.StartMultiplayServerAsync).
#if MULTIPLAY_SDK
            Report.AppendLine("note : MULTIPLAY_SDK is defined. Fleet registration is compiled in, but a fleet still has to exist.");
#else
            Report.AppendLine("note : step 4, Multiplay Hosting cannot be checked from the editor. The SDK is not installed");
            Report.AppendLine("       (it does not compile on 6000.5), so a dedicated host serves clients without");
            Report.AppendLine("       reporting to a fleet. Enabling it in the dashboard and adding billing is still");
            Report.AppendLine("       needed before fleet allocation works, and billing is what gates provisioning.");
#endif

            return true;
        }

        /// <summary>Reads one scalar out of ProjectSettings.asset without a YAML dependency.</summary>
        private static string ReadProjectSetting(string key)
        {
            try
            {
                foreach (string line in System.IO.File.ReadAllLines("ProjectSettings/ProjectSettings.asset"))
                {
                    string trimmed = line.TrimStart();
                    if (!trimmed.StartsWith(key + ":", StringComparison.Ordinal)) continue;
                    return trimmed.Substring(key.Length + 1).Trim();
                }
            }
            catch (Exception e)
            {
                return $"<unreadable: {e.Message}>";
            }

            return "";
        }

        private static string Show(string value) => string.IsNullOrEmpty(value) ? "(blank)" : value;

        private static void Check(string what, bool ok, string remedy)
        {
            Report.AppendLine(ok ? $"ok   : {what}" : $"FAIL : {what}  ->  {remedy}");
            if (!ok) _failures++;
        }

        private static void Flush()
        {
            try
            {
                string dir = System.IO.Path.GetDirectoryName(ResultPath);
                if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(ResultPath, Report.ToString());
            }
            catch (Exception e)
            {
                Debug.LogError($"[UgsCheck] could not write {ResultPath}: {e.Message}");
            }
        }
    }
}
