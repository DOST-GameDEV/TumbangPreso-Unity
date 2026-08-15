using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    public enum CameraMode { Fpp, Tpp }

    public enum AimSource { Mouse, Movement }

    /// <summary>
    /// The player camera. Ported from `camera_rig.gd`.
    ///
    /// ⚠️⚠️ THE GAME IS FIRST PERSON. A Person is ALWAYS FPP and a Prop is ALWAYS TPP; that is
    /// a stated directive, not a preference, and an earlier version of this port used a
    /// third-person follow camera for everything, which is a different game.
    ///
    /// ⚠️⚠️ THE CAMERA NEVER INHERITS THE BODY'S ROLL OR PITCH, AND THIS IS THE INVARIANT.
    /// It was reported three times from playtests, with screenshots: the whole 3D view rolled
    /// forty degrees while the HUD stayed level. The pivots are children of the body, so they
    /// inherit its full basis, and anything that leaves a non-yaw component on the body lands
    /// directly in the player's eye. Patching each writer was tried in pieces and failed. So
    /// the rig stops trusting its parent: every frame both pivots are given an ABSOLUTE
    /// rotation built from the body's YAW ONLY plus the rig's own pitch. Whatever the body does
    /// on the other two axes cannot reach the camera, from any code path, including ones
    /// nobody has written yet.
    ///
    /// ⚠️ AND YAW IS RECOVERED FROM THE FORWARD VECTOR, NOT FROM EULER ANGLES. Decomposing a
    /// basis that has roll in it does not give back the yaw you want, which is precisely the
    /// situation this exists to survive.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        // -------------------------------------------------------------------
        // Constants, carried over exactly.
        // -------------------------------------------------------------------

        public const float PitchMinDeg = -80.0f;
        public const float PitchMaxDeg = 70.0f;
        public const float BaseSensitivity = 0.15f;

        /// <summary>
        /// ⚠️ MEASURED, AND THE OLD 1.55 MUST NOT BE RESTORED. That value was "eye height on a
        /// 1.6 unit capsule" and was never checked against a model: it put the whole character
        /// below the near edge of the frame. 0.45 is 55% of the way up the head mesh, which is
        /// the eye line on the actual rig.
        /// </summary>
        public const float FppEyeHeight = 0.45f;

        /// <summary>Meshes whose name contains this are hidden in first person.</summary>
        public const string FppHiddenMeshHint = "head";

        public const float ViewmodelScale = 0.72f;

        /// <summary>
        /// ⚠️ PUSHED DOWN AND BACK AFTER SCALING. Shrinking the arms alone just leaves smaller
        /// arms in the same commanding spot; down clears the centre of frame and back is what
        /// actually stops them subtending half the vertical FOV.
        /// </summary>
        public static readonly Vector3 ViewmodelSeat = new Vector3(0.0f, -0.10f, -0.16f);

        public const float PersonCapsuleHeight = 1.6f;
        public const float TppMinSpringLength = 1.8f;
        public const float TppMinPitchDeg = -34.0f;
        public const float TppBaseSpringLength = 4.5f;
        public const float TppBasePitchDeg = -15.0f;
        public const float TppMountHeight = 1.2f;

        public const float VmKickTime = 0.22f;

        public const float EmotePitchMinDeg = -35.0f;
        public const float EmotePitchMaxDeg = 20.0f;

        // -------------------------------------------------------------------

        [SerializeField] private AimSource _aimSource = AimSource.Movement;
        [SerializeField] private float _fieldOfView = 75.0f;

        private CharacterMotor _character;
        private CameraMode _mode = CameraMode.Fpp;

        private Transform _fppPivot;
        private Transform _tppPivot;
        private Transform _viewmodel;
        private ViewmodelArms _arms;
        private UnityEngine.Camera _camera;

        private float _pitchDeg;
        private float _tppPitchDeg = TppBasePitchDeg;
        private float _tppSpringLength = TppBaseSpringLength;

        private bool _active;

        private readonly List<Renderer> _hiddenForFpp = new List<Renderer>();

        private float _shakeStrength;
        private float _shakeLeft;
        private float _vmKickLeft;
        private Vector3 _vmKickOffset;

        private bool _emoteView;
        private Social.EmotePlayer _emotes;
        private float _emoteYawDeg;
        private float _emotePitchDeg;
        private CameraMode _modeBeforeEmote = CameraMode.Fpp;

        public CameraMode Mode => _mode;
        public AimSource Aim => _aimSource;
        public bool IsLocalFpp => _active && _mode == CameraMode.Fpp;

        /// <summary>Is this rig looking through <paramref name="who"/>? A shake or a kick
        /// applied to another unit's camera is feedback landing on the wrong screen.</summary>
        public bool IsFollowing(CharacterMotor who) => _character == who && _active;
        public UnityEngine.Camera Camera => _camera;

        // -------------------------------------------------------------------

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            if (_camera == null) _camera = gameObject.AddComponent<UnityEngine.Camera>();

            _camera.fieldOfView = _fieldOfView;
            _camera.nearClipPlane = 0.05f;
        }

        /// <summary>
        /// Attach to a body. ⚠️ A PERSON IS ALWAYS FPP.
        /// </summary>
        public void Follow(CharacterMotor character, bool makeActive = true)
        {
            UnsubscribeEmotes();

            _character = character;
            _mode = CameraMode.Fpp;

            if (_character == null) return;

            SubscribeEmotes();
            BuildPivots();
            ApplyFppSelfHide();
            SetActive(makeActive);
        }

        /// <summary>
        /// ⚠️⚠️ THE EMOTE SWING IS WIRED HERE, ON THE RIG, AND THAT IS WHAT KEEPS IT LOCAL.
        ///
        /// 🧑 2026-08-04: *"i want the emotes to switch camera to TPP js for the emote and go
        /// back to FPP after the emote ends"*. The emote itself is replicated; the camera
        /// swing must NOT be, or every peer would spin to third person because somebody else
        /// danced.
        ///
        /// Subscribing from the rig gets that for free rather than by a flag someone has to
        /// remember: a rig only ever follows the unit this machine is looking through, so a
        /// remote player's emote has no rig subscribed to it and cannot move any camera.
        /// Wiring this the other way round — EmotePlayer reaching for a camera — would need
        /// an "am I local" test at the call site, which is the check that gets forgotten.
        /// </summary>
        private void SubscribeEmotes()
        {
            _emotes = _character.GetComponent<Social.EmotePlayer>();
            if (_emotes == null) return;

            _emotes.EmoteStarted += OnEmoteStarted;
            _emotes.EmoteStopped += OnEmoteStopped;
        }

        private void UnsubscribeEmotes()
        {
            if (_emotes == null) return;

            _emotes.EmoteStarted -= OnEmoteStarted;
            _emotes.EmoteStopped -= OnEmoteStopped;
            _emotes = null;
        }

        private void OnEmoteStarted(string id) => BeginEmoteView();

        /// ⚠️ AN EMOTE NEVER ENDS ON ITS OWN. 🧑 2026-08-15: *"the emotes only end when a
        /// user does smth to interrupt it like move or attack"*. So this fires on exactly one
        /// path — <see cref="Social.EmotePlayer.Stop"/>, reached by movement, a verb, or the
        /// unit losing the right to act — and there is no timer to race it.
        ///
        /// If a clip-finished path is ever added, it MUST route through Stop() as well.
        /// Restoring the camera from a second place is how a rig ends up stuck in third
        /// person: one path returns the view and the other silently does not.
        private void OnEmoteStopped() => EndEmoteView();

        private void OnDestroy() => UnsubscribeEmotes();

        private void BuildPivots()
        {
            // ⚠️ THE PIVOTS ARE NOT PARENTED TO THE BODY. In Godot they were children and
            // inherited its basis, which is the whole reason the roll bug existed. Here they
            // are free transforms positioned from the body every frame, so a rolled body
            // cannot reach them by construction rather than by care.
            if (_fppPivot == null)
            {
                _fppPivot = new GameObject("~FppPivot").transform;
                _tppPivot = new GameObject("~TppPivot").transform;
            }

            MountViewmodel();
        }

        /// <summary>
        /// ⚠️ FIRST PERSON GETS DEDICATED VIEWMODEL ARMS, NOT THE RIG'S OWN. From playtest:
        /// "don't see arms of ppl". The real body tops out below the eye line because the chibi
        /// head is big enough that the eye sits above the shoulders, so looking down showed
        /// nothing at all. The arms are mounted to the camera pivot and inherit its pitch, so
        /// they rise and fall with the view, and a remote player's rig is never the one being
        /// looked through.
        /// </summary>
        private void MountViewmodel()
        {
            if (_viewmodel != null) return;

            // ⚠️ BUILT IN CODE, NOT LOADED AS A PREFAB. This used to `Resources.Load` a
            // "Models/ViewmodelArms" prefab that has never existed, so it returned null and
            // returned early — every FPP view has been armless for the whole port, silently,
            // because a missing prefab is not an error. ViewmodelArms builds itself from the
            // .tscn's own baked transforms instead, so there is nothing to author and nothing
            // to go missing.
            var go = new GameObject("~ViewmodelArms");
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * ViewmodelScale;
            go.transform.localPosition = ViewmodelSeat;
            go.transform.localRotation = Quaternion.identity;

            _arms = go.AddComponent<ViewmodelArms>();

            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);

            _viewmodel = go.transform;
        }

        public void SetActive(bool active)
        {
            _active = active;
            if (_camera != null) _camera.enabled = active;

            if (_viewmodel != null) _viewmodel.gameObject.SetActive(active && _mode == CameraMode.Fpp);

            if (active) ApplyFppSelfHide();
            else RestoreSelfHide();
        }

        public void SetAimSource(AimSource source) => _aimSource = source;

        // -------------------------------------------------------------------

        private void LateUpdate()
        {
            if (_character == null || !_active) return;

            if (_emoteView) { ApplyEmoteView(); return; }

            StepLook();

            if (_mode == CameraMode.Fpp) ApplyFpp();
            else ApplyTpp();

            StepShake();
            StepViewmodelKick();
        }

        private void StepLook()
        {
            if (_aimSource != AimSource.Mouse) return;
            if (_character.Intent.Parked) return;

            // ⚠️ THE EMOTE WHEEL OWNS THE MOUSE WHILE IT IS OPEN. Both this and the wheel are
            // steered by the same deltas, and without this the player's body spins on the spot
            // while they pick a slice. Godot got this from `_input` running before
            // `_unhandled_input`; Unity has no such ordering, so it is an explicit check.
            if (UI.EmoteWheel.AnyOpen) return;

            var s = Settings.SettingsStore.Current;
            float sens = BaseSensitivity * s.MouseSensitivity;

            float dx = Input.GetAxisRaw("Mouse X") * sens;
            float dy = Input.GetAxisRaw("Mouse Y") * sens;
            if (s.InvertY) dy = -dy;

            // ⚠️ YAW GOES ON THE BODY, PITCH STAYS ON THE RIG. The body turning is what makes
            // a throw leave along the sight line; a rig that yawed on its own would let the
            // player look one way and throw another.
            if (Mathf.Abs(dx) > 0.0001f)
                _character.transform.Rotate(Vector3.up, dx * 10.0f, Space.World);

            _pitchDeg = Mathf.Clamp(_pitchDeg - dy * 10.0f, PitchMinDeg, PitchMaxDeg);
        }

        /// <summary>
        /// ⚠️ RECOVERED FROM THE FORWARD VECTOR. Euler decomposition of a basis carrying roll
        /// does not return the yaw you want, and that is exactly the case this survives.
        /// </summary>
        private float BodyYawDeg()
        {
            Vector3 forward = _character.transform.forward;

            if (Mathf.Abs(forward.x) < 0.00001f && Mathf.Abs(forward.z) < 0.00001f)
                return _character.transform.eulerAngles.y; // straight up or down: degenerate

            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        private void ApplyFpp()
        {
            float yaw = BodyYawDeg();
            Vector3 eye = _character.transform.position + Vector3.up * (PersonCapsuleHeight * 0.5f + FppEyeHeight);

            // Absolute, from yaw and pitch only. The body's roll cannot reach this.
            transform.SetPositionAndRotation(eye, Quaternion.Euler(_pitchDeg, yaw, 0.0f));

            if (_viewmodel != null && !_viewmodel.gameObject.activeSelf)
                _viewmodel.gameObject.SetActive(true);

            // The hand shows what the unit is actually carrying.
            if (_arms != null) _arms.SetHolding(_character.HoldingSlipper);
        }

        private void ApplyTpp()
        {
            float yaw = BodyYawDeg();
            Vector3 mount = _character.transform.position + Vector3.up * TppMountHeight;

            var rot = Quaternion.Euler(Mathf.Max(_tppPitchDeg, TppMinPitchDeg), yaw, 0.0f);
            Vector3 wanted = mount - (rot * Vector3.forward) * _tppSpringLength;

            // ⚠️ THE SPRING ARM EXCLUDES THE BODY IT IS WATCHING, or the cast hits the
            // character's own capsule every frame and drags the camera in against it.
            float length = _tppSpringLength;
            if (Physics.SphereCast(mount, 0.2f, (wanted - mount).normalized, out var hit,
                                   _tppSpringLength, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<CharacterMotor>() != _character)
                    length = Mathf.Max(TppMinSpringLength, hit.distance);
            }

            transform.SetPositionAndRotation(mount - (rot * Vector3.forward) * length, rot);

            if (_viewmodel != null) _viewmodel.gameObject.SetActive(false);
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ HIDE THE HEAD, NOT THE WHOLE BODY. The chibi head is large enough that in first
        /// person it fills the frame, but the body itself sits below the eye line and is what
        /// the player sees when they look down. Hiding everything leaves them floating.
        /// </summary>
        private void ApplyFppSelfHide()
        {
            RestoreSelfHide();

            if (_character == null || _mode != CameraMode.Fpp) return;

            foreach (var r in _character.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled) continue;
                if (r.name.IndexOf(FppHiddenMeshHint, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                r.enabled = false;
                _hiddenForFpp.Add(r);
            }
        }

        private void RestoreSelfHide()
        {
            foreach (var r in _hiddenForFpp)
                if (r != null) r.enabled = true;

            _hiddenForFpp.Clear();
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ SHAKE IS FOR THE PERSON IT HAPPENED TO. A body block shakes the BLOCKER's camera
        /// because it is a thing they did. Shaking everyone tells three other players something
        /// happened to them when nothing did.
        ///
        /// ⚠️ AND IT IS NOT HITSTOP. Hitstop writes a global time scale, which is fine for a
        /// shove on a long cooldown and completely wrong for a block that can fire as fast as
        /// three attackers can throw.
        /// </summary>
        public void Shake(float strength = 0.35f, float duration = 0.18f)
        {
            _shakeStrength = Mathf.Max(_shakeStrength, strength);
            _shakeLeft = Mathf.Max(_shakeLeft, duration);
        }

        private void StepShake()
        {
            if (_shakeLeft <= 0.0f) return;

            _shakeLeft -= Time.deltaTime;
            float k = Mathf.Clamp01(_shakeLeft) * _shakeStrength;

            transform.position += new Vector3(
                (Random.value - 0.5f) * 2.0f * k * 0.1f,
                (Random.value - 0.5f) * 2.0f * k * 0.1f,
                0.0f);

            if (_shakeLeft <= 0.0f) _shakeStrength = 0.0f;
        }

        /// <summary>A punch of the arms toward the player on an action, so a verb is felt.</summary>
        public void ViewmodelKick(Vector3 direction, float strength = 1.0f)
        {
            _vmKickLeft = VmKickTime;
            _vmKickOffset = direction.normalized * 0.06f * strength;
        }

        private void StepViewmodelKick()
        {
            if (_viewmodel == null) return;

            if (_vmKickLeft > 0.0f) _vmKickLeft -= Time.deltaTime;

            float k = _vmKickLeft <= 0.0f ? 0.0f : _vmKickLeft / VmKickTime;
            _viewmodel.localPosition = ViewmodelSeat + _vmKickOffset * k;
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ AN EMOTE SWITCHES TO A THIRD-PERSON LOOK, because the whole point of an emote is
        /// that YOU can see it too. In first person a player performing one sees nothing at all
        /// and reasonably concludes it did not fire.
        /// </summary>
        public void BeginEmoteView()
        {
            if (_emoteView) return;

            _emoteView = true;
            _modeBeforeEmote = _mode;
            _emoteYawDeg = BodyYawDeg() + 180.0f;
            _emotePitchDeg = -10.0f;

            if (_viewmodel != null) _viewmodel.gameObject.SetActive(false);
        }

        public void EndEmoteView()
        {
            if (!_emoteView) return;

            _emoteView = false;
            _mode = _modeBeforeEmote;

            if (_viewmodel != null)
                _viewmodel.gameObject.SetActive(_active && _mode == CameraMode.Fpp);
        }

        public bool IsEmoteView => _emoteView;

        private void ApplyEmoteView()
        {
            _emotePitchDeg = Mathf.Clamp(_emotePitchDeg, EmotePitchMinDeg, EmotePitchMaxDeg);

            Vector3 focus = _character.transform.position + Vector3.up * 1.0f;
            var rot = Quaternion.Euler(_emotePitchDeg, _emoteYawDeg, 0.0f);

            transform.SetPositionAndRotation(focus - (rot * Vector3.forward) * 2.6f, rot);
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// Where this player is aiming. ⚠️ IN FIRST PERSON IT IS THE SIGHT LINE, which is what
        /// makes the throw leave along the line the player is looking down. Measured: leaving
        /// from the hand instead sags the flight 0.38 to 0.43 m below that line and drops the
        /// slipper out of the bottom of the screen on release.
        /// </summary>
        public Vector3 AimPoint()
        {
            if (_character == null) return Vector3.zero;

            if (_mode == CameraMode.Fpp)
                return transform.position + transform.forward * 20.0f;

            var ground = new Plane(Vector3.up, _character.transform.position);
            var ray = new Ray(transform.position, transform.forward);

            return ground.Raycast(ray, out float enter)
                ? ray.GetPoint(enter)
                : _character.transform.position + _character.transform.forward * 10.0f;
        }
    }
}
