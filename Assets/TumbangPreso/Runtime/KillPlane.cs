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
    /// ⚠️⚠️ THE REAL TRANSFORM IS THE MAP'S, AND IT WAS BORROWED FROM THE WRONG CONSTANT.
    /// An earlier note here said the true numbers lived in the unported `Main.tscn` and used
    /// `Balance.VoidY` (-12) as a stand-in — but `VoidY` is the SLIPPER's lost-below-arena
    /// threshold, a different constant for a different object, and the plane is not in
    /// `Main.tscn` at all: **both arenas author it themselves**, at y = -10 with a
    /// 260 x 4 x 260 box. Those are the numbers now, read off the map when there is one.
    ///
    /// ⚠️ AND IT WAS NEVER ATTACHED TO ANYTHING. The map converter turns the Godot `Area3D`
    /// into a bare GameObject and its `CollisionShape3D` into a non-trigger BoxCollider, so
    /// the plane existed as geometry with no behaviour: a player who walked off the edge fell
    /// forever with gravity accumulating and the round simply lost them. `TscnImporter` binds
    /// this component now.
    /// </summary>
    public sealed class KillPlane : MonoBehaviour
    {
        /// <summary>Raised after the character has been put back. The Godot signal carried
        /// the character; this carries the same.</summary>
        public UnityEvent<CharacterMotor> CharacterRespawned = new UnityEvent<CharacterMotor>();

        /// <summary>The arena's own plane: `Shape_killplane` is Vector3(260, 4, 260) in both
        /// maps, and the Area3D sits at y = -10.</summary>
        public const float PlaneExtent = 260.0f;
        public const float PlaneThickness = 4.0f;
        public const float PlaneHeight = -10.0f;

        private void Awake()
        {
            // ⚠️ THE TRIGGER MUST BE ON THIS OBJECT. Unity delivers OnTriggerEnter to the
            // GameObject carrying the collider (and to a parent with a Rigidbody, which this has
            // none of), so a collider left on the converted child would fire nothing here.
            var box = GetComponent<BoxCollider>();
            if (box == null) box = gameObject.AddComponent<BoxCollider>();

            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = SizeFromChildren();

            // Any collider the conversion left on a child is now a duplicate, and a solid one
            // at that: left enabled it is a 260 m platform a falling player lands on.
            foreach (var child in GetComponentsInChildren<Collider>(true))
            {
                if (child == box) continue;
                child.enabled = false;
            }

            var p = transform.position;
            transform.position = new Vector3(p.x, PlaneHeight, p.z);
        }

        /// <summary>The converted `CollisionShape3D`'s box if there is one, so a map that
        /// re-authors the plane is obeyed rather than overridden.</summary>
        private Vector3 SizeFromChildren()
        {
            foreach (var child in GetComponentsInChildren<BoxCollider>(true))
            {
                if (child.gameObject == gameObject) continue;
                if (child.size.sqrMagnitude < 0.01f) continue;

                return child.size;
            }

            return new Vector3(PlaneExtent, PlaneThickness, PlaneExtent);
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
