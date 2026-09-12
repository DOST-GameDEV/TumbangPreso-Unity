using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.Settings;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    // Static profile comparisons use the actual followed first-person rig, inside
    // the legal court. Separate timestamped sequences run at ordinary speed.
    [Category("WallClock")]
    public sealed class MapExperienceProbe
    {
        private bool _bots, _spectator, _pinned;
        private int _seat, _quality;
        private float _timeScale;
        private CustomRules _rules;
        private static string Output => Environment.GetEnvironmentVariable("TUMP_EVIDENCE") ?? "Logs/map-owner-review-v1";
        private static readonly string[] Maps = { SceneFlow.Eskinita, SceneFlow.BayanPlaza, SceneFlow.IlalimNgTulay };
        private static string[] ReviewMaps()
        {
            string selected = Environment.GetEnvironmentVariable("TUMP_MAP_REVIEW");
            if (string.IsNullOrEmpty(selected)) return Maps;
            CollectionAssert.Contains(Maps, selected, "Unknown review map must not produce a zero-coverage pass.");
            return new[] { selected };
        }

        [UnitySetUp] public IEnumerator Before()
        {
            _bots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _seat = GameLaunch.SoloSeat;
            _quality = GraphicsProfiles.Current; _timeScale = Time.timeScale;
            _pinned = SceneFlow.RulesPinned; _rules = SceneFlow.SelectedRules.Clone();
            yield return PlayModeWorld.Reset();
            Directory.CreateDirectory(Output);
        }

        [UnityTearDown] public IEnumerator After()
        {
            Time.timeScale = 1;
            yield return PlayModeWorld.Reset();
            Time.timeScale = _timeScale; GraphicsProfiles.Apply(_quality);
            GameLaunch.AllBots = _bots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _seat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator LegalFirstPersonViewsAtMatchedQualityProfiles()
        {
            var report = new StringBuilder("map,mode,view,quality,body_x,body_y,body_z,eye_x,eye_y,eye_z,yaw,fov\n");
            try
            {
                foreach (var mode in new[] { GameMode.Classic, GameMode.HeroStrike })
                foreach (string map in ReviewMaps())
                {
                    Time.timeScale = 1;
                    yield return MapRetrievalProbe.Load(map, mode);
                    var who = GameServices.Round.PlayerAt(1);
                    StageOtherSeats(who);
                    var rig = Object.FindFirstObjectByType<CameraRig>();
                    Assert.IsNotNull(rig);
                    rig.Follow(who); rig.SetAimSource(AimSource.Mouse);
                    Assert.AreEqual(CameraMode.Fpp, rig.Mode);
                    Assert.AreSame(rig.Camera, Camera.main);
                    float halfX = AIController.PlayableHalfX, halfZ = AIController.PlayableHalfZ;
                    // Eight directions: view into the court, then turn to review
                    // the nearby street/civic/guideway edge from the same legal spot.
                    var stations = new[] { new Vector3(0, 0, 8.8f), new Vector3(-7.2f, 0, 0),
                        new Vector3(0, 0, -8.8f), new Vector3(7.2f, 0, 0) };
                    for (int station = 0; station < stations.Length; station++)
                    {
                        var at = stations[station];
                        Assert.Less(Mathf.Abs(at.x) + .4f, halfX);
                        Assert.Less(Mathf.Abs(at.z) + .4f, halfZ);
                        at.y = Slipper.GroundY(at);
                        who.Teleport(at);
                        yield return new WaitForFixedUpdate();
                        yield return new WaitForFixedUpdate();
                        for (int facing = 0; facing < 2; facing++)
                        {
                            Vector3 direction = facing == 0 ? -stations[station] : stations[station];
                            who.transform.rotation = Quaternion.LookRotation(direction.normalized);
                            Time.timeScale = 0;
                            yield return null;
                            string view = station + (facing == 0 ? "-court" : "-edge");
                            for (int quality = 0; quality < GraphicsProfiles.All.Length; quality++)
                            {
                                GraphicsProfiles.Apply(quality);
                                yield return null;
                                Assert.IsTrue(rig.IsFollowing(who) && rig.IsLocalFpp);
                                var p = who.transform.position; var eye = rig.Camera.transform.position;
                                foreach (var other in GameServices.Round.Players.Where(p => p != who))
                                    Assert.Greater(Vector3.Distance(p, other.transform.position), 1.5f,
                                        "The review camera was staged inside another player's visible head.");
                                Assert.AreEqual(CameraRig.PersonCapsuleHeight * .5f + CameraRig.FppEyeHeight,
                                    eye.y - p.y, .02f, "This is no longer the ordinary owner eye height.");
                                string profile = GraphicsProfiles.Of(quality).Label;
                                yield return GameplayShots.Render(rig.Camera, map + "-" + mode + "-" + view + "-" + profile,
                                    false, Output);
                                report.AppendLine(FormattableString.Invariant(
                                    $"{map},{mode},{view},{profile},{p.x:F4},{p.y:F4},{p.z:F4},{eye.x:F4},{eye.y:F4},{eye.z:F4},{rig.Camera.transform.eulerAngles.y:F2},{rig.Camera.fieldOfView:F2}"));
                            }
                            Time.timeScale = 1;
                        }
                    }
                    yield return GameplayShots.Render(rig.Camera, map + "-" + mode + "-with-hud", true, Output);
                    yield return PlayModeWorld.Reset();
                }
            }
            finally { File.WriteAllText(Path.Combine(Output, "owner-views.csv"), report.ToString()); }
        }

        [UnityTest]
        public IEnumerator DiagnoseOwnerOcclusion()
        {
            yield return MapRetrievalProbe.Load(SceneFlow.Eskinita);
            var who = GameServices.Round.PlayerAt(1);
            var rig = Object.FindFirstObjectByType<CameraRig>();
            rig.Follow(who); rig.SetAimSource(AimSource.Mouse);
            who.Teleport(new Vector3(0, .1f, 8.8f));
            who.transform.rotation = Quaternion.Euler(0, 180, 0);
            for (int i = 0; i < 5; i++) yield return null;
            var eye = rig.Camera.transform.position;
            var report = new StringBuilder($"eye={eye} near={rig.Camera.nearClipPlane} far={rig.Camera.farClipPlane} mask={rig.Camera.cullingMask} rig={rig.name}\n");
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                .Where(r => r.enabled && r.bounds.SqrDistance(eye) < 9).OrderBy(r => r.bounds.SqrDistance(eye)))
            {
                string path = r.name; for (var t = r.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
                report.AppendLine($"{path} enabled={r.enabled} forceOff={r.forceRenderingOff} shadow={r.shadowCastingMode} bounds={r.bounds} scale={r.transform.lossyScale} mats={string.Join(",", r.sharedMaterials.Select(m => m == null ? "null" : m.name + "/" + m.shader.name))}");
            }
            File.WriteAllText(Path.Combine(Output, "occluders.txt"), report.ToString());
            yield return GameplayShots.Render(rig.Camera, "occlusion-normal", false, Output);
            var outline = rig.Camera.GetComponent<WorldOutline>();
            bool outlining = outline != null && outline.enabled;
            if (outline != null) outline.enabled = false;
            yield return GameplayShots.Render(rig.Camera, "occlusion-no-outline", false, Output);
            if (outline != null) outline.enabled = outlining;
            var arms = rig.GetComponentsInChildren<ViewmodelArms>(true);
            foreach (var arm in arms) arm.gameObject.SetActive(false);
            yield return GameplayShots.Render(rig.Camera, "occlusion-no-viewmodel", false, Output);
            foreach (var arm in arms) arm.gameObject.SetActive(true);
            var body = who.GetComponentsInChildren<Renderer>(true);
            foreach (var r in body) r.forceRenderingOff = true;
            yield return GameplayShots.Render(rig.Camera, "occlusion-no-body", false, Output);
            foreach (var r in body) r.forceRenderingOff = false;
            StageOtherSeats(who);
            yield return null;
            yield return GameplayShots.Render(rig.Camera, "occlusion-seats-separated", false, Output);
        }

        private static void StageOtherSeats(CharacterMotor subject)
        {
            // Directly positioning a review subject bypasses motor collision. The
            // original north station occupied seat2's starting head, blacking out
            // the frame; the parked defender also blocked the controlled route.
            // Keep four visible people, but stage them off these measurement paths.
            var marks = new[] { new Vector3(-5, 0, -5), new Vector3(-5, 0, 5), new Vector3(5, 0, -5) };
            int i = 0;
            foreach (var other in GameServices.Round.Players.Where(p => p != subject))
            {
                var at = marks[i++]; at.y = Slipper.GroundY(at);
                other.Teleport(at);
            }
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator ControlledCarryThrowAndRetrievalAtOrdinarySpeed()
        {
            foreach (var mode in new[] { GameMode.Classic, GameMode.HeroStrike })
            foreach (string map in ReviewMaps())
            {
                Time.timeScale = 1; GraphicsProfiles.Apply(1);
                yield return MapRetrievalProbe.Load(map, mode);
                var who = GameServices.Round.PlayerAt(1);
                StageOtherSeats(who);
                var rig = Object.FindFirstObjectByType<CameraRig>();
                rig.Follow(who); rig.SetAimSource(AimSource.Movement);
                var at = new Vector3(0, 0, 9.2f); at.y = Slipper.GroundY(at);
                who.Teleport(at); who.Intent.Parked = false;
                var shoe = who.GetComponent<Carrier>().Held;
                Assert.IsNotNull(shoe);
                bool released = false, retrieved = false;
                var trace = new StringBuilder("seconds,body,shoe,state,grab,holding\n");
                float nextTrace = 0;
                var witness = new GameObject("MapMotionWitness").AddComponent<Camera>();
                witness.enabled = false; witness.fieldOfView = 52; witness.nearClipPlane = .05f;
                witness.farClipPlane = 400; witness.allowHDR = true; witness.cullingMask &= ~(1 << 5);
                witness.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
                var driver = who.gameObject.AddComponent<ReviewInput>();
                float began = Time.realtimeSinceStartup;
                try
                {
                    driver.Drive = () =>
                    {
                        float t = Time.realtimeSinceStartup - began;
                        who.Intent.Move = t > .5f && t < 2.5f ? new Vector2(.4f, 0) : Vector2.zero;
                        who.Intent.Set(Verb.Sprint, t > 1.2f && t < 2.5f);
                        who.Intent.AimPoint = new Vector3(1.5f, .2f, 0);
                        who.Intent.FaceAimPoint = true;
                        who.Intent.Set(Verb.SpecialAbility, t > 3 && t < 4.5f);
                        if (shoe.State == SlipperState.InFlight) released = true;
                        if (released && t > 5 && shoe.State == SlipperState.Loose)
                        {
                            Vector3 delta = shoe.transform.position - who.transform.position;
                            who.Intent.Move = new Vector2(delta.x, delta.z).normalized;
                            who.Intent.Set(Verb.Grab, Vector3.Distance(who.transform.position, shoe.transform.position) < Balance.PickupRadius);
                            who.Intent.AimPoint = shoe.transform.position;
                        }
                        if (released && who.GetComponent<Carrier>().Held == shoe) retrieved = true;
                        if (t >= nextTrace)
                        {
                            nextTrace = t + .25f;
                            trace.AppendLine($"{t:F3},{who.transform.position:F3},{shoe.transform.position:F3},{shoe.State},{who.Intent.Pressed(Verb.Grab)},{who.HoldingSlipper}");
                        }
                    };
                    yield return ImprovementEvidenceProbe.Record(witness, map + "-" + mode + "-controlled-sequence", 16, who);
                    Assert.IsTrue(released, map + " " + mode + " never released the throw through InputIntent.");
                    Assert.IsTrue(retrieved, map + " " + mode + " did not complete the controlled retrieval.");
                }
                finally
                {
                    File.WriteAllText(Path.Combine(Output, map + "-" + mode + "-retrieval-trace.txt"), trace.ToString());
                    driver.enabled = false; Object.Destroy(driver);
                    who.Intent.Clear(); Object.Destroy(witness.gameObject);
                }
                yield return PlayModeWorld.Reset();
            }
        }

        // Input producers run before Carrier.Update. A coroutine writes after
        // Update; the next motor FixedUpdate can commit away its pickup edge before
        // Carrier ever sees it. Match the real input phase instead of repeatedly
        // pressing or weakening the retrieval assertion to cover a fixture race.
        [DefaultExecutionOrder(-300)]
        private sealed class ReviewInput : MonoBehaviour
        {
            public Action Drive;
            private void Update() => Drive?.Invoke();
        }
    }
}
