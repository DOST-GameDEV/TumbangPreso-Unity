using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ BLINDFOLD, THE GHOST PEEKER LEAVES WAITING (owner 2026-10-15 as DON'T LOOK, retuned 2026-10-21, `NecroRules.DontLook*`).
    /// *"place a ghost in an area and it becomes invisible after"* 1.5 s (owner's second pass, from 2.5 s), *"and then if anyone goes near that area the ghost would
    /// lunge at their eyes"*, covering them and slowing them 25 % for 2.5 s. Gekko's way of fighting: a creature of her own
    /// does the work while she is elsewhere. One per Nemu (`Place` retires her last one).
    ///
    ///  * WAITING: Blindfold itself (`Visual.BlindfoldGhostModel`, the owner's approved mockup: an icy ghost child with a
    ///    cloth tied over its eyes), floating low over the spot, facing down Nemu's aim. Every screen sees it rise out of the
    ///    ground; over its first 1.5 s it fades to a faint shimmer, still there for anyone who looks hard (Killjoy's traps,
    ///    owner on v4: *"invisible but like medyo kita"*). Nemu keeps a clearer copy so she knows where it is.
    ///  * Its trigger ring, `NecroRules.DontLookTriggerRadius` (2 m), is drawn on the court for everyone (`Visual.NemuGhostRings`).
    ///  * SPRUNG: the first other player within 2 m once it is armed. ONLY THE HOST DECIDES: it writes BLINDED
    ///    (`CharacterMotor.ApplyBlinded`), and `SyncUnit` carries that clock to every peer. Protocol163's ghost frame names
    ///    the exact lifetime and victim, including their movement epoch; peers never guess from a nearby status edge.
    ///  * THE LUNGE: visible to everyone, its cloth drops to its neck, its cyan pinpoint eyes open, and it shoots at the
    ///    victim's face in 0.22 s and latches on, face to face, its arms round their head, until their eyes are
    ///    uncovered, then lets go and flies off.
    ///  * THE VICTIM'S OWN CAMERA sees the same ghost jump into their lens and cling there, alive (`PoseForLens`), with the
    ///    dark closing round it (`Shaders/DontLookBlind`). Owner on v4: a face drawn over the screen *"looks wrong"*, *"bat
    ///    parang slideshoww"*; *"wont it be nicer if the ghost jumps at u"*.
    ///  * Untripped, it goes after 15 s. A rejoiner gets the waiting ghost at the same age (`WorldEffectSnapshot.Kind.Blindfold`).
    ///  * SOULBOUND: when it is done (it let go of its victim, or its 15 s ran out) the host leaves a dormant Peeker where it
    ///    ended (`NemuDormant`), and the skill's cooldown waits until Nemu reclaims it.
    ///
    /// The host's bounded complete ghost frames also hydrate late arrivals and retire replaced lifetimes.
    /// </summary>
    public sealed partial class NemuBlindfold : MonoBehaviour
    {
        public static readonly List<NemuBlindfold> Live = new List<NemuBlindfold>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLive() => Live.Clear();

        private enum State { Waiting, Lunging, Covering, Leaving }

        public int OwnerSlot { get; private set; } = -1;
        public Vector3 Spot { get; private set; }
        public Vector3 Facing { get; private set; } = Vector3.forward;
        public float Age => _age;
        public bool Armed => _age >= NecroRules.DontLookArmSeconds;
        public bool Sprung => _state != State.Waiting;
        /// <summary>Going (sinking away unsprung, or flying off its victim).</summary>
        public bool Retiring => _state == State.Leaving;
        public CharacterMotor Victim => _victim;
        public float Remaining => Sprung ? 0.0f : Mathf.Max(0.0f, NecroRules.DontLookLifeSeconds - _age);

        private State _state;
        private float _age, _stateAge;
        private CharacterMotor _victim;
        private Vector3 _lungeFrom;
        /// <summary>Where the host leaves its dormant ghost once it has gone (null: recast or cleared, no dormant ghost).</summary>
        private Vector3? _dormantAt;
        private NemuSmokeBatch _smoke;
        private MeshFilter _mesh;
        private MeshRenderer _body;
        private MaterialPropertyBlock _block;
        // The pet mouse's darting figure-eights round its hiding spot (`PetGhostFlight`, drawing only, behind `PetGhostModels.PetGhosts`).
        private readonly PetGhostFlight _flight = new PetGhostFlight(PetGhostFlight.Kind.Mouse);
        private static readonly int TrailId = Shader.PropertyToID("_Trail");
        private static Material _material;
        private static readonly int FadeId = Shader.PropertyToID("_Fade"), FlashId = Shader.PropertyToID("_Flash"),
            ClockId = Shader.PropertyToID("_Clock"), SwayId = Shader.PropertyToID("_Sway");
        private const float LeaveSeconds = .45f, HoverHeight = .1f, RiseSeconds = .35f, CoverScale = 1f;
        /// <summary>What everyone but Nemu still sees of it once it is armed, and what she sees.</summary>
        public const float ShimmerSeen = .16f, OwnerSeen = .45f;
        /// <summary>In the victim's lens: its head's middle this far in front of the camera, this much bigger, so its face
        /// (0.4 m nearer, so 0.38 m off the lens) fills the dark hole round it (`Shaders/DontLookBlind`, its radius 0.8).
        /// (Film r12, 0.75 m with a wider hole, showed the court round it; r13, 0.56 m, was a white wall with eyes at the edges.)</summary>
        private const float LensDistance = .78f, LensScale = 2f;
        private Vector3 _worldAt; private Quaternion _worldFacing; private Vector3 _worldScale; private bool _posedForLens;

        /// <summary>Puts Nemu's ghost on <paramref name="at"/>, retiring her last one. <paramref name="age"/> &gt; 0 restores.</summary>
        public static NemuBlindfold Place(Vector3 at, Vector3 facing, int ownerSlot, float age = 0.0f)
        {
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null && Live[i].OwnerSlot == ownerSlot && !Live[i].Sprung) Live[i].Retire(null);
            var go = new GameObject("NemuBlindfold");
            var spot = VfxShapes.GroundPoint(at);
            go.transform.position = spot;
            var blindfold = go.AddComponent<NemuBlindfold>();
            blindfold.OwnerSlot = ownerSlot;
            blindfold.Spot = spot;
            facing.y = 0.0f;
            blindfold.Facing = facing.sqrMagnitude > 1e-4f ? facing.normalized : Vector3.forward;
            blindfold._age = Mathf.Clamp(float.IsFinite(age) ? age : 0.0f, 0.0f, NecroRules.DontLookLifeSeconds);
            blindfold._smoke = new NemuSmokeBatch(go.transform, "Blindfold", 1, 4, 1);
            blindfold.BuildBody();
            if (blindfold._age <= 0.0f) GameServices.Audio?.PlayAt("sfx_nemu_dontlook_place", spot);
            return blindfold;
        }

        public static NemuBlindfold OwnedBy(int ownerSlot)
        {
            foreach (var p in Live) if (p != null && p.OwnerSlot == ownerSlot && !p.Sprung) return p;
            return null;
        }

        private void BuildBody()
        {
            if (_material == null)
            {
                var shader = Shader.Find("TumbangPreso/BlindfoldGhost");
                if (shader == null) { Debug.LogError("[NemuBlindfold] Shaders/BlindfoldGhost is missing from the build (GameBuilder)."); return; }
                _material = new Material(shader) { name = "BlindfoldGhost" };
            }
            var go = new GameObject("BlindfoldBody");
            go.transform.SetParent(transform, false);
            _mesh = go.AddComponent<MeshFilter>();
            _mesh.sharedMesh = BlindfoldGhostModel.Waiting;
            _body = go.AddComponent<MeshRenderer>();
            _body.sharedMaterial = PetGhostModels.MaterialFor(_mesh.sharedMesh, _material);
            _body.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _body.receiveShadows = false;
            _block = new MaterialPropertyBlock();
        }

        /// <summary>Gone without springing (recast elsewhere, or its time ran out): it sinks back into the ground, leaving a
        /// dormant ghost at <paramref name="dormantAt"/> if there is one.</summary>
        public void Retire(Vector3? dormantAt)
        {
            if (_state == State.Leaving) return;
            _victim = null;
            _dormantAt = dormantAt;
            _state = State.Leaving; _stateAge = 0.0f;
        }

        /// <summary>Done with its victim: it lets go and flies off, and its dormant ghost is left where they stood.</summary>
        private void LetGo()
        {
            _dormantAt = _victim != null ? _victim.transform.position : Spot;
            _state = State.Leaving; _stateAge = 0.0f;
        }

        public Net.WorldEffectSnapshot.Field Capture() => new Net.WorldEffectSnapshot.Field
        {
            Type = Net.WorldEffectSnapshot.Kind.Blindfold, Source = gameObject, Position = Spot, Forward = Facing,
            Duration = NecroRules.DontLookLifeSeconds, Remaining = Remaining, Radius = NecroRules.DontLookTriggerRadius,
            Owner = OwnerSlot,
        };

        private void OnEnable() { Live.Add(this); NemuGhostRings.Ensure(); Camera.onPreCull += HideFromVictim; Camera.onPostRender += ShowAgain; }
        private void OnDisable() { Live.Remove(this); Camera.onPreCull -= HideFromVictim; Camera.onPostRender -= ShowAgain; }

        /// <summary>
        /// ⚠️ ONCE IT IS ON THEIR FACE, THE VICTIM'S OWN CAMERA DOES NOT DRAW IT. Their screen is the blind (`Shaders/DontLookBlind`);
        /// the ghost itself, a metre from their lens, was a black blob over the middle of their view as it tore away (film r8).
        /// Every other camera, and theirs during the lunge, still sees it. (Asked by the camera's place, not the seat: film r9's
        /// solo seat did not count as the local viewer.)
        /// </summary>
        private void HideFromVictim(Camera camera)
        {
            if (_victim == null || camera == null || _body == null) return;
            // A camera at their eyes is theirs (their first person, or a spectator riding it), whoever's seat this is.
            Vector3 at = camera.transform.position;
            float mine = (at - EyesOf(_victim)).sqrMagnitude;
            if (mine >= 1.5f * 1.5f) return;
            // ⚠️ ONLY ITS OWN VICTIM'S (owner 2026-10-21: *"it tries to attach to other players"*). Anyone standing beside the
            // victim has their own camera within that reach too, and it jumped into their lens as well. A camera belongs to the
            // body whose eyes are nearest it, so the ghost clings only to the view that is the victim's.
            var round = GameServices.Round;
            if (round != null)
                foreach (var p in round.Players)
                    if (p != null && p != _victim && p.gameObject.activeInHierarchy && (at - EyesOf(p)).sqrMagnitude < mine) return;
            PoseForLens(camera);
        }

        /// <summary>
        /// ⚠️⚠️ IN THE VICTIM'S OWN LENS THE GHOST JUMPS AT THEM AND CLINGS THERE, THE REAL MODEL, ALIVE. From wherever it is
        /// in the world it comes at the camera over the lunge and ends with its face filling the view, a hand's breadth
        /// off the lens, breathing, its head tilting; at the end it lets go and flies up out of view.
        /// Only this camera's draw is moved (`Camera.onPreCull`), and `ShowAgain` puts it back for every other camera.
        /// (Its lights are world-space, so they are hidden here; the shader's glow carries the eyes.)
        /// </summary>
        private void PoseForLens(Camera camera)
        {
            var cam = camera.transform;
            var body = _body.transform;
            _worldAt = body.position; _worldFacing = body.rotation; _worldScale = body.localScale; _posedForLens = true;
            if (_smoke != null) _smoke.Visible = false;
            float t = _stateAge;
            float tilt = 9f * Mathf.Sin(_age * 1.7f) + 4f * Mathf.Sin(_age * 4.1f);
            Quaternion facing = Quaternion.LookRotation(-cam.forward, cam.up) * Quaternion.AngleAxis(tilt, Vector3.forward);
            float breathe = 1f + .025f * Mathf.Sin(_age * 5.3f);
            Vector3 drift = cam.right * (.03f * Mathf.Sin(_age * 1.3f)) + cam.up * (.02f * Mathf.Sin(_age * 2.2f));
            float lean = .03f * Mathf.Sin(_age * 2.7f);
            Vector3 head = cam.position + cam.forward * (LensDistance - lean) + drift;
            float scale = LensScale * breathe;
            Vector3 at = head - facing * (BlindfoldGhostModel.HeadCentre * scale);
            switch (_state)
            {
                case State.Lunging:
                {
                    // Off the ground and at the lens, fastest at the end.
                    float u = Mathf.Clamp01(t / NecroRules.DontLookLungeSeconds);
                    float e = u * u * u;
                    at = Vector3.Lerp(_worldAt, at, e);
                    facing = Quaternion.Slerp(_worldFacing, facing, Mathf.Clamp01(u * 1.6f));
                    scale = Mathf.Lerp(_worldScale.x, scale, e);
                    break;
                }
                case State.Leaving:
                {
                    // It lets go and is gone up and over their head.
                    float u = Mathf.Clamp01(t / LeaveSeconds);
                    at += (cam.up * 1.6f + cam.forward * .4f) * (u * u);
                    break;
                }
            }
            body.SetPositionAndRotation(at, facing);
            body.localScale = Vector3.one * scale;
        }

        private static Vector3 EyesOf(CharacterMotor body) => body.transform.position + Vector3.up * 1.25f;

        private void ShowAgain(Camera camera)
        {
            if (_posedForLens && _body != null)
            {
                _body.transform.SetPositionAndRotation(_worldAt, _worldFacing);
                _body.transform.localScale = _worldScale;
                _posedForLens = false;
            }
            Show(true);
        }

        private void Show(bool on)
        {
            if (_smoke != null) _smoke.Visible = on;
            if (_body != null) _body.enabled = on && _drawn;
        }

        private bool _drawn;
        private bool _recorded;

        // A replay field carries the waiting ghost's spot, facing and age. Build its
        // authored drawing without Place, live registration, audio or a trigger loop.
        public static NemuBlindfold Recorded(Transform parent, Net.WorldEffectSnapshot.Field field)
        {
            var go = new GameObject("RecordedBlindfold");
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            var ghost = go.AddComponent<NemuBlindfold>();
            ghost.enabled = false;
            ghost._recorded = true;
            ghost.OwnerSlot = field.Owner;
            ghost.Spot = field.Position;
            ghost.Facing = field.Forward;
            ghost._smoke = new NemuSmokeBatch(go.transform, "Blindfold", 1, 4, 1);
            ghost.BuildBody();
            go.SetActive(true);
            return ghost;
        }

        public void StepRecorded(float age)
        {
            _age = Mathf.Clamp(age, 0f, NecroRules.DontLookLifeSeconds);
            Draw();
        }

        private void OnDestroy() { _smoke?.Dispose(); _smoke = null; }

        /// <summary>Can this body trip her ghost? Anyone but Nemu, on their feet near enough, and not already caught.</summary>
        public static bool Trips(Vector3 spot, int ownerSlot, CharacterMotor p)
        {
            if (p == null || !p.gameObject.activeInHierarchy || p.PlayerSlot == ownerSlot ||
                p.PlayerSlot < 0 || p.PlayerSlot >= Balance.PlayerCount || p.IsAloft || p.IsTagged) return false;
            Vector3 d = p.transform.position - spot;
            if (d.y > 2.2f || d.y < -1.0f) return false;
            d.y = 0.0f;
            return d.sqrMagnitude <= NecroRules.DontLookTriggerRadius * NecroRules.DontLookTriggerRadius;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;
            _stateAge += dt;
            if (!NetAuthority.ShouldResolve())
            {
                _victim = _networkVictim >= 0 ? GameServices.Round?.PlayerAt(_networkVictim) : null;
                if (_victim != null && _victim.MovementEpoch != _networkVictimEpoch) _victim = null;
                Draw();
                return;
            }
            switch (_state)
            {
                case State.Waiting:
                    if (_age >= NecroRules.DontLookLifeSeconds) { Retire(Spot); GameServices.Audio?.PlayAt("sfx_nemu_dontlook_fade", Spot); break; }
                    if (Armed) Watch();
                    break;
                case State.Lunging:
                    if (_victim == null || !_victim.gameObject.activeInHierarchy) { Retire(Spot); break; }
                    if (_stateAge >= NecroRules.DontLookLungeSeconds) { _state = State.Covering; _stateAge = 0.0f; }
                    break;
                case State.Covering:
                    // Clinging on until the eyes are uncovered (or the status was cleansed, or a safety net in case it never
                    // replicated on this peer), then it lets go.
                    if (_victim == null || !_victim.gameObject.activeInHierarchy || !_victim.EyesCovered ||
                        _stateAge >= NecroRules.DontLookBlindSeconds + .5f)
                        LetGo();
                    break;
                case State.Leaving:
                    if (_stateAge >= LeaveSeconds)
                    {
                        // ⚠️ THE GATE IS WRITTEN OUT, NOT HOISTED: `tools/audit_ability_authority.py` reads it by brace depth.
                        if (NetAuthority.ShouldResolve())
                        {
                            if (_dormantAt.HasValue && !_recorded)
                                NemuDormant.Spawn(NemuDormant.Ghost.Peeker, _dormantAt.Value, Facing, OwnerSlot);
                        }
                        Destroy(gameObject);
                        return;
                    }
                    break;
            }
            Draw();
        }

        /// <summary>
        /// The host springs it on the first eligible body in reach. Other peers receive the chosen identity.
        /// </summary>
        private void Watch()
        {
            var round = GameServices.Round;
            if (round == null || !round.RoundActive || PresentationClock.Held) return;
            foreach (var p in round.Players)
            {
                if (p == null || p.PlayerSlot == OwnerSlot) continue;
                // ⚠️ THE GATE IS WRITTEN OUT, NOT HOISTED: `tools/audit_ability_authority.py` reads it by brace depth.
                if (NetAuthority.ShouldResolve())
                {
                    if (!Trips(Spot, OwnerSlot, p) || p.IsBlinded) continue;
                    p.ApplyBlinded();
                    // Status immunity refuses it; a ghost that cannot reach the eyes stays waiting for the next one.
                    if (!p.IsBlinded) continue;
                    Spring(p);
                    return;
                }
                // Peers wait for the host's exact ghost/victim identity, never a proximity guess.
            }
        }

        private void Spring(CharacterMotor victim)
        {
            _victim = victim;
            _lungeFrom = BaseAt(_age);
            _state = State.Lunging; _stateAge = 0.0f;
            GameServices.Audio?.PlayAt("sfx_nemu_dontlook_spring", victim.transform.position);
        }

        // ------------------------------------------------------------------ drawing

        /// <summary>Where it floats while it waits: just off the ground over the spot, breathing up and down.</summary>
        private Vector3 BaseAt(float age)
        {
            float rise = Mathf.SmoothStep(0f, 1f, age / RiseSeconds);
            return Spot + Vector3.up * (HoverHeight + .05f * Mathf.Sin(age * 2.1f) - .9f * (1f - rise));
        }

        /// <summary>
        /// Where its head's middle goes to latch on: face to face with the victim, its face pressed to theirs.
        /// ⚠️ Owner on v4: *"actually latch sa face bcz ang laki ng space"*. It hung 0.95 m out in front (a fix for film r2,
        /// where the old smoke ghost was lost inside a head); now it is the victim's head's half depth plus its own.
        /// </summary>
        private static Vector3 FaceOf(CharacterMotor victim) =>
            victim.transform.position + Vector3.up * VictimHeadHeight +
            AheadOf(victim) * (VictimHeadHalfDepth + BlindfoldGhostModel.HeadHalfDepth * CoverScale - .03f);

        /// <summary>The cast's blocky heads (feet to the head's middle, and half its depth), from the court film.</summary>
        private const float VictimHeadHeight = 1.38f, VictimHeadHalfDepth = .28f;

        private static Vector3 AheadOf(CharacterMotor victim)
        {
            Vector3 ahead = victim.transform.forward; ahead.y = 0.0f;
            return ahead.sqrMagnitude > 1e-4f ? ahead.normalized : Vector3.forward;
        }

        /// <summary>How much of it this screen sees: all of it until it fades out at arming, then only Nemu (faint).</summary>
        private float Visibility() => _state != State.Waiting ? 1.0f : Mathf.Clamp01(_age / .2f) * Visibility(_age);

        private float Visibility(float age)
        {
            // A spectator is told everything, as Nemu is (owner, October 22: skills were not visible to spectators);
            // only the players she is hiding it from get the faint shimmer.
            bool mine = GameLaunch.Spectator || NetAuthority.LocalSlot == OwnerSlot;
            float fade = 1.0f - Mathf.SmoothStep(0f, 1f, (age - (NecroRules.DontLookArmSeconds - .8f)) / .8f);
            return Mathf.Max(fade, mine ? OwnerSeen : ShimmerSeen);
        }

        /// <summary>While it waits it looks down Nemu's aim, turning its blind head a little side to side, listening.</summary>
        private Quaternion WaitingFacing() =>
            Quaternion.LookRotation(Quaternion.AngleAxis(14f * Mathf.Sin(_age * .8f), Vector3.up) * Facing);

        private void Draw()
        {
            if (_body == null) return;
            Vector3 at;
            Quaternion facing;
            float scale = 1f, fade = Visibility(), flash = 0f, eyes = 0f, sway = .05f;
            bool sprung = _victim != null;
            switch (_state)
            {
                case State.Lunging:
                {
                    float u = Mathf.Clamp01(_stateAge / NecroRules.DontLookLungeSeconds);
                    float e = u * u * (3f - 2f * u);
                    facing = Quaternion.LookRotation(-AheadOf(_victim));
                    scale = Mathf.Lerp(1f, CoverScale, e);
                    // Off the ground and onto the face in an arc, its head arriving over their eyes.
                    Vector3 to = FaceOf(_victim) - facing * BlindfoldGhostModel.HeadCentre * scale;
                    at = Vector3.Lerp(_lungeFrom, to, e) + Vector3.up * (.3f * Mathf.Sin(u * Mathf.PI));
                    // A cold flash as the cloth drops and its eyes open.
                    flash = .55f * (1f - u);
                    eyes = 1.6f;
                    sway = .12f;
                    break;
                }
                case State.Covering:
                {
                    facing = Quaternion.LookRotation(-AheadOf(_victim));
                    scale = CoverScale;
                    // Clinging on: a small tremble as it holds.
                    Vector3 shake = new Vector3(Mathf.Sin(_stateAge * 37f), Mathf.Sin(_stateAge * 29f + 1f), 0f) * .012f;
                    at = FaceOf(_victim) - facing * (BlindfoldGhostModel.HeadCentre * scale) + facing * shake;
                    eyes = 1.2f + .3f * Mathf.Sin(_stateAge * 9f);
                    sway = .1f;
                    break;
                }
                case State.Leaving when _victim != null:
                {
                    // It lets go and flies up and away over their head, thinning to nothing.
                    float u = Mathf.Clamp01(_stateAge / LeaveSeconds);
                    facing = Quaternion.LookRotation(-AheadOf(_victim));
                    scale = CoverScale;
                    Vector3 from = FaceOf(_victim) - facing * (BlindfoldGhostModel.HeadCentre * scale);
                    at = from + (Vector3.up * 1.1f - AheadOf(_victim) * .4f) * (u * (.6f + .4f * u));
                    fade = 1f - u * u;
                    eyes = 1.2f * (1f - u);
                    break;
                }
                case State.Leaving:
                {
                    // Never sprung: it sinks back into the ground.
                    float u = Mathf.Clamp01(_stateAge / LeaveSeconds);
                    facing = WaitingFacing();
                    at = BaseAt(_age) - Vector3.up * (u * u);
                    fade = Visibility(_age - _stateAge) * (1f - u);
                    break;
                }
                default:
                    facing = WaitingFacing();
                    at = BaseAt(_age);
                    break;
            }
            if (PetGhostModels.PetGhosts)
            {
                // The pet mouse darts round its hiding spot while it waits; the moment it springs it is back on the point (the
                // lunge starts from there). Drawing only: the spot and the trigger radius do not move.
                _flight.Apply(ref at, ref facing, _age, _state == State.Waiting && !sprung ? 1f : 0f, _recorded ? 0f : Time.deltaTime);
                _block.SetVector(TrailId, _flight.TrailLocal);
            }
            var mesh = sprung ? BlindfoldGhostModel.Sprung : BlindfoldGhostModel.Waiting;
            if (_mesh.sharedMesh != mesh)
            {
                _mesh.sharedMesh = mesh;
                // The mint mouse (`PetGhostModels`, behind its switch) is drawn with its own painted material.
                _body.sharedMaterial = PetGhostModels.MaterialFor(mesh, _material);
            }
            var body = _body.transform;
            body.SetPositionAndRotation(at, facing);
            body.localScale = Vector3.one * scale;
            _drawn = fade > .005f;
            _body.enabled = _drawn;
            _block.SetFloat(FadeId, fade);
            _block.SetFloat(FlashId, flash);
            _block.SetFloat(ClockId, _age);
            _block.SetFloat(SwayId, sway);
            _body.SetPropertyBlock(_block);

            if (_smoke == null) return;
            // Its cold air round it, and, sprung, the cyan pinpoints in its eyes.
            _smoke.Begin();
            if (_drawn)
            {
                _smoke.Light(body.TransformPoint(BlindfoldGhostModel.HeadCentre * .6f), .9f * scale, .35f * fade, BlindfoldGhostModel.Aura);
                if (sprung && eyes > 0f)
                    foreach (var eye in BlindfoldGhostModel.Eyes)
                        _smoke.Light(body.TransformPoint(eye), .22f * scale, eyes * fade, BlindfoldGhostModel.Pinpoint);
            }
            _smoke.End(_age);
        }

        /// <summary>A rejoiner's waiting ghost, at the same age. Never springs on restore: the host's status decides that.</summary>
        public static NemuBlindfold Restore(Net.WorldEffectSnapshot.Field field, float elapsed)
        {
            float age = field.Duration - Mathf.Clamp(field.Remaining - elapsed, 0.0f, field.Duration);
            // Network peers hydrate from the host's complete ghost frame, including already sprung ghosts.
            return NetAuthority.ShouldResolve() ? Place(field.Position, field.Forward, field.Owner, Mathf.Max(.01f, age)) : null;
        }

        /// <summary>Every ghost on the court goes (the round ended, or the snapshot is replacing them).</summary>
        public static void RetireAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null) Destroy(Live[i].gameObject);
            Live.Clear();
        }
    }
}
