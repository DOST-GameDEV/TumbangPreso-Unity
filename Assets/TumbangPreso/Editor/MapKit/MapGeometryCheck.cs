using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TumbangPreso.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Refuse to ship an arena whose props hang in the air, whose floor has holes in it, or
    /// whose furniture stands inside the defender's box.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE "IT LOOKS FINE" IS NOT A MEASUREMENT AND A SCREENSHOT ONLY
    /// SHOWS THE FOUR ANGLES SOMEBODY CHOSE. Ilalim ng Tulay shipped with the entire west and
    /// east pavement floating 0.15 m over nothing, every kerb tile turned across the road
    /// instead of along it, five buildings standing on empty air 1.5 m past the edge of the
    /// ground, and every prop on the pavement sunk 0.062 m into it because the placement height
    /// was the tile's ORIGIN rather than the tile's TOP. Four probe renders were taken of that
    /// map and signed off. None of the four happened to look at a pavement edge.
    ///
    /// ⚠️ IT ABORTS RATHER THAN WARNS, the way `ArenaCheck` does, and for the same reason. A
    /// prop 60 mm into the floor does not get reported as a placement bug. It gets reported as
    /// "the chairs look wrong" or not at all.
    ///
    /// Run:
    ///   Unity.exe -batchmode -quit -nographics -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.MapKit.MapGeometryCheck.Run -logFile -
    /// </summary>
    public static class MapGeometryCheck
    {
        private const string ResultPath = "Logs/map-geometry-check.txt";

        /// <summary>
        /// How far a prop's underside may sit above whatever holds it up.
        ///
        /// ⚠️ 30 mm, AND IT IS NOT A TASTE NUMBER. The ground stack on this map steps by 60 mm
        /// (asphalt) and 62 mm (pavement tile), so a tolerance at or above 60 mm would accept a
        /// prop resting on the wrong layer entirely. Half the smallest step is the largest
        /// value that can still tell those two apart.
        /// </summary>
        public const float FloatTolerance = 0.030f;

        /// <summary>
        /// How far a prop may sink into what holds it up before it counts as buried.
        ///
        /// ⚠️ Deliberately looser than the float tolerance. Kerbs, potholes and barrier feet are
        /// MEANT to bite into the surface, and a prop 0.1 m down still reads as planted. What
        /// does not read as planted is a chair whose legs stop at the pavement, which is
        /// 0.062 m and is why this is not tighter than that either.
        /// </summary>
        public const float SinkTolerance = 0.100f;

        /// <summary>Fraction of a prop's footprint that has to be over solid ground.</summary>
        public const float SupportCoverage = 0.34f;

        /// <summary>
        /// Nothing solid may stand inside the box below this height.
        ///
        /// ⚠️ 1.2 m, which is chest height on the cast, not the full body height. A kerb or a
        /// painted line inside the box is fine; anything a taya has to walk AROUND is not,
        /// because the taya is CLAMPED into the box and cannot step out to go past it.
        /// </summary>
        public const float BoxClearanceHeight = 1.2f;

        /// <summary>How close a prop may come to the can, which spawns at the world origin.</summary>
        public const float LataClearance = 1.4f;

        /// <summary>
        /// The step a body climbs without pathing around it.
        ///
        /// ⚠️ IT IS `MatchInstaller`'s `CharacterController.stepOffset`, COPIED WITH ITS REASON,
        /// not a number chosen here. If that value ever moves, this check starts disagreeing
        /// with the engine about what counts as a wall, and it will disagree quietly.
        /// </summary>
        public const float StepOffset = 0.30f;

        [MenuItem("Tumbang Preso/Check Map Geometry")]
        public static void RunFromMenu() => Execute(true);

        public static void Run() => EditorApplication.Exit(Execute(true) ? 0 : 1);

        /// <summary>Report on every map, but only gate on the ones that have been through this.</summary>
        public static void RunReportOnly() => Execute(false);

        /// <summary>
        /// ⚠️ ONLY ILALIM NG TULAY IS GATED. Eskinita and Bayan Plaza were imported from the
        /// Godot `.tscn` files rather than built from code, so they are measured and printed
        /// but a finding in them is a TODO entry, not a build failure. Gating them today would
        /// mean either fixing two imported scenes in this pass or switching the gate off, and
        /// the second of those is how a check dies.
        /// </summary>
        private static readonly string[] Gated =
        {
            IlalimNgTulayBuilder.ScenePath,
        };

        private static readonly string[] Informational =
        {
            "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity",
            "Assets/TumbangPreso/Scenes/Maps/BayanPlaza.unity",
        };

        public static bool Execute(bool gate)
        {
            var sb = new StringBuilder();
            sb.AppendLine("MAP GEOMETRY CHECK");
            sb.AppendLine($"  float tolerance   : {FloatTolerance:F3} m above its support");
            sb.AppendLine($"  sink tolerance    : {SinkTolerance:F3} m below its support");
            sb.AppendLine($"  support coverage  : {SupportCoverage:P0} of the footprint");
            sb.AppendLine($"  box clearance     : nothing solid under {BoxClearanceHeight:F2} m " +
                          $"inside |x|,|z| < {Balance.ConfinementRadius:F2}");
            sb.AppendLine();

            int gatedFailures = 0;

            foreach (string scene in Gated)
                gatedFailures += Inspect(scene, sb, true);

            foreach (string scene in Informational)
                Inspect(scene, sb, false);

            sb.AppendLine();
            sb.AppendLine(gatedFailures > 0
                ? $"RESULT: ABORT. {gatedFailures} finding(s) in a gated map."
                : "RESULT: OK. No gated map has floating, buried, unsupported or box-blocking geometry.");

            try
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText(ResultPath, sb.ToString());
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapGeometryCheck] could not write {ResultPath}: {e.Message}");
            }

            if (gate && gatedFailures > 0) Debug.LogError(sb.ToString());
            else Debug.Log(sb.ToString());

            return !gate || gatedFailures == 0;
        }

        private sealed class Piece
        {
            public string Path;
            public Bounds World;
            public bool Airborne;
            public string AirborneReason;
        }

        private static int Inspect(string scenePath, StringBuilder sb, bool gated)
        {
            sb.AppendLine($"== {Path.GetFileNameWithoutExtension(scenePath)}  " +
                          $"({(gated ? "GATED" : "informational")})");

            if (!File.Exists(scenePath))
            {
                sb.AppendLine("   scene missing");
                return gated ? 1 : 0;
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var pieces = new List<Piece>();
            var excused = new List<Piece>();

            foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var mark = r.GetComponentInParent<AirborneByDesign>();
                var piece = new Piece
                {
                    Path = PathOf(r.transform),
                    World = r.bounds,
                    Airborne = mark != null,
                    AirborneReason = mark != null ? mark.Reason : null,
                };

                if (piece.Airborne) excused.Add(piece);
                pieces.Add(piece);
            }

            var content = new Bounds();
            bool first = true;
            foreach (var p in pieces)
            {
                if (first) { content = p.World; first = false; }
                else content.Encapsulate(p.World);
            }

            sb.AppendLine($"   renderers  {pieces.Count}");
            sb.AppendLine($"   content    x[{content.min.x:F2},{content.max.x:F2}] " +
                          $"y[{content.min.y:F2},{content.max.y:F2}] " +
                          $"z[{content.min.z:F2},{content.max.z:F2}]");

            int failures = 0;
            failures += CheckResting(pieces, sb);
            failures += CheckBoxIsClear(sb);
            failures += CheckLataIsClear(pieces, sb);
            failures += CheckFloorCoversThePlayableArea(pieces, sb);
            if (scenePath == IlalimNgTulayBuilder.ScenePath)
                failures += CheckIlalimElevatedAssembly(sb);

            if (excused.Count > 0)
            {
                sb.AppendLine($"   excused    {excused.Count} airborne by design:");
                foreach (var e in excused)
                    sb.AppendLine($"      {e.Path}  {(string.IsNullOrEmpty(e.AirborneReason) ? "NO REASON GIVEN" : e.AirborneReason)}");

                foreach (var e in excused)
                {
                    if (!string.IsNullOrEmpty(e.AirborneReason)) continue;

                    sb.AppendLine($"   FAIL {e.Path}: AirborneByDesign with an empty reason. " +
                                  "An unexplained exemption is how a real floating prop gets silenced.");
                    failures++;
                }
            }

            sb.AppendLine($"   findings   {failures}");
            sb.AppendLine();
            return failures;
        }

        /// <summary>
        /// The ordinary resting check deliberately skips `AirborneByDesign`, which is correct
        /// for signs and wrong as the only proof behind a bridge. This measures every join in
        /// the elevated assembly instead: pillars to soffit, bridge bay to bridge bay, track to
        /// deck, train to rail, and the complete utility span from ground to cable crown.
        /// </summary>
        private static int CheckIlalimElevatedAssembly(StringBuilder sb)
        {
            int failures = 0;
            var map = GameObject.Find("IlalimNgTulay");
            var tulay = map != null ? map.transform.Find("Dressing/Tulay") : null;
            var guideway = tulay != null ? tulay.Find("LrtGuideway") : null;

            if (guideway == null)
            {
                sb.AppendLine("   FAIL elevated: Dressing/Tulay/LrtGuideway is missing.");
                return 1;
            }

            var bays = new List<Bounds>();
            foreach (Transform child in guideway)
            {
                if (!child.name.StartsWith("GuidewayBay_")) continue;
                if (TryRendererBounds(child, out Bounds bounds)) bays.Add(bounds);
            }

            bays.Sort((a, b) => a.min.z.CompareTo(b.min.z));
            if (bays.Count != 12)
            {
                sb.AppendLine($"   FAIL elevated: guideway has {bays.Count} rendered bays, expected 12.");
                failures++;
            }

            Bounds deck = bays.Count > 0 ? bays[0] : new Bounds();
            for (int i = 0; i < bays.Count; i++)
            {
                Bounds bay = bays[i];
                if (i > 0)
                {
                    float seam = bay.min.z - bays[i - 1].max.z;
                    if (Mathf.Abs(seam) > FloatTolerance)
                    {
                        sb.AppendLine($"   FAIL elevated: guideway bay seam {i - 1}/{i} is {seam:F3} m.");
                        failures++;
                    }
                    deck.Encapsulate(bay);
                }
            }

            if (bays.Count > 0)
            {
                failures += Join(sb, "guideway soffit", deck.min.y, IlalimNgTulayBuilder.ViaductSoffit);
                failures += Join(sb, "guideway top", deck.max.y, IlalimNgTulayBuilder.GuidewayTop);
            }

            int pillars = 0;
            if (tulay != null)
            {
                foreach (Transform child in tulay)
                {
                    if (!child.name.StartsWith("LrtPillar_")) continue;
                    if (!TryRendererBounds(child, out Bounds pillar)) continue;
                    pillars++;
                    failures += Join(sb, child.name + " to soffit", pillar.max.y, deck.min.y);
                }
            }

            if (pillars != 8)
            {
                sb.AppendLine($"   FAIL elevated: guideway has {pillars} rendered pillars, expected 8.");
                failures++;
            }

            float railTop = float.NegativeInfinity;
            foreach (string trackName in new[] { "WestboundTrack", "EastboundTrack" })
            {
                var track = guideway.Find(trackName);
                if (track == null || !TryRendererBounds(track, out Bounds bounds))
                {
                    sb.AppendLine($"   FAIL elevated: {trackName} is missing.");
                    failures++;
                    continue;
                }

                failures += Join(sb, trackName + " to deck", bounds.min.y, deck.max.y);
                failures += Join(sb, trackName + " rail head", bounds.max.y, IlalimNgTulayBuilder.RailHead);
                railTop = Mathf.Max(railTop, bounds.max.y);
            }

            var train = tulay != null ? tulay.Find("LrtTrainSystem") : null;
            if (train == null)
            {
                sb.AppendLine("   FAIL elevated: LrtTrainSystem is missing.");
                failures++;
            }
            else if (!float.IsNegativeInfinity(railTop))
            {
                float wheelBottom = float.PositiveInfinity;
                int wheelRenderers = 0;
                foreach (var renderer in train.GetComponentsInChildren<Renderer>(includeInactive: true))
                {
                    if (!renderer.name.StartsWith("wheels-")) continue;
                    wheelBottom = Mathf.Min(wheelBottom, renderer.bounds.min.y);
                    wheelRenderers++;
                }

                if (wheelRenderers == 0)
                {
                    sb.AppendLine("   FAIL elevated: train has no named wheel renderers.");
                    failures++;
                }
                else
                {
                    failures += Join(sb, $"{wheelRenderers} train wheel meshes to rail head",
                                     wheelBottom, railTop);
                }
            }

            int utilitySpans = 0;
            int utilityPoles = 0;
            var westUtility = new List<Bounds>();
            var eastUtility = new List<Bounds>();
            var cable = map != null ? map.transform.Find("Dressing/Kable") : null;
            if (cable != null)
            {
                foreach (Transform child in cable)
                {
                    if (!TryRendererBounds(child, out Bounds span)) continue;

                    if (child.name.StartsWith("SidewalkWire_"))
                    {
                        utilitySpans++;
                        (span.center.x < 0.0f ? westUtility : eastUtility).Add(span);
                        if (span.size.z < 12.00f || span.size.x > 2.40f)
                        {
                            sb.AppendLine($"   FAIL elevated: utility wire is x {span.size.x:F2}, " +
                                          $"z {span.size.z:F2} m. It must run parallel to Z.");
                            failures++;
                        }
                    }
                    else if (child.name.StartsWith("SidewalkPole_"))
                    {
                        utilityPoles++;
                        failures += Join(sb, "utility pole foot", span.min.y,
                                         IlalimNgTulayBuilder.SurfaceTop(span.center.x));
                    }

                    if ((child.name.StartsWith("SidewalkWire_") || child.name.StartsWith("SidewalkPole_")) &&
                        Mathf.Abs(span.center.x) < 9.80f)
                    {
                        sb.AppendLine($"   FAIL elevated: utility piece centre x={span.center.x:F2} " +
                                      "is not at the shopfront edge of the pavement.");
                        failures++;
                    }
                }
            }

            foreach (var side in new[] { westUtility, eastUtility })
            {
                side.Sort((a, b) => a.min.z.CompareTo(b.min.z));
                for (int i = 1; i < side.Count; i++)
                {
                    float seam = side[i].min.z - side[i - 1].max.z;
                    if (Mathf.Abs(seam) <= FloatTolerance) continue;
                    sb.AppendLine($"   FAIL elevated: sidewalk cable seam {i - 1}/{i} is {seam:F3} m.");
                    failures++;
                }
            }

            if (utilitySpans != 26)
            {
                sb.AppendLine($"   FAIL elevated: map has {utilitySpans} complete utility spans, expected 26.");
                failures++;
            }
            if (utilityPoles != 28)
            {
                sb.AppendLine($"   FAIL elevated: map has {utilityPoles} single utility poles, expected 28.");
                failures++;
            }

            var trike = map != null ? map.transform.Find("Dressing/Tindahan/Cargo_Tricycle_Boxes") : null;
            if (trike == null || trike.Find("HandlebarStem") == null || trike.Find("HandlebarGripJoin") == null)
            {
                sb.AppendLine("   FAIL elevated: cargo tricycle handlebar stem or grip join is missing.");
                failures++;
            }

            var hoop = map != null ? map.transform.Find("Dressing/Tindahan/BridgeHoop") : null;
            if (hoop == null || hoop.Find("RimBracket") == null)
            {
                sb.AppendLine("   FAIL elevated: bridge hoop rim bracket is missing.");
                failures++;
            }

            sb.AppendLine($"   elevated   {bays.Count} joined bays, {pillars} pillar-to-soffit joins, " +
                          $"2 track-to-deck joins, train on rail, {utilitySpans} wire spans on " +
                          $"{utilityPoles} grounded single poles, " +
                          "trike and hoop brackets present");
            return failures;
        }

        private static int Join(StringBuilder sb, string name, float first, float second)
        {
            float gap = first - second;
            if (Mathf.Abs(gap) <= FloatTolerance) return 0;
            sb.AppendLine($"   FAIL elevated: {name} misses by {gap:F3} m ({first:F3} vs {second:F3}).");
            return 1;
        }

        private static bool TryRendererBounds(Transform root, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        /// <summary>
        /// Every prop either rests on something or is excused. ⚠️ THE SUPPORT IS THE HIGHEST
        /// SURFACE UNDER THE PROP'S FOOTPRINT, NOT y = 0. The pavement here is a real 0.212 m
        /// surface and a bin that rests on world zero is 0.212 m INSIDE it; testing against
        /// zero would have passed every one of the buried props that shipped.
        /// </summary>
        private static int CheckResting(List<Piece> pieces, StringBuilder sb)
        {
            int failures = 0;
            int floating = 0, buried = 0, unsupported = 0;

            foreach (var p in pieces)
            {
                if (p.Airborne) continue;

                float best = Support(pieces, p, out string bestName);

                if (float.IsNegativeInfinity(best))
                {
                    // Nothing at all under it. Only a finding if it is off the ground plane;
                    // the ground plate itself legitimately has nothing beneath it.
                    if (p.World.min.y <= FloatTolerance) continue;

                    sb.AppendLine($"   FAIL {p.Path}: underside at y={p.World.min.y:F3} with NOTHING " +
                                  "beneath it. It is standing on the sky.");
                    unsupported++;
                    failures++;
                    continue;
                }

                float gap = p.World.min.y - best;

                if (gap > FloatTolerance)
                {
                    // ⚠️ THE FOOTPRINT IS PRINTED WITH THE FINDING. A prop whose AABB reaches
                    // across two surfaces (a utility pole whose crossarm hangs 6.6 m out over
                    // the road) is held up by the surface under its POST, and the grid can only
                    // report the one that carries the most squares. Without the footprint in the
                    // message that reads as a placement bug rather than as an AABB that is
                    // mostly overhang.
                    sb.AppendLine($"   FAIL {p.Path}: floats {gap:F3} m above {bestName} " +
                                  $"(top y={best:F3}, prop underside y={p.World.min.y:F3}, " +
                                  $"footprint {p.World.max.x - p.World.min.x:F2} by " +
                                  $"{p.World.max.z - p.World.min.z:F2} m, under it: {Profile(pieces, p)}).");
                    floating++;
                    failures++;
                }
                else if (gap < -SinkTolerance)
                {
                    sb.AppendLine($"   FAIL {p.Path}: sunk {-gap:F3} m into {bestName} " +
                                  $"(surface y={best:F3}, prop underside y={p.World.min.y:F3}).");
                    buried++;
                    failures++;
                }
            }

            sb.AppendLine($"   resting    {floating} floating, {buried} buried, {unsupported} over void");
            return failures;
        }

        /// <summary>
        /// What the grid found under a prop, as "height x count", highest first.
        ///
        /// ⚠️ IT IS PRINTED WITH EVERY FLOAT FINDING BECAUSE THE COVERAGE RULE IS THE PART THAT
        /// IS EASY TO GET WRONG, AND ARGUING WITH IT FROM THE SOURCE WASTED A ROUND TRIP. A
        /// finding that says "0.212 x 10, 0.000 x 15" answers "is this prop misplaced or is its
        /// bounding box mostly overhang" without opening the scene.
        /// </summary>
        private static string Profile(List<Piece> pieces, Piece p)
        {
            var levels = SampleFootprint(pieces, p, out _);
            var counts = new Dictionary<int, int>();

            foreach (float level in levels)
            {
                int key = float.IsNegativeInfinity(level) ? int.MinValue : Mathf.RoundToInt(level * 1000.0f);
                counts.TryGetValue(key, out int n);
                counts[key] = n + 1;
            }

            var keys = new List<int>(counts.Keys);
            keys.Sort((a, b) => b.CompareTo(a));

            var sb = new StringBuilder();
            foreach (int key in keys)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(key == int.MinValue ? "void" : (key / 1000.0f).ToString("F3"));
                sb.Append(" x").Append(counts[key]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// The height of the surface holding this prop up, or negative infinity if there is none.
        ///
        /// ⚠️⚠️ SAMPLED ACROSS THE FOOTPRINT, BECAUSE THE GROUND IS TILED AND A SINGLE RENDERER
        /// IS NEVER THE ANSWER. The first version asked "which one renderer covers at least a
        /// third of this prop", which is right for a bin on a slab and wrong for everything
        /// standing on a pavement made of 2 m tiles: a 3.1 m by 4.1 m shophouse touches four
        /// tiles and no single tile covers a third of it, so the test skipped past all four and
        /// settled on the big apron plate 62 mm lower. It then reported twelve correctly placed
        /// props as floating by exactly one plaza tile's thickness, which is the most convincing
        /// possible false positive: the number is real, it is just the wrong two surfaces.
        ///
        /// The grid asks the question per square instead, so four tiles at one height count as
        /// one surface at that height, which is what they are.
        /// </summary>
        private static float[] SampleFootprint(List<Piece> pieces, Piece p, out string[] names)
        {
            const int grid = 5;

            float ceiling = p.World.min.y + SinkTolerance;
            var levels = new float[grid * grid];
            names = new string[grid * grid];

            for (int ix = 0; ix < grid; ix++)
            {
                float x = Mathf.Lerp(p.World.min.x, p.World.max.x, (ix + 0.5f) / grid);

                for (int iz = 0; iz < grid; iz++)
                {
                    float z = Mathf.Lerp(p.World.min.z, p.World.max.z, (iz + 0.5f) / grid);

                    float top = float.NegativeInfinity;
                    string name = null;

                    foreach (var q in pieces)
                    {
                        if (ReferenceEquals(p, q)) continue;
                        if (q.World.max.y > ceiling || q.World.max.y <= top) continue;
                        if (x < q.World.min.x || x > q.World.max.x) continue;
                        if (z < q.World.min.z || z > q.World.max.z) continue;

                        top = q.World.max.y;
                        name = q.Path;
                    }

                    levels[ix * grid + iz] = top;
                    names[ix * grid + iz] = name;
                }
            }

            return levels;
        }

        private static float Support(List<Piece> pieces, Piece p, out string bestName)
        {
            bestName = null;

            var levels = SampleFootprint(pieces, p, out string[] names);

            // The support is the HIGHEST level that carries enough of the footprint. Anything
            // higher than that is a kerb clipping one corner, not what the prop rests on.
            int needed = Mathf.CeilToInt(levels.Length * SupportCoverage);
            float best = float.NegativeInfinity;

            for (int i = 0; i < levels.Length; i++)
            {
                if (float.IsNegativeInfinity(levels[i]) || levels[i] <= best) continue;

                int carried = 0;
                for (int j = 0; j < levels.Length; j++)
                    if (levels[j] >= levels[i] - 0.001f) carried++;

                if (carried < needed) continue;

                best = levels[i];
                bestName = names[i];
            }

            return best;
        }

        /// <summary>
        /// ⚠️⚠️ THE DEFENDER IS CLAMPED INTO THE BOX AND CANNOT STEP OUT TO WALK AROUND
        /// ANYTHING. A pillar inside |x|,|z| &lt; 7 is not cover, it is a wall in the one room a
        /// taya is locked in, and it does not get reported as a placement bug: it gets reported
        /// as the taya "getting stuck" or as passive defence being unbeatable on one side.
        /// Ilalim ng Tulay shipped with two 3.4 m viaduct columns standing inside it.
        /// </summary>
        private static int CheckBoxIsClear(StringBuilder sb)
        {
            float r = Balance.ConfinementRadius;
            int failures = 0;

            foreach (var col in UnityEngine.Object.FindObjectsByType<Collider>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (col.isTrigger) continue;

                var b = col.bounds;

                // ⚠️⚠️ ANYTHING A BODY CAN WALK OVER IS GROUND, NOT FURNITURE, AND THE THRESHOLD
                // IS THE ONE THE ENGINE ALREADY USES. `MatchInstaller` gives every seat a
                // `CharacterController.stepOffset` of 0.30, so a kerb, a threshold or a ramp
                // below that is stepped over without the taya ever having to path around it.
                // Testing against zero instead reported both of Ilalim ng Tulay's 0.15 m kerbs
                // as walls in the defender's box, which would have meant either deleting the
                // kerb line that makes the chalk readable or switching this check off.
                if (b.max.y <= StepOffset) continue;
                if (b.min.y >= BoxClearanceHeight) continue;

                float insideX = Mathf.Min(b.max.x, r) - Mathf.Max(b.min.x, -r);
                float insideZ = Mathf.Min(b.max.z, r) - Mathf.Max(b.min.z, -r);
                if (insideX <= 0.0f || insideZ <= 0.0f) continue;

                // A collider that spans the whole box in both axes is the floor or a bounds
                // volume, not furniture.
                if (insideX >= r * 1.9f && insideZ >= r * 1.9f) continue;

                sb.AppendLine($"   FAIL {PathOf(col.transform)}: a solid collider stands inside the " +
                              $"defender's box ({insideX:F2} m by {insideZ:F2} m of it, from y=" +
                              $"{b.min.y:F2} to y={b.max.y:F2}). The taya is clamped in there and " +
                              "cannot walk around it.");
                failures++;
            }

            sb.AppendLine($"   box        {failures} solid object(s) inside the chalk");
            return failures;
        }

        /// <summary>
        /// ⚠️ THE CAN SPAWNS AT THE WORLD ORIGIN. `MatchInstaller.BuildLata` creates it at
        /// `Vector3.zero` and nothing on a map moves it, so a prop or a hazard at the origin is
        /// sitting inside the object the entire sport is about hitting. Ilalim ng Tulay had a
        /// trip hazard centred on it.
        /// </summary>
        private static int CheckLataIsClear(List<Piece> pieces, StringBuilder sb)
        {
            int failures = 0;

            foreach (var p in pieces)
            {
                if (p.Airborne) continue;
                if (p.World.max.y <= 0.25f) continue; // flat ground and markings are fine

                float dx = Mathf.Max(0.0f, Mathf.Max(p.World.min.x, -p.World.max.x));
                float dz = Mathf.Max(0.0f, Mathf.Max(p.World.min.z, -p.World.max.z));
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d >= LataClearance) continue;

                sb.AppendLine($"   FAIL {p.Path}: stands {d:F2} m from the world origin, where the " +
                              $"can spawns. Minimum is {LataClearance:F2} m.");
                failures++;
            }

            foreach (var t in UnityEngine.Object.FindObjectsByType<StreetTripHazard>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Vector3 c = t.transform.position;
                float d = new Vector2(c.x, c.z).magnitude;
                if (d >= LataClearance) continue;

                sb.AppendLine($"   FAIL {PathOf(t.transform)}: a trip hazard is centred {d:F2} m from " +
                              "the can. Retrieval already happens there; this trips everybody who " +
                              "goes for the tsinelas at the one spot they all have to reach.");
                failures++;
            }

            return failures;
        }

        /// <summary>
        /// Every square metre a player can stand on has floor under it.
        ///
        /// ⚠️ SAMPLED ON A GRID, NOT EYEBALLED FROM ABOVE. A hole a metre across is invisible in
        /// an overview render and is a fall through the kill plane in play. The grid is the
        /// walled area, because that is exactly what a body can reach.
        /// </summary>
        private static int CheckFloorCoversThePlayableArea(List<Piece> pieces, StringBuilder sb)
        {
            float halfX = 8.6f, halfZ = 13.0f;

            var bounds = GameObject.Find("Bounds");
            if (bounds != null)
            {
                foreach (var col in bounds.GetComponentsInChildren<BoxCollider>())
                {
                    Vector3 c = col.transform.position + col.center;
                    if (Mathf.Abs(c.x) > 1.0f) halfX = Mathf.Max(halfX, Mathf.Abs(c.x));
                    if (Mathf.Abs(c.z) > 1.0f) halfZ = Mathf.Max(halfZ, Mathf.Abs(c.z));
                }
            }

            // Only the ground stack counts as floor: a roof at 9 m is not something to stand on.
            var floor = new List<Bounds>();
            foreach (var p in pieces)
                if (p.World.min.y <= 0.35f && p.World.max.y <= 1.0f) floor.Add(p.World);

            const float step = 0.5f;
            int holes = 0;
            float firstX = 0.0f, firstZ = 0.0f;

            for (float x = -halfX + step * 0.5f; x < halfX; x += step)
            {
                for (float z = -halfZ + step * 0.5f; z < halfZ; z += step)
                {
                    bool covered = false;
                    foreach (var b in floor)
                    {
                        if (x < b.min.x || x > b.max.x || z < b.min.z || z > b.max.z) continue;
                        covered = true;
                        break;
                    }

                    if (covered) continue;
                    if (holes == 0) { firstX = x; firstZ = z; }
                    holes++;
                }
            }

            if (holes > 0)
            {
                sb.AppendLine($"   FAIL floor: {holes} sample(s) of the walled area at " +
                              $"x +/-{halfX:F1}, z +/-{halfZ:F1} have no ground under them, " +
                              $"first at ({firstX:F2}, {firstZ:F2}). A body there falls to the " +
                              "kill plane.");
                return 1;
            }

            sb.AppendLine($"   floor      solid across x +/-{halfX:F1}, z +/-{halfZ:F1}");
            return 0;
        }

        private static string PathOf(Transform t)
        {
            string s = t.name;
            for (var p = t.parent; p != null; p = p.parent) s = p.name + "/" + s;
            return s;
        }
    }
}
