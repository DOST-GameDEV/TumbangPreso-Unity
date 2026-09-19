using System.Collections;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// CHANGE PASSWORD, which is the only screen in the game that can change one.
    ///
    /// ⚠️⚠️ NOTHING HERE PRESSES SAVE ON A VALID FORM AND THAT IS DELIBERATE. A valid press
    /// reaches `PlayerAccount.ChangePasswordAsync` and therefore the live service, against the
    /// owner's own account, from a test run that happens a dozen times a day. What this fixture
    /// owns is everything up to that call: the shape of the sheet, and the four ways it refuses
    /// before it asks the service anything. The service half is `UgsCheck`'s territory.
    /// </summary>
    public sealed class OwnerPasswordTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [UnityTest]
        public IEnumerator ChangePasswordRefusesEveryBadFormBeforeItAsksTheService()
        {
            // ⚠⚠ OVER THE REAL SCREEN, NOT AN EMPTY ONE, AND NOT ONLY FOR THE PICTURE.
            // `CLAUDE.md` § 6.2b: a scrim and a paper sheet are numbers tuned against what is
            // behind them. It is also the difference between a capture and no capture at all,
            // because `UiRuntimeShots.Capture` photographs through `Camera.main` and an empty
            // scene has none: the first version of this fixture passed while writing no PNG.
            yield return SceneManager.LoadSceneAsync(SceneFlow.MainMenu);
            yield return null;
            var owner = new GameObject("PasswordFixture");
            try
            {
                var view = OwnerPasswordView.Open(owner.transform);
                yield return null;
                var canvas = GameObject.Find("OwnerPasswordCanvas").GetComponent<Canvas>();
                Assert.IsNotNull(canvas);

                var fields = canvas.GetComponentsInChildren<InputField>();
                Assert.AreEqual(3, fields.Length, "Current, new and confirm.");
                foreach (var field in fields)
                    Assert.AreEqual(InputField.ContentType.Password, field.contentType,
                        field.name + " must start hidden.");

                var current = fields.First(f => f.name == "CurrentPassword");
                var next = fields.First(f => f.name == "NewPassword");
                var confirm = fields.First(f => f.name == "ConfirmNewPassword");

                // ⚠️ THE KEY IS ON THE FIELD THE PLAYER ALREADY HAS. Her sheet drew it for FORGOT
                // PASSWORD and this screen borrows it, so a re-cut that drops the piece fails here.
                Assert.IsNotNull(current.GetComponentsInChildren<Image>(true)
                    .FirstOrDefault(i => i.name == "OriginalKey"), "The current field carries her key.");

                yield return UiRuntimeShots.Capture("OwnerPassword-v1", 1920, 1080);

                // --- nothing typed at all -------------------------------------------------
                Press(canvas, "SavePassword");
                yield return null;
                Assert.AreEqual("Enter your current password.", Fault(canvas, "CurrentPasswordFault"));
                Assert.IsNotEmpty(Fault(canvas, "NewPasswordFault"), "An empty new password is refused on a press.");
                Assert.IsTrue(view.IsOpen, "A refused form must not close.");

                // --- a new password the service would refuse -------------------------------
                current.text = "whatever-it-was";
                next.text = "short";
                confirm.text = "short";
                yield return null;
                Assert.AreEqual(AccountRules.PasswordFault("short"), Fault(canvas, "NewPasswordFault"),
                    "The live line is the shared rule's own sentence, not a second opinion.");
                Assert.IsEmpty(Fault(canvas, "CurrentPasswordFault"), "Typing a current password clears its fault.");

                // --- a confirmation that does not match ------------------------------------
                next.text = "Test-only1";
                confirm.text = "Test-only2";
                yield return null;
                Assert.IsEmpty(Fault(canvas, "NewPasswordFault"));
                Assert.AreEqual("Passwords do not match.", Fault(canvas, "ConfirmNewPasswordFault"));
                Press(canvas, "SavePassword");
                yield return null;
                Assert.IsTrue(view.IsOpen, "A mismatch must not reach the service.");

                // --- and the eye, on each of the three --------------------------------------
                Press(canvas, "RevealNewPassword");
                yield return null;
                Assert.AreEqual(InputField.ContentType.Standard, next.contentType);
                Press(canvas, "RevealNewPassword");
                yield return null;
                Assert.AreEqual(InputField.ContentType.Password, next.contentType);

                yield return UiRuntimeShots.Capture("OwnerPassword-faults-v1", 1920, 1080);

                Press(canvas, "PasswordBack");
                yield return null;
                Assert.IsFalse(view.IsOpen, "CANCEL must leave.");
            }
            finally { Object.DestroyImmediate(owner); }
        }

        private static string Fault(Canvas canvas, string name) =>
            canvas.GetComponentsInChildren<Text>(true).First(t => t.name == name).text;

        private static void Press(Canvas canvas, string name)
        {
            var button = canvas.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == name);
            Assert.IsNotNull(button, name + " is not on the sheet. It has: " +
                string.Join(", ", canvas.GetComponentsInChildren<Button>(true).Select(b => b.name)));
            button.onClick.Invoke();
        }
    }
}
