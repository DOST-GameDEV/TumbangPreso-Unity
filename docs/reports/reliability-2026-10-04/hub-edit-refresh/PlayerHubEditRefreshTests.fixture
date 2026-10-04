using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.Net;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class PlayerHubEditRefreshTests
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private PlayerAccount _oldAccount, _account;
        private CareerStore _oldCareer;
        private SocialStore _oldSocial;
        private GameObject _accountRoot, _hubRoot, _oldSelected;
        private PlayerHub _hub;

        [UnitySetUp] public IEnumerator Before()
        {
            yield return PlayModeWorld.Reset();
            _oldAccount = GameServices.Account; _oldCareer = GameServices.Career; _oldSocial = GameServices.Social;
            _oldSelected = EventSystem.current?.currentSelectedGameObject;
            _accountRoot = new GameObject("Dormant edit-flow account"); _accountRoot.SetActive(false);
            _account = _accountRoot.AddComponent<PlayerAccount>();
            SetOwner("edit-owner-a");
            Service("Account", _account); Service("Career", null); Service("Social", null);
            _hubRoot = new GameObject("Current edit-flow hub");
            _hub = _hubRoot.AddComponent<PlayerHub>(); _hub.Install(); _hub.OpenTab(PlayerHub.Door.Profile);
            yield return null;
            Assert.IsNotNull(EventSystem.current);
            Assert.IsTrue(_hub.IsOpen);
        }
        [UnityTearDown] public IEnumerator After()
        {
            if (_hubRoot != null) Object.Destroy(_hubRoot);
            yield return null;
            if (_accountRoot != null) Object.Destroy(_accountRoot);
            Service("Account", _oldAccount); Service("Career", _oldCareer); Service("Social", _oldSocial);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(_oldSelected != null && _oldSelected.activeInHierarchy ? _oldSelected : null);
            yield return PlayModeWorld.Reset();
        }
        private static void Service(string name, object value) => typeof(GameServices).GetProperty(name).SetValue(null, value);
        private void SetOwner(string id) => typeof(PlayerAccount).GetField("_profile", Hidden).SetValue(_account,
            AccountRules.Normalise(new AccountProfile { PlayerId = id, DisplayName = "FlowOwner" }));
        private Canvas HubCanvas => (Canvas)typeof(PlayerHub).GetField("_canvas", Hidden).GetValue(_hub);
        private InputField Field(string name) => HubCanvas.GetComponentsInChildren<InputField>()
            .Single(field => field.name == name);
        private IEnumerator OpenSearch()
        {
            _hub.OpenTab(PlayerHub.Door.Party); yield return null;
            if (!HubCanvas.GetComponentsInChildren<InputField>().Any(field => field.name == "FriendSearch"))
            {
                HubCanvas.GetComponentsInChildren<Button>().Single(button =>
                    button.transform.parent.name == "Group_Find a friend").onClick.Invoke();
                yield return null;
            }
        }
        private static IEnumerator Edit(InputField field, string text)
        {
            field.text = text;
            EventSystem.current.SetSelectedGameObject(field.gameObject);
            field.ActivateInputField();
            yield return null;
            Assert.IsTrue(field.isFocused, "Precondition: actual input field is editing.");
        }

        [UnityTest] public IEnumerator ProfileRefreshKeepsTheActiveDraftField()
        {
            yield return Edit(Field("PlayerNameEdit"), "DraftOwner");
            _hub.SendMessage("OnDataChanged");
            yield return null; yield return null;
            var field = Field("PlayerNameEdit");
            Assert.AreEqual("DraftOwner", field.text);
            Assert.IsTrue(field.isFocused, "Same-owner refresh interrupted profile editing.");
            Assert.AreSame(field.gameObject, EventSystem.current.currentSelectedGameObject);
        }
        [UnityTest] public IEnumerator FriendRefreshKeepsTheActiveSearchField()
        {
            yield return OpenSearch();
            yield return Edit(Field("FriendSearch"), "MARIA#4417");
            _hub.SendMessage("OnDataChanged");
            yield return null; yield return null;
            var field = Field("FriendSearch");
            Assert.AreEqual("MARIA#4417", field.text);
            Assert.IsTrue(field.isFocused, "Same-owner refresh interrupted friend search.");
            Assert.AreSame(field.gameObject, EventSystem.current.currentSelectedGameObject);
        }
        [UnityTest] public IEnumerator ChangingTabsDoesNotRestoreTheOldField()
        {
            yield return Edit(Field("PlayerNameEdit"), "DraftOwner");
            yield return OpenSearch();
            yield return null; yield return null;
            Assert.IsFalse(Field("FriendSearch").isFocused);
            Assert.IsFalse(HubCanvas.GetComponentsInChildren<InputField>().Any(field => field.name == "PlayerNameEdit"));
        }
        [UnityTest] public IEnumerator ReplacingTheAccountRetiresItsSearchAndEditing()
        {
            yield return OpenSearch();
            yield return Edit(Field("FriendSearch"), "MARIA#4417");
            SetOwner("edit-owner-b");
            _hub.SendMessage("OnDataChanged");
            yield return null; yield return null;
            Assert.AreEqual("", Field("FriendSearch").text);
            Assert.IsFalse(Field("FriendSearch").isFocused);
        }
    }
}
