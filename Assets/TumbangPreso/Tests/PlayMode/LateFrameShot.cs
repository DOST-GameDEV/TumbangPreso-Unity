using System.Collections;
using UnityEngine;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Runs a capture after every LateUpdate, so a review frame shows the pose actually drawn this frame. Presentation
    /// that is put back in Update and posed in LateUpdate (the can's raise, hop and clatter) always looks at rest to a
    /// shot taken from a test coroutine, which runs between the two.
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public sealed class LateFrameShot : MonoBehaviour
    {
        public System.Action<string> Shoot;
        public System.Action Each;
        public string Pending;

        private void LateUpdate()
        {
            Each?.Invoke();
            if (Pending != null && Shoot != null) { Shoot(Pending); Pending = null; }
        }

        /// <summary>Takes <paramref name="name"/> at the end of this frame's LateUpdate and waits for it.</summary>
        public IEnumerator Take(string name) { Pending = name; while (Pending != null) yield return null; }
    }
}
