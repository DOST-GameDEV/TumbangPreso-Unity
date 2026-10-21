using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class RecordChoiceFocusTests
    {
        private GameObject _root;
        private PlayerAccount _account;
        private CareerStore _career;
        private SocialStore _social;
        private GameObject _selected;
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp]
        public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _account = GameServices.Account; _career = GameServices.Career; _social = GameServices.Social;
            Service("Account", null); Service("Career", null); Service("Social", null);
            _selected = EventSystem.current?.currentSelectedGameObject;
            _root = new GameObject("Current record-choice focus scope");
        }
        [UnityTearDown]
        public IEnumerator After()
        {
            if (_root != null) Object.Destroy(_root);
            yield return null;
            Service("Account", _account); Service("Career", _career); Service("Social", _social);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_selected != null && _selected.activeInHierarchy ? _selected : null);
            yield return PlayModeWorld.Reset();
        }
        private static void Service(string name, object value) => typeof(GameServices).GetProperty(name).SetValue(null, value);
        private static GameObject OpenActualList(RecordChoice choice)
        {
            choice.OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
            var list = (GameObject)typeof(RecordChoice).GetField("_openList", Hidden).GetValue(choice);
            Assert.IsNotNull(list); Assert.IsTrue(list.activeInHierarchy);
            Assert.IsTrue(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(list.transform));
            return list;
        }
        private static Toggle Option(GameObject list, string label)
            => list.GetComponentsInChildren<Toggle>().Single(toggle => toggle.GetComponentInChildren<Text>().text == label);
        private RecordChoice SimpleChoice(System.Action<int> changed)
        {
            var canvas = OwnerUiLayout.Canvas(_root.transform, "Current record-choice canvas", 500);
            return RecordChoice.Create(canvas.transform, "DirectRecordChoice", new[] { "FIRST", "SECOND" }, 0, changed);
        }

        [UnityTest]
        public IEnumerator CareerModeRefreshDoesNotReselectItsRetiredChoice()
        {
            var hub = _root.AddComponent<PlayerHub>(); hub.Install(); hub.OpenTab(PlayerHub.Door.Career);
            var canvas = (Canvas)typeof(PlayerHub).GetField("_canvas", Hidden).GetValue(hub);
            var old = canvas.GetComponentsInChildren<RecordChoice>().Single(choice => choice.name == "CareerModeValue");
            int target = old.value == 0 ? 1 : 0;
            yield return null; // UGUI initializes its fade runner in the actual Start callback.
            var list = OpenActualList(old);
            var option = Option(list, target == 1 ? "HERO STRIKE" : "CLASSIC");
            option.Select(); option.isOn = true; // Real DropdownItem callback runs value change, refresh, then Hide.
            var replacement = canvas.GetComponentsInChildren<RecordChoice>().Single(choice => choice.name == "CareerModeValue" && choice != old);
            Assert.AreEqual(target, replacement.value, "The actual CareerMode callback did not redraw its new value.");
            var selected = EventSystem.current.currentSelectedGameObject;
            Assert.IsNotNull(selected); Assert.IsTrue(selected.activeInHierarchy,
                "Dropdown.Hide selected the old row after the current career page rebuilt its focus.");
            Assert.AreNotSame(old.gameObject, selected);
            yield return null;
            selected = EventSystem.current.currentSelectedGameObject;
            Assert.IsNotNull(selected); Assert.IsTrue(selected.activeInHierarchy, "The next frame lost navigation after the old row was destroyed.");
        }
        [UnityTest]
        public IEnumerator CancellingAnActiveChoiceReturnsFocusToItsOpener()
        {
            int changes = 0; var choice = SimpleChoice(_ => changes++);
            yield return null;
            OpenActualList(choice); choice.OnCancel(new BaseEventData(EventSystem.current));
            Assert.AreSame(choice.gameObject, EventSystem.current.currentSelectedGameObject);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.AreSame(choice.gameObject, EventSystem.current.currentSelectedGameObject); Assert.AreEqual(0, changes);
        }
        [UnityTest]
        public IEnumerator SelectingAnOptionWithoutARefreshKeepsTheNormalCallbackAndFocus()
        {
            int chosen = -1; var choice = SimpleChoice(value => chosen = value);
            yield return null;
            var list = OpenActualList(choice); var option = Option(list, "SECOND");
            option.Select(); option.isOn = true;
            Assert.AreEqual(1, chosen); Assert.AreEqual(1, choice.value);
            Assert.AreSame(choice.gameObject, EventSystem.current.currentSelectedGameObject);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.AreSame(choice.gameObject, EventSystem.current.currentSelectedGameObject);
        }
    }
}
