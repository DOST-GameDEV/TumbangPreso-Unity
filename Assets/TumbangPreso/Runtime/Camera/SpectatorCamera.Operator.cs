using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace TumbangPreso.CameraSystem
{
    public sealed partial class SpectatorCamera
    {
        private InputAction _speedCycle, _zoomCycle;
        public bool HasCameraMark => _hasBookmark;
        public bool WatchingFirstPerson => _pov && _follow != null;
        public string ViewLabel => ClockLabel + CameraLabel;
        private string ClockLabel => PresentationClock.RequestedScale <= 0f ? "PAUSED  /  "
            : PresentationClock.RequestedScale < .99f ? $"{PresentationClock.RequestedScale:0.##}x  /  " : "";
        private string CameraLabel => _replaying ? "INSTANT REPLAY"
            : AutopilotEngaged ? "AUTO CAMERA  /  " + _director.ShotName()
            : _follow != null ? (_pov ? ObservedPovLabel + "  /  " : "THIRD PERSON  /  ") + FollowName()
            : "FREE CAMERA";

        // Mouse, pad and touch share the same permission and state transitions.
        private bool ExecuteOperatorCommand(ViewCommand command)
        {
            switch (command)
            {
                case ViewCommand.Pause:
                    if (NetAuthority.IsNetworked)
                    {
                        if (!GameLaunch.Spectator) return false;
                        _broadcastPaused = PresentationClock.RequestedScale > 0f;
                        Net.MatchRpc.Instance?.RequestTimeScaleServerRpc(_broadcastPaused ? 0f : _selectedTimeScale);
                    }
                    else ToggleBroadcastPause();
                    return true;
                case ViewCommand.Replay:
                    if (_replaying) EndReplay(); else StartReplay(RecentHighlightReason());
                    return true;
                case ViewCommand.Save:
                    _bookmarkPosition = transform.position; _bookmarkRotation = transform.rotation;
                    _bookmarkFov = _camera != null ? _camera.fieldOfView : SpectatorFov;
                    _hasBookmark = true;
                    UI.Hud.Instance?.ShowToast("CAMERA POSITION SAVED", 1f);
                    return true;
                case ViewCommand.Recall:
                    if (!_hasBookmark) return false;
                    EnsureDirector(); _director.Engaged = false;
                    _follow = null; _followIndex = -1; _pov = false;
                    transform.SetPositionAndRotation(_bookmarkPosition, _bookmarkRotation);
                    _targetPosition = _bookmarkPosition;
                    if (_camera != null) _camera.fieldOfView = _bookmarkFov;
                    SyncAnglesFromTransform();
                    return true;
                case ViewCommand.Speed:
                    if (NetAuthority.IsNetworked && !GameLaunch.Spectator) return false;
                    float scale = _selectedTimeScale < .3f ? .5f : _selectedTimeScale < .75f ? 1f : .25f;
                    if (NetAuthority.IsNetworked) RequestBroadcastScale(scale); else SetBroadcastScale(scale);
                    return true;
                case ViewCommand.Zoom:
                    if (WatchingFirstPerson)
                    {
                        UI.Hud.Instance?.ShowToast("SWITCH TO THIRD PERSON TO ZOOM", 1.2f);
                        return false;
                    }
                    EnsureDirector();
                    if (_director.Engaged)
                    {
                        _director.Engaged = false;
                        _targetPosition = transform.position;
                    }
                    if (_follow != null)
                        _followDistance = _followDistance > 8f ? FollowDistance : _followDistance > 4f ? 3f : 13f;
                    else _speed = _speed < 2f ? BaseSpeed : _speed < 6f ? 9f : _speed < 14f ? 18f : SpeedMin;
                    return true;
                case ViewCommand.Rise:
                case ViewCommand.Lower:
                    EnsureDirector(); _director.Engaged = false;
                    _follow = null; _followIndex = -1; _pov = false;
                    _targetPosition = transform.position + Vector3.up * (command == ViewCommand.Rise ? 1f : -1f);
                    return true;
                default: return false;
            }
        }

        private void StepOperatorKeys()
        {
            var kb = Keyboard.current;
            bool Key(KeyControl key) => key != null && key.wasPressedThisFrame;
            if (_replaying)
            {
                if (Fired(_replayKey) || Key(kb?.escapeKey))
                {
                    if (Key(kb?.escapeKey)) UI.ScreenTakeover.ConsumeEscape();
                    EndReplay();
                }
                return;
            }
            if (!PresentationClock.Held && Fired(_mark)) ExecuteOperatorCommand(ViewCommand.Save);
            if (!PresentationClock.Held && Fired(_recall)) ExecuteOperatorCommand(ViewCommand.Recall);
            if (Key(kb?.f1Key)) SelectPlayerPov(0);
            if (Key(kb?.f2Key)) SelectPlayerPov(1);
            if (Key(kb?.f3Key)) SelectPlayerPov(2);
            if (Key(kb?.f4Key)) SelectPlayerPov(3);
            if (Fired(_pauseKey)) ExecuteOperatorCommand(ViewCommand.Pause);
            if (Fired(_replayKey) || ConsumeProbeReplayRequest()) ExecuteOperatorCommand(ViewCommand.Replay);
            if (Fired(_speedCycle)) ExecuteOperatorCommand(ViewCommand.Speed);
            if (Fired(_zoomCycle)) ExecuteOperatorCommand(ViewCommand.Zoom);
            if (NetAuthority.IsNetworked && !GameLaunch.Spectator) return;
            if (Key(kb?.digit1Key)) SelectOperatorSpeed(.25f);
            if (Key(kb?.digit2Key)) SelectOperatorSpeed(.5f);
            if (Key(kb?.digit3Key)) SelectOperatorSpeed(1f);
        }

        private void SelectOperatorSpeed(float scale)
        {
            if (NetAuthority.IsNetworked) RequestBroadcastScale(scale); else SetBroadcastScale(scale);
        }
    }
}
