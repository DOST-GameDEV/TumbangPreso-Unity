using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Map;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// THE ARENA'S SLIPPER BALLOON AND A SLIPPER THAT LEAVES THE STAGE, IN A LIVE ROUND
    /// (`Map.ArenaBalloon`, `Map.ArenaFallRecovery`). One batch run enters Play on the Arena scene
    /// and gives a pass or a fail, a report (Logs/arena/unity/balloon_probe.txt) and pictures from
    /// the game's own camera (Logs/arena/unity/balloon_*.png).
    ///   * THE RULE, asked directly (`ArenaBalloon.IsAimedAtBalloon`) from a grid of places on the
    ///     stage: aimed at the middle of the balloon at full charge it is always a hit; aimed at
    ///     the can, at every spawn mark, level and 45 degrees up on sixteen bearings and straight
    ///     up, it never is; nor at the balloon under the charge, or outside the cone.
    ///   * REAL THROWS through `Carrier.HostThrowAt`, the host's own entry: a full-charge throw at
    ///     the can from each attackers' mark, and level, lobbed and straight up, flies as a throw
    ///     and leaves the balloon alone; a full-charge throw at the balloon takes the slipper out
    ///     of play, counts one hit, and sets the slipper back LOOSE, ON FLOOR, near where its line
    ///     left the play area, within the promised time; a bot's throw on the same line does not.
    ///   * THE FIFTH HIT POPS IT (the body is gone, a sixth throw is an ordinary throw), and it
    ///     comes back (at the next round's start, or after `ReinflateSeconds`).
    ///   * A SLIPPER THROWN OFF EACH SIDE OF THE STAGE comes back loose, on floor, at the nearest
    ///     standable point to where it left, within a bounded time.
    ///   * A BOT whose slipper went off the stage has it in its hand again within a bounded time.
    /// </summary>
    [Category("WallClock")]
    public sealed class ArenaBalloonProbe
    {
        private const string Scene = "Assets/TumbangPreso/Scenes/Maps/Arena.unity";
        private const string Folder = "Logs/arena/unity";
        private const int Width = 1600, Height = 900;
        /// <summary>How long a bot may take to be holding a slipper that went off the stage: the
        /// return, the walk across the largest layout, and a taya in the way. The probe's own line.</summary>
        private const float BotRearmSeconds = 40.0f;

        private static readonly RaycastHit[] Hits = new RaycastHit[24];

        private readonly List<(CharacterMotor who, bool wasBot, AIController brain, bool brainOn)> _borrowed = new List<(CharacterMotor, bool, AIController, bool)>();

        [UnityTest, Timeout(900000)]
        public IEnumerator TheBalloonIsHitPoppedAndComesBackAndASlipperOffTheStageReturns()
        {
#if UNITY_EDITOR
            LogAssert.ignoreFailingMessages = true;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            Time.timeScale = 1f;
            float waited = 0f;
            while (waited < 90f && (Camera.main == null || ArenaStage.Instance == null || GameServices.Round == null || !GameServices.Round.RoundActive || PresentationClock.Held))
            { waited += Time.unscaledDeltaTime; Time.timeScale = PresentationClock.Held ? Time.timeScale : 1f; yield return null; }

            var stage = ArenaStage.Instance;
            var cam = Camera.main;
            var balloon = ArenaBalloon.Instance;
            var recovery = ArenaFallRecovery.Instance;
            Assert.IsNotNull(stage, "No ArenaStage in Play on " + Scene);
            Assert.IsNotNull(cam, "No main camera in Play on " + Scene);
            Assert.IsNotNull(balloon, "No ArenaBalloon in the scene: rebuild it (Tumbang Preso/Maps/Arena) after exporting the holo kit.");
            Assert.IsTrue(GameServices.Round != null && GameServices.Round.RoundActive, "No live round within 90 s of loading the Arena.");
            Directory.CreateDirectory(Folder);

            var report = new StringBuilder();
            var failures = new List<string>();
            void Fail(string line) { failures.Add(line); report.AppendLine("  FAIL " + line); }
            void Pass(string line) => report.AppendLine("  ok   " + line);
            report.AppendLine($"ARENA BALLOON PROBE, {Scene}");
            report.AppendLine($"balloon centre {balloon.Centre:F1}; the rule: charge >= {ArenaBalloon.MinCharge}, within {ArenaBalloon.ConeDegrees} degrees, at least {ArenaBalloon.MinElevationDegrees} up, " +
                              $"aimed past the play area; pops on hit {ArenaBalloon.PopHits}. Walls x {AIController.PlayableMinX:F1}..{AIController.PlayableMaxX:F1}, " +
                              $"z {AIController.PlayableMinZ:F1}..{AIController.PlayableMaxZ:F1}, ceiling {AIController.PlayableCeilingY:F1}. Layout {stage.Applied}.");
            if (recovery == null) Fail("no ArenaFallRecovery instance on the host");

            var marks = new List<Vector3>();
            var spawnRoot = GameObject.Find("SpawnPoints");
            if (spawnRoot != null) foreach (Transform mark in spawnRoot.transform) marks.Add(mark.position);

            // ------------------------------------------------------------ 1. the rule, asked directly
            report.AppendLine();
            report.AppendLine("1. THE RULE, from a grid of places on the stage (eye 1.5 m over the deck)");
            int asked = 0, wrong = 0;
            float leastElevation = 90f, mostElevation = 0f;
            for (float x = -20f; x <= 20f; x += 5f)
                for (float z = -20f; z <= 20f; z += 5f)
                {
                    if (new Vector2(x, z).magnitude > stage.Radius) continue;
                    var eye = new Vector3(x, stage.transform.position.y + 1.5f, z);
                    Vector3 toBalloon = (balloon.Centre - eye).normalized;
                    float elevation = Mathf.Asin(toBalloon.y) * Mathf.Rad2Deg;
                    leastElevation = Mathf.Min(leastElevation, elevation); mostElevation = Mathf.Max(mostElevation, elevation);

                    void Ask(Vector3 aim, float charge, bool expect, string what)
                    {
                        asked++;
                        if (ArenaBalloon.IsAimedAtBalloon(balloon.Centre, eye, aim, charge) == expect) return;
                        wrong++;
                        if (wrong <= 12) Fail($"from ({x:F0}, {z:F0}) {what}: expected {(expect ? "a hit" : "no hit")}");
                    }

                    Ask(eye + toBalloon * 40f, 1.0f, true, "at the balloon's middle, full charge");
                    Ask(eye + toBalloon * 40f, ArenaBalloon.MinCharge, true, "at the balloon's middle, least charge");
                    Ask(eye + toBalloon * 40f, ArenaBalloon.MinCharge - 0.05f, false, "at the balloon's middle, under the charge");
                    Ask(eye + toBalloon * 40f, 0.0f, false, "at the balloon's middle, a tap");
                    Vector3 across = Vector3.Cross(Vector3.up, toBalloon).normalized;
                    Ask(eye + (Quaternion.AngleAxis(ArenaBalloon.ConeDegrees - 1.5f, Vector3.up) * toBalloon) * 40f, 1.0f, true, "just inside the cone, to the side");
                    Ask(eye + (Quaternion.AngleAxis(ArenaBalloon.ConeDegrees + 2.5f, across) * toBalloon) * 40f, 1.0f, false, "outside the cone, below or above");
                    Ask(eye + (Quaternion.AngleAxis(-(ArenaBalloon.ConeDegrees + 2.5f), across) * toBalloon) * 40f, 1.0f, false, "outside the cone, the other way");
                    Ask(eye + (Quaternion.AngleAxis(ArenaBalloon.ConeDegrees + 6f, Vector3.up) * toBalloon) * 40f, 1.0f, false, "outside the cone, to the side");
                    // A point INSIDE the play area on the very line to the balloon (a body or a platform in the way).
                    Ask(eye + toBalloon * Mathf.Max(0.6f, ArenaBalloon.PlayExit(eye, toBalloon) - 3.0f), 1.0f, ArenaBalloon.PlayExit(eye, toBalloon) < 3.6f, "at a point in play on the balloon's line");
                    Ask(new Vector3(0f, stage.transform.position.y + stage.CanHeight + 0.3f, 0f), 1.0f, false, "at the can");
                    foreach (var mark in marks) Ask(mark + Vector3.up * 1.2f, 1.0f, false, "at a body on a spawn mark");
                    for (int b = 0; b < 16; b++)
                    {
                        Vector3 level = ArenaStageMesh.Direction(22.5f * b);
                        Ask(eye + level * 40f, 1.0f, false, $"level, bearing {22.5f * b:F0}");
                        Ask(eye + (level + Vector3.up).normalized * 40f, 1.0f, Vector3.Angle((level + Vector3.up).normalized, toBalloon) <= ArenaBalloon.ConeDegrees, $"45 degrees up, bearing {22.5f * b:F0}");
                        Ask(eye + level * 12f, 1.0f, false, $"at the deck 12 m off, bearing {22.5f * b:F0}");
                    }
                    Ask(eye + Vector3.up * 40f, 1.0f, false, "straight up");
                }
            if (wrong == 0) Pass($"{asked} questions answered as expected; the balloon stands {leastElevation:F1} to {mostElevation:F1} degrees up from the stage");
            else Fail($"{wrong} of {asked} answers were wrong");

            // ------------------------------------------------------------ 2. real throws that must NOT reach it
            report.AppendLine();
            report.AppendLine("2. ORDINARY FULL-CHARGE THROWS (Carrier.HostThrowAt)");
            var savedPos = cam.transform.position; var savedRot = cam.transform.rotation; float savedFov = cam.fieldOfView;
            Vector3 can = new Vector3(0f, stage.transform.position.y + stage.CanHeight + 0.3f, 0f);
            var ordinary = new List<(string what, Vector3 from, Vector3 aim)>();
            for (int m = 1; m < marks.Count; m++) ordinary.Add(($"at the can from attackers' mark {m}", marks[m] + Vector3.up * 1.4f, can));
            Vector3 centreEye = new Vector3(0f, stage.transform.position.y + stage.CanHeight + 1.5f, 9f);
            Vector3 south = (new Vector3(balloon.Centre.x, 0f, balloon.Centre.z)).normalized;
            ordinary.Add(("level toward the balloon's bearing", centreEye, centreEye + south * 40f));
            ordinary.Add(("a lob (45 degrees) to the north", centreEye, centreEye + (Vector3.forward + Vector3.up).normalized * 40f));
            ordinary.Add(("a lob (45 degrees) to the east", centreEye, centreEye + (Vector3.right + Vector3.up).normalized * 40f));
            ordinary.Add(("straight up", centreEye, centreEye + Vector3.up * 40f));
            ordinary.Add(("steeply up, away from the balloon", centreEye, centreEye + (-south + Vector3.up * 0.75f).normalized * 40f));
            foreach (var (what, from, aim) in ordinary)
            {
                CharacterMotor thrower = null; Slipper shoe = null;
                yield return Arm(r => thrower = r.who, r => shoe = r.shoe, false);
                if (thrower == null || shoe == null) { Fail($"{what}: no attacker could be armed"); continue; }
                int before = balloon.Hits;
                thrower.GetComponent<Carrier>().HostThrowAt(from, aim, 1.0f);
                yield return new WaitForFixedUpdate();
                if (balloon.Hits != before || !shoe.gameObject.activeSelf) Fail($"{what}: the balloon took it (hits {before} to {balloon.Hits}, slipper in play {shoe.gameObject.activeSelf})");
                else Pass($"{what}: an ordinary throw (slipper {shoe.State}, hits still {balloon.Hits})");
                yield return Seconds(1.2f);
            }

            // A BOT on the balloon's own line is refused by its seat.
            {
                CharacterMotor thrower = null; Slipper shoe = null;
                yield return Arm(r => thrower = r.who, r => shoe = r.shoe, true);
                if (thrower == null || shoe == null) report.AppendLine("  (no bot attacker to try: skipped)");
                else
                {
                    Vector3 from = thrower.transform.position + Vector3.up * 1.4f;
                    int before = balloon.Hits;
                    thrower.GetComponent<Carrier>().HostThrowAt(from, from + (balloon.Centre - from).normalized * 40f, 1.0f);
                    yield return new WaitForFixedUpdate();
                    if (balloon.Hits != before) Fail("a BOT's full-charge throw on the balloon's line was counted");
                    else Pass("a bot's full-charge throw on the balloon's line is an ordinary throw");
                    yield return Seconds(1.0f);
                }
            }

            // ------------------------------------------------------------ 3. the hits, the pop
            report.AppendLine();
            report.AppendLine("3. THROWS AT THE BALLOON");
            Shot(cam, new Vector3(0f, 3.3f, 6f), balloon.Centre, 22f, $"{Folder}/balloon_idle.png");
            Shot(cam, new Vector3(0f, 4.9f, 14.5f), new Vector3(0f, 26f, -60f), 60f, $"{Folder}/balloon_idle_wide.png");
            for (int n = balloon.Hits + 1; n <= ArenaBalloon.PopHits; n++)
            {
                CharacterMotor thrower = null; Slipper shoe = null;
                yield return Arm(r => thrower = r.who, r => shoe = r.shoe, false);
                if (thrower == null || shoe == null) { Fail($"hit {n}: no attacker could be armed"); break; }

                Vector3 from = thrower.transform.position + Vector3.up * 1.5f;
                Vector3 sight = (balloon.Centre - from).normalized;
                Vector3 left = from + sight * ArenaBalloon.PlayExit(from, sight);
                bool hasNear = stage.TryNearestStandable(left, out var expected);
                float thrownAt = Time.time;
                thrower.GetComponent<Carrier>().HostThrowAt(from, from + sight * 40f, 1.0f);
                yield return null;
                if (balloon.Hits != n) { Fail($"hit {n}: not counted (hits {balloon.Hits}); from {from:F1}, sight {sight:F2}"); break; }
                if (shoe.gameObject.activeSelf) Fail($"hit {n}: the slipper is still in play after the throw (state {shoe.State})");
                else Pass($"hit {n}: taken out of play at the throw, from seat {thrower.PlayerSlot} at ({from.x:F1}, {from.z:F1}); line leaves the play area at {left:F1}");

                // Half way up: the drawn flight. Then the strike.
                yield return Seconds(ArenaBalloon.FlightSeconds * 0.5f);
                if (n == 1) Shot(cam, from - sight * 2.0f + Vector3.up * 0.4f, balloon.Centre, 40f, $"{Folder}/balloon_flight.png");
                yield return Seconds(ArenaBalloon.FlightSeconds * 0.5f + 0.18f);
                Shot(cam, new Vector3(0f, 3.3f, 6f), balloon.Centre, 22f, $"{Folder}/balloon_hit_{n}.png");
                if (n == 1 || n == ArenaBalloon.PopHits - 1) Shot(cam, new Vector3(0f, 4.9f, 14.5f), new Vector3(0f, 26f, -60f), 60f, $"{Folder}/balloon_hit_{n}_wide.png");
                if (n < ArenaBalloon.PopHits && balloon.ShownHits != n) Fail($"hit {n}: the balloon is showing {balloon.ShownHits} hits after the strike");

                // Back on the stage.
                float limit = ArenaBalloon.FlightSeconds + ArenaBalloon.ReturnSeconds + 1.0f;
                while (Time.time - thrownAt < limit && !shoe.gameObject.activeSelf) yield return null;
                float took = Time.time - thrownAt;
                yield return new WaitForFixedUpdate();
                yield return null;
                if (!shoe.gameObject.activeSelf) Fail($"hit {n}: the slipper was not back within {limit:F1} s");
                else if (shoe.State == SlipperState.Held) Pass($"hit {n}: back after {took:F2} s and already picked up again");
                else
                {
                    Vector3 at = shoe.transform.position;
                    bool floor = Ground(at + Vector3.up * 0.5f, 1.5f, out var hit);
                    float off = hasNear ? Flat(at, expected) : -1f;
                    string line = $"hit {n}: back after {took:F2} s, {shoe.State}, at ({at.x:F2}, {at.y:F2}, {at.z:F2}), {(floor ? $"floor {at.y - hit.point.y:F2} m under it" : "NO FLOOR UNDER IT")}, {off:F2} m from the nearest standable point to where it left";
                    // `Slipper.Land` may hand a slipper resting out of its owner's reach to the owner: then it is at their feet.
                    bool atOwner = Flat(at, thrower.transform.position) < 0.3f;
                    if (shoe.State != SlipperState.Loose || !floor || (!atOwner && hasNear && off > 0.6f)) Fail(line); else Pass(line + (atOwner ? " (at its owner's feet)" : ""));
                    if (n == 1) Shot(cam, at + new Vector3(2.5f, 2.2f, 2.5f), at, 55f, $"{Folder}/balloon_slipper_back.png");
                }
            }

            if (balloon.Hits >= ArenaBalloon.PopHits)
            {
                yield return Seconds(0.9f);
                Shot(cam, new Vector3(0f, 3.3f, 6f), balloon.Centre, 22f, $"{Folder}/balloon_pop.png");
                Shot(cam, new Vector3(0f, 4.9f, 14.5f), new Vector3(0f, 26f, -60f), 60f, $"{Folder}/balloon_pop_wide.png");
                yield return Seconds(1.6f);
                if (!balloon.Popped || balloon.BodyShown) Fail($"after hit {ArenaBalloon.PopHits}: popped {balloon.Popped}, body still drawn {balloon.BodyShown}");
                else Pass($"hit {ArenaBalloon.PopHits} popped it: the body is gone");
                Shot(cam, new Vector3(0f, 3.3f, 6f), balloon.Centre, 22f, $"{Folder}/balloon_popped.png");

                CharacterMotor thrower = null; Slipper shoe = null;
                yield return Arm(r => thrower = r.who, r => shoe = r.shoe, false);
                if (thrower != null && shoe != null)
                {
                    Vector3 from = thrower.transform.position + Vector3.up * 1.5f;
                    thrower.GetComponent<Carrier>().HostThrowAt(from, from + (balloon.Centre - from).normalized * 40f, 1.0f);
                    yield return new WaitForFixedUpdate();
                    if (!shoe.gameObject.activeSelf || balloon.Hits > ArenaBalloon.PopHits) Fail("a throw at the popped balloon was taken");
                    else Pass("a throw at the popped balloon is an ordinary throw");
                }

                // It comes back at the next round's start or after ReinflateSeconds, whichever is first.
                float poppedAt = Time.unscaledTime;
                int round = GameServices.Match != null ? GameServices.Match.RoundNumber : 0;
                while (Time.unscaledTime - poppedAt < ArenaBalloon.ReinflateSeconds + 30f && balloon.Popped) yield return null;
                if (balloon.Popped) Fail($"still popped {Time.unscaledTime - poppedAt:F0} s later");
                else
                {
                    int now = GameServices.Match != null ? GameServices.Match.RoundNumber : 0;
                    yield return Seconds(1.2f);
                    Shot(cam, new Vector3(0f, 3.3f, 6f), balloon.Centre, 22f, $"{Folder}/balloon_reinflating.png");
                    yield return Seconds(2.2f);
                    if (!balloon.BodyShown || balloon.Hits != 0) Fail($"re-inflated but body drawn {balloon.BodyShown}, hits {balloon.Hits}");
                    else Pass($"re-inflated {Time.unscaledTime - poppedAt:F0} s after the pop ({(now != round ? $"round {round} to {now}" : "by its timer, in the same round")}), hits back to 0");
                    Shot(cam, new Vector3(0f, 3.3f, 6f), balloon.Centre, 22f, $"{Folder}/balloon_reinflated.png");
                }
            }

            // ------------------------------------------------------------ 4. a slipper thrown off each side
            report.AppendLine();
            report.AppendLine("4. A SLIPPER THROWN OFF THE STAGE");
            foreach (float bearing in new[] { 45f, 135f, 225f, 315f, 0f, 90f, 180f, 270f })
            {
                CharacterMotor thrower = null; Slipper shoe = null;
                yield return Arm(r => thrower = r.who, r => shoe = r.shoe, false);
                if (thrower == null || shoe == null) { Fail($"bearing {bearing:F0}: no attacker could be armed"); continue; }
                Vector3 outward = ArenaStageMesh.Direction(bearing);
                Vector3 from = stage.transform.position + outward * 12.0f + Vector3.up * 2.0f;
                float thrownAt = Time.time;
                shoe.HostThrow(thrower, from, outward * 15.0f + Vector3.up * 3.0f);
                Vector3 last = from; bool left = false; float gone = -1f;
                while (Time.time - thrownAt < 8.0f)
                {
                    if (shoe.gameObject.activeSelf && !left) last = shoe.transform.position;
                    if (!shoe.gameObject.activeSelf && !left) { left = true; gone = Time.time; }
                    if (left && shoe.gameObject.activeSelf) break;
                    if (!left && shoe.State != SlipperState.InFlight && Time.time - thrownAt > 0.3f && !ArenaFallRecovery.OverTheVoid(shoe.transform.position)) break;
                    yield return null;
                }
                yield return new WaitForFixedUpdate();
                yield return null;
                Vector3 at = shoe.transform.position;
                if (!left)
                {
                    // It stayed on the stage (this layout has floor that far out on this bearing): nothing to return.
                    if (shoe.gameObject.activeSelf && !ArenaFallRecovery.OverTheVoid(at)) Pass($"bearing {bearing:F0}: landed on the stage at ({at.x:F1}, {at.z:F1}), radius {new Vector2(at.x, at.z).magnitude:F1}: stays where it is");
                    else Fail($"bearing {bearing:F0}: resting over nothing at ({at.x:F2}, {at.y:F2}, {at.z:F2}) and never taken");
                    continue;
                }
                if (!shoe.gameObject.activeSelf) { Fail($"bearing {bearing:F0}: left the stage at ({last.x:F1}, {last.z:F1}) and was not back {Time.time - gone:F1} s later"); continue; }
                bool floor = Ground(at + Vector3.up * 0.5f, 1.5f, out var hit);
                bool hasNear = stage.TryNearestStandable(last, out var expected);
                float off = hasNear ? Flat(at, expected) : -1f;
                bool atOwner = Flat(at, thrower.transform.position) < 0.3f;
                string line = $"bearing {bearing:F0}: left at ({last.x:F1}, {last.y:F1}, {last.z:F1}), back {Time.time - gone:F2} s later at ({at.x:F2}, {at.y:F2}, {at.z:F2}), {shoe.State}, " +
                              $"{(floor ? $"floor {at.y - hit.point.y:F2} m under it" : "NO FLOOR UNDER IT")}, {off:F2} m from the nearest standable point";
                if (shoe.State == SlipperState.Held) Pass(line + " (already picked up)");
                else if (shoe.State != SlipperState.Loose || !floor || Time.time - gone > ArenaFallRecovery.EdgeReturnSeconds + 0.6f || (!atOwner && hasNear && off > 1.6f)) Fail(line);
                else Pass(line + (atOwner ? " (at its owner's feet)" : ""));
                if (bearing == 45f) Shot(cam, at + new Vector3(2.5f, 2.2f, 2.5f), at, 55f, $"{Folder}/balloon_slipper_edge_return.png");
            }

            // ------------------------------------------------------------ 5. a bot gets its slipper back
            report.AppendLine();
            report.AppendLine("5. A BOT WHOSE SLIPPER WENT OFF THE STAGE");
            Restore();
            {
                CharacterMotor bot = null; Slipper shoe = null;
                yield return Arm(r => bot = r.who, r => shoe = r.shoe, true);
                if (bot == null || shoe == null) report.AppendLine("  (no bot attacker in this round: skipped)");
                else
                {
                    Vector3 outward = ArenaStageMesh.Direction(135f);
                    float thrownAt = Time.time;
                    int round = GameServices.Match != null ? GameServices.Match.RoundNumber : 0;
                    shoe.HostThrow(bot, stage.transform.position + outward * 14.0f + Vector3.up * 2.0f, outward * 15.0f + Vector3.up * 3.0f);
                    var carrier = bot.GetComponent<Carrier>();
                    bool left = false; float back = -1f;
                    while (Time.time - thrownAt < BotRearmSeconds && carrier.Held != shoe)
                    {
                        if (!shoe.gameObject.activeSelf) left = true;
                        if (left && shoe.gameObject.activeSelf && back < 0f) back = Time.time - thrownAt;
                        if (GameServices.Match != null && GameServices.Match.RoundNumber != round) break;
                        yield return null;
                    }
                    if (GameServices.Match != null && GameServices.Match.RoundNumber != round) report.AppendLine("  (the round ended while the bot was fetching: not judged)");
                    else if (carrier.Held == shoe) Pass($"seat {bot.PlayerSlot} is holding it again {Time.time - thrownAt:F1} s after it went off (left the stage {left}, back on it at {back:F1} s)");
                    else Fail($"seat {bot.PlayerSlot} is not holding its slipper {BotRearmSeconds:F0} s after it went off (left {left}, back at {back:F1} s, slipper {shoe.State} at {shoe.transform.position:F1}, bot at {bot.transform.position:F1})");
                }
            }

            Restore();
            cam.transform.SetPositionAndRotation(savedPos, savedRot); cam.fieldOfView = savedFov;
            report.Insert(0, (failures.Count == 0 ? "PASS" : $"FAIL ({failures.Count})") + Environment.NewLine);
            File.WriteAllText(Folder + "/balloon_probe.txt", report.ToString());
            Debug.Log(report.ToString());
            Assert.IsEmpty(failures, "The Arena balloon probe failed:\n" + string.Join("\n", failures) + "\n\n" + report);
#else
            Assert.Ignore("Editor only: loads the scene by path.");
            yield break;
#endif
        }

        /// <summary>
        /// Waits for a live round, picks an attacker (a bot left as a bot if `asBot`, else a seat
        /// that plays as a person: a bot is borrowed, its brain switched off, until `Restore`),
        /// and puts its own slipper in its hand through the host's own equip.
        /// </summary>
        private IEnumerator Arm(Action<(CharacterMotor who, Slipper shoe)> who, Action<(CharacterMotor who, Slipper shoe)> shoe, bool asBot)
        {
            float waited = 0f;
            while (waited < 60f)
            {
                var round = GameServices.Round;
                if (round != null && round.RoundActive && !PresentationClock.Held)
                {
                    CharacterMotor pick = null;
                    foreach (var body in round.Players)
                    {
                        if (body == null || !body.gameObject.activeInHierarchy || body.IsDefender || body.IsEdgeRecovering || !body.CanAct()) continue;
                        bool borrowed = _borrowed.Exists(b => b.who == body);
                        if (asBot) { if (body.IsBot && !borrowed) { pick = body; break; } continue; }
                        // A seat that already plays as a person first; else the first bot, to borrow.
                        if (!body.IsBot) { pick = body; break; }
                        if (pick == null) pick = body;
                    }

                    Slipper mine = null;
                    if (pick != null)
                        foreach (var s in Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                            if (s.OwnerSlot == pick.PlayerSlot) { mine = s; break; }

                    if (pick != null && mine != null && mine.gameObject.activeSelf && mine.State != SlipperState.InFlight)
                    {
                        if (!asBot && pick.IsBot)
                        {
                            var brain = pick.GetComponent<AIController>();
                            _borrowed.Add((pick, true, brain, brain != null && brain.enabled));
                            if (brain != null) brain.enabled = false;
                            pick.IsBot = false;
                        }
                        var carrier = pick.GetComponent<Carrier>();
                        if (carrier.Held != mine) mine.HostForceEquip(pick);
                        yield return new WaitForFixedUpdate();
                        if (carrier.Held == mine) { who((pick, mine)); shoe((pick, mine)); yield break; }
                    }
                }
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        /// <summary>Every borrowed bot is a bot again, its brain back on.</summary>
        private void Restore()
        {
            foreach (var (who, wasBot, brain, brainOn) in _borrowed)
            {
                if (who == null) continue;
                who.IsBot = wasBot;
                if (brain != null) brain.enabled = brainOn;
            }
            _borrowed.Clear();
        }

        private static IEnumerator Seconds(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) yield return null;
        }

        private static float Flat(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        /// <summary>The highest solid thing under a point within `depth`, bodies, slippers and the can aside.</summary>
        private static bool Ground(Vector3 from, float depth, out RaycastHit best)
        {
            best = default;
            bool found = false;
            int count = Physics.RaycastNonAlloc(from, Vector3.down, Hits, depth, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var c = Hits[i].collider;
                if (c.GetComponentInParent<CharacterMotor>() != null || c.GetComponentInParent<Slipper>() != null || c.GetComponentInParent<Lata>() != null) continue;
                if (found && Hits[i].point.y <= best.point.y) continue;
                best = Hits[i];
                found = true;
            }
            return found;
        }

        /// <summary>One picture from the game's own camera, its whole effect chain included.</summary>
        private static void Shot(Camera cam, Vector3 at, Vector3 look, float fov, string path)
        {
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.Create();
            cam.transform.SetPositionAndRotation(at, Quaternion.LookRotation(look - at));
            cam.fieldOfView = fov;
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); image.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.Destroy(image);
            rt.Release(); Object.Destroy(rt);
        }
    }
}
