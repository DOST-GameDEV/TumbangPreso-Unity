using TumbangPreso.InputLayer;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// A key or button cap that names the control for the device in the player's hands.
    ///
    /// ⚠️ IT FOLLOWS `LastInputDevice`, SO A PLAYER WHO PICKS UP A PAD SEES THE PAD'S CAP ON THE NEXT
    /// FRAME, and it hides entirely on touch, where the sticker itself is the control
    /// (`CLAUDE.md` § 4a: "what does the prompt say on each?"). The menu's BACK and SELECT are the
    /// fixed `MenuNav` controls rather than rebindable actions, so their caps are named here; a
    /// prompt for a GAMEPLAY action must go through `Rebinding.DisplayNameFor` instead.
    /// </summary>
    public sealed class HubPrompt : MonoBehaviour
    {
        private Image _cap;
        private string _key, _pad;
        private int _revision = -1;

        public static HubPrompt Build(Transform parent, string name, string key, string pad)
        {
            var rect = HubKit.Rect(parent, name);
            var prompt = rect.gameObject.AddComponent<HubPrompt>();
            prompt._key = key;
            prompt._pad = pad;
            prompt._cap = rect.gameObject.AddComponent<Image>();
            prompt._cap.preserveAspect = true;
            prompt._cap.raycastTarget = false;
            prompt.Refresh();
            return prompt;
        }

        private void Update()
        {
            if (LastInputDevice.Revision != _revision) Refresh();
        }

        private void Refresh()
        {
            _revision = LastInputDevice.Revision;
            var device = LastInputDevice.Current;
            if (device == InputDeviceKind.Touch)
            {
                _cap.enabled = false;
                return;
            }

            var sprite = InputGlyphs.For(device == InputDeviceKind.Gamepad ? _pad : _key, onDark: true);
            _cap.sprite = sprite;
            _cap.enabled = sprite != null;
        }
    }
}
