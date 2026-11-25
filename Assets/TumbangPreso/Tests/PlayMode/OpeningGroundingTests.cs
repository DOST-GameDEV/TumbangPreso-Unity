using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Map;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// NOTHING IN A MATCH OPENING HANGS IN THE AIR (owner, 2026-11-26: "shit in the opening cutscenes would float").
    /// Each of the six courts plays its real opening (`MapFilm` on five, `ArenaIntro` on the Arena) on its own clock, and
    /// every drawn frame is measured after the film has posed it: each seat's drawn model, each body, each tsinelas and
    /// the can against the floor under it, and every object the film itself built against the floor under its own
    /// footprint. The film's own objects are reported (some are meant to be up: a line of washing, a banca on the water,
    /// a drone); the cast, the bodies, the tsinelas and the can are asserted.
    /// </summary>
    public sealed class OpeningGroundingTests
    {
        private const float Float = 0.15f, Sunk = -0.30f;

        /// <summary>
        /// Off the floor for this long without a break is hanging in the air. A film's jumping cheer (`MapFilm.Cheer`,
        /// 0.36 m every 0.42 s) is above `Float` for about 0.3 s at a time and lands between hops.
        /// </summary>
        private const float Hanging = 0.5f;

        /// <summary>
        /// Beats in which the cast is meant to be up, on something the court floor does not count: the Arena's drones,
        /// the banca on the water at Sitangkai, the jeepney's back step and the bicycle at BGC.
        /// </summary>
        private static readonly Dictionary<string, string[]> Aboard = new Dictionary<string, string[]>
        {
            { SceneFlow.Arena, new[] { "Build", "Handoff" } },
            { SceneFlow.LagoonCove, new[] { "glide", "banca" } },
            { SceneFlow.Kanto, new[] { "rush", "jeep", "bike" } },
        };
        private string _settings;
        private CustomRules _rules;
        private bool _rulesPinned;

        [UnitySetUp] public IEnumerator Before()
        {
            _rules = SceneFlow.SelectedRules.Clone(); _rulesPinned = SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
            _settings = JsonUtility.ToJson(Settings.SettingsStore.Current);
        }

        [UnityTearDown] public IEnumerator After()
        {
            MatchOpening.Stop();
            JsonUtility.FromJsonOverwrite(_settings, Settings.SettingsStore.Current);
            Time.timeScale = 1.0f;
            yield return PlayModeWorld.Reset();
            SceneFlow.AdoptRemoteRules(_rules);
            if (_rulesPinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        private sealed class Worst
        {
            public string what, map, beat; public float gap, age; public int samples, over;
            public Vector3 at; public float floor;
            public float runFrom = -1.0f; public bool failed;
        }

        private readonly Dictionary<string, Worst> _worst = new Dictionary<string, Worst>();
        private readonly List<string> _failures = new List<string>();

        [UnityTest, Timeout(1800000)]
        public IEnumerator EveryOpeningKeepsItsCastAndPropsOnTheFloor()
        {
            LogAssert.ignoreFailingMessages = true;
            foreach (string map in new[] { SceneFlow.Eskinita, SceneFlow.IlalimNgTulay, SceneFlow.LagoonCove, SceneFlow.Kanto, SceneFlow.Arena, SceneFlow.Payo })
            {
                yield return PlayModeWorld.Reset();
                yield return Opening(map, map == SceneFlow.Payo ? 0 : -1);
            }
            // Banaue's blocks rise and sink by layout (the can from 0.6 m below the plaza to 1.2 m above it), and the
            // opening walks the cast up over whichever is in: every layout is played.
            for (int layout = 1; layout < _payoLayouts; layout++)
            {
                yield return PlayModeWorld.Reset();
                yield return Opening(SceneFlow.Payo, layout);
            }
            string report = Report();
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "opening-grounding"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "report.txt"), report);
            Debug.Log("[OpeningGrounding]\n" + report);
            Assert.IsEmpty(_failures, string.Join("\n", _failures));
        }

        private int _payoLayouts;

        private IEnumerator Opening(string map, int layout = -1)
        {
            SceneFlow.Networked = false;
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false; GameLaunch.Spectator = false; GameLaunch.AllBots = false;
            Settings.SettingsStore.Current.CinematicCameraMotion = true;
            Settings.SettingsStore.Current.ReducedUiMotion = false;
            yield return SceneManager.LoadSceneAsync(map);
            string label = map;
            if (layout >= 0)
            {
                var stage = Object.FindFirstObjectByType<ArenaStage>();
                Assert.IsNotNull(stage, $"{map} has no stage to hold layout {layout} on");
                _payoLayouts = stage.LayoutCount;
                stage.HoldLayout = layout;
                stage.ApplyForRound(1);
                label = $"{map}#{layout}";
            }

            float waited = 0.0f;
            while (waited < 60.0f && !Playing()) { waited += Time.unscaledDeltaTime; yield return null; }
            Assert.IsTrue(Playing(), $"{map}'s opening never began");

            // Measured after every LateUpdate (the film poses its models after the animator), not from this coroutine,
            // which runs before the pose; and not at end of frame, which batchmode never reaches.
            float start = Time.realtimeSinceStartup, next = 0.0f;
            int frames = 0;
            var late = new GameObject("~OpeningGroundingLate").AddComponent<LateFrameShot>();
            late.Each = () =>
            {
                if (!Playing() || !Rolling() || Time.realtimeSinceStartup < next) return;
                next = Time.realtimeSinceStartup + 0.1f;
                frames++;
                Sample(map, label);
            };
            while (Playing() && Time.realtimeSinceStartup - start < 70.0f) yield return null;
            late.Each = null;
            Object.Destroy(late.gameObject);
            // Stop the match the film opened before the next court: this test is about the opening only.
            MatchOpening.Stop();
            Debug.Log($"[OpeningGrounding] {label}: {frames} frames over {Time.realtimeSinceStartup - start:0.0} s");
            Assert.Greater(frames, 20, $"{label}'s opening was not watched");
        }

        private static bool Playing() => (MapFilm.Instance != null && MapFilm.Instance.Playing) || (ArenaIntro.Instance != null && ArenaIntro.Instance.Playing);
        private static bool Rolling() => MapFilm.Instance != null && MapFilm.Instance.Playing ? !double.IsNaN(MapFilm.Instance.Began)
            : ArenaIntro.Instance != null && ArenaIntro.Instance.Playing && !double.IsNaN(ArenaIntro.Instance.Began);
        private static float Age() => MapFilm.Instance != null && MapFilm.Instance.Playing ? MapFilm.Instance.Age : ArenaIntro.Instance != null ? ArenaIntro.Instance.Age : 0;
        private static string Beat() => MapFilm.Instance != null && MapFilm.Instance.Playing ? MapFilm.Instance.Beat : ArenaIntro.Instance != null ? ArenaIntro.Instance.Now.ToString() : "";
        private static Transform FilmRoot() => MapFilm.Instance != null && MapFilm.Instance.Playing ? MapFilm.Instance.transform : ArenaIntro.Instance != null ? ArenaIntro.Instance.transform : null;

        private void Sample(string court, string map)
        {
            var round = GameServices.Round;
            if (round == null) return;
            float age = Age(); string beat = Beat();
            bool aboard = Aboard.TryGetValue(court, out var beats) && System.Array.IndexOf(beats, beat) >= 0;

            foreach (var body in round.Players)
            {
                if (body == null || !body.gameObject.activeInHierarchy) continue;
                var cc = body.GetComponent<CharacterController>();
                Vector3 feet = cc != null ? body.transform.TransformPoint(cc.center - Vector3.up * cc.height * 0.5f) : body.transform.position;
                if (WorldGround.TryBelow(feet, 0.3f, 14f, out float bodyFloor))
                    Note($"{map} body {body.PlayerSlot}", map, beat, age, feet.y - bodyFloor, feet, bodyFloor, true);

                var visual = body.GetComponent<CharacterVisual>();
                if (visual == null || visual.Model == null || !Drawn(visual.Model, out Bounds model)) continue;
                Vector3 probe = new Vector3(model.center.x, model.min.y, model.center.z);
                if (!WorldGround.TryBelow(probe, 0.6f, 14f, out float floor)) continue;
                // A step, a stair or a deck the court floor does not count is still a floor, if it is drawn.
                if (Support(probe, out float under) && under > floor) floor = under;
                Note($"{map} model {body.PlayerSlot}", map, beat, age, model.min.y - floor, probe, floor, !aboard);
            }

            foreach (var shoe in Object.FindObjectsByType<Slipper>())
            {
                if (shoe == null || !shoe.gameObject.activeInHierarchy || shoe.Holder != null || !Drawn(shoe.gameObject, out Bounds b)) continue;
                Vector3 probe = new Vector3(b.center.x, b.min.y, b.center.z);
                if (!WorldGround.TryBelow(probe, 0.3f, 14f, out float floor)) continue;
                Note($"{map} tsinelas {shoe.SeatOfOrigin}", map, beat, age, b.min.y - floor, probe, floor, true);
            }

            if (round.Lata != null && Drawn(round.Lata.gameObject, out Bounds can))
            {
                Vector3 probe = new Vector3(can.center.x, can.min.y, can.center.z);
                if (WorldGround.TryBelow(probe, 0.3f, 14f, out float floor))
                    Note($"{map} can", map, beat, age, can.min.y - floor, probe, floor, true);
            }

            var film = FilmRoot();
            if (film == null) return;
            foreach (Transform piece in film)
            {
                if (!piece.gameObject.activeInHierarchy || !Drawn(piece.gameObject, out Bounds b)) continue;
                if (b.size.y > 30f || b.size.x > 60f) continue;   // a backdrop, a sky card, a screen
                Vector3 probe = new Vector3(b.center.x, b.min.y, b.center.z);
                if (!WorldGround.TryBelow(probe, 0.5f, 20f, out float floor)) continue;
                Note($"{map} film:{piece.name}", map, beat, age, b.min.y - floor, probe, floor, false);
            }
        }

        private void Note(string what, string map, string beat, float age, float gap, Vector3 at, float floor, bool assert)
        {
            if (!_worst.TryGetValue(what, out var w)) _worst[what] = w = new Worst { what = what, map = map, gap = gap, beat = beat, age = age, at = at, floor = floor };
            w.samples++;
            bool bad = gap > Float || gap < Sunk;
            if (bad) w.over++;
            if (Mathf.Abs(gap) > Mathf.Abs(w.gap)) { w.gap = gap; w.beat = beat; w.age = age; w.at = at; w.floor = floor; }
            // Asserted when it stays off the floor (or sunk into it) for `Hanging` without a break.
            if (!bad || !assert) { w.runFrom = -1.0f; return; }
            if (w.runFrom < 0.0f) w.runFrom = age;
            if (!w.failed && age - w.runFrom >= Hanging)
            {
                w.failed = true;
                _failures.Add(string.Format(CultureInfo.InvariantCulture, "{0}: {1:0.000} m off the floor from age {2:0.00} to {3:0.00} ({4}) at {5} over floor {6:0.000}",
                    what, gap, w.runFrom, age, beat, at, floor));
            }
        }

        private string Report()
        {
            var text = new StringBuilder();
            foreach (var w in _worst.Values.OrderBy(v => v.map).ThenByDescending(v => Mathf.Abs(v.gap)))
                text.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0,-48} worst {1,8:0.000} m at age {2,6:0.00} ({3}) off {4}/{5} samples  at ({6:0.00},{7:0.00},{8:0.00}) floor {9:0.000}",
                    w.what, w.gap, w.age, w.beat, w.over, w.samples, w.at.x, w.at.y, w.at.z, w.floor));
            return text.ToString();
        }

        /// <summary>
        /// The highest drawn walkable surface under `feet`, from just above them (bodies, tsinelas and the can ignored).
        /// An invisible blocker does not count: standing on one is floating.
        /// </summary>
        private static bool Support(Vector3 feet, out float y)
        {
            y = float.NegativeInfinity;
            var hits = Physics.RaycastAll(feet + Vector3.up * 0.3f, Vector3.down, 14f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                var c = hit.collider;
                if (c == null || hit.normal.y < 0.65f || c.GetComponentInParent<CharacterMotor>() != null
                    || c.GetComponentInParent<Slipper>() != null || c.GetComponentInParent<Lata>() != null) continue;
                if (!DrawnAt(hit.point)) continue;
                y = hit.point.y;
                return true;
            }
            return false;
        }

        private static bool DrawnAt(Vector3 point)
        {
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || r.forceRenderingOff || r.shadowCastingMode == ShadowCastingMode.ShadowsOnly) continue;
                var b = r.bounds; b.Expand(0.2f);
                if (b.Contains(point)) return true;
            }
            return false;
        }

        private static bool Drawn(GameObject root, out Bounds bounds)
        {
            bool any = false; bounds = default;
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r.forceRenderingOff || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer
                    || r.shadowCastingMode == ShadowCastingMode.ShadowsOnly || r.GetComponentInParent<VfxRenderTag>() != null
                    || r.GetComponentInParent<CharacterNameplate>() != null) continue;
                if (r is SkinnedMeshRenderer || r is MeshRenderer || r is SpriteRenderer)
                {
                    if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
                }
            }
            return any;
        }
    }
}
