using UnityEngine;

namespace TumbangPreso.Abilities
{
    // For a single spatial effect owned by an ability's preparation/live clocks.
    // Presentation remains ability-owned; neither method sends messages or spends resources.
    public interface IPreparedWorldReplication
    {
        bool CapturePreparedWorld(out Vector3 centre, out float preparation, out float remaining);

        // True only when a newly restored preparation needs its existing body/FPP pose resumed.
        // Zero preparation and remaining is authoritative empty state and must release the effect/root.
        bool RestorePreparedWorld(AbilityContext context, Vector3 centre, float preparation, float remaining);
    }
}
