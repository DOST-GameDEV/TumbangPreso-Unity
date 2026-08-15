using UnityEngine;
using UnityEngine.Events;

namespace TumbangPreso
{
    /// <summary>
    /// Converted from `scripts/systems/kill_plane.gd`.
    ///
    /// B-15/B-35: the 40x40 arena floor had no bounds, no walls, no kill plane — walk off
    /// the edge and you fall forever with gravity accumulating, and the former ArenaCamera
    /// (removed A-2, v4.8) would have been dragged down with whoever fell, pinning everyone
    /// else off-screen too.
    ///
    /// Sits well below the playable floor. Whichever character enters it gets reset to its
    /// own spawn position with zero velocity — not despawned, not damaged — matching the
    /// GDD's "stun-only, no permanent elimination" rule and doubling as the landing spot for
    /// Option A's ring-out win condition.
    ///
    /// ⚠️ THE HEIGHT IS BORROWED AND IS NOT YET THE REAL NUMBER. Godot set this plane's
    /// position and size in `Main.tscn`, which is unported, so the true transform is not
    /// available to copy. `Balance.VoidY` (-12) is the SLIPPER's lost-below-arena threshold
    /// — a different constant for a different object — and it is used here only as a
    /// plausible default that is definitely below the floor. **When `Main.tscn` is
    /// converted, take the real transform from it and delete this note.**
    /// Wide enough in X/Z to catch a character that fell off the 40x40 floor with some
    /// horizontal drift still on it.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class KillPlane : MonoBehaviour
    {
        /// <summary>Raised after the character has been put back. The Godot signal carried
        /// the character; this carries the same.</summary>
        public UnityEvent<CharacterMotor> CharacterRespawned = new UnityEvent<CharacterMotor>();

        /// Comfortably wider than the 40x40 floor, so drift off a corner still lands in it.
        public const float PlaneExtent = 120.0f;
        public const float PlaneThickness = 4.0f;

        private void Reset() => SnapToVoidHeight();

        private void Awake()
        {
            var box = GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(PlaneExtent, PlaneThickness, PlaneExtent);
            SnapToVoidHeight();
        }

        private void SnapToVoidHeight()
        {
            var p = transform.position;
            transform.position = new Vector3(p.x, Core.Balance.VoidY, p.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            var character = other.GetComponentInParent<CharacterMotor>();
            if (character == null) return;

            character.Respawn();
            CharacterRespawned.Invoke(character);
        }
    }
}
