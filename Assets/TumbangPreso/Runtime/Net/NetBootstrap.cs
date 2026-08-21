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
    /// ⚠️ AND THE DEDICATED SERVER NEEDS THIS ANYWAY. The Singapore VPS runs a headless build
    /// with no menus to click, so `-tp-dedicated` is not a test affordance; it is how that
    /// build starts at all.
    ///
    ///   TumbangPreso.exe -tp-host                  host on the default port, seat 0
    ///   TumbangPreso.exe -tp-host 7777             host on a chosen port
    ///   TumbangPreso.exe -tp-dedicated 7777        referee with no seat
    ///   TumbangPreso.exe -tp-join 127.0.0.1 7777   client
    ///   TumbangPreso.exe -tp-map Eskinita          which arena to open
    ///   TumbangPreso.exe -logFile host.log         Unity's own, and it is REQUIRED for two
    ///                                              instances: both write to one Player.log
    ///                                              otherwise and the interleaving is unreadable
    ///
    /// ⚠️ IT RUNS BEFORE ANY SCENE, so a switch cannot be missed by whichever screen happened
    /// to load first.
    /// </summary>
    public static class NetBootstrap
    {
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

            string map = Value(args, MapSwitch) ?? UI.SceneFlow.Eskinita;

            bool isDedicated = Has(args, DedicatedSwitch) || Has(args, "-dedicated") || Has(args, "--dedicated") || (Application.isBatchMode && !Application.isEditor);
            bool isHost = Has(args, HostSwitch) || Has(args, "-host") || Has(args, "--host") || isDedicated;

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
                Defer(() =>
                {
                    var net = NetSession.Ensure();
                    bool ok = net.StartHost(port, isDedicated);

                    Debug.Log($"[NetBoot] host requested on {port} dedicated={isDedicated}: " +
                              (ok ? "listening" : "FAILED"));

                    if (ok) UI.SceneFlow.Go(map);
                });

                return;
            }

            string address = Value(args, JoinSwitch);
            if (string.IsNullOrEmpty(address)) return;

            int joinPort = Port(args, JoinSwitch, 1);

            Requested = true;
            Application.runInBackground = true;

            Defer(() =>
            {
                var net = NetSession.Ensure();
                bool ok = net.StartClient(address, joinPort);

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
