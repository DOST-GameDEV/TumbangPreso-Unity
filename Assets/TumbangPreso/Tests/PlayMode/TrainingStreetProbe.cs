using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// What is actually standing in the street on a guided run, and how big it is.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE THREE OF THE FOUR THINGS 🧑 PHOTOGRAPHED IN THE 4.70 TUTORIAL WERE
    /// OBJECTS NOBODY COULD NAME FROM THE PIXELS. *"theres a floating slipper check ss, and the
    /// pet of nemu is here??"*, *"i clicked N skip and this shit showed up wtf is this yellow
    /// shit on me??"*, *"i can pick up slippers from ppl's hands wtf?"*. `FppFrameProbe` was
    /// written for exactly this class of report and could not have caught any of them: it skips
    /// anything whose path contains "Slipper", which is the object all three turned out to be
    /// about, and it never loads the tutorial.
    ///
    /// ⚠️ SO THIS MEASURES SIZES AND HEIGHTS RATHER THAN PHOTOGRAPHING THEM. A tsinelas is
    /// 0.43 m long and rests within a few centimetres of the road; anything reporting otherwise
    /// is the thing in the screenshot, whatever it looked like.
    ///
    /// The report is `Logs/training-street.txt` and it is the point of the file. The assertions
    /// are narrow.
    /// </summary>
    public class TrainingStreetProbe
    {
        private const string OutPath = "Logs/training-street.txt";

        /// <summary>⚠️ A LIVE ROUND OUTLIVES THE SCENE. The directors are `DontDestroyOnLoad`,
        /// so a suite that leaves one running poisons the NEXT one. `SoloPracticeTests` carries
        /// the same TearDown for the same reason.</summary>
        [TearDown]
        public void TearDown() => Quiesce();

        /// <summary>
        /// End any match still running, in the DIRECTORS rather than in the scene.
        ///
        /// ⚠️⚠️ CALLED BEFORE EVERY LOAD AS WELL AS AFTER, AND THE SECOND HALF IS NOT
        /// BELT AND BRACES. `MatchDirector.StartMatch` refuses to open a match that is already
        /// in progress, so a test that inherits a live one from the suite before it gets an
        /// arena where `SliceRunner.Begin` silently does nothing: no round starts, the guided
        /// route never reaches `HideTheCast`, and the report describes a full match rather than a
        /// training street. That is exactly what this file measured when its three tests ran
        /// together and not when they ran alone. `docs/TODO.md` § 13 records the same shape of
        /// flake taking `LandedHighlightTests` down twice.
        /// </summary>
        private static void Quiesce()
        {
            // ⚠️⚠️ THE OLD ROUTE IS DESTROYED HERE, AND WITHOUT THIS LINE THE SECOND TEST IN
            // THE FILE MEASURED A FULL MATCH AND PASSED. `GuidedTraining.OnDestroy` clears
            // `GameLaunch.GuidedTutorial`, correctly, so that quitting the tutorial cannot leak
            // the flag into a match. A scene load destroys the old route DURING the load, which
            // is after a test has set the flag and before `MatchInstaller` reads it: the flag was
            // therefore false by the time the new arena asked, no route installed, and the report
            // described four live seats in a normal round. Nothing failed, which is the worst
            // version of this.
            foreach (var route in Object.FindObjectsByType<GuidedTraining>(FindObjectsSortMode.None))
                if (route != null) Object.DestroyImmediate(route);

            GameLaunch.GuidedTutorial = false;
            GameServices.Round?.EndRound();
            GameServices.Match?.ResetForNewMatch();
            GameServices.Round?.ResetForNewMatch();
        }

        [UnityTest]
        public IEnumerator TheGuidedStreetHoldsNothingItWasNotAskedFor()
        {
            Quiesce();

            GameLaunch.GuidedTutorial = true;
            GameLaunch.AllBots = false;
            GameLaunch.Spectator = false;

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            for (int i = 0; i < 60; i++) yield return null;

            // ⚠️ THE ROUTE HAS TO BE THERE OR THIS TEST IS MEASURING A MATCH. See `Quiesce`.
            Assert.IsNotNull(Object.FindFirstObjectByType<GuidedTraining>(),
                "the guided route did not install, so this arena is an ordinary match and every " +
                "assertion below would be about the wrong thing.");

            var lines = new StringBuilder();
            lines.AppendLine("THE GUIDED STREET, on the frame the route reaches its first lesson.");
            lines.AppendLine();

            // ---- the cast -------------------------------------------------------------
            lines.AppendLine("seats:");

            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsInactive.Include,
                                                                       FindObjectsSortMode.None))
            {
                if (!m.IsPerson) continue;

                lines.AppendLine($"  slot {m.PlayerSlot} {(m.IsDefender ? "TAYA    " : "attacker")} " +
                                 $"active={m.gameObject.activeInHierarchy,-5} " +
                                 $"at {m.transform.position.ToString("0.00")}");
            }

            // ---- the pet --------------------------------------------------------------
            //
            // ⚠️⚠️ `GhostPetCompanion.Bind` UNPARENTS THE PET TO THE SCENE ROOT, so hiding its
            // owner does not hide it. That is the whole of *"the pet of nemu is here??"*.
            lines.AppendLine();
            lines.AppendLine("pets:");

            var strayPets = new System.Collections.Generic.List<string>();

            foreach (var pet in Object.FindObjectsByType<Visual.GhostPetCompanion>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                bool drawing = false;

                foreach (var r in pet.GetComponentsInChildren<Renderer>(true))
                    if (r != null && r.enabled) drawing = true;

                bool ownerDrawn = OwnerIsDrawn(pet);

                lines.AppendLine($"  {pet.name} drawing={drawing,-5} ownerDrawn={ownerDrawn,-5} " +
                                 $"scale={pet.transform.lossyScale.ToString("0.00")} " +
                                 $"at {pet.transform.position.ToString("0.00")}");

                if (drawing && !ownerDrawn)
                    strayPets.Add($"{pet.name} is drawing while its owner is hidden");
            }

            // ---- every tsinelas -------------------------------------------------------
            lines.AppendLine();
            lines.AppendLine("tsinelas:");

            var floaters = new System.Collections.Generic.List<string>();

            foreach (var s in Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include,
                                                                FindObjectsSortMode.None))
            {
                var r = s.GetComponentInChildren<Renderer>(true);
                Vector3 size = r != null ? r.bounds.size : Vector3.zero;
                float bottom = r != null ? r.bounds.min.y : s.transform.position.y;

                float ground = GroundUnder(s.transform.position);
                float clearance = bottom - ground;

                lines.AppendLine($"  {s.name,-10} skin={s.SkinIndex} state={s.State,-8} " +
                                 $"active={s.gameObject.activeInHierarchy,-5} " +
                                 $"holder={(s.Holder != null ? s.Holder.name : "-"),-8} " +
                                 $"y={s.transform.position.y:0.000} ground={ground:0.000} " +
                                 $"clearance={clearance:0.000} size={size.ToString("0.00")}");

                // ⚠️ ONLY A LOOSE ONE. A tsinelas in a hand is SUPPOSED to be a metre up, and a
                // thrown one is supposed to be in the air.
                if (!s.gameObject.activeInHierarchy) continue;
                if (s.State != SlipperState.Loose) continue;

                if (clearance > 0.30f)
                    floaters.Add($"{s.name} rests {clearance:0.00} m off the road");
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText(OutPath, lines.ToString());
            Debug.Log($"[TrainingStreet] wrote {OutPath}\n{lines}");

            Assert.IsEmpty(strayPets,
                "a companion pet is drawing in a street its owner was taken out of:\n  "
                + string.Join("\n  ", strayPets));

            Assert.IsEmpty(floaters,
                "a loose tsinelas is resting in mid-air:\n  " + string.Join("\n  ", floaters)
                + "\nRead " + OutPath + " for the whole street.");
        }

        /// <summary>
        /// How big the tsinelas in the local player's own hands is, for every one of the four
        /// skins.
        ///
        /// ⚠️⚠️ THE VIEWMODEL SLIPPER IS THE ONE OBJECT IN THE GAME NOTHING WAS MEASURING.
        /// `FppFrameProbe` skips every path containing "Slipper" on purpose, because the held
        /// one is legitimately in the player's face; that exclusion is also why a skin-dependent
        /// size fault could ship. `ViewmodelArms.NormaliseHeldSize` divides by the arm's CURRENT
        /// world scale, so what it produces depends on which pose the arm was in when the mesh
        /// was swapped, and it only runs at all when the picked mesh differs from the authored
        /// default.
        ///
        /// ⚠️ IT DRIVES `MatchSkin` WITH ALL FOUR REAL SLIPPERS rather than changing the setting
        /// and reloading, because that is four scene loads to measure one function, and the four
        /// tsinelas in an ordinary match already wear the four skins.
        /// </summary>
        [UnityTest]
        public IEnumerator TheHeldTsinelasIsTheSameSizeInEverySkin()
        {
            Quiesce();

            GameLaunch.GuidedTutorial = false;
            GameLaunch.AllBots = false;
            GameLaunch.Spectator = false;

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            for (int i = 0; i < 60; i++) yield return null;

            var arms = Object.FindFirstObjectByType<CameraSystem.ViewmodelArms>();
            Assert.IsNotNull(arms, "the first-person arms were never built");

            var held = FindChild(arms.transform, "HeldSlipper");
            Assert.IsNotNull(held, "the viewmodel has no HeldSlipper node");

            var renderer = held.GetComponent<Renderer>();
            Assert.IsNotNull(renderer, "the viewmodel slipper has no renderer");

            arms.SetHolding(true);
            yield return null;

            var lines = new StringBuilder();
            lines.AppendLine("THE TSINELAS IN THE PLAYER'S OWN HANDS, per skin.");
            lines.AppendLine();
            lines.AppendLine($"presenting length is ViewmodelArms.SlipperLength = " +
                             $"{CameraSystem.ViewmodelArms.SlipperLength:0.000} m");
            lines.AppendLine();
            lines.AppendLine($"{"skin",4} {"mesh",-26} {"localScale",10} {"world size",24} {"longest",8}");
            lines.AppendLine(new string('-', 92));

            var wrong = new System.Collections.Generic.List<string>();

            foreach (var s in Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include,
                                                                FindObjectsSortMode.None))
            {
                arms.MatchSkin(s);

                // Two frames: the pose lerps, and the size must not depend on that.
                yield return null;
                yield return null;

                var filter = held.GetComponent<MeshFilter>();
                string mesh = filter != null && filter.sharedMesh != null ? filter.sharedMesh.name : "-";

                Vector3 size = renderer.bounds.size;
                float longest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));

                lines.AppendLine($"{s.SkinIndex,4} {mesh,-26} {held.localScale.x,10:0.000} " +
                                 $"{size.ToString("0.000"),24} {longest,8:0.000}");

                // ⚠️ THE BOUND IS THE PRESENTING LENGTH ITSELF, WITH ROOM FOR THE SKIN'S OWN
                // PROPORTIONS. A crocs is taller than a flip-flop and its longest axis is still
                // its length; twice the target is not a proportion, it is a bug.
                if (longest > CameraSystem.ViewmodelArms.SlipperLength * 2.0f)
                {
                    wrong.Add($"skin {s.SkinIndex} ({mesh}) presents {longest:0.00} m against the " +
                              $"{CameraSystem.ViewmodelArms.SlipperLength:0.00} m it is supposed to");
                }
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/held-tsinelas.txt", lines.ToString());
            Debug.Log($"[HeldTsinelas]\n{lines}");

            Assert.IsEmpty(wrong,
                "the tsinelas in the player's own hands is the wrong size:\n  "
                + string.Join("\n  ", wrong)
                + "\nRead Logs/held-tsinelas.txt.");
        }


        /// <summary>
        /// Walks the route lesson by lesson and writes down what is in front of the camera on
        /// each one.
        ///
        /// ⚠️⚠️ THIS IS THE TEST THAT ANSWERS *"wtf is this yellow shit on me"*. Every report
        /// off the 4.70 tutorial was about an OBJECT, and every one of them was reasoned about
        /// from the pixels for an hour before anybody measured it. The route is deterministic:
        /// entering a lesson is a function call, so the frame he photographed can be entered on
        /// purpose and written down as names, distances and viewport coordinates.
        ///
        /// ⚠️ IT ENTERS LESSONS THROUGH REFLECTION RATHER THAN THROUGH A NEW PUBLIC METHOD,
        /// because the route's shape is the thing under test and an entry point that only a
        /// probe uses is one more thing that can disagree with the N key.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryLessonIsEnteredWithNothingInThePlayersFace()
        {
            Quiesce();

            GameLaunch.GuidedTutorial = true;
            GameLaunch.AllBots = false;
            GameLaunch.Spectator = false;

            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            for (int i = 0; i < 60; i++) yield return null;

            var route = Object.FindFirstObjectByType<GuidedTraining>();
            Assert.IsNotNull(route, "the guided route never installed");

            var rig = Object.FindFirstObjectByType<CameraSystem.CameraRig>();
            Assert.IsNotNull(rig, "no camera rig in the arena");
            var cam = rig.Camera;
            Assert.IsNotNull(cam);

            var enter = typeof(GuidedTraining).GetMethod("EnterLesson",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(enter, "GuidedTraining.EnterLesson is gone; this probe needs it");

            var lines = new StringBuilder();
            lines.AppendLine("EVERY LESSON, AND WHAT IS DRAWN WITHIN TWO METRES OF THE EYE.");
            lines.AppendLine();

            var inTheFace = new System.Collections.Generic.List<string>();
            var floaters = new System.Collections.Generic.List<string>();
            var oversized = new System.Collections.Generic.List<string>();
            var earlyDeck = new System.Collections.Generic.List<string>();

            for (int step = 0; step < GuidedTraining.LessonCount; step++)
            {
                enter.Invoke(route, new object[] { (GuidedTraining.Lesson)step });

                for (int i = 0; i < 6; i++) yield return null;

                lines.AppendLine($"--- {step:00} {(GuidedTraining.Lesson)step} " +
                                 $"---------------------------------------------");

                // ⚠️⚠️ THE KIT IS NOT ON SCREEN UNTIL THE LESSON THAT TEACHES IT. 🧑, 2026-08-26:
                // *"make it so that my skills cant be seen too until i need to use them myself"*,
                // then *"THIS IS FOR TUTORIAL BTW NOT THE ACTUAL GAME"*. The assertion is one
                // directional on purpose: a deck that stays hidden AFTER its lesson would be a
                // bug too, but it is also what a seat with no kit legitimately does, and this
                // probe drives lessons directly rather than picking a hero.
                var deck = FindInScene("HeroDeck");
                bool deckDrawn = deck != null && deck.activeInHierarchy;

                lines.AppendLine($"    hero deck drawn = {deckDrawn}");

                if (deckDrawn && step < (int)GuidedTraining.Lesson.AbilityInfo)
                {
                    earlyDeck.Add($"{(GuidedTraining.Lesson)step}: the hero deck is on screen " +
                                  "before the lesson that teaches it");
                }

                foreach (var s in Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include,
                                                                    FindObjectsSortMode.None))
                {
                    if (!s.gameObject.activeInHierarchy) continue;

                    var sr = s.GetComponentInChildren<Renderer>(true);
                    float bottom = sr != null ? sr.bounds.min.y : s.transform.position.y;
                    float clearance = bottom - GroundUnder(s.transform.position);

                    lines.AppendLine($"    {s.name} state={s.State} clearance={clearance:0.000} " +
                                     $"holder={(s.Holder != null ? s.Holder.name : "-")}");

                    if (s.State == SlipperState.Loose && clearance > 0.30f)
                    {
                        floaters.Add($"{(GuidedTraining.Lesson)step}: {s.name} rests " +
                                     $"{clearance:0.00} m off the road");
                    }
                }

                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude,
                                                                     FindObjectsSortMode.None))
                {
                    if (r == null || !r.enabled) continue;
                    if (r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly) continue;

                    Vector3 view = cam.WorldToViewportPoint(r.bounds.center);
                    if (view.z <= 0.0f || view.z > 2.0f) continue;
                    if (view.x < -0.1f || view.x > 1.1f) continue;
                    if (view.y < -0.1f || view.y > 1.1f) continue;

                    string path = PathOf(r.transform);
                    if (path.Contains("Viewmodel") || path.Contains("HeldSlipper")) continue;

                    // ⚠️⚠️ THE MARKER IS MEASURED, NOT LOOKED AT, AND IT HAS BEEN WRONG TWICE.
                    // § 13.6 replaced a 5.2 m pole with what its own note calls a ground ring,
                    // and the mesh it reached for was a unit SPHERE that `VfxShapes.Lay` does not
                    // scale in Y: 1.40 by **2.00** by 1.39, a translucent ball standing on the
                    // target. Half a metre is the height of a thing lying on the road.
                    if (path.Contains("TrainingObjectiveMarker") && r.bounds.size.y > 0.60f)
                    {
                        oversized.Add($"{(GuidedTraining.Lesson)step}: {path} is " +
                                      $"{r.bounds.size.y:0.00} m tall");
                    }

                    lines.AppendLine($"    IN FRAME {view.z,5:0.00} m at ({view.x:0.00},{view.y:0.00}) " +
                                     $"size {r.bounds.size.ToString("0.00")}  {path}");

                    // The road under the player's own feet is allowed to be close.
                    if (path.Contains("Kalsada") || path.Contains("Markings")
                        || path.Contains("Floor") || path.Contains("Slab")) continue;

                    // ⚠️ ONE METRE, AND THE BOUND IS THE LESSONS THEMSELVES. Three lessons put a
                    // body or the lata deliberately close: the shove at 1.40 m, the punch at
                    // 1.50 and the defender reset with the lata at 1.15, each inside the reach of
                    // the verb it teaches. Anything nearer than a metre is not a lesson, it is
                    // something drawn over the player.
                    if (view.z <= 1.0f)
                    {
                        inTheFace.Add($"{(GuidedTraining.Lesson)step}: {path} at {view.z:0.00} m, " +
                                      $"viewport ({view.x:0.00},{view.y:0.00})");
                    }
                }
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/training-lessons.txt", lines.ToString());
            Debug.Log("[TrainingLessons] " + lines);

            Assert.IsEmpty(earlyDeck,
                "the guided route shows the kit early: "
                + string.Join(" | ", earlyDeck) + ". Read Logs/training-lessons.txt.");

            Assert.IsEmpty(oversized,
                "the training marker is not a mark on the road: "
                + string.Join(" | ", oversized) + ". Read Logs/training-lessons.txt.");

            Assert.IsEmpty(inTheFace,
                "a lesson puts something in the player's own face: "
                + string.Join(" | ", inTheFace) + ". Read Logs/training-lessons.txt.");

            Assert.IsEmpty(floaters,
                "a lesson leaves a loose tsinelas in mid-air: "
                + string.Join(" | ", floaters) + ". Read Logs/training-lessons.txt.");
        }

        /// <summary>The first object in the loaded scene with this name, active or not.</summary>
        private static GameObject FindInScene(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                  FindObjectsSortMode.None))
                if (t.name == name) return t.gameObject;

            return null;
        }

        private static string PathOf(Transform t)
        {
            var parts = new System.Collections.Generic.List<string>();
            for (var step = t; step != null; step = step.parent) parts.Add(step.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static bool OwnerIsDrawn(Visual.GhostPetCompanion pet)
        {
            foreach (var v in Object.FindObjectsByType<Visual.CharacterVisual>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (v.Pet != pet.gameObject) continue;
                return v.gameObject.activeInHierarchy;
            }

            // Nothing claims it. A pet whose owner is gone is a stray by definition.
            return false;
        }

        private static float GroundUnder(Vector3 where)
        {
            if (Physics.Raycast(where + Vector3.up * 4.0f, Vector3.down, out var hit, 20.0f,
                                ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point.y;
            }

            return 0.0f;
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;

            return null;
        }
    }
}
