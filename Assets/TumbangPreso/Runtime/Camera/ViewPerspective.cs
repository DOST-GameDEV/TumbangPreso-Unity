using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    // Presentation ownership only. Never use this to grant input or authority.
    public static class ViewPerspective
    {
        public static CharacterMotor EyesActor(Camera camera)
        {
            if (camera == null || !camera.enabled || !camera.gameObject.activeInHierarchy) return null;
            var rig = camera.GetComponent<CameraRig>();
            if (rig != null && rig.ShowsOwnEyes) return rig.Following;
            var spectator = camera.GetComponent<SpectatorCamera>();
            return spectator != null ? spectator.EyesTarget : null;
        }
        public static bool IsEyesOf(Camera camera, CharacterMotor actor)
            => actor != null && EyesActor(camera) == actor;
    }
}
