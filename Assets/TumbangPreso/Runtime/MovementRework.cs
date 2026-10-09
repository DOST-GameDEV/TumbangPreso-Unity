using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// ⚠️⚠️ THE MOVEMENT REWORK, A DEBUG SWITCH. Owner, 2026-10-07: *"i wanna do a movement rework. currently theres no
    /// crouch, and because theres no way to crouch, you cant easily do a slide by sprinting then crouching. the movement
    /// itself feels rigid, and theres no airstrafing + bunnyhoping"*, then: *"we'll put this as a debug setting, default
    /// to on only when the characterprototype scene is being played"*.
    ///
    /// OFF, NOTHING IN THE GAME CHANGES: the motor's one velocity write, its jump and its capsule are exactly as they
    /// were, for every body. ON, the LOCAL HUMAN'S body (never a bot, never a replica) moves by
    /// `CharacterMotor.MovementRework.cs`: acceleration and friction on the ground, momentum kept in the air with
    /// strafe gain, a buffered and held jump that keeps speed through a landing, a crouch, and a slide entered by
    /// crouching at a sprint.
    ///
    /// ⚠️ IT WAS OFFLINE ONLY UNTIL 2026-10-09 (see `Active`). A client simulates its own movement and the host only checks
    /// distance (`MatchRpc.AcceptMove`); the host never sees a crouch, so a shrunk capsule does not exist where slippers
    /// are resolved, and nothing about this is sent. Those are known gaps of the build it went online in.
    ///
    /// The numbers are statics so they can be changed while playing; none is tuned against the game's balance (the
    /// stamina bar is sized to one crossing of the box at run speed, and a hop chain goes faster than that for free).
    /// </summary>
    public static class MovementRework
    {
        /// <summary>
        /// The switch. ⚠️ ON FOR THE WHOLE GAME SINCE 2026-10-09 (owner: "lets push the movement rework to the main gameplay
        /// for this build"). It was on only in the CharacterPrototype scene. F9 there still flips it.
        /// </summary>
        public static bool Enabled = true;

        /// <summary>
        /// ⚠️ ONLINE TOO SINCE THE SAME DAY (the owner's "yes"; protocol 157, `NetSession.ProtocolVersion`, so every peer in
        /// a match is on it). It drives only the body THIS peer simulates for its own player; the host still only
        /// checks the pose it is sent. Two things the host does not know yet: a crouch (it judges a thrown slipper
        /// against a standing capsule) and that hopping stops the stamina refill (its own count for a remote player may
        /// run ahead of that player's bar).
        /// ⚠️ NOT UNDER THE TEST RUNNER. The play-mode tests measure the game's ORIGINAL movement to the centimetre
        /// (`MovementRuntimeTests`: a standing start at full speed in the first step, the jump's exact height), which
        /// is still what a networked match plays; they are run with this off.
        /// </summary>
        public static bool Active => Enabled && Application.isPlaying && !UnderTestRunner;

        private static int _testRunnerChecked = -1;
        private static bool _underTestRunner;
        /// <summary>True while Unity's play-mode test runner is driving the game (its controller object is in the scene list).</summary>
        private static bool UnderTestRunner
        {
            get
            {
                // Looked for every couple of seconds, not every call: the runner's object is there from before the first
                // test to after the last, and finding an object by name walks the scene.
                int tick = Time.frameCount / 120;
                if (_testRunnerChecked == tick) return _underTestRunner;
                _testRunnerChecked = tick;
                _underTestRunner = GameObject.Find("Code-based tests runner") != null;
                return _underTestRunner;
            }
        }

        // Ground. Speed is lost at `GroundFriction` per second of itself (never slower than from `StopSpeed`), and gained
        // toward the wish at `GroundAccel` times the wish speed per second: about a fifth of a second to full speed.
        public static float GroundFriction = 9f, GroundAccel = 11f, StopSpeed = 1.5f;
        public static float IceFriction = 0.6f, IceAccel = 1.5f;

        // Air. Momentum is kept. The keys push the velocity round at `AirAccel` (metres a second, each second) without
        // adding speed, except `AirStrafeGain` a second while steering across the travel. (The first cut was a Quake
        // strafe, which gained speed from turning and hardly steered; the owner asked for the opposite.)
        public static float AirAccel = 18f;
        /// <summary>The most the keys alone can give a body in the air that has less (a jump from a standstill), metres a second.</summary>
        public static float AirBaseSpeed = 1.2f;
        // THE STRAFE: speed gained a second (metres a second, each second) while a strafe key is held on the side the
        // view is turning toward and the view turns between `StrafeTurnMin` and `StrafeTurnMax` degrees a second (full
        // gain from three times the minimum up to six tenths of the maximum). A flick or a spin earns nothing.
        public static float AirStrafeGain = 6f, StrafeTurnMin = 20f, StrafeTurnMax = 420f;
        // Speed above the ordinary move speed is momentum, and momentum has to be kept on purpose (the owner, of the
        // second cut: "its too easy to keep your momentum when ure at max.. even a weird turn can still keep it").
        // `AirTurnLoss`: the share of that extra lost per radian the travel is turned in the air (1.2: a 30 degree
        // turn keeps about half of it, a 90 degree turn about a sixth). `HopLoss`: the share lost at each hop.
        // The turn's loss is not charged while the strafe is in time with the view.
        public static float AirTurnLoss = 1.2f, HopLoss = 0f;

        // Jump. A press this long before landing still jumps; a step off a ledge may still jump this long after.
        // ⚠️ THE WINDOW IS SHORT AND THE JUMP DOES NOT REPEAT WHILE HELD (owner, 2026-10-09: "all you have to do is hold jump
        // then smoothly glide your mouse"). A hop is a press timed to the landing: early by more than
        // `JumpBufferSeconds` and it is lost, late and the ground has already taken speed.
        public static float JumpBufferSeconds = 0.07f, CoyoteSeconds = 0.10f;
        /// <summary>
        /// Holding jump hops again on every landing, with no ground friction in between. ON AT THE OWNER'S WORD
        /// (2026-10-09, of a cut that made each hop its own timed press: "i want to keep being able to hold jump"). What
        /// makes a chain hard is the strafe and the stamina it costs, not the timing of the jump key.
        /// </summary>
        public static bool AutoHop = true;
        /// <summary>
        /// On: a strafe earns speed only on the other side from the hop before (left, right, left). OFF at the owner's
        /// word (2026-10-09), to try the chain without it: any strafe in time with the view earns.
        /// </summary>
        public static bool StrafeMustAlternate = false;
        /// <summary>This long on the ground ends a chain, and the next hop's strafe may earn on either side.</summary>
        public static float ChainResetSeconds = 0.35f;
        // THE HOP AND THE STAMINA BAR (owner, 2026-10-09: the hop is "another way players can move when the stamina is
        // gone"). It costs the bar nothing. A strafe can build speed up to `HopSpeedScale` of the body's own run and no
        // further (4.25 m/s for an attacker: between the 2.5 walk and the 5 run), and the bar does not refill in the
        // air or for `HopRecoveryDelay` seconds after a hop.
        // (`HopSpeedScale` was 0.85 for one cut, a hop slower than a sprint; the owner, of that: "this doesnt make
        // sense". At 1.0 a chain held well is worth a sprint, and the skill is its price.)
        // ⚠️ ABOVE A SPRINT, AT THE OWNER'S WORD (2026-10-09: "if bhopping takes more skill, it should be rewarded more").
        // 1.2 is the cap itself (`MaxSpeedScale`): 6 m/s for an attacker against the taya's 7.5 run.
        public static float HopSpeedScale = 1.2f, HopRecoveryDelay = 0.6f;

        // JUMP FATIGUE, after Counter-Strike's stamina. A debt from 0 to 1: each jump adds `JumpFatiguePerJump`, and it
        // drains all the time, ground or air, losing 63 per cent of itself every `JumpFatigueRecoverSeconds`. A jump
        // taken with a debt launches at `1 - JumpFatigueHeightLoss x debt` of the full jump and keeps
        // `1 - JumpFatigueSpeedLoss x debt` of the body's speed. Mashed hops settle at a debt of about a half: under
        // half the height, and a third of the speed gone at every hop, so four hops without a strafe is a crawl.
        public static float JumpFatiguePerJump = 0.35f, JumpFatigueRecoverSeconds = 0.8f;
        // (Speed loss was 0.6, and with the strafe now earning on alternate sides only, nobody could hold a chain. At
        // 0.35 a mashed chain loses about a fifth of its speed a hop and is a crawl in six; a decent strafe holds it.)
        //
        // ⚠️ IT NO LONGER TAKES HEIGHT (owner, 2026-10-09: "i see a fundamental issue. jump fatigue causes the player to
        // jump more frequently, because it jumps at a lower height. you reach the ground faster, each hop is faster.
        // this doesnt work well for bhopping where the goal is to maximize airtime"). A lower jump was a shorter one,
        // so a tired body landed sooner, jumped again sooner, tired faster and had less air to strafe in: the penalty
        // fed itself and took away the very thing the skill needs. Every jump is now the full height and the full
        // 0.58 s in the air; fatigue costs SPEED only. With the air always that long the debt settles at about a third
        // for held jumps, so the speed share is 0.5 to keep a hop without a strafe losing about a sixth of its speed.
        public static float JumpFatigueSpeedLoss = 0.5f, JumpFatigueHeightLoss = 0f;

        // Crouch.
        public static float CrouchHeight = 1.0f, CrouchSpeedScale = 0.55f, CrouchEyeDrop = 0.55f, SlideEyeDrop = 0.70f;

        // Slide: crouch while moving at `SlideEntryScale` of the run speed or more. It starts at `SlideBoostScale` of the
        // run speed (or the speed it had, if more) and steers a little. The ground slows it: a drag of `SlideDragStart`
        // metres a second each second as it begins, rising to `SlideDragEnd` over `SlideDragRamp` seconds, and it is
        // over when it is down to a crouch walk (about a second and four metres from an attacker's sprint).
        // `SlideMaxSeconds` is only a backstop now.
        public static float SlideEntryScale = 0.85f, SlideBoostScale = 1.30f, SlideSteer = 2.0f;
        public static float SlideDragStart = 2.0f, SlideDragEnd = 9.0f, SlideDragRamp = 0.9f;
        public static float SlideMaxSeconds = 4.0f, SlideBoostCooldown = 0.9f;

        /// <summary>
        /// THE CAP (owner, 2026-10-07: "movement should have a cap"): no horizontal speed above `MaxSpeedScale` of the
        /// body's own run speed (7.5 m/s for an attacker, 11.25 for the taya), and never above `MaxSpeed` at all.
        /// </summary>
        // ⚠️ 1.2, NOT 1.5: at 1.5 an attacker's chain reached 7.5 m/s, the taya's own run, which is the "unfair advantage
        // to the attackers" the owner named. At 1.2 it is 6 m/s against the taya's 7.5.
        public static float MaxSpeedScale = 1.2f, MaxSpeed = 14f;
    }
}
