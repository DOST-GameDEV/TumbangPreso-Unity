using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>Only the local victim's live gameplay view inherits Haunted perception.</summary>
    public static class HauntedPerception
    {
        public static bool Applies(Camera camera)
        {
            if (camera == null || !camera.enabled || camera != Camera.main ||
                GameServices.Round?.RoundActive != true || GameLaunch.Spectator ||
                GameServices.Audio?.IsInReplayMix == true) return false;
            var rig = camera.GetComponent<CameraRig>();
            var body = rig != null ? rig.Following : null;
            return body != null && body.gameObject.activeInHierarchy && rig.IsFollowing(body) &&
                body.PlayerSlot == NetAuthority.LocalSlot && body.Mode == GameMode.HeroStrike && body.IsHaunted;
        }
    }
}
