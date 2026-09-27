using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    public sealed class OwnerLoginFeedbackTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest, Timeout(30000)]
        public IEnumerator InvalidFieldsKeepTheirOwnMessagesAndPulseWithoutMovingTheirHitBoxes()
        {
            var owner = new GameObject("LoginFeedbackFixture");
            bool reduced = Settings.SettingsStore.Current.ReducedUiMotion;
            try
            {
                var login = owner.AddComponent<SignInScreen>(); login.Install(); login.OpenForUpgrade();
                var canvas = GameObject.Find("OwnerSignInCanvas").GetComponent<Canvas>();
                var fields = canvas.GetComponentsInChildren<InputField>(true);
                var user = fields.Single(x => x.name == "Username");
                var pass = fields.Single(x => x.name == "Password");
                var confirm = fields.Single(x => x.name == "ConfirmPassword");
                var submit = canvas.GetComponentsInChildren<Button>(true).Single(x => x.name == "SubmitAccount");
                user.text = pass.text = confirm.text = "";
                yield return null;
                Assert.IsEmpty(Fault(canvas, "UsernameFault"));
                Assert.IsEmpty(Fault(canvas, "ConfirmFault"));
                submit.onClick.Invoke(); // Every attempt here is invalid; never reaches an account service.
                yield return null;
                Assert.AreEqual("Enter a username.", Fault(canvas, "UsernameFault"));
                Assert.AreEqual("Enter a password.", Fault(canvas, "PasswordFault"));
                Assert.AreEqual("Confirm your password.", Fault(canvas, "ConfirmFault"));
                yield return null;
                Assert.AreEqual("Confirm your password.", Fault(canvas, "ConfirmFault"), "Watcher erased an uncorrected required field.");

                user.text = "feedback.fixture"; pass.text = "Test-only1";
                yield return null;
                Assert.IsEmpty(Fault(canvas, "UsernameFault")); Assert.IsEmpty(Fault(canvas, "PasswordFault"));
                Settings.SettingsStore.Current.ReducedUiMotion = true;
                var rect = (RectTransform)confirm.transform;
                var position = rect.anchoredPosition; var size = rect.sizeDelta;
                var scale = rect.localScale; var ink = confirm.image.color;
                submit.onClick.Invoke();
                Assert.IsFalse(user.GetComponent<OwnerFieldPulse>().IsPulsing);
                Assert.IsFalse(pass.GetComponent<OwnerFieldPulse>().IsPulsing);
                Assert.IsTrue(confirm.GetComponent<OwnerFieldPulse>().IsPulsing);
                Assert.AreNotEqual(ink, confirm.image.color);
                Assert.AreEqual(position, rect.anchoredPosition); Assert.AreEqual(size, rect.sizeDelta); Assert.AreEqual(scale, rect.localScale);
                confirm.text = pass.text;
                yield return null;
                Assert.IsEmpty(Fault(canvas, "ConfirmFault"));
                Assert.IsFalse(confirm.GetComponent<OwnerFieldPulse>().IsPulsing);
                Assert.AreEqual(ink, confirm.image.color);

                typeof(SignInScreen).GetMethod("SetMode", Private).Invoke(login, new object[] { false });
                var fail = typeof(SignInScreen).GetMethod("Fail", Private);
                fail.Invoke(login, new object[] { "Invalid credentials" });
                yield return null;
                Assert.AreEqual("TUMP ID or password invalid.", Fault(canvas, "UsernameFault"));
                Assert.IsTrue(user.GetComponent<OwnerFieldPulse>().IsPulsing);
                Assert.IsTrue(pass.GetComponent<OwnerFieldPulse>().IsPulsing);
                pass.text = "Corrected-only2";
                yield return null;
                Assert.IsEmpty(Fault(canvas, "UsernameFault"), "Changing either credential must clear the pair's server verdict.");
                fail.Invoke(login, new object[] { "Service unavailable" });
                Assert.AreEqual("Service unavailable", Fault(canvas, "AccountStatus"));
                Assert.IsEmpty(Fault(canvas, "PasswordFault"), "A service outage is not a bad password.");
            }
            finally
            {
                Settings.SettingsStore.Current.ReducedUiMotion = reduced;
                Object.Destroy(owner);
            }
        }

        private static string Fault(Canvas canvas, string name) =>
            canvas.GetComponentsInChildren<Text>(true).Single(x => x.name == name).text;
    }
}
