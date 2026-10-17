using System;
using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Run
{
    /// <summary>
    /// ⚠️⚠️ PAETE'S RUN, THE MODE: an obstacle race, not the tag game. Owner, 2026-10-10: "everyone is paete"; "race of
    /// several players"; "fall is just the same as if you fall in mariokart"; how it is entered: "we'll figure it out
    /// later". The course is data (<see cref="RunLayout"/>); the scene is `Scenes/Maps/PaeteRun.unity`, built by
    /// `Editor/MapKit/PaeteRunSceneBuilder.cs` and registered NOWHERE (no map list, no menu, no build settings).
    ///
    /// WHAT IT DOES, START TO FINISH:
    ///   * NO TAG GAME. Like the prototype map (`Diagnostics.PrototypeMapPlay`), it asks for the Training Range before
    ///     the launcher wakes: one body you control, no round to win, no bots. Then it freezes and hides the can, puts
    ///     the slippers away, switches off the chalk box's drawing, and makes the player PAETE.
    ///   * 3, 2, 1, GO on the game's own countdown (`UI.Hud.ShowCountdownTick`), bodies held on the start grid.
    ///   * A RACE CLOCK from GO, off <see cref="RunClock"/>.
    ///   * CHECKPOINTS TAKEN IN ORDER: one at the start of every leg, the finish, and one in the middle of each
    ///     shortcut. A racer is "at" the last one he reached, and may next reach any checkpoint that one leads to (the
    ///     next on the course, or a shortcut's).
    ///   * A FALL IS MARIO KART'S: under the height his leg is counted from (or far off the course for a while, or
    ///     Backspace), he is picked up, carried back in about two seconds and set down on his last checkpoint facing
    ///     down the course. The clock keeps running. Nothing else is lost.
    ///   * THE VINE IS HIS TO USE ALL THE WAY: LIANA LEAP's 30 s cooldown is a balance number of the tag game, so here
    ///     it is brought down to <see cref="VineCooldown"/> a second after each cast (`HeroAbility.ReduceCooldown`).
    ///   * STAMINA IS ENDLESS (<see cref="EndlessStamina"/>, a switch): a body runs 5 m/s only with sprint held and
    ///     walks 2.5, and every gap on the course is sized for the run. ⚠️ That is this mode's choice, not the owner's
    ///     word; turn it off to race on the real bar.
    ///   * THE CHUTES CARRY: in a slide chute a body is swept down the bed (`CharacterMotor.BeginCarry`, renewed each
    ///     step, 7 m/s rising to 13), and steers across it. The movement rework's own slide lasts about a second and
    ///     gains nothing from a slope; a chute that only worked while that slide lasted would be five metres long.
    ///   * THE WATER SWIMS: the layout's swim volumes are handed to `RooftopPool.MapWater`.
    ///   * AT THE FINISH: place and time for every racer, and R (or Enter, or Y on a pad) runs it again.
    ///
    /// BOTS: none, on purpose. A bot walks a route; it cannot swing, swim or ride on purpose.
    ///
    /// ⚠️⚠️ ONLINE IS DESIGNED FOR AND NOT BUILT. Nothing here sends anything. What a session would need is at the
    /// foot of this file (`ONLINE`). In a networked session this component logs once and stands down.
    ///
    /// ⚠️ NOTHING IN THE MOTOR WAS CHANGED FOR THIS. The pick-up places the body with `CharacterMotor.Teleport` every
    /// physics step (which also holds it still: a teleport's settle frames), the chute is a carry, the pendulums are a
    /// launch, a moving platform moves the controller itself (<see cref="RunMovers"/>).
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class RunMode : MonoBehaviour
    {
        public enum Phase { Loading, Countdown, Racing, Finished }

        public sealed class Racer
        {
            public CharacterMotor Body;
            /// <summary>The last checkpoint reached (an index into the layout's checkpoints).</summary>
            public int Node;
            public bool Finished, Returning;
            public float FinishTime = -1f, Progress, WrongWay, OffCourse, ReturnStart;
            public int Place = 1, Falls, Taken;
            public Vector3 ReturnFrom;
            public string ReturnWhy = "";
            internal LineRenderer Vine;
        }

        [Tooltip("Art/PaeteRun/paete_run_layout.json")]
        public TextAsset Layout;
        public RunMovers Movers;
        public float CountdownSeconds = 3f;
        /// <summary>How long a fall costs: caught, carried back and set down.</summary>
        public float ReturnSeconds = 2.0f;
        /// <summary>LIANA LEAP is ready again this long after a cast (the tag game's is 30 s).</summary>
        public float VineCooldown = 1.0f;
        /// <summary>Every racer's `CharacterMotor.BodySpeedScale`. 1 is the game's own speed.</summary>
        public float SpeedScale = 1.0f;
        public bool EndlessStamina = true;
        /// <summary>On the ground this far from his own legs of the course for this long, he is picked up as from a fall.</summary>
        public float OffCourseMetres = 18f, OffCourseSeconds = 2.5f;

        public static RunMode Instance { get; private set; }
        public RunLayout Data { get; private set; }
        public Phase State { get; private set; } = Phase.Loading;
        public IReadOnlyList<Racer> Racers => _racers;
        public Racer Local { get; private set; }
        public int MainCheckpoints => Data != null ? Data.finish : 0;
        /// <summary>Seconds since GO; frozen at the local racer's own time once he has finished.</summary>
        public float RaceTime => State == Phase.Racing ? (float)(RunClock.Now - _startedAt)
                                : State == Phase.Finished ? (Local != null && Local.Finished ? Local.FinishTime : _raceLength) : 0f;
        public float CountdownLeft => State == Phase.Countdown ? Mathf.Max(0f, CountdownSeconds - (Time.time - _countAt)) : 0f;

        private readonly List<Racer> _racers = new List<Racer>();
        private readonly List<Racer> _order = new List<Racer>();
        private RooftopPool.WaterRule _rule;
        private WaterBox[] _water = Array.Empty<WaterBox>();
        private double _startedAt;
        private float _countAt, _raceLength, _quietAt, _hideCountdownAt = -1f, _finishedAt;
        private int _lastTick = -1;
        private bool _solo, _warnedOnline;
        private RunHud _hud;

        private struct WaterBox { public Vector3 Centre; public float Cos, Sin, HalfX, HalfZ, Surface, Floor; }

        // ------------------------------------------------------------------ life

        private void Awake()
        {
            Instance = this;
            Data = RunLayout.Parse(Layout != null ? Layout.text : null);
            if (Data == null)
            {
                Debug.LogError("[PaeteRun] no layout on the mode (Art/PaeteRun/paete_run_layout.json): the race is off. Rebuild the scene.");
                enabled = false;
                return;
            }
            BuildWater();
            _solo = !NetAuthority.IsNetworked && !UI.SceneFlow.Networked;
            if (!_solo) return;
            // ⚠️ SET HERE, BEFORE THE MAP'S LAUNCHER WAKES (execution order -10000), as the prototype map does and for its
            // reason: a hook that runs on script reload does not run when the editor enters Play without reloading.
            GameLaunch.Reset();
            GameLaunch.Spectator = false;
            GameLaunch.AllBots = false;
            GameLaunch.TrainingRange = true;
            GameLaunch.SelectedMap = "eskinita";
            UI.SceneFlow.Networked = false;
            UI.SceneFlow.SelectedMode = GameMode.HeroStrike;
            Debug.Log("[PaeteRun] the race asked for a scene with no tag round (the Training Range's launch).");
        }

        private void OnEnable()
        {
            if (Data == null) return;
            _rule = Water;
            RooftopPool.MapWater = _rule;
        }

        private void OnDisable()
        {
            if (_rule != null && RooftopPool.MapWater == _rule) RooftopPool.MapWater = null;
            foreach (var r in _racers)
            {
                if (r.Body != null && !Mathf.Approximately(SpeedScale, 1f)) r.Body.BodySpeedScale = 1f;
                if (r.Vine != null) Destroy(r.Vine.gameObject);
            }
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------ water

        private void BuildWater()
        {
            var list = new List<WaterBox>();
            foreach (var w in Data.water ?? Array.Empty<RunLayout.Water>())
            {
                if (w == null) continue;
                float yaw = w.yaw * Mathf.Deg2Rad;
                list.Add(new WaterBox { Centre = w.c, Cos = Mathf.Cos(yaw), Sin = Mathf.Sin(yaw), HalfX = w.s.x * 0.5f, HalfZ = w.s.z * 0.5f, Surface = w.surface, Floor = w.floor });
            }
            _water = list.ToArray();
        }

        /// <summary>`RooftopPool.MapWater`: the swim volume a point is in or over, if any.</summary>
        private bool Water(Vector3 p, out float surface, out float floor)
        {
            for (int i = 0; i < _water.Length; i++)
            {
                var w = _water[i];
                if (p.y < w.Floor - 0.3f || p.y > w.Surface + 40f) continue;
                float dx = p.x - w.Centre.x, dz = p.z - w.Centre.z;
                // Into the box's own frame: its length runs along its yaw.
                float across = dx * w.Cos - dz * w.Sin, along = dx * w.Sin + dz * w.Cos;
                if (Mathf.Abs(across) > w.HalfX || Mathf.Abs(along) > w.HalfZ) continue;
                surface = w.Surface; floor = w.Floor;
                return true;
            }
            surface = 0f; floor = 0f;
            return false;
        }

        // ------------------------------------------------------------------ the frame

        private void Update()
        {
            if (Data == null) return;
            if (!_solo)
            {
                if (!_warnedOnline) { _warnedOnline = true; Debug.LogWarning("[PaeteRun] this is a networked session: the race is NOT built for it (see ONLINE in RunMode.cs) and stands down."); }
                return;
            }
            if (State == Phase.Loading)
            {
                var range = PracticeRange.Instance;
                if (PracticeRange.Active && range != null && range.CanEdit && range.Local != null) Setup(range);
                return;
            }
            if (Time.time >= _quietAt) { _quietAt = Time.time + 1f; Quiet(PracticeRange.Instance); }
            if (_hideCountdownAt >= 0f && Time.time >= _hideCountdownAt) { _hideCountdownAt = -1f; UI.Hud.Instance?.HideCountdown(); }

            float dt = Time.deltaTime;
            foreach (var r in _racers)
            {
                if (r.Body == null) continue;
                if (!r.Body.IsLocallySimulated()) continue;
                if (EndlessStamina) r.Body.Stamina.RefillAndClearFatigue();
                var vine = r.Body.AbilitySystem != null && r.Body.AbilitySystem.Kit != null ? r.Body.AbilitySystem.Kit.Skill1 : null;
                if (vine != null && vine.CooldownRemaining > VineCooldown) vine.ReduceCooldown(vine.CooldownRemaining - VineCooldown);
            }

            if (State == Phase.Countdown) StepCountdown();
            else if (State == Phase.Racing)
            {
                foreach (var r in _racers) StepRacer(r, dt);
                Rank();
                if (Local != null && Local.Finished) { State = Phase.Finished; _finishedAt = Time.time; _raceLength = Local.FinishTime; }
            }
            else if (State == Phase.Finished)
            {
                foreach (var r in _racers) StepRacer(r, dt);
                Rank();
                if (Time.time - _finishedAt > 1.0f && AgainPressed()) Restart();
            }

            var keys = UnityEngine.InputSystem.Keyboard.current;
            if (State == Phase.Racing && Local != null && !Local.Returning && !Local.Finished && keys != null && keys.backspaceKey.wasPressedThisFrame && !UI.Panel.AnyOpen)
                BeginReturn(Local, "asked");
            if (_hud != null) _hud.Tick(this);
        }

        private static bool AgainPressed()
        {
            if (UI.Panel.AnyOpen) return false;
            var keys = UnityEngine.InputSystem.Keyboard.current;
            var pad = UnityEngine.InputSystem.Gamepad.current;
            return (keys != null && (keys.rKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame || keys.numpadEnterKey.wasPressedThisFrame))
                   || (pad != null && pad.buttonNorth.wasPressedThisFrame);
        }

        private void FixedUpdate()
        {
            if (Data == null || !_solo || State == Phase.Loading || PresentationClock.Held) return;
            for (int i = 0; i < _racers.Count; i++)
            {
                var r = _racers[i];
                if (r.Body == null || !r.Body.IsLocallySimulated()) continue;
                if (State == Phase.Countdown) { HoldOnGrid(r, i); continue; }
                if (r.Returning) { StepReturn(r); continue; }
                if (r.Finished) continue;
                StepChutes(r);
            }
        }

        // ------------------------------------------------------------------ setting the scene up

        private void Setup(PracticeRange range)
        {
            Local = null;
            _racers.Clear();
            Quiet(range);

            // Everyone is Paete.
            var local = range.Local;
            var roster = Roster.GetPeople(local.Mode);
            int paete = -1;
            for (int i = 0; i < roster.Count; i++) if (roster[i].Id == "paete") { paete = i; break; }
            if (paete < 0 || !range.ChangeCharacter(paete))
                Debug.LogWarning("[PaeteRun] could not make the player Paete (no 'paete' in this mode's roster, or his art is missing): the race runs with whoever he is, and no vine.");

            var players = GameServices.Round != null ? GameServices.Round.Players : null;
            if (players != null)
                foreach (var p in players)
                    if (p != null && p.gameObject.activeInHierarchy) _racers.Add(new Racer { Body = p });
            if (_racers.Find(r => r.Body == local) == null) _racers.Add(new Racer { Body = local });
            Local = _racers.Find(r => r.Body == local);
            foreach (var r in _racers) if (!Mathf.Approximately(SpeedScale, 1f)) r.Body.BodySpeedScale = SpeedScale;

            if (Movers == null) Movers = FindFirstObjectByType<RunMovers>();
            _hud = gameObject.GetComponent<RunHud>();
            if (_hud == null) _hud = gameObject.AddComponent<RunHud>();
            _hud.Build(this);
            Debug.Log("[PaeteRun] ready: " + _racers.Count + " racer(s), " + Data.checkpoints.Length + " checkpoints, " + Data.water.Length + " pools, " + Data.chutes.Length + " chutes.");
            Restart();
        }

        /// <summary>The tag game put away: the can frozen and unseen, the slippers gone, the chalk box not drawn. Asked again each
        /// second, because the range's own reset (its pause menu) hands everything back.</summary>
        private void Quiet(PracticeRange range)
        {
            if (range != null && range.CanEdit && !range.Value(PracticeRange.Cheat.FreezeCan)) range.Set(PracticeRange.Cheat.FreezeCan, true);
            foreach (var can in FindObjectsByType<Lata>(FindObjectsSortMode.None))
            {
                foreach (var r in can.GetComponentsInChildren<Renderer>(true)) if (r.enabled) r.enabled = false;
                foreach (var c in can.GetComponentsInChildren<Collider>(true)) if (c.enabled) c.enabled = false;
            }
            var runner = FindFirstObjectByType<SliceRunner>();
            if (runner != null && runner.Slippers != null)
                foreach (var shoe in runner.Slippers)
                    if (shoe != null && shoe.gameObject.activeSelf) { shoe.HostDisarm(); shoe.gameObject.SetActive(false); }
            var chalk = FindFirstObjectByType<Visual.CourtBoundaryPresentation>();
            if (chalk != null && chalk.gameObject.activeSelf) chalk.gameObject.SetActive(false);
            var canClock = FindFirstObjectByType<Visual.LataClockPresentation>();
            if (canClock != null && canClock.gameObject.activeSelf) canClock.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ start

        /// <summary>Everybody back on the grid and the countdown again. Also the first start.</summary>
        public void Restart()
        {
            if (Data == null) return;
            if (Movers != null) Movers.ResetAll();
            for (int i = 0; i < _racers.Count; i++)
            {
                var r = _racers[i];
                r.Node = 0; r.Finished = false; r.Returning = false; r.FinishTime = -1f; r.Progress = 0f; r.WrongWay = 0f; r.OffCourse = 0f;
                r.Falls = 0; r.Taken = 0; r.Place = 1;
                if (r.Vine != null) r.Vine.enabled = false;
                if (r.Body == null) continue;
                var pose = GridPose(i);
                r.Body.SpawnPosition = pose.p;
                if (r.Body.IsLocallySimulated()) { r.Body.Teleport(pose.p); r.Body.transform.rotation = Quaternion.Euler(0f, pose.yaw, 0f); }
            }
            State = Phase.Countdown;
            _countAt = Time.time; _lastTick = -1; _raceLength = 0f;
        }

        private RunLayout.Pose GridPose(int i)
        {
            var grid = Data.start_grid;
            if (grid == null || grid.Length == 0) return new RunLayout.Pose { p = Data.checkpoints[0].p, yaw = Data.checkpoints[0].yaw };
            return grid[i % grid.Length];
        }

        /// <summary>Held where he stands on the grid until GO. He can look round: only his feet are held.</summary>
        private void HoldOnGrid(Racer r, int i)
        {
            var pose = GridPose(i);
            Vector3 p = r.Body.transform.position;
            if (new Vector2(p.x - pose.p.x, p.z - pose.p.z).sqrMagnitude > 0.01f || Mathf.Abs(p.y - pose.p.y) > 0.6f) r.Body.Teleport(pose.p);
            else r.Body.Teleport(new Vector3(pose.p.x, p.y, pose.p.z));
        }

        private void StepCountdown()
        {
            float left = CountdownSeconds - (Time.time - _countAt);
            int tick = Mathf.CeilToInt(left);
            if (left > 0f)
            {
                if (tick != _lastTick && tick <= 3) { _lastTick = tick; UI.Hud.Instance?.ShowCountdownTick(tick.ToString()); }
                return;
            }
            UI.Hud.Instance?.ShowCountdownTick("GO!");
            _hideCountdownAt = Time.time + 0.9f;
            _startedAt = RunClock.Now;
            State = Phase.Racing;
        }

        // ------------------------------------------------------------------ a racer's frame

        private void StepRacer(Racer r, float dt)
        {
            var body = r.Body;
            if (body == null || r.Returning) return;
            Vector3 p = body.transform.position;
            var node = Data.checkpoints[r.Node];
            if (!r.Finished)
            {
                foreach (int id in node.next)
                {
                    if (id < 0 || id >= Data.checkpoints.Length) continue;
                    var cp = Data.checkpoints[id];
                    if (!Reached(cp, p)) continue;
                    Take(r, cp);
                    node = cp;
                    break;
                }
            }
            if (r.Finished) { r.Progress = node.progress; return; }

            // Where he is along the course, for his place.
            var ahead = NextMain(node);
            if (ahead != null)
            {
                float whole = Vector3.Distance(node.p, ahead.p), left = Vector3.Distance(p, ahead.p);
                r.Progress = node.progress + Mathf.Clamp01(1f - left / Mathf.Max(0.1f, whole)) * (ahead.progress - node.progress);
                // The wrong way: moving, and steadily away from every checkpoint he may take next.
                Vector3 travel = body.Velocity; travel.y = 0f;
                bool away = travel.sqrMagnitude > 4f;
                if (away)
                    foreach (int id in node.next)
                    {
                        Vector3 to = Data.checkpoints[id].p - p; to.y = 0f;
                        if (to.sqrMagnitude < 1f || Vector3.Dot(travel.normalized, to.normalized) > -0.5f) { away = false; break; }
                    }
                r.WrongWay = away ? r.WrongWay + dt : Mathf.Max(0f, r.WrongWay - dt * 2f);
            }

            if (!body.IsLocallySimulated() || State != Phase.Racing) return;
            if (p.y < node.fall_y || p.y < Data.kill_y) { BeginReturn(r, "fell"); return; }
            if (body.IsGrounded && !body.IsSwimming && OffCourseBy(node, p) > OffCourseMetres) r.OffCourse += dt; else r.OffCourse = 0f;
            if (r.OffCourse > OffCourseSeconds) BeginReturn(r, "off the course");
        }

        private static bool Reached(RunLayout.Checkpoint cp, Vector3 p)
        {
            float dx = p.x - cp.p.x, dz = p.z - cp.p.z;
            return dx * dx + dz * dz <= cp.radius * cp.radius && Mathf.Abs(p.y - cp.p.y) <= cp.half_height;
        }

        private void Take(Racer r, RunLayout.Checkpoint cp)
        {
            r.Node = cp.id; r.Taken++; r.OffCourse = 0f; r.WrongWay = 0f;
            if (r.Body != null) r.Body.SpawnPosition = cp.p;
            if (cp.id != Data.finish) return;
            r.Finished = true;
            r.FinishTime = (float)(RunClock.Now - _startedAt);
            if (r == Local) UI.Hud.Instance?.ShowToast("FINISH  " + RunHud.Clock(r.FinishTime), 3f);
            Debug.Log("[PaeteRun] " + (r.Body != null ? r.Body.name : "a racer") + " finished in " + RunHud.Clock(r.FinishTime) + " with " + r.Falls + " fall(s).");
        }

        /// <summary>The next checkpoint ON THE COURSE from this one (a shortcut's middle leads back to the course).</summary>
        public RunLayout.Checkpoint NextMain(RunLayout.Checkpoint node)
        {
            if (node == null || node.next == null || node.next.Length == 0) return null;
            int best = node.next[0];
            foreach (int id in node.next) if (Data.checkpoints[id].shortcut < 0) { best = id; break; }
            return Data.checkpoints[best];
        }

        /// <summary>How far a point is from the legs that leave this checkpoint.</summary>
        private float OffCourseBy(RunLayout.Checkpoint node, Vector3 p)
        {
            if (node.next == null || node.next.Length == 0) return 0f;
            float best = float.MaxValue;
            foreach (int id in node.next)
            {
                Vector3 a = node.p, b = Data.checkpoints[id].p, ab = b - a;
                float t = ab.sqrMagnitude > 1e-4f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
                best = Mathf.Min(best, Vector3.Distance(p, a + ab * t));
            }
            return best;
        }

        private void Rank()
        {
            _order.Clear(); _order.AddRange(_racers);
            _order.Sort((a, b) =>
            {
                if (a.Finished != b.Finished) return a.Finished ? -1 : 1;
                if (a.Finished) return a.FinishTime.CompareTo(b.FinishTime);
                return b.Progress.CompareTo(a.Progress);
            });
            for (int i = 0; i < _order.Count; i++) _order[i].Place = i + 1;
        }

        // ------------------------------------------------------------------ the fall, Mario Kart's

        /// <summary>Picked up where he is and carried back to his last checkpoint. The clock keeps running.</summary>
        public void BeginReturn(Racer r, string why)
        {
            if (r == null || r.Body == null || r.Returning) return;
            r.Returning = true; r.ReturnStart = Time.time; r.ReturnWhy = why; r.Falls++;
            r.ReturnFrom = r.Body.transform.position;
            r.OffCourse = 0f; r.WrongWay = 0f;
            if (r == Local) UI.Hud.Instance?.ShowToast("BACK TO THE CHECKPOINT", 1.6f);
        }

        private void StepReturn(Racer r)
        {
            var cp = Data.checkpoints[r.Node];
            float elapsed = Time.time - r.ReturnStart;
            const float catchSeconds = 0.25f, setSeconds = 0.35f;
            float carrySeconds = Mathf.Max(0.3f, ReturnSeconds - catchSeconds - setSeconds);
            Vector3 caught = r.ReturnFrom + Vector3.down * 1.0f, over = cp.p + Vector3.up * 1.6f, at;
            if (elapsed < catchSeconds)
            {
                // The fall is taken up over a metre, not stopped dead.
                float u = elapsed / catchSeconds;
                at = Vector3.Lerp(r.ReturnFrom, caught, 1f - (1f - u) * (1f - u));
            }
            else if (elapsed < catchSeconds + carrySeconds)
            {
                float u = Mathf.SmoothStep(0f, 1f, (elapsed - catchSeconds) / carrySeconds);
                at = Vector3.Lerp(caught, over, u);
                at.y += Mathf.Sin(u * Mathf.PI) * 3.5f;        // up and over, never a straight drag through the course
            }
            else
            {
                float u = Mathf.SmoothStep(0f, 1f, (elapsed - catchSeconds - carrySeconds) / setSeconds);
                at = Vector3.Lerp(over, cp.p, u);
            }
            bool done = elapsed >= catchSeconds + carrySeconds + setSeconds;
            if (done) at = cp.p;
            r.Body.Teleport(at);
            // Turned to face down the course as he is carried, so he is set down already looking the right way.
            var facing = Quaternion.Euler(0f, cp.yaw, 0f);
            r.Body.transform.rotation = done ? facing : Quaternion.RotateTowards(r.Body.transform.rotation, facing, 360f * Time.fixedDeltaTime);
            ShowVine(r, !done, at);
            if (done) r.Returning = false;
        }

        /// <summary>What carries him: a vine from overhead. A stand-in the art can replace.</summary>
        private void ShowVine(Racer r, bool on, Vector3 feet)
        {
            if (!on) { if (r.Vine != null) r.Vine.enabled = false; return; }
            if (r.Vine == null)
            {
                var go = new GameObject("Run return vine");
                go.transform.SetParent(transform, false);
                r.Vine = go.AddComponent<LineRenderer>();
                var shader = Shader.Find("Sprites/Default");
                if (shader != null) r.Vine.material = new Material(shader);
                r.Vine.positionCount = 2; r.Vine.useWorldSpace = true;
                r.Vine.startWidth = 0.16f; r.Vine.endWidth = 0.10f;
                r.Vine.startColor = new Color(0.20f, 0.45f, 0.16f); r.Vine.endColor = new Color(0.42f, 0.66f, 0.22f);
                r.Vine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            r.Vine.enabled = true;
            r.Vine.SetPosition(0, feet + Vector3.up * 16f);
            r.Vine.SetPosition(1, feet + Vector3.up * 1.35f);
        }

        // ------------------------------------------------------------------ the chutes

        private void StepChutes(Racer r)
        {
            if (Data.chutes == null || State != Phase.Racing) return;
            Vector3 p = r.Body.transform.position;
            foreach (var chute in Data.chutes)
            {
                if (chute == null || chute.points == null) continue;
                if (!OnChute(chute, p, out Vector3 way, out float along)) continue;
                float speed = Mathf.Min(chute.speed, chute.start_speed + chute.gain * along);
                // The speed is his to keep as he leaves the bed (`CharacterMotor.StepCarry` hands it to the body).
                r.Body.KeepCarryMomentumFor(0.3f);
                r.Body.BeginCarry(way * speed, 0.06f);
                return;
            }
        }

        /// <summary>Whether a point is in a chute, which way the bed runs there, and how far down it he is.</summary>
        private static bool OnChute(RunLayout.Chute chute, Vector3 p, out Vector3 way, out float along)
        {
            way = Vector3.forward; along = 0f;
            float half = chute.width * 0.5f + 0.7f;
            var pts = chute.points;
            for (int i = 0; i + 1 < pts.Length; i++)
            {
                Vector3 a = pts[i], b = pts[i + 1];
                Vector3 ab = new Vector3(b.x - a.x, 0f, b.z - a.z);
                float len = ab.magnitude;
                if (len < 0.05f) continue;
                Vector3 dir = ab / len;
                float t = ((p.x - a.x) * dir.x + (p.z - a.z) * dir.z) / len;
                if (t >= 0f && t <= 1f)
                {
                    float side = Mathf.Abs((p.x - a.x) * dir.z - (p.z - a.z) * dir.x);
                    float bed = Mathf.Lerp(a.y, b.y, t);
                    if (side <= half && p.y >= bed - chute.below && p.y <= bed + chute.above) { way = dir; along += len * t; return true; }
                }
                along += len;
            }
            return false;
        }

        // ------------------------------------------------------------------ for the play probe

        public Racer RacerOf(CharacterMotor who) => _racers.Find(r => r.Body == who);

        /// <summary>The probe's: stand a racer on a checkpoint as if he had just reached it, facing down the course.</summary>
        public bool DebugPlace(CharacterMotor who, int node)
        {
            var r = RacerOf(who);
            if (r == null || Data == null || node < 0 || node >= Data.checkpoints.Length) return false;
            var cp = Data.checkpoints[node];
            r.Node = node; r.Returning = false; r.Finished = false; r.FinishTime = -1f; r.OffCourse = 0f; r.WrongWay = 0f;
            if (r.Vine != null) r.Vine.enabled = false;
            who.SpawnPosition = cp.p;
            who.Teleport(cp.p);
            who.transform.rotation = Quaternion.Euler(0f, cp.yaw, 0f);
            if (State == Phase.Finished) { State = Phase.Racing; }
            return true;
        }

        /* ------------------------------------------------------------------ ONLINE (designed for, NOT built)

        Nothing in this mode is on the wire, and protocol 158 is untouched. What a session of several players needs:

        ALREADY TRUE, NOTHING TO SEND
          * Each peer simulates its own body and the others are replicas of the poses already sent: running, hopping,
            swimming, the geysers (`JumpPad` launches only bodies this peer simulates) and the vine's swing (walked by
            its owner, `CharacterMotor.BeginSwing`) are the game's own.
          * Every mover is a function of `RunClock`, which in a session is Netcode's server time: no mover is sent.
          * Checkpoints, place and the finish can be worked out BY EVERY PEER FOR EVERY BODY from the poses it already
            receives (this file does exactly that for whoever is in `_racers`), so nobody has to report "I finished".
            Two peers would agree on a time to about the latency of the pose stream.
          * The pick-up after a fall is walked by the body's owner with `Teleport`; the host relays a teleport it makes
            for its own bodies already (`MatchRpc.BroadcastTeleport`).

        NEEDS THE OWNER'S WORD (each is a change on the wire or in a shared rule)
          1. ENTRY: the mode stands down when networked. A session needs a way in (a map id, or a flag on the launch),
             which is a registered scene and a lobby choice.
          2. NO TAG ROUND ONLINE: offline the Training Range supplies "a scene with no round". Online there is no such
             launch: `MatchInstaller` would deal a taya, a can and slippers. Needs a "no round" switch the host and the
             clients agree on.
          3. THE START: one agreed GO time. The round's own start is replicated and could be reused; otherwise one
             host message (a server time).
          4. EVERYONE AS PAETE: hero choice is replicated per seat; the mode would have to force it at the host.
          5. THE VINE'S COOLDOWN: the host resolves LIANA LEAP and keeps its own 30 s clock for every seat. Every peer
             running this mode lowers it the same way, but the host's copy is the one that decides, so it must run there.
          6. A CLIENT'S OWN PICK-UP: `Teleport` on a client moves its body, and the host's move check
             (`MatchRpc.AcceptMove`, a distance budget of 30 m/s) may refuse a carry of 40 m in 1.4 s. Either the carry
             is slowed to under the budget, or the host is told (a message).
          7. SPEEDS: a geyser throws 25 m/s up and a chute carries 13 m/s; both are inside the budget. A long fall is
             capped at 25 m/s by the motor.
          8. THE CRUMBLING SLABS are per peer (see `RunMovers`). Exact for everyone needs a message or a schedule.
          9. AN AUTHORITATIVE RESULT (a ladder, a record): then the host's reading of the finish is the one that counts
             and has to be sent, one small message a finisher.
        */
    }
}
