using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    // Shared deterministic world-flight prediction; actor/can interception remains live.
    internal static class SlipperLandingPrediction
    {
        internal static bool TryPredictLanding(Vector3 origin, Vector3 velocity, float spin, RaycastHit[] buffer, out Vector3 landing, List<Vector3> path = null,
            float skimDistance = 0, bool skimming = false, float restHeight = Balance.SlipperRestHeight, SlipperAffinity bankAffinity = SlipperAffinity.Normal, int initialBanks = 0)
        {
            landing = default;
            Vector3 point = origin;
            float dt = Time.fixedDeltaTime;
            if (dt <= 0 || !float.IsFinite(dt) || !float.IsFinite(spin) || !float.IsFinite(origin.sqrMagnitude) || !float.IsFinite(velocity.sqrMagnitude)) return false;
            path?.Clear(); path?.Add(point);
            if (skimming)
                return CompleteSkim(point, velocity, skimDistance, restHeight, dt, out landing, path);
            int banks = Mathf.Max(0, initialBanks);
            int steps = Mathf.Min(1200, Mathf.CeilToInt(Balance.MaxAirborneTime / dt));
            for (int i = 0; i < steps; i++)
            {
                velocity = Slipper.StepFlightVelocity(velocity, spin, dt);
                Vector3 next = point + velocity * dt;
                Vector3 disp = next - point;
                float distance = disp.magnitude;
                float restitution = Slipper.BankRestitution(spin, banks, bankAffinity);
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
                        bankAffinity = Slipper.ConsumePoweredBank(bankAffinity);
                    }
                }
                restitution = Slipper.BankRestitution(spin, banks, bankAffinity);
                bool powered = Slipper.IsPoweredBank(bankAffinity);
                bool bounded = BounceAxis(ref next.x, ref velocity.x, AIController.PlayableMinX, AIController.PlayableMaxX, powered ? 1 : restitution);
                bounded |= BounceAxis(ref next.z, ref velocity.z, AIController.PlayableMinZ, AIController.PlayableMaxZ, powered ? 1 : restitution);
                bool sideBank = bounded;
                float ceiling = AIController.PlayableCeilingY - Balance.SlipperHitRadius;
                if (next.y > ceiling) { next.y = ceiling; velocity.y = -Mathf.Abs(velocity.y) * (powered ? Balance.BounceRestitution : restitution); bounded = true; }
                if (sideBank)
                {
                    if (powered) velocity *= .85f;
                    bankAffinity = Slipper.ConsumePoweredBank(bankAffinity);
                }
                if (bounded) banks++;
                var support = next; support.y = Mathf.Max(point.y, next.y);
                float ground = Slipper.FindGroundY(support, Balance.SlipperRestHeight);
                path?.Add(next);
                if (next.y <= ground + Balance.SlipperRestHeight)
                {
                    if (skimDistance > 0 && new Vector2(velocity.x, velocity.z).sqrMagnitude >= .001f)
                        return CompleteSkim(new Vector3(next.x, ground + restHeight, next.z), velocity,
                            skimDistance, restHeight, dt, out landing, path);
                    landing = new Vector3(next.x, ground + TrajectoryPreview.FloorEpsilon, next.z); return true;
                }
                if (next.y < Balance.VoidY) return false;
                point = next;
            }
            return false;
        }

        private static bool CompleteSkim(Vector3 point, Vector3 velocity, float left, float restHeight,
            float dt, out Vector3 landing, List<Vector3> path)
        {
            var direction = new Vector3(velocity.x, 0, velocity.z).normalized;
            float ground = Slipper.FindGroundY(point, Balance.SlipperRestHeight);
            // The authored continuation is bounded; zero direction cannot start it.
            int steps = Mathf.Min(1200, Mathf.CeilToInt(RafiRules.SkimDistance / (RafiRules.SkimSpeed * dt)) + 1);
            for (int i = 0; i < steps && left > 0 && direction.sqrMagnitude > 0; i++)
            {
                float distance = Mathf.Min(left, RafiRules.SkimSpeed * dt);
                if (!Slipper.TrySkimStep(point, direction, distance, ground,
                    out var next, out float support, out float allowed)) break;
                ground = support;
                point = new Vector3(next.x, support + restHeight, next.z);
                path?.Add(point);
                left = allowed < distance - .001f ? 0 : Mathf.Max(0, left - allowed);
            }
            landing = new Vector3(point.x, ground + TrajectoryPreview.FloorEpsilon, point.z);
            return true;
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
