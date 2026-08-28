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
            while (load != null && !load.isDone) yield return null;

            for (int i = 0; i < SettleFrames; i++) yield return null;
            Canvas.ForceUpdateCanvases();

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
