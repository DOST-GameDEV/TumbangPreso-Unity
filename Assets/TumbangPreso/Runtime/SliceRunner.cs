using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// Drives a match over objects that ALREADY EXIST in the scene, rather than instantiating
    /// prefabs.
    ///
    /// ⚠️ THIS EXISTS BECAUSE PREFABS ARE A PHASE 6 PROBLEM AND THE SLICE IS NEEDED NOW.
    /// `MatchBootstrap` is the real spawner and takes prefab references; it is correct and it
    /// stays. But a prefab has to be authored, and the whole point of the vertical slice is to
    /// get a match RUNNING and MEASURABLE before any authoring exists. So this wires up scene
    /// objects the SceneBuilder placed, and shares every rule with the real path by calling
    /// the same directors.
    ///
    /// ⚠️ IT MUST NOT ACQUIRE RULES OF ITS OWN. The moment this file decides something the real
    /// bootstrap does not, the slice stops measuring the game and starts measuring itself.
    /// Everything here is placement and wiring; every decision routes to MatchDirector and
    /// RoundDirector.
    /// </summary>
    public sealed class SliceRunner : MonoBehaviour
    {
        public Lata Lata;
        public CharacterMotor[] Seats;
        public Slipper[] Slippers;

        [Tooltip("Seconds of round time per real second. 1 is real time; higher runs a whole " +
                 "match faster for a headless probe.")]
        public float TimeScale = 1.0f;

        public bool AutoStart = true;

        public bool Running { get; private set; }

        private void Start()
        {
            if (AutoStart) Begin();
        }

        public void Begin()
        {
            if (Lata == null || Seats == null || Slippers == null)
            {
                Debug.LogError("[SliceRunner] scene references are not wired.");
                return;
            }

            GameServices.Round.Clear();
            GameServices.Round.Lata = Lata;

            foreach (var s in Seats)
                if (s != null) GameServices.Round.Register(s);

            GameServices.Match.RoundStarted += OnRoundStarted;
            GameServices.Match.IntermissionStarted += OnIntermission;
            GameServices.Match.MatchEnded += OnMatchEnded;

            if (TimeScale > 1.0f) Time.timeScale = TimeScale;

            Running = true;
            GameServices.Match.StartMatch();
        }

        private void OnRoundStarted(int roundNumber, int defenderSlot)
        {
            ResetWorld(defenderSlot);
            GameServices.Round.BeginRound();

            Debug.Log($"[Slice] round {roundNumber} begins, taya is seat {defenderSlot}");
        }

        private void OnIntermission(int nextRound, int nextDefenderSlot) =>
            Invoke(nameof(Advance), Balance.IntermissionDuration);

        private void Advance() => GameServices.Match.AdvanceRound();

        private void OnMatchEnded(int winningSlot)
        {
            GameServices.Round.EndRound();
            Running = false;

            // ⚠️ END THE FREEZE BEFORE RESTORING THE SCALE, or the restore writes 1.0 and the
            // freeze's own restore then writes 0.05 back over it a few frames later.
            Hitstop.End();
            Time.timeScale = 1.0f;

            var m = GameServices.Match;
            Debug.Log($"[Slice] match over. scores: " +
                      $"{m.ScoreFor(0)} / {m.ScoreFor(1)} / {m.ScoreFor(2)} / {m.ScoreFor(3)}. " +
                      (winningSlot < 0 ? "draw" : $"seat {winningSlot} wins"));
        }

        /// <summary>
        /// ⚠️ ROLE ROTATION IS EXACTLY WHAT TRIGGERS THE SPAWN-SETTLE BUG, because two seats
        /// trade marks and each stands on the other's stale collider for a frame. Teleport()
        /// arms the settle, and it is not an optimisation to skip.
        /// </summary>
        public void ResetWorld(int defenderSlot)
        {
            for (int slot = 0; slot < Seats.Length; slot++)
            {
                var m = Seats[slot];
                if (m == null) continue;

                m.IsDefender = slot == defenderSlot;
                m.HoldingSlipper = false;
                m.Stamina.RefillAndClearFatigue();

                // ⚠️ THE SPAWN IS RECORDED, NOT JUST USED. The kill plane returns whoever falls
                // off the world to their OWN spawn, and it has no other way to know where that
                // is. Written every round because the mark moves when roles rotate.
                Vector3 mark = m.IsDefender
                    ? DefenderMark()
                    : AttackerSpawn(AttackerRoleFor(slot, defenderSlot));
                m.SpawnPosition = mark;
                m.Teleport(mark);

                // ⚠️⚠️ EVERYONE FACES THE LATA AT THE START OF A ROUND, and nothing set a
                // rotation at all. `main.gd::_role_spawn_yaw` exists for one reason: the taya
                // because the can is what they are guarding, the attackers because it is what
                // they are aiming at. Spawned facing world +Z instead, three of the four opened
                // every round looking at a wall, and the human seat began the match with its
                // camera pointed away from the game.
                //
                // ⚠️ AND IT IS THE WHOLE ROTATION, NOT JUST THE YAW. Writing only the y leaves
                // whatever pitch and roll the body carried, which is exactly the class of
                // leftover basis that ends up in a player's eye.
                Vector3 toCan = -new Vector3(mark.x, 0.0f, mark.z);

                m.transform.rotation = toCan.sqrMagnitude < 0.0001f
                    ? Quaternion.identity
                    : Quaternion.LookRotation(toCan.normalized, Vector3.up);

                // ⚠️ THE MARK IS FLAT, THE MAP IS NOT. Both arenas have kerbs and slabs at
                // different heights; without this a unit spawns inside one and the settle
                // frames shove it out sideways, which reads as a physics bug.
                MatchHost.SeatOnFloor(m);

                // Roles rotate every round, so the ring and tag have to re-colour with them.
                var plate = m.GetComponentInChildren<Visual.CharacterNameplate>();
                if (plate != null) { plate.ApplySizing(); plate.Refresh(); }
            }

            if (Lata != null)
            {
                // The lata wears whichever seat currently DEFENDS. That rotation is what makes
                // the can stats fair: your can is on the mark for exactly the one round you
                // defend, and everyone defends exactly once.
                Lata.SkinIndex = defenderSlot;
                Lata.HostRestore();
            }

            for (int slot = 0; slot < Slippers.Length; slot++)
            {
                if (Slippers[slot] == null) continue;
                Slippers[slot].transform.position = SlipperHome(slot);
            }
        }

        private static Vector3 DefenderMark() =>
            new Vector3(0.0f, 0.0f, -Balance.DefenderStartOffset);

        /// <summary>
        /// The three attackers stand on ONE LINE, not spread around the ring.
        ///
        /// ⚠️⚠️ THIS WAS A CIRCLE AND THE ORIGINAL IS A LINE. `main.gd::_role_spawn_point`
        /// puts every attacker at z = the ring with x = (role - 2) × spacing, so they start
        /// as a row facing the can: "close enough to read as one group, far enough that
        /// nobody spawns inside anybody". Spreading them around the ring surrounds the taya
        /// at the whistle, which is a different opening to the one the game was tuned around.
        ///
        /// ⚠️ AND IT INDEXES BY ROLE, NOT BY SEAT. Role 0 is the taya; the attackers are
        /// roles 1-3 whatever seats they hold this round. Feeding a seat index in gives a
        /// different line every round for no reason a player can see.
        /// </summary>
        private static Vector3 AttackerSpawn(int roleIndex)
        {
            float ring = Confinement.AttackerSpawnRing();
            float offset = (roleIndex - 2.0f) * Balance.AttackerSpawnSpacing;

            return new Vector3(offset, 0.0f, ring);
        }

        /// <summary>
        /// Which of the three attacker positions a seat takes this round: 1, 2 or 3, skipping
        /// whichever seat is the taya.
        /// </summary>
        private static int AttackerRoleFor(int slot, int defenderSlot)
        {
            int role = 1;

            for (int s = 0; s < Balance.PlayerCount; s++)
            {
                if (s == defenderSlot) continue;
                if (s == slot) return role;
                role++;
            }

            return 1;
        }

        /// <summary>
        /// Where a slipper waits at the start of a round: at its owner's feet.
        ///
        /// ⚠️⚠️ THE HEIGHT COMES FROM THE FLOOR, NOT FROM A LITERAL 0.045. That number is the
        /// .gd's `REST_HEIGHT`, which is a height ABOVE THE GROUND, and using it as a world y
        /// is only correct on a map whose floor is at zero. Neither of ours is, so every round
        /// opened with three tsinelas hovering over the road. 🧑 2026-08-16: *"also ur slippers
        /// are floating"*. `Slipper.Land` had the same fault and is fixed the same way; this is
        /// the other half, because a slipper that is never thrown never lands.
        /// </summary>
        private static Vector3 SlipperHome(int slot)
        {
            Vector3 p = AttackerSpawn(AttackerRoleFor(slot, GameServices.Match?.DefenderSlot ?? 0));

            // ⚠️ THROUGH `Slipper.GroundY`, WHICH SKIPS BODIES. A slipper starts at its owner's
            // feet, so a naive downward cast from above that mark lands on the owner's own head.
            return new Vector3(p.x, Slipper.GroundY(p) + Balance.SlipperRestHeight, p.z);
        }
    }
}
