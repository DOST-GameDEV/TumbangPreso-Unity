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
        private readonly RaycastHit[] _hits = new RaycastHit[32];
        private int _shownBeat = -2;

        public IEnumerator Run()
        {
            // Let the new gameplay rig establish its first real eye pose before saving it.
            yield return null;
            while (PresentationClock.Held) yield return null;
            PresentationClock.Hold(); _held = true;
            while (HubLoading.Visible) yield return null;
            _camera = Camera.main;
            if (_camera == null) { Finish(); yield break; }
            _position = _camera.transform.position; _rotation = _camera.transform.rotation; _fov = _camera.fieldOfView;
            _rig = _camera.GetComponent<CameraRig>();
            _spectator = _camera.GetComponent<SpectatorCamera>();
            _rigActive = _rig != null && _camera.enabled && (_spectator == null || !_spectator.enabled);
            if (_rigActive) _rig.SetActive(false);
            // SpectatorCamera already respects PresentationClock.Held. Disabling it would
            // unhook its highlight subscriptions, which a temporary shot must never do.
            _camera.enabled = true;
            foreach (var player in FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
                if (player.PlayerSlot >= 0 && player.PlayerSlot < _players.Length) _players[player.PlayerSlot] = player;
            FreshInput();
            BuildCaption();
            var map = SceneFlow.PreviewFor(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            Vector3 centre = Vector3.zero; int count = 0;
            foreach (var p in _players) if (p != null) { centre += p.transform.position; count++; }
            if (count > 0) centre /= count;
            centre.y += 1.2f;
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            for (float age = 0; age < Seconds; age += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(age / Seconds);
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
                Caption(beat, map);
                yield return null;
            }
            Finish();
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
                clear = Mathf.Min(clear, Mathf.Max(.8f, _hits[i].distance - .3f));
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
            if (_canvas != null) { Destroy(_canvas.gameObject); _canvas = null; }
            if (_camera != null)
            {
                _camera.transform.SetPositionAndRotation(_position, _rotation); _camera.fieldOfView = _fov;
                if (_rigActive && _rig != null) _rig.SetActive(true);
                _camera = null;
            }
            if (_held) { PresentationClock.Release(); _held = false; FreshInput(); }
        }
        public void Cancel() => Finish();
        private void OnDisable() => Finish();
        private void OnDestroy() => Finish();
    }
}
