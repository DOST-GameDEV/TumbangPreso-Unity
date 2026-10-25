using TumbangPreso.Net;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    public sealed partial class SpectatorCamera
    {
        private bool _observedViewAvailable, _observedFirstPerson, _observedHands, _observedBot;
        private CharacterMotor _observedActor;
        private int _observedEpoch = -1;
        private SpectatorViewState _observedPose;
        private bool _observedMotionChanged, _resetObservedMotion;
        private ObservedPrivateView _privateView;
        public int PrivatePhantomCount => _privateView?.Count ?? 0;
        public bool PrivateMarkerVisible => _privateView?.MarkerVisible ?? false;
        private string ObservedPovLabel => !_observedViewAvailable ? "WAITING FOR PLAYER VIEW"
            : _observedBot ? (_follow != null && _follow.IsBot ? "BOT VIEW" : "REFERENCE VIEW")
            : _observedFirstPerson ? "FIRST PERSON" : "PLAYER VIEW";
        public CharacterMotor PlayerViewTarget => isActiveAndEnabled && _pov && _follow != null && _observedActor == _follow
            && _observedEpoch == _follow.MovementEpoch && _observedViewAvailable && !SpectatorCutscene.Hides && !_replaying ? _follow : null;
        public bool IsWatchingEyes(CharacterMotor actor) => actor != null && PlayerViewTarget == actor && _observedFirstPerson;
        public CharacterMotor EyesTarget => IsWatchingEyes(_follow) ? _follow : null;
        public CharacterMotor FollowedPlayer => _follow;
        public bool TryWatchPlayer(int slot)
        {
            if (!isActiveAndEnabled || slot < 0 || slot >= Core.Balance.PlayerCount || UI.Panel.AnyOpen
                || PresentationClock.Held || SpectatorCutscene.Hides || UI.ScreenTakeover.AnyOpenOutside(transform)) return false;
            SelectPlayerPov(slot);
            return _follow != null && _follow.PlayerSlot == slot && _pov;
        }

        private void LateUpdate()
        {
            if (!_pov || _follow == null) { _privateView?.Clear(); return; }
            if (_observedActor != _follow || _observedEpoch != _follow.MovementEpoch)
            { _observedViewAvailable = false; _privateView?.Clear(); StepPovArms(0); }
            ObservedPlayerView.SetTarget(_follow);
            if (SpectatorCutscene.Hides || PresentationClock.Held || _replaying)
            {
                _observedViewAvailable = false; _privateView?.Clear(); StepPovArms(0); return;
            }
            if (!_follow.gameObject.activeInHierarchy)
            {
                _follow = null; _followIndex = -1; _pov = false; ResetObservedView(); StepPovArms(0); return;
            }
            if (UI.Panel.AnyOpen || UI.ScreenTakeover.AnyOpenOutside(transform))
            {
                // A translucent menu can hold the camera, but cannot keep stale private
                // geometry alive after its source stream or body scope is gone.
                if (!ObservedPlayerView.TryGet(_follow, out _, out _))
                { _observedViewAvailable = false; _privateView?.Clear(); StepPovArms(0); }
                return;
            }
            float dt = Time.unscaledDeltaTime;
            if (ObservedPlayerView.TryGet(_follow, out var view, out bool local))
            {
                bool snap = local || _observedActor != _follow || _observedEpoch != view.Epoch
                    || (view.Eye - transform.position).sqrMagnitude > 64;
                float blend = snap ? 1 : 1 - Mathf.Exp(-28f * dt);
                transform.SetPositionAndRotation(Vector3.Lerp(transform.position, view.Eye, blend),
                    Quaternion.Slerp(transform.rotation, view.Rotation.normalized, blend));
                _targetPosition = transform.position; SyncAnglesFromTransform();
                if (_camera != null) _camera.fieldOfView = snap ? view.Fov : Mathf.Lerp(_camera.fieldOfView, view.Fov, blend);
                Shader.SetGlobalFloat(Visual.NearFade.EyeDropId, view.EyeDrop);
                _resetObservedMotion |= _observedActor != _follow || _observedEpoch != view.Epoch;
                _observedMotionChanged = local || _resetObservedMotion || _observedPose.Sequence != view.Sequence;
                _observedPose = view;
                _observedActor = _follow; _observedEpoch = view.Epoch;
                _observedViewAvailable = true;
                _observedFirstPerson = (view.Flags & SpectatorViewState.FirstPerson) != 0;
                _observedHands = (view.Flags & SpectatorViewState.HandsVisible) != 0;
                _observedBot = (view.Flags & SpectatorViewState.BotView) != 0;
                StepPovArms(Time.deltaTime);
                if (local) _privateView?.Clear();
                else if (view.Private.Phantoms.Length > 0 || view.Private.Marker.Present || _privateView != null)
                { _privateView ??= new ObservedPrivateView(transform); _privateView.Apply(view.Private); }
            }
            else
            {
                // A missing/legacy stream is a labeled follow view, not a fabricated
                // "actual" eye pose driven by the operator's mouse pitch.
                _observedViewAvailable = false; _privateView?.Clear(); StepPovArms(0);
                if (_camera != null) _camera.fieldOfView = SpectatorFov;
                Shader.SetGlobalFloat(Visual.NearFade.EyeDropId, 0);
                Vector3 focus = _follow.transform.position + Vector3.up * PovEyeHeight();
                Vector3 eye = focus - _follow.transform.forward * _followDistance + Vector3.up * 2;
                transform.SetPositionAndRotation(eye, Quaternion.LookRotation(focus - eye, Vector3.up));
                _targetPosition = eye; SyncAnglesFromTransform();
            }
        }
        private void ResetObservedView()
        {
            _observedViewAvailable = _observedFirstPerson = _observedHands = _observedBot = false;
            _observedActor = null; _observedEpoch = -1;
            _observedMotionChanged = _resetObservedMotion = false; _observedPose = default;
            _privateView?.Dispose(); _privateView = null;
            ObservedPlayerView.SetTarget(null);
            if (_camera != null) _camera.fieldOfView = SpectatorFov;
            Shader.SetGlobalFloat(Visual.NearFade.EyeDropId, 0);
        }
    }
}
