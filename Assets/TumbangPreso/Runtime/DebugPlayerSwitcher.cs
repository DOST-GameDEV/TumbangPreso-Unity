using UnityEngine;
using UnityEngine.InputSystem;

namespace TumbangPreso
{
    /// <summary>
    /// Drive any seat from the keyboard, for testing. Converted from
    /// `scripts/systems/debug_player_switcher.gd`.
    ///
    /// F1-F4 take a seat, Tab cycles, F6 returns to the default.
    ///
    /// ⚠️ ONE SLOT, NOT TWO — the 2026-07-29 input overhaul removed the second. There is
    /// exactly one human-driven unit at a time; everything else is under AI.
    ///
    /// ⚠️ DEBUG-ONLY AND NETWORK-DISABLED. It refuses to act in a networked session, because
    /// seizing a seat locally would desync every peer's idea of who is driving what. It also
    /// removes itself from a release build.
    ///
    /// ⚠️ THE HANDOVER IS BOTH HALVES OR NEITHER. Taking a seat un-parks that unit's input
    /// AND disables its AI; every other seat is parked and re-enabled. Doing only one half
    /// leaves either two drivers on one body or none at all.
    /// </summary>
    public sealed class DebugPlayerSwitcher : MonoBehaviour
    {
        /// <summary>
        /// ⚠️⚠️ A FALLBACK, NOT THE ANSWER — SEE <see cref="DefaultSlot"/>. Hardcoding seat 0
        /// meant F6 handed "your own character back" to whoever happened to sit in slot 0, and
        /// the player's real seat is whichever one the setup screen chose. `GameLaunch.SoloSeat`
        /// defaults to P2, so on a default match this constant was wrong every time.
        /// </summary>
        public const int FallbackSlot = 0;

        /// <summary>
        /// The seat the PLAYER is driving, discovered rather than declared.
        ///
        /// ⚠️ THE HUMAN'S UNIT IS EXACTLY THE ONE WITH NO AI ON IT, which this can read off the
        /// seats directly and needs no cooperation from gameplay. `debug_player_switcher.gd`
        /// records what the hardcoded version cost there: a player who picked a different seat
        /// got, one frame into the match, their own character parked, the camera snapped
        /// elsewhere, and the unit they chose handed to a bot.
        /// </summary>
        public static int DefaultSlot
        {
            get
            {
                foreach (var unit in FindObjectsByType<CharacterMotor>(FindObjectsInactive.Exclude))
                {
                    var ai = unit.GetComponent<AIController>();
                    if (ai == null || !ai.enabled) return unit.PlayerSlot;
                }

                return FallbackSlot;
            }
        }

        /// <summary>Seat currently driven by the human, or -1 for none.</summary>
        public int DrivenSlot { get; private set; } = FallbackSlot;

        /// <summary>
        /// ⚠️ RESOLVED ON THE FIRST PRESS, NOT AT AWAKE. `MatchInstaller` builds the seats in
        /// its own Start, so at this component's Awake the honest answer to "which unit has no
        /// AI" is "all of them" — the failure the .gd had to defer around. Reading it lazily
        /// gets the same guarantee without a deferred call.
        /// </summary>
        private bool _resolved;

        private UI.DebugBar _bar;

        /// <summary>
        /// ⚠️⚠️ IT NO LONGER SELF-DESTRUCTS IN A RELEASE BUILD, AND THAT IS A DELIBERATE
        /// DIVERGENCE FROM THE .gd ON HUMAN INSTRUCTION. 🧑 on this build: *"some controls didnt
        /// get ported, tab cant let me switch chara in singleplayer, (not supposed to exist in
        /// multiplayer btw)"*.
        ///
        /// `debug_player_switcher.gd::_ready` is `if not OS.is_debug_build(): queue_free()`, and
        /// this file copied that as `!Debug.isDebugBuild &amp;&amp; !Application.isEditor`. The copy is
        /// faithful and it is why Tab did nothing in the .exe on the Desktop: a Unity player
        /// built without "Development Build" reports `isDebugBuild` false, so the component
        /// deleted itself before it ever read a key. In Godot the same rule is invisible in
        /// practice because every editor run IS a debug build, which is where the feature was
        /// being used and where the expectation that it ships came from.
        ///
        /// The gate is now the one he actually asked for: SINGLE PLAYER ONLY, which
        /// <see cref="NetAuthority.IsNetworked"/> already answers in `Update`, and which is the
        /// same condition the .gd's own `_is_active()` applies for a different reason. Seizing a
        /// seat locally would desync every peer's idea of who drives what, so the networked
        /// refusal is a rule, not a preference.
        ///
        /// ⚠️ THE §0.3 REMOVAL CONTRACT STILL HOLDS. This file is still `Debug`-prefixed, still
        /// discovers units by walking the scene, and no gameplay script names it. What changed
        /// is when it is alive, not what may depend on it.
        /// </summary>
        private void Awake()
        {

            // Debug registers with debug; no gameplay script names either one.
            var barGo = new GameObject("DebugBar");
            barGo.transform.SetParent(transform, false);
            _bar = barGo.AddComponent<UI.DebugBar>();
        }

        private void Update()
        {
            // ⚠️ NEVER IN A NETWORKED SESSION.
            if (NetAuthority.IsNetworked) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            // The player's own seat, before any key is read, so a cycle starts from where they
            // actually are rather than from slot 0.
            if (!_resolved) { DrivenSlot = DefaultSlot; _resolved = true; }

            if (kb.f1Key.wasPressedThisFrame) Assign(0);
            else if (kb.f2Key.wasPressedThisFrame) Assign(1);
            else if (kb.f3Key.wasPressedThisFrame) Assign(2);
            else if (kb.f4Key.wasPressedThisFrame) Assign(3);
            else if (kb.f6Key.wasPressedThisFrame) Assign(DefaultSlot);
            else if (kb.f5Key.wasPressedThisFrame) Cycle();

            // ⚠️ CYCLE MOVED OFF TAB ON 2026-08-23. Tab is the hold-to-read ability panel now
            // (`AbilityInfo` in the input map), and a debug tool must not sit on a key a player
            // presses in a real match: holding it to read a power would have silently handed
            // their body to a different seat. F5 is beside the four F-keys this already owns.
        }

        private void Cycle()
        {
            int next = DrivenSlot + 1;
            if (next >= Core.Balance.PlayerCount) next = 0;
            Assign(next);
        }

        private void Assign(int slot)
        {
            DrivenSlot = slot;
            ApplySlots();
            RefreshReadout();
        }

        /// <summary>
        /// ⚠️ BOTH HALVES, EVERY SEAT, EVERY TIME. Un-parking the new seat without re-enabling
        /// the old one's AI leaves a body nobody drives standing in the road.
        /// </summary>
        private void ApplySlots()
        {
            CharacterMotor claimed = null;

            foreach (var unit in FindObjectsByType<CharacterMotor>(FindObjectsInactive.Exclude))
            {
                bool driven = unit.PlayerSlot == DrivenSlot;
                if (driven) claimed = unit;

                // ⚠️⚠️ PARKING IS NOT RELEASING, AND THAT IS THE OTHER HALF OF THE 2026-09-04
                // REPORT: *"my character js keeps going left on its own"*. `Parked` stops new
                // input being READ; it does not clear what is already held. A player pressing a
                // seat key while walking left therefore left a body behind with left still down,
                // and an `AIController` was then re-enabled on top of a stale human intent.
                // `MatchRpc.HostPeerLeft` already does exactly this pair for the same reason
                // when a peer drops mid-key, and this is that fix applied to the local handover.
                //
                // ⚠️ `CommitFrame` AS WELL AS `Clear`, because the intent is double-buffered:
                // clearing the live half without committing leaves the previous frame's presses
                // readable for one more step, which is one step of a body walking itself.
                if (!driven && unit.Intent.Parked == false)
                {
                    unit.Intent.Clear();
                    unit.Intent.CommitFrame();
                }

                unit.Intent.Parked = !driven;

                var ai = unit.GetComponent<AIController>();
                if (ai != null) ai.enabled = !driven;

                var reader = unit.GetComponent<PlayerInputReader>();
                if (reader != null) reader.enabled = driven;

                // ⚠️⚠️ THE NAME FOLLOWS THE BODY YOU ARE IN, AND THE PORT DROPPED THIS ENTIRELY.
                // 🧑 on the Godot build, quoted in `debug_player_switcher.gd::_apply_slots`:
                // *"i want the character im playing currently to say my name and for the bot to
                // get their supposed name if i leave their body"*. `IsBot` is what
                // `CharacterMotor.DisplayName()` branches on, and nothing rewrote it here — so
                // Tabbing into LOLA PACING left her still labelled LOLA PACING while a human
                // drove her, and the seat just vacated went on wearing the player's own name.
                //
                // ⚠️ THE NAME IS WRITTEN, NOT ONLY THE FLAG. A bot seat has never been given one
                // (`BuildSeat` hands `PlayerName` to the human seat alone), so clearing `IsBot`
                // by itself would fall through to the bare seat label "P3".
                //
                // ⚠️ AND IT IS LEFT IN PLACE ON THE WAY OUT rather than cleared: `IsBot` stops it
                // being read, and the next Tab back finds it already correct.
                unit.IsBot = !driven;

                if (driven && string.IsNullOrEmpty(unit.PlayerName))
                    unit.PlayerName = Settings.SettingsStore.Current.PlayerName;
            }

            // ⚠️⚠️ THE CAMERA GOES WITH THE HANDOVER, AND WITHOUT THIS TAB WAS INVISIBLE.
            // `_apply_slots` in the .gd picks which rig is active as part of the same switch
            // (§3.5.3). Here the rig kept following the seat it was bound to at match start, so
            // a working Tab still left the player watching their old body while driving a
            // different one somewhere off screen — indistinguishable from Tab doing nothing,
            // which is how a fixed keybind would still have been reported as broken.
            //
            // ⚠️ THROUGH `Follow`, THE RIG'S ORDINARY PUBLIC API, AND THE MODE IS NOT TOUCHED.
            // FPP/TPP is derived from the subject (§3a) and stays that way; the switcher chooses
            // WHO is looked through, never HOW.
            if (claimed != null)
            {
                // ⚠️⚠️ `Camera.main` IS THE SPECTATOR'S OWN OBJECT WHENEVER ONE IS UP, AND THAT
                // IS WHY THE RIG IS LOOKED UP BY TYPE NOW. `MatchInstaller` tags the
                // `SpectatorCamera` it builds as `MainCamera`, so on any run that started with
                // nobody driving a seat (`-tp-allbots`, an authored all-bots scene, or a
                // spectator window) `Camera.main.GetComponent<CameraRig>()` answers **null** and
                // this whole block did nothing: the seat changed hands and the view stayed where
                // it was, which is the exact symptom the note above says this code exists to
                // prevent. Finding the rig by type cannot be fooled by which camera holds the tag.
                var rig = UnityEngine.Object.FindFirstObjectByType<CameraSystem.CameraRig>();

                if (rig != null)
                {
                    // ⚠️ AND IT IS SWITCHED BACK ON, because the branch that built the spectator
                    // camera also called `rig.SetActive(false)`. Following a disabled rig is a
                    // no-op that looks like a fix.
                    rig.SetActive(true);
                    rig.Follow(claimed);
                }

                // ⚠️⚠️ TAKING A SEAT ENDS SPECTATING, AND NOTHING SAID SO. This is the local
                // half of `docs/TODO.md` § 141: a body that is driven by a keyboard is not a
                // body being watched, so the watcher camera and the stripped HUD both have to
                // go. Without this the switcher hands the player a seat while the spectator
                // overlay keeps drawing over it, which is what 🧑 photographed:
                // **"IF IT isnt spectator why do i see spectator hud"**.
                var spectator =
                    UnityEngine.Object.FindFirstObjectByType<CameraSystem.SpectatorCamera>();

                if (spectator != null) spectator.enabled = false;

                UnityEngine.Object.FindFirstObjectByType<UI.Hud>()?.ExitSpectatorMode();
            }
        }

        /// <summary>§3.5.4: after a role swap you need to see at a glance which unit you are
        /// driving and what it has become.</summary>
        public void RefreshReadout()
        {
            if (_bar != null) _bar.Refresh(Describe());
        }

        private string Describe()
        {
            var unit = UnitAt(DrivenSlot);
            if (unit == null) return $"P{DrivenSlot + 1} (not spawned)";

            return $"{unit.DisplayName()} · {(unit.IsDefender ? "TAYA" : "ATTACKER")}";
        }

        private static CharacterMotor UnitAt(int slot)
        {
            foreach (var m in FindObjectsByType<CharacterMotor>(FindObjectsInactive.Exclude))
                if (m.PlayerSlot == slot) return m;

            return null;
        }
    }
}
