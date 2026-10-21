using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Owner 2026-10-21: *"when tagged it doesnt teleport the character back to the safezone"*. A stale event feed threw out
    /// of `MatchFlair.Play`, and the tag's teleport (and the block's knockback) come after the announce. A listener that
    /// throws is logged and passed; the next listener and the caller carry on.
    /// </summary>
    public sealed class MatchFlairListenerTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        [Test]
        public void AThrowingListenerCannotStopTheNextListenerOrTheCaller()
        {
            int reached = 0;
            System.Action<MatchFlair.Kind, int, int, Vector3, float> broken = (k, a, s, at, f) => throw new System.NullReferenceException("stale feed");
            System.Action<MatchFlair.Kind, int, int, Vector3, float> healthy = (k, a, s, at, f) => reached++;
            MatchFlair.Presented += broken;
            MatchFlair.Presented += healthy;
            try
            {
                LogAssert.Expect(LogType.Exception, new Regex("stale feed"));
                Assert.DoesNotThrow(() => MatchFlair.Play(MatchFlair.Kind.NearMiss, 0, -1, Vector3.zero));
                Assert.AreEqual(1, reached, "The listener after the broken one was skipped.");
            }
            finally
            {
                MatchFlair.Presented -= broken;
                MatchFlair.Presented -= healthy;
            }
        }
    }
}
