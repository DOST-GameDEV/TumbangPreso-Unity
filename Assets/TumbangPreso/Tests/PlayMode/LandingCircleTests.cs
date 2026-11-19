using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    public sealed class LandingCircleTests
    {
        readonly List<GameObject> _built = new List<GameObject>();
        static readonly MethodInfo Flight = typeof(Slipper).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly MethodInfo Ground = typeof(Slipper).GetMethod("FindGroundY", BindingFlags.Static | BindingFlags.NonPublic);
        private int _idleDelay;
        [UnitySetUp] public IEnumerator Before()
        {
#if UNITY_EDITOR
            _idleDelay=UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds;
            UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds=1;
#endif
            yield return PlayModeWorld.Reset();yield return new WaitForSecondsRealtime(1);
        }
        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
#if UNITY_EDITOR
            UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds=_idleDelay;
#endif
        }
        [TearDown] public void Cleanup() { foreach (var go in _built) if (go != null) Object.DestroyImmediate(go); _built.Clear(); }
        GameObject Track(GameObject go) { _built.Add(go); return go; }
        GameObject Slab(string name, Vector3 at, Vector3 scale)
        {
            var go = Track(GameObject.CreatePrimitive(PrimitiveType.Cube)); go.name = name;
            go.transform.position = at; go.transform.localScale = scale; Physics.SyncTransforms(); return go;
        }
        void Equivalent(float ground, float spin, bool wall)
        {
            Slab("FloorTest", new Vector3(0, ground - .5f, 0), new Vector3(30, 1, 30));
            if (wall) Slab("BankWall", new Vector3(0, ground + 2, 2), new Vector3(8, 4, .4f));
            var guide = Track(new GameObject("LandingPreview")).AddComponent<TrajectoryPreview>();
            guide.enabled = false;
            var shoe = Track(new GameObject("FlightComparison")).AddComponent<Slipper>(); shoe.enabled = false;
            Vector3 origin = new Vector3(0, ground + 1, -2), velocity = new Vector3(0, 3, 7);
            Assert.IsTrue(guide.TryPredictLanding(origin, velocity, spin, out var landing));
            using (NetCue.SuppressRelay())
            {
                shoe.HostThrow(null, origin, velocity, SlipperAffinity.Normal, spin);
                for (int i = 0; i < 320 && shoe.State == SlipperState.InFlight; i++) Flight.Invoke(shoe, null);
            }
            Assert.AreEqual(SlipperState.Loose, shoe.State);
            var actual = shoe.transform.position; actual.y = landing.y;
            Assert.Less(Vector3.Distance(actual, landing), .12f, "Preview and actual host flight must agree before actor interception.");
            Assert.That(landing.y, Is.EqualTo(ground + TrajectoryPreview.FloorEpsilon).Within(.001f));
        }

        [Test] public void FlatFlightMatchesThePredictedLanding() => Equivalent(0, 0, false);
        [Test] public void RaisedGroundAndCurveMatchRealFlight() => Equivalent(.6f, .65f, false);
        [Test] public void BankShotMatchesThePredictedLanding() => Equivalent(0, .9f, true);

        [UnityTest] public IEnumerator UphillFlightRemainsReadableAboveTheLaunchHeight()
        {
            Slab("FloorTest", new Vector3(0, -.5f, 0), new Vector3(28, 1, 28));
            Slab("FloorRaised", new Vector3(0, 2.1f, 4), new Vector3(7, 1, 8));
            var guide = Track(new GameObject("UphillGuide")).AddComponent<TrajectoryPreview>();
            guide.enabled = false;
            Vector3 origin = new Vector3(0, 1.15f, -4);
            Assert.IsTrue(guide.TryPredictLanding(origin, new Vector3(0, 11, 7), 0, out var landing));
            Assert.Greater(landing.y, origin.y + 1, "The elevated platform must be the landing surface.");
            var camera = Track(new GameObject("BelowTheLedge")).AddComponent<Camera>();
            camera.enabled = false;
            camera.transform.SetPositionAndRotation(origin - Vector3.forward * .15f, Quaternion.Euler(-14, 0, 0));
            var light = Track(new GameObject("FlightReviewLight")).AddComponent<Light>();
            light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(45, -35, 0);
            typeof(TrajectoryPreview).GetMethod("DrawFlight", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(guide, new object[] { camera, new Color(1, .92f, .58f, .92f), 0f });
            Assert.IsTrue(guide.FlightVisible);
            var mesh = guide.transform.Find("FlightPath").GetComponent<MeshFilter>().sharedMesh;
            Assert.Greater(mesh.bounds.max.y, landing.y + .4f);
            yield return GameplayShots.Render(camera, "Uphill-flight", false, "Logs/tj-cadena-fixes/trajectory");
            guide.enabled = true;
            guide.enabled = false;
            Assert.IsFalse(guide.FlightVisible);
        }

        [Test] public void SupportQueryDoesNotAllocateAfterWarmupAndDenseOverflowKeepsHighestFloor()
        {
            Slab("Support", new Vector3(0, -.5f, 0), new Vector3(20, 1, 20));
            var query = (System.Func<Vector3, float, float>)System.Delegate.CreateDelegate(typeof(System.Func<Vector3, float, float>), Ground);
            for (int i = 0; i < 5; i++) query(Vector3.up, .045f);
            using (var calibrationRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 100, ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            {
                Assert.IsTrue(calibrationRecorder.Valid);
                var calibration = new byte[4096]; System.GC.KeepAlive(calibration); calibrationRecorder.Stop();
                Assert.Greater(calibrationRecorder.Count, 0);
            }
            float total = 0; int allocations;
            using (var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 256, ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            {
                for (int i = 0; i < 100; i++) total += query(Vector3.up, .045f);
                recorder.Stop(); allocations = recorder.Count;
            }
            Assert.AreEqual(0, total, .001f); Assert.AreEqual(0, allocations, "Normal support queries should reuse their hit storage.");
            for (int i = 0; i < 70; i++) Slab("DenseSupport" + i, new Vector3(0, .1f + i * .02f, 0), new Vector3(2, .01f, 2));
            Assert.That(query(Vector3.up * 3, .045f), Is.EqualTo(1.485f).Within(.001f), "A full nonalloc buffer must not silently lose the highest support.");
        }

        [UnityTest, Timeout(180000)] public IEnumerator LocalChargeShowsTheArcAndLandingUntilRelease()
        {
            yield return MapRetrievalProbe.Load(UI.SceneFlow.Eskinita);
            var ready = Object.FindFirstObjectByType<ReadyGate>();
            ready.enabled = true; ready.StartLocalCountdown();
            yield return new WaitForSeconds(ReadyGate.HudRevealSeconds + 3.6f);
            Assert.IsFalse(ready.AwaitingReady || ready.CountingDown);
            yield return new WaitForSeconds(GameServices.Round.Lata.ProtectionLeft+.05f);
            var who = GameServices.Round.PlayerAt(1); var carrier = who.GetComponent<Carrier>();
            var rig = Object.FindFirstObjectByType<CameraSystem.CameraRig>();
            foreach (var brain in Object.FindObjectsByType<AIController>()) brain.enabled = false;
            var reader = who.GetComponent<PlayerInputReader>();
            if (reader != null) reader.enabled = false;
            who.Teleport(new Vector3(-3, .12f, -8)); who.Intent.Clear(); who.Intent.Parked = false;
            who.transform.rotation = Quaternion.identity;
            who.Intent.AimPoint = new Vector3(-3, .12f, 0);
            rig.Follow(who); rig.SetAimSource(CameraSystem.AimSource.Movement);
            yield return null;
            var direction = who.Intent.AimPoint - rig.transform.position;
            typeof(CameraSystem.CameraRig).GetField("_pitchDeg", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(rig, Mathf.Atan2(-direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg);
            yield return null;
            carrier.enabled = false; who.Intent.Set(Verb.SpecialAbility, true);
            Assert.IsTrue(GameServices.Round.CanThrow(who), "The visual check needs a legal throw outside the defender box.");
            var step = typeof(Carrier).GetMethod("StepAttacker", BindingFlags.Instance | BindingFlags.NonPublic);
            step.Invoke(carrier, new object[] { 0f }); step.Invoke(carrier, new object[] { Balance.ChargeFullTime });
            Assert.IsTrue(carrier.IsCharging);
            var guide = GameObject.Find("~AimArc1").GetComponent<TrajectoryPreview>();
            typeof(TrajectoryPreview).GetMethod("Rebuild", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(guide, null);
            Assert.IsTrue(guide.LandingVisible);
            Assert.IsTrue(guide.FlightVisible);
            var flightMesh = guide.transform.Find("FlightPath").GetComponent<MeshFilter>().sharedMesh;
            Assert.Greater(flightMesh.bounds.max.y, guide.LandingPoint.y + .2f, "Flight guidance has to leave the floor.");
            const int verticesPerMark = 2 * 2 * 3; // Dash and outline, two triangles each.
            Assert.LessOrEqual(flightMesh.vertexCount, TrajectoryPreview.MaxFlightMarks * verticesPerMark,
                "Keep the path sparse at full charge.");
            // Each dash lies along the dotted line it belongs to: the screen chord between its
            // neighbouring marks. The ink dash's first triangle is (start-across, start+across, end+across).
            var marks = flightMesh.vertices;
            int markCount = marks.Length / verticesPerMark;
            var centres = new Vector2[markCount];
            var axes = new Vector2[markCount];
            for (int m = 0; m < markCount; m++)
            {
                var v = m * verticesPerMark;
                centres[m] = rig.Camera.WorldToScreenPoint((marks[v] + marks[v + 2]) * .5f);
                axes[m] = (Vector2)rig.Camera.WorldToScreenPoint(marks[v + 2]) - (Vector2)rig.Camera.WorldToScreenPoint(marks[v + 1]);
            }
            for (int m = 1; m < markCount - 1; m++)
            {
                float turn = Vector2.Angle(axes[m], centres[m + 1] - centres[m - 1]);
                Assert.Less(Mathf.Min(turn, 180 - turn), 8f, $"Dash {m} of {markCount} must follow the dotted line, not bend away from it.");
            }
            // The line leaves just above the held tsinelas and keeps an even screen rhythm,
            // so the stretch beside the hand is as full as the far end.
            float gap = TrajectoryPreview.MarkSpacingPixels * Mathf.Max(1f, rig.Camera.pixelHeight / 1080f);
            Vector2 handStart = rig.Camera.WorldToScreenPoint(rig.Camera.ViewportToWorldPoint(TrajectoryPreview.HandStartViewport));
            Assert.GreaterOrEqual(markCount, 4, "Even this short on-screen arc carries several dashes.");
            Assert.Less(Vector2.Distance(centres[0], handStart), gap * 1.5f, "The guide starts just above the held tsinelas.");
            for (int m = 1; m < markCount; m++)
                Assert.Less(Vector2.Distance(centres[m], centres[m - 1]), gap * 1.3f,
                    $"Dashes {m - 1} and {m} must keep the even screen rhythm instead of leaving a gap.");
            Debug.Log("Landing review point=" + guide.LandingPoint + " viewport=" + rig.Camera.WorldToViewportPoint(guide.LandingPoint)
                + " camera=" + rig.Camera.transform.position + " facing=" + rig.Camera.transform.forward + " rigLocal=" + rig.IsLocalFpp);
            foreach (var vertex in guide.GetComponent<MeshFilter>().sharedMesh.vertices)
            {
                Assert.That(vertex.y, Is.EqualTo(guide.LandingPoint.y).Within(.001));
                Assert.Less(Vector3.Distance(vertex, guide.LandingPoint), .6f, "The landing circle stays on its own mesh.");
            }
            // Exercise the real mesh between two prediction ticks. Prediction remains exact;
            // only the visible centre travels linearly at render cadence.
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var exact=guide.LandingPoint;var from=exact+Vector3.right*.4f;
            typeof(TrajectoryPreview).GetField("_drawFrom",flags).SetValue(guide,from);
            typeof(TrajectoryPreview).GetField("_drawStart",flags).SetValue(guide,Time.unscaledTime);
            var draw=typeof(TrajectoryPreview).GetMethod("DrawInterpolated",flags);
            float start=Time.unscaledTime;
            draw.Invoke(guide,new object[]{start+TrajectoryPreview.PredictionInterval*.5f});
            Assert.Less(Vector3.Distance(guide.DisplayedLandingPoint,Vector3.Lerp(from,exact,.5f)),.002f);
            Assert.AreEqual(exact,guide.LandingPoint,"Presentation may not rewrite the ballistic prediction.");
            draw.Invoke(guide,new object[]{start+TrajectoryPreview.PredictionInterval*2});
            Assert.Less(Vector3.Distance(guide.DisplayedLandingPoint,exact),.001f,"Never overshoot a landing target.");
            string review = System.Environment.GetEnvironmentVariable("TUMP_THROW_GUIDE_REVIEW_DIR");
            string output = review ?? "Logs/throw-guide-review";
            System.IO.Directory.CreateDirectory(output);
            typeof(ThrowAimIntegrationProbe).GetMethod("CaptureGuideDifference", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { rig.Camera, guide, output, "flight-visible" });
            yield return GameplayShots.Render(rig.Camera, "Flight-guide-charge", true, output);
            Assert.IsTrue(guide.FlightVisible, "Actual render frames must retain the local flight path. charged=" + carrier.IsCharging
                + " held=" + (carrier.Held != null) + " round=" + GameServices.Round.RoundActive + " brain=" + (who.GetComponent<AIController>()?.enabled == true)
                + " main=" + (Camera.main == rig.Camera) + " local=" + rig.IsLocalFpp + " follows=" + rig.IsFollowing(who));
            if (!string.IsNullOrEmpty(review))
                yield return RecordGuide(rig, who, carrier, guide, step, System.IO.Path.Combine(output, "frames"));
            else
            {
                who.Intent.Set(Verb.SpecialAbility, false);
                step.Invoke(carrier, new object[] { .01f });
            }
            guide.SendMessage("LateUpdate");
            Assert.IsFalse(guide.LandingVisible);
            Assert.IsFalse(guide.FlightVisible);
            Assert.IsNull(carrier.Held, "The capture must finish with a real throw.");
        }

        private static IEnumerator RecordGuide(CameraSystem.CameraRig rig, CharacterMotor who, Carrier carrier,
            TrajectoryPreview guide, MethodInfo step, string folder)
        {
            const int width = 1280, height = 720, fps = 30, releaseFrame = 108, frameCount = 174, uiLayer = 31;
            System.IO.Directory.CreateDirectory(folder);
            var scene = rig.Camera;
            var hud = GameObject.Find("OwnerMatchCanvas").GetComponent<Canvas>();
            var world = new RenderTexture(width, height, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
            var resolved = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            var ui = new GameObject("Throw guide capture UI").AddComponent<Camera>();
            ui.enabled = false;
            ui.clearFlags = CameraClearFlags.Depth;
            ui.cullingMask = 1 << uiLayer;
            ui.orthographic = true;
            ui.nearClipPlane = .01f;
            ui.farClipPlane = 10;
            ui.targetTexture = resolved;
            var oldTarget = scene.targetTexture;
            var oldMode = hud.renderMode;
            var oldCamera = hud.worldCamera;
            float oldDistance = hud.planeDistance;
            int oldMask = scene.cullingMask, oldRate = Time.captureFramerate;
            var layers = new Dictionary<GameObject, int>();
            var pitch = typeof(CameraSystem.CameraRig).GetField("_pitchDeg", BindingFlags.Instance | BindingFlags.NonPublic);
            // The carrier stays disabled for stepping, so advance its hold clock here; otherwise
            // the aim drift never settles and the capture shows a fresh-press wobble throughout.
            var held = typeof(Carrier).GetField("_aimHeldSeconds", BindingFlags.Instance | BindingFlags.NonPublic);
            var shoe = carrier.Held;
            Vector3 predicted = default;
            bool landed = false;
            try
            {
                foreach (var child in hud.GetComponentsInChildren<Transform>(true))
                {
                    layers[child.gameObject] = child.gameObject.layer;
                    child.gameObject.layer = uiLayer;
                }
                scene.cullingMask &= ~(1 << uiLayer);
                scene.targetTexture = world;
                hud.renderMode = RenderMode.ScreenSpaceCamera;
                hud.worldCamera = ui;
                hud.planeDistance = 1;
                who.Intent.ClearAim();
                Time.captureFramerate = fps;
                yield return null;
                yield return null;

                for (int frame = 0; frame < frameCount; frame++)
                {
                    float turn = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(20, 86, frame));
                    float yaw = Mathf.Lerp(0, -12, turn);
                    float look = Mathf.Lerp(-2.5f, 5, turn);
                    who.transform.rotation = Quaternion.Euler(0, yaw, 0);
                    pitch.SetValue(rig, look);
                    if (frame < releaseFrame)
                    {
                        held.SetValue(carrier, (float)held.GetValue(carrier) + 1f / fps);
                        who.Intent.SpinInput = .48f * (1 - turn);
                        step.Invoke(carrier, new object[] { 1f / fps });
                    }
                    if (frame == releaseFrame)
                    {
                        predicted = guide.LandingPoint;
                        who.Intent.Set(Verb.SpecialAbility, false);
                        step.Invoke(carrier, new object[] { .01f });
                    }
                    yield return null;
                    if (frame >= releaseFrame && !landed && shoe.State == SlipperState.Loose)
                    {
                        landed = true;
                        Vector3 actual = shoe.transform.position;
                        actual.y = predicted.y;
                        Assert.Less(Vector3.Distance(predicted, actual), .35f,
                            $"The predicted landing at {predicted} must agree with the actual floor contact at {actual}.");
                    }
                    scene.Render();
                    Graphics.Blit(world, resolved);
                    Canvas.ForceUpdateCanvases();
                    ui.Render();
                    var previous = RenderTexture.active;
                    try
                    {
                        RenderTexture.active = resolved;
                        pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                        pixels.Apply();
                        System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, $"{frame:D5}.jpg"), pixels.EncodeToJPG(93));
                    }
                    finally { RenderTexture.active = previous; }
                }
            }
            finally
            {
                Time.captureFramerate = oldRate;
                scene.targetTexture = oldTarget;
                scene.cullingMask = oldMask;
                hud.renderMode = oldMode;
                hud.worldCamera = oldCamera;
                hud.planeDistance = oldDistance;
                foreach (var entry in layers) if (entry.Key != null) entry.Key.layer = entry.Value;
                Object.Destroy(ui.gameObject);
                Object.Destroy(pixels);
                world.Release(); Object.Destroy(world);
                resolved.Release(); Object.Destroy(resolved);
            }
            Assert.IsTrue(landed, "The recording must show the thrown slipper reaching the court.");
        }
    }
}
