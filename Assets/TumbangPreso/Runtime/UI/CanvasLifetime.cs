using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Ties a canvas that had to be built at the scene root to the object that owns it.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE `MenuKit.BuildCanvas` NOW DETACHES A TAKEOVER SCREEN FROM ITS OWNER,
    /// AND A DETACHED CANVAS OUTLIVES THE THING THAT MADE IT. `docs/TODO.md` § 111.2: a canvas
    /// nested inside another canvas silently ignores its own `CanvasScaler`, which is what drew the
    /// boot account screen as a floating form on the wrong side of the screen. The fix is to build
    /// it at the root; the cost is that `Destroy(owner)` no longer takes it with them, and a screen
    /// left behind after its owner is gone is a full-screen canvas nothing can close.
    ///
    /// ⚠️ IT WATCHES RATHER THAN BEING TOLD, for the same reason `PlayerNameplate.Update` watches
    /// the overlays: an owner destroyed with its scene never gets a chance to tidy up, and a rule
    /// that depends on somebody remembering is a rule that stops working the first time somebody
    /// adds a screen.
    ///
    /// ⚠️ AND IT COSTS ONE NULL CHECK A FRAME. Unity's fake-null on a destroyed object is what
    /// makes this reliable; there is no event for "my owner was destroyed".
    /// </summary>
    public sealed class CanvasLifetime : MonoBehaviour
    {
        private Object _owner;

        public void Bind(Object owner) => _owner = owner;

        private void LateUpdate()
        {
            if (_owner == null) Destroy(gameObject);
        }
    }
}
