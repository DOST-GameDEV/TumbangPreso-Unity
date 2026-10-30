using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Shows a renderer only in the local player's own view (`Camera.main`): never in a replay, another hero's cutscene camera, a
    /// film probe's witness camera or any other render. For things that belong to ONE screen, which are parented near that camera
    /// and would otherwise be drawn by every camera there.
    ///
    /// ⚠️ MOVED OUT OF `PhaisterOmenScreen.cs` WHEN THAT FILE WAS DELETED WITH HER RETIRED KIT (2026-10-29). Both of these are
    /// general camera filters rather than Phaister's: `HiddenFromMainCamera` is live on her hand props, Sean's molten look and
    /// `VoodooSoulDraw`. Nothing adds `MainCameraOnly` today (its two users, OMEN's veils and the manika at a cursed player's
    /// screen edge, went with that kit); it is kept as the other half of the pair rather than rewritten the next time a
    /// one-screen effect needs it.
    /// </summary>
    public sealed class MainCameraOnly : MonoBehaviour
    {
        private Renderer[] _renderers;

        public void Refresh() => _renderers = GetComponentsInChildren<Renderer>(true);

        private void OnEnable() { Refresh(); Camera.onPreCull += PreCull; }
        private void OnDisable() { Camera.onPreCull -= PreCull; }

        private void PreCull(Camera c)
        {
            if (_renderers == null) return;
            bool on = c != null && c == Camera.main;
            foreach (var r in _renderers) if (r != null) r.enabled = on;
        }
    }

    /// <summary>
    /// The opposite of <see cref="MainCameraOnly"/>: drawn by every camera EXCEPT the local player's own view. For a prop on a body
    /// the local player looks out of (HERO-10 film v8: the doll in her hand while she aims sat in front of her own first-person lens
    /// as an orange wall over a third of the frame; her body is shadows-only there, the prop was not).
    /// </summary>
    public sealed class HiddenFromMainCamera : MonoBehaviour
    {
        private Renderer[] _renderers;

        private void OnEnable() { _renderers = GetComponentsInChildren<Renderer>(true); Camera.onPreCull += PreCull; }
        private void OnDisable() { Camera.onPreCull -= PreCull; }

        private void PreCull(Camera c)
        {
            if (_renderers == null) return;
            bool on = c == null || c != Camera.main;
            foreach (var r in _renderers) if (r != null) r.enabled = on;
        }
    }
}
