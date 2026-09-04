using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Types into the lobby chat and asserts that something lands on the log.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE `docs/TODO.md` § 121.11 ASKED FOR IT BY NAME AND SAID WHY: 🧑 has
    /// reported **"chat doesnt work at all btw"** four separate ways, and the entry closes with
    /// *"Reproduce it in the player before writing anything, and a probe that types into the field
    /// and photographs the result is the cheapest way to make it repeatable."* Every previous pass
    /// read the code and guessed; the reading is genuinely ambiguous, because a line that never
    /// sends and a line that sends and never draws produce **the same empty panel**.
    ///
    /// ⚠️⚠️ THE ASSERTION IS DELIBERATELY WEAK AND THAT IS THE POINT. It does not require the line
    /// to be relayed, or a peer to exist, or the text to come back. It requires only that **the log
    /// stops reading `LobbyChat.EmptyLog` after a line is submitted**, because both legitimate
    /// outcomes write to it: a sent line echoes through `MatchRpc.HostRelayChat`, and an unsent one
    /// pushes *"Not connected. That line was not sent."* through `AddLocal`. **An empty log after a
    /// submit is the one state that cannot be correct**, which is exactly the state he photographed.
    /// </summary>
    public class LobbyChatProbe
    {
        /// <summary>
        /// ⚠️⚠️ THE PAIR THAT MAKES A FULL-SUITE RESULT MEAN ANYTHING. `docs/TODO.md` § 126.8:
        /// the full PlayMode run came back 42, 41 and then 56 red with the red set moving, and a
        /// gate whose red set moves is not measuring the code. `PlayModeWorld.Reset` has the
        /// mechanism and why BOTH hooks are needed rather than one.
        /// </summary>
        [UnitySetUp]
        public IEnumerator ResetWorldBefore() => PlayModeWorld.Reset();

        [UnityTearDown]
        public IEnumerator ResetWorldAfter() => PlayModeWorld.Reset();

        private const string Line = "probe hello";

        [UnityTest]
        public IEnumerator TypingALinePutsSomethingOnTheLog()
        {
            bool previousNetworked = SceneFlow.Networked;
            SceneFlow.Networked = true;

            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            // The same 4 s `UiRuntimeShots.TheLobbyDraws` waits: the auto-host handshake and the
            // additive arena load both have to finish or the chat is measured on half a screen.
            yield return new WaitForSecondsRealtime(4.0f);

            var chat = Object.FindFirstObjectByType<LobbyChat>(FindObjectsInactive.Include);
            Assert.IsNotNull(chat, "the lobby must have a LobbyChat.");

            // Open it the way a player does, through the chip, rather than by reaching in.
            var chip = FindByName("ChatChip")?.GetComponent<Button>();
            if (chip != null)
            {
                chip.onClick.Invoke();
                yield return new WaitForSecondsRealtime(0.35f);
            }

            var field = chat.GetComponentInChildren<InputField>(true);
            Assert.IsNotNull(field, "the chat must have an InputField.");

            // ⚠️ THE DIAGNOSTIC BLOCK IS THE HALF THAT SURVIVES A GREEN RUN. If this passes on the
            // machine and fails in his player, the log below is what says which of the four things
            // differs, and every one of them has been a real cause somewhere in this project.
            var report = new StringBuilder();
            report.AppendLine("[ChatProbe] state:");
            report.AppendLine($"  EventSystem present : {EventSystem.current != null}");
            report.AppendLine($"  MatchRpc.Instance   : {(MatchRpc.Instance == null ? "NULL" : "present")}");
            report.AppendLine($"  field active        : {field.gameObject.activeInHierarchy}");
            report.AppendLine($"  field interactable  : {field.interactable}");
            report.AppendLine($"  onSubmit listeners  : {field.onSubmit.GetPersistentEventCount()} persistent");
            report.AppendLine($"  raycaster above     : {(field.GetComponentInParent<GraphicRaycaster>() != null)}");

            var ownCanvas = field.GetComponentInParent<Canvas>();
            report.AppendLine($"  canvas order        : {(ownCanvas == null ? "none" : ownCanvas.sortingOrder.ToString())}");
            Debug.Log(report.ToString());

            // ⚠️⚠️⚠️ THE RAYCAST IS THE WHOLE PROBE NOW, BECAUSE THE SUBMIT PATH ALREADY PASSED.
            // The first run of this file proved `Submit`, the host echo and the draw are all
            // correct: invoking `onSubmit` puts the line on the log. **So the fault is upstream of
            // all three, in whether a press can reach the field at all**, which is the one thing
            // `field.interactable` and a present raycaster do NOT tell you. This asks the
            // EventSystem the question a player asks with the mouse: press here, what gets it?
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(
                    null, field.transform.position),
            };

            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);

            var top = hits.Count > 0 ? hits[0] : default;
            Debug.Log($"[ChatProbe] raycast at the field: {hits.Count} hit(s), " +
                      $"top = '{(top.gameObject == null ? "nothing" : top.gameObject.name)}' " +
                      $"on canvas order {(top.module is GraphicRaycaster gr && gr.GetComponent<Canvas>() != null ? gr.GetComponent<Canvas>().sortingOrder.ToString() : "?")}");

            for (int i = 0; i < hits.Count && i < 6; i++)
                Debug.Log($"[ChatProbe]   hit {i}: {hits[i].gameObject.name} " +
                          $"(sortingOrder {hits[i].sortingOrder}, depth {hits[i].depth})");

            bool reaches = top.gameObject != null &&
                           (top.gameObject == field.gameObject ||
                            top.gameObject.transform.IsChildOf(field.transform) ||
                            field.transform.IsChildOf(top.gameObject.transform));

            Debug.Log($"[ChatProbe] a press on the field reaches it: {reaches}");

            // ⚠️⚠️⚠️ THE FOCUS TEST, WHICH IS THE LAST LINK AND THE ONLY ONE LEFT UNPROVEN. The
            // submit path passes, the draw passes and the raycast reaches the field, so if typing
            // still does nothing in his player then the field is never taking focus and every
            // keystroke is going to whatever is selected instead. A press is delivered through
            // `ExecuteEvents` here rather than by calling `ActivateInputField` directly, because
            // the question is what the PLAYER'S click does, and the two are not the same call.
            ExecuteEvents.Execute(top.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return null;
            yield return null;

            var selected = EventSystem.current.currentSelectedGameObject;
            Debug.Log($"[ChatProbe] after a click: field.isFocused = {field.isFocused}, " +
                      $"selected = '{(selected == null ? "nothing" : selected.name)}'");

            field.ActivateInputField();
            yield return null;
            yield return null;

            selected = EventSystem.current.currentSelectedGameObject;
            Debug.Log($"[ChatProbe] after ActivateInputField: field.isFocused = {field.isFocused}, " +
                      $"selected = '{(selected == null ? "nothing" : selected.name)}'");
            Debug.Log($"[ChatProbe] LobbyChat.AnyTyping = {LobbyChat.AnyTyping}");

            string before = LogText(chat);
            Debug.Log($"[ChatProbe] log before: '{before}'");

            // ⚠️⚠️ THE FIELD IS DRIVEN THROUGH ITS OWN EVENT RATHER THAN THROUGH THE KEYBOARD,
            // because batch mode has no key presses and `InputField` reads the real input module.
            // **That is a deliberate narrowing and it has to be stated**: this proves the SUBMIT
            // path, not the focus path. A field nothing can click would still pass here, which is
            // why the diagnostic block above records the raycaster and the canvas order instead of
            // trusting them.
            field.text = Line;
            field.onSubmit.Invoke(Line);

            yield return new WaitForSecondsRealtime(0.5f);

            string after = LogText(chat);
            Debug.Log($"[ChatProbe] log after: '{after}'");

            SceneFlow.Networked = previousNetworked;

            Assert.AreNotEqual(LobbyChat.EmptyLog, after.Trim(),
                "the chat log still reads the empty placeholder after a line was submitted. " +
                "Both outcomes write to it (a relayed echo, or the not-connected note), so an " +
                "empty log means the submit never reached LobbyChat.Submit.");

            Assert.IsTrue(after.Contains(Line) || after.Contains("not sent"),
                $"the log changed but carries neither the line nor the refusal. It reads '{after}'.");
        }

        /// <summary>
        /// Everything the chat is currently drawing, joined.
        ///
        /// ⚠️ IT READS THE VISIBLE `Text` COMPONENTS RATHER THAN `_history`, because the fault this
        /// probe exists for could be either one: § 121.11 says *"either the submit never fires or
        /// the push never draws, and those are different bugs in different files"*. Asserting on
        /// the private list would prove the first and be blind to the second.
        /// </summary>
        private static string LogText(LobbyChat chat)
        {
            var parts = new List<string>();

            foreach (var text in chat.GetComponentsInChildren<Text>(true))
            {
                // The placeholder inside the field is not the log, and it always reads
                // "Say something", which would mask an empty panel on every comparison.
                if (text.transform.parent != null && text.transform.parent.GetComponent<InputField>() != null)
                    continue;

                if (!string.IsNullOrWhiteSpace(text.text)) parts.Add(text.text);
            }

            return string.Join(" | ", parts);
        }

        private static GameObject FindByName(string name)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                                                                 FindObjectsSortMode.None))
                if (t.name == name) return t.gameObject;

            return null;
        }
    }
}
