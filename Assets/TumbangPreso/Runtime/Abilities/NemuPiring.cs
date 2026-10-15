using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ PIRING, THE GHOST DON'T LOOK LEAVES WAITING (owner 2026-10-15, `NecroRules.DontLook*`). *"place a ghost in an area and
    /// it becomes invisible after 3 seconds and then if anyone goes near that area the ghost would lunge at their eyes and
    /// cover it for 2 secondns and slow them"*. Gekko's way of fighting: a creature of her own does the work while she is
    /// elsewhere. One per Nemu (`Place` retires her last one).
    ///
    ///  * WAITING: one of the ult's dead (`NemuSouls`, the same smoke and faces), coiled low over the spot. Every screen sees
    ///    it settle; over its first 3 s it fades from everyone but Nemu, who keeps a faint copy so she knows where it is.
    ///  * SPRUNG: the first other player within 1.8 m once it is armed. ONLY THE HOST DECIDES: it writes BLINDED
    ///    (`CharacterMotor.ApplyBlinded`), and `SyncUnit` carries that clock to every peer. Every other peer learns of the
    ///    spring from that replicated edge: a player near its ghost going Blinded is its victim. No new message.
    ///  * THE LUNGE: visible to everyone, it shoots off the ground to the victim's face in 0.22 s and clamps over the
    ///    eyes, its tail streaming back round their head, until the eyes are uncovered, then it tears away.
    ///    The victim's own screen gets its own treatment (`UI.TumpHudEffects`, `Shaders/PiringBlind`).
    ///  * Untripped, it goes after 30 s. A rejoiner gets the waiting ghost at the same age (`WorldEffectSnapshot.Kind.Piring`).
    ///
    /// Each peer runs the same clock from the accepted cast, so its cues are local, not `NetCue` (`audit_cue_relay.py`).
    /// </summary>
    public sealed class NemuPiring : MonoBehaviour
    {
        public static readonly List<NemuPiring> Live = new List<NemuPiring>();

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
        private readonly int _soul = 7;
        private const int TailPoints = 9;
        private readonly Vector3[] _tail = new Vector3[TailPoints];
        private readonly bool[] _on = new bool[TailPoints];
        private const float LeaveSeconds = .4f, HoverHeight = .85f, HeadSize = .72f;

        /// <summary>Puts Nemu's ghost on <paramref name="at"/>, retiring her last one. <paramref name="age"/> &gt; 0 restores.</summary>
        public static NemuPiring Place(Vector3 at, Vector3 facing, int ownerSlot, float age = 0.0f)
        {
            for (int i = Live.Count - 1; i >= 0; i--)
                if (Live[i] != null && Live[i].OwnerSlot == ownerSlot && !Live[i].Sprung) Live[i].Retire();
            var go = new GameObject("NemuPiring");
            var spot = VfxShapes.GroundPoint(at);
            go.transform.position = spot;
            var piring = go.AddComponent<NemuPiring>();
            piring.OwnerSlot = ownerSlot;
            piring.Spot = spot;
            facing.y = 0.0f;
            piring.Facing = facing.sqrMagnitude > 1e-4f ? facing.normalized : Vector3.forward;
            piring._age = Mathf.Clamp(float.IsFinite(age) ? age : 0.0f, 0.0f, NecroRules.DontLookLifeSeconds);
            piring._smoke = new NemuSmokeBatch(go.transform, "Piring", 24, 6, 16);
            if (piring._age <= 0.0f) GameServices.Audio?.PlayAt("sfx_nemu_dontlook_place", spot);
            return piring;
        }

        public static NemuPiring OwnedBy(int ownerSlot)
        {
            foreach (var p in Live) if (p != null && p.OwnerSlot == ownerSlot && !p.Sprung) return p;
            return null;
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
            Type = Net.WorldEffectSnapshot.Kind.Piring, Source = gameObject, Position = Spot, Forward = Facing,
            Duration = NecroRules.DontLookLifeSeconds, Remaining = Remaining, Radius = NecroRules.DontLookTriggerRadius,
            Owner = OwnerSlot,
        };

        private void OnEnable() { Live.Add(this); Camera.onPreCull += HideFromVictim; Camera.onPostRender += ShowAgain; }
        private void OnDisable() { Live.Remove(this); Camera.onPreCull -= HideFromVictim; Camera.onPostRender -= ShowAgain; }

        /// <summary>
        /// ⚠️ ONCE IT IS ON THEIR FACE, THE VICTIM'S OWN CAMERA DOES NOT DRAW IT. Their screen is the blind (`Shaders/PiringBlind`);
        /// the ghost itself, a metre from their lens, was a black blob over the middle of their view as it tore away (film r8).
        /// Every other camera, and theirs during the lunge, still sees it. (Asked by the camera's place, not the seat: film r9's
        /// solo seat did not count as the local viewer.)
        /// </summary>
        private void HideFromVictim(Camera camera)
        {
            if (_smoke == null || _victim == null || camera == null) return;
            if (_state != State.Covering && _state != State.Leaving) return;
            // A camera at their eyes is theirs (their first person, or a spectator riding it), whoever's seat this is.
            Vector3 eyes = _victim.transform.position + Vector3.up * 1.25f;
            if ((camera.transform.position - eyes).sqrMagnitude < 1.5f * 1.5f) _smoke.Visible = false;
        }

        private void ShowAgain(Camera camera) { if (_smoke != null) _smoke.Visible = true; }
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
                    // Clamped on until the eyes are uncovered (or the status was cleansed, or a safety net in case it never
                    // replicated on this peer), then it tears away.
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
            _lungeFrom = HeadAt(_age);
            _state = State.Lunging; _stateAge = 0.0f;
            GameServices.Audio?.PlayAt("sfx_nemu_dontlook_spring", victim.transform.position);
        }

        // ------------------------------------------------------------------ drawing

        /// <summary>Where its head floats while it waits: low over the spot, breathing up and down, peering about.</summary>
        private Vector3 HeadAt(float age)
        {
            float bob = .06f * Mathf.Sin(age * 2.1f);
            Vector3 sway = Quaternion.AngleAxis(25f * Mathf.Sin(age * .7f), Vector3.up) * Facing * .08f;
            return Spot + Vector3.up * (HoverHeight + bob) + sway;
        }

        /// <summary>The victim's face: in front of their eyes, where the ghost clamps on.</summary>
        private static Vector3 FaceOf(CharacterMotor victim)
        {
            Vector3 ahead = victim.transform.forward; ahead.y = 0.0f;
            ahead = ahead.sqrMagnitude > 1e-4f ? ahead.normalized : Vector3.forward;
            // Out in front of the face, not inside it: the cast's heads are big blocks (film r2 lost the ghost in one).
            return victim.transform.position + Vector3.up * 1.35f + ahead * .95f;
        }

        /// <summary>How much of it this screen sees: all of it until it fades out at arming, then only Nemu (faint).</summary>
        private float Visibility()
        {
            if (_state != State.Waiting) return 1.0f;
            float settle = Mathf.Clamp01(_age / .35f);
            bool mine = NetAuthority.LocalSlot == OwnerSlot && !GameLaunch.Spectator;
            float fade = 1.0f - Mathf.SmoothStep(0f, 1f, (_age - (NecroRules.DontLookArmSeconds - .8f)) / .8f);
            return settle * (mine ? Mathf.Max(fade, .4f) : fade);
        }

        private void Draw()
        {
            if (_smoke == null) return;
            var cam = Camera.main;
            float vis = Visibility();
            _smoke.Begin();
            if (cam == null || vis <= .005f) { _smoke.End(_age); return; }
            Vector3 eye = cam.transform.position;
            Vector3 head, back;
            float size = HeadSize, fade = vis, glow = .9f;
            switch (_state)
            {
                case State.Lunging:
                {
                    float u = Mathf.Clamp01(_stateAge / NecroRules.DontLookLungeSeconds);
                    Vector3 to = FaceOf(_victim);
                    // Up off the ground and down onto the face: an arc, fast at the end.
                    float e = u * u * (3f - 2f * u);
                    head = Vector3.Lerp(_lungeFrom, to, e) + Vector3.up * (.35f * Mathf.Sin(u * Mathf.PI));
                    back = (_lungeFrom - to).sqrMagnitude > 1e-4f ? (_lungeFrom - to).normalized : Vector3.down;
                    size = Mathf.Lerp(HeadSize, HeadSize * 1.6f, e);
                    glow = 1.4f;
                    break;
                }
                case State.Covering:
                {
                    head = FaceOf(_victim);
                    // Its tail wraps back over their head and streams behind them.
                    back = -_victim.transform.forward + Vector3.up * .35f;
                    size = HeadSize * 1.6f;
                    glow = 2.2f + .4f * Mathf.Sin(_stateAge * 9f);
                    break;
                }
                case State.Leaving:
                {
                    float u = Mathf.Clamp01(_stateAge / LeaveSeconds);
                    if (_victim != null)
                    {
                        // Torn away from the face, up and back, thinning to nothing.
                        Vector3 from = FaceOf(_victim);
                        head = from + (Vector3.up * .9f - _victim.transform.forward * .5f) * u;
                        back = -_victim.transform.forward;
                    }
                    else
                    {
                        // Never sprung: it sinks back into the ground.
                        head = HeadAt(_age) - Vector3.up * HoverHeight * u;
                        back = Vector3.down;
                    }
                    fade = vis * (1f - u) * (_victim != null ? 1f : Visibility(_age - _stateAge));
                    break;
                }
                default:
                    head = HeadAt(_age);
                    back = Vector3.down + Facing * -.15f;
                    break;
            }
            back = back.sqrMagnitude > 1e-4f ? back.normalized : Vector3.down;
            // A procedural tail behind the head, swaying, its far end flicking.
            for (int k = 0; k < TailPoints; k++)
            {
                float u = k / (float)(TailPoints - 1);
                Vector3 side = Vector3.Cross(back, Vector3.up);
                if (side.sqrMagnitude < 1e-4f) side = Vector3.right;
                side.Normalize();
                float wave = Mathf.Sin(_age * 4.3f - k * .9f) * .09f * u;
                _tail[k] = head + back * (u * 1.3f) + side * wave + Vector3.up * (.05f * Mathf.Sin(_age * 3.1f + k)) * u;
                if (_state == State.Waiting) _tail[k].y = Mathf.Max(_tail[k].y, Spot.y + .03f);
                _on[k] = true;
            }
            NemuSouls.Draw(_smoke, _tail, _on, TailPoints, _soul, size, fade, glow, _age, eye);
            _smoke.End(_age);
        }

        private float Visibility(float age)
        {
            bool mine = NetAuthority.LocalSlot == OwnerSlot && !GameLaunch.Spectator;
            float fade = 1.0f - Mathf.SmoothStep(0f, 1f, (age - (NecroRules.DontLookArmSeconds - .8f)) / .8f);
            return mine ? Mathf.Max(fade, .4f) : fade;
        }

        /// <summary>A rejoiner's waiting ghost, at the same age. Never springs on restore: the host's status decides that.</summary>
        public static NemuPiring Restore(Net.WorldEffectSnapshot.Field field, float elapsed)
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
