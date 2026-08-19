using System.Collections;
using NUnit.Framework;
using TumbangPreso.Net;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Sign-in runs once, at boot, and every later caller shares that one attempt.
    ///
    /// ⚠️ THIS CANNOT BE AN EDIT MODE TEST AND THAT IS THE WHOLE POINT.
    /// `RuntimeInitializeOnLoadMethod` never fires outside Play Mode, and UGS refuses to
    /// initialise outside it either, so the Edit Mode probes can only prove that repeated
    /// callers share an attempt. Whether the attempt happens at BOOT, without anybody asking,
    /// is only observable from here.
    ///
    /// ⚠️ IT ASSERTS ON A COUNTER, NOT ON THE IDENTITY OF THE RETURNED TASK. The obvious probe,
    /// checking that two callers get the same Task object back, is green even when caching is
    /// broken: an `async Task&lt;bool&gt;` that completes synchronously returns a SHARED cached
    /// Task for false, so two separate attempts compare equal. That version of this probe
    /// passed while proving nothing.
    /// </summary>
    public class OnlineSignInProbe
    {
        [UnityTest]
        public IEnumerator SignInRunsOnceAtBootAndLaterCallersShareIt()
        {
            // The boot hook runs at AfterSceneLoad, so one frame is enough for it to have fired.
            yield return null;

            int atBoot = NetIdentity.SignInAttempts;
            Assert.AreEqual(1, atBoot,
                "Boot must run exactly one sign-in attempt without anybody asking for it");

            Assert.AreNotEqual(OnlineState.Unknown, NetIdentity.State,
                "The boot attempt must settle the state rather than leave it Unknown");
            Assert.IsNotEmpty(NetIdentity.StateReason,
                "A settled state must carry the one sentence that explains it");

            // The five call sites that used to each start their own attempt.
            _ = NetIdentity.EnsureSignedInAsync();
            _ = NetIdentity.EnsureSignedInAsync();
            _ = NetIdentity.EnsureSignedInAsync();
            _ = NetIdentity.EnsureSignedInAsync();
            _ = NetIdentity.EnsureSignedInAsync();

            Assert.AreEqual(atBoot, NetIdentity.SignInAttempts,
                "Later callers must await the boot attempt, not start attempts of their own");
        }

        /// <summary>
        /// ⚠️ A LINKED BUILD MUST NEVER REPORT ITSELF AS UNLINKED. The first version of the
        /// three-way split caught `ServicesInitializationException` and called it NotLinked,
        /// which read "this build is not linked to a UGS project" at an editor that had simply
        /// declined to start services. Getting that backwards sends the reader to fix project
        /// settings that were never wrong.
        /// </summary>
        [UnityTest]
        public IEnumerator ALinkedBuildNeverSettlesOnNotLinked()
        {
            yield return null;

            if (string.IsNullOrEmpty(Application.cloudProjectId)) Assert.Ignore(
                "This build genuinely has no cloudProjectId, so NotLinked is the right answer");

            Assert.AreNotEqual(OnlineState.NotLinked, NetIdentity.State,
                $"cloudProjectId is set, so NotLinked is wrong. Reason given: {NetIdentity.StateReason}");
        }
    }
}
