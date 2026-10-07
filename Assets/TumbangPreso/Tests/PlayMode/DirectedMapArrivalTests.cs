using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    public sealed class DirectedMapArrivalTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private MatchArrivalPresentation _arrival;
        private Camera _camera;
        private RenderTexture _target;
        private string _settings;
        private int _idleDelay;
        private CustomRules _rules;
        private bool _rulesPinned;
        private object Field(string name) => typeof(MatchArrivalPresentation).GetField(name, Hidden).GetValue(_arrival);
        private void Set(string name, object value) => typeof(MatchArrivalPresentation).GetField(name, Hidden).SetValue(_arrival, value);
        private void Call(string name, params object[] args) => typeof(MatchArrivalPresentation).GetMethod(name, Hidden).Invoke(_arrival, args);
        [UnitySetUp] public IEnumerator Before()
        {
            _rules = SceneFlow.SelectedRules.Clone(); _rulesPinned = SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
            _settings = JsonUtility.ToJson(Settings.SettingsStore.Current);
#if UNITY_EDITOR
            _idleDelay = UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds;
            UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds = 1;
#endif
        }
        [UnityTearDown] public IEnumerator After()
        {
            _arrival?.Cancel();
            if (_camera != null) _camera.targetTexture = null;
            if (_target != null) { _target.Release(); Object.Destroy(_target); }
            JsonUtility.FromJsonOverwrite(_settings, Settings.SettingsStore.Current);
            yield return PlayModeWorld.Reset();
            SceneFlow.AdoptRemoteRules(_rules);
            if (_rulesPinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
#if UNITY_EDITOR
            UnityEditor.EditorUserSettings.idleImportWorkerShutdownDelayMilliseconds = _idleDelay;
#endif
        }
        [UnityTest] public IEnumerator BridgeLowArrival() => Inspect(SceneFlow.IlalimNgTulay);
        [UnityTest] public IEnumerator CoveCurvedArrival() => Inspect(SceneFlow.LagoonCove);
        [UnityTest] public IEnumerator KantoCrossingArrival() => Inspect(SceneFlow.Kanto);
        [UnityTest] public IEnumerator BridgeReducedMotionArrival() => Inspect(SceneFlow.IlalimNgTulay, true);
        [UnityTest] public IEnumerator CoveReducedMotionArrival() => Inspect(SceneFlow.LagoonCove, true);
        [UnityTest] public IEnumerator KantoReducedMotionArrival() => Inspect(SceneFlow.Kanto, true);
        [UnityTest] public IEnumerator BridgeCameraMotionOff() => Inspect(SceneFlow.IlalimNgTulay, false, false);
        [UnityTest] public IEnumerator CoveCameraMotionOff() => Inspect(SceneFlow.LagoonCove, false, false);
        [UnityTest] public IEnumerator KantoCameraMotionOff() => Inspect(SceneFlow.Kanto, false, false);
        [UnityTest] public IEnumerator BridgeNaturalHeroCountdown() => NaturalFlow(SceneFlow.IlalimNgTulay);
        [UnityTest] public IEnumerator CoveNaturalHeroCountdown() => NaturalFlow(SceneFlow.LagoonCove);
        [UnityTest] public IEnumerator KantoNaturalHeroCountdown() => NaturalFlow(SceneFlow.Kanto);

        private IEnumerator NaturalFlow(string mapId)
        {
            SceneFlow.Networked = false;
            var rules = CustomGameRules.Defaults(GameMode.HeroStrike); rules.ManualReady = false;
            rules.Bots = CustomGameRules.MaxBots;
            SceneFlow.PinSelectedRules(rules);
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false;
            Settings.SettingsStore.Current.CinematicCameraMotion = true;
            Settings.SettingsStore.Current.ReducedUiMotion = false;
            yield return SceneManager.LoadSceneAsync(mapId);
            // Scene-load completion precedes the installer's Start callback.
            float preparedBy = Time.realtimeSinceStartup + 15;
            while (Object.FindAnyObjectByType<ReadyGate>() == null && Time.realtimeSinceStartup < preparedBy)
                yield return null;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>()) reader.enabled = false;
            var gate = Object.FindAnyObjectByType<ReadyGate>(); Assert.IsNotNull(gate);
            var ticks = new List<string>(); gate.CountdownTick += ticks.Add;
            var observer = new GameObject("Natural arrival observer").AddComponent<DirectedArrivalFrameObserver>();
            var maximumTravel = new float[4]; var baseline = new Vector3[4]; bool capturedBaseline = false, sawDirection = false;
            int frame = 0; float until = Time.realtimeSinceStartup + 45;
            var folder = Path.Combine("Logs", "directed-map-arrivals", mapId + "-natural-hero"); Directory.CreateDirectory(folder);
            while (GameServices.Round?.RoundActive != true && Time.realtimeSinceStartup < until)
            {
                var presentation = Object.FindAnyObjectByType<MatchArrivalPresentation>();
                if (presentation != null && (bool)typeof(MatchArrivalPresentation).GetField("_directed", Hidden).GetValue(presentation))
                {
                    sawDirection = true;
                    var players = GameServices.Round.Players.ToArray();
                    if (!capturedBaseline)
                    {
                        var rests = (Vector3[])typeof(MatchArrivalPresentation).GetField("_walkRest", Hidden).GetValue(presentation);
                        System.Array.Copy(rests, baseline, 4); capturedBaseline = true;
                    }
                    observer.Observe = () =>
                    {
                        foreach (var player in players)
                        {
                            var root = player.GetComponent<CharacterVisual>()?.ModelRoot;
                            if (root != null) maximumTravel[player.PlayerSlot] = Mathf.Max(maximumTravel[player.PlayerSlot],
                                Vector3.Distance(baseline[player.PlayerSlot], root.localPosition));
                        }
                    };
                    frame++;
                }
                yield return null;
            }
            gate.CountdownTick -= ticks.Add; Object.Destroy(observer.gameObject);
            Assert.IsTrue(sawDirection && frame > 10, "Observe the actual automatic opening, not manual samples");
            Assert.AreEqual(4, maximumTravel.Count(v => v > .8f), "All real Hero actors must visibly arrive");
            Assert.IsTrue(GameServices.Round.RoundActive, "No input was pressed; the automatic opening must release into play");
            CollectionAssert.AreEqual(new[] { "3", "2", "1", "GO!" }, ticks);
            Assert.IsFalse(PresentationClock.Held); Assert.IsFalse(MatchArrivalPresentation.Active);
            foreach (var player in GameServices.Round.Players) Assert.IsTrue(player.CanAct());
            File.WriteAllText(Path.Combine(folder, "flow.txt"), "actualFrames=" + frame + " maxTravel=" + string.Join(",", maximumTravel)
                + " ticks=" + string.Join(",", ticks) + " liveRound=" + GameServices.Round.RoundActive);
        }

        private IEnumerator Inspect(string mapId, bool reduced = false, bool motion = true)
        {
            SceneFlow.Networked = false;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false;
            Settings.SettingsStore.Current.CinematicCameraMotion = motion;
            Settings.SettingsStore.Current.ReducedUiMotion = reduced;
            yield return SceneManager.LoadSceneAsync(mapId);
            while (HubLoading.Preparing) yield return null;
            yield return null;
            foreach (var old in Object.FindObjectsByType<MatchArrivalPresentation>(FindObjectsSortMode.None)) old.Cancel();
            foreach (var ai in Object.FindObjectsByType<AIController>()) ai.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>()) reader.enabled = false;
            var actors = GameServices.Round.Players.OrderBy(p => p.PlayerSlot).ToArray();
            Assert.AreEqual(4, actors.Length);
            foreach (var actor in actors) actor.enabled = false;
            _camera = Camera.main;
            var rig = _camera.GetComponent<CameraSystem.CameraRig>();
            var savedEye = _camera.transform.position; var savedTurn = _camera.transform.rotation; float savedFov = _camera.fieldOfView;
            var marks = actors.Select(p => p.transform.position).ToArray();
            var roots = actors.Select(p => p.GetComponent<CharacterVisual>().ModelRoot).ToArray();
            var rests = roots.Select(r => r.localPosition).ToArray(); var turns = roots.Select(r => r.localRotation).ToArray();
            _arrival = new GameObject("Directed map review").AddComponent<MatchArrivalPresentation>();
            typeof(MatchArrivalPresentation).GetField("_active", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, _arrival);
            typeof(PresentationClock).GetMethod("Hold", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            Set("_held", true);
            rig.SetActive(false);
            Set("_camera", _camera); Set("_rig", rig); Set("_rigActive", true);
            Set("_position", savedEye); Set("_rotation", savedTurn); Set("_fov", savedFov);
            var players = (CharacterMotor[])Field("_players"); var poses = (CharacterAnimator[])Field("_poses");
            for (int i = 0; i < 4; i++) { players[i] = actors[i]; poses[i] = actors[i].GetComponent<CharacterAnimator>(); }
            Call("BuildCaption"); var map = SceneFlow.PreviewFor(mapId);
            Call("PrepareShots", map); Call("PrepareDirection", map, reduced);
            Assert.IsTrue((bool)Field("_directed"));
            float arrivalEnd = (float)Field("_direction").GetType().GetField("Arrival").GetValue(Field("_direction"));
            float spotlight = (float)Field("_direction").GetType().GetField("Spotlight").GetValue(Field("_direction"));
            _target = new RenderTexture(960, 540, 24); _target.Create(); _camera.targetTexture = _target;
            var output = Path.Combine("Logs", "directed-map-arrivals", mapId + (reduced ? "-reduced" : motion ? "" : "-camera-off")); Directory.CreateDirectory(output);
            var facts = new List<string>(); var maxTravel = new float[4]; var minimumFloor = new float[4];
            facts.Add("preparedPathLengths=" + string.Join(",", (float[])Field("_walkLengths")));
            var focusPoints = (Vector3[])Field("_focus");
            var eyePoints = (Vector3[])Field("_eyes");
            for (int i = 0; i < 4; i++)
            {
                facts.Add("seat=" + i + " root=" + roots[i].name + " mark=" + marks[i] + " focus=" + focusPoints[i] + " eye=" + eyePoints[i]);
                foreach (var collider in Physics.OverlapSphere(focusPoints[i], .25f, ~0, QueryTriggerInteraction.Ignore))
                    facts.Add("focusOverlap=" + collider.name + " world=" + (collider.GetComponentInParent<CharacterMotor>() == null));
                Vector3 ray = eyePoints[i] - focusPoints[i];
                if (ray.sqrMagnitude > .0001f)
                    foreach (var hit in Physics.SphereCastAll(focusPoints[i], .2f, ray.normalized, Mathf.Max(8, ray.magnitude), ~0, QueryTriggerInteraction.Ignore))
                        facts.Add("focusRay=" + hit.collider.name + " distance=" + hit.distance + " point=" + hit.point
                            + " actor=" + (hit.collider.GetComponentInParent<CharacterMotor>() != null));
            }
            for (int i = 0; i < 4; i++) minimumFloor[i] = float.PositiveInfinity;
            int shot = 0;
            var observer = _arrival.gameObject.AddComponent<DirectedArrivalFrameObserver>();
            float closestPair = float.PositiveInfinity;
            for (int frame = 0; !reduced && motion && frame <= 24; frame++)
            {
                float age = arrivalEnd * frame / 24f;
                Call("Sample", age, reduced, map);
                bool observed = false;
                observer.Observe = () =>
                {
                    observed = true;
                    for (int a = 0; a < 4; a++) for (int b = a + 1; b < 4; b++)
                    {
                        Vector3 gap = roots[a].position - roots[b].position; gap.y = 0;
                        closestPair = Mathf.Min(closestPair, gap.magnitude);
                    }
                    Capture(Path.Combine(output, "arrival-motion.png"));
                };
                yield return null; yield return null;
                Assert.IsTrue(observed);
                facts.Add("arrivalAge=" + age + " closestPair=" + closestPair);
                File.WriteAllLines(Path.Combine(output, "composition.txt"), facts);
                Assert.GreaterOrEqual(closestPair, 1.4f, "Entrance routes crossed through another character");
            }
            Vector3? stationaryEye = null; Quaternion stationaryTurn = Quaternion.identity;
            foreach (float age in new[] { .06f, arrivalEnd * .35f, arrivalEnd * .75f, arrivalEnd + .4f,
                spotlight + .5f, _arrival.Duration - 1.9f, _arrival.Duration })
            {
                Call("Sample", age, reduced, map);
                System.Exception failure = null; bool observed = false;
                // Observe after animation, visual staging and the real carrier hand read.
                observer.Observe = () =>
                {
                try
                {
                observed = true;
                Capture(Path.Combine(output, "shot-" + shot++ + ".png"));
                facts.Add("age=" + age + " eye=" + _camera.transform.position + " beat=" + Field("_shownBeat"));
                File.WriteAllLines(Path.Combine(output, "composition.txt"), facts);
                for (int i = 0; i < 4; i++)
                {
                    Assert.Less(Vector3.Distance(marks[i], actors[i].transform.position), .001f, "Presentation moved a gameplay root");
                    maxTravel[i] = Mathf.Max(maxTravel[i], Vector3.Distance(rests[i], roots[i].localPosition));
                    if (reduced || !motion)
                    {
                        Vector3 travel = roots[i].localPosition - rests[i]; travel.y = 0;
                        Assert.Less(travel.magnitude, .001f, "Disabled arrival motion moved the cast");
                    }
                    float sole = SoleHeight(actors[i]); float floor = FloorUnder(roots[i].position, marks[i].y);
                    minimumFloor[i] = Mathf.Min(minimumFloor[i], sole - floor);
                    Assert.That(sole - floor, Is.InRange(-.02f, .04f), mapId + " seat" + i + " rendered sole support at" + age);
                    var carrier = actors[i].GetComponent<Carrier>();
                    if (carrier.Held != null)
                    {
                        var anchor = actors[i].GetComponent<CharacterVisual>().HandAnchor;
                        Vector3 expected = anchor.position + anchor.up * carrier.Held.CarrySupportExtent(anchor.up)
                            - carrier.Held.DrawnCentreOffset;
                        Assert.Less(Vector3.Distance(carrier.Held.transform.position, expected), .03f,
                            "Held slipper detached during the arrival");
                    }
                }
                Assert.IsTrue(float.IsFinite(_camera.transform.position.sqrMagnitude));
                if (!motion)
                {
                    Assert.Less(Vector3.Distance(savedEye, _camera.transform.position), .001f);
                    Assert.Less(Quaternion.Angle(savedTurn, _camera.transform.rotation), .01f);
                    Assert.That(_camera.fieldOfView, Is.EqualTo(savedFov).Within(.001f));
                }
                if (reduced && age < _arrival.Duration - 1.4f)
                {
                    if (!stationaryEye.HasValue) { stationaryEye = _camera.transform.position; stationaryTurn = _camera.transform.rotation; }
                    Assert.Less(Vector3.Distance(stationaryEye.Value, _camera.transform.position), .001f);
                    Assert.Less(Quaternion.Angle(stationaryTurn, _camera.transform.rotation), .01f);
                }
                if (!reduced && motion && age >= spotlight && age < spotlight + 1)
                {
                    Assert.AreEqual(MatchRules.DefenderSlotFor(1), (int)Field("_shownBeat"));
                    foreach (var point in RenderedPoints(actors[MatchRules.DefenderSlotFor(1)]))
                    {
                        Vector3 view = _camera.WorldToViewportPoint(point);
                        Assert.Greater(view.z, _camera.nearClipPlane);
                        Assert.That(view.x, Is.InRange(.025f, .975f), "Taya cropped horizontally");
                        Assert.That(view.y, Is.InRange(.025f, .975f), "Taya cropped vertically");
                    }
                }
                }
                catch (System.Exception error) { failure = error; }
                };
                yield return null; yield return null;
                Assert.IsTrue(observed, "The actual render phase was not observed");
                if (failure != null) throw failure;
            }
            facts.Add("maxTravel=" + string.Join(",", maxTravel));
            File.WriteAllLines(Path.Combine(output, "composition.txt"), facts);
            if (!reduced && motion) Assert.AreEqual(4, maxTravel.Count(v => v >= .8f), "Every actor needs an actual supported arrival");
            Assert.Less(Vector3.Distance(savedEye, _camera.transform.position), .001f);
            Assert.Less(Quaternion.Angle(savedTurn, _camera.transform.rotation), .01f);
            // Cancellation in the middle must put every model and view back exactly.
            Call("Sample", arrivalEnd * .4f, reduced, map); yield return null;
            _arrival.Cancel();
            for (int i = 0; i < 4; i++)
            {
                Assert.Less(Vector3.Distance(rests[i], roots[i].localPosition), .001f);
                Assert.Less(Quaternion.Angle(turns[i], roots[i].localRotation), .01f);
            }
            facts.Add("maxTravel=" + string.Join(",", maxTravel));
            facts.Add("minimumSoleAboveFloor=" + string.Join(",", minimumFloor));
            File.WriteAllLines(Path.Combine(output, "composition.txt"), facts);
        }

        private static IEnumerable<Vector3> RenderedPoints(CharacterMotor actor, bool feetOnly = false)
        {
            var mesh = new Mesh(); var vertices = new List<Vector3>();
            try
            {
                foreach (var skin in actor.GetComponent<CharacterVisual>().Model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (!skin.enabled || skin.sharedMesh == null) continue;
                    skin.BakeMesh(mesh, true); mesh.GetVertices(vertices);
                    var world = Matrix4x4.TRS(skin.transform.position, skin.transform.rotation, Vector3.one);
                    var weights = skin.sharedMesh.boneWeights; var bones = skin.bones;
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        if (feetOnly && (weights[i].weight0 < .5f || !bones[weights[i].boneIndex0].name.StartsWith("leg-"))) continue;
                        yield return world.MultiplyPoint3x4(vertices[i]);
                    }
                }
            }
            finally { Object.Destroy(mesh); }
        }
        private static float SoleHeight(CharacterMotor actor) => RenderedPoints(actor, true).Min(p => p.y);
        private static float FloorUnder(Vector3 at, float expected)
        {
            var hits = Physics.RaycastAll(at + Vector3.up * 1.25f, Vector3.down, 2.5f, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h.normal.y > .65f && h.collider.GetComponentInParent<CharacterMotor>() == null
                    && h.collider.GetComponentInParent<Slipper>() == null && h.collider.GetComponentInParent<Lata>() == null);
            return hits.OrderBy(h => Mathf.Abs(h.point.y - expected)).First().point.y;
        }
        private void Capture(string path)
        {
            var prior = RenderTexture.active; var texture = new Texture2D(_target.width, _target.height, TextureFormat.RGB24, false);
            try
            {
                _camera.Render(); RenderTexture.active = _target;
                texture.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0); texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { RenderTexture.active = prior; Object.Destroy(texture); }
        }
    }

    [DefaultExecutionOrder(9000)]
    public sealed class DirectedArrivalFrameObserver : MonoBehaviour
    {
        public System.Action Observe;
        private void LateUpdate()
        {
            var callback = Observe; Observe = null;
            callback?.Invoke();
        }
    }

}
