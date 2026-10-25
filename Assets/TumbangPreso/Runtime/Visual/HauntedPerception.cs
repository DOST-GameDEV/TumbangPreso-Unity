using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>The victim's own live view, including selected player-view playback.</summary>
    public static class HauntedPerception
    {
        public static bool Applies(Camera camera)
        {
            if (camera == null || !camera.enabled || camera != Camera.main ||
                GameServices.Round?.RoundActive != true ||
                GameServices.Audio?.IsInReplayMix == true) return false;
            var body = ViewPerspective.PlayerActor(camera);
            bool observing = camera.GetComponent<SpectatorCamera>() != null;
            return body != null && body.gameObject.activeInHierarchy &&
                (observing || (!GameLaunch.Spectator && body.PlayerSlot == NetAuthority.LocalSlot))
                && body.Mode == GameMode.HeroStrike && body.IsHaunted;
        }
    }
}
