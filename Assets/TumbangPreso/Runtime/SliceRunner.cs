using System.Collections.Generic;
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

            Subscribe();

            if (TimeScale > 1.0f) Time.timeScale = TimeScale;

            Running = true;

            // ⚠️ THE ONE PLACE THE ULTIMATE BANK IS EMPTIED. Charge carries across the four
            // rounds of a match on purpose; it must not carry into the NEXT match, and this
            // component is reachable more than once in a session.
            foreach (var seat in Seats)
                seat?.GetComponent<Abilities.HeroAbilitySystem>()?.ResetKitForMatch();

            GameServices.Match.StartMatch();
        }

        /// <summary>
        /// ⚠️⚠️ THE SUBSCRIPTION HAS TO BE UNDONE, AND NOT DOING IT IS WHAT FROZE EVERY SECOND
        /// MATCH AT 00:00. 🧑 2026-08-18, with a screenshot of the clock stopped and the body
        /// unable to move: *"WHY TF is it just stuck here when round ends, before it used to go
        /// to an end screen"*.
        ///
        /// `MatchDirector` is `DontDestroyOnLoad` and this component is not: it dies with the
        /// arena scene, and its three delegates stayed on the director's events pointing at a
        /// DESTROYED MonoBehaviour. His own `Player.log` has the whole thing:
        ///
        ///     [Slice] round 1 begins, taya is seat 0      <- the corpse from the first match
        ///     [Slice] round 1 begins, taya is seat 0      <- the live runner
        ///     ArgumentNullException: Value cannot be null. Parameter name: self
        ///       at UnityEngine.MonoBehaviour.InvokeDelayed (...)
        ///       at TumbangPreso.SliceRunner.OnIntermission (...)
        ///       at TumbangPreso.MatchDirector.BeginIntermission ()
        ///
        /// A C# event invokes its list IN ORDER and does not catch, so the dead runner's
        /// `Invoke` threw and every subscriber after it — including the LIVE runner, the one
        /// that schedules `Advance` — was never reached. The round therefore ended, froze the
        /// cast (`EndRound` clears `RoundActive`, which is `CanAct`), and nothing ever started
        /// the next one. The first match of a session is unaffected, which is why it survived
        /// every headless probe: nothing had died yet.
        ///
        /// ⚠️ AND IT IS IDEMPOTENT. `Begin` is reachable more than once — the ready gate raises
        /// `RoundShouldBegin`, and a probe may call it directly — so subscribing without
        /// removing first is how ONE live runner ends up running a round twice.
        /// </summary>
        private void Subscribe()
        {
            Unsubscribe();

            GameServices.Match.RoundStarted += OnRoundStarted;
            GameServices.Match.IntermissionStarted += OnIntermission;
            GameServices.Match.MatchEnded += OnMatchEnded;
        }

        private void Unsubscribe()
        {
            if (GameServices.Match == null) return;

            GameServices.Match.RoundStarted -= OnRoundStarted;
            GameServices.Match.IntermissionStarted -= OnIntermission;
            GameServices.Match.MatchEnded -= OnMatchEnded;
        }

        /// <summary>⚠️ OnDestroy, NOT OnDisable. Leaving the match destroys the scene without
        /// disabling anything first, which is exactly the path that leaked.</summary>
        private void OnDestroy()
        {
            Unsubscribe();

            // A pending Advance from an intermission that was interrupted by leaving the match
            // would fire into a dead runner on the next scene. Cancel it with the object.
            CancelInvoke();
        }

        private void OnRoundStarted(int roundNumber, int defenderSlot)
        {
            ResetWorld(defenderSlot);
            GameServices.Round.BeginRound();

            // ⚠️ AFTER BeginRound, AND THE ORDER IS THE WHOLE REASON THE EQUIP WORKS.
            // `main.gd::_equip_owned_slippers` returns early on `not RoundManager.round_active`,
            // and `Slipper.CanBeGrabbedBy` asks `CanAct()`, which is `RoundActive && NORMAL`.
            // Handing the tsinelas over inside `ResetWorld` is the version Godot measured and
            // reverted: *"0 throws, 0 knockdowns, three bots stuck in FETCH for a whole match"*.
            EquipOwnedSlippers(defenderSlot);

            Debug.Log($"[Slice] round {roundNumber} begins, taya is seat {defenderSlot}");
        }

        /// <summary>
        /// § EVERY ATTACKER STARTS THE ROUND HOLDING THEIR OWN TSINELAS.
        ///
        /// ⚠️⚠️ THE PORT NEVER CARRIED THIS AND IT IS TWO REPORTED BUGS AT ONCE. 🧑 2026-08-18:
        /// *"supposed to spawn in with slippers when it starts and ur an attacker"* and, of the
        /// tsinelas lying on the road in front of him, *"why is there a slipper here that i have
        /// to pick up"*. They are the same fault seen from both ends: `ResetWorld` parked each
        /// slipper on the ground at its owner's feet and stopped there, so round one opened with
        /// every attacker empty-handed and a retrieval run in front of their first throw.
        ///
        /// It is also most of the third one, *"i still genuinely cant throw shit"*: `CanThrow`
        /// requires `HoldingSlipper`, so with nothing in hand the charge branch in
        /// `Carrier.StepAttacker` returns before it ever starts, and the same press falls
        /// through to the shove. The controls were not fighting each other. There was simply
        /// never any ammunition.
        ///
        /// `main.gd::_reset_slippers` + `_equip_owned_slippers`, both halves:
        ///
        ///  * OWNERSHIP IS ASSIGNED EXPLICITLY, in SEAT ORDER, skipping the taya. The .gd's note
        ///    is that it must not ride on the grab succeeding, because a grab can silently
        ///    refuse and leave a slipper LOOSE with `owner_slot = -1` — which the foot arrow and
        ///    the owner glow then point nowhere for.
        ///  * THE HAND-OVER IS `host_force_equip`, NOT `host_grab`. A grab re-checks the pickup
        ///    RADIUS and `CanAct`, and one frame of interpolation on a seat that has just been
        ///    teleported is enough to miss it.
        ///
        /// ⚠️ THE TAYA GETS NOTHING. They have never been able to throw and the rules now say so.
        /// </summary>
        private void EquipOwnedSlippers(int defenderSlot)
        {
            if (!NetAuthority.ShouldResolve()) return;

            var attackers = new List<int>();
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
                if (slot != defenderSlot) attackers.Add(slot);

            for (int index = 0; index < Slippers.Length; index++)
            {
                var slipper = Slippers[index];
                if (slipper == null) continue;

                // A slipper with no attacker to own it is DISOWNED rather than left holding last
                // round's slot: a stale owner is worse than none, because every gate that reads
                // the field would then refuse everybody.
                slipper.OwnerSlot = index < attackers.Count ? attackers[index] : -1;

                if (slipper.OwnerSlot < 0) continue;

                var owner = GameServices.Round.PlayerAt(slipper.OwnerSlot);
                if (owner == null || owner.IsDefender) continue;

                slipper.transform.position = owner.transform.position;
                slipper.HostForceEquip(owner);
            }
        }

        private void OnIntermission(int nextRound, int nextDefenderSlot)
        {
            ResetWorld(nextDefenderSlot);
            EquipOwnedSlippers(nextDefenderSlot);

            CancelInvoke(nameof(Advance));
            Invoke(nameof(Advance), Balance.WarmupBufferDuration);
        }

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

                // ⚠⚠ THE HERO KIT RESETS WITH THE BODY. Ultimate charge, cooldowns and
                // anything still running are cleared here, at the one place every round
                // boundary already passes through. `HeroKit.Tick` trickles passive charge every
                // frame including practice time, and nothing called `ResetKit` before this, so
                // charge banked before the whistle survived into the next round.
                m.GetComponent<Abilities.HeroAbilitySystem>()?.ResetKit();

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

            // ⚠️ THE HAZARD MAP IS EMPTIED WITH THE ROUND. Hazard objects are destroyed at a
            // round boundary and each unregisters itself in OnDisable, but a teardown that skips
            // OnDisable would leave the bots steering around patches of empty road for the rest
            // of the match. Clearing here costs nothing and cannot go stale.
            Abilities.HazardMap.Clear();

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
