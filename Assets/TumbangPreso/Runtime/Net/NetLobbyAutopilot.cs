using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.Net
{
    /// <summary>
    /// Presses the lobby's buttons on a process that has no human in front of it, and narrates
    /// every press and every answer into the log.
    ///
    /// ⚠️⚠️ IT PRESSES THE BUTTON, IT DOES NOT CALL THE METHOD. Invoking
    /// `MatchRpc.RequestSeatServerRpc` directly would be one line shorter and would test the
    /// wrong thing: `RefreshSeats` decides `interactable` per row, and a rule that computes
    /// wrong on a client is one of the three standing suspects for 🧑's *"a player cannot
    /// switch from p1 to p4"*. Going through `onClick` means the gate is under test, and a row
    /// that is dead reports itself as dead rather than passing silently.
    ///
    /// ⚠️ IT REPORTS A TIMEOUT AS LOUDLY AS A SUCCESS. Two sessions have now written the lobby
    /// up as fixed from reading the source. A rig that only logs the happy path would have
    /// agreed with both of them.
    ///
    /// ⚠️ ONE MonoBehaviour, ITS OWN FILE, named after the class. The built player reports
    /// `The file 'levelN' is corrupted!` and dies on load otherwise, and that rule has already
    /// cost this port a whole pass.
    /// </summary>
    public sealed class NetLobbyAutopilot : MonoBehaviour
    {
        /// <summary>
        /// How long to let the lobby settle before pressing anything.
        ///
        /// ⚠️ IT IS NOT COSMETIC. A client reaches `MatchSetup` the frame its transport
        /// connects, and `Identify` -> `Seating` -> `SyncLobbyPicks` is a full round trip after
        /// that. Pressing a seat before the roster has landed tests a screen the human never
        /// sees, and would misreport an empty seat row as a broken one.
        /// </summary>
        private const float DefaultSettleSeconds = 3.0f;

        /// <summary>
        /// How long this process waits before it touches anything, from `-tp-autodelay`.
        ///
        /// ⚠️⚠️ IT IS A TEST PARAMETER, NOT A COSMETIC ONE, BECAUSE THE FAULT IS ABOUT ORDER.
        /// 🧑 reported seat switching working while alone and failing "the moment a second player
        /// joins", so a rig where both processes press their seat during the first three seconds
        /// reproduces the case that already worked and never reaches the case that does not. The
        /// host is given the longer delay so that its press lands after the client is seated.
        /// </summary>
        private float _settleSeconds = DefaultSettleSeconds;

        /// <summary>How long a seat request has to come back before it is called a failure.</summary>
        private const float ConfirmSeconds = 5.0f;

        /// <summary>How long after READY the arena has to appear before it is called a failure.</summary>
        private const float EnterSeconds = 20.0f;

        /// <summary>Seconds between roster dumps, so two logs can be lined up by wall clock.</summary>
        private const float DumpSeconds = 2.0f;

        private enum Phase { Settling, Seating, Confirming, Readying, Entering, Done }

        private int _seat = NetBootstrap.NoAutoSeat;
        private bool _wantsReady;

        private Phase _phase = Phase.Settling;
        private float _phaseAge;
        private float _sinceDump;

        public void Bind(int seat, bool ready, float settleSeconds)
        {
            _seat = seat;
            _wantsReady = ready;
            if (settleSeconds > 0.0f) _settleSeconds = settleSeconds;

            Log($"armed: seat={(seat == NetBootstrap.NoAutoSeat ? "none" : seat.ToString())} " +
                $"ready={ready} settle={_settleSeconds:0.0}s");
        }

        private void Update()
        {
            _phaseAge += Time.unscaledDeltaTime;
            _sinceDump += Time.unscaledDeltaTime;

            if (_sinceDump >= DumpSeconds && _phase != Phase.Done)
            {
                _sinceDump = 0.0f;
                Dump();
            }

            switch (_phase)
            {
                case Phase.Settling: Settling(); break;
                case Phase.Seating: Seating(); break;
                case Phase.Confirming: Confirming(); break;
                case Phase.Readying: Readying(); break;
                case Phase.Entering: Entering(); break;
            }
        }

        private void Settling()
        {
            if (_phaseAge < _settleSeconds) return;
            Enter(_seat == NetBootstrap.NoAutoSeat ? Phase.Readying : Phase.Seating);
        }

        private void Seating()
        {
            // ⚠️ SPECTATE IS A SEAT REQUEST FOR "NO SEAT" AND IT IS A DIFFERENT BUTTON.
            // `ToggleSpectate` is what sends -1; there is no SeatButton-1 to press. The name is
            // `Button_SPECTATE` because `MenuKit.WoodButton` names its object after its label,
            // and this row is built in code rather than converted from the .tscn.
            string wanted = _seat < 0 ? "Button_SPECTATE" : $"SeatButton{_seat}";
            var button = FindButton(wanted);

            if (button == null)
            {
                Log($"FAILED: no button named '{wanted}' in the lobby. The screen is not up, or it was renamed.");
                Enter(Phase.Done);
                return;
            }

            Log($"pressing '{wanted}': interactable={button.interactable} " +
                $"activeInHierarchy={button.gameObject.activeInHierarchy}");

            if (!button.interactable)
            {
                // ⚠️ NOT A FAILURE ON ITS OWN, AND SAYING SO MATTERS. A row is deliberately dead
                // when it is the seat this peer already holds, and `RefreshSeats` is entitled to
                // refuse a chair somebody else is in. What the log has to distinguish is a rule
                // that refused for a reason from a rule that refused everything.
                Log($"'{wanted}' is NOT interactable, so no request was sent. " +
                    "If this row should have been pressable, the interactable rule is the fault.");
                Enter(_wantsReady ? Phase.Readying : Phase.Done);
                return;
            }

            button.onClick.Invoke();
            Enter(Phase.Confirming);
        }

        private void Confirming()
        {
            var net = NetSession.Instance;
            int slot = net == null ? -99 : net.LocalSlot;
            bool spectating = GameLaunch.Spectator;

            bool settled = _seat < 0 ? (spectating && slot < 0) : (!spectating && slot == _seat);
            if (settled)
            {
                Log($"seat CONFIRMED after {_phaseAge:0.00}s: LocalSlot={slot} spectator={spectating}");
                Enter(_wantsReady ? Phase.Readying : Phase.Done);
                return;
            }

            if (_phaseAge < ConfirmSeconds) return;

            Log($"seat TIMED OUT after {_phaseAge:0.00}s: asked for {_seat}, still LocalSlot={slot} " +
                $"spectator={spectating}. The host either never answered or refused.");
            Enter(_wantsReady ? Phase.Readying : Phase.Done);
        }

        private void Readying()
        {
            if (!_wantsReady)
            {
                Enter(Phase.Done);
                return;
            }

            var button = FindButton("PrimaryButton");
            if (button == null)
            {
                Log("FAILED: no button named 'PrimaryButton' in the lobby.");
                Enter(Phase.Done);
                return;
            }

            Log($"pressing 'PrimaryButton' (READY): interactable={button.interactable} " +
                $"activeInHierarchy={button.gameObject.activeInHierarchy}");

            if (!button.interactable)
            {
                Log("'PrimaryButton' is NOT interactable, so READY was never declared.");
                Enter(Phase.Done);
                return;
            }

            button.onClick.Invoke();
            Enter(Phase.Entering);
        }

        private void Entering()
        {
            if (UI.SceneFlow.InMatch)
            {
                Log($"ENTERED the arena '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}' " +
                    $"{_phaseAge:0.00}s after READY.");
                Enter(Phase.Done);
                return;
            }

            if (_phaseAge < EnterSeconds) return;

            Log($"NEVER ENTERED the arena: {_phaseAge:0.00}s after READY the active scene is still " +
                $"'{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'.");
            Enter(Phase.Done);
        }

        private void Enter(Phase next)
        {
            if (next == _phase) return;
            _phase = next;
            _phaseAge = 0.0f;

            if (next == Phase.Done) Log("autopilot finished.");
        }

        /// <summary>
        /// One line naming everything the two logs have to be compared on.
        ///
        /// ⚠️ THE HOST'S LOBBY AND A CLIENT'S ARE DIFFERENT OBJECTS AND THIS PRINTS BOTH THE
        /// SAME WAY ON PURPOSE. A client's `LobbySession` holds no peers at all: its roster
        /// arrives as `_replicatedSeats` through `SyncLobbyPicks`, so `Lobby.PeerCount` reading
        /// 0 on a client is correct and reading 0 on the host is the bug.
        /// </summary>
        private void Dump()
        {
            var net = NetSession.Instance;
            if (net == null)
            {
                Log("state: no NetSession.");
                return;
            }

            var rows = new System.Text.StringBuilder();
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
            {
                var info = MatchRpc.Instance?.GetSeatInfo(slot);
                var button = FindButton($"SeatButton{slot}");
                rows.Append($" [{slot}");
                rows.Append(info != null && info.Occupied ? $" {info.Name}#{info.PeerId}" : " free");
                rows.Append(button == null ? " nobtn" : (button.interactable ? " live" : " dead"));
                rows.Append(']');
            }

            Log($"state: scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} " +
                $"host={NetAuthority.IsHost} networked={net.IsNetworked} slot={net.LocalSlot} " +
                $"spectator={GameLaunch.Spectator} peers={net.Lobby.PeerCount} " +
                $"inProgress={net.Lobby.MatchInProgress} rows{rows}");
        }

        /// <summary>
        /// ⚠️ `FindObjectsInactive.Include`, because a lobby row can legitimately be parented
        /// under a panel that is off while a match is loading, and a search that skips it
        /// reports "the button does not exist" for a button that plainly does.
        /// </summary>
        private static Button FindButton(string nodeName)
        {
            var all = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var button in all)
                if (button.name == nodeName) return button;

            return null;
        }

        private static void Log(string message) => Debug.Log($"[NetBoot] autopilot: {message}");
    }
}
