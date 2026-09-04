using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The local half of `scripts/main.gd` — the parts that do not need a peer on the other
    /// end: seating units on the floor at spawn, and entering and leaving spectator mode.
    ///
    /// ⚠️ main.gd IS 3,595 LINES AND MOST OF IT IS NETCODE. Spawning peers, late-join sync,
    /// pick replication, disconnection handling and dedicated-lobby recycling all need a
    /// transport that is not ported yet. What is here is what a single-player session
    /// actually exercises; the ledger tracks the rest rather than pretending it landed.
    /// </summary>
    public sealed class MatchHost : MonoBehaviour
    {
        /// <summary>How far above and below a spawn mark to look for the floor.</summary>
        public const float SpawnFloorProbeHeight = 2.0f;
        public const float SpawnFloorProbeDepth = 6.0f;
        public const float SpawnFloorClearance = 0.02f;

        private CameraSystem.SpectatorCamera _spectator;

        /// <summary>
        /// Drop a unit onto whatever floor is under its spawn mark.
        ///
        /// ⚠️⚠️ SPAWN MARKS ARE FLAT AND MAPS ARE NOT. Both arenas are dressed streets with
        /// kerbs, slabs and aprons at different heights, and a mark computed from the box
        /// geometry knows nothing about them. Without this a unit spawns inside a kerb and
        /// the settle frames shove it out sideways, which reads as a physics bug.
        ///
        /// ⚠️ IT EXCLUDES THE UNIT'S OWN COLLIDER. A capsule raycast that starts inside the
        /// thing it is casting for hits itself and seats the unit on its own head.
        /// </summary>
        public static void SeatOnFloor(CharacterMotor character)
        {
            if (character == null) return;

            var cc = character.GetComponent<CharacterController>();
            float height = cc != null ? cc.height : 1.6f;

            Vector3 from = character.transform.position + Vector3.up * SpawnFloorProbeHeight;

            // QueryTriggerInteraction.Ignore: the kill plane and every hazard zone are
            // triggers, and seating a unit on the kill plane puts it under the world.
            if (!Physics.Raycast(from, Vector3.down, out RaycastHit hit,
                    SpawnFloorProbeDepth, ~0, QueryTriggerInteraction.Ignore))
                return;

            // The collider we hit might be the unit itself if it was not excluded; Unity has
            // no per-cast exclude list, so check what we hit rather than trusting the cast.
            if (hit.collider != null && hit.collider.transform.IsChildOf(character.transform))
                return;

            Vector3 p = character.transform.position;

            // The origin is at the feet on these seats, so the floor point plus clearance IS
            // the position — no half-height offset, which the Godot expression needed because
            // its bodies were centred.
            float y = hit.point.y + SpawnFloorClearance;
            if (cc != null) y += Mathf.Max(0.0f, cc.center.y - height / 2.0f);

            var seated = new Vector3(p.x, y, p.z);

            // ⚠️⚠️ THE SPAWN IS RECORDED HERE, AND NOTHING IN A REAL MATCH RECORDED IT BEFORE.
            // `CharacterMotor.SpawnPosition` had exactly one writer in the whole port,
            // `SliceRunner` — a test harness. Every seat in an actual match therefore carried
            // the default Vector3.zero, which is not "unset", it is the middle of the arena
            // where the lata stands (`InputIntent.HasAimPoint` records the same trap for the
            // aim point). That made both readers wrong: the kill plane returned a fallen unit
            // to the can's mark, and the tag penalty could not use it at all, which is why
            // `RoundDirector` grew a ring-point of its own to work around it.
            //
            // `character_base.gd:831` writes `spawn_position = global_position` at spawn for
            // this reason, and this is the moment the port has the same answer: after the floor
            // probe, so the mark is the seated position rather than the authored marker's Y.
            character.SpawnPosition = seated;

            character.Teleport(seated);
        }

        /// <summary>
        /// ⚠️ A SPECTATOR IS CREATED, NOT A SEAT REPURPOSED. It has no body, holds no seat and
        /// spawns no character; its slot is filled by the same placeholder-AI path that fills
        /// any empty one, so a 2v2 stays a 2v2.
        /// </summary>
        public void EnterSpectatorMode()
        {
            if (_spectator != null) return;

            var go = new GameObject("Spectator");
            go.transform.SetParent(transform, false);
            _spectator = go.AddComponent<CameraSystem.SpectatorCamera>();

            GameLaunch.Spectator = true;

            // The gameplay HUD goes with it: a spectator has no stamina, no cooldowns and no
            // role, so every meter on it would be describing somebody else's unit.
            var hud = FindFirstObjectByType<UI.Hud>();
            if (hud != null) hud.gameObject.SetActive(false);
        }

        public void ExitSpectatorMode()
        {
            if (_spectator == null) return;

            Destroy(_spectator.gameObject);
            _spectator = null;
            GameLaunch.Spectator = false;

            var hud = FindFirstObjectByType<UI.Hud>();

            if (hud != null)
            {
                hud.gameObject.SetActive(true);

                // ⚠️ RE-ACTIVATING THE OBJECT IS NOT UNDOING THE STRIP. This method hides the
                // whole HUD and shows it again, which is right for the spectator IT creates; but
                // `MatchInstaller` can have put the same HUD into spectator mode separately
                // (`RebindLocalSeat`), and that strips individual pieces and leaves the
                // controls overlay drawn. Switching the parent back on restores none of it.
                // `ExitSpectatorMode` early-returns when it was never entered, so this costs a
                // branch on the ordinary path. `docs/TODO.md` § 141.
                hud.ExitSpectatorMode();
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public bool IsSpectating => _spectator != null;

        /// <summary>The spectator's live readout, for whatever draws it.</summary>
        public string SpectatorStatus =>
            _spectator != null ? _spectator.StatusText() : "";
    }
}
