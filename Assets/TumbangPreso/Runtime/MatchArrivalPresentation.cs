using System.Collections;
using TumbangPreso.CameraSystem;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso
{
    /// <summary>
    /// Eight-second arrival in the actual arena: establish the place, introduce its four players,
    /// then return the camera for the shared countdown. No model or physical spawn is moved.
    /// This owns only the pre-round hold and restores every camera setting if loading is interrupted.
    /// </summary>
    public sealed class MatchArrivalPresentation : MonoBehaviour
    {
        public const float Seconds = 8;
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
            // Let the new gameplay rig establish its first real eye pose before saving it.
            yield return null;
            if (!Current(generation)) yield break;
            while (PresentationClock.Held) { if (!Current(generation)) yield break; yield return null; }
            if (!Current(generation)) yield break;
            PresentationClock.Hold(); _held = true;
            while (HubLoading.Visible) { if (!Current(generation)) yield break; yield return null; }
            if (!Current(generation)) yield break;
            _camera = Camera.main;
            if (_camera == null) yield break;
            _position = _camera.transform.position; _rotation = _camera.transform.rotation; _fov = _camera.fieldOfView;
            _rig = _camera.GetComponent<CameraRig>();
            _spectator = _camera.GetComponent<SpectatorCamera>();
            bool cameraMotion = Settings.SettingsStore.Current.CinematicCameraMotion;
            _rigActive = _rig != null && _camera.enabled && (_spectator == null || !_spectator.enabled);
            if (_rigActive && cameraMotion) _rig.SetActive(false);
            // SpectatorCamera already respects PresentationClock.Held. Disabling it would
            // unhook its highlight subscriptions, which a temporary shot must never do.
            _camera.enabled = true;
            foreach (var player in FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
                if (player.PlayerSlot >= 0 && player.PlayerSlot < _players.Length) _players[player.PlayerSlot] = player;
            FreshInput();
            for (int i = 0; i < _players.Length; i++)
                _poses[i] = _players[i] != null ? _players[i].GetComponent<Visual.CharacterAnimator>() : null;
            BuildCaption();
            var map = SceneFlow.PreviewFor(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            Vector3 centre = Vector3.zero; int count = 0;
            foreach (var p in _players) if (p != null) { centre += p.transform.position; count++; }
            if (count > 0) centre /= count;
            centre.y += 1.2f;
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            for (float age = 0; age < Seconds; age += Time.unscaledDeltaTime)
            {
                if (!Current(generation)) yield break;
                float t = Mathf.Clamp01(age / Seconds);
                for (int i = 0; i < _poses.Length; i++)
                    if (_poses[i] != null) _poses[i].SetArrivalPose(i, reduced ? 1 :
                        Mathf.SmoothStep(0, 1, Mathf.Min(age / .35f, (Seconds - age) / .35f)));
                int beat = age < 2.4f || age >= 6.8f ? -1 : Mathf.Clamp((int)((age - 2.4f) / 1.1f), 0, 3);
                Vector3 focus = centre;
                Vector3 offset = Quaternion.Euler(0, map.Yaw + (reduced ? 0 : Mathf.Lerp(-5, 5, t)), 0)
                    * new Vector3(0, Mathf.Min(map.Height, 16), -Mathf.Min(map.Distance, 30));
                if (!reduced && beat >= 0 && _players[beat] != null)
                {
                    var actor = _players[beat].transform;
                    focus = actor.position + Vector3.up * 1.2f;
                    float local = Mathf.Repeat((age - 2.4f) / 1.1f, 1);
                    offset = Quaternion.Euler(0, Mathf.Lerp(-7, 7, local), 0) * actor.forward * 4.6f + Vector3.up * .65f;
                }
                if (cameraMotion)
                {
                    Vector3 eye = ClearEye(focus, focus + offset);
                    _camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(focus - eye, Vector3.up));
                    _camera.fieldOfView = 55;
                    if (!reduced && age > 7.35f)
                    {
                        float returnT = Mathf.SmoothStep(0, 1, (age - 7.35f) / .65f);
                        _camera.transform.position = Vector3.Lerp(eye, _position, returnT);
                        _camera.transform.rotation = Quaternion.Slerp(_camera.transform.rotation, _rotation, returnT);
                        _camera.fieldOfView = Mathf.Lerp(55, _fov, returnT);
                    }
                }
                Caption(beat, map);
                yield return null;
            }
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
            var plate = HubKit.Place(HubKit.Rect(root, "ArrivalCaption"), HubKit.BottomLeft,
                new Vector2(HubKit.Margin, HubKit.Margin + 100), new Vector2(1060, 200));
            HubKit.Stretch(HubKit.Shape(plate, "Plate", HubStyle.Night, false, 1301, 5, 24).rectTransform);
            _title = HubKit.Text(plate, "Title", "", HubStyle.Display, true, HubStyle.Honey);
            HubKit.Place(_title.rectTransform, HubKit.TopLeft, new Vector2(30, -18), new Vector2(1000, 105));
            _detail = HubKit.Text(plate, "Detail", "", HubStyle.Label, false, HubStyle.Golden);
            HubKit.Place(_detail.rectTransform, HubKit.TopLeft, new Vector2(34, -122), new Vector2(990, 62));
        }

        private void Caption(int beat, SceneFlow.MapEntry map)
        {
            if (_shownBeat == beat) return;
            _shownBeat = beat;
            var player = beat >= 0 ? _players[beat] : null;
            _title.text = player != null ? player.CharacterName().ToUpperInvariant() : map.Name;
            _title.fontSize = HubStyle.Size(HubStyle.Display); HubKit.Fit(_title, 1000);
            _detail.text = player != null ? "PLAYER " + (beat + 1) + "  ·  " + (beat == Core.MatchRules.DefenderSlotFor(1) ? "TAYA" : "ATTACKER")
                : "ROUND 1  ·  " + (SceneFlow.SelectedMode == Core.GameMode.Classic ? "CLASSIC" : "HERO STRIKE");
        }

        private void FreshInput()
        {
            foreach (var actor in _players) actor?.Intent.RequireFreshActions();
        }

        private void Finish()
        {
            for (int i = 0; i < _poses.Length; i++)
            {
                if (_poses[i] != null) _poses[i].SetArrivalPose(i, 0);
                _poses[i] = null;
            }
            if (_canvas != null) { Destroy(_canvas.gameObject); _canvas = null; }
            if (_camera != null)
            {
                _camera.transform.SetPositionAndRotation(_position, _rotation); _camera.fieldOfView = _fov;
                if (_rigActive && _rig != null) _rig.SetActive(true);
                _camera = null;
            }
            if (_held) { PresentationClock.Release(); _held = false; FreshInput(); }
        }
        public void Cancel() { _runGeneration++; Finish(); }
        private void OnDisable() => Cancel();
        private void OnDestroy() => Cancel();
    }
}
