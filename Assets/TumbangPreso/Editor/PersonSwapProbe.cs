using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TumbangPreso.EditorTools
{
    /// <summary>
    /// Proves a REPLACEMENT Person still does everything the game asks of a Person.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE EVERY WAY A SWAPPED RIG CAN FAIL IS SILENT. The art replacement in
    /// `docs/Port_Plan.md` section 8 hands the game a new mesh on an old skeleton, and each of
    /// the four things that can go wrong there produces a character that imports cleanly, spawns
    /// cleanly, and is simply wrong in play:
    ///
    ///   * clips that no longer bind      the character stands in its bind pose forever, and
    ///                                    `CharacterAnimator` logs nothing because the clips DO
    ///                                    exist, they just drive nothing.
    ///   * a renamed or missing bone      `arm-right` is hunted by string in two places, the
    ///                                    hand anchor and the third-person wind-up. A miss is one
    ///                                    warning during a match, and a carried tsinelas that
    ///                                    hangs in the air.
    ///   * a height outside the cast's   `PersonScale` is one constant for the whole cast.
    ///                                    The twelve rigs span 0.6613 to 0.7928 and all take
    ///                                    it, so the bound is that range, not the base rig.
    ///   * UVs outside the palette rows   `Toon.shader` falls through to the raw atlas, and the
    ///                                    character wears stock Kenney colours while every meter
    ///                                    and every name around it stays correct.
    ///
    /// So this asserts all four against the ASSET, and then photographs the result, because the
    /// fifth failure is the one no assert catches: it imports, it animates, and it looks wrong.
    ///
    /// ⚠️ IT MUST RUN WITHOUT `-nographics`. Same rule the sheet carries: with no rendering
    /// device the capture comes back blank and the run still reports success.
    ///
    /// Run:
    ///   Unity.exe -batchmode -quit -projectPath . \
    ///             -executeMethod TumbangPreso.EditorTools.PersonSwapProbe.Run
    /// </summary>
    public static class PersonSwapProbe
    {
        /// <summary>The rig under test, and the one it replaces. ⚠️ BOTH, because "the new one
        /// is 0.6790 tall" means nothing without the range it has to sit inside.</summary>
        private const string NewModel = "Assets/TumbangPreso/Art/characters/persons/team-zack.glb";
        private const string OldModel = "Assets/TumbangPreso/Art/characters/persons/character-female-b.glb";

        private const string RosterId = "zack";
        private const string ReportPath = "Logs/person-swap-probe.txt";
        private const string ShotPath = "Logs/person-swap-probe.png";

        /// <summary>
        /// The clips photographed, and they are the ones a match actually spends its time in.
        /// `static` is the bind pose and is included as the control: if every column looks like
        /// `static`, the clips are not binding.
        /// </summary>
        private static readonly string[] Poses =
        {
            "static", "idle", "walk", "sprint", "holding-right-shoot", "attack-kick-right",
            "die", "emote-yes", "emote-no", "crouch", "sit", "pick-up",
        };

        /// <summary>
        /// Every emote the wheel can fire, and the clip chain `CharacterAnimator` resolves it
        /// through. ⚠️ TRANSCRIBED FROM THAT FILE'S OWN TABLES, because they are the contract:
        /// a chain resolves to the FIRST clip the rig actually has, and returns null rather than
        /// guessing when it has none of them. A rig missing every clip in a chain does not
        /// error, it just never plays that emote.
        /// </summary>
        private static readonly (string Emote, string[] Chain)[] EmoteChains =
        {
            ("yes", new[] { "emote-yes", "interact-right" }),
            ("no", new[] { "emote-no", "interact-left" }),
            ("sit", new[] { "sit", "crouch" }),
            ("crouch", new[] { "crouch", "sit" }),
            ("dead", new[] { "die", "crouch" }),
            ("tpose", new[] { "static", "idle" }),
            ("bow", new[] { "pick-up", "interact-right" }),
        };

        private const int CellPixels = 300;

        [MenuItem("Tumbang Preso/Probe Person Swap")]
        public static void RunFromMenu() => Execute();

        public static void Run() => EditorApplication.Exit(Execute() ? 0 : 1);

        private static bool Execute()
        {
            var report = new StringBuilder();
            report.AppendLine("PERSON SWAP PROBE");
            report.AppendLine($"new: {NewModel}");
            report.AppendLine($"old: {OldModel}");
            report.AppendLine();

            bool ok = true;
            AssetDatabase.ImportAsset(NewModel, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ok &= CheckAsset(report);
            ok &= CheckRoster(report);
            ok &= CheckAnimationBinds(report);
            ok &= CheckEmotes(report);
            ok &= Shoot(report);
            ok &= ShootTurnaround(report);

            report.AppendLine();
            report.AppendLine(ok ? "RESULT: PASS" : "RESULT: FAIL");

            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, report.ToString());
            Debug.Log(report.ToString());

            return ok;
        }

        // -------------------------------------------------------------------

        private static bool CheckAsset(StringBuilder report)
        {
            report.AppendLine("-- asset");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NewModel);
            if (prefab == null)
            {
                report.AppendLine("FAIL: nothing imported. Is glTFast claiming the .glb?");
                return false;
            }

            bool ok = true;

            var clips = AssetDatabase.LoadAllAssetsAtPath(NewModel)
                                     .OfType<AnimationClip>()
                                     .Where(c => !c.name.StartsWith("__preview"))
                                     .ToList();

            var oldClips = AssetDatabase.LoadAllAssetsAtPath(OldModel)
                                        .OfType<AnimationClip>()
                                        .Where(c => !c.name.StartsWith("__preview"))
                                        .ToList();

            report.AppendLine($"clips: {clips.Count} (base rig has {oldClips.Count})");

            if (clips.Count != oldClips.Count)
            {
                report.AppendLine("FAIL: the clip set did not survive the rebuild.");
                ok = false;
            }

            // ⚠️ EVERY NAME `CharacterAnimator` CAN ASK FOR, not a sample of them. It resolves a
            // verb through a FALLBACK CHAIN and a missing clip drops silently to the next one, so
            // a rig short of `sprint` does not fail, it just never runs.
            foreach (string wanted in new[]
                     {
                         "idle", "walk", "sprint", "jump", "fall", "crouch", "sit", "die",
                         "pick-up", "static", "emote-yes", "emote-no", "holding-right",
                         "holding-right-shoot", "interact-right", "interact-left",
                         "attack-melee-right", "attack-kick-right",
                     })
            {
                if (clips.Any(c => c.name == wanted)) continue;

                report.AppendLine($"FAIL: no clip '{wanted}'.");
                ok = false;
            }

            var instance = Object.Instantiate(prefab);

            var skinned = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            report.AppendLine($"skinned meshes: {skinned.Length}");

            if (skinned.Length == 0)
            {
                report.AppendLine("FAIL: no SkinnedMeshRenderer. Nothing can be posed.");
                ok = false;
            }

            if (instance.GetComponentInChildren<Animator>(true) == null)
            {
                report.AppendLine("FAIL: no Animator. See ModelImportSetup: this fails silently.");
                ok = false;
            }

            // The two bones the game hunts by NAME. `CharacterVisual.BuildHandAnchor` parks the
            // carried tsinelas on the first of these it finds, and `CharacterAnimator`'s wind-up
            // writes the same bone so opponents can read the commitment.
            var bones = new HashSet<string>();
            foreach (var s in skinned)
                foreach (var b in s.bones)
                    if (b != null) bones.Add(b.name);

            report.AppendLine($"bones: {string.Join(", ", bones.OrderBy(b => b))}");

            foreach (string wanted in new[] { "root", "torso", "head",
                                              "arm-left", "arm-right", "leg-left", "leg-right" })
            {
                if (bones.Contains(wanted)) continue;

                report.AppendLine($"FAIL: no bone '{wanted}'.");
                ok = false;
            }

            // ⚠️ THE HAND HAS TO HAVE WEIGHTED VERTICES, not just a bone. `PalmCentre` needs at
            // least eight vertices weighted above 0.5 to the arm before it will place an anchor,
            // and it returns quietly when it cannot: the shoe floats and nothing is logged.
            foreach (string arm in new[] { "arm-right", "arm-left" })
            {
                int weighted = WeightedVertexCount(skinned, arm);
                report.AppendLine($"vertices weighted to {arm}: {weighted}");

                if (weighted >= 8) continue;

                report.AppendLine($"FAIL: {arm} carries {weighted} vertices, PalmCentre needs 8.");
                ok = false;
            }

            ok &= CheckHeight(report, instance);
            ok &= CheckPaletteRows(report, skinned);
            ok &= CheckHandAnchor(report, skinned);
            ok &= CheckFacing(report, skinned);
            ok &= CheckDyedSide(report, instance, skinned);

            Object.DestroyImmediate(instance);
            return ok;
        }

        /// <summary>
        /// Which way the new rig looks, measured against the rig it replaces.
        ///
        /// ⚠️⚠️ IT IS MEASURED OFF THE EYES, AND GETTING THIS WRONG IS INVISIBLE IN EVERY OTHER
        /// CHECK. A model built facing the wrong way imports, animates, poses, carries a slipper
        /// and passes every assert above; it simply walks backwards through the entire game.
        /// `CharacterVisual.PersonModelYaw` exists because the original build shipped exactly
        /// that, and its header records it as *"reported across more than ten sessions"* with
        /// every attempt going looking in the yaw maths, which was correct all along.
        ///
        /// The yaw constant is one number for the whole cast, so a replacement does not get to
        /// choose: it has to face the same way the base rig faces. Slot 8 is the eyes and the
        /// mouth and, on a head, nothing else, so where those vertices sit IS the answer.
        /// </summary>
        private static bool CheckFacing(StringBuilder report, SkinnedMeshRenderer[] skinned)
        {
            report.AppendLine();
            report.AppendLine("-- facing");

            float mine = InkDepth(skinned.Select(s => s == null ? null : s.sharedMesh).ToArray());
            float theirs = InkDepth(BaseMeshes());

            report.AppendLine($"face ink sits at z {mine:F4} (base rig {theirs:F4})");

            if (Mathf.Approximately(mine, 0.0f) || Mathf.Approximately(theirs, 0.0f))
            {
                report.AppendLine("FAIL: could not find face ink on one of the rigs.");
                return false;
            }

            if (Mathf.Sign(mine) == Mathf.Sign(theirs)) return true;

            report.AppendLine("FAIL: the new rig faces the opposite way to the one it replaces. "
                              + "PersonModelYaw is one constant for the whole cast, so this "
                              + "character would walk, aim and throw backwards.");
            return false;
        }

        /// <summary>
        /// Which side of the head the dyed hair is on, answered against a BONE.
        ///
        /// ⚠️⚠️ THIS EXISTS BECAUSE THE SIDE WAS GUESSED WRONG THREE TIMES FROM SCREENSHOTS.
        /// Two transforms sit between the box table and the pixels, glTFast's X negation and
        /// `PersonModelYaw`, and squinting at a 300 px render to decide whether the crimson is
        /// on the viewer's left is not a measurement, it is a coin toss that feels like one.
        ///
        /// The bone named `arm-left` IS the character's left arm, by definition and in whatever
        /// space Unity ends up using, so "the dye is on the character's left" is exactly "the
        /// crimson vertices and that bone share a sign in X". The reference art puts it on the
        /// viewer's right of a figure facing the camera, which is that figure's left.
        /// </summary>
        private static bool CheckDyedSide(StringBuilder report, GameObject instance,
                                          SkinnedMeshRenderer[] skinned)
        {
            report.AppendLine();
            report.AppendLine("-- dyed hair side");

            Transform arm = null;

            foreach (var t in instance.GetComponentsInChildren<Transform>(true))
                if (t.name == "arm-left") arm = t;

            if (arm == null)
            {
                report.AppendLine("FAIL: no arm-left bone to measure against.");
                return false;
            }

            float armX = instance.transform.InverseTransformPoint(arm.position).x;

            // Slot 2 is the dye. Resolved the same way the shader resolves it, on the head.
            Mesh head = null;

            foreach (var s in skinned)
                if (s != null && s.sharedMesh != null &&
                    (head == null || s.sharedMesh.bounds.max.y > head.bounds.max.y))
                    head = s.sharedMesh;

            if (head == null) return false;

            var uv = head.uv;
            var vertices = head.vertices;

            float sum = 0.0f;
            int n = 0;

            for (int i = 0; i < vertices.Length && i < uv.Length; i++)
            {
                int col = Mathf.Clamp(Mathf.FloorToInt(uv[i].x * 16.0f), 0, 15);
                int row = Mathf.Clamp(Mathf.FloorToInt(uv[i].y * 16.0f), 0, 15);

                if (row > 7) continue;
                if ((col / 2) + (row <= 3 ? 8 : 0) != 2) continue;

                sum += vertices[i].x;
                n++;
            }

            if (n == 0)
            {
                report.AppendLine("FAIL: no slot-2 vertices. The hair carries no dye at all.");
                return false;
            }

            float dyeX = sum / n;
            report.AppendLine($"dye mean x {dyeX:F4}, arm-left at x {armX:F4}, "
                              + $"{n} dyed vertices");

            if (Mathf.Sign(dyeX) == Mathf.Sign(armX))
            {
                report.AppendLine("the dye is on the character's LEFT, which is the viewer's "
                                  + "right. Matches the reference.");
                return true;
            }

            report.AppendLine("FAIL: the dye is on the character's RIGHT. The reference puts it "
                              + "on their left. Negate the X of the streak boxes.");
            return false;
        }

        /// <summary>
        /// Mean Z of the slot-8 vertices on the tallest mesh, which is the head.
        ///
        /// ⚠️ THE HEAD ONLY. Slot 8 is also a dark garment on the body of several rigs, and
        /// averaging the whole character mixes the eyes in with somebody's shorts and lands the
        /// answer near zero.
        /// </summary>
        private static float InkDepth(Mesh[] meshes)
        {
            Mesh head = null;

            foreach (var mesh in meshes)
                if (mesh != null && (head == null || mesh.bounds.max.y > head.bounds.max.y))
                    head = mesh;

            if (head == null) return 0.0f;

            var uv = head.uv;
            var vertices = head.vertices;

            float sum = 0.0f;
            int n = 0;

            for (int i = 0; i < vertices.Length && i < uv.Length; i++)
            {
                int col = Mathf.Clamp(Mathf.FloorToInt(uv[i].x * 16.0f), 0, 15);
                int row = Mathf.Clamp(Mathf.FloorToInt(uv[i].y * 16.0f), 0, 15);

                if (row > 7) continue;
                if ((col / 2) + (row <= 3 ? 8 : 0) != 8) continue;

                sum += vertices[i].z;
                n++;
            }

            return n == 0 ? 0.0f : sum / n;
        }

        /// <summary>
        /// Where a carried tsinelas will actually sit, asked of the GAME's own search.
        ///
        /// ⚠️⚠️ THIS IS THE CHECK A NEW RIG IS MOST LIKELY TO FAIL AND LEAST LIKELY TO FAIL
        /// LOUDLY. `CharacterVisual.BuildHandAnchor` finds the arm bone, averages the far eighth
        /// of what is weighted to it, and parks the shoe `HandTopLift` above that. Every step is
        /// derived from the MESH, so a replacement whose arm is a different shape moves the
        /// anchor without changing a line of code, and the failure is a slipper floating beside
        /// the hand during a throw. The Godot side records eight guessed offsets that each landed
        /// somewhere different and wrong: in the chest, under the arm, on the face.
        ///
        /// So the anchor is resolved here and checked against the geometry it is supposed to be
        /// resting on: it has to be inside the hand horizontally, and above it vertically.
        /// </summary>
        private static bool CheckHandAnchor(StringBuilder report, SkinnedMeshRenderer[] skinned)
        {
            report.AppendLine();
            report.AppendLine("-- hand anchor (a carried tsinelas rides this)");

            bool ok = true;

            foreach (string arm in new[] { "arm-right", "arm-left" })
            {
                var target = skinned.FirstOrDefault(
                    s => s != null && s.bones != null &&
                         s.bones.Any(b => b != null && b.name == arm));

                if (target == null)
                {
                    report.AppendLine($"FAIL: no skinned mesh carries {arm}.");
                    ok = false;
                    continue;
                }

                int index = System.Array.FindIndex(target.bones, b => b != null && b.name == arm);

                if (!CharacterVisual.PalmCentre(target, index, out Vector3 palm))
                {
                    report.AppendLine($"FAIL: PalmCentre found no hand on {arm}. A carried "
                                      + "slipper cannot follow the arm.");
                    ok = false;
                    continue;
                }

                Vector3 anchor = palm + Vector3.up * CharacterVisual.HandTopLift;

                // ⚠️ THE HAND, NOT THE ARM. Everything from the shoulder down is weighted to this
                // bone, so bounding all of it puts the "top of the hand" up at the shoulder and
                // the check passes whatever the geometry does. This is the same far-eighth
                // selection `PalmCentre` makes, and it is asserted below to contain the palm that
                // came back, so the two cannot quietly disagree about which end the hand is.
                Bounds hand = HandBounds(target, index);

                // ⚠️ EXPANDED BY A MILLIMETRE, AND NOT OUT OF LAZINESS. On a voxel hand the far
                // eighth of the limb is a SINGLE FLAT FACE, so every vertex in the blob shares
                // one X and the mean lands exactly on the bounds' own face. An exact `Contains`
                // then fails or passes on the last bit of a float average, which is a coin toss
                // rather than a test.
                var tolerant = hand;
                tolerant.Expand(0.002f);

                if (!tolerant.Contains(palm))
                {
                    report.AppendLine($"FAIL: {arm}: the measured palm is outside the hand blob. "
                                      + "This probe and PalmCentre disagree about the geometry.");
                    ok = false;
                    continue;
                }

                report.AppendLine($"   {arm}: palm {Fmt(palm)}  anchor {Fmt(anchor)}");
                report.AppendLine($"          hand spans {Fmt(hand.min)} to {Fmt(hand.max)}");

                float lift = anchor.y - hand.max.y;
                report.AppendLine($"          sits {lift * 1000.0f:F1} mm above the top of the hand "
                                  + $"(x {anchor.x:F4}, z {anchor.z:F4})");

                // ⚠️ THE TOLERANCES ARE THE FAILURES THEY DESCRIBE. Inside the hand box and the
                // shoe phases through the arm, which is what +0.0400 produced on the Godot side:
                // *"its almost on the arm, js phasing a bit thru it"*. Far above it and the shoe
                // hovers. Off the hand in X or Z and it is beside the character entirely.
                if (lift < -0.010f || lift > 0.060f)
                {
                    report.AppendLine("FAIL: the shoe would be buried in the hand or floating "
                                      + "above it.");
                    ok = false;
                }

                if (anchor.x < hand.min.x - 0.02f || anchor.x > hand.max.x + 0.02f ||
                    anchor.z < hand.min.z - 0.02f || anchor.z > hand.max.z + 0.02f)
                {
                    report.AppendLine("FAIL: the anchor is not over the hand.");
                    ok = false;
                }
            }

            return ok;
        }

        private static string Fmt(Vector3 v) => $"({v.x:F4}, {v.y:F4}, {v.z:F4})";

        /// <summary>
        /// The hand blob in the bone's own space: the far eighth of what the limb owns.
        ///
        /// ⚠️ IT MIRRORS `CharacterVisual.PalmCentre`'s SELECTION DELIBERATELY, down to the axis
        /// being chosen from the spread rather than assumed. The caller asserts that the palm
        /// that function returns falls inside the bounds this one produces, so if either drifts
        /// the probe fails instead of silently measuring a different part of the arm.
        /// </summary>
        private static Bounds HandBounds(SkinnedMeshRenderer skinned, int bone)
        {
            var mesh = skinned.sharedMesh;
            var weights = mesh.boneWeights;
            var vertices = mesh.vertices;
            var binds = mesh.bindposes;

            var local = new List<Vector3>();

            for (int i = 0; i < vertices.Length; i++)
            {
                var w = weights[i];

                float weight = (w.boneIndex0 == bone ? w.weight0 : 0.0f)
                             + (w.boneIndex1 == bone ? w.weight1 : 0.0f)
                             + (w.boneIndex2 == bone ? w.weight2 : 0.0f)
                             + (w.boneIndex3 == bone ? w.weight3 : 0.0f);

                if (weight < 0.5f) continue;

                local.Add(binds[bone].MultiplyPoint3x4(vertices[i]));
            }

            if (local.Count == 0) return default;

            Vector3 min = local[0], max = local[0];
            foreach (var v in local) { min = Vector3.Min(min, v); max = Vector3.Max(max, v); }

            Vector3 size = max - min;
            int axis = size.x >= size.y && size.x >= size.z ? 0 : (size.y >= size.z ? 1 : 2);

            bool towardMax = Mathf.Abs(max[axis]) > Mathf.Abs(min[axis]);
            float cut = towardMax ? max[axis] - size[axis] * 0.125f : min[axis] + size[axis] * 0.125f;

            bool any = false;
            Bounds bounds = default;

            foreach (var v in local)
            {
                if (towardMax ? v[axis] < cut : v[axis] > cut) continue;

                if (!any) { bounds = new Bounds(v, Vector3.zero); any = true; }
                else bounds.Encapsulate(v);
            }

            return bounds;
        }

        private static int WeightedVertexCount(SkinnedMeshRenderer[] skinned, string boneName)
        {
            int total = 0;

            foreach (var s in skinned)
            {
                if (s.sharedMesh == null || s.bones == null) continue;

                int index = System.Array.FindIndex(s.bones, b => b != null && b.name == boneName);
                if (index < 0) continue;

                foreach (var w in s.sharedMesh.boneWeights)
                {
                    float weight = (w.boneIndex0 == index ? w.weight0 : 0.0f)
                                 + (w.boneIndex1 == index ? w.weight1 : 0.0f)
                                 + (w.boneIndex2 == index ? w.weight2 : 0.0f)
                                 + (w.boneIndex3 == index ? w.weight3 : 0.0f);

                    if (weight >= 0.5f) total++;
                }
            }

            return total;
        }

        /// <summary>
        /// ⚠️⚠️ THE BOUND IS THE CAST'S RANGE, NOT THE BASE RIG'S ONE NUMBER. This compared the
        /// replacement to `OldModel` and failed at more than 5 mm, on the reasoning that
        /// `CharacterVisual.PersonScale` is a single 2.38 for all twelve so a taller rig "is
        /// simply the wrong size".
        ///
        /// The constant is real and the conclusion from it was not. Measured across the twelve
        /// CC0 rigs the port ships, model AABB height runs 0.6613 (male-b) to 0.7928 (male-c),
        /// a spread of 132 mm, and every one of them takes the same 2.38.
        /// `CharacterVisual.AlignToCapsuleFloor` re-measures the SCALED bounds and drops the feet
        /// onto the capsule floor, so a taller rig stands taller with its feet in the right place.
        /// 0.7234 was one member of that range.
        ///
        /// ⚠️ WHAT IT COST. The difference between the two ends of that range is HAIR: a bald rig
        /// is 0.66 and a rig with a mop is 0.78. Holding this character at the base rig's 0.7234
        /// while its donated skull already reached 0.7218 left under 2 mm for hair, and four
        /// passes of hand-built cap went into the gap that was not there. 🧑 on the last of them:
        /// *"yea hair doesnt loom good still"*. `build_person_voxel.py` carries the same table
        /// and the same correction beside its own copy of this check.
        /// </summary>
        private const float CastMinHeight = 0.6613f;

        private const float CastMaxHeight = 0.7928f;

        private static bool CheckHeight(StringBuilder report, GameObject instance)
        {
            var old = AssetDatabase.LoadAssetAtPath<GameObject>(OldModel);
            if (old == null) return true;

            var oldInstance = Object.Instantiate(old);

            float newHeight = MeasureHeight(instance);
            float oldHeight = MeasureHeight(oldInstance);

            Object.DestroyImmediate(oldInstance);

            report.AppendLine($"authored height: {newHeight:F4} (base {oldHeight:F4}, cast "
                              + $"{CastMinHeight:F4} to {CastMaxHeight:F4}), "
                              + $"scaled by {CharacterVisual.PersonScale} -> "
                              + $"{newHeight * CharacterVisual.PersonScale:F3}");

            // Widened by 5 mm at each end so a rig at either extreme can be matched exactly.
            if (newHeight >= CastMinHeight - 0.005f && newHeight <= CastMaxHeight + 0.005f)
            {
                return true;
            }

            report.AppendLine("FAIL: authored height is outside the range the twelve CC0 rigs "
                              + "span, so PersonScale's one constant cannot serve it.");
            return false;
        }

        private static float MeasureHeight(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return 0.0f;

            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            return bounds.size.y;
        }

        /// <summary>
        /// ⚠️⚠️ EVERY UV MUST LAND IN A ROW THE PALETTE OWNS, AND WHICH ROWS THOSE ARE IS
        /// MEASURED OFF THE WORKING RIG RATHER THAN ASSUMED. `Toon.shader` remaps rows 8 and up
        /// and falls THROUGH for rows 0 to 7, deliberately, so a stray model degrades to Kenney's
        /// colours instead of to black. That graceful fallback is exactly what makes a
        /// mis-authored UV invisible.
        ///
        /// ⚠️ AND THE ROW IS NOT THE ONE IN THE FILE. glTF puts its UV origin at the TOP left and
        /// Unity puts it at the bottom, so glTFast flips V on import and a cell authored in row 9
        /// of the .glb arrives in row 6 of the mesh. The first build of this model authored rows
        /// straight out of the shader's own comment, which describes the UNITY side, and every
        /// vertex landed in the fall-through band: the character imported, animated, and wore
        /// stock Kenney colours. The base rig is the reference for the convention because it is
        /// the one that demonstrably works.
        /// </summary>
        private static bool CheckPaletteRows(StringBuilder report, SkinnedMeshRenderer[] skinned)
        {
            var mine = Rows(skinned.Select(s => s == null ? null : s.sharedMesh).ToArray());
            var theirs = Rows(BaseMeshes());

            report.AppendLine($"atlas rows, new rig:  {string.Join(", ", mine.Keys)}");
            report.AppendLine($"atlas rows, base rig: {string.Join(", ", theirs.Keys)}");

            var slots = new SortedSet<int>();
            int stray = 0;

            foreach (var pair in mine)
            {
                if (pair.Key > 7) { stray += pair.Value.Item1; continue; }
                foreach (int slot in pair.Value.Item2) slots.Add(slot);
            }

            report.AppendLine($"palette slots used: {string.Join(", ", slots)}");

            if (stray > 0)
            {
                report.AppendLine($"FAIL: {stray} vertices sit in Unity atlas rows 8-15, which "
                                  + "the palette does not reach. They render in stock colours.");
                report.AppendLine("      glTFast flips V: a cell authored in .glb row r arrives "
                                  + "in Unity row 15 - r.");
                return false;
            }

            if (!slots.Contains(8))
            {
                report.AppendLine("FAIL: nothing uses slot 8. The face is drawn in it.");
                return false;
            }

            return true;
        }

        /// <summary>Vertex count and palette slots per atlas ROW, as Unity sees them.</summary>
        private static SortedDictionary<int, (int, SortedSet<int>)> Rows(Mesh[] meshes)
        {
            var rows = new SortedDictionary<int, (int, SortedSet<int>)>();

            foreach (var mesh in meshes)
            {
                if (mesh == null) continue;

                foreach (var uv in mesh.uv)
                {
                    int col = Mathf.Clamp(Mathf.FloorToInt(uv.x * 16.0f), 0, 15);
                    int row = Mathf.Clamp(Mathf.FloorToInt(uv.y * 16.0f), 0, 15);

                    if (!rows.TryGetValue(row, out var bucket))
                        bucket = (0, new SortedSet<int>());

                    // Same formula `Toon.shader` runs, in the same (Unity, V-flipped) frame.
                    bucket.Item1++;
                    if (row <= 7) bucket.Item2.Add((col / 2) + (row <= 3 ? 8 : 0));

                    rows[row] = bucket;
                }
            }

            return rows;
        }

        /// <summary>
        /// The base rig's meshes, read off the imported ASSET rather than off an instance.
        ///
        /// ⚠️ THE MESHES ARE SHARED SUB-ASSETS AND OUTLIVE ANY GameObject, which is why this can
        /// hand them back where handing back the renderers could not: destroying the temporary
        /// instance takes its components with it and every one of them reads as null a line later.
        /// </summary>
        private static Mesh[] BaseMeshes()
            => AssetDatabase.LoadAllAssetsAtPath(OldModel).OfType<Mesh>().ToArray();

        // -------------------------------------------------------------------

        private static bool CheckRoster(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("-- roster");

            var book = AssetDatabase.LoadAssetAtPath<RosterBook>(
                "Assets/TumbangPreso/Resources/RosterBook.asset");

            if (book == null)
            {
                report.AppendLine("FAIL: no RosterBook. Run RosterBookBuilder.Build first.");
                return false;
            }

            var entry = book.People.FirstOrDefault(p => p != null && p.Id == RosterId);

            if (entry == null)
            {
                report.AppendLine($"FAIL: no roster entry '{RosterId}'.");
                return false;
            }

            string modelPath = entry.Model == null ? "<none>" : AssetDatabase.GetAssetPath(entry.Model);
            report.AppendLine($"{RosterId} -> {modelPath}");
            report.AppendLine($"clips referenced: {(entry.Clips == null ? 0 : entry.Clips.Length)}");
            report.AppendLine($"palette entries: {(entry.Palette == null ? 0 : entry.Palette.Length)}");

            bool ok = true;

            if (modelPath != NewModel)
            {
                report.AppendLine("FAIL: the roster is still pointing at the old mesh.");
                ok = false;
            }

            // See RosterEntryAsset.Clips: an asset nothing references is stripped from the
            // player, and the whole cast stood still in every build because of it.
            if (entry.Clips == null || entry.Clips.Length == 0)
            {
                report.AppendLine("FAIL: no clips referenced, so they will not ship.");
                ok = false;
            }

            if (entry.Palette == null || entry.Palette.Length != 16)
            {
                report.AppendLine("FAIL: the palette is not sixteen colours.");
                ok = false;
            }
            else
            {
                // The one hard constraint, asserted here as well as in the generator because
                // this is the side that ships.
                Color face = entry.Palette[8];
                float lum = 0.2126f * face.r + 0.7152f * face.g + 0.0722f * face.b;
                report.AppendLine($"slot 8 luminance: {lum:F3}");

                if (lum > 0.30f)
                {
                    report.AppendLine("FAIL: slot 8 is too light. The face vanishes into the skin.");
                    ok = false;
                }
            }

            return ok;
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️⚠️ THE CLIPS EXISTING IS NOT THE CLIPS WORKING, and that distinction is the whole
        /// reason this method is not just a name check. A clip addresses transforms by PATH from
        /// the root, so a rebuilt model whose hierarchy differs by one node name imports 32
        /// perfectly valid clips that move nothing at all. Nothing errors: the character stands
        /// in its bind pose, which reads as an unfinished animation layer rather than as a broken
        /// one.
        ///
        /// So each clip is SAMPLED and the bones are measured before and after. A clip that
        /// leaves every bone where it found it is reported as dead.
        /// </summary>
        private static bool CheckAnimationBinds(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("-- animation binds");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NewModel);
            if (prefab == null) return false;

            var instance = Object.Instantiate(prefab);
            var clips = AssetDatabase.LoadAllAssetsAtPath(NewModel)
                                     .OfType<AnimationClip>()
                                     .Where(c => !c.name.StartsWith("__preview"))
                                     .OrderBy(c => c.name)
                                     .ToList();

            var bones = instance.GetComponentsInChildren<Transform>(true)
                                .Where(t => t.name is "root" or "torso" or "head"
                                            or "arm-left" or "arm-right"
                                            or "leg-left" or "leg-right")
                                .ToList();

            var rest = bones.Select(b => b.localRotation).ToList();

            int moved = 0;
            var dead = new List<string>();

            foreach (var clip in clips)
            {
                float most = 0.0f;

                // Two samples, because a clip whose first frame IS the rest pose would read as
                // dead off t=0 alone. A third of the way in is past every wind-up in this set.
                foreach (float t in new[] { clip.length * 0.34f, clip.length * 0.67f })
                {
                    clip.SampleAnimation(instance, t);

                    for (int i = 0; i < bones.Count; i++)
                        most = Mathf.Max(most, Quaternion.Angle(rest[i], bones[i].localRotation));
                }

                if (most > 1.0f) moved++;
                else dead.Add(clip.name);

                if (Poses.Contains(clip.name))
                    report.AppendLine($"   {clip.name,-22} {clip.length:F2}s  max bone delta {most:F1} deg");
            }

            report.AppendLine($"clips that move at least one bone: {moved}/{clips.Count}");

            Object.DestroyImmediate(instance);

            // ⚠️ `static` IS ALLOWED TO BE STILL AND IS THE ONLY ONE THAT IS. It is the bind pose
            // by definition, which is also why it is the control column in the photograph.
            var wrong = dead.Where(n => n != "static").ToList();

            if (wrong.Count == 0) return true;

            report.AppendLine($"FAIL: {wrong.Count} clips move nothing: {string.Join(", ", wrong)}");
            report.AppendLine("      The clips imported but are not addressing this hierarchy.");
            return false;
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// Every emote on the wheel resolves to a clip this rig has, and that clip moves it.
        ///
        /// ⚠️⚠️ THE EMOTE WHEEL IS THE PART OF THE ANIMATION LAYER MOST LIKELY TO BREAK QUIETLY
        /// ON A SWAP, because it is the only one that resolves through a FALLBACK CHAIN. A
        /// missing locomotion clip is obvious the first time somebody walks; a missing `emote-no`
        /// silently drops to `interact-left`, and a rig with neither plays nothing at all and
        /// logs nothing, so the wheel opens, the pick registers, the emote replicates to every
        /// peer, and the body does not move. 🧑 asked directly: *"like can it do the emotes"*.
        ///
        /// ⚠️ AND THE CLIP HAS TO MOVE THE RIG, not merely exist. Same distinction
        /// `CheckAnimationBinds` makes and for the same reason: a clip addresses transforms by
        /// path, so one that no longer matches the hierarchy is valid, present, and inert.
        /// </summary>
        private static bool CheckEmotes(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("-- emotes");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NewModel);
            if (prefab == null) return false;

            var instance = Object.Instantiate(prefab);

            var clips = AssetDatabase.LoadAllAssetsAtPath(NewModel)
                                     .OfType<AnimationClip>()
                                     .Where(c => !c.name.StartsWith("__preview"))
                                     .ToDictionary(c => c.name, c => c);

            var bones = instance.GetComponentsInChildren<Transform>(true)
                                .Where(t => t.name is "root" or "torso" or "head"
                                            or "arm-left" or "arm-right"
                                            or "leg-left" or "leg-right")
                                .ToList();

            var rest = bones.Select(b => b.localRotation).ToList();
            bool ok = true;

            foreach (var (emote, chain) in EmoteChains)
            {
                string resolved = chain.FirstOrDefault(clips.ContainsKey);

                if (resolved == null)
                {
                    report.AppendLine($"FAIL: '{emote}' resolves to nothing. Chain was "
                                      + string.Join(", ", chain));
                    ok = false;
                    continue;
                }

                var clip = clips[resolved];
                float most = 0.0f;

                foreach (float t in new[] { clip.length * 0.34f, clip.length * 0.67f })
                {
                    clip.SampleAnimation(instance, t);

                    for (int i = 0; i < bones.Count; i++)
                        most = Mathf.Max(most, Quaternion.Angle(rest[i], bones[i].localRotation));
                }

                // ⚠️ `tpose` RESOLVES TO `static`, WHICH IS THE BIND POSE AND IS SUPPOSED TO BE
                // STILL. That is the whole joke the emote is making, so it is the one exemption.
                bool still = most <= 1.0f;

                report.AppendLine($"   {emote,-7} -> {resolved,-16} {clip.length:F2}s  "
                                  + $"max bone delta {most:F1} deg"
                                  + (resolved != chain[0] ? "   (fallback)" : string.Empty));

                if (!still || emote == "tpose") continue;

                report.AppendLine($"FAIL: '{emote}' plays {resolved}, which moves nothing.");
                ok = false;
            }

            Object.DestroyImmediate(instance);
            return ok;
        }

        /// <summary>
        /// The picture: the new rig in each pose, wearing its real palette, over the old rig in
        /// the same poses for comparison.
        ///
        /// ⚠️ THE OLD ROW IS THE POINT OF THE SHEET. "Does the new one look right" is not a
        /// question a single column answers; every judgement about proportion, silhouette and
        /// how much the outline eats is relative to the thing it replaces.
        /// </summary>
        private static bool Shoot(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("-- capture");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLight();

            var palette = PaletteFor(RosterId);
            report.AppendLine(palette == null ? "no palette found, shooting stock"
                                              : "shooting with the roster palette");

            for (int i = 0; i < Poses.Length; i++)
            {
                Place(NewModel, palette, Poses[i], i, 0);
                Place(OldModel, null, Poses[i], i, 1);
            }

            var camera = BuildCamera(Poses.Length, 2);

            bool ok = Capture(camera, Poses.Length * CellPixels, 2 * CellPixels);
            report.AppendLine(ok ? $"wrote {ShotPath}" : "FAIL: capture wrote nothing.");

            EditorSceneManager.CloseScene(scene, true);
            return ok;
        }

        /// <summary>
        /// The same four angles the reference art was rendered from, in the bind pose.
        ///
        /// ⚠️⚠️ THE POSE SHEET CANNOT ANSWER "DOES IT LOOK LIKE THE REFERENCE" AND THIS CAN.
        /// Every cell of the other sheet is the same three-quarter angle, so a back that is
        /// missing its hair mass, a bow that is on the wrong side, or a silhouette that only
        /// works from one direction all survive it. The reference is a turnaround, so the check
        /// against it has to be one too, at matching angles, or the comparison is being done
        /// from memory.
        ///
        /// ⚠️ AND IT IS THE BIND POSE ON PURPOSE. A clip moves the limbs between angles and
        /// makes two cells disagree for reasons that have nothing to do with the model.
        /// </summary>
        private static bool ShootTurnaround(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("-- turnaround");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLight();

            var palette = PaletteFor(RosterId);
            var angles = new[] { ("front", 0.0f), ("three-quarter", 40.0f),
                                 ("side", 90.0f), ("back", 180.0f) };

            for (int i = 0; i < angles.Length; i++)
                PlaceTurn(angles[i].Item1, angles[i].Item2, palette, i);

            var camera = BuildCamera(angles.Length, 1);
            bool ok = CaptureTo(camera, angles.Length * CellPixels, CellPixels, TurnPath);

            report.AppendLine(ok ? $"wrote {TurnPath}" : "FAIL: turnaround wrote nothing.");

            EditorSceneManager.CloseScene(scene, true);
            return ok;
        }

        private static void PlaceTurn(string label, float yaw, Color[] palette, int col)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NewModel);
            if (prefab == null) return;

            var pivot = new GameObject($"turn-{col}");
            pivot.transform.position = new Vector3(col, 0.0f, 0.0f);

            var model = Object.Instantiate(prefab, pivot.transform);

            // ⚠️ THE 180 IS `CharacterVisual.PersonModelYaw` AND THE REST IS THE TURN. Shooting
            // the raw import photographs the back of the head and calls it the front.
            model.transform.localRotation =
                Quaternion.Euler(0.0f, CharacterVisual.PersonModelYaw + yaw, 0.0f);

            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, palette);

            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            float extent = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
            if (extent < 0.0001f) return;

            model.transform.localScale = Vector3.one * (0.76f / (extent * 2.0f));

            bounds = model.GetComponentsInChildren<Renderer>()[0].bounds;
            foreach (var r in model.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);

            model.transform.position += pivot.transform.position - bounds.center;

            Caption(pivot.transform, label);
        }

        private const string TurnPath = "Logs/person-swap-turnaround.png";

        private static Color[] PaletteFor(string id)
        {
            var book = AssetDatabase.LoadAssetAtPath<RosterBook>(
                "Assets/TumbangPreso/Resources/RosterBook.asset");

            var entry = book == null ? null : book.People.FirstOrDefault(p => p != null && p.Id == id);

            return entry != null && entry.Palette != null && entry.Palette.Length == 16
                ? entry.Palette : null;
        }

        private static void Place(string path, Color[] palette, string pose, int col, int row)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            var pivot = new GameObject($"cell-{row}-{col}");
            pivot.transform.position = new Vector3(col, -row, 0.0f);

            var model = Object.Instantiate(prefab, pivot.transform);

            // ⚠️ THE SAME 180 DEGREES `CharacterVisual` APPLIES, because the rig wears its face
            // on -Z. Shooting the raw import photographs the back of every head.
            model.transform.localRotation = Quaternion.Euler(0.0f, CharacterVisual.PersonModelYaw + 24.0f, 0.0f);

            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, palette);

            foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (!(sub is AnimationClip clip) || clip.name != pose) continue;

                clip.SampleAnimation(model, clip.length * 0.34f);
                break;
            }

            // ⚠️ THE SLIPPER IS IN THE PICTURE FOR THE POSES THAT CARRY ONE, because "the anchor
            // is 6 mm above the top of the hand" is a number, and whether a shoe looks held is
            // not. It is hung the same way `CharacterVisual.BuildHandAnchor` hangs it: a child of
            // the arm bone at the measured palm, AFTER the pose is sampled, so it rides the bone
            // wherever the clip put it.
            if (pose is "holding-right-shoot" or "holding-right") HangSlipper(model);

            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            float extent = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
            if (extent < 0.0001f) return;

            model.transform.localScale = Vector3.one * (0.76f / (extent * 2.0f));

            bounds = model.GetComponentsInChildren<Renderer>()[0].bounds;
            foreach (var r in model.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);

            model.transform.position += pivot.transform.position - bounds.center;

            // ⚠️ THE ROWS ARE LABELLED AS WHOSE THEY ARE, NOT JUST WHICH POSE. 🧑 read the first
            // sheet as one character and asked why it looked nothing like the reference: the
            // bottom row is the CC0 rig being retired, and "was: idle" was not enough to say so.
            Caption(pivot.transform, (row == 0 ? "NEW  " : "OLD (replaced)  ") + pose);
        }

        /// <summary>
        /// Parks the roster's default tsinelas on the hand, exactly where the game parks it.
        ///
        /// ⚠️ ENTRY 0 OF THE SLIPPER LIST, WHICH IS THE NEUTRAL ONE. It is what an unpicked prop
        /// wears, so it is the honest thing to photograph; picking a different row here would be
        /// showing a shoe most matches never see.
        /// </summary>
        private static void HangSlipper(GameObject model)
        {
            var skinned = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (string arm in new[] { "arm-right", "arm-left" })
            {
                var target = skinned.FirstOrDefault(
                    s => s != null && s.bones != null &&
                         s.bones.Any(b => b != null && b.name == arm));

                if (target == null) continue;

                int index = System.Array.FindIndex(target.bones, b => b != null && b.name == arm);
                if (!CharacterVisual.PalmCentre(target, index, out Vector3 palm)) continue;

                var book = AssetDatabase.LoadAssetAtPath<RosterBook>(
                    "Assets/TumbangPreso/Resources/RosterBook.asset");

                var entry = book == null || book.Slippers.Count == 0 ? null : book.Slippers[0];
                if (entry == null || entry.Model == null) return;

                var shoe = Object.Instantiate(entry.Model, target.bones[index]);
                shoe.transform.localPosition = palm + Vector3.up * CharacterVisual.HandTopLift;
                shoe.transform.localRotation = Quaternion.identity;

                ToonSkin.Apply(shoe, ToonSkin.PropOutlineWidth);
                return;
            }
        }

        private static void Caption(Transform parent, string text)
        {
            var go = new GameObject("caption");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0.0f, -0.44f, -0.5f);
            go.transform.localScale = Vector3.one * 0.012f;

            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 48;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(0.85f, 0.87f, 0.92f);
        }

        private static void BuildLight()
        {
            var go = new GameObject("Key");
            var light = go.AddComponent<Light>();

            light.type = LightType.Directional;
            light.intensity = 0.85f;
            light.color = new Color(1.0f, 0.97f, 0.9f);
            go.transform.rotation = Quaternion.Euler(38.0f, -40.0f, 0.0f);

            // Same numbers, and the same reason, as ModelSheet.BuildLight: there is no
            // ColourGrade lifecycle in an editor scene, so the arena's own ambient clips.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.58f, 0.52f) * 0.78f;
            RenderSettings.fog = false;
        }

        private static Camera BuildCamera(int cols, int rows)
        {
            var go = new GameObject("Probe Camera");
            var camera = go.AddComponent<Camera>();

            camera.orthographic = true;
            camera.orthographicSize = rows * 0.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.17f, 0.22f, 1.0f);

            go.transform.position = new Vector3((cols - 1) * 0.5f, -(rows - 1) * 0.5f, -20.0f);
            go.transform.rotation = Quaternion.identity;

            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 60.0f;

            camera.gameObject.AddComponent<ColourGrade>().Set(1.0f, 1.03f, 1.18f, 0.92f, 1.9f);

            return camera;
        }

        private static bool Capture(Camera camera, int width, int height)
            => CaptureTo(camera, width, height, ShotPath);

        private static bool CaptureTo(Camera camera, int width, int height, string path)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4,
            };

            camera.targetTexture = rt;
            camera.Render();

            RenderTexture.active = rt;

            var shot = new Texture2D(width, height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            shot.Apply();

            RenderTexture.active = null;
            camera.targetTexture = null;

            Directory.CreateDirectory("Logs");
            File.WriteAllBytes(path, shot.EncodeToPNG());

            Object.DestroyImmediate(shot);
            rt.Release();
            Object.DestroyImmediate(rt);

            return File.Exists(path) && new FileInfo(path).Length > 0;
        }
    }
}
