using System;
using System.Collections.Generic;
using NUnit.Framework;
using TumbangPreso.EditorTools;
using TumbangPreso.InputLayer;
using TumbangPreso.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TumbangPreso.Tests
{
    /// <summary>
    /// The cheap half of the input contract: everything that can be asserted without a scene.
    ///
    /// ⚠️⚠️ THE CHEAP ONE CARRIES THE NUMBERS ON PURPOSE, WHICH IS `docs/TODO.md` § 124.11'S OWN
    /// RULE: *"a bound only a twelve-minute PlayMode run can enforce is a bound somebody edits a
    /// string past on a Friday."* `InputSurfaceProbe` measures real rectangles at twelve
    /// resolutions and takes minutes. Everything here runs in about forty milliseconds and is
    /// what actually stops a bad commit.
    /// </summary>
    public class InputContractTests
    {
        private static InputActionAsset LoadAsset()
        {
            var asset = Resources.Load<InputActionAsset>("TumbangPreso");
            Assert.IsNotNull(asset, "no InputActionAsset at Resources/TumbangPreso.");
            return asset;
        }

        /// <summary>
        /// ⚠️⚠️ THIS IS THE BACKSTOP FOR THE COMPILE GATE, NOT A DUPLICATE OF IT.
        /// `InputCatalogue.For` is a switch expression with no discard arm and
        /// `Assets/TumbangPreso/Runtime/csc.rsp` turns CS8509 into an error, so a missing verb
        /// should never reach here. **This test is what fails if somebody deletes that .rsp**, and
        /// a one-line file is exactly the kind of thing a merge drops. Belt and braces, and the
        /// braces are eight milliseconds.
        /// </summary>
        [Test]
        public void EveryVerbDeclaresAGamepadAndATouchBinding()
        {
            var verbs = (Verb[])Enum.GetValues(typeof(Verb));

            Assert.AreEqual(verbs.Length, InputCatalogue.All.Count,
                "the catalogue and the Verb enum disagree on how many verbs there are. If this " +
                "failed rather than the compiler, `Runtime/csc.rsp` has been deleted: it is what " +
                "turns a non-exhaustive switch into an error.");

            foreach (var verb in verbs)
            {
                var entry = InputCatalogue.For(verb);

                Assert.IsNotEmpty(entry.Action, $"{verb} names no input action.");
                Assert.IsNotEmpty(entry.GamepadPath, $"{verb} has no gamepad binding.");
                Assert.IsNotEmpty(entry.TouchLabel, $"{verb}'s touch control has no label.");

                Assert.IsTrue(entry.GamepadPath.StartsWith(Rebinding.GamepadDevice),
                    $"{verb}'s gamepad path '{entry.GamepadPath}' does not name a gamepad.");

                Assert.GreaterOrEqual(TouchMetrics.UnitsFor(entry.Size),
                                      TouchMetrics.MinTargetUnits,
                    $"{verb}'s touch size is under the thumb floor.");
            }
        }

        // -------------------------------------------------------------------
        // § THE TOUCH LAYER MAY NOT TEACH A KEY
        //
        // ⚠️⚠️ 🧑 2026-09-03, WITH A SCREENSHOT OF THE ANDROID BUILD: *"why the fuck does it have
        // keybinds theres no keys in mobile"*, and *"ive never seen a mobile game say GRAB or
        // lunge, usually it has an intuitive icon for it or the skill icon"*. Two of the nine
        // thumb controls were painted `Q` and `E` and a third `ULT`, because `VerbInput` could
        // only hold a STRING for what a button draws, so whoever filled the table in wrote what
        // each control was called and for the hero slots that was its keyboard key.
        //
        // ⚠️⚠️ THE COMPILE GATE ALREADY STOPS A VERB SHIPPING WITHOUT A PICTURE (`VerbInput.Glyph`
        // is a constructor parameter with no default). **These two tests stop it shipping with the
        // WRONG one**, which is the half a type cannot check: nothing about `VerbGlyph.Jump`
        // prevents somebody giving it to LUNGE as well, and two controls with one picture is the
        // same defect as two with one word.
        // -------------------------------------------------------------------

        /// <summary>
        /// ⚠️ EVERY THUMB CONTROL DRAWS A DIFFERENT PICTURE. `AbilityGlyph`'s rule, one layer
        /// down: *"a wrong icon is worse than a generic one, because the player trusts it once
        /// and then stops trusting all of them."* Two controls sharing a glyph is a player
        /// pressing the wrong one and learning that the icons do not mean anything.
        /// </summary>
        [Test]
        public void NoTwoVerbsDrawTheSameTouchGlyph()
        {
            var owners = new Dictionary<UI.VerbGlyph, Verb>();

            foreach (var entry in InputCatalogue.All)
            {
                Assert.IsFalse(owners.ContainsKey(entry.Glyph),
                    $"{entry.Verb} and {owners.GetValueOrDefault(entry.Glyph)} both draw " +
                    $"{entry.Glyph} on the touch layer.");

                owners[entry.Glyph] = entry.Verb;

                Assert.IsNotNull(UI.VerbIcons.For(entry.Glyph),
                    $"{entry.Verb}'s glyph {entry.Glyph} bakes to nothing.");
            }
        }

        /// <summary>
        /// ⚠️⚠️ THE REGRESSION GUARD FOR THE REPORTED BUG. A touch label that is one or two
        /// characters is a key cap, not a name: `Q`, `E`, `F`, `LMB`. This is a text rule rather
        /// than a list of forbidden strings on purpose, because the next one to leak will be
        /// whatever key the next verb happens to be bound to.
        ///
        /// ⚠️ IT CHECKS THE LABEL EVEN THOUGH NOTHING DRAWS IT ON A BUTTON ANY MORE. The label
        /// still reaches the player through `TouchLayoutScreen` and through `GuidedTraining`'s
        /// touch branch, and both of those are places a bare `Q` would be exactly as wrong.
        /// </summary>
        [Test]
        public void NoTouchControlIsNamedAfterAKeyboardKey()
        {
            foreach (var entry in InputCatalogue.All)
            {
                Assert.GreaterOrEqual(entry.TouchLabel.Length, 3,
                    $"{entry.Verb}'s touch label is '{entry.TouchLabel}', which is a key cap " +
                    "rather than a name. A phone has no keys: see § THE TOUCH LAYER MAY NOT " +
                    "TEACH A KEY.");

                foreach (char c in entry.TouchLabel)
                    Assert.IsFalse(c == '[' || c == ']',
                        $"{entry.Verb}'s touch label '{entry.TouchLabel}' is drawn as a key cap.");
            }
        }

        /// <summary>
        /// ⚠️ ONE CONTROL, ONE ACTION, PER CONTEXT: the verbs all share the gameplay context, so
        /// two of them on one pad button is the plain form of `CLAUDE.md` § 4's rule.
        /// </summary>
        [Test]
        public void NoTwoVerbsShareAGamepadControl()
        {
            var owners = new Dictionary<string, Verb>();

            foreach (var entry in InputCatalogue.All)
            {
                Assert.IsFalse(owners.ContainsKey(entry.GamepadPath),
                    $"{entry.Verb} and {owners.GetValueOrDefault(entry.GamepadPath)} are both on " +
                    $"{entry.GamepadPath}, and both fire in the same context.");

                owners[entry.GamepadPath] = entry.Verb;
            }
        }

        /// <summary>
        /// ⚠️ TWO CONTROLS IN ONE ZONE AT ONE SLOT ARE DRAWN ON TOP OF EACH OTHER, and the
        /// failure is silent: the second simply eats the first's presses. `TouchHud` computes a
        /// position from the slot index, so the slot IS the position.
        /// </summary>
        [Test]
        public void NoTwoTouchControlsShareAZoneAndSlot()
        {
            var taken = new Dictionary<string, Verb>();

            foreach (var entry in InputCatalogue.All)
            {
                string key = $"{entry.Zone}#{entry.Slot}";

                Assert.IsFalse(taken.ContainsKey(key),
                    $"{entry.Verb} and {taken.GetValueOrDefault(key)} are both at " +
                    $"{entry.Zone} slot {entry.Slot}, so one is drawn on top of the other.");

                taken[key] = entry.Verb;
            }
        }

        /// <summary>
        /// Every rebindable action has been answered for on a pad, including a written-down "no".
        ///
        /// ⚠️⚠️ THIS IS THE ROW THAT CATCHES A NEW NON-VERB ACTION. The compile gate covers
        /// `Verb`; READY UP, HIDE HUD, the curve pair and the ten spectator rows are not verbs and
        /// cannot be covered by it. `Rebinding.RebindableActions` is the list they are added to,
        /// so this asserts the two lists agree in BOTH directions: an action with no answer, and
        /// an answer for an action that no longer exists. `Rebinding`'s own class note says why
        /// the second matters as much as the first: *"a stale row in either table is not
        /// cosmetic... here a missing action silently produces a dead row."*
        /// </summary>
        [Test]
        public void EveryRebindableActionDeclaresADeviceAnswer()
        {
            var unanswered = new List<string>();

            foreach (string action in Rebinding.RebindableActions)
            {
                // The four discrete movement rows are parts of the WASD composite; the pad's
                // stick is a separate binding on `Move` that no direction owns. That is the
                // correct answer for all four and it is why they are exempt here rather than
                // carrying four null rows that would read as forgotten.
                if (action.StartsWith("Move")) continue;

                if (InputCatalogue.VerbForAction(action) != null) continue;
                if (ScreenInputCatalogue.Declares(action)) continue;

                unanswered.Add(action);
            }

            Assert.IsEmpty(unanswered,
                "these actions have no controller or touch answer, not even a deliberate 'no'. " +
                "Add a row to ScreenInputCatalogue, or a Verb entry to InputCatalogue:\n"
                + string.Join("\n", unanswered));

            var stale = new List<string>();

            foreach (var row in ScreenInputCatalogue.Rows)
                if (Array.IndexOf(Rebinding.RebindableActions, row.Action) < 0)
                    stale.Add(row.Action);

            Assert.IsEmpty(stale,
                "ScreenInputCatalogue answers for actions that are no longer rebindable:\n"
                + string.Join("\n", stale));
        }

        /// <summary>
        /// The shipped asset carries every binding the catalogue declares.
        ///
        /// ⚠️⚠️ THE ASSET IS GENERATED FROM THE CATALOGUE AND THIS IS WHAT NOTICES WHEN IT IS
        /// NOT. `InputAssetSync.Regenerate` writes it; nothing forces anybody to run that after
        /// adding a verb, so this is the thing that says so. The repair is one command and the
        /// failure message names it.
        /// </summary>
        [Test]
        public void TheShippedAssetCarriesEveryGamepadBindingTheCatalogueDeclares()
        {
            var gaps = InputAssetSync.Missing(LoadAsset());

            Assert.IsEmpty(gaps,
                "the input asset and the catalogue have drifted apart. Run:\n" +
                "  Unity.exe -batchmode -quit -projectPath . -executeMethod " +
                "TumbangPreso.EditorTools.InputAssetSync.Regenerate\n\n"
                + string.Join("\n", gaps));
        }

        /// <summary>
        /// ⚠️ THE SAME ASSERTION `InputMapAndAbilityTests` ALREADY MAKES, RE-RUN AFTER THE PAD
        /// BINDINGS LANDED. `FindDuplicateBindings` used to read only each action's FIRST binding,
        /// which was the keyboard one; adding a pad binding beside every key would have doubled
        /// the map's size while halving what the rule actually checked, and nothing would have
        /// said so.
        /// </summary>
        [Test]
        public void AddingTheGamepadDidNotIntroduceAClashInAnyContext()
        {
            var clashes = Rebinding.FindDuplicateBindings(LoadAsset());

            Assert.IsEmpty(clashes,
                "two actions in one context share one control:\n" + string.Join("\n", clashes));
        }

        /// <summary>
        /// A rebind on one device leaves the other device's binding alone.
        ///
        /// ⚠️⚠️ THE FAULT THIS PINS DOWN DESTROYED A PLAYER'S KEYBOARD SILENTLY. `TryRebind`
        /// applied the override to binding index 0, which is the KEY, so rebinding SPRINT with a
        /// pad wrote `&lt;Gamepad&gt;/...` over `&lt;Keyboard&gt;/leftShift`: the row then read
        /// "Button South", the key stopped working, and Reset All was the only way back.
        /// </summary>
        [Test]
        public void RebindingOnOneDeviceLeavesTheOtherAlone()
        {
            var asset = LoadAsset();
            asset.RemoveAllBindingOverrides();

            string before = Rebinding.DisplayNameFor(asset, "Jump", "<Keyboard>");
            Assert.AreNotEqual("-", before, "Jump has no keyboard binding to protect.");

            var indices = Rebinding.ResolveBindingIndices(asset, "Jump", out var jump);
            Assert.IsNotNull(jump);

            int padIndex = -1;

            foreach (int i in indices)
                if (Rebinding.DeviceOf(jump.bindings[i].effectivePath) == Rebinding.GamepadDevice)
                    padIndex = i;

            Assert.GreaterOrEqual(padIndex, 0,
                "Jump has no gamepad binding, so this test cannot say anything. Run " +
                "InputAssetSync.Regenerate.");

            // Rebind the PAD half onto a different pad control.
            jump.ApplyBindingOverride(padIndex, "<Gamepad>/buttonEast");

            string after = Rebinding.DisplayNameFor(asset, "Jump", "<Keyboard>");

            Assert.AreEqual(before, after,
                "rebinding Jump on a gamepad changed what it is bound to on the keyboard.");

            asset.RemoveAllBindingOverrides();
        }

        /// <summary>
        /// ⚠️⚠️ CROSSPLAY IS A PROTOCOL CLAIM AND NOTHING IN THIS BATCH MAY MOVE IT. The handoff's
        /// own framing: `NetSession.ProtocolVersion` is the match FORMAT, and peers refuse each
        /// other across a mismatch by design. Input is entirely local: a pad, a thumb and a
        /// keyboard all arrive at `InputIntent` and nothing about which one was used goes on the
        /// wire. **So a phone and a desktop built from this commit must carry the same number**,
        /// and the way to guarantee that is to not touch it.
        ///
        /// ⚠️ IT ASSERTS THE VALUE, WHICH IS ON PURPOSE. A bump is legitimate when the match
        /// format changes; this test turning red is the prompt to ask whether it did, and to
        /// rebuild BOTH players before shipping either. `FUTURE.md` § 15: *"Mobile and desktop
        /// must ship the same version at the same time or they will refuse each other,
        /// correctly, and it will look like a bug."*
        /// </summary>
        [Test]
        public void TheInputPassDidNotMoveTheProtocolVersion()
        {
            // ⚠️ 24 SINCE 2026-09-05, AND THE MOVE WAS NOT AN INPUT CHANGE. `ConnectionHello`
            // gained the ladder rating the seat handover needs (`docs/TODO.md` § 144.7). This
            // assertion's job is unchanged: a bump has to be a deliberate act somebody edited a
            // test for, and the sentence below is what they read while doing it.
            Assert.AreEqual(24, Net.NetSession.ProtocolVersion,
                "ProtocolVersion moved. Input is local and does not travel, so if this changed " +
                "for an input reason it is wrong. If the match format genuinely changed, update " +
                "this number AND rebuild the Windows and Android players from the same commit.");
        }
    }

    internal static class DictionaryExtensions
    {
        /// <summary>Small helper so a failure message can name the other owner without a lookup.</summary>
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> map,
                                                             TKey key)
            => map.TryGetValue(key, out var value) ? value : default;
    }
}
