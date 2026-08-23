using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Where the dangerous ground is, right now, in a form something can steer around.
    ///
    /// ⚠️⚠️ THIS EXISTS BECAUSE THE BOTS WERE WALKING STRAIGHT THROUGH HERO HAZARDS AND IT COST
    /// REAL POINTS. `BotBehaviourProbe` measured unretrieved-slipper penalties in Hero Strike
    /// swinging between 0 and 28 across otherwise identical runs while Classic sat at a flat 0.
    /// The retrieval logic was never the problem: an attacker would decide correctly to go and
    /// fetch its tsinelas, `Drive` would point it at the slipper in a straight line, and the
    /// line went through a Permafrost Sheet or a Seance Void. It got slowed, slipped, or pulled
    /// off course, arrived late or not at all, and the round charged it 5 points a second for a
    /// slipper it was actively trying to collect. The variance was just how often a hazard
    /// happened to land between a bot and its slipper, which is why it looked random.
    ///
    /// ⚠️ IT IS A LIST OF DISCS, NOT A NAVMESH. The arena is a flat 14 by 14 box and every
    /// hazard in the game is a circle on the floor, so the whole problem is "is there a circle
    /// between me and where I am going, and which way do I go round it". A navmesh would need
    /// baking, carving and rebaking every time an ability fires, for the same answer.
    ///
    /// ⚠️ REGISTRATION IS THE HAZARD'S OWN JOB, in OnEnable/OnDisable, so an entry cannot
    /// outlive the object. A stale disc is worse than no disc at all: bots would spend the rest
    /// of the round walking around a patch of empty road.
    /// </summary>
    public static class HazardMap
    {
        public readonly struct Disc
        {
            public readonly Vector3 Centre;
            public readonly float Radius;
            public readonly int OwnerSlot;

            public Disc(Vector3 centre, float radius, int ownerSlot)
            {
                Centre = centre;
                Radius = radius;
                OwnerSlot = ownerSlot;
            }
        }

        private static readonly List<HazardVolume> Live = new List<HazardVolume>();

        public static int Count => Live.Count;

        public static void Register(HazardVolume volume)
        {
            if (volume != null && !Live.Contains(volume)) Live.Add(volume);
        }

        public static void Unregister(HazardVolume volume)
        {
            if (volume != null) Live.Remove(volume);
        }

        /// <summary>
        /// ⚠️ CALLED FROM `ResetWorld`. Hazards are destroyed with the round, but a destroyed
        /// object that never ran OnDisable (a scene teardown, a probe tearing the arena down
        /// between rounds) would leave a null in the list forever.
        /// </summary>
        public static void Clear() => Live.Clear();

        /// <summary>
        /// The disc most in the way of a walk from <paramref name="from"/> to
        /// <paramref name="to"/>, or none.
        ///
        /// ⚠️⚠️ "MOST IN THE WAY" IS THE NEAREST BLOCKER ALONG THE PATH, NOT THE NEAREST DISC.
        /// A hazard beside you that you have already walked past is not a reason to turn, and
        /// steering off the closest one by distance does exactly that: the bot sidesteps
        /// something behind it and walks into the one in front. Only discs whose centre
        /// projects ONTO the segment, within the avoid radius of it, count.
        ///
        /// ⚠️ A HAZARD YOU OWN IS NOT AVOIDED. Every kit's own trail sits under its own feet by
        /// design (Rocket Burn Dash, Static Rail Grind), so treating it as a blocker would make
        /// a hero refuse to walk where it had just been.
        ///
        /// ⚠️⚠️ NEITHER IS A HAZARD WIDER THAN `maxRadius`. There is no way round a disc that
        /// covers half the arena, and a body that tries walks the perimeter until the round
        /// ends. See `AiTuning.HazardAvoidMaxRadius` for the measurement that produced the cap
        /// and for why it is expected to stop mattering.
        /// </summary>
        public static bool TryFindBlocker(Vector3 from, Vector3 to, int mySlot, float bodyRadius,
                                          float maxRadius, out Disc blocker)
        {
            blocker = default;

            Vector3 path = to - from;
            path.y = 0.0f;

            float length = path.magnitude;
            if (length < 0.05f) return false;

            Vector3 dir = path / length;
            float bestAlong = float.MaxValue;
            bool found = false;

            for (int i = 0; i < Live.Count; i++)
            {
                var v = Live[i];
                if (v == null) continue;
                if (v.OwnerSlot >= 0 && v.OwnerSlot == mySlot) continue;
                if (v.Radius > maxRadius) continue;

                Vector3 centre = v.transform.position;
                Vector3 offset = centre - from;
                offset.y = 0.0f;

                float along = Vector3.Dot(offset, dir);

                // Behind me, or past where I am going. Neither is in the way.
                if (along < 0.0f || along > length) continue;

                float lateral = (offset - dir * along).magnitude;
                float clearance = v.Radius + bodyRadius;
                if (lateral > clearance) continue;

                if (along < bestAlong)
                {
                    bestAlong = along;
                    blocker = new Disc(centre, v.Radius, v.OwnerSlot);
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// A heading that goes AROUND <paramref name="blocker"/> instead of into it.
        ///
        /// ⚠️⚠️ IT PICKS A SIDE AND COMMITS, and the side is whichever the body is already
        /// leaning toward. Recomputing "shortest way round" every frame is what makes a steering
        /// bot oscillate at the exact centre line: two sides tie, floating point decides, and it
        /// jitters straight into the thing. Signing off the existing lateral offset breaks the
        /// tie in favour of the way it is already going.
        ///
        /// ⚠️ IT AIMS AT THE TANGENT, NOT PERPENDICULAR. Steering ninety degrees off makes the
        /// bot walk a square around a circle and lose far more time than the hazard would have
        /// cost. The tangent point is the shortest walk that still clears the edge.
        /// </summary>
        public static Vector3 SteerAround(Vector3 from, Vector3 to, Disc blocker, float bodyRadius)
        {
            Vector3 path = to - from;
            path.y = 0.0f;

            float length = path.magnitude;
            if (length < 0.05f) return path;

            Vector3 dir = path / length;

            Vector3 offset = blocker.Centre - from;
            offset.y = 0.0f;

            float along = Vector3.Dot(offset, dir);
            Vector3 lateral = offset - dir * along;

            // Perpendicular to the path, pointing away from the blocker's centre.
            Vector3 side = lateral.sqrMagnitude > 0.0001f
                ? -lateral.normalized
                : new Vector3(-dir.z, 0.0f, dir.x);

            float clearance = blocker.Radius + bodyRadius;

            // The point beside the hazard the body has to reach before it can turn back in.
            Vector3 gate = blocker.Centre + side * clearance;

            Vector3 steer = gate - from;
            steer.y = 0.0f;

            return steer.sqrMagnitude > 0.0001f ? steer.normalized : dir;
        }
    }

    /// <summary>
    /// Put this on anything the bots should walk around. One component, one disc.
    ///
    /// ⚠️ THE RADIUS IS THE HAZARD'S GAMEPLAY RADIUS, NOT ITS VISUAL ONE. The extra margin a
    /// body needs is added by the caller at query time, because it belongs to the body.
    /// </summary>
    public sealed class HazardVolume : MonoBehaviour
    {
        public float Radius = 2.0f;

        /// <summary>The slot that cast it. -1 for a hazard nobody owns.</summary>
        public int OwnerSlot = -1;

        public static HazardVolume Attach(GameObject go, float radius, int ownerSlot)
        {
            if (go == null) return null;

            var v = go.GetComponent<HazardVolume>();
            if (v == null) v = go.AddComponent<HazardVolume>();

            v.Radius = radius;
            v.OwnerSlot = ownerSlot;

            // ⚠⚠ REGISTERED HERE AS WELL AS IN OnEnable, AND THAT IS NOT BELT AND BRACES.
            // OUTSIDE PLAY MODE UNITY NEVER CALLS OnEnable on a plain MonoBehaviour, so an
            // EditMode test that attaches a volume gets an object that exists and a map that is
            // empty. `Register` refuses a duplicate, so the two paths cannot double up.
            HazardMap.Register(v);
            return v;
        }

        private void OnEnable() => HazardMap.Register(this);
        private void OnDisable() => HazardMap.Unregister(this);
    }
}
