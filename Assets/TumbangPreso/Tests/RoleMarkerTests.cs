using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The taya's floor marker is a RING and an attacker's is a DISC, asserted as geometry.
    ///
    /// ⚠️⚠️ THIS IS THE SECOND CHANNEL `docs/FUTURE.md` § 16.1 ASKS FOR, AND A COLOUR TEST COULD
    /// NOT CHECK IT. That section's whole point is that the roles must be separable **without**
    /// hue: *"a colourblind player, a bad projector at a tournament, or a cheap phone screen all
    /// produce the same failure: you cannot tell the taya from the attackers."* The thing that
    /// makes them separable is the SHAPE, so the shape is what gets asserted.
    ///
    /// ⚠️⚠️ AND IT IS ASSERTED RATHER THAN PHOTOGRAPHED BECAUSE A PHOTOGRAPH OF THIS IS HARDER
    /// THAN IT SOUNDS. `GameplayShots` runs a real round and the camera goes where the round puts
    /// it: the frames it produced on 2026-09-03 caught an attacker's disc cleanly from above and
    /// caught the taya's marker only edge-on, from a first-person camera standing on top of it,
    /// where a ring and a disc are the same picture. **A hole in a mesh is a fact about the mesh**,
    /// and checking it here costs a millisecond and cannot be defeated by a camera angle.
    /// ⚠️ `docs/TODO.md` § 127.3 still owes the greyscale frame; this does not replace it, it
    /// makes it a question about legibility rather than about whether the code did anything.
    ///
    /// ⚠️ `CLAUDE.md` § 6.5 is the rule one subsystem over: *"a shape difference survives a
    /// photograph and a colourblind player; a fill difference does not."*
    /// </summary>
    public sealed class RoleMarkerTests
    {
        /// <summary>
        /// The values `CharacterNameplate.TayaRingMesh` builds with. ⚠️ **Kept in step by hand and
        /// that is a known cost**: the mesh is generated inside a private method on a
        /// `MonoBehaviour` that needs a live `CharacterMotor` above it, so reaching the real one
        /// from EditMode would mean standing up a character. If the ring stops being a ring, this
        /// test still fails, because the assertion below is about what `Collar` produces from
        /// these arguments and § 127 is about that being an annulus at all.
        /// </summary>
        private const int Sides = 32;
        private const float Height = 0.10f;
        private const float InnerRatio = 0.66f;

        [Test]
        public void TheTayaMarkerIsAnAnnulusWithARealHoleInIt()
        {
            var ring = VfxShapes.Collar(sides: Sides, height: Height, innerRatio: InnerRatio);

            Assert.IsNotNull(ring, "VfxShapes.Collar built nothing.");
            Assert.Greater(ring.vertexCount, 0, "the ring mesh has no vertices.");

            float nearest = float.MaxValue;
            float furthest = 0.0f;

            foreach (var v in ring.vertices)
            {
                // ⚠️ THE RADIUS IS MEASURED IN XZ ONLY. `Collar` stands its walls up in Y, so a
                // vertex on the inner wall's top and one on its foot share a radius and differ
                // only in height; folding Y in would report the wall as a slope and the hole as
                // smaller than it is.
                float r = new Vector2(v.x, v.z).magnitude;

                nearest = Mathf.Min(nearest, r);
                furthest = Mathf.Max(furthest, r);
            }

            // ⚠️⚠️ THE HOLE IS THE WHOLE POINT. A disc has vertices at the centre; an annulus's
            // nearest vertex sits on its inner wall. If this ever drops toward zero the marker has
            // silently become a disc again and the taya is back to being told apart by hue alone.
            Assert.That(nearest, Is.GreaterThan(InnerRatio * 0.9f),
                        $"the taya's marker has a vertex {nearest:F3} from its centre, so it is " +
                        "filled rather than open. It has to be a RING: that is the second channel " +
                        "docs/FUTURE.md 16.1 asks for and the only thing separating the taya from " +
                        "an attacker with the colour taken away.");

            Assert.That(furthest, Is.EqualTo(1.0f).Within(0.02f),
                        "the ring is not built at unit radius any more. `ApplySizing` multiplies " +
                        "by `_ringUnitSpan`, which is 1.0 for this mesh and 2.0 for the cylinder " +
                        "primitive the attackers use, so a change here draws the marker at the " +
                        "wrong size rather than failing visibly.");

            Object.DestroyImmediate(ring);
        }

        /// <summary>
        /// ⚠️ THE TWO MARKERS MUST NOT BE THE SAME SHAPE, WHICH IS THE CLAIM ONE LEVEL UP FROM THE
        /// TEST ABOVE. A cylinder primitive is what an attacker gets, and its cap reaches the
        /// centre; asserting that here is what stops somebody "simplifying" both roles onto one
        /// mesh and quietly deleting the distinction.
        /// </summary>
        [Test]
        public void TheAttackerMarkerIsFilledRightToItsCentre()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            try
            {
                var disc = go.GetComponent<MeshFilter>().sharedMesh;
                Assert.IsNotNull(disc, "the cylinder primitive carries no mesh.");

                float nearest = float.MaxValue;

                foreach (var v in disc.vertices)
                    nearest = Mathf.Min(nearest, new Vector2(v.x, v.z).magnitude);

                Assert.That(nearest, Is.LessThan(0.05f),
                            "the attacker's disc no longer reaches its own centre, so it has " +
                            "become a ring too and the taya's marker is no longer distinct.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
