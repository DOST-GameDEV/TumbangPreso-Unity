using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Asks, of every text field in the lobby, whether a click on it leaves the player able to
    /// TYPE into it. That is a different question from `UiClickProbe`'s and neither one implies
    /// the other.
    ///
    /// ⚠️⚠️ `UiClickProbe` SAYS BOTH LOBBY FIELDS ARE REACHABLE AND BOTH WERE REPORTED DEAD.
    /// 🧑 2026-08-29: *"sa lobby hindi nagana yung player name, hindi makapag input ng name"* and
    /// *"hindi maka input ng code and lobby code sa lobby"*. Widening that probe to `InputField`
    /// was the first thing tried and it came back green on `PlayerNameEdit` and `JoinAddressEdit`
    /// alike, which is a real answer rather than a failed attempt: it rules out the whole class
    /// of cause that probe exists for, a decorative graphic sitting over the control. Nothing was
    /// covering either field. So the click lands, and the fault is somewhere after it.
    ///
    /// This probe walks the next two steps of the same press:
    ///
    ///   1. **Selection.** Pointer-down and click on the field, through the EventSystem, exactly
    ///      as the input module raises them. Afterwards `EventSystem.currentSelectedGameObject`
    ///      must BE the field. A control that takes the raycast and does not take the selection
    ///      is dead in precisely the way that gets reported as "hindi nagana".
    ///   2. **It keeps it.** Ten frames later it must STILL be selected. This is the half a
    ///      one-frame check cannot see: something that re-focuses another field, or clears the
    ///      selection every frame, hands the caret back before the player's second keystroke and
    ///      leaves the field looking alive and behaving dead. `LobbyChat` calls
    ///      `ActivateInputField` from three places and one of them runs on a bare Return.
    ///
    /// ⚠️ IT DOES NOT SYNTHESISE KEYSTROKES, AND THAT IS A LIMIT WORTH WRITING DOWN RATHER THAN
    /// WORKING AROUND. Legacy `InputField` pulls characters from the OS event queue through
    /// `Event.PopEvent`, which a test cannot fill: any "typing" here would be `field.text = "x"`,
    /// which proves the setter works and nothing else. Selection is the part another component
    /// can actually break, so selection is what is asserted.
    /// </summary>
    public class LobbyTypingProbe
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

        private const string OutPath = "Logs/lobby-typing.txt";

        /// <summary>See `UiClickProbe.SettleFrames`, for the same reason.</summary>
        private const int SettleFrames = 120;

        /// <summary>
        /// Frames held between selecting a field and re-reading the selection. Ten is more than
        /// one, which is the only thing that matters: every mechanism that could take the caret
        /// back does it from an `Update`.
        /// </summary>
        private const int HoldFrames = 10;

        [UnityTest]
        public IEnumerator EveryLobbyFieldTakesAndKeepsTheCaret()
        {
            var report = new StringBuilder();
            var broken = new List<string>();

            if (!Application.CanStreamedLevelBeLoaded("MatchSetup"))
            {
                Assert.Ignore("MatchSetup is not in the build settings.");
                yield break;
            }

            var load = SceneManager.LoadSceneAsync("MatchSetup", LoadSceneMode.Single);
            yield return ProbeWait.Done(load, "scene load");

            for (int i = 0; i < SettleFrames; i++) yield return null;
            Canvas.ForceUpdateCanvases();

            // ⚠️⚠️ THE CHAT IS OPENED FIRST, THROUGH ITS OWN DOOR, BECAUSE A CLOSED CHAT IS
            // DELIBERATELY UNTYPEABLE AND THIS PROBE READ THAT AS THE FAULT IT HUNTS.
            // `LobbyChat.SetPresented(false)` sets its CanvasGroup to alpha 0 and writes
            // `_field.interactable = false`, and it does NOT deactivate the object, so a sweep
            // that excludes inactive objects finds a live `ChatInput` a player cannot type into
            // and is right about the letter of it. The lobby opens with the chat closed on
            // purpose (§ 114: an empty log is a promise, not a screen element), so the only
            // question worth asking is about the OPEN chat, which is what CHAT gets you.
            yield return PresentTheChat(report);

            // The lobby's own furniture first, then the join card, which is built inactive and
            // is the only place the join-code field exists.
            yield return Check("lobby", report, broken);

            var join = FindByName("LobbyJoinPanel");

            if (join == null)
            {
                report.AppendLine("LobbyJoinPanel: NOT PRESENT");
                broken.Add("LobbyJoinPanel was never built");
            }
            else
            {
                join.SetActive(true);
                for (int i = 0; i < SettleFrames; i++) yield return null;
                Canvas.ForceUpdateCanvases();

                yield return Check("join card", report, broken);
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText(OutPath, report.ToString(), new UTF8Encoding(false));
            Debug.Log(report.ToString());

            Assert.IsEmpty(broken, "fields a player cannot type into:\n" + string.Join("\n", broken));
        }

        /// <summary>
        /// ⚠️ `internal` SO `NetworkedLobbyTypingProbe` RUNS THIS EXACT CODE RATHER THAN A COPY
        /// OF IT. The two probes differ in ONE thing, whether a real host is listening while the
        /// lobby is up, and that difference is only readable if everything else is identical.
        /// A second copy of the check is a second thing that can drift, and then a disagreement
        /// between the two probes stops meaning anything. `CLAUDE.md` § 4 makes the same argument
        /// about the core sources compiling in place rather than being copied.
        /// </summary>
        internal static IEnumerator Check(string where, StringBuilder report, List<string> broken)
        {
            var system = EventSystem.current;

            if (system == null)
            {
                report.AppendLine($"--- {where} --- NO EVENT SYSTEM");
                broken.Add($"{where}: no EventSystem");
                yield break;
            }

            report.AppendLine($"--- {where} ---");

            foreach (var field in Object.FindObjectsByType<InputField>(FindObjectsInactive.Exclude,
                                                                       FindObjectsSortMode.None))
            {
                var rect = field.transform as RectTransform;
                if (rect == null) continue;

                if (!field.IsInteractable())
                {
                    report.AppendLine($"   {field.name}: NOT INTERACTABLE");
                    broken.Add($"{where}: {field.name} is not interactable");
                    continue;
                }

                var canvas = field.GetComponentInParent<Canvas>();
                var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? canvas.worldCamera
                    : null;

                Vector3 centre = rect.TransformPoint(rect.rect.center);
                Vector2 point = RectTransformUtility.WorldToScreenPoint(cam, centre);

                // Off the batch runner's 4:3 viewport is a statement about the viewport, not
                // about the control. `UiClickProbe` carries the same note and the same decision.
                if (point.x < 0.0f || point.y < 0.0f
                    || point.x > Screen.width || point.y > Screen.height)
                {
                    report.AppendLine($"   {field.name}: OFF SCREEN at {point}");
                    continue;
                }

                system.SetSelectedGameObject(null);
                yield return null;

                var data = new PointerEventData(system)
                {
                    position = point,
                    button = PointerEventData.InputButton.Left,
                };

                ExecuteEvents.Execute(field.gameObject, data, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(field.gameObject, data, ExecuteEvents.pointerClickHandler);

                yield return null;

                var taken = system.currentSelectedGameObject;

                if (taken != field.gameObject)
                {
                    string who = taken == null ? "nothing" : taken.name;
                    report.AppendLine($"   {field.name}: CLICK DID NOT SELECT IT (selection is {who})");
                    broken.Add($"{where}: clicking {field.name} selects {who}");
                    continue;
                }

                for (int i = 0; i < HoldFrames; i++) yield return null;

                var kept = system.currentSelectedGameObject;

                if (kept != field.gameObject)
                {
                    string who = kept == null ? "nothing" : kept.name;
                    report.AppendLine($"   {field.name}: LOST THE CARET after {HoldFrames} frames (now {who})");
                    broken.Add($"{where}: {field.name} loses focus to {who} within {HoldFrames} frames");
                    continue;
                }

                report.AppendLine($"   {field.name}: ok (selected and held {HoldFrames} frames)");
            }
        }

        /// <summary>
        /// Press CHAT the way a player does, and fall back to the component when the lobby on
        /// screen has no such door. Reported either way, because "there was no CHAT button" is
        /// the thing a reader of this log needs to know before believing the lines under it.
        /// </summary>
        internal static IEnumerator PresentTheChat(StringBuilder report)
        {
            // ⚠️ EVERY `LobbyChat` IN THE SCENE, NOT THE FIRST ONE. The lobby builds one and the
            // join card builds its own, and this walk reports a field per surface: presenting
            // one of the two left the other half of the list still red and reading exactly as
            // it did before, which is the least useful shape a partial fix can have.
            var chats = Object.FindObjectsByType<TumbangPreso.UI.LobbyChat>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (chats.Length == 0)
            {
                report.AppendLine("--- chat --- NOT BUILT");
                yield break;
            }

            var door = FindByName("ChatButton") ?? FindByName("ChatChip");
            var button = door != null ? door.GetComponent<Button>() : null;

            if (button != null && button.IsInteractable())
            {
                button.onClick.Invoke();
                report.AppendLine($"--- chat --- opened by pressing {door.name}");
            }

            for (int i = 0; i < HoldFrames; i++) yield return null;

            // Whatever the door reached, the rest are presented directly: a join card's chat has
            // no door of its own on the lobby it is drawn over.
            foreach (var chat in chats)
            {
                if (chat == null || chat.IsPresented) continue;

                chat.SetPresented(true);
                report.AppendLine($"--- chat --- {Path(chat.transform)} presented directly");
            }

            for (int i = 0; i < HoldFrames; i++) yield return null;
            Canvas.ForceUpdateCanvases();

            foreach (var chat in chats)
                if (chat != null && !chat.IsPresented)
                    report.AppendLine($"--- chat --- {Path(chat.transform)} STILL CLOSED");
        }

        private static string Path(Transform t)
        {
            string path = t.name;

            for (var p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;

            return path;
        }

        internal static GameObject FindByName(string name)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var hit = FindIn(root.transform, name);
                if (hit != null) return hit;
            }

            return null;
        }

        private static GameObject FindIn(Transform node, string name)
        {
            if (node.name == name) return node.gameObject;

            for (int i = 0; i < node.childCount; i++)
            {
                var hit = FindIn(node.GetChild(i), name);
                if (hit != null) return hit;
            }

            return null;
        }
    }
}
