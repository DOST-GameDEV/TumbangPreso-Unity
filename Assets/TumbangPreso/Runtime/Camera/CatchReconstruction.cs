using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TumbangPreso.Visual;
using TumbangPreso.UI;

namespace TumbangPreso.CameraSystem
{
    // A victim-only view of recorded contact. Simulation, aim and the taya's
    // camera continue untouched; ending the overlay never changes recovery.
    [DefaultExecutionOrder(1900)]
    public sealed class CatchReconstruction : MonoBehaviour
    {
        private MatchPoseHistory _history;
        private MatchPoseHistory.Track _actorTrack, _victimTrack;
        private MatchPoseHistory.Copy _actorCopy, _victimCopy;
        private CharacterMotor _victim;
        private CameraRig _rig;
        private GameObject _stage;
        private Camera _camera;
        private RenderTexture _target;
        private Canvas _canvas;
        private RawImage _picture;
        private Text _caption;
        private float _contact, _began, _duration;
        private int _round;
        private readonly List<Renderer> _hidden = new List<Renderer>();
        private readonly List<bool> _previous = new List<bool>();
        private readonly List<Renderer> _scratch = new List<Renderer>();
        private readonly HashSet<Renderer> _seen = new HashSet<Renderer>();
        private Renderer[] _copiedItems;
        public bool Playing => _stage != null;
        public float Remaining => Playing ? Mathf.Max(0, _duration - (Time.unscaledTime - _began)) : 0;

        public static void Attach(GameObject owner)
        {
            var history = owner.GetComponent<MatchPoseHistory>() ?? owner.AddComponent<MatchPoseHistory>();
            var view = owner.GetComponent<CatchReconstruction>() ?? owner.AddComponent<CatchReconstruction>();
            view._history = history;
        }
        private void OnEnable() => MatchFlair.Presented += OnMoment;
        private void OnDisable() { MatchFlair.Presented -= OnMoment; End(); }
        private void OnDestroy() => End();
        private void OnMoment(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            if (kind != MatchFlair.Kind.Tag || _history == null || Panel.AnyOpen ||
                Settings.SettingsStore.Current.ReducedUiMotion) return;
            var round = GameServices.Round;
            var victim = round != null ? round.PlayerAt(subject) : null;
            var rig = Camera.main != null ? Camera.main.GetComponent<CameraRig>() : null;
            if (victim == null || rig == null || !rig.IsFollowing(victim) || victim.StunLeft <= .3f) return;
            if (Playing && _victim == victim) return;
            var a = _history.ForSeat(actor); var b = _history.ForSeat(subject);
            if (a == null || b == null || !a.Ready || !b.Ready) return;
            // The host emits before teleport. A late remote event must locate
            // recorded contact, not photograph the already-teleported body.
            float contact = b.ContactTime(at);
            if (contact < 0) return;
            if ((victim.transform.position - at).sqrMagnitude < .25f)
            { a.Record(Time.time); b.Record(Time.time); contact = Time.time; }
            Begin(a, b, victim, rig, contact);
        }
        private void Begin(MatchPoseHistory.Track actor, MatchPoseHistory.Track victimTrack,
                           CharacterMotor victim, CameraRig rig, float contact)
        {
            End();
            _stage = new GameObject("~CatchPlaybackCopies"); _stage.SetActive(false);
            _actorCopy = actor.Clone(_stage.transform); _victimCopy = victimTrack.Clone(_stage.transform);
            if (_actorCopy == null || _victimCopy == null) { End(); return; }
            _actorTrack = actor; _victimTrack = victimTrack; _victim = victim; _rig = rig;
            _contact = contact; _began = Time.unscaledTime; _duration = Mathf.Min(1.1f, victim.StunLeft - .18f);
            _round = GameServices.Match != null ? GameServices.Match.RoundNumber : 0;
            actor.Apply(_actorCopy, contact); victimTrack.Apply(_victimCopy, contact);
            CopyHeldItem(victimTrack, _victimCopy);
            _stage.SetActive(true);
            BuildView();
        }
        private void CopyHeldItem(MatchPoseHistory.Track track, MatchPoseHistory.Copy copy)
        {
            var held = track.Actor.GetComponent<Carrier>()?.Held;
            var visual = track.Actor.GetComponent<CharacterVisual>();
            if (held == null || visual == null || visual.HandAnchor == null) return;
            var anchor = track.CopiedBone(copy, visual.HandAnchor);
            if (anchor == null) return;
            var renderers = new List<Renderer>();
            foreach (var source in held.GetComponentsInChildren<MeshRenderer>())
            {
                if (source.GetComponent<VfxRenderTag>() != null) continue;
                var mesh = source.GetComponent<MeshFilter>(); if (mesh == null || mesh.sharedMesh == null) continue;
                var go = new GameObject("RecordedHeldSlipper"); go.transform.SetParent(anchor, false);
                go.transform.localPosition = visual.HandAnchor.InverseTransformPoint(source.transform.position);
                go.transform.localRotation = Quaternion.Inverse(visual.HandAnchor.rotation) * source.transform.rotation;
                Vector3 parentScale = visual.HandAnchor.lossyScale, scale = source.transform.lossyScale;
                go.transform.localScale = new Vector3(scale.x / parentScale.x, scale.y / parentScale.y, scale.z / parentScale.z);
                go.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
                var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterials = source.sharedMaterials;
                var block = new MaterialPropertyBlock(); source.GetPropertyBlock(block); renderer.SetPropertyBlock(block);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.forceRenderingOff = true; renderers.Add(renderer);
            }
            _copiedItems = renderers.ToArray();
        }
        private void BuildView()
        {
            var go = new GameObject("~CatchPlaybackCamera"); go.transform.SetParent(_stage.transform, false);
            _camera = go.AddComponent<Camera>(); _camera.CopyFrom(Camera.main); _camera.enabled = false;
            _camera.tag = "Untagged"; _camera.fieldOfView = 58; _camera.nearClipPlane = .08f;
            _camera.depth = -100; _camera.clearFlags = CameraClearFlags.Skybox;
            int width = Mathf.Clamp(Screen.width, 960, 1920);
            int height = Mathf.Max(540, Mathf.RoundToInt(width * Screen.height / (float)Mathf.Max(1, Screen.width)));
            _target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = "CatchPlaybackFrame" };
            _target.Create(); _camera.targetTexture = _target;
            _canvas = OwnerUiLayout.Canvas(transform, "CatchReconstructionCanvas", 180);
            var focus = _canvas.GetComponent<InputLayer.ScreenFocus>(); if (focus != null) focus.enabled = false;
            _picture = OwnerUiLayout.Rect(_canvas.transform, "RecordedCatch").gameObject.AddComponent<RawImage>();
            OwnerUiLayout.Fill(_picture.rectTransform); _picture.texture = _target; _picture.raycastTarget = false;
            _caption = OwnerUiLayout.Text(_canvas.transform, "CatchIdentity", "CAUGHT BY " + PlayerIdentity.Label(_actorTrack.Actor.PlayerSlot),
                34, OwnerUiLayout.TypeRole.Display);
            _caption.alignment = TextAnchor.MiddleCenter; _caption.color = PlayerIdentity.Colour(_actorTrack.Actor.PlayerSlot);
            var rect = _caption.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = new Vector2(0, 62); rect.sizeDelta = new Vector2(900, 62);
            var outline = _caption.gameObject.AddComponent<Outline>(); outline.effectColor = Color.black; outline.effectDistance = new Vector2(2, -2);
        }
        private void LateUpdate()
        {
            if (!Playing) return;
            float elapsed = Time.unscaledTime - _began;
            if (_victim == null || _rig == null || !_rig.IsFollowing(_victim) || _victim.StunLeft <= .18f ||
                _victim.CanAct() || Panel.AnyOpen || GameServices.Round == null || !GameServices.Round.RoundActive ||
                GameServices.Match == null || GameServices.Match.RoundNumber != _round || elapsed >= _duration)
            { End(); return; }
            float recordedTime = _contact - .28f + Mathf.Clamp01(elapsed / .48f) * .28f;
            _actorTrack.Apply(_actorCopy, recordedTime); _victimTrack.Apply(_victimCopy, recordedTime);
            Vector3 a = _actorCopy.Root.transform.position, b = _victimCopy.Root.transform.position;
            Vector3 forward = b - a; forward.y = 0;
            if (forward.sqrMagnitude < .01f) forward = _victimCopy.Root.transform.forward;
            forward.Normalize(); Vector3 side = Vector3.Cross(Vector3.up, forward);
            Vector3 focus = (a + b) * .5f + Vector3.up * .9f;
            Vector3 wanted = focus + side * 2.7f - forward * .7f + Vector3.up * .35f;
            Vector3 offset = wanted - focus; float distance = offset.magnitude;
            foreach (var hit in Physics.RaycastAll(focus, offset.normalized, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<CharacterMotor>() != null) continue;
                distance = Mathf.Min(distance, Mathf.Max(.5f, hit.distance - .18f));
            }
            _camera.transform.position = focus + offset.normalized * distance;
            _camera.transform.LookAt(focus);
            _picture.color = new Color(1, 1, 1, Mathf.Clamp01((_duration - elapsed) / .18f));
            RenderOnlyCopies();
        }
        private void RenderOnlyCopies()
        {
            _hidden.Clear(); _previous.Clear(); _seen.Clear();
            // A held shoe may also be under its actor. Hide each renderer once
            // so restoration cannot accidentally retain the temporary hidden flag.
            foreach (var actor in GameServices.Round.Players)
            {
                if (actor == null) continue;
                HideTree(actor.transform);
                var held = actor.GetComponent<Carrier>()?.Held;
                if (held != null) HideTree(held.transform);
            }
            HideTree(_rig.transform);
            _actorCopy.ShowOnlyForCapture(true); _victimCopy.ShowOnlyForCapture(true);
            if (_copiedItems != null) foreach (var r in _copiedItems) if (r != null) r.forceRenderingOff = false;
            try { _camera.Render(); }
            finally
            {
                _actorCopy.ShowOnlyForCapture(false); _victimCopy.ShowOnlyForCapture(false);
                if (_copiedItems != null) foreach (var r in _copiedItems) if (r != null) r.forceRenderingOff = true;
                for (int i = 0; i < _hidden.Count; i++) if (_hidden[i] != null) _hidden[i].forceRenderingOff = _previous[i];
            }
        }
        private void HideTree(Transform root)
        {
            _scratch.Clear(); root.GetComponentsInChildren<Renderer>(true, _scratch);
            foreach (var renderer in _scratch)
            {
                if (!_seen.Add(renderer)) continue;
                _hidden.Add(renderer); _previous.Add(renderer.forceRenderingOff); renderer.forceRenderingOff = true;
            }
        }
        public void End()
        {
            if (_camera != null) _camera.targetTexture = null;
            if (_canvas != null) Destroy(_canvas.gameObject);
            if (_target != null) { _target.Release(); Destroy(_target); }
            if (_stage != null) Destroy(_stage);
            _stage = null; _canvas = null; _target = null; _camera = null; _picture = null;
            _actorCopy = _victimCopy = null; _actorTrack = _victimTrack = null; _victim = null;
            _copiedItems = null; _hidden.Clear(); _previous.Clear(); _seen.Clear(); _scratch.Clear();
        }
    }
}
