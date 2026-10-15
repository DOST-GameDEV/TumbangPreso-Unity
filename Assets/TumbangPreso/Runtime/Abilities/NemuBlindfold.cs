using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ BLINDFOLD, THE GHOST DON'T LOOK LEAVES WAITING (owner 2026-10-15, `NecroRules.DontLook*`). *"place a ghost in an area and
    /// it becomes invisible after 3 seconds and then if anyone goes near that area the ghost would lunge at their eyes and
    /// cover it for 2 secondns and slow them"*. Gekko's way of fighting: a creature of her own does the work while she is
    /// elsewhere. One per Nemu (`Place` retires her last one).
    ///
    ///  * WAITING: Blindfold itself (`Visual.BlindfoldGhostModel`, the owner's approved mockup: an icy ghost child with a
    ///    cloth tied over its eyes), floating low over the spot, facing down Nemu's aim. Every screen sees it rise out of the
    ///    ground; over its first 3 s it fades from everyone but Nemu, who keeps a faint copy so she knows where it is.
    ///  * SPRUNG: the first other player within 1.8 m once it is armed. ONLY THE HOST DECIDES: it writes BLINDED
    ///    (`CharacterMotor.ApplyBlinded`), and `SyncUnit` carries that clock to every peer. Every other peer learns of the
    ///    spring from that replicated edge: a player near its ghost going Blinded is its victim. No new message.
    ///  * THE LUNGE: visible to everyone, its cloth drops to its neck, its cyan pinpoint eyes open, and it shoots at the
    ///    victim's face in 0.22 s, arms out, and clings over their eyes until they are uncovered, then lets go and flies
    ///    off. The victim's own screen is the same ghost from inside: its cloth over their eyes (`Shaders/DontLookBlind`).
    ///  * Untripped, it goes after 30 s. A rejoiner gets the waiting ghost at the same age (`WorldEffectSnapshot.Kind.Blindfold`).
    ///
    /// Each peer runs the same clock from the accepted cast, so its cues are local, not `NetCue` (`audit_cue_relay.py`).
    /// </summary>
    public sealed class NemuBlindfold : MonoBehaviour
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
        public CharacterMotor Victim => _victim;
        public float Remaining => Sprung ? 0.0f : Mathf.Max(0.0f, NecroRules.DontLookLifeSeconds - _age);

        private State _state;
        private float _age, _stateAge;
        private CharacterMotor _victim;
        private Vector3 _lungeFrom;
        private NemuSmokeBatch _smoke;
        private MeshFilter _mesh;
        private MeshRenderer _body;
        private MaterialPropertyBlock _block;
        private static Material _material;
        private static readonly int FadeId = Shader.PropertyToID("_Fade"), FlashId = Shader.PropertyToID("_Flash"),
            ClockId = Shader.PropertyToID("_Clock"), SwayId = Shader.PropertyToID("_Sway");
        private const float LeaveSeconds = .45f, HoverHeight = .1f, RiseSeconds = .35f, CoverScale = 1.15f;

        /// <summary>Puts Nemu's ghost on <paramref name="at"/>, retiring her last one. <paramref name="age"/> &gt; 0 restores.</summary>
        public static NemuBlindfold Place(Vector3 at, Vector3 facing, int ownerSlot, float age = 0.0f)
        {
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null && Live[i].OwnerSlot == ownerSlot && !Live[i].Sprung) Live[i].Retire();
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
            _body.sharedMaterial = _material;
            _body.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _body.receiveShadows = false;
            _block = new MaterialPropertyBlock();
        }

        /// <summary>Gone without springing (recast elsewhere, or round over): it sinks back into the ground.</summary>
        public void Retire()
        {
            if (_state == State.Leaving) return;
            _victim = null;
            _state = State.Leaving; _stateAge = 0.0f;
        }

        public Net.WorldEffectSnapshot.Field Capture() => new Net.WorldEffectSnapshot.Field
        {
            Type = Net.WorldEffectSnapshot.Kind.Blindfold, Source = gameObject, Position = Spot, Forward = Facing,
            Duration = NecroRules.DontLookLifeSeconds, Remaining = Remaining, Radius = NecroRules.DontLookTriggerRadius,
            Owner = OwnerSlot,
        };

        private void OnEnable() { Live.Add(this); Camera.onPreCull += HideFromVictim; Camera.onPostRender += ShowAgain; }
        private void OnDisable() { Live.Remove(this); Camera.onPreCull -= HideFromVictim; Camera.onPostRender -= ShowAgain; }

        /// <summary>
        /// ⚠️ ONCE IT IS ON THEIR FACE, THE VICTIM'S OWN CAMERA DOES NOT DRAW IT. Their screen is the blind (`Shaders/DontLookBlind`);
        /// the ghost itself, a metre from their lens, was a black blob over the middle of their view as it tore away (film r8).
        /// Every other camera, and theirs during the lunge, still sees it. (Asked by the camera's place, not the seat: film r9's
        /// solo seat did not count as the local viewer.)
        /// </summary>
        private void HideFromVictim(Camera camera)
        {
            if (_victim == null || camera == null) return;
            if (_state != State.Covering && _state != State.Leaving) return;
            // A camera at their eyes is theirs (their first person, or a spectator riding it), whoever's seat this is.
            Vector3 eyes = _victim.transform.position + Vector3.up * 1.25f;
            if ((camera.transform.position - eyes).sqrMagnitude < 1.5f * 1.5f) Show(false);
        }

        private void ShowAgain(Camera camera) => Show(true);

        private void Show(bool on)
        {
            if (_smoke != null) _smoke.Visible = on;
            if (_body != null) _body.enabled = on && _drawn;
        }

        private bool _drawn;

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
            switch (_state)
            {
                case State.Waiting:
                    if (_age >= NecroRules.DontLookLifeSeconds) { Retire(); GameServices.Audio?.PlayAt("sfx_nemu_dontlook_fade", Spot); break; }
                    if (Armed) Watch();
                    break;
                case State.Lunging:
                    if (_victim == null || !_victim.gameObject.activeInHierarchy) { Retire(); break; }
                    if (_stateAge >= NecroRules.DontLookLungeSeconds) { _state = State.Covering; _stateAge = 0.0f; }
                    break;
                case State.Covering:
                    // Clinging on until the eyes are uncovered (or the status was cleansed, or a safety net in case it never
                    // replicated on this peer), then it lets go.
                    if (_victim == null || !_victim.gameObject.activeInHierarchy || !_victim.EyesCovered ||
                        _stateAge >= NecroRules.DontLookBlindSeconds + .5f)
                    { _state = State.Leaving; _stateAge = 0.0f; }
                    break;
                case State.Leaving:
                    if (_stateAge >= LeaveSeconds) { Destroy(gameObject); return; }
                    break;
            }
            Draw();
        }

        /// <summary>
        /// The host springs it on the first body in reach. Every other peer springs its copy when a body near it goes
        /// Blinded (the host's replicated edge), within a looser reach because that body has moved on since.
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
                if (!p.IsBlinded || p.BlindedElapsed > .75f) continue;
                Vector3 d = p.transform.position - Spot; d.y = 0.0f;
                if (d.magnitude > NecroRules.DontLookTriggerRadius + 2.0f) continue;
                Spring(p);
                return;
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

        /// <summary>The victim's face: in front of their eyes, where the ghost's head clings on.</summary>
        private static Vector3 FaceOf(CharacterMotor victim) =>
            // Out in front of the face, not inside it: the cast's heads are big blocks (film r2 lost the ghost in one).
            victim.transform.position + Vector3.up * 1.35f + AheadOf(victim) * .95f;

        private static Vector3 AheadOf(CharacterMotor victim)
        {
            Vector3 ahead = victim.transform.forward; ahead.y = 0.0f;
            return ahead.sqrMagnitude > 1e-4f ? ahead.normalized : Vector3.forward;
        }

        /// <summary>How much of it this screen sees: all of it until it fades out at arming, then only Nemu (faint).</summary>
        private float Visibility() => _state != State.Waiting ? 1.0f : Mathf.Clamp01(_age / .2f) * Visibility(_age);

        private float Visibility(float age)
        {
            bool mine = NetAuthority.LocalSlot == OwnerSlot && !GameLaunch.Spectator;
            float fade = 1.0f - Mathf.SmoothStep(0f, 1f, (age - (NecroRules.DontLookArmSeconds - .8f)) / .8f);
            return mine ? Mathf.Max(fade, .4f) : fade;
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
            var mesh = sprung ? BlindfoldGhostModel.Sprung : BlindfoldGhostModel.Waiting;
            if (_mesh.sharedMesh != mesh) _mesh.sharedMesh = mesh;
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
            return Place(field.Position, field.Forward, field.Owner, Mathf.Max(.01f, age));
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
