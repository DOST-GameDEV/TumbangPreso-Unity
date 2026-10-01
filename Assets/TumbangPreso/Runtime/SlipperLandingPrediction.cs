using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    // Shared deterministic world-flight prediction; actor/can interception remains live.
    internal static class SlipperLandingPrediction
    {
        internal static bool TryPredictLanding(Vector3 origin, Vector3 velocity, float spin, RaycastHit[] buffer, out Vector3 landing, List<Vector3> path = null)
        {
            landing = default;
            Vector3 point = origin;
            float dt = Time.fixedDeltaTime;
            if (dt <= 0 || !float.IsFinite(dt) || !float.IsFinite(spin) || !float.IsFinite(origin.sqrMagnitude) || !float.IsFinite(velocity.sqrMagnitude)) return false;
            path?.Clear(); path?.Add(point);
            int banks = 0;
            int steps = Mathf.Min(1200, Mathf.CeilToInt(Balance.MaxAirborneTime / dt));
            for (int i = 0; i < steps; i++)
            {
                velocity = Slipper.StepFlightVelocity(velocity, spin, dt);
                Vector3 next = point + velocity * dt;
                Vector3 disp = next - point;
                float distance = disp.magnitude;
                float restitution = Mathf.Abs(spin) >= Balance.PektusBankSpinThreshold && banks == 0
                    ? Balance.PektusBankRestitution : Balance.BounceRestitution;
                if (distance > .001f)
                {
                    int count = Physics.SphereCastNonAlloc(point, Balance.SlipperHitRadius, disp / distance, buffer, distance, ~0, QueryTriggerInteraction.Ignore);
                    // A dense collision set cannot safely pick a partial nearest wall.
                    var hits = buffer;
                    if (count == hits.Length)
                    { hits = Physics.SphereCastAll(point, Balance.SlipperHitRadius, disp / distance, distance, ~0, QueryTriggerInteraction.Ignore); count = hits.Length; }
                    float nearest = float.PositiveInfinity; RaycastHit wall = default;
                    for (int k = 0; k < count; k++)
                    {
                        var h = hits[k]; var collider = h.collider;
                        if (collider == null || collider.GetComponentInParent<CharacterMotor>() != null
                            || collider.GetComponentInParent<Lata>() != null || collider.GetComponentInParent<Slipper>() != null
                            || collider.name.StartsWith("Floor", System.StringComparison.OrdinalIgnoreCase)
                            || h.distance <= .0001f || Vector3.Dot(h.normal, Vector3.up) > .6f) continue;
                        if (h.distance < nearest) { nearest = h.distance; wall = h; }
                    }
                    if (!float.IsPositiveInfinity(nearest))
                    {
                        Vector3 normal = wall.normal; normal.y = 0;
                        normal = normal.sqrMagnitude > .001f ? normal.normalized : -disp.normalized;
                        velocity = Vector3.Reflect(velocity, normal) * restitution;
                        next = wall.point + normal * (Balance.SlipperHitRadius + .02f); banks++;
                    }
                }
                restitution = Mathf.Abs(spin) >= Balance.PektusBankSpinThreshold && banks == 0
                    ? Balance.PektusBankRestitution : Balance.BounceRestitution;
                bool bounded = BounceAxis(ref next.x, ref velocity.x, AIController.PlayableMinX, AIController.PlayableMaxX, restitution);
                bounded |= BounceAxis(ref next.z, ref velocity.z, AIController.PlayableMinZ, AIController.PlayableMaxZ, restitution);
                float ceiling = AIController.PlayableCeilingY - Balance.SlipperHitRadius;
                if (next.y > ceiling) { next.y = ceiling; velocity.y = -Mathf.Abs(velocity.y) * restitution; bounded = true; }
                if (bounded) banks++;
                var support = next; support.y = Mathf.Max(point.y, next.y);
                float ground = Slipper.FindGroundY(support, Balance.SlipperRestHeight);
                path?.Add(next);
                if (next.y <= ground + Balance.SlipperRestHeight)
                { landing = new Vector3(next.x, ground + TrajectoryPreview.FloorEpsilon, next.z); return true; }
                if (next.y < Balance.VoidY) return false;
                point = next;
            }
            return false;
        }

        private static bool BounceAxis(ref float at, ref float velocity, float min, float max, float restitution)
        {
            float lo = min + Balance.SlipperHitRadius, hi = max - Balance.SlipperHitRadius;
            if (hi <= lo) return false;
            if (at > hi) { at = hi; velocity = -Mathf.Abs(velocity) * restitution; return true; }
            if (at < lo) { at = lo; velocity = Mathf.Abs(velocity) * restitution; return true; }
            return false;
        }

    }
}
