using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    // AMIHAN'S LIGHT BODY (owner, 2026-10-02: light footsteps, a floating run that looks like flying, and how she jumps,
    // throws and moves with it). `docs/reports/amihan-presentation-2026-10-02/light-body.md` has the heights and every verb.
    public sealed partial class AmihanKitPlayProbe
    {
        /// <summary>
        /// She stands, walks, sprints, jumps out of the sprint, sprints on, stops to charge and throw, and sprints again,
        /// pressed through `InputIntent` as a player presses it, at a fixed 30 fps game clock. Asserts the float's heights,
        /// that a throw settles her onto her toes instead of dropping her in two frames, and that her steps are her own sound.
        /// With TUMP_AMIHAN_FILM=1 it also films a low side view (the gap under her shoes), the court and her own screen
        /// under TUMP_EVIDENCE/amihan-light-body, with lift.csv carrying every frame.
        /// </summary>
        [UnityTest, Timeout(600000)]
        public IEnumerator HerRunFloatsAndEveryVerbSettlesSoftly()
        {
            bool filming = Environment.GetEnvironmentVariable("TUMP_AMIHAN_FILM") == "1";
            var round = GameServices.Round;
            var can = Flat(round.Lata.transform.position);
            int seat = GameLaunch.SoloSeat;
            if (round.PlayerAt(seat).IsDefender) seat = (seat + 1) % 4;
            var start = can + new Vector3(-6.5f, .12f, -12.0f);
            var her = Amihan(seat, start);
            Face(her, start + Vector3.right * 10f);
            yield return null;
            var step = her.GetComponent<AmihanAirStep>();
            Assert.IsNotNull(step, "Her body has no AmihanAirStep: the gait style did not attach it.");
            var carrier = her.GetComponent<Carrier>();
            if (carrier.Held == null)
                foreach (var shoe in Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
                {
                    if (shoe == null || !shoe.IsGrabbableIgnoringReach(her)) continue;
                    // Put a loose slipper at her feet and pick it up through the host's own pickup.
                    shoe.ApplySnapshotPose(her.transform.position + her.transform.forward * .3f, Quaternion.identity, Vector3.zero);
                    carrier.HostPickUp(shoe);
                    if (carrier.Held != null) break;
                }
            yield return null;
            bool held = carrier.Held != null;
            Assert.IsTrue(held, "The fixture could not put a slipper in her hand.");

            var side = Film.Make("AmihanLightSide", 38);
            var wide = Film.Make("AmihanLightWide", 50);
            var lift = new StringBuilder().AppendLine("frame,seconds,lift_m,grounded,speed,run,clip,holding");
            int ownSteps = 0, rubberSteps = 0;
            Action<string, Vector3, float, float> heard = (id, at, pitch, gain) =>
            {
                if (Vector3.Distance(Flat(at), Flat(her.transform.position)) > 1.5f) return;
                if (id == "step_amihan") ownSteps++;
                if (id == "step_rubber") rubberSteps++;
            };
            AudioDirector.WorldCuePlayed += heard;
            float standMax = 0, walkMax = 0, runMin = float.MaxValue, runMax = 0, airMax = 0, everMax = 0;
            float chargeAt = -1, settledAt = -1, liftAtCharge = 0;
            bool jumped = false, threw = false;
            int previousRate = Time.captureFramerate;
            Film film = filming ? new Film("amihan-light-body", "side", "wide", "owner") : null;
            if (!filming) Time.captureFramerate = 30;
            try
            {
                const int frames = 30 * 10;
                for (int f = 0; f < frames; f++)
                {
                    if (film != null) film.Frame = f;
                    float t = f / 30f;
                    // Stand, walk, sprint and charge a throw WHILE floating (the float must settle, not drop), throw, rest
                    // while her stamina refills, then sprint and jump out of the sprint. Two short sprints: a full bar buys
                    // about 2.07 s (`Balance.StaminaMax` 60 at `StaminaDrainRate` 29), and an exhausted body trudges grounded.
                    bool walk = t > .6f && t < 1.8f, sprint = (t > 1.8f && t < 3.2f) || (t > 6.2f && t < 7.9f);
                    her.Intent.Move = walk ? new Vector2(0, .45f) : sprint ? Vector2.up : Vector2.zero;
                    her.Intent.Set(Verb.Sprint, sprint);
                    her.Intent.Set(Verb.Jump, t > 6.9f && t < 7.0f);
                    bool charging = held && t > 2.6f && t < 3.2f;
                    her.Intent.Set(Verb.SpecialAbility, charging);
                    if (charging) her.Intent.AimPoint = her.transform.position + her.transform.forward * 6f;
                    yield return null;

                    float h = step.Lift;
                    var v = her.Velocity; float speed = new Vector2(v.x, v.z).magnitude;
                    var animator = her.GetComponentInChildren<CharacterAnimator>();
                    lift.AppendLine(FormattableString.Invariant(
                        $"{f},{t:F3},{h:F4},{her.IsGrounded},{speed:F2},{animator.GaitRunWeight:F2},{animator.CurrentClipName},{carrier.Held != null}"));
                    everMax = Mathf.Max(everMax, h);
                    if (t < .5f) standMax = Mathf.Max(standMax, h);
                    if (t > 1.2f && t < 1.75f) walkMax = Mathf.Max(walkMax, h);
                    if (((t > 2.25f && t < 2.58f) || (t > 6.6f && t < 6.88f)) && her.IsGrounded) { runMin = Mathf.Min(runMin, h); runMax = Mathf.Max(runMax, h); }
                    if (!her.IsGrounded) { jumped = true; airMax = Mathf.Max(airMax, h); }
                    if (charging && chargeAt < 0) { chargeAt = t; liftAtCharge = h; }
                    if (chargeAt >= 0 && settledAt < 0 && h < .02f) settledAt = t;
                    threw |= held && t > 3.15f && carrier.Held == null;

                    if (film == null) continue;
                    var at = her.transform.position;
                    var right = Vector3.Cross(Vector3.up, her.transform.forward);
                    side.transform.position = at + right * 4.6f + Vector3.up * .55f;
                    side.transform.LookAt(at + Vector3.up * .55f);
                    film.Shoot(side, "side");
                    wide.transform.position = start + new Vector3(5f, 7.5f, -7.5f);
                    wide.transform.LookAt(at + Vector3.up * .5f);
                    film.Shoot(wide, "wide");
                    film.Shoot(Camera.main, "owner");
                }
            }
            finally
            {
                AudioDirector.WorldCuePlayed -= heard;
                her.Intent.Clear();
                film?.Dispose();
                if (!filming) Time.captureFramerate = previousRate;
                Object.Destroy(side.gameObject); Object.Destroy(wide.gameObject);
                Directory.CreateDirectory(film != null ? film.Root : "Logs");
                File.WriteAllText(Path.Combine(film != null ? film.Root : "Logs", "lift.csv"), lift.ToString());
            }
            string summary = FormattableString.Invariant(
                $"stand max {standMax:F3}; walk max {walkMax:F3}; run {runMin:F3} to {runMax:F3}; ever {everMax:F3}; air max {airMax:F3}; jumped {jumped}; charge at {chargeAt:F2} from {liftAtCharge:F3}, settled at {settledAt:F2}; threw {threw}; steps own {ownSteps} rubber {rubberSteps}");
            Debug.Log("[AmihanLightBody] " + summary);
            Note("amihan_light_body", summary);
            Assert.Less(standMax, .005f, "Standing still she keeps her feet on the court.");
            Assert.LessOrEqual(walkMax, AmihanAirStep.WalkLift + .005f, "Walking she barely leaves the court.");
            Assert.GreaterOrEqual(runMin, AmihanAirStep.RunLow - .02f, "Running she floats.");
            Assert.LessOrEqual(everMax, AmihanAirStep.RunHigh + .005f, "The float never reaches jump or Featherfall heights.");
            Assert.IsTrue(jumped, "The jump never left the court.");
            Assert.Greater(chargeAt, 0, "She never charged the throw.");
            Assert.GreaterOrEqual(liftAtCharge, AmihanAirStep.RunLow - .02f, "The charge began while she was still floating.");
            Assert.Greater(settledAt - chargeAt, .15f, "Charging a throw dropped her in a couple of frames instead of settling onto her toes.");
            Assert.Greater(ownSteps, 1, "Her own light step never played.");
            Assert.AreEqual(0, rubberSteps, "Her body still played the shared rubber step.");
        }
    }
}
