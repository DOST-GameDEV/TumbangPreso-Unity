using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// Tests for the parts of the port that CANNOT live in Core.Tests, because they need
    /// UnityEngine types.
    ///
    /// ⚠️ EVERYTHING THAT CAN BE ASSERTED WITHOUT UNITY BELONGS IN Core.Tests INSTEAD. Those
    /// run in 89 ms from a terminal with no editor involved. Only put a test here when it
    /// genuinely needs a GameObject, a Transform, or a MonoBehaviour lifecycle: this suite is
    /// orders of magnitude slower to run and correspondingly less likely to be run.
    /// </summary>
    public class RuntimeLayerTests
    {
        // -------------------------------------------------------------------
        // InputIntent: the shared human/AI table.
        // -------------------------------------------------------------------

        [Test]
        public void Intent_DerivesEdgesFromTheCommittedFrame()
        {
            var i = new InputIntent();

            i.Set(Verb.Grab, true);
            Assert.IsTrue(i.Pressed(Verb.Grab));
            Assert.IsTrue(i.JustPressed(Verb.Grab), "first frame held is a press edge");

            i.CommitFrame();
            Assert.IsTrue(i.Pressed(Verb.Grab));
            Assert.IsFalse(i.JustPressed(Verb.Grab), "still held is not a new press");

            i.Set(Verb.Grab, false);
            Assert.IsTrue(i.JustReleased(Verb.Grab));

            i.CommitFrame();
            Assert.IsFalse(i.JustReleased(Verb.Grab));
        }

        /// <summary>
        /// ⚠️ PARKED IS NOT THE SAME AS NO INPUT. A verb held across a park boundary must read
        /// as released, or the player walks out of the pause menu already sprinting.
        /// </summary>
        [Test]
        public void Intent_ParkedReportsEverythingReleased()
        {
            var i = new InputIntent();
            i.Set(Verb.Sprint, true);
            i.Move = new Vector2(1.0f, 0.0f);
            i.CommitFrame();

            i.Parked = true;

            Assert.IsFalse(i.Pressed(Verb.Sprint));
            Assert.IsFalse(i.JustPressed(Verb.Sprint));
            Assert.AreEqual(Vector2.zero, i.MoveAxis);
        }

        [Test]
        public void Intent_ClearsBotFacingWithoutReleasingACharge()
        {
            var i = new InputIntent();
            i.Set(Verb.SpecialAbility, true);
            i.AimPoint = Vector3.forward * 10.0f;
            i.FaceAimPoint = true;

            i.ClearAim();

            Assert.IsFalse(i.HasAimPoint);
            Assert.IsFalse(i.FaceAimPoint);
            Assert.IsTrue(i.Pressed(Verb.SpecialAbility),
                "clearing last frame's aim must not release a held throw");
        }

        [Test]
        public void TayaJabReportsTagCooldownInsteadOfPunchCooldown()
        {
            var go = new GameObject("CooldownLabelTaya", typeof(CharacterController));
            var motor = go.AddComponent<CharacterMotor>();
            typeof(CharacterMotor).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(motor, null);
            motor.IsDefender = true;
            var carrier = go.AddComponent<Carrier>();
            var verbs = go.AddComponent<CombatVerbs>();

            var cooldown = typeof(CombatVerbs).GetField(
                "_punchCooldown", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(cooldown);
            cooldown.SetValue(verbs, 1.0f);

            var rows = new List<StatusRow>();
            StatusStack.Collect(motor, carrier, verbs, rows);

            Assert.IsTrue(rows.Exists(row => row.Label == "TAG CD"));
            Assert.IsFalse(rows.Exists(row => row.Label == "PUNCH CD"));

            Object.DestroyImmediate(go);
        }

        // -------------------------------------------------------------------
        // Confinement, through a real Transform.
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ THE TAYA IS CLAMPED IN; EVERYONE ELSE IS MERELY IN DANGER. An attacker clamped by
        /// mistake cannot retrieve a slipper at all, which deletes the game.
        /// </summary>
        [Test]
        public void OnlyTheDefenderIsConfined()
        {
            Assert.IsTrue(Confinement.IsConfined(roundActive: true, isDefender: true));
            Assert.IsFalse(Confinement.IsConfined(roundActive: true, isDefender: false));

            // ⚠️ AND NOBODY IS CONFINED WHILE THE ROUND IS NOT LIVE, or a taya is trapped in
            // the box through the intermission and cannot walk to their next mark.
            Assert.IsFalse(Confinement.IsConfined(roundActive: false, isDefender: true));
        }

        [Test]
        public void ClampKeepsTheCornerReachable()
        {
            // The corner of the square is legal ground for the taya. A radial clamp would
            // pull them off it, and the corner is exactly where a taya stands to cover one.
            float x = Balance.ConfinementRadius - 0.01f;
            float z = Balance.ConfinementRadius - 0.01f;
            float ox = x, oz = z;

            Confinement.ClampToBox(ref x, ref z);

            Assert.AreEqual(ox, x, 0.0001f, "the corner must survive the clamp untouched");
            Assert.AreEqual(oz, z, 0.0001f);
            Assert.IsTrue(Confinement.IsInsideBox(x, z));
        }

        // -------------------------------------------------------------------
        // Scoring, through the director.
        // -------------------------------------------------------------------

        [Test]
        public void Scoreboard_AccumulatesAcrossRoundsAndReportsDraws()
        {
            var board = new Scoreboard();

            board.Add(0, ScoreEvent.LataKnocked);
            board.Add(0, ScoreEvent.DefenseTick);
            Assert.AreEqual(110, board[0]);

            board.Add(1, ScoreEvent.Tag);
            board.Add(1, ScoreEvent.DefenseTick);
            Assert.AreEqual(-1, board.WinningSlot(), "an exact tie is an honest draw");

            board.Add(1, ScoreEvent.Sabotage);
            Assert.AreEqual(1, board.WinningSlot());
        }

        /// <summary>
        /// ⚠️ AN OUT-OF-RANGE SLOT MUST NOT THROW AND MUST NOT SCORE. This is read on paths fed
        /// by a replicated int, where -1 is a legitimate "no seat" value.
        /// </summary>
        [Test]
        public void Scoreboard_IgnoresSlotsThatDoNotExist()
        {
            var board = new Scoreboard();

            Assert.DoesNotThrow(() => board.Add(-1, ScoreEvent.Tag));
            Assert.DoesNotThrow(() => board.Add(99, ScoreEvent.Tag));
            Assert.AreEqual(0, board.Total);
            Assert.AreEqual(0, board[-1]);
        }

        // -------------------------------------------------------------------
        // The spawn ring, which is derived rather than authored.
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ AN ATTACKER SPAWNED INSIDE THE BOX IS VULNERABLE ON FRAME ONE, and it reads as a
        /// rules bug rather than as the placement bug it is. That is precisely why spawns are
        /// computed from the box instead of read from map markers.
        /// </summary>
        [Test]
        public void EverySpawnRingPointIsOutsideTheBox()
        {
            float ring = Confinement.AttackerSpawnRing();

            for (int i = 0; i < 360; i += 5)
            {
                float a = i * Mathf.Deg2Rad;
                float c = Mathf.Cos(a), s = Mathf.Sin(a);
                float scale = 1.0f / Mathf.Max(Mathf.Abs(c), Mathf.Abs(s));

                float x = c * ring * scale;
                float z = s * ring * scale;

                Assert.IsFalse(Confinement.IsInsideBox(x, z),
                    $"spawn at {i} degrees ({x:F2}, {z:F2}) is inside the box");
            }
        }

        /// <summary>
        /// ⚠️ THE THROWING LINE, NOT THE BOX, IS WHAT HAS TO FIT THE MAP, and there is a THIRD
        /// bound past it that nobody had written down until bots jammed against a wall: the
        /// AI's standoff ring has to fit inside the wall faces too.
        /// </summary>
        [Test]
        public void TheStandoffRingFitsInsideThePlayableArea()
        {
            const float throwStandoff = 1.2f;
            const float capsuleRadius = 0.4f;

            float ring = Balance.ConfinementRadius + throwStandoff + capsuleRadius;

            Assert.LessOrEqual(ring, AIController.PlayableHalfX,
                "the standoff ring lands inside a wall: bots will jam against it trying to " +
                "reach a goal they can never stand on, and it reads as broken pathfinding");
        }

        // -------------------------------------------------------------------
        // Hero Ability System & Gamemode Tests
        // -------------------------------------------------------------------

        [Test]
        public void HeroKits_CreateSuccessfully_ForEveryHero()
        {
            string[] heroes = { "zack", "cheska", "dante", "nemu", "sean" };
            foreach (var h in heroes)
            {
                var kit = Abilities.HeroAbilitySystem.CreateKitFor(h);
                Assert.IsNotNull(kit, $"kit for {h} must be created");
                Assert.IsNotNull(kit.Skill1, $"skill 1 for {h} must exist");
                Assert.IsNotNull(kit.Skill2, $"skill 2 for {h} must exist");
                Assert.IsNotNull(kit.Ultimate, $"ultimate for {h} must exist");
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE HALFWAY POINT IS HALF OF `UltimateCost`, NOT HALF OF 100, AND WRITING 50.0f
        /// HERE IS WHAT BROKE THIS TEST. Until 2026-08-25 every hero's ultimate cost the same
        /// `HeroKit.UltimateMax`, so the two were interchangeable and a literal was harmless.
        /// They are not interchangeable now: Zack pays 150 because Thunderstrike cannot miss,
        /// and Nemu pays 90 because Seance Void ends no round on its own.
        ///
        /// Derived from the kit rather than restated, so this keeps testing "half fills half the
        /// bar" through any future retune instead of testing one hero's price.
        /// </summary>
        [Test]
        public void HeroKit_ChargesAndActivates_Ultimate()
        {
            var kit = new Abilities.ZackHeroKit();
            Assert.AreEqual(0.0f, kit.UltimateCharge);
            Assert.IsFalse(kit.IsUltimateReady);

            float half = kit.UltimateCost * 0.5f;

            kit.AddUltimateCharge(half);
            Assert.AreEqual(0.5f, kit.UltimateRatio, 0.001f);
            Assert.IsFalse(kit.IsUltimateReady);

            kit.AddUltimateCharge(half);
            Assert.AreEqual(1.0f, kit.UltimateRatio, 0.001f);
            Assert.IsTrue(kit.IsUltimateReady);
        }

        [Test]
        public void Nemu_AstralProjection_SupportsReactivation()
        {
            var nemu = new Abilities.NemuHeroKit();
            Assert.IsTrue(nemu.Skill2.CanReactivate, "Nemu Skill 2 should support early reactivation");

            var go = new GameObject("TestMotor");
            var motor = go.AddComponent<CharacterMotor>();
            var ctx = new Abilities.AbilityContext(motor, null, null);

            Assert.IsTrue(nemu.TryActivateSkill2(ctx));
            Assert.IsTrue(nemu.Skill2.IsActive);

            // Second activation reactivates and ends early (teleport trigger)
            Assert.IsTrue(nemu.TryActivateSkill2(ctx));
            Assert.IsFalse(nemu.Skill2.IsActive, "Second activation should end early");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void MatchDirector_WarmupBuffer_BlocksScoreAwards()
        {
            var go = new GameObject("TestMatchDirector");
            var match = go.AddComponent<MatchDirector>();
            match.StartMatch();

            // When in live match, scoring works
            match.AddScore(0, ScoreEvent.Tag);
            Assert.AreEqual(Balance.ScoreTag, match.ScoreFor(0));

            // When warmup buffer is active, scoring is blocked
            match.IsWarmupBuffer = true;
            match.AddScore(0, ScoreEvent.LataKnocked);
            Assert.AreEqual(Balance.ScoreTag, match.ScoreFor(0), "Score must not increase during warmup buffer");

            Object.DestroyImmediate(go);
        }

        /// <summary>
        /// There is no ceiling on a score, at any point in the path that awards one.
        ///
        /// ⚠⚠ THIS EXISTS BECAUSE A PLAYTEST SAID THERE WAS ONE AND A SOURCE READ COULD NOT FIND
        /// IT. 🧑 2026-08-30: *"theres also a 6k points cap bug pls remove poitns cap"*, *"my
        /// playtesters found that it didnt go past 6k"*, with a result board reading
        /// **SEAN 6000 PTS** over 5530, 4880 and 4405.
        ///
        /// Every candidate was checked by reading and every one came back clean: `Scoreboard.Add`
        /// floors at zero and has no ceiling, the field is `int[]`, `MatchResult.RenderStandings`
        /// prints `ScoreFor` with no clamp, `SyncWorld` carries `int[]` in a 256-byte writer
        /// against about 54 bytes of payload, and a regex sweep for any four-digit clamp across
        /// the runtime and the core package returns nothing. `docs/TODO.md` § 84.14 lists the
        /// whole search so nobody runs it twice.
        ///
        /// ⚠ SO THIS IS A TRIPWIRE RATHER THAN A REPRODUCTION. It drives the real award path,
        /// guards and all, well past the number he reported. **If it is ever red, the cap is in
        /// this layer and this test names it.** While it is green the fault is somewhere else,
        /// and the next place to look is what STOPS awards rather than what bounds them:
        /// `AddScore` returns silently on `!MatchInProgress` and on `IsWarmupBuffer`, and the
        /// test directly above this one is about the second of those.
        ///
        /// ⚠ IT AWARDS THROUGH `AddScore`, NOT THROUGH `Scoreboard`, deliberately. The container
        /// is engine-free and provably uncapped; the guards are the part that could stop a match
        /// dead at an arbitrary number, and they are only reachable from here.
        /// </summary>
        [Test]
        public void MatchDirector_HasNoPointsCeiling_PastTheReported6000()
        {
            var go = new GameObject("TestScoreCeiling");
            var match = go.AddComponent<MatchDirector>();
            match.StartMatch();

            // 100 knockdowns at `ScoreLataKnocked` is 10000, comfortably past 6000 and past any
            // plausible round-length maximum, on one seat.
            for (int i = 0; i < 100; i++) match.AddScore(0, ScoreEvent.LataKnocked);

            Assert.AreEqual(Balance.ScoreLataKnocked * 100, match.ScoreFor(0),
                "the award path stopped short. A ceiling has appeared between MatchDirector."
                + "AddScore and Scoreboard.Add, which is exactly the 6000 report and this test "
                + "is where it is now visible.");

            Assert.Greater(match.ScoreFor(0), 6000,
                "the score did not pass 6000, which is the number the 2026-08-30 playtest "
                + "reported as a ceiling. docs/TODO.md section 84.14.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void GameMode_Rosters_AreDistinctAndCorrectSizes()
        {
            var classic = Roster.GetPeople(GameMode.Classic);
            var heroes = Roster.GetPeople(GameMode.HeroStrike);

            Assert.AreEqual(12, classic.Count);

            // ⚠ SIX SINCE 2026-08-26, when Phaister merged in. The number is asserted rather
            // than derived on purpose: a hero appearing or disappearing from the Hero Strike
            // roster is a product decision and should have to be typed here, not noticed later.
            // `docs/TODO.md` § 21.
            Assert.AreEqual(6, heroes.Count);
            Assert.AreEqual("bayan", classic[0].Id);
            Assert.AreEqual("dante", heroes[0].Id);
        }

        [Test]
        public void RoundSnapshotReappliesCurrentDefenderAndActionState()
        {
            var roundGo = new GameObject("SnapshotRound");
            var round = roundGo.AddComponent<RoundDirector>();
            var players = new CharacterMotor[Balance.PlayerCount];

            for (int slot = 0; slot < players.Length; slot++)
            {
                var go = new GameObject($"SnapshotSeat{slot}");
                go.AddComponent<CharacterController>();
                players[slot] = go.AddComponent<CharacterMotor>();
                players[slot].PlayerSlot = slot;
                players[slot].IsDefender = slot == 0; // stale role from the disconnected round
                round.Register(players[slot]);
            }

            round.ApplySnapshot(37.5f, true, defenderSlot: 2);

            Assert.AreEqual(37.5f, round.TimeLeft, 0.001f);
            Assert.IsTrue(round.RoundActive);
            for (int slot = 0; slot < players.Length; slot++)
            {
                Assert.AreEqual(slot == 2, players[slot].IsDefender);
                Assert.IsTrue(players[slot].RoundActive);
                Object.DestroyImmediate(players[slot].gameObject);
            }
            Object.DestroyImmediate(roundGo);
        }

        [Test]
        public void SlipperSnapshotRestoresLiveBallisticsWithoutReplayingThrow()
        {
            var go = new GameObject("SnapshotSlipper");
            var slipper = go.AddComponent<Slipper>();
            Vector3 velocity = new Vector3(8.0f, 2.0f, -4.0f);

            slipper.ApplySnapshotState(
                SlipperState.InFlight, null, new Vector3(1.0f, 3.0f, 5.0f),
                Quaternion.Euler(10.0f, 20.0f, 30.0f), velocity, 0.65f,
                SlipperAffinity.ElectricZap, throwerSlot: 3);

            Assert.AreEqual(SlipperState.InFlight, slipper.State);
            Assert.AreEqual(velocity, slipper.Velocity);
            Assert.AreEqual(0.65f, slipper.PektusSpin, 0.001f);
            Assert.AreEqual(SlipperAffinity.ElectricZap, slipper.Affinity);
            Assert.AreEqual(3, slipper.ThrowerSlot);
            Object.DestroyImmediate(go);
        }

        // -------------------------------------------------------------------
        // THE REMATCH VOTE
        //
        // ⚠️⚠️ THE COUNTING RULES ARE IN `Core.Tests`, NOT HERE, because `Core.RematchVote` is
        // engine-free on purpose and that suite runs in a second from a terminal. What is left
        // for this file is the one thing that needs UnityEngine: the sentence the screen draws.
        // ⚠️ AND THE TRANSPORT IS STILL UNPROVEN. `docs/TODO.md` § 1 says why: two real
        // processes on a LAN have never been run, and nothing in this repository can stand in
        // for that.
        // -------------------------------------------------------------------

        [Test]
        public void RematchTallyIsSilentWhenNobodyElseIsVoting()
        {
            Assert.AreEqual("", UI.MatchResult.TallyLine(1, 1),
                "\"1 / 1 WANT A REMATCH\" is a sentence about nobody");
            Assert.AreEqual("", UI.MatchResult.TallyLine(0, 0));
            Assert.AreEqual("2 / 3 WANT A REMATCH", UI.MatchResult.TallyLine(2, 3));
        }

        /// <summary>
        /// ⚠️⚠️ THIS TEST ASSERTED THE OPPOSITE UNTIL 2026-08-27, AND THE THING IT PROTECTED WAS
        /// THE BUG. It required a spawnable `Net/MatchRpc` prefab carrying a `NetworkObject` and a
        /// `MatchRpc`, and `NetSession` duly instantiated and spawned one on every host start.
        ///
        /// ⚠️⚠️ `MatchRpc` IS NOT A `NetworkBehaviour`. It is a plain `MonoBehaviour` already
        /// sitting on `NetSession`, and everything it does travels through
        /// `CustomMessagingManager`, whose handlers are registered against a NAME with exactly one
        /// owner. So the spawned copy was a SECOND router: its `Awake` overwrote `MatchRpc.Instance`
        /// with an object that had never been handed a `NetworkManager`, and every request the
        /// game made afterwards went to a router with no transport. That is the *"heavily broken"*
        /// in 🧑's report, upstream of everything § 32 found.
        ///
        /// The prefab, its meta and the whole `Resources/Net` folder are deleted. What this now
        /// asserts is the shape that replaced it: **one router, on the session object, reachable
        /// as a plain component**, and nothing spawnable that could become a second one.
        /// `MatchRpc.Awake` refuses a second instance outright and says so in the log.
        /// </summary>
        [Test]
        public void TheRpcRouterIsAPlainComponentAndNothingCanSpawnASecondOne()
        {
            Assert.IsNull(Resources.Load<GameObject>("Net/MatchRpc"),
                "a spawnable MatchRpc prefab is a SECOND router: its Awake overwrites "
                + "MatchRpc.Instance with an uninitialised copy and every request after that "
                + "goes nowhere. docs/TODO.md section 38.14 row 1.");

            // ⚠️ THE TYPE IS INSPECTED, NOT INSTANTIATED. `MatchRpc.Awake` claims the static
            // `Instance` or destroys itself, so building one here would either take the singleton
            // away from a live session or come back already destroyed. The question is about the
            // SHAPE of the class and reflection answers it without touching global state.
            var type = typeof(Net.MatchRpc);

            Assert.IsTrue(type.IsSubclassOf(typeof(MonoBehaviour)));

            // ⚠️ THE BASE CHAIN IS WALKED BY NAME, NOT BY TYPE. This test assembly does not
            // reference `Unity.Netcode`, and adding the reference just to name one class would
            // pull the whole transport into a suite that must stay quick and engine-light.
            for (var t = type; t != null; t = t.BaseType)
            {
                Assert.AreNotEqual("NetworkBehaviour", t.Name,
                    "MatchRpc routes named messages by hand and must not become a "
                    + "NetworkBehaviour: a NetworkBehaviour needs a spawned NetworkObject, which "
                    + "is exactly the prefab this test used to require.");
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE GODOT BUG, END TO END, IN ONE TEST. Reported as: a player drops out while
        /// they are an ATTACKER, the round turns over while they are away, and they come back
        /// into a seat that is now the TAYA holding their old attacker state. They cannot tag,
        /// the box does not confine them, and from their side the game is simply broken.
        ///
        /// The repair is three separate pieces and each of them is already covered on its own:
        /// the lobby reclaims by durable TOKEN rather than by transport id, the taya is DERIVED
        /// from the round number rather than remembered, and the round snapshot rewrites every
        /// seat's role. What no test asserted is that the three AGREE, which is the only thing
        /// the player actually experiences. A seat reclaim that lands next to a stale role is
        /// exactly the shipped bug with two thirds of the fix in place.
        /// </summary>
        [Test]
        public void AReturningPlayerWhoseRoleChangedWhileAwayComesBackAsTheTaya()
        {
            var lobby = new Net.LobbySession();
            lobby.OpenLobby(new System.Random(7));

            lobby.Admit(41, "token-host", "Host");
            var leaving = lobby.Admit(42, "token-returning", "Returning");
            lobby.Admit(43, "token-third", "Third");
            lobby.Admit(44, "token-fourth", "Fourth");

            int seat = leaving.Seat;
            Assert.AreEqual(1, seat, "the fixture wants the seat that defends in round two");

            lobby.StartMatch();

            // Round 1: this seat is an ATTACKER, which is the state it disconnects holding.
            Assert.AreNotEqual(seat, MatchRules.DefenderSlotFor(1));

            var roundGo = new GameObject("RejoinRound");
            var round = roundGo.AddComponent<RoundDirector>();
            var seats = new CharacterMotor[Balance.PlayerCount];

            for (int slot = 0; slot < seats.Length; slot++)
            {
                var go = new GameObject($"RejoinSeat{slot}");
                go.AddComponent<CharacterController>();
                seats[slot] = go.AddComponent<CharacterMotor>();
                seats[slot].PlayerSlot = slot;
                seats[slot].IsDefender = slot == MatchRules.DefenderSlotFor(1);
                seats[slot].RoundActive = true;
                round.Register(seats[slot]);
            }

            lobby.Depart(42);

            // ⚠️ THE ROUND TURNS OVER WHILE THEY ARE GONE. This is the whole scenario: nothing
            // is wrong until the role they left with stops being the role they own.
            int defenderNow = MatchRules.DefenderSlotFor(2);
            Assert.AreEqual(seat, defenderNow, "round two must hand this seat the taya");

            // ⚠️ A NEW TRANSPORT CONNECTION MEANS A NEW PEER ID, ALWAYS. Using it as identity
            // is the original fault; the durable token is what reclaims the seat.
            var returning = lobby.Admit(915, "token-returning", "Returning");
            Assert.AreEqual(seat, returning.Seat, "the seat was not reclaimed by token");
            Assert.AreSame(returning, lobby.PeerById(915));

            // What the host sends on the way back in, which is the authoritative round state
            // rather than anything the client remembered.
            round.ApplySnapshot(52.5f, roundActive: true, defenderSlot: defenderNow);

            Assert.IsTrue(seats[seat].IsDefender,
                "the returning player is still holding their old attacker role, which is the " +
                "reported bug: they cannot tag and the chalk box does not hold them");

            Assert.IsTrue(seats[seat].RoundActive,
                "a returning player who is not round-active cannot act at all");

            Assert.IsTrue(Confinement.IsConfined(seats[seat].RoundActive, seats[seat].IsDefender),
                "the box has to close around them the moment they are the taya again");

            for (int slot = 0; slot < seats.Length; slot++)
            {
                Assert.AreEqual(slot == defenderNow, seats[slot].IsDefender,
                    $"seat {slot} disagrees with the authoritative defender after the rejoin");
                Object.DestroyImmediate(seats[slot].gameObject);
            }

            Object.DestroyImmediate(roundGo);
        }
    }
}
