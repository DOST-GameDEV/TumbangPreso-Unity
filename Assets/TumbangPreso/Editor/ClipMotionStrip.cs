using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Photographs a clip ACROSS ITS LENGTH, which nothing in this repository could do before.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE EVERY CHARACTER PROBE HERE SAMPLES FRAME ZERO.
    /// `HeroTurnaroundProbe` and `PersonSwapProbe` both pose the rig with
    /// `clip.SampleAnimation(model, 0.0f)`, so what comes out is the first frame and nothing
    /// else. That is exactly right for *"is this the right character, at the right scale,
    /// wearing the right thing"* and it says nothing at all about motion, which is the only
    /// thing an animation is. `docs/TODO.md` § 151.16 is the entry, and the consequence it
    /// records is that `CLAUDE.md` § 6.1's *"show, do not describe"* could not be obeyed for an
    /// animation: an animator could attach a picture of the bind pose and nobody could tell
    /// from it whether the clip was any good.
    ///
    /// ⚠️⚠️ THE FLOOR IS DRAWN AND THE NUMBERS ARE PRINTED, BECAUSE A STRIP OF PRETTY POSES IS
    /// NOT EVIDENCE. The four faults an animation review has to be able to catch here are floor
    /// penetration, a hand that never reaches the shoe, a recovery that does not return, and a
    /// snap between two poses the strip did not happen to sample. A picture answers the first
    /// two only if the floor is visible in it, and it cannot answer the last two at all, so:
    ///
    ///   * the capture is composited with a FLOOR LINE at world y = 0 and the band below it
    ///     darkened, so anything under the street reads instantly rather than being measured;
    ///   * every column carries its own time, its lowest deformed vertex and its reaching-hand
    ///     height, in metres, under the pose it belongs to;
    ///   * `Logs/motion/<rig>_<clip>.txt` carries a TRACE sampled far finer than the strip
    ///     (`TraceSteps`), so an abrupt snap between two photographed poses still shows up as a
    ///     peak bone speed with a time attached;
    ///   * the same trace answers *"does it come back"* by comparing the last frame against the
    ///     rig's own `idle`, which is the pose the game blends home to.
    ///
    /// ⚠️⚠️ AND THE CLIP IS RESOLVED THROUGH `CharacterAnimator.ActionChains`, NOT LOADED BY
    /// NAME. That chain is a FALLBACK chain: `"slide"` walks past a missing `slide` to
    /// `attack-kick-right` and the character plays the lunge with nothing logged. A probe that
    /// loads `slide` directly out of the `.glb` would photograph a clip the game may not be
    /// playing, which is the most expensive possible way to approve an animation. The report
    /// says which name won and whether it was the first one asked for.
    ///
    /// ⚠️ THE RIG IS PLACED THE WAY `CharacterVisual` PLACES IT: `PersonScale`, `PersonModelYaw`,
    /// `ToonSkin.Apply` with the roster palette, and `AlignToCapsuleFloor`'s own drop computed
    /// from `Renderer.bounds` exactly as the game computes it. A strip shot at some convenient
    /// probe scale would put the penetration numbers in units nobody plays in.
    /// </summary>
    public static class ClipMotionStrip
    {
        private const int CellPixels = 560;

        /// ⚠️ THE TRACE IS SAMPLED FAR FINER THAN THE STRIP ON PURPOSE. Eight photographs of a
        /// 0.95 s clip are 119 ms apart and a snap lives between two of them.
        private const int TraceSteps = 240;

        /// <summary>
        /// The retrieval slide's authored contact beats, from `ASTRA.md` task 3 and
        /// `docs/TODO.md` § 151.16: *"contact at 0.14, 0.25 and 0.342 seconds"*.
        ///
        /// ⚠️ THESE ARE MERGED INTO THE EVEN SAMPLING RATHER THAN REPLACING IT. An evenly
        /// spaced strip is what shows pacing; the beats are what the reach was solved against,
        /// and a strip that misses them is a review of the frames either side of the thing
        /// being reviewed.
        /// </summary>
        private static readonly Dictionary<string, float[]> KeyBeats = new Dictionary<string, float[]>
        {
            { "slide", new[] { 0.140f, 0.250f, 0.342f } },
        };

        private static readonly (string Label, float Yaw)[] AllViews =
        {
            // ⚠️ SIDE IS FIRST AND IS THE ONE THAT ANSWERS THE QUESTION. The rig faces -Z, so a
            // 270 degree yaw sends it travelling to the RIGHT of frame with its RIGHT side, the
            // reaching side, toward the camera. Floor contact, body height and reach all read
            // against the drawn floor line in that one view.
            ("side", 270.0f),
            ("quarter", 220.0f),
        };

        [MenuItem("Tumbang Preso/Probe Sean Retrieval Slide Motion")]
        public static void RunFromMenu() => Execute("sean", "slide", 8, null, null);

        /// <summary>
        /// Batch entry. Every argument is optional and the defaults are § 151.16's subject:
        ///
        ///   -rig sean  -clip slide  -frames 8  -times 0,0.14,0.25  -views side,quarter
        /// </summary>
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();

            string rig = Arg(args, "-rig") ?? "sean";
            string clip = Arg(args, "-clip") ?? "slide";
            string views = Arg(args, "-views");

            int frames = 8;
            string framesArg = Arg(args, "-frames");
            if (!string.IsNullOrEmpty(framesArg)) int.TryParse(framesArg, out frames);

            float[] times = ParseTimes(Arg(args, "-times"));

            EditorApplication.Exit(Execute(rig, clip, frames, times, views) ? 0 : 1);
        }

        public static bool Execute(string rosterId, string clipName, int frames, float[] explicitTimes, string viewList)
        {
            var report = new StringBuilder();
            report.AppendLine("CLIP MOTION STRIP");
            report.AppendLine($"rig:  {rosterId}");
            report.AppendLine($"clip: {clipName}");
            report.AppendLine();

            var entry = FindEntry(rosterId);
            if (entry == null || entry.Model == null)
            {
                report.AppendLine($"FAIL: no roster entry or model for '{rosterId}'.");
                return Finish(rosterId, clipName, report, false);
            }

            string modelPath = AssetDatabase.GetAssetPath(entry.Model);
            report.AppendLine($"model: {modelPath}");

            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // ⚠️⚠️ THE CLIPS THE GAME ACTUALLY HANDS THE ANIMATOR, WHICH ARE THE ROSTER ENTRY'S
            // SERIALISED ONES. `CharacterVisual.ApplyModel` passes `entry.Clips` through to
            // `CharacterAnimator.CacheClips`, and the note on that method records why: the clips
            // are sub-assets of the `.glb`, nothing else references them, and a rig whose
            // references were not serialised is stripped from a build and never moves. Reading
            // the `.glb` instead would photograph a clip that may not ship.
            var supplied = (entry.Clips ?? Array.Empty<AnimationClip>())
                           .Where(c => c != null && !c.name.StartsWith("__preview"))
                           .ToList();

            report.AppendLine($"serialised clips on the roster entry: {supplied.Count}");

            string resolved = ResolveThroughGameChain(clipName, supplied, out string[] chain, out int slot);
            if (resolved == null)
            {
                report.AppendLine($"FAIL: nothing in the '{clipName}' chain resolves on this rig.");
                report.AppendLine($"chain: {(chain == null ? "(no such action)" : string.Join(" -> ", chain))}");
                return Finish(rosterId, clipName, report, false);
            }

            report.AppendLine($"chain: {string.Join(" -> ", chain)}");
            report.AppendLine(slot == 0
                ? $"resolved: '{resolved}' (slot 0, the action's OWN clip, not a fallback)"
                : $"resolved: '{resolved}' (slot {slot}, A FALLBACK. The authored '{chain[0]}' is not on this rig.)");

            var clip = supplied.First(c => c.name == resolved);
            report.AppendLine($"length: {clip.length.ToString("0.000", CultureInfo.InvariantCulture)} s, looping: {clip.isLooping}");
            report.AppendLine();

            float[] times = explicitTimes != null && explicitTimes.Length > 0
                ? explicitTimes.OrderBy(t => t).ToArray()
                : SampleTimes(clip.length, frames, KeyBeats.TryGetValue(clipName, out var beats) ? beats : null);

            report.AppendLine("-- samples");
            report.AppendLine($"times: {string.Join(", ", times.Select(t => t.ToString("0.000", CultureInfo.InvariantCulture)))}");
            report.AppendLine();

            var wanted = SelectViews(viewList);
            bool ok = true;
            var written = new List<string>();

            foreach (var view in wanted)
            {
                string path = NextVersionedPath(rosterId, clipName, view.Label);
                bool shot = ShootStrip(entry, clip, times, view, path, report, out var samples);
                ok &= shot;

                if (shot)
                {
                    written.Add(path);
                    if (view.Label == wanted[0].Label) WriteSampleTable(report, samples);
                }
            }

            report.AppendLine();
            report.AppendLine("-- motion trace (finer than the strip, so a snap between two photographs still shows)");
            ok &= Trace(entry, clip, supplied, report);

            report.AppendLine();
            foreach (string path in written) report.AppendLine($"wrote: {path}");

            return Finish(rosterId, clipName, report, ok);
        }

        // -------------------------------------------------------------------
        // Sampling
        // -------------------------------------------------------------------

        /// <summary>
        /// Where to photograph a clip of this length.
        ///
        /// ⚠️ EVENLY SPACED, INCLUSIVE OF BOTH ENDS, WHICH IS § 151.16'S OWN PRESCRIPTION:
        /// *"sampling at t = i / (n - 1) * clip.length instead of at 0.0f"*. The first frame and
        /// the last frame are the two the review needs most, because they are the entry and the
        /// return to rest.
        ///
        /// ⚠️⚠️ AND A KEY BEAT WINS A COLLISION WITH AN EVEN ONE. Merging naively produces two
        /// columns 4 ms apart that photograph the same pose twice and cost the strip a beat it
        /// does not have room for; dropping the key beat instead would drop the exact frame the
        /// pose was authored against. The window is a fraction of the clip rather than a
        /// constant, for `CLAUDE.md` § 6.2c's reason: a spacing written for a 0.95 s clip is not
        /// a spacing for a 0.33 s one.
        /// </summary>
        public static float[] SampleTimes(float length, int frames, float[] keyBeats)
        {
            if (frames < 2) frames = 2;
            if (length <= 0.0f) return new[] { 0.0f };

            float window = length * 0.05f;
            var chosen = new List<float>();

            if (keyBeats != null)
                foreach (float beat in keyBeats)
                    if (beat >= 0.0f && beat <= length) chosen.Add(beat);

            for (int i = 0; i < frames; i++)
            {
                float t = i / (float)(frames - 1) * length;
                if (chosen.Any(c => Mathf.Abs(c - t) < window)) continue;
                chosen.Add(t);
            }

            chosen.Sort();
            return chosen.ToArray();
        }

        /// <summary>
        /// The name the GAME would play for this action on this rig, and which slot of the
        /// fallback chain it came out of.
        ///
        /// ⚠️⚠️ SLOT 0 IS THE WHOLE POINT. `CharacterAnimator`'s chains exist so an authored clip
        /// drops in by name with no code change, and the failure mode they buy is silence: a rig
        /// missing `slide` plays `attack-kick-right`, looks like a dash, and logs nothing.
        /// A review of the wrong clip is worse than no review.
        /// </summary>
        public static string ResolveThroughGameChain(string action, IEnumerable<AnimationClip> supplied,
                                                     out string[] chain, out int slot)
        {
            chain = null;
            slot = -1;

            if (!CharacterAnimator.ActionChains.TryGetValue(action, out var names)) return null;

            chain = names;
            var have = new HashSet<string>(supplied.Where(c => c != null).Select(c => c.name));

            for (int i = 0; i < names.Length; i++)
            {
                if (!have.Contains(names[i])) continue;

                slot = i;
                return names[i];
            }

            return null;
        }

        // -------------------------------------------------------------------
        // Capture
        // -------------------------------------------------------------------

        private struct Sample
        {
            public float Time;
            public float LowestVertex;
            public float ReachingHand;
        }

        private static bool ShootStrip(RosterEntryAsset entry, AnimationClip clip, float[] times,
                                       (string Label, float Yaw) view, string outPath,
                                       StringBuilder report, out List<Sample> samples)
        {
            samples = new List<Sample>();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLight();

            // Cell 0 first, unposed, so the standing height and the floor drop are measured once
            // and every other cell is placed against the same numbers.
            var probe = Place(entry, view.Yaw, 0.0f);
            if (probe == null)
            {
                EditorSceneManager.CloseScene(scene, true);
                report.AppendLine($"FAIL [{view.Label}]: the model instanced with no renderers.");
                return false;
            }

            var box = WorldBox(probe);
            float drop = box.min.y;
            probe.transform.position -= new Vector3(0.0f, drop, 0.0f);

            float height = box.size.y;
            Calibrate(probe, height, report, view.Label);
            UnityEngine.Object.DestroyImmediate(probe);

            if (height <= 0.001f)
            {
                EditorSceneManager.CloseScene(scene, true);
                report.AppendLine($"FAIL [{view.Label}]: measured a standing height of {height} m.");
                return false;
            }

            float cellWidth = height * 1.60f;
            // ⚠️ THE HEADROOM IS ABOVE THE BIND-POSE BOX, NOT ABOVE THE TALLEST POSE. A raised
            // leg or a swung arm leaves that box, so 12 per cent put a knee against the top
            // edge on the contact frames.
            float viewTop = height * 1.20f;

            // ⚠️ THE BOTTOM MARGIN IS THE CAPTION BAND AS WELL AS THE PENETRATION HEADROOM. The
            // first strip cut the second line of every caption off, because the margin was sized
            // for the floor band alone. `CLAUDE.md` § 6.2c: what is this size measured against.
            float viewBottom = height * -0.34f;
            float viewHeight = viewTop - viewBottom;

            for (int i = 0; i < times.Length; i++)
            {
                var model = Place(entry, view.Yaw, i * cellWidth);
                if (model == null) continue;

                model.transform.position -= new Vector3(0.0f, drop, 0.0f);
                clip.SampleAnimation(model, times[i]);

                var sample = Measure(model, times[i]);
                samples.Add(sample);

                Caption(model.transform.parent != null ? model.transform.parent : model.transform,
                        $"{sample.Time.ToString("0.000", CultureInfo.InvariantCulture)} s\n" +
                        $"low {Metres(sample.LowestVertex)}\nhand {Metres(sample.ReachingHand)}",
                        i * cellWidth, height * -0.155f, height);
            }

            float stripWidth = times.Length * cellWidth;

            Caption(null,
                    $"{entry.Id.ToUpperInvariant()}  ·  {clip.name}  ·  {view.Label.ToUpperInvariant()}  ·  " +
                    $"{clip.length.ToString("0.00", CultureInfo.InvariantCulture)} s  ·  floor line at y = 0",
                    stripWidth * 0.5f - cellWidth * 0.5f, height * 1.00f, height);

            var camera = BuildCamera(stripWidth, cellWidth, viewBottom, viewHeight);

            int pixelWidth = times.Length * CellPixels;
            int pixelHeight = Mathf.RoundToInt(CellPixels * (viewHeight / cellWidth));

            bool success = CaptureTo(camera, pixelWidth, pixelHeight, outPath,
                                     viewTop, viewHeight, times.Length, height);

            EditorSceneManager.CloseScene(scene, true);

            report.AppendLine($"[{view.Label}] {(success ? "SUCCESS" : "FAIL")}: {times.Length} poses, " +
                              $"{pixelWidth}x{pixelHeight}");
            return success;
        }

        private static GameObject Place(RosterEntryAsset entry, float yaw, float x)
        {
            var pivot = new GameObject($"cell-{x.ToString("0.000", CultureInfo.InvariantCulture)}");
            pivot.transform.position = new Vector3(x, 0.0f, 0.0f);

            var model = UnityEngine.Object.Instantiate(entry.Model, pivot.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0.0f, CharacterVisual.PersonModelYaw + yaw, 0.0f);
            model.transform.localScale = Vector3.one * CharacterVisual.PersonScale;

            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth,
                           entry.Palette != null && entry.Palette.Length == 16 ? entry.Palette : null);

            return model.GetComponentsInChildren<Renderer>().Length == 0 ? null : model;
        }

        /// <summary>
        /// The rig's world box, which is both the game's own floor drop and the framing.
        ///
        /// ⚠️ THE GAME'S FLOOR DROP IS COMPUTED THE GAME'S OWN WAY.
        /// `CharacterVisual.AlignToCapsuleFloor` takes `Renderer.bounds.min.y` against the
        /// capsule base and moves the model root by the difference, ONCE, at spawn. A skinned
        /// renderer's bounds come from the import and do not track the pose, so that offset is a
        /// constant for the life of the character and every penetration measured against it is
        /// a real one. Re-aligning per pose would hide exactly the fault this probe exists for.
        ///
        /// ⚠️⚠️ AND THE CAMERA IS FRAMED OFF THE SAME BOX RATHER THAN OFF A BAKED MESH, BECAUSE
        /// THE FIRST STRIP CAME OUT WITH THE CHARACTER AT A FIFTH OF THE FRAME. The baked height
        /// was 2.38 times the rendered one, which is `CharacterVisual.PersonScale` exactly: the
        /// bake had already been scaled and the world transform scaled it again. `Renderer.bounds`
        /// is what the camera itself sees, so a framing computed from it cannot disagree with the
        /// picture. `Calibrate` below is what stopped the same factor corrupting the numbers.
        /// </summary>
        private static Bounds WorldBox(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            return bounds;
        }

        /// ⚠️⚠️ WHICH SPACE `BakeMesh` HANDS BACK IS DECIDED BY MEASUREMENT, NOT BY READING THE
        /// DOCUMENTATION. The idiom is `BakeMesh(mesh)` then `localToWorldMatrix`, and on this rig
        /// that produced a body 2.38 times too tall, `PersonScale` to three figures, because the
        /// bake had already resolved the bones through the scaled root. **A silently scaled
        /// measurement is the worst possible outcome here**: every metre in the report would have
        /// been wrong by one constant while looking entirely reasonable, and the one number that
        /// would NOT have looked wrong is the floor penetration, because the root sits at the feet
        /// so both candidates put the lowest vertex at zero.
        ///
        /// So the rest pose is baked both ways and the SPAN is compared against the renderer's own
        /// world box, which is the height the camera draws. The report says which won.
        private static bool _bakeCarriesScale;

        private static void Calibrate(GameObject model, float referenceHeight, StringBuilder report, string label)
        {
            float scaled = Span(DeformedVertices(model, carriesScale: true));
            float unscaled = Span(DeformedVertices(model, carriesScale: false));

            _bakeCarriesScale = Mathf.Abs(scaled - referenceHeight) <= Mathf.Abs(unscaled - referenceHeight);

            // ⚠️ AND THE OFFSET IS REPORTED AS WELL AS THE SPAN, because a span can be right while
            // the origin is wrong, and the origin is the floor. At rest the model has already been
            // dropped so its renderer box sits ON y = 0, so this number should read about zero;
            // anything else means every penetration below is measured against the wrong street.
            float restLow = DeformedVertices(model, _bakeCarriesScale).Min(v => v.Position.y);

            report.AppendLine(
                $"[{label}] bake space: {(_bakeCarriesScale ? "already scaled" : "needs the world scale")} " +
                $"(renderer box {referenceHeight.ToString("0.000", CultureInfo.InvariantCulture)} m, " +
                $"baked spans {scaled.ToString("0.000", CultureInfo.InvariantCulture)} and " +
                $"{unscaled.ToString("0.000", CultureInfo.InvariantCulture)}; " +
                $"rest pose sits at {restLow.ToString("0.000", CultureInfo.InvariantCulture)} m against a floor at 0)");
        }

        private static float Span(List<DeformedVertex> verts)
            => verts.Count == 0 ? 0.0f : verts.Max(v => v.Position.y) - verts.Min(v => v.Position.y);

        private static Sample Measure(GameObject model, float time)
        {
            var verts = DeformedVertices(model);

            float lowest = verts.Count == 0 ? 0.0f : verts.Min(v => v.Position.y);

            // ⚠️ THE HAND IS THE VERTICES, NOT THE BONE. `PersonSwapProbe.PalmCentre` already
            // makes this argument for the carried tsinelas: a bone is a point in the middle of a
            // wrist and the thing that has to reach the slipper is the geometry around it.
            // Dominant weight above 0.5 is that probe's own threshold, kept so the two agree.
            var hand = verts.Where(v => v.Arm).ToList();
            float reach = hand.Count == 0 ? float.NaN : hand.Min(v => v.Position.y);

            return new Sample { Time = time, LowestVertex = lowest, ReachingHand = reach };
        }

        private struct DeformedVertex
        {
            public Vector3 Position;
            public bool Arm;
        }

        /// <summary>
        /// The posed mesh in world space.
        ///
        /// ⚠️⚠️ `BakeMesh` IS THE ONLY THING HERE THAT SEES THE POSE. `Renderer.bounds` on a
        /// skinned mesh is the imported bind-pose box moved by the root bone; it does not
        /// change when a clip is sampled, which is why the floor drop above may use it and no
        /// measurement below may. Reading bounds for a penetration number would report zero for
        /// every pose of every clip and look like a clean result.
        ///
        /// ⚠️ The bake is in the renderer's own local space WITHOUT its scale, so the world
        /// transform is `localToWorldMatrix`, which carries `PersonScale`.
        /// </summary>
        private static List<DeformedVertex> DeformedVertices(GameObject model)
            => DeformedVertices(model, _bakeCarriesScale);

        private static List<DeformedVertex> DeformedVertices(GameObject model, bool carriesScale)
        {
            var output = new List<DeformedVertex>();
            var baked = new Mesh();

            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null) continue;

                smr.BakeMesh(baked);

                var vertices = baked.vertices;
                var weights = smr.sharedMesh.boneWeights;
                var bones = smr.bones;
                var matrix = carriesScale
                    ? Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one)
                    : smr.transform.localToWorldMatrix;

                for (int i = 0; i < vertices.Length; i++)
                {
                    bool arm = false;

                    if (weights.Length == vertices.Length && bones != null)
                    {
                        var w = weights[i];
                        int index = w.weight0 > 0.5f ? w.boneIndex0 : -1;

                        if (index >= 0 && index < bones.Length && bones[index] != null)
                            arm = bones[index].name == "arm-right";
                    }

                    output.Add(new DeformedVertex
                    {
                        Position = matrix.MultiplyPoint3x4(vertices[i]),
                        Arm = arm,
                    });
                }
            }

            UnityEngine.Object.DestroyImmediate(baked);
            return output;
        }

        // -------------------------------------------------------------------
        // The trace, which is what the strip cannot show
        // -------------------------------------------------------------------

        private static bool Trace(RosterEntryAsset entry, AnimationClip clip,
                                  List<AnimationClip> supplied, StringBuilder report)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var model = Place(entry, 0.0f, 0.0f);
            if (model == null)
            {
                EditorSceneManager.CloseScene(scene, true);
                report.AppendLine("FAIL: the model instanced with no renderers.");
                return false;
            }

            model.transform.position -= new Vector3(0.0f, WorldBox(model).min.y, 0.0f);

            var bones = model.GetComponentsInChildren<Transform>(true);

            Vector3[] previous = null;
            float peak = 0.0f;
            float peakAt = 0.0f;
            string peakBone = "-";
            float lowestOfAll = float.MaxValue;
            float lowestAt = 0.0f;

            float dt = clip.length / TraceSteps;

            for (int step = 0; step <= TraceSteps; step++)
            {
                float t = step * dt;
                clip.SampleAnimation(model, t);

                var now = bones.Select(b => b.position).ToArray();

                if (previous != null)
                {
                    for (int i = 0; i < now.Length; i++)
                    {
                        float speed = Vector3.Distance(now[i], previous[i]) / dt;
                        if (speed <= peak) continue;

                        peak = speed;
                        peakAt = t;
                        peakBone = bones[i].name;
                    }
                }

                previous = now;

                // The trace also finds the deepest point of the whole clip, which the strip only
                // finds if it happened to photograph it.
                if (step % 8 != 0) continue;

                var posed = DeformedVertices(model);
                if (posed.Count == 0) continue;

                float low = posed.Min(v => v.Position.y);
                if (low >= lowestOfAll) continue;

                lowestOfAll = low;
                lowestAt = t;
            }

            report.AppendLine($"steps: {TraceSteps + 1} across {clip.length.ToString("0.000", CultureInfo.InvariantCulture)} s " +
                              $"({(dt * 1000.0f).ToString("0.0", CultureInfo.InvariantCulture)} ms apart)");
            report.AppendLine($"peak bone speed: {peak.ToString("0.00", CultureInfo.InvariantCulture)} m/s on '{peakBone}' " +
                              $"at {peakAt.ToString("0.000", CultureInfo.InvariantCulture)} s");
            report.AppendLine(lowestOfAll == float.MaxValue
                ? "deepest point of the whole clip: nothing skinned to measure"
                : $"deepest point of the whole clip: {Metres(lowestOfAll)} at " +
                  $"{lowestAt.ToString("0.000", CultureInfo.InvariantCulture)} s");

            // ⚠️ DOES IT COME BACK. The game blends this action out into locomotion, so the pose
            // the last frame leaves behind is compared against the rig's own `idle` first frame:
            // a clip that ends somewhere else is the abrupt return an animation review is
            // supposed to catch, and no single photograph of either end shows it.
            var idle = supplied.FirstOrDefault(c => c.name == "idle");
            if (idle != null)
            {
                clip.SampleAnimation(model, clip.length);
                var ended = bones.Select(b => b.position).ToArray();

                idle.SampleAnimation(model, 0.0f);
                var rest = bones.Select(b => b.position).ToArray();

                float worst = 0.0f;
                string worstBone = "-";
                for (int i = 0; i < ended.Length; i++)
                {
                    float gap = Vector3.Distance(ended[i], rest[i]);
                    if (gap <= worst) continue;

                    worst = gap;
                    worstBone = bones[i].name;
                }

                report.AppendLine(worst <= 0.0f
                    ? "last frame against idle frame 0: 0.000 m, every bone identical. The clip " +
                      "ENDS at the rig's rest pose, so there is nothing to snap back from."
                    : $"last frame against idle frame 0: {Metres(worst)} on '{worstBone}'");
            }
            else
            {
                report.AppendLine("last frame against idle frame 0: no 'idle' clip on this rig to compare with");
            }

            EditorSceneManager.CloseScene(scene, true);
            return true;
        }

        // -------------------------------------------------------------------
        // Scene furniture, transcribed from HeroTurnaroundProbe so the two strips and the
        // turnarounds are lit and graded identically.
        // -------------------------------------------------------------------

        private static void BuildLight()
        {
            var go = new GameObject("Key");
            var light = go.AddComponent<Light>();

            light.type = LightType.Directional;
            light.intensity = 0.85f;
            light.color = new Color(1.0f, 0.97f, 0.9f);
            go.transform.rotation = Quaternion.Euler(38.0f, -40.0f, 0.0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.58f, 0.52f) * 0.78f;
            RenderSettings.fog = false;
        }

        private static Camera BuildCamera(float stripWidth, float cellWidth, float viewBottom, float viewHeight)
        {
            var go = new GameObject("Strip Camera");
            var camera = go.AddComponent<Camera>();

            camera.orthographic = true;
            camera.orthographicSize = viewHeight * 0.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.17f, 0.22f, 1.0f);

            go.transform.position = new Vector3(stripWidth * 0.5f - cellWidth * 0.5f,
                                                viewBottom + viewHeight * 0.5f,
                                                -20.0f);
            go.transform.rotation = Quaternion.identity;

            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 60.0f;

            camera.gameObject.AddComponent<ColourGrade>().Set(1.0f, 1.03f, 1.18f, 0.92f, 1.9f);

            return camera;
        }

        private static void Caption(Transform parent, string text, float x, float y, float height)
        {
            var go = new GameObject("caption");
            if (parent != null) go.transform.SetParent(parent, false);

            go.transform.position = new Vector3(x, y, -0.5f);
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * (height * 0.0125f);

            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 38;
            mesh.lineSpacing = 0.95f;
            mesh.anchor = TextAnchor.UpperCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(0.88f, 0.90f, 0.95f);
        }

        /// <summary>
        /// ⚠️⚠️ THE FLOOR IS COMPOSITED AFTER THE RENDER RATHER THAN BUILT AS GEOMETRY, and that
        /// is a decision rather than a shortcut. A ground plane seen edge on by an orthographic
        /// camera is a zero-thickness line, and thickening it into a slab puts a lit surface in
        /// front of the ankle the reader is trying to look at. Drawn here it is exactly one row
        /// of pixels at world y = 0, with the band beneath it darkened, so a foot through the
        /// street is visible at a glance and still fully legible.
        /// </summary>
        private static bool CaptureTo(Camera camera, int width, int height, string path,
                                      float viewTop, float viewHeight, int cells, float standing)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 8,
            };

            camera.targetTexture = rt;
            camera.Render();

            RenderTexture.active = rt;

            var shot = new Texture2D(width, height, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, width, height), 0, 0);

            // Rows counted DOWN from the top of the image, which is the frame `fromTop` below
            // works in. `viewTop` is the world height of the top edge above y = 0, so the share
            // of the image above the floor is exactly viewTop / viewHeight.
            int floorRow = Mathf.Clamp(Mathf.RoundToInt((viewTop / viewHeight) * height), 0, height - 1);

            int bandRows = Mathf.RoundToInt((standing * 0.12f / viewHeight) * height);

            var pixels = shot.GetPixels();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;

                    // Texture rows run bottom-up; the floor row was computed top-down.
                    int fromTop = height - 1 - y;

                    if (fromTop > floorRow && fromTop <= floorRow + bandRows)
                    {
                        pixels[i] = new Color(pixels[i].r * 0.52f, pixels[i].g * 0.52f, pixels[i].b * 0.55f);
                    }

                    if (fromTop == floorRow || fromTop == floorRow + 1)
                    {
                        pixels[i] = new Color(1.0f, 0.729f, 0.0f);
                    }

                    // Cell separators, quiet enough not to compete with the poses.
                    if (x % (width / Mathf.Max(cells, 1)) == 0 && x > 0)
                    {
                        pixels[i] = Color.Lerp(pixels[i], new Color(0.55f, 0.55f, 0.60f), 0.28f);
                    }
                }
            }

            shot.SetPixels(pixels);
            shot.Apply();

            RenderTexture.active = null;
            camera.targetTexture = null;

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(path, shot.EncodeToPNG());

            UnityEngine.Object.DestroyImmediate(shot);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);

            return File.Exists(path) && new FileInfo(path).Length > 0;
        }

        // -------------------------------------------------------------------

        private static RosterEntryAsset FindEntry(string rosterId)
        {
            var book = AssetDatabase.LoadAssetAtPath<RosterBook>("Assets/TumbangPreso/Resources/RosterBook.asset");
            var entry = book == null ? null : book.People.FirstOrDefault(p => p != null && p.Id == rosterId);

            return entry ?? AssetDatabase.LoadAssetAtPath<RosterEntryAsset>(
                       $"Assets/TumbangPreso/Resources/Roster/person_{rosterId}.asset");
        }

        private static (string Label, float Yaw)[] SelectViews(string list)
        {
            if (string.IsNullOrEmpty(list)) return AllViews;

            var wanted = list.Split(',').Select(s => s.Trim().ToLowerInvariant()).ToList();
            var picked = AllViews.Where(v => wanted.Contains(v.Label)).ToArray();

            return picked.Length == 0 ? AllViews : picked;
        }

        /// ⚠️ `CLAUDE.md` § 6.1: *"version the filename every time"*. Chat clients cache images by
        /// name, so overwriting a strip leaves the previous one on screen and the whole review
        /// is conducted against an image that is no longer on disk. This picks the next free
        /// number rather than trusting anybody to remember.
        private static string NextVersionedPath(string rosterId, string clipName, string view)
        {
            Directory.CreateDirectory("Logs/motion");

            for (int v = 1; v < 1000; v++)
            {
                string path = $"Logs/motion/{rosterId}_{clipName}_{view}_v{v}.png";
                if (!File.Exists(path)) return path;
            }

            return $"Logs/motion/{rosterId}_{clipName}_{view}_v999.png";
        }

        private static void WriteSampleTable(StringBuilder report, List<Sample> samples)
        {
            report.AppendLine();
            report.AppendLine("-- per pose, metres against the floor at y = 0 (negative is through the street)");
            report.AppendLine("     time    lowest vertex    reaching hand");

            foreach (var s in samples)
            {
                report.AppendLine($"   {s.Time.ToString("0.000", CultureInfo.InvariantCulture),6}  " +
                                  $"{Metres(s.LowestVertex),15}  {Metres(s.ReachingHand),15}");
            }
        }

        private static string Metres(float value)
            => float.IsNaN(value) ? "n/a" : value.ToString("0.000", CultureInfo.InvariantCulture) + " m";

        private static bool Finish(string rosterId, string clipName, StringBuilder report, bool ok)
        {
            report.AppendLine();
            report.AppendLine(ok ? "RESULT: PASS" : "RESULT: FAIL");

            Directory.CreateDirectory("Logs/motion");
            File.WriteAllText($"Logs/motion/{rosterId}_{clipName}.txt", report.ToString());
            Debug.Log(report.ToString());

            return ok;
        }

        private static string Arg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];

            return null;
        }

        private static float[] ParseTimes(string list)
        {
            if (string.IsNullOrEmpty(list)) return null;

            return list.Split(',')
                       .Select(s => float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float t) ? t : -1.0f)
                       .Where(t => t >= 0.0f)
                       .ToArray();
        }
    }
}
