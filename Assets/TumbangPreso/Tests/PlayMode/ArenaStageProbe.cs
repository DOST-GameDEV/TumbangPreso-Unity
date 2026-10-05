using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
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
            report.AppendLine($"{stage.LayoutCount} layouts, {stage.Pieces.Length} pieces, catch at y {ArenaStage.CatchY:F2} (data {stage.CatchHeight:F2}), " +
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
            CharacterMotor who = null;
            if (round != null) foreach (var p in round.Players) if (p != null && p.gameObject.activeInHierarchy && !p.IsEdgeRecovering) { who = p; break; }
            if (who == null) Fail("fall: no body to drop (no round, or no active player)");
            else if (!haveGap) Fail("fall: found no open cell beside the stage inside the walls to drop a body into");
            else
            {
                who.Teleport(new Vector3(gap.x, 1.0f, gap.z));
                float lowest = who.transform.position.y, began = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - began < 8f && who.EdgeKind != EdgeRecoveryKind.Drone)
                { Time.timeScale = 1f; lowest = Mathf.Min(lowest, who.transform.position.y); yield return null; }
                bool caught = who.EdgeKind == EdgeRecoveryKind.Drone;
                float caughtAfter = Time.realtimeSinceStartup - began;
                report.AppendLine($"FALL: {who.name} dropped at ({gap.x:F1}, {gap.z:F1}); caught {(caught ? "after " + caughtAfter.ToString("F2") + " s" : "NEVER")}, lowest y {lowest:F2}");
                if (!caught) Fail("fall: the body was never taken by the drone (EdgeKind never became Drone)");
                else
                {
                    if (lowest <= ArenaStage.MoveFloorY) Fail($"fall: the body reached y {lowest:F2} before it was caught, under the {ArenaStage.MoveFloorY} where AcceptMove refuses poses");
                    began = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - began < 10f && who.IsEdgeRecovering) { Time.timeScale = 1f; yield return null; }
                    if (who.IsEdgeRecovering) Fail("fall: the carry never ended");
                    else
                    {
                        Vector3 down = who.transform.position;
                        bool onFloor = Ground(stage.Layouts[0].Colliders.transform, down + Vector3.up * 0.5f, 1.0f, out var landed);
                        bool tagged = who.IsTagged;
                        report.AppendLine($"  set down after {Time.realtimeSinceStartup - began:F2} s at ({down.x:F2}, {down.y:F2}, {down.z:F2}); floor under it {(onFloor ? landed.collider.name : "NONE")}; tagged {tagged}");
                        if (!onFloor) Fail("fall: the body was not set down on the stage");
                        if (!tagged) Fail("fall: the body was not frozen (IsTagged false) after the carry");
                        began = Time.realtimeSinceStartup;
                        while (Time.realtimeSinceStartup - began < 1.5f) { Time.timeScale = 1f; yield return null; }
                        float moved = Vector3.Distance(who.transform.position, down);
                        report.AppendLine($"  1.5 s later: moved {moved:F2} m, tagged {who.IsTagged}");
                        if (moved > 0.25f) Fail($"fall: the frozen body moved {moved:F2} m in 1.5 s");
                        if (!who.IsTagged) Fail("fall: the freeze ended within 1.5 s (the tag's own is 5 s)");
                    }
                }
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
