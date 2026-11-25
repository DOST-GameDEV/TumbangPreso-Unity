using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso
{
    /// <summary>
    /// The cast between the final whistle and the result board (`MatchEndBeat`).
    ///
    /// ⚠️ SETTLING IS ORDINARY MOVEMENT FIRST. `FreezeForMatchEnd` takes away everything that holds a body up or walks it,
    /// and the body then comes down through the same collision-aware step as play. Only a body that has still not come to
    /// rest when the reveal's transition covers the court is moved, by the host, onto a verified map-local spot through
    /// the ordinary teleport correction (relayed to its owner and every replica, `MatchRpc.BroadcastTeleport`).
    /// </summary>
    public sealed partial class CharacterMotor
    {
        private static readonly RaycastHit[] MatchEndFloorHits = new RaycastHit[16];
        private static readonly Collider[] MatchEndOverlap = new Collider[24];

        /// <summary>How far under a recovery landing, or under a body about to touch down, the settle accepts a floor.</summary>
        public const float MatchEndNearFloor = 1.5f;

        /// <summary>
        /// Standing on something real: grounded, and not swimming, flying, hanging off an edge, hauled, carried or
        /// swinging. On the host a client's body reads the owner's own grounded flag, off the wire (`StepNetworkReplica`).
        /// </summary>
        public bool RestsForMatchEnd
            => _grounded && !IsSwimming && !IsFlying && !IsEdgeRecovering && !IsHauled && !IsCarried && !IsSwinging;

        /// <summary>
        /// The host's half of the settle: every body that has not come to rest is put on its verified spot. Returns how
        /// many were moved. Offline is a host with no peers.
        /// </summary>
        public static int CorrectUnrestedForMatchEnd(RoundDirector round)
        {
            if (round == null || !NetAuthority.ShouldResolve()) return 0;

            int moved = 0;
            foreach (var body in round.Players)
            {
                if (body == null || !body.isActiveAndEnabled || body.RestsForMatchEnd) continue;

                string state = $"grounded={body._grounded} swimming={body.IsSwimming} flying={body.IsFlying} " +
                               $"edge={body.IsEdgeRecovering} hauled={body.IsHauled} carried={body.IsCarried} at={body.transform.position}";
                if (!body.MatchEndRestSpot(out var feet))
                {
                    Debug.LogWarning($"[MatchEnd] seat {body.PlayerSlot} has no verified rest spot ({state}).");
                    continue;
                }

                Debug.Log($"[MatchEnd] seat {body.PlayerSlot} not at rest ({state}); placed at {feet}.");
                body.PlaceForMatchEnd(feet);
                moved++;
            }
            return moved;
        }

        /// <summary>
        /// Where this body stands for the board when it has not come to rest by itself. The landing its own recovery was
        /// already carrying it to comes first (a ledge, a deck, a drone's set-down, a haul's landing), then the floor just
        /// under a body about to touch down, then its own spawn mark for this round and the round's other marks. Each
        /// candidate is seated on the actual terrain under it and must fit the capsule, out of the water.
        /// </summary>
        internal bool MatchEndRestSpot(out Vector3 feet)
        {
            feet = default;
            if (IsEdgeRecovering && TrySeatForMatchEnd(_edgeLanding, 0.5f, MatchEndNearFloor, out feet)) return true;
            if (IsHauled && TrySeatForMatchEnd(_haulTo, 0.5f, MatchEndNearFloor, out feet)) return true;
            if (IsSwimming && MapEdgeGeometry.TryLagoon(this, out var anchor)
                && TrySeatForMatchEnd(anchor.Landing, 0.5f, MatchEndNearFloor, out feet)) return true;
            if (!IsSwimming && !IsEdgeRecovering && !IsHauled
                && TrySeatForMatchEnd(transform.position, 0.3f, MatchEndNearFloor, out feet)) return true;

            if (TrySeatForMatchEnd(SpawnPosition, MatchHost.SpawnFloorProbeHeight, MatchHost.SpawnFloorProbeDepth, out feet))
                return true;

            var match = GameServices.Match;
            int defender = match != null ? match.DefenderSlot : 0;
            for (int slot = 0; slot < Balance.PlayerCount; slot++)
                if (TrySeatForMatchEnd(SliceRunner.SpawnPointFor(slot, defender), MatchHost.SpawnFloorProbeHeight,
                                       MatchHost.SpawnFloorProbeDepth, out feet)) return true;
            return false;
        }

        /// <summary>
        /// Seat <paramref name="candidate"/> on the nearest floor under it (bodies, tsinelas and the lata are not floors,
        /// the probe `MatchHost.SeatOnFloor` uses), inside the court, out of the water, with the capsule clear.
        /// </summary>
        private bool TrySeatForMatchEnd(Vector3 candidate, float above, float below, out Vector3 feet)
        {
            feet = default;
            if (_cc == null || !Finite(candidate)) return false;

            var inside = AIController.ClampToPlayable(candidate);
            if (new Vector2(inside.x - candidate.x, inside.z - candidate.z).sqrMagnitude > 0.0001f) return false;

            Vector3 from = candidate + Vector3.up * above;
            int count = Physics.RaycastNonAlloc(from, Vector3.down, MatchEndFloorHits, above + below, ~0,
                                                QueryTriggerInteraction.Ignore);
            bool found = false;
            RaycastHit floor = default;
            for (int i = 0; i < count; i++)
            {
                var met = MatchEndFloorHits[i].collider;
                if (met == null || met.transform.IsChildOf(transform)) continue;
                if (met.GetComponentInParent<CharacterMotor>() != null || met.GetComponentInParent<Slipper>() != null
                    || met.GetComponentInParent<Lata>() != null) continue;
                if (found && MatchEndFloorHits[i].distance >= floor.distance) continue;
                floor = MatchEndFloorHits[i];
                found = true;
            }
            if (!found || floor.normal.y < 0.6f) return false;

            float y = floor.point.y + MatchHost.SpawnFloorClearance + Mathf.Max(0.0f, _cc.center.y - _cc.height * 0.5f);
            var seated = new Vector3(candidate.x, y, candidate.z);
            if (RooftopPool.TrySurface(seated, out float surface) && seated.y < surface - 0.18f) return false;
            if (!CapsuleClearForMatchEnd(seated)) return false;

            feet = seated;
            return true;
        }

        /// <summary>The capsule at <paramref name="feet"/> meets nothing but the floor it stands on (and other bodies).</summary>
        private bool CapsuleClearForMatchEnd(Vector3 feet)
        {
            float radius = Mathf.Max(0.1f, _cc.radius - 0.025f);
            float half = Mathf.Max(0.0f, _cc.height * 0.5f - radius);
            Vector3 centre = feet + _cc.center;
            int count = Physics.OverlapCapsuleNonAlloc(centre - Vector3.up * half, centre + Vector3.up * half, radius,
                                                       MatchEndOverlap, ~0, QueryTriggerInteraction.Ignore);
            if (count == MatchEndOverlap.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var hit = MatchEndOverlap[i];
                if (hit == null || hit.transform.IsChildOf(transform)) continue;
                if (hit.GetComponentInParent<CharacterMotor>() != null || hit.GetComponentInParent<Slipper>() != null) continue;
                // Supported feet may touch the floor they stand on within the controller's skin.
                if (hit.bounds.max.y <= feet.y + 0.04f) continue;
                return false;
            }
            return true;
        }

        /// <summary>
        /// The host's correction onto a verified spot. `Teleport` is the ordinary path: it clears what the body was
        /// carrying, settles it for a few frames and relays to the owner and every replica. It stays parked.
        /// </summary>
        internal void PlaceForMatchEnd(Vector3 feet)
        {
            Teleport(feet);
            FreezeForMatchEnd();
        }
    }
}
