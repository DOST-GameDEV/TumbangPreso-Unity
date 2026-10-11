using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// POSSESSED drains the possessed player's own world to grey (Nemu's PRUSISYON, `docs/reports/nemu-ultimate-2026-10-11/
    /// direction.md` section 5): the same hush the cutscene opens with, held on their screen while the ghost rides them.
    /// Only the local victim's live gameplay view, like Haunted's. The weight follows the status clock (it settles in and
    /// lets go with the rider), so nothing is stored.
    /// </summary>
    public static class PossessedPerception
    {
        public const float Saturation = .35f, Brightness = .86f;

        /// <summary>0 when this camera's view is not a possessed player's own; up to 1 while it is.</summary>
        public static float Weight(Camera camera)
        {
            if (camera == null || !camera.enabled || camera != Camera.main ||
                GameServices.Round?.RoundActive != true || GameLaunch.Spectator ||
                GameServices.Audio?.IsInReplayMix == true) return 0f;
            var rig = camera.GetComponent<CameraRig>();
            var body = rig != null ? rig.Following : null;
            if (body == null || !body.gameObject.activeInHierarchy || !rig.IsFollowing(body) ||
                body.PlayerSlot != NetAuthority.LocalSlot || !body.IsPossessed) return 0f;
            float elapsed = body.PossessionElapsed;
            return Mathf.Clamp01(elapsed / NemuRules.SettleSeconds) *
                   Mathf.Clamp01((NemuRules.PossessedSeconds - elapsed) / NemuRules.LeaveSeconds);
        }
    }
}
