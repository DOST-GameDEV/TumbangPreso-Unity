using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.Map
{
    /// <summary>
    /// THE TRANSFORMATION, MADE AN EVENT (owner, 2026-10-05, after playing the map: "map
    /// transformation is so dull, theres no emphasis on it. could also use some screenshake").
    /// The stage already travelled between its layouts in the break (`ArenaStage`); this puts
    /// the light, the sound and the shake on it, in the three beats `ArenaStage.BreakBeats`
    /// names, on an ordinary break of 8 s:
    ///
    ///   1. ALARM (0 to 2.4 s). The picture dims (`ArenaBreakCamera`), the alarm sounds with a
    ///      riser under it, beacons turn round the stage's rim, the next layout's hologram
    ///      sweeps in from the can behind a scanning ring, and its NAME stands over the stage
    ///      as a hologram title. The drones come down over every player, looking, and lock on.
    ///   2. THE MOVE (2.4 to 6.2 s). Every moving platform undocks at once with a jolt, a
    ///      clunk, a thruster burst from under it and arcs along it; the drones lift the
    ///      players clear; then the platforms go one after another, trailing light, and each
    ///      one that arrives LOCKS: its own thud, flash, shock ring, column of light and camera
    ///      punch, a little higher in pitch each time. A pulse chases round the LED barrier.
    ///   3. THE REVEAL (6.35 s). A stadium-wide flash and two shock rings from the can, the
    ///      boom, the lights back to full, the crowd on its feet and roaring, pyro jets and
    ///      confetti from the field's edge, fireworks over the stands, and the players set
    ///      down on their marks.
    ///
    /// ⚠️ PRESENTATION ONLY, AND NOTHING IS SENT. Every moment is a time on the break's shared
    /// clock (`SharedUltimatePhase.Now` against the host's `Began`), which every peer already
    /// has, so every peer fires the same beat on the same frame of the travel. A peer that
    /// joins in the middle of a break plays what is left and none of what it missed.
    ///
    /// ⚠️ THE PLAYERS' LIFT MOVES ONLY WHAT IS DRAWN. The design has the drones lift every
    /// player off the stage while it rebuilds and set them on their marks. A break holds the
    /// simulation, and the real reset is `SliceRunner.ResetWorld`'s teleport at the round's
    /// start, so nothing here touches a motor, a capsule or a transform that is replicated:
    /// only each body's `CharacterVisual.ModelRoot` (the child its model hangs under) is
    /// carried, from where the body stands to where that teleport will put it
    /// (`SliceRunner.SpawnPointFor` with the break's own next taya, on the new layout's floor),
    /// and put back exactly as it was when the break ends.
    ///
    /// HALFTIME IS LEFT ALONE: its own replay and standings cover the stage.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class ArenaShow : MonoBehaviour
    {
        /// <summary>1 on a lock or the reveal, falling away: what the shaft's rings and the
        /// hover emitters brighten by (`ArenaAmbience`).</summary>
        public static float Surge { get; private set; }

        /// <summary>The beacons round the rim, the points of the LED barrier's chase and the pyro jets at the field's edge.</summary>
        private const int Beacons = 8, BarrierLights = 24, Jets = 10;
        /// <summary>The LED barrier's radius and the field's inner edge (the art brief's numbers).</summary>
        private const float BarrierRadius = 80.0f, FieldEdge = 44.0f;
        /// <summary>How high over the stage the players are carried, and the title stands.</summary>
        private const float CarryHeight = 9.0f, TitleHeight = 7.5f;
        private const int MaxLifted = 8;

        private long _match = -1;
        private int _round = -1;
        private float _last;
        private bool[] _started = System.Array.Empty<bool>(), _locked = System.Array.Empty<bool>();
        private float[] _trail = System.Array.Empty<float>();
        private int _locks;
        private readonly Vector3[] _points = new Vector3[8];
        private bool _volley;

        // The title.
        private Canvas _titleCanvas;
        private CanvasGroup _titleGroup;
        private Text _title, _subtitle;
        private string _name = "";
        private int _typed = -1;

        // The lift.
        private readonly CharacterMotor[] _liftBody = new CharacterMotor[MaxLifted];
        private readonly Transform[] _liftRoot = new Transform[MaxLifted];
        private readonly Vector3[] _liftRest = new Vector3[MaxLifted], _liftFrom = new Vector3[MaxLifted], _liftTo = new Vector3[MaxLifted];
        private readonly Quaternion[] _liftTurn = new Quaternion[MaxLifted];
        private readonly ArenaDrone[] _liftDrone = new ArenaDrone[MaxLifted];
        private int _lifted;
        private bool _lifting;
        private Transform _droneRoot;
        private CameraSystem.CameraRig _rig;
        private static readonly RaycastHit[] FloorHits = new RaycastHit[16];

        private void OnDisable()
        {
            EndBreak();
            Surge = 0.0f;
        }

        private void OnDestroy()
        {
            if (_droneRoot != null) Destroy(_droneRoot.gameObject);
        }

        private void LateUpdate()
        {
            float dt = ArenaFx.Step;
            Surge = Mathf.MoveTowards(Surge, 0.0f, dt * 1.8f);

            var stage = ArenaStage.Instance;
            var fx = ArenaFx.Instance;
            var hp = HalftimePresentation.Instance;
            if (stage == null || fx == null || hp == null || !stage.TryBreak(out var beats) || beats.Halftime || beats.From == beats.To)
            {
                EndBreak();
                return;
            }

            if (hp.MatchId != _match || hp.CompletedRound != _round) Begin(stage, hp, beats);

            float age = beats.Age;
            Vector3 centre = stage.transform.position;

            // The break is watched through its own camera: this screen's own body is in the picture too.
            if (ArenaBreakCamera.Showing != null)
            {
                if (_rig == null) { var main = Camera.main; _rig = main != null ? main.GetComponent<CameraSystem.CameraRig>() : null; }
                if (_rig != null) _rig.ShowBodyForCutaway();
            }

            Alarm(stage, fx, beats, centre);
            Title(stage, beats, centre);
            Move(stage, fx, beats, centre, dt);
            Reveal(stage, fx, beats, centre);
            Lift(stage, fx, hp, beats, centre, dt);

            _last = age;
        }

        private bool Crossed(float at, float age) => _last < at && age >= at;

        private void Begin(ArenaStage stage, HalftimePresentation hp, in ArenaStage.BreakBeats beats)
        {
            EndBreak();
            _match = hp.MatchId; _round = hp.CompletedRound;
            // Joined in the middle: what is left plays, what was missed does not.
            _last = beats.Age <= 0.6f ? -1.0f : beats.Age;

            int n = stage.Pieces.Length;
            if (_started.Length != n) { _started = new bool[n]; _locked = new bool[n]; _trail = new float[n]; }
            for (int i = 0; i < n; i++)
            {
                var move = stage.MoveOf(i, beats.From, beats.To, beats.Travel);
                _started[i] = beats.Age > beats.At(move.Starts) + 0.3f;
                _locked[i] = beats.Age > beats.At(move.Locks) + 0.3f;
                _trail[i] = 0.0f;
            }

            _locks = 0; _volley = false;
            var layout = beats.To >= 0 && beats.To < stage.LayoutCount ? stage.Layouts[beats.To] : null;
            _name = layout != null && !string.IsNullOrEmpty(layout.Name) ? layout.Name.ToUpperInvariant() : "";
            _typed = -1;
        }

        private void EndBreak()
        {
            if (_match < 0 && !_lifting && (_titleCanvas == null || !_titleCanvas.gameObject.activeSelf)) return;
            _match = -1; _round = -1;
            if (_titleCanvas != null && _titleCanvas.gameObject.activeSelf) _titleCanvas.gameObject.SetActive(false);
            EndLift();
        }

        // ------------------------------------------------------------------ 1. the alarm

        private void Alarm(ArenaStage stage, ArenaFx fx, in ArenaStage.BreakBeats beats, Vector3 centre)
        {
            float age = beats.Age;
            if (Crossed(0.05f, age))
            {
                ArenaFx.CueFlat("sfx_arena_alarm");
                // A first pulse out from the can as the lights go down.
                fx.Ring(centre + Vector3.up * 0.15f, 0.5f, stage.Radius + 6.0f, ArenaFx.Magenta, 0.55f, 0.9f, ArenaFx.Cell.ThinRing);
            }

            if (age >= beats.Undock + 0.4f) return;

            // Beacons turning round the stage's rim, until the platforms let go.
            float fade = Mathf.Clamp01(age / 0.3f) * Mathf.Clamp01((beats.Undock + 0.4f - age) / 0.4f);
            for (int i = 0; i < Beacons; i++)
            {
                float turn = Mathf.Sin(age * Mathf.PI * 2.0f * 1.3f - i * Mathf.PI * 2.0f / Beacons * 2.0f);
                float lit = Mathf.Pow(Mathf.Max(0.0f, turn), 6.0f) * fade;
                if (lit <= 0.02f) continue;
                Vector3 at = centre + ArenaStageMesh.Direction(360.0f * i / Beacons) * (stage.Radius + 2.5f) + Vector3.up * 1.4f;
                fx.DrawBillboard(ArenaFx.Cell.Dot, at, 3.4f, ArenaFx.Magenta, 0.9f * lit);
                fx.DrawBillboard(ArenaFx.Cell.Star, at, 5.0f, ArenaFx.White, 0.5f * lit);
            }

            // The scan: a ring crossing the stage over every deck, the hologram coming up behind it.
            float scan = stage.ScanRadius(beats);
            if (age >= beats.ScanStart && !float.IsPositiveInfinity(scan))
            {
                float across = scan * ArenaFx.RingScale(ArenaFx.Cell.Scan);
                fx.DrawFlat(ArenaFx.Cell.Scan, centre + Vector3.up * 3.4f, across, across, 0.0f, ArenaFx.Cyan, 0.85f);
                fx.DrawFlat(ArenaFx.Cell.ThinRing, centre + Vector3.up * 0.14f, across, across, 0.0f, ArenaFx.White, 0.6f);
            }
            else if (age >= beats.ScanEnd)
            {
                // Scanned: the stage's outline holds, breathing, until it comes apart.
                float across = (stage.Radius + 1.5f) * ArenaFx.RingScale(ArenaFx.Cell.ThinRing);
                fx.DrawFlat(ArenaFx.Cell.ThinRing, centre + Vector3.up * 3.4f, across, across, 0.0f, ArenaFx.Cyan,
                            (0.45f + 0.25f * Mathf.Sin(age * 11.0f)) * fade);
            }

            if (Crossed(beats.ScanEnd, age))
            {
                fx.Ring(centre + Vector3.up * 3.4f, stage.Radius + 1.5f, stage.Radius + 5.0f, ArenaFx.Cyan, 0.7f, 0.45f, ArenaFx.Cell.ThinRing);
                for (int i = 0; i < ArenaFx.Count(10); i++)
                    fx.Glint(centre + ArenaStageMesh.Direction(fx.Rand(0.0f, 360.0f)) * fx.Rand(2.0f, stage.Radius) + Vector3.up * fx.Rand(1.0f, 4.0f), fx.Rand(1.2f, 2.6f), ArenaFx.White, 0.9f, fx.Rand(0.3f, 0.6f));
            }
        }

        // ------------------------------------------------------------------ the layout's name

        /// <summary>
        /// The next layout's NAME as a hologram over the stage (PLAZA, TORE, KRUS, HUKAY,
        /// ENTABLADO): typed on behind the scan, held through the alarm, gone as the platforms
        /// go. It faces the camera that is drawing and is sized to that camera's own frame, so
        /// it reads the same from the low shot and from above.
        /// </summary>
        private void Title(ArenaStage stage, in ArenaStage.BreakBeats beats, Vector3 centre)
        {
            float age = beats.Age, from = beats.ScanStart + 0.35f, until = beats.Undock + 0.9f;
            var view = ArenaFx.View;
            bool show = _name.Length > 0 && view != null && age >= from && age < until;
            if (!show)
            {
                if (_titleCanvas != null && _titleCanvas.gameObject.activeSelf) _titleCanvas.gameObject.SetActive(false);
                return;
            }

            if (_titleCanvas == null) BuildTitle();
            if (!_titleCanvas.gameObject.activeSelf) _titleCanvas.gameObject.SetActive(true);

            // Typed on, a letter at a time (a new string only when a letter is added).
            int typed = Mathf.Clamp(Mathf.CeilToInt((age - from) / 0.045f), 1, _name.Length);
            if (typed != _typed) { _typed = typed; _title.text = typed >= _name.Length ? _name : _name.Substring(0, typed); }

            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            float appear = Mathf.Clamp01((age - from) / 0.2f), leave = Mathf.Clamp01((until - age) / 0.6f);
            float flicker = reduced ? 1.0f : 0.88f + 0.12f * Mathf.Sin(Time.unscaledTime * 43.0f) * Mathf.Sin(Time.unscaledTime * 7.3f);
            _titleGroup.alpha = appear * leave * flicker;

            var eye = view.transform;
            Vector3 at = centre + Vector3.up * (stage.CanHeight + TitleHeight);
            float depth = Vector3.Dot(at - eye.position, eye.forward);
            if (depth <= view.nearClipPlane + 0.5f) { _titleGroup.alpha = 0.0f; return; }

            // 46 per cent of the frame's width for the full name, whatever the lens and the distance.
            float wide = 2.0f * depth * Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad) * view.aspect;
            float pixels = Mathf.Max(400.0f, _titleWidth);
            float scale = wide * 0.46f / pixels;
            // As the platforms let go the title lifts away and swells a little.
            float lift = (1.0f - leave) * 2.5f;
            _titleCanvas.transform.SetPositionAndRotation(at + Vector3.up * lift, eye.rotation);
            _titleCanvas.transform.localScale = Vector3.one * scale * (1.0f + 0.08f * (1.0f - leave));
        }

        private float _titleWidth = 900.0f;

        private void BuildTitle()
        {
            var go = new GameObject("Arena layout title");
            go.transform.SetParent(transform, false);
            _titleCanvas = go.AddComponent<Canvas>();
            _titleCanvas.renderMode = RenderMode.WorldSpace;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 2.0f;
            ((RectTransform)go.transform).sizeDelta = new Vector2(2400.0f, 520.0f);
            _titleGroup = go.AddComponent<CanvasGroup>();
            _titleGroup.blocksRaycasts = false; _titleGroup.interactable = false;

            _title = Label(go.transform, "Name", 240, new Vector2(0.0f, 40.0f), ArenaFx.Cyan);
            _subtitle = Label(go.transform, "Caption", 60, new Vector2(0.0f, -150.0f), ArenaFx.White);
            _subtitle.text = "NEXT STAGE";

            // The widest the name can be, measured once on the full word, so the title does not
            // change size as it is typed.
            _title.text = "ENTABLADO";
            _titleWidth = Mathf.Max(400.0f, _title.preferredWidth);
            _title.text = "";
        }

        private static Text Label(Transform parent, string name, int size, Vector2 at, Color colour)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = UI.MenuKit.Font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = colour;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = at;
            rect.sizeDelta = new Vector2(2400.0f, size * 1.3f);
            // A thin dark edge, so the cyan reads against the floodlit turf as well as the dark shaft.
            var ink = go.AddComponent<UI.GodotOutline>();
            ink.OutlineColour = new Color(0.02f, 0.04f, 0.12f, 0.7f);
            ink.Radius = size * 0.03f;
            return text;
        }

        // ------------------------------------------------------------------ 2. the move

        private void Move(ArenaStage stage, ArenaFx fx, in ArenaStage.BreakBeats beats, Vector3 centre, float dt)
        {
            float age = beats.Age, travel = beats.Travel;
            bool undock = Crossed(beats.Undock, age);
            if (undock)
            {
                ArenaFx.CueFlat("sfx_arena_undock");
                ArenaFx.CueFlat("sfx_arena_thruster", 0.92f, 1.0f, 0.9f);
                ArenaBreakCamera.Punch(0.9f);
                ArenaCrowd.Excite(0.55f, 3.5f);
                Surge = Mathf.Max(Surge, 0.7f);
            }

            bool arcs = age >= beats.Undock && age < beats.Undock + 0.5f;
            int last = stage.LastLocking(beats.From, beats.To), arcsDrawn = 0;

            for (int i = 0; i < stage.Pieces.Length && i < _started.Length; i++)
            {
                var move = stage.MoveOf(i, beats.From, beats.To, travel);
                if (move.Motion == ArenaStage.Motion.Absent || move.Motion == ArenaStage.Motion.Still) continue;

                var piece = stage.Pieces[i];
                bool wasThere = piece.Shapes[beats.From].Exists;
                float thick = wasThere ? piece.Shapes[beats.From].Thick : piece.Shapes[beats.To].Thick;

                // The undock: every piece that is there lets go at once.
                if (wasThere && (undock || arcs))
                {
                    int n = stage.PiecePoints(i, beats.From, _points);
                    if (undock)
                        for (int k = 0; k < n; k++)
                        {
                            Vector3 under = _points[k] + Vector3.down * thick;
                            fx.Sparks(under, Vector3.down, 24.0f, 7, 6.0f, 15.0f, ArenaFx.Cyan, 0.9f, 0.25f, 0.5f, 0.2f, 0.0f);
                            fx.Flash(under, 0.8f, 3.2f, ArenaFx.Cyan, 0.7f, 0.25f);
                            fx.Sparks(_points[k] + Vector3.up * 0.1f, Vector3.up, 70.0f, 5, 2.0f, 7.0f, ArenaFx.White, 0.9f, 0.3f, 0.6f, 0.12f, 9.0f);
                        }

                    // Arcs of light along it for half a second, never the same twice.
                    if (arcs && !Settings.SettingsStore.Current.ReducedEffects)
                        for (int k = 0; k + 1 < n && arcsDrawn < 36; k++, arcsDrawn++)
                        {
                            Vector3 a = _points[k] + fx.RandDirection() * 0.5f + Vector3.up * 0.4f, b = _points[k + 1] + fx.RandDirection() * 0.5f + Vector3.up * 0.4f;
                            Vector3 mid = (a + b) * 0.5f + fx.RandDirection(1.0f) * fx.Rand(0.4f, 1.6f);
                            float lit = fx.Rand(0.3f, 0.95f) * Mathf.Clamp01((beats.Undock + 0.5f - age) / 0.25f);
                            fx.DrawBeam(a, mid, 0.16f, 0.16f, ArenaFx.White, lit);
                            fx.DrawBeam(mid, b, 0.16f, 0.16f, ArenaFx.Cyan, lit);
                        }
                }

                float starts = beats.At(move.Starts), locks = beats.At(move.Locks);

                // Its own go: the thrusters under it fire again.
                if (!_started[i] && age >= starts)
                {
                    _started[i] = true;
                    if (wasThere)
                    {
                        int n = stage.PiecePoints(i, beats.From, _points);
                        for (int k = 0; k < n; k++)
                            fx.Sparks(_points[k] + Vector3.down * thick, Vector3.down, 20.0f, 8, 8.0f, 18.0f, ArenaFx.Cyan, 0.9f, 0.3f, 0.55f, 0.22f, 0.0f);
                    }
                    ArenaFx.CueFlat("sfx_arena_thruster", 0.95f, 1.2f, 0.6f);
                }

                // On its way: light trails off it.
                if (move.T > 0.0f && move.T < 1.0f)
                {
                    _trail[i] -= dt;
                    if (_trail[i] <= 0.0f)
                    {
                        _trail[i] = 0.06f;
                        int shown = move.Shown >= 0 ? move.Shown : beats.To;
                        int n = stage.PiecePoints(i, shown, _points);
                        for (int k = 0; k < n; k++)
                        {
                            Vector3 at = _points[k] + Vector3.up * move.Lift;
                            if (move.Motion == ArenaStage.Motion.Leave || (move.Motion == ArenaStage.Motion.Swap && move.T < 0.5f))
                            {
                                // Dropping away: a streak of light left standing where it was.
                                fx.Emit(ArenaFx.Cell.Streak, ArenaFx.Mode.Upright, at, Vector3.up * 2.0f, ArenaFx.Cyan, 0.55f, 0.55f, 0.5f, 0.1f, 3.0f, 6.0f);
                                fx.Emit(ArenaFx.Cell.Dot, ArenaFx.Mode.Billboard, at, Vector3.up * 3.0f, ArenaFx.White, 0.7f, 0.5f, 0.6f, 0.15f);
                            }
                            else if (move.Motion == ArenaStage.Motion.Arrive || move.Motion == ArenaStage.Motion.Swap)
                            {
                                // Rising: its thrusters burn under it.
                                fx.Sparks(at + Vector3.down * thick, Vector3.down, 16.0f, 2, 9.0f, 18.0f, ArenaFx.Cyan, 0.9f, 0.25f, 0.5f, 0.24f, 0.0f);
                                fx.Emit(ArenaFx.Cell.Dot, ArenaFx.Mode.Billboard, at + Vector3.down * thick, Vector3.zero, ArenaFx.Cyan, 0.6f, 0.2f, 1.6f, 0.6f);
                            }
                            else fx.Sparks(at + Vector3.up * 0.1f, Vector3.up, 80.0f, 1, 1.0f, 4.0f, ArenaFx.White, 0.8f, 0.3f, 0.6f, 0.1f, 7.0f);
                        }
                    }
                }

                // The lock.
                if (!_locked[i] && age >= locks)
                {
                    _locked[i] = true;
                    if (ArenaStage.Locks(move.Motion)) Lock(stage, fx, i, beats.To, i == last);
                }
            }

            // A pulse chasing round the LED barrier while the stage moves: two of them, opposite.
            if (age >= beats.Undock && age < beats.Reveal + 1.2f)
            {
                float fade = Mathf.Clamp01((age - beats.Undock) / 0.3f) * Mathf.Clamp01((beats.Reveal + 1.2f - age) / 0.5f);
                float head = age * 0.42f;
                for (int i = 0; i < BarrierLights; i++)
                {
                    float away = Mathf.Abs(Mathf.Repeat((float)i / BarrierLights - head + 0.25f, 0.5f) - 0.25f);
                    float lit = Mathf.Clamp01(1.0f - away * 14.0f);
                    lit = Mathf.Max(lit * lit, Surge * 0.6f) * fade;
                    if (lit <= 0.03f) continue;
                    Vector3 foot = centre + ArenaStageMesh.Direction(360.0f * i / BarrierLights) * (BarrierRadius - 0.4f) + Vector3.up * 0.2f;
                    fx.DrawBeam(foot, foot + Vector3.up * 3.2f, 5.0f, 5.0f, ArenaFx.White, 0.5f * lit, ArenaFx.Cell.Dot);
                }
            }
        }

        /// <summary>One platform locking home: percussion. Its own thud a step higher than the
        /// last, a flash and a shock ring at each of its points, a column of light, the punch.</summary>
        private void Lock(ArenaStage stage, ArenaFx fx, int index, int layout, bool last)
        {
            int n = stage.PiecePoints(index, layout, _points);
            float pitch = Mathf.Min(1.18f, 0.9f + 0.035f * _locks);
            _locks++;
            ArenaFx.CueFlat("sfx_arena_lock", pitch, pitch, last ? 1.2f : 1.0f);
            ArenaBreakCamera.Punch(last ? 1.5f : 1.0f);
            Surge = 1.0f;

            for (int k = 0; k < n; k++)
            {
                Vector3 at = _points[k] + Vector3.up * 0.1f;
                fx.Flash(at + Vector3.up * 0.4f, 1.4f, last ? 7.5f : 5.0f, ArenaFx.White, 0.9f, 0.22f);
                fx.Ring(at, 0.4f, last ? 7.5f : 5.0f, ArenaFx.Cyan, 0.85f, 0.42f);
                fx.Sparks(at, Vector3.up, 62.0f, 9, 3.0f, 10.0f, ArenaFx.White, 0.95f, 0.3f, 0.7f, 0.14f, 12.0f);
                if (k == 0 || last) fx.Pillar(at, last ? 16.0f : 10.0f, last ? 1.8f : 1.2f, ArenaFx.Cyan, 0.75f, 0.4f);
            }
        }

        // ------------------------------------------------------------------ 3. the reveal

        private void Reveal(ArenaStage stage, ArenaFx fx, in ArenaStage.BreakBeats beats, Vector3 centre)
        {
            float age = beats.Age;
            Vector3 can = centre + Vector3.up * (stage.CanHeight + 0.2f);

            if (Crossed(beats.Reveal, age))
            {
                ArenaFx.CueFlat("sfx_arena_reveal");
                ArenaCrowdAudio.Reveal();
                ArenaFx.CueFlat("sfx_arena_pyro", 0.92f, 1.0f, 0.9f);
                ArenaBreakCamera.Punch(2.2f);
                ArenaCrowd.Excite(1.0f, 5.5f);
                ArenaCrowd.Wave();
                ArenaAmbience.Stinger();
                Surge = 1.0f;

                // The flash and the shockwave, from the can outward.
                fx.Flash(can + Vector3.up * 2.0f, 8.0f, 90.0f, ArenaFx.White, 0.95f, 0.5f);
                fx.Ring(can, 1.0f, stage.Radius + 22.0f, ArenaFx.White, 0.95f, 0.85f);
                fx.Ring(can + Vector3.up * 0.05f, 0.5f, 62.0f, ArenaFx.Cyan, 0.7f, 1.5f);
                fx.Pillar(can, 34.0f, 3.5f, ArenaFx.Cyan, 0.8f, 0.7f);
                fx.Sparks(can + Vector3.up * 0.4f, Vector3.up, 75.0f, 40, 6.0f, 20.0f, ArenaFx.White, 0.95f, 0.5f, 1.1f, 0.2f, 12.0f);
                Volley(fx, centre, 0);
                _volley = false;
            }

            // A second volley from the jets between the first, and fireworks over the stands.
            if (!_volley && age >= beats.Reveal + 0.55f && _last < beats.Reveal + 0.55f)
            {
                _volley = true;
                Volley(fx, centre, 1);
                ArenaFx.CueFlat("sfx_arena_pyro", 1.0f, 1.12f, 0.8f);
            }

            for (int k = 0; k < 4; k++)
                if (Crossed(beats.Reveal + 0.25f + 0.28f * k, age))
                {
                    Vector3 over = centre + ArenaStageMesh.Direction(45.0f + 90.0f * k + fx.Rand(-18.0f, 18.0f)) * fx.Rand(96.0f, 124.0f) + Vector3.up * fx.Rand(48.0f, 62.0f);
                    fx.Firework(over, k % 2 == 0 ? ArenaFx.Gold : ArenaFx.Magenta, 1.6f);
                }
        }

        /// <summary>Pyro jets and confetti from the field's edge, every other jet per volley.</summary>
        private void Volley(ArenaFx fx, Vector3 centre, int phase)
        {
            for (int j = phase; j < Jets; j += 2)
            {
                Vector3 foot = centre + ArenaStageMesh.Direction(360.0f * j / Jets + 18.0f) * FieldEdge + Vector3.up * 0.2f;
                fx.Pillar(foot, 16.0f, 2.4f, ArenaFx.Gold, 0.8f, 0.55f);
                fx.Flash(foot + Vector3.up, 2.0f, 9.0f, ArenaFx.Gold, 0.8f, 0.3f);
                fx.Sparks(foot, Vector3.up, 10.0f, 22, 18.0f, 34.0f, ArenaFx.Gold, 0.95f, 0.6f, 1.2f, 0.4f, 10.0f, 0.8f);
                Color paper = j % 3 == 0 ? ArenaFx.Magenta : j % 3 == 1 ? ArenaFx.Cyan : ArenaFx.Lime;
                fx.Dots(foot + Vector3.up * 2.0f, Vector3.up, 26.0f, 12, 10.0f, 20.0f, paper, 0.9f, 2.0f, 3.4f, 0.45f, 3.5f, 1.3f);
                fx.Dots(foot + Vector3.up * 2.0f, Vector3.up, 26.0f, 8, 10.0f, 20.0f, ArenaFx.White, 0.9f, 2.0f, 3.4f, 0.4f, 3.5f, 1.3f);
            }
        }

        // ------------------------------------------------------------------ the players' lift

        private void Lift(ArenaStage stage, ArenaFx fx, HalftimePresentation hp, in ArenaStage.BreakBeats beats, Vector3 centre, float dt)
        {
            float age = beats.Age;
            float come = beats.Undock - 0.75f, rise = beats.Undock - 0.1f, across = beats.Undock + 0.9f, over = beats.MoveEnd - 0.2f;
            float lower = beats.Reveal + 0.15f, down = beats.Reveal + 1.05f, gone = Mathf.Min(beats.Duration - 0.05f, down + 0.9f);

            if (age < come || age >= beats.Duration - 0.02f) { if (_lifting) EndLift(); return; }
            if (!_lifting) BeginLift(hp);

            float carry = centre.y + Mathf.Max(0.0f, stage.CanHeight) + CarryHeight;
            for (int i = 0; i < _lifted; i++)
            {
                var body = _liftBody[i];
                var root = _liftRoot[i];
                if (body == null || root == null) continue;

                Vector3 from = _liftFrom[i], to = _liftTo[i];
                // Where the body is drawn: straight up, across at the carry height, straight down.
                float up = Smooth((age - rise) / (across - rise)), side = Smooth((age - across) / (over - across)), drop = Smooth((age - lower) / (down - lower));
                Vector3 at = Vector3.Lerp(from, to, side);
                float top = carry + Mathf.Sin((age + i * 0.7f) * 2.4f) * 0.12f * up * (1.0f - drop);
                at.y = age < lower ? Mathf.Lerp(from.y, top, up) : Mathf.Lerp(top, to.y, drop);

                // ONLY the drawn model moves: its root's own rest pose plus the offset. It turns
                // once, slowly, in the beam on its way over (a whole turn, so it lands as it stood).
                Vector3 offset = at - body.transform.position;
                var parent = root.parent;
                root.localPosition = _liftRest[i] + (parent != null ? parent.InverseTransformVector(offset) : offset);
                root.localRotation = _liftTurn[i] * Quaternion.AngleAxis(360.0f * Smooth((age - rise) / (lower - rise)), Vector3.up);

                // The drone over it: down onto the body looking for it, locked on, with it all the
                // way, a bow where it set the body down, and away. The drone draws each act itself.
                var drone = _liftDrone[i];
                if (drone == null) continue;
                float arrive = Smooth((age - come) / (rise - come)), leave = Mathf.Clamp01((age - down) / (gone - down));
                const float bow = 0.5f;
                ArenaDrone.Act act;
                float t;
                if (age < rise) { bool locked = arrive >= 0.7f; act = locked ? ArenaDrone.Act.Lock : ArenaDrone.Act.Search; t = locked ? (arrive - 0.7f) / 0.3f : arrive / 0.7f; }
                else if (age < across) { act = ArenaDrone.Act.Haul; t = (age - rise) / (across - rise); }
                else if (age < lower) { act = ArenaDrone.Act.Across; t = Mathf.Clamp01((age - across) / (over - across)); }
                else if (age < down) { act = ArenaDrone.Act.SetDown; t = (age - lower) / (down - lower); }
                else if (leave < bow) { act = ArenaDrone.Act.Proud; t = leave / bow; }
                else { act = ArenaDrone.Act.Leave; t = (leave - bow) / (1.0f - bow); }

                float away = act == ArenaDrone.Act.Leave ? t : 0.0f;
                float beam = age < down ? (ArenaDrone.Hover - 1.25f) * arrive : 0.0f;
                Vector3 hover = at + Vector3.up * (ArenaDrone.Hover + 12.0f * (1.0f - arrive) + 16.0f * away * away);
                drone.Hold(hover, act, t, beam, at, to, dt, i == 0);
            }
        }

        private void BeginLift(HalftimePresentation hp)
        {
            _lifting = true;
            _lifted = 0;
            var round = GameServices.Round;
            if (round == null) return;

            var template = ArenaFallRecovery.Instance != null ? ArenaFallRecovery.Instance.DroneTemplate : null;
            var players = round.Players;
            for (int p = 0; p < players.Count && _lifted < MaxLifted; p++)
            {
                var body = players[p];
                if (body == null || !body.gameObject.activeInHierarchy) continue;

                var visual = body.GetComponent<Visual.CharacterVisual>();
                var root = visual != null ? visual.ModelRoot : null;
                // A model hung on the seat itself cannot be moved without moving the body: left standing.
                if (root == null || root == body.transform) continue;

                int i = _lifted++;
                _liftBody[i] = body; _liftRoot[i] = root; _liftRest[i] = root.localPosition; _liftTurn[i] = root.localRotation;
                _liftFrom[i] = body.transform.position;
                _liftTo[i] = MarkFor(body, hp.NextTaya);

                if (_liftDrone[i] == null)
                {
                    if (_droneRoot == null) _droneRoot = new GameObject("Arena break drones").transform;
                    _liftDrone[i] = ArenaDrone.Build(_droneRoot, template);
                }
            }
        }

        /// <summary>Where the round's reset will stand this body: its mark for the next round's
        /// roles, on the floor the NEW layout has there (its colliders have been live since the
        /// break began), found the way `MatchHost.SeatOnFloor` finds it.</summary>
        private static Vector3 MarkFor(CharacterMotor body, int nextTaya)
        {
            Vector3 mark = SliceRunner.SpawnPointFor(body.PlayerSlot, nextTaya);
            int count = Physics.RaycastNonAlloc(mark + Vector3.up * 2.0f, Vector3.down, FloorHits, 6.0f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.NegativeInfinity;
            for (int h = 0; h < count; h++)
            {
                var hit = FloorHits[h].collider;
                if (hit == null || hit.GetComponentInParent<CharacterMotor>() != null || hit.GetComponentInParent<Slipper>() != null || hit.GetComponentInParent<Lata>() != null) continue;
                best = Mathf.Max(best, FloorHits[h].point.y);
            }

            if (!float.IsNegativeInfinity(best)) mark.y = best;
            return mark;
        }

        /// <summary>Every lifted model back exactly where its body is, and the drones put away.</summary>
        private void EndLift()
        {
            if (!_lifting) return;
            _lifting = false;
            for (int i = 0; i < _lifted; i++)
            {
                if (_liftRoot[i] != null) { _liftRoot[i].localPosition = _liftRest[i]; _liftRoot[i].localRotation = _liftTurn[i]; }
                if (_liftDrone[i] != null) _liftDrone[i].Release();
                _liftBody[i] = null; _liftRoot[i] = null;
            }
            _lifted = 0;
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3.0f - 2.0f * t);
        }
    }
}
