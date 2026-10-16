using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// CONCUSSED, ON THE VICTIM'S OWN SCREEN (Basilio's Boulder and Continental Drift; `docs/reports/dante-presentation-2026-10-16/
    /// plan.md` section 3, owner approved 2026-10-16). Breach's lesson (research.md): a status is felt, not only listed. The victim
    /// gets three things, all from the replicated Concussed clock so nothing here outlasts or undercuts the rule:
    ///  * a dull dust-ochre rim at the screen edges (`UI.TumpHudEffects`, `Shaders/ConcussedVignette`);
    ///  * a slow double image, a second copy of the frame offset about 2 px, swaying with the same seeded wobble the throw's
    ///    aim takes (`CharacterMotor.AimWobbleDegrees`), so what they see and where their slipper goes agree (`ColourGrade`);
    ///  * a high ringing and the rest of the mix muffled (`AudioDirector`).
    /// All of it settles in over 0.15 s and fades out over the last second. Only the local victim's live gameplay view.
    /// </summary>
    public static class ConcussedPerception
    {
        /// <summary>The ghost image's offset at full sway, in pixels at 1080 lines (scaled with the frame).</summary>
        public const float GhostPixels = 2.2f;
        /// <summary>The ghost copy's weight at full strength: a second image, never a blur that hides the court.</summary>
        public const float GhostWeight = .34f;
        /// <summary>The muffle's low-pass at full strength, Hz (Haunted's is 1400; this one still lets the court through).</summary>
        public const float MuffleHz = 1800f;

        /// <summary>The concussed local body this camera follows, or null.</summary>
        public static CharacterMotor Victim(Camera camera)
        {
            if (camera == null || !camera.enabled || camera != Camera.main ||
                GameServices.Round?.RoundActive != true || GameLaunch.Spectator ||
                GameServices.Audio?.IsInReplayMix == true) return null;
            var rig = camera.GetComponent<CameraRig>();
            var body = rig != null ? rig.Following : null;
            if (body == null || !body.gameObject.activeInHierarchy || !rig.IsFollowing(body) ||
                !body.IsLocalViewer || !body.IsConcussed) return null;
            return body;
        }

        /// <summary>0 when this camera is not a concussed player's own view; up to 1 while it is.</summary>
        public static float Weight(Camera camera)
        {
            var body = Victim(camera);
            return body == null ? 0f : Envelope(body.ConcussedLeft);
        }

        /// <summary>In over 0.15 s, held, out over the last second, from the time left on the clock.</summary>
        public static float Envelope(float left)
        {
            float elapsed = StatusRules.ConcussedSeconds - left;
            return Mathf.Clamp01(elapsed / .15f) * Mathf.Clamp01(left / 1f);
        }

        /// <summary>
        /// The ghost image's offset for this frame, in UV, from the body's own aim wobble (a seeded sine of its clock):
        /// mostly sideways, a little vertical, so it sways rather than shakes. Reduce visual effects halves it.
        /// </summary>
        public static Vector2 GhostOffset(Camera camera, int width, int height)
        {
            var body = Victim(camera);
            if (body == null || width <= 0 || height <= 0) return Vector2.zero;
            float sway = Mathf.Clamp(body.AimWobbleDegrees / StatusRules.ConcussedAimWobbleDegrees, -1f, 1f);
            float t = Time.time + body.PlayerSlot * 1.37f;
            float px = GhostPixels * height / 1080f * (Settings.SettingsStore.Current.ReducedEffects ? .5f : 1f);
            // Never fully centred: a double image that lines up for a frame reads as a flicker.
            float x = (.45f + .55f * Mathf.Abs(sway)) * Mathf.Sign(sway == 0f ? 1f : sway);
            float y = .35f * Mathf.Sin(t * 1.9f);
            return new Vector2(x * px / width, y * px / height);
        }
    }
}
