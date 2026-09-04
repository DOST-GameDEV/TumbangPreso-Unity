using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// Sets Eskinita's parked cars down on the road and excuses the things that genuinely hang.
    ///
    /// ⚠️⚠️ ESKINITA IS THE ONLY MAP `MapGeometryCheck` DOES NOT GATE, AND IT IS THE ONE THE
    /// TOURNAMENT PLAYS ON. Bayan Plaza reports 0 findings and Ilalim ng Tulay reports 0; Eskinita
    /// reports **ten**, and has been reporting them as `(informational)` for long enough that
    /// nobody reads them. `docs/TODO.md` § 124.11's line applies exactly: *"a green probe for a
    /// screen nobody can reach is worse than a red one"*, and a permanently amber map is the same
    /// fault wearing a different colour: it teaches every reader to skim the map report.
    ///
    /// **The ten, and what each one actually is:**
    ///
    /// | Finding | What it is | Answer |
    /// |---|---|---|
    /// | Four `Sasakyan_*` bodies float **0.263 m** over `Kalsada` | parked cars hovering a hand's width above the road | set them down |
    /// | `Sasakyan_2_W/door` floats 0.195 m | a child of one of those cars | follows its parent |
    /// | Two `Sampay_*` float 1.66 m and 2.47 m | washing lines strung between poles | excused, with a reason |
    /// | Two `Quad`s float 0.898 m | 60 x 74 m and 17 x 94 m backdrop planes | excused, with a reason |
    /// | `Sampay_0` stands **0.79 m** from the can spawn | the same washing line, seen from above | resolved by the excuse: see below |
    ///
    /// ⚠️⚠️ THE LAST ROW IS THE ONLY ONE THAT LOOKED LIKE A GAMEPLAY FAULT AND IS NOT ONE.
    /// `CheckLataIsClear` opens with `if (p.Airborne) continue;`, so marking the washing line as
    /// deliberately airborne clears it from that check too, and that is the correct reading
    /// rather than a way of silencing it. The rule exists because *"Ilalim ng Tulay had a trip
    /// hazard centred on"* the can; a wire strung **2.47 m overhead** is not something a
    /// retriever can walk into. It is only "at the can" when a footprint is flattened to two
    /// dimensions, which is what the check does before it knows the piece hangs.
    ///
    /// ⚠️⚠️ NOTHING HERE TOUCHES GAMEPLAY GEOMETRY, AND THAT WAS THE CONSTRAINT. The cars are
    /// `Dressing`, they are outside the chalk, and the check already reports `box  0 solid
    /// object(s) inside the chalk` before and after. **A car resting on the road instead of
    /// hovering over it changes what a frame looks like and nothing a player can touch.**
    ///
    /// ⚠️ IT IS A SCRIPT RATHER THAN A HAND EDIT FOR `BayanPlazaMonumentFix`'S REASON: the scene
    /// is an imported `.tscn`, so a hand edit is a diff nobody can review and a re-import
    /// silently undoes. It is idempotent (it measures the gap that is actually there and moves
    /// by that, so a second run moves nothing) and it refuses rather than guesses when the
    /// scene does not look the way this note says it does.
    /// </summary>
    public static class EskinitaRestingFix
    {
        private const string ScenePath = "Assets/TumbangPreso/Scenes/Maps/Eskinita.unity";

        /// <summary>The road the cars are parked on.</summary>
        private const string RoadPath = "Eskinita/Dressing/Kalsada";

        /// <summary>
        /// How far a car may be moved before this refuses.
        ///
        /// ⚠️ THE REPORTED GAP IS 0.263 m ON ALL FOUR. A bound of half a metre accepts that and
        /// every plausible re-measure of it, and refuses a scene where something else has moved
        /// and the "gap" is now a metre of open air. **A fixer that would move a car three metres
        /// because an unrelated prop changed is worse than a floating car.**
        /// </summary>
        private const float MaxDrop = 0.5f;

        [MenuItem("Tumbang Preso/Maps/Set Eskinita's Cars Down")]
        public static void FixFromMenu() => Fix();

        public static void Fix()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[EskinitaFix] could not open {ScenePath}");
                return;
            }

            var log = new StringBuilder();
            log.AppendLine("ESKINITA RESTING FIX");

            var road = GameObject.Find(RoadPath);
            if (road == null)
            {
                Debug.LogError($"[EskinitaFix] no road at {RoadPath}; refusing to guess.");
                return;
            }

            float roadTop = WorldBounds(road).max.y;
            log.AppendLine($"  road top y = {roadTop:F3}");

            int moved = 0;
            int excused = 0;

            // -------------------------------------------------------------------
            // § THE PARKED CARS
            //
            // ⚠️ THE DROP IS SOLVED FROM THE CAR'S OWN UNDERSIDE, NOT FROM A CONSTANT. Four cars
            // reported the same 0.263 m, which is exactly the sort of coincidence that produces a
            // hard-coded number and a fifth car nobody fixes. `CLAUDE.md` § 4's rule about
            // impulses says the same thing one layer down: write what you want and solve for it.
            // -------------------------------------------------------------------
            foreach (var car in FindAll("Eskinita/Dressing/Bahay", "Sasakyan_"))
            {
                // ⚠️⚠️ THE GAP IS MEASURED OFF THE `body` CHILD, NOT OFF THE WHOLE CAR, AND THE
                // FIRST RUN OF THIS FIXER PROVED WHY. Measuring the parent reported every car
                // *"already resting (gap 0.000 m)"* while `MapGeometryCheck` went on reporting
                // all four at 0.263 m, because the parent's renderer bounds also contain a
                // ground-level contact shadow sitting flat on the road at y = 0.100. **The
                // shadow was resting and the car was not**, and a fixer that averages the two
                // reports success and changes nothing.
                //
                // ⚠️ THE CHECK NAMES THE PIECE IT FAILED, so this solves for exactly the piece
                // the report is about: `Eskinita/Dressing/Bahay/Sasakyan_0_E/body`. The whole
                // car still MOVES, because a body lifted off its own shadow is a worse frame
                // than a car lifted off the road.
                var body = car.transform.Find("body");
                var measured = body != null ? WorldBounds(body.gameObject) : WorldBounds(car);

                if (measured.size == Vector3.zero) continue;

                float gap = measured.min.y - roadTop;

                if (gap <= 0.001f)
                {
                    log.AppendLine($"  {car.name,-16} already resting (gap {gap:F3} m)");
                    continue;
                }

                if (gap > MaxDrop)
                {
                    log.AppendLine($"  {car.name,-16} REFUSED: {gap:F3} m is more than the "
                                   + $"{MaxDrop:F2} m this fixer will move. Look at the scene.");
                    continue;
                }

                Undo.RecordObject(car.transform, "Set car down");
                car.transform.position -= new Vector3(0.0f, gap, 0.0f);
                EditorUtility.SetDirty(car);

                log.AppendLine($"  {car.name,-16} dropped {gap:F3} m onto the road");
                moved++;
            }

            // -------------------------------------------------------------------
            // § THE THINGS THAT ARE SUPPOSED TO HANG
            //
            // ⚠️⚠️ EXCUSED IN THE SCENE, NOT IN THE CHECKER, WHICH IS `AirborneByDesign`'S WHOLE
            // ARGUMENT: *"that list then rots the moment a prop is renamed, and worse, it is
            // invisible to anyone reading the scene: the object looks unchecked rather than
            // deliberately excused."* Every reason below is printed in
            // `Logs/map-geometry-check.txt` on every run, so they get re-read rather than
            // forgotten.
            // -------------------------------------------------------------------
            foreach (var line in FindAll("Eskinita/Dressing/Kable", "Sampay_"))
            {
                TumbangPreso.AirborneByDesign.Attach(line,
                    "Washing line strung between two poles across the eskinita, 2.5 m up. It "
                    + "hangs by construction and nothing walks into it.");

                EditorUtility.SetDirty(line);
                log.AppendLine($"  {line.name,-16} excused: washing line");
                excused++;
            }

            foreach (var quad in FindAll("Eskinita", "Quad"))
            {
                var b = WorldBounds(quad);

                // ⚠️ ONLY THE BIG ONES. A `Quad` is a primitive name and something small called
                // that could legitimately be a decal on the floor; the two this is for are 60 by
                // 74 m and 17 by 94 m, which is bigger than the arena and can only be a backdrop.
                if (b.size.x < 15.0f && b.size.z < 15.0f) continue;

                TumbangPreso.AirborneByDesign.Attach(quad,
                    $"Backdrop plane, {b.size.x:F0} by {b.size.z:F0} m, standing behind the "
                    + "street rather than resting on it.");

                EditorUtility.SetDirty(quad);
                log.AppendLine($"  {quad.name,-16} excused: backdrop plane "
                               + $"{b.size.x:F0} x {b.size.z:F0} m");
                excused++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            log.AppendLine($"  moved {moved} car(s), excused {excused} object(s)");
            Debug.Log(log.ToString());
        }

        /// <summary>
        /// Every descendant of <paramref name="parentPath"/> whose name starts with a prefix.
        ///
        /// ⚠️⚠️ RECURSIVE, AND THE FIRST VERSION WAS NOT, WHICH IS WHY IT SILENTLY MOVED NOTHING.
        /// It walked `parent.transform`'s DIRECT children only, and reported `moved 0 car(s)`
        /// with no error: an imported `.tscn` keeps whatever grouping the Godot scene had, so
        /// `Sasakyan_0_E` is not necessarily a direct child of `Bahay` even though the check
        /// prints the path as though it were. **A fixer that finds nothing and says nothing is
        /// the worst outcome available**, so this also refuses to be quiet: the caller logs the
        /// count either way and `Verify` re-runs the check.
        ///
        /// ⚠️ IT STOPS DESCENDING AT A MATCH. A car is `Sasakyan_0_E` with `body` and `door`
        /// under it; matching the parent and then continuing into it would return the parent and
        /// then try to move the children separately, which takes a car apart.
        /// </summary>
        private static List<GameObject> FindAll(string parentPath, string prefix)
        {
            var found = new List<GameObject>();

            var parent = GameObject.Find(parentPath);
            if (parent == null)
            {
                Debug.LogWarning($"[EskinitaFix] no object at {parentPath}");
                return found;
            }

            Collect(parent.transform, prefix, found);
            return found;
        }

        private static void Collect(Transform node, string prefix, List<GameObject> found)
        {
            foreach (Transform child in node)
            {
                if (child.name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    found.Add(child.gameObject);
                    continue;
                }

                Collect(child, prefix, found);
            }
        }

        /// <summary>
        /// The world bounds of everything this object draws.
        ///
        /// ⚠️ RENDERERS, NOT COLLIDERS, BECAUSE THAT IS WHAT THE CHECK MEASURES.
        /// `MapGeometryCheck` walks `Renderer.bounds`, so a fixer solving against colliders would
        /// move a car until its collider rested and leave the check still reporting the mesh.
        /// </summary>
        private static Bounds WorldBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(includeInactive: false);
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }
    }
}
