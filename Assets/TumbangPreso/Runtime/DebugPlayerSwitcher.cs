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
        public const int DefaultSlot = 0;

        /// <summary>Seat currently driven by the human, or -1 for none.</summary>
        public int DrivenSlot { get; private set; } = DefaultSlot;

        private UI.DebugBar _bar;

        private void Awake()
        {
            if (!Debug.isDebugBuild && !Application.isEditor)
            {
                Destroy(gameObject);
                return;
            }

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

            if (kb.f1Key.wasPressedThisFrame) Assign(0);
            else if (kb.f2Key.wasPressedThisFrame) Assign(1);
            else if (kb.f3Key.wasPressedThisFrame) Assign(2);
            else if (kb.f4Key.wasPressedThisFrame) Assign(3);
            else if (kb.f6Key.wasPressedThisFrame) Assign(DefaultSlot);
            else if (kb.tabKey.wasPressedThisFrame) Cycle();
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
            foreach (var unit in FindObjectsByType<CharacterMotor>(FindObjectsInactive.Exclude))
            {
                bool driven = unit.PlayerSlot == DrivenSlot;

                unit.Intent.Parked = !driven;

                var ai = unit.GetComponent<AIController>();
                if (ai != null) ai.enabled = !driven;

                var reader = unit.GetComponent<PlayerInputReader>();
                if (reader != null) reader.enabled = driven;
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
