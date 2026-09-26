using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ ONE CHARACTER'S WALK AND RUN. THE NUMBERS ARE AUTHORED BY HAND, PER BODY, IN `GaitStyles`.
    ///
    /// Owner, 2026-09-27, on the walk that fitted every body with the same solver: *"walk is fucking ugly hahaha do it one by
    /// oen dont generate the same one for all"*, *"really lock in with animating the walk and run of each character"*, *"think
    /// of their personalities and shti and how it will show up in walking"*, *"look dude everyones arms are floating and not
    /// even attached right"*. This type is only the vocabulary a gait is written in; what a character's walk looks like is
    /// the entry for that character in `GaitStyles`, written from who they are, and nothing here decides it.
    ///
    /// The rig has seven rigid bones and no knee or elbow (`tools/glb_action.py`), so a gait is: how far each leg swings
    /// and how it lingers at the plant, how the arms hang and swing, how the chest leans, rolls and twists, what the head
    /// does about it, and how the hips carry the weight (bob, bounce, side sway). Every angle is degrees in the CHARACTER's
    /// frame; every distance is a fraction of that body's own leg reach, so it reads the same on Paete and on Nemu.
    /// </summary>
    public struct Gait
    {
        /// <summary>Peak leg angle forward of hanging, and behind it.</summary>
        public float LegForward, LegBack;
        /// <summary>
        /// The shape of the leg swing: 1 is a sine; under 1 the leg lingers at the plant (weight, a stomp); over 1 it hurries
        /// through the extremes and spends its time passing under the body (light, quick feet).
        /// </summary>
        public float LegSnap;
        /// <summary>Each leg out from vertical: positive is a wide stance, negative crosses toward the centre line (a strut).</summary>
        public float Stance;
        /// <summary>
        /// Each arm out from vertical. ⚠️ THE SHOULDER NEVER MOVES: the arm pivots where the model put it, so its top stays
        /// buried in the torso and it is attached. The previous pass slid every shoulder out up to 0.8 of an arm's length and
        /// the owner saw arms floating beside the body. Clearance of the hips is bought here, per body, or accepted.
        /// </summary>
        public float ArmSpread;
        /// <summary>Peak arm swing forward of the arm's carry angle, and behind it.</summary>
        public float ArmForward, ArmBack;
        /// <summary>Where the arm swings about: positive carries it in front (a runner's fists), negative sweeps it back.</summary>
        public float ArmCarry;
        /// <summary>Shape of the arm swing, as `LegSnap`.</summary>
        public float ArmSnap;
        /// <summary>
        /// How far the arms trail the legs, in CYCLES (0.1 is a tenth of two steps). Loose, heavy or sleeved arms trail more.
        /// ⚠️ Was seconds in the first draft: at these bodies' two cycles a second, Nemu's 0.22 s put her arms 0.4 of a cycle
        /// late, swinging with the same-side leg (a pace, not a walk). Kept at or under 0.12.
        /// </summary>
        public float ArmLag;
        /// <summary>-1 to 1: positive makes the right arm swing more than the left (a one-sided swagger).</summary>
        public float ArmFavour;
        /// <summary>Chest pitch into the travel. Negative leans back.</summary>
        public float Lean;
        /// <summary>Extra chest pitch at each footfall, the weight arriving.</summary>
        public float LeanPulse;
        /// <summary>Chest roll over the stance leg.</summary>
        public float Roll;
        /// <summary>How late the roll follows the stance, in cycles (0 locked to the feet; a deck walker rolls late).</summary>
        public float RollDelay;
        /// <summary>Shoulders turning against the hips.</summary>
        public float Twist;
        /// <summary>Head pitch held while moving: positive looks down, negative lifts the chin.</summary>
        public float HeadPitch;
        /// <summary>Head roll held while moving: a cocked head.</summary>
        public float HeadTilt;
        /// <summary>Head nod per step, down on the footfall.</summary>
        public float HeadNod;
        /// <summary>0 to 1: how much of the chest's lean, roll and twist the head takes back to keep the face level.</summary>
        public float HeadSteady;
        /// <summary>Extra rise of the hips at passing, fraction of leg reach (a spring in the step; a runner's flight).</summary>
        public float Bounce;
        /// <summary>Where in the step the rise peaks, in cycles after passing.</summary>
        public float BounceDelay;
        /// <summary>A hard drop at each footfall, fraction of leg reach (a stomp).</summary>
        public float Stomp;
        /// <summary>The hips shifting over the stance foot, fraction of leg reach.</summary>
        public float Sway;
        /// <summary>
        /// ⚠️⚠️ HOW MUCH FURTHER THE BODY TRAVELS THAN ITS FEET STEP, 1 AND UP. MEASURED 2026-09-27: these bodies are 0.87 to
        /// 1.4 m tall with 0.42 m legs (0.58 Sean, 0.62 Paete) and move at 2.3 to 2.8 m/s walking and 3.4 to 4.2 running, so a
        /// stride that never slides costs 4 to 8 steps a second, and the first per-character film was a blur of legs (Nemu at
        /// 7.8). A little slide reads far better on a toy-sized body than frantic feet, so each character trades some: the
        /// cadence is divided by this. It is also character: Nemu drifts (1.6), Sean plants (1.1).
        /// </summary>
        public float Glide;

        public static Gait Lerp(in Gait a, in Gait b, float t)
        {
            float L(float x, float y) => x + (y - x) * t;
            return new Gait
            {
                LegForward = L(a.LegForward, b.LegForward), LegBack = L(a.LegBack, b.LegBack), LegSnap = L(a.LegSnap, b.LegSnap),
                Stance = L(a.Stance, b.Stance), ArmSpread = L(a.ArmSpread, b.ArmSpread),
                ArmForward = L(a.ArmForward, b.ArmForward), ArmBack = L(a.ArmBack, b.ArmBack), ArmCarry = L(a.ArmCarry, b.ArmCarry),
                ArmSnap = L(a.ArmSnap, b.ArmSnap), ArmLag = L(a.ArmLag, b.ArmLag), ArmFavour = L(a.ArmFavour, b.ArmFavour),
                Lean = L(a.Lean, b.Lean), LeanPulse = L(a.LeanPulse, b.LeanPulse), Roll = L(a.Roll, b.Roll),
                RollDelay = L(a.RollDelay, b.RollDelay), Twist = L(a.Twist, b.Twist),
                HeadPitch = L(a.HeadPitch, b.HeadPitch), HeadTilt = L(a.HeadTilt, b.HeadTilt), HeadNod = L(a.HeadNod, b.HeadNod),
                HeadSteady = L(a.HeadSteady, b.HeadSteady), Bounce = L(a.Bounce, b.Bounce), BounceDelay = L(a.BounceDelay, b.BounceDelay),
                Stomp = L(a.Stomp, b.Stomp), Sway = L(a.Sway, b.Sway), Glide = L(a.Glide, b.Glide),
            };
        }
    }

    /// <summary>
    /// The drawn pose of one frame, before it is put on the bones. Left and right are the CHARACTER's own sides (found by
    /// where each bone sits, because the importer mirrors X and a bone's name says nothing about its side).
    /// </summary>
    public struct GaitPose
    {
        public float LegLeft, LegRight, SplayLeft, SplayRight;
        public float ArmLeft, ArmRight, SpreadLeft, SpreadRight;
        public float TorsoPitch, TorsoRoll, TorsoYaw;
        public float HeadPitch, HeadRoll, HeadYaw;
        /// <summary>Hip offsets, fractions of leg reach: up, and toward the character's right.</summary>
        public float RootUp, RootRight;
        /// <summary>+1 while the left leg is fully forward (read by probes and the first-person arms).</summary>
        public float Stride;
        /// <summary>+1 while the left leg bears the weight at mid-stance.</summary>
        public float Stance;
    }

    /// <summary>What a gait's personal touch may read: phase, run weight, the clock, and the gait after blending.</summary>
    public struct GaitMoment
    {
        public float Phase, Run, Time, CycleRate;
        public Gait Gait;
    }

    public delegate void GaitQuirk(ref GaitPose pose, in GaitMoment moment);

    /// <summary>One body's walk, run and personal touch. See `GaitStyles` for every entry and the reason behind each.</summary>
    public sealed class GaitStyle
    {
        public string Name;
        public Gait Walk, Run;
        /// <summary>What this character does that no curve above can say (a scan of the court, a drifting head). May be null.</summary>
        public GaitQuirk Quirk;

        /// <summary>The metres one full cycle (two steps) covers, for a leg of `reach` metres: the no-slide stride times `Glide`.</summary>
        public float CycleMetres(float reach, float run)
        {
            var g = Gait.Lerp(Walk, Run, run);
            float spread = (Mathf.Sin(g.LegForward * Mathf.Deg2Rad) + Mathf.Sin(g.LegBack * Mathf.Deg2Rad)) * Mathf.Cos(g.Stance * Mathf.Deg2Rad);
            return 2f * reach * Mathf.Max(.1f, spread) * Mathf.Max(1f, g.Glide);
        }

        /// <summary>
        /// The pose at `phase` (0 to 1 over two steps). Plumbing only: every number it multiplies is the character's own.
        /// </summary>
        public GaitPose Evaluate(float phase, float run, float time, float cycleRate)
        {
            var g = Gait.Lerp(Walk, Run, run);
            float tau = 2f * Mathf.PI;
            float s = Mathf.Sin(tau * phase);
            float legs = Shape(s, g.LegSnap);
            float leg = legs >= 0 ? legs * g.LegForward : legs * g.LegBack;
            // The arms trail the legs by `ArmLag` of a cycle.
            float armPhase = phase - g.ArmLag;
            float a = Shape(Mathf.Sin(tau * armPhase), g.ArmSnap);
            float Arm(float x) => (x >= 0 ? x * g.ArmForward : x * g.ArmBack);
            // The left leg bears weight while it travels back under the body: the falling half of the sine.
            float stance = -Mathf.Cos(tau * phase);
            float rollStance = -Mathf.Cos(tau * (phase - g.RollDelay));
            // +1 at passing (legs together), -1 at each footfall; twice a cycle.
            float passing = Mathf.Cos(2f * tau * phase);
            float footfall = Mathf.Max(0f, -passing);
            float rise = .5f + .5f * Mathf.Cos(2f * tau * (phase - g.BounceDelay));

            var p = new GaitPose
            {
                Stride = s, Stance = stance,
                LegLeft = leg, LegRight = -(legs >= 0 ? legs * g.LegBack : legs * g.LegForward),
                SplayLeft = g.Stance, SplayRight = g.Stance,
                // An arm goes forward with the OTHER side's leg.
                ArmLeft = g.ArmCarry + Arm(-a) * (1f - g.ArmFavour),
                ArmRight = g.ArmCarry + Arm(a) * (1f + g.ArmFavour),
                SpreadLeft = g.ArmSpread, SpreadRight = g.ArmSpread,
                TorsoPitch = g.Lean + g.LeanPulse * footfall,
                TorsoRoll = g.Roll * rollStance,
                // Right shoulder forward with the left leg: the chest turns toward the left.
                TorsoYaw = -a * g.Twist,
                RootUp = g.Bounce * rise - g.Stomp * footfall * footfall * footfall,
                RootRight = -g.Sway * stance,
            };
            p.HeadPitch = g.HeadPitch - p.TorsoPitch * g.HeadSteady + g.HeadNod * footfall;
            p.HeadRoll = g.HeadTilt - p.TorsoRoll * g.HeadSteady;
            p.HeadYaw = -p.TorsoYaw * g.HeadSteady;
            Quirk?.Invoke(ref p, new GaitMoment { Phase = phase, Run = run, Time = time, CycleRate = cycleRate, Gait = g });
            return p;
        }

        private static float Shape(float x, float power) =>
            Mathf.Approximately(power, 1f) || power <= 0f ? x : Mathf.Sign(x) * Mathf.Pow(Mathf.Abs(x), power);
    }
}
