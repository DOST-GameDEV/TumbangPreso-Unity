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
        private bool _padOnly;
        private int _revision = -1;

        public static HubPrompt Build(Transform parent, string name, string key, string pad, bool padOnly = false)
        {
            var rect = HubKit.Rect(parent, name);
            var prompt = rect.gameObject.AddComponent<HubPrompt>();
            prompt._key = key;
            prompt._pad = pad;
            prompt._padOnly = padOnly;
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
            // ⚠️ NO CAP FOR THE KEYBOARD'S BACK (2026-09-23 UI review). The ESC cap was drawn in the
            // corner of every hub BACK sticker, where it measured about 16 pixels at 1280x720 and
            // could not be read, and Escape is the one key every PC player already knows backs
            // out. The owner's concise-copy rule keeps Back as the arrow alone. A pad's cap stays,
            // because which face button backs out differs by pad family and is worth showing.
            if (device == InputDeviceKind.Touch || (_padOnly && device != InputDeviceKind.Gamepad))
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
