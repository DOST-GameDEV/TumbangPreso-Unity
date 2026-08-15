using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The human seat can actually be driven.
    ///
    /// ⚠️⚠️ THIS IS THE REGRESSION TEST FOR THE WORST BUG IN THE PORT. `PlayerInputReader`
    /// took its InputActionAsset from a serialised field, and `MatchInstaller` installs it with
    /// `AddComponent`, which cannot carry an inspector reference. So the field was null on every
    /// unit in every build: the component logged one line and disabled itself, and the match then
    /// ran perfectly with three bots and a player who could not move. Every symptom of that
    /// points at the motor, the camera or the arena rather than at an unassigned field, which is
    /// why it survived so long.
    ///
    /// The assertion is deliberately about the COMPONENT STAYING ENABLED rather than about
    /// movement: enabled means it found its actions and bound all seven, and that is the exact
    /// thing that was false.
    /// </summary>
    public class InputReaderTests
    {
        [UnityTest]
        public IEnumerator ReaderFindsItsActionsWithNothingAssigned()
        {
            var go = new GameObject("Seat", typeof(CharacterController));
            go.AddComponent<CharacterMotor>();

            // Exactly how MatchInstaller does it: no inspector, no assignment.
            var reader = go.AddComponent<PlayerInputReader>();

            yield return null;

            Assert.IsTrue(reader.enabled,
                "PlayerInputReader disabled itself, which means it never found " +
                "Resources/TumbangPreso. The human seat is unplayable in a build.");

            Object.DestroyImmediate(go);
        }

        /// <summary>
        /// ⚠️ AND THE ASSET HAS TO BE WHERE BOTH SIDES LOOK. The settings panel rebinds on
        /// `Resources/TumbangPreso`; if the reader loaded a different copy, a rebind would apply
        /// to an object the game does not listen to and the setting would silently do nothing.
        /// </summary>
        [Test]
        public void TheActionAssetIsInResources()
        {
            var asset = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("TumbangPreso");

            Assert.IsNotNull(asset, "no InputActionAsset at Resources/TumbangPreso.");
            Assert.IsNotNull(asset.FindActionMap("Player", false), "no Player action map.");

            foreach (var name in new[]
                     { "Move", "Sprint", "Jump", "SpecialAbility", "Grab", "Lunge", "EmoteWheel" })
            {
                Assert.IsNotNull(asset.FindActionMap("Player").FindAction(name, false),
                                 $"the Player map has no '{name}' action.");
            }
        }
    }
}
