using UnityEngine;

namespace TumbangPreso.Visual
{
    public enum PickupStyle { Standing, Moving, RunStop, Crouched, Sliding, QuickSnatch }

    // Presentation timing only. The existing dwell and host grab still own possession.
    public static class PickupMotion
    {
        public static PickupStyle Choose(bool sliding, bool crouched, bool rightClick, float speed, bool recentlyRunning)
            => sliding ? PickupStyle.Sliding : crouched ? PickupStyle.Crouched
             : rightClick ? PickupStyle.QuickSnatch : recentlyRunning ? PickupStyle.RunStop
             : speed > .6f ? PickupStyle.Moving : PickupStyle.Standing;

        public static float Seconds(PickupStyle style, bool fromDwell)
            => style == PickupStyle.Sliding ? .32f : fromDwell ? .36f : style == PickupStyle.QuickSnatch ? .44f : .62f;

        public static float Lift(float progress, bool fromDwell)
            => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(fromDwell ? 0 : .30f, 1, progress));

        public static float Grip(float progress, bool fromDwell)
            => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(fromDwell ? 0 : .30f, fromDwell ? .16f : .46f, progress));

        public static float Bend(float progress, bool fromDwell)
            => fromDwell ? 1 - Lift(progress, true)
             : progress < .30f ? Mathf.SmoothStep(0, 1, progress / .30f) : 1 - Lift(progress, false);

        public static float EyeDrop(PickupStyle style) => style == PickupStyle.Sliding ? .05f
            : style == PickupStyle.Crouched ? .12f : style == PickupStyle.RunStop ? .60f
            : style == PickupStyle.Moving ? .40f : style == PickupStyle.QuickSnatch ? .32f : .52f;

        public static float LookDown(PickupStyle style) => style == PickupStyle.Sliding ? 7
            : style == PickupStyle.Crouched ? 12 : style == PickupStyle.QuickSnatch ? 18 : 28;

        public static Vector3 Shoulder(PickupStyle style) => style == PickupStyle.Sliding ? new Vector3(.64f, -.55f, -.08f)
            : style == PickupStyle.QuickSnatch ? new Vector3(.75f, -.42f, -.12f)
            : new Vector3(.46f, -.86f, -.05f);

        public static Vector3 Brace(PickupStyle style) => style == PickupStyle.RunStop ? new Vector3(-.06f, .40f, .12f)
            : style == PickupStyle.Sliding ? new Vector3(-.15f, .12f, -.15f)
            : style == PickupStyle.Moving ? new Vector3(-.12f, .20f, .04f)
            : style == PickupStyle.QuickSnatch ? new Vector3(.10f, .15f, -.12f)
            : new Vector3(.05f, .12f, -.06f);
    }
}
