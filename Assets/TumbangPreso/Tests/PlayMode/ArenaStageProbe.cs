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
    /// THE ARENA'S STAGE, WALKED AND FALLEN OFF IN REAL PLAY (docs/ARENA_MAP_BRIEF.md, ARENA-1.1).
    /// One batch run enters Play on the Arena scene and gives a pass or a fail, a report
    /// (Logs/arena/unity/stage_probe.txt) and pictures from the game's own camera
    /// (Logs/arena/unity/*.png). For EACH layout, held through `ArenaStage.HoldLayout`:
    ///   * there is floor under the can at the layout's `CanHeight`, and under every spawn mark
    ///     within `MatchHost.SeatOnFloor`'s reach (from 2 m above the mark down 6 m);
    ///   * every platform that is not a bonus one can be WALKED to from the taya's mark: a flood
    ///     fill over a 0.5 m grid of ground raycasts against the layout's own colliders, moving
    ///     between neighbouring cells only where the CharacterController could (a rise of at most
    ///     its 0.3 m step, on ground no steeper than its 45 degrees), inside the play walls.
    ///     ⚠️ Walking only: a drop a body could survive is not counted, because the way back up
    ///     would still have to exist. A bonus platform is reported and not required;
    ///   * every pad and pickup stands on floor;
    ///   * pictures: the whole stage from above and from the taya's eye.
    /// Then the transformation between the first two layouts, posed at three moments (hologram
    /// only, half way, nearly there). Then THE FALL: a body is put over the open shaft; the host
    /// must catch it above y -5 (where `MatchRpc.AcceptMove` would start refusing its owner's
    /// poses), the drone must set it down on the stage, and it must then hold the tag's freeze.
    /// </summary>
    [Category("WallClock")]
    public sealed class ArenaStageProbe
    {
        private const string Scene = "Assets/TumbangPreso/Scenes/Maps/Arena.unity";
        private const string Folder = "Logs/arena/unity";
        private const int Width = 1600, Height = 900;
        private const float Cell = 0.5f, Step = 0.3f, Slope = 45.0f, WallMargin = 0.4f;
        private const float CastFrom = 14.0f, CastDepth = 24.0f;

        private static readonly RaycastHit[] Hits = new RaycastHit[24];

        [UnityTest, Timeout(600000)]
        public IEnumerator EveryLayoutIsWalkableAndAFallIsCarriedBack()
        {
#if UNITY_EDITOR
            LogAssert.ignoreFailingMessages = true;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            Time.timeScale = 1f;
            float waited = 0f;
            while (waited < 40f && (Camera.main == null || ArenaStage.Instance == null || Object.FindObjectsByType<CharacterMotor>().Length < 2)) { waited += Time.unscaledDeltaTime; yield return null; }
            for (float t = 0f; t < 6f; t += Time.unscaledDeltaTime) { Time.timeScale = 1f; yield return null; }

            var stage = ArenaStage.Instance;
            var cam = Camera.main;
            Assert.IsNotNull(stage, "No ArenaStage in Play on " + Scene);
            Assert.IsNotNull(cam, "No main camera in Play on " + Scene);
            Directory.CreateDirectory(Folder);

            var report = new StringBuilder();
            var failures = new List<string>();
            void Fail(string line) { failures.Add(line); report.AppendLine("  FAIL " + line); }
            report.AppendLine($"ARENA STAGE PROBE, {Scene}");
            report.AppendLine($"{stage.LayoutCount} layouts, {stage.Pieces.Length} pieces, catch at y {ArenaStage.CatchY:F2} (data {stage.CatchLine:F2}; poses believed down to {ArenaStage.MoveFloorY:F1}, kill plane {stage.KillPlaneY:F1}), " +
                              $"walls x {AIController.PlayableMinX:F1}..{AIController.PlayableMaxX:F1}, z {AIController.PlayableMinZ:F1}..{AIController.PlayableMaxZ:F1}");
            if (stage.LayoutCount == 0) Fail("the stage has no layouts");

            var marks = new List<Vector3>();
            var spawnRoot = GameObject.Find("SpawnPoints");
            if (spawnRoot != null) foreach (Transform mark in spawnRoot.transform) marks.Add(mark.position);
            if (marks.Count == 0) Fail("no spawn marks under SpawnPoints");

            Vector3 gap = default;
            bool haveGap = false;
            var savedPos = cam.transform.position; var savedRot = cam.transform.rotation; float savedFov = cam.fieldOfView;

            for (int l = 0; l < stage.LayoutCount; l++)
            {
                stage.HoldLayout = l;
                // A frame for the stage to switch, a fixed step for physics to take the colliders.
                yield return null;
                yield return new WaitForFixedUpdate();
                yield return null;

                var layout = stage.Layouts[l];
                report.AppendLine();
                report.AppendLine($"LAYOUT {l} '{layout.Name}' (can floor {layout.CanHeight:F2})");
                if (stage.Applied != l) { Fail($"{layout.Name}: the stage did not apply the held layout (applied {stage.Applied})"); continue; }
                var floor = layout.Colliders.transform;

                // Floor under the can and the marks.
                if (!Ground(floor, new Vector3(0f, layout.CanHeight + 2f, 0f), 6f, out var canHit) || Mathf.Abs(canHit.point.y - layout.CanHeight) > 0.03f)
                    Fail($"{layout.Name}: no floor at {layout.CanHeight:F2} under the can (found {(canHit.collider != null ? canHit.point.y.ToString("F2") : "nothing")})");
                else report.AppendLine($"  can: floor at {canHit.point.y:F3} on {canHit.collider.name}");
                for (int m = 0; m < marks.Count; m++)
                {
                    if (Ground(floor, marks[m] + Vector3.up * 2f, 6f, out var hit)) report.AppendLine($"  mark {m} ({marks[m].x:F1}, {marks[m].z:F1}): floor at {hit.point.y:F3} on {hit.collider.name}");
                    else Fail($"{layout.Name}: no floor under spawn mark {m} at ({marks[m].x:F1}, {marks[m].z:F1})");
                }

                // The flood fill.
                float half = Mathf.Ceil(stage.Radius + 1f);
                int n = Mathf.RoundToInt(2f * half / Cell) + 1;
                var heights = new List<float>[n, n];
                int cells = 0;
                for (int ix = 0; ix < n; ix++)
                    for (int iz = 0; iz < n; iz++)
                    {
                        float x = -half + ix * Cell, z = -half + iz * Cell;
                        if (x < AIController.PlayableMinX + WallMargin || x > AIController.PlayableMaxX - WallMargin
                            || z < AIController.PlayableMinZ + WallMargin || z > AIController.PlayableMaxZ - WallMargin) continue;
                        int count = Physics.RaycastNonAlloc(new Vector3(x, CastFrom, z), Vector3.down, Hits, CastDepth, ~0, QueryTriggerInteraction.Ignore);
                        for (int h = 0; h < count; h++)
                        {
                            if (!Hits[h].collider.transform.IsChildOf(floor) || Vector3.Angle(Hits[h].normal, Vector3.up) > Slope) continue;
                            (heights[ix, iz] ??= new List<float>()).Add(Hits[h].point.y);
                            cells++;
                        }
                        // The first open cell well inside the walls, a little off the stage's edge: where the fall is tried.
                        if (!haveGap && l == 0 && count == 0 && Mathf.Abs(x) < AIController.PlayableMaxX - 3f && Mathf.Abs(z) < AIController.PlayableMaxZ - 3f
                            && Nearby(floor, x, z, 2.5f) && !Nearby(floor, x, z, 1.0f)) { gap = new Vector3(x, 0f, z); haveGap = true; }
                    }

                var reached = new HashSet<(int, int, int)>();
                var queue = new Queue<(int, int, int)>();
                Vector3 taya = marks.Count > 0 ? marks[0] : Vector3.zero;
                int sx = Mathf.RoundToInt((taya.x + half) / Cell), sz = Mathf.RoundToInt((taya.z + half) / Cell);
                if (sx >= 0 && sz >= 0 && sx < n && sz < n && heights[sx, sz] != null)
                {
                    // The taya stands on the highest floor within the seat's reach of its mark.
                    int start = 0;
                    for (int k = 1; k < heights[sx, sz].Count; k++)
                        if (heights[sx, sz][k] <= taya.y + 2f && heights[sx, sz][k] > heights[sx, sz][start]) start = k;
                    reached.Add((sx, sz, start));
                    queue.Enqueue((sx, sz, start));
                }
                else Fail($"{layout.Name}: no ground in the grid cell of the taya's mark");
                while (queue.Count > 0)
                {
                    var (ix, iz, k) = queue.Dequeue();
                    float y = heights[ix, iz][k];
                    for (int d = 0; d < 4; d++)
                    {
                        int jx = ix + (d == 0 ? 1 : d == 1 ? -1 : 0), jz = iz + (d == 2 ? 1 : d == 3 ? -1 : 0);
                        if (jx < 0 || jz < 0 || jx >= n || jz >= n || heights[jx, jz] == null) continue;
                        for (int j = 0; j < heights[jx, jz].Count; j++)
                            if (Mathf.Abs(heights[jx, jz][j] - y) <= Step && reached.Add((jx, jz, j))) queue.Enqueue((jx, jz, j));
                    }
                }
                report.AppendLine($"  flood fill: {reached.Count} of {cells} ground cells reached on foot from the taya's mark ({Cell} m grid, {Step} m step, {Slope:F0} degree slope)");

                foreach (var piece in stage.Pieces)
                {
                    var shape = piece.Shapes[l];
                    if (!shape.Exists) continue;
                    int on = 0, got = 0;
                    foreach (var (ix, iz, k) in AllCells(heights, n))
                    {
                        float x = -half + ix * Cell, z = -half + iz * Cell;
                        if (!shape.Contains(x, z, 0.2f) || Mathf.Abs(heights[ix, iz][k] - shape.HeightAt(x, z)) > 0.05f) continue;
                        on++;
                        if (reached.Contains((ix, iz, k))) got++;
                    }
                    string line = $"{piece.Id,-8} {shape.Kind,-5}{(shape.Bonus ? " bonus" : "")}: {got} of {on} cells reached";
                    if (on == 0 && !shape.Bonus) report.AppendLine($"  {line} (wholly outside the play walls, or too thin for the grid)");
                    else if (got == 0 && !shape.Bonus) Fail($"{layout.Name}: {line}: it cannot be walked to from the taya's mark");
                    else report.AppendLine("  " + line);
                }
                for (int m = 1; m < marks.Count; m++)
                {
                    int ix = Mathf.RoundToInt((marks[m].x + half) / Cell), iz = Mathf.RoundToInt((marks[m].z + half) / Cell);
                    bool any = false;
                    if (ix >= 0 && iz >= 0 && ix < n && iz < n && heights[ix, iz] != null)
                        for (int k = 0; k < heights[ix, iz].Count; k++) any |= reached.Contains((ix, iz, k));
                    if (!any) Fail($"{layout.Name}: spawn mark {m} cannot be walked to from the taya's mark");
                }

                // Pads and pickups stand on floor.
                foreach (Transform feature in layout.Features.transform)
                    if (!Ground(floor, feature.position + Vector3.up * 0.5f, 1.0f, out var under) || Mathf.Abs(under.point.y - feature.position.y) > 0.1f)
                        Fail($"{layout.Name}: {feature.name} at {feature.position} has no floor under it");

                float r = stage.Radius;
                Shot(cam, new Vector3(0.55f * r, 1.5f * r, -1.25f * r), new Vector3(0f, layout.CanHeight, 0f), 55f, $"{Folder}/layout_{l}_{layout.Name}_above.png");
                Shot(cam, taya + Vector3.up * 1.6f + (layout.CanHeight - taya.y) * Vector3.up, new Vector3(0f, layout.CanHeight + 1f, 9f), CameraSystem.CameraRig.FppFieldOfView, $"{Folder}/layout_{l}_{layout.Name}_taya.png");
            }

            // The transformation, posed without a break: hologram only, half way, nearly there.
            if (stage.LayoutCount > 1)
            {
                stage.HoldLayout = 1;
                yield return null; yield return null;
                float r = stage.Radius;
                var eye = new Vector3(0.8f * r, 1.1f * r, -1.3f * r);
                foreach (var (travel, name) in new[] { (0f, "hologram"), (0.5f, "half"), (0.85f, "late") })
                {
                    stage.PoseTravel(0, 1, travel, 1f);
                    Shot(cam, eye, Vector3.zero, 55f, $"{Folder}/transform_0_to_1_{name}.png");
                }
                stage.Present();
                report.AppendLine();
                report.AppendLine($"TRANSFORMATION '{stage.Layouts[0].Name}' to '{stage.Layouts[1].Name}': three pictures written (transform_0_to_1_*.png)");
            }
            cam.transform.SetPositionAndRotation(savedPos, savedRot); cam.fieldOfView = savedFov;

            // The fall.
            stage.HoldLayout = 0;
            yield return null;
            yield return new WaitForFixedUpdate();
            report.AppendLine();
            var round = GameServices.Round;
            // ⚠️ THE FALL IS TRIED IN A LIVE ROUND, NOT BEFORE ONE. The arrival's shots and the
            // ready countdown hold the simulation (`PresentationClock.Held`: `CharacterMotor.FixedUpdate`
            // returns at once), and the round's start then teleports every seat to its mark. A body
            // put over the shaft before that hung in the air at y 1.0 and was then put back on the
            // stage: that, not the catch, was this probe's "caught NEVER, lowest y 0.00".
            float ready = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - ready < 60f && (PresentationClock.Held || round == null || !round.RoundActive)) { round = GameServices.Round; yield return null; }
            report.AppendLine($"The round was live {Time.realtimeSinceStartup - ready:F1} s after the layouts were walked (held {PresentationClock.Held}, round active {(round != null && round.RoundActive)}).");
            if (PresentationClock.Held || round == null || !round.RoundActive) Fail("fall: no live round within 60 s, so nothing simulates");
            for (float t = 0f; t < 1.0f; t += Time.unscaledDeltaTime) yield return null;
            // ⚠️ AN ATTACKER, NEVER THE TAYA. `CharacterMotor.Confine` clamps the round's defender to
            // the chalk box every step, so a taya put over the shaft is back on the drum one step
            // later and never falls: that was this probe's "caught NEVER, lowest y 0.00".
            CharacterMotor who = null;
            if (round != null) foreach (var p in round.Players) if (p != null && p.gameObject.activeInHierarchy && !p.IsEdgeRecovering && !p.IsDefender && p.IsBot) { who = p; break; }
            if (round != null && who == null) foreach (var p in round.Players) if (p != null && p.gameObject.activeInHierarchy && !p.IsEdgeRecovering && !p.IsDefender) { who = p; break; }
            if (who == null) Fail("fall: no body to drop (no round, or no active attacker)");
            else if (!haveGap) Fail("fall: found no open cell beside the stage inside the walls to drop a body into");
            else
            {
                // The pictures look outward from the stage's side of the body, the drone's height in frame.
                void Picture(string name)
                {
                    Vector3 at = who.transform.position, inward = Vector3.ProjectOnPlane(-at, Vector3.up).normalized;
                    Vector3 side = Vector3.Cross(Vector3.up, inward);
                    Shot(cam, at + inward * 6.5f + side * 3.0f + Vector3.up * 3.2f, at + Vector3.up * 1.7f, 55f, $"{Folder}/fall_{name}.png");
                    cam.transform.SetPositionAndRotation(savedPos, savedRot); cam.fieldOfView = savedFov;
                }

                // Taken in a LateUpdate after every other one: the body is posed in FixedUpdate and
                // the drone follows it in its own LateUpdate, so a picture taken from this coroutine
                // shows the drone a frame behind a body it is in fact over.
                string wanted = null;
                var late = new GameObject("Arena fall pictures").AddComponent<LateRecorder>();
                late.OnLate = _ => { if (wanted != null) { Picture(wanted); wanted = null; } };

                who.Teleport(new Vector3(gap.x, 1.0f, gap.z));
                float lowest = who.transform.position.y, began = Time.realtimeSinceStartup;
                bool shotFalling = false; int traced = 0; var trace = new StringBuilder();
                while (Time.realtimeSinceStartup - began < 8f && who.EdgeKind != EdgeRecoveryKind.Drone)
                {
                    Time.timeScale = 1f; lowest = Mathf.Min(lowest, who.transform.position.y);
                    // The first second and a half, ten times a second: where the body is and what is under it.
                    if (traced < 15 && Time.realtimeSinceStartup - began >= traced * 0.1f)
                    {
                        traced++;
                        Vector3 at = who.transform.position;
                        string under = "nothing";
                        if (Physics.Raycast(at + Vector3.up * 0.5f, Vector3.down, out var below, 14f, ~0, QueryTriggerInteraction.Ignore))
                            under = $"{below.collider.name} at y {below.point.y:F2} (under {below.collider.transform.root.name})";
                        trace.AppendLine($"    {Time.realtimeSinceStartup - began:F2} s: ({at.x:F2}, {at.y:F2}, {at.z:F2}) grounded {who.IsGrounded} vy {who.Velocity.y:F2} held {PresentationClock.Held} round {who.RoundActive}; under it: {under}");
                    }
                    if (!shotFalling && who.transform.position.y < -1.5f) { shotFalling = true; wanted = "1_falling"; }
                    yield return null;
                }
                bool caught = who.EdgeKind == EdgeRecoveryKind.Drone;
                float caughtAfter = Time.realtimeSinceStartup - began;
                report.AppendLine($"FALL: {who.name} (bot {who.IsBot}) dropped at ({gap.x:F1}, {gap.z:F1}); caught {(caught ? "after " + caughtAfter.ToString("F2") + " s" : "NEVER")}, lowest y {lowest:F2}");
                report.Append(trace);
                if (!caught) Fail("fall: the body was never taken by the drone (EdgeKind never became Drone)");
                else
                {
                    if (lowest <= ArenaStage.MoveFloorY) Fail($"fall: the body reached y {lowest:F2} before it was caught, under the {ArenaStage.MoveFloorY} where AcceptMove refuses poses");
                    // The owner's rule (2026-10-05): a real fall. The early catch (`ArenaFallRecovery.PoseLead`) may take
                    // a body at terminal speed a metre or so above the line; anything higher is the old short fall.
                    if (lowest > ArenaStage.CatchY + 2.5f) Fail($"fall: the body was caught at y {lowest:F2}, well above the catch line {ArenaStage.CatchY:F2}: not the long fall the design asks for");
                    began = Time.realtimeSinceStartup;
                    bool shotLift = false, shotBeam = false; float carriedLowest = lowest, highest = lowest;
                    while (Time.realtimeSinceStartup - began < 10f && who.IsEdgeRecovering)
                    {
                        Time.timeScale = 1f;
                        carriedLowest = Mathf.Min(carriedLowest, who.transform.position.y); highest = Mathf.Max(highest, who.transform.position.y);
                        if (!shotLift && who.EdgePhase == 1 && who.EdgePhaseRatio > 0.2f) { shotLift = true; wanted = "2_lifted"; }
                        if (!shotBeam && who.EdgePhase == 1 && who.EdgePhaseRatio > 0.7f) { shotBeam = true; wanted = "3_in_the_beam"; }
                        yield return null;
                    }
                    float carried = Time.realtimeSinceStartup - began;
                    report.AppendLine($"  carried for {carried:F2} s (the design's {CharacterMotor.DroneCatchSeconds + CharacterMotor.DroneCarrySeconds + CharacterMotor.DroneSetDownSeconds:F2}); lowest {carriedLowest:F2}, highest {highest:F2}; drones in the scene: {Object.FindObjectsByType<ArenaDrone>().Length}");
                    if (carriedLowest <= ArenaStage.MoveFloorY) Fail($"fall: the carried body reached y {carriedLowest:F2}, under {ArenaStage.MoveFloorY}");
                    if (who.IsEdgeRecovering) Fail("fall: the carry never ended");
                    else
                    {
                        Vector3 down = who.transform.position;
                        bool onFloor = Ground(stage.Layouts[0].Colliders.transform, down + Vector3.up * 0.5f, 1.0f, out var landed);
                        bool tagged = who.IsTagged;
                        report.AppendLine($"  set down at ({down.x:F2}, {down.y:F2}, {down.z:F2}); floor under it {(onFloor ? landed.collider.name + " at " + landed.point.y.ToString("F2") : "NONE")}; tagged {tagged}");
                        if (!onFloor) Fail("fall: the body was not set down on the stage");
                        if (!tagged) Fail("fall: the body was not frozen (IsTagged false) after the carry");
                        wanted = "4_set_down";
                        yield return null; yield return null;
                        began = Time.realtimeSinceStartup;
                        float movedFrozen = 0f;
                        while (Time.realtimeSinceStartup - began < 9f && who.IsTagged)
                        { Time.timeScale = 1f; movedFrozen = Mathf.Max(movedFrozen, Vector3.Distance(who.transform.position, down)); yield return null; }
                        float frozen = Time.realtimeSinceStartup - began;
                        report.AppendLine($"  frozen for {frozen:F2} s (the tag's own is {StatusRules.TaggedSeconds:F1}), moved {movedFrozen:F2} m while frozen");
                        if (who.IsTagged) Fail("fall: the freeze never ended");
                        if (movedFrozen > 0.25f) Fail($"fall: the frozen body moved {movedFrozen:F2} m");
                        if (frozen < StatusRules.TaggedSeconds - 0.6f || frozen > StatusRules.TaggedSeconds + 0.6f) Fail($"fall: the freeze lasted {frozen:F2} s, not the tag's {StatusRules.TaggedSeconds:F1}");
                        began = Time.realtimeSinceStartup;
                        float movedAfter = 0f;
                        while (Time.realtimeSinceStartup - began < 8f && movedAfter < 0.5f)
                        { Time.timeScale = 1f; movedAfter = Vector3.Distance(who.transform.position, down); yield return null; }
                        report.AppendLine($"  after the freeze: moved {movedAfter:F2} m in {Time.realtimeSinceStartup - began:F2} s (a bot, on its own)");
                        if (movedAfter < 0.5f) Fail($"fall: the body did not move again within 8 s of the freeze ending (moved {movedAfter:F2} m)");
                    }
                }
            }

            foreach (var leftover in Object.FindObjectsByType<LateRecorder>()) Object.Destroy(leftover.gameObject);

            // The pads and the pickup, each stood on once in the live round (layout 0).
            report.AppendLine();
            CharacterMotor tester = null;
            if (round != null) foreach (var p in round.Players) if (p != null && p.gameObject.activeInHierarchy && !p.IsEdgeRecovering && !p.IsDefender && !p.IsStunned && p != who) { tester = p; break; }
            if (tester == null || !round.RoundActive) Fail("pads: no free attacker in a live round to stand on them");
            else
            {
                var brain = tester.GetComponent<AIController>();
                if (brain != null) brain.enabled = false;
                tester.Intent.Clear(); tester.Intent.CommitFrame(); tester.Intent.Parked = true;
                var features = stage.Layouts[0].Features;
                report.AppendLine($"PADS AND PICKUP on '{stage.Layouts[0].Name}', stood on by {tester.name} with its brain off:");

                var jump = features.GetComponentInChildren<JumpPad>();
                if (jump == null) Fail("pads: layout 0 has no jump pad");
                else
                {
                    Vector3 pad = jump.transform.position;
                    tester.ClearStun(); tester.ClearTrip();
                    tester.Teleport(pad + Vector3.up * 0.05f);
                    float began = Time.realtimeSinceStartup, top = pad.y; bool launched = false, landed = false, taken = false;
                    while (Time.realtimeSinceStartup - began < 6f)
                    {
                        Time.timeScale = 1f;
                        if (!launched && tester.Velocity.y > jump.LaunchSpeed * 0.5f) launched = true;
                        top = Mathf.Max(top, tester.transform.position.y);
                        if (tester.IsEdgeRecovering) { taken = true; break; }
                        if (launched && tester.IsGrounded && Time.realtimeSinceStartup - began > 0.6f) { landed = true; break; }
                        yield return null;
                    }
                    Vector3 down = tester.transform.position;
                    report.AppendLine($"  jump pad at ({pad.x:F1}, {pad.y:F2}, {pad.z:F1}), launch speed {jump.LaunchSpeed:F1}: launched {launched}, apex {top - pad.y:F2} m over the pad, " +
                                      (taken ? "came down over the shaft and was taken by the drone" : landed ? $"on the ground again at ({down.x:F1}, {down.y:F2}, {down.z:F1})" + (Vector3.Distance(down, pad) > 8f ? " (that far from the pad it was put there: a tag sends a body to its mark)" : "") : "did not land within 6 s"));
                    if (!launched) Fail("pads: the jump pad did not launch a body stood on it");
                    else if (top - pad.y < 3.0f || top > 12.0f) Fail($"pads: the jump pad's apex was {top - pad.y:F2} m over the pad (the design asks for about 6, under the 12 m ceiling)");
                    began = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - began < 12f && (tester.IsEdgeRecovering || tester.IsStunned)) { Time.timeScale = 1f; yield return null; }
                }

                var speed = features.GetComponentInChildren<ArenaSpeedPad>();
                if (speed == null) Fail("pads: layout 0 has no speed pad");
                else
                {
                    Vector3 pad = speed.transform.position;
                    tester.ClearStun(); tester.ClearTrip();
                    tester.Teleport(pad + Vector3.up * 0.05f);
                    float began = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - began < 2f && !tester.IsSpeedBoosted) { Time.timeScale = 1f; yield return null; }
                    report.AppendLine($"  speed pad at ({pad.x:F1}, {pad.y:F2}, {pad.z:F1}): boosted {tester.IsSpeedBoosted} after {Time.realtimeSinceStartup - began:F2} s, scale {tester.SpeedBoostScale:F2} for {tester.SpeedBoostLeft:F2} s more (the pad gives {speed.Scale:F2} for {speed.Seconds:F1} s)");
                    if (!tester.IsSpeedBoosted) Fail("pads: the speed pad did not boost a body stood on it");
                }

                ArenaStaminaPickup orb = null;
                foreach (var candidate in features.GetComponentsInChildren<ArenaStaminaPickup>()) if (candidate.Available) { orb = candidate; break; }
                if (orb == null) Fail("pads: layout 0 has no stamina pickup that is there to take");
                else
                {
                    Vector3 at = orb.transform.position;
                    tester.ClearStun(); tester.ClearTrip();
                    tester.Stamina.Deplete();
                    float before = tester.Stamina.Ratio;
                    tester.Teleport(at + Vector3.up * 0.05f);
                    float began = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - began < 2f && orb.Available) { Time.timeScale = 1f; yield return null; }
                    report.AppendLine($"  stamina pickup at ({at.x:F1}, {at.y:F2}, {at.z:F1}): taken {!orb.Available} after {Time.realtimeSinceStartup - began:F2} s; stamina {before * 100f:F0}% before, {tester.Stamina.Ratio * 100f:F0}% after, fatigued {tester.Stamina.IsFatigued}; it returns in {orb.RespawnSeconds:F0} s");
                    if (orb.Available) Fail("pads: the stamina pickup was not taken by a body with empty stamina stood on it");
                    else if (tester.Stamina.Ratio < 0.95f) Fail($"pads: the pickup was taken but stamina is {tester.Stamina.Ratio * 100f:F0}%");
                }

                tester.Intent.Parked = false;
                if (brain != null) brain.enabled = true;
            }

            stage.HoldLayout = -1;
            report.Insert(0, (failures.Count == 0 ? "PASS" : $"FAIL ({failures.Count})") + Environment.NewLine);
            File.WriteAllText(Folder + "/stage_probe.txt", report.ToString());
            Debug.Log(report.ToString());
            Assert.IsEmpty(failures, "The Arena stage probe failed:\n" + string.Join("\n", failures) + "\n\n" + report);
#else
            Assert.Ignore("Editor only: loads the scene by path.");
            yield break;
#endif
        }

        private static IEnumerable<(int, int, int)> AllCells(List<float>[,] heights, int n)
        {
            for (int ix = 0; ix < n; ix++)
                for (int iz = 0; iz < n; iz++)
                    if (heights[ix, iz] != null)
                        for (int k = 0; k < heights[ix, iz].Count; k++) yield return (ix, iz, k);
        }

        /// <summary>The highest top of the layout's own colliders under a point, within `depth`.</summary>
        private static bool Ground(Transform floor, Vector3 from, float depth, out RaycastHit best)
        {
            best = default;
            bool found = false;
            int count = Physics.RaycastNonAlloc(from, Vector3.down, Hits, depth, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (!Hits[i].collider.transform.IsChildOf(floor)) continue;
                if (found && Hits[i].point.y <= best.point.y) continue;
                best = Hits[i];
                found = true;
            }
            return found;
        }

        /// <summary>True when the layout has floor within `reach` of a plan point, on one of the four sides.</summary>
        private static bool Nearby(Transform floor, float x, float z, float reach)
        {
            foreach (var side in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                if (Ground(floor, new Vector3(x, CastFrom, z) + side * reach, CastDepth, out _)) return true;
            return false;
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
