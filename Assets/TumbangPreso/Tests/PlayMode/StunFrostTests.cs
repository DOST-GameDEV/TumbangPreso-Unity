using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// § THE STUN FROST, both halves, plus the rule change that shipped beside it.
    ///
    /// 🧑 2026-08-06, on the Godot build: *"can we have like a frost effect to indicate that an
    /// attacker is stunned after getting tagged?"* The screen frosts for the victim only; the
    /// body ices over for everybody. Two messages, two channels, and each is asserted separately
    /// here because a version that did only one of them would look finished from one seat.
    ///
    /// ⚠️⚠️ THE VIGNETTE IS ASSERTED BY ITS OWN SHADER PARAMETER, NOT BY "A FULL-SCREEN IMAGE
    /// EXISTS". The emote wheel shipped as eight white squares precisely because something was
    /// present and enabled while drawing nothing, and a frost overlay is the same shape of risk:
    /// a missing shader leaves an Image that is enabled, stretched, and invisible. Asking the
    /// material what coverage it is carrying is the cheapest question that cannot pass on a
    /// blank.
    /// </summary>
    public class StunFrostTests
    {
        private static IEnumerator LoadArena()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            for (int i = 0; i < 20; i++) yield return null;
        }

        private static CharacterMotor FirstAttacker()
        {
            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
            {
                if (m.IsPerson && !m.IsDefender) return m;
            }

            return null;
        }

        /// <summary>
        /// The body half: everybody in the arena sees a stunned attacker ice over, and sees the
        /// ice go away when the stun does.
        /// </summary>
        [UnityTest]
        public IEnumerator AStunnedBodyIcesOverAndThaws()
        {
            yield return LoadArena();

            var victim = FirstAttacker();
            Assert.IsNotNull(victim, "no attacker seat in the arena");

            var visual = victim.GetComponent<Visual.CharacterVisual>();
            Assert.IsNotNull(visual, "the attacker has no CharacterVisual to frost");

            Assert.AreEqual(0.0f, visual.FrostLevel, 0.001f,
                            "the body was already frosted before anything stunned it");

            victim.ApplyStagger(Balance.TagStunTime);

            // The ramp is FrostRampIn seconds to full, so give it comfortably more than that.
            float deadline = Time.time + Visual.CharacterVisual.FrostRampIn + 0.5f;
            while (Time.time < deadline && visual.FrostLevel < 0.99f) yield return null;

            Assert.Greater(visual.FrostLevel, 0.9f,
                $"the body never iced over: frost reached {visual.FrostLevel:0.000} while the " +
                $"motor reported {victim.StunLeft:0.00} s of stun left. The driver is " +
                "CharacterVisual.ProcessFrost and the uniform is _CaughtAmount on TumbangPreso/Toon. " +
                "It was _FrostAmount until 2026-08-26; see the note above that property.");

            // ⚠️ ASSERTED THROUGH THE REAL STUN EXPIRING, not by zeroing the level by hand. The
            // thaw is the half a player actually reads as "I am about to get control back", and
            // it is driven by StunLeft crossing FrostThawTime rather than by the state clearing.
            deadline = Time.time + Balance.TagStunTime + Visual.CharacterVisual.FrostRampOut + 1.0f;
            while (Time.time < deadline && visual.FrostLevel > 0.001f) yield return null;

            Assert.Less(visual.FrostLevel, 0.01f,
                        $"the body never thawed: frost stuck at {visual.FrostLevel:0.000} with " +
                        $"{victim.StunLeft:0.00} s of stun left.");
        }

        /// <summary>
        /// The screen half, and it is the VICTIM's screen only. Asserted through the material,
        /// for the reason in the class note.
        /// </summary>
        [UnityTest]
        public IEnumerator TheVictimsScreenFrostsOver()
        {
            yield return LoadArena();

            var hud = Object.FindFirstObjectByType<UI.Hud>();
            Assert.IsNotNull(hud, "no HUD in the arena");

            var victim = FirstAttacker();
            Assert.IsNotNull(victim, "no attacker seat in the arena");

            // Drive the HUD from a seat this test controls, rather than guessing which one the
            // installer handed the keyboard to.
            hud.Bind(victim);

            var frost = FindFrostImage(hud);
            Assert.IsNotNull(frost, "the HUD built no FrostVignette. Shader.Find(\"TumbangPreso/" +
                                    "FrostVignette\") returns null when the shader is not in the " +
                                    "build, and BuildFrostVignette bails silently in that case.");

            Assert.IsFalse(frost.enabled, "the frost was already drawing before anything stunned");

            victim.ApplyStagger(Balance.TagStunTime);

            // ⚠️ WAIT FOR THE RAMP, NOT FOR THE ENABLE FLAG, AND THE FIRST VERSION OF THIS TEST
            // FAILED ON EXACTLY THAT. `UpdateFrost` enables the Image the moment coverage crosses
            // 0.001 and then takes `FrostRampIn` seconds to climb to 1, so a loop that stops at
            // `frost.enabled` stops on the FIRST frame of the ramp and the coverage assertion
            // below read 0.018. Waiting on the value the assertion is about is the fix; the
            // enabled flag is checked separately, because the two can genuinely disagree.
            float deadline = Time.time + UI.Hud.FrostRampIn + 0.5f;
            while (Time.time < deadline && frost.material.GetFloat("_Coverage") < 0.99f)
                yield return null;

            Assert.IsTrue(frost.enabled,
                          $"the screen never frosted with {victim.StunLeft:0.00} s of stun left");

            // ⚠️ THE SHADER PARAMETER, NOT THE ENABLED FLAG. See the class note: an enabled Image
            // with a broken material is exactly what this is guarding against.
            Assert.Greater(frost.material.GetFloat("_Coverage"), 0.5f,
                           "the FrostVignette is enabled but its coverage is near zero, so it is " +
                           "drawing nothing");

            // The aspect has to be pushed or the band is ~1.8x thicker down the sides than across
            // the top, which is the failure that was measured on the original.
            Assert.Greater(frost.material.GetFloat("_Aspect"), 0.1f,
                           "the viewport aspect was never written, so the frost band is uneven");
        }

        /// <summary>
        /// ⚠️⚠️ A STUNNED ATTACKER IS TAGGABLE, AND THE OPPOSITE WAS A SHIPPED BUG IN BOTH TREES.
        /// 🧑 2026-08-06: *"a player that has been sabotaged by a player cannot be tagged by the
        /// defender. when the attacker is in a frozen state, it cannot be tagged."*
        ///
        /// `IsTaggable` asked `CanAct()`, whose second half is "not stunned" — a rule about what
        /// this player can DO, applied to something done TO them. It also made the 50-point
        /// sabotage award unreachable, because the shove that earns the credit is the same event
        /// that made the victim immune to the tag that pays it.
        /// </summary>
        [UnityTest]
        public IEnumerator AStunnedAttackerInTheBoxIsStillTaggable()
        {
            yield return LoadArena();

            var victim = FirstAttacker();
            Assert.IsNotNull(victim, "no attacker seat in the arena");

            // ⚠️ NOTHING ELSE MAY STEER THE VICTIM WHILE THIS RULE IS BEING ASKED. Turning
            // `RoundActive` on below is what lets a bot start playing, and a seat that walks out
            // of the box mid-test fails the assertion for a reason that has nothing to do with
            // the stun. Silencing the producer is the same trick `InputEdgeTests` needs and for
            // the same reason: the intent table has exactly one writer at a time.
            Silence(victim);

            victim.RoundActive = true;
            victim.HoldingSlipper = true;

            // Stand them squarely in the box, which is the other half of the rule.
            //
            // ⚠️⚠️ THROUGH `Teleport`, NEVER BY WRITING `transform.position`, AND THAT IS WHY
            // THIS TEST REPORTED "the harness failed to put the victim in the box". A
            // CharacterController keeps its own authoritative position and overwrites a direct
            // transform write on its next `Move`, so the capsule was back at its spawn before
            // `IsInsideBox()` was ever asked. `CharacterMotor.Teleport` disables the controller
            // around the write for exactly this reason and is the only supported way to place a
            // unit; `Confine` uses the same dance.
            victim.Teleport(new Vector3(0.0f, victim.transform.position.y, 0.0f));

            // Past the spawn-settle frames `Teleport` opens, which hold the position and return
            // early from the step.
            for (int i = 0; i < 6; i++) yield return new WaitForFixedUpdate();

            Assert.IsTrue(victim.IsInsideBox(), "the harness failed to put the victim in the box");
            Assert.IsTrue(victim.IsTaggable(), "an unstunned attacker in the box holding a " +
                                               "slipper was not taggable, so this test proves " +
                                               "nothing about the stun");

            victim.ApplyStagger(Balance.TagStunTime);
            yield return null;

            Assert.IsTrue(victim.IsStunned, "the harness failed to stun the victim");
            Assert.IsTrue(victim.IsTaggable(),
                          "a stunned attacker standing in the box holding a slipper was NOT " +
                          "taggable. That is IsTaggable() reading CanAct() again, which makes " +
                          "the most vulnerable state in the game into immunity and leaves the " +
                          "sabotage score unreachable.");

            // ⚠️ AND THE HALF THAT MUST STILL REFUSE. Dropping the round check would let a lunge
            // left over from the last frame of a round score into the intermission.
            victim.RoundActive = false;
            Assert.IsFalse(victim.IsTaggable(),
                           "a tag landed between rounds. RoundActive is the half of CanAct() " +
                           "that genuinely belongs in IsTaggable and must not have gone with it.");
        }

        /// <summary>
        /// Stop whatever normally drives this seat, so the test owns its intent and its position.
        ///
        /// ⚠️ BOTH PRODUCERS, because which one a given seat has is decided by the installer and
        /// is not this test's business: the human seat carries `PlayerInputReader`, every other
        /// carries `AIController`, and both write the whole intent table every Update.
        /// </summary>
        private static void Silence(CharacterMotor motor)
        {
            var reader = motor.GetComponent<PlayerInputReader>();
            if (reader != null) reader.enabled = false;

            var ai = motor.GetComponent<AIController>();
            if (ai != null) ai.enabled = false;

            motor.Intent.Clear();
            motor.Intent.CommitFrame();
        }

        private static Image FindFrostImage(UI.Hud hud)
        {
            foreach (var image in hud.GetComponentsInChildren<Image>(includeInactive: true))
            {
                if (image.gameObject.name == "FrostVignette") return image;
            }

            return null;
        }
    }
}
