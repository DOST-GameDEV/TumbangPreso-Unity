namespace TumbangPreso.CameraSystem
{
    public sealed partial class CameraRig
    {
        public bool ShowsOwnEyes => _active && _mode == CameraMode.Fpp && !_emoteView && !_fallView && !_blownView && !_wasPossessing;
        private void CaptureObservedView() => ObservedPlayerView.Capture(this, _character);
        internal bool TryObservedView(out Net.SpectatorViewState state)
        {
            state = default;
            if (_character == null || !_active || _camera == null || !_camera.enabled || PresentationClock.Held) return false;
            bool first = ShowsOwnEyes;
            bool hands = _viewmodel != null && _viewmodel.gameObject.activeInHierarchy;
            state = new Net.SpectatorViewState {
                Match = ObservedPlayerView.MatchIdentity, Round = GameServices.Match?.RoundNumber ?? 0,
                Slot = _character.PlayerSlot, Epoch = _character.MovementEpoch, Sequence = (uint)UnityEngine.Mathf.Max(1, UnityEngine.Time.frameCount),
                Eye = _camera.transform.position, Rotation = _camera.transform.rotation, Fov = _camera.fieldOfView,
                EyeDrop = UnityEngine.Mathf.Clamp(UnityEngine.Shader.GetGlobalFloat(Visual.NearFade.EyeDropId), 0, 10),
                Flags = (byte)((first ? Net.SpectatorViewState.FirstPerson : 0) | (hands ? Net.SpectatorViewState.HandsVisible : 0)),
                HandsSeat = hands ? _viewmodel.localPosition : ViewmodelSeat,
                HandsRotation = hands ? _viewmodel.localRotation : UnityEngine.Quaternion.identity,
                Action = new Unity.Collections.FixedString64Bytes(hands && _arms != null ? _arms.ObservedActionName : ""),
                Aim = new Unity.Collections.FixedString64Bytes(hands && _arms != null ? _arms.ObservedAimName : ""),
                ActionTime = hands && _arms != null ? _arms.ObservedActionTime : 0,
                VisualPhase = hands && _arms != null ? _arms.ObservedVisualPhase : 0,
                ActionSequence = hands && _arms != null ? _arms.ObservedActionSequence : 0
            };
            var main = UnityEngine.Camera.main;
            if (main == _camera)
            {
                Visual.HexedPhantomSlippers.CaptureObserved(_character, ref state.Private);
                Visual.DisorientedHallucinations.CaptureObserved(_character, ref state.Private);
                state.Private.Marker = _character.AbilitySystem != null ? _character.AbilitySystem.CaptureObservedMarker() : default;
            }
            return state.PoseIsValid;
        }
    }
}
