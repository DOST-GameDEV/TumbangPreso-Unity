using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// ⚠️ THE ORIGINAL CARET CHECK DOES NOT TYPE. The focused HOST case below uses explicit
    /// `InputField.ProcessEvent` calls to exercise Unity's key editing after a raycast press,
    /// but it does not fill the OS `Event.PopEvent` queue. Neither is proof of physical typing.
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
            yield return HubFlowTests.OpenHome();
            yield return HubFlowTests.Press("NamePlate");
            Assert.IsTrue(Object.FindFirstObjectByType<TumbangPreso.UI.PlayerHub>().IsOpen);
            yield return Check("profile name", report, broken);
            Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .First(b => b.name == "ClosePlayerHub" && b.isActiveAndEnabled).onClick.Invoke();
            yield return null; yield return null;
            yield return HubFlowTests.Press("ModeCard");
            yield return HubFlowTests.Press("CustomCard");
            yield return HubFlowTests.Press("HostDoor");
            yield return Check("host room name", report, broken);
            TumbangPreso.UI.Hub.TumpHub.Current.Back(); yield return null;
            yield return HubFlowTests.Press("CustomCard");
            yield return HubFlowTests.Press("JoinDoor");
            yield return HubFlowTests.Press("Source2");
            yield return Check("join code", report, broken);
            Directory.CreateDirectory("Logs");
            File.WriteAllText(OutPath, report.ToString(), new UTF8Encoding(false));
            Assert.IsEmpty(broken, "fields a player cannot type into:\n" + string.Join("\n", broken));
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator HostRoomNameRaycastsEditsAndCreatesTheTypedLanTitle()
        {
            Assert.IsTrue(System.Environment.GetCommandLineArgs().Any(arg =>
                string.Equals(arg, "-tp-profile", System.StringComparison.OrdinalIgnoreCase)),
                "Room creation needs a named isolated profile.");
            var settings = TumbangPreso.Settings.SettingsStore.Current;
            string savedSettings = JsonUtility.ToJson(settings);
            bool networked = TumbangPreso.UI.SceneFlow.Networked;
            var rules = TumbangPreso.UI.SceneFlow.SelectedRules.Clone();
            bool pinned = TumbangPreso.UI.SceneFlow.RulesPinned;
            string map = TumbangPreso.UI.SceneFlow.SelectedMap;
            string roomTitle = TumbangPreso.Net.NetSession.RoomTitle;
            string roomMap = TumbangPreso.Net.NetSession.RoomMap;
            int roomVisibility = TumbangPreso.Net.NetSession.RoomVisibility;
            try
            {
                yield return HubFlowTests.OpenHome();
                yield return HubFlowTests.Press("ModeCard");
                yield return HubFlowTests.Press("CustomCard");
                yield return HubFlowTests.Press("HostDoor");

                var hub = TumbangPreso.UI.Hub.TumpHub.Current;
                Assert.IsInstanceOf<TumbangPreso.UI.Hub.HubHost>(hub.Top);
                var field = hub.Top.GetComponentsInChildren<InputField>(false)
                    .Single(input => input.name == "LobbyName");
                var plate = field.targetGraphic;
                Assert.IsNotNull(plate);
                Assert.AreEqual("Plate", plate.name);
                Assert.IsTrue(plate.transform.IsChildOf(field.transform));
                Assert.AreEqual(TumbangPreso.Settings.GameSettings.RoomTitleMax, field.characterLimit);
                const string full = "ABCDEFGHIJKLMNOPQRSTUVWX";
                Assert.AreEqual(field.characterLimit, full.Length);
                field.text = full; // Deterministic full prefill, never the edit under test.
                Canvas.ForceUpdateCanvases();

                var system = EventSystem.current;
                Assert.IsNotNull(system);
                var raycaster = hub.Canvas.GetComponent<GraphicRaycaster>();
                Assert.IsNotNull(raycaster);
                var camera = hub.Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hub.Canvas.worldCamera;
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera,
                    plate.rectTransform.TransformPoint(plate.rectTransform.rect.center));
                var pointer = new PointerEventData(system)
                {
                    position = point,
                    pressPosition = point,
                    button = PointerEventData.InputButton.Left,
                    eligibleForClick = true,
                };
                var canvasHits = new List<RaycastResult>();
                raycaster.Raycast(pointer, canvasHits);
                Assert.IsNotEmpty(canvasHits, "The HOST canvas raycast found no field plate.");
                var allHits = new List<RaycastResult>();
                system.RaycastAll(pointer, allHits);
                Assert.IsNotEmpty(allHits, "The EventSystem found no visible HOST control.");
                Assert.AreSame(plate.gameObject, canvasHits[0].gameObject,
                    "Another HOST graphic covers the visible room-name plate.");
                Assert.AreSame(plate.gameObject, allHits[0].gameObject,
                    "Another canvas intercepts the room-name plate.");

                system.SetSelectedGameObject(null);
                pointer.pointerPressRaycast = allHits[0];
                pointer.pointerCurrentRaycast = allHits[0];
                Assert.AreSame(field.gameObject,
                    ExecuteEvents.ExecuteHierarchy(allHits[0].gameObject, pointer, ExecuteEvents.pointerDownHandler));
                Assert.AreSame(field.gameObject,
                    ExecuteEvents.ExecuteHierarchy(allHits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler));
                yield return null;
                yield return null;
                Assert.AreSame(field.gameObject, system.currentSelectedGameObject);
                Assert.IsTrue(field.isFocused, "The raycast press did not activate the InputField.");

                field.MoveTextEnd(false);
                field.ProcessEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Z, character = 'Z' });
                Assert.AreEqual(full, field.text, "A full 24-character prefill must reject an append, not replacement.");
                field.ProcessEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.A,
                    modifiers = EventModifiers.Control });
                field.ProcessEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Backspace });
                Assert.IsEmpty(field.text, "Control+A then Backspace did not replace the full prefill.");
                const string edited = "QA04 ROOM EDITED";
                foreach (char c in edited)
                    field.ProcessEvent(new Event { type = EventType.KeyDown, character = c });
                field.ForceLabelUpdate();
                yield return null;
                Canvas.ForceUpdateCanvases();
                Assert.AreEqual(edited, field.text);
                StringAssert.Contains(edited, field.textComponent.text);
                for (int i = 0; i < HoldFrames; i++) yield return null;
                Assert.AreSame(field.gameObject, system.currentSelectedGameObject);
                Assert.IsTrue(field.isFocused, "The edited room-name field lost focus before CREATE.");
                yield return TumpUiCapture.Capture("qa04-host-name-edited-960x540", hub.Canvas,
                    960, 540, false, checkActionBounds: true);

                yield return HubFlowTests.Press("CreateLobby"); // HOST defaults to LAN.
                float until = Time.realtimeSinceStartup + 15;
                while (!(hub.Top is TumbangPreso.UI.Hub.HubLobby) && Time.realtimeSinceStartup < until)
                    yield return null;
                Assert.IsInstanceOf<TumbangPreso.UI.Hub.HubLobby>(hub.Top,
                    "CREATE did not open the local room; inspect the host status separately.");
                Assert.IsFalse(TumbangPreso.Net.NetSession.Instance.IsRelay);
                Assert.AreEqual(edited, TumbangPreso.Net.NetSession.RoomTitle);
                Assert.AreEqual(edited, hub.Host.RoomTitle);
                Assert.AreEqual(edited, TumbangPreso.Net.NetSession.Instance.Beacon.HostName);
                var heading = hub.Top.Root.Find("Heading").GetComponent<Text>();
                Assert.AreEqual(edited, heading.text);
                yield return TumpUiCapture.Capture("qa04-host-name-created-960x540", hub.Canvas,
                    960, 540, false, checkActionBounds: true);
            }
            finally
            {
                var hub = TumbangPreso.UI.Hub.TumpHub.Current;
                if (hub != null) hub.Host.LeaveRoom();
                TumbangPreso.Net.NetSession.Instance?.Stop();
                TumbangPreso.Net.NetSession.ClearRoomSettings();
                TumbangPreso.Net.NetSession.RoomTitle = roomTitle;
                TumbangPreso.Net.NetSession.RoomMap = roomMap;
                TumbangPreso.Net.NetSession.RoomVisibility = roomVisibility;
                TumbangPreso.UI.SceneFlow.Networked = networked;
                TumbangPreso.UI.SceneFlow.SelectedMap = map;
                TumbangPreso.UI.SceneFlow.AdoptRemoteRules(rules);
                TumbangPreso.UI.SceneFlow.SelectedRules.Password = rules.Password;
                if (pinned) TumbangPreso.UI.SceneFlow.PinSelectedRules(rules);
                else TumbangPreso.UI.SceneFlow.UnpinSelectedRules();
                JsonUtility.FromJsonOverwrite(savedSettings, settings);
                TumbangPreso.Settings.SettingsStore.Save();
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            }
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
                if (field.GetComponentsInParent<Canvas>().Any(c => !c.isActiveAndEnabled)) continue;
                var chat = field.GetComponentInParent<TumbangPreso.UI.LobbyChat>();
                if (chat != null && !chat.IsPresented) continue; // intentionally closed behind HOST/JOIN
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

            var door = FindByName("ChatDoor") ?? FindByName("ChatButton") ?? FindByName("ChatChip");
            var button = door != null ? door.GetComponent<Button>() : null;

            if (button != null && button.IsInteractable())
            {
                button.onClick.Invoke();
                report.AppendLine($"--- chat --- opened by pressing {door.name}");
            }

            for (int i = 0; i < HoldFrames; i++) yield return null;

            Assert.IsNotNull(button, "The live room has no visible chat door.");
            Assert.IsTrue(chats.Any(chat => chat != null && chat.IsPresented), "CHAT did not open its field.");

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
