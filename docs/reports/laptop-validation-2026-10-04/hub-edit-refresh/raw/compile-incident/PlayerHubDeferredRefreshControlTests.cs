using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class PlayerHubDeferredRefreshControlTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly PlayerHubEditRefreshTests _world = new();
        private PlayerHub _hub;
        private PlayerAccount _account;
        private Mouse _mouse;
        private InputSettings.BackgroundBehavior _background;
        private InputSettings.EditorInputBehaviorInPlayMode _editor;
        private Canvas Canvas => (Canvas)typeof(PlayerHub).GetField("_canvas", Hidden).GetValue(_hub);
        private InputField Field(string name) => Canvas.GetComponentsInChildren<InputField>().Single(f => f.name == name);

        [UnitySetUp] public IEnumerator Before()
        {
            _background = InputSystem.settings.backgroundBehavior;
            _editor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            yield return _world.Before();
            _hub = GameObject.Find("Current edit-flow hub").GetComponent<PlayerHub>();
            _account = GameServices.Account;
            _mouse = InputSystem.AddDevice<Mouse>(); InputSystem.EnableDevice(_mouse);
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_mouse != null && _mouse.added) InputSystem.RemoveDevice(_mouse);
            yield return _world.After();
            InputSystem.settings.backgroundBehavior = _background;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editor;
        }
        private IEnumerator Edit(InputField field)
        {
            field.text = "DraftOwner";
            EventSystem.current.SetSelectedGameObject(field.gameObject); field.ActivateInputField();
            yield return null; Assert.IsTrue(field.isFocused);
        }
        private void Bio(string value) => ((AccountProfile)typeof(PlayerAccount).GetField("_profile", Hidden).GetValue(_account)).Bio = value;
        private void MouseHeld(bool down)
        {
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = Vector2.zero, buttons = down ? (ushort)1 : (ushort)0 });
            InputSystem.Update();
        }

        [UnityTest] public IEnumerator EditingKeepsSelectionThenBlurRefreshesNoneditedBio()
        {
            Canvas.GetComponentsInChildren<Button>().Single(b => b.transform.parent.name == "Group_Optional details").onClick.Invoke();
            yield return null;
            var field = Field("PlayerNameEdit"); yield return Edit(field);
            field.selectionAnchorPosition = 2; field.selectionFocusPosition = 5;
            var oldBio = Field("ProfileBio"); string before = oldBio.text;
            Bio("Fresh backend bio"); _hub.SendMessage("OnDataChanged");
            yield return null; yield return null;
            Assert.AreSame(field, Field("PlayerNameEdit")); Assert.IsTrue(field.isFocused);
            Assert.AreEqual(2, field.selectionAnchorPosition); Assert.AreEqual(5, field.selectionFocusPosition);
            Assert.AreSame(oldBio, Field("ProfileBio")); Assert.AreEqual(before, oldBio.text, "Rows rebuilt while editing was active.");
            field.DeactivateInputField(); EventSystem.current.SetSelectedGameObject(null);
            yield return null; yield return null; yield return null;
            Assert.AreEqual("Fresh backend bio", Field("ProfileBio").text, "Deferred backend data never reached the rows after blur.");
            Assert.AreEqual("DraftOwner", Field("PlayerNameEdit").text);
        }

        [UnityTest] public IEnumerator PointerUpDeliversItsButtonBeforePendingRowsRebuild()
        {
            _hub.OpenTab(PlayerHub.Door.Party); yield return null;
            Canvas.GetComponentsInChildren<Button>().Single(b => b.transform.parent.name == "Group_Find a friend").onClick.Invoke();
            yield return null;
            var button = Canvas.GetComponentsInChildren<Button>().Single(b => b.name == "SendFriendRequest");
            int delivered = 0; button.onClick.AddListener(() => ++delivered);
            var module = EventSystem.current.currentInputModule as InputSystemUIInputModule;
            Assert.IsNotNull(module); Assert.IsNotNull(module.leftClick?.action);
            MouseHeld(true); Assert.IsTrue(module.leftClick.action.IsPressed(), "Actual UI action did not observe the supplied mouse hold.");
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, pointerId = -1 };
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            _hub.SendMessage("OnDataChanged"); yield return null; yield return null;
            Assert.IsTrue(button != null && button.isActiveAndEnabled, "Pending refresh destroyed the pressed button before pointer-up.");
            Assert.Zero(delivered);
            MouseHeld(false);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.AreEqual(1, delivered);
            yield return null; yield return null;
            Assert.AreEqual(1, delivered);
            Assert.IsTrue(Canvas.GetComponentsInChildren<Button>().Any(b => b.name == "SendFriendRequest"));
        }

        [UnityTest] public IEnumerator ClosingWithPendingRefreshCannotReopenTheHub()
        {
            var field = Field("PlayerNameEdit"); yield return Edit(field);
            Bio("Pending while closed"); _hub.SendMessage("OnDataChanged");
            yield return null; Assert.AreSame(field, Field("PlayerNameEdit"));
            _hub.Close(); yield return null; yield return null; yield return null;
            Assert.IsFalse(_hub.IsOpen);
            Assert.IsFalse(((GameObject)typeof(PlayerHub).GetField("_root", Hidden).GetValue(_hub)).activeSelf);
        }
    }
}
