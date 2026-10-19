using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// THE PLAYERS' LIFT IN A PAYO BREAK (owner, 2026-10-19: "in arena, the players are lifted by the drones when the map
    /// changes. do the same thing here."). The Arena's break has its drones lift every player off the stage while it
    /// rebuilds and set them down on their marks (`ArenaShow.Lift`); this is that, beat for beat, on the stone stage,
    /// with Kanlit (`PayoBreakKanlit`, the wood spirit that already catches a fall on this map) in place of the drone:
    /// it dives onto each player, its vine hugs them, it hauls them up clear of every stone, carries them across while
    /// the monoliths move, lowers them onto their mark on the NEW layout, lets go with a pop and a hop, and zips away.
    ///
    /// ⚠️ PRESENTATION ONLY, AND NOTHING IS SENT, EXACTLY AS IN THE ARENA. Every moment is a time on the break's shared
    /// clock (`ArenaStage.TryBreak`: `SharedUltimatePhase.Now` against the host's `Began`), which every peer already has,
    /// and where each player goes is the mark the round's reset will put them on (`SliceRunner.SpawnPointFor` with the
    /// break's own next taya, on the new layout's floor, found the way `MatchHost.SeatOnFloor` finds it). So every peer
    /// draws the same lift on the same frame, and a peer that joins in the middle of a break sees the rest of it.
    ///
    /// ⚠️ ONLY WHAT IS DRAWN MOVES. A break holds the simulation and the real reset is `SliceRunner.ResetWorld`'s teleport
    /// at the round's start, so nothing here touches a motor, a capsule or a replicated transform: only each body's
    /// `CharacterVisual.ModelRoot` is carried, and it is put back exactly as it was when the break ends. Humans and bots
    /// alike (every body in the round). The slippers and the can are the reset's, untouched.
    ///
    /// WHAT IT ADDS TO THE ARENA'S PATH. The carry height is over the highest stone of either layout (the Arena's is a
    /// fixed height over its can), the body hangs from Kanlit's paws and swings like a weight on a line (it trails as the
    /// carry speeds up, swings ahead as it slows, and sways), and on the way over it turns to face the can, so it lands
    /// standing as the reset will stand it and nothing jumps when the round begins.
    ///
    /// NOT IN A REPLAY, AS IN THE ARENA: the local replay records the round only (`LocalReplayRecorder` samples on game
    /// time, which stops in a break, and only while `RoundActive`), so neither map's break lift is in it.
    /// </summary>
    [DefaultExecutionOrder(905)]
    public sealed class PayoBreakLift : MonoBehaviour
    {
        private const int MaxLifted = 8;
        /// <summary>How far over the highest stone top of either layout the feet are carried, and the least over where they start and end.</summary>
        private const float OverStones = 4.5f, OverEnds = 5.0f;
        /// <summary>How long Kanlit's swoop takes, from the lift's first moment (`ArenaShow.LiftLead` before the undock).</summary>
        private const float DiveSeconds = 0.4f;
        /// <summary>Within this of the break camera's lens this screen's own body and its Kanlit are not drawn, metres.</summary>
        private const float NearEye = 2.6f;
        /// <summary>Within this of the lens any other carried body or Kanlit is not drawn (the lens would be inside it), metres.</summary>
        private const float InLens = 1.5f;

        private long _match = -1;
        private int _round = -1;
        private readonly CharacterMotor[] _body = new CharacterMotor[MaxLifted];
        private readonly Transform[] _root = new Transform[MaxLifted];
        private readonly Vector3[] _rest = new Vector3[MaxLifted], _from = new Vector3[MaxLifted], _to = new Vector3[MaxLifted], _vineScale = new Vector3[MaxLifted];
        private readonly Quaternion[] _turn = new Quaternion[MaxLifted];
        /// <summary>The model's own offset from its body's feet in world space, how far round it turns to face the can, and
        /// how far over the feet Kanlit's paws hold the vine.</summary>
        private readonly Vector3[] _foot = new Vector3[MaxLifted];
        private readonly float[] _yawTo = new float[MaxLifted], _hang = new float[MaxLifted];
        private readonly bool[] _puffed = new bool[MaxLifted];
        private readonly PayoBreakKanlit[] _kanlit = new PayoBreakKanlit[MaxLifted];
        private int _lifted;
        private bool _lifting;
        private float _carry;
        private Transform _kanlitRoot;
        private CameraSystem.CameraRig _rig;
        private static readonly RaycastHit[] FloorHits = new RaycastHit[16];

        /// <summary>A probe's view: how many bodies are being carried, and the drawn feet of one.</summary>
        public int Lifted => _lifting ? _lifted : 0;
        /// <summary>A probe's count: frames this screen's own model was kept out of the break camera's lens.</summary>
        public static int NearEyeFrames;
        public bool TryDrawnFeet(int i, out CharacterMotor body, out Vector3 feet, out Vector3 mark)
        {
            body = null; feet = mark = default;
            if (!_lifting || i < 0 || i >= _lifted || _body[i] == null || _root[i] == null) return false;
            body = _body[i]; feet = _root[i].position - _foot[i]; mark = _to[i];
            return true;
        }

        private void OnDisable() => End();

        private void OnDestroy()
        {
            if (_kanlitRoot != null) Destroy(_kanlitRoot.gameObject);
        }

        private void LateUpdate()
        {
            var stage = ArenaStage.Instance;
            var hp = HalftimePresentation.Instance;
            if (stage == null || !stage.Stone || hp == null || !stage.TryBreak(out var beats) || !beats.Showing || beats.From == beats.To) { End(); return; }
            if (hp.MatchId != _match || hp.CompletedRound != _round) { End(); _match = hp.MatchId; _round = hp.CompletedRound; }

            // The break is watched through its own camera: this screen's own body is in the picture too (as `ArenaShow`).
            if (ArenaBreakCamera.Showing != null)
            {
                if (_rig == null) { var main = Camera.main; _rig = main != null ? main.GetComponent<CameraSystem.CameraRig>() : null; }
                if (_rig != null) _rig.ShowBodyForCutaway();
            }

            Lift(stage, hp.NextTaya, beats, ArenaFx.Step);
        }

        private void Lift(ArenaStage stage, int nextTaya, in ArenaStage.BreakBeats beats, float dt)
        {
            float age = beats.Age;
            // The Arena's own beats (`ArenaShow.Lift`).
            float come = beats.Undock - ArenaShow.LiftLead, rise = beats.Undock - 0.1f, across = beats.Undock + 0.9f, over = beats.MoveEnd - 0.2f;
            float lower = beats.Reveal + 0.15f, down = beats.Reveal + 1.05f, gone = Mathf.Min(beats.Duration - 0.05f, down + 0.9f);

            // Held to the very end of the break, so the model is handed back on the frame the round's reset takes over.
            if (age < come || age >= beats.Duration) { End(); return; }
            if (!_lifting) Begin(stage, nextTaya, beats);

            for (int i = 0; i < _lifted; i++)
            {
                var body = _body[i];
                var root = _root[i];
                if (body == null || root == null) continue;

                // ---- where the body is drawn: straight up, across over the stones, straight down (the Arena's path)
                Vector3 from = _from[i], to = _to[i];
                float up = Smooth((age - rise) / (across - rise)), side = Smooth((age - across) / (over - across)), drop = Smooth((age - lower) / (down - lower));
                Vector3 at = Vector3.Lerp(from, to, side);
                float held = up * (1.0f - drop);
                float top = _carry + Mathf.Sin((age + i * 0.7f) * 2.4f) * 0.12f * held;
                at.y = age < lower ? Mathf.Lerp(from.y, top, up) : Mathf.Lerp(top, to.y, drop);

                // ---- it hangs from the paws: a weight on a line, swung about the hold over its head
                Vector3 travel = to - from; travel.y = 0.0f;
                Vector3 dir = travel.sqrMagnitude > 0.01f ? travel.normalized : Vector3.forward;
                Vector3 pitchAxis = Vector3.Cross(Vector3.up, dir);
                float u = Mathf.Clamp01((age - across) / (over - across));
                // Trails as the carry speeds up, swings ahead as it slows (the carry's own acceleration), and sways.
                float trail = travel.sqrMagnitude > 0.25f ? 12.0f * (1.0f - 2.0f * u) * Mathf.Sin(u * Mathf.PI) : 0.0f;
                float kick = age > rise ? 9.0f * Mathf.Sin((age - rise) * 7.0f) * Mathf.Exp(-(age - rise) * 2.2f) : 0.0f;
                float sway = (5.0f * Mathf.Sin(age * 3.1f + i * 1.3f) + kick) * held;
                var swing = Quaternion.AngleAxis(trail * held, pitchAxis) * Quaternion.AngleAxis(sway, dir);
                // On the way over it turns to face the can, as the reset will stand it.
                var face = Quaternion.AngleAxis(_yawTo[i] * Smooth((age - rise) / (lower - rise)), Vector3.up);
                Vector3 hold = at + Vector3.up * _hang[i];
                var parent = root.parent;
                var restRotation = parent != null ? parent.rotation * _turn[i] : _turn[i];
                root.SetPositionAndRotation(hold + swing * (at + face * _foot[i] - hold), swing * face * restRotation);
                // THIS SCREEN'S OWN BODY IS NOT DRAWN IN THE EYE. Payo's break camera ends each shot by gliding into this
                // peer's eye at its spawn (`PayoBreakDirector.HandbackSeconds`), which is where the lift has just stood
                // this body's model: within a body's reach of the lens it is put out of sight (well under the map),
                // as first person would hide it, and `End` puts it back with the rest. Any other carried body the lens
                // would pass inside (a low dolly across the court) is put out of sight the same way for those frames.
                var view = ArenaBreakCamera.Showing;
                bool own = _rig != null && _rig.IsFollowing(body);
                float clear = own ? NearEye : InLens;
                if (view != null && (view.transform.position - (at + Vector3.up)).sqrMagnitude < clear * clear)
                { root.position = at + Vector3.down * 60.0f; NearEyeFrames++; }

                // ---- Kanlit over it: the act and the paws, off the same clock
                var kanlit = _kanlit[i];
                if (kanlit == null) continue;
                PayoBreakKanlit.Act act;
                float t, size = 1.0f, grow, release = -1.0f;
                Vector3 paws = hold;
                Vector3 vineAt = hold + swing * (at - hold);
                var vineTurn = swing * face * Quaternion.Euler(0.0f, body.transform.eulerAngles.y, 0.0f);
                float wrapFrom = come + DiveSeconds * 0.3f;
                grow = Smooth((age - wrapFrom) / (rise - 0.05f - wrapFrom));
                if (age < come + DiveSeconds)
                {
                    act = PayoBreakKanlit.Act.Dive; t = (age - come) / DiveSeconds;
                    float e = 1.0f - (1.0f - t) * (1.0f - t) * (1.0f - t);
                    Vector3 sky = hold + Vector3.up * 7.0f + ArenaStageMesh.Direction(35.0f + 45.0f * i) * 4.0f;
                    paws = Vector3.Lerp(sky, hold, e) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.8f);
                }
                else if (age < rise) { act = PayoBreakKanlit.Act.Wrap; t = (age - come - DiveSeconds) / Mathf.Max(0.05f, rise - come - DiveSeconds); }
                else if (age < across) { act = PayoBreakKanlit.Act.Haul; t = (age - rise) / (across - rise); }
                else if (age < lower) { act = PayoBreakKanlit.Act.Across; t = u; }
                else if (age < down) { act = PayoBreakKanlit.Act.SetDown; t = (age - lower) / (down - lower); }
                else
                {
                    // Let go on the mark: the vine runs back up into its paws, the flowers fly, a hop; then away, fast.
                    float since = age - down, hop = (gone - down) * 0.45f;
                    release = since;
                    float back = Mathf.Clamp01((since - 0.03f) / 0.28f);
                    grow = 1.0f - back * back;
                    if (since < hop) { act = PayoBreakKanlit.Act.LetGo; t = since / hop; paws = hold + Vector3.up * (0.5f * Mathf.Sin(t * Mathf.PI)); }
                    else
                    {
                        act = PayoBreakKanlit.Act.Away; t = Mathf.Clamp01((since - hop) / Mathf.Max(0.05f, gone - down - hop));
                        paws = hold + Vector3.up * (12.0f * t * t + 1.5f * t) + ArenaStageMesh.Direction(215.0f + 45.0f * i) * (5.0f * t);
                        size = 1.0f - 0.5f * Mathf.SmoothStep(0.5f, 1.0f, t);
                    }
                }
                if (age >= gone || (view != null && (view.transform.position - paws).sqrMagnitude < clear * clear)) { kanlit.Release(); continue; }
                kanlit.Hold(act, Mathf.Clamp01(t), paws, size, vineAt, vineTurn, _vineScale[i], grow, release, dt, i == 0);
                if (release >= 0.0f && !_puffed[i]) { _puffed[i] = true; SetDownPuff(to); }
            }
        }

        // A soft ring of dust where the feet land, and a few leaves: small, the creature is the show.
        private static void SetDownPuff(Vector3 mark)
        {
            var fx = ArenaFx.Instance;
            if (fx == null) return;
            var dust = new Color(0.86f, 0.84f, 0.78f);
            var leaf = new Color(0.47f, 0.74f, 0.32f);
            fx.Ring(mark + Vector3.up * 0.06f, 0.3f, 1.6f, dust, 0.45f * ArenaFx.Light, 0.45f);
            fx.Dots(mark + Vector3.up * 0.3f, Vector3.up, 70.0f, 5, 1.5f, 3.2f, leaf, 0.8f, 0.5f, 0.9f, 0.16f, 3.0f);
        }

        private void Begin(ArenaStage stage, int nextTaya, in ArenaStage.BreakBeats beats)
        {
            _lifting = true;
            _lifted = 0;
            var round = GameServices.Round;
            if (round == null) return;

            // Over the highest stone of either layout: nothing that moves can reach the carried feet.
            float highest = float.NegativeInfinity;
            foreach (var piece in stage.Pieces)
            {
                if (piece == null || piece.Shapes == null) continue;
                if (beats.From >= 0 && beats.From < piece.Shapes.Length && piece.Shapes[beats.From].Exists) highest = Mathf.Max(highest, Mathf.Max(piece.Shapes[beats.From].Top, piece.Shapes[beats.From].Top1));
                if (beats.To >= 0 && beats.To < piece.Shapes.Length && piece.Shapes[beats.To].Exists) highest = Mathf.Max(highest, Mathf.Max(piece.Shapes[beats.To].Top, piece.Shapes[beats.To].Top1));
            }
            if (float.IsNegativeInfinity(highest)) highest = 0.0f;
            _carry = stage.transform.TransformPoint(new Vector3(0.0f, highest, 0.0f)).y + OverStones;

            var players = round.Players;
            for (int p = 0; p < players.Count && _lifted < MaxLifted; p++)
            {
                var body = players[p];
                if (body == null || !body.gameObject.activeInHierarchy) continue;
                var visual = body.GetComponent<Visual.CharacterVisual>();
                var root = visual != null ? visual.ModelRoot : null;
                // A model hung on the seat itself cannot be moved without moving the body: left standing (as the Arena).
                if (root == null || root == body.transform) continue;

                int i = _lifted++;
                var feet = body.transform.position;
                _puffed[i] = false;
                _body[i] = body; _root[i] = root; _rest[i] = root.localPosition; _turn[i] = root.localRotation;
                _foot[i] = root.position - feet;
                _from[i] = feet;
                _to[i] = MarkFor(body, nextTaya);
                _carry = Mathf.Max(_carry, Mathf.Max(_from[i].y, _to[i].y) + OverEnds);
                // The reset faces everyone at the can (`SliceRunner.ResetWorld`): the model turns to it on the way over.
                Vector3 toCan = -new Vector3(_to[i].x, 0.0f, _to[i].z);
                float canYaw = toCan.sqrMagnitude > 1e-4f ? Mathf.Atan2(toCan.x, toCan.z) * Mathf.Rad2Deg : body.transform.eulerAngles.y;
                _yawTo[i] = Mathf.DeltaAngle(body.transform.eulerAngles.y, canYaw);

                // The vine sized to the capsule (as the catch sizes it), its lead held clear of the model's top.
                float tall = 1.75f, wide = 0.3f;
                var cc = body.GetComponent<CharacterController>();
                if (cc != null) { tall = cc.height * body.transform.lossyScale.y; wide = cc.radius * Mathf.Max(body.transform.lossyScale.x, body.transform.lossyScale.z); }
                float sy = Mathf.Clamp(tall / 1.75f, 0.85f, 1.4f), sx = Mathf.Clamp(wide / 0.3f, sy * 0.9f, 1.6f);
                _vineScale[i] = new Vector3(sx, sy, sx);
                float leadY = PayoKanlitMeshes.Lead.y * sy, topY = float.NegativeInfinity;
                foreach (var r in root.GetComponentsInChildren<Renderer>(false))
                {
                    if (r == null || !r.enabled || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                    topY = Mathf.Max(topY, r.bounds.max.y - feet.y);
                }
                _hang[i] = leadY + (float.IsInfinity(topY) ? 0.0f : Mathf.Clamp(topY + PayoKanlit.Clear - leadY, 0.0f, PayoKanlit.MostReach));

                if (_kanlit[i] == null)
                {
                    if (_kanlitRoot == null) _kanlitRoot = new GameObject("Payo break Kanlits (made at run time)") { hideFlags = HideFlags.DontSave }.transform;
                    _kanlit[i] = PayoBreakKanlit.Build(_kanlitRoot);
                }
            }
        }

        /// <summary>Where the round's reset will stand this body: its mark for the next round's roles, on the floor the
        /// NEW layout has there (its colliders have been live since the break began), as `ArenaShow` finds it.</summary>
        private static Vector3 MarkFor(CharacterMotor body, int nextTaya)
        {
            Vector3 mark = SliceRunner.SpawnPointFor(body.PlayerSlot, nextTaya);
            int count = Physics.RaycastNonAlloc(mark + Vector3.up * MatchHost.SpawnFloorProbeHeight, Vector3.down, FloorHits, MatchHost.SpawnFloorProbeDepth, ~0, QueryTriggerInteraction.Ignore);
            float best = float.NegativeInfinity;
            for (int h = 0; h < count; h++)
            {
                var hit = FloorHits[h].collider;
                if (hit == null || hit.GetComponentInParent<CharacterMotor>() != null || hit.GetComponentInParent<Slipper>() != null || hit.GetComponentInParent<Lata>() != null) continue;
                best = Mathf.Max(best, FloorHits[h].point.y);
            }
            if (!float.IsNegativeInfinity(best)) mark.y = best + MatchHost.SpawnFloorClearance;
            return mark;
        }

        /// <summary>Every lifted model back exactly where its body is, and the Kanlits put away.</summary>
        private void End()
        {
            if (!_lifting) return;
            _lifting = false;
            for (int i = 0; i < _lifted; i++)
            {
                if (_root[i] != null) { _root[i].localPosition = _rest[i]; _root[i].localRotation = _turn[i]; }
                if (_kanlit[i] != null) _kanlit[i].Release();
                _body[i] = null; _root[i] = null;
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
