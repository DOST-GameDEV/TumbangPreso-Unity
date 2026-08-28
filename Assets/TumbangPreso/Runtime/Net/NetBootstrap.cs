using System;
using UnityEngine;

namespace TumbangPreso.Net
{
    /// <summary>
    /// Starts a session from the command line, so a match with two peers can be run without a
    /// person clicking through two windows.
    ///
    /// ⚠️⚠️ THE NETCODE HALF OF THE PORT HAS BEEN "NEEDS TWO MACHINES" FOR ITS WHOLE LIFE, AND
    /// THAT WAS NEVER TRUE. Two copies of the player on one desktop are two peers; what was
    /// actually missing is a way to put one of them into host mode and the other into client
    /// mode without a human driving both sets of menus, and a way to tell their logs apart.
    /// Both are one switch each. Everything the ledger lists as pending under `main.gd` is
    /// verifiable from here.
    ///
    /// ⚠️ AND A DEDICATED SERVER NEEDS THIS ANYWAY. The Unity game has no active Vultr
    /// deployment, but any explicitly launched headless build has no menus to click, so
    /// `-tp-dedicated` is not only a test affordance; it is how that build starts at all.
    ///
    ///   TumbangPreso.exe -tp-host                  host on the default port, seat 0
    ///   TumbangPreso.exe -tp-host 7777             host on a chosen port
    ///   TumbangPreso.exe -tp-dedicated 7777        referee with no seat
    ///   TumbangPreso.exe -tp-join 127.0.0.1 7777   client
    ///   TumbangPreso.exe -tp-map Eskinita          which arena to open
    ///   TumbangPreso.exe -tp-autostart 2           ready when two peers are seated
    ///   TumbangPreso.exe -tp-autorematch           vote from the real result board
    ///   TumbangPreso.exe -logFile host.log         Unity's own, and it is REQUIRED for two
    ///                                              instances: both write to one Player.log
    ///                                              otherwise and the interleaving is unreadable
    ///
    /// ⚠️ IT RUNS BEFORE ANY SCENE, so a switch cannot be missed by whichever screen happened
    /// to load first.
    /// </summary>
    public static class NetBootstrap
    {
        /// <summary>
        /// Land in the MULTIPLAYER lobby instead of the title, so the LOBBY's own paths can be
        /// driven from a command line.
        ///
        /// ⚠️⚠️ `-tp-host` CANNOT TEST ANY OF THIS, WHICH IS WHY THESE EXIST. That switch skips
        /// `ConvertedMatchSetup` entirely and drops the process straight into the arena, so it
        /// verifies the MATCH and says nothing about the auto-host, the join panel, the LAN/online
        /// switch, the cast, the ready ticks or chat. `docs/TODO.md` § 68.14 step 7 is a list of
        /// things that only fail with two processes in a lobby, and every one of them is on the
        /// far side of this switch.
        ///
        /// ⚠️ THE PORT OVERRIDE IS NOT A CONVENIENCE. Two processes on ONE machine both
        /// auto-hosting want the same 8910, so without it the second one always lands in the
        /// bind-refused fallback and the host to leave to join path (§ 68.6) can never be reached.
        /// </summary>
        public const string LobbySwitch = "-tp-lobby";
        public const string LobbyPortSwitch = "-tp-lobbyport";
        public const string LobbyJoinSwitch = "-tp-lobbyjoin";
        public const string LobbyChatSwitch = "-tp-lobbychat";

        /// <summary>The port this process's lobby auto-hosts on, or 0 for the default.</summary>
        public static int LobbyPort { get; private set; }

        /// <summary>An address or join code to press JOIN with once the lobby has settled.</summary>
        public static string LobbyJoin { get; private set; }

        /// <summary>One line to say once this process is connected.</summary>
        public static string LobbyChat { get; private set; }

        public const string HostSwitch = "-tp-host";
        public const string DedicatedSwitch = "-tp-dedicated";
        public const string JoinSwitch = "-tp-join";
        public const string MapSwitch = "-tp-map";
        public const string ProfileSwitch = "-tp-profile";

        /// <summary>True when the command line asked for a session, so the menus are skipped.</summary>
        public static bool Requested { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Run()
        {
            string[] args;

            try { args = Environment.GetCommandLineArgs(); }
            catch { return; }

            if (args == null || args.Length < 2) return;

            string profile = Value(args, ProfileSwitch) ?? Value(args, "-profile");
            if (!string.IsNullOrEmpty(profile))
            {
                NetIdentity.SetProfile(profile);
            }

            // ⚠️⚠️ `-tp-allbots` IS WHAT MAKES A TWO-PROCESS TEST MEAN ANYTHING. Without it
            // nobody presses a key in either window, all four seats stand still, and two peers
            // agreeing that nothing happened is not evidence of anything. With it every seat
            // plays itself, so the distances, the casts and the props in `NetStateReport` are
            // real numbers on both sides and a disagreement between the two files names the
            // fault. `MatchInstaller` already answers this flag by giving the local seat an
            // `AIController` like any unoccupied one; the seat still belongs to this peer, so a
            // client still submits its own transforms exactly as a human would.
            if (Has(args, "-tp-allbots") || Has(args, "-allbots"))
            {
                GameLaunch.AllBots = true;
            }

            string map = Value(args, MapSwitch) ?? UI.SceneFlow.Eskinita;

            // ⚠️ READ BEFORE THE HOST AND JOIN BRANCHES BELOW AND HANDLED BEFORE THEM, because
            // `-tp-lobby` is the opposite request: those two skip the menus to reach the ARENA and
            // this one skips them to reach the LOBBY. Falling through into either would start a
            // second transport under the one the lobby is about to open.
            if (Has(args, LobbySwitch) || !string.IsNullOrEmpty(Value(args, LobbyJoinSwitch)))
            {
                if (int.TryParse(Value(args, LobbyPortSwitch), out int lobbyPort) && lobbyPort > 0)
                    LobbyPort = lobbyPort;

                LobbyJoin = Value(args, LobbyJoinSwitch);
                LobbyChat = Value(args, LobbyChatSwitch);

                Requested = true;
                Application.runInBackground = true;

                Debug.Log($"[NetBoot] lobby requested, port={(LobbyPort > 0 ? LobbyPort : NetSession.DefaultPort)} " +
                          $"join='{LobbyJoin}' chat='{LobbyChat}'");

                Defer(() =>
                {
                    UI.SceneFlow.Networked = true;
                    UI.SceneFlow.Go(UI.SceneFlow.MatchSetup);
                });

                return;
            }

            bool explicitJoin = !string.IsNullOrEmpty(Value(args, JoinSwitch));
            bool explicitHost = Has(args, HostSwitch) || Has(args, "-host") || Has(args, "--host");
            bool isDedicated = Has(args, DedicatedSwitch) || Has(args, "-dedicated") ||
                               Has(args, "--dedicated") ||
                               (Application.isBatchMode && !Application.isEditor &&
                                !explicitJoin && !explicitHost);
            bool isHost = explicitHost || isDedicated;

            if (isHost)
            {
                int port = Port(args, isDedicated ? DedicatedSwitch : HostSwitch);

                Requested = true;
                Application.runInBackground = true;

                if (isDedicated)
                {
                    Application.targetFrameRate = 60;
                    QualitySettings.vSyncCount = 0;
                }

                // ⚠️ ONE FRAME LATER, NOT NOW. `NetSession.Ensure` builds a GameObject, and
                // BeforeSceneLoad runs before there is a scene to build it into: the object is
                // created and then destroyed by the very first scene load.
                Defer(async () =>
                {
                    var net = NetSession.Ensure();
                    bool ok = await net.StartHostAsync(port, isDedicated);

                    Debug.Log($"[NetBoot] host requested on {port} dedicated={isDedicated}: " +
                              (ok ? "listening" : "FAILED"));

                    if (!ok) return;

                    // ⚠️⚠️ THE LOBBY IS TOLD THE MATCH IS RUNNING, BECAUSE THIS PATH SKIPS THE
                    // SCREEN THAT NORMALLY SAYS SO. A host launched with `-tp-host` goes straight
                    // into the arena on the line below and never passes through
                    // `ConvertedMatchSetup`, so `MatchRpc.HostStartMatch` — the only other caller
                    // of this — never runs. `LobbySession.MatchInProgress` then stayed FALSE for a
                    // host that was visibly playing, and it is the switch behind three separate
                    // rules: `Depart` only HOLDS a dropped player's seat while it is set, so a
                    // player who quit lost their chair instead of leaving a bot in it;
                    // `RuleOnArrival` answers Refuse rather than Spectate; and the `inProgress`
                    // flag on the seating packet is what sends a joining client into the arena
                    // rather than leaving it in the lobby.
                    //
                    // ⚠️ `Lobby.StartMatch()` RATHER THAN `MatchRpc.HostStartMatch()`. The latter
                    // also broadcasts a StartMatch to every peer and fires `OnMatchStarted`, which
                    // is right when a lobby full of people presses go and wrong here: there is
                    // nobody connected yet, and this host is loading the arena on the next line by
                    // itself.
                    NetSession.Instance?.Lobby.StartMatch();

                    UI.SceneFlow.Go(map);
                });

                return;
            }

            string address = Value(args, JoinSwitch);
            if (string.IsNullOrEmpty(address)) return;

            int joinPort = Port(args, JoinSwitch, 1);

            Requested = true;
            Application.runInBackground = true;

            Defer(async () =>
            {
                var net = NetSession.Ensure();
                bool ok = await net.StartClientAsync(address, joinPort);

                Debug.Log($"[NetBoot] join requested to {address}:{joinPort}: " +
                          (ok ? "connecting" : "FAILED"));

                if (ok) UI.SceneFlow.Go(map);
            });
        }

        /// <summary>
        /// Runs an action on the first frame there is a scene to run it in.
        ///
        /// ⚠️ A HIDDEN GameObject WITH `DontDestroyOnLoad`, because the very next thing that
        /// happens is a scene load and anything ordinary is destroyed by it.
        /// </summary>
        private static void Defer(Action action)
        {
            var go = new GameObject("~NetBoot") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<NetBootstrapRunner>().Bind(action);
        }

        private static bool Has(string[] args, string name)
        {
            foreach (string a in args)
                if (string.Equals(a, name, StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        /// <summary>The argument after a switch, or null when the switch is absent or last.</summary>
        private static string Value(string[] args, string name, int offset = 1)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) continue;
                if (i + offset >= args.Length) return null;

                string next = args[i + offset];
                return next.StartsWith("-") ? null : next;
            }

            return null;
        }

        /// <summary>
        /// ⚠️ THE PORT IS OPTIONAL AND ITS ABSENCE IS NOT AN ERROR. `-tp-host` alone has to
        /// work, or the switch is longer to type than clicking the menu.
        /// </summary>
        private static int Port(string[] args, string name, int offset = 1)
        {
            string raw = Value(args, name, offset + (name == JoinSwitch ? 1 : 0));
            return int.TryParse(raw, out int port) && port > 0 ? port : NetSession.DefaultPort;
        }
    }
}
