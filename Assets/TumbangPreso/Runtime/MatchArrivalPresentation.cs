using System.Collections;
using TumbangPreso.CameraSystem;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso
{
    /// <summary>
    /// A short arrival in the actual arena: establish the place, introduce its four players,
    /// then return the camera for the shared countdown. No model or physical spawn is moved.
    /// This owns only the pre-round hold and restores every camera setting if loading is interrupted.
    /// </summary>
    public sealed class MatchArrivalPresentation : MonoBehaviour
    {
        private static MatchArrivalPresentation _active;
        public static bool Active => _active != null;
        public static bool OwnsCamera => Active && !_active._returningToGameplay
            && Settings.SettingsStore.Current.CinematicCameraMotion;
        public const float Seconds = 8.6f;
        private const float EstablishSeconds = 2.8f, PortraitSeconds = 1.1f;
        private const float HandoffStart = 7.2f, PortraitFov = 50;
        private readonly Vector3[] _focus = new Vector3[4];
        private readonly Vector3[] _eyes = new Vector3[4];
        private Vector3 _centre, _wideStart, _wideEnd;
        private Image _ink;
        private CanvasGroup _captionGroup;
        private Camera _camera;
        private CameraRig _rig;
        private SpectatorCamera _spectator;
        private bool _rigActive, _held;
        private Vector3 _position;
        private Quaternion _rotation;
        private float _fov;
        private Canvas _canvas;
        private Text _title, _detail;
        private readonly CharacterMotor[] _players = new CharacterMotor[Core.Balance.PlayerCount];
        private readonly Visual.CharacterAnimator[] _poses = new Visual.CharacterAnimator[Core.Balance.PlayerCount];
        private readonly RaycastHit[] _hits = new RaycastHit[32];
        private int _shownBeat = -2;
        private int _runGeneration;
        private bool _returningToGameplay;

        public IEnumerator Run()
        {
            Cancel();
            _shownBeat = -2; System.Array.Clear(_players, 0, _players.Length);
            int generation = _runGeneration;
            var playback = RunCurrent(generation);
            try { while (playback.MoveNext()) yield return playback.Current; }
            finally
            {
                (playback as System.IDisposable)?.Dispose();
                if (generation == _runGeneration) Finish();
            }
        }

        private bool Current(int generation) => this != null && generation == _runGeneration;

        private IEnumerator RunCurrent(int generation)
        {
            _active = this;
            _returningToGameplay = false;
            BuildCaption();
            _ink.color = new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, 1);
            _captionGroup.alpha = 0;
            // Cover direct arena entry too. Never expose the inherited map/player camera
            // while waiting for installation, prewarm or the rig's first LateUpdate.
            yield return null;
            if (!Current(generation)) yield break;
            while (PresentationClock.Held) { if (!Current(generation)) yield break; yield return null; }
            if (!Current(generation)) yield break;
            PresentationClock.Hold(); _held = true;
            while (HubLoading.Preparing) { if (!Current(generation)) yield break; yield return null; }
            if (!Current(generation)) yield break;
            _camera = Camera.main;
            if (_camera == null) yield break;
            _rig = _camera.GetComponent<CameraRig>();
            _spectator = _camera.GetComponent<SpectatorCamera>();
            _rigActive = _rig != null && _camera.enabled && (_spectator == null || !_spectator.enabled);
            if (_rigActive) _rig.PrepareArrivalReturnView();
            _position = _camera.transform.position; _rotation = _camera.transform.rotation; _fov = _camera.fieldOfView;
            if (_rigActive && Settings.SettingsStore.Current.CinematicCameraMotion) _rig.SetActive(false);
            // SpectatorCamera already respects PresentationClock.Held. Disabling it would
            // unhook its highlight subscriptions, which a temporary shot must never do.
            _camera.enabled = true;
            foreach (var player in FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
                if (player.PlayerSlot >= 0 && player.PlayerSlot < _players.Length) _players[player.PlayerSlot] = player;
            FreshInput();
            for (int i = 0; i < _players.Length; i++)
                _poses[i] = _players[i] != null ? _players[i].GetComponent<Visual.CharacterAnimator>() : null;
            var map = SceneFlow.PreviewFor(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            PrepareShots(map);
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            // ARENA-INTRO begin. The Arena has its own opening in place of these shots (`Map.ArenaIntro`:
            // the tunnel, the glare, the taya on the screens, the stage built). Null on every other
            // map, and false when there is none to play here: then nothing below is different. The
            // hold, the input, the curtain and the camera's return stay this component's (`Finish`).
            var opening = Map.ArenaIntro.Ensure();
            if (opening != null && _rigActive && opening.Begin(_camera, _players, _position, _rotation, _fov, reduced))
            {
                _ink.color = Color.clear; _captionGroup.alpha = 0;
                while (HubLoading.Visible) { if (!Current(generation)) yield break; yield return null; }
                opening.Roll();
                while (opening != null && opening.Playing)
                {
                    if (!Current(generation)) yield break;
                    // The same single handoff as below: the rig is live again for the last of the return.
                    if (opening.Age >= Mathf.Lerp(opening.Timeline.Handoff, opening.Timeline.End, .7f) && MayReturnToGameplay()) _rig.SetActive(true);
                    yield return null;
                }
                yield break;
            }
            // ARENA-INTRO end.
            Sample(0, reduced, map);
            // Loading can now fade onto the already-prepared opening curtain/shot.
            while (HubLoading.Visible) { if (!Current(generation)) yield break; yield return null; }
            for (float age = 0; age < Seconds; age += Time.unscaledDeltaTime)
            {
                if (!Current(generation)) yield break;
                Sample(age, reduced, map);
                yield return null;
            }
        }

        private void PrepareShots(SceneFlow.MapEntry map)
        {
            _centre = Vector3.zero; int count = 0;
            foreach (var p in _players) if (p != null) { _centre += p.transform.position; count++; }
            if (count > 0) _centre /= count;
            var can = FindFirstObjectByType<Lata>();
            if (can != null) _centre = can.transform.position;
            _centre += Vector3.up;
            // Keep the map's authored low bridge camera and taller rooftop/cove viewpoints.
            _wideStart = ClearEye(_centre, _centre + Quaternion.Euler(0, map.Yaw - 4, 0) *
                new Vector3(0, map.Height, -map.Distance * 1.04f));
            _wideEnd = ClearEye(_centre, _centre + Quaternion.Euler(0, map.Yaw + 2, 0) *
                new Vector3(0, map.Height * .97f, -map.Distance));
            for (int i = 0; i < _players.Length; i++)
            {
                var actor = _players[i];
                if (actor == null) { _focus[i] = _centre; _eyes[i] = _wideEnd; continue; }
                // Hold a neutral grounded idle before measuring; only the featured player greets.
                _poses[i]?.SetArrivalPose(i, .001f);
                Bounds bounds = DrawnBounds(actor);
                _focus[i] = bounds.center - Vector3.up * bounds.size.y * .035f;
                float tangent = Mathf.Tan(PortraitFov * Mathf.Deg2Rad * .5f);
                float distance = Mathf.Max(bounds.extents.y / (.68f * tangent),
                    Mathf.Max(bounds.extents.x, bounds.extents.z) / (.68f * tangent * Mathf.Max(.5f, _camera.aspect)));
                distance = Mathf.Max(2.6f, distance + bounds.extents.z);
                // Search for an unobstructed front three-quarter shot before accepting a crop.
                float best = float.NegativeInfinity;
                for (int angle = 0; angle < 7; angle++)
                {
                    float yaw = angle == 0 ? 12 : (angle % 2 == 1 ? -1 : 1) * ((angle + 1) / 2) * 25;
                    Vector3 desired = _focus[i] + Quaternion.Euler(0, yaw, 0) * actor.transform.forward * distance
                        + Vector3.up * bounds.size.y * .12f;
                    Vector3 eye = ClearEye(_focus[i], desired);
                    float clearance = Vector3.Distance(eye, _focus[i]) / Vector3.Distance(desired, _focus[i]);
                    float score = clearance * 10 - Mathf.Abs(yaw) * .008f;
                    if (score > best) { best = score; _eyes[i] = eye; }
                }
            }
        }

        private static Bounds DrawnBounds(CharacterMotor actor)
        {
            var model = actor.GetComponent<Visual.CharacterVisual>()?.Model;
            var bounds = new Bounds(actor.transform.position + Vector3.up, new Vector3(1, 2, 1));
            if (model == null) return bounds;
            bool found = false;
            // Bake the actual neutral pose, not the importer's all-animation envelope.
            var mesh = new Mesh();
            try
            {
                foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                {
                    if (!renderer.enabled) continue;
                    Bounds local;
                    if (renderer is SkinnedMeshRenderer skin && skin.sharedMesh != null)
                    { skin.BakeMesh(mesh); local = mesh.bounds; }
                    else
                    {
                        var filter = renderer.GetComponent<MeshFilter>();
                        if (filter == null || filter.sharedMesh == null) continue;
                        local = filter.sharedMesh.bounds;
                    }
                    for (int corner = 0; corner < 8; corner++)
                    {
                        Vector3 point = renderer.transform.TransformPoint(local.center + Vector3.Scale(local.extents,
                            new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)));
                        if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                        else bounds.Encapsulate(point);
                    }
                }
            }
            finally { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
            return bounds;
        }

        // A deterministic sample shared by playback and native composition checks.
        private void Sample(float age, bool reduced, SceneFlow.MapEntry map)
        {
            age = Mathf.Clamp(age, 0, Seconds);
            int beat = reduced || age < EstablishSeconds ? -1 :
                Mathf.Clamp((int)((age - EstablishSeconds) / PortraitSeconds), 0, 3);
            float local = beat < 0 ? 0 : (age - EstablishSeconds - beat * PortraitSeconds) / PortraitSeconds;
            float handoff = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(HandoffStart, Seconds, age));
            for (int i = 0; i < _poses.Length; i++)
                _poses[i]?.SetArrivalPose(i, reduced ? .001f : Mathf.Max(.001f,
                    i == beat ? Mathf.SmoothStep(0, 1, local / .32f) * (1 - handoff) : 0));
            Vector3 eye = reduced ? _wideEnd : Vector3.Lerp(_wideStart, _wideEnd,
                Mathf.SmoothStep(0, 1, age / EstablishSeconds));
            Vector3 focus = _centre;
            float fov = 55;
            if (beat >= 0)
            {
                focus = _focus[beat];
                eye = Vector3.Lerp(_eyes[beat], focus, .025f * Mathf.SmoothStep(0, 1, local));
                eye = ClearEye(focus, eye); fov = PortraitFov;
            }
            Quaternion rotation = Quaternion.LookRotation(focus - eye, Vector3.up);
            // One deliberate handoff; never cut back to a second establishing shot.
            if (age >= HandoffStart)
            {
                if (reduced)
                {
                    if (handoff >= .5f) { eye = _position; rotation = _rotation; fov = _fov; }
                }
                else
                {
                    eye = Vector3.Lerp(eye, _position, handoff);
                    rotation = Quaternion.Slerp(rotation, _rotation, handoff);
                    fov = Mathf.Lerp(fov, _fov, handoff);
                }
                if (handoff >= .7f && MayReturnToGameplay())
                { _returningToGameplay = true; _rig.SetActive(true); }
            }
            bool cameraMotion = Settings.SettingsStore.Current.CinematicCameraMotion;
            if (!cameraMotion) { eye = _position; rotation = _rotation; fov = _fov; }
            _camera.transform.SetPositionAndRotation(eye, rotation); _camera.fieldOfView = fov;
            float ink = 1 - Mathf.SmoothStep(0, 1, age / .5f);
            if (!reduced)
                for (int cut = 0; cut < 4; cut++)
                    ink = Mathf.Max(ink, 1 - Mathf.Clamp01(Mathf.Abs(age - EstablishSeconds - cut * PortraitSeconds) / .12f));
            else if (age >= HandoffStart)
                ink = 1 - Mathf.Clamp01(Mathf.Abs(handoff - .5f) / .22f);
            if (!cameraMotion) ink = 0;
            if (_ink != null) _ink.color = new Color(HubStyle.Ink.r, HubStyle.Ink.g, HubStyle.Ink.b, ink);
            if (_captionGroup != null) _captionGroup.alpha = (1 - ink) * (1 - handoff);
            Caption(beat, map);
        }

        private Vector3 ClearEye(Vector3 focus, Vector3 desired)
        {
            Vector3 ray = desired - focus;
            float distance = ray.magnitude;
            int count = Physics.SphereCastNonAlloc(focus, .2f, ray.normalized, _hits, distance, ~0, QueryTriggerInteraction.Ignore);
            float clear = distance;
            for (int i = 0; i < count; i++)
            {
                var collider = _hits[i].collider;
                if (collider == null || collider.GetComponentInParent<CharacterMotor>() != null ||
                    collider.GetComponentInParent<Slipper>() != null || collider.GetComponentInParent<Lata>() != null) continue;
                // A framing minimum must not push the eye through a closer wall.
                // Retain a nonzero look vector while honoring the actual clearance.
                clear = Mathf.Min(clear, Mathf.Max(.01f, _hits[i].distance - .3f));
            }
            return focus + ray.normalized * clear;
        }

        private void BuildCaption()
        {
            _canvas = OwnerUiLayout.Canvas(transform, "MatchArrivalCanvas", 150);
            var root = (RectTransform)_canvas.transform;
            var curtain = new GameObject("CutInk", typeof(RectTransform), typeof(Image));
            curtain.transform.SetParent(root, false);
            _ink = curtain.GetComponent<Image>(); _ink.raycastTarget = false;
            HubKit.Stretch(_ink.rectTransform); _ink.color = Color.clear;
            var plate = HubKit.Place(HubKit.Rect(root, "ArrivalCaption"), HubKit.BottomLeft,
                new Vector2(HubKit.Margin, HubKit.Margin + 60), new Vector2(760, 156));
            _captionGroup = plate.gameObject.AddComponent<CanvasGroup>();
            _captionGroup.blocksRaycasts = false;
            HubKit.Stretch(HubKit.Shape(plate, "Plate", HubStyle.Night, false, 1301, 5, 24).rectTransform);
            _title = HubKit.Text(plate, "Title", "", HubStyle.Display, true, HubStyle.Honey);
            HubKit.Place(_title.rectTransform, HubKit.TopLeft, new Vector2(26, -12), new Vector2(708, 80));
            _detail = HubKit.Text(plate, "Detail", "", HubStyle.Label, false, HubStyle.Golden);
            HubKit.Place(_detail.rectTransform, HubKit.TopLeft, new Vector2(28, -96), new Vector2(704, 48));
        }

        private void Caption(int beat, SceneFlow.MapEntry map)
        {
            if (_shownBeat == beat) return;
            _shownBeat = beat;
            var player = beat >= 0 ? _players[beat] : null;
            _title.text = player != null ? player.CharacterName().ToUpperInvariant() : map.Name;
            _title.fontSize = HubStyle.Size(HubStyle.Display); HubKit.Fit(_title, 708);
            _detail.text = player != null ? "PLAYER " + (beat + 1) + "  ·  " + (beat == Core.MatchRules.DefenderSlotFor(1) ? "TAYA" : "ATTACKER")
                : "ROUND 1  ·  " + (SceneFlow.SelectedMode == Core.GameMode.Classic ? "CLASSIC" : "HERO STRIKE");
        }

        private void FreshInput()
        {
            foreach (var actor in _players) actor?.Intent.RequireFreshActions();
        }

        private bool MayReturnToGameplay()
        {
            if (!_rigActive || _rig == null) return false;
            // The public seat handoff may change ownership during the film.
            // Its current HUD role outranks the saved opening/launch preference.
            if (Hud.Instance != null) return !Hud.Instance.Spectating;
            var watcher = _spectator != null ? _spectator : FindFirstObjectByType<SpectatorCamera>();
            return !GameLaunch.Spectator && (watcher == null || !watcher.isActiveAndEnabled);
        }

        private void Finish()
        {
            // ARENA-INTRO: the Arena's opening, if one is up, puts back everything it posed. A no-op elsewhere.
            Map.ArenaIntro.Stop();
            if (_active == this) _active = null;
            for (int i = 0; i < _poses.Length; i++)
            {
                if (_poses[i] != null) _poses[i].SetArrivalPose(i, 0);
                _poses[i] = null;
            }
            if (_canvas != null) { Destroy(_canvas.gameObject); _canvas = null; }
            if (_camera != null)
            {
                _camera.transform.SetPositionAndRotation(_position, _rotation); _camera.fieldOfView = _fov;
                if (_rigActive && _rig != null) _rig.SetActive(MayReturnToGameplay());
                _camera = null;
            }
            if (_held) { PresentationClock.Release(); _held = false; FreshInput(); }
        }
        public void Cancel() { _runGeneration++; Finish(); }
        private void OnDisable() => Cancel();
        private void OnDestroy() => Cancel();
    }
}
